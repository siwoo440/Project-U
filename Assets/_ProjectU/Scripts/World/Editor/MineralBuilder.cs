using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// 117일차: 광물 · 제련 · 도구 등급
// 1. 광석 · 보석 · 주괴 · 운석 조각 아이템과 한글 이름 · 아이콘 · 바닥 Prefab
// 2. 용광로(건축물)와 제련법 (광석 + 석탄 → 주괴) — 요리 시스템을 그대로 쓴다
// 3. 등급 도구 (구리 · 철 · 강철 곡괭이 · 도끼)와 작업대 제작법
// 4. 보석 · 금은 이웃 선물 태그로 이어 준다
// 5. 밤의 운석 (구덩이 · 운석 덩어리 · Scene 관리자)
// 6. 대장간 주문 (드라비아 게시판 의뢰 3개)
// 자동 적용(ProjectUAutoContent)이 실행한다. 여러 번 실행해도 같은 결과가 나온다.
public static class MineralBuilder
{
    private const string ScenePath = "Assets/_ProjectU/Scenes/20_Gameplay.unity";
    private const string ItemFolder = "Assets/_ProjectU/Data/Items/Day117";
    private const string CookingFolder = "Assets/_ProjectU/Data/Cooking/Day117";
    private const string CraftingFolder = "Assets/_ProjectU/Data/Crafting/Day117";
    private const string BuildingFolder = "Assets/_ProjectU/Data/Building/Day117";
    private const string PrefabFolder = "Assets/_ProjectU/Prefabs/Building/Day117";
    private const string FurnacePlacedPath = PrefabFolder + "/FurnacePlaced.prefab";
    private const string FurnacePreviewPath = PrefabFolder + "/FurnacePreview.prefab";
    private const string StoneCampfirePlacedPath = "Assets/_ProjectU/Prefabs/Building/Day73/StoneCampfirePlaced.prefab";
    private const string StoneCampfirePreviewPath = "Assets/_ProjectU/Prefabs/Building/Day73/StoneCampfirePreview.prefab";
    private const string FurnaceRecipePath = BuildingFolder + "/BuildRecipe_Furnace.asset";
    private const string ItemDatabasePath = "Assets/_ProjectU/Data/Databases/ItemDatabase.asset";
    private const string MarketCatalogPath = "Assets/_ProjectU/Data/Market/MarketCatalog.asset";
    private const string KoreanNamesPath = "Assets/_ProjectU/Data/Items/ItemKoreanNames.csv";
    private const string GiftTagsPath = "Assets/_ProjectU/Data/Npc/Source/NpcGiftTags.csv";
    private const string GiftsPath = "Assets/_ProjectU/Data/Npc/Source/NpcGifts.csv";
    private const string WoodPath = "Assets/_ProjectU/Data/Items/ItemData_Wood.asset";
    private const string StonePath = "Assets/_ProjectU/Data/Items/ItemData_Stone.asset";
    private const string IronOrePath = "Assets/_ProjectU/Data/Items/Day71/ItemData_IronOre.asset";
    private const string IronAxePath = "Assets/_ProjectU/Data/Items/Day71/ItemData_IronAxe.asset";
    private const string TorchPath = "Assets/_ProjectU/Data/Items/Day116/ItemData_Torch.asset";

    public const string GemTag = "gem"; // 보석 선물 태그
    public const string TreasureTag = "treasure"; // 금 · 은 선물 태그

    public sealed class ItemSpec
    {
        public string Id;
        public string Asset;
        public string English;
        public string Korean;
        public string Description;
        public string Model; // 저폴리 모델 ID (비어 있으면 아이템 ID와 같다)
        public ItemCategory Category = ItemCategory.CraftingMaterial;
        public int Stack = 20;
        public ToolType Tool = ToolType.None;
        public int Tier;
    }

