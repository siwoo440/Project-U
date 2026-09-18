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

// 93일차: NPC 의뢰 도구
// 1. 원본 CSV(NpcQuests)로 알파 NPC 7명의 의뢰 묶음(게시판 의뢰 · 특별 의뢰)을 만든다
// 2. 게임 Scene의 NPC 관리자에 의뢰 관리자를 붙이고, 광장 게시판에 상호작용을 달고,
//    게시판 창 · 진행 중 의뢰 표시를 만들고, 대화 창을 다시 만들어 의뢰 전달 버튼을 넣는다
// 3. 검사 : 의뢰 ID · 대사 · 필요 물건 · 계절 · 보상(되사기 이익) · 계절별 의뢰 · 특별 의뢰 · 한글 글꼴 · Scene 연결
// 여러 번 실행해도 같은 Asset·오브젝트를 갱신한다. 실행 후 Ctrl+S로 Scene을 저장해야 반영된다.
public static class NpcQuestBuilder
{
    private const string DialogTitle = "Project U NPC 의뢰";
    private const string ScenePath = "Assets/_ProjectU/Scenes/20_Gameplay.unity";
    public const string QuestFolder = NpcContentBuilder.DataFolder + "/Quests";
    private const string QuestCsv = NpcContentBuilder.SourceFolder + "/NpcQuests.csv";
    private const string BoardBuildingPath = "Buildings/VillageBoard";
    public const string BoardInteractName = "QuestBoard";
    private const string InteractableLayer = "Interactable";

    private static readonly Regex QuestIdPattern = new Regex("^quest_[a-z0-9]+(?:_[a-z0-9]+)*$");

    // 코드에서 쓰는 의뢰 한글 문구 (고정 글꼴 Atlas에 모두 들어 있어야 함)
    private static readonly string[] RuntimeTexts =
    {
        "마을 게시판", "의뢰 없음", "새 의뢰", "오늘 의뢰 없음", "진행 중", "전달 가능", "전달 가능!", "특별", "특별 의뢰", "의뢰 받기", "의뢰 포기", "한 번 더 누르면 포기",
        "의뢰 전달", "의뢰 (2/3)", "오늘의 의뢰", "필요한 물건", "보상", "코인", "호감도", "오늘까지", "일 남음", "일 안에", "받은 날부터 일 안에 전달", "준비 완료! 에게 가져다주세요",
        "의뢰인", "의뢰 완료", "의뢰를 받았어요", "의뢰를 포기했어요", "게시판에 없는 의뢰예요.", "의뢰는 개까지 받을 수 있어요.", "이 주민의 의뢰를 이미 진행 중이에요.",
        "진행 중인 의뢰가 아니에요.", "기한이 지난 의뢰예요.", "개가 더 필요해요.", "의뢰 데이터가 잘못되었어요.", "가방에 보상을 받을 자리가 없어요.", "기한이 지나 의뢰가 사라졌어요",
        "오늘은 붙어 있는 의뢰가 없어요. 내일 다시 확인해 보세요.", "의뢰는 동시에 개까지 · 물건을 모아 의뢰한 주민에게 말을 걸어 전달하세요", "게시판 창이 없어요"
    };

    // ---------------------------------------------------------------- 메뉴

