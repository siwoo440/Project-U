using System; // 이벤트 기능
using System.Collections.Generic; // 목록 기능
using UnityEngine; // Unity 기본 기능

public sealed class NpcCraftVendor : IShopVendor, ICraftVendor // 104일차: 상점 없이 제작만 하는 NPC (알리우네 · 루네트)를 상인 창의 제작 탭으로 연결
{
    private readonly NpcShopManager manager; // 상점 관리자 (제작 주문 처리)
    private readonly NpcCraftBook book; // 제작 주문서
    private readonly NpcAgent owner; // 주인 NPC
    private Transform player; // 플레이어

    public NpcCraftVendor(NpcShopManager manager, NpcCraftBook book, NpcAgent owner) // 생성
    {
        this.manager = manager;
        this.book = book;
        this.owner = owner;
    }

    public NpcCraftBook Book => book; // 주문서 제공 (테스트용)
    public NpcAgent Owner => owner; // 주인 제공 (테스트용)
    public string Title => owner != null ? $"{owner.DisplayName} · {book.StationName}" : book.StationName; // "알리우네 · 치유 온실"
    public Sprite Portrait => owner != null && owner.Character != null ? owner.Character.Portrait : null; // 주인 초상
    public string SpeechLine => book.CraftLine; // 한마디
    public Vector3 SpeechPosition => owner != null ? owner.transform.position + Vector3.up * 2.3f : Vector3.zero; // 말풍선 위치
    public bool IsOpen => manager != null && manager.IsCraftOpenNow(book); // 일정상 작업 시간
    public string ClosingLine => string.IsNullOrEmpty(book.Farewell) ? "내일 또 와요!" : book.Farewell; // 마감 인사
    public IReadOnlyList<ShopOffer> Offers => Array.Empty<ShopOffer>(); // 파는 물건 없음
    public bool BuysFromPlayer => false; // 사 주지 않음
    public int BuybackLeftToday => 0; // 매입 없음
    public string BuyInfo => string.Empty; // 매입 안내 없음
    public int DiscountPercent => manager.GetCraftDiscountPercent(book); // 수수료 할인율

    public bool IsPresent // 주인이 곁에 있는지
    {
        get
        {
            if (manager == null || book == null || owner == null || !owner.isActiveAndEnabled || owner.IsInside)
            {
                return false;
            }

            return player == null || (player.position - owner.transform.position).sqrMagnitude <= NpcShopVendor.CloseDistance * NpcShopVendor.CloseDistance;
        }
    }

    public string InfoLabel // "제작 09:00 ~ 19:00 · 신뢰 할인 5%"
    {
        get
        {
            string text = $"제작 {manager.DescribeCraftHours(book)}";
            int discount = DiscountPercent;
            return discount > 0 ? $"{text}  ·  할인 {discount}%" : text;
        }
    }

    public event Action Changed // 제작 결과 알림
    {
        add { if (manager != null) manager.StockChanged += value; }
        remove { if (manager != null) manager.StockChanged -= value; }
    }

    public int GetUnitPrice(ShopOffer offer) => 0; // 파는 물건 없음
    public string GetLockReason(ShopOffer offer) => null; // 파는 물건 없음
    public int GetBuybackPrice(ItemData item) => 0; // 사 주지 않음

    public bool TryBuy(ShopOffer offer, int quantity, PlayerInventory inventory, out string message) // 파는 물건 없음
    {
        message = "NOTHING FOR SALE";
        return false;
    }

    public bool TrySell(ItemData item, int quantity, PlayerInventory inventory, out string message) // 사 주지 않음
    {
        message = "THEY DON'T BUY ANYTHING";
        return false;
    }

    public IReadOnlyList<NpcCraftBook.Order> CraftOrders => book.Orders; // 제작 주문
    public string CraftLine => book.CraftLine; // 제작 한마디
    public bool CraftOnly => true; // 제작 탭만
    public int GetCraftFee(NpcCraftBook.Order order) => manager.GetCraftFee(book, order); // 수수료
    public string GetCraftLockReason(NpcCraftBook.Order order) => manager.GetCraftLockReason(book, order); // 잠긴 이유
    public string GetCraftBlockReason(NpcCraftBook.Order order, PlayerInventory inventory) => manager.GetCraftBlockReason(book, order, inventory); // 불가 이유

    public bool TryCraft(NpcCraftBook.Order order, PlayerInventory inventory, out string message) // 제작 주문
    {
        return manager.TryCraft(book, order, inventory, out message);
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
