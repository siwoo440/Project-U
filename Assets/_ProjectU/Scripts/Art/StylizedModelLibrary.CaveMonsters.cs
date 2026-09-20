using UnityEngine;

// 120일차: 동굴 몬스터 모델 (박쥐 · 동굴 거미 · 돌 골렘 · 수정 골렘 보스)과 전리품 · 수정 장비
// 몬스터는 미터 단위 · 앞 방향 +Z · 바닥 y = 0 이다. 박쥐만 공중에 떠 있는 자세로 만든다.
public static partial class StylizedModelLibrary
{
    private static void RegisterCaveMonsters()
    {
        // 몬스터
        Register("monster_cave_bat", FitMode.UniformHeight, BuildCaveBat);
        Register("monster_cave_spider", FitMode.UniformHeight, BuildCaveSpider);
        Register("monster_stone_golem", FitMode.UniformHeight, BuildStoneGolem);
        Register("monster_crystal_golem", FitMode.UniformHeight, BuildCrystalGolem);
        Register("monster_crystal_shard", FitMode.UniformHeight, BuildCrystalShardling);

        // 보스 방 · 날아오는 수정
        Register("cave_crystal_gate", FitMode.Stretch, BuildCrystalGate);
        Register("fx_crystal_bolt", FitMode.UniformLargest, BuildCrystalBolt);

        // 전리품 · 수정 장비
        Register("resource_bat_wing", FitMode.UniformLargest, BuildBatWingItem);
        Register("resource_venom_fang", FitMode.UniformLargest, BuildVenomFangItem);
        Register("resource_crystal_heart", FitMode.UniformLargest, BuildCrystalHeartItem);
        Register("weapon_crystal_sword", FitMode.UniformLargest, BuildCrystalSword);
        Register("equipment_crystal_armor", FitMode.UniformLargest, BuildCrystalArmor);
    }

    // ---------------------------------------------------------------- 박쥐 (공중에 떠 있다)

