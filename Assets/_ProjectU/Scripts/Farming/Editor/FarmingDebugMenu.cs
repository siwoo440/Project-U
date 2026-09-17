using UnityEditor;
using UnityEngine;

// 81일차: Play 중 작물 성장을 빠르게 확인하기 위한 테스트 메뉴
public static class FarmingDebugMenu
{
    private const string MenuRoot = "Tools/Project U/Farming/Debug (Play Mode)/";

    [MenuItem(MenuRoot + "Water All Plots", false, 100)]
    private static void WaterAllPlots()
    {
        FarmManager manager = FarmManager.Instance;
        int day = manager.CurrentDay;

        foreach (FarmPlot plot in FarmManager.ActivePlots)
        {
            plot.ReceiveWater(day);
        }

        Debug.Log($"[Farming Debug] 밭 {FarmManager.ActivePlots.Count}칸에 물을 주었습니다. (DAY {day})");
    }

    [MenuItem(MenuRoot + "Advance To Next Morning", false, 101)]
    private static void AdvanceToNextMorning()
    {
        DayNightCycle cycle = Object.FindFirstObjectByType<DayNightCycle>();
        int nextDay = cycle.CurrentDay + 1;
        cycle.SetTime(nextDay, 6f);
        Debug.Log($"[Farming Debug] DAY {nextDay} 06:00으로 이동했습니다. 날짜 처리는 이번 프레임 끝에 실행됩니다.");
    }

    [MenuItem(MenuRoot + "Water All And Advance Day", false, 102)]
    private static void WaterAndAdvance()
    {
        WaterAllPlots();
        AdvanceToNextMorning();
    }

    [MenuItem(MenuRoot + "Water All Plots", true)]
    [MenuItem(MenuRoot + "Advance To Next Morning", true)]
    [MenuItem(MenuRoot + "Water All And Advance Day", true)]
    private static bool CanUseDebugMenu()
    {
        return EditorApplication.isPlaying && FarmManager.Instance != null && Object.FindFirstObjectByType<DayNightCycle>() != null;
    }
}
