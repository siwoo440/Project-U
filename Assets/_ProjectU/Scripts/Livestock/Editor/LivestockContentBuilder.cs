using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// 86일차: 가축 콘텐츠 생성 도구
// 1. 달걀·우유·사료 아이템과 사료 제작법, 닭·소 데이터
// 2. 닭장·외양간 건축물 (Prefab·건축 데이터)
// 3. 가축 관리자·우리 창·머리 위 상태 표시 Scene 연결
// 4. 달걀·우유 요리법 추가 (요리 생성 도구 실행 → 아이템 외형 생성 도구 실행)
// 여러 번 실행해도 같은 Asset·오브젝트를 갱신한다.
public static class LivestockContentBuilder
{
    private const string MenuRoot = "Tools/Project U/Livestock/";
    private const string DialogTitle = "Project U 가축 콘텐츠";

    private const string ItemFolder = "Assets/_ProjectU/Data/Items/Day86";
    private const string AnimalFolder = "Assets/_ProjectU/Data/Livestock";
    private const string CraftingFolder = "Assets/_ProjectU/Data/Crafting/Day86";
    private const string BuildRecipeFolder = "Assets/_ProjectU/Data/Building/Day86";
    private const string BuildingFolder = "Assets/_ProjectU/Prefabs/Building/Day86";
    private const string ItemDatabasePath = "Assets/_ProjectU/Data/Databases/ItemDatabase.asset";
    private const string PickupRegistryPath = "Assets/_ProjectU/Prefabs/Items/Day75/WorldItemPickupRegistry_Day75.asset";
    private const string LayerReferencePrefabPath = "Assets/_ProjectU/Prefabs/Building/Day73/WoodTablePlaced.prefab";
    private const string RegistryMenuPath = "Project U/Data/Create Or Refresh Game Data Registry";
    private const string ManagerName = "LivestockManager";
    private const string ShapeName = "Shape";
    private const string AnimalRootName = "Animals";
    private const string FeedVisualName = "FeedVisual";
    private const string ProductVisualName = "ProductVisual";
    private const string ColliderPrefix = "Collider_";

    private sealed class ItemSpec
    {
        public string AssetName;
        public string Id;
        public string DisplayName;
        public string Description;
        public ItemCategory Category;
        public float Hunger;
        public float Thirst;
        public int Stack;
    }

    private static readonly ItemSpec[] ItemSpecs =
    {
        new ItemSpec { AssetName = "ItemData_Egg", Id = "food_egg", DisplayName = "EGG", Category = ItemCategory.Food, Hunger = 6f, Stack = 20,
            Description = "A fresh egg from your chickens. Much better cooked." },
        new ItemSpec { AssetName = "ItemData_Milk", Id = "drink_milk", DisplayName = "MILK", Category = ItemCategory.Drink, Thirst = 25f, Stack = 10,
            Description = "Fresh milk from your cow. Drink it or warm it up at a campfire." },
        new ItemSpec { AssetName = "ItemData_AnimalFeed", Id = "item_animal_feed", DisplayName = "ANIMAL FEED", Category = ItemCategory.CraftingMaterial, Stack = 50,
            Description = "Chopped plant fiber for chickens and cows. Also lures new animals into a pen." }
    };

    private sealed class AnimalSpec
    {
        public string AssetName;
        public string Id;
        public string DisplayName;
        public string Plural;
        public string Prefix;
        public string ModelId;
        public float WalkSpeed;
        public int FeedPerDay;
        public int AttractCost;
        public string ProductId;
        public int ProductAmount;
        public int IntervalDays;
        public int MaxStored;
        public float BonusChance;
        public int PetGain;
    }

    private static readonly AnimalSpec[] AnimalSpecs =
    {
        new AnimalSpec { AssetName = "AnimalData_Chicken", Id = "animal_chicken", DisplayName = "CHICKEN", Plural = "CHICKENS", Prefix = "HEN",
            ModelId = "animal_chicken", WalkSpeed = 0.45f, FeedPerDay = 1, AttractCost = 4, ProductId = "food_egg", ProductAmount = 1,
            IntervalDays = 1, MaxStored = 3, BonusChance = 0.35f, PetGain = 8 },
        new AnimalSpec { AssetName = "AnimalData_Cow", Id = "animal_cow", DisplayName = "COW", Plural = "COWS", Prefix = "COW",
            ModelId = "animal_cow", WalkSpeed = 0.3f, FeedPerDay = 2, AttractCost = 8, ProductId = "drink_milk", ProductAmount = 1,
            IntervalDays = 1, MaxStored = 3, BonusChance = 0.25f, PetGain = 10 }
    };

