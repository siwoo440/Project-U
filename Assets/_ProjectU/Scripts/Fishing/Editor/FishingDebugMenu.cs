using UnityEditor;
using UnityEngine;

// 83일차: Play 중 물고기 출현·미니게임·보상을 빠르게 확인하기 위한 테스트 메뉴
public static class FishingDebugMenu
{
    private const string MenuRoot = "Tools/Project U/Fishing/Debug (Play Mode)/";

    [MenuItem(MenuRoot + "Bite Now", false, 100)]
    private static void BiteNow()
    {
        FishingController.Local.DebugBiteNow();
        Debug.Log($"[Fishing Debug] 대기 중이면 바로 입질합니다. (현재 상태 {FishingController.Local.State})");
    }

    [MenuItem(MenuRoot + "Win Minigame Now", false, 101)]
    private static void WinMinigame()
    {
        FishingMinigameController minigame = FishingController.Local.GetComponent<FishingMinigameController>();

        if (minigame != null)
        {
            minigame.DebugWin();
        }
    }

    [MenuItem(MenuRoot + "Log Current Fish Chances", false, 102)]
    private static void LogChances()
    {
        FishingController controller = FishingController.Local;
        FishingConditions conditions = controller.GetCurrentConditions();
        System.Text.StringBuilder report = new System.Text.StringBuilder($"[Fishing Debug] 현재 조건 : {conditions}\n");
        float rareMultiplier = controller.Rules != null ? controller.Rules.BaitRareWeightMultiplier : 1f;

        foreach (FishData fish in controller.GetFishList())
        {
            float chance = FishSelector.GetChance(controller.GetFishList(), fish, conditions, rareMultiplier);
            report.AppendLine($"{fish.DisplayName} ({FishRarityUtility.GetLabel(fish.Rarity)}) : {chance * 100f:0.0}%");
        }

        Debug.Log(report.ToString());
    }

    [MenuItem(MenuRoot + "Force Next Fish/Crucian Carp", false, 120)]
    private static void ForceCrucian() => Force("fish_crucian");

    [MenuItem(MenuRoot + "Force Next Fish/Trout", false, 121)]
    private static void ForceTrout() => Force("fish_trout");

    [MenuItem(MenuRoot + "Force Next Fish/Catfish", false, 122)]
    private static void ForceCatfish() => Force("fish_catfish");

    [MenuItem(MenuRoot + "Force Next Fish/Smelt", false, 123)]
    private static void ForceSmelt() => Force("fish_smelt");

    [MenuItem(MenuRoot + "Force Next Fish/Golden Carp", false, 124)]
    private static void ForceGoldenCarp() => Force("fish_golden_carp");

    [MenuItem(MenuRoot + "Force Next Fish/Clear (Use Conditions)", false, 140)]
    private static void ClearForced() => Force(string.Empty);

    [MenuItem(MenuRoot + "Weather/Clear", false, 160)]
    private static void WeatherClear() => SetWeather(WeatherType.Clear);

    [MenuItem(MenuRoot + "Weather/Rain", false, 161)]
    private static void WeatherRain() => SetWeather(WeatherType.Rain);

    [MenuItem(MenuRoot + "Weather/Storm", false, 162)]
    private static void WeatherStorm() => SetWeather(WeatherType.Storm);

    private static void Force(string fishId)
    {
        FishingController.DebugForcedFishId = fishId;
        Debug.Log(string.IsNullOrEmpty(fishId)
            ? "[Fishing Debug] 강제 물고기를 해제했습니다. 현재 조건으로 물고기를 뽑습니다."
            : $"[Fishing Debug] 다음 입질부터 {fishId}가 뭅니다.");
    }

    private static void SetWeather(WeatherType weather)
    {
        Object.FindFirstObjectByType<WeatherCycle>().ForceWeather(weather);
        Debug.Log($"[Fishing Debug] 날씨를 {weather}(으)로 바꿨습니다.");
    }

    [MenuItem(MenuRoot + "Bite Now", true)]
    [MenuItem(MenuRoot + "Win Minigame Now", true)]
    [MenuItem(MenuRoot + "Log Current Fish Chances", true)]
    [MenuItem(MenuRoot + "Force Next Fish/Crucian Carp", true)]
    [MenuItem(MenuRoot + "Force Next Fish/Trout", true)]
    [MenuItem(MenuRoot + "Force Next Fish/Catfish", true)]
    [MenuItem(MenuRoot + "Force Next Fish/Smelt", true)]
    [MenuItem(MenuRoot + "Force Next Fish/Golden Carp", true)]
    [MenuItem(MenuRoot + "Force Next Fish/Clear (Use Conditions)", true)]
    private static bool CanUseFishingMenu()
    {
        return EditorApplication.isPlaying && FishingController.Local != null;
    }

    [MenuItem(MenuRoot + "Weather/Clear", true)]
    [MenuItem(MenuRoot + "Weather/Rain", true)]
    [MenuItem(MenuRoot + "Weather/Storm", true)]
    private static bool CanUseWeatherMenu()
    {
        return EditorApplication.isPlaying && Object.FindFirstObjectByType<WeatherCycle>() != null;
    }
}
