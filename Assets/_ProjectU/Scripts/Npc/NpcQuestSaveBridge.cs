using System; // 문자열 비교 기능
using System.Collections.Generic; // 목록 기능

// 93일차: 게시판 · 진행 중 의뢰 · 완료 기록을 저장 파일과 연결 (관계 복원 뒤에 복원)
public static class NpcQuestSaveBridge
{
    public const int MaxActive = 20; // 저장 값 검사 상한 (진행 중 · 게시판)

    public static bool TryCapture(SaveGameData saveData, out string errorMessage) // 의뢰 상태 수집
    {
        if (saveData == null)
        {
            errorMessage = "NPC 의뢰 저장 데이터가 비어 있습니다.";
            return false;
        }

        NpcQuestManager manager = NpcQuestManager.Instance;

        if (manager == null) // NPC 의뢰가 없는 Scene
        {
            saveData.hasNpcQuestData = false;
            errorMessage = string.Empty;
            return true;
        }

        saveData.hasNpcQuestData = true;
        saveData.npcQuests = manager.CaptureSaveData();
        errorMessage = string.Empty;
        return true;
    }

    public static bool TryRestore(SaveGameData saveData, out string errorMessage) // 의뢰 상태 복원
    {
        if (saveData == null)
        {
            errorMessage = "NPC 의뢰 저장 데이터가 비어 있습니다.";
            return false;
        }

        NpcQuestManager manager = NpcQuestManager.Instance;

        if (manager == null)
        {
            errorMessage = string.Empty;
            return true;
        }

        if (!TryValidate(saveData, out errorMessage))
        {
            return false;
        }

        int today = saveData.time != null ? saveData.time.currentDay : manager.CurrentDay; // 시간 적용 전이므로 저장 값 사용

        if (saveData.hasNpcQuestData)
        {
            manager.ApplySaveData(saveData.npcQuests, today);
        }
        else
        {
            manager.ResetForLoad(today); // 93일차 이전 저장 파일 : 의뢰 없이 시작
        }

        errorMessage = string.Empty;
        return true;
    }

    public static bool TryValidate(SaveGameData saveData, out string errorMessage) // 저장 구조 검사
    {
        if (!saveData.hasNpcQuestData)
        {
            errorMessage = string.Empty;
            return true;
        }

        NpcQuestSaveData quests = saveData.npcQuests;

        if (quests == null || quests.board == null || quests.active == null || quests.records == null)
        {
            errorMessage = "NPC 의뢰 저장 데이터가 누락되었습니다.";
            return false;
        }

        if (quests.boardDay < 0 || quests.totalCompleted < 0 || quests.totalExpired < 0 || quests.board.Count > MaxActive || quests.active.Count > MaxActive)
        {
            errorMessage = "NPC 의뢰 기록 값이 잘못되었습니다.";
            return false;
        }

        HashSet<string> used = new HashSet<string>(StringComparer.Ordinal);

        foreach (string questId in quests.board)
        {
            if (!IsQuestId(questId) || !used.Add(questId))
            {
                errorMessage = $"게시판 의뢰 ID가 잘못되었거나 중복입니다: {questId}";
                return false;
            }
        }

        foreach (NpcQuestActiveSaveData entry in quests.active)
        {
            if (entry == null || !IsQuestId(entry.questId) || !used.Add(entry.questId))
            {
                errorMessage = $"진행 중 의뢰 ID가 잘못되었거나 중복입니다: {entry?.questId}";
                return false;
            }

            if (entry.acceptedDay < 1 || entry.deadlineDay < entry.acceptedDay)
            {
                errorMessage = $"진행 중 의뢰의 날짜가 잘못되었습니다: {entry.questId}";
                return false;
            }
        }

        HashSet<string> recorded = new HashSet<string>(StringComparer.Ordinal);

        foreach (NpcQuestRecordSaveData record in quests.records)
        {
            if (record == null || !IsQuestId(record.questId) || !recorded.Add(record.questId) || record.completedCount < 0)
            {
                errorMessage = $"의뢰 완료 기록이 잘못되었습니다: {record?.questId}";
                return false;
            }
        }

        errorMessage = string.Empty;
        return true;
    }

    private static bool IsQuestId(string questId) // quest_ 로 시작하는 ID
    {
        return !string.IsNullOrWhiteSpace(questId) && questId.StartsWith("quest_", StringComparison.Ordinal);
    }
}
