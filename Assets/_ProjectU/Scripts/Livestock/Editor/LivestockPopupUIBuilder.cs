using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static UIBuildKit;

// 86일차: 우리 창(PopupLayer)과 동물 머리 위 상태 표시(HUD)를 만든다.
// 실행할 때마다 같은 이름의 오브젝트를 지우고 새로 만든다.
public static class LivestockPopupUIBuilder
{
    public const string PopupRootName = "LP_AnimalPenPopup";
    public const string StatusRootName = "LP_AnimalStatusHUD";

    private const string PopupLayerName = "PopupLayer";
    private const string PromptName = "--- InteractionPrompt ---";
    private const float WindowWidth = 1000f;
    private const float WindowHeight = 680f;
    private const float Pad = 28f;
    private static readonly Vector2 CardSize = new Vector2(304f, 430f);

    public static string Build(FoodEffectIconSet iconSet, out AnimalPenPopupUI popup)
    {
        popup = null;

        if (UISpriteFactory.Panel == null || UISpriteFactory.Pill == null || UISpriteFactory.CircleSprite == null)
        {
            UISpriteFactory.GenerateAll();
        }

        RectTransform popupLayer = FindRect(PopupLayerName);

        if (popupLayer == null)
        {
            return "[경고] PopupLayer를 찾지 못해 우리 창을 만들지 못했습니다.";
        }

        popup = BuildPopup(popupLayer, iconSet);
        return "우리 창 생성 (PopupLayer) · " + BuildStatusHUD(iconSet);
    }

    // ------------------------------------------------------------ 우리 창

    private static AnimalPenPopupUI BuildPopup(RectTransform popupLayer, FoodEffectIconSet iconSet)
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

        Image outline = CreateImage(w, "LP_Outline", UISpriteFactory.PanelOutline, new Color(0.31f, 0.76f, 0.69f, 0.25f));
        outline.type = Image.Type.Sliced;
        Stretch(outline.rectTransform);

        // 머리글
        Image titleIcon = CreateImage(w, "LP_TitleIcon", null, Color.white);
        titleIcon.preserveAspect = true;
        TopLeft(titleIcon.rectTransform, new Vector2(Pad - 4f, -10f), new Vector2(46f, 46f));
        TMP_Text title = CreateText(w, "LP_Title", "CHICKEN COOP", 24f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, ProjectUUIPalette.TextPrimary);
        title.characterSpacing = 2f;
        TopLeft(title.rectTransform, new Vector2(Pad + 50f, -14f), new Vector2(360f, 38f));

