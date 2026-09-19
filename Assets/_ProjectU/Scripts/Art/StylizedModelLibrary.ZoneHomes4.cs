using UnityEngine;

// 110일차: 4차 NPC 새 집 — 리제의 정비 캡슐 · 샤리아의 구조대 망루 · 켄시아의 운송대 마구간
// 미터 단위, 입구는 +Z 쪽이다. 충돌 상자는 WorldZoneBuilder가 따로 붙인다.
public static partial class StylizedModelLibrary
{
    private static void RegisterZoneHomesWave4()
    {
        Register("zone_repair_pod", FitMode.UniformHeight, BuildRepairPod);
        Register("zone_lifeguard_tower", FitMode.UniformHeight, BuildLifeguardTower);
        Register("zone_caravan_stable", FitMode.UniformHeight, BuildCaravanStable);
    }

    // 리제 : 옛 문명의 정비 캡슐 (돌 받침 위에 비스듬히 누운 금속 캡슐 · 열린 유리 뚜껑 · 푸른 불빛 · 전선 · 단말기 · 이끼)
    private static void BuildRepairPod(LowPolyMeshBuilder b)
    {
        // 무너진 돌 받침
        b.AddBox(StylizedColor.RuinStone, new Vector3(0f, 0.15f, -0.2f), new Vector3(3.4f, 0.3f, 2.2f));
        b.AddBox(StylizedColor.StoneDark, new Vector3(-1.2f, 0.42f, -0.2f), new Vector3(0.5f, 0.3f, 1.6f));
        b.AddBox(StylizedColor.StoneDark, new Vector3(1.2f, 0.42f, -0.2f), new Vector3(0.5f, 0.3f, 1.6f));

        // 캡슐 몸통 (옆으로 누워 머리 쪽이 조금 올라감)
        b.AddLimb(StylizedColor.Iron, new Vector3(-1.35f, 1.05f, -0.2f), new Vector3(1.35f, 1.25f, -0.2f), 0.72f, 0.66f, 10);
        b.AddLowPolySphere(StylizedColor.Iron, new Vector3(-1.35f, 1.05f, -0.2f), new Vector3(0.55f, 0.72f, 0.72f), 1, 0f, 11001);
        b.AddLowPolySphere(StylizedColor.Iron, new Vector3(1.35f, 1.25f, -0.2f), new Vector3(0.5f, 0.66f, 0.66f), 1, 0f, 11002);

        foreach (float x in new[] { -0.8f, 0f, 0.8f })
        {
            b.Push(new Vector3(x, 1.05f + (x + 1.35f) / 2.7f * 0.2f, -0.2f), Euler(0f, 0f, 90f + 4.2f), Vector3.one);
            b.AddTorus(StylizedColor.IronDark, Vector3.zero, 0.73f, 0.05f, 12, 3);
            b.Pop();
        }

        // 앞쪽(+Z) 안이 보이는 입구와 들어 올린 유리 뚜껑
        b.AddBox(StylizedColor.Black, new Vector3(0f, 1.2f, 0.45f), new Vector3(2.1f, 0.5f, 0.1f));
        b.AddBox(StylizedColor.ClothCream, new Vector3(0f, 1.05f, 0.35f), new Vector3(1.9f, 0.12f, 0.25f));
        b.Push(new Vector3(0f, 1.78f, 0.3f), Euler(-55f, 0f, 0f), Vector3.one);
        b.AddBox(StylizedColor.Glass, new Vector3(0f, 0f, 0.4f), new Vector3(2.0f, 0.05f, 0.8f));
        b.AddBox(StylizedColor.IronDark, new Vector3(0f, 0.02f, 0.8f), new Vector3(2.05f, 0.07f, 0.06f));
        b.Pop();

        // 옆면 푸른 불빛 · 머리 쪽 표시등
        for (int index = 0; index < 5; index++)
        {
            float x = -1.1f + index * 0.55f;
            b.AddLowPolySphere(StylizedColor.RuneGlow, new Vector3(x, 1.2f + index * 0.04f, 0.5f), Vector3.one * 0.05f, 0, 0f, 11010 + index);
        }

        b.AddLowPolySphere(StylizedColor.RuneGlow, new Vector3(1.85f, 1.35f, -0.2f), new Vector3(0.05f, 0.18f, 0.18f), 1, 0f, 11016);

        // 안테나
        b.AddLimb(StylizedColor.IronDark, new Vector3(1.2f, 1.85f, -0.5f), new Vector3(1.35f, 2.75f, -0.6f), 0.03f, 0.02f, 4);
        b.AddLowPolySphere(StylizedColor.LampGlow, new Vector3(1.35f, 2.8f, -0.6f), Vector3.one * 0.07f, 0, 0f, 11017);

        // 전선 (땅으로) · 단말기
        b.AddLimb(StylizedColor.Black, new Vector3(-1.6f, 0.9f, -0.4f), new Vector3(-1.9f, 0.05f, 0.2f), 0.04f, 0.04f, 4);
        b.AddLimb(StylizedColor.Black, new Vector3(1.55f, 0.95f, 0.1f), new Vector3(1.9f, 0.3f, 0.75f), 0.035f, 0.035f, 4);
        b.AddBox(StylizedColor.IronDark, new Vector3(1.95f, 0.45f, 0.85f), new Vector3(0.5f, 0.9f, 0.35f));
        b.Push(new Vector3(1.95f, 0.8f, 1.03f), Euler(-20f, 0f, 0f), Vector3.one);
        b.AddBox(StylizedColor.Crystal, Vector3.zero, new Vector3(0.38f, 0.26f, 0.03f));
        b.Pop();

        // 오래된 흔적 : 이끼 · 덩굴 · 떨어진 부품
        b.AddLowPolySphere(StylizedColor.LeafDark, new Vector3(-1.5f, 0.35f, 0.75f), new Vector3(0.35f, 0.14f, 0.25f), 1, 0.2f, 11020);
        b.AddLowPolySphere(StylizedColor.Leaf, new Vector3(0.9f, 0.34f, -1.15f), new Vector3(0.4f, 0.12f, 0.2f), 1, 0.2f, 11021);
        b.AddLimb(StylizedColor.LeafDark, new Vector3(-0.9f, 0.3f, -1.0f), new Vector3(-0.6f, 1.6f, -0.75f), 0.025f, 0.02f, 4);
        b.AddLimb(StylizedColor.LeafDark, new Vector3(-0.6f, 1.6f, -0.75f), new Vector3(-0.1f, 1.9f, -0.35f), 0.02f, 0.015f, 4);
        b.AddBox(StylizedColor.Iron, new Vector3(-0.6f, 0.05f, 1.0f), new Vector3(0.3f, 0.08f, 0.2f));
        b.AddTorus(StylizedColor.IronDark, new Vector3(0.4f, 0.04f, 1.15f), 0.12f, 0.03f, 8, 3);
    }

