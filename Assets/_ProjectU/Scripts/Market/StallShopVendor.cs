using System; // 이벤트 기능
using System.Collections.Generic; // 목록 기능
using UnityEngine; // Unity 기본 기능

public sealed class StallShopVendor : IShopVendor // 92일차: 87일차 가판대 떠돌이 상인을 상점 창 형식으로 연결 (마을 NPC가 없는 Scene용)
{
    private readonly MarketStall stall; // 가판대
    private readonly MarketManager market; // 상점 관리자

    public StallShopVendor(MarketStall stall, MarketManager market) // 생성
    {
        this.stall = stall; // 가판대
        this.market = market; // 관리자
    }

    public string Title => stall != null ? stall.MerchantName : "MERCHANT"; // 제목
    public Sprite Portrait => null; // 기본 상인 얼굴 유지
    public string SpeechLine => string.Empty; // 한마디 없음
    public Vector3 SpeechPosition => stall != null ? stall.transform.position + Vector3.up * 2.2f : Vector3.zero; // 인사 위치
    public bool IsPresent => stall != null && stall.isActiveAndEnabled && market != null; // 가판대 유지
    public bool IsOpen => market != null && market.IsShopOpen; // 영업 시간
    public string ClosingLine => "SEE YOU TOMORROW!"; // 마감 인사
    public IReadOnlyList<ShopOffer> Offers => market.Offers; // 오늘 재고
    public bool BuysFromPlayer => false; // 판매는 판매 상자로만
    public int BuybackLeftToday => 0; // 매입 없음
    public string BuyInfo => string.Empty; // 매입 없음

    public string InfoLabel // 재고 안내
    {
        get
        {
            int left = 0; // 남은 재고 합계

            foreach (ShopOffer offer in market.Offers) // 순회
            {
                left += offer.Remaining; // 합계
            }

            return $"{ShopPopupUI.SeasonLabel(market.CurrentSeason)} STOCK  ·  {left} LEFT TODAY"; // 결과 반환
        }
    }

    public event Action Changed // 재고 변경 알림
    {
        add { if (market != null) market.StockChanged += value; } // 구독
        remove { if (market != null) market.StockChanged -= value; } // 해제
    }

    public int GetUnitPrice(ShopOffer offer) => offer.Price; // 할인 없음
    public int DiscountPercent => 0; // 할인 없음
    public string GetLockReason(ShopOffer offer) => null; // 잠김 없음
    public int GetBuybackPrice(ItemData item) => 0; // 매입 없음

    public bool TryBuy(ShopOffer offer, int quantity, PlayerInventory inventory, out string message) // 87일차 규칙 그대로
    {
        return market.TryBuy(offer, quantity, inventory, out message); // 결과 반환
    }

    public bool TrySell(ItemData item, int quantity, PlayerInventory inventory, out string message) // 매입 없음
    {
        message = "USE THE SHIPPING BIN TO SELL"; // 문구
        return false; // 실패
    }

    public void Opened(Transform player) { } // 할 일 없음
    public void Closed() { } // 할 일 없음
}
