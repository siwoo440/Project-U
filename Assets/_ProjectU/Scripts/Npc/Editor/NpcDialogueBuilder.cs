using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using static UIBuildKit;

// 91일차: NPC 대화 도구 (93일차: 대화 창에 의뢰 전달 버튼 추가, 11번 메뉴가 창만 다시 만들 수 있음)
// 1. 알파 NPC 7명의 초상(저폴리 모델 가슴 위, 캐릭터 색)을 만들어 NPC 데이터에 연결
// 2. 게임 Scene의 PopupLayer에 대화 창(초상 · 이름 · 호감도 · 대사 · 대화/선물/거래 · 선물 목록)을 만들고 GameUIManager에 연결
// 3. NPC 관리자 오브젝트에 관계 관리자(호감도 · 만남 · 하루 대화·선물)를 붙인다
// 여러 번 실행해도 같은 Asset·오브젝트를 갱신한다. 실행 후 Ctrl+S로 Scene을 저장해야 반영된다.
public static class NpcDialogueBuilder
{
    public const string BuildMenuRoot = "Tools/Project U/Build Content/";
    private const string DialogTitle = "Project U NPC 대화";
    private const string ScenePath = "Assets/_ProjectU/Scenes/20_Gameplay.unity";
    public const string PortraitFolder = "Assets/_ProjectU/UI/Icons/Npc";
    public const string PopupRootName = "LP_NpcDialogue";
    private const string PopupLayerName = "PopupLayer";

    private const float WindowWidth = 1180f;
    private const float WindowHeight = 262f; // 93일차: 버튼 4칸 (대화 · 선물 · 의뢰 · 거래)
    private const float ButtonHeight = 40f;
    private const float ButtonStep = 46f;
    public const int ChoiceCount = 3; // 94일차: 이벤트 선택지 버튼 수
    private const float WindowBottom = 122f; // 핫바(화면 아래) 위로
    private const float Pad = 20f;
    private const float PortraitSize = 210f;
    private const float TextLeft = Pad + PortraitSize + 24f;
    private const float ButtonWidth = 168f;
    private const float GiftHeight = 176f;

    // ---------------------------------------------------------------- 메뉴

    [MenuItem(BuildMenuRoot + "9. NPC Dialogue (Portraits + Popup + Relationships)", false, 28)]
    private static void BuildAllMenu()
    {
        bool confirmed = EditorUtility.DisplayDialog(
            DialogTitle,
            "알파 NPC 7명의 초상을 만들고,\n"
            + "현재 게임 Scene(20_Gameplay)에 NPC 대화 창과 관계(호감도) 관리자를 추가합니다.\n\n"
            + "먼저 8번 메뉴(NPC 마을 배치)를 실행해 두어야 합니다.\n"
            + "실행 후 Ctrl+S로 Scene을 저장해야 반영됩니다.",
            "실행",
            "취소");

        if (!confirmed)
        {
            return;
        }

        string report = BuildAll();
        Debug.Log(report);
        EditorUtility.DisplayDialog(DialogTitle, report.Length <= 1800 ? report : report.Substring(0, 1800) + "\n... (전체 내용은 Console 참고)", "확인");
    }

    // ---------------------------------------------------------------- 전체 생성

