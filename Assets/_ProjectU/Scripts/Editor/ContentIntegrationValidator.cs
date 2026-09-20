using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// 88일차: 생활 콘텐츠 통합 점검
// 1. 기능별 검사(아이템 외형 · 농사 · 낚시 · 요리 · 가축 · 판매)와 아이템 데이터 · Registry 검사를 한 번에 실행
// 2. 기능 사이 연결 검사 : 생산물 판매 가격, 요리 재료를 구하는 곳, 씨앗 판매, 저장 목록, 날짜 처리 관리자
// 89일차: NPC 검사와 NPC 선물을 구하는 곳 검사 추가
// 92일차: NPC 상점 검사 추가, NPC 상점 판매 물건을 구하는 곳에 포함
// 93일차: NPC 의뢰 검사 추가, 의뢰에 필요한 물건을 구할 수 있는지 검사
// 94일차: NPC 이벤트 검사 추가
// 95일차: 밸런스 검사(활동별 수입 · 반복 사냥 · 의뢰 보상 · 호감도 속도) 추가
// 96일차: 데이터 점검(외형 연결 · ID · 외형 설정 · 깨진 참조 · 쓰이지 않는 데이터) 추가
// 97일차: 무기 외형(도끼 · 활 3인칭 · 1인칭) 검사 추가
// 98일차: 건축물 · 채집 자원 외형 설정(카드) 검사 추가
// 99일차: 메뉴 화면(설정 창 · 로딩 화면 · 메인 메뉴 · 빌드 Scene 목록) 검사 추가
public static class ContentIntegrationValidator
{
    private const string DialogTitle = "Project U 전체 콘텐츠 검사";
    private const string ItemDatabasePath = "Assets/_ProjectU/Data/Databases/ItemDatabase.asset";
    private const string PickupRegistryPath = "Assets/_ProjectU/Prefabs/Items/Day75/WorldItemPickupRegistry_Day75.asset";

