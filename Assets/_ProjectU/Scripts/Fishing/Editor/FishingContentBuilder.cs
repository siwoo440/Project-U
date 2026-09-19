using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

// 82일차: 낚시 콘텐츠(낚싯대·미끼·물고기 데이터, 연못, 낚시 조작)를 한 번에 만들고
// 아이템 DB, 월드 아이템 Registry, GameDataRegistry, 현재 게임 Scene에 연결한다.
// 여러 번 실행해도 같은 Asset·오브젝트를 갱신하며 중복 생성하지 않는다.
// 83일차: 출현 조건·보상 참조, 잡은 기록, 끌어올리기 미니게임 HUD 연결과 검증 추가
public static class FishingContentBuilder
{
    public const string BuildMenuRoot = "Tools/Project U/Build Content/";
    private const string DialogTitle = "Project U 낚시 콘텐츠";

    private const string ItemFolder = "Assets/_ProjectU/Data/Items/Day82";
    private const string FishFolder = "Assets/_ProjectU/Data/Fishing/Fish";
    private const string RodDataPath = "Assets/_ProjectU/Data/Fishing/FishingRod_Basic.asset";
    private const string RulesPath = "Assets/_ProjectU/Data/Fishing/FishingRules_Default.asset";
    private const string CraftingFolder = "Assets/_ProjectU/Data/Crafting/Day82";
    private const string PickupFolder = "Assets/_ProjectU/Prefabs/Items/Day82";
    private const string PickupTemplatePath = "Assets/_ProjectU/Prefabs/Items/Day71/WildMushroomPickup.prefab";
    private const string ItemDatabasePath = "Assets/_ProjectU/Data/Databases/ItemDatabase.asset";
    private const string PickupRegistryPath = "Assets/_ProjectU/Prefabs/Items/Day75/WorldItemPickupRegistry_Day75.asset";
    private const string RegistryPath = "Assets/_ProjectU/Data/Registry/GameDataRegistry.asset";
    private const string FarmingRulesPath = "Assets/_ProjectU/Data/Farming/FarmingRules_Default.asset";
    private const string WoodItemPath = "Assets/_ProjectU/Data/Items/ItemData_Wood.asset";
    private const string PlantFiberItemPath = "Assets/_ProjectU/Data/Items/Day71/ItemData_PlantFiber.asset";
    private const string LineMaterialPath = "Assets/_ProjectU/Art/Generated/Materials/M_FishingLine.mat";
    private const string PondRootName = "=== Day82 Fishing Pond ===";
    private const string CastTargetName = "FishingCastTarget";
    private const string LineName = "FishingLine";
    private const string RodVisualName = "FishingRodVisual";
    private const string RodTipName = "RodTip";
    private const string EnvironmentRootName = "=== Stylized Environment ===";
    private const string FarmStarterRootName = "=== Day79 Farming Starter ===";

    private sealed class ItemSpec
    {
        public string AssetName;
        public string Id;
        public string DisplayName;
        public string Description;
        public ItemCategory Category;
        public ToolType Tool;
        public int MaximumStack;
        public string ModelId;
        public string PickupName;
        public float PickupScale = 1f;
        public bool LyingTool;
        public int StarterQuantity;
    }

    private static readonly ItemSpec[] ItemSpecs =
    {
        new ItemSpec
        {
            AssetName = "ItemData_FishingRodBasic", Id = "tool_fishing_rod_basic", DisplayName = "FISHING ROD",
            Description = "A basic rod for fishing at ponds and rivers.",
            Category = ItemCategory.Tool, Tool = ToolType.FishingRod, MaximumStack = 1,
            ModelId = "tool_fishing_rod", PickupName = "FishingRodPickup", PickupScale = 2.6f, LyingTool = true, StarterQuantity = 1
        },
        new ItemSpec
        {
            AssetName = "ItemData_WormBait", Id = "resource_worm_bait", DisplayName = "WORM BAIT",
            Description = "Basic bait that freshwater fish love. Sometimes found while tilling soil.",
            Category = ItemCategory.CraftingMaterial, MaximumStack = 99,
            ModelId = "item_worm_bait", PickupName = "WormBaitPickup", PickupScale = 1.1f, StarterQuantity = 10
        },
        Fish("FishCrucian", "resource_fish_crucian", "CRUCIAN CARP", "A common pond fish.", "item_fish_crucian"),
        Fish("FishTrout", "resource_fish_trout", "TROUT", "A lively fish that bites in the morning and evening.", "item_fish_trout"),
        Fish("FishCatfish", "resource_fish_catfish", "CATFISH", "A whiskered fish that comes out in the evening.", "item_fish_catfish"),
        Fish("FishSmelt", "resource_fish_smelt", "SMELT", "A small fish that appears in winter.", "item_fish_smelt"),
        Fish("FishGoldenCarp", "resource_fish_golden_carp", "GOLDEN CARP", "A rare golden fish seen on rainy days.", "item_fish_golden_carp")
    };

    private static ItemSpec Fish(string name, string id, string displayName, string description, string modelId)
    {
        return new ItemSpec
        {
            AssetName = "ItemData_" + name, Id = id, DisplayName = displayName, Description = description,
            Category = ItemCategory.CraftingMaterial, MaximumStack = 20, ModelId = modelId,
            PickupName = name + "Pickup", PickupScale = 1.3f
        };
    }

    private sealed class FishSpec
    {
        public string AssetName;
        public string Id;
        public string DisplayName;
        public string Description;
        public FishRarity Rarity;
        public string ItemId;
        public WaterBodyType[] Waters;
        public FishTimeWindow Time;
        public SeasonType[] Seasons;
        public WeatherType[] Weathers;
        public float Weight;
        public float Difficulty;
        public int SuccessCount;
    }

    // 기획서 9.3 물고기 출현 조건(지역·시간대·날씨·계절·낚싯대 등급) 기준 대표 물고기
    private static readonly FishSpec[] FishSpecs =
    {
        new FishSpec
        {
            AssetName = "FishData_Crucian", Id = "fish_crucian", DisplayName = "CRUCIAN CARP", Description = "Common in ponds from spring to autumn.",
            Rarity = FishRarity.Common, ItemId = "resource_fish_crucian", Waters = new[] { WaterBodyType.Lake, WaterBodyType.River },
            Time = FishTimeWindow.AnyTime, Seasons = new[] { SeasonType.Spring, SeasonType.Summer, SeasonType.Autumn },
            Weathers = new WeatherType[0], Weight = 50f, Difficulty = 0.2f, SuccessCount = 3
        },
        new FishSpec
        {
            AssetName = "FishData_Trout", Id = "fish_trout", DisplayName = "TROUT", Description = "Bites in the morning and evening of spring and autumn.",
            Rarity = FishRarity.Uncommon, ItemId = "resource_fish_trout", Waters = new[] { WaterBodyType.River, WaterBodyType.Lake },
            Time = FishTimeWindow.Morning | FishTimeWindow.Evening, Seasons = new[] { SeasonType.Spring, SeasonType.Autumn },
            Weathers = new WeatherType[0], Weight = 25f, Difficulty = 0.45f, SuccessCount = 4
        },
        new FishSpec
        {
            AssetName = "FishData_Catfish", Id = "fish_catfish", DisplayName = "CATFISH", Description = "Appears in the evening and at night in summer and autumn.",
            Rarity = FishRarity.Uncommon, ItemId = "resource_fish_catfish", Waters = new[] { WaterBodyType.Lake, WaterBodyType.River },
            Time = FishTimeWindow.Evening | FishTimeWindow.Night, Seasons = new[] { SeasonType.Summer, SeasonType.Autumn },
            Weathers = new WeatherType[0], Weight = 20f, Difficulty = 0.5f, SuccessCount = 4
        },
        new FishSpec
        {
            AssetName = "FishData_Smelt", Id = "fish_smelt", DisplayName = "SMELT", Description = "A winter fish of cold lakes and ice holes.",
            Rarity = FishRarity.Common, ItemId = "resource_fish_smelt", Waters = new[] { WaterBodyType.Lake, WaterBodyType.IceHole },
            Time = FishTimeWindow.AnyTime, Seasons = new[] { SeasonType.Winter },
            Weathers = new WeatherType[0], Weight = 45f, Difficulty = 0.25f, SuccessCount = 3
        },
        new FishSpec
        {
            AssetName = "FishData_GoldenCarp", Id = "fish_golden_carp", DisplayName = "GOLDEN CARP", Description = "A rare fish that rises on rainy and stormy days.",
            Rarity = FishRarity.Rare, ItemId = "resource_fish_golden_carp", Waters = new[] { WaterBodyType.Lake },
            Time = FishTimeWindow.AnyTime, Seasons = new SeasonType[0],
            Weathers = new[] { WeatherType.Rain, WeatherType.Storm }, Weight = 6f, Difficulty = 0.75f, SuccessCount = 6
        }
    };

