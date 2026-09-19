using System.Collections.Generic;
using UnityEngine;

// 107일차: 무인도 구역 자리 (100일차 구역을 마을 옆에서 섬 곳곳으로 옮김)
// - 구역 소품 · NPC 위치 · 적 생성 지점은 예전 좌표("설계 좌표")로 만든 뒤 구역마다 정한 만큼 통째로 옮긴다.
// - 구역 자리는 평평한 터(패드)로 다듬고, 흙길을 낸다 (Terrain 높이 · 바닥 칠하기 · NavMesh에 함께 쓰임).
// - 해안 구역은 동쪽 진짜 해변 : 설계 x 100 (예전 바닷가) = 동쪽 물가, 부두 앞은 배가 뜨는 깊은 물로 판다.
// 108일차: 흙길을 길망으로 (마을 → 구역 큰길 6 + 구역끼리 잇는 둘레길 7 + 갈림길 2), 모두 구불구불하게
// 섬 Terrain(22번 메뉴) · 구역(18번 메뉴) · 섬 자연(24번 메뉴)이 같은 값을 쓴다.
public static class IslandZoneLayout
{
    public sealed class Site
    {
        public string ZoneId; // 구역 ID (WorldZoneBuilder.Zones)
        public Vector2 Offset; // 설계 좌표 → 섬 좌표 (x, z)
        public Vector2 PadCenter; // 평평한 터 가운데 (설계 좌표)
        public Vector2 PadRadius; // 평평한 터 반지름 (x, z)
        public bool Coast; // 해안 (높이 0 · 부두 앞 깊은 물)
    }

    public enum RoadKind
    {
        Main, // 마을 → 구역 큰길 (표지판)
        Link, // 구역끼리 잇는 둘레길
        Branch // 해변 · 석호로 가는 갈림길
    }

    public sealed class Road
    {
        public string Id; // 흙길 ID (자원 저장 ID · 검사)
        public RoadKind Kind; // 종류
        public string ZoneId; // 큰길이 가는 구역
        public Vector3[] Controls; // 지나는 점 (섬 좌표) : 이 점들은 그대로 지나고, 사이만 구불구불
        public float Wiggle; // 구불구불한 정도 (m)
        public string StartName; // 둘레길 · 갈림길 시작 쪽 이름 (끝 쪽 표지판에 씀)
        public string EndName; // 끝 쪽 이름 (시작 쪽 표지판에 씀)
    }

    public const float PadBlend = 40f; // 터 가장자리가 자연 지형으로 이어지는 거리
    public const float RoadHalfWidth = 3f; // 흙길 절반 폭 (다듬는 폭)
    public const float RoadMaxGrade = 10f; // 흙길 가장 가파른 경사 (°)
    private const float RoadStep = 2f; // 흙길 높이 표 간격
    private const float RoadSmooth = 20f; // 흙길 높이를 고르게 하는 거리
    private const float RoadBend = 12f; // 구불구불한 길 점 간격
    private const float RoadReach = RoadHalfWidth + 36f + 2f; // 흙길이 둘레 땅 높이에 닿는 가장 먼 거리
    private const float GridCell = 40f; // 흙길 조각 찾기 칸
    private const float VillageHalf = 132f; // 마을 평지 (높이 0 유지)

    // 설계 x 100 = 예전 바닷가. 해안 구역은 동쪽 물가(섬 가운데에서 약 765m)에 맞춘다.
    public const float CoastSeaLineDesign = 100f;
    private static readonly Vector2 HarborZ = new Vector2(-36f, 44f); // 부두 앞 깊은 물 (설계 z)
    private const float HarborDepth = 2.1f;

    public static readonly Site[] Sites =
    {
        new Site { ZoneId = "coast", Offset = new Vector2(665f, 0f), PadCenter = new Vector2(84f, 4f), PadRadius = new Vector2(30f, 72f), Coast = true },
        new Site { ZoneId = "ruins", Offset = new Vector2(-247f, 247f), PadCenter = new Vector2(-80f, 80f), PadRadius = new Vector2(28f, 28f) },
        new Site { ZoneId = "forest", Offset = new Vector2(-242f, 82f), PadCenter = new Vector2(-88f, -32f), PadRadius = new Vector2(38f, 36f) },
        new Site { ZoneId = "snow", Offset = new Vector2(-26f, 220f), PadCenter = new Vector2(22f, 102f), PadRadius = new Vector2(56f, 26f) },
        new Site { ZoneId = "desert", Offset = new Vector2(358f, -197f), PadCenter = new Vector2(80f, -92f), PadRadius = new Vector2(44f, 36f) },
        new Site { ZoneId = "swamp", Offset = new Vector2(-260f, -146f), PadCenter = new Vector2(-60f, -96f), PadRadius = new Vector2(34f, 28f) }
    };

