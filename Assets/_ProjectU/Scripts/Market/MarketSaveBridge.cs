using System; // 문자열 비교 기능
using System.Collections.Generic; // 목록 기능

// 87일차: 코인·오늘 상인 재고·판매 기록을 저장 파일과 연결 (판매 상자 내용은 보관함 저장을 그대로 사용)
public static class MarketSaveBridge
{
    public static bool TryCapture(SaveGameData saveData, out string errorMessage) // 상점 상태 수집
    {
        if (saveData == null) // 저장 데이터 확인
        {
            errorMessage = "상점 저장 데이터가 비어 있습니다."; // 오류
            return false; // 실패
        }

        MarketManager manager = MarketManager.Instance; // 관리자

        if (manager == null) // 상점 기능이 없는 Scene
        {
            saveData.hasMarketData = false; // 데이터 없음
            errorMessage = string.Empty; // 오류 없음
            return true; // 생략
        }

        saveData.hasMarketData = true; // 데이터 존재
        saveData.market = manager.CaptureSaveData(); // 저장
        errorMessage = string.Empty; // 오류 없음
        return true; // 성공
    }

    public static bool TryRestore(SaveGameData saveData, out string errorMessage) // 상점 상태 복원
    {
        if (saveData == null) // 저장 데이터 확인
        {
            errorMessage = "상점 저장 데이터가 비어 있습니다."; // 오류
            return false; // 실패
        }

        MarketManager manager = MarketManager.Instance; // 관리자

        if (manager == null) // 상점 기능이 없는 Scene
        {
            errorMessage = string.Empty; // 오류 없음
            return true; // 생략
        }

        if (!TryValidate(saveData, out errorMessage)) // 구조 검사
        {
            return false; // 실패
        }

        int today = saveData.time != null ? saveData.time.currentDay : manager.CurrentDay; // 불러올 날짜 (시간 적용 전이므로 저장 값 사용)
        PlayerWallet wallet = manager.Wallet; // 지갑

        if (saveData.hasMarketData) // 87일차 이후 저장 파일
        {
            if (wallet != null) // 지갑 확인
            {
                wallet.SetForLoad(saveData.market.coins); // 코인
            }

            manager.ApplySaveData(saveData.market, today); // 재고·기록
        }
        else // 이전 저장 파일 : 코인 0
        {
            if (wallet != null) // 지갑 확인
            {
                wallet.SetForLoad(0); // 코인
            }

            manager.ResetForLoad(today); // 오늘부터
        }

        errorMessage = string.Empty; // 오류 없음
        return true; // 성공
    }

    public static bool TryValidate(SaveGameData saveData, out string errorMessage) // 저장 구조 검사
    {
        if (!saveData.hasMarketData) // 이전 저장 파일
        {
            errorMessage = string.Empty; // 오류 없음
            return true; // 허용
        }

        MarketSaveData market = saveData.market; // 데이터

        if (market == null || market.stock == null) // 목록 확인
        {
            errorMessage = "상점 저장 데이터가 누락되었습니다."; // 오류
            return false; // 실패
        }

        if (market.coins < 0 || market.coins > PlayerWallet.MaxCoins) // 코인 범위
        {
            errorMessage = $"저장된 코인이 범위를 벗어났습니다: {market.coins}"; // 오류
            return false; // 실패
        }

        if (market.totalCoinsEarned < 0 || market.totalItemsSold < 0 || market.lastProcessedDay < 0 || market.stockDay < 0) // 음수 확인
        {
            errorMessage = "상점 기록 값이 잘못되었습니다."; // 오류
            return false; // 실패
        }

        HashSet<string> usedIds = new HashSet<string>(StringComparer.Ordinal); // 중복 검사

        foreach (MarketStockSaveData entry in market.stock) // 재고 순회
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.itemId) || entry.remaining < 0) // 항목 확인
            {
                errorMessage = "상인 재고 저장 항목이 잘못되었습니다."; // 오류
                return false; // 실패
            }

            if (!usedIds.Add(entry.itemId)) // 중복 확인
            {
                errorMessage = $"상인 재고 아이템이 중복되었습니다: {entry.itemId}"; // 오류
                return false; // 실패
            }
        }

        errorMessage = string.Empty; // 오류 없음
        return true; // 성공
    }
}
