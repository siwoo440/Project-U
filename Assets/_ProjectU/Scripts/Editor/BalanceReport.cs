using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// 95일차: 전체 콘텐츠 밸런스 계산 · 검사
// 1. 데이터(가격표 · 작물 · 물고기 · 가축 · 요리 · 적 · 채집 · 의뢰 · 호감도 규칙)로 활동별 수입과 성장 속도를 계산한다
// 2. 목표에서 크게 벗어난 값(반복 사냥 · 손해 보는 의뢰 · 너무 빠른 호감도 등)을 오류로 알린다
// 3. 계산 결과를 Docs/Balance/BalanceReport.md 표로 남긴다
// 시간은 모두 "실제 시간" 기준이다. (하루 = DayNightCycle 하루 길이, 기본 600초)
public static class BalanceReport
{
    public const string ReportPath = "Docs/Balance/BalanceReport.md";
    private const string AxeItemId = "tool_axe";
    private const float PlayerHealth = 100f;

    // ---------------------------------------------------------------- 밸런스 목표
    private const float CropBandLow = 0.7f; // 작물 하루 이익은 평균의 70% ~ 150%
    private const float CropBandHigh = 1.5f;
    private const float CombatIncomeLimit = 0.5f; // 적 사냥 수입은 낚시의 절반 이하
    private const float MinRespawnHours = 4f; // 적은 게임 시간 4시간 이상 지나야 다시 나온다
    private const int MinHitsToKill = 3; // 도끼로 3 ~ 12번에 쓰러뜨림
    private const int MaxHitsToKill = 12;
    private const int MinHitsToDownPlayer = 5; // 적은 플레이어를 5번 이상 때려야 쓰러뜨림
    private const float QuestRatioLow = 1f; // 게시판 의뢰 보상 코인은 필요 물건 판매가 이상
    private const float QuestRatioNote = 1.15f; // 이보다 낮으면 참고 표시
    private const int TalkOnlyCuriousMax = 28; // 대화만으로 한 계절 안에 호기심
    private const int LikedCuriousMin = 5; // 좋아하는 선물 : 호기심 5 ~ 14일, 신뢰 35일 안
    private const int LikedCuriousMax = 14;
    private const int LikedTrustMax = 35;
    private const int LovedCuriousMin = 4; // 매우 좋아하는 선물 + 이벤트 : 호기심 4일 이후, 신뢰 8일 이후, 애정 42일 안
    private const int LovedTrustMin = 8;
    private const int LovedAffectionMax = 42;
    private const int SimulationDays = 112;

    private static readonly SeasonType[] Seasons = { SeasonType.Spring, SeasonType.Summer, SeasonType.Autumn, SeasonType.Winter };
    private static readonly string[] SeasonNames = { "봄", "여름", "가을", "겨울" };

    // ---------------------------------------------------------------- 계산 결과

    public sealed class CropRow { public string Name; public string Season; public int Days; public int SeedCost; public float Harvest; public int Unit; public float Profit; public float PerDay; public float SeasonProfit; }
    public sealed class FishRow { public string Season; public int Kinds; public float Value; public float Seconds; public float PerMinute; }
    public sealed class CombatRow { public string Point; public string Enemy; public string Table; public bool TableMatches; public int Hits; public float KillSeconds; public int PlayerHits; public float Loot; public float Respawn; public float RespawnHours; public float PerMinute; }
    public sealed class GatherRow { public string Item; public int Nodes; public float Respawn; public float PerMinute; }
    public sealed class AnimalRow { public string Name; public float FeedCost; public float Product; public float Profit; public float LureCost; public float Payback; }
    public sealed class CookRow { public string Name; public float Ingredients; public float Result; public float Gain; }
    public sealed class QuestRow { public string Id; public string Owner; public bool Special; public string Season; public float Need; public float Reward; public float Ratio; }
    public sealed class RelationRow { public string Scenario; public int[] Min = new int[3]; public int[] Max = new int[3]; }
    public sealed class PurchaseRow { public string Name; public string Seller; public int Price; public float FishMinutes; public float FarmDays; }

    public sealed class Snapshot
    {
        public float DaySeconds = 600f;
        public int DaysPerSeason = 28;
        public float HourSeconds => DaySeconds / 24f;
        public bool SceneLoaded;
        public readonly List<CropRow> Crops = new List<CropRow>();
        public float CropAverage;
        public readonly List<FishRow> Fish = new List<FishRow>();
        public float FishPerMinute;
        public readonly List<CombatRow> Combat = new List<CombatRow>();
        public float CombatPerMinute;
        public readonly List<GatherRow> Gathering = new List<GatherRow>();
        public readonly List<AnimalRow> Animals = new List<AnimalRow>();
        public readonly List<CookRow> Cooking = new List<CookRow>();
        public readonly List<QuestRow> Quests = new List<QuestRow>();
        public readonly List<RelationRow> Relations = new List<RelationRow>();
        public readonly List<PurchaseRow> Purchases = new List<PurchaseRow>();
        public string GiftRule = string.Empty;
        public float HungerPerDay;
        public float ThirstPerDay;
        public string Survival = string.Empty;
        public readonly List<string> Errors = new List<string>();
        public readonly List<string> Notes = new List<string>();
    }

    // ---------------------------------------------------------------- 메뉴

    // ContentIntegrationValidator · 13번 메뉴에서 사용
    public static string Validate(out int errorCount)
    {
        Snapshot snapshot = Analyze();
        errorCount = snapshot.Errors.Count;
        return Summary(snapshot);
    }