    // 길망 : 큰길은 마을 표지판 → 예전 입구 → 새 입구, 둘레길은 구역 터 가장자리끼리, 갈림길은 큰길 · 오솔길 위 점에서 시작
    public static readonly Road[] Roads =
    {
        new Road { Id = "coast", Kind = RoadKind.Main, ZoneId = "coast", Wiggle = 8f, Controls = new[] { new Vector3(38f, 0f, 10f), new Vector3(86f, 0f, 10f), new Vector3(94f, 0f, 58f), new Vector3(190f, 0f, 62f), new Vector3(470f, 0f, 30f), new Vector3(751f, 0f, 10f) } },
        new Road { Id = "ruins", Kind = RoadKind.Main, ZoneId = "ruins", Wiggle = 8f, Controls = new[] { new Vector3(-22f, 0f, 36f), new Vector3(-66f, 0f, 66f), new Vector3(-190f, 0f, 180f), new Vector3(-313f, 0f, 313f) } },
        new Road { Id = "forest", Kind = RoadKind.Main, ZoneId = "forest", Wiggle = 8f, Controls = new[] { new Vector3(-32f, 0f, -22f), new Vector3(-74f, 0f, -30f), new Vector3(-200f, 0f, 13f), new Vector3(-316f, 0f, 52f) } },
        new Road { Id = "snow", Kind = RoadKind.Main, ZoneId = "snow", Wiggle = 8f, Controls = new[] { new Vector3(4f, 0f, 38f), new Vector3(12f, 0f, 86f), new Vector3(-14f, 0f, 306f) } },
        new Road { Id = "desert", Kind = RoadKind.Main, ZoneId = "desert", Wiggle = 8f, Controls = new[] { new Vector3(32f, 0f, -14f), new Vector3(64f, 0f, -73f), new Vector3(240f, 0f, -170f), new Vector3(422f, 0f, -270f) } },
        new Road { Id = "swamp", Kind = RoadKind.Main, ZoneId = "swamp", Wiggle = 8f, Controls = new[] { new Vector3(-18f, 0f, -32f), new Vector3(-50f, 0f, -84f), new Vector3(-180f, 0f, -160f), new Vector3(-310f, 0f, -230f) } },
        new Road { Id = "ruins_snow", Kind = RoadKind.Link, Wiggle = 12f, StartName = "고대 폐허", EndName = "설산 기슭 · 사당", Controls = new[] { new Vector3(-292f, 0f, 332f), new Vector3(-180f, 0f, 352f), new Vector3(-64f, 0f, 328f) } },
        new Road { Id = "snow_coast", Kind = RoadKind.Link, Wiggle = 14f, StartName = "설산 기슭 · 사당", EndName = "해안 · 부두", Controls = new[] { new Vector3(54f, 0f, 326f), new Vector3(260f, 0f, 330f), new Vector3(470f, 0f, 245f), new Vector3(600f, 0f, 150f), new Vector3(735f, 0f, 82f) } },
        new Road { Id = "coast_desert", Kind = RoadKind.Link, Wiggle = 12f, StartName = "해안 · 부두", EndName = "붉은 사막", Controls = new[] { new Vector3(735f, 0f, -72f), new Vector3(640f, 0f, -160f), new Vector3(482f, 0f, -282f) } },
        new Road { Id = "desert_trail", Kind = RoadKind.Link, Wiggle = 14f, StartName = "붉은 사막", EndName = "남쪽 오솔길", Controls = new[] { new Vector3(394f, 0f, -292f), new Vector3(250f, 0f, -360f), new Vector3(90f, 0f, -420f), new Vector3(17f, 0f, -400f) } },
        new Road { Id = "trail_swamp", Kind = RoadKind.Link, Wiggle = 14f, StartName = "남쪽 오솔길", EndName = "안개 습지", Controls = new[] { new Vector3(-8f, 0f, -500f), new Vector3(-150f, 0f, -420f), new Vector3(-284f, 0f, -252f) } },
        new Road { Id = "swamp_forest", Kind = RoadKind.Link, Wiggle = 12f, StartName = "안개 습지", EndName = "깊은 숲", Controls = new[] { new Vector3(-322f, 0f, -212f), new Vector3(-352f, 0f, -90f), new Vector3(-335f, 0f, 12f) } },
        new Road { Id = "forest_ruins", Kind = RoadKind.Link, Wiggle = 12f, StartName = "깊은 숲", EndName = "고대 폐허", Controls = new[] { new Vector3(-328f, 0f, 88f), new Vector3(-362f, 0f, 200f), new Vector3(-322f, 0f, 295f) } },
        new Road { Id = "west_beach", Kind = RoadKind.Branch, Wiggle = 12f, StartName = "깊은 숲 큰길", EndName = "서쪽 해변", Controls = new[] { new Vector3(-200f, 0f, 13f), new Vector3(-420f, 0f, -50f), new Vector3(-712f, 0f, -55f) } },
        new Road { Id = "lagoon_path", Kind = RoadKind.Branch, Wiggle = 0f, StartName = "마을", EndName = "석호", Controls = new[] { new Vector3(64f, 0f, -73f), new Vector3(100f, 0f, -68f), new Vector3(140f, 0f, -58f) } }
    };

