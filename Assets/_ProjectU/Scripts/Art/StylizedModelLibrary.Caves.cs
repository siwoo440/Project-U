using UnityEngine;

// 116일차: 동굴 (지상 입구 3종 · 동굴 안 소품)
// 미터 단위, 바닥 y = 0, 들어가는 쪽은 +Z.
public static partial class StylizedModelLibrary
{
    public const float CaveEntranceWidth = 4.4f; // 굴 입구 너비 (생성 도구 충돌체)
    public const float CaveEntranceHeight = 3.6f; // 굴 입구 높이

    private static void RegisterCaves()
    {
        Register("cave_entrance_arch", FitMode.UniformHeight, BuildCaveEntranceArch); // 바위 아치 굴 입구
        Register("cave_crack", FitMode.UniformHeight, BuildCaveCrack); // 바위 틈
        Register("cave_sinkhole", FitMode.UniformHeight, BuildCaveSinkhole); // 무너진 구덩이 (밧줄)
        Register("cave_exit_ladder", FitMode.UniformHeight, BuildCaveExitLadder); // 동굴 안 출구 (사다리 · 밧줄)
        Register("cave_pillar", FitMode.UniformHeight, BuildCavePillar); // 돌기둥
        Register("cave_stalagmite", FitMode.UniformHeight, BuildCaveStalagmite); // 석순
        Register("cave_stalactite", FitMode.UniformHeight, BuildCaveStalactite); // 종유석 (천장에 매달림)
        Register("cave_crystal", FitMode.UniformHeight, BuildCaveCrystal); // 빛나는 수정 무리
        Register("cave_ore_vein", FitMode.UniformHeight, BuildCaveOreVein); // 광석 덩어리
        Register("cave_rubble", FitMode.UniformHeight, BuildCaveRubble); // 무너진 돌무더기
        Register("cave_pool", FitMode.Stretch, b => BuildFlatQuad(b, StylizedColor.PondDeep)); // 지하 물웅덩이
        Register("cave_cliff", FitMode.UniformHeight, b => BuildCaveCliff(b, 1f)); // 굴 입구 뒤 절벽
        Register("cave_cliff_small", FitMode.UniformHeight, b => BuildCaveCliff(b, 0.62f)); // 바위 틈 뒤 바위 언덕
        Register("item_torch", FitMode.UniformHeight, BuildTorch); // 횃불 아이템
    }

    // 지상 : 바위 아치 굴 입구 (가운데가 뚫린 어두운 구멍)
    private static void BuildCaveEntranceArch(LowPolyMeshBuilder builder)
    {
        float half = CaveEntranceWidth * 0.5f;

        for (int side = -1; side <= 1; side += 2)
        {
            builder.Push(new Vector3(side * (half + 0.9f), 0f, 0f), Quaternion.Euler(0f, side * 12f, side * -6f), Vector3.one);
            builder.AddBeveledBox(StylizedColor.StoneDark, new Vector3(0f, CaveEntranceHeight * 0.5f, 0f), new Vector3(1.8f, CaveEntranceHeight, 2.6f), 0.35f);
            builder.AddBeveledBox(StylizedColor.Stone, new Vector3(0f, CaveEntranceHeight * 0.78f, 0.5f), new Vector3(1.3f, 1.2f, 1.6f), 0.3f);
            builder.Pop();
        }

        // 위를 덮는 바위
        builder.AddBeveledBox(StylizedColor.StoneDark, new Vector3(0f, CaveEntranceHeight + 0.55f, 0.1f), new Vector3(CaveEntranceWidth + 2.6f, 1.5f, 2.9f), 0.5f);
        builder.AddBeveledBox(StylizedColor.Stone, new Vector3(-0.4f, CaveEntranceHeight + 1.35f, -0.2f), new Vector3(3.2f, 1.1f, 2.2f), 0.45f);

        // 어두운 안쪽 (검은 면 + 바닥 흙)
        builder.AddQuad(StylizedColor.Black,
            new Vector3(-half, 0.02f, 1.2f), new Vector3(half, 0.02f, 1.2f),
            new Vector3(half, CaveEntranceHeight, 1.2f), new Vector3(-half, CaveEntranceHeight, 1.2f));
        builder.AddQuad(StylizedColor.Dirt,
            new Vector3(-half, 0.03f, -1.6f), new Vector3(half, 0.03f, -1.6f),
            new Vector3(half, 0.03f, 1.2f), new Vector3(-half, 0.03f, 1.2f));

        for (int index = 0; index < 3; index++)
        {
            float x = -1.4f + index * 1.4f;
            builder.AddLowPolySphere(StylizedColor.Stone, new Vector3(x, 0.18f, -1.3f - (index % 2) * 0.5f), Vector3.one * (0.38f + index * 0.06f), 1, 0.12f, 20 + index);
        }
    }

