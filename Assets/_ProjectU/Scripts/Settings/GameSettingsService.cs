using UnityEngine; // Unity 기본 기능

public static class GameSettingsService // 게임 설정 저장과 적용 기능
{
    private const string MasterVolumeKey = "ProjectU.MasterVolume"; // 마스터 볼륨 저장 키
    private const string MouseSensitivityKey = "ProjectU.MouseSensitivity"; // 마우스 감도 저장 키
    private const string FullscreenKey = "ProjectU.Fullscreen"; // 전체 화면 저장 키

    public const float DefaultMasterVolume = 1f; // 기본 마스터 볼륨
    public const float DefaultMouseSensitivity = 0.1f; // 기본 마우스 감도
    public const float MinimumMouseSensitivity = 0.02f; // 최소 마우스 감도
    public const float MaximumMouseSensitivity = 0.5f; // 최대 마우스 감도

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

    public static void ApplyStoredSettings(
        ThirdPersonCameraFollow cameraFollow) // 저장된 설정을 현재 게임에 적용
    {
        ApplyMasterVolume(MasterVolume); // 마스터 볼륨 적용
        ApplyMouseSensitivity(
            cameraFollow,
            MouseSensitivity); // 마우스 감도 적용
        ApplyFullscreen(Fullscreen); // 전체 화면 적용
    }

    public static void SaveAndApply(
        float masterVolume,
        float mouseSensitivity,
        bool fullscreen,
        ThirdPersonCameraFollow cameraFollow) // 설정 저장과 즉시 적용
    {
        float validMasterVolume =
            Mathf.Clamp01(masterVolume); // 마스터 볼륨 범위 제한

        float validMouseSensitivity =
            Mathf.Clamp(
                mouseSensitivity,
                MinimumMouseSensitivity,
                MaximumMouseSensitivity); // 마우스 감도 범위 제한

        PlayerPrefs.SetFloat(
            MasterVolumeKey,
            validMasterVolume); // 마스터 볼륨 저장

        PlayerPrefs.SetFloat(
            MouseSensitivityKey,
            validMouseSensitivity); // 마우스 감도 저장

        PlayerPrefs.SetInt(
            FullscreenKey,
            fullscreen ? 1 : 0); // 전체 화면 여부 저장

        PlayerPrefs.Save(); // 설정 파일 즉시 저장

        ApplyMasterVolume(validMasterVolume); // 마스터 볼륨 즉시 적용
        ApplyMouseSensitivity(
            cameraFollow,
            validMouseSensitivity); // 마우스 감도 즉시 적용
        ApplyFullscreen(fullscreen); // 전체 화면 즉시 적용
    }

    private static void ApplyMasterVolume(
        float masterVolume) // 마스터 볼륨 적용
    {
        AudioListener.volume =
            Mathf.Clamp01(masterVolume); // 전체 Unity 오디오 볼륨 적용
    }

    private static void ApplyMouseSensitivity(
        ThirdPersonCameraFollow cameraFollow,
        float mouseSensitivity) // 마우스 감도 적용
    {
        if (cameraFollow == null) // 카메라 관리자 존재 확인
        {
            return; // 마우스 감도 적용 생략
        }

        cameraFollow.SetMouseSensitivity(
            mouseSensitivity); // 추적 카메라 감도 적용
    }

    private static void ApplyFullscreen(
        bool fullscreen) // 전체 화면 상태 적용
    {
        if (Screen.fullScreen == fullscreen) // 기존 전체 화면 상태 확인
        {
            return; // 동일 상태 재적용 생략
        }

        Screen.fullScreen = fullscreen; // 전체 화면 상태 변경
    }
}
