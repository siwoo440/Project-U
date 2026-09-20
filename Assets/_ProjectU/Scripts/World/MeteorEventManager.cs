using System; // 직렬화
using System.Collections.Generic; // 목록
using System.Linq; // 목록 계산
using TMPro; // 지도 이름표
using UnityEngine; // Unity 기본 기능

[Serializable]
public sealed class MeteorCraterSaveData // 117일차: 떨어진 운석 하나
{
    [Tooltip("구덩이 ID.")]
    public string craterId = string.Empty; // 구덩이 ID
    [Tooltip("떨어진 자리.")]
    public SaveVector3Data position = new SaveVector3Data(); // 떨어진 자리
    [Tooltip("아직 남은 운석 덩어리 수.")]
    public int remaining; // 남은 덩어리 수
    [Tooltip("덩어리마다 남은 운석 조각 수.")]
    public List<int> rockQuantities = new List<int>(); // 덩어리별 남은 수량
    [Tooltip("떨어진 날.")]
    public int fellDay; // 떨어진 날
    [Tooltip("다 캔 뒤 사라지기까지 지난 게임 시간 (시간).")]
    public float clearingHours; // 사라지기까지 지난 시간
}

[Serializable]
public sealed class MeteorSaveData // 117일차: 운석 전체 상태
{
    [Tooltip("지금 섬에 있는 운석 구덩이.")]
    public List<MeteorCraterSaveData> craters = new List<MeteorCraterSaveData>(); // 구덩이 목록
    [Tooltip("운석 떨어짐을 확인한 마지막 밤.")]
    public int lastRolledDay = -1; // 마지막 확인 날짜
}

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class MeteorEventManager : MonoBehaviour // 117일차: 밤에 가끔 운석이 떨어지고 구덩이에서 운석 조각을 캔다
{
    public const string FallMessage = "밤하늘에서 무언가 떨어졌어요!"; // 떨어졌을 때 알림
    public const string MapLabelGroupName = "MeteorMapLabels"; // 지도 이름표 묶음 이름

    [Header("References")] // 참조 설정 묶음
    [Tooltip("운석 구덩이 바닥 모델 Prefab.")]
    [SerializeField] private GameObject craterGroundPrefab; // 구덩이 바닥 모델
    [Tooltip("운석 덩어리 채집물 Prefab.")]
    [SerializeField] private GameObject meteorRockPrefab; // 운석 덩어리 채집물

    [Header("Falling")] // 떨어짐 설정 묶음
    [Tooltip("밤마다 운석이 떨어질 확률.")]
    [SerializeField, Range(0f, 1f)] private float nightChance = 0.2f; // 떨어질 확률
    [Tooltip("운석이 떨어지는 시각.")]
    [SerializeField, Range(0f, 24f)] private float fallHour = 22.5f; // 떨어지는 시각
    [Tooltip("섬에 동시에 있을 수 있는 구덩이 수.")]
    [SerializeField, Min(1)] private int maximumCraters = 2; // 최대 구덩이 수
    [Tooltip("구덩이 하나에 놓이는 운석 덩어리 수.")]
    [SerializeField, Min(1)] private int rocksPerCrater = 5; // 구덩이 덩어리 수
    [Tooltip("다 캐고 이 게임 시간이 지나면 구덩이가 사라집니다.")]
    [SerializeField, Min(0.5f)] private float clearingHours = 6f; // 사라지기까지 걸리는 시간
    [Tooltip("캐지 않아도 이 날짜가 지나면 구덩이가 사라집니다.")]
    [SerializeField, Min(1)] private int expireDays = 3; // 남아 있는 날짜

    [Header("Where")] // 떨어지는 자리 설정 묶음
    [Tooltip("섬 가운데에서 떨어지는 거리 범위.")]
    [SerializeField] private Vector2 distanceRange = new Vector2(180f, 640f); // 떨어지는 거리 범위
    [Tooltip("이 각도보다 가파른 곳에는 떨어지지 않습니다.")]
    [SerializeField, Min(5f)] private float maximumSlope = 22f; // 최대 경사
    [Tooltip("땅 높이가 이 값보다 낮으면 바다로 보고 건너뜁니다.")]
    [SerializeField] private float minimumGroundHeight = 3f; // 최소 땅 높이
    [Tooltip("플레이어와 이 거리보다 가까운 곳에는 떨어지지 않습니다.")]
    [SerializeField, Min(20f)] private float minimumPlayerDistance = 120f; // 플레이어와 최소 거리
    [Tooltip("구덩이끼리 이 거리보다 가깝게 떨어지지 않습니다.")]
    [SerializeField, Min(20f)] private float minimumCraterGap = 120f; // 구덩이끼리 최소 거리

    private sealed class Crater // 실제로 놓인 구덩이 하나
    {
        public MeteorCraterSaveData Data; // 저장 데이터
        public GameObject Root; // 구덩이 묶음
        public GameObject Label; // 지도 이름표
        public readonly List<GatherableResource> Rocks = new List<GatherableResource>(); // 운석 덩어리
    }

    private readonly List<Crater> craters = new List<Crater>(); // 지금 섬의 구덩이
    private DayNightCycle cycle; // 낮과 밤
    private Terrain terrain; // 섬 지형
    private Transform player; // 플레이어
    private Transform labelGroup; // 지도 이름표 묶음
    private int lastRolledDay = -1; // 마지막으로 확인한 밤
    private float lastHour = -1f; // 지난번에 본 시각
    private float checkTimer; // 확인까지 남은 시간 (0.5초마다 확인해 CPU를 아낀다)

    public static MeteorEventManager Instance { get; private set; } // Scene 관리자
    public int CraterCount => craters.Count; // 지금 구덩이 수 (테스트용)
    public int FallCount { get; private set; } // 떨어진 횟수 (테스트용)
    public int RocksPerCrater => Mathf.Max(1, rocksPerCrater); // 구덩이 덩어리 수 (테스트용)
    public float ClearingHours => clearingHours; // 사라지기까지 시간 (테스트용)
    public bool HasPrefabs => craterGroundPrefab != null && meteorRockPrefab != null; // 참조 확인 (테스트용)
    public GameObject MeteorRockPrefab => meteorRockPrefab; // 운석 덩어리 Prefab 제공 (검사용)

    public float LastCheckHour => lastHour; // 마지막으로 확인한 시각 (테스트용)

    public IReadOnlyList<MeteorCraterSaveData> CraterData => craters.Select(item => item.Data).ToList(); // 구덩이 상태 (테스트용)

    public IReadOnlyList<Vector3> CraterPositions => craters // 구덩이 자리 (테스트용)
        .Where(item => item.Root != null)
        .Select(item => item.Root.transform.position)
        .ToList();

    private void Awake() // 관리자 등록
    {
        Instance = this;
    }

    private void OnDestroy() // 관리자 해제
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private const float CheckInterval = 0.5f; // 확인 간격 (초)

    private void Update() // 밤마다 한 번 확인하고 구덩이를 정리 (0.5초마다)
    {
        checkTimer -= Time.unscaledDeltaTime; // 게임이 멈춰 있어도(시간 배율 0) 확인 간격은 흐른다

        if (checkTimer > 0f)
        {
            return;
        }

        checkTimer = CheckInterval;

        if (!FindReferences())
        {
            return;
        }

        float hour = cycle.CurrentHour;
        int day = cycle.CurrentDay;

        if (lastHour < 0f) // 시작한 첫 프레임
        {
            lastHour = hour;
            return;
        }

        if (lastRolledDay != day && PassedHour(fallHour, lastHour, hour)) // 떨어지는 시각을 지났는지
        {
            lastRolledDay = day;

            if (craters.Count < maximumCraters && UnityEngine.Random.value < nightChance)
            {
                TryFall(day);
            }
        }

        float passedHours = Mathf.Repeat(hour - lastHour, 24f); // 이번 프레임에 흐른 게임 시간
        lastHour = hour;
        UpdateCraters(day, passedHours > 12f ? 0f : passedHours); // 잠을 자서 시간이 건너뛰어도 세어 준다
    }

    private static bool PassedHour(float target, float previous, float current) // 이번 프레임에 그 시각을 지났는지
    {
        if (Mathf.Approximately(previous, current))
        {
            return false;
        }

        return previous < current ? previous < target && target <= current : previous < target || target <= current;
    }

    public bool TryFall(int day) // 운석 떨어뜨리기 (테스트에서도 사용)
    {
        if (!FindReferences() || !HasPrefabs || craters.Count >= maximumCraters)
        {
            return false;
        }

        if (!TryFindSpot(out Vector3 spot))
        {
            return false;
        }

        MeteorCraterSaveData data = new MeteorCraterSaveData
        {
            craterId = $"meteor_{day:000}_{UnityEngine.Random.Range(100, 1000)}",
            position = SaveVector3Data.FromVector3(spot),
            remaining = RocksPerCrater,
            rockQuantities = Enumerable.Repeat(0, RocksPerCrater).ToList(), // 0 = Prefab 기본 수량
            fellDay = day,
            clearingHours = 0f
        };

        if (!SpawnCrater(data))
        {
            return false;
        }

        FallCount++;

        if (player != null)
        {
            CombatDamagePopup.SpawnText(player.position + Vector3.up * 2.6f, FallMessage, new Color(0.72f, 0.86f, 1f, 1f), 2f);
        }

        return true;
    }

    private bool TryFindSpot(out Vector3 spot) // 바다 · 가파른 곳 · 플레이어 옆은 피한다
    {
        spot = Vector3.zero;
        Vector3 size = terrain.terrainData.size;

        for (int attempt = 0; attempt < 60; attempt++)
        {
            float angle = UnityEngine.Random.value * Mathf.PI * 2f;
            float distance = Mathf.Lerp(distanceRange.x, distanceRange.y, UnityEngine.Random.value);
            Vector3 candidate = new Vector3(Mathf.Sin(angle) * distance, 0f, Mathf.Cos(angle) * distance);
            candidate.y = terrain.SampleHeight(candidate) + terrain.transform.position.y;

            if (candidate.y < minimumGroundHeight) // 바다와 물가
            {
                continue;
            }

            Vector3 local = candidate - terrain.transform.position;
            float steepness = terrain.terrainData.GetSteepness(Mathf.Clamp01(local.x / size.x), Mathf.Clamp01(local.z / size.z));

            if (steepness > maximumSlope) // 너무 가파른 곳
            {
                continue;
            }

            if (player != null && Vector3.Distance(player.position, candidate) < minimumPlayerDistance) // 머리 위는 피한다
            {
                continue;
            }

            if (craters.Any(item => item.Root != null && Vector3.Distance(item.Root.transform.position, candidate) < minimumCraterGap))
            {
                continue;
            }

            spot = candidate;
            return true;
        }

        return false;
    }

    private bool SpawnCrater(MeteorCraterSaveData data) // 구덩이 · 운석 덩어리 · 지도 표시 만들기
    {
        if (!HasPrefabs)
        {
            return false;
        }

        Vector3 spot = data.position.ToVector3();
        GameObject root = new GameObject($"MeteorCrater_{data.craterId}");
        root.transform.SetParent(transform, true);
        root.transform.position = spot;
        Crater crater = new Crater { Data = data, Root = root };

        GameObject ground = Instantiate(craterGroundPrefab, spot + Vector3.up * 0.05f, Quaternion.Euler(0f, UnityEngine.Random.value * 360f, 0f), root.transform);
        ground.name = "CraterGround";

        foreach (Renderer renderer in ground.GetComponentsInChildren<Renderer>())
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // 바닥 자국은 그림자를 만들지 않는다
        }

        List<int> quantities = data.rockQuantities != null && data.rockQuantities.Count > 0
            ? data.rockQuantities
            : Enumerable.Repeat(0, Mathf.Clamp(data.remaining, 1, RocksPerCrater)).ToList();
        int rockCount = quantities.Count;

        for (int index = 0; index < rockCount; index++)
        {
            float angle = index / (float)rockCount * Mathf.PI * 2f + UnityEngine.Random.value;
            Vector3 rockSpot = spot + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * UnityEngine.Random.Range(1.4f, 3.4f);
            rockSpot.y = terrain.SampleHeight(rockSpot) + terrain.transform.position.y;
            GameObject rock = Instantiate(meteorRockPrefab, rockSpot, Quaternion.Euler(0f, UnityEngine.Random.value * 360f, 0f), root.transform);
            rock.name = $"MeteorRock_{index:00}";
            GatherableResource resource = rock.GetComponent<GatherableResource>();

            if (resource != null)
            {
                resource.MarkTemporary($"meteor_{data.craterId}_{index:00}"); // 월드 저장에서 빼고 실행용 ID를 준다

                if (quantities[index] > 0) // 저장해 둔 남은 수량 되살리기
                {
                    resource.RestoreFromSave(Mathf.Min(quantities[index], resource.TotalQuantity), false, 0f);
                }

                crater.Rocks.Add(resource);
            }
        }

        crater.Label = CreateMapLabel(spot);
        craters.Add(crater);
        return true;
    }

    private GameObject CreateMapLabel(Vector3 spot) // 전체 지도에 운석 표시
    {
        int layer = LayerMask.NameToLayer("MapLabel");

        if (layer < 0)
        {
            return null;
        }

        if (labelGroup == null)
        {
            labelGroup = new GameObject(MapLabelGroupName).transform;
            labelGroup.SetParent(transform, false);
            labelGroup.gameObject.AddComponent<MapLabelScaler>();
        }

        GameObject holder = new GameObject("MapLabel_Meteor", typeof(RectTransform));
        holder.transform.SetParent(labelGroup, false);
        holder.layer = layer;
        TextMeshPro label = holder.AddComponent<TextMeshPro>();
        label.rectTransform.sizeDelta = new Vector2(70f, 1f);
        label.fontSize = 42f;
        label.alignment = TextAlignmentOptions.Center;
        label.richText = true;
        label.text = "<mark=#2A1B14B0 padding=\"18,18,8,8\">운석</mark>";
        holder.transform.SetPositionAndRotation(spot + Vector3.up * 40f, Quaternion.Euler(90f, 0f, 0f));
        holder.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return holder;
    }

    private void UpdateCraters(int day, float passedHours) // 다 캔 구덩이와 오래된 구덩이를 치운다
    {
        for (int index = craters.Count - 1; index >= 0; index--)
        {
            Crater crater = craters[index];
            crater.Data.remaining = crater.Rocks.Count(rock => rock != null && !rock.IsDepleted);

            if (crater.Data.remaining <= 0) // 다 캐면 잠시 뒤에 사라진다
            {
                crater.Data.clearingHours += passedHours;

                if (crater.Data.clearingHours >= clearingHours)
                {
                    RemoveCrater(index);
                    continue;
                }
            }
            else
            {
                crater.Data.clearingHours = 0f; // 다시 생긴 덩어리가 있으면 처음부터
            }

            if (day - crater.Data.fellDay >= expireDays) // 오래 두면 사라진다
            {
                RemoveCrater(index);
            }
        }
    }

    private void RemoveCrater(int index) // 구덩이 하나 치우기
    {
        Crater crater = craters[index];

        if (crater.Label != null)
        {
            Destroy(crater.Label);
        }

        if (crater.Root != null)
        {
            Destroy(crater.Root);
        }

        craters.RemoveAt(index);
    }

    private bool FindReferences() // 필요한 참조 찾기
    {
        if (cycle == null)
        {
            cycle = FindFirstObjectByType<DayNightCycle>();
        }

        if (terrain == null)
        {
            terrain = Terrain.activeTerrain != null ? Terrain.activeTerrain : FindFirstObjectByType<Terrain>();
        }

        if (player == null)
        {
            PlayerInventory inventory = FindFirstObjectByType<PlayerInventory>();
            player = inventory != null ? inventory.transform : null;
        }

        return cycle != null && terrain != null;
    }

    // ---------------------------------------------------------------- 저장

    public MeteorSaveData CaptureSaveData() // 운석 상태 저장
    {
        return new MeteorSaveData
        {
            lastRolledDay = lastRolledDay,
            craters = craters.Select(crater => new MeteorCraterSaveData
            {
                craterId = crater.Data.craterId,
                position = crater.Data.position,
                remaining = crater.Rocks.Count(rock => rock != null && !rock.IsDepleted),
                rockQuantities = crater.Rocks
                    .Where(rock => rock != null && !rock.IsDepleted)
                    .Select(rock => Mathf.Max(1, rock.RemainingQuantity))
                    .ToList(),
                fellDay = crater.Data.fellDay,
                clearingHours = crater.Data.clearingHours
            }).ToList()
        };
    }

    public void ApplySaveData(MeteorSaveData data) // 불러오기 : 있던 구덩이를 다시 만든다
    {
        for (int index = craters.Count - 1; index >= 0; index--)
        {
            RemoveCrater(index);
        }

        lastRolledDay = data != null ? data.lastRolledDay : -1;
        lastHour = -1f;

        if (data == null || data.craters == null || !FindReferences())
        {
            return;
        }

        foreach (MeteorCraterSaveData crater in data.craters)
        {
            if (crater != null && crater.remaining > 0)
            {
                SpawnCrater(crater);
            }
        }
    }

    public void ResetForLoad() // 운석 기록이 없는 저장 파일
    {
        ApplySaveData(null);
    }

#if UNITY_EDITOR
    public void EditorAssign(GameObject ground, GameObject rock) // 생성 도구 전용 참조 연결
    {
        craterGroundPrefab = ground;
        meteorRockPrefab = rock;
    }
#endif
}
