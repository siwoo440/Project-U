using UnityEngine; // Unity 기본 기능

public static class ItemIconUtility // 아이템 아이콘 보조 기능
{
    public static Color GetFallbackColor(ItemCategory itemCategory) // 분류별 대체 색상 반환
    {
        switch (itemCategory) // 아이템 분류 확인
        {
            case ItemCategory.CraftingMaterial: // 제작 재료 분기
                return new Color(0.55f, 0.36f, 0.18f, 1f); // 갈색 반환

            case ItemCategory.Tool: // 도구 분기
                return new Color(0.45f, 0.55f, 0.65f, 1f); // 금속색 반환

            case ItemCategory.Food: // 음식 분기
                return new Color(0.8f, 0.25f, 0.3f, 1f); // 붉은색 반환

            case ItemCategory.Equipment: // 장비 분기
                return new Color(0.25f, 0.45f, 0.8f, 1f); // 푸른색 반환

            default: // 미정 분류
                return Color.white; // 기본색 반환
        }
    }
}