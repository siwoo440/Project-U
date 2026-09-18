using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// 92일차: NPC 상점 도구
// 1. 원본 CSV(NpcShops · NpcShopStock)로 밀키 · 드라비아 · 리첼의 상점 데이터(판매 목록 · 가격 · 사 주는 물건 · 영업 위치 · 인사)를 만든다
// 2. 게임 Scene의 NPC 관리자에 상점 관리자를 붙이고, 상인 창을 다시 만들어 NPC 한마디 칸을 추가한다
// 3. 검사 : 주인 · 영업 시간(일정 데이터) · 재고 · 되팔기 이익 · 계절 씨앗 · 한글 글꼴 · Scene 연결
// 여러 번 실행해도 같은 Asset·오브젝트를 갱신한다. 실행 후 Ctrl+S로 Scene을 저장해야 반영된다.
public static class NpcShopBuilder
{
    private const string DialogTitle = "Project U NPC 상점";
    private const string ScenePath = "Assets/_ProjectU/Scenes/20_Gameplay.unity";
    public const string ShopFolder = NpcContentBuilder.DataFolder + "/Shops";
    private const string ShopCsv = NpcContentBuilder.SourceFolder + "/NpcShops.csv";
    private const string StockCsv = NpcContentBuilder.SourceFolder + "/NpcShopStock.csv";

    public static readonly string[] ExpectedShops = { "shop_milky", "shop_dravia", "shop_lichel" };
    private static readonly Regex ShopIdPattern = new Regex("^shop_[a-z0-9]+(?:_[a-z0-9]+)*$");
    private static readonly string[] WeekdayNames = { "월", "화", "수", "목", "금", "토", "일" };

    // 코드에서 쓰는 상점 한글 문구 (고정 글꼴 Atlas에 모두 들어 있어야 함)
    private static readonly string[] RuntimeTexts =
    {
        "쉬는 날", "영업", "할인", "대화 · 거래", "거래", "거래 (닫힘)", "오늘 영업 : ", "내일 또 와요!",
        "가게 주인이 없어요.", "가판대가 있어야 물건을 펼칠 수 있어요. 상인 가판대를 지어 주세요.", "가게로 가는 중이에요. 도착하면 거래할 수 있어요.",
        "오늘은 쉬는 날이에요.", "오늘 영업은 끝났어요.", "지금은 영업 시간이 아니에요.", "내일 월화수목금토일요일 에 열어요.", "당분간 문을 열지 않아요.",
        "' 단계부터 살 수 있어요", "무관심 호기심 신뢰 애정 사랑", "상점 창을 열 수 없어요. Build Content > 10. NPC Shops를 실행하세요."
    };

    // ---------------------------------------------------------------- 메뉴

    [MenuItem(NpcContentBuilder.BuildMenuRoot + "10. NPC Shops (Stock + Prices + Hours + Trade)", false, 29)]
    private static void BuildAllMenu()
    {
        bool confirmed = EditorUtility.DisplayDialog(
            DialogTitle,
            "밀키(선셋 카페) · 드라비아(대장간) · 리첼(떠돌이 상점)의 판매 목록과 가격을 원본 CSV로 만들고,\n"
            + "현재 게임 Scene(20_Gameplay)의 NPC 관리자에 상점 관리자를 붙입니다.\n"
            + "상인 창은 NPC 한마디 칸을 넣어 다시 만듭니다.\n\n"
            + "먼저 6번(판매·상점) · 8번(NPC 배치) · 9번(NPC 대화) 메뉴를 실행해 두어야 합니다.\n"
            + "실행 후 Ctrl+S로 Scene을 저장해야 반영됩니다.",
            "실행",
            "취소");

        if (!confirmed)
        {
            return;
        }

        string report = BuildAll();
        Debug.Log(report);
        EditorUtility.DisplayDialog(DialogTitle, report.Length <= 1800 ? report : report.Substring(0, 1800) + "\n... (전체 내용은 Console 참고)", "확인");
    }

    // ---------------------------------------------------------------- 전체 생성

