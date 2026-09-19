using System.Collections.Generic; // 목록 기능
using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.InputSystem; // 키보드 입력 기능
using UnityEngine.UI; // Unity UI 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class ShopPopupUI : MonoBehaviour, IGameScenePopup // 87일차: 떠돌이 상인 창 (오늘 재고 사기 · 판매 가격표) / 92일차: NPC 상점 (할인 · 잠긴 물건 · 팔기) / 102일차: 제작 주문
{
    public enum ShopTab // 창 탭
    {
        Buy = 0, // 사기
        Prices = 1, // 판매 가격표 (87일차 가판대)
        Sell = 2, // 팔기 (92일차 NPC 상점)
        Craft = 3 // 제작 주문 (102일차 NPC 상점)
    }

    [Header("Root")] // 루트
    [Tooltip("켜고 끄는 창 전체.")]
    [SerializeField] private GameObject panelRoot; // 창 루트
    [Tooltip("제목 (상인 이름).")]
    [SerializeField] private TMP_Text titleText; // 제목
    [Tooltip("제목 아이콘 (상인 얼굴).")]
    [SerializeField] private Image titleIcon; // 제목 아이콘
    [Tooltip("재고 안내 알약.")]
    [SerializeField] private CookingChipUI infoChip; // 재고 안내
    [Tooltip("내 코인 알약.")]
    [SerializeField] private CookingChipUI coinChip; // 코인
    [Tooltip("닫기 버튼.")]
    [SerializeField] private Button closeButton; // 닫기
    [Tooltip("92일차: 가게 주인 한마디 (탭 오른쪽). 비어 있으면 표시하지 않습니다.")]
    [SerializeField] private TMP_Text speechText; // 한마디

    [Header("Tabs")] // 탭
    [SerializeField] private Button buyTabButton; // 사기 탭
    [SerializeField] private Image buyTabImage; // 사기 탭 배경
    [SerializeField] private TMP_Text buyTabLabel; // 사기 탭 문구
    [SerializeField] private Button pricesTabButton; // 두 번째 탭 (가격표 · 팔기)
    [SerializeField] private Image pricesTabImage; // 두 번째 탭 배경
    [SerializeField] private TMP_Text pricesTabLabel; // 두 번째 탭 문구
    [Tooltip("102일차: 제작 주문 탭 (제작 주문서가 있는 NPC 상점만 표시).")]
    [SerializeField] private Button craftTabButton; // 제작 탭
    [SerializeField] private Image craftTabImage; // 제작 탭 배경
    [SerializeField] private TMP_Text craftTabLabel; // 제작 탭 문구

    [Header("List")] // 목록
    [Tooltip("목록 줄 부모.")]
    [SerializeField] private Transform listRoot; // 목록 부모
    [Tooltip("목록 스크롤.")]
    [SerializeField] private ScrollRect listScroll; // 스크롤
    [Tooltip("목록 줄 템플릿 (꺼진 상태).")]
    [SerializeField] private ShopRowUI rowTemplate; // 줄 템플릿
    [Tooltip("목록이 비었을 때 문구.")]
    [SerializeField] private TMP_Text emptyListText; // 빈 목록 문구

    [Header("Detail")] // 상세
    [SerializeField] private Image detailIcon; // 아이콘
    [SerializeField] private TMP_Text detailNameText; // 이름
    [SerializeField] private TMP_Text detailInfoText; // 설명
    [SerializeField] private CookingChipUI typeChip; // 분류 알약
    [SerializeField] private CookingChipUI stateChip; // 재고·제철 알약
    [SerializeField] private TMP_Text priceText; // 가격
    [SerializeField] private TMP_Text noteText; // 보조 안내

    [Header("Buy")] // 사기
    [Tooltip("수량·구매 버튼 묶음 (가격표 탭에서는 숨김).")]
    [SerializeField] private GameObject buyGroup; // 구매 묶음
    [SerializeField] private Button minusButton; // 수량 감소
    [SerializeField] private Button plusButton; // 수량 증가
    [SerializeField] private TMP_Text quantityText; // 수량
    [SerializeField] private Button buyButton; // 구매 · 판매
    [SerializeField] private TMP_Text buyLabel; // 구매 · 판매 문구

    [Header("Footer")] // 아래쪽
    [SerializeField] private TMP_Text hintText; // 안내
    [SerializeField] private TMP_Text messageText; // 결과 알림
    [Tooltip("알림 표시 시간.")]
    [SerializeField, Min(0.5f)] private float messageDuration = 2.4f; // 알림 시간

    [Header("Icons")] // 아이콘
    [SerializeField] private Sprite coinSprite; // 코인
    [SerializeField] private Sprite tagSprite; // 가격표
    [SerializeField] private Sprite crateSprite; // 상자
    [SerializeField] private Sprite leafSprite; // 제철

    private readonly List<ShopRowUI> rows = new List<ShopRowUI>(); // 목록 줄
    private readonly List<MarketPriceEntry> priceEntries = new List<MarketPriceEntry>(); // 가격표 정렬 목록
    private readonly List<ItemData> sellItems = new List<ItemData>(); // 팔 수 있는 가방 물건 (92일차)
    private GameUIManager manager; // 관리자
    private IShopVendor vendor; // 현재 가게 (92일차)
    private MarketManager market; // 상점 관리자 (가격표 · 계절)
    private PlayerInventory inventory; // 인벤토리
    private PlayerWallet wallet; // 지갑
    private Sprite defaultTitleSprite; // 기본 상인 얼굴
    private ShopTab tab = ShopTab.Buy; // 현재 탭
    private int selectedIndex; // 선택 줄
    private int quantity = 1; // 수량
    private bool isDirty; // 다시 그리기 필요
    private float messageHideTime; // 알림 숨김 시각

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf && vendor != null; // 열림 여부
    public IShopVendor Vendor => vendor; // 현재 가게 (테스트용)
    public ShopTab CurrentTab => tab; // 탭 제공
    public int SelectedIndex => selectedIndex; // 선택 제공
    public int Quantity => quantity; // 수량 제공
    public IReadOnlyList<ShopRowUI> Rows => rows; // 줄 제공 (테스트용)
    public IReadOnlyList<ItemData> SellItems => sellItems; // 팔기 목록 (테스트용)
    public string TitleLabel => titleText != null ? titleText.text : string.Empty; // 제목 (테스트용)
    public string SpeechLabel => speechText != null && speechText.gameObject.activeSelf ? speechText.text : string.Empty; // 한마디 (테스트용)
    public string BuyLabel => buyLabel != null ? buyLabel.text : string.Empty; // 구매 문구 제공 (테스트용)
    public bool CanPressBuy => buyButton != null && buyButton.interactable; // 구매 가능 제공 (테스트용)
    public string MessageLabel => messageText != null && messageText.gameObject.activeSelf ? messageText.text : string.Empty; // 알림 제공 (테스트용)
    public ShopTab SecondTab => vendor != null && vendor.BuysFromPlayer ? ShopTab.Sell : ShopTab.Prices; // 두 번째 탭 종류
    private ICraftVendor CraftVendor => vendor as ICraftVendor; // 102일차: 제작 주문 가게
    public bool HasCraftTab => CraftVendor != null && CraftVendor.CraftOrders.Count > 0; // 제작 탭 표시 여부
    public bool CraftTabVisible => craftTabButton != null && craftTabButton.gameObject.activeSelf; // 제작 탭 버튼 표시 (테스트용)

    private void Awake() // 버튼 연결
    {
        if (closeButton != null) closeButton.onClick.AddListener(RequestClose); // 닫기
        if (buyTabButton != null) buyTabButton.onClick.AddListener(() => SetTab(ShopTab.Buy)); // 사기 탭
        if (pricesTabButton != null) pricesTabButton.onClick.AddListener(() => SetTab(SecondTab)); // 두 번째 탭
        if (craftTabButton != null) craftTabButton.onClick.AddListener(() => SetTab(ShopTab.Craft)); // 제작 탭
        if (minusButton != null) minusButton.onClick.AddListener(() => ChangeQuantity(-1)); // 감소
        if (plusButton != null) plusButton.onClick.AddListener(() => ChangeQuantity(1)); // 증가
        if (buyButton != null) buyButton.onClick.AddListener(Confirm); // 구매 · 판매
        if (rowTemplate != null) rowTemplate.gameObject.SetActive(false); // 템플릿 숨김
        if (titleIcon != null) defaultTitleSprite = titleIcon.sprite; // 기본 상인 얼굴

        if (panelRoot != null && vendor == null) // 시작 상태
        {
            panelRoot.SetActive(false); // 숨김
        }
    }

    private void OnDestroy() // 구독 해제
    {
        Unsubscribe(); // 해제
    }

    public bool ShowFromManager(GameUIManager owner, MarketStall targetStall, PlayerInventory playerInventory) // 87일차 가판대 상인 창 열기
    {
        return ShowVendor(owner, targetStall != null ? new StallShopVendor(targetStall, MarketManager.Instance) : null, playerInventory); // 결과 반환
    }

    public bool ShowVendor(GameUIManager owner, IShopVendor targetVendor, PlayerInventory playerInventory) // 92일차: 가게 창 열기 (가판대 · NPC 상점)
    {
        MarketManager targetMarket = MarketManager.Instance; // 상점 관리자
        PlayerWallet targetWallet = targetMarket != null ? targetMarket.Wallet : null; // 지갑

        if (panelRoot == null || rowTemplate == null || targetVendor == null || playerInventory == null || targetMarket == null || targetMarket.Catalog == null || targetWallet == null) // 참조 확인
        {
            Debug.LogError("상인 창 참조가 누락되었습니다. Tools > Project U > Build Content > 6. Market를 다시 실행하세요.", this); // 오류
            return false; // 실패
        }

        Unsubscribe(); // 이전 구독 해제
        manager = owner; // 관리자
        vendor = targetVendor; // 가게
        market = targetMarket; // 상점
        inventory = playerInventory; // 인벤토리
        wallet = targetWallet; // 지갑
        vendor.Changed += MarkDirty; // 재고 변경 구독
        wallet.CoinsChanged += HandleCoins; // 코인 변경 구독
        inventory.InventoryChanged += MarkDirty; // 인벤토리 변경 구독
        vendor.Opened(inventory.transform); // 주인 멈추기 등
        tab = ShopTab.Buy; // 사기 탭
        selectedIndex = FirstAvailableOffer(); // 첫 물건
        quantity = 1; // 수량
        BuildPriceEntries(); // 가격표 정렬
        panelRoot.SetActive(true); // 표시
        ShowMessage(string.Empty, Color.clear, 0f); // 알림 초기화
        ResetScroll(); // 맨 위부터
        Rebuild(); // 그리기
        return true; // 성공
    }

    public void HideFromManager() // 창 닫기
    {
        Unsubscribe(); // 해제

        if (vendor != null) // 가게 확인
        {
            vendor.Closed(); // 주인 다시 움직이기 등
        }

        vendor = null; // 가게 해제
        market = null; // 상점 해제
        inventory = null; // 인벤토리 해제
        wallet = null; // 지갑 해제

        if (panelRoot != null) // 루트 확인
        {
            panelRoot.SetActive(false); // 숨김
        }
    }

    private void Unsubscribe() // 이벤트 해제
    {
        if (vendor != null) vendor.Changed -= MarkDirty; // 재고
        if (wallet != null) wallet.CoinsChanged -= HandleCoins; // 코인
        if (inventory != null) inventory.InventoryChanged -= MarkDirty; // 인벤토리
    }

    private void MarkDirty() // 다시 그리기 요청
    {
        isDirty = true; // 기록
    }

    private void HandleCoins(int coins, int delta) // 코인 변경
    {
        isDirty = true; // 기록
    }

    private void Update() // 상태 갱신
    {
        if (panelRoot == null || !panelRoot.activeSelf) // 열림 확인
        {
            return; // 생략
        }

        if (vendor == null || !vendor.IsPresent || market == null) // 가판대 · 주인이 사라짐
        {
            RequestClose(); // 닫기
            return; // 생략
        }

        if (!vendor.IsOpen) // 영업 종료
        {
            CombatDamagePopup.SpawnText(vendor.SpeechPosition, vendor.ClosingLine, ProjectUUIPalette.Accent, 2f); // 인사
            RequestClose(); // 닫기
            return; // 생략
        }

        Keyboard keyboard = Keyboard.current; // 키보드

        if (keyboard != null && keyboard.tabKey.wasPressedThisFrame) // Tab으로 탭 전환
        {
            SetTab(NextTab); // 전환 (사기 → 가격표 · 팔기 → 제작 → 사기)
        }

        if (isDirty) // 변경 확인
        {
            Rebuild(); // 다시 그리기
        }

        if (messageText != null && messageText.gameObject.activeSelf && Time.unscaledTime >= messageHideTime) // 알림 시간
        {
            messageText.gameObject.SetActive(false); // 숨김
        }
    }

    private void RequestClose() // 닫기 요청
    {
        if (manager != null) // 관리자 확인
        {
            manager.CloseShop(); // 관리자 통해 닫기
            return; // 완료
        }

        HideFromManager(); // 직접 닫기
    }

    // ------------------------------------------------------------ 조작

    public void SetTab(ShopTab next) // 탭 바꾸기
    {
        if (vendor == null) // 열림 확인
        {
            return; // 생략
        }

        if (next == ShopTab.Prices || next == ShopTab.Sell) // 두 번째 탭은 가게 종류에 맞춤
        {
            next = SecondTab; // 가격표 또는 팔기
        }
        else if (next == ShopTab.Craft && !HasCraftTab) // 제작 주문이 없는 가게
        {
            next = ShopTab.Buy; // 사기
        }

        if (tab != next) // 다른 탭
        {
            tab = next; // 적용
            selectedIndex = tab == ShopTab.Buy ? FirstAvailableOffer() : 0; // 첫 줄
            quantity = 1; // 수량
            ResetScroll(); // 맨 위부터
        }

        Rebuild(); // 다시 그리기
    }

    public void Select(int index) // 줄 선택
    {
        if (index != selectedIndex) // 다른 줄
        {
            selectedIndex = index; // 적용
            quantity = 1; // 수량 초기화
        }

        Rebuild(); // 다시 그리기
    }

    private ShopTab NextTab => tab == ShopTab.Buy ? SecondTab : tab != ShopTab.Craft && HasCraftTab ? ShopTab.Craft : ShopTab.Buy; // Tab 키 다음 탭

    private static string TabName(ShopTab target) // 탭 이름 (안내 문구)
    {
        switch (target)
        {
            case ShopTab.Prices: return "SELL PRICES";
            case ShopTab.Sell: return "SELL";
            case ShopTab.Craft: return "CRAFT";
            default: return "BUY";
        }
    }

    public void ChangeQuantity(int delta) // 수량 바꾸기 (Shift : 10개씩)
    {
        if (tab == ShopTab.Craft) // 제작 주문은 한 번에 하나
        {
            return; // 생략
        }

        Keyboard keyboard = Keyboard.current; // 키보드
        bool shift = keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed); // Shift
        int max; // 최대 수량

        if (tab == ShopTab.Sell) // 팔기
        {
            ItemData item = SelectedSellItem; // 선택 물건

            if (item == null) // 확인
            {
                return; // 생략
            }

            max = MaxSellQuantity(item); // 최대
        }
        else // 사기
        {
            ShopOffer offer = SelectedOffer; // 선택 물건

            if (offer == null) // 확인
            {
                return; // 생략
            }

            max = MaxQuantity(offer); // 최대
        }

        quantity = Mathf.Clamp(quantity + delta * (shift ? 10 : 1), 1, Mathf.Max(1, max)); // 적용
        Rebuild(); // 다시 그리기
    }

    public void Confirm() // 버튼 : 사기 탭이면 사기, 팔기 탭이면 팔기, 제작 탭이면 제작 주문
    {
        if (tab == ShopTab.Craft) // 제작
        {
            Craft(); // 제작 주문
            return; // 완료
        }

        if (tab == ShopTab.Sell) // 팔기
        {
            Sell(); // 팔기
            return; // 완료
        }

        Buy(); // 사기
    }

    public void Buy() // 선택 물건 사기
    {
        ShopOffer offer = SelectedOffer; // 선택 물건

        if (offer == null || vendor == null) // 확인
        {
            return; // 생략
        }

        bool success = vendor.TryBuy(offer, quantity, inventory, out string message); // 사기
        ShowMessage(message, success ? ProjectUUIPalette.Teal : ProjectUUIPalette.Danger, messageDuration); // 알림

        if (success) // 성공
        {
            quantity = 1; // 수량 초기화
        }

        Rebuild(); // 다시 그리기
    }

    public void Sell() // 92일차: 선택 물건 팔기
    {
        ItemData item = SelectedSellItem; // 선택 물건

        if (item == null || vendor == null) // 확인
        {
            return; // 생략
        }

        bool success = vendor.TrySell(item, quantity, inventory, out string message); // 팔기
        ShowMessage(message, success ? ProjectUUIPalette.Teal : ProjectUUIPalette.Danger, messageDuration); // 알림

        if (success) // 성공
        {
            quantity = 1; // 수량 초기화
        }

        Rebuild(); // 다시 그리기
    }

    public void Craft() // 102일차: 선택 주문 제작
    {
        NpcCraftBook.Order order = SelectedOrder; // 선택 주문

        if (order == null || CraftVendor == null) // 확인
        {
            return; // 생략
        }

        bool success = CraftVendor.TryCraft(order, inventory, out string message); // 제작
        ShowMessage(message, success ? ProjectUUIPalette.Teal : ProjectUUIPalette.Danger, messageDuration); // 알림
        Rebuild(); // 다시 그리기
    }

    private NpcCraftBook.Order SelectedOrder => tab == ShopTab.Craft && HasCraftTab && selectedIndex >= 0 && selectedIndex < CraftVendor.CraftOrders.Count ? CraftVendor.CraftOrders[selectedIndex] : null; // 선택 주문

    private ShopOffer SelectedOffer => tab == ShopTab.Buy && vendor != null && selectedIndex >= 0 && selectedIndex < vendor.Offers.Count ? vendor.Offers[selectedIndex] : null; // 선택 물건
    private ItemData SelectedSellItem => tab == ShopTab.Sell && selectedIndex >= 0 && selectedIndex < sellItems.Count ? sellItems[selectedIndex] : null; // 선택한 팔 물건

    private int MaxQuantity(ShopOffer offer) // 최대 구매 수량 (재고·코인)
    {
        int unit = vendor.GetUnitPrice(offer); // 한 개 가격
        int affordable = unit > 0 ? wallet.Coins / unit : offer.Remaining; // 살 수 있는 수
        return Mathf.Min(offer.Remaining, affordable, 99); // 결과 반환
    }

    private int MaxSellQuantity(ItemData item) // 최대 판매 수량 (가방 · 오늘 매입 남은 수)
    {
        return Mathf.Min(inventory.GetItemQuantity(item), vendor.BuybackLeftToday, 99); // 결과 반환
    }

    private int FirstAvailableOffer() // 품절 · 잠김이 아닌 첫 물건
    {
        if (vendor == null) // 확인
        {
            return 0; // 기본
        }

        IReadOnlyList<ShopOffer> offers = vendor.Offers; // 재고

        for (int index = 0; index < offers.Count; index++) // 순회
        {
            if (!offers[index].SoldOut && vendor.GetLockReason(offers[index]) == null) // 살 수 있음
            {
                return index; // 결과
            }
        }

        return 0; // 기본
    }

    private void BuildPriceEntries() // 가격표 정렬 목록 (분류 → 가격 높은 순)
    {
        priceEntries.Clear(); // 초기화

        foreach (MarketPriceEntry entry in market.Catalog.Prices) // 순회
        {
            if (entry != null && entry.Item != null) // 확인
            {
                priceEntries.Add(entry); // 추가
            }
        }

        priceEntries.Sort((left, right) =>
        {
            int type = left.GoodsType.CompareTo(right.GoodsType); // 분류
            return type != 0 ? type : right.BasePrice.CompareTo(left.BasePrice); // 가격
        }); // 정렬
    }

    private void BuildSellItems() // 가방에서 이 가게가 사 주는 물건 (가방 순서)
    {
        sellItems.Clear(); // 초기화

        for (int index = 0; index < inventory.SlotCapacity; index++) // 칸 순회
        {
            InventorySlot slot = inventory.GetSlot(index); // 칸

            if (slot == null || slot.ItemData == null || slot.Quantity <= 0 || sellItems.Contains(slot.ItemData)) // 빈 칸 · 중복
            {
                continue; // 다음
            }

            if (vendor.GetBuybackPrice(slot.ItemData) > 0) // 사 주는 물건
            {
                sellItems.Add(slot.ItemData); // 추가
            }
        }
    }

    private void ResetScroll() // 목록 맨 위로
    {
        if (listScroll != null) // 스크롤 확인
        {
            listScroll.verticalNormalizedPosition = 1f; // 맨 위
        }
    }

    // ------------------------------------------------------------ 그리기

    private void Rebuild() // 전체 다시 그리기
    {
        isDirty = false; // 기록 해제

        if (vendor == null || market == null || inventory == null || wallet == null) // 상태 확인
        {
            return; // 생략
        }

        SeasonType season = market.CurrentSeason; // 계절
        titleText.SetText(vendor.Title); // 제목
        titleIcon.sprite = vendor.Portrait != null ? vendor.Portrait : defaultTitleSprite; // 주인 초상 (없으면 기본 얼굴)
        titleIcon.enabled = titleIcon.sprite != null; // 아이콘

        if (speechText != null) // 한마디
        {
            string line = tab == ShopTab.Craft && !string.IsNullOrEmpty(CraftVendor?.CraftLine) ? CraftVendor.CraftLine : vendor.SpeechLine; // 문구 (제작 탭은 제작 한마디)
            speechText.gameObject.SetActive(!string.IsNullOrEmpty(line)); // 표시
            speechText.SetText(line ?? string.Empty); // 적용
        }

        if (vendor.BuysFromPlayer) // 팔기 목록은 가방이 바뀔 수 있어 매번 갱신
        {
            BuildSellItems(); // 갱신
        }
        else
        {
            sellItems.Clear(); // 없음
        }

        infoChip.Bind(vendor.InfoLabel, ProjectUUIPalette.Teal, crateSprite); // 가게 안내
        coinChip.Bind($"{wallet.Coins:N0}", ProjectUUIPalette.Accent, coinSprite); // 코인
        SetTabVisual(buyTabImage, buyTabLabel, tab == ShopTab.Buy); // 사기 탭
        SetTabVisual(pricesTabImage, pricesTabLabel, tab == ShopTab.Prices || tab == ShopTab.Sell); // 두 번째 탭
        buyTabLabel.SetText($"BUY  ({vendor.Offers.Count})"); // 사기 탭 문구
        pricesTabLabel.SetText(SecondTab == ShopTab.Sell ? $"SELL  ({sellItems.Count})" : "SELL PRICES"); // 두 번째 탭 문구
        bool hasCraft = HasCraftTab; // 제작 탭

        if (craftTabButton != null) // 102일차: 제작 탭
        {
            craftTabButton.gameObject.SetActive(hasCraft); // 표시
            SetTabVisual(craftTabImage, craftTabLabel, tab == ShopTab.Craft); // 모양
            craftTabLabel.SetText(hasCraft ? $"CRAFT  ({CraftVendor.CraftOrders.Count})" : "CRAFT"); // 문구
        }

        if (tab == ShopTab.Craft && !hasCraft) // 주문서가 사라짐
        {
            tab = ShopTab.Buy; // 사기로
        }

        if (tab == ShopTab.Buy) // 사기 탭
        {
            RebuildBuy(); // 그리기
        }
        else if (tab == ShopTab.Craft) // 제작 탭
        {
            RebuildCraft(); // 그리기
        }
        else if (tab == ShopTab.Sell) // 팔기 탭
        {
            RebuildSell(); // 그리기
        }
        else // 가격표 탭
        {
            RebuildPrices(season); // 그리기
        }
    }

    private void RebuildBuy() // 사기 탭
    {
        IReadOnlyList<ShopOffer> offers = vendor.Offers; // 재고
        selectedIndex = offers.Count == 0 ? 0 : Mathf.Clamp(selectedIndex, 0, offers.Count - 1); // 선택 보정

        for (int index = 0; index < offers.Count; index++) // 줄 그리기
        {
            ShopOffer offer = offers[index]; // 물건
            int unit = vendor.GetUnitPrice(offer); // 오늘 가격
            string locked = vendor.GetLockReason(offer); // 잠김
            bool affordable = wallet.Coins >= unit; // 살 수 있음
            string status; // 상태
            Color statusColor; // 상태 색

            if (offer.SoldOut)
            {
                status = "SOLD OUT"; // 품절
                statusColor = ProjectUUIPalette.Danger; // 빨강
            }
            else if (locked != null)
            {
                status = locked; // 잠김
                statusColor = ProjectUUIPalette.TextSecondary; // 회색
            }
            else
            {
                status = offer.IsSpecial ? $"RARE FIND  ·  {offer.Remaining} LEFT" : $"{offer.Remaining} LEFT"; // 남은 수
                if (unit < offer.Price) status = $"{vendor.DiscountPercent}% OFF  ·  {status}"; // 할인
                statusColor = offer.IsSpecial || unit < offer.Price ? ProjectUUIPalette.Accent : ProjectUUIPalette.Teal; // 색
            }

            bool dim = offer.SoldOut || locked != null || !affordable; // 흐리게
            Color priceColor = offer.SoldOut || locked != null || affordable ? ProjectUUIPalette.Accent : ProjectUUIPalette.Danger; // 가격 색
            GetRow(index).Bind(index, offer.Item, status, statusColor, unit, priceColor, index == selectedIndex, dim, Select); // 표시
        }

        HideRowsFrom(offers.Count); // 남는 줄
        emptyListText.gameObject.SetActive(offers.Count == 0); // 빈 목록
        emptyListText.SetText("NOTHING FOR SALE TODAY"); // 문구
        buyGroup.SetActive(true); // 구매 묶음
        hintText.SetText($"BUY WITH COINS  ·  SHIFT + / - FOR 10  ·  TAB: {TabName(NextTab)}"); // 안내

        ShopOffer selected = SelectedOffer; // 선택

        if (selected == null) // 선택 없음
        {
            BindEmptyDetail("NOTHING TO BUY"); // 빈 상세
            return; // 완료
        }

        ItemData item = selected.Item; // 아이템
        int selectedUnit = vendor.GetUnitPrice(selected); // 한 개 가격
        string selectedLock = vendor.GetLockReason(selected); // 잠김
        int max = MaxQuantity(selected); // 최대 수량
        quantity = Mathf.Clamp(quantity, 1, Mathf.Max(1, max)); // 수량 보정
        int cost = selectedUnit * quantity; // 금액
        BindItemDetail(item); // 공통 상세
        typeChip.Bind(TypeLabel(item), ProjectUUIPalette.TextSecondary, tagSprite); // 분류

        if (selectedLock != null) // 잠김
        {
            stateChip.Bind(selectedLock, ProjectUUIPalette.TextSecondary, crateSprite); // 이유
        }
        else
        {
            stateChip.Bind(selected.SoldOut ? "SOLD OUT TODAY" : $"{selected.Remaining} LEFT TODAY", selected.SoldOut ? ProjectUUIPalette.Danger : ProjectUUIPalette.Teal, crateSprite); // 재고
        }

        string was = selectedUnit < selected.Price ? $"  <size=55%><color=#9A968C><s>{selected.Price}</s></color></size>" : string.Empty; // 할인 전 가격
        priceText.SetText($"{selectedUnit} <size=60%>COINS EACH</size>{was}"); // 가격
        priceText.color = ProjectUUIPalette.Accent; // 색
        int inBag = inventory.GetItemQuantity(item); // 가방 수량
        noteText.SetText(inBag > 0 ? $"YOU HAVE {inBag} IN YOUR BAG" : "YOU DON'T HAVE ANY YET"); // 보조 안내
        quantityText.SetText($"x{quantity}"); // 수량

        string reason = null; // 불가 이유

        if (selected.SoldOut) reason = "SOLD OUT"; // 품절
        else if (selectedLock != null) reason = "LOCKED"; // 관계 단계 부족
        else if (!wallet.CanAfford(cost)) reason = $"NEED {cost - wallet.Coins} MORE COINS"; // 코인 부족
        else if (!inventory.CanAddItem(item, quantity)) reason = "BAG IS FULL"; // 가방 부족

        buyButton.interactable = reason == null; // 가능 여부
        buyLabel.SetText(reason ?? $"BUY x{quantity}  ·  {cost}"); // 문구
        minusButton.interactable = quantity > 1; // 감소
        plusButton.interactable = quantity < max; // 증가
    }

    private void RebuildSell() // 92일차: 팔기 탭
    {
        selectedIndex = sellItems.Count == 0 ? 0 : Mathf.Clamp(selectedIndex, 0, sellItems.Count - 1); // 선택 보정
        int left = vendor.BuybackLeftToday; // 오늘 매입 남은 수

        for (int index = 0; index < sellItems.Count; index++) // 줄 그리기
        {
            ItemData item = sellItems[index]; // 아이템
            int unit = vendor.GetBuybackPrice(item); // 매입 가격
            int bin = market.GetQuote(item).UnitPrice; // 판매 상자 가격
            string status = $"{inventory.GetItemQuantity(item)} IN BAG"; // 상태
            if (unit > bin) status += $"  ·  +{Mathf.RoundToInt((unit / (float)bin - 1f) * 100f)}% VS BIN"; // 판매 상자보다 비쌈
            GetRow(index).Bind(index, item, status, unit > bin ? ProjectUUIPalette.Accent : ProjectUUIPalette.Teal, unit, ProjectUUIPalette.Accent, index == selectedIndex, left <= 0, Select); // 표시
        }

        HideRowsFrom(sellItems.Count); // 남는 줄
        emptyListText.gameObject.SetActive(sellItems.Count == 0); // 빈 목록
        emptyListText.SetText("NOTHING IN YOUR BAG THIS SHOP BUYS"); // 문구
        buyGroup.SetActive(true); // 판매 묶음
        hintText.SetText($"BUYS: {vendor.BuyInfo}  ·  TAB: {TabName(NextTab)}"); // 안내

        ItemData selected = SelectedSellItem; // 선택

        if (selected == null) // 선택 없음
        {
            BindEmptyDetail("NOTHING TO SELL"); // 빈 상세
            return; // 완료
        }

        int selectedUnit = vendor.GetBuybackPrice(selected); // 매입 가격
        MarketPriceQuote quote = market.GetQuote(selected); // 판매 상자 가격
        int inBag = inventory.GetItemQuantity(selected); // 가방 수량
        int max = MaxSellQuantity(selected); // 최대 수량
        quantity = Mathf.Clamp(quantity, 1, Mathf.Max(1, max)); // 수량 보정
        BindItemDetail(selected); // 공통 상세
        typeChip.Bind(GoodsLabel(quote.GoodsType), ProjectUUIPalette.TextSecondary, tagSprite); // 분류
        string compare = selectedUnit > quote.UnitPrice ? $"BIN PAYS {quote.UnitPrice}  ·  HERE +{selectedUnit - quote.UnitPrice}" : selectedUnit == quote.UnitPrice ? "SAME AS SHIPPING BIN" : $"SHIPPING BIN PAYS {quote.UnitPrice}"; // 비교
        stateChip.Bind(compare, selectedUnit >= quote.UnitPrice ? ProjectUUIPalette.Teal : ProjectUUIPalette.TextSecondary, coinSprite); // 판매 상자 비교
        priceText.SetText($"{selectedUnit} <size=60%>COINS EACH</size>"); // 가격
        priceText.color = ProjectUUIPalette.Accent; // 색
        noteText.SetText($"BUYS {left} MORE TODAY  ·  YOU HAVE {inBag}"); // 보조 안내
        quantityText.SetText($"x{quantity}"); // 수량

        string reason = null; // 불가 이유

        if (left <= 0) reason = "NO MORE BUYING TODAY"; // 오늘 매입 끝
        else if (inBag <= 0) reason = "NONE IN YOUR BAG"; // 가방에 없음

        buyButton.interactable = reason == null; // 가능 여부
        buyLabel.SetText(reason ?? $"SELL x{quantity}  ·  +{selectedUnit * quantity}"); // 문구
        minusButton.interactable = quantity > 1; // 감소
        plusButton.interactable = quantity < max; // 증가
    }

    private void RebuildCraft() // 102일차: 제작 탭 (재료 + 수수료 → 물건)
    {
        ICraftVendor crafter = CraftVendor; // 제작 가게
        IReadOnlyList<NpcCraftBook.Order> orders = crafter.CraftOrders; // 주문
        selectedIndex = orders.Count == 0 ? 0 : Mathf.Clamp(selectedIndex, 0, orders.Count - 1); // 선택 보정

        for (int index = 0; index < orders.Count; index++) // 줄 그리기
        {
            NpcCraftBook.Order order = orders[index]; // 주문
            string locked = crafter.GetCraftLockReason(order); // 잠김
            bool ready = NpcShopManager.HasIngredients(order, inventory); // 재료 모두 있음
            int fee = crafter.GetCraftFee(order); // 수수료
            string status; // 상태
            Color statusColor; // 상태 색

            if (locked != null)
            {
                status = locked; // 잠김
                statusColor = ProjectUUIPalette.TextSecondary; // 회색
            }
            else
            {
                status = $"x{order.ResultAmount}  ·  {(ready ? "MATERIALS READY" : $"MISSING {MissingCount(order)}")}"; // 재료 상태
                statusColor = ready ? ProjectUUIPalette.Teal : ProjectUUIPalette.TextSecondary; // 색
            }

            bool dim = locked != null || !ready || !wallet.CanAfford(fee); // 흐리게
            GetRow(index).Bind(index, order.Result, status, statusColor, fee, wallet.CanAfford(fee) ? ProjectUUIPalette.Accent : ProjectUUIPalette.Danger, index == selectedIndex, dim, Select); // 표시
        }

        HideRowsFrom(orders.Count); // 남는 줄
        emptyListText.gameObject.SetActive(orders.Count == 0); // 빈 목록
        emptyListText.SetText("NO CRAFT ORDERS"); // 문구
        buyGroup.SetActive(true); // 주문 묶음
        hintText.SetText($"BRING MATERIALS + FEE  ·  MADE ON THE SPOT  ·  TAB: {TabName(NextTab)}"); // 안내

        NpcCraftBook.Order selected = SelectedOrder; // 선택

        if (selected == null || selected.Result == null) // 선택 없음
        {
            BindEmptyDetail("NOTHING TO CRAFT"); // 빈 상세
            return; // 완료
        }

        BindItemDetail(selected.Result); // 공통 상세
        List<string> lines = new List<string>(); // 재료 줄

        foreach (NpcCraftBook.Ingredient ingredient in selected.Ingredients) // 재료
        {
            if (ingredient == null || ingredient.Item == null) // 빈 칸
            {
                continue; // 다음
            }

            int have = inventory.GetItemQuantity(ingredient.Item); // 가진 수
            string color = have >= ingredient.Amount ? "#7FD6C2" : "#E0776B"; // 색
            lines.Add($"<color={color}>{ingredient.Item.DisplayName} {Mathf.Min(have, 999)}/{ingredient.Amount}</color>"); // 줄
        }

        detailInfoText.SetText($"NEEDS  {string.Join("  ·  ", lines)}"); // 재료 (설명 칸 두 줄에 맞춤, 설명은 사기 탭에서)
        string selectedLock = crafter.GetCraftLockReason(selected); // 잠김
        typeChip.Bind(TypeLabel(selected.Result), ProjectUUIPalette.TextSecondary, tagSprite); // 분류
        stateChip.Bind(selectedLock ?? $"MAKES {selected.ResultAmount}", selectedLock != null ? ProjectUUIPalette.TextSecondary : ProjectUUIPalette.Teal, crateSprite); // 결과 수
        int selectedFee = crafter.GetCraftFee(selected); // 수수료
        string was = selectedFee < selected.Fee ? $"  <size=55%><color=#9A968C><s>{selected.Fee}</s></color></size>" : string.Empty; // 할인 전
        priceText.SetText(selectedFee > 0 ? $"{selectedFee} <size=60%>COIN FEE</size>{was}" : "FREE"); // 수수료
        priceText.color = ProjectUUIPalette.Accent; // 색
        int inBag = inventory.GetItemQuantity(selected.Result); // 가방 수량
        noteText.SetText(inBag > 0 ? $"YOU HAVE {inBag} IN YOUR BAG" : "YOU DON'T HAVE ANY YET"); // 보조 안내
        quantity = 1; // 한 번에 하나
        quantityText.SetText($"x{selected.ResultAmount}"); // 결과 수량

        string reason = crafter.GetCraftBlockReason(selected, inventory); // 불가 이유
        if (reason != null && selectedLock != null) reason = "LOCKED"; // 관계 단계 부족
        buyButton.interactable = reason == null; // 가능 여부
        buyLabel.SetText(reason ?? (selectedFee > 0 ? $"CRAFT  ·  {selectedFee}" : "CRAFT")); // 문구
        minusButton.interactable = false; // 수량 없음
        plusButton.interactable = false; // 수량 없음
    }

    private int MissingCount(NpcCraftBook.Order order) // 모자란 재료 종류 수
    {
        int missing = 0; // 수

        foreach (NpcCraftBook.Ingredient ingredient in order.Ingredients) // 재료
        {
            if (ingredient != null && ingredient.Item != null && inventory.GetItemQuantity(ingredient.Item) < ingredient.Amount) // 부족
            {
                missing++; // 추가
            }
        }

        return missing; // 결과 반환
    }

    private void RebuildPrices(SeasonType season) // 가격표 탭
    {
        selectedIndex = priceEntries.Count == 0 ? 0 : Mathf.Clamp(selectedIndex, 0, priceEntries.Count - 1); // 선택 보정

        for (int index = 0; index < priceEntries.Count; index++) // 줄 그리기
        {
            MarketPriceEntry entry = priceEntries[index]; // 항목
            MarketPriceQuote quote = market.Catalog.GetQuote(entry.Item, season); // 오늘 가격
            int inBag = inventory.GetItemQuantity(entry.Item); // 가방 수량
            string status = quote.IsFresh ? $"FRESH +{Mathf.RoundToInt((market.Catalog.FreshMultiplier - 1f) * 100f)}%" : GoodsLabel(entry.GoodsType); // 상태
            if (inBag > 0) status += $"  ·  {inBag} IN BAG"; // 가방 수량
            Color statusColor = quote.IsFresh ? ProjectUUIPalette.Teal : ProjectUUIPalette.TextSecondary; // 상태 색
            GetRow(index).Bind(index, entry.Item, status, statusColor, quote.UnitPrice, ProjectUUIPalette.Accent, index == selectedIndex, false, Select); // 표시
        }

        HideRowsFrom(priceEntries.Count); // 남는 줄
        emptyListText.gameObject.SetActive(priceEntries.Count == 0); // 빈 목록
        emptyListText.SetText("NO PRICES"); // 문구
        buyGroup.SetActive(false); // 구매 묶음 숨김
        hintText.SetText($"SHIPPING BIN SELLS AT MIDNIGHT  ·  {market.Catalog.BulkThreshold}+ OF ONE ITEM: {Mathf.RoundToInt(market.Catalog.BulkMultiplier * 100f)}% FOR THE REST"); // 안내

        if (priceEntries.Count == 0) // 목록 없음
        {
            BindEmptyDetail("NOTHING TO BUY"); // 빈 상세
            return; // 완료
        }

        MarketPriceEntry selected = priceEntries[selectedIndex]; // 선택
        MarketPriceQuote selectedQuote = market.Catalog.GetQuote(selected.Item, season); // 가격
        BindItemDetail(selected.Item); // 공통 상세
        typeChip.Bind(GoodsLabel(selected.GoodsType), ProjectUUIPalette.TextSecondary, tagSprite); // 분류
        string freshSeasons = FreshSeasonText(selected); // 제철 계절
        stateChip.Bind(selectedQuote.IsFresh ? $"IN SEASON  ·  +{Mathf.RoundToInt((market.Catalog.FreshMultiplier - 1f) * 100f)}%" : string.IsNullOrEmpty(freshSeasons) ? "SAME PRICE ALL YEAR" : $"FRESH IN {freshSeasons}", selectedQuote.IsFresh ? ProjectUUIPalette.Teal : ProjectUUIPalette.TextSecondary, leafSprite); // 제철
        priceText.SetText($"{selectedQuote.UnitPrice} <size=60%>COINS EACH</size>"); // 가격
        priceText.color = ProjectUUIPalette.Accent; // 색
        int bag = inventory.GetItemQuantity(selected.Item); // 가방 수량
        noteText.SetText(bag > 0 ? $"YOUR {bag} WOULD SELL FOR ABOUT {market.Catalog.GetBatchValue(selectedQuote.UnitPrice, bag)} COINS" : "THE SHIPPING BIN PAYS THIS PRICE AT MIDNIGHT"); // 보조 안내
    }

    private void BindItemDetail(ItemData item) // 공통 상세 (아이콘 · 이름 · 설명)
    {
        detailIcon.sprite = item.Icon; // 아이콘
        detailIcon.color = item.Icon != null ? Color.white : ItemIconUtility.GetFallbackColor(item.ItemCategory); // 색
        detailIcon.enabled = true; // 표시
        detailNameText.SetText(item.DisplayName); // 이름
        detailInfoText.SetText(item.Description); // 설명
    }

    private void BindEmptyDetail(string buttonText) // 빈 상세
    {
        detailIcon.enabled = false; // 숨김
        detailNameText.SetText("NOTHING SELECTED"); // 이름
        detailInfoText.SetText(string.Empty); // 설명
        typeChip.gameObject.SetActive(false); // 숨김
        stateChip.gameObject.SetActive(false); // 숨김
        priceText.SetText(string.Empty); // 가격
        noteText.SetText(string.Empty); // 안내
        buyButton.interactable = false; // 구매 불가
        buyLabel.SetText(buttonText); // 문구
        minusButton.interactable = false; // 감소 불가
        plusButton.interactable = false; // 증가 불가
        quantityText.SetText("x0"); // 수량
    }

    private ShopRowUI GetRow(int index) // 줄 가져오기 (없으면 만들기)
    {
        while (rows.Count <= index) // 부족한 줄
        {
            ShopRowUI created = Instantiate(rowTemplate, listRoot); // 복제
            created.name = $"Row_{rows.Count:00}"; // 이름
            rows.Add(created); // 추가
        }

        rows[index].transform.SetSiblingIndex(index + 1); // 순서 (템플릿 다음)
        return rows[index]; // 결과 반환
    }

    private void HideRowsFrom(int count) // 남는 줄 숨김
    {
        for (int index = count; index < rows.Count; index++) // 순회
        {
            rows[index].gameObject.SetActive(false); // 숨김
        }
    }

    private static void SetTabVisual(Image image, TMP_Text label, bool active) // 탭 모양
    {
        image.color = active ? ProjectUUIPalette.Accent : new Color(1f, 1f, 1f, 0.06f); // 배경
        label.color = active ? ProjectUUIPalette.TextDark : ProjectUUIPalette.TextSecondary; // 문구
    }

    private void ShowMessage(string text, Color color, float duration) // 알림 표시
    {
        if (messageText == null) // 확인
        {
            return; // 생략
        }

        bool visible = !string.IsNullOrEmpty(text) && duration > 0f; // 표시 여부
        messageText.gameObject.SetActive(visible); // 표시
        messageText.SetText(text); // 문구
        messageText.color = color; // 색
        messageHideTime = Time.unscaledTime + duration; // 숨김 시각
    }

    // ------------------------------------------------------------ 문구

    private string TypeLabel(ItemData item) // 상인 물건 분류 문구
    {
        MarketPriceQuote quote = market.Catalog.GetQuote(item, market.CurrentSeason); // 가격표

        if (quote.Sellable) // 판매 상자 분류
        {
            return GoodsLabel(quote.GoodsType); // 결과 반환
        }

        if (item.IsTool) return "TOOL"; // 도구
        if (item.IsWeapon) return "WEAPON"; // 무기
        if (item.IsEquipment) return "GEAR"; // 장비
        return item.ItemCategory == ItemCategory.Seed ? "SEED" : item.ItemCategory.ToString().ToUpperInvariant(); // 결과 반환
    }

    public static string GoodsLabel(MarketGoodsType type) // 분류 문구
    {
        switch (type) // 분류 분기
        {
            case MarketGoodsType.Crop: return "CROP"; // 작물
            case MarketGoodsType.Forage: return "FORAGE"; // 채집물
            case MarketGoodsType.Fish: return "FISH"; // 물고기
            case MarketGoodsType.AnimalProduct: return "ANIMAL PRODUCT"; // 가축 생산물
            case MarketGoodsType.Cooked: return "COOKED FOOD"; // 요리
            case MarketGoodsType.Drink: return "DRINK"; // 음료
            case MarketGoodsType.Seed: return "SEED"; // 씨앗
            case MarketGoodsType.Supply: return "SUPPLY"; // 소모품
            default: return "MATERIAL"; // 재료
        }
    }

    public static string SeasonLabel(SeasonType season) // 계절 문구
    {
        switch (season) // 계절 분기
        {
            case SeasonType.Summer: return "SUMMER"; // 여름
            case SeasonType.Autumn: return "AUTUMN"; // 가을
            case SeasonType.Winter: return "WINTER"; // 겨울
            default: return "SPRING"; // 봄
        }
    }

    private static string FreshSeasonText(MarketPriceEntry entry) // 제철 계절 문구
    {
        if (entry.FreshSeasons == null || entry.FreshSeasons.Count == 0) // 제철 없음
        {
            return string.Empty; // 없음
        }

        List<string> names = new List<string>(); // 이름

        foreach (SeasonType season in entry.FreshSeasons) // 순회
        {
            names.Add(SeasonLabel(season)); // 추가
        }

        return string.Join(" / ", names); // 결과 반환
    }
}
