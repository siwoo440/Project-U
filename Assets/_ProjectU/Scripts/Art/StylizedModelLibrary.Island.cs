using UnityEngine;

// 105일차: 무인도 시작 해변 — 난파선 · 떠밀려 온 널빤지
// 미터 단위, 뱃머리는 +Z. 난파선은 약 14 × 5 × 7m (돛대 포함), 모래에 절반쯤 묻힌 모습이다.
public static partial class StylizedModelLibrary
{
    private static void RegisterIsland()
    {
        Register("zone_shipwreck", FitMode.UniformHeight, BuildShipwreck);
        Register("zone_wreck_planks", FitMode.UniformLargest, BuildWreckPlanks);
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
}
