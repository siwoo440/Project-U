using System.Text;
using UnityEditor;
using UnityEngine;

// 87일차: Play 중 판매·상점 확인용 메뉴
public static class MarketDebugMenu
{
    private const string Root = "Tools/Project U/Debug (Play Mode)/Market/";

    private static readonly (string itemId, int amount)[] Kit =
    {
        ("item_wood", 30), ("item_plant_fiber", 14), ("resource_stone", 8),
        ("food_egg", 6), ("drink_milk", 3), ("food_potato", 8), ("food_pumpkin", 2), ("resource_fish_trout", 2)
    };

    [MenuItem(Root + "Give Market Kit (Materials + Goods)", false, 0)]
    private static void GiveKit()
    {
        PlayerInventory inventory = Object.FindFirstObjectByType<PlayerInventory>();

        if (inventory == null)
        {
            return;
        }

        StringBuilder report = new StringBuilder("[판매·상점 재료 지급]");

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

    [MenuItem(Root + "Give 500 Coins", false, 1)]
    private static void GiveCoins()
    {
        PlayerWallet wallet = PlayerWallet.Local;

        if (wallet == null)
        {
            Debug.LogWarning("플레이어 지갑이 없습니다. Tools > Project U > Market > 1. Build Market Content를 실행하세요.");
            return;
        }

        wallet.Add(500);
        Debug.Log($"코인 +500 → {wallet.Coins}");
    }

    [MenuItem(Root + "Sell Shipping Bins Now", false, 20)]
    private static void SellNow()
    {
        MarketManager manager = MarketManager.Instance;

        if (manager == null)
        {
            return;
        }

        ShippingSaleReport result = manager.DebugSellNow();
        StringBuilder report = new StringBuilder($"[판매 상자 즉시 판매] {result.ItemsSold}개 +{result.Coins} 코인, 못 판 물건 {result.UnsoldItems}개");

        foreach (ShippingSaleLine line in result.Lines)
        {
            report.Append($"\n  {line.Item.DisplayName} x{line.Quantity} +{line.Coins}");
        }

        Debug.Log(report.ToString());
    }

    [MenuItem(Root + "Refresh Merchant Stock", false, 21)]
    private static void RefreshStock()
    {
        MarketManager manager = MarketManager.Instance;

        if (manager == null)
        {
            return;
        }

        manager.RefreshStock(manager.CurrentDay);
        LogMarket();
    }

    [MenuItem(Root + "Log Market", false, 41)]
    private static void LogMarket()
    {
        MarketManager manager = MarketManager.Instance;

        if (manager == null)
        {
            Debug.LogWarning("MarketManager가 없습니다.");
            return;
        }

        manager.EstimateBins(out int coins, out int sellable, out int unsellable);
        StringBuilder report = new StringBuilder($"[상점] DAY {manager.CurrentDay} {manager.CurrentSeason}, 영업 {(manager.IsShopOpen ? "중" : "종료")}, 코인 {(manager.Wallet != null ? manager.Wallet.Coins : 0)}");
        report.Append($"\n판매 상자 {MarketManager.ActiveBins.Count}개 : {sellable}개 예상 +{coins} (못 팖 {unsellable}), 누적 수입 {manager.TotalCoinsEarned} / {manager.TotalItemsSold}개");

        foreach (ShopOffer offer in manager.Offers)
        {
            report.Append($"\n  {offer.Item.DisplayName} {offer.Price} 코인, 남은 {offer.Remaining}{(offer.IsSpecial ? " (가끔)" : string.Empty)}");
        }

        Debug.Log(report.ToString());
    }

    [MenuItem(Root + "Give Market Kit (Materials + Goods)", true)]
    [MenuItem(Root + "Give 500 Coins", true)]
    [MenuItem(Root + "Sell Shipping Bins Now", true)]
    [MenuItem(Root + "Refresh Merchant Stock", true)]
    [MenuItem(Root + "Log Market", true)]
    private static bool IsPlaying()
    {
        return EditorApplication.isPlaying;
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
