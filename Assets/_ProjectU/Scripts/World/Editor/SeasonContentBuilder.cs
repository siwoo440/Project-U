using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 119일차: 계절 채집과 농사 넓히기
// 1. 계절마다 나는 숲 채집물 12종 (봄 산나물 · 여름 산딸기 · 가을 밤 · 겨울 마른 가지)
// 2. 심어서 기르는 과일나무 3종 (사과 · 자두 · 감)과 묘목
// 3. 새 작물 3종 (옥수수 · 양배추 · 고구마)과 절구에서 만드는 동물 사료
// 4. 계절 요리 · 약과 계절이 바뀔 때 알림
// 자동 적용(ProjectUAutoContent)이 실행한다. 여러 번 실행해도 같은 결과가 나온다.
public static partial class SeasonContentBuilder
{
    private const string ScenePath = "Assets/_ProjectU/Scenes/20_Gameplay.unity";
    private const string ItemFolder = "Assets/_ProjectU/Data/Items/Day119";
    private const string CookingFolder = "Assets/_ProjectU/Data/Cooking/Day119";
    private const string CraftingFolder = "Assets/_ProjectU/Data/Crafting/Day119";
    private const string BuildingFolder = "Assets/_ProjectU/Data/Building/Day119";
    private const string ForagePrefabFolder = "Assets/_ProjectU/Prefabs/Gathering/Day119";
    private const string TreePrefabFolder = "Assets/_ProjectU/Prefabs/Orchard/Day119";
    private const string BuildPrefabFolder = "Assets/_ProjectU/Prefabs/Building/Day119";
    private const string ItemDatabasePath = "Assets/_ProjectU/Data/Databases/ItemDatabase.asset";
    private const string KoreanNamesPath = "Assets/_ProjectU/Data/Items/ItemKoreanNames.csv";
    private const string GiftTagsPath = "Assets/_ProjectU/Data/Npc/Source/NpcGiftTags.csv";
    private const string GiftsPath = "Assets/_ProjectU/Data/Npc/Source/NpcGifts.csv";
    private const string QuestsPath = "Assets/_ProjectU/Data/Npc/Source/NpcQuests.csv";
    private const string StoneResourcePath = "Assets/_ProjectU/Prefabs/Gathering/StoneResource_01.prefab";
    private const string CampfirePlacedPath = "Assets/_ProjectU/Prefabs/Building/Day73/StoneCampfirePlaced.prefab";
    private const string CampfirePreviewPath = "Assets/_ProjectU/Prefabs/Building/Day73/StoneCampfirePreview.prefab";
    private const string MortarPlacedPath = BuildPrefabFolder + "/MortarPlaced.prefab";
    private const string MortarPreviewPath = BuildPrefabFolder + "/MortarPreview.prefab";
    private const string MortarRecipePath = BuildingFolder + "/BuildRecipe_Mortar.asset";
    private const string WoodPath = "Assets/_ProjectU/Data/Items/ItemData_Wood.asset";
    private const string StonePath = "Assets/_ProjectU/Data/Items/ItemData_Stone.asset";