    public static string Summary(Snapshot s)
    {
        StringBuilder report = new StringBuilder("[밸런스 검사]\n");
        report.AppendLine($"하루 {s.DaySeconds:0}초 (게임 1시간 = {s.HourSeconds:0.#}초) · 한 계절 {s.DaysPerSeason}일");

        if (s.Crops.Count > 0)
        {
            report.AppendLine($"농사 : 한 칸 하루 이익 평균 {s.CropAverage:0.0}코인 ({string.Join(" · ", s.Crops.Select(row => $"{row.Name} {row.PerDay:0.0}"))})");
        }

        report.AppendLine($"낚시 : 1분에 약 {s.FishPerMinute:0}코인 ({string.Join(" · ", s.Fish.Select(row => $"{row.Season} {row.PerMinute:0}"))})");
        report.AppendLine(s.SceneLoaded ? $"적 사냥 : 1분에 약 {s.CombatPerMinute:0.0}코인 (적 {s.Combat.Count}곳, 낚시의 {Percent(s.CombatPerMinute, s.FishPerMinute)})" : "적 사냥 · 채집 · 생존 : [건너뜀] 게임 Scene이 열려 있지 않습니다.");

        foreach (GatherRow row in s.Gathering)
        {
            report.AppendLine($"채집 {row.Item} : 한 곳 1분에 약 {row.PerMinute:0.0}코인 ({row.Nodes}곳)");
        }

        foreach (AnimalRow row in s.Animals)
        {
            report.AppendLine($"가축 {row.Name} : 하루 이익 {row.Profit:0.0}코인 (데려오기 {row.LureCost:0}코인 → {row.Payback:0.#}일에 회수)");
        }

        List<QuestRow> board = s.Quests.Where(row => !row.Special).ToList();

        if (board.Count > 0)
        {
            report.AppendLine($"게시판 의뢰 {board.Count}개 : 보상 ÷ 필요 물건 판매가 {board.Min(row => row.Ratio):0.00} ~ {board.Max(row => row.Ratio):0.00}배");
        }

        report.AppendLine($"호감도 ({s.GiftRule})");

        foreach (RelationRow row in s.Relations)
        {
            report.AppendLine($"  {row.Scenario} : 호기심 {Range(row, 0)} · 신뢰 {Range(row, 1)} · 애정 {Range(row, 2)}");
        }

        if (s.SceneLoaded)
        {
            report.AppendLine($"생존 : {s.Survival}");
        }

        foreach (string note in s.Notes)
        {
            report.AppendLine("△ " + note);
        }

        foreach (string error in s.Errors)
        {
            report.AppendLine("✗ " + error);
        }

        report.AppendLine(s.Errors.Count == 0 ? "결과 : 오류 0개" : $"결과 : 오류 {s.Errors.Count}개");
        return report.ToString();
    }

    private static string Range(RelationRow row, int stage)
    {
        if (row.Max[stage] < 0)
        {
            return row.Min[stage] < 0 ? $"{SimulationDays}일 안에 못 함" : $"{row.Min[stage]}일 ~ (일부 못 함)";
        }

        return row.Min[stage] == row.Max[stage] ? $"{row.Min[stage]}일" : $"{row.Min[stage]}~{row.Max[stage]}일";
    }

    private static string Percent(float value, float basis) => basis > 0f ? $"{value / basis * 100f:0}%" : "-";

    // ---------------------------------------------------------------- 계산

    public static Snapshot Analyze()
    {
        Snapshot s = new Snapshot();
        MarketCatalogData catalog = AssetDatabase.LoadAssetAtPath<MarketCatalogData>(MarketContentBuilder.CatalogPath);

        if (catalog == null)
        {
            s.Errors.Add("판매 가격표가 없습니다. Build Content > 6. Market을 실행하세요.");
            return s;
        }

        List<NpcShopData> shops = NpcShopBuilder.LoadShops();
        ReadTime(s);
        AnalyzeCrops(s, catalog, shops);
        AnalyzeFish(s, catalog);
        AnalyzeCombat(s, catalog);
        AnalyzeGathering(s, catalog);
        AnalyzeAnimals(s, catalog, shops);
        AnalyzeCooking(s, catalog);
        AnalyzeQuests(s, catalog, shops);
        AnalyzeRelationships(s);
        AnalyzeSurvival(s, catalog, shops);
        AnalyzePurchases(s, catalog, shops);
        return s;
    }

    private static void ReadTime(Snapshot s)
    {
        DayNightCycle cycle = Object.FindFirstObjectByType<DayNightCycle>(FindObjectsInactive.Include);
        SeasonCycle season = Object.FindFirstObjectByType<SeasonCycle>(FindObjectsInactive.Include);
        s.SceneLoaded = cycle != null && Object.FindFirstObjectByType<GameplaySaveController>(FindObjectsInactive.Include) != null;

        if (cycle != null)
        {
            s.DaySeconds = Mathf.Max(1f, new SerializedObject(cycle).FindProperty("fullDayDurationSeconds").floatValue);
        }

        if (season != null)
        {
            s.DaysPerSeason = Mathf.Max(1, new SerializedObject(season).FindProperty("daysPerSeason").intValue);
        }
    }

    // 판매 상자 가격 (계절 · 제철 포함)
    private static int Sell(MarketCatalogData catalog, ItemData item, SeasonType season)
    {
        MarketPriceQuote quote = catalog.GetQuote(item, season);
        return quote.Sellable ? quote.UnitPrice : 0;
    }

    private static int BestSell(MarketCatalogData catalog, ItemData item) => Seasons.Max(season => Sell(catalog, item, season));

    // 그 계절에 상인 · NPC 상점에서 사는 가장 싼 값 (할인 없음), 못 사면 0
    private static int Buy(MarketCatalogData catalog, List<NpcShopData> shops, ItemData item, SeasonType season)
    {
        int best = int.MaxValue;

        foreach (MarketStockEntry entry in catalog.Stock)
        {
            if (entry != null && entry.Item == item && entry.IsSoldIn(season))
            {
                best = Mathf.Min(best, entry.Price);
            }
        }

        foreach (NpcShopData shop in shops)
        {
            foreach (NpcShopData.StockEntry entry in shop.Stock)
            {
                if (entry != null && entry.Item == item && entry.IsSoldIn(season))
                {
                    best = Mathf.Min(best, entry.Price);
                }
            }
        }

        return best == int.MaxValue ? 0 : best;
    }

