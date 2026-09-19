using UnityEngine;

// 112일차: 5차 NPC 새 집 — 릴리카의 고딕 저택 · 베아트리체의 벌통 오두막 · 란후아의 수련장 · 레이븐나의 성문 초소
// 미터 단위, 입구는 +Z 쪽이다. 충돌 상자는 WorldZoneBuilder가 따로 붙인다.
public static partial class StylizedModelLibrary
{
    private static void RegisterZoneHomesWave5()
    {
        Register("zone_gothic_manor", FitMode.UniformHeight, BuildGothicManor);
        Register("zone_beehive_hut", FitMode.UniformHeight, BuildBeehiveHut);
        Register("zone_training_yard", FitMode.UniformHeight, BuildTrainingYard);
        Register("zone_gate_watchpost", FitMode.UniformHeight, BuildGateWatchpost);
    }

    // 릴리카 : 안개 속 작은 고딕 저택 (검은 돌 기단 · 보랏빛 벽 · 뾰족 지붕 · 두 탑 · 아치 문 · 촛불 창 · 철 울타리 · 장미)
    private static void BuildGothicManor(LowPolyMeshBuilder b)
    {
        b.AddBox(StylizedColor.StoneDark, new Vector3(0f, 0.15f, 0f), new Vector3(4.6f, 0.3f, 3.8f));
        b.AddBox(StylizedColor.BerryPurple, new Vector3(0f, 1.6f, -0.1f), new Vector3(3.6f, 2.6f, 3.0f));
        b.AddBox(StylizedColor.StoneDark, new Vector3(0f, 2.95f, -0.1f), new Vector3(3.8f, 0.14f, 3.2f));
        b.AddWedge(StylizedColor.Black, new Vector3(0f, 3.8f, -0.1f), new Vector3(3.9f, 1.6f, 3.3f));

        // 양쪽 탑 (원기둥 + 뾰족 고깔)
        foreach (float x in new[] { -2.05f, 2.05f })
        {
            b.AddCylinder(StylizedColor.StoneDark, new Vector3(x, 0.3f, 0.9f), 0.55f, 3.6f, 8);
            b.AddCone(StylizedColor.Black, new Vector3(x, 3.9f, 0.9f), 0.7f, 1.6f, 8);
            b.AddLimb(StylizedColor.IronDark, new Vector3(x, 5.5f, 0.9f), new Vector3(x, 5.9f, 0.9f), 0.02f, 0.015f, 3);
            b.Push(new Vector3(x, 2.4f, 1.44f), Euler(90f, 0f, 0f), Vector3.one);
            b.AddCylinder(StylizedColor.LampGlow, Vector3.zero, 0.16f, 0.04f, 8);
            b.Pop();
        }

        // 아치 문 · 문 위 창 · 양옆 촛불 창
        b.AddBox(StylizedColor.WoodDark, new Vector3(0f, 0.95f, 1.42f), new Vector3(0.9f, 1.3f, 0.06f));
        b.Push(new Vector3(0f, 1.6f, 1.42f), Euler(90f, 0f, 0f), Vector3.one);
        b.AddCylinder(StylizedColor.WoodDark, Vector3.zero, 0.45f, 0.06f, 10);
        b.Pop();
        b.AddLowPolySphere(StylizedColor.Gold, new Vector3(0.28f, 1.0f, 1.47f), Vector3.one * 0.04f, 0, 0f, 11201);

        foreach (float x in new[] { -1.1f, 1.1f })
        {
            b.AddBox(StylizedColor.LampGlow, new Vector3(x, 1.9f, 1.41f), new Vector3(0.45f, 0.7f, 0.04f));
            b.AddBox(StylizedColor.Black, new Vector3(x, 1.9f, 1.43f), new Vector3(0.06f, 0.72f, 0.04f));
        }

        b.Push(new Vector3(0f, 2.55f, 1.4f), Euler(90f, 0f, 0f), Vector3.one);
        b.AddCylinder(StylizedColor.ClothRed, Vector3.zero, 0.26f, 0.04f, 10);
        b.Pop();

        // 앞 계단 · 철 울타리 · 장미 덤불
        b.AddBox(StylizedColor.StoneDark, new Vector3(0f, 0.1f, 1.85f), new Vector3(1.4f, 0.2f, 0.5f));

        for (int post = 0; post < 7; post++)
        {
            float x = -2.2f + post * 0.73f;

            if (Mathf.Abs(x) < 0.6f)
            {
                continue;
            }

            b.AddLimb(StylizedColor.IronDark, new Vector3(x, 0f, 2.45f), new Vector3(x, 0.9f, 2.45f), 0.025f, 0.02f, 4);
            b.AddCone(StylizedColor.IronDark, new Vector3(x, 0.9f, 2.45f), 0.05f, 0.14f, 4);
        }

        b.AddLimb(StylizedColor.IronDark, new Vector3(-2.2f, 0.75f, 2.45f), new Vector3(-0.6f, 0.75f, 2.45f), 0.015f, 0.015f, 3);
        b.AddLimb(StylizedColor.IronDark, new Vector3(0.6f, 0.75f, 2.45f), new Vector3(2.2f, 0.75f, 2.45f), 0.015f, 0.015f, 3);

        foreach (float x in new[] { -1.5f, 1.5f })
        {
            b.AddLowPolySphere(StylizedColor.LeafDark, new Vector3(x, 0.35f, 1.95f), new Vector3(0.45f, 0.35f, 0.3f), 1, 0.15f, 11210 + (int)(x * 10f));
            b.AddLowPolySphere(StylizedColor.ClothRed, new Vector3(x - 0.15f, 0.62f, 2.1f), Vector3.one * 0.08f, 0, 0f, 11220 + (int)(x * 10f));
            b.AddLowPolySphere(StylizedColor.ClothRed, new Vector3(x + 0.2f, 0.52f, 2.15f), Vector3.one * 0.07f, 0, 0f, 11230 + (int)(x * 10f));
        }

        // 지붕 위 박쥐 풍향계
        b.AddLimb(StylizedColor.IronDark, new Vector3(0f, 4.6f, -0.1f), new Vector3(0f, 5.2f, -0.1f), 0.02f, 0.02f, 3);
        b.AddWedge(StylizedColor.Black, new Vector3(0f, 5.25f, -0.1f), new Vector3(0.5f, 0.12f, 0.05f));
    }

