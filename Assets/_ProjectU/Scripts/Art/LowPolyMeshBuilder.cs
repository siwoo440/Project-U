using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// 78일차: 여러 색상(Material 슬롯)을 가진 저폴리 메시를 코드로 조립하는 도구
// 모든 면은 정점을 공유하지 않아 각진(Flat Shading) 느낌으로 표시된다.
public sealed class LowPolyMeshBuilder
{
    private readonly List<Vector3> vertices = new List<Vector3>(1024);
    private readonly List<Vector3> normals = new List<Vector3>(1024);
    private readonly Dictionary<StylizedColor, List<int>> trianglesByColor =
        new Dictionary<StylizedColor, List<int>>();
    private readonly Stack<Matrix4x4> matrixStack = new Stack<Matrix4x4>();
    private Matrix4x4 currentMatrix = Matrix4x4.identity;

    public int VertexCount => vertices.Count;

    // 이후 추가하는 도형에 적용할 위치·회전·크기를 누적
    public void Push(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        matrixStack.Push(currentMatrix);
        currentMatrix = currentMatrix * Matrix4x4.TRS(position, rotation, scale);
    }

    public void Push(Vector3 position)
    {
        Push(position, Quaternion.identity, Vector3.one);
    }

    public void Pop()
    {
        if (matrixStack.Count > 0)
        {
            currentMatrix = matrixStack.Pop();
        }
    }

    public void Clear()
    {
        vertices.Clear();
        normals.Clear();
        trianglesByColor.Clear();
        matrixStack.Clear();
        currentMatrix = Matrix4x4.identity;
    }

    public void AddTriangle(StylizedColor color, Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 wa = currentMatrix.MultiplyPoint3x4(a);
        Vector3 wb = currentMatrix.MultiplyPoint3x4(b);
        Vector3 wc = currentMatrix.MultiplyPoint3x4(c);
        Vector3 normal = Vector3.Cross(wb - wa, wc - wa);

        if (normal.sqrMagnitude < 1e-12f)
        {
            return;
        }

        normal.Normalize();

        if (!trianglesByColor.TryGetValue(color, out List<int> triangles))
        {
            triangles = new List<int>(256);
            trianglesByColor.Add(color, triangles);
        }

        int start = vertices.Count;
        vertices.Add(wa);
        vertices.Add(wb);
        vertices.Add(wc);
        normals.Add(normal);
        normals.Add(normal);
        normals.Add(normal);
        triangles.Add(start);
        triangles.Add(start + 1);
        triangles.Add(start + 2);
    }

    public void AddQuad(StylizedColor color, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        AddTriangle(color, a, b, c);
        AddTriangle(color, a, c, d);
    }

    // 중심 기준 상자
    public void AddBox(StylizedColor color, Vector3 center, Vector3 size)
    {
        Vector3 h = size * 0.5f;
        Vector3 p0 = center + new Vector3(-h.x, -h.y, -h.z);
        Vector3 p1 = center + new Vector3(h.x, -h.y, -h.z);
        Vector3 p2 = center + new Vector3(h.x, -h.y, h.z);
        Vector3 p3 = center + new Vector3(-h.x, -h.y, h.z);
        Vector3 p4 = center + new Vector3(-h.x, h.y, -h.z);
        Vector3 p5 = center + new Vector3(h.x, h.y, -h.z);
        Vector3 p6 = center + new Vector3(h.x, h.y, h.z);
        Vector3 p7 = center + new Vector3(-h.x, h.y, h.z);

        AddQuad(color, p4, p7, p6, p5); // 위
        AddQuad(color, p0, p1, p2, p3); // 아래
        AddQuad(color, p0, p4, p5, p1); // 앞(-Z)
        AddQuad(color, p2, p6, p7, p3); // 뒤(+Z)
        AddQuad(color, p1, p5, p6, p2); // 오른쪽
        AddQuad(color, p3, p7, p4, p0); // 왼쪽
    }

