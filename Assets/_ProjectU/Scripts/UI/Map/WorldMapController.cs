using UnityEngine; // Unity 기본 기능
using UnityEngine.InputSystem; // Input System 키보드와 마우스 입력 기능

[DefaultExecutionOrder(-9000)] // PauseMenuController의 상태 기록 이후 지도 입력 처리
[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class WorldMapController : MonoBehaviour // 미니맵 크기 전환과 전체 지도 패널 관리자
{
    private const string WorldMapLockId = "WorldMap"; // GameplayInputLock 전체 지도 잠금 ID

    [Header("Scene References")] // Scene 참조 묶음
    [Tooltip("전체 지도 사용 중 플레이어 입력과 HUD를 잠그는 공통 입력 잠금 관리자입니다.")]
    [SerializeField] private GameplayInputLock gameplayInputLock; // 공통 입력 잠금 관리자

    [Tooltip("전체 지도 사용 중 일반 인벤토리와 보관함을 닫을 공통 UI 관리자입니다.")]
    [SerializeField] private GameUIManager gameUIManager; // 공통 게임 UI 관리자

    [Tooltip("지도 카메라가 따라가고 방향 아이콘이 표시할 Player Transform입니다.")]
    [SerializeField] private Transform playerTarget; // 플레이어 Transform

    [Tooltip("플레이어 위에서 지도 RenderTexture를 만드는 전용 카메라 관리자입니다.")]
    [SerializeField] private MinimapCameraController minimapCameraController; // 지도 카메라 관리자

    [Tooltip("Gameplay HUD에 항상 표시할 작은 미니맵 화면입니다.")]
    [SerializeField] private MinimapHUDView minimapHUDView; // 미니맵 HUD 화면

    [Tooltip("전체 지도 프리팹을 런타임 생성할 Canvas 아래 PopupLayer입니다.")]
    [SerializeField] private Transform popupLayer; // 전체 지도 생성 부모

    [Header("World Map Prefab")] // 전체 지도 프리팹 설정 묶음
    [Tooltip("M 또는 미니맵 클릭 최초 입력 시 생성하고 이후 재사용할 전체 지도 프리팹입니다.")]
    [SerializeField] private WorldMapPanelView worldMapPanelPrefab; // 전체 지도 프리팹

    [Header("Start State")] // 지도 시작 상태 묶음
    [Tooltip("게임 시작 시 HUD 미니맵을 표시할지 설정합니다.")]
    [SerializeField] private bool minimapVisibleOnStart = true; // 시작 미니맵 표시 여부

    [Header("Full Map Settings")] // 전체 지도 동작 설정 묶음
    [Tooltip("전체 지도 패널을 열 때 Time.timeScale을 0으로 변경할지 설정합니다.")]
    [SerializeField] private bool pauseGameTime = true; // 전체 지도 사용 중 게임 시간 정지 여부

    private WorldMapPanelView worldMapPanelInstance; // 런타임 생성 전체 지도 인스턴스
    private bool initialized; // 전체 지도 시스템 초기화 완료 여부
    private bool isFullMapOpen; // 현재 전체 지도 패널 열림 여부
    private float previousTimeScale = 1f; // 전체 지도 이전 게임 시간 배율

    public bool IsFullMapOpen => isFullMapOpen; // 현재 전체 지도 패널 열림 여부 제공
    public WorldMapPanelView WorldMapPanelInstance => worldMapPanelInstance; // 생성된 전체 지도 인스턴스 제공

    private void Awake() // 지도 시스템 초기화
    {
        ResolveSceneReferences(); // 누락된 Scene 참조 자동 검색

        initialized =
            gameplayInputLock != null
            && gameUIManager != null
            && playerTarget != null
            && minimapCameraController != null
            && minimapHUDView != null
            && popupLayer != null
            && worldMapPanelPrefab != null; // 필수 참조 상태 계산

        if (!initialized) // 필수 참조 누락 확인
        {
            Debug.LogError(
                $"{gameObject.name}의 WorldMapController 필수 참조를 모두 연결해야 합니다.",
                this); // 지도 시스템 참조 오류 출력

            enabled = false; // 지도 입력 처리 비활성화
            return; // 초기화 중단
        }

        if (!minimapCameraController.Initialize(playerTarget)) // 지도 Camera 초기화 시도
        {
            Debug.LogError(
                "MinimapCameraController 초기화에 실패했습니다.",
                minimapCameraController); // 지도 Camera 초기화 오류 출력

            enabled = false; // 지도 입력 처리 비활성화
            return; // 초기화 중단
        }

        if (!minimapHUDView.Initialize(
            this,
            minimapCameraController.OutputTexture,
            playerTarget)) // 미니맵 HUD와 클릭 입력 초기화 시도
        {
            Debug.LogError(
                "MinimapHUDView 초기화에 실패했습니다.",
                minimapHUDView); // 미니맵 HUD 초기화 오류 출력

            enabled = false; // 지도 입력 처리 비활성화
            return; // 초기화 중단
        }

        minimapHUDView.SetUserVisible(minimapVisibleOnStart); // 시작 미니맵 표시 상태 적용
        minimapCameraController.SetViewMode(
            minimapHUDView.CurrentCameraMode); // 시작 미니맵 크기에 맞는 Camera 범위 적용
    }

    private void Update() // N, M, ESC와 전체 지도 휠 입력 처리
    {
        if (!initialized) // 초기화 상태 확인
        {
            return; // 지도 입력 처리 중단
        }

        Keyboard keyboard = Keyboard.current; // 현재 키보드 장치 조회

        if (isFullMapOpen) // 현재 전체 지도 표시 여부 확인
        {
            HandleFullMapZoomInput(); // 전체 지도 마우스 휠 줌 처리

            if (keyboard != null
                && (keyboard.escapeKey.wasPressedThisFrame
                    || keyboard.mKey.wasPressedThisFrame)) // 전체 지도 닫기 입력 확인
            {
                CloseFullMap(); // 전체 지도 닫기
            }

            return; // 전체 지도 중 미니맵 N 입력 차단
        }

        if (keyboard == null) // 키보드 장치 존재 확인
        {
            return; // 키보드 지도 입력 처리 중단
        }

        if (keyboard.mKey.wasPressedThisFrame) // M 입력 확인
        {
            OpenFullMap(); // 전체 지도 열기
            return; // 같은 프레임 N 입력 생략
        }

        if (!keyboard.nKey.wasPressedThisFrame) // N 입력 여부 확인
        {
            return; // 지도 크기 입력 없음
        }

        bool altPressed =
            keyboard.leftAltKey.isPressed
            || keyboard.rightAltKey.isPressed; // 좌우 Alt 커서 모드 상태 확인

        if (altPressed) // Alt를 누른 상태에서 N 입력 확인
        {
            return; // Alt+N 조합을 지도 입력으로 사용하지 않음
        }

        ToggleMinimapSize(); // N 단독 입력으로 작은 미니맵과 확장 미니맵 전환
    }

    private void LateUpdate() // 전체 지도와 다른 게임 팝업 동시 표시 방지
    {
        if (!initialized || !isFullMapOpen) // 초기화와 전체 지도 상태 확인
        {
            return; // 팝업 충돌 처리 생략
        }

        if (gameUIManager.HasOpenPopup) // 인벤토리 또는 보관함 팝업 열림 확인
        {
            CloseAllGamePopups(); // 전체 지도와 충돌하는 게임 팝업 닫기
        }
    }

    public void ToggleMinimapSize() // N 단독 입력 미니맵 크기 전환
    {
        if (!initialized
            || isFullMapOpen
            || PauseMenuController.IsPaused
            || gameUIManager.HasOpenPopup) // 다른 전체 화면 UI와 팝업 상태 확인
        {
            return; // 미니맵 크기 전환 차단
        }

        MapCameraViewMode viewMode =
            minimapHUDView.ToggleSize(); // 작은 미니맵과 확장 미니맵 UI 전환

        minimapCameraController.SetViewMode(viewMode); // 변경된 미니맵 범위 적용
    }

    public void ToggleFullMap() // M 또는 전체 지도 Button의 상태 전환
    {
        if (isFullMapOpen) // 현재 전체 지도 열림 여부 확인
        {
            CloseFullMap(); // 전체 지도 닫기
            return; // 상태 전환 종료
        }

        OpenFullMap(); // 전체 지도 열기
    }

    public void OpenFullMapFromMinimap() // Alt 커서 상태의 미니맵 클릭으로 전체 지도 열기
    {
        bool cursorActive =
            Cursor.visible
            && Cursor.lockState != CursorLockMode.Locked; // 현재 커서 활성 상태 확인

        if (!cursorActive) // Alt 커서 모드가 아닌 상태 확인
        {
            return; // 미니맵 클릭 전체 지도 열기 차단
        }

        OpenFullMap(); // M 키와 동일한 전체 지도 패널 열기
    }

    public void OpenFullMap() // 전체 지도 패널 열기
    {
        if (!initialized
            || isFullMapOpen
            || PauseMenuController.IsPaused) // 초기화와 다른 전체 화면 메뉴 상태 확인
        {
            return; // 전체 지도 중복 또는 충돌 열기 방지
        }

        if (!EnsureWorldMapPanelInstance()) // 전체 지도 인스턴스 생성 또는 기존 확인
        {
            return; // 전체 지도 생성 실패로 열기 중단
        }

        CloseAllGamePopups(); // 인벤토리와 보관함 팝업 닫기

        previousTimeScale =
            Time.timeScale > 0f
                ? Time.timeScale
                : 1f; // 전체 지도 이전 게임 시간 배율 저장

        gameplayInputLock.Acquire(WorldMapLockId); // 플레이어 입력과 HUD 잠금 획득

        if (pauseGameTime) // 전체 지도 시간 정지 사용 여부 확인
        {
            Time.timeScale = 0f; // Gameplay 시간 정지
        }

        isFullMapOpen = true; // 전체 지도 열림 상태 저장
        minimapHUDView.SetSuppressedByFullMap(true); // 작은 미니맵 임시 숨김
        minimapCameraController.SetViewMode(MapCameraViewMode.FullMap); // 현재 저장된 전체 지도 줌 범위 적용
        worldMapPanelInstance.Show(); // M과 미니맵 클릭이 공유하는 전체 지도 표시
    }

    public void CloseFullMap() // 전체 지도 패널 닫기
    {
        if (!isFullMapOpen) // 현재 전체 지도 상태 확인
        {
            return; // 중복 닫기 방지
        }

        if (worldMapPanelInstance != null) // 전체 지도 인스턴스 존재 확인
        {
            worldMapPanelInstance.HideImmediate(); // 전체 지도 화면 숨김
        }

        isFullMapOpen = false; // 전체 지도 열림 상태 해제
        minimapCameraController.SetViewMode(
            minimapHUDView.CurrentCameraMode); // 기존 미니맵 범위 복구

        minimapHUDView.SetSuppressedByFullMap(false); // 작은 미니맵 표시 상태 복구
        gameplayInputLock.Release(WorldMapLockId); // 플레이어 입력과 HUD 잠금 해제

        if (pauseGameTime) // 전체 지도 시간 정지 사용 여부 확인
        {
            Time.timeScale = previousTimeScale; // 전체 지도 이전 시간 배율 복구
        }
    }

    public void CloseFullMapImmediate() // Scene 전환과 다른 메뉴에서 전체 지도 즉시 정리
    {
        if (worldMapPanelInstance != null) // 전체 지도 인스턴스 존재 확인
        {
            worldMapPanelInstance.HideImmediate(); // 전체 지도 화면 즉시 숨김
        }

        bool wasOpen = isFullMapOpen; // 기존 전체 지도 열림 상태 저장
        isFullMapOpen = false; // 전체 지도 상태 해제

        if (minimapCameraController != null
            && minimapHUDView != null) // 지도 Camera와 HUD 존재 확인
        {
            minimapCameraController.SetViewMode(
                minimapHUDView.CurrentCameraMode); // 기존 미니맵 범위 복구

            minimapHUDView.SetSuppressedByFullMap(false); // 작은 미니맵 표시 상태 복구
        }

        if (gameplayInputLock != null) // 입력 잠금 관리자 존재 확인
        {
            gameplayInputLock.Release(WorldMapLockId); // 전체 지도 입력 잠금 해제
        }

        if (wasOpen && pauseGameTime) // 실제 열린 지도와 시간 정지 사용 여부 확인
        {
            Time.timeScale = previousTimeScale; // 전체 지도 이전 시간 배율 복구
        }
    }

    public void SetMinimapVisible(bool visible) // 설정 UI에서 사용할 미니맵 표시 변경
    {
        if (minimapHUDView == null) // 미니맵 HUD 존재 확인
        {
            return; // 표시 상태 변경 생략
        }

        minimapHUDView.SetUserVisible(visible); // 사용자 미니맵 표시 상태 적용
    }

    public void ResetFullMapZoom() // 전체 지도 줌을 Inspector 기본값으로 복구
    {
        if (minimapCameraController == null) // 지도 Camera 관리자 존재 확인
        {
            return; // 줌 복구 처리 생략
        }

        minimapCameraController.ResetFullMapZoom(); // 전체 지도 기본 Orthographic Size 복구
    }

    private void HandleFullMapZoomInput() // 전체 지도 마우스 휠 입력 처리
    {
        Mouse mouse = Mouse.current; // 현재 마우스 장치 조회

        if (mouse == null) // 마우스 장치 존재 확인
        {
            return; // 전체 지도 줌 처리 생략
        }

        float scrollDelta =
            mouse.scroll.ReadValue().y; // 현재 프레임 마우스 휠 입력 조회

        minimapCameraController.ZoomFullMap(
            scrollDelta); // 휠 방향에 따라 전체 지도 줌인 또는 줌아웃
    }

    private bool EnsureWorldMapPanelInstance() // 전체 지도 프리팹 최초 생성과 재사용
    {
        if (worldMapPanelInstance != null) // 기존 전체 지도 인스턴스 존재 확인
        {
            return true; // 기존 인스턴스 재사용
        }

        worldMapPanelInstance = Instantiate(
            worldMapPanelPrefab,
            popupLayer); // PopupLayer 아래 전체 지도 프리팹 생성

        worldMapPanelInstance.name =
            worldMapPanelPrefab.name; // 런타임 Clone 접미사 제거

        if (worldMapPanelInstance.Initialize(
            this,
            minimapCameraController.OutputTexture,
            playerTarget)) // 전체 지도 런타임 참조 연결 시도
        {
            worldMapPanelInstance.HideImmediate(); // 최초 생성 전체 지도 숨김
            return true; // 전체 지도 생성 성공 반환
        }

        Debug.LogError(
            "WorldMapPanelView 초기화에 실패했습니다.",
            worldMapPanelInstance); // 전체 지도 초기화 오류 출력

        Destroy(worldMapPanelInstance.gameObject); // 잘못 생성된 전체 지도 제거
        worldMapPanelInstance = null; // 전체 지도 인스턴스 참조 초기화
        return false; // 전체 지도 생성 실패 반환
    }

    private void CloseAllGamePopups() // 인벤토리와 보관함 팝업 닫기
    {
        if (gameUIManager == null) // 공통 게임 UI 관리자 존재 확인
        {
            return; // 팝업 닫기 생략
        }

        gameUIManager.CloseInventory(); // 일반 인벤토리 팝업 닫기
        gameUIManager.CloseStorage(); // 보관함 팝업 닫기
    }

    private void ResolveSceneReferences() // 누락된 지도 Scene 참조 자동 검색
    {
        if (gameplayInputLock == null) // 입력 잠금 관리자 참조 확인
        {
            gameplayInputLock =
                FindFirstObjectByType<GameplayInputLock>(
                    FindObjectsInactive.Include); // Scene 입력 잠금 관리자 검색
        }

        if (gameUIManager == null) // 공통 게임 UI 관리자 참조 확인
        {
            gameUIManager =
                FindFirstObjectByType<GameUIManager>(
                    FindObjectsInactive.Include); // Scene GameUIManager 검색
        }

        if (minimapCameraController == null) // 지도 Camera 관리자 참조 확인
        {
            minimapCameraController =
                FindFirstObjectByType<MinimapCameraController>(
                    FindObjectsInactive.Include); // Scene 지도 Camera 검색
        }

        if (minimapHUDView == null) // 미니맵 HUD 참조 확인
        {
            minimapHUDView =
                FindFirstObjectByType<MinimapHUDView>(
                    FindObjectsInactive.Include); // Scene 미니맵 HUD 검색
        }

        if (playerTarget == null) // Player Transform 참조 확인
        {
            PlayerInventory playerInventory =
                FindFirstObjectByType<PlayerInventory>(
                    FindObjectsInactive.Include); // Scene PlayerInventory 검색

            if (playerInventory != null) // 플레이어 컴포넌트 검색 성공 확인
            {
                playerTarget = playerInventory.transform; // PlayerInventory 오브젝트를 Player Transform으로 사용
            }
        }

        if (popupLayer == null) // PopupLayer 참조 확인
        {
            popupLayer = FindPopupLayer(); // Scene PopupLayer 이름 검색
        }
    }

    private Transform FindPopupLayer() // Scene의 PopupLayer Transform 검색
    {
        Transform[] transforms =
            FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None); // Scene 전체 Transform 검색

        for (int index = 0; index < transforms.Length; index++) // 전체 Transform 순회
        {
            Transform candidate = transforms[index]; // 현재 Transform 조회

            if (candidate == null
                || !candidate.gameObject.scene.IsValid()) // Scene 오브젝트 여부 확인
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

    private void OnDisable() // 지도 관리자 비활성화 정리
    {
        if (!Application.isPlaying || !isFullMapOpen) // Play Mode와 전체 지도 상태 확인
        {
            return; // 정리할 전체 지도 상태 없음
        }

        CloseFullMapImmediate(); // 입력 잠금과 게임 시간 즉시 복구
    }
}
