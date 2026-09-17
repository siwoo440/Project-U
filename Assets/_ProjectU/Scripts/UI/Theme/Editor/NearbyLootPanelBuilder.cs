using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 82일차 추가: 근처 아이템 HUD를 다시 만든다.
// 오른쪽 열(미니맵·시간 패널 아래)에 같은 너비로 붙고, 줄 수에 맞춰 높이가 늘어난다.
// 이미지는 모두 LP_ 이름이라 UI 테마 도구를 다시 실행해도 덮어쓰지 않는다.
public static class NearbyLootPanelBuilder
{
    private const string MenuPath = "Tools/Project U/Art & UI/6. Rebuild Nearby Items Panel";
    private const string EntryPrefabPath = "Assets/_ProjectU/Prefabs/UI/Day75/PF_UI_NearbyLootEntry.prefab";
    private const string PanelName = "NearbyLootPanel";
    private const string ThemeFolder = "Assets/_ProjectU/UI/Themes";

    private const float DefaultWidth = 260f;
    private const float ColumnGap = 12f;
    private const float RowHeight = 34f;
    private const int MaxRows = 6;

    private static readonly Color RowColor = new Color(0.14f, 0.165f, 0.205f, 0.78f);
    private static readonly Color BadgeColor = new Color(0.95f, 0.72f, 0.3f, 0.2f);
    private static readonly Color DividerColor = new Color(0.95f, 0.72f, 0.3f, 0.3f);

    private static bool useUndo;

    [MenuItem(MenuPath, false, 25)]
    private static void BuildFromMenu()
    {
        string report = Build(true);
        Debug.Log("[Project U] 근처 아이템 UI\n" + report);
        EditorUtility.DisplayDialog("근처 아이템 UI", report, "확인");
    }

    public static string Build(bool withUndo)
    {
        if (UISpriteFactory.Panel == null || UISpriteFactory.Slot == null)
        {
            UISpriteFactory.GenerateAll();
        }
        else if (UISpriteFactory.Icon("Pickup") == null)
        {
            UISpriteFactory.GeneratePickupIcon();
        }

        StringBuilder report = new StringBuilder();
        report.AppendLine(BuildEntryPrefab());
        report.Append(BuildScenePanel(withUndo));
        return report.ToString();
    }

    // ------------------------------------------------------------ 한 줄 Prefab

    private static string BuildEntryPrefab()
    {
        useUndo = false;
        GameObject root = PrefabUtility.LoadPrefabContents(EntryPrefabPath);

        try
        {
            for (int index = root.transform.childCount - 1; index >= 0; index--)
            {
                Object.DestroyImmediate(root.transform.GetChild(index).gameObject);
            }

            RemoveComponent<LayoutGroup>(root);
            RemoveComponent<ContentSizeFitter>(root);
            RemoveComponent<Image>(root);

            RectTransform rect = (RectTransform)root.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, RowHeight);

            LayoutElement layout = GetOrAdd<LayoutElement>(root);
            layout.minHeight = RowHeight;
            layout.preferredHeight = RowHeight;
            layout.flexibleHeight = 0f;

            Image background = CreateImage(root.transform, "LP_RowBackground", UISpriteFactory.Panel, RowColor);
            background.type = Image.Type.Sliced;
            background.pixelsPerUnitMultiplier = 2.2f;
            Stretch(background.rectTransform, 0f, 0f, 0f, 0f);

            Image iconBackground = CreateImage(root.transform, "LP_IconBackground", UISpriteFactory.Slot, Color.white);
            Place(iconBackground.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(5f, 0f), new Vector2(26f, 26f));

            Image icon = CreateImage(iconBackground.transform, "LP_Icon", null, Color.white);
            icon.preserveAspect = true;
            icon.enabled = false;
            Stretch(icon.rectTransform, 3f, 3f, 3f, 3f);

            TMP_Text letter = CreateText(iconBackground.transform, "LP_Letter", "A", 13f, FontStyles.Bold, TextAlignmentOptions.Center, ProjectUUIPalette.TextPrimary, false);
            Stretch(letter.rectTransform, 0f, 0f, 0f, 0f);

            TMP_Text itemName = CreateText(root.transform, "LP_Name", "ITEM NAME", 13f, FontStyles.Normal, TextAlignmentOptions.BottomLeft, ProjectUUIPalette.TextPrimary, false);
            SetOffsets(itemName.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(38f, -1f), new Vector2(-54f, -3f));

            TMP_Text category = CreateText(root.transform, "LP_Category", "MATERIAL", 8.5f, FontStyles.Bold, TextAlignmentOptions.TopLeft, ProjectUUIPalette.TextSecondary, false);
            category.characterSpacing = 3f;
            SetOffsets(category.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(38f, 3f), new Vector2(-54f, -1f));

            TMP_Text quantity = CreateText(root.transform, "LP_Quantity", "x1", 13f, FontStyles.Bold, TextAlignmentOptions.BottomRight, ProjectUUIPalette.Accent, false);
            SetOffsets(quantity.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 1f), new Vector2(-52f, -1f), new Vector2(-8f, -3f));

