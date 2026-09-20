using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// 119일차: 계절 채집물 Prefab · 섬 배치 · 과일나무 · 절구
public static partial class SeasonContentBuilder
{
    // ---------------------------------------------------------------- 계절 채집물 표

    public sealed class ForageSpec
    {
        public string Id; // 채집물 ID
        public string ItemId; // 얻는 아이템
        public string Korean; // 한글 이름
        public SeasonType[] Seasons; // 나는 계절
        public string[] Biomes; // 사는 곳
        public int Count = 10; // 놓는 수
        public int Quantity = 2; // 한 곳에서 캐는 수
        public float Respawn = 120f; // 다시 나는 시간
        public float Scale = 1.8f; // 모델 크기 (아이템 모델을 키워서 쓴다)
        public float Height = 0.05f; // 바닥에서 띄우는 높이
    }

    public static readonly ForageSpec[] Forages =
    {
        // 봄
        new ForageSpec { Id = "wild_greens", ItemId = "resource_wild_greens", Korean = "산나물", Seasons = new[] { SeasonType.Spring }, Biomes = new[] { "forest", "field" }, Count = 12, Scale = 2f },
        new ForageSpec { Id = "wild_flower", ItemId = "resource_wild_flower", Korean = "들꽃", Seasons = new[] { SeasonType.Spring, SeasonType.Summer }, Biomes = new[] { "field" }, Count = 12, Scale = 2f },
        new ForageSpec { Id = "bamboo_shoot", ItemId = "food_bamboo_shoot", Korean = "죽순", Seasons = new[] { SeasonType.Spring }, Biomes = new[] { "forest" }, Count = 10, Scale = 2.2f },

        // 여름
        new ForageSpec { Id = "raspberry", ItemId = "food_raspberry", Korean = "산딸기", Seasons = new[] { SeasonType.Summer }, Biomes = new[] { "forest" }, Count = 12, Quantity = 3, Scale = 2f },
        new ForageSpec { Id = "herb_leaf", ItemId = "resource_herb_leaf", Korean = "약초 잎", Seasons = new[] { SeasonType.Summer, SeasonType.Spring }, Biomes = new[] { "swamp", "forest" }, Count = 10, Scale = 2.2f },
        new ForageSpec { Id = "bamboo", ItemId = "resource_bamboo", Korean = "대나무", Seasons = new[] { SeasonType.Summer, SeasonType.Autumn }, Biomes = new[] { "forest" }, Count = 10, Scale = 2.6f, Height = 0f },

        // 가을
        new ForageSpec { Id = "chestnut", ItemId = "food_chestnut", Korean = "밤", Seasons = new[] { SeasonType.Autumn }, Biomes = new[] { "forest" }, Count = 12, Quantity = 3, Scale = 2f },
        new ForageSpec { Id = "big_mushroom", ItemId = "food_big_mushroom", Korean = "큰 버섯", Seasons = new[] { SeasonType.Autumn }, Biomes = new[] { "forest", "swamp" }, Count = 12, Scale = 2.2f },
        new ForageSpec { Id = "acorn", ItemId = "resource_acorn", Korean = "도토리", Seasons = new[] { SeasonType.Autumn }, Biomes = new[] { "forest", "field" }, Count = 12, Quantity = 4, Scale = 2f },

        // 겨울
        new ForageSpec { Id = "dry_branch", ItemId = "resource_dry_branch", Korean = "마른 가지", Seasons = new[] { SeasonType.Winter }, Biomes = new[] { "forest", "field" }, Count = 12, Quantity = 3, Scale = 2.2f },
        new ForageSpec { Id = "ice_flower", ItemId = "resource_ice_flower", Korean = "얼음꽃", Seasons = new[] { SeasonType.Winter }, Biomes = new[] { "snow" }, Count = 8, Quantity = 1, Respawn = 180f, Scale = 2f },
        new ForageSpec { Id = "pine_cone", ItemId = "resource_pine_cone", Korean = "솔방울", Seasons = new[] { SeasonType.Winter, SeasonType.Autumn }, Biomes = new[] { "snow", "forest" }, Count = 10, Quantity = 3, Scale = 2f }
    };