    [MenuItem(NpcContentBuilder.BuildMenuRoot + "11. NPC Quests (Board + Requests + Delivery)", false, 30)]
    private static void BuildAllMenu()
    {
        bool confirmed = EditorUtility.DisplayDialog(
            DialogTitle,
            "알파 NPC 7명의 의뢰(게시판 의뢰 · 특별 의뢰)를 원본 CSV로 만들고,\n"
            + "현재 게임 Scene(20_Gameplay)에 의뢰 관리자 · 광장 게시판 상호작용 · 게시판 창 · 진행 중 의뢰 표시를 추가합니다.\n"
            + "NPC 대화 창은 의뢰 전달 버튼을 넣어 다시 만듭니다.\n\n"
            + "먼저 8번(NPC 배치) · 9번(NPC 대화) · 10번(NPC 상점) 메뉴를 실행해 두어야 합니다.\n"
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
        StringBuilder report = new StringBuilder("[NPC 의뢰 생성]\n");
        List<NpcQuestBook> books;

        try
        {
            EditorUtility.DisplayProgressBar(DialogTitle, "의뢰 데이터", 0.2f);
            NpcDatabase database = AssetDatabase.LoadAssetAtPath<NpcDatabase>(NpcContentBuilder.DatabasePath);

            if (database == null)
            {
                report.AppendLine("✗ NpcDatabase가 없습니다. 7번 메뉴를 먼저 실행하세요.");
                return report.ToString();
            }

            books = CreateBooks(database, report);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayProgressBar(DialogTitle, "한글 글꼴", 0.5f);
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

    // ---------------------------------------------------------------- 의뢰 데이터

    public static string RefreshQuestData() // 94일차: 의뢰 CSV만 다시 읽어 의뢰 묶음 갱신 (특별 의뢰의 필요 이벤트, 12번 메뉴에서 사용)
    {
        NpcDatabase database = AssetDatabase.LoadAssetAtPath<NpcDatabase>(NpcContentBuilder.DatabasePath);

        if (database == null)
        {
            return "✗ NpcDatabase가 없어 의뢰 데이터를 갱신하지 못했습니다.";
        }

        StringBuilder report = new StringBuilder();
        CreateBooks(database, report);
        AssetDatabase.SaveAssets();
        return report.ToString().TrimEnd();
    }

    private static List<NpcQuestBook> CreateBooks(NpcDatabase database, StringBuilder report)
    {
        List<string> errors = new List<string>();
        List<NpcCsv.Row> rows = NpcCsv.Read(QuestCsv, errors);
        Dictionary<string, ItemData> items = LoadItemsById();
        Dictionary<string, List<NpcQuestBook.Quest>> byOwner = new Dictionary<string, List<NpcQuestBook.Quest>>(StringComparer.Ordinal);
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        int special = 0;

        foreach (NpcCsv.Row row in rows)
        {
            string where = $"NpcQuests.csv {row.LineNumber}줄";
            string id = row["QuestID"];

            if (!QuestIdPattern.IsMatch(id) || !ids.Add(id))
            {
                errors.Add($"{where} : 의뢰 ID가 잘못되었거나 중복입니다 ({id})");
                continue;
            }

            if (!database.TryGet(row["Owner"], out NpcCharacterData owner))
            {
                errors.Add($"{where} : 의뢰인 {row["Owner"]} 을(를) NpcDatabase에서 찾지 못했습니다.");
                continue;
            }

            if (!Enum.TryParse(row["Kind"], out NpcQuestKind kind))
            {
                errors.Add($"{where} : 종류 {row["Kind"]} 을(를) 알 수 없습니다 (Board · Special).");
                continue;
            }

            List<NpcQuestBook.Requirement> requirements = new List<NpcQuestBook.Requirement>();

            foreach ((string itemId, int amount) in ParseItems(row["Requirements"], where, errors))
            {
                if (items.TryGetValue(itemId, out ItemData item))
                {
                    requirements.Add(new NpcQuestBook.Requirement(item, amount));
                }
                else
                {
                    errors.Add($"{where} : 필요 물건 {itemId} 이(가) 없습니다.");
                }
            }

            ItemData rewardItem = null;
            int rewardAmount = 0;

            foreach ((string itemId, int amount) in ParseItems(row["RewardItem"], where, errors))
            {
                if (!items.TryGetValue(itemId, out rewardItem))
                {
                    errors.Add($"{where} : 보상 아이템 {itemId} 이(가) 없습니다.");
                }

                rewardAmount = amount;
            }

            List<SeasonType> seasons = new List<SeasonType>();

            foreach (string token in Split(row["Seasons"]))
            {
                if (token == "All")
                {
                    seasons.Clear();
                    break;
                }

                if (Enum.TryParse(token, out SeasonType season))
                {
                    seasons.Add(season);
                }
                else
                {
                    errors.Add($"{where} : 계절 {token} 을(를) 알 수 없습니다.");
                }
            }

            AffinityStage stage = AffinityStage.Uninterested;

            if (!string.IsNullOrEmpty(row["MinStage"]) && !Enum.TryParse(row["MinStage"], out stage))
            {
                errors.Add($"{where} : 관계 단계 {row["MinStage"]} 을(를) 알 수 없습니다.");
            }

            if (!byOwner.TryGetValue(owner.CharacterId, out List<NpcQuestBook.Quest> list))
            {
                list = new List<NpcQuestBook.Quest>();
                byOwner.Add(owner.CharacterId, list);
            }

            list.Add(new NpcQuestBook.Quest(id, row["Title"], kind, requirements, ParseInt(row["RewardCoins"], 0), ParseInt(row["RewardAffinity"], 0),
                rewardItem, rewardAmount, ParseInt(row["Days"], 3), seasons.ToArray(), stage, row["Request"], row["Thanks"], row["RequiredEvent"]));
            special += kind == NpcQuestKind.Special ? 1 : 0;
        }

        StylizedArtAssetFactory.EnsureFolder(QuestFolder);
        List<NpcQuestBook> books = new List<NpcQuestBook>();

        foreach (NpcCharacterData owner in database.GetAlphaCast())
        {
            if (!byOwner.TryGetValue(owner.CharacterId, out List<NpcQuestBook.Quest> quests))
            {
                continue;
            }

            string path = $"{QuestFolder}/NpcQuests_{owner.EnglishName.Replace(" ", string.Empty)}.asset";
            NpcQuestBook book = LoadOrCreateAsset<NpcQuestBook>(path);
            string group = string.IsNullOrEmpty(owner.Ids.questGroupId) ? $"quest_{owner.EnglishName.ToLowerInvariant()}" : owner.Ids.questGroupId;
            book.EditorAssign(group, owner.CharacterId, quests);
            EditorUtility.SetDirty(book);
            books.Add(book);
        }

        foreach (string ownerId in byOwner.Keys.Where(ownerId => !books.Any(book => book.OwnerId == ownerId)))
        {
            errors.Add($"{ownerId} : 마을에 배치된 NPC가 아니라 의뢰 묶음을 만들지 않았습니다.");
        }

        report.AppendLine($"의뢰 묶음 {books.Count}개 · 의뢰 {ids.Count}개 (게시판 {ids.Count - special} · 특별 {special}) ({QuestFolder})");

        foreach (string error in errors)
        {
            report.AppendLine("✗ " + error);
        }

        return books;
    }

    private static IEnumerable<(string itemId, int amount)> ParseItems(string text, string where, List<string> errors) // item_id:3|item_id:1
    {
        foreach (string token in Split(text))
        {
            string[] parts = token.Split(':');

            if (parts.Length != 2 || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int amount) || amount <= 0)
            {
                errors.Add($"{where} : 아이템 칸 {token} 을(를) 읽지 못했습니다 (item_id:수량).");
                continue;
            }

            yield return (parts[0].Trim(), amount);
        }
    }

    private static string[] Split(string text)
    {
        return string.IsNullOrWhiteSpace(text) ? new string[0] : text.Split('|').Select(part => part.Trim()).Where(part => part.Length > 0).ToArray();
    }

    private static int ParseInt(string text, int fallback)
    {
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : fallback;
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

    public static List<NpcQuestBook> LoadBooks()
    {
        List<NpcQuestBook> books = new List<NpcQuestBook>();

        if (!AssetDatabase.IsValidFolder(QuestFolder))
        {
            return books;
        }

        foreach (string guid in AssetDatabase.FindAssets("t:NpcQuestBook", new[] { QuestFolder }))
        {
            NpcQuestBook book = AssetDatabase.LoadAssetAtPath<NpcQuestBook>(AssetDatabase.GUIDToAssetPath(guid));

            if (book != null)
            {
                books.Add(book);
            }
        }

        books.Sort((left, right) => string.CompareOrdinal(left.GroupId, right.GroupId));
        return books;
    }

    // ---------------------------------------------------------------- 한글 글꼴

    public static IEnumerable<string> CollectQuestTexts() // 의뢰 제목 · 대사 · 코드 문구
    {
        foreach (NpcQuestBook book in LoadBooks())
        {
            foreach (NpcQuestBook.Quest quest in book.Quests)
            {
                if (quest == null)
                {
                    continue;
                }

                yield return quest.Title;
                yield return quest.RequestLine;
                yield return quest.ThanksLine;
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
        KoreanFontBuilder.Validate(CollectQuestTexts(), _ => missing++, new StringBuilder());

        if (missing == 0)
        {
            report.AppendLine("한글 글꼴 : 의뢰 글자가 모두 들어 있음");
            return;
        }

        KoreanFontBuilder.Build(NpcContentBuilder.CollectDisplayTexts(database), report); // 대사 + 상점 + 의뢰 글자로 다시 만듦 (GUID 유지)
    }

    // ---------------------------------------------------------------- Scene

    private static string WireScene(List<NpcQuestBook> books)
    {
        StringBuilder report = new StringBuilder();
        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.GetActiveScene();
        NpcManager npcManager = Object.FindFirstObjectByType<NpcManager>(FindObjectsInactive.Include);
        GameUIManager uiManager = Object.FindFirstObjectByType<GameUIManager>(FindObjectsInactive.Include);

        if (scene.path != ScenePath || uiManager == null)
        {
            report.AppendLine($"✗ 게임 Scene({ScenePath})을 열고 다시 실행하세요. 의뢰 데이터만 만들었습니다.");
            return report.ToString();
        }

        if (npcManager == null || npcManager.GetComponent<NpcRelationshipManager>() == null)
        {
            report.AppendLine("✗ NPC 관리자 · 관계 관리자가 없습니다. 8번 · 9번 메뉴를 먼저 실행하세요.");
            return report.ToString();
        }

        // 의뢰 관리자
        NpcQuestManager quests = npcManager.GetComponent<NpcQuestManager>();

        if (quests == null)
        {
            quests = npcManager.gameObject.AddComponent<NpcQuestManager>();
        }

        quests.EditorAssign(books, npcManager, npcManager.GetComponent<NpcRelationshipManager>());
        EditorUtility.SetDirty(quests);
        report.AppendLine($"의뢰 관리자 : {npcManager.name} (의뢰 묶음 {books.Count}개)");

        // 광장 게시판 상호작용
        Transform boardBuilding = npcManager.transform.Find(BoardBuildingPath);

        if (boardBuilding == null)
        {
            report.AppendLine("✗ 광장 게시판(VillageBoard)이 없습니다. 8번 메뉴를 다시 실행하세요.");
        }
        else
        {
            Transform interact = boardBuilding.Find(BoardInteractName);

            if (interact == null)
            {
                interact = new GameObject(BoardInteractName).transform;
                interact.SetParent(boardBuilding, false);
            }

            interact.localPosition = Vector3.zero;
            interact.localRotation = Quaternion.identity;
            interact.gameObject.layer = LayerMask.NameToLayer(InteractableLayer);
            BoxCollider box = interact.GetComponent<BoxCollider>();

            if (box == null)
            {
                box = interact.gameObject.AddComponent<BoxCollider>();
            }

            box.center = new Vector3(0f, 1.1f, 0f);
            box.size = new Vector3(2f, 2.2f, 0.45f);
            box.isTrigger = false;

            if (interact.GetComponent<QuestBoard>() == null)
            {
                interact.gameObject.AddComponent<QuestBoard>();
            }

            EditorUtility.SetDirty(interact.gameObject);
            report.AppendLine("광장 게시판 상호작용 (F - 마을 게시판)");
        }

        // 게시판 창 · 진행 중 의뢰 표시 · 대화 창
        NpcQuestBoardPopup popup = NpcQuestPopupUIBuilder.BuildBoardPopup(out string popupReport);
        report.AppendLine(popupReport);

        if (popup != null)
        {
            SerializedObject serialized = new SerializedObject(uiManager);
            serialized.FindProperty("questBoardPopup").objectReferenceValue = popup;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(uiManager);
        }

        NpcQuestPopupUIBuilder.BuildTracker(out string trackerReport);
        report.AppendLine(trackerReport);
        report.AppendLine(NpcDialogueBuilder.RebuildPopup());
        EditorSceneManager.MarkSceneDirty(scene);
        report.AppendLine("Scene 변경 완료 : Ctrl+S로 저장하세요.");
        return report.ToString();
    }

    // ---------------------------------------------------------------- 검증

    public static string Validate(out int errorCount)
    {
        StringBuilder report = new StringBuilder("[NPC 의뢰 검증]\n");
        int errors = 0;

        void Error(string message)
        {
            errors++;
            report.AppendLine("✗ " + message);
        }

        NpcDatabase database = AssetDatabase.LoadAssetAtPath<NpcDatabase>(NpcContentBuilder.DatabasePath);
        MarketCatalogData catalog = AssetDatabase.LoadAssetAtPath<MarketCatalogData>(MarketContentBuilder.CatalogPath);
        List<NpcQuestBook> books = LoadBooks();

        if (database == null || catalog == null)
        {
            Error("NpcDatabase 또는 판매 가격표가 없습니다. 6번 · 7번 메뉴를 먼저 실행하세요.");
            errorCount = errors;
            report.AppendLine($"결과 : 오류 {errors}개");
            return report.ToString();
        }

        List<NpcShopData> shops = NpcShopBuilder.LoadShops();
        List<CropData> crops = LoadAll<CropData>();
        List<FishData> fish = LoadAll<FishData>();
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        int questCount = 0;
        int specialCount = 0;

        foreach (NpcCharacterData character in database.GetAlphaCast())
        {
            NpcQuestBook book = books.FirstOrDefault(candidate => candidate.OwnerId == character.CharacterId);

            if (book == null)
            {
                Error($"{character.CharacterId} : 의뢰 묶음이 없습니다. 11번 메뉴를 실행하세요.");
                continue;
            }

            if (book.GroupId != character.Ids.questGroupId || !character.HasRole(NpcRole.QuestGiver) || !character.CanInteract(NpcInteraction.Quest))
            {
                Error($"{character.CharacterId} : 의뢰 묶음 ID · 퀘스트 역할 · 의뢰 상호작용이 맞지 않습니다.");
            }

            int[] boardPerSeason = new int[4];
            int specials = 0;

            foreach (NpcQuestBook.Quest quest in book.Quests)
            {
                if (quest == null)
                {
                    Error($"{book.GroupId} : 빈 의뢰가 있습니다.");
                    continue;
                }

                questCount++;
                ValidateQuest(quest, book, shops, catalog, crops, fish, ids, Error);

                if (quest.IsSpecial)
                {
                    specials++;
                    specialCount++;
                    continue;
                }

                for (int season = 0; season < 4; season++)
                {
                    if (quest.IsOfferedIn((SeasonType)season) && quest.RequiredStage == AffinityStage.Uninterested)
                    {
                        boardPerSeason[season]++;
                    }
                }
            }

            for (int season = 0; season < 4; season++)
            {
                if (boardPerSeason[season] == 0)
                {
                    Error($"{character.CharacterId} : {(SeasonType)season} 에 게시판 의뢰가 없습니다.");
                }
            }

            if (specials == 0)
            {
                Error($"{character.CharacterId} : 관계가 깊어지면 열리는 특별 의뢰가 없습니다.");
            }

            report.AppendLine($"{book.GroupId} ({character.DisplayName}) : 의뢰 {book.Quests.Count}개 (특별 {specials}) · 계절별 게시판 의뢰 {string.Join("/", boardPerSeason)}");
        }

        KoreanFontBuilder.Validate(CollectQuestTexts(), Error, report);
        ValidateScene(books, report, Error);
        report.AppendLine($"의뢰 {questCount}개 (게시판 {questCount - specialCount} · 특별 {specialCount})");
        errorCount = errors;
        report.AppendLine(errors == 0 ? "결과 : 오류 0개" : $"결과 : 오류 {errors}개");
        return report.ToString();
    }

    private static void ValidateQuest(NpcQuestBook.Quest quest, NpcQuestBook book, List<NpcShopData> shops, MarketCatalogData catalog, List<CropData> crops, List<FishData> fish, HashSet<string> ids, Action<string> error)
    {
        string id = quest.QuestId;

        if (!QuestIdPattern.IsMatch(id ?? string.Empty) || !ids.Add(id))
        {
            error($"{book.GroupId} : 의뢰 ID가 잘못되었거나 중복입니다 ({id})");
        }

        if (string.IsNullOrWhiteSpace(quest.Title) || string.IsNullOrWhiteSpace(quest.RequestLine) || string.IsNullOrWhiteSpace(quest.ThanksLine))
        {
            error($"{id} : 제목 · 의뢰 대사 · 감사 대사가 비어 있습니다.");
        }

        if (quest.Requirements.Count == 0 || quest.Requirements.Count > 3)
        {
            error($"{id} : 필요 물건은 1~3종이어야 합니다.");
        }

        HashSet<ItemData> seen = new HashSet<ItemData>();

        foreach (NpcQuestBook.Requirement requirement in quest.Requirements)
        {
            if (requirement?.Item == null || !seen.Add(requirement.Item))
            {
                error($"{id} : 필요 물건이 비었거나 중복입니다.");
                continue;
            }

            ValidateSeasons(quest, requirement.Item, shops, crops, fish, error);
        }

        if (quest.RewardAffinity <= 0 || quest.Days < 1 || quest.Days > 7)
        {
            error($"{id} : 보상 호감도는 1 이상, 기한은 1~7일이어야 합니다.");
        }

        if (quest.IsSpecial && quest.RequiredStage == AffinityStage.Uninterested)
        {
            error($"{id} : 특별 의뢰는 관계 단계 조건이 있어야 합니다.");
        }

        if (!quest.IsSpecial)
        {
            ValidateReward(quest, shops, catalog, error);
        }

        if (!string.IsNullOrEmpty(quest.RequiredEventId)) // 94일차: 필요한 이벤트는 같은 NPC의 이벤트여야 한다
        {
            NpcEventBook eventBook = NpcEventBuilder.LoadBooks().FirstOrDefault(candidate => candidate.OwnerId == book.OwnerId);
            NpcEventBook.Event required = eventBook?.Events.FirstOrDefault(candidate => candidate != null && candidate.EventId == quest.RequiredEventId);

            if (required == null)
            {
                error($"{id} : 필요한 이벤트 {quest.RequiredEventId} 가 {book.OwnerId} 의 이벤트에 없습니다. 12번 메뉴를 먼저 실행하세요.");
            }
            else if (required.RequiredStage > quest.RequiredStage)
            {
                error($"{id} : 필요한 이벤트의 관계 단계가 의뢰 단계보다 높습니다.");
            }
        }
    }

    // 작물 · 물고기는 게시되는 계절에 얻을 수 있어야 한다 (그 계절에 파는 NPC 상점이 있으면 허용)
    private static void ValidateSeasons(NpcQuestBook.Quest quest, ItemData item, List<NpcShopData> shops, List<CropData> crops, List<FishData> fish, Action<string> error)
    {
        List<SeasonType> available = null;
        CropData crop = crops.FirstOrDefault(candidate => candidate.HarvestItem == item);
        FishData caught = fish.FirstOrDefault(candidate => candidate.ResultItem == item);

        if (crop != null)
        {
            available = crop.GrowingSeasons.ToList();
        }
        else if (caught != null && caught.Seasons.Count > 0)
        {
            available = caught.Seasons.ToList();
        }

        if (available == null)
        {
            return; // 계절과 관계없이 얻는 물건
        }

        for (int season = 0; season < 4; season++)
        {
            SeasonType value = (SeasonType)season;
            bool sold = shops.Any(shop => shop.Stock.Any(entry => entry != null && entry.Item == item && entry.IsSoldIn(value)));

            if (quest.IsOfferedIn(value) && !available.Contains(value) && !sold)
            {
                error($"{quest.QuestId} : {value} 에는 {item.ItemId} 를 얻을 수 없는데 의뢰가 나옵니다.");
            }
        }
    }

    // 게시판 의뢰 보상이 필요 물건을 상점에서 사서 되파는 값보다 크면 안 된다 (사서 전달하는 반복 이익 방지)
    private static void ValidateReward(NpcQuestBook.Quest quest, List<NpcShopData> shops, MarketCatalogData catalog, Action<string> error)
    {
        bool anyBuyable = false;
        int costFloor = 0;

        foreach (NpcQuestBook.Requirement requirement in quest.Requirements)
        {
            if (requirement?.Item == null)
            {
                continue;
            }

            int cheapest = int.MaxValue;

            foreach (NpcShopData shop in shops)
            {
                foreach (NpcShopData.StockEntry entry in shop.Stock)
                {
                    if (entry != null && entry.Item == requirement.Item)
                    {
                        cheapest = Mathf.Min(cheapest, NpcShopData.DiscountedPrice(entry.Price, shop.MaxDiscountPercent));
                    }
                }
            }

            if (cheapest != int.MaxValue)
            {
                anyBuyable = true;
                costFloor += cheapest * requirement.Amount;
            }
            else
            {
                costFloor += MaxSellPrice(catalog, requirement.Item) * requirement.Amount; // 직접 구하는 물건은 판매 상자 값으로 계산
            }
        }

        int rewardValue = quest.RewardCoins + (quest.RewardItem != null ? MaxSellPrice(catalog, quest.RewardItem) * quest.RewardItemAmount : 0);

        if (anyBuyable && rewardValue > costFloor)
        {
            error($"{quest.QuestId} : 보상 {rewardValue} > 필요 물건 최소 비용 {costFloor} (상점에서 사서 전달하면 이익)");
        }
    }

    private static int MaxSellPrice(MarketCatalogData catalog, ItemData item)
    {
        int best = 0;

        for (int season = 0; season < 4; season++)
        {
            MarketPriceQuote quote = catalog.GetQuote(item, (SeasonType)season);
            best = quote.Sellable ? Mathf.Max(best, quote.UnitPrice) : best;
        }

        return best;
    }

    private static List<T> LoadAll<T>() where T : ScriptableObject
    {
        List<T> result = new List<T>();

        foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { "Assets/_ProjectU/Data" }))
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));

            if (asset != null)
            {
                result.Add(asset);
            }
        }

        return result;
    }

