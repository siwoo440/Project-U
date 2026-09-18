using System; // Serializable · 이벤트
using System.Collections.Generic; // 목록
using UnityEngine; // Unity 기본 기능

[Serializable] // JSON 저장
public sealed class NpcRelationshipSaveData // 91일차: NPC 한 명과의 관계 (캐릭터 시트 CharacterRuntimeState)
{
    [Tooltip("캐릭터 ID.")]
    public string characterId = string.Empty; // 캐릭터 ID
    [Tooltip("처음 만났는지 (IsDiscovered).")]
    public bool met; // 만남 여부
    [Tooltip("현재 호감도 (CurrentAffinity).")]
    public int affinity; // 호감도
    [Tooltip("마지막으로 대화해 호감도를 받은 날짜.")]
    public int lastTalkDay = -1; // 마지막 대화 날짜
    [Tooltip("마지막으로 선물한 날짜 (LastGiftDay).")]
    public int lastGiftDay = -1; // 마지막 선물 날짜
    [Tooltip("마지막 선물 날짜에 준 선물 수.")]
    public int giftsOnLastGiftDay; // 그날 선물 수
    [Tooltip("지금까지 받은 선물 수 (ReceivedGiftCount).")]
    public int receivedGiftCount; // 받은 선물 수
    [Tooltip("지금까지 대화한 날 수.")]
    public int talkDays; // 대화한 날 수
    [Tooltip("95일차: 마지막으로 선물한 주 (1일차가 있는 주 = 0).")]
    public int giftWeek = -1; // 마지막 선물 주
    [Tooltip("95일차: 그 주에 준 선물 수 (생일 선물 제외).")]
    public int giftsThisWeek; // 그 주 선물 수
}

[Serializable] // JSON 저장
public sealed class NpcSaveData // 91일차: 전체 NPC 관계 저장
{
    [Tooltip("NPC별 관계.")]
    public List<NpcRelationshipSaveData> relationships = new List<NpcRelationshipSaveData>(); // 관계 목록
}

