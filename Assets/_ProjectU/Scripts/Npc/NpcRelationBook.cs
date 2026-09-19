using System; // 직렬화
using System.Collections.Generic; // 목록
using UnityEngine; // Unity 기본 기능

public enum NpcRelationKind // 114일차: 이웃끼리의 관계
{
    Friend = 0, // 친구
    Rival = 1, // 라이벌
    Partner = 2, // 짝꿍 (같이 일함)
    Mentor = 3 // 스승과 제자
}

[CreateAssetMenu(menuName = "Project U/NPC/Relation Book", fileName = "NpcRelationBook")]
public sealed class NpcRelationBook : ScriptableObject // 114일차: 이웃끼리의 관계 · 만나면 나누는 대화 · 서로를 말하는 대사 (NpcRelations.csv)
{
    [Serializable]
    public sealed class Line
    {
        [Tooltip("true면 A가, false면 B가 말합니다.")]
        public bool byA; // 말하는 쪽
        public string text; // 대사
    }

    [Serializable]
    public sealed class Banter
    {
        public List<Line> lines = new List<Line>(); // 번갈아 나누는 말 (2~4줄)
    }

    [Serializable]
    public sealed class Pair
    {
        public string pairId; // 쌍 ID
        public string characterA; // NPC A
        public string characterB; // NPC B
        public NpcRelationKind kind; // 관계
        public List<Banter> banters = new List<Banter>(); // 만나면 나누는 대화 (날마다 돌아가며)
        public List<string> mentionsByA = new List<string>(); // A가 B를 말하는 대사
        public List<string> mentionsByB = new List<string>(); // B가 A를 말하는 대사

        public bool Has(string characterId) => characterA == characterId || characterB == characterId;
        public string Other(string characterId) => characterA == characterId ? characterB : characterA;
    }

    [SerializeField] private List<Pair> pairs = new List<Pair>(); // 관계 목록

    public IReadOnlyList<Pair> Pairs => pairs; // 목록 제공

    public Pair Get(string pairId) => pairs.Find(pair => pair != null && pair.pairId == pairId);

    public IEnumerable<string> MentionsBy(string characterId) // 이 NPC가 다른 이웃을 말하는 대사
    {
        foreach (Pair pair in pairs)
        {
            if (pair == null)
            {
                continue;
            }

            if (pair.characterA == characterId)
            {
                foreach (string line in pair.mentionsByA)
                {
                    yield return line;
                }
            }
            else if (pair.characterB == characterId)
            {
                foreach (string line in pair.mentionsByB)
                {
                    yield return line;
                }
            }
        }
    }

    public static string KindName(NpcRelationKind kind) // 관계 이름
    {
        switch (kind)
        {
            case NpcRelationKind.Rival: return "라이벌";
            case NpcRelationKind.Partner: return "짝꿍";
            case NpcRelationKind.Mentor: return "스승과 제자";
            default: return "친구";
        }
    }

#if UNITY_EDITOR
    public void EditorAssign(List<Pair> newPairs) // 생성 도구 전용
    {
        pairs = newPairs ?? new List<Pair>();
    }
#endif
}
