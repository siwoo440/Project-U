using System.Collections.Generic; // 목록
using System.Text; // 문자열
using UnityEngine; // Unity 기본 기능
using UnityEngine.AI; // 115일차: 서는 곳을 길 위로

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class NpcManager : MonoBehaviour // 90일차: 마을 NPC 일정 실행 (시간·요일·계절·날씨에 따라 위치 이동)
{
    public static NpcManager Instance { get; private set; } // Scene 관리자

    [Header("References")] // 참조
    [Tooltip("NPC 데이터베이스.")]
    [SerializeField] private NpcDatabase database; // 데이터베이스
    [Tooltip("낮밤 순환.")]
    [SerializeField] private DayNightCycle dayNightCycle; // 시간
    [Tooltip("계절 순환.")]
    [SerializeField] private SeasonCycle seasonCycle; // 계절
    [Tooltip("날씨 순환.")]
    [SerializeField] private WeatherCycle weatherCycle; // 날씨
    [Tooltip("Scene에 배치한 NPC.")]
    [SerializeField] private List<NpcAgent> agents = new List<NpcAgent>(); // NPC
    [Tooltip("Scene에 배치한 일정 위치.")]
    [SerializeField] private List<NpcLocationPoint> locations = new List<NpcLocationPoint>(); // 위치

    [Header("Rules")] // 규칙
    [Tooltip("상인 가판대에 서는 NPC (87일차 떠돌이 상인 대신).")]
    [SerializeField] private string stallMerchantId = "char_lichel"; // 가판대 상인
    [Tooltip("집에 있는 NPC가 안으로 들어가는 시각.")]
    [SerializeField, Range(0f, 24f)] private float bedtimeHour = 21f; // 잠자는 시각
    [Tooltip("집 안에서 나오는 시각.")]
    [SerializeField, Range(0f, 24f)] private float wakeHour = 6f; // 일어나는 시각
    [Tooltip("일정 확인 간격 (초).")]
    [SerializeField, Min(0.05f)] private float updateInterval = 0.25f; // 확인 간격
    [Tooltip("이보다 크게 시간이 건너뛰면(잠·불러오기) 걷지 않고 바로 옮깁니다 (게임 시간).")]
    [SerializeField, Min(0.05f)] private float jumpThresholdHours = 0.25f; // 순간 이동 기준

    private readonly Dictionary<string, NpcLocationPoint> lookup = new Dictionary<string, NpcLocationPoint>(); // 위치 검색
    private readonly Dictionary<string, int> slotCounter = new Dictionary<string, int>(); // 위치별 자리 수
    private readonly List<Vector3> claimedStands = new List<Vector3>(); // 115일차: 이번에 정한 서는 곳 (가까운 두 위치의 자리가 겹치지 않게)
    private const float MinimumStandGap = 0.9f; // 115일차: 서는 곳 사이 최소 거리 (m)
    private const int StandRetries = 6; // 겹치면 다음 자리를 찾는 횟수
    private float timer; // 확인 타이머
    private int lastDay = -1; // 마지막 날짜
    private float lastHour; // 마지막 시각
    private MarketStall cachedStall; // 가판대
    private AnimalPen[] cachedPens = new AnimalPen[0]; // 가축 우리
    private float structureSearchTimer; // 건축물 검색 타이머

    public NpcDatabase Database => database; // 데이터베이스 제공
    public IReadOnlyList<NpcAgent> Agents => agents; // NPC 제공
    public IReadOnlyList<NpcLocationPoint> Locations => locations; // 위치 제공
    public int CurrentDay => dayNightCycle != null ? dayNightCycle.CurrentDay : 1; // 날짜 제공
    public float CurrentHour => dayNightCycle != null ? dayNightCycle.CurrentHour : 12f; // 시각 제공
    public SeasonType CurrentSeason => seasonCycle != null ? seasonCycle.CurrentSeason : SeasonType.Spring; // 계절 제공
    public int CurrentDayInSeason => seasonCycle != null ? seasonCycle.CurrentDayInSeason : 1; // 계절 안 날짜 제공 (91일차 생일)
    public WeatherType CurrentWeather => weatherCycle != null ? weatherCycle.CurrentWeather : WeatherType.Clear; // 날씨 제공
    public static bool HasStallMerchant => Instance != null && Instance.isActiveAndEnabled && Instance.FindAgent(Instance.stallMerchantId) != null; // 가판대 상인 NPC 여부
    public NpcAgent StallMerchant => FindAgent(stallMerchantId); // 92일차: 가판대 상인 NPC (리첼)

    private void Awake() // 준비
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("NpcManager가 두 개 있습니다. 나중 것은 사용하지 않습니다.", this);
            enabled = false;
            return;
        }

        Instance = this;

        if (dayNightCycle == null || seasonCycle == null || weatherCycle == null)
        {
            Debug.LogError("NpcManager의 낮밤·계절·날씨 참조가 없습니다. Build Content > 8. NPC Placement를 다시 실행하세요.", this);
            enabled = false;
            return;
        }

        RebuildLookup();
    }

    private void OnDestroy() // 정리
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start() // 첫 위치
    {
        Refresh(true);
    }

    private void Update() // 일정 확인
    {
        timer -= Time.deltaTime;
        structureSearchTimer -= Time.deltaTime;

        if (timer > 0f)
        {
            return;
        }

        timer = updateInterval;
        int day = CurrentDay;
        float hour = CurrentHour;
        float passed = (day - lastDay) * 24f + hour - lastHour;
        bool jumped = lastDay < 0 || passed < -0.001f || passed > jumpThresholdHours;
        Refresh(jumped);
    }

    public void Refresh(bool snap) // 모든 NPC의 오늘 일정 칸 적용 (snap = 걷지 않고 바로 옮김)
    {
        int day = CurrentDay;
        float hour = CurrentHour;
        SeasonType season = CurrentSeason;
        WeatherType weather = CurrentWeather;
        bool night = IsSleepTime(hour);
        lastDay = day;
        lastHour = hour;
        slotCounter.Clear();
        claimedStands.Clear();

        if (structureSearchTimer <= 0f || snap)
        {
            FindStructures();
        }

        foreach (NpcAgent agent in agents)
        {
            if (agent == null || agent.Character == null || agent.Character.Schedule == null || agent.IsFollowing) // 109일차: 동료는 일정 대신 플레이어를 따라감
            {
                continue;
            }

            NpcScheduleData.Plan plan = agent.Character.Schedule.SelectPlan(day, season, weather);
            NpcScheduleData.Stop stop = plan?.GetStop(hour);

            if (stop == null || !lookup.TryGetValue(stop.LocationId, out NpcLocationPoint point))
            {
                continue;
            }

            bool inside = point.IsHome && night;
            int slot = NextSlot(point.LocationId);
            ResolveStand(point, slot, out Vector3 position, out Quaternion facing);

            if (!inside) // 115일차: 가까운 다른 위치(광장 · 카페 등)의 자리와 겹치면 다음 자리로 (길 위로 옮긴 자리로 비교)
            {
                position = SnapToPath(position);

                for (int retry = 0; retry < StandRetries && IsStandTaken(position); retry++)
                {
                    slot = NextSlot(point.LocationId);
                    ResolveStand(point, slot, out position, out facing);
                    position = SnapToPath(position);
                }

                claimedStands.Add(position);
            }

            string key = $"{plan.PlanId}|{stop.Hour}|{stop.LocationId}|{slot}|{inside}|{AnchorKey(point)}";

            if (!snap && key == agent.StopKey)
            {
                continue;
            }

            agent.StopKey = key;
            agent.GoTo(point, position, facing, stop.Activity, inside, snap);
        }
    }

    private int NextSlot(string locationId) // 위치의 다음 자리 번호
    {
        int slot = slotCounter.TryGetValue(locationId, out int used) ? used : 0;
        slotCounter[locationId] = slot + 1;
        return slot;
    }

    private static Vector3 SnapToPath(Vector3 position) // 서는 곳을 걸을 수 있는 곳으로 (NpcAgent.GoTo와 같은 방식 : 벽 · 나무 안이면 가장 가까운 길 위)
    {
        return NavMesh.SamplePosition(position, out NavMeshHit hit, 2.5f, NavMesh.AllAreas) ? hit.position : position;
    }

    private bool IsStandTaken(Vector3 position) // 115일차: 이미 정한 서는 곳과 너무 가까운지
    {
        foreach (Vector3 claimed in claimedStands)
        {
            float x = claimed.x - position.x;
            float z = claimed.z - position.z;

            if (x * x + z * z < MinimumStandGap * MinimumStandGap)
            {
                return true;
            }
        }

        return false;
    }

    public bool IsSleepTime(float hour) // 집 안에서 자는 시간
    {
        return bedtimeHour > wakeHour ? hour >= bedtimeHour || hour < wakeHour : hour >= bedtimeHour && hour < wakeHour;
    }

    public NpcAgent FindAgent(string characterId) // ID로 NPC 찾기
    {
        foreach (NpcAgent agent in agents)
        {
            if (agent != null && agent.CharacterId == characterId)
            {
                return agent;
            }
        }

        return null;
    }

    public bool TryGetLocation(string locationId, out NpcLocationPoint point) // 위치 찾기
    {
        if (lookup.Count == 0)
        {
            RebuildLookup();
        }

        return lookup.TryGetValue(locationId ?? string.Empty, out point);
    }

    public SeasonType GetSeasonForDay(int day) // 92일차: 지정 날짜의 계절 (다음 영업일 · 불러오기 재고 계산)
    {
        int daysPerSeason = seasonCycle != null ? Mathf.Max(1, seasonCycle.DaysPerSeason) : 28;
        return (SeasonType)((Mathf.Max(1, day) - 1) / daysPerSeason % 4);
    }

    public bool IsAnchorReady(NpcLocationPoint point) // 92일차: 위치가 따라가는 건축물이 있는지 (상인 가판대는 있어야 장사 가능)
    {
        if (point == null || point.Anchor != NpcLocationAnchor.MarketStall)
        {
            return point != null;
        }

        return cachedStall != null && cachedStall.isActiveAndEnabled; // 가판대 검색은 2초마다 (FindStructures)
    }

    public MarketStall GetStallFor(NpcAgent agent) // 가판대에 도착해 서 있는 상인 NPC면 그 가판대
    {
        if (agent == null || agent.CharacterId != stallMerchantId || !agent.HasArrived || agent.CurrentPoint == null || agent.CurrentPoint.Anchor != NpcLocationAnchor.MarketStall)
        {
            return null;
        }

        return cachedStall != null && cachedStall.isActiveAndEnabled ? cachedStall : null;
    }

    public string DescribeAll() // 디버그용 현재 상태
    {
        StringBuilder text = new StringBuilder($"[NPC] DAY {CurrentDay} {MarketStall.FormatHour(CurrentHour)} {CurrentSeason} {CurrentWeather} ({NpcCalendar.GetWeekday(CurrentDay)})");

        foreach (NpcAgent agent in agents)
        {
            if (agent == null)
            {
                continue;
            }

            NpcScheduleData.Plan plan = agent.Character?.Schedule?.SelectPlan(CurrentDay, CurrentSeason, CurrentWeather);
            string state = agent.IsInside ? "집 안" : agent.HasArrived ? "도착" : "이동 중";
            text.Append($"\n{agent.DisplayName} : {agent.CurrentLocationId} · {agent.CurrentActivity} · {state} · 계획 {plan?.PlanId} · 위치 {agent.transform.position:F1}");
        }

        return text.ToString();
    }

    private void ResolveStand(NpcLocationPoint point, int slot, out Vector3 position, out Quaternion facing) // 위치 지점 → 서는 곳
    {
        position = point.GetStandPosition(slot);
        facing = point.transform.rotation;

        if (point.Anchor == NpcLocationAnchor.MarketStall && cachedStall != null)
        {
            Transform stall = cachedStall.transform;
            Transform spot = cachedStall.Merchant != null ? cachedStall.Merchant : stall;
            facing = stall.rotation * Quaternion.Euler(0f, 180f, 0f); // 가판대 앞(-Z)을 바라봄
            position = spot.position + NpcLocationPoint.GetSlotOffset(stall.right, slot, point.SlotSpacing);
        }
        else if (point.Anchor == NpcLocationAnchor.AnimalPen && TryFindNearestPen(point.transform.position, out Bounds bounds))
        {
            Vector3 edge = bounds.ClosestPoint(point.transform.position);
            Vector3 outward = edge - bounds.center;
            outward.y = 0f;
            outward = outward.sqrMagnitude > 0.001f ? outward.normalized : Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, outward);
            position = edge + outward * 0.9f + NpcLocationPoint.GetSlotOffset(right, slot, point.SlotSpacing);
            position.y = point.transform.position.y;
            facing = Quaternion.LookRotation(-outward); // 우리를 바라봄
        }
    }

    private bool TryFindNearestPen(Vector3 from, out Bounds bounds) // 가장 가까운 가축 우리
    {
        bounds = default;
        float best = float.MaxValue;
        bool found = false;

        foreach (AnimalPen pen in cachedPens)
        {
            if (pen == null || !pen.isActiveAndEnabled)
            {
                continue;
            }

            float distance = (pen.transform.position - from).sqrMagnitude;

            if (distance >= best)
            {
                continue;
            }

            Bounds penBounds = new Bounds(pen.transform.position, Vector3.one * 2f);

            foreach (Collider collider in pen.GetComponentsInChildren<Collider>())
            {
                if (!collider.isTrigger)
                {
                    penBounds.Encapsulate(collider.bounds);
                }
            }

            best = distance;
            bounds = penBounds;
            found = true;
        }

        return found;
    }

    private string AnchorKey(NpcLocationPoint point) // 건축물이 생기거나 옮겨지면 자리를 다시 잡기 위한 키
    {
        switch (point.Anchor)
        {
            case NpcLocationAnchor.MarketStall:
                return cachedStall != null ? cachedStall.GetInstanceID().ToString() : "none";
            case NpcLocationAnchor.AnimalPen:
                return TryFindNearestPen(point.transform.position, out Bounds bounds) ? bounds.center.ToString("F1") : "none";
            default:
                return string.Empty;
        }
    }

    private void FindStructures() // 플레이어가 세운 가판대·가축 우리 찾기
    {
        structureSearchTimer = 2f;
        cachedStall = null;

        foreach (MarketStall stall in FindObjectsByType<MarketStall>(FindObjectsSortMode.None))
        {
            if (stall.isActiveAndEnabled && (cachedStall == null || stall.GetInstanceID() < cachedStall.GetInstanceID()))
            {
                cachedStall = stall; // 여러 개면 항상 같은 가판대
            }
        }

        cachedPens = FindObjectsByType<AnimalPen>(FindObjectsSortMode.None);
    }

    private void RebuildLookup() // 위치 ID 검색 표
    {
        lookup.Clear();

        foreach (NpcLocationPoint point in locations)
        {
            if (point != null && !string.IsNullOrEmpty(point.LocationId) && !lookup.ContainsKey(point.LocationId))
            {
                lookup.Add(point.LocationId, point);
            }
        }
    }

#if UNITY_EDITOR
    public void EditorAssign(NpcDatabase data, DayNightCycle time, SeasonCycle seasons, WeatherCycle weatherSystem, List<NpcAgent> npcs, List<NpcLocationPoint> points) // 생성 도구 전용
    {
        database = data;
        dayNightCycle = time;
        seasonCycle = seasons;
        weatherCycle = weatherSystem;
        agents = npcs ?? new List<NpcAgent>();
        locations = points ?? new List<NpcLocationPoint>();
        lookup.Clear();
    }
#endif
}
