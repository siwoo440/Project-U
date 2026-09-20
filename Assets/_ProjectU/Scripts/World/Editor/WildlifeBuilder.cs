using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 118일차: 야생동물과 사냥
// 1. 고기 · 가죽 · 뿔 · 곤충 등 사냥 물건과 요리 · 장비 · 도구 아이템
// 2. 야생동물 13종 (순한 6 · 사나운 4 · 작은 것 3)과 섬 구역별 영역 배치
// 3. 덫 · 무두질대 건축물과 무두질 · 요리 · 제작법
// 4. 사냥 전리품을 이웃 선물 · 게시판 의뢰로 이어 준다
// 자동 적용(ProjectUAutoContent)이 실행한다. 여러 번 실행해도 같은 결과가 나온다.
public static partial class WildlifeBuilder
{
    private const string ScenePath = "Assets/_ProjectU/Scenes/20_Gameplay.unity";
    private const string ItemFolder = "Assets/_ProjectU/Data/Items/Day118";
    private const string CookingFolder = "Assets/_ProjectU/Data/Cooking/Day118";
    private const string CraftingFolder = "Assets/_ProjectU/Data/Crafting/Day118";
    private const string BuildingFolder = "Assets/_ProjectU/Data/Building/Day118";
    private const string EnemyDataFolder = "Assets/_ProjectU/Data/Enemies/Day118";
    private const string AnimalPrefabFolder = "Assets/_ProjectU/Prefabs/Wildlife/Day118";
    private const string LootFolder = "Assets/_ProjectU/Prefabs/Wildlife/Day118/Loot";
    private const string CreatureFolder = "Assets/_ProjectU/Prefabs/Gathering/Day118";
    private const string BuildPrefabFolder = "Assets/_ProjectU/Prefabs/Building/Day118";
    private const string ItemDatabasePath = "Assets/_ProjectU/Data/Databases/ItemDatabase.asset";
    private const string KoreanNamesPath = "Assets/_ProjectU/Data/Items/ItemKoreanNames.csv";
    private const string GiftTagsPath = "Assets/_ProjectU/Data/Npc/Source/NpcGiftTags.csv";
    private const string GiftsPath = "Assets/_ProjectU/Data/Npc/Source/NpcGifts.csv";
    private const string BowItemPath = "Assets/_ProjectU/Data/Items/ItemData_Bow.asset";
    private const string PickupRegistryPath = "Assets/_ProjectU/Prefabs/Items/Day75/WorldItemPickupRegistry_Day75.asset";

    public const string RootName = "=== Wildlife ==="; // Scene 야생동물 묶음
    public const string RuntimeParentName = "WildlifeRuntime"; // 실행 중 생긴 동물이 들어가는 곳
    public const string FurTag = "fur"; // 털가죽 선물 태그
    public const string HuntTag = "hunt"; // 사냥 자랑거리 선물 태그
    public const string InsectTag = "insect"; // 곤충 선물 태그
    public const string HeartyTag = "hearty"; // 든든한 요리 (기존 태그)

    // ---------------------------------------------------------------- 아이템 표

    public sealed class ItemSpec
    {
        public string Id; // 아이템 ID
        public string Asset; // 저장 파일 이름
        public string English; // 표시 이름
        public string Korean; // 한글 이름
        public string Description; // 설명
        public ItemCategory Category = ItemCategory.CraftingMaterial; // 분류
        public int Stack = 20; // 최대 중첩
        public ToolType Tool = ToolType.None; // 도구 종류
        public EquipmentSlotType Slot = EquipmentSlotType.None; // 장비 칸
        public int SlotBonus; // 가방 칸 증가
        public float Defense; // 피해 감소 (%)
        public float Cold; // 추위 감소 (%)
        public float Hunger; // 허기 회복
        public float Thirst; // 갈증 회복
        public float Health; // 체력 회복
        public FoodBuffType Buff = FoodBuffType.None; // 보조 효과
        public float BuffStrength; // 보조 효과 세기
        public float BuffSeconds; // 보조 효과 시간
        public float Damage; // 공격력 (도구 · 무기)
        public bool Ranged; // 원거리 무기
    }