    public static int RoadCount => Roads.Length;

    private static Dictionary<string, float> padHeights; // 구역 터 높이 (자연 지형 가운데 높이)
    private static List<Vector2[]> roadPoints; // 구불구불한 흙길 점
    private static List<float[]> roadProfiles; // 흙길 높이 표 (RoadStep 간격)
    private static List<float[]> roadLengths; // 흙길 점 누적 길이
    private static Dictionary<long, List<int>> roadGrid; // 칸 → 흙길 조각 (road << 16 | segment)
    private static float[] scratchDistance; // 계산용 (흙길마다 가장 가까운 거리)
    private static float[] scratchAlong; // 계산용 (흙길마다 가장 가까운 곳의 길이 위치)

    public static Site Get(string zoneId)
    {
        foreach (Site site in Sites)
        {
            if (site.ZoneId == zoneId)
            {
                return site;
            }
        }

        return null;
    }

    public static Vector2 OffsetOf(string zoneId)
    {
        Site site = Get(zoneId);
        return site != null ? site.Offset : Vector2.zero;
    }

    public static Vector3 ToWorld(string zoneId, Vector3 design) // 설계 좌표 → 섬 좌표 (높이는 그대로)
    {
        Vector2 offset = OffsetOf(zoneId);
        return new Vector3(design.x + offset.x, design.y, design.z + offset.y);
    }

    public static Vector3 ToDesign(string zoneId, Vector3 world)
    {
        Vector2 offset = OffsetOf(zoneId);
        return new Vector3(world.x - offset.x, world.y, world.z - offset.y);
    }

    public static float PadHeight(string zoneId)
    {
        Prepare();
        return padHeights.TryGetValue(zoneId, out float height) ? height : 0f;
    }

    public static int MainRoadIndex(string zoneId) // 구역으로 가는 큰길 번호 (없으면 -1)
    {
        for (int road = 0; road < Roads.Length; road++)
        {
            if (Roads[road].Kind == RoadKind.Main && Roads[road].ZoneId == zoneId)
            {
                return road;
            }
        }

        return -1;
    }

    // ---------------------------------------------------------------- 준비 (자연 지형 기준 터 높이 · 흙길 점 · 높이 표 · 찾기 칸)

    private static float SmoothStep(float edge0, float edge1, float value)
    {
        float t = Mathf.Clamp01((value - edge0) / (edge1 - edge0));
        return t * t * (3f - 2f * t);
    }

    private static bool InVillage(Vector2 point) => Mathf.Abs(point.x) < VillageHalf && Mathf.Abs(point.y) < VillageHalf;

