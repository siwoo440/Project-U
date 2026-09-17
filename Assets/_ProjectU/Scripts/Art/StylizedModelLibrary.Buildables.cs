using UnityEngine;

// 건축·가구 모델은 X,Z -0.5~0.5 / Y 0~1 상자 기준으로 만든다.
public static partial class StylizedModelLibrary
{
    private static void RegisterBuildables()
    {
        Register("build_wood_floor", FitMode.Stretch, BuildWoodFloor);
        Register("build_stone_floor", FitMode.Stretch, BuildStoneFloor);
        Register("build_wood_foundation", FitMode.Stretch, BuildWoodFoundation);
        Register("build_stone_foundation", FitMode.Stretch, BuildStoneFoundation);
        Register("build_wood_wall", FitMode.Stretch, BuildWoodWall);
        Register("build_stone_wall", FitMode.Stretch, BuildStoneWall);
        Register("build_workbench", FitMode.Stretch, BuildWorkbench);
        Register("build_chest_small", FitMode.Stretch, b => BuildChest(b, false));
        Register("build_chest_large", FitMode.Stretch, b => BuildChest(b, true));
        Register("build_sleeping_bag", FitMode.Stretch, BuildSleepingBag);
        Register("build_lamp", FitMode.Stretch, BuildStandingLamp);
        Register("build_table", FitMode.Stretch, BuildTable);
        Register("build_chair", FitMode.Stretch, BuildChair);
        Register("build_campfire", FitMode.UniformFootprint, b => BuildCampfire(b, false));
        Register("build_campfire_stone", FitMode.UniformFootprint, b => BuildCampfire(b, true));
        Register("fx_flame", FitMode.UniformHeight, BuildFlame);
    }

    private static void BuildWoodFloor(LowPolyMeshBuilder b)
    {
        // 바닥 틈 색 (아래층)
        b.AddBox(StylizedColor.WoodDark, new Vector3(0f, 0.45f, 0f), new Vector3(1f, 0.9f, 1f));
        const int planks = 5;
        float width = 1f / planks;

        for (int index = 0; index < planks; index++)
        {
            float z = -0.5f + width * (index + 0.5f);
            StylizedColor color = index % 2 == 0 ? StylizedColor.WoodPlank : StylizedColor.WoodLight;
            float height = index % 3 == 1 ? 0.97f : 1f;
            b.AddBox(color, new Vector3(0f, height * 0.5f + 0.001f, z), new Vector3(0.995f, height, width - 0.012f));
            // 못 자국
            b.AddBox(StylizedColor.IronDark, new Vector3(0.44f, height + 0.004f, z), new Vector3(0.018f, 0.01f, 0.018f));
            b.AddBox(StylizedColor.IronDark, new Vector3(-0.44f, height + 0.004f, z), new Vector3(0.018f, 0.01f, 0.018f));
        }
    }

    private static void BuildStoneFloor(LowPolyMeshBuilder b)
    {
        b.AddBox(StylizedColor.StoneDark, new Vector3(0f, 0.45f, 0f), new Vector3(1f, 0.9f, 1f));
        const int tiles = 3;
        float size = 1f / tiles;

        for (int x = 0; x < tiles; x++)
        {
            for (int z = 0; z < tiles; z++)
            {
                bool light = (x + z) % 2 == 0;
                float height = (x * 7 + z * 3) % 4 == 0 ? 0.96f : 1f;
                Vector3 center = new Vector3(-0.5f + size * (x + 0.5f), height * 0.5f, -0.5f + size * (z + 0.5f));
                b.AddBeveledBox(light ? StylizedColor.StoneLight : StylizedColor.Stone, center, new Vector3(size - 0.02f, height, size - 0.02f), 0.02f);
            }
        }
    }

