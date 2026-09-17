using System.Text;
using UnityEditor;
using UnityEngine;

// 86일차: Play 중 가축 확인용 메뉴
public static class LivestockDebugMenu
{
    private const string Root = "Tools/Project U/Livestock/Debug (Play Mode)/";

    private static readonly (string itemId, int amount)[] Kit =
    {
        ("item_animal_feed", 30), ("item_wood", 40), ("item_plant_fiber", 10), ("resource_stone", 10)
    };

    [MenuItem(Root + "Give Livestock Kit (Feed + Pen Materials)", false, 0)]
    private static void GiveKit()
    {
        PlayerInventory inventory = Object.FindFirstObjectByType<PlayerInventory>();

        if (inventory == null)
        {
            return;
        }

        StringBuilder report = new StringBuilder("[가축 재료 지급]");

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

    [MenuItem(Root + "Add Animal (Nearest Pen)", false, 20)]
    private static void AddAnimal()
    {
        AnimalPen pen = FindNearestPen();

        if (pen != null)
        {
            pen.DebugAddAnimal();
            Debug.Log($"{pen.PenDisplayName} 동물 {pen.Animals.Count}/{pen.Capacity}");
        }
    }

    [MenuItem(Root + "Feed All (Nearest Pen, Free)", false, 21)]
    private static void FeedFree()
    {
        AnimalPen pen = FindNearestPen();

        if (pen == null)
        {
            return;
        }

        foreach (PenAnimal animal in pen.Animals)
        {
            animal.MarkFed();
        }

        Debug.Log($"{pen.PenDisplayName} 모든 동물 먹이 처리");
    }

    [MenuItem(Root + "Set Mood 100 (Nearest Pen)", false, 22)]
    private static void MoodHigh() => SetMood(100);

    [MenuItem(Root + "Set Mood 10 (Nearest Pen)", false, 23)]
    private static void MoodLow() => SetMood(10);

    [MenuItem(Root + "Advance One Day", false, 40)]
    private static void AdvanceDay()
    {
        DayNightCycle cycle = Object.FindFirstObjectByType<DayNightCycle>();

        if (cycle == null)
        {
            return;
        }

        cycle.SetTime(cycle.CurrentDay + 1, 7f);
        Debug.Log($"DAY {cycle.CurrentDay} 07:00 으로 이동");
    }

    [MenuItem(Root + "Log Nearest Pen", false, 41)]
    private static void LogNearest()
    {
        AnimalPen pen = FindNearestPen();

        if (pen == null)
        {
            return;
        }

        StringBuilder report = new StringBuilder($"[{pen.PenDisplayName}] {pen.Animals.Count}/{pen.Capacity}, 처리 날짜 {pen.LastProcessedDay}");

        foreach (PenAnimal animal in pen.Animals)
        {
            report.Append($"\n  {animal.DisplayName} 기분 {animal.Mood} ({animal.MoodLevel}) 먹음 {animal.FedToday} 쓰다듬 {animal.PettedToday} 생산 {animal.ProductReady} 경과 {animal.DaysSinceProduct}");
        }

        Debug.Log(report.ToString());
    }

    [MenuItem(Root + "Give Livestock Kit (Feed + Pen Materials)", true)]
    [MenuItem(Root + "Add Animal (Nearest Pen)", true)]
    [MenuItem(Root + "Feed All (Nearest Pen, Free)", true)]
    [MenuItem(Root + "Set Mood 100 (Nearest Pen)", true)]
    [MenuItem(Root + "Set Mood 10 (Nearest Pen)", true)]
    [MenuItem(Root + "Advance One Day", true)]
    [MenuItem(Root + "Log Nearest Pen", true)]
    private static bool IsPlaying()
    {
        return EditorApplication.isPlaying;
    }

    private static void SetMood(int mood)
    {
        AnimalPen pen = FindNearestPen();

        if (pen == null)
        {
            return;
        }

        foreach (PenAnimal animal in pen.Animals)
        {
            animal.DebugSetMood(mood);
        }

        Debug.Log($"{pen.PenDisplayName} 기분 {mood}");
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

    private static AnimalPen FindNearestPen()
    {
        PlayerInventory player = Object.FindFirstObjectByType<PlayerInventory>();
        AnimalPen best = null;
        float bestDistance = float.MaxValue;

        foreach (AnimalPen pen in LivestockManager.ActivePens)
        {
            float distance = player != null ? (pen.transform.position - player.transform.position).sqrMagnitude : 0f;

            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = pen;
            }
        }

        if (best == null)
        {
            Debug.LogWarning("우리가 없습니다. B 키 건축으로 닭장이나 외양간을 설치하세요.");
        }

        return best;
    }
}
