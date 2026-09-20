using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using TMPro;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// 116일차: 동굴
// 1. 섬 옆 빈 곳(CaveOrigin)에 고정 동굴 맵을 짓는다 : 입구방 12 + 넓은 방 4 + 가운데 큰 방 1 + 잇는 굴 28
// 2. 섬 곳곳 12곳에 지상 입구(굴 6 · 바위 틈 4 · 무너진 구덩이 2)를 세우고, 동굴 안 같은 자리와 짝을 지어 드나들게 한다
// 3. 동굴 전용 NavMesh를 따로 굽고, 광석 · 수정 · 석순을 놓는다
// 4. 횃불 아이템(맨손 제작)과 손에 들면 켜지는 불빛을 넣는다
// 자동 적용(ProjectUAutoContent)이 실행한다. 여러 번 실행해도 같은 결과가 나온다.
public static class CaveBuilder
{
    public const string RootName = "=== Caves ===";
    public const string AreaId = "cave_underdeep"; // 동굴 구역 ID (저장)
    public const string TorchItemId = "item_torch";
    public static readonly Vector3 CaveOrigin = new Vector3(2600f, 40f, 0f); // 섬 밖 · 바다 수면 위 (헤엄 판정 · 섬 경계와 겹치지 않게)

    private const string ScenePath = "Assets/_ProjectU/Scenes/20_Gameplay.unity";
    private const string StoneResourcePath = "Assets/_ProjectU/Prefabs/Gathering/StoneResource_01.prefab";
    private const string IronOreItemPath = "Assets/_ProjectU/Data/Items/Day71/ItemData_IronOre.asset";
    private const string WoodItemPath = "Assets/_ProjectU/Data/Items/ItemData_Wood.asset";
    private const string FiberItemPath = "Assets/_ProjectU/Data/Items/Day71/ItemData_PlantFiber.asset";
    private const string StoneItemPath = "Assets/_ProjectU/Data/Items/ItemData_Stone.asset";
    private const string ItemFolder = "Assets/_ProjectU/Data/Items/Day116";
    private const string TorchItemPath = ItemFolder + "/ItemData_Torch.asset";
    private const string CraftingFolder = "Assets/_ProjectU/Data/Crafting/Day116";
    private const string TorchRecipePath = CraftingFolder + "/CraftingRecipe_Torch.asset";
    private const string PickupFolder = "Assets/_ProjectU/Prefabs/Items/Day116";
    private const string TorchPickupPath = PickupFolder + "/TorchPickup.prefab";
    private const string PickupTemplatePath = "Assets/_ProjectU/Prefabs/Items/Day71/WildMushroomPickup.prefab";
    private const string ItemDatabasePath = "Assets/_ProjectU/Data/Databases/ItemDatabase.asset";
    private const string PickupRegistryPath = "Assets/_ProjectU/Prefabs/Items/Day75/WorldItemPickupRegistry_Day75.asset";
    private const string HeldVisualsPath = "Assets/_ProjectU/Data/Items/HeldItemVisuals.asset";
    private const string InteractableLayer = "Interactable";
    private const string ShelterLayer = "WeatherShelter";
    public const string CaveNavMeshName = "CaveNavMesh"; // 섬 NavMesh와 구분하는 이름
    public const string CaveLayerName = "Cave"; // 동굴 바닥 · 벽 · 소품 (동굴 안 지도에서 이 레이어만 그림)

    // ---------------------------------------------------------------- 고정 맵 설계

    public enum PortalKind
    {
        Arch, // 바위 아치 굴
        Crack, // 바위 틈
        Sinkhole // 무너진 구덩이
    }

    public sealed class EntranceSpec
    {
        public string Id; // 입구 ID (지상 · 동굴 안 짝)
        public string Name; // 표시 이름
        public PortalKind Kind; // 생김새
        public string RoadId; // 어느 흙길 옆인지
        public float Along; // 흙길 시작에서의 거리 (음수면 끝에서부터)
        public float Side; // 길 오른쪽(+) · 왼쪽(-) 으로 떨어진 거리
    }

    // 섬 곳곳 12곳 (큰길 6 · 둘레길 · 갈림길 6)
    public static readonly EntranceSpec[] Entrances =
    {
        new EntranceSpec { Id = "cave_snow", Name = "설산 광굴", Kind = PortalKind.Arch, RoadId = "snow", Along = -70f, Side = -17f },
        new EntranceSpec { Id = "cave_ruins", Name = "폐허 지하굴", Kind = PortalKind.Arch, RoadId = "ruins", Along = -60f, Side = 18f },
        new EntranceSpec { Id = "cave_forest", Name = "숲속 굴", Kind = PortalKind.Arch, RoadId = "forest", Along = -75f, Side = -19f },
        new EntranceSpec { Id = "cave_desert", Name = "사막 모래굴", Kind = PortalKind.Arch, RoadId = "desert", Along = -65f, Side = 18f },
        new EntranceSpec { Id = "cave_swamp", Name = "습지 굴", Kind = PortalKind.Arch, RoadId = "swamp", Along = -60f, Side = -17f },
        new EntranceSpec { Id = "cave_coast", Name = "해안 물굴", Kind = PortalKind.Arch, RoadId = "coast", Along = -55f, Side = 19f },
        new EntranceSpec { Id = "cave_west_beach", Name = "서쪽 해변 바위 틈", Kind = PortalKind.Crack, RoadId = "west_beach", Along = -45f, Side = -14f },
        new EntranceSpec { Id = "cave_south_trail", Name = "남쪽 오솔길 바위 틈", Kind = PortalKind.Crack, RoadId = "desert_trail", Along = 150f, Side = 15f },
        new EntranceSpec { Id = "cave_east_shore", Name = "동쪽 언덕 바위 틈", Kind = PortalKind.Crack, RoadId = "coast", Along = -200f, Side = 26f },
        new EntranceSpec { Id = "cave_village_hill", Name = "마을 뒷산 바위 틈", Kind = PortalKind.Crack, RoadId = "snow", Along = 95f, Side = 21f },
        new EntranceSpec { Id = "cave_north_ring", Name = "북쪽 둘레길 구덩이", Kind = PortalKind.Sinkhole, RoadId = "snow_coast", Along = 220f, Side = -16f },
        new EntranceSpec { Id = "cave_west_ring", Name = "서쪽 둘레길 구덩이", Kind = PortalKind.Sinkhole, RoadId = "forest_ruins", Along = 110f, Side = 16f }
    };

    public const float HillHeight = 13f; // 입구 뒤 절벽 높이 (m)
    public const float HillRise = 9f; // 이 거리 안에서 절벽이 솟는다 (m, 가팔라서 올라갈 수 없음)
    public const float HillWidth = 13f; // 절벽이 가장 높은 폭 (m)
    public const float HillFade = 30f; // 이 거리에서 원래 땅과 만난다 (m)
    public const float MouthWidth = 5.5f; // 입구 앞 평평한 통로 폭 (m)
    public const float MouthDepth = 5f; // 입구 앞 평평한 통로 깊이 (m, 이보다 뒤는 절벽)

    private const int RoomSides = 18; // 방 둘레 조각 수
    private const float ChamberRadius = 8.5f; // 입구방 반지름
    private const float ChamberHeight = 6.5f; // 입구방 높이
    private const float HallRadius = 14f; // 넓은 방
    private const float HallHeight = 9f;
    private const float GreatRadius = 19f; // 가운데 큰 방
    private const float GreatHeight = 12f;
    private const float ChamberRing = 205f; // 입구방이 놓이는 반지름
    private const float HallRing = 92f; // 넓은 방이 놓이는 반지름
    private const float TunnelWidth = 5f; // 굴 너비
    private const float TunnelHeight = 4.4f; // 굴 높이
    public const float DeepDepth = 18f; // 117일차: 깊은층 깊이 (m)
    private const float DeepRing = 62f; // 깊은층 방이 놓이는 반지름
    private const float DeepRadius = 15f; // 깊은층 방 반지름
    private const float DeepHeight = 8.5f; // 깊은층 방 높이
    private const float DeepGreatRadius = 20f; // 깊은층 가운데 방

    private sealed class Room
    {
        public string Id;
        public Vector3 Center; // 동굴 맵 기준 위치 (y = 0 바닥)
        public float Radius;
        public float Height;
        public int Layer = 1; // 117일차: 1 입구층 · 2 깊은층
        public readonly List<float> Openings = new List<float>(); // 굴이 뚫린 방향 (도)
    }

    private sealed class Tunnel
    {
        public string Id;
        public Room From;
        public Room To;
    }

    // ---------------------------------------------------------------- 만들기

