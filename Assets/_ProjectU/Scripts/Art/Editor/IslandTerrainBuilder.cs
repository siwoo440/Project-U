using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// 105일차: 무인도 (2km 섬 · 사방 바다 · 해변 · 난파선 시작)
// 1. Terrain을 2048m로 키우고 섬 모양(울퉁불퉁한 해안선 · 언덕 · 북쪽 산 · 해변 · 얕은 바다 · 깊은 바다)을 코드로 만든다
//    마을 · 기존 구역 자리(가운데 ±130m)는 지금처럼 높이 0으로 평평하게 둔다 (저장된 건물 · 밭 · NPC 위치 유지)
//    해안 구역 부두 자리는 바닷물이 들어오는 석호로 파 둔다 (107일차에 해안 구역을 진짜 동쪽 해변으로 옮김)
// 2. 바닥 칠하기 : 기본(풀 · 흙) → 섬(해변 모래 · 바다 밑 · 눈 · 절벽 · 해변 오솔길) → 19번 메뉴(구역 바닥 · NPC 배치 · NavMesh · 초상)
// 3. 사방 바다 · 먼 바다 밑 · 남쪽 해변 난파선 · 떠밀려 온 물건 · 시작 위치 · 기본 부활 위치 · 바다 경계 · 안개
// 106일차: 바다 경계는 먼 바다 파도, 물속 수면 · 바닷속(해초 · 산호 · 잠수 물건)은 23번 메뉴와 같은 코드로 함께 만든다
// 107일차: 높이에 구역 터 · 흙길(IslandZoneLayout)을 더하고, NavMesh는 섬 뭍 전체(수면 위)를 굽는다
// 여러 번 실행해도 같은 결과가 나온다 (섬 묶음을 지우고 다시 만듦).
public static class IslandTerrainBuilder
{
    public const string RootName = "=== Island ===";
    private const string ScenePath = "Assets/_ProjectU/Scenes/20_Gameplay.unity";
    private const string DialogTitle = "Project U 무인도";
    private const string PickupRegistryPath = "Assets/_ProjectU/Prefabs/Items/Day75/WorldItemPickupRegistry_Day75.asset";

    public const float TerrainSize = 2048f; // 섬 Terrain 한 변 (m)
    public const float TerrainBase = -40f; // Terrain 바닥 높이 (가장 깊은 바다)
    public const float HeightRange = 260f; // Terrain 높이 범위
    public const int HeightResolution = 2049; // 약 1m마다 한 점
    public const int AlphaResolution = 2048; // 바닥 칠 약 1m
    public const float SeaLevel = -0.5f; // 바다 수면 (마을 땅은 0)
    public const float PlateauHalf = 130f; // 마을 · 기존 구역 평지 (가운데 ±130m 높이 0)
    private const float PlateauBlend = 240f; // 평지에서 언덕으로 이어지는 거리
    public const float CoastRadius = 800f; // 평균 해안선 반지름
    private const float BeachWidth = 45f; // 모래 해변 폭
    public static readonly Vector3 LegacyStart = Vector3.zero; // 104일차까지 시작 위치 (마을 가운데)
    public static readonly Vector3 LegacyRespawn = new Vector3(5.85f, 0f, 1.43f); // 104일차까지 기본 부활 위치

    // 해안 구역 항구 석호 (부두 · 배가 떠 있는 바닷물 웅덩이)
    public static readonly Vector2 LagoonMin = new Vector2(100f, -46f);
    public static readonly Vector2 LagoonMax = new Vector2(168f, 42f);
    private const float LagoonCorner = 14f;
    private const float LagoonBank = 2.5f;

    private const float StartAngle = -90f; // 남쪽 해변
    private const int PickupSeed = 105;

    public static bool IsIslandTerrain(Terrain terrain) // 섬 Terrain인지 (104일차까지는 250m)
    {
        return terrain != null && terrain.terrainData != null && terrain.terrainData.size.x >= 1000f;
    }

    // ---------------------------------------------------------------- 메뉴

