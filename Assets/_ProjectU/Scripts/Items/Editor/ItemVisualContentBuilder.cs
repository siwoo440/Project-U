using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 84일차: 모든 아이템이 가방(아이콘)과 바닥(Pickup 외형)에서 제 모양으로 보이게 한다.
// 1. 아이템별 저폴리 모델 확인·생성
// 2. 바닥용 Pickup Prefab 확인·생성과 월드 아이템 Registry 등록 (버리기·불러오기에서 자루 대신 제 모델)
// 3. 모델을 찍어 아이콘 PNG를 만들고 ItemData에 연결
// 4. 핫바·인벤토리·보관함 아이콘 표시 정리, 버리기 기능 연결, 선택 아이템 이름 표시
// 5. 손에 든 외형 : 활 전용 외형과 그 밖의 아이템 외형 목록 (전용 도구 외형이 없는 아이템)
// 여러 번 실행해도 같은 Asset·오브젝트를 갱신한다.
public static class ItemVisualContentBuilder
{
    public const string BuildMenuRoot = "Tools/Project U/Build Content/";
    private const string DialogTitle = "Project U 아이템 외형";

    private const string ItemDatabasePath = "Assets/_ProjectU/Data/Databases/ItemDatabase.asset";
    private const string PickupRegistryPath = "Assets/_ProjectU/Prefabs/Items/Day75/WorldItemPickupRegistry_Day75.asset";
    private const string PickupSearchFolder = "Assets/_ProjectU/Prefabs/Items";
    private const string PickupFolder = "Assets/_ProjectU/Prefabs/Items/Day84";
    private const string PickupTemplatePath = "Assets/_ProjectU/Prefabs/Items/Day71/WildMushroomPickup.prefab";
    private const string GenericDropPath = "Assets/_ProjectU/Prefabs/Items/WorldItemDrop.prefab";
    public const string IconFolder = "Assets/_ProjectU/UI/Icons/Items";
    private const string InventoryPopupPath = "Assets/_ProjectU/Prefabs/UI/Popups/PF_UI_InventoryPopup.prefab";
    private const string StoragePopupPath = "Assets/_ProjectU/Prefabs/UI/Popups/PF_UI_StoragePopup.prefab";
    private const string SelectedNameRootName = "LP_SelectedItemName";
    private const string WaterGaugeName = "LP_WaterGauge";
    public const string HeldVisualSetPath = "Assets/_ProjectU/Data/Items/HeldItemVisuals.asset";
    private const string BowVisualName = "BowVisual";
    private const string NockedArrowName = "NockedArrow";

    // 전용 손 외형(ToolHolder 아래 오브젝트)이 있는 아이템
    private static readonly HashSet<string> DedicatedHeldItems = new HashSet<string>
    {
        "tool_axe", "tool_pickaxe", "tool_hoe", "tool_watering_can", "tool_fishing_rod_basic", "weapon_bow"
    };

    // 아이템 ID → 저폴리 모델 ID (새 아이템은 여기에 추가하거나 Pickup의 모델 기록을 사용)
    private static readonly Dictionary<string, string> ModelByItem = new Dictionary<string, string>
    {
        { "drink_water_bottle", "item_water_bottle" },
        { "equipment_cloth_cap", "item_cap" },
        { "equipment_cloth_shirt", "item_shirt" },
        { "equipment_small_backpack", "item_backpack" },
        { "equipment_work_hat", "item_work_hat" },
        { "food_apple", "item_apple" },
        { "food_baked_apple", "item_baked_apple" },
        { "food_berry", "item_berry" },
        { "food_potato", "item_potato" },
        { "food_pumpkin", "item_pumpkin" },
        { "food_strawberry", "item_strawberry" },
        { "food_tomato", "item_tomato" },
        { "food_winter_radish", "item_winter_radish" },
        // 85일차 요리
        { "food_baked_potato", "item_baked_potato" },
        { "food_grilled_fish", "item_grilled_fish" },
        { "food_mushroom_skewer", "item_mushroom_skewer" },
        { "food_pumpkin_soup", "item_pumpkin_soup" },
        { "food_tomato_stew", "item_tomato_stew" },
        { "food_golden_feast", "item_golden_feast" },
        // 86일차 가축
        { "food_egg", "item_egg" },
        { "drink_milk", "item_milk" },
        { "item_animal_feed", "item_animal_feed" },
        { "food_fried_egg", "item_fried_egg" },
        { "food_veggie_omelette", "item_veggie_omelette" },
        { "food_warm_milk", "item_warm_milk" },
        { "item_arrow", "item_arrow_bundle" },
        { "item_herbal_tea", "item_herbal_tea" },
        { "item_iron_axe", "tool_iron_axe" },
        { "item_iron_ore", "item_iron_ore" },
        { "item_plant_fiber", "item_plant_fiber" },
        { "item_wild_mushroom", "item_mushroom" },
        { "item_wood", "item_wood_bundle" },
        { "medicine_bandage", "item_bandage" },
        { "resource_fish_catfish", "item_fish_catfish" },
        { "resource_fish_crucian", "item_fish_crucian" },
        { "resource_fish_golden_carp", "item_fish_golden_carp" },
        { "resource_fish_smelt", "item_fish_smelt" },
        { "resource_fish_trout", "item_fish_trout" },
        { "resource_stone", "item_stone" },
        { "resource_worm_bait", "item_worm_bait" },
        { "seed_potato", "item_seed_potato" },
        { "seed_pumpkin", "item_seed_pumpkin" },
        { "seed_strawberry", "item_seed_strawberry" },
        { "seed_tomato", "item_seed_tomato" },
        { "seed_winter_radish", "item_seed_winter_radish" },
        { "tool_axe", "tool_stone_axe" },
        { "tool_fishing_rod_basic", "tool_fishing_rod" },
        { "tool_hoe", "tool_hoe" },
        { "tool_pickaxe", "tool_pickaxe" },
        { "tool_watering_can", "tool_watering_can" },
        { "weapon_bow", "tool_bow" },
        // 102일차 2차 NPC 가게 · 제작
        { "item_vitality_potion", "item_vitality_potion" },
        { "item_spider_silk", "item_spider_silk" },
        { "food_sweet_jelly", "item_sweet_jelly" },
        { "food_inari_sushi", "item_inari_sushi" },
        { "medicine_antidote", "item_antidote" },
        { "item_scrap_parts", "item_scrap_parts" },
        { "medicine_desert_salve", "item_desert_salve" },
        // 104일차 3차 NPC
        { "item_pearl", "item_pearl" },
        { "food_seaweed_salad", "item_seaweed_salad" },
        { "food_grilled_clams", "item_grilled_clams" },
        { "medicine_flower_balm", "item_flower_balm" },
        // 117일차 광물 · 보석 · 주괴 · 등급 도구
        { "resource_coal", "resource_coal" },
        { "resource_copper_ore", "resource_copper_ore" },
        { "resource_silver_ore", "resource_silver_ore" },
        { "resource_gold_ore", "resource_gold_ore" },
        { "resource_crystal", "resource_crystal" },
        { "resource_meteorite_shard", "resource_meteorite_shard" },
        { "resource_gem_ruby", "gem_ruby" },
        { "resource_gem_sapphire", "gem_sapphire" },
        { "resource_gem_emerald", "gem_emerald" },
        { "resource_gem_amethyst", "gem_amethyst" },
        { "resource_gem_diamond", "gem_diamond" },
        { "resource_ingot_copper", "ingot_copper" },
        { "resource_ingot_iron", "ingot_iron" },
        { "resource_ingot_silver", "ingot_silver" },
        { "resource_ingot_gold", "ingot_gold" },
        { "resource_ingot_steel", "ingot_steel" },
        { "resource_ingot_meteorite", "ingot_meteorite" },
        { "tool_pickaxe_copper", "tool_pickaxe_copper" },
        { "tool_pickaxe_iron", "tool_pickaxe_iron" },
        { "tool_pickaxe_steel", "tool_pickaxe_steel" },
        { "tool_axe_copper", "tool_axe_copper" },
        { "tool_axe_steel", "tool_axe_steel" }
    };