    // 샤리아 : 바닷가 구조대 망루 (네 기둥 · 발판 · 바다 쪽(+X)으로 트인 빨간 줄무늬 망루 · 사다리(+Z) · 구명 튜브 · 깃발 · 구조 보드)
    private static void BuildLifeguardTower(LowPolyMeshBuilder b)
    {
        const float deck = 2.2f;

        // 기둥 · 버팀대
        foreach (float x in new[] { -1.2f, 1.2f })
        {
            foreach (float z in new[] { -1.2f, 1.2f })
            {
                b.AddCylinder(StylizedColor.WoodLight, new Vector3(x, 0f, z), 0.1f, deck, 6);
            }

            b.AddLimb(StylizedColor.WoodDark, new Vector3(x, 0.2f, -1.2f), new Vector3(x, deck - 0.2f, 1.2f), 0.04f, 0.04f, 4);
        }

        b.AddLimb(StylizedColor.WoodDark, new Vector3(-1.2f, 0.25f, -1.2f), new Vector3(1.2f, deck - 0.25f, -1.2f), 0.04f, 0.04f, 4);

        // 발판 · 난간 (사다리 자리는 비움)
        b.AddBox(StylizedColor.WoodPlank, new Vector3(0f, deck, 0f), new Vector3(2.7f, 0.12f, 2.7f));

        foreach ((Vector3 from, Vector3 to) in new[]
        {
            (new Vector3(1.3f, deck, -1.3f), new Vector3(1.3f, deck, 1.3f)),
            (new Vector3(-1.3f, deck, 1.3f), new Vector3(-0.4f, deck, 1.3f)),
            (new Vector3(0.4f, deck, 1.3f), new Vector3(1.3f, deck, 1.3f))
        })
        {
            b.AddLimb(StylizedColor.White, from + Vector3.up * 0.9f, to + Vector3.up * 0.9f, 0.035f, 0.035f, 4);
            b.AddLimb(StylizedColor.White, from + Vector3.up * 0.45f, to + Vector3.up * 0.45f, 0.03f, 0.03f, 4);
            b.AddLimb(StylizedColor.White, from, from + Vector3.up * 0.9f, 0.04f, 0.04f, 4);
            b.AddLimb(StylizedColor.White, to, to + Vector3.up * 0.9f, 0.04f, 0.04f, 4);
        }

        // 망루 오두막 (뒤쪽 -X 벽 · 옆벽 · 빨간 줄무늬 · 지붕)
        b.AddBox(StylizedColor.White, new Vector3(-0.95f, deck + 0.8f, 0f), new Vector3(0.12f, 1.5f, 2.2f));

        foreach (float z in new[] { -1.05f, 1.05f })
        {
            b.AddBox(StylizedColor.White, new Vector3(-0.45f, deck + 0.8f, z), new Vector3(1.1f, 1.5f, 0.1f));
            b.AddBox(StylizedColor.ClothRed, new Vector3(-0.45f, deck + 0.9f, z * 1.01f), new Vector3(1.12f, 0.28f, 0.11f));
        }

        b.AddBox(StylizedColor.ClothRed, new Vector3(-0.96f, deck + 0.9f, 0f), new Vector3(0.13f, 0.28f, 2.22f));
        b.Push(new Vector3(0f, deck + 1.72f, 0f), Euler(0f, 0f, 8f), Vector3.one);
        b.AddBox(StylizedColor.ClothRed, Vector3.zero, new Vector3(3.0f, 0.12f, 2.9f));
        b.AddBox(StylizedColor.White, new Vector3(0f, -0.1f, 0f), new Vector3(2.9f, 0.08f, 2.8f));
        b.Pop();

        // 망루 의자 (바다 쪽을 봄)
        b.AddBox(StylizedColor.WoodDark, new Vector3(-0.4f, deck + 0.45f, 0f), new Vector3(0.5f, 0.08f, 0.55f));
        b.AddBox(StylizedColor.WoodDark, new Vector3(-0.65f, deck + 0.75f, 0f), new Vector3(0.08f, 0.6f, 0.55f));
        b.AddLimb(StylizedColor.WoodDark, new Vector3(-0.4f, deck, 0f), new Vector3(-0.4f, deck + 0.45f, 0f), 0.05f, 0.05f, 4);

        // 사다리 (+Z)
        for (int rail = -1; rail <= 1; rail += 2)
        {
            b.AddLimb(StylizedColor.WoodDark, new Vector3(rail * 0.3f, 0f, 2.15f), new Vector3(rail * 0.3f, deck + 0.3f, 1.3f), 0.04f, 0.04f, 5);
        }

        for (int rung = 0; rung < 7; rung++)
        {
            float t = (rung + 0.6f) / 7.2f;
            b.AddLimb(StylizedColor.WoodLight, new Vector3(-0.3f, t * (deck + 0.3f), 2.15f - t * 0.85f), new Vector3(0.3f, t * (deck + 0.3f), 2.15f - t * 0.85f), 0.025f, 0.025f, 4);
        }

        // 구명 튜브 (빨강 · 흰색) : 난간에 하나, 기둥에 하나
        AddLifebuoy(b, new Vector3(0.9f, deck + 0.55f, 1.33f), Euler(90f, 0f, 0f));
        AddLifebuoy(b, new Vector3(1.32f, 1.0f, -1.2f), Euler(0f, 0f, 90f));

        // 깃대 · 빨간 깃발
        b.AddLimb(StylizedColor.WoodLight, new Vector3(-1.2f, deck + 1.8f, -1.2f), new Vector3(-1.2f, deck + 3.1f, -1.2f), 0.03f, 0.025f, 4);
        b.AddBox(StylizedColor.ClothRed, new Vector3(-0.95f, deck + 2.9f, -1.2f), new Vector3(0.5f, 0.32f, 0.02f));

        // 기둥에 기댄 구조 보드 · 모래 위 부표
        b.Push(new Vector3(-1.45f, 0f, 0.4f), Euler(0f, 0f, 12f), Vector3.one);
        b.AddBeveledBox(StylizedColor.ClothRed, new Vector3(0f, 1.0f, 0f), new Vector3(0.1f, 2.0f, 0.5f), 0.04f);
        b.AddBox(StylizedColor.White, new Vector3(0.06f, 1.0f, 0f), new Vector3(0.02f, 1.6f, 0.12f));
        b.Pop();
        b.AddLowPolySphere(StylizedColor.ClothRed, new Vector3(1.8f, 0.2f, 1.6f), Vector3.one * 0.22f, 1, 0f, 11030);
        b.AddLimb(StylizedColor.Rope, new Vector3(1.8f, 0.2f, 1.6f), new Vector3(1.2f, 0.4f, 1.2f), 0.015f, 0.015f, 3);
    }

