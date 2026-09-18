using System; // 문자열 비교 기능
using System.Collections.Generic; // 목록 기능

// 91일차: NPC 관계(만남 · 호감도 · 하루 대화·선물 기록)를 저장 파일과 연결
// NPC 위치는 시간으로 정해지므로 저장하지 않는다.
public static class NpcSaveBridge
{
    public const int MaxAffinity = 100; // 저장 값 검사 상한

    public static bool TryCapture(SaveGameData saveData, out string errorMessage) // 관계 수집
    {
        if (saveData == null)
        {
            errorMessage = "NPC 저장 데이터가 비어 있습니다.";
            return false;
        }

        NpcRelationshipManager manager = NpcRelationshipManager.Instance;

        if (manager == null) // NPC 기능이 없는 Scene
        {
            saveData.hasNpcData = false;
            errorMessage = string.Empty;
            return true;
        }

        saveData.hasNpcData = true;
        saveData.npc = manager.CaptureSaveData();
        errorMessage = string.Empty;
        return true;
    }

    public static bool TryRestore(SaveGameData saveData, out string errorMessage) // 관계 복원
    {
        if (saveData == null)
        {
            errorMessage = "NPC 저장 데이터가 비어 있습니다.";
            return false;
        }

        NpcRelationshipManager manager = NpcRelationshipManager.Instance;

        if (manager == null)
        {
            errorMessage = string.Empty;
            return true;
        }

        if (!TryValidate(saveData, out errorMessage))
        {
            return false;
        }

        if (saveData.hasNpcData)
        {
            manager.ApplySaveData(saveData.npc);
        }
        else
        {
            manager.ResetForLoad(); // 91일차 이전 저장 파일 : 관계 없이 시작
        }

        errorMessage = string.Empty;
        return true;
    }

    public static bool TryValidate(SaveGameData saveData, out string errorMessage) // 저장 구조 검사
    {
        if (!saveData.hasNpcData)
        {
            errorMessage = string.Empty;
            return true;
        }

        if (saveData.npc == null || saveData.npc.relationships == null)
        {
            errorMessage = "NPC 관계 저장 데이터가 누락되었습니다.";
            return false;
        }

        HashSet<string> usedIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (NpcRelationshipSaveData entry in saveData.npc.relationships)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.characterId) || !entry.characterId.StartsWith("char_"))
            {
                errorMessage = "NPC 관계 저장 항목의 캐릭터 ID가 잘못되었습니다.";
                return false;
            }

            if (!usedIds.Add(entry.characterId))
            {
                errorMessage = $"NPC 관계 저장 항목이 중복되었습니다: {entry.characterId}";
                return false;
            }

            if (entry.affinity < 0 || entry.affinity > MaxAffinity)
            {
                errorMessage = $"NPC 호감도가 범위를 벗어났습니다: {entry.characterId} {entry.affinity}";
                return false;
            }

            if (entry.lastTalkDay < -1 || entry.lastGiftDay < -1 || entry.giftsOnLastGiftDay < 0 || entry.receivedGiftCount < 0 || entry.talkDays < 0)
            {
                errorMessage = $"NPC 관계 기록 값이 잘못되었습니다: {entry.characterId}";
                return false;
            }
        }

        errorMessage = string.Empty;
        return true;
    }
}
