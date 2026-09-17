using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 78일차: Project U UI 테마(둥근 패널·버튼·슬롯, 색상, 글자 그림자)와 HUD 배치를 적용한다.
public static class UIThemeApplier
{
    private const string PrefabFolder = "Assets/_ProjectU/Prefabs/UI";
    private const string ThemeFolder = "Assets/_ProjectU/UI/Themes";
    private const string GeneratedPrefix = "LP_";

    private enum ImageRole
    {
        Skip,
        Overlay,
        Panel,
        Button,
        Slot,
        BarFill,
        ScrollTrack,
        ScrollHandle,
        ToggleBox,
        Accent,
        Inset,
        Clear
    }

    private static bool useUndo;
    private static Material hudTextMaterial;

    // ------------------------------------------------------------ 진입점

    public static string ApplyToPrefabs()
    {
        EnsureSprites();
        useUndo = false;
        StringBuilder report = new StringBuilder();
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder });
        int count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);

            try
            {
                int styled = RestyleHierarchy(root.transform, false);

                if (styled > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    count++;
                    report.AppendLine($"{System.IO.Path.GetFileName(path)} : UI 요소 {styled}개");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        report.Insert(0, $"UI Prefab 테마 적용 {count}개\n");
        return report.ToString();
    }

    public static string ApplyToScene()
    {
        EnsureSprites();
        useUndo = true;
        hudTextMaterial = GetOrCreateHudTextMaterial();
        Scene scene = SceneManager.GetActiveScene();
        StringBuilder report = new StringBuilder();
        int total = 0;

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
            {
                if (canvas.renderMode == RenderMode.WorldSpace || !canvas.isRootCanvas)
                {
                    continue;
                }

                total += RestyleHierarchy(canvas.transform, true);
            }
        }

        report.AppendLine($"Scene UI 요소 {total}개 스타일 적용");
        report.AppendLine(LayoutSurvivalBars());
        report.AppendLine(LayoutDayTime());
        report.AppendLine(LayoutMinimap());
        report.AppendLine(LayoutPromptAndDebug());
        report.AppendLine(LayoutNearbyLoot());
        report.AppendLine(LayoutDeathScreen());
        report.AppendLine(LayoutHotbarAndBuild());

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(scene);
        return report.ToString();
    }

    private static void EnsureSprites()
    {
        if (UISpriteFactory.Panel == null || UISpriteFactory.Icon("Health") == null || BarFillSprite == null)
        {
            UISpriteFactory.GenerateAll();
        }
    }

    private static Sprite BarFillSprite => AssetDatabase.LoadAssetAtPath<Sprite>($"{UISpriteFactory.SpriteFolder}/UI_BarFill.png");

    // ------------------------------------------------------------ 일반 규칙

    private static int RestyleHierarchy(Transform root, bool isScene)
    {
        int count = 0;

        foreach (Image image in root.GetComponentsInChildren<Image>(true))
        {
            if (image.name.StartsWith(GeneratedPrefix))
            {
                continue;
            }

            ImageRole role = Classify(image);

            if (role == ImageRole.Skip)
            {
                continue;
            }

            StyleImage(image, role);
            count++;
        }

        foreach (Selectable selectable in root.GetComponentsInChildren<Selectable>(true))
        {
            StyleSelectable(selectable);
        }

        foreach (Outline outline in root.GetComponentsInChildren<Outline>(true))
        {
            Record(outline);
            outline.effectColor = ProjectUUIPalette.Accent;
            outline.effectDistance = new Vector2(2f, -2f);
        }

        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            StyleText(text, isScene);
            count++;
        }

        foreach (InventorySlotsUI slotsUI in root.GetComponentsInChildren<InventorySlotsUI>(true))
        {
            SerializedObject serialized = new SerializedObject(slotsUI);
            SetColor(serialized, "hotbarAreaColor", new Color(0.95f, 0.72f, 0.3f, 0.12f));
            SetColor(serialized, "inventoryAreaColor", new Color(0f, 0f, 0f, 0.28f));
            SerializedProperty sprite = serialized.FindProperty("sectionSprite");

            if (sprite != null)
            {
                sprite.objectReferenceValue = UISpriteFactory.Panel;
            }

            ApplySerialized(serialized);
        }

        return count;
    }

    private static ImageRole Classify(Image image)
    {
        string name = image.name;

        if (image.GetComponent<Mask>() != null)
        {
            return ImageRole.Skip;
        }

        // RectMask2D Viewport의 이미지는 스크롤 입력 영역으로만 쓰이므로 보이지 않게 한다
        if (name == "Viewport")
        {
            return ImageRole.Clear;
        }

        if (name.Contains("Icon") || name.Contains("Flash") || name.Contains("Fade") || name.Contains("Portrait"))
        {
            return ImageRole.Skip;
        }

        if (image.sprite != null && !IsThemeReplaceable(image.sprite))
        {
            return ImageRole.Skip;
        }

        Scrollbar parentScrollbar = image.GetComponentInParent<Scrollbar>(true);

        if (parentScrollbar != null)
        {
            if (parentScrollbar.handleRect == image.rectTransform)
            {
                return ImageRole.ScrollHandle;
            }

            if (parentScrollbar.gameObject == image.gameObject)
            {
                return ImageRole.ScrollTrack;
            }
        }

        Slider parentSlider = image.GetComponentInParent<Slider>(true);

        if (parentSlider != null)
        {
            if (parentSlider.fillRect == image.rectTransform || parentSlider.handleRect == image.rectTransform)
            {
                return ImageRole.Accent;
            }

            return ImageRole.ScrollTrack;
        }

        Toggle parentToggle = image.GetComponentInParent<Toggle>(true);

        if (parentToggle != null)
        {
            if (parentToggle.graphic == image)
            {
                return ImageRole.Accent;
            }

            if (parentToggle.targetGraphic == image)
            {
                return ImageRole.ToggleBox;
            }
        }

        if (image.type == Image.Type.Filled || name.Contains("Fill"))
        {
            return ImageRole.BarFill;
        }

        bool isSlot = name.EndsWith("Slot") || name.Contains("SlotTemplate")
            || image.GetComponent<InventorySlotView>() != null
            || image.GetComponent<StorageSlotView>() != null
            || image.GetComponent<EquipmentSlotUI>() != null;

        if (isSlot)
        {
            return ImageRole.Slot;
        }

        if (image.GetComponent<Selectable>() != null)
        {
            return ImageRole.Button;
        }

        // 스크롤 목록 배경은 패널 안쪽에 파인 듯한 어두운 영역으로 표시
        if (image.GetComponent<ScrollRect>() != null)
        {
            return ImageRole.Inset;
        }

        RectTransform rect = image.rectTransform;
        bool stretched = rect.anchorMin == Vector2.zero && rect.anchorMax == Vector2.one && rect.sizeDelta.sqrMagnitude < 1f;
        bool parentIsCanvasOrRoot = rect.parent == null
            || rect.parent.GetComponent<Canvas>() != null
            || rect.parent.parent == null;

        if (stretched && parentIsCanvasOrRoot)
        {
            return ImageRole.Overlay;
        }

        return ImageRole.Panel;
    }

    private static bool IsThemeReplaceable(Sprite sprite)
    {
        string path = AssetDatabase.GetAssetPath(sprite);
        return path.StartsWith("Resources/unity_builtin_extra")
            || path == "Library/unity default resources"
            || path.StartsWith(UISpriteFactory.SpriteFolder);
    }

    private static void StyleImage(Image image, ImageRole role)
    {
        Record(image);

        switch (role)
        {
            case ImageRole.Overlay:
                image.sprite = null;
                image.type = Image.Type.Simple;
                image.color = ProjectUUIPalette.Overlay;
                break;
            case ImageRole.Panel:
            {
                int depth = CountPanelAncestors(image.transform);
                image.sprite = UISpriteFactory.Panel;
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 1f;
                image.color = depth == 0 ? ProjectUUIPalette.PanelDark : depth == 1 ? ProjectUUIPalette.PanelMid : ProjectUUIPalette.PanelLight;
                break;
            }
            case ImageRole.Button:
                image.sprite = UISpriteFactory.Button;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
                break;
            case ImageRole.Slot:
                image.sprite = UISpriteFactory.Slot;
                image.type = Image.Type.Sliced;
                image.color = image.GetComponent<Selectable>() != null ? Color.white : ProjectUUIPalette.Slot;
                break;
            case ImageRole.BarFill:
                image.sprite = BarFillSprite;
                image.type = Image.Type.Filled;
                image.fillMethod = Image.FillMethod.Horizontal;
                image.fillOrigin = (int)Image.OriginHorizontal.Left;
                break;
            case ImageRole.ScrollTrack:
                image.sprite = UISpriteFactory.Pill;
                image.type = Image.Type.Sliced;
                image.color = ProjectUUIPalette.BarBackground;
                break;
            case ImageRole.ScrollHandle:
                image.sprite = UISpriteFactory.Pill;
                image.type = Image.Type.Sliced;
                image.color = new Color(0.55f, 0.58f, 0.63f, 0.9f);
                break;
            case ImageRole.ToggleBox:
                image.sprite = UISpriteFactory.Slot;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
                break;
            case ImageRole.Accent:
                image.sprite = UISpriteFactory.Pill;
                image.type = Image.Type.Sliced;
                image.color = ProjectUUIPalette.Accent;
                break;
            case ImageRole.Clear:
                image.color = new Color(1f, 1f, 1f, 0f);
                break;
            case ImageRole.Inset:
                image.sprite = UISpriteFactory.Panel;
                image.type = Image.Type.Sliced;
                image.color = new Color(0f, 0f, 0f, 0.22f);
                break;
        }

        EditorUtility.SetDirty(image);
    }

    private static int CountPanelAncestors(Transform target)
    {
        int depth = 0;
        Transform current = target.parent;

        while (current != null)
        {
            if (current.GetComponent<Canvas>() != null && current.GetComponent<Canvas>().isRootCanvas)
            {
                break;
            }

            Image image = current.GetComponent<Image>();

            if (image != null && image.enabled && image.color.a > 0.05f && image.GetComponent<Selectable>() == null)
            {
                depth++;
            }

            current = current.parent;
        }

        return depth;
    }

    private static void StyleSelectable(Selectable selectable)
    {
        Record(selectable);
        selectable.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = selectable.colors;
        bool isSlot = selectable.GetComponent<EquipmentSlotUI>() != null || selectable.name.Contains("Slot");

        colors.normalColor = isSlot ? ProjectUUIPalette.Slot : ProjectUUIPalette.ButtonNormal;
        colors.highlightedColor = isSlot ? new Color(0.22f, 0.25f, 0.31f, 1f) : ProjectUUIPalette.ButtonHighlight;
        colors.pressedColor = ProjectUUIPalette.ButtonPressed;
        colors.selectedColor = isSlot ? new Color(0.24f, 0.22f, 0.17f, 1f) : ProjectUUIPalette.ButtonHighlight;
        colors.disabledColor = ProjectUUIPalette.ButtonDisabled;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;

        if (selectable is Toggle || selectable is Slider || selectable is Scrollbar)
        {
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            colors.selectedColor = Color.white;
        }

        selectable.colors = colors;
        EditorUtility.SetDirty(selectable);
    }

    private static void StyleText(TMP_Text text, bool isScene)
    {
        Record(text);
        string name = text.name;
        bool inButton = text.GetComponentInParent<Button>(true) != null;

        if (name.Contains("Title") || name == "Header")
        {
            text.color = ProjectUUIPalette.Accent;
            text.fontStyle |= FontStyles.Bold;
            text.characterSpacing = Mathf.Max(text.characterSpacing, 2f);
        }
        else if (inButton)
        {
            text.color = ProjectUUIPalette.TextPrimary;
            text.fontStyle |= FontStyles.Bold;
            text.characterSpacing = Mathf.Max(text.characterSpacing, 1f);
        }
        else if (name.Contains("Status") || name.Contains("Help") || name.Contains("Description") || name.Contains("Hint"))
        {
            text.color = ProjectUUIPalette.TextSecondary;
        }
        else
        {
            text.color = ProjectUUIPalette.TextPrimary;
        }

        if (isScene && hudTextMaterial != null && text.font != null && hudTextMaterial.mainTexture == text.font.material.mainTexture)
        {
            text.fontSharedMaterial = hudTextMaterial;
        }

        EditorUtility.SetDirty(text);
    }

    private static Material GetOrCreateHudTextMaterial()
    {
        TMP_FontAsset font = TMP_Settings.defaultFontAsset;

        if (font == null || font.material == null)
        {
            return null;
        }

        StylizedArtAssetFactory.EnsureFolder(ThemeFolder);
        string path = $"{ThemeFolder}/{font.name} - HUD Shadow.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material == null)
        {
            material = new Material(font.material);
            AssetDatabase.CreateAsset(material, path);
        }

        material.CopyPropertiesFromMaterial(font.material);
        material.shaderKeywords = font.material.shaderKeywords;
        material.EnableKeyword(ShaderUtilities.Keyword_Underlay);
        material.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.75f));
        material.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.55f);
        material.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.55f);
        material.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.25f);
        material.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.35f);
        EditorUtility.SetDirty(material);
        return material;
    }

    // ------------------------------------------------------------ HUD 배치

    private static readonly (string root, string icon, Color color)[] SurvivalBars =
    {
        ("--- HealthBarRoot ---", "Health", ProjectUUIPalette.Health),
        ("--- HungerBarRoot ---", "Hunger", ProjectUUIPalette.Hunger),
        ("--- ThirstBarRoot ---", "Thirst", ProjectUUIPalette.Thirst),
        ("--- WetnessBarRoot ---", "Wetness", ProjectUUIPalette.Wetness),
        ("--- TemperatureBarRoot ---", "Temperature", ProjectUUIPalette.Temperature)
    };

    private static string LayoutSurvivalBars()
    {
        const float rowHeight = 36f;
        const float top = 22f;
        RectTransform firstBar = null;
        int laidOut = 0;

        for (int index = 0; index < SurvivalBars.Length; index++)
        {
            (string rootName, string iconName, Color color) = SurvivalBars[index];
            RectTransform bar = FindRect(rootName);

            if (bar == null)
            {
                continue;
            }

            firstBar ??= bar;
            Record(bar);
            bar.anchorMin = new Vector2(0f, 1f);
            bar.anchorMax = new Vector2(0f, 1f);
            bar.pivot = new Vector2(0f, 1f);
            bar.sizeDelta = new Vector2(236f, 22f);
            bar.anchoredPosition = new Vector2(64f, -(top + index * rowHeight));

            Image background = bar.GetComponent<Image>();

            if (background != null)
            {
                Record(background);
                background.sprite = UISpriteFactory.Pill;
                background.type = Image.Type.Sliced;
                background.color = ProjectUUIPalette.BarBackground;
                background.raycastTarget = false;
            }

            foreach (Transform child in bar)
            {
                Image fill = child.GetComponent<Image>();
                TMP_Text label = child.GetComponent<TMP_Text>();
                RectTransform childRect = (RectTransform)child;

                if (fill != null && child.name.Contains("Fill"))
                {
                    Record(fill);
                    Record(childRect);
                    childRect.anchorMin = Vector2.zero;
                    childRect.anchorMax = Vector2.one;
                    childRect.sizeDelta = new Vector2(-6f, -6f);
                    childRect.anchoredPosition = Vector2.zero;
                    fill.sprite = BarFillSprite;
                    fill.type = Image.Type.Filled;
                    fill.fillMethod = Image.FillMethod.Horizontal;
                    fill.fillOrigin = (int)Image.OriginHorizontal.Left;
                    fill.color = color;
                    fill.raycastTarget = false;
                }

                if (label != null)
                {
                    Record(label);
                    Record(childRect);
                    childRect.anchorMin = Vector2.zero;
                    childRect.anchorMax = Vector2.one;
                    childRect.sizeDelta = new Vector2(-18f, 0f);
                    childRect.anchoredPosition = Vector2.zero;
                    label.fontSize = 13f;
                    label.enableAutoSizing = false;
                    label.fontStyle = FontStyles.Bold;
                    label.alignment = TextAlignmentOptions.MidlineRight;
                    label.color = ProjectUUIPalette.TextPrimary;
                    label.raycastTarget = false;

                    if (hudTextMaterial != null)
                    {
                        label.fontSharedMaterial = hudTextMaterial;
                    }
                }
            }

            Image icon = GetOrCreateImage(bar, GeneratedPrefix + "Icon");
            RectTransform iconRect = icon.rectTransform;
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(24f, 24f);
            iconRect.anchoredPosition = new Vector2(-22f, 0f);
            icon.sprite = UISpriteFactory.Icon(iconName);
            icon.preserveAspect = true;
            icon.color = color;
            icon.raycastTarget = false;
            laidOut++;
        }

        if (firstBar == null)
        {
            return "생존 게이지를 찾지 못함";
        }

        Image panel = GetOrCreateImage(firstBar.parent, GeneratedPrefix + "SurvivalPanel");
        RectTransform panelRect = panel.rectTransform;
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(14f, -10f);
        panelRect.sizeDelta = new Vector2(300f, top + laidOut * rowHeight - 4f);
        panel.sprite = UISpriteFactory.Panel;
        panel.type = Image.Type.Sliced;
        panel.color = ProjectUUIPalette.HudPanel;
        panel.raycastTarget = false;
        panelRect.SetSiblingIndex(firstBar.GetSiblingIndex());
        return $"생존 게이지 {laidOut}개 재배치";
    }

    private static string LayoutDayTime()
    {
        RectTransform root = FindRect("--- DayTimeHUD ---");

        if (root == null)
        {
            return "시간 표시를 찾지 못함";
        }

        Record(root);
        root.anchorMin = Vector2.one;
        root.anchorMax = Vector2.one;
        root.pivot = Vector2.one;
        root.anchoredPosition = new Vector2(-12f, -284f);
        root.sizeDelta = new Vector2(260f, 114f);

        Image background = root.GetComponent<Image>();

        if (background != null)
        {
            Record(background);
            background.sprite = UISpriteFactory.Panel;
            background.type = Image.Type.Sliced;
            background.color = ProjectUUIPalette.HudPanel;
            background.raycastTarget = false;
        }

        (string name, float top, float height, float size, Color color, float left)[] lines =
        {
            ("DayTimeText", 10f, 30f, 22f, ProjectUUIPalette.TextPrimary, 44f),
            ("SeasonText", 42f, 22f, 15f, ProjectUUIPalette.TextSecondary, 18f),
            ("WeatherText", 64f, 22f, 15f, ProjectUUIPalette.TextSecondary, 18f),
            ("ExposureText", 86f, 20f, 13f, ProjectUUIPalette.Accent, 18f)
        };

        foreach ((string name, float lineTop, float height, float size, Color color, float left) in lines)
        {
            Transform child = root.Find(name);
            TMP_Text text = child != null ? child.GetComponent<TMP_Text>() : null;

            if (text == null)
            {
                continue;
            }

            RectTransform rect = (RectTransform)child;
            Record(rect);
            Record(text);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.offsetMin = new Vector2(left, -(lineTop + height));
            rect.offsetMax = new Vector2(-12f, -lineTop);
            text.enableAutoSizing = false;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.fontStyle = name == "DayTimeText" ? FontStyles.Bold : FontStyles.Normal;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
        }

        Image icon = GetOrCreateImage(root, GeneratedPrefix + "SunIcon");
        RectTransform iconRect = icon.rectTransform;
        iconRect.anchorMin = new Vector2(0f, 1f);
        iconRect.anchorMax = new Vector2(0f, 1f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = new Vector2(26f, -25f);
        iconRect.sizeDelta = new Vector2(24f, 24f);
        icon.sprite = UISpriteFactory.Icon("Sun");
        icon.color = ProjectUUIPalette.Accent;
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        return "시간·날씨 표시 재배치";
    }

    private static string LayoutMinimap()
    {
        RectTransform panel = FindRect("MinimapPanel");

        if (panel == null)
        {
            return "미니맵을 찾지 못함";
        }

        Image background = panel.GetComponent<Image>();

        if (background != null)
        {
            Record(background);
            background.sprite = UISpriteFactory.Panel;
            background.type = Image.Type.Sliced;
            background.color = ProjectUUIPalette.PanelDark;
        }

        Image frame = GetOrCreateImage(panel, GeneratedPrefix + "MinimapFrame");
        RectTransform frameRect = frame.rectTransform;
        frameRect.anchorMin = Vector2.zero;
        frameRect.anchorMax = Vector2.one;
        frameRect.sizeDelta = Vector2.zero;
        frameRect.anchoredPosition = Vector2.zero;
        frame.sprite = UISpriteFactory.PanelOutline;
        frame.type = Image.Type.Sliced;
        frame.color = ProjectUUIPalette.AccentSoft;
        frame.raycastTarget = false;
        frameRect.SetAsLastSibling();

        Transform arrow = panel.Find("PlayerDirectionIcon");

        if (arrow != null && arrow.GetComponent<TMP_Text>() != null)
        {
            TMP_Text arrowText = arrow.GetComponent<TMP_Text>();
            Record(arrowText);
            arrowText.color = ProjectUUIPalette.Accent;
            arrow.SetAsLastSibling();
        }

        TMP_Text north = GetOrCreateText(panel, GeneratedPrefix + "NorthLabel");
        RectTransform northRect = north.rectTransform;
        northRect.anchorMin = new Vector2(0.5f, 1f);
        northRect.anchorMax = new Vector2(0.5f, 1f);
        northRect.pivot = new Vector2(0.5f, 1f);
        northRect.anchoredPosition = new Vector2(0f, -6f);
        northRect.sizeDelta = new Vector2(30f, 22f);
        north.text = "N";
        north.fontSize = 16f;
        north.fontStyle = FontStyles.Bold;
        north.alignment = TextAlignmentOptions.Center;
        north.color = ProjectUUIPalette.Accent;
        north.raycastTarget = false;
        return "미니맵 테두리 적용";
    }

    private static string LayoutPromptAndDebug()
    {
        RectTransform prompt = FindRect("--- InteractionPrompt ---");

        if (prompt != null)
        {
            Record(prompt);
            prompt.sizeDelta = new Vector2(380f, 46f);
            prompt.anchoredPosition = new Vector2(0f, 150f);
            Image background = prompt.GetComponent<Image>();

            if (background != null)
            {
                Record(background);
                background.sprite = UISpriteFactory.Pill;
                background.type = Image.Type.Sliced;
                background.color = new Color(0.05f, 0.06f, 0.08f, 0.85f);
            }

            TMP_Text text = prompt.GetComponentInChildren<TMP_Text>(true);

            if (text != null)
            {
                Record(text);
                text.fontSize = 20f;
                text.fontStyle = FontStyles.Bold;
                text.characterSpacing = 1.5f;
                text.color = ProjectUUIPalette.TextPrimary;
            }

            Image accent = GetOrCreateImage(prompt, GeneratedPrefix + "PromptAccent");
            RectTransform accentRect = accent.rectTransform;
            accentRect.anchorMin = new Vector2(0f, 0f);
            accentRect.anchorMax = new Vector2(1f, 0f);
            accentRect.pivot = new Vector2(0.5f, 0f);
            accentRect.sizeDelta = new Vector2(-40f, 3f);
            accentRect.anchoredPosition = new Vector2(0f, 2f);
            accent.sprite = UISpriteFactory.Pill;
            accent.type = Image.Type.Sliced;
            accent.color = ProjectUUIPalette.Accent;
            accent.raycastTarget = false;
        }

        RectTransform state = FindRect("StateText");

        if (state != null && state.GetComponent<TMP_Text>() != null)
        {
            TMP_Text text = state.GetComponent<TMP_Text>();
            Record(state);
            Record(text);
            state.anchorMin = Vector2.zero;
            state.anchorMax = Vector2.zero;
            state.pivot = Vector2.zero;
            state.anchoredPosition = new Vector2(16f, 12f);
            state.sizeDelta = new Vector2(520f, 24f);
            text.fontSize = 13f;
            text.enableAutoSizing = false;
            text.alignment = TextAlignmentOptions.BottomLeft;
            text.color = new Color(ProjectUUIPalette.TextSecondary.r, ProjectUUIPalette.TextSecondary.g, ProjectUUIPalette.TextSecondary.b, 0.55f);
        }

        return "상호작용 안내·상태 문구 정리";
    }

    private static string LayoutNearbyLoot()
    {
        RectTransform panel = FindRect("NearbyLootPanel");

        if (panel == null)
        {
            return "근처 아이템 패널 없음";
        }

        Image background = panel.GetComponent<Image>();

        if (background != null)
        {
            Record(background);
            background.sprite = UISpriteFactory.Panel;
            background.type = Image.Type.Sliced;
            background.color = new Color(0.075f, 0.09f, 0.115f, 0.82f);
        }

        Transform scroll = panel.Find("NearbyLootScrollView");
        Image scrollImage = scroll != null ? scroll.GetComponent<Image>() : null;

        if (scrollImage != null)
        {
            Record(scrollImage);
            scrollImage.color = new Color(0f, 0f, 0f, 0.18f);
        }

        return "근처 아이템 패널 스타일 적용";
    }

    private static string LayoutDeathScreen()
    {
        RectTransform panel = FindRect("DeathPanel");

        if (panel == null)
        {
            return "사망 화면 없음";
        }

        Image background = panel.GetComponent<Image>();

        if (background != null)
        {
            Record(background);
            background.sprite = null;
            background.type = Image.Type.Simple;
            background.color = new Color(0.08f, 0.02f, 0.02f, 0.72f);
        }

        Image vignette = GetOrCreateImage(panel, GeneratedPrefix + "Vignette");
        RectTransform vignetteRect = vignette.rectTransform;
        vignetteRect.anchorMin = Vector2.zero;
        vignetteRect.anchorMax = Vector2.one;
        vignetteRect.sizeDelta = Vector2.zero;
        vignetteRect.anchoredPosition = Vector2.zero;
        vignette.sprite = UISpriteFactory.Vignette;
        vignette.color = new Color(0f, 0f, 0f, 0.9f);
        vignette.raycastTarget = false;
        vignetteRect.SetAsFirstSibling();

        Transform title = panel.Find("DeathTitle");

        if (title != null && title.GetComponent<TMP_Text>() != null)
        {
            TMP_Text text = title.GetComponent<TMP_Text>();
            Record(text);
            text.color = ProjectUUIPalette.Danger;
            text.fontSize = 78f;
            text.fontStyle = FontStyles.Bold;
            text.characterSpacing = 12f;
        }

        Transform retry = panel.Find("RetryButton");

        if (retry != null)
        {
            Button button = retry.GetComponent<Button>();
            Image image = retry.GetComponent<Image>();

            if (button != null && image != null)
            {
                Record(button);
                Record(image);
                image.sprite = UISpriteFactory.Button;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
                ColorBlock colors = button.colors;
                colors.normalColor = ProjectUUIPalette.Accent;
                colors.highlightedColor = new Color(1f, 0.82f, 0.45f, 1f);
                colors.selectedColor = colors.highlightedColor;
                colors.pressedColor = new Color(0.8f, 0.58f, 0.22f, 1f);
                button.colors = colors;
            }

            TMP_Text label = retry.GetComponentInChildren<TMP_Text>(true);

            if (label != null)
            {
                Record(label);
                label.color = ProjectUUIPalette.TextDark;
                label.fontStyle = FontStyles.Bold;
                label.characterSpacing = 4f;
            }
        }

        return "사망 화면 스타일 적용";
    }

    private static string LayoutHotbarAndBuild()
    {
        RectTransform hotbar = FindRect("HotbarPanel");

        if (hotbar != null)
        {
            Image background = hotbar.GetComponent<Image>();

            if (background != null)
            {
                Record(background);
                background.sprite = UISpriteFactory.Panel;
                background.type = Image.Type.Sliced;
                background.color = new Color(0.05f, 0.06f, 0.08f, 0.72f);
            }

            foreach (TMP_Text text in hotbar.GetComponentsInChildren<TMP_Text>(true))
            {
                Record(text);

                if (text.name.Contains("Quantity"))
                {
                    text.color = ProjectUUIPalette.Accent;
                    text.fontStyle = FontStyles.Bold;
                }
                else if (text.name.Contains("Shortcut") || text.name.Contains("ItemName"))
                {
                    text.color = ProjectUUIPalette.TextSecondary;
                }
            }
        }

        RectTransform build = FindRect("BuildPanel");

        if (build != null)
        {
            Image background = build.GetComponent<Image>();

            if (background != null)
            {
                Record(background);
                background.sprite = UISpriteFactory.Panel;
                background.type = Image.Type.Sliced;
                background.color = ProjectUUIPalette.PanelDark;
            }

            Transform status = build.Find("BuildStatusText");

            if (status != null && status.GetComponent<TMP_Text>() != null)
            {
                Record(status.GetComponent<TMP_Text>());
                status.GetComponent<TMP_Text>().color = ProjectUUIPalette.Accent;
            }
        }

        return "핫바·건축 패널 스타일 적용";
    }

    // ------------------------------------------------------------ 도우미

    private static RectTransform FindRect(string name)
    {
        Scene scene = SceneManager.GetActiveScene();

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
            {
                if (rect.name == name)
                {
                    return rect;
                }
            }
        }

        return null;
    }

    private static Image GetOrCreateImage(Transform parent, string name)
    {
        Transform existing = parent.Find(name);

        if (existing != null && existing.GetComponent<Image>() != null)
        {
            Record(existing);
            Record(existing.GetComponent<Image>());
            return existing.GetComponent<Image>();
        }

        GameObject created = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        created.layer = parent.gameObject.layer;

        if (useUndo)
        {
            Undo.RegisterCreatedObjectUndo(created, "Create Theme UI");
        }

        created.transform.SetParent(parent, false);
        return created.GetComponent<Image>();
    }

    private static TMP_Text GetOrCreateText(Transform parent, string name)
    {
        Transform existing = parent.Find(name);

        if (existing != null && existing.GetComponent<TMP_Text>() != null)
        {
            Record(existing.GetComponent<TMP_Text>());
            return existing.GetComponent<TMP_Text>();
        }

        GameObject created = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        created.layer = parent.gameObject.layer;

        if (useUndo)
        {
            Undo.RegisterCreatedObjectUndo(created, "Create Theme UI");
        }

        created.transform.SetParent(parent, false);
        TMP_Text text = created.GetComponent<TMP_Text>();

        if (hudTextMaterial != null)
        {
            text.fontSharedMaterial = hudTextMaterial;
        }

        return text;
    }

    private static void Record(Object target)
    {
        if (useUndo && target != null)
        {
            Undo.RecordObject(target, "Apply UI Theme");
        }
    }

    private static void SetColor(SerializedObject serialized, string property, Color color)
    {
        SerializedProperty found = serialized.FindProperty(property);

        if (found != null)
        {
            found.colorValue = color;
        }
    }

    private static void ApplySerialized(SerializedObject serialized)
    {
        if (useUndo)
        {
            serialized.ApplyModifiedProperties();
        }
        else
        {
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