    public static string BuildAll()
    {
        StringBuilder report = new StringBuilder("[NPC 대화 생성]\n");

        try
        {
            EditorUtility.DisplayProgressBar(DialogTitle, "초상", 0.1f);
            NpcDatabase database = AssetDatabase.LoadAssetAtPath<NpcDatabase>(NpcContentBuilder.DatabasePath);

            if (database == null)
            {
                report.AppendLine("✗ NpcDatabase가 없습니다. 7번·8번 메뉴를 먼저 실행하세요.");
                return report.ToString();
            }

            report.AppendLine(CreatePortraits(database.GetPlacedCast())); // 101일차: 섬에 배치되는 NPC 모두

            EditorUtility.DisplayProgressBar(DialogTitle, "대화 창", 0.6f);
            report.Append(WireScene(database));
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        report.Append(Validate(out _));
        return report.ToString();
    }

    // ---------------------------------------------------------------- 초상

    public static string GetPortraitPath(NpcCharacterData character)
    {
        return $"{PortraitFolder}/PORTRAIT_{character.EnglishName.Replace(" ", string.Empty)}.png";
    }

    private static string CreatePortraits(List<NpcCharacterData> cast)
    {
        StylizedArtAssetFactory.EnsureFolder(PortraitFolder);
        List<(NpcCharacterData character, string path)> written = new List<(NpcCharacterData, string)>();
        List<string> failed = new List<string>();

        using (ItemIconRenderer renderer = new ItemIconRenderer())
        {
            foreach (NpcCharacterData character in cast)
            {
                GameObject model = StylizedArtAssetFactory.LoadModelPrefab(StylizedModelLibrary.GetNpcModelId(character.CharacterId));

                if (model == null)
                {
                    failed.Add($"{character.DisplayName}(모델 없음)");
                    continue;
                }

                float height = model.GetComponentInChildren<MeshFilter>().sharedMesh.bounds.max.y;
                float cut = height * 0.6f; // 가슴 위만 화면에 맞춤
                bool special = StylizedModelLibrary.TryGetNpcPortraitBox(StylizedModelLibrary.GetNpcModelId(character.CharacterId), out Bounds upper); // 101일차: 특수 체형 · 날개
                Color outfit = character.ThemeColor;
                Color accent = character.AccentColor;
                Color hair = NpcPlacementBuilder.GetHairColor(character.CharacterId);

                byte[] png = renderer.RenderPng(
                    model,
                    new ItemIconRenderer.Framing(Vector3.zero, 18f, 4f, 1f),
                    out string error,
                    instance =>
                    {
                        NpcAppearance appearance = instance.AddComponent<NpcAppearance>();
                        appearance.EditorAssignRenderers(instance.GetComponentsInChildren<Renderer>(true));
                        appearance.SetColors(outfit, accent, hair);
                    },
                    world => special ? upper.Contains(world) : world.y >= cut);

                if (png == null)
                {
                    failed.Add($"{character.DisplayName}({error})");
                    continue;
                }

                string path = GetPortraitPath(character);
                ItemIconRenderer.WritePng(png, path);
                written.Add((character, path));
            }
        }

        foreach ((NpcCharacterData character, string path) in written)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            ItemIconRenderer.ImportAsSprite(path);
            character.EditorAssignPortrait(AssetDatabase.LoadAssetAtPath<Sprite>(path));
            EditorUtility.SetDirty(character);
        }

        AssetDatabase.SaveAssets();
        return failed.Count == 0
            ? $"NPC 초상 {written.Count}개 생성 ({PortraitFolder})"
            : $"NPC 초상 {written.Count}개 생성, ✗ 실패 : {string.Join(", ", failed)}";
    }

    // ---------------------------------------------------------------- Scene

    private static string WireScene(NpcDatabase database)
    {
        StringBuilder report = new StringBuilder();
        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.GetActiveScene();
        GameUIManager uiManager = Object.FindFirstObjectByType<GameUIManager>(FindObjectsInactive.Include);
        NpcManager npcManager = Object.FindFirstObjectByType<NpcManager>(FindObjectsInactive.Include);

        if (scene.path != ScenePath || uiManager == null)
        {
            report.AppendLine($"✗ 게임 Scene({ScenePath})을 열고 다시 실행하세요. 초상만 만들었습니다.");
            return report.ToString();
        }

        if (npcManager == null)
        {
            report.AppendLine("✗ NPC 관리자가 없습니다. 8번 메뉴(NPC 마을 배치)를 먼저 실행하세요.");
            return report.ToString();
        }

        NpcRelationshipManager relations = npcManager.GetComponent<NpcRelationshipManager>();

        if (relations == null)
        {
            relations = npcManager.gameObject.AddComponent<NpcRelationshipManager>();
        }

        relations.EditorAssign(database);
        EditorUtility.SetDirty(relations);
        report.AppendLine($"관계 관리자 : {npcManager.name}");

        if (UISpriteFactory.Panel == null || UISpriteFactory.Pill == null || UISpriteFactory.Slot == null)
        {
            UISpriteFactory.GenerateAll();
        }

        RectTransform popupLayer = FindRect(PopupLayerName);

        if (popupLayer == null)
        {
            report.AppendLine("✗ PopupLayer를 찾지 못해 대화 창을 만들지 못했습니다.");
            return report.ToString();
        }

        NpcDialoguePopup popup = BuildPopup(popupLayer);
        SerializedObject serialized = new SerializedObject(uiManager);
        serialized.FindProperty("npcDialoguePopup").objectReferenceValue = popup;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(uiManager);
        report.AppendLine("대화 창 생성 (PopupLayer) · GameUIManager 연결");

        EditorSceneManager.MarkSceneDirty(scene);
        report.AppendLine("Scene 변경 완료 : Ctrl+S로 저장하세요.");
        return report.ToString();
    }

