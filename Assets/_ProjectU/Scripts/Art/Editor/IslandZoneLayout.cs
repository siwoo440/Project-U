using System.Collections.Generic;
using UnityEngine;

// 107일차: 무인도 구역 자리 (100일차 구역을 마을 옆에서 섬 곳곳으로 옮김)
// - 구역 소품 · NPC 위치 · 적 생성 지점은 예전 좌표("설계 좌표")로 만든 뒤 구역마다 정한 만큼 통째로 옮긴다.
// - 구역 자리는 평평한 터(패드)로 다듬고, 마을에서 구역 입구까지 흙길을 낸다 (Terrain 높이 · 바닥 칠하기 · NavMesh에 함께 쓰임).
// - 해안 구역은 동쪽 진짜 해변 : 설계 x 100 (예전 바닷가) = 동쪽 물가, 부두 앞은 배가 뜨는 깊은 물로 판다.
// 섬 Terrain(22번 메뉴)과 구역(18번 메뉴)이 같은 값을 쓴다.
public static class IslandZoneLayout
{
    public sealed class Site
    {
        public string ZoneId; // 구역 ID (WorldZoneBuilder.Zones)
        public Vector2 Offset; // 설계 좌표 → 섬 좌표 (x, z)
        public Vector2 PadCenter; // 평평한 터 가운데 (설계 좌표)
        public Vector2 PadRadius; // 평평한 터 반지름 (x, z)
        public bool Coast; // 해안 (높이 0 · 부두 앞 깊은 물)
        public Vector3[] RoadWaypoints; // 마을 길 시작 → 예전 입구 → … → 새 입구 (섬 좌표)
    }

    public const float PadBlend = 40f; // 터 가장자리가 자연 지형으로 이어지는 거리
    public const float RoadHalfWidth = 3f; // 흙길 절반 폭 (다듬는 폭)
    public const float RoadMaxGrade = 10f; // 흙길 가장 가파른 경사 (°)
    private const float RoadStep = 2f; // 흙길 높이 표 간격
    private const float RoadSmooth = 20f; // 흙길 높이를 고르게 하는 거리
    private const float VillageHalf = 132f; // 마을 평지 (높이 0 유지)

    // 설계 x 100 = 예전 바닷가. 해안 구역은 동쪽 물가(섬 가운데에서 약 765m)에 맞춘다.
    public const float CoastSeaLineDesign = 100f;
    private static readonly Vector2 HarborZ = new Vector2(-36f, 44f); // 부두 앞 깊은 물 (설계 z)
    private const float HarborDepth = 2.1f;

    public static readonly Site[] Sites =
    {
        new Site { ZoneId = "coast", Offset = new Vector2(665f, 0f), PadCenter = new Vector2(84f, 4f), PadRadius = new Vector2(30f, 72f), Coast = true,
            RoadWaypoints = new[] { new Vector3(38f, 0f, 10f), new Vector3(86f, 0f, 10f), new Vector3(94f, 0f, 58f), new Vector3(190f, 0f, 62f), new Vector3(751f, 0f, 10f) } },
        new Site { ZoneId = "ruins", Offset = new Vector2(-247f, 247f), PadCenter = new Vector2(-80f, 80f), PadRadius = new Vector2(28f, 28f),
            RoadWaypoints = new[] { new Vector3(-22f, 0f, 36f), new Vector3(-66f, 0f, 66f), new Vector3(-313f, 0f, 313f) } },
        new Site { ZoneId = "forest", Offset = new Vector2(-242f, 82f), PadCenter = new Vector2(-88f, -32f), PadRadius = new Vector2(38f, 36f),
            RoadWaypoints = new[] { new Vector3(-32f, 0f, -22f), new Vector3(-74f, 0f, -30f), new Vector3(-316f, 0f, 52f) } },
        new Site { ZoneId = "snow", Offset = new Vector2(-26f, 220f), PadCenter = new Vector2(22f, 102f), PadRadius = new Vector2(56f, 26f),
            RoadWaypoints = new[] { new Vector3(4f, 0f, 38f), new Vector3(12f, 0f, 86f), new Vector3(-14f, 0f, 306f) } },
        new Site { ZoneId = "desert", Offset = new Vector2(358f, -197f), PadCenter = new Vector2(80f, -92f), PadRadius = new Vector2(44f, 36f),
            RoadWaypoints = new[] { new Vector3(32f, 0f, -14f), new Vector3(64f, 0f, -73f), new Vector3(422f, 0f, -270f) } },
        new Site { ZoneId = "swamp", Offset = new Vector2(-260f, -146f), PadCenter = new Vector2(-60f, -96f), PadRadius = new Vector2(34f, 28f),
            RoadWaypoints = new[] { new Vector3(-18f, 0f, -32f), new Vector3(-50f, 0f, -84f), new Vector3(-310f, 0f, -230f) } }
    };

