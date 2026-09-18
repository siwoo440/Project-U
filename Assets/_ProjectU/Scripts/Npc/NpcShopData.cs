using System; // Serializable
using System.Collections.Generic; // 목록
using UnityEngine; // Unity 기본 기능

[CreateAssetMenu(fileName = "NpcShop_New", menuName = "Project U/NPC/Shop")] // NPC 상점 생성 메뉴
public sealed class NpcShopData : ScriptableObject // 92일차: NPC 상점 한 곳 (판매 목록 · 가격 · 사 주는 물건 · 영업 위치)
{
    [Serializable]
    public sealed class StockEntry // 파는 물건 한 종류
    {
        [Tooltip("파는 아이템.")]
        [SerializeField] private ItemData item; // 아이템
        [Tooltip("한 개 가격 (코인, 할인 전).")]
        [SerializeField, Min(1)] private int price = 1; // 가격
        [Tooltip("하루 재고 수 (날짜가 바뀌면 다시 채움).")]
        [SerializeField, Min(1)] private int dailyStock = 1; // 하루 재고
        [Tooltip("파는 계절. 비어 있으면 모든 계절에 팝니다.")]
        [SerializeField] private SeasonType[] seasons = new SeasonType[0]; // 판매 계절
        [Tooltip("그날 가게에 들어올 확률 (1 = 매일).")]
        [SerializeField, Range(0f, 1f)] private float dailyChance = 1f; // 등장 확률
        [Tooltip("이 관계 단계부터 살 수 있습니다.")]
        [SerializeField] private AffinityStage requiredStage = AffinityStage.Uninterested; // 필요 단계

        public StockEntry(ItemData item, int price, int dailyStock, SeasonType[] seasons, float dailyChance, AffinityStage requiredStage) // 생성 도구에서 사용
        {
            this.item = item;
            this.price = price;
            this.dailyStock = dailyStock;
            this.seasons = seasons ?? new SeasonType[0];
            this.dailyChance = dailyChance;
            this.requiredStage = requiredStage;
        }

        public ItemData Item => item; // 아이템 제공
        public int Price => Mathf.Max(1, price); // 가격 제공
        public int DailyStock => Mathf.Max(1, dailyStock); // 하루 재고 제공
        public IReadOnlyList<SeasonType> Seasons => seasons; // 판매 계절 제공
        public float DailyChance => Mathf.Clamp01(dailyChance); // 등장 확률 제공
        public AffinityStage RequiredStage => requiredStage; // 필요 단계 제공
        public bool IsSpecial => dailyChance < 1f; // 가끔만 들어오는 물건

        public bool IsSoldIn(SeasonType season) // 지정 계절 판매 여부
        {
            return seasons == null || seasons.Length == 0 || Array.IndexOf(seasons, season) >= 0;
        }
    }

    [Serializable]
    public sealed class BuyRule // 플레이어에게서 사 주는 물건 규칙 (판매 상자 가격 × 배율)
    {
        [Tooltip("이 아이템만 (비어 있으면 분류 규칙).")]
        [SerializeField] private ItemData item; // 아이템
        [Tooltip("모든 분류를 사 줍니다 (아이템이 비어 있을 때).")]
        [SerializeField] private bool anyGoods; // 모든 분류
        [Tooltip("사 주는 분류 (아이템이 비어 있고 모든 분류가 아닐 때).")]
        [SerializeField] private MarketGoodsType goodsType = MarketGoodsType.Crop; // 분류
        [Tooltip("판매 상자 가격에 곱하는 값.")]
        [SerializeField, Range(0.1f, 2f)] private float multiplier = 1f; // 배율

        public BuyRule(ItemData item, bool anyGoods, MarketGoodsType goodsType, float multiplier) // 생성 도구에서 사용
        {
            this.item = item;
            this.anyGoods = anyGoods;
            this.goodsType = goodsType;
            this.multiplier = multiplier;
        }

        public ItemData Item => item; // 아이템 제공
        public bool AnyGoods => anyGoods; // 모든 분류 여부 제공
        public MarketGoodsType GoodsType => goodsType; // 분류 제공
        public float Multiplier => Mathf.Clamp(multiplier, 0.1f, 2f); // 배율 제공
    }

    [Header("Identity")] // 식별 정보
    [Tooltip("상점 ID (캐릭터 데이터의 shopId와 같음).")]
    [SerializeField] private string shopId = "shop_new"; // 상점 ID
    [Tooltip("주인 캐릭터 ID.")]
    [SerializeField] private string ownerId = "char_new"; // 주인
    [Tooltip("가게 이름 (창 제목).")]
    [SerializeField] private string shopName = "새 가게"; // 가게 이름

    [Header("Hours")] // 영업
    [Tooltip("주인이 일정상 이 위치에 있을 때 영업합니다 (영업 시간 · 휴무일은 일정 데이터로 정해짐).")]
    [SerializeField] private string[] openLocationIds = new string[0]; // 영업 위치

