using UnityEngine;

// 105일차: 무인도 시작 해변 — 난파선 · 떠밀려 온 널빤지
// 미터 단위, 뱃머리는 +Z. 난파선은 약 14 × 5 × 7m (돛대 포함), 모래에 절반쯤 묻힌 모습이다.
// 106일차: 바닷속 — 해초 (약 2.4m) · 산호 무더기 (약 1.3m)
public static partial class StylizedModelLibrary
{
    private static void RegisterIsland()
    {
        Register("zone_shipwreck", FitMode.UniformHeight, BuildShipwreck);
        Register("zone_wreck_planks", FitMode.UniformLargest, BuildWreckPlanks);
        Register("zone_kelp", FitMode.UniformHeight, BuildKelp);
        Register("zone_coral", FitMode.UniformHeight, BuildCoral);
    }

    // 부서진 범선 : 오른쪽(+X) 옆구리가 뜯겨 갈비뼈가 드러나고, 돛대는 부러져 비스듬히 누웠다
    private static void BuildShipwreck(LowPolyMeshBuilder b)
    {
        const float length = 13f;
        const float halfWidth = 2.4f;
        const int sections = 9;

        // 선체 : 앞뒤로 좁아지는 단면을 이어 붙인다 (오른쪽 가운데 세 칸은 뚫림)
        Vector3[] Section(float t)
        {
            float z = (t - 0.5f) * length;
            float taper = Mathf.Sin(Mathf.Clamp01(t * 0.92f + 0.04f) * Mathf.PI);
            float width = Mathf.Lerp(0.35f, halfWidth, Mathf.Pow(taper, 0.6f));
            float keel = Mathf.Lerp(0.9f, 0f, taper) * 0.6f;
            float deck = 2.2f + Mathf.Pow(Mathf.Abs(t - 0.5f) * 2f, 2f) * 0.8f;
            return new[]
            {
                new Vector3(-width, deck, z),
                new Vector3(-width * 0.92f, 1.1f, z),
                new Vector3(-width * 0.45f, 0.25f + keel, z),
                new Vector3(0f, keel, z),
                new Vector3(width * 0.45f, 0.25f + keel, z),
                new Vector3(width * 0.92f, 1.1f, z),
                new Vector3(width, deck, z)
            };
        }

        for (int index = 0; index < sections; index++)
        {
            Vector3[] a = Section(index / (float)sections);
            Vector3[] c = Section((index + 1) / (float)sections);
            bool broken = index >= 3 && index <= 5;

            for (int edge = 0; edge < a.Length - 1; edge++)
            {
                if (broken && edge >= 4)
                {
                    continue; // 뜯겨 나간 오른쪽 옆구리
                }

                StylizedColor color = edge == 0 || edge == a.Length - 2 ? StylizedColor.WoodDark : (index + edge) % 3 == 0 ? StylizedColor.WoodLight : StylizedColor.WoodPlank;
                b.AddQuad(color, a[edge], c[edge], c[edge + 1], a[edge + 1]);
                b.AddQuad(StylizedColor.WoodDark, a[edge + 1], c[edge + 1], c[edge], a[edge]); // 안쪽 면
            }

            if (broken)
            {
                // 드러난 갈비뼈 (둥근 늑재)
                Vector3[] ribs = Section((index + 0.5f) / sections);

                for (int edge = 3; edge < ribs.Length - 1; edge++)
                {
                    b.AddLimb(StylizedColor.WoodDark, ribs[edge], ribs[edge + 1] + new Vector3(0.1f, index == 4 ? -0.4f : 0f, 0f), 0.09f, 0.08f, 5);
                }
            }
        }

        // 갑판 (앞쪽 절반만 남음) · 뱃머리 기둥 · 선미 난간
        b.AddBox(StylizedColor.WoodPlank, new Vector3(-0.3f, 2.25f, 3.2f), new Vector3(3.6f, 0.12f, 5.2f));
        b.AddLimb(StylizedColor.WoodDark, new Vector3(0f, 2.6f, length * 0.5f - 0.2f), new Vector3(0f, 3.6f, length * 0.5f + 1.4f), 0.16f, 0.1f, 6);
        b.AddBox(StylizedColor.WoodDark, new Vector3(0f, 3.1f, -length * 0.5f + 0.6f), new Vector3(2.4f, 0.8f, 0.2f));

        // 부러진 돛대 : 밑동은 서 있고, 윗부분은 옆으로 누워 모래에 닿음
        b.AddCylinder(StylizedColor.WoodDark, new Vector3(0f, 1.0f, 0.8f), 0.22f, 3.3f, 8);
        b.AddLimb(StylizedColor.WoodDark, new Vector3(0.2f, 4.1f, 0.8f), new Vector3(4.6f, 0.4f, -2.8f), 0.18f, 0.13f, 7);
        b.AddLimb(StylizedColor.WoodDark, new Vector3(2.8f, 2.0f, -1.3f), new Vector3(2.0f, 3.1f, -3.9f), 0.08f, 0.07f, 5); // 활대

        // 찢어진 돛 (활대에서 늘어짐)
        b.AddQuad(StylizedColor.ClothCream, new Vector3(2.9f, 2.0f, -1.4f), new Vector3(2.1f, 3.0f, -3.8f), new Vector3(3.4f, 0.6f, -3.6f), new Vector3(4.0f, 0.5f, -1.9f));
        b.AddQuad(StylizedColor.ClothCream, new Vector3(4.0f, 0.5f, -1.9f), new Vector3(3.4f, 0.6f, -3.6f), new Vector3(2.1f, 3.0f, -3.8f), new Vector3(2.9f, 2.0f, -1.4f));
        b.AddTriangle(StylizedColor.ClothCream, new Vector3(3.4f, 0.6f, -3.6f), new Vector3(4.4f, 0.1f, -4.2f), new Vector3(4.0f, 0.5f, -1.9f));

        // 밧줄 · 매달린 등불 · 선체 틈 해초
        b.AddLimb(StylizedColor.Rope, new Vector3(0f, 3.6f, length * 0.5f + 1.3f), new Vector3(1.5f, 0.2f, length * 0.5f + 1.5f), 0.03f, 0.03f, 4);
        b.AddLimb(StylizedColor.Rope, new Vector3(0.1f, 4.0f, 0.8f), new Vector3(-2.2f, 2.2f, -2.5f), 0.025f, 0.025f, 4);
        b.AddLimb(StylizedColor.IronDark, new Vector3(-2.2f, 2.9f, 2.4f), new Vector3(-2.2f, 2.5f, 2.4f), 0.015f, 0.015f, 3);
        b.AddFrustum(StylizedColor.IronDark, new Vector3(-2.2f, 2.2f, 2.4f), 0.12f, 0.1f, 0.3f, 6);
        b.AddLowPolySphere(StylizedColor.LampGlow, new Vector3(-2.2f, 2.35f, 2.4f), Vector3.one * 0.08f, 0, 0f, 10501);

        for (int index = 0; index < 5; index++)
        {
            b.AddLowPolySphere(StylizedColor.LeafDark, new Vector3(-1.8f + index * 0.9f, 0.35f + (index % 2) * 0.2f, -5.5f + index * 2.4f), new Vector3(0.35f, 0.12f, 0.25f), 1, 0.2f, 10510 + index);
        }

        // 쏟아진 상자 · 나무통 (배 옆 모래 위)
        b.AddBox(StylizedColor.WoodLight, new Vector3(3.6f, 0.35f, 1.8f), new Vector3(0.8f, 0.7f, 0.8f));
        b.AddBox(StylizedColor.WoodDark, new Vector3(3.6f, 0.72f, 1.8f), new Vector3(0.82f, 0.06f, 0.82f));
        b.Push(new Vector3(3.9f, 0.3f, 3.2f), Euler(0f, 35f, 90f), Vector3.one);
        b.AddFrustum(StylizedColor.WoodPlank, new Vector3(0f, -0.4f, 0f), 0.3f, 0.3f, 0.8f, 8);
        b.Pop();
    }