    private static int BuyAnySeason(MarketCatalogData catalog, List<NpcShopData> shops, ItemData item)
    {
        int best = 0;

        foreach (SeasonType season in Seasons)
        {
            int price = Buy(catalog, shops, item, season);

            if (price > 0 && (best == 0 || price < best))
            {
                best = price;
            }
        }

        return best;
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

        return result;
    }

    private static string Name(ItemData item) => item != null ? item.DisplayName : "-";

    // 농사 : 씨앗 한 개를 심어 거둘 때까지의 이익 (평균 수확량 · 제철 가격 · 씨앗 돌려받기 포함)
    private static void AnalyzeCrops(Snapshot s, MarketCatalogData catalog, List<NpcShopData> shops)
    {
        foreach (CropData crop in LoadAll<CropData>().OrderBy(crop => crop.CropId, StringComparer.Ordinal))
        {
            if (crop.HarvestItem == null || crop.SeedItem == null || crop.GrowingSeasons.Count == 0)
            {
                continue;
            }

            SeasonType season = crop.GrowingSeasons[0];
            int seed = Buy(catalog, shops, crop.SeedItem, season);
            float harvest = (crop.MinimumHarvestAmount + crop.MaximumHarvestAmount) * 0.5f;
            int unit = Sell(catalog, crop.HarvestItem, season);
            float profit = harvest * unit + crop.SeedReturnChance * seed - seed;
            CropRow row = new CropRow
            {
                Name = crop.DisplayName, Season = SeasonNames[(int)season], Days = crop.GrowthDays, SeedCost = seed, Harvest = harvest, Unit = unit,
                Profit = profit, PerDay = profit / crop.GrowthDays, SeasonProfit = profit * Mathf.Floor(s.DaysPerSeason / (float)crop.GrowthDays) * crop.GrowingSeasons.Count
            };
            s.Crops.Add(row);

            if (seed <= 0)
            {
                s.Errors.Add($"농사 {crop.DisplayName} : {row.Season}에 씨앗을 파는 곳이 없습니다.");
            }

            if (profit <= 0f)
            {
                s.Errors.Add($"농사 {crop.DisplayName} : 씨앗 값({seed})보다 수확 값이 적어 심을수록 손해입니다.");
            }
        }

        if (s.Crops.Count == 0)
        {
            return;
        }

        s.CropAverage = s.Crops.Average(row => row.PerDay);

        foreach (CropRow row in s.Crops)
        {
            float ratio = row.PerDay / Mathf.Max(0.01f, s.CropAverage);

            if (ratio < CropBandLow || ratio > CropBandHigh)
            {
                s.Errors.Add($"농사 {row.Name} : 한 칸 하루 이익 {row.PerDay:0.0}코인이 평균 {s.CropAverage:0.0}의 {ratio * 100f:0}%입니다. ({CropBandLow * 100f:0}~{CropBandHigh * 100f:0}% 목표)");
            }
        }
    }

    // 낚시 : 계절마다 맑은 날 연못에서 한 마리 평균 값과 한 마리에 걸리는 시간
    private static void AnalyzeFish(Snapshot s, MarketCatalogData catalog)
    {
        FishingRulesData rules = LoadAll<FishingRulesData>().FirstOrDefault();
        List<FishData> fish = LoadAll<FishData>();
        float wait = rules != null ? (rules.MinimumBiteWait + rules.MaximumBiteWait) * 0.5f + rules.CastDuration : 6f;

        for (int index = 0; index < Seasons.Length; index++)
        {
            float weight = 0f;
            float value = 0f;
            float seconds = 0f;
            HashSet<FishData> kinds = new HashSet<FishData>();

            for (int hour = 6; hour < 24; hour++)
            {
                foreach (FishData entry in fish)
                {
                    if (entry.ResultItem == null || !entry.CanAppear(WaterBodyType.Lake, Seasons[index], WeatherType.Clear, hour + 0.5f, 1))
                    {
                        continue;
                    }

                    kinds.Add(entry);
                    weight += entry.SpawnWeight;
                    value += entry.SpawnWeight * Sell(catalog, entry.ResultItem, Seasons[index]);
                    seconds += entry.SpawnWeight * (wait + entry.RequiredSuccessCount * 1.1f + 2f); // 기다림 + 미니게임(성공 1번 약 1.1초) + 줍기
                }
            }

            if (weight <= 0f)
            {
                s.Fish.Add(new FishRow { Season = SeasonNames[index] });
                continue;
            }

            float average = value / weight;
            float time = seconds / weight;
            s.Fish.Add(new FishRow { Season = SeasonNames[index], Kinds = kinds.Count, Value = average, Seconds = time, PerMinute = average * 60f / time });
        }

        List<FishRow> active = s.Fish.Where(row => row.PerMinute > 0f).ToList();
        s.FishPerMinute = active.Count > 0 ? active.Average(row => row.PerMinute) : 0f;
    }

