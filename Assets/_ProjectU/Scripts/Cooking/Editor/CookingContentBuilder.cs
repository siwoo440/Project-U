using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// 85일차: 요리 콘텐츠 생성 도구
// 1. 요리 음식 데이터(효과 포함)와 요리법 데이터
// 2. 모닥불·돌 모닥불 Prefab에 요리법·조리 칸 연결
// 3. 음식 모델·아이콘·바닥 Prefab·손에 든 외형 (아이템 외형 생성 도구 재사용)
// 4. 요리 창·진행 고리·음식 효과 표시와 플레이어 효과 관리자 연결
// 여러 번 실행해도 같은 Asset·오브젝트를 갱신한다.
public static class CookingContentBuilder
{
    private const string MenuRoot = "Tools/Project U/Cooking/";
    private const string DialogTitle = "Project U 요리 콘텐츠";

    private const string ItemFolder = "Assets/_ProjectU/Data/Items/Day85";
    private const string LivestockFoodFolder = "Assets/_ProjectU/Data/Items/Day86";
    private const string RecipeFolder = "Assets/_ProjectU/Data/Cooking/Recipes";
    public const string IconSetPath = "Assets/_ProjectU/Data/Cooking/FoodEffectIcons.asset";
    private const string ItemDatabasePath = "Assets/_ProjectU/Data/Databases/ItemDatabase.asset";
    private const string CampfirePrefabPath = "Assets/_ProjectU/Prefabs/Building/CampfirePlaced.prefab";
    private const string StoneCampfirePrefabPath = "Assets/_ProjectU/Prefabs/Building/Day73/StoneCampfirePlaced.prefab";
    private const string RegistryMenuPath = "Project U/Data/Create Or Refresh Game Data Registry";

    private sealed class FoodSpec
    {
        public string AssetName;
        public string Id;
        public string DisplayName;
        public string Description;
        public float Hunger;
        public float Thirst;
        public float Health;
        public FoodBuffType Buff;
        public float BuffStrength;
        public float BuffSeconds;
        public int Stack;
        public string Folder = ItemFolder;
    }

    private static readonly FoodSpec[] FoodSpecs =
    {
        new FoodSpec
        {
            AssetName = "ItemData_BakedPotato", Id = "food_baked_potato", DisplayName = "BAKED POTATO",
            Description = "A potato baked in the embers and topped with butter.",
            Hunger = 30f, Health = 5f, Stack = 10
        },
        new FoodSpec
        {
            AssetName = "ItemData_MushroomSkewer", Id = "food_mushroom_skewer", DisplayName = "MUSHROOM SKEWER",
            Description = "Wild mushrooms grilled on a stick. Keeps you full for a while.",
            Hunger = 25f, Buff = FoodBuffType.Satiety, BuffStrength = 30f, BuffSeconds = 240f, Stack = 10
        },
        new FoodSpec
        {
            AssetName = "ItemData_GrilledFish", Id = "food_grilled_fish", DisplayName = "GRILLED FISH",
            Description = "Freshly caught fish grilled over the fire. Helps you catch your breath.",
            Hunger = 35f, Buff = FoodBuffType.StaminaRecovery, BuffStrength = 30f, BuffSeconds = 180f, Stack = 10
        },
        new FoodSpec
        {
            AssetName = "ItemData_PumpkinSoup", Id = "food_pumpkin_soup", DisplayName = "PUMPKIN SOUP",
            Description = "A creamy pumpkin soup that warms you from the inside.",
            Hunger = 40f, Thirst = 20f, Buff = FoodBuffType.Warmth, BuffStrength = 50f, BuffSeconds = 300f, Stack = 5
        },
        new FoodSpec
        {
            AssetName = "ItemData_TomatoStew", Id = "food_tomato_stew", DisplayName = "TOMATO STEW",
            Description = "A hearty stew of tomatoes and potatoes.",
            Hunger = 50f, Thirst = 15f, Health = 15f, Buff = FoodBuffType.Satiety, BuffStrength = 40f, BuffSeconds = 300f, Stack = 5
        },
        new FoodSpec
        {
            AssetName = "ItemData_GoldenFeast", Id = "food_golden_feast", DisplayName = "GOLDEN CARP FEAST",
            Description = "A rare golden carp grilled with vegetables. Light on your feet after eating.",
            Hunger = 70f, Health = 30f, Buff = FoodBuffType.MoveSpeed, BuffStrength = 12f, BuffSeconds = 300f, Stack = 3
        },
        // 86일차: 달걀·우유 요리
        new FoodSpec
        {
            AssetName = "ItemData_FriedEgg", Id = "food_fried_egg", DisplayName = "FRIED EGGS",
            Description = "Two sunny-side-up eggs. A quick breakfast that keeps you going.",
            Hunger = 25f, Buff = FoodBuffType.StaminaRecovery, BuffStrength = 20f, BuffSeconds = 120f, Stack = 10, Folder = LivestockFoodFolder
        },
        new FoodSpec
        {
            AssetName = "ItemData_VeggieOmelette", Id = "food_veggie_omelette", DisplayName = "VEGGIE OMELETTE",
            Description = "A fluffy omelette with tomato and mushroom.",
            Hunger = 45f, Health = 10f, Buff = FoodBuffType.Satiety, BuffStrength = 30f, BuffSeconds = 240f, Stack = 5, Folder = LivestockFoodFolder
        },
        new FoodSpec
        {
            AssetName = "ItemData_WarmMilk", Id = "food_warm_milk", DisplayName = "WARM MILK",
            Description = "A mug of warm milk with a drizzle of honey. Great on cold nights.",
            Hunger = 10f, Thirst = 30f, Buff = FoodBuffType.Warmth, BuffStrength = 30f, BuffSeconds = 180f, Stack = 5, Folder = LivestockFoodFolder
        }
    };

