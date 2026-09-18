using System; // Serializable
using System.Collections.Generic; // 목록
using UnityEngine; // Unity 기본 기능

[CreateAssetMenu(fileName = "NpcEvents_New", menuName = "Project U/NPC/Event Book")] // NPC 이벤트 묶음 생성 메뉴
public sealed class NpcEventBook : ScriptableObject // 94일차: NPC 한 명의 하트 이벤트 (관계 단계마다 한 번 보는 짧은 이야기)
{
    [Serializable]
    public sealed class Line // 이벤트 대사 한 줄
    {
        [SerializeField] private bool fromPlayer; // 플레이어가 하는 말
        [SerializeField, TextArea(1, 3)] private string text; // 문장

        public Line(bool fromPlayer, string text) // 생성 도구에서 사용
        {
            this.fromPlayer = fromPlayer;
            this.text = text;
        }

        public bool FromPlayer => fromPlayer; // 플레이어 대사 여부
        public string Text => text; // 문장 제공
    }

    [Serializable]
    public sealed class Choice // 선택지 하나
    {
        [SerializeField] private string label; // 버튼 문구 (플레이어 대답)
        [SerializeField, TextArea(1, 3)] private string reply; // NPC 대답
        [SerializeField] private int affinity; // 호감도 변화 (음수 가능)

        public Choice(string label, string reply, int affinity) // 생성 도구에서 사용
        {
            this.label = label;
            this.reply = reply;
            this.affinity = affinity;
        }

        public string Label => label; // 버튼 문구 제공
        public string Reply => reply; // NPC 대답 제공
        public int Affinity => affinity; // 호감도 변화 제공
    }

    [Serializable]
    public sealed class Event // 이벤트 하나
    {
        [Tooltip("이벤트 ID (event_mio_trust).")]
        [SerializeField] private string eventId; // ID
        [Tooltip("장면 제목.")]
        [SerializeField] private string title; // 제목
        [Tooltip("이 관계 단계가 되면 볼 수 있습니다.")]
        [SerializeField] private AffinityStage requiredStage = AffinityStage.Curious; // 필요 단계
        [Tooltip("NPC가 이 일정 위치에 도착해 있을 때만 시작합니다.")]
        [SerializeField] private string locationId; // 장소
        [Tooltip("시작할 수 있는 시각 (이상).")]
        [SerializeField, Range(0f, 24f)] private float fromHour; // 시작 시각
        [Tooltip("시작할 수 있는 시각 (미만).")]
        [SerializeField, Range(0f, 24f)] private float toHour = 24f; // 끝 시각
        [Tooltip("날씨 조건.")]
        [SerializeField] private NpcWeatherCondition weather = NpcWeatherCondition.Any; // 날씨
        [SerializeField] private List<Line> lines = new List<Line>(); // 선택지 전 대사
        [SerializeField] private List<Choice> choices = new List<Choice>(); // 선택지
        [SerializeField] private List<Line> afterLines = new List<Line>(); // 대답 뒤 대사
        [Tooltip("끝나면 받는 아이템 (없으면 비움).")]
        [SerializeField] private ItemData rewardItem; // 보상 아이템
        [SerializeField, Min(0)] private int rewardItemAmount; // 보상 수량

        public Event(string eventId, string title, AffinityStage requiredStage, string locationId, float fromHour, float toHour, NpcWeatherCondition weather, List<Line> lines, List<Choice> choices, List<Line> afterLines, ItemData rewardItem, int rewardItemAmount) // 생성 도구에서 사용
        {
            this.eventId = eventId;
            this.title = title;
            this.requiredStage = requiredStage;
            this.locationId = locationId;
            this.fromHour = fromHour;
            this.toHour = toHour;
            this.weather = weather;
            this.lines = lines ?? new List<Line>();
            this.choices = choices ?? new List<Choice>();
            this.afterLines = afterLines ?? new List<Line>();
            this.rewardItem = rewardItem;
            this.rewardItemAmount = rewardItem != null ? Mathf.Max(1, rewardItemAmount) : 0;
        }

        public string EventId => eventId; // ID 제공
        public string Title => title; // 제목 제공
        public AffinityStage RequiredStage => requiredStage; // 필요 단계 제공
        public string LocationId => locationId; // 장소 제공
        public float FromHour => fromHour; // 시작 시각 제공
        public float ToHour => toHour; // 끝 시각 제공
        public NpcWeatherCondition Weather => weather; // 날씨 조건 제공
        public IReadOnlyList<Line> Lines => lines; // 선택지 전 대사 제공
        public IReadOnlyList<Choice> Choices => choices; // 선택지 제공
        public IReadOnlyList<Line> AfterLines => afterLines; // 대답 뒤 대사 제공
        public ItemData RewardItem => rewardItem; // 보상 아이템 제공
        public int RewardItemAmount => rewardItem != null ? Mathf.Max(1, rewardItemAmount) : 0; // 보상 수량 제공

        public bool IsInTimeWindow(float hour) // 시각 조건
        {
            return hour >= fromHour && hour < toHour;
        }
    }

    [Tooltip("이벤트 주인공 NPC ID.")]
    [SerializeField] private string ownerId = "char_new"; // 주인
    [SerializeField] private List<Event> events = new List<Event>(); // 이벤트 (관계 단계 순서)

    public string OwnerId => ownerId; // 주인 제공
    public IReadOnlyList<Event> Events => events; // 이벤트 제공

#if UNITY_EDITOR
    public void EditorAssign(string owner, List<Event> list) // 생성 도구 전용
    {
        ownerId = owner;
        events = list ?? new List<Event>();
    }
#endif
}
