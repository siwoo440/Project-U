using System.Collections.Generic;
using System.IO;
using System.Text;

// 89일차: NPC 원본 CSV 읽기 (쉼표 · 큰따옴표 · 줄바꿈이 들어간 칸 지원)
public static class NpcCsv
{
    public sealed class Row
    {
        private readonly Dictionary<string, string> values;

        public Row(Dictionary<string, string> values, int lineNumber)
        {
            this.values = values;
            LineNumber = lineNumber;
        }

        public int LineNumber { get; }

        public string this[string column] => values.TryGetValue(column, out string value) ? value.Trim() : string.Empty;
    }

    public static List<Row> Read(string path, List<string> errors)
    {
        List<Row> rows = new List<Row>();

        if (!File.Exists(path))
        {
            errors.Add($"CSV 파일이 없습니다: {path}");
            return rows;
        }

        List<List<string>> records = Parse(File.ReadAllText(path, Encoding.UTF8));

        if (records.Count == 0)
        {
            errors.Add($"CSV 내용이 비어 있습니다: {path}");
            return rows;
        }

        List<string> header = records[0];

        for (int index = 1; index < records.Count; index++)
        {
            List<string> record = records[index];

            if (record.Count == 1 && string.IsNullOrWhiteSpace(record[0]))
            {
                continue; // 빈 줄
            }

            Dictionary<string, string> values = new Dictionary<string, string>();

            for (int column = 0; column < header.Count; column++)
            {
                values[header[column].Trim().TrimStart('\uFEFF')] = column < record.Count ? record[column] : string.Empty;
            }

            rows.Add(new Row(values, index + 1));
        }

        return rows;
    }

    private static List<List<string>> Parse(string text)
    {
        List<List<string>> records = new List<List<string>>();
        List<string> record = new List<string>();
        StringBuilder cell = new StringBuilder();
        bool quoted = false;

        for (int index = 0; index < text.Length; index++)
        {
            char current = text[index];

            if (quoted)
            {
                if (current == '"')
                {
                    if (index + 1 < text.Length && text[index + 1] == '"')
                    {
                        cell.Append('"'); // "" → "
                        index++;
                    }
                    else
                    {
                        quoted = false;
                    }
                }
                else
                {
                    cell.Append(current);
                }

                continue;
            }

            switch (current)
            {
                case '"':
                    quoted = true;
                    break;
                case ',':
                    record.Add(cell.ToString());
                    cell.Clear();
                    break;
                case '\r':
                    break;
                case '\n':
                    record.Add(cell.ToString());
                    cell.Clear();
                    records.Add(record);
                    record = new List<string>();
                    break;
                default:
                    cell.Append(current);
                    break;
            }
        }

        if (cell.Length > 0 || record.Count > 0)
        {
            record.Add(cell.ToString());
            records.Add(record);
        }

        return records;
    }
}
