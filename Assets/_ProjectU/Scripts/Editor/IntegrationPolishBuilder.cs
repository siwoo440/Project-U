using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

// 115일차: 35명 통합 점검 · 남은 문제 정리
// 1. 한글 줄바꿈 : 말풍선 · 대화 창에서 낱말 중간(예 "만들었는\n데")이 아니라 띄어쓰기에서 줄이 나뉘게 (TMP 설정)
// 2. 아이템 한글 이름 : NPC 대사 · 의뢰 · 선물 창 ("TROUT 찾았다냥" → "송어 찾았다냥")
// 3. 이웃 관계 6쌍 추가 : 관계가 없던 12명도 모두 짝이 생김 (21쌍)
// 4. 흙길 갈림목 표지판 : 구역 입구에서 마을 쪽 · 갈림길이 갈라지는 큰길에서 양쪽
// 5. NavMesh 다시 굽기 : Terrain 나무 · 바위 자리를 길에서 빼 NPC가 나무를 지나가지 않게
// 코드로만 바뀐 것 (자동 적용 없음) : NPC 자리 두 줄 · 자리 겹침 방지, 지도 한낮 조명, 말풍선 줄 나누기, 적이 동료도 공격 · 동료 기력
// 자동 적용(ProjectUAutoContent)이 실행한다. 여러 번 실행해도 같은 결과가 나온다.
public static class IntegrationPolishBuilder
{
    private const string ScenePath = "Assets/_ProjectU/Scenes/20_Gameplay.unity";
    private const float TreeCheckRoadDistance = 25f; // 흙길에서 이 거리 안 나무만 검사 (NPC가 다니는 곳)
    private const float AllowedTreeOnPathRatio = 0.03f; // 줄기 자리가 길로 남아도 되는 비율
    private const float MinimumQuestRewardRatio = 1.1f; // 보상 코인 ÷ 가져올 물건 판매가 (보상 아이템 없는 의뢰)

    public static string BuildAll(bool saveScene)
    {
        StringBuilder report = new StringBuilder("[115일차 통합 점검 · 정리]\n");
        report.AppendLine(SetHangulLineBreaking());
        report.AppendLine(ItemKoreanNameBuilder.Apply());
        string relations = NpcRelationBuilder.BuildAll(false);
        report.AppendLine(string.Join("\n", relations.Split('\n').Where(line => line.StartsWith("✗") || line.StartsWith("관계 ") || line.StartsWith("이웃 대화")).Select(line => line.TrimEnd('\r'))));

        if (EditorSceneManager.GetActiveScene().path != ScenePath)
        {
            report.AppendLine($"✗ 게임 Scene({ScenePath})이 열려 있지 않아 표지판 · NavMesh는 바꾸지 못했습니다.");
            return report.ToString();
        }

        report.AppendLine(WorldZoneBuilder.RefreshJunctionSigns(EditorSceneManager.GetActiveScene()));
        report.AppendLine(WorldZoneBuilder.RebakeNavMesh());
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        if (saveScene)
        {
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            report.AppendLine("게임 Scene 저장 완료");
        }

        report.Append(Validate(out _));
        return report.ToString();
    }

    private static string SetHangulLineBreaking() // TMP 한글 줄바꿈 : 띄어쓰기 기준 (현대 한글 규칙)
    {
        TMP_Settings settings = TMP_Settings.instance;

        if (settings == null)
        {
            return "✗ TMP Settings를 찾지 못했습니다.";
        }

        SerializedObject serialized = new SerializedObject(settings);
        SerializedProperty property = serialized.FindProperty("m_UseModernHangulLineBreakingRules");

        if (property == null)
        {
            return "✗ TMP Settings에 한글 줄바꿈 설정이 없습니다.";
        }

        if (property.boolValue)
        {
            return "한글 줄바꿈 : 이미 띄어쓰기 기준";
        }

        property.boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        return "한글 줄바꿈 : 띄어쓰기 기준으로 바꿈";
    }