    private static Vector2[] BuildPolyline(Road road, int seed) // 지나는 점은 그대로, 사이는 옆으로 부드럽게 흔들림 (마을 안은 곧게)
    {
        List<Vector2> points = new List<Vector2>();
        float along = 0f;

        for (int index = 1; index < road.Controls.Length; index++)
        {
            Vector2 a = new Vector2(road.Controls[index - 1].x, road.Controls[index - 1].z);
            Vector2 b = new Vector2(road.Controls[index].x, road.Controls[index].z);
            float length = Vector2.Distance(a, b);
            Vector2 across = new Vector2(-(b - a).y, (b - a).x).normalized;
            int steps = Mathf.Max(1, Mathf.CeilToInt(length / RoadBend));

            for (int step = index == 1 ? 0 : 1; step <= steps; step++)
            {
                float u = step / (float)steps;
                Vector2 basePoint = Vector2.Lerp(a, b, u);
                float village = SmoothStep(VillageHalf, VillageHalf + 40f, Mathf.Max(Mathf.Abs(basePoint.x), Mathf.Abs(basePoint.y)));
                float noise = (Mathf.PerlinNoise((along + u * length) / 85f + seed * 3.7f, seed * 1.9f + 0.5f) - 0.5f) * 2f;
                float wave = Mathf.Sin(u * Mathf.PI); // 지나는 점에서는 0
                points.Add(basePoint + across * (road.Wiggle * noise * wave * village));
            }

            along += length;
        }

        return points.ToArray();
    }

    private static void Prepare()
    {
        if (padHeights != null && roadProfiles != null)
        {
            return;
        }

        padHeights = new Dictionary<string, float>();

        foreach (Site site in Sites)
        {
            Vector2 center = site.PadCenter + site.Offset;
            padHeights[site.ZoneId] = site.Coast ? 0f : Mathf.Round(IslandTerrainBuilder.NaturalHeightAt(center.x, center.y) * 10f) / 10f;
        }

        roadPoints = new List<Vector2[]>();
        roadProfiles = new List<float[]>();
        roadLengths = new List<float[]>();
        roadGrid = new Dictionary<long, List<int>>();
        scratchDistance = new float[Roads.Length];
        scratchAlong = new float[Roads.Length];

        for (int road = 0; road < Roads.Length; road++)
        {
            Vector2[] points = BuildPolyline(Roads[road], road + 1);
            float[] cumulative = new float[points.Length];

            for (int index = 1; index < points.Length; index++)
            {
                cumulative[index] = cumulative[index - 1] + Vector2.Distance(points[index - 1], points[index]);
            }

            float total = cumulative[points.Length - 1];
            int count = Mathf.CeilToInt(total / RoadStep) + 1;
            float[] raw = new float[count];
            bool[] village = new bool[count];

            for (int index = 0; index < count; index++)
            {
                Vector2 point = PointAlong(points, cumulative, Mathf.Min(total, index * RoadStep));
                village[index] = InVillage(point);
                raw[index] = village[index] ? 0f : PaddedHeight(point.x, point.y);
            }

            // 고르게 (앞뒤 RoadSmooth m 평균) → 마을 안은 0 → 가장 가파른 경사 제한 (앞 · 뒤로)
            int window = Mathf.RoundToInt(RoadSmooth / RoadStep);
            float[] profile = new float[count];

            for (int index = 0; index < count; index++)
            {
                float sum = 0f;
                int used = 0;

                for (int k = Mathf.Max(0, index - window); k <= Mathf.Min(count - 1, index + window); k++)
                {
                    sum += raw[k];
                    used++;
                }

                profile[index] = village[index] ? 0f : sum / used;
            }

            profile[0] = raw[0]; // 끝점은 그 자리 땅 높이 (구역 터 · 오솔길 · 큰길과 이어짐)
            profile[count - 1] = raw[count - 1];
            float maxRise = Mathf.Tan(RoadMaxGrade * Mathf.Deg2Rad) * RoadStep;

            for (int pass = 0; pass < 3; pass++)
            {
                for (int index = 1; index < count; index++)
                {
                    profile[index] = Mathf.Clamp(profile[index], profile[index - 1] - maxRise, profile[index - 1] + maxRise);
                }

                for (int index = count - 2; index >= 0; index--)
                {
                    profile[index] = Mathf.Clamp(profile[index], profile[index + 1] - maxRise, profile[index + 1] + maxRise);
                }

                for (int index = 0; index < count; index++)
                {
                    if (village[index])
                    {
                        profile[index] = 0f;
                    }
                }
            }

            roadPoints.Add(points);
            roadProfiles.Add(profile);
            roadLengths.Add(cumulative);

            // 찾기 칸 : 조각마다 닿는 범위의 칸에 등록
            for (int segment = 1; segment < points.Length; segment++)
            {
                Vector2 a = points[segment - 1];
                Vector2 b = points[segment];
                int minX = Mathf.FloorToInt((Mathf.Min(a.x, b.x) - RoadReach) / GridCell);
                int maxX = Mathf.FloorToInt((Mathf.Max(a.x, b.x) + RoadReach) / GridCell);
                int minZ = Mathf.FloorToInt((Mathf.Min(a.y, b.y) - RoadReach) / GridCell);
                int maxZ = Mathf.FloorToInt((Mathf.Max(a.y, b.y) + RoadReach) / GridCell);

                for (int cx = minX; cx <= maxX; cx++)
                {
                    for (int cz = minZ; cz <= maxZ; cz++)
                    {
                        long key = CellKey(cx, cz);

                        if (!roadGrid.TryGetValue(key, out List<int> list))
                        {
                            list = new List<int>();
                            roadGrid[key] = list;
                        }

                        list.Add(road << 16 | segment);
                    }
                }
            }
        }
    }

