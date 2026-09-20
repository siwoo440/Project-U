using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// 79일차: 농사 콘텐츠(씨앗·작물·수확물·농사 도구·밭·제작법)를 한 번에 만들고
// 아이템 DB, 월드 아이템 Registry, GameDataRegistry, 현재 게임 Scene에 연결한다.
// 여러 번 실행해도 같은 Asset을 갱신하며 새로 중복 생성하지 않는다.
public static class FarmingContentBuilder
{
    public const string BuildMenuRoot = "Tools/Project U/Build Content/";
    private const string DialogTitle = "Project U 농사 콘텐츠";

    private const string ItemFolder = "Assets/_ProjectU/Data/Items/Day79";
    private const string CropFolder = "Assets/_ProjectU/Data/Farming/Crops";
    private const string RulesPath = "Assets/_ProjectU/Data/Farming/FarmingRules_Default.asset";
    private const string CraftingFolder = "Assets/_ProjectU/Data/Crafting/Day79";
    private const string BuildRecipePath = "Assets/_ProjectU/Data/Building/Day79/BuildRecipe_FarmPlot.asset";
    private const string PickupFolder = "Assets/_ProjectU/Prefabs/Items/Day79";
    private const string BuildingFolder = "Assets/_ProjectU/Prefabs/Building/Day79";
    private const string PlotPlacedPath = BuildingFolder + "/FarmPlotPlaced.prefab";
    private const string PlotPreviewPath = BuildingFolder + "/FarmPlotPreview.prefab";
    private const string PickupTemplatePath = "Assets/_ProjectU/Prefabs/Items/Day71/WildMushroomPickup.prefab";
    private const string LayerReferencePrefabPath = "Assets/_ProjectU/Prefabs/Building/Day73/WoodTablePlaced.prefab";
    private const string ItemDatabasePath = "Assets/_ProjectU/Data/Databases/ItemDatabase.asset";
    private const string PickupRegistryPath = "Assets/_ProjectU/Prefabs/Items/Day75/WorldItemPickupRegistry_Day75.asset";
    private const string WoodItemPath = "Assets/_ProjectU/Data/Items/ItemData_Wood.asset";
    private const string StoneItemPath = "Assets/_ProjectU/Data/Items/ItemData_Stone.asset";
    private const string PlantFiberItemPath = "Assets/_ProjectU/Data/Items/Day71/ItemData_PlantFiber.asset";
    private const string IronOreItemPath = "Assets/_ProjectU/Data/Items/Day71/ItemData_IronOre.asset";
    private const string PickaxeItemPath = "Assets/_ProjectU/Data/Items/ItemData_Pickaxe.asset";
    private const string StarterRootName = "=== Day79 Farming Starter ===";
    private const string WetSoilName = "WetSoil";
    private const string CropAnchorName = "CropAnchor";
    private const int StarterSeedQuantity = 6;

    private sealed class ItemSpec
    {
        public string AssetName;
        public string Id;
        public string DisplayName;
        public string Description;
        public ItemCategory Category;
        public ToolType Tool;
        public float HungerRestore;
        public int MaximumStack;
        public string ModelId;
        public string PickupName;
        public float PickupScale = 1f;
        public bool LyingTool;
        public int StarterQuantity;
    }

    private static readonly ItemSpec[] ItemSpecs =
    {
        Seed("SeedPotato", "seed_potato", "POTATO SEEDS", "Seed potatoes. Plant them in a farm plot in spring.", "item_seed_potato"),
        Seed("SeedStrawberry", "seed_strawberry", "STRAWBERRY SEEDS", "Strawberry seeds. Plant them in a farm plot in spring.", "item_seed_strawberry"),
        Seed("SeedTomato", "seed_tomato", "TOMATO SEEDS", "Tomato seeds. Plant them in a farm plot in summer.", "item_seed_tomato"),
        Seed("SeedPumpkin", "seed_pumpkin", "PUMPKIN SEEDS", "Pumpkin seeds. Plant them in a farm plot in autumn.", "item_seed_pumpkin"),
        Seed("SeedWinterRadish", "seed_winter_radish", "WINTER RADISH SEEDS", "Winter radish seeds. Plant them in a farm plot in winter.", "item_seed_winter_radish"),
        Food("Potato", "food_potato", "POTATO", "A starchy potato grown in a farm plot.", 15f, 20, "item_potato"),
        Food("Strawberry", "food_strawberry", "STRAWBERRY", "A sweet spring strawberry.", 8f, 30, "item_strawberry"),
        Food("Tomato", "food_tomato", "TOMATO", "A juicy summer tomato.", 12f, 20, "item_tomato"),
        Food("Pumpkin", "food_pumpkin", "PUMPKIN", "A heavy autumn pumpkin. Great for cooking.", 30f, 10, "item_pumpkin"),
        Food("WinterRadish", "food_winter_radish", "WINTER RADISH", "A crisp radish that grows in the cold.", 14f, 20, "item_winter_radish"),
        // 119일차 새 작물 (처음 상자에는 넣지 않는다)
        NewSeed("SeedCorn", "seed_corn", "CORN SEEDS", "Corn seeds. Plant them in a farm plot in summer.", "item_seed_corn"),
        NewSeed("SeedCabbage", "seed_cabbage", "CABBAGE SEEDS", "Cabbage seeds. They grow in spring and autumn.", "item_seed_cabbage"),
        NewSeed("SeedSweetPotato", "seed_sweet_potato", "SWEET POTATO SEEDS", "Sweet potato slips. Plant them in autumn.", "item_seed_sweet_potato"),
        Food("Corn", "food_corn", "CORN", "A sweet summer corn. Animals like it too.", 18f, 20, "food_corn"),
        Food("Cabbage", "food_cabbage", "CABBAGE", "A round cabbage for wraps and hotpots.", 16f, 20, "food_cabbage"),
        Food("SweetPotato", "food_sweet_potato", "SWEET POTATO", "A sweet root that bakes well.", 20f, 20, "food_sweet_potato"),
        new ItemSpec
        {
            AssetName = "ItemData_Hoe", Id = "tool_hoe", DisplayName = "HOE",
            Description = "A basic tool that turns soil in your base into a farm plot.",
            Category = ItemCategory.Tool, Tool = ToolType.Hoe, MaximumStack = 1,
            ModelId = "tool_hoe", PickupName = "HoePickup", PickupScale = 2.4f, LyingTool = true, StarterQuantity = 1
        },
        new ItemSpec
        {
            AssetName = "ItemData_WateringCan", Id = "tool_watering_can", DisplayName = "WATERING CAN",
            Description = "A basic can for watering crops and farm plots.",
            Category = ItemCategory.Tool, Tool = ToolType.WateringCan, MaximumStack = 1,
            ModelId = "tool_watering_can", PickupName = "WateringCanPickup", PickupScale = 1.5f, StarterQuantity = 1
        }
    };

