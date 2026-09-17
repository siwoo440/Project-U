using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 85일차: 요리 창(PopupLayer), 모닥불 진행 고리(HUD), 음식 효과 알약(생존 게이지 아래)을 만든다.
// 실행할 때마다 같은 이름의 오브젝트를 지우고 새로 만든다.
public static class CookingPopupUIBuilder
{
    public const string PopupRootName = "LP_CookingPopup";
    public const string ProgressRootName = "LP_CookingProgressHUD";
    public const string BuffRootName = "LP_FoodBuffHUD";
    public const int BuffChipCount = 4;
    public const int SlotViewCount = 3;

    private const string PopupLayerName = "PopupLayer";
    private const string PromptName = "--- InteractionPrompt ---";
    private const string SurvivalPanelName = "LP_SurvivalPanel";
    private const string HealthBarName = "--- HealthBarRoot ---";

    private const float WindowWidth = 1040f;
    private const float WindowHeight = 760f;
    private const float Pad = 24f;
    private const float RowHeight = 48f;

    private static Color Faint => new Color(1f, 1f, 1f, 0.08f);

    public static string Build(FoodEffectIconSet iconSet, out CookingPopupUI popup)
    {
        popup = null;

        if (UISpriteFactory.Panel == null || UISpriteFactory.Pill == null || UISpriteFactory.CircleRing == null)
        {
            UISpriteFactory.GenerateAll();
        }

        RectTransform popupLayer = FindRect(PopupLayerName);

        if (popupLayer == null)
        {
            return "[경고] PopupLayer를 찾지 못해 요리 창을 만들지 못했습니다.";
        }

        popup = BuildPopup(popupLayer, iconSet);
        string progress = BuildProgressHUD(iconSet);
        string buffs = BuildBuffHUD(iconSet);
        return $"요리 창 생성 (PopupLayer) · {progress} · {buffs}";
    }

    // ------------------------------------------------------------ 요리 창

    private static CookingPopupUI BuildPopup(RectTransform popupLayer, FoodEffectIconSet iconSet)
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
        Image flame = CreateImage(w, "LP_TitleIcon", UISpriteFactory.Icon("Flame"), ProjectUUIPalette.Accent);
        flame.preserveAspect = true;
        TopLeft(flame.rectTransform, new Vector2(Pad, -18f), new Vector2(30f, 30f));
        TMP_Text title = CreateText(w, "LP_Title", "CAMPFIRE COOKING", 24f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, ProjectUUIPalette.TextPrimary);
        title.characterSpacing = 2f;
        TopLeft(title.rectTransform, new Vector2(Pad + 42f, -14f), new Vector2(420f, 38f));

