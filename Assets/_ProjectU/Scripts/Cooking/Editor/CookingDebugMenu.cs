using System.Text;
using UnityEditor;
using UnityEngine;

// 85일차: Play 중 요리 확인용 메뉴
public static class CookingDebugMenu
{
    private const string Root = "Tools/Project U/Cooking/Debug (Play Mode)/";

    private static readonly (string itemId, int amount)[] Kit =
    {
        ("item_wood", 10), ("food_apple", 3), ("food_potato", 4), ("item_wild_mushroom", 4),
        ("resource_fish_crucian", 2), ("resource_fish_trout", 1), ("food_pumpkin", 2), ("drink_water_bottle", 3),
        ("food_tomato", 4), ("resource_fish_golden_carp", 1), ("food_winter_radish", 1), ("resource_stone", 10)
    };

    [MenuItem(Root + "Give Cooking Ingredients", false, 0)]
    private static void GiveIngredients()
    {
        PlayerInventory inventory = Object.FindFirstObjectByType<PlayerInventory>();

        if (inventory == null)
        {
            return;
        }

        StringBuilder report = new StringBuilder("[요리 재료 지급]");

        foreach ((string itemId, int amount) in Kit)
        {
            ItemData item = FindItem(itemId);

            if (item == null)
            {
                report.Append($"\n{itemId} 없음");
                continue;
            }

            int left = inventory.AddItem(item, amount);
            report.Append($"\n{item.DisplayName} x{amount - left}{(left > 0 ? " (가방 가득 참)" : string.Empty)}");
        }

        Debug.Log(report.ToString());
    }

    [MenuItem(Root + "Finish Cooking (Nearest Campfire)", false, 20)]
    private static void FinishCooking()
    {
        CampfireCookingStation station = FindNearestStation();

        if (station == null)
        {
            Debug.LogWarning("가까운 모닥불이 없습니다. B 키 건축으로 모닥불을 설치하세요.");
            return;
        }

        station.DebugCompleteAll();
        Debug.Log($"{station.name} 조리 완료 (완성 칸 {station.ReadyCount})");
    }

    [MenuItem(Root + "Log Nearest Campfire", false, 21)]
    private static void LogNearest()
    {
        CampfireCookingStation station = FindNearestStation();

        if (station == null)
        {
            Debug.LogWarning("가까운 모닥불이 없습니다.");
            return;
        }

        StringBuilder report = new StringBuilder($"[{station.name}] {station.StationDisplayName}, 칸 {station.SlotCount}");

        for (int index = 0; index < station.Slots.Count; index++)
        {
            CookingSlot slot = station.Slots[index];
            string state = slot.IsEmpty ? "EMPTY" : slot.IsReady ? $"READY {slot.Recipe.RecipeId} x{slot.ReadyAmount}" : $"COOKING {slot.Recipe.RecipeId} x{slot.BatchCount} {slot.RemainingSeconds:0.0}s";
            report.Append($"\n  {index}: {state}");
        }

        Debug.Log(report.ToString());
    }

    [MenuItem(Root + "Buff/Stamina +30% (45s)", false, 40)]
    private static void BuffStamina() => ApplyBuff(FoodBuffType.StaminaRecovery, 30f);

    [MenuItem(Root + "Buff/Warm -50% Cold (45s)", false, 41)]
    private static void BuffWarmth() => ApplyBuff(FoodBuffType.Warmth, 50f);

    [MenuItem(Root + "Buff/Speed +12% (45s)", false, 42)]
    private static void BuffSpeed() => ApplyBuff(FoodBuffType.MoveSpeed, 12f);

    [MenuItem(Root + "Buff/Full -40% Hunger (45s)", false, 43)]
    private static void BuffSatiety() => ApplyBuff(FoodBuffType.Satiety, 40f);

    [MenuItem(Root + "Buff/Clear All", false, 60)]
    private static void ClearBuffs()
    {
        if (FoodBuffController.Local != null)
        {
            FoodBuffController.Local.ClearAll();
        }
    }

    [MenuItem(Root + "Give Cooking Ingredients", true)]
    [MenuItem(Root + "Finish Cooking (Nearest Campfire)", true)]
    [MenuItem(Root + "Log Nearest Campfire", true)]
    [MenuItem(Root + "Buff/Stamina +30% (45s)", true)]
    [MenuItem(Root + "Buff/Warm -50% Cold (45s)", true)]
    [MenuItem(Root + "Buff/Speed +12% (45s)", true)]
    [MenuItem(Root + "Buff/Full -40% Hunger (45s)", true)]
    [MenuItem(Root + "Buff/Clear All", true)]
    private static bool IsPlaying()
    {
        return EditorApplication.isPlaying;
    }

    private static void ApplyBuff(FoodBuffType type, float strength)
    {
        if (FoodBuffController.Local == null)
        {
            Debug.LogWarning("플레이어에 FoodBuffController가 없습니다. 요리 콘텐츠 생성 도구를 실행하세요.");
            return;
        }

        FoodBuffController.Local.Apply(type, strength, 45f);
    }

    private static ItemData FindItem(string itemId)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:ItemData", new[] { "Assets/_ProjectU/Data" }))
        {
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(guid));

            if (item != null && item.ItemId == itemId)
            {
                return item;
            }
        }

        return null;
    }

    private static CampfireCookingStation FindNearestStation()
    {
        PlayerInventory player = Object.FindFirstObjectByType<PlayerInventory>();
        CampfireCookingStation best = null;
        float bestDistance = float.MaxValue;

        foreach (CampfireCookingStation station in CampfireCookingStation.ActiveStations)
        {
            float distance = player != null ? (station.transform.position - player.transform.position).sqrMagnitude : 0f;

            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = station;
            }
        }

        return best;
    }
}
