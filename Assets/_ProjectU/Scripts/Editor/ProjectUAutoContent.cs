using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 111일차: 콘텐츠 자동 적용 — Tools 메뉴를 누르지 않아도 새 콘텐츠가 한 번씩 적용된다
// 1. Unity를 열거나 스크립트 컴파일이 끝나면, 아래 Updates 목록에서 아직 적용하지 않은 항목을 찾는다
// 2. 게임 Scene을 열고 항목을 차례대로 실행 → Scene · Asset 저장 → 전체 콘텐츠 검사
// 3. 적용 기록(ProjectSettings/ProjectUContentState.json, 커밋함)에 남겨 다시 실행하지 않는다
// 4. 결과는 Console과 Logs/ProjectU_AutoContent.log 에 남고, 원래 열려 있던 Scene으로 돌아간다
// 새 콘텐츠를 넣을 때는 Updates 맨 아래에 항목을 하나 추가한다 (이미 있는 Id는 바꾸지 않음).
[InitializeOnLoad]
public static class ProjectUAutoContent
{
    private const string ScenePath = "Assets/_ProjectU/Scenes/20_Gameplay.unity";
    private const string StatePath = "ProjectSettings/ProjectUContentState.json";
    private const string ReportPath = "Logs/ProjectU_AutoContent.log";
    private const string BatchSwitch = "PROJECTU_AUTOCONTENT"; // 창 없는 실행(검증용)에서는 이 값이 1일 때만 동작
    private const int MaxWaitTicks = 600;

    public sealed class Update
    {
        public string Id; // 적용 기록 이름 (한 번 정하면 바꾸지 않음)
        public string Title; // 사람이 읽는 이름
        public Func<string> Apply; // 실행 (보고서 반환, ✗ 줄 = 오류)
    }

    // 적용할 콘텐츠 목록 (위에서부터 차례대로)
    private static readonly Update[] Updates =
    {
        new Update { Id = "111-island-npc-stories", Title = "111일차 : 4차 NPC 의뢰 · 하트 이벤트 (21번)", Apply = () => NpcStoryBuilder.BuildAll(false) },
        new Update { Id = "112-wave5-cast", Title = "112일차 : 5차 NPC 7명 등장 · 새 집 4채 (구역 · 배치 · 초상 · 동료)", Apply = () => NpcCastBuilder.BuildAll(false) },
        new Update { Id = "113-wave5-stories", Title = "113일차 : 5차 NPC 의뢰 · 하트 이벤트 (35명 모두 이야기 · 동료 잠금 해제)", Apply = () => NpcStoryBuilder.BuildAll(false) }
    };

    [Serializable]
    private sealed class AppliedEntry
    {
        public string id;
        public string appliedAt;
        public bool ok;
    }

    [Serializable]
    private sealed class State
    {
        public List<AppliedEntry> applied = new List<AppliedEntry>();
    }

    private static int waitTicks;

    static ProjectUAutoContent()
    {
        if (Application.isBatchMode && Environment.GetEnvironmentVariable(BatchSwitch) != "1")
        {
            return;
        }

        waitTicks = 0;
        EditorApplication.update -= WaitUntilReady;
        EditorApplication.update += WaitUntilReady;
    }

    public static IReadOnlyList<Update> AllUpdates => Updates;

    public static List<Update> PendingUpdates()
    {
        State state = LoadState();
        return Updates.Where(update => !state.applied.Any(entry => entry.id == update.Id)).ToList();
    }

    private static void WaitUntilReady() // 컴파일 · 가져오기 · Play가 끝난 뒤 한 번 실행
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            if (++waitTicks > MaxWaitTicks && EditorApplication.isPlaying)
            {
                EditorApplication.update -= WaitUntilReady; // Play 중이면 다음 컴파일 때 다시
            }

