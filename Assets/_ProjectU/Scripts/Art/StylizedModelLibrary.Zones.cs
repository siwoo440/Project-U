using UnityEngine;

// 100일차: 새 구역 소품 (해안 · 고대 폐허 · 깊은 숲 · 설산 기슭과 사당 · 사막 · 습지)
// 미터 단위, 바닥 y = 0, 앞(문 · 입구)은 +Z. 부두와 나무길은 +Z 방향으로 뻗는다.
public static partial class StylizedModelLibrary
{
    // 생성 도구가 충돌체 · 위치에 쓰는 크기
    public const float ZoneDockLength = 14f;
    public const float ZoneDockWidth = 3f;
    public const float ZoneDockDeckHeight = 0.3f;
    public const float ZoneBoardwalkLength = 10f;
    public const float ZoneBoardwalkWidth = 1.6f;
    public const float ZoneBoardwalkDeckHeight = 0.25f;
    public const float ZoneOasisRadius = 4.6f;
    public const float ZoneSwampPoolRadius = 5f;

    private static void RegisterZones()
    {
        // 해안
        Register("zone_sea", FitMode.Stretch, b => BuildFlatQuad(b, StylizedColor.Water));
        Register("zone_seabed", FitMode.Stretch, b => BuildFlatQuad(b, StylizedColor.PondDeep));
        Register("zone_shore_foam", FitMode.UniformHeight, BuildShoreFoam);
        Register("zone_dock", FitMode.UniformHeight, BuildDock);
        Register("zone_boat", FitMode.UniformHeight, BuildRowBoat);
        Register("zone_fisher_hut", FitMode.UniformHeight, BuildFisherHut);
        Register("zone_net_rack", FitMode.UniformHeight, BuildNetRack);
        Register("zone_lighthouse", FitMode.UniformHeight, BuildLighthouse);
        Register("zone_tide_rocks", FitMode.UniformHeight, BuildTideRocks);

        // 고대 폐허
        Register("zone_ruin_pillar", FitMode.UniformHeight, b => BuildRuinPillar(b, false));
        Register("zone_ruin_pillar_broken", FitMode.UniformHeight, b => BuildRuinPillar(b, true));
        Register("zone_ruin_arch", FitMode.UniformHeight, BuildRuinArch);
        Register("zone_ruin_wall", FitMode.UniformHeight, BuildRuinWall);
        Register("zone_ruin_floor", FitMode.UniformHeight, BuildRuinFloor);
        Register("zone_ruin_statue", FitMode.UniformHeight, BuildRuinStatue);
        Register("zone_ruin_gate", FitMode.UniformHeight, BuildRuinGate);
        Register("zone_ruin_bookshelf", FitMode.UniformHeight, BuildRuinBookshelf);
        Register("zone_ruin_obelisk", FitMode.UniformHeight, BuildRuinObelisk);
        Register("zone_ruin_treasure", FitMode.UniformHeight, BuildRuinTreasure);
        Register("zone_rune_crystals", FitMode.UniformHeight, BuildRuneCrystals);

        // 깊은 숲
        Register("zone_giant_mushroom", FitMode.UniformHeight, BuildGiantMushroom);
        Register("zone_glow_mushrooms", FitMode.UniformHeight, BuildGlowMushrooms);
        Register("zone_glow_flowers", FitMode.UniformHeight, BuildGlowFlowers);
        Register("zone_fairy_ring", FitMode.UniformHeight, BuildFairyRing);
        Register("zone_witch_hut", FitMode.UniformHeight, BuildWitchHut);
        Register("zone_great_stump", FitMode.UniformHeight, BuildGreatStump);

        // 설산 기슭 · 사당
        Register("zone_snow_pine", FitMode.UniformHeight, BuildSnowPine);
        Register("zone_ice_rock", FitMode.UniformHeight, BuildIceRock);
        Register("zone_snow_tent", FitMode.UniformHeight, BuildSnowTent);
        Register("zone_fur_rack", FitMode.UniformHeight, BuildFurRack);
        Register("zone_snow_pile", FitMode.UniformHeight, BuildSnowPile);
        Register("zone_torii", FitMode.UniformHeight, BuildTorii);
        Register("zone_stone_lantern", FitMode.UniformHeight, BuildStoneLantern);
        Register("zone_hokora", FitMode.UniformHeight, BuildHokora);
        Register("zone_shrine_house", FitMode.UniformHeight, BuildShrineHouse); // 101일차: 카스미의 사당 집

        // 사막
        Register("zone_cactus", FitMode.UniformHeight, BuildCactus);
        Register("zone_mesa_rock", FitMode.UniformHeight, BuildMesaRock);
        Register("zone_palm", FitMode.UniformHeight, BuildPalm);
        Register("zone_desert_tent", FitMode.UniformHeight, BuildDesertTent);
        Register("zone_bones", FitMode.UniformHeight, BuildBones);
        Register("zone_oasis_pool", FitMode.UniformHeight, BuildOasisPool);
        Register("zone_dune", FitMode.UniformHeight, BuildDune);

        // 습지
        Register("zone_swamp_pool", FitMode.UniformHeight, BuildSwampPool);
        Register("zone_dead_tree", FitMode.UniformHeight, BuildDeadTree);
        Register("zone_boardwalk", FitMode.UniformHeight, BuildBoardwalk);
        Register("zone_stilt_hut", FitMode.UniformHeight, BuildStiltHut);
    }

    // ---------------------------------------------------------------- 해안

    // 가로 · 세로 1m 평면 (바다 · 바다 밑). 배치할 때 크기를 늘려 쓴다.
    private static void BuildFlatQuad(LowPolyMeshBuilder b, StylizedColor color)
    {
        b.AddQuad(color, new Vector3(-0.5f, 0f, -0.5f), new Vector3(-0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, -0.5f));
    }

    // 물가 거품 (Z 방향 20m)
    private static void BuildShoreFoam(LowPolyMeshBuilder b)
    {
        System.Random random = new System.Random(1001);

        for (int index = 0; index < 16; index++)
        {
            float z = -10f + index * 1.3f + Rand(random, -0.3f, 0.3f);
            b.AddLowPolySphere(StylizedColor.Glass, new Vector3(Rand(random, -0.4f, 0.4f), 0f, z), new Vector3(Rand(random, 0.5f, 0.9f), 0.02f, Rand(random, 0.7f, 1.1f)), 0, 0f, 1002 + index);
        }
    }

    // 나무 부두 : 경사로 · 널빤지 · 기둥 · 밧줄 난간 · 끝의 등불과 계선주
    private static void BuildDock(LowPolyMeshBuilder b)
    {
        float half = ZoneDockWidth * 0.5f;
        float deck = ZoneDockDeckHeight;

        b.Push(new Vector3(0f, deck * 0.5f, 0.6f), Euler(-14f, 0f, 0f), Vector3.one);
        b.AddBox(StylizedColor.WoodPlank, Vector3.zero, new Vector3(ZoneDockWidth - 0.4f, 0.06f, 1.25f));
        b.Pop();

        for (int index = 0; index < 26; index++)
        {
            float z = 1.2f + index * 0.5f + 0.25f;
            b.AddBox(index % 3 == 0 ? StylizedColor.WoodLight : StylizedColor.WoodPlank, new Vector3(0f, deck - 0.04f, z), new Vector3(ZoneDockWidth, 0.08f, 0.46f));
        }

        for (int side = -1; side <= 1; side += 2)
        {
            b.AddBox(StylizedColor.WoodDark, new Vector3(side * (half - 0.3f), deck - 0.14f, 7.6f), new Vector3(0.15f, 0.12f, 12.8f));

            for (int post = 0; post < 6; post++)
            {
                float z = 1.5f + post * 2.5f;
                b.AddCylinder(StylizedColor.WoodDark, new Vector3(side * (half - 0.05f), -1.2f, z), 0.1f, 2.1f, 6);

                if (post > 0)
                {
                    b.AddLimb(StylizedColor.Rope, new Vector3(side * (half - 0.05f), 0.78f, z - 2.5f), new Vector3(side * (half - 0.05f), 0.72f, z), 0.02f, 0.02f, 4);
                }
            }

            b.AddCylinder(StylizedColor.IronDark, new Vector3(side * (half - 0.35f), deck, ZoneDockLength - 0.4f), 0.1f, 0.3f, 6);
            b.AddLowPolySphere(StylizedColor.IronDark, new Vector3(side * (half - 0.35f), deck + 0.32f, ZoneDockLength - 0.4f), Vector3.one * 0.12f, 0, 0f, 1030 + side);
        }

        b.AddLimb(StylizedColor.WoodDark, new Vector3(half - 0.1f, deck, ZoneDockLength - 1.2f), new Vector3(half - 0.1f, deck + 2.2f, ZoneDockLength - 1.2f), 0.06f, 0.05f, 6);
        b.AddBox(StylizedColor.WoodDark, new Vector3(half - 0.35f, deck + 2.15f, ZoneDockLength - 1.2f), new Vector3(0.5f, 0.06f, 0.06f));
        b.AddLowPolySphere(StylizedColor.LampGlow, new Vector3(half - 0.55f, deck + 1.95f, ZoneDockLength - 1.2f), Vector3.one * 0.1f, 0, 0f, 1034);
    }

    // 작은 나룻배 (길이 3.2m, 물에 뜬 높이 기준)
    private static void BuildRowBoat(LowPolyMeshBuilder b)
    {
        b.AddLowPolySphere(StylizedColor.WoodPlank, new Vector3(0f, 0.25f, 0f), new Vector3(0.62f, 0.32f, 1.6f), 1, 0f, 1040);
        b.AddBox(StylizedColor.WoodDark, new Vector3(0f, 0.43f, 0f), new Vector3(0.9f, 0.02f, 2.4f));
        b.Push(new Vector3(0f, 0.46f, 0f), Quaternion.identity, new Vector3(0.62f, 1f, 1.6f));
        b.AddTorus(StylizedColor.WoodDark, Vector3.zero, 0.96f, 0.05f, 16, 4);
        b.Pop();

        foreach (float z in new[] { -0.5f, 0.45f })
        {
            b.AddBox(StylizedColor.WoodLight, new Vector3(0f, 0.5f, z), new Vector3(1.0f, 0.05f, 0.24f));
        }

        for (int side = -1; side <= 1; side += 2)
        {
            b.AddLimb(StylizedColor.WoodLight, new Vector3(side * 0.2f, 0.56f, -0.2f), new Vector3(side * 1.1f, 0.12f, 0.4f), 0.025f, 0.025f, 5);
            b.AddBox(StylizedColor.WoodLight, new Vector3(side * 1.12f, 0.1f, 0.42f), new Vector3(0.14f, 0.02f, 0.4f));
        }
    }

