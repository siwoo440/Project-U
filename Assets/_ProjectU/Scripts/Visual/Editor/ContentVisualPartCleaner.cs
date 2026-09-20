using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

// 120일차: 외형 카드(Visual Profile) 부품만 남은 Prefab 정리.
// 동물 · 몬스터처럼 카드를 쓰지 않는 Prefab에서 ContentVisualIdentity만 지우면
// ContentVisualProfileBinder가 실행 중에 "Content Identity가 없습니다" 오류를 낸다.
// 서로를 요구(RequireComponent)하므로 반드시 바깥쪽부터 차례대로 지워야 한다.
public static class ContentVisualPartCleaner
{
    private const string PrefabFolder = "Assets/_ProjectU/Prefabs";

    public static string Clean() // 콘텐츠 자동 적용에서 사용
    {
        List<string> cleaned = new List<string>();

        foreach (string path in OrphanPrefabPaths())
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);

            try
            {
                RemoveParts(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                cleaned.Add(Path.GetFileNameWithoutExtension(path));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        if (cleaned.Count > 0)
        {
            AssetDatabase.SaveAssets();
        }

        return cleaned.Count > 0
            ? $"외형 카드 부품만 남은 Prefab {cleaned.Count}개 정리 ({string.Join(" · ", cleaned)})"
            : "외형 카드 부품 정리 : 고칠 Prefab 없음";
    }

    public static void RemoveParts(GameObject root) // 외형 카드 부품 정리 (서로 요구하므로 순서대로 지운다)
    {
        foreach (ContentVisualDataSourceBinder binder in root.GetComponentsInChildren<ContentVisualDataSourceBinder>(true))
        {
            Object.DestroyImmediate(binder, true);
        }

        foreach (ContentVisualProfileBinder binder in root.GetComponentsInChildren<ContentVisualProfileBinder>(true))
        {
            Object.DestroyImmediate(binder, true);
        }

        foreach (ContentVisualRoot visual in root.GetComponentsInChildren<ContentVisualRoot>(true))
        {
            Object.DestroyImmediate(visual, true);
        }

        foreach (ContentVisualIdentity identity in root.GetComponentsInChildren<ContentVisualIdentity>(true))
        {
            Object.DestroyImmediate(identity, true);
        }
    }

    public static string Validate(out int errorCount) // 전체 콘텐츠 검사에서 사용
    {
        StringBuilder report = new StringBuilder("[외형 카드 부품 검증]\n");
        string[] orphans = OrphanPrefabPaths().ToArray();
        errorCount = orphans.Length;

        foreach (string path in orphans)
        {
            report.AppendLine($"✗ {Path.GetFileNameWithoutExtension(path)} : 외형 카드 부품(Profile Binder)만 남아 실행 중 오류가 납니다. ({path})");
        }

        if (errorCount == 0)
        {
            report.AppendLine($"외형 카드를 쓰지 않는 Prefab 정리 상태 : 문제 없음");
        }

        return report.ToString();
    }

    private static IEnumerable<string> OrphanPrefabPaths() // 카드 이름표 없이 연결 부품만 남은 Prefab 찾기
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null)
            {
                continue;
            }

            bool hasBinder = prefab.GetComponentsInChildren<ContentVisualProfileBinder>(true).Length > 0
                || prefab.GetComponentsInChildren<ContentVisualDataSourceBinder>(true).Length > 0;

            if (hasBinder && prefab.GetComponentsInChildren<ContentVisualIdentity>(true).Length == 0)
            {
                yield return path;
            }
        }
    }
}
