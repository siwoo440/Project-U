using System; // Serializable · 이벤트
using System.Collections.Generic; // 목록
using System.Text; // 문자열
using UnityEngine; // Unity 기본 기능

[Serializable] // JSON 저장
public sealed class NpcShopStockSaveData // 92일차: NPC 상점 재고 한 칸
{
    [Tooltip("아이템 ID.")]
    public string itemId = string.Empty; // 아이템 ID
    [Tooltip("남은 수량.")]
    public int remaining; // 남은 수량
}

[Serializable] // JSON 저장
public sealed class NpcShopStateSaveData // 92일차: NPC 상점 한 곳의 오늘 재고 · 매입 기록
{
    [Tooltip("상점 ID.")]
    public string shopId = string.Empty; // 상점 ID
    [Tooltip("재고를 채운 날짜.")]
    public int stockDay; // 재고 날짜
    [Tooltip("오늘 재고의 남은 수량.")]
    public List<NpcShopStockSaveData> stock = new List<NpcShopStockSaveData>(); // 재고
    [Tooltip("마지막으로 플레이어 물건을 사 준 날짜.")]
    public int buybackDay = -1; // 매입 날짜
    [Tooltip("그날 사 준 수량.")]
    public int boughtFromPlayer; // 매입 수량
    [Tooltip("플레이어가 이 가게에서 쓴 코인 합계.")]
    public int totalCoinsSpent; // 구매 합계
    [Tooltip("플레이어가 이 가게에 팔아 받은 코인 합계.")]
    public int totalCoinsEarned; // 판매 합계
}

[Serializable] // JSON 저장
public sealed class NpcShopSaveData // 92일차: 전체 NPC 상점 저장
{
    [Tooltip("상점별 상태.")]
    public List<NpcShopStateSaveData> shops = new List<NpcShopStateSaveData>(); // 상점 목록
}

public sealed class NpcShopOffer : ShopOffer // 92일차: NPC 상점이 오늘 파는 물건 한 칸
{
    public NpcShopOffer(NpcShopData.StockEntry entry, int remaining) : base(entry.Item, entry.Price, entry.DailyStock, entry.IsSpecial, remaining)
    {
        StockEntry = entry;
    }

    public NpcShopData.StockEntry StockEntry { get; } // 원본 데이터
}