    // 어부 오두막 (4 × 4m, 파란 지붕 · 그물 · 부표)
    private static void BuildFisherHut(LowPolyMeshBuilder b)
    {
        b.AddBox(StylizedColor.StoneLight, new Vector3(0f, 0.1f, 0f), new Vector3(4.2f, 0.2f, 4.2f));
        b.AddBox(StylizedColor.WoodPlank, new Vector3(0f, 1.3f, 0f), new Vector3(4f, 2.2f, 4f));

        foreach (float x in new[] { -1.98f, 1.98f })
        {
            foreach (float z in new[] { -1.98f, 1.98f })
            {
                b.AddBox(StylizedColor.WoodDark, new Vector3(x, 1.3f, z), new Vector3(0.16f, 2.2f, 0.16f));
            }
        }

        b.AddWedge(StylizedColor.ClothBlue, new Vector3(0f, 3.0f, 0f), new Vector3(4.6f, 1.3f, 4.6f));
        b.AddBox(StylizedColor.WoodDark, new Vector3(0.7f, 1.1f, 2.01f), new Vector3(0.9f, 1.8f, 0.06f));
        b.AddBox(StylizedColor.WoodLight, new Vector3(-0.9f, 1.5f, 2.01f), new Vector3(0.9f, 0.8f, 0.06f));
        b.AddBox(StylizedColor.Glass, new Vector3(-0.9f, 1.5f, 2.03f), new Vector3(0.74f, 0.64f, 0.04f));

        // 옆벽 그물 · 부표 · 노
        for (int index = 0; index < 5; index++)
        {
            b.AddLimb(StylizedColor.Rope, new Vector3(2.03f, 2.1f, -1.4f + index * 0.6f), new Vector3(2.05f, 0.7f, -1.3f + index * 0.55f), 0.012f, 0.012f, 3);
        }

        for (int index = 0; index < 3; index++)
        {
            b.AddLimb(StylizedColor.Rope, new Vector3(2.04f, 1.9f - index * 0.5f, -1.5f), new Vector3(2.04f, 1.8f - index * 0.45f, 1.1f), 0.012f, 0.012f, 3);
        }

        b.AddLowPolySphere(StylizedColor.ClothRed, new Vector3(2.12f, 1.2f, 1.4f), Vector3.one * 0.14f, 0, 0f, 1050);
        b.AddLowPolySphere(StylizedColor.White, new Vector3(2.12f, 0.9f, 1.62f), Vector3.one * 0.12f, 0, 0f, 1051);
        b.AddLimb(StylizedColor.WoodLight, new Vector3(-1.7f, 0.05f, 2.2f), new Vector3(-1.5f, 2.1f, 2.1f), 0.03f, 0.03f, 5);
        b.AddBox(StylizedColor.WoodLight, new Vector3(-1.72f, 0.3f, 2.21f), new Vector3(0.16f, 0.5f, 0.03f));
    }

    // 그물 건조대 (말리는 생선 두 마리)
    private static void BuildNetRack(LowPolyMeshBuilder b)
    {
        for (int side = -1; side <= 1; side += 2)
        {
            b.AddLimb(StylizedColor.WoodDark, new Vector3(side * 1.2f, 0f, 0f), new Vector3(side * 1.2f, 2f, 0f), 0.06f, 0.05f, 6);
        }

        b.AddLimb(StylizedColor.Rope, new Vector3(-1.2f, 1.9f, 0f), new Vector3(1.2f, 1.9f, 0f), 0.02f, 0.02f, 4);

        for (int index = 0; index < 6; index++)
        {
            float x = -1f + index * 0.4f;
            b.AddLimb(StylizedColor.Rope, new Vector3(x, 1.9f, 0f), new Vector3(x + 0.05f, 0.6f + Mathf.Abs(index - 2.5f) * 0.1f, 0.05f), 0.01f, 0.01f, 3);
        }

        for (int index = 0; index < 3; index++)
        {
            float y = 1.55f - index * 0.35f;
            b.AddLimb(StylizedColor.Rope, new Vector3(-1.05f, y, 0.02f), new Vector3(1.05f, y - 0.08f, 0.02f), 0.01f, 0.01f, 3);
        }

        b.AddLowPolySphere(StylizedColor.FishSilver, new Vector3(-0.5f, 1.6f, 0.1f), new Vector3(0.06f, 0.2f, 0.04f), 0, 0f, 1060);
        b.AddLowPolySphere(StylizedColor.FishOlive, new Vector3(0.6f, 1.55f, 0.1f), new Vector3(0.06f, 0.22f, 0.04f), 0, 0f, 1061);
    }

    // 작은 등대 (7.8m, 흰색 · 빨간 줄무늬)
    private static void BuildLighthouse(LowPolyMeshBuilder b)
    {
        b.AddFrustum(StylizedColor.StoneLight, Vector3.zero, 1.6f, 1.3f, 1f, 10);

        for (int index = 0; index < 4; index++)
        {
            float t0 = index / 4f;
            float t1 = (index + 1) / 4f;
            b.AddFrustum(index % 2 == 0 ? StylizedColor.White : StylizedColor.ClothRed, new Vector3(0f, 1f + index * 1.5f, 0f), Mathf.Lerp(1.2f, 0.8f, t0), Mathf.Lerp(1.2f, 0.8f, t1), 1.5f, 10, false, false);
        }

        b.AddCylinder(StylizedColor.IronDark, new Vector3(0f, 7f, 0f), 1.15f, 0.12f, 10);
        b.AddTorus(StylizedColor.IronDark, new Vector3(0f, 7.55f, 0f), 1.05f, 0.03f, 12, 3);
        b.AddCylinder(StylizedColor.Glass, new Vector3(0f, 7.12f, 0f), 0.7f, 1f, 8);
        b.AddLowPolySphere(StylizedColor.LampGlow, new Vector3(0f, 7.6f, 0f), Vector3.one * 0.35f, 1, 0f, 1070);
        b.AddCone(StylizedColor.ClothRed, new Vector3(0f, 8.1f, 0f), 0.85f, 0.9f, 8);
        b.AddBox(StylizedColor.WoodDark, new Vector3(0f, 0.95f, 1.27f), new Vector3(0.8f, 1.5f, 0.1f));
    }

    // 갯바위와 바위 웅덩이 (산호 · 불가사리 · 조개)
    private static void BuildTideRocks(LowPolyMeshBuilder b)
    {
        b.AddLowPolySphere(StylizedColor.Stone, new Vector3(0f, 0.45f, 0f), new Vector3(1.1f, 0.6f, 0.9f), 1, 0.22f, 1080);
        b.AddLowPolySphere(StylizedColor.StoneDark, new Vector3(1.1f, 0.3f, 0.6f), new Vector3(0.6f, 0.4f, 0.55f), 1, 0.25f, 1081);
        b.AddLowPolySphere(StylizedColor.StoneLight, new Vector3(-1.0f, 0.25f, 0.5f), new Vector3(0.55f, 0.3f, 0.5f), 1, 0.25f, 1082);
        b.AddDisc(StylizedColor.Water, new Vector3(0.2f, 0.06f, 1.2f), 0.6f, 10);
        b.AddLowPolySphere(StylizedColor.Coral, new Vector3(0.3f, 0.95f, 0.3f), new Vector3(0.12f, 0.1f, 0.12f), 0, 0f, 1083);
        b.AddLowPolySphere(StylizedColor.Coral, new Vector3(-0.4f, 0.1f, 1.1f), new Vector3(0.15f, 0.12f, 0.12f), 0, 0f, 1084);

        for (int arm = 0; arm < 5; arm++)
        {
            float angle = arm * Mathf.PI * 0.4f;
            b.AddLimb(StylizedColor.Coral, new Vector3(0.9f, 0.62f, 0.25f), new Vector3(0.9f + Mathf.Cos(angle) * 0.14f, 0.6f, 0.25f + Mathf.Sin(angle) * 0.14f), 0.03f, 0.012f, 4);
        }

        b.AddLowPolySphere(StylizedColor.White, new Vector3(-0.7f, 0.03f, 1.3f), new Vector3(0.07f, 0.03f, 0.06f), 0, 0f, 1085);
        b.AddLowPolySphere(StylizedColor.Egg, new Vector3(0.8f, 0.03f, 1.4f), new Vector3(0.06f, 0.025f, 0.05f), 0, 0f, 1086);
    }

    // ---------------------------------------------------------------- 고대 폐허

    private static void AddMoss(LowPolyMeshBuilder b, Vector3 center, Vector3 radii, int seed)
    {
        b.AddLowPolySphere(StylizedColor.LeafDark, center, radii, 1, 0.2f, seed);
    }

    // 기둥 (4.2m) · 부러진 기둥 (옆에 떨어진 토막)
    private static void BuildRuinPillar(LowPolyMeshBuilder b, bool broken)
    {
        b.AddBeveledBox(StylizedColor.RuinStone, new Vector3(0f, 0.18f, 0f), new Vector3(1.1f, 0.36f, 1.1f), 0.05f);

        if (!broken)
        {
            b.AddFrustum(StylizedColor.RuinStone, new Vector3(0f, 0.36f, 0f), 0.38f, 0.33f, 3.5f, 8);
            b.AddBeveledBox(StylizedColor.RuinStone, new Vector3(0f, 4.0f, 0f), new Vector3(1.0f, 0.3f, 1.0f), 0.05f);
            AddMoss(b, new Vector3(0.1f, 4.18f, 0.05f), new Vector3(0.4f, 0.08f, 0.35f), 1100);
            AddMoss(b, new Vector3(0.3f, 0.4f, 0.25f), new Vector3(0.3f, 0.12f, 0.25f), 1101);
            return;
        }

        b.AddFrustum(StylizedColor.RuinStone, new Vector3(0f, 0.36f, 0f), 0.38f, 0.35f, 1.4f, 8);
        b.AddLowPolySphere(StylizedColor.RuinStone, new Vector3(0f, 1.8f, 0f), new Vector3(0.36f, 0.22f, 0.36f), 1, 0.3f, 1102);
        b.Push(new Vector3(1.2f, 0.33f, 0.5f), Euler(0f, 30f, 90f), Vector3.one);
        b.AddFrustum(StylizedColor.RuinStone, new Vector3(0f, -0.55f, 0f), 0.35f, 0.34f, 1.1f, 8);
        b.Pop();
        b.AddLowPolySphere(StylizedColor.StoneDark, new Vector3(-0.7f, 0.12f, 0.6f), new Vector3(0.2f, 0.12f, 0.18f), 0, 0.2f, 1103);
        AddMoss(b, new Vector3(1.0f, 0.62f, 0.55f), new Vector3(0.3f, 0.08f, 0.25f), 1104);
    }