    // 광석 · 보석 · 주괴 · 도구 (모델 ID는 아이템 ID와 같다)
    public static readonly ItemSpec[] Items =
    {
        new ItemSpec { Id = "resource_coal", Asset = "ItemData_Coal", English = "COAL", Korean = "석탄", Description = "Black rock that burns hot and long." },
        new ItemSpec { Id = "resource_copper_ore", Asset = "ItemData_CopperOre", English = "COPPER ORE", Korean = "구리 광석", Description = "Raw copper from the cave walls." },
        new ItemSpec { Id = "resource_silver_ore", Asset = "ItemData_SilverOre", English = "SILVER ORE", Korean = "은 광석", Description = "Shiny ore from the deep cave." },
        new ItemSpec { Id = "resource_gold_ore", Asset = "ItemData_GoldOre", English = "GOLD ORE", Korean = "금 광석", Description = "Rare ore that glitters in torchlight." },
        new ItemSpec { Id = "resource_crystal", Asset = "ItemData_Crystal", English = "CRYSTAL", Korean = "수정", Description = "Clear crystal used to sharpen tools." },
        new ItemSpec { Id = "resource_meteorite_shard", Asset = "ItemData_MeteoriteShard", English = "METEORITE SHARD", Korean = "운석 조각", Description = "A shard from a fallen star." },
        new ItemSpec { Id = "resource_gem_ruby", Model = "gem_ruby", Asset = "ItemData_Ruby", English = "RUBY", Korean = "루비", Description = "A deep red gem.", Stack = 10 },
        new ItemSpec { Id = "resource_gem_sapphire", Model = "gem_sapphire", Asset = "ItemData_Sapphire", English = "SAPPHIRE", Korean = "사파이어", Description = "A clear blue gem.", Stack = 10 },
        new ItemSpec { Id = "resource_gem_emerald", Model = "gem_emerald", Asset = "ItemData_Emerald", English = "EMERALD", Korean = "에메랄드", Description = "A green gem the colour of spring.", Stack = 10 },
        new ItemSpec { Id = "resource_gem_amethyst", Model = "gem_amethyst", Asset = "ItemData_Amethyst", English = "AMETHYST", Korean = "자수정", Description = "A purple gem from the deep cave.", Stack = 10 },
        new ItemSpec { Id = "resource_gem_diamond", Model = "gem_diamond", Asset = "ItemData_Diamond", English = "DIAMOND", Korean = "다이아몬드", Description = "The hardest gem on the island.", Stack = 10 },
        new ItemSpec { Id = "resource_ingot_copper", Model = "ingot_copper", Asset = "ItemData_CopperIngot", English = "COPPER INGOT", Korean = "구리 주괴", Description = "Smelted copper, ready for tools." },
        new ItemSpec { Id = "resource_ingot_iron", Model = "ingot_iron", Asset = "ItemData_IronIngot", English = "IRON INGOT", Korean = "철 주괴", Description = "Smelted iron, ready for tools." },
        new ItemSpec { Id = "resource_ingot_silver", Model = "ingot_silver", Asset = "ItemData_SilverIngot", English = "SILVER INGOT", Korean = "은 주괴", Description = "Smelted silver for fine work." },
        new ItemSpec { Id = "resource_ingot_gold", Model = "ingot_gold", Asset = "ItemData_GoldIngot", English = "GOLD INGOT", Korean = "금 주괴", Description = "Smelted gold, a fine gift." },
        new ItemSpec { Id = "resource_ingot_steel", Model = "ingot_steel", Asset = "ItemData_SteelIngot", English = "STEEL INGOT", Korean = "강철 주괴", Description = "Iron forged again with coal." },
        new ItemSpec { Id = "resource_ingot_meteorite", Model = "ingot_meteorite", Asset = "ItemData_MeteoriteIngot", English = "METEORITE INGOT", Korean = "운석 주괴", Description = "Star metal, still faintly warm." },
        new ItemSpec { Id = "tool_pickaxe_copper", Asset = "ItemData_CopperPickaxe", English = "COPPER PICKAXE", Korean = "구리 곡괭이", Description = "Mines faster than stone.", Category = ItemCategory.Tool, Stack = 1, Tool = ToolType.Pickaxe, Tier = 1 },
        new ItemSpec { Id = "tool_pickaxe_iron", Asset = "ItemData_IronPickaxe", English = "IRON PICKAXE", Korean = "철 곡괭이", Description = "Mines silver, crystal and gems.", Category = ItemCategory.Tool, Stack = 1, Tool = ToolType.Pickaxe, Tier = 2 },
        new ItemSpec { Id = "tool_pickaxe_steel", Asset = "ItemData_SteelPickaxe", English = "STEEL PICKAXE", Korean = "강철 곡괭이", Description = "The best pickaxe on the island.", Category = ItemCategory.Tool, Stack = 1, Tool = ToolType.Pickaxe, Tier = 3 },
        new ItemSpec { Id = "tool_axe_copper", Asset = "ItemData_CopperAxe", English = "COPPER AXE", Korean = "구리 도끼", Description = "Chops faster than stone.", Category = ItemCategory.Tool, Stack = 1, Tool = ToolType.Axe, Tier = 1 },
        new ItemSpec { Id = "tool_axe_steel", Asset = "ItemData_SteelAxe", English = "STEEL AXE", Korean = "강철 도끼", Description = "The best axe on the island.", Category = ItemCategory.Tool, Stack = 1, Tool = ToolType.Axe, Tier = 3 }
    };

    private sealed class SmeltSpec
    {
        public string Id;
        public string Asset;
        public string English;
        public string ResultId;
        public int ResultAmount = 1;
        public float Seconds;
        public (string id, int amount)[] Ingredients;
    }

    // 용광로 제련법 (광석 + 석탄 → 주괴)
    private static readonly SmeltSpec[] Smelts =
    {
        new SmeltSpec { Id = "smelt_copper", Asset = "CookingRecipe_SmeltCopper", English = "COPPER INGOT", ResultId = "resource_ingot_copper", Seconds = 20f, Ingredients = new[] { ("resource_copper_ore", 2), ("resource_coal", 1) } },
        new SmeltSpec { Id = "smelt_iron", Asset = "CookingRecipe_SmeltIron", English = "IRON INGOT", ResultId = "resource_ingot_iron", Seconds = 26f, Ingredients = new[] { ("item_iron_ore", 2), ("resource_coal", 1) } },
        new SmeltSpec { Id = "smelt_silver", Asset = "CookingRecipe_SmeltSilver", English = "SILVER INGOT", ResultId = "resource_ingot_silver", Seconds = 30f, Ingredients = new[] { ("resource_silver_ore", 2), ("resource_coal", 1) } },
        new SmeltSpec { Id = "smelt_gold", Asset = "CookingRecipe_SmeltGold", English = "GOLD INGOT", ResultId = "resource_ingot_gold", Seconds = 34f, Ingredients = new[] { ("resource_gold_ore", 2), ("resource_coal", 1) } },
        new SmeltSpec { Id = "smelt_steel", Asset = "CookingRecipe_SmeltSteel", English = "STEEL INGOT", ResultId = "resource_ingot_steel", Seconds = 40f, Ingredients = new[] { ("resource_ingot_iron", 2), ("resource_coal", 2) } },
        new SmeltSpec { Id = "smelt_meteorite", Asset = "CookingRecipe_SmeltMeteorite", English = "METEORITE INGOT", ResultId = "resource_ingot_meteorite", Seconds = 45f, Ingredients = new[] { ("resource_meteorite_shard", 3), ("resource_coal", 2) } }
    };

    private sealed class CraftSpec
    {
        public string Id;
        public string Asset;
        public string English;
        public string ResultId;
        public int ResultAmount = 1;
        public CraftingFacilityType Facility = CraftingFacilityType.Workbench;
        public (string id, int amount)[] Ingredients;
    }