public readonly struct NpcShopStatus // 92일차: 지금 가게 상태
{
    public readonly bool IsOpen; // 거래 가능
    public readonly string Reason; // 닫힌 이유 (거래 가능하면 빈 문자열)
    public readonly string TodayHours; // 오늘 영업 시간 ("08:00 ~ 18:00", "쉬는 날")

    public NpcShopStatus(bool isOpen, string reason, string todayHours)
    {
        IsOpen = isOpen;
        Reason = reason;
        TodayHours = todayHours;
    }
}

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class NpcShopManager : MonoBehaviour // 92일차: NPC 상점 (영업 시간 · 재고 · 하루 재고 채우기 · 사고팔기 · 저장)
{
    public static NpcShopManager Instance { get; private set; } // Scene 관리자

    private const int LookAheadDays = 7; // 다음 영업일을 찾는 범위
    private static readonly string[] WeekdayNames = { "월", "화", "수", "목", "금", "토", "일" };

    [Tooltip("NPC 상점 데이터.")]
    [SerializeField] private List<NpcShopData> shops = new List<NpcShopData>(); // 상점
    [Tooltip("NPC 일정 · 시간 · 날씨.")]
    [SerializeField] private NpcManager npcManager; // NPC 관리자
    [Tooltip("호감도 (할인 · 잠긴 물건).")]
    [SerializeField] private NpcRelationshipManager relations; // 관계

    private sealed class ShopState // 상점 한 곳의 실행 상태
    {
        public NpcShopData Data;
        public readonly List<ShopOffer> Offers = new List<ShopOffer>();
        public int StockDay = int.MinValue;
        public int BuybackDay = -1;
        public int BoughtFromPlayer;
        public int TotalSpent;
        public int TotalEarned;
    }

    private readonly Dictionary<string, ShopState> states = new Dictionary<string, ShopState>(StringComparer.Ordinal); // 상점 ID → 상태
    private int lastDay = int.MinValue; // 마지막으로 재고를 채운 날짜

    public event Action StockChanged; // 재고 · 매입 변경
    public event Action<NpcShopData, ItemData, int, int> Traded; // 거래 (상점, 아이템, 수량 : 산 수 + / 판 수 -, 코인 변화)

    public IReadOnlyList<NpcShopData> Shops => shops; // 상점 제공
    public int CurrentDay => npcManager != null ? npcManager.CurrentDay : 1; // 날짜
    public float CurrentHour => npcManager != null ? npcManager.CurrentHour : 12f; // 시각
    public SeasonType CurrentSeason => npcManager != null ? npcManager.CurrentSeason : SeasonType.Spring; // 계절
    public WeatherType CurrentWeather => npcManager != null ? npcManager.CurrentWeather : WeatherType.Clear; // 날씨
    private static PlayerWallet Wallet => MarketManager.Instance != null && MarketManager.Instance.Wallet != null ? MarketManager.Instance.Wallet : PlayerWallet.Local; // 지갑

    private void Awake() // 준비
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("NpcShopManager가 두 개 있습니다. 나중 것은 사용하지 않습니다.", this);
            enabled = false;
            return;
        }

        Instance = this;

        if (npcManager == null) npcManager = GetComponent<NpcManager>();
        if (relations == null) relations = GetComponent<NpcRelationshipManager>();

        if (npcManager == null)
        {
            Debug.LogError("NpcShopManager에 NPC 관리자가 없습니다. Build Content > 10. NPC Shops를 다시 실행하세요.", this);
        }

        BuildStates();
    }

    private void OnDestroy() // 정리
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start() // 첫 재고 (불러오기로 이미 채웠으면 생략)
    {
        if (lastDay != CurrentDay)
        {
            RefreshAll(CurrentDay);
        }
    }

    private void LateUpdate() // 날짜가 바뀌면 모든 가게 재고 채우기
    {
        if (npcManager != null && CurrentDay != lastDay)
        {
            RefreshAll(CurrentDay);
        }
    }

    private void BuildStates() // 상점 ID별 상태
    {
        states.Clear();

        foreach (NpcShopData shop in shops)
        {
            if (shop != null && !string.IsNullOrEmpty(shop.ShopId) && !states.ContainsKey(shop.ShopId))
            {
                states.Add(shop.ShopId, new ShopState { Data = shop });
            }
        }
    }

    // ------------------------------------------------------------ 찾기

    public bool TryGetShop(NpcCharacterData character, out NpcShopData shop) // NPC의 상점 (캐릭터 데이터 shopId 또는 주인 ID)
    {
        shop = null;

        if (character == null)
        {
            return false;
        }

        foreach (NpcShopData candidate in shops)
        {
            if (candidate != null && (candidate.ShopId == character.Ids.shopId || candidate.OwnerId == character.CharacterId))
            {
                shop = candidate;
                return true;
            }
        }

        return false;
    }

    public NpcShopData FindShop(string shopId) // ID로 상점
    {
        return !string.IsNullOrEmpty(shopId) && states.TryGetValue(shopId, out ShopState state) ? state.Data : null;
    }

    public NpcCharacterData GetOwner(NpcShopData shop) // 상점 주인 데이터
    {
        NpcCharacterData owner = null;

        if (shop != null && npcManager != null && npcManager.Database != null)
        {
            npcManager.Database.TryGet(shop.OwnerId, out owner);
        }

        return owner;
    }

    public NpcAgent GetOwnerAgent(NpcShopData shop) // 상점 주인 NPC
    {
        return shop != null && npcManager != null ? npcManager.FindAgent(shop.OwnerId) : null;
    }

    private ShopState GetState(NpcShopData shop) // 상태 (오늘 재고가 없으면 채움)
    {
        if (shop == null || !states.TryGetValue(shop.ShopId, out ShopState state))
        {
            return null;
        }

        if (state.StockDay != CurrentDay && npcManager != null)
        {
            RefreshStock(state, CurrentDay, npcManager.GetSeasonForDay(CurrentDay));
        }

        return state;
    }

    // ------------------------------------------------------------ 영업 시간 (일정 데이터 기준)

    public List<(float start, float end)> GetOpenIntervals(NpcShopData shop, int day, WeatherType weather) // 그날 주인이 영업 위치에 있는 시간
    {
        return ComputeOpenIntervals(shop, GetPlan(shop, day, weather));
    }

    public static List<(float start, float end)> ComputeOpenIntervals(NpcShopData shop, NpcScheduleData.Plan plan) // 계획에서 영업 위치에 있는 시간 구간 (검사 도구도 사용)
    {
        List<(float start, float end)> result = new List<(float, float)>();

        if (shop == null || plan == null || plan.Stops.Count == 0)
        {
            return result;
        }

        IReadOnlyList<NpcScheduleData.Stop> stops = plan.Stops;

        if (shop.IsOpenLocation(stops[stops.Count - 1].LocationId) && stops[0].Hour > 0f)
        {
            result.Add((0f, stops[0].Hour)); // 자정을 넘겨 이어지는 영업
        }

        for (int index = 0; index < stops.Count; index++)
        {
            if (!shop.IsOpenLocation(stops[index].LocationId))
            {
                continue;
            }

            float start = stops[index].Hour;
            float end = index + 1 < stops.Count ? stops[index + 1].Hour : 24f;

            if (result.Count > 0 && Mathf.Approximately(result[result.Count - 1].end, start))
            {
                result[result.Count - 1] = (result[result.Count - 1].start, end); // 이어지는 칸 합치기
            }
            else if (end > start)
            {
                result.Add((start, end));
            }
        }

        return result;
    }

    public static string FormatIntervals(List<(float start, float end)> intervals) // "08:00 ~ 18:00" · "09:00 ~ 13:00, 14:00 ~ 18:00" · "쉬는 날"
    {
        if (intervals == null || intervals.Count == 0)
        {
            return "쉬는 날";
        }

        List<string> parts = new List<string>();

        foreach ((float start, float end) in intervals)
        {
            parts.Add($"{FormatHour(start)} ~ {FormatHour(end)}");
        }

        return string.Join(", ", parts);
    }

    public static string FormatHour(float hour) // 24시는 00:00 대신 24:00
    {
        return hour >= 24f ? "24:00" : MarketStall.FormatHour(hour);
    }

    public bool IsScheduledOpen(NpcShopData shop, int day, float hour, WeatherType weather) // 일정상 영업 시간인지
    {
        NpcScheduleData.Stop stop = GetPlan(shop, day, weather)?.GetStop(hour);
        return stop != null && shop.IsOpenLocation(stop.LocationId);
    }

    public bool IsScheduledOpenNow(NpcShopData shop) // 지금 영업 시간인지 (창이 열린 동안 확인)
    {
        return IsScheduledOpen(shop, CurrentDay, CurrentHour, CurrentWeather);
    }

    public string DescribeHours(NpcShopData shop, int day, WeatherType weather) // 그날 영업 시간 문구
    {
        return FormatIntervals(GetOpenIntervals(shop, day, weather));
    }

    public NpcShopStatus GetStatus(NpcShopData shop, NpcAgent owner) // 지금 거래할 수 있는지와 이유
    {
        if (shop == null || owner == null || npcManager == null)
        {
            return new NpcShopStatus(false, "가게 주인이 없어요.", string.Empty);
        }

        int day = CurrentDay;
        string hours = DescribeHours(shop, day, CurrentWeather);
        NpcScheduleData.Stop stop = GetPlan(shop, day, CurrentWeather)?.GetStop(CurrentHour);

        if (stop == null || !shop.IsOpenLocation(stop.LocationId) || owner.IsInside)
        {
            return new NpcShopStatus(false, ClosedReason(shop), hours);
        }

        if (!npcManager.TryGetLocation(stop.LocationId, out NpcLocationPoint point) || !npcManager.IsAnchorReady(point))
        {
            return new NpcShopStatus(false, "가판대가 있어야 물건을 펼칠 수 있어요. 상인 가판대를 지어 주세요.", hours);
        }

        if (owner.CurrentLocationId != stop.LocationId || !owner.HasArrived)
        {
            return new NpcShopStatus(false, "가게로 가는 중이에요. 도착하면 거래할 수 있어요.", hours);
        }

        return new NpcShopStatus(true, string.Empty, hours);
    }

    private string ClosedReason(NpcShopData shop) // 닫힌 이유 + 다음 영업 시각
    {
        int day = CurrentDay;
        float hour = CurrentHour;
        List<(float start, float end)> today = GetOpenIntervals(shop, day, CurrentWeather);
        bool finishedToday = today.Count > 0 && today[today.Count - 1].end <= hour + 0.001f;
        string head = today.Count == 0 ? "오늘은 쉬는 날이에요." : finishedToday ? "오늘 영업은 끝났어요." : "지금은 영업 시간이 아니에요.";

        for (int offset = 0; offset <= LookAheadDays; offset++)
        {
            int target = day + offset;
            WeatherType weather = offset == 0 ? CurrentWeather : WeatherType.Clear; // 앞날 날씨는 알 수 없으니 맑음 기준

            foreach ((float start, float end) in GetOpenIntervals(shop, target, weather))
            {
                if (offset == 0 && start <= hour)
                {
                    continue;
                }

                string when = offset == 0 ? string.Empty : offset == 1 ? "내일 " : $"{WeekdayName(target)}요일 ";
                return $"{head} {when}{FormatHour(start)}에 열어요.";
            }
        }

        return $"{head} 당분간 문을 열지 않아요.";
    }

    public static string WeekdayName(int day) // 1일차 = 월요일
    {
        return WeekdayNames[((Mathf.Max(1, day) - 1) % NpcCalendar.DaysPerWeek + NpcCalendar.DaysPerWeek) % NpcCalendar.DaysPerWeek];
    }

    private NpcScheduleData.Plan GetPlan(NpcShopData shop, int day, WeatherType weather) // 주인의 그날 계획
    {
        NpcCharacterData owner = GetOwner(shop);
        return owner != null && owner.Schedule != null && npcManager != null ? owner.Schedule.SelectPlan(day, npcManager.GetSeasonForDay(day), weather) : null;
    }

    // ------------------------------------------------------------ 재고

    public void RefreshAll(int day) // 모든 가게 하루 재고 채우기
    {
        SeasonType season = npcManager != null ? npcManager.GetSeasonForDay(day) : CurrentSeason;

        foreach (ShopState state in states.Values)
        {
            RefreshStock(state, day, season);
        }

        lastDay = day;
        StockChanged?.Invoke();
    }

    private static void RefreshStock(ShopState state, int day, SeasonType season) // 날짜 · 계절에 맞는 재고 (같은 날이면 항상 같음)
    {
        state.Offers.Clear();
        state.StockDay = day;
        System.Random random = new System.Random(unchecked(day * 7919 + state.Data.StockSeed));

        foreach (NpcShopData.StockEntry entry in state.Data.Stock)
        {
            if (entry == null || entry.Item == null)
            {
                continue;
            }

            float roll = (float)random.NextDouble(); // 계절과 관계없이 항상 뽑아 순서를 고정

            if (entry.IsSoldIn(season) && roll < entry.DailyChance)
            {
                state.Offers.Add(new NpcShopOffer(entry, entry.DailyStock));
            }
        }
    }

    public IReadOnlyList<ShopOffer> GetOffers(NpcShopData shop) // 오늘 파는 물건
    {
        ShopState state = GetState(shop);
        return state != null ? state.Offers : (IReadOnlyList<ShopOffer>)Array.Empty<ShopOffer>();
    }

    public AffinityStage GetOwnerStage(NpcShopData shop) // 주인과의 관계 단계
    {
        return relations != null ? relations.PeekStage(GetOwner(shop)) : AffinityStage.Uninterested;
    }

    public int GetDiscountPercent(NpcShopData shop) // 지금 할인율
    {
        return shop != null ? shop.GetDiscountPercent(GetOwnerStage(shop)) : 0;
    }

    public int GetUnitPrice(NpcShopData shop, ShopOffer offer) // 할인 적용 가격
    {
        return offer != null && shop != null ? NpcShopData.DiscountedPrice(offer.Price, GetDiscountPercent(shop)) : 0;
    }

    public string GetLockReason(NpcShopData shop, ShopOffer offer) // 관계 단계가 부족하면 이유
    {
        if (!(offer is NpcShopOffer npcOffer) || npcOffer.StockEntry.RequiredStage <= GetOwnerStage(shop))
        {
            return null;
        }

        return $"'{NpcDialogueSelector.StageName(npcOffer.StockEntry.RequiredStage)}' 단계부터 살 수 있어요";
    }

    public bool TryBuy(NpcShopData shop, ShopOffer offer, int quantity, PlayerInventory inventory, out string message) // 가게에서 사기
    {
        ShopState state = GetState(shop);
        PlayerWallet wallet = Wallet;

        if (state == null || offer == null || inventory == null || wallet == null || !state.Offers.Contains(offer))
        {
            message = "CANNOT TRADE RIGHT NOW";
            return false;
        }

        if (!IsScheduledOpenNow(shop))
        {
            message = "THE SHOP IS CLOSED";
            return false;
        }

        string locked = GetLockReason(shop, offer);

        if (locked != null)
        {
            message = locked;
            return false;
        }

        if (quantity <= 0 || offer.SoldOut)
        {
            message = $"{offer.Item.DisplayName} IS SOLD OUT";
            return false;
        }

        if (quantity > offer.Remaining)
        {
            message = $"ONLY {offer.Remaining} LEFT";
            return false;
        }

        int unit = GetUnitPrice(shop, offer);
        int cost = unit * quantity;

        if (!wallet.CanAfford(cost))
        {
            message = $"NEED {cost - wallet.Coins} MORE COINS";
            return false;
        }

        if (!inventory.CanAddItem(offer.Item, quantity))
        {
            message = "NOT ENOUGH ROOM IN YOUR BAG";
            return false;
        }

        if (!wallet.TrySpend(cost))
        {
            message = "PAYMENT FAILED";
            return false;
        }

        int left = inventory.AddItem(offer.Item, quantity);

        if (left > 0)
        {
            wallet.Add(left * unit); // 예외적으로 못 넣은 수량은 환불
        }

        int bought = quantity - left;
        offer.Remaining -= bought;
        state.TotalSpent = (int)Math.Min((long)state.TotalSpent + bought * unit, int.MaxValue);
        StockChanged?.Invoke();
        Traded?.Invoke(shop, offer.Item, bought, -bought * unit);
        message = $"BOUGHT {bought} {offer.Item.DisplayName}";
        return bought > 0;
    }

    // ------------------------------------------------------------ 사 주기 (플레이어가 팔기)

    public int GetBuybackPrice(NpcShopData shop, ItemData item) // 한 개 매입 가격 (0 = 사 주지 않음)
    {
        MarketManager market = MarketManager.Instance;

        if (shop == null || item == null || market == null)
        {
            return 0;
        }

        MarketPriceQuote quote = market.GetQuote(item);

        if (!quote.Sellable)
        {
            return 0; // 판매 상자 가격표에 없는 물건 (도구 · 장비 등)
        }

        NpcShopData.BuyRule rule = shop.FindBuyRule(item, quote.GoodsType);
        return rule != null ? BuybackPrice(quote.UnitPrice, rule.Multiplier) : 0;
    }

    public static int BuybackPrice(int unitPrice, float multiplier) // 판매 상자 가격 × 배율 (최소 1)
    {
        return Mathf.Max(1, Mathf.RoundToInt(unitPrice * multiplier));
    }

    public int GetBuybackLeft(NpcShopData shop) // 오늘 더 사 줄 수 있는 수량
    {
        ShopState state = GetState(shop);

        if (state == null)
        {
            return 0;
        }

        int bought = state.BuybackDay == CurrentDay ? state.BoughtFromPlayer : 0;
        return Mathf.Max(0, shop.DailyBuyLimit - bought);
    }

    public string DescribeBuys(NpcShopData shop) // "CROP · ANIMAL PRODUCT · FORAGE" 식 매입 안내
    {
        if (shop == null || shop.BuyRules.Count == 0)
        {
            return string.Empty;
        }

        List<string> parts = new List<string>();

        foreach (NpcShopData.BuyRule rule in shop.BuyRules)
        {
            if (rule == null)
            {
                continue;
            }

            string label = rule.Item != null ? rule.Item.DisplayName : rule.AnyGoods ? "ANYTHING SELLABLE" : ShopPopupUI.GoodsLabel(rule.GoodsType);
            int percent = Mathf.RoundToInt(rule.Multiplier * 100f);
            parts.Add(percent == 100 ? label : $"{label} {percent}%");
        }

        return string.Join(" · ", parts);
    }

    public bool TrySell(NpcShopData shop, ItemData item, int quantity, PlayerInventory inventory, out string message) // 가게에 팔기
    {
        ShopState state = GetState(shop);
        PlayerWallet wallet = Wallet;

        if (state == null || item == null || inventory == null || wallet == null)
        {
            message = "CANNOT TRADE RIGHT NOW";
            return false;
        }

        if (!IsScheduledOpenNow(shop))
        {
            message = "THE SHOP IS CLOSED";
            return false;
        }

        int unit = GetBuybackPrice(shop, item);

        if (unit <= 0)
        {
            message = $"THEY DON'T BUY {item.DisplayName}";
            return false;
        }

        int left = GetBuybackLeft(shop);

        if (left <= 0)
        {
            message = "NO MORE BUYING TODAY";
            return false;
        }

        if (quantity <= 0 || inventory.GetItemQuantity(item) < quantity)
        {
            message = "NOT ENOUGH IN YOUR BAG";
            return false;
        }

        if (quantity > left)
        {
            message = $"ONLY {left} MORE TODAY";
            return false;
        }

        int removed = inventory.RemoveItem(item, quantity);

        if (removed <= 0)
        {
            message = "NOT ENOUGH IN YOUR BAG";
            return false;
        }

        int coins = unit * removed;
        wallet.Add(coins);

        if (state.BuybackDay != CurrentDay)
        {
            state.BuybackDay = CurrentDay;
            state.BoughtFromPlayer = 0;
        }

        state.BoughtFromPlayer += removed;
        state.TotalEarned = (int)Math.Min((long)state.TotalEarned + coins, int.MaxValue);
        StockChanged?.Invoke();
        Traded?.Invoke(shop, item, -removed, coins);
        message = $"SOLD {removed} {item.DisplayName}  +{coins}";
        return true;
    }

    // ------------------------------------------------------------ 저장

    public NpcShopSaveData CaptureSaveData() // 저장 데이터
    {
        NpcShopSaveData data = new NpcShopSaveData();

        foreach (ShopState state in states.Values)
        {
            bool boughtToday = state.BuybackDay == CurrentDay; // 지난 날짜 매입 기록은 저장하지 않음
            NpcShopStateSaveData saved = new NpcShopStateSaveData
            {
                shopId = state.Data.ShopId,
                stockDay = state.StockDay == int.MinValue ? CurrentDay : state.StockDay,
                buybackDay = boughtToday ? state.BuybackDay : -1,
                boughtFromPlayer = boughtToday ? state.BoughtFromPlayer : 0,
                totalCoinsSpent = state.TotalSpent,
                totalCoinsEarned = state.TotalEarned
            };

            foreach (ShopOffer offer in state.Offers)
            {
                saved.stock.Add(new NpcShopStockSaveData { itemId = offer.Item.ItemId, remaining = offer.Remaining });
            }

            data.shops.Add(saved);
        }

        data.shops.Sort((left, right) => string.CompareOrdinal(left.shopId, right.shopId));
        return data;
    }

    public void ApplySaveData(NpcShopSaveData data, int loadedDay) // 불러오기 (시간 적용 전 호출 : 저장된 날짜 기준)
    {
        SeasonType season = npcManager != null ? npcManager.GetSeasonForDay(loadedDay) : CurrentSeason;

        foreach (ShopState state in states.Values)
        {
            RefreshStock(state, loadedDay, season);
            state.BuybackDay = -1;
            state.BoughtFromPlayer = 0;
            state.TotalSpent = 0;
            state.TotalEarned = 0;
            NpcShopStateSaveData saved = data?.shops?.Find(entry => entry != null && entry.shopId == state.Data.ShopId);

            if (saved == null)
            {
                continue; // 새로 생긴 가게 : 오늘 재고 그대로
            }

            state.TotalSpent = Mathf.Max(0, saved.totalCoinsSpent);
            state.TotalEarned = Mathf.Max(0, saved.totalCoinsEarned);

            if (saved.stockDay == loadedDay) // 같은 날 저장 → 남은 수량
            {
                foreach (ShopOffer offer in state.Offers)
                {
                    NpcShopStockSaveData stock = saved.stock?.Find(entry => entry != null && entry.itemId == offer.Item.ItemId);
                    offer.Remaining = stock != null ? Mathf.Clamp(stock.remaining, 0, offer.DailyStock) : offer.Remaining;
                }
            }

            if (saved.buybackDay == loadedDay) // 같은 날 매입 기록
            {
                state.BuybackDay = loadedDay;
                state.BoughtFromPlayer = Mathf.Clamp(saved.boughtFromPlayer, 0, state.Data.DailyBuyLimit);
            }
        }

        lastDay = loadedDay;
        StockChanged?.Invoke();
    }

    public void ResetForLoad(int loadedDay) // 92일차 이전 저장 파일 : 그날 재고로 시작
    {
        ApplySaveData(null, loadedDay);
    }

    // ------------------------------------------------------------ 테스트

    public string Describe() // 디버그용 상태
    {
        StringBuilder text = new StringBuilder($"[NPC 상점] DAY {CurrentDay} ({WeekdayName(CurrentDay)}) {MarketStall.FormatHour(CurrentHour)} {CurrentSeason} {CurrentWeather}");

        foreach (ShopState state in states.Values)
        {
            NpcShopData shop = state.Data;
            NpcShopStatus status = GetStatus(shop, GetOwnerAgent(shop));
            text.Append($"\n{shop.ShopId} ({shop.ShopName}) : {(status.IsOpen ? "영업 중" : status.Reason)} · 오늘 {status.TodayHours} · 할인 {GetDiscountPercent(shop)}% · 매입 남은 {GetBuybackLeft(shop)}/{shop.DailyBuyLimit}");

            foreach (ShopOffer offer in GetOffers(shop))
            {
                string locked = GetLockReason(shop, offer);
                text.Append($"\n   {offer.Item.ItemId} {GetUnitPrice(shop, offer)}코인 (기본 {offer.Price}) 남은 {offer.Remaining}/{offer.DailyStock}{(offer.IsSpecial ? " 가끔" : string.Empty)}{(locked != null ? " 잠김" : string.Empty)}");
            }
        }

        return text.ToString();
    }

#if UNITY_EDITOR
    public void EditorAssign(List<NpcShopData> shopList, NpcManager manager, NpcRelationshipManager relationshipManager) // 생성 도구 전용
    {
        shops = shopList ?? new List<NpcShopData>();
        npcManager = manager;
        relations = relationshipManager;
        states.Clear();
    }
#endif
}
