using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.EventSystems; // UI 포인터 이벤트 기능
using UnityEngine.InputSystem; // 새로운 입력 시스템
using UnityEngine.UI; // Unity UI 기능

public sealed class InventorySlotView : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler // 슬롯 클릭과 드래그 처리
{
    [SerializeField] private TMP_Text shortcutText; // 숫자키 표시 Text
    [SerializeField] private TMP_Text itemNameText; // 아이템 이름 Text
    [SerializeField] private TMP_Text quantityText; // 아이템 수량 Text
    [SerializeField] private Outline selectionOutline; // 선택 테두리

    private PlayerInventory playerInventory; // 연결된 플레이어 인벤토리
    private int inventoryIndex; // 실제 인벤토리 슬롯 번호
    private bool allowItemDrag; // 드래그 허용 여부
    private bool requireAltKeyForDrag; // Alt 드래그 요구 여부
    private bool referencesValid; // UI 참조 연결 상태
    private bool isDragging; // 현재 드래그 상태
    private bool isSelected; // 현재 선택 상태
    private GameObject dragPreviewObject; // 드래그 복제 슬롯
    private RectTransform dragPreviewRect; // 복제 슬롯 위치 정보
    private RectTransform dragCanvasRect; // 최상위 Canvas 위치 정보

    private void Awake() // UI 참조 검사
    {
        referencesValid = shortcutText != null && itemNameText != null && quantityText != null && selectionOutline != null; // 필수 참조 검사

        if (!referencesValid) // 참조 누락 확인
        {
            Debug.LogError($"{gameObject.name}의 슬롯 UI 참조를 모두 연결해야 합니다.", this); // 연결 오류 출력
        }
    }

    private void OnDisable() // 비활성화 상태 정리
    {
        CleanupDragPreview(); // 남은 드래그 화면 제거
    }

    public void Configure(PlayerInventory newPlayerInventory, int newInventoryIndex, bool newAllowItemDrag, bool newRequireAltKeyForDrag) // 슬롯 기능 설정
    {
        playerInventory = newPlayerInventory; // 플레이어 인벤토리 저장
        inventoryIndex = newInventoryIndex; // 실제 슬롯 번호 저장
        allowItemDrag = newAllowItemDrag; // 드래그 허용 상태 저장
        requireAltKeyForDrag = newRequireAltKeyForDrag; // Alt 드래그 조건 저장
    }

    public void SetSlot(InventorySlot slot, int slotNumber, bool showShortcut, bool newIsSelected) // 슬롯 화면 갱신
    {
        if (!referencesValid) // 참조 상태 확인
        {
            return; // 화면 갱신 중단
        }

        isSelected = newIsSelected; // 선택 상태 저장
        shortcutText.gameObject.SetActive(showShortcut); // 숫자 표시 상태 적용
        shortcutText.SetText(slotNumber.ToString()); // 슬롯 번호 출력
        selectionOutline.enabled = isDragging || isSelected; // 테두리 상태 적용

        if (slot == null) // 빈 슬롯 확인
        {
            itemNameText.SetText(string.Empty); // 아이템 이름 제거
            quantityText.SetText(string.Empty); // 아이템 수량 제거
            return; // 빈 슬롯 처리 종료
        }

        itemNameText.SetText(slot.ItemData.DisplayName); // 아이템 이름 출력
        quantityText.SetText($"x{slot.Quantity}"); // 아이템 수량 출력
    }

    public void OnPointerClick(PointerEventData eventData) // 슬롯 클릭 선택 처리
    {
        if (eventData.button != PointerEventData.InputButton.Left) // 왼쪽 버튼 확인
        {
            return; // 다른 버튼 클릭 차단
        }

        if (!referencesValid || playerInventory == null) // 선택 가능 상태 확인
        {
            return; // 선택 처리 중단
        }

        playerInventory.SelectInventorySlot(inventoryIndex); // 클릭한 슬롯 선택
    }

    public void OnBeginDrag(PointerEventData eventData) // 드래그 시작 처리
    {
        if (eventData.button != PointerEventData.InputButton.Left) // 왼쪽 버튼 확인
        {
            eventData.pointerDrag = null; // 드래그 대상 제거
            return; // 다른 버튼 드래그 차단
        }

        if (!referencesValid || !CanDragItem() || playerInventory == null) // 드래그 가능 상태 확인
        {
            eventData.pointerDrag = null; // 드래그 대상 제거
            return; // 드래그 시작 차단
        }

        if (playerInventory.GetSlot(inventoryIndex) == null) // 현재 슬롯 아이템 확인
        {
            eventData.pointerDrag = null; // 드래그 대상 제거
            return; // 빈 슬롯 드래그 차단
        }

        playerInventory.SelectInventorySlot(inventoryIndex); // 드래그 출발 슬롯 선택
        isDragging = true; // 드래그 상태 활성화
        selectionOutline.enabled = true; // 출발 슬롯 테두리 표시
        CreateDragPreview(eventData); // 드래그 복제 화면 생성
    }

    public void OnDrag(PointerEventData eventData) // 드래그 위치 갱신
    {
        if (!isDragging) // 드래그 상태 확인
        {
            return; // 위치 갱신 중단
        }

        UpdateDragPreviewPosition(eventData); // 복제 슬롯 위치 이동
    }

    public void OnEndDrag(PointerEventData eventData) // 드래그 종료 처리
    {
        CleanupDragPreview(); // 복제 슬롯 제거

        if (selectionOutline != null) // 테두리 참조 확인
        {
            selectionOutline.enabled = isSelected; // 기존 선택 상태 복구
        }
    }