    public static string BuildAll(bool saveScene)
    {
        StringBuilder report = new StringBuilder("[동굴 만들기]\n");

        surfaceCache.Clear();
        forwardCache.Clear();

        try
        {
            EditorUtility.DisplayProgressBar("Project U 동굴", "동굴 모델", 0.05f);
            report.AppendLine($"동굴 모델 {EnsureModels()}종 준비");

            EditorUtility.DisplayProgressBar("Project U 동굴", "횃불 아이템", 0.15f);
            report.AppendLine(BuildTorchItem());

            Scene scene = EditorSceneManager.GetActiveScene();

            if (scene.path != ScenePath)
            {
                report.AppendLine($"✗ 게임 Scene({ScenePath})을 열고 다시 실행하세요. 모델 · 아이템만 만들었습니다.");
                return report.ToString();
            }

            Terrain terrain = Terrain.activeTerrain != null ? Terrain.activeTerrain : Object.FindFirstObjectByType<Terrain>();

            if (terrain == null)
            {
                report.AppendLine("✗ 섬 Terrain이 없습니다.");
                return report.ToString();
            }

            GameObject root = scene.GetRootGameObjects().FirstOrDefault(item => item.name == RootName) ?? new GameObject(RootName);
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            foreach (Transform child in root.transform.Cast<Transform>().ToList())
            {
                Object.DestroyImmediate(child.gameObject); // 여러 번 실행해도 같도록 지우고 다시
            }

            EditorUtility.DisplayProgressBar("Project U 동굴", "입구 절벽 (땅 올리기)", 0.25f);
            List<Room> rooms0 = null; // 입구 자리를 먼저 정해 둔다
            List<Tunnel> tunnels0 = null;
            Dictionary<string, Room> chamberByEntrance0 = null;
            rooms0 = LayoutRooms(terrain, out tunnels0, out chamberByEntrance0);
            report.AppendLine(ShapeEntranceCliffs(terrain));

            EditorUtility.DisplayProgressBar("Project U 동굴", "동굴 맵", 0.35f);
            List<Room> rooms = rooms0;
            List<Tunnel> tunnels = tunnels0;
            Dictionary<string, Room> chamberByEntrance = chamberByEntrance0;
            Transform mapGroup = new GameObject("CaveMap").transform;
            mapGroup.SetParent(root.transform, false);
            mapGroup.position = CaveOrigin;
            int meshes = BuildCaveMeshes(mapGroup, rooms, tunnels);
            report.AppendLine($"동굴 맵 : 방 {rooms.Count}개 · 잇는 굴 {tunnels.Count}개 · 모양 {meshes}개 (섬 밖 {CaveOrigin.x:0}m 지점)");

            EditorUtility.DisplayProgressBar("Project U 동굴", "동굴 소품 · 광석", 0.55f);
            report.AppendLine(DecorateCave(mapGroup, rooms, tunnels));

            EditorUtility.DisplayProgressBar("Project U 동굴", "지상 입구", 0.7f);
            report.AppendLine(BuildPortals(root.transform, mapGroup, terrain, chamberByEntrance));

            EditorUtility.DisplayProgressBar("Project U 동굴", "동굴 길(NavMesh)", 0.85f);
            report.AppendLine(BuildCaveNavMesh(root.transform, rooms));
            report.AppendLine(WorldZoneBuilder.RebakeNavMesh()); // 절벽으로 바뀐 섬 길 다시 굽기
            report.AppendLine(WireScene(scene, root.transform));
            EditorSceneManager.MarkSceneDirty(scene);

            if (saveScene)
            {
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                report.AppendLine("게임 Scene 저장 완료");
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
        }

        report.Append(Validate(out _));
        return report.ToString();
    }

    private static int EnsureModels()
    {
        string[] ids = { "cave_entrance_arch", "cave_crack", "cave_sinkhole", "cave_exit_ladder", "cave_cliff", "cave_cliff_small", "cave_pillar", "cave_stalagmite", "cave_stalactite", "cave_crystal", "cave_ore_vein", "cave_rubble", "item_torch" };
        return ids.Count(id => StylizedArtAssetFactory.GetOrCreateModelPrefab(id, true) != null);
    }

    // ---------------------------------------------------------------- 동굴 맵 설계 · 모양

    private static List<Room> LayoutRooms(Terrain terrain, out List<Tunnel> tunnels, out Dictionary<string, Room> chamberByEntrance)
    {
        List<Room> rooms = new List<Room>();
        tunnels = new List<Tunnel>();
        chamberByEntrance = new Dictionary<string, Room>(StringComparer.Ordinal);

        // 입구방은 지상 입구가 있는 방향과 같은 순서로 둘러 놓는다 (섬에서 북쪽 입구는 동굴에서도 북쪽)
        List<(EntranceSpec spec, float angle)> sorted = Entrances
            .Select(spec => (spec, angle: Mathf.Atan2(SurfacePoint(terrain, spec).x, SurfacePoint(terrain, spec).z) * Mathf.Rad2Deg))
            .OrderBy(item => item.angle)
            .ToList();

        Room great = new Room { Id = "great_hall", Center = Vector3.zero, Radius = GreatRadius, Height = GreatHeight };
        rooms.Add(great);
        Room[] halls = new Room[4];

        for (int index = 0; index < 4; index++)
        {
            float angle = 45f + index * 90f;
            halls[index] = new Room { Id = $"hall_{index}", Center = Direction(angle) * HallRing, Radius = HallRadius, Height = HallHeight };
            rooms.Add(halls[index]);
            tunnels.Add(new Tunnel { Id = $"tunnel_hall_{index}", From = halls[index], To = great });
        }

        Room[] chambers = new Room[sorted.Count];

        for (int index = 0; index < sorted.Count; index++)
        {
            float angle = index * (360f / sorted.Count);
            Room chamber = new Room { Id = $"chamber_{sorted[index].spec.Id}", Center = Direction(angle) * ChamberRing, Radius = ChamberRadius, Height = ChamberHeight };
            chambers[index] = chamber;
            rooms.Add(chamber);
            chamberByEntrance[sorted[index].spec.Id] = chamber;
            Room hall = halls[Mathf.Clamp(Mathf.FloorToInt(Mathf.Repeat(angle + 45f, 360f) / 90f), 0, 3)];
            tunnels.Add(new Tunnel { Id = $"tunnel_{sorted[index].spec.Id}", From = chamber, To = hall });
        }

        for (int index = 0; index < chambers.Length; index++) // 둘레를 잇는 굴 (옆 입구방끼리)
        {
            tunnels.Add(new Tunnel { Id = $"tunnel_ring_{index:00}", From = chambers[index], To = chambers[(index + 1) % chambers.Length] });
        }

        // 117일차: 깊은층 (가운데 방에서 내려가는 비탈길 + 갈래 방 4)
        Room deepGreat = new Room { Id = "deep_hall", Center = new Vector3(0f, -DeepDepth, 0f), Radius = DeepGreatRadius, Height = DeepHeight + 2f, Layer = 2 };
        rooms.Add(deepGreat);
        tunnels.Add(new Tunnel { Id = "tunnel_deep_ramp", From = great, To = deepGreat });

        for (int index = 0; index < 4; index++)
        {
            float angle = index * 90f + 45f;
            Room deep = new Room { Id = $"deep_{index}", Center = Direction(angle) * DeepRing + Vector3.down * DeepDepth, Radius = DeepRadius, Height = DeepHeight, Layer = 2 };
            rooms.Add(deep);
            tunnels.Add(new Tunnel { Id = $"tunnel_deep_{index}", From = deepGreat, To = deep });
        }

        foreach (Tunnel tunnel in tunnels) // 굴이 닿는 방향에 구멍을 뚫는다
        {
            Vector3 direction = tunnel.To.Center - tunnel.From.Center;
            float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            tunnel.From.Openings.Add(angle);
            tunnel.To.Openings.Add(angle + 180f);
        }

        return rooms;
    }

    private static Vector3 Direction(float degrees) => new Vector3(Mathf.Sin(degrees * Mathf.Deg2Rad), 0f, Mathf.Cos(degrees * Mathf.Deg2Rad));

    private static int BuildCaveMeshes(Transform parent, List<Room> rooms, List<Tunnel> tunnels)
    {
        int made = 0;

        foreach (Room room in rooms)
        {
            made += BuildRoomMesh(parent, room) ? 1 : 0;
        }

        foreach (Tunnel tunnel in tunnels)
        {
            made += BuildTunnelMesh(parent, tunnel) ? 1 : 0;
        }

        return made;
    }

    private static bool BuildRoomMesh(Transform parent, Room room)
    {
        LowPolyMeshBuilder floorWalls = new LowPolyMeshBuilder();
        LowPolyMeshBuilder roof = new LowPolyMeshBuilder();
        int seed = Mathf.Abs(room.Id.GetHashCode());
        Vector3[] floor = new Vector3[RoomSides];
        Vector3[] ceiling = new Vector3[RoomSides];

        for (int index = 0; index < RoomSides; index++)
        {
            float angle = index * 360f / RoomSides;
            float wobble = 1f + (Mathf.PerlinNoise(seed * 0.37f + index * 0.31f, 3.1f) - 0.5f) * 0.22f;
            Vector3 direction = Direction(angle);
            floor[index] = direction * (room.Radius * wobble);
            ceiling[index] = direction * (room.Radius * wobble * 0.86f) + Vector3.up * (room.Height * (0.88f + wobble * 0.12f));
        }

        // 굴이 지나갈 구멍 : 굴 너비만큼, 그리고 적어도 벽 조각 하나는 확실히 비운다
        float openingHalf = Mathf.Max(Mathf.Asin(Mathf.Clamp01((TunnelWidth * 0.5f + 0.7f) / room.Radius)) * Mathf.Rad2Deg, 360f / RoomSides * 0.8f);

        for (int index = 0; index < RoomSides; index++)
        {
            int next = (index + 1) % RoomSides;
            floorWalls.AddTriangle(index % 2 == 0 ? StylizedColor.Dirt : StylizedColor.StoneDark, Vector3.zero, floor[index], floor[next]); // 바닥
            roof.AddTriangle(StylizedColor.StoneDark, Vector3.up * room.Height, ceiling[next], ceiling[index]); // 천장

            float midAngle = (index + 0.5f) * 360f / RoomSides;
            bool opening = room.Openings.Any(angle => Mathf.Abs(Mathf.DeltaAngle(midAngle, angle)) < openingHalf);

            if (!opening) // 굴이 뚫린 곳은 벽을 세우지 않는다
            {
                floorWalls.AddQuad(index % 3 == 0 ? StylizedColor.Stone : StylizedColor.StoneDark, floor[next], floor[index], ceiling[index], ceiling[next]);
            }
        }

        foreach (float angle in room.Openings) // 굴 입구 양옆 · 위 메우기 (하늘이 보이지 않게)
        {
            BuildOpeningCollar(floorWalls, room, angle, openingHalf, floor, ceiling);
        }

        CreateMeshObject(parent, $"Room_{room.Id}", room.Center, floorWalls, false);
        CreateMeshObject(parent, $"Roof_{room.Id}", room.Center, roof, true);
        return true;
    }

    // 방 벽의 구멍(조각 단위)과 굴 입구(너비 5m) 사이 빈틈을 막는다
    private static void BuildOpeningCollar(LowPolyMeshBuilder builder, Room room, float angle, float openingHalf, Vector3[] floor, Vector3[] ceiling)
    {
        Vector3 direction = Direction(angle);
        Vector3 right = Vector3.Cross(Vector3.up, direction);
        Vector3 mouth = direction * (room.Radius * 0.82f);
        Vector3 mouthLeft = mouth - right * (TunnelWidth * 0.5f);
        Vector3 mouthRight = mouth + right * (TunnelWidth * 0.5f);
        Vector3 mouthLeftTop = mouthLeft + Vector3.up * TunnelHeight;
        Vector3 mouthRightTop = mouthRight + Vector3.up * TunnelHeight;
        int left = -1;
        int rightEdge = -1;

        for (int index = 0; index < RoomSides; index++) // 비워 둔 조각의 양 끝 꼭짓점 찾기
        {
            float midAngle = (index + 0.5f) * 360f / RoomSides;

            if (Mathf.Abs(Mathf.DeltaAngle(midAngle, angle)) >= openingHalf)
            {
                continue;
            }

            if (left < 0)
            {
                left = index;
            }

            rightEdge = (index + 1) % RoomSides;
        }

        if (left < 0 || rightEdge < 0)
        {
            return;
        }

        builder.AddQuad(StylizedColor.StoneDark, floor[left], mouthLeft, mouthLeftTop, ceiling[left]); // 왼쪽 메움
        builder.AddQuad(StylizedColor.StoneDark, mouthRight, floor[rightEdge], ceiling[rightEdge], mouthRightTop); // 오른쪽 메움
        builder.AddQuad(StylizedColor.Stone, mouthLeftTop, mouthRightTop, ceiling[rightEdge], ceiling[left]); // 입구 위 메움
    }

    private static bool BuildTunnelMesh(Transform parent, Tunnel tunnel)
    {
        Vector3 direction = tunnel.To.Center - tunnel.From.Center;
        direction.y = 0f; // 층이 다르면 비탈길이 된다 (방 높이는 아래에서 이어 준다)
        float distance = direction.magnitude;

        if (distance < 1f)
        {
            return false;
        }

        direction /= distance;
        Vector3 right = Vector3.Cross(Vector3.up, direction);
        Vector3 start = tunnel.From.Center + direction * (tunnel.From.Radius * 0.82f);
        start.y = tunnel.From.Center.y;
        Vector3 end = tunnel.To.Center - direction * (tunnel.To.Radius * 0.82f);
        end.y = tunnel.To.Center.y;
        Vector3 center = (start + end) * 0.5f;
        LowPolyMeshBuilder shell = new LowPolyMeshBuilder();
        LowPolyMeshBuilder roof = new LowPolyMeshBuilder();
        float half = TunnelWidth * 0.5f;
        int steps = Mathf.Max(2, Mathf.RoundToInt((end - start).magnitude / 12f));
        Vector3 previousLeft = start - right * half - center;
        Vector3 previousRight = start + right * half - center;

        for (int step = 1; step <= steps; step++)
        {
            float t = step / (float)steps;
            Vector3 point = Vector3.Lerp(start, end, t) - center;
            float wobble = (Mathf.PerlinNoise(t * 3.3f + tunnel.Id.Length, 1.7f) - 0.5f) * 1.4f;
            Vector3 left = point - right * (half + wobble);
            Vector3 rightPoint = point + right * (half - wobble);
            // 면이 굴 안쪽을 보도록 (바닥은 위로, 천장은 아래로, 벽은 가운데로)
            shell.AddQuad(step % 2 == 0 ? StylizedColor.Dirt : StylizedColor.StoneDark, left, rightPoint, previousRight, previousLeft); // 바닥
            shell.AddQuad(StylizedColor.StoneDark, previousLeft + Vector3.up * TunnelHeight, left + Vector3.up * TunnelHeight, left, previousLeft); // 왼쪽 벽
            shell.AddQuad(StylizedColor.Stone, rightPoint + Vector3.up * TunnelHeight, previousRight + Vector3.up * TunnelHeight, previousRight, rightPoint); // 오른쪽 벽
            roof.AddQuad(StylizedColor.StoneDark, previousLeft + Vector3.up * TunnelHeight, previousRight + Vector3.up * TunnelHeight, rightPoint + Vector3.up * TunnelHeight, left + Vector3.up * TunnelHeight); // 천장
            previousLeft = left;
            previousRight = rightPoint;
        }

        CreateMeshObject(parent, $"Tunnel_{tunnel.Id}", center, shell, false);
        CreateMeshObject(parent, $"TunnelRoof_{tunnel.Id}", center, roof, true);
        return true;
    }

    public static int EnsureCaveLayer() // 동굴 전용 레이어 (없으면 빈 칸에 만든다)
    {
        int existing = LayerMask.NameToLayer(CaveLayerName);

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
                layer.stringValue = CaveLayerName;
                tagManager.ApplyModifiedPropertiesWithoutUndo();
                return index;
            }
        }