            return;
        }

        EditorApplication.update -= WaitUntilReady;

        if (PendingUpdates().Count > 0)
        {
            RunPending();
        }
    }

    // 아직 적용하지 않은 항목을 모두 적용한다 (보고서 반환). 검증 도구에서도 부른다.
    public static string RunPending()
    {
        List<Update> pending = PendingUpdates();
        StringBuilder report = new StringBuilder($"[Project U 콘텐츠 자동 적용] {DateTime.Now:yyyy-MM-dd HH:mm}\n");

        if (pending.Count == 0)
        {
            report.AppendLine("적용할 새 콘텐츠가 없습니다.");
            return report.ToString();
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) // 저장하지 않은 Scene이 있으면 먼저 물어봄
        {
            report.AppendLine("열린 Scene 저장을 취소해 이번에는 적용하지 않았습니다. 다음 컴파일 때 다시 시도합니다.");
            Finish(report.ToString(), true);
            return report.ToString();
        }

        SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
        State state = LoadState();
        int failed = 0;

        try
        {
            if (EditorSceneManager.GetActiveScene().path != ScenePath || EditorSceneManager.sceneCount > 1)
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            for (int index = 0; index < pending.Count; index++)
            {
                Update update = pending[index];
                EditorUtility.DisplayProgressBar("Project U 콘텐츠 자동 적용", $"{update.Title} ({index + 1}/{pending.Count})", (index + 0.5f) / pending.Count);
                string result;

                try
                {
                    result = update.Apply();
                }
                catch (Exception exception)
                {
                    result = $"✗ 실행 중 예외 : {exception.Message}\n{exception.StackTrace}";
                }

                List<string> errors = result.Split('\n').Where(line => line.StartsWith("✗")).Select(line => line.TrimEnd('\r')).ToList();
                bool ok = errors.Count == 0;
                failed += ok ? 0 : 1;
                report.AppendLine($"{(ok ? "완료" : "✗ 문제")} : {update.Title}");

                foreach (string error in errors.Take(20))
                {
                    report.AppendLine("  " + error);
                }

                state.applied.Add(new AppliedEntry { id = update.Id, appliedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm"), ok = ok });
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                AssetDatabase.SaveAssets();
                SaveState(state); // 항목마다 기록 (중간에 멈춰도 끝난 항목은 다시 하지 않음)
            }

            EditorUtility.DisplayProgressBar("Project U 콘텐츠 자동 적용", "전체 콘텐츠 검사", 1f);
            string validation = ContentIntegrationValidator.ValidateAll(out int validationErrors, out int warnings);
            report.AppendLine(validationErrors == 0 ? $"전체 콘텐츠 검사 : 문제 없음 (경고 {warnings}개)" : $"✗ 전체 콘텐츠 검사 : 오류 {validationErrors}개");

            if (validationErrors > 0)
            {
                failed++;
                report.AppendLine(validation);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();

            if (previous.Length > 0 && !(previous.Length == 1 && previous[0].path == ScenePath))
            {
                EditorSceneManager.RestoreSceneManagerSetup(previous); // 원래 열려 있던 Scene으로
            }
        }

        report.AppendLine(failed == 0 ? $"결과 : {pending.Count}개 적용 · 문제 없음" : $"결과 : {pending.Count}개 적용 · ✗ 문제 {failed}건 (Console 확인)");
        Finish(report.ToString(), failed > 0);
        return report.ToString();
    }

    private static void Finish(string report, bool problem)
    {
        if (problem)
        {
            Debug.LogError(report);
        }
        else
        {
            Debug.Log(report);
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.AppendAllText(ReportPath, report + "\n");
        }
        catch (IOException)
        {
        }

        if (!Application.isBatchMode && SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.ShowNotification(new GUIContent(problem ? "콘텐츠 자동 적용 : 문제 있음 (Console)" : "콘텐츠 자동 적용 완료"), 4f);
        }
    }

    private static State LoadState()
    {
        try
        {
            return File.Exists(StatePath) ? JsonUtility.FromJson<State>(File.ReadAllText(StatePath)) ?? new State() : new State();
        }
        catch (Exception)
        {
            return new State();
        }
    }

    private static void SaveState(State state)
    {
        File.WriteAllText(StatePath, JsonUtility.ToJson(state, true));
    }
}
