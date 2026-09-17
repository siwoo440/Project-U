using System.Collections.Generic; // 목록 기능
using System.Text; // 문자열 조립 기능
using UnityEngine; // Unity 기본 기능

public enum FoodEffectKind // 85일차: 음식 효과 표시 종류
{
    Hunger = 0, // 허기 회복
    Thirst = 1, // 갈증 회복
    Health = 2, // 체력 회복
    Buff = 3 // 보조 효과
}

public struct FoodEffectEntry // 음식 효과 한 줄
{
    public FoodEffectKind Kind; // 종류
    public FoodBuffType BuffType; // 보조 효과 종류
    public string Label; // 문구
    public Color Color; // 색상
}

public static class FoodEffectUtility // 음식 효과 목록·문구 도우미 (요리 창과 아이템 상세 공통)
{
    public static readonly Color HungerColor = new Color(0.94f, 0.63f, 0.29f, 1f); // 허기 색
    public static readonly Color ThirstColor = new Color(0.31f, 0.7f, 0.91f, 1f); // 갈증 색
    public static readonly Color HealthColor = new Color(0.9f, 0.33f, 0.31f, 1f); // 체력 색

    public static void Collect(ItemData item, List<FoodEffectEntry> output) // 효과 목록 만들기
    {
        output.Clear(); // 초기화

        if (item == null) // 아이템 확인
        {
            return; // 생략
        }

        float hunger = item.HungerRestoreAmount; // 허기
        float thirst = item.IsFood ? item.FoodThirstRestoreAmount : item.ThirstRestoreAmount; // 갈증
        float health = item.IsFood ? item.FoodHealthRestoreAmount : item.HealthRestoreAmount; // 체력

        if (hunger > 0f) // 허기 효과
        {
            output.Add(new FoodEffectEntry { Kind = FoodEffectKind.Hunger, Label = $"HUNGER +{hunger:0}", Color = HungerColor }); // 추가
        }

        if (thirst > 0f) // 갈증 효과
        {
            output.Add(new FoodEffectEntry { Kind = FoodEffectKind.Thirst, Label = $"THIRST +{thirst:0}", Color = ThirstColor }); // 추가
        }

        if (health > 0f) // 체력 효과
        {
            output.Add(new FoodEffectEntry { Kind = FoodEffectKind.Health, Label = $"HEALTH +{health:0}", Color = HealthColor }); // 추가
        }

        if (item.FoodBuffType != FoodBuffType.None) // 보조 효과
        {
            output.Add(new FoodEffectEntry
            {
                Kind = FoodEffectKind.Buff,
                BuffType = item.FoodBuffType,
                Label = $"{FoodBuffUtility.GetShortLabel(item.FoodBuffType, item.FoodBuffStrength)} {FoodBuffUtility.FormatTime(item.FoodBuffDuration)}",
                Color = FoodBuffUtility.GetColor(item.FoodBuffType)
            }); // 추가
        }
    }

    public static string BuildRichText(ItemData item) // 아이템 상세용 한 줄 문구
    {
        List<FoodEffectEntry> entries = new List<FoodEffectEntry>(); // 목록
        Collect(item, entries); // 수집

        if (entries.Count == 0) // 효과 확인
        {
            return string.Empty; // 없음
        }

        StringBuilder builder = new StringBuilder(); // 조립

        foreach (FoodEffectEntry entry in entries) // 순회
        {
            if (builder.Length > 0) // 구분
            {
                builder.Append("   "); // 간격
            }

            string label = entry.Kind == FoodEffectKind.Buff
                ? $"{FoodBuffUtility.GetLabel(entry.BuffType, item.FoodBuffStrength)} {FoodBuffUtility.FormatTime(item.FoodBuffDuration)}"
                : entry.Label; // 상세 문구
            builder.Append($"<color=#{ColorUtility.ToHtmlStringRGB(entry.Color)}>{label}</color>"); // 색 문구
        }

        return builder.ToString(); // 결과 반환
    }
}