    [Header("Stock")] // 파는 물건
    [SerializeField] private List<StockEntry> stock = new List<StockEntry>(); // 판매 목록
    [Tooltip("재고 뽑기 기준 값 (같은 날이면 항상 같은 재고).")]
    [SerializeField] private int stockSeed = 1; // 재고 기준 값
    [Tooltip("관계 단계별 할인율 % (무관심 · 호기심 · 신뢰 · 애정 · 사랑).")]
    [SerializeField] private int[] discountByStage = { 0, 0, 5, 10, 15 }; // 할인율

    [Header("Buying")] // 사 주는 물건
    [SerializeField] private List<BuyRule> buyRules = new List<BuyRule>(); // 사 주는 규칙
    [Tooltip("하루에 플레이어에게서 사 주는 최대 수량.")]
    [SerializeField, Min(0)] private int dailyBuyLimit = 20; // 하루 매입 수

    [Header("Lines")] // 대사
    [Tooltip("상점 창 인사 (날짜마다 바뀜).")]
    [SerializeField] private string[] greetings = new string[0]; // 인사
    [Tooltip("영업이 끝나 창이 닫힐 때 말풍선.")]
    [SerializeField] private string farewell = string.Empty; // 마감 인사

    public string ShopId => shopId; // ID 제공
    public string OwnerId => ownerId; // 주인 제공
    public string ShopName => shopName; // 이름 제공
    public IReadOnlyList<string> OpenLocationIds => openLocationIds; // 영업 위치 제공
    public IReadOnlyList<StockEntry> Stock => stock; // 판매 목록 제공
    public int StockSeed => stockSeed; // 재고 기준 값 제공
    public IReadOnlyList<BuyRule> BuyRules => buyRules; // 사 주는 규칙 제공
    public int DailyBuyLimit => Mathf.Max(0, dailyBuyLimit); // 하루 매입 수 제공
    public IReadOnlyList<string> Greetings => greetings; // 인사 제공
    public string Farewell => farewell; // 마감 인사 제공
    public int MaxDiscountPercent => GetDiscountPercent(AffinityStage.Love); // 최대 할인율

    public bool IsOpenLocation(string locationId) // 영업 위치 여부
    {
        return !string.IsNullOrEmpty(locationId) && Array.IndexOf(openLocationIds, locationId) >= 0;
    }

    public int GetDiscountPercent(AffinityStage stage) // 단계별 할인율
    {
        int index = (int)stage;
        int value = discountByStage != null && index >= 0 && index < discountByStage.Length ? discountByStage[index] : 0;
        return Mathf.Clamp(value, 0, 90);
    }

    public int GetPrice(StockEntry entry, AffinityStage stage) // 할인 적용 가격
    {
        return DiscountedPrice(entry.Price, GetDiscountPercent(stage));
    }

    public static int DiscountedPrice(int price, int discountPercent) // 할인 가격 (최소 1)
    {
        return Mathf.Max(1, Mathf.RoundToInt(price * (100 - discountPercent) / 100f));
    }

    public BuyRule FindBuyRule(ItemData item, MarketGoodsType goodsType) // 아이템 규칙 → 분류 규칙 → 모든 분류 순서
    {
        BuyRule typeRule = null;
        BuyRule anyRule = null;

        foreach (BuyRule rule in buyRules)
        {
            if (rule == null)
            {
                continue;
            }

            if (rule.Item != null)
            {
                if (rule.Item == item)
                {
                    return rule;
                }
            }
            else if (rule.AnyGoods)
            {
                anyRule = anyRule ?? rule;
            }
            else if (rule.GoodsType == goodsType)
            {
                typeRule = typeRule ?? rule;
            }
        }

        return typeRule ?? anyRule;
    }

    public string GetGreeting(int day) // 날짜별 인사
    {
        if (greetings == null || greetings.Length == 0)
        {
            return string.Empty;
        }

        return greetings[Mathf.Abs(day) % greetings.Length];
    }

#if UNITY_EDITOR
    public void EditorAssign(string id, string owner, string title, string[] locations, List<StockEntry> entries, int seed, int[] discounts, List<BuyRule> rules, int buyLimit, string[] lines, string closing) // 생성 도구 전용
    {
        shopId = id;
        ownerId = owner;
        shopName = title;
        openLocationIds = locations ?? new string[0];
        stock = entries ?? new List<StockEntry>();
        stockSeed = seed;
        discountByStage = discounts ?? new[] { 0, 0, 0, 0, 0 };
        buyRules = rules ?? new List<BuyRule>();
        dailyBuyLimit = Mathf.Max(0, buyLimit);
        greetings = lines ?? new string[0];
        farewell = closing ?? string.Empty;
    }
#endif
}
