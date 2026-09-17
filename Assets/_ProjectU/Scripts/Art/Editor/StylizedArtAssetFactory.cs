using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// 78일차: 저폴리 모델 도감으로 Material·Mesh·Prefab 에셋을 만들고 재사용한다.
public static class StylizedArtAssetFactory
{
    public const string RootFolder = "Assets/_ProjectU/Art/Generated";
    public const string MaterialFolder = RootFolder + "/Materials";
    public const string MeshFolder = RootFolder + "/Meshes";
    public const string PrefabFolder = RootFolder + "/Prefabs";
    public const string TextureFolder = RootFolder + "/Textures";

    private static readonly Dictionary<StylizedColor, Material> materialCache = new Dictionary<StylizedColor, Material>();

    public static void EnsureFolders()
    {
        EnsureFolder(RootFolder);
        EnsureFolder(MaterialFolder);
        EnsureFolder(MeshFolder);
        EnsureFolder(PrefabFolder);
        EnsureFolder(TextureFolder);
    }

    public static void EnsureFolder(string assetFolder)
    {
        if (AssetDatabase.IsValidFolder(assetFolder))
        {
            return;
        }

        string parent = Path.GetDirectoryName(assetFolder)?.Replace('\\', '/');
        string leaf = Path.GetFileName(assetFolder);

        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }

