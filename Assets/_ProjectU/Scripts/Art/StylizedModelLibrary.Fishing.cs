using UnityEngine;

// 82일차: 낚시 콘텐츠 모델 (낚싯대, 찌, 던질 지점 고리, 미끼, 물고기, 연못)
// 찌·고리·연못은 크기를 맞추지 않고 그대로 쓰므로 미터 단위로 만든다.
public static partial class StylizedModelLibrary
{
    public const float PondWaterRadius = 3.1f;
    public const float PondWaterHeight = 0.12f;
    public const float PondRimRadius = 3.4f;

    private static void RegisterFishing()
    {
        Register("tool_fishing_rod", FitMode.UniformLargest, BuildFishingRod);
        Register("fx_fishing_bobber", FitMode.UniformLargest, BuildBobber);
        Register("fx_cast_ring", FitMode.UniformLargest, BuildCastRing);
        Register("item_worm_bait", FitMode.UniformLargest, BuildWormBait);

        Register("item_fish_crucian", FitMode.UniformLargest, b => BuildFish(b, StylizedColor.FishSilver, StylizedColor.ClothCream, StylizedColor.FishOlive, 0.16f, 0.035f, 0.08f, FishDetail.None, 301));
        Register("item_fish_trout", FitMode.UniformLargest, b => BuildFish(b, StylizedColor.FishOlive, StylizedColor.ClothCream, StylizedColor.FishOlive, 0.22f, 0.035f, 0.06f, FishDetail.Stripe | FishDetail.Spots, 302));
        Register("item_fish_catfish", FitMode.UniformLargest, b => BuildFish(b, StylizedColor.FishDark, StylizedColor.ClothCream, StylizedColor.Black, 0.26f, 0.05f, 0.07f, FishDetail.Whiskers, 303));
        Register("item_fish_smelt", FitMode.UniformLargest, b => BuildFish(b, StylizedColor.FishBlue, StylizedColor.White, StylizedColor.FishBlue, 0.11f, 0.016f, 0.026f, FishDetail.None, 304));
        Register("item_fish_golden_carp", FitMode.UniformLargest, b => BuildFish(b, StylizedColor.FishGold, StylizedColor.FlowerYellow, StylizedColor.AppleRed, 0.2f, 0.045f, 0.1f, FishDetail.Whiskers | FishDetail.Spots, 305));

        Register("prop_pond", FitMode.UniformLargest, BuildPond);
    }

    [System.Flags]
    private enum FishDetail
    {
        None = 0,
        Stripe = 1,
        Spots = 2,
        Whiskers = 4
    }

    // ---------------------------------------------------------------- 낚싯대·찌·미끼

    // 손잡이는 +Y 방향, 낚싯대 끝은 (0, 1.6, 0)
    private static void BuildFishingRod(LowPolyMeshBuilder b)
    {
        b.AddLimb(StylizedColor.WoodPlank, new Vector3(0f, 0f, 0f), new Vector3(0f, 1.6f, 0f), 0.022f, 0.006f, 6);
        b.AddLimb(StylizedColor.Sand, new Vector3(0f, 0.03f, 0f), new Vector3(0f, 0.34f, 0f), 0.03f, 0.028f, 7);
        b.AddTorus(StylizedColor.BarkDark, new Vector3(0f, 0.36f, 0f), 0.03f, 0.008f, 8, 3);

        // 릴
        b.Push(new Vector3(0.055f, 0.43f, 0f), Quaternion.Euler(0f, 0f, 90f), Vector3.one);
        b.AddCylinder(StylizedColor.IronDark, new Vector3(0f, -0.028f, 0f), 0.042f, 0.056f, 10);
        b.AddCylinder(StylizedColor.Iron, new Vector3(0f, -0.03f, 0f), 0.02f, 0.06f, 8);
        b.Pop();
        b.AddLimb(StylizedColor.IronDark, new Vector3(0.02f, 0.43f, 0f), new Vector3(0.055f, 0.43f, 0f), 0.008f, 0.008f, 4);
        b.AddLimb(StylizedColor.Iron, new Vector3(0.085f, 0.43f, 0f), new Vector3(0.085f, 0.43f, 0.05f), 0.005f, 0.005f, 4);
        b.AddLowPolySphere(StylizedColor.BarkDark, new Vector3(0.085f, 0.43f, 0.055f), Vector3.one * 0.01f, 0, 0f, 1);

        // 줄 가이드와 줄
        float[] guides = { 0.8f, 1.1f, 1.35f, 1.55f };

        for (int index = 0; index < guides.Length; index++)
        {
            b.Push(new Vector3(0.018f, guides[index], 0f), Quaternion.Euler(90f, 0f, 0f), Vector3.one);
            b.AddTorus(StylizedColor.Iron, Vector3.zero, 0.011f - index * 0.001f, 0.0028f, 6, 3);
            b.Pop();
        }

        b.AddLimb(StylizedColor.White, new Vector3(0.05f, 0.44f, 0f), new Vector3(0.012f, 1.6f, 0f), 0.0018f, 0.0018f, 3);
    }