    public static string ValidateAll(out int errorCount, out int warningCount)
    {
        StringBuilder report = new StringBuilder("[전체 콘텐츠 검사]\n");
        int errors = 0;
        int warnings = 0;

        // ------------------------------------------------------------ 1. 기능별 검사
        report.AppendLine("[1. 기능별 검사]");
        List<string> details = new List<string>();
        Func<int> Feature(ValidateMethod validate) => () =>
        {
            string featureReport = validate(out int count);

            // 기능별 검사의 오류 줄(✗)을 그대로 옮겨 어떤 항목이 문제인지 바로 보이게 한다
            foreach (string line in featureReport.Split('\n'))
            {
                if (line.StartsWith("✗"))
                {
                    details.Add(line.TrimEnd('\r'));
                }
            }

            return count;
        };

        (string label, Func<int> run)[] checks =
        {
            ("아이템 외형", Feature(ItemVisualContentBuilder.Validate)),
            ("농사", Feature(FarmingContentBuilder.Validate)),
            ("낚시", Feature(FishingContentBuilder.Validate)),
            ("요리", Feature(CookingContentBuilder.Validate)),
            ("가축", Feature(LivestockContentBuilder.Validate)),
            ("판매·상점", Feature(MarketContentBuilder.Validate)),
            ("NPC", Feature(NpcContentBuilder.Validate)),
            ("NPC 마을 배치", Feature(NpcPlacementBuilder.Validate)),
            ("NPC 대화", Feature(NpcDialogueBuilder.Validate)),
            ("NPC 상점", Feature(NpcShopBuilder.Validate)),
            ("NPC 의뢰", Feature(NpcQuestBuilder.Validate)),
            ("NPC 이벤트", Feature(NpcEventBuilder.Validate)),
            ("밸런스", Feature(BalanceBuilder.Validate)),
            ("무기 외형", Feature(WeaponViewBuilder.Validate)),
            ("건축물·자원 외형", Feature(BuildableVisualProfileBuilder.Validate)),
            ("메뉴 화면", Feature(MenuSceneBuilder.Validate)),
            ("새 구역 · 특수 체형", Feature(WorldZoneBuilder.Validate)),
            ("섬 NPC 차수", Feature(NpcCastBuilder.Validate)),
            ("섬 NPC 가게 물건", Feature(NpcGoodsBuilder.Validate)),
            ("섬 NPC 이야기", Feature(NpcStoryBuilder.Validate)),
            ("무인도", Feature(IslandTerrainBuilder.Validate)),
            ("수영 · 잠수", Feature(SwimmingBuilder.Validate)),
            ("섬 자연 · 들판", Feature(IslandNatureBuilder.Validate)),
            ("NPC 동료", Feature(NpcCompanionBuilder.Validate)),
            ("NPC 관계", Feature(NpcRelationBuilder.Validate)),
            ("아이템 한글 이름", Feature(ItemKoreanNameBuilder.Validate)),
            ("115일차 통합 점검", Feature(IntegrationPolishBuilder.Validate)),
            ("동굴", Feature(CaveBuilder.Validate)),
            ("광물 · 제련 · 밤의 운석", Feature(MineralBuilder.Validate)),
            ("데이터 점검", Feature(DataAuditBuilder.Validate)),
            ("아이템 데이터 (ID 규칙)", ItemDataValidator.ValidateAllItemData),
            ("Game Data Registry", () => CountLoggedErrors(GameDataRegistryEditor.ValidateDefaultRegistry, details))
        };

        foreach ((string label, Func<int> run) in checks)
        {
            int count;
            details.Clear();

            try
            {
                count = run();
            }
            catch (Exception exception)
            {
                count = 1;
                Debug.LogException(exception);
            }

            errors += count;
            report.AppendLine(count == 0 ? $"  {label} : 문제 없음" : $"  · {label} : 오류 {count}개" + (details.Count > 0 ? string.Empty : " (자세한 내용은 Console)"));

            foreach (string detail in details)
            {
                report.AppendLine($"  · {label} {detail}");
            }
        }

        // ------------------------------------------------------------ 2. 생활 콘텐츠 연결 검사
        report.AppendLine("[2. 생활 콘텐츠 연결 검사]");
        LifeLinks links = new LifeLinks(report);
        links.Run();
        errors += links.Errors;
        warnings += links.Warnings;

        errorCount = errors;
        warningCount = warnings;
        report.Append(errors == 0 && warnings == 0 ? "결과 : 문제 없음" : $"결과 : 오류 {errors}개, 경고 {warnings}개");
        return report.ToString();
    }

    private delegate string ValidateMethod(out int errorCount);

    private static int CountLoggedErrors(Action action, List<string> details)
    {
        int count = 0;
        void Listen(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception)
            {
                count++;
                details.Add("✗ " + condition.Split('\n')[0]); // 오류 메시지 첫 줄만 옮긴다
            }
        }

        Application.logMessageReceived += Listen;

        try
        {
            action();
        }
        finally
        {
            Application.logMessageReceived -= Listen;
        }