    public static string Validate(out int errorCount)
    {
        StringBuilder report = new StringBuilder("[115일차 통합 점검 검증]\n");
        int errors = 0;

        void Error(string message)
        {
            errors++;
            report.AppendLine("✗ " + message);
        }

        // 1. 한글 줄바꿈
        if (TMP_Settings.instance == null || !TMP_Settings.useModernHangulLineBreakingRules)
        {
            Error("TMP 한글 줄바꿈이 띄어쓰기 기준이 아닙니다. 콘텐츠 자동 적용을 기다리세요.");
        }

        // 2. 말풍선 줄 나누기 (대사가 가장 긴 줄)
        NpcRelationBook book = AssetDatabase.LoadAssetAtPath<NpcRelationBook>(NpcRelationBuilder.BookPath);
        List<string> speech = book != null ? NpcRelationBuilder.CollectTexts(book).Where(text => !string.IsNullOrWhiteSpace(text)).ToList() : new List<string>();
        int longestLine = speech.Count > 0 ? speech.Max(text => NpcAgent.BalanceLines(text).Split('\n').Max(line => line.Length)) : 0;
        report.AppendLine($"말풍선 : 이웃 대사 {speech.Count}줄 · 나눈 뒤 가장 긴 줄 {longestLine}자 (한 줄 기준 {NpcAgent.SpeechLineChars}자)");

        // 3. 관계 없는 이웃
        NpcDatabase database = AssetDatabase.LoadAssetAtPath<NpcDatabase>(NpcContentBuilder.DatabasePath);

        if (database != null && book != null)
        {
            HashSet<string> linked = new HashSet<string>(book.Pairs.SelectMany(pair => new[] { pair.characterA, pair.characterB }), StringComparer.Ordinal);
            List<NpcCharacterData> alone = database.GetPlacedCast().Where(character => !linked.Contains(character.CharacterId)).ToList();
            report.AppendLine($"이웃 관계 : {book.Pairs.Count}쌍 · 관계 없는 이웃 {alone.Count}명{(alone.Count > 0 ? " (" + string.Join(", ", alone.Select(character => character.DisplayName)) + ")" : string.Empty)}");
        }

        // 4. 의뢰 보상 균형 : 보상 아이템이 없는 의뢰는 코인이 가져올 물건 판매가보다 커야 함
        ValidateQuestRewards(Error, report);

        // 5. 게임 Scene : 갈림목 표지판 · 나무와 길
        if (EditorSceneManager.GetActiveScene().path == ScenePath)
        {
            GameObject root = EditorSceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(item => item.name == WorldZoneBuilder.RootName);
            Transform signs = root != null ? root.transform.Find("Signposts") : null;
            int junctionSigns = signs != null ? signs.Cast<Transform>().Count(child => child.name.StartsWith("Sign_" + WorldZoneBuilder.JunctionPrefix, StringComparison.Ordinal)) : 0;

            if (IslandTerrainBuilder.IsIslandTerrain(Terrain.activeTerrain) && junctionSigns != WorldZoneBuilder.ExpectedJunctionSigns)
            {
                Error($"흙길 갈림목 표지판이 {junctionSigns}개입니다 ({WorldZoneBuilder.ExpectedJunctionSigns}개 필요). 콘텐츠 자동 적용을 기다리세요.");
            }

            report.AppendLine($"갈림목 표지판 {junctionSigns}개");
            ValidateTrees(Error, report);
        }

        report.AppendLine($"결과 : 오류 {errors}개");
        errorCount = errors;
        return report.ToString();
    }

    private static void ValidateQuestRewards(Action<string> error, StringBuilder report)
    {
        MarketCatalogData catalog = AssetDatabase.LoadAssetAtPath<MarketCatalogData>(MarketContentBuilder.CatalogPath);

        if (catalog == null)
        {
            return;
        }

        Dictionary<ItemData, int> prices = new Dictionary<ItemData, int>();

        foreach (MarketPriceEntry entry in catalog.Prices.Where(entry => entry != null && entry.Item != null))
        {
            prices[entry.Item] = entry.BasePrice;
        }

        float lowest = float.MaxValue;
        string lowestQuest = string.Empty;
        int checkedQuests = 0;

        foreach (NpcQuestBook questBook in NpcQuestBuilder.LoadBooks())
        {
            foreach (NpcQuestBook.Quest quest in questBook.Quests)
            {
                if (quest.RewardItem != null)
                {
                    continue; // 보상 아이템(도끼 · 가방 · 진주 등)이 있으면 코인이 적어도 됨
                }

                int value = quest.Requirements.Sum(requirement => requirement.Item != null && prices.TryGetValue(requirement.Item, out int price) ? price * requirement.Amount : 0);

                if (value <= 0)
                {
                    continue;
                }

                checkedQuests++;
                float ratio = quest.RewardCoins / (float)value;

                if (ratio < lowest)
                {
                    lowest = ratio;
                    lowestQuest = quest.QuestId;
                }

                if (ratio < MinimumQuestRewardRatio)
                {
                    error($"{quest.QuestId} 의뢰 보상 {quest.RewardCoins}코인이 가져올 물건 판매가 {value}코인의 {MinimumQuestRewardRatio:0.0}배보다 적습니다 (파는 게 더 이득).");
                }
            }
        }

        report.AppendLine($"의뢰 보상 : 보상 아이템 없는 의뢰 {checkedQuests}개 · 가장 낮은 보상 비율 {(checkedQuests > 0 ? lowest : 0f):0.00}배 ({lowestQuest})");
    }

    private static void ValidateTrees(Action<string> error, StringBuilder report)
    {
        Terrain terrain = Terrain.activeTerrain;

        if (terrain == null || terrain.terrainData == null || !IslandTerrainBuilder.IsIslandTerrain(terrain) || NavMesh.CalculateTriangulation().indices.Length == 0)
        {
            return;
        }

        TerrainData data = terrain.terrainData;
        bool[] solid = data.treePrototypes.Select(prototype => prototype.prefab != null && prototype.prefab.GetComponentInChildren<CapsuleCollider>() != null).ToArray();
        int checkedTrees = 0;
        int onPath = 0;

        foreach (TreeInstance tree in data.treeInstances)
        {
            if (tree.prototypeIndex < 0 || tree.prototypeIndex >= solid.Length || !solid[tree.prototypeIndex])
            {
                continue;
            }

            Vector3 world = Vector3.Scale(tree.position, data.size) + terrain.transform.position;

            if (IslandZoneLayout.RoadDistanceWorld(world) > TreeCheckRoadDistance)
            {
                continue;
            }

            checkedTrees++;
            onPath += NavMesh.SamplePosition(world, out NavMeshHit _, 0.35f, NavMesh.AllAreas) ? 1 : 0;
        }

        if (checkedTrees > 0 && onPath > checkedTrees * AllowedTreeOnPathRatio)
        {
            error($"흙길 가까운 나무 {checkedTrees}그루 가운데 {onPath}그루의 줄기 자리가 NPC 길(NavMesh)에 남아 있습니다. 콘텐츠 자동 적용을 기다리세요.");
        }

        report.AppendLine($"나무와 NPC 길 : 흙길 가까운 나무 · 바위 {checkedTrees}개 중 줄기 자리가 길인 것 {onPath}개");
    }
}