    // 물 위에 반쯤 잠기는 찌 (중심이 수면)
    private static void BuildBobber(LowPolyMeshBuilder b)
    {
        b.AddLowPolySphere(StylizedColor.ClothRed, Vector3.zero, new Vector3(0.05f, 0.045f, 0.05f), 1, 0f, 1);
        b.AddLowPolySphere(StylizedColor.White, new Vector3(0f, 0.028f, 0f), new Vector3(0.042f, 0.032f, 0.042f), 1, 0f, 2);
        b.AddLimb(StylizedColor.White, new Vector3(0f, 0.05f, 0f), new Vector3(0f, 0.12f, 0f), 0.006f, 0.004f, 4);
        b.AddLowPolySphere(StylizedColor.ClothRed, new Vector3(0f, 0.125f, 0f), Vector3.one * 0.011f, 0, 0f, 3);
        b.AddLimb(StylizedColor.BarkDark, new Vector3(0f, -0.04f, 0f), new Vector3(0f, -0.1f, 0f), 0.005f, 0.003f, 3);
    }

    // 던질 지점을 알려 주는 수면 위 고리
    private static void BuildCastRing(LowPolyMeshBuilder b)
    {
        b.AddTorus(StylizedColor.White, Vector3.zero, 0.34f, 0.018f, 20, 3);
        b.AddTorus(StylizedColor.FishBlue, Vector3.zero, 0.2f, 0.012f, 16, 3);
        b.AddLowPolySphere(StylizedColor.White, Vector3.zero, new Vector3(0.03f, 0.01f, 0.03f), 0, 0f, 1);
    }

    private static void BuildWormBait(LowPolyMeshBuilder b)
    {
        b.AddCylinder(StylizedColor.IronDark, Vector3.zero, 0.12f, 0.1f, 12);
        b.AddTorus(StylizedColor.ClothGreen, new Vector3(0f, 0.05f, 0f), 0.121f, 0.02f, 12, 3);
        b.AddDisc(StylizedColor.SoilTilled, new Vector3(0f, 0.101f, 0f), 0.108f, 12);

        // 흙 밖으로 나온 지렁이
        Vector3[][] worms =
        {
            new[] { new Vector3(-0.05f, 0.1f, 0.02f), new Vector3(-0.03f, 0.15f, 0.03f), new Vector3(0.01f, 0.16f, 0.01f), new Vector3(0.03f, 0.12f, -0.02f) },
            new[] { new Vector3(0.04f, 0.1f, 0.05f), new Vector3(0.06f, 0.14f, 0.07f), new Vector3(0.1f, 0.13f, 0.1f), new Vector3(0.15f, 0.06f, 0.12f), new Vector3(0.18f, 0.005f, 0.12f) },
            new[] { new Vector3(0.02f, 0.1f, -0.06f), new Vector3(0f, 0.13f, -0.09f), new Vector3(-0.03f, 0.12f, -0.1f) }
        };

        foreach (Vector3[] worm in worms)
        {
            for (int index = 1; index < worm.Length; index++)
            {
                b.AddLimb(StylizedColor.Worm, worm[index - 1], worm[index], 0.012f, 0.011f, 5);
                b.AddLowPolySphere(StylizedColor.Worm, worm[index], Vector3.one * 0.0115f, 0, 0f, index);
            }
        }
    }

    // ---------------------------------------------------------------- 물고기