    // 아치 문 (폭 5m · 높이 5.6m, 가운데 쐐기돌에 빛나는 룬)
    private static void BuildRuinArch(LowPolyMeshBuilder b)
    {
        for (int side = -1; side <= 1; side += 2)
        {
            b.AddBeveledBox(StylizedColor.RuinStone, new Vector3(side * 2.05f, 1.7f, 0f), new Vector3(0.9f, 3.4f, 1.0f), 0.05f);
        }

        for (int index = 0; index < 7; index++)
        {
            float theta = Mathf.PI * (index + 0.5f) / 7f;
            Vector3 center = new Vector3(Mathf.Cos(theta) * 2.05f, 3.4f + Mathf.Sin(theta) * 2.05f, 0f);
            b.Push(center, Euler(0f, 0f, theta * Mathf.Rad2Deg - 90f), Vector3.one);
            b.AddBeveledBox(index == 3 ? StylizedColor.StoneLight : StylizedColor.RuinStone, Vector3.zero, new Vector3(0.97f, 0.9f, 1.0f), 0.04f);

            if (index == 3)
            {
                b.AddBox(StylizedColor.RuneGlow, new Vector3(0f, 0f, 0.51f), new Vector3(0.3f, 0.4f, 0.02f));
            }

            b.Pop();
        }

        for (int index = 0; index < 3; index++)
        {
            float x = -1.4f + index * 1.3f;
            b.AddLimb(StylizedColor.LeafDark, new Vector3(x, 4.9f - Mathf.Abs(x) * 0.5f, 0.52f), new Vector3(x + 0.1f, 3.6f - index * 0.3f, 0.55f), 0.03f, 0.02f, 4);
        }

        AddMoss(b, new Vector3(-2.0f, 3.5f, 0.3f), new Vector3(0.45f, 0.1f, 0.5f), 1110);
    }

    // 무너진 벽 (길이 6m, 계단처럼 부서진 윗부분 · 떨어진 벽돌)
    private static void BuildRuinWall(LowPolyMeshBuilder b)
    {
        float[] heights = { 2.8f, 3.0f, 2.2f, 1.4f, 0.9f, 1.8f };

        for (int index = 0; index < heights.Length; index++)
        {
            float h = heights[index];
            b.AddBeveledBox(index % 2 == 0 ? StylizedColor.RuinStone : StylizedColor.Stone, new Vector3(-2.5f + index, h * 0.5f, (index % 3) * 0.03f), new Vector3(1.0f, h, 0.9f), 0.04f);
        }

        System.Random random = new System.Random(1120);

        for (int index = 0; index < 5; index++)
        {
            b.Push(new Vector3(Rand(random, -1f, 2.5f), 0.14f, Rand(random, 0.7f, 1.4f)), Euler(0f, Rand(random, 0f, 90f), Rand(random, -8f, 8f)), Vector3.one);
            b.AddBeveledBox(StylizedColor.RuinStone, Vector3.zero, new Vector3(0.5f, 0.28f, 0.34f), 0.03f);
            b.Pop();
        }

        AddMoss(b, new Vector3(-1.5f, 3.02f, 0f), new Vector3(0.45f, 0.08f, 0.4f), 1121);
        AddMoss(b, new Vector3(0.5f, 0.3f, 0.45f), new Vector3(0.6f, 0.2f, 0.2f), 1122);
    }

    // 깨진 바닥 돌 (8 × 8m, 일부가 빠져 있음)
    private static void BuildRuinFloor(LowPolyMeshBuilder b)
    {
        System.Random random = new System.Random(1130);

        for (int x = 0; x < 8; x++)
        {
            for (int z = 0; z < 8; z++)
            {
                if (random.NextDouble() < 0.14)
                {
                    continue;
                }

                float height = Rand(random, 0.05f, 0.09f);
                StylizedColor color = random.NextDouble() < 0.3 ? StylizedColor.StoneLight : StylizedColor.RuinStone;
                b.AddBox(color, new Vector3(-3.5f + x, height * 0.5f, -3.5f + z), new Vector3(Rand(random, 0.88f, 0.96f), height, Rand(random, 0.88f, 0.96f)));
            }
        }
    }

    // 석상 (책을 든 옛 학자, 한쪽 팔은 부러짐)
    private static void BuildRuinStatue(LowPolyMeshBuilder b)
    {
        b.AddBeveledBox(StylizedColor.RuinStone, new Vector3(0f, 0.5f, 0f), new Vector3(1.3f, 1.0f, 1.3f), 0.06f);
        b.AddBox(StylizedColor.StoneLight, new Vector3(0f, 1.05f, 0f), new Vector3(1.4f, 0.1f, 1.4f));
        b.AddFrustum(StylizedColor.RuinStone, new Vector3(0f, 1.1f, 0f), 0.48f, 0.26f, 1.5f, 8);
        b.AddBeveledBox(StylizedColor.RuinStone, new Vector3(0f, 2.75f, 0f), new Vector3(0.58f, 0.5f, 0.36f), 0.08f);
        b.AddLowPolySphere(StylizedColor.RuinStone, new Vector3(0f, 3.22f, 0f), Vector3.one * 0.23f, 1, 0.05f, 1140);
        b.AddLimb(StylizedColor.RuinStone, new Vector3(0.3f, 2.9f, 0f), new Vector3(0.42f, 2.5f, 0.2f), 0.09f, 0.08f, 6);
        b.AddBeveledBox(StylizedColor.StoneLight, new Vector3(0.36f, 2.55f, 0.34f), new Vector3(0.36f, 0.08f, 0.28f), 0.02f);
        b.AddLimb(StylizedColor.RuinStone, new Vector3(-0.3f, 2.9f, 0f), new Vector3(-0.38f, 2.66f, 0.04f), 0.09f, 0.085f, 6);
        AddMoss(b, new Vector3(-0.25f, 3.0f, -0.05f), new Vector3(0.18f, 0.06f, 0.18f), 1141);
        AddMoss(b, new Vector3(0.4f, 1.12f, 0.45f), new Vector3(0.35f, 0.07f, 0.25f), 1142);
    }

    // 지하 유적 입구 (반쯤 묻힌 돌 언덕 · 문틀 · 룬으로 봉인된 문 · 계단)
    private static void BuildRuinGate(LowPolyMeshBuilder b)
    {
        b.AddLowPolySphere(StylizedColor.StoneDark, new Vector3(0f, 0f, -0.6f), new Vector3(3.2f, 2.8f, 2.6f), 1, 0.12f, 1150);
        AddMoss(b, new Vector3(-0.8f, 2.55f, -0.8f), new Vector3(1.4f, 0.3f, 1.2f), 1151);

        for (int side = -1; side <= 1; side += 2)
        {
            b.AddBeveledBox(StylizedColor.RuinStone, new Vector3(side * 1.35f, 1.5f, 1.9f), new Vector3(0.6f, 3f, 0.8f), 0.04f);
            b.AddLowPolySphere(StylizedColor.RuneGlow, new Vector3(side * 2.1f, 0.9f, 2.3f), new Vector3(0.12f, 0.2f, 0.12f), 0, 0f, 1152 + side);
            b.AddCylinder(StylizedColor.RuinStone, new Vector3(side * 2.1f, 0f, 2.3f), 0.2f, 0.7f, 6);
        }

        b.AddBeveledBox(StylizedColor.RuinStone, new Vector3(0f, 3.25f, 1.9f), new Vector3(3.4f, 0.6f, 0.9f), 0.04f);
        b.AddBox(StylizedColor.IronDark, new Vector3(0f, 1.4f, 1.9f), new Vector3(2.1f, 2.8f, 0.22f));
        b.Push(new Vector3(0f, 1.5f, 2.02f), Euler(90f, 0f, 0f), Vector3.one);
        b.AddTorus(StylizedColor.RuneGlow, Vector3.zero, 0.6f, 0.04f, 16, 3);
        b.AddTorus(StylizedColor.RuneGlow, Vector3.zero, 0.32f, 0.03f, 12, 3);
        b.Pop();
        b.AddBox(StylizedColor.RuneGlow, new Vector3(0f, 1.5f, 2.02f), new Vector3(0.05f, 1.1f, 0.02f));
        b.AddBox(StylizedColor.RuinStone, new Vector3(0f, 0.08f, 2.6f), new Vector3(2.6f, 0.16f, 0.6f));
        b.AddBox(StylizedColor.RuinStone, new Vector3(0f, 0.04f, 3.1f), new Vector3(2.8f, 0.08f, 0.5f));
    }

