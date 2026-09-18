using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static UIBuildKit;

// 87일차: 상인 창(PopupLayer), 코인 알약(생존 게이지 아래), 아침 판매 알림(화면 위 가운데),
// 보관함 창의 판매 상자 안내 문구를 만든다. 실행할 때마다 같은 이름의 오브젝트를 지우고 새로 만든다.
// 92일차: 상인 창에 NPC 상점 주인 한마디 칸 추가, 상인 창만 다시 만드는 기능(10번 메뉴에서 사용)
public static class MarketPopupUIBuilder
{
    public const string PopupRootName = "LP_ShopPopup";
    public const string CoinRootName = "LP_CoinHUD";
    public const string BannerRootName = "LP_SaleBanner";
    public const string StorageInfoName = "StorageInfoText";

    private const string PopupLayerName = "PopupLayer";
    private const string SurvivalPanelName = "LP_SurvivalPanel";
    private const string HealthBarName = "--- HealthBarRoot ---";
    private const float WindowWidth = 1040f;
    private const float WindowHeight = 680f;
    private const float Pad = 24f;
    private const float ListWidth = 400f;
    private const float BodyTop = -124f;
    private const float BodyHeight = 470f;
    private const float RowHeight = 52f;
    private const float CoinHeight = 30f;

    public static string Build(Sprite merchantIcon, out ShopPopupUI popup, out CoinHUD coinHud)
    {
        popup = null;
        coinHud = null;

        if (UISpriteFactory.Panel == null || UISpriteFactory.Pill == null || UISpriteFactory.CircleSprite == null)
        {
            UISpriteFactory.GenerateAll();
        }

        RectTransform popupLayer = FindRect(PopupLayerName);

        if (popupLayer == null)
        {
            return "[경고] PopupLayer를 찾지 못해 상인 창을 만들지 못했습니다.";
        }

        popup = BuildPopup(popupLayer, merchantIcon);
        string hud = BuildCoinHUD(out coinHud);
        return $"상인 창 생성 (PopupLayer) · {hud}";
    }

    public static string RebuildShopPopup(out ShopPopupUI popup) // 92일차: 상인 창만 다시 만들고 GameUIManager에 연결 (NPC 상점 한마디 칸)
    {
        popup = null;

        if (UISpriteFactory.Panel == null || UISpriteFactory.Pill == null || UISpriteFactory.CircleSprite == null)
        {
            UISpriteFactory.GenerateAll();
        }

        RectTransform popupLayer = FindRect(PopupLayerName);

        if (popupLayer == null)
        {
            return "✗ PopupLayer를 찾지 못해 상인 창을 만들지 못했습니다.";
        }

        popup = BuildPopup(popupLayer, AssetDatabase.LoadAssetAtPath<Sprite>(MarketContentBuilder.MerchantIconPath));
        GameUIManager uiManager = Object.FindFirstObjectByType<GameUIManager>(FindObjectsInactive.Include);

        if (uiManager == null)
        {
            return "✗ GameUIManager가 없어 상인 창을 연결하지 못했습니다.";
        }

        SerializedObject serialized = new SerializedObject(uiManager);
        serialized.FindProperty("shopPopup").objectReferenceValue = popup;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(uiManager);
        return "상인 창 다시 생성 (NPC 한마디 칸 포함) · GameUIManager 연결";
    }

    // ------------------------------------------------------------ 상인 창