    // 84·85일차에 새로 만든 모델 (코드 변경을 반영하도록 항상 다시 만든다)
    private static readonly HashSet<string> NewModels = new HashSet<string>
    {
        "item_baked_apple", "item_arrow_bundle",
        "item_baked_potato", "item_grilled_fish", "item_mushroom_skewer", "item_pumpkin_soup", "item_tomato_stew", "item_golden_feast",
        "item_egg", "item_milk", "item_animal_feed", "item_fried_egg", "item_veggie_omelette", "item_warm_milk",
        "item_vitality_potion", "item_spider_silk", "item_sweet_jelly", "item_inari_sushi", "item_antidote", "item_scrap_parts", "item_desert_salve",
        "item_pearl", "item_seaweed_salad", "item_grilled_clams", "item_flower_balm"
    };

    private sealed class PickupSpec
    {
        public string Name;
        public float Scale;
        public bool LyingTool;
    }

    // 바닥용 Prefab이 없는 아이템
    private static readonly Dictionary<string, PickupSpec> NewPickups = new Dictionary<string, PickupSpec>
    {
        { "tool_axe", new PickupSpec { Name = "StoneAxePickup", Scale = 2.4f, LyingTool = true } },
        { "tool_pickaxe", new PickupSpec { Name = "PickaxePickup", Scale = 2.4f, LyingTool = true } },
        { "weapon_bow", new PickupSpec { Name = "BowPickup", Scale = 2.6f, LyingTool = true } },
        { "item_arrow", new PickupSpec { Name = "ArrowBundlePickup", Scale = 1.9f } },
        { "food_baked_apple", new PickupSpec { Name = "BakedApplePickup", Scale = 1.15f } },
        // 85일차 요리
        { "food_baked_potato", new PickupSpec { Name = "BakedPotatoPickup", Scale = 1.2f } },
        { "food_grilled_fish", new PickupSpec { Name = "GrilledFishPickup", Scale = 1.4f } },
        { "food_mushroom_skewer", new PickupSpec { Name = "MushroomSkewerPickup", Scale = 1.4f } },
        { "food_pumpkin_soup", new PickupSpec { Name = "PumpkinSoupPickup", Scale = 1.15f } },
        { "food_tomato_stew", new PickupSpec { Name = "TomatoStewPickup", Scale = 1.15f } },
        { "food_golden_feast", new PickupSpec { Name = "GoldenFeastPickup", Scale = 1.5f } },
        // 86일차 가축
        { "food_egg", new PickupSpec { Name = "EggPickup", Scale = 1.1f } },
        { "drink_milk", new PickupSpec { Name = "MilkPickup", Scale = 1.1f } },
        { "item_animal_feed", new PickupSpec { Name = "AnimalFeedPickup", Scale = 1.4f } },
        { "food_fried_egg", new PickupSpec { Name = "FriedEggPickup", Scale = 1.3f } },
        { "food_veggie_omelette", new PickupSpec { Name = "VeggieOmelettePickup", Scale = 1.4f } },
        { "food_warm_milk", new PickupSpec { Name = "WarmMilkPickup", Scale = 1.1f } },
        // 102일차 2차 NPC 가게 · 제작
        { "item_vitality_potion", new PickupSpec { Name = "VitalityPotionPickup", Scale = 1.1f } },
        { "item_spider_silk", new PickupSpec { Name = "SpiderSilkPickup", Scale = 1.2f } },
        { "food_sweet_jelly", new PickupSpec { Name = "SweetJellyPickup", Scale = 1.15f } },
        { "food_inari_sushi", new PickupSpec { Name = "InariSushiPickup", Scale = 1.4f } },
        { "medicine_antidote", new PickupSpec { Name = "AntidotePickup", Scale = 1.1f } },
        { "item_scrap_parts", new PickupSpec { Name = "ScrapPartsPickup", Scale = 1.3f } },
        { "medicine_desert_salve", new PickupSpec { Name = "DesertSalvePickup", Scale = 1.1f } },
        // 104일차 3차 NPC
        { "item_pearl", new PickupSpec { Name = "PearlPickup", Scale = 1.0f } },
        { "food_seaweed_salad", new PickupSpec { Name = "SeaweedSaladPickup", Scale = 1.15f } },
        { "food_grilled_clams", new PickupSpec { Name = "GrilledClamsPickup", Scale = 1.3f } },
        { "medicine_flower_balm", new PickupSpec { Name = "FlowerBalmPickup", Scale = 1.0f } },
        // 117일차 광물 · 보석 · 주괴 · 등급 도구
        { "resource_coal", new PickupSpec { Name = "CoalPickup", Scale = 1.8f } },
        { "resource_copper_ore", new PickupSpec { Name = "CopperOrePickup", Scale = 1.8f } },
        { "resource_silver_ore", new PickupSpec { Name = "SilverOrePickup", Scale = 1.8f } },
        { "resource_gold_ore", new PickupSpec { Name = "GoldOrePickup", Scale = 1.8f } },
        { "resource_crystal", new PickupSpec { Name = "CrystalPickup", Scale = 2.2f } },
        { "resource_meteorite_shard", new PickupSpec { Name = "MeteoriteShardPickup", Scale = 2.0f } },
        { "resource_gem_ruby", new PickupSpec { Name = "RubyPickup", Scale = 2.2f } },
        { "resource_gem_sapphire", new PickupSpec { Name = "SapphirePickup", Scale = 2.2f } },
        { "resource_gem_emerald", new PickupSpec { Name = "EmeraldPickup", Scale = 2.2f } },
        { "resource_gem_amethyst", new PickupSpec { Name = "AmethystPickup", Scale = 2.2f } },
        { "resource_gem_diamond", new PickupSpec { Name = "DiamondPickup", Scale = 2.2f } },
        { "resource_ingot_copper", new PickupSpec { Name = "CopperIngotPickup", Scale = 1.8f } },
        { "resource_ingot_iron", new PickupSpec { Name = "IronIngotPickup", Scale = 1.8f } },
        { "resource_ingot_silver", new PickupSpec { Name = "SilverIngotPickup", Scale = 1.8f } },
        { "resource_ingot_gold", new PickupSpec { Name = "GoldIngotPickup", Scale = 1.8f } },
        { "resource_ingot_steel", new PickupSpec { Name = "SteelIngotPickup", Scale = 1.8f } },
        { "resource_ingot_meteorite", new PickupSpec { Name = "MeteoriteIngotPickup", Scale = 1.8f } },
        { "tool_pickaxe_copper", new PickupSpec { Name = "CopperPickaxePickup", Scale = 2.4f, LyingTool = true } },
        { "tool_pickaxe_iron", new PickupSpec { Name = "IronPickaxePickup", Scale = 2.4f, LyingTool = true } },
        { "tool_pickaxe_steel", new PickupSpec { Name = "SteelPickaxePickup", Scale = 2.4f, LyingTool = true } },
        { "tool_axe_copper", new PickupSpec { Name = "CopperAxePickup", Scale = 2.4f, LyingTool = true } },
        { "tool_axe_steel", new PickupSpec { Name = "SteelAxePickup", Scale = 2.4f, LyingTool = true } }
    };