    // 돌 책장 (책 · 바닥에 떨어진 책)
    private static void BuildRuinBookshelf(LowPolyMeshBuilder b)
    {
        for (int side = -1; side <= 1; side += 2)
        {
            b.AddBox(StylizedColor.RuinStone, new Vector3(side * 0.9f, 1.2f, 0f), new Vector3(0.14f, 2.4f, 0.5f));
        }

        b.AddBox(StylizedColor.RuinStone, new Vector3(0f, 2.36f, 0f), new Vector3(1.94f, 0.12f, 0.52f));
        b.AddBox(StylizedColor.StoneDark, new Vector3(0f, 1.2f, -0.23f), new Vector3(1.7f, 2.3f, 0.04f));
        StylizedColor[] books = { StylizedColor.ClothRed, StylizedColor.ClothBlue, StylizedColor.Leather, StylizedColor.ClothGreen, StylizedColor.SeedPaper, StylizedColor.BerryPurple };
        System.Random random = new System.Random(1160);

        for (int shelf = 0; shelf < 4; shelf++)
        {
            float y = 0.06f + shelf * 0.58f;
            b.AddBox(StylizedColor.RuinStone, new Vector3(0f, y, 0f), new Vector3(1.7f, 0.08f, 0.48f));
            float x = -0.8f;

            while (x < 0.75f)
            {
                float width = Rand(random, 0.06f, 0.12f);

                if (random.NextDouble() < 0.18)
                {
                    x += width * 2f; // 빈 자리
                    continue;
                }

                float height = Rand(random, 0.3f, 0.44f);
                b.AddBox(books[random.Next(books.Length)], new Vector3(x + width * 0.5f, y + 0.04f + height * 0.5f, 0.02f), new Vector3(width, height, 0.34f));
                x += width + 0.01f;
            }
        }

        b.Push(new Vector3(0.4f, 0.03f, 0.55f), Euler(0f, 25f, 0f), Vector3.one);
        b.AddBox(StylizedColor.ClothRed, Vector3.zero, new Vector3(0.3f, 0.06f, 0.22f));
        b.Pop();
        b.Push(new Vector3(-0.3f, 0.03f, 0.7f), Euler(0f, -40f, 0f), Vector3.one);
        b.AddBox(StylizedColor.SeedPaper, Vector3.zero, new Vector3(0.36f, 0.02f, 0.24f));
        b.Pop();
    }

    // 검은 오벨리스크 (5m, 빛나는 룬 · 떠 있는 조각)
    private static void BuildRuinObelisk(LowPolyMeshBuilder b)
    {
        b.AddBeveledBox(StylizedColor.StoneDark, new Vector3(0f, 0.15f, 0f), new Vector3(1.8f, 0.3f, 1.8f), 0.05f);
        b.AddBeveledBox(StylizedColor.StoneDark, new Vector3(0f, 0.4f, 0f), new Vector3(1.3f, 0.2f, 1.3f), 0.04f);
        b.AddFrustum(StylizedColor.Chitin, new Vector3(0f, 0.5f, 0f), 0.55f, 0.36f, 4.2f, 4, true, true, 45f);
        b.AddFrustum(StylizedColor.Chitin, new Vector3(0f, 4.7f, 0f), 0.36f, 0f, 0.6f, 4, false, false, 45f);

        for (int index = 0; index < 4; index++)
        {
            float y = 1.2f + index * 0.85f;
            float inset = Mathf.Lerp(0.39f, 0.26f, (y - 0.5f) / 4.2f);
            b.AddBox(StylizedColor.RuneGlow, new Vector3(0f, y, inset), new Vector3(0.2f, 0.34f, 0.02f));
            b.AddBox(StylizedColor.RuneGlow, new Vector3(inset, y + 0.3f, 0f), new Vector3(0.02f, 0.24f, 0.16f));
        }

        b.AddLowPolySphere(StylizedColor.RuneGlow, new Vector3(0.7f, 4.4f, 0.2f), new Vector3(0.08f, 0.14f, 0.08f), 0, 0f, 1170);
        b.AddLowPolySphere(StylizedColor.RuneGlow, new Vector3(-0.6f, 3.9f, -0.4f), new Vector3(0.06f, 0.1f, 0.06f), 0, 0f, 1171);
    }

    // 보물 더미 (금화 · 열린 상자 · 잔 · 보석)
    private static void BuildRuinTreasure(LowPolyMeshBuilder b)
    {
        System.Random random = new System.Random(1180);
        b.AddLowPolySphere(StylizedColor.Gold, new Vector3(0f, 0.05f, 0f), new Vector3(1.0f, 0.3f, 0.8f), 1, 0.1f, 1181);

        for (int index = 0; index < 14; index++)
        {
            float angle = Rand(random, 0f, Mathf.PI * 2f);
            float distance = Rand(random, 0.6f, 1.3f);
            b.AddLowPolySphere(StylizedColor.Gold, new Vector3(Mathf.Cos(angle) * distance, 0.02f, Mathf.Sin(angle) * distance), new Vector3(0.07f, 0.015f, 0.07f), 0, 0f, 1182 + index);
        }

        b.AddBeveledBox(StylizedColor.WoodDark, new Vector3(-0.6f, 0.3f, -0.5f), new Vector3(0.8f, 0.5f, 0.55f), 0.03f);
        b.AddBox(StylizedColor.Gold, new Vector3(-0.6f, 0.56f, -0.5f), new Vector3(0.7f, 0.04f, 0.45f));
        b.Push(new Vector3(-0.6f, 0.55f, -0.78f), Euler(-100f, 0f, 0f), Vector3.one);
        b.AddBeveledBox(StylizedColor.WoodDark, new Vector3(0f, 0.05f, 0.28f), new Vector3(0.82f, 0.1f, 0.57f), 0.03f);
        b.Pop();
        b.AddFrustum(StylizedColor.Gold, new Vector3(0.7f, 0.2f, 0.3f), 0.08f, 0.14f, 0.2f, 8, false, true);
        b.AddCylinder(StylizedColor.Gold, new Vector3(0.7f, 0f, 0.3f), 0.1f, 0.2f, 6);
        b.AddLowPolySphere(StylizedColor.Crystal, new Vector3(0.2f, 0.36f, 0.1f), Vector3.one * 0.08f, 0, 0f, 1200);
        b.AddLowPolySphere(StylizedColor.AppleRed, new Vector3(-0.2f, 0.33f, 0.3f), Vector3.one * 0.07f, 0, 0f, 1201);
        b.AddLowPolySphere(StylizedColor.BerryPurple, new Vector3(0.45f, 0.24f, -0.3f), Vector3.one * 0.06f, 0, 0f, 1202);
    }

    // 빛나는 룬 결정 무리
    private static void BuildRuneCrystals(LowPolyMeshBuilder b)
    {
        b.AddLowPolySphere(StylizedColor.StoneDark, new Vector3(0f, 0.15f, 0f), new Vector3(0.7f, 0.25f, 0.6f), 1, 0.2f, 1210);
        float[] heights = { 1.2f, 0.8f, 0.9f, 0.6f, 0.7f };

        for (int index = 0; index < heights.Length; index++)
        {
            float angle = index * 1.3f;
            b.Push(new Vector3(Mathf.Cos(angle) * 0.25f * (index > 0 ? 1 : 0), 0.2f, Mathf.Sin(angle) * 0.22f * (index > 0 ? 1 : 0)), Euler(Mathf.Sin(angle) * 18f, 0f, Mathf.Cos(angle) * 18f * (index > 0 ? 1 : 0)), Vector3.one);
            b.AddCylinder(index % 2 == 0 ? StylizedColor.RuneGlow : StylizedColor.Crystal, Vector3.zero, 0.1f, heights[index] * 0.7f, 5);
            b.AddCone(index % 2 == 0 ? StylizedColor.RuneGlow : StylizedColor.Crystal, new Vector3(0f, heights[index] * 0.7f, 0f), 0.1f, heights[index] * 0.3f, 5);
            b.Pop();
        }
    }

    // ---------------------------------------------------------------- 깊은 숲

    // 거대 버섯 (3.4m, 흰 점 · 아래 주름)
    private static void BuildGiantMushroom(LowPolyMeshBuilder b)
    {
        b.AddLimb(StylizedColor.MushroomStem, new Vector3(0f, 0f, 0f), new Vector3(0.1f, 1.4f, 0f), 0.45f, 0.36f, 8);
        b.AddLimb(StylizedColor.MushroomStem, new Vector3(0.1f, 1.4f, 0f), new Vector3(0f, 2.6f, 0f), 0.36f, 0.32f, 8);
        b.Push(new Vector3(0f, 2.52f, 0f), Euler(180f, 0f, 0f), Vector3.one);
        b.AddDisc(StylizedColor.MushroomStem, Vector3.zero, 1.55f, 12);
        b.Pop();
        b.AddFrustum(StylizedColor.MushroomCap, new Vector3(0f, 2.5f, 0f), 1.6f, 1.1f, 0.45f, 12, false, false);
        b.AddFrustum(StylizedColor.MushroomCap, new Vector3(0f, 2.95f, 0f), 1.1f, 0f, 0.6f, 12, false, false);
        System.Random random = new System.Random(1220);

        for (int index = 0; index < 8; index++)
        {
            float angle = index * Mathf.PI * 2f / 8f + Rand(random, -0.2f, 0.2f);
            float ring = index % 2 == 0 ? 1.15f : 0.7f;
            float y = index % 2 == 0 ? 2.8f : 3.15f;
            b.AddLowPolySphere(StylizedColor.White, new Vector3(Mathf.Cos(angle) * ring, y, Mathf.Sin(angle) * ring), Vector3.one * Rand(random, 0.1f, 0.16f), 0, 0f, 1221 + index);
        }
    }