    // 지상 : 바위 틈 (두 바위 사이 좁은 검은 틈)
    private static void BuildCaveCrack(LowPolyMeshBuilder builder)
    {
        for (int side = -1; side <= 1; side += 2)
        {
            builder.Push(new Vector3(side * 1.6f, 0f, 0f), Quaternion.Euler(-4f, side * 8f, side * 9f), Vector3.one);
            builder.AddBeveledBox(StylizedColor.StoneDark, new Vector3(0f, 1.5f, 0f), new Vector3(2.4f, 3f, 3.4f), 0.5f);
            builder.AddBeveledBox(StylizedColor.Stone, new Vector3(side * 0.3f, 2.6f, 0.3f), new Vector3(1.7f, 1.4f, 2.2f), 0.4f);
            builder.Pop();
        }

        builder.AddQuad(StylizedColor.Black,
            new Vector3(-0.85f, 0.02f, 0.6f), new Vector3(0.85f, 0.02f, 0.6f),
            new Vector3(0.85f, 2.5f, 0.6f), new Vector3(-0.85f, 2.5f, 0.6f));
        builder.AddQuad(StylizedColor.Dirt,
            new Vector3(-0.85f, 0.03f, -1.2f), new Vector3(0.85f, 0.03f, -1.2f),
            new Vector3(0.85f, 0.03f, 0.6f), new Vector3(-0.85f, 0.03f, 0.6f));
        builder.AddLowPolySphere(StylizedColor.Stone, new Vector3(-1.1f, 0.2f, -1.1f), Vector3.one * 0.4f, 1, 0.12f, 31);
        builder.AddLowPolySphere(StylizedColor.StoneDark, new Vector3(1.2f, 0.16f, -1.4f), Vector3.one * 0.34f, 1, 0.12f, 32);
    }

    // 지상 : 무너진 구덩이 (둘레 돌 + 내려가는 밧줄)
    private static void BuildCaveSinkhole(LowPolyMeshBuilder builder)
    {
        for (int index = 0; index < 9; index++)
        {
            float angle = index / 9f * Mathf.PI * 2f;
            Vector3 center = new Vector3(Mathf.Sin(angle) * 2.3f, 0.25f, Mathf.Cos(angle) * 2.3f);
            builder.Push(center, Quaternion.Euler(0f, angle * Mathf.Rad2Deg, index % 2 == 0 ? 7f : -5f), Vector3.one);
            builder.AddBeveledBox(index % 3 == 0 ? StylizedColor.Stone : StylizedColor.StoneDark, Vector3.zero, new Vector3(1.5f, 0.8f, 1.1f), 0.24f);
            builder.Pop();
        }

        builder.AddDisc(StylizedColor.Black, new Vector3(0f, 0.04f, 0f), 1.9f, 10);
        builder.AddLimb(StylizedColor.Rope, new Vector3(0.7f, 0.9f, 1.6f), new Vector3(0.5f, 0.05f, 0.4f), 0.07f, 0.06f, 5); // 내려가는 밧줄
        builder.AddLimb(StylizedColor.Bark, new Vector3(0.2f, 0f, 2.1f), new Vector3(1.3f, 1.05f, 1.5f), 0.12f, 0.1f, 5); // 밧줄을 묶은 말뚝
    }

