using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// 114일차: 이웃끼리의 관계
// 1. NpcRelations.csv → 관계 목록(NpcRelationBook) : 짝 · 관계 · 만나면 나누는 대화 · 서로를 말하는 대사
// 2. 게임 Scene : NPC 관리자 오브젝트에 이웃 대화 관리자(NpcBanterManager) 연결
// 3. 검사 : 두 NPC가 일정상 실제로 같은 곳에 함께 있는 시간이 있는지 (못 만나는 짝은 오류)
// 자동 적용(ProjectUAutoContent)이 실행한다. 여러 번 실행해도 같은 결과가 나온다.
public static class NpcRelationBuilder
{
    public const string RelationCsv = NpcContentBuilder.SourceFolder + "/NpcRelations.csv";
    public const string BookFolder = NpcContentBuilder.DataFolder + "/Relations";
    public const string BookPath = BookFolder + "/NpcRelationBook.asset";
    private const string ScenePath = "Assets/_ProjectU/Scenes/20_Gameplay.unity";
    private const float MinimumMeetHours = 1f; // 하루에 이만큼 이어서 같은 곳에 있어야 만남으로 봄
    private const float DayStart = 6f; // 일어나는 시각
    private const float DayEnd = 21f; // 자는 시각

    public static string BuildAll(bool saveScene)
    {
        StringBuilder report = new StringBuilder("[NPC 관계 만들기]\n");
        NpcRelationBook book = BuildBook(report);

        if (EditorSceneManager.GetActiveScene().path != ScenePath)
        {
            report.AppendLine($"✗ 게임 Scene({ScenePath})이 열려 있지 않아 관계 정보만 만들었습니다.");
            return report.ToString();
        }

        NpcManager npcManager = Object.FindFirstObjectByType<NpcManager>(FindObjectsInactive.Include);

        if (npcManager == null)
        {
            report.AppendLine("✗ NPC 관리자(NpcManager)가 없습니다.");
            return report.ToString();
        }

        NpcBanterManager banter = npcManager.GetComponent<NpcBanterManager>();

        if (banter == null)
        {
            banter = npcManager.gameObject.AddComponent<NpcBanterManager>();
        }

        banter.EditorAssign(book);
        EditorUtility.SetDirty(banter);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        report.AppendLine($"이웃 대화 관리자 연결 ({npcManager.name})");

        if (saveScene)
        {
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            report.AppendLine("게임 Scene 저장 완료");
        }

        report.Append(Validate(out _));
        return report.ToString();
    }

    private static NpcRelationBook BuildBook(StringBuilder report)
    {
        List<string> errors = new List<string>();
        List<NpcCsv.Row> rows = NpcCsv.Read(RelationCsv, errors);
        List<NpcRelationBook.Pair> pairs = new List<NpcRelationBook.Pair>();

        foreach (NpcCsv.Row row in rows)
        {
            string id = row["PairID"];
            string kind = row["Row"];
            NpcRelationBook.Pair pair = pairs.FirstOrDefault(item => item.pairId == id);

            if (kind == "Pair")
            {
                if (pair != null || !Enum.TryParse(row["Relation"], out NpcRelationKind relation))
                {
                    errors.Add($"NpcRelations.csv {row.LineNumber}줄 : 쌍이 중복이거나 관계를 읽을 수 없습니다 ({id}).");
                    continue;
                }

                pairs.Add(new NpcRelationBook.Pair { pairId = id, characterA = row["CharacterA"], characterB = row["CharacterB"], kind = relation });
                continue;
            }

            if (pair == null)
            {
                errors.Add($"NpcRelations.csv {row.LineNumber}줄 : Pair 줄보다 먼저 나온 대사입니다 ({id}).");
                continue;
            }

            bool byA = row["Speaker"] == "A";

            if (kind == "Mention")
            {
                (byA ? pair.mentionsByA : pair.mentionsByB).Add(row["Text"]);
            }
            else if (kind.StartsWith("Banter", StringComparison.Ordinal) && int.TryParse(kind.Substring(6), out int number) && number >= 1)
            {
                while (pair.banters.Count < number)
                {
                    pair.banters.Add(new NpcRelationBook.Banter());
                }

                pair.banters[number - 1].lines.Add(new NpcRelationBook.Line { byA = byA, text = row["Text"] });
            }
            else
            {
                errors.Add($"NpcRelations.csv {row.LineNumber}줄 : 알 수 없는 줄 종류입니다 ({kind}).");
            }
        }

        StylizedArtAssetFactory.EnsureFolder(BookFolder);
        NpcRelationBook book = AssetDatabase.LoadAssetAtPath<NpcRelationBook>(BookPath);

        if (book == null)
        {
            book = ScriptableObject.CreateInstance<NpcRelationBook>();
            AssetDatabase.CreateAsset(book, BookPath);
        }

        book.EditorAssign(pairs);
        EditorUtility.SetDirty(book);
        AssetDatabase.SaveAssets();

        foreach (string error in errors)
        {
            report.AppendLine("✗ " + error);
        }

        report.AppendLine($"관계 {pairs.Count}쌍 : " + string.Join(" · ", Enum.GetValues(typeof(NpcRelationKind)).Cast<NpcRelationKind>().Where(kind => pairs.Any(pair => pair.kind == kind)).Select(kind => $"{NpcRelationBook.KindName(kind)} {pairs.Count(pair => pair.kind == kind)}")));
        return book;
    }

