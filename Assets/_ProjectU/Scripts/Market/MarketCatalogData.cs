using System; // 문자열 비교 기능
using System.Collections.Generic; // 목록 기능
using UnityEngine; // Unity 기본 기능

[CreateAssetMenu(fileName = "MarketCatalog", menuName = "Project U/Market Catalog")] // 생성 메뉴
public sealed class MarketCatalogData : ScriptableObject // 87일차: 판매 가격표 · 상인 재고 · 가격 규칙
{
    [Header("Sell Prices")] // 판매 가격표
    [Tooltip("판매 상자가 사 주는 아이템과 기본 가격. 목록에 없는 아이템은 팔 수 없습니다.")]
    [SerializeField] private List<MarketPriceEntry> prices = new List<MarketPriceEntry>(); // 판매 가격표

    [Header("Merchant Stock")] // 상인 재고
    [Tooltip("떠돌이 상인이 파는 물건 후보.")]
    [SerializeField] private List<MarketStockEntry> stock = new List<MarketStockEntry>(); // 상인 재고 후보

    [Header("Rules")] // 가격 규칙
    [Tooltip("제철 물건 가격 배율.")]
    [SerializeField, Range(1f, 3f)] private float freshMultiplier = 1.25f; // 제철 배율
    [Tooltip("하룻밤에 같은 아이템을 이 수보다 많이 팔면 초과분은 싸게 팔립니다.")]
    [SerializeField, Min(1)] private int bulkThreshold = 20; // 대량 판매 기준
    [Tooltip("대량 판매 초과분 가격 배율.")]
    [SerializeField, Range(0.1f, 1f)] private float bulkMultiplier = 0.7f; // 대량 판매 배율
    [Tooltip("상인이 가판대에 나오는 시각.")]
    [SerializeField, Range(0f, 23f)] private float openHour = 6f; // 여는 시각
    [Tooltip("상인이 돌아가는 시각.")]
    [SerializeField, Range(1f, 24f)] private float closeHour = 20f; // 닫는 시각
    [Tooltip("재고 뽑기 기준 값 (같은 날이면 항상 같은 재고).")]
    [SerializeField] private int stockSeed = 8731; // 재고 기준 값

    private Dictionary<ItemData, MarketPriceEntry> priceLookup; // 아이템별 가격 캐시

    public IReadOnlyList<MarketPriceEntry> Prices => prices; // 가격표 제공
    public IReadOnlyList<MarketStockEntry> Stock => stock; // 재고 후보 제공
    public float FreshMultiplier => Mathf.Max(1f, freshMultiplier); // 제철 배율 제공
    public int BulkThreshold => Mathf.Max(1, bulkThreshold); // 대량 기준 제공
    public float BulkMultiplier => Mathf.Clamp(bulkMultiplier, 0.1f, 1f); // 대량 배율 제공
    public float OpenHour => openHour; // 여는 시각 제공
    public float CloseHour => closeHour; // 닫는 시각 제공
    public int StockSeed => stockSeed; // 재고 기준 값 제공

    private void OnEnable() // 캐시 초기화
    {
        priceLookup = null; // 다시 만들기
    }

    private void OnValidate() // Inspector 값 변경
    {
        priceLookup = null; // 다시 만들기
        closeHour = Mathf.Max(openHour + 1f, closeHour); // 닫는 시각 보정
    }

    public bool TryGetPrice(ItemData item, out MarketPriceEntry entry) // 아이템 가격 찾기
    {
        entry = null; // 초기화

        if (item == null) // 요청 확인
        {
            return false; // 없음
        }

        if (priceLookup == null) // 캐시 만들기
        {
            priceLookup = new Dictionary<ItemData, MarketPriceEntry>(); // 생성

            foreach (MarketPriceEntry candidate in prices) // 순회
            {
                if (candidate != null && candidate.Item != null && !priceLookup.ContainsKey(candidate.Item)) // 첫 항목만
                {
                    priceLookup.Add(candidate.Item, candidate); // 등록
                }
            }
        }

        return priceLookup.TryGetValue(item, out entry); // 결과 반환
    }

    public MarketPriceQuote GetQuote(ItemData item, SeasonType season) // 오늘 가격 안내
    {
        if (!TryGetPrice(item, out MarketPriceEntry entry)) // 가격표 확인
        {
            return new MarketPriceQuote(item, false, 0, 0, false, MarketGoodsType.Supply); // 팔 수 없음
        }

        bool fresh = entry.IsFreshIn(season); // 제철 여부
        int unit = Mathf.Max(1, Mathf.RoundToInt(entry.BasePrice * (fresh ? FreshMultiplier : 1f))); // 오늘 가격
        return new MarketPriceQuote(item, true, entry.BasePrice, unit, fresh, entry.GoodsType); // 결과 반환
    }

    public int GetBatchValue(int unitPrice, int quantity) // 같은 아이템 여러 개 판매 금액 (대량 판매 규칙 포함)
    {
        if (unitPrice <= 0 || quantity <= 0) // 요청 확인
        {
            return 0; // 없음
        }

        int full = Mathf.Min(quantity, BulkThreshold); // 제값 수량
        int extra = quantity - full; // 초과 수량
        int extraUnit = Mathf.Max(1, Mathf.FloorToInt(unitPrice * BulkMultiplier)); // 초과분 가격
        return full * unitPrice + extra * extraUnit; // 결과 반환
    }

    public bool IsOpenAt(float hour) // 상인 영업 시간 확인
    {
        return hour >= openHour && hour < closeHour; // 결과 반환
    }

    public MarketStockEntry FindStock(string itemId) // ID로 재고 후보 찾기
    {
        foreach (MarketStockEntry entry in stock) // 순회
        {
            if (entry != null && entry.Item != null && string.Equals(entry.Item.ItemId, itemId, StringComparison.Ordinal)) // 비교
            {
                return entry; // 결과
            }
        }

        return null; // 없음
    }
}
