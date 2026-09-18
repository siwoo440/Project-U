using System; // Serializable
using System.Collections.Generic; // 목록
using UnityEngine; // Unity 기본 기능

[CreateAssetMenu(fileName = "NpcGift_New", menuName = "Project U/NPC/Gift Profile")] // 선물 반응 생성 메뉴
public sealed class NpcGiftProfile : ScriptableObject // 89일차: NPC 한 명의 선물 반응 5단계
{
    [Serializable]
    public sealed class Entry // 아이템 하나의 반응
    {
        [Tooltip("선물 아이템.")]
        [SerializeField] private ItemData item; // 아이템
        [Tooltip("반응 단계.")]
        [SerializeField] private GiftPreference preference = GiftPreference.Liked; // 반응
        [Tooltip("이 항목을 만든 규칙 (item_id 또는 tag:분류).")]
        [SerializeField] private string source; // 출처 규칙

        public Entry(ItemData item, GiftPreference preference, string source) // 생성 도구에서 사용
        {
            this.item = item;
            this.preference = preference;
            this.source = source;
        }

        public ItemData Item => item; // 아이템 제공
        public GiftPreference Preference => preference; // 반응 제공
        public string Source => source; // 출처 제공
    }

    [Tooltip("선물 반응 ID (gift_lunette).")]
    [SerializeField] private string profileId = "gift_new"; // 선물 반응 ID
    [Tooltip("캐릭터 시트의 선호 아이템 원문.")]
    [SerializeField, TextArea(2, 4)] private string sheetNote; // 시트 원문
    [Tooltip("목록에 없는 아이템의 반응.")]
    [SerializeField] private GiftPreference defaultPreference = GiftPreference.Neutral; // 기본 반응
    [Tooltip("아이템별 반응 (같은 아이템이 여러 번 있으면 먼저 나온 항목 사용).")]
    [SerializeField] private List<Entry> entries = new List<Entry>(); // 반응 목록

    public string ProfileId => profileId; // ID 제공
    public string SheetNote => sheetNote; // 시트 원문 제공
    public GiftPreference DefaultPreference => defaultPreference; // 기본 반응 제공
    public IReadOnlyList<Entry> Entries => entries; // 반응 목록 제공

    public GiftPreference GetPreference(ItemData item) // 아이템 반응 검색
    {
        if (item != null)
        {
            foreach (Entry entry in entries)
            {
                if (entry != null && entry.Item == item)
                {
                    return entry.Preference; // 등록된 반응
                }
            }
        }

        return defaultPreference; // 목록에 없으면 기본 반응
    }

    public int Count(GiftPreference preference) // 단계별 아이템 수
    {
        int count = 0;

        foreach (Entry entry in entries)
        {
            if (entry != null && entry.Item != null && entry.Preference == preference)
            {
                count++;
            }
        }

        return count;
    }

#if UNITY_EDITOR
    public void EditorAssign(string id, string note, List<Entry> newEntries) // 생성 도구 전용
    {
        profileId = id;
        sheetNote = note;
        defaultPreference = GiftPreference.Neutral;
        entries = newEntries ?? new List<Entry>();
    }
#endif
}