    private sealed class RecipeSpec
    {
        public string AssetName;
        public string Id;
        public string DisplayName;
        public int Order;
        public CookingStationTier Station;
        public string ResultId;
        public int ResultQuantity = 1;
        public float Seconds;
        public (string itemId, int amount)[] Ingredients;
    }

    private static readonly RecipeSpec[] RecipeSpecs =
    {
        new RecipeSpec { AssetName = "CookingRecipe_BakedApple", Id = "cook_baked_apple", DisplayName = "BAKED APPLE", Order = 0,
            Station = CookingStationTier.Campfire, ResultId = "food_baked_apple", Seconds = 5f, Ingredients = new[] { ("food_apple", 1) } },
        new RecipeSpec { AssetName = "CookingRecipe_BakedPotato", Id = "cook_baked_potato", DisplayName = "BAKED POTATO", Order = 1,
            Station = CookingStationTier.Campfire, ResultId = "food_baked_potato", Seconds = 6f, Ingredients = new[] { ("food_potato", 1) } },
        new RecipeSpec { AssetName = "CookingRecipe_MushroomSkewer", Id = "cook_mushroom_skewer", DisplayName = "MUSHROOM SKEWER", Order = 2,
            Station = CookingStationTier.Campfire, ResultId = "food_mushroom_skewer", Seconds = 5f, Ingredients = new[] { ("item_wild_mushroom", 2) } },
        new RecipeSpec { AssetName = "CookingRecipe_GrilledCrucian", Id = "cook_grilled_crucian", DisplayName = "GRILLED CRUCIAN", Order = 3,
            Station = CookingStationTier.Campfire, ResultId = "food_grilled_fish", Seconds = 8f, Ingredients = new[] { ("resource_fish_crucian", 1) } },
        new RecipeSpec { AssetName = "CookingRecipe_GrilledTrout", Id = "cook_grilled_trout", DisplayName = "GRILLED TROUT", Order = 4,
            Station = CookingStationTier.Campfire, ResultId = "food_grilled_fish", Seconds = 8f, Ingredients = new[] { ("resource_fish_trout", 1) } },
        new RecipeSpec { AssetName = "CookingRecipe_PumpkinSoup", Id = "cook_pumpkin_soup", DisplayName = "PUMPKIN SOUP", Order = 5,
            Station = CookingStationTier.StoneCampfire, ResultId = "food_pumpkin_soup", ResultQuantity = 2, Seconds = 12f,
            Ingredients = new[] { ("food_pumpkin", 1), ("drink_water_bottle", 1) } },
        new RecipeSpec { AssetName = "CookingRecipe_TomatoStew", Id = "cook_tomato_stew", DisplayName = "TOMATO STEW", Order = 6,
            Station = CookingStationTier.StoneCampfire, ResultId = "food_tomato_stew", ResultQuantity = 2, Seconds = 14f,
            Ingredients = new[] { ("food_tomato", 2), ("food_potato", 1), ("drink_water_bottle", 1) } },
        new RecipeSpec { AssetName = "CookingRecipe_GoldenFeast", Id = "cook_golden_feast", DisplayName = "GOLDEN CARP FEAST", Order = 7,
            Station = CookingStationTier.StoneCampfire, ResultId = "food_golden_feast", Seconds = 18f,
            Ingredients = new[] { ("resource_fish_golden_carp", 1), ("food_winter_radish", 1), ("food_tomato", 1) } },
        // 86일차: 가축 생산물 요리 (달걀·우유가 없으면 건너뜀)
        new RecipeSpec { AssetName = "CookingRecipe_FriedEgg", Id = "cook_fried_egg", DisplayName = "FRIED EGGS", Order = 8,
            Station = CookingStationTier.Campfire, ResultId = "food_fried_egg", Seconds = 6f, Ingredients = new[] { ("food_egg", 2) } },
        new RecipeSpec { AssetName = "CookingRecipe_VeggieOmelette", Id = "cook_veggie_omelette", DisplayName = "VEGGIE OMELETTE", Order = 9,
            Station = CookingStationTier.Campfire, ResultId = "food_veggie_omelette", Seconds = 9f,
            Ingredients = new[] { ("food_egg", 2), ("food_tomato", 1), ("item_wild_mushroom", 1) } },
        new RecipeSpec { AssetName = "CookingRecipe_WarmMilk", Id = "cook_warm_milk", DisplayName = "WARM MILK", Order = 10,
            Station = CookingStationTier.Campfire, ResultId = "food_warm_milk", Seconds = 5f, Ingredients = new[] { ("drink_milk", 1) } }
    };

