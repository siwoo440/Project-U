using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// 78일차: Terrain에 코드로 만든 풀·흙·바위 텍스처를 칠하고 풀(Detail)을 심는다.
// Terrain 높이는 건드리지 않으므로 기존 NavMesh와 배치가 그대로 유지된다.
public static class StylizedTerrainPainter
{
    private const int TextureSize = 256;

    public sealed class PaintZones
    {
        // 흙길 (선분 목록)
        public readonly List<(Vector3 from, Vector3 to, float width)> Paths = new List<(Vector3, Vector3, float)>();
        // 흙 바닥 영역 (중심, 반지름)
        public readonly List<(Vector3 center, float radius)> DirtCircles = new List<(Vector3, float)>();
        // 풀을 심지 않을 영역
        public readonly List<(Vector3 center, float radius)> NoGrassCircles = new List<(Vector3, float)>();
        public readonly List<Bounds> NoGrassRects = new List<Bounds>();
    }

    public static string Paint(Terrain terrain, PaintZones zones, bool plantGrass)
    {
        TerrainData data = terrain.terrainData;
        Undo.RegisterCompleteObjectUndo(data, "Paint Stylized Terrain");

        TerrainLayer grass = GetOrCreateLayer("Grass", GrassPixel, 5f);
        TerrainLayer dryGrass = GetOrCreateLayer("DryGrass", DryGrassPixel, 6f);
        TerrainLayer dirt = GetOrCreateLayer("Dirt", DirtPixel, 3.5f);
        TerrainLayer rock = GetOrCreateLayer("Rock", RockPixel, 6f);
        data.terrainLayers = new[] { grass, dryGrass, dirt, rock };

        int resolution = data.alphamapResolution;
        float[,,] alphas = new float[resolution, resolution, 4];
        Vector3 origin = terrain.transform.position;
        Vector3 size = data.size;

        for (int y = 0; y < resolution; y++)
        {
            float normalizedZ = y / (float)(resolution - 1);

            for (int x = 0; x < resolution; x++)
            {
                float normalizedX = x / (float)(resolution - 1);
                Vector3 world = new Vector3(origin.x + normalizedX * size.x, 0f, origin.z + normalizedZ * size.z);

                float steepness = data.GetSteepness(normalizedX, normalizedZ);
                float rockWeight = Mathf.InverseLerp(28f, 42f, steepness);
                float dirtWeight = DirtWeight(world, zones);
                float variation = Mathf.PerlinNoise(world.x * 0.035f + 17.3f, world.z * 0.035f + 5.1f);
                float dryWeight = Mathf.Clamp01((variation - 0.52f) * 3.2f);

                float grassWeight = 1f;
                grassWeight *= 1f - rockWeight;
                dryWeight *= 1f - rockWeight;
                grassWeight *= 1f - dirtWeight;
                dryWeight *= 1f - dirtWeight;
                grassWeight *= 1f - dryWeight;

                float total = grassWeight + dryWeight + dirtWeight * (1f - rockWeight) + rockWeight;
                total = Mathf.Max(0.0001f, total);
                alphas[y, x, 0] = grassWeight / total;
                alphas[y, x, 1] = dryWeight / total;
                alphas[y, x, 2] = dirtWeight * (1f - rockWeight) / total;
                alphas[y, x, 3] = rockWeight / total;
            }
        }

        data.SetAlphamaps(0, 0, alphas);

        string grassReport = plantGrass ? PlantGrass(terrain, zones) : "풀 심기 생략";
        EditorUtility.SetDirty(data);
        return $"Terrain 채색 완료 (해상도 {resolution}) / {grassReport}";
    }

    private static float DirtWeight(Vector3 world, PaintZones zones)
    {
        float weight = 0f;
        float edgeNoise = Mathf.PerlinNoise(world.x * 0.25f, world.z * 0.25f) * 0.8f;

        for (int index = 0; index < zones.Paths.Count; index++)
        {
            (Vector3 from, Vector3 to, float width) = zones.Paths[index];
            float distance = DistanceToSegmentXZ(world, from, to);
            float half = width * 0.5f + edgeNoise * 0.6f;
            weight = Mathf.Max(weight, 1f - Mathf.InverseLerp(half * 0.55f, half, distance));
        }

        for (int index = 0; index < zones.DirtCircles.Count; index++)
        {
            (Vector3 center, float radius) = zones.DirtCircles[index];
            float distance = Vector2.Distance(new Vector2(world.x, world.z), new Vector2(center.x, center.z));
            float edge = radius + edgeNoise;
            weight = Mathf.Max(weight, (1f - Mathf.InverseLerp(edge * 0.6f, edge, distance)) * 0.85f);
        }

        return Mathf.Clamp01(weight);
    }