    // 사냥 전리품 · 요리 · 장비 · 도구 (모델 ID는 아이템 ID와 같다)
    public static readonly ItemSpec[] Items =
    {
        // 날고기 · 게살
        new ItemSpec { Id = "food_small_meat", Asset = "ItemData_SmallMeat", English = "SMALL GAME MEAT", Korean = "작은 고기", Description = "Raw meat from a small animal.", Category = ItemCategory.Food, Hunger = 8f, Health = -2f },
        new ItemSpec { Id = "food_red_meat", Asset = "ItemData_RedMeat", English = "RED MEAT", Korean = "붉은 고기", Description = "Raw meat from a deer or boar.", Category = ItemCategory.Food, Hunger = 12f, Health = -2f },
        new ItemSpec { Id = "food_bear_meat", Asset = "ItemData_BearMeat", English = "BEAR MEAT", Korean = "곰 고기", Description = "A heavy cut from a great bear.", Category = ItemCategory.Food, Hunger = 16f, Health = -2f },
        new ItemSpec { Id = "food_crab_meat", Asset = "ItemData_CrabMeat", English = "CRAB MEAT", Korean = "게살", Description = "Sweet meat from a shore crab.", Category = ItemCategory.Food, Hunger = 10f },

        // 가죽 · 털 · 부산물
        new ItemSpec { Id = "resource_small_hide", Asset = "ItemData_SmallHide", English = "SMALL HIDE", Korean = "작은 가죽", Description = "A small hide from a rabbit or fox." },
        new ItemSpec { Id = "resource_thick_hide", Asset = "ItemData_ThickHide", English = "THICK HIDE", Korean = "두꺼운 가죽", Description = "A thick hide from a deer, goat or boar." },
        new ItemSpec { Id = "resource_fine_pelt", Asset = "ItemData_FinePelt", English = "FINE PELT", Korean = "고급 털가죽", Description = "Soft fur from a fox, wolf or bear.", Stack = 10 },
        new ItemSpec { Id = "resource_wool", Asset = "ItemData_Wool", English = "THICK WOOL", Korean = "두꺼운 털", Description = "Warm wool from a mountain goat." },
        new ItemSpec { Id = "resource_scale", Asset = "ItemData_LizardScale", English = "LIZARD SCALE", Korean = "도마뱀 비늘", Description = "Hard scales from a desert lizard." },
        new ItemSpec { Id = "resource_feather", Asset = "ItemData_Feather", English = "FEATHER", Korean = "깃털", Description = "A long feather from a marsh bird." },
        new ItemSpec { Id = "resource_antler", Asset = "ItemData_Antler", English = "ANTLER", Korean = "뿔", Description = "A branched antler. Neighbours like it.", Stack = 10 },
        new ItemSpec { Id = "resource_beast_fang", Asset = "ItemData_BeastFang", English = "BEAST FANG", Korean = "짐승 이빨", Description = "A fang or tusk from a fierce animal.", Stack = 10 },
        new ItemSpec { Id = "resource_stinger", Asset = "ItemData_Stinger", English = "SCORPION STINGER", Korean = "독침", Description = "A venom sac used in medicine.", Stack = 10 },
        new ItemSpec { Id = "resource_crab_shell", Asset = "ItemData_CrabShell", English = "CRAB SHELL", Korean = "게 껍데기", Description = "A hard shell from a shore crab." },
        new ItemSpec { Id = "resource_tanned_leather", Asset = "ItemData_TannedLeather", English = "TANNED LEATHER", Korean = "무두질 가죽", Description = "Leather made ready on a tanning rack." },
        new ItemSpec { Id = "resource_butterfly", Asset = "ItemData_Butterfly", English = "BUTTERFLY", Korean = "나비", Description = "A butterfly in a jar.", Stack = 10 },
        new ItemSpec { Id = "resource_firefly", Asset = "ItemData_Firefly", English = "FIREFLY", Korean = "반딧불이", Description = "A firefly that glows at night.", Stack = 10 },

        // 요리
        new ItemSpec { Id = "food_roast_meat", Asset = "ItemData_RoastMeat", English = "ROAST MEAT", Korean = "구운 고기", Description = "Meat roasted on a stick.", Category = ItemCategory.Food, Stack = 10, Hunger = 30f, Health = 6f },
        new ItemSpec { Id = "food_meat_stew", Asset = "ItemData_MeatStew", English = "MEAT STEW", Korean = "고기 스튜", Description = "A warm stew that keeps you going.", Category = ItemCategory.Food, Stack = 10, Hunger = 45f, Thirst = 15f, Health = 10f, Buff = FoodBuffType.StaminaRecovery, BuffStrength = 25f, BuffSeconds = 240f },
        new ItemSpec { Id = "food_venison_steak", Asset = "ItemData_VenisonSteak", English = "VENISON STEAK", Korean = "사슴 스테이크", Description = "A grilled steak. Light on the feet.", Category = ItemCategory.Food, Stack = 10, Hunger = 40f, Health = 14f, Buff = FoodBuffType.MoveSpeed, BuffStrength = 12f, BuffSeconds = 180f },
        new ItemSpec { Id = "food_crab_soup", Asset = "ItemData_CrabSoup", English = "CRAB SOUP", Korean = "게살 수프", Description = "A hot soup that keeps the cold away.", Category = ItemCategory.Food, Stack = 10, Hunger = 35f, Thirst = 22f, Buff = FoodBuffType.Warmth, BuffStrength = 30f, BuffSeconds = 300f },

        // 장비 · 도구
        new ItemSpec { Id = "equipment_leather_backpack", Asset = "ItemData_LeatherBackpack", English = "LEATHER BACKPACK", Korean = "가죽 가방", Description = "A big backpack. Six more slots.", Category = ItemCategory.Equipment, Stack = 1, Slot = EquipmentSlotType.Backpack, SlotBonus = 6 },
        new ItemSpec { Id = "equipment_leather_hat", Asset = "ItemData_LeatherHat", English = "LEATHER HAT", Korean = "가죽 모자", Description = "A sturdy hat for the hunt.", Category = ItemCategory.Equipment, Stack = 1, Slot = EquipmentSlotType.Head, Defense = 10f, Cold = 10f },
        new ItemSpec { Id = "equipment_winter_coat", Asset = "ItemData_WinterCoat", English = "WINTER COAT", Korean = "겨울 외투", Description = "A fur coat for snow and night.", Category = ItemCategory.Equipment, Stack = 1, Slot = EquipmentSlotType.Body, Defense = 12f, Cold = 35f },
        new ItemSpec { Id = "tool_net", Asset = "ItemData_CatchNet", English = "CATCH NET", Korean = "채집망", Description = "Catches butterflies, fireflies and crabs.", Category = ItemCategory.Tool, Stack = 1, Tool = ToolType.Net, Damage = 2f },
        new ItemSpec { Id = "weapon_fine_bow", Asset = "ItemData_FineBow", English = "FINE BOW", Korean = "좋은 활", Description = "A leather-wrapped bow with steel tips.", Category = ItemCategory.Weapon, Stack = 1, Damage = 24f, Ranged = true }
    };