    public static string RebuildPopup() // 93일차: 대화 창만 다시 만들고 GameUIManager에 연결 (의뢰 버튼, 11번 메뉴에서 사용)
    {
        if (UISpriteFactory.Panel == null || UISpriteFactory.Pill == null || UISpriteFactory.Slot == null)
        {
            UISpriteFactory.GenerateAll();
        }

        RectTransform popupLayer = FindRect(PopupLayerName);
        GameUIManager uiManager = Object.FindFirstObjectByType<GameUIManager>(FindObjectsInactive.Include);

        if (popupLayer == null || uiManager == null)
        {
            return "✗ PopupLayer 또는 GameUIManager가 없어 대화 창을 다시 만들지 못했습니다.";
        }

        NpcDialoguePopup popup = BuildPopup(popupLayer);
        SerializedObject serialized = new SerializedObject(uiManager);
        serialized.FindProperty("npcDialoguePopup").objectReferenceValue = popup;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(uiManager);
        return "대화 창 다시 생성 (의뢰 버튼 포함) · GameUIManager 연결";
    }

    private static NpcDialoguePopup BuildPopup(RectTransform popupLayer)
    {
        RemoveExisting(popupLayer, PopupRootName);
        RectTransform root = CreateRect(popupLayer, PopupRootName);
        Stretch(root);
        root.SetAsFirstSibling();

        RectTransform panel = CreateRect(root, "LP_Panel");
        Stretch(panel);

        // 아래쪽 대화 상자
        Image shadow = CreateImage(panel, "LP_Shadow", UISpriteFactory.Shadow, Color.white);
        shadow.type = Image.Type.Sliced;
        Place(shadow.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, WindowBottom - 30f), new Vector2(WindowWidth + 70f, WindowHeight + 70f));