    private static ItemSpec NewSeed(string name, string id, string displayName, string description, string modelId) // 119일차: 처음 상자에 넣지 않는 씨앗
    {
        ItemSpec spec = Seed(name, id, displayName, description, modelId);
        spec.StarterQuantity = 0;
        return spec;
    }

    private static ItemSpec Seed(string name, string id, string displayName, string description, string modelId)
    {
        return new ItemSpec
        {
            AssetName = "ItemData_" + name, Id = id, DisplayName = displayName, Description = description,
            Category = ItemCategory.Seed, MaximumStack = 99, ModelId = modelId,
            PickupName = name + "Pickup", PickupScale = 1.3f, StarterQuantity = StarterSeedQuantity
        };
    }

    private static ItemSpec Food(string name, string id, string displayName, string description, float hunger, int stack, string modelId)
    {
        return new ItemSpec
        {
            AssetName = "ItemData_" + name, Id = id, DisplayName = displayName, Description = description,
            Category = ItemCategory.Food, HungerRestore = hunger, MaximumStack = stack, ModelId = modelId,
            PickupName = name + "Pickup"
        };
    }

    private sealed class CropSpec
    {
        public string AssetName;
        public string Id;
        public string DisplayName;
        public string Description;
        public CropCategory Category;
        public string SeedId;
        public string HarvestId;
        public int MinimumHarvest;
        public int MaximumHarvest;
        public int GrowthDays;
        public int[] StageDays;
        public SeasonType[] Seasons;
        public bool Greenhouse;
        public float StormDamage;
        public string ModelKey;
    }

    // 기획서 9.2 작물 성장 예시 기준 (감자·딸기·토마토·호박·겨울무)
    private static readonly CropSpec[] CropSpecs =
    {
        new CropSpec
        {
            AssetName = "CropData_Potato", Id = "crop_potato", DisplayName = "POTATO", Description = "A basic crop that grows quickly in spring.",
            Category = CropCategory.Basic, SeedId = "seed_potato", HarvestId = "food_potato", MinimumHarvest = 1, MaximumHarvest = 3,
            GrowthDays = 3, StageDays = new[] { 0, 1, 2, 3 }, Seasons = new[] { SeasonType.Spring }, Greenhouse = true, StormDamage = 0.1f, ModelKey = "potato"
        },
        new CropSpec
        {
            AssetName = "CropData_Strawberry", Id = "crop_strawberry", DisplayName = "STRAWBERRY", Description = "A seasonal spring crop with sweet berries.",
            Category = CropCategory.Seasonal, SeedId = "seed_strawberry", HarvestId = "food_strawberry", MinimumHarvest = 2, MaximumHarvest = 5,
            GrowthDays = 5, StageDays = new[] { 0, 1, 3, 5 }, Seasons = new[] { SeasonType.Spring }, Greenhouse = true, StormDamage = 0.1f, ModelKey = "strawberry"
        },
        new CropSpec
        {
            AssetName = "CropData_Tomato", Id = "crop_tomato", DisplayName = "TOMATO", Description = "A summer cooking crop grown on a stake.",
            Category = CropCategory.Cooking, SeedId = "seed_tomato", HarvestId = "food_tomato", MinimumHarvest = 2, MaximumHarvest = 4,
            GrowthDays = 6, StageDays = new[] { 0, 2, 4, 6 }, Seasons = new[] { SeasonType.Summer }, Greenhouse = true, StormDamage = 0.15f, ModelKey = "tomato"
        },
        new CropSpec
        {
            AssetName = "CropData_Pumpkin", Id = "crop_pumpkin", DisplayName = "PUMPKIN", Description = "A large autumn crop that takes a week to grow.",
            Category = CropCategory.Seasonal, SeedId = "seed_pumpkin", HarvestId = "food_pumpkin", MinimumHarvest = 1, MaximumHarvest = 2,
            GrowthDays = 7, StageDays = new[] { 0, 2, 4, 7 }, Seasons = new[] { SeasonType.Autumn }, Greenhouse = false, StormDamage = 0.05f, ModelKey = "pumpkin"
        },
        new CropSpec
        {
            AssetName = "CropData_WinterRadish", Id = "crop_winter_radish", DisplayName = "WINTER RADISH", Description = "A hardy crop that grows in winter.",
            Category = CropCategory.Seasonal, SeedId = "seed_winter_radish", HarvestId = "food_winter_radish", MinimumHarvest = 1, MaximumHarvest = 3,
            GrowthDays = 4, StageDays = new[] { 0, 1, 2, 4 }, Seasons = new[] { SeasonType.Winter }, Greenhouse = false, StormDamage = 0.1f, ModelKey = "winter_radish"
        },
        // 119일차 새 작물 (옥수수 · 양배추 · 고구마)
        new CropSpec
        {
            AssetName = "CropData_Corn", Id = "crop_corn", DisplayName = "CORN", Description = "A tall summer crop. Its grain also feeds animals.",
            Category = CropCategory.Cooking, SeedId = "seed_corn", HarvestId = "food_corn", MinimumHarvest = 2, MaximumHarvest = 3,
            GrowthDays = 6, StageDays = new[] { 0, 2, 4, 6 }, Seasons = new[] { SeasonType.Summer }, Greenhouse = true, StormDamage = 0.15f, ModelKey = "corn"
        },
        new CropSpec
        {
            AssetName = "CropData_Cabbage", Id = "crop_cabbage", DisplayName = "CABBAGE", Description = "A leafy crop that grows in spring and autumn.",
            Category = CropCategory.Basic, SeedId = "seed_cabbage", HarvestId = "food_cabbage", MinimumHarvest = 2, MaximumHarvest = 3,
            GrowthDays = 4, StageDays = new[] { 0, 1, 2, 4 }, Seasons = new[] { SeasonType.Spring, SeasonType.Autumn }, Greenhouse = true, StormDamage = 0.1f, ModelKey = "cabbage"
        },
        new CropSpec
        {
            AssetName = "CropData_SweetPotato", Id = "crop_sweet_potato", DisplayName = "SWEET POTATO", Description = "An autumn root crop that keeps well.",
            Category = CropCategory.Seasonal, SeedId = "seed_sweet_potato", HarvestId = "food_sweet_potato", MinimumHarvest = 2, MaximumHarvest = 4,
            GrowthDays = 7, StageDays = new[] { 0, 2, 4, 7 }, Seasons = new[] { SeasonType.Autumn }, Greenhouse = false, StormDamage = 0.05f, ModelKey = "sweet_potato"
        }
    };

    private static readonly string[] StageNames = { "SEEDED", "SPROUT", "GROWING", "MATURE" };

    // ---------------------------------------------------------------- 전체 생성

