using UnityEngine;

public static partial class StylizedModelLibrary
{
    private static void RegisterNature()
    {
        Register("tree_round", FitMode.UniformHeight, b => BuildRoundTree(b, StylizedColor.Leaf, StylizedColor.LeafDark, StylizedColor.LeafLight, 11));
        Register("tree_round_b", FitMode.UniformHeight, b => BuildRoundTree(b, StylizedColor.LeafLight, StylizedColor.Leaf, StylizedColor.LeafDark, 23));
        Register("tree_autumn", FitMode.UniformHeight, b => BuildRoundTree(b, StylizedColor.LeafAutumn, StylizedColor.FlowerYellow, StylizedColor.AppleRed, 37));
        Register("tree_pine", FitMode.UniformHeight, BuildPineTree);
        Register("rock_large", FitMode.Stretch, b => BuildRock(b, 5, true));
        Register("rock_small", FitMode.UniformLargest, b => BuildRock(b, 9, false));
        Register("rock_resource", FitMode.Stretch, BuildResourceRock);
        Register("bush", FitMode.UniformLargest, b => BuildBush(b, false));
        Register("bush_berry", FitMode.UniformLargest, b => BuildBush(b, true));
        Register("grass_tuft", FitMode.UniformHeight, BuildGrassTuft);
        Register("flower_patch", FitMode.UniformLargest, BuildFlowerPatch);
        Register("mushroom_cluster", FitMode.UniformLargest, BuildMushroomCluster);
        Register("stump", FitMode.UniformLargest, BuildStump);
        Register("fallen_log", FitMode.UniformLargest, BuildFallenLog);
        Register("pebbles", FitMode.UniformLargest, BuildPebbles);
        Register("reeds", FitMode.UniformHeight, BuildReeds);
        Register("mountain", FitMode.UniformHeight, b => BuildMountain(b, 3, true));
        Register("mountain_low", FitMode.UniformHeight, b => BuildMountain(b, 7, false));
        Register("cliff", FitMode.Stretch, BuildCliff);
    }

    private static void BuildRoundTree(LowPolyMeshBuilder b, StylizedColor main, StylizedColor shade, StylizedColor highlight, int seed)
    {
        // 줄기와 뿌리
        b.AddFrustum(StylizedColor.Bark, Vector3.zero, 0.26f, 0.15f, 1.9f, 7);
        for (int index = 0; index < 4; index++)
        {
            float angle = index * 90f + 20f;
            b.Push(Vector3.zero, Euler(0f, angle, 0f), Vector3.one);
            b.AddWedge(StylizedColor.BarkDark, new Vector3(0f, 0.12f, 0.28f), new Vector3(0.14f, 0.24f, 0.26f));
            b.Pop();
        }

        // 가지
        b.AddLimb(StylizedColor.Bark, new Vector3(0f, 1.45f, 0f), new Vector3(0.55f, 2.05f, 0.15f), 0.08f, 0.04f, 5);
        b.AddLimb(StylizedColor.Bark, new Vector3(0f, 1.6f, 0f), new Vector3(-0.45f, 2.2f, -0.2f), 0.07f, 0.035f, 5);

        // 수관 (여러 잎 덩어리)
        AddFoliage(b, new Vector3(0f, 2.75f, 0f), new Vector3(1.25f, 1.05f, 1.25f), main, seed);
        AddFoliage(b, new Vector3(0.7f, 2.35f, 0.25f), new Vector3(0.78f, 0.7f, 0.78f), shade, seed + 1);
        AddFoliage(b, new Vector3(-0.62f, 2.45f, -0.3f), new Vector3(0.82f, 0.72f, 0.82f), shade, seed + 2);
        AddFoliage(b, new Vector3(0.15f, 3.45f, -0.1f), new Vector3(0.75f, 0.62f, 0.75f), highlight, seed + 3);
        AddFoliage(b, new Vector3(-0.2f, 2.5f, 0.75f), new Vector3(0.6f, 0.55f, 0.6f), main, seed + 4);
    }

