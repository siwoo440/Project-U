using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 102일차: 2차 NPC 가게 · 제작 한 번에 갱신 (104일차: 3차 마리엘 · 알리우네 물건 추가, 폴더는 Day102 그대로)
// 1. NPC 가게 물건(102일차 7종 + 104일차 4종) 아이템 데이터를 만들고 ItemDatabase에 등록
// 2. 1번(아이템 외형 : 모델 · 아이콘 · 바닥용 Prefab · 손 외형) → 판매 상자 가격표 → 10번(NPC 상점 · 제작 주문) 순서로 실행하고 게임 Scene을 저장
// 여러 번 실행해도 같은 Asset을 갱신한다.
public static class NpcGoodsBuilder
{
    private const string ScenePath = "Assets/_ProjectU/Scenes/20_Gameplay.unity";
    private const string DialogTitle = "Project U 섬 NPC 가게";
    public const string ItemFolder = "Assets/_ProjectU/Data/Items/Day102";
    private const string ItemDatabasePath = "Assets/_ProjectU/Data/Databases/ItemDatabase.asset";

    public sealed class GoodsSpec
    {
        public string AssetName;
        public string Id;
        public string Owner;
        public string DisplayName;
        public string Description;
        public ItemCategory Category;
        public float Hunger;
        public float FoodThirst;
        public float FoodHealth;
        public FoodBuffType Buff;
        public float BuffStrength;
        public float BuffDuration;
        public float Health;
        public int Stack;
    }

    public static readonly GoodsSpec[] Goods =
    {
        new GoodsSpec { AssetName = "ItemData_VitalityPotion", Id = "item_vitality_potion", Owner = "char_bellamorta", DisplayName = "VITALITY POTION", Category = ItemCategory.Food,
            FoodHealth = 45f, Buff = FoodBuffType.StaminaRecovery, BuffStrength = 40f, BuffDuration = 240f, Stack = 5,
            Description = "Bellamorta's rose-red tonic. Heals wounds and keeps your stamina coming back fast." },
        new GoodsSpec { AssetName = "ItemData_SpiderSilk", Id = "item_spider_silk", Owner = "char_arachne", DisplayName = "SPIDER SILK", Category = ItemCategory.CraftingMaterial,
            Stack = 50, Description = "Fine silk thread spun by Arachne. Strong and light, perfect for clothes and bags." },
        new GoodsSpec { AssetName = "ItemData_SweetJelly", Id = "food_sweet_jelly", Owner = "char_milu", DisplayName = "SWEET JELLY", Category = ItemCategory.Food,
            Hunger = 8f, FoodThirst = 25f, Buff = FoodBuffType.MoveSpeed, BuffStrength = 8f, BuffDuration = 180f, Stack = 10,
            Description = "Milu's wobbly fruit jelly. Quenches thirst and puts a spring in your step." },
        new GoodsSpec { AssetName = "ItemData_InariSushi", Id = "food_inari_sushi", Owner = "char_kasumi", DisplayName = "INARI SUSHI", Category = ItemCategory.Food,
            Hunger = 30f, Buff = FoodBuffType.Warmth, BuffStrength = 30f, BuffDuration = 240f, Stack = 10,
            Description = "Sweet fried tofu stuffed with rice, left over from the shrine offering. Warms you from inside." },
        new GoodsSpec { AssetName = "ItemData_Antidote", Id = "medicine_antidote", Owner = "char_seira", DisplayName = "ANTIDOTE", Category = ItemCategory.Medicine,
            Health = 40f, Stack = 5, Description = "Seira's bitter green remedy, brewed from forest mushrooms. Restores health." },
        new GoodsSpec { AssetName = "ItemData_ScrapParts", Id = "item_scrap_parts", Owner = "char_pipi", DisplayName = "SCRAP PARTS", Category = ItemCategory.CraftingMaterial,
            Stack = 50, Description = "Gears, bolts and copper bits sorted by Pipi. Tinkerers turn them into tools." },
        new GoodsSpec { AssetName = "ItemData_DesertSalve", Id = "medicine_desert_salve", Owner = "char_safira", DisplayName = "DESERT SALVE", Category = ItemCategory.Medicine,
            Health = 55f, Stack = 5, Description = "Safira's cooling ointment from oasis herbs. Heals deep wounds quickly." },
        // 104일차: 3차 NPC (마리엘 가게 · 알리우네 제작)
        new GoodsSpec { AssetName = "ItemData_Pearl", Id = "item_pearl", Owner = "char_marielle", DisplayName = "PEARL", Category = ItemCategory.CraftingMaterial,
            Stack = 20, Description = "A lustrous pearl Marielle found under the tide rocks. Collectors pay well for it." },
        new GoodsSpec { AssetName = "ItemData_SeaweedSalad", Id = "food_seaweed_salad", Owner = "char_marielle", DisplayName = "SEAWEED SALAD", Category = ItemCategory.Food,
            Hunger = 18f, FoodThirst = 10f, Buff = FoodBuffType.Satiety, BuffStrength = 25f, BuffDuration = 240f, Stack = 10,
            Description = "Crunchy seaweed tossed with sesame. Light, salty, and keeps hunger away for a while." },
        new GoodsSpec { AssetName = "ItemData_GrilledClams", Id = "food_grilled_clams", Owner = "char_marielle", DisplayName = "GRILLED CLAMS", Category = ItemCategory.Food,
            Hunger = 28f, FoodHealth = 10f, Buff = FoodBuffType.StaminaRecovery, BuffStrength = 25f, BuffDuration = 180f, Stack = 10,
            Description = "Clams grilled in their shells with a drop of butter. Warm, savory and restoring." },
        new GoodsSpec { AssetName = "ItemData_FlowerBalm", Id = "medicine_flower_balm", Owner = "char_aliune", DisplayName = "FLOWER BALM", Category = ItemCategory.Medicine,
            Health = 35f, Stack = 5, Description = "Aliune's soft pink balm pressed from healing petals. Soothes scrapes and bruises." }
    };