    private static void BuildWoodFoundation(LowPolyMeshBuilder b)
    {
        b.AddBeveledBox(StylizedColor.StoneDark, new Vector3(0f, 0.3f, 0f), new Vector3(1f, 0.6f, 1f), 0.04f);
        // 기초 돌 무늬
        for (int index = 0; index < 4; index++)
        {
            float x = -0.36f + index * 0.24f;
            b.AddBeveledBox(StylizedColor.Stone, new Vector3(x, 0.3f, 0.5f), new Vector3(0.2f, 0.4f, 0.02f), 0.01f);
            b.AddBeveledBox(StylizedColor.Stone, new Vector3(x, 0.3f, -0.5f), new Vector3(0.2f, 0.4f, 0.02f), 0.01f);
            b.AddBeveledBox(StylizedColor.Stone, new Vector3(0.5f, 0.3f, x), new Vector3(0.02f, 0.4f, 0.2f), 0.01f);
            b.AddBeveledBox(StylizedColor.Stone, new Vector3(-0.5f, 0.3f, x), new Vector3(0.02f, 0.4f, 0.2f), 0.01f);
        }

        b.AddBox(StylizedColor.WoodDark, new Vector3(0f, 0.8f, 0f), new Vector3(0.98f, 0.4f, 0.98f));
        b.AddBox(StylizedColor.WoodPlank, new Vector3(0f, 0.8f, 0.49f), new Vector3(1f, 0.3f, 0.03f));
        b.AddBox(StylizedColor.WoodPlank, new Vector3(0f, 0.8f, -0.49f), new Vector3(1f, 0.3f, 0.03f));
        b.AddBox(StylizedColor.WoodPlank, new Vector3(0.49f, 0.8f, 0f), new Vector3(0.03f, 0.3f, 1f));
        b.AddBox(StylizedColor.WoodPlank, new Vector3(-0.49f, 0.8f, 0f), new Vector3(0.03f, 0.3f, 1f));
        b.AddBox(StylizedColor.WoodLight, new Vector3(0f, 0.995f, 0f), new Vector3(1f, 0.01f, 1f));
    }

    private static void BuildStoneFoundation(LowPolyMeshBuilder b)
    {
        b.AddBox(StylizedColor.StoneDark, new Vector3(0f, 0.5f, 0f), new Vector3(0.98f, 1f, 0.98f));
        const int rows = 3;

        for (int row = 0; row < rows; row++)
        {
            float y = (row + 0.5f) / rows;
            float height = 1f / rows - 0.03f;
            float offset = row % 2 == 0 ? 0f : 0.16f;

            for (int index = -2; index <= 2; index++)
            {
                float a = index * 0.32f + offset;
                if (a < -0.52f || a > 0.52f)
                {
                    continue;
                }

                float width = Mathf.Min(0.3f, 1.0f - Mathf.Abs(a) * 2f + 0.3f);
                StylizedColor color = (row + index) % 2 == 0 ? StylizedColor.Stone : StylizedColor.StoneLight;
                b.AddBeveledBox(color, new Vector3(Mathf.Clamp(a, -0.35f, 0.35f), y, 0.49f), new Vector3(width, height, 0.04f), 0.012f);
                b.AddBeveledBox(color, new Vector3(Mathf.Clamp(-a, -0.35f, 0.35f), y, -0.49f), new Vector3(width, height, 0.04f), 0.012f);
                b.AddBeveledBox(color, new Vector3(0.49f, y, Mathf.Clamp(a, -0.35f, 0.35f)), new Vector3(0.04f, height, width), 0.012f);
                b.AddBeveledBox(color, new Vector3(-0.49f, y, Mathf.Clamp(-a, -0.35f, 0.35f)), new Vector3(0.04f, height, width), 0.012f);
            }
        }

        b.AddBox(StylizedColor.StoneLight, new Vector3(0f, 0.995f, 0f), new Vector3(1f, 0.01f, 1f));
    }

    // 벽은 X 방향으로 넓고 Z 방향으로 얇다
    private static void BuildWoodWall(LowPolyMeshBuilder b)
    {
        b.AddBox(StylizedColor.WoodDark, new Vector3(0f, 0.5f, 0f), new Vector3(0.96f, 1f, 0.6f));
        const int planks = 6;
        float width = 0.92f / planks;

        for (int index = 0; index < planks; index++)
        {
            float x = -0.46f + width * (index + 0.5f);
            StylizedColor color = index % 2 == 0 ? StylizedColor.WoodPlank : StylizedColor.WoodLight;
            b.AddBox(color, new Vector3(x, 0.5f, 0f), new Vector3(width - 0.012f, 0.98f, 0.9f));
        }

        // 가로 보와 기둥
        b.AddBox(StylizedColor.Bark, new Vector3(0f, 0.12f, 0f), new Vector3(0.96f, 0.07f, 1f));
        b.AddBox(StylizedColor.Bark, new Vector3(0f, 0.88f, 0f), new Vector3(0.96f, 0.07f, 1f));
        b.AddBox(StylizedColor.BarkDark, new Vector3(0.48f, 0.5f, 0f), new Vector3(0.05f, 1f, 1f));
        b.AddBox(StylizedColor.BarkDark, new Vector3(-0.48f, 0.5f, 0f), new Vector3(0.05f, 1f, 1f));
        b.AddBox(StylizedColor.IronDark, new Vector3(0.3f, 0.12f, 0f), new Vector3(0.03f, 0.03f, 1.02f));
        b.AddBox(StylizedColor.IronDark, new Vector3(-0.3f, 0.88f, 0f), new Vector3(0.03f, 0.03f, 1.02f));
    }