    // 작업대 제작법 (주괴 → 도구) · 석탄 횃불
    private static readonly CraftSpec[] Crafts =
    {
        new CraftSpec { Id = "recipe_pickaxe_copper", Asset = "CraftingRecipe_CopperPickaxe", English = "COPPER PICKAXE", ResultId = "tool_pickaxe_copper", Ingredients = new[] { ("resource_ingot_copper", 3), ("item_wood", 2) } },
        new CraftSpec { Id = "recipe_pickaxe_iron", Asset = "CraftingRecipe_IronPickaxe", English = "IRON PICKAXE", ResultId = "tool_pickaxe_iron", Ingredients = new[] { ("resource_ingot_iron", 3), ("item_wood", 2) } },
        new CraftSpec { Id = "recipe_pickaxe_steel", Asset = "CraftingRecipe_SteelPickaxe", English = "STEEL PICKAXE", ResultId = "tool_pickaxe_steel", Ingredients = new[] { ("resource_ingot_steel", 3), ("item_wood", 2) } },
        new CraftSpec { Id = "recipe_axe_copper", Asset = "CraftingRecipe_CopperAxe", English = "COPPER AXE", ResultId = "tool_axe_copper", Ingredients = new[] { ("resource_ingot_copper", 3), ("item_wood", 2) } },
        new CraftSpec { Id = "recipe_axe_steel", Asset = "CraftingRecipe_SteelAxe", English = "STEEL AXE", ResultId = "tool_axe_steel", Ingredients = new[] { ("resource_ingot_steel", 3), ("item_wood", 2) } },
        new CraftSpec { Id = "recipe_torch_coal", Asset = "CraftingRecipe_CoalTorch", English = "TORCH", ResultId = "item_torch", ResultAmount = 4, Facility = CraftingFacilityType.Hand, Ingredients = new[] { ("item_wood", 1), ("resource_coal", 1) } }
    };

    // 보석 · 금은 이웃 선물 (태그로 묶어 35명 취향에 연결)
    private static readonly (string character, string preference, string tag)[] GiftPreferences =
    {
        ("char_lichel", "Loved", GemTag),
        ("char_pipi", "Loved", TreasureTag),
        ("char_chesca", "Loved", GemTag),
        ("char_dravia", "Loved", TreasureTag),
        ("char_bellamorta", "Liked", GemTag),
        ("char_seira", "Liked", GemTag),
        ("char_arachne", "Liked", GemTag),
        ("char_serena", "Liked", GemTag),
        ("char_milu", "Liked", GemTag),
        ("char_lilica", "Liked", TreasureTag),
        ("char_ravenna", "Liked", TreasureTag)
    };

    // ---------------------------------------------------------------- 만들기

    // 1단계: 아이템만 먼저 만든다. 111 · 113일차 의뢰(대장간 주문)가 이 아이템을 쓰기 때문에 순서가 앞이다.
    public static string BuildItems()
    {
        StringBuilder report = new StringBuilder("[광물 아이템 만들기]\n");

        try
        {
            EnsureFolders();
            EditorUtility.DisplayProgressBar("Project U 광물", "광물 모델", 0.1f);
            report.AppendLine($"광물 모델 {EnsureModels()}종 준비");

            EditorUtility.DisplayProgressBar("Project U 광물", "아이템", 0.4f);
            Dictionary<string, ItemData> items = CreateItems();
            report.AppendLine($"광물 · 주괴 · 도구 아이템 {items.Count}개");
            report.AppendLine(UpdateKoreanNames());
            report.AppendLine(SetIronTiers());
            report.AppendLine(RegisterItems(items));
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayProgressBar("Project U 광물", "판매 가격 · 외형", 0.7f);
            MarketContentBuilder.RefreshCatalog(); // 광석 · 보석 · 주괴 판매 가격
            GameDataRegistryEditor.CreateOrRefreshDefaultRegistry();
            string visual = ItemVisualContentBuilder.BuildAll(); // 모델 · 아이콘 · 바닥 Prefab · 손에 든 외형
            report.AppendLine(visual.Split('\n').FirstOrDefault(line => line.StartsWith("✗")) ?? "판매 가격 · 아이콘 · 바닥 Prefab 생성");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
        }

        return report.ToString();
    }

    private static Dictionary<string, ItemData> CreateItems()
    {
        Dictionary<string, ItemData> items = new Dictionary<string, ItemData>(StringComparer.Ordinal);

        foreach (ItemSpec spec in Items)
        {
            items[spec.Id] = CreateItem(spec);
        }

        return items;
    }

