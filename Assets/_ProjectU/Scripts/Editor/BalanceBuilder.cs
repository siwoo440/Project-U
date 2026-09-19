using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// 95일차: 1차 밸런스 조정 도구
// 1. 요리법(송어 구이 2개) · 판매 가격표(딸기 가격) · 의뢰 보상(원본 CSV)을 다시 읽는다
// 2. 적 전리품 표의 확률 · 수량을 조정하고, 원거리 적이 자기 전리품 표를 쓰도록 Prefab을 고친다
// 3. 게임 Scene의 적 생성 지점 재생성 시간을 늘린다 (반복 사냥 방지)
// 4. NPC 선물을 한 주에 2번까지로 정한다
// 5. 밸런스 검사를 하고 Docs/Balance/BalanceReport.md 표를 만든다
// 여러 번 실행해도 같은 값이 된다. 실행 후 Ctrl+S로 Scene을 저장해야 반영된다.
public static class BalanceBuilder
{
    private const string DialogTitle = "Project U 밸런스 조정";
    private const string ScenePath = "Assets/_ProjectU/Scenes/20_Gameplay.unity";
    private const string LootFolder = "Assets/_ProjectU/Prefabs/Items/Day75/Loot/";
    private const string EnemyFolder = "Assets/_ProjectU/Prefabs/Enemies/Day74/";
    public const int GiftsPerWeek = 2;

    // 적 전리품 : 표 → (아이템, 떨어질 확률, 최소, 최대). 표에 이미 있는 아이템만 고친다.
    private static readonly (string table, (string itemId, float chance, int min, int max)[] entries)[] LootTuning =
    {
        ("EnemyLootTable_MeleeGrunt", new[] { ("item_plant_fiber", 1f, 1, 2), ("item_wild_mushroom", 0.6f, 1, 1), ("item_iron_ore", 0.35f, 1, 1) }),
        ("EnemyLootTable_RangedSpitter", new[] { ("item_plant_fiber", 1f, 1, 2), ("item_wild_mushroom", 0.4f, 1, 2), ("item_iron_ore", 0.25f, 1, 1) })
    };

    // 적 Prefab → 쓸 전리품 표
    private static readonly (string prefab, string table)[] LootLinks =
    {
        ("Enemy_MeleeGrunt", "EnemyLootTable_MeleeGrunt"),
        ("Enemy_RangedSpitter", "EnemyLootTable_RangedSpitter")
    };

    // 적 Prefab → 다시 나오는 시간 (실제 초, 하루 600초 기준 근접 약 7시간 · 원거리 약 10시간)
    private static readonly (string prefab, float seconds)[] RespawnSeconds =
    {
        ("Enemy_MeleeGrunt", 180f),
        ("Enemy_RangedSpitter", 240f)
    };

    // ---------------------------------------------------------------- 전체 적용