    public static string BuildAll(bool saveScene)
    {
        StringBuilder report = new StringBuilder("[섬 NPC 가게 갱신]\n");

        if (EditorSceneManager.GetActiveScene().path != ScenePath)
        {
            report.AppendLine($"✗ 게임 Scene({ScenePath})을 열고 다시 실행하세요.");
            return report.ToString();
        }

        report.AppendLine(CreateItems());
        report.AppendLine(Result("1. 아이템 외형", ItemVisualContentBuilder.BuildAll()));
        report.AppendLine("판매 상자 가격표 : " + MarketContentBuilder.RefreshCatalog().Replace("\n", " "));
        report.AppendLine(Result("10. NPC 상점 · 제작 주문", NpcShopBuilder.BuildAll()));
        GameDataRegistryEditor.CreateOrRefreshDefaultRegistry();

        if (saveScene)
        {
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            report.AppendLine("게임 Scene 저장 완료");
        }

        report.Append(Validate(out _));
        return report.ToString();
    }

    private static string Result(string label, string report)
    {
        List<string> errors = report.Split('\n').Where(line => line.StartsWith("✗")).Select(line => line.TrimEnd('\r')).ToList();
        int failed = report.Split('\n').Count(line => line.StartsWith("결과") && !line.Contains("오류 0개"));
        return errors.Count == 0 && failed == 0 ? $"{label} : 완료" : $"{label} : ✗ 문제 {Math.Max(errors.Count, failed)}개\n{string.Join("\n", errors)}";
    }

    // ---------------------------------------------------------------- 아이템

