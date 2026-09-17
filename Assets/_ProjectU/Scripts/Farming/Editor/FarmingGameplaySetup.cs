using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 80일차: 밭 설치·심기·물주기 기능을 밭 Prefab과 게임 Scene에 연결한다.
// FarmingContentBuilder의 생성 메뉴에서 함께 실행된다.
public static class FarmingGameplaySetup
{
    private const string RegistryPath = "Assets/_ProjectU/Data/Registry/GameDataRegistry.asset";
    private const string HighlightMaterialPath = "Assets/_ProjectU/Art/Generated/Materials/M_FarmCellHighlight.mat";
    private const string ThemeFolder = "Assets/_ProjectU/UI/Themes";
    private const string ManagerName = "FarmManager";
    private const string HighlightName = "FarmCellHighlight";
    private const string GaugeName = "LP_WaterGauge";
    private const string WellModelName = "LP_prop_well";
    private const string StarterRootName = "=== Day79 Farming Starter ===";

    // ---------------------------------------------------------------- 밭 Prefab

    public static void ConfigurePlotComponent(GameObject root, Transform cropAnchor, GameObject wetSoil)
    {
        FarmPlot plot = root.GetComponent<FarmPlot>();

        if (plot == null)
        {
            plot = root.AddComponent<FarmPlot>();
        }

        SerializedObject serialized = new SerializedObject(plot);
        serialized.FindProperty("promptMessage").stringValue = "FARM PLOT";
        serialized.FindProperty("cropAnchor").objectReferenceValue = cropAnchor;
        serialized.FindProperty("wetSoilVisual").objectReferenceValue = wetSoil;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------------------------------------------------------------- Scene

    public static string SetupScene(FarmingRulesData rules)
    {
        StringBuilder report = new StringBuilder();
        PlayerInteractor interactor = Object.FindFirstObjectByType<PlayerInteractor>(FindObjectsInactive.Include);

        if (interactor == null)
        {
            return "[경고] PlayerInteractor를 찾지 못해 농사 기능을 연결하지 않았습니다.";
        }

        Scene scene = interactor.gameObject.scene;
        FarmManager manager = SetupManager(scene, rules);
        report.AppendLine($"밭 관리자 : {manager.name}");

        FarmTillTarget tillTarget = SetupHighlight(scene, out Renderer highlightRenderer);
        FarmingToolController tools = SetupPlayerTools(interactor, tillTarget, highlightRenderer);
        report.AppendLine($"플레이어 농사 도구 : {tools.gameObject.name}");

        report.AppendLine(SetupWaterSources(scene));
        report.Append(SetupWaterGauge(tools, manager));
        return report.ToString();
    }

    private static FarmManager SetupManager(Scene scene, FarmingRulesData rules)
    {
        FarmManager manager = Object.FindFirstObjectByType<FarmManager>(FindObjectsInactive.Include);

        if (manager == null)
        {
            GameObject created = new GameObject(ManagerName);
            SceneManager.MoveGameObjectToScene(created, scene);
            Undo.RegisterCreatedObjectUndo(created, "Create Farm Manager");
            manager = Undo.AddComponent<FarmManager>(created);
        }

        SerializedObject serialized = new SerializedObject(manager);
        serialized.FindProperty("dayNightCycle").objectReferenceValue = Object.FindFirstObjectByType<DayNightCycle>(FindObjectsInactive.Include);
        serialized.FindProperty("seasonCycle").objectReferenceValue = Object.FindFirstObjectByType<SeasonCycle>(FindObjectsInactive.Include);
        serialized.FindProperty("weatherCycle").objectReferenceValue = Object.FindFirstObjectByType<WeatherCycle>(FindObjectsInactive.Include);
        serialized.FindProperty("gameDataRegistry").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameDataRegistry>(RegistryPath);
        serialized.FindProperty("farmingRules").objectReferenceValue = rules;
        serialized.ApplyModifiedProperties();
        return manager;
    }

    private static FarmTillTarget SetupHighlight(Scene scene, out Renderer highlightRenderer)
    {
        GameObject highlight = FindRoot(scene, HighlightName);

        if (highlight == null)
        {
            highlight = GameObject.CreatePrimitive(PrimitiveType.Quad);
            highlight.name = HighlightName;
            Object.DestroyImmediate(highlight.GetComponent<Collider>());
            SceneManager.MoveGameObjectToScene(highlight, scene);
            Undo.RegisterCreatedObjectUndo(highlight, "Create Farm Cell Highlight");
        }

        highlight.transform.localScale = new Vector3(0.96f, 0.96f, 1f);
        highlightRenderer = highlight.GetComponent<MeshRenderer>();
        Undo.RecordObject(highlightRenderer, "Setup Farm Cell Highlight");
        highlightRenderer.sharedMaterial = GetOrCreateHighlightMaterial();
        highlightRenderer.shadowCastingMode = ShadowCastingMode.Off;
        highlightRenderer.receiveShadows = false;
        highlightRenderer.enabled = false;

        FarmTillTarget target = highlight.GetComponent<FarmTillTarget>();

        if (target == null)
        {
            target = Undo.AddComponent<FarmTillTarget>(highlight);
        }

        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty("promptMessage").stringValue = "F - TILL SOIL";
        serialized.ApplyModifiedProperties();
        return target;
    }

    private static Material GetOrCreateHighlightMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(HighlightMaterialPath);
        bool isNew = material == null;

        if (isNew)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            material = new Material(shader != null ? shader : Shader.Find("Sprites/Default"));
        }

