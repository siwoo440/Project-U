using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using static UIBuildKit;

// 99일차: 메뉴 Scene 디자인 · 설정 창 도구
// 1. 공용 설정 창 Prefab (화면 모드 · 해상도 · 그래픽 품질 · 수직 동기화 · 프레임 제한 · 볼륨 · 마우스 감도)
// 2. 일시정지 메뉴의 SETTINGS 버튼이 공용 설정 창을 열도록 연결
// 3. 00_Bootstrap : 시작 카메라 · 로딩 화면 (AppRoot 아래라 Scene이 바뀌어도 유지)
// 4. 10_MainMenu : 배경 마을(저폴리 모델)과 천천히 도는 카메라, 게임 UI 스타일 메뉴(새 게임 · 이어하기 · 설정 · 게임 종료), 새 게임 확인 창
// 5. 빌드 Scene 목록 확인
// 두 Scene은 이 도구가 직접 저장한다. 여러 번 실행해도 같은 구성이 된다.
public static class MenuSceneBuilder
{
    private const string DialogTitle = "Project U 메뉴 화면";
    private const string BootstrapPath = "Assets/_ProjectU/Scenes/00_Bootstrap.unity";
    private const string MainMenuPath = "Assets/_ProjectU/Scenes/10_MainMenu.unity";
    private const string GameplayPath = "Assets/_ProjectU/Scenes/20_Gameplay.unity";
    public const string SettingsPrefabPath = "Assets/_ProjectU/Prefabs/UI/Popups/PF_UI_SettingsWindow.prefab";
    private const string PausePrefabPath = "Assets/_ProjectU/Prefabs/UI/Popups/PF_UI_PauseMenu.prefab";
    private const string MenuCanvasName = "MainMenuCanvas";
    private const string BackdropName = "=== Menu Backdrop ===";
    private const string LoadingCanvasName = "LoadingCanvas";
    private const string BootCameraName = "BootCamera";

    private static readonly Color Background = new Color(0.04f, 0.05f, 0.07f, 1f);

    // 배경 마을 : 모델, 위치, 방향, 높이(m)
    private static readonly (string model, Vector3 position, float yaw, float height)[] Backdrop =
    {
        ("build_npc_house", new Vector3(-7.5f, 0f, 4.5f), 150f, 5.5f),
        ("build_npc_cafe", new Vector3(7.5f, 0f, 5.5f), 210f, 5.2f),
        ("build_npc_smithy", new Vector3(0.5f, 0f, 10f), 180f, 5f),
        ("prop_well", new Vector3(0f, 0f, 0.5f), 20f, 2.2f),
        ("build_campfire", new Vector3(3.2f, 0f, -3.5f), 0f, 0.7f),
        ("fx_flame", new Vector3(3.2f, 0.25f, -3.5f), 0f, 0.8f),
        ("char_player", new Vector3(1.8f, 0f, -2.4f), 215f, 1.8f),
        ("npc_mio", new Vector3(4.6f, 0f, -2.1f), 245f, 1.55f),
        ("npc_milky", new Vector3(-2.6f, 0f, -0.8f), 120f, 1.7f),
        ("npc_verona", new Vector3(-5.2f, 0f, -5.4f), 60f, 1.8f),
        ("prop_lantern_post", new Vector3(-1.6f, 0f, -3.2f), 0f, 2.4f),
        ("prop_signpost", new Vector3(5.8f, 0f, -0.6f), 300f, 1.6f),
        ("prop_village_board", new Vector3(-3.8f, 0f, 1.8f), 160f, 2.2f),
        ("prop_barrel", new Vector3(9.6f, 0f, 2.4f), 0f, 1f),
        ("prop_crate", new Vector3(10.4f, 0f, 3.4f), 20f, 0.8f),
        ("prop_woodpile", new Vector3(-10.4f, 0f, 1.6f), 90f, 1f),
        ("build_farm_plot", new Vector3(-7f, 0f, -4f), 0f, 0.25f),
        ("build_farm_plot", new Vector3(-8f, 0f, -4f), 0f, 0.25f),
        ("build_farm_plot", new Vector3(-7f, 0f, -5f), 0f, 0.25f),
        ("build_farm_plot", new Vector3(-8f, 0f, -5f), 0f, 0.25f),
        ("crop_tomato_mature", new Vector3(-7f, 0.2f, -4f), 0f, 0.9f),
        ("crop_pumpkin_mature", new Vector3(-8f, 0.2f, -4f), 40f, 0.6f),
        ("crop_potato_mature", new Vector3(-7f, 0.2f, -5f), 90f, 0.6f),
        ("crop_strawberry_mature", new Vector3(-8f, 0.2f, -5f), 0f, 0.5f),
        ("animal_chicken", new Vector3(-4.2f, 0f, -6.2f), 30f, 0.55f),
        ("animal_cow", new Vector3(-11f, 0f, -6.5f), 70f, 1.5f),
        ("prop_fence", new Vector3(-10f, 0f, -8.6f), 0f, 1f),
        ("prop_fence", new Vector3(-7.6f, 0f, -8.6f), 0f, 1f),
        ("prop_fence", new Vector3(-5.2f, 0f, -8.6f), 0f, 1f),
        ("tree_round", new Vector3(-13f, 0f, -2f), 0f, 7f),
        ("tree_round_b", new Vector3(13.5f, 0f, -1.5f), 60f, 6.5f),
        ("tree_pine", new Vector3(-11f, 0f, 11f), 0f, 8.5f),
        ("tree_autumn", new Vector3(11f, 0f, 12f), 120f, 7f),
        ("tree_pine", new Vector3(16f, 0f, 6f), 30f, 8f),
        ("tree_round", new Vector3(-16f, 0f, 7f), 200f, 6.2f),
        ("tree_pine", new Vector3(4f, 0f, 17f), 80f, 9f),
        ("tree_round_b", new Vector3(-5f, 0f, 16f), 10f, 7f),
        ("rock_large", new Vector3(13f, 0f, -8f), 40f, 1.8f),
        ("bush_berry", new Vector3(-13.5f, 0f, -7.5f), 0f, 1.1f),
        ("bush", new Vector3(8.5f, 0f, -7.5f), 0f, 1f),
        ("flower_patch", new Vector3(0.5f, 0f, -6.5f), 0f, 0.4f),
        ("flower_patch", new Vector3(6f, 0f, -6f), 90f, 0.4f),
        ("mountain", new Vector3(0f, 0f, 55f), 0f, 26f),
        ("mountain_low", new Vector3(-40f, 0f, 38f), 40f, 16f),
        ("mountain", new Vector3(42f, 0f, 36f), 200f, 22f)
    };

