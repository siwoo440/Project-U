using System.Collections.Generic; // List 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

public sealed class InventorySlotsUI : MonoBehaviour // 여러 인벤토리 슬롯 표시
{
    [SerializeField] private PlayerInventory playerInventory; // 표시할 플레이어 인벤토리
    [SerializeField] private Transform slotContainer; // 슬롯 생성 부모
    [SerializeField] private InventorySlotView slotTemplate; // 복제할 슬롯 원본
    [SerializeField] private int visibleSlotCount = 8; // 표시할 슬롯 개수
    [SerializeField] private int startSlotIndex; // 첫 번째 표시 슬롯 번호
    [SerializeField] private bool showShortcutNumbers = true; // 숫자키 표시 여부
    [SerializeField] private bool showSelection = true; // 활성 핫바 테두리 표시 여부
    [SerializeField] private bool showClickedSelection; // 클릭 슬롯 테두리 표시 여부
    [SerializeField] private bool allowItemDrag; // 아이템 드래그 허용 여부
    [SerializeField] private bool requireAltKeyForDrag; // Alt 드래그 요구 여부
    [SerializeField] private bool separateHotbarArea; // 핫바 영역 분리 여부
    [SerializeField] private float separatedAreaSpacing = 16f; // 분리 영역 사이 간격
    [SerializeField] private Color hotbarAreaColor = new Color(0.22f, 0.18f, 0.04f, 0.65f); // 핫바 영역 배경색
    [SerializeField] private Color inventoryAreaColor = new Color(0.04f, 0.04f, 0.04f, 0.65f); // 일반 영역 배경색

    private readonly List<InventorySlotView> slotViews = new List<InventorySlotView>(); // 생성된 슬롯 목록

    private void Awake() // 슬롯 화면 생성
    {
        if (playerInventory == null || slotContainer == null || slotTemplate == null) // 필수 참조 확인
        {
            Debug.LogError($"{gameObject.name}의 인벤토리 UI 참조를 모두 연결해야 합니다.", this); // 참조 누락 오류
            enabled = false; // UI 기능 비활성화
            return; // 초기화 중단
        }

        slotTemplate.gameObject.SetActive(false); // 원본 슬롯 숨김

        int safeStartIndex = Mathf.Clamp(startSlotIndex, 0, playerInventory.SlotCapacity); // 시작 슬롯 번호 보정
        int availableSlotCount = Mathf.Max(0, playerInventory.SlotCapacity - safeStartIndex); // 표시 가능한 슬롯 계산
        int targetSlotCount = visibleSlotCount <= 0 ? availableSlotCount : Mathf.Min(visibleSlotCount, availableSlotCount); // 실제 생성 개수 계산

        startSlotIndex = safeStartIndex; // 보정된 시작 번호 저장

        if (separateHotbarArea && startSlotIndex == 0 && targetSlotCount > playerInventory.HotbarSlotCount) // 영역 분리 조건 확인
        {
            CreateSeparatedSlotAreas(targetSlotCount); // 핫바와 일반 영역 분리 생성
            return; // 일반 슬롯 생성 생략
        }

        CreateSlotViews(slotContainer, startSlotIndex, targetSlotCount); // 연속 슬롯 화면 생성
    }

    private void OnEnable() // 변경 이벤트 연결
    {
        if (playerInventory == null) // 인벤토리 연결 확인
        {
            return; // 이벤트 연결 중단
        }

        playerInventory.InventoryChanged += Refresh; // 아이템 변경 이벤트 구독
        playerInventory.HotbarSelectionChanged += Refresh; // 핫바 선택 이벤트 구독
        playerInventory.InventorySelectionChanged += Refresh; // 클릭 선택 이벤트 구독
        Refresh(); // 현재 상태 즉시 표시
    }

    private void OnDisable() // 변경 이벤트 해제
    {
        if (playerInventory == null) // 인벤토리 연결 확인
        {
            return; // 이벤트 해제 중단
        }

        playerInventory.InventoryChanged -= Refresh; // 아이템 변경 이벤트 해제
        playerInventory.HotbarSelectionChanged -= Refresh; // 핫바 선택 이벤트 해제
        playerInventory.InventorySelectionChanged -= Refresh; // 클릭 선택 이벤트 해제
    }

