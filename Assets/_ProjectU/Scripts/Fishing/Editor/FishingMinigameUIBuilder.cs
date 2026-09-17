using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 83일차: 끌어올리기 미니게임 HUD를 게임 Canvas(상호작용 안내 바로 위)에 만들고
// 플레이어에 미니게임 컨트롤러를 붙여 연결한다. 실행할 때마다 HUD를 새로 만든다.
public static class FishingMinigameUIBuilder
{
    public const string RootName = "LP_FishingMinigameHUD";
    public const int SuccessPipCount = 8;
    public const int MissPipCount = 5;

    private const string PromptName = "--- InteractionPrompt ---";
    private const float Width = 380f;
    private const float Height = 94f;
    private const float Padding = 14f;
    private const float PipSize = 10f;
    private const float PipStep = 14f;

    private static readonly Color PanelColor = new Color(0.075f, 0.09f, 0.115f, 0.92f);
    private static readonly Color TrackColor = new Color(0.03f, 0.035f, 0.05f, 0.9f);
    private static readonly Color TimeBackColor = new Color(1f, 1f, 1f, 0.08f);
    private static readonly Color EmptyPipColor = new Color(1f, 1f, 1f, 0.16f);

    public static string Build(FishingController controller)
    {
        if (UISpriteFactory.Panel == null || UISpriteFactory.Pill == null || UISpriteFactory.CircleSprite == null)
        {
            UISpriteFactory.GenerateAll();
        }

        RectTransform prompt = FindRect(PromptName);

        if (prompt == null || prompt.parent == null)
        {
            return "[경고] 상호작용 안내 UI를 찾지 못해 미니게임 HUD를 만들지 못했습니다.";
        }

        Transform canvas = prompt.parent;
        Transform existing = canvas.Find(RootName);

        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        // 바깥 틀 : 항상 켜져 있고 FishingMinigameUI를 가진다
        GameObject rootObject = new GameObject(RootName, typeof(RectTransform));
        rootObject.layer = canvas.gameObject.layer;
        rootObject.transform.SetParent(canvas, false);
        Undo.RegisterCreatedObjectUndo(rootObject, "Create Fishing Minigame HUD");
        rootObject.transform.SetSiblingIndex(prompt.GetSiblingIndex() + 1);
        RectTransform root = (RectTransform)rootObject.transform;
        root.anchorMin = new Vector2(0.5f, 0f);
        root.anchorMax = new Vector2(0.5f, 0f);
        root.pivot = new Vector2(0.5f, 0f);
        root.anchoredPosition = new Vector2(0f, prompt.anchoredPosition.y + prompt.sizeDelta.y + 10f);
        root.sizeDelta = new Vector2(Width, Height);

        Image panel = CreateImage(root, "LP_Panel", UISpriteFactory.Panel, PanelColor);
        panel.type = Image.Type.Sliced;
        SetOffsets(panel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        TMP_Text fishName = CreateText(panel.transform, "LP_FishName", "???", 15f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, ProjectUUIPalette.TextPrimary);
        SetOffsets(fishName.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(Padding, -30f), new Vector2(-110f, -8f));

        TMP_Text rarity = CreateText(panel.transform, "LP_Rarity", "COMMON", 10f, FontStyles.Bold, TextAlignmentOptions.MidlineRight, ProjectUUIPalette.TextSecondary);
        rarity.characterSpacing = 2f;
        SetOffsets(rarity.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-110f, -30f), new Vector2(-Padding, -8f));

        // 막대 : 목표 구간과 왕복 표시
        Image track = CreateImage(panel.transform, "LP_Track", UISpriteFactory.Pill, TrackColor);
        track.type = Image.Type.Sliced;
        track.pixelsPerUnitMultiplier = 1.5f;
        SetOffsets(track.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(Padding, -58f), new Vector2(-Padding, -36f));

        Image zone = CreateImage(track.transform, "LP_Zone", UISpriteFactory.Pill, ProjectUUIPalette.Teal);
        zone.type = Image.Type.Sliced;
        zone.pixelsPerUnitMultiplier = 2f;
        SetOffsets(zone.rectTransform, new Vector2(0.4f, 0f), new Vector2(0.6f, 1f), new Vector2(0f, 3f), new Vector2(0f, -3f));

        Image marker = CreateImage(track.transform, "LP_Marker", UISpriteFactory.Pill, ProjectUUIPalette.TextPrimary);
        marker.type = Image.Type.Sliced;
        marker.pixelsPerUnitMultiplier = 5f;
        Place(marker.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(6f, 32f));

        // 남은 시간
        Image timeBack = CreateImage(panel.transform, "LP_TimeBack", null, TimeBackColor);
        SetOffsets(timeBack.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(Padding, -67f), new Vector2(-Padding, -64f));

        Image timeFill = CreateImage(timeBack.transform, "LP_TimeFill", null, ProjectUUIPalette.Accent);
        SetOffsets(timeFill.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // 성공 점(왼쪽) · 안내(가운데) · 실수 점(오른쪽)
        Image[] successPips = CreatePips(panel.transform, "LP_SuccessPips", SuccessPipCount, true);
        Image[] missPips = CreatePips(panel.transform, "LP_MissPips", MissPipCount, false);

        TMP_Text hint = CreateText(panel.transform, "LP_Hint", "F - PULL IN THE ZONE", 10f, FontStyles.Bold, TextAlignmentOptions.Center, ProjectUUIPalette.TextSecondary);
        hint.characterSpacing = 1.5f;
        SetOffsets(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-78f, 6f), new Vector2(78f, 22f));

        panel.gameObject.SetActive(false);

        FishingMinigameUI ui = rootObject.AddComponent<FishingMinigameUI>();
        SerializedObject uiSerialized = new SerializedObject(ui);
        uiSerialized.FindProperty("panelRoot").objectReferenceValue = panel.gameObject;
        uiSerialized.FindProperty("titleText").objectReferenceValue = fishName;
        uiSerialized.FindProperty("rarityText").objectReferenceValue = rarity;
        uiSerialized.FindProperty("zoneRect").objectReferenceValue = zone.rectTransform;
        uiSerialized.FindProperty("zoneImage").objectReferenceValue = zone;
        uiSerialized.FindProperty("markerRect").objectReferenceValue = marker.rectTransform;
        uiSerialized.FindProperty("markerImage").objectReferenceValue = marker;
        uiSerialized.FindProperty("timeFillRect").objectReferenceValue = timeFill.rectTransform;
        uiSerialized.FindProperty("timeFillImage").objectReferenceValue = timeFill;
        AssignArray(uiSerialized.FindProperty("successPips"), successPips);
        AssignArray(uiSerialized.FindProperty("missPips"), missPips);
        uiSerialized.ApplyModifiedPropertiesWithoutUndo();

        // 플레이어 미니게임 컨트롤러
        GameObject player = controller.gameObject;
        FishingMinigameController minigame = player.GetComponent<FishingMinigameController>();

        if (minigame == null)
        {
            minigame = Undo.AddComponent<FishingMinigameController>(player);
        }

        SerializedObject minigameSerialized = new SerializedObject(minigame);
        minigameSerialized.FindProperty("fishingController").objectReferenceValue = controller;
        minigameSerialized.FindProperty("minigameUI").objectReferenceValue = ui;
        minigameSerialized.ApplyModifiedProperties();

        return $"끌어올리기 미니게임 HUD 생성 (상호작용 안내 위 {root.anchoredPosition.y:0}px) · 플레이어 미니게임 연결";
    }

