using System; // Serializable
using UnityEngine; // Unity 기본 기능

[CreateAssetMenu(fileName = "NpcData_New", menuName = "Project U/NPC/Character Data")] // NPC 데이터 생성 메뉴
public sealed class NpcCharacterData : ScriptableObject // 89일차: 캐릭터 시트 한 명의 고정 설정 (CharacterDefinition + GameplayDefinition)
{
    [Serializable]
    public sealed class ProfileText // 기본 정보 · 출신
    {
        [Tooltip("종족 ID (race_rabbitkin).")] public string raceId;
        [Tooltip("종족 표시 이름.")] public string raceName;
        [Tooltip("직업 ID (job_herbalist).")] public string jobId;
        [Tooltip("직업 표시 이름.")] public string jobName;
        [Tooltip("성별.")] public string gender;
        [Tooltip("나이 표시.")] public string age;
        [Tooltip("키 표시.")] public string height;
        [Tooltip("한 줄 소개.")] [TextArea(2, 4)] public string summary;
        [Tooltip("상징 모티프.")] public string symbolMotif;
        [Tooltip("출신지.")] public string origin;
        [Tooltip("소속.")] public string affiliation;
        [Tooltip("관계 메모 (시트 원문).")] [TextArea(2, 6)] public string relationships;
    }

    [Serializable]
    public sealed class AppearanceText // 외형 (모델·초상화 제작 참고)
    {
        [Tooltip("머리.")] public string hair;
        [Tooltip("눈.")] public string eyes;
        [Tooltip("체형 인상.")] public string bodyImpression;
        [Tooltip("복장.")] [TextArea(1, 3)] public string outfit;
        [Tooltip("장신구.")] public string accessories;
        [Tooltip("종족 특징 (귀·뿔·꼬리 등).")] [TextArea(1, 3)] public string features;
        [Tooltip("일러스트 스타일.")] public string illustrationStyle;
    }

    [Serializable]
    public sealed class PersonalityText // 내면 설정
    {
        [Tooltip("성격 태그.")] public string[] tags = Array.Empty<string>();
        [Tooltip("말투.")] [TextArea(1, 3)] public string speechStyle;
        [Tooltip("상처.")] [TextArea(1, 3)] public string trauma;
        [Tooltip("바라는 것.")] [TextArea(1, 3)] public string desire;
        [Tooltip("배경 이야기.")] [TextArea(2, 6)] public string background;
        [Tooltip("목소리 콘셉트.")] public string voiceConcept;
    }

    [Serializable]
    public sealed class GameplayIds // 다른 시스템과 연결할 ID (상점·제작·퀘스트·영입)
    {
        [Tooltip("상점 ID (Merchant 역할).")] public string shopId;
        [Tooltip("제작대 ID.")] public string craftingStationId;
        [Tooltip("마을 작업 능력 ID.")] public string workAbilityId;
        [Tooltip("퀘스트 묶음 ID.")] public string questGroupId;
        [Tooltip("영입 조건 ID (Companion 역할).")] public string recruitmentConditionId;
        [Tooltip("동료 능력 ID.")] public string companionAbilityId;
        [Tooltip("처음 만나는 조건 ID.")] public string unlockConditionId;
        [Tooltip("관계 표 ID.")] public string relationshipTableId;
    }

    [Header("Identity")] // 식별 정보
    [Tooltip("캐릭터 ID (char_lunette).")]
    [SerializeField] private string characterId = "char_new"; // 캐릭터 ID
    [Tooltip("게임 안 표시 이름 (한국어).")]
    [SerializeField] private string displayName = "새 캐릭터"; // 표시 이름
    [Tooltip("영문 이름.")]
    [SerializeField] private string englishName = "New"; // 영문 이름
    [Tooltip("알파 마을에 실제로 배치하는 NPC인지 여부.")]
    [SerializeField] private bool isAlphaCast; // 알파 배치 여부
    [Tooltip("시트의 IsAvailable 값.")]
    [SerializeField] private bool isAvailable = true; // 사용 가능 여부

    [Header("Profile")] // 기본 정보
    [SerializeField] private ProfileText profile = new ProfileText(); // 기본 정보
    [SerializeField] private AppearanceText appearance = new AppearanceText(); // 외형
    [Tooltip("대화 창 초상 (91일차 생성 도구가 저폴리 모델로 만듦).")]
    [SerializeField] private Sprite portrait; // 초상
    [Tooltip("대표 색 (이름표·대화창 강조).")]
    [SerializeField] private Color themeColor = Color.white; // 대표 색
    [Tooltip("보조 색.")]
    [SerializeField] private Color accentColor = Color.gray; // 보조 색
    [SerializeField] private PersonalityText personality = new PersonalityText(); // 내면 설정

    [Header("Gameplay")] // 게임 규칙
    [Tooltip("NPC 역할.")]
    [SerializeField] private NpcRole roles = NpcRole.DialogueOnly; // 역할
    [Tooltip("할 수 있는 상호작용.")]
    [SerializeField] private NpcInteraction interactions = NpcInteraction.Dialogue | NpcInteraction.Gift; // 상호작용
    [SerializeField] private GameplayIds gameplayIds = new GameplayIds(); // 연결 ID
    [Tooltip("시작 호감도.")]
    [SerializeField, Range(0, 100)] private int defaultAffinity; // 시작 호감도
    [Tooltip("최대 호감도.")]
    [SerializeField, Range(1, 100)] private int maxAffinity = 100; // 최대 호감도
    [Tooltip("생일 계절.")]
    [SerializeField] private SeasonType birthdaySeason = SeasonType.Spring; // 생일 계절
    [Tooltip("생일 날짜 (계절 안 1~28).")]
    [SerializeField, Range(1, 28)] private int birthdayDay = 1; // 생일 날짜
    [Tooltip("집 위치 ID (90일차 배치).")]
    [SerializeField] private string homeLocationId; // 집 위치
    [Tooltip("일터 위치 ID (90일차 배치).")]
    [SerializeField] private string workLocationId; // 일터 위치

