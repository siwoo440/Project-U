using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 85·86일차: 코드로 UI를 만드는 도구들이 함께 쓰는 부품 (요리 창·우리 창)
public static class UIBuildKit
{
    public static Color Faint => new Color(1f, 1f, 1f, 0.08f);


    public static CookingChipUI CreateChip(Transform parent, string name, float height, float fontSize, bool fitWidth)
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

    public static void ConfigureLayout(HorizontalLayoutGroup layout, bool controlWidth)
    {
        layout.childControlWidth = controlWidth;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;
    }

    public static Button CreateButton(Transform parent, string name, string text, float fontSize, Color color, Color textColor, out Image image, out TMP_Text label)
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

    public static void SetTint(Button button, float highlight)
    {
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(highlight, highlight, highlight, 1f);
        colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.62f, 0.62f, 0.62f, 0.5f);
        colors.colorMultiplier = 1f;
        button.colors = colors;
        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;
    }

    public static TMP_Text CreateLabel(Transform parent, string name, string value)
    {
        TMP_Text label = CreateText(parent, name, value, 11.5f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, ProjectUUIPalette.TextSecondary);
        label.characterSpacing = 3f;
        return label;
    }

    public static RectTransform CreateRect(Transform parent, string name)
    {
        GameObject created = new GameObject(name, typeof(RectTransform));
        created.layer = parent.gameObject.layer;
        created.transform.SetParent(parent, false);
        Undo.RegisterCreatedObjectUndo(created, "Create UI");
        return (RectTransform)created.transform;
    }

    public static Image CreateImage(Transform parent, string name, Sprite sprite, Color color)
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

    public static TMP_Text CreateText(Transform parent, string name, string value, float size, FontStyles style, TextAlignmentOptions alignment, Color color)
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

    public static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public static void TopLeft(RectTransform rect, Vector2 position, Vector2 size)
    {
        Place(rect, new Vector2(0f, 1f), new Vector2(0f, 1f), position, size);
    }

    public static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    public static void SetOffsets(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    public static void RemoveExisting(Transform parent, string name)
    {
        Transform existing = parent.Find(name);

        while (existing != null)
        {
            Undo.DestroyObjectImmediate(existing.gameObject);
            existing = parent.Find(name);
        }
    }

    public static RectTransform FindChildRect(Transform parent, string name)
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
