using System; // Serializable
using System.Collections.Generic; // 목록
using UnityEngine; // Unity 기본 기능

[CreateAssetMenu(fileName = "NpcDatabase", menuName = "Project U/NPC/Database")] // NPC 데이터베이스 생성 메뉴
public sealed class NpcDatabase : ScriptableObject // 89일차: 전체 NPC 목록 · 일정 위치 · 호감도 규칙
{
    [Serializable]
    public sealed class Location // NPC 일정 위치 (90일차에 Scene 표시 지점으로 배치)
    {
        [Tooltip("위치 ID (loc_village_square).")]
        [SerializeField] private string locationId; // 위치 ID
        [Tooltip("표시 이름.")]
        [SerializeField] private string displayName; // 표시 이름
        [Tooltip("어디에 두는지 설명.")]
        [SerializeField] private string description; // 설명
        [Tooltip("100일차: 위치가 속한 구역 (village = 마을, 그 밖은 18번 메뉴가 만드는 새 구역).")]
        [SerializeField] private string zoneId = VillageZoneId; // 구역
        [Tooltip("100일차: 물가 위치 (인어 · 크라켄 · 상어족은 물가에만 섭니다).")]
        [SerializeField] private bool waterside; // 물가 여부

        public const string VillageZoneId = "village"; // 마을 구역 ID

        public Location(string locationId, string displayName, string description, string zoneId = VillageZoneId, bool waterside = false) // 생성 도구에서 사용
        {
            this.locationId = locationId;
            this.displayName = displayName;
            this.description = description;
            this.zoneId = string.IsNullOrEmpty(zoneId) ? VillageZoneId : zoneId;
            this.waterside = waterside;
        }

        public string LocationId => locationId; // ID 제공
        public string DisplayName => displayName; // 이름 제공
        public string Description => description; // 설명 제공
        public string ZoneId => string.IsNullOrEmpty(zoneId) ? VillageZoneId : zoneId; // 구역 제공
        public bool IsVillage => ZoneId == VillageZoneId; // 마을 위치인지
        public bool IsWaterside => waterside; // 물가 여부 제공
    }

    [Header("Characters")] // 캐릭터
    [Tooltip("캐릭터 시트의 전체 NPC.")]
    [SerializeField] private List<NpcCharacterData> characters = new List<NpcCharacterData>(); // 전체 NPC

    [Header("Locations")] // 위치
    [SerializeField] private List<Location> locations = new List<Location>(); // 일정 위치

    [Header("Affinity Rules")] // 호감도 규칙
    [Tooltip("단계 시작 점수 (호기심 · 신뢰 · 애정 · 사랑).")]
    [SerializeField] private int[] stageThresholds = { 20, 40, 60, 80 }; // 단계 기준
    [Tooltip("선물 점수 (매우 좋아함 · 좋아함 · 보통 · 싫어함 · 매우 싫어함).")]
    [SerializeField] private int[] giftPoints = { 8, 4, 1, -5, -10 }; // 선물 점수
    [Tooltip("생일 선물 점수 배율.")]
    [SerializeField, Min(1)] private int birthdayMultiplier = 3; // 생일 배율
    [Tooltip("하루 첫 대화 점수.")]
    [SerializeField, Min(0)] private int dailyTalkPoints = 1; // 대화 점수
    [Tooltip("NPC 한 명에게 하루에 줄 수 있는 선물 수.")]
    [SerializeField, Min(1)] private int giftsPerDay = 1; // 하루 선물 수
    [Tooltip("95일차: NPC 한 명에게 한 주(월~일)에 줄 수 있는 선물 수. 생일 선물은 세지 않습니다.")]
    [SerializeField, Min(1)] private int giftsPerWeek = 2; // 한 주 선물 수

    private Dictionary<string, NpcCharacterData> lookup; // ID 검색

    public IReadOnlyList<NpcCharacterData> Characters => characters; // 전체 NPC 제공
    public IReadOnlyList<Location> Locations => locations; // 위치 제공
    public int BirthdayMultiplier => Mathf.Max(1, birthdayMultiplier); // 생일 배율 제공
    public int DailyTalkPoints => Mathf.Max(0, dailyTalkPoints); // 대화 점수 제공
    public int GiftsPerDay => Mathf.Max(1, giftsPerDay); // 하루 선물 수 제공
    public int GiftsPerWeek => Mathf.Max(1, giftsPerWeek); // 한 주 선물 수 제공
    public int StageThreshold(AffinityStage stage) => stage <= AffinityStage.Uninterested ? 0 : stageThresholds[Mathf.Clamp((int)stage - 1, 0, stageThresholds.Length - 1)]; // 95일차: 단계 시작 점수 (밸런스 계산)

    public bool TryGet(string characterId, out NpcCharacterData character) // ID로 NPC 검색
    {
        if (lookup == null || lookup.Count != characters.Count)
        {
            lookup = new Dictionary<string, NpcCharacterData>(StringComparer.Ordinal);

            foreach (NpcCharacterData data in characters)
            {
                if (data != null && !string.IsNullOrEmpty(data.CharacterId) && !lookup.ContainsKey(data.CharacterId))
                {
                    lookup.Add(data.CharacterId, data);
                }
            }
        }

        character = null;
        return !string.IsNullOrEmpty(characterId) && lookup.TryGetValue(characterId, out character);
    }

    public List<NpcCharacterData> GetAlphaCast() // 알파 마을에 배치하는 NPC
    {
        return characters.FindAll(data => data != null && data.IsAlphaCast && data.IsAvailable);
    }

    public bool HasLocation(string locationId) // 위치 ID 확인
    {
        return locations.Exists(location => location != null && location.LocationId == locationId);
    }

    public Location GetLocation(string locationId) // 100일차: 위치 ID → 위치 (없으면 null)
    {
        return locations.Find(location => location != null && location.LocationId == locationId);
    }

    public AffinityStage GetStage(int affinity) // 호감도 → 단계
    {
        AffinityStage stage = AffinityStage.Uninterested;

        for (int index = 0; index < stageThresholds.Length && index < 4; index++)
        {
            if (affinity >= stageThresholds[index])
            {
                stage = (AffinityStage)(index + 1);
            }
        }

        return stage;
    }

    public int GetGiftPoints(GiftPreference preference, bool isBirthday) // 선물 점수
    {
        int index = (int)preference;
        int points = index >= 0 && index < giftPoints.Length ? giftPoints[index] : 0;
        return isBirthday && points > 0 ? points * BirthdayMultiplier : points; // 생일에는 좋은 반응만 배로
    }

#if UNITY_EDITOR
    public void EditorAssign(List<NpcCharacterData> newCharacters, List<Location> newLocations) // 생성 도구 전용
    {
        characters = newCharacters ?? new List<NpcCharacterData>();
        locations = newLocations ?? new List<Location>();
        lookup = null;
    }
#endif
}
