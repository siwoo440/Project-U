using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// 98일차: 건축물 · 채집 자원 외형을 외형 설정(Visual Profile) 카드로 정리하는 도구
// 1. 건축물 20종(설치본 · 미리보기)과 나무 · 돌 자원마다 외형 설정 카드를 만든다
//    처음 만들 때는 지금 Prefab에 붙은 저폴리 모델과 위치 · 회전 · 크기를 그대로 옮겨 모양이 바뀌지 않게 한다
// 2. 카드가 이미 있으면 카드가 기준이다 : Prefab의 모델 · 위치가 카드와 다르면 카드대로 바꿔 끼운다
// 3. Prefab마다 콘텐츠 ID(ContentVisualIdentity)를 붙여 ID → 카드로 이어지게 한다 (structure_ → visual_buildable_)
// 4. 돌벽 설치 미리보기가 나무 벽 미리보기를 쓰던 연결을 고친다
// 5. GameDataRegistry 등록 목록을 다시 모으고 검사한다
// 여러 번 실행해도 같은 값이 된다. Scene은 바꾸지 않는다 (Scene의 자원은 Prefab을 따라감).
public static class BuildableVisualProfileBuilder
{
    private const string DialogTitle = "Project U 건축물 · 자원 외형";
    private const string BuildableFolder = "Assets/_ProjectU/Data/VisualProfiles/Buildables/";
    private const string ResourceFolder = "Assets/_ProjectU/Data/VisualProfiles/Resources/";
    private const string BuildRecipeRoot = "Assets/_ProjectU/Data/Building";
    private const string StoneWallRecipeId = "structure_stone_wall";
    private const string StoneWallPreviewPath = "Assets/_ProjectU/Prefabs/Building/Day73/StoneWallPreview.prefab";
    private const float PoseTolerance = 0.001f;

    public sealed class Target
    {
        public string ProfileId;
        public string AssetPath;
        public string DisplayName;
        public ContentVisualCategory Category;
        public ContentVisualIdentityCategory IdentityCategory;
        public string ContentId;
        public bool ExplicitProfileId;
        public readonly List<string> PrefabPaths = new List<string>();
    }

    // 채집 자원 : Prefab → 외형 설정 (자원은 아이템 ID와 달라 설정 ID를 직접 쓴다)
    private static readonly (string prefabPath, string profileId, string assetName, string displayName)[] Resources =
    {
        ("Assets/_ProjectU/Prefabs/Gathering/TreeResource_01.prefab", "visual_resource_tree", "VP_Resource_Tree", "TREE"),
        ("Assets/_ProjectU/Prefabs/Gathering/StoneResource_01.prefab", "visual_resource_stone", "VP_Resource_Stone", "STONE ROCK")
    };

    // ---------------------------------------------------------------- 메뉴

