using System.Collections.Generic; // 목록
using UnityEngine; // Unity 기본 기능

public struct GameSettingsData // 99일차: 설정 한 벌 (설정 창 · 저장 · 적용에서 함께 사용)
{
    public float MasterVolume; // 마스터 볼륨 (0~1)
    public float MouseSensitivity; // 마우스 감도
    public bool Fullscreen; // 전체 화면 여부
    public int Width; // 해상도 가로
    public int Height; // 해상도 세로
    public int QualityLevel; // 그래픽 품질 단계
    public bool VSync; // 수직 동기화
    public int FrameLimit; // 프레임 제한 (0 = 제한 없음)
}

public static class GameSettingsService // 게임 설정 저장과 적용 기능 / 99일차: 해상도 · 그래픽 품질 · 수직 동기화 · 프레임 제한 추가
{
    private const string MasterVolumeKey = "ProjectU.MasterVolume"; // 마스터 볼륨 저장 키
    private const string MouseSensitivityKey = "ProjectU.MouseSensitivity"; // 마우스 감도 저장 키
    private const string FullscreenKey = "ProjectU.Fullscreen"; // 전체 화면 저장 키
    private const string ResolutionWidthKey = "ProjectU.ResolutionWidth"; // 해상도 가로 저장 키
    private const string ResolutionHeightKey = "ProjectU.ResolutionHeight"; // 해상도 세로 저장 키
    private const string QualityKey = "ProjectU.QualityLevel"; // 그래픽 품질 저장 키
    private const string VSyncKey = "ProjectU.VSync"; // 수직 동기화 저장 키
    private const string FrameLimitKey = "ProjectU.FrameLimit"; // 프레임 제한 저장 키

    public const float DefaultMasterVolume = 1f; // 기본 마스터 볼륨
    public const float DefaultMouseSensitivity = 0.1f; // 기본 마우스 감도
    public const float MinimumMouseSensitivity = 0.02f; // 최소 마우스 감도
    public const float MaximumMouseSensitivity = 0.5f; // 최대 마우스 감도
    public const int DefaultFrameLimit = 60; // 기본 프레임 제한
    public static readonly int[] FrameLimits = { 30, 60, 120, 144, 0 }; // 고를 수 있는 프레임 제한 (0 = 제한 없음)
    private const int MinimumResolutionWidth = 1280; // 목록에 넣는 가장 작은 가로 해상도

    public static float MasterVolume =>
        Mathf.Clamp01(
            PlayerPrefs.GetFloat(
                MasterVolumeKey,
                DefaultMasterVolume)); // 저장된 마스터 볼륨 제공

    public static float MouseSensitivity =>
        Mathf.Clamp(
            PlayerPrefs.GetFloat(
                MouseSensitivityKey,
                DefaultMouseSensitivity),
            MinimumMouseSensitivity,
            MaximumMouseSensitivity); // 저장된 마우스 감도 제공

    public static bool Fullscreen =>
        PlayerPrefs.GetInt(
            FullscreenKey,
            Screen.fullScreen ? 1 : 0) == 1; // 저장된 전체 화면 여부 제공

    public static GameSettingsData Defaults // 99일차: 기본값 (설정 창의 기본값 버튼)
    {
        get
        {
            Vector2Int native = NativeResolution();
            return new GameSettingsData
            {
                MasterVolume = DefaultMasterVolume, MouseSensitivity = DefaultMouseSensitivity, Fullscreen = true,
                Width = native.x, Height = native.y, QualityLevel = Mathf.Max(0, QualitySettings.names.Length - 1),
                VSync = true, FrameLimit = DefaultFrameLimit
            };
        }
    }

    public static GameSettingsData Load() // 99일차: 저장된 설정 한 벌 (없는 값은 지금 상태)
    {
        Vector2Int current = new Vector2Int(Screen.width, Screen.height);
        return new GameSettingsData
        {
            MasterVolume = MasterVolume,
            MouseSensitivity = MouseSensitivity,
            Fullscreen = Fullscreen,
            Width = PlayerPrefs.GetInt(ResolutionWidthKey, current.x),
            Height = PlayerPrefs.GetInt(ResolutionHeightKey, current.y),
            QualityLevel = Mathf.Clamp(PlayerPrefs.GetInt(QualityKey, QualitySettings.GetQualityLevel()), 0, Mathf.Max(0, QualitySettings.names.Length - 1)),
            VSync = PlayerPrefs.GetInt(VSyncKey, QualitySettings.vSyncCount > 0 ? 1 : 0) == 1,
            FrameLimit = PlayerPrefs.GetInt(FrameLimitKey, DefaultFrameLimit)
        };
    }

