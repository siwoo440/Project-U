using System; // 직렬화 · 이벤트 기능
using UnityEngine; // Unity 기본 기능

[Serializable]
public sealed class NpcCompanionSaveData // 109일차: 동료 저장
{
    [Tooltip("지금 함께 다니는 NPC (없으면 빈 값).")]
    public string companionId = string.Empty; // 동료 ID
    [Tooltip("이번 동행에서 함께한 게임 시간 (시간).")]
    public float hoursTogether; // 함께한 시간
    [Tooltip("동행 호감도를 받은 날.")]
    public int affinityDay; // 호감도 날짜
    [Tooltip("그날 받은 동행 호감도.")]
    public int affinityGained; // 그날 받은 호감도
    [Tooltip("채집형 동료가 찾아 준 날.")]
    public int gatherDay; // 채집 날짜
    [Tooltip("그날 찾아 준 횟수.")]
    public int gathered; // 그날 채집 횟수
    [Tooltip("115일차: 기력이 다 떨어져 돌아간 동료 (그날은 다시 함께 가지 않음).")]
    public string tiredId = string.Empty; // 지친 동료 ID
    [Tooltip("지쳐서 돌아간 날.")]
    public int tiredDay = -1; // 지친 날
}

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class NpcCompanionManager : MonoBehaviour // 109일차: 동료 (친해진 NPC가 따라다니며 전투 · 채집 · 치유)
{
    public const string AlreadyMessage = "이미 다른 동료와 함께 다니고 있어요."; // 한 명 제한
    public const string NightMessage = "밤이 늦었어요. 내일 함께 가요."; // 밤에는 합류 안 함
    public const string JoinedMessage = "동료가 되었어요"; // 합류 알림
    public const string LeftMessage = "동료가 돌아갔어요"; // 헤어짐 알림
    public const string TiredMessage = "오늘은 너무 지쳤어요. 내일 다시 함께 가요."; // 115일차: 지쳐서 돌아간 날
    public const string ExhaustedText = "지쳐서 돌아갔어요"; // 115일차: 기력이 다 떨어짐 알림

    [Tooltip("동료 정보 (25번 메뉴가 연결).")]
    [SerializeField] private NpcCompanionBook book; // 동료 정보
    [Tooltip("전투형 동료가 적을 찾는 거리 (플레이어 기준, m).")]
    [SerializeField, Min(2f)] private float fightRadius = 14f; // 전투 거리
    [Tooltip("전투형 동료가 때리는 거리 (m).")]
    [SerializeField, Min(0.5f)] private float hitReach = 2.4f; // 때리는 거리
    [Tooltip("치유형 동료가 회복해 주는 체력 비율 (이 아래일 때).")]
    [SerializeField, Range(0.1f, 0.95f)] private float healBelow = 0.5f; // 치유 기준
    [Tooltip("채집형 동료가 하루에 찾아 주는 횟수.")]
    [SerializeField, Min(1)] private int dailyGatherLimit = 8; // 하루 채집
    [Tooltip("채집형 동료는 마을 밖(가운데에서 이 거리)에서만 찾아 줍니다.")]
    [SerializeField, Min(0f)] private float gatherOutside = 140f; // 마을 밖
    [Tooltip("함께한 게임 시간 몇 시간마다 호감도 +1.")]
    [SerializeField, Min(0.5f)] private float hoursPerAffinity = 2f; // 호감도 간격
    [Tooltip("동행으로 하루에 오르는 호감도 한도.")]
    [SerializeField, Min(0)] private int dailyAffinityLimit = 3; // 하루 한도
    [Tooltip("혼잣말 간격 (초).")]
    [SerializeField] private Vector2 idleLineSeconds = new Vector2(35f, 70f); // 혼잣말 간격

    private NpcAgent companion; // 지금 동료
    private NpcCompanionBook.Entry entry; // 동료 정보
    private Transform player; // 플레이어
    private PlayerHealth playerHealth; // 플레이어 체력
    private PlayerInventory inventory; // 가방
    private float actionTimer; // 역할 행동 간격
    private float idleTimer; // 혼잣말 간격
    private float lastHour = -1f; // 지난 시각 (함께한 시간 계산)
    private int lastDay = -1; // 지난 날
    private int attackSequence = 900000; // 공격 번호 (적 중복 피해 방지)
    private NpcCompanionSaveData state = new NpcCompanionSaveData(); // 저장 상태
    private NpcCompanionVitality vitality; // 115일차: 지금 동료의 기력

    public static NpcCompanionManager Instance { get; private set; } // Scene 관리자
    public NpcCompanionBook Book => book; // 동료 정보
    public NpcAgent Companion => companion; // 지금 동료
    public NpcCompanionBook.Entry CurrentEntry => entry; // 지금 동료 정보
    public bool HasCompanion => companion != null; // 동료 여부
    public int HitCount { get; private set; } // 전투형 동료가 때린 횟수 (테스트용)
    public int HealCount { get; private set; } // 치유 횟수 (테스트용)
    public int GatherCount => state.gathered; // 오늘 채집 횟수 (테스트용)
    public float HoursTogether => state.hoursTogether; // 함께한 시간 (테스트용)
    public NpcCompanionVitality Vitality => companion != null ? vitality : null; // 115일차: 지금 동료의 기력
    public int ExhaustedCount { get; private set; } // 지쳐서 돌아간 횟수 (테스트용)

    public event Action<NpcAgent> CompanionChanged; // 동료가 바뀜 (없으면 null)

    private void Awake() // 준비
    {
        Instance = this;
        actionTimer = 1f;
        idleTimer = UnityEngine.Random.Range(idleLineSeconds.x, idleLineSeconds.y);
    }

    private void OnDestroy() // 정리
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private bool FindPlayer()
    {
        if (player != null)
        {
            return true;
        }

        PlayerInventory found = FindFirstObjectByType<PlayerInventory>();

        if (found == null)
        {
            return false;
        }

        inventory = found;
        player = found.transform;
        playerHealth = found.GetComponent<PlayerHealth>();
        return true;
    }

    // ---------------------------------------------------------------- 합류 · 헤어짐

    public NpcCompanionBook.Entry EntryFor(NpcCharacterData character) // 동료가 될 수 있는 NPC면 정보 (아니면 null)
    {
        NpcCompanionBook.Entry found = book != null && character != null ? book.Get(character.CharacterId) : null;
        return found != null && IsUnlocked(character) ? found : null;
    }

    public static bool IsUnlocked(NpcCharacterData character) // 110일차: 의뢰 · 하트 이벤트가 붙은 차수만 동료가 됨 (4차 후보는 이야기가 붙을 때까지 잠김)
    {
        return character != null && character.CastWave <= NpcDatabase.StoryReadyWave;
    }

    public bool IsCompanion(NpcAgent agent) => agent != null && agent == companion; // 지금 동료인지

    public bool CanRecruit(NpcAgent agent, out string reason) // 함께 갈 수 있는지 (안 되면 이유 · 거절 대사)
    {
        NpcCompanionBook.Entry candidate = agent != null ? EntryFor(agent.Character) : null;
        NpcManager manager = NpcManager.Instance;
        NpcRelationshipManager relations = NpcRelationshipManager.Instance;

        if (candidate == null || manager == null || relations == null)
        {
            reason = "함께 다닐 수 없는 이웃이에요.";
            return false;
        }

        if (companion != null)
        {
            reason = companion == agent ? string.Empty : AlreadyMessage;
            return false;
        }

        if (manager.IsSleepTime(manager.CurrentHour))
        {
            reason = NightMessage;
            return false;
        }

        if (relations.GetStage(agent.Character) < candidate.requiredStage)
        {
            reason = candidate.refuseLine;
            return false;
        }

        if (state.tiredDay == manager.CurrentDay && state.tiredId == agent.CharacterId) // 115일차: 지쳐서 돌아간 날은 쉼
        {
            reason = TiredMessage;
            return false;
        }

        reason = string.Empty;
        return true;
    }

    public bool Recruit(NpcAgent agent, out string message) // 동료로 데려가기
    {
        if (!CanRecruit(agent, out message) || !FindPlayer())
        {
            return false;
        }

        companion = agent;
        entry = EntryFor(agent.Character);
        state = new NpcCompanionSaveData { companionId = agent.CharacterId, affinityDay = state.affinityDay, affinityGained = state.affinityGained, gatherDay = state.gatherDay, gathered = state.gathered, tiredId = state.tiredId, tiredDay = state.tiredDay };
        lastHour = -1f;
        actionTimer = 1f;
        agent.BeginFollow(player, "동행");
        StartVitality(agent);
        message = entry.joinLine;
        CompanionChanged?.Invoke(companion);
        return true;
    }

    public void Dismiss(bool night, bool speak = true) // 헤어지기 (일정으로 돌아감, 대화 창에서 부르면 말풍선 없이)
    {
        if (companion == null)
        {
            return;
        }

        NpcAgent leaving = companion;
        string line = night ? entry.nightLine : entry.leaveLine;
        leaving.SetEngageTarget(false, Vector3.zero);
        leaving.EndFollow();
        StopVitality();

        if (speak)
        {
            leaving.Say(line, 4f);
        }

        companion = null;
        entry = null;
        state.companionId = string.Empty;
        state.hoursTogether = 0f;
        CompanionChanged?.Invoke(null);
    }

    public void HandleExhausted(NpcAgent agent) // 115일차: 기력이 다 떨어지면 그날은 지쳐서 집으로 (NpcCompanionVitality)
    {
        NpcManager npcManager = NpcManager.Instance;

        if (agent == null || agent != companion || npcManager == null)
        {
            return;
        }

        state.tiredId = agent.CharacterId;
        state.tiredDay = npcManager.CurrentDay;
        ExhaustedCount++;
        CombatDamagePopup.SpawnText(agent.transform.position + Vector3.up * 2.6f, $"{agent.DisplayName} · {ExhaustedText}", new Color(1f, 0.72f, 0.4f, 1f), 1.6f);
        Dismiss(false);
    }

    private void StartVitality(NpcAgent agent) // 115일차: 동료 기력 켜기 (적이 노릴 수 있음)
    {
        StopVitality();
        vitality = agent.GetComponent<NpcCompanionVitality>();

        if (vitality == null)
        {
            vitality = agent.gameObject.AddComponent<NpcCompanionVitality>();
        }

        vitality.Begin(NpcCompanionVitality.MaximumFor(entry.role));
    }

    private void StopVitality() // 동료 기력 끄기
    {
        if (vitality != null)
        {
            vitality.End();
        }

        vitality = null;
    }

    // ---------------------------------------------------------------- 매 프레임

    private void Update()
    {
        if (companion == null || entry == null || !FindPlayer())
        {
            return;
        }

        NpcManager manager = NpcManager.Instance;

        if (manager != null && manager.IsSleepTime(manager.CurrentHour)) // 밤에는 집으로
        {
            Dismiss(true);
            return;
        }

        CountTimeTogether(manager);

        if (playerHealth != null && playerHealth.IsDead)
        {
            companion.SetEngageTarget(false, Vector3.zero);
            return;
        }

        actionTimer -= Time.deltaTime;

        switch (entry.role)
        {
            case NpcCompanionRole.Fighter:
                UpdateFighter();
                break;
            case NpcCompanionRole.Gatherer:
                UpdateGatherer(manager);
                break;
            default:
                UpdateHealer();
                break;
        }

        idleTimer -= Time.deltaTime;

        if (idleTimer <= 0f && entry.idleLines.Count > 0 && !companion.IsTalking)
        {
            idleTimer = UnityEngine.Random.Range(idleLineSeconds.x, idleLineSeconds.y);
            companion.Say(entry.idleLines[UnityEngine.Random.Range(0, entry.idleLines.Count)], 3.5f);
        }
    }

    private void CountTimeTogether(NpcManager manager) // 함께한 시간 · 동행 호감도
    {
        if (manager == null)
        {
            return;
        }

        int day = manager.CurrentDay;
        float hour = manager.CurrentHour;

        if (lastHour >= 0f && day == lastDay && hour >= lastHour)
        {
            state.hoursTogether += hour - lastHour;
        }

        lastHour = hour;
        lastDay = day;

        if (state.affinityDay != day)
        {
            state.affinityDay = day;
            state.affinityGained = 0;
        }

        NpcRelationshipManager relations = NpcRelationshipManager.Instance;

        if (relations != null && state.hoursTogether >= hoursPerAffinity && state.affinityGained < dailyAffinityLimit)
        {
            state.hoursTogether -= hoursPerAffinity;
            state.affinityGained++;
            relations.ChangeAffinity(companion.Character, 1);
            CombatDamagePopup.SpawnText(companion.transform.position + Vector3.up * 2.4f, "♥ +1", new Color(0.95f, 0.55f, 0.66f, 1f), 1.6f);
        }
    }

    private void UpdateFighter() // 전투 : 플레이어 둘레의 가장 가까운 적에게 다가가 때림
    {
        EnemyHealth target = null;
        float best = fightRadius * fightRadius;

        foreach (EnemyHealth enemy in FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
        {
            if (enemy == null || !enemy.IsAlive)
            {
                continue;
            }

            float distance = (enemy.transform.position - player.position).sqrMagnitude;

            if (distance < best)
            {
                best = distance;
                target = enemy;
            }
        }

        if (target == null)
        {
            companion.SetEngageTarget(false, Vector3.zero);
            return;
        }

        companion.SetEngageTarget(true, target.transform.position);
        Vector3 toEnemy = target.transform.position - companion.transform.position;
        toEnemy.y = 0f;

        if (toEnemy.magnitude > hitReach || actionTimer > 0f)
        {
            return;
        }

        actionTimer = entry.interval;
        companion.FaceTowards(target.transform.position);
        Collider hitCollider = target.GetComponentInChildren<Collider>();
        Vector3 hitPoint = target.transform.position + Vector3.up;
        CombatHitData hit = new CombatHitData(companion.gameObject, null, WeaponAttackType.Melee, entry.power, 2f, hitPoint, toEnemy, hitCollider, ++attackSequence, 0);

        if (target.ReceiveDamage(hit))
        {
            HitCount++;

            if (HitCount % 4 == 1)
            {
                companion.Say(entry.roleLine, 2f);
            }
        }
    }

    private void UpdateGatherer(NpcManager manager) // 채집 : 마을 밖을 함께 다니면 가끔 물건을 찾아 줌 (하루 한도)
    {
        if (actionTimer > 0f || entry.lootItems.Count == 0 || inventory == null)
        {
            return;
        }

        actionTimer = entry.interval;
        int day = manager != null ? manager.CurrentDay : 1;

        if (state.gatherDay != day)
        {
            state.gatherDay = day;
            state.gathered = 0;
        }

        if (state.gathered >= dailyGatherLimit || new Vector2(player.position.x, player.position.z).magnitude < gatherOutside || PlayerSwimming.IsLocalSwimming)
        {
            return;
        }

        ItemData item = entry.lootItems[UnityEngine.Random.Range(0, entry.lootItems.Count)];
        int amount = Mathf.Max(1, Mathf.RoundToInt(entry.power));

        if (item == null || inventory.AddItem(item, amount) >= amount)
        {
            return; // 가방이 가득 참
        }

        state.gathered++;
        companion.Say(entry.roleLine.Replace("{item}", item.KoreanName), 3f); // 115일차: 한글 이름
        CombatDamagePopup.SpawnText(player.position + Vector3.up * 2.2f, $"+{amount} {item.KoreanName}", new Color(0.75f, 0.92f, 0.6f, 1f), 1.8f);
    }

    private void UpdateHealer() // 치유 : 플레이어 체력이 낮으면 회복 (대기 시간)
    {
        if (actionTimer > 0f || playerHealth == null || playerHealth.NormalizedHealth >= healBelow)
        {
            return;
        }

        if (playerHealth.Heal(entry.power))
        {
            actionTimer = entry.interval;
            HealCount++;
            companion.Say(entry.roleLine, 3f);
            CombatDamagePopup.SpawnText(player.position + Vector3.up * 2.2f, $"+{Mathf.RoundToInt(entry.power)}", new Color(0.55f, 0.95f, 0.6f, 1f), 1.8f);
        }
    }

    // ---------------------------------------------------------------- 저장

    public NpcCompanionSaveData CaptureSaveData()
    {
        state.companionId = companion != null ? companion.CharacterId : string.Empty;
        return new NpcCompanionSaveData { companionId = state.companionId, hoursTogether = state.hoursTogether, affinityDay = state.affinityDay, affinityGained = state.affinityGained, gatherDay = state.gatherDay, gathered = state.gathered, tiredId = state.tiredId, tiredDay = state.tiredDay };
    }

    public void ApplySaveData(NpcCompanionSaveData data) // 불러오기 (동료가 있으면 다시 따라다님)
    {
        ResetForLoad();

        if (data == null)
        {
            return;
        }

        state = new NpcCompanionSaveData { affinityDay = data.affinityDay, affinityGained = data.affinityGained, gatherDay = data.gatherDay, gathered = data.gathered, tiredId = data.tiredId ?? string.Empty, tiredDay = data.tiredDay };
        NpcManager manager = NpcManager.Instance;
        NpcAgent agent = manager != null && !string.IsNullOrEmpty(data.companionId) ? manager.FindAgent(data.companionId) : null;

        if (agent == null || EntryFor(agent.Character) == null || !FindPlayer())
        {
            return;
        }

        companion = agent;
        entry = EntryFor(agent.Character);
        state.companionId = agent.CharacterId;
        state.hoursTogether = Mathf.Max(0f, data.hoursTogether);
        lastHour = -1f;
        agent.BeginFollow(player, "동행");
        StartVitality(agent);
        CompanionChanged?.Invoke(companion);
    }

    public void ResetForLoad() // 동료 없이 시작
    {
        if (companion != null)
        {
            companion.SetEngageTarget(false, Vector3.zero);
            companion.EndFollow();
        }

        StopVitality();
        companion = null;
        entry = null;
        state = new NpcCompanionSaveData();
        CompanionChanged?.Invoke(null);
    }

#if UNITY_EDITOR
    public void EditorAssign(NpcCompanionBook companionBook) // 생성 도구 전용
    {
        book = companionBook;
    }
#endif
}
