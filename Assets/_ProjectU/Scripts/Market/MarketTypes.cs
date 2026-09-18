using System; // 직렬화 기능
using System.Collections.Generic; // 목록 기능
using UnityEngine; // Unity 기본 기능

public enum MarketGoodsType // 87일차: 판매 물건 분류 (가격표 정렬·표시용)
{
    Crop = 0, // 작물
    Forage = 1, // 채집물
    Fish = 2, // 물고기
    AnimalProduct = 3, // 가축 생산물
    Cooked = 4, // 요리
    Drink = 5, // 음료
    Material = 6, // 재료
    Seed = 7, // 씨앗
    Supply = 8 // 소모품·도구
}

[Serializable] // Inspector 표시 허용
public sealed class MarketPriceEntry // 87일차: 판매 상자에서 한 아이템을 사 주는 기본 가격
{
    [Tooltip("팔 수 있는 아이템.")]
    [SerializeField] private ItemData item; // 아이템
    [Tooltip("기본 판매 가격 (코인).")]
    [SerializeField, Min(1)] private int basePrice = 1; // 기본 가격
    [Tooltip("물건 분류.")]
    [SerializeField] private MarketGoodsType goodsType = MarketGoodsType.Material; // 분류
    [Tooltip("이 계절에는 제철 가격을 받습니다. 비어 있으면 제철이 없습니다.")]
    [SerializeField] private SeasonType[] freshSeasons = new SeasonType[0]; // 제철 계절

    public ItemData Item => item; // 아이템 제공
    public int BasePrice => Mathf.Max(1, basePrice); // 기본 가격 제공
    public MarketGoodsType GoodsType => goodsType; // 분류 제공
    public IReadOnlyList<SeasonType> FreshSeasons => freshSeasons; // 제철 계절 제공

    public bool IsFreshIn(SeasonType season) // 지정 계절 제철 여부
    {
        return freshSeasons != null && Array.IndexOf(freshSeasons, season) >= 0; // 결과 반환
    }
}

[Serializable] // Inspector 표시 허용
public sealed class MarketStockEntry // 87일차: 떠돌이 상인이 파는 물건 한 종류
{
    [Tooltip("파는 아이템.")]
    [SerializeField] private ItemData item; // 아이템
    [Tooltip("한 개 가격 (코인).")]
    [SerializeField, Min(1)] private int price = 1; // 가격
    [Tooltip("하루 재고 수.")]
    [SerializeField, Min(1)] private int dailyStock = 10; // 하루 재고
    [Tooltip("파는 계절. 비어 있으면 모든 계절에 팝니다.")]
    [SerializeField] private SeasonType[] seasons = new SeasonType[0]; // 판매 계절
    [Tooltip("그날 상인이 이 물건을 가져올 확률 (1 = 매일).")]
    [SerializeField, Range(0f, 1f)] private float dailyChance = 1f; // 등장 확률

    public ItemData Item => item; // 아이템 제공
    public int Price => Mathf.Max(1, price); // 가격 제공
    public int DailyStock => Mathf.Max(1, dailyStock); // 하루 재고 제공
    public IReadOnlyList<SeasonType> Seasons => seasons; // 판매 계절 제공
    public float DailyChance => Mathf.Clamp01(dailyChance); // 등장 확률 제공
    public bool IsSpecial => dailyChance < 1f; // 가끔만 오는 물건 여부

    public bool IsSoldIn(SeasonType season) // 지정 계절 판매 여부
    {
        return seasons == null || seasons.Length == 0 || Array.IndexOf(seasons, season) >= 0; // 결과 반환
    }
}

public readonly struct MarketPriceQuote // 87일차: 현재 계절 기준 판매 가격 안내
{
    public readonly ItemData Item; // 아이템
    public readonly bool Sellable; // 판매 가능 여부
    public readonly int BasePrice; // 기본 가격
    public readonly int UnitPrice; // 오늘 한 개 가격
    public readonly bool IsFresh; // 제철 여부
    public readonly MarketGoodsType GoodsType; // 분류

    public MarketPriceQuote(ItemData item, bool sellable, int basePrice, int unitPrice, bool isFresh, MarketGoodsType goodsType) // 생성
    {
        Item = item; // 아이템
        Sellable = sellable; // 판매 가능
        BasePrice = basePrice; // 기본 가격
        UnitPrice = unitPrice; // 오늘 가격
        IsFresh = isFresh; // 제철
        GoodsType = goodsType; // 분류
    }
}

public struct ShippingSaleLine // 87일차: 판매 결과 한 줄
{
    public ItemData Item; // 아이템
    public int Quantity; // 판매 수량
    public int Coins; // 받은 코인
}

public sealed class ShippingSaleReport // 87일차: 하루 판매 결과
{
    public int Day; // 판매한 날짜 (새 날)
    public int ItemsSold; // 판매 수량 합계
    public int Coins; // 받은 코인 합계
    public int UnsoldItems; // 팔 수 없어 남은 수량
    public readonly List<ShippingSaleLine> Lines = new List<ShippingSaleLine>(); // 아이템별 결과

    public bool HasSales => ItemsSold > 0; // 판매 여부
}

public sealed class ShopOffer // 87일차: 오늘 상인이 파는 물건 한 칸
{
    public ShopOffer(MarketStockEntry entry, int remaining) // 생성
    {
        Entry = entry; // 원본
        Remaining = remaining; // 남은 수
    }

    public MarketStockEntry Entry { get; } // 원본 데이터
    public int Remaining { get; set; } // 남은 재고
    public ItemData Item => Entry.Item; // 아이템 제공
    public int Price => Entry.Price; // 가격 제공
    public bool SoldOut => Remaining <= 0; // 품절 여부
}

public interface IStorageInfoProvider // 87일차: 보관함 창 위쪽 안내 문구와 상호작용 안내 추가 문구
{
    string StorageInfo { get; } // 보관함 창 안내 문구
    Color StorageInfoColor { get; } // 안내 문구 색
    string PromptSuffix { get; } // 상호작용 안내 뒤에 붙일 문구 (없으면 빈 문자열)
}