    // ---------------------------------------------------------------- 검사

    // 일정상 두 NPC가 같은 곳에 이어서 함께 있는 시간 (4계절 × 7요일 × 맑음 · 비)
    public static float MeetHours(NpcCharacterData a, NpcCharacterData b, out int daysMet, out string place)
    {
        daysMet = 0;
        place = string.Empty;
        float best = 0f;

        if (a?.Schedule == null || b?.Schedule == null)
        {
            return 0f;
        }

        for (int season = 0; season < 4; season++)
        {
            for (int weekday = 0; weekday < NpcCalendar.DaysPerWeek; weekday++)
            {
                int day = season * 28 + weekday + 1;

                foreach (WeatherType weather in new[] { WeatherType.Clear, WeatherType.Rain })
                {
                    NpcScheduleData.Plan planA = a.Schedule.SelectPlan(day, (SeasonType)season, weather);
                    NpcScheduleData.Plan planB = b.Schedule.SelectPlan(day, (SeasonType)season, weather);
                    float run = 0f;
                    float dayBest = 0f;

                    for (float hour = DayStart; hour < DayEnd; hour += 0.25f)
                    {
                        string at = planA?.GetStop(hour)?.LocationId;
                        string bt = planB?.GetStop(hour)?.LocationId;

                        if (!string.IsNullOrEmpty(at) && at == bt)
                        {
                            run += 0.25f;

                            if (run > best)
                            {
                                best = run;
                                place = at;
                            }

                            dayBest = Mathf.Max(dayBest, run);
                        }
                        else
                        {
                            run = 0f;
                        }
                    }

                    if (dayBest >= MinimumMeetHours)
                    {
                        daysMet++;
                    }
                }
            }
        }

        return best;
    }

    public static IEnumerable<string> CollectTexts(NpcRelationBook book)
    {
        yield return "친구 라이벌 짝꿍 스승과 제자";

        if (book == null)
        {
            yield break;
        }

        foreach (NpcRelationBook.Pair pair in book.Pairs)
        {
            foreach (NpcRelationBook.Line line in pair.banters.SelectMany(banter => banter.lines))
            {
                yield return line.text;
            }

            foreach (string line in pair.mentionsByA.Concat(pair.mentionsByB))
            {
                yield return line;
            }
        }
    }

