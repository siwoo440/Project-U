using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// 100일차: 확장 준비 - 특수 체형 NPC 모델 · 새 구역
// 1. NPC 데이터 갱신(새 위치) → NPC 35명 모델 · 새 구역 소품 모델 생성
// 2. 게임 Scene 섬 바깥쪽에 6개 구역 (해안 · 고대 폐허 · 깊은 숲 · 설산 기슭 · 붉은 사막 · 안개 습지) 과 마을에서 이어지는 흙길
// 3. 바닥 칠하기 (모래 · 눈 · 진흙 · 돌바닥) · 풀과 기존 나무 정리 · 소품 · 충돌체
// 4. NPC 일정 위치 등록 · 섬 경계 벽과 바다 · 지도 구역 이름 · 적 생성 지점 2곳 · NavMesh 다시 굽기
// 여러 번 실행해도 같은 결과가 된다 (구역 오브젝트는 지우고 다시 만든다). 게임 Scene은 도구가 저장한다.
// 107일차: 무인도에서는 구역을 예전 좌표(설계 좌표)로 만든 뒤 IslandZoneLayout 자리로 통째로 옮기고, 땅 높이에 맞춘다.
//          흙길은 마을에서 새 구역 입구까지, 석호에는 작은 선착장을 남긴다.
public static class WorldZoneBuilder
{
    public const string RootName = "=== World Zones ===";
    public const string MapLabelLayerName = "MapLabel";
    private const string ScenePath = "Assets/_ProjectU/Scenes/20_Gameplay.unity";
    private const string DialogTitle = "Project U 새 구역 · 특수 체형";
    private const string BuildingLayerName = "Building";
    private const string WaterLayerName = "Water";
    private const string EnvironmentRootName = "=== Stylized Environment ===";
    private const string SpawnRootName = "=== Enemy Spawn Points ===";
    private const string LocationsName = "Locations";
    private const int Seed = 10023;
    private const float TerrainLimit = 121f; // 소품을 둘 수 있는 섬 안쪽 한계
    private const float SeaLine = 100f; // 이 x보다 동쪽은 바다
    private const float ShrineHouseYaw = 270f; // 101일차: 카스미의 사당 집 (문이 사당 쪽 서쪽)
    private static readonly Vector3 ShrineHousePosition = new Vector3(50f, 0f, 104f); // 아래 위치 표보다 먼저 초기화되어야 한다
    // 103일차: 3차 NPC 집 (입구 +Z를 바라보는 방향)
    private static readonly Vector3 CoralGrottoPosition = new Vector3(98.2f, 0f, -15.5f); // 마리엘 (등대 아래 바닷가)
    private const float CoralGrottoYaw = 270f;
    private static readonly Vector3 CocoonHousePosition = new Vector3(-71.5f, 0f, -46.5f); // 루미나 (빛나는 꽃밭 동쪽)
    private const float CocoonHouseYaw = 291f;
    private static readonly Vector3 GreenhousePosition = new Vector3(-91f, 0f, -47f); // 알리우네 (깊은 숲 남쪽)
    private const float GreenhouseYaw = 11f;
    // 110일차: 4차 NPC 집 (입구 +Z를 바라보는 방향)
    private static readonly Vector3 RepairPodPosition = new Vector3(-98f, 0f, 98f); // 리제 (폐허 북서쪽 모퉁이)
    private const float RepairPodYaw = 135f;
    private static readonly Vector3 LifeguardTowerPosition = new Vector3(96.8f, 0f, 44f); // 샤리아 (해안 북쪽 물가, 바다 쪽 +X)
    private const float LifeguardTowerYaw = 0f;
    private static readonly Vector3 CaravanStablePosition = new Vector3(104f, 0f, -72f); // 켄시아 (사막 북동쪽)
    private const float CaravanStableYaw = 250f;
    private static readonly Vector3 HunterHutPosition = new Vector3(-86f, 0f, -86f); // 하르카 (습지 서쪽 기둥 오두막)
    private const float HunterHutYaw = 107f;
    private static readonly Vector3 WitchHutPosition = new Vector3(-100f, 0f, -18f); // 노크티아 (숲 까마귀 오두막, 100일차)
    private const float WitchHutYaw = 135f;
    private static readonly Vector3 CampTentPosition = new Vector3(14f, 0f, 103.5f); // 라그 (설원 야영지 천막, 100일차)
    private const float CampTentYaw = 180f;
    private static readonly Vector3 LighthousePosition = new Vector3(95.5f, 0f, -24f); // 에이리 (해안 등대, 100일차)
    private const float LighthouseYaw = 30f;
    // 112일차: 5차 NPC 집
    private static readonly Vector3 GothicManorPosition = new Vector3(-49f, 0f, -118f); // 릴리카 (습지 남쪽 끝 안개 속)
    private const float GothicManorYaw = 339f;
    private static readonly Vector3 BeehiveHutPosition = new Vector3(106f, 0f, -96f); // 베아트리체 (오아시스 동쪽)
    private const float BeehiveHutYaw = 276f;
    private static readonly Vector3 TrainingYardPosition = new Vector3(26f, 0f, 116f); // 란후아 (설원 야영지 북쪽)
    private const float TrainingYardYaw = 180f;
    private static readonly Vector3 AkaneShrineHousePosition = new Vector3(58f, 0f, 94f); // 아카네 (카스미 사당 집 남쪽, 같은 모양)
    private const float AkaneShrineHouseYaw = 270f;
    private static readonly Vector3 GateWatchpostPosition = new Vector3(-103f, 0f, 84f); // 레이븐나 (폐허 서쪽 무너진 성문)
    private const float GateWatchpostYaw = 90f;
    private static readonly Vector3 GreatStumpPosition = new Vector3(-97f, 0f, -38f); // 에리나 (깊은 숲 큰 그루터기, 100일차)
    private const float GreatStumpYaw = 30f;
    private static readonly Vector3 TidePoolHomePosition = new Vector3(96.9f, 0f, -1.5f); // 옥타비아 (갯바위 북쪽 물가, 바다에서 3.1m)

    public static readonly string[] ZoneLayerNames = { "ZoneSand", "ZoneSnow", "ZoneMud", "ZoneFlagstone" };

    public sealed class ZoneInfo
    {
        public string Id; // 위치 CSV의 Zone 값
        public string ObjectName; // Scene 오브젝트 이름
        public string DisplayName; // 지도 · 표지판 이름
        public Vector3 Center; // 구역 가운데
        public Vector3 PathStart; // 마을 쪽 흙길 시작
        public Vector3 Entrance; // 구역 입구 (흙길 끝)
    }

    public static readonly ZoneInfo[] Zones =
    {
        new ZoneInfo { Id = "coast", ObjectName = "Coast", DisplayName = "해안 · 부두", Center = new Vector3(94f, 0f, 4f), PathStart = new Vector3(38f, 0f, 10f), Entrance = new Vector3(86f, 0f, 10f) },
        new ZoneInfo { Id = "ruins", ObjectName = "Ruins", DisplayName = "고대 폐허", Center = new Vector3(-78f, 0f, 78f), PathStart = new Vector3(-22f, 0f, 36f), Entrance = new Vector3(-66f, 0f, 66f) },
        new ZoneInfo { Id = "forest", ObjectName = "DeepForest", DisplayName = "깊은 숲", Center = new Vector3(-88f, 0f, -32f), PathStart = new Vector3(-32f, 0f, -22f), Entrance = new Vector3(-74f, 0f, -30f) },
        new ZoneInfo { Id = "snow", ObjectName = "SnowFoothills", DisplayName = "설산 기슭 · 사당", Center = new Vector3(26f, 0f, 100f), PathStart = new Vector3(4f, 0f, 38f), Entrance = new Vector3(12f, 0f, 86f) },
        new ZoneInfo { Id = "desert", ObjectName = "Desert", DisplayName = "붉은 사막", Center = new Vector3(78f, 0f, -86f), PathStart = new Vector3(32f, 0f, -14f), Entrance = new Vector3(64f, 0f, -73f) },
        new ZoneInfo { Id = "swamp", ObjectName = "Swamp", DisplayName = "안개 습지", Center = new Vector3(-60f, 0f, -94f), PathStart = new Vector3(-18f, 0f, -32f), Entrance = new Vector3(-50f, 0f, -84f) }
    };

    // NPC 일정 위치 (서는 곳 · 바라보는 곳)
    private static readonly Dictionary<string, (Vector3 position, Vector3 lookAt)> PointLayout = new Dictionary<string, (Vector3, Vector3)>
    {
        { "loc_coast_dock", (new Vector3(96.9f, 0f, 10f), new Vector3(110f, 0f, 10f)) },
        { "loc_coast_beach", (new Vector3(91f, 0f, 13.5f), new Vector3(100f, 0f, 13f)) },
        { "loc_coast_rocks", (new Vector3(97.1f, 0f, -7.3f), new Vector3(100f, 0f, -8f)) },
        { "loc_ruins_gate", (new Vector3(-72f, 0f, 91.6f), new Vector3(-72f, 0f, 97f)) },
        { "loc_ruins_library", (new Vector3(-92f, 0f, 86.6f), new Vector3(-92f, 0f, 90f)) },
        { "loc_ruins_plaza", (new Vector3(-75.2f, 0f, 75.2f), new Vector3(-78f, 0f, 78f)) },
        { "loc_forest_fairy_ring", (new Vector3(-86f, 0f, -28f), new Vector3(-88f, 0f, -33f)) },
        { "loc_forest_flower_garden", (new Vector3(-78f, 0f, -40.8f), new Vector3(-78f, 0f, -44f)) },
        { "loc_forest_witch_hut", (Local(new Vector3(-100f, 0f, -18f), 135f, new Vector3(0.5f, 0f, 3.6f)), new Vector3(-100f, 0f, -18f)) },
        { "loc_snow_camp", (new Vector3(14f, 0f, 93.2f), new Vector3(14f, 0f, 96f)) },
        { "loc_snow_shrine", (new Vector3(42f, 0f, 102.4f), new Vector3(42f, 0f, 106f)) },
        { "loc_desert_oasis", (new Vector3(80.2f, 0f, -91.4f), new Vector3(86f, 0f, -94f)) },
        { "loc_desert_camp", (Local(new Vector3(68f, 0f, -79f), 315f, new Vector3(0f, 0f, 3.4f)), new Vector3(68f, 0f, -79f)) },
        { "loc_swamp_boardwalk", (new Vector3(-62f, 0f, -98f), new Vector3(-62f, 0f, -103f)) },
        { "loc_swamp_hut", (Local(new Vector3(-72f, 0f, -106f), 45f, new Vector3(0f, 0f, 3.8f)), new Vector3(-72f, 0f, -106f)) },
        // 101일차: 2차 NPC 집 (밤에는 안으로 들어감)
        { "loc_home_seira", (new Vector3(-73.7f, 0f, 93.3f), new Vector3(-73f, 0f, 97f)) },
        { "loc_home_arachne", (new Vector3(-70.3f, 0f, 93.3f), new Vector3(-71f, 0f, 97f)) },
        { "loc_home_milu", (new Vector3(-64.6f, 0f, -86f), new Vector3(-68f, 0f, -86f)) },
        { "loc_home_kasumi", (Local(ShrineHousePosition, ShrineHouseYaw, new Vector3(0f, 0f, 2.9f)), ShrineHousePosition) },
        { "loc_home_safira", (Local(new Vector3(68f, 0f, -79f), 315f, new Vector3(0f, 0f, 0.3f)), Local(new Vector3(68f, 0f, -79f), 315f, new Vector3(0f, 0f, 3f))) },
        // 103일차: 3차 NPC 집
        { "loc_home_serena", (new Vector3(-88.4f, 0f, 87.6f), new Vector3(-90f, 0f, 89.2f)) }, // 무너진 도서관 책장 끝 (촛불 자리)
        { "loc_home_chesca", (new Vector3(-79.4f, 0f, 95.3f), new Vector3(-80f, 0f, 97.5f)) }, // 유적 보물 더미 앞
        { "loc_home_liriel", (new Vector3(-81f, 0f, -21.9f), new Vector3(-81f, 0f, -24f)) }, // 큰 버섯 아래
        { "loc_home_lumina", (Local(CocoonHousePosition, CocoonHouseYaw, new Vector3(0f, 0f, 1.9f)), CocoonHousePosition) },
        { "loc_home_aliune", (Local(GreenhousePosition, GreenhouseYaw, new Vector3(0f, 0f, 2.6f)), GreenhousePosition) },
        { "loc_home_marielle", (Local(CoralGrottoPosition, CoralGrottoYaw, new Vector3(0f, 0f, 1.3f)), CoralGrottoPosition) },
        { "loc_home_neri", (Local(new Vector3(-72f, 0f, -106f), 45f, new Vector3(1.2f, 0f, 2.4f)), new Vector3(-72f, 0f, -106f)) }, // 기둥 위 관측 오두막
        // 110일차: 4차 NPC 집
        { "loc_home_noctia", (Local(WitchHutPosition, WitchHutYaw, new Vector3(-0.2f, 0f, 2.5f)), WitchHutPosition) }, // 까마귀 오두막 문 앞
        { "loc_home_ragh", (Local(CampTentPosition, CampTentYaw, new Vector3(0f, 0f, 2.2f)), CampTentPosition) }, // 모닥불 뒤 털가죽 천막
        { "loc_home_rize", (Local(RepairPodPosition, RepairPodYaw, new Vector3(0f, 0f, 1.6f)), RepairPodPosition) }, // 정비 캡슐 앞
        { "loc_home_sharia", (Local(LifeguardTowerPosition, LifeguardTowerYaw, new Vector3(0.9f, 0f, 2.8f)), LifeguardTowerPosition) }, // 망루 사다리 옆 (물가)
        { "loc_home_censia", (Local(CaravanStablePosition, CaravanStableYaw, new Vector3(0f, 0f, 3.0f)), CaravanStablePosition) }, // 마구간 앞
        { "loc_home_harka", (Local(HunterHutPosition, HunterHutYaw, new Vector3(1.2f, 0f, 2.6f)), HunterHutPosition) }, // 기둥 오두막 사다리 옆
        { "loc_home_eiri", (Local(LighthousePosition, LighthouseYaw, new Vector3(0f, 0f, 2.2f)), LighthousePosition) }, // 등대 문 앞
        // 112일차: 5차 NPC 집
        { "loc_home_erina", (Local(GreatStumpPosition, GreatStumpYaw, new Vector3(0f, 0f, 3.8f)), GreatStumpPosition) }, // 큰 그루터기 아래
        { "loc_home_lilica", (Local(GothicManorPosition, GothicManorYaw, new Vector3(0f, 0f, 3.0f)), GothicManorPosition) }, // 저택 울타리 문 앞
        { "loc_home_beatrice", (Local(BeehiveHutPosition, BeehiveHutYaw, new Vector3(-0.3f, 0f, 2.8f)), BeehiveHutPosition) }, // 벌통 오두막 문 앞
        { "loc_home_lanhua", (Local(TrainingYardPosition, TrainingYardYaw, new Vector3(0f, 0f, 2.9f)), TrainingYardPosition) }, // 수련장 계단 앞
        { "loc_home_akane", (Local(AkaneShrineHousePosition, AkaneShrineHouseYaw, new Vector3(0f, 0f, 2.9f)), AkaneShrineHousePosition) }, // 사당 집 툇마루 앞
        { "loc_home_ravenna", (Local(GateWatchpostPosition, GateWatchpostYaw, new Vector3(-1.9f, 0f, 2.3f)), GateWatchpostPosition) }, // 성문 초소 차양 앞
        { "loc_home_octavia", (TidePoolHomePosition, TidePoolHomePosition + new Vector3(12f, 0f, 0f)) } // 갯바위 물가 (바다를 봄)
    };

