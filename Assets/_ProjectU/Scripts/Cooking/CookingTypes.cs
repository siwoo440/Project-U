using UnityEngine; // Unity 기본 기능

public enum CookingStationTier // 85일차: 조리 시설 등급
{
    Campfire = 0, // 모닥불 (굽기)
    StoneCampfire = 1, // 돌 모닥불 (냄비 요리까지)
    Furnace = 2, // 117일차: 용광로 (광석 → 주괴)
    TanningRack = 3, // 118일차: 무두질대 (가죽 → 무두질 가죽)
    Mortar = 4 // 119일차: 절구 (옥수수 · 도토리 → 동물 사료)
}

public enum CookingRecipeStatus // 요리법 현재 상태
{
    Ready = 0, // 바로 조리 가능
    MissingItems = 1, // 재료 부족
    NeedStation = 2, // 더 좋은 조리 시설 필요
    NoFreeSlot = 3 // 빈 조리 칸 없음
}

public enum FoodBuffType // 음식 보조 효과 종류
{
    None = 0, // 효과 없음
    StaminaRecovery = 1, // 스태미나 회복 속도 증가
    Warmth = 2, // 추위 감소
    MoveSpeed = 3, // 이동 속도 증가
    Satiety = 4 // 허기 감소 속도 감소
}

public static class CookingStationUtility // 조리 시설 표시 도우미
{
    public static string GetLabel(CookingStationTier tier) // 시설 표시 이름
    {
        switch (tier) // 등급 분기
        {
            case CookingStationTier.StoneCampfire: return "STONE CAMPFIRE"; // 돌 모닥불
            case CookingStationTier.Furnace: return "FURNACE"; // 117일차: 용광로
            case CookingStationTier.TanningRack: return "TANNING RACK"; // 118일차: 무두질대
            case CookingStationTier.Mortar: return "MORTAR"; // 119일차: 절구
            default: return "CAMPFIRE"; // 모닥불
        }
    }
}

public static class FoodBuffUtility // 음식 보조 효과 표시 도우미
{
    public static string GetLabel(FoodBuffType type, float strength) // 효과 문구
    {
        switch (type) // 종류 분기
        {
            case FoodBuffType.StaminaRecovery: return $"STAMINA +{strength:0}%"; // 스태미나
            case FoodBuffType.Warmth: return $"WARM -{strength:0}% COLD"; // 보온
            case FoodBuffType.MoveSpeed: return $"SPEED +{strength:0}%"; // 이동 속도
            case FoodBuffType.Satiety: return $"FULL -{strength:0}% HUNGER"; // 포만감
            default: return string.Empty; // 효과 없음
        }
    }

    public static string GetShortLabel(FoodBuffType type, float strength) // HUD용 짧은 문구
    {
        switch (type) // 종류 분기
        {
            case FoodBuffType.StaminaRecovery: return $"STAMINA +{strength:0}%"; // 스태미나
            case FoodBuffType.Warmth: return "WARM"; // 보온
            case FoodBuffType.MoveSpeed: return $"SPEED +{strength:0}%"; // 이동 속도
            case FoodBuffType.Satiety: return "FULL"; // 포만감
            default: return string.Empty; // 효과 없음
        }
    }

    public static Color GetColor(FoodBuffType type) // 효과 색상
    {
        switch (type) // 종류 분기
        {
            case FoodBuffType.StaminaRecovery: return new Color(0.49f, 0.83f, 0.4f, 1f); // 초록
            case FoodBuffType.Warmth: return new Color(0.98f, 0.55f, 0.25f, 1f); // 주황
            case FoodBuffType.MoveSpeed: return new Color(0.42f, 0.78f, 0.95f, 1f); // 하늘
            case FoodBuffType.Satiety: return new Color(0.94f, 0.63f, 0.29f, 1f); // 허기색
            default: return Color.white; // 기본
        }
    }

    public static string FormatTime(float seconds) // 분:초 문구
    {
        int total = Mathf.Max(0, Mathf.CeilToInt(seconds)); // 올림 초
        return $"{total / 60}:{total % 60:00}"; // 결과 반환
    }
}
