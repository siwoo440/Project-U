using System; // Serializable
using System.Collections.Generic; // 목록
using UnityEngine; // Unity 기본 기능

public enum NpcQuestKind // 의뢰 종류
{
    Board = 0, // 게시판 의뢰 (여러 번 나옴)
    Special = 1 // 특별 의뢰 (관계 단계가 오르면 한 번)
}

[CreateAssetMenu(fileName = "NpcQuests_New", menuName = "Project U/NPC/Quest Book")] // NPC 의뢰 묶음 생성 메뉴
public sealed class NpcQuestBook : ScriptableObject // 93일차: NPC 한 명의 의뢰 묶음 (캐릭터 데이터 questGroupId와 연결)
{
    [Serializable]
    public sealed class Requirement // 가져올 물건 한 종류
    {
        [SerializeField] private ItemData item; // 아이템
        [SerializeField, Min(1)] private int amount = 1; // 수량

        public Requirement(ItemData item, int amount) // 생성 도구에서 사용
        {
            this.item = item;
            this.amount = amount;
        }

        public ItemData Item => item; // 아이템 제공
        public int Amount => Mathf.Max(1, amount); // 수량 제공
    }

    [Serializable]
    public sealed class Quest // 의뢰 하나
    {
        [Tooltip("의뢰 ID (quest_mio_crucian).")]
        [SerializeField] private string questId; // ID
        [Tooltip("게시판 제목.")]
        [SerializeField] private string title; // 제목
        [Tooltip("게시판 의뢰는 여러 번, 특별 의뢰는 한 번만 나옵니다.")]
        [SerializeField] private NpcQuestKind kind = NpcQuestKind.Board; // 종류
        [Tooltip("가져올 물건.")]
        [SerializeField] private List<Requirement> requirements = new List<Requirement>(); // 필요 물건
        [Tooltip("보상 코인.")]
        [SerializeField, Min(0)] private int rewardCoins; // 보상 코인
        [Tooltip("보상 호감도.")]
        [SerializeField, Min(0)] private int rewardAffinity; // 보상 호감도
        [Tooltip("보상 아이템 (없으면 비움).")]
        [SerializeField] private ItemData rewardItem; // 보상 아이템
        [SerializeField, Min(0)] private int rewardItemAmount; // 보상 아이템 수량
        [Tooltip("받은 날부터 며칠 안에 전달해야 하는지.")]
        [SerializeField, Min(1)] private int days = 3; // 기한
        [Tooltip("게시판에 나오는 계절. 비어 있으면 모든 계절.")]
        [SerializeField] private SeasonType[] seasons = new SeasonType[0]; // 계절
        [Tooltip("이 관계 단계부터 게시판에 나옵니다.")]
        [SerializeField] private AffinityStage requiredStage = AffinityStage.Uninterested; // 필요 단계
        [Tooltip("의뢰할 때 대사 (게시판 쪽지).")]
        [SerializeField, TextArea(1, 3)] private string requestLine; // 의뢰 대사
        [Tooltip("전달했을 때 대사.")]
        [SerializeField, TextArea(1, 3)] private string thanksLine; // 감사 대사

        public Quest(string questId, string title, NpcQuestKind kind, List<Requirement> requirements, int rewardCoins, int rewardAffinity, ItemData rewardItem, int rewardItemAmount, int days, SeasonType[] seasons, AffinityStage requiredStage, string requestLine, string thanksLine) // 생성 도구에서 사용
        {
            this.questId = questId;
            this.title = title;
            this.kind = kind;
            this.requirements = requirements ?? new List<Requirement>();
            this.rewardCoins = rewardCoins;
            this.rewardAffinity = rewardAffinity;
            this.rewardItem = rewardItem;
            this.rewardItemAmount = rewardItem != null ? Mathf.Max(1, rewardItemAmount) : 0;
            this.days = days;
            this.seasons = seasons ?? new SeasonType[0];
            this.requiredStage = requiredStage;
            this.requestLine = requestLine;
            this.thanksLine = thanksLine;
        }

        public string QuestId => questId; // ID 제공
        public string Title => title; // 제목 제공
        public NpcQuestKind Kind => kind; // 종류 제공
        public bool IsSpecial => kind == NpcQuestKind.Special; // 특별 의뢰 여부
        public IReadOnlyList<Requirement> Requirements => requirements; // 필요 물건 제공
        public int RewardCoins => Mathf.Max(0, rewardCoins); // 보상 코인 제공
        public int RewardAffinity => Mathf.Max(0, rewardAffinity); // 보상 호감도 제공
        public ItemData RewardItem => rewardItem; // 보상 아이템 제공
        public int RewardItemAmount => rewardItem != null ? Mathf.Max(1, rewardItemAmount) : 0; // 보상 아이템 수량 제공
        public int Days => Mathf.Max(1, days); // 기한 제공
        public IReadOnlyList<SeasonType> Seasons => seasons; // 계절 제공
        public AffinityStage RequiredStage => requiredStage; // 필요 단계 제공
        public string RequestLine => requestLine; // 의뢰 대사 제공
        public string ThanksLine => thanksLine; // 감사 대사 제공

        public bool IsOfferedIn(SeasonType season) // 지정 계절 게시 여부
        {
            return seasons == null || seasons.Length == 0 || Array.IndexOf(seasons, season) >= 0;
        }
    }

    [Tooltip("의뢰 묶음 ID (캐릭터 데이터 questGroupId와 같음).")]
    [SerializeField] private string groupId = "quest_new"; // 묶음 ID
    [Tooltip("의뢰를 내는 NPC ID.")]
    [SerializeField] private string ownerId = "char_new"; // 주인
    [SerializeField] private List<Quest> quests = new List<Quest>(); // 의뢰 목록

    public string GroupId => groupId; // 묶음 ID 제공
    public string OwnerId => ownerId; // 주인 제공
    public IReadOnlyList<Quest> Quests => quests; // 의뢰 목록 제공

#if UNITY_EDITOR
    public void EditorAssign(string group, string owner, List<Quest> list) // 생성 도구 전용
    {
        groupId = group;
        ownerId = owner;
        quests = list ?? new List<Quest>();
    }
#endif
}