    private sealed class PenSpec
    {
        public string Name;
        public string RecipeAsset;
        public string RecipeId;
        public string DisplayName;
        public string ModelId;
        public Vector3 Size;
        public string AnimalId;
        public int Capacity;
        public float FenceUnitHeight;
        public Vector3 HouseUnitMin;
        public Vector3 HouseUnitMax;
        public Vector3 TroughUnit;
        public float TroughUnitWidth;
        public Vector3 ProductUnit;
        public float ProductTopUnit;
        public string ProductModel;
        public float ProductScale;
        public Vector2 WanderUnitMin;
        public Vector2 WanderUnitMax;
        public (string itemId, int amount)[] Ingredients;
    }

    private static readonly PenSpec[] PenSpecs =
    {
        new PenSpec
        {
            Name = "ChickenCoop", RecipeAsset = "BuildRecipe_ChickenCoop", RecipeId = "structure_chicken_coop", DisplayName = "CHICKEN COOP",
            ModelId = "build_chicken_coop", Size = new Vector3(2.6f, 1.4f, 2.6f), AnimalId = "animal_chicken", Capacity = 3,
            FenceUnitHeight = 0.36f, HouseUnitMin = new Vector3(-0.38f, 0f, 0.12f), HouseUnitMax = new Vector3(0.22f, 1f, 0.48f),
            TroughUnit = StylizedModelLibrary.CoopTroughUnit, TroughUnitWidth = 0.24f,
            ProductUnit = StylizedModelLibrary.CoopNestUnit, ProductTopUnit = 0.085f, ProductModel = "fx_nest_eggs", ProductScale = 1f,
            WanderUnitMin = new Vector2(-0.4f, -0.42f), WanderUnitMax = new Vector2(0.4f, 0.06f),
            Ingredients = new[] { ("item_wood", 12), ("item_plant_fiber", 4) }
        },
        new PenSpec
        {
            Name = "Barn", RecipeAsset = "BuildRecipe_Barn", RecipeId = "structure_barn", DisplayName = "BARN",
            ModelId = "build_barn", Size = new Vector3(3.6f, 2.2f, 3.6f), AnimalId = "animal_cow", Capacity = 2,
            FenceUnitHeight = 0.44f, HouseUnitMin = new Vector3(-0.5f, 0f, 0.16f), HouseUnitMax = new Vector3(0.5f, 1f, 0.48f),
            TroughUnit = StylizedModelLibrary.BarnTroughUnit, TroughUnitWidth = 0.3f,
            ProductUnit = StylizedModelLibrary.BarnPailUnit, ProductTopUnit = 0.01f, ProductModel = "fx_milk_pail", ProductScale = 1.2f,
            WanderUnitMin = new Vector2(-0.3f, -0.3f), WanderUnitMax = new Vector2(0.3f, 0.02f),
            Ingredients = new[] { ("item_wood", 20), ("resource_stone", 8) }
        }
    };

    // ---------------------------------------------------------------- 메뉴