    public static string BuildAll()
    {
        StringBuilder report = new StringBuilder("[농사 콘텐츠 생성]\n");

        try
        {
            EnsureFolders();

            Progress("저폴리 모델 생성", 0.05f);
            int models = GenerateModels();
            report.AppendLine($"농사 모델 {models}개 생성·갱신");

            Progress("아이템 데이터", 0.2f);
            Dictionary<string, ItemData> items = new Dictionary<string, ItemData>();

            foreach (ItemSpec spec in ItemSpecs)
            {
                items[spec.Id] = CreateOrUpdateItem(spec);
            }

            report.AppendLine($"아이템 데이터 {items.Count}개 (씨앗 5 / 수확물 5 / 도구 2)");

            Progress("제작법", 0.3f);
            List<CraftingRecipeData> craftingRecipes = CreateCraftingRecipes(items, report);

            Progress("밭 Prefab", 0.4f);
            int buildingLayer = ResolveBuildingLayer();
            GameObject plotPlaced = CreateOrUpdatePlotPrefab(PlotPlacedPath, true, buildingLayer, report);
            GameObject plotPreview = CreateOrUpdatePlotPrefab(PlotPreviewPath, false, buildingLayer, report);
            BuildRecipeData plotRecipe = CreateOrUpdatePlotRecipe(plotPlaced, plotPreview);
            report.AppendLine($"밭 건축 데이터 : {plotRecipe.RecipeId} (Layer {LayerMask.LayerToName(buildingLayer)})");

            Progress("작물 데이터", 0.5f);
            List<CropData> crops = new List<CropData>();

            foreach (CropSpec spec in CropSpecs)
            {
                crops.Add(CreateOrUpdateCrop(spec, items));
            }

            report.AppendLine($"작물 데이터 {crops.Count}개");
            FarmingRulesData rules = CreateOrUpdateRules(plotRecipe);
            report.AppendLine($"농사 규칙 : {rules.RulesId}");

            Progress("월드 아이템 Prefab", 0.6f);
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

            Progress("아이템 DB와 Registry 등록", 0.7f);
            report.AppendLine(RegisterItems(items, pickups));
            AssetDatabase.SaveAssets();

            GameDataRegistryEditor.CreateOrRefreshDefaultRegistry();
            report.AppendLine("GameDataRegistry 자동 수집 완료");

            Progress("게임 Scene 연결", 0.85f);
            report.AppendLine(WireScene(craftingRecipes, plotRecipe, rules, pickups));
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

    private static void Progress(string message, float value)
    {
        EditorUtility.DisplayProgressBar(DialogTitle, message, value);
    }

    private static void EnsureFolders()
    {
        StylizedArtAssetFactory.EnsureFolder(ItemFolder);
        StylizedArtAssetFactory.EnsureFolder(CropFolder);
        StylizedArtAssetFactory.EnsureFolder(Path.GetDirectoryName(RulesPath).Replace('\\', '/'));
        StylizedArtAssetFactory.EnsureFolder(CraftingFolder);
        StylizedArtAssetFactory.EnsureFolder(Path.GetDirectoryName(BuildRecipePath).Replace('\\', '/'));
        StylizedArtAssetFactory.EnsureFolder(PickupFolder);
        StylizedArtAssetFactory.EnsureFolder(BuildingFolder);
    }

    private static int GenerateModels()
    {
        int count = 0;

        foreach (string modelId in StylizedModelLibrary.Catalog.Keys)
        {
            bool isFarmingModel = modelId.StartsWith("crop_")
                || modelId.StartsWith("item_seed_")
                || modelId == "build_farm_plot"
                || modelId == "fx_farm_plot_wet"
                || modelId == "fx_ready_sparkle"
                || modelId == "tool_hoe"
                || modelId == "tool_watering_can"
                || modelId == "prop_scarecrow"
                || modelId == "item_potato"
                || modelId == "item_strawberry"
                || modelId == "item_tomato"
                || modelId == "item_pumpkin"
                || modelId == "item_winter_radish";

            if (isFarmingModel && StylizedArtAssetFactory.GetOrCreateModelPrefab(modelId, true) != null)
            {
                count++;
            }
        }

        return count;
    }

    // ---------------------------------------------------------------- 아이템

    private static ItemData CreateOrUpdateItem(ItemSpec spec)
    {
        ItemData item = LoadOrCreateAsset<ItemData>($"{ItemFolder}/{spec.AssetName}.asset");
        SerializedObject serialized = new SerializedObject(item);
        serialized.FindProperty("itemId").stringValue = spec.Id;
        serialized.FindProperty("displayName").stringValue = spec.DisplayName;
        serialized.FindProperty("description").stringValue = spec.Description;
        serialized.FindProperty("itemCategory").intValue = (int)spec.Category;
        serialized.FindProperty("toolType").intValue = (int)spec.Tool;
        serialized.FindProperty("hungerRestoreAmount").floatValue = spec.HungerRestore;
        serialized.FindProperty("maximumStack").intValue = spec.MaximumStack;

        bool isHoe = spec.Tool == ToolType.Hoe;
        serialized.FindProperty("weaponAttackType").intValue = (int)(isHoe ? WeaponAttackType.Melee : WeaponAttackType.None);
        serialized.FindProperty("baseDamage").floatValue = isHoe ? 10f : 0f;
        serialized.FindProperty("attackCooldown").floatValue = isHoe ? 0.65f : 0.6f;
        serialized.FindProperty("attackRange").floatValue = isHoe ? 2.1f : 2f;
        serialized.FindProperty("attackRadius").floatValue = isHoe ? 0.31f : 0.4f;
        serialized.FindProperty("staminaCost").floatValue = isHoe ? 7f : 0f;
        serialized.FindProperty("impactForce").floatValue = isHoe ? 4f : 0f;

        // 괭이는 곡괭이와 같은 휘두르기 동작을 사용
        ItemData pickaxe = AssetDatabase.LoadAssetAtPath<ItemData>(PickaxeItemPath);
        serialized.FindProperty("meleeComboData").objectReferenceValue = isHoe && pickaxe != null ? pickaxe.MeleeComboData : null;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(item);
        return item;
    }

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

    // ---------------------------------------------------------------- 제작법

    private static List<CraftingRecipeData> CreateCraftingRecipes(Dictionary<string, ItemData> items, StringBuilder report)
    {
        ItemData wood = AssetDatabase.LoadAssetAtPath<ItemData>(WoodItemPath);
        ItemData stone = AssetDatabase.LoadAssetAtPath<ItemData>(StoneItemPath);
        ItemData fiber = AssetDatabase.LoadAssetAtPath<ItemData>(PlantFiberItemPath);
        ItemData ironOre = AssetDatabase.LoadAssetAtPath<ItemData>(IronOreItemPath);
        List<CraftingRecipeData> recipes = new List<CraftingRecipeData>();

        if (wood == null || stone == null || fiber == null || ironOre == null)
        {
            report.AppendLine("[경고] 기본 재료 아이템(나무·돌·식물 섬유·철광석)을 찾지 못해 제작법을 만들지 않았습니다.");
            return recipes;
        }

        // 기획서 조합법의 나무 막대기·조약돌·목재 판자·철 주괴는 아직 없어 현재 재료로 대체
        recipes.Add(CreateOrUpdateCraftingRecipe("CraftingRecipe_Hoe", "recipe_hoe", "HOE", items["tool_hoe"],
            (wood, 2), (stone, 2), (fiber, 1)));
        recipes.Add(CreateOrUpdateCraftingRecipe("CraftingRecipe_WateringCan", "recipe_watering_can", "WATERING CAN", items["tool_watering_can"],
            (wood, 2), (ironOre, 1)));
        report.AppendLine($"작업대 제작법 {recipes.Count}개 (괭이, 물뿌리개)");
        return recipes;
    }

    private static CraftingRecipeData CreateOrUpdateCraftingRecipe(string assetName, string id, string displayName, ItemData result, params (ItemData item, int amount)[] ingredients)
    {
        CraftingRecipeData recipe = LoadOrCreateAsset<CraftingRecipeData>($"{CraftingFolder}/{assetName}.asset");
        SerializedObject serialized = new SerializedObject(recipe);
        serialized.FindProperty("recipeId").stringValue = id;
        serialized.FindProperty("displayName").stringValue = displayName;
        serialized.FindProperty("requiredFacility").intValue = (int)CraftingFacilityType.Workbench;
        serialized.FindProperty("unlockType").intValue = (int)CraftingUnlockType.Default;
        serialized.FindProperty("unlockId").stringValue = string.Empty;
        serialized.FindProperty("resultItem").objectReferenceValue = result;
        serialized.FindProperty("resultQuantity").intValue = 1;
        AssignIngredients(serialized.FindProperty("ingredients"), ingredients);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(recipe);
        return recipe;
    }

    private static void AssignIngredients(SerializedProperty property, (ItemData item, int amount)[] ingredients)
    {
        property.arraySize = ingredients.Length;

        for (int index = 0; index < ingredients.Length; index++)
        {
            SerializedProperty element = property.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("itemData").objectReferenceValue = ingredients[index].item;
            element.FindPropertyRelative("amount").intValue = ingredients[index].amount;
        }
    }

    // ---------------------------------------------------------------- 밭

    private static int ResolveBuildingLayer()
    {
        GameObject reference = AssetDatabase.LoadAssetAtPath<GameObject>(LayerReferencePrefabPath);
        return reference != null ? reference.layer : 0;
    }

    private static GameObject CreateOrUpdatePlotPrefab(string path, bool placed, int layer, StringBuilder report)
    {
        bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
        GameObject root = exists
            ? PrefabUtility.LoadPrefabContents(path)
            : new GameObject(Path.GetFileNameWithoutExtension(path));

        try
        {
            ConfigurePlot(root, placed, layer, report);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            if (exists)
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            else
            {
                Object.DestroyImmediate(root);
            }
        }

        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    private static void ConfigurePlot(GameObject root, bool placed, int layer, StringBuilder report)
    {
        // 이전 저폴리 외형을 걷어내고 기본 도형 흙 상자를 기준으로 다시 맞춘다
        StylizedVisualReplacer.Restore(root, false);
        root.layer = layer;

        Transform soil = root.transform.Find("Soil");

        if (soil == null)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(cube.GetComponent<Collider>());
            cube.name = "Soil";
            cube.transform.SetParent(root.transform, false);
            soil = cube.transform;
        }

        soil.localPosition = new Vector3(0f, 0.08f, 0f);
        soil.localRotation = Quaternion.identity;
        soil.localScale = new Vector3(1f, 0.16f, 1f);
        soil.gameObject.layer = layer;

        if (placed)
        {
            BoxCollider box = root.GetComponent<BoxCollider>();

            if (box == null)
            {
                box = root.AddComponent<BoxCollider>();
            }

            box.center = new Vector3(0f, 0.08f, 0f);
            box.size = new Vector3(1f, 0.16f, 1f);

            if (root.GetComponent<WorldObjectIdentity>() == null)
            {
                root.AddComponent<WorldObjectIdentity>();
            }

            if (root.GetComponent<PlacedBuildObject>() == null)
            {
                root.AddComponent<PlacedBuildObject>();
            }

            // 80일차 밭 기능이 작물 외형을 붙일 기준점
            Transform anchor = root.transform.Find(CropAnchorName);

            if (anchor == null)
            {
                anchor = new GameObject(CropAnchorName).transform;
                anchor.SetParent(root.transform, false);
            }

            anchor.localPosition = new Vector3(0f, StylizedModelLibrary.CropAnchorHeight, 0f);
            anchor.localRotation = Quaternion.identity;
            anchor.localScale = Vector3.one;
            anchor.gameObject.layer = layer;
        }

        StylizedVisualReplacer.Options options = new StylizedVisualReplacer.Options
        {
            FitOverride = StylizedModelLibrary.FitMode.Stretch
        };

        if (!StylizedVisualReplacer.Replace(root, "build_farm_plot", options, out string message))
        {
            report.AppendLine("[오류] " + message);
            return;
        }

        if (!placed)
        {
            return;
        }

        // 물을 준 상태에서 켜는 젖은 흙 (기본은 꺼짐)
        StylizedVisualReplacement record = root.GetComponent<StylizedVisualReplacement>();
        GameObject wetPrefab = StylizedArtAssetFactory.GetOrCreateModelPrefab("fx_farm_plot_wet", false);

        if (record == null || record.GeneratedVisual == null || wetPrefab == null)
        {
            report.AppendLine("[경고] 젖은 흙 외형을 붙이지 못했습니다.");
            return;
        }

        GameObject wet = (GameObject)PrefabUtility.InstantiatePrefab(wetPrefab, record.GeneratedVisual.transform);
        wet.name = WetSoilName;
        wet.transform.localPosition = Vector3.zero;
        wet.transform.localRotation = Quaternion.identity;
        wet.transform.localScale = Vector3.one;
        StylizedVisualReplacer.SetLayerRecursively(wet.transform, layer);
        wet.SetActive(false);

        // 80일차: 칸별 작물·물 상태 컴포넌트
        FarmingGameplaySetup.ConfigurePlotComponent(root, root.transform.Find(CropAnchorName), wet);
    }

    private static BuildRecipeData CreateOrUpdatePlotRecipe(GameObject placed, GameObject preview)
    {
        BuildRecipeData recipe = LoadOrCreateAsset<BuildRecipeData>(BuildRecipePath);
        SerializedObject serialized = new SerializedObject(recipe);
        serialized.FindProperty("recipeId").stringValue = "structure_farm_plot";
        serialized.FindProperty("displayName").stringValue = "FARM PLOT";
        serialized.FindProperty("structureType").intValue = (int)BuildStructureType.None;
        serialized.FindProperty("allowGroundPlacement").boolValue = false;
        serialized.FindProperty("placementType").intValue = (int)BuildPlacementType.Floor;
        serialized.FindProperty("rotationStep").floatValue = 90f;
        serialized.FindProperty("previewOffset").vector3Value = Vector3.zero;
        serialized.FindProperty("placedPrefab").objectReferenceValue = placed;
        serialized.FindProperty("previewPrefab").objectReferenceValue = preview;
        serialized.FindProperty("placementCheckCenter").vector3Value = new Vector3(0f, 0.1f, 0f);
        serialized.FindProperty("placementCheckHalfExtents").vector3Value = new Vector3(0.48f, 0.09f, 0.48f);
        serialized.FindProperty("maximumSlopeAngle").floatValue = 15f;
        serialized.FindProperty("maximumHeightDifference").floatValue = 0.12f;
        // 기획서: 밭은 재료 제작이 아니라 괭이로 지면을 전환한다 (재료 없음, 철거 반환 없음)
        serialized.FindProperty("ingredients").arraySize = 0;
        serialized.FindProperty("requiredTool").intValue = (int)ToolType.Hoe;
        serialized.FindProperty("demolitionRefundRatio").floatValue = 0f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(recipe);
        return recipe;
    }

    // ---------------------------------------------------------------- 작물·규칙

    private static CropData CreateOrUpdateCrop(CropSpec spec, Dictionary<string, ItemData> items)
    {
        CropData crop = LoadOrCreateAsset<CropData>($"{CropFolder}/{spec.AssetName}.asset");
        SerializedObject serialized = new SerializedObject(crop);
        serialized.FindProperty("cropId").stringValue = spec.Id;
        serialized.FindProperty("displayName").stringValue = spec.DisplayName;
        serialized.FindProperty("description").stringValue = spec.Description;
        serialized.FindProperty("cropCategory").intValue = (int)spec.Category;
        serialized.FindProperty("seedItem").objectReferenceValue = items[spec.SeedId];
        serialized.FindProperty("harvestItem").objectReferenceValue = items[spec.HarvestId];
        serialized.FindProperty("minimumHarvestAmount").intValue = spec.MinimumHarvest;
        serialized.FindProperty("maximumHarvestAmount").intValue = spec.MaximumHarvest;
        serialized.FindProperty("seedReturnChance").floatValue = 0.3f;
        serialized.FindProperty("growthDays").intValue = spec.GrowthDays;
        serialized.FindProperty("requiresWater").boolValue = true;
        serialized.FindProperty("greenhouseAllowed").boolValue = spec.Greenhouse;
        serialized.FindProperty("stormDamageChance").floatValue = spec.StormDamage;

        string[] stageModels =
        {
            "crop_seeded",
            "crop_sprout",
            $"crop_{spec.ModelKey}_growing",
            $"crop_{spec.ModelKey}_mature"
        };

        SerializedProperty stages = serialized.FindProperty("growthStages");
        stages.arraySize = stageModels.Length;

        for (int index = 0; index < stageModels.Length; index++)
        {
            SerializedProperty stage = stages.GetArrayElementAtIndex(index);
            stage.FindPropertyRelative("stageName").stringValue = StageNames[index];
            stage.FindPropertyRelative("startGrowthDay").intValue = spec.StageDays[index];
            stage.FindPropertyRelative("visualPrefab").objectReferenceValue = StylizedArtAssetFactory.GetOrCreateModelPrefab(stageModels[index], false);
        }

        SerializedProperty seasons = serialized.FindProperty("growingSeasons");
        seasons.arraySize = spec.Seasons.Length;

        for (int index = 0; index < spec.Seasons.Length; index++)
        {
            seasons.GetArrayElementAtIndex(index).intValue = (int)spec.Seasons[index];
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(crop);
        return crop;
    }

    private static FarmingRulesData CreateOrUpdateRules(BuildRecipeData plotRecipe)
    {
        FarmingRulesData rules = LoadOrCreateAsset<FarmingRulesData>(RulesPath);
        SerializedObject serialized = new SerializedObject(rules);
        serialized.FindProperty("rulesId").stringValue = "farming_rules_default";
        serialized.FindProperty("farmPlotRecipe").objectReferenceValue = plotRecipe;
        serialized.FindProperty("tillingTool").intValue = (int)ToolType.Hoe;
        serialized.FindProperty("wateringTool").intValue = (int)ToolType.WateringCan;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(rules);
        return rules;
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

            if (pickup == null)
            {
                report.AppendLine($"[오류] {path}에 WorldItemPickup이 없습니다.");
                return null;
            }

            SerializedObject serialized = new SerializedObject(pickup);
            serialized.FindProperty("promptMessage").stringValue = $"F - PICK UP {spec.DisplayName}";
            serialized.FindProperty("itemData").objectReferenceValue = item;
            serialized.FindProperty("quantity").intValue = 1;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            WorldObjectIdentity identity = root.GetComponent<WorldObjectIdentity>();

            if (identity != null)
            {
                // Prefab 자체에는 저장 ID를 두지 않는다 (Scene 배치 시 발급)
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

        if (database == null)
        {
            report.AppendLine($"[오류] ItemDatabase를 찾지 못했습니다: {ItemDatabasePath}");
        }
        else
        {
            SerializedObject serialized = new SerializedObject(database);
            SerializedProperty list = serialized.FindProperty("items");
            int added = 0;

            // 농사 아이템과 함께, 이전에 등록이 빠진 아이템(활·화살 등)도 채운다.
            // ItemDatabase에 없는 아이템이 인벤토리나 월드에 있으면 저장이 실패한다.
            List<ItemData> targets = new List<ItemData>(items.Values);
            List<string> previouslyMissing = new List<string>();

            foreach (string guid in AssetDatabase.FindAssets("t:ItemData", new[] { "Assets/_ProjectU/Data" }))
            {
                ItemData projectItem = AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(guid));

                if (projectItem != null && !targets.Contains(projectItem) && !ContainsReference(list, projectItem))
                {
                    targets.Add(projectItem);
                    previouslyMissing.Add(projectItem.ItemId);
                }
            }

            foreach (ItemData item in targets)
            {
                if (ContainsReference(list, item))
                {
                    continue;
                }

                list.arraySize++;
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = item;
                added++;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
            report.AppendLine($"ItemDatabase 추가 {added}개");

            if (previouslyMissing.Count > 0)
            {
                report.AppendLine($"  └ 기존 누락 아이템 보완: {string.Join(", ", previouslyMissing)}");
            }
        }

        WorldItemPickupRegistry registry = AssetDatabase.LoadAssetAtPath<WorldItemPickupRegistry>(PickupRegistryPath);

        if (registry == null)
        {
            report.AppendLine($"[오류] 월드 아이템 Registry를 찾지 못했습니다: {PickupRegistryPath}");
            return report.ToString().TrimEnd();
        }

        SerializedObject registrySerialized = new SerializedObject(registry);
        SerializedProperty entries = registrySerialized.FindProperty("entries");
        int registered = 0;

        foreach (KeyValuePair<string, WorldItemPickup> pair in pickups)
        {
            ItemData item = items[pair.Key];
            SerializedProperty entry = null;

            for (int index = 0; index < entries.arraySize; index++)
            {
                SerializedProperty candidate = entries.GetArrayElementAtIndex(index);

                if (candidate.FindPropertyRelative("itemData").objectReferenceValue == item)
                {
                    entry = candidate;
                    break;
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

    private static string WireScene(List<CraftingRecipeData> craftingRecipes, BuildRecipeData plotRecipe, FarmingRulesData rules, Dictionary<string, WorldItemPickup> pickups)
    {
        BuildPlacementController buildController = Object.FindFirstObjectByType<BuildPlacementController>(FindObjectsInactive.Include);

        if (buildController == null)
        {
            return "[건너뜀] 현재 Scene이 게임 Scene(20_Gameplay)이 아니어서 Scene 연결을 생략했습니다.";
        }

        StringBuilder report = new StringBuilder();
        Scene scene = buildController.gameObject.scene;

        CraftingUnlockManager unlockManager = Object.FindFirstObjectByType<CraftingUnlockManager>(FindObjectsInactive.Include);
        report.AppendLine(AddReferences(unlockManager, "allRecipes", craftingRecipes.ToArray(), "제작법 해금 목록"));
        report.AppendLine(AddReferences(buildController, "buildRecipes", new Object[] { plotRecipe }, "건축 목록"));

        PlacedStructureSaveBridge saveBridge = Object.FindFirstObjectByType<PlacedStructureSaveBridge>(FindObjectsInactive.Include);
        report.AppendLine(AddReferences(saveBridge, "buildRecipes", new Object[] { plotRecipe }, "건축물 저장 목록"));

        report.AppendLine(SetupToolVisuals());
        report.AppendLine(PlaceStarterKit(pickups));
        report.AppendLine(FarmingGameplaySetup.SetupScene(rules));

        WorldObjectIdValidator.AssignAndValidateWorldObjectIds();
        report.AppendLine("월드 오브젝트 저장 ID 발급 완료");

        EditorSceneManager.MarkSceneDirty(scene);
        report.Append("Scene 변경 완료 → Ctrl+S로 저장하세요");
        return report.ToString();
    }

    private static string AddReferences(Object target, string propertyName, Object[] references, string label)
    {
        if (target == null)
        {
            return $"[경고] {label} 컴포넌트를 찾지 못했습니다.";
        }

        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty list = serialized.FindProperty(propertyName);

        if (list == null || !list.isArray)
        {
            return $"[경고] {target.GetType().Name}.{propertyName} 목록을 찾지 못했습니다.";
        }

        int added = 0;

        foreach (Object reference in references)
        {
            if (reference == null || ContainsReference(list, reference))
            {
                continue;
            }

            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = reference;
            added++;
        }

        serialized.ApplyModifiedProperties();
        return $"{label} 추가 {added}개 (전체 {list.arraySize}개)";
    }

    private static string SetupToolVisuals()
    {
        EquippedToolView toolView = Object.FindFirstObjectByType<EquippedToolView>(FindObjectsInactive.Include);

        if (toolView == null)
        {
            return "[경고] EquippedToolView를 찾지 못해 손에 든 도구 외형을 만들지 않았습니다.";
        }

        SerializedObject serialized = new SerializedObject(toolView);
        GameObject template = serialized.FindProperty("axeVisual").objectReferenceValue as GameObject;
        bool hoe = EnsureToolVisual(serialized.FindProperty("hoeVisual"), template, "HoeVisual", "tool_hoe", 1f);
        bool can = EnsureToolVisual(serialized.FindProperty("wateringCanVisual"), template, "WateringCanVisual", "tool_watering_can", 0.55f);
        serialized.ApplyModifiedProperties();
        return $"손에 든 도구 외형 : 괭이 {(hoe ? "완료" : "실패")} / 물뿌리개 {(can ? "완료" : "실패")}";
    }

    private static bool EnsureToolVisual(SerializedProperty property, GameObject template, string objectName, string modelId, float scale)
    {
        GameObject visual = property.objectReferenceValue as GameObject;

        if (visual == null)
        {
            if (template == null)
            {
                return false;
            }

            // 도끼 외형을 복제해 같은 손 위치·크기 기준을 사용
            visual = Object.Instantiate(template, template.transform.parent);
            Undo.RegisterCreatedObjectUndo(visual, "Create Farming Tool Visual");
            visual.name = objectName;
            visual.transform.SetSiblingIndex(template.transform.GetSiblingIndex() + 1);
            property.objectReferenceValue = visual;
        }

        StylizedVisualReplacer.Options options = new StylizedVisualReplacer.Options
        {
            ParentOverride = visual.transform,
            AlignModelUpToLongestAxis = true,
            BladeHintObjectName = "Head",
            ScaleMultiplier = scale,
            UseUndo = true
        };

        bool applied = StylizedVisualReplacer.Replace(visual, modelId, options, out _);
        Undo.RecordObject(visual, "Create Farming Tool Visual");
        visual.SetActive(false);
        return applied;
    }

    private static string PlaceStarterKit(Dictionary<string, WorldItemPickup> pickups)
    {
        BuildGridArea grid = Object.FindFirstObjectByType<BuildGridArea>(FindObjectsInactive.Include);

        if (grid == null)
        {
            return "[경고] BuildGridArea를 찾지 못해 농사 시작 아이템을 배치하지 않았습니다.";
        }

        SerializedObject gridSerialized = new SerializedObject(grid);
        float cellSize = gridSerialized.FindProperty("cellSize").floatValue;
        float width = gridSerialized.FindProperty("gridWidth").intValue * cellSize;
        float depth = gridSerialized.FindProperty("gridDepth").intValue * cellSize;

        // 플레이어와 가장 가까운 건축 구역 가장자리 바깥(울타리 안쪽 통로)에 한 줄로 놓는다
        Vector3 playerLocal = grid.transform.InverseTransformPoint(FindPlayerPosition(grid));
        ResolveStarterEdge(playerLocal, width, depth, out Vector3 edgeCenter, out Vector3 along, out Vector3 outward);

        GameObject root = FindOrCreateRoot(grid.gameObject.scene);
        List<ItemSpec> kit = new List<ItemSpec>();

        foreach (ItemSpec spec in ItemSpecs)
        {
            if (spec.StarterQuantity > 0 && pickups.ContainsKey(spec.Id))
            {
                kit.Add(spec);
            }
        }

        int placed = 0;
        const float spacing = 0.9f;
        const float outwardDistance = 1.1f;

        for (int index = 0; index < kit.Count; index++)
        {
            ItemSpec spec = kit[index];
            Transform existing = root.transform.Find(spec.PickupName);
            WorldItemPickup instance = existing != null ? existing.GetComponent<WorldItemPickup>() : null;

            if (instance == null)
            {
                GameObject created = (GameObject)PrefabUtility.InstantiatePrefab(pickups[spec.Id].gameObject, root.transform);
                Undo.RegisterCreatedObjectUndo(created, "Place Farming Starter");
                created.name = spec.PickupName;
                float offset = (index - (kit.Count - 1) * 0.5f) * spacing;
                Vector3 local = edgeCenter + along * offset + outward * outwardDistance;
                Vector3 world = grid.transform.TransformPoint(local);
                world.y = SampleGroundHeight(world) + 0.3f;
                created.transform.SetPositionAndRotation(world, Quaternion.Euler(0f, grid.transform.eulerAngles.y + index * 23f, 0f));
                instance = created.GetComponent<WorldItemPickup>();
                placed++;
            }

            SerializedObject serialized = new SerializedObject(instance);
            serialized.FindProperty("quantity").intValue = spec.StarterQuantity;
            serialized.ApplyModifiedProperties();
        }

        string scarecrow = PlaceScarecrow(root.transform, grid, edgeCenter, along, outward, kit.Count * spacing * 0.5f + 1.6f);
        return $"농사 시작 아이템 배치 {placed}개 (씨앗 각 {StarterSeedQuantity}개, 괭이, 물뿌리개) {scarecrow}";
    }

    private static Vector3 FindPlayerPosition(BuildGridArea grid)
    {
        PlayerMovement player = Object.FindFirstObjectByType<PlayerMovement>(FindObjectsInactive.Include);

        if (player != null)
        {
            return player.transform.position;
        }

        GameObject respawn = GameObject.Find("-- DefaultRespawnPoint --");
        return respawn != null ? respawn.transform.position : grid.transform.position;
    }

    private static void ResolveStarterEdge(Vector3 player, float width, float depth, out Vector3 center, out Vector3 along, out Vector3 outward)
    {
        const float cornerMargin = 5f;
        float clampedX = Mathf.Clamp(player.x, cornerMargin, Mathf.Max(cornerMargin, width - cornerMargin));
        float clampedZ = Mathf.Clamp(player.z, cornerMargin, Mathf.Max(cornerMargin, depth - cornerMargin));

        (Vector3 center, Vector3 along, Vector3 outward, float distance)[] edges =
        {
            (new Vector3(clampedX, 0f, 0f), Vector3.right, Vector3.back, Mathf.Abs(player.z)),
            (new Vector3(clampedX, 0f, depth), Vector3.right, Vector3.forward, Mathf.Abs(player.z - depth)),
            (new Vector3(0f, 0f, clampedZ), Vector3.forward, Vector3.left, Mathf.Abs(player.x)),
            (new Vector3(width, 0f, clampedZ), Vector3.forward, Vector3.right, Mathf.Abs(player.x - width))
        };

        int best = 0;

        for (int index = 1; index < edges.Length; index++)
        {
            if (edges[index].distance < edges[best].distance)
            {
                best = index;
            }
        }

        center = edges[best].center;
        along = edges[best].along;
        outward = edges[best].outward;
    }

    private static GameObject FindOrCreateRoot(Scene scene)
    {
        foreach (GameObject candidate in scene.GetRootGameObjects())
        {
            if (candidate.name == StarterRootName)
            {
                return candidate;
            }
        }

        GameObject root = new GameObject(StarterRootName);
        SceneManager.MoveGameObjectToScene(root, scene);
        Undo.RegisterCreatedObjectUndo(root, "Create Farming Starter Root");
        return root;
    }

    private static string PlaceScarecrow(Transform root, BuildGridArea grid, Vector3 edgeCenter, Vector3 along, Vector3 outward, float sideOffset)
    {
        if (root.Find("Scarecrow") != null)
        {
            return string.Empty;
        }

        GameObject prefab = StylizedArtAssetFactory.GetOrCreateModelPrefab("prop_scarecrow", false);

        if (prefab == null)
        {
            return string.Empty;
        }

        GameObject scarecrow = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
        Undo.RegisterCreatedObjectUndo(scarecrow, "Place Scarecrow");
        scarecrow.name = "Scarecrow";
        Vector3 world = grid.transform.TransformPoint(edgeCenter + along * sideOffset + outward * 1.1f);
        world.y = SampleGroundHeight(world);
        Vector3 facing = grid.transform.TransformDirection(-outward);
        scarecrow.transform.SetPositionAndRotation(world, Quaternion.LookRotation(facing, Vector3.up));

        CapsuleCollider capsule = scarecrow.AddComponent<CapsuleCollider>();
        capsule.center = new Vector3(0f, 0.85f, 0f);
        capsule.radius = 0.2f;
        capsule.height = 1.7f;
        return "/ 허수아비 1개";
    }

    private static float SampleGroundHeight(Vector3 world)
    {
        foreach (Terrain terrain in Terrain.activeTerrains)
        {
            Vector3 local = world - terrain.transform.position;
            Vector3 size = terrain.terrainData.size;

            if (local.x >= 0f && local.z >= 0f && local.x <= size.x && local.z <= size.z)
            {
                return terrain.SampleHeight(world) + terrain.transform.position.y;
            }
        }

        return world.y;
    }

    // ---------------------------------------------------------------- 검증

    public static string Validate(out int errorCount)
    {
        StringBuilder report = new StringBuilder("[농사 콘텐츠 검증]\n");
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
                Error($"월드 아이템 Registry 미등록 또는 Prefab 불일치: {spec.Id}");
            }

            itemCount++;
        }

        report.AppendLine($"아이템 {itemCount}/{ItemSpecs.Length}");

        if (database != null && !database.TryValidate(out string databaseError))
        {
            Error("ItemDatabase: " + databaseError);
        }

        if (database != null)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:ItemData", new[] { "Assets/_ProjectU/Data" }))
            {
                ItemData projectItem = AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(guid));

                if (projectItem != null && (!database.TryGetItem(projectItem.ItemId, out ItemData registered) || registered != projectItem))
                {
                    Error($"ItemDatabase에 없는 아이템 (저장 실패 원인): {projectItem.ItemId}");
                }
            }
        }

        if (pickupRegistry != null && !pickupRegistry.TryValidate(out string pickupError))
        {
            Error("월드 아이템 Registry: " + pickupError);
        }

        GameDataRegistry registry = AssetDatabase.LoadAssetAtPath<GameDataRegistry>("Assets/_ProjectU/Data/Registry/GameDataRegistry.asset");
        int cropCount = 0;
        HashSet<ItemData> usedSeeds = new HashSet<ItemData>();

        foreach (CropSpec spec in CropSpecs)
        {
            CropData crop = AssetDatabase.LoadAssetAtPath<CropData>($"{CropFolder}/{spec.AssetName}.asset");

            if (crop == null)
            {
                Error($"작물 없음: {spec.AssetName}");
                continue;
            }

            if (!crop.TryValidate(out string cropError))
            {
                Error(cropError);
                continue;
            }

            if (!usedSeeds.Add(crop.SeedItem))
            {
                Error($"씨앗 중복 사용: {crop.SeedItem.name}");
            }

            if (registry == null || !registry.TryGetCrop(crop.CropId, out CropData registeredCrop) || registeredCrop != crop)
            {
                Error($"GameDataRegistry 작물 미등록: {crop.CropId}");
            }
            else if (!registry.TryGetCropBySeed(crop.SeedItem, out CropData seedCrop) || seedCrop != crop)
            {
                Error($"씨앗으로 작물 검색 실패: {crop.SeedItem.ItemId}");
            }

            cropCount++;
        }

        report.AppendLine($"작물 {cropCount}/{CropSpecs.Length}");

        FarmingRulesData rules = AssetDatabase.LoadAssetAtPath<FarmingRulesData>(RulesPath);

        if (rules == null)
        {
            Error("농사 규칙 데이터 없음");
        }
        else if (!rules.TryValidate(out string rulesError))
        {
            Error(rulesError);
        }

        BuildRecipeData plotRecipe = AssetDatabase.LoadAssetAtPath<BuildRecipeData>(BuildRecipePath);

        if (plotRecipe == null || plotRecipe.PlacedPrefab == null || plotRecipe.PreviewPrefab == null)
        {
            Error("밭 건축 데이터 또는 Prefab 연결 누락");
        }
        else
        {
            GameObject placed = plotRecipe.PlacedPrefab;

            if (placed.GetComponent<PlacedBuildObject>() == null || placed.GetComponent<Collider>() == null)
            {
                Error("밭 설치 Prefab에 PlacedBuildObject 또는 Collider가 없습니다.");
            }

            if (placed.transform.Find(CropAnchorName) == null)
            {
                Error("밭 설치 Prefab에 CropAnchor가 없습니다.");
            }

            bool hasWetSoil = false;

            foreach (Transform child in placed.GetComponentsInChildren<Transform>(true))
            {
                hasWetSoil |= child.name == WetSoilName;
            }

            if (!hasWetSoil)
            {
                Error("밭 설치 Prefab에 젖은 흙 외형이 없습니다.");
            }

            FarmingGameplaySetup.ValidatePrefab(placed, Error);

            if (plotRecipe.RequiredTool != ToolType.Hoe)
            {
                Error("밭 건축 데이터에 필요 도구(괭이)가 설정되지 않았습니다.");
            }

            if (plotRecipe.PreviewPrefab.GetComponent<PlacedBuildObject>() != null)
            {
                Error("밭 미리보기 Prefab에는 PlacedBuildObject가 없어야 합니다.");
            }

            report.AppendLine("밭 건축물 확인");
        }

        foreach (string recipeName in new[] { "CraftingRecipe_Hoe", "CraftingRecipe_WateringCan" })
        {
            CraftingRecipeData recipe = AssetDatabase.LoadAssetAtPath<CraftingRecipeData>($"{CraftingFolder}/{recipeName}.asset");

            if (recipe == null || recipe.ResultItem == null || recipe.Ingredients.Count == 0)
            {
                Error($"제작법 누락 또는 설정 오류: {recipeName}");
            }
        }

        ValidateScene(plotRecipe, report, Error);

        errorCount = errors;
        report.AppendLine(errors == 0 ? "결과 : 오류 0개" : $"결과 : 오류 {errors}개");
        return report.ToString();
    }

    private static void ValidateScene(BuildRecipeData plotRecipe, StringBuilder report, System.Action<string> error)
    {
        BuildPlacementController buildController = Object.FindFirstObjectByType<BuildPlacementController>(FindObjectsInactive.Include);

        if (buildController == null)
        {
            report.AppendLine("Scene 검사 생략 (게임 Scene이 열려 있지 않음)");
            return;
        }

        if (!SceneListContains(buildController, "buildRecipes", plotRecipe))
        {
            error("BuildPlacementController 건축 목록에 밭이 없습니다.");
        }

        PlacedStructureSaveBridge saveBridge = Object.FindFirstObjectByType<PlacedStructureSaveBridge>(FindObjectsInactive.Include);

        if (saveBridge != null && !SceneListContains(saveBridge, "buildRecipes", plotRecipe))
        {
            error("PlacedStructureSaveBridge 저장 목록에 밭이 없습니다.");
        }

        CraftingUnlockManager unlockManager = Object.FindFirstObjectByType<CraftingUnlockManager>(FindObjectsInactive.Include);

        foreach (string recipeName in new[] { "CraftingRecipe_Hoe", "CraftingRecipe_WateringCan" })
        {
            CraftingRecipeData recipe = AssetDatabase.LoadAssetAtPath<CraftingRecipeData>($"{CraftingFolder}/{recipeName}.asset");

            if (unlockManager != null && recipe != null && !SceneListContains(unlockManager, "allRecipes", recipe))
            {
                error($"CraftingUnlockManager 제작법 목록에 {recipeName}이 없습니다.");
            }
        }

        EquippedToolView toolView = Object.FindFirstObjectByType<EquippedToolView>(FindObjectsInactive.Include);

        if (toolView != null)
        {
            SerializedObject serialized = new SerializedObject(toolView);

            if (serialized.FindProperty("hoeVisual").objectReferenceValue == null
                || serialized.FindProperty("wateringCanVisual").objectReferenceValue == null)
            {
                error("EquippedToolView에 괭이·물뿌리개 외형이 연결되지 않았습니다.");
            }
        }

        SeasonCycle seasonCycle = Object.FindFirstObjectByType<SeasonCycle>(FindObjectsInactive.Include);

        if (seasonCycle != null)
        {
            foreach (CropSpec spec in CropSpecs)
            {
                if (spec.GrowthDays > seasonCycle.DaysPerSeason)
                {
                    error($"{spec.Id}의 성장 일수({spec.GrowthDays})가 계절 길이({seasonCycle.DaysPerSeason})보다 깁니다.");
                }
            }
        }

        report.AppendLine("Scene 연결 확인");
        FarmingGameplaySetup.ValidateScene(report, error);
    }

    private static bool SceneListContains(Object target, string propertyName, Object reference)
    {
        if (reference == null)
        {
            return false;
        }

        SerializedProperty list = new SerializedObject(target).FindProperty(propertyName);
        return list != null && list.isArray && ContainsReference(list, reference);
    }
}
