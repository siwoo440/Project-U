using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public static class CaveSaveBridge // 116일차: 동굴 상태 저장 연결 (지금 있는 동굴 · 찾아낸 입구)
{
    public static bool TryCapture(SaveGameData saveData, out string errorMessage) // 저장
    {
        if (saveData == null)
        {
            errorMessage = "동굴 저장 데이터가 없습니다.";
            return false;
        }

        CaveManager manager = CaveManager.Instance;
        saveData.hasCaveData = manager != null;
        saveData.cave = manager != null ? manager.CaptureSaveData() : new CaveSaveData();
        errorMessage = string.Empty;
        return true;
    }

    public static void Apply(SaveGameData saveData) // 불러오기
    {
        CaveManager manager = CaveManager.Instance;

        if (manager == null)
        {
            return;
        }

        if (saveData != null && saveData.hasCaveData)
        {
            manager.ApplySaveData(saveData.cave);
            return;
        }

        manager.ResetForLoad(); // 116일차 이전 저장 파일 : 밖에서 시작
    }
}