    private static ShopPopupUI BuildPopup(RectTransform popupLayer, Sprite merchantIcon)
    {
        RemoveExisting(popupLayer, PopupRootName);
        RectTransform root = CreateRect(popupLayer, PopupRootName);
        Stretch(root);
        root.SetAsFirstSibling();

        RectTransform panel = CreateRect(root, "LP_Panel");
        Stretch(panel);

        Image dim = CreateImage(panel, "LP_Dim", null, new Color(0.02f, 0.025f, 0.035f, 0.72f));
        dim.raycastTarget = true;
        Stretch(dim.rectTransform);

        Image shadow = CreateImage(panel, "LP_Shadow", UISpriteFactory.Shadow, Color.white);
        shadow.type = Image.Type.Sliced;
        Place(shadow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(WindowWidth + 80f, WindowHeight + 80f));

        Image window = CreateImage(panel, "LP_Window", UISpriteFactory.Panel, ProjectUUIPalette.PanelDark);
        window.type = Image.Type.Sliced;
        window.raycastTarget = true;
        Place(window.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(WindowWidth, WindowHeight));
        RectTransform w = window.rectTransform;

        Image outline = CreateImage(w, "LP_Outline", UISpriteFactory.PanelOutline, new Color(0.95f, 0.72f, 0.3f, 0.25f));
        outline.type = Image.Type.Sliced;
        Stretch(outline.rectTransform);

        // 머리글
        Image titleIcon = CreateImage(w, "LP_TitleIcon", merchantIcon, Color.white);
        titleIcon.preserveAspect = true;
        TopLeft(titleIcon.rectTransform, new Vector2(Pad - 6f, -8f), new Vector2(50f, 50f));
        TMP_Text title = CreateText(w, "LP_Title", "TRAVELING MERCHANT", 24f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, ProjectUUIPalette.TextPrimary);
        title.characterSpacing = 2f;
        TopLeft(title.rectTransform, new Vector2(Pad + 50f, -14f), new Vector2(380f, 38f));

        RectTransform coinParent = CreateRect(w, "LP_Coins");
        Place(coinParent, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-(Pad + 90f), -19f), new Vector2(150f, 28f));
        HorizontalLayoutGroup coinLayout = coinParent.gameObject.AddComponent<HorizontalLayoutGroup>();
        coinLayout.childAlignment = TextAnchor.MiddleRight;
        ConfigureLayout(coinLayout, true);
        CookingChipUI coinChip = CreateChip(coinParent, "LP_CoinChip", 28f, 15f, false);