    private static Dictionary<string, float> padHeights; // 구역 터 높이 (자연 지형 가운데 높이)
    private static List<float[]> roadProfiles; // 흙길 높이 표 (RoadStep 간격)
    private static List<float[]> roadLengths; // 흙길 구간 누적 길이
    private static List<Rect> roadBounds; // 흙길 범위 (빠른 건너뛰기)

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

    // ---------------------------------------------------------------- 준비 (자연 지형 기준 터 높이 · 흙길 높이 표)

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

        roadProfiles = new List<float[]>();
        roadLengths = new List<float[]>();
        roadBounds = new List<Rect>();

        foreach (Site site in Sites)
        {
            Vector3[] points = site.RoadWaypoints;
            float[] cumulative = new float[points.Length];

            for (int index = 1; index < points.Length; index++)
            {
                cumulative[index] = cumulative[index - 1] + Flat(points[index - 1], points[index]);
            }

            float total = cumulative[points.Length - 1];
            int count = Mathf.CeilToInt(total / RoadStep) + 1;
            float[] raw = new float[count];
            bool[] village = new bool[count];

            for (int index = 0; index < count; index++)
            {
                Vector2 point = PointAlong(points, cumulative, Mathf.Min(total, index * RoadStep));
                village[index] = Mathf.Abs(point.x) < VillageHalf && Mathf.Abs(point.y) < VillageHalf;
                raw[index] = village[index] ? 0f : PaddedHeight(point.x, point.y);
            }

            // 고르게 (앞뒤 RoadSmooth m 평균) → 마을 안은 0 → 가장 가파른 경사 제한 (앞 · 뒤로 두 번)
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

            profile[count - 1] = raw[count - 1]; // 구역 입구는 터 높이
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

            float minX = float.MaxValue, minZ = float.MaxValue, maxX = float.MinValue, maxZ = float.MinValue;

            foreach (Vector3 point in points)
            {
                minX = Mathf.Min(minX, point.x);
                maxX = Mathf.Max(maxX, point.x);
                minZ = Mathf.Min(minZ, point.z);
                maxZ = Mathf.Max(maxZ, point.z);
            }

            const float margin = RoadHalfWidth + 40f;
            roadProfiles.Add(profile);
            roadLengths.Add(cumulative);
            roadBounds.Add(Rect.MinMaxRect(minX - margin, minZ - margin, maxX + margin, maxZ + margin));
        }
    }

    private static float Flat(Vector3 a, Vector3 b)
    {
        return new Vector2(a.x - b.x, a.z - b.z).magnitude;
    }

    private static Vector2 PointAlong(Vector3[] points, float[] cumulative, float distance)
    {
        for (int index = 1; index < points.Length; index++)
        {
            if (distance <= cumulative[index] || index == points.Length - 1)
            {
                float t = Mathf.InverseLerp(cumulative[index - 1], cumulative[index], distance);
                Vector3 point = Vector3.Lerp(points[index - 1], points[index], t);
                return new Vector2(point.x, point.z);
            }
        }

        return new Vector2(points[0].x, points[0].z);
    }

    // ---------------------------------------------------------------- 높이

    private static float SmoothStep(float edge0, float edge1, float value)
    {
        float t = Mathf.Clamp01((value - edge0) / (edge1 - edge0));
        return t * t * (3f - 2f * t);
    }

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

    public static float Apply(float x, float z) // 섬 최종 높이 = 자연 지형 + 구역 터 + 흙길
    {
        Prepare();
        float height = PaddedHeight(x, z);

        if (Mathf.Abs(x) < VillageHalf && Mathf.Abs(z) < VillageHalf)
        {
            return height; // 마을 평지는 그대로 (흙길 높이 0)
        }

        for (int road = 0; road < Sites.Length; road++)
        {
            if (!roadBounds[road].Contains(new Vector2(x, z)))
            {
                continue;
            }

            float distance = RoadDistance(road, x, z, out float roadHeight);
            float blend = Mathf.Clamp(Mathf.Abs(height - roadHeight) * 2.5f, 6f, 36f);

            if (distance >= RoadHalfWidth + blend)
            {
                continue;
            }

            height = Mathf.Lerp(height, roadHeight, 1f - SmoothStep(RoadHalfWidth, RoadHalfWidth + blend, distance));
        }

        return height;
    }

    private static float RoadDistance(int road, float x, float z, out float roadHeight) // 흙길까지 거리와 가장 가까운 곳의 흙길 높이
    {
        Vector3[] points = Sites[road].RoadWaypoints;
        float[] cumulative = roadLengths[road];
        float[] profile = roadProfiles[road];
        Vector2 p = new Vector2(x, z);
        float best = float.MaxValue;
        float along = 0f;

        for (int index = 1; index < points.Length; index++)
        {
            Vector2 a = new Vector2(points[index - 1].x, points[index - 1].z);
            Vector2 b = new Vector2(points[index].x, points[index].z);
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            float distance = Vector2.Distance(p, a + ab * t);

            if (distance < best)
            {
                best = distance;
                along = cumulative[index - 1] + ab.magnitude * t;
            }
        }

        float sample = along / RoadStep;
        int low = Mathf.Clamp(Mathf.FloorToInt(sample), 0, profile.Length - 1);
        int high = Mathf.Min(low + 1, profile.Length - 1);
        roadHeight = Mathf.Lerp(profile[low], profile[high], sample - low);
        return best;
    }

    public static float RoadDistanceWorld(Vector3 position) // 가장 가까운 흙길까지 거리 (바닥 칠하기 · 나무 정리)
    {
        Prepare();
        float best = float.MaxValue;

        for (int road = 0; road < Sites.Length; road++)
        {
            if (!roadBounds[road].Contains(new Vector2(position.x, position.z)))
            {
                continue;
            }

            best = Mathf.Min(best, RoadDistance(road, position.x, position.z, out _));
        }

        return best;
    }

    public static float RoadHeightAt(int road, float distanceAlong) // 검사용 : 흙길 높이 표
    {
        Prepare();
        float[] profile = roadProfiles[road];
        float sample = distanceAlong / RoadStep;
        int low = Mathf.Clamp(Mathf.FloorToInt(sample), 0, profile.Length - 1);
        int high = Mathf.Min(low + 1, profile.Length - 1);
        return Mathf.Lerp(profile[low], profile[high], sample - low);
    }

    public static float RoadLength(int road)
    {
        Prepare();
        float[] cumulative = roadLengths[road];
        return cumulative[cumulative.Length - 1];
    }

    public static Vector3 RoadPoint(int road, float distanceAlong) // 검사 · 표지판용 : 흙길 위 점 (섬 좌표, 높이 제외)
    {
        Prepare();
        Vector2 point = PointAlong(Sites[road].RoadWaypoints, roadLengths[road], distanceAlong);
        return new Vector3(point.x, 0f, point.y);
    }
}