    // ---------------------------------------------------------------- 전체 생성

    public static string BuildAll()
    {
        StringBuilder report = new StringBuilder("[낚시 콘텐츠 생성]\n");

        try
        {
            EnsureFolders();

            EditorUtility.DisplayProgressBar(DialogTitle, "저폴리 모델", 0.05f);
            report.AppendLine($"낚시 모델 {GenerateModels()}개 생성·갱신");

            EditorUtility.DisplayProgressBar(DialogTitle, "아이템·낚싯대·물고기 데이터", 0.2f);
            Dictionary<string, ItemData> items = new Dictionary<string, ItemData>();

            foreach (ItemSpec spec in ItemSpecs)
            {
                items[spec.Id] = CreateOrUpdateItem(spec);
            }

            FishingRodData rod = CreateOrUpdateRod(items["tool_fishing_rod_basic"]);
            FishingRulesData rules = CreateOrUpdateRules(rod, items["resource_worm_bait"]);
            int fishCount = 0;

            foreach (FishSpec spec in FishSpecs)
            {
                CreateOrUpdateFish(spec, items[spec.ItemId]);
                fishCount++;
            }

            report.AppendLine($"아이템 {items.Count}개 / 낚싯대 데이터 1개 / 물고기 데이터 {fishCount}개 / 낚시 규칙 {rules.RulesId}");

            CraftingRecipeData rodRecipe = CreateOrUpdateRodRecipe(items["tool_fishing_rod_basic"], report);

            EditorUtility.DisplayProgressBar(DialogTitle, "월드 아이템 Prefab", 0.4f);
            Dictionary<string, WorldItemPickup> pickups = new Dictionary<string, WorldItemPickup>();

            foreach (ItemSpec spec in ItemSpecs)
            {
                WorldItemPickup pickup = CreateOrUpdatePickup(spec, items[spec.Id], report);

                if (pickup != null)
                {
                    pickups[spec.Id] = pickup;
                }
            }

            report.AppendLine($"월드 아이템 Prefab {pickups.Count}개");
            report.AppendLine(RegisterItems(items, pickups));
            report.AppendLine(ConnectTillingBait(items["resource_worm_bait"]));
            AssetDatabase.SaveAssets();

            GameDataRegistryEditor.CreateOrRefreshDefaultRegistry();
            report.AppendLine("GameDataRegistry 자동 수집 완료");

            EditorUtility.DisplayProgressBar(DialogTitle, "게임 Scene 연결", 0.7f);
            report.AppendLine(WireScene(rules, rodRecipe, pickups));
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
        }

        report.AppendLine();
        report.Append(Validate(out _));
        return report.ToString();
    }

    private static void EnsureFolders()
    {
        StylizedArtAssetFactory.EnsureFolder(ItemFolder);
        StylizedArtAssetFactory.EnsureFolder(FishFolder);
        StylizedArtAssetFactory.EnsureFolder(CraftingFolder);
        StylizedArtAssetFactory.EnsureFolder(PickupFolder);
        StylizedArtAssetFactory.EnsureFolder(StylizedArtAssetFactory.MaterialFolder);
    }

    private static int GenerateModels()
    {
        int count = 0;

        foreach (string modelId in StylizedModelLibrary.Catalog.Keys)
        {
            bool isFishingModel = modelId.StartsWith("item_fish_")
                || modelId == "tool_fishing_rod"
                || modelId == "fx_fishing_bobber"
                || modelId == "fx_cast_ring"
                || modelId == "item_worm_bait"
                || modelId == "prop_pond";

            if (isFishingModel && StylizedArtAssetFactory.GetOrCreateModelPrefab(modelId, true) != null)
            {
                count++;
            }
        }

        return count;
    }

    // ---------------------------------------------------------------- 데이터