    // ---------------------------------------------------------------- 과일나무 표

    public sealed class TreeSpec
    {
        public string Id; // 나무 종류 ID
        public string Korean; // 한글 이름
        public string English; // 표시 이름
        public string SaplingItemId; // 심는 데 쓰는 묘목
        public string FruitItemId; // 열리는 과일
        public string MatureModel; // 다 자란 모습
        public string FruitModel; // 열매 모습
        public SeasonType[] FruitSeasons; // 열매가 열리는 계절
        public int DaysPerStage = 5; // 한 단계 자라는 날
        public int FruitAmount = 4; // 한 번에 열리는 수
    }

    public static readonly TreeSpec[] Trees =
    {
        new TreeSpec
        {
            Id = "apple", Korean = "사과나무", English = "APPLE TREE", SaplingItemId = "resource_sapling_apple", FruitItemId = "food_apple",
            MatureModel = "tree_apple_mature", FruitModel = "tree_apple_fruit", FruitSeasons = new[] { SeasonType.Autumn }, FruitAmount = 5
        },
        new TreeSpec
        {
            Id = "plum", Korean = "자두나무", English = "PLUM TREE", SaplingItemId = "resource_sapling_plum", FruitItemId = "food_plum",
            MatureModel = "tree_plum_mature", FruitModel = "tree_plum_fruit", FruitSeasons = new[] { SeasonType.Summer }, FruitAmount = 4
        },
        new TreeSpec
        {
            Id = "persimmon", Korean = "감나무", English = "PERSIMMON TREE", SaplingItemId = "resource_sapling_persimmon", FruitItemId = "food_persimmon",
            MatureModel = "tree_persimmon_mature", FruitModel = "tree_persimmon_fruit", FruitSeasons = new[] { SeasonType.Autumn }, FruitAmount = 4
        }
    };

    public static string ForagePrefabPath(ForageSpec spec) => $"{ForagePrefabFolder}/Forage_{spec.Id}.prefab";

    public static string TreePrefabPath(TreeSpec spec) => $"{TreePrefabFolder}/FruitTree_{spec.Id}.prefab";

    public static string TreePreviewPath(TreeSpec spec) => $"{TreePrefabFolder}/FruitTreePreview_{spec.Id}.prefab";

    public static string TreeRecipePath(TreeSpec spec) => $"{BuildingFolder}/BuildRecipe_Tree{Pascal(spec.Id)}.asset";

    private static string Pascal(string text) => string.Concat(text.Split('_').Where(part => part.Length > 0).Select(part => char.ToUpperInvariant(part[0]) + part.Substring(1)));

    // ---------------------------------------------------------------- 채집물 Prefab