        AssetDatabase.CreateFolder(parent, leaf);
    }

    public static Material GetMaterial(StylizedColor color)
    {
        if (materialCache.TryGetValue(color, out Material cached) && cached != null)
        {
            return cached;
        }

        EnsureFolders();
        string path = $"{MaterialFolder}/M_LP_{color}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        bool isNew = material == null;

        if (isNew)
        {
            Shader shader = FindLitShader();
            material = new Material(shader);
        }

        ConfigureMaterial(material, StylizedPalette.Get(color));

        if (isNew)
        {
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            EditorUtility.SetDirty(material);
        }

        materialCache[color] = material;
        return material;
    }

    public static Shader FindLitShader()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        return shader != null ? shader : Shader.Find("Standard");
    }

    private static void ConfigureMaterial(Material material, StylizedPalette.Entry entry)
    {
        material.enableInstancing = true;
        SetColorIfExists(material, "_BaseColor", entry.Color);
        SetColorIfExists(material, "_Color", entry.Color);
        SetFloatIfExists(material, "_Smoothness", entry.Smoothness);
        SetFloatIfExists(material, "_Glossiness", entry.Smoothness);
        SetFloatIfExists(material, "_Metallic", entry.Metallic);
        SetFloatIfExists(material, "_EnvironmentReflections", entry.Smoothness > 0.5f ? 1f : 0f);
        SetFloatIfExists(material, "_SpecularHighlights", entry.Smoothness > 0.3f ? 1f : 0f);

        if (entry.Emission > 0f)
        {
            material.EnableKeyword("_EMISSION");
            SetColorIfExists(material, "_EmissionColor", entry.Color * entry.Emission);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        else
        {
            material.DisableKeyword("_EMISSION");
            SetColorIfExists(material, "_EmissionColor", Color.black);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        }

        if (entry.Transparent)
        {
            SetFloatIfExists(material, "_Surface", 1f);
            SetFloatIfExists(material, "_Blend", 0f);
            SetFloatIfExists(material, "_SrcBlend", (float)BlendMode.SrcAlpha);
            SetFloatIfExists(material, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            SetFloatIfExists(material, "_SrcBlendAlpha", (float)BlendMode.One);
            SetFloatIfExists(material, "_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            SetFloatIfExists(material, "_ZWrite", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.SetShaderPassEnabled("ShadowCaster", false);
        }
        else
        {
            SetFloatIfExists(material, "_Surface", 0f);
            SetFloatIfExists(material, "_SrcBlend", (float)BlendMode.One);
            SetFloatIfExists(material, "_DstBlend", (float)BlendMode.Zero);
            SetFloatIfExists(material, "_ZWrite", 1f);
            material.SetOverrideTag("RenderType", "Opaque");
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = -1;
            material.SetShaderPassEnabled("ShadowCaster", true);
        }
    }

    private static void SetColorIfExists(Material material, string property, Color value)
    {
        if (material.HasProperty(property))
        {
            material.SetColor(property, value);
        }
    }

    private static void SetFloatIfExists(Material material, string property, float value)
    {
        if (material.HasProperty(property))
        {
            material.SetFloat(property, value);
        }
    }

    public static Material[] GetMaterials(StylizedColor[] colors)
    {
        Material[] materials = new Material[colors.Length];

        for (int index = 0; index < colors.Length; index++)
        {
            materials[index] = GetMaterial(colors[index]);
        }

        return materials;
    }

    public static string GetPrefabPath(string modelId)
    {
        return $"{PrefabFolder}/LP_{modelId}.prefab";
    }

    public static GameObject LoadModelPrefab(string modelId)
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>(GetPrefabPath(modelId));
    }

    // 모델 Prefab을 만들거나 최신 도감 내용으로 갱신한다 (GUID는 유지)
    public static GameObject GetOrCreateModelPrefab(string modelId, bool rebuild)
    {
        GameObject existing = LoadModelPrefab(modelId);

        if (existing != null && !rebuild)
        {
            return existing;
        }

        LowPolyMeshBuilder builder = new LowPolyMeshBuilder();

        if (!StylizedModelLibrary.TryBuild(modelId, builder))
        {
            Debug.LogError($"저폴리 모델 도감에 없는 ID입니다: {modelId}");
            return null;
        }

        EnsureFolders();
        Mesh built = builder.BuildMesh($"LP_{modelId}", out StylizedColor[] colors);
        string meshPath = $"{MeshFolder}/LP_{modelId}.asset";
        Mesh meshAsset = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);

        if (meshAsset == null)
        {
            AssetDatabase.CreateAsset(built, meshPath);
            meshAsset = built;
        }
        else
        {
            EditorUtility.CopySerialized(built, meshAsset);
            meshAsset.name = $"LP_{modelId}";
            EditorUtility.SetDirty(meshAsset);
            Object.DestroyImmediate(built);
        }

        MeshUtility.Optimize(meshAsset);

        GameObject temp = new GameObject($"LP_{modelId}");

        try
        {
            temp.AddComponent<MeshFilter>().sharedMesh = meshAsset;
            MeshRenderer renderer = temp.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = GetMaterials(colors);
            renderer.shadowCastingMode = HasOnlyTransparentOrEmissive(colors)
                ? ShadowCastingMode.Off
                : ShadowCastingMode.On;
            renderer.receiveShadows = true;
            renderer.lightProbeUsage = LightProbeUsage.BlendProbes;

            if (modelId == "fx_flame")
            {
                temp.AddComponent<StylizedFlameFlicker>();
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, GetPrefabPath(modelId));
            return prefab;
        }
        finally
        {
            Object.DestroyImmediate(temp);
        }
    }

    private static bool HasOnlyTransparentOrEmissive(StylizedColor[] colors)
    {
        for (int index = 0; index < colors.Length; index++)
        {
            StylizedPalette.Entry entry = StylizedPalette.Get(colors[index]);

            if (!entry.Transparent && entry.Emission <= 0f)
            {
                return false;
            }
        }

        return true;
    }

    public static int GenerateAllModels(bool rebuild)
    {
        int count = 0;
        materialCache.Clear();

        try
        {
            int total = StylizedModelLibrary.Catalog.Count;
            int index = 0;

            foreach (string modelId in StylizedModelLibrary.Catalog.Keys)
            {
                index++;
                EditorUtility.DisplayProgressBar("Project U 저폴리 모델 생성", modelId, index / (float)total);

                if (GetOrCreateModelPrefab(modelId, rebuild) != null)
                {
                    count++;
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
        }

        return count;
    }
}