    // 베아트리체 : 오아시스 옆 둥근 벌통 오두막 (짚 벽 · 둥근 지붕 · 벌집 창 · 짚 벌통 셋 · 꽃 · 표지판)
    private static void BuildBeehiveHut(LowPolyMeshBuilder b)
    {
        b.AddFrustum(StylizedColor.Sand, Vector3.zero, 1.75f, 1.6f, 2.0f, 12);
        b.AddLowPolySphere(StylizedColor.Straw, new Vector3(0f, 2.0f, 0f), new Vector3(1.85f, 1.2f, 1.85f), 2, 0.03f, 11240);

        for (int ring = 0; ring < 3; ring++)
        {
            b.AddTorus(StylizedColor.Grain, new Vector3(0f, 0.5f + ring * 0.6f, 0f), 1.7f - ring * 0.04f, 0.05f, 16, 3);
        }

        // 벌집 무늬 창 (육각 판) · 문
        foreach (Vector2 cell in new[] { new Vector2(-0.95f, 1.45f), new Vector2(-0.7f, 1.2f), new Vector2(-0.7f, 1.7f), new Vector2(0.95f, 1.45f), new Vector2(0.7f, 1.2f), new Vector2(0.7f, 1.7f) })
        {
            float z = Mathf.Sqrt(Mathf.Max(0f, 1.7f * 1.7f - cell.x * cell.x));
            b.Push(new Vector3(cell.x, cell.y, z), Quaternion.LookRotation(new Vector3(cell.x, 0f, z).normalized) * Euler(90f, 0f, 0f), Vector3.one);
            b.AddCylinder(StylizedColor.FlowerYellow, Vector3.zero, 0.14f, 0.04f, 6);
            b.Pop();
        }

        b.AddBox(StylizedColor.WoodDark, new Vector3(0f, 0.8f, 1.66f), new Vector3(0.8f, 1.5f, 0.08f));
        b.AddBox(StylizedColor.WoodPlank, new Vector3(0f, 0.04f, 2.0f), new Vector3(1.0f, 0.08f, 0.6f));

        // 짚 벌통 셋 (쌓은 고리) · 받침
        Vector3[] hives = { new Vector3(-1.9f, 0f, 1.3f), new Vector3(2.0f, 0f, 1.1f), new Vector3(2.2f, 0f, -0.4f) };

        for (int index = 0; index < hives.Length; index++)
        {
            Vector3 root = hives[index];
            b.AddBox(StylizedColor.WoodDark, root + new Vector3(0f, 0.2f, 0f), new Vector3(0.6f, 0.4f, 0.6f));

            for (int ring = 0; ring < 4; ring++)
            {
                float radius = 0.3f - ring * 0.05f;
                b.AddTorus(StylizedColor.Straw, root + new Vector3(0f, 0.48f + ring * 0.12f, 0f), radius, 0.07f, 10, 4);
            }

            b.AddLowPolySphere(StylizedColor.Straw, root + new Vector3(0f, 0.9f, 0f), new Vector3(0.14f, 0.1f, 0.14f), 0, 0f, 11250 + index);
            b.AddLowPolySphere(StylizedColor.Black, root + new Vector3(0f, 0.5f, 0.3f), new Vector3(0.06f, 0.05f, 0.03f), 0, 0f, 11260 + index);
        }

        // 꽃 · 벌 몇 마리 · 표지판
        Vector3[] flowers = { new Vector3(-1.0f, 0f, 2.2f), new Vector3(1.1f, 0f, 2.3f), new Vector3(-2.3f, 0f, 0.2f), new Vector3(-1.6f, 0f, -1.3f), new Vector3(1.4f, 0f, -1.6f) };

        for (int index = 0; index < flowers.Length; index++)
        {
            Vector3 root = flowers[index];
            b.AddLimb(StylizedColor.Leaf, root, root + new Vector3(0f, 0.4f, 0f), 0.015f, 0.012f, 4);
            b.AddLowPolySphere(index % 2 == 0 ? StylizedColor.FlowerYellow : StylizedColor.Flower, root + new Vector3(0f, 0.43f, 0f), new Vector3(0.11f, 0.05f, 0.11f), 0, 0f, 11270 + index);
        }

        foreach (Vector3 bee in new[] { new Vector3(-1.5f, 1.2f, 1.7f), new Vector3(1.7f, 1.4f, 1.6f), new Vector3(2.5f, 1.1f, -0.1f) })
        {
            b.AddLowPolySphere(StylizedColor.FlowerYellow, bee, new Vector3(0.06f, 0.05f, 0.08f), 0, 0f, 11280 + (int)(bee.x * 10f));
            b.AddLowPolySphere(StylizedColor.WingGlass, bee + new Vector3(0f, 0.05f, 0f), new Vector3(0.07f, 0.01f, 0.04f), 0, 0f, 11290 + (int)(bee.x * 10f));
        }

        b.AddLimb(StylizedColor.WoodDark, new Vector3(0.9f, 0f, 2.6f), new Vector3(0.9f, 1.1f, 2.6f), 0.035f, 0.03f, 4);
        b.AddBox(StylizedColor.WoodPlank, new Vector3(0.9f, 1.05f, 2.62f), new Vector3(0.7f, 0.35f, 0.05f));
        b.Push(new Vector3(0.9f, 1.05f, 2.66f), Euler(90f, 0f, 0f), Vector3.one);
        b.AddCylinder(StylizedColor.FlowerYellow, Vector3.zero, 0.11f, 0.02f, 6);
        b.Pop();
    }

