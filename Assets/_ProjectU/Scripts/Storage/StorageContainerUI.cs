using System.Collections.Generic; // 슬롯 화면 목록 기능
using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.InputSystem; // 키보드 입력 기능
using UnityEngine.UI; // Unity UI 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class StorageContainerUI : MonoBehaviour // 보관함 화면 관리자
{
    [Header("References")] // UI 참조 묶음
    [Tooltip("보관함 전체 패널.")]
    [SerializeField] private GameObject panelRoot; // 보관함 전체 패널
    [Tooltip("보관함 제목 Text.")]
    [SerializeField] private TMP_Text titleText; // 보관함 제목 Text
    [Tooltip("보관함 닫기 버튼.")]
    [SerializeField] private Button closeButton; // 보관함 닫기 버튼
    [Tooltip("보관함 슬롯 생성 부모.")]
    [SerializeField] private Transform storageSlotContainer; // 보관함 슬롯 생성 부모
    [Tooltip("보관함 슬롯 격자.")]
    [SerializeField] private GridLayoutGroup storageGridLayout; // 보관함 슬롯 격자
    [Tooltip("보관함 슬롯 원본.")]
    [SerializeField] private StorageSlotView storageSlotTemplate; // 보관함 슬롯 원본

    [Header("Close Settings")] // 보관함 종료 설정 묶음
    [Tooltip("F키 재입력 닫기 여부.")]
    [SerializeField] private bool closeWithInteractKey = true; // F키 재입력 닫기 여부
    [Tooltip("최대 거리 초과 자동 닫기 여부.")]
    [SerializeField] private bool closeWhenTooFar = true; // 최대 거리 초과 자동 닫기 여부
    [Tooltip("플레이어와 보관함의 최대 유지 거리.")]
    [SerializeField] private float maximumOpenDistance = 3f; // 플레이어와 보관함의 최대 유지 거리

    [Header("Debug")] // 임시 테스트 묶음
    [Tooltip("테스트 대상 보관함.")]
    [SerializeField] private StorageContainer debugStorageContainer; // 테스트 대상 보관함

    private readonly List<StorageSlotView> slotViews = new List<StorageSlotView>(); // 생성된 슬롯 화면 목록
    private InventorySlotsUI[] playerInventoryViews = new InventorySlotsUI[0]; // 팝업 내부 플레이어 인벤토리 화면 목록
    private GameUIManager gameUIManager; // 공통 게임 UI 관리자
    private StorageContainer currentStorage; // 현재 열린 보관함
    private Transform playerTransform; // 플레이어 거리 검사 대상
    private bool referencesValid; // UI 내부 참조 연결 상태
    private bool runtimeInitialized; // 런타임 외부 참조 초기화 상태
    private int openedFrame = -1; // 보관함이 열린 프레임

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf; // 보관함 화면 표시 여부
    public bool IsRuntimeInitialized => runtimeInitialized; // 런타임 초기화 여부 제공
    public StorageContainer CurrentStorage => currentStorage; // 현재 보관함 제공

    private void Awake() // 보관함 UI 내부 참조 초기화
    {
        referencesValid = panelRoot != null // 전체 패널 참조 확인
            && titleText != null // 제목 Text 참조 확인
            && closeButton != null // 닫기 버튼 참조 확인
            && storageSlotContainer != null // 슬롯 부모 참조 확인
            && storageGridLayout != null // 격자 참조 확인
            && storageSlotTemplate != null; // 슬롯 원본 참조 확인

        if (!referencesValid) // 참조 누락 확인
        {
            Debug.LogError($"{gameObject.name}의 보관함 UI 내부 참조가 누락되었습니다.", this); // 연결 오류 출력
            enabled = false; // 보관함 UI 기능 비활성화
            return; // 초기화 중단
        }

        playerInventoryViews = GetComponentsInChildren<InventorySlotsUI>(true); // 팝업 내부 인벤토리 화면 검색
        storageSlotTemplate.gameObject.SetActive(false); // 슬롯 원본 숨김
        closeButton.onClick.AddListener(Close); // 닫기 버튼 기능 연결
        panelRoot.SetActive(false); // 초기 보관함 화면 숨김
    }

    private void Start() // 공통 관리자 자동 검색
    {
        ResolveManager(); // 게임 UI 관리자 검색
    }

    private void Update() // 보관함 종료 조건 처리
    {
        if (!IsOpen) // 보관함 화면 상태 확인
        {
            return; // 입력과 거리 검사 중단
        }

        if (ShouldCloseAutomatically()) // 자동 종료 조건 확인
        {
            Close(); // 보관함 화면 종료
            return; // 같은 프레임 추가 처리 중단
        }

        Keyboard keyboard = Keyboard.current; // 현재 키보드 조회

        if (keyboard == null) // 키보드 존재 확인
        {
            return; // 키 입력 검사 중단
        }

        if (keyboard.escapeKey.wasPressedThisFrame) // ESC 입력 확인
        {
            Close(); // 보관함 화면 종료
            return; // 같은 프레임 추가 처리 중단
        }

        bool hasInteractCloseInput =
            closeWithInteractKey
            && Time.frameCount != openedFrame
            && keyboard.fKey.wasPressedThisFrame; // 열기 프레임을 제외한 F키 닫기 조건 계산

        if (hasInteractCloseInput) // F키 닫기 입력 확인
        {
            Close(); // 보관함 화면 종료
        }
    }

    public bool Initialize(GameUIManager manager, PlayerInventory playerInventory) // 런타임 외부 참조 초기화
    {
        if (!referencesValid || manager == null || playerInventory == null) // 내부와 외부 참조 확인
        {
            Debug.LogError($"{gameObject.name}의 보관함 UI 런타임 참조가 누락되었습니다.", this); // 런타임 참조 오류 출력
            runtimeInitialized = false; // 런타임 초기화 실패 기록
            return false; // 초기화 실패 반환
        }

        gameUIManager = manager; // 공통 게임 UI 관리자 저장
        playerTransform = playerInventory.transform; // 플레이어 Transform 저장

        for (int index = 0; index < playerInventoryViews.Length; index++) // 팝업 내부 인벤토리 화면 순회
        {
            InventorySlotsUI inventoryView = playerInventoryViews[index]; // 현재 인벤토리 화면 조회

            if (inventoryView == null) // 인벤토리 화면 존재 확인
            {
                continue; // 빈 화면 참조 제외
            }

            if (!inventoryView.Initialize(playerInventory)) // 플레이어 인벤토리 전달 시도
            {
                Debug.LogError($"{inventoryView.gameObject.name}의 플레이어 인벤토리 초기화에 실패했습니다.", inventoryView); // 초기화 오류 출력
                runtimeInitialized = false; // 런타임 초기화 실패 기록
                return false; // 초기화 실패 반환
            }
        }

        runtimeInitialized = true; // 런타임 초기화 완료 기록
        return true; // 초기화 성공 반환
    }

    public void Open(StorageContainer storageContainer) // 외부 요청으로 보관함 화면 열기
    {
        ResolveManager(); // 게임 UI 관리자 참조 확인

        if (gameUIManager == null) // 게임 UI 관리자 확인
        {
            Debug.LogError("보관함 UI를 열 GameUIManager를 찾을 수 없습니다.", this); // 관리자 누락 오류 출력
            return; // 화면 열기 중단
        }

        gameUIManager.OpenStorage(storageContainer); // 공통 관리자에서 보관함 팝업 열기
    }

    public void Close() // 외부 요청으로 보관함 화면 강제 종료
    {
        ResolveManager(); // 게임 UI 관리자 참조 확인

        if (gameUIManager != null) // 게임 UI 관리자 확인
        {
            gameUIManager.CloseStorage(); // 관리자에서 보관함 화면과 입력 잠금 종료
        }

        if (IsOpen) // 관리자 상태 불일치로 화면이 남았는지 확인
        {
            HideFromManager(); // 보관함 패널 직접 숨김
        }
    }

    public bool ShowFromManager(StorageContainer storageContainer) // 공통 관리자에서 지정 보관함 표시
    {
        if (!referencesValid || !runtimeInitialized || storageContainer == null) // UI와 런타임 초기화 상태 확인
        {
            return false; // 화면 열기 실패 반환
        }

        if (!storageContainer.TryValidateSetup(out string errorMessage)) // 보관함 설정 검사
        {
            Debug.LogError($"보관함 화면 열기 실패\n{errorMessage}", storageContainer); // 설정 오류 출력
            return false; // 화면 열기 실패 반환
        }

        DetachCurrentStorage(); // 기존 보관함 이벤트 연결 해제
        currentStorage = storageContainer; // 새로운 보관함 저장
        currentStorage.StorageChanged += Refresh; // 보관함 변경 이벤트 연결
        openedFrame = Time.frameCount; // 보관함 열기 프레임 저장
        panelRoot.SetActive(true); // 보관함 화면 표시
        titleText.SetText(currentStorage.DisplayName); // 보관함 이름 표시
        RebuildSlotViews(); // 보관함 용량에 맞는 슬롯 생성
        Refresh(); // 현재 슬롯 내용 표시
        return true; // 화면 열기 성공 반환
    }

    public void HideFromManager() // 공통 관리자에서 보관함 숨김
    {
        DetachCurrentStorage(); // 현재 보관함 이벤트 연결 해제
        openedFrame = -1; // 보관함 열기 프레임 초기화

        if (panelRoot != null) // 전체 패널 존재 확인
        {
            panelRoot.SetActive(false); // 보관함 화면 숨김
        }
    }

    private bool ShouldCloseAutomatically() // 보관함 자동 종료 조건 확인
    {
        if (currentStorage == null) // 현재 보관함 존재 확인
        {
            return true; // 연결 대상 없음으로 종료
        }

        if (!currentStorage.isActiveAndEnabled || !currentStorage.gameObject.activeInHierarchy) // 보관함 활성 상태 확인
        {
            return true; // 보관함 제거 또는 비활성화로 종료
        }

        if (!closeWhenTooFar || playerTransform == null) // 거리 자동 종료 사용 여부 확인
        {
            return false; // 거리 검사 생략
        }

        float safeMaximumDistance = Mathf.Max(0.5f, maximumOpenDistance); // 최소 유지 거리 보정
        float squaredMaximumDistance = safeMaximumDistance * safeMaximumDistance; // 최대 거리 제곱 계산
        float squaredCurrentDistance =
            (playerTransform.position - currentStorage.transform.position).sqrMagnitude; // 플레이어와 보관함 거리 제곱 계산

        return squaredCurrentDistance > squaredMaximumDistance; // 최대 거리 초과 여부 반환
    }

    private void DetachCurrentStorage() // 현재 보관함 이벤트 연결 해제
    {
        if (currentStorage != null) // 현재 보관함 확인
        {
            currentStorage.StorageChanged -= Refresh; // 보관함 변경 이벤트 해제
        }

        currentStorage = null; // 현재 보관함 참조 제거
    }

    private void RebuildSlotViews() // 보관함 슬롯 화면 재생성
    {
        if (currentStorage == null) // 현재 보관함 확인
        {
            return; // 슬롯 재생성 중단
        }

        for (int index = 0; index < slotViews.Count; index++) // 기존 슬롯 화면 순회
        {
            if (slotViews[index] == null) // 슬롯 화면 존재 확인
            {
                continue; // 빈 참조 제외
            }

            slotViews[index].gameObject.SetActive(false); // 기존 슬롯 즉시 숨김
            Destroy(slotViews[index].gameObject); // 기존 슬롯 화면 제거
        }

        slotViews.Clear(); // 기존 슬롯 목록 초기화
        storageGridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount; // 고정 열 방식 적용
        storageGridLayout.constraintCount = currentStorage.ColumnCount; // 보관함별 열 개수 적용

        for (int index = 0; index < currentStorage.SlotCapacity; index++) // 필요한 슬롯 수만큼 반복
        {
            StorageSlotView newSlotView = Instantiate(storageSlotTemplate, storageSlotContainer); // 슬롯 화면 복제
            newSlotView.Configure(currentStorage, index); // 보관함 컨테이너와 실제 슬롯 번호 연결
            newSlotView.gameObject.SetActive(true); // 복제 슬롯 표시
            slotViews.Add(newSlotView); // 생성 슬롯 목록 등록
        }

        RectTransform containerRect = storageSlotContainer as RectTransform; // 슬롯 부모 위치 정보 조회

        if (containerRect != null) // 위치 정보 존재 확인
        {
            Canvas.ForceUpdateCanvases(); // Canvas 크기 계산
            LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect); // 슬롯 레이아웃 즉시 갱신
        }
    }

    private void Refresh() // 보관함 슬롯 화면 갱신
    {
        if (currentStorage == null) // 현재 보관함 확인
        {
            return; // 화면 갱신 중단
        }

        for (int index = 0; index < slotViews.Count; index++) // 전체 슬롯 화면 순회
        {
            InventorySlot slot = currentStorage.GetSlot(index); // 현재 보관함 슬롯 조회
            slotViews[index].SetSlot(slot, index + 1); // 슬롯 번호와 내용 표시
        }
    }

    private void ResolveManager() // 공통 게임 UI 관리자 자동 검색
    {
        if (gameUIManager == null) // 게임 UI 관리자 참조 확인
        {
            gameUIManager = FindFirstObjectByType<GameUIManager>(); // Scene 게임 UI 관리자 검색
        }
    }

    [ContextMenu("Debug Open Storage")] // Inspector 테스트 열기 메뉴
    private void DebugOpenStorage() // 지정 보관함 테스트 열기
    {
        if (debugStorageContainer == null) // 테스트 보관함 확인
        {
            Debug.LogError("Debug Storage Container 참조가 누락되었습니다.", this); // 참조 오류 출력
            return; // 테스트 중단
        }

        Open(debugStorageContainer); // 테스트 보관함 화면 열기
    }

    [ContextMenu("Debug Close Storage")] // Inspector 테스트 닫기 메뉴
    private void DebugCloseStorage() // 보관함 테스트 닫기
    {
        Close(); // 현재 보관함 화면 닫기
    }

    private void OnValidate() // Inspector 설정값 보정
    {
        maximumOpenDistance = Mathf.Max(0.5f, maximumOpenDistance); // 자동 종료 거리 최소값 적용
    }

    private void OnDestroy() // 보관함 UI 이벤트 정리
    {
        DetachCurrentStorage(); // 현재 보관함 이벤트 연결 해제

        if (closeButton != null) // 닫기 버튼 확인
        {
            closeButton.onClick.RemoveListener(Close); // 닫기 버튼 이벤트 해제
        }
    }
}
