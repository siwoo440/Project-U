using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

// 89일차: 한글 TMP 글꼴 (Noto Sans KR Regular, SIL OFL 1.1)
// 자주 쓰는 한글 2,350자 + 한글 자모 + 게임 대사에 쓰는 글자로 고정 글꼴 Atlas를 만들고 TMP 공통 대체 글꼴로 등록한다.
// 글자가 바뀌지 않았으면 다시 만들지 않는다 (git 변경 최소화).
public static class KoreanFontBuilder
{
    public const string FontFolder = "Assets/_ProjectU/UI/Fonts/NotoSansKR";
    public const string FontFilePath = FontFolder + "/NotoSansKR-VF.ttf";
    public const string FontAssetPath = FontFolder + "/NotoSansKR-Regular SDF.asset";
    public const string CommonCharactersPath = FontFolder + "/KoreanCommonCharacters.txt"; // KS X 1001 한글 2,350자 + 자모

    private const int RegularFaceIndex = 4 << 16; // 가변 글꼴의 4번째 이름 있는 굵기 = Regular
    private const int SamplingPointSize = 32;
    private const int AtlasPadding = 4;
    private const int AtlasSize = 1024;

    // 대사 밖에서도 자주 쓰는 글자 (영문 · 숫자 · 문장 부호)
    private static readonly string BaseCharacters = BuildAscii() + "…·“”‘’~!?※○×「」『』—–→" + ReadCommonCharacters();

    public static string Build(IEnumerable<string> texts, StringBuilder report)
    {
        string characters = CollectCharacters(texts);

        if (!File.Exists(FontFilePath))
        {
            report.AppendLine($"✗ 한글 글꼴 파일이 없습니다: {FontFilePath}");
            return null;
        }

        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);

        if (existing != null && CountMissing(existing, characters) == 0 && existing.characterTable.Count == characters.Length)
        {
            RegisterFallback(existing);
            report.AppendLine($"한글 글꼴 : 글자 {characters.Length}자, 변경 없음");
            return characters;
        }

        TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(FontFilePath, RegularFaceIndex, SamplingPointSize, AtlasPadding, GlyphRenderMode.SDFAA, AtlasSize, AtlasSize);

        if (font == null)
        {
            report.AppendLine("✗ 한글 글꼴을 불러오지 못했습니다.");
            return null;
        }

        if (font.faceInfo.styleName != "Regular")
        {
            report.AppendLine($"✗ 한글 글꼴 굵기가 Regular가 아닙니다: {font.faceInfo.styleName}");
        }

        font.TryAddCharacters(characters, out string missing);
        font.atlasPopulationMode = AtlasPopulationMode.Static;
        font.name = Path.GetFileNameWithoutExtension(FontAssetPath);
        SaveFontAsset(font, existing);
        TMP_FontAsset saved = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        RegisterFallback(saved);

