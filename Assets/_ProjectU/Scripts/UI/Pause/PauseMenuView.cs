using System; // 완료 콜백 기능
using System.Collections; // 코루틴 기능
using TMPro; // TextMeshPro UI 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI Button 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class PauseMenuView : MonoBehaviour // 좌측 슬라이드 일시정지 메뉴 화면
{
    [Header("Panel")] // 메뉴 화면 설정 묶음
    [Tooltip("화면 전체의 어두운 배경과 좌측 메뉴를 포함하는 최상위 오브젝트입니다.")]
    [SerializeField] private GameObject panelRoot; // 일시정지 메뉴 전체 화면 루트

    [Tooltip("배경 페이드와 클릭 차단을 담당하는 루트 CanvasGroup입니다.")]
    [SerializeField] private CanvasGroup rootCanvasGroup; // 전체 화면 투명도와 입력 차단

    [Tooltip("화면 왼쪽 바깥에서 안쪽으로 이동할 실제 메뉴 패널 RectTransform입니다.")]
    [SerializeField] private RectTransform drawerPanel; // 좌측 슬라이드 메뉴 패널

    [Header("Pages")] // 일시정지 메뉴 페이지 묶음
    [Tooltip("RESUME, SETTINGS, SAVE와 LOAD 버튼을 포함하는 메인 페이지 루트입니다.")]
    [SerializeField] private GameObject mainPageRoot; // 일시정지 메인 페이지

    [Tooltip("마스터 볼륨, 마우스 감도와 전체 화면 설정을 관리하는 설정 페이지입니다.")]
    [SerializeField] private PauseSettingsPanel settingsPanel; // 일시정지 설정 페이지

    [Header("Main Buttons")] // 일시정지 메인 버튼 참조 묶음
    [Tooltip("일시정지를 종료하고 Gameplay로 돌아가는 버튼입니다.")]
    [SerializeField] private Button resumeButton; // 게임 계속하기 버튼

    [Tooltip("일시정지 메뉴의 설정 페이지를 여는 버튼입니다.")]
    [SerializeField] private Button settingsButton; // 설정 화면 버튼

    [Tooltip("현재 Gameplay 상태를 기본 저장 슬롯에 저장하는 버튼입니다.")]
    [SerializeField] private Button saveButton; // 현재 게임 저장 버튼

    [Tooltip("기본 저장 슬롯의 Gameplay 상태를 불러오는 버튼입니다.")]
    [SerializeField] private Button loadButton; // 저장 게임 불러오기 버튼

    [Tooltip("지정된 메인 메뉴 Scene으로 이동하는 버튼입니다.")]
    [SerializeField] private Button mainMenuButton; // 메인 메뉴 이동 버튼

    [Tooltip("현재 실행 중인 게임을 종료하는 버튼입니다.")]
    [SerializeField] private Button quitButton; // 게임 종료 버튼

    [Header("Feedback")] // 저장과 불러오기 안내 묶음
    [Tooltip("SAVE 또는 LOAD 버튼을 눌렀을 때 안내 문구를 표시하는 Text입니다.")]
    [SerializeField] private TMP_Text actionStatusText; // 저장과 불러오기 안내 Text

    [Header("Slide Animation")] // 좌측 슬라이드 애니메이션 설정 묶음
    [Tooltip("좌측 메뉴가 열리고 닫히는 데 걸리는 실제 시간입니다.")]
    [SerializeField, Min(0.01f)] private float transitionDuration = 0.28f; // 열기와 닫기 애니메이션 시간

    [Tooltip("닫힌 상태에서 메뉴가 화면 왼쪽 밖으로 추가 이동할 거리입니다.")]
    [SerializeField, Min(0f)] private float hiddenPadding = 30f; // 화면 밖 추가 숨김 거리

    private PauseMenuController controller; // 일시정지 메뉴 관리자
    private Coroutine transitionRoutine; // 현재 실행 중인 슬라이드 코루틴
    private Vector2 openAnchoredPosition; // 화면 안쪽 열린 위치
    private Vector2 hiddenAnchoredPosition; // 화면 왼쪽 바깥 숨김 위치
    private bool internalReferencesValid; // 프리팹 내부 참조 상태
    private bool internalInitializationCompleted; // 내부 초기화 실행 완료 여부
    private bool listenersRegistered; // 버튼 이벤트 등록 여부
    private bool slidePositionsInitialized; // 열린 위치와 숨김 위치 계산 완료 여부

    public bool IsVisible =>
        panelRoot != null
        && panelRoot.activeSelf
        && rootCanvasGroup != null
        && rootCanvasGroup.alpha > 0.001f; // 실제 일시정지 메뉴 표시 여부 제공

    public bool IsTransitioning =>
        transitionRoutine != null; // 현재 슬라이드 애니메이션 진행 여부 제공

    public bool IsSettingsPageOpen =>
        settingsPanel != null
        && settingsPanel.IsVisible; // 현재 설정 페이지 표시 여부 제공

    private void Awake() // 일시정지 메뉴 내부 초기화
    {
        EnsureInternalInitialization(); // 프리팹 내부 참조와 버튼 이벤트 초기화
    }

    public bool Initialize(
        PauseMenuController owner,
        ThirdPersonCameraFollow cameraFollow) // 런타임 관리자와 설정 카메라 연결
    {
        if (!EnsureInternalInitialization() || owner == null) // 내부 초기화와 관리자 확인
        {
            return false; // 초기화 실패 반환
        }

        controller = owner; // 일시정지 메뉴 관리자 저장

        if (!settingsPanel.Initialize(
            this,
            cameraFollow)) // 설정 페이지 런타임 초기화
        {
            Debug.LogError(
                "PauseSettingsPanel 초기화에 실패했습니다.",
                settingsPanel); // 설정 페이지 초기화 오류 출력

            return false; // 전체 일시정지 메뉴 초기화 실패 반환
        }

        ShowMainPage(); // 모든 자식 초기화 완료 후 메인 페이지 표시
        return true; // 초기화 성공 반환
    }

    private bool EnsureInternalInitialization() // PauseMenuView 내부 초기화 보장
    {
        if (internalInitializationCompleted) // 기존 초기화 실행 여부 확인
        {
            return internalReferencesValid; // 기존 초기화 결과 반환
        }

        internalInitializationCompleted = true; // 내부 초기화 실행 기록

        internalReferencesValid =
            panelRoot != null
            && rootCanvasGroup != null
            && drawerPanel != null
            && mainPageRoot != null
            && settingsPanel != null
            && resumeButton != null
            && settingsButton != null
            && saveButton != null
            && loadButton != null
            && mainMenuButton != null
            && quitButton != null
            && actionStatusText != null; // 필수 프리팹 내부 참조 상태 계산

        if (!internalReferencesValid) // 내부 참조 누락 확인
        {
            Debug.LogError(
                $"{gameObject.name}의 PauseMenuView 내부 참조를 모두 연결해야 합니다.",
                this); // 내부 참조 오류 출력

            enabled = false; // 잘못된 메뉴 기능 비활성화
            return false; // 내부 초기화 실패 반환
        }

        CaptureSlidePositions(); // Inspector의 열린 위치와 화면 밖 숨김 위치 계산
        ApplyHiddenVisual(); // 생성 직후 화면 밖 숨김 위치 적용
        RegisterButtonListeners(); // 일시정지 메뉴 버튼 이벤트 등록
        return true; // 내부 초기화 성공 반환
    }

    public void Show() // 좌측 일시정지 메뉴 열기
    {
        if (!EnsureInternalInitialization()) // 내부 초기화 상태 확인
        {
            return; // 화면 표시 중단
        }

        StopTransition(); // 이전 열기 또는 닫기 애니메이션 중단
        ShowMainPage(); // 메뉴를 열 때 메인 페이지부터 표시
        panelRoot.SetActive(true); // 전체 화면 루트 활성화
        RefreshHiddenPosition(); // 현재 패널 너비로 화면 밖 숨김 위치 갱신
        rootCanvasGroup.blocksRaycasts = true; // 배경 뒤 Gameplay 클릭 차단
        rootCanvasGroup.interactable = false; // 열기 애니메이션 중 버튼 입력 차단

        transitionRoutine = StartCoroutine(
            AnimateMenu(
                openAnchoredPosition,
                1f,
                true,
                null)); // 화면 왼쪽에서 안쪽으로 메뉴 열기
    }

    public void Hide(Action onHidden = null) // 좌측 일시정지 메뉴 닫기
    {
        if (!EnsureInternalInitialization()) // 내부 초기화 상태 확인
        {
            onHidden?.Invoke(); // 화면 없이 완료 콜백 실행
            return; // 화면 숨김 중단
        }

        StopTransition(); // 이전 열기 또는 닫기 애니메이션 중단
        rootCanvasGroup.interactable = false; // 닫기 애니메이션 중 버튼 입력 차단
        rootCanvasGroup.blocksRaycasts = true; // 닫기 완료 전 Gameplay 클릭 차단

        transitionRoutine = StartCoroutine(
            AnimateMenu(
                hiddenAnchoredPosition,
                0f,
                false,
                onHidden)); // 메뉴를 왼쪽 화면 밖으로 이동 후 숨김
    }

    public void HideImmediate() // 애니메이션 없이 일시정지 메뉴 즉시 숨김
    {
        if (!EnsureInternalInitialization()) // 내부 초기화 상태 확인
        {
            return; // 즉시 숨김 중단
        }

        StopTransition(); // 진행 중 애니메이션 중단
        ShowMainPage(); // 다음 표시를 위한 메인 페이지 복구
        RefreshHiddenPosition(); // 현재 패널 너비로 숨김 위치 다시 계산
        ApplyHiddenVisual(); // 완전히 닫힌 화면 상태 적용
        panelRoot.SetActive(false); // 전체 화면 루트 비활성화
    }

    public void ShowMainPage() // 일시정지 메인 페이지 표시
    {
        if (!EnsureInternalInitialization()) // 내부 초기화 상태 확인
        {
            return; // 페이지 변경 중단
        }

        settingsPanel.Hide(); // 설정 페이지 숨김
        mainPageRoot.SetActive(true); // 일시정지 메인 페이지 표시
        actionStatusText.SetText(string.Empty); // 이전 저장과 불러오기 문구 제거
        resumeButton.Select(); // 메인 페이지 기본 선택 버튼 지정
    }

    private void ShowSettingsPage() // 설정 페이지 표시
    {
        if (!EnsureInternalInitialization()) // 내부 초기화 상태 확인
        {
            return; // 페이지 변경 중단
        }

        mainPageRoot.SetActive(false); // 일시정지 메인 페이지 숨김
        settingsPanel.Show(); // 설정 페이지 표시
    }

    private IEnumerator AnimateMenu(
        Vector2 targetPosition,
        float targetAlpha,
        bool opening,
        Action onCompleted) // 실제 시간 기반 좌측 슬라이드와 배경 페이드
    {
        Vector2 startPosition =
            drawerPanel.anchoredPosition; // 애니메이션 시작 패널 위치 저장

        float startAlpha =
            rootCanvasGroup.alpha; // 애니메이션 시작 화면 투명도 저장

        float elapsed = 0f; // 애니메이션 진행 시간 초기화

        while (elapsed < transitionDuration) // 설정 시간 동안 애니메이션 반복
        {
            elapsed += Time.unscaledDeltaTime; // Time.timeScale 영향 없는 실제 시간 누적

            float normalizedTime =
                Mathf.Clamp01(
                    elapsed / transitionDuration); // 0부터 1까지 진행률 계산

            float easedTime =
                normalizedTime
                * normalizedTime
                * (3f - 2f * normalizedTime); // 부드러운 SmoothStep 보간 계산

            drawerPanel.anchoredPosition =
                Vector2.LerpUnclamped(
                    startPosition,
                    targetPosition,
                    easedTime); // 좌측 메뉴 위치 부드럽게 이동

            rootCanvasGroup.alpha =
                Mathf.LerpUnclamped(
                    startAlpha,
                    targetAlpha,
                    easedTime); // 화면 어두운 배경 부드럽게 표시

            yield return null; // 다음 프레임까지 대기
        }

        drawerPanel.anchoredPosition =
            targetPosition; // 최종 메뉴 위치 정확히 적용

        rootCanvasGroup.alpha =
            targetAlpha; // 최종 화면 투명도 정확히 적용

        transitionRoutine = null; // 애니메이션 진행 상태 해제

        if (opening) // 열기 애니메이션 완료 여부 확인
        {
            rootCanvasGroup.interactable = true; // 일시정지 메뉴 버튼 입력 허용
            rootCanvasGroup.blocksRaycasts = true; // 배경 뒤 Gameplay 클릭 차단 유지
            resumeButton.Select(); // 기본 선택 버튼 지정
        }
        else // 닫기 애니메이션 완료 처리
        {
            rootCanvasGroup.interactable = false; // 메뉴 버튼 입력 차단
            rootCanvasGroup.blocksRaycasts = false; // 전체 화면 클릭 차단 해제
            onCompleted?.Invoke(); // Gameplay 입력과 시간 복구 콜백 실행
            panelRoot.SetActive(false); // 콜백 완료 후 전체 일시정지 화면 비활성화
            yield break; // 닫기 애니메이션 코루틴 종료
        }

        onCompleted?.Invoke(); // 열기 애니메이션 완료 콜백 실행
    }

    private void CaptureSlidePositions() // Inspector에서 설정한 열린 위치 최초 저장
    {
        Canvas.ForceUpdateCanvases(); // RectTransform 실제 크기 즉시 갱신

        openAnchoredPosition =
            drawerPanel.anchoredPosition; // Inspector의 현재 위치를 열린 위치로 저장

        slidePositionsInitialized = true; // 열린 위치 저장 완료 기록
        RefreshHiddenPosition(); // 열린 위치 기준 화면 밖 숨김 위치 계산
    }

    private void RefreshHiddenPosition() // 저장된 열린 위치 기준 화면 밖 숨김 위치 갱신
    {
        if (!slidePositionsInitialized) // 열린 위치 저장 여부 확인
        {
            CaptureSlidePositions(); // 최초 열린 위치와 숨김 위치 계산
            return; // 중복 계산 방지
        }

        Canvas.ForceUpdateCanvases(); // 현재 해상도의 RectTransform 크기 갱신

        float panelWidth =
            Mathf.Max(
                drawerPanel.rect.width,
                drawerPanel.sizeDelta.x); // 실제 메뉴 패널 너비 계산

        if (panelWidth <= 0f) // 패널 너비 계산 실패 확인
        {
            panelWidth = 420f; // 기본 좌측 메뉴 너비 적용
        }

        hiddenAnchoredPosition =
            openAnchoredPosition
            + Vector2.left
            * (panelWidth + hiddenPadding); // 메뉴 전체가 왼쪽 화면 밖에 있도록 숨김 위치 계산
    }

    private void ApplyHiddenVisual() // 완전히 닫힌 시각 상태 적용
    {
        drawerPanel.anchoredPosition =
            hiddenAnchoredPosition; // 좌측 화면 밖 위치 적용

        rootCanvasGroup.alpha = 0f; // 전체 화면 투명 처리
        rootCanvasGroup.interactable = false; // 버튼 입력 차단
        rootCanvasGroup.blocksRaycasts = false; // Gameplay 클릭 차단 해제
    }

    private void StopTransition() // 현재 슬라이드 애니메이션 중단
    {
        if (transitionRoutine == null) // 실행 중 코루틴 존재 확인
        {
            return; // 중단할 애니메이션 없음
        }

        StopCoroutine(transitionRoutine); // 현재 슬라이드 코루틴 중단
        transitionRoutine = null; // 애니메이션 상태 초기화
    }

    private void RegisterButtonListeners() // 버튼 클릭 이벤트 등록
    {
        if (listenersRegistered || !internalReferencesValid) // 기존 등록과 내부 참조 확인
        {
            return; // 중복 이벤트 등록 방지
        }

        resumeButton.onClick.AddListener(
            OnResumeButtonClicked); // RESUME 버튼 이벤트 등록

        settingsButton.onClick.AddListener(
            OnSettingsButtonClicked); // SETTINGS 버튼 이벤트 등록

        saveButton.onClick.AddListener(
            OnSaveButtonClicked); // SAVE GAME 버튼 이벤트 등록

        loadButton.onClick.AddListener(
            OnLoadButtonClicked); // LOAD GAME 버튼 이벤트 등록

        mainMenuButton.onClick.AddListener(
            OnMainMenuButtonClicked); // MAIN MENU 버튼 이벤트 등록

        quitButton.onClick.AddListener(
            OnQuitButtonClicked); // QUIT GAME 버튼 이벤트 등록

        listenersRegistered = true; // 버튼 이벤트 등록 완료 기록
    }

    private void RemoveButtonListeners() // 버튼 클릭 이벤트 제거
    {
        if (!listenersRegistered) // 이벤트 등록 여부 확인
        {
            return; // 제거할 이벤트 없음
        }

        if (resumeButton != null) // RESUME 버튼 존재 확인
        {
            resumeButton.onClick.RemoveListener(
                OnResumeButtonClicked); // RESUME 버튼 이벤트 제거
        }

        if (settingsButton != null) // SETTINGS 버튼 존재 확인
        {
            settingsButton.onClick.RemoveListener(
                OnSettingsButtonClicked); // SETTINGS 버튼 이벤트 제거
        }

        if (saveButton != null) // SAVE GAME 버튼 존재 확인
        {
            saveButton.onClick.RemoveListener(
                OnSaveButtonClicked); // SAVE GAME 버튼 이벤트 제거
        }

        if (loadButton != null) // LOAD GAME 버튼 존재 확인
        {
            loadButton.onClick.RemoveListener(
                OnLoadButtonClicked); // LOAD GAME 버튼 이벤트 제거
        }

        if (mainMenuButton != null) // MAIN MENU 버튼 존재 확인
        {
            mainMenuButton.onClick.RemoveListener(
                OnMainMenuButtonClicked); // MAIN MENU 버튼 이벤트 제거
        }

        if (quitButton != null) // QUIT 버튼 존재 확인
        {
            quitButton.onClick.RemoveListener(
                OnQuitButtonClicked); // QUIT 버튼 이벤트 제거
        }

        listenersRegistered = false; // 버튼 이벤트 등록 상태 초기화
    }

    private void OnResumeButtonClicked() // RESUME 버튼 클릭 처리
    {
        if (controller == null) // 일시정지 관리자 존재 확인
        {
            return; // 버튼 처리 중단
        }

        controller.ClosePauseMenu(); // 좌측 슬라이드 닫기 후 Gameplay 복구
    }

    private void OnSettingsButtonClicked() // SETTINGS 버튼 클릭 처리
    {
        ShowSettingsPage(); // 일시정지 설정 페이지 표시
    }

    private void OnSaveButtonClicked() // SAVE GAME 버튼 클릭 처리
    {
        if (controller == null) // 일시정지 관리자 존재 확인
        {
            return; // 저장 처리 중단
        }

        actionStatusText.SetText(
            "SAVING..."); // 저장 요청 문구 표시

        controller.SaveCurrentGameFromPause(); // 기존 Gameplay 저장 기능 실행

        actionStatusText.SetText(
            "SAVE COMMAND FINISHED\nCHECK CONSOLE FOR RESULT"); // 저장 결과 확인 안내
    }

    private void OnLoadButtonClicked() // LOAD GAME 버튼 클릭 처리
    {
        if (controller == null) // 일시정지 관리자 존재 확인
        {
            return; // 불러오기 처리 중단
        }

        actionStatusText.SetText(
            "LOADING..."); // 불러오기 요청 문구 표시

        controller.LoadCurrentGameFromPause(); // 기존 Gameplay 불러오기 기능 실행

        actionStatusText.SetText(
            "LOAD COMMAND FINISHED\nCHECK CONSOLE FOR RESULT"); // 불러오기 결과 확인 안내
    }

    private void OnMainMenuButtonClicked() // MAIN MENU 버튼 클릭 처리
    {
        if (controller == null) // 일시정지 관리자 존재 확인
        {
            return; // 버튼 처리 중단
        }

        controller.ReturnToMainMenu(); // 메인 메뉴 Scene 이동 요청
    }

    private void OnQuitButtonClicked() // QUIT 버튼 클릭 처리
    {
        if (controller == null) // 일시정지 관리자 존재 확인
        {
            return; // 버튼 처리 중단
        }

        controller.QuitGame(); // 게임 종료 요청
    }

    private void OnDestroy() // 일시정지 메뉴 제거 정리
    {
        StopTransition(); // 실행 중 슬라이드 애니메이션 중단
        RemoveButtonListeners(); // 버튼 이벤트 제거
    }
}