    public static string BuildAll()
    {
        StringBuilder report = new StringBuilder("[밸런스 조정]\n");

        try
        {
            EditorUtility.DisplayProgressBar(DialogTitle, "요리법", 0.1f);
            report.AppendLine(CookingContentBuilder.RefreshRecipes());

            EditorUtility.DisplayProgressBar(DialogTitle, "판매 가격표", 0.2f);
            report.AppendLine(MarketContentBuilder.RefreshCatalog());

            EditorUtility.DisplayProgressBar(DialogTitle, "의뢰 보상", 0.3f);
            report.AppendLine(NpcQuestBuilder.RefreshQuestData());

            EditorUtility.DisplayProgressBar(DialogTitle, "적 전리품", 0.5f);
            TuneLootTables(report);
            LinkEnemyLoot(report);

            EditorUtility.DisplayProgressBar(DialogTitle, "NPC 선물 규칙", 0.65f);
            TuneGiftRule(report);
            AssetDatabase.SaveAssets();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        report.Append(WireScene());
        BalanceReport.Snapshot snapshot = BalanceReport.Analyze();
        report.AppendLine($"밸런스 표 : {BalanceReport.WriteMarkdown(snapshot)}");
        report.Append(Validate(out _));
        return report.ToString();
    }

    private static void TuneLootTables(StringBuilder report)
    {
        foreach ((string tableName, (string itemId, float chance, int min, int max)[] entries) in LootTuning)
        {
            EnemyLootTable table = AssetDatabase.LoadAssetAtPath<EnemyLootTable>(LootFolder + tableName + ".asset");

            if (table == null)
            {
                report.AppendLine($"✗ 전리품 표 {tableName} 가 없습니다.");
                continue;
            }

            SerializedObject serialized = new SerializedObject(table);
            SerializedProperty list = serialized.FindProperty("entries");
            List<string> applied = new List<string>();

            foreach ((string itemId, float chance, int min, int max) in entries)
            {
                for (int index = 0; index < list.arraySize; index++)
                {
                    SerializedProperty element = list.GetArrayElementAtIndex(index);

                    if (element.FindPropertyRelative("itemData").objectReferenceValue is ItemData item && item.ItemId == itemId)
                    {
                        element.FindPropertyRelative("dropChance").floatValue = chance;
                        element.FindPropertyRelative("minimumQuantity").intValue = min;
                        element.FindPropertyRelative("maximumQuantity").intValue = max;
                        applied.Add($"{item.DisplayName} {chance * 100f:0}% {min}~{max}");
                    }
                }
            }

            if (serialized.ApplyModifiedPropertiesWithoutUndo())
            {
                EditorUtility.SetDirty(table);
            }

            report.AppendLine($"{tableName} : {string.Join(" · ", applied)}");
        }
    }

    private static void LinkEnemyLoot(StringBuilder report)
    {
        foreach ((string prefabName, string tableName) in LootLinks)
        {
            string path = EnemyFolder + prefabName + ".prefab";
            EnemyLootTable table = AssetDatabase.LoadAssetAtPath<EnemyLootTable>(LootFolder + tableName + ".asset");
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            EnemyLootDropper dropper = root != null ? root.GetComponentInChildren<EnemyLootDropper>(true) : null;

            if (table == null || dropper == null)
            {
                report.AppendLine($"✗ {prefabName} Prefab 또는 전리품 표({tableName})를 찾지 못했습니다.");
                continue;
            }

            SerializedObject serialized = new SerializedObject(dropper);
            SerializedProperty property = serialized.FindProperty("lootTable");

            if (property.objectReferenceValue != table)
            {
                property.objectReferenceValue = table;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(dropper);
                PrefabUtility.SavePrefabAsset(root);
                report.AppendLine($"{prefabName} : 전리품 표를 {tableName} 로 연결");
            }
            else
            {
                report.AppendLine($"{prefabName} : 전리품 표 {tableName} (변경 없음)");
            }
        }
    }

    private static void TuneGiftRule(StringBuilder report)
    {
        NpcDatabase database = AssetDatabase.LoadAssetAtPath<NpcDatabase>(NpcContentBuilder.DatabasePath);

        if (database == null)
        {
            report.AppendLine("✗ NpcDatabase가 없습니다. 7번 메뉴를 먼저 실행하세요.");
            return;
        }

        SerializedObject serialized = new SerializedObject(database);
        serialized.FindProperty("giftsPerWeek").intValue = GiftsPerWeek;

        if (serialized.ApplyModifiedPropertiesWithoutUndo())
        {
            EditorUtility.SetDirty(database);
        }

        report.AppendLine($"NPC 선물 : 하루 {database.GiftsPerDay}번 · 한 주 {database.GiftsPerWeek}번 (생일 선물은 한 주 제한 없음)");
    }

    private static string WireScene()
    {
        StringBuilder report = new StringBuilder();
        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.GetActiveScene();

        if (scene.path != ScenePath)
        {
            report.AppendLine($"✗ 게임 Scene({ScenePath})을 열고 다시 실행하세요. 적 재생성 시간은 바꾸지 못했습니다.");
            return report.ToString();
        }

        int changed = 0;
        List<string> points = new List<string>();

        foreach (EnemySpawnPoint point in Object.FindObjectsByType<EnemySpawnPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            SerializedObject serialized = new SerializedObject(point);
            GameObject prefab = serialized.FindProperty("enemyPrefab").objectReferenceValue as GameObject;
            float target = TargetRespawn(prefab);

            if (target <= 0f)
            {
                continue;
            }

            SerializedProperty delay = serialized.FindProperty("respawnDelay");

            if (!Mathf.Approximately(delay.floatValue, target))
            {
                delay.floatValue = target;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(point);
                changed++;
            }

            points.Add($"{point.name} {target:0}초");
        }

        points.Sort(StringComparer.Ordinal);
        report.AppendLine($"적 재생성 시간 : {string.Join(" · ", points)} (바뀐 곳 {changed})");

        if (changed > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
        }

        report.AppendLine("Scene 변경 완료 : Ctrl+S로 저장하세요.");
        return report.ToString();
    }

    private static float TargetRespawn(GameObject prefab)
    {
        if (prefab == null)
        {
            return 0f;
        }

        foreach ((string prefabName, float seconds) in RespawnSeconds)
        {
            if (prefab.name == prefabName)
            {
                return seconds;
            }
        }

        return 0f;
    }

    // ---------------------------------------------------------------- 검사

    public static string Validate(out int errorCount)
    {
        string balance = BalanceReport.Validate(out int balanceErrors);
        StringBuilder report = new StringBuilder();
        int errors = 0;

        void Error(string message)
        {
            errors++;
            report.AppendLine("✗ " + message);
        }

        // 조정 값이 적용되어 있는지 (13번 메뉴를 실행했는지)
        foreach ((string tableName, (string itemId, float chance, int min, int max)[] entries) in LootTuning)
        {
            EnemyLootTable table = AssetDatabase.LoadAssetAtPath<EnemyLootTable>(LootFolder + tableName + ".asset");

            if (table == null)
            {
                Error($"전리품 표 {tableName} 가 없습니다.");
                continue;
            }

            foreach ((string itemId, float chance, int min, int max) in entries)
            {
                bool found = false;

                foreach (EnemyLootTable.LootEntry entry in table.Entries)
                {
                    if (entry?.ItemData != null && entry.ItemData.ItemId == itemId)
                    {
                        found = true;

                        if (!Mathf.Approximately(entry.DropChance, chance) || entry.MinimumQuantity != min || entry.MaximumQuantity != max)
                        {
                            Error($"{tableName} {itemId} : 조정 값과 다릅니다. 13번 메뉴를 실행하세요.");
                        }
                    }
                }

                if (!found)
                {
                    Error($"{tableName} 에 {itemId} 가 없습니다.");
                }
            }
        }

        NpcDatabase database = AssetDatabase.LoadAssetAtPath<NpcDatabase>(NpcContentBuilder.DatabasePath);

        if (database != null && database.GiftsPerWeek != GiftsPerWeek)
        {
            Error($"NPC 선물 한 주 제한이 {database.GiftsPerWeek}번입니다. 13번 메뉴를 실행하세요. ({GiftsPerWeek}번 목표)");
        }

        errorCount = balanceErrors + errors;
        string text = balance.Replace(balanceErrors == 0 ? "결과 : 오류 0개" : $"결과 : 오류 {balanceErrors}개", string.Empty).TrimEnd('\r', '\n');
        return text + "\n" + report + (errorCount == 0 ? "결과 : 오류 0개\n" : $"결과 : 오류 {errorCount}개\n");
    }
}