    private void Refresh() // 전체 슬롯 화면 갱신
    {
        for (int viewIndex = 0; viewIndex < slotViews.Count; viewIndex++) // 생성 슬롯 순회
        {
            int inventoryIndex = startSlotIndex + viewIndex; // 실제 인벤토리 번호 계산
            InventorySlot slot = playerInventory.GetSlot(inventoryIndex); // 해당 슬롯 조회
            bool isHotbarSelected = showSelection && inventoryIndex == playerInventory.SelectedHotbarIndex; // 활성 핫바 상태 계산
            bool isClickedSelected = showClickedSelection && inventoryIndex == playerInventory.SelectedInventoryIndex; // 클릭 선택 상태 계산
            bool isSelected = isHotbarSelected || isClickedSelected; // 최종 선택 상태 계산
            bool showShortcut = showShortcutNumbers && inventoryIndex < playerInventory.HotbarSlotCount; // 숫자 표시 상태 계산
            slotViews[viewIndex].SetSlot(slot, inventoryIndex + 1, showShortcut, isSelected); // 슬롯 화면 적용
        }
    }

    private void CreateSeparatedSlotAreas(int targetSlotCount) // 분리된 슬롯 영역 생성
    {
        GridLayoutGroup sourceGrid = slotContainer.GetComponent<GridLayoutGroup>(); // 기존 격자 설정 가져오기
        RectTransform rootRect = slotContainer as RectTransform; // 전체 영역 위치 정보

        if (sourceGrid == null || rootRect == null) // 필수 레이아웃 확인
        {
            Debug.LogError($"{gameObject.name}의 분리 슬롯 영역에는 GridLayoutGroup이 필요합니다.", this); // 레이아웃 누락 오류
            CreateSlotViews(slotContainer, startSlotIndex, targetSlotCount); // 기존 방식 대체 생성
            return; // 분리 생성 중단
        }

        sourceGrid.enabled = false; // 기존 단일 격자 비활성화

        int hotbarViewCount = Mathf.Min(playerInventory.HotbarSlotCount, targetSlotCount); // 핫바 표시 개수 계산
        int inventoryViewCount = targetSlotCount - hotbarViewCount; // 일반 슬롯 표시 개수 계산
        int columnCount = Mathf.Max(1, sourceGrid.constraintCount); // 한 줄 슬롯 개수 계산
        float hotbarHeight = CalculateSectionHeight(hotbarViewCount, columnCount, sourceGrid); // 핫바 영역 높이 계산
        float inventoryHeight = CalculateSectionHeight(inventoryViewCount, columnCount, sourceGrid); // 일반 영역 높이 계산
        float totalHeight = hotbarHeight + separatedAreaSpacing + inventoryHeight; // 전체 분리 영역 높이 계산
        float sectionWidth = CalculateSectionWidth(columnCount, sourceGrid); // 각 영역 너비 계산

        rootRect.sizeDelta = new Vector2(sectionWidth, totalHeight); // 전체 영역 크기 적용

        Transform hotbarContainer = CreateSectionContainer("InventoryHotbarArea", rootRect, sourceGrid, sectionWidth, hotbarHeight, totalHeight, true); // 핫바 영역 생성
        Transform inventoryContainer = CreateSectionContainer("InventoryStorageArea", rootRect, sourceGrid, sectionWidth, inventoryHeight, totalHeight, false); // 일반 영역 생성

        CreateSlotViews(hotbarContainer, 0, hotbarViewCount); // 핫바 슬롯 생성
        CreateSlotViews(inventoryContainer, hotbarViewCount, inventoryViewCount); // 일반 슬롯 생성
    }