    // ---------------------------------------------------------------- 메뉴

    [MenuItem(MenuRoot + "1. Build Cooking Content (Data + Campfires + Visuals + UI)", false, 0)]
    private static void BuildAllMenu()
    {
        bool confirmed = EditorUtility.DisplayDialog(
            DialogTitle,
            "요리 음식·요리법 데이터를 만들고 모닥불·돌 모닥불 Prefab에 연결합니다.\n"
            + "음식 모델·아이콘을 만들고, 현재 게임 Scene(20_Gameplay)에 요리 창·진행 고리·음식 효과 표시를 추가합니다.\n\n"
            + "실행 전에 Scene을 저장해 두세요. 실행 후 Ctrl+S로 Scene을 저장해야 반영됩니다.",
            "실행",
            "취소");

        if (!confirmed)
        {
            return;
        }

        string report = BuildAll();
        Debug.Log(report);
        EditorUtility.DisplayDialog(DialogTitle, Shorten(report), "확인");
    }

    [MenuItem(MenuRoot + "2. Validate Cooking Content", false, 1)]
    private static void ValidateMenu()
    {
        string report = Validate(out int errorCount);

        if (errorCount > 0)
        {
            Debug.LogError(report);
        }
        else
        {
            Debug.Log(report);
        }

        EditorUtility.DisplayDialog(DialogTitle, Shorten(report), "확인");
    }

    private static string Shorten(string report)
    {
        const int limit = 1800;
        return report.Length <= limit ? report : report.Substring(0, limit) + "\n... (전체 내용은 Console 참고)";
    }

    // ---------------------------------------------------------------- 전체 생성