    private static void BuildStoneWall(LowPolyMeshBuilder b)
    {
        b.AddBox(StylizedColor.StoneDark, new Vector3(0f, 0.5f, 0f), new Vector3(0.99f, 1f, 0.7f));
        const int rows = 6;

        for (int row = 0; row < rows; row++)
        {
            float y = (row + 0.5f) / rows;
            float height = 1f / rows - 0.02f;
            float start = row % 2 == 0 ? -0.5f : -0.625f;

            for (float x = start; x < 0.5f; x += 0.25f)
            {
                float minX = Mathf.Max(-0.5f, x);
                float maxX = Mathf.Min(0.5f, x + 0.25f);
                float width = maxX - minX - 0.015f;

                if (width < 0.05f)
                {
                    continue;
                }

                int hash = Mathf.Abs(Mathf.RoundToInt(x * 40f) + row * 13);
                StylizedColor color = hash % 3 == 0 ? StylizedColor.StoneLight : StylizedColor.Stone;
                float depth = hash % 2 == 0 ? 0.92f : 0.86f;
                b.AddBeveledBox(color, new Vector3((minX + maxX) * 0.5f, y, 0f), new Vector3(width, height, depth), 0.015f);
            }
        }
    }

    private static void BuildWorkbench(LowPolyMeshBuilder b)
    {
        // 상판과 다리
        b.AddBeveledBox(StylizedColor.WoodLight, new Vector3(0f, 0.9f, 0f), new Vector3(1f, 0.2f, 1f), 0.02f);
        b.AddBox(StylizedColor.WoodPlank, new Vector3(0f, 0.9f, 0f), new Vector3(0.3f, 0.201f, 1.001f));
        Vector3 legSize = new Vector3(0.1f, 0.82f, 0.12f);
        b.AddBox(StylizedColor.WoodDark, new Vector3(0.42f, 0.41f, 0.38f), legSize);
        b.AddBox(StylizedColor.WoodDark, new Vector3(-0.42f, 0.41f, 0.38f), legSize);
        b.AddBox(StylizedColor.WoodDark, new Vector3(0.42f, 0.41f, -0.38f), legSize);
        b.AddBox(StylizedColor.WoodDark, new Vector3(-0.42f, 0.41f, -0.38f), legSize);
        // 아래 선반과 재료
        b.AddBox(StylizedColor.WoodPlank, new Vector3(0f, 0.22f, 0f), new Vector3(0.88f, 0.05f, 0.82f));
        AddLog(b, new Vector3(-0.15f, 0.3f, -0.1f), 0.6f, 0.05f, 0f, StylizedColor.Bark, StylizedColor.WoodLight);
        AddLog(b, new Vector3(-0.1f, 0.3f, 0.05f), 0.55f, 0.05f, 5f, StylizedColor.Bark, StylizedColor.WoodLight);
        b.AddLowPolySphere(StylizedColor.Stone, new Vector3(0.25f, 0.3f, 0.1f), new Vector3(0.08f, 0.06f, 0.07f), 0, 0.2f, 5);
        // 바이스
        b.AddBox(StylizedColor.IronDark, new Vector3(0.38f, 1.06f, 0.42f), new Vector3(0.16f, 0.12f, 0.1f));
        b.AddLimb(StylizedColor.Iron, new Vector3(0.38f, 1.06f, 0.47f), new Vector3(0.38f, 1.06f, 0.62f), 0.012f, 0.012f, 4);
        // 톱과 망치
        b.AddBox(StylizedColor.Iron, new Vector3(-0.2f, 1.005f, 0.1f), new Vector3(0.36f, 0.01f, 0.1f));
        b.AddBox(StylizedColor.Bark, new Vector3(-0.42f, 1.02f, 0.1f), new Vector3(0.1f, 0.04f, 0.07f));
        b.AddLimb(StylizedColor.WoodDark, new Vector3(0.05f, 1.02f, -0.25f), new Vector3(0.3f, 1.02f, -0.3f), 0.015f, 0.015f, 5);
        b.AddBox(StylizedColor.IronDark, new Vector3(0.31f, 1.03f, -0.3f), new Vector3(0.05f, 0.05f, 0.12f));
        // 서랍
        b.AddBox(StylizedColor.WoodPlank, new Vector3(0f, 0.72f, 0.46f), new Vector3(0.5f, 0.14f, 0.08f));
        b.AddBox(StylizedColor.Gold, new Vector3(0f, 0.72f, 0.51f), new Vector3(0.08f, 0.03f, 0.02f));
    }