        return -1;
    }

    private static void SetCaveLayer(GameObject target) // 동굴 안 물건 (천장 제외)
    {
        int layer = EnsureCaveLayer();

        if (layer < 0 || target == null)
        {
            return;
        }

        foreach (Transform child in target.GetComponentsInChildren<Transform>(true))
        {
            child.gameObject.layer = layer;
        }
    }

    private static void CreateMeshObject(Transform parent, string name, Vector3 localPosition, LowPolyMeshBuilder builder, bool shelter)
    {
        Mesh built = builder.BuildMesh($"LP_{name}", out StylizedColor[] colors);
        string path = $"{StylizedArtAssetFactory.MeshFolder}/LP_{name}.asset";
        Mesh asset = AssetDatabase.LoadAssetAtPath<Mesh>(path);

        if (asset == null)
        {
            AssetDatabase.CreateAsset(built, path);
            asset = built;
        }
        else
        {
            EditorUtility.CopySerialized(built, asset);
            asset.name = $"LP_{name}";
            EditorUtility.SetDirty(asset);
            Object.DestroyImmediate(built);
        }

        GameObject holder = new GameObject(name);
        holder.transform.SetParent(parent, false);
        holder.transform.localPosition = localPosition;
        holder.AddComponent<MeshFilter>().sharedMesh = asset;
        MeshRenderer renderer = holder.AddComponent<MeshRenderer>();
        renderer.sharedMaterials = StylizedArtAssetFactory.GetMaterials(colors);
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // 동굴은 햇빛이 없어 그림자가 필요 없음
        renderer.receiveShadows = false;
        holder.AddComponent<MeshCollider>().sharedMesh = asset;

        if (shelter)
        {
            int layer = LayerMask.NameToLayer(ShelterLayer); // 천장 : 비 · 눈을 막아 주는 지붕 (동굴 안 지도에는 그리지 않음)
            holder.layer = layer >= 0 ? layer : holder.layer;
        }
        else
        {
            SetCaveLayer(holder); // 바닥 · 벽 : 동굴 안 지도에 보이는 레이어
        }

        GameObjectUtility.SetStaticEditorFlags(holder, StaticEditorFlags.OccludeeStatic);
    }

    // ---------------------------------------------------------------- 동굴 안 소품 · 광석

    private static string DecorateCave(Transform parent, List<Room> rooms, List<Tunnel> tunnels)
    {
        Transform props = new GameObject("CaveProps").transform;
        props.SetParent(parent, false);
        Transform veins = new GameObject("CaveResources").transform;
        veins.SetParent(parent, false);
        int propCount = 0;
        int lights = 0;
        Dictionary<string, int> veinCounts = new Dictionary<string, int>(StringComparer.Ordinal); // 117일차: 광물별 광맥 수

        foreach (Room room in rooms)
        {
            int seed = Mathf.Abs(room.Id.GetHashCode());
            bool isChamber = room.Radius <= ChamberRadius + 0.1f;
            int decorations = isChamber ? 5 : 9;

            for (int index = 0; index < decorations; index++)
            {
                float angle = Hash(seed, index, 1) * 360f;
                float distance = Mathf.Lerp(room.Radius * 0.35f, room.Radius * 0.86f, Hash(seed, index, 2));
                Vector3 spot = room.Center + Direction(angle) * distance;
                float pick = Hash(seed, index, 3);
                string model = pick < 0.4f ? "cave_stalagmite" : pick < 0.6f ? "cave_rubble" : pick < 0.8f ? "cave_ore_vein" : "cave_pillar";

                if (model == "cave_pillar" && isChamber)
                {
                    model = "cave_stalagmite";
                }

                propCount += Place(props, model, spot, Hash(seed, index, 4) * 360f, 0.8f + Hash(seed, index, 5) * 0.6f) != null ? 1 : 0;

                if (index % 3 == 0) // 천장에 매달린 종유석
                {
                    propCount += Place(props, "cave_stalactite", spot + Vector3.up * (room.Height - 0.2f) + Direction(angle + 40f) * 1.5f, Hash(seed, index, 6) * 360f, 0.7f + Hash(seed, index, 7) * 0.7f) != null ? 1 : 0;
                }
            }

            if (!isChamber) // 넓은 방 : 빛나는 수정 (길잡이 불빛)
            {
                for (int index = 0; index < 2; index++)
                {
                    Vector3 spot = room.Center + Direction(120f * index + 40f) * (room.Radius * 0.7f);
                    GameObject crystal = Place(props, "cave_crystal", spot, Hash(seed, index, 8) * 360f, 1.1f + Hash(seed, index, 9) * 0.5f);

                    if (crystal == null)
                    {
                        continue;
                    }

                    propCount++;
                    GameObject lightObject = new GameObject("CrystalLight");
                    lightObject.transform.SetParent(crystal.transform, false);
                    lightObject.transform.localPosition = new Vector3(0f, 1.4f, 0f);
                    Light light = lightObject.AddComponent<Light>();
                    light.type = LightType.Point;
                    light.color = new Color(0.55f, 0.78f, 1f, 1f);
                    light.range = 16f;
                    light.intensity = 2.2f;
                    light.shadows = LightShadows.None;
                    lights++;
                }
            }

            int veinCount = isChamber ? 3 : room.Layer == 2 ? 7 : 5; // 117일차: 깊은층에 더 많다

            for (int index = 0; index < veinCount; index++)
            {
                float angle = Hash(seed, index, 10) * 360f;
                Vector3 spot = room.Center + Direction(angle) * (room.Radius * 0.9f);
                MineralBuilder.VeinSpec spec = PickVein(room.Layer, Hash(seed, index, 11));
                GameObject prefab = spec != null ? MineralBuilder.LoadVeinPrefab(spec) : null;

                if (prefab == null)
                {
                    continue;
                }

                GameObject vein = (GameObject)PrefabUtility.InstantiatePrefab(prefab, veins);
                vein.transform.SetLocalPositionAndRotation(spot, Quaternion.Euler(0f, Hash(seed, index, 12) * 360f, 0f));
                vein.name = $"CaveVein_{room.Id}_{index:00}_{spec.Id}";
                WorldObjectIdentity identity = vein.GetComponent<WorldObjectIdentity>();

                if (identity == null)
                {
                    identity = vein.AddComponent<WorldObjectIdentity>();
                }

                identity.AssignWorldObjectId($"cave_vein_{room.Id}_{index:00}");
                EditorUtility.SetDirty(identity);
                GameObjectUtility.SetStaticEditorFlags(vein, 0);
                SetCaveLayer(vein);
                veinCounts[spec.Id] = veinCounts.TryGetValue(spec.Id, out int count) ? count + 1 : 1;
            }
        }

        foreach (Tunnel tunnel in tunnels) // 굴 벽에도 가끔 광맥
        {
            if (Mathf.Abs(tunnel.Id.GetHashCode()) % 3 != 0)
            {
                continue;
            }

            int layer = tunnel.From.Layer == 2 && tunnel.To.Layer == 2 ? 2 : 1;
            MineralBuilder.VeinSpec spec = PickVein(layer, Hash(Mathf.Abs(tunnel.Id.GetHashCode()), 0, 13));
            GameObject prefab = spec != null ? MineralBuilder.LoadVeinPrefab(spec) : null;

            if (prefab == null)
            {
                continue;
            }

            Vector3 spot = Vector3.Lerp(tunnel.From.Center, tunnel.To.Center, 0.5f);
            Vector3 flat = tunnel.To.Center - tunnel.From.Center;
            flat.y = 0f;
            Vector3 side = Vector3.Cross(Vector3.up, flat.normalized) * (TunnelWidth * 0.5f - 0.6f);
            GameObject vein = (GameObject)PrefabUtility.InstantiatePrefab(prefab, veins);
            vein.transform.SetLocalPositionAndRotation(spot + side, Quaternion.identity);
            vein.name = $"CaveVein_{tunnel.Id}_{spec.Id}";
            WorldObjectIdentity identity = vein.GetComponent<WorldObjectIdentity>();

            if (identity == null)
            {
                identity = vein.AddComponent<WorldObjectIdentity>();
            }

            identity.AssignWorldObjectId($"cave_vein_{tunnel.Id}");
            EditorUtility.SetDirty(identity);
            GameObjectUtility.SetStaticEditorFlags(vein, 0);
            SetCaveLayer(vein);
            veinCounts[spec.Id] = veinCounts.TryGetValue(spec.Id, out int count) ? count + 1 : 1;
        }

        foreach (MineralBuilder.VeinSpec spec in MineralBuilder.Veins) // 117일차: 없는 광물은 그 층 방에 하나씩 채워 준다
        {
            if (veinCounts.ContainsKey(spec.Id))
            {
                continue;
            }

            Room home = rooms.FirstOrDefault(room => room.Layer == spec.Layer && room.Radius > ChamberRadius + 0.1f) ?? rooms.FirstOrDefault(room => room.Layer == spec.Layer);
            GameObject prefab = home != null ? MineralBuilder.LoadVeinPrefab(spec) : null;

            if (prefab == null)
            {
                continue;
            }

            float angle = Mathf.Abs(spec.Id.GetHashCode()) % 360;
            GameObject vein = (GameObject)PrefabUtility.InstantiatePrefab(prefab, veins);
            vein.transform.SetLocalPositionAndRotation(home.Center + Direction(angle) * (home.Radius * 0.75f), Quaternion.Euler(0f, angle, 0f));
            vein.name = $"CaveVein_{home.Id}_fill_{spec.Id}";
            WorldObjectIdentity identity = vein.GetComponent<WorldObjectIdentity>();

            if (identity == null)
            {
                identity = vein.AddComponent<WorldObjectIdentity>();
            }

            identity.AssignWorldObjectId($"cave_vein_fill_{spec.Id}");
            EditorUtility.SetDirty(identity);
            GameObjectUtility.SetStaticEditorFlags(vein, 0);
            SetCaveLayer(vein);
            veinCounts[spec.Id] = 1;
        }

        return $"동굴 소품 {propCount}개 · 길잡이 수정 불빛 {lights}개 · 광맥 {veinCounts.Values.Sum()}개 ({string.Join(" · ", veinCounts.OrderByDescending(pair => pair.Value).Select(pair => $"{pair.Key} {pair.Value}"))})";
    }

    // 117일차: 층에 맞는 광물 뽑기 (깊을수록 좋은 광물)
    private static MineralBuilder.VeinSpec PickVein(int layer, float roll)
    {
        MineralBuilder.VeinSpec[] pool = MineralBuilder.Veins.Where(spec => spec.Layer == layer).ToArray();

        if (pool.Length == 0)
        {
            return null;
        }

        float total = pool.Sum(spec => spec.Weight);
        float pick = Mathf.Clamp01(roll) * total;

        foreach (MineralBuilder.VeinSpec spec in pool)
        {
            pick -= spec.Weight;

            if (pick <= 0f)
            {
                return spec;
            }
        }

        return pool[pool.Length - 1];
    }

    private static float Hash(int seed, int index, int salt)
    {
        return Mathf.Abs(Mathf.Sin(seed * 0.0177f + index * 12.9898f + salt * 78.233f) * 43758.5453f) % 1f;
    }

    private static GameObject Place(Transform parent, string modelId, Vector3 localPosition, float yaw, float scale)
    {
        GameObject prefab = StylizedArtAssetFactory.LoadModelPrefab(modelId) ?? StylizedArtAssetFactory.GetOrCreateModelPrefab(modelId, false);

        if (prefab == null)
        {
            return null;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.transform.SetLocalPositionAndRotation(localPosition, Quaternion.Euler(0f, yaw, 0f));
        instance.transform.localScale = Vector3.one * scale;
        GameObjectUtility.SetStaticEditorFlags(instance, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);

        foreach (MeshRenderer renderer in instance.GetComponentsInChildren<MeshRenderer>())
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        if (parent != null && parent.name != "CaveEntrances") // 지상 입구는 섬 레이어 그대로
        {
            SetCaveLayer(instance);
        }

        return instance;
    }

    // ---------------------------------------------------------------- 지상 입구 · 동굴 안 출구

    private static readonly Dictionary<string, Vector3> surfaceCache = new Dictionary<string, Vector3>(StringComparer.Ordinal); // 입구 자리 (한 번 정하면 같은 자리)

    // 흙길 옆에서 평평하고 나무가 없는 자리를 고른다 (길 · 바다 · 가파른 비탈 피하기)
    private static Vector3 SurfacePoint(Terrain terrain, EntranceSpec spec)
    {
        if (surfaceCache.TryGetValue(spec.Id, out Vector3 cached))
        {
            return cached;
        }

        int road = Array.FindIndex(IslandZoneLayout.Roads, item => item.Id == spec.RoadId);

        if (road < 0)
        {
            return Vector3.zero;
        }

        float length = IslandZoneLayout.RoadLength(road);
        float baseAlong = Mathf.Clamp(spec.Along >= 0f ? spec.Along : length + spec.Along, 12f, length - 12f);
        Vector3 best = Vector3.zero;
        float bestScore = float.MinValue;

        foreach (float alongOffset in new[] { 0f, 10f, -10f, 20f, -20f, 32f, -32f, 50f, -50f, 75f, -75f })
        {
            float along = Mathf.Clamp(baseAlong + alongOffset, 12f, length - 12f);
            Vector3 point = IslandZoneLayout.RoadPoint(road, along);
            Vector3 next = IslandZoneLayout.RoadPoint(road, Mathf.Min(length, along + 3f));
            Vector3 forward = (next - point).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, forward) * Mathf.Sign(spec.Side);

            foreach (float sign in new[] { 1f, -1f }) // 길 반대쪽도 살펴본다
            {
                foreach (float distance in new[] { Mathf.Abs(spec.Side), 18f, 21f, 24f, 27f, 30f })
                {
                    Vector3 spot = point + side * sign * distance;
                    spot.y = IslandZoneLayout.Apply(spot.x, spot.z); // 절벽을 만들기 전 원래 높이
                    float score = Score(spot);

                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = spot;
                    }

                    if (score > 90f) // 충분히 좋은 자리면 그만 찾는다
                    {
                        surfaceCache[spec.Id] = spot;
                        return spot;
                    }
                }
            }
        }

        surfaceCache[spec.Id] = best;
        return best;
    }

    // 입구 자리 점수 : 바다 위 · 흙길에서 충분히 멀리 · 조금 비탈진 언덕 옆 (나무는 나중에 치운다)
    private static float Score(Vector3 spot)
    {
        if (spot.y < IslandTerrainBuilder.SeaLevel + 2f)
        {
            return -100f; // 바다 · 물가
        }

        float road = IslandZoneLayout.RoadDistanceWorld(spot);
        float roadScore = road < 16f ? -200f : Mathf.Min(road, 30f);
        float slopeScore = SlopeAt(spot.x, spot.z) < 6f ? SlopeAt(spot.x, spot.z) * 2f : 12f - Mathf.Max(0f, SlopeAt(spot.x, spot.z) - 18f) * 3f;
        return 60f + roadScore + slopeScore;
    }

    private static float SlopeAt(float x, float z) // 원래 섬 높이로 계산한 비탈 (도)
    {
        float dx = (IslandZoneLayout.Apply(x + 3f, z) - IslandZoneLayout.Apply(x - 3f, z)) / 6f;
        float dz = (IslandZoneLayout.Apply(x, z + 3f) - IslandZoneLayout.Apply(x, z - 3f)) / 6f;
        return Mathf.Atan(Mathf.Sqrt(dx * dx + dz * dz)) * Mathf.Rad2Deg;
    }

    private static string BuildPortals(Transform root, Transform mapGroup, Terrain terrain, Dictionary<string, Room> chamberByEntrance)
    {
        Transform surfaceGroup = new GameObject("CaveEntrances").transform;
        surfaceGroup.SetParent(root, false);
        Transform exitGroup = new GameObject("CaveExits").transform;
        exitGroup.SetParent(mapGroup, false);
        int interactable = LayerMask.NameToLayer(InteractableLayer);
        int made = 0;
        List<string> names = new List<string>();

        foreach (EntranceSpec spec in Entrances)
        {
            if (!chamberByEntrance.TryGetValue(spec.Id, out Room chamber))
            {
                continue;
            }

            Vector3 surface = SurfacePoint(terrain, spec);
            Vector3 toIsland = EntranceForward(terrain, spec); // 입구는 내리막(플레이어가 올라오는 쪽)을 바라봄
            float yaw = Mathf.Atan2(toIsland.x, toIsland.z) * Mathf.Rad2Deg;
            string model = spec.Kind == PortalKind.Arch ? "cave_entrance_arch" : spec.Kind == PortalKind.Crack ? "cave_crack" : "cave_sinkhole";
            GameObject entrance = Place(surfaceGroup, model, surface, yaw, 1f);

            if (entrance == null)
            {
                continue;
            }

            entrance.name = $"CaveEntrance_{spec.Id}";
            entrance.transform.position = surface;
            BuildEntranceCliff(surfaceGroup, spec, surface, yaw); // 입구가 절벽에 박힌 것처럼

            // 동굴 안 : 짝이 되는 출구(사다리)
            Vector3 exitSpot = chamber.Center + Direction(Mathf.Atan2(-chamber.Center.x, -chamber.Center.z) * Mathf.Rad2Deg + 180f) * (chamber.Radius * 0.72f);
            float exitYaw = Mathf.Atan2(chamber.Center.x - exitSpot.x, chamber.Center.z - exitSpot.z) * Mathf.Rad2Deg;
            GameObject exit = Place(exitGroup, "cave_exit_ladder", exitSpot, exitYaw, 1f);
            exit.name = $"CaveExit_{spec.Id}";

            // 도착 지점 : 입구 앞(밖) · 사다리 앞(안)
            Transform surfaceSpawn = CreateSpawn(entrance.transform, "SurfaceSpawn", surface + toIsland * 3.2f + Vector3.up * 0.2f, yaw + 180f);
            Transform caveSpawn = CreateSpawn(exit.transform, "CaveSpawn", mapGroup.TransformPoint(exitSpot + Direction(exitYaw) * 3f) + Vector3.up * 0.2f, exitYaw);

            AddPortal(entrance, spec, false, caveSpawn, interactable, new Vector3(0f, 1.6f, 0.6f), new Vector3(4.2f, 3.2f, 2.4f));
            AddPortal(exit, spec, true, surfaceSpawn, interactable, new Vector3(0f, 1.6f, 0.4f), new Vector3(3f, 3.2f, 2f));
            names.Add($"{spec.Name}({surface.x:0},{surface.z:0})");
            made++;
        }

        return $"동굴 입구 {made}곳 : {string.Join(" · ", names)}";
    }

    // 입구 뒤 땅을 올려 절벽을 만든다 (입구 앞 통로는 평평하게 남긴다). 여러 번 해도 같은 결과가 나오도록 원래 높이에서 다시 계산한다.
    private static string ShapeEntranceCliffs(Terrain terrain)
    {
        TerrainData data = terrain.terrainData;
        int resolution = data.heightmapResolution;
        float size = data.size.x;
        float range = data.size.y;
        Vector3 origin = terrain.transform.position;
        float step = size / (resolution - 1);
        int patch = Mathf.CeilToInt(HillFade * 1.6f / step);
        int changed = 0;
        float highest = 0f;

        foreach (EntranceSpec spec in Entrances)
        {
            if (spec.Kind == PortalKind.Sinkhole)
            {
                continue; // 구덩이는 평지에 뚫린 구멍
            }

            Vector3 surface = SurfacePoint(terrain, spec);
            int centerX = Mathf.RoundToInt((surface.x - origin.x) / step);
            int centerY = Mathf.RoundToInt((surface.z - origin.z) / step);
            int x0 = Mathf.Clamp(centerX - patch, 0, resolution - 1);
            int y0 = Mathf.Clamp(centerY - patch, 0, resolution - 1);
            int width = Mathf.Clamp(centerX + patch, 0, resolution - 1) - x0 + 1;
            int height = Mathf.Clamp(centerY + patch, 0, resolution - 1) - y0 + 1;

            if (width <= 1 || height <= 1)
            {
                continue;
            }

            float[,] heights = data.GetHeights(x0, y0, width, height);

            for (int y = 0; y < height; y++)
            {
                float worldZ = origin.z + (y0 + y) * step;

                for (int x = 0; x < width; x++)
                {
                    float worldX = origin.x + (x0 + x) * step;
                    float baseHeight = IslandZoneLayout.Apply(worldX, worldZ); // 원래 섬 높이
                    float added = CliffHeightAt(terrain, worldX, worldZ);
                    highest = Mathf.Max(highest, added);
                    heights[y, x] = Mathf.Clamp01((baseHeight + added - origin.y) / range);
                }
            }

            data.SetHeights(x0, y0, heights);
            changed++;
        }

        int movedTrees = RefreshTrees(terrain);
        int movedProps = RefreshGroundObjects(terrain);
        RefreshEntranceHeights(terrain);
        string paint = IslandTerrainBuilder.RepaintIslandGround(terrain); // 가파른 곳은 바위로 칠해짐
        return $"입구 절벽 {changed}곳 (가장 높은 곳 {highest:0.0}m) · 나무 정리 {movedTrees}그루 · 자연 물건 {movedProps}개 · {paint.Split('\n').FirstOrDefault()}";
    }

    // 입구 하나가 만드는 언덕 높이 (입구 앞은 0, 뒤로 갈수록 높아짐, 가운데 통로는 비움)
    public static float CliffHeightAt(Terrain terrain, float worldX, float worldZ)
    {
        float total = 0f;

        foreach (EntranceSpec spec in Entrances)
        {
            if (spec.Kind == PortalKind.Sinkhole)
            {
                continue;
            }

            Vector3 surface = SurfacePoint(terrain, spec);
            Vector3 forward = EntranceForward(terrain, spec); // 입구가 바라보는 쪽 (내리막)
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 offset = new Vector3(worldX - surface.x, 0f, worldZ - surface.z);
            float along = Vector3.Dot(offset, forward); // 앞(+) · 뒤(-)
            float side = Mathf.Abs(Vector3.Dot(offset, right));

            if (along > 6f || -along > HillFade + 20f || side > HillFade)
            {
                continue;
            }

            float rise = Smooth(0f, -HillRise, along) * HillHeight; // 입구에서 뒤로 갈수록 높아짐
            float lateral = 1f - Smooth(HillWidth, HillFade, side); // 옆으로 갈수록 낮아짐
            float notch = (1f - Smooth(MouthWidth * 0.5f, MouthWidth * 0.5f + 2.5f, side)) * (1f - Smooth(-MouthDepth, -MouthDepth - 4f, along)); // 입구 앞 통로 (뒤로 갈수록 다시 절벽)
            total = Mathf.Max(total, rise * lateral * (1f - notch));
        }

        return total * KeepClearFactor(worldX, worldZ); // 흙길 · 마을 · 구역 터는 덮지 않는다
    }

    // 흙길 · 마을 · 구역 터에서 멀수록 1 (그 위에는 언덕을 만들지 않는다)
    private static float KeepClearFactor(float worldX, float worldZ)
    {
        float road = Smooth(IslandNatureBuilder.RoadClear + 2f, IslandNatureBuilder.RoadClear + 13f, IslandZoneLayout.RoadDistanceWorld(new Vector3(worldX, 0f, worldZ)));
        float village = Smooth(IslandNatureBuilder.VillageClear - 10f, IslandNatureBuilder.VillageClear + 20f, Mathf.Max(Mathf.Abs(worldX), Mathf.Abs(worldZ)));
        float pad = 1f;

        foreach (IslandZoneLayout.Site site in IslandZoneLayout.Sites)
        {
            pad = Mathf.Min(pad, 1f - IslandZoneLayout.PadWeight(site, worldX, worldZ));
        }

        return Mathf.Min(Mathf.Min(road, village), pad);
    }

    private static void RefreshEntranceHeights(Terrain terrain) // 땅이 바뀌었으니 입구 높이를 다시 맞춘다
    {
        foreach (EntranceSpec spec in Entrances)
        {
            if (!surfaceCache.TryGetValue(spec.Id, out Vector3 spot))
            {
                continue;
            }

            spot.y = terrain.SampleHeight(spot) + terrain.transform.position.y;
            surfaceCache[spec.Id] = spot;
        }
    }

    // 절벽 자리의 섬 자연 물건(채집 나무 · 돌)을 새 땅 높이에 맞춘다
    private static int RefreshGroundObjects(Terrain terrain)
    {
        GameObject root = GameObject.Find(IslandNatureBuilder.RootName);

        if (root == null)
        {
            return 0;
        }

        int moved = 0;

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.parent == null || child.childCount > 0 && child.GetComponent<GatherableResource>() == null)
            {
                continue;
            }

            Vector3 position = child.position;
            bool nearCliff = Entrances.Any(spec => spec.Kind != PortalKind.Sinkhole
                && Vector2.Distance(new Vector2(position.x, position.z), new Vector2(SurfacePoint(terrain, spec).x, SurfacePoint(terrain, spec).z)) < HillFade + 25f);

            if (!nearCliff)
            {
                continue;
            }

            position.y = terrain.SampleHeight(position) + terrain.transform.position.y;
            child.position = position;
            moved++;
        }

        return moved;
    }

    private static readonly Dictionary<string, Vector3> forwardCache = new Dictionary<string, Vector3>(StringComparer.Ordinal); // 입구가 바라보는 쪽

    // 입구는 언덕 아래(내리막)를 바라본다. 평평하면 섬 안쪽을 본다.
    private static Vector3 EntranceForward(Terrain terrain, EntranceSpec spec)
    {
        if (forwardCache.TryGetValue(spec.Id, out Vector3 cached))
        {
            return cached;
        }

        Vector3 surface = SurfacePoint(terrain, spec);
        Vector3 best = new Vector3(-surface.x, 0f, -surface.z).normalized; // 기본 : 섬 안쪽
        float bestScore = float.MinValue;

        for (int index = 0; index < 24; index++) // 둘레 24방향 : 내리막이면서 뒤에 언덕을 세울 자리가 있는 쪽
        {
            Vector3 direction = Direction(index * 15f);
            Vector3 front = surface + direction * 16f;
            Vector3 behind = surface - direction * 14f;
            float downhill = surface.y - IslandZoneLayout.Apply(front.x, front.z); // 앞이 낮을수록 좋음
            float room = KeepClearFactor(behind.x, behind.z) * 14f; // 뒤에 흙길 · 마을 · 구역이 없을수록 좋음
            float frontRoom = KeepClearFactor(front.x, front.z) < 0.4f ? 4f : 0f; // 앞이 길이면 걸어오기 좋음
            float score = downhill * 1.5f + room + frontRoom;

            if (score > bestScore)
            {
                bestScore = score;
                best = direction;
            }
        }

        forwardCache[spec.Id] = best;
        return best;
    }

    private static float Smooth(float edge0, float edge1, float value)
    {
        float t = Mathf.Clamp01((value - edge0) / (edge1 - edge0));
        return t * t * (3f - 2f * t);
    }

    // 절벽이 생긴 자리의 나무를 땅 높이에 맞추고, 입구 앞 통로의 나무는 지운다
    private static int RefreshTrees(Terrain terrain)
    {
        TerrainData data = terrain.terrainData;
        List<TreeInstance> kept = new List<TreeInstance>();
        int touched = 0;

        foreach (TreeInstance tree in data.treeInstances)
        {
            Vector3 world = Vector3.Scale(tree.position, data.size) + terrain.transform.position;
            float added = CliffHeightAt(terrain, world.x, world.z);
            bool nearMouth = Entrances.Any(spec => Vector2.Distance(new Vector2(world.x, world.z), new Vector2(SurfacePoint(terrain, spec).x, SurfacePoint(terrain, spec).z)) < 18f); // 입구 둘레는 비운다

            if (nearMouth)
            {
                touched++;
                continue; // 입구 앞은 비운다
            }

            if (added > 0.2f)
            {
                TreeInstance moved = tree;
                float ground = IslandZoneLayout.Apply(world.x, world.z) + added;
                moved.position = new Vector3(tree.position.x, Mathf.Clamp01((ground - terrain.transform.position.y) / data.size.y), tree.position.z);
                kept.Add(moved);
                touched++;
                continue;
            }

            kept.Add(tree);
        }

        data.SetTreeInstances(kept.ToArray(), true);
        return touched;
    }

    // 입구 앞에 바위 얼굴을 세워 굴 입구처럼 보이게 한다 (언덕은 위에서 땅으로 만든다)
    private static void BuildEntranceCliff(Transform parent, EntranceSpec spec, Vector3 surface, float yaw)
    {
        if (spec.Kind == PortalKind.Sinkhole)
        {
            return; // 무너진 구덩이는 평지에 뚫린 구멍
        }

        bool arch = spec.Kind == PortalKind.Arch;
        Vector3 back = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward; // 입구가 바라보는 쪽(섬 안쪽)
        Vector3 spot = surface - back * (arch ? 2.2f : 1.4f) - Vector3.up * 1.2f; // 땅에 조금 묻어 언덕처럼
        int seed = Mathf.Abs(spec.Id.GetHashCode());
        float scale = arch ? 0.92f + Hash(seed, 0, 21) * 0.22f : 0.85f + Hash(seed, 0, 22) * 0.3f; // 절벽마다 조금씩 다른 크기
        GameObject cliff = Place(parent, arch ? "cave_cliff" : "cave_cliff_small", spot, yaw + (Hash(seed, 0, 23) - 0.5f) * 10f, scale);

        if (cliff == null)
        {
            return;
        }

        cliff.name = $"CaveCliff_{spec.Id}";
        cliff.transform.position = spot;
        BoxCollider left = cliff.AddComponent<BoxCollider>(); // 절벽 양옆은 막고 가운데(입구)는 지나갈 수 있게
        left.center = new Vector3(arch ? -5.4f : -3.4f, 3.2f, 0.2f);
        left.size = new Vector3(arch ? 6f : 3.8f, 7f, 6f);
        BoxCollider right = cliff.AddComponent<BoxCollider>();
        right.center = new Vector3(arch ? 5.4f : 3.4f, 3.2f, 0.2f);
        right.size = new Vector3(arch ? 6f : 3.8f, 7f, 6f);
        BoxCollider body = cliff.AddComponent<BoxCollider>();
        body.center = new Vector3(0f, 3.5f, arch ? -7f : -4.5f);
        body.size = new Vector3(arch ? 20f : 12f, 10f, arch ? 14f : 9f);
    }

    private static Transform CreateSpawn(Transform parent, string name, Vector3 worldPosition, float yaw)
    {
        GameObject spawn = new GameObject(name);
        spawn.transform.SetParent(parent, false);
        spawn.transform.SetPositionAndRotation(worldPosition, Quaternion.Euler(0f, yaw, 0f));
        return spawn.transform;
    }

    private static void AddPortal(GameObject target, EntranceSpec spec, bool isExit, Transform destination, int layer, Vector3 colliderCenter, Vector3 colliderSize)
    {
        CavePortal portal = target.GetComponent<CavePortal>();

        if (portal == null)
        {
            portal = target.AddComponent<CavePortal>();
        }

        portal.EditorAssign(spec.Id, spec.Name, AreaId, isExit, destination);
        EditorUtility.SetDirty(portal);
        BoxCollider box = target.GetComponent<BoxCollider>();

        if (box == null)
        {
            box = target.AddComponent<BoxCollider>();
        }

        box.center = colliderCenter;
        box.size = colliderSize;
        box.isTrigger = true; // 지나갈 수 있고, F로만 드나든다

        if (layer >= 0)
        {
            target.layer = layer;
        }
    }

    // ---------------------------------------------------------------- 동굴 길(NavMesh) · Scene 연결

    private static string BuildCaveNavMesh(Transform root, List<Room> rooms)
    {
        GameObject holder = new GameObject(CaveNavMeshName);
        holder.transform.SetParent(root, false);
        holder.transform.position = CaveOrigin;
        Physics.SyncTransforms(); // 방금 만든 동굴 충돌체를 물리 세계에 반영한 뒤 굽는다
        NavMeshSurface surface = holder.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Volume;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.size = new Vector3(ChamberRing * 2.6f, 80f, ChamberRing * 2.6f);
        surface.center = new Vector3(0f, 0f, 0f); // 117일차: 깊은층까지 포함
        surface.BuildNavMesh();
        NavMeshData data = surface.navMeshData;

        if (data == null)
        {
            return "✗ 동굴 길(NavMesh)을 굽지 못했습니다.";
        }

        string folder = System.IO.Path.ChangeExtension(ScenePath, null);

        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder(System.IO.Path.GetDirectoryName(folder), System.IO.Path.GetFileName(folder));
        }

        string path = $"{folder}/NavMesh-Caves.asset";
        NavMeshData existing = AssetDatabase.LoadAssetAtPath<NavMeshData>(path);

        if (existing != null)
        {
            surface.RemoveData();
            EditorUtility.CopySerialized(data, existing);
            existing.name = "NavMesh-Caves";
            surface.navMeshData = existing;
            surface.AddData();
            Object.DestroyImmediate(data);
            EditorUtility.SetDirty(existing);
            AssetDatabase.SaveAssetIfDirty(existing);
        }
        else
        {
            AssetDatabase.CreateAsset(data, path);
        }

        EditorUtility.SetDirty(surface);
        bool walkable = NavMesh.SamplePosition(CaveOrigin, out NavMeshHit _, 6f, NavMesh.AllAreas);
        return walkable
            ? $"동굴 길(NavMesh) 굽기 완료 ({rooms.Count}개 방)"
            : "✗ 동굴 길(NavMesh)에 걸어 다닐 바닥이 없습니다.";
    }

    private static string WireScene(Scene scene, Transform root)
    {
        StringBuilder report = new StringBuilder();
        report.AppendLine(ConnectCaveMap());
        report.AppendLine(BuildEntranceMapLabels(root));
        CaveManager manager = Object.FindFirstObjectByType<CaveManager>(FindObjectsInactive.Include);

        if (manager == null)
        {
            manager = root.gameObject.AddComponent<CaveManager>();
            report.AppendLine("동굴 관리자 추가");
        }

        EditorUtility.SetDirty(manager);
        PlayerInventory inventory = Object.FindFirstObjectByType<PlayerInventory>(FindObjectsInactive.Include);

        if (inventory != null && inventory.GetComponent<PlayerTorchLight>() == null)
        {
            inventory.gameObject.AddComponent<PlayerTorchLight>();
            EditorUtility.SetDirty(inventory.gameObject);
            report.AppendLine("플레이어 횃불 불빛 추가");
        }

        return report.Length > 0 ? report.ToString().TrimEnd() : "동굴 관리자 · 횃불 불빛 이미 연결됨";
    }

    private static string ConnectCaveMap() // 지도 카메라가 동굴 안에서 쓸 레이어 연결
    {
        MinimapCameraController map = Object.FindFirstObjectByType<MinimapCameraController>(FindObjectsInactive.Include);
        int caveLayer = EnsureCaveLayer();
        int labelLayer = LayerMask.NameToLayer("MapLabel");

        if (map == null || caveLayer < 0)
        {
            return "✗ 지도 카메라 또는 동굴 레이어가 없습니다.";
        }

        int mask = 1 << caveLayer;

        if (labelLayer >= 0)
        {
            mask |= 1 << labelLayer;
        }

        SerializedObject serialized = new SerializedObject(map);
        serialized.FindProperty("caveLayerMask").intValue = mask;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(map);
        return $"동굴 안 지도 레이어 연결 (Cave {caveLayer})";
    }

    private static string BuildEntranceMapLabels(Transform root) // 섬 지도에 동굴 입구 이름
    {
        int layer = LayerMask.NameToLayer("MapLabel");

        if (layer < 0)
        {
            return "지도 이름표 레이어가 없어 동굴 입구 이름은 넣지 않았습니다.";
        }

        Transform group = new GameObject("CaveMapLabels").transform;
        group.SetParent(root, false);
        group.gameObject.AddComponent<MapLabelScaler>();
        int made = 0;

        foreach (CavePortal portal in root.GetComponentsInChildren<CavePortal>(true).Where(item => !item.IsExit))
        {
            GameObject holder = new GameObject("MapLabel_" + portal.PortalId, typeof(RectTransform));
            holder.transform.SetParent(group, false);
            holder.layer = layer;
            TextMeshPro label = holder.AddComponent<TextMeshPro>();
            label.rectTransform.sizeDelta = new Vector2(70f, 1f);
            label.fontSize = 42f;
            label.alignment = TextAlignmentOptions.Center;
            label.richText = true;
            label.text = $"<mark=#1C1C20B0 padding=\"18,18,8,8\">{portal.DisplayName}</mark>";
            holder.transform.SetPositionAndRotation(portal.transform.position + Vector3.up * 40f, Quaternion.Euler(90f, 0f, 0f));
            holder.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            made++;
        }

        return $"섬 지도에 동굴 입구 이름 {made}개";
    }

    // ---------------------------------------------------------------- 횃불 아이템

    private static string BuildTorchItem()
    {
        StylizedArtAssetFactory.EnsureFolder(ItemFolder);
        StylizedArtAssetFactory.EnsureFolder(CraftingFolder);
        StylizedArtAssetFactory.EnsureFolder(PickupFolder);
        ItemData torch = LoadOrCreate<ItemData>(TorchItemPath);
        SerializedObject serialized = new SerializedObject(torch);
        serialized.FindProperty("itemId").stringValue = TorchItemId;
        serialized.FindProperty("displayName").stringValue = "TORCH";
        serialized.FindProperty("koreanName").stringValue = "횃불";
        serialized.FindProperty("description").stringValue = "A burning torch that lights dark caves.";
        serialized.FindProperty("itemCategory").intValue = (int)ItemCategory.CraftingMaterial;
        serialized.FindProperty("maximumStack").intValue = 10;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(torch);

        ItemData wood = AssetDatabase.LoadAssetAtPath<ItemData>(WoodItemPath);
        ItemData fiber = AssetDatabase.LoadAssetAtPath<ItemData>(FiberItemPath);
        ItemData stone = AssetDatabase.LoadAssetAtPath<ItemData>(StoneItemPath);

        if (wood == null || fiber == null)
        {
            return "✗ 나무 · 식물 섬유 아이템이 없어 횃불 제작법을 만들지 못했습니다.";
        }

        CraftingRecipeData recipe = LoadOrCreate<CraftingRecipeData>(TorchRecipePath);
        SerializedObject recipeObject = new SerializedObject(recipe);
        recipeObject.FindProperty("recipeId").stringValue = "recipe_torch";
        recipeObject.FindProperty("displayName").stringValue = "TORCH";
        recipeObject.FindProperty("requiredFacility").intValue = (int)CraftingFacilityType.Hand;
        recipeObject.FindProperty("unlockType").intValue = (int)CraftingUnlockType.Default;
        recipeObject.FindProperty("unlockId").stringValue = string.Empty;
        recipeObject.FindProperty("resultItem").objectReferenceValue = torch;
        recipeObject.FindProperty("resultQuantity").intValue = 2;
        SerializedProperty ingredients = recipeObject.FindProperty("ingredients");
        ingredients.arraySize = 2;
        ingredients.GetArrayElementAtIndex(0).FindPropertyRelative("itemData").objectReferenceValue = wood;
        ingredients.GetArrayElementAtIndex(0).FindPropertyRelative("amount").intValue = 1;
        ingredients.GetArrayElementAtIndex(1).FindPropertyRelative("itemData").objectReferenceValue = fiber;
        ingredients.GetArrayElementAtIndex(1).FindPropertyRelative("amount").intValue = 2;
        recipeObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(recipe);

        string pickupReport = BuildTorchPickup(torch);
        string registerReport = RegisterTorch(torch);
        string heldReport = ConnectHeldVisual(torch);
        AssetDatabase.SaveAssets();
        GameDataRegistryEditor.CreateOrRefreshDefaultRegistry();
        string visualReport = ItemVisualContentBuilder.BuildAll(); // 모델 · 아이콘 · 바닥용 Prefab · 손에 든 외형
        string visualLine = visualReport.Split('\n').FirstOrDefault(line => line.StartsWith("✗")) ?? "아이템 외형 · 아이콘 생성";
        return $"횃불 아이템 · 맨손 제작법(나무 1 + 식물 섬유 2 → 2개) · {pickupReport} · {registerReport} · {heldReport} · {visualLine}";
    }

    private static string BuildTorchPickup(ItemData torch)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(TorchPickupPath) == null && !AssetDatabase.CopyAsset(PickupTemplatePath, TorchPickupPath))
        {
            return "✗ 횃불 줍기 Prefab 복사 실패";
        }

        GameObject root = PrefabUtility.LoadPrefabContents(TorchPickupPath);

        try
        {
            root.name = "TorchPickup";
            WorldItemPickup pickup = root.GetComponent<WorldItemPickup>();
            SerializedObject serialized = new SerializedObject(pickup);
            serialized.FindProperty("promptMessage").stringValue = "F - PICK UP TORCH";
            serialized.FindProperty("itemData").objectReferenceValue = torch;
            serialized.FindProperty("quantity").intValue = 1;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, TorchPickupPath);
            return "줍기 Prefab";
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static string RegisterTorch(ItemData torch)
    {
        ItemDatabase database = AssetDatabase.LoadAssetAtPath<ItemDatabase>(ItemDatabasePath);

        if (database == null)
        {
            return "✗ ItemDatabase 없음";
        }

        SerializedObject serialized = new SerializedObject(database);
        SerializedProperty list = serialized.FindProperty("items");

        for (int index = 0; index < list.arraySize; index++)
        {
            if (list.GetArrayElementAtIndex(index).objectReferenceValue == torch)
            {
                return "ItemDatabase 등록됨";
            }
        }

        list.arraySize++;
        list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = torch;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(database);
        return "ItemDatabase 추가";
    }

    private static string ConnectHeldVisual(ItemData torch)
    {
        HeldItemVisualSet set = AssetDatabase.LoadAssetAtPath<HeldItemVisualSet>(HeldVisualsPath);
        GameObject model = StylizedArtAssetFactory.LoadModelPrefab("item_torch");

        if (set == null || model == null)
        {
            return "✗ 손에 든 외형 목록 또는 횃불 모델 없음";
        }

        SerializedObject serialized = new SerializedObject(set);
        SerializedProperty entries = serialized.FindProperty("entries");
        SerializedProperty entry = null;

        for (int index = 0; index < entries.arraySize; index++)
        {
            if (entries.GetArrayElementAtIndex(index).FindPropertyRelative("item").objectReferenceValue == torch)
            {
                entry = entries.GetArrayElementAtIndex(index);
                break;
            }
        }

        if (entry == null)
        {
            entries.arraySize++;
            entry = entries.GetArrayElementAtIndex(entries.arraySize - 1);
        }

        entry.FindPropertyRelative("item").objectReferenceValue = torch;
        entry.FindPropertyRelative("modelPrefab").objectReferenceValue = model;
        entry.FindPropertyRelative("holderLocalPosition").vector3Value = new Vector3(0f, 0.05f, 0.04f);
        entry.FindPropertyRelative("holderLocalEuler").vector3Value = new Vector3(-12f, 0f, 0f);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(set);
        return "손에 든 외형 연결";
    }

    private static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);

        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
        }

        return asset;
    }

    // ---------------------------------------------------------------- 검증

    public static string Validate(out int errorCount)
    {
        StringBuilder report = new StringBuilder("[동굴 검증]\n");
        int errors = 0;

        void Error(string message)
        {
            errors++;
            report.AppendLine("✗ " + message);
        }

        ItemData torch = AssetDatabase.LoadAssetAtPath<ItemData>(TorchItemPath);

        if (torch == null || torch.ItemId != TorchItemId)
        {
            Error("횃불 아이템이 없습니다. 콘텐츠 자동 적용을 기다리세요.");
        }

        if (AssetDatabase.LoadAssetAtPath<CraftingRecipeData>(TorchRecipePath) == null)
        {
            Error("횃불 제작법이 없습니다.");
        }

        if (EditorSceneManager.GetActiveScene().path != ScenePath)
        {
            report.AppendLine($"결과 : 오류 {errors}개 (게임 Scene이 아니어서 동굴 배치는 확인하지 않음)");
            errorCount = errors;
            return report.ToString();
        }

        GameObject root = EditorSceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(item => item.name == RootName);

        if (root == null)
        {
            Error("동굴 묶음(=== Caves ===)이 없습니다. 콘텐츠 자동 적용을 기다리세요.");
            report.AppendLine($"결과 : 오류 {errors}개");
            errorCount = errors;
            return report.ToString();
        }

        CavePortal[] portals = root.GetComponentsInChildren<CavePortal>(true);
        CavePortal[] entrances = portals.Where(portal => !portal.IsExit).ToArray();
        CavePortal[] exits = portals.Where(portal => portal.IsExit).ToArray();

        if (entrances.Length != Entrances.Length || exits.Length != Entrances.Length)
        {
            Error($"동굴 입구 {entrances.Length}곳 · 출구 {exits.Length}곳입니다 ({Entrances.Length}곳씩 필요).");
        }

        Terrain terrain = Terrain.activeTerrain;

        foreach (CavePortal portal in portals)
        {
            if (portal.Destination == null)
            {
                Error($"{portal.name} : 도착 지점이 없습니다.");
                continue;
            }

            bool inside = portal.IsExit;
            bool destinationInsideCave = Vector2.Distance(new Vector2(portal.Destination.position.x, portal.Destination.position.z), new Vector2(CaveOrigin.x, CaveOrigin.z)) < ChamberRing * 1.6f;

            if (inside == destinationInsideCave)
            {
                Error($"{portal.name} : 도착 지점이 반대쪽(동굴 · 지상)에 있어야 합니다.");
            }

            if (!NavMesh.SamplePosition(portal.Destination.position, out NavMeshHit _, 3f, NavMesh.AllAreas))
            {
                Error($"{portal.name} : 도착 지점에 걸어 다닐 길이 없습니다.");
            }

            if (!inside && terrain != null)
            {
                float ground = terrain.SampleHeight(portal.transform.position) + terrain.transform.position.y;

                if (portal.transform.position.y < IslandTerrainBuilder.SeaLevel + 1f || Mathf.Abs(portal.transform.position.y - ground) > 1.5f)
                {
                    Error($"{portal.name} : 지상 입구가 땅 위에 서 있지 않습니다 (바다 · 공중).");
                }
            }
        }

        // 동굴 안 : 어느 입구방에서든 가운데 큰 방까지 걸어갈 수 있어야 함 (지상 입구의 도착 지점 = 동굴 안 자리)
        if (entrances.Length > 0)
        {
            int reachable = entrances.Count(entrance => entrance.Destination != null && CanWalk(entrance.Destination.position, CaveOrigin));

            if (reachable < entrances.Length)
            {
                Error($"동굴 안에서 가운데 큰 방까지 갈 수 없는 입구방이 {entrances.Length - reachable}곳 있습니다.");
            }

            report.AppendLine($"동굴 안 길 : 입구방 {reachable}/{entrances.Length}곳에서 가운데 큰 방까지 이어짐");
        }

        report.AppendLine($"동굴 입구 {entrances.Length}곳 · 출구 {exits.Length}곳 · 광맥 {root.GetComponentsInChildren<GatherableResource>(true).Length}개");
        report.AppendLine($"결과 : 오류 {errors}개");
        errorCount = errors;
        return report.ToString();
    }

    private static bool CanWalk(Vector3 from, Vector3 to) // NPC처럼 부분 경로를 이어서 확인
    {
        if (!NavMesh.SamplePosition(from, out NavMeshHit start, 3f, NavMesh.AllAreas) || !NavMesh.SamplePosition(to, out NavMeshHit end, 6f, NavMesh.AllAreas))
        {
            return false;
        }

        NavMeshPath path = new NavMeshPath();
        Vector3 point = start.position;

        for (int repath = 0; repath < 6; repath++)
        {
            if (!NavMesh.CalculatePath(point, end.position, NavMesh.AllAreas, path) || path.corners.Length == 0)
            {
                return false;
            }

            if (path.status == NavMeshPathStatus.PathComplete)
            {
                return true;
            }

            Vector3 reached = path.corners[path.corners.Length - 1];

            if (Vector3.Distance(reached, point) < 1f)
            {
                return false;
            }

            point = reached;
        }

        return false;
    }
}