        RectTransform stationChipParent = CreateRect(w, "LP_StationInfo");
        Place(stationChipParent, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-(Pad + 96f), -19f), new Vector2(360f, 28f));
        HorizontalLayoutGroup stationLayout = stationChipParent.gameObject.AddComponent<HorizontalLayoutGroup>();
        stationLayout.childAlignment = TextAnchor.MiddleRight;
        ConfigureLayout(stationLayout, true);
        CookingChipUI stationChip = CreateChip(stationChipParent, "LP_StationChip", 28f, 13f, false);

        Button close = CreateButton(w, "LP_Close", "ESC", 14f, ProjectUUIPalette.ButtonNormal, ProjectUUIPalette.TextPrimary, out _, out _);
        Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-Pad, -15f), new Vector2(76f, 36f));

        Image divider = CreateImage(w, "LP_Divider", null, Faint);
        Place(divider.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -64f), new Vector2(WindowWidth - Pad * 2f, 1f));

        // 왼쪽 요리법 목록
        TMP_Text recipesLabel = CreateLabel(w, "LP_RecipesLabel", "RECIPES");
        TopLeft(recipesLabel.rectTransform, new Vector2(Pad, -80f), new Vector2(380f, 20f));

        RectTransform list = CreateRect(w, "LP_RecipeList");
        TopLeft(list, new Vector2(Pad, -106f), new Vector2(380f, 440f));
        VerticalLayoutGroup listLayout = list.gameObject.AddComponent<VerticalLayoutGroup>();
        listLayout.spacing = 4f;
        listLayout.childAlignment = TextAnchor.UpperLeft;
        listLayout.childControlWidth = true;
        listLayout.childControlHeight = false;
        listLayout.childForceExpandWidth = true;
        listLayout.childForceExpandHeight = false;
        CookingRecipeRowUI rowTemplate = CreateRecipeRow(list);

        // 오른쪽 상세
        Image detail = CreateImage(w, "LP_Detail", UISpriteFactory.Panel, ProjectUUIPalette.PanelMid);
        detail.type = Image.Type.Sliced;
        TopLeft(detail.rectTransform, new Vector2(Pad + 404f, -80f), new Vector2(WindowWidth - Pad * 2f - 404f, 466f));
        RectTransform d = detail.rectTransform;

        Image iconBack = CreateImage(d, "LP_IconBack", UISpriteFactory.Slot, new Color(0.2f, 0.23f, 0.28f, 1f));
        iconBack.type = Image.Type.Sliced;
        TopLeft(iconBack.rectTransform, new Vector2(20f, -20f), new Vector2(104f, 104f));
        Image detailIcon = CreateImage(iconBack.rectTransform, "LP_Icon", null, Color.white);
        detailIcon.preserveAspect = true;
        Place(detailIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(88f, 88f));

        TMP_Text detailName = CreateText(d, "LP_Name", "DISH", 26f, FontStyles.Bold, TextAlignmentOptions.TopLeft, ProjectUUIPalette.TextPrimary);
        TopLeft(detailName.rectTransform, new Vector2(142f, -20f), new Vector2(420f, 34f));
        TMP_Text detailInfo = CreateText(d, "LP_Info", "Description", 13f, FontStyles.Normal, TextAlignmentOptions.TopLeft, ProjectUUIPalette.TextSecondary);
        detailInfo.textWrappingMode = TextWrappingModes.Normal;
        detailInfo.overflowMode = TextOverflowModes.Ellipsis;
        TopLeft(detailInfo.rectTransform, new Vector2(142f, -58f), new Vector2(420f, 36f));

        RectTransform effects = CreateRect(d, "LP_Effects");
        TopLeft(effects, new Vector2(142f, -98f), new Vector2(430f, 26f));
        HorizontalLayoutGroup effectLayout = effects.gameObject.AddComponent<HorizontalLayoutGroup>();
        effectLayout.spacing = 6f;
        effectLayout.childAlignment = TextAnchor.MiddleLeft;
        ConfigureLayout(effectLayout, true);
        CookingChipUI effectTemplate = CreateChip(effects, "LP_EffectTemplate", 26f, 12f, false);

        TMP_Text ingredientsLabel = CreateLabel(d, "LP_IngredientsLabel", "INGREDIENTS");
        TopLeft(ingredientsLabel.rectTransform, new Vector2(20f, -144f), new Vector2(300f, 20f));

        RectTransform ingredients = CreateRect(d, "LP_Ingredients");
        TopLeft(ingredients, new Vector2(20f, -168f), new Vector2(d.sizeDelta.x - 40f, 170f));
        VerticalLayoutGroup ingredientLayout = ingredients.gameObject.AddComponent<VerticalLayoutGroup>();
        ingredientLayout.spacing = 4f;
        ingredientLayout.childControlWidth = true;
        ingredientLayout.childControlHeight = false;
        ingredientLayout.childForceExpandWidth = true;
        ingredientLayout.childForceExpandHeight = false;
        CookingIngredientRowUI ingredientTemplate = CreateIngredientRow(ingredients);

        RectTransform meta = CreateRect(d, "LP_Meta");
        TopLeft(meta, new Vector2(20f, -338f), new Vector2(400f, 28f));
        HorizontalLayoutGroup metaLayout = meta.gameObject.AddComponent<HorizontalLayoutGroup>();
        metaLayout.spacing = 8f;
        metaLayout.childAlignment = TextAnchor.MiddleLeft;
        ConfigureLayout(metaLayout, true);
        CookingChipUI timeChip = CreateChip(meta, "LP_TimeChip", 28f, 13f, false);
        CookingChipUI requiredChip = CreateChip(meta, "LP_StationChip", 28f, 13f, false);

        // 수량과 조리 버튼
        TMP_Text qtyLabel = CreateLabel(d, "LP_QuantityLabel", "QUANTITY");
        Place(qtyLabel.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(20f, 76f), new Vector2(160f, 18f));
        Button minus = CreateButton(d, "LP_Minus", "-", 22f, ProjectUUIPalette.ButtonNormal, ProjectUUIPalette.TextPrimary, out _, out _);
        Place((RectTransform)minus.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(20f, 20f), new Vector2(44f, 44f));
        TMP_Text quantity = CreateText(d, "LP_Quantity", "x1", 22f, FontStyles.Bold, TextAlignmentOptions.Center, ProjectUUIPalette.TextPrimary);
        Place(quantity.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(70f, 20f), new Vector2(64f, 44f));
        Button plus = CreateButton(d, "LP_Plus", "+", 22f, ProjectUUIPalette.ButtonNormal, ProjectUUIPalette.TextPrimary, out _, out _);
        Place((RectTransform)plus.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(140f, 20f), new Vector2(44f, 44f));

        Button cook = CreateButton(d, "LP_Cook", "COOK", 18f, ProjectUUIPalette.Accent, ProjectUUIPalette.TextDark, out Image cookImage, out TMP_Text cookLabel);
        Place((RectTransform)cook.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 20f), new Vector2(280f, 52f));
        cookLabel.characterSpacing = 1.5f;

        // 아래 조리 칸
        TMP_Text slotHeader = CreateLabel(w, "LP_SlotHeader", "FIRE SLOTS");
        TopLeft(slotHeader.rectTransform, new Vector2(Pad, -560f), new Vector2(560f, 20f));
        TMP_Text message = CreateText(w, "LP_Message", string.Empty, 15f, FontStyles.Bold, TextAlignmentOptions.MidlineRight, ProjectUUIPalette.Teal);
        Place(message.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-Pad, -556f), new Vector2(440f, 26f));
        message.gameObject.SetActive(false);

        float slotWidth = (WindowWidth - Pad * 2f - 12f * (SlotViewCount - 1)) / SlotViewCount;
        CookingSlotView[] slots = new CookingSlotView[SlotViewCount];

        for (int index = 0; index < SlotViewCount; index++)
        {
            slots[index] = CreateSlotView(w, index, new Vector2(Pad + index * (slotWidth + 12f), -586f), new Vector2(slotWidth, 150f));
        }

        panel.gameObject.SetActive(false);

        CookingPopupUI ui = root.gameObject.AddComponent<CookingPopupUI>();
        SerializedObject so = new SerializedObject(ui);
        so.FindProperty("panelRoot").objectReferenceValue = panel.gameObject;
        so.FindProperty("titleText").objectReferenceValue = title;
        so.FindProperty("stationInfoChip").objectReferenceValue = stationChip;
        so.FindProperty("closeButton").objectReferenceValue = close;
        so.FindProperty("iconSet").objectReferenceValue = iconSet;
        so.FindProperty("recipeListRoot").objectReferenceValue = list;
        so.FindProperty("recipeRowTemplate").objectReferenceValue = rowTemplate;
        so.FindProperty("detailIcon").objectReferenceValue = detailIcon;
        so.FindProperty("detailNameText").objectReferenceValue = detailName;
        so.FindProperty("detailInfoText").objectReferenceValue = detailInfo;
        so.FindProperty("effectListRoot").objectReferenceValue = effects;
        so.FindProperty("effectChipTemplate").objectReferenceValue = effectTemplate;
        so.FindProperty("ingredientListRoot").objectReferenceValue = ingredients;
        so.FindProperty("ingredientRowTemplate").objectReferenceValue = ingredientTemplate;
        so.FindProperty("timeChip").objectReferenceValue = timeChip;
        so.FindProperty("requiredStationChip").objectReferenceValue = requiredChip;
        so.FindProperty("quantityText").objectReferenceValue = quantity;
        so.FindProperty("minusButton").objectReferenceValue = minus;
        so.FindProperty("plusButton").objectReferenceValue = plus;
        so.FindProperty("cookButton").objectReferenceValue = cook;
        so.FindProperty("cookButtonImage").objectReferenceValue = cookImage;
        so.FindProperty("cookButtonLabel").objectReferenceValue = cookLabel;
        so.FindProperty("slotHeaderText").objectReferenceValue = slotHeader;
        so.FindProperty("messageText").objectReferenceValue = message;
        SerializedProperty slotProperty = so.FindProperty("slotViews");
        slotProperty.arraySize = slots.Length;

        for (int index = 0; index < slots.Length; index++)
        {
            slotProperty.GetArrayElementAtIndex(index).objectReferenceValue = slots[index];
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        return ui;
    }

    private static CookingRecipeRowUI CreateRecipeRow(RectTransform parent)
    {
        Image background = CreateImage(parent, "LP_RecipeTemplate", UISpriteFactory.Panel, new Color(1f, 1f, 1f, 0.035f));
        background.type = Image.Type.Sliced;
        background.pixelsPerUnitMultiplier = 1.6f;
        background.raycastTarget = true;
        RectTransform row = background.rectTransform;
        row.sizeDelta = new Vector2(380f, RowHeight);
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
        Place(accent.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(3f, 0f), new Vector2(4f, 26f));

        Image iconBack = CreateImage(row, "LP_IconBack", UISpriteFactory.Slot, new Color(1f, 1f, 1f, 0.06f));
        iconBack.type = Image.Type.Sliced;
        iconBack.pixelsPerUnitMultiplier = 2f;
        Place(iconBack.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(38f, 38f));
        Image icon = CreateImage(iconBack.rectTransform, "LP_Icon", null, Color.white);
        icon.preserveAspect = true;
        Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(34f, 34f));

        TMP_Text name = CreateText(row, "LP_Name", "DISH", 15f, FontStyles.Bold, TextAlignmentOptions.BottomLeft, ProjectUUIPalette.TextPrimary);
        SetOffsets(name.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(60f, 0f), new Vector2(-60f, -4f));
        TMP_Text status = CreateText(row, "LP_Status", "READY TO COOK", 10.5f, FontStyles.Bold, TextAlignmentOptions.TopLeft, ProjectUUIPalette.Teal);
        status.characterSpacing = 1.5f;
        SetOffsets(status.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(60f, 4f), new Vector2(-60f, -1f));
        TMP_Text time = CreateText(row, "LP_Time", "6s", 13f, FontStyles.Bold, TextAlignmentOptions.MidlineRight, ProjectUUIPalette.TextSecondary);
        Place(time.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-14f, 0f), new Vector2(50f, 24f));

        CookingRecipeRowUI ui = background.gameObject.AddComponent<CookingRecipeRowUI>();
        SerializedObject so = new SerializedObject(ui);
        so.FindProperty("button").objectReferenceValue = button;
        so.FindProperty("background").objectReferenceValue = background;
        so.FindProperty("selectionOutline").objectReferenceValue = outline;
        so.FindProperty("iconBackground").objectReferenceValue = iconBack;
        so.FindProperty("icon").objectReferenceValue = icon;
        so.FindProperty("nameText").objectReferenceValue = name;
        so.FindProperty("statusText").objectReferenceValue = status;
        so.FindProperty("timeText").objectReferenceValue = time;
        so.ApplyModifiedPropertiesWithoutUndo();
        background.gameObject.SetActive(false);
        return ui;
    }

    private static CookingIngredientRowUI CreateIngredientRow(RectTransform parent)
    {
        Image background = CreateImage(parent, "LP_IngredientTemplate", UISpriteFactory.Panel, new Color(1f, 1f, 1f, 0.04f));
        background.type = Image.Type.Sliced;
        background.pixelsPerUnitMultiplier = 2f;
        RectTransform row = background.rectTransform;
        row.sizeDelta = new Vector2(500f, 38f);

        Image bar = CreateImage(row, "LP_StateBar", UISpriteFactory.Pill, ProjectUUIPalette.Teal);
        bar.type = Image.Type.Sliced;
        bar.pixelsPerUnitMultiplier = 6f;
        Place(bar.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(4f, 0f), new Vector2(4f, 24f));

        Image icon = CreateImage(row, "LP_Icon", null, Color.white);
        icon.preserveAspect = true;
        Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(30f, 30f));

        TMP_Text name = CreateText(row, "LP_Name", "ITEM", 14f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, ProjectUUIPalette.TextPrimary);
        name.richText = true;
        SetOffsets(name.rectTransform, Vector2.zero, Vector2.one, new Vector2(56f, 0f), new Vector2(-110f, 0f));
        TMP_Text count = CreateText(row, "LP_Count", "0 / 1", 15f, FontStyles.Bold, TextAlignmentOptions.MidlineRight, ProjectUUIPalette.Teal);
        Place(count.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-14f, 0f), new Vector2(96f, 30f));

        CookingIngredientRowUI ui = background.gameObject.AddComponent<CookingIngredientRowUI>();
        SerializedObject so = new SerializedObject(ui);
        so.FindProperty("icon").objectReferenceValue = icon;
        so.FindProperty("nameText").objectReferenceValue = name;
        so.FindProperty("countText").objectReferenceValue = count;
        so.FindProperty("stateBar").objectReferenceValue = bar;
        so.ApplyModifiedPropertiesWithoutUndo();
        background.gameObject.SetActive(false);
        return ui;
    }

    private static CookingSlotView CreateSlotView(RectTransform parent, int index, Vector2 topLeft, Vector2 size)
    {
        Image background = CreateImage(parent, $"LP_Slot_{index}", UISpriteFactory.Panel, ProjectUUIPalette.PanelMid);
        background.type = Image.Type.Sliced;
        TopLeft(background.rectTransform, topLeft, size);
        RectTransform card = background.rectTransform;

        Image outline = CreateImage(card, "LP_Outline", UISpriteFactory.PanelOutline, Faint);
        outline.type = Image.Type.Sliced;
        Stretch(outline.rectTransform);

        // 빈 칸
        RectTransform empty = CreateRect(card, "LP_Empty");
        Stretch(empty);
        TMP_Text emptyTitle = CreateText(empty, "LP_EmptyTitle", "EMPTY SLOT", 15f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.72f, 0.7f, 0.65f, 0.7f));
        emptyTitle.characterSpacing = 2f;
        SetOffsets(emptyTitle.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(10f, 0f), new Vector2(-10f, 26f));
        TMP_Text emptyHint = CreateText(empty, "LP_EmptyHint", "PICK A RECIPE AND PRESS COOK", 11f, FontStyles.Normal, TextAlignmentOptions.Center, new Color(0.72f, 0.7f, 0.65f, 0.5f));
        SetOffsets(emptyHint.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(10f, -22f), new Vector2(-10f, 0f));

        // 내용
        RectTransform content = CreateRect(card, "LP_Content");
        Stretch(content);
        Image iconBack = CreateImage(content, "LP_IconBack", UISpriteFactory.Slot, new Color(1f, 1f, 1f, 0.06f));
        iconBack.type = Image.Type.Sliced;
        TopLeft(iconBack.rectTransform, new Vector2(14f, -16f), new Vector2(64f, 64f));
        Image icon = CreateImage(iconBack.rectTransform, "LP_Icon", null, Color.white);
        icon.preserveAspect = true;
        Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(56f, 56f));

        TMP_Text name = CreateText(content, "LP_Name", "DISH x1", 15f, FontStyles.Bold, TextAlignmentOptions.TopLeft, ProjectUUIPalette.TextPrimary);
        SetOffsets(name.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(92f, -42f), new Vector2(-12f, -18f));
        TMP_Text status = CreateText(content, "LP_Status", "COOKING 0:04", 12f, FontStyles.Bold, TextAlignmentOptions.TopLeft, ProjectUUIPalette.Accent);
        status.characterSpacing = 1.5f;
        SetOffsets(status.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(92f, -64f), new Vector2(-12f, -44f));

        Image progressBack = CreateImage(content, "LP_Progress", UISpriteFactory.Pill, ProjectUUIPalette.BarBackground);
        progressBack.type = Image.Type.Sliced;
        progressBack.pixelsPerUnitMultiplier = 3f;
        SetOffsets(progressBack.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(14f, 22f), new Vector2(-14f, 34f));
        // Filled 방식은 Sprite가 없으면 채움 비율이 적용되지 않는다
        Sprite barFill = AssetDatabase.LoadAssetAtPath<Sprite>($"{UISpriteFactory.SpriteFolder}/UI_BarFill.png");
        Image progressFill = CreateImage(progressBack.rectTransform, "LP_Fill", barFill, ProjectUUIPalette.Accent);
        progressFill.type = Image.Type.Filled;
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        progressFill.fillAmount = 0.4f;
        SetOffsets(progressFill.rectTransform, Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f));

        Button take = CreateButton(content, "LP_Take", "TAKE", 15f, ProjectUUIPalette.Teal, ProjectUUIPalette.TextDark, out _, out TMP_Text takeLabel);
        takeLabel.characterSpacing = 2f;
        Place((RectTransform)take.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-14f, 14f), new Vector2(120f, 38f));

        CookingSlotView view = background.gameObject.AddComponent<CookingSlotView>();
        SerializedObject so = new SerializedObject(view);
        so.FindProperty("outline").objectReferenceValue = outline;
        so.FindProperty("icon").objectReferenceValue = icon;
        so.FindProperty("nameText").objectReferenceValue = name;
        so.FindProperty("statusText").objectReferenceValue = status;
        so.FindProperty("progressFill").objectReferenceValue = progressFill;
        so.FindProperty("progressRoot").objectReferenceValue = progressBack.gameObject;
        so.FindProperty("takeButton").objectReferenceValue = take;
        so.FindProperty("emptyRoot").objectReferenceValue = empty.gameObject;
        so.FindProperty("contentRoot").objectReferenceValue = content.gameObject;
        so.ApplyModifiedPropertiesWithoutUndo();
        content.gameObject.SetActive(false);
        return view;
    }

    // ------------------------------------------------------------ HUD

    private static string BuildProgressHUD(FoodEffectIconSet iconSet)
    {
        RectTransform prompt = FindRect(PromptName);

        if (prompt == null || prompt.parent == null)
        {
            return "[경고] 상호작용 안내 UI가 없어 진행 고리를 만들지 못함";
        }

        Transform canvas = prompt.parent;
        RemoveExisting(canvas, ProgressRootName);
        RectTransform root = CreateRect(canvas, ProgressRootName);
        Stretch(root);
        root.SetSiblingIndex(prompt.GetSiblingIndex());

        RectTransform marker = CreateRect(root, "LP_Marker");
        Place(marker, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64f, 64f));

        Image back = CreateImage(marker, "LP_Back", UISpriteFactory.CircleSprite, new Color(0.05f, 0.06f, 0.08f, 0.75f));
        Place(back.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(58f, 58f));
        Image track = CreateImage(marker, "LP_Track", UISpriteFactory.CircleRing, new Color(1f, 1f, 1f, 0.15f));
        Stretch(track.rectTransform);
        Image ring = CreateImage(marker, "LP_Ring", UISpriteFactory.CircleRing, ProjectUUIPalette.Accent);
        ring.type = Image.Type.Filled;
        ring.fillMethod = Image.FillMethod.Radial360;
        ring.fillOrigin = (int)Image.Origin360.Top;
        ring.fillClockwise = true;
        ring.fillAmount = 0.6f;
        Stretch(ring.rectTransform);
        Image center = CreateImage(marker, "LP_Icon", iconSet != null ? iconSet.Flame : null, ProjectUUIPalette.Accent);
        center.preserveAspect = true;
        Place(center.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(28f, 28f));

        Image labelBack = CreateImage(marker, "LP_LabelBack", UISpriteFactory.Pill, new Color(0.05f, 0.06f, 0.08f, 0.8f));
        labelBack.type = Image.Type.Sliced;
        Place(labelBack.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(92f, 24f));
        TMP_Text label = CreateText(labelBack.rectTransform, "LP_Label", "0:04", 13f, FontStyles.Bold, TextAlignmentOptions.Center, ProjectUUIPalette.TextPrimary);
        Stretch(label.rectTransform);

        CookingProgressHUD hud = root.gameObject.AddComponent<CookingProgressHUD>();
        SerializedObject so = new SerializedObject(hud);
        so.FindProperty("gameUIManager").objectReferenceValue = Object.FindFirstObjectByType<GameUIManager>(FindObjectsInactive.Include);
        PlayerInventory player = Object.FindFirstObjectByType<PlayerInventory>(FindObjectsInactive.Include);
        so.FindProperty("player").objectReferenceValue = player != null ? player.transform : null;
        so.FindProperty("markerRoot").objectReferenceValue = marker;
        so.FindProperty("ringFill").objectReferenceValue = ring;
        so.FindProperty("centerIcon").objectReferenceValue = center;
        so.FindProperty("label").objectReferenceValue = label;
        so.FindProperty("cookingSprite").objectReferenceValue = iconSet != null ? iconSet.Flame : null;
        so.FindProperty("readySprite").objectReferenceValue = UISpriteFactory.Icon("Check");
        so.ApplyModifiedPropertiesWithoutUndo();
        marker.gameObject.SetActive(false);
        return "모닥불 진행 고리 생성";
    }

    private static string BuildBuffHUD(FoodEffectIconSet iconSet)
    {
        RectTransform healthBar = FindRect(HealthBarName);

        if (healthBar == null || healthBar.parent == null)
        {
            return "[경고] 생존 게이지가 없어 음식 효과 표시를 만들지 못함";
        }

        Transform parent = healthBar.parent;
        RemoveExisting(parent, BuffRootName);
        RectTransform survivalPanel = FindChildRect(parent, SurvivalPanelName);
        float top = survivalPanel != null
            ? survivalPanel.anchoredPosition.y - survivalPanel.sizeDelta.y - 8f
            : -216f;
        float left = survivalPanel != null ? survivalPanel.anchoredPosition.x : 14f;

        RectTransform root = CreateRect(parent, BuffRootName);
        TopLeft(root, new Vector2(left, top), new Vector2(300f, BuffChipCount * 26f + (BuffChipCount - 1) * 4f));
        VerticalLayoutGroup layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 4f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        CookingChipUI[] chips = new CookingChipUI[BuffChipCount];

        for (int index = 0; index < BuffChipCount; index++)
        {
            chips[index] = CreateChip(root, $"LP_BuffChip_{index}", 26f, 13f, true);
            Image background = chips[index].GetComponent<Image>();
            background.color = new Color(0.05f, 0.06f, 0.08f, 0.62f);
            chips[index].gameObject.SetActive(false);
        }

        FoodBuffHUD hud = root.gameObject.AddComponent<FoodBuffHUD>();
        SerializedObject so = new SerializedObject(hud);
        SerializedProperty chipProperty = so.FindProperty("chips");
        chipProperty.arraySize = chips.Length;

        for (int index = 0; index < chips.Length; index++)
        {
            chipProperty.GetArrayElementAtIndex(index).objectReferenceValue = chips[index];
        }

        so.FindProperty("iconSet").objectReferenceValue = iconSet;
        FoodBuffController controller = Object.FindFirstObjectByType<FoodBuffController>(FindObjectsInactive.Include);
        so.FindProperty("controller").objectReferenceValue = controller;
        SerializedObject chipAlpha;

        foreach (CookingChipUI chip in chips)
        {
            chipAlpha = new SerializedObject(chip);
            chipAlpha.FindProperty("backgroundAlpha").floatValue = 0.28f;
            chipAlpha.ApplyModifiedPropertiesWithoutUndo();
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        return $"음식 효과 표시 {BuffChipCount}칸 (생존 게이지 아래 y {top:0})";
    }

    // ------------------------------------------------------------ 공통 부품

    private static CookingChipUI CreateChip(Transform parent, string name, float height, float fontSize, bool fitWidth)
    {
        Image background = CreateImage(parent, name, UISpriteFactory.Pill, new Color(1f, 1f, 1f, 0.16f));
        background.type = Image.Type.Sliced;
        background.pixelsPerUnitMultiplier = 32f / height;
        RectTransform chip = background.rectTransform;
        chip.sizeDelta = new Vector2(120f, height);
        HorizontalLayoutGroup layout = background.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(Mathf.RoundToInt(height * 0.35f), Mathf.RoundToInt(height * 0.45f), 0, 0);
        layout.spacing = 5f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        if (fitWidth)
        {
            ContentSizeFitter fitter = background.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
        }

        Image icon = CreateImage(chip, "LP_Icon", null, Color.white);
        icon.preserveAspect = true;
        LayoutElement iconLayout = icon.gameObject.AddComponent<LayoutElement>();
        iconLayout.preferredWidth = height * 0.58f;
        iconLayout.preferredHeight = height * 0.58f;

        TMP_Text label = CreateText(chip, "LP_Label", "EFFECT", fontSize, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, ProjectUUIPalette.TextPrimary);
        label.characterSpacing = 0.5f;
        label.overflowMode = TextOverflowModes.Overflow;

        CookingChipUI ui = background.gameObject.AddComponent<CookingChipUI>();
        SerializedObject so = new SerializedObject(ui);
        so.FindProperty("background").objectReferenceValue = background;
        so.FindProperty("icon").objectReferenceValue = icon;
        so.FindProperty("label").objectReferenceValue = label;
        so.ApplyModifiedPropertiesWithoutUndo();
        return ui;
    }

    private static void ConfigureLayout(HorizontalLayoutGroup layout, bool controlWidth)
    {
        layout.childControlWidth = controlWidth;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;
    }

    private static Button CreateButton(Transform parent, string name, string text, float fontSize, Color color, Color textColor, out Image image, out TMP_Text label)
    {
        image = CreateImage(parent, name, UISpriteFactory.Button, color);
        image.type = Image.Type.Sliced;
        image.raycastTarget = true;
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        SetTint(button, 1.1f);
        label = CreateText(image.rectTransform, "LP_Label", text, fontSize, FontStyles.Bold, TextAlignmentOptions.Center, textColor);
        label.overflowMode = TextOverflowModes.Overflow;
        Stretch(label.rectTransform);
        return button;
    }

    private static void SetTint(Button button, float highlight)
    {
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(highlight, highlight, highlight, 1f);
        colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(1f, 1f, 1f, 0.55f);
        colors.colorMultiplier = 1f;
        button.colors = colors;
        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;
    }

    private static TMP_Text CreateLabel(Transform parent, string name, string value)
    {
        TMP_Text label = CreateText(parent, name, value, 11.5f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, ProjectUUIPalette.TextSecondary);
        label.characterSpacing = 3f;
        return label;
    }

    private static RectTransform CreateRect(Transform parent, string name)
    {
        GameObject created = new GameObject(name, typeof(RectTransform));
        created.layer = parent.gameObject.layer;
        created.transform.SetParent(parent, false);
        Undo.RegisterCreatedObjectUndo(created, "Create Cooking UI");
        return (RectTransform)created.transform;
    }

    private static Image CreateImage(Transform parent, string name, Sprite sprite, Color color)
    {
        GameObject created = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        created.layer = parent.gameObject.layer;
        created.transform.SetParent(parent, false);
        Image image = created.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text CreateText(Transform parent, string name, string value, float size, FontStyles style, TextAlignmentOptions alignment, Color color)
    {
        GameObject created = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        created.layer = parent.gameObject.layer;
        created.transform.SetParent(parent, false);
        TMP_Text text = created.GetComponent<TMP_Text>();

        if (TMP_Settings.defaultFontAsset != null)
        {
            text.font = TMP_Settings.defaultFontAsset;
        }

        text.text = value;
        text.fontSize = size;
        text.enableAutoSizing = false;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void TopLeft(RectTransform rect, Vector2 position, Vector2 size)
    {
        Place(rect, new Vector2(0f, 1f), new Vector2(0f, 1f), position, size);
    }

    private static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void SetOffsets(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static void RemoveExisting(Transform parent, string name)
    {
        Transform existing = parent.Find(name);

        while (existing != null)
        {
            Undo.DestroyObjectImmediate(existing.gameObject);
            existing = parent.Find(name);
        }
    }

    private static RectTransform FindChildRect(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        return child as RectTransform;
    }

    public static RectTransform FindRect(string name)
    {
        Scene scene = SceneManager.GetActiveScene();

        foreach (GameObject sceneRoot in scene.GetRootGameObjects())
        {
            foreach (RectTransform rect in sceneRoot.GetComponentsInChildren<RectTransform>(true))
            {
                if (rect.name == name)
                {
                    return rect;
                }
            }
        }

        return null;
    }
}
