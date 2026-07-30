using UnityEditor; // Unity Editor 메뉴 기능
using UnityEngine; // Unity JSON과 위치 기능

public static class SaveSchemaValidator // 저장 구조 Editor 검사
{
    [MenuItem("Tools/Project U/Validate Save Data Schema")] // 저장 구조 검사 메뉴
    private static void ValidateSaveDataSchema() // JSON 왕복 변환 검사
    {
        SaveGameData sampleSaveData = SaveGameData.CreateNew("20_Gameplay"); // 예제 저장 데이터 생성
        sampleSaveData.player.position = SaveVector3Data.FromVector3(new Vector3(5f, 1f, 3f)); // 예제 플레이어 위치 적용
        sampleSaveData.player.health = 75f; // 예제 체력 적용
        sampleSaveData.player.wetness = 35f; // 예제 젖음 수치 적용
        sampleSaveData.player.hasTemperatureData = true; // 예제 체온 데이터 존재 적용
        sampleSaveData.player.temperature = 64f; // 예제 체온 수치 적용

        sampleSaveData.time.currentDay = 3; // 예제 날짜 적용
        sampleSaveData.time.currentHour = 14.5f; // 예제 시간 적용
        sampleSaveData.time.hasWeatherData = true; // 예제 날씨 데이터 존재 적용
        sampleSaveData.time.currentWeather = (int)WeatherType.Storm; // 예제 폭풍 날씨 적용
        sampleSaveData.time.remainingWeatherHours = 2.5f; // 예제 날씨 남은 시간 적용

        InventorySlotSaveData sampleSlot = new InventorySlotSaveData(); // 예제 인벤토리 슬롯 생성
        sampleSlot.slotIndex = 0; // 예제 슬롯 번호 적용
        sampleSlot.itemId = "material_wood"; // 예제 아이템 ID 적용
        sampleSlot.quantity = 5; // 예제 아이템 수량 적용
        sampleSaveData.inventory.slots.Add(sampleSlot); // 예제 슬롯 목록 추가

        sampleSaveData.hasCraftingData = true; // 예제 제작법 해금 데이터 존재 적용
        sampleSaveData.crafting.unlockedRecipeIds.Add("recipe_axe"); // 예제 도끼 제작법 해금 적용
        sampleSaveData.crafting.unlockedRecipeIds.Add("recipe_pickaxe"); // 예제 곡괭이 제작법 해금 적용

        string json = JsonUtility.ToJson(sampleSaveData, true); // 저장 데이터를 JSON으로 변환
        SaveGameData restoredSaveData = JsonUtility.FromJson<SaveGameData>(json); // JSON을 저장 데이터로 복원

        if (!SaveDataValidator.TryValidate(restoredSaveData, out string errorMessage)) // 복원 데이터 유효성 확인
        {
            Debug.LogError($"저장 구조 검사 실패: {errorMessage}"); // 검사 실패 출력
            return; // 추가 검사 중단
        }

        bool slotRestored = restoredSaveData.inventory.slots.Count == 1
            && restoredSaveData.inventory.slots[0].itemId == "material_wood"
            && restoredSaveData.inventory.slots[0].quantity == 5; // 인벤토리 복원 결과 확인

        if (!slotRestored) // 인벤토리 복원 실패 확인
        {
            Debug.LogError("저장 구조 검사 실패: 인벤토리 데이터가 일치하지 않습니다."); // 복원 오류 출력
            return; // 검사 중단
        }

        bool craftingRestored = restoredSaveData.hasCraftingData // 제작 데이터 존재 확인
            && restoredSaveData.crafting != null // 제작 저장 묶음 확인
            && restoredSaveData.crafting.unlockedRecipeIds.Count == 2 // 해금 목록 수량 확인
            && restoredSaveData.crafting.unlockedRecipeIds.Contains("recipe_axe") // 도끼 제작법 확인
            && restoredSaveData.crafting.unlockedRecipeIds.Contains("recipe_pickaxe"); // 곡괭이 제작법 확인

        if (!craftingRestored) // 제작법 해금 복원 실패 확인
        {
            Debug.LogError("저장 구조 검사 실패: 제작법 해금 데이터가 일치하지 않습니다."); // 복원 오류 출력
            return; // 검사 중단
        }

        bool wetnessRestored = Mathf.Approximately(restoredSaveData.player.wetness, 35f); // 젖음 복원 결과 확인

        if (!wetnessRestored) // 젖음 복원 실패 확인
        {
            Debug.LogError("저장 구조 검사 실패: 젖음 데이터가 일치하지 않습니다."); // 젖음 복원 오류 출력
            return; // 검사 중단
        }

        bool temperatureRestored = restoredSaveData.player.hasTemperatureData
            && Mathf.Approximately(restoredSaveData.player.temperature, 64f); // 체온 복원 결과 확인

        if (!temperatureRestored) // 체온 복원 실패 확인
        {
            Debug.LogError("저장 구조 검사 실패: 체온 데이터가 일치하지 않습니다."); // 체온 복원 오류 출력
            return; // 검사 중단
        }

        bool weatherRestored = restoredSaveData.time.hasWeatherData // 날씨 데이터 존재 확인
            && restoredSaveData.time.currentWeather == (int)WeatherType.Storm // 폭풍 날씨 복원 확인
            && Mathf.Approximately(restoredSaveData.time.remainingWeatherHours, 2.5f); // 남은 시간 복원 확인

        if (!weatherRestored) // 날씨 복원 실패 확인
        {
            Debug.LogError("저장 구조 검사 실패: 날씨 데이터가 일치하지 않습니다."); // 날씨 복원 오류 출력
            return; // 검사 중단
        }

        SaveVersionStatus futureStatus = SaveVersionPolicy.GetStatus(SaveVersionPolicy.CurrentVersion + 1); // 미래 버전 판정 검사
        SaveVersionStatus invalidStatus = SaveVersionPolicy.GetStatus(0); // 잘못된 버전 판정 검사

        bool versionRulesAreValid = futureStatus == SaveVersionStatus.NewerThanGame
            && invalidStatus == SaveVersionStatus.Invalid; // 버전 판정 결과 확인

        if (!versionRulesAreValid) // 버전 규칙 오류 확인
        {
            Debug.LogError("저장 구조 검사 실패: 버전 판정 규칙이 올바르지 않습니다."); // 버전 오류 출력
            return; // 검사 중단
        }

        Debug.Log($"저장 데이터 구조와 버전 규칙 검사 완료\n{json}"); // 성공 결과와 JSON 출력
    }
}