        RectTransform infoParent = CreateRect(w, "LP_Info");
        Place(infoParent, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-(Pad + 250f), -19f), new Vector2(360f, 28f));
        HorizontalLayoutGroup infoLayout = infoParent.gameObject.AddComponent<HorizontalLayoutGroup>();
        infoLayout.childAlignment = TextAnchor.MiddleRight;
        ConfigureLayout(infoLayout, true);
        CookingChipUI infoChip = CreateChip(infoParent, "LP_InfoChip", 28f, 12f, false);

        Button close = CreateButton(w, "LP_Close", "ESC", 14f, ProjectUUIPalette.ButtonNormal, ProjectUUIPalette.TextPrimary, out _, out _);
        Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-Pad, -15f), new Vector2(76f, 36f));

        Image divider = CreateImage(w, "LP_Divider", null, Faint);
        Place(divider.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -64f), new Vector2(WindowWidth - Pad * 2f, 1f));

        // 탭
        Button buyTab = CreateButton(w, "LP_TabBuy", "BUY", 14f, ProjectUUIPalette.Accent, ProjectUUIPalette.TextDark, out Image buyTabImage, out TMP_Text buyTabLabel);
        buyTabLabel.characterSpacing = 2f;
        TopLeft((RectTransform)buyTab.transform, new Vector2(Pad, -78f), new Vector2(190f, 34f));
        Button pricesTab = CreateButton(w, "LP_TabPrices", "SELL PRICES", 14f, new Color(1f, 1f, 1f, 0.06f), ProjectUUIPalette.TextSecondary, out Image pricesTabImage, out TMP_Text pricesTabLabel);
        pricesTabLabel.characterSpacing = 2f;
        TopLeft((RectTransform)pricesTab.transform, new Vector2(Pad + 200f, -78f), new Vector2(190f, 34f));
        TMP_Text tabHint = CreateText(w, "LP_TabHint", "TAB", 10.5f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, new Color(0.72f, 0.7f, 0.65f, 0.6f));
        tabHint.characterSpacing = 2f;
        TopLeft(tabHint.rectTransform, new Vector2(Pad + 398f, -78f), new Vector2(60f, 34f));

        // 92일차: NPC 상점 주인의 한마디 (탭 오른쪽, 가판대 상인은 숨김)
        TMP_Text speech = CreateText(w, "LP_Speech", "\"어서 와요\"", 15f, FontStyles.Normal, TextAlignmentOptions.MidlineRight, new Color(0.96f, 0.88f, 0.72f, 1f));
        speech.textWrappingMode = TextWrappingModes.NoWrap;
        speech.overflowMode = TextOverflowModes.Ellipsis;
        TopLeft(speech.rectTransform, new Vector2(Pad + 460f, -78f), new Vector2(WindowWidth - Pad * 2f - 460f, 34f));
        speech.gameObject.SetActive(false);

        // 왼쪽 목록
        Image viewport = CreateImage(w, "LP_ListScroll", null, new Color(0f, 0f, 0f, 0f));
        viewport.raycastTarget = true;
        TopLeft(viewport.rectTransform, new Vector2(Pad, BodyTop), new Vector2(ListWidth, BodyHeight));
        viewport.gameObject.AddComponent<RectMask2D>();

        RectTransform list = CreateRect(viewport.rectTransform, "LP_List");
        list.anchorMin = new Vector2(0f, 1f);
        list.anchorMax = new Vector2(1f, 1f);
        list.pivot = new Vector2(0.5f, 1f);
        list.anchoredPosition = Vector2.zero;
        list.sizeDelta = new Vector2(-10f, BodyHeight);
        VerticalLayoutGroup listLayout = list.gameObject.AddComponent<VerticalLayoutGroup>();
        listLayout.spacing = 4f;
        listLayout.childAlignment = TextAnchor.UpperLeft;
        listLayout.childControlWidth = true;
        listLayout.childControlHeight = false;
        listLayout.childForceExpandWidth = true;
        listLayout.childForceExpandHeight = false;
        ContentSizeFitter listFitter = list.gameObject.AddComponent<ContentSizeFitter>();
        listFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        listFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        ShopRowUI rowTemplate = CreateRow(list);

        ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.content = list;
        scroll.viewport = viewport.rectTransform;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 28f;
        scroll.inertia = true;

        Image track = CreateImage(w, "LP_ScrollTrack", UISpriteFactory.Pill, Faint);
        track.type = Image.Type.Sliced;
        track.pixelsPerUnitMultiplier = 8f;
        TopLeft(track.rectTransform, new Vector2(Pad + ListWidth - 4f, BodyTop), new Vector2(4f, BodyHeight));
        Scrollbar scrollbar = track.gameObject.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        RectTransform handleArea = CreateRect(track.rectTransform, "LP_HandleArea");
        Stretch(handleArea);
        Image handle = CreateImage(handleArea, "LP_Handle", UISpriteFactory.Pill, new Color(0.95f, 0.72f, 0.3f, 0.7f));
        handle.type = Image.Type.Sliced;
        handle.pixelsPerUnitMultiplier = 8f;
        handle.raycastTarget = true;
        Stretch(handle.rectTransform);
        scrollbar.handleRect = handle.rectTransform;
        scrollbar.targetGraphic = handle;
        Navigation navigation = scrollbar.navigation;
        navigation.mode = Navigation.Mode.None;
        scrollbar.navigation = navigation;
        scroll.verticalScrollbar = scrollbar;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        TMP_Text emptyList = CreateText(viewport.rectTransform, "LP_EmptyList", "NOTHING TODAY", 14f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.72f, 0.7f, 0.65f, 0.7f));
        emptyList.textWrappingMode = TextWrappingModes.Normal;
        SetOffsets(emptyList.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(20f, -30f), new Vector2(-20f, 30f));
        emptyList.gameObject.SetActive(false);

        // 오른쪽 상세
        Image detail = CreateImage(w, "LP_Detail", UISpriteFactory.Panel, ProjectUUIPalette.PanelMid);
        detail.type = Image.Type.Sliced;
        float detailWidth = WindowWidth - Pad * 2f - ListWidth - 20f;
        TopLeft(detail.rectTransform, new Vector2(Pad + ListWidth + 20f, BodyTop), new Vector2(detailWidth, BodyHeight));
        RectTransform d = detail.rectTransform;

        Image iconBack = CreateImage(d, "LP_IconBack", UISpriteFactory.Slot, new Color(0.2f, 0.23f, 0.28f, 1f));
        iconBack.type = Image.Type.Sliced;
        TopLeft(iconBack.rectTransform, new Vector2(20f, -20f), new Vector2(110f, 110f));
        Image detailIcon = CreateImage(iconBack.rectTransform, "LP_Icon", null, Color.white);
        detailIcon.preserveAspect = true;
        Place(detailIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(94f, 94f));

        TMP_Text detailName = CreateText(d, "LP_Name", "ITEM", 26f, FontStyles.Bold, TextAlignmentOptions.TopLeft, ProjectUUIPalette.TextPrimary);
        TopLeft(detailName.rectTransform, new Vector2(148f, -20f), new Vector2(detailWidth - 168f, 34f));
        TMP_Text detailInfo = CreateText(d, "LP_Info", "Description", 13f, FontStyles.Normal, TextAlignmentOptions.TopLeft, ProjectUUIPalette.TextSecondary);
        detailInfo.textWrappingMode = TextWrappingModes.Normal;
        detailInfo.overflowMode = TextOverflowModes.Ellipsis;
        TopLeft(detailInfo.rectTransform, new Vector2(148f, -58f), new Vector2(detailWidth - 168f, 40f));

        RectTransform chips = CreateRect(d, "LP_Chips");
        TopLeft(chips, new Vector2(148f, -102f), new Vector2(detailWidth - 168f, 26f));
        HorizontalLayoutGroup chipLayout = chips.gameObject.AddComponent<HorizontalLayoutGroup>();
        chipLayout.spacing = 6f;
        chipLayout.childAlignment = TextAnchor.MiddleLeft;
        ConfigureLayout(chipLayout, true);
        CookingChipUI typeChip = CreateChip(chips, "LP_TypeChip", 26f, 12f, false);
        CookingChipUI stateChip = CreateChip(chips, "LP_StateChip", 26f, 12f, false);

        Image priceBack = CreateImage(d, "LP_PriceBack", UISpriteFactory.Panel, new Color(1f, 1f, 1f, 0.035f));
        priceBack.type = Image.Type.Sliced;
        priceBack.pixelsPerUnitMultiplier = 1.6f;
        TopLeft(priceBack.rectTransform, new Vector2(20f, -150f), new Vector2(detailWidth - 40f, 92f));
        Image priceCoin = CreateImage(priceBack.rectTransform, "LP_PriceCoin", UISpriteFactory.Icon("Coin"), ProjectUUIPalette.Accent);
        priceCoin.preserveAspect = true;
        Place(priceCoin.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -12f), new Vector2(38f, 38f));
        TMP_Text price = CreateText(priceBack.rectTransform, "LP_Price", "0 COINS EACH", 32f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, ProjectUUIPalette.Accent);
        price.richText = true;
        price.overflowMode = TextOverflowModes.Overflow;
        Place(price.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(66f, -8f), new Vector2(detailWidth - 120f, 46f));
        TMP_Text note = CreateText(priceBack.rectTransform, "LP_Note", "NOTE", 13f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, ProjectUUIPalette.TextSecondary);
        note.characterSpacing = 0.5f;
        Place(note.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(18f, 10f), new Vector2(detailWidth - 76f, 24f));

        // 수량과 구매 버튼
        RectTransform buyGroup = CreateRect(d, "LP_BuyGroup");
        SetOffsets(buyGroup, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 110f));
        TMP_Text qtyLabel = CreateLabel(buyGroup, "LP_QuantityLabel", "QUANTITY");
        Place(qtyLabel.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(20f, 76f), new Vector2(200f, 18f));
        Button minus = CreateButton(buyGroup, "LP_Minus", "-", 22f, ProjectUUIPalette.ButtonNormal, ProjectUUIPalette.TextPrimary, out _, out _);
        Place((RectTransform)minus.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(20f, 20f), new Vector2(44f, 44f));
        TMP_Text quantity = CreateText(buyGroup, "LP_Quantity", "x1", 22f, FontStyles.Bold, TextAlignmentOptions.Center, ProjectUUIPalette.TextPrimary);
        Place(quantity.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(70f, 20f), new Vector2(64f, 44f));
        Button plus = CreateButton(buyGroup, "LP_Plus", "+", 22f, ProjectUUIPalette.ButtonNormal, ProjectUUIPalette.TextPrimary, out _, out _);
        Place((RectTransform)plus.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(140f, 20f), new Vector2(44f, 44f));
        Button buy = CreateButton(buyGroup, "LP_Buy", "BUY", 17f, ProjectUUIPalette.Accent, ProjectUUIPalette.TextDark, out _, out TMP_Text buyLabel);
        buyLabel.characterSpacing = 1.5f;
        Place((RectTransform)buy.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 20f), new Vector2(300f, 52f));

        // 아래쪽
        TMP_Text hint = CreateText(w, "LP_Hint", "HINT", 11.5f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, ProjectUUIPalette.TextSecondary);
        hint.characterSpacing = 1f;
        Place(hint.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(Pad, 22f), new Vector2(560f, 24f));
        TMP_Text message = CreateText(w, "LP_Message", string.Empty, 15f, FontStyles.Bold, TextAlignmentOptions.MidlineRight, ProjectUUIPalette.Teal);
        Place(message.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-Pad, 22f), new Vector2(420f, 26f));
        message.gameObject.SetActive(false);

        panel.gameObject.SetActive(false);

        ShopPopupUI ui = root.gameObject.AddComponent<ShopPopupUI>();
        SerializedObject so = new SerializedObject(ui);
        so.FindProperty("panelRoot").objectReferenceValue = panel.gameObject;
        so.FindProperty("titleText").objectReferenceValue = title;
        so.FindProperty("titleIcon").objectReferenceValue = titleIcon;
        so.FindProperty("infoChip").objectReferenceValue = infoChip;
        so.FindProperty("coinChip").objectReferenceValue = coinChip;
        so.FindProperty("closeButton").objectReferenceValue = close;
        so.FindProperty("speechText").objectReferenceValue = speech;
        so.FindProperty("buyTabButton").objectReferenceValue = buyTab;
        so.FindProperty("buyTabImage").objectReferenceValue = buyTabImage;
        so.FindProperty("buyTabLabel").objectReferenceValue = buyTabLabel;
        so.FindProperty("pricesTabButton").objectReferenceValue = pricesTab;
        so.FindProperty("pricesTabImage").objectReferenceValue = pricesTabImage;
        so.FindProperty("pricesTabLabel").objectReferenceValue = pricesTabLabel;
        so.FindProperty("listRoot").objectReferenceValue = list;
        so.FindProperty("listScroll").objectReferenceValue = scroll;
        so.FindProperty("rowTemplate").objectReferenceValue = rowTemplate;
        so.FindProperty("emptyListText").objectReferenceValue = emptyList;
        so.FindProperty("detailIcon").objectReferenceValue = detailIcon;
        so.FindProperty("detailNameText").objectReferenceValue = detailName;
        so.FindProperty("detailInfoText").objectReferenceValue = detailInfo;
        so.FindProperty("typeChip").objectReferenceValue = typeChip;
        so.FindProperty("stateChip").objectReferenceValue = stateChip;
        so.FindProperty("priceText").objectReferenceValue = price;
        so.FindProperty("noteText").objectReferenceValue = note;
        so.FindProperty("buyGroup").objectReferenceValue = buyGroup.gameObject;
        so.FindProperty("minusButton").objectReferenceValue = minus;
        so.FindProperty("plusButton").objectReferenceValue = plus;
        so.FindProperty("quantityText").objectReferenceValue = quantity;
        so.FindProperty("buyButton").objectReferenceValue = buy;
        so.FindProperty("buyLabel").objectReferenceValue = buyLabel;
        so.FindProperty("hintText").objectReferenceValue = hint;
        so.FindProperty("messageText").objectReferenceValue = message;
        so.FindProperty("coinSprite").objectReferenceValue = UISpriteFactory.Icon("Coin");
        so.FindProperty("tagSprite").objectReferenceValue = UISpriteFactory.Icon("Tag");
        so.FindProperty("crateSprite").objectReferenceValue = UISpriteFactory.Icon("Crate");
        so.FindProperty("leafSprite").objectReferenceValue = UISpriteFactory.Icon("Leaf");
        so.ApplyModifiedPropertiesWithoutUndo();
        return ui;
    }

    private static ShopRowUI CreateRow(RectTransform parent)
    {
        Image background = CreateImage(parent, "LP_RowTemplate", UISpriteFactory.Panel, new Color(1f, 1f, 1f, 0.035f));
        background.type = Image.Type.Sliced;
        background.pixelsPerUnitMultiplier = 1.6f;
        background.raycastTarget = true;
        RectTransform row = background.rectTransform;
        row.sizeDelta = new Vector2(ListWidth, RowHeight);
        Button button = background.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        SetTint(button, 1.12f);

        Image outline = CreateImage(row, "LP_Selected", UISpriteFactory.PanelOutline, ProjectUUIPalette.Accent);
        outline.type = Image.Type.Sliced;
        outline.pixelsPerUnitMultiplier = 1.6f;
        Stretch(outline.rectTransform);
        Image accent = CreateImage(outline.rectTransform, "LP_AccentBar", UISpriteFactory.Pill, ProjectUUIPalette.Accent);
        accent.type = Image.Type.Sliced;
        accent.pixelsPerUnitMultiplier = 6f;
        Place(accent.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(3f, 0f), new Vector2(4f, 28f));

        Image iconBack = CreateImage(row, "LP_IconBack", UISpriteFactory.Slot, new Color(1f, 1f, 1f, 0.06f));
        iconBack.type = Image.Type.Sliced;
        iconBack.pixelsPerUnitMultiplier = 2f;
        Place(iconBack.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(40f, 40f));
        Image icon = CreateImage(iconBack.rectTransform, "LP_Icon", null, Color.white);
        icon.preserveAspect = true;
        Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(36f, 36f));

        TMP_Text name = CreateText(row, "LP_Name", "ITEM", 15f, FontStyles.Bold, TextAlignmentOptions.BottomLeft, ProjectUUIPalette.TextPrimary);
        SetOffsets(name.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(62f, 0f), new Vector2(-96f, -5f));
        TMP_Text status = CreateText(row, "LP_Status", "12 LEFT", 10.5f, FontStyles.Bold, TextAlignmentOptions.TopLeft, ProjectUUIPalette.Teal);
        status.characterSpacing = 1.5f;
        SetOffsets(status.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(62f, 4f), new Vector2(-96f, -2f));

        TMP_Text price = CreateText(row, "LP_Price", "0", 17f, FontStyles.Bold, TextAlignmentOptions.MidlineRight, ProjectUUIPalette.Accent);
        price.overflowMode = TextOverflowModes.Overflow;
        Place(price.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-38f, 0f), new Vector2(70f, 28f));
        Image priceIcon = CreateImage(row, "LP_PriceIcon", UISpriteFactory.Icon("Coin"), ProjectUUIPalette.Accent);
        priceIcon.preserveAspect = true;
        Place(priceIcon.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(20f, 20f));

        ShopRowUI ui = background.gameObject.AddComponent<ShopRowUI>();
        SerializedObject so = new SerializedObject(ui);
        so.FindProperty("button").objectReferenceValue = button;
        so.FindProperty("background").objectReferenceValue = background;
        so.FindProperty("selectionOutline").objectReferenceValue = outline;
        so.FindProperty("iconBackground").objectReferenceValue = iconBack;
        so.FindProperty("icon").objectReferenceValue = icon;
        so.FindProperty("nameText").objectReferenceValue = name;
        so.FindProperty("statusText").objectReferenceValue = status;
        so.FindProperty("priceText").objectReferenceValue = price;
        so.FindProperty("priceIcon").objectReferenceValue = priceIcon;
        so.ApplyModifiedPropertiesWithoutUndo();
        background.gameObject.SetActive(false);
        return ui;
    }

    // ------------------------------------------------------------ 코인 알약 · 판매 알림

    public static RectTransform FindCoinRoot(Transform hudParent)
    {
        return hudParent != null ? FindChildRect(hudParent, CoinRootName) : null;
    }

    private static string BuildCoinHUD(out CoinHUD hud)
    {
        hud = null;
        RectTransform healthBar = FindRect(HealthBarName);

        if (healthBar == null || healthBar.parent == null)
        {
            return "[경고] 생존 게이지가 없어 코인 표시를 만들지 못함";
        }

        Transform parent = healthBar.parent;
        RemoveExisting(parent, CoinRootName);
        RemoveExisting(parent, BannerRootName);
        RectTransform survivalPanel = FindChildRect(parent, SurvivalPanelName);
        float top = survivalPanel != null ? survivalPanel.anchoredPosition.y - survivalPanel.sizeDelta.y - 8f : -216f;
        float left = survivalPanel != null ? survivalPanel.anchoredPosition.x : 14f;

        // 코인 알약 (생존 게이지 바로 아래)
        RectTransform root = CreateRect(parent, CoinRootName);
        TopLeft(root, new Vector2(left, top), new Vector2(150f, CoinHeight));
        HorizontalLayoutGroup layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        ConfigureLayout(layout, true);
        CookingChipUI chip = CreateChip(root, "LP_CoinChip", CoinHeight, 16f, false);
        SerializedObject chipSerialized = new SerializedObject(chip);
        chipSerialized.FindProperty("backgroundAlpha").floatValue = 0.3f;
        chipSerialized.ApplyModifiedPropertiesWithoutUndo();
        chip.Bind("0", ProjectUUIPalette.Accent, UISpriteFactory.Icon("Coin"));

        TMP_Text delta = CreateText(root, "LP_CoinDelta", "+0", 16f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, ProjectUUIPalette.Accent);
        delta.overflowMode = TextOverflowModes.Overflow;
        LayoutElement ignore = delta.gameObject.AddComponent<LayoutElement>();
        ignore.ignoreLayout = true;
        Place(delta.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(158f, 0f), new Vector2(120f, CoinHeight));
        delta.gameObject.SetActive(false);

        // 아침 판매 알림 (화면 위 가운데)
        Image banner = CreateImage(parent, BannerRootName, UISpriteFactory.Panel, new Color(0.075f, 0.09f, 0.115f, 0.94f));
        banner.type = Image.Type.Sliced;
        Place(banner.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(660f, 76f));
        RectTransform b = banner.rectTransform;
        Image bannerOutline = CreateImage(b, "LP_Outline", UISpriteFactory.PanelOutline, new Color(0.95f, 0.72f, 0.3f, 0.45f));
        bannerOutline.type = Image.Type.Sliced;
        Stretch(bannerOutline.rectTransform);
        Image coinBack = CreateImage(b, "LP_CoinBack", UISpriteFactory.CircleSprite, new Color(0.95f, 0.72f, 0.3f, 0.16f));
        Place(coinBack.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(52f, 52f));
        Image coinIcon = CreateImage(coinBack.rectTransform, "LP_Icon", UISpriteFactory.Icon("Coin"), ProjectUUIPalette.Accent);
        coinIcon.preserveAspect = true;
        Place(coinIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(32f, 32f));
        TMP_Text bannerTitle = CreateText(b, "LP_Title", "SHIPPING BIN", 12f, FontStyles.Bold, TextAlignmentOptions.BottomLeft, ProjectUUIPalette.TextSecondary);
        bannerTitle.characterSpacing = 2.5f;
        SetOffsets(bannerTitle.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(80f, 2f), new Vector2(-150f, -10f));
        TMP_Text bannerDetail = CreateText(b, "LP_Detail", "12 ITEMS", 15f, FontStyles.Bold, TextAlignmentOptions.TopLeft, ProjectUUIPalette.TextPrimary);
        SetOffsets(bannerDetail.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(80f, 8f), new Vector2(-150f, -4f));
        TMP_Text bannerCoins = CreateText(b, "LP_Coins", "+0", 30f, FontStyles.Bold, TextAlignmentOptions.MidlineRight, ProjectUUIPalette.Accent);
        bannerCoins.overflowMode = TextOverflowModes.Overflow;
        Place(bannerCoins.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-20f, 0f), new Vector2(130f, 44f));
        banner.gameObject.SetActive(false);

        hud = root.gameObject.AddComponent<CoinHUD>();
        SerializedObject so = new SerializedObject(hud);
        so.FindProperty("coinChip").objectReferenceValue = chip;
        so.FindProperty("coinSprite").objectReferenceValue = UISpriteFactory.Icon("Coin");
        so.FindProperty("deltaText").objectReferenceValue = delta;
        so.FindProperty("banner").objectReferenceValue = b;
        so.FindProperty("bannerTitle").objectReferenceValue = bannerTitle;
        so.FindProperty("bannerDetail").objectReferenceValue = bannerDetail;
        so.FindProperty("bannerCoins").objectReferenceValue = bannerCoins;
        so.FindProperty("sleepSystem").objectReferenceValue = Object.FindFirstObjectByType<SleepSystem>(FindObjectsInactive.Include);
        so.ApplyModifiedPropertiesWithoutUndo();

        // 음식 효과 알약을 코인 아래로 내림 (요리 생성 도구도 같은 규칙을 쓴다)
        RectTransform buffs = FindChildRect(parent, CookingPopupUIBuilder.BuffRootName);
        string buffText = string.Empty;

        if (buffs != null)
        {
            Vector2 position = buffs.anchoredPosition;
            position.y = top - CoinHeight - 6f;
            buffs.anchoredPosition = position;
            buffText = $", 음식 효과 표시 y {position.y:0}";
        }

        return $"코인 표시 (생존 게이지 아래 y {top:0}{buffText}) · 판매 알림 (화면 위)";
    }

    // ------------------------------------------------------------ 보관함 창 안내 문구

    public static string PatchStoragePopup(StorageContainerUI prefab)
    {
        if (prefab == null)
        {
            return "[경고] 보관함 창 Prefab을 찾지 못해 판매 상자 안내를 추가하지 못했습니다.";
        }

        string path = AssetDatabase.GetAssetPath(prefab);

        if (string.IsNullOrEmpty(path))
        {
            return "[경고] 보관함 창 Prefab 경로가 없습니다.";
        }

        GameObject root = PrefabUtility.LoadPrefabContents(path);

        try
        {
            StorageContainerUI ui = root.GetComponentInChildren<StorageContainerUI>(true);
            Transform area = FindDeep(root.transform, "StorageArea");
            Transform title = area != null ? area.Find("StorageTitle") : null;

            if (ui == null || area == null)
            {
                return "[경고] 보관함 창 구조(StorageArea)를 찾지 못했습니다.";
            }

            Transform existing = area.Find(StorageInfoName);

            while (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
                existing = area.Find(StorageInfoName);
            }

            GameObject created = new GameObject(StorageInfoName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            created.layer = area.gameObject.layer;
            created.transform.SetParent(area, false);
            TMP_Text info = created.GetComponent<TMP_Text>();
            TMP_Text titleText = title != null ? title.GetComponent<TMP_Text>() : null;
            info.font = titleText != null ? titleText.font : TMP_Settings.defaultFontAsset;
            info.fontSize = 15f;
            info.enableAutoSizing = true;
            info.fontSizeMin = 10f;
            info.fontSizeMax = 14f;
            info.fontStyle = FontStyles.Bold;
            info.characterSpacing = 1f;
            info.alignment = TextAlignmentOptions.MidlineLeft;
            info.color = ProjectUUIPalette.Accent;
            info.textWrappingMode = TextWrappingModes.NoWrap;
            info.overflowMode = TextOverflowModes.Ellipsis;
            info.raycastTarget = false;
            info.text = "SELLS AT MIDNIGHT";

            // 제목(STORAGE) 바로 아래 줄, 칸 목록 너비만큼 (오른쪽은 미니맵에 가려지므로 쓰지 않음)
            RectTransform rect = info.rectTransform;
            RectTransform titleRect = title as RectTransform;
            float titleBottom = titleRect != null ? titleRect.anchoredPosition.y - titleRect.sizeDelta.y : -27f;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(22f, titleBottom - 4f);
            rect.sizeDelta = new Vector2(430f, 24f);

            created.SetActive(false);
            SerializedObject so = new SerializedObject(ui);
            so.FindProperty("infoText").objectReferenceValue = info;
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        return $"보관함 창에 판매 상자 안내 문구 추가 ({System.IO.Path.GetFileName(path)})";
    }

    private static Transform FindDeep(Transform parent, string name)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name)
            {
                return child;
            }
        }

        return null;
    }
}