    // 적 사냥 : Scene의 적 생성 지점마다 도끼로 쓰러뜨리는 시간 · 전리품 기대 값 · 다시 나오는 시간
    private static void AnalyzeCombat(Snapshot s, MarketCatalogData catalog)
    {
        if (!s.SceneLoaded)
        {
            return;
        }

        ItemData axe = LoadAll<ItemData>().FirstOrDefault(item => item.ItemId == AxeItemId);
        float damage = axe != null ? axe.BaseDamage : 10f;
        float cooldown = axe != null ? axe.AttackCooldown : 0.6f;

        foreach (EnemySpawnPoint point in Object.FindObjectsByType<EnemySpawnPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None).OrderBy(point => point.name, StringComparer.Ordinal))
        {
            if (!point.gameObject.scene.IsValid())
            {
                continue;
            }

            SerializedObject serialized = new SerializedObject(point);
            GameObject prefab = serialized.FindProperty("enemyPrefab").objectReferenceValue as GameObject;
            bool respawnEnabled = serialized.FindProperty("respawnEnabled").boolValue;
            float respawn = serialized.FindProperty("respawnDelay").floatValue;
            EnemyHealth health = prefab != null ? prefab.GetComponentInChildren<EnemyHealth>(true) : null;
            EnemyLootDropper dropper = prefab != null ? prefab.GetComponentInChildren<EnemyLootDropper>(true) : null;
            EnemyCombatData data = health != null ? new SerializedObject(health).FindProperty("combatData").objectReferenceValue as EnemyCombatData : null;
            EnemyLootTable table = dropper != null ? new SerializedObject(dropper).FindProperty("lootTable").objectReferenceValue as EnemyLootTable : null;

            if (prefab == null || data == null)
            {
                s.Errors.Add($"적 생성 지점 {point.name} : 적 Prefab 또는 전투 데이터가 없습니다.");
                continue;
            }

            float loot = 0f;

            if (table != null)
            {
                foreach (EnemyLootTable.LootEntry entry in table.Entries)
                {
                    if (entry?.ItemData != null)
                    {
                        loot += entry.DropChance * (entry.MinimumQuantity + entry.MaximumQuantity) * 0.5f * Sell(catalog, entry.ItemData, SeasonType.Spring);
                    }
                }
            }

            string enemyKey = prefab.name.StartsWith("Enemy_", StringComparison.Ordinal) ? prefab.name.Substring("Enemy_".Length) : prefab.name;
            float hitDamage = damage * (1f - Mathf.Clamp01(data.DefensePercent / 100f));
            int hits = hitDamage > 0f ? Mathf.CeilToInt(data.MaximumHealth / hitDamage) : 999;
            float kill = hits * cooldown;
            float cycle = (respawnEnabled ? respawn : s.DaySeconds) + kill + 3f; // 다시 찾아가는 시간 3초
            CombatRow row = new CombatRow
            {
                Point = point.name, Enemy = prefab.name, Table = table != null ? table.name : "-", TableMatches = table != null && table.name.EndsWith(enemyKey, StringComparison.Ordinal),
                Hits = hits, KillSeconds = kill, PlayerHits = data.AttackDamage > 0f ? Mathf.CeilToInt(PlayerHealth / data.AttackDamage) : 999,
                Loot = loot, Respawn = respawn, RespawnHours = respawn / s.HourSeconds, PerMinute = loot * 60f / cycle
            };
            s.Combat.Add(row);

            if (table == null)
            {
                s.Errors.Add($"적 {prefab.name} : 전리품 표가 없습니다.");
            }
            else if (!row.TableMatches)
            {
                s.Errors.Add($"적 {prefab.name} : 다른 적의 전리품 표({table.name})를 쓰고 있습니다.");
            }

            if (respawnEnabled && row.RespawnHours < MinRespawnHours)
            {
                s.Errors.Add($"적 생성 지점 {point.name} : {respawn:0}초(게임 {row.RespawnHours:0.#}시간)마다 다시 나와 반복 사냥이 됩니다. (게임 {MinRespawnHours:0}시간 = {MinRespawnHours * s.HourSeconds:0}초 이상 목표)");
            }

            if (hits < MinHitsToKill || hits > MaxHitsToKill)
            {
                s.Errors.Add($"적 {prefab.name} : 도끼로 {hits}번 때려야 쓰러집니다. ({MinHitsToKill}~{MaxHitsToKill}번 목표)");
            }

            if (row.PlayerHits < MinHitsToDownPlayer)
            {
                s.Errors.Add($"적 {prefab.name} : {row.PlayerHits}번 맞으면 플레이어가 쓰러집니다. ({MinHitsToDownPlayer}번 이상 목표)");
            }
        }

        s.CombatPerMinute = s.Combat.Sum(row => row.PerMinute);