public readonly struct NpcGiftResult // 선물 결과
{
    public readonly bool Accepted; // 받았는지
    public readonly GiftPreference Preference; // 반응
    public readonly int Points; // 호감도 변화
    public readonly bool Birthday; // 생일 선물
    public readonly string Reason; // 못 받은 이유

    public NpcGiftResult(bool accepted, GiftPreference preference, int points, bool birthday, string reason)
    {
        Accepted = accepted;
        Preference = preference;
        Points = points;
        Birthday = birthday;
        Reason = reason;
    }
}

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class NpcRelationshipManager : MonoBehaviour // 91일차: NPC 호감도 · 만남 · 하루 대화·선물 제한 / 95일차: 한 주 선물 제한
{
    public static NpcRelationshipManager Instance { get; private set; } // Scene 관리자

    [Tooltip("NPC 데이터베이스 (호감도 규칙).")]
    [SerializeField] private NpcDatabase database; // 데이터베이스
    [Tooltip("현재 관계 (실행 중 확인용).")]
    [SerializeField] private List<NpcRelationshipSaveData> relationships = new List<NpcRelationshipSaveData>(); // 관계

    private readonly Dictionary<string, NpcRelationshipSaveData> lookup = new Dictionary<string, NpcRelationshipSaveData>(StringComparer.Ordinal); // 검색

    public event Action<NpcCharacterData, int, AffinityStage, AffinityStage> AffinityChanged; // 호감도 변화 (NPC, 변화량, 이전 단계, 새 단계)

    public NpcDatabase Database => database; // 데이터베이스 제공
    public IReadOnlyList<NpcRelationshipSaveData> Relationships => relationships; // 관계 제공

    private void Awake() // 준비
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("NpcRelationshipManager가 두 개 있습니다. 나중 것은 사용하지 않습니다.", this);
            enabled = false;
            return;
        }

        Instance = this;
        RebuildLookup();
    }

    private void OnDestroy() // 정리
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public NpcRelationshipSaveData GetState(NpcCharacterData character) // 관계 (없으면 시작 호감도로 만듦)
    {
        if (character == null)
        {
            return null;
        }

        if (!lookup.TryGetValue(character.CharacterId, out NpcRelationshipSaveData state))
        {
            state = new NpcRelationshipSaveData { characterId = character.CharacterId, affinity = character.DefaultAffinity };
            relationships.Add(state);
            lookup.Add(state.characterId, state);
        }

        return state;
    }

    public int GetAffinity(NpcCharacterData character) => GetState(character)?.affinity ?? 0; // 호감도
    public AffinityStage GetStage(NpcCharacterData character) => database != null ? database.GetStage(GetAffinity(character)) : AffinityStage.Uninterested; // 단계
    public bool HasMet(NpcCharacterData character) => GetState(character)?.met ?? false; // 만남 여부

    public AffinityStage PeekStage(NpcCharacterData character) // 92일차: 관계 기록을 새로 만들지 않고 단계만 확인 (상점 할인용)
    {
        if (character == null || database == null)
        {
            return AffinityStage.Uninterested;
        }

        int affinity = lookup.TryGetValue(character.CharacterId, out NpcRelationshipSaveData state) ? state.affinity : character.DefaultAffinity;
        return database.GetStage(affinity);
    }

    public bool HasTalkedToday(NpcCharacterData character, int day) // 오늘 대화 점수를 받았는지
    {
        NpcRelationshipSaveData state = GetState(character);
        return state != null && state.lastTalkDay == day;
    }

    public int GiftsLeftToday(NpcCharacterData character, int day, bool isBirthday = false) // 오늘 더 줄 수 있는 선물 수 (95일차: 한 주 제한 포함, 생일은 한 주 제한 없음)
    {
        NpcRelationshipSaveData state = GetState(character);
        int limit = database != null ? database.GiftsPerDay : 1;
        int given = state != null && state.lastGiftDay == day ? state.giftsOnLastGiftDay : 0;
        int left = Mathf.Max(0, limit - given);
        return isBirthday ? left : Mathf.Min(left, GiftsLeftThisWeek(character, day));
    }

    public int GiftsLeftThisWeek(NpcCharacterData character, int day) // 95일차: 이번 주에 더 줄 수 있는 선물 수
    {
        NpcRelationshipSaveData state = GetState(character);
        int limit = database != null ? database.GiftsPerWeek : 2;
        int given = state != null && state.giftWeek == WeekOf(day) ? state.giftsThisWeek : 0;
        return Mathf.Max(0, limit - given);
    }

    public string GiftLimitReason(NpcCharacterData character, int day, bool isBirthday = false) // 95일차: 선물을 줄 수 없는 이유 (줄 수 있으면 빈 문자열)
    {
        NpcRelationshipSaveData state = GetState(character);
        int dailyLimit = database != null ? database.GiftsPerDay : 1;

        if (state != null && state.lastGiftDay == day && state.giftsOnLastGiftDay >= dailyLimit)
        {
            return "오늘은 이미 선물을 받았어요. 내일 다시 주세요.";
        }

        if (!isBirthday && GiftsLeftThisWeek(character, day) <= 0)
        {
            int weekly = database != null ? database.GiftsPerWeek : 2;
            return $"이번 주에는 선물을 {weekly}번 받았어요. 월요일부터 다시 줄 수 있어요.";
        }

        return string.Empty;
    }

    public static int WeekOf(int day) => (Mathf.Max(1, day) - 1) / NpcCalendar.DaysPerWeek; // 95일차: 날짜 → 주 번호 (1일차 = 월요일)

    public bool MarkMet(NpcCharacterData character) // 처음 만남 기록 (처음이면 true)
    {
        NpcRelationshipSaveData state = GetState(character);

        if (state == null || state.met)
        {
            return false;
        }

        state.met = true;
        return true;
    }

    public int Talk(NpcCharacterData character, int day) // 오늘 첫 대화면 호감도 +, 받은 점수 반환
    {
        NpcRelationshipSaveData state = GetState(character);

        if (state == null || state.lastTalkDay == day)
        {
            return 0;
        }

        state.lastTalkDay = day;
        state.talkDays++;
        int points = database != null ? database.DailyTalkPoints : 1;
        ChangeAffinity(character, points);
        return points;
    }

    public NpcGiftResult GiveGift(NpcCharacterData character, ItemData item, int day, bool isBirthday) // 선물 (아이템 차감은 호출하는 쪽)
    {
        NpcRelationshipSaveData state = GetState(character);

        if (state == null || item == null || character.GiftProfile == null)
        {
            return new NpcGiftResult(false, GiftPreference.Neutral, 0, false, "선물을 줄 수 없어요.");
        }

        string limitReason = GiftLimitReason(character, day, isBirthday);

        if (limitReason.Length > 0)
        {
            return new NpcGiftResult(false, GiftPreference.Neutral, 0, false, limitReason);
        }

        GiftPreference preference = character.GiftProfile.GetPreference(item);
        int points = database != null ? database.GetGiftPoints(preference, isBirthday) : 0;

        if (state.lastGiftDay != day)
        {
            state.lastGiftDay = day;
            state.giftsOnLastGiftDay = 0;
        }

        state.giftsOnLastGiftDay++;
        state.receivedGiftCount++;

        if (!isBirthday) // 95일차: 생일 선물은 한 주 선물 수에 넣지 않는다
        {
            int week = WeekOf(day);

            if (state.giftWeek != week)
            {
                state.giftWeek = week;
                state.giftsThisWeek = 0;
            }

            state.giftsThisWeek++;
        }

        state.met = true;
        ChangeAffinity(character, points);
        return new NpcGiftResult(true, preference, points, isBirthday, string.Empty);
    }

    public void ChangeAffinity(NpcCharacterData character, int delta) // 호감도 변화 (0 ~ 최대값)
    {
        NpcRelationshipSaveData state = GetState(character);

        if (state == null || delta == 0)
        {
            return;
        }

        AffinityStage before = GetStage(character);
        int next = Mathf.Clamp(state.affinity + delta, 0, character.MaxAffinity);
        int applied = next - state.affinity;
        state.affinity = next;

        if (applied != 0)
        {
            AffinityChanged?.Invoke(character, applied, before, GetStage(character));
        }
    }

    public NpcSaveData CaptureSaveData() // 저장
    {
        NpcSaveData data = new NpcSaveData();

        foreach (NpcRelationshipSaveData state in relationships)
        {
            if (state == null || string.IsNullOrEmpty(state.characterId))
            {
                continue;
            }

            data.relationships.Add(new NpcRelationshipSaveData
            {
                characterId = state.characterId, met = state.met, affinity = state.affinity, lastTalkDay = state.lastTalkDay,
                lastGiftDay = state.lastGiftDay, giftsOnLastGiftDay = state.giftsOnLastGiftDay,
                receivedGiftCount = state.receivedGiftCount, talkDays = state.talkDays,
                giftWeek = state.giftWeek, giftsThisWeek = state.giftsThisWeek
            });
        }

        data.relationships.Sort((left, right) => string.CompareOrdinal(left.characterId, right.characterId));
        return data;
    }

    public void ApplySaveData(NpcSaveData data) // 불러오기
    {
        relationships.Clear();

        if (data != null && data.relationships != null)
        {
            foreach (NpcRelationshipSaveData state in data.relationships)
            {
                if (state == null || string.IsNullOrEmpty(state.characterId))
                {
                    continue;
                }

                NpcCharacterData character = null;
                database?.TryGet(state.characterId, out character);
                int max = character != null ? character.MaxAffinity : 100;
                state.affinity = Mathf.Clamp(state.affinity, 0, max);
                relationships.Add(state);
            }
        }

        RebuildLookup();
    }

    public void ResetForLoad() // 이전 저장 파일 : 관계 없이 시작
    {
        relationships.Clear();
        RebuildLookup();
    }

    private void RebuildLookup() // 검색 표
    {
        lookup.Clear();

        foreach (NpcRelationshipSaveData state in relationships)
        {
            if (state != null && !string.IsNullOrEmpty(state.characterId) && !lookup.ContainsKey(state.characterId))
            {
                lookup.Add(state.characterId, state);
            }
        }
    }

#if UNITY_EDITOR
    public void EditorAssign(NpcDatabase data) // 생성 도구 전용
    {
        database = data;
    }
#endif
}