    public static string BuildAll()
    {
        StringBuilder report = new StringBuilder("[요리 콘텐츠 생성]\n");
        List<CookingRecipeData> recipes;

        try
        {
            StylizedArtAssetFactory.EnsureFolder(ItemFolder);
            StylizedArtAssetFactory.EnsureFolder(LivestockFoodFolder);
            StylizedArtAssetFactory.EnsureFolder(RecipeFolder);

            EditorUtility.DisplayProgressBar(DialogTitle, "음식 데이터", 0.1f);
            Dictionary<string, ItemData> items = LoadItemsById();
            int foods = 0;

            foreach (FoodSpec spec in FoodSpecs)
            {
                items[spec.Id] = CreateOrUpdateFood(spec);
                foods++;
            }

            report.AppendLine($"요리 음식 데이터 {foods}개 ({ItemFolder})");
            report.AppendLine(RegisterItems(items));
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayProgressBar(DialogTitle, "요리법 데이터", 0.25f);
            recipes = new List<CookingRecipeData>();

            foreach (RecipeSpec spec in RecipeSpecs)
            {
                CookingRecipeData recipe = CreateOrUpdateRecipe(spec, items, report);

                if (recipe != null)
                {
                    recipes.Add(recipe);
                }
            }

            report.AppendLine($"요리법 {recipes.Count}개 (모닥불 {CountTier(recipes, CookingStationTier.Campfire)} / 돌 모닥불 {CountTier(recipes, CookingStationTier.StoneCampfire)})");

            EditorUtility.DisplayProgressBar(DialogTitle, "모닥불 Prefab", 0.35f);
            report.AppendLine(ConfigureStation(CampfirePrefabPath, CookingStationTier.Campfire, 2, recipes));
            report.AppendLine(ConfigureStation(StoneCampfirePrefabPath, CookingStationTier.StoneCampfire, 3, recipes));
            AssetDatabase.SaveAssets();

            if (EditorApplication.ExecuteMenuItem(RegistryMenuPath))
            {
                report.AppendLine("GameDataRegistry 자동 수집 완료");
            }
            else
            {
                report.AppendLine("[경고] GameDataRegistry 갱신 메뉴를 찾지 못했습니다.");
            }

            EditorUtility.DisplayProgressBar(DialogTitle, "요리 아이콘", 0.45f);
            UISpriteFactory.GenerateCookingIcons();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        FoodEffectIconSet iconSet = CreateOrUpdateIconSet();
        report.AppendLine($"효과 아이콘 묶음 : {IconSetPath}");

        report.AppendLine(WireScene(iconSet));

        // 음식 모델·아이콘·바닥 Prefab·손에 든 외형 (84일차 도구)
        report.AppendLine();
        report.AppendLine(ItemVisualContentBuilder.BuildAll());
        AssetDatabase.SaveAssets();

        report.AppendLine();
        report.Append(Validate(out _));
        return report.ToString();
    }

    private static int CountTier(List<CookingRecipeData> recipes, CookingStationTier tier)
    {
        int count = 0;

        foreach (CookingRecipeData recipe in recipes)
        {
            if (recipe.RequiredStation == tier)
            {
                count++;
            }
        }

        return count;
    }

    // ---------------------------------------------------------------- 데이터

    private static Dictionary<string, ItemData> LoadItemsById()
    {
        Dictionary<string, ItemData> result = new Dictionary<string, ItemData>();

        foreach (string guid in AssetDatabase.FindAssets("t:ItemData", new[] { "Assets/_ProjectU/Data" }))
        {
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(guid));

            if (item != null && !string.IsNullOrEmpty(item.ItemId) && !result.ContainsKey(item.ItemId))
            {
                result.Add(item.ItemId, item);
            }
        }

        return result;
    }

    private static ItemData CreateOrUpdateFood(FoodSpec spec)
    {
        ItemData item = LoadOrCreateAsset<ItemData>($"{spec.Folder}/{spec.AssetName}.asset");
        SerializedObject serialized = new SerializedObject(item);
        serialized.FindProperty("itemId").stringValue = spec.Id;
        serialized.FindProperty("displayName").stringValue = spec.DisplayName;
        serialized.FindProperty("description").stringValue = spec.Description;
        serialized.FindProperty("itemCategory").intValue = (int)ItemCategory.Food;
        serialized.FindProperty("toolType").intValue = (int)ToolType.None;
        serialized.FindProperty("weaponAttackType").intValue = (int)WeaponAttackType.None;
        serialized.FindProperty("baseDamage").floatValue = 0f;
        serialized.FindProperty("staminaCost").floatValue = 0f;
        serialized.FindProperty("impactForce").floatValue = 0f;
        serialized.FindProperty("hungerRestoreAmount").floatValue = spec.Hunger;
        serialized.FindProperty("foodThirstRestoreAmount").floatValue = spec.Thirst;
        serialized.FindProperty("foodHealthRestoreAmount").floatValue = spec.Health;
        serialized.FindProperty("foodBuffType").intValue = (int)spec.Buff;
        serialized.FindProperty("foodBuffStrength").floatValue = spec.BuffStrength;
        serialized.FindProperty("foodBuffDuration").floatValue = spec.BuffSeconds;
        serialized.FindProperty("maximumStack").intValue = spec.Stack;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(item);
        return item;
    }