    // 빛나는 작은 버섯 무리
    private static void BuildGlowMushrooms(LowPolyMeshBuilder b)
    {
        System.Random random = new System.Random(1230);

        for (int index = 0; index < 6; index++)
        {
            float angle = index * 1.1f;
            float distance = index == 0 ? 0f : Rand(random, 0.3f, 0.7f);
            float height = Rand(random, 0.25f, 0.6f);
            float cap = Rand(random, 0.14f, 0.3f);
            Vector3 root = new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);
            b.AddCylinder(StylizedColor.MushroomStem, root, cap * 0.28f, height, 6);
            b.AddFrustum(StylizedColor.GlowMushroom, root + Vector3.up * height, cap, 0f, cap * 0.8f, 8, true, false);
        }
    }

    // 빛나는 꽃밭 (3 × 3m, 반딧불)
    private static void BuildGlowFlowers(LowPolyMeshBuilder b)
    {
        System.Random random = new System.Random(1240);
        StylizedColor[] petals = { StylizedColor.GlowMushroom, StylizedColor.Flower, StylizedColor.GlowMushroom, StylizedColor.FlowerYellow };

        for (int index = 0; index < 28; index++)
        {
            Vector3 root = new Vector3(Rand(random, -1.5f, 1.5f), 0f, Rand(random, -1.5f, 1.5f));
            float height = Rand(random, 0.35f, 0.8f);
            b.AddLimb(StylizedColor.Leaf, root, root + Vector3.up * height, 0.015f, 0.012f, 4);
            AddSmallFlower(b, root + Vector3.up * height, Rand(random, 0.08f, 0.13f), petals[index % petals.Length], 1250 + index * 6);
        }

        for (int index = 0; index < 7; index++)
        {
            b.AddLowPolySphere(StylizedColor.LampGlow, new Vector3(Rand(random, -1.5f, 1.5f), Rand(random, 0.8f, 1.6f), Rand(random, -1.5f, 1.5f)), Vector3.one * 0.03f, 0, 0f, 1450 + index);
        }
    }

    // 요정의 버섯 고리 (반지름 2m)
    private static void BuildFairyRing(LowPolyMeshBuilder b)
    {
        for (int index = 0; index < 12; index++)
        {
            float angle = index * Mathf.PI * 2f / 12f;
            AddMushroom(b, new Vector3(Mathf.Cos(angle) * 2f, 0f, Mathf.Sin(angle) * 2f), 0.55f + (index % 3) * 0.15f, (index % 2 == 0 ? 1 : -1) * 6f);
        }

        for (int index = 0; index < 5; index++)
        {
            float angle = index * 1.3f;
            b.AddLowPolySphere(StylizedColor.GlowMushroom, new Vector3(Mathf.Cos(angle) * 1.1f, 0.9f + index * 0.18f, Mathf.Sin(angle) * 1.1f), Vector3.one * 0.04f, 0, 0f, 1360 + index);
        }
    }

    // 마녀의 오두막 (기울어진 벽 · 휘어진 뾰족 지붕 · 둥근 창 · 가마솥 · 까마귀)
    private static void BuildWitchHut(LowPolyMeshBuilder b)
    {
        b.AddBox(StylizedColor.StoneDark, new Vector3(0f, 0.12f, 0f), new Vector3(3.9f, 0.24f, 3.7f));
        b.Push(new Vector3(0f, 0.24f, 0f), Euler(0f, 0f, 2.5f), Vector3.one);
        b.AddBox(StylizedColor.BarkDark, new Vector3(0f, 1.2f, 0f), new Vector3(3.6f, 2.4f, 3.4f));

        foreach (float x in new[] { -1.75f, 1.75f })
        {
            b.AddBox(StylizedColor.WoodDark, new Vector3(x, 1.2f, 1.72f), new Vector3(0.14f, 2.4f, 0.1f));
        }

        b.AddBox(StylizedColor.WoodDark, new Vector3(0.5f, 0.95f, 1.72f), new Vector3(0.9f, 1.7f, 0.06f));
        b.Push(new Vector3(-0.9f, 1.6f, 1.72f), Euler(90f, 0f, 0f), Vector3.one);
        b.AddCylinder(StylizedColor.WoodDark, Vector3.zero, 0.36f, 0.06f, 10);
        b.AddDisc(StylizedColor.LampGlow, new Vector3(0f, 0.065f, 0f), 0.3f, 10);
        b.Pop();
        b.Push(new Vector3(0f, 2.4f, 0f), Euler(0f, 0f, -6f), Vector3.one);
        b.AddFrustum(StylizedColor.Chitin, Vector3.zero, 2.9f, 0.55f, 1.8f, 8);
        b.Push(new Vector3(0f, 1.8f, 0f), Euler(0f, 0f, -22f), Vector3.one);
        b.AddFrustum(StylizedColor.Chitin, Vector3.zero, 0.55f, 0.18f, 1.0f, 8);
        b.Push(new Vector3(0f, 1.0f, 0f), Euler(0f, 0f, -30f), Vector3.one);
        b.AddCone(StylizedColor.Chitin, Vector3.zero, 0.18f, 0.6f, 6);
        b.Pop();
        b.Pop();
        b.Pop();

        // 굴뚝과 그 위의 까마귀
        b.AddBox(StylizedColor.StoneDark, new Vector3(-1.2f, 3.6f, -0.9f), new Vector3(0.45f, 1.4f, 0.45f));
        b.AddLowPolySphere(StylizedColor.Black, new Vector3(-1.2f, 4.42f, -0.9f), new Vector3(0.12f, 0.12f, 0.2f), 0, 0f, 1370);
        b.AddLowPolySphere(StylizedColor.Black, new Vector3(-1.18f, 4.57f, -0.72f), Vector3.one * 0.08f, 0, 0f, 1371);
        b.Push(new Vector3(-1.18f, 4.57f, -0.66f), Euler(90f, 0f, 0f), Vector3.one);
        b.AddCone(StylizedColor.FlowerYellow, Vector3.zero, 0.025f, 0.08f, 4);
        b.Pop();
        b.Pop();

        // 가마솥 · 불 · 빗자루
        b.AddLowPolySphere(StylizedColor.IronDark, new Vector3(1.9f, 0.45f, 2.3f), new Vector3(0.5f, 0.4f, 0.5f), 1, 0f, 1372);
        b.AddDisc(StylizedColor.SpitterSac, new Vector3(1.9f, 0.78f, 2.3f), 0.4f, 10);
        b.AddTorus(StylizedColor.IronDark, new Vector3(1.9f, 0.8f, 2.3f), 0.42f, 0.04f, 10, 3);
        b.AddLowPolySphere(StylizedColor.Fire, new Vector3(1.9f, 0.08f, 2.3f), new Vector3(0.3f, 0.1f, 0.3f), 0, 0f, 1373);
        b.AddLimb(StylizedColor.WoodDark, new Vector3(-1.9f, 0f, 1.95f), new Vector3(-1.8f, 1.6f, 1.85f), 0.025f, 0.025f, 5);
        b.AddFrustum(StylizedColor.Straw, new Vector3(-1.9f, 0f, 1.95f), 0.2f, 0.05f, 0.45f, 6);
    }

    // 세계수 그루터기 (반지름 3m · 뿌리 · 이끼 · 안쪽 빛)
    private static void BuildGreatStump(LowPolyMeshBuilder b)
    {
        b.AddFrustum(StylizedColor.Bark, Vector3.zero, 3.0f, 2.5f, 2.6f, 12, false, false);
        b.AddDisc(StylizedColor.WoodLight, new Vector3(0f, 2.6f, 0f), 2.5f, 12);

        for (int ring = 1; ring <= 3; ring++)
        {
            b.AddTorus(StylizedColor.WoodPlank, new Vector3(0f, 2.6f, 0f), ring * 0.6f, 0.03f, 16, 3);
        }

        for (int index = 0; index < 8; index++)
        {
            float angle = index * Mathf.PI * 0.25f + 0.2f;
            Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            AddTube(b, StylizedColor.BarkDark, new[] { dir * 2.5f + Vector3.up * 0.9f, dir * 3.4f + Vector3.up * 0.35f, dir * 4.2f + Vector3.up * 0.05f }, new[] { 0.45f, 0.3f, 0.12f }, 6, 1380 + index * 3);
        }

        b.AddBox(StylizedColor.Black, new Vector3(0f, 0.8f, 2.65f), new Vector3(1.1f, 1.6f, 0.3f));
        b.AddLowPolySphere(StylizedColor.Crystal, new Vector3(0f, 0.6f, 2.55f), Vector3.one * 0.3f, 1, 0.1f, 1410);
        AddMoss(b, new Vector3(-1.2f, 2.62f, 0.8f), new Vector3(1.0f, 0.12f, 0.8f), 1411);
        AddMoss(b, new Vector3(1.8f, 1.2f, -1.6f), new Vector3(0.6f, 0.5f, 0.4f), 1412);
        AddMushroom(b, new Vector3(2.4f, 1.6f, 1.2f), 0.7f, -30f);
        AddMushroom(b, new Vector3(-2.6f, 0.9f, -0.8f), 0.6f, 25f);
    }

    // ---------------------------------------------------------------- 설산 기슭 · 사당

    // 눈 덮인 전나무 (6.5m)
    private static void BuildSnowPine(LowPolyMeshBuilder b)
    {
        b.AddFrustum(StylizedColor.BarkDark, Vector3.zero, 0.28f, 0.18f, 1.4f, 6);
        float[] heights = { 1.0f, 2.3f, 3.5f, 4.6f };
        float[] radii = { 2.2f, 1.75f, 1.3f, 0.85f };

        for (int index = 0; index < heights.Length; index++)
        {
            b.AddFrustum(index % 2 == 0 ? StylizedColor.LeafDark : StylizedColor.Leaf, new Vector3(0f, heights[index], 0f), radii[index], 0f, 1.8f, 7, true, false, index * 23f);
            b.AddFrustum(StylizedColor.Snow, new Vector3(0f, heights[index] + 0.95f, 0f), radii[index] * 0.52f, 0f, 0.86f, 7, true, false, index * 23f);
        }
    }

    // 눈 · 얼음 바위
    private static void BuildIceRock(LowPolyMeshBuilder b)
    {
        b.AddLowPolySphere(StylizedColor.Stone, new Vector3(0f, 0.7f, 0f), new Vector3(1.4f, 1.0f, 1.2f), 1, 0.2f, 1420);
        b.AddLowPolySphere(StylizedColor.Snow, new Vector3(0f, 1.45f, 0f), new Vector3(1.1f, 0.35f, 0.95f), 1, 0.15f, 1421);

        for (int index = 0; index < 3; index++)
        {
            float angle = index * 2.1f;
            b.Push(new Vector3(Mathf.Cos(angle) * 1.1f, 0.2f, Mathf.Sin(angle) * 0.9f), Euler(Mathf.Sin(angle) * 20f, 0f, -Mathf.Cos(angle) * 20f), Vector3.one);
            b.AddCylinder(StylizedColor.Ice, Vector3.zero, 0.14f, 0.6f, 5);
            b.AddCone(StylizedColor.Ice, new Vector3(0f, 0.6f, 0f), 0.14f, 0.35f, 5);
            b.Pop();
        }
    }

    // 털가죽 천막 (원뿔형 · 흰 털 테두리 · 기둥 끝)
    private static void BuildSnowTent(LowPolyMeshBuilder b)
    {
        b.AddFrustum(StylizedColor.Leather, Vector3.zero, 1.6f, 0.14f, 2.6f, 8);
        b.AddTorus(StylizedColor.White, new Vector3(0f, 0.08f, 0f), 1.58f, 0.1f, 12, 4);
        b.AddFrustum(StylizedColor.Snow, new Vector3(0f, 1.9f, 0f), 0.5f, 0.1f, 0.6f, 8);

        for (int index = 0; index < 3; index++)
        {
            float angle = index * 2.1f;
            b.AddLimb(StylizedColor.WoodDark, new Vector3(Mathf.Cos(angle) * 0.15f, 2.3f, Mathf.Sin(angle) * 0.15f), new Vector3(Mathf.Cos(angle) * 0.35f, 3.0f, Mathf.Sin(angle) * 0.35f), 0.04f, 0.03f, 5);
        }

        b.Push(new Vector3(0f, 0.7f, 1.18f), Euler(-30f, 0f, 0f), Vector3.one);
        b.AddBox(StylizedColor.Black, Vector3.zero, new Vector3(0.8f, 1.3f, 0.04f));
        b.Pop();
    }

    // 가죽 건조대 (A자 틀 · 털가죽 3장)
    private static void BuildFurRack(LowPolyMeshBuilder b)
    {
        for (int side = -1; side <= 1; side += 2)
        {
            b.AddLimb(StylizedColor.WoodDark, new Vector3(side * 1.2f, 0f, -0.5f), new Vector3(side * 1.2f, 2f, 0f), 0.05f, 0.04f, 5);
            b.AddLimb(StylizedColor.WoodDark, new Vector3(side * 1.2f, 0f, 0.5f), new Vector3(side * 1.2f, 2f, 0f), 0.05f, 0.04f, 5);
        }

        b.AddLimb(StylizedColor.WoodDark, new Vector3(-1.3f, 1.95f, 0f), new Vector3(1.3f, 1.95f, 0f), 0.04f, 0.04f, 5);
        StylizedColor[] furs = { StylizedColor.White, StylizedColor.Leather, StylizedColor.StoneLight };

        for (int index = 0; index < furs.Length; index++)
        {
            b.AddBeveledBox(furs[index], new Vector3(-0.75f + index * 0.75f, 1.35f, 0.04f), new Vector3(0.62f, 1.1f, 0.05f), 0.02f);
        }
    }

    // 눈 더미
    private static void BuildSnowPile(LowPolyMeshBuilder b)
    {
        b.AddLowPolySphere(StylizedColor.Snow, new Vector3(0f, 0f, 0f), new Vector3(1.4f, 0.5f, 1.1f), 1, 0.12f, 1430);
        b.AddLowPolySphere(StylizedColor.Snow, new Vector3(1.0f, 0f, 0.6f), new Vector3(0.7f, 0.3f, 0.6f), 1, 0.12f, 1431);
        b.AddLowPolySphere(StylizedColor.Snow, new Vector3(-0.9f, 0f, -0.5f), new Vector3(0.6f, 0.25f, 0.55f), 1, 0.12f, 1432);
    }

    // 붉은 문 (도리이, 높이 5m · 폭 5.4m, Z 방향으로 지나감)
    private static void BuildTorii(LowPolyMeshBuilder b)
    {
        for (int side = -1; side <= 1; side += 2)
        {
            b.AddCylinder(StylizedColor.Black, new Vector3(side * 1.9f, 0f, 0f), 0.28f, 0.4f, 8);
            b.AddFrustum(StylizedColor.ClothRed, new Vector3(side * 1.9f, 0.4f, 0f), 0.22f, 0.2f, 4.1f, 8);
            b.Push(new Vector3(side * 2.55f, 4.72f, 0f), Euler(0f, 0f, side * 10f), Vector3.one);
            b.AddBox(StylizedColor.Black, Vector3.zero, new Vector3(0.7f, 0.3f, 0.5f));
            b.Pop();
        }

        b.AddBox(StylizedColor.ClothRed, new Vector3(0f, 3.5f, 0f), new Vector3(4.6f, 0.26f, 0.28f));
        b.AddBox(StylizedColor.ClothRed, new Vector3(0f, 4.32f, 0f), new Vector3(4.9f, 0.24f, 0.38f));
        b.AddBox(StylizedColor.Black, new Vector3(0f, 4.6f, 0f), new Vector3(4.6f, 0.3f, 0.48f));
        b.AddBox(StylizedColor.Black, new Vector3(0f, 3.9f, 0.05f), new Vector3(0.5f, 0.6f, 0.08f));
        b.AddBox(StylizedColor.Gold, new Vector3(0f, 3.9f, 0.1f), new Vector3(0.36f, 0.44f, 0.02f));
    }

    // 석등 (1.8m)
    private static void BuildStoneLantern(LowPolyMeshBuilder b)
    {
        b.AddBeveledBox(StylizedColor.StoneLight, new Vector3(0f, 0.1f, 0f), new Vector3(0.6f, 0.2f, 0.6f), 0.03f);
        b.AddCylinder(StylizedColor.StoneLight, new Vector3(0f, 0.2f, 0f), 0.12f, 0.7f, 6);
        b.AddBeveledBox(StylizedColor.StoneLight, new Vector3(0f, 0.96f, 0f), new Vector3(0.62f, 0.12f, 0.62f), 0.03f);

        foreach (Vector2 corner in new[] { new Vector2(-0.2f, -0.2f), new Vector2(0.2f, -0.2f), new Vector2(-0.2f, 0.2f), new Vector2(0.2f, 0.2f) })
        {
            b.AddBox(StylizedColor.StoneLight, new Vector3(corner.x, 1.2f, corner.y), new Vector3(0.08f, 0.36f, 0.08f));
        }

        b.AddBox(StylizedColor.LampGlow, new Vector3(0f, 1.2f, 0f), new Vector3(0.28f, 0.28f, 0.28f));
        b.AddFrustum(StylizedColor.StoneDark, new Vector3(0f, 1.38f, 0f), 0.52f, 0.06f, 0.36f, 4, true, false, 45f);
        b.AddLowPolySphere(StylizedColor.StoneDark, new Vector3(0f, 1.8f, 0f), Vector3.one * 0.07f, 0, 0f, 1440);
    }

    // 작은 사당 (호코라, 2.4m · 금줄과 흰 종이 · 공물)
    private static void BuildHokora(LowPolyMeshBuilder b)
    {
        b.AddBeveledBox(StylizedColor.StoneDark, new Vector3(0f, 0.2f, 0f), new Vector3(1.7f, 0.4f, 1.5f), 0.04f);
        b.AddBox(StylizedColor.WoodDark, new Vector3(0f, 0.95f, 0f), new Vector3(1.1f, 1.1f, 0.9f));
        b.AddBox(StylizedColor.WoodLight, new Vector3(0f, 0.9f, 0.46f), new Vector3(0.8f, 0.8f, 0.03f));
        b.AddBox(StylizedColor.WoodDark, new Vector3(0f, 0.9f, 0.48f), new Vector3(0.03f, 0.8f, 0.02f));
        b.AddWedge(StylizedColor.Black, new Vector3(0f, 1.78f, 0f), new Vector3(1.7f, 0.56f, 1.4f));
        b.AddBox(StylizedColor.Gold, new Vector3(0f, 2.07f, 0f), new Vector3(1.72f, 0.05f, 0.08f));
        b.AddLimb(StylizedColor.Rope, new Vector3(-0.62f, 1.45f, 0.5f), new Vector3(0.62f, 1.45f, 0.5f), 0.04f, 0.04f, 6);

        for (int index = 0; index < 4; index++)
        {
            float x = -0.45f + index * 0.3f;
            b.Push(new Vector3(x, 1.3f, 0.53f), Euler(0f, 0f, index % 2 == 0 ? 12f : -12f), Vector3.one);
            b.AddBox(StylizedColor.White, Vector3.zero, new Vector3(0.06f, 0.22f, 0.01f));
            b.Pop();
        }

        b.AddBox(StylizedColor.WoodLight, new Vector3(0f, 0.45f, 0.62f), new Vector3(0.6f, 0.1f, 0.25f));
        b.AddCylinder(StylizedColor.White, new Vector3(-0.2f, 0.5f, 0.62f), 0.05f, 0.18f, 6);
        b.AddCylinder(StylizedColor.White, new Vector3(0.2f, 0.5f, 0.62f), 0.05f, 0.18f, 6);
        b.AddLowPolySphere(StylizedColor.AppleRed, new Vector3(0f, 0.55f, 0.64f), Vector3.one * 0.06f, 0, 0f, 1450);
    }

    // 101일차: 사당 옆 무녀의 집 (4 × 3.4m, 돌 기단 · 툇마루 · 붉은 기둥 · 장지문 · 눈 덮인 검은 지붕 · 금줄 · 등불)
    private static void BuildShrineHouse(LowPolyMeshBuilder b)
    {
        b.AddBox(StylizedColor.StoneDark, new Vector3(0f, 0.18f, 0f), new Vector3(4.2f, 0.36f, 3.6f));
        b.AddBox(StylizedColor.WoodPlank, new Vector3(0f, 0.42f, 1.95f), new Vector3(4.2f, 0.12f, 0.7f));
        b.AddBox(StylizedColor.StoneLight, new Vector3(0f, 0.12f, 2.55f), new Vector3(1.1f, 0.24f, 0.5f));
        b.AddBox(StylizedColor.WoodLight, new Vector3(0f, 1.4f, 0f), new Vector3(3.8f, 2.0f, 3.2f));

        foreach (float x in new[] { -1.95f, 1.95f })
        {
            foreach (float z in new[] { -1.65f, 1.65f, 2.25f })
            {
                b.AddCylinder(StylizedColor.ClothRed, new Vector3(x, 0.36f, z), 0.1f, 2.1f, 6);
            }
        }

        // 장지문 두 짝 (흰 종이 · 나무 격자)
        for (int side = -1; side <= 1; side += 2)
        {
            b.AddBox(StylizedColor.White, new Vector3(side * 0.45f, 1.3f, 1.61f), new Vector3(0.85f, 1.6f, 0.03f));

            for (int bar = 0; bar < 3; bar++)
            {
                b.AddBox(StylizedColor.WoodDark, new Vector3(side * 0.45f, 0.75f + bar * 0.55f, 1.63f), new Vector3(0.87f, 0.03f, 0.02f));
            }

            b.AddBox(StylizedColor.WoodDark, new Vector3(side * 0.45f, 1.3f, 1.63f), new Vector3(0.03f, 1.6f, 0.02f));
        }

        // 지붕 · 눈 · 처마 끝
        b.AddWedge(StylizedColor.Black, new Vector3(0f, 3.0f, 0.2f), new Vector3(4.8f, 1.2f, 4.6f));
        b.AddWedge(StylizedColor.Snow, new Vector3(0f, 3.28f, 0.2f), new Vector3(4.5f, 0.7f, 3.6f));

        for (int side = -1; side <= 1; side += 2)
        {
            b.Push(new Vector3(side * 2.45f, 2.45f, 2.45f), Euler(0f, 0f, side * 12f), Vector3.one);
            b.AddBox(StylizedColor.Black, Vector3.zero, new Vector3(0.3f, 0.12f, 0.3f));
            b.Pop();
            b.AddLimb(StylizedColor.IronDark, new Vector3(side * 1.6f, 2.45f, 2.3f), new Vector3(side * 1.6f, 2.15f, 2.3f), 0.01f, 0.01f, 3);
            b.AddCylinder(StylizedColor.ClothRed, new Vector3(side * 1.6f, 1.8f, 2.3f), 0.13f, 0.35f, 8);
            b.AddLowPolySphere(StylizedColor.LampGlow, new Vector3(side * 1.6f, 1.97f, 2.3f), Vector3.one * 0.1f, 0, 0f, 1455 + side);
        }

        // 금줄 · 흰 종이
        b.AddLimb(StylizedColor.Rope, new Vector3(-1.3f, 2.3f, 1.7f), new Vector3(1.3f, 2.3f, 1.7f), 0.05f, 0.05f, 6);

        for (int index = 0; index < 5; index++)
        {
            b.Push(new Vector3(-1f + index * 0.5f, 2.1f, 1.74f), Euler(0f, 0f, index % 2 == 0 ? 10f : -10f), Vector3.one);
            b.AddBox(StylizedColor.White, Vector3.zero, new Vector3(0.07f, 0.28f, 0.01f));
            b.Pop();
        }
    }

    // ---------------------------------------------------------------- 사막

    // 선인장 (3m, 팔 두 개 · 꽃)
    private static void BuildCactus(LowPolyMeshBuilder b)
    {
        b.AddCylinder(StylizedColor.Leaf, Vector3.zero, 0.28f, 2.7f, 8);
        b.AddLowPolySphere(StylizedColor.Leaf, new Vector3(0f, 2.7f, 0f), new Vector3(0.28f, 0.2f, 0.28f), 1, 0f, 1460);
        AddTube(b, StylizedColor.Leaf, new[] { new Vector3(0.2f, 1.3f, 0f), new Vector3(0.66f, 1.35f, 0f), new Vector3(0.68f, 2.1f, 0f) }, new[] { 0.17f, 0.17f, 0.16f }, 8, 1461);
        b.AddLowPolySphere(StylizedColor.Leaf, new Vector3(0.68f, 2.1f, 0f), new Vector3(0.16f, 0.12f, 0.16f), 0, 0f, 1464);
        AddTube(b, StylizedColor.Leaf, new[] { new Vector3(-0.2f, 0.9f, 0.05f), new Vector3(-0.58f, 0.95f, 0.08f), new Vector3(-0.6f, 1.5f, 0.08f) }, new[] { 0.15f, 0.15f, 0.14f }, 8, 1465);
        b.AddLowPolySphere(StylizedColor.Leaf, new Vector3(-0.6f, 1.5f, 0.08f), new Vector3(0.14f, 0.1f, 0.14f), 0, 0f, 1468);
        AddSmallFlower(b, new Vector3(0f, 2.9f, 0f), 0.07f, StylizedColor.Flower, 1470);
    }

    // 붉은 사암 바위 (4.6m, 평평한 꼭대기 · 층 무늬 · 굴러떨어진 바위)
    private static void BuildMesaRock(LowPolyMeshBuilder b)
    {
        b.AddFrustum(StylizedColor.Sandstone, Vector3.zero, 2.4f, 2.1f, 1.7f, 7, false, false, 8f);
        b.AddFrustum(StylizedColor.Sand, new Vector3(0f, 1.7f, 0f), 2.1f, 2.02f, 0.28f, 7, false, false, 8f);
        b.AddFrustum(StylizedColor.Sandstone, new Vector3(0f, 1.98f, 0f), 2.02f, 1.75f, 1.5f, 7, false, false, 20f);
        b.AddFrustum(StylizedColor.AppleBaked, new Vector3(0f, 3.48f, 0f), 1.75f, 1.62f, 0.3f, 7, false, false, 20f);
        b.AddFrustum(StylizedColor.Sandstone, new Vector3(0f, 3.78f, 0f), 1.62f, 1.1f, 0.8f, 7, false, true, 34f);
        b.AddLowPolySphere(StylizedColor.Sandstone, new Vector3(2.5f, 0.35f, 1.0f), new Vector3(0.7f, 0.45f, 0.6f), 1, 0.2f, 1484);
        b.AddLowPolySphere(StylizedColor.AppleBaked, new Vector3(-2.2f, 0.25f, -1.3f), new Vector3(0.45f, 0.3f, 0.4f), 1, 0.2f, 1485);
    }

    // 야자수 (6.5m, 휘어진 줄기 · 잎 7장 · 코코넛)
    private static void BuildPalm(LowPolyMeshBuilder b)
    {
        Vector3[] trunk = { Vector3.zero, new Vector3(0.15f, 1.5f, 0f), new Vector3(0.45f, 3f, 0f), new Vector3(0.9f, 4.4f, 0f), new Vector3(1.4f, 5.6f, 0f) };
        float[] radii = { 0.3f, 0.26f, 0.23f, 0.2f, 0.17f };

        for (int index = 0; index < trunk.Length - 1; index++)
        {
            b.AddLimb(StylizedColor.Bark, trunk[index], trunk[index + 1], radii[index], radii[index + 1], 6);
            b.AddTorus(StylizedColor.BarkDark, trunk[index + 1], radii[index + 1] + 0.01f, 0.035f, 6, 3);
        }

        Vector3 crown = trunk[trunk.Length - 1];

        for (int index = 0; index < 7; index++)
        {
            AddLeaf(b, index % 2 == 0 ? StylizedColor.Leaf : StylizedColor.LeafLight, crown, index * 360f / 7f, -28f, 2.6f, 0.38f, 1490 + index);
        }

        for (int index = 0; index < 3; index++)
        {
            float angle = index * 2.1f;
            b.AddLowPolySphere(StylizedColor.WoodDark, crown + new Vector3(Mathf.Cos(angle) * 0.2f, -0.25f, Mathf.Sin(angle) * 0.2f), Vector3.one * 0.14f, 0, 0f, 1500 + index);
        }
    }

    // 사막 상인 천막 (4 × 4m, 줄무늬 지붕 · 양탄자 · 항아리 · 방석)
    private static void BuildDesertTent(LowPolyMeshBuilder b)
    {
        foreach (float x in new[] { -1.9f, 1.9f })
        {
            foreach (float z in new[] { -1.9f, 1.9f })
            {
                b.AddLimb(StylizedColor.WoodDark, new Vector3(x, 0f, z), new Vector3(x, 2.4f, z), 0.06f, 0.05f, 6);
            }
        }

        b.AddWedge(StylizedColor.ClothRed, new Vector3(0f, 2.9f, 0f), new Vector3(4.3f, 1.0f, 4.3f));

        for (int index = -1; index <= 1; index++)
        {
            b.AddBox(StylizedColor.ClothCream, new Vector3(index * 1.4f, 2.42f, 2.12f), new Vector3(0.7f, 0.3f, 0.04f));
        }

        b.AddBox(StylizedColor.ClothCream, new Vector3(0f, 1.2f, -1.95f), new Vector3(3.9f, 2.4f, 0.05f));
        b.AddBox(StylizedColor.ClothRed, new Vector3(0f, 0.01f, 0.3f), new Vector3(2.6f, 0.02f, 1.8f));
        b.AddBox(StylizedColor.FlowerYellow, new Vector3(0f, 0.015f, 0.3f), new Vector3(2.2f, 0.02f, 1.4f));
        b.AddBox(StylizedColor.ClothBlue, new Vector3(0f, 0.02f, 0.3f), new Vector3(1.8f, 0.02f, 1.0f));

        foreach (Vector3 pot in new[] { new Vector3(-1.5f, 0f, -1.4f), new Vector3(-1.1f, 0f, -1.5f), new Vector3(1.5f, 0f, -1.3f) })
        {
            b.AddLowPolySphere(StylizedColor.Sandstone, pot + Vector3.up * 0.3f, new Vector3(0.26f, 0.3f, 0.26f), 1, 0f, 1510);
            b.AddCylinder(StylizedColor.Sandstone, pot + Vector3.up * 0.55f, 0.12f, 0.15f, 6);
        }

        b.AddBeveledBox(StylizedColor.BerryPurple, new Vector3(0.9f, 0.12f, -0.6f), new Vector3(0.6f, 0.2f, 0.6f), 0.08f);
        b.AddBeveledBox(StylizedColor.Copper, new Vector3(-0.8f, 0.12f, -0.7f), new Vector3(0.6f, 0.2f, 0.6f), 0.08f);
    }

    // 짐승 뼈 (두개골 · 반쯤 묻힌 갈비뼈)
    private static void BuildBones(LowPolyMeshBuilder b)
    {
        b.AddLowPolySphere(StylizedColor.Bone, new Vector3(0f, 0.22f, 0.9f), new Vector3(0.3f, 0.24f, 0.4f), 1, 0.05f, 1520);
        b.AddBox(StylizedColor.Black, new Vector3(0.12f, 0.28f, 1.22f), new Vector3(0.08f, 0.06f, 0.05f));
        b.AddBox(StylizedColor.Black, new Vector3(-0.12f, 0.28f, 1.22f), new Vector3(0.08f, 0.06f, 0.05f));

        for (int side = -1; side <= 1; side += 2)
        {
            b.Push(new Vector3(side * 0.25f, 0.35f, 0.8f), Euler(0f, 0f, -side * 60f), Vector3.one);
            b.AddCone(StylizedColor.Bone, Vector3.zero, 0.06f, 0.5f, 5);
            b.Pop();
        }

        b.AddLimb(StylizedColor.Bone, new Vector3(0f, 0.12f, 0.5f), new Vector3(0f, 0.08f, -1.3f), 0.05f, 0.04f, 5);

        for (int index = 0; index < 5; index++)
        {
            float z = 0.2f - index * 0.32f;

            for (int side = -1; side <= 1; side += 2)
            {
                AddTube(b, StylizedColor.Bone, new[] { new Vector3(0f, 0.12f, z), new Vector3(side * 0.35f, 0.45f, z), new Vector3(side * 0.55f, 0.15f, z) }, new[] { 0.035f, 0.03f, 0.02f }, 4, 1530 + index * 4 + side);
            }
        }
    }

    // 오아시스 샘 (반지름 4.6m, 모래 테두리 · 바위 · 갈대)
    private static void BuildOasisPool(LowPolyMeshBuilder b)
    {
        float radius = ZoneOasisRadius;
        b.AddFrustum(StylizedColor.Sand, Vector3.zero, radius + 0.7f, radius, 0.12f, 20, false, false);
        b.AddDisc(StylizedColor.PondDeep, new Vector3(0f, 0.02f, 0f), radius, 20);
        b.AddDisc(StylizedColor.Water, new Vector3(0f, 0.09f, 0f), radius + 0.05f, 20);

        for (int index = 0; index < 6; index++)
        {
            float angle = index * 1.05f + 0.3f;
            b.AddLowPolySphere(StylizedColor.Sandstone, new Vector3(Mathf.Cos(angle) * (radius + 0.5f), 0.15f, Mathf.Sin(angle) * (radius + 0.5f)), new Vector3(0.45f, 0.3f, 0.4f), 1, 0.2f, 1560 + index);
        }

        for (int index = 0; index < 10; index++)
        {
            float angle = index * 0.63f + 2.6f;
            Vector3 root = new Vector3(Mathf.Cos(angle) * (radius - 0.2f), 0f, Mathf.Sin(angle) * (radius - 0.2f));
            b.AddLimb(StylizedColor.Leaf, root, root + new Vector3(Mathf.Cos(angle) * 0.15f, 1.0f + (index % 3) * 0.25f, 0f), 0.025f, 0.01f, 4);
        }
    }

    // 모래 언덕 (10 × 6m)
    private static void BuildDune(LowPolyMeshBuilder b)
    {
        b.AddLowPolySphere(StylizedColor.Sand, new Vector3(0f, -0.35f, 0f), new Vector3(6f, 0.95f, 3.4f), 2, 0.05f, 1570);
    }

    // ---------------------------------------------------------------- 습지

    // 탁한 습지 웅덩이 (반지름 5m, 진흙 테두리 · 연잎 · 연꽃)
    private static void BuildSwampPool(LowPolyMeshBuilder b)
    {
        float radius = ZoneSwampPoolRadius;
        b.AddFrustum(StylizedColor.SwampMud, Vector3.zero, radius + 0.6f, radius, 0.1f, 18, false, false);
        b.AddDisc(StylizedColor.SwampMud, new Vector3(0f, 0.01f, 0f), radius, 18);
        b.AddDisc(StylizedColor.SwampWater, new Vector3(0f, 0.07f, 0f), radius + 0.05f, 18);
        System.Random random = new System.Random(1580);

        for (int index = 0; index < 9; index++)
        {
            float angle = Rand(random, 0f, Mathf.PI * 2f);
            float distance = Rand(random, 1.2f, radius - 0.6f);
            Vector3 center = new Vector3(Mathf.Cos(angle) * distance, 0.085f, Mathf.Sin(angle) * distance);
            b.AddDisc(StylizedColor.Leaf, center, Rand(random, 0.3f, 0.5f), 7, true);

            if (index % 3 == 0)
            {
                AddSmallFlower(b, center + Vector3.up * 0.05f, 0.08f, StylizedColor.Flower, 1590 + index * 6);
            }
        }
    }

    // 죽은 나무 (5m, 비틀린 가지 · 늘어진 이끼)
    private static void BuildDeadTree(LowPolyMeshBuilder b)
    {
        b.AddLimb(StylizedColor.BarkDark, Vector3.zero, new Vector3(0.2f, 1.8f, 0.1f), 0.38f, 0.26f, 7);
        b.AddLimb(StylizedColor.BarkDark, new Vector3(0.2f, 1.8f, 0.1f), new Vector3(-0.1f, 3.4f, 0f), 0.26f, 0.14f, 7);
        Vector3[] starts = { new Vector3(0.2f, 1.9f, 0.1f), new Vector3(0.05f, 2.6f, 0.05f), new Vector3(-0.05f, 3.1f, 0f), new Vector3(0.15f, 2.3f, 0.1f), new Vector3(-0.08f, 3.35f, 0f) };
        Vector3[] ends = { new Vector3(1.5f, 2.8f, 0.4f), new Vector3(-1.4f, 3.5f, 0.5f), new Vector3(0.9f, 4.4f, -0.6f), new Vector3(-0.6f, 3.0f, -1.3f), new Vector3(-0.4f, 4.8f, 0.3f) };

        for (int index = 0; index < starts.Length; index++)
        {
            Vector3 middle = Vector3.Lerp(starts[index], ends[index], 0.5f) + new Vector3(0f, 0.25f, 0f);
            AddTube(b, StylizedColor.BarkDark, new[] { starts[index], middle, ends[index] }, new[] { 0.1f, 0.06f, 0.015f }, 5, 1600 + index * 3);

            if (index < 4)
            {
                b.AddLimb(StylizedColor.LeafDark, middle, middle + new Vector3(0.02f, -0.9f - index * 0.15f, 0f), 0.04f, 0.01f, 4);
            }
        }

        for (int root = 0; root < 4; root++)
        {
            float angle = root * Mathf.PI * 0.5f + 0.4f;
            b.AddLimb(StylizedColor.BarkDark, new Vector3(Mathf.Cos(angle) * 0.2f, 0.35f, Mathf.Sin(angle) * 0.2f), new Vector3(Mathf.Cos(angle) * 0.9f, 0f, Mathf.Sin(angle) * 0.9f), 0.14f, 0.05f, 5);
        }
    }

    // 습지 나무길 (길이 10m · 폭 1.6m, 양 끝 경사로)
    private static void BuildBoardwalk(LowPolyMeshBuilder b)
    {
        float deck = ZoneBoardwalkDeckHeight;
        float half = ZoneBoardwalkWidth * 0.5f;

        for (int end = 0; end < 2; end++)
        {
            float z = end == 0 ? 0.5f : ZoneBoardwalkLength - 0.5f;
            b.Push(new Vector3(0f, deck * 0.5f, z), Euler(end == 0 ? -14f : 14f, 0f, 0f), Vector3.one);
            b.AddBox(StylizedColor.WoodPlank, Vector3.zero, new Vector3(ZoneBoardwalkWidth - 0.2f, 0.05f, 1.05f));
            b.Pop();
        }

        for (int index = 0; index < 17; index++)
        {
            float z = 1f + index * 0.5f + 0.25f;
            b.AddBox(index % 4 == 1 ? StylizedColor.WoodLight : StylizedColor.WoodPlank, new Vector3(0f, deck - 0.03f, z), new Vector3(ZoneBoardwalkWidth, 0.06f, 0.44f));
        }

        for (int side = -1; side <= 1; side += 2)
        {
            for (int post = 0; post < 5; post++)
            {
                b.AddCylinder(StylizedColor.WoodDark, new Vector3(side * (half - 0.05f), -0.6f, 1.2f + post * 2f), 0.07f, 1.1f, 5);
            }
        }
    }

    // 기둥 위 관측 오두막 (바닥 높이 1.3m, 사다리 · 초가 지붕 · 등불 · 풍향계)
    private static void BuildStiltHut(LowPolyMeshBuilder b)
    {
        foreach (float x in new[] { -1.6f, 1.6f })
        {
            foreach (float z in new[] { -1.6f, 1.6f })
            {
                b.AddCylinder(StylizedColor.WoodDark, new Vector3(x, -0.3f, z), 0.12f, 1.6f, 6);
            }
        }

        b.AddBox(StylizedColor.WoodPlank, new Vector3(0f, 1.3f, 0f), new Vector3(3.8f, 0.15f, 3.8f));
        b.AddBox(StylizedColor.WoodPlank, new Vector3(0f, 2.37f, -0.2f), new Vector3(3.0f, 2.0f, 2.8f));
        b.AddBox(StylizedColor.WoodDark, new Vector3(0.6f, 2.2f, 1.21f), new Vector3(0.8f, 1.6f, 0.06f));
        b.AddBox(StylizedColor.Glass, new Vector3(-0.8f, 2.5f, 1.21f), new Vector3(0.6f, 0.5f, 0.04f));
        b.AddFrustum(StylizedColor.Straw, new Vector3(0f, 3.35f, -0.2f), 2.5f, 0.05f, 1.4f, 4, true, false, 45f);

        for (int side = -1; side <= 1; side += 2)
        {
            b.AddLimb(StylizedColor.WoodDark, new Vector3(side * 0.3f, 0f, 2.6f), new Vector3(side * 0.3f, 1.4f, 1.9f), 0.035f, 0.035f, 5);
        }

        for (int rung = 0; rung < 5; rung++)
        {
            float t = (rung + 0.5f) / 5f;
            b.AddLimb(StylizedColor.WoodLight, new Vector3(-0.3f, t * 1.4f, 2.6f - t * 0.7f), new Vector3(0.3f, t * 1.4f, 2.6f - t * 0.7f), 0.025f, 0.025f, 4);
        }

        b.AddLimb(StylizedColor.IronDark, new Vector3(1.2f, 3.0f, 1.25f), new Vector3(1.2f, 2.6f, 1.25f), 0.01f, 0.01f, 3);
        b.AddLowPolySphere(StylizedColor.LampGlow, new Vector3(1.2f, 2.52f, 1.25f), Vector3.one * 0.09f, 0, 0f, 1620);
        b.AddLimb(StylizedColor.IronDark, new Vector3(0f, 4.7f, -0.2f), new Vector3(0f, 5.3f, -0.2f), 0.02f, 0.02f, 4);
        b.AddBox(StylizedColor.IronDark, new Vector3(0.1f, 5.2f, -0.2f), new Vector3(0.4f, 0.03f, 0.03f));
        b.AddCone(StylizedColor.IronDark, new Vector3(0.3f, 5.2f, -0.2f), 0.06f, 0.1f, 3);
        b.Push(new Vector3(-1.52f, 2.5f, -0.2f), Euler(0f, 0f, 90f), Vector3.one);
        b.AddCylinder(StylizedColor.Gold, Vector3.zero, 0.12f, 0.03f, 8);
        b.Pop();
    }
}
