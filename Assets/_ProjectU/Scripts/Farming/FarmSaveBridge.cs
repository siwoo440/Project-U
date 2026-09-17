using System; // 문자열 비교 기능
using System.Collections.Generic; // 목록과 Dictionary 기능
using UnityEngine; // Unity 기본 기능

// 80일차: 밭 칸별 작물·물 상태와 물뿌리개 남은 물을 저장 파일과 연결
public static class FarmSaveBridge
{
    public static bool TryCapture(SaveGameData saveData, out string errorMessage) // 밭 상태 수집
    {
        if (saveData == null) // 저장 데이터 확인
        {
            errorMessage = "밭 저장 데이터가 비어 있습니다."; // 오류 저장
            return false; // 수집 실패
        }

        FarmManager manager = FarmManager.Instance; // 밭 관리자 조회

        if (manager == null) // 농사 기능이 없는 Scene 확인
        {
            saveData.hasFarmData = false; // 밭 데이터 없음
            errorMessage = string.Empty; // 오류 없음
            return true; // 수집 생략
        }

        IReadOnlyList<FarmPlot> plots = FarmManager.ActivePlots; // 활성 밭 목록
        List<FarmPlotSaveData> savedPlots = new List<FarmPlotSaveData>(plots.Count); // 저장 목록
        HashSet<string> usedIds = new HashSet<string>(StringComparer.Ordinal); // 중복 검사 목록

        for (int index = 0; index < plots.Count; index++) // 전체 밭 순회
        {
            FarmPlot plot = plots[index]; // 현재 밭
            string structureId = plot.StructureId; // 건축물 ID

            if (string.IsNullOrWhiteSpace(structureId)) // ID 확인
            {
                errorMessage = $"{plot.name}의 건축물 ID가 비어 있습니다."; // 오류 저장
                return false; // 수집 실패
            }

            if (!usedIds.Add(structureId)) // 중복 확인
            {
                errorMessage = $"밭 건축물 ID가 중복되었습니다: {structureId}"; // 오류 저장
                return false; // 수집 실패
            }

            savedPlots.Add(plot.CaptureSaveData()); // 저장 항목 추가
        }

        saveData.hasFarmData = true; // 밭 데이터 존재 표시
        saveData.farm = new FarmSaveData
        {
            wateringCanWater = manager.WateringCanWater, // 남은 물
            plots = savedPlots // 칸별 상태
        };
        errorMessage = string.Empty; // 오류 없음
        return true; // 수집 성공
    }

    public static bool TryRestore(SaveGameData saveData, out string errorMessage) // 밭 상태 복원 (건축물 복원 뒤 호출)
    {
        if (saveData == null) // 저장 데이터 확인
        {
            errorMessage = "밭 저장 데이터가 비어 있습니다."; // 오류 저장
            return false; // 복원 실패
        }

        FarmManager manager = FarmManager.Instance; // 밭 관리자 조회

        // 80일차 이전 저장 파일이거나 농사 기능이 없는 Scene은 현재 상태 유지
        if (!saveData.hasFarmData || manager == null)
        {
            errorMessage = string.Empty; // 오류 없음
            return true; // 복원 생략
        }

        if (saveData.farm == null || saveData.farm.plots == null) // 목록 확인
        {
            errorMessage = "밭 저장 목록이 누락되었습니다."; // 오류 저장
            return false; // 복원 실패
        }

        Dictionary<string, (FarmPlotSaveData data, CropData crop)> savedById =
            new Dictionary<string, (FarmPlotSaveData, CropData)>(StringComparer.Ordinal); // ID별 저장 항목

        // 적용 전에 전체 항목과 작물 ID를 먼저 확인한다
        for (int index = 0; index < saveData.farm.plots.Count; index++) // 저장 항목 순회
        {
            FarmPlotSaveData data = saveData.farm.plots[index]; // 현재 항목
            CropData crop = null; // 작물 데이터

            if (!string.IsNullOrEmpty(data.cropId) && !manager.TryGetCrop(data.cropId, out crop)) // 작물 검색
            {
                errorMessage = $"등록되지 않은 작물 ID입니다: {data.cropId}"; // 오류 저장
                return false; // 복원 실패
            }

            savedById[data.structureId] = (data, crop); // 항목 등록
        }

        IReadOnlyList<FarmPlot> plots = FarmManager.ActivePlots; // 복원된 밭 목록

        for (int index = 0; index < plots.Count; index++) // 전체 밭 순회
        {
            FarmPlot plot = plots[index]; // 현재 밭

            if (savedById.TryGetValue(plot.StructureId, out (FarmPlotSaveData data, CropData crop) saved)) // 저장 항목 확인
            {
                plot.ApplySaveData(saved.data, saved.crop); // 저장 상태 적용
            }
            else // 저장 이후 새로 생긴 밭
            {
                plot.ResetForLoad(); // 빈 밭 적용
            }
        }

        manager.SetWaterForLoad(saveData.farm.wateringCanWater); // 남은 물 적용
        errorMessage = string.Empty; // 오류 없음
        return true; // 복원 성공
    }
}