    private static void BuildCaveBat(LowPolyMeshBuilder b)
    {
        const float bodyY = 0.62f; // 떠 있는 높이

        // 몸통과 배
        b.AddLowPolySphere(StylizedColor.EnemySkinDark, new Vector3(0f, bodyY, 0f), new Vector3(0.1f, 0.13f, 0.12f), 2, 0.06f, 5001);
        b.AddLowPolySphere(StylizedColor.EnemyBelly, new Vector3(0f, bodyY - 0.03f, 0.05f), new Vector3(0.075f, 0.09f, 0.07f), 1, 0.05f, 5002);

        // 머리 · 귀 · 이빨 · 눈
        Vector3 head = new Vector3(0f, bodyY + 0.14f, 0.04f);
        b.AddLowPolySphere(StylizedColor.EnemySkinDark, head, new Vector3(0.085f, 0.08f, 0.09f), 1, 0.05f, 5003);
        b.AddLowPolySphere(StylizedColor.EnemySkin, head + new Vector3(0f, -0.02f, 0.07f), new Vector3(0.045f, 0.04f, 0.05f), 1, 0.04f, 5004);
        b.AddLowPolySphere(StylizedColor.Black, head + new Vector3(0f, -0.015f, 0.11f), Vector3.one * 0.016f, 0, 0f, 5005);

        for (int side = -1; side <= 1; side += 2)
        {
            b.Push(head + new Vector3(side * 0.045f, 0.06f, -0.01f), Euler(-18f, side * 22f, side * 8f), Vector3.one);
            b.AddCone(StylizedColor.EnemySkinDark, Vector3.zero, 0.042f, 0.14f, 5); // 큰 귀
            b.AddCone(StylizedColor.EnemySkin, new Vector3(0f, 0.01f, 0.006f), 0.026f, 0.1f, 5);
            b.Pop();
            b.AddLowPolySphere(StylizedColor.LampGlow, head + new Vector3(side * 0.04f, 0.02f, 0.075f), Vector3.one * 0.016f, 0, 0f, 5006 + side); // 빛나는 눈
            b.AddCone(StylizedColor.White, head + new Vector3(side * 0.022f, -0.055f, 0.085f), 0.008f, 0.03f, 4); // 이빨
        }

        // 날개 : 뼈대 3줄과 그 사이 막
        for (int side = -1; side <= 1; side += 2)
        {
            Vector3 shoulder = new Vector3(side * 0.09f, bodyY + 0.05f, 0f);
            Vector3 elbow = shoulder + new Vector3(side * 0.22f, 0.07f, -0.02f);
            Vector3 wrist = elbow + new Vector3(side * 0.2f, -0.02f, -0.03f);
            b.AddLimb(StylizedColor.EnemySkinDark, shoulder, elbow, 0.022f, 0.016f, 5);
            b.AddLimb(StylizedColor.EnemySkinDark, elbow, wrist, 0.016f, 0.011f, 5);

            Vector3[] fingers =
            {
                wrist + new Vector3(side * 0.12f, 0.02f, -0.14f),
                wrist + new Vector3(side * 0.1f, -0.03f, -0.28f),
                wrist + new Vector3(side * 0.04f, -0.08f, -0.34f)
            };

            Vector3 previous = wrist;

            for (int index = 0; index < fingers.Length; index++)
            {
                b.AddLimb(StylizedColor.EnemySkinDark, wrist, fingers[index], 0.012f, 0.006f, 4); // 날개 손가락 뼈
                b.AddQuad(StylizedColor.EnemySkin, shoulder, previous, fingers[index], shoulder + new Vector3(0f, -0.02f, -0.05f)); // 막 (앞면)
                b.AddQuad(StylizedColor.EnemySkin, shoulder + new Vector3(0f, -0.02f, -0.05f), fingers[index], previous, shoulder); // 막 (뒷면)
                previous = fingers[index];
            }

            b.AddQuad(StylizedColor.EnemySkin, shoulder, previous, new Vector3(0f, bodyY - 0.1f, -0.1f), shoulder);
            b.AddQuad(StylizedColor.EnemySkin, new Vector3(0f, bodyY - 0.1f, -0.1f), previous, shoulder, shoulder);
        }

        // 다리와 꼬리
        for (int side = -1; side <= 1; side += 2)
        {
            Vector3 hip = new Vector3(side * 0.05f, bodyY - 0.11f, 0f);
            Vector3 foot = hip + new Vector3(side * 0.03f, -0.1f, -0.02f);
            b.AddLimb(StylizedColor.EnemySkinDark, hip, foot, 0.014f, 0.008f, 4);

            for (int claw = -1; claw <= 1; claw++)
            {
                b.AddCone(StylizedColor.Bone, foot + new Vector3(claw * 0.012f, -0.005f, 0.01f), 0.005f, 0.025f, 4);
            }
        }

        b.AddLimb(StylizedColor.EnemySkinDark, new Vector3(0f, bodyY - 0.1f, -0.08f), new Vector3(0f, bodyY - 0.16f, -0.16f), 0.012f, 0.005f, 4);
    }

    // ---------------------------------------------------------------- 동굴 거미