    public static string BuildAll()
    {
        StringBuilder report = new StringBuilder("[NPC 상점 생성]\n");
        List<NpcShopData> shops;

        try
        {
            EditorUtility.DisplayProgressBar(DialogTitle, "상점 데이터", 0.2f);
            NpcDatabase database = AssetDatabase.LoadAssetAtPath<NpcDatabase>(NpcContentBuilder.DatabasePath);

            if (database == null)
            {
                report.AppendLine("✗ NpcDatabase가 없습니다. 7번 메뉴를 먼저 실행하세요.");
                return report.ToString();
            }

            shops = CreateShops(database, report);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayProgressBar(DialogTitle, "한글 글꼴", 0.5f);
            EnsureFont(database, report);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        report.Append(WireScene(shops));
        AssetDatabase.SaveAssets();
        report.Append(Validate(out _));
        return report.ToString();
    }

    // ---------------------------------------------------------------- 상점 데이터

    private static List<NpcShopData> CreateShops(NpcDatabase database, StringBuilder report)
    {
        List<string> errors = new List<string>();
        List<NpcCsv.Row> shopRows = NpcCsv.Read(ShopCsv, errors);
        List<NpcCsv.Row> stockRows = NpcCsv.Read(StockCsv, errors);
        Dictionary<string, ItemData> items = LoadItemsById();
        List<NpcShopData> result = new List<NpcShopData>();
        HashSet<string> shopIds = new HashSet<string>(StringComparer.Ordinal);
        StylizedArtAssetFactory.EnsureFolder(ShopFolder);
        int stockCount = 0;
        int ruleCount = 0;

        foreach (NpcCsv.Row row in shopRows)
        {
            string id = row["ShopID"];
            string where = $"NpcShops.csv {row.LineNumber}줄";

            if (!ShopIdPattern.IsMatch(id) || !shopIds.Add(id))
            {
                errors.Add($"{where} : 상점 ID가 잘못되었거나 중복입니다 ({id})");
                continue;
            }

            if (!database.TryGet(row["Owner"], out NpcCharacterData owner))
            {
                errors.Add($"{where} : 주인 {row["Owner"]} 을(를) NpcDatabase에서 찾지 못했습니다.");
                continue;
            }

            List<NpcShopData.StockEntry> stock = new List<NpcShopData.StockEntry>();

            foreach (NpcCsv.Row stockRow in stockRows.Where(candidate => candidate["ShopID"] == id))
            {
                NpcShopData.StockEntry entry = ParseStock(stockRow, items, errors);

                if (entry != null)
                {
                    stock.Add(entry);
                }
            }

            List<NpcShopData.BuyRule> rules = ParseBuys(row["Buys"], items, errors, where);
            int[] discounts = ParseDiscounts(row["Discounts"], errors, where);
            string path = $"{ShopFolder}/NpcShop_{owner.EnglishName.Replace(" ", string.Empty)}.asset";
            NpcShopData shop = LoadOrCreateAsset<NpcShopData>(path);
            shop.EditorAssign(
                id,
                owner.CharacterId,
                row["Name"],
                Split(row["OpenLocations"]),
                stock,
                ParseInt(row["StockSeed"], 1),
                discounts,
                rules,
                ParseInt(row["DailyBuyLimit"], 0),
                Split(row["Greetings"]),
                row["Farewell"]);
            EditorUtility.SetDirty(shop);
            result.Add(shop);
            stockCount += stock.Count;
            ruleCount += rules.Count;
        }

        foreach (NpcCsv.Row stockRow in stockRows.Where(candidate => !shopIds.Contains(candidate["ShopID"])))
        {
            errors.Add($"NpcShopStock.csv {stockRow.LineNumber}줄 : 없는 상점 {stockRow["ShopID"]}");
        }

        result.Sort((left, right) => Array.IndexOf(ExpectedShops, left.ShopId).CompareTo(Array.IndexOf(ExpectedShops, right.ShopId)));
        report.AppendLine($"상점 {result.Count}곳 · 판매 물건 {stockCount}종 · 사 주는 규칙 {ruleCount}개 ({ShopFolder})");

        foreach (string error in errors)
        {
            report.AppendLine("✗ " + error);
        }

        return result;
    }

    private static NpcShopData.StockEntry ParseStock(NpcCsv.Row row, Dictionary<string, ItemData> items, List<string> errors)
    {
        string where = $"NpcShopStock.csv {row.LineNumber}줄";

        if (!items.TryGetValue(row["ItemID"], out ItemData item))
        {
            errors.Add($"{where} : 아이템 {row["ItemID"]} 이(가) 없습니다.");
            return null;
        }

        List<SeasonType> seasons = new List<SeasonType>();

        foreach (string token in Split(row["Seasons"]))
        {
            if (token == "All")
            {
                seasons.Clear();
                break;
            }

            if (Enum.TryParse(token, out SeasonType season))
            {
                seasons.Add(season);
            }
            else
            {
                errors.Add($"{where} : 계절 {token} 을(를) 알 수 없습니다.");
            }
        }

        AffinityStage stage = AffinityStage.Uninterested;

        if (!string.IsNullOrEmpty(row["MinStage"]) && !Enum.TryParse(row["MinStage"], out stage))
        {
            errors.Add($"{where} : 관계 단계 {row["MinStage"]} 을(를) 알 수 없습니다.");
        }

        float chance = string.IsNullOrEmpty(row["Chance"]) ? 1f : float.Parse(row["Chance"], CultureInfo.InvariantCulture);
        return new NpcShopData.StockEntry(item, ParseInt(row["Price"], 1), ParseInt(row["DailyStock"], 1), seasons.ToArray(), Mathf.Clamp01(chance), stage);
    }

    private static List<NpcShopData.BuyRule> ParseBuys(string text, Dictionary<string, ItemData> items, List<string> errors, string where)
    {
        List<NpcShopData.BuyRule> rules = new List<NpcShopData.BuyRule>();

        foreach (string token in Split(text))
        {
            string[] parts = token.Split(':');

            if (parts[0] == "type" && parts.Length == 3) // type:Crop:1.0 · type:*:0.8
            {
                float multiplier = float.Parse(parts[2], CultureInfo.InvariantCulture);

                if (parts[1] == "*")
                {
                    rules.Add(new NpcShopData.BuyRule(null, true, MarketGoodsType.Crop, multiplier));
                }
                else if (Enum.TryParse(parts[1], out MarketGoodsType goods))
                {
                    rules.Add(new NpcShopData.BuyRule(null, false, goods, multiplier));
                }
                else
                {
                    errors.Add($"{where} : 분류 {parts[1]} 을(를) 알 수 없습니다.");
                }
            }
            else if (parts.Length == 2 && items.TryGetValue(parts[0], out ItemData item)) // item_iron_ore:1.2
            {
                rules.Add(new NpcShopData.BuyRule(item, false, MarketGoodsType.Crop, float.Parse(parts[1], CultureInfo.InvariantCulture)));
            }
            else
            {
                errors.Add($"{where} : 사 주는 규칙 {token} 을(를) 읽지 못했습니다.");
            }
        }

        return rules;
    }

    private static int[] ParseDiscounts(string text, List<string> errors, string where)
    {
        string[] parts = text.Split('/');

        if (parts.Length != 5 || parts.Any(part => !int.TryParse(part, out _)))
        {
            errors.Add($"{where} : 할인율은 단계 5개를 0/0/5/10/15 형식으로 적어야 합니다 ({text})");
            return new[] { 0, 0, 0, 0, 0 };
        }

        return parts.Select(int.Parse).ToArray();
    }

    private static string[] Split(string text)
    {
        return string.IsNullOrWhiteSpace(text) ? new string[0] : text.Split('|').Select(part => part.Trim()).Where(part => part.Length > 0).ToArray();
    }

    private static int ParseInt(string text, int fallback)
    {
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : fallback;
    }

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

    public static List<NpcShopData> LoadShops()
    {
        List<NpcShopData> shops = new List<NpcShopData>();

        if (!AssetDatabase.IsValidFolder(ShopFolder))
        {
            return shops;
        }

        foreach (string guid in AssetDatabase.FindAssets("t:NpcShopData", new[] { ShopFolder }))
        {
            NpcShopData shop = AssetDatabase.LoadAssetAtPath<NpcShopData>(AssetDatabase.GUIDToAssetPath(guid));

            if (shop != null)
            {
                shops.Add(shop);
            }
        }

        shops.Sort((left, right) => string.CompareOrdinal(left.ShopId, right.ShopId));
        return shops;
    }

    // ---------------------------------------------------------------- 한글 글꼴

    public static IEnumerable<string> CollectShopTexts() // 상점 이름 · 인사 · 코드 문구
    {
        foreach (NpcShopData shop in LoadShops())
        {
            yield return shop.ShopName;
            yield return shop.Farewell;

            foreach (string line in shop.Greetings)
            {
                yield return line;
            }
        }

        foreach (string text in RuntimeTexts)
        {
            yield return text;
        }
    }

    private static void EnsureFont(NpcDatabase database, StringBuilder report)
    {
        int missing = 0;
        KoreanFontBuilder.Validate(CollectShopTexts(), _ => missing++, new StringBuilder());

        if (missing == 0)
        {
            report.AppendLine("한글 글꼴 : 상점 글자가 모두 들어 있음");
            return;
        }

        KoreanFontBuilder.Build(NpcContentBuilder.CollectDisplayTexts(database), report); // 대사 + 상점 글자로 다시 만듦 (GUID 유지)
    }

    // ---------------------------------------------------------------- Scene

    private static string WireScene(List<NpcShopData> shops)
    {
        StringBuilder report = new StringBuilder();
        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.GetActiveScene();
        NpcManager npcManager = Object.FindFirstObjectByType<NpcManager>(FindObjectsInactive.Include);

        if (scene.path != ScenePath)
        {
            report.AppendLine($"✗ 게임 Scene({ScenePath})을 열고 다시 실행하세요. 상점 데이터만 만들었습니다.");
            return report.ToString();
        }

        if (npcManager == null)
        {
            report.AppendLine("✗ NPC 관리자가 없습니다. 8번 메뉴(NPC 마을 배치)를 먼저 실행하세요.");
            return report.ToString();
        }

        NpcRelationshipManager relations = npcManager.GetComponent<NpcRelationshipManager>();

        if (relations == null)
        {
            report.AppendLine("✗ 관계 관리자가 없습니다. 9번 메뉴(NPC 대화)를 먼저 실행하세요.");
            return report.ToString();
        }

        NpcShopManager shopManager = npcManager.GetComponent<NpcShopManager>();

        if (shopManager == null)
        {
            shopManager = npcManager.gameObject.AddComponent<NpcShopManager>();
        }

        shopManager.EditorAssign(shops, npcManager, relations);
        EditorUtility.SetDirty(shopManager);
        report.AppendLine($"상점 관리자 : {npcManager.name} (상점 {shops.Count}곳)");
        report.AppendLine(MarketPopupUIBuilder.RebuildShopPopup(out _));
        EditorSceneManager.MarkSceneDirty(scene);
        report.AppendLine("Scene 변경 완료 : Ctrl+S로 저장하세요.");
        return report.ToString();
    }

    // ---------------------------------------------------------------- 검증

    public static string Validate(out int errorCount)
    {
        StringBuilder report = new StringBuilder("[NPC 상점 검증]\n");
        int errors = 0;

        void Error(string message)
        {
            errors++;
            report.AppendLine("✗ " + message);
        }

        NpcDatabase database = AssetDatabase.LoadAssetAtPath<NpcDatabase>(NpcContentBuilder.DatabasePath);
        MarketCatalogData catalog = AssetDatabase.LoadAssetAtPath<MarketCatalogData>(MarketContentBuilder.CatalogPath);
        List<NpcShopData> shops = LoadShops();

        if (database == null || catalog == null)
        {
            Error("NpcDatabase 또는 판매 가격표가 없습니다. 6번 · 7번 메뉴를 먼저 실행하세요.");
            errorCount = errors;
            report.AppendLine($"결과 : 오류 {errors}개");
            return report.ToString();
        }

        foreach (string expected in ExpectedShops)
        {
            int count = shops.Count(shop => shop.ShopId == expected);

            if (count != 1)
            {
                Error($"{expected} 상점 데이터가 {count}개입니다. 10번 메뉴를 실행하세요.");
            }
        }

        foreach (NpcCharacterData character in database.GetAlphaCast().Where(character => character.HasRole(NpcRole.Merchant)))
        {
            if (!shops.Any(shop => shop.OwnerId == character.CharacterId))
            {
                Error($"{character.CharacterId} : 상인 역할인데 상점 데이터가 없습니다.");
            }
        }

        int offers = 0;

        foreach (NpcShopData shop in shops)
        {
            offers += shop.Stock.Count;
            ValidateShop(shop, database, catalog, report, Error);
        }

        ValidateResale(shops, catalog, Error);
        ValidateSeeds(shops, Error);
        KoreanFontBuilder.Validate(CollectShopTexts(), Error, report);
        ValidateScene(shops, report, Error);
        report.AppendLine($"상점 {shops.Count}곳 · 판매 물건 {offers}종");
        errorCount = errors;
        report.AppendLine(errors == 0 ? "결과 : 오류 0개" : $"결과 : 오류 {errors}개");
        return report.ToString();
    }

    private static void ValidateShop(NpcShopData shop, NpcDatabase database, MarketCatalogData catalog, StringBuilder report, Action<string> error)
    {
        string id = shop.ShopId;

        if (!ShopIdPattern.IsMatch(id ?? string.Empty))
        {
            error($"{shop.name} : 상점 ID가 잘못되었습니다 ({id})");
        }

        if (!database.TryGet(shop.OwnerId, out NpcCharacterData owner))
        {
            error($"{id} : 주인 {shop.OwnerId} 이(가) 없습니다.");
            return;
        }

        if (owner.Ids.shopId != id || !owner.HasRole(NpcRole.Merchant) || !owner.CanInteract(NpcInteraction.Trade))
        {
            error($"{id} : 주인 {owner.CharacterId} 의 상점 ID · 상인 역할 · 거래 상호작용이 맞지 않습니다.");
        }

        if (!owner.IsAlphaCast || owner.Schedule == null)
        {
            error($"{id} : 주인 {owner.CharacterId} 이(가) 마을에 배치되지 않았거나 일정이 없습니다.");
            return;
        }

        if (string.IsNullOrWhiteSpace(shop.ShopName) || shop.Greetings.Count == 0 || string.IsNullOrWhiteSpace(shop.Farewell))
        {
            error($"{id} : 가게 이름 · 인사 · 마감 인사가 비어 있습니다.");
        }

        if (shop.OpenLocationIds.Count == 0)
        {
            error($"{id} : 영업 위치가 없습니다.");
        }

        foreach (string location in shop.OpenLocationIds)
        {
            if (!database.HasLocation(location))
            {
                error($"{id} : 영업 위치 {location} 이(가) NPC 위치 목록에 없습니다.");
            }
        }

        // 영업 시간 · 휴무일 (일정 데이터) : 계절마다 영업하는 날이 있어야 한다
        List<string> week = new List<string>();

        for (int season = 0; season < 4; season++)
        {
            int openDays = 0;

            for (int weekday = 0; weekday < NpcCalendar.DaysPerWeek; weekday++)
            {
                int day = season * 28 + weekday + 1;
                NpcScheduleData.Plan plan = owner.Schedule.SelectPlan(day, (SeasonType)season, WeatherType.Clear);
                List<(float start, float end)> intervals = NpcShopManager.ComputeOpenIntervals(shop, plan);
                openDays += intervals.Count > 0 ? 1 : 0;

                if (season == 0)
                {
                    week.Add($"{WeekdayNames[weekday]} {NpcShopManager.FormatIntervals(intervals)}");
                }
            }

            if (openDays == 0)
            {
                error($"{id} : {(SeasonType)season} 에 영업하는 날이 없습니다 (일정의 영업 위치 확인).");
            }
        }

        report.AppendLine($"{id} ({owner.DisplayName} · {shop.ShopName}) 봄 맑은 날 : {string.Join(" / ", week)}");

        // 판매 목록
        HashSet<ItemData> seen = new HashSet<ItemData>();
        int[] regularPerSeason = new int[4];

        foreach (NpcShopData.StockEntry entry in shop.Stock)
        {
            if (entry == null || entry.Item == null)
            {
                error($"{id} : 판매 목록에 빈 아이템이 있습니다.");
                continue;
            }

            if (!seen.Add(entry.Item))
            {
                error($"{id} : 판매 목록 중복 {entry.Item.ItemId}");
            }

            if (entry.DailyChance <= 0f)
            {
                error($"{id} : {entry.Item.ItemId} 의 등장 확률이 0입니다.");
            }

            for (int season = 0; season < 4; season++)
            {
                if (entry.IsSoldIn((SeasonType)season) && !entry.IsSpecial && entry.RequiredStage == AffinityStage.Uninterested)
                {
                    regularPerSeason[season]++;
                }
            }
        }

        for (int season = 0; season < 4; season++)
        {
            if (regularPerSeason[season] < 3)
            {
                error($"{id} : {(SeasonType)season} 에 매일 파는 물건이 3종보다 적습니다 ({regularPerSeason[season]}종).");
            }
        }

        // 할인율 : 단계가 오를수록 같거나 커야 한다
        int previous = 0;

        for (int stage = 0; stage < 5; stage++)
        {
            int discount = shop.GetDiscountPercent((AffinityStage)stage);

            if (discount < previous || discount > 50)
            {
                error($"{id} : 할인율은 0~50%이고 단계가 오를수록 같거나 커야 합니다.");
                break;
            }

            previous = discount;
        }

        // 사 주는 물건
        if (shop.BuyRules.Count > 0 && shop.DailyBuyLimit <= 0)
        {
            error($"{id} : 사 주는 규칙이 있는데 하루 매입 수가 0입니다.");
        }

        foreach (NpcShopData.BuyRule rule in shop.BuyRules)
        {
            if (rule == null)
            {
                error($"{id} : 빈 매입 규칙이 있습니다.");
                continue;
            }

            if (rule.Item != null && !catalog.TryGetPrice(rule.Item, out _))
            {
                error($"{id} : {rule.Item.ItemId} 는 판매 가격표에 없어 사 줄 수 없습니다.");
            }
        }

        report.AppendLine($"   판매 {shop.Stock.Count}종 · 매입 {shop.BuyRules.Count}규칙 (하루 {shop.DailyBuyLimit}개) · 최대 할인 {shop.MaxDiscountPercent}%");
    }

    // 가장 싸게 살 수 있는 값(최대 할인)이 판매 상자 · 모든 상점 매입 값(제철 포함)보다 커야 되팔기 이익이 없다
    private static void ValidateResale(List<NpcShopData> shops, MarketCatalogData catalog, Action<string> error)
    {
        foreach (NpcShopData shop in shops)
        {
            foreach (NpcShopData.StockEntry entry in shop.Stock)
            {
                if (entry?.Item == null)
                {
                    continue;
                }

                int resale = 0;

                for (int season = 0; season < 4; season++)
                {
                    MarketPriceQuote quote = catalog.GetQuote(entry.Item, (SeasonType)season);

                    if (!quote.Sellable)
                    {
                        continue;
                    }

                    resale = Mathf.Max(resale, quote.UnitPrice); // 판매 상자

                    foreach (NpcShopData buyer in shops)
                    {
                        NpcShopData.BuyRule rule = buyer.FindBuyRule(entry.Item, quote.GoodsType);

                        if (rule != null)
                        {
                            resale = Mathf.Max(resale, NpcShopManager.BuybackPrice(quote.UnitPrice, rule.Multiplier));
                        }
                    }
                }

                int cheapest = NpcShopData.DiscountedPrice(entry.Price, shop.MaxDiscountPercent);

                if (resale > 0 && cheapest <= resale)
                {
                    error($"{shop.ShopId} : {entry.Item.ItemId} 가장 싼 값 {cheapest} ≤ 되팔 값 {resale} (되팔기 이익)");
                }
            }
        }
    }

    // 작물 씨앗은 재배 계절에 NPC 상점에서 살 수 있어야 한다 (87일차 떠돌이 상인 대신 리첼)
    private static void ValidateSeeds(List<NpcShopData> shops, Action<string> error)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:CropData", new[] { "Assets/_ProjectU/Data" }))
        {
            CropData crop = AssetDatabase.LoadAssetAtPath<CropData>(AssetDatabase.GUIDToAssetPath(guid));

            if (crop == null || crop.SeedItem == null)
            {
                continue;
            }

            bool sold = false;

            foreach (SeasonType season in crop.GrowingSeasons)
            {
                sold |= shops.Any(shop => shop.Stock.Any(entry => entry != null && entry.Item == crop.SeedItem && entry.IsSoldIn(season)));
            }

            if (!sold)
            {
                error($"{crop.CropId} : 재배 계절에 씨앗을 파는 NPC 상점이 없습니다.");
            }
        }
    }

