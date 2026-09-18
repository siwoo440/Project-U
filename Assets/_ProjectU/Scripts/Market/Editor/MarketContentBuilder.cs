using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// 87일차: 판매·상점 콘텐츠 생성 도구
// 1. 판매 가격표 (작물·채집물·물고기·가축 생산물·요리·재료) 와 떠돌이 상인 재고
// 2. 판매 상자 · 상인 가판대 건축물 (Prefab · 건축 데이터)
// 3. 상점 관리자 · 플레이어 지갑 · 상인 창 · 코인 표시 · 판매 알림 · 보관함 창 안내 Scene 연결
// 여러 번 실행해도 같은 Asset·오브젝트를 갱신한다.
public static class MarketContentBuilder
{
    public const string BuildMenuRoot = "Tools/Project U/Build Content/";
    private const string DialogTitle = "Project U 판매·상점 콘텐츠";

    public const string DataFolder = "Assets/_ProjectU/Data/Market";
    public const string CatalogPath = DataFolder + "/MarketCatalog.asset";
    public const string StorageTypePath = "Assets/_ProjectU/Data/Storage/StorageType_ShippingBin.asset";
    public const string StorageTypeId = "storage_shipping_bin";
    private const string BuildRecipeFolder = "Assets/_ProjectU/Data/Building/Day87";
    private const string BuildingFolder = "Assets/_ProjectU/Prefabs/Building/Day87";
    private const string IconFolder = "Assets/_ProjectU/UI/Icons/Market";
    public const string MerchantIconPath = IconFolder + "/ICON_merchant.png";
    private const string LayerReferencePrefabPath = "Assets/_ProjectU/Prefabs/Building/Day73/WoodTablePlaced.prefab";
    private const string ManagerName = "MarketManager";
    private const string ShapeName = "Shape";
    private const string FlagName = "Flag";
    private const string MerchantName = "Merchant";
    private const string ColliderPrefix = "Collider_";

    // 고정 판매 가격 (물고기·요리·씨앗은 규칙으로 계산)
    private static readonly (string id, int price, MarketGoodsType type)[] FixedPrices =
    {
        ("food_potato", 8, MarketGoodsType.Crop),
        ("food_strawberry", 7, MarketGoodsType.Crop), // 95일차: 5 → 7 (다른 작물보다 하루 이익이 낮던 문제)
        ("food_tomato", 9, MarketGoodsType.Crop),
        ("food_pumpkin", 28, MarketGoodsType.Crop),
        ("food_winter_radish", 12, MarketGoodsType.Crop),
        ("food_apple", 4, MarketGoodsType.Forage),
        ("food_berry", 2, MarketGoodsType.Forage),
        ("item_wild_mushroom", 6, MarketGoodsType.Forage),
        ("food_egg", 10, MarketGoodsType.AnimalProduct),
        ("drink_milk", 16, MarketGoodsType.AnimalProduct),
        ("drink_water_bottle", 3, MarketGoodsType.Drink),
        ("item_herbal_tea", 9, MarketGoodsType.Drink),
        ("medicine_bandage", 6, MarketGoodsType.Supply),
        ("item_arrow", 1, MarketGoodsType.Supply),
        ("resource_worm_bait", 1, MarketGoodsType.Supply),
        ("item_wood", 1, MarketGoodsType.Material),
        ("resource_stone", 1, MarketGoodsType.Material),
        ("item_plant_fiber", 1, MarketGoodsType.Material),
        ("item_iron_ore", 7, MarketGoodsType.Material),
        ("item_animal_feed", 1, MarketGoodsType.Material)
    };

    // 물고기 : 기본 14 × 희귀도 배율 × (1 + 난이도 × 0.5)
    private const int FishBasePrice = 14;
    private static readonly float[] FishRarityMultiplier = { 1f, 2f, 5f, 12f };

    // 요리 : (재료 가격 합 ÷ 완성 수량) × 1.5 + 4
    private const float CookedMultiplier = 1.5f;
    private const int CookedBonus = 4;

    // 씨앗 가격 (상인 판매가, 판매 상자는 절반)
    private static readonly Dictionary<string, int> SeedPrices = new Dictionary<string, int>
    {
        { "crop_potato", 6 }, { "crop_strawberry", 8 }, { "crop_tomato", 8 }, { "crop_pumpkin", 14 }, { "crop_winter_radish", 9 }
    };

    private const int SeedDailyStock = 12;

    // 상인 고정 재고 : 아이템, 가격, 하루 재고, 등장 확률
    private static readonly (string id, int price, int stock, float chance)[] SupplyStock =
    {
        ("item_animal_feed", 3, 30, 1f),
        ("resource_worm_bait", 2, 20, 1f),
        ("item_iron_ore", 20, 5, 1f),
        ("medicine_bandage", 18, 3, 1f),
        ("item_arrow", 2, 20, 0.5f),
        ("tool_fishing_rod_basic", 60, 1, 0.25f),
        ("equipment_small_backpack", 450, 1, 0.15f)
    };