    private static void BuildCaveSpider(LowPolyMeshBuilder b)
    {
        const float bodyY = 0.34f;

        // 배 · 등 무늬 · 가슴
        b.AddLowPolySphere(StylizedColor.EnemySkinDark, new Vector3(0f, bodyY, -0.2f), new Vector3(0.24f, 0.2f, 0.28f), 2, 0.07f, 5021);
        b.AddLowPolySphere(StylizedColor.GlowMushroom, new Vector3(0f, bodyY + 0.16f, -0.22f), new Vector3(0.1f, 0.03f, 0.14f), 1, 0.12f, 5022);
        b.AddLowPolySphere(StylizedColor.SpitterSac, new Vector3(0f, bodyY + 0.1f, -0.36f), new Vector3(0.07f, 0.05f, 0.06f), 1, 0.1f, 5023);
        b.AddLowPolySphere(StylizedColor.EnemySkin, new Vector3(0f, bodyY - 0.02f, 0.02f), new Vector3(0.16f, 0.13f, 0.16f), 1, 0.06f, 5024);

        // 머리 · 눈 여섯 개 · 독니
        Vector3 head = new Vector3(0f, bodyY, 0.16f);
        b.AddLowPolySphere(StylizedColor.EnemySkinDark, head, new Vector3(0.12f, 0.1f, 0.11f), 1, 0.05f, 5025);

        for (int side = -1; side <= 1; side += 2)
        {
            b.AddLowPolySphere(StylizedColor.LampGlow, head + new Vector3(side * 0.045f, 0.05f, 0.08f), Vector3.one * 0.022f, 0, 0f, 5026 + side); // 큰 눈
            b.AddLowPolySphere(StylizedColor.Black, head + new Vector3(side * 0.08f, 0.03f, 0.05f), Vector3.one * 0.014f, 0, 0f, 5028 + side);
            b.AddLowPolySphere(StylizedColor.Black, head + new Vector3(side * 0.025f, 0.075f, 0.05f), Vector3.one * 0.012f, 0, 0f, 5030 + side);
            b.Push(head + new Vector3(side * 0.04f, -0.05f, 0.09f), Euler(52f, side * 10f, 0f), Vector3.one);
            b.AddCone(StylizedColor.Bone, Vector3.zero, 0.018f, 0.1f, 5); // 독니
            b.Pop();
            b.AddLowPolySphere(StylizedColor.Slime, head + new Vector3(side * 0.05f, -0.12f, 0.12f), Vector3.one * 0.012f, 0, 0f, 5032 + side); // 독 방울
        }

        // 다리 여덟 개 (무릎이 위로 꺾인 거미 다리)
        for (int side = -1; side <= 1; side += 2)
        {
            for (int leg = 0; leg < 4; leg++)
            {
                float spread = 0.55f + leg * 0.28f;
                Vector3 hip = new Vector3(side * 0.13f, bodyY, 0.08f - leg * 0.11f);
                Vector3 knee = hip + new Vector3(side * 0.22f, 0.3f - leg * 0.02f, Mathf.Cos(spread) * 0.16f);
                Vector3 foot = knee + new Vector3(side * 0.26f, -0.62f, Mathf.Cos(spread) * 0.26f);
                b.AddLimb(StylizedColor.EnemySkinDark, hip, knee, 0.038f, 0.028f, 5);
                b.AddLimb(StylizedColor.EnemySkinDark, knee, foot, 0.028f, 0.012f, 5);
                b.AddLowPolySphere(StylizedColor.EnemySkin, knee, Vector3.one * 0.038f, 1, 0.06f, 5041 + side * 4 + leg);
                b.AddCone(StylizedColor.Bone, foot, 0.012f, 0.05f, 4); // 발톱
            }
        }
    }

    // ---------------------------------------------------------------- 돌 골렘

    private static void BuildStoneGolem(LowPolyMeshBuilder b)
    {
        BuildGolemBody(b, StylizedColor.Stone, StylizedColor.StoneDark, StylizedColor.Ore, 1f, 5061);

        // 이끼와 깨진 자리
        b.AddLowPolySphere(StylizedColor.Leaf, new Vector3(0.22f, 1.42f, -0.2f), new Vector3(0.16f, 0.04f, 0.14f), 1, 0.2f, 5081);
        b.AddLowPolySphere(StylizedColor.LeafDark, new Vector3(-0.26f, 1.12f, 0.18f), new Vector3(0.13f, 0.035f, 0.12f), 1, 0.2f, 5082);
        b.AddLowPolySphere(StylizedColor.Grass, new Vector3(0.05f, 1.72f, -0.24f), new Vector3(0.12f, 0.03f, 0.1f), 1, 0.22f, 5083);
    }