    // 란후아 : 설산 수련장 (나무 단 · 붉은 기둥 정자 · 눈 덮인 검은 지붕 · 나무 인형 · 창 거치대 · 새끼줄 감은 기둥 · 계단)
    private static void BuildTrainingYard(LowPolyMeshBuilder b)
    {
        b.AddBox(StylizedColor.StoneDark, new Vector3(0f, 0.12f, 0f), new Vector3(5.4f, 0.24f, 4.2f));
        b.AddBox(StylizedColor.WoodPlank, new Vector3(0f, 0.34f, 0f), new Vector3(5.2f, 0.2f, 4.0f));
        b.AddBox(StylizedColor.WoodDark, new Vector3(0f, 0.12f, 2.3f), new Vector3(1.6f, 0.24f, 0.45f)); // 계단

        // 뒤쪽 정자 (붉은 기둥 넷 · 지붕)
        foreach (float x in new[] { -2.3f, 2.3f })
        {
            foreach (float z in new[] { -1.8f, -0.2f })
            {
                b.AddCylinder(StylizedColor.ClothRed, new Vector3(x, 0.44f, z), 0.1f, 2.3f, 8);
            }
        }

        b.AddBox(StylizedColor.ClothRed, new Vector3(0f, 2.7f, -1.0f), new Vector3(4.9f, 0.14f, 1.9f));
        b.AddWedge(StylizedColor.Black, new Vector3(0f, 3.1f, -1.0f), new Vector3(5.5f, 0.7f, 2.6f));
        b.AddWedge(StylizedColor.Snow, new Vector3(0f, 3.28f, -1.0f), new Vector3(5.3f, 0.42f, 2.2f));

        // 나무 인형 (몸통 · 팔 셋)
        Vector3 dummy = new Vector3(-1.2f, 0.44f, 1.0f);
        b.AddCylinder(StylizedColor.WoodDark, dummy, 0.14f, 1.7f, 8);
        b.AddLowPolySphere(StylizedColor.WoodLight, dummy + new Vector3(0f, 1.8f, 0f), Vector3.one * 0.16f, 1, 0f, 11300);

        foreach ((float height, float yaw) in new[] { (1.25f, 0f), (1.0f, 120f), (0.7f, 240f) })
        {
            Vector3 dir = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            b.AddLimb(StylizedColor.WoodLight, dummy + new Vector3(0f, height, 0f), dummy + new Vector3(0f, height, 0f) + dir * 0.5f, 0.04f, 0.035f, 5);
        }

        // 새끼줄 감은 치는 기둥
        Vector3 post = new Vector3(1.3f, 0.44f, 1.1f);
        b.AddCylinder(StylizedColor.WoodDark, post, 0.12f, 1.5f, 8);

        for (int ring = 0; ring < 5; ring++)
        {
            b.AddTorus(StylizedColor.Rope, post + new Vector3(0f, 0.55f + ring * 0.12f, 0f), 0.13f, 0.03f, 8, 3);
        }

        // 창 거치대 (창 셋)
        b.AddBox(StylizedColor.WoodDark, new Vector3(2.35f, 1.3f, 0.9f), new Vector3(0.1f, 0.1f, 1.2f));
        b.AddBox(StylizedColor.WoodDark, new Vector3(2.35f, 0.8f, 0.9f), new Vector3(0.1f, 0.1f, 1.2f));

        for (int spear = 0; spear < 3; spear++)
        {
            float z = 0.45f + spear * 0.45f;
            b.AddLimb(StylizedColor.WoodLight, new Vector3(2.45f, 0.44f, z), new Vector3(2.45f, 2.2f, z), 0.025f, 0.02f, 4);
            b.AddCone(StylizedColor.Iron, new Vector3(2.45f, 2.2f, z), 0.05f, 0.22f, 4);
        }

        // 눈 더미 · 등불
        b.AddLowPolySphere(StylizedColor.Snow, new Vector3(-2.8f, 0.1f, 1.8f), new Vector3(0.5f, 0.2f, 0.4f), 1, 0.1f, 11310);
        b.AddLimb(StylizedColor.WoodDark, new Vector3(0.9f, 2.6f, -0.2f), new Vector3(0.9f, 2.2f, -0.2f), 0.012f, 0.012f, 3);
        b.AddLowPolySphere(StylizedColor.ClothRed, new Vector3(0.9f, 2.05f, -0.2f), new Vector3(0.16f, 0.2f, 0.16f), 1, 0f, 11311);
        b.AddLowPolySphere(StylizedColor.LampGlow, new Vector3(0.9f, 2.05f, -0.2f), Vector3.one * 0.1f, 0, 0f, 11312);
    }