    private sealed class StructureSpec
    {
        public string Name;
        public string RecipeAsset;
        public string RecipeId;
        public string DisplayName;
        public string ModelId;
        public Vector3 Size;
        public (string itemId, int amount)[] Ingredients;
    }

    private static readonly StructureSpec BinSpec = new StructureSpec
    {
        Name = "ShippingBin", RecipeAsset = "BuildRecipe_ShippingBin", RecipeId = "structure_shipping_bin", DisplayName = "SHIPPING BIN",
        ModelId = "build_shipping_bin", Size = StylizedModelLibrary.ShippingBinSize,
        Ingredients = new[] { ("item_wood", 8), ("item_plant_fiber", 4) }
    };

    private static readonly StructureSpec StallSpec = new StructureSpec
    {
        Name = "MarketStall", RecipeAsset = "BuildRecipe_MarketStall", RecipeId = "structure_market_stall", DisplayName = "MARKET STALL",
        ModelId = "build_market_stall", Size = StylizedModelLibrary.MarketStallSize,
        Ingredients = new[] { ("item_wood", 18), ("item_plant_fiber", 8), ("resource_stone", 6) }
    };

    // ---------------------------------------------------------------- 메뉴

    [MenuItem(BuildMenuRoot + "6. Market (Prices + Shipping Bin + Stall + UI)", false, 25)]
    private static void BuildAllMenu()
    {
        bool confirmed = EditorUtility.DisplayDialog(
            DialogTitle,
            "판매 가격표와 상인 재고, 판매 상자·상인 가판대 건축물을 만들고\n"
            + "현재 게임 Scene(20_Gameplay)에 상점 관리자·지갑·상인 창·코인 표시를 추가합니다.\n"
            + "보관함 창 Prefab에는 판매 상자 안내 문구가 추가됩니다.\n\n"
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

    private static string Shorten(string report)
    {
        const int limit = 1800;
        return report.Length <= limit ? report : report.Substring(0, limit) + "\n... (전체 내용은 Console 참고)";
    }

    // ---------------------------------------------------------------- 전체 생성

    public static string BuildAll()
    {
        StringBuilder report = new StringBuilder("[판매·상점 콘텐츠 생성]\n");
        List<BuildRecipeData> recipes = new List<BuildRecipeData>();
        MarketCatalogData catalog;
        Sprite merchantIcon;

        try
        {
            StylizedArtAssetFactory.EnsureFolder(DataFolder);
            StylizedArtAssetFactory.EnsureFolder(BuildRecipeFolder);
            StylizedArtAssetFactory.EnsureFolder(BuildingFolder);
            StylizedArtAssetFactory.EnsureFolder(IconFolder);

            EditorUtility.DisplayProgressBar(DialogTitle, "아이콘과 저폴리 모델", 0.05f);

            if (UISpriteFactory.Panel == null)
            {
                UISpriteFactory.GenerateAll();
            }

            UISpriteFactory.GenerateMarketIcons();
            int models = 0;

            foreach (string modelId in new[] { "build_shipping_bin", "build_market_stall", "npc_merchant", "fx_bin_flag" })
            {
                if (StylizedArtAssetFactory.GetOrCreateModelPrefab(modelId, true) != null)
                {
                    models++;
                }
            }

            report.AppendLine($"코인·가격표·상자 아이콘, 상점 모델 {models}개 생성·갱신");

            EditorUtility.DisplayProgressBar(DialogTitle, "가격표와 상인 재고", 0.2f);
            Dictionary<string, ItemData> items = LoadItemsById();
            catalog = CreateOrUpdateCatalog(items, report);
            StorageTypeData storageType = CreateOrUpdateStorageType();
            report.AppendLine($"판매 상자 보관함 종류 : {storageType.DisplayName} {storageType.SlotCapacity}칸");
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayProgressBar(DialogTitle, "판매 상자·가판대", 0.4f);
            int layer = ResolveBuildingLayer();

            foreach (StructureSpec spec in new[] { BinSpec, StallSpec })
            {
                GameObject placed = CreateOrUpdatePrefab(spec, true, layer, storageType, report);
                GameObject preview = CreateOrUpdatePrefab(spec, false, layer, storageType, report);
                recipes.Add(CreateOrUpdateRecipe(spec, placed, preview, items));
                report.AppendLine($"{spec.DisplayName} : {spec.Size.x}×{spec.Size.z}m, 재료 {IngredientText(spec.Ingredients)}");
            }

            AssetDatabase.SaveAssets();

            EditorUtility.DisplayProgressBar(DialogTitle, "상인 초상", 0.6f);
            merchantIcon = RenderMerchantIcon(report);

            GameDataRegistryEditor.CreateOrRefreshDefaultRegistry();
            report.AppendLine("GameDataRegistry 자동 수집 완료");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        report.AppendLine(WireScene(catalog, recipes, merchantIcon));
        AssetDatabase.SaveAssets();
        report.AppendLine();
        report.Append(Validate(out _));
        return report.ToString();
    }

    public static string RefreshCatalog() // 95일차: 가격표 · 상인 재고만 다시 만들기 (13번 밸런스 메뉴에서 사용)
    {
        StringBuilder report = new StringBuilder();
        CreateOrUpdateCatalog(LoadItemsById(), report);
        AssetDatabase.SaveAssets();
        return report.ToString().TrimEnd();
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

    private static List<T> LoadAll<T>() where T : ScriptableObject
    {
        List<T> result = new List<T>();

        foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { "Assets/_ProjectU/Data" }))
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));

            if (asset != null)
            {
                result.Add(asset);
            }
        }

