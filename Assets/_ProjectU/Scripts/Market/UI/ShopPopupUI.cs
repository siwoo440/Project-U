using System.Collections.Generic; // 목록 기능
using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.InputSystem; // 키보드 입력 기능
using UnityEngine.UI; // Unity UI 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class ShopPopupUI : MonoBehaviour, IGameScenePopup // 87일차: 떠돌이 상인 창 (오늘 재고 사기 · 판매 가격표)
{
    public enum ShopTab // 창 탭
    {
        Buy = 0, // 사기
        Prices = 1 // 판매 가격표
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

    [Header("Tabs")] // 탭
    [SerializeField] private Button buyTabButton; // 사기 탭
    [SerializeField] private Image buyTabImage; // 사기 탭 배경
    [SerializeField] private TMP_Text buyTabLabel; // 사기 탭 문구
    [SerializeField] private Button pricesTabButton; // 가격표 탭
    [SerializeField] private Image pricesTabImage; // 가격표 탭 배경
    [SerializeField] private TMP_Text pricesTabLabel; // 가격표 탭 문구

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
    [SerializeField] private Button buyButton; // 구매
    [SerializeField] private TMP_Text buyLabel; // 구매 문구

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
    private GameUIManager manager; // 관리자
    private MarketStall stall; // 현재 가판대
    private MarketManager market; // 상점 관리자
    private PlayerInventory inventory; // 인벤토리
    private PlayerWallet wallet; // 지갑
    private ShopTab tab = ShopTab.Buy; // 현재 탭
    private int selectedIndex; // 선택 줄
    private int quantity = 1; // 구매 수량
    private bool isDirty; // 다시 그리기 필요
    private float messageHideTime; // 알림 숨김 시각

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf && stall != null; // 열림 여부
    public ShopTab CurrentTab => tab; // 탭 제공
    public int SelectedIndex => selectedIndex; // 선택 제공
    public int Quantity => quantity; // 수량 제공
    public IReadOnlyList<ShopRowUI> Rows => rows; // 줄 제공 (테스트용)
    public string BuyLabel => buyLabel != null ? buyLabel.text : string.Empty; // 구매 문구 제공 (테스트용)
    public bool CanPressBuy => buyButton != null && buyButton.interactable; // 구매 가능 제공 (테스트용)
    public string MessageLabel => messageText != null && messageText.gameObject.activeSelf ? messageText.text : string.Empty; // 알림 제공 (테스트용)

    private void Awake() // 버튼 연결
    {
        if (closeButton != null) closeButton.onClick.AddListener(RequestClose); // 닫기
        if (buyTabButton != null) buyTabButton.onClick.AddListener(() => SetTab(ShopTab.Buy)); // 사기 탭
        if (pricesTabButton != null) pricesTabButton.onClick.AddListener(() => SetTab(ShopTab.Prices)); // 가격표 탭
        if (minusButton != null) minusButton.onClick.AddListener(() => ChangeQuantity(-1)); // 감소
        if (plusButton != null) plusButton.onClick.AddListener(() => ChangeQuantity(1)); // 증가
        if (buyButton != null) buyButton.onClick.AddListener(Buy); // 구매
        if (rowTemplate != null) rowTemplate.gameObject.SetActive(false); // 템플릿 숨김

        if (panelRoot != null && stall == null) // 시작 상태
        {
            panelRoot.SetActive(false); // 숨김
        }
    }

    private void OnDestroy() // 구독 해제
    {
        Unsubscribe(); // 해제
    }

    public bool ShowFromManager(GameUIManager owner, MarketStall targetStall, PlayerInventory playerInventory) // 창 열기
    {
        MarketManager targetMarket = MarketManager.Instance; // 상점 관리자
        PlayerWallet targetWallet = targetMarket != null ? targetMarket.Wallet : null; // 지갑

        if (panelRoot == null || rowTemplate == null || targetStall == null || playerInventory == null || targetMarket == null || targetMarket.Catalog == null || targetWallet == null) // 참조 확인
        {
            Debug.LogError("상인 창 참조가 누락되었습니다. Tools > Project U > Market > 1. Build Market Content를 다시 실행하세요.", this); // 오류
            return false; // 실패
        }

        Unsubscribe(); // 이전 구독 해제
        manager = owner; // 관리자
        stall = targetStall; // 가판대
        market = targetMarket; // 상점
        inventory = playerInventory; // 인벤토리
        wallet = targetWallet; // 지갑
        market.StockChanged += MarkDirty; // 재고 변경 구독
        wallet.CoinsChanged += HandleCoins; // 코인 변경 구독
        inventory.InventoryChanged += MarkDirty; // 인벤토리 변경 구독
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
        stall = null; // 가판대 해제
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
        if (market != null) market.StockChanged -= MarkDirty; // 재고
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

        if (stall == null || !stall.isActiveAndEnabled || market == null) // 가판대가 사라짐
        {
            RequestClose(); // 닫기
            return; // 생략
        }

        if (!market.IsShopOpen) // 영업 종료
        {
            CombatDamagePopup.SpawnText(stall.transform.position + Vector3.up * 2.2f, "SEE YOU TOMORROW!", ProjectUUIPalette.Accent, 2f); // 인사
            RequestClose(); // 닫기
            return; // 생략
        }

        Keyboard keyboard = Keyboard.current; // 키보드

        if (keyboard != null && keyboard.tabKey.wasPressedThisFrame) // Tab으로 탭 전환
        {
            SetTab(tab == ShopTab.Buy ? ShopTab.Prices : ShopTab.Buy); // 전환
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
        if (market == null) // 열림 확인
        {
            return; // 생략
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

    public void ChangeQuantity(int delta) // 수량 바꾸기 (Shift : 10개씩)
    {
        Keyboard keyboard = Keyboard.current; // 키보드
        bool shift = keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed); // Shift
        ShopOffer offer = SelectedOffer; // 선택 물건

        if (offer == null) // 확인
        {
            return; // 생략
        }

        quantity = Mathf.Clamp(quantity + delta * (shift ? 10 : 1), 1, Mathf.Max(1, MaxQuantity(offer))); // 적용
        Rebuild(); // 다시 그리기
    }

    public void Buy() // 선택 물건 사기
    {
        ShopOffer offer = SelectedOffer; // 선택 물건

        if (offer == null || market == null) // 확인
        {
            return; // 생략
        }

        bool success = market.TryBuy(offer, quantity, inventory, out string message); // 사기
        ShowMessage(message, success ? ProjectUUIPalette.Teal : ProjectUUIPalette.Danger, messageDuration); // 알림

        if (success) // 성공
        {
            quantity = 1; // 수량 초기화
        }

        Rebuild(); // 다시 그리기
    }

    private ShopOffer SelectedOffer => tab == ShopTab.Buy && market != null && selectedIndex >= 0 && selectedIndex < market.Offers.Count ? market.Offers[selectedIndex] : null; // 선택 물건

    private int MaxQuantity(ShopOffer offer) // 최대 구매 수량 (재고·코인)
    {
        int affordable = offer.Price > 0 ? wallet.Coins / offer.Price : offer.Remaining; // 살 수 있는 수
        return Mathf.Min(offer.Remaining, affordable, 99); // 결과 반환
    }

    private int FirstAvailableOffer() // 품절이 아닌 첫 물건
    {
        MarketManager target = market != null ? market : MarketManager.Instance; // 관리자

        if (target == null) // 확인
        {
            return 0; // 기본
        }

        for (int index = 0; index < target.Offers.Count; index++) // 순회
        {
            if (!target.Offers[index].SoldOut) // 재고 있음
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

        if (market == null || inventory == null || wallet == null) // 상태 확인
        {
            return; // 생략
        }

        SeasonType season = market.CurrentSeason; // 계절
        titleText.SetText(stall.MerchantName); // 제목
        titleIcon.enabled = titleIcon.sprite != null; // 아이콘
        int left = 0; // 남은 재고 합계

        foreach (ShopOffer offer in market.Offers) // 순회
        {
            left += offer.Remaining; // 합계
        }

        infoChip.Bind($"{SeasonLabel(season)} STOCK  ·  {left} LEFT TODAY", ProjectUUIPalette.Teal, crateSprite); // 재고 안내
        coinChip.Bind($"{wallet.Coins:N0}", ProjectUUIPalette.Accent, coinSprite); // 코인
        SetTabVisual(buyTabImage, buyTabLabel, tab == ShopTab.Buy); // 사기 탭
        SetTabVisual(pricesTabImage, pricesTabLabel, tab == ShopTab.Prices); // 가격표 탭
        buyTabLabel.SetText($"BUY  ({market.Offers.Count})"); // 사기 탭 문구
        pricesTabLabel.SetText("SELL PRICES"); // 가격표 탭 문구

        if (tab == ShopTab.Buy) // 사기 탭
        {
            RebuildBuy(); // 그리기
        }
        else // 가격표 탭
        {
            RebuildPrices(season); // 그리기
        }
    }

    private void RebuildBuy() // 사기 탭
    {
        IReadOnlyList<ShopOffer> offers = market.Offers; // 재고
        selectedIndex = offers.Count == 0 ? 0 : Mathf.Clamp(selectedIndex, 0, offers.Count - 1); // 선택 보정

        for (int index = 0; index < offers.Count; index++) // 줄 그리기
        {
            ShopOffer offer = offers[index]; // 물건
            bool affordable = wallet.Coins >= offer.Price; // 살 수 있음
            string status = offer.SoldOut ? "SOLD OUT" : offer.Entry.IsSpecial ? $"RARE FIND  ·  {offer.Remaining} LEFT" : $"{offer.Remaining} LEFT"; // 상태
            Color statusColor = offer.SoldOut ? ProjectUUIPalette.Danger : offer.Entry.IsSpecial ? ProjectUUIPalette.Accent : ProjectUUIPalette.Teal; // 상태 색
            Color priceColor = offer.SoldOut || affordable ? ProjectUUIPalette.Accent : ProjectUUIPalette.Danger; // 가격 색
            GetRow(index).Bind(index, offer.Item, status, statusColor, offer.Price, priceColor, index == selectedIndex, offer.SoldOut || !affordable, Select); // 표시
        }

        HideRowsFrom(offers.Count); // 남는 줄
        emptyListText.gameObject.SetActive(offers.Count == 0); // 빈 목록
        emptyListText.SetText("THE MERCHANT HAS NOTHING TO SELL TODAY"); // 문구
        buyGroup.SetActive(true); // 구매 묶음
        hintText.SetText("BUY WITH COINS  ·  SHIFT + / - FOR 10  ·  TAB: SELL PRICES"); // 안내

        ShopOffer selected = SelectedOffer; // 선택

        if (selected == null) // 선택 없음
        {
            BindEmptyDetail(); // 빈 상세
            return; // 완료
        }

        ItemData item = selected.Item; // 아이템
        int max = MaxQuantity(selected); // 최대 수량
        quantity = Mathf.Clamp(quantity, 1, Mathf.Max(1, max)); // 수량 보정
        int cost = selected.Price * quantity; // 금액
        BindItemDetail(item); // 공통 상세
        typeChip.Bind(TypeLabel(item), ProjectUUIPalette.TextSecondary, tagSprite); // 분류
        stateChip.Bind(selected.SoldOut ? "SOLD OUT TODAY" : $"{selected.Remaining} LEFT TODAY", selected.SoldOut ? ProjectUUIPalette.Danger : ProjectUUIPalette.Teal, crateSprite); // 재고
        priceText.SetText($"{selected.Price} <size=60%>COINS EACH</size>"); // 가격
        priceText.color = ProjectUUIPalette.Accent; // 색
        int inBag = inventory.GetItemQuantity(item); // 가방 수량
        noteText.SetText(inBag > 0 ? $"YOU HAVE {inBag} IN YOUR BAG" : "YOU DON'T HAVE ANY YET"); // 보조 안내
        quantityText.SetText($"x{quantity}"); // 수량

        string reason = null; // 불가 이유

        if (selected.SoldOut) reason = "SOLD OUT"; // 품절
        else if (!wallet.CanAfford(selected.Price * quantity)) reason = $"NEED {selected.Price * quantity - wallet.Coins} MORE COINS"; // 코인 부족
        else if (!inventory.CanAddItem(item, quantity)) reason = "BAG IS FULL"; // 가방 부족

        buyButton.interactable = reason == null; // 가능 여부
        buyLabel.SetText(reason ?? $"BUY x{quantity}  ·  {cost}"); // 문구
        minusButton.interactable = quantity > 1; // 감소
        plusButton.interactable = quantity < max; // 증가
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
            BindEmptyDetail(); // 빈 상세
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

    private void BindEmptyDetail() // 빈 상세
    {
        detailIcon.enabled = false; // 숨김
        detailNameText.SetText("NOTHING SELECTED"); // 이름
        detailInfoText.SetText(string.Empty); // 설명
        typeChip.gameObject.SetActive(false); // 숨김
        stateChip.gameObject.SetActive(false); // 숨김
        priceText.SetText(string.Empty); // 가격
        noteText.SetText(string.Empty); // 안내
        buyButton.interactable = false; // 구매 불가
        buyLabel.SetText("NOTHING TO BUY"); // 문구
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
        return quote.Sellable ? GoodsLabel(quote.GoodsType) : item.ItemCategory == ItemCategory.Seed ? "SEED" : item.ItemCategory.ToString().ToUpperInvariant(); // 결과 반환
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