        if (s.FishPerMinute > 0f && s.CombatPerMinute > s.FishPerMinute * CombatIncomeLimit)
        {
            s.Errors.Add($"적 사냥 수입이 1분에 약 {s.CombatPerMinute:0}코인으로 낚시({s.FishPerMinute:0})의 {Percent(s.CombatPerMinute, s.FishPerMinute)}입니다. ({CombatIncomeLimit * 100f:0}% 이하 목표)");
        }
    }

    // 채집 : 자원 한 곳을 다 캐고 다시 나올 때까지 반복했을 때 1분 수입
    private static void AnalyzeGathering(Snapshot s, MarketCatalogData catalog)
    {
        if (!s.SceneLoaded)
        {
            return;
        }

        Dictionary<ItemData, GatherRow> rows = new Dictionary<ItemData, GatherRow>();

        foreach (GatherableResource resource in Object.FindObjectsByType<GatherableResource>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            SerializedObject serialized = new SerializedObject(resource);

            if (!(serialized.FindProperty("resourceItem").objectReferenceValue is ItemData item))
            {
                continue;
            }

            int quantity = Mathf.Max(1, serialized.FindProperty("totalQuantity").intValue);
            float respawn = serialized.FindProperty("respawnEnabled").boolValue ? serialized.FindProperty("respawnDelay").floatValue : s.DaySeconds;
            float swing = Mathf.Max(0.6f, serialized.FindProperty("gatherCooldown").floatValue);
            float perMinute = quantity * Sell(catalog, item, SeasonType.Spring) * 60f / (respawn + quantity * swing);

            if (!rows.TryGetValue(item, out GatherRow row))
            {
                row = new GatherRow { Item = item.DisplayName, Respawn = respawn, PerMinute = perMinute };
                rows.Add(item, row);
            }

            row.Nodes++;
            row.PerMinute = Mathf.Max(row.PerMinute, perMinute);
        }

        s.Gathering.AddRange(rows.Values.OrderBy(row => row.Item, StringComparer.Ordinal));

        foreach (GatherRow row in s.Gathering)
        {
            if (s.FishPerMinute > 0f && row.PerMinute > s.FishPerMinute)
            {
                s.Errors.Add($"채집 {row.Item} : 한 곳에서 1분에 {row.PerMinute:0}코인으로 낚시({s.FishPerMinute:0})보다 많습니다.");
            }
        }
    }

    // 가축 : 하루 생산물 값 - 하루 먹이 값 (먹이는 상인 가격)
    private static void AnalyzeAnimals(Snapshot s, MarketCatalogData catalog, List<NpcShopData> shops)
    {
        foreach (AnimalData animal in LoadAll<AnimalData>().OrderBy(animal => animal.AnimalId, StringComparer.Ordinal))
        {
            if (animal.FeedItem == null || animal.ProductItem == null)
            {
                continue;
            }

            float feed = Mathf.Max(1, BuyAnySeason(catalog, shops, animal.FeedItem));
            float feedCost = animal.FeedPerDay * feed;
            float product = animal.ProductAmount * (1f + animal.BonusChance) / animal.ProductIntervalDays * Sell(catalog, animal.ProductItem, SeasonType.Spring);
            float profit = product - feedCost;
            float lure = animal.AttractFeedCost * feed;
            s.Animals.Add(new AnimalRow { Name = animal.DisplayName, FeedCost = feedCost, Product = product, Profit = profit, LureCost = lure, Payback = profit > 0f ? lure / profit : 0f });

            if (profit <= 0f)
            {
                s.Errors.Add($"가축 {animal.DisplayName} : 하루 먹이 값({feedCost:0})이 생산물 값({product:0})보다 커서 기를수록 손해입니다.");
            }
        }
    }

    // 요리 : 요리한 값이 재료를 그냥 판 값보다 작으면 안 된다
    private static void AnalyzeCooking(Snapshot s, MarketCatalogData catalog)
    {
        foreach (CookingRecipeData recipe in LoadAll<CookingRecipeData>().OrderBy(recipe => recipe.SortOrder))
        {
            if (recipe.ResultItem == null)
            {
                continue;
            }

            float ingredients = 0f;

            foreach (CraftingIngredient ingredient in recipe.Ingredients)
            {
                if (ingredient?.ItemData != null)
                {
                    ingredients += ingredient.Amount * BestSell(catalog, ingredient.ItemData);
                }
            }

            float result = recipe.ResultQuantity * BestSell(catalog, recipe.ResultItem);
            s.Cooking.Add(new CookRow { Name = recipe.DisplayName, Ingredients = ingredients, Result = result, Gain = result - ingredients });

            if (result < ingredients)
            {
                s.Errors.Add($"요리 {recipe.DisplayName} : 요리 값({result:0})이 재료 값({ingredients:0})보다 작습니다.");
            }
        }
    }

    // 의뢰 : 보상 코인 ÷ 필요 물건을 판매 상자에 판 값 (그 계절 가격)
    private static void AnalyzeQuests(Snapshot s, MarketCatalogData catalog, List<NpcShopData> shops)
    {
        foreach (NpcQuestBook book in NpcQuestBuilder.LoadBooks().OrderBy(book => book.OwnerId, StringComparer.Ordinal))
        {
            foreach (NpcQuestBook.Quest quest in book.Quests)
            {
                if (quest == null)
                {
                    continue;
                }

                bool allSeasons = quest.Seasons == null || quest.Seasons.Count == 0 || quest.Seasons.Count >= Seasons.Length;
                SeasonType season = allSeasons ? SeasonType.Spring : quest.Seasons[0];
                float need = 0f;

                foreach (NpcQuestBook.Requirement requirement in quest.Requirements)
                {
                    if (requirement?.Item != null)
                    {
                        need += requirement.Amount * Math.Max(Sell(catalog, requirement.Item, season), 1);
                    }
                }

                float rewardItem = quest.RewardItem != null ? quest.RewardItemAmount * Math.Max(BestSell(catalog, quest.RewardItem), BuyAnySeason(catalog, shops, quest.RewardItem)) : 0f;
                float reward = quest.RewardCoins + rewardItem;
                QuestRow row = new QuestRow
                {
                    Id = quest.QuestId, Owner = book.OwnerId, Special = quest.IsSpecial, Season = allSeasons ? "모든 계절" : SeasonNames[(int)season],
                    Need = need, Reward = reward, Ratio = need > 0f ? (quest.IsSpecial ? reward : quest.RewardCoins) / need : 0f
                };
                s.Quests.Add(row);

                if (quest.IsSpecial)
                {
                    continue;
                }

                if (row.Ratio < QuestRatioLow)
                {
                    s.Errors.Add($"의뢰 {quest.QuestId} : 보상 {quest.RewardCoins}코인이 필요 물건 판매가 {need:0}코인보다 적어 판매 상자보다 손해입니다.");
                }
                else if (row.Ratio < QuestRatioNote)
                {
                    s.Notes.Add($"의뢰 {quest.QuestId} : 보상이 판매가의 {row.Ratio:0.00}배로 조금 낮습니다. (상점에서 사서 전달하는 값보다 높일 수 없는 경우)");
                }
            }
        }
    }

    // 호감도 : 규칙대로 매일 대화 · 선물 · 이벤트를 했을 때 단계에 닿는 날
    private static void AnalyzeRelationships(Snapshot s)
    {
        NpcDatabase database = AssetDatabase.LoadAssetAtPath<NpcDatabase>(NpcContentBuilder.DatabasePath);

        if (database == null)
        {
            s.Errors.Add("NpcDatabase가 없습니다.");
            return;
        }

        List<NpcEventBook> events = NpcEventBuilder.LoadBooks();
        int loved = database.GetGiftPoints(GiftPreference.Loved, false);
        int liked = database.GetGiftPoints(GiftPreference.Liked, false);
        s.GiftRule = $"대화 +{database.DailyTalkPoints} · 매우 좋아함 +{loved} · 좋아함 +{liked} · 선물 하루 {database.GiftsPerDay}번 · 한 주 {database.GiftsPerWeek}번 · 단계 {database.StageThreshold(AffinityStage.Curious)}/{database.StageThreshold(AffinityStage.Trust)}/{database.StageThreshold(AffinityStage.Affection)}";
        (string name, int gift, bool useEvents)[] scenarios =
        {
            ("대화만 (이벤트 포함)", 0, true),
            ("대화 + 좋아하는 선물", liked, true),
            ("대화 + 매우 좋아하는 선물", loved, true)
        };

        foreach ((string name, int gift, bool useEvents) in scenarios)
        {
            RelationRow row = new RelationRow { Scenario = name };

            for (int stage = 0; stage < 3; stage++)
            {
                row.Min[stage] = int.MaxValue;
                row.Max[stage] = int.MinValue;
            }

            foreach (NpcCharacterData character in database.GetStoryCast())
            {
                NpcEventBook book = events.FirstOrDefault(candidate => candidate.OwnerId == character.CharacterId);
                int[] days = Simulate(database, character, gift, useEvents ? book : null);

                for (int stage = 0; stage < 3; stage++)
                {
                    int day = days[stage] < 0 ? SimulationDays + 1 : days[stage];
                    row.Min[stage] = Mathf.Min(row.Min[stage], day);
                    row.Max[stage] = Mathf.Max(row.Max[stage], day);
                }
            }

            for (int stage = 0; stage < 3; stage++)
            {
                row.Min[stage] = row.Min[stage] > SimulationDays ? -1 : row.Min[stage];
                row.Max[stage] = row.Max[stage] > SimulationDays ? -1 : row.Max[stage];
            }

            s.Relations.Add(row);
        }

        if (s.Relations.Count < 3)
        {
            return;
        }

        RelationRow talk = s.Relations[0];
        RelationRow likedRow = s.Relations[1];
        RelationRow lovedRow = s.Relations[2];
        Check(talk.Max[0] < 0 || talk.Max[0] > TalkOnlyCuriousMax, $"대화만으로 호기심까지 {Range(talk, 0)} 걸립니다. ({TalkOnlyCuriousMax}일 안 목표)");
        Check(likedRow.Min[0] >= 0 && likedRow.Min[0] < LikedCuriousMin, $"좋아하는 선물로 호기심까지 {likedRow.Min[0]}일이면 너무 빠릅니다. ({LikedCuriousMin}일 이상 목표)");
        Check(likedRow.Max[0] < 0 || likedRow.Max[0] > LikedCuriousMax, $"좋아하는 선물로 호기심까지 {Range(likedRow, 0)} 걸립니다. ({LikedCuriousMax}일 안 목표)");
        Check(likedRow.Max[1] < 0 || likedRow.Max[1] > LikedTrustMax, $"좋아하는 선물로 신뢰까지 {Range(likedRow, 1)} 걸립니다. ({LikedTrustMax}일 안 목표)");
        Check(lovedRow.Min[0] >= 0 && lovedRow.Min[0] < LovedCuriousMin, $"매우 좋아하는 선물로 호기심까지 {lovedRow.Min[0]}일이면 너무 빠릅니다. ({LovedCuriousMin}일 이상 목표)");
        Check(lovedRow.Min[1] >= 0 && lovedRow.Min[1] < LovedTrustMin, $"매우 좋아하는 선물로 신뢰까지 {lovedRow.Min[1]}일이면 너무 빠릅니다. ({LovedTrustMin}일 이상 목표)");
        Check(lovedRow.Max[2] < 0 || lovedRow.Max[2] > LovedAffectionMax, $"매우 좋아하는 선물로 애정까지 {Range(lovedRow, 2)} 걸립니다. ({LovedAffectionMax}일 안 목표)");

        void Check(bool failed, string message)
        {
            if (failed)
            {
                s.Errors.Add("호감도 : " + message);
            }
        }
    }

    // 하루 : 첫 대화 → (단계에 닿은 다음 날부터) 이벤트 최고 선택 → 선물 (하루 · 한 주 제한)
    public static int[] Simulate(NpcDatabase database, NpcCharacterData character, int giftPoints, NpcEventBook book)
    {
        int[] reached = { -1, -1, -1 };
        int affinity = character.DefaultAffinity;
        int week = -1;
        int giftsThisWeek = 0;
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);

        for (int day = 1; day <= SimulationDays && reached[2] < 0; day++)
        {
            AffinityStage stageAtDawn = database.GetStage(affinity);
            affinity += database.DailyTalkPoints;

            if (book != null)
            {
                NpcEventBook.Event next = book.Events
                    .Where(candidate => candidate != null && !seen.Contains(candidate.EventId) && candidate.RequiredStage <= stageAtDawn && candidate.Choices.Count > 0)
                    .OrderBy(candidate => candidate.RequiredStage)
                    .FirstOrDefault();

                if (next != null)
                {
                    seen.Add(next.EventId);
                    affinity += next.Choices.Max(choice => choice.Affinity);
                }
            }

            if (giftPoints != 0)
            {
                int currentWeek = NpcRelationshipManager.WeekOf(day);

                if (currentWeek != week)
                {
                    week = currentWeek;
                    giftsThisWeek = 0;
                }

                int gifts = Mathf.Min(database.GiftsPerDay, database.GiftsPerWeek - giftsThisWeek);

                for (int gift = 0; gift < gifts; gift++)
                {
                    affinity += giftPoints;
                    giftsThisWeek++;
                }
            }

            affinity = Mathf.Clamp(affinity, 0, character.MaxAffinity);

            for (int stage = 0; stage < 3; stage++)
            {
                if (reached[stage] < 0 && affinity >= database.StageThreshold((AffinityStage)(stage + 1)))
                {
                    reached[stage] = day;
                }
            }
        }

        return reached;
    }

    // 생존 : 하루에 줄어드는 허기 · 갈증 (깨어 있는 시간 + 잠잘 때 비용)
    private static void AnalyzeSurvival(Snapshot s, MarketCatalogData catalog, List<NpcShopData> shops)
    {
        if (!s.SceneLoaded)
        {
            return;
        }

        PlayerHunger hunger = Object.FindFirstObjectByType<PlayerHunger>(FindObjectsInactive.Include);
        PlayerThirst thirst = Object.FindFirstObjectByType<PlayerThirst>(FindObjectsInactive.Include);
        SleepSystem sleep = Object.FindFirstObjectByType<SleepSystem>(FindObjectsInactive.Include);

        if (hunger == null || thirst == null)
        {
            return;
        }

        float wake = sleep != null ? new SerializedObject(sleep).FindProperty("wakeHour").floatValue : 6f;
        float awakeSeconds = (24f - wake) * s.HourSeconds;
        s.HungerPerDay = new SerializedObject(hunger).FindProperty("depletionPerSecond").floatValue * awakeSeconds + (sleep != null ? new SerializedObject(sleep).FindProperty("hungerCost").floatValue : 0f);
        s.ThirstPerDay = new SerializedObject(thirst).FindProperty("depletionPerSecond").floatValue * awakeSeconds + (sleep != null ? new SerializedObject(sleep).FindProperty("thirstCost").floatValue : 0f);
        ItemData water = LoadAll<ItemData>().FirstOrDefault(item => item.ItemId == "drink_water_bottle");
        int waterPrice = water != null ? BuyAnySeason(catalog, shops, water) : 0;
        s.Survival = $"하루 허기 {s.HungerPerDay:0} · 갈증 {s.ThirstPerDay:0} 줄어듦 (감자 {s.HungerPerDay / 15f:0.#}개, 물병 {s.ThirstPerDay / 35f:0.#}개 = {Mathf.CeilToInt(s.ThirstPerDay / 35f) * waterPrice}코인)";

        if (s.HungerPerDay > 100f || s.ThirstPerDay > 100f)
        {
            s.Errors.Add($"생존 : 하루에 허기 {s.HungerPerDay:0} · 갈증 {s.ThirstPerDay:0}이 줄어 가득 채워도 하루를 못 버팁니다.");
        }
    }

    // 목표 물건 : 100코인 이상인 물건을 낚시 · 농사로 모으는 데 걸리는 시간
    private static void AnalyzePurchases(Snapshot s, MarketCatalogData catalog, List<NpcShopData> shops)
    {
        Dictionary<ItemData, PurchaseRow> rows = new Dictionary<ItemData, PurchaseRow>();

        void Add(ItemData item, int price, string seller)
        {
            if (item == null || price < 100 || (rows.TryGetValue(item, out PurchaseRow old) && old.Price <= price))
            {
                return;
            }

            rows[item] = new PurchaseRow
            {
                Name = item.DisplayName, Seller = seller, Price = price,
                FishMinutes = s.FishPerMinute > 0f ? price / s.FishPerMinute : 0f,
                FarmDays = s.CropAverage > 0f ? price / (s.CropAverage * 10f) : 0f
            };
        }

        foreach (MarketStockEntry entry in catalog.Stock)
        {
            Add(entry?.Item, entry?.Price ?? 0, "떠돌이 상인");
        }

        foreach (NpcShopData shop in shops)
        {
            foreach (NpcShopData.StockEntry entry in shop.Stock)
            {
                Add(entry?.Item, entry?.Price ?? 0, shop.ShopName);
            }
        }

        s.Purchases.AddRange(rows.Values.OrderBy(row => row.Price));
    }

    // ---------------------------------------------------------------- 표 (Markdown)

    public static string WriteMarkdown(Snapshot s)
    {
        StringBuilder md = new StringBuilder();
        string F(float value) => value.ToString("0.#", CultureInfo.InvariantCulture);

        md.AppendLine("# Project U 밸런스 보고서");
        md.AppendLine();
        md.AppendLine("`Tools > Project U > Balance Report` 또는 `Build Content > 13. Balance Pass`가 만든 표입니다. 데이터를 바꾸면 다시 실행하세요.");
        md.AppendLine();
        md.AppendLine($"- 하루 {F(s.DaySeconds)}초 (게임 1시간 = {F(s.HourSeconds)}초), 한 계절 {s.DaysPerSeason}일");
        md.AppendLine($"- 결과 : 오류 {s.Errors.Count}개");
        md.AppendLine();

        md.AppendLine("## 1. 활동별 수입");
        md.AppendLine();
        md.AppendLine("| 활동 | 수입 | 기준 |");
        md.AppendLine("| --- | --- | --- |");
        md.AppendLine($"| 농사 | 한 칸 하루 {F(s.CropAverage)}코인 | 작물마다 평균의 {CropBandLow * 100f:0}~{CropBandHigh * 100f:0}% |");
        md.AppendLine($"| 낚시 | 1분 {F(s.FishPerMinute)}코인 | 비교 기준 |");
        md.AppendLine($"| 적 사냥 | 1분 {F(s.CombatPerMinute)}코인 | 낚시의 {CombatIncomeLimit * 100f:0}% 이하 |");

        foreach (GatherRow row in s.Gathering)
        {
            md.AppendLine($"| 채집 ({row.Item}) | 한 곳 1분 {F(row.PerMinute)}코인 | 낚시 이하 |");
        }

        md.AppendLine();
        md.AppendLine("## 2. 농사 (씨앗 1개)");
        md.AppendLine();
        md.AppendLine("| 작물 | 계절 | 자라는 날 | 씨앗 | 평균 수확 | 한 개 값 | 한 번 이익 | 한 칸 하루 | 한 계절 |");
        md.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- |");

        foreach (CropRow row in s.Crops)
        {
            md.AppendLine($"| {row.Name} | {row.Season} | {row.Days}일 | {row.SeedCost} | {F(row.Harvest)} | {row.Unit} | {F(row.Profit)} | {F(row.PerDay)} | {F(row.SeasonProfit)} |");
        }

        md.AppendLine();
        md.AppendLine("## 3. 낚시 (맑은 날 연못)");
        md.AppendLine();
        md.AppendLine("| 계절 | 나오는 종류 | 한 마리 평균 값 | 한 마리 시간 | 1분 수입 |");
        md.AppendLine("| --- | --- | --- | --- | --- |");

        foreach (FishRow row in s.Fish)
        {
            md.AppendLine($"| {row.Season} | {row.Kinds} | {F(row.Value)} | {F(row.Seconds)}초 | {F(row.PerMinute)} |");
        }

        md.AppendLine();
        md.AppendLine("## 4. 적 (도끼 기준)");
        md.AppendLine();
        md.AppendLine("| 생성 지점 | 적 | 전리품 표 | 쓰러뜨리는 타수 | 걸리는 시간 | 플레이어가 버티는 타수 | 전리품 기대 값 | 다시 나옴 | 1분 수입 |");
        md.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- |");

        foreach (CombatRow row in s.Combat)
        {
            md.AppendLine($"| {row.Point} | {row.Enemy} | {row.Table} | {row.Hits} | {F(row.KillSeconds)}초 | {row.PlayerHits} | {F(row.Loot)} | {F(row.Respawn)}초 (게임 {F(row.RespawnHours)}시간) | {F(row.PerMinute)} |");
        }

        md.AppendLine();
        md.AppendLine("## 5. 가축 · 요리");
        md.AppendLine();
        md.AppendLine("| 가축 | 하루 먹이 값 | 하루 생산물 값 | 하루 이익 | 데려오기 | 회수 |");
        md.AppendLine("| --- | --- | --- | --- | --- | --- |");

        foreach (AnimalRow row in s.Animals)
        {
            md.AppendLine($"| {row.Name} | {F(row.FeedCost)} | {F(row.Product)} | {F(row.Profit)} | {F(row.LureCost)} | {F(row.Payback)}일 |");
        }

        md.AppendLine();
        md.AppendLine("| 요리 | 재료 판매가 | 요리 판매가 | 늘어난 값 |");
        md.AppendLine("| --- | --- | --- | --- |");

        foreach (CookRow row in s.Cooking)
        {
            md.AppendLine($"| {row.Name} | {F(row.Ingredients)} | {F(row.Result)} | {F(row.Gain)} |");
        }

        md.AppendLine();
        md.AppendLine("## 6. 의뢰");
        md.AppendLine();
        md.AppendLine("| 의뢰 | NPC | 계절 | 필요 물건 판매가 | 보상 값 | 배율 |");
        md.AppendLine("| --- | --- | --- | --- | --- | --- |");

        foreach (QuestRow row in s.Quests)
        {
            md.AppendLine($"| {row.Id}{(row.Special ? " (특별)" : string.Empty)} | {row.Owner} | {row.Season} | {F(row.Need)} | {F(row.Reward)} | {row.Ratio:0.00} |");
        }

        md.AppendLine();
        md.AppendLine("## 7. 호감도 (매일 말 걸기 기준, 알파 NPC 7명)");
        md.AppendLine();
        md.AppendLine(s.GiftRule);
        md.AppendLine();
        md.AppendLine("| 방법 | 호기심 | 신뢰 | 애정 |");
        md.AppendLine("| --- | --- | --- | --- |");

        foreach (RelationRow row in s.Relations)
        {
            md.AppendLine($"| {row.Scenario} | {Range(row, 0)} | {Range(row, 1)} | {Range(row, 2)} |");
        }

        md.AppendLine();
        md.AppendLine("## 8. 생존 · 큰 물건");
        md.AppendLine();
        md.AppendLine(string.IsNullOrEmpty(s.Survival) ? "- (게임 Scene이 열려 있지 않아 생략)" : "- " + s.Survival);
        md.AppendLine();
        md.AppendLine("| 물건 | 파는 곳 | 가격 | 낚시로 | 밭 10칸으로 |");
        md.AppendLine("| --- | --- | --- | --- | --- |");

        foreach (PurchaseRow row in s.Purchases)
        {
            md.AppendLine($"| {row.Name} | {row.Seller} | {row.Price} | {F(row.FishMinutes)}분 | {F(row.FarmDays)}일 |");
        }

        if (s.Errors.Count > 0 || s.Notes.Count > 0)
        {
            md.AppendLine();
            md.AppendLine("## 9. 검사 결과");
            md.AppendLine();

            foreach (string error in s.Errors)
            {
                md.AppendLine("- 오류 : " + error);
            }

            foreach (string note in s.Notes)
            {
                md.AppendLine("- 참고 : " + note);
            }
        }

        string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ReportPath));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
        File.WriteAllText(fullPath, md.ToString().Replace("\r\n", "\n"), new UTF8Encoding(false));
        return ReportPath;
    }
}
