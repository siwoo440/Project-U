using System; // 문자열 비교 기능
using System.Collections.Generic; // 목록 기능

// 92일차: NPC 상점의 오늘 재고 · 매입 기록 · 거래 합계를 저장 파일과 연결 (코인은 87일차 상점 저장을 그대로 사용)
public static class NpcShopSaveBridge
{
    public const int MaxDailyAmount = 9999; // 저장 값 검사 상한 (재고 · 하루 매입 수)

    public static bool TryCapture(SaveGameData saveData, out string errorMessage) // 상점 상태 수집
    {
        if (saveData == null)
        {
            errorMessage = "NPC 상점 저장 데이터가 비어 있습니다.";
            return false;
        }

        NpcShopManager manager = NpcShopManager.Instance;

        if (manager == null) // NPC 상점이 없는 Scene
        {
            saveData.hasNpcShopData = false;
            errorMessage = string.Empty;
            return true;
        }

        saveData.hasNpcShopData = true;
        saveData.npcShops = manager.CaptureSaveData();
        errorMessage = string.Empty;
        return true;
    }

    public static bool TryRestore(SaveGameData saveData, out string errorMessage) // 상점 상태 복원
    {
        if (saveData == null)
        {
            errorMessage = "NPC 상점 저장 데이터가 비어 있습니다.";
            return false;
        }

        NpcShopManager manager = NpcShopManager.Instance;

        if (manager == null)
        {
            errorMessage = string.Empty;
            return true;
        }

        if (!TryValidate(saveData, out errorMessage))
        {
            return false;
        }

        int today = saveData.time != null ? saveData.time.currentDay : manager.CurrentDay; // 시간 적용 전이므로 저장 값 사용

        if (saveData.hasNpcShopData)
        {
            manager.ApplySaveData(saveData.npcShops, today);
        }
        else
        {
            manager.ResetForLoad(today); // 92일차 이전 저장 파일 : 그날 재고로 시작
        }

        errorMessage = string.Empty;
        return true;
    }

    public static bool TryValidate(SaveGameData saveData, out string errorMessage) // 저장 구조 검사
    {
        if (!saveData.hasNpcShopData)
        {
            errorMessage = string.Empty;
            return true;
        }

        if (saveData.npcShops == null || saveData.npcShops.shops == null)
        {
            errorMessage = "NPC 상점 저장 데이터가 누락되었습니다.";
            return false;
        }

        HashSet<string> shopIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (NpcShopStateSaveData shop in saveData.npcShops.shops)
        {
            if (shop == null || string.IsNullOrWhiteSpace(shop.shopId) || !shop.shopId.StartsWith("shop_"))
            {
                errorMessage = "NPC 상점 저장 항목의 상점 ID가 잘못되었습니다.";
                return false;
            }

            if (!shopIds.Add(shop.shopId))
            {
                errorMessage = $"NPC 상점 저장 항목이 중복되었습니다: {shop.shopId}";
                return false;
            }

            if (shop.stock == null || shop.stockDay < 0 || shop.buybackDay < -1 || shop.boughtFromPlayer < 0 || shop.boughtFromPlayer > MaxDailyAmount
                || shop.totalCoinsSpent < 0 || shop.totalCoinsEarned < 0)
            {
                errorMessage = $"NPC 상점 기록 값이 잘못되었습니다: {shop.shopId}";
                return false;
            }

            HashSet<string> itemIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (NpcShopStockSaveData stock in shop.stock)
            {
                if (stock == null || string.IsNullOrWhiteSpace(stock.itemId) || stock.remaining < 0 || stock.remaining > MaxDailyAmount)
                {
                    errorMessage = $"NPC 상점 재고 저장 항목이 잘못되었습니다: {shop.shopId}";
                    return false;
                }

                if (!itemIds.Add(stock.itemId))
                {
                    errorMessage = $"NPC 상점 재고 아이템이 중복되었습니다: {shop.shopId} {stock.itemId}";
                    return false;
                }
            }
        }

        errorMessage = string.Empty;
        return true;
    }
}
