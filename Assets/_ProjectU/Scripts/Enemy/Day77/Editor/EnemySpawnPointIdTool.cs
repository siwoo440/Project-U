using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 77일차: 현재 Scene의 EnemySpawnPoint 저장 ID 발급과 중복 수정
public static class EnemySpawnPointIdTool
{
    [MenuItem("Tools/Project U/Assign Enemy Spawn Point IDs")]
    private static void AssignEnemySpawnPointIds()
    {
        EnemySpawnPoint[] spawnPoints = UnityEngine.Object.FindObjectsByType<EnemySpawnPoint>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        HashSet<string> usedIds = new HashSet<string>(StringComparer.Ordinal);
        int generatedCount = 0;

        for (int index = 0; index < spawnPoints.Length; index++)
        {
            EnemySpawnPoint spawnPoint = spawnPoints[index];
            bool needsNewId = !spawnPoint.HasValidSpawnPointId || !usedIds.Add(spawnPoint.SpawnPointId);

            if (!needsNewId)
            {
                continue;
            }

            string newId = $"enemy_spawn_{Guid.NewGuid():N}";
            Undo.RecordObject(spawnPoint, "Assign Enemy Spawn Point ID");
            spawnPoint.AssignSpawnPointId(newId);
            usedIds.Add(newId);
            EditorUtility.SetDirty(spawnPoint);

            if (PrefabUtility.IsPartOfPrefabInstance(spawnPoint))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(spawnPoint);
            }

            if (spawnPoint.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(spawnPoint.gameObject.scene);
            }

            generatedCount++;
        }

        Debug.Log(
            $"Enemy Spawn Point ID 검사 완료 / 전체 {spawnPoints.Length}개 / 새 ID {generatedCount}개"
            + (generatedCount > 0 ? " / Scene을 저장하세요." : string.Empty));
    }
}
