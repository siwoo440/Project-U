using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

// 101일차: 섬 NPC 한 번에 갱신 (차수별 등장)
// 18번(새 구역 · 구역 안 집) → 8번(NPC 마을 배치 · Prefab · 위치 · NavMesh) → 9번(초상 · 대화 창) 순서로 실행하고 게임 Scene을 저장한다.
// 새 차수를 넣을 때 (NpcProfileExtras.csv CastWave · 일정 · 대사) 이 메뉴 하나만 실행하면 된다.
public static class NpcCastBuilder
{
    private const string ScenePath = "Assets/_ProjectU/Scenes/20_Gameplay.unity";
    private const string DialogTitle = "Project U 섬 NPC";

    [MenuItem(MarketContentBuilder.BuildMenuRoot + "19. Island NPC Cast (Zones + Village + Portraits)", false, 38)]
    private static void BuildAllMenu()
    {
        bool confirmed = EditorUtility.DisplayDialog(
            DialogTitle,
            "섬에 배치되는 NPC(1차 알파 · 2차 ...)를 한 번에 갱신합니다.\n"
            + "18번(새 구역 · 구역 안 집) → 8번(NPC 마을 배치 · Prefab · NavMesh) → 9번(초상 · 대화 창) 순서로 실행하고 게임 Scene을 저장합니다.\n\n"
            + "게임 Scene을 열고 실행하세요 (1분 정도 걸릴 수 있습니다).",
            "실행",
            "취소");

        if (!confirmed)
        {
            return;
        }

        if (EditorSceneManager.GetActiveScene().path != ScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        string report = BuildAll(true);
        Debug.Log(report);
        EditorUtility.DisplayDialog(DialogTitle, report.Length <= 1800 ? report : report.Substring(0, 1800) + "\n... (전체 내용은 Console 참고)", "확인");
    }

    public static string BuildAll(bool saveScene)
    {
        StringBuilder report = new StringBuilder("[섬 NPC 갱신]\n");

        if (EditorSceneManager.GetActiveScene().path != ScenePath)
        {
            report.AppendLine($"✗ 게임 Scene({ScenePath})을 열고 다시 실행하세요.");
            return report.ToString();
        }

        report.AppendLine(Result("18. 새 구역", WorldZoneBuilder.BuildAll(false)));
        report.AppendLine(Result("8. NPC 마을 배치", NpcPlacementBuilder.BuildAll()));
        report.AppendLine(Result("9. 초상 · 대화 창", NpcDialogueBuilder.BuildAll()));
        report.AppendLine(Result("25. NPC 동료", NpcCompanionBuilder.BuildAll(false))); // 109일차: 동료 버튼 · 화면 표시 유지

        if (saveScene)
        {
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            report.AppendLine("게임 Scene 저장 완료");
        }

        report.Append(Validate(out _));
        return report.ToString();
    }

    // 하위 메뉴 보고서에서 오류 줄과 결과 줄만 모은다
    private static string Result(string label, string report)
    {
        List<string> errors = report.Split('\n').Where(line => line.StartsWith("✗")).Select(line => line.TrimEnd('\r')).ToList();
        int failed = report.Split('\n').Count(line => line.StartsWith("결과") && !line.Contains("오류 0개"));
        return errors.Count == 0 && failed == 0 ? $"{label} : 완료" : $"{label} : ✗ 문제 {Math.Max(errors.Count, failed)}개\n{string.Join("\n", errors)}";
    }

    // ---------------------------------------------------------------- 검증

    // 차수별 준비 상태 : 집(밤에 들어가는 위치) · 일정 · 대사 · 초상 · 체형 규칙에 맞는 Prefab · Scene 배치
    public static string Validate(out int errorCount)
    {
        StringBuilder report = new StringBuilder("[섬 NPC 차수 검증]\n");
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

        bool sceneOpen = EditorSceneManager.GetActiveScene().path == ScenePath;
        NpcManager manager = sceneOpen ? Object.FindFirstObjectByType<NpcManager>(FindObjectsInactive.Include) : null;
        List<NpcCharacterData> cast = database.GetPlacedCast();
        List<NpcShopData> allShops = NpcShopBuilder.LoadShops();
        List<NpcCraftBook> allBooks = NpcShopBuilder.LoadCraftBooks(); // 102일차
        List<NpcQuestBook> questBooks = NpcQuestBuilder.LoadBooks(); // 103일차
        List<NpcEventBook> eventBooks = NpcEventBuilder.LoadBooks(); // 103일차

        foreach (IGrouping<int, NpcCharacterData> wave in cast.GroupBy(character => character.CastWave).OrderBy(group => group.Key))
        {
            int ready = 0;

            foreach (NpcCharacterData character in wave)
            {
                string id = character.CharacterId;
                int before = errors;
                NpcDatabase.Location home = database.GetLocation(character.HomeLocationId);

                if (home == null || !home.IsHome)
                {
                    Error($"{id} : 집 위치가 없거나 '집'으로 표시되지 않았습니다 ({character.HomeLocationId}). NpcLocations.csv Home 칸을 확인하세요.");
                }

                if (character.Schedule == null || character.Schedule.Plans.Count < 2)
                {
                    Error($"{id} : 하루 일정이 부족합니다 (기본 · 궂은 날 일정 필요).");
                }

                int lines = character.DialogueSet != null ? character.DialogueSet.Lines.Count : 0;

                if (lines < 20)
                {
                    Error($"{id} : 대사가 {lines}줄입니다 (20줄 이상 필요).");
                }

                if (character.Portrait == null)
                {
                    Error($"{id} : 초상이 없습니다. 19번 메뉴를 실행하세요.");
                }

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{NpcPlacementBuilder.PrefabFolder}/NPC_{character.EnglishName.Replace(" ", string.Empty)}.prefab");
                NpcAgent prefabAgent = prefab != null ? prefab.GetComponent<NpcAgent>() : null;
                NpcBodyRules.TryGet(id, out NpcBodyMetrics metrics);

                if (prefabAgent == null)
                {
                    Error($"{id} : NPC Prefab이 없습니다. 19번 메뉴를 실행하세요.");
                }
                else if (prefabAgent.MotionStyle != metrics.Motion || !Mathf.Approximately(prefab.GetComponent<NavMeshAgent>().radius, metrics.AgentRadius))
                {
                    Error($"{id} : NPC Prefab이 체형 규칙과 다릅니다 (움직임 {prefabAgent.MotionStyle}/{metrics.Motion}). 19번 메뉴를 실행하세요.");
                }

                if (manager != null && !manager.Agents.Any(agent => agent != null && agent.Character == character))
                {
                    Error($"{id} : 게임 Scene에 배치되지 않았습니다.");
                }

                ready += errors == before ? 1 : 0;
            }

            // 상점 · 제작 · 의뢰 · 이벤트는 차수마다 뒤이어 붙인다 (102일차: 2차 상점 · 제작, 103일차: 2차 의뢰 · 이벤트)
            List<NpcCharacterData> members = wave.ToList();
            int shops = members.Count(character => character.HasRole(NpcRole.Merchant) && allShops.Any(shop => shop.OwnerId == character.CharacterId));
            int merchants = members.Count(character => character.HasRole(NpcRole.Merchant));
            bool ShopCrafter(NpcCharacterData character) => character.HasRole(NpcRole.Crafter) && character.CanInteract(NpcInteraction.Craft); // 104일차: 상점 제작 탭 · 작업장 제작 창 모두
            int crafters = members.Count(ShopCrafter);
            int books = members.Count(character => ShopCrafter(character) && allBooks.Any(book => book.OwnerId == character.CharacterId));
            int quests = members.Count(character => questBooks.Any(book => book.OwnerId == character.CharacterId));
            int events = members.Count(character => eventBooks.Any(book => book.OwnerId == character.CharacterId && book.Events.Count >= 3));
            report.AppendLine($"{wave.Key}차 {members.Count}명 준비 {ready}/{members.Count} ({string.Join(" · ", members.Select(character => character.DisplayName))}) · 상점 {shops}/{merchants} · 제작 {books}/{crafters} · 의뢰 {quests}/{members.Count} · 이벤트 {events}/{members.Count}");
        }

        if (!sceneOpen)
        {
            report.AppendLine("Scene 검사 생략 (게임 Scene이 열려 있지 않음)");
        }

        errorCount = errors;
        report.AppendLine(errors == 0 ? "결과 : 오류 0개" : $"결과 : 오류 {errors}개");
        return report.ToString();
    }
}
