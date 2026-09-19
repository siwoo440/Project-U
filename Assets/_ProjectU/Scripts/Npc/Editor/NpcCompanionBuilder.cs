using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// 109일차: NPC 동료
// 1. NpcCompanions.csv → 동료 정보(NpcCompanionBook) : 역할(전투 · 채집 · 치유) · 필요한 관계 단계 · 힘 · 간격 · 채집 물건 · 대사
// 2. 게임 Scene : NPC 관리자 오브젝트에 동료 관리자(NpcCompanionManager) 연결
// 3. NPC 대화 창을 다시 만들어 "함께 가자 · 이제 돌아가" 버튼 추가, 화면 왼쪽 아래 동료 표시
// 여러 번 실행해도 같은 결과가 나온다.
public static class NpcCompanionBuilder
{
    public const string CompanionCsv = NpcContentBuilder.SourceFolder + "/NpcCompanions.csv";
    public const string BookFolder = NpcContentBuilder.DataFolder + "/Companions";
    public const string BookPath = BookFolder + "/NpcCompanionBook.asset";
    private const string ScenePath = "Assets/_ProjectU/Scenes/20_Gameplay.unity";
    private const string DialogTitle = "Project U NPC 동료";
    private const string CanvasName = "=== GameplayCanvas ===";
    public const string HudName = "LP_CompanionHUD";
    private const int MinimumPerRole = 2;

    // ---------------------------------------------------------------- 메뉴

    public static string BuildAll(bool saveScene)
    {
        StringBuilder report = new StringBuilder("[NPC 동료 만들기]\n");
        NpcCompanionBook book = BuildBook(report);

        if (EditorSceneManager.GetActiveScene().path != ScenePath)
        {
            report.AppendLine($"✗ 게임 Scene({ScenePath})을 열고 다시 실행하세요. 동료 정보만 만들었습니다.");
            return report.ToString();
        }

        report.AppendLine(ConnectManager(book));
        report.AppendLine(NpcDialogueBuilder.RebuildPopup());
        report.AppendLine(BuildHud());
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        if (saveScene)
        {
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            report.AppendLine("게임 Scene 저장 완료");
        }

        report.Append(Validate(out _));
        return report.ToString();
    }

    // ---------------------------------------------------------------- 동료 정보

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

    private static NpcCompanionBook BuildBook(StringBuilder report)
    {
        List<string> errors = new List<string>();
        List<NpcCsv.Row> rows = NpcCsv.Read(CompanionCsv, errors);
        Dictionary<string, ItemData> items = LoadItemsById();
        List<NpcCompanionBook.Entry> entries = new List<NpcCompanionBook.Entry>();

        foreach (NpcCsv.Row row in rows)
        {
            string where = $"NpcCompanions.csv {row.LineNumber}줄";

            if (!Enum.TryParse(row["Role"], out NpcCompanionRole role) || !Enum.TryParse(row["RequiredStage"], out AffinityStage stage))
            {
                errors.Add($"{where} : 역할 · 관계 단계를 읽을 수 없습니다.");
                continue;
            }

            NpcCompanionBook.Entry entry = new NpcCompanionBook.Entry
            {
                characterId = row["CharacterID"],
                role = role,
                requiredStage = stage,
                power = float.TryParse(row["Power"], NumberStyles.Float, CultureInfo.InvariantCulture, out float power) ? power : 0f,
                interval = float.TryParse(row["Interval"], NumberStyles.Float, CultureInfo.InvariantCulture, out float interval) ? interval : 0f,
                joinLine = row["JoinLine"],
                leaveLine = row["LeaveLine"],
                nightLine = row["NightLine"],
                refuseLine = row["RefuseLine"],
                roleLine = row["RoleLine"],
                idleLines = row["IdleLines"].Split('|').Select(line => line.Trim()).Where(line => line.Length > 0).ToList()
            };

            foreach (string itemId in row["Loot"].Split('|').Select(id => id.Trim()).Where(id => id.Length > 0))
            {
                if (items.TryGetValue(itemId, out ItemData item))
                {
                    entry.lootItems.Add(item);
                }
                else
                {
                    errors.Add($"{where} : 채집 물건 {itemId}이(가) 없습니다.");
                }
            }

            entries.Add(entry);
        }

        StylizedArtAssetFactory.EnsureFolder(BookFolder);
        NpcCompanionBook book = AssetDatabase.LoadAssetAtPath<NpcCompanionBook>(BookPath);

        if (book == null)
        {
            book = ScriptableObject.CreateInstance<NpcCompanionBook>();
            AssetDatabase.CreateAsset(book, BookPath);
        }

        book.EditorAssign(entries);
        EditorUtility.SetDirty(book);
        AssetDatabase.SaveAssets();

        foreach (string error in errors)
        {
            report.AppendLine("✗ " + error);
        }

        report.AppendLine($"동료 정보 {entries.Count}명 : " + string.Join(" · ", Enum.GetValues(typeof(NpcCompanionRole)).Cast<NpcCompanionRole>().Select(role => $"{NpcCompanionBook.RoleName(role)} {entries.Count(entry => entry.role == role)}")));
        return book;
    }

