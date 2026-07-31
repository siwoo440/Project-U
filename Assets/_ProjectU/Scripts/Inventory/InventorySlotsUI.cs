using System.Collections; // 코루틴 기능
using System.Collections.Generic; // List 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
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
    private readonly List<Transform> generatedSectionContainers = new List<Transform>(); // 생성된 분리 영역 목록
    private Coroutine layoutRefreshCoroutine; // 레이아웃 갱신 코루틴
    private bool internalReferencesValid; // 내부 UI 참조 연결 상태
    private bool slotViewsBuilt; // 슬롯 화면 생성 상태
    private bool eventsSubscribed; // 인벤토리 이벤트 구독 상태

    public bool IsInitialized => internalReferencesValid && playerInventory != null && slotViewsBuilt; // 슬롯 UI 초기화 여부 제공

    private void Awake() // 슬롯 UI 내부 참조 초기화
    {
        if (!EnsureInternalReferences()) // 내부 UI 참조 검사
        {
            Debug.LogError($"{gameObject.name}의 인벤토리 UI 내부 참조를 연결해야 합니다.", this); // 참조 누락 오류 출력
            enabled = false; // UI 기능 비활성화
            return; // 초기화 중단
        }

        if (playerInventory != null && !slotViewsBuilt) // Scene 인벤토리 참조와 슬롯 생성 상태 확인
        {
            BuildSlotViews(); // 현재 용량 기준 슬롯 생성
            slotViewsBuilt = true; // 슬롯 생성 완료 기록
        }
    }

    private bool EnsureInternalReferences() // 내부 UI 참조 검사와 템플릿 초기화
    {
        if (internalReferencesValid) // 기존 내부 참조 검사 완료 확인
        {
            return true; // 기존 검사 결과 반환
        }

        internalReferencesValid = slotContainer != null && slotTemplate != null; // 내부 UI 참조 검사

        if (!internalReferencesValid) // 내부 참조 누락 확인
        {
            return false; // 내부 참조 검사 실패 반환
        }

        slotTemplate.gameObject.SetActive(false); // 원본 슬롯 숨김
        return true; // 내부 참조 검사 성공 반환
    }

    private void OnEnable() // 변경 이벤트 연결과 화면 갱신
    {
        if (!internalReferencesValid || playerInventory == null) // 초기화 상태 확인
        {
            return; // 이벤트 연결 중단
        }

        if (!slotViewsBuilt) // 슬롯 화면 생성 여부 확인
        {
            BuildSlotViews(); // 현재 용량 기준 슬롯 생성
            slotViewsBuilt = true; // 슬롯 생성 완료 기록
        }

        SubscribeEvents(); // 인벤토리 변경 이벤트 연결

        if (slotViews.Count != GetTargetSlotCount()) // 비활성 중 용량 변경 확인
        {
            RebuildSlotViews(); // 현재 용량 기준 화면 재생성
            return; // 중복 화면 갱신 생략
        }

        Refresh(); // 현재 상태 즉시 표시
        RequestLayoutRefresh(); // 스크롤 레이아웃 갱신 요청
    }

    private void OnDisable() // 변경 이벤트와 코루틴 해제
    {
        if (layoutRefreshCoroutine != null) // 실행 중인 코루틴 확인
        {
            StopCoroutine(layoutRefreshCoroutine); // 레이아웃 코루틴 중단
            layoutRefreshCoroutine = null; // 코루틴 참조 초기화
        }

        UnsubscribeEvents(); // 인벤토리 변경 이벤트 해제
    }

    public bool Initialize(PlayerInventory inventory) // 런타임 플레이어 인벤토리 연결
    {
        if (!EnsureInternalReferences() || inventory == null) // 내부 UI와 인벤토리 참조 확인
        {
            Debug.LogError($"{gameObject.name}의 런타임 인벤토리 참조가 누락되었습니다.", this); // 런타임 참조 오류 출력
            return false; // 초기화 실패 반환
        }

        if (playerInventory == inventory && slotViewsBuilt) // 동일 인벤토리 초기화 완료 확인
        {
            if (isActiveAndEnabled) // UI 활성 상태 확인
            {
                SubscribeEvents(); // 인벤토리 변경 이벤트 연결
                Refresh(); // 현재 슬롯 상태 갱신
                RequestLayoutRefresh(); // 레이아웃 갱신 요청
            }

            return true; // 기존 초기화 상태 반환
        }

        UnsubscribeEvents(); // 기존 인벤토리 이벤트 연결 해제
        ClearGeneratedViews(); // 기존 생성 슬롯 제거
        playerInventory = inventory; // 새로운 플레이어 인벤토리 저장
        BuildSlotViews(); // 현재 용량 기준 슬롯 생성
        slotViewsBuilt = true; // 슬롯 생성 완료 기록

        if (isActiveAndEnabled) // UI 활성 상태 확인
        {
            SubscribeEvents(); // 새로운 인벤토리 이벤트 연결
            Refresh(); // 현재 슬롯 상태 갱신
            RequestLayoutRefresh(); // 레이아웃 갱신 요청
        }

        return true; // 초기화 성공 반환
    }

    private void SubscribeEvents() // 인벤토리 변경 이벤트 연결
    {
        if (eventsSubscribed || playerInventory == null) // 기존 구독과 인벤토리 참조 확인
        {
            return; // 중복 이벤트 연결 생략
        }

        playerInventory.InventoryChanged += Refresh; // 아이템 변경 이벤트 구독
        playerInventory.HotbarSelectionChanged += Refresh; // 핫바 선택 이벤트 구독
        playerInventory.InventorySelectionChanged += Refresh; // 클릭 선택 이벤트 구독
        playerInventory.CapacityChanged += RebuildSlotViews; // 용량 변경 이벤트 구독
        eventsSubscribed = true; // 이벤트 구독 완료 기록
    }

    private void UnsubscribeEvents() // 인벤토리 변경 이벤트 해제
    {
        if (!eventsSubscribed || playerInventory == null) // 이벤트 구독과 인벤토리 참조 확인
        {
            eventsSubscribed = false; // 이벤트 구독 상태 초기화
            return; // 이벤트 해제 생략
        }

        playerInventory.InventoryChanged -= Refresh; // 아이템 변경 이벤트 해제
        playerInventory.HotbarSelectionChanged -= Refresh; // 핫바 선택 이벤트 해제
        playerInventory.InventorySelectionChanged -= Refresh; // 클릭 선택 이벤트 해제
        playerInventory.CapacityChanged -= RebuildSlotViews; // 용량 변경 이벤트 해제
        eventsSubscribed = false; // 이벤트 구독 상태 초기화
    }

    private void BuildSlotViews() // 현재 용량 기준 슬롯 구성
    {
        if (playerInventory == null) // 플레이어 인벤토리 참조 확인
        {
            return; // 슬롯 생성 중단
        }

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

    private int GetTargetSlotCount() // 현재 표시 대상 슬롯 개수 계산
    {
        if (playerInventory == null) // 플레이어 인벤토리 참조 확인
        {
            return 0; // 표시 슬롯 없음 반환
        }

        int safeStartIndex = Mathf.Clamp(startSlotIndex, 0, playerInventory.SlotCapacity); // 시작 슬롯 번호 보정
        int availableSlotCount = Mathf.Max(0, playerInventory.SlotCapacity - safeStartIndex); // 표시 가능한 슬롯 계산
        return visibleSlotCount <= 0 ? availableSlotCount : Mathf.Min(visibleSlotCount, availableSlotCount); // 실제 슬롯 개수 반환
    }

    private void RebuildSlotViews() // 인벤토리 용량 화면 재생성
    {
        if (!internalReferencesValid || playerInventory == null) // 초기화 상태 확인
        {
            return; // 인벤토리 화면 재생성 중단
        }

        ClearGeneratedViews(); // 기존 생성 슬롯 제거
        BuildSlotViews(); // 현재 용량 기준 슬롯 다시 생성
        slotViewsBuilt = true; // 슬롯 생성 완료 기록
        Refresh(); // 인벤토리 화면 갱신
        RequestLayoutRefresh(); // 변경된 Content 크기 갱신
    }

    private void ClearGeneratedViews() // 생성된 슬롯과 분리 영역 제거
    {
        for (int index = 0; index < slotViews.Count; index++) // 기존 슬롯 화면 순회
        {
            if (slotViews[index] == null) // 슬롯 화면 존재 확인
            {
                continue; // 제거 대상 제외
            }

            slotViews[index].gameObject.SetActive(false); // 기존 슬롯 즉시 숨김
            Destroy(slotViews[index].gameObject); // 기존 슬롯 제거
        }

        slotViews.Clear(); // 슬롯 화면 목록 초기화

        for (int index = 0; index < generatedSectionContainers.Count; index++) // 분리 영역 순회
        {
            if (generatedSectionContainers[index] == null) // 분리 영역 존재 확인
            {
                continue; // 제거 대상 제외
            }

            generatedSectionContainers[index].gameObject.SetActive(false); // 분리 영역 즉시 숨김
            Destroy(generatedSectionContainers[index].gameObject); // 분리 영역 제거
        }

        generatedSectionContainers.Clear(); // 분리 영역 목록 초기화

        if (slotContainer != null) // 슬롯 생성 부모 확인
        {
            GridLayoutGroup rootGrid = slotContainer.GetComponent<GridLayoutGroup>(); // 기본 격자 가져오기

            if (rootGrid != null) // 기본 격자 존재 확인
            {
                rootGrid.enabled = true; // 기본 격자 다시 활성화
            }
        }

        slotViewsBuilt = false; // 슬롯 화면 생성 상태 초기화
    }

    private void Refresh() // 전체 슬롯 화면 갱신
    {
        if (playerInventory == null) // 플레이어 인벤토리 참조 확인
        {
            return; // 슬롯 화면 갱신 중단
        }

        for (int viewIndex = 0; viewIndex < slotViews.Count; viewIndex++) // 생성 슬롯 순회
        {
            int inventoryIndex = startSlotIndex + viewIndex; // 실제 인벤토리 번호 계산

            if (inventoryIndex < 0 || inventoryIndex >= playerInventory.SlotCapacity) // 인벤토리 번호 범위 확인
            {
                continue; // 잘못된 슬롯 번호 제외
            }

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
            Debug.LogError($"{gameObject.name}의 분리 슬롯 영역에는 GridLayoutGroup이 필요합니다.", this); // 레이아웃 누락 오류 출력
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
        generatedSectionContainers.Add(hotbarContainer); // 핫바 영역 목록 등록
        generatedSectionContainers.Add(inventoryContainer); // 일반 영역 목록 등록

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

    private void RequestLayoutRefresh() // 레이아웃 갱신 예약
    {
        if (!isActiveAndEnabled) // UI 활성 상태 확인
        {
            return; // 비활성 상태 처리 중단
        }

        if (layoutRefreshCoroutine != null) // 기존 코루틴 확인
        {
            StopCoroutine(layoutRefreshCoroutine); // 기존 코루틴 중단
        }

        layoutRefreshCoroutine = StartCoroutine(RefreshLayoutNextFrame()); // 다음 프레임 갱신 시작
    }

    private IEnumerator RefreshLayoutNextFrame() // 다음 프레임 스크롤 레이아웃 갱신
    {
        yield return null; // 슬롯 생성 완료 대기

        RectTransform contentRect = slotContainer as RectTransform; // 슬롯 Content 위치 정보 조회

        if (contentRect == null) // Content 연결 상태 확인
        {
            Debug.LogError($"{gameObject.name}의 Slot Container를 찾을 수 없습니다.", this); // 실제 연결 오류 출력
            layoutRefreshCoroutine = null; // 코루틴 참조 초기화
            yield break; // 레이아웃 갱신 중단
        }

        ScrollRect parentScrollRect = contentRect.GetComponentInParent<ScrollRect>(); // Content 기준 상위 ScrollRect 조회

        if (parentScrollRect == null) // 일반 핫바 영역 확인
        {
            layoutRefreshCoroutine = null; // 코루틴 완료 처리
            yield break; // 스크롤 갱신 생략
        }

        Canvas.ForceUpdateCanvases(); // Canvas 크기 계산
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect); // Content 레이아웃 즉시 계산
        Canvas.ForceUpdateCanvases(); // ScrollRect 이동 범위 계산
        parentScrollRect.StopMovement(); // 기존 스크롤 이동 중단
        parentScrollRect.verticalNormalizedPosition = 1f; // 스크롤 위치 맨 위 설정
        layoutRefreshCoroutine = null; // 코루틴 완료 처리
    }

    private float CalculateSectionWidth(int columnCount, GridLayoutGroup sourceGrid) // 슬롯 영역 너비 계산
    {
        float cellWidth = columnCount * sourceGrid.cellSize.x; // 전체 셀 너비 계산
        float spacingWidth = Mathf.Max(0, columnCount - 1) * sourceGrid.spacing.x; // 전체 가로 간격 계산
        return sourceGrid.padding.left + sourceGrid.padding.right + cellWidth + spacingWidth; // 최종 영역 너비 반환
    }

    private void OnDestroy() // 인벤토리 슬롯 UI 이벤트 정리
    {
        UnsubscribeEvents(); // 인벤토리 변경 이벤트 해제
    }
}
