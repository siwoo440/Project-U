using System; // 직렬화 기능
using System.Collections.Generic; // 목록 기능
using UnityEngine; // Unity 기본 기능

public enum NpcCompanionRole // 109일차: 동료가 잘하는 일
{
    Fighter = 0, // 전투 : 가까운 적을 공격
    Gatherer = 1, // 채집 : 다니면서 물건을 찾아 줌
    Healer = 2 // 치유 : 체력이 낮으면 회복
}

[CreateAssetMenu(fileName = "NpcCompanionBook", menuName = "Project U/NPC/Companion Book")] // 생성 메뉴
public sealed class NpcCompanionBook : ScriptableObject // 109일차: 동료가 될 수 있는 NPC와 역할 · 대사 (NpcCompanions.csv → 25번 메뉴)
{
    [Serializable]
    public sealed class Entry // NPC 한 명의 동료 정보
    {
        [Tooltip("캐릭터 ID.")]
        public string characterId = string.Empty; // 캐릭터 ID
        [Tooltip("역할.")]
        public NpcCompanionRole role; // 역할
        [Tooltip("함께 가려면 필요한 관계 단계.")]
        public AffinityStage requiredStage = AffinityStage.Trust; // 필요한 단계
        [Tooltip("전투 피해량 · 치유량 (채집은 한 번에 찾는 수).")]
        public float power = 10f; // 힘
        [Tooltip("공격 · 채집 · 치유 간격 (초).")]
        public float interval = 2f; // 간격
        [Tooltip("채집형이 찾아 주는 물건.")]
        public List<ItemData> lootItems = new List<ItemData>(); // 채집 물건
        [Tooltip("함께 가기로 했을 때.")]
        public string joinLine = string.Empty; // 합류
        [Tooltip("돌아갈 때.")]
        public string leaveLine = string.Empty; // 헤어짐
        [Tooltip("밤이 되어 돌아갈 때.")]
        public string nightLine = string.Empty; // 밤
        [Tooltip("아직 친하지 않을 때.")]
        public string refuseLine = string.Empty; // 거절
        [Tooltip("역할 행동을 할 때 ({item} = 찾은 물건).")]
        public string roleLine = string.Empty; // 역할 대사
        [Tooltip("함께 걸을 때 가끔 하는 말.")]
        public List<string> idleLines = new List<string>(); // 혼잣말
    }

    [SerializeField] private List<Entry> entries = new List<Entry>(); // 동료 목록

    public IReadOnlyList<Entry> Entries => entries; // 목록 제공

    public Entry Get(string characterId) // 캐릭터 ID로 찾기 (없으면 null)
    {
        foreach (Entry entry in entries)
        {
            if (entry != null && entry.characterId == characterId)
            {
                return entry;
            }
        }

        return null;
    }

    public static string RoleName(NpcCompanionRole role) // 역할 이름
    {
        switch (role)
        {
            case NpcCompanionRole.Fighter: return "전투";
            case NpcCompanionRole.Gatherer: return "채집";
            default: return "치유";
        }
    }

#if UNITY_EDITOR
    public void EditorAssign(List<Entry> newEntries) // 생성 도구 전용
    {
        entries = newEntries;
    }
#endif
}