    private static void ValidateScene(List<NpcQuestBook> books, StringBuilder report, Action<string> error)
    {
        if (EditorSceneManager.GetActiveScene().path != ScenePath)
        {
            report.AppendLine("Scene 검사 생략 (게임 Scene이 열려 있지 않음)");
            return;
        }

        NpcManager npcManager = Object.FindFirstObjectByType<NpcManager>(FindObjectsInactive.Include);
        NpcQuestManager quests = npcManager != null ? npcManager.GetComponent<NpcQuestManager>() : null;

        if (quests == null)
        {
            error("NPC 관리자에 의뢰 관리자가 없습니다. 11번 메뉴를 실행하세요.");
            return;
        }

        SerializedObject serialized = new SerializedObject(quests);

        if (serialized.FindProperty("npcManager").objectReferenceValue != npcManager || serialized.FindProperty("relations").objectReferenceValue == null)
        {
            error("의뢰 관리자의 NPC 관리자 · 관계 관리자 연결이 비어 있습니다.");
        }

        HashSet<NpcQuestBook> assigned = new HashSet<NpcQuestBook>(quests.Books.Where(book => book != null));

        foreach (NpcQuestBook book in books.Where(book => !assigned.Contains(book)))
        {
            error($"의뢰 관리자에 {book.GroupId} 이(가) 연결되지 않았습니다.");
        }

        Transform interact = npcManager.transform.Find(BoardBuildingPath + "/" + BoardInteractName);
        BoxCollider box = interact != null ? interact.GetComponent<BoxCollider>() : null;

        if (interact == null || interact.GetComponent<QuestBoard>() == null || box == null || box.isTrigger || interact.gameObject.layer != LayerMask.NameToLayer(InteractableLayer))
        {
            error("광장 게시판에 상호작용(QuestBoard · 상호작용 레이어 충돌체)이 없습니다.");
        }

        GameUIManager uiManager = Object.FindFirstObjectByType<GameUIManager>(FindObjectsInactive.Include);
        NpcQuestBoardPopup popup = uiManager != null ? uiManager.QuestBoardPopup : null;

        if (popup == null)
        {
            error("GameUIManager에 게시판 창이 연결되지 않았습니다.");
        }
        else
        {
            SerializedObject popupSerialized = new SerializedObject(popup);

            foreach (string field in NpcQuestPopupUIBuilder.PopupFields.Where(field => popupSerialized.FindProperty(field).objectReferenceValue == null))
            {
                error($"게시판 창의 {field} 연결이 비어 있습니다.");
            }
        }

        NpcQuestTrackerUI tracker = Object.FindFirstObjectByType<NpcQuestTrackerUI>(FindObjectsInactive.Include);

        if (tracker == null)
        {
            error("진행 중 의뢰 표시가 없습니다.");
        }
        else
        {
            SerializedObject trackerSerialized = new SerializedObject(tracker);

            foreach (string field in NpcQuestPopupUIBuilder.TrackerFields.Where(field => trackerSerialized.FindProperty(field).objectReferenceValue == null))
            {
                error($"진행 중 의뢰 표시의 {field} 연결이 비어 있습니다.");
            }
        }

        NpcDialoguePopup dialogue = uiManager != null ? uiManager.NpcDialoguePopup : null;

        if (dialogue == null || new SerializedObject(dialogue).FindProperty("questButton").objectReferenceValue == null)
        {
            error("NPC 대화 창에 의뢰 전달 버튼이 없습니다. 11번 메뉴를 다시 실행하세요.");
        }

        report.AppendLine($"Scene : 의뢰 관리자 의뢰 묶음 {assigned.Count}개 · 게시판 · 게시판 창 · 의뢰 표시 연결");
    }
}
