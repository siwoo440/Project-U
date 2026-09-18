using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// 89일차: NPC 콘텐츠 생성 도구
// 1. 캐릭터 시트(Google Sheet → NpcCharacterSheet.csv) 35명을 NPC 데이터로 가져오기
// 2. 선물 반응 · 대사 · 알파 NPC 7명의 하루 일정을 원본 CSV에서 만들기
// 3. 대사에 쓰는 글자로 한글 글꼴을 만들고 Game Data Registry에 등록
// 여러 번 실행해도 같은 Asset을 갱신한다.
public static class NpcContentBuilder
{
    public const string BuildMenuRoot = "Tools/Project U/Build Content/";
    private const string DialogTitle = "Project U NPC 콘텐츠";

    public const string DataFolder = "Assets/_ProjectU/Data/Npc";
    public const string SourceFolder = DataFolder + "/Source";
    public const string DatabasePath = DataFolder + "/NpcDatabase.asset";
    private const string CharacterFolder = DataFolder + "/Characters";
    private const string GiftFolder = DataFolder + "/Gifts";
    private const string DialogueFolder = DataFolder + "/Dialogue";
    private const string ScheduleFolder = DataFolder + "/Schedules";

    private const string SheetCsv = SourceFolder + "/NpcCharacterSheet.csv";
    private const string ExtrasCsv = SourceFolder + "/NpcProfileExtras.csv";
    private const string DialogueCsv = SourceFolder + "/NpcDialogue.csv";
    private const string GiftCsv = SourceFolder + "/NpcGifts.csv";
    private const string GiftTagCsv = SourceFolder + "/NpcGiftTags.csv";
    private const string ScheduleCsv = SourceFolder + "/NpcSchedules.csv";
    private const string LocationCsv = SourceFolder + "/NpcLocations.csv";

    private const string CommonDialogueId = "*"; // 모든 NPC 공통 대사 (자기 대사가 없을 때 사용)
    public const int ExpectedAlphaCast = 7;

    private static readonly Regex IdPattern = new Regex("^char_[a-z0-9]+(?:_[a-z0-9]+)*$");
    private static readonly Regex ColorPattern = new Regex("#[0-9A-Fa-f]{6}");
    private static readonly Regex StagePattern = new Regex(@"(무관심|호기심|신뢰|애정|사랑)\s*:\s*(.*?)(?=\s*•?\s*(?:무관심|호기심|신뢰|애정|사랑)\s*:|$)", RegexOptions.Singleline);
    private static readonly Dictionary<string, AffinityStage> StageNames = new Dictionary<string, AffinityStage>
    {
        { "무관심", AffinityStage.Uninterested }, { "호기심", AffinityStage.Curious }, { "신뢰", AffinityStage.Trust },
        { "애정", AffinityStage.Affection }, { "사랑", AffinityStage.Love }
    };

    // ---------------------------------------------------------------- 메뉴

    [MenuItem(BuildMenuRoot + "7. NPC (Character Sheet + Dialogue + Gifts + Korean Font)", false, 26)]
    private static void BuildAllMenu()
    {
        bool confirmed = EditorUtility.DisplayDialog(
            DialogTitle,
            "캐릭터 시트(Data/Npc/Source)의 NPC 35명을 NPC 데이터로 가져오고\n"
            + "선물 반응 · 대사 · 알파 NPC 7명의 일정과 한글 글꼴을 만듭니다.\n\n"
            + "Scene은 바뀌지 않습니다.",
            "실행",
            "취소");

        if (!confirmed)
        {
            return;
        }

        string report = BuildAll();
        Debug.Log(report);
        EditorUtility.DisplayDialog(DialogTitle, Shorten(report), "확인");
    }

    private static string Shorten(string report)
    {
        const int limit = 1800;
        return report.Length <= limit ? report : report.Substring(0, limit) + "\n... (전체 내용은 Console 참고)";
    }

    // ---------------------------------------------------------------- 전체 생성

