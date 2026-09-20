using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 120일차: 동굴 몬스터와 첫 보스
// 1. 동굴 몬스터 3종 (박쥐 · 동굴 거미 · 돌 골렘)과 층별 배치
// 2. 깊은층 가운데 방의 보스 (수정 골렘) · 수정 문 · 수정 조각 소환
// 3. 보스 전리품(수정 심장)으로 만드는 수정 검 · 수정 갑옷
// 4. 동굴 전리품을 이웃 선물 · 게시판 의뢰로 이어 준다
// 자동 적용(ProjectUAutoContent)이 실행한다. 여러 번 실행해도 같은 결과가 나온다.
public static partial class CaveMonsterBuilder
{
    private const string ScenePath = "Assets/_ProjectU/Scenes/20_Gameplay.unity";
    private const string ItemFolder = "Assets/_ProjectU/Data/Items/Day120";
    private const string CraftingFolder = "Assets/_ProjectU/Data/Crafting/Day120";
    private const string EnemyDataFolder = "Assets/_ProjectU/Data/Enemies/Day120";
    private const string MonsterPrefabFolder = "Assets/_ProjectU/Prefabs/Enemies/Day120";
    private const string LootFolder = "Assets/_ProjectU/Prefabs/Enemies/Day120/Loot";
    private const string ItemDatabasePath = "Assets/_ProjectU/Data/Databases/ItemDatabase.asset";
    private const string KoreanNamesPath = "Assets/_ProjectU/Data/Items/ItemKoreanNames.csv";
    private const string GiftTagsPath = "Assets/_ProjectU/Data/Npc/Source/NpcGiftTags.csv";
    private const string GiftsPath = "Assets/_ProjectU/Data/Npc/Source/NpcGifts.csv";
    private const string QuestsPath = "Assets/_ProjectU/Data/Npc/Source/NpcQuests.csv";
    private const string GruntPrefabPath = "Assets/_ProjectU/Prefabs/Enemies/Day74/Enemy_MeleeGrunt.prefab";
    private const string SpitterPrefabPath = "Assets/_ProjectU/Prefabs/Enemies/Day74/Enemy_RangedSpitter.prefab";
    private const string ProjectilePrefabPath = "Assets/_ProjectU/Prefabs/Enemies/Day74/EnemyProjectile_Day74.prefab";
    private const string BoltPrefabPath = MonsterPrefabFolder + "/CrystalBoltProjectile.prefab";
    private const string PickupRegistryPath = "Assets/_ProjectU/Prefabs/Items/Day75/WorldItemPickupRegistry_Day75.asset";

