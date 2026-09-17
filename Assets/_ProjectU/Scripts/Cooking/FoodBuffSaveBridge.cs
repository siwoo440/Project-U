using UnityEngine; // Unity 기본 기능

// 85일차: 음식 보조 효과를 저장 파일과 연결
public static class FoodBuffSaveBridge
{
    public static bool TryCapture(SaveGameData saveData, out string errorMessage) // 음식 효과 수집
    {
        if (saveData == null) // 저장 데이터 확인
        {
            errorMessage = "음식 효과 저장 데이터가 비어 있습니다."; // 오류 저장
            return false; // 수집 실패
        }

        FoodBuffController controller = FindController(); // 효과 관리자 조회

        if (controller == null) // 요리 기능이 없는 Scene 확인
        {
            saveData.hasFoodBuffData = false; // 효과 없음
            errorMessage = string.Empty; // 오류 없음
            return true; // 수집 생략
        }

        saveData.hasFoodBuffData = true; // 효과 존재 표시
        saveData.foodBuffs = controller.CaptureSaveData(); // 효과 저장
        errorMessage = string.Empty; // 오류 없음
        return true; // 수집 성공
    }

    public static bool TryRestore(SaveGameData saveData, out string errorMessage) // 음식 효과 복원
    {
        if (saveData == null) // 저장 데이터 확인
        {
            errorMessage = "음식 효과 저장 데이터가 비어 있습니다."; // 오류 저장
            return false; // 복원 실패
        }

        FoodBuffController controller = FindController(); // 효과 관리자 조회

        if (controller == null) // 요리 기능이 없는 Scene 확인
        {
            errorMessage = string.Empty; // 오류 없음
            return true; // 복원 생략
        }

        if (!saveData.hasFoodBuffData) // 85일차 이전 저장 파일은 효과 없음
        {
            controller.RestoreSaveData(null); // 빈 효과 적용
            errorMessage = string.Empty; // 오류 없음
            return true; // 복원 성공
        }

        if (saveData.foodBuffs == null) // 목록 확인
        {
            errorMessage = "음식 효과 저장 목록이 누락되었습니다."; // 오류 저장
            return false; // 복원 실패
        }

        controller.RestoreSaveData(saveData.foodBuffs); // 효과 적용
        errorMessage = string.Empty; // 오류 없음
        return true; // 복원 성공
    }

    public static bool TryValidate(SaveGameData saveData, out string errorMessage) // 저장 데이터 검사
    {
        if (!saveData.hasFoodBuffData) // 이전 저장 파일 허용
        {
            errorMessage = string.Empty; // 오류 없음
            return true; // 검사 성공
        }

        if (saveData.foodBuffs == null) // 목록 확인
        {
            errorMessage = "음식 효과 저장 목록이 누락되었습니다."; // 오류 저장
            return false; // 검사 실패
        }

        bool[] used = new bool[System.Enum.GetValues(typeof(FoodBuffType)).Length + 1]; // 중복 확인

        for (int index = 0; index < saveData.foodBuffs.Count; index++) // 순회
        {
            FoodBuffSaveData buff = saveData.foodBuffs[index]; // 항목

            if (buff == null || !System.Enum.IsDefined(typeof(FoodBuffType), buff.buffType) || buff.buffType == (int)FoodBuffType.None) // 종류 확인
            {
                errorMessage = $"음식 효과 항목 {index}의 종류가 잘못되었습니다."; // 오류 저장
                return false; // 검사 실패
            }

            if (used[buff.buffType]) // 중복 확인
            {
                errorMessage = $"음식 효과가 중복되었습니다: {(FoodBuffType)buff.buffType}"; // 오류 저장
                return false; // 검사 실패
            }

            used[buff.buffType] = true; // 기록
            bool invalid = buff.strength < 0f || buff.remainingSeconds < 0f || buff.durationSeconds < 0f
                || float.IsNaN(buff.strength) || float.IsNaN(buff.remainingSeconds) || float.IsInfinity(buff.remainingSeconds); // 수치 확인

            if (invalid) // 수치 오류 확인
            {
                errorMessage = $"음식 효과 수치가 잘못되었습니다: {(FoodBuffType)buff.buffType}"; // 오류 저장
                return false; // 검사 실패
            }
        }

        errorMessage = string.Empty; // 오류 없음
        return true; // 검사 성공
    }

    private static FoodBuffController FindController() // 현재 Scene 효과 관리자 검색
    {
        return FoodBuffController.Local != null ? FoodBuffController.Local : Object.FindFirstObjectByType<FoodBuffController>(); // 결과 반환
    }
}
