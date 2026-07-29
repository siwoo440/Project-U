public static class SaveDataValidator // 저장 데이터 유효성 검사
{
    public static bool TryValidate(SaveGameData saveData, out string errorMessage) // 전체 저장 데이터 검사
    {
        if (saveData == null) // 저장 데이터 존재 확인
        {
            errorMessage = "저장 데이터가 비어 있습니다."; // 오류 원인 저장
            return false; // 검사 실패
        }

        SaveVersionStatus versionStatus = SaveVersionPolicy.GetStatus(saveData.saveVersion); // 버전 상태 확인

        if (versionStatus == SaveVersionStatus.Invalid) // 잘못된 버전 확인
        {
            errorMessage = "저장 데이터 버전이 잘못되었습니다."; // 버전 오류 저장
            return false; // 검사 실패
        }

        if (versionStatus == SaveVersionStatus.UnsupportedOlderVersion) // 지원 중단 버전 확인
        {
            errorMessage = "더 이상 지원하지 않는 저장 데이터입니다."; // 지원 오류 저장
            return false; // 검사 실패
        }

        if (versionStatus == SaveVersionStatus.RequiresMigration) // 변환 필요 버전 확인
        {
            errorMessage = "저장 데이터 버전 변환이 필요합니다."; // 변환 필요 저장
            return false; // 현재 단계 불러오기 차단
        }

        if (versionStatus == SaveVersionStatus.NewerThanGame) // 더 최신 버전 확인
        {
            errorMessage = "현재 게임보다 최신 버전에서 생성된 저장 데이터입니다."; // 최신 버전 오류 저장
            return false; // 검사 실패
        }

        if (string.IsNullOrWhiteSpace(saveData.saveSlotId)) // 저장 슬롯 ID 확인
        {
            errorMessage = "저장 슬롯 ID가 비어 있습니다."; // 슬롯 오류 저장
            return false; // 검사 실패
        }

        if (string.IsNullOrWhiteSpace(saveData.savedAtUtc)) // 저장 시각 확인
        {
            errorMessage = "저장 시각이 비어 있습니다."; // 시각 오류 저장
            return false; // 검사 실패
        }

        if (string.IsNullOrWhiteSpace(saveData.sceneName)) // Scene 이름 확인
        {
            errorMessage = "저장 Scene 이름이 비어 있습니다."; // Scene 오류 저장
            return false; // 검사 실패
        }

        bool hasMissingSection = saveData.player == null
            || saveData.time == null
            || saveData.inventory == null
            || saveData.equipment == null
            || saveData.world == null
            || saveData.respawn == null; // 필수 데이터 묶음 확인

        if (hasMissingSection) // 필수 데이터 누락 확인
        {
            errorMessage = "필수 저장 데이터 묶음이 누락되었습니다."; // 묶음 오류 저장
            return false; // 검사 실패
        }

        bool hasMissingCollection = saveData.inventory.slots == null
            || saveData.equipment.slots == null
            || saveData.world.worldItems == null
            || saveData.world.gatherableResources == null
            || saveData.world.placedStructures == null; // 필수 목록 확인

        if (hasMissingCollection) // 필수 목록 누락 확인
        {
            errorMessage = "저장 데이터 목록이 누락되었습니다."; // 목록 오류 저장
            return false; // 검사 실패
        }

        errorMessage = string.Empty; // 오류 내용 초기화
        return true; // 전체 검사 성공
    }
}