    // 모서리를 깎은 상자 (가구·상자 등)
    public void AddBeveledBox(StylizedColor color, Vector3 center, Vector3 size, float bevel)
    {
        float b = Mathf.Clamp(bevel, 0f, Mathf.Min(size.x, size.y, size.z) * 0.45f);

        if (b <= 0.0001f)
        {
            AddBox(color, center, size);
            return;
        }

        Vector3 h = size * 0.5f;
        // 8개 모서리 각각을 3개 점으로 깎는 대신, 옆면을 한 단계 좁힌 캡 형태로 단순화
        Vector3 innerTop = new Vector3(h.x - b, h.y, h.z - b);
        Vector3 outerMidTop = new Vector3(h.x, h.y - b, h.z);
        Vector3 outerMidBottom = new Vector3(h.x, -h.y + b, h.z);
        Vector3 innerBottom = new Vector3(h.x - b, -h.y, h.z - b);

        Vector3[] top = Ring(center, innerTop);
        Vector3[] upper = Ring(center, outerMidTop);
        Vector3[] lower = Ring(center, outerMidBottom);
        Vector3[] bottom = Ring(center, innerBottom);

        AddQuad(color, top[0], top[3], top[2], top[1]);
        AddQuad(color, bottom[0], bottom[1], bottom[2], bottom[3]);
        Bridge(color, upper, top);
        Bridge(color, lower, upper);
        Bridge(color, bottom, lower);
    }

    private static Vector3[] Ring(Vector3 center, Vector3 half)
    {
        return new[]
        {
            center + new Vector3(-half.x, half.y, -half.z),
            center + new Vector3(half.x, half.y, -half.z),
            center + new Vector3(half.x, half.y, half.z),
            center + new Vector3(-half.x, half.y, half.z)
        };
    }

    // 같은 개수의 두 고리를 옆면으로 연결 (아래 고리 → 위 고리)
    private void Bridge(StylizedColor color, Vector3[] lower, Vector3[] upper)
    {
        int count = lower.Length;

        for (int index = 0; index < count; index++)
        {
            int next = (index + 1) % count;
            AddQuad(color, lower[index], upper[index], upper[next], lower[next]);
        }
    }

    // 원기둥·원뿔대 (Y축 방향, 바닥 중심 기준)
    public void AddFrustum(
        StylizedColor color,
        Vector3 baseCenter,
        float bottomRadius,
        float topRadius,
        float height,
        int sides,
        bool capBottom = true,
        bool capTop = true,
        float angleOffsetDegrees = 0f)
    {
        sides = Mathf.Max(3, sides);
        Vector3[] bottom = new Vector3[sides];
        Vector3[] top = new Vector3[sides];
        float offset = angleOffsetDegrees * Mathf.Deg2Rad;

        for (int index = 0; index < sides; index++)
        {
            float angle = offset + index * Mathf.PI * 2f / sides;
            Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            bottom[index] = baseCenter + dir * bottomRadius;
            top[index] = baseCenter + dir * topRadius + Vector3.up * height;
        }

        for (int index = 0; index < sides; index++)
        {
            int next = (index + 1) % sides;

            if (topRadius <= 0.0001f)
            {
                AddTriangle(color, bottom[index], baseCenter + Vector3.up * height, bottom[next]);
            }
            else
            {
                AddQuad(color, bottom[index], top[index], top[next], bottom[next]);
            }
        }

        if (capBottom && bottomRadius > 0.0001f)
        {
            for (int index = 0; index < sides; index++)
            {
                int next = (index + 1) % sides;
                AddTriangle(color, baseCenter, bottom[index], bottom[next]);
            }
        }

        if (capTop && topRadius > 0.0001f)
        {
            Vector3 topCenter = baseCenter + Vector3.up * height;

            for (int index = 0; index < sides; index++)
            {
                int next = (index + 1) % sides;
                AddTriangle(color, topCenter, top[next], top[index]);
            }
        }
    }

    public void AddCylinder(StylizedColor color, Vector3 baseCenter, float radius, float height, int sides)
    {
        AddFrustum(color, baseCenter, radius, radius, height, sides);
    }

    public void AddCone(StylizedColor color, Vector3 baseCenter, float radius, float height, int sides)
    {
        AddFrustum(color, baseCenter, radius, 0f, height, sides, true, false);
    }

