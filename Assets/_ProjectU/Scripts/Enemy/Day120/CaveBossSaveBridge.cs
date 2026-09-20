using System; // 직렬화
using UnityEngine; // Unity 기본 기능

[Serializable]
public sealed class CaveBossSaveData // 120일차: 동굴 보스 저장 (쓰러뜨렸는지)
{
    [Tooltip("보스 ID.")]
    public string bossId = string.Empty; // 보스 ID
    [Tooltip("이미 쓰러뜨렸는지.")]
    public bool defeated; // 쓰러뜨림 여부
}

public static class CaveBossSaveBridge // 120일차: 동굴 보스 상태 저장 연결
{
    public static bool TryCapture(SaveGameData saveData, out string errorMessage) // 저장
    {
        if (saveData == null)
        {
            errorMessage = "동굴 보스 저장 데이터가 없습니다.";
            return false;
        }

        CaveBossController boss = CaveBossController.Instance;
        saveData.hasCaveBossData = boss != null;
        saveData.caveBoss = boss != null
            ? new CaveBossSaveData { bossId = boss.BossId, defeated = boss.CaptureDefeated() }
            : new CaveBossSaveData();
        errorMessage = string.Empty;
        return true;
    }

    public static void Apply(SaveGameData saveData) // 불러오기
    {
        CaveBossController boss = CaveBossController.Instance;

        if (boss == null)
        {
            return;
        }

        if (saveData == null || !saveData.hasCaveBossData || saveData.caveBoss == null)
        {
            boss.ApplyDefeated(false); // 120일차 이전 저장 파일 : 보스가 그대로 있다
            return;
        }

        boss.ApplyDefeated(saveData.caveBoss.defeated);
    }
}