        return count;
    }

    // ---------------------------------------------------------------- 생활 콘텐츠 연결

    private sealed class LifeLinks
    {
        private readonly StringBuilder report;
        private readonly List<ItemData> items = new List<ItemData>();
        private readonly List<CropData> crops = new List<CropData>();
        private readonly List<FishData> fish = new List<FishData>();
        private readonly List<AnimalData> animals = new List<AnimalData>();
        private readonly List<CookingRecipeData> cookingRecipes = new List<CookingRecipeData>();
        private readonly List<CraftingRecipeData> craftingRecipes = new List<CraftingRecipeData>();
        private readonly Dictionary<ItemData, string> sources = new Dictionary<ItemData, string>();
        private MarketCatalogData catalog;

        public int Errors { get; private set; }
        public int Warnings { get; private set; }

        public LifeLinks(StringBuilder report)
        {
            this.report = report;
        }

        private void Error(string message)
        {
            Errors++;
            report.AppendLine("  · [오류] " + message);
        }

        private void Warning(string message)
        {
            Warnings++;
            report.AppendLine("  · [경고] " + message);
        }

        private void Ok(string message)
        {
            report.AppendLine("  " + message);
        }

        public void Run()
        {
            Load(items);
            Load(crops);
            Load(fish);
            Load(animals);
            Load(cookingRecipes);
            Load(craftingRecipes);
            catalog = AssetDatabase.LoadAssetAtPath<MarketCatalogData>(MarketContentBuilder.CatalogPath);
            CollectSources();
            CheckProductsSellable();
            CheckCookingIngredients();
            CheckSeeds();
            CheckItemRegistration();
            CheckNpcGifts();
            CheckNpcQuests();
            CheckScene();
        }

        private static void Load<T>(List<T> target) where T : ScriptableObject
        {
            foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { "Assets/_ProjectU/Data" }))
            {
                T asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));

                if (asset != null)
                {
                    target.Add(asset);
                }
            }
        }

        private void AddSource(ItemData item, string source)
        {
            if (item != null && !sources.ContainsKey(item))
            {
                sources.Add(item, source);
            }
        }

        // 아이템을 얻을 수 있는 곳 : 수확 · 낚시 · 가축 · 요리 · 제작 · 상인 · 채집 자원 · 월드 아이템 · 적 전리품
        private void CollectSources()
        {
            foreach (CropData crop in crops) AddSource(crop.HarvestItem, "수확");
            foreach (FishData entry in fish) AddSource(entry.ResultItem, "낚시");
            foreach (AnimalData animal in animals) AddSource(animal.ProductItem, "가축");
            foreach (CookingRecipeData recipe in cookingRecipes) AddSource(recipe.ResultItem, "요리");
            foreach (CraftingRecipeData recipe in craftingRecipes) AddSource(recipe.ResultItem, "제작");

            if (catalog != null)
            {
                foreach (MarketStockEntry entry in catalog.Stock) AddSource(entry?.Item, "상인");
            }

            foreach (NpcShopData shop in NpcShopBuilder.LoadShops())
            {
                foreach (NpcShopData.StockEntry entry in shop.Stock) AddSource(entry?.Item, "NPC 상점");
            }

            foreach (string guid in AssetDatabase.FindAssets("t:EnemyLootTable", new[] { "Assets/_ProjectU" }))
            {
                EnemyLootTable table = AssetDatabase.LoadAssetAtPath<EnemyLootTable>(AssetDatabase.GUIDToAssetPath(guid));

                if (table != null)
                {
                    foreach (EnemyLootTable.LootEntry entry in table.Entries) AddSource(entry?.ItemData, "적 전리품");
                }
            }

            foreach (GatherableResource resource in Object.FindObjectsByType<GatherableResource>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                AddSource(new SerializedObject(resource).FindProperty("resourceItem").objectReferenceValue as ItemData, "채집");
            }

            MeteorEventManager meteor = Object.FindFirstObjectByType<MeteorEventManager>(FindObjectsInactive.Include); // 117일차: 밤에 떨어지는 운석

            if (meteor != null && meteor.MeteorRockPrefab != null)
            {
                GatherableResource meteorRock = meteor.MeteorRockPrefab.GetComponent<GatherableResource>();

                if (meteorRock != null)
                {
                    AddSource(new SerializedObject(meteorRock).FindProperty("resourceItem").objectReferenceValue as ItemData, "밤의 운석");
                }
            }

            foreach (WorldItemPickup pickup in Object.FindObjectsByType<WorldItemPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (pickup.gameObject.scene.IsValid())
                {
                    AddSource(pickup.ItemData, "월드 아이템");
                }
            }
        }

        // 생활 생산물(수확 · 물고기 · 가축 · 요리)은 판매 상자에서 팔 수 있어야 한다
        private void CheckProductsSellable()
        {
            if (catalog == null)
            {
                Error("판매 가격표가 없습니다. Build Content > 6. Market을 실행하세요.");
                return;
            }

            List<(ItemData item, string kind)> products = new List<(ItemData, string)>();
            foreach (CropData crop in crops) products.Add((crop.HarvestItem, "작물"));
            foreach (FishData entry in fish) products.Add((entry.ResultItem, "물고기"));
            foreach (AnimalData animal in animals) products.Add((animal.ProductItem, "가축 생산물"));
            foreach (CookingRecipeData recipe in cookingRecipes) products.Add((recipe.ResultItem, "요리"));
            int sellable = 0;

            foreach ((ItemData item, string kind) in products)
            {
                if (item == null)
                {
                    Error($"{kind} 결과 아이템이 비어 있습니다.");
                    continue;
                }

                if (!catalog.TryGetPrice(item, out _))
                {
                    Error($"{kind} {item.ItemId} : 판매 가격이 없습니다. (Build Content > 6. Market 다시 실행)");
                    continue;
                }

                sellable++;
            }

            Ok($"생산물 판매 가격 : {sellable}/{products.Count}개 (작물 {crops.Count} · 물고기 {fish.Count} · 가축 {animals.Count} · 요리 {cookingRecipes.Count})");
        }

        // 요리 재료는 모두 어딘가에서 얻을 수 있어야 한다
        private void CheckCookingIngredients()
        {
            HashSet<ItemData> ingredients = new HashSet<ItemData>();

            foreach (CookingRecipeData recipe in cookingRecipes)
            {
                foreach (CraftingIngredient ingredient in recipe.Ingredients)
                {
                    if (ingredient?.ItemData != null)
                    {
                        ingredients.Add(ingredient.ItemData);
                    }
                }
            }

            List<string> found = new List<string>();

            foreach (ItemData ingredient in ingredients)
            {
                if (sources.TryGetValue(ingredient, out string source))
                {
                    found.Add($"{ingredient.DisplayName}({source})");
                }
                else
                {
                    Warning($"요리 재료 {ingredient.ItemId} 를 얻는 곳을 찾지 못했습니다.");
                }
            }

            Ok($"요리 재료 {found.Count}/{ingredients.Count}종 구하는 곳 확인");

            foreach (AnimalData animal in animals)
            {
                if (animal.FeedItem == null || !sources.ContainsKey(animal.FeedItem))
                {
                    Error($"{animal.AnimalId} : 먹이를 얻는 곳(제작·상인)이 없습니다.");
                }
            }
        }

        // 89일차: NPC가 매우 좋아하는 선물을 게임 안에서 구할 수 있어야 한다
        private void CheckNpcGifts()
        {
            NpcDatabase database = AssetDatabase.LoadAssetAtPath<NpcDatabase>(NpcContentBuilder.DatabasePath);

            if (database == null)
            {
                Error("NpcDatabase가 없습니다.");
                return;
            }

            int obtainable = 0;

            foreach (NpcCharacterData character in database.Characters)
            {
                if (character == null || character.GiftProfile == null)
                {
                    continue;
                }

                bool found = false;

                foreach (NpcGiftProfile.Entry entry in character.GiftProfile.Entries)
                {
                    if (entry?.Item == null || entry.Preference != GiftPreference.Loved)
                    {
                        continue;
                    }

                    if (sources.ContainsKey(entry.Item))
                    {
                        found = true;
                    }
                    else if (character.IsPlaced)
                    {
                        Warning($"{character.CharacterId} : 매우 좋아하는 선물 {entry.Item.ItemId} 를 얻는 곳을 찾지 못했습니다.");
                    }
                }

                if (found)
                {
                    obtainable++;
                }
                else if (character.IsPlaced)
                {
                    Error($"{character.CharacterId} : 매우 좋아하는 선물을 게임 안에서 구할 수 없습니다.");
                }
                else
                {
                    Warning($"{character.CharacterId} : 매우 좋아하는 선물을 게임 안에서 구할 수 없습니다.");
                }
            }

            Ok($"NPC {database.Characters.Count}명 중 {obtainable}명 : 매우 좋아하는 선물을 구할 수 있음");
        }

        // 93일차: 의뢰에 필요한 물건과 보상 아이템은 게임 안에서 구할 수 있어야 한다
        private void CheckNpcQuests()
        {
            int quests = 0;
            int items = 0;

            foreach (NpcQuestBook book in NpcQuestBuilder.LoadBooks())
            {
                foreach (NpcQuestBook.Quest quest in book.Quests)
                {
                    if (quest == null)
                    {
                        continue;
                    }

                    quests++;

                    foreach (NpcQuestBook.Requirement requirement in quest.Requirements)
                    {
                        if (requirement?.Item == null)
                        {
                            continue;
                        }

                        items++;

                        if (!sources.ContainsKey(requirement.Item))
                        {
                            Error($"{quest.QuestId} : 필요 물건 {requirement.Item.ItemId} 를 얻는 곳을 찾지 못했습니다.");
                        }
                    }
                }
            }

            Ok($"NPC 의뢰 {quests}개 : 필요 물건 {items}칸 구하는 곳 확인");
        }

        // 작물마다 씨앗을 살 수 있고, 계절마다 심을 작물이 있어야 한다
        private void CheckSeeds()
        {
            bool[] seasonHasCrop = new bool[4];

            foreach (CropData crop in crops)
            {
                foreach (SeasonType season in crop.GrowingSeasons)
                {
                    seasonHasCrop[(int)season] = true;
                }

                if (crop.SeedItem == null)
                {
                    Error($"{crop.CropId} : 씨앗 아이템이 없습니다.");
                    continue;
                }

                bool sold = false;

                if (catalog != null)
                {
                    foreach (MarketStockEntry entry in catalog.Stock)
                    {
                        if (entry == null || entry.Item != crop.SeedItem)
                        {
                            continue;
                        }

                        foreach (SeasonType season in crop.GrowingSeasons)
                        {
                            sold |= entry.IsSoldIn(season);
                        }
                    }
                }

                if (!sold)
                {
                    Warning($"{crop.CropId} : 재배 계절에 상인이 씨앗을 팔지 않습니다.");
                }
            }

            for (int season = 0; season < 4; season++)
            {
                if (!seasonHasCrop[season])
                {
                    Warning($"{(SeasonType)season} 에 심을 수 있는 작물이 없습니다.");
                }
            }

            Ok($"작물 {crops.Count}종 : 씨앗 판매 · 계절별 작물 확인");
        }

        // 저장·바닥 드롭에 필요한 등록
        private void CheckItemRegistration()
        {
            ItemDatabase database = AssetDatabase.LoadAssetAtPath<ItemDatabase>(ItemDatabasePath);
            WorldItemPickupRegistry registry = AssetDatabase.LoadAssetAtPath<WorldItemPickupRegistry>(PickupRegistryPath);
            int missingDatabase = 0;
            int missingPickup = 0;

            foreach (ItemData item in items)
            {
                if (database == null || !database.TryGetItem(item.ItemId, out ItemData registered) || registered != item)
                {
                    Error($"{item.ItemId} : ItemDatabase에 없어 저장 파일에서 복원할 수 없습니다.");
                    missingDatabase++;
                }

                if (registry == null || !registry.TryGetPickup(item, out _))
                {
                    Warning($"{item.ItemId} : 바닥에 떨어뜨릴 Prefab이 없습니다.");
                    missingPickup++;
                }
            }

            Ok($"아이템 {items.Count}종 : 저장 등록 누락 {missingDatabase}, 바닥 드롭 누락 {missingPickup}");
        }

        private void CheckScene()
        {
            GameUIManager uiManager = Object.FindFirstObjectByType<GameUIManager>(FindObjectsInactive.Include);

            if (uiManager == null)
            {
                Ok("[건너뜀] 게임 Scene(20_Gameplay)이 열려 있지 않아 Scene 연결 검사를 생략했습니다.");
                return;
            }

            // 건축 목록과 건축물 저장 목록이 같아야 불러오기에서 건축물이 사라지지 않는다
            BuildPlacementController build = Object.FindFirstObjectByType<BuildPlacementController>(FindObjectsInactive.Include);
            PlacedStructureSaveBridge structureSave = Object.FindFirstObjectByType<PlacedStructureSaveBridge>(FindObjectsInactive.Include);
            HashSet<Object> buildList = ReadList(build, "buildRecipes");
            HashSet<Object> saveList = ReadList(structureSave, "buildRecipes");

            foreach (Object recipe in buildList)
            {
                if (!saveList.Contains(recipe))
                {
                    Error($"{recipe.name} : 건축 목록에는 있지만 건축물 저장 목록에 없습니다.");
                }
            }

            Ok($"건축 목록 {buildList.Count}개 · 저장 목록 {saveList.Count}개");

            // 날짜가 바뀔 때 함께 처리되는 관리자 (잠자기 · 자정)
            DayNightCycle cycle = Object.FindFirstObjectByType<DayNightCycle>(FindObjectsInactive.Include);
            (string label, Object target)[] managers =
            {
                ("낮밤 순환", cycle),
                ("수면", Object.FindFirstObjectByType<SleepSystem>(FindObjectsInactive.Include)),
                ("밭 관리자", Object.FindFirstObjectByType<FarmManager>(FindObjectsInactive.Include)),
                ("가축 관리자", Object.FindFirstObjectByType<LivestockManager>(FindObjectsInactive.Include)),
                ("상점 관리자", Object.FindFirstObjectByType<MarketManager>(FindObjectsInactive.Include)),
                ("저장 관리자", Object.FindFirstObjectByType<GameplaySaveController>(FindObjectsInactive.Include))
            };

            foreach ((string label, Object target) in managers)
            {
                if (target == null)
                {
                    Error($"Scene에 {label}가 없습니다.");
                }
            }

            foreach ((string label, Object target, string property) in new (string, Object, string)[]
            {
                ("가축 관리자", Object.FindFirstObjectByType<LivestockManager>(FindObjectsInactive.Include), "dayNightCycle"),
                ("상점 관리자", Object.FindFirstObjectByType<MarketManager>(FindObjectsInactive.Include), "dayNightCycle")
            })
            {
                if (target != null && new SerializedObject(target).FindProperty(property).objectReferenceValue != cycle)
                {
                    Error($"{label}가 다른 낮밤 순환을 보고 있습니다.");
                }
            }

            Ok("날짜 처리 : 낮밤 순환 · 수면 · 밭 · 가축 · 상점 · 저장 관리자 확인");

            // 창 연결
            if (uiManager.CookingPopup == null) Error("GameUIManager에 요리 창이 없습니다.");
            if (uiManager.AnimalPenPopup == null) Error("GameUIManager에 우리 창이 없습니다.");
            if (uiManager.ShopPopup == null) Error("GameUIManager에 상인 창이 없습니다.");
            Ok("창 연결 : 요리 · 우리 · 상인");

            // 저장 관리자 참조
            GameplaySaveController save = Object.FindFirstObjectByType<GameplaySaveController>(FindObjectsInactive.Include);
            int missing = 0;

            if (save != null)
            {
                SerializedProperty property = new SerializedObject(save).GetIterator();

                while (property.NextVisible(true))
                {
                    if (property.propertyType == SerializedPropertyType.ObjectReference && property.name != "m_Script" && property.objectReferenceValue == null)
                    {
                        Error($"저장 관리자 : {property.propertyPath} 연결 없음");
                        missing++;
                    }
                }
            }

            Ok($"저장 관리자 연결 누락 {missing}개");
        }

        private static HashSet<Object> ReadList(Object target, string propertyName)
        {
            HashSet<Object> result = new HashSet<Object>();

            if (target == null)
            {
                return result;
            }

            SerializedProperty list = new SerializedObject(target).FindProperty(propertyName);

            for (int index = 0; list != null && index < list.arraySize; index++)
            {
                Object value = list.GetArrayElementAtIndex(index).objectReferenceValue;

                if (value != null)
                {
                    result.Add(value);
                }
            }

            return result;
        }
    }
}