    public const string RootName = "=== Season Forage ==="; // Scene 계절 채집물 묶음
    public const string ForageTag = "forage"; // 제철 채집물 선물 태그

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
        public float Hunger; // 허기 회복
        public float Thirst; // 갈증 회복
        public float Health; // 체력 회복
        public FoodBuffType Buff = FoodBuffType.None; // 보조 효과
        public float BuffStrength; // 보조 효과 세기
        public float BuffSeconds; // 보조 효과 시간
    }

    // 계절 채집물 · 요리 · 과일 · 묘목 (모델 ID는 아이템 ID와 같다)
    public static readonly ItemSpec[] Items =
    {
        // 봄
        new ItemSpec { Id = "resource_wild_greens", Asset = "ItemData_WildGreens", English = "WILD GREENS", Korean = "산나물", Description = "Spring greens picked in the forest." },
        new ItemSpec { Id = "resource_wild_flower", Asset = "ItemData_WildFlower", English = "WILD FLOWERS", Korean = "들꽃", Description = "A small bunch of spring flowers." },
        new ItemSpec { Id = "food_bamboo_shoot", Asset = "ItemData_BambooShoot", English = "BAMBOO SHOOT", Korean = "죽순", Description = "A tender shoot that comes up in spring.", Category = ItemCategory.Food, Hunger = 10f },

        // 여름
        new ItemSpec { Id = "food_raspberry", Asset = "ItemData_Raspberry", English = "RASPBERRY", Korean = "나무딸기", Description = "Sweet summer berries from the forest.", Category = ItemCategory.Food, Stack = 30, Hunger = 6f, Thirst = 4f },
        new ItemSpec { Id = "resource_herb_leaf", Asset = "ItemData_HerbLeaf", English = "HERB LEAF", Korean = "약초 잎", Description = "A medicine leaf that grows by the marsh." },
        new ItemSpec { Id = "resource_bamboo", Asset = "ItemData_Bamboo", English = "BAMBOO", Korean = "대나무", Description = "A light, strong stalk for crafting." },

        // 가을
        new ItemSpec { Id = "food_chestnut", Asset = "ItemData_Chestnut", English = "CHESTNUT", Korean = "밤", Description = "An autumn nut. Better roasted.", Category = ItemCategory.Food, Hunger = 8f },
        new ItemSpec { Id = "food_big_mushroom", Asset = "ItemData_BigMushroom", English = "BIG MUSHROOM", Korean = "큰 버섯", Description = "A large autumn mushroom for hotpots.", Category = ItemCategory.Food, Hunger = 12f },
        new ItemSpec { Id = "resource_acorn", Asset = "ItemData_Acorn", English = "ACORN", Korean = "도토리", Description = "Acorns. Ground in a mortar they feed animals." },

        // 겨울
        new ItemSpec { Id = "resource_dry_branch", Asset = "ItemData_DryBranch", English = "DRY BRANCH", Korean = "마른 가지", Description = "Dry wood that lights a fire quickly." },
        new ItemSpec { Id = "resource_ice_flower", Asset = "ItemData_IceFlower", English = "ICE FLOWER", Korean = "얼음꽃", Description = "A rare winter flower used in medicine.", Stack = 10 },
        new ItemSpec { Id = "resource_pine_cone", Asset = "ItemData_PineCone", English = "PINE CONE", Korean = "솔방울", Description = "A pine cone. Good kindling." },

        // 계절 요리 · 약
        new ItemSpec { Id = "food_greens_salad", Asset = "ItemData_GreensSalad", English = "GREENS SALAD", Korean = "나물 무침", Description = "Spring greens dressed and tossed.", Category = ItemCategory.Food, Stack = 10, Hunger = 28f, Health = 6f },
        new ItemSpec { Id = "food_bamboo_stirfry", Asset = "ItemData_BambooStirfry", English = "BAMBOO STIR-FRY", Korean = "죽순 볶음", Description = "Bamboo shoots fried with meat.", Category = ItemCategory.Food, Stack = 10, Hunger = 40f, Health = 8f, Buff = FoodBuffType.Satiety, BuffStrength = 20f, BuffSeconds = 240f },
        new ItemSpec { Id = "food_berry_drink", Asset = "ItemData_BerryDrink", English = "BERRY DRINK", Korean = "나무딸기 음료", Description = "A cool summer drink.", Category = ItemCategory.Drink, Stack = 10, Thirst = 45f, Hunger = 8f, Buff = FoodBuffType.MoveSpeed, BuffStrength = 10f, BuffSeconds = 180f },
        new ItemSpec { Id = "food_roast_chestnut", Asset = "ItemData_RoastChestnut", English = "ROAST CHESTNUTS", Korean = "군밤", Description = "Warm roasted chestnuts in a paper bag.", Category = ItemCategory.Food, Stack = 10, Hunger = 32f, Buff = FoodBuffType.Warmth, BuffStrength = 18f, BuffSeconds = 240f },
        new ItemSpec { Id = "food_mushroom_hotpot", Asset = "ItemData_MushroomHotpot", English = "MUSHROOM HOTPOT", Korean = "버섯 전골", Description = "A hot autumn pot full of mushrooms.", Category = ItemCategory.Food, Stack = 10, Hunger = 46f, Thirst = 16f, Health = 12f, Buff = FoodBuffType.StaminaRecovery, BuffStrength = 22f, BuffSeconds = 300f },
        new ItemSpec { Id = "medicine_winter_tonic", Asset = "ItemData_WinterTonic", English = "WINTER TONIC", Korean = "겨울 약", Description = "Ice flower medicine that keeps the cold out.", Category = ItemCategory.Medicine, Stack = 10, Health = 25f, Buff = FoodBuffType.Warmth, BuffStrength = 40f, BuffSeconds = 420f },

        // 과일 · 묘목
        new ItemSpec { Id = "food_plum", Asset = "ItemData_Plum", English = "PLUM", Korean = "자두", Description = "A summer plum from a planted tree.", Category = ItemCategory.Food, Hunger = 9f, Thirst = 5f },
        new ItemSpec { Id = "food_persimmon", Asset = "ItemData_Persimmon", English = "PERSIMMON", Korean = "감", Description = "An autumn persimmon from a planted tree.", Category = ItemCategory.Food, Hunger = 11f },
        new ItemSpec { Id = "resource_sapling_apple", Asset = "ItemData_SaplingApple", English = "APPLE SAPLING", Korean = "사과나무 묘목", Description = "Plant it and it bears apples every autumn.", Stack = 5 },
        new ItemSpec { Id = "resource_sapling_plum", Asset = "ItemData_SaplingPlum", English = "PLUM SAPLING", Korean = "자두나무 묘목", Description = "Plant it and it bears plums every summer.", Stack = 5 },
        new ItemSpec { Id = "resource_sapling_persimmon", Asset = "ItemData_SaplingPersimmon", English = "PERSIMMON SAPLING", Korean = "감나무 묘목", Description = "Plant it and it bears persimmons every autumn.", Stack = 5 },

        // 새 작물 요리
        new ItemSpec { Id = "food_grilled_corn", Asset = "ItemData_GrilledCorn", English = "GRILLED CORN", Korean = "옥수수 구이", Description = "Sweet corn grilled on a stick.", Category = ItemCategory.Food, Stack = 10, Hunger = 30f, Health = 5f },
        new ItemSpec { Id = "food_cabbage_wrap", Asset = "ItemData_CabbageWrap", English = "CABBAGE WRAP", Korean = "양배추 쌈", Description = "Meat wrapped in cabbage leaves.", Category = ItemCategory.Food, Stack = 10, Hunger = 38f, Health = 10f },
        new ItemSpec { Id = "food_baked_sweet_potato", Asset = "ItemData_BakedSweetPotato", English = "BAKED SWEET POTATO", Korean = "고구마 구이", Description = "A hot baked sweet potato.", Category = ItemCategory.Food, Stack = 10, Hunger = 42f, Buff = FoodBuffType.Warmth, BuffStrength = 22f, BuffSeconds = 260f }
    };

    // ---------------------------------------------------------------- 요리 · 제작 표

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
        new CookSpec { Id = "cook_greens_salad", Asset = "CookingRecipe_GreensSalad", English = "GREENS SALAD", ResultId = "food_greens_salad", Seconds = 18f, Ingredients = new[] { ("resource_wild_greens", 2) } },
        new CookSpec { Id = "cook_roast_chestnut", Asset = "CookingRecipe_RoastChestnut", English = "ROAST CHESTNUTS", ResultId = "food_roast_chestnut", Seconds = 20f, Ingredients = new[] { ("food_chestnut", 3) } },
        new CookSpec { Id = "cook_grilled_corn", Asset = "CookingRecipe_GrilledCorn", English = "GRILLED CORN", ResultId = "food_grilled_corn", Seconds = 20f, Ingredients = new[] { ("food_corn", 1) } },
        new CookSpec { Id = "cook_baked_sweet_potato", Asset = "CookingRecipe_BakedSweetPotato", English = "BAKED SWEET POTATO", ResultId = "food_baked_sweet_potato", Seconds = 24f, Ingredients = new[] { ("food_sweet_potato", 2) } },
        new CookSpec { Id = "cook_bamboo_stirfry", Asset = "CookingRecipe_BambooStirfry", English = "BAMBOO STIR-FRY", ResultId = "food_bamboo_stirfry", Seconds = 30f, Station = CookingStationTier.StoneCampfire, Ingredients = new[] { ("food_bamboo_shoot", 2), ("food_red_meat", 1) } },
        new CookSpec { Id = "cook_berry_drink", Asset = "CookingRecipe_BerryDrink", English = "BERRY DRINK", ResultId = "food_berry_drink", Seconds = 26f, Station = CookingStationTier.StoneCampfire, Ingredients = new[] { ("food_raspberry", 3), ("drink_water_bottle", 1) } },
        new CookSpec { Id = "cook_mushroom_hotpot", Asset = "CookingRecipe_MushroomHotpot", English = "MUSHROOM HOTPOT", ResultId = "food_mushroom_hotpot", Seconds = 40f, Station = CookingStationTier.StoneCampfire, Ingredients = new[] { ("food_big_mushroom", 2), ("food_cabbage", 1), ("drink_water_bottle", 1) } },
        new CookSpec { Id = "cook_cabbage_wrap", Asset = "CookingRecipe_CabbageWrap", English = "CABBAGE WRAP", ResultId = "food_cabbage_wrap", Seconds = 28f, Station = CookingStationTier.StoneCampfire, Ingredients = new[] { ("food_cabbage", 1), ("food_red_meat", 1) } },
        new CookSpec { Id = "cook_winter_tonic", Asset = "CookingRecipe_WinterTonic", English = "WINTER TONIC", ResultId = "medicine_winter_tonic", Seconds = 36f, Station = CookingStationTier.StoneCampfire, Ingredients = new[] { ("resource_ice_flower", 1), ("resource_herb_leaf", 2) } },

        // 절구 : 곡식과 도토리를 갈아 동물 사료로
        new CookSpec { Id = "mortar_feed_acorn", Asset = "CookingRecipe_FeedAcorn", English = "ANIMAL FEED", ResultId = "item_animal_feed", ResultAmount = 14, Seconds = 35f, Station = CookingStationTier.Mortar, Ingredients = new[] { ("resource_acorn", 4) } },
        new CookSpec { Id = "mortar_feed_corn", Asset = "CookingRecipe_FeedCorn", English = "ANIMAL FEED", ResultId = "item_animal_feed", ResultAmount = 22, Seconds = 30f, Station = CookingStationTier.Mortar, Ingredients = new[] { ("food_corn", 1) } }
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
        new CraftSpec { Id = "recipe_sapling_apple", Asset = "CraftingRecipe_SaplingApple", English = "APPLE SAPLING", ResultId = "resource_sapling_apple", Ingredients = new[] { ("item_wood", 3), ("food_apple", 2) } },
        new CraftSpec { Id = "recipe_bamboo_basket", Asset = "CraftingRecipe_BambooBasket", English = "PLANT FIBER", ResultId = "item_plant_fiber", ResultAmount = 4, Facility = CraftingFacilityType.Hand, Ingredients = new[] { ("resource_bamboo", 1) } },
        new CraftSpec { Id = "recipe_dry_kindling", Asset = "CraftingRecipe_DryKindling", English = "WOOD", ResultId = "item_wood", ResultAmount = 2, Facility = CraftingFacilityType.Hand, Ingredients = new[] { ("resource_dry_branch", 2), ("resource_pine_cone", 1) } }
    };

    // 계절 채집물 선물 취향
    private static readonly (string character, string preference, string target, string note)[] GiftRules =
    {
        ("char_erina", "Loved", "tag:" + ForageTag, "숲에서 난 것"),
        ("char_lunette", "Loved", "resource_herb_leaf", "약 재료"),
        ("char_beatrice", "Loved", "resource_wild_flower", "꽃"),
        ("char_lanhua", "Loved", "resource_wild_flower", "꽃"),
        ("char_akane", "Liked", "tag:" + ForageTag, "제철 음식"),
        ("char_octavia", "Liked", "tag:" + ForageTag, "제철 재료"),
        ("char_noctia", "Liked", "resource_ice_flower", "겨울 꽃"),
        ("char_seira", "Liked", "food_roast_chestnut", "따뜻한 군밤")
    };

    // 계절 게시판 의뢰
    private static readonly (string id, string owner, string title, string requirement, int coins, int affinity, int days, string season, string request, string thanks)[] Quests =
    {
        ("quest_erina_greens", "char_erina", "봄 산나물 모으기", "resource_wild_greens:6", 48, 4, 3, "Spring", "봄 산나물 여섯 줌만 모아 주실래요? 숲이 깨어난 걸 느끼고 싶어요.", "향이 좋네요. 숲의 봄을 나눠 주셔서 고마워요."),
        ("quest_lunette_herb", "char_lunette", "약에 쓸 약초 잎", "resource_herb_leaf:4", 52, 4, 3, "Summer", "약초 잎 네 장이 필요해요… 습지 쪽에 많이 자란다고 들었어요.", "이걸 말려 두면 겨울에도 약을 만들 수 있어요."),
        ("quest_beatrice_flower", "char_beatrice", "꿀을 모을 들꽃", "resource_wild_flower:5", 34, 4, 2, "Spring", "들꽃 다섯 다발! 일벌들이 꿀을 모을 곳이 필요해.", "좋아. 이번 봄 꿀은 네 덕이야.")
    };

    // ---------------------------------------------------------------- 만들기 (1단계 : 아이템)

    // 111 · 113일차 의뢰(계절 게시판)가 이 아이템을 쓰기 때문에 아이템을 먼저 만든다.
    public static string BuildItems()
    {
        StringBuilder report = new StringBuilder("[계절 채집 아이템 만들기]\n");

        try
        {
            EnsureFolders();
            EditorUtility.DisplayProgressBar("Project U 계절", "모델", 0.1f);
            report.AppendLine($"계절 채집물 · 과일 · 작물 모델 {EnsureModels()}종 준비");

            EditorUtility.DisplayProgressBar("Project U 계절", "아이템", 0.35f);
            Dictionary<string, ItemData> items = CreateItems();
            report.AppendLine($"계절 채집물 · 요리 · 과일 · 묘목 아이템 {items.Count}개");
            report.AppendLine(UpdateKoreanNames());

            EditorUtility.DisplayProgressBar("Project U 계절", "새 작물", 0.55f);
            FarmingContentBuilder.BuildAll(); // 옥수수 · 양배추 · 고구마 작물과 씨앗
            report.AppendLine("새 작물 3종 (옥수수 · 양배추 · 고구마) 씨앗 · 작물 데이터 생성");
            report.AppendLine(ItemKoreanNameBuilder.Apply()); // 새 아이템에 한글 이름 넣기
            NpcShopBuilder.BuildAll(); // 리첼 씨앗 가게에 새 씨앗 넣기
            report.AppendLine("씨앗 가게에 새 작물 씨앗 등록");

            report.AppendLine(RegisterItems(items));
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayProgressBar("Project U 계절", "판매 가격 · 외형", 0.8f);
            MarketContentBuilder.RefreshCatalog();
            GameDataRegistryEditor.CreateOrRefreshDefaultRegistry();
            report.AppendLine(BuildVisuals("판매 가격 · 아이콘 · 바닥 Prefab 생성"));
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
        }

        return report.ToString();
    }

    // ---------------------------------------------------------------- 만들기 (2단계 : 채집물 · 과일나무 · 절구)

    public static string BuildAll(bool saveScene)
    {
        StringBuilder report = new StringBuilder("[계절 채집 · 과일나무 만들기]\n");

        try
        {
            EnsureFolders();
            EditorUtility.DisplayProgressBar("Project U 계절", "모델 · 아이템", 0.05f);
            EnsureModels();
            Dictionary<string, ItemData> items = CreateItems();
            report.AppendLine($"계절 채집물 · 요리 · 과일 · 묘목 아이템 {items.Count}개");
            report.AppendLine(UpdateKoreanNames());

            EditorUtility.DisplayProgressBar("Project U 계절", "요리 · 제작법", 0.2f);
            List<CookingRecipeData> cooks = Cooks.Select(CreateCook).Where(recipe => recipe != null).ToList();
            report.AppendLine($"계절 요리 · 절구 제조법 {cooks.Count}개");
            int crafts = Crafts.Count(spec => CreateCraft(spec) != null);
            report.AppendLine($"제작법 {crafts}개 (사과나무 묘목 · 대나무 섬유 · 마른 가지 장작)");

            EditorUtility.DisplayProgressBar("Project U 계절", "채집물 Prefab", 0.35f);
            report.AppendLine(BuildForagePrefabs());

            EditorUtility.DisplayProgressBar("Project U 계절", "섬에 채집물 놓기", 0.5f);
            report.AppendLine(PlaceForage());

            EditorUtility.DisplayProgressBar("Project U 계절", "과일나무", 0.65f);
            report.AppendLine(BuildFruitTrees());

            EditorUtility.DisplayProgressBar("Project U 계절", "절구", 0.75f);
            report.AppendLine(BuildMortar(cooks));

            EditorUtility.DisplayProgressBar("Project U 계절", "계절 알림 · 상인", 0.82f);
            report.AppendLine(ConnectSeasonNotice());

            EditorUtility.DisplayProgressBar("Project U 계절", "선물 · 의뢰", 0.88f);
            report.AppendLine(UpdateGiftTags());
            report.AppendLine(UpdateQuests());
            report.AppendLine(ItemKoreanNameBuilder.Apply()); // 새 아이템에 한글 이름 넣기
            NpcShopBuilder.BuildAll(); // 씨앗 가게 갱신
            report.AppendLine("씨앗 가게 갱신");

            EditorUtility.DisplayProgressBar("Project U 계절", "아이템 등록 · 외형", 0.93f);
            report.AppendLine(RegisterItems(items));
            AssetDatabase.SaveAssets();
            MarketContentBuilder.RefreshCatalog();
            GameDataRegistryEditor.CreateOrRefreshDefaultRegistry();
            report.AppendLine(BuildVisuals("아이템 외형 · 아이콘 · 바닥 Prefab 생성"));
            BuildableVisualProfileBuilder.BuildAll();
            report.AppendLine("과일나무 · 절구 외형 카드 연결");
            NpcContentBuilder.BuildAll();
            NpcQuestBuilder.BuildAll();
            report.AppendLine("이웃 선물 취향 · 계절 게시판 의뢰 갱신");

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

    // 새 아이템이 많을 때는 줍기 Prefab과 등록 목록이 한 번에 맞춰지지 않을 수 있어 한 번 더 돌린다
    private static string BuildVisuals(string okMessage)
    {
        string visual = ItemVisualContentBuilder.BuildAll();

        if (visual.Contains("✗"))
        {
            visual = ItemVisualContentBuilder.BuildAll();
        }

        return visual.Split('\n').FirstOrDefault(line => line.StartsWith("✗")) ?? okMessage;
    }

    private static void EnsureFolders()
    {
        foreach (string folder in new[] { ItemFolder, CookingFolder, CraftingFolder, BuildingFolder, ForagePrefabFolder, TreePrefabFolder, BuildPrefabFolder })
        {
            StylizedArtAssetFactory.EnsureFolder(folder);
        }
    }

    private static int EnsureModels()
    {
        List<string> ids = Items.Select(spec => spec.Id).ToList();
        ids.AddRange(new[] { "tree_sapling", "tree_young", "build_mortar" });
        ids.AddRange(Trees.SelectMany(spec => new[] { spec.MatureModel, spec.FruitModel }));
        ids.AddRange(new[] { "item_seed_corn", "item_seed_cabbage", "item_seed_sweet_potato", "food_corn", "food_cabbage", "food_sweet_potato" });
        ids.AddRange(new[] { "crop_corn_growing", "crop_corn_mature", "crop_cabbage_growing", "crop_cabbage_mature", "crop_sweet_potato_growing", "crop_sweet_potato_mature" });
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
        serialized.FindProperty("hungerRestoreAmount").floatValue = spec.Category == ItemCategory.Food ? spec.Hunger : 0f;
        serialized.FindProperty("foodThirstRestoreAmount").floatValue = spec.Category == ItemCategory.Food ? spec.Thirst : 0f;
        serialized.FindProperty("foodHealthRestoreAmount").floatValue = spec.Category == ItemCategory.Food ? spec.Health : 0f;
        serialized.FindProperty("thirstRestoreAmount").floatValue = spec.Category == ItemCategory.Drink ? spec.Thirst : 0f;
        serialized.FindProperty("healthRestoreAmount").floatValue = spec.Category == ItemCategory.Drink || spec.Category == ItemCategory.Medicine ? spec.Health : 0f;
        serialized.FindProperty("foodBuffType").intValue = (int)spec.Buff;
        serialized.FindProperty("foodBuffStrength").floatValue = spec.BuffStrength;
        serialized.FindProperty("foodBuffDuration").floatValue = spec.BuffSeconds;
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

        foreach ((string id, string korean) in Items.Select(spec => (spec.Id, spec.Korean))
            .Concat(new[] { ("seed_corn", "옥수수 씨앗"), ("seed_cabbage", "양배추 씨앗"), ("seed_sweet_potato", "고구마 씨앗"), ("food_corn", "옥수수"), ("food_cabbage", "양배추"), ("food_sweet_potato", "고구마") }))
        {
            if (existing.Add(id))
            {
                lines.Add($"{id},{korean}");
                added++;
            }
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
            (ForageTag, "resource_wild_greens", "제철 채집물"), (ForageTag, "food_bamboo_shoot", "제철 채집물"), (ForageTag, "food_raspberry", "제철 채집물"),
            (ForageTag, "food_chestnut", "제철 채집물"), (ForageTag, "food_big_mushroom", "제철 채집물"), (ForageTag, "resource_herb_leaf", "제철 채집물"),
            ("dish", "food_greens_salad", "요리"), ("dish", "food_bamboo_stirfry", "요리"), ("dish", "food_mushroom_hotpot", "요리"), ("dish", "food_cabbage_wrap", "요리"),
            ("sweet", "food_berry_drink", "단것"), ("sweet", "food_roast_chestnut", "단것"), ("sweet", "food_baked_sweet_potato", "단것"),
            ("hearty", "food_mushroom_hotpot", "든든한 요리"), ("hearty", "food_grilled_corn", "든든한 요리")
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

        return $"선물 태그 {tags}개 · 이웃 취향 {gifts}개 추가 (제철 채집물 · 꽃 · 계절 요리)";
    }

    private static string UpdateQuests()
    {
        if (!File.Exists(QuestsPath))
        {
            return "✗ 의뢰 목록이 없습니다.";
        }

        List<string> lines = File.ReadAllLines(QuestsPath, Encoding.UTF8).ToList();
        HashSet<string> existing = new HashSet<string>(lines.Skip(1).Select(line => line.Split(',')[0].Trim()), StringComparer.Ordinal);
        int added = 0;

        foreach ((string id, string owner, string title, string requirement, int coins, int affinity, int days, string season, string request, string thanks) in Quests)
        {
            if (existing.Contains(id))
            {
                continue;
            }

            lines.Add($"{id},{owner},Board,{title},{requirement},{coins},{affinity},,{days},{season},,{request},{thanks},");
            added++;
        }

        if (added > 0)
        {
            File.WriteAllLines(QuestsPath, lines, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(QuestsPath);
        }

        return $"계절 게시판 의뢰 {added}개 추가 (에리나 산나물 · 뤼네트 약초 · 베아트리스 들꽃)";
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