    // 옆으로 누운 물고기 : 길이는 X, 두께는 Y, 몸 높이는 Z (머리가 +X)
    private static void BuildFish(LowPolyMeshBuilder b, StylizedColor body, StylizedColor belly, StylizedColor fin, float halfLength, float halfThickness, float halfHeight, FishDetail details, int seed)
    {
        float y = halfThickness;
        b.AddLowPolySphere(body, new Vector3(0f, y, 0f), new Vector3(halfLength, halfThickness, halfHeight), 2, 0.02f, seed);
        b.AddLowPolySphere(belly, new Vector3(halfLength * 0.05f, y * 1.02f, -halfHeight * 0.35f), new Vector3(halfLength * 0.78f, halfThickness * 0.92f, halfHeight * 0.55f), 1, 0f, seed + 1);

        // 꼬리지느러미 (양면)
        Vector3 tailRoot = new Vector3(-halfLength * 0.9f, y, 0f);
        Vector3 tailTop = new Vector3(-halfLength * 1.55f, y, halfHeight * 0.95f);
        Vector3 tailBottom = new Vector3(-halfLength * 1.55f, y, -halfHeight * 0.95f);
        Vector3 tailNotch = new Vector3(-halfLength * 1.35f, y, 0f);
        AddDoubleTriangle(b, fin, tailRoot, tailTop, tailNotch);
        AddDoubleTriangle(b, fin, tailRoot, tailNotch, tailBottom);

        // 등지느러미와 배지느러미 (양면)
        AddDoubleTriangle(b, fin, new Vector3(-halfLength * 0.25f, y, halfHeight * 0.85f), new Vector3(halfLength * 0.3f, y, halfHeight * 0.85f), new Vector3(-halfLength * 0.1f, y, halfHeight * 1.5f));
        AddDoubleTriangle(b, fin, new Vector3(-halfLength * 0.1f, y, -halfHeight * 0.85f), new Vector3(halfLength * 0.2f, y, -halfHeight * 0.85f), new Vector3(0f, y, -halfHeight * 1.3f));

        // 옆지느러미 (위쪽 면)
        AddDoubleTriangle(b, fin, new Vector3(halfLength * 0.45f, y * 1.9f, -halfHeight * 0.1f), new Vector3(halfLength * 0.3f, y * 1.95f, -halfHeight * 0.1f), new Vector3(halfLength * 0.22f, y * 1.95f, -halfHeight * 0.6f));

        // 눈
        Vector3 eye = new Vector3(halfLength * 0.66f, y * 1.75f, halfHeight * 0.22f);
        b.AddLowPolySphere(StylizedColor.White, eye, Vector3.one * Mathf.Max(0.006f, halfHeight * 0.2f), 0, 0f, seed + 2);
        b.AddLowPolySphere(StylizedColor.Black, eye + new Vector3(0.003f, halfHeight * 0.08f, 0f), Vector3.one * Mathf.Max(0.004f, halfHeight * 0.12f), 0, 0f, seed + 3);

        // 아가미 선
        b.AddLowPolySphere(fin, new Vector3(halfLength * 0.45f, y * 1.8f, 0f), new Vector3(halfLength * 0.02f, halfThickness * 0.2f, halfHeight * 0.55f), 0, 0f, seed + 4);

        if ((details & FishDetail.Stripe) != 0)
        {
            b.AddLowPolySphere(StylizedColor.Strawberry, new Vector3(0f, y * 1.85f, 0f), new Vector3(halfLength * 0.8f, halfThickness * 0.2f, halfHeight * 0.14f), 1, 0f, seed + 5);
        }

        if ((details & FishDetail.Spots) != 0)
        {
            System.Random random = new System.Random(seed);

            for (int index = 0; index < 8; index++)
            {
                Vector3 spot = new Vector3(Rand(random, -0.6f, 0.5f) * halfLength, y * 1.88f, Rand(random, -0.2f, 0.7f) * halfHeight);
                b.AddLowPolySphere(StylizedColor.BarkDark, spot, new Vector3(halfLength * 0.04f, halfThickness * 0.15f, halfLength * 0.04f), 0, 0f, seed + 10 + index);
            }
        }

        if ((details & FishDetail.Whiskers) != 0)
        {
            Vector3 mouth = new Vector3(halfLength * 0.98f, y, -halfHeight * 0.1f);
            b.AddLimb(StylizedColor.Black, mouth, mouth + new Vector3(halfLength * 0.35f, 0f, -halfHeight * 0.9f), 0.004f, 0.002f, 3);
            b.AddLimb(StylizedColor.Black, mouth, mouth + new Vector3(halfLength * 0.4f, 0f, halfHeight * 0.3f), 0.004f, 0.002f, 3);
        }
    }

