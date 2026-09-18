using System.Text;
using UnityEditor;
using UnityEngine;

// 88일차: Tools > Project U 공통 메뉴 (저장 폴더 · 날짜 이동 · 날씨 · 생활 시작 재료)
// 기능별 테스트 메뉴는 Tools > Project U > Debug (Play Mode) > 농사·낚시·요리·가축·판매 아래에 있다.
public static class ProjectUToolsMenu
{
    private const string Root = "Tools/Project U/";
    private const string DebugRoot = Root + "Debug (Play Mode)/";

    // 생활 콘텐츠를 처음부터 이어서 해 볼 때 필요한 재료 (도구는 없을 때만)
    private static readonly (string itemId, int amount)[] StarterKit =
    {
        ("tool_hoe", 1), ("tool_watering_can", 1), ("tool_fishing_rod_basic", 1),
        ("seed_potato", 6), ("seed_strawberry", 4), ("seed_tomato", 4), ("seed_pumpkin", 2), ("seed_winter_radish", 4),
        ("resource_worm_bait", 10), ("item_animal_feed", 20),
        ("item_wood", 40), ("item_plant_fiber", 20), ("resource_stone", 15)
    };

    [MenuItem(Root + "Open Save Folder", false, 80)]
    private static void OpenSaveFolder()
    {
        System.IO.Directory.CreateDirectory(SaveFileService.SaveDirectoryPath);
        EditorUtility.RevealInFinder(SaveFileService.SaveDirectoryPath);
    }

    [MenuItem(DebugRoot + "Give Life Starter Kit (Tools + Seeds + Materials)", false, 40)]
    private static void GiveStarterKit()
    {
        PlayerInventory inventory = Object.FindFirstObjectByType<PlayerInventory>();

        if (inventory == null)
        {
            return;
        }

        StringBuilder report = new StringBuilder("[생활 시작 재료 지급]");

        foreach ((string itemId, int amount) in StarterKit)
        {
            ItemData item = FindItem(itemId);

            if (item == null)
            {
                report.Append($"\n{itemId} 없음");
                continue;
            }

            if (item.IsTool && inventory.GetItemQuantity(item) > 0)
            {
                continue;
            }

            int left = inventory.AddItem(item, amount);
            report.Append($"\n{item.DisplayName} x{amount - left}{(left > 0 ? " (가방 가득 참)" : string.Empty)}");
        }

        if (PlayerWallet.Local != null)
        {
            PlayerWallet.Local.Add(200);
            report.Append($"\n코인 +200 → {PlayerWallet.Local.Coins}");
        }

        Debug.Log(report.ToString());
    }

    [MenuItem(DebugRoot + "Advance One Day (07:00)", false, 41)]
    private static void AdvanceDay()
    {
        DayNightCycle cycle = Object.FindFirstObjectByType<DayNightCycle>();

        if (cycle == null)
        {
            return;
        }

        // 잠자기와 같은 날짜 처리(밭 성장 · 가축 생산 · 판매 상자 판매)가 이번 프레임 끝에 실행된다
        cycle.SetTime(cycle.CurrentDay + 1, 7f);
        Debug.Log($"DAY {cycle.CurrentDay} 07:00 으로 이동 (밭 성장 · 가축 생산 · 판매 상자 판매 처리)");
    }

    [MenuItem(DebugRoot + "Weather/Clear", false, 60)]
    private static void WeatherClear() => SetWeather(WeatherType.Clear);

    [MenuItem(DebugRoot + "Weather/Rain", false, 61)]
    private static void WeatherRain() => SetWeather(WeatherType.Rain);

    [MenuItem(DebugRoot + "Weather/Storm", false, 62)]
    private static void WeatherStorm() => SetWeather(WeatherType.Storm);

    [MenuItem(DebugRoot + "Give Life Starter Kit (Tools + Seeds + Materials)", true)]
    [MenuItem(DebugRoot + "Advance One Day (07:00)", true)]
    [MenuItem(DebugRoot + "Weather/Clear", true)]
    [MenuItem(DebugRoot + "Weather/Rain", true)]
    [MenuItem(DebugRoot + "Weather/Storm", true)]
    private static bool IsPlaying()
    {
        return EditorApplication.isPlaying;
    }

    private static void SetWeather(WeatherType weather)
    {
        WeatherCycle cycle = Object.FindFirstObjectByType<WeatherCycle>();

        if (cycle != null)
        {
            cycle.ForceWeather(weather);
            Debug.Log($"날씨 : {weather}");
        }
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
}