    private static void BuildPineTree(LowPolyMeshBuilder b)
    {
        b.AddFrustum(StylizedColor.BarkDark, Vector3.zero, 0.2f, 0.12f, 1.4f, 6);
        float[] heights = { 0.9f, 1.8f, 2.6f, 3.3f };
        float[] radii = { 1.35f, 1.1f, 0.85f, 0.55f };

        for (int index = 0; index < heights.Length; index++)
        {
            StylizedColor color = index % 2 == 0 ? StylizedColor.LeafDark : StylizedColor.Leaf;
            b.AddFrustum(color, new Vector3(0f, heights[index], 0f), radii[index], 0f, 1.35f, 7, true, false, index * 23f);
        }
    }

    private static void BuildRock(LowPolyMeshBuilder b, int seed, bool withMoss)
    {
        b.AddLowPolySphere(StylizedColor.Stone, new Vector3(0f, 0.38f, 0f), new Vector3(0.5f, 0.42f, 0.46f), 1, 0.22f, seed);
        b.AddLowPolySphere(StylizedColor.StoneDark, new Vector3(0.28f, 0.2f, 0.18f), new Vector3(0.24f, 0.2f, 0.22f), 1, 0.25f, seed + 1);
        b.AddLowPolySphere(StylizedColor.StoneLight, new Vector3(-0.22f, 0.16f, -0.24f), new Vector3(0.2f, 0.16f, 0.18f), 1, 0.25f, seed + 2);

        if (withMoss)
        {
            b.AddLowPolySphere(StylizedColor.LeafDark, new Vector3(-0.05f, 0.74f, 0.02f), new Vector3(0.3f, 0.07f, 0.26f), 1, 0.2f, seed + 3);
        }
    }

    private static void BuildResourceRock(LowPolyMeshBuilder b)
    {
        BuildRock(b, 17, false);
        // 채굴 가능한 돌임을 알리는 밝은 줄무늬 조각
        b.AddLowPolySphere(StylizedColor.StoneLight, new Vector3(0.12f, 0.58f, -0.2f), new Vector3(0.12f, 0.08f, 0.1f), 0, 0.1f, 3);
        b.AddLowPolySphere(StylizedColor.Sand, new Vector3(-0.25f, 0.5f, 0.22f), new Vector3(0.1f, 0.07f, 0.09f), 0, 0.1f, 4);
    }