    private static void AddDoubleTriangle(LowPolyMeshBuilder b, StylizedColor color, Vector3 a, Vector3 c, Vector3 d)
    {
        b.AddTriangle(color, a, c, d);
        b.AddTriangle(color, a, d, c);
    }

    // ---------------------------------------------------------------- 연못

    // 반지름 3.1m 물, 3.4m 돌 테두리 (기준점이 연못 중심 바닥)
    private static void BuildPond(LowPolyMeshBuilder b)
    {
        b.AddDisc(StylizedColor.PondDeep, new Vector3(0f, 0.02f, 0f), PondWaterRadius + 0.1f, 28);
        b.AddFrustum(StylizedColor.Dirt, Vector3.zero, PondRimRadius + 0.25f, PondWaterRadius + 0.05f, 0.15f, 28, false, false);
        b.AddDisc(StylizedColor.Water, new Vector3(0f, PondWaterHeight, 0f), PondWaterRadius + 0.05f, 28);

        System.Random random = new System.Random(411);
        StylizedColor[] stoneColors = { StylizedColor.Stone, StylizedColor.StoneLight, StylizedColor.StoneDark };

        // 테두리 돌
        const int stones = 30;

        for (int index = 0; index < stones; index++)
        {
            float angle = (index + Rand(random, -0.3f, 0.3f)) * Mathf.PI * 2f / stones;
            float distance = PondRimRadius + Rand(random, -0.12f, 0.12f);
            float size = Rand(random, 0.12f, 0.26f);
            Vector3 position = new Vector3(Mathf.Cos(angle) * distance, 0.1f, Mathf.Sin(angle) * distance);
            b.AddLowPolySphere(stoneColors[index % stoneColors.Length], position, new Vector3(size * 1.3f, size * 0.8f, size), 1, 0.2f, 420 + index);
        }

        // 연잎과 꽃
        for (int index = 0; index < 7; index++)
        {
            float angle = Rand(random, 0f, Mathf.PI * 2f);
            float distance = Rand(random, 0.8f, 2.4f);
            float radius = Rand(random, 0.16f, 0.26f);
            Vector3 center = new Vector3(Mathf.Cos(angle) * distance, PondWaterHeight + 0.008f, Mathf.Sin(angle) * distance);
            b.AddDisc(index % 2 == 0 ? StylizedColor.LeafDark : StylizedColor.Leaf, center, radius, 9);

            if (index % 3 == 0)
            {
                AddSmallFlower(b, center + new Vector3(0.02f, 0.03f, 0.02f), 0.03f, StylizedColor.Flower, 460 + index);
            }
        }

        // 갈대 무리
        for (int cluster = 0; cluster < 6; cluster++)
        {
            float angle = cluster * Mathf.PI * 2f / 6f + Rand(random, -0.2f, 0.2f);
            Vector3 root = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (PondWaterRadius - 0.1f);

            for (int stem = 0; stem < 5; stem++)
            {
                Vector3 offset = new Vector3(Rand(random, -0.18f, 0.18f), 0.08f, Rand(random, -0.18f, 0.18f));
                float height = Rand(random, 0.7f, 1.2f);
                Vector3 top = root + offset + new Vector3(Rand(random, -0.08f, 0.08f), height, Rand(random, -0.08f, 0.08f));
                b.AddLimb(stem % 2 == 0 ? StylizedColor.Grass : StylizedColor.LeafDark, root + offset, top, 0.012f, 0.004f, 3);

                if (stem % 2 == 0)
                {
                    b.AddLimb(StylizedColor.Bark, top - new Vector3(0f, 0.2f, 0f), top - new Vector3(0f, 0.04f, 0f), 0.02f, 0.018f, 5);
                }
            }
        }
    }
}
