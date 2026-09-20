using UnityEngine; // Unity 기본 기능

public static class MeteorSaveBridge // 117일차: 운석 구덩이 저장 연결 (남은 운석 덩어리)
{
    public static bool TryCapture(SaveGameData saveData, out string errorMessage) // 저장
    {
        if (saveData == null)
        {
            errorMessage = "운석 저장 데이터가 없습니다.";
            return false;
        }

        MeteorEventManager manager = MeteorEventManager.Instance;
        saveData.hasMeteorData = manager != null;
        saveData.meteor = manager != null ? manager.CaptureSaveData() : new MeteorSaveData();
        errorMessage = string.Empty;
        return true;
    }

    public static void Apply(SaveGameData saveData) // 불러오기
    {
        MeteorEventManager manager = MeteorEventManager.Instance;

        if (manager == null)
        {
            return;
        }

        if (saveData != null && saveData.hasMeteorData)
        {
            manager.ApplySaveData(saveData.meteor);
            return;
        }

        manager.ResetForLoad(); // 117일차 이전 저장 파일 : 구덩이 없음
    }
}
