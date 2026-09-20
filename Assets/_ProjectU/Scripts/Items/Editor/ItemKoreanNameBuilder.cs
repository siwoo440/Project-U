using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;

// 115일차: 아이템 한글 이름 (ItemKoreanNames.csv → ItemData.koreanName)
// NPC 대사 · 의뢰 · 선물 창처럼 한글로 말하는 곳에서 "TROUT 찾았다냥" 대신 "송어 찾았다냥"이 되게 한다.
// 가방 · 제작 · 상점 같은 영어 화면은 그대로 표시 이름(DisplayName)을 쓴다.
public static class ItemKoreanNameBuilder
{
    public const string SourcePath = "Assets/_ProjectU/Data/Items/ItemKoreanNames.csv";

    public static string Apply() // 콘텐츠 자동 적용에서 사용
    {
        Dictionary<string, string> names = ReadNames(new List<string>());
        int changed = 0;

        foreach (ItemData item in AllItems())
        {
            if (!names.TryGetValue(item.ItemId, out string korean))
            {
                continue;
            }

            SerializedObject serialized = new SerializedObject(item);
            SerializedProperty property = serialized.FindProperty("koreanName");

            if (property == null || property.stringValue == korean)
            {
                continue;
            }

            property.stringValue = korean;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(item);
            changed++;
        }

        AssetDatabase.SaveAssets();
        return $"아이템 한글 이름 {names.Count}개 · 바뀐 아이템 {changed}개";
    }

    public static string Validate(out int errorCount)
    {
        StringBuilder report = new StringBuilder("[아이템 한글 이름 검증]\n");
        int errors = 0;

        void Error(string message)
        {
            errors++;
            report.AppendLine("✗ " + message);
        }

        List<string> problems = new List<string>();
        Dictionary<string, string> names = ReadNames(problems);
        problems.ForEach(Error);
        List<ItemData> items = AllItems().ToList();
        HashSet<string> ids = new HashSet<string>(items.Select(item => item.ItemId), StringComparer.Ordinal);

        foreach (ItemData item in items)
        {
            if (!names.TryGetValue(item.ItemId, out string korean))
            {
                Error($"{item.ItemId} ({item.DisplayName}) : {SourcePath}에 한글 이름이 없습니다.");
            }
            else if (item.KoreanName != korean)
            {
                Error($"{item.ItemId} : 한글 이름이 아직 적용되지 않았습니다. 콘텐츠 자동 적용을 기다리세요.");
            }
        }

        foreach (string id in names.Keys.Where(id => !ids.Contains(id)))
        {
            Error($"{SourcePath}의 {id} 아이템이 없습니다.");
        }

        foreach (IGrouping<string, string> same in names.Values.GroupBy(name => name).Where(group => group.Count() > 1))
        {
            Error($"한글 이름 '{same.Key}'이(가) 여러 아이템에 쓰였습니다.");
        }

        KoreanFontBuilder.Validate(names.Values, Error, new StringBuilder());
        report.AppendLine($"아이템 {items.Count}개 · 한글 이름 {names.Count}개 (예 : {string.Join(", ", names.Take(4).Select(pair => $"{pair.Key}={pair.Value}"))})");
        report.AppendLine($"결과 : 오류 {errors}개");
        errorCount = errors;
        return report.ToString();
    }

    public static IEnumerable<ItemData> AllItems() // 게임 아이템 전체
    {
        return AssetDatabase.FindAssets("t:ItemData", new[] { "Assets/_ProjectU" })
            .Select(guid => AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(item => item != null && !string.IsNullOrEmpty(item.ItemId));
    }

    private static Dictionary<string, string> ReadNames(List<string> problems)
    {
        Dictionary<string, string> names = new Dictionary<string, string>(StringComparer.Ordinal);

        if (!File.Exists(SourcePath))
        {
            problems.Add($"{SourcePath} 파일이 없습니다.");
            return names;
        }

        string[] lines = File.ReadAllLines(SourcePath, Encoding.UTF8);

        for (int index = 1; index < lines.Length; index++)
        {
            string line = lines[index].Trim();

            if (line.Length == 0)
            {
                continue;
            }

            int comma = line.IndexOf(',');
            string id = comma > 0 ? line.Substring(0, comma).Trim() : string.Empty;
            string korean = comma > 0 ? line.Substring(comma + 1).Trim() : string.Empty;

            if (id.Length == 0 || korean.Length == 0)
            {
                problems.Add($"{SourcePath} {index + 1}번째 줄 : 아이템 ID와 한글 이름이 필요합니다.");
            }
            else if (!names.TryAdd(id, korean))
            {
                problems.Add($"{SourcePath} : {id}가 두 번 있습니다.");
            }
        }

        return names;
    }
}
