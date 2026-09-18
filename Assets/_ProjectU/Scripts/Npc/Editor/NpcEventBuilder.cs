using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// 94일차: NPC 하트 이벤트 도구
// 1. 원본 CSV(NpcEvents)로 알파 NPC 7명의 이벤트 묶음(호기심 · 신뢰 · 애정 장면)을 만든다
// 2. 의뢰 CSV를 다시 읽어 특별 의뢰의 필요 이벤트를 연결한다
// 3. 게임 Scene의 NPC 관리자에 이벤트 관리자를 붙이고, 대화 창을 다시 만들어 선택지 버튼을 넣는다
// 4. 검사 : 이벤트 ID · 대사 · 선택지 · 단계별 이벤트 · 장소 · 일정상 그 시간에 그 장소에 있는지 · 한글 글꼴 · Scene 연결
// 여러 번 실행해도 같은 Asset·오브젝트를 갱신한다. 실행 후 Ctrl+S로 Scene을 저장해야 반영된다.
public static class NpcEventBuilder
{
    private const string DialogTitle = "Project U NPC 이벤트";
    private const string ScenePath = "Assets/_ProjectU/Scenes/20_Gameplay.unity";
    public const string EventFolder = NpcContentBuilder.DataFolder + "/Events";
    private const string EventCsv = NpcContentBuilder.SourceFolder + "/NpcEvents.csv";

    private static readonly Regex EventIdPattern = new Regex("^event_[a-z0-9]+(?:_[a-z0-9]+)*$");
    private static readonly AffinityStage[] RequiredStages = { AffinityStage.Curious, AffinityStage.Trust, AffinityStage.Affection };

    // 코드에서 쓰는 이벤트 한글 문구 (고정 글꼴 Atlas에 모두 들어 있어야 함)
    private static readonly string[] RuntimeTexts = { "이야기 · ", "다음", "대화하기", "나", "선택지", "호감도" };

    // ---------------------------------------------------------------- 메뉴