    private static void BuildChest(LowPolyMeshBuilder b, bool large)
    {
        b.AddBeveledBox(StylizedColor.WoodPlank, new Vector3(0f, 0.3f, 0f), new Vector3(1f, 0.6f, 1f), 0.03f);
        // 둥근 뚜껑
        b.Push(new Vector3(0f, 0.6f, 0f), Euler(0f, 0f, 90f), new Vector3(1f, 1f, 1f));
        b.AddFrustum(StylizedColor.WoodLight, new Vector3(0f, -0.5f, 0f), 0.5f, 0.5f, 1f, 10, true, true, 0f);
        b.Pop();
        // 뚜껑 아래 절반을 가리는 판 (몸통 위)
        b.AddBox(StylizedColor.WoodDark, new Vector3(0f, 0.61f, 0f), new Vector3(1.01f, 0.03f, 1.01f));

        // 금속 띠
        float[] bands = large ? new[] { -0.36f, 0f, 0.36f } : new[] { -0.3f, 0.3f };
        for (int index = 0; index < bands.Length; index++)
        {
            b.AddBox(StylizedColor.IronDark, new Vector3(bands[index], 0.3f, 0f), new Vector3(0.06f, 0.62f, 1.02f));
            b.Push(new Vector3(bands[index], 0.6f, 0f), Euler(0f, 0f, 90f), Vector3.one);
            b.AddFrustum(StylizedColor.IronDark, new Vector3(0f, -0.03f, 0f), 0.51f, 0.51f, 0.06f, 10, false, false);
            b.Pop();
        }

        // 자물쇠 (+Z 앞면)
        b.AddBox(StylizedColor.Gold, new Vector3(0f, 0.58f, 0.52f), new Vector3(0.12f, 0.16f, 0.04f));
        b.AddBox(StylizedColor.Black, new Vector3(0f, 0.56f, 0.545f), new Vector3(0.03f, 0.05f, 0.01f));
        // 발
        b.AddBox(StylizedColor.IronDark, new Vector3(0.46f, 0.02f, 0.46f), new Vector3(0.08f, 0.04f, 0.08f));
        b.AddBox(StylizedColor.IronDark, new Vector3(-0.46f, 0.02f, 0.46f), new Vector3(0.08f, 0.04f, 0.08f));
        b.AddBox(StylizedColor.IronDark, new Vector3(0.46f, 0.02f, -0.46f), new Vector3(0.08f, 0.04f, 0.08f));
        b.AddBox(StylizedColor.IronDark, new Vector3(-0.46f, 0.02f, -0.46f), new Vector3(0.08f, 0.04f, 0.08f));
    }

    // 침낭은 Z 방향으로 길다
    private static void BuildSleepingBag(LowPolyMeshBuilder b)
    {
        b.AddBeveledBox(StylizedColor.ClothGreen, new Vector3(0f, 0.35f, 0.02f), new Vector3(0.9f, 0.7f, 0.96f), 0.18f);
        // 접힌 윗부분
        b.AddBeveledBox(StylizedColor.ClothCream, new Vector3(0f, 0.72f, -0.18f), new Vector3(0.86f, 0.12f, 0.2f), 0.05f);
        b.AddBeveledBox(StylizedColor.ClothBlue, new Vector3(0.1f, 0.75f, 0.2f), new Vector3(0.7f, 0.1f, 0.5f), 0.05f);
        // 베개
        b.AddBeveledBox(StylizedColor.White, new Vector3(0f, 0.72f, -0.4f), new Vector3(0.6f, 0.3f, 0.16f), 0.08f);
        // 끝에 말린 매트와 끈
        b.Push(new Vector3(0f, 0.18f, 0.47f), Euler(0f, 0f, 90f), Vector3.one);
        b.AddCylinder(StylizedColor.ClothRed, new Vector3(0f, -0.45f, 0f), 0.18f, 0.9f, 9);
        b.Pop();
        b.Push(new Vector3(0.25f, 0.18f, 0.47f), Euler(0f, 0f, 90f), Vector3.one);
        b.AddTorus(StylizedColor.Rope, Vector3.zero, 0.19f, 0.02f, 9, 3);
        b.Pop();
        b.Push(new Vector3(-0.25f, 0.18f, 0.47f), Euler(0f, 0f, 90f), Vector3.one);
        b.AddTorus(StylizedColor.Rope, Vector3.zero, 0.19f, 0.02f, 9, 3);
        b.Pop();
    }