    [MenuItem(MarketContentBuilder.BuildMenuRoot + "22. Island Terrain (Sea + Beaches + Shipwreck Start)", false, 41)]
    private static void BuildAllMenu()
    {
        bool confirmed = EditorUtility.DisplayDialog(
            DialogTitle,
            "Terrain을 2km 무인도로 바꿉니다.\n"
            + "섬 모양 · 해변 · 사방 바다 · 남쪽 해변 난파선 · 새 시작 위치를 만들고,\n"
            + "19번(구역 · NPC 배치 · NavMesh · 초상)까지 이어서 실행한 뒤 게임 Scene을 저장합니다.\n\n"
            + "마을과 기존 구역 자리는 지금처럼 평평하게 유지됩니다. 2 ~ 3분 정도 걸릴 수 있습니다.",
            "실행",
            "취소");

        if (!confirmed)
        {
            return;
        }

        if (EditorSceneManager.GetActiveScene().path != ScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        string report = BuildAll(true);
        Debug.Log(report);
        EditorUtility.DisplayDialog(DialogTitle, report.Length <= 1800 ? report : report.Substring(0, 1800) + "\n... (전체 내용은 Console 참고)", "확인");
    }

    public static string BuildAll(bool saveScene)
    {
        StringBuilder report = new StringBuilder("[무인도 만들기]\n");

        if (EditorSceneManager.GetActiveScene().path != ScenePath)
        {
            report.AppendLine($"✗ 게임 Scene({ScenePath})을 열고 다시 실행하세요.");
            return report.ToString();
        }

        Terrain terrain = Object.FindFirstObjectByType<Terrain>(FindObjectsInactive.Include);

        if (terrain == null || terrain.terrainData == null)
        {
            report.AppendLine("✗ 게임 Scene에 Terrain이 없습니다.");
            return report.ToString();
        }

        DateTime started = DateTime.Now;

        try
        {
            GameObject old = GameObject.Find(RootName);

            if (old != null)
            {
                Object.DestroyImmediate(old); // 섬 묶음은 매번 새로 만듦
            }

            EditorUtility.DisplayProgressBar(DialogTitle, "섬 지형", 0.1f);
            report.AppendLine(ShapeTerrain(terrain));
            report.AppendLine(RemoveBackdropMountains());
            report.AppendLine(ConfigureNavMeshVolume());

            EditorUtility.DisplayProgressBar(DialogTitle, "기본 바닥 칠하기", 0.3f);
            report.AppendLine(StylizedSceneDresser.RepaintTerrain());

            EditorUtility.DisplayProgressBar(DialogTitle, "해변 · 바다 밑 · 눈 칠하기", 0.45f);
            report.AppendLine(PaintIsland(terrain));
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        report.AppendLine(Result("19. 섬 NPC (구역 · 배치 · NavMesh · 초상)", NpcCastBuilder.BuildAll(false)));

        try
        {
            EditorUtility.DisplayProgressBar(DialogTitle, "바다 · 난파선 · 시작 위치", 0.9f);
            Transform root = new GameObject(RootName).transform;
            report.AppendLine(BuildSea(root));
            report.AppendLine(BuildShipwreckStart(root, terrain));
            report.AppendLine(SwimmingBuilder.ConfigureWaves(root));
            report.AppendLine(SwimmingBuilder.EnsureOceanUnderside(root));
            report.AppendLine(SwimmingBuilder.BuildUnderwater(root));
            report.AppendLine(ApplyAtmosphere());
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        if (saveScene)
        {
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            report.AppendLine("게임 Scene 저장 완료");
        }

        report.AppendLine($"걸린 시간 {(DateTime.Now - started).TotalSeconds:0}초");
        report.Append(Validate(out _));
        return report.ToString();
    }

    private static string Result(string label, string report)
    {
        List<string> errors = report.Split('\n').Where(line => line.StartsWith("✗")).Select(line => line.TrimEnd('\r')).ToList();
        int failed = report.Split('\n').Count(line => line.StartsWith("결과") && !line.Contains("오류 0개"));
        return errors.Count == 0 && failed == 0 ? $"{label} : 완료" : $"{label} : ✗ 문제 {Math.Max(errors.Count, failed)}개\n{string.Join("\n", errors)}";
    }

    // ---------------------------------------------------------------- 섬 모양

    public static float CoastRadiusAt(float angleRadians) // 방향별 해안선 반지름 (울퉁불퉁한 해안)
    {
        float cx = Mathf.Cos(angleRadians);
        float sz = Mathf.Sin(angleRadians);
        float broad = Mathf.PerlinNoise(cx * 1.3f + 5.3f, sz * 1.3f + 7.1f) * 2f - 1f;
        float fine = Mathf.PerlinNoise(cx * 3.4f + 11.2f, sz * 3.4f + 2.9f) * 2f - 1f;
        return CoastRadius + broad * 60f + fine * 20f;
    }

    public static float LagoonDistance(float x, float z) // 석호 가장자리까지 거리 (안쪽은 음수)
    {
        Vector2 center = (LagoonMin + LagoonMax) * 0.5f;
        Vector2 half = (LagoonMax - LagoonMin) * 0.5f - new Vector2(LagoonCorner, LagoonCorner);
        Vector2 q = new Vector2(Mathf.Abs(x - center.x), Mathf.Abs(z - center.y)) - half;
        return new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - LagoonCorner;
    }

    public static bool IsLagoon(float x, float z) // 석호 물 위인지 (풀 없애기 · 물가 확인)
    {
        return LagoonDistance(x, z) < 0.5f;
    }

    private static float SmoothStep(float edge0, float edge1, float value)
    {
        float t = Mathf.Clamp01((value - edge0) / (edge1 - edge0));
        return t * t * (3f - 2f * t);
    }

    private static float Bump(float x, float z, Vector2 center, float radius) // 둥근 언덕 (가운데 1 → 반지름 0)
    {
        float t = 1f - SmoothStep(0f, radius, Vector2.Distance(new Vector2(x, z), center));
        return t * t;
    }

    private static float Fbm(float x, float z) // 0 ~ 1 언덕 무늬
    {
        return Mathf.PerlinNoise(x + 31.7f, z + 12.9f) * 0.6f + Mathf.PerlinNoise(x * 2.1f + 7.4f, z * 2.1f + 48.2f) * 0.28f + Mathf.PerlinNoise(x * 4.3f + 91.3f, z * 4.3f + 3.8f) * 0.12f;
    }

    public static float HeightAt(float x, float z) // 월드 높이 (y) : 자연 지형 + 107일차 구역 터 · 흙길
    {
        return IslandZoneLayout.Apply(x, z);
    }

    public static float NaturalHeightAt(float x, float z) // 자연 지형 높이 (구역 터 · 흙길 전)
    {
        float distance = Mathf.Sqrt(x * x + z * z);
        float coast = CoastRadiusAt(Mathf.Atan2(z, x)) - distance; // 해안선 안쪽 거리 (바다는 음수)

        // 섬 안쪽 : 언덕 · 북쪽 산 · 서쪽 능선 · 동쪽 언덕
        float hills = Fbm(x / 260f, z / 260f) * 22f;
        float northShape = Bump(x, z, new Vector2(-60f, 600f), 330f);
        float ridges = 1f - Mathf.Abs(Mathf.PerlinNoise(x / 95f + 3.1f, z / 95f + 17.7f) * 2f - 1f); // 능선
        float north = northShape * (130f + 40f * Mathf.PerlinNoise(x / 170f + 8.2f, z / 170f + 1.3f)) + Mathf.Pow(northShape, 0.7f) * ridges * 28f; // 울퉁불퉁한 북쪽 산
        float west = 45f * Bump(x, z, new Vector2(-560f, 80f), 250f);
        float east = 30f * Bump(x, z, new Vector2(520f, 300f), 220f);
        float inland = 2f + hills + north + west + east;
        float plateau = Mathf.Max(Mathf.Abs(x), Mathf.Abs(z)); // 마을 평지는 네모 (기존 구역 모서리까지)
        float height = Mathf.Lerp(0f, inland, SmoothStep(PlateauHalf, PlateauBlend, plateau));

        // 해안 : 모래 해변 → 얕은 바다(약 50m) → 깊은 바다
        const float beachTop = SeaLevel + 1.6f;

        if (coast < 0f)
        {
            float offshore = -coast;
            height = SeaLevel - 0.05f - Mathf.Min(36f, offshore * 0.07f + Mathf.Max(0f, offshore - 50f) * 0.3f);
        }
        else if (coast < 150f)
        {
            float beach = SeaLevel + 1.6f * SmoothStep(0f, BeachWidth, coast);
            height = Mathf.Lerp(beach, Mathf.Max(height, beachTop), SmoothStep(BeachWidth, 150f, coast));
        }

        // Terrain 가장자리는 깊은 바다 밑 (먼 바다 판과 이어짐)
        float edge = TerrainSize * 0.5f - Mathf.Max(Mathf.Abs(x), Mathf.Abs(z));
        height = Mathf.Lerp(TerrainBase + 3f, height, SmoothStep(0f, 80f, edge));

        // 석호 (해안 구역 부두)
        float lagoon = LagoonDistance(x, z);

        if (lagoon < LagoonBank)
        {
            float water = SeaLevel - 0.3f - Mathf.Min(2.1f, Mathf.Max(0f, -lagoon) * 0.22f);
            height = Mathf.Lerp(water, height, SmoothStep(0f, LagoonBank, lagoon));
        }

        return Mathf.Max(height, TerrainBase + 1f);
    }

    private static string ShapeTerrain(Terrain terrain)
    {
        TerrainData data = terrain.terrainData;
        data.heightmapResolution = HeightResolution;
        data.size = new Vector3(TerrainSize, HeightRange, TerrainSize);
        data.alphamapResolution = AlphaResolution;
        data.baseMapResolution = 1024;
        terrain.transform.position = new Vector3(-TerrainSize * 0.5f, TerrainBase, -TerrainSize * 0.5f);

        int resolution = data.heightmapResolution;
        float[,] heights = new float[resolution, resolution];
        float highest = float.MinValue;

        for (int y = 0; y < resolution; y++)
        {
            float z = -TerrainSize * 0.5f + y / (float)(resolution - 1) * TerrainSize;

            for (int x = 0; x < resolution; x++)
            {
                float worldX = -TerrainSize * 0.5f + x / (float)(resolution - 1) * TerrainSize;
                float height = HeightAt(worldX, z);
                highest = Mathf.Max(highest, height);
                heights[y, x] = Mathf.Clamp01((height - TerrainBase) / HeightRange);
            }
        }

        data.SetHeights(0, 0, heights);
        TerrainCollider collider = terrain.GetComponent<TerrainCollider>();

        if (collider != null)
        {
            collider.terrainData = data;
        }

        terrain.heightmapPixelError = 5f;
        terrain.basemapDistance = 400f;
        terrain.treeDistance = 800f;
        terrain.drawInstanced = true;
        EditorUtility.SetDirty(data);
        EditorUtility.SetDirty(terrain);
        return $"섬 지형 : {TerrainSize:0}m × {TerrainSize:0}m (높이 해상도 {resolution}), 해안선 반지름 약 {CoastRadius:0}m, 가장 높은 곳 {highest:0}m, 마을 평지 ±{PlateauHalf:0}m (높이 0), 수면 {SeaLevel}m";
    }

    private static string RemoveBackdropMountains() // 104일차까지 섬 밖을 둘러싸던 원경 산 (이제 섬 안이라 길을 막음)
    {
        GameObject environment = GameObject.Find(StylizedSceneDresser.EnvironmentRootName);
        Transform backdrop = environment != null ? environment.transform.Find("Backdrop") : null;

        if (backdrop == null || backdrop.childCount == 0)
        {
            return "원경 산 : 없음";
        }

        int count = backdrop.childCount;

        for (int index = backdrop.childCount - 1; index >= 0; index--)
        {
            Object.DestroyImmediate(backdrop.GetChild(index).gameObject);
        }

        return $"원경 산 {count}개 정리 (섬 지형의 산으로 대신함)";
    }

    public const float NavMeshSize = 1800f; // 107일차 : 섬 뭍 전체 (해안선 약 760 ~ 850m)
    public const float NavMeshTop = 200f; // 북쪽 산꼭대기(약 184m) 위까지
    public static float NavMeshBottom => SeaLevel + 0.1f; // 수면 아래(바다 밑 · 석호 · 항구)는 굽지 않음

    private static string ConfigureNavMeshVolume() // 105일차 : 마을 · 기존 구역 → 107일차 : 섬 뭍 전체 (NPC가 멀리 떨어진 구역까지 걸어감)
    {
        NavMeshSurface surface = Object.FindFirstObjectByType<NavMeshSurface>(FindObjectsInactive.Include);

        if (surface == null)
        {
            return "✗ NavMeshSurface가 없습니다.";
        }

        surface.collectObjects = CollectObjects.Volume;
        surface.center = new Vector3(0f, (NavMeshBottom + NavMeshTop) * 0.5f, 0f) - surface.transform.position;
        surface.size = new Vector3(NavMeshSize, NavMeshTop - NavMeshBottom, NavMeshSize);
        EditorUtility.SetDirty(surface);
        return $"NavMesh 범위 : 섬 뭍 전체 {surface.size.x:0}m 네모 (수면 {NavMeshBottom:0.0}m 위만)";
    }

    // ---------------------------------------------------------------- 바닥 칠하기

    private static Color SandPixel(float u, float v)
    {
        float large = StylizedTerrainPainter.TileNoise(u, v, 4f, 1.7f);
        float ripple = Mathf.Sin((u + StylizedTerrainPainter.TileNoise(u, v, 3f, 8.4f) * 0.25f) * Mathf.PI * 2f * 12f) * 0.5f + 0.5f;
        Color color = Color.Lerp(new Color(0.9f, 0.83f, 0.64f), new Color(0.95f, 0.89f, 0.72f), large);
        color = Color.Lerp(color, new Color(0.82f, 0.74f, 0.56f), ripple * 0.15f);
        return StylizedTerrainPainter.Hash(u, v, 256) > 0.975f ? Color.Lerp(color, Color.white, 0.5f) : color; // 조개 조각
    }

    private static Color SeabedPixel(float u, float v)
    {
        float large = StylizedTerrainPainter.TileNoise(u, v, 4f, 6.2f);
        Color color = Color.Lerp(new Color(0.55f, 0.52f, 0.4f), new Color(0.66f, 0.62f, 0.47f), large);
        return StylizedTerrainPainter.Hash(u, v, 128) > 0.96f ? Color.Lerp(color, new Color(0.3f, 0.45f, 0.3f), 0.6f) : color; // 해초
    }

    private static Color SnowPixel(float u, float v)
    {
        float large = StylizedTerrainPainter.TileNoise(u, v, 4f, 13.1f);
        return Color.Lerp(new Color(0.88f, 0.92f, 0.97f), new Color(0.98f, 0.99f, 1f), large);
    }

    public static List<Vector3> TrailPoints() // 시작 해변 → 마을 오솔길
    {
        (Vector3 spawn, _, _) = StartPoints();
        return new List<Vector3>
        {
            spawn + new Vector3(0f, 0f, 6f),
            new Vector3(spawn.x - 30f, 0f, Mathf.Lerp(spawn.z, -PlateauHalf, 0.3f)),
            new Vector3(spawn.x + 25f, 0f, Mathf.Lerp(spawn.z, -PlateauHalf, 0.62f)),
            new Vector3(0f, 0f, -PlateauHalf + 10f),
            new Vector3(0f, 0f, -PlateauHalf + 40f)
        };
    }

    private static float TrailDistance(Vector3 world, List<Vector3> trail)
    {
        float best = float.MaxValue;

        for (int index = 0; index + 1 < trail.Count; index++)
        {
            best = Mathf.Min(best, StylizedTerrainPainter.DistanceToSegmentXZ(world, trail[index], trail[index + 1]));
        }

        return best;
    }

    private static string PaintIsland(Terrain terrain)
    {
        TerrainData data = terrain.terrainData;
        TerrainLayer sand = StylizedTerrainPainter.GetOrCreateLayer("IslandSand", SandPixel, 5f);
        TerrainLayer seabed = StylizedTerrainPainter.GetOrCreateLayer("IslandSeabed", SeabedPixel, 7f);
        TerrainLayer snow = StylizedTerrainPainter.GetOrCreateLayer("IslandSnow", SnowPixel, 8f);
        List<TerrainLayer> layers = data.terrainLayers.Where(layer => layer != null && !layer.name.StartsWith("TL_Island", StringComparison.Ordinal)).ToList();
        int rockIndex = layers.FindIndex(layer => layer.name == "TL_Rock");
        int dirtIndex = layers.FindIndex(layer => layer.name == "TL_Dirt");
        int resolution = data.alphamapResolution;
        float[,,] previous = data.GetAlphamaps(0, 0, resolution, resolution);
        TerrainLayer[] oldLayers = data.terrainLayers;
        int[] oldIndexOf = layers.Select(layer => Array.IndexOf(oldLayers, layer)).ToArray();
        layers.Add(sand);
        layers.Add(seabed);
        layers.Add(snow);
        data.terrainLayers = layers.ToArray();
        int count = layers.Count;
        int sandIndex = count - 3;
        int seabedIndex = count - 2;
        int snowIndex = count - 1;
        float[,,] alphas = new float[resolution, resolution, count];
        float[] weights = new float[count];
        List<Vector3> trail = TrailPoints();
        Vector3 origin = terrain.transform.position;
        int beachTexels = 0;

        void Paint(int index, float amount)
        {
            if (index < 0 || amount <= 0f)
            {
                return;
            }

            for (int layer = 0; layer < count; layer++)
            {
                weights[layer] *= 1f - amount;
            }

            weights[index] += amount;
        }

        for (int y = 0; y < resolution; y++)
        {
            float normalizedZ = y / (float)(resolution - 1);
            float z = origin.z + normalizedZ * TerrainSize;

            for (int x = 0; x < resolution; x++)
            {
                float normalizedX = x / (float)(resolution - 1);
                float worldX = origin.x + normalizedX * TerrainSize;
                Array.Clear(weights, 0, count);

                for (int index = 0; index < oldIndexOf.Length; index++)
                {
                    weights[index] = oldIndexOf[index] >= 0 ? previous[y, x, oldIndexOf[index]] : 0f;
                }

                bool inPlateau = Mathf.Max(Mathf.Abs(worldX), Mathf.Abs(z)) < PlateauHalf - 2f && LagoonDistance(worldX, z) > LagoonBank + 1f;

                if (!inPlateau)
                {
                    float height = HeightAt(worldX, z);
                    float coast = CoastRadiusAt(Mathf.Atan2(z, worldX)) - Mathf.Sqrt(worldX * worldX + z * z);
                    float steepness = data.GetSteepness(normalizedX, normalizedZ);
                    float seabedAmount = 1f - SmoothStep(SeaLevel - 1.1f, SeaLevel - 0.2f, height);
                    float sandAmount = (1f - SmoothStep(SeaLevel + 1.5f, SeaLevel + 2.4f, height)) * (coast < 80f || LagoonDistance(worldX, z) < 8f ? 1f : 0f);
                    float snowAmount = SmoothStep(72f, 92f, height);
                    float rockAmount = SmoothStep(30f, 42f, steepness);
                    float trailAmount = 1f - SmoothStep(1.1f, 1.8f, TrailDistance(new Vector3(worldX, 0f, z), trail));

                    Paint(snowIndex, snowAmount);
                    Paint(sandIndex, sandAmount);
                    Paint(dirtIndex, trailAmount * (1f - sandAmount) * 0.85f);
                    Paint(rockIndex, rockAmount * (1f - seabedAmount));
                    Paint(seabedIndex, seabedAmount);
                    beachTexels += sandAmount > 0.5f ? 1 : 0;
                }

                float total = 0f;

                for (int layer = 0; layer < count; layer++)
                {
                    total += weights[layer];
                }

                if (total < 0.0001f)
                {
                    weights[0] = 1f;
                    total = 1f;
                }

                for (int layer = 0; layer < count; layer++)
                {
                    alphas[y, x, layer] = weights[layer] / total;
                }
            }
        }

        data.SetAlphamaps(0, 0, alphas);

        // 모래 · 물 · 눈 · 절벽 · 오솔길의 풀 없애기
        int removed = 0;

        if (data.detailPrototypes.Length > 0)
        {
            int detailResolution = data.detailResolution;
            int[,] density = data.GetDetailLayer(0, 0, detailResolution, detailResolution, 0);

            for (int y = 0; y < detailResolution; y++)
            {
                float normalizedZ = (y + 0.5f) / detailResolution;
                float z = origin.z + normalizedZ * TerrainSize;

                for (int x = 0; x < detailResolution; x++)
                {
                    if (density[y, x] == 0)
                    {
                        continue;
                    }

                    float normalizedX = (x + 0.5f) / detailResolution;
                    float worldX = origin.x + normalizedX * TerrainSize;

                    if (Mathf.Max(Mathf.Abs(worldX), Mathf.Abs(z)) < PlateauHalf - 2f && LagoonDistance(worldX, z) > LagoonBank + 1f)
                    {
                        continue;
                    }

                    float height = HeightAt(worldX, z);

                    if (height < SeaLevel + 2.4f || height > 70f || data.GetSteepness(normalizedX, normalizedZ) > 26f || TrailDistance(new Vector3(worldX, 0f, z), trail) < 2f)
                    {
                        removed += density[y, x];
                        density[y, x] = 0;
                    }
                }
            }

            data.SetDetailLayer(0, 0, 0, density);
        }

        EditorUtility.SetDirty(data);
        return $"섬 바닥 : 층 {count}개 (해변 모래 · 바다 밑 · 눈 추가), 해변 칸 {beachTexels}, 풀 {removed}포기 정리, 오솔길 {trail.Count - 1}구간";
    }

    // ---------------------------------------------------------------- 바다

    private static GameObject Place(Transform parent, string modelId, Vector3 position, float yaw, Vector3 scale, int layer)
    {
        GameObject prefab = StylizedArtAssetFactory.GetOrCreateModelPrefab(modelId, false);

        if (prefab == null)
        {
            return null;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        instance.transform.localScale = scale;

        if (layer >= 0)
        {
            foreach (Transform child in instance.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = layer;
            }
        }

        return instance;
    }

    private static string BuildSea(Transform root)
    {
        Transform sea = new GameObject("Sea").transform;
        sea.SetParent(root, false);
        int water = LayerMask.NameToLayer("Water");
        GameObject ocean = Place(sea, "zone_sea", new Vector3(0f, SeaLevel, 0f), 0f, new Vector3(6000f, 1f, 6000f), water);
        GameObject floor = Place(sea, "zone_seabed", new Vector3(0f, TerrainBase + 2.8f, 0f), 0f, new Vector3(6000f, 1f, 6000f), water);

        foreach (GameObject flat in new[] { ocean, floor })
        {
            if (flat == null)
            {
                continue;
            }

            foreach (MeshRenderer renderer in flat.GetComponentsInChildren<MeshRenderer>(true))
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            GameObjectUtility.SetStaticEditorFlags(flat, 0);
        }

        if (ocean != null) ocean.name = "Ocean";
        if (floor != null) floor.name = "FarSeabed";
        return $"바다 : 수면 {SeaLevel}m · 6km 바다 판 · 먼 바다 밑 {TerrainBase + 2.8f:0.0}m";
    }

    // ---------------------------------------------------------------- 난파선 시작 해변

    public static float ShoreRadiusAt(float angleDegrees) // 가운데에서 이 방향으로 처음 바다가 되는 거리
    {
        Vector2 direction = new Vector2(Mathf.Cos(angleDegrees * Mathf.Deg2Rad), Mathf.Sin(angleDegrees * Mathf.Deg2Rad));

        for (float radius = PlateauHalf + 50f; radius < TerrainSize * 0.5f; radius += 0.5f)
        {
            Vector2 point = direction * radius;

            if (HeightAt(point.x, point.y) < SeaLevel)
            {
                return radius;
            }
        }

        return TerrainSize * 0.5f;
    }

    public static (Vector3 spawn, Vector3 wreck, Vector3 respawn) StartPoints() // 시작 위치 · 난파선 · 기본 부활 위치
    {
        float shore = ShoreRadiusAt(StartAngle);
        Vector3 spawn = new Vector3(0f, 0f, -(shore - 30f));
        Vector3 wreck = new Vector3(-9f, 0f, -(shore + 2f));
        Vector3 respawn = new Vector3(3f, 0f, -(shore - 34f));
        spawn.y = HeightAt(spawn.x, spawn.z);
        wreck.y = HeightAt(wreck.x, wreck.z);
        respawn.y = HeightAt(respawn.x, respawn.z);
        return (spawn, wreck, respawn);
    }

    private static Vector3 OnGround(float x, float z, float lift = 0f)
    {
        return new Vector3(x, HeightAt(x, z) + lift, z);
    }

    private static void AddBoxCollider(GameObject target, Vector3 center, Vector3 size)
    {
        BoxCollider box = target.AddComponent<BoxCollider>();
        box.center = center;
        box.size = size;
    }

    private static void AddBoundsCollider(GameObject target)
    {
        MeshFilter filter = target.GetComponentInChildren<MeshFilter>();

        if (filter != null && filter.sharedMesh != null)
        {
            Bounds bounds = filter.sharedMesh.bounds;
            AddBoxCollider(target, bounds.center, bounds.size);
        }
    }

    private static string BuildShipwreckStart(Transform root, Terrain terrain)
    {
        Transform group = new GameObject("ShipwreckBeach").transform;
        group.SetParent(root, false);
        int building = LayerMask.NameToLayer("Building");
        (Vector3 spawn, Vector3 wreckPoint, Vector3 respawnPoint) = StartPoints();
        System.Random random = new System.Random(PickupSeed);

        // 난파선 : 물가에 옆으로 기울어 절반쯤 묻힘
        GameObject wreck = Place(group, "zone_shipwreck", wreckPoint + Vector3.down * 0.7f, 78f, Vector3.one, building);

        if (wreck != null)
        {
            wreck.name = "Shipwreck";
            wreck.transform.rotation = Quaternion.Euler(4f, 78f, -9f);
            AddBoxCollider(wreck, new Vector3(0f, 1.3f, 0f), new Vector3(4.4f, 2.6f, 12.4f));
            GameObjectUtility.SetStaticEditorFlags(wreck, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
        }

        // 해변 소품 : 야자수 · 바위 · 떠밀려 온 통나무 · 상자 · 나무통 · 널빤지
        int props = 0;

        void Prop(string model, float x, float z, float yaw, float scale, bool collider, float sink = 0f)
        {
            GameObject placed = Place(group, model, OnGround(x, z, -sink), yaw, Vector3.one * scale, collider ? building : -1);

            if (placed == null)
            {
                return;
            }

            if (collider)
            {
                if (model == "zone_palm")
                {
                    CapsuleCollider trunk = placed.AddComponent<CapsuleCollider>();
                    trunk.center = new Vector3(0f, 3f, 0f);
                    trunk.radius = 0.3f;
                    trunk.height = 6f;
                }
                else
                {
                    AddBoundsCollider(placed);
                }
            }

            GameObjectUtility.SetStaticEditorFlags(placed, StaticEditorFlags.BatchingStatic);
            props++;
        }

        float shoreZ = wreckPoint.z - 2f; // 물가 (z)

        for (int index = 0; index < 7; index++)
        {
            float x = -60f + index * 20f + (float)random.NextDouble() * 8f;
            Prop("zone_palm", x, shoreZ + 38f + (float)random.NextDouble() * 10f, (float)random.NextDouble() * 360f, 0.9f + (float)random.NextDouble() * 0.3f, true);
        }

        Prop("rock_large", wreckPoint.x - 11f, shoreZ + 1f, 30f, 1.3f, true, 0.3f);
        Prop("rock_large", wreckPoint.x + 16f, shoreZ - 1f, 140f, 1.1f, true, 0.4f);
        Prop("rock_large", spawn.x + 24f, shoreZ + 6f, 210f, 0.9f, true, 0.2f);

        for (int index = 0; index < 6; index++)
        {
            Prop("rock_small", -30f + index * 11f + (float)random.NextDouble() * 4f, shoreZ + 2f + (float)random.NextDouble() * 8f, (float)random.NextDouble() * 360f, 0.8f + (float)random.NextDouble() * 0.6f, false);
        }

        Prop("fallen_log", spawn.x - 12f, spawn.z - 6f, 70f, 1f, true);
        Prop("fallen_log", spawn.x + 15f, spawn.z - 11f, 150f, 0.85f, true);
        Prop("prop_crate", wreckPoint.x + 6f, shoreZ + 5f, 20f, 1f, true);
        Prop("prop_crate", wreckPoint.x + 7.2f, shoreZ + 6.1f, 55f, 0.9f, true);
        Prop("prop_barrel", wreckPoint.x - 5f, shoreZ + 6f, 0f, 1f, true);
        Prop("prop_barrel", spawn.x + 5f, spawn.z - 9f, 0f, 1f, true);
        Prop("zone_wreck_planks", wreckPoint.x + 2f, shoreZ + 9f, 25f, 1f, false);
        Prop("zone_wreck_planks", spawn.x - 6f, spawn.z - 4f, 110f, 1f, false);
        Prop("zone_wreck_planks", wreckPoint.x + 12f, shoreZ + 3f, 70f, 1f, false);

        // 떠밀려 온 물건 (주울 수 있음 · 저장 ID 고정)
        WorldItemPickupRegistry registry = AssetDatabase.LoadAssetAtPath<WorldItemPickupRegistry>(PickupRegistryPath);
        (string itemId, float x, float z, int quantity)[] loot =
        {
            ("drink_water_bottle", spawn.x + 2f, spawn.z - 3f, 1),
            ("drink_water_bottle", wreckPoint.x + 5f, shoreZ + 7f, 1),
            ("food_apple", spawn.x - 3f, spawn.z - 2f, 2),
            ("food_apple", wreckPoint.x - 3f, shoreZ + 8f, 1),
            ("medicine_bandage", wreckPoint.x + 7f, shoreZ + 7.5f, 1),
            ("item_wood", spawn.x - 5f, spawn.z - 5f, 3),
            ("item_wood", wreckPoint.x + 3f, shoreZ + 10f, 2),
            ("item_plant_fiber", spawn.x + 6f, spawn.z - 7f, 3),
            ("item_plant_fiber", wreckPoint.x - 6f, shoreZ + 9f, 2)
        };
        int pickups = 0;
        Transform lootGroup = new GameObject("WashedUpItems").transform;
        lootGroup.SetParent(group, false);

        for (int index = 0; index < loot.Length; index++)
        {
            (string itemId, float x, float z, int quantity) = loot[index];

            if (registry == null || !registry.TryGetPickup(itemId, out WorldItemPickup prefab))
            {
                continue;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject, lootGroup);
            instance.transform.SetPositionAndRotation(OnGround(x, z, 0.05f), Quaternion.Euler(0f, index * 47f, 0f));
            instance.name = $"WashedUp_{itemId}_{index}";
            SerializedObject serialized = new SerializedObject(instance.GetComponent<WorldItemPickup>());
            serialized.FindProperty("quantity").intValue = quantity;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            WorldObjectIdentity identity = instance.GetComponent<WorldObjectIdentity>() ?? instance.AddComponent<WorldObjectIdentity>();
            identity.AssignWorldObjectId($"island_wreck_{index:00}_{itemId}");
            EditorUtility.SetDirty(identity);
            pickups++;
        }

        // 시작 위치 · 기본 부활 위치 · 카메라 (난파선을 바라보며 시작)
        PlayerMovement player = Object.FindFirstObjectByType<PlayerMovement>(FindObjectsInactive.Include);
        Vector3 look = wreckPoint - spawn;
        look.y = 0f;
        Quaternion facing = Quaternion.LookRotation(look.normalized);

        if (player != null)
        {
            player.transform.SetPositionAndRotation(spawn + Vector3.up * 0.05f, facing);
            EditorUtility.SetDirty(player.transform);
        }

        PlayerRespawnSystem respawn = Object.FindFirstObjectByType<PlayerRespawnSystem>(FindObjectsInactive.Include);
        Transform respawnTransform = respawn != null ? new SerializedObject(respawn).FindProperty("defaultRespawnPoint").objectReferenceValue as Transform : null;

        if (respawnTransform != null)
        {
            respawnTransform.SetPositionAndRotation(respawnPoint + Vector3.up * 0.05f, facing);
            EditorUtility.SetDirty(respawnTransform);
        }

        Camera main = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();

        if (main != null && player != null)
        {
            main.transform.position = player.transform.position - facing * Vector3.forward * 8f + Vector3.up * 6f;
            main.transform.LookAt(player.transform.position + Vector3.up * 1.4f);
            EditorUtility.SetDirty(main.transform);
        }

        // 바다 경계 (106일차: 먼 바다 파도 · 해안선은 SwimmingBuilder.ConfigureWaves가 채움)
        GameObject guardObject = new GameObject("ShoreGuard");
        guardObject.transform.SetParent(root, false);
        IslandShoreGuard guard = guardObject.AddComponent<IslandShoreGuard>();
        guard.EditorAssign(SeaLevel, player != null ? player.transform : null, respawnTransform, SwimmingBuilder.CoastRadii());

        return $"난파선 시작 해변 : 물가 z {shoreZ:0} · 난파선 {wreckPoint:F0} · 시작 {spawn:F0} (마을까지 {spawn.magnitude:0}m) · 소품 {props}개 · 떠밀려 온 물건 {pickups}개";
    }

    private static string ApplyAtmosphere() // 먼 섬 · 수평선이 자연스럽게 흐려지게
    {
        Camera main = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();

        if (main != null)
        {
            main.farClipPlane = 2600f;
            EditorUtility.SetDirty(main);
        }

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 350f;
        RenderSettings.fogEndDistance = 2400f;
        return "카메라 거리 2600m · 안개 350 ~ 2400m";
    }

    // ---------------------------------------------------------------- 검증

    public static string Validate(out int errorCount)
    {
        StringBuilder report = new StringBuilder("[무인도 검증]\n");
        int errors = 0;

        void Error(string message)
        {
            errors++;
            report.AppendLine("✗ " + message);
        }

        Terrain terrain = Object.FindFirstObjectByType<Terrain>(FindObjectsInactive.Include);

        if (EditorSceneManager.GetActiveScene().path != ScenePath || terrain == null)
        {
            report.AppendLine("Scene 검사 생략 (게임 Scene이 열려 있지 않음)");
            errorCount = errors;
            report.AppendLine("결과 : 오류 0개");
            return report.ToString();
        }

        if (!IsIslandTerrain(terrain) || terrain.terrainData.heightmapResolution != HeightResolution || Vector3.Distance(terrain.transform.position, new Vector3(-TerrainSize * 0.5f, TerrainBase, -TerrainSize * 0.5f)) > 0.01f)
        {
            Error($"Terrain이 무인도 크기가 아닙니다 ({terrain.terrainData.size}). 22번 메뉴를 실행하세요.");
            errorCount = errors;
            report.AppendLine($"결과 : 오류 {errors}개");
            return report.ToString();
        }

        float Ground(float x, float z) => terrain.SampleHeight(new Vector3(x, 0f, z)) + terrain.transform.position.y;

        // 마을 · 기존 구역 평지 (높이 0 유지)
        int flat = 0;
        float worst = 0f;

        for (float x = -124f; x <= 124f; x += 8f)
        {
            for (float z = -124f; z <= 124f; z += 8f)
            {
                if (LagoonDistance(x, z) < LagoonBank + 1f)
                {
                    continue;
                }

                float delta = Mathf.Abs(Ground(x, z));
                worst = Mathf.Max(worst, delta);
                flat += delta < 0.03f ? 1 : 0;
            }
        }

        if (worst >= 0.03f)
        {
            Error($"마을 평지 높이가 0에서 {worst:0.00}m 벗어났습니다.");
        }

        // 사방 해안 · 가장자리 바다
        int coastOk = 0;
        float nearest = float.MaxValue;
        float farthest = 0f;

        for (int step = 0; step < 72; step++)
        {
            float angle = step * 5f;
            Vector2 direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            float found = -1f;

            for (float radius = 300f; radius < TerrainSize * 0.5f; radius += 2f)
            {
                Vector2 point = direction * radius;

                if (Ground(point.x, point.y) < SeaLevel)
                {
                    found = radius;
                    break;
                }
            }

            if (found < 650f || found > 960f)
            {
                Error($"{angle:0}° 방향 해안선이 {found:0}m에 있습니다 (650 ~ 960m 필요).");
                continue;
            }

            coastOk++;
            nearest = Mathf.Min(nearest, found);
            farthest = Mathf.Max(farthest, found);
        }

        for (int step = 0; step < 16; step++)
        {
            float t = step / 16f * 4f;
            float half = TerrainSize * 0.5f - 2f;
            Vector2 point = t < 1f ? new Vector2(-half + t * 2f * half, -half) : t < 2f ? new Vector2(half, -half + (t - 1f) * 2f * half) : t < 3f ? new Vector2(half - (t - 2f) * 2f * half, half) : new Vector2(-half, half - (t - 3f) * 2f * half);

            if (Ground(point.x, point.y) > SeaLevel - 25f)
            {
                Error($"Terrain 가장자리 {point}가 깊은 바다가 아닙니다.");
            }
        }

        GameObject root = GameObject.Find(RootName);
        Transform ocean = root != null ? root.transform.Find("Sea/Ocean") : null;

        if (ocean == null || Mathf.Abs(ocean.position.y - SeaLevel) > 0.01f || ocean.lossyScale.x < 4000f)
        {
            Error("사방 바다 판이 없거나 수면 높이가 다릅니다. 22번 메뉴를 실행하세요.");
        }

        GameObject zones = GameObject.Find(WorldZoneBuilder.RootName);

        if (zones != null && zones.GetComponentsInChildren<Transform>(true).Any(item => item.name.StartsWith("Edge_", StringComparison.Ordinal) || item.name.StartsWith("SeaWall_", StringComparison.Ordinal) || item.name == "Sea" || item.name == "Seabed"))
        {
            Error("104일차 섬 가장자리 벽 · 동쪽 바다가 남아 있습니다. 19번(18번) 메뉴를 다시 실행하세요.");
        }

        // 난파선 시작
        (Vector3 spawn, Vector3 wreckPoint, Vector3 respawnPoint) = StartPoints();
        PlayerMovement player = Object.FindFirstObjectByType<PlayerMovement>(FindObjectsInactive.Include);
        PlayerRespawnSystem respawn = Object.FindFirstObjectByType<PlayerRespawnSystem>(FindObjectsInactive.Include);
        Transform respawnTransform = respawn != null ? new SerializedObject(respawn).FindProperty("defaultRespawnPoint").objectReferenceValue as Transform : null;

        if (root == null || root.transform.Find("ShipwreckBeach/Shipwreck") == null)
        {
            Error("남쪽 해변 난파선이 없습니다.");
        }

        if (player == null || Vector3.Distance(player.transform.position, spawn) > 1f || Ground(spawn.x, spawn.z) < SeaLevel + 0.6f)
        {
            Error($"시작 위치가 난파선 해변이 아니거나 물가에 너무 가깝습니다 ({player?.transform.position}).");
        }

        if (respawnTransform == null || Vector3.Distance(respawnTransform.position, respawnPoint) > 1f)
        {
            Error("기본 부활 위치가 시작 해변이 아닙니다.");
        }

        WorldItemPickup[] loot = root != null ? root.GetComponentsInChildren<WorldItemPickup>(true) : new WorldItemPickup[0];
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);

        if (loot.Length < 8 || loot.Any(pickup => pickup.GetComponent<WorldObjectIdentity>() == null || !pickup.GetComponent<WorldObjectIdentity>().HasValidId || !ids.Add(pickup.GetComponent<WorldObjectIdentity>().WorldObjectId)))
        {
            Error($"떠밀려 온 물건이 {loot.Length}개이거나 저장 ID가 비었거나 겹칩니다 (8개 이상 필요).");
        }

        // 해변 → 마을 오솔길 : 걸을 수 있는 경사
        List<Vector3> trail = TrailPoints();
        float steepest = 0f;

        for (int index = 0; index + 1 < trail.Count; index++)
        {
            float length = Vector3.Distance(trail[index], trail[index + 1]);

            for (float d = 0f; d < length; d += 4f)
            {
                Vector3 point = Vector3.Lerp(trail[index], trail[index + 1], d / length);
                Vector3 normalized = new Vector3((point.x - terrain.transform.position.x) / TerrainSize, 0f, (point.z - terrain.transform.position.z) / TerrainSize);
                steepest = Mathf.Max(steepest, terrain.terrainData.GetSteepness(normalized.x, normalized.z));
            }
        }

        if (steepest > 30f)
        {
            Error($"해변 → 마을 오솔길 경사가 {steepest:0}°입니다 (30° 이하 필요).");
        }

        IslandShoreGuard guard = root != null ? root.GetComponentInChildren<IslandShoreGuard>(true) : null;

        if (guard == null || guard.Player == null || guard.FallbackPoint == null || !Mathf.Approximately(guard.SeaLevel, SeaLevel))
        {
            Error("바다 경계(ShoreGuard)가 없거나 플레이어 · 시작 해변 연결이 비었습니다.");
        }

        NavMeshSurface surface = Object.FindFirstObjectByType<NavMeshSurface>(FindObjectsInactive.Include);

        if (surface == null || surface.collectObjects != CollectObjects.Volume || surface.size.x < NavMeshSize - 1f || Mathf.Abs(surface.transform.position.y + surface.center.y - surface.size.y * 0.5f - NavMeshBottom) > 0.05f)
        {
            Error("NavMesh 범위가 섬 뭍 전체(수면 위)가 아닙니다. 22번 메뉴를 실행하세요.");
        }

        KoreanFontBuilder.Validate(new[] { IslandShoreGuard.BlockedMessage }, Error, new StringBuilder());

        report.AppendLine($"Terrain {terrain.terrainData.size.x:0}m · 마을 평지 {flat}곳 높이 0 (최대 차이 {worst:0.000}m)");
        report.AppendLine($"해안선 {coastOk}/72 방향 · 반지름 {nearest:0} ~ {farthest:0}m (섬 지름 약 {(nearest + farthest):0}m)");
        report.AppendLine($"시작 해변 {spawn:F0} · 마을까지 {spawn.magnitude:0}m · 떠밀려 온 물건 {loot.Length}개 · 오솔길 가장 가파른 곳 {steepest:0}°");
        errorCount = errors;
        report.AppendLine(errors == 0 ? "결과 : 오류 0개" : $"결과 : 오류 {errors}개");
        return report.ToString();
    }
}