        result.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
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

    private sealed class PriceRow
    {
        public ItemData Item;
        public int Price;
        public MarketGoodsType Type;
        public SeasonType[] Fresh = new SeasonType[0];
    }

    private sealed class StockRow
    {
        public ItemData Item;
        public int Price;
        public int Stock;
        public SeasonType[] Seasons = new SeasonType[0];
        public float Chance = 1f;
    }

    private static MarketCatalogData CreateOrUpdateCatalog(Dictionary<string, ItemData> items, StringBuilder report)
    {
        List<PriceRow> prices = new List<PriceRow>();
        List<StockRow> stock = new List<StockRow>();
        Dictionary<ItemData, PriceRow> byItem = new Dictionary<ItemData, PriceRow>();
        List<string> missing = new List<string>();

        void AddPrice(ItemData item, int price, MarketGoodsType type, SeasonType[] fresh)
        {
            if (item == null || byItem.ContainsKey(item))
            {
                return;
            }

            PriceRow row = new PriceRow { Item = item, Price = Mathf.Max(1, price), Type = type, Fresh = fresh ?? new SeasonType[0] };
            prices.Add(row);
            byItem.Add(item, row);
        }

        // 작물 제철 (CropData 의 재배 계절)
        Dictionary<ItemData, SeasonType[]> cropSeasons = new Dictionary<ItemData, SeasonType[]>();
        List<CropData> crops = LoadAll<CropData>();

        foreach (CropData crop in crops)
        {
            if (crop.HarvestItem != null)
            {
                cropSeasons[crop.HarvestItem] = new List<SeasonType>(crop.GrowingSeasons).ToArray();
            }
        }

        foreach ((string id, int price, MarketGoodsType type) in FixedPrices)
        {
            if (!items.TryGetValue(id, out ItemData item))
            {
                missing.Add(id);
                continue;
            }

            cropSeasons.TryGetValue(item, out SeasonType[] fresh);
            AddPrice(item, price, type, type == MarketGoodsType.Crop ? fresh : null);
        }

        // 물고기 (희귀도·난이도)
        int fishCount = 0;

        foreach (FishData fish in LoadAll<FishData>())
        {
            if (fish.ResultItem == null)
            {
                continue;
            }

            float rarity = FishRarityMultiplier[Mathf.Clamp((int)fish.Rarity, 0, FishRarityMultiplier.Length - 1)];
            int price = Mathf.RoundToInt(FishBasePrice * rarity * (1f + fish.Difficulty * 0.5f));
            AddPrice(fish.ResultItem, price, MarketGoodsType.Fish, null);
            fishCount++;
        }

        // 요리 (재료 가격 기준)
        int cookedCount = 0;

        foreach (CookingRecipeData recipe in LoadAll<CookingRecipeData>())
        {
            if (recipe.ResultItem == null || byItem.ContainsKey(recipe.ResultItem))
            {
                continue;
            }

            int ingredients = 0;

            foreach (CraftingIngredient ingredient in recipe.Ingredients)
            {
                if (ingredient == null || ingredient.ItemData == null)
                {
                    continue;
                }

                int unit = byItem.TryGetValue(ingredient.ItemData, out PriceRow row) ? row.Price : 1;
                ingredients += unit * ingredient.Amount;
            }

            int price = Mathf.RoundToInt(ingredients / (float)recipe.ResultQuantity * CookedMultiplier) + CookedBonus;
            AddPrice(recipe.ResultItem, price, MarketGoodsType.Cooked, null);
            cookedCount++;
        }

        // 씨앗 (상인 판매 · 판매 상자는 절반)
        foreach (CropData crop in crops)
        {
            if (crop.SeedItem == null)
            {
                continue;
            }

            int seedPrice = SeedPrices.TryGetValue(crop.CropId, out int value) ? value : 8;
            stock.Add(new StockRow { Item = crop.SeedItem, Price = seedPrice, Stock = SeedDailyStock, Seasons = new List<SeasonType>(crop.GrowingSeasons).ToArray() });
            AddPrice(crop.SeedItem, Mathf.Max(1, seedPrice / 2), MarketGoodsType.Seed, null);
        }

        foreach ((string id, int price, int count, float chance) in SupplyStock)
        {
            if (!items.TryGetValue(id, out ItemData item))
            {
                missing.Add(id);
                continue;
            }

            stock.Add(new StockRow { Item = item, Price = price, Stock = count, Chance = chance });
        }

        MarketCatalogData catalog = LoadOrCreateAsset<MarketCatalogData>(CatalogPath);
        SerializedObject serialized = new SerializedObject(catalog);
        SerializedProperty priceList = serialized.FindProperty("prices");
        priceList.arraySize = prices.Count;

        for (int index = 0; index < prices.Count; index++)
        {
            SerializedProperty element = priceList.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("item").objectReferenceValue = prices[index].Item;
            element.FindPropertyRelative("basePrice").intValue = prices[index].Price;
            element.FindPropertyRelative("goodsType").intValue = (int)prices[index].Type;
            SetSeasons(element.FindPropertyRelative("freshSeasons"), prices[index].Fresh);
        }

        SerializedProperty stockList = serialized.FindProperty("stock");
        stockList.arraySize = stock.Count;

        for (int index = 0; index < stock.Count; index++)
        {
            SerializedProperty element = stockList.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("item").objectReferenceValue = stock[index].Item;
            element.FindPropertyRelative("price").intValue = stock[index].Price;
            element.FindPropertyRelative("dailyStock").intValue = stock[index].Stock;
            element.FindPropertyRelative("dailyChance").floatValue = stock[index].Chance;
            SetSeasons(element.FindPropertyRelative("seasons"), stock[index].Seasons);
        }

        serialized.FindProperty("freshMultiplier").floatValue = 1.25f;
        serialized.FindProperty("bulkThreshold").intValue = 20;
        serialized.FindProperty("bulkMultiplier").floatValue = 0.7f;
        serialized.FindProperty("openHour").floatValue = 6f;
        serialized.FindProperty("closeHour").floatValue = 20f;
        serialized.FindProperty("stockSeed").intValue = 8731;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(catalog);

        report.AppendLine($"판매 가격표 {prices.Count}종 (물고기 {fishCount}, 요리 {cookedCount}, 씨앗 {crops.Count}), 상인 재고 후보 {stock.Count}종");

        if (missing.Count > 0)
        {
            report.AppendLine($"[참고] 아직 없는 아이템은 건너뜀: {string.Join(", ", missing)}");
        }

        return catalog;
    }