    private static long CellKey(int cx, int cz) => ((long)cx << 32) ^ (uint)cz;

    private static Vector2 PointAlong(Vector2[] points, float[] cumulative, float distance)
    {
        for (int index = 1; index < points.Length; index++)
        {
            if (distance <= cumulative[index] || index == points.Length - 1)
            {
                float t = Mathf.InverseLerp(cumulative[index - 1], cumulative[index], distance);
                return Vector2.Lerp(points[index - 1], points[index], t);
            }
        }

        return points[0];
    }

    // ---------------------------------------------------------------- 높이

    public static float PadWeight(Site site, float x, float z) // 이 위치가 구역 터에 얼마나 들어가는지 (1 안쪽 ~ 0 자연 지형)
    {
        Vector2 design = new Vector2(x - site.Offset.x, z - site.Offset.y) - site.PadCenter;
        float e = Mathf.Sqrt(design.x * design.x / (site.PadRadius.x * site.PadRadius.x) + design.y * design.y / (site.PadRadius.y * site.PadRadius.y));
        return 1f - SmoothStep(1f, 1f + PadBlend / Mathf.Min(site.PadRadius.x, site.PadRadius.y), e);
    }

    private static float CoastTarget(float designX, float designZ, float natural, out float weight) // 해안 구역 : 뭍은 높이 0, 부두 앞은 깊은 물
    {
        float land = 1f - SmoothStep(1f, 1f + PadBlend / 30f, Mathf.Sqrt(Mathf.Pow(Mathf.Max(0f, 70f - designX) / 30f, 2f) + Mathf.Pow(Mathf.Max(0f, Mathf.Abs(designZ - 4f) - 42f) / 30f, 2f)));
        float harborZ = 1f - SmoothStep(0f, 10f, Mathf.Max(HarborZ.x - designZ, designZ - HarborZ.y));
        float harbor = harborZ * (1f - SmoothStep(140f, 160f, designX));
        const float bank = 2.5f;
        float water = IslandTerrainBuilder.SeaLevel - 0.3f - Mathf.Min(HarborDepth, Mathf.Max(0f, designX - CoastSeaLineDesign) * 0.22f);

        if (designX <= CoastSeaLineDesign - bank) // 뭍
        {
            weight = land;
            return 0f;
        }

        if (designX <= CoastSeaLineDesign) // 물가 둑
        {
            float t = SmoothStep(CoastSeaLineDesign - bank, CoastSeaLineDesign, designX);
            weight = Mathf.Max(land * (1f - t), harbor * t);
            float target = Mathf.Lerp(0f, water, t);
            return weight > 0f ? target : natural;
        }

        weight = harbor; // 부두 앞 깊은 물 (자연 바다가 더 깊으면 그대로)
        return Mathf.Min(water, natural);
    }

