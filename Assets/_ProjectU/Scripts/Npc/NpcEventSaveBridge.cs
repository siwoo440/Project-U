using System; // 문자열 비교 기능
using System.Collections.Generic; // 목록 기능

// 94일차: 본 하트 이벤트(고른 선택지 · 날짜)를 저장 파일과 연결 (이벤트 대기 상태는 관계 단계로 다시 계산)
public static class NpcEventSaveBridge
{
    public const int MaxRecords = 200; // 저장 값 검사 상한

    public static bool TryCapture(SaveGameData saveData, out string errorMessage) // 이벤트 기록 수집
    {
        if (saveData == null)
        {
            errorMessage = "NPC 이벤트 저장 데이터가 비어 있습니다.";
            return false;
        }

        NpcEventManager manager = NpcEventManager.Instance;

        if (manager == null) // NPC 이벤트가 없는 Scene
        {
            saveData.hasNpcEventData = false;
            errorMessage = string.Empty;
            return true;
        }

        saveData.hasNpcEventData = true;
        saveData.npcEvents = manager.CaptureSaveData();
        errorMessage = string.Empty;
        return true;
    }

    public static bool TryRestore(SaveGameData saveData, out string errorMessage) // 이벤트 기록 복원
    {
        if (saveData == null)
        {
            errorMessage = "NPC 이벤트 저장 데이터가 비어 있습니다.";
            return false;
        }

        NpcEventManager manager = NpcEventManager.Instance;

        if (manager == null)
        {
            errorMessage = string.Empty;
            return true;
        }

        if (!TryValidate(saveData, out errorMessage))
        {
            return false;
        }

        if (saveData.hasNpcEventData)
        {
            manager.ApplySaveData(saveData.npcEvents);
        }
        else
        {
            manager.ResetForLoad(); // 94일차 이전 저장 파일 : 본 이벤트 없음
        }

        errorMessage = string.Empty;
        return true;
    }

    public static bool TryValidate(SaveGameData saveData, out string errorMessage) // 저장 구조 검사
    {
        if (!saveData.hasNpcEventData)
        {
            errorMessage = string.Empty;
            return true;
        }

        if (saveData.npcEvents == null || saveData.npcEvents.seen == null || saveData.npcEvents.seen.Count > MaxRecords)
        {
            errorMessage = "NPC 이벤트 저장 데이터가 누락되었거나 너무 많습니다.";
            return false;
        }

        HashSet<string> used = new HashSet<string>(StringComparer.Ordinal);

        foreach (NpcEventRecordSaveData record in saveData.npcEvents.seen)
        {
            if (record == null || string.IsNullOrWhiteSpace(record.eventId) || !record.eventId.StartsWith("event_", StringComparison.Ordinal) || !used.Add(record.eventId))
            {
                errorMessage = $"NPC 이벤트 ID가 잘못되었거나 중복입니다: {record?.eventId}";
                return false;
            }

            if (record.choiceIndex < 0 || record.day < 1)
            {
                errorMessage = $"NPC 이벤트 기록 값이 잘못되었습니다: {record.eventId}";
                return false;
            }
        }

        errorMessage = string.Empty;
        return true;
    }
}