    private static CookingRecipeData CreateOrUpdateRecipe(RecipeSpec spec, Dictionary<string, ItemData> items, StringBuilder report)
    {
        if (!items.TryGetValue(spec.ResultId, out ItemData result))
        {
            report.AppendLine($"[오류] {spec.Id} 결과 아이템을 찾지 못했습니다: {spec.ResultId}");
            return null;
        }

        foreach ((string itemId, int _) in spec.Ingredients)
        {
            if (!items.ContainsKey(itemId))
            {
                report.AppendLine($"[건너뜀] {spec.Id} : 재료 {itemId}가 아직 없습니다. (가축 콘텐츠 생성 도구를 먼저 실행하세요)");
                return null;
            }
        }

        CookingRecipeData recipe = LoadOrCreateAsset<CookingRecipeData>($"{RecipeFolder}/{spec.AssetName}.asset");
        SerializedObject serialized = new SerializedObject(recipe);
        serialized.FindProperty("recipeId").stringValue = spec.Id;
        serialized.FindProperty("displayName").stringValue = spec.DisplayName;
        serialized.FindProperty("sortOrder").intValue = spec.Order;
        serialized.FindProperty("requiredStation").intValue = (int)spec.Station;
        serialized.FindProperty("resultItem").objectReferenceValue = result;
        serialized.FindProperty("resultQuantity").intValue = spec.ResultQuantity;
        serialized.FindProperty("cookingSeconds").floatValue = spec.Seconds;
        serialized.FindProperty("extraBatchTimeRatio").floatValue = 0.6f;
        SerializedProperty ingredients = serialized.FindProperty("ingredients");
        ingredients.arraySize = spec.Ingredients.Length;

        for (int index = 0; index < spec.Ingredients.Length; index++)
        {
            SerializedProperty element = ingredients.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("itemData").objectReferenceValue = items[spec.Ingredients[index].itemId];
            element.FindPropertyRelative("amount").intValue = spec.Ingredients[index].amount;
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(recipe);
        return recipe;
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

    private static string RegisterItems(Dictionary<string, ItemData> items)
    {
        ItemDatabase database = AssetDatabase.LoadAssetAtPath<ItemDatabase>(ItemDatabasePath);

        if (database == null)
        {
            return $"[오류] ItemDatabase를 찾지 못했습니다: {ItemDatabasePath}";
        }

        SerializedObject serialized = new SerializedObject(database);
        SerializedProperty list = serialized.FindProperty("items");
        int added = 0;

        foreach (FoodSpec spec in FoodSpecs)
        {
            ItemData item = items[spec.Id];

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
        return $"ItemDatabase 추가 {added}개 (전체 {list.arraySize}개)";
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

    private static string ConfigureStation(string path, CookingStationTier tier, int slots, List<CookingRecipeData> recipes)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
        {
            return $"[경고] 모닥불 Prefab 없음: {path}";
        }

        GameObject root = PrefabUtility.LoadPrefabContents(path);

        try
        {
            CampfireCookingStation station = root.GetComponentInChildren<CampfireCookingStation>(true);

            if (station == null)
            {
                return $"[경고] {Path.GetFileName(path)}에 CampfireCookingStation이 없습니다.";
            }

            SerializedObject serialized = new SerializedObject(station);
            serialized.FindProperty("stationTier").intValue = (int)tier;
            serialized.FindProperty("slotCount").intValue = slots;
            serialized.FindProperty("maxBatchQuantity").intValue = 5;
            serialized.FindProperty("fuelAmount").intValue = 1;
            serialized.FindProperty("promptMessage").stringValue = "F - COOK";
            serialized.FindProperty("slots").arraySize = 0;
            SerializedProperty list = serialized.FindProperty("recipes");
            list.arraySize = recipes.Count;

            for (int index = 0; index < recipes.Count; index++)
            {
                list.GetArrayElementAtIndex(index).objectReferenceValue = recipes[index];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            string fuel = station.FuelItem != null ? station.FuelItem.DisplayName : "없음";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            return $"{Path.GetFileNameWithoutExtension(path)} : {CookingStationUtility.GetLabel(tier)}, 조리 칸 {slots}, 요리법 {recipes.Count}, 연료 {fuel}";
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static FoodEffectIconSet CreateOrUpdateIconSet()
    {
        StylizedArtAssetFactory.EnsureFolder(Path.GetDirectoryName(IconSetPath).Replace('\\', '/'));
        FoodEffectIconSet set = LoadOrCreateAsset<FoodEffectIconSet>(IconSetPath);
        SerializedObject serialized = new SerializedObject(set);
        serialized.FindProperty("hunger").objectReferenceValue = UISpriteFactory.Icon("Hunger");
        serialized.FindProperty("thirst").objectReferenceValue = UISpriteFactory.Icon("Thirst");
        serialized.FindProperty("health").objectReferenceValue = UISpriteFactory.Icon("Health");
        serialized.FindProperty("stamina").objectReferenceValue = UISpriteFactory.Icon("Bolt");
        serialized.FindProperty("warmth").objectReferenceValue = UISpriteFactory.Icon("Temperature");
        serialized.FindProperty("speed").objectReferenceValue = UISpriteFactory.Icon("Speed");
        serialized.FindProperty("satiety").objectReferenceValue = UISpriteFactory.Icon("Hunger");
        serialized.FindProperty("time").objectReferenceValue = UISpriteFactory.Icon("Clock");
        serialized.FindProperty("flame").objectReferenceValue = UISpriteFactory.Icon("Flame");
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(set);
        AssetDatabase.SaveAssets();
        return set;
    }

    // ---------------------------------------------------------------- Scene 연결

    private static string WireScene(FoodEffectIconSet iconSet)
    {
        GameUIManager manager = Object.FindFirstObjectByType<GameUIManager>(FindObjectsInactive.Include);
        PlayerInventory player = Object.FindFirstObjectByType<PlayerInventory>(FindObjectsInactive.Include);

        if (manager == null || player == null)
        {
            return "[건너뜀] 현재 Scene이 게임 Scene(20_Gameplay)이 아니어서 Scene 연결을 생략했습니다.";
        }

        StringBuilder report = new StringBuilder();
        Scene scene = manager.gameObject.scene;

        // 플레이어 음식 효과 관리자
        FoodBuffController buffs = player.GetComponent<FoodBuffController>();

        if (buffs == null)
        {
            buffs = Undo.AddComponent<FoodBuffController>(player.gameObject);
            report.AppendLine("플레이어에 음식 효과 관리자 추가");
        }

        HotbarItemUse itemUse = Object.FindFirstObjectByType<HotbarItemUse>(FindObjectsInactive.Include);

        if (itemUse != null)
        {
            SerializedObject useSerialized = new SerializedObject(itemUse);
            useSerialized.FindProperty("foodBuffController").objectReferenceValue = buffs;
            useSerialized.ApplyModifiedProperties();
        }

        report.AppendLine(CookingPopupUIBuilder.Build(iconSet, out CookingPopupUI popup));

        SerializedObject managerSerialized = new SerializedObject(manager);
        managerSerialized.FindProperty("cookingPopup").objectReferenceValue = popup;
        managerSerialized.ApplyModifiedProperties();
        report.AppendLine(popup != null ? "GameUIManager에 요리 창 연결" : "[경고] 요리 창을 연결하지 못했습니다.");

        EditorSceneManager.MarkSceneDirty(scene);
        report.Append("Scene 변경 완료 → Ctrl+S로 저장하세요");
        return report.ToString();
    }

    // ---------------------------------------------------------------- 검증

    public static string Validate(out int errorCount)
    {
        StringBuilder report = new StringBuilder("[요리 콘텐츠 검증]\n");
        int errors = 0;
        void Error(string message)
        {
            errors++;
            report.AppendLine("[오류] " + message);
        }

        ItemDatabase database = AssetDatabase.LoadAssetAtPath<ItemDatabase>(ItemDatabasePath);
        SerializedProperty databaseItems = database != null ? new SerializedObject(database).FindProperty("items") : null;
        Dictionary<string, ItemData> items = LoadItemsById();

        // 음식
        foreach (FoodSpec spec in FoodSpecs)
        {
            if (!items.TryGetValue(spec.Id, out ItemData food))
            {
                Error($"음식 데이터 없음: {spec.Id}");
                continue;
            }

            if (!food.IsFood || food.HungerRestoreAmount <= 0f)
            {
                Error($"{spec.Id} : 음식 분류·허기 회복량 확인 필요");
            }

            if (spec.Buff != FoodBuffType.None && food.FoodBuffType != spec.Buff)
            {
                Error($"{spec.Id} : 보조 효과가 적용되지 않았습니다.");
            }

            if (databaseItems != null && !ContainsReference(databaseItems, food))
            {
                Error($"{spec.Id} : ItemDatabase에 없음 (저장 실패 원인)");
            }

            if (food.Icon == null)
            {
                Error($"{spec.Id} : 아이콘 없음");
            }
        }

        // 요리법
        List<CookingRecipeData> recipes = new List<CookingRecipeData>();
        HashSet<string> ids = new HashSet<string>();
        int skipped = 0;

        foreach (RecipeSpec spec in RecipeSpecs)
        {
            CookingRecipeData recipe = AssetDatabase.LoadAssetAtPath<CookingRecipeData>($"{RecipeFolder}/{spec.AssetName}.asset");

            if (recipe == null)
            {
                if (HasAllIngredients(spec, items))
                {
                    Error($"요리법 없음: {spec.Id}");
                }
                else
                {
                    skipped++;
                }

                continue;
            }

            recipes.Add(recipe);

            if (!ids.Add(recipe.RecipeId) || !GameDataRegistry.IsValidContentId(recipe.RecipeId))
            {
                Error($"요리법 ID 중복 또는 형식 오류: {recipe.RecipeId}");
            }

            if (recipe.ResultItem == null)
            {
                Error($"{recipe.RecipeId} : 결과 음식 없음");
            }

            if (recipe.Ingredients.Count == 0)
            {
                Error($"{recipe.RecipeId} : 재료 없음");
            }

            foreach (CraftingIngredient ingredient in recipe.Ingredients)
            {
                if (ingredient == null || ingredient.ItemData == null)
                {
                    Error($"{recipe.RecipeId} : 빈 재료");
                }
                else if (databaseItems != null && !ContainsReference(databaseItems, ingredient.ItemData))
                {
                    Error($"{recipe.RecipeId} : 재료 {ingredient.ItemData.ItemId}가 ItemDatabase에 없음");
                }
            }
        }

        report.AppendLine($"요리법 {recipes.Count}/{RecipeSpecs.Length}개 확인{(skipped > 0 ? $" (재료가 없어 건너뜀 {skipped}개)" : string.Empty)}");

        // 모닥불
        ValidateStation(CampfirePrefabPath, CookingStationTier.Campfire, recipes, report, Error);
        ValidateStation(StoneCampfirePrefabPath, CookingStationTier.StoneCampfire, recipes, report, Error);

        // 아이콘 묶음
        FoodEffectIconSet iconSet = AssetDatabase.LoadAssetAtPath<FoodEffectIconSet>(IconSetPath);

        if (iconSet == null)
        {
            Error($"효과 아이콘 묶음 없음: {IconSetPath}");
        }
        else
        {
            SerializedObject iconSerialized = new SerializedObject(iconSet);

            foreach (string property in new[] { "hunger", "thirst", "health", "stamina", "warmth", "speed", "satiety", "time", "flame" })
            {
                if (iconSerialized.FindProperty(property).objectReferenceValue == null)
                {
                    Error($"효과 아이콘 비어 있음: {property}");
                }
            }
        }

        ValidateScene(report, Error);
        errorCount = errors;
        report.Append(errors == 0 ? "결과 : 문제 없음" : $"결과 : 오류 {errors}개");
        return report.ToString();
    }

    private static bool HasAllIngredients(RecipeSpec spec, Dictionary<string, ItemData> items)
    {
        foreach ((string itemId, int _) in spec.Ingredients)
        {
            if (!items.ContainsKey(itemId))
            {
                return false;
            }
        }

        return true;
    }

    private static void ValidateStation(string path, CookingStationTier tier, List<CookingRecipeData> recipes, StringBuilder report, System.Action<string> error)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        CampfireCookingStation station = prefab != null ? prefab.GetComponentInChildren<CampfireCookingStation>(true) : null;

        if (station == null)
        {
            error($"모닥불 Prefab 또는 조리 기능 없음: {path}");
            return;
        }

        if (station.Tier != tier)
        {
            error($"{prefab.name} : 시설 등급이 {tier}가 아닙니다.");
        }

        if (station.FuelItem == null)
        {
            error($"{prefab.name} : 연료 아이템 없음");
        }

        int cookable = 0;

        foreach (CookingRecipeData recipe in recipes)
        {
            if (station.FindRecipe(recipe.RecipeId) != recipe)
            {
                error($"{prefab.name} : 요리법 {recipe.RecipeId} 연결 안 됨");
            }

            if (station.SupportsRecipe(recipe))
            {
                cookable++;
            }
        }

        if (cookable == 0)
        {
            error($"{prefab.name} : 조리할 수 있는 요리법이 없습니다.");
        }

        report.AppendLine($"{prefab.name} : {CookingStationUtility.GetLabel(station.Tier)}, 조리 칸 {station.SlotCount}, 조리 가능 {cookable}/{recipes.Count}");
    }

    private static void ValidateScene(StringBuilder report, System.Action<string> error)
    {
        GameUIManager manager = Object.FindFirstObjectByType<GameUIManager>(FindObjectsInactive.Include);

        if (manager == null)
        {
            report.AppendLine("[건너뜀] 게임 Scene이 열려 있지 않아 Scene 검사를 생략했습니다.");
            return;
        }

        CookingPopupUI popup = manager.CookingPopup;

        if (popup == null)
        {
            error("GameUIManager에 요리 창이 연결되지 않았습니다.");
        }
        else
        {
            CheckReferences(popup, "요리 창", error);

            foreach (CookingSlotView view in popup.GetComponentsInChildren<CookingSlotView>(true))
            {
                CheckReferences(view, $"조리 칸 {view.name}", error);
            }

            foreach (CookingRecipeRowUI row in popup.GetComponentsInChildren<CookingRecipeRowUI>(true))
            {
                CheckReferences(row, "요리법 줄", error);
            }
        }

        PlayerInventory player = Object.FindFirstObjectByType<PlayerInventory>(FindObjectsInactive.Include);

        if (player == null || player.GetComponent<FoodBuffController>() == null)
        {
            error("플레이어에 FoodBuffController가 없습니다.");
        }

        HotbarItemUse itemUse = Object.FindFirstObjectByType<HotbarItemUse>(FindObjectsInactive.Include);

        if (itemUse != null && new SerializedObject(itemUse).FindProperty("foodBuffController").objectReferenceValue == null)
        {
            error("HotbarItemUse에 음식 효과 관리자가 연결되지 않았습니다.");
        }

        CookingProgressHUD progress = Object.FindFirstObjectByType<CookingProgressHUD>(FindObjectsInactive.Include);

        if (progress == null)
        {
            error("모닥불 진행 고리(LP_CookingProgressHUD)가 없습니다.");
        }
        else
        {
            CheckReferences(progress, "진행 고리", error);
        }

        FoodBuffHUD buffHud = Object.FindFirstObjectByType<FoodBuffHUD>(FindObjectsInactive.Include);

        if (buffHud == null)
        {
            error("음식 효과 표시(LP_FoodBuffHUD)가 없습니다.");
        }
        else
        {
            CheckReferences(buffHud, "음식 효과 표시", error);
        }

        report.AppendLine("Scene : 요리 창·진행 고리·음식 효과 표시·플레이어 효과 관리자 확인");
    }

    private static void CheckReferences(Object target, string label, System.Action<string> error)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.GetIterator();

        while (property.NextVisible(true))
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference || property.name == "m_Script")
            {
                continue;
            }

            if (property.objectReferenceValue == null)
            {
                error($"{label} : {property.propertyPath} 참조 없음");
            }
        }
    }
}
