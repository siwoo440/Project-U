using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static UIBuildKit;

// 93일차: 마을 게시판 창(PopupLayer)과 진행 중 의뢰 표시(화면 왼쪽, 음식 효과 아래)를 코드로 만든다.
// 실행할 때마다 같은 이름의 오브젝트를 지우고 새로 만든다.
public static class NpcQuestPopupUIBuilder
{
    public const string PopupRootName = "LP_QuestBoard";
    public const string TrackerRootName = "LP_QuestTracker";
    private const string PopupLayerName = "PopupLayer";

    private const float WindowWidth = 1040f;
    private const float WindowHeight = 640f;
    private const float Pad = 24f;
    private const float ListWidth = 400f;
    private const float BodyTop = -80f;
    private const float BodyHeight = 480f;
    private const float RowHeight = 70f;
    private const float TrackerWidth = 300f;

    public static readonly string[] PopupFields =
    {
        "panelRoot", "titleText", "summaryText", "closeButton", "listRoot", "rowTemplate", "emptyText", "detailRoot", "portrait", "portraitFrame",
        "ownerText", "questTitleText", "requestText", "requirementRoot", "requirementTemplate", "rewardText", "deadlineText", "actionButton", "actionLabel", "hintText", "messageText"
    };

    public static readonly string[] TrackerFields = { "panel", "titleText", "bodyText", "noticeText" };

    // ------------------------------------------------------------ 게시판 창