    private Transform CreateSectionContainer(string sectionName, RectTransform rootRect, GridLayoutGroup sourceGrid, float sectionWidth, float sectionHeight, float totalHeight, bool isHotbarArea) // 슬롯 영역 오브젝트 생성
    {
        GameObject sectionObject = new GameObject(sectionName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(GridLayoutGroup)); // 영역 오브젝트 생성
        RectTransform sectionRect = sectionObject.GetComponent<RectTransform>(); // 영역 위치 정보 가져오기
        Image sectionImage = sectionObject.GetComponent<Image>(); // 영역 배경 이미지 가져오기
        GridLayoutGroup sectionGrid = sectionObject.GetComponent<GridLayoutGroup>(); // 영역 격자 가져오기

        sectionRect.SetParent(rootRect, false); // 전체 영역 아래 배치
        sectionRect.anchorMin = new Vector2(0.5f, 0.5f); // 중앙 최소 앵커 설정
        sectionRect.anchorMax = new Vector2(0.5f, 0.5f); // 중앙 최대 앵커 설정
        sectionRect.pivot = new Vector2(0.5f, 0.5f); // 중앙 기준점 설정
        sectionRect.sizeDelta = new Vector2(sectionWidth, sectionHeight); // 영역 크기 설정

        float sectionY = isHotbarArea ? (totalHeight - sectionHeight) * 0.5f : -(totalHeight - sectionHeight) * 0.5f; // 영역 세로 위치 계산
        sectionRect.anchoredPosition = new Vector2(0f, sectionY); // 영역 세로 위치 적용

        sectionImage.color = isHotbarArea ? hotbarAreaColor : inventoryAreaColor; // 영역별 배경색 적용
        sectionImage.raycastTarget = false; // 배경 입력 차단 해제

        sectionGrid.padding = new RectOffset(sourceGrid.padding.left, sourceGrid.padding.right, sourceGrid.padding.top, sourceGrid.padding.bottom); // 기존 여백 복사
        sectionGrid.cellSize = sourceGrid.cellSize; // 기존 슬롯 크기 복사
        sectionGrid.spacing = sourceGrid.spacing; // 기존 슬롯 간격 복사
        sectionGrid.startCorner = sourceGrid.startCorner; // 기존 시작 모서리 복사
        sectionGrid.startAxis = sourceGrid.startAxis; // 기존 시작 축 복사
        sectionGrid.childAlignment = sourceGrid.childAlignment; // 기존 정렬 방식 복사
        sectionGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; // 고정 열 개수 설정
        sectionGrid.constraintCount = Mathf.Max(1, sourceGrid.constraintCount); // 기존 열 개수 복사

        return sectionRect; // 생성 영역 반환
    }

    private void CreateSlotViews(Transform targetContainer, int firstInventoryIndex, int slotCount) // 지정 영역 슬롯 생성
    {
        for (int localIndex = 0; localIndex < slotCount; localIndex++) // 필요한 슬롯 수만큼 반복
        {
            int inventoryIndex = firstInventoryIndex + localIndex; // 실제 인벤토리 번호 계산
            InventorySlotView newSlotView = Instantiate(slotTemplate, targetContainer); // 슬롯 원본 복제
            newSlotView.Configure(playerInventory, inventoryIndex, allowItemDrag, requireAltKeyForDrag); // 슬롯 데이터와 드래그 조건 연결
            newSlotView.gameObject.SetActive(true); // 복제 슬롯 표시
            slotViews.Add(newSlotView); // 생성 목록 등록
        }
    }

    private float CalculateSectionHeight(int slotCount, int columnCount, GridLayoutGroup sourceGrid) // 슬롯 영역 높이 계산
    {
        int rowCount = Mathf.Max(1, Mathf.CeilToInt(slotCount / (float)columnCount)); // 필요한 행 개수 계산
        float cellHeight = rowCount * sourceGrid.cellSize.y; // 전체 셀 높이 계산
        float spacingHeight = Mathf.Max(0, rowCount - 1) * sourceGrid.spacing.y; // 전체 세로 간격 계산
        return sourceGrid.padding.top + sourceGrid.padding.bottom + cellHeight + spacingHeight; // 최종 영역 높이 반환
    }

    private float CalculateSectionWidth(int columnCount, GridLayoutGroup sourceGrid) // 슬롯 영역 너비 계산
    {
        float cellWidth = columnCount * sourceGrid.cellSize.x; // 전체 셀 너비 계산
        float spacingWidth = Mathf.Max(0, columnCount - 1) * sourceGrid.spacing.x; // 전체 가로 간격 계산
        return sourceGrid.padding.left + sourceGrid.padding.right + cellWidth + spacingWidth; // 최종 영역 너비 반환
    }
}