    private static void ValidateScene(List<NpcShopData> shops, StringBuilder report, Action<string> error)
    {
        if (EditorSceneManager.GetActiveScene().path != ScenePath)
        {
            report.AppendLine("Scene 검사 생략 (게임 Scene이 열려 있지 않음)");
            return;
        }

        NpcManager npcManager = Object.FindFirstObjectByType<NpcManager>(FindObjectsInactive.Include);
        NpcShopManager shopManager = npcManager != null ? npcManager.GetComponent<NpcShopManager>() : null;

        if (shopManager == null)
        {
            error("NPC 관리자에 상점 관리자가 없습니다. 10번 메뉴를 실행하세요.");
            return;
        }

        SerializedObject serialized = new SerializedObject(shopManager);

        if (serialized.FindProperty("npcManager").objectReferenceValue != npcManager || serialized.FindProperty("relations").objectReferenceValue == null)
        {
            error("상점 관리자의 NPC 관리자 · 관계 관리자 연결이 비어 있습니다.");
        }

        HashSet<NpcShopData> assigned = new HashSet<NpcShopData>(shopManager.Shops.Where(shop => shop != null));

        foreach (NpcShopData shop in shops.Where(shop => !assigned.Contains(shop)))
        {
            error($"상점 관리자에 {shop.ShopId} 이(가) 연결되지 않았습니다.");
        }

        foreach (NpcShopData shop in shops)
        {
            if (npcManager.FindAgent(shop.OwnerId) == null)
            {
                error($"{shop.ShopId} : 주인 {shop.OwnerId} 이(가) Scene에 배치되지 않았습니다.");
            }

            foreach (string location in shop.OpenLocationIds)
            {
                if (!npcManager.Locations.Any(point => point != null && point.LocationId == location))
                {
                    error($"{shop.ShopId} : 영업 위치 {location} 표시 지점이 Scene에 없습니다.");
                }
            }
        }

        GameUIManager uiManager = Object.FindFirstObjectByType<GameUIManager>(FindObjectsInactive.Include);
        ShopPopupUI popup = uiManager != null ? uiManager.ShopPopup : null;

        if (popup == null || new SerializedObject(popup).FindProperty("speechText").objectReferenceValue == null)
        {
            error("GameUIManager의 상인 창이 없거나 NPC 한마디 칸이 없습니다. 10번 메뉴를 다시 실행하세요.");
        }

        if (uiManager != null && uiManager.NpcDialoguePopup == null)
        {
            error("NPC 대화 창이 없어 거래 버튼을 쓸 수 없습니다. 9번 메뉴를 실행하세요.");
        }

        if (Object.FindFirstObjectByType<MarketManager>(FindObjectsInactive.Include) == null)
        {
            error("상점 관리자(MarketManager · 코인)가 없습니다. 6번 메뉴를 실행하세요.");
        }

        report.AppendLine($"Scene : 상점 관리자 상점 {assigned.Count}곳 연결");
    }
}
