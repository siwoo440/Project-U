using System.Collections; // 코루틴 기능
using System.Collections.Generic; // 목록 기능
using System.Text; // 문자열 조합 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.EventSystems; // EventSystem 검사 기능

[DefaultExecutionOrder(1000)] // 다른 UI 컴포넌트 초기화 이후 검사
[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class GameplayUIRuntimeValidator : MonoBehaviour // Gameplay UI 런타임 검증 관리자
{
    [Header("Scene References")] // Scene 참조 묶음
    [Tooltip("공통 게임 UI 관리자.")]
    [SerializeField] private GameUIManager gameUIManager; // 공통 게임 UI 관리자
    [Tooltip("게임 입력 잠금 관리자.")]
    [SerializeField] private GameplayInputLock gameplayInputLock; // 게임 입력 잠금 관리자
    [Tooltip("플레이어 인벤토리.")]
    [SerializeField] private PlayerInventory playerInventory; // 플레이어 인벤토리
    [Tooltip("화면 하단 고정 Hotbar 슬롯 UI.")]
    [SerializeField] private InventorySlotsUI fixedHotbarSlotsUI; // 화면 하단 고정 Hotbar 슬롯 UI
    [Tooltip("건축 모드 관리자.")]
    [SerializeField] private BuildPlacementController buildPlacementController; // 건축 모드 관리자
    [Tooltip("런타임 팝업 생성 부모.")]
    [SerializeField] private Transform popupLayer; // 런타임 팝업 생성 부모

    [Header("Popup Prefabs")] // 팝업 프리팹 참조 묶음
    [Tooltip("일반 인벤토리 팝업 프리팹.")]
    [SerializeField] private InventoryPopupController inventoryPopupPrefab; // 일반 인벤토리 팝업 프리팹
    [Tooltip("보관함 팝업 프리팹.")]
    [SerializeField] private StorageContainerUI storagePopupPrefab; // 보관함 팝업 프리팹

    [Header("Validation")] // 검증 설정 묶음
    [Tooltip("게임 시작 자동 검증 여부.")]
    [SerializeField] private bool validateOnStart = true; // 게임 시작 자동 검증 여부
    [Tooltip("실행 중 팝업 상태 감시 여부.")]
    [SerializeField] private bool monitorRuntimeState = true; // 실행 중 팝업 상태 감시 여부
    [Tooltip("팝업 상태 불일치 자동 복구 여부.")]
    [SerializeField] private bool repairPopupStateMismatch = true; // 팝업 상태 불일치 자동 복구 여부
    [Tooltip("런타임 검사 간격.")]
    [SerializeField] private float monitorInterval = 0.5f; // 런타임 검사 간격
    [Tooltip("정상 검증 요약 출력 여부.")]
    [SerializeField] private bool logSuccessSummary = true; // 정상 검증 요약 출력 여부

    private float nextMonitorTime; // 다음 런타임 검사 시간
    private string lastRuntimeIssueKey = string.Empty; // 마지막 런타임 문제 식별값

    public bool LastValidationSucceeded { get; private set; } // 마지막 전체 검증 성공 여부 제공

    private IEnumerator Start() // 다른 컴포넌트 초기화 이후 첫 검증
    {
        yield return null; // Awake와 첫 활성화 처리 완료 대기

        ResolveSceneReferences(); // Scene 참조 자동 검색

        if (validateOnStart) // 시작 검증 사용 여부 확인
        {
            ValidateGameplayUI(); // 전체 UI 구성 검증
        }

        nextMonitorTime = Time.unscaledTime + monitorInterval; // 첫 런타임 검사 시간 설정
    }

    private void Update() // 실행 중 팝업 상태 감시
    {
        if (!monitorRuntimeState) // 런타임 감시 사용 여부 확인
        {
            return; // 상태 감시 중단
        }

        if (Time.unscaledTime < nextMonitorTime) // 검사 간격 확인
        {
            return; // 다음 검사 시간까지 대기
        }

        nextMonitorTime = Time.unscaledTime + monitorInterval; // 다음 검사 시간 갱신
        MonitorRuntimeState(); // 현재 팝업과 Hotbar 상태 검사
    }

    [ContextMenu("Resolve Scene References")] // Inspector Scene 참조 자동 검색 메뉴
    public void ResolveSceneReferences() // Scene 참조 자동 검색
    {
        if (gameUIManager == null) // 게임 UI 관리자 참조 확인
        {
            gameUIManager = FindFirstObjectByType<GameUIManager>(FindObjectsInactive.Include); // Scene 관리자 검색
        }

        if (gameplayInputLock == null) // 입력 잠금 관리자 참조 확인
        {
            gameplayInputLock = FindFirstObjectByType<GameplayInputLock>(FindObjectsInactive.Include); // Scene 입력 잠금 검색
        }

        if (playerInventory == null) // 플레이어 인벤토리 참조 확인
        {
            playerInventory = FindFirstObjectByType<PlayerInventory>(FindObjectsInactive.Include); // Scene 인벤토리 검색
        }

        if (buildPlacementController == null) // 건축 관리자 참조 확인
        {
            buildPlacementController = FindFirstObjectByType<BuildPlacementController>(FindObjectsInactive.Include); // Scene 건축 관리자 검색
        }

        if (popupLayer == null) // 팝업 부모 참조 확인
        {
            popupLayer = FindSceneTransformByName("PopupLayer"); // PopupLayer 이름으로 검색
        }

        if (fixedHotbarSlotsUI == null) // 고정 Hotbar 참조 확인
        {
            fixedHotbarSlotsUI = FindFixedHotbarSlotsUI(); // Scene Hotbar 슬롯 UI 검색
        }
    }

    [ContextMenu("Validate Gameplay UI")] // Inspector 전체 검증 실행 메뉴
    public void ValidateGameplayUI() // Gameplay UI 전체 구성 검증
    {
        ResolveSceneReferences(); // 누락된 Scene 참조 자동 검색

        List<string> errors = new List<string>(); // 오류 문구 목록
        List<string> warnings = new List<string>(); // 경고 문구 목록

        ValidateUniqueSceneManagers(errors); // Scene 관리자 중복 검사
        ValidateSceneReferences(errors); // 필수 Scene 참조 검사
        ValidateFixedHotbar(errors, warnings); // 화면 하단 Hotbar 검사
        ValidatePopupPrefabs(errors, warnings); // 팝업 프리팹 구성 검사
        ValidateInitialRuntimeState(warnings); // 시작 팝업 상태 검사

        LastValidationSucceeded = errors.Count == 0; // 전체 검증 성공 여부 저장
        PrintValidationResult(errors, warnings); // 검증 결과 Console 출력
    }

    [ContextMenu("Repair Current Popup State")] // Inspector 현재 팝업 상태 복구 메뉴
    public void RepairCurrentPopupState() // 현재 관리자와 실제 팝업 상태 정리
    {
        ResolveSceneReferences(); // 게임 UI 관리자 참조 확인

        if (gameUIManager == null) // 게임 UI 관리자 존재 확인
        {
            Debug.LogError("팝업 상태를 복구할 GameUIManager가 없습니다.", this); // 복구 실패 출력
            return; // 복구 처리 중단
        }

        RepairPopupState(); // 실제 팝업 상태와 관리자 상태 정리
        Debug.Log("Gameplay UI 팝업 상태 복구를 실행했습니다.", this); // 복구 실행 결과 출력
    }

    private void ValidateUniqueSceneManagers(List<string> errors) // Scene 핵심 관리자 중복 검사
    {
        GameUIManager[] gameUIManagers = FindObjectsByType<GameUIManager>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None); // 전체 GameUIManager 검색

        if (gameUIManagers.Length != 1) // 관리자 개수 확인
        {
            errors.Add($"GameUIManager가 Scene에 {gameUIManagers.Length}개 있습니다. 정확히 1개만 유지해야 합니다."); // 중복 오류 추가
        }

        GameplayInputLock[] inputLocks = FindObjectsByType<GameplayInputLock>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None); // 전체 입력 잠금 관리자 검색

        if (inputLocks.Length != 1) // 입력 잠금 관리자 개수 확인
        {
            errors.Add($"GameplayInputLock이 Scene에 {inputLocks.Length}개 있습니다. 정확히 1개만 유지해야 합니다."); // 중복 오류 추가
        }

        EventSystem[] eventSystems = FindObjectsByType<EventSystem>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None); // 전체 EventSystem 검색

        if (eventSystems.Length != 1) // EventSystem 개수 확인
        {
            errors.Add($"EventSystem이 Scene에 {eventSystems.Length}개 있습니다. UI 입력을 위해 정확히 1개만 유지해야 합니다."); // EventSystem 오류 추가
        }
    }

    private void ValidateSceneReferences(List<string> errors) // 필수 Scene 참조 검사
    {
        if (gameUIManager == null) // GameUIManager 연결 확인
        {
            errors.Add("GameplayUIRuntimeValidator의 Game UI Manager가 연결되지 않았습니다."); // 관리자 참조 오류 추가
        }
        else if (!gameUIManager.enabled) // GameUIManager 활성 상태 확인
        {
            errors.Add("GameUIManager가 비활성화되어 I 키와 팝업 입력을 처리할 수 없습니다."); // 관리자 비활성 오류 추가
        }

        if (gameplayInputLock == null) // 입력 잠금 관리자 연결 확인
        {
            errors.Add("GameplayUIRuntimeValidator의 Gameplay Input Lock이 연결되지 않았습니다."); // 입력 잠금 참조 오류 추가
        }
        else if (!gameplayInputLock.enabled) // 입력 잠금 관리자 활성 상태 확인
        {
            errors.Add("GameplayInputLock이 비활성화되어 커서와 플레이어 입력을 제어할 수 없습니다."); // 입력 잠금 비활성 오류 추가
        }

        if (playerInventory == null) // 플레이어 인벤토리 연결 확인
        {
            errors.Add("PlayerInventory를 찾을 수 없습니다."); // 플레이어 인벤토리 오류 추가
        }

        if (buildPlacementController == null) // 건축 관리자 연결 확인
        {
            errors.Add("BuildPlacementController를 찾을 수 없어 B 키 건축 상태를 검증할 수 없습니다."); // 건축 관리자 오류 추가
        }
        else if (!buildPlacementController.enabled
    && (gameplayInputLock == null || !gameplayInputLock.IsLocked)) // 의도적인 입력 잠금 제외
        {
            errors.Add("BuildPlacementController가 입력 잠금 없이 비활성화되어 있습니다."); // 실제 비활성 오류 추가
        }

        if (popupLayer == null) // PopupLayer 연결 확인
        {
            errors.Add("Canvas 아래 PopupLayer를 찾을 수 없습니다."); // PopupLayer 오류 추가
        }
        else if (!popupLayer.gameObject.activeInHierarchy) // PopupLayer 활성 상태 확인
        {
            errors.Add("PopupLayer가 비활성화되어 런타임 팝업을 표시할 수 없습니다."); // PopupLayer 비활성 오류 추가
        }
    }

    private void ValidateFixedHotbar(
        List<string> errors,
        List<string> warnings) // 화면 하단 고정 Hotbar 검사
    {
        if (fixedHotbarSlotsUI == null) // 고정 Hotbar 참조 확인
        {
            errors.Add("화면 하단 고정 Hotbar의 InventorySlotsUI를 찾을 수 없습니다."); // Hotbar 참조 오류 추가
            return; // 추가 검사 중단
        }

        if (!fixedHotbarSlotsUI.gameObject.scene.IsValid()) // Scene 오브젝트 여부 확인
        {
            errors.Add("Fixed Hotbar Slots UI에는 프리팹 에셋이 아니라 Gameplay Scene의 HotbarPanel을 연결해야 합니다."); // 잘못된 참조 오류 추가
        }

        if (!fixedHotbarSlotsUI.enabled) // Hotbar 컴포넌트 활성 상태 확인
        {
            errors.Add("화면 하단 Hotbar의 InventorySlotsUI가 비활성화되어 있습니다."); // Hotbar 비활성 오류 추가
        }

        if (!fixedHotbarSlotsUI.gameObject.activeInHierarchy) // Hotbar 오브젝트 활성 상태 확인
        {
            errors.Add("화면 하단 Hotbar 오브젝트가 비활성화되어 있습니다."); // Hotbar 오브젝트 오류 추가
        }

        if (Application.isPlaying && !fixedHotbarSlotsUI.IsInitialized) // 실행 중 슬롯 초기화 상태 확인
        {
            errors.Add("화면 하단 Hotbar 슬롯이 초기화되지 않았습니다. Player Inventory, Slot Container와 Slot Template을 확인해야 합니다."); // Hotbar 초기화 오류 추가
        }

        if (!ContainsNameInHierarchy(fixedHotbarSlotsUI.transform, "Hotbar")) // Hotbar 계층 이름 확인
        {
            warnings.Add("Fixed Hotbar Slots UI가 이름에 Hotbar가 포함된 계층 아래에 있지 않습니다. 잘못된 InventorySlotsUI를 연결했는지 확인해야 합니다."); // Hotbar 선택 경고 추가
        }
    }

    private void ValidatePopupPrefabs(
        List<string> errors,
        List<string> warnings) // 런타임 팝업 프리팹 검사
    {
        if (inventoryPopupPrefab == null) // 인벤토리 프리팹 연결 확인
        {
            errors.Add("Inventory Popup Prefab이 연결되지 않았습니다."); // 인벤토리 프리팹 오류 추가
        }
        else // 인벤토리 프리팹 세부 검사
        {
            if (!inventoryPopupPrefab.gameObject.activeSelf) // 프리팹 루트 활성 상태 확인
            {
                errors.Add("PF_UI_InventoryPopup 루트는 프리팹 에셋에서 활성 상태여야 합니다."); // 인벤토리 루트 오류 추가
            }

            Transform inventoryPanel = FindChildByName(
                inventoryPopupPrefab.transform,
                "InventoryPopup"); // 실제 인벤토리 패널 검색

            if (inventoryPanel == null) // 실제 패널 존재 확인
            {
                errors.Add("PF_UI_InventoryPopup 아래에서 InventoryPopup 자식을 찾을 수 없습니다."); // 인벤토리 패널 오류 추가
            }
            else if (!inventoryPanel.gameObject.activeSelf) // 프리팹 자식 활성 상태 확인
            {
                errors.Add("InventoryPopup 자식은 자식 UI Awake 실행을 위해 프리팹 에셋에서 활성 상태여야 합니다."); // 초기화 순서 오류 추가
            }

            InventorySlotsUI[] popupSlotViews = inventoryPopupPrefab.GetComponentsInChildren<InventorySlotsUI>(true); // 프리팹 슬롯 UI 검색

            if (popupSlotViews.Length < 2) // Hotbar와 일반 인벤토리 영역 확인
            {
                warnings.Add($"PF_UI_InventoryPopup 안의 InventorySlotsUI가 {popupSlotViews.Length}개입니다. Hotbar와 일반 인벤토리 영역 구성을 확인해야 합니다."); // 슬롯 UI 수 경고 추가
            }
        }

        if (storagePopupPrefab == null) // 보관함 프리팹 연결 확인
        {
            errors.Add("Storage Popup Prefab이 연결되지 않았습니다."); // 보관함 프리팹 오류 추가
        }
        else if (!storagePopupPrefab.gameObject.activeSelf) // 보관함 프리팹 활성 상태 확인
        {
            errors.Add("PF_UI_StoragePopup 루트는 프리팹 에셋에서 활성 상태여야 합니다."); // 보관함 루트 오류 추가
        }
    }

    private void ValidateInitialRuntimeState(List<string> warnings) // 시작 팝업 상태 검사
    {
        if (!Application.isPlaying || gameUIManager == null) // Play Mode와 관리자 확인
        {
            return; // 런타임 상태 검사 생략
        }

        if (gameUIManager.CurrentPopupType != GamePopupType.None) // 게임 시작 팝업 상태 확인
        {
            warnings.Add($"검증 시점에 Current Popup Type이 {gameUIManager.CurrentPopupType}입니다."); // 시작 상태 경고 추가
        }

        if (popupLayer == null) // PopupLayer 존재 확인
        {
            return; // 팝업 개수 검사 중단
        }

        InventoryPopupController[] inventoryInstances = popupLayer.GetComponentsInChildren<InventoryPopupController>(true); // 생성된 인벤토리 팝업 검색
        StorageContainerUI[] storageInstances = popupLayer.GetComponentsInChildren<StorageContainerUI>(true); // 생성된 보관함 팝업 검색

        if (inventoryInstances.Length > 1) // 인벤토리 중복 생성 확인
        {
            warnings.Add($"PopupLayer 아래 인벤토리 팝업이 {inventoryInstances.Length}개 생성되어 있습니다."); // 인벤토리 중복 경고 추가
        }

        if (storageInstances.Length > 1) // 보관함 중복 생성 확인
        {
            warnings.Add($"PopupLayer 아래 보관함 팝업이 {storageInstances.Length}개 생성되어 있습니다."); // 보관함 중복 경고 추가
        }
    }

    private void MonitorRuntimeState() // 실행 중 UI 상태 검사
    {
        ResolveSceneReferences(); // 파괴 또는 교체된 Scene 참조 재검색

        List<string> issues = new List<string>(); // 현재 런타임 문제 목록

        if (gameUIManager == null) // 게임 UI 관리자 존재 확인
        {
            issues.Add("GameUIManager 없음"); // 관리자 문제 추가
        }
        else if (!gameUIManager.enabled) // 게임 UI 관리자 활성 상태 확인
        {
            issues.Add("GameUIManager 비활성화"); // 관리자 비활성 문제 추가
        }

        if (fixedHotbarSlotsUI == null) // 고정 Hotbar 존재 확인
        {
            issues.Add("고정 Hotbar 없음"); // Hotbar 참조 문제 추가
        }
        else if (!fixedHotbarSlotsUI.IsInitialized) // Hotbar 초기화 상태 확인
        {
            issues.Add("고정 Hotbar 초기화 실패"); // Hotbar 초기화 문제 추가
        }

        if (buildPlacementController == null) // 건축 관리자 존재 확인
        {
            issues.Add("BuildPlacementController 없음"); // 건축 관리자 문제 추가
        }
        else if (!buildPlacementController.enabled
    && (gameplayInputLock == null || !gameplayInputLock.IsLocked)) // 의도적인 입력 잠금 제외
        {
            issues.Add("BuildPlacementController 비정상 비활성화"); // 실제 건축 비활성 문제 추가
        }

        if (popupLayer != null) // PopupLayer 존재 확인
        {
            InventoryPopupController[] inventoryInstances = popupLayer.GetComponentsInChildren<InventoryPopupController>(true); // 인벤토리 인스턴스 검색
            StorageContainerUI[] storageInstances = popupLayer.GetComponentsInChildren<StorageContainerUI>(true); // 보관함 인스턴스 검색

            if (inventoryInstances.Length > 1) // 인벤토리 중복 확인
            {
                issues.Add($"인벤토리 팝업 중복 {inventoryInstances.Length}개"); // 중복 문제 추가
            }

            if (storageInstances.Length > 1) // 보관함 중복 확인
            {
                issues.Add($"보관함 팝업 중복 {storageInstances.Length}개"); // 중복 문제 추가
            }
        }

        bool hasPopupMismatch = DetectPopupStateMismatch(issues); // 관리자와 실제 팝업 상태 검사

        if (hasPopupMismatch && repairPopupStateMismatch) // 상태 불일치와 자동 복구 설정 확인
        {
            RepairPopupState(); // 실제 팝업 상태 복구
        }

        ReportRuntimeIssues(issues); // 중복 방지 런타임 문제 출력
    }

    private bool DetectPopupStateMismatch(List<string> issues) // 관리자와 실제 팝업 상태 불일치 검사
    {
        if (gameUIManager == null) // 게임 UI 관리자 확인
        {
            return false; // 팝업 상태 검사 중단
        }

        bool inventoryOpen =
    gameUIManager.InventoryPopupInstance != null
    && gameUIManager.InventoryPopupInstance.IsVisible; // 실제 인벤토리 화면 표시 상태 계산

        bool storageOpen =
            gameUIManager.StoragePopupInstance != null
            && gameUIManager.StoragePopupInstance.IsOpen; // 실제 보관함 열림 상태 계산

        bool mismatchDetected = false; // 상태 불일치 여부

        if (inventoryOpen && storageOpen) // 두 팝업 동시 표시 확인
        {
            issues.Add("인벤토리와 보관함이 동시에 열림"); // 동시 표시 문제 추가
            mismatchDetected = true; // 상태 불일치 기록
        }

        switch (gameUIManager.CurrentPopupType) // 관리자 팝업 상태 분기
        {
            case GamePopupType.Inventory: // 인벤토리 상태
                if (!inventoryOpen) // 실제 인벤토리 표시 여부 확인
                {
                    issues.Add("관리자는 Inventory 상태지만 실제 인벤토리가 닫힘"); // 인벤토리 상태 문제 추가
                    mismatchDetected = true; // 상태 불일치 기록
                }

                break; // 인벤토리 상태 검사 종료

            case GamePopupType.Storage: // 보관함 상태
                if (!storageOpen) // 실제 보관함 표시 여부 확인
                {
                    issues.Add("관리자는 Storage 상태지만 실제 보관함이 닫힘"); // 보관함 상태 문제 추가
                    mismatchDetected = true; // 상태 불일치 기록
                }

                break; // 보관함 상태 검사 종료

            case GamePopupType.None: // 열린 팝업 없음 상태
                if (inventoryOpen || storageOpen) // 실제 팝업 표시 여부 확인
                {
                    issues.Add("관리자는 None 상태지만 실제 팝업이 열림"); // 관리자 상태 문제 추가
                    mismatchDetected = true; // 상태 불일치 기록
                }

                break; // 없음 상태 검사 종료
        }

        return mismatchDetected; // 상태 불일치 결과 반환
    }

    private void RepairPopupState() // 관리자와 실제 팝업 상태 복구
    {
        if (gameUIManager == null) // 게임 UI 관리자 존재 확인
        {
            return; // 복구 처리 중단
        }

        bool inventoryOpen =
    gameUIManager.InventoryPopupInstance != null
    && gameUIManager.InventoryPopupInstance.IsVisible; // 실제 인벤토리 화면 표시 상태 계산

        bool storageOpen =
            gameUIManager.StoragePopupInstance != null
            && gameUIManager.StoragePopupInstance.IsOpen; // 실제 보관함 열림 상태 계산

        if (inventoryOpen && storageOpen) // 두 팝업 동시 표시 확인
        {
            switch (gameUIManager.CurrentPopupType) // 관리자 상태 기준 유지 팝업 결정
            {
                case GamePopupType.Inventory: // 인벤토리 유지 상태
                    gameUIManager.CloseStorage(); // 보관함만 숨김
                    return; // 복구 완료

                case GamePopupType.Storage: // 보관함 유지 상태
                    gameUIManager.CloseInventory(); // 인벤토리만 숨김
                    return; // 복구 완료

                default: // 관리자 상태 없음
                    gameUIManager.CloseInventory(); // 인벤토리 종료
                    gameUIManager.CloseStorage(); // 보관함 종료
                    return; // 복구 완료
            }
        }

        switch (gameUIManager.CurrentPopupType) // 관리자 상태별 복구
        {
            case GamePopupType.Inventory: // 인벤토리 상태
                if (!inventoryOpen) // 실제 인벤토리 닫힘 확인
                {
                    gameUIManager.CloseInventory(); // 관리자 상태와 입력 잠금 초기화
                }

                break; // 인벤토리 상태 복구 종료

            case GamePopupType.Storage: // 보관함 상태
                if (!storageOpen) // 실제 보관함 닫힘 확인
                {
                    gameUIManager.CloseStorage(); // 관리자 상태와 입력 잠금 초기화
                }

                break; // 보관함 상태 복구 종료

            case GamePopupType.None: // 관리자 팝업 없음 상태
                if (inventoryOpen) // 실제 인벤토리 표시 확인
                {
                    gameUIManager.CloseInventory(); // 숨은 관리자 상태의 인벤토리 종료
                }

                if (storageOpen) // 실제 보관함 표시 확인
                {
                    gameUIManager.CloseStorage(); // 숨은 관리자 상태의 보관함 종료
                }

                break; // 없음 상태 복구 종료
        }
    }

    private void PrintValidationResult(
        List<string> errors,
        List<string> warnings) // 전체 검증 결과 출력
    {
        for (int index = 0; index < errors.Count; index++) // 전체 오류 순회
        {
            Debug.LogError($"[Gameplay UI Validation] {errors[index]}", this); // 오류 출력
        }

        for (int index = 0; index < warnings.Count; index++) // 전체 경고 순회
        {
            Debug.LogWarning($"[Gameplay UI Validation] {warnings[index]}", this); // 경고 출력
        }

        if (!LastValidationSucceeded || !logSuccessSummary) // 성공 요약 출력 조건 확인
        {
            return; // 정상 요약 출력 생략
        }

        Debug.Log(
            $"[Gameplay UI Validation] 검증 완료 | 오류 0 | 경고 {warnings.Count} | "
            + $"Hotbar 초기화 {GetHotbarStateLabel()} | "
            + $"현재 팝업 {GetPopupStateLabel()}",
            this); // 정상 검증 요약 출력
    }

    private void ReportRuntimeIssues(List<string> issues) // 중복 방지 런타임 문제 출력
    {
        string currentIssueKey = BuildIssueKey(issues); // 현재 문제 식별값 생성

        if (currentIssueKey == lastRuntimeIssueKey) // 이전 출력과 동일 여부 확인
        {
            return; // 반복 로그 출력 차단
        }

        lastRuntimeIssueKey = currentIssueKey; // 새로운 문제 식별값 저장

        if (issues.Count == 0) // 현재 문제 없음 확인
        {
            return; // 정상 상태 로그 생략
        }

        Debug.LogWarning(
            $"[Gameplay UI Runtime] {string.Join(" | ", issues)}",
            this); // 현재 런타임 문제 묶음 출력
    }

    private string BuildIssueKey(List<string> issues) // 현재 문제 식별 문자열 생성
    {
        if (issues.Count == 0) // 문제 없음 확인
        {
            return string.Empty; // 빈 식별값 반환
        }

        StringBuilder builder = new StringBuilder(); // 문제 식별 문자열 조합기 생성

        for (int index = 0; index < issues.Count; index++) // 전체 문제 순회
        {
            if (index > 0) // 첫 문제 이후 확인
            {
                builder.Append('|'); // 문제 구분자 추가
            }

            builder.Append(issues[index]); // 현재 문제 추가
        }

        return builder.ToString(); // 완성 문제 식별값 반환
    }

    private string GetHotbarStateLabel() // Hotbar 초기화 표시 문구 반환
    {
        if (fixedHotbarSlotsUI == null) // Hotbar 참조 확인
        {
            return "MISSING"; // 누락 문구 반환
        }

        return fixedHotbarSlotsUI.IsInitialized
            ? "READY"
            : "NOT READY"; // 초기화 상태 문구 반환
    }

    private string GetPopupStateLabel() // 현재 팝업 표시 문구 반환
    {
        if (gameUIManager == null) // 게임 UI 관리자 참조 확인
        {
            return "NO MANAGER"; // 관리자 누락 문구 반환
        }

        return gameUIManager.CurrentPopupType.ToString(); // 현재 팝업 종류 반환
    }

    private InventorySlotsUI FindFixedHotbarSlotsUI() // Scene 고정 Hotbar 슬롯 UI 검색
    {
        InventorySlotsUI[] slotViews = FindObjectsByType<InventorySlotsUI>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None); // Scene 전체 슬롯 UI 검색

        for (int index = 0; index < slotViews.Length; index++) // 전체 슬롯 UI 순회
        {
            InventorySlotsUI candidate = slotViews[index]; // 현재 슬롯 UI 조회

            if (candidate == null || !candidate.gameObject.scene.IsValid()) // Scene 오브젝트 여부 확인
            {
                continue; // 프리팹 에셋과 빈 참조 제외
            }

            if (ContainsNameInHierarchy(candidate.transform, "Hotbar")) // Hotbar 계층 여부 확인
            {
                return candidate; // 고정 Hotbar 후보 반환
            }
        }

        return null; // 고정 Hotbar 검색 실패 반환
    }

    private Transform FindSceneTransformByName(string targetName) // Scene Transform 이름 검색
    {
        Transform[] transforms = FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None); // Scene 전체 Transform 검색

        for (int index = 0; index < transforms.Length; index++) // 전체 Transform 순회
        {
            Transform candidate = transforms[index]; // 현재 Transform 조회

            if (candidate == null || !candidate.gameObject.scene.IsValid()) // Scene 오브젝트 여부 확인
            {
                continue; // 프리팹 에셋과 빈 참조 제외
            }

            if (candidate.name == targetName) // 대상 이름 확인
            {
                return candidate; // 일치 Transform 반환
            }
        }

        return null; // Transform 검색 실패 반환
    }

    private Transform FindChildByName(
        Transform root,
        string targetName) // 하위 Transform 이름 검색
    {
        if (root == null) // 검색 루트 확인
        {
            return null; // 검색 실패 반환
        }

        Transform[] children = root.GetComponentsInChildren<Transform>(true); // 전체 하위 Transform 검색

        for (int index = 0; index < children.Length; index++) // 전체 하위 Transform 순회
        {
            Transform candidate = children[index]; // 현재 Transform 조회

            if (candidate.name == targetName) // 대상 이름 확인
            {
                return candidate; // 일치 Transform 반환
            }
        }

        return null; // 하위 Transform 검색 실패 반환
    }

    private bool ContainsNameInHierarchy(
        Transform target,
        string keyword) // 부모 계층 이름 포함 여부 확인
    {
        Transform current = target; // 현재 계층 위치 설정

        while (current != null) // 최상위 부모까지 반복
        {
            bool containsKeyword =
                current.name.IndexOf(
                    keyword,
                    System.StringComparison.OrdinalIgnoreCase) >= 0; // 현재 이름의 키워드 포함 여부 계산

            if (containsKeyword) // 키워드 포함 확인
            {
                return true; // 계층 이름 일치 반환
            }

            current = current.parent; // 다음 부모로 이동
        }

        return false; // 계층 이름 불일치 반환
    }

    private void OnValidate() // Inspector 설정값 보정
    {
        monitorInterval = Mathf.Max(0.1f, monitorInterval); // 최소 검사 간격 적용
    }
}
