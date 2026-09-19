using System; // 이벤트
using System.Collections.Generic; // 목록
using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class SettingsWindowUI : MonoBehaviour // 99일차: 메인 메뉴 · 일시정지 메뉴 공용 설정 창
{
    [Serializable]
    public sealed class CycleRow // 왼쪽 · 오른쪽 버튼으로 값을 고르는 줄
    {
        public Button previous; // 이전 값
        public Button next; // 다음 값
        public TMP_Text value; // 현재 값 표시
    }

    [Header("Root")]
    [SerializeField] private GameObject panelRoot; // 창 전체 (어두운 배경 포함)

    [Header("Display")]
    [SerializeField] private CycleRow displayModeRow = new CycleRow(); // 화면 모드
    [SerializeField] private CycleRow resolutionRow = new CycleRow(); // 해상도
    [SerializeField] private CycleRow qualityRow = new CycleRow(); // 그래픽 품질
    [SerializeField] private CycleRow vSyncRow = new CycleRow(); // 수직 동기화
    [SerializeField] private CycleRow frameLimitRow = new CycleRow(); // 프레임 제한

    [Header("Audio · Control")]
    [SerializeField] private Slider volumeSlider; // 마스터 볼륨
    [SerializeField] private TMP_Text volumeValue; // 볼륨 값
    [SerializeField] private Slider sensitivitySlider; // 마우스 감도
    [SerializeField] private TMP_Text sensitivityValue; // 감도 값

    [Header("Buttons")]
    [SerializeField] private Button defaultsButton; // 기본값
    [SerializeField] private Button applyButton; // 적용
    [SerializeField] private Button closeButton; // 닫기
    [SerializeField] private TMP_Text statusText; // 적용 결과

    private GameSettingsData pending; // 아직 적용하지 않은 값
    private List<Vector2Int> resolutions = new List<Vector2Int>(); // 고를 수 있는 해상도
    private ThirdPersonCameraFollow cameraFollow; // 게임 Scene의 카메라 (마우스 감도 적용)
    private bool listenersRegistered; // 버튼 연결 여부
    private bool refreshing; // 값을 채우는 중 (슬라이더 이벤트 무시)

    public event Action Closed; // 창이 닫힘

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf; // 열림 여부
    public GameSettingsData Pending => pending; // 적용 전 값 (테스트용)
    public string StatusLabel => statusText != null ? statusText.text : string.Empty; // 결과 문구 (테스트용)

    private void Awake() // 버튼 연결 · 처음에는 숨김
    {
        RegisterListeners();

        if (panelRoot != null && !IsOpen)
        {
            panelRoot.SetActive(false);
        }
    }

    private void RegisterListeners()
    {
        if (listenersRegistered)
        {
            return;
        }

        listenersRegistered = true;
        Bind(displayModeRow, () => pending.Fullscreen = !pending.Fullscreen, () => pending.Fullscreen = !pending.Fullscreen);
        Bind(resolutionRow, () => StepResolution(-1), () => StepResolution(1));
        Bind(qualityRow, () => pending.QualityLevel = Wrap(pending.QualityLevel - 1, QualitySettings.names.Length), () => pending.QualityLevel = Wrap(pending.QualityLevel + 1, QualitySettings.names.Length));
        Bind(vSyncRow, () => pending.VSync = !pending.VSync, () => pending.VSync = !pending.VSync);
        Bind(frameLimitRow, () => StepFrameLimit(-1), () => StepFrameLimit(1));

        if (volumeSlider != null) volumeSlider.onValueChanged.AddListener(value => { if (!refreshing) { pending.MasterVolume = value; RefreshLabels(); } });
        if (sensitivitySlider != null) sensitivitySlider.onValueChanged.AddListener(value => { if (!refreshing) { pending.MouseSensitivity = value; RefreshLabels(); } });
        if (defaultsButton != null) defaultsButton.onClick.AddListener(ResetToDefaults);
        if (applyButton != null) applyButton.onClick.AddListener(Apply);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
    }

    private void Bind(CycleRow row, Action previous, Action next) // 줄 버튼 연결
    {
        if (row == null)
        {
            return;
        }

        if (row.previous != null) row.previous.onClick.AddListener(() => { previous(); RefreshLabels(); });
        if (row.next != null) row.next.onClick.AddListener(() => { next(); RefreshLabels(); });
    }

    private static int Wrap(int value, int count) => count <= 0 ? 0 : ((value % count) + count) % count;

    public void Show(ThirdPersonCameraFollow targetCamera) // 창 열기 (저장된 값으로 채움)
    {
        RegisterListeners();
        cameraFollow = targetCamera;
        pending = GameSettingsService.Load();
        resolutions = new List<Vector2Int>(GameSettingsService.AvailableResolutions);
        SnapResolution();
        RefreshAll();
        SetStatus(string.Empty, ProjectUUIPalette.TextSecondary);

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }
    }

    public void Close() // 창 닫기 (적용하지 않은 값은 버림)
    {
        if (panelRoot != null && panelRoot.activeSelf)
        {
            panelRoot.SetActive(false);
            Closed?.Invoke();
        }
    }

    public void Apply() // 저장 · 적용
    {
        GameSettingsService.SaveAndApply(pending, cameraFollow);
        pending = GameSettingsService.Load();
        SnapResolution();
        RefreshAll();
        SetStatus("SETTINGS SAVED", ProjectUUIPalette.Teal);
    }

    public void ResetToDefaults() // 기본값으로 (적용 버튼을 눌러야 저장)
    {
        pending = GameSettingsService.Defaults;
        SnapResolution();
        RefreshAll();
        SetStatus("DEFAULTS LOADED - PRESS APPLY", ProjectUUIPalette.Accent);
    }

    // 테스트 · 키보드용 : 줄 번호(0 화면 모드 · 1 해상도 · 2 품질 · 3 수직 동기화 · 4 프레임 제한)를 한 칸 넘긴다
    public void StepRow(int row, int direction)
    {
        CycleRow target = row == 0 ? displayModeRow : row == 1 ? resolutionRow : row == 2 ? qualityRow : row == 3 ? vSyncRow : frameLimitRow;
        Button button = direction < 0 ? target.previous : target.next;

        if (button != null)
        {
            button.onClick.Invoke();
        }
    }

    public void SetVolume(float value) // 테스트용 : 슬라이더 움직이기
    {
        if (volumeSlider != null)
        {
            volumeSlider.value = value;
        }
    }

    private void StepResolution(int direction)
    {
        if (resolutions.Count == 0)
        {
            return;
        }

        int index = resolutions.IndexOf(new Vector2Int(pending.Width, pending.Height));
        index = Wrap((index < 0 ? 0 : index) + direction, resolutions.Count);
        pending.Width = resolutions[index].x;
        pending.Height = resolutions[index].y;
    }

    private void StepFrameLimit(int direction)
    {
        int index = Array.IndexOf(GameSettingsService.FrameLimits, pending.FrameLimit);
        pending.FrameLimit = GameSettingsService.FrameLimits[Wrap((index < 0 ? 1 : index) + direction, GameSettingsService.FrameLimits.Length)];
    }

    private void SnapResolution() // 목록에 없는 해상도는 가장 가까운 것으로
    {
        Vector2Int wanted = new Vector2Int(pending.Width, pending.Height);

        if (resolutions.Count == 0 || resolutions.Contains(wanted))
        {
            return;
        }

        Vector2Int best = resolutions[0];

        foreach (Vector2Int candidate in resolutions)
        {
            if (Mathf.Abs(candidate.x - wanted.x) + Mathf.Abs(candidate.y - wanted.y) < Mathf.Abs(best.x - wanted.x) + Mathf.Abs(best.y - wanted.y))
            {
                best = candidate;
            }
        }

        pending.Width = best.x;
        pending.Height = best.y;
    }

    private void RefreshAll() // 슬라이더 범위 · 값 · 문구
    {
        refreshing = true;

        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.value = pending.MasterVolume;
        }

        if (sensitivitySlider != null)
        {
            sensitivitySlider.minValue = GameSettingsService.MinimumMouseSensitivity;
            sensitivitySlider.maxValue = GameSettingsService.MaximumMouseSensitivity;
            sensitivitySlider.value = pending.MouseSensitivity;
        }

        refreshing = false;
        RefreshLabels();
    }

    private void RefreshLabels() // 줄마다 현재 값 표시
    {
        SetValue(displayModeRow, pending.Fullscreen ? "FULLSCREEN" : "WINDOWED");
        SetValue(resolutionRow, $"{pending.Width} x {pending.Height}");
        SetValue(qualityRow, QualitySettings.names.Length > 0 ? QualitySettings.names[Mathf.Clamp(pending.QualityLevel, 0, QualitySettings.names.Length - 1)].ToUpperInvariant() : "-");
        SetValue(vSyncRow, pending.VSync ? "ON" : "OFF");
        SetValue(frameLimitRow, pending.VSync ? "VSYNC" : GameSettingsService.FrameLimitLabel(pending.FrameLimit));

        if (volumeValue != null) volumeValue.text = $"{Mathf.RoundToInt(pending.MasterVolume * 100f)}%";
        if (sensitivityValue != null) sensitivityValue.text = pending.MouseSensitivity.ToString("0.00");
    }

    private static void SetValue(CycleRow row, string value)
    {
        if (row != null && row.value != null)
        {
            row.value.text = value;
        }
    }

    private void SetStatus(string message, Color color)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = color;
        }
    }

    public string RowValue(int row) // 테스트용 : 줄 값
    {
        CycleRow target = row == 0 ? displayModeRow : row == 1 ? resolutionRow : row == 2 ? qualityRow : row == 3 ? vSyncRow : frameLimitRow;
        return target.value != null ? target.value.text : string.Empty;
    }

#if UNITY_EDITOR
    public void EditorAssign(GameObject root, CycleRow[] rows, Slider volume, TMP_Text volumeText, Slider sensitivity, TMP_Text sensitivityText, Button defaults, Button apply, Button close, TMP_Text status) // 생성 도구 전용
    {
        panelRoot = root;
        displayModeRow = rows[0];
        resolutionRow = rows[1];
        qualityRow = rows[2];
        vSyncRow = rows[3];
        frameLimitRow = rows[4];
        volumeSlider = volume;
        volumeValue = volumeText;
        sensitivitySlider = sensitivity;
        sensitivityValue = sensitivityText;
        defaultsButton = defaults;
        applyButton = apply;
        closeButton = close;
        statusText = status;
    }
#endif
}
