using System; // 이벤트
using System.Collections.Generic; // 목록
using UnityEngine; // Unity 기본 기능
using UnityEngine.Rendering; // 환경광 모드

[Serializable]
public sealed class CaveSaveData // 116일차: 동굴 저장 (지금 있는 동굴 · 찾아낸 입구)
{
    [Tooltip("지금 있는 동굴 구역 ID (밖이면 빈 값).")]
    public string currentAreaId = string.Empty; // 지금 동굴
    [Tooltip("지금까지 찾아낸 동굴 입구 ID 목록.")]
    public List<string> discoveredPortals = new List<string>(); // 찾아낸 입구
}

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class CaveManager : MonoBehaviour // 116일차: 동굴 드나들기와 동굴 안 어둠 (섬 옆에 따로 지은 고정 동굴 맵)
{
    [Header("Cave Air")] // 동굴 안 공기
    [Tooltip("동굴 안 환경광 (아주 어둡게).")]
    [SerializeField] private Color caveAmbient = new Color(0.045f, 0.05f, 0.06f, 1f); // 동굴 환경광
    [Tooltip("동굴 안 안개 색.")]
    [SerializeField] private Color caveFogColor = new Color(0.02f, 0.025f, 0.03f, 1f); // 동굴 안개 색
    [Tooltip("동굴 안 안개 짙기 (클수록 앞이 안 보임).")]
    [SerializeField, Min(0f)] private float caveFogDensity = 0.05f; // 동굴 안개 짙기
    [Tooltip("동굴 안에서 햇빛 밝기 (0이면 완전히 끔).")]
    [SerializeField, Min(0f)] private float caveSunIntensity = 0f; // 동굴 햇빛

    private readonly HashSet<string> discovered = new HashSet<string>(StringComparer.Ordinal); // 찾아낸 입구
    private string currentAreaId = string.Empty; // 지금 동굴 구역
    private Transform player; // 플레이어
    private Light overriddenSun; // 어둡게 바꾼 햇빛
    private float savedSunIntensity; // 바꾸기 전 햇빛 밝기
    private bool savedFog; // 바꾸기 전 안개 사용
    private Color savedFogColor; // 바꾸기 전 안개 색
    private float savedFogDensity; // 바꾸기 전 안개 짙기
    private FogMode savedFogMode; // 바꾸기 전 안개 방식
    private AmbientMode savedAmbientMode; // 바꾸기 전 환경광 방식
    private Color savedAmbientColor; // 바꾸기 전 환경광 색
    private bool atmosphereApplied; // 동굴 공기를 적용했는지

    public static CaveManager Instance { get; private set; } // Scene 관리자
    public bool IsInside => currentAreaId.Length > 0; // 동굴 안인지
    public string CurrentAreaId => currentAreaId; // 지금 동굴 구역 ID
    public int DiscoveredCount => discovered.Count; // 찾아낸 입구 수 (테스트용)
    public int TravelCount { get; private set; } // 드나든 횟수 (테스트용)

    public event Action<bool> InsideChanged; // 동굴에 들어가거나 나옴

    public bool IsDiscovered(string portalId) => !string.IsNullOrEmpty(portalId) && discovered.Contains(portalId); // 찾아낸 입구인지

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            RestoreAtmosphere();
            Instance = null;
        }
    }

    public bool Travel(CavePortal portal) // 입구 · 출구로 드나들기
    {
        if (portal == null || portal.Destination == null || !FindPlayer())
        {
            return false;
        }

        discovered.Add(portal.PortalId);
        MovePlayer(portal.Destination);
        currentAreaId = portal.IsExit ? string.Empty : portal.AreaId;
        TravelCount++;

        if (!IsInside)
        {
            RestoreAtmosphere(); // 밖으로 나오면 바로 원래 하늘로
        }

        InsideChanged?.Invoke(IsInside);
        return true;
    }

    private void MovePlayer(Transform destination) // 플레이어를 목적지로 옮김 (CharacterController는 잠시 끔)
    {
        CharacterController controller = player.GetComponent<CharacterController>();

        if (controller != null)
        {
            controller.enabled = false;
        }

        player.SetPositionAndRotation(destination.position, Quaternion.Euler(0f, destination.eulerAngles.y, 0f));

        if (controller != null)
        {
            controller.enabled = true;
        }
    }

    // 낮과 밤 · 날씨가 매 프레임 하늘을 바꾸므로, 그다음(LateUpdate)에 동굴 공기를 덮어쓴다
    private void LateUpdate()
    {
        if (IsInside)
        {
            ApplyAtmosphere();
        }
        else if (atmosphereApplied)
        {
            RestoreAtmosphere();
        }
    }

    private void ApplyAtmosphere() // 동굴 안 : 햇빛 끄고 · 환경광 어둡게 · 검은 안개
    {
        Light sun = RenderSettings.sun;

        if (!atmosphereApplied)
        {
            atmosphereApplied = true;
            savedAmbientMode = RenderSettings.ambientMode;
            savedAmbientColor = RenderSettings.ambientLight;
            savedFog = RenderSettings.fog;
            savedFogColor = RenderSettings.fogColor;
            savedFogDensity = RenderSettings.fogDensity;
            savedFogMode = RenderSettings.fogMode;

            if (sun != null)
            {
                overriddenSun = sun;
                savedSunIntensity = sun.intensity;
            }
        }

        if (overriddenSun != null)
        {
            overriddenSun.intensity = caveSunIntensity;
        }

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = caveAmbient;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = caveFogColor;
        RenderSettings.fogDensity = caveFogDensity;
    }

    private void RestoreAtmosphere() // 밖 : 원래 하늘로 되돌림
    {
        if (!atmosphereApplied)
        {
            return;
        }

        atmosphereApplied = false;

        if (overriddenSun != null)
        {
            overriddenSun.intensity = savedSunIntensity;
            overriddenSun = null;
        }

        RenderSettings.ambientMode = savedAmbientMode;
        RenderSettings.ambientLight = savedAmbientColor;
        RenderSettings.fog = savedFog;
        RenderSettings.fogMode = savedFogMode;
        RenderSettings.fogColor = savedFogColor;
        RenderSettings.fogDensity = savedFogDensity;
    }

    private bool FindPlayer()
    {
        if (player != null)
        {
            return true;
        }

        PlayerInventory found = FindFirstObjectByType<PlayerInventory>();
        player = found != null ? found.transform : null;
        return player != null;
    }

    // ---------------------------------------------------------------- 저장

    public CaveSaveData CaptureSaveData()
    {
        return new CaveSaveData { currentAreaId = currentAreaId, discoveredPortals = new List<string>(discovered) };
    }

    public void ApplySaveData(CaveSaveData data) // 불러오기 : 동굴 안에서 저장했으면 동굴 안 상태로 복원 (위치는 플레이어 저장값)
    {
        discovered.Clear();
        currentAreaId = string.Empty;

        if (data != null)
        {
            currentAreaId = data.currentAreaId ?? string.Empty;

            foreach (string portalId in data.discoveredPortals ?? new List<string>())
            {
                if (!string.IsNullOrEmpty(portalId))
                {
                    discovered.Add(portalId);
                }
            }
        }

        if (!IsInside)
        {
            RestoreAtmosphere();
        }

        InsideChanged?.Invoke(IsInside);
    }

    public void ResetForLoad() // 저장 파일에 동굴 기록이 없으면 밖에서 시작
    {
        ApplySaveData(null);
    }
}