    private static void BuildStandingLamp(LowPolyMeshBuilder b)
    {
        b.AddFrustum(StylizedColor.StoneDark, Vector3.zero, 0.45f, 0.38f, 0.08f, 8);
        b.AddFrustum(StylizedColor.IronDark, new Vector3(0f, 0.08f, 0f), 0.08f, 0.05f, 0.64f, 6);
        // 등불 상자
        b.AddBox(StylizedColor.IronDark, new Vector3(0f, 0.72f, 0f), new Vector3(0.36f, 0.03f, 0.36f));
        b.AddBox(StylizedColor.LampGlow, new Vector3(0f, 0.82f, 0f), new Vector3(0.26f, 0.17f, 0.26f));
        b.AddBox(StylizedColor.IronDark, new Vector3(0.15f, 0.82f, 0.15f), new Vector3(0.03f, 0.2f, 0.03f));
        b.AddBox(StylizedColor.IronDark, new Vector3(-0.15f, 0.82f, 0.15f), new Vector3(0.03f, 0.2f, 0.03f));
        b.AddBox(StylizedColor.IronDark, new Vector3(0.15f, 0.82f, -0.15f), new Vector3(0.03f, 0.2f, 0.03f));
        b.AddBox(StylizedColor.IronDark, new Vector3(-0.15f, 0.82f, -0.15f), new Vector3(0.03f, 0.2f, 0.03f));
        b.AddFrustum(StylizedColor.ClothRed, new Vector3(0f, 0.92f, 0f), 0.3f, 0f, 0.07f, 4, true, false, 45f);
        b.AddLowPolySphere(StylizedColor.IronDark, new Vector3(0f, 0.99f, 0f), Vector3.one * 0.02f, 0, 0f, 1);
    }

    private static void BuildTable(LowPolyMeshBuilder b)
    {
        b.AddBeveledBox(StylizedColor.WoodLight, new Vector3(0f, 0.92f, 0f), new Vector3(1f, 0.16f, 1f), 0.03f);
        b.AddBox(StylizedColor.WoodPlank, new Vector3(0f, 0.925f, 0f), new Vector3(1.002f, 0.1f, 0.02f));
        Vector3 legSize = new Vector3(0.1f, 0.85f, 0.1f);
        b.AddBox(StylizedColor.WoodDark, new Vector3(0.4f, 0.425f, 0.4f), legSize);
        b.AddBox(StylizedColor.WoodDark, new Vector3(-0.4f, 0.425f, 0.4f), legSize);
        b.AddBox(StylizedColor.WoodDark, new Vector3(0.4f, 0.425f, -0.4f), legSize);
        b.AddBox(StylizedColor.WoodDark, new Vector3(-0.4f, 0.425f, -0.4f), legSize);
        b.AddBox(StylizedColor.WoodPlank, new Vector3(0f, 0.25f, 0.4f), new Vector3(0.8f, 0.05f, 0.04f));
        b.AddBox(StylizedColor.WoodPlank, new Vector3(0f, 0.25f, -0.4f), new Vector3(0.8f, 0.05f, 0.04f));
        // 탁자 위 작은 소품
        b.AddFrustum(StylizedColor.ClothCream, new Vector3(0.2f, 1f, 0.1f), 0.06f, 0.08f, 0.1f, 8, true, false);
        b.AddBox(StylizedColor.ClothRed, new Vector3(-0.15f, 1.005f, -0.1f), new Vector3(0.4f, 0.01f, 0.3f));
    }

