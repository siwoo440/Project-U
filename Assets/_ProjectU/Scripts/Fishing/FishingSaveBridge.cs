using UnityEngine; // Unity 기본 기능

// 83일차: 잡은 물고기 기록을 저장 파일과 연결
public static class FishingSaveBridge
{
    public static bool TryCapture(SaveGameData saveData, out string errorMessage) // 낚시 기록 수집
    {
        if (saveData == null) // 저장 데이터 확인
        {
            errorMessage = "낚시 저장 데이터가 비어 있습니다."; // 오류 저장
            return false; // 수집 실패
        }

        FishingJournal journal = FindJournal(); // 기록 조회

        if (journal == null) // 낚시 기능이 없는 Scene 확인
        {
            saveData.hasFishingData = false; // 기록 없음
            errorMessage = string.Empty; // 오류 없음
            return true; // 수집 생략
        }

        saveData.hasFishingData = true; // 기록 존재 표시
        saveData.fishing = new FishingSaveData { catches = journal.CaptureSaveData() }; // 기록 저장
        errorMessage = string.Empty; // 오류 없음
        return true; // 수집 성공
    }

    public static bool TryRestore(SaveGameData saveData, out string errorMessage) // 낚시 기록 복원
    {
        if (saveData == null) // 저장 데이터 확인
        {
            errorMessage = "낚시 저장 데이터가 비어 있습니다."; // 오류 저장
            return false; // 복원 실패
        }

        FishingJournal journal = FindJournal(); // 기록 조회

        if (journal == null) // 낚시 기능이 없는 Scene 확인
        {
            errorMessage = string.Empty; // 오류 없음
            return true; // 복원 생략
        }

        if (!saveData.hasFishingData) // 83일차 이전 저장 파일은 잡은 기록이 없는 상태로 적용
        {
            journal.RestoreSaveData(null); // 빈 기록 적용
            errorMessage = string.Empty; // 오류 없음
            return true; // 복원 성공
        }

        if (saveData.fishing == null || saveData.fishing.catches == null) // 목록 확인
        {
            errorMessage = "낚시 기록 저장 목록이 누락되었습니다."; // 오류 저장
            return false; // 복원 실패
        }

        journal.RestoreSaveData(saveData.fishing.catches); // 기록 적용
        errorMessage = string.Empty; // 오류 없음
        return true; // 복원 성공
    }

    private static FishingJournal FindJournal() // 현재 Scene 기록 검색
    {
        return FishingJournal.Local != null ? FishingJournal.Local : Object.FindFirstObjectByType<FishingJournal>(); // 기록 반환
    }
}