    private static string BuildForagePrefabs()
    {
        int made = 0;

        foreach (ForageSpec spec in Forages)
        {
            string path = ForagePrefabPath(spec);
            ItemData item = FindItem(spec.ItemId);
            GameObject model = StylizedArtAssetFactory.GetOrCreateModelPrefab(spec.ItemId, true);

            if (item == null || model == null || !CopyPrefab(StoneResourcePath, path))
            {
                continue;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(path);

            try
            {
                root.name = "Forage_" + spec.Id;
                GatherableResource resource = root.GetComponent<GatherableResource>();
                SerializedObject serialized = new SerializedObject(resource);
                serialized.FindProperty("resourceItem").objectReferenceValue = item;
                serialized.FindProperty("promptMessage").stringValue = $"LMB - {spec.Korean} 캐기";
                serialized.FindProperty("totalQuantity").intValue = spec.Quantity;
                serialized.FindProperty("quantityPerInteraction").intValue = 1;
                serialized.FindProperty("requiredToolType").intValue = (int)ToolType.None; // 맨손으로 딴다
                serialized.FindProperty("requiredToolTier").intValue = 0;
                serialized.FindProperty("respawnEnabled").boolValue = true;
                serialized.FindProperty("respawnDelay").floatValue = spec.Respawn;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                SeasonForage forage = root.GetComponent<SeasonForage>();

                if (forage == null)
                {
                    forage = root.AddComponent<SeasonForage>();
                }

                forage.EditorAssign(spec.Id, spec.Seasons);
                ReplaceVisual(root, model, spec.Scale);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                made++;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        return $"계절 채집물 Prefab {made}종 (봄 3 · 여름 3 · 가을 3 · 겨울 3)";
    }

    private static void ReplaceVisual(GameObject root, GameObject model, float scale) // 외형을 채집물 모델로 바꾼다
    {
        ContentVisualRoot visualRoot = root.GetComponentInChildren<ContentVisualRoot>(true);

        if (visualRoot != null)
        {
            UnityEngine.Object.DestroyImmediate(visualRoot, true);
        }

        Transform visual = root.transform.Find("VisualRoot") ?? root.transform.Find("Visual");

        if (visual == null)
        {
            visual = new GameObject("VisualRoot").transform;
            visual.SetParent(root.transform, false);
        }

        foreach (Transform child in visual.Cast<Transform>().ToList())
        {
            UnityEngine.Object.DestroyImmediate(child.gameObject);
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model, visual);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one * scale;
    }

    // ---------------------------------------------------------------- 섬에 놓기

    private static string PlaceForage()
    {
        Scene scene = EditorSceneManager.GetActiveScene();

        if (scene.path != ScenePath)
        {
            return "게임 Scene이 열려 있지 않아 채집물은 놓지 않았습니다.";
        }

        Terrain terrain = Terrain.activeTerrain != null ? Terrain.activeTerrain : UnityEngine.Object.FindFirstObjectByType<Terrain>();

        if (terrain == null)
        {
            return "✗ 섬 지형이 없습니다.";
        }

        GameObject root = scene.GetRootGameObjects().FirstOrDefault(item => item.name == RootName);

        if (root == null)
        {
            root = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(root, scene);
        }

        foreach (Transform child in root.transform.Cast<Transform>().ToList()) // 다시 만들 때는 비우고 새로 놓는다
        {
            UnityEngine.Object.DestroyImmediate(child.gameObject);
        }

        List<Vector3> candidates = Candidates(terrain).ToList();
        List<Vector3> used = new List<Vector3>();
        List<string> counts = new List<string>();
        int total = 0;

        foreach (ForageSpec spec in Forages)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ForagePrefabPath(spec));
            Transform group = new GameObject("Forage_" + spec.Id).transform;
            group.SetParent(root.transform, false);
            int placed = 0;

            foreach (Vector3 point in candidates)
            {
                if (placed >= spec.Count)
                {
                    break;
                }

                if (prefab == null || !MatchesBiome(spec.Biomes, terrain, point) || TooClose(used, point, 26f))
                {
                    continue;
                }

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, group);
                instance.transform.SetPositionAndRotation(point + Vector3.up * spec.Height, Quaternion.Euler(0f, (point.x * 3f + point.z) % 360f, 0f));
                instance.name = $"Forage_{spec.Id}_{placed:00}";
                WorldObjectIdentity identity = instance.GetComponent<WorldObjectIdentity>();

                if (identity == null)
                {
                    identity = instance.AddComponent<WorldObjectIdentity>();
                }

                identity.AssignWorldObjectId($"season_forage_{spec.Id}_{placed:00}");
                EditorUtility.SetDirty(identity);
                GameObjectUtility.SetStaticEditorFlags(instance, 0);
                used.Add(point);
                placed++;
                total++;
            }

            counts.Add($"{spec.Korean} {placed}");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        return $"섬에 계절 채집물 {total}곳 ({string.Join(" · ", counts)})";
    }

    // 섬 위 후보 자리 (항상 같은 순서, 야생동물보다 촘촘하게)
    private static IEnumerable<Vector3> Candidates(Terrain terrain)
    {
        for (int ring = 0; ring < 22; ring++)
        {
            float radius = 95f + ring * 28f;
            int steps = 18 + ring * 4;

            for (int step = 0; step < steps; step++)
            {
                float angle = (step + ring * 0.29f) / steps * Mathf.PI * 2f;
                Vector3 point = new Vector3(Mathf.Sin(angle) * radius, 0f, Mathf.Cos(angle) * radius);
                point.y = terrain.SampleHeight(point) + terrain.transform.position.y;
                yield return point;
            }
        }
    }

    private static bool MatchesBiome(string[] biomes, Terrain terrain, Vector3 point)
    {
        if (biomes == null || biomes.Length == 0)
        {
            return false;
        }

        Vector3 size = terrain.terrainData.size;
        Vector3 local = point - terrain.transform.position;
        float steep = terrain.terrainData.GetSteepness(Mathf.Clamp01(local.x / size.x), Mathf.Clamp01(local.z / size.z));

        if (point.y < 2.8f || steep > 24f || !IslandNatureBuilder.IsClear(point.x, point.z, 10f))
        {
            return false;
        }

        float forest = IslandNatureBuilder.ForestNoise(point.x, point.z);

        foreach (string biome in biomes)
        {
            switch (biome)
            {
                case "forest":
                    if (forest > 0.48f && point.y < 30f) return true;
                    break;
                case "field":
                    if (forest < 0.4f && point.y < 22f) return true;
                    break;
                case "snow":
                    if (ZoneDistance("snow", point) < 230f) return true;
                    break;
                case "swamp":
                    if (ZoneDistance("swamp", point) < 190f) return true;
                    break;
                case "shore":
                    if (point.y < 6.5f) return true;
                    break;
            }
        }

        return false;
    }

    private static float ZoneDistance(string zoneId, Vector3 point)
    {
        IslandZoneLayout.Site site = IslandZoneLayout.Get(zoneId);

        if (site == null)
        {
            return float.PositiveInfinity;
        }

        Vector2 center = new Vector2(site.Offset.x + site.PadCenter.x, site.Offset.y + site.PadCenter.y);
        return Vector2.Distance(new Vector2(point.x, point.z), center);
    }

    private static bool TooClose(List<Vector3> used, Vector3 point, float gap)
    {
        return used.Any(item => Vector2.Distance(new Vector2(item.x, item.z), new Vector2(point.x, point.z)) < gap);
    }

    // ---------------------------------------------------------------- 과일나무

    private static string BuildFruitTrees()
    {
        int made = 0;

        foreach (TreeSpec spec in Trees)
        {
            if (BuildFruitTreePrefab(spec) && BuildFruitTreePreview(spec))
            {
                CreateTreeRecipe(spec);
                made++;
            }
        }

        AssetDatabase.SaveAssets();
        return $"과일나무 {made}종 (묘목 심기 · 15일 자람 · 제철마다 열매) · {string.Join(" · ", Trees.Select(spec => $"{spec.Korean} {ConnectBuildMenu(AssetDatabase.LoadAssetAtPath<BuildRecipeData>(TreeRecipePath(spec)))}"))}";
    }

    private static bool BuildFruitTreePrefab(TreeSpec spec)
    {
        ItemData fruit = FindItem(spec.FruitItemId);
        GameObject sapling = StylizedArtAssetFactory.GetOrCreateModelPrefab("tree_sapling", true);
        GameObject young = StylizedArtAssetFactory.GetOrCreateModelPrefab("tree_young", true);
        GameObject mature = StylizedArtAssetFactory.GetOrCreateModelPrefab(spec.MatureModel, true);
        GameObject fruitModel = StylizedArtAssetFactory.GetOrCreateModelPrefab(spec.FruitModel, true);

        if (fruit == null || sapling == null || young == null || mature == null || fruitModel == null)
        {
            return false;
        }

        GameObject root = new GameObject("FruitTree_" + spec.Id);

        try
        {
            CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
            capsule.radius = 0.5f;
            capsule.height = 2.6f;
            capsule.center = new Vector3(0f, 1.3f, 0f);

            GameObject[] stages = new GameObject[3];
            stages[0] = AddStage(root.transform, "Stage0_Sapling", sapling);
            stages[1] = AddStage(root.transform, "Stage1_Young", young);
            stages[2] = AddStage(root.transform, "Stage2_Mature", mature);
            GameObject fruitObject = AddStage(root.transform, "FruitVisual", fruitModel);

            FruitTree tree = root.AddComponent<FruitTree>();
            tree.EditorAssign(spec.Id, spec.Korean, fruit, spec.DaysPerStage, spec.FruitSeasons, spec.FruitAmount, stages, fruitObject);
            SerializedObject serialized = new SerializedObject(tree);
            serialized.FindProperty("promptMessage").stringValue = $"{spec.Korean} : 자라는 중";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, TreePrefabPath(spec));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }

        return AssetDatabase.LoadAssetAtPath<GameObject>(TreePrefabPath(spec)) != null;
    }

