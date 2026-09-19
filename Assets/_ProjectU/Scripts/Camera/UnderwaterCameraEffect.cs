using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

[DefaultExecutionOrder(1000)] // 카메라 추적(ThirdPersonCameraFollow) 뒤에 실행
[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
[RequireComponent(typeof(Camera))] // 게임 카메라
public sealed class UnderwaterCameraEffect : MonoBehaviour // 106일차: 물속 화면 (푸른 안개 · 푸른 배경 · 먹먹한 소리) + 3인칭 카메라가 수면에 걸리지 않게
{
    [Tooltip("수면 높이 (IslandShoreGuard가 있으면 그 값을 씁니다).")]
    [SerializeField] private float seaLevel = -0.5f; // 수면
    [Tooltip("물속 안개 · 배경 색.")]
    [SerializeField] private Color waterColor = new Color(0.07f, 0.3f, 0.4f, 1f); // 물 색
    [Tooltip("물속 안개 밀도 (지수 제곱, 약 20m 앞까지 보임).")]
    [SerializeField, Min(0.001f)] private float fogDensity = 0.075f; // 물속 안개
    [Tooltip("화면에 덮는 물빛.")]
    [SerializeField] private Color tintColor = new Color(0.08f, 0.36f, 0.5f, 0.26f); // 화면 물빛
    [Tooltip("물속 소리 먹먹함 (낮을수록 먹먹함, Hz).")]
    [SerializeField, Min(100f)] private float muffledCutoff = 900f; // 먹먹한 소리
    [Tooltip("3인칭 카메라를 수면에서 이만큼 떨어뜨립니다 (m).")]
    [SerializeField, Min(0.05f)] private float surfaceMargin = 0.3f; // 수면 여유
    [Tooltip("카메라 추적 (비우면 같은 오브젝트에서 찾음).")]
    [SerializeField] private ThirdPersonCameraFollow follow; // 카메라 추적

    private Camera controlledCamera; // 게임 카메라
    private AudioLowPassFilter lowPass; // 먹먹한 소리
    private GameObject tintRoot; // 화면 물빛 Canvas
    private Terrain terrain; // 섬 Terrain
    private WeatherEffectsController weatherEffects; // 비 · 눈 (물속에서 숨김)
    private bool underwater; // 지금 물속인지

    private bool savedFog; // 물에 들어가기 전 안개 사용
    private FogMode savedFogMode; // 안개 방식
    private Color savedFogColor; // 안개 색
    private float savedFogDensity; // 안개 밀도
    private CameraClearFlags savedClearFlags; // 카메라 배경 방식
    private Color savedBackground; // 카메라 배경 색

    public static UnderwaterCameraEffect Instance { get; private set; } // 게임 카메라 효과
    public bool IsUnderwater => underwater; // 카메라가 물속인지
    public int EnterCount { get; private set; } // 물속에 들어간 횟수 (테스트용)
    public float SeaLevel => IslandShoreGuard.Instance != null ? IslandShoreGuard.Instance.SeaLevel : seaLevel; // 수면

    private void Awake() // 준비
    {
        Instance = this;
        controlledCamera = GetComponent<Camera>();

        if (follow == null)
        {
            follow = GetComponent<ThirdPersonCameraFollow>();
        }

        if (GetComponent<AudioListener>() != null)
        {
            lowPass = GetComponent<AudioLowPassFilter>();

            if (lowPass == null)
            {
                lowPass = gameObject.AddComponent<AudioLowPassFilter>();
            }

            lowPass.cutoffFrequency = muffledCutoff;
            lowPass.enabled = false;
        }

        weatherEffects = FindFirstObjectByType<WeatherEffectsController>();
        BuildTint();
    }

    private void OnDestroy() // 정리
    {
        if (underwater)
        {
            Exit();
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnDisable() // 꺼질 때 화면 되돌림
    {
        if (underwater)
        {
            Exit();
        }
    }

    private void BuildTint() // 화면 전체 물빛 (HUD 뒤)
    {
        tintRoot = new GameObject("UnderwaterTint");
        tintRoot.transform.SetParent(transform, false);
        Canvas canvas = tintRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -50; // 게임 화면 UI보다 뒤
        GameObject imageObject = new GameObject("Tint");
        imageObject.transform.SetParent(tintRoot.transform, false);
        Image image = imageObject.AddComponent<Image>();
        image.color = tintColor;
        image.raycastTarget = false;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        tintRoot.SetActive(false);
    }

    private float GroundHeight(Vector3 position) // Terrain 높이
    {
        if (terrain == null)
        {
            terrain = Terrain.activeTerrain != null ? Terrain.activeTerrain : FindFirstObjectByType<Terrain>();
        }

        return terrain != null ? terrain.SampleHeight(position) + terrain.transform.position.y : float.MaxValue;
    }

    private bool IsOverWater(Vector3 position) // 바닷물 위인지 (땅 밑 동굴 제외)
    {
        return GroundHeight(position) < SeaLevel - 0.2f;
    }

    private void LateUpdate() // 카메라 위치가 정해진 뒤
    {
        KeepOffWaterline();
        Vector3 position = transform.position;
        bool now = position.y < SeaLevel - 0.02f && IsOverWater(position);

        if (now && !underwater)
        {
            Enter();
        }
        else if (!now && underwater)
        {
            Exit();
        }

        if (underwater)
        {
            Apply();
        }
    }

    private void KeepOffWaterline() // 3인칭 : 잠수하면 카메라도 물속, 수면에 떠 있으면 카메라는 물 위
    {
        PlayerSwimming swimming = PlayerSwimming.Local;

        if (swimming == null || !swimming.IsSwimming || follow == null || follow.IsFirstPerson || follow.IsViewTransitioning || follow.IsExternalCameraControl)
        {
            return;
        }

        Vector3 position = transform.position;

        if (!IsOverWater(position))
        {
            return;
        }

        if (swimming.IsUnderwater)
        {
            float limit = SeaLevel - surfaceMargin;
            Vector3 focus = swimming.transform.position + Vector3.up * 1.4f; // 플레이어 머리 쪽

            if (position.y > limit && focus.y < limit) // 플레이어 쪽으로 당겨서 물속에 둠 (보는 방향 유지)
            {
                position = Vector3.Lerp(focus, position, (limit - focus.y) / (position.y - focus.y));
            }
            else
            {
                position.y = Mathf.Min(position.y, limit);
            }
        }
        else
        {
            position.y = Mathf.Max(position.y, SeaLevel + surfaceMargin);
        }

        transform.position = position;
    }

    private void Enter() // 물속으로
    {
        underwater = true;
        EnterCount++;
        savedFog = RenderSettings.fog;
        savedFogMode = RenderSettings.fogMode;
        savedFogColor = RenderSettings.fogColor;
        savedFogDensity = RenderSettings.fogDensity;
        savedClearFlags = controlledCamera.clearFlags;
        savedBackground = controlledCamera.backgroundColor;

        if (lowPass != null)
        {
            lowPass.enabled = true;
        }

        if (tintRoot != null)
        {
            tintRoot.SetActive(true);
        }

        if (weatherEffects != null)
        {
            weatherEffects.SetPrecipitationHidden(true);
        }

        Apply();
    }

    private void Apply() // 물속 안개 · 배경 (날씨가 안개를 바꿔도 물속에서는 다시 덮음)
    {
        if (RenderSettings.fogColor != waterColor || !Mathf.Approximately(RenderSettings.fogDensity, fogDensity)) // 물속에 있는 동안 날씨가 바꾼 값은 물 밖에서 쓰도록 기억
        {
            savedFogColor = RenderSettings.fogColor;
            savedFogDensity = RenderSettings.fogDensity;
            savedFog = savedFogMode == FogMode.Linear ? savedFog : savedFogDensity > 0f; // 날씨 안개는 밀도가 있을 때만 켬
        }

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = waterColor;
        RenderSettings.fogDensity = fogDensity;
        controlledCamera.clearFlags = CameraClearFlags.SolidColor;
        controlledCamera.backgroundColor = waterColor;
    }

    private void Exit() // 물 밖으로
    {
        underwater = false;
        RenderSettings.fog = savedFog;
        RenderSettings.fogMode = savedFogMode;
        RenderSettings.fogColor = savedFogColor;
        RenderSettings.fogDensity = savedFogDensity;

        if (controlledCamera != null)
        {
            controlledCamera.clearFlags = savedClearFlags;
            controlledCamera.backgroundColor = savedBackground;
        }

        if (lowPass != null)
        {
            lowPass.enabled = false;
        }

        if (tintRoot != null)
        {
            tintRoot.SetActive(false);
        }

        if (weatherEffects != null)
        {
            weatherEffects.SetPrecipitationHidden(false);
        }
    }

#if UNITY_EDITOR
    public void EditorAssign(float level, ThirdPersonCameraFollow cameraFollow) // 생성 도구 전용
    {
        seaLevel = level;
        follow = cameraFollow;
    }
#endif
}