    // 동굴 안 : 밖으로 나가는 사다리와 빛줄기
    private static void BuildCaveExitLadder(LowPolyMeshBuilder builder)
    {
        for (int side = -1; side <= 1; side += 2)
        {
            builder.AddLimb(StylizedColor.Bark, new Vector3(side * 0.42f, 0f, 0f), new Vector3(side * 0.42f, 4.2f, -0.35f), 0.09f, 0.09f, 5);
        }

        for (int step = 0; step < 7; step++)
        {
            float height = 0.45f + step * 0.55f;
            builder.AddLimb(StylizedColor.WoodLight, new Vector3(-0.42f, height, -height * 0.083f), new Vector3(0.42f, height, -height * 0.083f), 0.06f, 0.06f, 4);
        }

        builder.AddBeveledBox(StylizedColor.StoneDark, new Vector3(0f, 0.2f, 0.75f), new Vector3(2.6f, 0.4f, 1.1f), 0.18f);
        builder.AddQuad(StylizedColor.White, // 위에서 들어오는 빛
            new Vector3(-0.8f, 4.3f, -0.9f), new Vector3(0.8f, 4.3f, -0.9f),
            new Vector3(0.8f, 4.3f, 0.7f), new Vector3(-0.8f, 4.3f, 0.7f));
    }

    private static void BuildCavePillar(LowPolyMeshBuilder builder) // 바닥과 천장을 잇는 돌기둥
    {
        builder.AddFrustum(StylizedColor.StoneDark, new Vector3(0f, 0f, 0f), 1.15f, 0.55f, 2.6f, 7);
        builder.AddFrustum(StylizedColor.Stone, new Vector3(0f, 2.6f, 0f), 0.55f, 0.75f, 2.3f, 7);
        builder.AddFrustum(StylizedColor.StoneDark, new Vector3(0f, 4.9f, 0f), 0.75f, 1.25f, 1.6f, 7);
    }

    private static void BuildCaveStalagmite(LowPolyMeshBuilder builder) // 바닥에서 솟은 돌
    {
        builder.AddNoisyCone(StylizedColor.StoneDark, StylizedColor.Stone, StylizedColor.StoneLight, 0.75f, new Vector3(0f, 0f, 0f), 0.55f, 2.1f, 7, 4, 0.14f, 3);
        builder.AddNoisyCone(StylizedColor.Stone, StylizedColor.StoneDark, StylizedColor.StoneLight, 0.8f, new Vector3(0.52f, 0f, 0.28f), 0.33f, 1.2f, 6, 3, 0.12f, 5);
        builder.AddNoisyCone(StylizedColor.StoneDark, StylizedColor.Stone, StylizedColor.StoneLight, 0.8f, new Vector3(-0.45f, 0f, -0.35f), 0.26f, 0.8f, 6, 3, 0.1f, 7);
    }

    private static void BuildCaveStalactite(LowPolyMeshBuilder builder) // 천장에 매달린 돌 (아래로 자람)
    {
        builder.Push(Vector3.zero, Quaternion.Euler(180f, 0f, 0f), Vector3.one);
        builder.AddNoisyCone(StylizedColor.StoneDark, StylizedColor.Stone, StylizedColor.StoneLight, 0.8f, Vector3.zero, 0.42f, 1.8f, 6, 4, 0.13f, 11);
        builder.AddNoisyCone(StylizedColor.Stone, StylizedColor.StoneDark, StylizedColor.StoneLight, 0.8f, new Vector3(0.45f, 0f, 0.2f), 0.26f, 1.1f, 6, 3, 0.1f, 13);
        builder.Pop();
    }