    // 2단계: 제련 · 광맥 · 용광로 · 운석 · 선물 · 대장간 주문
    public static string BuildAll(bool saveScene)
    {
        StringBuilder report = new StringBuilder("[광물 · 제련 · 도구 등급 만들기]\n");

        try
        {
            EnsureFolders();
            EditorUtility.DisplayProgressBar("Project U 광물", "광물 모델", 0.05f);
            report.AppendLine($"광물 모델 {EnsureModels()}종 준비");

            EditorUtility.DisplayProgressBar("Project U 광물", "아이템", 0.2f);
            Dictionary<string, ItemData> items = CreateItems();
            report.AppendLine($"광물 · 주괴 · 도구 아이템 {items.Count}개");
            report.AppendLine(UpdateKoreanNames());
            report.AppendLine(SetIronTiers());

            EditorUtility.DisplayProgressBar("Project U 광물", "제련법 · 제작법", 0.4f);
            List<CookingRecipeData> smelts = Smelts.Select(CreateSmelt).Where(recipe => recipe != null).ToList();
            report.AppendLine($"용광로 제련법 {smelts.Count}개 (광석 + 석탄 → 주괴)");
            int crafts = Crafts.Count(spec => CreateCraft(spec) != null);
            report.AppendLine($"작업대 · 맨손 제작법 {crafts}개 (등급 도구 · 석탄 횃불)");

            EditorUtility.DisplayProgressBar("Project U 광물", "광맥 Prefab", 0.5f);
            report.AppendLine(BuildVeinPrefabs());

            EditorUtility.DisplayProgressBar("Project U 광물", "용광로", 0.55f);
            report.AppendLine(BuildFurnace(smelts, items));

            EditorUtility.DisplayProgressBar("Project U 광물", "밤의 운석", 0.65f);
            report.AppendLine(BuildMeteorContent(saveScene));

            EditorUtility.DisplayProgressBar("Project U 광물", "선물 취향", 0.7f);
            report.AppendLine(UpdateGiftTags());

            EditorUtility.DisplayProgressBar("Project U 광물", "아이템 등록 · 외형", 0.8f);
            report.AppendLine(RegisterItems(items));
            AssetDatabase.SaveAssets();
            MarketContentBuilder.RefreshCatalog(); // 광석 · 보석 · 주괴 판매 가격 넣기
            report.AppendLine("판매 상자 가격표 갱신 (광석 · 보석 · 주괴)");

            EditorUtility.DisplayProgressBar("Project U 광물", "건축물 외형 카드", 0.85f);
            BuildableVisualProfileBuilder.BuildAll(); // 용광로 외형 카드 · 콘텐츠 ID
            report.AppendLine("용광로 외형 카드 연결");

            EditorUtility.DisplayProgressBar("Project U 광물", "이웃 선물 취향", 0.9f);
            NpcContentBuilder.BuildAll(); // 보석 · 귀금속 선물 취향을 NPC 자료에 넣는다
            NpcQuestBuilder.BuildAll(); // 드라비아 광물 의뢰 3개를 다시 만든다
            report.AppendLine("이웃 선물 취향 갱신 · 대장간 주문 : 드라비아 게시판 의뢰 3개 (석탄 · 구리 광석 · 강철 주괴)");
            GameDataRegistryEditor.CreateOrRefreshDefaultRegistry();
            string visual = ItemVisualContentBuilder.BuildAll();
            report.AppendLine(visual.Split('\n').FirstOrDefault(line => line.StartsWith("✗")) ?? "아이템 외형 · 아이콘 · 바닥 Prefab 생성");

            if (EditorSceneManager.GetActiveScene().path == ScenePath && saveScene)
            {
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                AssetDatabase.SaveAssets();
                report.AppendLine("게임 Scene 저장 완료");
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
        }

        report.Append(Validate(out _));
        return report.ToString();
    }

    private static void EnsureFolders()
    {
        foreach (string folder in new[] { ItemFolder, CookingFolder, CraftingFolder, BuildingFolder, PrefabFolder })
        {
            StylizedArtAssetFactory.EnsureFolder(folder);
        }
    }

    public static string ModelId(ItemSpec spec) => string.IsNullOrEmpty(spec.Model) ? spec.Id : spec.Model; // 아이템의 저폴리 모델 ID

    private static int EnsureModels()
    {
        List<string> ids = Items.Select(ModelId).ToList();
        ids.AddRange(Veins.Select(spec => spec.Model));
        ids.AddRange(new[] { "build_furnace", "meteor_rock", "meteor_crater" });
        ids = ids.Distinct().ToList();
        return ids.Count(id => StylizedArtAssetFactory.GetOrCreateModelPrefab(id, true) != null);
    }

    private static ItemData CreateItem(ItemSpec spec)
    {
        ItemData item = LoadOrCreate<ItemData>($"{ItemFolder}/{spec.Asset}.asset");
        SerializedObject serialized = new SerializedObject(item);
        serialized.FindProperty("itemId").stringValue = spec.Id;
        serialized.FindProperty("displayName").stringValue = spec.English;
        serialized.FindProperty("koreanName").stringValue = spec.Korean;
        serialized.FindProperty("description").stringValue = spec.Description;
        serialized.FindProperty("itemCategory").intValue = (int)spec.Category;
        serialized.FindProperty("maximumStack").intValue = spec.Stack;
        serialized.FindProperty("toolType").intValue = (int)spec.Tool;
        serialized.FindProperty("toolTier").intValue = spec.Tier;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(item);
        return item;
    }

    private static string SetIronTiers() // 기존 돌 도구 0등급 · 철 도끼 2등급
    {
        int changed = 0;

        foreach ((string path, int tier) in new[] { (IronAxePath, 2) })
        {
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);

            if (item == null)
            {
                continue;
            }

            SerializedObject serialized = new SerializedObject(item);
            SerializedProperty property = serialized.FindProperty("toolTier");

            if (property.intValue == tier)
            {
                continue;
            }

            property.intValue = tier;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(item);
            changed++;
        }

        return $"기존 도구 등급 맞춤 {changed}개 (철 도끼 2등급)";
    }

    private static CookingRecipeData CreateSmelt(SmeltSpec spec)
    {
        ItemData result = FindItem(spec.ResultId);

        if (result == null)
        {
            return null;
        }

        CookingRecipeData recipe = LoadOrCreate<CookingRecipeData>($"{CookingFolder}/{spec.Asset}.asset");
        SerializedObject serialized = new SerializedObject(recipe);
        serialized.FindProperty("recipeId").stringValue = spec.Id;
        serialized.FindProperty("displayName").stringValue = spec.English;
        serialized.FindProperty("requiredStation").intValue = (int)CookingStationTier.Furnace;
        serialized.FindProperty("resultItem").objectReferenceValue = result;
        serialized.FindProperty("resultQuantity").intValue = spec.ResultAmount;
        serialized.FindProperty("cookingSeconds").floatValue = spec.Seconds;
        FillIngredients(serialized.FindProperty("ingredients"), spec.Ingredients);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(recipe);
        return recipe;
    }

    private static CraftingRecipeData CreateCraft(CraftSpec spec)
    {
        ItemData result = FindItem(spec.ResultId);

        if (result == null)
        {
            return null;
        }

        CraftingRecipeData recipe = LoadOrCreate<CraftingRecipeData>($"{CraftingFolder}/{spec.Asset}.asset");
        SerializedObject serialized = new SerializedObject(recipe);
        serialized.FindProperty("recipeId").stringValue = spec.Id;
        serialized.FindProperty("displayName").stringValue = spec.English;
        serialized.FindProperty("requiredFacility").intValue = (int)spec.Facility;
        serialized.FindProperty("unlockType").intValue = (int)CraftingUnlockType.Default;
        serialized.FindProperty("unlockId").stringValue = string.Empty;
        serialized.FindProperty("resultItem").objectReferenceValue = result;
        serialized.FindProperty("resultQuantity").intValue = spec.ResultAmount;
        FillIngredients(serialized.FindProperty("ingredients"), spec.Ingredients);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(recipe);
        return recipe;
    }

    private static void FillIngredients(SerializedProperty property, (string id, int amount)[] ingredients)
    {
        property.arraySize = ingredients.Length;

        for (int index = 0; index < ingredients.Length; index++)
        {
            SerializedProperty entry = property.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("itemData").objectReferenceValue = FindItem(ingredients[index].id);
            entry.FindPropertyRelative("amount").intValue = ingredients[index].amount;
        }
    }

    public static ItemData FindItem(string itemId) // 새 아이템 · 기존 아이템 모두에서 찾기
    {
        foreach (string guid in AssetDatabase.FindAssets("t:ItemData", new[] { "Assets/_ProjectU" }))
        {
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(guid));

            if (item != null && item.ItemId == itemId)
            {
                return item;
            }
        }