    private static void AddLifebuoy(LowPolyMeshBuilder b, Vector3 center, Quaternion rotation)
    {
        b.Push(center, rotation, Vector3.one);

        for (int part = 0; part < 4; part++)
        {
            float angle = part * 90f + 45f;
            b.Push(Vector3.zero, Euler(0f, angle, 0f), Vector3.one);
            b.AddLimb(part % 2 == 0 ? StylizedColor.ClothRed : StylizedColor.White, new Vector3(-0.19f, 0f, 0.19f), new Vector3(0.19f, 0f, 0.19f), 0.07f, 0.07f, 5);
            b.Pop();
        }

        b.Pop();
    }

    // 켄시아 : 사막 운송대 마구간 (앞(+Z)이 트인 큰 천막 · 줄무늬 천 지붕 · 건초 · 물통 · 짐 상자 · 짐수레 · 등불 · 편자 간판)
    private static void BuildCaravanStable(LowPolyMeshBuilder b)
    {
        const float height = 3.1f;

        // 모래 위 널빤지 바닥
        b.AddBox(StylizedColor.WoodPlank, new Vector3(0f, 0.04f, 0f), new Vector3(5.8f, 0.08f, 3.8f));

        // 기둥 여섯
        foreach (float x in new[] { -2.8f, 0f, 2.8f })
        {
            b.AddCylinder(StylizedColor.WoodDark, new Vector3(x, 0f, -1.8f), 0.1f, height - 0.2f, 6);
            b.AddCylinder(StylizedColor.WoodDark, new Vector3(x, 0f, 1.8f), 0.1f, height, 6);
        }

        // 천 지붕 (앞이 높고 뒤가 낮음) · 빨간 줄무늬 · 앞쪽 늘어진 천
        b.Push(new Vector3(0f, height + 0.05f, 0f), Euler(-6f, 0f, 0f), Vector3.one);
        b.AddBox(StylizedColor.ClothCream, Vector3.zero, new Vector3(6.2f, 0.08f, 4.2f));

        for (int stripe = 0; stripe < 4; stripe++)
        {
            b.AddBox(StylizedColor.ClothRed, new Vector3(-2.25f + stripe * 1.5f, 0.05f, 0f), new Vector3(0.45f, 0.02f, 4.22f));
        }

        for (int flap = 0; flap < 6; flap++)
        {
            float x = -2.6f + flap * 1.04f;
            b.AddWedge(flap % 2 == 0 ? StylizedColor.ClothRed : StylizedColor.ClothCream, new Vector3(x, -0.18f, 2.1f), new Vector3(1.0f, 0.3f, 0.06f));
        }

        b.Pop();

        // 뒤 · 옆 천벽 (뒤는 막고 옆은 반만)
        b.AddBox(StylizedColor.ClothCream, new Vector3(0f, 1.4f, -1.9f), new Vector3(5.7f, 2.8f, 0.06f));
        b.AddBox(StylizedColor.ClothCream, new Vector3(-2.9f, 1.8f, -0.8f), new Vector3(0.06f, 2.0f, 2.1f));

        // 건초 더미 · 여물통 · 물통
        b.AddBox(StylizedColor.Straw, new Vector3(-2.1f, 0.35f, -1.3f), new Vector3(1.1f, 0.6f, 0.7f));
        b.AddBox(StylizedColor.Straw, new Vector3(-1.6f, 0.95f, -1.35f), new Vector3(1.0f, 0.6f, 0.65f));
        b.AddBox(StylizedColor.Straw, new Vector3(-2.4f, 0.35f, -0.45f), new Vector3(0.8f, 0.6f, 0.7f));
        b.AddBox(StylizedColor.WoodDark, new Vector3(0.6f, 0.45f, -1.45f), new Vector3(1.6f, 0.5f, 0.55f));
        b.AddBox(StylizedColor.Water, new Vector3(0.6f, 0.68f, -1.45f), new Vector3(1.45f, 0.04f, 0.42f));
        b.AddFrustum(StylizedColor.WoodLight, new Vector3(1.9f, 0f, -1.3f), 0.35f, 0.32f, 0.7f, 8);
        b.AddTorus(StylizedColor.IronDark, new Vector3(1.9f, 0.5f, -1.3f), 0.34f, 0.025f, 10, 3);

        // 짐 상자 · 자루 · 안장 가방
        b.AddBox(StylizedColor.WoodLight, new Vector3(2.2f, 0.35f, 0.4f), new Vector3(0.7f, 0.6f, 0.7f));
        b.AddBox(StylizedColor.WoodPlank, new Vector3(2.15f, 0.95f, 0.35f), new Vector3(0.55f, 0.5f, 0.55f));
        b.AddLowPolySphere(StylizedColor.ClothCream, new Vector3(1.4f, 0.3f, 0.7f), new Vector3(0.3f, 0.32f, 0.26f), 1, 0.08f, 11040);
        b.AddLowPolySphere(StylizedColor.Leather, new Vector3(-0.6f, 1.25f, -1.72f), new Vector3(0.3f, 0.25f, 0.12f), 1, 0.05f, 11041);
        b.AddLowPolySphere(StylizedColor.Leather, new Vector3(-0.1f, 1.25f, -1.72f), new Vector3(0.3f, 0.25f, 0.12f), 1, 0.05f, 11042);
        b.AddLimb(StylizedColor.Rope, new Vector3(-0.9f, 1.5f, -1.75f), new Vector3(0.2f, 1.5f, -1.75f), 0.02f, 0.02f, 3);

        // 바깥 짐수레 (+X 옆)
        b.AddBox(StylizedColor.WoodPlank, new Vector3(3.9f, 0.75f, 0.3f), new Vector3(1.0f, 0.12f, 1.8f));

        foreach (float z in new[] { -0.55f, 0.55f })
        {
            b.AddBox(StylizedColor.WoodDark, new Vector3(3.9f, 0.9f, z * 1.55f), new Vector3(1.0f, 0.3f, 0.06f));
        }

        for (int side = -1; side <= 1; side += 2)
        {
            b.Push(new Vector3(3.9f + side * 0.58f, 0.45f, 0.3f), Euler(0f, 0f, 90f), Vector3.one);
            b.AddTorus(StylizedColor.WoodDark, Vector3.zero, 0.4f, 0.05f, 10, 3);
            b.AddCylinder(StylizedColor.WoodDark, new Vector3(0f, -0.03f, 0f), 0.08f, 0.06f, 6);
            b.Pop();
        }

        b.AddLimb(StylizedColor.WoodDark, new Vector3(3.9f, 0.7f, 1.2f), new Vector3(3.9f, 0.35f, 2.3f), 0.04f, 0.04f, 4);
        b.AddBox(StylizedColor.WoodLight, new Vector3(3.85f, 1.05f, 0.1f), new Vector3(0.6f, 0.5f, 0.6f));

        // 등불 기둥 · 편자 간판
        b.AddLimb(StylizedColor.IronDark, new Vector3(2.8f, height - 0.1f, 1.8f), new Vector3(2.8f, height - 0.6f, 2.1f), 0.02f, 0.02f, 3);
        b.AddLowPolySphere(StylizedColor.LampGlow, new Vector3(2.8f, height - 0.72f, 2.1f), Vector3.one * 0.14f, 1, 0f, 11043);
        b.AddBox(StylizedColor.WoodPlank, new Vector3(0f, height - 0.55f, 1.86f), new Vector3(1.2f, 0.45f, 0.06f));
        b.Push(new Vector3(0f, height - 0.55f, 1.9f), Euler(90f, 0f, 0f), Vector3.one);
        b.AddTorus(StylizedColor.Iron, Vector3.zero, 0.14f, 0.03f, 8, 3);
        b.Pop();
    }
}