            TMP_Text distance = CreateText(root.transform, "LP_Distance", "0.0m", 10f, FontStyles.Normal, TextAlignmentOptions.TopRight, ProjectUUIPalette.TextSecondary, false);
            SetOffsets(distance.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0.5f), new Vector2(-52f, 3f), new Vector2(-8f, -1f));

            NearbyLootUIEntry entry = GetOrAdd<NearbyLootUIEntry>(root);
            SerializedObject serialized = new SerializedObject(entry);
            serialized.FindProperty("iconImage").objectReferenceValue = icon;
            serialized.FindProperty("iconBackground").objectReferenceValue = iconBackground;
            serialized.FindProperty("iconLetterText").objectReferenceValue = letter;
            serialized.FindProperty("itemNameText").objectReferenceValue = itemName;
            serialized.FindProperty("categoryText").objectReferenceValue = category;
            serialized.FindProperty("quantityText").objectReferenceValue = quantity;
            serialized.FindProperty("distanceText").objectReferenceValue = distance;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, EntryPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        return "근처 아이템 한 줄 Prefab 재구성 (분류 색 아이콘 · 이름/분류 · 수량/거리)";
    }

    // ------------------------------------------------------------ HUD 패널

    public static string BuildScenePanel(bool withUndo)
    {
        useUndo = withUndo;
        NearbyLootUI ui = Object.FindFirstObjectByType<NearbyLootUI>(FindObjectsInactive.Include);

        if (ui == null)
        {
            return "[경고] Scene에 NearbyLootUI가 없습니다. 게임 Scene(20_Gameplay)을 연 뒤 다시 실행하세요.";
        }

        SerializedObject serialized = new SerializedObject(ui);
        GameObject panelObject = serialized.FindProperty("panelRoot").objectReferenceValue as GameObject;

        if (panelObject == null)
        {
            RectTransform found = FindRect(PanelName);
            panelObject = found != null ? found.gameObject : null;
        }

        if (panelObject == null)
        {
            return "[경고] NearbyLootPanel을 찾지 못했습니다.";
        }

        Transform panelTransform = panelObject.transform;

        for (int index = panelTransform.childCount - 1; index >= 0; index--)
        {
            GameObject child = panelTransform.GetChild(index).gameObject;

            if (useUndo)
            {
                Undo.DestroyObjectImmediate(child);
            }
            else
            {
                Object.DestroyImmediate(child);
            }
        }

        RemoveComponent<ScrollRect>(panelObject);
        RemoveComponent<Mask>(panelObject);

        // 오른쪽 열 : 시간 패널 바로 아래, 같은 너비
        RectTransform panel = (RectTransform)panelTransform;
        Record(panel);
        Vector2 position = new Vector2(-ColumnGap, -410f);
        float width = DefaultWidth;
        RectTransform dayTime = FindRect("--- DayTimeHUD ---");

        if (dayTime != null && dayTime.anchorMin == Vector2.one && dayTime.anchorMax == Vector2.one)
        {
            Vector2 topRight = dayTime.anchoredPosition + new Vector2((1f - dayTime.pivot.x) * dayTime.sizeDelta.x, (1f - dayTime.pivot.y) * dayTime.sizeDelta.y);
            position = new Vector2(topRight.x, topRight.y - dayTime.sizeDelta.y - ColumnGap);
            width = dayTime.sizeDelta.x;
        }

        panel.anchorMin = Vector2.one;
        panel.anchorMax = Vector2.one;
        panel.pivot = Vector2.one;
        panel.anchoredPosition = position;
        panel.sizeDelta = new Vector2(width, 0f);
        panel.localScale = Vector3.one;

        Image background = GetOrAdd<Image>(panelObject);
        Record(background);
        background.sprite = UISpriteFactory.Panel;
        background.type = Image.Type.Sliced;
        background.pixelsPerUnitMultiplier = 1f;
        background.color = ProjectUUIPalette.HudPanel;
        background.raycastTarget = false;

        VerticalLayoutGroup group = GetOrAdd<VerticalLayoutGroup>(panelObject);
        Record(group);
        ConfigureColumn(group, 5f);
        group.padding = new RectOffset(10, 10, 8, 10);

        ContentSizeFitter fitter = GetOrAdd<ContentSizeFitter>(panelObject);
        Record(fitter);
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Material hudMaterial = LoadHudTextMaterial();

        // 머리글 : 줍기 아이콘 · 제목 · 종류 수 배지
        RectTransform header = CreateRect(panelTransform, "LP_Header");
        SetRowHeight(header.gameObject, 20f);

        Image headerIcon = CreateImage(header, "LP_HeaderIcon", UISpriteFactory.Icon("Pickup"), ProjectUUIPalette.Accent);
        Place(headerIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(1f, 0f), new Vector2(15f, 15f));

        TMP_Text title = CreateText(header, "LP_HeaderTitle", "NEARBY ITEMS", 12f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, ProjectUUIPalette.Accent, true, hudMaterial);
        title.characterSpacing = 3f;
        SetOffsets(title.rectTransform, Vector2.zero, Vector2.one, new Vector2(22f, 0f), new Vector2(-36f, 0f));

        Image badge = CreateImage(header, "LP_CountBadge", UISpriteFactory.Pill, BadgeColor);
        badge.type = Image.Type.Sliced;
        badge.pixelsPerUnitMultiplier = 2f;
        Place(badge.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(30f, 16f));

        TMP_Text count = CreateText(badge.transform, "LP_Count", "0", 10.5f, FontStyles.Bold, TextAlignmentOptions.Center, ProjectUUIPalette.Accent, false);
        Stretch(count.rectTransform, 0f, 0f, 0f, 0f);

        Image divider = CreateImage(panelTransform, "LP_Divider", null, DividerColor);
        SetRowHeight(divider.gameObject, 1f);

        // 목록 (NearbyLootUI가 Entry를 이 아래에 만든다)
        RectTransform list = CreateRect(panelTransform, "LP_List");
        VerticalLayoutGroup listGroup = list.gameObject.AddComponent<VerticalLayoutGroup>();
        ConfigureColumn(listGroup, 3f);

        TMP_Text more = CreateText(panelTransform, "LP_MoreHint", "+0 MORE", 10f, FontStyles.Bold, TextAlignmentOptions.Center, ProjectUUIPalette.TextSecondary, true, hudMaterial);
        more.characterSpacing = 2f;
        SetRowHeight(more.gameObject, 13f);
        more.gameObject.SetActive(false);

        GameObject entryPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EntryPrefabPath);
        serialized.FindProperty("panelRoot").objectReferenceValue = panelObject;
        serialized.FindProperty("contentRoot").objectReferenceValue = list;
        serialized.FindProperty("entryPrefab").objectReferenceValue = entryPrefab != null ? entryPrefab.GetComponent<NearbyLootUIEntry>() : null;
        serialized.FindProperty("countText").objectReferenceValue = count;
        serialized.FindProperty("moreText").objectReferenceValue = more;
        serialized.FindProperty("maximumEntries").intValue = MaxRows;

        if (useUndo)
        {
            serialized.ApplyModifiedProperties();
            Undo.RecordObject(panelObject, "Rebuild Nearby Items Panel");
        }
        else
        {
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // 실행 중에 아이템이 있을 때만 NearbyLootUI가 켠다
        panelObject.SetActive(false);
        EditorSceneManager.MarkSceneDirty(panelObject.scene);
        return $"근처 아이템 패널 재구성 : 오른쪽 열 {position} / 너비 {width} / 최대 {MaxRows}줄 + 나머지 요약\nScene 변경 완료 → Ctrl+S로 저장하세요";
    }

    // ------------------------------------------------------------ 보조

    private static void ConfigureColumn(VerticalLayoutGroup group, float spacing)
    {
        group.spacing = spacing;
        group.childAlignment = TextAnchor.UpperLeft;
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = true;
        group.childForceExpandHeight = false;
        group.childScaleWidth = false;
        group.childScaleHeight = false;
    }

    private static void SetRowHeight(GameObject target, float height)
    {
        LayoutElement element = GetOrAdd<LayoutElement>(target);
        element.minHeight = height;
        element.preferredHeight = height;
        element.flexibleHeight = 0f;
    }

    private static RectTransform CreateRect(Transform parent, string name)
    {
        GameObject created = new GameObject(name, typeof(RectTransform));
        Register(created, parent);
        return (RectTransform)created.transform;
    }

    private static Image CreateImage(Transform parent, string name, Sprite sprite, Color color)
    {
        GameObject created = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        Register(created, parent);
        Image image = created.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text CreateText(Transform parent, string name, string value, float size, FontStyles style, TextAlignmentOptions alignment, Color color, bool allowOverflow, Material material = null)
    {
        GameObject created = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        Register(created, parent);
        TMP_Text text = created.GetComponent<TMP_Text>();

        if (TMP_Settings.defaultFontAsset != null)
        {
            text.font = TMP_Settings.defaultFontAsset;
        }

        if (material != null)
        {
            text.fontSharedMaterial = material;
        }

        text.text = value;
        text.fontSize = size;
        text.enableAutoSizing = false;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = allowOverflow ? TextOverflowModes.Overflow : TextOverflowModes.Ellipsis;
        text.margin = Vector4.zero;
        return text;
    }

    private static void Register(GameObject created, Transform parent)
    {
        created.layer = parent.gameObject.layer;

        if (useUndo)
        {
            Undo.RegisterCreatedObjectUndo(created, "Rebuild Nearby Items Panel");
        }

        created.transform.SetParent(parent, false);
    }

    private static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect, float left, float right, float top, float bottom)
    {
        SetOffsets(rect, Vector2.zero, Vector2.one, new Vector2(left, bottom), new Vector2(-right, -top));
    }

    private static void SetOffsets(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();

        if (component != null)
        {
            return component;
        }

        return useUndo ? Undo.AddComponent<T>(target) : target.AddComponent<T>();
    }

    private static void RemoveComponent<T>(GameObject target) where T : Component
    {
        foreach (T component in target.GetComponents<T>())
        {
            if (useUndo)
            {
                Undo.DestroyObjectImmediate(component);
            }
            else
            {
                Object.DestroyImmediate(component);
            }
        }
    }

    private static void Record(Object target)
    {
        if (useUndo && target != null)
        {
            Undo.RecordObject(target, "Rebuild Nearby Items Panel");
        }
    }

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

    private static Material LoadHudTextMaterial()
    {
        TMP_FontAsset font = TMP_Settings.defaultFontAsset;
        return font != null ? AssetDatabase.LoadAssetAtPath<Material>($"{ThemeFolder}/{font.name} - HUD Shadow.mat") : null;
    }
}