    private static GameObject AddStage(Transform parent, string name, GameObject model) // 자라는 단계 하나
    {
        GameObject holder = new GameObject(name);
        holder.transform.SetParent(parent, false);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model, holder.transform);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        return holder;
    }

    private static bool BuildFruitTreePreview(TreeSpec spec)
    {
        GameObject sapling = StylizedArtAssetFactory.GetOrCreateModelPrefab("tree_sapling", true);

        if (sapling == null)
        {
            return false;
        }

        GameObject root = new GameObject("FruitTreePreview_" + spec.Id);

        try
        {
            AddStage(root.transform, "Preview", sapling);
            PrefabUtility.SaveAsPrefabAsset(root, TreePreviewPath(spec));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }

        return AssetDatabase.LoadAssetAtPath<GameObject>(TreePreviewPath(spec)) != null;
    }

    private static void CreateTreeRecipe(TreeSpec spec)
    {
        BuildRecipeData recipe = LoadOrCreate<BuildRecipeData>(TreeRecipePath(spec));
        SerializedObject serialized = new SerializedObject(recipe);
        serialized.FindProperty("recipeId").stringValue = "structure_tree_" + spec.Id;
        serialized.FindProperty("displayName").stringValue = spec.English;
        serialized.FindProperty("structureType").intValue = 4; // 기능성 가구
        serialized.FindProperty("allowGroundPlacement").boolValue = true;
        serialized.FindProperty("placementType").intValue = 2;
        serialized.FindProperty("rotationStep").floatValue = 90f;
        serialized.FindProperty("placedPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(TreePrefabPath(spec));
        serialized.FindProperty("previewPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(TreePreviewPath(spec));
        serialized.FindProperty("placementCheckCenter").vector3Value = new Vector3(0f, 0.6f, 0f);
        serialized.FindProperty("placementCheckHalfExtents").vector3Value = new Vector3(0.8f, 0.6f, 0.8f);
        serialized.FindProperty("maximumSlopeAngle").floatValue = 20f;
        serialized.FindProperty("maximumHeightDifference").floatValue = 0.25f;
        serialized.FindProperty("demolitionRefundRatio").floatValue = 0f; // 뽑으면 묘목은 돌아오지 않는다
        SerializedProperty ingredients = serialized.FindProperty("ingredients");
        ingredients.arraySize = 1;
        ingredients.GetArrayElementAtIndex(0).FindPropertyRelative("itemData").objectReferenceValue = FindItem(spec.SaplingItemId);
        ingredients.GetArrayElementAtIndex(0).FindPropertyRelative("amount").intValue = 1;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(recipe);
    }

    // ---------------------------------------------------------------- 절구

    private static string BuildMortar(List<CookingRecipeData> cooks)
    {
        if (!CopyPrefab(CampfirePlacedPath, MortarPlacedPath) || !CopyPrefab(CampfirePreviewPath, MortarPreviewPath))
        {
            return "✗ 절구 Prefab을 만들지 못했습니다.";
        }

        GameObject model = StylizedArtAssetFactory.GetOrCreateModelPrefab("build_mortar", true);
        List<CookingRecipeData> grinding = cooks.Where(recipe => recipe != null && recipe.RequiredStation == CookingStationTier.Mortar).ToList();
        GameObject placed = PrefabUtility.LoadPrefabContents(MortarPlacedPath);

        try
        {
            placed.name = "MortarPlaced";

            foreach (Component component in placed.GetComponentsInChildren<Light>(true).Cast<Component>().Concat(placed.GetComponentsInChildren<ParticleSystem>(true)))
            {
                UnityEngine.Object.DestroyImmediate(component, true);
            }

            CampfireCookingStation station = placed.GetComponentInChildren<CampfireCookingStation>(true);

            if (station == null)
            {
                return "✗ 절구에 작업대 부품이 없습니다.";
            }

            SerializedObject serialized = new SerializedObject(station);
            serialized.FindProperty("stationTier").intValue = (int)CookingStationTier.Mortar;
            serialized.FindProperty("slotCount").intValue = 1;
            serialized.FindProperty("maxBatchQuantity").intValue = 3;
            serialized.FindProperty("fuelItem").objectReferenceValue = null; // 절구는 연료가 없다
            serialized.FindProperty("fuelAmount").intValue = 1;
            serialized.FindProperty("promptMessage").stringValue = "F - GRIND";
            serialized.FindProperty("slots").arraySize = 0;
            SerializedProperty list = serialized.FindProperty("recipes");
            list.arraySize = grinding.Count;

            for (int index = 0; index < grinding.Count; index++)
            {
                list.GetArrayElementAtIndex(index).objectReferenceValue = grinding[index];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            ReplaceVisual(placed, model, 1f);
            PrefabUtility.SaveAsPrefabAsset(placed, MortarPlacedPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(placed);
        }

        GameObject preview = PrefabUtility.LoadPrefabContents(MortarPreviewPath);

        try
        {
            preview.name = "MortarPreview";
            ReplaceVisual(preview, model, 1f);
            PrefabUtility.SaveAsPrefabAsset(preview, MortarPreviewPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(preview);
        }

        BuildRecipeData recipe = LoadOrCreate<BuildRecipeData>(MortarRecipePath);
        SerializedObject recipeObject = new SerializedObject(recipe);
        recipeObject.FindProperty("recipeId").stringValue = "structure_mortar";
        recipeObject.FindProperty("displayName").stringValue = "MORTAR";
        recipeObject.FindProperty("structureType").intValue = 4;
        recipeObject.FindProperty("allowGroundPlacement").boolValue = true;
        recipeObject.FindProperty("placementType").intValue = 2;
        recipeObject.FindProperty("rotationStep").floatValue = 45f;
        recipeObject.FindProperty("placedPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(MortarPlacedPath);
        recipeObject.FindProperty("previewPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(MortarPreviewPath);
        recipeObject.FindProperty("placementCheckCenter").vector3Value = new Vector3(0f, 0.4f, 0f);
        recipeObject.FindProperty("placementCheckHalfExtents").vector3Value = new Vector3(0.6f, 0.4f, 0.6f);
        recipeObject.FindProperty("maximumSlopeAngle").floatValue = 20f;
        recipeObject.FindProperty("maximumHeightDifference").floatValue = 0.15f;
        recipeObject.FindProperty("demolitionRefundRatio").floatValue = 0.5f;
        SerializedProperty ingredients = recipeObject.FindProperty("ingredients");
        ingredients.arraySize = 2;
        ingredients.GetArrayElementAtIndex(0).FindPropertyRelative("itemData").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ItemData>(StonePath);
        ingredients.GetArrayElementAtIndex(0).FindPropertyRelative("amount").intValue = 12;
        ingredients.GetArrayElementAtIndex(1).FindPropertyRelative("itemData").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ItemData>(WoodPath);
        ingredients.GetArrayElementAtIndex(1).FindPropertyRelative("amount").intValue = 4;
        recipeObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(recipe);
        AssetDatabase.SaveAssets();
        return $"절구 건축물 (돌 12 + 나무 4) · 사료 제조법 {grinding.Count}개 · {ConnectBuildMenu(recipe)}";
    }

    private static string ConnectBuildMenu(BuildRecipeData recipe) // 건축 목록 · 건축물 저장 목록에 넣기
    {
        if (recipe == null)
        {
            return "✗ 건축 데이터 없음";
        }

        if (EditorSceneManager.GetActiveScene().path != ScenePath)
        {
            return "건축 목록 연결은 게임 Scene에서";
        }

        BuildPlacementController controller = UnityEngine.Object.FindFirstObjectByType<BuildPlacementController>(FindObjectsInactive.Include);
        PlacedStructureSaveBridge bridge = UnityEngine.Object.FindFirstObjectByType<PlacedStructureSaveBridge>(FindObjectsInactive.Include);
        int added = 0;

        foreach (UnityEngine.Object target in new UnityEngine.Object[] { controller, bridge })
        {
            if (target == null)
            {
                continue;
            }

            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty list = serialized.FindProperty("buildRecipes");
            bool found = false;

            for (int index = 0; index < list.arraySize; index++)
            {
                found |= list.GetArrayElementAtIndex(index).objectReferenceValue == recipe;
            }

            if (found)
            {
                continue;
            }

            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = recipe;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
            added++;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        return added > 0 ? "건축 목록 추가" : "건축 목록에 있음";
    }

    // ---------------------------------------------------------------- 계절 알림

    private static string ConnectSeasonNotice()
    {
        Scene scene = EditorSceneManager.GetActiveScene();

        if (scene.path != ScenePath)
        {
            return "게임 Scene이 열려 있지 않아 계절 알림은 연결하지 않았습니다.";
        }

        SeasonNoticeManager manager = UnityEngine.Object.FindFirstObjectByType<SeasonNoticeManager>(FindObjectsInactive.Include);

        if (manager == null)
        {
            GameObject holder = new GameObject("SeasonNoticeManager");
            SceneManager.MoveGameObjectToScene(holder, scene);
            manager = holder.AddComponent<SeasonNoticeManager>();
            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(scene);
            return "계절 알림 관리자 추가";
        }

        return "계절 알림 관리자 있음";
    }

    // ---------------------------------------------------------------- 검증

    public static string Validate(out int errorCount)
    {
        StringBuilder report = new StringBuilder("[계절 채집 · 과일나무 검증]\n");
        int errors = 0;

        void Error(string message)
        {
            errors++;
            report.AppendLine("✗ " + message);
        }

        foreach (ItemSpec spec in Items)
        {
            ItemData item = FindItem(spec.Id);

            if (item == null || item.KoreanName != spec.Korean)
            {
                Error($"{spec.Id} 아이템이 없거나 한글 이름이 다릅니다.");
            }
        }

        foreach (ForageSpec spec in Forages)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ForagePrefabPath(spec));
            GatherableResource resource = prefab != null ? prefab.GetComponent<GatherableResource>() : null;
            SeasonForage forage = prefab != null ? prefab.GetComponent<SeasonForage>() : null;

            if (resource == null || forage == null || forage.Seasons.Length != spec.Seasons.Length)
            {
                Error($"{spec.Id} 채집물 Prefab이 없거나 계절 설정이 다릅니다.");
            }
        }

        foreach (TreeSpec spec in Trees)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TreePrefabPath(spec));
            FruitTree tree = prefab != null ? prefab.GetComponent<FruitTree>() : null;
            BuildRecipeData recipe = AssetDatabase.LoadAssetAtPath<BuildRecipeData>(TreeRecipePath(spec));

            if (tree == null || tree.FruitItem == null || recipe == null || recipe.PlacedPrefab == null)
            {
                Error($"{spec.Id} 과일나무 Prefab · 건축 데이터가 없습니다.");
            }
        }

        foreach (CookSpec spec in Cooks)
        {
            CookingRecipeData recipe = AssetDatabase.LoadAssetAtPath<CookingRecipeData>($"{CookingFolder}/{spec.Asset}.asset");

            if (recipe == null || recipe.ResultItem == null || recipe.RequiredStation != spec.Station)
            {
                Error($"{spec.Id} 요리 · 제조법이 없거나 시설이 다릅니다.");
            }
        }

        foreach (CraftSpec spec in Crafts)
        {
            if (AssetDatabase.LoadAssetAtPath<CraftingRecipeData>($"{CraftingFolder}/{spec.Asset}.asset") == null)
            {
                Error($"{spec.Id} 제작법이 없습니다.");
            }
        }

        foreach (string cropId in new[] { "crop_corn", "crop_cabbage", "crop_sweet_potato" })
        {
            CropData crop = AssetDatabase.FindAssets("t:CropData", new[] { "Assets/_ProjectU/Data" })
                .Select(guid => AssetDatabase.LoadAssetAtPath<CropData>(AssetDatabase.GUIDToAssetPath(guid)))
                .FirstOrDefault(asset => asset != null && asset.CropId == cropId);

            if (crop == null || crop.SeedItem == null || crop.HarvestItem == null || crop.GrowthStages.Count < 4)
            {
                Error($"{cropId} 작물 데이터가 없거나 단계가 모자랍니다.");
            }
        }

        GameObject mortar = AssetDatabase.LoadAssetAtPath<GameObject>(MortarPlacedPath);
        CampfireCookingStation mortarStation = mortar != null ? mortar.GetComponentInChildren<CampfireCookingStation>(true) : null;

        if (mortarStation == null || mortarStation.Tier != CookingStationTier.Mortar || mortarStation.Recipes.Count != 2)
        {
            Error("절구 Prefab이 없거나 사료 제조법이 2개가 아닙니다.");
        }

        if (EditorSceneManager.GetActiveScene().path == ScenePath)
        {
            GameObject root = EditorSceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(item => item.name == RootName);
            SeasonForage[] placed = root != null ? root.GetComponentsInChildren<SeasonForage>(true) : new SeasonForage[0];
            int expected = Forages.Sum(spec => spec.Count);

            if (root == null || placed.Length < expected * 0.8f)
            {
                Error($"섬에 놓인 계절 채집물이 {placed.Length}곳입니다 ({expected}곳 목표).");
            }

            foreach (ForageSpec spec in Forages.Where(spec => !placed.Any(item => item.ForageId == spec.Id)))
            {
                Error($"{spec.Korean}이(가) 섬에 하나도 없습니다.");
            }

            foreach (SeasonType season in new[] { SeasonType.Spring, SeasonType.Summer, SeasonType.Autumn, SeasonType.Winter })
            {
                int count = placed.Count(item => item.IsInSeason(season));

                if (count < 12)
                {
                    Error($"{season} 에 나는 채집물이 {count}곳뿐입니다 (12곳 이상 목표).");
                }
            }

            if (UnityEngine.Object.FindFirstObjectByType<SeasonNoticeManager>(FindObjectsInactive.Include) == null)
            {
                Error("Scene에 계절 알림 관리자가 없습니다.");
            }
        }

        report.AppendLine($"계절 채집물 {Forages.Length}종 · 과일나무 {Trees.Length}종 · 아이템 {Items.Length}개 · 요리 {Cooks.Length}개 · 제작법 {Crafts.Length}개");
        report.AppendLine($"결과 : 오류 {errors}개");
        errorCount = errors;
        return report.ToString();
    }
}