    // 물 (물가 검사 · 지도) : 가운데 · 반지름
    private static readonly (string name, Vector3 center, float radius)[] Pools =
    {
        ("Oasis", new Vector3(86f, 0f, -94f), StylizedModelLibrary.ZoneOasisRadius),
        ("SwampPool_A", new Vector3(-62f, 0f, -98f), StylizedModelLibrary.ZoneSwampPoolRadius),
        ("SwampPool_B", new Vector3(-49f, 0f, -104f), StylizedModelLibrary.ZoneSwampPoolRadius * 0.7f),
        ("SwampPool_C", new Vector3(-68f, 0f, -86f), StylizedModelLibrary.ZoneSwampPoolRadius * 0.6f)
    };

    private static readonly (string id, string template, string zoneId, Vector3 position)[] ZoneSpawns =
    {
        ("spawn_zone_ruins_grunt", "Spawn_MeleeGrunt_01", "ruins", new Vector3(-60f, 0f, 82f)),
        ("spawn_zone_swamp_spitter", "Spawn_RangedSpitter_01", "swamp", new Vector3(-46f, 0f, -104f))
    };

    private static bool islandLayout; // 107일차: 무인도 자리로 옮기는 중인지 (BuildAll · Validate 시작에서 정함)

    private static Terrain ActiveTerrain => Terrain.activeTerrain != null ? Terrain.activeTerrain : Object.FindFirstObjectByType<Terrain>();

    private static bool DetectIsland()
    {
        islandLayout = IslandTerrainBuilder.IsIslandTerrain(ActiveTerrain);
        return islandLayout;
    }

    private static float GroundY(Vector3 position) // Terrain 높이 (무인도가 아니면 0)
    {
        Terrain terrain = ActiveTerrain;
        return islandLayout && terrain != null ? terrain.SampleHeight(position) + terrain.transform.position.y : 0f;
    }

    public static Vector3 ZoneToWorld(string zoneId, Vector3 design) // 107일차: 설계 좌표 → 섬 좌표 (땅 높이)
    {
        if (!islandLayout)
        {
            return design;
        }

        Vector3 world = IslandZoneLayout.ToWorld(zoneId, design);
        world.y = GroundY(world) + design.y;
        return world;
    }

    public static Vector3 ZoneCenterWorld(ZoneInfo zone) => ZoneToWorld(zone.Id, zone.Center);

    public static Vector3 ZoneEntranceWorld(ZoneInfo zone) => ZoneToWorld(zone.Id, zone.Entrance);

    private enum PropCollider
    {
        None,
        Bounds,
        Trunk
    }

    // ---------------------------------------------------------------- 전체 생성