    // 레이븐나 : 무너진 성문 초소 (돌 탑 둘 · 반쯤 무너진 아치 · 푸른 영혼 등불 · 검은 깃발 · 검 거치대 · 돌무더기)
    private static void BuildGateWatchpost(LowPolyMeshBuilder b)
    {
        foreach (float x in new[] { -1.9f, 1.9f })
        {
            b.AddBox(StylizedColor.RuinStone, new Vector3(x, x < 0f ? 1.7f : 1.3f, -0.4f), new Vector3(1.3f, x < 0f ? 3.4f : 2.6f, 1.3f));

            for (int merlon = -1; merlon <= 1; merlon += 2)
            {
                if (x > 0f && merlon > 0)
                {
                    continue; // 오른쪽 탑은 한쪽이 무너짐
                }

                b.AddBox(StylizedColor.StoneDark, new Vector3(x + merlon * 0.4f, (x < 0f ? 3.6f : 2.8f), -0.4f), new Vector3(0.4f, 0.4f, 1.3f));
            }
        }

        // 반쯤 남은 아치
        b.AddBox(StylizedColor.RuinStone, new Vector3(-0.8f, 3.1f, -0.4f), new Vector3(1.4f, 0.5f, 1.0f));
        b.AddWedge(StylizedColor.StoneDark, new Vector3(-0.3f, 3.45f, -0.4f), new Vector3(0.8f, 0.3f, 1.0f));

        // 돌무더기
        foreach ((Vector3 position, float size) in new[] { (new Vector3(1.1f, 0.2f, 0.3f), 0.35f), (new Vector3(1.5f, 0.15f, 0.9f), 0.25f), (new Vector3(2.6f, 0.2f, 0.5f), 0.3f), (new Vector3(0.7f, 0.1f, 1.1f), 0.18f) })
        {
            b.AddLowPolySphere(StylizedColor.RuinStone, position, Vector3.one * size, 1, 0.2f, 11320 + (int)(position.x * 10f));
        }

        // 초소 : 왼쪽 탑 앞 천막 차양 · 의자
        b.AddLimb(StylizedColor.WoodDark, new Vector3(-2.6f, 0f, 1.4f), new Vector3(-2.6f, 2.0f, 1.4f), 0.05f, 0.05f, 5);
        b.AddLimb(StylizedColor.WoodDark, new Vector3(-1.2f, 0f, 1.4f), new Vector3(-1.2f, 2.0f, 1.4f), 0.05f, 0.05f, 5);
        b.Push(new Vector3(-1.9f, 2.05f, 0.75f), Euler(-18f, 0f, 0f), Vector3.one);
        b.AddBox(StylizedColor.Black, Vector3.zero, new Vector3(1.7f, 0.05f, 1.5f));
        b.Pop();
        b.AddBox(StylizedColor.WoodDark, new Vector3(-1.9f, 0.45f, 0.6f), new Vector3(0.6f, 0.1f, 0.5f));

        // 푸른 영혼 등불 (기둥 끝)
        b.AddLimb(StylizedColor.IronDark, new Vector3(0.4f, 0f, 1.6f), new Vector3(0.4f, 1.7f, 1.6f), 0.03f, 0.03f, 4);
        b.AddLimb(StylizedColor.IronDark, new Vector3(0.4f, 1.7f, 1.6f), new Vector3(0.75f, 1.7f, 1.6f), 0.02f, 0.02f, 3);
        b.AddLowPolySphere(StylizedColor.SoulFlame, new Vector3(0.75f, 1.5f, 1.6f), new Vector3(0.14f, 0.2f, 0.14f), 1, 0f, 11330);
        b.AddTorus(StylizedColor.IronDark, new Vector3(0.75f, 1.5f, 1.6f), 0.15f, 0.02f, 8, 3);

        // 검은 깃발 (왼쪽 탑 위)
        b.AddLimb(StylizedColor.WoodDark, new Vector3(-1.9f, 3.8f, -0.4f), new Vector3(-1.9f, 5.2f, -0.4f), 0.03f, 0.025f, 4);
        b.AddBox(StylizedColor.Black, new Vector3(-1.55f, 4.9f, -0.4f), new Vector3(0.7f, 0.5f, 0.03f));
        b.AddBox(StylizedColor.ClothBlue, new Vector3(-1.55f, 4.9f, -0.38f), new Vector3(0.25f, 0.25f, 0.03f));

        // 검 거치대
        b.AddBox(StylizedColor.WoodDark, new Vector3(2.4f, 0.6f, 1.3f), new Vector3(0.8f, 0.08f, 0.2f));
        b.AddLimb(StylizedColor.WoodDark, new Vector3(2.05f, 0f, 1.3f), new Vector3(2.05f, 0.65f, 1.3f), 0.03f, 0.03f, 4);
        b.AddLimb(StylizedColor.WoodDark, new Vector3(2.75f, 0f, 1.3f), new Vector3(2.75f, 0.65f, 1.3f), 0.03f, 0.03f, 4);

        foreach (float x in new[] { 2.2f, 2.55f })
        {
            b.AddLimb(StylizedColor.Iron, new Vector3(x, 0.05f, 1.35f), new Vector3(x, 1.05f, 1.35f), 0.03f, 0.01f, 4);
            b.AddBox(StylizedColor.IronDark, new Vector3(x, 0.95f, 1.35f), new Vector3(0.2f, 0.04f, 0.05f));
        }
    }
}
