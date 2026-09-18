using System; // Serializable · 이벤트
using System.Collections.Generic; // 목록
using System.Text; // 문자열
using UnityEngine; // Unity 기본 기능

[Serializable] // JSON 저장
public sealed class NpcEventRecordSaveData // 94일차: 본 이벤트 하나
{
    [Tooltip("이벤트 ID.")]
    public string eventId = string.Empty; // 이벤트 ID
    [Tooltip("고른 선택지 번호 (0부터).")]
    public int choiceIndex; // 선택지
    [Tooltip("본 날짜.")]
    public int day; // 날짜
}

[Serializable] // JSON 저장
public sealed class NpcEventSaveData // 94일차: 본 이벤트 목록
{
    [Tooltip("본 이벤트.")]
    public List<NpcEventRecordSaveData> seen = new List<NpcEventRecordSaveData>(); // 기록
}

public readonly struct NpcEventResult // 94일차: 이벤트 결과
{
    public readonly int Affinity; // 실제 호감도 변화
    public readonly ItemData Item; // 받은 아이템
    public readonly int ItemAmount; // 받은 수
    public readonly AffinityStage Before; // 이전 단계
    public readonly AffinityStage After; // 새 단계

    public NpcEventResult(int affinity, ItemData item, int itemAmount, AffinityStage before, AffinityStage after)
    {
        Affinity = affinity;
        Item = item;
        ItemAmount = itemAmount;
        Before = before;
        After = after;
    }
}

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class NpcEventManager : MonoBehaviour // 94일차: NPC 하트 이벤트 (단계 도달 → 장소·시간·날씨가 맞으면 … 표시 → 말을 걸면 장면)
{
    public static NpcEventManager Instance { get; private set; } // Scene 관리자

    [Tooltip("NPC별 이벤트 묶음.")]
    [SerializeField] private List<NpcEventBook> books = new List<NpcEventBook>(); // 이벤트 묶음
    [Tooltip("NPC 일정 · 날짜 · 날씨.")]
    [SerializeField] private NpcManager npcManager; // NPC 관리자
    [Tooltip("호감도 (단계 조건 · 선택 결과).")]
    [SerializeField] private NpcRelationshipManager relations; // 관계
    [Tooltip("머리 위 표시를 확인하는 간격 (초).")]
    [SerializeField, Min(0.1f)] private float markerInterval = 0.5f; // 확인 간격

    private readonly Dictionary<string, (NpcEventBook book, NpcEventBook.Event data)> lookup = new Dictionary<string, (NpcEventBook, NpcEventBook.Event)>(StringComparer.Ordinal); // ID 검색
    private readonly Dictionary<string, NpcEventRecordSaveData> seen = new Dictionary<string, NpcEventRecordSaveData>(StringComparer.Ordinal); // 본 이벤트
    private float markerTimer; // 표시 타이머

    public event Action<NpcEventBook.Event, NpcCharacterData, int> EventCompleted; // 이벤트 완료 (이벤트, NPC, 고른 선택지)

    public IReadOnlyList<NpcEventBook> Books => books; // 이벤트 묶음 제공
    public int SeenCount => seen.Count; // 본 이벤트 수
    public int CurrentDay => npcManager != null ? npcManager.CurrentDay : 1; // 날짜

    private void Awake() // 준비
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("NpcEventManager가 두 개 있습니다. 나중 것은 사용하지 않습니다.", this);
            enabled = false;
            return;
        }

        Instance = this;

        if (npcManager == null) npcManager = GetComponent<NpcManager>();
        if (relations == null) relations = GetComponent<NpcRelationshipManager>();

        if (npcManager == null)
        {
            Debug.LogError("NpcEventManager에 NPC 관리자가 없습니다. Build Content > 12. NPC Events를 다시 실행하세요.", this);
        }

        BuildLookup();
    }

    private void OnDestroy() // 정리
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update() // 머리 위 … 표시
    {
        markerTimer -= Time.deltaTime;

        if (markerTimer > 0f)
        {
            return;
        }

        markerTimer = markerInterval;
        RefreshMarkers();
    }

    private void BuildLookup() // ID 검색 표
    {
        lookup.Clear();

        foreach (NpcEventBook book in books)
        {
            if (book == null)
            {
                continue;
            }

            foreach (NpcEventBook.Event data in book.Events)
            {
                if (data != null && !string.IsNullOrEmpty(data.EventId) && !lookup.ContainsKey(data.EventId))
                {
                    lookup.Add(data.EventId, (book, data));
                }
            }
        }
    }

    // ------------------------------------------------------------ 찾기

    public bool TryGetEvent(string eventId, out NpcEventBook.Event data) // ID로 이벤트
    {
        data = null;

        if (string.IsNullOrEmpty(eventId) || !lookup.TryGetValue(eventId, out (NpcEventBook book, NpcEventBook.Event data) found))
        {
            return false;
        }

        data = found.data;
        return true;
    }

    public bool HasSeen(string eventId) // 본 이벤트인지
    {
        return !string.IsNullOrEmpty(eventId) && seen.ContainsKey(eventId);
    }

    public int GetChoice(string eventId) // 고른 선택지 (-1 = 안 봄)
    {
        return seen.TryGetValue(eventId ?? string.Empty, out NpcEventRecordSaveData record) ? record.choiceIndex : -1;
    }

    private NpcEventBook GetBook(string characterId) // NPC의 이벤트 묶음
    {
        return books.Find(book => book != null && book.OwnerId == characterId);
    }

    public NpcEventBook.Event GetNextEvent(NpcCharacterData character) // 관계 단계가 된 이벤트 중 아직 안 본 첫 이벤트 (단계 순서대로)
    {
        NpcEventBook book = character != null ? GetBook(character.CharacterId) : null;

        if (book == null || relations == null)
        {
            return null;
        }

        AffinityStage stage = relations.PeekStage(character);
        NpcEventBook.Event next = null;

        foreach (NpcEventBook.Event data in book.Events)
        {
            if (data != null && !HasSeen(data.EventId) && data.RequiredStage <= stage && (next == null || data.RequiredStage < next.RequiredStage))
            {
                next = data;
            }
        }

        return next;
    }

    public bool SawEventToday(string characterId) // 오늘 이미 이 NPC의 이벤트를 봤는지 (하루 한 번)
    {
        NpcEventBook book = GetBook(characterId);

        if (book == null)
        {
            return false;
        }

        foreach (NpcEventBook.Event data in book.Events)
        {
            if (data != null && seen.TryGetValue(data.EventId, out NpcEventRecordSaveData record) && record.day == CurrentDay)
            {
                return true;
            }
        }

        return false;
    }

    public bool TryGetReadyEvent(NpcAgent agent, out NpcEventBook.Event data) // 지금 말을 걸면 시작할 이벤트
    {
        data = null;

        if (agent == null || agent.Character == null || npcManager == null || agent.IsInside || SawEventToday(agent.CharacterId))
        {
            return false;
        }

        NpcEventBook.Event next = GetNextEvent(agent.Character);

        if (next == null || agent.CurrentLocationId != next.LocationId || !agent.HasArrived || !next.IsInTimeWindow(npcManager.CurrentHour) || !NpcCalendar.Matches(next.Weather, npcManager.CurrentWeather))
        {
            return false;
        }

        data = next;
        return true;
    }

    public string DescribeWaiting(NpcCharacterData character) // 기다리는 이벤트 조건 안내 (디버그 · 테스트)
    {
        NpcEventBook.Event next = GetNextEvent(character);
        return next == null ? "없음" : $"{next.EventId} · {next.LocationId} · {MarketStall.FormatHour(next.FromHour)}~{MarketStall.FormatHour(next.ToHour)} · {next.Weather}";
    }

    // ------------------------------------------------------------ 완료

    public bool Complete(string eventId, int choiceIndex, PlayerInventory inventory, out NpcEventResult result) // 선택 결과 적용 · 기록
    {
        result = default;

        if (!lookup.TryGetValue(eventId ?? string.Empty, out (NpcEventBook book, NpcEventBook.Event data) found) || HasSeen(eventId) || choiceIndex < 0 || choiceIndex >= found.data.Choices.Count)
        {
            return false;
        }

        NpcCharacterData owner = null;
        npcManager?.Database?.TryGet(found.book.OwnerId, out owner);
        AffinityStage before = relations != null && owner != null ? relations.GetStage(owner) : AffinityStage.Uninterested;
        int affinityBefore = relations != null && owner != null ? relations.GetAffinity(owner) : 0;

        if (relations != null && owner != null)
        {
            relations.MarkMet(owner);
            relations.ChangeAffinity(owner, found.data.Choices[choiceIndex].Affinity);
        }

        int given = 0;

        if (found.data.RewardItem != null && inventory != null)
        {
            given = found.data.RewardItemAmount - inventory.AddItem(found.data.RewardItem, found.data.RewardItemAmount);
        }

        seen[eventId] = new NpcEventRecordSaveData { eventId = eventId, choiceIndex = choiceIndex, day = CurrentDay };
        int affinityAfter = relations != null && owner != null ? relations.GetAffinity(owner) : 0;
        AffinityStage after = relations != null && owner != null ? relations.GetStage(owner) : before;
        result = new NpcEventResult(affinityAfter - affinityBefore, found.data.RewardItem, given, before, after);
        EventCompleted?.Invoke(found.data, owner, choiceIndex);
        RefreshMarkers();
        return true;
    }

    public void RefreshMarkers() // 이벤트를 시작할 수 있는 NPC 머리 위 … 표시
    {
        if (npcManager == null)
        {
            return;
        }

        foreach (NpcAgent agent in npcManager.Agents)
        {
            if (agent != null)
            {
                agent.SetEventMarker(TryGetReadyEvent(agent, out _));
            }
        }
    }

    // ------------------------------------------------------------ 저장

    public NpcEventSaveData CaptureSaveData() // 저장 데이터
    {
        NpcEventSaveData data = new NpcEventSaveData();

        foreach (NpcEventRecordSaveData record in seen.Values)
        {
            data.seen.Add(new NpcEventRecordSaveData { eventId = record.eventId, choiceIndex = record.choiceIndex, day = record.day });
        }

        data.seen.Sort((left, right) => string.CompareOrdinal(left.eventId, right.eventId));
        return data;
    }

    public void ApplySaveData(NpcEventSaveData data) // 불러오기 (데이터에서 사라진 이벤트는 버림)
    {
        seen.Clear();

        foreach (NpcEventRecordSaveData record in data?.seen ?? new List<NpcEventRecordSaveData>())
        {
            if (record != null && lookup.TryGetValue(record.eventId, out (NpcEventBook book, NpcEventBook.Event data) found) && !seen.ContainsKey(record.eventId))
            {
                seen.Add(record.eventId, new NpcEventRecordSaveData { eventId = record.eventId, choiceIndex = Mathf.Clamp(record.choiceIndex, 0, Mathf.Max(0, found.data.Choices.Count - 1)), day = record.day });
            }
        }

        RefreshMarkers();
    }

    public void ResetForLoad() // 94일차 이전 저장 파일 : 본 이벤트 없음
    {
        seen.Clear();
        RefreshMarkers();
    }

    // ------------------------------------------------------------ 테스트

    public string Describe() // 디버그용 상태
    {
        StringBuilder text = new StringBuilder($"[NPC 이벤트] DAY {CurrentDay} {MarketStall.FormatHour(npcManager != null ? npcManager.CurrentHour : 0f)} · 본 이벤트 {seen.Count}개");

        foreach (NpcEventBook book in books)
        {
            NpcCharacterData owner = null;
            npcManager?.Database?.TryGet(book.OwnerId, out owner);
            NpcAgent agent = npcManager != null ? npcManager.FindAgent(book.OwnerId) : null;
            bool ready = TryGetReadyEvent(agent, out _);
            text.Append($"\n{book.OwnerId} : 다음 {DescribeWaiting(owner)}{(ready ? " · 지금 시작 가능" : string.Empty)}");
        }

        return text.ToString();
    }

#if UNITY_EDITOR
    public void EditorAssign(List<NpcEventBook> bookList, NpcManager manager, NpcRelationshipManager relationshipManager) // 생성 도구 전용
    {
        books = bookList ?? new List<NpcEventBook>();
        npcManager = manager;
        relations = relationshipManager;
        lookup.Clear();
    }
#endif
}
