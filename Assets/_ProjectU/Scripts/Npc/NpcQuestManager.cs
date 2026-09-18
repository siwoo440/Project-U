using System; // Serializable · 이벤트
using System.Collections.Generic; // 목록
using System.Text; // 문자열
using UnityEngine; // Unity 기본 기능

[Serializable] // JSON 저장
public sealed class NpcQuestActiveSaveData // 93일차: 진행 중인 의뢰 하나
{
    [Tooltip("의뢰 ID.")]
    public string questId = string.Empty; // 의뢰 ID
    [Tooltip("받은 날짜.")]
    public int acceptedDay; // 받은 날
    [Tooltip("이 날짜까지 전달해야 함.")]
    public int deadlineDay; // 마감 날짜
}

[Serializable] // JSON 저장
public sealed class NpcQuestRecordSaveData // 93일차: 끝낸 의뢰 기록
{
    [Tooltip("의뢰 ID.")]
    public string questId = string.Empty; // 의뢰 ID
    [Tooltip("끝낸 횟수.")]
    public int completedCount; // 완료 횟수
    [Tooltip("마지막으로 끝낸 날짜.")]
    public int lastCompletedDay; // 마지막 완료 날짜
}

[Serializable] // JSON 저장
public sealed class NpcQuestSaveData // 93일차: 게시판 · 진행 중 의뢰 · 완료 기록
{
    [Tooltip("게시판을 채운 날짜.")]
    public int boardDay; // 게시판 날짜
    [Tooltip("게시판에 남아 있는 의뢰 ID.")]
    public List<string> board = new List<string>(); // 게시판
    [Tooltip("진행 중인 의뢰.")]
    public List<NpcQuestActiveSaveData> active = new List<NpcQuestActiveSaveData>(); // 진행 중
    [Tooltip("끝낸 의뢰 기록.")]
    public List<NpcQuestRecordSaveData> records = new List<NpcQuestRecordSaveData>(); // 완료 기록
    [Tooltip("지금까지 끝낸 의뢰 수.")]
    public int totalCompleted; // 완료 합계
    [Tooltip("기한이 지나 사라진 의뢰 수.")]
    public int totalExpired; // 만료 합계
}

public sealed class NpcActiveQuest // 93일차: 진행 중인 의뢰 (실행 중)
{
    public NpcActiveQuest(NpcQuestBook book, NpcQuestBook.Quest quest, int acceptedDay, int deadlineDay)
    {
        Book = book;
        Quest = quest;
        AcceptedDay = acceptedDay;
        DeadlineDay = deadlineDay;
    }

    public NpcQuestBook Book { get; } // 의뢰 묶음
    public NpcQuestBook.Quest Quest { get; } // 의뢰
    public int AcceptedDay { get; } // 받은 날
    public int DeadlineDay { get; } // 마감 날짜
    public string OwnerId => Book.OwnerId; // 의뢰한 NPC
}

public readonly struct NpcQuestResult // 93일차: 의뢰 완료 결과
{
    public readonly int Coins; // 받은 코인
    public readonly int Affinity; // 오른 호감도
    public readonly ItemData Item; // 받은 아이템
    public readonly int ItemAmount; // 받은 아이템 수
    public readonly AffinityStage Before; // 이전 관계 단계
    public readonly AffinityStage After; // 새 관계 단계

    public NpcQuestResult(int coins, int affinity, ItemData item, int itemAmount, AffinityStage before, AffinityStage after)
    {
        Coins = coins;
        Affinity = affinity;
        Item = item;
        ItemAmount = itemAmount;
        Before = before;
        After = after;
    }
}

