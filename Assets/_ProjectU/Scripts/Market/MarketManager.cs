using System; // 이벤트·문자열 비교 기능
using System.Collections.Generic; // 목록 기능
using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class MarketManager : MonoBehaviour // 87일차: 판매 상자 밤사이 판매 · 상인 재고 · 가격 계산
{
    [Header("Time")] // 시간 참조
    [Tooltip("현재 날짜·시각을 제공하는 낮밤 순환입니다.")]
    [SerializeField] private DayNightCycle dayNightCycle; // 날짜 제공
    [Tooltip("제철 가격과 계절 재고에 쓰는 계절 순환입니다.")]
    [SerializeField] private SeasonCycle seasonCycle; // 계절 제공

    [Header("Data")] // 데이터
    [Tooltip("판매 가격표·상인 재고·가격 규칙.")]
    [SerializeField] private MarketCatalogData catalog; // 가격표
    [Tooltip("코인을 받을 지갑. 비어 있으면 현재 플레이어 지갑을 사용합니다.")]
    [SerializeField] private PlayerWallet wallet; // 지갑

    private static readonly List<ShippingBin> activeBins = new List<ShippingBin>(); // 활성 판매 상자
    private static MarketManager instance; // 현재 Scene 관리자
    private readonly List<ShopOffer> offers = new List<ShopOffer>(); // 오늘 재고
    private readonly Dictionary<ItemData, int> saleBuffer = new Dictionary<ItemData, int>(); // 판매 집계 버퍼
    private int lastKnownDay = int.MinValue; // 마지막 확인 날짜
    private int stockDay = int.MinValue; // 재고를 만든 날짜
    private int totalCoinsEarned; // 누적 수입
    private int totalItemsSold; // 누적 판매 수

    public static MarketManager Instance // 현재 관리자 제공
    {
        get
        {
            if (instance == null) // 등록 전 조회
            {
                instance = FindFirstObjectByType<MarketManager>(); // Scene 검색
            }

            return instance; // 결과 반환
        }
    }

    public static IReadOnlyList<ShippingBin> ActiveBins => activeBins; // 활성 판매 상자 제공
    public MarketCatalogData Catalog => catalog; // 가격표 제공
    public IReadOnlyList<ShopOffer> Offers => offers; // 오늘 재고 제공
    public int CurrentDay => dayNightCycle != null ? dayNightCycle.CurrentDay : 1; // 날짜 제공
    public float CurrentHour => dayNightCycle != null ? dayNightCycle.CurrentHour : 12f; // 시각 제공
    public SeasonType CurrentSeason => seasonCycle != null ? seasonCycle.CurrentSeason : SeasonType.Spring; // 계절 제공
    public bool IsShopOpen => catalog != null && catalog.IsOpenAt(CurrentHour); // 상인 영업 여부
    public int StockDay => stockDay; // 재고 날짜 제공
    public int LastProcessedDay => lastKnownDay; // 처리 날짜 제공
    public int TotalCoinsEarned => totalCoinsEarned; // 누적 수입 제공
    public int TotalItemsSold => totalItemsSold; // 누적 판매 수 제공
    public ShippingSaleReport LastReport { get; private set; } // 마지막 판매 결과
    public PlayerWallet Wallet => wallet != null ? wallet : PlayerWallet.Local; // 지갑 제공

    public event Action<ShippingSaleReport> SaleCompleted; // 판매 완료 알림
    public event Action StockChanged; // 재고 변경 알림

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] // 도메인 재로드 없는 Play 대비
    private static void ResetStatics() // 정적 상태 초기화
    {
        activeBins.Clear(); // 목록 초기화
        instance = null; // 참조 초기화
    }

    public static void RegisterBin(ShippingBin bin) // 판매 상자 등록
    {
        if (bin != null && !activeBins.Contains(bin)) // 중복 확인
        {
            activeBins.Add(bin); // 추가
        }
    }

    public static void UnregisterBin(ShippingBin bin) // 판매 상자 해제
    {
        activeBins.Remove(bin); // 제거
    }

    private void Awake() // 초기화
    {
        if (instance != null && instance != this) // 중복 확인
        {
            Debug.LogWarning("MarketManager가 여러 개 있습니다. 먼저 등록된 관리자를 사용합니다.", this); // 경고
            enabled = false; // 비활성화
            return; // 중단
        }

        instance = this; // 등록

        if (dayNightCycle == null) // 날짜 참조 확인
        {
            dayNightCycle = FindFirstObjectByType<DayNightCycle>(); // Scene 검색
        }

        if (seasonCycle == null) // 계절 참조 확인
        {
            seasonCycle = FindFirstObjectByType<SeasonCycle>(); // Scene 검색
        }

        if (catalog == null) // 가격표 확인
        {
            Debug.LogError("MarketManager에 가격표가 없습니다. Tools > Project U > Build Content > 6. Market를 실행하세요.", this); // 오류
        }
    }

    private void Start() // 첫 재고 (계절 적용 뒤)
    {
        lastKnownDay = CurrentDay; // 시작 날짜

        if (stockDay != CurrentDay) // 불러오기로 이미 재고가 있으면 생략
        {
            RefreshStock(CurrentDay); // 오늘 재고
        }
    }

    private void OnDisable() // 해제
    {
        if (instance == this) // 현재 관리자 확인
        {
            instance = null; // 해제
        }
    }

    private void LateUpdate() // 날짜 변경을 프레임 끝에 한 번 처리
    {
        int day = CurrentDay; // 현재 날짜

        if (lastKnownDay == int.MinValue) // 시작 전
        {
            lastKnownDay = day; // 기록
            return; // 생략
        }

        if (day == lastKnownDay) // 변경 없음
        {
            return; // 생략
        }

        lastKnownDay = day; // 기록
        ProcessDay(day); // 처리
    }

    public void ProcessDay(int today) // 새 날 : 판매 상자 판매 → 새 재고
    {
        ShippingSaleReport report = SellAllBins(today); // 판매
        RefreshStock(today); // 새 재고

        if (report.HasSales || report.UnsoldItems > 0) // 알림 대상
        {
            LastReport = report; // 기록
            SaleCompleted?.Invoke(report); // 알림
        }
    }

    // ------------------------------------------------------------ 가격

    public MarketPriceQuote GetQuote(ItemData item) // 현재 계절 가격
    {
        return catalog != null ? catalog.GetQuote(item, CurrentSeason) : new MarketPriceQuote(item, false, 0, 0, false, MarketGoodsType.Supply); // 결과 반환
    }

    public void EstimateBins(out int coins, out int sellable, out int unsellable) // 모든 판매 상자의 예상 수입
    {
        CollectBins(out unsellable); // 집계
        coins = 0; // 초기화
        sellable = 0; // 초기화

        foreach (KeyValuePair<ItemData, int> pair in saleBuffer) // 아이템 순회
        {
            MarketPriceQuote quote = GetQuote(pair.Key); // 가격
            coins += catalog.GetBatchValue(quote.UnitPrice, pair.Value); // 금액
            sellable += pair.Value; // 수량
        }
    }

    private void CollectBins(out int unsellable) // 판매 상자 내용 집계 (팔 수 있는 것만 버퍼에)
    {
        saleBuffer.Clear(); // 초기화
        unsellable = 0; // 초기화

        foreach (ShippingBin bin in activeBins) // 상자 순회
        {
            StorageContainer container = bin != null ? bin.Container : null; // 보관함

            if (container == null) // 확인
            {
                continue; // 다음
            }

            for (int index = 0; index < container.SlotCapacity; index++) // 칸 순회
            {
                InventorySlot slot = container.GetSlot(index); // 칸

                if (slot == null || slot.ItemData == null || slot.Quantity <= 0) // 빈 칸
                {
                    continue; // 다음
                }

                if (catalog == null || !catalog.TryGetPrice(slot.ItemData, out _)) // 팔 수 없음
                {
                    unsellable += slot.Quantity; // 기록
                    continue; // 다음
                }

                saleBuffer.TryGetValue(slot.ItemData, out int count); // 기존 수량
                saleBuffer[slot.ItemData] = count + slot.Quantity; // 합산
            }
        }
    }

    private ShippingSaleReport SellAllBins(int today) // 판매 상자 판매
    {
        ShippingSaleReport report = new ShippingSaleReport { Day = today }; // 결과
        PlayerWallet target = Wallet; // 지갑

        if (catalog == null || target == null) // 판매 불가
        {
            if (activeBins.Count > 0) // 상자가 있을 때만 경고
            {
                Debug.LogWarning("가격표 또는 플레이어 지갑이 없어 판매 상자를 처리하지 못했습니다.", this); // 경고
            }

            return report; // 빈 결과
        }

        CollectBins(out int unsellable); // 집계
        report.UnsoldItems = unsellable; // 남은 수량

        foreach (KeyValuePair<ItemData, int> pair in saleBuffer) // 아이템 순회
        {
            MarketPriceQuote quote = GetQuote(pair.Key); // 가격
            int coins = catalog.GetBatchValue(quote.UnitPrice, pair.Value); // 금액
            report.Lines.Add(new ShippingSaleLine { Item = pair.Key, Quantity = pair.Value, Coins = coins }); // 기록
            report.ItemsSold += pair.Value; // 합계
            report.Coins += coins; // 합계
        }

        report.Lines.Sort((left, right) => right.Coins.CompareTo(left.Coins)); // 비싼 순

        foreach (ShippingBin bin in activeBins) // 팔린 아이템 비우기
        {
            if (bin != null)
            {
                bin.RemoveSold(catalog); // 비우기
            }
        }

        if (report.Coins > 0) // 코인 지급
        {
            target.Add(report.Coins); // 지급
            totalCoinsEarned = (int)Math.Min((long)totalCoinsEarned + report.Coins, int.MaxValue); // 누적
            totalItemsSold = (int)Math.Min((long)totalItemsSold + report.ItemsSold, int.MaxValue); // 누적
        }

        return report; // 결과 반환
    }

    // ------------------------------------------------------------ 상인

    public void RefreshStock(int day) // 날짜에 맞는 재고 만들기
    {
        offers.Clear(); // 초기화
        stockDay = day; // 기록

        if (catalog == null) // 확인
        {
            StockChanged?.Invoke(); // 알림
            return; // 생략
        }

        System.Random random = new System.Random(unchecked(day * 7919 + catalog.StockSeed)); // 같은 날이면 같은 재고
        SeasonType season = CurrentSeason; // 계절

        foreach (MarketStockEntry entry in catalog.Stock) // 후보 순회
        {
            if (entry == null || entry.Item == null) // 확인
            {
                continue; // 다음
            }

            float roll = (float)random.NextDouble(); // 등장 판정 (계절과 무관하게 항상 뽑아 순서를 고정)

            if (!entry.IsSoldIn(season) || roll >= entry.DailyChance) // 계절·확률
            {
                continue; // 오늘은 없음
            }

            offers.Add(new ShopOffer(entry, entry.DailyStock)); // 추가
        }

        StockChanged?.Invoke(); // 알림
    }

    public bool TryBuy(ShopOffer offer, int quantity, PlayerInventory inventory, out string message) // 상인에게서 사기
    {
        PlayerWallet target = Wallet; // 지갑

        if (offer == null || inventory == null || target == null || !offers.Contains(offer)) // 요청 확인
        {
            message = "CANNOT TRADE RIGHT NOW"; // 문구
            return false; // 실패
        }

        if (!IsShopOpen) // 영업 시간
        {
            message = "THE MERCHANT HAS GONE HOME"; // 문구
            return false; // 실패
        }

        if (quantity <= 0 || offer.SoldOut) // 재고 확인
        {
            message = $"{offer.Item.DisplayName} IS SOLD OUT"; // 문구
            return false; // 실패
        }

        if (quantity > offer.Remaining) // 재고 부족
        {
            message = $"ONLY {offer.Remaining} LEFT"; // 문구
            return false; // 실패
        }

        int cost = offer.Price * quantity; // 가격

        if (!target.CanAfford(cost)) // 코인 확인
        {
            message = $"NEED {cost - target.Coins} MORE COINS"; // 문구
            return false; // 실패
        }

        if (!inventory.CanAddItem(offer.Item, quantity)) // 가방 공간
        {
            message = "NOT ENOUGH ROOM IN YOUR BAG"; // 문구
            return false; // 실패
        }

        if (!target.TrySpend(cost)) // 지불
        {
            message = "PAYMENT FAILED"; // 문구
            return false; // 실패
        }

        int left = inventory.AddItem(offer.Item, quantity); // 가방에 넣기

        if (left > 0) // 예외적으로 못 넣은 수량은 환불
        {
            target.Add(left * offer.Price); // 환불
        }

        int bought = quantity - left; // 산 수량
        offer.Remaining -= bought; // 재고 차감
        StockChanged?.Invoke(); // 알림
        message = $"BOUGHT {bought} {offer.Item.DisplayName}"; // 문구
        return bought > 0; // 결과 반환
    }

    // ------------------------------------------------------------ 저장

    public MarketSaveData CaptureSaveData() // 저장 데이터 생성
    {
        MarketSaveData data = new MarketSaveData
        {
            coins = Wallet != null ? Wallet.Coins : 0,
            lastProcessedDay = lastKnownDay == int.MinValue ? CurrentDay : lastKnownDay,
            stockDay = stockDay == int.MinValue ? CurrentDay : stockDay,
            totalCoinsEarned = totalCoinsEarned,
            totalItemsSold = totalItemsSold
        }; // 기본

        foreach (ShopOffer offer in offers) // 재고 순회
        {
            data.stock.Add(new MarketStockSaveData { itemId = offer.Item.ItemId, remaining = offer.Remaining }); // 추가
        }

        return data; // 결과 반환
    }

    public void ApplySaveData(MarketSaveData data, int loadedDay) // 저장 데이터 적용 (시간 적용 전 호출)
    {
        totalCoinsEarned = Mathf.Max(0, data.totalCoinsEarned); // 누적 수입
        totalItemsSold = Mathf.Max(0, data.totalItemsSold); // 누적 판매 수
        LastReport = null; // 판매 결과 초기화
        RefreshStockForLoad(loadedDay); // 불러온 날의 재고

        if (data.stockDay == loadedDay) // 같은 날 저장 → 남은 수량 적용
        {
            foreach (ShopOffer offer in offers) // 재고 순회
            {
                MarketStockSaveData saved = data.stock.Find(entry => entry != null && string.Equals(entry.itemId, offer.Item.ItemId, StringComparison.Ordinal)); // 저장 찾기
                offer.Remaining = saved != null ? Mathf.Clamp(saved.remaining, 0, offer.DailyStock) : offer.Remaining; // 적용
            }
        }

        SyncDayForLoad(loadedDay); // 날짜 기록
        StockChanged?.Invoke(); // 알림
    }

    public void ResetForLoad(int loadedDay) // 이전 저장 파일 : 기록 없이 오늘부터
    {
        totalCoinsEarned = 0; // 초기화
        totalItemsSold = 0; // 초기화
        LastReport = null; // 초기화
        RefreshStockForLoad(loadedDay); // 재고
        SyncDayForLoad(loadedDay); // 날짜 기록
    }

    private void RefreshStockForLoad(int loadedDay) // 시간 적용 전이므로 저장 날짜의 계절로 재고 만들기
    {
        if (seasonCycle != null) // 계절 확인
        {
            seasonCycle.SetCurrentDay(loadedDay); // 불러올 날짜 계절 미리 적용
        }

        RefreshStock(loadedDay); // 재고
    }

    public void SyncDayForLoad(int loadedDay) // 불러온 날짜를 이미 처리한 날짜로 기록
    {
        lastKnownDay = loadedDay; // 기록
    }

    // ------------------------------------------------------------ 테스트

    public ShippingSaleReport DebugSellNow() // 테스트용 : 날짜를 넘기지 않고 지금 판매
    {
        ShippingSaleReport report = SellAllBins(CurrentDay); // 판매

        if (report.HasSales || report.UnsoldItems > 0) // 알림
        {
            LastReport = report; // 기록
            SaleCompleted?.Invoke(report); // 알림
        }

        return report; // 결과 반환
    }
}