    // 두 점 사이를 잇는 원기둥 (가지·손잡이·다리 등)
    public void AddLimb(StylizedColor color, Vector3 from, Vector3 to, float fromRadius, float toRadius, int sides)
    {
        Vector3 direction = to - from;
        float length = direction.magnitude;

        if (length < 0.0001f)
        {
            return;
        }

        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, direction / length);
        Push(from, rotation, Vector3.one);
        AddFrustum(color, Vector3.zero, fromRadius, toRadius, length, sides);
        Pop();
    }

    // 각진 구 (정팔면체를 나눈 형태). noise로 바위처럼 울퉁불퉁하게 만들 수 있다.
    public void AddLowPolySphere(
        StylizedColor color,
        Vector3 center,
        Vector3 radii,
        int subdivisions,
        float noise,
        int seed)
    {
        List<Vector3> points = new List<Vector3>
        {
            Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back
        };
        List<int> faces = new List<int>
        {
            0, 4, 3, 0, 3, 5, 0, 5, 2, 0, 2, 4,
            1, 3, 4, 1, 5, 3, 1, 2, 5, 1, 4, 2
        };

        Dictionary<long, int> midpointCache = new Dictionary<long, int>();
        subdivisions = Mathf.Clamp(subdivisions, 0, 3);

        for (int level = 0; level < subdivisions; level++)
        {
            List<int> nextFaces = new List<int>(faces.Count * 4);
            midpointCache.Clear();

            for (int index = 0; index < faces.Count; index += 3)
            {
                int a = faces[index];
                int b = faces[index + 1];
                int c = faces[index + 2];
                int ab = Midpoint(points, midpointCache, a, b);
                int bc = Midpoint(points, midpointCache, b, c);
                int ca = Midpoint(points, midpointCache, c, a);
                nextFaces.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
            }

            faces = nextFaces;
        }

        System.Random random = new System.Random(seed);
        Vector3[] displaced = new Vector3[points.Count];

        for (int index = 0; index < points.Count; index++)
        {
            float jitter = 1f + ((float)random.NextDouble() * 2f - 1f) * noise;
            Vector3 p = points[index].normalized * jitter;
            displaced[index] = center + Vector3.Scale(p, radii);
        }

        for (int index = 0; index < faces.Count; index += 3)
        {
            AddTriangle(color, displaced[faces[index]], displaced[faces[index + 1]], displaced[faces[index + 2]]);
        }
    }

    private static int Midpoint(List<Vector3> points, Dictionary<long, int> cache, int a, int b)
    {
        long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;

        if (cache.TryGetValue(key, out int existing))
        {
            return existing;
        }

        points.Add(((points[a] + points[b]) * 0.5f).normalized);
        int created = points.Count - 1;
        cache.Add(key, created);
        return created;
    }

    // 울퉁불퉁한 원뿔 (산·언덕). 높이 비율 capStart 위쪽 면은 capColor로 칠한다.
    public void AddNoisyCone(
        StylizedColor color,
        StylizedColor altColor,
        StylizedColor capColor,
        float capStart,
        Vector3 baseCenter,
        float radius,
        float height,
        int sides,
        int rings,
        float noise,
        int seed)
    {
        sides = Mathf.Max(3, sides);
        rings = Mathf.Max(1, rings);
        System.Random random = new System.Random(seed);
        Vector3[,] grid = new Vector3[rings, sides];

        for (int ring = 0; ring < rings; ring++)
        {
            float t = ring / (float)rings;
            float ringRadius = radius * Mathf.Pow(1f - t, 0.9f);
            float y = height * t;

            for (int side = 0; side < sides; side++)
            {
                float angle = (side + (ring % 2) * 0.5f) * Mathf.PI * 2f / sides;
                float radial = ringRadius * (1f + ((float)random.NextDouble() * 2f - 1f) * noise);
                float vertical = ring == 0 ? 0f : y + ((float)random.NextDouble() * 2f - 1f) * noise * height * 0.08f;
                grid[ring, side] = baseCenter + new Vector3(Mathf.Cos(angle) * radial, vertical, Mathf.Sin(angle) * radial);
            }
        }

        Vector3 apex = baseCenter + new Vector3(
            ((float)random.NextDouble() * 2f - 1f) * radius * 0.05f,
            height,
            ((float)random.NextDouble() * 2f - 1f) * radius * 0.05f);

        for (int ring = 0; ring < rings; ring++)
        {
            for (int side = 0; side < sides; side++)
            {
                int next = (side + 1) % sides;
                Vector3 a = grid[ring, side];
                Vector3 d = grid[ring, next];

                if (ring == rings - 1)
                {
                    AddTriangle(PickConeColor(a, apex, d, baseCenter, height, capStart, color, altColor, capColor, side), a, apex, d);
                    continue;
                }

                Vector3 b = grid[ring + 1, side];
                Vector3 c = grid[ring + 1, next];
                AddTriangle(PickConeColor(a, b, c, baseCenter, height, capStart, color, altColor, capColor, side + ring), a, b, c);
                AddTriangle(PickConeColor(a, c, d, baseCenter, height, capStart, color, altColor, capColor, side + ring + 1), a, c, d);
            }
        }

        for (int side = 0; side < sides; side++)
        {
            int next = (side + 1) % sides;
            AddTriangle(color, baseCenter, grid[0, side], grid[0, next]);
        }
    }

    private static StylizedColor PickConeColor(Vector3 a, Vector3 b, Vector3 c, Vector3 baseCenter, float height, float capStart, StylizedColor color, StylizedColor altColor, StylizedColor capColor, int variant)
    {
        float centroid = ((a.y + b.y + c.y) / 3f - baseCenter.y) / Mathf.Max(0.0001f, height);

        if (centroid >= capStart)
        {
            return capColor;
        }

        return variant % 3 == 0 ? altColor : color;
    }

    // 원판 (Y 위 방향)
    public void AddDisc(StylizedColor color, Vector3 center, float radius, int sides, bool doubleSided = false)
    {
        for (int index = 0; index < sides; index++)
        {
            float a0 = index * Mathf.PI * 2f / sides;
            float a1 = (index + 1) * Mathf.PI * 2f / sides;
            Vector3 p0 = center + new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)) * radius;
            Vector3 p1 = center + new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * radius;
            AddTriangle(color, center, p1, p0);

            if (doubleSided)
            {
                AddTriangle(color, center, p0, p1);
            }
        }
    }

    // 도넛 형태 (Y축 기준 고리)
    public void AddTorus(StylizedColor color, Vector3 center, float ringRadius, float tubeRadius, int ringSides, int tubeSides)
    {
        Vector3[,] grid = new Vector3[ringSides, tubeSides];

        for (int i = 0; i < ringSides; i++)
        {
            float u = i * Mathf.PI * 2f / ringSides;
            Vector3 ringDir = new Vector3(Mathf.Cos(u), 0f, Mathf.Sin(u));

            for (int j = 0; j < tubeSides; j++)
            {
                float v = j * Mathf.PI * 2f / tubeSides;
                grid[i, j] = center + ringDir * (ringRadius + Mathf.Cos(v) * tubeRadius) + Vector3.up * (Mathf.Sin(v) * tubeRadius);
            }
        }

        for (int i = 0; i < ringSides; i++)
        {
            int ni = (i + 1) % ringSides;

            for (int j = 0; j < tubeSides; j++)
            {
                int nj = (j + 1) % tubeSides;
                AddQuad(color, grid[i, j], grid[i, nj], grid[ni, nj], grid[ni, j]);
            }
        }
    }

    // 쐐기(삼각기둥) : 바닥 사각형 + 위쪽 능선 (지붕·도끼날 등)
    public void AddWedge(StylizedColor color, Vector3 center, Vector3 size)
    {
        Vector3 h = size * 0.5f;
        Vector3 b0 = center + new Vector3(-h.x, -h.y, -h.z);
        Vector3 b1 = center + new Vector3(h.x, -h.y, -h.z);
        Vector3 b2 = center + new Vector3(h.x, -h.y, h.z);
        Vector3 b3 = center + new Vector3(-h.x, -h.y, h.z);
        Vector3 r0 = center + new Vector3(-h.x, h.y, 0f);
        Vector3 r1 = center + new Vector3(h.x, h.y, 0f);

        AddQuad(color, b0, b1, b2, b3);
        AddQuad(color, b0, r0, r1, b1);
        AddQuad(color, b2, r1, r0, b3);
        AddTriangle(color, b1, r1, b2);
        AddTriangle(color, b3, r0, b0);
    }

    public Bounds CalculateBounds()
    {
        if (vertices.Count == 0)
        {
            return new Bounds(Vector3.zero, Vector3.zero);
        }

        Bounds bounds = new Bounds(vertices[0], Vector3.zero);

        for (int index = 1; index < vertices.Count; index++)
        {
            bounds.Encapsulate(vertices[index]);
        }

        return bounds;
    }

    // 색상 순서대로 SubMesh를 만들고 사용한 색상 목록을 돌려준다
    public Mesh BuildMesh(string meshName, out StylizedColor[] usedColors)
    {
        Mesh mesh = new Mesh { name = meshName };

        if (vertices.Count > 65000)
        {
            mesh.indexFormat = IndexFormat.UInt32;
        }

        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);

        List<StylizedColor> colors = new List<StylizedColor>(trianglesByColor.Keys);
        colors.Sort();
        mesh.subMeshCount = colors.Count;

        for (int index = 0; index < colors.Count; index++)
        {
            mesh.SetTriangles(trianglesByColor[colors[index]], index, false);
        }

        mesh.RecalculateBounds();
        usedColors = colors.ToArray();
        return mesh;
    }
}
