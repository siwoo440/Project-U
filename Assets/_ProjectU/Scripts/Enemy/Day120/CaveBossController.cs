using System.Collections.Generic; // 목록
using System.Linq; // 목록 계산
using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class CaveBossController : MonoBehaviour // 120일차: 동굴 깊은층 보스 (세 단계로 싸우고, 이기면 다시 나오지 않는다)
{
    private const float CheckInterval = 0.25f; // 판단 간격 (초)

    [Header("Boss")] // 보스 설정 묶음
    [Tooltip("보스 ID (저장 · 검사용).")]
    [SerializeField] private string bossId = "boss_crystal_golem"; // 보스 ID
    [Tooltip("보스 한글 이름.")]
    [SerializeField] private string koreanName = "수정 골렘"; // 한글 이름
    [Tooltip("보스 방 반경 (이 안에 들어오면 싸움이 시작됩니다).")]
    [SerializeField, Min(5f)] private float arenaRadius = 18f; // 보스 방 반경

    [Header("Phases")] // 단계 설정 묶음
    [Tooltip("체력이 이 비율 아래면 2단계 (수정 던지기).")]
    [SerializeField, Range(0.1f, 0.95f)] private float secondPhaseHealth = 0.66f; // 2단계 체력
    [Tooltip("체력이 이 비율 아래면 3단계 (수정 조각 부르기).")]
    [SerializeField, Range(0.05f, 0.9f)] private float thirdPhaseHealth = 0.33f; // 3단계 체력
    [Tooltip("3단계에서 부르는 수정 조각 수.")]
    [SerializeField, Min(1)] private int summonCount = 4; // 부르는 수

    [Header("References")] // 참조 설정 묶음
    [Tooltip("보스 방을 막는 수정 문 (싸우는 동안 켜집니다).")]
    [SerializeField] private GameObject gateObject; // 수정 문
    [Tooltip("3단계에서 부르는 수정 조각 Prefab.")]
    [SerializeField] private GameObject summonPrefab; // 수정 조각 Prefab

    private EnemyHealth health; // 보스 체력
    private EnemyCombatController combat; // 전투 관리자
    private EnemyRangedAttackController ranged; // 수정 던지기
    private Transform player; // 플레이어
    private readonly List<GameObject> summons = new List<GameObject>(); // 부른 수정 조각
    private float nextCheckTime; // 다음 판단 시각
    private int phase = 1; // 지금 단계
    private bool fighting; // 싸우는 중
    private bool defeated; // 이미 쓰러뜨렸는지
    private bool summoned; // 3단계에서 불렀는지

    public static CaveBossController Instance { get; private set; } // Scene 보스
    public string BossId => bossId; // 보스 ID 제공
    public string KoreanName => koreanName; // 한글 이름 제공
    public int Phase => phase; // 지금 단계 제공 (테스트용)
    public bool IsFighting => fighting; // 싸우는 중인지 제공 (테스트용)
    public bool IsDefeated => defeated; // 쓰러뜨렸는지 제공 (테스트용)
    public int SummonCount => summons.Count(item => item != null); // 부른 수정 조각 수 (테스트용)
    public float ArenaRadius => arenaRadius; // 보스 방 반경 제공 (테스트용)
    public bool GateClosed => gateObject != null && gateObject.activeSelf; // 문이 닫혀 있는지 (테스트용)

#if UNITY_EDITOR
    public void EditorAssign(string id, string korean, GameObject gate, GameObject summon, float radius) // 생성 도구 전용
    {
        bossId = id;
        koreanName = korean;
        gateObject = gate;
        summonPrefab = summon;
        arenaRadius = radius;
    }
#endif

    private void Awake()
    {
        Instance = this;
        health = GetComponent<EnemyHealth>();
        combat = GetComponent<EnemyCombatController>();
        ranged = GetComponent<EnemyRangedAttackController>();

        if (combat != null)
        {
            combat.enabled = false; // 방에 들어와야 깨어난다
        }

        if (ranged != null)
        {
            ranged.enabled = false;
        }

        if (health != null)
        {
            health.Died += OnDied;
        }

        CloseGate(false);
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.Died -= OnDied;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (defeated || Time.time < nextCheckTime)
        {
            return;
        }

        nextCheckTime = Time.time + CheckInterval;

        if (!FindPlayer())
        {
            return;
        }

        float distance = Flat(transform.position, player.position);

        if (!fighting)
        {
            if (distance <= arenaRadius)
            {
                StartFight();
            }

            return;
        }

        if (distance > arenaRadius * 1.8f) // 멀리 달아나면 싸움이 끝나고 보스가 회복한다
        {
            ResetFight();
            return;
        }

        UpdatePhase();
    }

    private void StartFight()
    {
        fighting = true;
        phase = 1;
        summoned = false;

        if (combat != null)
        {
            combat.enabled = true;
        }

        CloseGate(true);
        CombatDamagePopup.SpawnText(transform.position + Vector3.up * 3.2f, $"{koreanName}이(가) 깨어났다!", new Color(0.66f, 0.92f, 1f, 1f), 3f);
        Debug.Log($"{koreanName} 전투 시작", this);
    }

    private void ResetFight() // 방에서 나가면 처음 상태로 돌아간다
    {
        fighting = false;
        phase = 1;
        summoned = false;
        ClearSummons();
        CloseGate(false);

        if (combat != null)
        {
            combat.enabled = false;
        }

        if (ranged != null)
        {
            ranged.enabled = false;
        }

        if (health != null && health.IsAlive)
        {
            health.Revive(); // 체력을 되돌린다
        }
    }

    private void UpdatePhase()
    {
        if (health == null)
        {
            return;
        }

        float ratio = health.NormalizedHealth;
        int next = ratio <= thirdPhaseHealth ? 3 : ratio <= secondPhaseHealth ? 2 : 1;

        if (next == phase)
        {
            return;
        }

        phase = next;
        ApplyPhase();
    }

    private void ApplyPhase()
    {
        if (ranged != null)
        {
            ranged.enabled = phase >= 2; // 2단계부터 수정을 던진다
        }

        string message = phase == 2 ? $"{koreanName}이(가) 수정을 던진다!" : $"{koreanName}이(가) 수정 조각을 부른다!";
        CombatDamagePopup.SpawnText(transform.position + Vector3.up * 3.2f, message, new Color(0.8f, 0.9f, 1f, 1f), 2.6f);

        if (phase < 3 || summoned)
        {
            return;
        }

        summoned = true;
        Summon();
    }

    private void Summon() // 수정 조각 부르기
    {
        if (summonPrefab == null)
        {
            return;
        }

        for (int index = 0; index < summonCount; index++)
        {
            float angle = index / (float)summonCount * Mathf.PI * 2f;
            Vector3 spot = transform.position + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * 4.5f;
            GameObject summon = Instantiate(summonPrefab, spot, Quaternion.Euler(0f, angle * Mathf.Rad2Deg, 0f), transform.parent);
            summon.name = $"CrystalShardling_{index:00}";
            summons.Add(summon);
        }

        Debug.Log($"{koreanName} 수정 조각 {summons.Count}개 소환", this);
    }

    private void ClearSummons()
    {
        foreach (GameObject summon in summons.Where(item => item != null))
        {
            Destroy(summon);
        }

        summons.Clear();
    }

    private void OnDied(CombatHitData hit) // 보스를 쓰러뜨렸다
    {
        defeated = true;
        fighting = false;
        ClearSummons();
        CloseGate(false);
        CombatDamagePopup.SpawnText(transform.position + Vector3.up * 3.2f, $"{koreanName}을(를) 쓰러뜨렸다!", new Color(1f, 0.86f, 0.42f, 1f), 3.4f);
        Debug.Log($"{koreanName} 격파", this);
        ApplyReward();
    }

    private void ApplyReward() // 보상 : 깊은층 광맥이 더 빨리 다시 생긴다
    {
        int changed = 0;

        foreach (GatherableResource vein in FindObjectsByType<GatherableResource>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!vein.name.StartsWith("CaveVein_deep", System.StringComparison.Ordinal))
            {
                continue;
            }

            vein.SetRespawnDelay(vein.RespawnDelay * 0.7f);
            changed++;
        }

        if (changed > 0)
        {
            Debug.Log($"보스 보상 : 깊은층 광맥 {changed}곳이 더 빨리 다시 생깁니다.", this);
        }
    }

    private void CloseGate(bool closed)
    {
        if (gateObject != null)
        {
            gateObject.SetActive(closed);
        }
    }

    private static float Flat(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private bool FindPlayer()
    {
        if (player == null)
        {
            PlayerInventory inventory = FindFirstObjectByType<PlayerInventory>();
            player = inventory != null ? inventory.transform : null;
        }

        return player != null;
    }

    // ---------------------------------------------------------------- 저장

    public bool CaptureDefeated() => defeated; // 저장

    public void ApplyDefeated(bool savedDefeated) // 불러오기
    {
        defeated = savedDefeated;

        if (!defeated)
        {
            return;
        }

        fighting = false;
        ClearSummons();
        CloseGate(false);
        ApplyReward();
        gameObject.SetActive(false); // 이미 쓰러뜨린 보스는 나오지 않는다
    }
}
