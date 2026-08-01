using UnityEngine; // Unity 기본 기능
using UnityEngine.InputSystem; // Input System 키보드 입력 기능
using UnityEngine.SceneManagement; // Scene 전환 기능

[DefaultExecutionOrder(-10000)] // 다른 UI Update보다 먼저 ESC 입력 상태 기록
[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class PauseMenuController : MonoBehaviour // Gameplay 일시정지 메뉴 관리자
{
    private const string PauseLockId = "PauseMenu"; // GameplayInputLock 일시정지 잠금 ID

    [Header("Scene References")] // Scene 참조 묶음
    [Tooltip("플레이어 입력과 HUD를 잠그는 공통 입력 잠금 관리자입니다.")]
    [SerializeField] private GameplayInputLock gameplayInputLock; // 공통 입력 잠금 관리자

    [Tooltip("일반 인벤토리와 보관함 팝업을 닫기 위한 공통 게임 UI 관리자입니다.")]
    [SerializeField] private GameUIManager gameUIManager; // 공통 게임 UI 관리자

    [Tooltip("ESC로 전체 지도를 닫은 같은 프레임에 일시정지 메뉴가 열리지 않도록 확인할 지도 관리자입니다.")]
    [SerializeField] private WorldMapController worldMapController; // 미니맵과 전체 지도 관리자

    [Tooltip("ESC로 건축 모드를 종료한 같은 프레임에 일시정지 메뉴가 열리지 않도록 확인할 건축 관리자입니다.")]
    [SerializeField] private BuildPlacementController buildPlacementController; // 건축 배치 관리자

    [Tooltip("현재 게임 상태를 저장하고 불러오는 Gameplay 저장 관리자입니다.")]
    [SerializeField] private GameplaySaveController gameplaySaveController; // Gameplay 저장 관리자

    [Tooltip("설정 화면에서 마우스 감도를 적용할 플레이어 추적 카메라입니다.")]
    [SerializeField] private ThirdPersonCameraFollow thirdPersonCameraFollow; // 플레이어 추적 카메라

    [Tooltip("일시정지 메뉴 프리팹을 생성할 Canvas 아래 PopupLayer입니다.")]
    [SerializeField] private Transform popupLayer; // 런타임 팝업 생성 부모

    [Header("Pause Menu Prefab")] // 일시정지 메뉴 프리팹 설정 묶음
    [Tooltip("최초 ESC 입력 시 생성하고 이후 재사용할 일시정지 메뉴 프리팹입니다.")]
    [SerializeField] private PauseMenuView pauseMenuPrefab; // 일시정지 메뉴 프리팹

    [Header("Scene Transition")] // Scene 전환 설정 묶음
    [Tooltip("MAIN MENU 버튼을 눌렀을 때 이동할 메인 메뉴 Scene 이름입니다.")]
    [SerializeField] private string mainMenuSceneName = "00_MainMenu"; // 메인 메뉴 Scene 이름

    [Header("Pause Settings")] // 일시정지 동작 설정 묶음
    [Tooltip("일시정지 메뉴를 열 때 Time.timeScale을 0으로 변경할지 설정합니다.")]
    [SerializeField] private bool pauseGameTime = true; // 게임 시간 정지 여부

    private PauseMenuView pauseMenuInstance; // 런타임 생성 일시정지 메뉴 인스턴스
    private bool escapePressedThisFrame; // 현재 프레임 ESC 입력 여부
    private bool hadGamePopupAtFrameStart; // 현재 프레임 시작 시 게임 팝업 존재 여부
    private bool hadWorldMapOpenAtFrameStart; // 현재 프레임 시작 시 전체 지도 열림 여부
    private bool hadBuildModeAtFrameStart; // 현재 프레임 시작 시 건축 모드 실행 여부
    private float previousTimeScale = 1f; // 일시정지 이전 게임 시간 배율
    private bool initialized; // 필수 참조 초기화 완료 여부
    private bool isClosing; // 닫기 애니메이션 진행 여부

    public static bool IsPaused { get; private set; } // 현재 Gameplay 일시정지 여부 제공
    public PauseMenuView PauseMenuInstance => pauseMenuInstance; // 생성된 일시정지 메뉴 인스턴스 제공

    private void Awake() // 일시정지 관리자 초기화
    {
        IsPaused = false; // Scene 시작 일시정지 상태 초기화
        isClosing = false; // Scene 시작 닫기 상태 초기화
        ResolveSceneReferences(); // 누락된 Scene 참조 자동 검색

        initialized =
            gameplayInputLock != null
            && gameUIManager != null
            && worldMapController != null
            && buildPlacementController != null
            && gameplaySaveController != null
            && thirdPersonCameraFollow != null
            && popupLayer != null
            && pauseMenuPrefab != null; // 필수 참조 상태 계산

        if (!initialized) // 필수 참조 누락 확인
        {
            Debug.LogError(
                $"{gameObject.name}의 PauseMenuController 필수 참조를 모두 연결해야 합니다.",
                this); // 필수 참조 오류 출력

            enabled = false; // 일시정지 입력 처리 비활성화
            return; // 초기화 중단
        }

        GameSettingsService.ApplyStoredSettings(thirdPersonCameraFollow); // 저장된 게임 설정 시작 적용
    }

    private void Update() // ESC 입력과 프레임 시작 UI 상태 기록
    {
        escapePressedThisFrame = false; // 현재 프레임 ESC 입력 초기화
        hadGamePopupAtFrameStart = gameUIManager != null && gameUIManager.HasOpenPopup; // 현재 프레임 시작 팝업 상태 저장
        hadWorldMapOpenAtFrameStart = worldMapController != null && worldMapController.IsFullMapOpen; // 현재 프레임 시작 전체 지도 상태 저장
        hadBuildModeAtFrameStart = buildPlacementController != null && buildPlacementController.IsBuildMode; // 현재 프레임 시작 건축 모드 상태 저장

        Keyboard keyboard = Keyboard.current; // 현재 키보드 장치 조회

        if (keyboard == null) // 키보드 장치 존재 확인
        {
            return; // 입력 처리 중단
        }

        escapePressedThisFrame = keyboard.escapeKey.wasPressedThisFrame; // 현재 프레임 ESC 입력 저장
    }

    private void LateUpdate() // 기존 UI와 건축 처리 이후 일시정지 메뉴 입력 처리
    {
        if (!initialized) // 초기화 상태 확인
        {
            return; // 일시정지 처리 중단
        }

        if (IsPaused && gameUIManager != null && gameUIManager.HasOpenPopup) // 일시정지 중 다른 팝업 열림 확인
        {
            CloseAllGamePopups(); // 일시정지와 충돌하는 게임 팝업 닫기
        }

        if (!escapePressedThisFrame) // ESC 입력 여부 확인
        {
            return; // 일시정지 전환 생략
        }

        if (hadGamePopupAtFrameStart
            || hadWorldMapOpenAtFrameStart
            || hadBuildModeAtFrameStart) // ESC 입력 전 기존 UI 또는 건축 모드 확인
        {
            return; // 기존 UI 또는 건축 모드 종료만 수행하고 일시정지 메뉴는 열지 않음
        }

        TogglePauseMenu(); // 일시정지 메뉴 상태 전환
    }

    public void TogglePauseMenu() // 일시정지 메뉴 열기와 닫기 전환
    {
        if (isClosing) // 닫기 애니메이션 진행 여부 확인
        {
            return; // 애니메이션 중 중복 입력 방지
        }

        if (IsPaused && pauseMenuInstance != null && pauseMenuInstance.IsSettingsPageOpen) // 설정 화면 표시 여부 확인
        {
            pauseMenuInstance.ShowMainPage(); // ESC로 일시정지 메인 화면 복귀
            return; // 메뉴 자체는 유지
        }

        if (IsPaused) // 현재 일시정지 상태 확인
        {
            ClosePauseMenu(); // 일시정지 메뉴 닫기
            return; // 상태 전환 종료
        }

        OpenPauseMenu(); // 일시정지 메뉴 열기
    }

    public void OpenPauseMenu() // 일시정지 메뉴 열기
    {
        if (!initialized || IsPaused || isClosing) // 초기화와 기존 전환 상태 확인
        {
            return; // 중복 열기 방지
        }

        if (buildPlacementController != null && buildPlacementController.IsBuildMode) // 건축 모드 실행 여부 확인
        {
            buildPlacementController.ExitBuildModeFromExternal(); // 일시정지 전 자유 건축 Camera와 Preview 정리
        }

        if (worldMapController != null && worldMapController.IsFullMapOpen) // 전체 지도 열림 여부 확인
        {
            worldMapController.CloseFullMapImmediate(); // 일시정지 메뉴 전 전체 지도 상태 정리
        }

        if (!EnsurePauseMenuInstance()) // 일시정지 메뉴 인스턴스 준비 확인
        {
            return; // 메뉴 생성 실패로 열기 중단
        }

        CloseAllGamePopups(); // 인벤토리와 보관함 팝업 닫기
        previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f; // 일시정지 이전 게임 시간 배율 저장
        gameplayInputLock.Acquire(PauseLockId); // 플레이어 입력과 HUD 잠금 획득

        if (pauseGameTime) // 게임 시간 정지 사용 여부 확인
        {
            Time.timeScale = 0f; // Gameplay 시간 정지
        }

        IsPaused = true; // 일시정지 상태 저장
        pauseMenuInstance.Show(); // 좌측 슬라이드 메뉴 화면 표시
    }

    public void ClosePauseMenu() // 일시정지 메뉴 닫기
    {
        if (!IsPaused || isClosing) // 현재 일시정지와 닫기 상태 확인
        {
            return; // 중복 닫기 방지
        }

        isClosing = true; // 닫기 애니메이션 진행 상태 저장

        if (pauseMenuInstance == null) // 메뉴 인스턴스 존재 확인
        {
            CompleteClosePauseMenu(); // 화면 없이 일시정지 상태 즉시 정리
            return; // 닫기 처리 종료
        }

        pauseMenuInstance.Hide(CompleteClosePauseMenu); // 좌측 슬라이드 종료 후 Gameplay 복구
    }

    public void SaveCurrentGameFromPause() // 일시정지 메뉴에서 현재 게임 저장
    {
        if (gameplaySaveController == null) // 저장 관리자 존재 확인
        {
            Debug.LogError("PauseMenuController에 GameplaySaveController가 연결되지 않았습니다.", this); // 저장 관리자 누락 오류 출력
            return; // 저장 처리 중단
        }

        gameplaySaveController.SaveCurrentGame(); // 기존 Gameplay 저장 기능 실행
    }

    public void LoadCurrentGameFromPause() // 일시정지 메뉴에서 저장 게임 불러오기
    {
        if (gameplaySaveController == null) // 저장 관리자 존재 확인
        {
            Debug.LogError("PauseMenuController에 GameplaySaveController가 연결되지 않았습니다.", this); // 저장 관리자 누락 오류 출력
            return; // 불러오기 처리 중단
        }

        gameplaySaveController.LoadCurrentGame(); // 기존 Gameplay 불러오기 기능 실행
    }

    public void ReturnToMainMenu() // 메인 메뉴 Scene 이동
    {
        if (string.IsNullOrWhiteSpace(mainMenuSceneName)) // 메인 메뉴 Scene 이름 확인
        {
            Debug.LogError("PauseMenuController의 Main Menu Scene Name이 비어 있습니다.", this); // Scene 이름 오류 출력
            return; // Scene 이동 중단
        }

        PrepareForSceneExit(); // Scene 이동 전 일시정지 상태 정리
        SceneManager.LoadScene(mainMenuSceneName); // 메인 메뉴 Scene 불러오기
    }

    public void QuitGame() // 게임 종료
    {
        PrepareForSceneExit(); // 게임 종료 전 일시정지 상태 정리

#if UNITY_EDITOR
        Debug.Log("Unity Editor에서는 Application.Quit이 실행 파일을 종료하지 않습니다.", this); // Editor 종료 제한 안내
#endif

        Application.Quit(); // 실행 중인 게임 종료
    }

    private bool EnsurePauseMenuInstance() // 일시정지 메뉴 최초 생성 또는 기존 인스턴스 확인
    {
        if (pauseMenuInstance != null) // 기존 인스턴스 존재 확인
        {
            return true; // 기존 인스턴스 재사용
        }

        pauseMenuInstance = Instantiate(pauseMenuPrefab, popupLayer); // PopupLayer 아래 일시정지 메뉴 프리팹 생성
        pauseMenuInstance.name = pauseMenuPrefab.name; // 런타임 Clone 접미사 제거

        if (pauseMenuInstance.Initialize(this, thirdPersonCameraFollow)) // 메뉴 버튼과 설정 화면 연결 시도
        {
            pauseMenuInstance.HideImmediate(); // 최초 생성 메뉴 즉시 숨김
            return true; // 메뉴 생성 성공 반환
        }

        Debug.LogError("PauseMenuView 초기화에 실패했습니다.", pauseMenuInstance); // 메뉴 초기화 오류 출력
        Destroy(pauseMenuInstance.gameObject); // 잘못 생성된 메뉴 제거
        pauseMenuInstance = null; // 메뉴 인스턴스 참조 초기화
        return false; // 메뉴 생성 실패 반환
    }

    private void CompleteClosePauseMenu() // 닫기 애니메이션 이후 Gameplay 복구
    {
        IsPaused = false; // 일시정지 상태 해제
        isClosing = false; // 닫기 애니메이션 상태 해제

        if (gameplayInputLock != null) // 입력 잠금 관리자 존재 확인
        {
            gameplayInputLock.Release(PauseLockId); // 플레이어 입력과 HUD 잠금 해제
        }

        if (pauseGameTime) // 게임 시간 정지 사용 여부 확인
        {
            Time.timeScale = previousTimeScale; // 일시정지 이전 게임 시간 배율 복구
        }
    }

    private void CloseAllGamePopups() // 인벤토리와 보관함 팝업 닫기
    {
        if (gameUIManager == null) // 게임 UI 관리자 존재 확인
        {
            return; // 팝업 닫기 처리 생략
        }

        gameUIManager.CloseInventory(); // 일반 인벤토리 팝업 닫기
        gameUIManager.CloseStorage(); // 보관함 팝업 닫기
    }

    private void ResolveSceneReferences() // 누락된 Scene 참조 자동 검색
    {
        if (gameplayInputLock == null) // 입력 잠금 관리자 참조 확인
        {
            gameplayInputLock = FindFirstObjectByType<GameplayInputLock>(FindObjectsInactive.Include); // Scene 입력 잠금 관리자 검색
        }

        if (gameUIManager == null) // 게임 UI 관리자 참조 확인
        {
            gameUIManager = FindFirstObjectByType<GameUIManager>(FindObjectsInactive.Include); // Scene 게임 UI 관리자 검색
        }

        if (worldMapController == null) // 지도 관리자 참조 확인
        {
            worldMapController = FindFirstObjectByType<WorldMapController>(FindObjectsInactive.Include); // Scene 지도 관리자 검색
        }

        if (buildPlacementController == null) // 건축 관리자 참조 확인
        {
            buildPlacementController = FindFirstObjectByType<BuildPlacementController>(FindObjectsInactive.Include); // Scene 건축 관리자 검색
        }

        if (gameplaySaveController == null) // 저장 관리자 참조 확인
        {
            gameplaySaveController = FindFirstObjectByType<GameplaySaveController>(FindObjectsInactive.Include); // Scene 저장 관리자 검색
        }

        if (thirdPersonCameraFollow == null) // 추적 카메라 참조 확인
        {
            thirdPersonCameraFollow = FindFirstObjectByType<ThirdPersonCameraFollow>(FindObjectsInactive.Include); // Scene 추적 카메라 검색
        }

        if (popupLayer == null) // PopupLayer 참조 확인
        {
            popupLayer = FindPopupLayer(); // Scene PopupLayer 이름 검색
        }
    }

    private Transform FindPopupLayer() // Scene의 PopupLayer Transform 검색
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

            if (candidate.name == "PopupLayer") // PopupLayer 이름 확인
            {
                return candidate; // PopupLayer Transform 반환
            }
        }

        return null; // PopupLayer 검색 실패 반환
    }

    private void PrepareForSceneExit() // Scene 이동과 게임 종료 전 상태 정리
    {
        if (pauseMenuInstance != null) // 일시정지 메뉴 인스턴스 존재 확인
        {
            pauseMenuInstance.HideImmediate(); // 메뉴 애니메이션 중단과 즉시 숨김
        }

        if (buildPlacementController != null && buildPlacementController.IsBuildMode) // 건축 모드 실행 여부 확인
        {
            buildPlacementController.ExitBuildModeFromExternal(); // Scene 이동 전 자유 건축 Camera와 Preview 정리
        }

        if (worldMapController != null) // 지도 관리자 존재 확인
        {
            worldMapController.CloseFullMapImmediate(); // Scene 이동 전 전체 지도 상태 정리
        }

        IsPaused = false; // 일시정지 상태 해제
        isClosing = false; // 닫기 상태 해제

        if (gameplayInputLock != null) // 입력 잠금 관리자 존재 확인
        {
            gameplayInputLock.Release(PauseLockId); // 일시정지 입력 잠금 해제
        }

        Time.timeScale = 1f; // Scene 전환을 위한 기본 게임 시간 복구
    }

    private void OnDisable() // 일시정지 관리자 비활성화 정리
    {
        if (!Application.isPlaying) // Play Mode 여부 확인
        {
            return; // Edit Mode 정리 생략
        }

        if (!IsPaused && !isClosing) // 정리할 일시정지 상태 확인
        {
            return; // 정리할 상태 없음
        }

        PrepareForSceneExit(); // 비활성화 시 입력 잠금과 시간 복구
    }
}
