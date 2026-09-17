using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// 78일차: 오브젝트 안의 Unity 기본 도형(Cube·Sphere 등) 외형을 저폴리 모델로 교체한다.
// 기존 MeshRenderer·Collider·스크립트 참조는 그대로 두고 MeshFilter의 메시만 비운 뒤,
// 같은 위치·크기에 맞춘 모델을 자식으로 추가한다.
public static class StylizedVisualReplacer
{
    public const string GeneratedVisualName = "LP_Visual";
    public const string GeneratedFlameName = "LP_Flame";

    public sealed class Options
    {
        public float YawDegrees;
        // 벽처럼 얇은 방향이 X인 경우 모델을 90도 돌린다
        public bool OrientThinAxisToZ;
        // 침낭처럼 긴 방향이 X인 경우 모델을 90도 돌린다
        public bool OrientLongAxisToZ;
        // 손잡이가 +Y인 도구 모델을 가장 긴 방향에 맞춘다
        public bool AlignModelUpToLongestAxis;
        public string BladeHintObjectName;
        // 누운 아이템처럼 추가 회전이 필요한 경우
        public Vector3 ExtraEuler;
        public StylizedModelLibrary.FitMode? FitOverride;
        public float ScaleMultiplier = 1f;
        // 모델을 붙일 부모를 직접 지정 (플레이어 외형 루트 등)
        public Transform ParentOverride;
        public Transform FlameRoot;
        public Light FlameLight;
        public bool UseUndo;
    }

    public static bool IsBuiltinPrimitive(Mesh mesh)
    {
        if (mesh == null)
        {
            return false;
        }

        string path = AssetDatabase.GetAssetPath(mesh);
        return path == "Library/unity default resources" || path.StartsWith("Resources/unity_builtin_extra");
    }

    public static bool Replace(GameObject root, string modelId, Options options, out string message)
    {
        options ??= new Options();

        if (root == null)
        {
            message = "대상 오브젝트가 없습니다.";
            return false;
        }

        if (!StylizedModelLibrary.Catalog.TryGetValue(modelId, out StylizedModelLibrary.ModelInfo modelInfo))
        {
            message = $"모델 ID가 도감에 없습니다: {modelId}";
            return false;
        }

        GameObject modelPrefab = StylizedArtAssetFactory.GetOrCreateModelPrefab(modelId, false);

        if (modelPrefab == null)
        {
            message = $"모델 Prefab 생성 실패: {modelId}";
            return false;
        }

        Restore(root, options.UseUndo);

        List<MeshFilter> targets = CollectPrimitiveFilters(root);

        if (targets.Count == 0)
        {
            message = $"{root.name}: 교체할 기본 도형 외형이 없습니다.";
            return false;
        }

        Transform parent = options.ParentOverride != null ? options.ParentOverride : FindCommonParent(targets);
        Bounds targetBounds = CalculateBounds(parent, targets);
        Mesh modelMesh = modelPrefab.GetComponent<MeshFilter>().sharedMesh;
        Bounds modelBounds = modelMesh.bounds;

        Quaternion rotation = ResolveRotation(parent, targets, targetBounds, modelBounds, options);
        Bounds rotatedModel = RotateBounds(modelBounds, rotation);
        StylizedModelLibrary.FitMode fit = options.FitOverride ?? modelInfo.Fit;
        Vector3 scale = ResolveScale(fit, targetBounds.size, rotatedModel.size) * options.ScaleMultiplier;

        // 회전·크기 적용 후 모델 바닥 중심이 기존 외형 바닥 중심에 오도록 배치
        Vector3 scaledCenter = Vector3.Scale(rotatedModel.center, scale);
        Vector3 scaledMin = Vector3.Scale(rotatedModel.min, scale);
        Vector3 modelBottomCenter = new Vector3(scaledCenter.x, Mathf.Min(scaledMin.y, Vector3.Scale(rotatedModel.max, scale).y), scaledCenter.z);
        Vector3 targetBottomCenter = new Vector3(targetBounds.center.x, targetBounds.min.y, targetBounds.center.z);
        Vector3 localPosition = targetBottomCenter - modelBottomCenter;

        GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, parent);

        if (options.UseUndo)
        {
            Undo.RegisterCreatedObjectUndo(visual, "Apply Stylized Visual");
        }

        visual.name = GeneratedVisualName;
        visual.transform.localPosition = localPosition;
        // 스케일을 회전 전 모델 축 기준으로 되돌려 적용
        visual.transform.localRotation = rotation;
        visual.transform.localScale = InverseRotateScale(scale, rotation);
        SetLayerRecursively(visual.transform, parent.gameObject.layer);

        MeshFilter[] hidden = targets.ToArray();
        Mesh[] originals = new Mesh[hidden.Length];