    public static string BuildAll()
    {
        StringBuilder report = new StringBuilder("[NPC 콘텐츠 생성]\n");

        try
        {
            foreach (string folder in new[] { DataFolder, CharacterFolder, GiftFolder, DialogueFolder, ScheduleFolder })
            {
                StylizedArtAssetFactory.EnsureFolder(folder);
            }

            EditorUtility.DisplayProgressBar(DialogTitle, "원본 CSV 읽기", 0.05f);
            SourceData source = SourceData.Load(report);

            if (source.Errors.Count > 0)
            {
                foreach (string error in source.Errors)
                {
                    report.AppendLine("✗ " + error);
                }
            }

            Dictionary<string, ItemData> items = LoadItemsById();
            List<NpcCharacterData> characters = new List<NpcCharacterData>();
            int lineCount = 0;
            int giftCount = 0;
            int scheduleCount = 0;

            for (int index = 0; index < source.CharacterIds.Count; index++)
            {
                string id = source.CharacterIds[index];
                EditorUtility.DisplayProgressBar(DialogTitle, id, 0.1f + 0.6f * index / source.CharacterIds.Count);
                Dictionary<string, string> sheet = source.Sheet[id];
                string assetKey = AssetKey(sheet);

                NpcGiftProfile gifts = CreateOrUpdateGifts(id, assetKey, sheet, source, items, report);
                NpcDialogueSet dialogue = CreateOrUpdateDialogue(id, assetKey, sheet, source);
                NpcScheduleData schedule = CreateOrUpdateSchedule(id, assetKey, sheet, source, report);
                characters.Add(CreateOrUpdateCharacter(id, assetKey, sheet, source, gifts, dialogue, schedule));
                lineCount += dialogue.Lines.Count;
                giftCount += gifts.Entries.Count;
                scheduleCount += schedule != null ? 1 : 0;
            }

            NpcDatabase database = LoadOrCreateAsset<NpcDatabase>(DatabasePath);
            database.EditorAssign(characters, source.Locations);
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            int alphaCount = characters.Count(character => character.IsAlphaCast);
            report.AppendLine($"NPC {characters.Count}명 (알파 배치 {alphaCount}명) · 대사 {lineCount}줄 · 선물 반응 {giftCount}개 · 일정 {scheduleCount}개 · 위치 {source.Locations.Count}곳");

            EditorUtility.DisplayProgressBar(DialogTitle, "한글 글꼴", 0.75f);
            KoreanFontBuilder.Build(CollectDisplayTexts(database), report);

            EditorUtility.DisplayProgressBar(DialogTitle, "Game Data Registry", 0.9f);
            GameDataRegistryEditor.CreateOrRefreshDefaultRegistry();
            report.AppendLine("GameDataRegistry 자동 수집 완료");
            AssetDatabase.SaveAssets();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        report.Append(Validate(out _));
        return report.ToString();
    }

    // ---------------------------------------------------------------- 원본 CSV

    private sealed class SourceData
    {
        public readonly List<string> Errors = new List<string>();
        public readonly List<string> CharacterIds = new List<string>();
        public readonly Dictionary<string, Dictionary<string, string>> Sheet = new Dictionary<string, Dictionary<string, string>>();
        public readonly Dictionary<string, NpcCsv.Row> Extras = new Dictionary<string, NpcCsv.Row>();
        public readonly List<NpcCsv.Row> Dialogue = new List<NpcCsv.Row>();
        public readonly List<NpcCsv.Row> Gifts = new List<NpcCsv.Row>();
        public readonly Dictionary<string, List<string>> GiftTags = new Dictionary<string, List<string>>();
        public readonly List<NpcCsv.Row> Schedules = new List<NpcCsv.Row>();
        public readonly List<NpcDatabase.Location> Locations = new List<NpcDatabase.Location>();

        public static SourceData Load(StringBuilder report)
        {
            SourceData data = new SourceData();

            foreach (NpcCsv.Row row in NpcCsv.Read(SheetCsv, data.Errors))
            {
                string id = row["CharacterID"];

                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                if (!data.Sheet.TryGetValue(id, out Dictionary<string, string> fields))
                {
                    fields = new Dictionary<string, string>();
                    data.Sheet.Add(id, fields);
                    data.CharacterIds.Add(id);
                }

                fields[row["FieldKey"]] = row["Value"];
            }

            foreach (NpcCsv.Row row in NpcCsv.Read(ExtrasCsv, data.Errors))
            {
                data.Extras[row["CharacterID"]] = row;
            }

            data.Dialogue.AddRange(NpcCsv.Read(DialogueCsv, data.Errors));
            data.Gifts.AddRange(NpcCsv.Read(GiftCsv, data.Errors));
            data.Schedules.AddRange(NpcCsv.Read(ScheduleCsv, data.Errors));

            foreach (NpcCsv.Row row in NpcCsv.Read(GiftTagCsv, data.Errors))
            {
                if (!data.GiftTags.TryGetValue(row["Tag"], out List<string> itemIds))
                {
                    itemIds = new List<string>();
                    data.GiftTags.Add(row["Tag"], itemIds);
                }

                itemIds.Add(row["ItemID"]);
            }

            foreach (NpcCsv.Row row in NpcCsv.Read(LocationCsv, data.Errors))
            {
                data.Locations.Add(new NpcDatabase.Location(row["LocationID"], row["DisplayName"], row["Description"]));
            }

            report.AppendLine($"원본 CSV : 캐릭터 시트 {data.CharacterIds.Count}명, 대사 {data.Dialogue.Count}줄, 선물 규칙 {data.Gifts.Count}개, 일정 {data.Schedules.Count}칸, 위치 {data.Locations.Count}곳");
            return data;
        }
    }

    private static string Field(Dictionary<string, string> sheet, string key)
    {
        return sheet.TryGetValue(key, out string value) ? value.Trim() : string.Empty;
    }

    private static string AssetKey(Dictionary<string, string> sheet)
    {
        string english = Regex.Replace(Field(sheet, "EnglishName"), "[^A-Za-z0-9]", string.Empty);
        return string.IsNullOrEmpty(english) ? Field(sheet, "CharacterID") : english;
    }

    private static string StripQuotes(string text)
    {
        return string.IsNullOrEmpty(text) ? string.Empty : text.Trim().Trim('•').Trim().Trim('“', '”', '"').Trim();
    }

    // ---------------------------------------------------------------- 캐릭터

    private static NpcCharacterData CreateOrUpdateCharacter(string id, string assetKey, Dictionary<string, string> sheet, SourceData source,
        NpcGiftProfile gifts, NpcDialogueSet dialogue, NpcScheduleData schedule)
    {
        NpcCharacterData character = LoadOrCreateAsset<NpcCharacterData>($"{CharacterFolder}/NpcData_{assetKey}.asset");
        source.Extras.TryGetValue(id, out NpcCsv.Row extras);
        bool alpha = extras != null && extras["AlphaCast"].Equals("TRUE", StringComparison.OrdinalIgnoreCase);
        bool available = !Field(sheet, "IsAvailable").Equals("FALSE", StringComparison.OrdinalIgnoreCase);
        character.EditorAssignIdentity(id, Field(sheet, "DisplayName"), Field(sheet, "EnglishName"), alpha, available);

        NpcCharacterData.ProfileText profile = new NpcCharacterData.ProfileText
        {
            raceId = Field(sheet, "RaceID"), raceName = Field(sheet, "RaceDisplay"),
            jobId = Field(sheet, "JobID"), jobName = Field(sheet, "JobDisplay"),
            gender = Field(sheet, "Gender"), age = Field(sheet, "AgeDisplay"), height = Field(sheet, "HeightDisplay"),
            summary = StripQuotes(Field(sheet, "Summary")), symbolMotif = Field(sheet, "SymbolMotif"),
            origin = Field(sheet, "Origin"), affiliation = Field(sheet, "Affiliation"), relationships = Field(sheet, "RelationshipRaw")
        };
        NpcCharacterData.AppearanceText appearance = new NpcCharacterData.AppearanceText
        {
            hair = Field(sheet, "Hair"), eyes = Field(sheet, "Eyes"), bodyImpression = Field(sheet, "BodyImpression"),
            outfit = Field(sheet, "Outfit"), accessories = Field(sheet, "Accessories"), features = Field(sheet, "Features"),
            illustrationStyle = Field(sheet, "IllustrationStyle")
        };
        NpcCharacterData.PersonalityText personality = new NpcCharacterData.PersonalityText
        {
            tags = SplitList(Field(sheet, "PersonalityTags"), ','), speechStyle = Field(sheet, "SpeechStyle"),
            trauma = Field(sheet, "Trauma"), desire = Field(sheet, "Desire"), background = Field(sheet, "Background"),
            voiceConcept = Field(sheet, "VoiceConcept")
        };

        MatchCollection colors = ColorPattern.Matches(Field(sheet, "ThemeColor"));
        Color theme = colors.Count > 0 && ColorUtility.TryParseHtmlString(colors[0].Value, out Color first) ? first : Color.white;
        Color accent = colors.Count > 1 && ColorUtility.TryParseHtmlString(colors[1].Value, out Color second) ? second : theme * 0.7f;
        character.EditorAssignText(profile, appearance, personality, theme, accent);

        NpcCharacterData.GameplayIds ids = new NpcCharacterData.GameplayIds
        {
            shopId = Field(sheet, "ShopID"), craftingStationId = Field(sheet, "CraftingStationID"), workAbilityId = Field(sheet, "WorkAbilityID"),
            questGroupId = Field(sheet, "QuestGroupID"), recruitmentConditionId = Field(sheet, "RecruitmentConditionID"),
            companionAbilityId = Field(sheet, "CompanionAbilityID"), unlockConditionId = Field(sheet, "UnlockConditionID"),
            relationshipTableId = Field(sheet, "RelationshipTableID")
        };

        SeasonType birthdaySeason = SeasonType.Spring;
        int birthdayDay = 1;
        string home = string.Empty;
        string work = string.Empty;

        if (extras != null)
        {
            Enum.TryParse(extras["BirthdaySeason"], out birthdaySeason);
            int.TryParse(extras["BirthdayDay"], out birthdayDay);
            home = extras["HomeLocation"];
            work = extras["WorkLocation"];
        }

        int.TryParse(Field(sheet, "DefaultAffinity"), out int startAffinity);

        if (!int.TryParse(Field(sheet, "MaxAffinity"), out int maxAffinity) || maxAffinity <= 0)
        {
            maxAffinity = 100;
        }

        character.EditorAssignGameplay(ParseFlags<NpcRole>(Field(sheet, "NpcRoles")), ParseFlags<NpcInteraction>(Field(sheet, "InteractionTypes")),
            ids, startAffinity, maxAffinity, birthdaySeason, birthdayDay, home, work);

        string filled = alpha ? "생일 · 집·일터 · 하루 일정 · 추가 대사 · 선물 반응" : "생일 · 공통 계절·날씨·선물 대사 · 선물 반응";
        string note = $"캐릭터 시트 검토일 {Field(sheet, "LastReviewedDate")}. 89일차 임시 작성: {filled}. 시트의 선호 물건은 게임 아이템으로 대신 연결.";
        character.EditorAssignLinks(gifts, dialogue, schedule, Field(sheet, "DataStatus"), note);
        EditorUtility.SetDirty(character);
        return character;
    }

    private static TFlags ParseFlags<TFlags>(string text) where TFlags : struct, Enum
    {
        int value = 0;

        foreach (string part in SplitList(text, '|'))
        {
            if (Enum.TryParse(part, true, out TFlags flag))
            {
                value |= Convert.ToInt32(flag);
            }
        }

        return (TFlags)Enum.ToObject(typeof(TFlags), value);
    }

    private static string[] SplitList(string text, char separator)
    {
        return string.IsNullOrWhiteSpace(text)
            ? Array.Empty<string>()
            : text.Split(separator).Select(part => part.Trim()).Where(part => part.Length > 0).ToArray();
    }

    // ---------------------------------------------------------------- 선물

    private static NpcGiftProfile CreateOrUpdateGifts(string id, string assetKey, Dictionary<string, string> sheet, SourceData source,
        Dictionary<string, ItemData> items, StringBuilder report)
    {
        NpcGiftProfile gifts = LoadOrCreateAsset<NpcGiftProfile>($"{GiftFolder}/NpcGift_{assetKey}.asset");
        List<NpcCsv.Row> rules = source.Gifts.Where(row => row["CharacterID"] == id).ToList();
        List<NpcGiftProfile.Entry> entries = new List<NpcGiftProfile.Entry>();
        HashSet<ItemData> assigned = new HashSet<ItemData>();

        // 아이템을 직접 적은 규칙을 먼저, 분류(tag:) 규칙을 나중에 적용한다 (먼저 정해진 아이템은 유지)
        foreach (NpcCsv.Row rule in rules.OrderBy(row => row["Target"].StartsWith("tag:") ? 1 : 0))
        {
            if (!Enum.TryParse(rule["Preference"], out GiftPreference preference))
            {
                report.AppendLine($"✗ {id} 선물 반응 단계를 알 수 없습니다: {rule["Preference"]} (NpcGifts.csv {rule.LineNumber}줄)");
                continue;
            }

            string target = rule["Target"];
            List<string> itemIds = target.StartsWith("tag:")
                ? (source.GiftTags.TryGetValue(target.Substring(4), out List<string> tagged) ? tagged : null)
                : new List<string> { target };

            if (itemIds == null)
            {
                report.AppendLine($"✗ {id} 선물 분류가 없습니다: {target} (NpcGifts.csv {rule.LineNumber}줄)");
                continue;
            }

            foreach (string itemId in itemIds)
            {
                if (!items.TryGetValue(itemId, out ItemData item))
                {
                    report.AppendLine($"✗ {id} 선물 아이템이 없습니다: {itemId} (NpcGifts.csv {rule.LineNumber}줄)");
                    continue;
                }

                if (assigned.Add(item))
                {
                    entries.Add(new NpcGiftProfile.Entry(item, preference, target));
                }
            }
        }

        entries.Sort((left, right) => left.Preference != right.Preference
            ? left.Preference.CompareTo(right.Preference)
            : string.CompareOrdinal(left.Item.ItemId, right.Item.ItemId));
        string profileId = Field(sheet, "GiftProfileID");
        gifts.EditorAssign(string.IsNullOrEmpty(profileId) ? "gift_" + id.Substring(5) : profileId, Field(sheet, "PreferredItems"), entries);
        EditorUtility.SetDirty(gifts);
        return gifts;
    }

    // ---------------------------------------------------------------- 대사

    private static NpcDialogueSet CreateOrUpdateDialogue(string id, string assetKey, Dictionary<string, string> sheet, SourceData source)
    {
        NpcDialogueSet dialogue = LoadOrCreateAsset<NpcDialogueSet>($"{DialogueFolder}/NpcDialogue_{assetKey}.asset");
        List<NpcDialogueSet.Line> lines = new List<NpcDialogueSet.Line>();

        void Add(NpcDialogueKind kind, string text, AffinityStage stage = AffinityStage.Uninterested, SeasonType season = SeasonType.Spring,
            WeatherType weather = WeatherType.Clear, GiftPreference gift = GiftPreference.Neutral)
        {
            text = StripQuotes(text);

            if (!string.IsNullOrEmpty(text))
            {
                lines.Add(new NpcDialogueSet.Line(kind, stage, season, weather, gift, text));
            }
        }

        // 캐릭터 시트 대사
        Add(NpcDialogueKind.Greeting, Field(sheet, "Greeting"));
        Add(NpcDialogueKind.Talk, Field(sheet, "RandomTalk"));

        foreach (Match match in StagePattern.Matches(Field(sheet, "AffinityReactions")))
        {
            Add(NpcDialogueKind.Stage, match.Groups[2].Value, StageNames[match.Groups[1].Value]);
        }

        Add(NpcDialogueKind.Event, Field(sheet, "EventLines"));

        // 추가 대사 (캐릭터 전용 → 없으면 공통)
        List<NpcCsv.Row> own = source.Dialogue.Where(row => row["CharacterID"] == id).ToList();
        List<NpcCsv.Row> common = source.Dialogue.Where(row => row["CharacterID"] == CommonDialogueId).ToList();

        foreach (NpcCsv.Row row in own)
        {
            AddRow(row);
        }

        foreach (NpcCsv.Row row in common)
        {
            if (!own.Any(mine => mine["Kind"] == row["Kind"] && mine["Condition"] == row["Condition"]))
            {
                AddRow(row);
            }
        }

        void AddRow(NpcCsv.Row row)
        {
            if (!Enum.TryParse(row["Kind"], out NpcDialogueKind kind))
            {
                return;
            }

            string condition = row["Condition"];

            switch (kind)
            {
                case NpcDialogueKind.Season:
                    Enum.TryParse(condition, out SeasonType season);
                    Add(kind, row["Text"], season: season);
                    break;
                case NpcDialogueKind.Weather:
                    Enum.TryParse(condition, out WeatherType weather);
                    Add(kind, row["Text"], weather: weather);
                    break;
                case NpcDialogueKind.Gift:
                    Enum.TryParse(condition, out GiftPreference gift);
                    Add(kind, row["Text"], gift: gift);
                    break;
                default:
                    AffinityStage stage = AffinityStage.Uninterested;

                    if (!string.IsNullOrEmpty(condition))
                    {
                        Enum.TryParse(condition, out stage);
                    }

                    Add(kind, row["Text"], stage);
                    break;
            }
        }

        lines.Sort((left, right) => left.Kind.CompareTo(right.Kind));
        string dialogueId = Field(sheet, "DialogueTableID");
        dialogue.EditorAssign(string.IsNullOrEmpty(dialogueId) ? "dlg_" + id.Substring(5) : dialogueId, 1, SplitList(Field(sheet, "ExpressionTags"), '/'), lines);
        EditorUtility.SetDirty(dialogue);
        return dialogue;
    }

    // ---------------------------------------------------------------- 일정

    private static NpcScheduleData CreateOrUpdateSchedule(string id, string assetKey, Dictionary<string, string> sheet, SourceData source, StringBuilder report)
    {
        List<NpcCsv.Row> rows = source.Schedules.Where(row => row["CharacterID"] == id).ToList();
        string path = $"{ScheduleFolder}/NpcSchedule_{assetKey}.asset";

        if (rows.Count == 0)
        {
            if (AssetDatabase.LoadAssetAtPath<NpcScheduleData>(path) != null)
            {
                AssetDatabase.DeleteAsset(path); // 일정이 빠진 NPC는 이전 일정 삭제
            }

            return null;
        }

        NpcScheduleData schedule = LoadOrCreateAsset<NpcScheduleData>(path);
        List<NpcScheduleData.Plan> plans = new List<NpcScheduleData.Plan>();

        foreach (IGrouping<string, NpcCsv.Row> group in rows.GroupBy(row => row["Plan"]))
        {
            NpcCsv.Row first = group.First();
            int.TryParse(first["Priority"], out int priority);

            if (!Enum.TryParse(first["Weather"], out NpcWeatherCondition weather))
            {
                report.AppendLine($"✗ {id} 일정 날씨 조건을 알 수 없습니다: {first["Weather"]} (NpcSchedules.csv {first.LineNumber}줄)");
            }

            List<NpcScheduleData.Stop> stops = group
                .Select(row => new NpcScheduleData.Stop(float.TryParse(row["Hour"], out float hour) ? hour : 0f, row["Location"], row["Activity"]))
                .OrderBy(stop => stop.Hour)
                .ToList();
            plans.Add(new NpcScheduleData.Plan(group.Key, priority, ParseMask<NpcWeekdays>(first["Days"]), ParseMask<NpcSeasons>(first["Seasons"]), weather, stops));
        }

        string scheduleId = Field(sheet, "ScheduleID");
        schedule.EditorAssign(string.IsNullOrEmpty(scheduleId) ? "schedule_" + id.Substring(5) : scheduleId, plans);
        EditorUtility.SetDirty(schedule);
        return schedule;
    }

    private static TFlags ParseMask<TFlags>(string text) where TFlags : struct, Enum
    {
        return string.IsNullOrWhiteSpace(text) ? (TFlags)Enum.Parse(typeof(TFlags), "All") : ParseFlags<TFlags>(text);
    }

    // ---------------------------------------------------------------- 공통

    public static IEnumerable<string> CollectDisplayTexts(NpcDatabase database)
    {
        if (database == null)
        {
            yield break;
        }

        foreach (NpcDatabase.Location location in database.Locations)
        {
            yield return location.DisplayName;
        }

        foreach (NpcCharacterData character in database.Characters)
        {
            if (character == null)
            {
                continue;
            }

            yield return character.DisplayName;
            yield return character.EnglishName;
            yield return character.Profile.raceName;
            yield return character.Profile.jobName;
            yield return character.Profile.summary;

            if (character.DialogueSet != null)
            {
                foreach (NpcDialogueSet.Line line in character.DialogueSet.Lines)
                {
                    yield return line.Text;
                }
            }

            if (character.Schedule != null)
            {
                foreach (NpcScheduleData.Plan plan in character.Schedule.Plans)
                {
                    foreach (NpcScheduleData.Stop stop in plan.Stops)
                    {
                        yield return stop.Activity;
                    }
                }
            }
        }

        foreach (string text in NpcShopBuilder.CollectShopTexts()) // 92일차: 상점 이름 · 인사도 글꼴에 포함
        {
            yield return text;
        }

        foreach (string text in NpcQuestBuilder.CollectQuestTexts()) // 93일차: 의뢰 제목 · 대사도 글꼴에 포함
        {
            yield return text;
        }
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

    // ---------------------------------------------------------------- 검증

    public static string Validate(out int errorCount)
    {
        StringBuilder report = new StringBuilder("[NPC 검증]\n");
        int errors = 0;

        void Error(string message)
        {
            errors++;
            report.AppendLine("✗ " + message);
        }

        NpcDatabase database = AssetDatabase.LoadAssetAtPath<NpcDatabase>(DatabasePath);

        if (database == null)
        {
            Error("NpcDatabase가 없습니다. Tools > Project U > Build Content > 7. NPC를 실행하세요.");
            errorCount = errors;
            report.AppendLine($"결과 : 오류 {errors}개");
            return report.ToString();
        }

        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        int alpha = 0;
        int lines = 0;

        foreach (NpcCharacterData character in database.Characters)
        {
            if (character == null)
            {
                Error("NpcDatabase에 빈 캐릭터 칸이 있습니다.");
                continue;
            }

            string id = character.CharacterId;

            if (!IdPattern.IsMatch(id ?? string.Empty) || !ids.Add(id))
            {
                Error($"{character.name} : 캐릭터 ID가 잘못되었거나 중복입니다 ({id})");
            }

            if (string.IsNullOrEmpty(character.DisplayName))
            {
                Error($"{id} : 표시 이름이 비어 있습니다.");
            }

            if (character.HasRole(NpcRole.Merchant) && string.IsNullOrEmpty(character.Ids.shopId))
            {
                Error($"{id} : 상인 역할인데 상점 ID가 없습니다.");
            }

            ValidateGifts(character, Error);
            lines += ValidateDialogue(character, Error);

            if (character.IsAlphaCast)
            {
                alpha++;
                ValidateAlpha(character, database, Error);
            }
        }

        if (alpha != ExpectedAlphaCast)
        {
            Error($"알파 배치 NPC가 {alpha}명입니다 (기준 {ExpectedAlphaCast}명).");
        }

        foreach (NpcDatabase.Location location in database.Locations)
        {
            if (location == null || string.IsNullOrEmpty(location.LocationId) || !location.LocationId.StartsWith("loc_"))
            {
                Error("위치 ID는 loc_로 시작해야 합니다.");
            }
        }

        report.AppendLine($"NPC {database.Characters.Count}명 (알파 배치 {alpha}명) · 대사 {lines}줄 · 위치 {database.Locations.Count}곳");
        KoreanFontBuilder.Validate(CollectDisplayTexts(database), Error, report);
        errorCount = errors;
        report.AppendLine(errors == 0 ? "결과 : 오류 0개" : $"결과 : 오류 {errors}개");
        return report.ToString();
    }

    private static void ValidateGifts(NpcCharacterData character, Action<string> error)
    {
        NpcGiftProfile gifts = character.GiftProfile;

        if (gifts == null)
        {
            error($"{character.CharacterId} : 선물 반응이 연결되지 않았습니다.");
            return;
        }

        foreach (NpcGiftProfile.Entry entry in gifts.Entries)
        {
            if (entry == null || entry.Item == null)
            {
                error($"{character.CharacterId} : 선물 반응에 빈 아이템이 있습니다.");
            }
        }

        if (gifts.Count(GiftPreference.Loved) == 0 || gifts.Count(GiftPreference.Liked) == 0)
        {
            error($"{character.CharacterId} : 매우 좋아함·좋아함 선물이 각각 하나 이상 필요합니다.");
        }

        if (character.IsAlphaCast && gifts.Count(GiftPreference.Disliked) + gifts.Count(GiftPreference.Hated) == 0)
        {
            error($"{character.CharacterId} : 알파 NPC는 싫어하는 선물이 하나 이상 필요합니다.");
        }
    }

    private static int ValidateDialogue(NpcCharacterData character, Action<string> error)
    {
        NpcDialogueSet dialogue = character.DialogueSet;
        string id = character.CharacterId;

        if (dialogue == null)
        {
            error($"{id} : 대사가 연결되지 않았습니다.");
            return 0;
        }

        foreach (NpcDialogueKind kind in new[] { NpcDialogueKind.FirstMeeting, NpcDialogueKind.Greeting, NpcDialogueKind.Talk, NpcDialogueKind.Birthday })
        {
            if (dialogue.Count(kind) == 0)
            {
                error($"{id} : {kind} 대사가 없습니다.");
            }
        }

        foreach (AffinityStage stage in Enum.GetValues(typeof(AffinityStage)))
        {
            if (!dialogue.TryGetStageLine(stage, out _))
            {
                error($"{id} : 호감도 {stage} 대사가 없습니다.");
            }
        }

        foreach (SeasonType season in Enum.GetValues(typeof(SeasonType)))
        {
            if (!dialogue.TryGetSeasonLine(season, out _))
            {
                error($"{id} : {season} 계절 대사가 없습니다.");
            }
        }

        foreach (WeatherType weather in new[] { WeatherType.Rain, WeatherType.Snow, WeatherType.Storm })
        {
            if (!dialogue.TryGetWeatherLine(weather, out _))
            {
                error($"{id} : {weather} 날씨 대사가 없습니다.");
            }
        }

        foreach (GiftPreference preference in Enum.GetValues(typeof(GiftPreference)))
        {
            if (!dialogue.TryGetGiftLine(preference, out _))
            {
                error($"{id} : 선물 {preference} 반응 대사가 없습니다.");
            }
        }

        if (character.IsAlphaCast && (dialogue.Count(NpcDialogueKind.Greeting) < 2 || dialogue.Count(NpcDialogueKind.Talk) < 3))
        {
            error($"{id} : 알파 NPC는 인사 2줄·잡담 3줄 이상이 필요합니다.");
        }

        return dialogue.Lines.Count;
    }

    private static void ValidateAlpha(NpcCharacterData character, NpcDatabase database, Action<string> error)
    {
        string id = character.CharacterId;

        foreach (string location in new[] { character.HomeLocationId, character.WorkLocationId })
        {
            if (!database.HasLocation(location))
            {
                error($"{id} : 집·일터 위치가 위치 목록에 없습니다 ({location}).");
            }
        }

        NpcScheduleData schedule = character.Schedule;

        if (schedule == null || schedule.Plans.Count == 0)
        {
            error($"{id} : 하루 일정이 없습니다.");
            return;
        }

        bool hasDefault = false;
        bool hasBadWeather = false;

        foreach (NpcScheduleData.Plan plan in schedule.Plans)
        {
            hasDefault |= plan.Days == NpcWeekdays.All && plan.Seasons == NpcSeasons.All && plan.Weather == NpcWeatherCondition.Any;
            hasBadWeather |= plan.Weather == NpcWeatherCondition.Bad;

            if (plan.Days == NpcWeekdays.None || plan.Seasons == NpcSeasons.None || plan.Stops.Count == 0)
            {
                error($"{id} : '{plan.PlanId}' 일정의 요일·계절·일정 칸이 비어 있습니다.");
            }

            foreach (NpcScheduleData.Stop stop in plan.Stops)
            {
                if (!database.HasLocation(stop.LocationId))
                {
                    error($"{id} : '{plan.PlanId}' 일정 위치가 위치 목록에 없습니다 ({stop.LocationId}).");
                }
            }
        }

        if (!hasDefault)
        {
            error($"{id} : 조건 없는 기본 일정(default)이 없습니다.");
        }

        if (!hasBadWeather)
        {
            error($"{id} : 비·눈·폭풍 날의 일정이 없습니다.");
        }
    }
}