    // 떠밀려 온 널빤지 더미 (부서진 판자 · 밧줄 매듭)
    private static void BuildWreckPlanks(LowPolyMeshBuilder b)
    {
        float[] yaws = { 8f, -24f, 41f, -6f };

        for (int index = 0; index < yaws.Length; index++)
        {
            b.Push(new Vector3(0f, 0.05f + index * 0.07f, 0f), Euler(0f, yaws[index], index == 2 ? 6f : 0f), Vector3.one);
            b.AddBox(index % 2 == 0 ? StylizedColor.WoodPlank : StylizedColor.WoodLight, Vector3.zero, new Vector3(0.28f, 0.06f, 1.9f - index * 0.25f));
            b.Pop();
        }

        b.AddTorus(StylizedColor.Rope, new Vector3(0.2f, 0.12f, 0.3f), 0.18f, 0.035f, 8, 3);
        b.AddBox(StylizedColor.IronDark, new Vector3(-0.1f, 0.3f, -0.5f), new Vector3(0.05f, 0.02f, 0.12f));
    }

    // 해초 : 바닥에서 물결치며 올라가는 줄기 5개 + 잎
    private static void BuildKelp(LowPolyMeshBuilder b)
    {
        System.Random random = new System.Random(10601);

        for (int strand = 0; strand < 5; strand++)
        {
            float angle = strand * 72f + Rand(random, -20f, 20f);
            Vector3 root = Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, 0f, Rand(random, 0.05f, 0.35f));
            float height = Rand(random, 1.6f, 2.6f);
            float phase = Rand(random, 0f, 6.28f);
            Vector3 previous = root;
            const int segments = 6;

            for (int segment = 1; segment <= segments; segment++)
            {
                float t = segment / (float)segments;
                Vector3 point = root + new Vector3(Mathf.Sin(phase + t * 4.2f) * 0.22f * t, height * t, Mathf.Cos(phase + t * 3.1f) * 0.16f * t);
                b.AddLimb(segment % 2 == 0 ? StylizedColor.LeafDark : StylizedColor.Leaf, previous, point, 0.045f * (1.1f - t * 0.5f), 0.04f * (1.1f - t * 0.6f), 5);

                if (segment < segments)
                {
                    Vector3 side = new Vector3(Mathf.Cos(phase + segment), 0f, Mathf.Sin(phase + segment)) * 0.14f;
                    b.AddLowPolySphere(StylizedColor.Leaf, point + side, new Vector3(0.16f, 0.05f, 0.09f), 0, 0f, 10620 + strand * 10 + segment);
                }

                previous = point;
            }

            b.AddLowPolySphere(StylizedColor.LeafLight, previous + Vector3.up * 0.05f, new Vector3(0.12f, 0.09f, 0.12f), 0, 0.1f, 10690 + strand);
        }