    private static void BuildCaveCrystal(LowPolyMeshBuilder builder) // 빛나는 수정 무리 (불빛 기준)
    {
        builder.AddBeveledBox(StylizedColor.StoneDark, new Vector3(0f, 0.18f, 0f), new Vector3(1.5f, 0.36f, 1.3f), 0.16f);

        for (int index = 0; index < 5; index++)
        {
            float angle = index / 5f * Mathf.PI * 2f + 0.4f;
            float height = 0.9f + (index % 3) * 0.45f;
            builder.Push(new Vector3(Mathf.Sin(angle) * 0.34f, 0.3f, Mathf.Cos(angle) * 0.3f), Quaternion.Euler(index % 2 == 0 ? 14f : -11f, angle * Mathf.Rad2Deg, index % 3 == 0 ? 9f : -7f), Vector3.one);
            builder.AddCone(StylizedColor.Crystal, Vector3.zero, 0.2f, height, 5);
            builder.Pop();
        }
    }

    private static void BuildCaveOreVein(LowPolyMeshBuilder builder) // 바위에 박힌 광석
    {
        builder.AddLowPolySphere(StylizedColor.StoneDark, new Vector3(0f, 0.6f, 0f), new Vector3(0.95f, 0.8f, 0.9f), 2, 0.14f, 41);
        builder.AddLowPolySphere(StylizedColor.Stone, new Vector3(0.55f, 0.42f, 0.3f), Vector3.one * 0.52f, 1, 0.14f, 42);

        for (int index = 0; index < 4; index++)
        {
            float angle = index / 4f * Mathf.PI * 2f + 0.6f;
            builder.AddLowPolySphere(StylizedColor.Ore, new Vector3(Mathf.Sin(angle) * 0.62f, 0.62f + (index % 2) * 0.24f, Mathf.Cos(angle) * 0.58f), Vector3.one * 0.26f, 1, 0.1f, 50 + index);
        }
    }

    private static void BuildCaveRubble(LowPolyMeshBuilder builder) // 무너진 돌무더기
    {
        for (int index = 0; index < 6; index++)
        {
            float angle = index / 6f * Mathf.PI * 2f;
            builder.Push(new Vector3(Mathf.Sin(angle) * 0.7f, 0.22f + (index % 3) * 0.16f, Mathf.Cos(angle) * 0.62f), Quaternion.Euler(index * 17f, angle * Mathf.Rad2Deg, index * 9f), Vector3.one);
            builder.AddBeveledBox(index % 2 == 0 ? StylizedColor.Stone : StylizedColor.StoneDark, Vector3.zero, new Vector3(0.8f, 0.5f, 0.7f), 0.15f);
            builder.Pop();
        }
    }