    private static string CreateItems()
    {
        StylizedArtAssetFactory.EnsureFolder(ItemFolder);
        ItemDatabase database = AssetDatabase.LoadAssetAtPath<ItemDatabase>(ItemDatabasePath);

        if (database == null)
        {
            return $"✗ ItemDatabase를 찾지 못했습니다: {ItemDatabasePath}";
        }

        SerializedObject databaseObject = new SerializedObject(database);
        SerializedProperty list = databaseObject.FindProperty("items");
        int added = 0;

        foreach (GoodsSpec spec in Goods)
        {
            string path = $"{ItemFolder}/{spec.AssetName}.asset";
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);

            if (item == null)
            {
                item = ScriptableObject.CreateInstance<ItemData>();
                AssetDatabase.CreateAsset(item, path);
            }

            SerializedObject serialized = new SerializedObject(item);
            serialized.FindProperty("itemId").stringValue = spec.Id;
            serialized.FindProperty("displayName").stringValue = spec.DisplayName;
            serialized.FindProperty("description").stringValue = spec.Description;
            serialized.FindProperty("itemCategory").intValue = (int)spec.Category;
            serialized.FindProperty("toolType").intValue = (int)ToolType.None;
            serialized.FindProperty("weaponAttackType").intValue = (int)WeaponAttackType.None;
            serialized.FindProperty("hungerRestoreAmount").floatValue = spec.Hunger;
            serialized.FindProperty("foodThirstRestoreAmount").floatValue = spec.FoodThirst;
            serialized.FindProperty("foodHealthRestoreAmount").floatValue = spec.FoodHealth;
            serialized.FindProperty("foodBuffType").intValue = (int)spec.Buff;
            serialized.FindProperty("foodBuffStrength").floatValue = spec.BuffStrength;
            serialized.FindProperty("foodBuffDuration").floatValue = spec.BuffDuration;
            serialized.FindProperty("thirstRestoreAmount").floatValue = 0f;
            serialized.FindProperty("healthRestoreAmount").floatValue = spec.Health;
            serialized.FindProperty("maximumStack").intValue = spec.Stack;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(item);

            if (!Enumerable.Range(0, list.arraySize).Any(index => list.GetArrayElementAtIndex(index).objectReferenceValue == item))
            {
                list.arraySize++;
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = item;
                added++;
            }
        }

        databaseObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
        return $"NPC 가게 물건 {Goods.Length}종 ({ItemFolder}), ItemDatabase 추가 {added}개";
    }

    // ---------------------------------------------------------------- 검증

    // 물건 7종 : 아이템 데이터 · ItemDatabase · 판매 가격 · 아이콘 · 주인 가게에서 팔거나 만들어 줌
    public static string Validate(out int errorCount)
    {
        StringBuilder report = new StringBuilder("[섬 NPC 가게 물건 검증]\n");
        int errors = 0;

        void Error(string message)
        {
            errors++;
            report.AppendLine("✗ " + message);
        }

        ItemDatabase database = AssetDatabase.LoadAssetAtPath<ItemDatabase>(ItemDatabasePath);
        MarketCatalogData catalog = AssetDatabase.LoadAssetAtPath<MarketCatalogData>(MarketContentBuilder.CatalogPath);
        List<NpcShopData> shops = NpcShopBuilder.LoadShops();
        List<NpcCraftBook> books = NpcShopBuilder.LoadCraftBooks();
        HashSet<ItemData> registered = new HashSet<ItemData>();

        if (database != null)
        {
            SerializedProperty list = new SerializedObject(database).FindProperty("items");

            for (int index = 0; index < list.arraySize; index++)
            {
                if (list.GetArrayElementAtIndex(index).objectReferenceValue is ItemData item)
                {
                    registered.Add(item);
                }
            }
        }

        int ready = 0;

        foreach (GoodsSpec spec in Goods)
        {
            int before = errors;
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>($"{ItemFolder}/{spec.AssetName}.asset");

            if (item == null || item.ItemId != spec.Id)
            {
                Error($"{spec.Id} : 아이템 데이터가 없습니다. 20번 메뉴를 실행하세요.");
                continue;
            }

            if (!registered.Contains(item))
            {
                Error($"{spec.Id} : ItemDatabase에 없습니다.");
            }

            if (catalog == null || !catalog.TryGetPrice(item, out _))
            {
                Error($"{spec.Id} : 판매 상자 가격표에 없습니다.");
            }

            if (item.Icon == null)
            {
                Error($"{spec.Id} : 아이콘이 없습니다. 20번 메뉴를 실행하세요.");
            }

            bool sold = shops.Any(shop => shop.OwnerId == spec.Owner && shop.Stock.Any(entry => entry?.Item == item));
            bool crafted = books.Any(book => book.OwnerId == spec.Owner && book.Orders.Any(order => order?.Result == item));

            if (!sold && !crafted)
            {
                Error($"{spec.Id} : 주인 {spec.Owner} 의 가게에서 팔지도 만들어 주지도 않습니다.");
            }

            ready += errors == before ? 1 : 0;
            report.AppendLine($"{spec.Id} ({spec.Owner}) : {(sold ? "판매" : "-")} · {(crafted ? "제작" : "-")}");
        }

        report.AppendLine($"물건 준비 {ready}/{Goods.Length}");
        errorCount = errors;
        report.AppendLine(errors == 0 ? "결과 : 오류 0개" : $"결과 : 오류 {errors}개");
        return report.ToString();
    }
}