        b.AddLowPolySphere(StylizedColor.StoneDark, new Vector3(0f, 0.05f, 0f), new Vector3(0.45f, 0.12f, 0.4f), 1, 0.2f, 10699); // 붙어 있는 돌
    }

    // 산호 무더기 : 가지 산호 · 둥근 뇌 산호 · 부채 산호 · 작은 조개
    private static void BuildCoral(LowPolyMeshBuilder b)
    {
        System.Random random = new System.Random(10602);
        b.AddLowPolySphere(StylizedColor.Stone, new Vector3(0f, 0.08f, 0f), new Vector3(0.75f, 0.2f, 0.6f), 1, 0.25f, 10700); // 바위 받침

        // 가지 산호 (분홍)
        for (int branch = 0; branch < 7; branch++)
        {
            float angle = branch * 51f;
            Vector3 root = Quaternion.Euler(0f, angle, 0f) * new Vector3(0.18f, 0.2f, 0f) + new Vector3(-0.2f, 0f, 0.1f);
            Vector3 tip = root + Quaternion.Euler(0f, angle, 0f) * new Vector3(Rand(random, 0.1f, 0.3f), Rand(random, 0.55f, 0.95f), 0f);
            b.AddLimb(StylizedColor.Coral, root, tip, 0.06f, 0.035f, 5);
            Vector3 fork = Vector3.Lerp(root, tip, 0.55f);
            b.AddLimb(StylizedColor.Coral, fork, fork + Quaternion.Euler(0f, angle + 70f, 0f) * new Vector3(0.18f, 0.3f, 0f), 0.035f, 0.025f, 4);
            b.AddLowPolySphere(StylizedColor.Coral, tip, Vector3.one * 0.05f, 0, 0f, 10710 + branch);
        }

        // 둥근 뇌 산호 (노랑)
        b.AddLowPolySphere(StylizedColor.FlowerYellow, new Vector3(0.38f, 0.3f, -0.2f), new Vector3(0.3f, 0.24f, 0.28f), 1, 0.12f, 10730);

        // 부채 산호 (보라 · 비늘빛)
        b.Push(new Vector3(0.15f, 0.25f, 0.35f), Euler(0f, 25f, 0f), Vector3.one);
        b.AddTriangle(StylizedColor.MermaidScale, new Vector3(0f, 0f, 0f), new Vector3(-0.35f, 0.75f, 0f), new Vector3(0.35f, 0.75f, 0f));
        b.AddTriangle(StylizedColor.MermaidScale, new Vector3(0.35f, 0.75f, 0f), new Vector3(-0.35f, 0.75f, 0f), new Vector3(0f, 0f, 0f));
        b.AddLimb(StylizedColor.BerryPurple, Vector3.zero, new Vector3(0f, 0.7f, 0.01f), 0.02f, 0.015f, 4);
        b.Pop();

        // 작은 조개 · 불가사리
        b.AddLowPolySphere(StylizedColor.White, new Vector3(-0.5f, 0.2f, -0.25f), new Vector3(0.1f, 0.05f, 0.08f), 0, 0f, 10740);

        for (int arm = 0; arm < 5; arm++)
        {
            Vector3 center = new Vector3(0.55f, 0.18f, 0.2f);
            b.AddLimb(StylizedColor.Pumpkin, center, center + Quaternion.Euler(0f, arm * 72f, 0f) * new Vector3(0.14f, 0f, 0f), 0.035f, 0.012f, 4);
        }
    }
}