    public static string BuildAll(bool saveScene)
    {
        StringBuilder report = new StringBuilder("[새 구역 · 특수 체형]\n");

        try
        {
            EditorUtility.DisplayProgressBar(DialogTitle, "NPC 데이터 (새 위치)", 0.05f);
            string dataReport = NpcContentBuilder.BuildAll();
            report.AppendLine($"NPC 데이터 갱신 ({(dataReport.Split('\n').LastOrDefault(line => line.StartsWith("결과")) ?? "결과 없음").Trim()})");

            EditorUtility.DisplayProgressBar(DialogTitle, "NPC 모델", 0.15f);
            int npcModels = StylizedModelLibrary.NpcModelIds.Count(id => StylizedArtAssetFactory.GetOrCreateModelPrefab(id, true) != null);
            EditorUtility.DisplayProgressBar(DialogTitle, "구역 소품 모델", 0.25f);
            int zoneModels = StylizedModelLibrary.Catalog.Keys.Where(id => id.StartsWith("zone_", StringComparison.Ordinal)).Count(id => StylizedArtAssetFactory.GetOrCreateModelPrefab(id, true) != null);
            AssetDatabase.SaveAssets();
            report.AppendLine($"저폴리 모델 : NPC {npcModels}명 · 구역 소품 {zoneModels}종");

            Scene scene = EditorSceneManager.GetActiveScene();

            if (scene.path != ScenePath)
            {
                report.AppendLine($"✗ 게임 Scene({ScenePath})을 열고 다시 실행하세요. 모델만 만들었습니다.");
                return report.ToString();
            }

            EditorUtility.DisplayProgressBar(DialogTitle, "구역 정리", 0.3f);
            GameObject root = scene.GetRootGameObjects().FirstOrDefault(item => item.name == RootName) ?? new GameObject(RootName);
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            foreach (Transform child in root.transform.Cast<Transform>().ToList())
            {
                Object.DestroyImmediate(child.gameObject);
            }

            DetectIsland();
            EditorUtility.DisplayProgressBar(DialogTitle, "바닥 칠하기", 0.35f);
            report.AppendLine(PaintTerrain());
            report.AppendLine(HideEnvironment(scene));

            EditorUtility.DisplayProgressBar(DialogTitle, "구역 소품", 0.5f);
            int building = LayerMask.NameToLayer(BuildingLayerName);
            int props = 0;
            props += BuildCoast(Group(root.transform, "Coast"), building);
            props += BuildRuins(Group(root.transform, "Ruins"), building);
            props += BuildForest(Group(root.transform, "DeepForest"), building);
            props += BuildSnow(Group(root.transform, "SnowFoothills"), building);
            props += BuildDesert(Group(root.transform, "Desert"), building);
            props += BuildSwamp(Group(root.transform, "Swamp"), building);
            props += BuildSignposts(Group(root.transform, "Signposts"));
            report.AppendLine($"구역 6곳 · 소품 {props}개 · 흙길 {Zones.Length}개 · 표지판 {Zones.Length}개");

            if (islandLayout) // 107일차: 섬 곳곳으로 옮기기 · 석호 선착장
            {
                report.AppendLine(MoveZonesToIsland(root.transform));
                report.AppendLine($"둘레길 · 갈림길 표지판 {BuildRoadSigns(Group(root.transform, "Signposts"))}개 (108일차 길망)");
                report.AppendLine(BuildLagoonPier(Group(root.transform, "LagoonPier"), building));
            }

            report.AppendLine(BuildBoundary(Group(root.transform, "Boundary")));
            report.AppendLine(BuildMapLabels(scene, Group(root.transform, "MapLabels")));
            report.AppendLine(BuildLocations(scene, Group(root.transform, LocationsName)));
            report.AppendLine(BuildEnemySpawns(scene));

            EditorUtility.DisplayProgressBar(DialogTitle, "NavMesh 다시 굽기", 0.8f);
            report.AppendLine(RebakeNavMesh());
            EditorSceneManager.MarkSceneDirty(scene);

            if (saveScene)
            {
                EditorSceneManager.SaveScene(scene);
                report.AppendLine("게임 Scene 저장 완료");
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        report.Append(Validate(out _));
        return report.ToString();
    }

    private static Transform Group(Transform parent, string name)
    {
        Transform child = parent.Find(name);

        if (child == null)
        {
            child = new GameObject(name).transform;
            child.SetParent(parent, false);
        }

        return child;
    }

    // 로컬 좌표 → 월드 (건물 기준 위치 계산)
    private static Vector3 Local(Vector3 origin, float yaw, Vector3 local)
    {
        return origin + Quaternion.Euler(0f, yaw, 0f) * local;
    }

    // 101일차: 새 구역 위치만 지금 위치 목록(CSV)대로 다시 만든다 (NPC 마을 배치 도구에서 사용). 새 구역이 없으면 빈 문자열
    public static string RefreshLocations(Scene scene)
    {
        GameObject root = scene.GetRootGameObjects().FirstOrDefault(item => item.name == RootName);

        if (root == null)
        {
            return string.Empty;
        }

        Transform group = Group(root.transform, LocationsName);

        foreach (Transform child in group.Cast<Transform>().ToList())
        {
            Object.DestroyImmediate(child.gameObject);
        }

        return BuildLocations(scene, group);
    }

    public static NpcLocationPoint FindZonePoint(Scene scene, string locationId) // NPC 마을 배치 도구가 새 구역 위치를 찾을 때 사용
    {
        GameObject root = scene.GetRootGameObjects().FirstOrDefault(item => item.name == RootName);

        if (root == null)
        {
            return null;
        }

        return root.GetComponentsInChildren<NpcLocationPoint>(true).FirstOrDefault(point => point.LocationId == locationId);
    }

    // ---------------------------------------------------------------- 배치 도우미

    private static GameObject Place(Transform parent, string modelId, Vector3 position, float yaw, float scale, PropCollider collider, int layer)
    {
        return Place(parent, modelId, position, yaw, Vector3.one * scale, collider, layer);
    }

    private static GameObject Place(Transform parent, string modelId, Vector3 position, float yaw, Vector3 scale, PropCollider collider, int layer)
    {
        GameObject prefab = StylizedArtAssetFactory.LoadModelPrefab(modelId) ?? StylizedArtAssetFactory.GetOrCreateModelPrefab(modelId, false);

        if (prefab == null)
        {
            return null;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        instance.transform.localScale = scale;
        instance.layer = layer;
        GameObjectUtility.SetStaticEditorFlags(instance, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
        Bounds mesh = instance.GetComponent<MeshFilter>().sharedMesh.bounds;

        if (collider == PropCollider.Bounds)
        {
            BoxCollider box = instance.AddComponent<BoxCollider>();
            box.center = mesh.center;
            box.size = Vector3.Scale(mesh.size, new Vector3(0.85f, 0.95f, 0.85f));
        }
        else if (collider == PropCollider.Trunk)
        {
            CapsuleCollider capsule = instance.AddComponent<CapsuleCollider>();
            capsule.radius = 0.32f;
            capsule.height = Mathf.Min(3f, mesh.size.y);
            capsule.center = new Vector3(0f, capsule.height * 0.5f, 0f);
        }

        return instance;
    }

    private static void AddBox(GameObject target, Vector3 center, Vector3 size, Vector3 euler = default)
    {
        if (euler == default)
        {
            BoxCollider box = target.AddComponent<BoxCollider>();
            box.center = center;
            box.size = size;
            return;
        }

        GameObject child = new GameObject("Collider");
        child.layer = target.layer;
        child.transform.SetParent(target.transform, false);
        child.transform.localPosition = center;
        child.transform.localRotation = Quaternion.Euler(euler);
        child.AddComponent<BoxCollider>().size = size;
    }

    private static void AddCapsule(GameObject target, Vector3 center, float radius, float height)
    {
        CapsuleCollider capsule = target.AddComponent<CapsuleCollider>();
        capsule.center = center;
        capsule.radius = radius;
        capsule.height = height;
    }

    private static float Rand(System.Random random, float min, float max)
    {
        return min + (float)random.NextDouble() * (max - min);
    }

    private static bool InsideIsland(Vector3 position, float margin = 0f)
    {
        return Mathf.Abs(position.x) < TerrainLimit - margin && Mathf.Abs(position.z) < TerrainLimit - margin && position.x < SeaLine - 3f;
    }

    private static float PathDistance(Vector3 position)
    {
        float best = float.MaxValue;

        foreach (ZoneInfo zone in Zones)
        {
            best = Mathf.Min(best, StylizedTerrainPainter.DistanceToSegmentXZ(position, zone.PathStart, zone.Entrance));
            best = Mathf.Min(best, StylizedTerrainPainter.DistanceToSegmentXZ(position, zone.Entrance, zone.Center));
        }

        return best;
    }

    // 무작위 흩뿌리기 (막힌 곳 · 길 · 서로 간격 피하기)
    private static int Scatter(System.Random random, Transform parent, string[] models, int count, Func<Vector3> sample, (Vector3 center, float radius)[] blocked,
        float spacing, float minScale, float maxScale, PropCollider collider, int layer, List<Vector3> placed)
    {
        int made = 0;

        for (int attempt = 0; attempt < count * 30 && made < count; attempt++)
        {
            Vector3 position = sample();

            if (!InsideIsland(position, 1f) || PathDistance(position) < 3f || blocked.Any(block => Flat(position, block.center) < block.radius) || placed.Any(other => Flat(other, position) < spacing))
            {
                continue;
            }

            string model = models[random.Next(models.Length)];

            if (Place(parent, model, position, Rand(random, 0f, 360f), Rand(random, minScale, maxScale), collider, layer) != null)
            {
                placed.Add(position);
                made++;
            }
        }

        return made;
    }

    private static float Flat(Vector3 a, Vector3 b)
    {
        return new Vector2(a.x - b.x, a.z - b.z).magnitude;
    }

    private static Vector3 InEllipse(System.Random random, Vector3 center, float radiusX, float radiusZ)
    {
        float angle = Rand(random, 0f, Mathf.PI * 2f);
        float distance = Mathf.Sqrt((float)random.NextDouble());
        return center + new Vector3(Mathf.Cos(angle) * radiusX * distance, 0f, Mathf.Sin(angle) * radiusZ * distance);
    }

    // ---------------------------------------------------------------- 구역별 소품

    private static int BuildCoast(Transform parent, int building)
    {
        int water = LayerMask.NameToLayer(WaterLayerName);
        int count = 0;
        bool island = IslandTerrainBuilder.IsIslandTerrain(Terrain.activeTerrain != null ? Terrain.activeTerrain : Object.FindFirstObjectByType<Terrain>());
        float boatHeight = island ? IslandTerrainBuilder.SeaLevel + 0.02f : 0.02f; // 105일차: 무인도는 석호 수면에 뜸

        if (island) // 105일차: 사방 바다는 무인도 도구가 만들고, 부두는 석호에 둔다
        {
            count += BuildDockAndHarbor(parent, building, boatHeight);
            return count;
        }

        // 바다 · 바다 밑 · 물가 거품 (섬 밖까지 넓게, 그림자 없음)
        GameObject sea = Place(parent, "zone_sea", new Vector3(280f, 0.08f, 0f), 0f, new Vector3(360f, 1f, 700f), PropCollider.None, water);
        GameObject seabed = Place(parent, "zone_seabed", new Vector3(280f, -1.5f, 0f), 0f, new Vector3(360f, 1f, 700f), PropCollider.None, water);

        foreach (GameObject flat in new[] { sea, seabed })
        {
            MeshRenderer renderer = flat.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            GameObjectUtility.SetStaticEditorFlags(flat, 0);
        }

        sea.name = "Sea";
        seabed.name = "Seabed";

        for (int index = 0; index < 13; index++)
        {
            Place(parent, "zone_shore_foam", new Vector3(SeaLine + 0.1f, 0.09f, -120f + index * 20f), 0f, 1f, PropCollider.None, water);
        }

        count += 15;
        count += BuildDockAndHarbor(parent, building, boatHeight);
        return count;
    }

    private static int BuildDockAndHarbor(Transform parent, int building, float boatHeight) // 부두 · 배 · 어부 오두막 · 등대 · 소품 · 바위
    {
        int count = 0;

        // 부두 (동쪽으로 뻗음) : 널빤지 · 경사로 · 난간 벽
        GameObject dock = Place(parent, "zone_dock", new Vector3(SeaLine - 3f, 0f, 10f), 90f, 1f, PropCollider.None, building);
        float half = StylizedModelLibrary.ZoneDockWidth * 0.5f;
        float length = StylizedModelLibrary.ZoneDockLength;
        AddBox(dock, new Vector3(0f, StylizedModelLibrary.ZoneDockDeckHeight * 0.5f, 1.2f + (length - 1.2f) * 0.5f), new Vector3(StylizedModelLibrary.ZoneDockWidth, StylizedModelLibrary.ZoneDockDeckHeight, length - 1.2f));
        AddBox(dock, new Vector3(0f, 0.12f, 0.6f), new Vector3(StylizedModelLibrary.ZoneDockWidth - 0.4f, 0.06f, 1.3f), new Vector3(-14f, 0f, 0f));

        for (int side = -1; side <= 1; side += 2)
        {
            AddBox(dock, new Vector3(side * (half + 0.1f), 0.9f, 3.1f + (length - 3.1f) * 0.5f), new Vector3(0.2f, 1.8f, length - 3.1f));
        }

        AddBox(dock, new Vector3(0f, 0.9f, length + 0.1f), new Vector3(StylizedModelLibrary.ZoneDockWidth + 0.4f, 1.8f, 0.2f));
        dock.name = "Dock";
        count++;

        Place(parent, "zone_boat", new Vector3(104.5f, boatHeight, 4f), 20f, 1f, PropCollider.None, 0).name = "Boat_A";
        Place(parent, "zone_boat", new Vector3(106.5f, boatHeight, 17f), -35f, 1f, PropCollider.None, 0).name = "Boat_B";
        GameObject hut = Place(parent, "zone_fisher_hut", new Vector3(90f, 0f, 20f), 270f, 1f, PropCollider.None, building);
        AddBox(hut, new Vector3(0f, 1.3f, 0f), new Vector3(4.2f, 2.6f, 4.2f));
        hut.name = "FisherHut";
        Place(parent, "zone_net_rack", new Vector3(92.5f, 0f, 3f), 0f, 1f, PropCollider.Bounds, building);
        GameObject lighthouse = Place(parent, "zone_lighthouse", LighthousePosition, LighthouseYaw, 1f, PropCollider.None, building); // 110일차: 에이리의 집
        AddCapsule(lighthouse, new Vector3(0f, 4f, 0f), 1.4f, 8f);
        lighthouse.name = "Lighthouse";
        Place(parent, "zone_tide_rocks", new Vector3(99.9f, 0f, -8f), 180f, 1f, PropCollider.Bounds, building);
        GameObject grotto = Place(parent, "zone_coral_grotto", CoralGrottoPosition, CoralGrottoYaw, 1f, PropCollider.None, building); // 103일차: 마리엘의 집
        AddBox(grotto, new Vector3(0f, 1.1f, -0.95f), new Vector3(3.6f, 2.2f, 0.9f));
        AddBox(grotto, new Vector3(-1.5f, 0.9f, 0.05f), new Vector3(0.9f, 1.8f, 1.6f));
        AddBox(grotto, new Vector3(1.5f, 0.9f, 0.05f), new Vector3(0.9f, 1.8f, 1.6f));
        grotto.name = "CoralGrotto_Marielle";
        GameObject tower = Place(parent, "zone_lifeguard_tower", LifeguardTowerPosition, LifeguardTowerYaw, 1f, PropCollider.None, building); // 110일차: 샤리아의 집
        AddBox(tower, new Vector3(0f, 2.1f, 0f), new Vector3(2.7f, 4.2f, 2.7f));
        tower.name = "LifeguardTower_Sharia";
        Place(parent, "prop_crate", new Vector3(95f, 0f, 7f), 15f, 1f, PropCollider.Bounds, building);
        Place(parent, "prop_barrel", new Vector3(95.6f, 0f, 6.2f), 0f, 1f, PropCollider.Bounds, building);
        Place(parent, "prop_barrel", new Vector3(95.4f, 0f, 13.3f), 0f, 1f, PropCollider.Bounds, building);
        Place(parent, "prop_lantern_post", new Vector3(95.5f, 0f, 11.9f), 90f, 1f, PropCollider.Bounds, building);
        count += 11;

        System.Random random = new System.Random(Seed + 1);
        List<Vector3> placed = new List<Vector3> { new Vector3(90f, 0f, 20f), new Vector3(95.5f, 0f, -24f) };
        count += Scatter(random, parent, new[] { "rock_large", "rock_small", "pebbles" }, 10, () => new Vector3(Rand(random, 88f, 98.5f), 0f, Rand(random, -60f, 60f)),
            new[] { (new Vector3(95f, 0f, 10f), 6f), (new Vector3(91f, 0f, 16f), 5f), (new Vector3(97f, 0f, -8f), 4f), (CoralGrottoPosition, 4.5f), (LifeguardTowerPosition, 5f), (LighthousePosition + new Vector3(1.1f, 0f, 1.9f), 3f), (TidePoolHomePosition, 3f) }, 4f, 0.8f, 1.6f, PropCollider.None, 0, placed);
        return count;
    }

    private static int BuildRuins(Transform parent, int building)
    {
        int count = 0;
        Vector3 center = new Vector3(-78f, 0f, 78f);
        Place(parent, "zone_ruin_floor", center, 45f, 1.4f, PropCollider.None, 0);
        GameObject statue = Place(parent, "zone_ruin_statue", center, 135f, 1f, PropCollider.Bounds, building);
        statue.name = "Statue";

        // 기둥 고리 (마을 쪽 남동 방향은 입구라 비움)
        for (int index = 0; index < 10; index++)
        {
            float angle = index * 36f + 18f;

            if (Mathf.Abs(Mathf.DeltaAngle(angle, 315f)) < 40f)
            {
                continue;
            }

            Vector3 position = center + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * 9.5f;
            bool broken = index % 3 == 1;
            GameObject pillar = Place(parent, broken ? "zone_ruin_pillar_broken" : "zone_ruin_pillar", position, angle, 1f, PropCollider.None, building);
            AddBox(pillar, new Vector3(0f, broken ? 1f : 2.1f, 0f), new Vector3(1.0f, broken ? 2f : 4.2f, 1.0f));
            count++;
        }

        GameObject arch = Place(parent, "zone_ruin_arch", center + new Vector3(8.8f, 0f, -8.8f), 135f, 1f, PropCollider.None, building);
        AddBox(arch, new Vector3(-2.05f, 1.7f, 0f), new Vector3(0.9f, 3.4f, 1.0f));
        AddBox(arch, new Vector3(2.05f, 1.7f, 0f), new Vector3(0.9f, 3.4f, 1.0f));
        AddBox(arch, new Vector3(0f, 4.9f, 0f), new Vector3(5f, 1.4f, 1.0f));
        arch.name = "EntranceArch";

        // 무너진 벽 · 도서관 (책장 3개) · 지하 입구 · 오벨리스크 · 보물 · 룬 결정
        Place(parent, "zone_ruin_wall", new Vector3(-92f, 0f, 90f), 0f, 1f, PropCollider.Bounds, building);
        Place(parent, "zone_ruin_wall", new Vector3(-95.6f, 0f, 86.2f), 90f, 1f, PropCollider.Bounds, building);
        Place(parent, "zone_ruin_wall", new Vector3(-96f, 0f, 72f), 80f, 1f, PropCollider.Bounds, building);
        Place(parent, "zone_ruin_wall", new Vector3(-84f, 0f, 95f), 200f, 1f, PropCollider.Bounds, building);

        for (int index = 0; index < 3; index++)
        {
            Place(parent, "zone_ruin_bookshelf", new Vector3(-94.1f + index * 2.05f, 0f, 89.2f), 0f, 1f, PropCollider.Bounds, building);
        }

        GameObject gate = Place(parent, "zone_ruin_gate", new Vector3(-72f, 0f, 97f), 180f, 1f, PropCollider.None, building);
        AddBox(gate, new Vector3(0f, 1.4f, -0.2f), new Vector3(5.6f, 2.8f, 4.4f));
        gate.name = "UndergroundGate";
        Place(parent, "zone_ruin_obelisk", new Vector3(-95f, 0f, 63f), 20f, 1f, PropCollider.Bounds, building).name = "Obelisk";
        Place(parent, "zone_ruin_treasure", new Vector3(-80f, 0f, 97.5f), 160f, 1f, PropCollider.None, 0).name = "Treasure";
        GameObject pod = Place(parent, "zone_repair_pod", RepairPodPosition, RepairPodYaw, 1f, PropCollider.None, building); // 110일차: 리제의 집
        AddBox(pod, new Vector3(0f, 0.9f, -0.2f), new Vector3(3.8f, 1.8f, 1.6f));
        AddBox(pod, new Vector3(1.95f, 0.45f, 0.85f), new Vector3(0.5f, 0.9f, 0.35f));
        pod.name = "RepairPod_Rize";
        GameObject watchpost = Place(parent, "zone_gate_watchpost", GateWatchpostPosition, GateWatchpostYaw, 1f, PropCollider.None, building); // 112일차: 레이븐나의 집
        AddBox(watchpost, new Vector3(-1.9f, 1.7f, -0.4f), new Vector3(1.3f, 3.4f, 1.3f));
        AddBox(watchpost, new Vector3(1.9f, 1.3f, -0.4f), new Vector3(1.3f, 2.6f, 1.3f));
        AddCapsule(watchpost, new Vector3(-2.6f, 1f, 1.4f), 0.1f, 2f);
        AddCapsule(watchpost, new Vector3(-1.2f, 1f, 1.4f), 0.1f, 2f);
        AddCapsule(watchpost, new Vector3(0.4f, 0.85f, 1.6f), 0.08f, 1.7f);
        AddBox(watchpost, new Vector3(2.4f, 0.5f, 1.3f), new Vector3(0.9f, 1.0f, 0.3f));
        watchpost.name = "GateWatchpost_Ravenna";

        foreach (Vector3 crystal in new[] { new Vector3(-86f, 0f, 70f), new Vector3(-67f, 0f, 90f), new Vector3(-90f, 0f, 94f), new Vector3(-70f, 0f, 82f) })
        {
            Place(parent, "zone_rune_crystals", crystal, crystal.x * 7f, 1f, PropCollider.Bounds, building);
        }

        count += 18;
        System.Random random = new System.Random(Seed + 2);
        List<Vector3> placed = new List<Vector3>();
        count += Scatter(random, parent, new[] { "rock_large", "rock_small", "bush", "fallen_log" }, 12, () => InEllipse(random, center, 20f, 20f),
            new[] { (center, 12f), (new Vector3(-92f, 0f, 88f), 5f), (new Vector3(-72f, 0f, 95f), 5f), (new Vector3(-95f, 0f, 63f), 3f), (RepairPodPosition, 5f), (GateWatchpostPosition, 5f) }, 3.5f, 0.8f, 1.5f, PropCollider.None, 0, placed);
        return count;
    }

    private static int BuildForest(Transform parent, int building)
    {
        int count = 0;
        Vector3 center = new Vector3(-88f, 0f, -32f);
        Vector3 ring = new Vector3(-86f, 0f, -28f);
        Vector3 garden = new Vector3(-78f, 0f, -44f);
        Vector3 hutPosition = WitchHutPosition; // 110일차: 노크티아의 집
        Vector3 stump = GreatStumpPosition; // 112일차: 에리나의 집

        Place(parent, "zone_fairy_ring", ring, 0f, 1f, PropCollider.None, 0).name = "FairyRing";
        Place(parent, "zone_glow_flowers", garden, 0f, 1.3f, PropCollider.None, 0).name = "GlowFlowerGarden";
        Place(parent, "zone_glow_flowers", garden + new Vector3(3.2f, 0f, -1.5f), 90f, 1f, PropCollider.None, 0);
        GameObject hut = Place(parent, "zone_witch_hut", hutPosition, WitchHutYaw, 1f, PropCollider.None, building);
        AddBox(hut, new Vector3(0f, 1.5f, 0f), new Vector3(3.9f, 3f, 3.7f));
        AddCapsule(hut, new Vector3(1.9f, 0.5f, 2.3f), 0.5f, 1f);
        hut.name = "WitchHut";
        GameObject cocoon = Place(parent, "zone_cocoon_house", CocoonHousePosition, CocoonHouseYaw, 1f, PropCollider.None, building); // 103일차: 루미나의 집
        AddCapsule(cocoon, new Vector3(0f, 1.75f, 0.1f), 0.9f, 2.4f);
        AddCapsule(cocoon, new Vector3(0f, 1.6f, -1.25f), 0.3f, 3.2f);
        cocoon.name = "CocoonHouse_Lumina";
        GameObject greenhouse = Place(parent, "zone_flower_greenhouse", GreenhousePosition, GreenhouseYaw, 1f, PropCollider.None, building); // 103일차: 알리우네의 집
        AddCapsule(greenhouse, new Vector3(0f, 1.3f, 0f), 1.95f, 2.6f);
        greenhouse.name = "Greenhouse_Aliune";
        GameObject great = Place(parent, "zone_great_stump", stump, GreatStumpYaw, 1f, PropCollider.None, building);
        AddCapsule(great, new Vector3(0f, 1.3f, 0f), 2.9f, 2.6f);
        great.name = "GreatStump";

        foreach ((Vector3 position, float scale) in new[] { (new Vector3(-81f, 0f, -24f), 1f), (new Vector3(-93f, 0f, -25f), 1.25f), (new Vector3(-82f, 0f, -37f), 0.8f), (new Vector3(-96f, 0f, -29f), 0.9f) })
        {
            GameObject mushroom = Place(parent, "zone_giant_mushroom", position, position.z * 11f, scale, PropCollider.None, building);
            AddCapsule(mushroom, new Vector3(0f, 1.3f, 0f), 0.45f, 2.6f);
        }

        foreach (Vector3 position in new[] { new Vector3(-75f, 0f, -41f), new Vector3(-81.5f, 0f, -46f), new Vector3(-97f, 0f, -21f), new Vector3(-92f, 0f, -35f), new Vector3(-84f, 0f, -31f), new Vector3(-100f, 0f, -34f) })
        {
            Place(parent, "zone_glow_mushrooms", position, position.x * 13f, 1f, PropCollider.None, 0);
        }

        count += 16;

        // 빈터를 둘러싼 큰 나무 숲
        System.Random random = new System.Random(Seed + 3);
        (Vector3, float)[] clearings = { (ring, 5f), (garden, 5.5f), (hutPosition, 7f), (stump, 6f), (center, 9f), (new Vector3(-81f, 0f, -23f), 3.5f), (new Vector3(-93f, 0f, -25f), 2.5f),
            (CocoonHousePosition, 5f), (GreenhousePosition, 5.5f) }; // 103일차: 3차 NPC 집 둘레 비움
        List<Vector3> placed = new List<Vector3>();
        count += Scatter(random, parent, new[] { "tree_round", "tree_round_b", "tree_pine", "tree_round" }, 46, () => InEllipse(random, center, 32f, 30f), clearings, 3.4f, 1.5f, 2.1f, PropCollider.Trunk, 0, placed);
        count += Scatter(random, parent, new[] { "bush", "bush_berry", "mushroom_cluster", "fallen_log", "stump" }, 22, () => InEllipse(random, center, 30f, 28f), clearings, 2.2f, 0.9f, 1.4f, PropCollider.None, 0, placed);
        return count;
    }

    private static int BuildSnow(Transform parent, int building)
    {
        int count = 0;
        Vector3 fire = new Vector3(14f, 0f, 96f);
        Place(parent, "build_campfire_stone", fire, 0f, 1f, PropCollider.None, 0).name = "Campfire";
        Place(parent, "fx_flame", fire + Vector3.up * 0.1f, 0f, 1f, PropCollider.None, 0);

        foreach ((Vector3 position, float yaw) in new[] { (new Vector3(9.5f, 0f, 100f), 140f), (new Vector3(18.5f, 0f, 100.5f), 220f), (CampTentPosition, CampTentYaw) }) // 110일차: 가운데 천막은 라그의 집
        {
            Place(parent, "zone_snow_tent", position, yaw, 1f, PropCollider.Bounds, building);
        }

        Place(parent, "zone_fur_rack", new Vector3(20f, 0f, 94.5f), 290f, 1f, PropCollider.Bounds, building);
        Place(parent, "prop_woodpile", new Vector3(8.5f, 0f, 94f), 30f, 1f, PropCollider.Bounds, building);
        Place(parent, "prop_banner", new Vector3(11f, 0f, 92.5f), 180f, 1f, PropCollider.None, 0);

        // 사당 : 붉은 문 · 석등 두 쌍 · 호코라
        GameObject torii = Place(parent, "zone_torii", new Vector3(42f, 0f, 97f), 0f, 1f, PropCollider.None, building);
        AddCapsule(torii, new Vector3(-1.9f, 2.2f, 0f), 0.3f, 4.4f);
        AddCapsule(torii, new Vector3(1.9f, 2.2f, 0f), 0.3f, 4.4f);
        torii.name = "Torii";

        foreach (Vector3 lantern in new[] { new Vector3(39.6f, 0f, 100.2f), new Vector3(44.4f, 0f, 100.2f), new Vector3(39.6f, 0f, 104f), new Vector3(44.4f, 0f, 104f) })
        {
            Place(parent, "zone_stone_lantern", lantern, 0f, 1f, PropCollider.Bounds, building);
        }

        Place(parent, "zone_hokora", new Vector3(42f, 0f, 106.5f), 180f, 1f, PropCollider.Bounds, building).name = "Hokora";
        GameObject shrineHouse = Place(parent, "zone_shrine_house", ShrineHousePosition, ShrineHouseYaw, 1f, PropCollider.None, building); // 101일차: 카스미의 집
        AddBox(shrineHouse, new Vector3(0f, 1.3f, 0f), new Vector3(4.2f, 2.6f, 3.6f));
        shrineHouse.name = "ShrineHouse_Kasumi";
        GameObject akaneHouse = Place(parent, "zone_shrine_house", AkaneShrineHousePosition, AkaneShrineHouseYaw, 1f, PropCollider.None, building); // 112일차: 아카네의 집 (같은 모양)
        AddBox(akaneHouse, new Vector3(0f, 1.3f, 0f), new Vector3(4.2f, 2.6f, 3.6f));
        akaneHouse.name = "ShrineHouse_Akane";
        GameObject yard = Place(parent, "zone_training_yard", TrainingYardPosition, TrainingYardYaw, 1f, PropCollider.None, building); // 112일차: 란후아의 집
        AddBox(yard, new Vector3(0f, 0.22f, 0f), new Vector3(5.4f, 0.44f, 4.2f));
        AddBox(yard, new Vector3(0f, 1.8f, -1.0f), new Vector3(4.8f, 2.8f, 1.8f));
        AddCapsule(yard, new Vector3(-1.2f, 1.3f, 1.0f), 0.2f, 1.8f);
        AddCapsule(yard, new Vector3(1.3f, 1.2f, 1.1f), 0.2f, 1.6f);
        AddBox(yard, new Vector3(2.4f, 1.3f, 0.9f), new Vector3(0.3f, 1.8f, 1.4f));
        yard.name = "TrainingYard_Lanhua";
        count += 16;

        System.Random random = new System.Random(Seed + 4);
        (Vector3, float)[] blocked = { (fire, 9f), (new Vector3(42f, 0f, 102f), 8f), (ShrineHousePosition, 6f), (AkaneShrineHousePosition, 6f), (TrainingYardPosition, 6.5f) };
        List<Vector3> placed = new List<Vector3>();
        count += Scatter(random, parent, new[] { "zone_snow_pine" }, 20, () => new Vector3(Rand(random, -26f, 70f), 0f, Rand(random, 90f, 119f)), blocked, 5f, 0.85f, 1.3f, PropCollider.Trunk, 0, placed);
        count += Scatter(random, parent, new[] { "zone_ice_rock" }, 6, () => new Vector3(Rand(random, -20f, 66f), 0f, Rand(random, 88f, 118f)), blocked, 6f, 0.8f, 1.3f, PropCollider.Bounds, building, placed);
        count += Scatter(random, parent, new[] { "zone_snow_pile" }, 8, () => new Vector3(Rand(random, -24f, 68f), 0f, Rand(random, 86f, 119f)), blocked, 4f, 0.8f, 1.5f, PropCollider.None, 0, placed);
        return count;
    }

    private static int BuildDesert(Transform parent, int building)
    {
        int count = 0;
        Vector3 oasis = Pools[0].center;
        GameObject pool = Place(parent, "zone_oasis_pool", oasis, 0f, 1f, PropCollider.None, 0);
        AddCapsule(pool, new Vector3(0f, 1f, 0f), StylizedModelLibrary.ZoneOasisRadius, 2f);
        pool.name = "Water_Oasis";

        foreach ((Vector3 offset, float yaw) in new[] { (new Vector3(5.6f, 0f, 2f), 200f), (new Vector3(-3.5f, 0f, 5.2f), 120f), (new Vector3(2.5f, 0f, -6f), 300f), (new Vector3(-6.4f, 0f, -2.6f), 40f) })
        {
            Place(parent, "zone_palm", oasis + offset, yaw, 1f, PropCollider.Trunk, 0);
        }

        GameObject tent = Place(parent, "zone_desert_tent", new Vector3(68f, 0f, -79f), 315f, 1f, PropCollider.None, building);
        AddBox(tent, new Vector3(0f, 1.2f, -1.95f), new Vector3(3.9f, 2.4f, 0.2f));

        foreach (Vector2 post in new[] { new Vector2(-1.9f, -1.9f), new Vector2(1.9f, -1.9f), new Vector2(-1.9f, 1.9f), new Vector2(1.9f, 1.9f) })
        {
            AddCapsule(tent, new Vector3(post.x, 1.2f, post.y), 0.12f, 2.4f);
        }

        tent.name = "MerchantTent";
        GameObject stable = Place(parent, "zone_caravan_stable", CaravanStablePosition, CaravanStableYaw, 1f, PropCollider.None, building); // 110일차: 켄시아의 집
        AddBox(stable, new Vector3(0f, 1.4f, -1.9f), new Vector3(5.9f, 2.8f, 0.25f));
        AddBox(stable, new Vector3(-2.1f, 0.65f, -1.0f), new Vector3(1.4f, 1.3f, 1.5f));
        AddBox(stable, new Vector3(0.6f, 0.45f, -1.45f), new Vector3(1.7f, 0.9f, 0.65f));
        AddBox(stable, new Vector3(2.1f, 0.6f, 0.4f), new Vector3(0.9f, 1.2f, 0.9f));
        AddBox(stable, new Vector3(3.9f, 0.6f, 0.3f), new Vector3(1.3f, 1.2f, 2.0f));

        foreach (float x in new[] { -2.8f, 0f, 2.8f })
        {
            AddCapsule(stable, new Vector3(x, 1.5f, 1.8f), 0.15f, 3f);
        }

        stable.name = "CaravanStable_Censia";
        GameObject beehive = Place(parent, "zone_beehive_hut", BeehiveHutPosition, BeehiveHutYaw, 1f, PropCollider.None, building); // 112일차: 베아트리체의 집
        AddCapsule(beehive, new Vector3(0f, 1.5f, 0f), 1.8f, 3f);

        foreach (Vector3 hive in new[] { new Vector3(-1.9f, 0f, 1.3f), new Vector3(2.0f, 0f, 1.1f), new Vector3(2.2f, 0f, -0.4f) })
        {
            AddBox(beehive, hive + new Vector3(0f, 0.5f, 0f), new Vector3(0.65f, 1.0f, 0.65f));
        }

        beehive.name = "BeehiveHut_Beatrice";

        foreach ((Vector3 position, float yaw, float scale) in new[] { (new Vector3(92f, 0f, -76f), 20f, 1.2f), (new Vector3(62f, 0f, -98f), 110f, 1f), (new Vector3(98f, 0f, -108f), 200f, 1.4f), (new Vector3(74f, 0f, -110f), 300f, 0.9f) })
        {
            Place(parent, "zone_mesa_rock", position, yaw, scale, PropCollider.Bounds, building);
        }

        foreach ((Vector3 position, float yaw) in new[] { (new Vector3(84f, 0f, -80f), 30f), (new Vector3(70f, 0f, -100f), 80f), (new Vector3(96f, 0f, -92f), 160f), (new Vector3(58f, 0f, -86f), 10f), (new Vector3(88f, 0f, -114f), 120f) })
        {
            GameObject dune = Place(parent, "zone_dune", position, yaw, 1f, PropCollider.None, 0);
            dune.AddComponent<MeshCollider>().sharedMesh = dune.GetComponent<MeshFilter>().sharedMesh;
        }

        Place(parent, "zone_bones", new Vector3(76f, 0f, -92f), 60f, 1f, PropCollider.None, 0);
        Place(parent, "zone_bones", new Vector3(100f, 0f, -84f), 200f, 0.8f, PropCollider.None, 0);
        count += 19;

        System.Random random = new System.Random(Seed + 5);
        (Vector3, float)[] blocked = { (oasis, 8f), (new Vector3(68f, 0f, -79f), 6f), (new Vector3(76f, 0f, -92f), 2f), (CaravanStablePosition, 7f), (BeehiveHutPosition, 5f) };
        List<Vector3> placed = new List<Vector3> { new Vector3(92f, 0f, -76f), new Vector3(62f, 0f, -98f), new Vector3(98f, 0f, -108f), new Vector3(74f, 0f, -110f) };
        count += Scatter(random, parent, new[] { "zone_cactus" }, 12, () => InEllipse(random, new Vector3(80f, 0f, -90f), 30f, 26f), blocked, 5f, 0.7f, 1.2f, PropCollider.Trunk, 0, placed);
        count += Scatter(random, parent, new[] { "rock_small", "pebbles" }, 8, () => InEllipse(random, new Vector3(80f, 0f, -90f), 30f, 26f), blocked, 3f, 0.8f, 1.4f, PropCollider.None, 0, placed);
        return count;
    }

    private static int BuildSwamp(Transform parent, int building)
    {
        int count = 0;

        foreach ((string name, Vector3 poolCenter, float poolRadius) in Pools.Skip(1))
        {
            Place(parent, "zone_swamp_pool", poolCenter, poolCenter.x * 17f, poolRadius / StylizedModelLibrary.ZoneSwampPoolRadius, PropCollider.None, 0).name = "Water_" + name;
        }

        // 나무길 (웅덩이 A를 동서로 가로지름)
        GameObject walk = Place(parent, "zone_boardwalk", new Vector3(-67.5f, 0f, -98f), 90f, 1f, PropCollider.None, building);
        AddBox(walk, new Vector3(0f, StylizedModelLibrary.ZoneBoardwalkDeckHeight * 0.5f, StylizedModelLibrary.ZoneBoardwalkLength * 0.5f), new Vector3(StylizedModelLibrary.ZoneBoardwalkWidth, StylizedModelLibrary.ZoneBoardwalkDeckHeight, StylizedModelLibrary.ZoneBoardwalkLength - 1.6f));
        walk.name = "Boardwalk";
        GameObject hut = Place(parent, "zone_stilt_hut", new Vector3(-72f, 0f, -106f), 45f, 1f, PropCollider.None, building);
        AddBox(hut, new Vector3(0f, 1.7f, 0f), new Vector3(3.8f, 3.4f, 3.8f));
        hut.name = "StiltHut";
        GameObject hunterHut = Place(parent, "zone_stilt_hut", HunterHutPosition, HunterHutYaw, 1f, PropCollider.None, building); // 110일차: 하르카의 집
        AddBox(hunterHut, new Vector3(0f, 1.7f, 0f), new Vector3(3.8f, 3.4f, 3.8f));
        hunterHut.name = "StiltHut_Harka";
        GameObject manor = Place(parent, "zone_gothic_manor", GothicManorPosition, GothicManorYaw, 1f, PropCollider.None, building); // 112일차: 릴리카의 집
        AddBox(manor, new Vector3(0f, 1.6f, -0.1f), new Vector3(4.6f, 3.2f, 3.2f));
        AddBox(manor, new Vector3(-1.45f, 0.45f, 2.45f), new Vector3(1.7f, 0.9f, 0.12f));
        AddBox(manor, new Vector3(1.45f, 0.45f, 2.45f), new Vector3(1.7f, 0.9f, 0.12f));
        manor.name = "GothicManor_Lilica";
        count += 7;

        System.Random random = new System.Random(Seed + 6);
        Vector3 center = new Vector3(-60f, 0f, -94f);
        (Vector3, float)[] blocked = Pools.Skip(1).Select(pool => (pool.center, pool.radius + 1f)).Concat(new[] { (new Vector3(-72f, 0f, -106f), 4.5f), (new Vector3(-62f, 0f, -98f), 6f), (HunterHutPosition, 5f), (GothicManorPosition, 5.5f) }).ToArray();
        List<Vector3> placed = new List<Vector3>();
        count += Scatter(random, parent, new[] { "zone_dead_tree" }, 8, () => InEllipse(random, center, 26f, 20f), blocked, 5f, 0.8f, 1.3f, PropCollider.Trunk, 0, placed);
        count += Scatter(random, parent, new[] { "zone_glow_mushrooms", "mushroom_cluster", "rock_small" }, 8, () => InEllipse(random, center, 24f, 18f), blocked, 3f, 0.8f, 1.2f, PropCollider.None, 0, placed);

        // 웅덩이 가장자리 갈대
        foreach ((string _, Vector3 poolCenter, float poolRadius) in Pools.Skip(1))
        {
            for (int index = 0; index < 6; index++)
            {
                float angle = index * 1.05f + poolCenter.z;
                Vector3 position = poolCenter + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (poolRadius + 0.4f);

                if (Flat(position, new Vector3(-62f, 0f, -98f)) < poolRadius + 1f && Mathf.Abs(position.z + 98f) < 1.4f)
                {
                    continue; // 나무길 끝은 비움
                }

                Place(parent, "reeds", position, angle * 50f, Rand(random, 1.2f, 1.8f), PropCollider.None, 0);
                count++;
            }
        }

        return count;
    }

    // 마을 쪽 길 시작의 표지판 (구역 이름, 마을에서 읽히게)
    private static int BuildSignposts(Transform parent)
    {
        foreach (ZoneInfo zone in Zones)
        {
            Vector3 along = (zone.Entrance - zone.PathStart).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, along);
            Vector3 position = zone.PathStart + along * 2.5f + side * 2.4f;
            float yaw = Mathf.Atan2(along.x, along.z) * Mathf.Rad2Deg - 90f;
            GameObject post = Place(parent, "prop_signpost", position, yaw, 1f, PropCollider.None, 0);
            AddBox(post, new Vector3(0f, 0.9f, 0f), new Vector3(0.2f, 1.8f, 0.2f));
            post.name = "Sign_" + zone.ObjectName;
            Vector3 toVillage = -along;
            int road = IslandZoneLayout.MainRoadIndex(zone.Id);
            string distance = islandLayout && road >= 0 ? $" {Mathf.RoundToInt(IslandZoneLayout.RoadLength(road) / 10f) * 10}m" : string.Empty; // 107일차: 흙길 길이
            TMP_Text label = CreateText(post.transform.parent, "SignLabel_" + zone.ObjectName, $"<mark=#3A2A20C0 padding=\"16,16,6,6\">{zone.DisplayName} →{distance}</mark>", 3.2f, 7.5f);
            label.transform.SetPositionAndRotation(position + Vector3.up * 2.35f, Quaternion.Euler(0f, Mathf.Atan2(-toVillage.x, -toVillage.z) * Mathf.Rad2Deg, 0f));
        }

        return Zones.Length;
    }

    private static TMP_Text CreateText(Transform parent, string name, string text, float fontSize, float width)
    {
        GameObject holder = new GameObject(name, typeof(RectTransform));
        holder.transform.SetParent(parent, false);
        TextMeshPro label = holder.AddComponent<TextMeshPro>();
        label.rectTransform.sizeDelta = new Vector2(width, 1f);
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.color = Color.white;
        label.richText = true;
        label.text = text;
        MeshRenderer renderer = holder.GetComponent<MeshRenderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return label;
    }

    // ---------------------------------------------------------------- 바닥 (모래 · 눈 · 진흙 · 돌바닥 · 흙길)

    private static float Smooth(float edge0, float edge1, float value)
    {
        return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(edge0, edge1, value));
    }

    private static float Ellipse(float x, float z, float cx, float cz, float rx, float rz, float fade, float noise)
    {
        float dx = (x - cx) / rx;
        float dz = (z - cz) / rz;
        float distance = Mathf.Sqrt(dx * dx + dz * dz) + noise * 0.12f;
        return 1f - Smooth(1f - fade, 1f, distance);
    }

    // 좌표의 구역 바닥 비율 (모래 · 눈 · 진흙 · 돌바닥)과 흙 비율 (흙길 · 숲 바닥)
    private static bool islandGround; // 105일차: 바닥 칠하기 중인 Terrain이 무인도인지

    public static void GroundWeights(float x, float z, float[] zone, out float dirt)
    {
        if (islandGround)
        {
            IslandGroundWeights(x, z, zone, out dirt);
            return;
        }

        float noise = Mathf.PerlinNoise(x * 0.07f + 31.7f, z * 0.07f + 12.9f) - 0.5f;
        float coast = Smooth(84.5f, 90.5f, x + noise * 5f);

        if (islandGround) // 105일차: 무인도에서는 석호 둘레 해변만 모래
        {
            coast *= 1f - Smooth(12f, 32f, IslandTerrainBuilder.LagoonDistance(x, z) + noise * 6f);
        }

        float desert = Ellipse(x, z, 80f, -90f, 38f, 32f, 0.25f, noise);
        zone[0] = Mathf.Max(coast, desert);
        zone[1] = Ellipse(x, z, 24f, 120f, 64f, 36f, 0.3f, noise);
        zone[2] = Ellipse(x, z, -60f, -96f, 29f, 23f, 0.3f, noise);
        zone[3] = Ellipse(x, z, -80f, 80f, 21f, 21f, 0.25f, noise) * (0.35f + 0.65f * Mathf.PerlinNoise(x * 0.35f + 5.1f, z * 0.35f + 8.3f));
        float sum = zone[0] + zone[1] + zone[2] + zone[3];

        if (sum > 1f)
        {
            for (int index = 0; index < 4; index++)
            {
                zone[index] /= sum;
            }
        }

        float path = 0f;
        Vector3 point = new Vector3(x, 0f, z);

        foreach (ZoneInfo info in Zones)
        {
            float distance = StylizedTerrainPainter.DistanceToSegmentXZ(point, info.PathStart, info.Entrance);
            float half = 1.6f + noise * 0.8f;
            path = Mathf.Max(path, 1f - Smooth(half * 0.55f, half, distance));
        }

        float forest = Ellipse(x, z, -88f, -32f, 32f, 30f, 0.3f, noise) * 0.42f;
        dirt = Mathf.Max(path, forest);
    }

    // 107일차: 무인도 바닥 비율 = 구역마다 설계 좌표로 바꿔 예전 모양 그대로 + 석호 둘레 모래 + 마을 → 구역 흙길
    private static void IslandGroundWeights(float x, float z, float[] zone, out float dirt)
    {
        float noise = Mathf.PerlinNoise(x * 0.07f + 31.7f, z * 0.07f + 12.9f) - 0.5f;
        Vector2 coast = IslandZoneLayout.OffsetOf("coast");
        float coastX = x - coast.x;
        float coastZ = z - coast.y;
        float coastSand = Smooth(84.5f, 90.5f, coastX + noise * 5f) * (1f - Smooth(66f, 80f, Mathf.Abs(coastZ - 4f) + noise * 6f)) * (1f - Smooth(150f, 170f, coastX));
        float lagoonSand = 1f - Smooth(12f, 32f, IslandTerrainBuilder.LagoonDistance(x, z) + noise * 6f);
        Vector2 desert = IslandZoneLayout.OffsetOf("desert");
        Vector2 snow = IslandZoneLayout.OffsetOf("snow");
        Vector2 swamp = IslandZoneLayout.OffsetOf("swamp");
        Vector2 ruins = IslandZoneLayout.OffsetOf("ruins");
        Vector2 forest = IslandZoneLayout.OffsetOf("forest");
        zone[0] = Mathf.Max(Mathf.Max(coastSand, lagoonSand), Ellipse(x - desert.x, z - desert.y, 80f, -90f, 38f, 32f, 0.25f, noise));
        zone[1] = Ellipse(x - snow.x, z - snow.y, 24f, 120f, 64f, 36f, 0.3f, noise);
        zone[2] = Ellipse(x - swamp.x, z - swamp.y, -60f, -96f, 29f, 23f, 0.3f, noise);
        zone[3] = Ellipse(x - ruins.x, z - ruins.y, -80f, 80f, 21f, 21f, 0.25f, noise) * (0.35f + 0.65f * Mathf.PerlinNoise(x * 0.35f + 5.1f, z * 0.35f + 8.3f));
        float sum = zone[0] + zone[1] + zone[2] + zone[3];

        if (sum > 1f)
        {
            for (int index = 0; index < 4; index++)
            {
                zone[index] /= sum;
            }
        }

        float half = 1.6f + noise * 0.8f;
        float path = 1f - Smooth(half * 0.55f, half, IslandZoneLayout.RoadDistanceWorld(new Vector3(x, 0f, z)));
        float woods = Ellipse(x - forest.x, z - forest.y, -88f, -32f, 32f, 30f, 0.3f, noise) * 0.42f;
        dirt = Mathf.Max(path, woods);
    }

    private static string PaintTerrain()
    {
        Terrain terrain = Terrain.activeTerrain != null ? Terrain.activeTerrain : Object.FindFirstObjectByType<Terrain>();

        if (terrain == null || terrain.terrainData == null)
        {
            return "✗ Terrain이 없어 바닥 칠하기를 건너뜀";
        }

        TerrainData data = terrain.terrainData;
        islandGround = IslandTerrainBuilder.IsIslandTerrain(terrain);
        List<TerrainLayer> baseLayers = data.terrainLayers.Where(layer => layer != null && !layer.name.StartsWith("TL_Zone", StringComparison.Ordinal)).ToList();
        TerrainLayer[] zoneLayers =
        {
            StylizedTerrainPainter.GetOrCreateLayer(ZoneLayerNames[0], SandPixel, 6f),
            StylizedTerrainPainter.GetOrCreateLayer(ZoneLayerNames[1], SnowPixel, 7f),
            StylizedTerrainPainter.GetOrCreateLayer(ZoneLayerNames[2], MudPixel, 5f),
            StylizedTerrainPainter.GetOrCreateLayer(ZoneLayerNames[3], FlagstonePixel, 4f)
        };

        // 기존 비율을 층 이름으로 옮겨 담고 층 목록을 (기본 층 + 구역 층 4개)로 맞춘다
        int resolution = data.alphamapResolution;
        float[,,] previous = data.GetAlphamaps(0, 0, resolution, resolution);
        TerrainLayer[] oldLayers = data.terrainLayers;
        data.terrainLayers = baseLayers.Concat(zoneLayers).ToArray();
        int baseCount = baseLayers.Count;
        int dirtIndex = baseLayers.FindIndex(layer => layer.name == "TL_Dirt");
        int[] oldIndexOf = data.terrainLayers.Select(layer => Array.IndexOf(oldLayers, layer)).ToArray();
        float[,,] alphas = new float[resolution, resolution, data.terrainLayers.Length];
        Vector3 origin = terrain.transform.position;
        Vector3 size = data.size;
        float[] zone = new float[4];
        float[] bases = new float[baseCount];

        for (int y = 0; y < resolution; y++)
        {
            float z = origin.z + y / (float)(resolution - 1) * size.z;

            for (int x = 0; x < resolution; x++)
            {
                float worldX = origin.x + x / (float)(resolution - 1) * size.x;
                GroundWeights(worldX, z, zone, out float dirt);
                float baseSum = 0f;

                for (int index = 0; index < baseCount; index++)
                {
                    bases[index] = oldIndexOf[index] >= 0 ? previous[y, x, oldIndexOf[index]] : 0f;
                    baseSum += bases[index];
                }

                if (baseSum < 0.0001f)
                {
                    Array.Clear(bases, 0, baseCount);
                    bases[0] = 1f;
                }
                else
                {
                    for (int index = 0; index < baseCount; index++)
                    {
                        bases[index] /= baseSum; // 이전 실행의 구역 비율을 빼고 원래 바닥 비율로 되돌림
                    }
                }

                if (dirtIndex >= 0 && dirt > bases[dirtIndex])
                {
                    float rest = 1f - bases[dirtIndex];
                    float scale = rest > 0.0001f ? (1f - dirt) / rest : 0f;

                    for (int index = 0; index < baseCount; index++)
                    {
                        bases[index] = index == dirtIndex ? dirt : bases[index] * scale;
                    }
                }

                float zoneSum = zone[0] + zone[1] + zone[2] + zone[3];

                for (int index = 0; index < baseCount; index++)
                {
                    alphas[y, x, index] = bases[index] * (1f - zoneSum);
                }

                for (int index = 0; index < 4; index++)
                {
                    alphas[y, x, baseCount + index] = zone[index];
                }
            }
        }

        data.SetAlphamaps(0, 0, alphas);

        // 구역 바닥 · 흙길의 풀 없애기
        int removed = 0;
        bool islandTerrain = IslandTerrainBuilder.IsIslandTerrain(terrain); // 105일차: 무인도는 석호만 물

        if (data.detailPrototypes.Length > 0)
        {
            int detailResolution = data.detailResolution;
            int[,] density = data.GetDetailLayer(0, 0, detailResolution, detailResolution, 0);

            for (int y = 0; y < detailResolution; y++)
            {
                float z = origin.z + (y + 0.5f) / detailResolution * size.z;

                for (int x = 0; x < detailResolution; x++)
                {
                    if (density[y, x] == 0)
                    {
                        continue;
                    }

                    float worldX = origin.x + (x + 0.5f) / detailResolution * size.x;
                    GroundWeights(worldX, z, zone, out float dirt);

                    if (zone[0] + zone[1] + zone[2] + zone[3] > 0.3f || dirt > 0.4f || (islandTerrain ? IslandTerrainBuilder.IsLagoon(worldX, z) : worldX > SeaLine - 1f))
                    {
                        removed += density[y, x];
                        density[y, x] = 0;
                    }
                }
            }

            data.SetDetailLayer(0, 0, 0, density);
        }

        EditorUtility.SetDirty(data);
        return $"바닥 칠하기 : 층 {data.terrainLayers.Length}개 (기본 {baseCount} + 모래 · 눈 · 진흙 · 돌바닥), 해상도 {resolution}, 풀 {removed}포기 정리";
    }

    private static Color SandPixel(float u, float v)
    {
        float large = StylizedTerrainPainter.TileNoise(u, v, 4f, 2.4f);
        float ripple = Mathf.Sin((v + StylizedTerrainPainter.TileNoise(u, v, 3f, 6.1f) * 0.3f) * Mathf.PI * 2f * 10f) * 0.5f + 0.5f;
        Color color = Color.Lerp(new Color(0.84f, 0.74f, 0.52f), new Color(0.9f, 0.81f, 0.6f), large);
        color = Color.Lerp(color, new Color(0.76f, 0.66f, 0.46f), ripple * 0.18f);

        if (StylizedTerrainPainter.Hash(u, v, 256) > 0.97f)
        {
            color = Color.Lerp(color, new Color(0.62f, 0.55f, 0.46f), 0.6f);
        }

        return color;
    }

    private static Color SnowPixel(float u, float v)
    {
        float large = StylizedTerrainPainter.TileNoise(u, v, 4f, 9.2f);
        float small = StylizedTerrainPainter.TileNoise(u, v, 16f, 3.3f);
        Color color = Color.Lerp(new Color(0.86f, 0.9f, 0.95f), new Color(0.97f, 0.98f, 1f), large);
        return Color.Lerp(color, new Color(0.8f, 0.86f, 0.94f), small * 0.3f);
    }

    private static Color MudPixel(float u, float v)
    {
        float large = StylizedTerrainPainter.TileNoise(u, v, 5f, 4.8f);
        float wet = StylizedTerrainPainter.TileNoise(u, v, 9f, 1.4f);
        Color color = Color.Lerp(new Color(0.3f, 0.27f, 0.2f), new Color(0.4f, 0.37f, 0.26f), large);

        if (wet > 0.6f)
        {
            color = Color.Lerp(color, new Color(0.24f, 0.28f, 0.22f), 0.6f);
        }

        return color;
    }

    private static Color FlagstonePixel(float u, float v)
    {
        // 4 × 4 돌판 (줄마다 반 칸 어긋남) · 이음새 · 돌마다 다른 밝기
        float row = Mathf.Floor(v * 4f);
        float shifted = u * 4f + (row % 2f) * 0.5f;
        float column = Mathf.Floor(shifted);
        float localU = shifted - column;
        float localV = v * 4f - row;
        bool seam = localU < 0.05f || localU > 0.95f || localV < 0.05f || localV > 0.95f;
        float tone = StylizedTerrainPainter.Hash((column % 4f + 0.5f) / 4f, (row + 0.5f) / 4f, 4);
        Color color = Color.Lerp(new Color(0.55f, 0.54f, 0.49f), new Color(0.7f, 0.68f, 0.61f), tone);
        color = Color.Lerp(color, new Color(0.45f, 0.44f, 0.4f), StylizedTerrainPainter.TileNoise(u, v, 12f, 7.4f) * 0.25f);
        return seam ? color * 0.62f : color;
    }

    // ---------------------------------------------------------------- 기존 나무 · 바위 · 풀 정리

    private static bool IsZoneCore(Vector3 position)
    {
        if (islandGround) // 107일차: 무인도는 석호 둘레 · 흙길 · 옮긴 구역 바닥
        {
            float[] weights = new float[4];
            GroundWeights(position.x, position.z, weights, out float ground);
            return IslandTerrainBuilder.LagoonDistance(position.x, position.z) < 4f || weights[0] > 0.4f || weights[1] > 0.4f || weights[2] > 0.4f || weights[3] > 0.2f || ground > 0.5f;
        }

        if (position.x > 86f)
        {
            return true; // 해안 · 바다
        }

        float[] zone = new float[4];
        GroundWeights(position.x, position.z, zone, out _);

        if (zone[0] > 0.4f || zone[1] > 0.4f || zone[2] > 0.4f || zone[3] > 0.2f)
        {
            return true; // 모래 · 눈 · 진흙 · 돌바닥 위
        }

        return PathDistance(position) < 2.6f || Flat(position, new Vector3(-88f, 0f, -32f)) < 16f; // 흙길 · 숲 빈터
    }

    private static string HideEnvironment(Scene scene)
    {
        GameObject environment = scene.GetRootGameObjects().FirstOrDefault(item => item.name == EnvironmentRootName);

        if (environment == null)
        {
            return "기존 나무 정리 : 환경 오브젝트 없음";
        }

        int hidden = 0;

        foreach (Transform group in environment.transform)
        {
            if (group.name != "Trees" && group.name != "Rocks" && group.name != "Plants")
            {
                continue;
            }

            foreach (Transform item in group)
            {
                if (item.gameObject.activeSelf && IsZoneCore(item.position))
                {
                    item.gameObject.SetActive(false);
                    hidden++;
                }
            }
        }

        return $"기존 나무 · 바위 · 풀 정리 : 구역 · 흙길 자리 {hidden}개 숨김";
    }

    // ---------------------------------------------------------------- 섬 경계 · 바다 벽

    private static string BuildBoundary(Transform parent)
    {
        void Wall(string name, Vector3 center, Vector3 size)
        {
            GameObject wall = new GameObject(name);
            wall.transform.SetParent(parent, false);
            wall.transform.position = center;
            wall.AddComponent<BoxCollider>().size = size;
        }

        if (IslandTerrainBuilder.IsIslandTerrain(Terrain.activeTerrain != null ? Terrain.activeTerrain : Object.FindFirstObjectByType<Terrain>()))
        {
            // 105일차: 무인도에는 가장자리 벽이 없다 (바다 경계는 무인도 도구의 ShoreGuard). 석호는 NPC가 걷지 않음
            GameObject lagoon = new GameObject("Lagoon_NotWalkable");
            lagoon.transform.SetParent(parent, false);
            Vector2 center = (IslandTerrainBuilder.LagoonMin + IslandTerrainBuilder.LagoonMax) * 0.5f;
            Vector2 extent = IslandTerrainBuilder.LagoonMax - IslandTerrainBuilder.LagoonMin;
            lagoon.transform.position = new Vector3(center.x, IslandTerrainBuilder.SeaLevel, center.y);
            NavMeshModifierVolume lagoonVolume = lagoon.AddComponent<NavMeshModifierVolume>();
            lagoonVolume.size = new Vector3(extent.x, 6f, extent.y);
            lagoonVolume.center = Vector3.zero;
            lagoonVolume.area = NavMesh.GetAreaFromName("Not Walkable");
            return "무인도 : 가장자리 벽 없음 · 석호 NavMesh 제외";
        }

        Wall("Edge_North", new Vector3(0f, 3f, 124.8f), new Vector3(252f, 6f, 0.6f));
        Wall("Edge_South", new Vector3(0f, 3f, -124.8f), new Vector3(252f, 6f, 0.6f));
        Wall("Edge_West", new Vector3(-124.8f, 3f, 0f), new Vector3(0.6f, 6f, 252f));
        Wall("Edge_East", new Vector3(124.8f, 3f, 0f), new Vector3(0.6f, 6f, 252f));

        // 물가 벽 (부두 자리만 비움)
        float gapHalf = StylizedModelLibrary.ZoneDockWidth * 0.5f + 0.2f;
        float north = 125f - (10f + gapHalf);
        float south = (10f - gapHalf) + 125f;
        Wall("SeaWall_North", new Vector3(SeaLine + 0.3f, 1.5f, 10f + gapHalf + north * 0.5f), new Vector3(0.6f, 3f, north));
        Wall("SeaWall_South", new Vector3(SeaLine + 0.3f, 1.5f, 10f - gapHalf - south * 0.5f), new Vector3(0.6f, 3f, south));

        // 바다는 NPC가 걷지 않는 곳 (NavMesh 제외)
        GameObject noWalk = new GameObject("Sea_NotWalkable");
        noWalk.transform.SetParent(parent, false);
        noWalk.transform.position = new Vector3((SeaLine + 0.3f + 125f) * 0.5f, 1f, 0f);
        NavMeshModifierVolume volume = noWalk.AddComponent<NavMeshModifierVolume>();
        volume.size = new Vector3(125f - SeaLine - 0.3f, 4f, 252f);
        volume.center = Vector3.zero;
        volume.area = NavMesh.GetAreaFromName("Not Walkable");
        return "섬 경계 벽 4개 · 바다 벽 2개 (부두 자리 비움) · 바다 NavMesh 제외";
    }

    // ---------------------------------------------------------------- 지도 구역 이름

    private static int EnsureMapLabelLayer()
    {
        int existing = LayerMask.NameToLayer(MapLabelLayerName);

        if (existing >= 0)
        {
            return existing;
        }

        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");

        for (int index = 19; index < layers.arraySize; index++)
        {
            SerializedProperty layer = layers.GetArrayElementAtIndex(index);

            if (string.IsNullOrEmpty(layer.stringValue))
            {
                layer.stringValue = MapLabelLayerName;
                tagManager.ApplyModifiedPropertiesWithoutUndo();
                return index;
            }
        }

        return -1;
    }

    private static string BuildMapLabels(Scene scene, Transform parent)
    {
        int layer = EnsureMapLabelLayer();

        if (layer < 0)
        {
            return "✗ 지도 이름용 레이어를 만들 빈 칸이 없습니다.";
        }

        List<(string text, Vector3 position)> labels = Zones.Select(zone => (zone.DisplayName, ZoneCenterWorld(zone))).ToList(); // 107일차: 옮긴 자리
        labels.Add(("마을", new Vector3(0f, 0f, 14f)));

        if (islandLayout) // 108일차: 섬 전체 지도 이름 (난파선 해변 · 석호)
        {
            (_, Vector3 wreck, _) = IslandTerrainBuilder.StartPoints();
            Vector2 lagoon = (IslandTerrainBuilder.LagoonMin + IslandTerrainBuilder.LagoonMax) * 0.5f;
            labels.Add((WreckLabel, wreck + new Vector3(0f, 0f, 40f)));
            labels.Add((LagoonLabel, new Vector3(lagoon.x, 0f, lagoon.y)));
        }

        if (parent.GetComponent<MapLabelScaler>() == null) // 108일차: 섬 전체를 볼수록 이름을 크게
        {
            parent.gameObject.AddComponent<MapLabelScaler>();
        }

        foreach ((string text, Vector3 position) in labels)
        {
            TMP_Text label = CreateText(parent, "MapLabel_" + text, $"<mark=#1C1C20B0 padding=\"20,20,8,8\"><b>{text}</b></mark>", 60f, 80f);
            label.transform.SetPositionAndRotation(new Vector3(position.x, Mathf.Max(40f, position.y + 40f), position.z), Quaternion.Euler(90f, 0f, 0f));
            label.gameObject.layer = layer;
        }

        // 지도 카메라만 이 레이어를 그리고, 다른 카메라는 그리지 않는다
        int bit = 1 << layer;
        MinimapCameraController map = Object.FindFirstObjectByType<MinimapCameraController>(FindObjectsInactive.Include);
        Camera mapCamera = null;

        if (map != null)
        {
            SerializedObject serialized = new SerializedObject(map);
            SerializedProperty mask = serialized.FindProperty("mapLayerMask");
            mask.intValue |= bit;
            mapCamera = serialized.FindProperty("mapCamera").objectReferenceValue as Camera;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        int excluded = 0;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Camera camera in root.GetComponentsInChildren<Camera>(true))
            {
                if (camera == mapCamera || (map != null && camera.transform.IsChildOf(map.transform)))
                {
                    camera.cullingMask |= bit;
                    continue;
                }

                camera.cullingMask &= ~bit;
                EditorUtility.SetDirty(camera);
                excluded++;
            }
        }

        return $"지도 구역 이름 {labels.Count}개 (레이어 {MapLabelLayerName}, 지도 카메라에만 표시 · 다른 카메라 {excluded}개 제외)";
    }

    // ---------------------------------------------------------------- NPC 일정 위치

    private static string BuildLocations(Scene scene, Transform parent)
    {
        NpcDatabase database = AssetDatabase.LoadAssetAtPath<NpcDatabase>(NpcContentBuilder.DatabasePath);

        if (database == null)
        {
            return "✗ NpcDatabase가 없습니다.";
        }

        List<NpcLocationPoint> created = new List<NpcLocationPoint>();
        StringBuilder report = new StringBuilder();

        foreach (NpcDatabase.Location location in database.Locations.Where(item => !item.IsVillage))
        {
            if (!PointLayout.TryGetValue(location.LocationId, out (Vector3 position, Vector3 lookAt) spec))
            {
                report.AppendLine($"✗ 새 구역 위치 배치 정보가 없습니다: {location.LocationId}");
                continue;
            }

            GameObject holder = new GameObject(location.LocationId);
            holder.transform.SetParent(parent, false);
            Vector3 facing = spec.lookAt - spec.position;
            holder.transform.SetPositionAndRotation(ZoneToWorld(location.ZoneId, spec.position), Quaternion.Euler(0f, Mathf.Atan2(facing.x, facing.z) * Mathf.Rad2Deg, 0f)); // 107일차: 구역 자리로
            NpcLocationPoint point = holder.AddComponent<NpcLocationPoint>();
            point.EditorAssign(location.LocationId, location.DisplayName, location.IsHome, NpcLocationAnchor.Fixed); // 101일차: 집이면 밤에 안으로
            created.Add(point);
        }

        // NPC 관리자 위치 목록 = 마을 위치 + 새 구역 위치
        NpcManager manager = Object.FindFirstObjectByType<NpcManager>(FindObjectsInactive.Include);

        if (manager == null)
        {
            report.AppendLine($"새 구역 위치 {created.Count}곳 (NPC 관리자가 없어 연결 생략 : 8번 메뉴를 먼저 실행하세요)");
            return report.ToString().TrimEnd();
        }

        List<NpcLocationPoint> merged = manager.Locations.Where(point => point != null && !created.Any(item => item.LocationId == point.LocationId) && !point.transform.IsChildOf(parent.parent)).ToList();
        int village = merged.Count;
        merged.AddRange(created);
        SerializedObject serialized = new SerializedObject(manager);
        SerializedProperty list = serialized.FindProperty("locations");
        list.arraySize = merged.Count;

        for (int index = 0; index < merged.Count; index++)
        {
            list.GetArrayElementAtIndex(index).objectReferenceValue = merged[index];
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        report.AppendLine($"새 구역 위치 {created.Count}곳 · NPC 관리자 위치 {merged.Count}곳 (마을 {village} + 새 구역 {created.Count})");
        return report.ToString().TrimEnd();
    }

    // ---------------------------------------------------------------- 적 생성 지점

    private static string BuildEnemySpawns(Scene scene)
    {
        GameObject root = scene.GetRootGameObjects().FirstOrDefault(item => item.name == SpawnRootName);

        if (root == null)
        {
            return "적 생성 지점 : 적 생성 루트가 없어 건너뜀";
        }

        int made = 0;

        foreach ((string id, string templateName, string zoneId, Vector3 design) in ZoneSpawns)
        {
            Vector3 position = ZoneToWorld(zoneId, design); // 107일차: 구역 자리로
            string objectName = "Spawn_" + id.Replace("spawn_", string.Empty);
            Transform existing = root.transform.Find(objectName);
            Transform template = root.transform.Find(templateName);

            if (template == null)
            {
                continue;
            }

            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            GameObject clone = Object.Instantiate(template.gameObject, root.transform);
            clone.name = objectName;
            clone.transform.SetPositionAndRotation(position, template.rotation);
            SerializedObject serialized = new SerializedObject(clone.GetComponent<EnemySpawnPoint>());
            serialized.FindProperty("spawnPointId").stringValue = id;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            made++;
        }

        return $"적 생성 지점 {made}곳 (고대 폐허 근접형 · 안개 습지 원거리형)";
    }

    // ---------------------------------------------------------------- NavMesh

    // 즉시 굽고 기존 NavMesh Asset에 덮어써 GUID를 유지한다 (101일차: NPC 마을 배치 도구도 사용)
    public static string RebakeNavMesh()
    {
        NavMeshSurface surface = Object.FindFirstObjectByType<NavMeshSurface>();

        if (surface == null)
        {
            return "✗ NavMeshSurface가 없어 NavMesh를 굽지 못했습니다.";
        }

        // 112일차: NPC는 돌아다니므로 길(NavMesh)에 구멍을 내지 않게 굽는 동안 잠시 숨긴다 (물가에 선 옥타비아 자리에 길이 끊기던 문제)
        List<GameObject> hiddenNpcs = Object.FindObjectsByType<NpcAgent>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Select(agent => agent.gameObject).ToList();
        hiddenNpcs.ForEach(npc => npc.SetActive(false));
        Physics.SyncTransforms();
        NavMeshData previous = surface.navMeshData;
        string path = previous != null ? AssetDatabase.GetAssetPath(previous) : null;
        float started = Time.realtimeSinceStartup;

        try
        {
            surface.BuildNavMesh();
        }
        finally
        {
            hiddenNpcs.ForEach(npc => npc.SetActive(true));
            Physics.SyncTransforms();
        }

        NavMeshData built = surface.navMeshData;

        if (built == null)
        {
            return "✗ NavMesh 굽기에 실패했습니다.";
        }

        if (!string.IsNullOrEmpty(path) && built != previous)
        {
            surface.RemoveData();
            EditorUtility.CopySerialized(built, previous);
            previous.name = System.IO.Path.GetFileNameWithoutExtension(path);
            surface.navMeshData = previous;
            surface.AddData();
            Object.DestroyImmediate(built);
            EditorUtility.SetDirty(previous);
            AssetDatabase.SaveAssetIfDirty(previous);
        }
        else if (string.IsNullOrEmpty(path))
        {
            string folder = System.IO.Path.ChangeExtension(ScenePath, null);

            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder(System.IO.Path.GetDirectoryName(folder), System.IO.Path.GetFileName(folder));
            }

            AssetDatabase.CreateAsset(built, $"{folder}/NavMesh-{surface.name}.asset");
        }

        EditorUtility.SetDirty(surface);
        NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
        return $"NavMesh 다시 굽기 완료 ({Time.realtimeSinceStartup - started:0.0}초, 삼각형 {triangulation.indices.Length / 3}개)";
    }

    // ---------------------------------------------------------------- 107일차: 섬 곳곳으로 옮기기

    // 구역 묶음을 설계 좌표 → 섬 자리로 옮기고, 소품을 땅 높이에 맞춘다 (해안은 높이 0 그대로)
    private static string MoveZonesToIsland(Transform root)
    {
        StringBuilder report = new StringBuilder("구역 자리 : ");
        Physics.SyncTransforms();

        foreach (ZoneInfo zone in Zones)
        {
            Transform group = root.Find(zone.ObjectName);
            IslandZoneLayout.Site site = IslandZoneLayout.Get(zone.Id);

            if (group == null || site == null)
            {
                continue;
            }

            group.position = new Vector3(site.Offset.x, 0f, site.Offset.y);

            if (!site.Coast)
            {
                foreach (Transform child in group)
                {
                    Vector3 position = child.position;
                    position.y = GroundY(position) + position.y; // 설계 높이(모닥불 불꽃 0.1 등)는 그대로 더함
                    child.position = position;
                }
            }

            Vector3 center = ZoneCenterWorld(zone);
            report.Append($"{zone.DisplayName} ({center.x:0}, {center.z:0}, 높이 {center.y:0.0}) · ");
        }

        return report.ToString().TrimEnd(' ', '·');
    }

    // 108일차: 둘레길 · 갈림길 양 끝 표지판 (반대쪽 끝 이름과 거리)
    private static int BuildRoadSigns(Transform parent)
    {
        int made = 0;

        for (int road = 0; road < IslandZoneLayout.RoadCount; road++)
        {
            IslandZoneLayout.Road info = IslandZoneLayout.Roads[road];

            if (info.Kind == IslandZoneLayout.RoadKind.Main)
            {
                continue;
            }

            float length = IslandZoneLayout.RoadLength(road);
            int distance = Mathf.RoundToInt(length / 10f) * 10;

            for (int end = 0; end < 2; end++)
            {
                float at = end == 0 ? 7f : length - 7f;
                Vector3 point = IslandZoneLayout.RoadPoint(road, at);
                Vector3 next = IslandZoneLayout.RoadPoint(road, end == 0 ? at + 4f : at - 4f);
                Vector3 along = next - point;
                along.y = 0f;
                along.Normalize();
                Vector3 side = Vector3.Cross(Vector3.up, along);
                Vector3 position = point + side * 2.6f;
                position.y = GroundY(position);
                float yaw = Mathf.Atan2(along.x, along.z) * Mathf.Rad2Deg - 90f;
                GameObject post = Place(parent, "prop_signpost", position, yaw, 1f, PropCollider.None, 0);

                if (post == null)
                {
                    continue;
                }

                AddBox(post, new Vector3(0f, 0.9f, 0f), new Vector3(0.2f, 1.8f, 0.2f));
                post.name = $"Sign_{info.Id}_{(end == 0 ? "start" : "end")}";
                string target = end == 0 ? info.EndName : info.StartName;
                TMP_Text label = CreateText(parent, $"SignLabel_{info.Id}_{(end == 0 ? "start" : "end")}", $"<mark=#3A2A20C0 padding=\"16,16,6,6\">{target} → {distance}m</mark>", 3.2f, 7.5f);
                label.transform.SetPositionAndRotation(position + Vector3.up * 2.35f, Quaternion.Euler(0f, Mathf.Atan2(along.x, along.z) * Mathf.Rad2Deg, 0f));
                made++;
            }
        }

        return made;
    }

    // 석호 : 해안 구역이 떠난 자리에 작은 선착장과 작은 배
    private static string BuildLagoonPier(Transform parent, int building)
    {
        GameObject pier = Place(parent, "zone_dock", new Vector3(SeaLine - 3f, 0f, 10f), 90f, new Vector3(1f, 1f, 0.45f), PropCollider.None, building);

        if (pier == null)
        {
            return "✗ 석호 선착장 모델이 없습니다.";
        }

        float length = StylizedModelLibrary.ZoneDockLength;
        float half = StylizedModelLibrary.ZoneDockWidth * 0.5f;
        AddBox(pier, new Vector3(0f, StylizedModelLibrary.ZoneDockDeckHeight * 0.5f, 1.2f + (length - 1.2f) * 0.5f), new Vector3(StylizedModelLibrary.ZoneDockWidth, StylizedModelLibrary.ZoneDockDeckHeight, length - 1.2f));
        AddBox(pier, new Vector3(0f, 0.12f, 0.6f), new Vector3(StylizedModelLibrary.ZoneDockWidth - 0.4f, 0.06f, 1.3f), new Vector3(-14f, 0f, 0f));

        for (int side = -1; side <= 1; side += 2)
        {
            AddBox(pier, new Vector3(side * (half + 0.1f), 0.9f, 3.1f + (length - 3.1f) * 0.5f), new Vector3(0.2f, 1.8f, length - 3.1f));
        }

        AddBox(pier, new Vector3(0f, 0.9f, length + 0.1f), new Vector3(StylizedModelLibrary.ZoneDockWidth + 0.4f, 1.8f, 0.2f));
        pier.name = "LagoonPier";
        Place(parent, "zone_boat", new Vector3(SeaLine + 5f, IslandTerrainBuilder.SeaLevel + 0.02f, 4.5f), 15f, 0.8f, PropCollider.None, 0).name = "LagoonBoat";
        return "석호 : 작은 선착장 · 작은 배 (해안 구역은 동쪽 해변으로)";
    }

    private static void ValidateIslandSites(GameObject root, Action<string> error, StringBuilder report)
    {
        Terrain terrain = ActiveTerrain;
        float Ground(float x, float z) => terrain.SampleHeight(new Vector3(x, 0f, z)) + terrain.transform.position.y;
        float worstPad = 0f;
        string worstPadZone = string.Empty;

        foreach (ZoneInfo zone in Zones)
        {
            IslandZoneLayout.Site site = IslandZoneLayout.Get(zone.Id);
            Transform group = root.transform.Find(zone.ObjectName);

            if (site == null)
            {
                error($"{zone.DisplayName} 구역의 섬 자리가 정해지지 않았습니다 (IslandZoneLayout).");
                continue;
            }

            if (group == null || Vector2.Distance(new Vector2(group.position.x, group.position.z), site.Offset) > 0.01f)
            {
                error($"{zone.DisplayName} 구역이 섬 자리로 옮겨지지 않았습니다. 18번(22번) 메뉴를 실행하세요.");
            }

            // 터 가운데 (반지름 60%)는 평평해야 함
            if (!site.Coast)
            {
                float pad = IslandZoneLayout.PadHeight(zone.Id);
                Vector2 center = site.PadCenter + site.Offset;

                for (float u = -0.6f; u <= 0.6f; u += 0.2f)
                {
                    for (float v = -0.6f; v <= 0.6f; v += 0.2f)
                    {
                        if (u * u + v * v > 0.36f)
                        {
                            continue;
                        }

                        float delta = Mathf.Abs(Ground(center.x + u * site.PadRadius.x, center.y + v * site.PadRadius.y) - pad);

                        if (delta > worstPad)
                        {
                            worstPad = delta;
                            worstPadZone = zone.DisplayName;
                        }
                    }
                }
            }

            // 섬 안쪽 (해안 구역은 물가)
            Vector3 world = ZoneCenterWorld(zone);
            float beyond = new Vector2(world.x, world.z).magnitude - IslandTerrainBuilder.CoastRadiusAt(Mathf.Atan2(world.z, world.x));

            if (!site.Coast && beyond > -80f)
            {
                error($"{zone.DisplayName} 구역이 바닷가에 너무 가깝습니다 (해안선까지 {-beyond:0}m).");
            }
        }

        if (worstPad > 0.35f)
        {
            error($"{worstPadZone} 구역 터가 평평하지 않습니다 (차이 {worstPad:0.00}m). 22번 메뉴를 실행하세요.");
        }

        // 흙길 : 가장 가파른 곳
        float steepest = 0f;
        string steepestRoad = string.Empty;
        float totalLength = 0f;

        for (int road = 0; road < IslandZoneLayout.RoadCount; road++)
        {
            float length = IslandZoneLayout.RoadLength(road);
            totalLength += length;
            Vector3 previous = IslandZoneLayout.RoadPoint(road, 0f);
            float previousHeight = Ground(previous.x, previous.z);

            for (float distance = 4f; distance <= length; distance += 4f)
            {
                Vector3 point = IslandZoneLayout.RoadPoint(road, distance);
                float height = Ground(point.x, point.z);
                float grade = Mathf.Atan2(Mathf.Abs(height - previousHeight), 4f) * Mathf.Rad2Deg;

                if (grade > steepest)
                {
                    steepest = grade;
                    steepestRoad = IslandZoneLayout.Roads[road].Id;
                }

                previousHeight = height;
            }
        }

        if (steepest > 15f)
        {
            error($"{steepestRoad} 흙길이 {steepest:0}°로 가파릅니다 (15° 이하 필요). 22번 메뉴를 실행하세요.");
        }

        if (root.transform.Find("LagoonPier/LagoonPier") == null)
        {
            error("석호 선착장이 없습니다. 18번 메뉴를 실행하세요.");
        }

        report.AppendLine($"섬 구역 자리 {Zones.Length}곳 · 터 평평함 (가장 큰 차이 {worstPad:0.00}m) · 흙길 {IslandZoneLayout.RoadCount}개 {totalLength:0}m (가장 가파른 곳 {steepest:0}°)");
    }

    // ---------------------------------------------------------------- 검증

    private const string WreckLabel = "난파선 해변"; // 108일차 지도 이름
    private const string LagoonLabel = "석호";

    public static IEnumerable<string> CollectTexts()
    {
        yield return WreckLabel;
        yield return LagoonLabel;

        foreach (IslandZoneLayout.Road road in IslandZoneLayout.Roads.Where(item => item.Kind != IslandZoneLayout.RoadKind.Main)) // 108일차 길망 표지판
        {
            yield return road.StartName + " →";
            yield return road.EndName + " →";
        }

        foreach (ZoneInfo zone in Zones)
        {
            yield return zone.DisplayName + " →";
        }

        yield return "마을";
    }

    public static string Validate(out int errorCount)
    {
        StringBuilder report = new StringBuilder("[새 구역 · 특수 체형 검증]\n");
        int errors = 0;

        void Error(string message)
        {
            errors++;
            report.AppendLine("✗ " + message);
        }

        // 1. NPC 모델 (35명)
        NpcDatabase database = AssetDatabase.LoadAssetAtPath<NpcDatabase>(NpcContentBuilder.DatabasePath);
        Dictionary<StylizedModelLibrary.NpcBody, int> bodies = new Dictionary<StylizedModelLibrary.NpcBody, int>();
        int models = 0;
        int waterBound = 0;

        if (database == null)
        {
            Error("NpcDatabase가 없습니다.");
        }
        else
        {
            LowPolyMeshBuilder builder = new LowPolyMeshBuilder();

            foreach (NpcCharacterData character in database.Characters.Where(item => item != null))
            {
                string modelId = StylizedModelLibrary.GetNpcModelId(character.CharacterId);

                if (!StylizedModelLibrary.TryGetNpcLook(modelId, out StylizedModelLibrary.NpcLook look))
                {
                    Error($"{character.CharacterId} : 모양 값(모델 도감)이 없습니다 ({modelId}).");
                    continue;
                }

                models++;
                bodies[look.Body] = bodies.TryGetValue(look.Body, out int used) ? used + 1 : 1;

                if (StylizedArtAssetFactory.LoadModelPrefab(modelId) == null)
                {
                    Error($"{character.CharacterId} : 모델 Prefab이 없습니다. 18번 메뉴를 실행하세요.");
                }

                if (!NpcPlacementBuilder.HasHairColor(character.CharacterId))
                {
                    Error($"{character.CharacterId} : 머리 색이 정해지지 않았습니다.");
                }

                StylizedModelLibrary.TryBuild(modelId, builder);
                Bounds bounds = builder.CalculateBounds();

                if (bounds.size.y < 1.0f || bounds.size.y > 3.2f || bounds.size.x > 3f || bounds.size.z > 3.4f || bounds.min.y < -0.02f)
                {
                    Error($"{character.CharacterId} : 모델 크기가 범위를 벗어났습니다 (높이 {bounds.size.y:0.00} · 폭 {bounds.size.x:0.00} · 길이 {bounds.size.z:0.00} · 바닥 {bounds.min.y:0.00}).");
                }

                NpcBodyMetrics metrics = NpcBodyRules.Get(look);

                if (metrics.AgentRadius > 0.6f || metrics.Height < 1.2f || metrics.Height > 3f)
                {
                    Error($"{character.CharacterId} : 길찾기 크기가 범위를 벗어났습니다 (반지름 {metrics.AgentRadius:0.00} · 높이 {metrics.Height:0.00}).");
                }

                if (metrics.WaterBound)
                {
                    waterBound++;
                    ValidateWaterside(character, database, Error);
                }
            }

            report.AppendLine($"NPC 모델 {models}/{database.Characters.Count}명 · 하반신 {string.Join(" · ", bodies.OrderBy(pair => pair.Key).Select(pair => $"{BodyName(pair.Key)} {pair.Value}"))} · 물가 NPC {waterBound}명");

            // 새 구역 위치 데이터
            foreach (ZoneInfo zone in Zones)
            {
                if (!database.Locations.Any(location => location.ZoneId == zone.Id))
                {
                    Error($"{zone.DisplayName} 구역에 NPC 위치가 없습니다 (NpcLocations.csv).");
                }
            }

            foreach (NpcDatabase.Location location in database.Locations.Where(item => !item.IsVillage))
            {
                if (!Zones.Any(zone => zone.Id == location.ZoneId))
                {
                    Error($"{location.LocationId} : 알 수 없는 구역입니다 ({location.ZoneId}).");
                }

                if (!PointLayout.ContainsKey(location.LocationId))
                {
                    Error($"{location.LocationId} : 새 구역 배치 정보가 없습니다 (WorldZoneBuilder.PointLayout).");
                }
            }
        }

        int zoneModels = StylizedModelLibrary.Catalog.Keys.Count(id => id.StartsWith("zone_", StringComparison.Ordinal));
        int missingZoneModels = StylizedModelLibrary.Catalog.Keys.Count(id => id.StartsWith("zone_", StringComparison.Ordinal) && StylizedArtAssetFactory.LoadModelPrefab(id) == null);

        if (missingZoneModels > 0)
        {
            Error($"구역 소품 모델 Prefab {missingZoneModels}개가 없습니다. 18번 메뉴를 실행하세요.");
        }

        report.AppendLine($"구역 소품 모델 {zoneModels - missingZoneModels}/{zoneModels}종");
        KoreanFontBuilder.Validate(CollectTexts(), Error, new StringBuilder());

        // 2. 게임 Scene
        Scene scene = EditorSceneManager.GetActiveScene();

        if (scene.path != ScenePath)
        {
            report.AppendLine("Scene 검사 생략 (게임 Scene이 열려 있지 않음)");
        }
        else if (database != null)
        {
            ValidateScene(scene, database, Error, report);
        }

        errorCount = errors;
        report.AppendLine(errors == 0 ? "결과 : 오류 0개" : $"결과 : 오류 {errors}개");
        return report.ToString();
    }

    private static string BodyName(StylizedModelLibrary.NpcBody body)
    {
        switch (body)
        {
            case StylizedModelLibrary.NpcBody.SnakeTail: return "뱀 꼬리";
            case StylizedModelLibrary.NpcBody.SpiderLegs: return "거미 다리";
            case StylizedModelLibrary.NpcBody.HorseBody: return "말 몸";
            case StylizedModelLibrary.NpcBody.FishTail: return "물고기 꼬리";
            case StylizedModelLibrary.NpcBody.Tentacles: return "촉수";
            case StylizedModelLibrary.NpcBody.ScorpionBody: return "전갈 몸";
            case StylizedModelLibrary.NpcBody.Floating: return "떠 있음";
            case StylizedModelLibrary.NpcBody.SlimeBase: return "슬라임";
            case StylizedModelLibrary.NpcBody.MimicChest: return "보물상자";
            default: return "사람 다리";
        }
    }

    // 물가 NPC(인어 · 크라켄 · 상어족)의 집 · 일터 · 일정 위치는 모두 물가여야 한다
    private static void ValidateWaterside(NpcCharacterData character, NpcDatabase database, Action<string> error)
    {
        IEnumerable<string> locations = new[] { character.HomeLocationId, character.WorkLocationId };

        if (character.Schedule != null)
        {
            locations = locations.Concat(character.Schedule.Plans.SelectMany(plan => plan.Stops).Select(stop => stop.LocationId));
        }

        foreach (string locationId in locations.Where(id => !string.IsNullOrEmpty(id)).Distinct())
        {
            NpcDatabase.Location location = database.GetLocation(locationId);

            if (location != null && !location.IsWaterside)
            {
                error($"{character.CharacterId} : 물가 NPC인데 물가가 아닌 위치가 있습니다 ({locationId}).");
            }
        }
    }

    private static float DistanceToWater(Vector3 position, string zoneId)
    {
        if (islandLayout)
        {
            position = IslandZoneLayout.ToDesign(zoneId, position); // 107일차: 설계 좌표로 비교
        }

        float best = Mathf.Max(0f, SeaLine - position.x);

        foreach ((string _, Vector3 center, float radius) in Pools)
        {
            best = Mathf.Min(best, Mathf.Max(0f, Flat(position, center) - radius));
        }

        return best;
    }

    private static void ValidateScene(Scene scene, NpcDatabase database, Action<string> error, StringBuilder report)
    {
        DetectIsland();
        GameObject root = scene.GetRootGameObjects().FirstOrDefault(item => item.name == RootName);

        if (root == null)
        {
            error("게임 Scene에 새 구역이 없습니다. Build Content > 18. World Zones를 실행하세요.");
            return;
        }

        foreach (ZoneInfo zone in Zones)
        {
            Transform group = root.transform.Find(zone.ObjectName);

            if (group == null || group.childCount < 5)
            {
                error($"{zone.DisplayName} 구역 소품이 없습니다.");
            }
        }

        Physics.SyncTransforms();
        int building = LayerMask.GetMask(BuildingLayerName);
        NpcManager manager = Object.FindFirstObjectByType<NpcManager>(FindObjectsInactive.Include);
        int good = 0;
        List<NpcDatabase.Location> zoneLocations = database.Locations.Where(item => !item.IsVillage).ToList();

        foreach (NpcDatabase.Location location in zoneLocations)
        {
            NpcLocationPoint point = root.GetComponentsInChildren<NpcLocationPoint>(true).FirstOrDefault(item => item.LocationId == location.LocationId);

            if (point == null)
            {
                error($"새 구역 위치가 Scene에 없습니다: {location.LocationId}");
                continue;
            }

            Vector3 position = point.transform.position;

            if (!NavMesh.SamplePosition(position, out NavMeshHit _, 2f, NavMesh.AllAreas))
            {
                error($"새 구역 위치가 걸을 수 있는 곳(NavMesh) 밖입니다: {location.LocationId} {position}");
                continue;
            }

            if (Physics.CheckSphere(position + Vector3.up * 0.9f, 0.3f, building, QueryTriggerInteraction.Ignore))
            {
                error($"새 구역 위치가 건물 · 소품 안에 있습니다: {location.LocationId}");
                continue;
            }

            if (location.IsWaterside && DistanceToWater(position, location.ZoneId) > 3.5f)
            {
                error($"물가 위치인데 물에서 {DistanceToWater(position, location.ZoneId):0.0}m 떨어져 있습니다: {location.LocationId}");
                continue;
            }

            if (manager != null && !manager.Locations.Contains(point))
            {
                error($"NPC 관리자에 새 구역 위치가 연결되지 않았습니다: {location.LocationId}");
                continue;
            }

            good++;
        }

        // 흙길 · 입구가 걸을 수 있는지, 바다는 걸을 수 없는지
        int reachable = 0;

        foreach (ZoneInfo zone in Zones)
        {
            NavMeshPath path = new NavMeshPath();
            bool fromVillage = NavMesh.SamplePosition(zone.PathStart, out NavMeshHit start, 3f, NavMesh.AllAreas);
            bool toZone = NavMesh.SamplePosition(ZoneEntranceWorld(zone), out NavMeshHit end, 3f, NavMesh.AllAreas); // 107일차: 옮긴 입구

            if (!fromVillage || !toZone || !NavMesh.CalculatePath(start.position, end.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete)
            {
                error($"마을에서 {zone.DisplayName} 입구까지 걸어갈 수 없습니다 (NavMesh 길 없음).");
                continue;
            }

            reachable++;
        }

        bool island = IslandTerrainBuilder.IsIslandTerrain(Terrain.activeTerrain != null ? Terrain.activeTerrain : Object.FindFirstObjectByType<Terrain>());
        Vector2 lagoonCenter = (IslandTerrainBuilder.LagoonMin + IslandTerrainBuilder.LagoonMax) * 0.5f;
        Vector3 seaSample = island ? new Vector3(lagoonCenter.x, IslandTerrainBuilder.SeaLevel, lagoonCenter.y - 20f) : new Vector3(113f, 0f, 60f); // 105일차: 무인도는 석호

        if (NavMesh.SamplePosition(seaSample, out NavMeshHit sea, 1f, NavMesh.AllAreas))
        {
            error($"바다가 걸을 수 있는 곳으로 되어 있습니다 ({sea.position}). 18번 메뉴로 NavMesh를 다시 구우세요.");
        }

        Transform boundary = root.transform.Find("Boundary");

        if (island) // 105일차: 무인도는 가장자리 벽 대신 바다 경계(ShoreGuard) · 석호 NavMesh 제외
        {
            if (boundary == null || boundary.Find("Lagoon_NotWalkable") == null)
            {
                error("석호 NavMesh 제외 영역이 없습니다. 18번 메뉴를 실행하세요.");
            }
        }
        else
        {
            int walls = boundary != null ? boundary.GetComponentsInChildren<BoxCollider>().Length : 0;

            if (walls < 6)
            {
                error($"섬 경계 · 바다 벽이 {walls}/6개입니다.");
            }
        }

        Terrain terrain = Terrain.activeTerrain != null ? Terrain.activeTerrain : Object.FindFirstObjectByType<Terrain>();
        int zoneLayers = terrain != null && terrain.terrainData != null ? terrain.terrainData.terrainLayers.Count(layer => layer != null && layer.name.StartsWith("TL_Zone", StringComparison.Ordinal)) : 0;

        if (zoneLayers != ZoneLayerNames.Length)
        {
            error($"Terrain 구역 바닥 층이 {zoneLayers}/{ZoneLayerNames.Length}개입니다.");
        }

        int labelLayer = LayerMask.NameToLayer(MapLabelLayerName);
        Camera main = Camera.main != null ? Camera.main : Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(camera => camera.CompareTag("MainCamera"));

        if (labelLayer < 0)
        {
            error("지도 이름 레이어(MapLabel)가 없습니다.");
        }
        else if (main != null && (main.cullingMask & (1 << labelLayer)) != 0)
        {
            error("게임 카메라가 지도 이름을 그립니다 (MapLabel 레이어를 빼야 합니다).");
        }

        EnemySpawnPoint[] spawns = Object.FindObjectsByType<EnemySpawnPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach ((string id, string _, string _, Vector3 _) in ZoneSpawns)
        {
            if (!spawns.Any(spawn => spawn.SpawnPointId == id))
            {
                error($"새 구역 적 생성 지점이 없습니다: {id}");
            }
        }

        if (spawns.Select(spawn => spawn.SpawnPointId).Distinct().Count() != spawns.Length)
        {
            error("적 생성 지점 ID가 겹칩니다.");
        }

        report.AppendLine($"새 구역 위치 {good}/{zoneLocations.Count}곳 정상 · 마을→입구 길 {reachable}/{Zones.Length}개 · 경계 {(island ? "바다 경계(ShoreGuard) · 석호 제외" : "벽")} · 바닥 층 {zoneLayers}개 · 적 생성 지점 {spawns.Length}곳");

        if (island)
        {
            ValidateIslandSites(root, error, report); // 107일차
        }
    }
}
