using UnityEngine;

// 87일차: 판매 상자 · 상인 가판대 · 떠돌이 상인 · 판매 상자 깃발
// 판매 상자와 가판대는 가로·세로·높이 1 상자 안에 만들고, 플레이어를 바라보는 앞면은 -Z 이다.
// 상인과 깃발은 미터 단위이며 상인은 +Z 를 바라본다.
public static partial class StylizedModelLibrary
{
    // 생성 도구가 실제 크기로 쓰는 값 (가판대 과일이 찌그러지지 않게 모델에서도 사용)
    public static readonly Vector3 ShippingBinSize = new Vector3(1.2f, 0.95f, 0.8f);
    public static readonly Vector3 MarketStallSize = new Vector3(2.6f, 2.5f, 1.8f);
    public const float ShippingBinBodyTopUnit = 0.72f;
    public const float MarketCounterTopUnit = 0.43f;

    private static void RegisterMarket()
    {
        Register("build_shipping_bin", FitMode.Stretch, BuildShippingBin);
        Register("build_market_stall", FitMode.Stretch, BuildMarketStall);
        Register("npc_merchant", FitMode.UniformHeight, BuildMerchant);
        Register("fx_bin_flag", FitMode.UniformLargest, BuildBinFlag);
    }

    // ---------------------------------------------------------------- 판매 상자 (1 상자)

    private static void BuildShippingBin(LowPolyMeshBuilder b)
    {
        Vector3 size = ShippingBinSize;
        float top = ShippingBinBodyTopUnit;

        // 발과 몸통
        for (int x = -1; x <= 1; x += 2)
        {
            for (int z = -1; z <= 1; z += 2)
            {
                b.AddBox(StylizedColor.WoodDark, new Vector3(x * 0.4f, 0.04f, z * 0.36f), new Vector3(0.1f, 0.08f, 0.12f));
            }
        }

        b.AddBox(StylizedColor.WoodPlank, new Vector3(0f, 0.07f + (top - 0.07f) * 0.5f, 0f), new Vector3(0.96f, top - 0.07f, 0.9f));

        // 판자 이음새와 모서리 기둥
        foreach (float y in new[] { 0.29f, 0.51f })
        {
            b.AddBox(StylizedColor.WoodDark, new Vector3(0f, y, 0f), new Vector3(0.968f, 0.018f, 0.908f));
        }

        for (int x = -1; x <= 1; x += 2)
        {
            for (int z = -1; z <= 1; z += 2)
            {
                b.AddBox(StylizedColor.WoodDark, new Vector3(x * 0.47f, 0.07f + (top - 0.07f) * 0.5f, z * 0.44f), new Vector3(0.06f, top - 0.06f, 0.06f));
            }
        }

        // 윗테두리 (속이 빈 네모 띠) 와 안쪽 바닥
        b.AddBox(StylizedColor.IronDark, new Vector3(0f, top - 0.02f, -0.445f), new Vector3(0.985f, 0.04f, 0.035f));
        b.AddBox(StylizedColor.IronDark, new Vector3(0f, top - 0.02f, 0.445f), new Vector3(0.985f, 0.04f, 0.035f));
        b.AddBox(StylizedColor.IronDark, new Vector3(-0.475f, top - 0.02f, 0f), new Vector3(0.035f, 0.04f, 0.925f));
        b.AddBox(StylizedColor.IronDark, new Vector3(0.475f, top - 0.02f, 0f), new Vector3(0.035f, 0.04f, 0.925f));
        b.AddBox(StylizedColor.WoodDark, new Vector3(0f, top - 0.1f, 0f), new Vector3(0.9f, 0.01f, 0.84f));

        // 뚜껑 틈 사이로 보이는 물건 (호박·토마토·사과·달걀)
        AddScaledBall(b, StylizedColor.Pumpkin, new Vector3(-0.22f, top - 0.06f, -0.12f), 0.11f, size, 1301);
        AddScaledBall(b, StylizedColor.Tomato, new Vector3(0.05f, top - 0.06f, -0.2f), 0.075f, size, 1302);
        AddScaledBall(b, StylizedColor.AppleRed, new Vector3(0.24f, top - 0.06f, -0.1f), 0.07f, size, 1303);
        AddScaledBall(b, StylizedColor.Egg, new Vector3(0.12f, top - 0.07f, 0.1f), 0.05f, size, 1304);

        // 뚜껑 (뒤쪽 경첩, 앞이 살짝 들림)
        b.Push(new Vector3(0f, top + 0.005f, 0.455f), Euler(14f, 0f, 0f), Vector3.one);
        b.AddBox(StylizedColor.WoodLight, new Vector3(0f, 0.035f, -0.475f), new Vector3(1f, 0.07f, 0.95f));
        b.AddBox(StylizedColor.WoodDark, new Vector3(0f, 0.073f, -0.3f), new Vector3(1.002f, 0.012f, 0.05f));
        b.AddBox(StylizedColor.WoodDark, new Vector3(0f, 0.073f, -0.7f), new Vector3(1.002f, 0.012f, 0.05f));
        b.AddBox(StylizedColor.IronDark, new Vector3(0f, 0.03f, -0.955f), new Vector3(0.22f, 0.05f, 0.04f));
        b.Pop();

        // 앞면 코인 간판
        b.AddBox(StylizedColor.ClothCream, new Vector3(0f, 0.4f, -0.458f), new Vector3(0.34f, 0.3f, 0.02f));
        b.Push(new Vector3(0f, 0.4f, -0.472f), Euler(-90f, 0f, 0f), new Vector3(1f / size.x, 1f, 1f / size.y));
        b.AddDisc(StylizedColor.Gold, Vector3.zero, 0.11f, 10);
        b.AddDisc(StylizedColor.Copper, new Vector3(0f, 0.004f, 0f), 0.065f, 10);
        b.Pop();
    }