    public static NpcQuestBoardPopup BuildBoardPopup(out string report)
    {
        EnsureSprites();
        RectTransform popupLayer = FindRect(PopupLayerName);

        if (popupLayer == null)
        {
            report = "✗ PopupLayer를 찾지 못해 게시판 창을 만들지 못했습니다.";
            return null;
        }

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
        Image titleIcon = CreateImage(w, "LP_TitleIcon", UISpriteFactory.Icon("Tag"), ProjectUUIPalette.Accent);
        titleIcon.preserveAspect = true;
        TopLeft(titleIcon.rectTransform, new Vector2(Pad, -14f), new Vector2(36f, 36f));
        TMP_Text title = CreateText(w, "LP_Title", "마을 게시판", 26f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, ProjectUUIPalette.TextPrimary);
        TopLeft(title.rectTransform, new Vector2(Pad + 46f, -12f), new Vector2(360f, 40f));
        TMP_Text summary = CreateText(w, "LP_Summary", "오늘의 의뢰 0 · 진행 중 0/3", 15f, FontStyles.Bold, TextAlignmentOptions.MidlineRight, ProjectUUIPalette.TextSecondary);
        Place(summary.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-(Pad + 92f), -18f), new Vector2(460f, 30f));
        Button close = CreateButton(w, "LP_Close", "ESC", 14f, ProjectUUIPalette.ButtonNormal, ProjectUUIPalette.TextPrimary, out _, out _);
        Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-Pad, -15f), new Vector2(76f, 36f));
        Image divider = CreateImage(w, "LP_Divider", null, Faint);
        Place(divider.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -64f), new Vector2(WindowWidth - Pad * 2f, 1f));

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
        list.sizeDelta = new Vector2(-6f, BodyHeight);
        VerticalLayoutGroup listLayout = list.gameObject.AddComponent<VerticalLayoutGroup>();
        listLayout.spacing = 6f;
        listLayout.childAlignment = TextAnchor.UpperLeft;
        listLayout.childControlWidth = true;
        listLayout.childControlHeight = false;
        listLayout.childForceExpandWidth = true;
        listLayout.childForceExpandHeight = false;
        ContentSizeFitter listFitter = list.gameObject.AddComponent<ContentSizeFitter>();
        listFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        listFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        NpcQuestRowUI rowTemplate = CreateRow(list);
        ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.content = list;
        scroll.viewport = viewport.rectTransform;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 28f;

        TMP_Text empty = CreateText(viewport.rectTransform, "LP_Empty", "오늘은 붙어 있는 의뢰가 없어요.", 15f, FontStyles.Bold, TextAlignmentOptions.Center, ProjectUUIPalette.TextSecondary);
        empty.textWrappingMode = TextWrappingModes.Normal;
        SetOffsets(empty.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(20f, -40f), new Vector2(-20f, 40f));
        empty.gameObject.SetActive(false);

        // 오른쪽 상세
        float detailWidth = WindowWidth - Pad * 2f - ListWidth - 20f;
        Image detail = CreateImage(w, "LP_Detail", UISpriteFactory.Panel, ProjectUUIPalette.PanelMid);
        detail.type = Image.Type.Sliced;
        TopLeft(detail.rectTransform, new Vector2(Pad + ListWidth + 20f, BodyTop), new Vector2(detailWidth, BodyHeight));
        RectTransform d = detail.rectTransform;

        Image frame = CreateImage(d, "LP_PortraitFrame", UISpriteFactory.Slot, Color.white);
        frame.type = Image.Type.Sliced;
        TopLeft(frame.rectTransform, new Vector2(20f, -20f), new Vector2(104f, 104f));
        Image portraitBack = CreateImage(frame.rectTransform, "LP_PortraitBack", UISpriteFactory.Slot, ProjectUUIPalette.PanelDark);
        portraitBack.type = Image.Type.Sliced;
        SetOffsets(portraitBack.rectTransform, Vector2.zero, Vector2.one, new Vector2(5f, 5f), new Vector2(-5f, -5f));
        Image portrait = CreateImage(portraitBack.rectTransform, "LP_Portrait", null, Color.white);
        portrait.preserveAspect = true;
        SetOffsets(portrait.rectTransform, Vector2.zero, Vector2.one, new Vector2(3f, 3f), new Vector2(-3f, -3f));

        float textLeft = 142f;
        float textWidth = detailWidth - textLeft - 20f;
        TMP_Text owner = CreateText(d, "LP_Owner", "이름", 18f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, ProjectUUIPalette.TextPrimary);
        owner.richText = true;
        TopLeft(owner.rectTransform, new Vector2(textLeft, -18f), new Vector2(textWidth, 28f));
        TMP_Text questTitle = CreateText(d, "LP_QuestTitle", "의뢰 제목", 22f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, ProjectUUIPalette.Accent);
        questTitle.richText = true;
        TopLeft(questTitle.rectTransform, new Vector2(textLeft, -48f), new Vector2(textWidth, 32f));
        TMP_Text request = CreateText(d, "LP_Request", "“의뢰 대사”", 15f, FontStyles.Normal, TextAlignmentOptions.TopLeft, ProjectUUIPalette.TextPrimary);
        request.textWrappingMode = TextWrappingModes.Normal;
        request.overflowMode = TextOverflowModes.Ellipsis;
        request.lineSpacing = 6f;
        TopLeft(request.rectTransform, new Vector2(textLeft, -86f), new Vector2(textWidth, 66f));

        TMP_Text needLabel = CreateLabel(d, "LP_NeedLabel", "필요한 물건");
        TopLeft(needLabel.rectTransform, new Vector2(20f, -170f), new Vector2(300f, 18f));
        RectTransform requirements = CreateRect(d, "LP_Requirements");
        TopLeft(requirements, new Vector2(20f, -194f), new Vector2(detailWidth - 40f, 62f));
        HorizontalLayoutGroup requirementLayout = requirements.gameObject.AddComponent<HorizontalLayoutGroup>();
        requirementLayout.spacing = 10f;
        requirementLayout.childAlignment = TextAnchor.MiddleLeft;
        requirementLayout.childControlWidth = false;
        requirementLayout.childControlHeight = false;
        requirementLayout.childForceExpandWidth = false;
        requirementLayout.childForceExpandHeight = false;
        NpcQuestItemUI requirementTemplate = CreateRequirement(requirements);

        TMP_Text rewardLabel = CreateLabel(d, "LP_RewardLabel", "보상");
        TopLeft(rewardLabel.rectTransform, new Vector2(20f, -276f), new Vector2(300f, 18f));
        TMP_Text reward = CreateText(d, "LP_Reward", "코인 0 · 호감도 +0", 18f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, ProjectUUIPalette.Accent);
        TopLeft(reward.rectTransform, new Vector2(20f, -298f), new Vector2(detailWidth - 40f, 30f));
        TMP_Text deadline = CreateText(d, "LP_Deadline", "받은 날부터 3일 안에 전달", 14f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, ProjectUUIPalette.TextSecondary);
        deadline.textWrappingMode = TextWrappingModes.Normal;
        TopLeft(deadline.rectTransform, new Vector2(20f, -340f), new Vector2(detailWidth - 40f, 44f));

        Button action = CreateButton(d, "LP_Action", "의뢰 받기", 17f, ProjectUUIPalette.Accent, ProjectUUIPalette.TextDark, out _, out TMP_Text actionLabel);
        actionLabel.textWrappingMode = TextWrappingModes.Normal;
        actionLabel.fontSizeMin = 11f;
        actionLabel.enableAutoSizing = true;
        actionLabel.fontSizeMax = 17f;
        Place((RectTransform)action.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 20f), new Vector2(280f, 52f));

        // 아래쪽
        TMP_Text hint = CreateText(w, "LP_Hint", "안내", 12.5f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, ProjectUUIPalette.TextSecondary);
        Place(hint.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(Pad, 22f), new Vector2(620f, 24f));
        TMP_Text message = CreateText(w, "LP_Message", string.Empty, 15f, FontStyles.Bold, TextAlignmentOptions.MidlineRight, ProjectUUIPalette.Teal);
        Place(message.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-Pad, 22f), new Vector2(360f, 26f));
        message.gameObject.SetActive(false);

        panel.gameObject.SetActive(false);

        NpcQuestBoardPopup popup = root.gameObject.AddComponent<NpcQuestBoardPopup>();
        SerializedObject so = new SerializedObject(popup);
        Set(so, "panelRoot", panel.gameObject);
        Set(so, "titleText", title);
        Set(so, "summaryText", summary);
        Set(so, "closeButton", close);
        Set(so, "listRoot", list);
        Set(so, "rowTemplate", rowTemplate);
        Set(so, "emptyText", empty);
        Set(so, "detailRoot", detail.gameObject);
        Set(so, "portrait", portrait);
        Set(so, "portraitFrame", frame);
        Set(so, "ownerText", owner);
        Set(so, "questTitleText", questTitle);
        Set(so, "requestText", request);
        Set(so, "requirementRoot", requirements);
        Set(so, "requirementTemplate", requirementTemplate);
        Set(so, "rewardText", reward);
        Set(so, "deadlineText", deadline);
        Set(so, "actionButton", action);
        Set(so, "actionLabel", actionLabel);
        Set(so, "hintText", hint);
        Set(so, "messageText", message);
        so.ApplyModifiedPropertiesWithoutUndo();
        report = "게시판 창 생성 (PopupLayer)";
        return popup;
    }

    private static NpcQuestRowUI CreateRow(RectTransform parent)
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

        Image faceBack = CreateImage(row, "LP_FaceBack", UISpriteFactory.Slot, new Color(1f, 1f, 1f, 0.06f));
        faceBack.type = Image.Type.Sliced;
        Place(faceBack.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(54f, 54f));
        Image face = CreateImage(faceBack.rectTransform, "LP_Face", null, Color.white);
        face.preserveAspect = true;
        SetOffsets(face.rectTransform, Vector2.zero, Vector2.one, new Vector2(3f, 3f), new Vector2(-3f, -3f));

        TMP_Text title = CreateText(row, "LP_Title", "의뢰", 16f, FontStyles.Bold, TextAlignmentOptions.BottomLeft, ProjectUUIPalette.TextPrimary);
        SetOffsets(title.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(76f, 1f), new Vector2(-104f, -8f));
        TMP_Text subtitle = CreateText(row, "LP_Subtitle", "의뢰인", 12.5f, FontStyles.Normal, TextAlignmentOptions.TopLeft, ProjectUUIPalette.TextSecondary);
        SetOffsets(subtitle.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(76f, 8f), new Vector2(-104f, -3f));

        Image tagBack = CreateImage(row, "LP_Tag", UISpriteFactory.Pill, ProjectUUIPalette.Accent);
        tagBack.type = Image.Type.Sliced;
        tagBack.pixelsPerUnitMultiplier = 1.4f;
        Place(tagBack.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-10f, 0f), new Vector2(84f, 26f));
        TMP_Text tag = CreateText(tagBack.rectTransform, "LP_TagLabel", "특별", 12f, FontStyles.Bold, TextAlignmentOptions.Center, ProjectUUIPalette.TextDark);
        Stretch(tag.rectTransform);

        NpcQuestRowUI ui = background.gameObject.AddComponent<NpcQuestRowUI>();
        SerializedObject so = new SerializedObject(ui);
        Set(so, "button", button);
        Set(so, "background", background);
        Set(so, "selectionOutline", outline);
        Set(so, "portrait", face);
        Set(so, "titleText", title);
        Set(so, "subtitleText", subtitle);
        Set(so, "tagBackground", tagBack);
        Set(so, "tagText", tag);
        so.ApplyModifiedPropertiesWithoutUndo();
        background.gameObject.SetActive(false);
        return ui;
    }

    private static NpcQuestItemUI CreateRequirement(RectTransform parent)
    {
        Image background = CreateImage(parent, "LP_RequirementTemplate", UISpriteFactory.Slot, new Color(1f, 1f, 1f, 0.05f));
        background.type = Image.Type.Sliced;
        background.rectTransform.sizeDelta = new Vector2(168f, 60f);
        Image icon = CreateImage(background.rectTransform, "LP_Icon", null, Color.white);
        icon.preserveAspect = true;
        Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(44f, 44f));
        TMP_Text itemName = CreateText(background.rectTransform, "LP_Name", "ITEM", 12f, FontStyles.Bold, TextAlignmentOptions.BottomLeft, ProjectUUIPalette.TextSecondary);
        SetOffsets(itemName.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(58f, 0f), new Vector2(-6f, -6f));
        TMP_Text count = CreateText(background.rectTransform, "LP_Count", "0 / 1", 17f, FontStyles.Bold, TextAlignmentOptions.TopLeft, ProjectUUIPalette.TextPrimary);
        SetOffsets(count.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(58f, 4f), new Vector2(-6f, -2f));

        NpcQuestItemUI ui = background.gameObject.AddComponent<NpcQuestItemUI>();
        SerializedObject so = new SerializedObject(ui);
        Set(so, "background", background);
        Set(so, "icon", icon);
        Set(so, "nameText", itemName);
        Set(so, "countText", count);
        so.ApplyModifiedPropertiesWithoutUndo();
        background.gameObject.SetActive(false);
        return ui;
    }

    // ------------------------------------------------------------ 진행 중 의뢰 표시

    public static NpcQuestTrackerUI BuildTracker(out string report)
    {
        EnsureSprites();
        RectTransform buffs = FindRect(CookingPopupUIBuilder.BuffRootName); // 오른쪽 시간 표시 아래는 주변 아이템 창 자리라서 왼쪽 음식 효과 아래에 둔다

        if (buffs == null || buffs.parent == null)
        {
            report = "✗ 음식 효과 표시(LP_FoodBuffHUD)를 찾지 못해 의뢰 표시를 만들지 못했습니다. 5번 메뉴(요리)를 확인하세요.";
            return null;
        }

        Transform parent = buffs.parent;
        RemoveExisting(parent, TrackerRootName);
        float top = buffs.anchoredPosition.y - buffs.sizeDelta.y - 8f; // 음식 효과 표시 바로 아래
        RectTransform root = CreateRect(parent, TrackerRootName);
        TopLeft(root, new Vector2(buffs.anchoredPosition.x, top), new Vector2(TrackerWidth, 10f));

        Image panel = CreateImage(root, "LP_Panel", UISpriteFactory.Panel, ProjectUUIPalette.HudPanel);
        panel.type = Image.Type.Sliced;
        RectTransform p = panel.rectTransform;
        p.anchorMin = new Vector2(0f, 1f);
        p.anchorMax = new Vector2(1f, 1f);
        p.pivot = new Vector2(0.5f, 1f);
        p.anchoredPosition = Vector2.zero;
        p.sizeDelta = new Vector2(0f, 60f);
        VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 10, 12);
        layout.spacing = 5f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TMP_Text title = CreateText(p, "LP_Title", "의뢰 0/3", 13f, FontStyles.Bold, TextAlignmentOptions.TopLeft, ProjectUUIPalette.Accent);
        title.characterSpacing = 1f;
        TMP_Text body = CreateText(p, "LP_Body", string.Empty, 14f, FontStyles.Normal, TextAlignmentOptions.TopLeft, ProjectUUIPalette.TextPrimary);
        body.textWrappingMode = TextWrappingModes.Normal;
        body.overflowMode = TextOverflowModes.Overflow;
        body.richText = true;
        body.lineSpacing = 2f;
        TMP_Text notice = CreateText(p, "LP_Notice", string.Empty, 12.5f, FontStyles.Bold, TextAlignmentOptions.TopLeft, ProjectUUIPalette.Teal);
        notice.textWrappingMode = TextWrappingModes.Normal;
        notice.overflowMode = TextOverflowModes.Overflow;
        notice.gameObject.SetActive(false);
        panel.gameObject.SetActive(false);

        NpcQuestTrackerUI tracker = root.gameObject.AddComponent<NpcQuestTrackerUI>();
        SerializedObject so = new SerializedObject(tracker);
        Set(so, "panel", panel.gameObject);
        Set(so, "titleText", title);
        Set(so, "bodyText", body);
        Set(so, "noticeText", notice);
        so.ApplyModifiedPropertiesWithoutUndo();
        report = $"진행 중 의뢰 표시 (화면 왼쪽, 음식 효과 아래 y {top:0})";
        return tracker;
    }

    private static void EnsureSprites()
    {
        if (UISpriteFactory.Panel == null || UISpriteFactory.Pill == null || UISpriteFactory.Slot == null)
        {
            UISpriteFactory.GenerateAll();
        }
    }

    private static void Set(SerializedObject serialized, string property, Object value)
    {
        serialized.FindProperty(property).objectReferenceValue = value;
    }
}
