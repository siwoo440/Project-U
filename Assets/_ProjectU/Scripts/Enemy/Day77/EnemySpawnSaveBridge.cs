using System;
using System.Collections.Generic;
using UnityEngine;

// 77일차: Scene의 EnemySpawnPoint 처치·재생성 상태를 저장 파일과 연결
public static class EnemySpawnSaveBridge
{
    public static bool TryCapture(SaveGameData saveData, out string errorMessage)
    {
        if (saveData == null)
        {
            errorMessage = "적 스폰 저장 데이터가 비어 있습니다.";
            return false;
        }

        EnemySpawnPoint[] spawnPoints = FindSpawnPoints();
        List<EnemySpawnPointSaveData> savedPoints = new List<EnemySpawnPointSaveData>(spawnPoints.Length);
        HashSet<string> usedIds = new HashSet<string>(StringComparer.Ordinal);

        for (int index = 0; index < spawnPoints.Length; index++)
        {
            EnemySpawnPoint spawnPoint = spawnPoints[index];

            if (!spawnPoint.HasValidSpawnPointId)
            {
                Debug.LogWarning(
                    $"{spawnPoint.name}에 Spawn Point ID가 없어 저장에서 제외합니다. "
                    + "Tools/Project U/Assign Enemy Spawn Point IDs 메뉴로 ID를 발급하세요.",
                    spawnPoint);
                continue;
            }

            if (!usedIds.Add(spawnPoint.SpawnPointId))
            {
                errorMessage = $"적 Spawn Point ID가 중복되었습니다: {spawnPoint.SpawnPointId}";
                return false;
            }

            savedPoints.Add(new EnemySpawnPointSaveData
            {
                spawnPointId = spawnPoint.SpawnPointId,
                isDefeated = spawnPoint.IsDefeated,
                respawnRemainingSeconds = spawnPoint.IsDefeated ? spawnPoint.RespawnRemainingSeconds : 0f
            });
        }

        saveData.hasEnemySpawnData = true;
        saveData.enemySpawns = new EnemySpawnSaveData { spawnPoints = savedPoints };
        errorMessage = string.Empty;
        return true;
    }

    public static bool TryRestore(SaveGameData saveData, out string errorMessage)
    {
        if (saveData == null)
        {
            errorMessage = "적 스폰 저장 데이터가 비어 있습니다.";
            return false;
        }

        // 77일차 이전 저장 파일은 기본 생성 상태를 그대로 사용
        if (!saveData.hasEnemySpawnData)
        {
            errorMessage = string.Empty;
            return true;
        }

        if (saveData.enemySpawns == null || saveData.enemySpawns.spawnPoints == null)
        {
            errorMessage = "적 스폰 지점 저장 목록이 누락되었습니다.";
            return false;
        }

        Dictionary<string, EnemySpawnPointSaveData> savedById =
            new Dictionary<string, EnemySpawnPointSaveData>(StringComparer.Ordinal);

        for (int index = 0; index < saveData.enemySpawns.spawnPoints.Count; index++)
        {
            EnemySpawnPointSaveData savedPoint = saveData.enemySpawns.spawnPoints[index];

            if (savedPoint != null && !string.IsNullOrWhiteSpace(savedPoint.spawnPointId))
            {
                savedById[savedPoint.spawnPointId] = savedPoint;
            }
        }

        EnemySpawnPoint[] spawnPoints = FindSpawnPoints();

        for (int index = 0; index < spawnPoints.Length; index++)
        {
            EnemySpawnPoint spawnPoint = spawnPoints[index];

            // 저장 이후 새로 추가된 Spawn Point는 살아 있는 기본 상태로 둔다
            if (!spawnPoint.HasValidSpawnPointId
                || !savedById.TryGetValue(spawnPoint.SpawnPointId, out EnemySpawnPointSaveData savedPoint))
            {
                spawnPoint.ApplySavedState(false, 0f);
                continue;
            }

            spawnPoint.ApplySavedState(savedPoint.isDefeated, savedPoint.respawnRemainingSeconds);
        }

        errorMessage = string.Empty;
        return true;
    }

    private static EnemySpawnPoint[] FindSpawnPoints()
    {
        return UnityEngine.Object.FindObjectsByType<EnemySpawnPoint>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);
    }
}