    // ------------------------------------------------------------ 보조

    private static Image[] CreatePips(Transform parent, string name, int count, bool fromLeft)
    {
        GameObject group = new GameObject(name, typeof(RectTransform));
        group.layer = parent.gameObject.layer;
        group.transform.SetParent(parent, false);
        RectTransform groupRect = (RectTransform)group.transform;
        float anchorX = fromLeft ? 0f : 1f;
        Place(groupRect, new Vector2(anchorX, 0f), new Vector2(anchorX, 0f), new Vector2(fromLeft ? Padding : -Padding, 9f), new Vector2(count * PipStep, PipSize));
        Image[] pips = new Image[count];

        for (int index = 0; index < count; index++)
        {
            Image pip = CreateImage(groupRect, $"LP_Pip_{index:00}", UISpriteFactory.CircleSprite, EmptyPipColor);
            float x = fromLeft ? index * PipStep : -index * PipStep;
            Place(pip.rectTransform, new Vector2(anchorX, 0.5f), new Vector2(anchorX, 0.5f), new Vector2(x, 0f), new Vector2(PipSize, PipSize));
            pips[index] = pip;
        }

        return pips;
    }

    private static void AssignArray(SerializedProperty property, Image[] values)
    {
        property.arraySize = values.Length;

        for (int index = 0; index < values.Length; index++)
        {
            property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
        }
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