    // 86일차: 동물 아이콘 (가축 창·HUD)
    public const string AnimalIconFolder = "Assets/_ProjectU/UI/Icons/Animals";
    private const string AnimalDataFolder = "Assets/_ProjectU/Data/Livestock";

    // 아이콘 구도 (기본 : 앞쪽 오른쪽 위에서 비스듬히)
    private static ItemIconRenderer.Framing GetFraming(string modelId)
    {
        switch (modelId)
        {
            case "tool_stone_axe":
            case "tool_iron_axe":
            case "tool_pickaxe":
            case "tool_hoe":
            case "tool_pickaxe_copper": // 117일차 등급 도구
            case "tool_pickaxe_iron":
            case "tool_pickaxe_steel":
            case "tool_axe_copper":
            case "tool_axe_steel":
                return new ItemIconRenderer.Framing(new Vector3(0f, 0f, -38f), 10f, 12f);
            case "tool_fishing_rod":
            case "tool_bow":
                return new ItemIconRenderer.Framing(new Vector3(0f, 0f, -42f), 8f, 10f);
            case "item_arrow_bundle":
            case "item_mushroom_skewer":
                return new ItemIconRenderer.Framing(new Vector3(0f, 0f, 30f), 20f, 28f);
            case "item_pumpkin_soup":
            case "item_tomato_stew":
                return new ItemIconRenderer.Framing(Vector3.zero, 25f, 34f);
            case "item_grilled_fish":
            case "item_golden_feast":
            case "item_fried_egg":
            case "item_veggie_omelette":
                return new ItemIconRenderer.Framing(Vector3.zero, 15f, 52f);
            case "animal_chicken":
                return new ItemIconRenderer.Framing(Vector3.zero, 40f, 12f);
            case "animal_cow":
                return new ItemIconRenderer.Framing(Vector3.zero, 55f, 10f);
            case "item_shirt":
                return new ItemIconRenderer.Framing(Vector3.zero, 20f, 58f);
            case "item_bandage":
            case "item_plant_fiber":
            case "item_wood_bundle":
                return new ItemIconRenderer.Framing(Vector3.zero, 35f, 36f);
            default:
                // 물고기는 옆으로 누운 모델이라 위에서 내려다보면 옆모습이 보인다 (머리는 오른쪽 위)
                if (modelId.StartsWith("item_fish_"))
                {
                    return new ItemIconRenderer.Framing(new Vector3(0f, -24f, 0f), 180f, 76f);
                }

                return ItemIconRenderer.Framing.Default;
        }
    }

    // ---------------------------------------------------------------- 전체 생성