    private static void SetSeasons(SerializedProperty list, SeasonType[] seasons)
    {
        list.arraySize = seasons.Length;

        for (int index = 0; index < seasons.Length; index++)
        {
            list.GetArrayElementAtIndex(index).enumValueIndex = (int)seasons[index];
        }
    }

    private static StorageTypeData CreateOrUpdateStorageType()
    {
        StorageTypeData type = LoadOrCreateAsset<StorageTypeData>(StorageTypePath);
        SerializedObject serialized = new SerializedObject(type);
        serialized.FindProperty("storageTypeId").stringValue = StorageTypeId;
        serialized.FindProperty("displayName").stringValue = "SHIPPING BIN";
        serialized.FindProperty("slotCapacity").intValue = 16;
        serialized.FindProperty("columnCount").intValue = 4;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(type);
        return type;
    }

    // ---------------------------------------------------------------- 건축물 Prefab

    private static int ResolveBuildingLayer()
    {
        GameObject reference = AssetDatabase.LoadAssetAtPath<GameObject>(LayerReferencePrefabPath);
        return reference != null ? reference.layer : 0;
    }

    private static GameObject CreateOrUpdatePrefab(StructureSpec spec, bool placed, int layer, StorageTypeData storageType, StringBuilder report)
    {
        string path = $"{BuildingFolder}/{spec.Name}{(placed ? "Placed" : "Preview")}.prefab";
        bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
        GameObject root = exists ? PrefabUtility.LoadPrefabContents(path) : new GameObject(Path.GetFileNameWithoutExtension(path));

        try
        {
            ConfigureStructure(root, spec, placed, layer, storageType, report);
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

    private static void ConfigureStructure(GameObject root, StructureSpec spec, bool placed, int layer, StorageTypeData storageType, StringBuilder report)
    {
        StylizedVisualReplacer.Restore(root, false);
        root.layer = layer;

        // 이전 실행에서 만든 하위 오브젝트 정리
        for (int index = root.transform.childCount - 1; index >= 0; index--)
        {
            Transform child = root.transform.GetChild(index);

            if (child.name.StartsWith(ColliderPrefix) || child.name == FlagName || child.name == MerchantName)
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

        if (root.GetComponent<WorldObjectIdentity>() == null)
        {
            root.AddComponent<WorldObjectIdentity>();
        }

        if (root.GetComponent<PlacedBuildObject>() == null)
        {
            root.AddComponent<PlacedBuildObject>();
        }

        SerializedObject identity = new SerializedObject(root.GetComponent<WorldObjectIdentity>());
        identity.FindProperty("worldObjectId").stringValue = string.Empty;
        identity.ApplyModifiedPropertiesWithoutUndo();

        if (spec == BinSpec)
        {
            ConfigureBin(root, spec, layer, storageType);
        }
        else
        {
            ConfigureStall(root, spec, layer);
        }
    }

    private static void ConfigureBin(GameObject root, StructureSpec spec, int layer, StorageTypeData storageType)
    {
        AddBoxCollider(root, "Body", new Vector3(0f, spec.Size.y * 0.5f, 0f), spec.Size, layer);

        // 깃발 (오른쪽 옆면, 물건이 있으면 선다)
        GameObject flag = InstantiateModel("fx_bin_flag", root.transform, FlagName, new Vector3(spec.Size.x * 0.5f + 0.03f, spec.Size.y * 0.36f, spec.Size.z * 0.18f), 1f, layer);
        flag.SetActive(true);

        StorageContainer container = GetOrAdd<StorageContainer>(root);
        SerializedObject containerSerialized = new SerializedObject(container);
        containerSerialized.FindProperty("storageTypeData").objectReferenceValue = storageType;
        containerSerialized.FindProperty("slots").arraySize = 0;
        containerSerialized.FindProperty("placedBuildObject").objectReferenceValue = root.GetComponent<PlacedBuildObject>();
        containerSerialized.FindProperty("debugStructureId").stringValue = string.Empty;
        containerSerialized.ApplyModifiedPropertiesWithoutUndo();

        StorageInteractable interactable = GetOrAdd<StorageInteractable>(root);
        SerializedObject interactSerialized = new SerializedObject(interactable);
        interactSerialized.FindProperty("promptMessage").stringValue = "F - OPEN SHIPPING BIN";
        interactSerialized.FindProperty("storageContainer").objectReferenceValue = container;
        interactSerialized.ApplyModifiedPropertiesWithoutUndo();

        ShippingBin bin = GetOrAdd<ShippingBin>(root);
        SerializedObject binSerialized = new SerializedObject(bin);
        binSerialized.FindProperty("flag").objectReferenceValue = flag.transform;
        binSerialized.FindProperty("flagRaisedEuler").vector3Value = Vector3.zero;
        binSerialized.FindProperty("flagLoweredEuler").vector3Value = new Vector3(-90f, 0f, 0f);
        binSerialized.ApplyModifiedPropertiesWithoutUndo();
        flag.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
    }

    private static void ConfigureStall(GameObject root, StructureSpec spec, int layer)
    {
        // 가판대 전체 (계산대 높이) + 상인 자리
        float counterHeight = StylizedModelLibrary.MarketCounterTopUnit * spec.Size.y;
        AddBoxCollider(root, "Body", new Vector3(0f, counterHeight * 0.5f + 0.02f, 0f), new Vector3(spec.Size.x - 0.1f, counterHeight, spec.Size.z - 0.1f), layer);
        AddBoxCollider(root, "Merchant", new Vector3(0f, 0.95f, spec.Size.z * 0.1f), new Vector3(0.7f, 1.9f, 0.7f), layer);

        GameObject merchant = InstantiateModel("npc_merchant", root.transform, MerchantName, new Vector3(0f, 0f, spec.Size.z * 0.1f), 1f, layer);
        merchant.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        merchant.SetActive(true);

        MarketStall stall = GetOrAdd<MarketStall>(root);
        SerializedObject serialized = new SerializedObject(stall);
        serialized.FindProperty("promptMessage").stringValue = "F - TRADE";
        serialized.FindProperty("stallDisplayName").stringValue = spec.DisplayName;
        serialized.FindProperty("merchantName").stringValue = "TRAVELING MERCHANT";
        serialized.FindProperty("merchant").objectReferenceValue = merchant.transform;
        serialized.FindProperty("merchantBaseYaw").floatValue = 180f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static T GetOrAdd<T>(GameObject root) where T : Component
    {
        T component = root.GetComponent<T>();
        return component != null ? component : root.AddComponent<T>();
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
        return instance;
    }

    private static BuildRecipeData CreateOrUpdateRecipe(StructureSpec spec, GameObject placed, GameObject preview, Dictionary<string, ItemData> items)
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
        serialized.FindProperty("maximumHeightDifference").floatValue = 0.3f;
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

    // ---------------------------------------------------------------- 상인 초상

    private static Sprite RenderMerchantIcon(StringBuilder report)
    {
        GameObject model = StylizedArtAssetFactory.LoadModelPrefab("npc_merchant");

        if (model == null)
        {
            report.AppendLine("[경고] 상인 모델이 없어 초상을 만들지 못했습니다.");
            return AssetDatabase.LoadAssetAtPath<Sprite>(MerchantIconPath);
        }

        byte[] png;
        string error;

        using (ItemIconRenderer renderer = new ItemIconRenderer())
        {
            png = renderer.RenderPng(model, new ItemIconRenderer.Framing(Vector3.zero, 28f, 8f), out error);
        }

        if (png == null)
        {
            report.AppendLine($"[경고] 상인 초상 실패 : {error}");
            return AssetDatabase.LoadAssetAtPath<Sprite>(MerchantIconPath);
        }

        ItemIconRenderer.WritePng(png, MerchantIconPath);
        AssetDatabase.ImportAsset(MerchantIconPath, ImportAssetOptions.ForceUpdate);
        ItemIconRenderer.ImportAsSprite(MerchantIconPath);
        report.AppendLine("상인 초상 아이콘 생성");
        return AssetDatabase.LoadAssetAtPath<Sprite>(MerchantIconPath);
    }

    // ---------------------------------------------------------------- Scene 연결

    private static string WireScene(MarketCatalogData catalog, List<BuildRecipeData> recipes, Sprite merchantIcon)
    {
        GameUIManager uiManager = Object.FindFirstObjectByType<GameUIManager>(FindObjectsInactive.Include);
        BuildPlacementController buildController = Object.FindFirstObjectByType<BuildPlacementController>(FindObjectsInactive.Include);

        if (uiManager == null || buildController == null)
        {
            return "[건너뜀] 현재 Scene이 게임 Scene(20_Gameplay)이 아니어서 Scene 연결을 생략했습니다.";
        }

        StringBuilder report = new StringBuilder();
        Scene scene = uiManager.gameObject.scene;

        // 플레이어 지갑
        PlayerInventory player = Object.FindFirstObjectByType<PlayerInventory>(FindObjectsInactive.Include);
        PlayerWallet wallet = null;

        if (player != null)
        {
            wallet = player.GetComponent<PlayerWallet>();

            if (wallet == null)
            {
                wallet = Undo.AddComponent<PlayerWallet>(player.gameObject);
            }

            report.AppendLine($"플레이어 지갑 : {player.name}");
        }
        else
        {
            report.AppendLine("[경고] 플레이어를 찾지 못해 지갑을 추가하지 못했습니다.");
        }

        // 상점 관리자
        MarketManager manager = Object.FindFirstObjectByType<MarketManager>(FindObjectsInactive.Include);

        if (manager == null)
        {
            GameObject created = new GameObject(ManagerName);
            SceneManager.MoveGameObjectToScene(created, scene);
            Undo.RegisterCreatedObjectUndo(created, "Create Market Manager");
            manager = Undo.AddComponent<MarketManager>(created);
        }

        SerializedObject managerSerialized = new SerializedObject(manager);
        managerSerialized.FindProperty("dayNightCycle").objectReferenceValue = Object.FindFirstObjectByType<DayNightCycle>(FindObjectsInactive.Include);
        managerSerialized.FindProperty("seasonCycle").objectReferenceValue = Object.FindFirstObjectByType<SeasonCycle>(FindObjectsInactive.Include);
        managerSerialized.FindProperty("catalog").objectReferenceValue = catalog;
        managerSerialized.FindProperty("wallet").objectReferenceValue = wallet;
        managerSerialized.ApplyModifiedProperties();
        report.AppendLine($"상점 관리자 : {manager.name}");

        report.AppendLine(AddReferences(buildController, "buildRecipes", recipes.ToArray(), "건축 목록"));
        PlacedStructureSaveBridge saveBridge = Object.FindFirstObjectByType<PlacedStructureSaveBridge>(FindObjectsInactive.Include);
        report.AppendLine(AddReferences(saveBridge, "buildRecipes", recipes.ToArray(), "건축물 저장 목록"));

        report.AppendLine(MarketPopupUIBuilder.Build(merchantIcon, out ShopPopupUI popup, out CoinHUD coinHud));
        SerializedObject uiSerialized = new SerializedObject(uiManager);
        uiSerialized.FindProperty("shopPopup").objectReferenceValue = popup;
        StorageContainerUI storagePrefab = uiSerialized.FindProperty("storagePopupPrefab").objectReferenceValue as StorageContainerUI;
        uiSerialized.ApplyModifiedProperties();
        report.AppendLine(popup != null ? "GameUIManager에 상인 창 연결" : "[경고] 상인 창을 연결하지 못했습니다.");
        report.AppendLine(MarketPopupUIBuilder.PatchStoragePopup(storagePrefab));

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
            if (reference != null && !ContainsReference(list, reference))
            {
                list.arraySize++;
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = reference;
                added++;
            }
        }

        serialized.ApplyModifiedProperties();
        return $"{label} 추가 {added}개 (전체 {list.arraySize}개)";
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

    // ---------------------------------------------------------------- 검증

    public static string Validate(out int errorCount)
    {
        StringBuilder report = new StringBuilder("[판매·상점 콘텐츠 검증]\n");
        int errors = 0;
        void Error(string message)
        {
            errors++;
            report.AppendLine("[오류] " + message);
        }

        MarketCatalogData catalog = AssetDatabase.LoadAssetAtPath<MarketCatalogData>(CatalogPath);

        if (catalog == null)
        {
            Error("가격표(MarketCatalog)가 없습니다.");
        }
        else
        {
            ValidateCatalog(catalog, report, Error);
        }

        if (UISpriteFactory.Icon("Coin") == null || UISpriteFactory.Icon("Tag") == null || UISpriteFactory.Icon("Crate") == null)
        {
            Error("코인·가격표·상자 아이콘이 없습니다.");
        }

        if (AssetDatabase.LoadAssetAtPath<Sprite>(MerchantIconPath) == null)
        {
            Error("상인 초상 아이콘이 없습니다.");
        }

        StorageTypeData storageType = AssetDatabase.LoadAssetAtPath<StorageTypeData>(StorageTypePath);

        if (storageType == null || storageType.StorageTypeId != StorageTypeId)
        {
            Error("판매 상자 보관함 종류가 없습니다.");
        }

        ValidateStructure(BinSpec, report, Error);
        ValidateStructure(StallSpec, report, Error);
        ValidateScene(report, Error);
        errorCount = errors;
        report.Append(errors == 0 ? "결과 : 문제 없음" : $"결과 : 오류 {errors}개");
        return report.ToString();
    }

    private static void ValidateCatalog(MarketCatalogData catalog, StringBuilder report, System.Action<string> error)
    {
        HashSet<ItemData> seen = new HashSet<ItemData>();
        Dictionary<ItemData, int> sellPrices = new Dictionary<ItemData, int>();

        foreach (MarketPriceEntry entry in catalog.Prices)
        {
            if (entry == null || entry.Item == null)
            {
                error("가격표에 빈 항목이 있습니다.");
                continue;
            }

            if (!seen.Add(entry.Item))
            {
                error($"가격표 중복 : {entry.Item.ItemId}");
            }

            if (entry.Item.IsTool || entry.Item.IsWeapon || entry.Item.IsEquipment)
            {
                error($"도구·무기·장비는 팔 수 없어야 합니다 : {entry.Item.ItemId}");
            }

            sellPrices[entry.Item] = catalog.GetQuote(entry.Item, SeasonType.Spring).UnitPrice;
        }

        foreach (MarketStockEntry entry in catalog.Stock)
        {
            if (entry == null || entry.Item == null)
            {
                error("상인 재고에 빈 항목이 있습니다.");
                continue;
            }

            // 사서 되팔아 이익이 나면 안 된다 (제철 가격 기준)
            MarketPriceQuote best = catalog.GetQuote(entry.Item, SeasonType.Spring);

            foreach (SeasonType season in new[] { SeasonType.Summer, SeasonType.Autumn, SeasonType.Winter })
            {
                MarketPriceQuote quote = catalog.GetQuote(entry.Item, season);

                if (quote.UnitPrice > best.UnitPrice)
                {
                    best = quote;
                }
            }

            if (best.Sellable && best.UnitPrice >= entry.Price)
            {
                error($"{entry.Item.ItemId} : 상인 가격 {entry.Price} ≤ 판매 가격 {best.UnitPrice} (되팔기 이익)");
            }
        }

        int[] seasonOffers = new int[4];

        for (int season = 0; season < 4; season++)
        {
            foreach (MarketStockEntry entry in catalog.Stock)
            {
                if (entry != null && entry.Item != null && entry.Item.ItemCategory == ItemCategory.Seed && entry.IsSoldIn((SeasonType)season))
                {
                    seasonOffers[season]++;
                }
            }

            if (seasonOffers[season] == 0)
            {
                error($"{(SeasonType)season} 에 파는 씨앗이 없습니다.");
            }
        }

        report.AppendLine($"가격표 {catalog.Prices.Count}종, 상인 재고 후보 {catalog.Stock.Count}종, 계절별 씨앗 {seasonOffers[0]}/{seasonOffers[1]}/{seasonOffers[2]}/{seasonOffers[3]}");
    }

    private static void ValidateStructure(StructureSpec spec, StringBuilder report, System.Action<string> error)
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

        if (placed.GetComponent<PlacedBuildObject>() == null || placed.GetComponent<WorldObjectIdentity>() == null)
        {
            error($"{spec.DisplayName} : 건축물 저장 컴포넌트 없음");
        }

        if (placed.GetComponent<StylizedVisualReplacement>() == null || recipe.PreviewPrefab.GetComponent<StylizedVisualReplacement>() == null)
        {
            error($"{spec.DisplayName} : 저폴리 외형이 적용되지 않았습니다.");
        }

        if (placed.GetComponentsInChildren<BoxCollider>(true).Length == 0)
        {
            error($"{spec.DisplayName} : 충돌체가 없습니다.");
        }

        if (spec == BinSpec)
        {
            StorageContainer container = placed.GetComponent<StorageContainer>();
            ShippingBin bin = placed.GetComponent<ShippingBin>();
            StorageInteractable interactable = placed.GetComponent<StorageInteractable>();

            if (container == null || bin == null || interactable == null || container.StorageTypeId != StorageTypeId)
            {
                error("판매 상자 : 보관함·판매·상호작용 연결 오류");
                return;
            }

            if (new SerializedObject(bin).FindProperty("flag").objectReferenceValue == null)
            {
                error("판매 상자 : 깃발 연결 없음");
            }

            report.AppendLine($"{spec.DisplayName} : 보관함 {container.SlotCapacity}칸, 깃발 있음");
        }
        else
        {
            MarketStall stall = placed.GetComponent<MarketStall>();

            if (stall == null || stall.Merchant == null)
            {
                error("상인 가판대 : 가판대 기능 또는 상인 모델 없음");
                return;
            }

            report.AppendLine($"{spec.DisplayName} : 상인 모델 {stall.Merchant.name}, 충돌체 {placed.GetComponentsInChildren<BoxCollider>(true).Length}개");
        }
    }

    private static void ValidateScene(StringBuilder report, System.Action<string> error)
    {
        GameUIManager uiManager = Object.FindFirstObjectByType<GameUIManager>(FindObjectsInactive.Include);

        if (uiManager == null)
        {
            report.AppendLine("[건너뜀] 게임 Scene이 열려 있지 않아 Scene 검사를 생략했습니다.");
            return;
        }

        MarketManager manager = Object.FindFirstObjectByType<MarketManager>(FindObjectsInactive.Include);

        if (manager == null)
        {
            error("Scene에 MarketManager가 없습니다.");
        }
        else
        {
            SerializedObject serialized = new SerializedObject(manager);

            foreach (string property in new[] { "dayNightCycle", "seasonCycle", "catalog", "wallet" })
            {
                if (serialized.FindProperty(property).objectReferenceValue == null)
                {
                    error($"MarketManager.{property} 연결 없음");
                }
            }
        }

        PlayerInventory player = Object.FindFirstObjectByType<PlayerInventory>(FindObjectsInactive.Include);

        if (player == null || player.GetComponent<PlayerWallet>() == null)
        {
            error("플레이어에 PlayerWallet이 없습니다.");
        }

        BuildPlacementController buildController = Object.FindFirstObjectByType<BuildPlacementController>(FindObjectsInactive.Include);
        PlacedStructureSaveBridge saveBridge = Object.FindFirstObjectByType<PlacedStructureSaveBridge>(FindObjectsInactive.Include);

        foreach (StructureSpec spec in new[] { BinSpec, StallSpec })
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

        ShopPopupUI popup = uiManager.ShopPopup;

        if (popup == null)
        {
            error("GameUIManager에 상인 창이 연결되지 않았습니다.");
        }
        else
        {
            CheckReferences(popup, "상인 창", error);

            foreach (ShopRowUI row in popup.GetComponentsInChildren<ShopRowUI>(true))
            {
                CheckReferences(row, "상인 창 줄", error);
            }
        }

        CoinHUD coinHud = Object.FindFirstObjectByType<CoinHUD>(FindObjectsInactive.Include);

        if (coinHud == null)
        {
            error("코인 표시(LP_CoinHUD)가 없습니다.");
        }
        else
        {
            CheckReferences(coinHud, "코인 표시", error);
        }

        StorageContainerUI storagePrefab = new SerializedObject(uiManager).FindProperty("storagePopupPrefab").objectReferenceValue as StorageContainerUI;

        if (storagePrefab == null || new SerializedObject(storagePrefab).FindProperty("infoText").objectReferenceValue == null)
        {
            error("보관함 창 Prefab에 판매 상자 안내 문구가 없습니다.");
        }

        report.AppendLine("Scene : 상점 관리자·지갑·건축 목록·저장 목록·상인 창·코인 표시·보관함 안내 확인");
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
