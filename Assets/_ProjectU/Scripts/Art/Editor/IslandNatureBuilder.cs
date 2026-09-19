using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

// 108일차: 넓은 섬 다듬기
// 1. 섬 자연 : 땅 모양(해변 · 들판 · 숲 · 산비탈 · 눈 덮인 산 · 습지 둘레)에 맞춰 나무 · 바위 · 덤불을 Terrain 나무로 심는다
//    (Scene 오브젝트가 아니라 Terrain에 저장되어 가볍고, 멀리 있는 나무는 Terrain이 알아서 그리지 않는다)
//    마을 · 흙길 · 오솔길 · 구역 터 · 난파선 해변 · 석호 · 물가는 비운다
// 2. 흙길 옆 채집 나무 · 돌 (도끼 · 곡괭이, 저장 ID 고정)
// 3. 구역 사이 들판의 적 생성 지점 (흙길에서 조금 떨어진 곳, 근접형 · 원거리형 번갈아)
// 4. 지도 : 섬 전체를 볼 수 있게 줌 범위를 넓히고, 멀리 볼수록 섬 가운데를 보여 줌
// 여러 번 실행해도 같은 결과가 나온다 (칸마다 정해진 무작위 값).
public static class IslandNatureBuilder
{
    public const string RootName = "=== Island Nature ===";
    public const string ResourceIdPrefix = "island_res_";
    public const string FieldSpawnPrefix = "spawn_field_";
    private const string ScenePath = "Assets/_ProjectU/Scenes/20_Gameplay.unity";
    private const string DialogTitle = "Project U 섬 자연 · 들판";
    private const string PrototypeFolder = "Assets/_ProjectU/Art/Generated/Prefabs/Nature";
    private const string TreeResourcePath = "Assets/_ProjectU/Prefabs/Gathering/TreeResource_01.prefab";
    private const string StoneResourcePath = "Assets/_ProjectU/Prefabs/Gathering/StoneResource_01.prefab";
    private const string SpawnRootName = "=== Enemy Spawn Points ===";
    private const float CellSize = 7f; // 심을 자리 칸 (한 칸에 하나까지)
    public const float VillageClear = 138f; // 마을 · 기존 나무가 있는 가운데 (심지 않음)
    public const float RoadClear = 7f; // 흙길 양옆 비움
    public const float TreeDistance = 800f; // 나무를 그리는 거리

    private enum Shape
    {
        Tree, // 줄기 충돌 (굵은 기둥)
        Rock, // 몸통 충돌
        Bush, // 충돌 없음
        Stump // 낮은 충돌
    }

    // Terrain 나무 종류 (순서 = Terrain 나무 번호)
    private static readonly (string model, Shape shape)[] Prototypes =
    {
        ("tree_round", Shape.Tree),
        ("tree_round_b", Shape.Tree),
        ("tree_pine", Shape.Tree),
        ("tree_autumn", Shape.Tree),
        ("zone_snow_pine", Shape.Tree),
        ("zone_palm", Shape.Tree),
        ("zone_dead_tree", Shape.Tree),
        ("rock_large", Shape.Rock),
        ("rock_small", Shape.Bush),
        ("zone_ice_rock", Shape.Rock),
        ("bush", Shape.Bush),
        ("bush_berry", Shape.Bush),
        ("stump", Shape.Stump),
        ("fallen_log", Shape.Bush)
    };

    private const int RoundTree = 0, RoundTreeB = 1, Pine = 2, Autumn = 3, SnowPine = 4, Palm = 5, DeadTree = 6, RockLarge = 7, RockSmall = 8, IceRock = 9, Bush = 10, BerryBush = 11, Stump = 12, Log = 13;

    // ---------------------------------------------------------------- 메뉴