    // ---------------------------------------------------------------- 수정 골렘 (보스)

    private static void BuildCrystalGolem(LowPolyMeshBuilder b)
    {
        BuildGolemBody(b, StylizedColor.StoneDark, StylizedColor.MountainDark, StylizedColor.Crystal, 1.45f, 5101);

        // 어깨 · 등 · 팔에 박힌 수정
        (Vector3 spot, float radius, float height, float pitch, float yaw)[] shards =
        {
            (new Vector3(0.52f, 2.2f, -0.1f), 0.12f, 0.6f, -24f, 20f),
            (new Vector3(-0.52f, 2.24f, -0.12f), 0.11f, 0.52f, -22f, -26f),
            (new Vector3(0.16f, 2.36f, -0.42f), 0.1f, 0.66f, -34f, 8f),
            (new Vector3(-0.2f, 2.28f, -0.46f), 0.09f, 0.5f, -38f, -14f),
            (new Vector3(0.66f, 1.5f, 0.12f), 0.08f, 0.34f, 60f, 30f),
            (new Vector3(-0.66f, 1.46f, 0.1f), 0.08f, 0.32f, 62f, -30f)
        };

        foreach ((Vector3 spot, float radius, float height, float pitch, float yaw) in shards)
        {
            b.Push(spot, Euler(pitch, yaw, 0f), Vector3.one);
            b.AddFrustum(StylizedColor.Crystal, Vector3.zero, radius, radius * 0.45f, height * 0.7f, 6, true, false);
            b.AddCone(StylizedColor.Glass, new Vector3(0f, height * 0.7f, 0f), radius * 0.45f, height * 0.4f, 6);
            b.Pop();
        }

        // 가슴 속 수정 심장
        b.AddLowPolySphere(StylizedColor.MountainDark, new Vector3(0f, 1.72f, 0.3f), new Vector3(0.22f, 0.22f, 0.1f), 1, 0.1f, 5121);
        b.AddLowPolySphere(StylizedColor.Crystal, new Vector3(0f, 1.72f, 0.34f), new Vector3(0.15f, 0.16f, 0.1f), 1, 0.06f, 5122);
        b.AddLowPolySphere(StylizedColor.RuneGlow, new Vector3(0f, 1.72f, 0.37f), new Vector3(0.09f, 0.1f, 0.06f), 1, 0.05f, 5123);

        // 머리 위 수정 왕관
        for (int index = 0; index < 5; index++)
        {
            float angle = (index - 2f) * 24f;
            b.Push(new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad) * 0.2f, 2.52f, Mathf.Cos(angle * Mathf.Deg2Rad) * 0.1f - 0.04f), Euler(-16f, angle, 0f), Vector3.one);
            b.AddCone(StylizedColor.Crystal, Vector3.zero, 0.05f, 0.24f + (index % 2) * 0.1f, 5);
            b.Pop();
        }

        // 몸에 흐르는 빛 금
        for (int index = 0; index < 4; index++)
        {
            b.AddBox(StylizedColor.RuneGlow, new Vector3(-0.18f + index * 0.12f, 1.3f + (index % 2) * 0.3f, 0.29f), new Vector3(0.03f, 0.22f, 0.02f));
        }
    }

    // 골렘 공통 몸 (돌 골렘 · 수정 골렘이 크기와 색만 다르게 쓴다)
    private static void BuildGolemBody(LowPolyMeshBuilder b, StylizedColor rock, StylizedColor rockDark, StylizedColor glow, float scale, int seed)
    {
        float s = scale;

        // 다리 · 발
        for (int side = -1; side <= 1; side += 2)
        {
            b.AddBeveledBox(rockDark, new Vector3(side * 0.24f * s, 0.34f * s, 0f), new Vector3(0.28f, 0.68f, 0.3f) * s, 0.06f * s);
            b.AddBeveledBox(rock, new Vector3(side * 0.26f * s, 0.08f * s, 0.05f * s), new Vector3(0.34f, 0.16f, 0.42f) * s, 0.04f * s);
            b.AddLowPolySphere(rock, new Vector3(side * 0.24f * s, 0.66f * s, 0f), new Vector3(0.19f, 0.16f, 0.19f) * s, 1, 0.12f, seed + 1 + side);
        }

        // 몸통 (아래가 좁고 위가 넓다)
        b.AddBeveledBox(rock, new Vector3(0f, 0.92f * s, 0f), new Vector3(0.5f, 0.44f, 0.42f) * s, 0.07f * s);
        b.AddBeveledBox(rockDark, new Vector3(0f, 1.26f * s, 0f), new Vector3(0.72f, 0.48f, 0.52f) * s, 0.09f * s);
        b.AddLowPolySphere(rock, new Vector3(0f, 1.48f * s, -0.06f * s), new Vector3(0.42f, 0.24f, 0.32f) * s, 1, 0.12f, seed + 4);

        // 깨진 금 (몸통 앞)
        b.AddBox(rockDark, new Vector3(0.06f * s, 1.1f * s, 0.21f * s), new Vector3(0.05f, 0.34f, 0.02f) * s);
        b.AddBox(rockDark, new Vector3(-0.1f * s, 0.95f * s, 0.2f * s), new Vector3(0.04f, 0.22f, 0.02f) * s);

        // 어깨 · 팔 · 주먹
        for (int side = -1; side <= 1; side += 2)
        {
            Vector3 shoulder = new Vector3(side * 0.42f * s, 1.42f * s, 0f);
            Vector3 elbow = shoulder + new Vector3(side * 0.16f * s, -0.42f * s, 0.04f * s);
            Vector3 fist = elbow + new Vector3(side * 0.08f * s, -0.44f * s, 0.12f * s);
            b.AddLowPolySphere(rock, shoulder, new Vector3(0.26f, 0.24f, 0.26f) * s, 1, 0.14f, seed + 6 + side);
            b.AddLimb(rockDark, shoulder, elbow, 0.17f * s, 0.14f * s, 6);
            b.AddLimb(rock, elbow, fist, 0.15f * s, 0.13f * s, 6);
            b.AddBeveledBox(rockDark, fist + new Vector3(0f, -0.06f * s, 0f), new Vector3(0.34f, 0.3f, 0.34f) * s, 0.06f * s);
            b.AddLowPolySphere(rock, fist + new Vector3(side * 0.06f * s, -0.04f * s, 0.14f * s), new Vector3(0.1f, 0.09f, 0.08f) * s, 1, 0.14f, seed + 8 + side);
        }

        // 머리 (어깨 사이에 파묻힘) · 빛나는 눈
        b.AddBeveledBox(rockDark, new Vector3(0f, 1.78f * s, 0.02f * s), new Vector3(0.38f, 0.36f, 0.36f) * s, 0.07f * s);
        b.AddBox(rock, new Vector3(0f, 1.66f * s, 0.2f * s), new Vector3(0.3f, 0.1f, 0.06f) * s);
        b.AddLowPolySphere(glow, new Vector3(0.1f * s, 1.82f * s, 0.19f * s), new Vector3(0.05f, 0.04f, 0.03f) * s, 0, 0f, seed + 10);
        b.AddLowPolySphere(glow, new Vector3(-0.1f * s, 1.82f * s, 0.19f * s), new Vector3(0.05f, 0.04f, 0.03f) * s, 0, 0f, seed + 11);

        // 등에 솟은 돌
        for (int index = 0; index < 3; index++)
        {
            b.Push(new Vector3((index - 1) * 0.2f * s, 1.6f * s, -0.26f * s), Euler(-38f, (index - 1) * 16f, 0f), Vector3.one);
            b.AddCone(rock, Vector3.zero, 0.11f * s, 0.34f * s, 5);
            b.Pop();
        }
    }

    // ---------------------------------------------------------------- 수정 조각 (보스가 부르는 작은 적)

    private static void BuildCrystalShardling(LowPolyMeshBuilder b)
    {
        b.AddLowPolySphere(StylizedColor.RuneGlow, new Vector3(0f, 0.45f, 0f), Vector3.one * 0.1f, 1, 0.06f, 5141); // 빛나는 속

        for (int index = 0; index < 5; index++)
        {
            float angle = index / 5f * Mathf.PI * 2f;
            b.Push(new Vector3(Mathf.Sin(angle) * 0.1f, 0.45f, Mathf.Cos(angle) * 0.1f), Euler(-28f, angle * Mathf.Rad2Deg, 0f), Vector3.one);
            b.AddFrustum(StylizedColor.Crystal, Vector3.zero, 0.07f, 0.03f, 0.22f, 5, true, false);
            b.AddCone(StylizedColor.Glass, new Vector3(0f, 0.22f, 0f), 0.03f, 0.1f, 5);
            b.Pop();
        }

        b.AddLowPolySphere(StylizedColor.Crystal, new Vector3(0f, 0.2f, 0f), new Vector3(0.09f, 0.07f, 0.09f), 1, 0.1f, 5142);
        b.AddLowPolySphere(StylizedColor.Glass, new Vector3(0f, 0.68f, 0f), new Vector3(0.06f, 0.09f, 0.06f), 1, 0.08f, 5143);
    }

    // ---------------------------------------------------------------- 보스 방 수정 문 · 날아오는 수정

    private static void BuildCrystalGate(LowPolyMeshBuilder b) // 가로 1 · 높이 1 상자 안 (생성 도구가 굴 크기로 늘린다)
    {
        b.AddBox(StylizedColor.Crystal, new Vector3(0f, 0.5f, 0f), new Vector3(1f, 1f, 0.12f));

        for (int index = 0; index < 7; index++) // 삐죽 솟은 수정
        {
            float x = -0.42f + index * 0.14f;
            float height = 0.3f + (index % 3) * 0.22f;
            b.Push(new Vector3(x, index % 2 == 0 ? 0f : 1f, 0f), Euler(index % 2 == 0 ? 0f : 180f, 0f, (index - 3) * 6f), Vector3.one);
            b.AddCone(StylizedColor.Glass, Vector3.zero, 0.09f, height, 6);
            b.Pop();
        }

        b.AddBox(StylizedColor.RuneGlow, new Vector3(0f, 0.5f, 0.07f), new Vector3(0.9f, 0.06f, 0.02f));
        b.AddBox(StylizedColor.RuneGlow, new Vector3(0f, 0.2f, 0.07f), new Vector3(0.7f, 0.04f, 0.02f));
        b.AddBox(StylizedColor.RuneGlow, new Vector3(0f, 0.8f, 0.07f), new Vector3(0.7f, 0.04f, 0.02f));
    }

    private static void BuildCrystalBolt(LowPolyMeshBuilder b) // 보스가 던지는 수정 조각
    {
        b.AddFrustum(StylizedColor.Crystal, new Vector3(0f, 0f, -0.12f), 0.06f, 0.03f, 0.18f, 6, true, false);
        b.Push(new Vector3(0f, 0f, 0.06f), Euler(90f, 0f, 0f), Vector3.one);
        b.AddCone(StylizedColor.Glass, Vector3.zero, 0.05f, 0.16f, 6);
        b.Pop();
        b.AddLowPolySphere(StylizedColor.RuneGlow, new Vector3(0f, 0f, -0.02f), Vector3.one * 0.045f, 1, 0.05f, 5161);
    }

    // ---------------------------------------------------------------- 전리품 · 수정 장비

    private static void BuildBatWingItem(LowPolyMeshBuilder b) // 박쥐 날개
    {
        Vector3 root = new Vector3(-0.1f, 0.03f, -0.06f);
        Vector3 wrist = new Vector3(0.04f, 0.06f, 0.02f);
        Vector3[] fingers = { new Vector3(0.16f, 0.05f, 0.1f), new Vector3(0.19f, 0.04f, -0.02f), new Vector3(0.14f, 0.03f, -0.12f) };
        b.AddLimb(StylizedColor.BarkDark, root, wrist, 0.012f, 0.009f, 5);
        Vector3 previous = wrist;

        foreach (Vector3 finger in fingers)
        {
            b.AddLimb(StylizedColor.BarkDark, wrist, finger, 0.008f, 0.004f, 4);
            b.AddQuad(StylizedColor.EnemySkin, root, previous, finger, root);
            b.AddQuad(StylizedColor.EnemySkin, root, finger, previous, root);
            previous = finger;
        }

        b.AddLowPolySphere(StylizedColor.EnemySkinDark, root, Vector3.one * 0.022f, 1, 0.08f, 5171);
    }

    private static void BuildVenomFangItem(LowPolyMeshBuilder b) // 독니
    {
        for (int index = 0; index < 2; index++)
        {
            b.Push(new Vector3(-0.03f + index * 0.06f, 0.02f, 0f), Euler(-14f + index * 8f, index * 30f, 0f), Vector3.one);
            b.AddCone(StylizedColor.Bone, Vector3.zero, 0.022f, 0.16f, 6);
            b.AddFrustum(StylizedColor.EnemySkinDark, Vector3.zero, 0.024f, 0.02f, 0.03f, 6, true, false);
            b.Pop();
        }

        b.AddLowPolySphere(StylizedColor.Slime, new Vector3(0.03f, 0.17f, 0.01f), Vector3.one * 0.018f, 1, 0.06f, 5181);
        b.AddLowPolySphere(StylizedColor.Slime, new Vector3(-0.03f, 0.14f, -0.01f), Vector3.one * 0.013f, 1, 0.06f, 5182);
        b.AddLimb(StylizedColor.Rope, new Vector3(-0.05f, 0.03f, 0f), new Vector3(0.05f, 0.03f, 0f), 0.005f, 0.005f, 4);
    }

    private static void BuildCrystalHeartItem(LowPolyMeshBuilder b) // 수정 심장
    {
        b.AddLowPolySphere(StylizedColor.Crystal, new Vector3(0f, 0.11f, 0f), new Vector3(0.085f, 0.1f, 0.075f), 1, 0.08f, 5191);
        b.AddLowPolySphere(StylizedColor.RuneGlow, new Vector3(0f, 0.11f, 0f), new Vector3(0.05f, 0.06f, 0.045f), 1, 0.06f, 5192);

        for (int index = 0; index < 4; index++) // 삐죽 나온 조각
        {
            float angle = index / 4f * Mathf.PI * 2f + 0.4f;
            b.Push(new Vector3(Mathf.Sin(angle) * 0.06f, 0.12f, Mathf.Cos(angle) * 0.055f), Euler(-34f, angle * Mathf.Rad2Deg, 0f), Vector3.one);
            b.AddCone(StylizedColor.Glass, Vector3.zero, 0.028f, 0.1f, 5);
            b.Pop();
        }

        b.AddFrustum(StylizedColor.IronDark, Vector3.zero, 0.07f, 0.055f, 0.035f, 8, true, false); // 받침
        b.AddLimb(StylizedColor.Iron, new Vector3(-0.06f, 0.05f, 0f), new Vector3(0.06f, 0.05f, 0f), 0.008f, 0.008f, 4);
    }

    private static void BuildCrystalSword(LowPolyMeshBuilder b) // 수정 검
    {
        b.AddCylinder(StylizedColor.Leather, new Vector3(0f, -0.28f, 0f), 0.028f, 0.2f, 8); // 손잡이
        b.AddLowPolySphere(StylizedColor.IronDark, new Vector3(0f, -0.3f, 0f), Vector3.one * 0.038f, 1, 0.05f, 5201);

        for (int index = 0; index < 4; index++) // 가죽 감은 자리
        {
            b.AddCylinder(StylizedColor.BarkDark, new Vector3(0f, -0.26f + index * 0.045f, 0f), 0.03f, 0.012f, 8);
        }

        b.AddBeveledBox(StylizedColor.IronDark, new Vector3(0f, -0.06f, 0f), new Vector3(0.22f, 0.04f, 0.06f), 0.015f); // 날밑
        b.AddLowPolySphere(StylizedColor.Crystal, new Vector3(0f, -0.06f, 0f), new Vector3(0.05f, 0.04f, 0.04f), 1, 0.05f, 5202);
        b.AddBeveledBox(StylizedColor.Crystal, new Vector3(0f, 0.22f, 0f), new Vector3(0.09f, 0.5f, 0.035f), 0.02f); // 수정 날
        b.AddBeveledBox(StylizedColor.Glass, new Vector3(0f, 0.24f, 0f), new Vector3(0.05f, 0.44f, 0.045f), 0.015f);
        b.Push(new Vector3(0f, 0.47f, 0f), Euler(0f, 0f, 0f), Vector3.one);
        b.AddCone(StylizedColor.Crystal, Vector3.zero, 0.045f, 0.16f, 4); // 칼끝
        b.Pop();
        b.AddBox(StylizedColor.RuneGlow, new Vector3(0f, 0.22f, 0.02f), new Vector3(0.02f, 0.42f, 0.01f)); // 빛나는 홈
    }

    private static void BuildCrystalArmor(LowPolyMeshBuilder b) // 수정 갑옷
    {
        b.AddBeveledBox(StylizedColor.IronDark, new Vector3(0f, 0.34f, 0f), new Vector3(0.44f, 0.56f, 0.26f), 0.06f); // 몸판
        b.AddBeveledBox(StylizedColor.Iron, new Vector3(0f, 0.56f, 0.02f), new Vector3(0.46f, 0.14f, 0.28f), 0.04f); // 목깃
        b.AddLowPolySphere(StylizedColor.Crystal, new Vector3(0f, 0.38f, 0.15f), new Vector3(0.12f, 0.13f, 0.06f), 1, 0.07f, 5211); // 가슴 수정
        b.AddLowPolySphere(StylizedColor.RuneGlow, new Vector3(0f, 0.38f, 0.18f), new Vector3(0.07f, 0.08f, 0.03f), 1, 0.05f, 5212);

        for (int side = -1; side <= 1; side += 2) // 어깨 수정
        {
            b.AddBeveledBox(StylizedColor.Iron, new Vector3(side * 0.26f, 0.5f, 0f), new Vector3(0.16f, 0.16f, 0.24f), 0.04f);
            b.Push(new Vector3(side * 0.28f, 0.58f, -0.02f), Euler(-24f, side * 18f, side * 12f), Vector3.one);
            b.AddCone(StylizedColor.Crystal, Vector3.zero, 0.06f, 0.24f, 5);
            b.Pop();
            b.AddBox(StylizedColor.RuneGlow, new Vector3(side * 0.22f, 0.34f, 0.13f), new Vector3(0.02f, 0.3f, 0.01f));
        }

        b.AddBeveledBox(StylizedColor.Leather, new Vector3(0f, 0.08f, 0f), new Vector3(0.46f, 0.1f, 0.28f), 0.03f); // 허리 가죽
        b.AddBox(StylizedColor.Gold, new Vector3(0f, 0.08f, 0.15f), new Vector3(0.07f, 0.06f, 0.02f));
    }
}