    // ---------------------------------------------------------------- Scene

    private static string ConnectManager(NpcCompanionBook book)
    {
        NpcManager npcManager = Object.FindFirstObjectByType<NpcManager>(FindObjectsInactive.Include);

        if (npcManager == null)
        {
            return "✗ NPC 관리자(NpcManager)가 없습니다. 8번 메뉴를 먼저 실행하세요.";
        }

        NpcCompanionManager manager = npcManager.GetComponent<NpcCompanionManager>();

        if (manager == null)
        {
            manager = npcManager.gameObject.AddComponent<NpcCompanionManager>();
        }

        manager.EditorAssign(book);
        EditorUtility.SetDirty(manager);
        return $"동료 관리자 연결 ({npcManager.name})";
    }

    private static string BuildHud()
    {
        GameObject canvas = GameObject.Find(CanvasName);

        if (canvas == null)
        {
            return "✗ 게임 화면 Canvas가 없습니다.";
        }

        UIBuildKit.RemoveExisting(canvas.transform, HudName);
        RectTransform root = UIBuildKit.CreateRect(canvas.transform, HudName);
        UIBuildKit.Place(root, Vector2.zero, Vector2.zero, new Vector2(16f, 46f), new Vector2(340f, 46f));
        Image panel = UIBuildKit.CreateImage(root, "LP_Panel", UISpriteFactory.Pill, ProjectUUIPalette.HudPanel);
        panel.type = Image.Type.Sliced;
        UIBuildKit.Stretch(panel.rectTransform);
        Image frame = UIBuildKit.CreateImage(panel.rectTransform, "LP_PortraitFrame", UISpriteFactory.CircleSprite, Color.white);
        UIBuildKit.Place(frame.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(5f, 0f), new Vector2(38f, 38f));
        Image portrait = UIBuildKit.CreateImage(frame.rectTransform, "LP_Portrait", null, Color.white);
        portrait.preserveAspect = true;
        UIBuildKit.Place(portrait.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(34f, 34f));
        TMP_Text label = UIBuildKit.CreateText(panel.rectTransform, "LP_Label", "동료", 16f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, ProjectUUIPalette.TextPrimary);
        UIBuildKit.SetOffsets(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(52f, 0f), new Vector2(-10f, 0f));
        label.richText = true;

        foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
        {
            graphic.raycastTarget = false;
        }

        NpcCompanionHUD hud = root.gameObject.AddComponent<NpcCompanionHUD>();
        hud.EditorAssign(panel.gameObject, portrait, frame, label);
        panel.gameObject.SetActive(false);
        EditorUtility.SetDirty(hud);
        return "화면 동료 표시 (왼쪽 아래, 동료가 있을 때만)";
    }

    // ---------------------------------------------------------------- 검증

    public static IEnumerable<string> CollectTexts(NpcCompanionBook book)
    {
        yield return NpcCompanionManager.AlreadyMessage;
        yield return NpcCompanionManager.NightMessage;
        yield return NpcCompanionManager.JoinedMessage + " 전투 채집 치유";
        yield return NpcCompanionManager.LeftMessage;
        yield return "함께 가자 이제 돌아가 동료 함께 다닐 수 없는 이웃이에요.";

        if (book == null)
        {
            yield break;
        }

        foreach (NpcCompanionBook.Entry entry in book.Entries)
        {
            yield return entry.joinLine;
            yield return entry.leaveLine;
            yield return entry.nightLine;
            yield return entry.refuseLine;
            yield return entry.roleLine;

            foreach (string line in entry.idleLines)
            {
                yield return line;
            }

            foreach (ItemData item in entry.lootItems.Where(item => item != null))
            {
                yield return item.DisplayName;
            }
        }
    }