        RectTransform infoParent = CreateRect(w, "LP_Info");
        Place(infoParent, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-(Pad + 92f), -19f), new Vector2(460f, 28f));
        HorizontalLayoutGroup infoLayout = infoParent.gameObject.AddComponent<HorizontalLayoutGroup>();
        infoLayout.childAlignment = TextAnchor.MiddleRight;
        ConfigureLayout(infoLayout, true);
        CookingChipUI infoChip = CreateChip(infoParent, "LP_InfoChip", 28f, 13f, false);

        Button close = CreateButton(w, "LP_Close", "ESC", 14f, ProjectUUIPalette.ButtonNormal, ProjectUUIPalette.TextPrimary, out _, out _);
        Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-Pad, -15f), new Vector2(76f, 36f));

        Image divider = CreateImage(w, "LP_Divider", null, Faint);
        Place(divider.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -64f), new Vector2(WindowWidth - Pad * 2f, 1f));

        // 동물 카드
        RectTransform cards = CreateRect(w, "LP_Cards");
        TopLeft(cards, new Vector2(Pad, -82f), new Vector2(WindowWidth - Pad * 2f, CardSize.y));
        GridLayoutGroup grid = cards.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = CardSize;
        grid.spacing = new Vector2(16f, 16f);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        AnimalCardUI cardTemplate = CreateCard(cards, iconSet);

        // 아래쪽
        TMP_Text message = CreateText(w, "LP_Message", string.Empty, 15f, FontStyles.Bold, TextAlignmentOptions.MidlineRight, ProjectUUIPalette.Teal);
        Place(message.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-Pad, 100f), new Vector2(520f, 26f));
        message.gameObject.SetActive(false);

        RectTransform feedParent = CreateRect(w, "LP_Feed");
        Place(feedParent, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(Pad, 50f), new Vector2(360f, 30f));
        HorizontalLayoutGroup feedLayout = feedParent.gameObject.AddComponent<HorizontalLayoutGroup>();
        feedLayout.childAlignment = TextAnchor.MiddleLeft;
        ConfigureLayout(feedLayout, true);
        CookingChipUI feedChip = CreateChip(feedParent, "LP_FeedChip", 30f, 14f, false);

        TMP_Text hint = CreateText(w, "LP_Hint", "FEED ONCE A DAY", 11.5f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, ProjectUUIPalette.TextSecondary);
        hint.characterSpacing = 1f;
        Place(hint.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(Pad, 18f), new Vector2(560f, 22f));

        Button collectAll = CreateButton(w, "LP_TakeAll", "TAKE ALL", 16f, ProjectUUIPalette.Accent, ProjectUUIPalette.TextDark, out _, out TMP_Text collectAllLabel);
        collectAllLabel.characterSpacing = 1.5f;
        Place((RectTransform)collectAll.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-Pad, 26f), new Vector2(220f, 52f));
        Button feedAll = CreateButton(w, "LP_FeedAll", "FEED ALL", 16f, ProjectUUIPalette.Teal, ProjectUUIPalette.TextDark, out _, out TMP_Text feedAllLabel);
        feedAllLabel.characterSpacing = 1.5f;
        Place((RectTransform)feedAll.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-(Pad + 232f), 26f), new Vector2(220f, 52f));

        panel.gameObject.SetActive(false);

        AnimalPenPopupUI ui = root.gameObject.AddComponent<AnimalPenPopupUI>();
        SerializedObject so = new SerializedObject(ui);
        so.FindProperty("panelRoot").objectReferenceValue = panel.gameObject;
        so.FindProperty("titleText").objectReferenceValue = title;
        so.FindProperty("titleIcon").objectReferenceValue = titleIcon;
        so.FindProperty("infoChip").objectReferenceValue = infoChip;
        so.FindProperty("closeButton").objectReferenceValue = close;
        so.FindProperty("iconSet").objectReferenceValue = iconSet;
        so.FindProperty("cardRoot").objectReferenceValue = cards;
        so.FindProperty("cardTemplate").objectReferenceValue = cardTemplate;
        so.FindProperty("feedChip").objectReferenceValue = feedChip;
        so.FindProperty("feedAllButton").objectReferenceValue = feedAll;
        so.FindProperty("feedAllLabel").objectReferenceValue = feedAllLabel;
        so.FindProperty("collectAllButton").objectReferenceValue = collectAll;
        so.FindProperty("collectAllLabel").objectReferenceValue = collectAllLabel;
        so.FindProperty("hintText").objectReferenceValue = hint;
        so.FindProperty("messageText").objectReferenceValue = message;
        so.ApplyModifiedPropertiesWithoutUndo();
        return ui;
    }

    private static AnimalCardUI CreateCard(RectTransform parent, FoodEffectIconSet iconSet)
    {
        Image background = CreateImage(parent, "LP_CardTemplate", UISpriteFactory.Panel, ProjectUUIPalette.PanelMid);
        background.type = Image.Type.Sliced;
        RectTransform card = background.rectTransform;
        card.sizeDelta = CardSize;

        Image outline = CreateImage(card, "LP_Outline", UISpriteFactory.PanelOutline, Faint);
        outline.type = Image.Type.Sliced;
        Stretch(outline.rectTransform);

        // 동물 카드 내용
        RectTransform content = CreateRect(card, "LP_Content");
        Stretch(content);

        Image iconBack = CreateImage(content, "LP_IconBack", UISpriteFactory.CircleSprite, new Color(1f, 1f, 1f, 0.05f));
        Place(iconBack.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(124f, 124f));
        Image icon = CreateImage(iconBack.rectTransform, "LP_Icon", null, Color.white);
        icon.preserveAspect = true;
        Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(112f, 112f));

        Button release = CreateButton(content, "LP_Release", "RELEASE", 10.5f, new Color(1f, 1f, 1f, 0.06f), ProjectUUIPalette.TextSecondary, out _, out TMP_Text releaseLabel);
        releaseLabel.characterSpacing = 1f;
        Place((RectTransform)release.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-10f, -10f), new Vector2(76f, 24f));

        TMP_Text name = CreateText(content, "LP_Name", "HEN 1", 20f, FontStyles.Bold, TextAlignmentOptions.Center, ProjectUUIPalette.TextPrimary);
        name.characterSpacing = 1.5f;
        Place(name.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -144f), new Vector2(270f, 30f));

        TMP_Text mood = CreateText(content, "LP_Mood", "CONTENT  60", 12.5f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, ProjectUUIPalette.Accent);
        mood.characterSpacing = 1.5f;
        Place(mood.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(12f, -180f), new Vector2(232f, 20f));
        Image heart = CreateImage(content, "LP_MoodIcon", UISpriteFactory.Icon("Health"), ProjectUUIPalette.Health);
        heart.preserveAspect = true;
        Place(heart.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-120f, -180f), new Vector2(18f, 18f));

        Image moodBack = CreateImage(content, "LP_MoodBar", UISpriteFactory.Pill, ProjectUUIPalette.BarBackground);
        moodBack.type = Image.Type.Sliced;
        moodBack.pixelsPerUnitMultiplier = 3f;
        Place(moodBack.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -204f), new Vector2(256f, 12f));
        Sprite barFill = AssetDatabase.LoadAssetAtPath<Sprite>($"{UISpriteFactory.SpriteFolder}/UI_BarFill.png");
        Image moodFill = CreateImage(moodBack.rectTransform, "LP_Fill", barFill, ProjectUUIPalette.Accent);
        moodFill.type = Image.Type.Filled;
        moodFill.fillMethod = Image.FillMethod.Horizontal;
        moodFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        moodFill.fillAmount = 0.6f;
        SetOffsets(moodFill.rectTransform, Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f));

        RectTransform status = CreateRect(content, "LP_Status");
        Place(status, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -230f), new Vector2(270f, 62f));
        VerticalLayoutGroup statusLayout = status.gameObject.AddComponent<VerticalLayoutGroup>();
        statusLayout.spacing = 6f;
        statusLayout.childAlignment = TextAnchor.UpperCenter;
        statusLayout.childControlWidth = false;
        statusLayout.childControlHeight = false;
        statusLayout.childForceExpandWidth = false;
        statusLayout.childForceExpandHeight = false;
        CookingChipUI feedChip = CreateChip(status, "LP_FeedState", 26f, 12f, true);
        CookingChipUI productChip = CreateChip(status, "LP_ProductState", 26f, 12f, true);

        Button feed = CreateButton(content, "LP_Feed", "FEED", 14f, ProjectUUIPalette.Teal, ProjectUUIPalette.TextDark, out _, out TMP_Text feedLabel);
        Place((RectTransform)feed.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-66f, 68f), new Vector2(124f, 40f));
        Button pet = CreateButton(content, "LP_Pet", "PET", 14f, ProjectUUIPalette.ButtonNormal, ProjectUUIPalette.TextPrimary, out _, out TMP_Text petLabel);
        Place((RectTransform)pet.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(66f, 68f), new Vector2(124f, 40f));
        Button take = CreateButton(content, "LP_Take", "TAKE", 15f, ProjectUUIPalette.Accent, ProjectUUIPalette.TextDark, out _, out TMP_Text takeLabel);
        takeLabel.characterSpacing = 1.5f;
        Place((RectTransform)take.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(256f, 44f));

        // 빈 자리
        RectTransform empty = CreateRect(card, "LP_Empty");
        Stretch(empty);
        Image ghost = CreateImage(empty, "LP_Ghost", UISpriteFactory.CircleRing, new Color(1f, 1f, 1f, 0.08f));
        Place(ghost.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(140f, 140f));
        TMP_Text plus = CreateText(ghost.rectTransform, "LP_Plus", "+", 56f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.18f));
        plus.overflowMode = TextOverflowModes.Overflow;
        Stretch(plus.rectTransform);
        TMP_Text emptyTitle = CreateText(empty, "LP_EmptyTitle", "EMPTY SPACE", 16f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.72f, 0.7f, 0.65f, 0.75f));
        emptyTitle.characterSpacing = 2f;
        Place(emptyTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -222f), new Vector2(270f, 26f));
        Button attract = CreateButton(empty, "LP_Attract", "ATTRACT", 15f, ProjectUUIPalette.ButtonNormal, ProjectUUIPalette.TextPrimary, out _, out TMP_Text attractLabel);
        attractLabel.richText = true;
        attractLabel.textWrappingMode = TextWrappingModes.Normal;
        attractLabel.lineSpacing = -8f;
        Place((RectTransform)attract.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(256f, 64f));

        AnimalCardUI ui = background.gameObject.AddComponent<AnimalCardUI>();
        SerializedObject so = new SerializedObject(ui);
        so.FindProperty("outline").objectReferenceValue = outline;
        so.FindProperty("contentRoot").objectReferenceValue = content.gameObject;
        so.FindProperty("emptyRoot").objectReferenceValue = empty.gameObject;
        so.FindProperty("icon").objectReferenceValue = icon;
        so.FindProperty("nameText").objectReferenceValue = name;
        so.FindProperty("moodText").objectReferenceValue = mood;
        so.FindProperty("moodFill").objectReferenceValue = moodFill;
        so.FindProperty("feedChip").objectReferenceValue = feedChip;
        so.FindProperty("productChip").objectReferenceValue = productChip;
        so.FindProperty("feedButton").objectReferenceValue = feed;
        so.FindProperty("feedLabel").objectReferenceValue = feedLabel;
        so.FindProperty("petButton").objectReferenceValue = pet;
        so.FindProperty("petLabel").objectReferenceValue = petLabel;
        so.FindProperty("collectButton").objectReferenceValue = take;
        so.FindProperty("collectLabel").objectReferenceValue = takeLabel;
        so.FindProperty("releaseButton").objectReferenceValue = release;
        so.FindProperty("releaseLabel").objectReferenceValue = releaseLabel;
        so.FindProperty("attractButton").objectReferenceValue = attract;
        so.FindProperty("attractLabel").objectReferenceValue = attractLabel;
        so.FindProperty("emptyTitle").objectReferenceValue = emptyTitle;
        so.ApplyModifiedPropertiesWithoutUndo();
        empty.gameObject.SetActive(false);
        background.gameObject.SetActive(false);
        return ui;
    }

    // ------------------------------------------------------------ 머리 위 상태

    private static string BuildStatusHUD(FoodEffectIconSet iconSet)
    {
        RectTransform prompt = FindRect(PromptName);

        if (prompt == null || prompt.parent == null)
        {
            return "[경고] 상호작용 안내 UI가 없어 동물 상태 표시를 만들지 못함";
        }

        Transform canvas = prompt.parent;
        RemoveExisting(canvas, StatusRootName);
        RectTransform root = CreateRect(canvas, StatusRootName);
        Stretch(root);
        root.SetSiblingIndex(prompt.GetSiblingIndex());

        Image marker = CreateImage(root, "LP_MarkerTemplate", UISpriteFactory.CircleSprite, new Color(0.05f, 0.06f, 0.08f, 0.8f));
        Place(marker.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(38f, 38f));
        Image ring = CreateImage(marker.rectTransform, "LP_Ring", UISpriteFactory.CircleRing, new Color(1f, 1f, 1f, 0.25f));
        Stretch(ring.rectTransform);
        Image icon = CreateImage(marker.rectTransform, "LP_Icon", null, Color.white);
        icon.preserveAspect = true;
        Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(26f, 26f));
        marker.gameObject.SetActive(false);

        AnimalStatusHUD hud = root.gameObject.AddComponent<AnimalStatusHUD>();
        SerializedObject so = new SerializedObject(hud);
        so.FindProperty("gameUIManager").objectReferenceValue = Object.FindFirstObjectByType<GameUIManager>(FindObjectsInactive.Include);
        PlayerInventory player = Object.FindFirstObjectByType<PlayerInventory>(FindObjectsInactive.Include);
        so.FindProperty("player").objectReferenceValue = player != null ? player.transform : null;
        so.FindProperty("markerTemplate").objectReferenceValue = marker.rectTransform;
        so.FindProperty("hungrySprite").objectReferenceValue = iconSet != null ? iconSet.Get(new FoodEffectEntry { Kind = FoodEffectKind.Hunger }) : UISpriteFactory.Icon("Hunger");
        so.ApplyModifiedPropertiesWithoutUndo();
        return "동물 머리 위 상태 표시 생성";
    }
}