    public static string BuildAll()
    {
        StringBuilder report = new StringBuilder("[아이템 외형 생성]\n");
        List<ItemData> items = LoadItems();

        try
        {
            EditorUtility.DisplayProgressBar(DialogTitle, "저폴리 모델", 0.05f);
            report.AppendLine(BuildModels(items));

            EditorUtility.DisplayProgressBar(DialogTitle, "바닥용 Prefab과 Registry", 0.25f);
            report.AppendLine(BuildPickups(items));
            AssetDatabase.SaveAssets();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        report.AppendLine(BuildIcons(items));
        report.AppendLine(BuildHeldVisualSet(items));
        report.AppendLine(ApplyPopupPrefabs());
        report.AppendLine(WireScene());
        AssetDatabase.SaveAssets();
        report.AppendLine();
        report.Append(Validate(out _));
        return report.ToString();
    }

    private static List<ItemData> LoadItems()
    {
        List<ItemData> items = new List<ItemData>();
        ItemDatabase database = AssetDatabase.LoadAssetAtPath<ItemDatabase>(ItemDatabasePath);

        if (database == null)
        {
            return items;
        }

        SerializedProperty list = new SerializedObject(database).FindProperty("items");

        for (int index = 0; index < list.arraySize; index++)
        {
            if (list.GetArrayElementAtIndex(index).objectReferenceValue is ItemData item && !items.Contains(item))
            {
                items.Add(item);
            }
        }

        items.Sort((left, right) => string.CompareOrdinal(left.ItemId, right.ItemId));
        return items;
    }

    public static string ResolveModelId(ItemData item)
    {
        if (item == null)
        {
            return null;
        }

        if (ModelByItem.TryGetValue(item.ItemId, out string modelId))
        {
            return modelId;
        }

        // 표에 없는 새 아이템은 등록된 Pickup의 저폴리 모델 기록을 사용
        WorldItemPickupRegistry registry = AssetDatabase.LoadAssetAtPath<WorldItemPickupRegistry>(PickupRegistryPath);

        if (registry != null && registry.TryGetPickup(item, out WorldItemPickup pickup))
        {
            StylizedVisualReplacement record = pickup.GetComponentInChildren<StylizedVisualReplacement>(true);

            if (record != null && !string.IsNullOrEmpty(record.ModelId))
            {
                return record.ModelId;
            }
        }

        return null;
    }

    private static string BuildModels(List<ItemData> items)
    {
        int created = 0;
        int missing = 0;

        foreach (ItemData item in items)
        {
            string modelId = ResolveModelId(item);

            if (modelId == null || !StylizedModelLibrary.Catalog.ContainsKey(modelId))
            {
                missing++;
                continue;
            }

            bool rebuild = NewModels.Contains(modelId) || StylizedArtAssetFactory.LoadModelPrefab(modelId) == null;

            if (StylizedArtAssetFactory.GetOrCreateModelPrefab(modelId, rebuild) != null && rebuild)
            {
                created++;
            }
        }

        return $"저폴리 모델 확인 {items.Count}개 (새로 만듦 {created}개, 모델 없음 {missing}개)";
    }

    // ---------------------------------------------------------------- 바닥용 Prefab

    private static string BuildPickups(List<ItemData> items)
    {
        WorldItemPickupRegistry registry = AssetDatabase.LoadAssetAtPath<WorldItemPickupRegistry>(PickupRegistryPath);

        if (registry == null)
        {
            return "[오류] 월드 아이템 Registry를 찾지 못했습니다.";
        }

        Dictionary<ItemData, WorldItemPickup> existingPrefabs = FindExistingPickupPrefabs();
        StylizedArtAssetFactory.EnsureFolder(PickupFolder);
        int registeredExisting = 0;
        int createdNew = 0;
        StringBuilder details = new StringBuilder();

        foreach (ItemData item in items)
        {
            WorldItemPickup pickup = null;

            if (NewPickups.TryGetValue(item.ItemId, out PickupSpec spec))
            {
                pickup = CreateOrUpdatePickup(item, spec, details);

                if (pickup != null && !registry.TryGetPickup(item, out _))
                {
                    createdNew++;
                }
            }
            else if (registry.TryGetPickup(item, out _))
            {
                continue;
            }
            else if (existingPrefabs.TryGetValue(item, out WorldItemPickup existing))
            {
                pickup = existing;
                registeredExisting++;
            }

            if (pickup != null)
            {
                SetRegistryEntry(registry, item, pickup);
            }
        }

        EditorUtility.SetDirty(registry);
        return $"바닥용 Prefab : 새로 만듦 {createdNew}개, 기존 Prefab 등록 {registeredExisting}개, Registry {registry.Entries.Count}개" + details;
    }

    private static Dictionary<ItemData, WorldItemPickup> FindExistingPickupPrefabs()
    {
        Dictionary<ItemData, WorldItemPickup> result = new Dictionary<ItemData, WorldItemPickup>();

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PickupSearchFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            if (path == GenericDropPath)
            {
                continue;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            WorldItemPickup pickup = prefab != null ? prefab.GetComponent<WorldItemPickup>() : null;

            if (pickup != null && pickup.ItemData != null && HasOwnVisual(pickup) && !result.ContainsKey(pickup.ItemData))
            {
                result.Add(pickup.ItemData, pickup);
            }
        }

        return result;
    }

    public static bool HasOwnVisual(WorldItemPickup pickup)
    {
        return pickup != null
            && (pickup.GetComponentInChildren<StylizedVisualReplacement>(true) != null
                || pickup.GetComponentInChildren<ContentVisualRoot>(true) != null);
    }

    private static WorldItemPickup CreateOrUpdatePickup(ItemData item, PickupSpec spec, StringBuilder report)
    {
        string path = $"{PickupFolder}/{spec.Name}.prefab";

        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null && !AssetDatabase.CopyAsset(PickupTemplatePath, path))
        {
            report.Append($"\n[오류] 바닥용 Prefab 복사 실패: {path}");
            return null;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(path);

        try
        {
            root.name = spec.Name;
            WorldItemPickup pickup = root.GetComponent<WorldItemPickup>();
            SerializedObject serialized = new SerializedObject(pickup);
            serialized.FindProperty("promptMessage").stringValue = $"F - PICK UP {item.DisplayName}";
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
                ScaleMultiplier = spec.Scale
            };

            if (!StylizedVisualReplacer.Replace(root, ResolveModelId(item), options, out string message))
            {
                report.Append("\n[경고] " + message);
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

    private static void SetRegistryEntry(WorldItemPickupRegistry registry, ItemData item, WorldItemPickup pickup)
    {
        SerializedObject serialized = new SerializedObject(registry);
        SerializedProperty entries = serialized.FindProperty("entries");
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
        }

        entry.FindPropertyRelative("pickupPrefab").objectReferenceValue = pickup;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------------------------------------------------------------- 아이콘

    public static string GetIconPath(ItemData item)
    {
        return $"{IconFolder}/ICON_{item.ItemId}.png";
    }

    public static string BuildIcons(List<ItemData> items)
    {
        StylizedArtAssetFactory.EnsureFolder(IconFolder);
        List<(ItemData item, string path)> written = new List<(ItemData, string)>();
        List<(AnimalData animal, string path)> writtenAnimals = new List<(AnimalData, string)>();
        List<AnimalData> animals = LoadAnimals();
        StringBuilder problems = new StringBuilder();

        // 1단계 : 한 번의 미리보기 세션에서 모두 찍어 PNG로 저장
        using (ItemIconRenderer renderer = new ItemIconRenderer())
        {
            for (int index = 0; index < items.Count; index++)
            {
                ItemData item = items[index];
                EditorUtility.DisplayProgressBar(DialogTitle, $"아이콘 {item.DisplayName}", (float)index / Mathf.Max(1, items.Count));
                string modelId = ResolveModelId(item);
                GameObject model = modelId != null ? StylizedArtAssetFactory.LoadModelPrefab(modelId) : null;

                if (model == null)
                {
                    problems.Append($"\n[경고] {item.ItemId} 모델이 없어 아이콘을 만들지 못했습니다.");
                    continue;
                }

                byte[] png = renderer.RenderPng(model, GetFraming(modelId), out string error);

                if (png == null)
                {
                    problems.Append($"\n[경고] {item.ItemId} 아이콘 실패 : {error}");
                    continue;
                }

                string path = GetIconPath(item);
                ItemIconRenderer.WritePng(png, path);
                written.Add((item, path));
            }

            if (animals.Count > 0)
            {
                StylizedArtAssetFactory.EnsureFolder(AnimalIconFolder);
            }

            foreach (AnimalData animal in animals)
            {
                GameObject model = animal.ModelPrefab;
                string modelId = model != null ? model.name.Replace("LP_", string.Empty) : null;
                byte[] png = model != null ? renderer.RenderPng(model, GetFraming(modelId), out _) : null;

                if (png == null)
                {
                    problems.Append($"\n[경고] {animal.AnimalId} 동물 아이콘을 만들지 못했습니다.");
                    continue;
                }

                string path = $"{AnimalIconFolder}/ICON_{animal.AnimalId}.png";
                ItemIconRenderer.WritePng(png, path);
                writtenAnimals.Add((animal, path));
            }
        }

        EditorUtility.ClearProgressBar();

        // 2단계 : 세션을 닫은 뒤 Sprite로 가져와 ItemData에 연결
        AssetDatabase.StartAssetEditing();

        try
        {
            foreach ((ItemData item, string path) in written)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }

            foreach ((AnimalData animal, string path) in writtenAnimals)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        int linked = 0;

        foreach ((ItemData item, string path) in written)
        {
            ItemIconRenderer.ImportAsSprite(path);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

            if (sprite == null)
            {
                problems.Append($"\n[경고] {item.ItemId} 아이콘 Sprite를 불러오지 못했습니다.");
                continue;
            }

            SerializedObject serialized = new SerializedObject(item);
            SerializedProperty icon = serialized.FindProperty("icon");

            if (icon.objectReferenceValue != sprite)
            {
                icon.objectReferenceValue = sprite;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(item);
            }

            linked++;
        }

        foreach ((AnimalData animal, string path) in writtenAnimals)
        {
            ItemIconRenderer.ImportAsSprite(path);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            SerializedObject serialized = new SerializedObject(animal);
            serialized.FindProperty("icon").objectReferenceValue = sprite;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(animal);
        }

        AssetDatabase.SaveAssets();
        string animalText = animals.Count > 0 ? $", 동물 아이콘 {writtenAnimals.Count}/{animals.Count}개" : string.Empty;
        return $"아이콘 {written.Count}/{items.Count}개 생성, ItemData 연결 {linked}개{animalText} ({IconFolder})" + problems;
    }

    private static List<AnimalData> LoadAnimals()
    {
        List<AnimalData> result = new List<AnimalData>();

        if (!AssetDatabase.IsValidFolder(AnimalDataFolder))
        {
            return result;
        }

        foreach (string guid in AssetDatabase.FindAssets("t:AnimalData", new[] { AnimalDataFolder }))
        {
            AnimalData animal = AssetDatabase.LoadAssetAtPath<AnimalData>(AssetDatabase.GUIDToAssetPath(guid));

            if (animal != null)
            {
                result.Add(animal);
            }
        }

        result.Sort((left, right) => string.CompareOrdinal(left.AnimalId, right.AnimalId));
        return result;
    }

    // ---------------------------------------------------------------- 손에 든 외형

    // 손에 쥐었을 때의 크기(가장 긴 변, m)
    private static float GetHeldSize(ItemData item, string modelId)
    {
        switch (modelId)
        {
            case "item_pumpkin": return 0.3f;
            case "item_wood_bundle": return 0.36f;
            case "item_plant_fiber": return 0.3f;
            case "item_arrow_bundle": return 0.55f;
            case "item_backpack": return 0.36f;
            case "item_shirt": return 0.38f;
            case "item_bandage": return 0.18f;
            case "item_grilled_fish": return 0.3f;
            case "item_mushroom_skewer": return 0.34f;
            case "item_pumpkin_soup":
            case "item_tomato_stew": return 0.24f;
            case "item_golden_feast": return 0.34f;
            case "item_animal_feed": return 0.3f;
            case "item_veggie_omelette": return 0.3f;
        }

        if (modelId.StartsWith("item_fish_"))
        {
            return 0.4f;
        }

        switch (item.ItemCategory)
        {
            case ItemCategory.Equipment: return 0.3f;
            case ItemCategory.CraftingMaterial: return 0.24f;
            default: return 0.2f;
        }
    }

    public static bool TryGetHeldPose(ItemData item, out string modelId, out HeldItemVisualSet.Entry pose)
    {
        pose = null;
        modelId = ResolveModelId(item);
        GameObject prefab = modelId != null ? StylizedArtAssetFactory.LoadModelPrefab(modelId) : null;
        MeshFilter filter = prefab != null ? prefab.GetComponentInChildren<MeshFilter>() : null;

        if (filter == null || filter.sharedMesh == null)
        {
            return false;
        }

        pose = new HeldItemVisualSet.Entry { item = item, modelPrefab = prefab };

        // 긴 도구는 돌도끼와 같은 자세 (손잡이 아래쪽을 쥠)
        if (modelId.StartsWith("tool_"))
        {
            pose.holderLocalPosition = new Vector3(0f, 0f, 0.1f);
            pose.holderLocalEuler = new Vector3(0f, 270f, 0f);
            pose.modelLocalPosition = new Vector3(0.04f, -0.3f, 0f);
            pose.modelScale = 0.62f;
            return true;
        }

        // 작은 아이템은 모델 중심을 손 기준점에 맞추고 크기를 맞춘다
        Bounds bounds = filter.sharedMesh.bounds;
        float largest = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
        float scale = GetHeldSize(item, modelId) / Mathf.Max(0.001f, largest);
        pose.holderLocalPosition = new Vector3(0f, 0.04f, 0.14f);
        pose.holderLocalEuler = modelId.StartsWith("item_fish_") ? new Vector3(0f, 270f, 70f) : new Vector3(0f, 250f, 0f);
        pose.modelLocalPosition = -bounds.center * scale;
        pose.modelScale = scale;
        return true;
    }

    private static string BuildHeldVisualSet(List<ItemData> items)
    {
        HeldItemVisualSet set = AssetDatabase.LoadAssetAtPath<HeldItemVisualSet>(HeldVisualSetPath);

        if (set == null)
        {
            set = ScriptableObject.CreateInstance<HeldItemVisualSet>();
            AssetDatabase.CreateAsset(set, HeldVisualSetPath);
        }

        SerializedObject serialized = new SerializedObject(set);
        SerializedProperty entries = serialized.FindProperty("entries");
        entries.ClearArray();
        int missing = 0;

        foreach (ItemData item in items)
        {
            if (DedicatedHeldItems.Contains(item.ItemId))
            {
                continue;
            }

            if (!TryGetHeldPose(item, out _, out HeldItemVisualSet.Entry pose))
            {
                missing++;
                continue;
            }

            entries.arraySize++;
            SerializedProperty entry = entries.GetArrayElementAtIndex(entries.arraySize - 1);
            entry.FindPropertyRelative("item").objectReferenceValue = pose.item;
            entry.FindPropertyRelative("modelPrefab").objectReferenceValue = pose.modelPrefab;
            entry.FindPropertyRelative("holderLocalPosition").vector3Value = pose.holderLocalPosition;
            entry.FindPropertyRelative("holderLocalEuler").vector3Value = pose.holderLocalEuler;
            entry.FindPropertyRelative("modelLocalPosition").vector3Value = pose.modelLocalPosition;
            entry.FindPropertyRelative("modelScale").floatValue = pose.modelScale;
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(set);
        return $"손에 든 외형 목록 {set.Entries.Count}개 (전용 외형 {DedicatedHeldItems.Count}개 제외{(missing > 0 ? $", 모델 없음 {missing}개" : string.Empty)})";
    }

    private static string SetupHeldVisualsInScene()
    {
        EquippedToolView toolView = Object.FindFirstObjectByType<EquippedToolView>(FindObjectsInactive.Include);

        if (toolView == null)
        {
            return "[경고] Scene에 EquippedToolView가 없어 손에 든 외형 연결을 건너뛰었습니다.";
        }

        SerializedObject serialized = new SerializedObject(toolView);
        serialized.FindProperty("heldItemVisuals").objectReferenceValue = AssetDatabase.LoadAssetAtPath<HeldItemVisualSet>(HeldVisualSetPath);
        GameObject template = serialized.FindProperty("axeVisual").objectReferenceValue as GameObject;
        GameObject bow = serialized.FindProperty("bowVisual").objectReferenceValue as GameObject;

        if (bow == null && template != null)
        {
            bow = Object.Instantiate(template, template.transform.parent);
            Undo.RegisterCreatedObjectUndo(bow, "Create Bow Visual");
            bow.name = BowVisualName;
            bow.transform.SetSiblingIndex(template.transform.GetSiblingIndex() + 1);
            serialized.FindProperty("bowVisual").objectReferenceValue = bow;
        }

        serialized.ApplyModifiedProperties();

        if (bow == null)
        {
            return "[경고] 활 외형을 만들 기준(도끼 외형)을 찾지 못했습니다.";
        }

        StylizedVisualReplacer.Options options = new StylizedVisualReplacer.Options
        {
            ParentOverride = bow.transform,
            AlignModelUpToLongestAxis = true,
            BladeHintObjectName = "Head",
            ScaleMultiplier = 1.5f,
            UseUndo = true
        };

        StylizedVisualReplacer.Replace(bow, "tool_bow", options, out _);
        StylizedVisualReplacement record = bow.GetComponent<StylizedVisualReplacement>();
        Transform arrow = bow.transform.Find(NockedArrowName);

        if (arrow == null)
        {
            GameObject arrowModel = (GameObject)PrefabUtility.InstantiatePrefab(StylizedArtAssetFactory.GetOrCreateModelPrefab("projectile_arrow", false));
            Undo.RegisterCreatedObjectUndo(arrowModel, "Create Nocked Arrow");
            arrowModel.name = NockedArrowName;
            arrowModel.transform.SetParent(bow.transform, false);
            arrow = arrowModel.transform;
        }

        // 활 모델 : 손잡이는 높이 0.56, 몸체가 휘는 +X가 과녁 방향, 시위는 X = 0
        // 휜 몸체가 플레이어 정면을 보도록 모델을 세로축 기준으로 돌린다
        if (record != null && record.GeneratedVisual != null)
        {
            Transform model = record.GeneratedVisual.transform;
            Vector3 up = model.TransformDirection(Vector3.up).normalized;
            Vector3 forward = Vector3.ProjectOnPlane(toolView.transform.forward, up).normalized;
            Vector3 currentAim = Vector3.ProjectOnPlane(model.TransformDirection(Vector3.right), up).normalized;
            Vector3 grip = model.TransformPoint(new Vector3(0f, 0.56f, 0f));
            Undo.RecordObject(model, "Face Bow Forward");
            model.RotateAround(grip, up, Vector3.SignedAngle(currentAim, forward, up));
            Vector3 aim = model.TransformDirection(Vector3.right).normalized;
            float arrowScale = model.lossyScale.y * 0.8f;
            Undo.RecordObject(arrow, "Place Nocked Arrow");
            arrow.localScale = Vector3.one * (arrowScale / Mathf.Max(0.001f, bow.transform.lossyScale.y));
            arrow.rotation = Quaternion.LookRotation(aim, up);
            arrow.position = model.TransformPoint(new Vector3(0f, 0.56f, 0f)) + aim * (0.3f * arrowScale);
        }

        arrow.gameObject.SetActive(false);
        bow.SetActive(false);

        PlayerBowChargeController bowController = Object.FindFirstObjectByType<PlayerBowChargeController>(FindObjectsInactive.Include);

        if (bowController != null)
        {
            SerializedObject bowSerialized = new SerializedObject(bowController);
            bowSerialized.FindProperty("nockedArrowVisual").objectReferenceValue = arrow.gameObject;
            bowSerialized.ApplyModifiedProperties();
        }

        return "손에 든 외형 : 활 전용 외형과 장전 화살 연결, 그 밖의 아이템은 외형 목록 사용";
    }

    // ---------------------------------------------------------------- UI

    private static string ApplyPopupPrefabs()
    {
        int changed = 0;

        foreach (string path in new[] { InventoryPopupPath, StoragePopupPath })
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                continue;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(path);

            try
            {
                changed += KeepIconAspect(root.transform);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        return $"인벤토리·보관함 팝업 아이콘 비율 유지 {changed}곳";
    }

    // 슬롯·장비·상세 아이콘 Image가 정사각형 아이콘을 늘리지 않도록 한다
    private static int KeepIconAspect(Transform root)
    {
        int changed = 0;

        foreach (MonoBehaviour view in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (!(view is InventorySlotView || view is StorageSlotView || view is EquipmentSlotUI || view is InventoryDetailUI))
            {
                continue;
            }

            SerializedProperty property = new SerializedObject(view).FindProperty("itemIconImage");
            Image image = property != null ? property.objectReferenceValue as Image : null;

            if (image != null && !image.preserveAspect)
            {
                image.preserveAspect = true;
                EditorUtility.SetDirty(image);
                changed++;
            }
        }

        return changed;
    }

    private static string WireScene()
    {
        StringBuilder report = new StringBuilder();
        Scene scene = SceneManager.GetActiveScene();
        WorldItemPickupRegistry registry = AssetDatabase.LoadAssetAtPath<WorldItemPickupRegistry>(PickupRegistryPath);
        InventoryItemDropper dropper = Object.FindFirstObjectByType<InventoryItemDropper>(FindObjectsInactive.Include);

        if (dropper != null)
        {
            SerializedObject serialized = new SerializedObject(dropper);
            serialized.FindProperty("pickupRegistry").objectReferenceValue = registry;
            serialized.ApplyModifiedProperties();
            report.AppendLine("아이템 버리기 : 아이템별 외형 Prefab 연결");
        }
        else
        {
            report.AppendLine("[경고] Scene에 InventoryItemDropper가 없어 버리기 연결을 건너뛰었습니다.");
        }

        InventorySlotsUI hotbar = FindHotbar();

        if (hotbar == null)
        {
            report.Append("[경고] 핫바를 찾지 못했습니다. 게임 Scene(20_Gameplay)을 연 뒤 다시 실행하세요.");
            return report.ToString();
        }

        report.AppendLine(SetupHeldVisualsInScene());
        report.AppendLine(SetupHotbarTemplate(hotbar));
        report.AppendLine(SetupSelectedItemName(hotbar));
        KeepIconAspect(hotbar.transform);
        EditorSceneManager.MarkSceneDirty(scene);
        report.Append("Scene 변경 완료 → Ctrl+S로 저장하세요");
        return report.ToString();
    }

    private static InventorySlotsUI FindHotbar()
    {
        foreach (InventorySlotsUI slots in Object.FindObjectsByType<InventorySlotsUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (slots.gameObject.scene.IsValid() && slots.name == "HotbarPanel")
            {
                return slots;
            }
        }

        return null;
    }

    private static InventorySlotView FindHotbarTemplate(InventorySlotsUI hotbar)
    {
        Object template = new SerializedObject(hotbar).FindProperty("slotTemplate").objectReferenceValue;

        switch (template)
        {
            case InventorySlotView view:
                return view;
            case GameObject gameObject:
                return gameObject.GetComponent<InventorySlotView>();
            case Component component:
                return component.GetComponent<InventorySlotView>();
            default:
                return null;
        }
    }

    // 핫바 칸 : 이름 대신 아이콘을 크게 (아이콘이 없는 아이템은 이름 표시)
    private static string SetupHotbarTemplate(InventorySlotsUI hotbar)
    {
        InventorySlotView view = FindHotbarTemplate(hotbar);

        if (view == null)
        {
            return "[경고] 핫바 칸 Template을 찾지 못했습니다.";
        }

        SerializedObject serialized = new SerializedObject(view);
        serialized.FindProperty("hideNameWhenIconShown").boolValue = true;
        serialized.ApplyModifiedProperties();

        Image icon = serialized.FindProperty("itemIconImage").objectReferenceValue as Image;

        if (icon != null)
        {
            Undo.RecordObject(icon.rectTransform, "Hotbar Icon Layout");
            Undo.RecordObject(icon, "Hotbar Icon Layout");
            icon.rectTransform.anchorMin = Vector2.zero;
            icon.rectTransform.anchorMax = Vector2.one;
            icon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            icon.rectTransform.anchoredPosition = new Vector2(0f, 1f);
            icon.rectTransform.sizeDelta = new Vector2(-14f, -14f);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
        }

        return "핫바 칸 : 아이콘 58px, 아이콘이 있으면 이름 숨김";
    }

    // 선택 아이템 이름 : 핫바 바로 위 (물뿌리개 게이지와 같은 자리, 게이지가 보이면 숨김)
    private static string SetupSelectedItemName(InventorySlotsUI hotbar)
    {
        RectTransform hotbarRect = (RectTransform)hotbar.transform;
        Transform canvas = hotbarRect.parent;
        Transform existing = canvas.Find(SelectedNameRootName);

        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        GameObject rootObject = new GameObject(SelectedNameRootName, typeof(RectTransform), typeof(CanvasGroup));
        rootObject.layer = canvas.gameObject.layer;
        rootObject.transform.SetParent(canvas, false);
        Undo.RegisterCreatedObjectUndo(rootObject, "Create Selected Item Name");
        rootObject.transform.SetSiblingIndex(hotbarRect.GetSiblingIndex() + 1);
        RectTransform root = (RectTransform)rootObject.transform;
        root.anchorMin = hotbarRect.anchorMin;
        root.anchorMax = hotbarRect.anchorMax;
        root.pivot = new Vector2(0.5f, 0f);
        root.anchoredPosition = new Vector2(0f, hotbarRect.anchoredPosition.y + hotbarRect.sizeDelta.y + 14f);
        root.sizeDelta = new Vector2(hotbarRect.sizeDelta.x, 26f);

        GameObject pillObject = new GameObject("LP_Pill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        pillObject.layer = rootObject.layer;
        pillObject.transform.SetParent(root, false);
        RectTransform pill = (RectTransform)pillObject.transform;
        pill.anchorMin = new Vector2(0.5f, 0f);
        pill.anchorMax = new Vector2(0.5f, 0f);
        pill.pivot = new Vector2(0.5f, 0f);
        pill.anchoredPosition = Vector2.zero;
        Image pillImage = pillObject.GetComponent<Image>();
        pillImage.sprite = UISpriteFactory.Pill;
        pillImage.type = Image.Type.Sliced;
        pillImage.pixelsPerUnitMultiplier = 1.2f;
        pillImage.color = new Color(0.05f, 0.06f, 0.08f, 0.82f);
        pillImage.raycastTarget = false;
        HorizontalLayoutGroup layout = pillObject.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 4, 4);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = pillObject.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject textObject = new GameObject("LP_ItemName", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.layer = rootObject.layer;
        textObject.transform.SetParent(pill, false);
        TMP_Text text = textObject.GetComponent<TMP_Text>();

        if (TMP_Settings.defaultFontAsset != null)
        {
            text.font = TMP_Settings.defaultFontAsset;
        }

        text.text = "ITEM NAME";
        text.fontSize = 14f;
        text.fontStyle = FontStyles.Bold;
        text.characterSpacing = 2f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = ProjectUUIPalette.TextPrimary;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;

        GameObject gauge = FindWaterGaugePanel(canvas);
        SelectedItemNameUI ui = rootObject.AddComponent<SelectedItemNameUI>();
        SerializedObject serialized = new SerializedObject(ui);
        serialized.FindProperty("playerInventory").objectReferenceValue = new SerializedObject(hotbar).FindProperty("playerInventory").objectReferenceValue;
        serialized.FindProperty("canvasGroup").objectReferenceValue = rootObject.GetComponent<CanvasGroup>();
        serialized.FindProperty("nameText").objectReferenceValue = text;
        serialized.FindProperty("hideWhileActive").objectReferenceValue = gauge;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        CanvasGroup group = rootObject.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;
        return $"선택 아이템 이름 표시 (핫바 위 {root.anchoredPosition.y:0}px{(gauge != null ? ", 물뿌리개 게이지가 보이면 숨김" : string.Empty)})";
    }

    // 물뿌리개 게이지는 바깥 틀이 항상 켜져 있고 안쪽 패널만 켜졌다 꺼진다
    private static GameObject FindWaterGaugePanel(Transform canvas)
    {
        Transform container = canvas.Find(WaterGaugeName);
        WateringCanGaugeUI gauge = container != null ? container.GetComponent<WateringCanGaugeUI>() : null;

        if (gauge == null)
        {
            return null;
        }

        GameObject panel = new SerializedObject(gauge).FindProperty("gaugeRoot").objectReferenceValue as GameObject;
        return panel != null ? panel : container.gameObject;
    }

    // ---------------------------------------------------------------- 검증

    public static string Validate(out int errorCount)
    {
        StringBuilder report = new StringBuilder("[아이템 외형 검증]\n");
        int errors = 0;
        List<ItemData> items = LoadItems();
        WorldItemPickupRegistry registry = AssetDatabase.LoadAssetAtPath<WorldItemPickupRegistry>(PickupRegistryPath);
        int modelOk = 0;
        int iconOk = 0;
        int pickupOk = 0;

        void Error(string message)
        {
            errors++;
            report.AppendLine("✗ " + message);
        }

        if (registry == null)
        {
            Error("월드 아이템 Registry 없음");
        }
        else if (!registry.TryValidate(out string registryError))
        {
            Error(registryError);
        }

        foreach (ItemData item in items)
        {
            string modelId = ResolveModelId(item);

            if (modelId == null || StylizedArtAssetFactory.LoadModelPrefab(modelId) == null)
            {
                Error($"{item.ItemId} : 저폴리 모델 없음");
            }
            else
            {
                modelOk++;
            }

            Sprite expected = AssetDatabase.LoadAssetAtPath<Sprite>(GetIconPath(item));

            if (item.Icon == null)
            {
                Error($"{item.ItemId} : 아이콘 없음");
            }
            else if (item.Icon != expected)
            {
                Error($"{item.ItemId} : 아이콘이 생성 파일과 다름");
            }
            else
            {
                iconOk++;
            }

            if (registry == null || !registry.TryGetPickup(item, out WorldItemPickup pickup))
            {
                Error($"{item.ItemId} : 바닥용 Prefab 미등록 (버리면 자루 모양)");
            }
            else if (pickup.ItemData != item)
            {
                Error($"{item.ItemId} : 등록된 Prefab의 아이템이 다름 ({pickup.name})");
            }
            else if (!HasOwnVisual(pickup))
            {
                Error($"{item.ItemId} : 등록된 Prefab에 저폴리 외형이 없음 ({pickup.name})");
            }
            else
            {
                pickupOk++;
            }
        }

        HeldItemVisualSet heldSet = AssetDatabase.LoadAssetAtPath<HeldItemVisualSet>(HeldVisualSetPath);
        int heldOk = 0;

        foreach (ItemData item in items)
        {
            if (DedicatedHeldItems.Contains(item.ItemId) || (heldSet != null && heldSet.TryGet(item, out _)))
            {
                heldOk++;
            }
            else
            {
                Error($"{item.ItemId} : 손에 든 외형 없음");
            }
        }

        report.AppendLine($"아이템 {items.Count}개 : 모델 {modelOk} / 아이콘 {iconOk} / 바닥용 Prefab {pickupOk} / 손에 든 외형 {heldOk}");
        ValidateScene(report, Error);
        errorCount = errors;
        report.AppendLine(errors == 0 ? "결과 : 오류 0개" : $"결과 : 오류 {errors}개");
        return report.ToString();
    }

    private static void ValidateScene(StringBuilder report, System.Action<string> error)
    {
        InventorySlotsUI hotbar = FindHotbar();

        if (hotbar == null)
        {
            report.AppendLine("Scene 검사 생략 (게임 Scene이 열려 있지 않음)");
            return;
        }

        InventoryItemDropper dropper = Object.FindFirstObjectByType<InventoryItemDropper>(FindObjectsInactive.Include);

        if (dropper != null && new SerializedObject(dropper).FindProperty("pickupRegistry").objectReferenceValue == null)
        {
            error("InventoryItemDropper에 Pickup Registry가 연결되지 않았습니다.");
        }

        InventorySlotView view = FindHotbarTemplate(hotbar);

        if (view == null || !new SerializedObject(view).FindProperty("hideNameWhenIconShown").boolValue)
        {
            error("핫바 칸이 아이콘 표시 방식으로 설정되지 않았습니다.");
        }

        EquippedToolView toolView = Object.FindFirstObjectByType<EquippedToolView>(FindObjectsInactive.Include);

        if (toolView != null)
        {
            SerializedObject toolSerialized = new SerializedObject(toolView);

            foreach (string property in new[] { "bowVisual", "heldItemVisuals", "hoeVisual", "wateringCanVisual", "fishingRodVisual" })
            {
                if (toolSerialized.FindProperty(property).objectReferenceValue == null)
                {
                    error($"EquippedToolView.{property} 연결이 비어 있습니다.");
                }
            }
        }

        PlayerBowChargeController bowController = Object.FindFirstObjectByType<PlayerBowChargeController>(FindObjectsInactive.Include);

        if (bowController != null && new SerializedObject(bowController).FindProperty("nockedArrowVisual").objectReferenceValue == null)
        {
            error("PlayerBowChargeController에 장전 화살 외형이 연결되지 않았습니다.");
        }

        Transform nameRoot = hotbar.transform.parent.Find(SelectedNameRootName);
        SelectedItemNameUI nameUI = nameRoot != null ? nameRoot.GetComponent<SelectedItemNameUI>() : null;

        if (nameUI == null)
        {
            error("선택 아이템 이름 표시가 없습니다.");
        }
        else
        {
            SerializedObject serialized = new SerializedObject(nameUI);

            foreach (string property in new[] { "playerInventory", "canvasGroup", "nameText" })
            {
                if (serialized.FindProperty(property).objectReferenceValue == null)
                {
                    error($"SelectedItemNameUI.{property} 연결이 비어 있습니다.");
                }
            }
        }

        report.AppendLine("Scene 연결 확인 (핫바·버리기·선택 이름·손에 든 외형)");
    }
}