    public const string RootName = "=== Cave Monsters ==="; // Scene 동굴 몬스터 묶음
    public const string RuntimeParentName = "CaveMonsterRuntime"; // 실행 중 생긴 몬스터가 들어가는 곳
    public const string BossId = "boss_crystal_golem"; // 보스 ID
    public const string CaveTrophyTag = "cave_trophy"; // 동굴 전리품 선물 태그

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
        public EquipmentSlotType Slot = EquipmentSlotType.None; // 장비 칸
        public float Defense; // 피해 감소 (%)
        public float Cold; // 추위 감소 (%)
        public float MaximumHealthBonus; // 최대 체력 증가
        public float Damage; // 무기 공격력
    }

    // 동굴 전리품과 수정 장비 (모델 ID는 아이템 ID와 같다)
    public static readonly ItemSpec[] Items =
    {
        new ItemSpec { Id = "resource_bat_wing", Asset = "ItemData_BatWing", English = "BAT WING", Korean = "박쥐 날개", Description = "A thin wing from a cave bat." },
        new ItemSpec { Id = "resource_venom_fang", Asset = "ItemData_VenomFang", English = "VENOM FANG", Korean = "독니", Description = "A cave spider fang still wet with venom.", Stack = 10 },
        new ItemSpec { Id = "resource_crystal_heart", Asset = "ItemData_CrystalHeart", English = "CRYSTAL HEART", Korean = "수정 심장", Description = "The glowing heart of the crystal golem.", Stack = 5 },
        new ItemSpec
        {
            Id = "weapon_crystal_sword", Asset = "ItemData_CrystalSword", English = "CRYSTAL SWORD", Korean = "수정 검", Description = "A blade grown from the golem's heart.",
            Category = ItemCategory.Weapon, Stack = 1, Damage = 34f
        },
        new ItemSpec
        {
            Id = "equipment_crystal_armor", Asset = "ItemData_CrystalArmor", English = "CRYSTAL ARMOR", Korean = "수정 갑옷", Description = "Stone and crystal plate that turns blows aside.",
            Category = ItemCategory.Equipment, Stack = 1, Slot = EquipmentSlotType.Body, Defense = 22f, Cold = 15f, MaximumHealthBonus = 20f
        }
    };

    // ---------------------------------------------------------------- 제작 · 선물 · 의뢰 표

    private sealed class CraftSpec
    {
        public string Id;
        public string Asset;
        public string English;
        public string ResultId;
        public CraftingFacilityType Facility = CraftingFacilityType.Workbench;
        public (string id, int amount)[] Ingredients;
    }

    private static readonly CraftSpec[] Crafts =
    {
        new CraftSpec { Id = "recipe_crystal_sword", Asset = "CraftingRecipe_CrystalSword", English = "CRYSTAL SWORD", ResultId = "weapon_crystal_sword", Ingredients = new[] { ("resource_crystal_heart", 1), ("resource_ingot_steel", 3), ("resource_tanned_leather", 2) } },
        new CraftSpec { Id = "recipe_crystal_armor", Asset = "CraftingRecipe_CrystalArmor", English = "CRYSTAL ARMOR", ResultId = "equipment_crystal_armor", Ingredients = new[] { ("resource_crystal_heart", 1), ("resource_tanned_leather", 4), ("resource_crystal", 4) } }
    };

    private static readonly (string character, string preference, string target, string note)[] GiftRules =
    {
        ("char_ragh", "Liked", "tag:" + CaveTrophyTag, "동굴 자랑거리"),
        ("char_dravia", "Loved", "resource_crystal_heart", "대장장이의 보물"),
        ("char_safira", "Liked", "resource_venom_fang", "같은 독을 쓰는 사이"),
        ("char_noctia", "Liked", "resource_bat_wing", "밤의 날개"),
        ("char_arachne", "Loved", "resource_venom_fang", "거미의 송곳니")
    };

    private static readonly (string id, string owner, string title, string requirement, int coins, int affinity, int days, string request, string thanks)[] Quests =
    {
        ("quest_ragh_batwing", "char_ragh", "동굴 박쥐 사냥", "resource_bat_wing:4", 62, 4, 3, "동굴 박쥐 날개 네 장. 놈들이 자꾸 횃불을 꺼뜨려서 말이야.", "잘했다. 이제 동굴이 좀 조용하겠군."),
        ("quest_safira_fang", "char_safira", "거미 독니 구하기", "resource_venom_fang:3", 92, 4, 4, "동굴 거미의 독니 세 개… 독을 다루는 연습에 쓰려고 해.", "고마워. 이 독은 내 것보다 거칠지만 쓸 만하네.")
    };

    // ---------------------------------------------------------------- 만들기 (1단계 : 아이템)

    // 111 · 113일차 의뢰(동굴 사냥 게시판)가 이 아이템을 쓰기 때문에 아이템을 먼저 만든다.
    public static string BuildItems()
    {
        StringBuilder report = new StringBuilder("[동굴 몬스터 아이템 만들기]\n");

        try
        {
            EnsureFolders();
            EditorUtility.DisplayProgressBar("Project U 동굴 몬스터", "모델", 0.15f);
            report.AppendLine($"몬스터 · 전리품 · 수정 장비 모델 {EnsureModels()}종 준비");

            EditorUtility.DisplayProgressBar("Project U 동굴 몬스터", "아이템", 0.45f);
            Dictionary<string, ItemData> items = CreateItems();
            report.AppendLine($"동굴 전리품 · 수정 장비 아이템 {items.Count}개");
            report.AppendLine(UpdateKoreanNames());
            report.AppendLine(RegisterItems(items));
            AssetDatabase.SaveAssets();
            ItemKoreanNameBuilder.Apply();

            EditorUtility.DisplayProgressBar("Project U 동굴 몬스터", "판매 가격 · 외형", 0.8f);
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

    // ---------------------------------------------------------------- 만들기 (2단계 : 몬스터 · 보스)

    public static string BuildAll(bool saveScene)
    {
        StringBuilder report = new StringBuilder("[동굴 몬스터 · 보스 만들기]\n");

        try
        {
            EnsureFolders();
            EditorUtility.DisplayProgressBar("Project U 동굴 몬스터", "모델 · 아이템", 0.05f);
            EnsureModels();
            Dictionary<string, ItemData> items = CreateItems();
            report.AppendLine($"동굴 전리품 · 수정 장비 아이템 {items.Count}개");
            report.AppendLine(UpdateKoreanNames());

            EditorUtility.DisplayProgressBar("Project U 동굴 몬스터", "제작법", 0.15f);
            int crafts = Crafts.Count(spec => CreateCraft(spec) != null);
            report.AppendLine($"수정 장비 제작법 {crafts}개 (수정 검 · 수정 갑옷)");

            EditorUtility.DisplayProgressBar("Project U 동굴 몬스터", "몬스터 Prefab", 0.35f);
            report.AppendLine(BuildMonsterAssets());

            EditorUtility.DisplayProgressBar("Project U 동굴 몬스터", "동굴에 몬스터 놓기", 0.6f);
            report.AppendLine(PlaceMonsters());

            EditorUtility.DisplayProgressBar("Project U 동굴 몬스터", "선물 · 의뢰", 0.8f);
            report.AppendLine(UpdateGiftTags());
            report.AppendLine(UpdateQuests());

            EditorUtility.DisplayProgressBar("Project U 동굴 몬스터", "아이템 등록 · 외형", 0.9f);
            report.AppendLine(RegisterItems(items));
            AssetDatabase.SaveAssets();
            ItemKoreanNameBuilder.Apply();
            MarketContentBuilder.RefreshCatalog();
            GameDataRegistryEditor.CreateOrRefreshDefaultRegistry();
            report.AppendLine(BuildVisuals("아이템 외형 · 아이콘 · 바닥 Prefab 생성"));
            NpcContentBuilder.BuildAll();
            NpcQuestBuilder.BuildAll();
            report.AppendLine("이웃 선물 취향 · 동굴 게시판 의뢰 갱신");

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
        foreach (string folder in new[] { ItemFolder, CraftingFolder, EnemyDataFolder, MonsterPrefabFolder, LootFolder })
        {
            StylizedArtAssetFactory.EnsureFolder(folder);
        }
    }

    private static int EnsureModels()
    {
        List<string> ids = Items.Select(spec => spec.Id).ToList();
        ids.AddRange(Monsters.Select(spec => spec.Model));
        ids.AddRange(new[] { "monster_crystal_shard", "cave_crystal_gate", "fx_crystal_bolt" });
        return ids.Distinct().Count(id => StylizedArtAssetFactory.GetOrCreateModelPrefab(id, true) != null);
    }

    private static string BuildVisuals(string okMessage) // 아이템이 늘면 줍기 Prefab 등록이 한 번에 맞지 않을 수 있어 한 번 더 돌린다
    {
        string visual = ItemVisualContentBuilder.BuildAll();

        if (visual.Contains("✗"))
        {
            visual = ItemVisualContentBuilder.BuildAll();
        }

        return visual.Split('\n').FirstOrDefault(line => line.StartsWith("✗")) ?? okMessage;
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
        serialized.FindProperty("equipmentSlotType").intValue = (int)spec.Slot;
        serialized.FindProperty("defensePercent").floatValue = spec.Defense;
        serialized.FindProperty("coldResistancePercent").floatValue = spec.Cold;
        serialized.FindProperty("maximumHealthBonus").floatValue = spec.MaximumHealthBonus;

        if (spec.Damage > 0f) // 수정 검
        {
            serialized.FindProperty("weaponAttackType").intValue = (int)WeaponAttackType.Melee;
            serialized.FindProperty("baseDamage").floatValue = spec.Damage;
            serialized.FindProperty("attackCooldown").floatValue = 0.62f;
            serialized.FindProperty("attackRange").floatValue = 2.4f;
            serialized.FindProperty("attackRadius").floatValue = 0.45f;
            serialized.FindProperty("staminaCost").floatValue = 6f;
            serialized.FindProperty("impactForce").floatValue = 3f;
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(item);
        return item;
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
        serialized.FindProperty("resultQuantity").intValue = 1;
        SerializedProperty ingredients = serialized.FindProperty("ingredients");
        (string id, int amount)[] used = spec.Ingredients.Where(entry => FindItem(entry.id) != null).ToArray();
        ingredients.arraySize = used.Length;

        for (int index = 0; index < used.Length; index++)
        {
            ingredients.GetArrayElementAtIndex(index).FindPropertyRelative("itemData").objectReferenceValue = FindItem(used[index].id);
            ingredients.GetArrayElementAtIndex(index).FindPropertyRelative("amount").intValue = used[index].amount;
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(recipe);
        return recipe;
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
            (CaveTrophyTag, "resource_bat_wing", "동굴 전리품"),
            (CaveTrophyTag, "resource_venom_fang", "동굴 전리품"),
            (CaveTrophyTag, "resource_crystal_heart", "동굴 전리품")
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

        return $"선물 태그 {tags}개 · 이웃 취향 {gifts}개 추가 (동굴 전리품 · 수정 심장)";
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
            File.WriteAllLines(QuestsPath, lines, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(QuestsPath);
        }

        return $"동굴 게시판 의뢰 {added}개 추가 (라그 박쥐 날개 · 사피라 독니)";
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