        Color color = new Color(0.45f, 0.95f, 0.45f, 0.35f);
        SetFloat(material, "_Surface", 1f);
        SetFloat(material, "_Blend", 0f);
        SetFloat(material, "_SrcBlend", (float)BlendMode.SrcAlpha);
        SetFloat(material, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        SetFloat(material, "_ZWrite", 0f);
        SetFloat(material, "_Cull", (float)CullMode.Off);

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        material.SetOverrideTag("RenderType", "Transparent");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)RenderQueue.Transparent;
        material.SetShaderPassEnabled("ShadowCaster", false);

        if (isNew)
        {
            StylizedArtAssetFactory.EnsureFolder(StylizedArtAssetFactory.MaterialFolder);
            AssetDatabase.CreateAsset(material, HighlightMaterialPath);
        }
        else
        {
            EditorUtility.SetDirty(material);
        }

        return material;
    }

    private static void SetFloat(Material material, string property, float value)
    {
        if (material.HasProperty(property))
        {
            material.SetFloat(property, value);
        }
    }

    private static FarmingToolController SetupPlayerTools(PlayerInteractor interactor, FarmTillTarget tillTarget, Renderer highlightRenderer)
    {
        GameObject player = interactor.gameObject;
        FarmingToolController tools = player.GetComponent<FarmingToolController>();

        if (tools == null)
        {
            tools = Undo.AddComponent<FarmingToolController>(player);
        }

        SerializedObject interactorSerialized = new SerializedObject(interactor);
        Transform viewTransform = interactorSerialized.FindProperty("viewTransform").objectReferenceValue as Transform;
        BuildPlacementController build = interactorSerialized.FindProperty("buildPlacementController").objectReferenceValue as BuildPlacementController;
        interactorSerialized.FindProperty("farmingToolController").objectReferenceValue = tools;
        interactorSerialized.ApplyModifiedProperties();

        SerializedObject serialized = new SerializedObject(tools);
        serialized.FindProperty("playerInventory").objectReferenceValue = FindOnPlayerOrScene<PlayerInventory>(player);
        serialized.FindProperty("playerStamina").objectReferenceValue = FindOnPlayerOrScene<PlayerStamina>(player);
        serialized.FindProperty("playerHealth").objectReferenceValue = FindOnPlayerOrScene<PlayerHealth>(player);
        serialized.FindProperty("buildPlacementController").objectReferenceValue = build != null ? build : FindOnPlayerOrScene<BuildPlacementController>(player);
        serialized.FindProperty("viewTransform").objectReferenceValue = viewTransform;
        serialized.FindProperty("tillTarget").objectReferenceValue = tillTarget;
        serialized.FindProperty("highlightRenderer").objectReferenceValue = highlightRenderer;
        serialized.ApplyModifiedProperties();
        return tools;
    }

    private static T FindOnPlayerOrScene<T>(GameObject player) where T : Component
    {
        T found = player.GetComponent<T>();
        return found != null ? found : Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
    }

    private static string SetupWaterSources(Scene scene)
    {
        int interactableLayer = LayerMask.NameToLayer("Interactable");
        int count = 0;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == WellModelName && ConfigureWell(child.gameObject, interactableLayer))
                {
                    count++;
                }
            }
        }

        if (count > 0)
        {
            return $"물 공급처(우물) {count}개 연결";
        }

        // 맵 꾸미기에서 우물이 배치되지 않았으면 농사 시작 아이템 옆에 하나 놓는다
        GameObject starter = FindRoot(scene, StarterRootName);
        GameObject prefab = StylizedArtAssetFactory.GetOrCreateModelPrefab("prop_well", false);

        if (starter == null || prefab == null)
        {
            return "[경고] 우물이 없어 물뿌리개를 채울 곳이 없습니다.";
        }

        Transform anchor = starter.transform.Find("Scarecrow");

        if (anchor == null)
        {
            anchor = starter.transform;
        }

        GameObject well = (GameObject)PrefabUtility.InstantiatePrefab(prefab, starter.transform);
        Undo.RegisterCreatedObjectUndo(well, "Place Well");
        well.name = WellModelName;
        well.transform.SetPositionAndRotation(anchor.position + anchor.right * 2.5f, anchor.rotation);
        ConfigureWell(well, interactableLayer);
        return "우물이 없어 농사 시작 아이템 옆에 새로 배치하고 연결";
    }

    private static bool ConfigureWell(GameObject well, int interactableLayer)
    {
        if (well.GetComponentInChildren<Collider>() == null)
        {
            BoxCollider box = Undo.AddComponent<BoxCollider>(well);
            box.center = new Vector3(0f, 0.6f, 0f);
            box.size = new Vector3(1.6f, 1.2f, 1.6f);
        }

        if (well.GetComponent<WaterSource>() == null)
        {
            Undo.AddComponent<WaterSource>(well);
        }

        if (interactableLayer >= 0)
        {
            foreach (Transform child in well.GetComponentsInChildren<Transform>(true))
            {
                Undo.RecordObject(child.gameObject, "Set Well Layer");
                child.gameObject.layer = interactableLayer;
            }
        }

        return true;
    }

    private static string SetupWaterGauge(FarmingToolController tools, FarmManager manager)
    {
        RectTransform hotbar = FindRect("HotbarPanel");

        if (hotbar == null || hotbar.parent == null)
        {
            return "[경고] HotbarPanel을 찾지 못해 물뿌리개 게이지를 만들지 않았습니다.";
        }

        Transform parent = hotbar.parent;
        RectTransform container = GetOrCreateRect(parent, GaugeName);
        container.anchorMin = new Vector2(0.5f, 0f);
        container.anchorMax = new Vector2(0.5f, 0f);
        container.pivot = new Vector2(0.5f, 0f);
        container.anchoredPosition = new Vector2(0f, hotbar.anchoredPosition.y + hotbar.sizeDelta.y + 12f);
        container.sizeDelta = new Vector2(220f, 34f);

        RectTransform panel = GetOrCreateRect(container, "Panel");
        Stretch(panel, Vector2.zero, Vector2.zero);
        Image background = EnsureImage(panel.gameObject);
        background.sprite = UISpriteFactory.Pill;
        background.type = Image.Type.Sliced;
        background.color = ProjectUUIPalette.HudPanel;
        background.raycastTarget = false;

        RectTransform icon = GetOrCreateRect(panel, "Icon");
        icon.anchorMin = new Vector2(0f, 0.5f);
        icon.anchorMax = new Vector2(0f, 0.5f);
        icon.pivot = new Vector2(0f, 0.5f);
        icon.anchoredPosition = new Vector2(10f, 0f);
        icon.sizeDelta = new Vector2(22f, 22f);
        Image iconImage = EnsureImage(icon.gameObject);
        iconImage.sprite = UISpriteFactory.Icon("Thirst");
        iconImage.color = ProjectUUIPalette.Thirst;
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;

        RectTransform barBack = GetOrCreateRect(panel, "Bar");
        barBack.anchorMin = new Vector2(0f, 0.5f);
        barBack.anchorMax = new Vector2(1f, 0.5f);
        barBack.pivot = new Vector2(0f, 0.5f);
        barBack.offsetMin = new Vector2(40f, -5f);
        barBack.offsetMax = new Vector2(-72f, 5f);
        Image barImage = EnsureImage(barBack.gameObject);
        barImage.sprite = UISpriteFactory.Pill;
        barImage.type = Image.Type.Sliced;
        barImage.color = ProjectUUIPalette.BarBackground;
        barImage.raycastTarget = false;

        RectTransform fill = GetOrCreateRect(barBack, "Fill");
        Stretch(fill, new Vector2(1f, 1f), new Vector2(-1f, -1f));
        Image fillImage = EnsureImage(fill.gameObject);
        fillImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UISpriteFactory.SpriteFolder}/UI_BarFill.png");
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.fillAmount = 1f;
        fillImage.color = ProjectUUIPalette.Thirst;
        fillImage.raycastTarget = false;

        RectTransform value = GetOrCreateRect(panel, "Value");
        value.anchorMin = new Vector2(1f, 0f);
        value.anchorMax = new Vector2(1f, 1f);
        value.pivot = new Vector2(1f, 0.5f);
        value.anchoredPosition = new Vector2(-12f, 0f);
        value.sizeDelta = new Vector2(56f, 0f);
        TMP_Text valueText = value.GetComponent<TMP_Text>();

        if (valueText == null)
        {
            valueText = Undo.AddComponent<TextMeshProUGUI>(value.gameObject);
        }

        valueText.fontSize = 17f;
        valueText.fontStyle = FontStyles.Bold;
        valueText.alignment = TextAlignmentOptions.MidlineRight;
        valueText.color = ProjectUUIPalette.TextPrimary;
        valueText.raycastTarget = false;
        valueText.textWrappingMode = TextWrappingModes.NoWrap;
        valueText.text = "10/10";
        Material hudMaterial = FindHudTextMaterial();

        if (hudMaterial != null)
        {
            valueText.fontSharedMaterial = hudMaterial;
        }

        WateringCanGaugeUI gauge = container.GetComponent<WateringCanGaugeUI>();

        if (gauge == null)
        {
            gauge = Undo.AddComponent<WateringCanGaugeUI>(container.gameObject);
        }

        SerializedObject serialized = new SerializedObject(gauge);
        serialized.FindProperty("playerInventory").objectReferenceValue = new SerializedObject(tools).FindProperty("playerInventory").objectReferenceValue;
        serialized.FindProperty("farmManager").objectReferenceValue = manager;
        serialized.FindProperty("gaugeRoot").objectReferenceValue = panel.gameObject;
        serialized.FindProperty("fillImage").objectReferenceValue = fillImage;
        serialized.FindProperty("valueText").objectReferenceValue = valueText;
        serialized.ApplyModifiedProperties();
        panel.gameObject.SetActive(false);
        TidyHotbarNames(hotbar);
        return "물뿌리개 남은 물 게이지 생성 (핫바 위)";
    }

    // 핫바 아이템 이름 칸이 슬롯보다 넓어 긴 이름(씨앗 등)이 옆 칸과 겹치던 문제 정리
    private static void TidyHotbarNames(RectTransform hotbar)
    {
        foreach (TMP_Text text in hotbar.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.name != "ItemNameText")
            {
                continue;
            }

            RectTransform rect = text.rectTransform;
            Undo.RecordObject(rect, "Tidy Hotbar Names");
            Undo.RecordObject(text, "Tidy Hotbar Names");
            rect.sizeDelta = new Vector2(-6f, rect.sizeDelta.y);
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.enableAutoSizing = true;
            text.fontSizeMin = 8f;
            text.fontSizeMax = 12f;
        }
    }

    private static Material FindHudTextMaterial()
    {
        TMP_FontAsset font = TMP_Settings.defaultFontAsset;
        return font != null ? AssetDatabase.LoadAssetAtPath<Material>($"{ThemeFolder}/{font.name} - HUD Shadow.mat") : null;
    }

    private static RectTransform GetOrCreateRect(Transform parent, string name)
    {
        Transform existing = parent.Find(name);

        if (existing != null)
        {
            Undo.RecordObject(existing, "Setup Water Gauge");
            return (RectTransform)existing;
        }

        GameObject created = new GameObject(name, typeof(RectTransform));
        created.layer = parent.gameObject.layer;
        Undo.RegisterCreatedObjectUndo(created, "Setup Water Gauge");
        created.transform.SetParent(parent, false);
        return (RectTransform)created.transform;
    }

    private static Image EnsureImage(GameObject target)
    {
        Image image = target.GetComponent<Image>();

        if (image == null)
        {
            image = Undo.AddComponent<Image>(target);
        }

        Undo.RecordObject(image, "Setup Water Gauge");
        return image;
    }

    private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
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

    private static GameObject FindRoot(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == name)
            {
                return root;
            }
        }

        return null;
    }

    // ---------------------------------------------------------------- 검증

    public static void ValidatePrefab(GameObject placedPlot, System.Action<string> error)
    {
        FarmPlot plot = placedPlot.GetComponent<FarmPlot>();

        if (plot == null)
        {
            error("밭 설치 Prefab에 FarmPlot이 없습니다.");
            return;
        }

        SerializedObject serialized = new SerializedObject(plot);

        if (serialized.FindProperty("cropAnchor").objectReferenceValue == null
            || serialized.FindProperty("wetSoilVisual").objectReferenceValue == null)
        {
            error("FarmPlot의 CropAnchor 또는 WetSoil 연결이 비어 있습니다.");
        }
    }

    public static void ValidateScene(StringBuilder report, System.Action<string> error)
    {
        FarmManager manager = Object.FindFirstObjectByType<FarmManager>(FindObjectsInactive.Include);

        if (manager == null)
        {
            error("Scene에 FarmManager가 없습니다.");
        }
        else
        {
            SerializedObject serialized = new SerializedObject(manager);

            foreach (string property in new[] { "dayNightCycle", "seasonCycle", "weatherCycle", "gameDataRegistry", "farmingRules" })
            {
                if (serialized.FindProperty(property).objectReferenceValue == null)
                {
                    error($"FarmManager.{property} 연결이 비어 있습니다.");
                }
            }
        }

        FarmingToolController tools = Object.FindFirstObjectByType<FarmingToolController>(FindObjectsInactive.Include);

        if (tools == null)
        {
            error("플레이어에 FarmingToolController가 없습니다.");
        }
        else
        {
            SerializedObject serialized = new SerializedObject(tools);

            foreach (string property in new[] { "playerInventory", "playerStamina", "buildPlacementController", "viewTransform", "tillTarget", "highlightRenderer" })
            {
                if (serialized.FindProperty(property).objectReferenceValue == null)
                {
                    error($"FarmingToolController.{property} 연결이 비어 있습니다.");
                }
            }

            PlayerInteractor interactor = tools.GetComponent<PlayerInteractor>();

            if (interactor == null || new SerializedObject(interactor).FindProperty("farmingToolController").objectReferenceValue != tools)
            {
                error("PlayerInteractor에 FarmingToolController가 연결되지 않았습니다.");
            }
        }

        WaterSource[] sources = Object.FindObjectsByType<WaterSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int interactableLayer = LayerMask.NameToLayer("Interactable");
        int usable = 0;

        foreach (WaterSource source in sources)
        {
            Collider collider = source.GetComponentInChildren<Collider>();

            if (collider != null && collider.gameObject.layer == interactableLayer)
            {
                usable++;
            }
        }

        if (usable == 0)
        {
            error("상호작용 가능한 물 공급처(우물)가 없습니다.");
        }

        WateringCanGaugeUI gauge = Object.FindFirstObjectByType<WateringCanGaugeUI>(FindObjectsInactive.Include);

        if (gauge == null)
        {
            error("물뿌리개 게이지 UI가 없습니다.");
        }
        else
        {
            SerializedObject serialized = new SerializedObject(gauge);

            foreach (string property in new[] { "playerInventory", "farmManager", "gaugeRoot", "fillImage", "valueText" })
            {
                if (serialized.FindProperty(property).objectReferenceValue == null)
                {
                    error($"WateringCanGaugeUI.{property} 연결이 비어 있습니다.");
                }
            }
        }

        report.AppendLine($"농사 기능 Scene 연결 확인 (물 공급처 {usable}개)");
    }
}