        Image window = CreateImage(panel, "LP_Window", UISpriteFactory.Panel, ProjectUUIPalette.PanelDark);
        window.type = Image.Type.Sliced;
        window.raycastTarget = true;
        Place(window.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, WindowBottom), new Vector2(WindowWidth, WindowHeight));
        RectTransform w = window.rectTransform;

        Image outline = CreateImage(w, "LP_Outline", UISpriteFactory.PanelOutline, new Color(0.95f, 0.72f, 0.3f, 0.25f));
        outline.type = Image.Type.Sliced;
        Stretch(outline.rectTransform);

        // 초상 (대표 색 테두리)
        Image frame = CreateImage(w, "LP_PortraitFrame", UISpriteFactory.Slot, Color.white);
        frame.type = Image.Type.Sliced;
        TopLeft(frame.rectTransform, new Vector2(Pad, -Pad), new Vector2(PortraitSize, PortraitSize));
        Image portraitBack = CreateImage(frame.rectTransform, "LP_PortraitBack", UISpriteFactory.Slot, ProjectUUIPalette.PanelMid);
        portraitBack.type = Image.Type.Sliced;
        SetOffsets(portraitBack.rectTransform, Vector2.zero, Vector2.one, new Vector2(6f, 6f), new Vector2(-6f, -6f));
        Image portrait = CreateImage(portraitBack.rectTransform, "LP_Portrait", null, Color.white);
        portrait.preserveAspect = true;
        SetOffsets(portrait.rectTransform, Vector2.zero, Vector2.one, new Vector2(4f, 4f), new Vector2(-4f, -4f));

        // 이름 · 직업 · 호감도
        TMP_Text nameText = CreateText(w, "LP_Name", "이름", 30f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, ProjectUUIPalette.TextPrimary);
        TopLeft(nameText.rectTransform, new Vector2(TextLeft, -18f), new Vector2(360f, 40f));
        TMP_Text jobText = CreateText(w, "LP_Job", "종족 · 직업", 17f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, ProjectUUIPalette.TextSecondary);
        TopLeft(jobText.rectTransform, new Vector2(TextLeft, -56f), new Vector2(420f, 26f));

        float affinityLeft = WindowWidth - Pad - ButtonWidth - 24f - 280f;
        TMP_Text stageText = CreateText(w, "LP_Stage", "무관심 · 호감도 0/100", 16f, FontStyles.Bold, TextAlignmentOptions.MidlineRight, ProjectUUIPalette.Accent);
        TopLeft(stageText.rectTransform, new Vector2(affinityLeft, -22f), new Vector2(280f, 26f));
        Image barBack = CreateImage(w, "LP_AffinityBar", UISpriteFactory.Pill, ProjectUUIPalette.BarBackground);
        barBack.type = Image.Type.Sliced;
        TopLeft(barBack.rectTransform, new Vector2(affinityLeft, -54f), new Vector2(280f, 12f));
        Image barFill = CreateImage(barBack.rectTransform, "LP_Fill", UISpriteFactory.Pill, ProjectUUIPalette.Accent);
        barFill.type = Image.Type.Sliced;
        SetOffsets(barFill.rectTransform, Vector2.zero, new Vector2(0.3f, 1f), Vector2.zero, Vector2.zero);

        for (int index = 1; index < 5; index++)
        {
            Image tick = CreateImage(barBack.rectTransform, $"LP_Tick{index}", null, new Color(0f, 0f, 0f, 0.45f));
            SetOffsets(tick.rectTransform, new Vector2(index * 0.2f, 0f), new Vector2(index * 0.2f, 1f), new Vector2(-1f, 0f), new Vector2(1f, 0f));
        }

        Image divider = CreateImage(w, "LP_Divider", null, Faint);
        TopLeft(divider.rectTransform, new Vector2(TextLeft, -90f), new Vector2(WindowWidth - TextLeft - ButtonWidth - Pad - 24f, 1f));

        // 대사 (누르면 전체 표시)
        Image lineArea = CreateImage(w, "LP_LineArea", null, new Color(0f, 0f, 0f, 0f));
        lineArea.raycastTarget = true;
        TopLeft(lineArea.rectTransform, new Vector2(TextLeft, -100f), new Vector2(WindowWidth - TextLeft - ButtonWidth - Pad - 24f, 110f));
        Button lineButton = lineArea.gameObject.AddComponent<Button>();
        lineButton.transition = Selectable.Transition.None;
        TMP_Text lineText = CreateText(lineArea.rectTransform, "LP_Line", "대사", 23f, FontStyles.Normal, TextAlignmentOptions.TopLeft, ProjectUUIPalette.TextPrimary);
        lineText.textWrappingMode = TextWrappingModes.Normal;
        lineText.overflowMode = TextOverflowModes.Overflow;
        lineText.lineSpacing = 8f;
        Stretch(lineText.rectTransform);

        TMP_Text message = CreateText(w, "LP_Message", string.Empty, 15f, FontStyles.Bold, TextAlignmentOptions.BottomLeft, ProjectUUIPalette.Accent);
        message.textWrappingMode = TextWrappingModes.Normal;
        message.overflowMode = TextOverflowModes.Overflow;
        TopLeft(message.rectTransform, new Vector2(TextLeft, -WindowHeight + 52f), new Vector2(WindowWidth - TextLeft - ButtonWidth - Pad - 24f, 40f));
        message.gameObject.SetActive(false);

        // 오른쪽 버튼 (93일차: 의뢰 버튼을 넣으려고 한 칸 40px로 줄임)
        float buttonX = WindowWidth - Pad - ButtonWidth;
        Button talk = CreateButton(w, "LP_Talk", "대화하기", 18f, ProjectUUIPalette.Accent, ProjectUUIPalette.TextDark, out _, out _);
        TopLeft((RectTransform)talk.transform, new Vector2(buttonX, -Pad), new Vector2(ButtonWidth, ButtonHeight));
        Button gift = CreateButton(w, "LP_Gift", "선물하기", 18f, ProjectUUIPalette.ButtonNormal, ProjectUUIPalette.TextPrimary, out _, out TMP_Text giftLabel);
        TopLeft((RectTransform)gift.transform, new Vector2(buttonX, -Pad - ButtonStep), new Vector2(ButtonWidth, ButtonHeight));
        Button quest = CreateButton(w, "LP_Quest", "의뢰 전달", 17f, ProjectUUIPalette.Hunger, ProjectUUIPalette.TextDark, out _, out TMP_Text questLabel);
        TopLeft((RectTransform)quest.transform, new Vector2(buttonX, -Pad - ButtonStep * 2f), new Vector2(ButtonWidth, ButtonHeight));
        quest.gameObject.SetActive(false);
        Button trade = CreateButton(w, "LP_Trade", "거래", 18f, ProjectUUIPalette.Teal, ProjectUUIPalette.TextDark, out _, out _);
        TopLeft((RectTransform)trade.transform, new Vector2(buttonX, -Pad - ButtonStep * 3f), new Vector2(ButtonWidth, ButtonHeight));
        Button close = CreateButton(w, "LP_Close", "닫기 (ESC)", 15f, new Color(1f, 1f, 1f, 0.06f), ProjectUUIPalette.TextSecondary, out _, out _);
        TopLeft((RectTransform)close.transform, new Vector2(buttonX, -WindowHeight + Pad + 36f), new Vector2(ButtonWidth, 36f));

        // 94일차: 하트 이벤트 선택지 (대화 · 선물 · 의뢰 자리에 겹쳐 두고 선택할 때만 보임)
        Button[] choices = new Button[ChoiceCount];
        TMP_Text[] choiceLabels = new TMP_Text[ChoiceCount];

        for (int index = 0; index < ChoiceCount; index++)
        {
            choices[index] = CreateButton(w, $"LP_Choice{index}", "선택지", 15f, new Color(0.94f, 0.55f, 0.66f, 1f), ProjectUUIPalette.TextDark, out _, out TMP_Text choiceLabel);
            choiceLabel.textWrappingMode = TextWrappingModes.Normal;
            choiceLabel.enableAutoSizing = true;
            choiceLabel.fontSizeMin = 11f;
            choiceLabel.fontSizeMax = 15f;
            choiceLabel.margin = new Vector4(8f, 2f, 8f, 2f);
            TopLeft((RectTransform)choices[index].transform, new Vector2(buttonX, -Pad - ButtonStep * index), new Vector2(ButtonWidth, ButtonHeight));
            choices[index].gameObject.SetActive(false);
            choiceLabels[index] = choiceLabel;
        }

        TMP_Text hint = CreateText(w, "LP_Hint", "스페이스 · 엔터 : 다음 말", 12f, FontStyles.Normal, TextAlignmentOptions.MidlineRight, new Color(0.72f, 0.7f, 0.65f, 0.6f));
        TopLeft(hint.rectTransform, new Vector2(buttonX - 250f, -WindowHeight + 30f), new Vector2(236f, 20f));

        // 선물 목록 (대화 상자 위)
        Image giftWindow = CreateImage(panel, "LP_GiftPanel", UISpriteFactory.Panel, ProjectUUIPalette.PanelMid);
        giftWindow.type = Image.Type.Sliced;
        giftWindow.raycastTarget = true;
        Place(giftWindow.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, WindowBottom + WindowHeight + 12f), new Vector2(WindowWidth, GiftHeight));
        RectTransform g = giftWindow.rectTransform;
        TMP_Text giftTitle = CreateText(g, "LP_GiftTitle", "선물할 아이템을 고르세요 (하루 한 번)", 16f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, ProjectUUIPalette.TextPrimary);
        TopLeft(giftTitle.rectTransform, new Vector2(Pad, -10f), new Vector2(600f, 28f));
        Button cancel = CreateButton(g, "LP_GiftCancel", "취소", 14f, ProjectUUIPalette.ButtonNormal, ProjectUUIPalette.TextPrimary, out _, out _);
        Place((RectTransform)cancel.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-Pad, -8f), new Vector2(96f, 32f));

        Image viewport = CreateImage(g, "LP_GiftScroll", null, new Color(0f, 0f, 0f, 0f));
        viewport.raycastTarget = true;
        TopLeft(viewport.rectTransform, new Vector2(Pad, -44f), new Vector2(WindowWidth - Pad * 2f, GiftHeight - 54f));
        viewport.gameObject.AddComponent<RectMask2D>();
        RectTransform list = CreateRect(viewport.rectTransform, "LP_GiftList");
        list.anchorMin = new Vector2(0f, 0f);
        list.anchorMax = new Vector2(0f, 1f);
        list.pivot = new Vector2(0f, 0.5f);
        list.anchoredPosition = Vector2.zero;
        list.sizeDelta = new Vector2(WindowWidth, 0f);
        HorizontalLayoutGroup layout = list.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = list.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
        ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.content = list;
        scroll.horizontal = true;
        scroll.vertical = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;

        NpcGiftSlotUI template = BuildGiftSlot(list);
        TMP_Text empty = CreateText(g, "LP_GiftEmpty", "가방에 선물할 아이템이 없어요.", 16f, FontStyles.Normal, TextAlignmentOptions.Center, ProjectUUIPalette.TextSecondary);
        TopLeft(empty.rectTransform, new Vector2(Pad, -60f), new Vector2(WindowWidth - Pad * 2f, 60f));
        empty.gameObject.SetActive(false);
        giftWindow.gameObject.SetActive(false);

        // 연결
        NpcDialoguePopup popup = root.gameObject.AddComponent<NpcDialoguePopup>();
        SerializedObject serialized = new SerializedObject(popup);
        Set(serialized, "panelRoot", panel.gameObject);
        Set(serialized, "portraitFrame", frame);
        Set(serialized, "portrait", portrait);
        Set(serialized, "nameText", nameText);
        Set(serialized, "jobText", jobText);
        Set(serialized, "stageText", stageText);
        Set(serialized, "affinityFill", barFill.rectTransform);
        Set(serialized, "lineText", lineText);
        Set(serialized, "lineButton", lineButton);
        Set(serialized, "messageText", message);
        Set(serialized, "talkButton", talk);
        Set(serialized, "giftButton", gift);
        Set(serialized, "giftLabel", giftLabel);
        Set(serialized, "tradeButton", trade);
        Set(serialized, "questButton", quest);
        Set(serialized, "questLabel", questLabel);
        SerializedProperty choiceButtonList = serialized.FindProperty("choiceButtons");
        SerializedProperty choiceLabelList = serialized.FindProperty("choiceLabels");
        choiceButtonList.arraySize = ChoiceCount;
        choiceLabelList.arraySize = ChoiceCount;

        for (int index = 0; index < ChoiceCount; index++)
        {
            choiceButtonList.GetArrayElementAtIndex(index).objectReferenceValue = choices[index];
            choiceLabelList.GetArrayElementAtIndex(index).objectReferenceValue = choiceLabels[index];
        }
        Set(serialized, "closeButton", close);
        Set(serialized, "giftPanel", giftWindow.gameObject);
        Set(serialized, "giftListRoot", list);
        Set(serialized, "giftSlotTemplate", template);
        Set(serialized, "giftEmptyText", empty);
        Set(serialized, "giftCancelButton", cancel);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        panel.gameObject.SetActive(false);
        return popup;
    }

    private static NpcGiftSlotUI BuildGiftSlot(RectTransform parent)
    {
        Image background = CreateImage(parent, "LP_GiftSlotTemplate", UISpriteFactory.Slot, ProjectUUIPalette.Slot);
        background.type = Image.Type.Sliced;
        background.raycastTarget = true;
        background.rectTransform.sizeDelta = new Vector2(104f, 112f);
        Button button = background.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        SetTint(button, 1.25f);

        Image icon = CreateImage(background.rectTransform, "LP_Icon", null, Color.white);
        icon.preserveAspect = true;
        Place(icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(64f, 64f));
        TMP_Text count = CreateText(background.rectTransform, "LP_Count", "x2", 13f, FontStyles.Bold, TextAlignmentOptions.TopRight, ProjectUUIPalette.TextPrimary);
        Place(count.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-6f, -4f), new Vector2(50f, 20f));
        TMP_Text itemName = CreateText(background.rectTransform, "LP_Name", "ITEM", 11f, FontStyles.Bold, TextAlignmentOptions.Center, ProjectUUIPalette.TextSecondary);
        itemName.textWrappingMode = TextWrappingModes.Normal;
        itemName.overflowMode = TextOverflowModes.Ellipsis;
        Place(itemName.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(96f, 32f));

        NpcGiftSlotUI slot = background.gameObject.AddComponent<NpcGiftSlotUI>();
        SerializedObject serialized = new SerializedObject(slot);
        Set(serialized, "button", button);
        Set(serialized, "icon", icon);
        Set(serialized, "nameText", itemName);
        Set(serialized, "countText", count);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        background.gameObject.SetActive(false);
        return slot;
    }

    private static void Set(SerializedObject serialized, string property, Object value)
    {
        serialized.FindProperty(property).objectReferenceValue = value;
    }

    // ---------------------------------------------------------------- 검증

    private static readonly string[] PopupFields =
    {
        "panelRoot", "portraitFrame", "portrait", "nameText", "jobText", "stageText", "affinityFill", "lineText", "lineButton", "messageText",
        "talkButton", "giftButton", "giftLabel", "tradeButton", "questButton", "questLabel", "closeButton", "giftPanel", "giftListRoot", "giftSlotTemplate", "giftEmptyText", "giftCancelButton"
    };

    public static string Validate(out int errorCount)
    {
        StringBuilder report = new StringBuilder("[NPC 대화 검증]\n");
        int errors = 0;

        void Error(string message)
        {
            errors++;
            report.AppendLine("✗ " + message);
        }

        NpcDatabase database = AssetDatabase.LoadAssetAtPath<NpcDatabase>(NpcContentBuilder.DatabasePath);
        List<NpcCharacterData> cast = database != null ? database.GetPlacedCast() : new List<NpcCharacterData>();
        int portraits = cast.Count(character => character.Portrait != null);

        foreach (NpcCharacterData character in cast.Where(character => character.Portrait == null))
        {
            Error($"{character.CharacterId} : 대화 창 초상이 없습니다. 9번 메뉴를 실행하세요.");
        }

        if (EditorSceneManager.GetActiveScene().path != ScenePath)
        {
            report.AppendLine("Scene 검사 생략 (게임 Scene이 열려 있지 않음)");
        }
        else
        {
            GameUIManager uiManager = Object.FindFirstObjectByType<GameUIManager>(FindObjectsInactive.Include);
            NpcDialoguePopup popup = uiManager != null ? uiManager.NpcDialoguePopup : null;

            if (popup == null)
            {
                Error("GameUIManager에 NPC 대화 창이 연결되지 않았습니다.");
            }
            else
            {
                SerializedObject serialized = new SerializedObject(popup);

                foreach (string field in PopupFields.Where(field => serialized.FindProperty(field).objectReferenceValue == null))
                {
                    Error($"NPC 대화 창의 {field} 연결이 비어 있습니다.");
                }

                foreach (string list in new[] { "choiceButtons", "choiceLabels" }) // 94일차: 이벤트 선택지
                {
                    SerializedProperty property = serialized.FindProperty(list);

                    if (property.arraySize != ChoiceCount || Enumerable.Range(0, property.arraySize).Any(index => property.GetArrayElementAtIndex(index).objectReferenceValue == null))
                    {
                        Error($"NPC 대화 창의 이벤트 선택지({list})가 {ChoiceCount}개가 아닙니다. 12번 메뉴를 실행하세요.");
                    }
                }
            }

            NpcManager npcManager = Object.FindFirstObjectByType<NpcManager>(FindObjectsInactive.Include);
            NpcRelationshipManager relations = npcManager != null ? npcManager.GetComponent<NpcRelationshipManager>() : null;

            if (relations == null || relations.Database == null)
            {
                Error("NPC 관리자에 관계 관리자(데이터베이스 연결)가 없습니다.");
            }
        }

        report.AppendLine($"초상 {portraits}/{cast.Count}개");
        errorCount = errors;
        report.AppendLine(errors == 0 ? "결과 : 오류 0개" : $"결과 : 오류 {errors}개");
        return report.ToString();
    }
}
