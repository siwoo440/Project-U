using System; // Flags 특성

// 89일차: 캐릭터 시트 공통 코드표 (NpcRole · InteractionType · AffinityStage · GiftPreference)

[Flags]
public enum NpcRole // NPC 역할 (여러 개 가능)
{
    None = 0,
    DialogueOnly = 1 << 0, // 대화 전용
    Merchant = 1 << 1, // 상점 운영
    QuestGiver = 1 << 2, // 퀘스트 제공
    Crafter = 1 << 3, // 제작 담당
    Worker = 1 << 4, // 마을 작업
    Companion = 1 << 5, // 동료 영입 가능
    Guard = 1 << 6, // 경비·호위 가능
    Special = 1 << 7 // 특수 상호작용
}

[Flags]
public enum NpcInteraction // 상호작용 종류 (여러 개 가능)
{
    None = 0,
    Dialogue = 1 << 0, // 대화
    Trade = 1 << 1, // 거래
    Craft = 1 << 2, // 제작
    Quest = 1 << 3, // 퀘스트
    Recruit = 1 << 4, // 영입
    Gift = 1 << 5, // 선물
    Train = 1 << 6 // 훈련
}

public enum AffinityStage // 호감도 단계 (0~100)
{
    Uninterested = 0, // 무관심 0~19
    Curious = 1, // 호기심 20~39
    Trust = 2, // 신뢰 40~59
    Affection = 3, // 애정 60~79
    Love = 4 // 사랑 80~100
}

public enum GiftPreference // 선물 반응 5단계
{
    Loved = 0, // 매우 좋아함
    Liked = 1, // 좋아함
    Neutral = 2, // 보통
    Disliked = 3, // 싫어함
    Hated = 4 // 매우 싫어함
}

public enum NpcDialogueKind // 대사 종류
{
    FirstMeeting = 0, // 처음 만났을 때
    Greeting = 1, // 인사
    Talk = 2, // 평소 잡담
    Stage = 3, // 호감도 단계별 (stage 사용)
    Season = 4, // 계절별 (season 사용)
    Weather = 5, // 날씨별 (weather 사용)
    Gift = 6, // 선물 반응 (giftPreference 사용)
    Birthday = 7, // 생일 선물
    Event = 8 // 이벤트 대사
}

[Flags]
public enum NpcWeekdays // 요일 (7일 주기, 1일차 = 월요일)
{
    None = 0,
    Monday = 1 << 0,
    Tuesday = 1 << 1,
    Wednesday = 1 << 2,
    Thursday = 1 << 3,
    Friday = 1 << 4,
    Saturday = 1 << 5,
    Sunday = 1 << 6,
    All = Monday | Tuesday | Wednesday | Thursday | Friday | Saturday | Sunday
}

[Flags]
public enum NpcSeasons // 계절 조건
{
    None = 0,
    Spring = 1 << 0,
    Summer = 1 << 1,
    Autumn = 1 << 2,
    Winter = 1 << 3,
    All = Spring | Summer | Autumn | Winter
}

public enum NpcWeatherCondition // 날씨 조건
{
    Any = 0, // 날씨 무관
    Fair = 1, // 맑음 · 흐림
    Bad = 2 // 비 · 눈 · 폭풍
}

public static class NpcCalendar // 날짜 → 요일·계절 조건 변환
{
    public const int DaysPerWeek = 7; // 한 주 길이

    public static NpcWeekdays GetWeekday(int day) // 1일차 = 월요일
    {
        int index = ((Math.Max(1, day) - 1) % DaysPerWeek + DaysPerWeek) % DaysPerWeek; // 0 = 월요일
        return (NpcWeekdays)(1 << index); // 요일 플래그 반환
    }

    public static NpcSeasons ToMask(SeasonType season) // 계절 → 계절 조건
    {
        return (NpcSeasons)(1 << (int)season); // 계절 플래그 반환
    }

    public static bool IsBadWeather(WeatherType weather) // 비·눈·폭풍 여부
    {
        return weather == WeatherType.Rain || weather == WeatherType.Snow || weather == WeatherType.Storm; // 나쁜 날씨 판정
    }

    public static bool Matches(NpcWeatherCondition condition, WeatherType weather) // 날씨 조건 일치 여부
    {
        switch (condition)
        {
            case NpcWeatherCondition.Fair:
                return !IsBadWeather(weather); // 맑음·흐림만 허용
            case NpcWeatherCondition.Bad:
                return IsBadWeather(weather); // 비·눈·폭풍만 허용
            default:
                return true; // 날씨 무관
        }
    }
}