    // 입구 뒤 절벽 : 풀 덮인 언덕 몸통 + 앞면에 드러난 암벽 (가운데는 입구 자리로 비워 둔다)
    private static void BuildCaveCliff(LowPolyMeshBuilder builder, float scale)
    {
        builder.Push(Vector3.zero, Quaternion.identity, Vector3.one * scale);

        // 드러난 암벽 (입구 양옆 기둥처럼)
        for (int side = -1; side <= 1; side += 2)
        {
            builder.Push(new Vector3(side * 5.4f, 0f, 0.4f), Quaternion.Euler(0f, side * 9f, side * -4f), Vector3.one);
            builder.AddBeveledBox(StylizedColor.Stone, new Vector3(0f, 3.2f, 0f), new Vector3(5.4f, 6.6f, 5.2f), 0.7f);
            builder.AddBeveledBox(StylizedColor.StoneLight, new Vector3(side * -0.6f, 5.6f, 0.7f), new Vector3(3.6f, 2.6f, 4f), 0.6f);
            builder.AddBeveledBox(StylizedColor.Stone, new Vector3(side * 1.2f, 1.4f, 1.4f), new Vector3(3f, 2.8f, 2.6f), 0.5f);
            builder.Pop();
        }

        // 입구 위 모서리 바위 (위가 좁은 굴 모양)
        for (int side = -1; side <= 1; side += 2)
        {
            builder.Push(new Vector3(side * 2.25f, 3.75f, -1.6f), Quaternion.Euler(0f, 0f, side * -26f), Vector3.one);
            builder.AddBeveledBox(StylizedColor.StoneDark, Vector3.zero, new Vector3(2.2f, 1.3f, 5.2f), 0.35f);
            builder.Pop();
        }

        // 입구 위를 덮는 바위 처마
        builder.AddBeveledBox(StylizedColor.Stone, new Vector3(0f, 5.4f, -0.4f), new Vector3(9.5f, 2.6f, 5f), 0.8f);
        builder.AddBeveledBox(StylizedColor.StoneLight, new Vector3(-1.2f, 7.1f, -1.6f), new Vector3(6.5f, 1.8f, 4.2f), 0.7f);

        // 안쪽으로 파인 굴 (깊이감) : 바닥 · 양옆 벽 · 천장 · 막다른 어둠
        const float mouthHalf = 2.7f;
        const float mouthTop = 4.2f;
        const float mouthBack = -4.2f;
        const float mouthFront = 0.4f;
        builder.AddQuad(StylizedColor.Dirt, // 바닥
            new Vector3(-mouthHalf, 0.06f, mouthBack), new Vector3(-mouthHalf, 0.06f, mouthFront),
            new Vector3(mouthHalf, 0.06f, mouthFront), new Vector3(mouthHalf, 0.06f, mouthBack));
        builder.AddQuad(StylizedColor.StoneDark, // 왼쪽 벽
            new Vector3(-mouthHalf, 0f, mouthFront), new Vector3(-mouthHalf, 0f, mouthBack),
            new Vector3(-mouthHalf, mouthTop, mouthBack), new Vector3(-mouthHalf, mouthTop, mouthFront));
        builder.AddQuad(StylizedColor.Stone, // 오른쪽 벽
            new Vector3(mouthHalf, 0f, mouthBack), new Vector3(mouthHalf, 0f, mouthFront),
            new Vector3(mouthHalf, mouthTop, mouthFront), new Vector3(mouthHalf, mouthTop, mouthBack));
        builder.AddQuad(StylizedColor.StoneDark, // 천장
            new Vector3(mouthHalf, mouthTop, mouthFront), new Vector3(-mouthHalf, mouthTop, mouthFront),
            new Vector3(-mouthHalf, mouthTop, mouthBack), new Vector3(mouthHalf, mouthTop, mouthBack));
        builder.AddQuad(StylizedColor.Black, // 막다른 어둠
            new Vector3(-mouthHalf, 0.05f, mouthBack), new Vector3(mouthHalf, 0.05f, mouthBack),
            new Vector3(mouthHalf, mouthTop, mouthBack), new Vector3(-mouthHalf, mouthTop, mouthBack));

        // 바닥 돌무더기
        for (int index = 0; index < 5; index++)
        {
            float angle = index / 5f * Mathf.PI * 2f + 0.5f;
            builder.AddLowPolySphere(index % 2 == 0 ? StylizedColor.Stone : StylizedColor.StoneDark,
                new Vector3(Mathf.Sin(angle) * 4.6f, 0.25f, 1.6f + Mathf.Cos(angle) * 1.6f), Vector3.one * (0.5f + index * 0.09f), 1, 0.15f, 95 + index);
        }

        builder.Pop();
    }

    private static void BuildTorch(LowPolyMeshBuilder builder) // 횃불 : 나무 자루 + 천 + 불꽃
    {
        builder.AddLimb(StylizedColor.Bark, new Vector3(0f, 0f, 0f), new Vector3(0f, 0.62f, 0f), 0.045f, 0.05f, 6);
        builder.AddLimb(StylizedColor.ClothCream, new Vector3(0f, 0.58f, 0f), new Vector3(0f, 0.74f, 0f), 0.09f, 0.08f, 6);
        builder.AddCone(StylizedColor.Fire, new Vector3(0f, 0.72f, 0f), 0.11f, 0.3f, 6);
        builder.AddCone(StylizedColor.FireCore, new Vector3(0f, 0.78f, 0f), 0.07f, 0.22f, 5);
    }
}