    [Header("Links")] // 연결 데이터
    [Tooltip("선물 반응.")]
    [SerializeField] private NpcGiftProfile giftProfile; // 선물 반응
    [Tooltip("대사 묶음.")]
    [SerializeField] private NpcDialogueSet dialogueSet; // 대사
    [Tooltip("하루 일정 (알파 배치 NPC만).")]
    [SerializeField] private NpcScheduleData schedule; // 일정

    [Header("Source")] // 출처
    [Tooltip("데이터 상태 (확정 / 임시 / 미작성).")]
    [SerializeField] private string dataStatus; // 데이터 상태
    [Tooltip("시트에서 가져온 날짜와 임시로 채운 항목.")]
    [SerializeField, TextArea(1, 3)] private string sourceNote; // 출처 메모

    public string CharacterId => characterId; // ID 제공
    public string DisplayName => displayName; // 표시 이름 제공
    public string EnglishName => englishName; // 영문 이름 제공
    public bool IsAlphaCast => isAlphaCast; // 알파 배치 여부 제공
    public bool IsAvailable => isAvailable; // 사용 가능 여부 제공
    public ProfileText Profile => profile; // 기본 정보 제공
    public AppearanceText Appearance => appearance; // 외형 제공
    public Sprite Portrait => portrait; // 초상 제공
    public Color ThemeColor => themeColor; // 대표 색 제공
    public Color AccentColor => accentColor; // 보조 색 제공
    public PersonalityText Personality => personality; // 내면 설정 제공
    public NpcRole Roles => roles; // 역할 제공
    public NpcInteraction Interactions => interactions; // 상호작용 제공
    public GameplayIds Ids => gameplayIds; // 연결 ID 제공
    public int DefaultAffinity => Mathf.Clamp(defaultAffinity, 0, MaxAffinity); // 시작 호감도 제공
    public int MaxAffinity => Mathf.Clamp(maxAffinity, 1, 100); // 최대 호감도 제공
    public SeasonType BirthdaySeason => birthdaySeason; // 생일 계절 제공
    public int BirthdayDay => birthdayDay; // 생일 날짜 제공
    public string HomeLocationId => homeLocationId; // 집 위치 제공
    public string WorkLocationId => workLocationId; // 일터 위치 제공
    public NpcGiftProfile GiftProfile => giftProfile; // 선물 반응 제공
    public NpcDialogueSet DialogueSet => dialogueSet; // 대사 제공
    public NpcScheduleData Schedule => schedule; // 일정 제공
    public string DataStatus => dataStatus; // 데이터 상태 제공
    public string SourceNote => sourceNote; // 출처 메모 제공

    public bool HasRole(NpcRole role) => (roles & role) == role; // 역할 확인
    public bool CanInteract(NpcInteraction interaction) => (interactions & interaction) == interaction; // 상호작용 확인
    public bool IsBirthday(SeasonType season, int dayInSeason) => season == birthdaySeason && dayInSeason == birthdayDay; // 생일 확인

#if UNITY_EDITOR
    public void EditorAssignIdentity(string id, string koreanName, string latinName, bool alphaCast, bool available) // 생성 도구 전용
    {
        characterId = id;
        displayName = koreanName;
        englishName = latinName;
        isAlphaCast = alphaCast;
        isAvailable = available;
    }

    public void EditorAssignText(ProfileText newProfile, AppearanceText newAppearance, PersonalityText newPersonality, Color theme, Color accent) // 생성 도구 전용
    {
        profile = newProfile ?? new ProfileText();
        appearance = newAppearance ?? new AppearanceText();
        personality = newPersonality ?? new PersonalityText();
        themeColor = theme;
        accentColor = accent;
    }

    public void EditorAssignGameplay(NpcRole newRoles, NpcInteraction newInteractions, GameplayIds ids, int startAffinity, int affinityLimit, SeasonType season, int day, string home, string work) // 생성 도구 전용
    {
        roles = newRoles;
        interactions = newInteractions;
        gameplayIds = ids ?? new GameplayIds();
        maxAffinity = Mathf.Clamp(affinityLimit, 1, 100);
        defaultAffinity = Mathf.Clamp(startAffinity, 0, maxAffinity);
        birthdaySeason = season;
        birthdayDay = Mathf.Clamp(day, 1, 28);
        homeLocationId = home;
        workLocationId = work;
    }

    public void EditorAssignPortrait(Sprite sprite) // 생성 도구 전용
    {
        portrait = sprite;
    }

    public void EditorAssignLinks(NpcGiftProfile gifts, NpcDialogueSet dialogue, NpcScheduleData dailySchedule, string status, string note) // 생성 도구 전용
    {
        giftProfile = gifts;
        dialogueSet = dialogue;
        schedule = dailySchedule;
        dataStatus = status;
        sourceNote = note;
    }
#endif
}
