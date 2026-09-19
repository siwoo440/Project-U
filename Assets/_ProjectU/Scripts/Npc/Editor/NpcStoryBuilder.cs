using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 103일차: 섬 NPC 이야기 한 번에 갱신 (하트 이벤트 + 의뢰)
// 12번(하트 이벤트) → 11번(의뢰 : 특별 의뢰가 신뢰 이벤트를 참조) 순서로 실행하고 게임 Scene을 저장한다.
// 의뢰 · 이벤트를 갖추는 차수는 NpcDatabase.StoryReadyWave까지다.
public static class NpcStoryBuilder
{
    private const string ScenePath = "Assets/_ProjectU/Scenes/20_Gameplay.unity";
    private const string DialogTitle = "Project U 섬 NPC 이야기";

    public static string BuildAll(bool saveScene)
    {
        StringBuilder report = new StringBuilder("[섬 NPC 이야기 갱신]\n");

        if (EditorSceneManager.GetActiveScene().path != ScenePath)
        {
            report.AppendLine($"✗ 게임 Scene({ScenePath})을 열고 다시 실행하세요.");
            return report.ToString();
        }

        report.AppendLine(Result("12. 하트 이벤트", NpcEventBuilder.BuildAll()));
        report.AppendLine(Result("11. 의뢰", NpcQuestBuilder.BuildAll()));

        if (saveScene)
        {
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            report.AppendLine("게임 Scene 저장 완료");
        }

        report.Append(Validate(out _));
        return report.ToString();
    }

    private static string Result(string label, string report)
    {
        List<string> errors = report.Split('\n').Where(line => line.StartsWith("✗")).Select(line => line.TrimEnd('\r')).ToList();
        int failed = report.Split('\n').Count(line => line.StartsWith("결과") && !line.Contains("오류 0개"));
        return errors.Count == 0 && failed == 0 ? $"{label} : 완료" : $"{label} : ✗ 문제 {Math.Max(errors.Count, failed)}개\n{string.Join("\n", errors)}";
    }

    // 차수별 이야기 준비 : 이야기 차수(1 · 2차)는 모두 의뢰(특별 의뢰 포함) · 하트 이벤트 3개를 갖춰야 한다
    public static string Validate(out int errorCount)
    {
        StringBuilder report = new StringBuilder("[섬 NPC 이야기 검증]\n");
        int errors = 0;

        void Error(string message)
        {
            errors++;
            report.AppendLine("✗ " + message);
        }

        NpcDatabase database = AssetDatabase.LoadAssetAtPath<NpcDatabase>(NpcContentBuilder.DatabasePath);

        if (database == null)
        {
            Error("NpcDatabase가 없습니다.");
            errorCount = errors;
            return report.ToString();
        }

        List<NpcQuestBook> questBooks = NpcQuestBuilder.LoadBooks();
        List<NpcEventBook> eventBooks = NpcEventBuilder.LoadBooks();

        foreach (IGrouping<int, NpcCharacterData> wave in database.GetPlacedCast().GroupBy(character => character.CastWave).OrderBy(group => group.Key))
        {
            int ready = 0;

            foreach (NpcCharacterData character in wave)
            {
                NpcQuestBook quests = questBooks.FirstOrDefault(book => book.OwnerId == character.CharacterId);
                NpcEventBook events = eventBooks.FirstOrDefault(book => book.OwnerId == character.CharacterId);
                bool hasQuests = quests != null && quests.Quests.Any(quest => quest != null && !quest.IsSpecial) && quests.Quests.Any(quest => quest != null && quest.IsSpecial);
                bool hasEvents = events != null && events.Events.Count(data => data != null) >= 3;

                if (wave.Key <= NpcDatabase.StoryReadyWave && (!hasQuests || !hasEvents))
                {
                    Error($"{character.CharacterId} : 의뢰(게시판 · 특별) 또는 하트 이벤트 3개가 없습니다. 21번 메뉴를 실행하세요.");
                }

                ready += hasQuests && hasEvents ? 1 : 0;
            }

            string state = wave.Key <= NpcDatabase.StoryReadyWave ? string.Empty : " (아직 이야기 차수가 아님)";
            report.AppendLine($"{wave.Key}차 이야기 준비 {ready}/{wave.Count()}{state}");
        }

        report.AppendLine($"의뢰 묶음 {questBooks.Count}개 · 이벤트 묶음 {eventBooks.Count}개");
        errorCount = errors;
        report.AppendLine(errors == 0 ? "결과 : 오류 0개" : $"결과 : 오류 {errors}개");
        return report.ToString();
    }
}