    private static TAsset LoadOrCreateAsset<TAsset>(string path) where TAsset : ScriptableObject
    {
        TAsset asset = AssetDatabase.LoadAssetAtPath<TAsset>(path);

        if (asset != null)
        {
            return asset;
        }

        asset = ScriptableObject.CreateInstance<TAsset>();
        asset.name = Path.GetFileNameWithoutExtension(path);
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static ItemData CreateOrUpdateItem(ItemSpec spec)
    {
        ItemData item = LoadOrCreateAsset<ItemData>($"{ItemFolder}/{spec.AssetName}.asset");
        SerializedObject serialized = new SerializedObject(item);
        serialized.FindProperty("itemId").stringValue = spec.Id;
        serialized.FindProperty("displayName").stringValue = spec.DisplayName;
        serialized.FindProperty("description").stringValue = spec.Description;
        serialized.FindProperty("itemCategory").intValue = (int)spec.Category;
        serialized.FindProperty("toolType").intValue = (int)spec.Tool;
        serialized.FindProperty("maximumStack").intValue = spec.MaximumStack;
        serialized.FindProperty("weaponAttackType").intValue = (int)WeaponAttackType.None;
        serialized.FindProperty("baseDamage").floatValue = 0f;
        serialized.FindProperty("staminaCost").floatValue = 0f;
        serialized.FindProperty("impactForce").floatValue = 0f;
        serialized.FindProperty("meleeComboData").objectReferenceValue = null;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(item);
        return item;
    }

    private static FishingRodData CreateOrUpdateRod(ItemData rodItem)
    {
        FishingRodData rod = LoadOrCreateAsset<FishingRodData>(RodDataPath);
        SerializedObject serialized = new SerializedObject(rod);
        serialized.FindProperty("rodItem").objectReferenceValue = rodItem;
        serialized.FindProperty("tier").intValue = 1;
        serialized.FindProperty("castDistance").floatValue = 4f;
        serialized.FindProperty("biteTimeMultiplier").floatValue = 1f;
        serialized.FindProperty("inputTimeBonus").floatValue = 0f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(rod);
        return rod;
    }

    private static FishingRulesData CreateOrUpdateRules(FishingRodData rod, ItemData bait)
    {
        FishingRulesData rules = LoadOrCreateAsset<FishingRulesData>(RulesPath);
        SerializedObject serialized = new SerializedObject(rules);
        serialized.FindProperty("rulesId").stringValue = "fishing_rules_default";
        SerializedProperty rods = serialized.FindProperty("rods");
        rods.arraySize = 1;
        rods.GetArrayElementAtIndex(0).objectReferenceValue = rod;
        serialized.FindProperty("baitItem").objectReferenceValue = bait;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(rules);
        return rules;
    }

    private static void CreateOrUpdateFish(FishSpec spec, ItemData item)
    {
        FishData fish = LoadOrCreateAsset<FishData>($"{FishFolder}/{spec.AssetName}.asset");
        SerializedObject serialized = new SerializedObject(fish);
        serialized.FindProperty("fishId").stringValue = spec.Id;
        serialized.FindProperty("displayName").stringValue = spec.DisplayName;
        serialized.FindProperty("description").stringValue = spec.Description;
        serialized.FindProperty("rarity").intValue = (int)spec.Rarity;
        serialized.FindProperty("resultItem").objectReferenceValue = item;
        serialized.FindProperty("timeWindow").intValue = (int)spec.Time;
        serialized.FindProperty("requiredRodTier").intValue = 1;
        serialized.FindProperty("spawnWeight").floatValue = spec.Weight;
        serialized.FindProperty("difficulty").floatValue = spec.Difficulty;
        serialized.FindProperty("requiredSuccessCount").intValue = spec.SuccessCount;
        AssignEnumArray(serialized.FindProperty("waterBodies"), spec.Waters, value => (int)value);
        AssignEnumArray(serialized.FindProperty("seasons"), spec.Seasons, value => (int)value);
        AssignEnumArray(serialized.FindProperty("weathers"), spec.Weathers, value => (int)value);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(fish);
    }

    private static void AssignEnumArray<TEnum>(SerializedProperty property, TEnum[] values, System.Func<TEnum, int> toInt)
    {
        property.arraySize = values.Length;

        for (int index = 0; index < values.Length; index++)
        {
            property.GetArrayElementAtIndex(index).intValue = toInt(values[index]);
        }
    }

    private static CraftingRecipeData CreateOrUpdateRodRecipe(ItemData rodItem, StringBuilder report)
    {
        ItemData wood = AssetDatabase.LoadAssetAtPath<ItemData>(WoodItemPath);
        ItemData fiber = AssetDatabase.LoadAssetAtPath<ItemData>(PlantFiberItemPath);

        if (wood == null || fiber == null)
        {
            report.AppendLine("[경고] 나무·식물 섬유 아이템을 찾지 못해 낚싯대 제작법을 만들지 않았습니다.");
            return null;
        }

        CraftingRecipeData recipe = LoadOrCreateAsset<CraftingRecipeData>($"{CraftingFolder}/CraftingRecipe_FishingRodBasic.asset");
        SerializedObject serialized = new SerializedObject(recipe);
        serialized.FindProperty("recipeId").stringValue = "recipe_fishing_rod_basic";
        serialized.FindProperty("displayName").stringValue = "FISHING ROD";
        serialized.FindProperty("requiredFacility").intValue = (int)CraftingFacilityType.Workbench;
        serialized.FindProperty("unlockType").intValue = (int)CraftingUnlockType.Default;
        serialized.FindProperty("unlockId").stringValue = string.Empty;
        serialized.FindProperty("resultItem").objectReferenceValue = rodItem;
        serialized.FindProperty("resultQuantity").intValue = 1;

        // 기획서 조합법의 목재 판자·나무 막대기는 아직 없어 나무와 식물 섬유(낚싯줄)로 대체
        SerializedProperty ingredients = serialized.FindProperty("ingredients");
        ingredients.arraySize = 2;
        ingredients.GetArrayElementAtIndex(0).FindPropertyRelative("itemData").objectReferenceValue = wood;
        ingredients.GetArrayElementAtIndex(0).FindPropertyRelative("amount").intValue = 2;
        ingredients.GetArrayElementAtIndex(1).FindPropertyRelative("itemData").objectReferenceValue = fiber;
        ingredients.GetArrayElementAtIndex(1).FindPropertyRelative("amount").intValue = 3;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(recipe);
        report.AppendLine("작업대 제작법 : 낚싯대 (나무 2 + 식물 섬유 3)");
        return recipe;
    }

    private static string ConnectTillingBait(ItemData bait)
    {
        FarmingRulesData farmingRules = AssetDatabase.LoadAssetAtPath<FarmingRulesData>(FarmingRulesPath);

        if (farmingRules == null)
        {
            return "[경고] 농사 규칙을 찾지 못해 경작 중 지렁이 획득을 연결하지 않았습니다.";
        }

        SerializedObject serialized = new SerializedObject(farmingRules);
        serialized.FindProperty("tillingBonusItem").objectReferenceValue = bait;
        serialized.FindProperty("tillingBonusChance").floatValue = 0.3f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(farmingRules);
        return "괭이로 밭을 만들 때 30% 확률로 지렁이 미끼 획득 연결";
    }

    // ---------------------------------------------------------------- 월드 아이템

    private static WorldItemPickup CreateOrUpdatePickup(ItemSpec spec, ItemData item, StringBuilder report)
    {
        string path = $"{PickupFolder}/{spec.PickupName}.prefab";

        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null && !AssetDatabase.CopyAsset(PickupTemplatePath, path))
        {
            report.AppendLine($"[오류] 월드 아이템 Prefab 복사 실패: {path}");
            return null;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(path);

        try
        {
            root.name = spec.PickupName;
            WorldItemPickup pickup = root.GetComponent<WorldItemPickup>();
            SerializedObject serialized = new SerializedObject(pickup);
            serialized.FindProperty("promptMessage").stringValue = $"F - PICK UP {spec.DisplayName}";
            serialized.FindProperty("itemData").objectReferenceValue = item;
            serialized.FindProperty("quantity").intValue = 1;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            WorldObjectIdentity identity = root.GetComponent<WorldObjectIdentity>();

            if (identity != null)
            {
                SerializedObject identitySerialized = new SerializedObject(identity);
                identitySerialized.FindProperty("worldObjectId").stringValue = string.Empty;
                identitySerialized.ApplyModifiedPropertiesWithoutUndo();
            }

            StylizedVisualReplacer.Options options = new StylizedVisualReplacer.Options
            {
                ExtraEuler = spec.LyingTool ? new Vector3(0f, 0f, 90f) : Vector3.zero,
                ScaleMultiplier = spec.PickupScale
            };

            if (!StylizedVisualReplacer.Replace(root, spec.ModelId, options, out string message))
            {
                report.AppendLine("[경고] " + message);
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        GameObject saved = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        return saved != null ? saved.GetComponent<WorldItemPickup>() : null;
    }

    private static string RegisterItems(Dictionary<string, ItemData> items, Dictionary<string, WorldItemPickup> pickups)
    {
        StringBuilder report = new StringBuilder();
        ItemDatabase database = AssetDatabase.LoadAssetAtPath<ItemDatabase>(ItemDatabasePath);

        if (database != null)
        {
            SerializedObject serialized = new SerializedObject(database);
            SerializedProperty list = serialized.FindProperty("items");
            int added = 0;

            foreach (ItemData item in items.Values)
            {
                if (!ContainsReference(list, item))
                {
                    list.arraySize++;
                    list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = item;
                    added++;
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
            report.AppendLine($"ItemDatabase 추가 {added}개");
        }

        WorldItemPickupRegistry registry = AssetDatabase.LoadAssetAtPath<WorldItemPickupRegistry>(PickupRegistryPath);

        if (registry == null)
        {
            report.Append("[오류] 월드 아이템 Registry를 찾지 못했습니다.");
            return report.ToString();
        }

        SerializedObject registrySerialized = new SerializedObject(registry);
        SerializedProperty entries = registrySerialized.FindProperty("entries");
        int registered = 0;

        foreach (KeyValuePair<string, WorldItemPickup> pair in pickups)
        {
            ItemData item = items[pair.Key];
            SerializedProperty entry = null;

            for (int index = 0; index < entries.arraySize && entry == null; index++)
            {
                SerializedProperty candidate = entries.GetArrayElementAtIndex(index);

                if (candidate.FindPropertyRelative("itemData").objectReferenceValue == item)
                {
                    entry = candidate;
                }
            }

            if (entry == null)
            {
                entries.arraySize++;
                entry = entries.GetArrayElementAtIndex(entries.arraySize - 1);
                entry.FindPropertyRelative("itemData").objectReferenceValue = item;
                registered++;
            }

            entry.FindPropertyRelative("pickupPrefab").objectReferenceValue = pair.Value;
        }

        registrySerialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(registry);
        report.Append($"월드 아이템 Registry 추가 {registered}개");
        return report.ToString();
    }

    private static bool ContainsReference(SerializedProperty list, Object target)
    {
        for (int index = 0; index < list.arraySize; index++)
        {
            if (list.GetArrayElementAtIndex(index).objectReferenceValue == target)
            {
                return true;
            }
        }

        return false;
    }

    // ---------------------------------------------------------------- Scene 연결

    private static string WireScene(FishingRulesData rules, CraftingRecipeData rodRecipe, Dictionary<string, WorldItemPickup> pickups)
    {
        PlayerInteractor interactor = Object.FindFirstObjectByType<PlayerInteractor>(FindObjectsInactive.Include);

        if (interactor == null || Object.FindFirstObjectByType<BuildGridArea>(FindObjectsInactive.Include) == null)
        {
            return "[건너뜀] 현재 Scene이 게임 Scene(20_Gameplay)이 아니어서 Scene 연결을 생략했습니다.";
        }

        StringBuilder report = new StringBuilder();
        Scene scene = interactor.gameObject.scene;

        CraftingUnlockManager unlockManager = Object.FindFirstObjectByType<CraftingUnlockManager>(FindObjectsInactive.Include);

        if (unlockManager != null && rodRecipe != null)
        {
            SerializedObject serialized = new SerializedObject(unlockManager);
            SerializedProperty list = serialized.FindProperty("allRecipes");

            if (!ContainsReference(list, rodRecipe))
            {
                list.arraySize++;
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = rodRecipe;
                serialized.ApplyModifiedProperties();
            }

            report.AppendLine("제작법 해금 목록에 낚싯대 연결");
        }

        GameObject rodVisual = SetupRodVisual(out Transform rodTip);
        report.AppendLine(rodVisual != null ? "손에 든 낚싯대 외형 완료" : "[경고] 손에 든 낚싯대 외형을 만들지 못했습니다.");

        FishingController controller = SetupController(scene, interactor, rules, rodTip);
        report.AppendLine($"플레이어 낚시 조작 : {controller.gameObject.name}");
        report.AppendLine(FishingMinigameUIBuilder.Build(controller));

        report.AppendLine(SetupPond(scene, pickups));

        WorldObjectIdValidator.AssignAndValidateWorldObjectIds();
        report.AppendLine("월드 오브젝트 저장 ID 발급 완료");

        EditorSceneManager.MarkSceneDirty(scene);
        report.Append("Scene 변경 완료 → Ctrl+S로 저장하세요");
        return report.ToString();
    }

    private static GameObject SetupRodVisual(out Transform rodTip)
    {
        rodTip = null;
        EquippedToolView toolView = Object.FindFirstObjectByType<EquippedToolView>(FindObjectsInactive.Include);

        if (toolView == null)
        {
            return null;
        }

        SerializedObject serialized = new SerializedObject(toolView);
        SerializedProperty property = serialized.FindProperty("fishingRodVisual");
        GameObject template = serialized.FindProperty("axeVisual").objectReferenceValue as GameObject;
        GameObject visual = property.objectReferenceValue as GameObject;

        if (visual == null)
        {
            if (template == null)
            {
                return null;
            }

            visual = Object.Instantiate(template, template.transform.parent);
            Undo.RegisterCreatedObjectUndo(visual, "Create Fishing Rod Visual");
            visual.name = RodVisualName;
            visual.transform.SetSiblingIndex(template.transform.GetSiblingIndex() + 1);
            property.objectReferenceValue = visual;
            serialized.ApplyModifiedProperties();
        }

        StylizedVisualReplacer.Options options = new StylizedVisualReplacer.Options
        {
            ParentOverride = visual.transform,
            AlignModelUpToLongestAxis = true,
            BladeHintObjectName = "Head",
            ScaleMultiplier = 2.2f,
            UseUndo = true
        };

        StylizedVisualReplacer.Replace(visual, "tool_fishing_rod", options, out _);

        // 낚싯대 끝 : 모델 메시 기준 (0, 1.6, 0) 지점
        Transform tip = visual.transform.Find(RodTipName);
        StylizedVisualReplacement record = visual.GetComponent<StylizedVisualReplacement>();

        if (tip == null)
        {
            tip = new GameObject(RodTipName).transform;
            Undo.RegisterCreatedObjectUndo(tip.gameObject, "Create Rod Tip");
            tip.SetParent(visual.transform, false);
        }

        if (record != null && record.GeneratedVisual != null)
        {
            Transform model = record.GeneratedVisual.transform;
            tip.position = model.TransformPoint(new Vector3(0.012f, 1.6f, 0f));
        }

        Undo.RecordObject(visual, "Create Fishing Rod Visual");
        visual.SetActive(false);
        rodTip = tip;
        return visual;
    }

    private static FishingController SetupController(Scene scene, PlayerInteractor interactor, FishingRulesData rules, Transform rodTip)
    {
        GameObject player = interactor.gameObject;
        FishingController controller = player.GetComponent<FishingController>();

        if (controller == null)
        {
            controller = Undo.AddComponent<FishingController>(player);
        }

        // 던질 지점 대상과 고리 표시
        GameObject target = FindRoot(scene, CastTargetName);

        if (target == null)
        {
            target = new GameObject(CastTargetName);
            SceneManager.MoveGameObjectToScene(target, scene);
            Undo.RegisterCreatedObjectUndo(target, "Create Fishing Cast Target");
        }

        FishingCastTarget castTarget = target.GetComponent<FishingCastTarget>();

        if (castTarget == null)
        {
            castTarget = Undo.AddComponent<FishingCastTarget>(target);
        }

        SerializedObject targetSerialized = new SerializedObject(castTarget);
        targetSerialized.FindProperty("promptMessage").stringValue = "F - CAST LINE";
        targetSerialized.ApplyModifiedProperties();

        Transform marker = target.transform.Find("CastMarker");

        if (marker == null)
        {
            GameObject ring = (GameObject)PrefabUtility.InstantiatePrefab(StylizedArtAssetFactory.GetOrCreateModelPrefab("fx_cast_ring", false), target.transform);
            Undo.RegisterCreatedObjectUndo(ring, "Create Cast Marker");
            ring.name = "CastMarker";
            marker = ring.transform;
        }

        foreach (Renderer renderer in marker.GetComponentsInChildren<Renderer>(true))
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        marker.gameObject.SetActive(false);

        // 낚싯줄
        Transform lineTransform = player.transform.Find(LineName);

        if (lineTransform == null)
        {
            lineTransform = new GameObject(LineName).transform;
            Undo.RegisterCreatedObjectUndo(lineTransform.gameObject, "Create Fishing Line");
            lineTransform.SetParent(player.transform, false);
        }

        LineRenderer line = lineTransform.GetComponent<LineRenderer>();

        if (line == null)
        {
            line = Undo.AddComponent<LineRenderer>(lineTransform.gameObject);
        }

        line.useWorldSpace = true;
        line.positionCount = 14;
        line.widthMultiplier = 0.012f;
        line.numCapVertices = 0;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.sharedMaterial = GetOrCreateLineMaterial();
        line.enabled = false;

        SerializedObject interactorSerialized = new SerializedObject(interactor);
        interactorSerialized.FindProperty("fishingController").objectReferenceValue = controller;
        Transform viewTransform = interactorSerialized.FindProperty("viewTransform").objectReferenceValue as Transform;
        BuildPlacementController build = interactorSerialized.FindProperty("buildPlacementController").objectReferenceValue as BuildPlacementController;
        interactorSerialized.ApplyModifiedProperties();

        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("playerInventory").objectReferenceValue = FindOnPlayerOrScene<PlayerInventory>(player);
        serialized.FindProperty("playerStamina").objectReferenceValue = FindOnPlayerOrScene<PlayerStamina>(player);
        serialized.FindProperty("playerHealth").objectReferenceValue = FindOnPlayerOrScene<PlayerHealth>(player);
        serialized.FindProperty("playerMovement").objectReferenceValue = FindOnPlayerOrScene<PlayerMovement>(player);
        serialized.FindProperty("buildPlacementController").objectReferenceValue = build != null ? build : FindOnPlayerOrScene<BuildPlacementController>(player);
        serialized.FindProperty("viewTransform").objectReferenceValue = viewTransform;
        serialized.FindProperty("rules").objectReferenceValue = rules;
        serialized.FindProperty("castTarget").objectReferenceValue = castTarget;
        serialized.FindProperty("castMarker").objectReferenceValue = marker.gameObject;
        serialized.FindProperty("rodTip").objectReferenceValue = rodTip;
        serialized.FindProperty("fishingLine").objectReferenceValue = line;
        serialized.FindProperty("bobberPrefab").objectReferenceValue = StylizedArtAssetFactory.GetOrCreateModelPrefab("fx_fishing_bobber", false);

        // 83일차 : 출현 조건·잡은 기록·바닥 드롭
        FishingJournal journal = player.GetComponent<FishingJournal>();

        if (journal == null)
        {
            journal = Undo.AddComponent<FishingJournal>(player);
        }

        serialized.FindProperty("gameDataRegistry").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameDataRegistry>(RegistryPath);
        serialized.FindProperty("dayNightCycle").objectReferenceValue = Object.FindFirstObjectByType<DayNightCycle>(FindObjectsInactive.Include);
        serialized.FindProperty("seasonCycle").objectReferenceValue = Object.FindFirstObjectByType<SeasonCycle>(FindObjectsInactive.Include);
        serialized.FindProperty("weatherCycle").objectReferenceValue = Object.FindFirstObjectByType<WeatherCycle>(FindObjectsInactive.Include);
        serialized.FindProperty("journal").objectReferenceValue = journal;
        serialized.FindProperty("pickupRegistry").objectReferenceValue = AssetDatabase.LoadAssetAtPath<WorldItemPickupRegistry>(PickupRegistryPath);
        serialized.FindProperty("dropContainer").objectReferenceValue = Object.FindFirstObjectByType<WorldItemDropContainer>(FindObjectsInactive.Include);
        serialized.ApplyModifiedProperties();
        return controller;
    }

    private static Material GetOrCreateLineMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(LineMaterialPath);

        if (material != null)
        {
            return material;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        material = new Material(shader != null ? shader : Shader.Find("Sprites/Default"));
        Color color = new Color(0.92f, 0.92f, 0.88f, 1f);

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        AssetDatabase.CreateAsset(material, LineMaterialPath);
        return material;
    }

    private static T FindOnPlayerOrScene<T>(GameObject player) where T : Component
    {
        T found = player.GetComponent<T>();
        return found != null ? found : Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == name)
            {
                return root;
            }
        }

        return null;
    }

    // ---------------------------------------------------------------- 연못

    private static string SetupPond(Scene scene, Dictionary<string, WorldItemPickup> pickups)
    {
        StringBuilder report = new StringBuilder();
        GameObject root = FindRoot(scene, PondRootName);
        BuildGridArea grid = Object.FindFirstObjectByType<BuildGridArea>(FindObjectsInactive.Include);
        Bounds gridBounds = GetGridBounds(grid);

        // 몬스터 스폰 지점과 너무 가까운 기존 연못은 다시 만든다 (낚시가 매번 피격으로 끊기는 문제 방지)
        if (root != null && GetNearestSpawnDistance(root.transform.position) < GetEnemySpawnClearance())
        {
            report.AppendLine($"기존 연못이 몬스터 스폰 지점과 가까워({GetNearestSpawnDistance(root.transform.position):0.0}m) 위치를 다시 정합니다.");
            Undo.DestroyObjectImmediate(root);
            root = null;
        }

        if (root == null)
        {
            if (!TryFindPondLocation(scene, grid, gridBounds, out Vector3 center, out string locationReport))
            {
                return "[경고] 연못을 놓을 평평한 빈 곳을 찾지 못했습니다. " + locationReport;
            }

            root = new GameObject(PondRootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Create Fishing Pond");
            root.transform.position = center;
            report.AppendLine($"연못 위치 : {center} ({locationReport})");
            report.AppendLine($"연못 자리의 나무·바위·풀 정리 {ClearEnvironment(scene, center)}개");
        }

        // 연못 모델
        Transform pondModel = root.transform.Find("Pond");

        if (pondModel == null)
        {
            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(StylizedArtAssetFactory.GetOrCreateModelPrefab("prop_pond", false), root.transform);
            Undo.RegisterCreatedObjectUndo(model, "Create Pond Model");
            model.name = "Pond";
            model.transform.localPosition = Vector3.zero;
            GameObjectUtility.SetStaticEditorFlags(model, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
        }

        // 낚시터
        FishingSpot spot = root.GetComponent<FishingSpot>();

        if (spot == null)
        {
            spot = Undo.AddComponent<FishingSpot>(root);
        }

        SerializedObject spotSerialized = new SerializedObject(spot);
        spotSerialized.FindProperty("spotId").stringValue = "spot_camp_pond";
        spotSerialized.FindProperty("waterBodyType").intValue = (int)WaterBodyType.Lake;
        spotSerialized.FindProperty("radius").floatValue = StylizedModelLibrary.PondWaterRadius - 0.2f;
        spotSerialized.FindProperty("surfaceHeight").floatValue = StylizedModelLibrary.PondWaterHeight;
        spotSerialized.ApplyModifiedProperties();

        // 물에 들어가지 못하게 막는 테두리 충돌체
        Transform blockers = root.transform.Find("RimBlockers");

        if (blockers == null)
        {
            blockers = new GameObject("RimBlockers").transform;
            Undo.RegisterCreatedObjectUndo(blockers.gameObject, "Create Rim Blockers");
            blockers.SetParent(root.transform, false);
            const int segments = 16;
            float radius = StylizedModelLibrary.PondRimRadius + 0.05f;
            float width = 2f * Mathf.PI * radius / segments + 0.15f;

            for (int index = 0; index < segments; index++)
            {
                float angle = index * Mathf.PI * 2f / segments;
                Vector3 outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                GameObject blocker = new GameObject($"Blocker_{index:00}");
                blocker.transform.SetParent(blockers, false);
                blocker.transform.localPosition = outward * radius;
                blocker.transform.localRotation = Quaternion.LookRotation(outward);
                BoxCollider box = blocker.AddComponent<BoxCollider>();
                box.center = new Vector3(0f, 0.6f, 0f);
                box.size = new Vector3(width, 1.2f, 0.5f);
            }
        }

        NavMeshObstacle obstacle = root.GetComponent<NavMeshObstacle>();

        if (obstacle == null)
        {
            obstacle = Undo.AddComponent<NavMeshObstacle>(root);
        }

        obstacle.shape = NavMeshObstacleShape.Capsule;
        obstacle.center = new Vector3(0f, 0.75f, 0f);
        obstacle.radius = StylizedModelLibrary.PondRimRadius + 0.2f;
        obstacle.height = 1.5f;
        obstacle.carving = true;

        report.Append(PlaceStarterPickups(root, gridBounds, pickups));
        return report.ToString();
    }

    private static Bounds GetGridBounds(BuildGridArea grid)
    {
        SerializedObject serialized = new SerializedObject(grid);
        float cell = serialized.FindProperty("cellSize").floatValue;
        float width = serialized.FindProperty("gridWidth").intValue * cell;
        float depth = serialized.FindProperty("gridDepth").intValue * cell;
        Bounds bounds = new Bounds(grid.transform.position, Vector3.zero);
        bounds.Encapsulate(grid.transform.TransformPoint(new Vector3(width, 0f, 0f)));
        bounds.Encapsulate(grid.transform.TransformPoint(new Vector3(0f, 0f, depth)));
        bounds.Encapsulate(grid.transform.TransformPoint(new Vector3(width, 0f, depth)));
        return bounds;
    }

    private static bool TryFindPondLocation(Scene scene, BuildGridArea grid, Bounds gridBounds, out Vector3 bestCenter, out string summary)
    {
        List<Vector3> avoid = CollectAvoidPoints(scene);
        List<Vector3> spawns = CollectEnemySpawnPoints();
        float spawnClearance = GetEnemySpawnClearance();
        Vector3 player = Object.FindFirstObjectByType<PlayerMovement>(FindObjectsInactive.Include) != null
            ? Object.FindFirstObjectByType<PlayerMovement>(FindObjectsInactive.Include).transform.position
            : gridBounds.center;
        Vector3[] directions = { Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
        float bestScore = float.MaxValue;
        bestCenter = Vector3.zero;
        summary = string.Empty;
        const float fenceMargin = 2.2f;
        float ringRadius = StylizedModelLibrary.PondRimRadius + 0.6f;

        foreach (Vector3 direction in directions)
        {
            Vector3 lateral = new Vector3(direction.z, 0f, -direction.x);
            float extent = Mathf.Abs(Vector3.Dot(gridBounds.extents, new Vector3(Mathf.Abs(direction.x), 0f, Mathf.Abs(direction.z))));

            for (int distanceStep = 0; distanceStep < 5; distanceStep++)
            {
                for (int lateralStep = -3; lateralStep <= 3; lateralStep++)
                {
                    Vector3 center = gridBounds.center
                        + direction * (extent + fenceMargin + 1.8f + ringRadius + distanceStep * 3f)
                        + lateral * (lateralStep * 5f);

                    if (!TrySampleArea(center, ringRadius + 0.6f, out float average, out float range))
                    {
                        continue;
                    }

                    if (range > 1.2f || HasNearbyObject(avoid, center, ringRadius + 2.5f) || HasNearbyObject(spawns, center, spawnClearance))
                    {
                        continue;
                    }

                    int clutter = CountEnvironment(scene, center);
                    float playerDistance = Vector3.Distance(new Vector3(player.x, 0f, player.z), new Vector3(center.x, 0f, center.z));
                    float score = range * 12f + clutter * 0.4f + playerDistance * 0.08f + distanceStep;

                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestCenter = new Vector3(center.x, average, center.z);
                        summary = $"높이 차 {range:0.00}m, 정리할 배치물 {clutter}개, 플레이어 거리 {playerDistance:0.0}m, 몬스터 스폰 거리 {GetNearestSpawnDistance(center):0.0}m";
                    }
                }
            }
        }

        return bestScore < float.MaxValue;
    }

    private static List<Vector3> CollectAvoidPoints(Scene scene)
    {
        List<Vector3> points = new List<Vector3>();

        foreach (GatherableResource resource in Object.FindObjectsByType<GatherableResource>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            points.Add(resource.transform.position);
        }

        foreach (WorldItemPickup pickup in Object.FindObjectsByType<WorldItemPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            points.Add(pickup.transform.position);
        }

        // 거점 소품(천막·우물 등)과 농사 시작 아이템 주변은 피한다
        GameObject environment = FindRoot(scene, EnvironmentRootName);
        Transform camp = environment != null ? environment.transform.Find("Camp") : null;

        if (camp != null)
        {
            foreach (Transform child in camp)
            {
                points.Add(child.position);
            }
        }

        GameObject farmStarter = FindRoot(scene, FarmStarterRootName);

        if (farmStarter != null)
        {
            foreach (Transform child in farmStarter.transform)
            {
                points.Add(child.position);
            }
        }

        return points;
    }

    private static List<Vector3> CollectEnemySpawnPoints()
    {
        List<Vector3> points = new List<Vector3>();

        foreach (EnemySpawnPoint spawn in Object.FindObjectsByType<EnemySpawnPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            points.Add(spawn.transform.position);
        }

        return points;
    }

    // 연못 가장자리에서 낚시하는 플레이어를 몬스터가 발견하지 못하는 거리 (가장 긴 탐지 거리 + 연못 테두리 + 여유)
    private static float GetEnemySpawnClearance()
    {
        float detection = 10f;

        foreach (string guid in AssetDatabase.FindAssets("t:EnemyCombatData"))
        {
            EnemyCombatData data = AssetDatabase.LoadAssetAtPath<EnemyCombatData>(AssetDatabase.GUIDToAssetPath(guid));

            if (data != null)
            {
                detection = Mathf.Max(detection, data.DetectionRange);
            }
        }

        return detection + StylizedModelLibrary.PondRimRadius + 1.5f;
    }

    private static float GetNearestSpawnDistance(Vector3 center)
    {
        float nearest = float.MaxValue;

        foreach (Vector3 point in CollectEnemySpawnPoints())
        {
            nearest = Mathf.Min(nearest, Vector2.Distance(new Vector2(point.x, point.z), new Vector2(center.x, center.z)));
        }

        return nearest;
    }

    private static bool HasNearbyObject(List<Vector3> points, Vector3 center, float radius)
    {
        float radiusSqr = radius * radius;

        foreach (Vector3 point in points)
        {
            float dx = point.x - center.x;
            float dz = point.z - center.z;

            if (dx * dx + dz * dz < radiusSqr)
            {
                return true;
            }
        }

        return false;
    }

    private static bool TrySampleArea(Vector3 center, float radius, out float average, out float range)
    {
        average = 0f;
        range = float.MaxValue;
        float min = float.MaxValue;
        float max = float.MinValue;
        float sum = 0f;
        int count = 0;

        for (int ring = 0; ring <= 2; ring++)
        {
            int samples = ring == 0 ? 1 : 12;

            for (int index = 0; index < samples; index++)
            {
                float angle = index * Mathf.PI * 2f / samples;
                Vector3 point = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (radius * ring / 2f);

                if (!TrySampleGround(point, out float height))
                {
                    return false;
                }

                min = Mathf.Min(min, height);
                max = Mathf.Max(max, height);
                sum += height;
                count++;
            }
        }

        average = sum / count;
        range = max - min;
        return true;
    }

    private static bool TrySampleGround(Vector3 world, out float height)
    {
        foreach (Terrain terrain in Terrain.activeTerrains)
        {
            Vector3 local = world - terrain.transform.position;
            Vector3 size = terrain.terrainData.size;
            const float margin = 4f;

            if (local.x >= margin && local.z >= margin && local.x <= size.x - margin && local.z <= size.z - margin)
            {
                height = terrain.SampleHeight(world) + terrain.transform.position.y;
                return true;
            }
        }

        height = 0f;
        return false;
    }

    private static IEnumerable<Transform> EnvironmentProps(Scene scene)
    {
        GameObject environment = FindRoot(scene, EnvironmentRootName);

        if (environment == null)
        {
            yield break;
        }

        foreach (string group in new[] { "Trees", "Rocks", "Plants" })
        {
            Transform groupTransform = environment.transform.Find(group);

            if (groupTransform == null)
            {
                continue;
            }

            foreach (Transform child in groupTransform)
            {
                yield return child;
            }
        }
    }

    private static int CountEnvironment(Scene scene, Vector3 center)
    {
        float radius = StylizedModelLibrary.PondRimRadius + 1.5f;
        int count = 0;

        foreach (Transform prop in EnvironmentProps(scene))
        {
            Vector3 offset = prop.position - center;
            offset.y = 0f;

            if (offset.sqrMagnitude < radius * radius)
            {
                count++;
            }
        }

        return count;
    }

    private static int ClearEnvironment(Scene scene, Vector3 center)
    {
        float radius = StylizedModelLibrary.PondRimRadius + 1.5f;
        List<GameObject> targets = new List<GameObject>();

        foreach (Transform prop in EnvironmentProps(scene))
        {
            Vector3 offset = prop.position - center;
            offset.y = 0f;

            if (offset.sqrMagnitude < radius * radius)
            {
                targets.Add(prop.gameObject);
            }
        }

        foreach (GameObject target in targets)
        {
            Undo.DestroyObjectImmediate(target);
        }

        return targets.Count;
    }

    private static string PlaceStarterPickups(GameObject root, Bounds gridBounds, Dictionary<string, WorldItemPickup> pickups)
    {
        Vector3 toCamp = gridBounds.center - root.transform.position;
        toCamp.y = 0f;
        toCamp = toCamp.sqrMagnitude > 0.01f ? toCamp.normalized : Vector3.forward;
        Vector3 side = new Vector3(toCamp.z, 0f, -toCamp.x);
        int placed = 0;
        int index = 0;

        foreach (ItemSpec spec in ItemSpecs)
        {
            if (spec.StarterQuantity <= 0 || !pickups.TryGetValue(spec.Id, out WorldItemPickup prefab))
            {
                continue;
            }

            Transform existing = root.transform.Find(spec.PickupName);
            WorldItemPickup instance = existing != null ? existing.GetComponent<WorldItemPickup>() : null;

            if (instance == null)
            {
                GameObject created = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject, root.transform);
                Undo.RegisterCreatedObjectUndo(created, "Place Fishing Starter");
                created.name = spec.PickupName;
                Vector3 position = root.transform.position + toCamp * (StylizedModelLibrary.PondRimRadius + 1.4f) + side * ((index - 0.5f) * 1.1f);
                position.y = (TrySampleGround(position, out float height) ? height : position.y) + 0.3f;
                created.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, index * 40f, 0f));
                instance = created.GetComponent<WorldItemPickup>();
                placed++;
            }

            SerializedObject serialized = new SerializedObject(instance);
            serialized.FindProperty("quantity").intValue = spec.StarterQuantity;
            serialized.ApplyModifiedProperties();
            index++;
        }

        return $"연못 옆 낚시 시작 아이템 배치 {placed}개 (낚싯대, 지렁이 미끼 10개)";
    }

    // ---------------------------------------------------------------- 검증

    public static string Validate(out int errorCount)
    {
        StringBuilder report = new StringBuilder("[낚시 콘텐츠 검증]\n");
        int errors = 0;

        void Error(string message)
        {
            errors++;
            report.AppendLine("✗ " + message);
        }

        ItemDatabase database = AssetDatabase.LoadAssetAtPath<ItemDatabase>(ItemDatabasePath);
        WorldItemPickupRegistry pickupRegistry = AssetDatabase.LoadAssetAtPath<WorldItemPickupRegistry>(PickupRegistryPath);
        int itemCount = 0;

        foreach (ItemSpec spec in ItemSpecs)
        {
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>($"{ItemFolder}/{spec.AssetName}.asset");

            if (item == null)
            {
                Error($"아이템 없음: {spec.AssetName}");
                continue;
            }

            if (item.ItemId != spec.Id || item.ItemCategory != spec.Category || item.ToolType != spec.Tool)
            {
                Error($"아이템 설정 불일치: {spec.AssetName}");
            }

            if (database == null || !database.TryGetItem(spec.Id, out ItemData registered) || registered != item)
            {
                Error($"ItemDatabase 미등록: {spec.Id}");
            }

            if (pickupRegistry == null || !pickupRegistry.TryGetPickup(item, out WorldItemPickup pickup) || pickup.ItemData != item)
            {
                Error($"월드 아이템 Registry 미등록: {spec.Id}");
            }

            itemCount++;
        }

        report.AppendLine($"아이템 {itemCount}/{ItemSpecs.Length}");

        GameDataRegistry registry = AssetDatabase.LoadAssetAtPath<GameDataRegistry>(RegistryPath);
        int fishCount = 0;

        foreach (FishSpec spec in FishSpecs)
        {
            FishData fish = AssetDatabase.LoadAssetAtPath<FishData>($"{FishFolder}/{spec.AssetName}.asset");

            if (fish == null)
            {
                Error($"물고기 없음: {spec.AssetName}");
                continue;
            }

            if (!fish.TryValidate(out string fishError))
            {
                Error(fishError);
                continue;
            }

            if (registry == null || !registry.TryGetFish(fish.FishId, out FishData registeredFish) || registeredFish != fish)
            {
                Error($"GameDataRegistry 물고기 미등록: {fish.FishId}");
            }

            fishCount++;
        }

        report.AppendLine($"물고기 {fishCount}/{FishSpecs.Length}");

        // 모든 계절에 한 종류 이상 낚이는지 확인 (기본 낚싯대, 연못, 맑은 날)
        foreach (SeasonType season in new[] { SeasonType.Spring, SeasonType.Summer, SeasonType.Autumn, SeasonType.Winter })
        {
            bool any = false;

            foreach (float hour in new[] { 8f, 14f, 20f, 2f })
            {
                foreach (FishSpec spec in FishSpecs)
                {
                    FishData fish = AssetDatabase.LoadAssetAtPath<FishData>($"{FishFolder}/{spec.AssetName}.asset");
                    any |= fish != null && fish.CanAppear(WaterBodyType.Lake, season, WeatherType.Clear, hour, 1);
                }
            }

            if (!any)
            {
                Error($"{season}에 연못에서 낚을 수 있는 물고기가 없습니다.");
            }
        }

        FishingRulesData rules = AssetDatabase.LoadAssetAtPath<FishingRulesData>(RulesPath);

        if (rules == null)
        {
            Error("낚시 규칙 데이터 없음");
        }
        else if (!rules.TryValidate(out string rulesError))
        {
            Error(rulesError);
        }
        else
        {
            ValidateSelectionAndMinigame(rules, report, Error);
        }

        FarmingRulesData farmingRules = AssetDatabase.LoadAssetAtPath<FarmingRulesData>(FarmingRulesPath);

        if (farmingRules != null && (rules == null || farmingRules.TillingBonusItem != rules.BaitItem))
        {
            Error("경작 보너스 아이템이 지렁이 미끼로 연결되지 않았습니다.");
        }

        if (AssetDatabase.LoadAssetAtPath<CraftingRecipeData>($"{CraftingFolder}/CraftingRecipe_FishingRodBasic.asset") == null)
        {
            Error("낚싯대 제작법 없음");
        }

        ValidateScene(report, Error);
        errorCount = errors;
        report.AppendLine(errors == 0 ? "결과 : 오류 0개" : $"결과 : 오류 {errors}개");
        return report.ToString();
    }

    private static void ValidateSelectionAndMinigame(FishingRulesData rules, StringBuilder report, System.Action<string> error)
    {
        List<FishData> allFish = new List<FishData>();

        foreach (FishSpec spec in FishSpecs)
        {
            FishData fish = AssetDatabase.LoadAssetAtPath<FishData>($"{FishFolder}/{spec.AssetName}.asset");

            if (fish != null)
            {
                allFish.Add(fish);
            }
        }

        SeasonType[] seasons = { SeasonType.Spring, SeasonType.Summer, SeasonType.Autumn, SeasonType.Winter };
        WeatherType[] weathers = { WeatherType.Clear, WeatherType.Cloudy, WeatherType.Rain, WeatherType.Snow, WeatherType.Storm };
        float[] hours = { 8f, 14f, 20f, 2f };

        foreach (FishData fish in allFish)
        {
            // 어떤 계절·날씨·시각에 한 번이라도 뽑힐 수 있는지 (기본 낚싯대, 첫 번째 출현 물가)
            float bestChance = 0f;
            WaterBodyType water = fish.WaterBodies.Count > 0 ? fish.WaterBodies[0] : WaterBodyType.Lake;

            foreach (SeasonType season in seasons)
            {
                foreach (WeatherType weather in weathers)
                {
                    foreach (float hour in hours)
                    {
                        FishingConditions conditions = new FishingConditions(water, season, weather, hour, fish.RequiredRodTier, true);
                        bestChance = Mathf.Max(bestChance, FishSelector.GetChance(allFish, fish, conditions, rules.BaitRareWeightMultiplier));
                    }
                }
            }

            if (bestChance <= 0f)
            {
                error($"{fish.FishId}가 뽑힐 수 있는 조건이 없습니다.");
            }

            // 표시가 한 번 왕복하는 시간 × 필요 성공 횟수가 제한 시간 안에 들어오는지
            float roundTrip = 2f / rules.GetMarkerSpeed(fish.Difficulty);
            float worstCase = roundTrip * fish.RequiredSuccessCount;
            float timeLimit = rules.GetTimeLimit(fish.RequiredSuccessCount, 0f);

            if (worstCase > timeLimit)
            {
                error($"{fish.FishId} 미니게임을 제한 시간 안에 끝낼 수 없습니다. ({worstCase:0.0}초 > {timeLimit:0.0}초)");
            }

            if (fish.RequiredSuccessCount > FishingMinigameUIBuilder.SuccessPipCount)
            {
                error($"{fish.FishId} 필요 성공 횟수가 HUD 점 수({FishingMinigameUIBuilder.SuccessPipCount})보다 많습니다.");
            }
        }

        if (rules.MinigameMaxMisses > FishingMinigameUIBuilder.MissPipCount)
        {
            error($"최대 실수 횟수가 HUD 점 수({FishingMinigameUIBuilder.MissPipCount})보다 많습니다.");
        }

        // 대표 조건별 확률 (연못, 미끼 사용)
        foreach ((string label, SeasonType season, WeatherType weather, float hour) sample in new[]
                 {
                     ("봄 맑은 아침", SeasonType.Spring, WeatherType.Clear, 8f),
                     ("봄 비 오는 아침", SeasonType.Spring, WeatherType.Rain, 8f),
                     ("여름 맑은 밤", SeasonType.Summer, WeatherType.Clear, 22f),
                     ("겨울 눈 오는 낮", SeasonType.Winter, WeatherType.Snow, 14f)
                 })
        {
            FishingConditions conditions = new FishingConditions(WaterBodyType.Lake, sample.season, sample.weather, sample.hour, 1, true);
            StringBuilder line = new StringBuilder($"  {sample.label} :");

            foreach (FishData fish in allFish)
            {
                float chance = FishSelector.GetChance(allFish, fish, conditions, rules.BaitRareWeightMultiplier);

                if (chance > 0f)
                {
                    line.Append($" {fish.DisplayName} {chance * 100f:0}%");
                }
            }

            report.AppendLine(line.ToString());
        }
    }

    private static void ValidateScene(StringBuilder report, System.Action<string> error)
    {
        PlayerInteractor interactor = Object.FindFirstObjectByType<PlayerInteractor>(FindObjectsInactive.Include);

        if (interactor == null || Object.FindFirstObjectByType<BuildGridArea>(FindObjectsInactive.Include) == null)
        {
            report.AppendLine("Scene 검사 생략 (게임 Scene이 열려 있지 않음)");
            return;
        }

        FishingController controller = interactor.GetComponent<FishingController>();

        if (controller == null)
        {
            error("플레이어에 FishingController가 없습니다.");
        }
        else
        {
            SerializedObject serialized = new SerializedObject(controller);

            foreach (string property in new[] { "playerInventory", "playerStamina", "playerHealth", "playerMovement", "buildPlacementController", "viewTransform", "rules", "castTarget", "castMarker", "rodTip", "fishingLine", "bobberPrefab", "gameDataRegistry", "dayNightCycle", "seasonCycle", "weatherCycle", "journal", "pickupRegistry", "dropContainer" })
            {
                if (serialized.FindProperty(property).objectReferenceValue == null)
                {
                    error($"FishingController.{property} 연결이 비어 있습니다.");
                }
            }

            if (new SerializedObject(interactor).FindProperty("fishingController").objectReferenceValue != controller)
            {
                error("PlayerInteractor에 FishingController가 연결되지 않았습니다.");
            }

            FishingMinigameController minigame = interactor.GetComponent<FishingMinigameController>();

            if (minigame == null)
            {
                error("플레이어에 FishingMinigameController가 없습니다.");
            }
            else
            {
                SerializedObject minigameSerialized = new SerializedObject(minigame);

                foreach (string property in new[] { "fishingController", "minigameUI" })
                {
                    if (minigameSerialized.FindProperty(property).objectReferenceValue == null)
                    {
                        error($"FishingMinigameController.{property} 연결이 비어 있습니다.");
                    }
                }

                FishingMinigameUI ui = minigameSerialized.FindProperty("minigameUI").objectReferenceValue as FishingMinigameUI;

                if (ui != null)
                {
                    SerializedObject uiSerialized = new SerializedObject(ui);

                    foreach (string property in new[] { "panelRoot", "titleText", "rarityText", "zoneRect", "zoneImage", "markerRect", "markerImage", "timeFillRect", "timeFillImage" })
                    {
                        if (uiSerialized.FindProperty(property).objectReferenceValue == null)
                        {
                            error($"FishingMinigameUI.{property} 연결이 비어 있습니다.");
                        }
                    }

                    if (uiSerialized.FindProperty("successPips").arraySize < FishingMinigameUIBuilder.SuccessPipCount
                        || uiSerialized.FindProperty("missPips").arraySize < FishingMinigameUIBuilder.MissPipCount)
                    {
                        error("FishingMinigameUI 성공·실수 점이 부족합니다.");
                    }
                }
            }
        }

        EquippedToolView toolView = Object.FindFirstObjectByType<EquippedToolView>(FindObjectsInactive.Include);

        if (toolView != null && new SerializedObject(toolView).FindProperty("fishingRodVisual").objectReferenceValue == null)
        {
            error("EquippedToolView에 낚싯대 외형이 연결되지 않았습니다.");
        }

        FishingSpot[] spots = Object.FindObjectsByType<FishingSpot>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (spots.Length == 0)
        {
            error("Scene에 낚시터(FishingSpot)가 없습니다.");
        }

        float spawnClearance = GetEnemySpawnClearance();

        foreach (FishingSpot spot in spots)
        {
            float distance = GetNearestSpawnDistance(spot.transform.position);

            if (distance < spawnClearance)
            {
                error($"낚시터 {spot.SpotId}가 몬스터 스폰 지점과 너무 가깝습니다 ({distance:0.0}m < {spawnClearance:0.0}m).");
            }
        }

        CraftingUnlockManager unlockManager = Object.FindFirstObjectByType<CraftingUnlockManager>(FindObjectsInactive.Include);
        CraftingRecipeData recipe = AssetDatabase.LoadAssetAtPath<CraftingRecipeData>($"{CraftingFolder}/CraftingRecipe_FishingRodBasic.asset");

        if (unlockManager != null && recipe != null && !ContainsReference(new SerializedObject(unlockManager).FindProperty("allRecipes"), recipe))
        {
            error("CraftingUnlockManager에 낚싯대 제작법이 없습니다.");
        }

        report.AppendLine($"Scene 연결 확인 (낚시터 {spots.Length}개)");
    }
}
