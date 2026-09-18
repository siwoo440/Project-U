using System; // Serializable
using System.Collections.Generic; // 목록
using UnityEngine; // Unity 기본 기능

[CreateAssetMenu(fileName = "NpcDialogue_New", menuName = "Project U/NPC/Dialogue Set")] // 대사 묶음 생성 메뉴
public sealed class NpcDialogueSet : ScriptableObject // 89일차: NPC 한 명의 대사 (인사·잡담·호감도·계절·날씨·선물)
{
    [Serializable]
    public sealed class Line // 대사 한 줄
    {
        [Tooltip("대사 종류.")]
        [SerializeField] private NpcDialogueKind kind; // 종류
        [Tooltip("호감도 단계 (Stage 대사). 다른 종류는 이 단계 이상일 때만 사용합니다.")]
        [SerializeField] private AffinityStage stage; // 호감도 단계
        [Tooltip("계절 (Season 대사).")]
        [SerializeField] private SeasonType season; // 계절
        [Tooltip("날씨 (Weather 대사).")]
        [SerializeField] private WeatherType weather; // 날씨
        [Tooltip("선물 반응 (Gift 대사).")]
        [SerializeField] private GiftPreference giftPreference; // 선물 반응
        [Tooltip("대사 내용.")]
        [SerializeField, TextArea(1, 4)] private string text; // 대사

        public Line(NpcDialogueKind kind, AffinityStage stage, SeasonType season, WeatherType weather, GiftPreference giftPreference, string text) // 생성 도구에서 사용
        {
            this.kind = kind;
            this.stage = stage;
            this.season = season;
            this.weather = weather;
            this.giftPreference = giftPreference;
            this.text = text;
        }

        public NpcDialogueKind Kind => kind; // 종류 제공
        public AffinityStage Stage => stage; // 단계 제공
        public SeasonType Season => season; // 계절 제공
        public WeatherType Weather => weather; // 날씨 제공
        public GiftPreference GiftPreference => giftPreference; // 선물 반응 제공
        public string Text => text; // 대사 제공
    }

    [Tooltip("대사 묶음 ID (dlg_lunette).")]
    [SerializeField] private string dialogueId = "dlg_new"; // 대사 ID
    [Tooltip("하루에 호감도가 오르는 대화 횟수.")]
    [SerializeField, Min(1)] private int dailyTalkLimit = 1; // 하루 대화 제한
    [Tooltip("표정 태그 (시트 ExpressionTags).")]
    [SerializeField] private string[] expressionTags = Array.Empty<string>(); // 표정
    [SerializeField] private List<Line> lines = new List<Line>(); // 대사 목록

    public string DialogueId => dialogueId; // ID 제공
    public int DailyTalkLimit => Mathf.Max(1, dailyTalkLimit); // 하루 대화 제한 제공
    public IReadOnlyList<string> ExpressionTags => expressionTags; // 표정 제공
    public IReadOnlyList<Line> Lines => lines; // 대사 목록 제공

    public List<Line> GetLines(NpcDialogueKind kind, AffinityStage currentStage) // 종류별 대사 (현재 단계 이하 조건만)
    {
        List<Line> result = new List<Line>();

        foreach (Line line in lines)
        {
            if (line == null || line.Kind != kind || string.IsNullOrEmpty(line.Text))
            {
                continue;
            }

            if (kind == NpcDialogueKind.Stage ? line.Stage == currentStage : line.Stage <= currentStage)
            {
                result.Add(line); // 조건에 맞는 대사
            }
        }

        return result;
    }

    public bool TryGetSeasonLine(SeasonType season, out Line result) => TryFind(line => line.Kind == NpcDialogueKind.Season && line.Season == season, out result); // 계절 대사
    public bool TryGetWeatherLine(WeatherType weather, out Line result) => TryFind(line => line.Kind == NpcDialogueKind.Weather && line.Weather == weather, out result); // 날씨 대사
    public bool TryGetGiftLine(GiftPreference preference, out Line result) => TryFind(line => line.Kind == NpcDialogueKind.Gift && line.GiftPreference == preference, out result); // 선물 대사
    public bool TryGetStageLine(AffinityStage stage, out Line result) => TryFind(line => line.Kind == NpcDialogueKind.Stage && line.Stage == stage, out result); // 호감도 대사

    public int Count(NpcDialogueKind kind) // 종류별 대사 수
    {
        int count = 0;

        foreach (Line line in lines)
        {
            if (line != null && line.Kind == kind && !string.IsNullOrEmpty(line.Text))
            {
                count++;
            }
        }

        return count;
    }

    private bool TryFind(Predicate<Line> match, out Line result) // 조건에 맞는 첫 대사
    {
        foreach (Line line in lines)
        {
            if (line != null && !string.IsNullOrEmpty(line.Text) && match(line))
            {
                result = line;
                return true;
            }
        }

        result = null;
        return false;
    }

#if UNITY_EDITOR
    public void EditorAssign(string id, int talkLimit, string[] expressions, List<Line> newLines) // 생성 도구 전용
    {
        dialogueId = id;
        dailyTalkLimit = Mathf.Max(1, talkLimit);
        expressionTags = expressions ?? Array.Empty<string>();
        lines = newLines ?? new List<Line>();
    }
#endif
}