    private static void BuildChair(LowPolyMeshBuilder b)
    {
        b.AddBeveledBox(StylizedColor.WoodLight, new Vector3(0f, 0.47f, 0.05f), new Vector3(0.9f, 0.08f, 0.85f), 0.02f);
        Vector3 legSize = new Vector3(0.09f, 0.45f, 0.09f);
        b.AddBox(StylizedColor.WoodDark, new Vector3(0.38f, 0.225f, 0.4f), legSize);
        b.AddBox(StylizedColor.WoodDark, new Vector3(-0.38f, 0.225f, 0.4f), legSize);
        b.AddBox(StylizedColor.WoodDark, new Vector3(0.38f, 0.225f, -0.32f), legSize);
        b.AddBox(StylizedColor.WoodDark, new Vector3(-0.38f, 0.225f, -0.32f), legSize);
        // 등받이 (-Z 뒤쪽)
        b.AddBox(StylizedColor.WoodDark, new Vector3(0.38f, 0.75f, -0.38f), new Vector3(0.08f, 0.52f, 0.08f));
        b.AddBox(StylizedColor.WoodDark, new Vector3(-0.38f, 0.75f, -0.38f), new Vector3(0.08f, 0.52f, 0.08f));
        b.AddBox(StylizedColor.WoodPlank, new Vector3(0f, 0.95f, -0.38f), new Vector3(0.84f, 0.1f, 0.06f));
        b.AddBox(StylizedColor.WoodPlank, new Vector3(0f, 0.72f, -0.38f), new Vector3(0.84f, 0.08f, 0.05f));
        b.AddBox(StylizedColor.WoodLight, new Vector3(0f, 0.84f, -0.38f), new Vector3(0.12f, 0.2f, 0.04f));
    }

    private static void BuildCampfire(LowPolyMeshBuilder b, bool stoneRing)
    {
        // 재와 숯
        b.AddFrustum(StylizedColor.StoneDark, Vector3.zero, 0.34f, 0.3f, 0.03f, 10);
        b.AddLowPolySphere(StylizedColor.Black, new Vector3(0f, 0.04f, 0f), new Vector3(0.18f, 0.03f, 0.18f), 1, 0.2f, 3);

        int stones = stoneRing ? 12 : 9;
        for (int index = 0; index < stones; index++)
        {
            float angle = index * Mathf.PI * 2f / stones;
            Vector3 position = new Vector3(Mathf.Cos(angle) * 0.42f, 0.07f, Mathf.Sin(angle) * 0.42f);
            StylizedColor color = index % 3 == 0 ? StylizedColor.StoneLight : (index % 3 == 1 ? StylizedColor.Stone : StylizedColor.StoneDark);
            b.AddLowPolySphere(color, position, new Vector3(0.1f, 0.07f, 0.09f), 1, 0.14f, index + 5);

            if (stoneRing)
            {
                Vector3 upper = new Vector3(Mathf.Cos(angle + 0.26f) * 0.4f, 0.18f, Mathf.Sin(angle + 0.26f) * 0.4f);
                b.AddLowPolySphere(StylizedColor.Stone, upper, new Vector3(0.08f, 0.055f, 0.08f), 1, 0.14f, index + 40);
            }
        }

        // 원뿔형으로 세운 장작
        for (int index = 0; index < 5; index++)
        {
            float angle = index * 72f;
            Quaternion yaw = Quaternion.Euler(0f, angle, 0f);
            Vector3 foot = yaw * new Vector3(0f, 0.02f, 0.26f);
            Vector3 top = new Vector3(0f, 0.42f, 0f) + yaw * new Vector3(0f, 0f, 0.03f);
            b.AddLimb(index % 2 == 0 ? StylizedColor.Bark : StylizedColor.BarkDark, foot, top, 0.045f, 0.03f, 6);
        }
    }

    // 모닥불 불꽃 (별도 오브젝트로 붙여 켜고 끌 수 있게 한다)
    private static void BuildFlame(LowPolyMeshBuilder b)
    {
        b.AddFrustum(StylizedColor.Fire, Vector3.zero, 0.2f, 0f, 0.62f, 6, true, false);
        b.AddFrustum(StylizedColor.Fire, new Vector3(0.09f, 0f, 0.05f), 0.12f, 0f, 0.42f, 5, true, false, 20f);
        b.AddFrustum(StylizedColor.Fire, new Vector3(-0.08f, 0f, -0.06f), 0.11f, 0f, 0.38f, 5, true, false, 40f);
        b.AddFrustum(StylizedColor.FireCore, new Vector3(0f, 0f, 0f), 0.12f, 0f, 0.4f, 5, true, false, 10f);
    }
}