    public void OnDrop(PointerEventData eventData) // 아이템 놓기 처리
    {
        if (!CanDragItem() || playerInventory == null) // 드롭 가능 상태 확인
        {
            return; // 드롭 처리 중단
        }

        if (eventData.pointerDrag == null) // 출발 오브젝트 확인
        {
            return; // 드롭 처리 중단
        }

        InventorySlotView sourceView = eventData.pointerDrag.GetComponent<InventorySlotView>(); // 출발 슬롯 화면 가져오기

        if (sourceView == null || !sourceView.isDragging) // 올바른 출발 슬롯 확인
        {
            return; // 잘못된 드롭 차단
        }

        if (sourceView.playerInventory != playerInventory) // 같은 인벤토리 확인
        {
            return; // 다른 인벤토리 이동 차단
        }

        bool moveSucceeded = playerInventory.MoveOrMergeSlot(sourceView.inventoryIndex, inventoryIndex); // 이동 또는 합치기 실행

        if (moveSucceeded) // 이동 성공 확인
        {
            playerInventory.SelectInventorySlot(inventoryIndex); // 도착 슬롯 선택
        }
    }

    private bool CanDragItem() // 현재 드래그 허용 상태 확인
    {
        if (!allowItemDrag) // 기본 드래그 허용 확인
        {
            return false; // 드래그 차단
        }

        if (!requireAltKeyForDrag) // Alt 조건 사용 여부 확인
        {
            return true; // 일반 드래그 허용
        }

        Keyboard keyboard = Keyboard.current; // 현재 키보드 가져오기

        if (keyboard == null) // 키보드 존재 확인
        {
            return false; // Alt 확인 불가 처리
        }

        return keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed; // Alt 입력 상태 반환
    }

    private void CreateDragPreview(PointerEventData eventData) // 드래그 복제 화면 생성
    {
        Canvas currentCanvas = GetComponentInParent<Canvas>(); // 현재 슬롯 Canvas 가져오기
        Canvas rootCanvas = currentCanvas == null ? null : currentCanvas.rootCanvas; // 최상위 Canvas 가져오기

        if (rootCanvas == null) // Canvas 존재 확인
        {
            isDragging = false; // 드래그 상태 취소
            selectionOutline.enabled = isSelected; // 기존 테두리 복구
            return; // 복제 화면 생성 중단
        }

        dragCanvasRect = rootCanvas.transform as RectTransform; // Canvas 위치 정보 저장
        dragPreviewObject = Instantiate(gameObject, rootCanvas.transform); // 현재 슬롯 화면 복제
        dragPreviewObject.name = "InventoryDragPreview"; // 복제 화면 이름 설정

        InventorySlotView previewSlotView = dragPreviewObject.GetComponent<InventorySlotView>(); // 복제 슬롯 기능 가져오기

        if (previewSlotView != null) // 복제 슬롯 기능 확인
        {
            previewSlotView.enabled = false; // 복제 슬롯 입력 차단
        }

        CanvasGroup previewCanvasGroup = dragPreviewObject.GetComponent<CanvasGroup>(); // 복제 CanvasGroup 가져오기

        if (previewCanvasGroup == null) // CanvasGroup 존재 확인
        {
            previewCanvasGroup = dragPreviewObject.AddComponent<CanvasGroup>(); // CanvasGroup 추가
        }

        previewCanvasGroup.alpha = 0.85f; // 복제 화면 투명도 적용
        previewCanvasGroup.interactable = false; // 복제 화면 상호작용 차단
        previewCanvasGroup.blocksRaycasts = false; // 복제 화면 광선 차단 해제

        RectTransform sourceRect = transform as RectTransform; // 원본 슬롯 위치 정보
        dragPreviewRect = dragPreviewObject.transform as RectTransform; // 복제 슬롯 위치 정보 저장
        dragPreviewRect.anchorMin = new Vector2(0.5f, 0.5f); // 최소 앵커 설정
        dragPreviewRect.anchorMax = new Vector2(0.5f, 0.5f); // 최대 앵커 설정
        dragPreviewRect.pivot = new Vector2(0.5f, 0.5f); // 중심점 설정
        dragPreviewRect.sizeDelta = sourceRect.rect.size; // 원본 슬롯 크기 적용
        dragPreviewRect.localScale = Vector3.one; // 복제 화면 배율 초기화
        dragPreviewRect.SetAsLastSibling(); // 최상단 표시
        UpdateDragPreviewPosition(eventData); // 최초 마우스 위치 적용
    }

    private void UpdateDragPreviewPosition(PointerEventData eventData) // 복제 화면 마우스 추적
    {
        if (dragPreviewRect == null || dragCanvasRect == null) // 위치 정보 확인
        {
            return; // 위치 갱신 중단
        }

        bool positionFound = RectTransformUtility.ScreenPointToLocalPointInRectangle(dragCanvasRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint); // 화면 좌표 변환

        if (!positionFound) // 좌표 변환 결과 확인
        {
            return; // 위치 적용 중단
        }

        dragPreviewRect.anchoredPosition = localPoint; // 복제 화면 위치 적용
    }

    private void CleanupDragPreview() // 드래그 복제 화면 정리
    {
        if (dragPreviewObject != null) // 복제 화면 존재 확인
        {
            Destroy(dragPreviewObject); // 복제 화면 제거
        }

        dragPreviewObject = null; // 복제 오브젝트 참조 초기화
        dragPreviewRect = null; // 복제 위치 참조 초기화
        dragCanvasRect = null; // Canvas 위치 참조 초기화
        isDragging = false; // 드래그 상태 해제
    }
}