// 109일차: 동료(지금 함께 다니는 NPC · 함께한 시간 · 하루 채집 · 동행 호감도)를 저장 파일과 연결
public static class NpcCompanionSaveBridge
{
    public static bool TryCapture(SaveGameData saveData, out string errorMessage) // 동료 수집
    {
        if (saveData == null)
        {
            errorMessage = "동료 저장 데이터가 비어 있습니다.";
            return false;
        }

        NpcCompanionManager manager = NpcCompanionManager.Instance;

        if (manager == null) // 동료 기능이 없는 Scene
        {
            saveData.hasCompanionData = false;
            errorMessage = string.Empty;
            return true;
        }

        saveData.hasCompanionData = true;
        saveData.companion = manager.CaptureSaveData();
        errorMessage = string.Empty;
        return true;
    }

    public static bool TryRestore(SaveGameData saveData, out string errorMessage) // 동료 복원
    {
        if (saveData == null)
        {
            errorMessage = "동료 저장 데이터가 비어 있습니다.";
            return false;
        }

        NpcCompanionManager manager = NpcCompanionManager.Instance;

        if (manager == null)
        {
            errorMessage = string.Empty;
            return true;
        }

        if (!TryValidate(saveData, out errorMessage))
        {
            return false;
        }

        if (saveData.hasCompanionData)
        {
            manager.ApplySaveData(saveData.companion);
        }
        else
        {
            manager.ResetForLoad(); // 109일차 이전 저장 파일 : 동료 없이 시작
        }

        errorMessage = string.Empty;
        return true;
    }

    public static bool TryValidate(SaveGameData saveData, out string errorMessage) // 동료 값 검사
    {
        if (saveData == null || !saveData.hasCompanionData)
        {
            errorMessage = string.Empty;
            return true;
        }

        NpcCompanionSaveData data = saveData.companion;

        if (data == null)
        {
            errorMessage = "동료 저장 데이터가 비어 있습니다.";
            return false;
        }

        if (data.hoursTogether < 0f || data.hoursTogether > 1000f || data.affinityGained < 0 || data.affinityGained > 100 || data.gathered < 0 || data.gathered > 1000)
        {
            errorMessage = "동료 저장 값이 범위를 벗어났습니다.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }
}