    private static void BuildBinFlag(LowPolyMeshBuilder b)
    {
        // 바닥이 경첩 : 세우면 위로, 눕히면 앞으로
        b.AddBox(StylizedColor.IronDark, new Vector3(0f, 0.01f, 0f), new Vector3(0.05f, 0.05f, 0.05f));
        b.AddCylinder(StylizedColor.IronDark, Vector3.zero, 0.012f, 0.42f, 6);
        b.AddBox(StylizedColor.ClothRed, new Vector3(0.08f, 0.355f, 0f), new Vector3(0.15f, 0.1f, 0.014f));
        b.AddLowPolySphere(StylizedColor.Gold, new Vector3(0f, 0.43f, 0f), Vector3.one * 0.018f, 0, 0f, 1305);
    }

    // ---------------------------------------------------------------- 가판대 (1 상자)

    private static void BuildMarketStall(LowPolyMeshBuilder b)
    {
        Vector3 size = MarketStallSize;
        float counterTop = MarketCounterTopUnit;

        // 기둥 (앞 낮게, 뒤 높게)
        for (int x = -1; x <= 1; x += 2)
        {
            b.AddBox(StylizedColor.WoodDark, new Vector3(x * 0.47f, 0.4f, -0.44f), new Vector3(0.045f, 0.8f, 0.06f));
            b.AddBox(StylizedColor.WoodDark, new Vector3(x * 0.47f, 0.465f, 0.44f), new Vector3(0.045f, 0.93f, 0.06f));
            b.AddBox(StylizedColor.WoodDark, new Vector3(x * 0.47f, 0.2f, 0f), new Vector3(0.03f, 0.03f, 0.88f));
        }

        // 계산대
        b.AddBox(StylizedColor.WoodPlank, new Vector3(0f, (counterTop - 0.03f) * 0.5f, -0.3f), new Vector3(0.9f, counterTop - 0.03f, 0.28f));

        for (int index = -2; index <= 2; index++)
        {
            b.AddBox(StylizedColor.WoodDark, new Vector3(index * 0.18f, (counterTop - 0.03f) * 0.5f, -0.442f), new Vector3(0.012f, counterTop - 0.06f, 0.008f));
        }

        b.AddBox(StylizedColor.WoodLight, new Vector3(0f, counterTop - 0.015f, -0.3f), new Vector3(0.95f, 0.03f, 0.34f));

        // 계산대 위 물건 : 과일 상자 · 호박 · 생선 · 달걀 바구니
        AddProduceCrate(b, new Vector3(-0.3f, counterTop, -0.3f), StylizedColor.AppleRed, size, 1311);
        AddProduceCrate(b, new Vector3(0.06f, counterTop, -0.3f), StylizedColor.Tomato, size, 1321);
        AddScaledBall(b, StylizedColor.Pumpkin, new Vector3(0.33f, counterTop + 0.045f, -0.32f), 0.14f, size, 1331);
        AddScaledBall(b, StylizedColor.CropGreen, new Vector3(0.33f, counterTop + 0.095f, -0.32f), 0.025f, size, 1332);
        AddScaledBall(b, StylizedColor.Pumpkin, new Vector3(0.22f, counterTop + 0.03f, -0.24f), 0.1f, size, 1333);

        // 뒤쪽 짐 상자와 곡식 자루
        b.AddBox(StylizedColor.WoodLight, new Vector3(-0.36f, 0.075f, 0.3f), new Vector3(0.18f, 0.15f, 0.22f));
        b.AddBox(StylizedColor.WoodDark, new Vector3(-0.36f, 0.15f, 0.3f), new Vector3(0.185f, 0.012f, 0.225f));
        b.AddBox(StylizedColor.WoodLight, new Vector3(-0.36f, 0.2f, 0.32f), new Vector3(0.13f, 0.1f, 0.16f));
        AddScaledBall(b, StylizedColor.Grain, new Vector3(0.36f, 0.09f, 0.3f), 0.24f, size, 1341);
        AddScaledBall(b, StylizedColor.Straw, new Vector3(0.24f, 0.07f, 0.36f), 0.18f, size, 1342);

        // 줄무늬 차양 (뒤 높게, 앞으로 기울어짐)
        // 차양이 1 상자를 벗어나지 않게 깊이 0.98, 너비 1 로 만든다 (모델 크기 = 실제 크기)
        const float backY = 0.95f;
        const float frontY = 0.79f;
        const float depth = 0.98f;
        float slope = Mathf.Atan2(backY - frontY, depth) * Mathf.Rad2Deg;
        b.Push(new Vector3(0f, (backY + frontY) * 0.5f, 0f), Euler(-slope, 0f, 0f), Vector3.one);
        const int stripes = 8;
        float stripe = 1f / stripes;

        for (int index = 0; index < stripes; index++)
        {
            StylizedColor color = index % 2 == 0 ? StylizedColor.ClothRed : StylizedColor.ClothCream;
            float x = -0.5f + stripe * (index + 0.5f);
            b.AddBox(color, new Vector3(x, 0f, 0f), new Vector3(stripe, 0.018f, depth));
            // 앞쪽 물결 장식
            b.AddBox(color, new Vector3(x, -0.03f, -depth * 0.5f + 0.004f), new Vector3(stripe * 0.86f, 0.05f, 0.008f));
        }

        b.Pop();

        // 차양 위 간판 (뒤쪽)
        b.AddBox(StylizedColor.WoodDark, new Vector3(0f, 0.955f, 0.47f), new Vector3(0.36f, 0.09f, 0.02f));
        b.Push(new Vector3(0f, 0.955f, 0.457f), Euler(-90f, 0f, 0f), new Vector3(1f / size.x, 1f, 1f / size.y));
        b.AddDisc(StylizedColor.Gold, Vector3.zero, 0.1f, 10);
        b.AddDisc(StylizedColor.Copper, new Vector3(0f, 0.004f, 0f), 0.06f, 10);
        b.Pop();
    }