        return null;
    }

    // ---------------------------------------------------------------- 광맥 Prefab (동굴에 놓는 채집물)

    public sealed class VeinSpec
    {
        public string Id; // 광맥 ID
        public string ItemId; // 나오는 아이템
        public string Model; // 모델 ID
        public string Korean; // 안내 문구에 쓰는 이름
        public int Tier; // 필요한 곡괭이 등급
        public int Quantity; // 한 광맥에서 캐는 수
        public float Respawn; // 다시 생기는 시간 (초)
        public int Layer; // 1 = 입구층 · 2 = 깊은층
        public float Weight; // 그 층에서 뽑히는 비율
    }

    public static readonly VeinSpec[] Veins =
    {
        new VeinSpec { Id = "coal", ItemId = "resource_coal", Model = "cave_vein_coal", Korean = "석탄", Tier = 0, Quantity = 4, Respawn = 25f, Layer = 1, Weight = 0.34f },
        new VeinSpec { Id = "copper", ItemId = "resource_copper_ore", Model = "cave_vein_copper", Korean = "구리 광석", Tier = 0, Quantity = 4, Respawn = 25f, Layer = 1, Weight = 0.34f },
        new VeinSpec { Id = "iron", ItemId = "item_iron_ore", Model = "cave_vein_iron", Korean = "철광석", Tier = 0, Quantity = 3, Respawn = 30f, Layer = 1, Weight = 0.32f },
        new VeinSpec { Id = "silver", ItemId = "resource_silver_ore", Model = "cave_vein_silver", Korean = "은 광석", Tier = 2, Quantity = 3, Respawn = 35f, Layer = 2, Weight = 0.26f },
        new VeinSpec { Id = "crystal", ItemId = "resource_crystal", Model = "cave_vein_crystal", Korean = "수정", Tier = 2, Quantity = 2, Respawn = 40f, Layer = 2, Weight = 0.22f },
        new VeinSpec { Id = "gold", ItemId = "resource_gold_ore", Model = "cave_vein_gold", Korean = "금 광석", Tier = 2, Quantity = 2, Respawn = 45f, Layer = 2, Weight = 0.16f },
        new VeinSpec { Id = "amethyst", ItemId = "resource_gem_amethyst", Model = "cave_vein_gem", Korean = "자수정", Tier = 2, Quantity = 2, Respawn = 60f, Layer = 2, Weight = 0.09f },
        new VeinSpec { Id = "ruby", ItemId = "resource_gem_ruby", Model = "cave_vein_ruby", Korean = "루비", Tier = 2, Quantity = 1, Respawn = 50f, Layer = 2, Weight = 0.07f },
        new VeinSpec { Id = "sapphire", ItemId = "resource_gem_sapphire", Model = "cave_vein_sapphire", Korean = "사파이어", Tier = 2, Quantity = 1, Respawn = 50f, Layer = 2, Weight = 0.07f },
        new VeinSpec { Id = "emerald", ItemId = "resource_gem_emerald", Model = "cave_vein_emerald", Korean = "에메랄드", Tier = 2, Quantity = 1, Respawn = 60f, Layer = 2, Weight = 0.06f },
        new VeinSpec { Id = "diamond", ItemId = "resource_gem_diamond", Model = "cave_vein_diamond", Korean = "다이아몬드", Tier = 3, Quantity = 1, Respawn = 120f, Layer = 2, Weight = 0.07f }
    };

    public const string VeinPrefabFolder = "Assets/_ProjectU/Prefabs/Gathering/Day117";
    private const string StoneResourcePath = "Assets/_ProjectU/Prefabs/Gathering/StoneResource_01.prefab";

    public static string VeinPrefabPath(VeinSpec spec) => $"{VeinPrefabFolder}/OreVein_{spec.Id}.prefab";

    public static GameObject LoadVeinPrefab(VeinSpec spec) => AssetDatabase.LoadAssetAtPath<GameObject>(VeinPrefabPath(spec));

    private static string BuildVeinPrefabs()
    {
        StylizedArtAssetFactory.EnsureFolder(VeinPrefabFolder);
        int made = 0;

        foreach (VeinSpec spec in Veins)
        {
            string path = VeinPrefabPath(spec);
            ItemData item = FindItem(spec.ItemId);
            GameObject model = StylizedArtAssetFactory.GetOrCreateModelPrefab(spec.Model, true);

            if (item == null || model == null)
            {
                continue;
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null && !AssetDatabase.CopyAsset(StoneResourcePath, path))
            {
                continue;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(path);

            try
            {
                root.name = $"OreVein_{spec.Id}";
                GatherableResource resource = root.GetComponent<GatherableResource>();
                SerializedObject serialized = new SerializedObject(resource);
                serialized.FindProperty("resourceItem").objectReferenceValue = item;
                serialized.FindProperty("promptMessage").stringValue = $"LMB - {spec.Korean} 캐기";
                serialized.FindProperty("totalQuantity").intValue = spec.Quantity;
                serialized.FindProperty("quantityPerInteraction").intValue = 1;
                serialized.FindProperty("requiredToolType").intValue = (int)ToolType.Pickaxe;
                serialized.FindProperty("requiredToolTier").intValue = spec.Tier;
                serialized.FindProperty("respawnEnabled").boolValue = true;
                serialized.FindProperty("respawnDelay").floatValue = spec.Respawn;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                ReplaceVisual(root, model, 0.85f);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                made++;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        return $"광맥 Prefab {made}개 (석탄 · 구리 · 철 · 은 · 수정 · 금 · 보석 5종)";
    }

    private static void ReplaceVisual(GameObject root, GameObject model, float scale) // 채집물 외형을 고정 모델로 바꾼다
    {
        ContentVisualRoot visualRoot = root.GetComponentInChildren<ContentVisualRoot>(true); // 자동 외형 교체를 끈다

        if (visualRoot != null)
        {
            UnityEngine.Object.DestroyImmediate(visualRoot, true);
        }

        Transform visual = root.transform.Find("VisualRoot");

        if (visual == null)
        {
            return;
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

    // ---------------------------------------------------------------- 밤의 운석 (구덩이 · 운석 덩어리 · Scene 관리자)

    public const string MeteorRockPath = VeinPrefabFolder + "/MeteorRock.prefab"; // 운석 덩어리 채집물
    public const string MeteorCraterPath = VeinPrefabFolder + "/MeteorCrater.prefab"; // 운석 구덩이 바닥
    public const string MeteorManagerName = "MeteorEventManager"; // Scene 관리자 이름
    private const string MeteorShardItemId = "resource_meteorite_shard"; // 운석 조각 아이템
    private const int MeteorRockTier = 2; // 운석 덩어리는 철 곡괭이 이상

    private static string BuildMeteorContent(bool saveScene)
    {
        StylizedArtAssetFactory.EnsureFolder(VeinPrefabFolder);
        List<string> lines = new List<string>();
        GameObject rock = BuildMeteorRockPrefab();
        GameObject crater = BuildMeteorCraterPrefab();
        lines.Add(rock != null && crater != null
            ? "운석 덩어리 · 구덩이 Prefab 준비"
            : "✗ 운석 Prefab을 만들지 못했습니다.");
        lines.Add(ConnectMeteorManager(rock, crater, saveScene));
        return string.Join("\n", lines);
    }

    private static GameObject BuildMeteorRockPrefab() // 운석 덩어리 (철 곡괭이로 캐는 자원)
    {
        ItemData shard = FindItem(MeteorShardItemId);
        GameObject model = StylizedArtAssetFactory.GetOrCreateModelPrefab("meteor_rock", true);

        if (shard == null || model == null)
        {
            return null;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(MeteorRockPath) == null && !AssetDatabase.CopyAsset(StoneResourcePath, MeteorRockPath))
        {
            return null;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(MeteorRockPath);

        try
        {
            root.name = "MeteorRock";
            GatherableResource resource = root.GetComponent<GatherableResource>();
            SerializedObject serialized = new SerializedObject(resource);
            serialized.FindProperty("resourceItem").objectReferenceValue = shard;
            serialized.FindProperty("promptMessage").stringValue = "LMB - 운석 조각 캐기";
            serialized.FindProperty("totalQuantity").intValue = 3;
            serialized.FindProperty("quantityPerInteraction").intValue = 1;
            serialized.FindProperty("requiredToolType").intValue = (int)ToolType.Pickaxe;
            serialized.FindProperty("requiredToolTier").intValue = MeteorRockTier;
            serialized.FindProperty("respawnEnabled").boolValue = false; // 구덩이 운석은 다시 생기지 않는다
            serialized.ApplyModifiedPropertiesWithoutUndo();
            ReplaceVisual(root, model, 0.9f);
            PrefabUtility.SaveAsPrefabAsset(root, MeteorRockPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        return AssetDatabase.LoadAssetAtPath<GameObject>(MeteorRockPath);
    }

    private static GameObject BuildMeteorCraterPrefab() // 구덩이 바닥 자국
    {
        GameObject model = StylizedArtAssetFactory.GetOrCreateModelPrefab("meteor_crater", true);

        if (model == null)
        {
            return null;
        }

        GameObject root = new GameObject("MeteorCrater");

        try
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            PrefabUtility.SaveAsPrefabAsset(root, MeteorCraterPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }

        return AssetDatabase.LoadAssetAtPath<GameObject>(MeteorCraterPath);
    }

    private static string ConnectMeteorManager(GameObject rock, GameObject crater, bool saveScene) // Scene에 운석 관리자 두기
    {
        Scene scene = EditorSceneManager.GetActiveScene();

        if (scene.path != ScenePath)
        {
            return "게임 Scene이 열려 있지 않아 운석 관리자는 연결하지 않았습니다.";
        }

        MeteorEventManager manager = UnityEngine.Object.FindFirstObjectByType<MeteorEventManager>(FindObjectsInactive.Include);

        if (manager == null)
        {
            GameObject holder = new GameObject(MeteorManagerName);
            SceneManager.MoveGameObjectToScene(holder, scene);
            manager = holder.AddComponent<MeteorEventManager>();
        }

        manager.EditorAssign(crater, rock);
        EditorUtility.SetDirty(manager);
        EditorSceneManager.MarkSceneDirty(scene);

        if (saveScene)
        {
            EditorSceneManager.SaveScene(scene);
        }

        return "Scene 운석 관리자 연결 완료 (밤 22시 30분 · 확률 20% · 구덩이 최대 2곳)";
    }

    // ---------------------------------------------------------------- 용광로

    private static string BuildFurnace(List<CookingRecipeData> smelts, Dictionary<string, ItemData> items)
    {
        if (!CopyPrefab(StoneCampfirePlacedPath, FurnacePlacedPath) || !CopyPrefab(StoneCampfirePreviewPath, FurnacePreviewPath))
        {
            return "✗ 용광로 Prefab을 만들지 못했습니다.";
        }

        GameObject placed = PrefabUtility.LoadPrefabContents(FurnacePlacedPath);

        try
        {
            placed.name = "FurnacePlaced";
            CampfireCookingStation station = placed.GetComponentInChildren<CampfireCookingStation>(true);

            if (station == null)
            {
                return "✗ 용광로에 조리대 부품이 없습니다.";
            }

            SerializedObject serialized = new SerializedObject(station);
            serialized.FindProperty("stationTier").intValue = (int)CookingStationTier.Furnace;
            serialized.FindProperty("slotCount").intValue = 2;
            serialized.FindProperty("maxBatchQuantity").intValue = 5;
            serialized.FindProperty("fuelItem").objectReferenceValue = items["resource_coal"];
            serialized.FindProperty("fuelAmount").intValue = 1;
            serialized.FindProperty("promptMessage").stringValue = "F - SMELT";
            serialized.FindProperty("slots").arraySize = 0;
            SerializedProperty list = serialized.FindProperty("recipes");
            list.arraySize = smelts.Count;

            for (int index = 0; index < smelts.Count; index++)
            {
                list.GetArrayElementAtIndex(index).objectReferenceValue = smelts[index];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(placed, FurnacePlacedPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(placed);
        }

        BuildRecipeData recipe = LoadOrCreate<BuildRecipeData>(FurnaceRecipePath);
        SerializedObject recipeObject = new SerializedObject(recipe);
        recipeObject.FindProperty("recipeId").stringValue = "structure_furnace";
        recipeObject.FindProperty("displayName").stringValue = "FURNACE";
        recipeObject.FindProperty("structureType").intValue = 4; // 기능성 가구
        recipeObject.FindProperty("allowGroundPlacement").boolValue = true;
        recipeObject.FindProperty("placementType").intValue = 2;
        recipeObject.FindProperty("rotationStep").floatValue = 45f;
        recipeObject.FindProperty("placedPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(FurnacePlacedPath);
        recipeObject.FindProperty("previewPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(FurnacePreviewPath);
        recipeObject.FindProperty("placementCheckCenter").vector3Value = new Vector3(0f, 0.5f, 0f);
        recipeObject.FindProperty("placementCheckHalfExtents").vector3Value = new Vector3(0.75f, 0.5f, 0.65f);
        recipeObject.FindProperty("maximumSlopeAngle").floatValue = 20f;
        recipeObject.FindProperty("maximumHeightDifference").floatValue = 0.15f;
        recipeObject.FindProperty("demolitionRefundRatio").floatValue = 0.5f;
        SerializedProperty ingredients = recipeObject.FindProperty("ingredients");
        ingredients.arraySize = 2;
        ingredients.GetArrayElementAtIndex(0).FindPropertyRelative("itemData").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ItemData>(StonePath);
        ingredients.GetArrayElementAtIndex(0).FindPropertyRelative("amount").intValue = 20;
        ingredients.GetArrayElementAtIndex(1).FindPropertyRelative("itemData").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ItemData>(IronOrePath);
        ingredients.GetArrayElementAtIndex(1).FindPropertyRelative("amount").intValue = 3;
        recipeObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(recipe);
        AssetDatabase.SaveAssets();
        return $"용광로 건축물 (돌 20 + 철광석 3) · 제련 칸 2개 · 연료 석탄 · {ConnectBuildMenu(recipe)}";
    }

    private static bool CopyPrefab(string source, string target)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(target) != null)
        {
            return true;
        }

        return AssetDatabase.LoadAssetAtPath<GameObject>(source) != null && AssetDatabase.CopyAsset(source, target);
    }

    private static string ConnectBuildMenu(BuildRecipeData recipe) // 건축 목록에 용광로 넣기
    {
        if (EditorSceneManager.GetActiveScene().path != ScenePath)
        {
            return "건축 목록 연결은 게임 Scene에서";
        }

        BuildPlacementController controller = UnityEngine.Object.FindFirstObjectByType<BuildPlacementController>(FindObjectsInactive.Include);

        if (controller == null)
        {
            return "✗ 건축 관리자가 없습니다.";
        }

        SerializedObject serialized = new SerializedObject(controller);
        SerializedProperty list = serialized.FindProperty("buildRecipes");

        for (int index = 0; index < list.arraySize; index++)
        {
            if (list.GetArrayElementAtIndex(index).objectReferenceValue == recipe)
            {
                return "건축 목록에 이미 있음";
            }
        }

        list.arraySize++;
        list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = recipe;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        return "건축 목록에 추가" + ConnectStructureSave(recipe);
    }

    private static string ConnectStructureSave(BuildRecipeData recipe) // 건축물 저장 목록에 용광로 넣기
    {
        PlacedStructureSaveBridge bridge = UnityEngine.Object.FindFirstObjectByType<PlacedStructureSaveBridge>(FindObjectsInactive.Include);

        if (bridge == null)
        {
            return " · ✗ 건축물 저장 관리자가 없습니다.";
        }

        SerializedObject serialized = new SerializedObject(bridge);
        SerializedProperty list = serialized.FindProperty("buildRecipes");

        for (int index = 0; index < list.arraySize; index++)
        {
            if (list.GetArrayElementAtIndex(index).objectReferenceValue == recipe)
            {
                return " · 저장 목록에 이미 있음";
            }
        }

        list.arraySize++;
        list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = recipe;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(bridge);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        return " · 저장 목록에 추가";
    }

    // ---------------------------------------------------------------- 한글 이름 · 선물 · 등록

    private static string UpdateKoreanNames()
    {
        if (!File.Exists(KoreanNamesPath))
        {
            return "✗ 아이템 한글 이름 목록이 없습니다.";
        }

        List<string> lines = File.ReadAllLines(KoreanNamesPath, Encoding.UTF8).ToList();
        HashSet<string> existing = new HashSet<string>(lines.Skip(1).Select(line => line.Split(',')[0].Trim()), StringComparer.Ordinal);
        int added = 0;

        foreach (ItemSpec spec in Items.Where(spec => !existing.Contains(spec.Id)))
        {
            lines.Add($"{spec.Id},{spec.Korean}");
            added++;
        }

        if (added > 0)
        {
            File.WriteAllLines(KoreanNamesPath, lines, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(KoreanNamesPath);
        }

        return $"아이템 한글 이름 {added}개 추가";
    }

    private static string UpdateGiftTags()
    {
        if (!File.Exists(GiftTagsPath) || !File.Exists(GiftsPath))
        {
            return "✗ 선물 목록이 없습니다.";
        }

        List<string> tagLines = File.ReadAllLines(GiftTagsPath, Encoding.UTF8).ToList();
        HashSet<string> tagPairs = new HashSet<string>(tagLines.Skip(1).Select(line => string.Join(",", line.Split(',').Take(2))), StringComparer.Ordinal);
        int tags = 0;

        foreach ((string tag, string itemId, string note) in new[]
        {
            (GemTag, "resource_gem_ruby", "보석"), (GemTag, "resource_gem_sapphire", "보석"), (GemTag, "resource_gem_emerald", "보석"), (GemTag, "resource_gem_amethyst", "보석"), (GemTag, "resource_gem_diamond", "보석"),
            (TreasureTag, "resource_ingot_gold", "귀금속"), (TreasureTag, "resource_ingot_silver", "귀금속"), (TreasureTag, "resource_gold_ore", "귀금속")
        })
        {
            if (tagPairs.Add($"{tag},{itemId}"))
            {
                tagLines.Add($"{tag},{itemId},{note}");
                tags++;
            }
        }

        if (tags > 0)
        {
            File.WriteAllLines(GiftTagsPath, tagLines, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(GiftTagsPath);
        }

        List<string> giftLines = File.ReadAllLines(GiftsPath, Encoding.UTF8).ToList();
        HashSet<string> giftPairs = new HashSet<string>(giftLines.Skip(1).Select(line => string.Join(",", line.Split(',').Take(3))), StringComparer.Ordinal);
        int gifts = 0;

        foreach ((string character, string preference, string tag) in GiftPreferences)
        {
            if (giftPairs.Add($"{character},{preference},tag:{tag}"))
            {
                giftLines.Add($"{character},{preference},tag:{tag},{(tag == GemTag ? "보석" : "귀금속")}");
                gifts++;
            }
        }

        if (gifts > 0)
        {
            File.WriteAllLines(GiftsPath, giftLines, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(GiftsPath);
        }

        return $"선물 태그 {tags}개 · 이웃 취향 {gifts}개 추가 (보석 · 귀금속)";
    }

    private static string RegisterItems(Dictionary<string, ItemData> items)
    {
        ItemDatabase database = AssetDatabase.LoadAssetAtPath<ItemDatabase>(ItemDatabasePath);

        if (database == null)
        {
            return "✗ ItemDatabase가 없습니다.";
        }

        SerializedObject serialized = new SerializedObject(database);
        SerializedProperty list = serialized.FindProperty("items");
        HashSet<UnityEngine.Object> present = new HashSet<UnityEngine.Object>();

        for (int index = 0; index < list.arraySize; index++)
        {
            present.Add(list.GetArrayElementAtIndex(index).objectReferenceValue);
        }

        int added = 0;

        foreach (ItemData item in items.Values.Where(item => !present.Contains(item)))
        {
            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = item;
            added++;
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(database);
        return $"ItemDatabase 등록 {added}개";
    }

    private static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);

        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
        }

        return asset;
    }

    // ---------------------------------------------------------------- 검증

    public static string Validate(out int errorCount)
    {
        StringBuilder report = new StringBuilder("[광물 · 제련 검증]\n");
        int errors = 0;

        void Error(string message)
        {
            errors++;
            report.AppendLine("✗ " + message);
        }

        foreach (ItemSpec spec in Items)
        {
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>($"{ItemFolder}/{spec.Asset}.asset");

            if (item == null || item.ItemId != spec.Id)
            {
                Error($"{spec.Id} 아이템이 없습니다. 콘텐츠 자동 적용을 기다리세요.");
                continue;
            }

            if (item.KoreanName != spec.Korean)
            {
                Error($"{spec.Id} 한글 이름이 '{item.KoreanName}'입니다 ('{spec.Korean}' 필요).");
            }

            if (spec.Category == ItemCategory.Tool && item.ToolTier != spec.Tier)
            {
                Error($"{spec.Id} 도구 등급이 {item.ToolTier}입니다 ({spec.Tier} 필요).");
            }
        }

        foreach (SmeltSpec spec in Smelts)
        {
            CookingRecipeData recipe = AssetDatabase.LoadAssetAtPath<CookingRecipeData>($"{CookingFolder}/{spec.Asset}.asset");

            if (recipe == null || recipe.RequiredStation != CookingStationTier.Furnace || recipe.ResultItem == null)
            {
                Error($"{spec.Id} 제련법이 없거나 용광로용이 아닙니다.");
            }
        }

        foreach (CraftSpec spec in Crafts)
        {
            CraftingRecipeData recipe = AssetDatabase.LoadAssetAtPath<CraftingRecipeData>($"{CraftingFolder}/{spec.Asset}.asset");

            if (recipe == null || recipe.ResultItem == null)
            {
                Error($"{spec.Id} 제작법이 없습니다.");
            }
        }

        foreach (VeinSpec spec in Veins)
        {
            GameObject prefab = LoadVeinPrefab(spec);
            GatherableResource resource = prefab != null ? prefab.GetComponent<GatherableResource>() : null;

            if (resource == null || resource.RequiredToolTier != spec.Tier)
            {
                Error($"{spec.Id} 광맥 Prefab이 없거나 필요한 곡괭이 등급이 다릅니다.");
            }
        }

        GameObject furnace = AssetDatabase.LoadAssetAtPath<GameObject>(FurnacePlacedPath);
        CampfireCookingStation station = furnace != null ? furnace.GetComponentInChildren<CampfireCookingStation>(true) : null;

        if (station == null || station.Tier != CookingStationTier.Furnace)
        {
            Error("용광로 Prefab이 없거나 제련 시설이 아닙니다.");
        }

        BuildRecipeData buildRecipe = AssetDatabase.LoadAssetAtPath<BuildRecipeData>(FurnaceRecipePath);

        if (buildRecipe == null || buildRecipe.PlacedPrefab == null)
        {
            Error("용광로 건축 데이터가 없습니다.");
        }

        GameObject meteorRock = AssetDatabase.LoadAssetAtPath<GameObject>(MeteorRockPath);
        GatherableResource meteorResource = meteorRock != null ? meteorRock.GetComponent<GatherableResource>() : null;

        if (meteorResource == null || meteorResource.RequiredToolTier != MeteorRockTier)
        {
            Error("운석 덩어리 Prefab이 없거나 필요한 곡괭이 등급이 다릅니다.");
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(MeteorCraterPath) == null)
        {
            Error("운석 구덩이 Prefab이 없습니다.");
        }

        if (EditorSceneManager.GetActiveScene().path == ScenePath)
        {
            MeteorEventManager manager = UnityEngine.Object.FindFirstObjectByType<MeteorEventManager>(FindObjectsInactive.Include);

            if (manager == null || !manager.HasPrefabs)
            {
                Error("Scene에 운석 관리자가 없거나 Prefab 연결이 비어 있습니다.");
            }
        }

        MarketCatalogData catalog = AssetDatabase.LoadAssetAtPath<MarketCatalogData>(MarketCatalogPath);

        foreach (ItemSpec spec in Items.Where(entry => entry.Category != ItemCategory.Tool))
        {
            ItemData item = FindItem(spec.Id);

            if (catalog == null || item == null || !catalog.TryGetPrice(item, out _))
            {
                Error($"{spec.Id} 판매 가격이 없습니다.");
            }
        }

        ItemData torch = AssetDatabase.LoadAssetAtPath<ItemData>(TorchPath);

        if (torch == null)
        {
            Error("횃불 아이템이 없습니다 (116일차).");
        }

        report.AppendLine($"광물 · 주괴 · 도구 {Items.Length}개 · 제련법 {Smelts.Length}개 · 제작법 {Crafts.Length}개 · 광맥 {Veins.Length}종");
        report.AppendLine($"결과 : 오류 {errors}개");
        errorCount = errors;
        return report.ToString();
    }
}
