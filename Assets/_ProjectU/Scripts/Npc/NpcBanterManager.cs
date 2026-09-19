using System.Collections.Generic; // 목록
using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class NpcBanterManager : MonoBehaviour // 114일차: 관계 있는 이웃 둘이 같은 장소에 서 있으면 서로 바라보며 말풍선으로 몇 마디 나눔 (쌍마다 하루 한 번, 여러 쌍이 동시에 가능)
{
    [Tooltip("관계 목록 (자동 적용이 연결).")]
    [SerializeField] private NpcRelationBook book; // 관계 목록
    [Tooltip("두 NPC가 이 거리 안에 서 있어야 대화합니다 (m).")]
    [SerializeField, Min(1f)] private float meetDistance = 6f; // 만남 거리
    [Tooltip("플레이어가 이 거리 안에 있을 때만 대화합니다 (m).")]
    [SerializeField, Min(5f)] private float playerRange = 30f; // 보이는 거리
    [Tooltip("한 줄 말풍선 간격 (초).")]
    [SerializeField, Min(1f)] private float lineSeconds = 3.4f; // 줄 간격
    [Tooltip("만남 확인 간격 (초).")]
    [SerializeField, Min(0.2f)] private float checkInterval = 1f; // 확인 간격
    [Tooltip("동시에 나눌 수 있는 대화 수.")]
    [SerializeField, Min(1)] private int maxConversations = 4; // 동시 대화

    private sealed class Conversation
    {
        public NpcRelationBook.Pair Pair;
        public NpcRelationBook.Banter Banter;
        public NpcAgent A;
        public NpcAgent B;
        public int LineIndex;
        public float LineTimer;
    }

    private readonly Dictionary<string, int> talkedDay = new Dictionary<string, int>(); // 쌍마다 대화한 날
    private readonly List<Conversation> conversations = new List<Conversation>(); // 지금 나누는 대화
    private float checkTimer;
    private Transform player;

    public static NpcBanterManager Instance { get; private set; } // Scene 관리자
    public NpcRelationBook Book => book; // 관계 목록
    public string ActivePairId => conversations.Count > 0 ? conversations[0].Pair.pairId : string.Empty; // 지금 대화 중인 첫 쌍 (테스트용)
    public int ActiveCount => conversations.Count; // 지금 대화 수 (테스트용)
    public int CompletedCount { get; private set; } // 끝까지 나눈 대화 수 (테스트용)

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public bool IsActive(string pairId) => conversations.Exists(item => item.Pair.pairId == pairId); // 지금 대화 중인지

    public bool TalkedToday(string pairId) // 오늘 이미 나눴는지 (중간에 끊겨도 포함)
    {
        NpcManager manager = NpcManager.Instance;
        return manager != null && talkedDay.TryGetValue(pairId, out int day) && day == manager.CurrentDay;
    }

    public static IEnumerable<string> MentionLinesFor(NpcCharacterData character) // 대화하기에 섞을 이웃 이야기 (NpcDialogueSelector)
    {
        NpcBanterManager manager = Instance;

        if (manager == null || manager.book == null || character == null)
        {
            yield break;
        }

        foreach (string line in manager.book.MentionsBy(character.CharacterId))
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                yield return line;
            }
        }
    }

    private void Update()
    {
        for (int index = conversations.Count - 1; index >= 0; index--)
        {
            Advance(conversations[index], Time.deltaTime);
        }

        checkTimer -= Time.deltaTime;

        if (checkTimer > 0f)
        {
            return;
        }

        checkTimer = checkInterval;
        TryStart();
    }

    // 대화에 낄 수 있는지 : 일정 자리에 도착해 서 있고, 플레이어와 대화 · 동행 · 집 안 · 다른 이웃과 대화 중이 아님
    private static bool Ready(NpcAgent agent)
    {
        return agent != null && agent.isActiveAndEnabled && agent.HasArrived && !string.IsNullOrEmpty(agent.CurrentLocationId)
            && !agent.IsTalking && !agent.IsFollowing && !agent.IsInside && !agent.IsTravelingFast && !agent.IsChatting;
    }

    private static float Flat(Vector3 a, Vector3 b) => new Vector2(a.x - b.x, a.z - b.z).magnitude;

    private void TryStart()
    {
        NpcManager manager = NpcManager.Instance;

        if (book == null || manager == null || conversations.Count >= maxConversations || !FindPlayer())
        {
            return;
        }

        int day = manager.CurrentDay;

        foreach (NpcRelationBook.Pair pair in book.Pairs)
        {
            if (conversations.Count >= maxConversations)
            {
                return;
            }

            if (pair == null || pair.banters.Count == 0 || (talkedDay.TryGetValue(pair.pairId, out int last) && last == day))
            {
                continue;
            }

            NpcAgent a = manager.FindAgent(pair.characterA);
            NpcAgent b = manager.FindAgent(pair.characterB);

            if (!Ready(a) || !Ready(b) || a.CurrentLocationId != b.CurrentLocationId || Flat(a.transform.position, b.transform.position) > meetDistance)
            {
                continue;
            }

            if (Mathf.Min(Flat(player.position, a.transform.position), Flat(player.position, b.transform.position)) > playerRange)
            {
                continue; // 보는 사람이 없으면 하지 않음
            }

            Begin(pair, a, b, day);
        }
    }

    private void Begin(NpcRelationBook.Pair pair, NpcAgent a, NpcAgent b, int day)
    {
        conversations.Add(new Conversation
        {
            Pair = pair,
            Banter = pair.banters[Mathf.Abs(day + pair.pairId.Length) % pair.banters.Count], // 날마다 다른 대화
            A = a,
            B = b
        });

        talkedDay[pair.pairId] = day; // 중간에 끊겨도 하루 한 번
        a.SetChatPartner(b.transform);
        b.SetChatPartner(a.transform);
    }

    private void Advance(Conversation conversation, float deltaTime)
    {
        NpcAgent a = conversation.A;
        NpcAgent b = conversation.B;
        bool broken = a == null || b == null || a.IsTalking || b.IsTalking || a.IsFollowing || b.IsFollowing
            || !a.HasArrived || !b.HasArrived || a.IsInside || b.IsInside
            || Flat(a.transform.position, b.transform.position) > meetDistance + 3f;

        if (broken)
        {
            Stop(conversation, false); // 플레이어가 말을 걸거나 한쪽이 떠나면 멈춤
            return;
        }

        conversation.LineTimer -= deltaTime;

        if (conversation.LineTimer > 0f)
        {
            return;
        }

        if (conversation.LineIndex >= conversation.Banter.lines.Count)
        {
            Stop(conversation, true);
            return;
        }

        NpcRelationBook.Line line = conversation.Banter.lines[conversation.LineIndex];
        (line.byA ? a : b).Say(line.text, lineSeconds + 0.4f);
        conversation.LineIndex++;
        conversation.LineTimer = lineSeconds;
    }

    private void Stop(Conversation conversation, bool completed)
    {
        if (conversation.A != null)
        {
            conversation.A.SetChatPartner(null);
        }

        if (conversation.B != null)
        {
            conversation.B.SetChatPartner(null);
        }

        CompletedCount += completed ? 1 : 0;
        conversations.Remove(conversation);
    }

    public void ResetForLoad() // 불러오기 : 대화를 멈추고 오늘 기록을 지움 (대화는 분위기용이라 저장하지 않음)
    {
        for (int index = conversations.Count - 1; index >= 0; index--)
        {
            Stop(conversations[index], false);
        }

        talkedDay.Clear();
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

#if UNITY_EDITOR
    public void EditorAssign(NpcRelationBook relationBook) // 생성 도구 전용
    {
        book = relationBook;
    }
#endif
}
