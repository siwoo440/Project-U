using System; // 문자열 비교 기능
using System.Collections.Generic; // 목록 기능

// 86일차: 우리별 가축 상태를 저장 파일과 연결 (건축물 복원 뒤 호출)
public static class LivestockSaveBridge
{
    public static bool TryCapture(SaveGameData saveData, out string errorMessage) // 가축 상태 수집
    {
        if (saveData == null) // 저장 데이터 확인
        {
            errorMessage = "가축 저장 데이터가 비어 있습니다."; // 오류
            return false; // 실패
        }

        if (LivestockManager.Instance == null) // 가축 기능이 없는 Scene
        {
            saveData.hasLivestockData = false; // 데이터 없음
            errorMessage = string.Empty; // 오류 없음
            return true; // 생략
        }

        LivestockSaveData livestock = new LivestockSaveData(); // 저장 묶음
        HashSet<string> usedIds = new HashSet<string>(StringComparer.Ordinal); // 중복 검사

        foreach (AnimalPen pen in LivestockManager.ActivePens) // 우리 순회
        {
            string structureId = pen.StructureId; // 건축물 ID

            if (string.IsNullOrWhiteSpace(structureId)) // ID 확인
            {
                errorMessage = $"{pen.name}의 건축물 ID가 비어 있습니다."; // 오류
                return false; // 실패
            }

            if (!usedIds.Add(structureId)) // 중복 확인
            {
                errorMessage = $"우리 건축물 ID가 중복되었습니다: {structureId}"; // 오류
                return false; // 실패
            }

            livestock.pens.Add(pen.CaptureSaveData()); // 추가
        }

        saveData.hasLivestockData = true; // 데이터 존재
        saveData.livestock = livestock; // 저장
        errorMessage = string.Empty; // 오류 없음
        return true; // 성공
    }

    public static bool TryRestore(SaveGameData saveData, out string errorMessage) // 가축 상태 복원
    {
        if (saveData == null) // 저장 데이터 확인
        {
            errorMessage = "가축 저장 데이터가 비어 있습니다."; // 오류
            return false; // 실패
        }

        LivestockManager manager = LivestockManager.Instance; // 관리자

        if (manager == null) // 가축 기능이 없는 Scene
        {
            errorMessage = string.Empty; // 오류 없음
            return true; // 생략
        }

        Dictionary<string, AnimalPenSaveData> savedById = new Dictionary<string, AnimalPenSaveData>(StringComparer.Ordinal); // ID별 저장

        if (saveData.hasLivestockData) // 86일차 이후 저장 파일
        {
            if (!TryValidate(saveData, out errorMessage)) // 구조 검사
            {
                return false; // 실패
            }

            foreach (AnimalPenSaveData pen in saveData.livestock.pens) // 우리 순회
            {
                foreach (PenAnimalSaveData animal in pen.animals) // 동물 순회
                {
                    if (!manager.TryGetAnimal(animal.animalId, out _)) // 종류 확인
                    {
                        errorMessage = $"등록되지 않은 동물 ID입니다: {animal.animalId}"; // 오류
                        return false; // 실패
                    }
                }

                savedById[pen.structureId] = pen; // 등록
            }
        }

        int today = saveData.time != null ? saveData.time.currentDay : manager.CurrentDay; // 불러올 날짜 (시간 적용 전이므로 저장 값 사용)

        foreach (AnimalPen pen in LivestockManager.ActivePens) // 복원된 우리 순회
        {
            if (savedById.TryGetValue(pen.StructureId, out AnimalPenSaveData data)) // 저장 확인
            {
                pen.ApplySaveData(data, manager); // 적용
            }
            else // 저장 이후 새로 지은 우리 또는 이전 저장 파일
            {
                pen.ResetForLoad(today); // 빈 우리
            }
        }

        manager.SyncDayForLoad(today); // 불러온 날짜 기록
        errorMessage = string.Empty; // 오류 없음
        return true; // 성공
    }

    public static bool TryValidate(SaveGameData saveData, out string errorMessage) // 저장 구조 검사
    {
        if (!saveData.hasLivestockData) // 이전 저장 파일
        {
            errorMessage = string.Empty; // 오류 없음
            return true; // 허용
        }

        if (saveData.livestock == null || saveData.livestock.pens == null) // 목록 확인
        {
            errorMessage = "가축 저장 목록이 누락되었습니다."; // 오류
            return false; // 실패
        }

        HashSet<string> usedIds = new HashSet<string>(StringComparer.Ordinal); // 중복 검사

        foreach (AnimalPenSaveData pen in saveData.livestock.pens) // 우리 순회
        {
            if (pen == null || string.IsNullOrWhiteSpace(pen.structureId) || pen.animals == null) // 항목 확인
            {
                errorMessage = "비어 있는 우리 저장 항목이 있습니다."; // 오류
                return false; // 실패
            }

            if (!usedIds.Add(pen.structureId)) // 중복 확인
            {
                errorMessage = $"우리 저장 ID가 중복되었습니다: {pen.structureId}"; // 오류
                return false; // 실패
            }

            if (pen.animals.Count > 6) // 마리 수 확인
            {
                errorMessage = $"우리 동물 수가 너무 많습니다: {pen.structureId} ({pen.animals.Count})"; // 오류
                return false; // 실패
            }

            HashSet<int> numbers = new HashSet<int>(); // 번호 중복 검사

            foreach (PenAnimalSaveData animal in pen.animals) // 동물 순회
            {
                bool invalid = animal == null
                    || string.IsNullOrWhiteSpace(animal.animalId)
                    || animal.mood < 0 || animal.mood > PenAnimal.MaxMood
                    || animal.productReady < 0
                    || animal.daysSinceProduct < 0
                    || animal.nameNumber < 1
                    || !numbers.Add(animal.nameNumber); // 값 확인

                if (invalid) // 오류
                {
                    errorMessage = $"우리 동물 저장 값이 잘못되었습니다: {pen.structureId}"; // 오류
                    return false; // 실패
                }
            }
        }

        errorMessage = string.Empty; // 오류 없음
        return true; // 성공
    }
}