    private static void AddProduceCrate(LowPolyMeshBuilder b, Vector3 bottomCenter, StylizedColor produce, Vector3 size, int seed)
    {
        b.AddBox(StylizedColor.WoodLight, bottomCenter + new Vector3(0f, 0.025f, 0f), new Vector3(0.22f, 0.05f, 0.2f));
        b.AddBox(StylizedColor.WoodDark, bottomCenter + new Vector3(0f, 0.05f, 0f), new Vector3(0.225f, 0.008f, 0.205f));

        for (int index = 0; index < 5; index++)
        {
            float x = -0.07f + (index % 3) * 0.07f;
            float z = index < 3 ? -0.04f : 0.05f;
            AddScaledBall(b, produce, bottomCenter + new Vector3(x + (index < 3 ? 0f : 0.035f), 0.06f, z), 0.075f, size, seed + index);
        }
    }

    // 1 상자 모델 안에서도 실제 크기로 동그랗게 보이는 공 (반지름은 미터)
    private static void AddScaledBall(LowPolyMeshBuilder b, StylizedColor color, Vector3 unitCenter, float radiusMeters, Vector3 size, int seed)
    {
        Vector3 radii = new Vector3(radiusMeters / size.x, radiusMeters / size.y, radiusMeters / size.z);
        b.AddLowPolySphere(color, unitCenter, radii, 1, 0.04f, seed);
    }