    [MenuItem(MenuRoot + "1. Build Livestock Content (Data + Pens + UI + Recipes)", false, 0)]
    private static void BuildAllMenu()
    {
        bool confirmed = EditorUtility.DisplayDialog(
            DialogTitle,
            "달걀·우유·사료와 닭·소 데이터, 닭장·외양간 건축물을 만들고\n"
            + "현재 게임 Scene(20_Gameplay)에 가축 관리자·우리 창·머리 위 상태 표시를 추가합니다.\n"
            + "이어서 요리 생성 도구(달걀·우유 요리)와 아이템 외형 생성 도구를 실행합니다.\n\n"
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

    [MenuItem(MenuRoot + "2. Validate Livestock Content", false, 1)]
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
        StringBuilder report = new StringBuilder("[가축 콘텐츠 생성]\n");
        List<AnimalData> animals = new List<AnimalData>();
        List<BuildRecipeData> penRecipes = new List<BuildRecipeData>();
        CraftingRecipeData feedRecipe = null;

        try
        {
            StylizedArtAssetFactory.EnsureFolder(ItemFolder);
            StylizedArtAssetFactory.EnsureFolder(AnimalFolder);
            StylizedArtAssetFactory.EnsureFolder(CraftingFolder);
            StylizedArtAssetFactory.EnsureFolder(BuildRecipeFolder);
            StylizedArtAssetFactory.EnsureFolder(BuildingFolder);

            EditorUtility.DisplayProgressBar(DialogTitle, "저폴리 모델", 0.05f);
            int models = 0;

            foreach (string modelId in new[] { "animal_chicken", "animal_cow", "build_chicken_coop", "build_barn", "fx_trough_feed", "fx_nest_eggs", "fx_milk_pail" })
            {
                if (StylizedArtAssetFactory.GetOrCreateModelPrefab(modelId, true) != null)
                {
                    models++;
                }
            }

            report.AppendLine($"가축 모델 {models}개 생성·갱신");

            EditorUtility.DisplayProgressBar(DialogTitle, "아이템과 동물 데이터", 0.15f);
            Dictionary<string, ItemData> items = LoadItemsById();

            foreach (ItemSpec spec in ItemSpecs)
            {
                items[spec.Id] = CreateOrUpdateItem(spec);
            }

            report.AppendLine(RegisterItems(items));

            foreach (AnimalSpec spec in AnimalSpecs)
            {
                animals.Add(CreateOrUpdateAnimal(spec, items));
            }

            report.AppendLine($"동물 데이터 {animals.Count}종 (닭 : 달걀 매일, 소 : 우유 매일)");
            feedRecipe = CreateOrUpdateFeedRecipe(items, report);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayProgressBar(DialogTitle, "닭장·외양간", 0.3f);
            int layer = ResolveBuildingLayer();

            foreach (PenSpec spec in PenSpecs)
            {
                AnimalData animal = animals.Find(a => a.AnimalId == spec.AnimalId);
                GameObject placed = CreateOrUpdatePenPrefab(spec, true, layer, animal, report);
                GameObject preview = CreateOrUpdatePenPrefab(spec, false, layer, animal, report);
                BuildRecipeData recipe = CreateOrUpdatePenRecipe(spec, placed, preview, items);
                penRecipes.Add(recipe);
                report.AppendLine($"{spec.DisplayName} : {spec.Size.x}×{spec.Size.z}m, {spec.Capacity}마리, 재료 {IngredientText(spec.Ingredients)}");
            }

            AssetDatabase.SaveAssets();

            if (EditorApplication.ExecuteMenuItem(RegistryMenuPath))
            {
                report.AppendLine("GameDataRegistry 자동 수집 완료");
            }
            else
            {
                report.AppendLine("[경고] GameDataRegistry 갱신 메뉴를 찾지 못했습니다.");
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        report.AppendLine(WireScene(animals, penRecipes, feedRecipe));

        // 달걀·우유 요리 → 요리 창·모닥불 갱신 → 아이템 외형(아이콘·동물 아이콘) 생성
        report.AppendLine();
        report.AppendLine(CookingContentBuilder.BuildAll());
        AssetDatabase.SaveAssets();

        report.AppendLine();
        report.Append(Validate(out _));
        return report.ToString();
    }

    private static string IngredientText((string itemId, int amount)[] ingredients)
    {
        List<string> parts = new List<string>();

        foreach ((string itemId, int amount) in ingredients)
        {
            parts.Add($"{itemId} x{amount}");
        }

        return string.Join(", ", parts);
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
        serialized.FindProperty("toolType").intValue = (int)ToolType.None;
        serialized.FindProperty("weaponAttackType").intValue = (int)WeaponAttackType.None;
        serialized.FindProperty("baseDamage").floatValue = 0f;
        serialized.FindProperty("staminaCost").floatValue = 0f;
        serialized.FindProperty("impactForce").floatValue = 0f;
        serialized.FindProperty("hungerRestoreAmount").floatValue = spec.Hunger;
        serialized.FindProperty("thirstRestoreAmount").floatValue = spec.Thirst;
        serialized.FindProperty("maximumStack").intValue = spec.Stack;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(item);
        return item;
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

        foreach (ItemSpec spec in ItemSpecs)
        {
            if (AddReference(list, items[spec.Id]))
            {
                added++;
            }
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(database);
        return $"가축 아이템 {ItemSpecs.Length}개 (달걀·우유·사료), ItemDatabase 추가 {added}개";
    }

    private static AnimalData CreateOrUpdateAnimal(AnimalSpec spec, Dictionary<string, ItemData> items)
    {
        AnimalData animal = LoadOrCreateAsset<AnimalData>($"{AnimalFolder}/{spec.AssetName}.asset");
        SerializedObject serialized = new SerializedObject(animal);
        serialized.FindProperty("animalId").stringValue = spec.Id;
        serialized.FindProperty("displayName").stringValue = spec.DisplayName;
        serialized.FindProperty("pluralName").stringValue = spec.Plural;
        serialized.FindProperty("individualPrefix").stringValue = spec.Prefix;
        serialized.FindProperty("modelPrefab").objectReferenceValue = StylizedArtAssetFactory.GetOrCreateModelPrefab(spec.ModelId, false);
        serialized.FindProperty("walkSpeed").floatValue = spec.WalkSpeed;
        serialized.FindProperty("feedItem").objectReferenceValue = items["item_animal_feed"];
        serialized.FindProperty("feedPerDay").intValue = spec.FeedPerDay;
        serialized.FindProperty("attractFeedCost").intValue = spec.AttractCost;
        serialized.FindProperty("productItem").objectReferenceValue = items[spec.ProductId];
        serialized.FindProperty("productAmount").intValue = spec.ProductAmount;
        serialized.FindProperty("productIntervalDays").intValue = spec.IntervalDays;
        serialized.FindProperty("maxStoredProduct").intValue = spec.MaxStored;
        serialized.FindProperty("bonusMood").intValue = 80;
        serialized.FindProperty("bonusChance").floatValue = spec.BonusChance;
        serialized.FindProperty("startMood").intValue = 60;
        serialized.FindProperty("minimumProduceMood").intValue = 30;
        serialized.FindProperty("fedMoodGain").intValue = 10;
        serialized.FindProperty("hungryMoodLoss").intValue = 25;
        serialized.FindProperty("petMoodGain").intValue = spec.PetGain;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(animal);
        return animal;
    }

    private static CraftingRecipeData CreateOrUpdateFeedRecipe(Dictionary<string, ItemData> items, StringBuilder report)
    {
        if (!items.TryGetValue("item_plant_fiber", out ItemData fiber))
        {
            report.AppendLine("[경고] 식물 섬유 아이템이 없어 사료 제작법을 만들지 못했습니다.");
            return null;
        }

        CraftingRecipeData recipe = LoadOrCreateAsset<CraftingRecipeData>($"{CraftingFolder}/CraftingRecipe_AnimalFeed.asset");
        SerializedObject serialized = new SerializedObject(recipe);
        serialized.FindProperty("recipeId").stringValue = "recipe_animal_feed";
        serialized.FindProperty("displayName").stringValue = "ANIMAL FEED";
        serialized.FindProperty("requiredFacility").intValue = (int)CraftingFacilityType.Hand;
        serialized.FindProperty("unlockType").intValue = (int)CraftingUnlockType.Default;
        serialized.FindProperty("unlockId").stringValue = string.Empty;
        serialized.FindProperty("resultItem").objectReferenceValue = items["item_animal_feed"];
        serialized.FindProperty("resultQuantity").intValue = 3;
        SerializedProperty ingredients = serialized.FindProperty("ingredients");
        ingredients.arraySize = 1;
        ingredients.GetArrayElementAtIndex(0).FindPropertyRelative("itemData").objectReferenceValue = fiber;
        ingredients.GetArrayElementAtIndex(0).FindPropertyRelative("amount").intValue = 2;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(recipe);
        report.AppendLine("사료 제작법 (맨손) : 식물 섬유 2 → 사료 3");
        return recipe;
    }

    // ---------------------------------------------------------------- 우리 Prefab

    private static int ResolveBuildingLayer()
    {
        GameObject reference = AssetDatabase.LoadAssetAtPath<GameObject>(LayerReferencePrefabPath);
        return reference != null ? reference.layer : 0;
    }

    private static Vector3 UnitToLocal(PenSpec spec, Vector3 unit)
    {
        return Vector3.Scale(unit, spec.Size);
    }

    private static GameObject CreateOrUpdatePenPrefab(PenSpec spec, bool placed, int layer, AnimalData animal, StringBuilder report)
    {
        string path = $"{BuildingFolder}/{spec.Name}{(placed ? "Placed" : "Preview")}.prefab";
        bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
        GameObject root = exists ? PrefabUtility.LoadPrefabContents(path) : new GameObject(Path.GetFileNameWithoutExtension(path));

        try
        {
            ConfigurePen(root, spec, placed, layer, animal, report);
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

    private static void ConfigurePen(GameObject root, PenSpec spec, bool placed, int layer, AnimalData animal, StringBuilder report)
    {
        StylizedVisualReplacer.Restore(root, false);
        root.layer = layer;

        // 이전 실행에서 만든 하위 오브젝트 정리
        for (int index = root.transform.childCount - 1; index >= 0; index--)
        {
            Transform child = root.transform.GetChild(index);

            if (child.name.StartsWith(ColliderPrefix) || child.name == AnimalRootName || child.name == FeedVisualName || child.name == ProductVisualName)
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }

        foreach (Collider collider in root.GetComponents<Collider>())
        {
            Object.DestroyImmediate(collider);
        }

        // 크기 기준 상자 (저폴리 모델로 교체된다)
        Transform shape = root.transform.Find(ShapeName);

        if (shape == null)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(cube.GetComponent<Collider>());
            cube.name = ShapeName;
            cube.transform.SetParent(root.transform, false);
            shape = cube.transform;
        }

        shape.localPosition = new Vector3(0f, spec.Size.y * 0.5f, 0f);
        shape.localRotation = Quaternion.identity;
        shape.localScale = spec.Size;
        shape.gameObject.layer = layer;

        StylizedVisualReplacer.Options options = new StylizedVisualReplacer.Options { FitOverride = StylizedModelLibrary.FitMode.Stretch };

        if (!StylizedVisualReplacer.Replace(root, spec.ModelId, options, out string message))
        {
            report.AppendLine("[오류] " + message);
        }

        if (!placed)
        {
            return;
        }

        // 울타리와 건물 충돌체 (플레이어가 우리 안으로 들어가지 않게)
        float fenceHeight = spec.FenceUnitHeight * spec.Size.y + 0.1f;
        float halfX = spec.Size.x * 0.5f;
        float halfZ = spec.Size.z * 0.5f;
        AddBoxCollider(root, "Fence_Front", new Vector3(0f, fenceHeight * 0.5f, -halfZ + 0.04f), new Vector3(spec.Size.x, fenceHeight, 0.12f), layer);
        AddBoxCollider(root, "Fence_Back", new Vector3(0f, fenceHeight * 0.5f, halfZ - 0.04f), new Vector3(spec.Size.x, fenceHeight, 0.12f), layer);
        AddBoxCollider(root, "Fence_Left", new Vector3(-halfX + 0.04f, fenceHeight * 0.5f, 0f), new Vector3(0.12f, fenceHeight, spec.Size.z), layer);
        AddBoxCollider(root, "Fence_Right", new Vector3(halfX - 0.04f, fenceHeight * 0.5f, 0f), new Vector3(0.12f, fenceHeight, spec.Size.z), layer);
        Vector3 houseMin = UnitToLocal(spec, spec.HouseUnitMin);
        Vector3 houseMax = UnitToLocal(spec, spec.HouseUnitMax);
        AddBoxCollider(root, "House", (houseMin + houseMax) * 0.5f, houseMax - houseMin, layer);

        if (root.GetComponent<WorldObjectIdentity>() == null)
        {
            root.AddComponent<WorldObjectIdentity>();
        }

        if (root.GetComponent<PlacedBuildObject>() == null)
        {
            root.AddComponent<PlacedBuildObject>();
        }

        WorldObjectIdentity identity = root.GetComponent<WorldObjectIdentity>();
        SerializedObject identitySerialized = new SerializedObject(identity);
        identitySerialized.FindProperty("worldObjectId").stringValue = string.Empty;
        identitySerialized.ApplyModifiedPropertiesWithoutUndo();

        GameObject animalRoot = new GameObject(AnimalRootName);
        animalRoot.layer = layer;
        animalRoot.transform.SetParent(root.transform, false);

        // 먹이통 사료와 생산물 외형
        Vector3 trough = UnitToLocal(spec, spec.TroughUnit) + Vector3.up * (0.058f * spec.Size.y);
        GameObject feed = InstantiateModel("fx_trough_feed", root.transform, FeedVisualName, trough, spec.TroughUnitWidth * spec.Size.x / 0.62f, layer);
        Vector3 product = UnitToLocal(spec, spec.ProductUnit) + Vector3.up * (spec.ProductTopUnit * spec.Size.y);
        GameObject productVisual = InstantiateModel(spec.ProductModel, root.transform, ProductVisualName, product, spec.ProductScale, layer);

        AnimalPen pen = root.GetComponent<AnimalPen>();

        if (pen == null)
        {
            pen = root.AddComponent<AnimalPen>();
        }

        Vector2 wanderMin = Vector2.Scale(spec.WanderUnitMin, new Vector2(spec.Size.x, spec.Size.z));
        Vector2 wanderMax = Vector2.Scale(spec.WanderUnitMax, new Vector2(spec.Size.x, spec.Size.z));
        SerializedObject penSerialized = new SerializedObject(pen);
        penSerialized.FindProperty("promptMessage").stringValue = $"F - {spec.DisplayName}";
        penSerialized.FindProperty("penDisplayName").stringValue = spec.DisplayName;
        penSerialized.FindProperty("acceptedAnimal").objectReferenceValue = animal;
        penSerialized.FindProperty("capacity").intValue = spec.Capacity;
        penSerialized.FindProperty("animalRoot").objectReferenceValue = animalRoot.transform;
        penSerialized.FindProperty("wanderCenter").vector3Value = new Vector3((wanderMin.x + wanderMax.x) * 0.5f, 0.02f, (wanderMin.y + wanderMax.y) * 0.5f);
        penSerialized.FindProperty("wanderSize").vector2Value = wanderMax - wanderMin;
        penSerialized.FindProperty("feedVisual").objectReferenceValue = feed;
        penSerialized.FindProperty("productVisual").objectReferenceValue = productVisual;
        penSerialized.FindProperty("animals").arraySize = 0;
        penSerialized.FindProperty("lastProcessedDay").intValue = 0;
        penSerialized.FindProperty("hasDayRecord").boolValue = false;
        penSerialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AddBoxCollider(GameObject root, string name, Vector3 center, Vector3 size, int layer)
    {
        GameObject holder = new GameObject(ColliderPrefix + name);
        holder.layer = layer;
        holder.transform.SetParent(root.transform, false);
        BoxCollider box = holder.AddComponent<BoxCollider>();
        box.center = center;
        box.size = size;
    }

    private static GameObject InstantiateModel(string modelId, Transform parent, string name, Vector3 localPosition, float scale, int layer)
    {
        GameObject prefab = StylizedArtAssetFactory.GetOrCreateModelPrefab(modelId, false);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.name = name;
        instance.transform.localPosition = localPosition;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one * scale;
        StylizedVisualReplacer.SetLayerRecursively(instance.transform, layer);
        instance.SetActive(false);
        return instance;
    }

    private static BuildRecipeData CreateOrUpdatePenRecipe(PenSpec spec, GameObject placed, GameObject preview, Dictionary<string, ItemData> items)
    {
        BuildRecipeData recipe = LoadOrCreateAsset<BuildRecipeData>($"{BuildRecipeFolder}/{spec.RecipeAsset}.asset");
        SerializedObject serialized = new SerializedObject(recipe);
        serialized.FindProperty("recipeId").stringValue = spec.RecipeId;
        serialized.FindProperty("displayName").stringValue = spec.DisplayName;
        serialized.FindProperty("structureType").intValue = (int)BuildStructureType.Furniture;
        serialized.FindProperty("allowGroundPlacement").boolValue = true;
        serialized.FindProperty("placementType").intValue = (int)BuildPlacementType.Free;
        serialized.FindProperty("rotationStep").floatValue = 90f;
        serialized.FindProperty("previewOffset").vector3Value = Vector3.zero;
        serialized.FindProperty("placedPrefab").objectReferenceValue = placed;
        serialized.FindProperty("previewPrefab").objectReferenceValue = preview;
        serialized.FindProperty("placementCheckCenter").vector3Value = new Vector3(0f, spec.Size.y * 0.5f + 0.05f, 0f);
        serialized.FindProperty("placementCheckHalfExtents").vector3Value = new Vector3(spec.Size.x * 0.5f - 0.05f, spec.Size.y * 0.5f - 0.05f, spec.Size.z * 0.5f - 0.05f);
        serialized.FindProperty("maximumSlopeAngle").floatValue = 18f;
        serialized.FindProperty("maximumHeightDifference").floatValue = 0.35f;
        serialized.FindProperty("requiredTool").intValue = (int)ToolType.None;
        serialized.FindProperty("demolitionRefundRatio").floatValue = 0.5f;
        SerializedProperty ingredients = serialized.FindProperty("ingredients");
        ingredients.arraySize = spec.Ingredients.Length;

        for (int index = 0; index < spec.Ingredients.Length; index++)
        {
            SerializedProperty element = ingredients.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("itemData").objectReferenceValue = items.TryGetValue(spec.Ingredients[index].itemId, out ItemData item) ? item : null;
            element.FindPropertyRelative("amount").intValue = spec.Ingredients[index].amount;
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(recipe);
        return recipe;
    }

    // ---------------------------------------------------------------- Scene 연결

    private static string WireScene(List<AnimalData> animals, List<BuildRecipeData> penRecipes, CraftingRecipeData feedRecipe)
    {
        GameUIManager uiManager = Object.FindFirstObjectByType<GameUIManager>(FindObjectsInactive.Include);
        BuildPlacementController buildController = Object.FindFirstObjectByType<BuildPlacementController>(FindObjectsInactive.Include);

        if (uiManager == null || buildController == null)
        {
            return "[건너뜀] 현재 Scene이 게임 Scene(20_Gameplay)이 아니어서 Scene 연결을 생략했습니다.";
        }

        StringBuilder report = new StringBuilder();
        Scene scene = uiManager.gameObject.scene;

        // 가축 관리자
        LivestockManager manager = Object.FindFirstObjectByType<LivestockManager>(FindObjectsInactive.Include);

        if (manager == null)
        {
            GameObject created = new GameObject(ManagerName);
            SceneManager.MoveGameObjectToScene(created, scene);
            Undo.RegisterCreatedObjectUndo(created, "Create Livestock Manager");
            manager = Undo.AddComponent<LivestockManager>(created);
        }

        SerializedObject managerSerialized = new SerializedObject(manager);
        managerSerialized.FindProperty("dayNightCycle").objectReferenceValue = Object.FindFirstObjectByType<DayNightCycle>(FindObjectsInactive.Include);
        SerializedProperty animalList = managerSerialized.FindProperty("animals");
        animalList.arraySize = animals.Count;

        for (int index = 0; index < animals.Count; index++)
        {
            animalList.GetArrayElementAtIndex(index).objectReferenceValue = animals[index];
        }

        managerSerialized.FindProperty("pickupRegistry").objectReferenceValue = AssetDatabase.LoadAssetAtPath<WorldItemPickupRegistry>(PickupRegistryPath);
        managerSerialized.FindProperty("dropContainer").objectReferenceValue = Object.FindFirstObjectByType<WorldItemDropContainer>(FindObjectsInactive.Include);
        managerSerialized.ApplyModifiedProperties();
        report.AppendLine($"가축 관리자 : {manager.name} (동물 {animals.Count}종)");

        report.AppendLine(AddReferences(buildController, "buildRecipes", penRecipes.ToArray(), "건축 목록"));
        PlacedStructureSaveBridge saveBridge = Object.FindFirstObjectByType<PlacedStructureSaveBridge>(FindObjectsInactive.Include);
        report.AppendLine(AddReferences(saveBridge, "buildRecipes", penRecipes.ToArray(), "건축물 저장 목록"));

        if (feedRecipe != null)
        {
            CraftingUnlockManager unlockManager = Object.FindFirstObjectByType<CraftingUnlockManager>(FindObjectsInactive.Include);
            report.AppendLine(AddReferences(unlockManager, "allRecipes", new Object[] { feedRecipe }, "제작법 목록"));
        }

        FoodEffectIconSet iconSet = AssetDatabase.LoadAssetAtPath<FoodEffectIconSet>(CookingContentBuilder.IconSetPath);
        report.AppendLine(LivestockPopupUIBuilder.Build(iconSet, out AnimalPenPopupUI popup));
        SerializedObject uiSerialized = new SerializedObject(uiManager);
        uiSerialized.FindProperty("animalPenPopup").objectReferenceValue = popup;
        uiSerialized.ApplyModifiedProperties();
        report.AppendLine(popup != null ? "GameUIManager에 우리 창 연결" : "[경고] 우리 창을 연결하지 못했습니다.");

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
            if (reference != null && AddReference(list, reference))
            {
                added++;
            }
        }

        serialized.ApplyModifiedProperties();
        return $"{label} 추가 {added}개 (전체 {list.arraySize}개)";
    }

    private static bool AddReference(SerializedProperty list, Object target)
    {
        for (int index = 0; index < list.arraySize; index++)
        {
            if (list.GetArrayElementAtIndex(index).objectReferenceValue == target)
            {
                return false;
            }
        }

        list.arraySize++;
        list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = target;
        return true;
    }

    // ---------------------------------------------------------------- 검증

    public static string Validate(out int errorCount)
    {
        StringBuilder report = new StringBuilder("[가축 콘텐츠 검증]\n");
        int errors = 0;
        void Error(string message)
        {
            errors++;
            report.AppendLine("[오류] " + message);
        }

        Dictionary<string, ItemData> items = LoadItemsById();
        ItemDatabase database = AssetDatabase.LoadAssetAtPath<ItemDatabase>(ItemDatabasePath);
        SerializedProperty databaseItems = database != null ? new SerializedObject(database).FindProperty("items") : null;

        foreach (ItemSpec spec in ItemSpecs)
        {
            if (!items.TryGetValue(spec.Id, out ItemData item))
            {
                Error($"아이템 없음: {spec.Id}");
                continue;
            }

            if (databaseItems != null && !ContainsReference(databaseItems, item))
            {
                Error($"{spec.Id} : ItemDatabase에 없음");
            }

            if (item.Icon == null)
            {
                Error($"{spec.Id} : 아이콘 없음");
            }
        }

        foreach (AnimalSpec spec in AnimalSpecs)
        {
            AnimalData animal = AssetDatabase.LoadAssetAtPath<AnimalData>($"{AnimalFolder}/{spec.AssetName}.asset");

            if (animal == null)
            {
                Error($"동물 데이터 없음: {spec.Id}");
                continue;
            }

            if (animal.ModelPrefab == null || animal.FeedItem == null || animal.ProductItem == null)
            {
                Error($"{spec.Id} : 모델·먹이·생산물 연결이 비어 있습니다.");
            }

            if (animal.Icon == null)
            {
                Error($"{spec.Id} : 동물 아이콘 없음");
            }
        }

        CraftingRecipeData feedRecipe = AssetDatabase.LoadAssetAtPath<CraftingRecipeData>($"{CraftingFolder}/CraftingRecipe_AnimalFeed.asset");

        if (feedRecipe == null || feedRecipe.ResultItem == null)
        {
            Error("사료 제작법이 없습니다.");
        }

        foreach (PenSpec spec in PenSpecs)
        {
            ValidatePen(spec, report, Error);
        }

        ValidateScene(report, Error);
        errorCount = errors;
        report.Append(errors == 0 ? "결과 : 문제 없음" : $"결과 : 오류 {errors}개");
        return report.ToString();
    }

    private static void ValidatePen(PenSpec spec, StringBuilder report, System.Action<string> error)
    {
        BuildRecipeData recipe = AssetDatabase.LoadAssetAtPath<BuildRecipeData>($"{BuildRecipeFolder}/{spec.RecipeAsset}.asset");

        if (recipe == null || recipe.PlacedPrefab == null || recipe.PreviewPrefab == null)
        {
            error($"{spec.DisplayName} 건축 데이터 또는 Prefab 없음");
            return;
        }

        foreach (CraftingIngredient ingredient in recipe.Ingredients)
        {
            if (ingredient == null || ingredient.ItemData == null)
            {
                error($"{spec.DisplayName} : 빈 건축 재료");
            }
        }

        GameObject placed = recipe.PlacedPrefab;
        AnimalPen pen = placed.GetComponent<AnimalPen>();

        if (pen == null || pen.AcceptedAnimal == null || pen.AcceptedAnimal.AnimalId != spec.AnimalId)
        {
            error($"{spec.DisplayName} : 우리 기능 또는 동물 연결 오류");
            return;
        }

        if (placed.GetComponent<PlacedBuildObject>() == null || placed.GetComponent<WorldObjectIdentity>() == null)
        {
            error($"{spec.DisplayName} : 건축물 저장 컴포넌트 없음");
        }

        if (placed.GetComponentsInChildren<BoxCollider>(true).Length < 5)
        {
            error($"{spec.DisplayName} : 울타리·건물 충돌체가 부족합니다.");
        }

        if (placed.GetComponent<StylizedVisualReplacement>() == null || recipe.PreviewPrefab.GetComponent<StylizedVisualReplacement>() == null)
        {
            error($"{spec.DisplayName} : 저폴리 외형이 적용되지 않았습니다.");
        }

        if (placed.GetComponentInChildren<AnimalPen>(true) != pen)
        {
            error($"{spec.DisplayName} : 우리 기능이 여러 개입니다.");
        }

        SerializedObject serialized = new SerializedObject(pen);

        foreach (string property in new[] { "animalRoot", "feedVisual", "productVisual" })
        {
            if (serialized.FindProperty(property).objectReferenceValue == null)
            {
                error($"{spec.DisplayName} : {property} 연결 없음");
            }
        }

        report.AppendLine($"{spec.DisplayName} : {pen.AcceptedAnimal.DisplayName} {pen.Capacity}마리, 충돌체 {placed.GetComponentsInChildren<BoxCollider>(true).Length}개");
    }

    private static void ValidateScene(StringBuilder report, System.Action<string> error)
    {
        GameUIManager uiManager = Object.FindFirstObjectByType<GameUIManager>(FindObjectsInactive.Include);

        if (uiManager == null)
        {
            report.AppendLine("[건너뜀] 게임 Scene이 열려 있지 않아 Scene 검사를 생략했습니다.");
            return;
        }

        LivestockManager manager = Object.FindFirstObjectByType<LivestockManager>(FindObjectsInactive.Include);

        if (manager == null)
        {
            error("Scene에 LivestockManager가 없습니다.");
        }
        else
        {
            SerializedObject serialized = new SerializedObject(manager);

            foreach (string property in new[] { "dayNightCycle", "pickupRegistry" })
            {
                if (serialized.FindProperty(property).objectReferenceValue == null)
                {
                    error($"LivestockManager.{property} 연결 없음");
                }
            }

            if (manager.Animals.Count < AnimalSpecs.Length)
            {
                error("LivestockManager 동물 목록이 부족합니다.");
            }
        }

        BuildPlacementController buildController = Object.FindFirstObjectByType<BuildPlacementController>(FindObjectsInactive.Include);
        PlacedStructureSaveBridge saveBridge = Object.FindFirstObjectByType<PlacedStructureSaveBridge>(FindObjectsInactive.Include);

        foreach (PenSpec spec in PenSpecs)
        {
            BuildRecipeData recipe = AssetDatabase.LoadAssetAtPath<BuildRecipeData>($"{BuildRecipeFolder}/{spec.RecipeAsset}.asset");

            if (!ListContains(buildController, "buildRecipes", recipe))
            {
                error($"건축 목록에 {spec.DisplayName}이(가) 없습니다.");
            }

            if (!ListContains(saveBridge, "buildRecipes", recipe))
            {
                error($"건축물 저장 목록에 {spec.DisplayName}이(가) 없습니다.");
            }
        }

        CraftingRecipeData feedRecipe = AssetDatabase.LoadAssetAtPath<CraftingRecipeData>($"{CraftingFolder}/CraftingRecipe_AnimalFeed.asset");

        if (!ListContains(Object.FindFirstObjectByType<CraftingUnlockManager>(FindObjectsInactive.Include), "allRecipes", feedRecipe))
        {
            error("제작법 목록에 사료가 없습니다.");
        }

        AnimalPenPopupUI popup = uiManager.AnimalPenPopup;

        if (popup == null)
        {
            error("GameUIManager에 우리 창이 연결되지 않았습니다.");
        }
        else
        {
            CheckReferences(popup, "우리 창", error);

            foreach (AnimalCardUI card in popup.GetComponentsInChildren<AnimalCardUI>(true))
            {
                CheckReferences(card, "동물 카드", error);
            }
        }

        AnimalStatusHUD status = Object.FindFirstObjectByType<AnimalStatusHUD>(FindObjectsInactive.Include);

        if (status == null)
        {
            error("동물 머리 위 상태 표시(LP_AnimalStatusHUD)가 없습니다.");
        }
        else
        {
            CheckReferences(status, "상태 표시", error);
        }

        report.AppendLine("Scene : 가축 관리자·건축 목록·저장 목록·제작법·우리 창·상태 표시 확인");
    }

    private static bool ListContains(Object target, string propertyName, Object reference)
    {
        if (target == null || reference == null)
        {
            return false;
        }

        SerializedProperty list = new SerializedObject(target).FindProperty(propertyName);
        return list != null && ContainsReference(list, reference);
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

    private static void CheckReferences(Object target, string label, System.Action<string> error)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.GetIterator();

        while (property.NextVisible(true))
        {
            if (property.propertyType == SerializedPropertyType.ObjectReference && property.name != "m_Script" && property.objectReferenceValue == null)
            {
                error($"{label} : {property.propertyPath} 참조 없음");
            }
        }
    }
}