    [MenuItem(NpcContentBuilder.BuildMenuRoot + "12. NPC Events (Heart Scenes + Choices)", false, 31)]
    private static void BuildAllMenu()
    {
        bool confirmed = EditorUtility.DisplayDialog(
            DialogTitle,
            "알파 NPC 7명의 하트 이벤트(호기심 · 신뢰 · 애정 장면)를 원본 CSV로 만들고,\n"
            + "특별 의뢰가 '신뢰' 이벤트를 본 뒤에 나오도록 의뢰 데이터를 갱신합니다.\n"
            + "현재 게임 Scene(20_Gameplay)에 이벤트 관리자를 추가하고, 대화 창을 선택지 버튼을 넣어 다시 만듭니다.\n\n"
            + "먼저 9번(NPC 대화) · 11번(NPC 의뢰) 메뉴를 실행해 두어야 합니다.\n"
            + "실행 후 Ctrl+S로 Scene을 저장해야 반영됩니다.",
            "실행",
            "취소");

        if (!confirmed)
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
        StringBuilder report = new StringBuilder("[NPC 이벤트 생성]\n");
        List<NpcEventBook> books;

        try
        {
            EditorUtility.DisplayProgressBar(DialogTitle, "이벤트 데이터", 0.2f);
            NpcDatabase database = AssetDatabase.LoadAssetAtPath<NpcDatabase>(NpcContentBuilder.DatabasePath);

            if (database == null)
            {
                report.AppendLine("✗ NpcDatabase가 없습니다. 7번 메뉴를 먼저 실행하세요.");
                return report.ToString();
            }

            books = CreateBooks(database, report);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayProgressBar(DialogTitle, "의뢰 데이터 (필요 이벤트)", 0.4f);
            report.AppendLine(NpcQuestBuilder.RefreshQuestData());

            EditorUtility.DisplayProgressBar(DialogTitle, "한글 글꼴", 0.6f);
            EnsureFont(database, report);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        report.Append(WireScene(books));
        AssetDatabase.SaveAssets();
        report.Append(Validate(out _));
        return report.ToString();
    }

    // ---------------------------------------------------------------- 이벤트 데이터

    private sealed class Draft // CSV를 읽는 동안 모으는 이벤트
    {
        public string Id;
        public string OwnerId;
        public string Title;
        public AffinityStage Stage;
        public string Location;
        public float From;
        public float To;
        public NpcWeatherCondition Weather;
        public ItemData Reward;
        public int RewardAmount;
        public readonly List<NpcEventBook.Line> Lines = new List<NpcEventBook.Line>();
        public readonly List<NpcEventBook.Choice> Choices = new List<NpcEventBook.Choice>();
        public readonly List<NpcEventBook.Line> After = new List<NpcEventBook.Line>();
    }

    private static List<NpcEventBook> CreateBooks(NpcDatabase database, StringBuilder report)
    {
        List<string> errors = new List<string>();
        List<NpcCsv.Row> rows = NpcCsv.Read(EventCsv, errors);
        Dictionary<string, ItemData> items = LoadItemsById();
        List<Draft> drafts = new List<Draft>();
        Dictionary<string, Draft> byId = new Dictionary<string, Draft>(StringComparer.Ordinal);

        foreach (NpcCsv.Row row in rows)
        {
            string where = $"NpcEvents.csv {row.LineNumber}줄";
            string id = row["EventID"];
            string kind = row["Row"];

            if (kind == "Event")
            {
                if (!EventIdPattern.IsMatch(id) || byId.ContainsKey(id))
                {
                    errors.Add($"{where} : 이벤트 ID가 잘못되었거나 중복입니다 ({id})");
                    continue;
                }

                if (!database.TryGet(row["Owner"], out NpcCharacterData owner))
                {
                    errors.Add($"{where} : 주인공 {row["Owner"]} 을(를) NpcDatabase에서 찾지 못했습니다.");
                    continue;
                }

                Draft draft = new Draft
                {
                    Id = id, OwnerId = owner.CharacterId, Title = row["Title"], Location = row["Location"],
                    From = ParseFloat(row["From"], 0f), To = ParseFloat(row["To"], 24f)
                };

                if (!Enum.TryParse(row["Stage"], out draft.Stage))
                {
                    errors.Add($"{where} : 관계 단계 {row["Stage"]} 을(를) 알 수 없습니다.");
                }

                if (!Enum.TryParse(string.IsNullOrEmpty(row["Weather"]) ? "Any" : row["Weather"], out draft.Weather))
                {
                    errors.Add($"{where} : 날씨 {row["Weather"]} 을(를) 알 수 없습니다 (Any · Fair · Bad).");
                }

                if (!string.IsNullOrEmpty(row["Reward"]))
                {
                    string[] parts = row["Reward"].Split(':');

                    if (parts.Length != 2 || !items.TryGetValue(parts[0], out draft.Reward) || !int.TryParse(parts[1], out draft.RewardAmount) || draft.RewardAmount <= 0)
                    {
                        errors.Add($"{where} : 보상 {row["Reward"]} 을(를) 읽지 못했습니다 (item_id:수량).");
                        draft.Reward = null;
                    }
                }

                drafts.Add(draft);
                byId.Add(id, draft);
                continue;
            }

            if (!byId.TryGetValue(id, out Draft target))
            {
                errors.Add($"{where} : Event 줄보다 먼저 나온 {kind} 줄입니다 ({id})");
                continue;
            }

            switch (kind)
            {
                case "Line":
                    target.Lines.Add(new NpcEventBook.Line(row["Speaker"] == "player", row["Text"]));
                    break;
                case "Choice":
                    target.Choices.Add(new NpcEventBook.Choice(row["Text"], row["Reply"], (int)ParseFloat(row["Affinity"], 0f)));
                    break;
                case "After":
                    target.After.Add(new NpcEventBook.Line(row["Speaker"] == "player", row["Text"]));
                    break;
                default:
                    errors.Add($"{where} : 줄 종류 {kind} 을(를) 알 수 없습니다 (Event · Line · Choice · After).");
                    break;
            }
        }

        StylizedArtAssetFactory.EnsureFolder(EventFolder);
        List<NpcEventBook> books = new List<NpcEventBook>();

        foreach (NpcCharacterData owner in database.GetAlphaCast())
        {
            List<Draft> own = drafts.Where(draft => draft.OwnerId == owner.CharacterId).OrderBy(draft => draft.Stage).ToList();

            if (own.Count == 0)
            {
                continue;
            }

            string path = $"{EventFolder}/NpcEvents_{owner.EnglishName.Replace(" ", string.Empty)}.asset";
            NpcEventBook book = LoadOrCreateAsset<NpcEventBook>(path);
            book.EditorAssign(owner.CharacterId, own.Select(draft => new NpcEventBook.Event(draft.Id, draft.Title, draft.Stage, draft.Location, draft.From, draft.To, draft.Weather, draft.Lines, draft.Choices, draft.After, draft.Reward, draft.RewardAmount)).ToList());
            EditorUtility.SetDirty(book);
            books.Add(book);
        }

        report.AppendLine($"이벤트 묶음 {books.Count}개 · 이벤트 {drafts.Count}개 · 대사 {drafts.Sum(draft => draft.Lines.Count + draft.After.Count + draft.Choices.Count)}줄 ({EventFolder})");

        foreach (string error in errors)
        {
            report.AppendLine("✗ " + error);
        }

        return books;
    }

    private static float ParseFloat(string text, float fallback)
    {
        return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ? value : fallback;
    }

    private static Dictionary<string, ItemData> LoadItemsById()
    {
        Dictionary<string, ItemData> result = new Dictionary<string, ItemData>();

        foreach (string guid in AssetDatabase.FindAssets("t:ItemData", new[] { "Assets/_ProjectU/Data" }))
        {
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(guid));

            if (item != null && !string.IsNullOrEmpty(item.ItemId) && !result.ContainsKey(item.ItemId))
            {
                result.Add(item.ItemId, item);
            }
        }

        return result;
    }