    // ---------------------------------------------------------------- 떠돌이 상인 (미터, +Z 방향)

    private static void BuildMerchant(LowPolyMeshBuilder b)
    {
        // 장화와 바지
        b.AddBeveledBox(StylizedColor.Leather, new Vector3(0.1f, 0.07f, 0.03f), new Vector3(0.15f, 0.14f, 0.26f), 0.03f);
        b.AddBeveledBox(StylizedColor.Leather, new Vector3(-0.1f, 0.07f, 0.03f), new Vector3(0.15f, 0.14f, 0.26f), 0.03f);
        b.AddLimb(StylizedColor.WoodDark, new Vector3(0.1f, 0.12f, 0f), new Vector3(0.1f, 0.62f, 0f), 0.07f, 0.08f, 6);
        b.AddLimb(StylizedColor.WoodDark, new Vector3(-0.1f, 0.12f, 0f), new Vector3(-0.1f, 0.62f, 0f), 0.07f, 0.08f, 6);

        // 긴 외투 (아래로 넓게) · 허리띠 · 금 버클
        b.AddFrustum(StylizedColor.ClothGreen, new Vector3(0f, 0.36f, 0f), 0.3f, 0.25f, 0.5f, 8);
        b.AddBeveledBox(StylizedColor.ClothGreen, new Vector3(0f, 1.1f, 0f), new Vector3(0.5f, 0.56f, 0.3f), 0.08f);
        b.AddBox(StylizedColor.ClothCream, new Vector3(0f, 1.12f, 0.152f), new Vector3(0.12f, 0.5f, 0.01f));
        b.AddBox(StylizedColor.Leather, new Vector3(0f, 0.86f, 0f), new Vector3(0.52f, 0.08f, 0.32f));
        b.AddBox(StylizedColor.Gold, new Vector3(0f, 0.86f, 0.165f), new Vector3(0.08f, 0.07f, 0.02f));

        // 돈주머니 (오른쪽 허리)
        b.AddLowPolySphere(StylizedColor.Leather, new Vector3(0.27f, 0.76f, 0.08f), new Vector3(0.07f, 0.09f, 0.07f), 1, 0.05f, 1351);
        b.AddLowPolySphere(StylizedColor.Gold, new Vector3(0.27f, 0.84f, 0.08f), new Vector3(0.035f, 0.02f, 0.035f), 0, 0f, 1352);

        // 팔과 손
        b.AddLimb(StylizedColor.ClothGreen, new Vector3(0.3f, 1.32f, 0f), new Vector3(0.36f, 1.0f, 0.05f), 0.08f, 0.07f, 6);
        b.AddLimb(StylizedColor.Skin, new Vector3(0.36f, 1.0f, 0.05f), new Vector3(0.3f, 0.86f, 0.14f), 0.055f, 0.05f, 6);
        b.AddLimb(StylizedColor.ClothGreen, new Vector3(-0.3f, 1.32f, 0f), new Vector3(-0.36f, 1.0f, 0.05f), 0.08f, 0.07f, 6);
        b.AddLimb(StylizedColor.Skin, new Vector3(-0.36f, 1.0f, 0.05f), new Vector3(-0.3f, 0.86f, 0.14f), 0.055f, 0.05f, 6);
        b.AddLowPolySphere(StylizedColor.Skin, new Vector3(0.29f, 0.83f, 0.16f), Vector3.one * 0.06f, 0, 0f, 1353);
        b.AddLowPolySphere(StylizedColor.Skin, new Vector3(-0.29f, 0.83f, 0.16f), Vector3.one * 0.06f, 0, 0f, 1354);

        // 목도리 · 머리 · 수염
        b.AddTorus(StylizedColor.ClothRed, new Vector3(0f, 1.4f, 0f), 0.13f, 0.05f, 8, 4);
        b.AddBeveledBox(StylizedColor.Skin, new Vector3(0f, 1.6f, 0f), new Vector3(0.34f, 0.34f, 0.32f), 0.08f);
        b.AddBeveledBox(StylizedColor.White, new Vector3(0f, 1.5f, 0.13f), new Vector3(0.3f, 0.16f, 0.1f), 0.05f);
        b.AddBeveledBox(StylizedColor.White, new Vector3(0f, 1.42f, 0.14f), new Vector3(0.18f, 0.12f, 0.08f), 0.04f);
        b.AddBox(StylizedColor.White, new Vector3(0f, 1.575f, 0.165f), new Vector3(0.16f, 0.035f, 0.02f));
        b.AddLowPolySphere(StylizedColor.AppleRed, new Vector3(0f, 1.61f, 0.175f), Vector3.one * 0.035f, 0, 0f, 1355);
        b.AddBox(StylizedColor.Eye, new Vector3(0.075f, 1.66f, 0.162f), new Vector3(0.045f, 0.045f, 0.01f));
        b.AddBox(StylizedColor.Eye, new Vector3(-0.075f, 1.66f, 0.162f), new Vector3(0.045f, 0.045f, 0.01f));
        b.AddBox(StylizedColor.White, new Vector3(0.075f, 1.7f, 0.162f), new Vector3(0.07f, 0.02f, 0.01f));
        b.AddBox(StylizedColor.White, new Vector3(-0.075f, 1.7f, 0.162f), new Vector3(0.07f, 0.02f, 0.01f));

        // 챙 넓은 모자
        b.AddFrustum(StylizedColor.Leather, new Vector3(0f, 1.76f, 0f), 0.34f, 0.32f, 0.03f, 10);
        b.AddFrustum(StylizedColor.Leather, new Vector3(0f, 1.79f, 0f), 0.2f, 0.16f, 0.17f, 8);
        b.AddFrustum(StylizedColor.ClothRed, new Vector3(0f, 1.79f, 0f), 0.205f, 0.195f, 0.04f, 8, false, false);
        b.Push(new Vector3(0.16f, 1.86f, 0f), Euler(0f, 0f, -35f), Vector3.one);
        b.AddBox(StylizedColor.FlowerYellow, new Vector3(0f, 0.06f, 0f), new Vector3(0.02f, 0.14f, 0.04f));
        b.Pop();

        // 등짐 : 큰 배낭 · 말아 둔 천 · 냄비
        b.AddBeveledBox(StylizedColor.Leather, new Vector3(0f, 1.1f, -0.25f), new Vector3(0.44f, 0.56f, 0.22f), 0.05f);
        b.AddBox(StylizedColor.WoodDark, new Vector3(0.16f, 1.1f, -0.145f), new Vector3(0.04f, 0.5f, 0.02f));
        b.AddBox(StylizedColor.WoodDark, new Vector3(-0.16f, 1.1f, -0.145f), new Vector3(0.04f, 0.5f, 0.02f));
        b.Push(new Vector3(0f, 1.44f, -0.27f), Euler(0f, 0f, 90f), Vector3.one);
        b.AddCylinder(StylizedColor.ClothBlue, new Vector3(0f, -0.26f, 0f), 0.08f, 0.52f, 8);
        b.Pop();
        b.AddFrustum(StylizedColor.Copper, new Vector3(0.2f, 0.84f, -0.3f), 0.09f, 0.1f, 0.12f, 8);
        b.AddLimb(StylizedColor.IronDark, new Vector3(0.2f, 0.96f, -0.3f), new Vector3(0.2f, 1.02f, -0.3f), 0.012f, 0.012f, 4);
    }
}