    public static float DistanceToSegmentXZ(Vector3 point, Vector3 from, Vector3 to)
    {
        Vector2 p = new Vector2(point.x, point.z);
        Vector2 a = new Vector2(from.x, from.z);
        Vector2 b = new Vector2(to.x, to.z);
        Vector2 ab = b - a;
        float t = ab.sqrMagnitude < 0.0001f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return Vector2.Distance(p, a + ab * t);
    }

    private static string PlantGrass(Terrain terrain, PaintZones zones)
    {
        TerrainData data = terrain.terrainData;
        Texture2D grassTexture = GetOrCreateTexture("GrassBlades", 128, GrassBladePixel, true);

        DetailPrototype prototype = new DetailPrototype
        {
            prototypeTexture = grassTexture,
            usePrototypeMesh = false,
            renderMode = DetailRenderMode.Grass,
            healthyColor = new Color(0.52f, 0.78f, 0.36f),
            dryColor = new Color(0.74f, 0.74f, 0.4f),
            minWidth = 0.6f,
            maxWidth = 1.1f,
            minHeight = 0.35f,
            maxHeight = 0.75f,
            noiseSpread = 0.35f,
            useDensityScaling = true
        };

        data.detailPrototypes = new[] { prototype };

        // 칸 하나가 약 1m가 되도록 해상도를 맞춰 풀 수가 지나치게 많아지지 않게 한다
        int desiredResolution = Mathf.Clamp(Mathf.ClosestPowerOfTwo(Mathf.RoundToInt(Mathf.Max(data.size.x, data.size.z))), 128, 1024);

        if (data.detailResolution != desiredResolution)
        {
            data.SetDetailResolution(desiredResolution, 32);
        }

        int resolution = data.detailResolution;
        int[,] density = new int[resolution, resolution];
        Vector3 origin = terrain.transform.position;
        Vector3 size = data.size;
        int planted = 0;

        for (int y = 0; y < resolution; y++)
        {
            float normalizedZ = (y + 0.5f) / resolution;

            for (int x = 0; x < resolution; x++)
            {
                float normalizedX = (x + 0.5f) / resolution;
                Vector3 world = new Vector3(origin.x + normalizedX * size.x, 0f, origin.z + normalizedZ * size.z);

                if (data.GetSteepness(normalizedX, normalizedZ) > 26f || IsNoGrass(world, zones))
                {
                    continue;
                }

                float clump = Mathf.PerlinNoise(world.x * 0.12f + 3.7f, world.z * 0.12f + 9.2f);

                if (clump < 0.38f)
                {
                    continue;
                }

                int count = clump > 0.7f ? 3 : (clump > 0.55f ? 2 : 1);
                density[y, x] = count;
                planted += count;
            }
        }

        data.SetDetailLayer(0, 0, 0, density);
        terrain.detailObjectDistance = 70f;
        terrain.detailObjectDensity = 0.85f;
        return $"풀 {planted}포기";
    }

    private static bool IsNoGrass(Vector3 world, PaintZones zones)
    {
        if (DirtWeight(world, zones) > 0.35f)
        {
            return true;
        }

        for (int index = 0; index < zones.NoGrassCircles.Count; index++)
        {
            (Vector3 center, float radius) = zones.NoGrassCircles[index];

            if ((new Vector2(world.x - center.x, world.z - center.z)).sqrMagnitude < radius * radius)
            {
                return true;
            }
        }

        for (int index = 0; index < zones.NoGrassRects.Count; index++)
        {
            Bounds rect = zones.NoGrassRects[index];

            if (world.x >= rect.min.x && world.x <= rect.max.x && world.z >= rect.min.z && world.z <= rect.max.z)
            {
                return true;
            }
        }

        return false;
    }

    // ------------------------------------------------------------------ 텍스처

    public delegate Color PixelFunction(float u, float v);

    public static TerrainLayer GetOrCreateLayer(string name, PixelFunction pixel, float tileSize) // 100일차: 새 구역 바닥도 사용
    {
        StylizedArtAssetFactory.EnsureFolders();
        string layerPath = $"{StylizedArtAssetFactory.TextureFolder}/TL_{name}.terrainlayer";
        TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath);
        Texture2D texture = GetOrCreateTexture(name, TextureSize, pixel, false);

        if (layer == null)
        {
            layer = new TerrainLayer();
            AssetDatabase.CreateAsset(layer, layerPath);
        }