        string missingText = string.IsNullOrEmpty(missing) ? string.Empty : $", 글꼴에 없는 글자 {missing.Length}자 ({missing})";
        report.AppendLine($"한글 글꼴 : 글자 {characters.Length}자, Atlas {saved.atlasTextures.Length}장 ({AtlasSize}×{AtlasSize}){missingText}");
        return characters;
    }

    public static void Validate(IEnumerable<string> texts, System.Action<string> error, StringBuilder report)
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);

        if (font == null)
        {
            error("한글 글꼴 Asset이 없습니다. NPC 생성 도구를 실행하세요.");
            return;
        }

        string characters = CollectCharacters(texts);
        int missing = CountMissing(font, characters);

        if (missing > 0)
        {
            error($"한글 글꼴에 대사 글자 {missing}자가 없습니다. NPC 생성 도구를 다시 실행하세요.");
        }

        if (font.faceInfo.styleName != "Regular")
        {
            error($"한글 글꼴 굵기가 Regular가 아닙니다: {font.faceInfo.styleName}");
        }

        if (TMP_Settings.instance == null || TMP_Settings.fallbackFontAssets == null || !TMP_Settings.fallbackFontAssets.Contains(font))
        {
            error("한글 글꼴이 TMP 공통 대체 글꼴(TMP Settings)에 등록되지 않았습니다.");
        }

        report.AppendLine($"한글 글꼴 : {font.characterTable.Count}자 (필요 {characters.Length}자, 없음 {missing}자)");
    }

    public static string CollectCharacters(IEnumerable<string> texts)
    {
        SortedSet<char> set = new SortedSet<char>(BaseCharacters);

        foreach (string text in texts)
        {
            if (string.IsNullOrEmpty(text))
            {
                continue;
            }

            foreach (char character in text)
            {
                if (IsDrawable(character))
                {
                    set.Add(character);
                }
            }
        }

        return new string(set.ToArray());
    }

    private static bool IsDrawable(char character)
    {
        if (char.IsControl(character) || char.IsSurrogate(character) || character == '\uFEFF' || character == '\uFE0F')
        {
            return false; // 제어 문자 · 이모지 조각
        }

        return character < 0x2600 || character >= 0x3000; // 기호·이모지 영역(2600~2FFF)은 글꼴에 없음
    }

    private static int CountMissing(TMP_FontAsset font, string characters)
    {
        HashSet<uint> present = new HashSet<uint>(font.characterTable.Select(entry => entry.unicode));
        return characters.Count(character => !present.Contains(character));
    }

    private static void SaveFontAsset(TMP_FontAsset font, TMP_FontAsset existing)
    {
        if (existing == null)
        {
            AssetDatabase.CreateAsset(font, FontAssetPath);
            AddSubAssets(font, font);
            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();
            return;
        }

        // 기존 Asset 안의 내용만 바꿔 GUID(다른 곳의 연결)를 유지한다
        foreach (Object subAsset in AssetDatabase.LoadAllAssetRepresentationsAtPath(FontAssetPath))
        {
            if (subAsset is Texture2D || subAsset is Material)
            {
                AssetDatabase.RemoveObjectFromAsset(subAsset);
                Object.DestroyImmediate(subAsset, true);
            }
        }

        EditorUtility.CopySerialized(font, existing);
        AddSubAssets(font, existing);
        font.atlasTextures = null; // 임시 글꼴을 지울 때 옮겨 간 Atlas·Material이 함께 지워지지 않게 연결 해제
        font.material = null;
        Object.DestroyImmediate(font);
        existing.ReadFontAssetDefinition();
        EditorUtility.SetDirty(existing);
        AssetDatabase.SaveAssets();
    }

    private static void AddSubAssets(TMP_FontAsset source, TMP_FontAsset owner)
    {
        for (int index = 0; index < source.atlasTextures.Length; index++)
        {
            Texture2D atlas = source.atlasTextures[index];
            atlas.name = $"{owner.name} Atlas {index}";
            AssetDatabase.AddObjectToAsset(atlas, owner);
        }

        source.material.name = $"{owner.name} Material";
        AssetDatabase.AddObjectToAsset(source.material, owner);
    }

    private static void RegisterFallback(TMP_FontAsset font)
    {
        TMP_Settings settings = TMP_Settings.instance;

        if (settings == null || font == null)
        {
            return;
        }

        SerializedObject serialized = new SerializedObject(settings);
        SerializedProperty list = serialized.FindProperty("m_fallbackFontAssets");
        bool found = false;

        for (int index = list.arraySize - 1; index >= 0; index--)
        {
            Object entry = list.GetArrayElementAtIndex(index).objectReferenceValue;

            if (entry == null)
            {
                list.DeleteArrayElementAtIndex(index); // 끊어진 연결 정리
            }
            else if (entry == font)
            {
                found = true;
            }
        }

        if (!found)
        {
            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = font;
        }

        if (serialized.ApplyModifiedPropertiesWithoutUndo())
        {
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }
    }

    private static string ReadCommonCharacters()
    {
        return File.Exists(CommonCharactersPath) ? File.ReadAllText(CommonCharactersPath, Encoding.UTF8).Replace("\n", string.Empty).Replace("\r", string.Empty) : string.Empty;
    }

    private static string BuildAscii()
    {
        StringBuilder ascii = new StringBuilder();

        for (char character = ' '; character <= '~'; character++)
        {
            ascii.Append(character);
        }

        return ascii.ToString();
    }
}