    private static TAsset LoadOrCreateAsset<TAsset>(string path) where TAsset : ScriptableObject
    {
        TAsset asset = AssetDatabase.LoadAssetAtPath<TAsset>(path);

        if (asset != null)
        {
            return asset;
        }

        asset = ScriptableObject.CreateInstance<TAsset>();
        asset.name = Path.GetFileNameWithoutExtension(path);
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    public static List<NpcEventBook> LoadBooks()
    {
        List<NpcEventBook> books = new List<NpcEventBook>();

        if (!AssetDatabase.IsValidFolder(EventFolder))
        {
            return books;
        }

        foreach (string guid in AssetDatabase.FindAssets("t:NpcEventBook", new[] { EventFolder }))
        {
            NpcEventBook book = AssetDatabase.LoadAssetAtPath<NpcEventBook>(AssetDatabase.GUIDToAssetPath(guid));

            if (book != null)
            {
                books.Add(book);
            }
        }

        books.Sort((left, right) => string.CompareOrdinal(left.OwnerId, right.OwnerId));
        return books;
    }

    // ---------------------------------------------------------------- 한글 글꼴

    public static IEnumerable<string> CollectEventTexts() // 이벤트 제목 · 대사 · 선택지 · 코드 문구
    {
        foreach (NpcEventBook book in LoadBooks())
        {
            foreach (NpcEventBook.Event data in book.Events)
            {
                if (data == null)
                {
                    continue;
                }

                yield return data.Title;

                foreach (NpcEventBook.Line line in data.Lines.Concat(data.AfterLines))
                {
                    yield return line?.Text;
                }

                foreach (NpcEventBook.Choice choice in data.Choices)
                {
                    yield return choice?.Label;
                    yield return choice?.Reply;
                }
            }
        }

        foreach (string text in RuntimeTexts)
        {
            yield return text;
        }
    }

    private static void EnsureFont(NpcDatabase database, StringBuilder report)
    {
        int missing = 0;
        KoreanFontBuilder.Validate(CollectEventTexts(), _ => missing++, new StringBuilder());

        if (missing == 0)
        {
            report.AppendLine("한글 글꼴 : 이벤트 글자가 모두 들어 있음");
            return;
        }

        KoreanFontBuilder.Build(NpcContentBuilder.CollectDisplayTexts(database), report); // 대사 + 상점 + 의뢰 + 이벤트 글자로 다시 만듦 (GUID 유지)
    }

    // ---------------------------------------------------------------- Scene

    private static string WireScene(List<NpcEventBook> books)
    {
        StringBuilder report = new StringBuilder();
        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.GetActiveScene();
        NpcManager npcManager = Object.FindFirstObjectByType<NpcManager>(FindObjectsInactive.Include);

        if (scene.path != ScenePath)
        {
            report.AppendLine($"✗ 게임 Scene({ScenePath})을 열고 다시 실행하세요. 이벤트 데이터만 만들었습니다.");
            return report.ToString();
        }

        if (npcManager == null || npcManager.GetComponent<NpcRelationshipManager>() == null)
        {
            report.AppendLine("✗ NPC 관리자 · 관계 관리자가 없습니다. 8번 · 9번 메뉴를 먼저 실행하세요.");
            return report.ToString();
        }

        NpcEventManager events = npcManager.GetComponent<NpcEventManager>();

        if (events == null)
        {
            events = npcManager.gameObject.AddComponent<NpcEventManager>();
        }

        events.EditorAssign(books, npcManager, npcManager.GetComponent<NpcRelationshipManager>());
        EditorUtility.SetDirty(events);
        report.AppendLine($"이벤트 관리자 : {npcManager.name} (이벤트 묶음 {books.Count}개)");
        report.AppendLine(NpcDialogueBuilder.RebuildPopup().Replace("의뢰 버튼 포함", "의뢰 버튼 · 선택지 포함"));
        EditorSceneManager.MarkSceneDirty(scene);
        report.AppendLine("Scene 변경 완료 : Ctrl+S로 저장하세요.");
        return report.ToString();
    }

    // ---------------------------------------------------------------- 검증

    public static string Validate(out int errorCount)
    {
        StringBuilder report = new StringBuilder("[NPC 이벤트 검증]\n");
        int errors = 0;

        void Error(string message)
        {
            errors++;
            report.AppendLine("✗ " + message);
        }

        NpcDatabase database = AssetDatabase.LoadAssetAtPath<NpcDatabase>(NpcContentBuilder.DatabasePath);

        if (database == null)
        {
            Error("NpcDatabase가 없습니다. 7번 메뉴를 먼저 실행하세요.");
            errorCount = errors;
            report.AppendLine($"결과 : 오류 {errors}개");
            return report.ToString();
        }

        List<NpcEventBook> books = LoadBooks();
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        int eventCount = 0;

        foreach (NpcCharacterData character in database.GetAlphaCast())
        {
            NpcEventBook book = books.FirstOrDefault(candidate => candidate.OwnerId == character.CharacterId);

            if (book == null)
            {
                Error($"{character.CharacterId} : 이벤트 묶음이 없습니다. 12번 메뉴를 실행하세요.");
                continue;
            }

            foreach (AffinityStage stage in RequiredStages)
            {
                int count = book.Events.Count(data => data != null && data.RequiredStage == stage);

                if (count != 1)
                {
                    Error($"{character.CharacterId} : '{NpcDialogueSelector.StageName(stage)}' 단계 이벤트가 {count}개입니다 (1개 필요).");
                }
            }

            List<string> summary = new List<string>();

            foreach (NpcEventBook.Event data in book.Events)
            {
                if (data == null)
                {
                    Error($"{character.CharacterId} : 빈 이벤트가 있습니다.");
                    continue;
                }

                eventCount++;
                ValidateEvent(data, character, database, ids, Error);
                summary.Add($"{NpcDialogueSelector.StageName(data.RequiredStage)} {data.Title} ({data.LocationId} {MarketStall.FormatHour(data.FromHour)}~{MarketStall.FormatHour(data.ToHour)}{(data.Weather != NpcWeatherCondition.Any ? " " + data.Weather : string.Empty)})");
            }

            report.AppendLine($"{character.DisplayName} : {string.Join(" / ", summary)}");
        }

        KoreanFontBuilder.Validate(CollectEventTexts(), Error, report);
        ValidateScene(books, report, Error);
        report.AppendLine($"이벤트 {eventCount}개");
        errorCount = errors;
        report.AppendLine(errors == 0 ? "결과 : 오류 0개" : $"결과 : 오류 {errors}개");
        return report.ToString();
    }

    private static void ValidateEvent(NpcEventBook.Event data, NpcCharacterData owner, NpcDatabase database, HashSet<string> ids, Action<string> error)
    {
        string id = data.EventId;

        if (!EventIdPattern.IsMatch(id ?? string.Empty) || !ids.Add(id))
        {
            error($"{owner.CharacterId} : 이벤트 ID가 잘못되었거나 중복입니다 ({id})");
        }

        if (string.IsNullOrWhiteSpace(data.Title) || data.Lines.Count == 0 || data.Lines.Any(line => line == null || string.IsNullOrWhiteSpace(line.Text)))
        {
            error($"{id} : 제목이나 선택지 전 대사가 비어 있습니다.");
        }

        if (data.Choices.Count < 2 || data.Choices.Count > NpcDialogueBuilder.ChoiceCount)
        {
            error($"{id} : 선택지는 2~{NpcDialogueBuilder.ChoiceCount}개여야 합니다 ({data.Choices.Count}개).");
        }

        foreach (NpcEventBook.Choice choice in data.Choices)
        {
            if (choice == null || string.IsNullOrWhiteSpace(choice.Label) || string.IsNullOrWhiteSpace(choice.Reply) || choice.Affinity < -10 || choice.Affinity > 15)
            {
                error($"{id} : 선택지 문구 · 대답이 비었거나 호감도 변화가 -10~15를 벗어났습니다.");
            }
        }

        if (!data.Choices.Any(choice => choice != null && choice.Affinity > 0))
        {
            error($"{id} : 호감도가 오르는 선택지가 하나는 있어야 합니다.");
        }

        if (data.RequiredStage == AffinityStage.Uninterested)
        {
            error($"{id} : 이벤트는 '호기심' 단계 이상이어야 합니다.");
        }

        if (!database.HasLocation(data.LocationId))
        {
            error($"{id} : 장소 {data.LocationId} 가 NPC 위치 목록에 없습니다.");
        }

        if (data.FromHour < 0f || data.ToHour > 24f || data.FromHour >= data.ToHour)
        {
            error($"{id} : 시간 범위가 잘못되었습니다 ({data.FromHour}~{data.ToHour}).");
        }
        else if (!IsReachable(data, owner))
        {
            error($"{id} : {owner.DisplayName}은(는) 일정상 {MarketStall.FormatHour(data.FromHour)}~{MarketStall.FormatHour(data.ToHour)}에 {data.LocationId} 에 있는 날이 없습니다.");
        }
    }

    // 일정 데이터에서 그 시간대에 그 장소에 있는 날이 한 번이라도 있어야 이벤트를 볼 수 있다
    public static bool IsReachable(NpcEventBook.Event data, NpcCharacterData owner)
    {
        if (owner.Schedule == null)
        {
            return false;
        }

        WeatherType[] weathers = data.Weather == NpcWeatherCondition.Bad ? new[] { WeatherType.Rain } : data.Weather == NpcWeatherCondition.Fair ? new[] { WeatherType.Clear } : new[] { WeatherType.Clear, WeatherType.Rain };

        for (int season = 0; season < 4; season++)
        {
            for (int weekday = 0; weekday < NpcCalendar.DaysPerWeek; weekday++)
            {
                int day = season * 28 + weekday + 1;

                foreach (WeatherType weather in weathers)
                {
                    NpcScheduleData.Plan plan = owner.Schedule.SelectPlan(day, (SeasonType)season, weather);

                    for (float hour = data.FromHour; hour < data.ToHour; hour += 0.25f)
                    {
                        if (plan?.GetStop(hour)?.LocationId == data.LocationId)
                        {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    private static void ValidateScene(List<NpcEventBook> books, StringBuilder report, Action<string> error)
    {
        if (EditorSceneManager.GetActiveScene().path != ScenePath)
        {
            report.AppendLine("Scene 검사 생략 (게임 Scene이 열려 있지 않음)");
            return;
        }

        NpcManager npcManager = Object.FindFirstObjectByType<NpcManager>(FindObjectsInactive.Include);
        NpcEventManager events = npcManager != null ? npcManager.GetComponent<NpcEventManager>() : null;

        if (events == null)
        {
            error("NPC 관리자에 이벤트 관리자가 없습니다. 12번 메뉴를 실행하세요.");
            return;
        }

        SerializedObject serialized = new SerializedObject(events);

        if (serialized.FindProperty("npcManager").objectReferenceValue != npcManager || serialized.FindProperty("relations").objectReferenceValue == null)
        {
            error("이벤트 관리자의 NPC 관리자 · 관계 관리자 연결이 비어 있습니다.");
        }

        HashSet<NpcEventBook> assigned = new HashSet<NpcEventBook>(events.Books.Where(book => book != null));

        foreach (NpcEventBook book in books.Where(book => !assigned.Contains(book)))
        {
            error($"이벤트 관리자에 {book.OwnerId} 이벤트 묶음이 연결되지 않았습니다.");
        }

        GameUIManager uiManager = Object.FindFirstObjectByType<GameUIManager>(FindObjectsInactive.Include);
        NpcDialoguePopup dialogue = uiManager != null ? uiManager.NpcDialoguePopup : null;
        SerializedProperty choices = dialogue != null ? new SerializedObject(dialogue).FindProperty("choiceButtons") : null;

        if (choices == null || choices.arraySize != NpcDialogueBuilder.ChoiceCount)
        {
            error("NPC 대화 창에 이벤트 선택지 버튼이 없습니다. 12번 메뉴를 다시 실행하세요.");
        }

        report.AppendLine($"Scene : 이벤트 관리자 이벤트 묶음 {assigned.Count}개 · 대화 창 선택지 연결");
    }
}