        for (int index = 0; index < hidden.Length; index++)
        {
            if (options.UseUndo)
            {
                Undo.RecordObject(hidden[index], "Apply Stylized Visual");
            }

            originals[index] = hidden[index].sharedMesh;
            hidden[index].sharedMesh = null;
            EditorUtility.SetDirty(hidden[index]);
        }

        GameObject flame = null;

        if (options.FlameRoot != null)
        {
            flame = AddFlame(options, parent, targetBounds, scale);
        }

        StylizedVisualReplacement record = root.GetComponent<StylizedVisualReplacement>();

        if (record == null)
        {
            record = options.UseUndo
                ? Undo.AddComponent<StylizedVisualReplacement>(root)
                : root.AddComponent<StylizedVisualReplacement>();
        }
        else if (options.UseUndo)
        {
            Undo.RecordObject(record, "Apply Stylized Visual");
        }

        record.Record(modelId, visual, hidden, originals);
        EditorUtility.SetDirty(record);

        if (flame != null)
        {
            flame.transform.SetAsLastSibling();
        }

        message = $"{root.name}: {modelId} 적용 (기본 도형 {hidden.Length}개 교체)";
        return true;
    }

    // 이전 교체 결과를 되돌린다 (다시 적용할 때도 먼저 호출)
    public static bool Restore(GameObject root, bool useUndo)
    {
        StylizedVisualReplacement record = root.GetComponent<StylizedVisualReplacement>();

        if (record == null)
        {
            return false;
        }

        MeshFilter[] filters = record.HiddenMeshFilters;
        Mesh[] meshes = record.OriginalMeshes;

        for (int index = 0; index < filters.Length && index < meshes.Length; index++)
        {
            if (filters[index] == null)
            {
                continue;
            }

            if (useUndo)
            {
                Undo.RecordObject(filters[index], "Restore Stylized Visual");
            }

            filters[index].sharedMesh = meshes[index];
            EditorUtility.SetDirty(filters[index]);
        }

        DestroyGenerated(record.GeneratedVisual, useUndo);

        // 교체 과정에서 추가한 불꽃도 함께 제거
        Transform[] children = root.GetComponentsInChildren<Transform>(true);

        for (int index = children.Length - 1; index >= 0; index--)
        {
            if (children[index] != null && children[index].name == GeneratedFlameName)
            {
                DestroyGenerated(children[index].gameObject, useUndo);
            }
        }

        if (useUndo)
        {
            Undo.DestroyObjectImmediate(record);
        }
        else
        {
            Object.DestroyImmediate(record, true);
        }

        return true;
    }

    private static void DestroyGenerated(GameObject target, bool useUndo)
    {
        if (target == null)
        {
            return;
        }

        if (useUndo)
        {
            Undo.DestroyObjectImmediate(target);
        }
        else
        {
            Object.DestroyImmediate(target, true);
        }
    }

    private static List<MeshFilter> CollectPrimitiveFilters(GameObject root)
    {
        List<MeshFilter> result = new List<MeshFilter>();
        MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);

        for (int index = 0; index < filters.Length; index++)
        {
            MeshFilter filter = filters[index];

            if (!IsBuiltinPrimitive(filter.sharedMesh) || filter.GetComponent<MeshRenderer>() == null)
            {
                continue;
            }

            // 저폴리 모델 안이나 불꽃 연출 아래 오브젝트는 제외
            if (IsUnderNamed(filter.transform, root.transform, GeneratedVisualName)
                || IsUnderNamed(filter.transform, root.transform, GeneratedFlameName))
            {
                continue;
            }

            // 꺼져 있는 Renderer는 원래 보이지 않는 외형이므로 제외
            MeshRenderer renderer = filter.GetComponent<MeshRenderer>();

            if (!renderer.enabled)
            {
                continue;
            }

            result.Add(filter);
        }

        return result;
    }

    private static bool IsUnderNamed(Transform target, Transform stop, string name)
    {
        Transform current = target;

        while (current != null && current != stop)
        {
            if (current.name == name)
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static Transform FindCommonParent(List<MeshFilter> filters)
    {
        Transform common = filters[0].transform;

        // 기본 도형이 하나이고 자식이 없는 단독 외형이면 그 부모를 기준으로 삼는다
        if (filters.Count == 1)
        {
            Transform single = filters[0].transform;
            bool hasOwnLogic = single.GetComponents<Component>().Length > 3 || single.parent == null;
            return hasOwnLogic ? single : single.parent;
        }

        for (int index = 1; index < filters.Count; index++)
        {
            Transform other = filters[index].transform;

            while (common != null && !other.IsChildOf(common))
            {
                common = common.parent;
            }
        }

        return common != null ? common : filters[0].transform.root;
    }

    private static Bounds CalculateBounds(Transform space, List<MeshFilter> filters)
    {
        bool initialized = false;
        Bounds bounds = new Bounds();
        Matrix4x4 worldToSpace = space.worldToLocalMatrix;

        for (int index = 0; index < filters.Count; index++)
        {
            MeshFilter filter = filters[index];
            Bounds meshBounds = filter.sharedMesh.bounds;
            Matrix4x4 matrix = worldToSpace * filter.transform.localToWorldMatrix;
            Vector3 min = meshBounds.min;
            Vector3 max = meshBounds.max;

            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 point = new Vector3(
                    (corner & 1) == 0 ? min.x : max.x,
                    (corner & 2) == 0 ? min.y : max.y,
                    (corner & 4) == 0 ? min.z : max.z);
                Vector3 transformed = matrix.MultiplyPoint3x4(point);

                if (!initialized)
                {
                    bounds = new Bounds(transformed, Vector3.zero);
                    initialized = true;
                }
                else
                {
                    bounds.Encapsulate(transformed);
                }
            }
        }

        return bounds;
    }

    private static Quaternion ResolveRotation(Transform parent, List<MeshFilter> targets, Bounds targetBounds, Bounds modelBounds, Options options)
    {
        Quaternion rotation = Quaternion.Euler(0f, options.YawDegrees, 0f);
        Vector3 size = targetBounds.size;

        if (options.OrientThinAxisToZ && size.x < size.z)
        {
            rotation = Quaternion.Euler(0f, 90f, 0f) * rotation;
        }

        if (options.OrientLongAxisToZ && size.x > size.z)
        {
            rotation = Quaternion.Euler(0f, 90f, 0f) * rotation;
        }

        if (options.AlignModelUpToLongestAxis)
        {
            Vector3 longest = LongestAxis(size);
            Bounds? hintBounds = FindHintBounds(parent, targets, options.BladeHintObjectName);

            // 도구 머리가 긴 축의 음수 쪽에 있으면 모델 위쪽도 음수 방향으로 맞춘다
            if (hintBounds.HasValue && Vector3.Dot(hintBounds.Value.center - targetBounds.center, longest) < 0f)
            {
                longest = -longest;
            }

            rotation = Quaternion.FromToRotation(Vector3.up, longest);

            if (hintBounds.HasValue)
            {
                Vector3 bladeDirection = FindBladeDirection(hintBounds.Value, targetBounds, longest);

                if (bladeDirection.sqrMagnitude > 0.0001f)
                {
                    Vector3 currentBlade = rotation * Vector3.right;
                    float angle = Vector3.SignedAngle(currentBlade, bladeDirection, longest);
                    rotation = Quaternion.AngleAxis(angle, longest) * rotation;
                }
            }
        }

        if (options.ExtraEuler != Vector3.zero)
        {
            rotation *= Quaternion.Euler(options.ExtraEuler);
        }

        return rotation;
    }

    private static Bounds? FindHintBounds(Transform parent, List<MeshFilter> targets, string hintName)
    {
        if (string.IsNullOrEmpty(hintName))
        {
            return null;
        }

        for (int index = 0; index < targets.Count; index++)
        {
            if (targets[index].name == hintName)
            {
                return CalculateBounds(parent, new List<MeshFilter> { targets[index] });
            }
        }

        return null;
    }

    private static Vector3 FindBladeDirection(Bounds hintBounds, Bounds targetBounds, Vector3 longAxis)
    {
        Vector3 offset = hintBounds.center - targetBounds.center;
        Vector3 perpendicular = offset - Vector3.Project(offset, longAxis);

        if (perpendicular.sqrMagnitude > 0.0004f)
        {
            return perpendicular.normalized;
        }

        // 머리 부분이 가장 넓게 퍼진 수평 방향을 날 방향으로 사용
        Vector3 hintSize = hintBounds.size;
        Vector3 axis = new Vector3(Mathf.Abs(longAxis.x), Mathf.Abs(longAxis.y), Mathf.Abs(longAxis.z));
        hintSize = Vector3.Scale(hintSize, Vector3.one - axis);
        return LongestAxis(hintSize);
    }

    private static Vector3 LongestAxis(Vector3 size)
    {
        if (size.x >= size.y && size.x >= size.z)
        {
            return Vector3.right;
        }

        return size.y >= size.z ? Vector3.up : Vector3.forward;
    }

    private static Bounds RotateBounds(Bounds bounds, Quaternion rotation)
    {
        Bounds result = new Bounds(rotation * bounds.center, Vector3.zero);
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        for (int corner = 0; corner < 8; corner++)
        {
            Vector3 point = new Vector3(
                (corner & 1) == 0 ? min.x : max.x,
                (corner & 2) == 0 ? min.y : max.y,
                (corner & 4) == 0 ? min.z : max.z);
            result.Encapsulate(rotation * point);
        }

        return result;
    }

    private static Vector3 ResolveScale(StylizedModelLibrary.FitMode fit, Vector3 target, Vector3 model)
    {
        Vector3 safeModel = new Vector3(Mathf.Max(0.0001f, model.x), Mathf.Max(0.0001f, model.y), Mathf.Max(0.0001f, model.z));

        switch (fit)
        {
            case StylizedModelLibrary.FitMode.Stretch:
            {
                Vector3 scale = new Vector3(target.x / safeModel.x, target.y / safeModel.y, target.z / safeModel.z);
                float fallback = Mathf.Max(scale.x, scale.y, scale.z);

                // 거의 두께가 없는 축은 다른 축 비율로 대체
                if (target.x < 0.001f) scale.x = fallback;
                if (target.y < 0.001f) scale.y = fallback;
                if (target.z < 0.001f) scale.z = fallback;
                return scale;
            }
            case StylizedModelLibrary.FitMode.UniformHeight:
                return Vector3.one * (target.y / safeModel.y);
            case StylizedModelLibrary.FitMode.UniformFootprint:
                return Vector3.one * Mathf.Min(target.x / safeModel.x, target.z / safeModel.z);
            default:
            {
                float targetLargest = Mathf.Max(target.x, target.y, target.z);
                float modelLargest = Mathf.Max(safeModel.x, safeModel.y, safeModel.z);
                return Vector3.one * (targetLargest / modelLargest);
            }
        }
    }

    // 부모 공간 기준 크기를 모델 로컬 축 기준 크기로 변환 (90도 단위 회전 가정)
    private static Vector3 InverseRotateScale(Vector3 scale, Quaternion rotation)
    {
        Vector3 localX = rotation * Vector3.right;
        Vector3 localY = rotation * Vector3.up;
        Vector3 localZ = rotation * Vector3.forward;
        return new Vector3(
            Mathf.Abs(ProjectScale(scale, localX)),
            Mathf.Abs(ProjectScale(scale, localY)),
            Mathf.Abs(ProjectScale(scale, localZ)));
    }

    private static float ProjectScale(Vector3 scale, Vector3 axis)
    {
        Vector3 absolute = new Vector3(Mathf.Abs(axis.x), Mathf.Abs(axis.y), Mathf.Abs(axis.z));
        float sum = absolute.x + absolute.y + absolute.z;

        if (sum < 0.0001f)
        {
            return 1f;
        }

        return (scale.x * absolute.x + scale.y * absolute.y + scale.z * absolute.z) / sum;
    }

    private static GameObject AddFlame(Options options, Transform modelParent, Bounds targetBounds, Vector3 scale)
    {
        GameObject flamePrefab = StylizedArtAssetFactory.GetOrCreateModelPrefab("fx_flame", false);

        if (flamePrefab == null)
        {
            return null;
        }

        GameObject flame = (GameObject)PrefabUtility.InstantiatePrefab(flamePrefab, options.FlameRoot);

        if (options.UseUndo)
        {
            Undo.RegisterCreatedObjectUndo(flame, "Apply Stylized Flame");
        }

        flame.name = GeneratedFlameName;
        Vector3 worldBottom = modelParent.TransformPoint(new Vector3(targetBounds.center.x, targetBounds.min.y + targetBounds.size.y * 0.05f, targetBounds.center.z));
        flame.transform.position = worldBottom;
        flame.transform.rotation = modelParent.rotation;
        float uniform = Mathf.Max(0.01f, (scale.x + scale.z) * 0.5f);
        Vector3 parentLossy = options.FlameRoot.lossyScale;
        Vector3 modelLossy = modelParent.lossyScale;
        flame.transform.localScale = new Vector3(
            uniform * modelLossy.x / Mathf.Max(0.0001f, parentLossy.x),
            uniform * modelLossy.y / Mathf.Max(0.0001f, parentLossy.y),
            uniform * modelLossy.z / Mathf.Max(0.0001f, parentLossy.z));
        SetLayerRecursively(flame.transform, options.FlameRoot.gameObject.layer);

        StylizedFlameFlicker flicker = flame.GetComponent<StylizedFlameFlicker>();

        if (flicker != null && options.FlameLight != null)
        {
            SerializedObject serializedFlicker = new SerializedObject(flicker);
            serializedFlicker.FindProperty("targetLight").objectReferenceValue = options.FlameLight;
            serializedFlicker.ApplyModifiedPropertiesWithoutUndo();
        }

        return flame;
    }

    public static void SetLayerRecursively(Transform target, int layer)
    {
        target.gameObject.layer = layer;

        for (int index = 0; index < target.childCount; index++)
        {
            SetLayerRecursively(target.GetChild(index), layer);
        }
    }
}