    private static float PaddedHeight(float x, float z) // 자연 지형 + 구역 터
    {
        float height = IslandTerrainBuilder.NaturalHeightAt(x, z);

        foreach (Site site in Sites)
        {
            if (site.Coast)
            {
                float designX = x - site.Offset.x;
                float designZ = z - site.Offset.y;

                if (designX < 20f || designX > 170f || Mathf.Abs(designZ - 4f) > 130f)
                {
                    continue;
                }

                float target = CoastTarget(designX, designZ, height, out float coastWeight);
                height = Mathf.Lerp(height, target, coastWeight);
                continue;
            }

            float weight = PadWeight(site, x, z);

            if (weight > 0f)
            {
                height = Mathf.Lerp(height, padHeights[site.ZoneId], weight);
            }
        }

        return height;
    }

    private static bool NearestPerRoad(float x, float z) // 이 칸의 흙길마다 가장 가까운 거리 · 길이 위치 (scratch 배열), 흙길이 하나도 없으면 false
    {
        if (!roadGrid.TryGetValue(CellKey(Mathf.FloorToInt(x / GridCell), Mathf.FloorToInt(z / GridCell)), out List<int> list))
        {
            return false;
        }

        for (int road = 0; road < Roads.Length; road++)
        {
            scratchDistance[road] = float.MaxValue;
        }

        Vector2 p = new Vector2(x, z);

        foreach (int entry in list)
        {
            int road = entry >> 16;
            int segment = entry & 0xFFFF;
            Vector2 a = roadPoints[road][segment - 1];
            Vector2 b = roadPoints[road][segment];
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(0.0001f, ab.sqrMagnitude));
            float distance = Vector2.Distance(p, a + ab * t);

            if (distance < scratchDistance[road])
            {
                scratchDistance[road] = distance;
                scratchAlong[road] = roadLengths[road][segment - 1] + ab.magnitude * t;
            }
        }

        return true;
    }

    private static float ProfileAt(int road, float along)
    {
        float[] profile = roadProfiles[road];
        float sample = along / RoadStep;
        int low = Mathf.Clamp(Mathf.FloorToInt(sample), 0, profile.Length - 1);
        int high = Mathf.Min(low + 1, profile.Length - 1);
        return Mathf.Lerp(profile[low], profile[high], sample - low);
    }

    public static float Apply(float x, float z) // 섬 최종 높이 = 자연 지형 + 구역 터 + 흙길
    {
        Prepare();
        float height = PaddedHeight(x, z);

        if (InVillage(new Vector2(x, z)) || !NearestPerRoad(x, z))
        {
            return height; // 마을 평지는 그대로 (흙길 높이 0)
        }

        for (int road = 0; road < Roads.Length; road++)
        {
            float distance = scratchDistance[road];

            if (distance >= RoadReach)
            {
                continue;
            }

            float roadHeight = ProfileAt(road, scratchAlong[road]);
            float blend = Mathf.Clamp(Mathf.Abs(height - roadHeight) * 2.5f, 6f, 36f);

            if (distance >= RoadHalfWidth + blend)
            {
                continue;
            }

            height = Mathf.Lerp(height, roadHeight, 1f - SmoothStep(RoadHalfWidth, RoadHalfWidth + blend, distance));
        }

        return height;
    }

    public static float RoadDistanceWorld(Vector3 position) // 가장 가까운 흙길까지 거리 (바닥 칠하기 · 나무 정리, 멀면 매우 큰 값)
    {
        Prepare();

        if (!NearestPerRoad(position.x, position.z))
        {
            return float.MaxValue;
        }

        float best = float.MaxValue;

        for (int road = 0; road < Roads.Length; road++)
        {
            best = Mathf.Min(best, scratchDistance[road]);
        }

        return best;
    }

    public static float RoadHeightAt(int road, float distanceAlong) // 검사용 : 흙길 높이 표
    {
        Prepare();
        return ProfileAt(road, distanceAlong);
    }

    public static float RoadLength(int road)
    {
        Prepare();
        float[] cumulative = roadLengths[road];
        return cumulative[cumulative.Length - 1];
    }

    public static Vector3 RoadPoint(int road, float distanceAlong) // 검사 · 표지판 · 자원용 : 흙길 위 점 (섬 좌표, 높이 제외)
    {
        Prepare();
        Vector2 point = PointAlong(roadPoints[road], roadLengths[road], distanceAlong);
        return new Vector3(point.x, 0f, point.y);
    }
}
