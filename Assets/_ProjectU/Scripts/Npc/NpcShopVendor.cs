using System; // 이벤트 기능
using System.Collections.Generic; // 목록 기능
using UnityEngine; // Unity 기본 기능

public sealed class NpcShopVendor : IShopVendor // 92일차: NPC 상점을 87일차 상점 창에 연결 (주인 초상 · 인사 · 할인 · 팔기)
{
    public const float CloseDistance = 4.5f; // 플레이어가 이보다 멀어지면 창을 닫음 (대화 창과 같은 거리)

    private readonly NpcShopManager manager; // 상점 관리자
    private readonly NpcShopData shop; // 상점
    private readonly NpcAgent owner; // 주인 NPC
    private Transform player; // 플레이어

    public NpcShopVendor(NpcShopManager manager, NpcShopData shop, NpcAgent owner) // 생성
    {
        this.manager = manager;
        this.shop = shop;
        this.owner = owner;
    }

    public NpcShopData Shop => shop; // 상점 제공 (테스트용)
    public NpcAgent Owner => owner; // 주인 제공 (테스트용)
    public string Title => owner != null ? $"{owner.DisplayName} · {shop.ShopName}" : shop.ShopName; // "밀키 · 선셋 카페"
    public Sprite Portrait => owner != null && owner.Character != null ? owner.Character.Portrait : null; // 주인 초상
    public string SpeechLine => shop.GetGreeting(manager.CurrentDay); // 오늘 인사
    public Vector3 SpeechPosition => owner != null ? owner.transform.position + Vector3.up * 2.3f : Vector3.zero; // 말풍선 위치
    public bool IsOpen => manager != null && manager.IsScheduledOpenNow(shop); // 일정상 영업 시간 (창이 열린 동안 주인은 멈춰 있음)
    public string ClosingLine => string.IsNullOrEmpty(shop.Farewell) ? "내일 또 와요!" : shop.Farewell; // 마감 인사
    public IReadOnlyList<ShopOffer> Offers => manager.GetOffers(shop); // 오늘 재고
    public bool BuysFromPlayer => shop.BuyRules.Count > 0 && shop.DailyBuyLimit > 0; // 팔기 탭
    public int BuybackLeftToday => manager.GetBuybackLeft(shop); // 오늘 매입 남은 수
    public string BuyInfo => manager.DescribeBuys(shop); // 사 주는 물건 안내

    public bool IsPresent // 주인이 곁에 있는지
    {
        get
        {
            if (manager == null || shop == null || owner == null || !owner.isActiveAndEnabled || owner.IsInside)
            {
                return false;
            }

            return player == null || (player.position - owner.transform.position).sqrMagnitude <= CloseDistance * CloseDistance;
        }
    }

    public string InfoLabel // "영업 08:00 ~ 18:00 · 신뢰 할인 5%"
    {
        get
        {
            string text = $"영업 {manager.DescribeHours(shop, manager.CurrentDay, manager.CurrentWeather)}";
            int discount = manager.GetDiscountPercent(shop);
            return discount > 0 ? $"{text}  ·  {NpcDialogueSelector.StageName(manager.GetOwnerStage(shop))} 할인 {discount}%" : text;
        }
    }

    public event Action Changed // 재고 · 매입 변경 알림
    {
        add { if (manager != null) manager.StockChanged += value; }
        remove { if (manager != null) manager.StockChanged -= value; }
    }

    public int GetUnitPrice(ShopOffer offer) => manager.GetUnitPrice(shop, offer); // 할인 적용 가격
    public int DiscountPercent => manager.GetDiscountPercent(shop); // 관계 단계 할인율
    public string GetLockReason(ShopOffer offer) => manager.GetLockReason(shop, offer); // 잠긴 이유
    public int GetBuybackPrice(ItemData item) => manager.GetBuybackPrice(shop, item); // 매입 가격

    public bool TryBuy(ShopOffer offer, int quantity, PlayerInventory inventory, out string message) // 사기
    {
        return manager.TryBuy(shop, offer, quantity, inventory, out message);
    }

    public bool TrySell(ItemData item, int quantity, PlayerInventory inventory, out string message) // 팔기
    {
        return manager.TrySell(shop, item, quantity, inventory, out message);
    }

    public void Opened(Transform playerTransform) // 창이 열리면 주인이 멈춰 플레이어를 바라봄
    {
        player = playerTransform;

        if (owner != null && player != null)
        {
            owner.SetTalking(true, player.position);
        }
    }

    public void Closed() // 창이 닫히면 다시 일정대로
    {
        if (owner != null)
        {
            owner.SetTalking(false, Vector3.zero);
        }
    }
}