    public static string Validate(out int errorCount)
    {
        StringBuilder report = new StringBuilder("[NPC 동료 검증]\n");
        int errors = 0;

        void Error(string message)
        {
            errors++;
            report.AppendLine("✗ " + message);
        }

        NpcCompanionBook book = AssetDatabase.LoadAssetAtPath<NpcCompanionBook>(BookPath);
        NpcDatabase database = AssetDatabase.LoadAssetAtPath<NpcDatabase>(NpcContentBuilder.DatabasePath);

        if (book == null || database == null)
        {
            Error("동료 정보 또는 NPC 데이터가 없습니다. 25번 메뉴를 실행하세요.");
            errorCount = errors;
            report.AppendLine($"결과 : 오류 {errors}개");
            return report.ToString();
        }

        List<NpcCharacterData> cast = database.GetPlacedCast(); // 110일차: 이야기 차수가 아닌 후보도 미리 넣을 수 있음 (잠김)
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (NpcCompanionBook.Entry entry in book.Entries)
        {
            string who = entry.characterId;

            if (!seen.Add(who))
            {
                Error($"{who} : 동료 정보가 두 번 있습니다.");
            }

            if (!cast.Any(character => character.CharacterId == who))
            {
                Error($"{who} : 섬에 사는 NPC가 아닙니다.");
            }

            if (entry.power <= 0f || entry.interval <= 0.2f)
            {
                Error($"{who} : 힘 · 간격이 비었습니다.");
            }

            if (entry.role == NpcCompanionRole.Gatherer && (entry.lootItems.Count == 0 || entry.lootItems.Any(item => item == null) || !entry.roleLine.Contains("{item}")))
            {
                Error($"{who} : 채집형인데 찾아 줄 물건이나 역할 대사({{item}})가 없습니다.");
            }

            if (entry.requiredStage < AffinityStage.Curious)
            {
                Error($"{who} : 처음 만나자마자 동료가 됩니다 (호기심 이상 필요).");
            }

            if (string.IsNullOrWhiteSpace(entry.joinLine) || string.IsNullOrWhiteSpace(entry.leaveLine) || string.IsNullOrWhiteSpace(entry.nightLine) || string.IsNullOrWhiteSpace(entry.refuseLine) || string.IsNullOrWhiteSpace(entry.roleLine) || entry.idleLines.Count < 2)
            {
                Error($"{who} : 동료 대사가 비었습니다 (합류 · 헤어짐 · 밤 · 거절 · 역할 · 혼잣말 2개 이상).");
            }
        }

        foreach (NpcCompanionRole role in Enum.GetValues(typeof(NpcCompanionRole)))
        {
            if (book.Entries.Count(entry => entry.role == role && database.TryGet(entry.characterId, out NpcCharacterData character) && NpcCompanionManager.IsUnlocked(character)) < MinimumPerRole)
            {
                Error($"지금 데려갈 수 있는 {NpcCompanionBook.RoleName(role)} 동료가 {MinimumPerRole}명보다 적습니다.");
            }
        }

        KoreanFontBuilder.Validate(CollectTexts(book), Error, new StringBuilder());

        if (EditorSceneManager.GetActiveScene().path == ScenePath)
        {
            NpcManager npcManager = Object.FindFirstObjectByType<NpcManager>(FindObjectsInactive.Include);
            NpcCompanionManager manager = npcManager != null ? npcManager.GetComponent<NpcCompanionManager>() : null;

            if (manager == null || manager.Book != book)
            {
                Error("NPC 관리자에 동료 관리자가 없거나 동료 정보가 연결되지 않았습니다. 25번 메뉴를 실행하세요.");
            }

            NpcDialoguePopup popup = Object.FindFirstObjectByType<NpcDialoguePopup>(FindObjectsInactive.Include);

            if (popup == null || new SerializedObject(popup).FindProperty("companionButton").objectReferenceValue == null)
            {
                Error("NPC 대화 창에 '함께 가자' 버튼이 없습니다. 25번(9번) 메뉴를 실행하세요.");
            }

            NpcCompanionHUD hud = Object.FindFirstObjectByType<NpcCompanionHUD>(FindObjectsInactive.Include);

            if (hud == null || new SerializedObject(hud).FindProperty("label").objectReferenceValue == null)
            {
                Error("화면 동료 표시가 없습니다. 25번 메뉴를 실행하세요.");
            }
        }
        else
        {
            report.AppendLine("Scene 검사 생략 (게임 Scene이 열려 있지 않음)");
        }

        string Describe(NpcCompanionBook.Entry entry)
        {
            bool found = database.TryGet(entry.characterId, out NpcCharacterData character);
            string locked = found && !NpcCompanionManager.IsUnlocked(character) ? " · 잠김" : string.Empty;
            return $"{(found ? character.DisplayName : entry.characterId)}({NpcCompanionBook.RoleName(entry.role)}{locked})";
        }

        int lockedCount = book.Entries.Count(entry => database.TryGet(entry.characterId, out NpcCharacterData character) && !NpcCompanionManager.IsUnlocked(character));
        report.AppendLine($"동료 {book.Entries.Count}명 (이야기가 붙기 전이라 잠긴 후보 {lockedCount}명) : " + string.Join(" · ", book.Entries.Select(Describe)));
        errorCount = errors;
        report.AppendLine(errors == 0 ? "결과 : 오류 0개" : $"결과 : 오류 {errors}개");
        return report.ToString();
    }
}