    private static void BuildBush(LowPolyMeshBuilder b, bool withBerries)
    {
        AddFoliage(b, new Vector3(0f, 0.38f, 0f), new Vector3(0.5f, 0.4f, 0.5f), StylizedColor.Leaf, 41);
        AddFoliage(b, new Vector3(0.35f, 0.28f, 0.1f), new Vector3(0.34f, 0.3f, 0.34f), StylizedColor.LeafDark, 42);
        AddFoliage(b, new Vector3(-0.3f, 0.3f, -0.12f), new Vector3(0.36f, 0.3f, 0.36f), StylizedColor.LeafLight, 43);

        if (!withBerries)
        {
            return;
        }

        System.Random random = new System.Random(7);

        for (int index = 0; index < 9; index++)
        {
            float angle = Rand(random, 0f, Mathf.PI * 2f);
            float height = Rand(random, 0.25f, 0.65f);
            float radius = Mathf.Lerp(0.5f, 0.3f, (height - 0.25f) / 0.4f);
            Vector3 position = new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius);
            b.AddLowPolySphere(StylizedColor.BerryPurple, position, Vector3.one * 0.06f, 1, 0.04f, index);
        }
    }

    private static void BuildGrassTuft(LowPolyMeshBuilder b)
    {
        System.Random random = new System.Random(3);

        for (int index = 0; index < 7; index++)
        {
            float angle = index * 51f;
            float lean = Rand(random, 8f, 22f);
            Vector3 offset = new Vector3(Rand(random, -0.12f, 0.12f), 0f, Rand(random, -0.12f, 0.12f));
            StylizedColor color = index % 3 == 0 ? StylizedColor.LeafLight : StylizedColor.Grass;
            b.Push(offset, Euler(lean, angle, 0f), Vector3.one);
            b.AddFrustum(color, Vector3.zero, 0.045f, 0f, Rand(random, 0.35f, 0.6f), 3, false, false);
            b.Pop();
        }
    }

    private static void BuildFlowerPatch(LowPolyMeshBuilder b)
    {
        BuildGrassTuft(b);
        System.Random random = new System.Random(19);

        for (int index = 0; index < 5; index++)
        {
            Vector3 root = new Vector3(Rand(random, -0.25f, 0.25f), 0f, Rand(random, -0.25f, 0.25f));
            float height = Rand(random, 0.3f, 0.5f);
            b.AddLimb(StylizedColor.LeafDark, root, root + Vector3.up * height, 0.015f, 0.012f, 3);
            StylizedColor petal = index % 2 == 0 ? StylizedColor.Flower : StylizedColor.FlowerYellow;
            b.AddFrustum(petal, root + Vector3.up * height, 0.02f, 0.09f, 0.05f, 5, false, true, index * 17f);
            b.AddLowPolySphere(StylizedColor.FlowerYellow, root + Vector3.up * (height + 0.055f), Vector3.one * 0.03f, 0, 0f, index);
        }
    }

    private static void BuildMushroomCluster(LowPolyMeshBuilder b)
    {
        AddMushroom(b, Vector3.zero, 1f, 0f);
        AddMushroom(b, new Vector3(0.22f, 0f, 0.1f), 0.65f, 20f);
        AddMushroom(b, new Vector3(-0.16f, 0f, 0.18f), 0.5f, -15f);
    }

    private static void AddMushroom(LowPolyMeshBuilder b, Vector3 position, float scale, float tilt)
    {
        b.Push(position, Euler(0f, 0f, tilt), Vector3.one * scale);
        b.AddFrustum(StylizedColor.MushroomStem, Vector3.zero, 0.07f, 0.055f, 0.28f, 6);
        b.AddFrustum(StylizedColor.MushroomCap, new Vector3(0f, 0.26f, 0f), 0.24f, 0.16f, 0.07f, 8, true, false);
        b.AddFrustum(StylizedColor.MushroomCap, new Vector3(0f, 0.33f, 0f), 0.16f, 0f, 0.12f, 8, false, false);
        b.AddLowPolySphere(StylizedColor.White, new Vector3(0.1f, 0.34f, 0.05f), Vector3.one * 0.03f, 0, 0f, 1);
        b.AddLowPolySphere(StylizedColor.White, new Vector3(-0.07f, 0.36f, -0.07f), Vector3.one * 0.025f, 0, 0f, 2);
        b.AddLowPolySphere(StylizedColor.White, new Vector3(-0.02f, 0.31f, 0.14f), Vector3.one * 0.025f, 0, 0f, 3);
        b.Pop();
    }

    private static void BuildStump(LowPolyMeshBuilder b)
    {
        b.AddFrustum(StylizedColor.Bark, Vector3.zero, 0.42f, 0.34f, 0.45f, 8, false, false);
        b.AddDisc(StylizedColor.WoodLight, new Vector3(0f, 0.45f, 0f), 0.34f, 8);
        b.AddDisc(StylizedColor.WoodPlank, new Vector3(0f, 0.452f, 0f), 0.16f, 8);

        for (int index = 0; index < 3; index++)
        {
            b.Push(Vector3.zero, Euler(0f, index * 120f + 15f, 0f), Vector3.one);
            b.AddWedge(StylizedColor.BarkDark, new Vector3(0f, 0.1f, 0.42f), new Vector3(0.16f, 0.2f, 0.24f));
            b.Pop();
        }

        AddFoliage(b, new Vector3(0.28f, 0.1f, -0.3f), new Vector3(0.14f, 0.08f, 0.12f), StylizedColor.LeafDark, 5);
    }

    private static void BuildFallenLog(LowPolyMeshBuilder b)
    {
        AddLog(b, new Vector3(0f, 0.28f, 0f), 2.2f, 0.28f, 8f, StylizedColor.Bark, StylizedColor.WoodLight);
        b.AddLimb(StylizedColor.Bark, new Vector3(0.4f, 0.45f, 0.05f), new Vector3(0.65f, 0.85f, 0.15f), 0.06f, 0.03f, 5);
        AddFoliage(b, new Vector3(-0.5f, 0.52f, 0f), new Vector3(0.35f, 0.08f, 0.2f), StylizedColor.LeafDark, 12);
        AddMushroom(b, new Vector3(0.7f, 0.1f, 0.3f), 0.45f, 0f);
    }

    private static void BuildPebbles(LowPolyMeshBuilder b)
    {
        System.Random random = new System.Random(29);

        for (int index = 0; index < 6; index++)
        {
            Vector3 position = new Vector3(Rand(random, -0.35f, 0.35f), 0.04f, Rand(random, -0.35f, 0.35f));
            float size = Rand(random, 0.06f, 0.13f);
            StylizedColor color = index % 2 == 0 ? StylizedColor.Stone : StylizedColor.StoneLight;
            b.AddLowPolySphere(color, position, new Vector3(size, size * 0.6f, size), 1, 0.15f, index);
        }
    }

    private static void BuildReeds(LowPolyMeshBuilder b)
    {
        System.Random random = new System.Random(31);

        for (int index = 0; index < 9; index++)
        {
            Vector3 root = new Vector3(Rand(random, -0.3f, 0.3f), 0f, Rand(random, -0.3f, 0.3f));
            float height = Rand(random, 0.7f, 1.2f);
            Vector3 top = root + new Vector3(Rand(random, -0.1f, 0.1f), height, Rand(random, -0.1f, 0.1f));
            b.AddLimb(StylizedColor.Herb, root, top, 0.02f, 0.01f, 3);

            if (index % 3 == 0)
            {
                b.AddLimb(StylizedColor.Bark, top - Vector3.up * 0.18f, top, 0.035f, 0.03f, 5);
            }
        }
    }

    private static void BuildMountain(LowPolyMeshBuilder b, int seed, bool snow)
    {
        // 크기 : 지름 약 60, 높이 약 35 (배경용)
        if (snow)
        {
            b.AddNoisyCone(StylizedColor.Mountain, StylizedColor.MountainDark, StylizedColor.Snow, 0.68f, Vector3.zero, 30f, 36f, 9, 5, 0.14f, seed);
            b.AddNoisyCone(StylizedColor.MountainDark, StylizedColor.Mountain, StylizedColor.Snow, 0.78f, new Vector3(20f, 0f, 8f), 18f, 22f, 8, 4, 0.16f, seed + 1);
            b.AddNoisyCone(StylizedColor.Mountain, StylizedColor.MountainDark, StylizedColor.Snow, 0.8f, new Vector3(-21f, 0f, -5f), 16f, 18f, 8, 4, 0.16f, seed + 2);
        }
        else
        {
            // 나무로 덮인 낮은 언덕
            b.AddNoisyCone(StylizedColor.LeafDark, StylizedColor.Leaf, StylizedColor.LeafDark, 2f, Vector3.zero, 30f, 20f, 10, 4, 0.12f, seed);
            b.AddNoisyCone(StylizedColor.Leaf, StylizedColor.LeafDark, StylizedColor.Leaf, 2f, new Vector3(18f, 0f, 6f), 18f, 13f, 8, 3, 0.14f, seed + 1);
            b.AddNoisyCone(StylizedColor.MountainDark, StylizedColor.Mountain, StylizedColor.LeafDark, 0.85f, new Vector3(-16f, 0f, -8f), 14f, 16f, 7, 3, 0.16f, seed + 2);
        }
    }

    private static void BuildCliff(LowPolyMeshBuilder b)
    {
        System.Random random = new System.Random(53);

        for (int index = 0; index < 6; index++)
        {
            float x = -0.45f + index * 0.18f;
            float height = Rand(random, 0.6f, 1f);
            StylizedColor color = index % 2 == 0 ? StylizedColor.Stone : StylizedColor.StoneDark;
            b.AddLowPolySphere(color, new Vector3(x, height * 0.5f, Rand(random, -0.08f, 0.08f)), new Vector3(0.14f, height * 0.5f, 0.45f), 1, 0.15f, index);
        }

        AddFoliage(b, new Vector3(0f, 0.95f, 0f), new Vector3(0.5f, 0.06f, 0.4f), StylizedColor.LeafDark, 77);
    }
}
