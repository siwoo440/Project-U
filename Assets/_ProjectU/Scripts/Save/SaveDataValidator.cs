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

        if (!TryValidateWeatherData(saveData, out errorMessage)) // 날씨 저장 데이터 검사
        {
            return false; // 전체 검사 실패
        }

        if (!TryValidateRespawnData(saveData, out errorMessage)) // 부활 지점 데이터 검사
        {
            return false; // 전체 검사 실패
        }

        errorMessage = string.Empty; // 오류 내용 초기화
        return true; // 전체 검사 성공
    }

    private static bool TryValidateWeatherData(SaveGameData saveData, out string errorMessage) // 날씨 저장 데이터 검사
    {
        if (!saveData.time.hasWeatherData) // 이전 저장 파일 확인
        {
            errorMessage = string.Empty; // 오류 내용 초기화
            return true; // 이전 저장 파일 허용
        }

        bool isWeatherDefined = System.Enum.IsDefined(typeof(WeatherType), saveData.time.currentWeather); // 날씨 숫자값 검사

        if (!isWeatherDefined) // 잘못된 날씨 확인
        {
            errorMessage = $"저장된 날씨 값이 잘못되었습니다: {saveData.time.currentWeather}"; // 날씨 오류 저장
            return false; // 검사 실패
        }

        float remainingHours = saveData.time.remainingWeatherHours; // 남은 날씨 시간 조회
        bool hasInvalidDuration = float.IsNaN(remainingHours) // 숫자 아님 확인
            || float.IsInfinity(remainingHours) // 무한대 확인
            || remainingHours <= 0f; // 양수 여부 확인

        if (hasInvalidDuration) // 잘못된 지속 시간 확인
        {
            errorMessage = $"날씨 남은 시간이 잘못되었습니다: {remainingHours}"; // 지속 시간 오류 저장
            return false; // 검사 실패
        }

        errorMessage = string.Empty; // 오류 내용 초기화
        return true; // 날씨 데이터 검사 성공
    }

    private static bool TryValidateRespawnData(
        SaveGameData saveData,
        out string errorMessage) // 부활 지점과 건축물 연결 검사
    {
        bool hasRegisteredPoint = saveData.respawn.hasRegisteredPoint; // 침낭 등록 상태 조회
        string registeredStructureId = saveData.respawn.registeredStructureId; // 저장 침낭 ID 조회

        if (!hasRegisteredPoint) // 미등록 상태 확인
        {
            if (!string.IsNullOrWhiteSpace(registeredStructureId)) // 미등록 상태의 ID 존재 확인
            {
                errorMessage = "부활 지점이 미등록 상태지만 침낭 ID가 남아 있습니다."; // 상태 충돌 저장
                return false; // 검사 실패
            }

            errorMessage = string.Empty; // 오류 내용 초기화
            return true; // 미등록 데이터 정상
        }

        if (string.IsNullOrWhiteSpace(registeredStructureId)) // 등록 침낭 ID 존재 확인
        {
            errorMessage = "등록된 부활 지점의 침낭 ID가 비어 있습니다."; // ID 오류 저장
            return false; // 검사 실패
        }

        bool hasMatchingStructure = false; // 일치 건축물 존재 여부 초기화

        for (int index = 0; index < saveData.world.placedStructures.Count; index++) // 저장 건축물 순회
        {
            PlacedStructureSaveData structureSaveData = saveData.world.placedStructures[index]; // 현재 건축물 조회

            if (structureSaveData == null) // 빈 건축물 항목 확인
            {
                errorMessage = "비어 있는 설치 건축물 저장 항목이 있습니다."; // 항목 오류 저장
                return false; // 검사 실패
            }

            if (!string.Equals(
                structureSaveData.structureId,
                registeredStructureId,
                System.StringComparison.Ordinal)) // 등록 침낭 ID 비교
            {
                continue; // 다른 건축물 건너뛰기
            }

            if (hasMatchingStructure) // 기존 일치 건축물 확인
            {
                errorMessage = $"부활 침낭 Structure ID가 중복되었습니다: {registeredStructureId}"; // 중복 오류 저장
                return false; // 검사 실패
            }

            hasMatchingStructure = true; // 일치 건축물 존재 표시
        }

        if (!hasMatchingStructure) // 저장 침낭 누락 여부 확인
        {
            errorMessage = $"부활 지점과 일치하는 건축물이 없습니다: {registeredStructureId}"; // 참조 오류 저장
            return false; // 검사 실패
        }

        errorMessage = string.Empty; // 오류 내용 초기화
        return true; // 검사 성공
    }
}
