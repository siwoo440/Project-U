using System; // Flags 기능
using UnityEngine; // 색상 기능

public enum WaterBodyType // 낚시 가능한 물가 종류 (기획서 9.3)
{
    River = 0, // 강
    Lake = 1, // 호수·연못
    Beach = 2, // 해변
    CaveLake = 3, // 동굴 호수
    IceHole = 4, // 설원 얼음 구멍
    Hidden = 5 // 숨겨진 낚시터
}

public enum FishRarity // 물고기 희귀도
{
    Common = 0, // 일반
    Uncommon = 1, // 드묾
    Rare = 2, // 희귀
    Legendary = 3 // 전설
}

[Flags] // 여러 시간대 선택 허용
public enum FishTimeWindow // 물고기 출현 시간대
{
    None = 0, // 없음
    Morning = 1, // 아침 06~12시
    Afternoon = 2, // 오후 12~18시
    Evening = 4, // 저녁 18~22시
    Night = 8, // 밤 22~06시
    AnyTime = Morning | Afternoon | Evening | Night // 언제나
}

public enum FishingState // 플레이어 낚시 진행 상태
{
    Idle = 0, // 낚시하지 않음
    Casting = 1, // 찌를 던지는 중
    Waiting = 2, // 입질 대기
    Bite = 3, // 입질 (챔질 가능)
    Reeling = 4 // 끌어올리는 중 (83일차 미니게임)
}

public static class FishTimeWindowUtility // 게임 시간과 출현 시간대 변환
{
    public static FishTimeWindow FromHour(float hour) // 시각으로 시간대 계산
    {
        if (hour >= 6f && hour < 12f) // 아침 확인
        {
            return FishTimeWindow.Morning; // 아침 반환
        }

        if (hour >= 12f && hour < 18f) // 오후 확인
        {
            return FishTimeWindow.Afternoon; // 오후 반환
        }

        if (hour >= 18f && hour < 22f) // 저녁 확인
        {
            return FishTimeWindow.Evening; // 저녁 반환
        }

        return FishTimeWindow.Night; // 밤 반환
    }
}

public readonly struct FishingConditions // 83일차: 물고기 출현 판정에 쓰는 현재 조건 묶음
{
    public readonly WaterBodyType WaterBody; // 물가 종류
    public readonly SeasonType Season; // 계절
    public readonly WeatherType Weather; // 날씨
    public readonly float Hour; // 시각
    public readonly int RodTier; // 낚싯대 등급
    public readonly bool UsingBait; // 미끼 사용 여부

    public FishingConditions(WaterBodyType waterBody, SeasonType season, WeatherType weather, float hour, int rodTier, bool usingBait) // 조건 생성
    {
        WaterBody = waterBody; // 물가 저장
        Season = season; // 계절 저장
        Weather = weather; // 날씨 저장
        Hour = hour; // 시각 저장
        RodTier = rodTier; // 등급 저장
        UsingBait = usingBait; // 미끼 저장
    }

    public override string ToString() // 로그용 문자열
    {
        return $"{WaterBody} / {Season} / {Weather} / {Hour:00.0}h / 낚싯대 {RodTier} / 미끼 {(UsingBait ? "O" : "X")}"; // 조건 문자열 반환
    }
}

public static class FishRarityUtility // 83일차: 희귀도별 표시 이름과 색상
{
    public static string GetLabel(FishRarity rarity) // 표시 이름 반환
    {
        switch (rarity) // 희귀도 확인
        {
            case FishRarity.Uncommon: return "UNCOMMON"; // 드묾
            case FishRarity.Rare: return "RARE"; // 희귀
            case FishRarity.Legendary: return "LEGENDARY"; // 전설
            default: return "COMMON"; // 일반
        }
    }

    public static Color GetColor(FishRarity rarity) // 표시 색상 반환
    {
        switch (rarity) // 희귀도 확인
        {
            case FishRarity.Uncommon: return new Color(0.45f, 0.88f, 0.52f, 1f); // 초록
            case FishRarity.Rare: return new Color(0.42f, 0.7f, 1f, 1f); // 파랑
            case FishRarity.Legendary: return new Color(1f, 0.78f, 0.28f, 1f); // 금색
            default: return new Color(0.9f, 0.92f, 0.95f, 1f); // 흰색
        }
    }
}
