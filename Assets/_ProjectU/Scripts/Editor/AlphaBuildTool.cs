using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

// 99일차: Windows 알파 빌드 도구
// 1. 빌드 전 검사 : 전체 콘텐츠 검사(오류 0) · 메뉴 화면 검사 · 빌드 Scene 목록 · 제품 정보
// 2. Windows 64비트 빌드 (Builds/Windows/Project U.exe)
// 3. 결과(성공 여부 · 크기 · 시간 · 경고 · 오류)를 Docs/Build/AlphaBuildReport.md 로 남긴다
public static class AlphaBuildTool
{
    private const string DialogTitle = "Project U 알파 빌드";
    public const string OutputFolder = "Builds/Windows";
    public const string ExeName = "Project U.exe";
    public const string ReportPath = "Docs/Build/AlphaBuildReport.md";
    private const string GameplayScenePath = "Assets/_ProjectU/Scenes/20_Gameplay.unity";

    [MenuItem("Tools/Project U/Build/Windows Alpha Build", false, 3)]
    private static void BuildReleaseMenu() => BuildMenu(false);

    [MenuItem("Tools/Project U/Build/Windows Alpha Build (Development)", false, 4)]
    private static void BuildDevelopmentMenu() => BuildMenu(true);

    private static void BuildMenu(bool development)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(DialogTitle, "Play 중에는 빌드할 수 없습니다.", "확인");
            return;
        }

        bool confirmed = EditorUtility.DisplayDialog(
            DialogTitle,
            $"Windows 64비트 {(development ? "개발용 " : string.Empty)}알파 빌드를 만듭니다.\n"
            + $"· 결과 : 프로젝트 폴더/{OutputFolder}/{ExeName}\n"
            + "· 먼저 전체 콘텐츠 검사를 하고, 오류가 있으면 빌드하지 않습니다.\n"
            + "· 처음 빌드는 몇 분 걸릴 수 있습니다.\n\n"
            + "열린 Scene에 저장하지 않은 변경이 있으면 먼저 저장할지 묻습니다.",
            "빌드",
            "취소");

        if (!confirmed || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        string report = Build(development, OutputFolder, false);
        Debug.Log(report);
        EditorUtility.DisplayDialog(DialogTitle, report.Length <= 1800 ? report : report.Substring(0, 1800) + "\n... (전체 내용은 Console 참고)", "확인");

        if (report.Contains("결과 : 빌드 성공"))
        {
            EditorUtility.RevealInFinder(Path.Combine(OutputFolder, ExeName));
        }
    }

    public static string Build(bool development, string outputFolder, bool skipContentValidation)
    {
        StringBuilder report = new StringBuilder("[Windows 알파 빌드]\n");
        StringBuilder md = new StringBuilder("# Project U 알파 빌드 보고서\n\n");
        DateTime started = DateTime.Now;
        md.AppendLine($"- 날짜 : {started:yyyy-MM-dd HH:mm}");
        md.AppendLine($"- 제품 : {PlayerSettings.productName} {PlayerSettings.bundleVersion} ({PlayerSettings.companyName})");
        md.AppendLine($"- 종류 : Windows 64비트 {(development ? "개발용(Development)" : "배포용")} · Unity {Application.unityVersion}");

        // 1. 빌드 전 검사
        string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
        md.AppendLine($"- Scene : {string.Join(" → ", scenes.Select(Path.GetFileNameWithoutExtension))}");
        int preErrors = 0;

        if (scenes.Length == 0 || !scenes[0].EndsWith("00_Bootstrap.unity", StringComparison.Ordinal))
        {
            preErrors++;
            report.AppendLine("✗ 빌드 Scene 목록의 첫 Scene이 00_Bootstrap 이 아닙니다. 17번 메뉴를 실행하세요.");
        }

        string menu = MenuSceneBuilder.Validate(out int menuErrors);
        preErrors += menuErrors;

        if (menuErrors > 0)
        {
            report.Append(menu);
        }

        if (!skipContentValidation)
        {
            bool openedGameplay = false;

            if (EditorSceneManager.GetActiveScene().path != GameplayScenePath) // 전체 검사는 게임 Scene 연결도 본다
            {
                EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
                openedGameplay = true;
            }

            string content = ContentIntegrationValidator.ValidateAll(out int contentErrors, out int contentWarnings);
            preErrors += contentErrors;
            md.AppendLine($"- 전체 콘텐츠 검사 : 오류 {contentErrors} · 경고 {contentWarnings}{(openedGameplay ? " (게임 Scene을 열어 검사)" : string.Empty)}");

            if (contentErrors > 0)
            {
                report.Append(content);
            }
        }

        if (PlayerSettings.companyName == "DefaultCompany")
        {
            report.AppendLine("△ 회사 이름이 DefaultCompany 입니다. 저장 폴더 경로에 들어가므로 출시 전에 정하세요.");
        }

        if (preErrors > 0)
        {
            report.AppendLine($"결과 : 빌드 전 검사 오류 {preErrors}개 - 빌드하지 않았습니다.");
            md.AppendLine($"- 결과 : 빌드 전 검사 오류 {preErrors}개로 빌드하지 않음");
            WriteReport(md);
            return report.ToString();
        }

        // 2. 빌드
        string exePath = Path.Combine(outputFolder, ExeName);
        Directory.CreateDirectory(outputFolder);
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = exePath,
            target = BuildTarget.StandaloneWindows64,
            targetGroup = BuildTargetGroup.Standalone,
            options = development ? BuildOptions.Development : BuildOptions.None
        };

        BuildReport build = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = build.summary;
        int knownErrors = build.steps.SelectMany(step => step.messages).Count(message => (message.type == LogType.Error || message.type == LogType.Exception) && IsKnownEngineMessage(message.content));
        int realErrors = Math.Max(0, summary.totalErrors - knownErrors);
        long folderBytes = Directory.Exists(outputFolder) ? new DirectoryInfo(outputFolder).GetFiles("*", SearchOption.AllDirectories).Sum(file => file.Length) : 0;
        report.AppendLine($"결과 : {(summary.result == BuildResult.Succeeded ? "빌드 성공" : "빌드 실패 (" + summary.result + ")")}");
        report.AppendLine($"실행 파일 : {Path.GetFullPath(exePath)}");
        report.AppendLine($"폴더 크기 {folderBytes / (1024f * 1024f):0.0} MB · 걸린 시간 {summary.totalTime.TotalSeconds:0}초 · 오류 {realErrors} · 경고 {summary.totalWarnings}{(knownErrors > 0 ? $" (알려진 Unity URP 메시지 {knownErrors}개 제외)" : string.Empty)}");

        md.AppendLine($"- 결과 : {(summary.result == BuildResult.Succeeded ? "빌드 성공" : "빌드 실패 (" + summary.result + ")")}");
        md.AppendLine($"- 실행 파일 : `{outputFolder}/{ExeName}`");
        md.AppendLine($"- 폴더 크기 : {folderBytes / (1024f * 1024f):0.0} MB");
        md.AppendLine($"- 걸린 시간 : {summary.totalTime.TotalSeconds:0}초");
        md.AppendLine($"- 빌드 오류 {realErrors} · 경고 {summary.totalWarnings}{(knownErrors > 0 ? $" (알려진 Unity URP 메시지 {knownErrors}개 제외 : .urtshader 가져오기 안내, 게임에는 영향 없음)" : string.Empty)}");

        var messages = build.steps.SelectMany(step => step.messages).Where(message => message.type == LogType.Error || message.type == LogType.Exception || message.type == LogType.Warning).Take(30).ToList();

        if (messages.Count > 0)
        {
            md.AppendLine();
            md.AppendLine("## 빌드 메시지 (최대 30개)");
            md.AppendLine();

            foreach (BuildStepMessage message in messages)
            {
                md.AppendLine($"- {message.type}{(IsKnownEngineMessage(message.content) ? " (알려진 Unity 메시지)" : string.Empty)} : {message.content.Split('\n')[0]}");
            }
        }

        WriteReport(md);
        report.AppendLine($"보고서 : {ReportPath}");
        return report.ToString();
    }

    private static bool IsKnownEngineMessage(string content) // URP 패키지의 .urtshader 가져오기 안내 (Unity 6000.3 알려진 메시지)
    {
        return content != null && content.Contains(".urtshader");
    }

    private static void WriteReport(StringBuilder md)
    {
        string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ReportPath));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
        File.WriteAllText(fullPath, md.ToString().Replace("\r\n", "\n"), new UTF8Encoding(false));
    }
}