    public static string Validate(out int errorCount)
    {
        StringBuilder report = new StringBuilder("[NPC 관계 검증]\n");
        int errors = 0;

        void Error(string message)
        {
            errors++;
            report.AppendLine("✗ " + message);
        }

        NpcRelationBook book = AssetDatabase.LoadAssetAtPath<NpcRelationBook>(BookPath);
        NpcDatabase database = AssetDatabase.LoadAssetAtPath<NpcDatabase>(NpcContentBuilder.DatabasePath);

        if (book == null || database == null)
        {
            Error("관계 목록 또는 NPC 데이터가 없습니다. 콘텐츠 자동 적용을 기다리세요.");
            errorCount = errors;
            report.AppendLine($"결과 : 오류 {errors}개");
            return report.ToString();
        }

        List<NpcCharacterData> cast = database.GetPlacedCast();
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> couples = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> linked = new HashSet<string>(StringComparer.Ordinal);

        foreach (NpcRelationBook.Pair pair in book.Pairs)
        {
            string id = pair.pairId;
            NpcCharacterData a = cast.FirstOrDefault(character => character.CharacterId == pair.characterA);
            NpcCharacterData b = cast.FirstOrDefault(character => character.CharacterId == pair.characterB);

            if (string.IsNullOrEmpty(id) || !ids.Add(id))
            {
                Error($"{id} : 쌍 ID가 비었거나 중복입니다.");
            }

            string couple = string.CompareOrdinal(pair.characterA, pair.characterB) < 0 ? pair.characterA + "|" + pair.characterB : pair.characterB + "|" + pair.characterA;

            if (!couples.Add(couple))
            {
                Error($"{id} : 같은 두 NPC의 관계가 두 번 있습니다.");
            }

            if (a == null || b == null || a == b)
            {
                Error($"{id} : 두 NPC가 섬에 사는 서로 다른 이웃이 아닙니다 ({pair.characterA} · {pair.characterB}).");
                continue;
            }

            linked.Add(a.CharacterId);
            linked.Add(b.CharacterId);

            if (pair.banters.Count < 2)
            {
                Error($"{id} : 만나면 나누는 대화가 2세트보다 적습니다.");
            }

            for (int index = 0; index < pair.banters.Count; index++)
            {
                List<NpcRelationBook.Line> lines = pair.banters[index].lines;

                if (lines.Count < 2 || lines.Count > 4 || !lines.Any(line => line.byA) || !lines.Any(line => !line.byA) || lines.Any(line => string.IsNullOrWhiteSpace(line.text)))
                {
                    Error($"{id} : 대화 {index + 1}은 2~4줄이고 두 NPC가 모두 말해야 합니다.");
                }
            }

            if (pair.mentionsByA.Count == 0 || pair.mentionsByB.Count == 0 || pair.mentionsByA.Concat(pair.mentionsByB).Any(string.IsNullOrWhiteSpace))
            {
                Error($"{id} : 서로를 말하는 대사가 양쪽 모두 필요합니다.");
            }

            float hours = MeetHours(a, b, out int daysMet, out string place);

            if (hours < MinimumMeetHours)
            {
                Error($"{id} : {a.DisplayName} · {b.DisplayName}은(는) 일정상 같은 곳에 {MinimumMeetHours:0}시간 넘게 함께 있는 날이 없습니다.");
            }

            string placeName = database.GetLocation(place)?.DisplayName ?? place;
            report.AppendLine($"{a.DisplayName} · {b.DisplayName} ({NpcRelationBook.KindName(pair.kind)}) : 만나는 날 {daysMet}/56 · 가장 긴 만남 {hours:0.#}시간 ({placeName})");
        }

        KoreanFontBuilder.Validate(CollectTexts(book), Error, new StringBuilder());

        if (EditorSceneManager.GetActiveScene().path == ScenePath)
        {
            NpcManager npcManager = Object.FindFirstObjectByType<NpcManager>(FindObjectsInactive.Include);
            NpcBanterManager banter = npcManager != null ? npcManager.GetComponent<NpcBanterManager>() : null;

            if (banter == null || banter.Book != book)
            {
                Error("NPC 관리자에 이웃 대화 관리자가 없거나 관계 목록이 연결되지 않았습니다.");
            }
        }
        else
        {
            report.AppendLine("Scene 검사 생략 (게임 Scene이 열려 있지 않음)");
        }

        report.AppendLine($"관계 {book.Pairs.Count}쌍 · 관계가 있는 이웃 {linked.Count}/{cast.Count}명");
        errorCount = errors;
        report.AppendLine(errors == 0 ? "결과 : 오류 0개" : $"결과 : 오류 {errors}개");
        return report.ToString();
    }
}