public enum NpcQuestMarker // NPC 머리 위 의뢰 표시
{
    None = 0, // 없음
    Active = 1, // 진행 중 (?)
    Ready = 2 // 전달 가능 (!)
}

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class NpcQuestManager : MonoBehaviour // 93일차: 마을 게시판 의뢰 (게시 · 받기 · 전달 · 기한 · 저장)
{
    public static NpcQuestManager Instance { get; private set; } // Scene 관리자

    [Tooltip("NPC별 의뢰 묶음.")]
    [SerializeField] private List<NpcQuestBook> books = new List<NpcQuestBook>(); // 의뢰 묶음
    [Tooltip("NPC 일정 · 날짜 · 계절.")]
    [SerializeField] private NpcManager npcManager; // NPC 관리자
    [Tooltip("호감도 (단계 조건 · 보상).")]
    [SerializeField] private NpcRelationshipManager relations; // 관계
    [Tooltip("게시판에 하루 붙는 의뢰 수.")]
    [SerializeField, Min(1)] private int boardSize = 3; // 게시판 크기
    [Tooltip("동시에 진행할 수 있는 의뢰 수.")]
    [SerializeField, Min(1)] private int activeLimit = 3; // 진행 한도
    [Tooltip("게시판 뽑기 기준 값 (같은 날이면 항상 같은 의뢰).")]
    [SerializeField] private int boardSeed = 9301; // 기준 값

    private readonly Dictionary<string, (NpcQuestBook book, NpcQuestBook.Quest quest)> lookup = new Dictionary<string, (NpcQuestBook, NpcQuestBook.Quest)>(StringComparer.Ordinal); // ID 검색
    private readonly List<string> board = new List<string>(); // 게시판
    private readonly List<NpcActiveQuest> active = new List<NpcActiveQuest>(); // 진행 중
    private readonly Dictionary<string, NpcQuestRecordSaveData> records = new Dictionary<string, NpcQuestRecordSaveData>(StringComparer.Ordinal); // 완료 기록
    private int boardDay = int.MinValue; // 게시판 날짜
    private int lastDay = int.MinValue; // 마지막 처리 날짜
    private int totalCompleted; // 완료 합계
    private int totalExpired; // 만료 합계
    private PlayerInventory inventory; // 인벤토리 (전달 가능 표시)

    public event Action QuestsChanged; // 게시판 · 진행 변경
    public event Action<NpcQuestBook.Quest, NpcCharacterData> QuestCompleted; // 완료
    public event Action<NpcQuestBook.Quest> QuestExpired; // 기한 만료
    public event Action<string> Notice; // 화면 알림 문구

    public IReadOnlyList<NpcQuestBook> Books => books; // 의뢰 묶음 제공
    public IReadOnlyList<string> BoardIds => board; // 게시판 ID 제공
    public IReadOnlyList<NpcActiveQuest> Active => active; // 진행 중 제공
    public int BoardSize => boardSize; // 게시판 크기 제공
    public int ActiveLimit => activeLimit; // 진행 한도 제공
    public int BoardDay => boardDay; // 게시판 날짜 제공
    public int TotalCompleted => totalCompleted; // 완료 합계 제공
    public int TotalExpired => totalExpired; // 만료 합계 제공
    public int CurrentDay => npcManager != null ? npcManager.CurrentDay : 1; // 날짜
    private static PlayerWallet Wallet => MarketManager.Instance != null && MarketManager.Instance.Wallet != null ? MarketManager.Instance.Wallet : PlayerWallet.Local; // 지갑

    private void Awake() // 준비
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("NpcQuestManager가 두 개 있습니다. 나중 것은 사용하지 않습니다.", this);
            enabled = false;
            return;
        }

        Instance = this;

        if (npcManager == null) npcManager = GetComponent<NpcManager>();
        if (relations == null) relations = GetComponent<NpcRelationshipManager>();

        if (npcManager == null)
        {
            Debug.LogError("NpcQuestManager에 NPC 관리자가 없습니다. Build Content > 11. NPC Quests를 다시 실행하세요.", this);
        }

        BuildLookup();
    }

    private void OnEnable() // 인벤토리 변경 구독 (전달 가능 표시)
    {
        inventory = FindFirstObjectByType<PlayerInventory>();

        if (inventory != null)
        {
            inventory.InventoryChanged += RefreshMarkers;
        }
    }

    private void OnDisable() // 구독 해제
    {
        if (inventory != null)
        {
            inventory.InventoryChanged -= RefreshMarkers;
        }
    }

    private void OnDestroy() // 정리
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start() // 첫 게시판 (불러오기로 이미 채웠으면 생략)
    {
        if (lastDay != CurrentDay)
        {
            lastDay = CurrentDay;
            GenerateBoard(CurrentDay);
            Changed();
        }
    }

    private void LateUpdate() // 날짜가 바뀌면 기한 확인 · 게시판 새로 붙이기
    {
        if (npcManager != null && CurrentDay != lastDay)
        {
            ProcessDay(CurrentDay);
        }
    }

    private void BuildLookup() // ID 검색 표
    {
        lookup.Clear();

        foreach (NpcQuestBook book in books)
        {
            if (book == null)
            {
                continue;
            }

            foreach (NpcQuestBook.Quest quest in book.Quests)
            {
                if (quest != null && !string.IsNullOrEmpty(quest.QuestId) && !lookup.ContainsKey(quest.QuestId))
                {
                    lookup.Add(quest.QuestId, (book, quest));
                }
            }
        }
    }

    // ------------------------------------------------------------ 찾기

    public bool TryGetQuest(string questId, out NpcQuestBook book, out NpcQuestBook.Quest quest) // ID로 의뢰
    {
        book = null;
        quest = null;

        if (string.IsNullOrEmpty(questId) || !lookup.TryGetValue(questId, out (NpcQuestBook book, NpcQuestBook.Quest quest) found))
        {
            return false;
        }

        book = found.book;
        quest = found.quest;
        return true;
    }

    public NpcCharacterData GetOwner(NpcQuestBook book) // 의뢰한 NPC 데이터
    {
        NpcCharacterData owner = null;

        if (book != null && npcManager != null && npcManager.Database != null)
        {
            npcManager.Database.TryGet(book.OwnerId, out owner);
        }

        return owner;
    }

    public NpcCharacterData GetOwner(string questId) // 의뢰 ID → NPC 데이터
    {
        return TryGetQuest(questId, out NpcQuestBook book, out _) ? GetOwner(book) : null;
    }

    public NpcActiveQuest GetActive(string questId) // 진행 중인 의뢰
    {
        return active.Find(entry => entry.Quest.QuestId == questId);
    }

    public NpcActiveQuest GetActiveFor(NpcCharacterData character) // 이 NPC가 준 진행 중 의뢰
    {
        return character != null ? active.Find(entry => entry.OwnerId == character.CharacterId) : null;
    }

    public int GetCompletedCount(string questId) // 끝낸 횟수
    {
        return records.TryGetValue(questId ?? string.Empty, out NpcQuestRecordSaveData record) ? record.completedCount : 0;
    }

    public int DaysLeft(NpcActiveQuest entry) // 남은 날 (1 = 오늘까지)
    {
        return entry != null ? entry.DeadlineDay - CurrentDay + 1 : 0;
    }

    public static string DaysLeftText(int daysLeft) // "오늘까지" · "2일 남음"
    {
        return daysLeft <= 1 ? "오늘까지" : $"{daysLeft}일 남음";
    }

    // ------------------------------------------------------------ 진행 상황

    public int CountInBag(ItemData item) // 가방 수량
    {
        return inventory != null && item != null ? inventory.GetItemQuantity(item) : 0;
    }

    public bool IsReady(NpcQuestBook.Quest quest) // 가방에 필요한 물건이 다 있는지
    {
        if (quest == null)
        {
            return false;
        }

        foreach (NpcQuestBook.Requirement requirement in quest.Requirements)
        {
            if (requirement == null || requirement.Item == null || CountInBag(requirement.Item) < requirement.Amount)
            {
                return false;
            }
        }

        return true;
    }

    public string ProgressText(NpcQuestBook.Quest quest) // "붕어 2/3 · 우유 1/2"
    {
        List<string> parts = new List<string>();

        foreach (NpcQuestBook.Requirement requirement in quest.Requirements)
        {
            if (requirement?.Item != null)
            {
                parts.Add($"{requirement.Item.DisplayName} {Mathf.Min(CountInBag(requirement.Item), requirement.Amount)}/{requirement.Amount}");
            }
        }

        return string.Join(" · ", parts);
    }

    public string MissingText(NpcQuestBook.Quest quest) // "CRUCIAN CARP 1개가 더 필요해요"
    {
        foreach (NpcQuestBook.Requirement requirement in quest.Requirements)
        {
            int have = requirement?.Item != null ? CountInBag(requirement.Item) : 0;

            if (requirement?.Item != null && have < requirement.Amount)
            {
                return $"{requirement.Item.DisplayName} {requirement.Amount - have}개가 더 필요해요.";
            }
        }

        return string.Empty;
    }

    public static string RewardText(NpcQuestBook.Quest quest) // "코인 60 · 호감도 +4 · WORM BAIT x10"
    {
        List<string> parts = new List<string>();

        if (quest.RewardCoins > 0) parts.Add($"코인 {quest.RewardCoins}");
        if (quest.RewardAffinity > 0) parts.Add($"호감도 +{quest.RewardAffinity}");
        if (quest.RewardItem != null) parts.Add($"{quest.RewardItem.DisplayName} x{quest.RewardItemAmount}");
        return string.Join(" · ", parts);
    }

    // ------------------------------------------------------------ 받기 · 포기 · 전달

    public bool CanAccept(string questId, out string reason) // 받을 수 있는지
    {
        if (!board.Contains(questId) || !TryGetQuest(questId, out NpcQuestBook book, out _))
        {
            reason = "게시판에 없는 의뢰예요.";
            return false;
        }

        if (active.Count >= activeLimit)
        {
            reason = $"의뢰는 {activeLimit}개까지 받을 수 있어요.";
            return false;
        }

        if (active.Exists(entry => entry.OwnerId == book.OwnerId))
        {
            reason = "이 주민의 의뢰를 이미 진행 중이에요.";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    public bool Accept(string questId, out string message) // 의뢰 받기
    {
        if (!CanAccept(questId, out message))
        {
            return false;
        }

        TryGetQuest(questId, out NpcQuestBook book, out NpcQuestBook.Quest quest);
        int today = CurrentDay;
        active.Add(new NpcActiveQuest(book, quest, today, today + quest.Days - 1));
        board.Remove(questId);
        message = $"의뢰를 받았어요 : {quest.Title} ({DaysLeftText(quest.Days)})";
        Changed();
        return true;
    }

    public bool Abandon(string questId, out string message) // 의뢰 포기 (게시판으로 돌아가지 않음)
    {
        NpcActiveQuest entry = GetActive(questId);

        if (entry == null)
        {
            message = "진행 중인 의뢰가 아니에요.";
            return false;
        }

        active.Remove(entry);
        message = $"의뢰를 포기했어요 : {entry.Quest.Title}";
        Changed();
        return true;
    }

    public bool TryComplete(string questId, PlayerInventory bag, out NpcQuestResult result, out string message) // 의뢰 전달 (필요 물건 차감 → 보상)
    {
        result = default;
        NpcActiveQuest entry = GetActive(questId);
        PlayerWallet wallet = Wallet;

        if (entry == null || bag == null)
        {
            message = "진행 중인 의뢰가 아니에요.";
            return false;
        }

        if (DaysLeft(entry) <= 0)
        {
            message = "기한이 지난 의뢰예요.";
            return false;
        }

        NpcQuestBook.Quest quest = entry.Quest;

        foreach (NpcQuestBook.Requirement requirement in quest.Requirements)
        {
            int have = requirement?.Item != null ? bag.GetItemQuantity(requirement.Item) : 0;

            if (requirement?.Item == null || have < requirement.Amount)
            {
                message = requirement?.Item != null ? $"{requirement.Item.DisplayName} {requirement.Amount - have}개가 더 필요해요." : "의뢰 데이터가 잘못되었어요.";
                return false;
            }
        }

        if (quest.RewardItem != null && !bag.CanAddItem(quest.RewardItem, quest.RewardItemAmount))
        {
            message = "가방에 보상을 받을 자리가 없어요.";
            return false;
        }

        foreach (NpcQuestBook.Requirement requirement in quest.Requirements)
        {
            bag.RemoveItem(requirement.Item, requirement.Amount);
        }

        NpcCharacterData owner = GetOwner(entry.Book);
        AffinityStage before = relations != null ? relations.GetStage(owner) : AffinityStage.Uninterested;
        int affinityBefore = relations != null ? relations.GetAffinity(owner) : 0;

        if (quest.RewardCoins > 0 && wallet != null)
        {
            wallet.Add(quest.RewardCoins);
        }

        if (relations != null && owner != null && quest.RewardAffinity > 0)
        {
            relations.MarkMet(owner);
            relations.ChangeAffinity(owner, quest.RewardAffinity);
        }

        int given = quest.RewardItem != null ? quest.RewardItemAmount - bag.AddItem(quest.RewardItem, quest.RewardItemAmount) : 0;
        int affinityGained = relations != null ? relations.GetAffinity(owner) - affinityBefore : 0;
        AffinityStage after = relations != null ? relations.GetStage(owner) : before;

        if (!records.TryGetValue(quest.QuestId, out NpcQuestRecordSaveData record))
        {
            record = new NpcQuestRecordSaveData { questId = quest.QuestId };
            records.Add(quest.QuestId, record);
        }

        record.completedCount++;
        record.lastCompletedDay = CurrentDay;
        totalCompleted++;
        active.Remove(entry);
        result = new NpcQuestResult(quest.RewardCoins, affinityGained, quest.RewardItem, given, before, after);
        message = $"의뢰 완료 : {quest.Title}";
        QuestCompleted?.Invoke(quest, owner);
        Changed();
        return true;
    }

    // ------------------------------------------------------------ 하루 처리

    public void ProcessDay(int day) // 기한 지난 의뢰 정리 → 게시판 새로 붙이기
    {
        lastDay = day;
        ExpireQuests(day);
        GenerateBoard(day);
        Changed();
    }

    private void ExpireQuests(int day) // 마감 날짜가 지난 의뢰 제거
    {
        for (int index = active.Count - 1; index >= 0; index--)
        {
            NpcActiveQuest entry = active[index];

            if (entry.DeadlineDay >= day)
            {
                continue;
            }

            active.RemoveAt(index);
            totalExpired++;
            NpcCharacterData owner = GetOwner(entry.Book);
            QuestExpired?.Invoke(entry.Quest);
            Notice?.Invoke($"기한이 지나 의뢰가 사라졌어요 : {entry.Quest.Title}{(owner != null ? $" ({owner.DisplayName})" : string.Empty)}");
        }
    }

    public void GenerateBoard(int day) // 그날 게시판 (같은 날이면 항상 같음 · 특별 의뢰 먼저 · 한 사람당 하나)
    {
        board.Clear();
        boardDay = day;

        if (npcManager == null)
        {
            return;
        }

        SeasonType season = npcManager.GetSeasonForDay(day);
        List<(NpcQuestBook book, NpcQuestBook.Quest quest)> specials = new List<(NpcQuestBook, NpcQuestBook.Quest)>();
        List<(NpcQuestBook book, NpcQuestBook.Quest quest)> regular = new List<(NpcQuestBook, NpcQuestBook.Quest)>();

        foreach (NpcQuestBook book in books)
        {
            NpcCharacterData owner = GetOwner(book);

            if (book == null || owner == null || npcManager.FindAgent(book.OwnerId) == null || active.Exists(entry => entry.OwnerId == book.OwnerId))
            {
                continue; // 배치되지 않았거나 이미 의뢰를 진행 중인 NPC
            }

            AffinityStage stage = relations != null ? relations.PeekStage(owner) : AffinityStage.Uninterested;

            foreach (NpcQuestBook.Quest quest in book.Quests)
            {
                if (quest == null || !quest.IsOfferedIn(season) || quest.RequiredStage > stage || (quest.IsSpecial && GetCompletedCount(quest.QuestId) > 0) || !RequiredEventSeen(quest))
                {
                    continue;
                }

                (quest.IsSpecial ? specials : regular).Add((book, quest));
            }
        }

        specials.Sort((left, right) => string.CompareOrdinal(left.quest.QuestId, right.quest.QuestId));
        regular.Sort((left, right) => string.CompareOrdinal(left.quest.QuestId, right.quest.QuestId));
        System.Random random = new System.Random(unchecked(day * 7919 + boardSeed));
        Shuffle(specials, random);
        Shuffle(regular, random);
        HashSet<string> owners = new HashSet<string>(StringComparer.Ordinal);
        bool specialPosted = false;

        foreach ((NpcQuestBook book, NpcQuestBook.Quest quest) in specials)
        {
            if (!specialPosted && board.Count < boardSize && owners.Add(book.OwnerId))
            {
                board.Add(quest.QuestId); // 특별 의뢰는 하루에 하나
                specialPosted = true;
            }
        }

        foreach ((NpcQuestBook book, NpcQuestBook.Quest quest) in regular)
        {
            if (board.Count < boardSize && owners.Add(book.OwnerId))
            {
                board.Add(quest.QuestId);
            }
        }
    }

    private static bool RequiredEventSeen(NpcQuestBook.Quest quest) // 94일차: 필요한 하트 이벤트를 봤는지 (이벤트 기능이 없는 Scene은 통과)
    {
        NpcEventManager events = NpcEventManager.Instance;
        return string.IsNullOrEmpty(quest.RequiredEventId) || events == null || events.HasSeen(quest.RequiredEventId);
    }

    private static void Shuffle<T>(List<T> list, System.Random random) // 같은 기준 값이면 같은 순서
    {
        for (int index = list.Count - 1; index > 0; index--)
        {
            int swap = random.Next(index + 1);
            (list[index], list[swap]) = (list[swap], list[index]);
        }
    }

    private void Changed() // 변경 알림 + 머리 위 표시
    {
        RefreshMarkers();
        QuestsChanged?.Invoke();
    }

    public void RefreshMarkers() // NPC 머리 위 ! · ? 표시
    {
        if (npcManager == null)
        {
            return;
        }

        foreach (NpcAgent agent in npcManager.Agents)
        {
            if (agent == null)
            {
                continue;
            }

            NpcActiveQuest entry = active.Find(candidate => candidate.OwnerId == agent.CharacterId);
            agent.SetQuestMarker(entry == null ? NpcQuestMarker.None : IsReady(entry.Quest) ? NpcQuestMarker.Ready : NpcQuestMarker.Active);
        }
    }

    // ------------------------------------------------------------ 저장

    public NpcQuestSaveData CaptureSaveData() // 저장 데이터
    {
        NpcQuestSaveData data = new NpcQuestSaveData
        {
            boardDay = boardDay == int.MinValue ? CurrentDay : boardDay,
            board = new List<string>(board),
            totalCompleted = totalCompleted,
            totalExpired = totalExpired
        };

        foreach (NpcActiveQuest entry in active)
        {
            data.active.Add(new NpcQuestActiveSaveData { questId = entry.Quest.QuestId, acceptedDay = entry.AcceptedDay, deadlineDay = entry.DeadlineDay });
        }

        foreach (NpcQuestRecordSaveData record in records.Values)
        {
            data.records.Add(new NpcQuestRecordSaveData { questId = record.questId, completedCount = record.completedCount, lastCompletedDay = record.lastCompletedDay });
        }

        data.records.Sort((left, right) => string.CompareOrdinal(left.questId, right.questId));
        return data;
    }

    public void ApplySaveData(NpcQuestSaveData data, int loadedDay) // 불러오기 (관계 복원 뒤 · 시간 적용 전)
    {
        ClearState();

        if (data != null)
        {
            totalCompleted = Mathf.Max(0, data.totalCompleted);
            totalExpired = Mathf.Max(0, data.totalExpired);

            foreach (NpcQuestRecordSaveData record in data.records ?? new List<NpcQuestRecordSaveData>())
            {
                if (record != null && lookup.ContainsKey(record.questId) && !records.ContainsKey(record.questId))
                {
                    records.Add(record.questId, new NpcQuestRecordSaveData { questId = record.questId, completedCount = Mathf.Max(0, record.completedCount), lastCompletedDay = record.lastCompletedDay });
                }
            }

            foreach (NpcQuestActiveSaveData saved in data.active ?? new List<NpcQuestActiveSaveData>())
            {
                if (saved != null && TryGetQuest(saved.questId, out NpcQuestBook book, out NpcQuestBook.Quest quest) && GetActive(saved.questId) == null)
                {
                    active.Add(new NpcActiveQuest(book, quest, saved.acceptedDay, saved.deadlineDay)); // 데이터에서 사라진 의뢰는 버림
                }
            }

            foreach (string questId in data.board ?? new List<string>())
            {
                if (lookup.ContainsKey(questId) && GetActive(questId) == null && !board.Contains(questId))
                {
                    board.Add(questId);
                }
            }

            boardDay = data.boardDay;
        }

        lastDay = loadedDay;
        ExpireQuests(loadedDay);

        if (boardDay != loadedDay) // 다른 날 저장 → 그날 게시판
        {
            GenerateBoard(loadedDay);
        }

        Changed();
    }

    public void ResetForLoad(int loadedDay) // 93일차 이전 저장 파일 : 의뢰 없이 그날 게시판으로 시작
    {
        ClearState();
        lastDay = loadedDay;
        GenerateBoard(loadedDay);
        Changed();
    }

    private void ClearState() // 상태 비우기
    {
        board.Clear();
        active.Clear();
        records.Clear();
        boardDay = int.MinValue;
        totalCompleted = 0;
        totalExpired = 0;
    }

    // ------------------------------------------------------------ 테스트

    public string Describe() // 디버그용 상태
    {
        StringBuilder text = new StringBuilder($"[NPC 의뢰] DAY {CurrentDay} · 게시판 {boardDay}일 · 완료 {totalCompleted} · 만료 {totalExpired}");

        foreach (string questId in board)
        {
            TryGetQuest(questId, out NpcQuestBook book, out NpcQuestBook.Quest quest);
            text.Append($"\n게시판 : {questId} ({book.OwnerId}) {quest.Title} · {RewardText(quest)}");
        }

        foreach (NpcActiveQuest entry in active)
        {
            text.Append($"\n진행 : {entry.Quest.QuestId} · {ProgressText(entry.Quest)} · {DaysLeftText(DaysLeft(entry))}{(IsReady(entry.Quest) ? " · 전달 가능" : string.Empty)}");
        }

        return text.ToString();
    }

#if UNITY_EDITOR
    public void EditorAssign(List<NpcQuestBook> bookList, NpcManager manager, NpcRelationshipManager relationshipManager) // 생성 도구 전용
    {
        books = bookList ?? new List<NpcQuestBook>();
        npcManager = manager;
        relations = relationshipManager;
        lookup.Clear();
    }
#endif
}
