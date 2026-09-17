using UnityEditor;
using UnityEngine;

// 78일차: 저폴리 모델·맵·UI 테마 적용 메뉴
public static class ProjectUArtMenu
{
    private const string Root = "Tools/Project U/Art & UI/";

    [MenuItem(Root + "Run All (Models + Map + UI)", false, 0)]
    private static void RunAll()
    {
        if (!EditorUtility.DisplayDialog(
                "Project U 외형 전체 적용",
                "저폴리 모델 생성 → 게임 Prefab 적용 → 현재 Scene 맵 꾸미기 → UI 테마 적용을 차례로 실행합니다.\n\n"
                + "실행 전에 Scene을 저장해 두세요. Scene 변경은 Ctrl+Z로 되돌릴 수 있고, Prefab 변경은 'Restore' 메뉴로 되돌릴 수 있습니다.",
                "실행",
                "취소"))
        {
            return;
        }

        string report = StylizedArtAssetFactory.GenerateAllModels(true) + "개 모델 생성\n"
            + StylizedArtPrefabApplier.ApplyAll() + "\n"
            + UIThemeApplier.ApplyToPrefabs() + "\n"
            + StylizedSceneDresser.DressActiveScene() + "\n"
            + UIThemeApplier.ApplyToScene();
        ShowReport("전체 적용 결과", report);
    }

    [MenuItem(Root + "1. Generate Low-poly Models", false, 20)]
    private static void GenerateModels()
    {
        int count = StylizedArtAssetFactory.GenerateAllModels(true);
        ShowReport("모델 생성", $"저폴리 모델 {count}개를 {StylizedArtAssetFactory.PrefabFolder}에 생성했습니다.");
    }

    [MenuItem(Root + "2. Apply Models To Game Prefabs", false, 21)]
    private static void ApplyPrefabs()
    {
        ShowReport("Prefab 외형 적용", StylizedArtPrefabApplier.ApplyAll());
    }

    [MenuItem(Root + "3. Dress Current Scene (Map)", false, 22)]
    private static void DressScene()
    {
        ShowReport("맵 꾸미기", StylizedSceneDresser.DressActiveScene());
    }

    [MenuItem(Root + "4. Apply UI Theme To UI Prefabs", false, 23)]
    private static void ApplyUiPrefabs()
    {
        UISpriteFactory.GenerateAll();
        ShowReport("UI Prefab 테마", UIThemeApplier.ApplyToPrefabs());
    }

    [MenuItem(Root + "5. Apply UI Theme To Current Scene (HUD)", false, 24)]
    private static void ApplyUiScene()
    {
        ShowReport("HUD 테마", UIThemeApplier.ApplyToScene());
    }

    [MenuItem(Root + "Restore/Restore Primitive Visuals In Prefabs", false, 60)]
    private static void RestorePrefabs()
    {
        ShowReport("Prefab 복구", StylizedArtPrefabApplier.RestoreAll());
    }

    [MenuItem(Root + "Restore/Remove Scene Environment Props", false, 61)]
    private static void RemoveEnvironment()
    {
        ShowReport("환경 제거", StylizedSceneDresser.RemoveEnvironment());
    }

    [MenuItem(Root + "Restore/Restore Selected Object Visual", false, 62)]
    private static void RestoreSelected()
    {
        int restored = 0;

        foreach (GameObject selected in Selection.gameObjects)
        {
            if (StylizedVisualReplacer.Restore(selected, true))
            {
                restored++;
            }
        }

        ShowReport("선택 오브젝트 복구", $"{restored}개 오브젝트의 기본 도형 외형을 복구했습니다.");
    }

    private static void ShowReport(string title, string report)
    {
        Debug.Log($"[Project U Art & UI] {title}\n{report}");
        string shortReport = report.Length > 1200 ? report.Substring(0, 1200) + "\n... (전체 내용은 Console 참고)" : report;
        EditorUtility.DisplayDialog(title, shortReport, "확인");
    }
}
