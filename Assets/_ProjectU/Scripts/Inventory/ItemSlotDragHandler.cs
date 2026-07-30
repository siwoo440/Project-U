using UnityEngine; // Unity 기본 기능
using UnityEngine.EventSystems; // UI 포인터 이벤트 기능
using UnityEngine.InputSystem; // 새로운 입력 시스템
using UnityEngine.UI; // Unity UI 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class ItemSlotDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler // 공통 슬롯 드래그 처리
{
    [Header("References")] // UI 참조 묶음
    [SerializeField] private Outline dragOutline; // 드래그 출발 슬롯 테두리

    private IItemSlotContainer slotContainer; // 현재 슬롯 소유 컨테이너
    private int slotIndex = -1; // 현재 데이터 슬롯 번호
    private bool allowItemDrag; // 아이템 드래그 허용 여부
    private bool requireAltKeyForDrag; // Alt 키 요구 여부
    private bool isDragging; // 현재 드래그 상태
    private bool restingOutlineEnabled; // 평상시 테두리 상태
    private GameObject dragPreviewObject; // 드래그 미리보기 오브젝트
    private RectTransform dragPreviewRect; // 미리보기 위치 정보
    private RectTransform dragCanvasRect; // 최상위 Canvas 위치 정보

    public bool IsDragging => isDragging; // 현재 드래그 상태 제공

    private void Awake() // 드래그 UI 참조 초기화
    {
        if (dragOutline == null) // Inspector 테두리 참조 확인
        {
            dragOutline = GetComponent<Outline>(); // 같은 오브젝트 테두리 자동 검색
        }

        if (dragOutline == null) // 테두리 컴포넌트 누락 확인
        {
            Debug.LogError($"{gameObject.name}의 ItemSlotDragHandler에 Outline이 필요합니다.", this); // 연결 오류 출력
            enabled = false; // 드래그 기능 비활성화
            return; // 초기화 중단
        }

        restingOutlineEnabled = dragOutline.enabled; // 초기 평상시 테두리 상태 저장
    }

    private void OnDisable() // 비활성화 상태 정리
    {
        CleanupDragPreview(); // 남은 드래그 미리보기 제거
    }

    public void Configure( // 드래그 대상 슬롯 설정
        IItemSlotContainer newSlotContainer, // 새로운 슬롯 컨테이너
        int newSlotIndex, // 새로운 슬롯 번호
        bool newAllowItemDrag, // 새로운 드래그 허용 상태
        bool newRequireAltKeyForDrag) // 새로운 Alt 요구 상태
    {
        slotContainer = newSlotContainer; // 슬롯 컨테이너 저장
        slotIndex = newSlotIndex; // 슬롯 번호 저장
        allowItemDrag = newAllowItemDrag; // 드래그 허용 상태 저장
        requireAltKeyForDrag = newRequireAltKeyForDrag; // Alt 요구 상태 저장
    }

    public void SetRestingOutlineState(bool shouldEnable) // 평상시 테두리 상태 갱신
    {
        restingOutlineEnabled = shouldEnable; // 새로운 평상시 상태 저장

        if (!isDragging && dragOutline != null) // 드래그 중이 아닌 테두리 확인
        {
            dragOutline.enabled = restingOutlineEnabled; // 평상시 테두리 상태 적용
        }
    }

    public void OnBeginDrag(PointerEventData eventData) // 드래그 시작 처리
    {
        if (eventData.button != PointerEventData.InputButton.Left) // 왼쪽 마우스 버튼 확인
        {
            eventData.pointerDrag = null; // 드래그 대상 제거
            return; // 다른 버튼 드래그 차단
        }

        if (!CanDragItem()) // 드래그 가능 상태 확인
        {
            eventData.pointerDrag = null; // 드래그 대상 제거
            return; // 드래그 시작 차단
        }

        InventorySlot currentSlot = slotContainer.GetSlot(slotIndex); // 현재 슬롯 데이터 조회

        if (currentSlot == null || currentSlot.ItemData == null || currentSlot.Quantity <= 0) // 현재 아이템 존재 확인
        {
            eventData.pointerDrag = null; // 드래그 대상 제거
            return; // 빈 슬롯 드래그 차단
        }

        if (slotContainer is PlayerInventory playerInventory) // 플레이어 인벤토리 슬롯 확인
        {
            playerInventory.SelectInventorySlot(slotIndex); // 드래그 출발 슬롯 선택
        }

        isDragging = true; // 드래그 상태 활성화
        dragOutline.enabled = true; // 출발 슬롯 테두리 표시
        CreateDragPreview(eventData); // 드래그 미리보기 생성

        if (dragPreviewObject == null) // 미리보기 생성 실패 확인
        {
            eventData.pointerDrag = null; // 드래그 대상 제거
        }
    }

    public void OnDrag(PointerEventData eventData) // 드래그 위치 갱신
    {
        if (!isDragging) // 드래그 상태 확인
        {
            return; // 위치 갱신 중단
        }

        UpdateDragPreviewPosition(eventData); // 미리보기 마우스 위치 적용
    }

    public void OnEndDrag(PointerEventData eventData) // 드래그 종료 처리
    {
        CleanupDragPreview(); // 미리보기와 테두리 상태 정리
    }

    public void OnDrop(PointerEventData eventData) // 아이템 놓기 처리
    {
        if (!allowItemDrag || slotContainer == null) // 대상 슬롯 사용 가능 상태 확인
        {
            return; // 드롭 처리 중단
        }

        if (eventData.pointerDrag == null) // 출발 오브젝트 존재 확인
        {
            return; // 드롭 처리 중단
        }

        ItemSlotDragHandler sourceHandler = eventData.pointerDrag.GetComponent<ItemSlotDragHandler>(); // 출발 슬롯 드래그 기능 조회

        if (sourceHandler == null || !sourceHandler.isDragging) // 올바른 출발 슬롯 여부 확인
        {
            return; // 잘못된 드롭 차단
        }

        bool transferSucceeded = ItemSlotTransferUtility.TryMoveOrMerge( // 공통 이동 처리 실행
            sourceHandler.slotContainer, // 출발 컨테이너 전달
            sourceHandler.slotIndex, // 출발 슬롯 번호 전달
            slotContainer, // 대상 컨테이너 전달
            slotIndex); // 대상 슬롯 번호 전달

        if (transferSucceeded && slotContainer is PlayerInventory playerInventory) // 플레이어 인벤토리 도착 여부 확인
        {
            playerInventory.SelectInventorySlot(slotIndex); // 이동한 도착 슬롯 선택
        }
    }

    private bool CanDragItem() // 현재 드래그 가능 상태 확인
    {
        if (!allowItemDrag || slotContainer == null) // 기본 설정과 컨테이너 확인
        {
            return false; // 드래그 차단
        }

        if (slotIndex < 0 || slotIndex >= slotContainer.SlotCapacity) // 슬롯 번호 범위 확인
        {
            return false; // 잘못된 슬롯 드래그 차단
        }

        if (!requireAltKeyForDrag) // Alt 키 조건 사용 여부 확인
        {
            return true; // 일반 드래그 허용
        }

        Keyboard keyboard = Keyboard.current; // 현재 키보드 조회

        if (keyboard == null) // 키보드 존재 확인
        {
            return false; // Alt 입력 확인 불가 처리
        }

        return keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed; // Alt 입력 상태 반환
    }

    private void CreateDragPreview(PointerEventData eventData) // 드래그 미리보기 생성
    {
        Canvas currentCanvas = GetComponentInParent<Canvas>(); // 현재 슬롯 Canvas 조회
        Canvas rootCanvas = currentCanvas == null ? null : currentCanvas.rootCanvas; // 최상위 Canvas 조회

        if (rootCanvas == null) // Canvas 존재 확인
        {
            CleanupDragPreview(); // 드래그 상태 정리
            return; // 미리보기 생성 중단
        }

        dragCanvasRect = rootCanvas.transform as RectTransform; // Canvas 위치 정보 저장
        dragPreviewObject = Instantiate(gameObject, rootCanvas.transform); // 현재 슬롯 화면 복제
        dragPreviewObject.name = "ItemSlotDragPreview"; // 미리보기 이름 적용

        ItemSlotDragHandler previewHandler = dragPreviewObject.GetComponent<ItemSlotDragHandler>(); // 복제 드래그 기능 조회

        if (previewHandler != null) // 복제 드래그 기능 존재 확인
        {
            previewHandler.enabled = false; // 복제 입력 기능 비활성화
        }

        CanvasGroup previewCanvasGroup = dragPreviewObject.GetComponent<CanvasGroup>(); // 복제 CanvasGroup 조회

        if (previewCanvasGroup == null) // CanvasGroup 존재 확인
        {
            previewCanvasGroup = dragPreviewObject.AddComponent<CanvasGroup>(); // 복제 오브젝트에 CanvasGroup 추가
        }

        previewCanvasGroup.alpha = 0.85f; // 미리보기 투명도 적용
        previewCanvasGroup.interactable = false; // 미리보기 상호작용 차단
        previewCanvasGroup.blocksRaycasts = false; // 미리보기 광선 차단 해제

        RectTransform sourceRect = transform as RectTransform; // 원본 슬롯 위치 정보 조회
        dragPreviewRect = dragPreviewObject.transform as RectTransform; // 미리보기 위치 정보 저장

        if (sourceRect == null || dragPreviewRect == null || dragCanvasRect == null) // 위치 정보 존재 확인
        {
            CleanupDragPreview(); // 잘못된 미리보기 정리
            return; // 위치 설정 중단
        }

        dragPreviewRect.anchorMin = new Vector2(0.5f, 0.5f); // 최소 앵커 중앙 설정
        dragPreviewRect.anchorMax = new Vector2(0.5f, 0.5f); // 최대 앵커 중앙 설정
        dragPreviewRect.pivot = new Vector2(0.5f, 0.5f); // 중심점 중앙 설정
        dragPreviewRect.sizeDelta = sourceRect.rect.size; // 원본 슬롯 크기 적용
        dragPreviewRect.localScale = Vector3.one; // 미리보기 배율 초기화
        dragPreviewRect.SetAsLastSibling(); // 미리보기 최상단 표시
        UpdateDragPreviewPosition(eventData); // 최초 마우스 위치 적용
    }

    private void UpdateDragPreviewPosition(PointerEventData eventData) // 미리보기 마우스 위치 적용
    {
        if (dragPreviewRect == null || dragCanvasRect == null) // 위치 정보 존재 확인
        {
            return; // 위치 갱신 중단
        }

        bool positionFound = RectTransformUtility.ScreenPointToLocalPointInRectangle( // 화면 좌표를 Canvas 좌표로 변환
            dragCanvasRect, // 최상위 Canvas 위치 전달
            eventData.position, // 현재 마우스 화면 위치 전달
            eventData.pressEventCamera, // 포인터 입력 카메라 전달
            out Vector2 localPoint); // 변환된 로컬 위치 수신

        if (!positionFound) // 좌표 변환 결과 확인
        {
            return; // 위치 적용 중단
        }

        dragPreviewRect.anchoredPosition = localPoint; // 미리보기 위치 적용
    }

    private void CleanupDragPreview() // 드래그 미리보기와 상태 정리
    {
        if (dragPreviewObject != null) // 미리보기 존재 확인
        {
            Destroy(dragPreviewObject); // 미리보기 제거
        }

        dragPreviewObject = null; // 미리보기 오브젝트 참조 초기화
        dragPreviewRect = null; // 미리보기 위치 참조 초기화
        dragCanvasRect = null; // Canvas 위치 참조 초기화

        if (dragOutline != null) // 테두리 참조 확인
        {
            dragOutline.enabled = restingOutlineEnabled; // 최신 평상시 테두리 상태 복구
        }

        isDragging = false; // 드래그 상태 해제
    }
}
