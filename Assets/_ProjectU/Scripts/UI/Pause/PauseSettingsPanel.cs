using TMPro; // TextMeshPro UI 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Slider, Toggle과 Button 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class PauseSettingsPanel : MonoBehaviour // 일시정지 메뉴 설정 화면
{
    [Header("Panel")] // 설정 화면 루트 묶음
    [Tooltip("설정 항목 전체를 표시하거나 숨길 SettingsPage 루트입니다.")]
    [SerializeField] private GameObject panelRoot; // 설정 화면 전체 루트

    [Header("Master Volume")] // 마스터 볼륨 UI 묶음
    [Tooltip("전체 게임 오디오 볼륨을 0부터 1까지 조절하는 Slider입니다.")]
    [SerializeField] private Slider masterVolumeSlider; // 마스터 볼륨 Slider

    [Tooltip("현재 마스터 볼륨을 백분율로 표시하는 Text입니다.")]
    [SerializeField] private TMP_Text masterVolumeValueText; // 마스터 볼륨 값 Text

    [Header("Mouse Sensitivity")] // 마우스 감도 UI 묶음
    [Tooltip("플레이어 카메라의 마우스 회전 감도를 조절하는 Slider입니다.")]
    [SerializeField] private Slider mouseSensitivitySlider; // 마우스 감도 Slider

    [Tooltip("현재 마우스 감도 수치를 표시하는 Text입니다.")]
    [SerializeField] private TMP_Text mouseSensitivityValueText; // 마우스 감도 값 Text

    [Header("Display")] // 화면 설정 UI 묶음
    [Tooltip("전체 화면 사용 여부를 선택하는 Toggle입니다.")]
    [SerializeField] private Toggle fullscreenToggle; // 전체 화면 Toggle

    [Header("Buttons")] // 설정 화면 버튼 묶음
    [Tooltip("현재 UI 값을 저장하고 게임에 적용하는 버튼입니다.")]
    [SerializeField] private Button applyButton; // 설정 적용 버튼

    [Tooltip("설정을 적용하지 않고 일시정지 메인 화면으로 돌아가는 버튼입니다.")]
    [SerializeField] private Button backButton; // 설정 뒤로가기 버튼

    [Header("Feedback")] // 설정 결과 표시 묶음
    [Tooltip("설정 적용 결과를 표시하는 Text입니다.")]
    [SerializeField] private TMP_Text statusText; // 설정 적용 결과 Text

    private PauseMenuView pauseMenuView; // 상위 일시정지 메뉴 화면
    private ThirdPersonCameraFollow cameraFollow; // 마우스 감도 적용 카메라
    private bool internalReferencesValid; // 내부 UI 참조 상태
    private bool internalInitializationCompleted; // 내부 초기화 실행 완료 여부
    private bool listenersRegistered; // UI 이벤트 등록 여부

    public bool IsVisible =>
        panelRoot != null
        && panelRoot.activeSelf; // 설정 화면 표시 여부 제공

    private void Awake() // 설정 화면 내부 초기화
    {
        EnsureInternalInitialization(); // 내부 참조와 UI 이벤트 초기화
    }

    public bool Initialize(
        PauseMenuView owner,
        ThirdPersonCameraFollow targetCameraFollow) // 상위 메뉴와 카메라 연결
    {
        if (!EnsureInternalInitialization() || owner == null) // 내부 초기화와 상위 메뉴 확인
        {
            return false; // 초기화 실패 반환
        }

        pauseMenuView = owner; // 상위 일시정지 메뉴 저장
        cameraFollow = targetCameraFollow; // 마우스 감도 적용 카메라 저장
        RefreshFromStoredSettings(); // 저장된 설정값 UI 반영
        panelRoot.SetActive(false); // 모든 초기화 완료 후 설정 화면 숨김
        return true; // 초기화 성공 반환
    }

    private bool EnsureInternalInitialization() // PauseSettingsPanel 내부 초기화 보장
    {
        if (internalInitializationCompleted) // 기존 초기화 실행 여부 확인
        {
            return internalReferencesValid; // 기존 초기화 결과 반환
        }

        internalInitializationCompleted = true; // 내부 초기화 실행 기록

        internalReferencesValid =
            panelRoot != null
            && masterVolumeSlider != null
            && masterVolumeValueText != null
            && mouseSensitivitySlider != null
            && mouseSensitivityValueText != null
            && fullscreenToggle != null
            && applyButton != null
            && backButton != null
            && statusText != null; // 필수 내부 참조 상태 계산

        if (!internalReferencesValid) // 내부 참조 누락 확인
        {
            Debug.LogError(
                $"{gameObject.name}의 PauseSettingsPanel 내부 참조를 모두 연결해야 합니다.",
                this); // 내부 참조 오류 출력

            enabled = false; // 설정 화면 기능 비활성화
            return false; // 내부 초기화 실패 반환
        }

        ConfigureControlRanges(); // Slider 범위 설정
        RegisterListeners(); // UI 이벤트 등록
        return true; // 내부 초기화 성공 반환
    }

    public void Show() // 설정 화면 표시
    {
        if (!EnsureInternalInitialization()) // 내부 초기화 상태 확인
        {
            return; // 설정 화면 표시 중단
        }

        RefreshFromStoredSettings(); // 저장된 설정값 다시 불러오기
        statusText.SetText(string.Empty); // 이전 적용 결과 제거
        panelRoot.SetActive(true); // 설정 화면 표시
        masterVolumeSlider.Select(); // 첫 설정 항목 기본 선택
    }

    public void Hide() // 설정 화면 숨김
    {
        if (panelRoot == null) // 설정 화면 루트 존재 확인
        {
            return; // 설정 화면 숨김 중단
        }

        panelRoot.SetActive(false); // 설정 화면 숨김
    }

    private void ConfigureControlRanges() // 설정 Slider 범위 구성
    {
        masterVolumeSlider.minValue = 0f; // 마스터 볼륨 최소값 설정
        masterVolumeSlider.maxValue = 1f; // 마스터 볼륨 최대값 설정
        masterVolumeSlider.wholeNumbers = false; // 마스터 볼륨 소수값 허용

        mouseSensitivitySlider.minValue =
            GameSettingsService.MinimumMouseSensitivity; // 마우스 감도 최소값 설정

        mouseSensitivitySlider.maxValue =
            GameSettingsService.MaximumMouseSensitivity; // 마우스 감도 최대값 설정

        mouseSensitivitySlider.wholeNumbers = false; // 마우스 감도 소수값 허용
    }

    private void RefreshFromStoredSettings() // 저장된 설정값 UI 반영
    {
        masterVolumeSlider.SetValueWithoutNotify(
            GameSettingsService.MasterVolume); // 저장된 마스터 볼륨 적용

        mouseSensitivitySlider.SetValueWithoutNotify(
            GameSettingsService.MouseSensitivity); // 저장된 마우스 감도 적용

        fullscreenToggle.SetIsOnWithoutNotify(
            GameSettingsService.Fullscreen); // 저장된 전체 화면 여부 적용

        RefreshMasterVolumeText(
            masterVolumeSlider.value); // 마스터 볼륨 문구 갱신

        RefreshMouseSensitivityText(
            mouseSensitivitySlider.value); // 마우스 감도 문구 갱신
    }

    private void RegisterListeners() // 설정 UI 이벤트 등록
    {
        if (listenersRegistered || !internalReferencesValid) // 기존 등록과 내부 참조 확인
        {
            return; // 중복 이벤트 등록 방지
        }

        masterVolumeSlider.onValueChanged.AddListener(
            OnMasterVolumeChanged); // 마스터 볼륨 값 변경 이벤트 등록

        mouseSensitivitySlider.onValueChanged.AddListener(
            OnMouseSensitivityChanged); // 마우스 감도 값 변경 이벤트 등록

        applyButton.onClick.AddListener(
            OnApplyButtonClicked); // APPLY 버튼 이벤트 등록

        backButton.onClick.AddListener(
            OnBackButtonClicked); // BACK 버튼 이벤트 등록

        listenersRegistered = true; // 이벤트 등록 완료 기록
    }

    private void RemoveListeners() // 설정 UI 이벤트 제거
    {
        if (!listenersRegistered) // 이벤트 등록 여부 확인
        {
            return; // 제거할 이벤트 없음
        }

        if (masterVolumeSlider != null) // 마스터 볼륨 Slider 존재 확인
        {
            masterVolumeSlider.onValueChanged.RemoveListener(
                OnMasterVolumeChanged); // 마스터 볼륨 이벤트 제거
        }

        if (mouseSensitivitySlider != null) // 마우스 감도 Slider 존재 확인
        {
            mouseSensitivitySlider.onValueChanged.RemoveListener(
                OnMouseSensitivityChanged); // 마우스 감도 이벤트 제거
        }

        if (applyButton != null) // APPLY 버튼 존재 확인
        {
            applyButton.onClick.RemoveListener(
                OnApplyButtonClicked); // APPLY 버튼 이벤트 제거
        }

        if (backButton != null) // BACK 버튼 존재 확인
        {
            backButton.onClick.RemoveListener(
                OnBackButtonClicked); // BACK 버튼 이벤트 제거
        }

        listenersRegistered = false; // 이벤트 등록 상태 초기화
    }

    private void OnMasterVolumeChanged(
        float value) // 마스터 볼륨 Slider 변경 처리
    {
        RefreshMasterVolumeText(value); // 마스터 볼륨 문구 갱신
    }

    private void OnMouseSensitivityChanged(
        float value) // 마우스 감도 Slider 변경 처리
    {
        RefreshMouseSensitivityText(value); // 마우스 감도 문구 갱신
    }

    private void RefreshMasterVolumeText(
        float value) // 마스터 볼륨 백분율 문구 갱신
    {
        int percentage =
            Mathf.RoundToInt(
                Mathf.Clamp01(value) * 100f); // 0부터 100 사이 백분율 계산

        masterVolumeValueText.SetText(
            $"{percentage}%"); // 마스터 볼륨 문구 표시
    }

    private void RefreshMouseSensitivityText(
        float value) // 마우스 감도 문구 갱신
    {
        mouseSensitivityValueText.SetText(
            value.ToString("0.00")); // 소수점 두 자리 감도 표시
    }

    private void OnApplyButtonClicked() // APPLY 버튼 클릭 처리
    {
        GameSettingsService.SaveAndApply(
            masterVolumeSlider.value,
            mouseSensitivitySlider.value,
            fullscreenToggle.isOn,
            cameraFollow); // 현재 설정 저장과 즉시 적용

        statusText.SetText(
            "SETTINGS APPLIED"); // 설정 적용 완료 문구 표시
    }

    private void OnBackButtonClicked() // BACK 버튼 클릭 처리
    {
        if (pauseMenuView == null) // 상위 일시정지 메뉴 존재 확인
        {
            Debug.LogError(
                "PauseSettingsPanel에 PauseMenuView가 연결되지 않았습니다.",
                this); // 상위 메뉴 누락 오류 출력

            return; // 뒤로가기 처리 중단
        }

        pauseMenuView.ShowMainPage(); // 일시정지 메인 화면으로 복귀
    }

    private void OnDestroy() // 설정 화면 제거 정리
    {
        RemoveListeners(); // 설정 UI 이벤트 제거
    }
}