        layer.diffuseTexture = texture;
        layer.tileSize = new Vector2(tileSize, tileSize);
        layer.smoothness = 0f;
        layer.metallic = 0f;
        EditorUtility.SetDirty(layer);
        return layer;
    }

    private static Texture2D GetOrCreateTexture(string name, int size, PixelFunction pixel, bool alpha)
    {
        string assetPath = $"{StylizedArtAssetFactory.TextureFolder}/T_{name}.png";
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                pixels[y * size + x] = pixel(x / (float)size, y / (float)size);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
        File.WriteAllBytes(fullPath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);

        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.wrapMode = alpha ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            importer.alphaSource = alpha ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
            importer.alphaIsTransparency = alpha;
            importer.mipmapEnabled = true;
            importer.isReadable = alpha;
            importer.sRGBTexture = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
    }

    // 반복 타일이 되도록 경계를 감싸는 노이즈
    public static float TileNoise(float u, float v, float frequency, float seed)
    {
        float angleU = u * Mathf.PI * 2f;
        float angleV = v * Mathf.PI * 2f;
        float radius = frequency / (Mathf.PI * 2f);
        float x = Mathf.Cos(angleU) * radius + seed;
        float y = Mathf.Sin(angleU) * radius + seed * 1.7f;
        float z = Mathf.Cos(angleV) * radius + seed * 0.3f;
        float w = Mathf.Sin(angleV) * radius + seed * 2.1f;
        return (Mathf.PerlinNoise(x + z, y + w) + Mathf.PerlinNoise(x - w, z + y)) * 0.5f;
    }

    public static float Hash(float u, float v, int size)
    {
        int x = Mathf.FloorToInt(u * size);
        int y = Mathf.FloorToInt(v * size);
        unchecked
        {
            int h = x * 374761393 + y * 668265263;
            h = (h ^ (h >> 13)) * 1274126177;
            return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
        }
    }

    private static Color GrassPixel(float u, float v)
    {
        float large = TileNoise(u, v, 4f, 1.3f);
        float small = TileNoise(u, v, 18f, 7.7f);
        float speck = Hash(u, v, TextureSize);
        Color baseColor = Color.Lerp(new Color(0.36f, 0.58f, 0.26f), new Color(0.47f, 0.7f, 0.31f), large);
        baseColor = Color.Lerp(baseColor, new Color(0.3f, 0.5f, 0.22f), small * 0.35f);

        if (speck > 0.965f)
        {
            baseColor = Color.Lerp(baseColor, new Color(0.62f, 0.8f, 0.4f), 0.6f);
        }

        return baseColor;
    }

    private static Color DryGrassPixel(float u, float v)
    {
        float large = TileNoise(u, v, 4f, 3.1f);
        float small = TileNoise(u, v, 16f, 2.2f);
        Color baseColor = Color.Lerp(new Color(0.56f, 0.62f, 0.3f), new Color(0.7f, 0.7f, 0.38f), large);
        return Color.Lerp(baseColor, new Color(0.48f, 0.55f, 0.27f), small * 0.4f);
    }

    private static Color DirtPixel(float u, float v)
    {
        float large = TileNoise(u, v, 5f, 5.5f);
        float small = TileNoise(u, v, 22f, 1.9f);
        float speck = Hash(u, v, TextureSize / 2);
        Color baseColor = Color.Lerp(new Color(0.48f, 0.37f, 0.25f), new Color(0.6f, 0.47f, 0.32f), large);
        baseColor = Color.Lerp(baseColor, new Color(0.4f, 0.31f, 0.21f), small * 0.3f);

        if (speck > 0.92f)
        {
            baseColor = Color.Lerp(baseColor, new Color(0.66f, 0.63f, 0.58f), 0.7f);
        }

        return baseColor;
    }

    private static Color RockPixel(float u, float v)
    {
        float large = TileNoise(u, v, 3f, 8.8f);
        float small = TileNoise(u, v, 14f, 4.4f);
        float crack = Mathf.Abs(TileNoise(u, v, 9f, 2.6f) - 0.5f);
        Color baseColor = Color.Lerp(new Color(0.46f, 0.47f, 0.5f), new Color(0.6f, 0.61f, 0.63f), large);
        baseColor = Color.Lerp(baseColor, new Color(0.38f, 0.39f, 0.42f), small * 0.35f);

        if (crack < 0.03f)
        {
            baseColor *= 0.72f;
        }

        baseColor.a = 1f;
        return baseColor;
    }

    private static Color GrassBladePixel(float u, float v)
    {
        // 아래쪽이 넓고 위쪽이 뾰족한 풀잎 여러 장
        float alpha = 0f;
        float[] centers = { 0.18f, 0.34f, 0.5f, 0.66f, 0.82f };
        float[] heights = { 0.7f, 0.95f, 0.85f, 0.98f, 0.72f };
        float[] leans = { -0.08f, 0.05f, -0.02f, 0.07f, 0.1f };

        for (int index = 0; index < centers.Length; index++)
        {
            if (v > heights[index])
            {
                continue;
            }

            float t = v / heights[index];
            float center = centers[index] + leans[index] * t * t;
            float halfWidth = Mathf.Lerp(0.055f, 0.004f, t);

            if (Mathf.Abs(u - center) < halfWidth)
            {
                alpha = 1f;
            }
        }

        Color color = Color.Lerp(new Color(0.33f, 0.52f, 0.22f), new Color(0.7f, 0.86f, 0.46f), v);
        color.a = alpha;
        return color;
    }
}