    [MenuItem(MarketContentBuilder.BuildMenuRoot + "16. Buildable + Resource Visual Profiles", false, 35)]
    private static void BuildAllMenu()
    {
        bool confirmed = EditorUtility.DisplayDialog(
            DialogTitle,
            "건축물 20종과 나무 · 돌 자원의 외형을 외형 설정(Visual Profile) 카드로 정리합니다.\n"
            + "· 처음에는 지금 모양 그대로 카드를 만듭니다 (모양은 바뀌지 않음).\n"
            + "· 카드를 고친 뒤 다시 실행하면 Prefab 모양이 카드대로 바뀝니다.\n"
            + "· 돌벽 설치 미리보기 연결을 고칩니다.\n\n"
            + "Prefab과 데이터만 바뀌고 Scene은 바뀌지 않습니다.",
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

    // ---------------------------------------------------------------- 전체 적용

    public static string BuildAll()
    {
        StringBuilder report = new StringBuilder("[건축물 · 자원 외형 정리]\n");
        int created = 0;
        int synced = 0;
        int linked = 0;

        try
        {
            EditorUtility.DisplayProgressBar(DialogTitle, "돌벽 미리보기", 0.05f);
            FixStoneWallPreview(report);
            List<Target> targets = CollectTargets(report);

            for (int index = 0; index < targets.Count; index++)
            {
                Target target = targets[index];
                EditorUtility.DisplayProgressBar(DialogTitle, target.ProfileId, 0.1f + 0.8f * index / targets.Count);
                ContentVisualProfile profile = EnsureProfile(target, report, ref created);

                if (profile == null)
                {
                    continue;
                }

                foreach (string path in target.PrefabPaths)
                {
                    SyncPrefab(path, target, profile, report, ref synced, ref linked);
                }
            }

            AssetDatabase.SaveAssets();
            EditorUtility.DisplayProgressBar(DialogTitle, "등록 목록", 0.95f);
            GameDataRegistryEditor.CreateOrRefreshDefaultRegistry();
            AssetDatabase.SaveAssets();
            report.AppendLine($"외형 설정 {targets.Count}개 (새로 만듦 {created}) · 카드대로 모델 교체 {synced}곳 · 콘텐츠 ID 연결 {linked}곳");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        report.Append(Validate(out _));
        return report.ToString();
    }

    private static List<BuildRecipeData> LoadRecipes()
    {
        List<BuildRecipeData> recipes = new List<BuildRecipeData>();

        foreach (string guid in AssetDatabase.FindAssets("t:BuildRecipeData", new[] { BuildRecipeRoot }))
        {
            BuildRecipeData recipe = AssetDatabase.LoadAssetAtPath<BuildRecipeData>(AssetDatabase.GUIDToAssetPath(guid));

            if (recipe != null && !string.IsNullOrEmpty(recipe.RecipeId))
            {
                recipes.Add(recipe);
            }
        }

        recipes.Sort((left, right) => string.CompareOrdinal(left.RecipeId, right.RecipeId));
        return recipes;
    }

    public static List<Target> CollectTargets(StringBuilder report)
    {
        List<Target> targets = new List<Target>();

        foreach (BuildRecipeData recipe in LoadRecipes())
        {
            string suffix = recipe.RecipeId.StartsWith("structure_", StringComparison.Ordinal) ? recipe.RecipeId.Substring("structure_".Length) : recipe.RecipeId;
            Target target = new Target
            {
                ProfileId = "visual_buildable_" + suffix, AssetPath = BuildableFolder + "VP_Buildable_" + Pascal(suffix) + ".asset", DisplayName = recipe.DisplayName,
                Category = ContentVisualCategory.Buildable, IdentityCategory = ContentVisualIdentityCategory.Buildable, ContentId = recipe.RecipeId
            };

            foreach (GameObject prefab in new[] { recipe.PlacedPrefab, recipe.PreviewPrefab })
            {
                string path = prefab != null ? AssetDatabase.GetAssetPath(prefab) : null;

                if (!string.IsNullOrEmpty(path) && !target.PrefabPaths.Contains(path))
                {
                    target.PrefabPaths.Add(path);
                }
            }

            targets.Add(target);
        }

        foreach ((string prefabPath, string profileId, string assetName, string displayName) in Resources)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            GatherableResource resource = prefab != null ? prefab.GetComponent<GatherableResource>() : null;
            ItemData item = resource != null ? new SerializedObject(resource).FindProperty("resourceItem").objectReferenceValue as ItemData : null;

            if (item == null)
            {
                report?.AppendLine($"✗ {prefabPath} : 채집 자원 · 아이템을 찾지 못했습니다.");
                continue;
            }

            Target target = new Target
            {
                ProfileId = profileId, AssetPath = ResourceFolder + assetName + ".asset", DisplayName = displayName,
                Category = ContentVisualCategory.Resource, IdentityCategory = ContentVisualIdentityCategory.Resource, ContentId = item.ItemId, ExplicitProfileId = true
            };
            target.PrefabPaths.Add(prefabPath);
            targets.Add(target);
        }

        return targets;
    }

    private static string Pascal(string snake)
    {
        return string.Concat(snake.Split('_').Where(part => part.Length > 0).Select(part => char.ToUpperInvariant(part[0]) + part.Substring(1)));
    }

    // 돌벽 레시피의 미리보기가 나무 벽 미리보기를 가리키던 문제
    private static void FixStoneWallPreview(StringBuilder report)
    {
        BuildRecipeData recipe = LoadRecipes().FirstOrDefault(candidate => candidate.RecipeId == StoneWallRecipeId);
        GameObject preview = AssetDatabase.LoadAssetAtPath<GameObject>(StoneWallPreviewPath);

        if (recipe == null || preview == null)
        {
            report.AppendLine("✗ 돌벽 레시피 또는 돌벽 미리보기 Prefab이 없습니다.");
            return;
        }

        if (recipe.PreviewPrefab != preview)
        {
            SerializedObject serialized = new SerializedObject(recipe);
            serialized.FindProperty("previewPrefab").objectReferenceValue = preview;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(recipe);
            report.AppendLine("돌벽 설치 미리보기 : WoodWallPreview → StoneWallPreview 로 수정");
        }
        else
        {
            report.AppendLine("돌벽 설치 미리보기 : StoneWallPreview (변경 없음)");
        }
    }

    // ---------------------------------------------------------------- 카드

    private static ContentVisualProfile EnsureProfile(Target target, StringBuilder report, ref int created)
    {
        ContentVisualProfile profile = AssetDatabase.LoadAssetAtPath<ContentVisualProfile>(target.AssetPath);

        // 이미 외형 설정을 쓰는 Prefab(나무 바닥 설치본)은 그 카드를 그대로 쓴다
        foreach (string path in target.PrefabPaths)
        {
            ContentVisualRoot visualRoot = AssetDatabase.LoadAssetAtPath<GameObject>(path)?.GetComponent<ContentVisualRoot>();

            if (profile == null && visualRoot != null && visualRoot.AppliedProfile != null)
            {
                profile = visualRoot.AppliedProfile;
            }
        }

        if (profile != null)
        {
            return profile;
        }

        // 처음 : 지금 Prefab에 붙은 모델과 위치를 카드로 옮긴다
        foreach (string path in target.PrefabPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (!TryReadModel(prefab, out GameObject model, out Matrix4x4 pose))
            {
                continue;
            }

            StylizedArtAssetFactory.EnsureFolder(Path.GetDirectoryName(target.AssetPath).Replace('\\', '/'));
            profile = ScriptableObject.CreateInstance<ContentVisualProfile>();
            AssetDatabase.CreateAsset(profile, target.AssetPath);
            SerializedObject serialized = new SerializedObject(profile);
            serialized.FindProperty("profileId").stringValue = target.ProfileId;
            serialized.FindProperty("displayName").stringValue = target.DisplayName;
            serialized.FindProperty("category").intValue = (int)target.Category;
            serialized.FindProperty("visualPrefab").objectReferenceValue = model;
            serialized.FindProperty("createPlaceholderWhenPrefabMissing").boolValue = true;
            serialized.FindProperty("visualLocalPosition").vector3Value = Round(pose.GetColumn(3));
            serialized.FindProperty("visualLocalEulerAngles").vector3Value = Round(Rotation(pose).eulerAngles);
            serialized.FindProperty("visualLocalScale").vector3Value = Round(Scale(pose));
            serialized.FindProperty("inheritRootLayer").boolValue = true;
            serialized.FindProperty("removeVisualColliders").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            created++;
            report.AppendLine($"{Path.GetFileNameWithoutExtension(target.AssetPath)} ({target.ProfileId}) : {model.name} 로 새로 만듦");
            return profile;
        }

        report.AppendLine($"✗ {target.ProfileId} : 모델을 읽을 Prefab이 없습니다.");
        return null;
    }

    // Prefab에 붙은 저폴리 모델 : 원본 모델 Prefab과 Prefab 루트 기준 위치 · 회전 · 크기
    private static bool TryReadModel(GameObject root, out GameObject model, out Matrix4x4 pose)
    {
        model = null;
        pose = Matrix4x4.identity;
        StylizedVisualReplacement record = root != null ? root.GetComponent<StylizedVisualReplacement>() : null;
        GameObject generated = record != null ? record.GeneratedVisual : null;

        if (generated == null)
        {
            return false;
        }

        model = PrefabUtility.GetCorrespondingObjectFromSource(generated) ?? StylizedArtAssetFactory.LoadModelPrefab(record.ModelId);

        if (model == null)
        {
            return false;
        }

        pose = root.transform.worldToLocalMatrix * generated.transform.localToWorldMatrix;
        return true;
    }

    private static Matrix4x4 ProfilePose(ContentVisualProfile profile) => Matrix4x4.TRS(profile.VisualLocalPosition, Quaternion.Euler(profile.VisualLocalEulerAngles), profile.VisualLocalScale);
    private static Quaternion Rotation(Matrix4x4 matrix) => Quaternion.LookRotation(matrix.GetColumn(2), matrix.GetColumn(1));
    private static Vector3 Scale(Matrix4x4 matrix) => new Vector3(matrix.GetColumn(0).magnitude, matrix.GetColumn(1).magnitude, matrix.GetColumn(2).magnitude);

    private static Vector3 Round(Vector3 value)
    {
        return new Vector3(Mathf.Round(value.x * 10000f) / 10000f, Mathf.Round(value.y * 10000f) / 10000f, Mathf.Round(value.z * 10000f) / 10000f);
    }

    private static float PoseDifference(Matrix4x4 left, Matrix4x4 right)
    {
        float difference = 0f;

        for (int index = 0; index < 16; index++)
        {
            difference = Mathf.Max(difference, Mathf.Abs(left[index] - right[index]));
        }

        return difference;
    }

    // ---------------------------------------------------------------- Prefab

    private static void SyncPrefab(string path, Target target, ContentVisualProfile profile, StringBuilder report, ref int synced, ref int linked)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        bool changed = false;

        try
        {
            ContentVisualRoot visualRoot = root.GetComponent<ContentVisualRoot>();

            if (visualRoot == null) // 저폴리 모델 교체 방식 : 카드와 다르면 모델을 바꿔 끼운다
            {
                StylizedVisualReplacement record = root.GetComponent<StylizedVisualReplacement>();
                GameObject generated = record != null ? record.GeneratedVisual : null;

                if (generated == null)
                {
                    report.AppendLine($"✗ {Path.GetFileName(path)} : 저폴리 모델이 없습니다.");
                    return;
                }

                GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(generated);
                Matrix4x4 current = root.transform.worldToLocalMatrix * generated.transform.localToWorldMatrix;
                Matrix4x4 wanted = ProfilePose(profile);

                if (profile.VisualPrefab != null && (source != profile.VisualPrefab || PoseDifference(current, wanted) > PoseTolerance))
                {
                    Transform parent = generated.transform.parent;
                    int sibling = generated.transform.GetSiblingIndex();
                    string name = generated.name;
                    Object.DestroyImmediate(generated);
                    GameObject replacement = (GameObject)PrefabUtility.InstantiatePrefab(profile.VisualPrefab, parent);
                    replacement.name = name;
                    replacement.transform.SetSiblingIndex(sibling);
                    Matrix4x4 local = parent.worldToLocalMatrix * root.transform.localToWorldMatrix * wanted;
                    replacement.transform.localPosition = local.GetColumn(3);
                    replacement.transform.localRotation = Rotation(local);
                    replacement.transform.localScale = Scale(local);
                    SetLayerRecursively(replacement.transform, parent.gameObject.layer);
                    record.Record(record.ModelId, replacement, record.HiddenMeshFilters, record.OriginalMeshes);
                    EditorUtility.SetDirty(record);
                    synced++;
                    changed = true;
                    report.AppendLine($"{Path.GetFileName(path)} : 카드({profile.ProfileId})대로 모델 교체");
                }
            }

            // 콘텐츠 ID → 외형 설정 카드
            ContentVisualIdentity identity = root.GetComponent<ContentVisualIdentity>();

            if (identity == null)
            {
                identity = root.AddComponent<ContentVisualIdentity>();
                changed = true;
            }

            SerializedObject serialized = new SerializedObject(identity);
            serialized.FindProperty("category").intValue = (int)target.IdentityCategory;
            serialized.FindProperty("contentId").stringValue = target.ContentId;
            serialized.FindProperty("useExplicitVisualProfileId").boolValue = target.ExplicitProfileId;
            serialized.FindProperty("explicitVisualProfileId").stringValue = target.ExplicitProfileId ? target.ProfileId : string.Empty;

            if (serialized.ApplyModifiedPropertiesWithoutUndo())
            {
                changed = true;
            }

            if (identity.RefreshResolvedProfileId() && identity.ResolvedVisualProfileId == profile.ProfileId)
            {
                linked++;
            }
            else
            {
                report.AppendLine($"✗ {Path.GetFileName(path)} : 콘텐츠 ID {target.ContentId} 가 {profile.ProfileId} 로 이어지지 않습니다.");
            }

            if (changed)
            {
                EditorUtility.SetDirty(identity);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void SetLayerRecursively(Transform target, int layer)
    {
        target.gameObject.layer = layer;

        foreach (Transform child in target)
        {
            SetLayerRecursively(child, layer);
        }
    }

    // ---------------------------------------------------------------- 검사

    public static string Validate(out int errorCount)
    {
        StringBuilder report = new StringBuilder("[건축물 · 자원 외형 검증]\n");
        int errors = 0;
        int prefabs = 0;

        void Error(string message)
        {
            errors++;
            report.AppendLine("✗ " + message);
        }

        List<Target> targets = CollectTargets(null);
        GameDataRegistry registry = AssetDatabase.LoadAssetAtPath<GameDataRegistry>("Assets/_ProjectU/Data/Registry/GameDataRegistry.asset");
        registry?.RebuildLookup(false);

        foreach (BuildRecipeData recipe in LoadRecipes())
        {
            string placed = recipe.PlacedPrefab != null ? recipe.PlacedPrefab.name : string.Empty;
            string preview = recipe.PreviewPrefab != null ? recipe.PreviewPrefab.name : string.Empty;

            if (placed.EndsWith("Placed", StringComparison.Ordinal) && preview.EndsWith("Preview", StringComparison.Ordinal)
                && placed.Substring(0, placed.Length - "Placed".Length) != preview.Substring(0, preview.Length - "Preview".Length))
            {
                Error($"{recipe.RecipeId} : 설치본({placed})과 미리보기({preview})가 다른 건축물입니다.");
            }
        }

        foreach (Target target in targets)
        {
            ContentVisualProfile profile = registry != null && registry.TryGetVisualProfile(target.ProfileId, out ContentVisualProfile found) ? found : null;

            if (profile == null)
            {
                Error($"{target.ProfileId} 외형 설정이 없거나 GameDataRegistry에 없습니다. 16번 메뉴를 실행하세요.");
                continue;
            }

            if (profile.Category != target.Category) Error($"{target.ProfileId} : 분류가 {target.Category} 가 아닙니다.");
            if (profile.VisualPrefab == null) Error($"{target.ProfileId} : 외형 모델이 없습니다.");

            foreach (string path in target.PrefabPaths)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                ContentVisualIdentity identity = prefab != null ? prefab.GetComponent<ContentVisualIdentity>() : null;
                prefabs++;

                if (identity == null || !identity.TryGetVisualProfileId(out string resolved, out _) || resolved != target.ProfileId)
                {
                    Error($"{Path.GetFileName(path)} : 콘텐츠 ID가 {target.ProfileId} 로 이어지지 않습니다.");
                    continue;
                }

                ContentVisualRoot visualRoot = prefab.GetComponent<ContentVisualRoot>();

                if (visualRoot != null)
                {
                    if (visualRoot.AppliedProfile != profile) Error($"{Path.GetFileName(path)} : 외형 설정 방식 Prefab이 다른 카드를 씁니다.");
                    continue;
                }

                if (!TryReadModel(prefab, out GameObject model, out Matrix4x4 pose))
                {
                    Error($"{Path.GetFileName(path)} : 저폴리 모델이 없습니다.");
                    continue;
                }

                if (model != profile.VisualPrefab || PoseDifference(pose, ProfilePose(profile)) > PoseTolerance * 10f)
                {
                    Error($"{Path.GetFileName(path)} : 모양이 카드({profile.ProfileId})와 다릅니다. 16번 메뉴를 실행하세요.");
                }
            }
        }

        report.AppendLine($"외형 설정 {targets.Count}개 (건축물 {targets.Count(target => target.Category == ContentVisualCategory.Buildable)} · 자원 {targets.Count(target => target.Category == ContentVisualCategory.Resource)}) · Prefab {prefabs}개");
        report.AppendLine(errors == 0 ? "결과 : 오류 0개" : $"결과 : 오류 {errors}개");
        errorCount = errors;
        return report.ToString();
    }
}