    // ---------------------------------------------------------------- 메뉴

    [MenuItem(MarketContentBuilder.BuildMenuRoot + "17. Menu Scenes (Boot + Main Menu + Settings + Loading)", false, 36)]
    private static void BuildAllMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(DialogTitle, "Play 중에는 실행할 수 없습니다.", "확인");
            return;
        }

        bool confirmed = EditorUtility.DisplayDialog(
            DialogTitle,
            "메뉴 화면을 게임 UI 스타일로 다시 만듭니다.\n"
            + "· 공용 설정 창 Prefab (메인 메뉴 · 일시정지 메뉴)\n"
            + "· 00_Bootstrap : 로딩 화면\n"
            + "· 10_MainMenu : 배경 마을 · 새 게임 · 이어하기 · 설정 · 게임 종료\n\n"
            + "00_Bootstrap · 10_MainMenu Scene은 이 도구가 직접 저장합니다.\n"
            + "지금 열린 Scene에 저장하지 않은 변경이 있으면 먼저 저장할지 묻습니다.",
            "실행",
            "취소");

        if (!confirmed || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
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
        StringBuilder report = new StringBuilder("[메뉴 화면 생성]\n");

        if (UISpriteFactory.Panel == null)
        {
            UISpriteFactory.GenerateAll();
        }

        SettingsWindowUI windowPrefab = BuildSettingsPrefab(report);
        LinkPauseMenu(windowPrefab, report);
        EditScene(BootstrapPath, scene => BuildBootstrap(scene, report), report);
        EditScene(MainMenuPath, scene => BuildMainMenu(scene, windowPrefab, report), report);
        CheckBuildScenes(report);
        AssetDatabase.SaveAssets();
        report.Append(Validate(out _));
        return report.ToString();
    }

    private static void EditScene(string path, Action<Scene> edit, StringBuilder report)
    {
        Scene scene = SceneManager.GetSceneByPath(path);
        bool wasLoaded = scene.IsValid() && scene.isLoaded;

        if (!wasLoaded)
        {
            scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        }

        Scene previous = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(scene); // 새 오브젝트 · 조명 설정이 이 Scene에 들어가게

        try
        {
            edit(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            report.AppendLine($"{Path.GetFileNameWithoutExtension(path)} 저장");
        }
        finally
        {
            if (previous.IsValid() && previous != scene && previous.isLoaded)
            {
                SceneManager.SetActiveScene(previous);
            }

            if (!wasLoaded)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }

    private static GameObject FindRoot(Scene scene, string name) => scene.GetRootGameObjects().FirstOrDefault(root => root.name == name);

    private static void RemoveRoots(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects().Where(root => root.name == name).ToArray())
        {
            Object.DestroyImmediate(root);
        }
    }

    // ---------------------------------------------------------------- 공용 UI 부품

    private static Canvas CreateCanvas(Transform parent, string name, int sortingOrder)
    {
        GameObject created = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        created.layer = LayerMask.NameToLayer("UI");

        if (parent != null)
        {
            created.transform.SetParent(parent, false);
        }

        Canvas canvas = created.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        CanvasScaler scaler = created.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static Image Box(Transform parent, string name, Color color, Sprite sprite = null)
    {
        Image image = CreateImage(parent, name, sprite, color);

        if (sprite != null)
        {
            image.type = Image.Type.Sliced;
        }

        return image;
    }

    private static Button MenuButton(Transform parent, string name, string text, bool primary, Vector2 position, Vector2 size, float fontSize = 22f)
    {
        Button button = CreateButton(parent, name, text, fontSize, primary ? ProjectUUIPalette.Accent : ProjectUUIPalette.ButtonNormal, primary ? ProjectUUIPalette.TextDark : ProjectUUIPalette.TextPrimary, out _, out TMP_Text label);
        label.characterSpacing = 4f;
        TopLeft((RectTransform)button.transform, position, size);
        return button;
    }

    private static Slider CreateSlider(Transform parent, string name, Vector2 position, Vector2 size)
    {
        RectTransform root = CreateRect(parent, name);
        TopLeft(root, position, size);
        Image track = Box(root, "LP_Track", ProjectUUIPalette.BarBackground, UISpriteFactory.Pill);
        track.raycastTarget = true;
        SetOffsets(track.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -5f), new Vector2(0f, 5f));
        RectTransform fillArea = CreateRect(root, "LP_FillArea");
        SetOffsets(fillArea, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -5f), new Vector2(0f, 5f));
        Image fill = Box(fillArea, "LP_Fill", ProjectUUIPalette.Accent, UISpriteFactory.Pill);
        SetOffsets(fill.rectTransform, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, Vector2.zero);
        RectTransform handleArea = CreateRect(root, "LP_HandleArea");
        SetOffsets(handleArea, Vector2.zero, Vector2.one, new Vector2(10f, 0f), new Vector2(-10f, 0f));
        Image handle = CreateImage(handleArea, "LP_Handle", UISpriteFactory.CircleSprite, ProjectUUIPalette.TextPrimary);
        handle.raycastTarget = true;
        handle.rectTransform.sizeDelta = new Vector2(20f, 20f);
        Slider slider = root.gameObject.AddComponent<Slider>();
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.direction = Slider.Direction.LeftToRight;
        Navigation navigation = slider.navigation;
        navigation.mode = Navigation.Mode.None;
        slider.navigation = navigation;
        return slider;
    }

    // ---------------------------------------------------------------- 설정 창 Prefab

    private static SettingsWindowUI BuildSettingsPrefab(StringBuilder report)
    {
        StylizedArtAssetFactory.EnsureFolder(Path.GetDirectoryName(SettingsPrefabPath).Replace('\\', '/'));
        bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(SettingsPrefabPath) != null;
        GameObject root = exists ? PrefabUtility.LoadPrefabContents(SettingsPrefabPath) : new GameObject("PF_UI_SettingsWindow", typeof(RectTransform));

        try
        {
            root.layer = LayerMask.NameToLayer("UI");

            for (int index = root.transform.childCount - 1; index >= 0; index--)
            {
                Object.DestroyImmediate(root.transform.GetChild(index).gameObject);
            }

            Stretch((RectTransform)root.transform);
            SettingsWindowUI ui = root.GetComponent<SettingsWindowUI>();

            if (ui == null)
            {
                ui = root.AddComponent<SettingsWindowUI>();
            }

            RectTransform panel = CreateRect(root.transform, "LP_Root");
            Stretch(panel);
            Image dim = Box(panel, "LP_Dim", new Color(0.02f, 0.025f, 0.035f, 0.72f));
            dim.raycastTarget = true; // 뒤쪽 클릭 막기
            Stretch(dim.rectTransform);
            Image window = Box(panel, "LP_Window", ProjectUUIPalette.PanelDark, UISpriteFactory.Panel);
            window.raycastTarget = true;
            Place(window.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700f, 650f));
            RectTransform w = window.rectTransform;

            TMP_Text title = CreateText(w, "LP_Title", "SETTINGS", 30f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, ProjectUUIPalette.Accent);
            title.characterSpacing = 6f;
            TopLeft(title.rectTransform, new Vector2(40f, -26f), new Vector2(620f, 44f));
            Image line = Box(w, "LP_Divider", UIBuildKit.Faint);
            TopLeft(line.rectTransform, new Vector2(40f, -76f), new Vector2(620f, 2f));

            TMP_Text display = CreateLabel(w, "LP_SectionDisplay", "DISPLAY");
            TopLeft(display.rectTransform, new Vector2(40f, -88f), new Vector2(620f, 26f));
            SettingsWindowUI.CycleRow[] rows =
            {
                CycleRow(w, "LP_DisplayMode", "DISPLAY MODE", -116f),
                CycleRow(w, "LP_Resolution", "RESOLUTION", -162f),
                CycleRow(w, "LP_Quality", "GRAPHICS QUALITY", -208f),
                CycleRow(w, "LP_VSync", "VSYNC", -254f),
                CycleRow(w, "LP_FrameLimit", "FRAME LIMIT", -300f)
            };

            TMP_Text audio = CreateLabel(w, "LP_SectionAudio", "AUDIO & CONTROLS");
            TopLeft(audio.rectTransform, new Vector2(40f, -356f), new Vector2(620f, 26f));
            Slider volume = SliderRow(w, "LP_Volume", "MASTER VOLUME", -384f, out TMP_Text volumeValue);
            Slider sensitivity = SliderRow(w, "LP_Sensitivity", "MOUSE SENSITIVITY", -430f, out TMP_Text sensitivityValue);

            TMP_Text status = CreateText(w, "LP_Status", string.Empty, 16f, FontStyles.Bold, TextAlignmentOptions.Center, ProjectUUIPalette.Teal);
            status.characterSpacing = 3f;
            TopLeft(status.rectTransform, new Vector2(40f, -494f), new Vector2(620f, 30f));
            Button defaults = MenuButton(w, "LP_Defaults", "DEFAULTS", false, new Vector2(40f, -556f), new Vector2(180f, 54f), 18f);
            Button apply = MenuButton(w, "LP_Apply", "APPLY", true, new Vector2(260f, -556f), new Vector2(200f, 54f), 18f);
            Button close = MenuButton(w, "LP_Close", "CLOSE", false, new Vector2(480f, -556f), new Vector2(180f, 54f), 18f);

            ui.EditorAssign(panel.gameObject, rows, volume, volumeValue, sensitivity, sensitivityValue, defaults, apply, close, status);
            panel.gameObject.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root, SettingsPrefabPath);
        }
        finally
        {
            if (exists)
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            else
            {
                Object.DestroyImmediate(root);
            }
        }

        report.AppendLine($"공용 설정 창 Prefab : {SettingsPrefabPath}");
        return AssetDatabase.LoadAssetAtPath<GameObject>(SettingsPrefabPath).GetComponent<SettingsWindowUI>();
    }

    private static SettingsWindowUI.CycleRow CycleRow(RectTransform parent, string name, string label, float y)
    {
        RectTransform row = CreateRect(parent, name);
        TopLeft(row, new Vector2(40f, y), new Vector2(620f, 44f));
        TMP_Text title = CreateText(row, "LP_Label", label, 17f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, ProjectUUIPalette.TextSecondary);
        title.characterSpacing = 2f;
        TopLeft(title.rectTransform, Vector2.zero, new Vector2(270f, 44f));
        Button previous = MenuButton(row, "LP_Previous", "<", false, new Vector2(300f, -4f), new Vector2(44f, 36f), 20f);
        TMP_Text value = CreateText(row, "LP_Value", "-", 18f, FontStyles.Bold, TextAlignmentOptions.Center, ProjectUUIPalette.TextPrimary);
        TopLeft(value.rectTransform, new Vector2(348f, 0f), new Vector2(224f, 44f));
        Button next = MenuButton(row, "LP_Next", ">", false, new Vector2(576f, -4f), new Vector2(44f, 36f), 20f);
        return new SettingsWindowUI.CycleRow { previous = previous, next = next, value = value };
    }

    private static Slider SliderRow(RectTransform parent, string name, string label, float y, out TMP_Text value)
    {
        RectTransform row = CreateRect(parent, name);
        TopLeft(row, new Vector2(40f, y), new Vector2(620f, 44f));
        TMP_Text title = CreateText(row, "LP_Label", label, 17f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, ProjectUUIPalette.TextSecondary);
        title.characterSpacing = 2f;
        TopLeft(title.rectTransform, Vector2.zero, new Vector2(270f, 44f));
        Slider slider = CreateSlider(row, "LP_Slider", new Vector2(300f, -12f), new Vector2(236f, 20f));
        value = CreateText(row, "LP_Value", "-", 18f, FontStyles.Bold, TextAlignmentOptions.MidlineRight, ProjectUUIPalette.TextPrimary);
        TopLeft(value.rectTransform, new Vector2(544f, 0f), new Vector2(76f, 44f));
        return slider;
    }

    // ---------------------------------------------------------------- 일시정지 메뉴

    private static void LinkPauseMenu(SettingsWindowUI windowPrefab, StringBuilder report)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PausePrefabPath);

        try
        {
            PauseMenuView view = root.GetComponentInChildren<PauseMenuView>(true);

            if (view == null)
            {
                report.AppendLine("✗ 일시정지 메뉴 Prefab에 PauseMenuView가 없습니다.");
                return;
            }

            SerializedObject serialized = new SerializedObject(view);
            SerializedProperty property = serialized.FindProperty("settingsWindowPrefab");

            if (property.objectReferenceValue != windowPrefab)
            {
                property.objectReferenceValue = windowPrefab;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PausePrefabPath);
                report.AppendLine("일시정지 메뉴 SETTINGS → 공용 설정 창 연결");
            }
            else
            {
                report.AppendLine("일시정지 메뉴 SETTINGS → 공용 설정 창 (변경 없음)");
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // ---------------------------------------------------------------- 00_Bootstrap

    private static void BuildBootstrap(Scene scene, StringBuilder report)
    {
        GameObject appRoot = FindRoot(scene, "AppRoot");
        SceneFlowManager flow = appRoot != null ? appRoot.GetComponent<SceneFlowManager>() : null;

        if (flow == null)
        {
            report.AppendLine("✗ 00_Bootstrap에 AppRoot(SceneFlowManager)가 없습니다.");
            return;
        }

        // 시작 카메라 : 첫 화면이 비지 않게 어두운 배경을 그린다 (메인 메뉴로 넘어가면 사라짐)
        RemoveRoots(scene, BootCameraName);
        GameObject cameraObject = new GameObject(BootCameraName, typeof(Camera), typeof(AudioListener));
        Camera bootCamera = cameraObject.GetComponent<Camera>();
        bootCamera.clearFlags = CameraClearFlags.SolidColor;
        bootCamera.backgroundColor = Background;
        bootCamera.cullingMask = 0;
        bootCamera.depth = -10f;

        // 로딩 화면 : AppRoot 아래 (DontDestroyOnLoad로 계속 유지)
        RemoveExisting(appRoot.transform, LoadingCanvasName);
        Canvas canvas = CreateCanvas(appRoot.transform, LoadingCanvasName, 1000);
        LoadingScreenUI loading = canvas.gameObject.AddComponent<LoadingScreenUI>();
        RectTransform root = CreateRect(canvas.transform, "LP_Root");
        Stretch(root);
        CanvasGroup group = root.gameObject.AddComponent<CanvasGroup>();
        Image background = Box(root, "LP_Background", Background);
        background.raycastTarget = true;
        Stretch(background.rectTransform);
        Image vignette = CreateImage(root, "LP_Vignette", UISpriteFactory.Vignette, new Color(0f, 0f, 0f, 0.6f));
        Stretch(vignette.rectTransform);

        TMP_Text title = CreateText(root, "LP_Title", "PROJECT U", 110f, FontStyles.Bold, TextAlignmentOptions.Center, ProjectUUIPalette.TextPrimary);
        title.characterSpacing = 10f;
        Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 70f), new Vector2(1200f, 140f));
        TMP_Text subtitle = CreateText(root, "LP_Subtitle", "SURVIVE  ·  FARM  ·  BEFRIEND", 20f, FontStyles.Bold, TextAlignmentOptions.Center, ProjectUUIPalette.Accent);
        subtitle.characterSpacing = 8f;
        Place(subtitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -8f), new Vector2(900f, 30f));

        TMP_Text status = CreateText(root, "LP_Status", "LOADING", 18f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, ProjectUUIPalette.TextSecondary);
        status.characterSpacing = 5f;
        Place(status.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 176f), new Vector2(720f, 28f));
        Image bar = Box(root, "LP_Bar", ProjectUUIPalette.BarBackground, UISpriteFactory.Pill);
        Place(bar.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 156f), new Vector2(720f, 10f));
        Image fill = Box(bar.rectTransform, "LP_Fill", ProjectUUIPalette.Accent, UISpriteFactory.Pill);
        SetOffsets(fill.rectTransform, Vector2.zero, new Vector2(0.001f, 1f), Vector2.zero, Vector2.zero);
        TMP_Text tip = CreateText(root, "LP_Tip", string.Empty, 17f, FontStyles.Normal, TextAlignmentOptions.Center, ProjectUUIPalette.TextSecondary);
        Place(tip.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(1200f, 30f));

        loading.EditorAssign(root.gameObject, group, fill.rectTransform, status, tip);
        root.gameObject.SetActive(false);
        flow.EditorAssignLoadingScreen(loading);
        EditorUtility.SetDirty(flow);
        report.AppendLine("00_Bootstrap : 시작 카메라 · 로딩 화면(AppRoot/LoadingCanvas) 연결");
    }

    // ---------------------------------------------------------------- 10_MainMenu

    private static void BuildMainMenu(Scene scene, SettingsWindowUI windowPrefab, StringBuilder report)
    {
        // 배경 마을
        RemoveRoots(scene, BackdropName);
        GameObject backdrop = new GameObject(BackdropName);
        List<string> missing = new List<string>();
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ground.name = "Ground";
        ground.transform.SetParent(backdrop.transform, false);
        ground.transform.localScale = new Vector3(90f, 0.05f, 90f);
        ground.transform.localPosition = new Vector3(0f, -0.05f, 10f);
        ground.GetComponent<MeshRenderer>().sharedMaterial = StylizedArtAssetFactory.GetMaterial(StylizedColor.Grass);
        Object.DestroyImmediate(ground.GetComponent<Collider>());
        GameObject plaza = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        plaza.name = "Plaza";
        plaza.transform.SetParent(backdrop.transform, false);
        plaza.transform.localScale = new Vector3(9f, 0.02f, 9f);
        plaza.transform.localPosition = new Vector3(0.5f, 0f, -1f);
        plaza.GetComponent<MeshRenderer>().sharedMaterial = StylizedArtAssetFactory.GetMaterial(StylizedColor.Dirt);
        Object.DestroyImmediate(plaza.GetComponent<Collider>());
        int placed = 0;

        foreach ((string model, Vector3 position, float yaw, float height) in Backdrop)
        {
            if (PlaceModel(backdrop.transform, model, position, yaw, height, missing) != null)
            {
                placed++;
            }
        }

        report.AppendLine($"10_MainMenu : 배경 마을 모델 {placed}개" + (missing.Count > 0 ? $" (없는 모델 : {string.Join(", ", missing.Distinct())})" : string.Empty));

        // 카메라 · 조명 · 안개
        GameObject cameraObject = FindRoot(scene, "Main Camera");

        if (cameraObject == null)
        {
            cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
        }

        Camera menuCamera = cameraObject.GetComponent<Camera>();
        menuCamera.clearFlags = CameraClearFlags.Skybox;
        menuCamera.fieldOfView = 42f;
        menuCamera.nearClipPlane = 0.3f;
        menuCamera.farClipPlane = 400f;
        MenuBackdropCamera orbit = cameraObject.GetComponent<MenuBackdropCamera>();

        if (orbit == null)
        {
            orbit = cameraObject.AddComponent<MenuBackdropCamera>();
        }

        orbit.EditorAssign(new Vector3(0.5f, 0f, 1f), 21f, 6.5f, 1.8f, 2.2f, 205f);
        GameObject lightObject = FindRoot(scene, "Directional Light");

        if (lightObject != null && lightObject.TryGetComponent(out Light sun))
        {
            sun.color = new Color(1f, 0.94f, 0.84f);
            sun.intensity = 1.2f;
            sun.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(38f, -35f, 0f);
        }

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.72f, 0.82f, 0.9f);
        RenderSettings.fogStartDistance = 40f;
        RenderSettings.fogEndDistance = 140f;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.72f, 0.8f, 0.9f);
        RenderSettings.ambientEquatorColor = new Color(0.62f, 0.66f, 0.6f);
        RenderSettings.ambientGroundColor = new Color(0.32f, 0.3f, 0.26f);

        // 메뉴 UI
        RemoveRoots(scene, MenuCanvasName);
        Canvas canvas = CreateCanvas(null, MenuCanvasName, 10);
        RectTransform root = (RectTransform)canvas.transform;
        Image vignette = CreateImage(root, "LP_Vignette", UISpriteFactory.Vignette, new Color(0f, 0f, 0f, 0.45f));
        Stretch(vignette.rectTransform);
        Image shade = Box(root, "LP_Shade", new Color(0.04f, 0.05f, 0.07f, 0.8f));
        SetOffsets(shade.rectTransform, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(620f, 0f));
        Image edge = Box(root, "LP_ShadeEdge", ProjectUUIPalette.AccentSoft);
        SetOffsets(edge.rectTransform, Vector2.zero, new Vector2(0f, 1f), new Vector2(620f, 0f), new Vector2(623f, 0f));

        TMP_Text title = CreateText(root, "LP_Title", "PROJECT U", 72f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, ProjectUUIPalette.TextPrimary);
        title.characterSpacing = 3f;
        title.overflowMode = TextOverflowModes.Overflow; // 글꼴 · 해상도가 달라도 잘리지 않게
        TopLeft(title.rectTransform, new Vector2(94f, -150f), new Vector2(520f, 110f));
        TMP_Text subtitle = CreateText(root, "LP_Subtitle", "SURVIVE  ·  FARM  ·  BEFRIEND", 18f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, ProjectUUIPalette.Accent);
        subtitle.characterSpacing = 6f;
        TopLeft(subtitle.rectTransform, new Vector2(98f, -262f), new Vector2(500f, 28f));
        Image bar = Box(root, "LP_AccentBar", ProjectUUIPalette.Accent);
        TopLeft(bar.rectTransform, new Vector2(98f, -304f), new Vector2(72f, 4f));

        Button newGame = MenuButton(root, "LP_NewGame", "NEW GAME", true, new Vector2(96f, -380f), new Vector2(400f, 62f));
        Button continueGame = MenuButton(root, "LP_Continue", "CONTINUE", false, new Vector2(96f, -458f), new Vector2(400f, 62f));
        Button settings = MenuButton(root, "LP_Settings", "SETTINGS", false, new Vector2(96f, -536f), new Vector2(400f, 62f));
        Button quit = MenuButton(root, "LP_Quit", "QUIT", false, new Vector2(96f, -614f), new Vector2(400f, 62f));
        TMP_Text saveInfo = CreateText(root, "LP_SaveInfo", "NO SAVED GAME", 17f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, ProjectUUIPalette.TextSecondary);
        saveInfo.characterSpacing = 3f;
        TopLeft(saveInfo.rectTransform, new Vector2(98f, -694f), new Vector2(500f, 30f));
        TMP_Text version = CreateText(root, "LP_Version", "ALPHA", 15f, FontStyles.Bold, TextAlignmentOptions.BottomLeft, ProjectUUIPalette.TextSecondary);
        version.characterSpacing = 3f;
        Place(version.rectTransform, Vector2.zero, Vector2.zero, new Vector2(98f, 46f), new Vector2(400f, 24f));
        TMP_Text hint = CreateText(root, "LP_Hint", "ESC  CLOSE WINDOW", 14f, FontStyles.Bold, TextAlignmentOptions.BottomRight, new Color(1f, 1f, 1f, 0.55f));
        hint.characterSpacing = 3f;
        Place(hint.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-48f, 46f), new Vector2(400f, 24f));

        // 새 게임 확인 창
        RectTransform confirm = CreateRect(root, "LP_Confirm");
        Stretch(confirm);
        Image confirmDim = Box(confirm, "LP_Dim", new Color(0.02f, 0.025f, 0.035f, 0.72f));
        confirmDim.raycastTarget = true;
        Stretch(confirmDim.rectTransform);
        Image confirmWindow = Box(confirm, "LP_Window", ProjectUUIPalette.PanelDark, UISpriteFactory.Panel);
        confirmWindow.raycastTarget = true;
        Place(confirmWindow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 290f));
        TMP_Text confirmTitle = CreateText(confirmWindow.rectTransform, "LP_Title", "START A NEW GAME?", 26f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, ProjectUUIPalette.Accent);
        confirmTitle.characterSpacing = 4f;
        TopLeft(confirmTitle.rectTransform, new Vector2(40f, -28f), new Vector2(540f, 40f));
        TMP_Text confirmText = CreateText(confirmWindow.rectTransform, "LP_Message", string.Empty, 18f, FontStyles.Normal, TextAlignmentOptions.TopLeft, ProjectUUIPalette.TextPrimary);
        confirmText.textWrappingMode = TextWrappingModes.Normal;
        TopLeft(confirmText.rectTransform, new Vector2(40f, -84f), new Vector2(540f, 90f));
        Button yes = MenuButton(confirmWindow.rectTransform, "LP_Yes", "START NEW", true, new Vector2(40f, -202f), new Vector2(260f, 56f), 18f);
        Button no = MenuButton(confirmWindow.rectTransform, "LP_No", "CANCEL", false, new Vector2(320f, -202f), new Vector2(260f, 56f), 18f);
        confirm.gameObject.SetActive(false);

        // 공용 설정 창
        SettingsWindowUI window = ((GameObject)PrefabUtility.InstantiatePrefab(windowPrefab.gameObject, root)).GetComponent<SettingsWindowUI>();
        Stretch((RectTransform)window.transform);
        window.transform.SetAsLastSibling();

        MainMenuController controller = canvas.gameObject.AddComponent<MainMenuController>();
        controller.EditorAssign(newGame, continueGame, settings, quit, saveInfo, version, window, confirm.gameObject, confirmText, yes, no);

        if (Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None).All(system => system.gameObject.scene != scene))
        {
            report.AppendLine("✗ 10_MainMenu에 EventSystem이 없습니다.");
        }

        report.AppendLine("10_MainMenu : 메뉴 UI (새 게임 · 이어하기 · 설정 · 게임 종료 · 새 게임 확인 창 · 설정 창)");
    }

    private static GameObject PlaceModel(Transform parent, string modelId, Vector3 position, float yaw, float height, List<string> missing)
    {
        GameObject prefab = StylizedArtAssetFactory.GetOrCreateModelPrefab(modelId, false);
        MeshFilter filter = prefab != null ? prefab.GetComponent<MeshFilter>() : null;

        if (filter == null || filter.sharedMesh == null)
        {
            missing.Add(modelId);
            return null;
        }

        Bounds bounds = filter.sharedMesh.bounds;
        float scale = height / Mathf.Max(0.01f, bounds.size.y);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.transform.localScale = Vector3.one * scale;
        instance.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        instance.transform.localPosition = position - Vector3.up * (bounds.min.y * scale);

        foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
        {
            Object.DestroyImmediate(collider);
        }

        return instance;
    }

    // ---------------------------------------------------------------- 빌드 Scene 목록

    private static void CheckBuildScenes(StringBuilder report)
    {
        string[] wanted = { BootstrapPath, MainMenuPath, GameplayPath };
        EditorBuildSettingsScene[] current = EditorBuildSettings.scenes;
        bool same = current.Length == wanted.Length && current.Select((scene, index) => scene.enabled && scene.path == wanted[index]).All(ok => ok);

        if (!same)
        {
            EditorBuildSettings.scenes = wanted.Select(path => new EditorBuildSettingsScene(path, true)).ToArray();
            report.AppendLine("빌드 Scene 목록 : 00_Bootstrap → 10_MainMenu → 20_Gameplay 로 정리");
        }
        else
        {
            report.AppendLine("빌드 Scene 목록 : 00_Bootstrap → 10_MainMenu → 20_Gameplay (변경 없음)");
        }
    }

    // ---------------------------------------------------------------- 검사

    public static string Validate(out int errorCount)
    {
        StringBuilder report = new StringBuilder("[메뉴 화면 검증]\n");
        int errors = 0;

        void Error(string message)
        {
            errors++;
            report.AppendLine("✗ " + message);
        }

        // 설정 창 Prefab
        SettingsWindowUI window = AssetDatabase.LoadAssetAtPath<GameObject>(SettingsPrefabPath)?.GetComponent<SettingsWindowUI>();

        if (window == null)
        {
            Error("공용 설정 창 Prefab이 없습니다. 17번 메뉴를 실행하세요.");
        }
        else
        {
            CheckReferences(window, "설정 창", Error);
        }

        // 일시정지 메뉴
        PauseMenuView pause = AssetDatabase.LoadAssetAtPath<GameObject>(PausePrefabPath)?.GetComponentInChildren<PauseMenuView>(true);

        if (pause == null || new SerializedObject(pause).FindProperty("settingsWindowPrefab").objectReferenceValue != window)
        {
            Error("일시정지 메뉴 SETTINGS가 공용 설정 창에 연결되지 않았습니다.");
        }

        // 빌드 Scene 목록
        string[] wanted = { BootstrapPath, MainMenuPath, GameplayPath };
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).ToArray();

        if (scenes.Length != wanted.Length || scenes.Where((scene, index) => scene.path != wanted[index]).Any())
        {
            Error($"빌드 Scene 목록이 00_Bootstrap → 10_MainMenu → 20_Gameplay 가 아닙니다. ({string.Join(", ", scenes.Select(scene => Path.GetFileNameWithoutExtension(scene.path)))})");
        }

        // Scene 파일 (열지 않고 파일 내용으로 확인)
        string bootstrap = File.Exists(BootstrapPath) ? File.ReadAllText(BootstrapPath) : string.Empty;
        string menu = File.Exists(MainMenuPath) ? File.ReadAllText(MainMenuPath) : string.Empty;

        if (!bootstrap.Contains("m_Name: " + LoadingCanvasName) || !bootstrap.Contains("m_Name: " + BootCameraName))
        {
            Error("00_Bootstrap에 로딩 화면 · 시작 카메라가 없습니다.");
        }

        if (!menu.Contains("m_Name: " + BackdropName) || !menu.Contains("m_Name: LP_NewGame") || !menu.Contains("m_Name: LP_Settings") || !menu.Contains("m_Name: LP_Quit") || !menu.Contains("m_Name: LP_Confirm"))
        {
            Error("10_MainMenu에 배경 마을 · 메뉴 버튼 · 확인 창이 없습니다.");
        }

        if (menu.Contains("m_Name: StartButton") || menu.Contains("m_Name: StateText"))
        {
            Error("10_MainMenu에 예전 메뉴(StartButton · StateText)가 남아 있습니다.");
        }

        // 열려 있는 Scene이면 연결까지 확인
        Scene menuScene = SceneManager.GetSceneByPath(MainMenuPath);

        if (menuScene.IsValid() && menuScene.isLoaded)
        {
            MainMenuController controller = menuScene.GetRootGameObjects().Select(root => root.GetComponentInChildren<MainMenuController>(true)).FirstOrDefault(found => found != null);

            if (controller == null) Error("10_MainMenu에 MainMenuController가 없습니다.");
            else CheckReferences(controller, "메인 메뉴", Error);
        }

        Scene bootScene = SceneManager.GetSceneByPath(BootstrapPath);

        if (bootScene.IsValid() && bootScene.isLoaded)
        {
            SceneFlowManager flow = bootScene.GetRootGameObjects().Select(root => root.GetComponent<SceneFlowManager>()).FirstOrDefault(found => found != null);

            if (flow == null || flow.LoadingScreen == null) Error("SceneFlowManager에 로딩 화면이 연결되지 않았습니다.");
        }

        report.AppendLine("설정 창 · 일시정지 연결 · 빌드 Scene 목록 · 00_Bootstrap · 10_MainMenu 확인");
        report.AppendLine(errors == 0 ? "결과 : 오류 0개" : $"결과 : 오류 {errors}개");
        errorCount = errors;
        return report.ToString();
    }

    private static void CheckReferences(Object target, string label, Action<string> error)
    {
        SerializedProperty property = new SerializedObject(target).GetIterator();

        while (property.NextVisible(true))
        {
            if (property.propertyType == SerializedPropertyType.ObjectReference && property.name != "m_Script" && property.objectReferenceValue == null)
            {
                error($"{label} : {property.propertyPath} 연결 없음");
            }
        }
    }
}