    public static IReadOnlyList<Vector2Int> AvailableResolutions // 99일차: 고를 수 있는 해상도 (가로 1280 이상, 같은 크기는 하나)
    {
        get
        {
            List<Vector2Int> result = new List<Vector2Int>();

            foreach (Resolution resolution in Screen.resolutions)
            {
                Vector2Int size = new Vector2Int(resolution.width, resolution.height);

                if (size.x >= MinimumResolutionWidth && !result.Contains(size))
                {
                    result.Add(size);
                }
            }

            Vector2Int current = new Vector2Int(Screen.width, Screen.height);

            if (!result.Contains(current)) // 목록에 없는 지금 크기(창 모드 · 에디터)도 고를 수 있게
            {
                result.Add(current);
            }

            result.Sort((left, right) => left.x != right.x ? left.x.CompareTo(right.x) : left.y.CompareTo(right.y));
            return result;
        }
    }

    public static string FrameLimitLabel(int frameLimit) => frameLimit <= 0 ? "UNLIMITED" : $"{frameLimit} FPS"; // 프레임 제한 표시

    private static Vector2Int NativeResolution()
    {
        Resolution native = Screen.currentResolution;
        return native.width > 0 ? new Vector2Int(native.width, native.height) : new Vector2Int(Screen.width, Screen.height);
    }

    public static void ApplyStoredSettings(
        ThirdPersonCameraFollow cameraFollow) // 저장된 설정을 현재 게임에 적용
    {
        Apply(Load(), cameraFollow); // 99일차: 설정 한 벌 적용
    }

    public static void SaveAndApply(
        float masterVolume,
        float mouseSensitivity,
        bool fullscreen,
        ThirdPersonCameraFollow cameraFollow) // 설정 저장과 즉시 적용 (기존 일시정지 설정 화면)
    {
        GameSettingsData data = Load(); // 나머지 설정은 그대로
        data.MasterVolume = masterVolume;
        data.MouseSensitivity = mouseSensitivity;
        data.Fullscreen = fullscreen;
        SaveAndApply(data, cameraFollow); // 저장 · 적용
    }

    public static void SaveAndApply(GameSettingsData data, ThirdPersonCameraFollow cameraFollow) // 99일차: 설정 한 벌 저장과 즉시 적용
    {
        data = Sanitize(data); // 범위 보정
        PlayerPrefs.SetFloat(MasterVolumeKey, data.MasterVolume); // 마스터 볼륨 저장
        PlayerPrefs.SetFloat(MouseSensitivityKey, data.MouseSensitivity); // 마우스 감도 저장
        PlayerPrefs.SetInt(FullscreenKey, data.Fullscreen ? 1 : 0); // 전체 화면 여부 저장
        PlayerPrefs.SetInt(ResolutionWidthKey, data.Width); // 해상도 저장
        PlayerPrefs.SetInt(ResolutionHeightKey, data.Height);
        PlayerPrefs.SetInt(QualityKey, data.QualityLevel); // 그래픽 품질 저장
        PlayerPrefs.SetInt(VSyncKey, data.VSync ? 1 : 0); // 수직 동기화 저장
        PlayerPrefs.SetInt(FrameLimitKey, data.FrameLimit); // 프레임 제한 저장
        PlayerPrefs.Save(); // 설정 파일 즉시 저장
        Apply(data, cameraFollow); // 즉시 적용
    }

    public static GameSettingsData Sanitize(GameSettingsData data) // 99일차: 잘못된 값 보정
    {
        data.MasterVolume = Mathf.Clamp01(data.MasterVolume);
        data.MouseSensitivity = Mathf.Clamp(data.MouseSensitivity, MinimumMouseSensitivity, MaximumMouseSensitivity);
        data.QualityLevel = Mathf.Clamp(data.QualityLevel, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
        data.FrameLimit = System.Array.IndexOf(FrameLimits, data.FrameLimit) >= 0 ? data.FrameLimit : DefaultFrameLimit;

        if (data.Width < 640 || data.Height < 360) // 너무 작은 해상도는 지금 화면 크기로
        {
            data.Width = Screen.width;
            data.Height = Screen.height;
        }

        return data;
    }

    public static void Apply(GameSettingsData data, ThirdPersonCameraFollow cameraFollow) // 99일차: 설정 한 벌 적용
    {
        data = Sanitize(data);
        AudioListener.volume = data.MasterVolume; // 전체 Unity 오디오 볼륨 적용

        if (cameraFollow != null) // 게임 Scene에서만 마우스 감도 적용
        {
            cameraFollow.SetMouseSensitivity(data.MouseSensitivity); // 추적 카메라 감도 적용
        }

        if (QualitySettings.GetQualityLevel() != data.QualityLevel) // 그래픽 품질 변경
        {
            QualitySettings.SetQualityLevel(data.QualityLevel, true);
        }

        QualitySettings.vSyncCount = data.VSync ? 1 : 0; // 수직 동기화
        Application.targetFrameRate = data.VSync || data.FrameLimit <= 0 ? -1 : data.FrameLimit; // 수직 동기화가 켜져 있으면 모니터에 맞춘다

        FullScreenMode mode = data.Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed; // 전체 화면(테두리 없는 창) · 창 모드

        if (!Application.isEditor && (Screen.width != data.Width || Screen.height != data.Height || Screen.fullScreenMode != mode)) // 에디터 Game 창은 크기를 바꾸지 않는다
        {
            Screen.SetResolution(data.Width, data.Height, mode);
        }
    }
}