    public static string BuildAll(bool saveScene)
    {
        StringBuilder report = new StringBuilder("[섬 자연 · 들판 만들기]\n");
        Terrain terrain = Object.FindFirstObjectByType<Terrain>(FindObjectsInactive.Include);

        if (EditorSceneManager.GetActiveScene().path != ScenePath || !IslandTerrainBuilder.IsIslandTerrain(terrain))
        {
            report.AppendLine("✗ 게임 Scene을 열고 22번 메뉴로 무인도를 먼저 만드세요.");
            return report.ToString();
        }

        DateTime started = DateTime.Now;

        try
        {
            EditorUtility.DisplayProgressBar(DialogTitle, "숲 바닥 칠하기", 0.2f);
            report.AppendLine(IslandTerrainBuilder.RepaintIslandGround(terrain));
            EditorUtility.DisplayProgressBar(DialogTitle, "나무 · 바위 심기", 0.5f);
            report.Append(PlantAll(terrain));
            EditorUtility.DisplayProgressBar(DialogTitle, "NavMesh 다시 굽기", 0.8f);
            report.AppendLine(WorldZoneBuilder.RebakeNavMesh());
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

    public static string PlantAll(Terrain terrain) // 22번 메뉴도 사용 (숲 바닥은 섬 바닥 칠하기에서, NavMesh는 19번에서)
    {
        StringBuilder report = new StringBuilder();
        List<Vector3> resources = new List<Vector3>();
        report.AppendLine(PlaceResources(terrain, resources));
        report.AppendLine(PlantTrees(terrain, resources));
        report.AppendLine(PlaceFieldSpawns(terrain));
        report.AppendLine(ConfigureMap());
        return report.ToString();
    }

    // ---------------------------------------------------------------- 땅 모양

    private static float SmoothStep(float edge0, float edge1, float value)
    {
        float t = Mathf.Clamp01((value - edge0) / (edge1 - edge0));
        return t * t * (3f - 2f * t);
    }

    public static float ForestNoise(float x, float z) // 0 ~ 1 숲 무늬 (넓은 숲 덩어리 + 작은 빈터)
    {
        return Mathf.PerlinNoise(x / 230f + 17.3f, z / 230f + 41.9f) * 0.65f + Mathf.PerlinNoise(x / 80f + 3.3f, z / 80f + 7.7f) * 0.35f;
    }

    public static float ForestFloor(float x, float z, float height) // 숲 바닥 칠하기 비율 (섬 바닥 칠하기에서 사용)
    {
        if (height < IslandTerrainBuilder.SeaLevel + 3f || height > 110f)
        {
            return 0f;
        }

        float forest = ForestNoise(x, z);
        float lowland = SmoothStep(0.5f, 0.6f, forest) * (1f - SmoothStep(40f, 50f, height));
        float mountain = SmoothStep(0.42f, 0.55f, forest) * SmoothStep(40f, 50f, height) * (1f - SmoothStep(95f, 110f, height));
        float amount = Mathf.Max(lowland, mountain) * 0.85f;

        if (amount <= 0.001f)
        {
            return 0f;
        }

        // 가장자리가 네모 · 줄로 잘려 보이지 않게 마을 · 흙길 · 구역은 부드럽게 비움
        float radial = Mathf.Sqrt(x * x + z * z) + (Mathf.PerlinNoise(x / 45f + 5.5f, z / 45f + 1.7f) - 0.5f) * 40f; // 둥글고 울퉁불퉁한 가장자리
        float village = SmoothStep(150f, 215f, radial);
        float road = SmoothStep(RoadClear - 3f, RoadClear + 5f, IslandZoneLayout.RoadDistanceWorld(new Vector3(x, 0f, z)));
        float zone = 1f;

        foreach (IslandZoneLayout.Site site in IslandZoneLayout.Sites)
        {
            zone = Mathf.Min(zone, site.Coast ? (x - site.Offset.x < 40f ? 1f : 0f) : 1f - SmoothStep(0.15f, 0.35f, IslandZoneLayout.PadWeight(site, x, z)));
        }

        amount *= village * road * zone;
        return amount > 0.001f && IsClear(x, z, -12f) ? amount : 0f; // 시작 해변 · 오솔길 · 석호는 조금 좁게 비움
    }

    private static float Hash(int x, int z, int salt) // 칸마다 정해진 무작위 값 0 ~ 1
    {
        unchecked
        {
            uint h = (uint)(x * 73856093) ^ (uint)(z * 19349663) ^ (uint)(salt * 83492791) ^ 0x9E3779B9u;
            h ^= h >> 16;
            h *= 0x7feb352du;
            h ^= h >> 15;
            h *= 0x846ca68bu;
            h ^= h >> 16;
            return (h & 0xFFFFFF) / (float)0x1000000;
        }
    }

    public static bool IsClear(float x, float z, float extra) // 심어도 되는 곳인지 (마을 · 흙길 · 오솔길 · 구역 · 시작 해변 · 석호)
    {
        if (Mathf.Abs(x) < VillageClear + extra && Mathf.Abs(z) < VillageClear + extra)
        {
            return false;
        }

        if (IslandTerrainBuilder.LagoonDistance(x, z) < 12f + extra)
        {
            return false;
        }

        Vector3 point = new Vector3(x, 0f, z);

        if (IslandZoneLayout.RoadDistanceWorld(point) < RoadClear + extra)
        {
            return false;
        }

        PrepareLandmarks();

        for (int index = 0; index + 1 < trail.Count; index++)
        {
            if (StylizedTerrainPainter.DistanceToSegmentXZ(point, trail[index], trail[index + 1]) < 8f + extra)
            {
                return false;
            }
        }

        if (Flat(point, startSpawn) < 75f + extra || Flat(point, startWreck) < 45f + extra)
        {
            return false;
        }

        foreach (IslandZoneLayout.Site site in IslandZoneLayout.Sites)
        {
            if (site.Coast)
            {
                float designX = x - site.Offset.x;
                float designZ = z - site.Offset.y;

                if (designX > 40f - extra && Mathf.Abs(designZ - 4f) < 100f + extra)
                {
                    return false;
                }

                continue;
            }

            if (IslandZoneLayout.PadWeight(site, x, z) > 0.35f)
            {
                return false;
            }
        }

        return true;
    }

    private static List<Vector3> trail; // 시작 해변 → 마을 오솔길 (한 번만 계산)
    private static Vector3 startSpawn; // 시작 위치
    private static Vector3 startWreck; // 난파선

    private static void PrepareLandmarks()
    {
        if (trail != null)
        {
            return;
        }

        trail = IslandTerrainBuilder.TrailPoints();
        (startSpawn, startWreck, _) = IslandTerrainBuilder.StartPoints();
    }

    private static float Flat(Vector3 a, Vector3 b)
    {
        return new Vector2(a.x - b.x, a.z - b.z).magnitude;
    }

    // ---------------------------------------------------------------- Terrain 나무 종류

    private static GameObject EnsurePrototype(string modelId, Shape shape)
    {
        StylizedArtAssetFactory.EnsureFolder(PrototypeFolder);
        string path = $"{PrototypeFolder}/NP_{modelId}.prefab";
        GameObject model = StylizedArtAssetFactory.GetOrCreateModelPrefab(modelId, false);

        if (model == null)
        {
            return null;
        }

        MeshFilter sourceFilter = model.GetComponent<MeshFilter>();
        MeshRenderer sourceRenderer = model.GetComponent<MeshRenderer>();
        GameObject holder = new GameObject("NP_" + modelId);
        holder.AddComponent<MeshFilter>().sharedMesh = sourceFilter.sharedMesh;
        MeshRenderer renderer = holder.AddComponent<MeshRenderer>();
        renderer.sharedMaterials = sourceRenderer.sharedMaterials;
        Bounds bounds = sourceFilter.sharedMesh.bounds;

        if (shape != Shape.Bush)
        {
            CapsuleCollider capsule = holder.AddComponent<CapsuleCollider>();

            switch (shape)
            {
                case Shape.Tree:
                    capsule.radius = 0.35f;
                    capsule.height = Mathf.Min(3f, bounds.size.y);
                    break;
                case Shape.Stump:
                    capsule.radius = Mathf.Max(0.2f, Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.8f);
                    capsule.height = Mathf.Max(capsule.radius * 2f, bounds.size.y);
                    break;
                default:
                    capsule.radius = Mathf.Max(0.25f, Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.85f);
                    capsule.height = Mathf.Max(capsule.radius * 2f, bounds.size.y);
                    break;
            }

            capsule.center = new Vector3(0f, capsule.height * 0.5f, 0f);
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(holder, path);
        Object.DestroyImmediate(holder);
        return prefab;
    }

    // ---------------------------------------------------------------- 나무 · 바위 · 덤불

    private static string PlantTrees(Terrain terrain, List<Vector3> keepClear)
    {
        TerrainData data = terrain.terrainData;
        TreePrototype[] prototypes = new TreePrototype[Prototypes.Length];

        for (int index = 0; index < Prototypes.Length; index++)
        {
            GameObject prefab = EnsurePrototype(Prototypes[index].model, Prototypes[index].shape);

            if (prefab == null)
            {
                return $"✗ 나무 모델({Prototypes[index].model})을 만들지 못했습니다.";
            }

            prototypes[index] = new TreePrototype { prefab = prefab, bendFactor = 0f };
        }

        data.treePrototypes = prototypes;
        List<TreeInstance> instances = new List<TreeInstance>();
        int[] counts = new int[Prototypes.Length];
        Vector3 origin = terrain.transform.position;
        Vector3 size = data.size;
        Vector3 swamp = IslandZoneLayout.ToWorld("swamp", new Vector3(-60f, 0f, -96f));
        float half = IslandTerrainBuilder.TerrainSize * 0.5f - 30f;
        int cells = Mathf.CeilToInt(half * 2f / CellSize);

        for (int gz = 0; gz < cells; gz++)
        {
            for (int gx = 0; gx < cells; gx++)
            {
                float x = -half + (gx + Hash(gx, gz, 1)) * CellSize;
                float z = -half + (gz + Hash(gx, gz, 2)) * CellSize;
                Vector3 point = new Vector3(x, 0f, z);
                float height = terrain.SampleHeight(point) + origin.y;

                if (height < IslandTerrainBuilder.SeaLevel + 0.9f || !IsClear(x, z, 0f) || keepClear.Any(item => Flat(item, point) < 5f))
                {
                    continue;
                }

                float nx = (x - origin.x) / size.x;
                float nz = (z - origin.z) / size.z;
                float slope = data.GetSteepness(nx, nz);
                float coast = IslandTerrainBuilder.CoastRadiusAt(Mathf.Atan2(z, x)) - Mathf.Sqrt(x * x + z * z);
                float roll = Hash(gx, gz, 3);
                float pick = Hash(gx, gz, 4);
                int prototype = -1;

                if (slope > 36f) // 가파른 곳 : 바위만
                {
                    prototype = roll < 0.03f ? RockLarge : -1;
                }
                else if (coast < 70f && height < 4.5f) // 해변
                {
                    prototype = roll < 0.022f && slope < 20f ? Palm : roll < 0.03f ? RockSmall : -1;
                }
                else if (height > 120f) // 눈 덮인 산
                {
                    prototype = roll < 0.03f ? RockLarge : roll < 0.05f ? IceRock : roll < 0.065f && height < 150f ? SnowPine : -1;
                }
                else if (height > 45f) // 산비탈
                {
                    float forest = ForestNoise(x, z);
                    float pineChance = 0.06f + 0.3f * SmoothStep(0.42f, 0.6f, forest);

                    prototype = roll < pineChance ? (height > 85f || pick < 0.25f ? SnowPine : Pine)
                        : roll < pineChance + 0.035f ? (pick < 0.6f ? RockLarge : RockSmall)
                        : roll < pineChance + 0.055f ? Bush : -1;
                }
                else // 들판 · 숲
                {
                    float forest = SmoothStep(0.5f, 0.6f, ForestNoise(x, z));
                    float edge = SmoothStep(0.44f, 0.52f, ForestNoise(x, z)) * (1f - forest);
                    float swampNear = 1f - SmoothStep(90f, 150f, Flat(point, swamp));
                    float treeChance = 0.012f + 0.43f * forest + 0.07f * edge;
                    float bushChance = 0.03f + 0.1f * forest + 0.05f * edge;

                    if (roll < treeChance)
                    {
                        if (pick < swampNear * 0.6f)
                        {
                            prototype = DeadTree;
                        }
                        else
                        {
                            float kind = Mathf.PerlinNoise(x / 60f + 9.1f, z / 60f + 2.4f);
                            prototype = kind < 0.36f ? RoundTree : kind < 0.55f ? RoundTreeB : kind < 0.76f ? Pine : Autumn;
                        }
                    }
                    else if (roll < treeChance + bushChance)
                    {
                        prototype = pick < 0.75f ? Bush : BerryBush;
                    }
                    else if (roll < treeChance + bushChance + 0.012f)
                    {
                        prototype = pick < 0.5f ? RockLarge : RockSmall;
                    }
                    else if (roll < treeChance + bushChance + 0.012f + 0.004f + 0.02f * forest)
                    {
                        prototype = pick < 0.55f ? Stump : Log;
                    }
                }

                if (prototype < 0)
                {
                    continue;
                }

                float scale;

                switch (Prototypes[prototype].shape)
                {
                    case Shape.Tree:
                        scale = Mathf.Lerp(0.85f, 1.5f, Hash(gx, gz, 5));
                        break;
                    case Shape.Rock:
                        scale = Mathf.Lerp(0.9f, 2.2f, Hash(gx, gz, 5));
                        break;
                    default:
                        scale = Mathf.Lerp(0.7f, 1.4f, Hash(gx, gz, 5));
                        break;
                }

                float tint = Mathf.Lerp(0.88f, 1f, Hash(gx, gz, 6));
                instances.Add(new TreeInstance
                {
                    position = new Vector3(nx, (height - origin.y) / size.y, nz),
                    widthScale = scale,
                    heightScale = scale * Mathf.Lerp(0.92f, 1.08f, Hash(gx, gz, 7)),
                    rotation = Hash(gx, gz, 8) * Mathf.PI * 2f,
                    color = new Color(tint, tint, tint, 1f),
                    lightmapColor = Color.white,
                    prototypeIndex = prototype
                });
                counts[prototype]++;
            }
        }

        data.SetTreeInstances(instances.ToArray(), true);
        terrain.treeDistance = TreeDistance;
        terrain.treeBillboardDistance = TreeDistance;
        terrain.drawTreesAndFoliage = true;
        TerrainCollider collider = terrain.GetComponent<TerrainCollider>();

        if (collider != null) // 나무 · 바위에 부딪힘
        {
            SerializedObject serialized = new SerializedObject(collider);
            SerializedProperty treeColliders = serialized.FindProperty("m_EnableTreeColliders");

            if (treeColliders != null && !treeColliders.boolValue)
            {
                treeColliders.boolValue = true;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        EditorUtility.SetDirty(terrain);
        EditorUtility.SetDirty(data);
        int trees = counts[RoundTree] + counts[RoundTreeB] + counts[Pine] + counts[Autumn] + counts[SnowPine] + counts[Palm] + counts[DeadTree];
        int rocks = counts[RockLarge] + counts[RockSmall] + counts[IceRock];
        int bushes = counts[Bush] + counts[BerryBush] + counts[Stump] + counts[Log];
        return $"섬 자연 (Terrain 나무) : 나무 {trees} (야자수 {counts[Palm]} · 눈 소나무 {counts[SnowPine]} · 마른 나무 {counts[DeadTree]}) · 바위 {rocks} · 덤불 · 그루터기 {bushes} · 그리는 거리 {TreeDistance:0}m";
    }

    // ---------------------------------------------------------------- 흙길 옆 채집 나무 · 돌

    private static string PlaceResources(Terrain terrain, List<Vector3> placed)
    {
        GameObject old = GameObject.Find(RootName);

        if (old != null)
        {
            Object.DestroyImmediate(old);
        }

        GameObject treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TreeResourcePath);
        GameObject stonePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StoneResourcePath);

        if (treePrefab == null || stonePrefab == null)
        {
            return "✗ 채집 나무 · 돌 Prefab이 없습니다.";
        }

        Transform root = new GameObject(RootName).transform;
        Transform group = new GameObject("Resources").transform;
        group.SetParent(root, false);
        int trees = 0;
        int stones = 0;

        for (int road = 0; road < IslandZoneLayout.RoadCount; road++)
        {
            float length = IslandZoneLayout.RoadLength(road);
            int index = 0;

            for (float along = IslandZoneLayout.Roads[road].Kind == IslandZoneLayout.RoadKind.Main ? 110f : 40f; along < length - 40f; along += 90f, index++)
            {
                Vector3 a = IslandZoneLayout.RoadPoint(road, along);
                Vector3 b = IslandZoneLayout.RoadPoint(road, Mathf.Min(length, along + 2f));
                Vector3 side = Vector3.Cross(Vector3.up, (b - a).normalized) * (index % 2 == 0 ? 1f : -1f);
                bool stone = Hash(road, index, 11) < 0.45f;
                Vector3 point = a + side * Mathf.Lerp(12f, 22f, Hash(road, index, 12));
                point.y = terrain.SampleHeight(point) + terrain.transform.position.y;
                Vector3 normalized = new Vector3((point.x - terrain.transform.position.x) / terrain.terrainData.size.x, 0f, (point.z - terrain.transform.position.z) / terrain.terrainData.size.z);

                if (!IsClear(point.x, point.z, 0f) || point.y < IslandTerrainBuilder.SeaLevel + 1.5f || terrain.terrainData.GetSteepness(normalized.x, normalized.z) > 24f)
                {
                    continue;
                }

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(stone ? stonePrefab : treePrefab, group);
                instance.transform.SetPositionAndRotation(point, Quaternion.Euler(0f, Hash(road, index, 13) * 360f, 0f));
                string kind = stone ? "stone" : "tree";
                instance.name = $"Field_{kind}_{IslandZoneLayout.Roads[road].Id}_{index:00}";
                WorldObjectIdentity identity = instance.GetComponent<WorldObjectIdentity>();

                if (identity == null)
                {
                    identity = instance.AddComponent<WorldObjectIdentity>();
                }

                identity.AssignWorldObjectId($"{ResourceIdPrefix}{IslandZoneLayout.Roads[road].Id}_{index:00}_{kind}");
                EditorUtility.SetDirty(identity);
                GameObjectUtility.SetStaticEditorFlags(instance, 0);
                placed.Add(point);

                if (stone)
                {
                    stones++;
                }
                else
                {
                    trees++;
                }
            }
        }

        return $"흙길 옆 채집 자원 : 나무 {trees} · 돌 {stones} (저장 ID {ResourceIdPrefix}..)";
    }

    // ---------------------------------------------------------------- 들판의 적

    private static string PlaceFieldSpawns(Terrain terrain)
    {
        GameObject root = GameObject.Find(SpawnRootName);

        if (root == null)
        {
            return "들판 적 생성 지점 : 적 생성 루트가 없어 건너뜀";
        }

        foreach (Transform child in root.transform.Cast<Transform>().ToList())
        {
            EnemySpawnPoint existing = child.GetComponent<EnemySpawnPoint>();

            if (existing != null && existing.SpawnPointId.StartsWith(FieldSpawnPrefix, StringComparison.Ordinal))
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }

        Transform melee = root.transform.Find("Spawn_MeleeGrunt_01");
        Transform ranged = root.transform.Find("Spawn_RangedSpitter_01");

        if (melee == null || ranged == null)
        {
            return "✗ 적 생성 지점 틀(Spawn_MeleeGrunt_01 · Spawn_RangedSpitter_01)이 없습니다.";
        }

        (Vector3 spawn, _, _) = IslandTerrainBuilder.StartPoints();
        int made = 0;
        float[] fractions = { 0.3f, 0.5f, 0.7f, 0.88f };
        List<Vector3> used = new List<Vector3>();

        for (int road = 0; road < IslandZoneLayout.RoadCount && made < 12; road++)
        {
            float length = IslandZoneLayout.RoadLength(road);

            for (int slot = 0; slot < fractions.Length && made < 12; slot++)
            {
                Vector3 a = IslandZoneLayout.RoadPoint(road, length * fractions[slot]);
                Vector3 b = IslandZoneLayout.RoadPoint(road, length * fractions[slot] + 2f);
                Vector3 across = Vector3.Cross(Vector3.up, (b - a).normalized);
                Vector3 point = Vector3.zero;
                bool found = false;

                foreach (float sideSign in (road + slot) % 2 == 0 ? new[] { 1f, -1f } : new[] { -1f, 1f }) // 한쪽이 안 되면 반대쪽
                {
                    point = a + across * (sideSign * 50f);
                    point.y = terrain.SampleHeight(point) + terrain.transform.position.y;
                    Vector3 candidate = point;
                    bool nearZone = IslandZoneLayout.Sites.Any(site => !site.Coast && Flat(candidate, new Vector3(site.PadCenter.x + site.Offset.x, 0f, site.PadCenter.y + site.Offset.y)) < Mathf.Max(site.PadRadius.x, site.PadRadius.y) + 70f)
                        || candidate.x - IslandZoneLayout.OffsetOf("coast").x > 20f;

                    if (Flat(point, Vector3.zero) >= 230f && Flat(point, spawn) >= 180f && !nearZone && IsClear(point.x, point.z, 0f) && point.y >= IslandTerrainBuilder.SeaLevel + 1.5f && used.All(other => Flat(other, candidate) > 90f))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    continue;
                }

                used.Add(point);
                Transform template = made % 2 == 0 ? melee : ranged;
                GameObject clone = Object.Instantiate(template.gameObject, root.transform);
                string id = $"{FieldSpawnPrefix}{made:00}";
                clone.name = "Spawn_field_" + made.ToString("00");
                clone.transform.SetPositionAndRotation(point, template.rotation);
                SerializedObject serialized = new SerializedObject(clone.GetComponent<EnemySpawnPoint>());
                serialized.FindProperty("spawnPointId").stringValue = id;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                made++;
            }
        }

        return $"들판 적 생성 지점 {made}곳 (흙길에서 50m 옆, 근접형 · 원거리형 번갈아)";
    }

    // ---------------------------------------------------------------- 지도

    private static string ConfigureMap()
    {
        MinimapCameraController map = Object.FindFirstObjectByType<MinimapCameraController>(FindObjectsInactive.Include);

        if (map == null)
        {
            return "✗ 지도 카메라(MinimapCameraController)가 없습니다.";
        }

        SerializedObject serialized = new SerializedObject(map);
        serialized.FindProperty("fullMapDefaultOrthographicSize").floatValue = 320f;
        serialized.FindProperty("fullMapMinimumOrthographicSize").floatValue = 45f;
        serialized.FindProperty("fullMapMaximumOrthographicSize").floatValue = 1000f;
        serialized.FindProperty("fullMapZoomFactor").floatValue = 1.2f;
        serialized.FindProperty("fullMapCenter").vector3Value = Vector3.zero;
        serialized.FindProperty("minimumCameraHeight").floatValue = 260f;
        serialized.FindProperty("hideTreesOnMap").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(map);
        return "지도 : 전체 지도 기본 640m · 가장 넓게 2000m(섬 전체) · 멀리 볼수록 섬 가운데 · 지도에서는 나무 대신 숲 바닥 색";
    }

    // ---------------------------------------------------------------- 검증

    public static string Validate(out int errorCount)
    {
        StringBuilder report = new StringBuilder("[섬 자연 · 들판 검증]\n");
        int errors = 0;

        void Error(string message)
        {
            errors++;
            report.AppendLine("✗ " + message);
        }

        Terrain terrain = Object.FindFirstObjectByType<Terrain>(FindObjectsInactive.Include);

        if (EditorSceneManager.GetActiveScene().path != ScenePath || !IslandTerrainBuilder.IsIslandTerrain(terrain))
        {
            report.AppendLine("Scene 검사 생략 (무인도 게임 Scene이 열려 있지 않음)");
            errorCount = 0;
            report.AppendLine("결과 : 오류 0개");
            return report.ToString();
        }

        TerrainData data = terrain.terrainData;
        TreeInstance[] instances = data.treeInstances;
        Vector3 origin = terrain.transform.position;
        Vector3 size = data.size;
        int wrongPlace = 0;
        string example = string.Empty;

        if (data.treePrototypes.Length != Prototypes.Length || data.treePrototypes.Any(prototype => prototype.prefab == null))
        {
            Error("Terrain 나무 종류가 맞지 않습니다. 24번 메뉴를 실행하세요.");
        }
        else
        {
            for (int index = 0; index < Prototypes.Length; index++)
            {
                GameObject prefab = data.treePrototypes[index].prefab;

                if (prefab.name != "NP_" + Prototypes[index].model || (Prototypes[index].shape == Shape.Tree && prefab.GetComponent<CapsuleCollider>() == null))
                {
                    Error($"Terrain 나무 {index}번 모양 · 충돌이 다릅니다 ({prefab.name}).");
                }
            }
        }

        if (instances.Length < 3000 || instances.Length > 25000)
        {
            Error($"섬 나무 · 바위 개수가 {instances.Length}개입니다 (3000 ~ 25000 필요). 24번 메뉴를 실행하세요.");
        }

        foreach (TreeInstance instance in instances)
        {
            float x = origin.x + instance.position.x * size.x;
            float z = origin.z + instance.position.z * size.z;
            float y = origin.y + instance.position.y * size.y;

            if (!IsClear(x, z, -1.5f) || y < IslandTerrainBuilder.SeaLevel + 0.5f)
            {
                wrongPlace++;
                example = $"({x:0}, {z:0})";
            }
        }

        if (wrongPlace > 0)
        {
            Error($"나무 · 바위 {wrongPlace}개가 마을 · 흙길 · 구역 · 시작 해변 · 물 위에 있습니다 (예 {example}).");
        }

        if (terrain.treeDistance < 300f || !terrain.drawTreesAndFoliage)
        {
            Error("나무를 그리는 거리가 너무 짧거나 나무를 그리지 않습니다.");
        }

        // 채집 자원
        GameObject root = GameObject.Find(RootName);
        GatherableResource[] resources = root != null ? root.GetComponentsInChildren<GatherableResource>(true) : new GatherableResource[0];
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);

        if (resources.Length < 20)
        {
            Error($"흙길 옆 채집 자원이 {resources.Length}개입니다 (20개 이상 필요).");
        }

        foreach (GatherableResource resource in resources)
        {
            WorldObjectIdentity identity = resource.GetComponent<WorldObjectIdentity>();
            Vector3 position = resource.transform.position;

            if (identity == null || !identity.HasValidId || !identity.WorldObjectId.StartsWith(ResourceIdPrefix, StringComparison.Ordinal) || !ids.Add(identity.WorldObjectId))
            {
                Error($"{resource.name}의 저장 ID가 비었거나 겹칩니다.");
            }

            if (Mathf.Abs(position.y - (terrain.SampleHeight(position) + origin.y)) > 0.3f)
            {
                Error($"{resource.name}이(가) 땅 위에 있지 않습니다.");
            }
        }

        // 들판의 적
        EnemySpawnPoint[] spawns = Object.FindObjectsByType<EnemySpawnPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(spawn => spawn.SpawnPointId.StartsWith(FieldSpawnPrefix, StringComparison.Ordinal)).ToArray();
        (Vector3 start, _, _) = IslandTerrainBuilder.StartPoints();

        if (spawns.Length < 8)
        {
            Error($"들판 적 생성 지점이 {spawns.Length}곳입니다 (8곳 이상 필요).");
        }

        foreach (EnemySpawnPoint spawn in spawns)
        {
            Vector3 position = spawn.transform.position;

            if (!NavMesh.SamplePosition(position, out NavMeshHit _, 3f, NavMesh.AllAreas))
            {
                Error($"{spawn.SpawnPointId} 자리가 NavMesh 밖입니다 {position}.");
            }

            if (Flat(position, Vector3.zero) < 220f || Flat(position, start) < 170f)
            {
                Error($"{spawn.SpawnPointId}가 마을이나 시작 해변에 너무 가깝습니다.");
            }
        }

        if (spawns.Select(spawn => spawn.SpawnPointId).Distinct().Count() != spawns.Length)
        {
            Error("들판 적 생성 지점 ID가 겹칩니다.");
        }

        // 지도
        MinimapCameraController map = Object.FindFirstObjectByType<MinimapCameraController>(FindObjectsInactive.Include);

        if (map == null || map.FullMapMaximumSize < 950f || new SerializedObject(map).FindProperty("minimumCameraHeight").floatValue < 200f)
        {
            Error("전체 지도가 섬 전체를 볼 수 없습니다 (가장 넓은 범위 · 지도 카메라 높이). 24번 메뉴를 실행하세요.");
        }

        GameObject zones = GameObject.Find(WorldZoneBuilder.RootName);
        Transform labels = zones != null ? zones.transform.Find("MapLabels") : null;

        if (labels == null || labels.GetComponent<MapLabelScaler>() == null)
        {
            Error("지도 이름 크기 조절(MapLabelScaler)이 없습니다. 18번(22번) 메뉴를 실행하세요.");
        }

        int[] counts = new int[Prototypes.Length];

        foreach (TreeInstance instance in instances)
        {
            if (instance.prototypeIndex >= 0 && instance.prototypeIndex < counts.Length)
            {
                counts[instance.prototypeIndex]++;
            }
        }

        report.AppendLine($"섬 자연 {instances.Length}개 (" + string.Join(" · ", Prototypes.Select((prototype, index) => $"{prototype.model} {counts[index]}").Where((_, index) => counts[index] > 0)) + ")");
        report.AppendLine($"흙길 옆 채집 자원 {resources.Length}개 · 들판 적 생성 지점 {spawns.Length}곳 · 지도 가장 넓게 {(map != null ? map.FullMapMaximumSize * 2f : 0f):0}m");
        errorCount = errors;
        report.AppendLine(errors == 0 ? "결과 : 오류 0개" : $"결과 : 오류 {errors}개");
        return report.ToString();
    }
}