    // ---------------------------------------------------------------- 요리 · 무두질 · 제작 표

    private sealed class CookSpec
    {
        public string Id;
        public string Asset;
        public string English;
        public string ResultId;
        public int ResultAmount = 1;
        public float Seconds;
        public CookingStationTier Station = CookingStationTier.Campfire;
        public (string id, int amount)[] Ingredients;
    }

    private static readonly CookSpec[] Cooks =
    {
        new CookSpec { Id = "cook_roast_meat", Asset = "CookingRecipe_RoastMeat", English = "ROAST MEAT", ResultId = "food_roast_meat", Seconds = 16f, Ingredients = new[] { ("food_small_meat", 1) } },
        new CookSpec { Id = "cook_venison_steak", Asset = "CookingRecipe_VenisonSteak", English = "VENISON STEAK", ResultId = "food_venison_steak", Seconds = 24f, Ingredients = new[] { ("food_red_meat", 1), ("item_herb_bundle", 0) } },
        new CookSpec { Id = "cook_meat_stew", Asset = "CookingRecipe_MeatStew", English = "MEAT STEW", ResultId = "food_meat_stew", Seconds = 40f, Station = CookingStationTier.StoneCampfire, Ingredients = new[] { ("food_red_meat", 2), ("food_potato", 1), ("drink_water_bottle", 1) } },
        new CookSpec { Id = "cook_crab_soup", Asset = "CookingRecipe_CrabSoup", English = "CRAB SOUP", ResultId = "food_crab_soup", Seconds = 30f, Station = CookingStationTier.StoneCampfire, Ingredients = new[] { ("food_crab_meat", 2), ("drink_water_bottle", 1) } },
        new CookSpec { Id = "tan_thick_hide", Asset = "CookingRecipe_TanThickHide", English = "TANNED LEATHER", ResultId = "resource_tanned_leather", ResultAmount = 2, Seconds = 45f, Station = CookingStationTier.TanningRack, Ingredients = new[] { ("resource_thick_hide", 2) } },
        new CookSpec { Id = "tan_small_hide", Asset = "CookingRecipe_TanSmallHide", English = "TANNED LEATHER", ResultId = "resource_tanned_leather", Seconds = 38f, Station = CookingStationTier.TanningRack, Ingredients = new[] { ("resource_small_hide", 4) } }
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

    private static readonly CraftSpec[] Crafts =
    {
        new CraftSpec { Id = "recipe_catch_net", Asset = "CraftingRecipe_CatchNet", English = "CATCH NET", ResultId = "tool_net", Facility = CraftingFacilityType.Hand, Ingredients = new[] { ("item_wood", 2), ("item_plant_fiber", 3) } },
        new CraftSpec { Id = "recipe_leather_hat", Asset = "CraftingRecipe_LeatherHat", English = "LEATHER HAT", ResultId = "equipment_leather_hat", Ingredients = new[] { ("resource_tanned_leather", 3), ("item_plant_fiber", 2) } },
        new CraftSpec { Id = "recipe_leather_backpack", Asset = "CraftingRecipe_LeatherBackpack", English = "LEATHER BACKPACK", ResultId = "equipment_leather_backpack", Ingredients = new[] { ("resource_tanned_leather", 6), ("item_plant_fiber", 6), ("resource_ingot_copper", 1) } },
        new CraftSpec { Id = "recipe_winter_coat", Asset = "CraftingRecipe_WinterCoat", English = "WINTER COAT", ResultId = "equipment_winter_coat", Ingredients = new[] { ("resource_tanned_leather", 4), ("resource_wool", 4) } },
        new CraftSpec { Id = "recipe_fine_bow", Asset = "CraftingRecipe_FineBow", English = "FINE BOW", ResultId = "weapon_fine_bow", Ingredients = new[] { ("resource_tanned_leather", 3), ("item_wood", 4), ("resource_ingot_steel", 1) } }
    };

    // 이웃 선물 취향 (태그 · 아이템)
    private static readonly (string character, string preference, string target, string note)[] GiftRules =
    {
        ("char_ragh", "Loved", "tag:" + HuntTag, "사냥 자랑거리"),
        ("char_harka", "Liked", "tag:" + HuntTag, "사냥 자랑거리"),
        ("char_dravia", "Liked", "tag:" + FurTag, "가죽 손잡이"),
        ("char_lunette", "Liked", "resource_stinger", "약 재료"),
        ("char_beatrice", "Loved", "tag:" + InsectTag, "곤충"),
        ("char_arachne", "Liked", "tag:" + InsectTag, "곤충"),
        ("char_safira", "Liked", "resource_scale", "같은 사막 살이"),
        ("char_noctia", "Loved", "resource_firefly", "밤의 불빛"),
        ("char_seira", "Liked", "tag:" + FurTag, "따뜻한 털"),
        ("char_mireille", "Liked", "tag:" + FurTag, "따뜻한 털"),
        ("char_erina", "Liked", "tag:" + InsectTag, "숲의 친구")
    };

    // 사냥 게시판 의뢰
    private static readonly (string id, string owner, string title, string requirement, int coins, int affinity, int days, string request, string thanks)[] Quests =
    {
        ("quest_ragh_meat", "char_ragh", "사냥꾼의 고기", "food_red_meat:4", 75, 5, 3, "붉은 고기 네 덩이. 사슴이든 멧돼지든 상관없다.", "좋은 고기군. 오늘은 배부르게 먹겠어."),
        ("quest_harka_pelt", "char_harka", "겨울 준비 털가죽", "resource_fine_pelt:2", 95, 5, 4, "고급 털가죽 두 장이 필요해. 여우나 늑대 것이면 돼.", "고맙다. 이걸로 겨울을 넘길 수 있어."),
        ("quest_lunette_stinger", "char_lunette", "약에 쓸 독침", "resource_stinger:2", 62, 4, 4, "사막 전갈의 독침… 두 개만 구해 주실 수 있을까요? 해독제를 만들려고요.", "조심해서 다뤄야 해요. 정말 고마워요.")
    };

    // ---------------------------------------------------------------- 만들기 (1단계 : 아이템)

    // 111 · 113일차 의뢰(사냥 게시판)가 이 아이템을 쓰기 때문에 아이템을 먼저 만든다.
    public static string BuildItems()
    {
        StringBuilder report = new StringBuilder("[야생동물 아이템 만들기]\n");

        try
        {
            EnsureFolders();
            EditorUtility.DisplayProgressBar("Project U 야생동물", "모델", 0.1f);
            report.AppendLine($"야생동물 · 사냥 물건 모델 {EnsureModels()}종 준비");

            EditorUtility.DisplayProgressBar("Project U 야생동물", "아이템", 0.4f);
            Dictionary<string, ItemData> items = CreateItems();
            report.AppendLine($"사냥 물건 · 요리 · 장비 아이템 {items.Count}개");
            report.AppendLine(UpdateKoreanNames());
            report.AppendLine(RegisterItems(items));
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayProgressBar("Project U 야생동물", "판매 가격 · 외형", 0.7f);
            MarketContentBuilder.RefreshCatalog();
            GameDataRegistryEditor.CreateOrRefreshDefaultRegistry();
            string visual = ItemVisualContentBuilder.BuildAll();
            report.AppendLine(visual.Split('\n').FirstOrDefault(line => line.StartsWith("✗")) ?? "판매 가격 · 아이콘 · 바닥 Prefab 생성");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
        }

        return report.ToString();
    }

    // ---------------------------------------------------------------- 만들기 (2단계 : 동물 · 사냥 · 가공)

    public static string BuildAll(bool saveScene)
    {
        StringBuilder report = new StringBuilder("[야생동물 · 사냥 만들기]\n");

        try
        {
            EnsureFolders();
            EditorUtility.DisplayProgressBar("Project U 야생동물", "모델 · 아이템", 0.05f);
            EnsureModels();
            Dictionary<string, ItemData> items = CreateItems();
            report.AppendLine($"사냥 물건 · 요리 · 장비 아이템 {items.Count}개");
            report.AppendLine(UpdateKoreanNames());

            EditorUtility.DisplayProgressBar("Project U 야생동물", "요리 · 제작법", 0.2f);
            List<CookingRecipeData> cooks = Cooks.Select(CreateCook).Where(recipe => recipe != null).ToList();
            report.AppendLine($"요리 · 무두질법 {cooks.Count}개 (구운 고기 · 스튜 · 스테이크 · 게살 수프 · 무두질 2)");
            int crafts = Crafts.Count(spec => CreateCraft(spec) != null);
            report.AppendLine($"작업대 · 맨손 제작법 {crafts}개 (채집망 · 가죽 장비 3 · 좋은 활)");

            EditorUtility.DisplayProgressBar("Project U 야생동물", "동물 데이터 · Prefab", 0.4f);
            report.AppendLine(BuildAnimalAssets());

            EditorUtility.DisplayProgressBar("Project U 야생동물", "섬에 동물 놓기", 0.6f);
            report.AppendLine(PlaceWildlife());

            EditorUtility.DisplayProgressBar("Project U 야생동물", "덫 · 무두질대", 0.75f);
            report.AppendLine(BuildTrap(items));
            report.AppendLine(BuildTanningRack(cooks));

            EditorUtility.DisplayProgressBar("Project U 야생동물", "선물 · 의뢰", 0.85f);
            report.AppendLine(UpdateGiftTags());
            report.AppendLine(UpdateQuests());

            EditorUtility.DisplayProgressBar("Project U 야생동물", "아이템 등록 · 외형", 0.9f);
            report.AppendLine(RegisterItems(items));
            AssetDatabase.SaveAssets();
            MarketContentBuilder.RefreshCatalog();
            GameDataRegistryEditor.CreateOrRefreshDefaultRegistry();
            string visual = ItemVisualContentBuilder.BuildAll();
            report.AppendLine(visual.Split('\n').FirstOrDefault(line => line.StartsWith("✗")) ?? "아이템 외형 · 아이콘 · 바닥 Prefab 생성");
            BuildableVisualProfileBuilder.BuildAll();
            report.AppendLine("덫 · 무두질대 외형 카드 연결");
            NpcContentBuilder.BuildAll();
            NpcQuestBuilder.BuildAll();
            report.AppendLine("이웃 선물 취향 · 사냥 게시판 의뢰 갱신");

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
        foreach (string folder in new[] { ItemFolder, CookingFolder, CraftingFolder, BuildingFolder, EnemyDataFolder, AnimalPrefabFolder, LootFolder, CreatureFolder, BuildPrefabFolder })
        {
            StylizedArtAssetFactory.EnsureFolder(folder);
        }
    }

    private static int EnsureModels()
    {
        List<string> ids = Items.Select(spec => spec.Id).ToList();
        ids.AddRange(Animals.Select(spec => spec.Model));
        ids.AddRange(Creatures.Select(spec => spec.Model));
        ids.AddRange(new[] { "build_animal_trap", "build_tanning_rack" });
        return ids.Distinct().Count(id => StylizedArtAssetFactory.GetOrCreateModelPrefab(id, true) != null);
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
        serialized.FindProperty("equipmentSlotType").intValue = (int)spec.Slot;
        serialized.FindProperty("inventorySlotBonus").intValue = spec.SlotBonus;
        serialized.FindProperty("defensePercent").floatValue = spec.Defense;
        serialized.FindProperty("coldResistancePercent").floatValue = spec.Cold;
        serialized.FindProperty("hungerRestoreAmount").floatValue = spec.Hunger;
        serialized.FindProperty("foodThirstRestoreAmount").floatValue = spec.Thirst;
        serialized.FindProperty("foodHealthRestoreAmount").floatValue = spec.Health;
        serialized.FindProperty("foodBuffType").intValue = (int)spec.Buff;
        serialized.FindProperty("foodBuffStrength").floatValue = spec.BuffStrength;
        serialized.FindProperty("foodBuffDuration").floatValue = spec.BuffSeconds;

        if (spec.Damage > 0f) // 도구 · 무기 능력치
        {
            serialized.FindProperty("weaponAttackType").intValue = (int)(spec.Ranged ? WeaponAttackType.Ranged : WeaponAttackType.Melee);
            serialized.FindProperty("baseDamage").floatValue = spec.Damage;
            serialized.FindProperty("attackCooldown").floatValue = spec.Ranged ? 0.55f : 0.7f;
            serialized.FindProperty("attackRange").floatValue = spec.Ranged ? 38f : 2.2f;
            serialized.FindProperty("attackRadius").floatValue = spec.Ranged ? 0.1f : 0.4f;
            serialized.FindProperty("staminaCost").floatValue = spec.Ranged ? 4f : 2f;

            if (spec.Ranged) // 좋은 활은 기본 활의 발사 데이터를 그대로 쓴다
            {
                ItemData bow = AssetDatabase.LoadAssetAtPath<ItemData>(BowItemPath);
                SerializedObject bowObject = bow != null ? new SerializedObject(bow) : null;
                serialized.FindProperty("rangedWeaponData").objectReferenceValue = bowObject?.FindProperty("rangedWeaponData").objectReferenceValue;
            }
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(item);
        return item;
    }

    private static CookingRecipeData CreateCook(CookSpec spec)
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
        serialized.FindProperty("requiredStation").intValue = (int)spec.Station;
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
        (string id, int amount)[] used = ingredients.Where(entry => entry.amount > 0 && FindItem(entry.id) != null).ToArray();
        property.arraySize = used.Length;

        for (int index = 0; index < used.Length; index++)
        {
            SerializedProperty entry = property.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("itemData").objectReferenceValue = FindItem(used[index].id);
            entry.FindPropertyRelative("amount").intValue = used[index].amount;
        }
    }

    // ---------------------------------------------------------------- 한글 이름 · 선물 · 의뢰 · 등록

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
            (FurTag, "resource_fine_pelt", "고급 털가죽"), (FurTag, "resource_thick_hide", "두꺼운 가죽"), (FurTag, "resource_wool", "두꺼운 털"), (FurTag, "resource_tanned_leather", "무두질 가죽"),
            (HuntTag, "resource_antler", "뿔"), (HuntTag, "resource_beast_fang", "짐승 이빨"), (HuntTag, "resource_feather", "깃털"), (HuntTag, "resource_stinger", "독침"),
            (InsectTag, "resource_butterfly", "나비"), (InsectTag, "resource_firefly", "반딧불이"),
            (HeartyTag, "food_roast_meat", "든든한 요리"), (HeartyTag, "food_meat_stew", "든든한 요리"), (HeartyTag, "food_venison_steak", "든든한 요리"), (HeartyTag, "food_crab_soup", "든든한 요리")
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

        foreach ((string character, string preference, string target, string note) in GiftRules)
        {
            if (giftPairs.Add($"{character},{preference},{target}"))
            {
                giftLines.Add($"{character},{preference},{target},{note}");
                gifts++;
            }
        }

        if (gifts > 0)
        {
            File.WriteAllLines(GiftsPath, giftLines, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(GiftsPath);
        }

        return $"선물 태그 {tags}개 · 이웃 취향 {gifts}개 추가 (털가죽 · 사냥 자랑거리 · 곤충)";
    }

    private static string UpdateQuests()
    {
        string path = "Assets/_ProjectU/Data/Npc/Source/NpcQuests.csv";

        if (!File.Exists(path))
        {
            return "✗ 의뢰 목록이 없습니다.";
        }

        List<string> lines = File.ReadAllLines(path, Encoding.UTF8).ToList();
        HashSet<string> existing = new HashSet<string>(lines.Skip(1).Select(line => line.Split(',')[0].Trim()), StringComparer.Ordinal);
        int added = 0;

        foreach ((string id, string owner, string title, string requirement, int coins, int affinity, int days, string request, string thanks) in Quests)
        {
            if (existing.Contains(id))
            {
                continue;
            }

            lines.Add($"{id},{owner},Board,{title},{requirement},{coins},{affinity},,{days},All,,{request},{thanks},");
            added++;
        }

        if (added > 0)
        {
            File.WriteAllLines(path, lines, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(path);
        }

        return $"사냥 게시판 의뢰 {added}개 추가 (라그 고기 · 하르카 털가죽 · 뤼네트 독침)";
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
        HashSet<UnityEngine.Object> existing = new HashSet<UnityEngine.Object>();

        for (int index = 0; index < list.arraySize; index++)
        {
            existing.Add(list.GetArrayElementAtIndex(index).objectReferenceValue);
        }

        int added = 0;

        foreach (ItemData item in items.Values.Where(item => item != null && !existing.Contains(item)))
        {
            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = item;
            added++;
        }

        if (added > 0)
        {
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
        }

        return $"ItemDatabase 등록 {added}개";
    }

    // ---------------------------------------------------------------- 공통 도우미

    public static ItemData FindItem(string itemId)
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

    private static bool CopyPrefab(string from, string to)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(to) != null)
        {
            return true;
        }

        return AssetDatabase.LoadAssetAtPath<GameObject>(from) != null && AssetDatabase.CopyAsset(from, to);
    }
}
