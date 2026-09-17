using UnityEngine;

// 86일차: 가축 모델 (닭·소), 우리 건축물 (닭장·외양간), 생산물·먹이·요리
// 동물·먹이통 사료·둥지 달걀·우유 통은 미터 단위, 우리 건축물은 가로·세로·높이 1 상자 안에 만든다.
public static partial class StylizedModelLibrary
{
    // 우리 건축물의 1 상자 기준 위치 (생성 도구가 실제 크기를 곱해 사료·생산물 위치를 맞춘다)
    public static readonly Vector3 CoopTroughUnit = new Vector3(-0.28f, 0.02f, -0.36f);
    public static readonly Vector3 CoopNestUnit = new Vector3(0.3f, 0.02f, 0.02f);
    public static readonly Vector3 BarnTroughUnit = new Vector3(-0.28f, 0.02f, -0.36f);
    public static readonly Vector3 BarnPailUnit = new Vector3(0.3f, 0.02f, -0.3f);
    public const float CoopHouseFrontUnit = 0.12f;
    public const float BarnShedFrontUnit = 0.16f;

    private static void RegisterLivestock()
    {
        Register("animal_chicken", FitMode.UniformHeight, BuildChicken);
        Register("animal_cow", FitMode.UniformHeight, BuildCow);
        Register("build_chicken_coop", FitMode.Stretch, BuildChickenCoop);
        Register("build_barn", FitMode.Stretch, BuildBarn);
        Register("fx_trough_feed", FitMode.UniformLargest, BuildTroughFeed);
        Register("fx_nest_eggs", FitMode.UniformLargest, BuildNestEggs);
        Register("fx_milk_pail", FitMode.UniformLargest, BuildMilkPail);
        Register("item_egg", FitMode.UniformLargest, BuildEggItem);
        Register("item_milk", FitMode.UniformLargest, BuildMilkBottle);
        Register("item_animal_feed", FitMode.UniformLargest, BuildFeedSack);
        Register("item_fried_egg", FitMode.UniformLargest, BuildFriedEgg);
        Register("item_veggie_omelette", FitMode.UniformLargest, BuildOmelette);
        Register("item_warm_milk", FitMode.UniformLargest, BuildWarmMilk);
    }

    // ---------------------------------------------------------------- 동물 (미터, 앞 방향 +Z)

    private static void BuildChicken(LowPolyMeshBuilder b)
    {
        b.AddLowPolySphere(StylizedColor.White, new Vector3(0f, 0.24f, 0f), new Vector3(0.13f, 0.12f, 0.17f), 1, 0.03f, 1201);
        b.Push(new Vector3(0f, 0.32f, -0.15f), Euler(-30f, 0f, 0f), Vector3.one);
        b.AddLowPolySphere(StylizedColor.White, Vector3.zero, new Vector3(0.05f, 0.1f, 0.05f), 1, 0.05f, 1202);
        b.Pop();
        b.AddLowPolySphere(StylizedColor.ClothCream, new Vector3(0f, 0.36f, -0.2f), new Vector3(0.035f, 0.06f, 0.03f), 0, 0f, 1203);

        // 날개
        b.AddLowPolySphere(StylizedColor.ClothCream, new Vector3(0.12f, 0.25f, -0.01f), new Vector3(0.03f, 0.07f, 0.11f), 1, 0f, 1204);
        b.AddLowPolySphere(StylizedColor.ClothCream, new Vector3(-0.12f, 0.25f, -0.01f), new Vector3(0.03f, 0.07f, 0.11f), 1, 0f, 1205);

        // 머리
        b.AddLowPolySphere(StylizedColor.White, new Vector3(0f, 0.38f, 0.12f), new Vector3(0.07f, 0.08f, 0.07f), 1, 0.02f, 1206);
        b.AddLowPolySphere(StylizedColor.ClothRed, new Vector3(0f, 0.465f, 0.1f), new Vector3(0.012f, 0.03f, 0.025f), 0, 0f, 1207);
        b.AddLowPolySphere(StylizedColor.ClothRed, new Vector3(0f, 0.47f, 0.14f), new Vector3(0.012f, 0.028f, 0.022f), 0, 0f, 1208);
        b.AddLowPolySphere(StylizedColor.ClothRed, new Vector3(0f, 0.33f, 0.175f), new Vector3(0.015f, 0.028f, 0.015f), 0, 0f, 1209);
        b.Push(new Vector3(0f, 0.375f, 0.18f), Euler(90f, 0f, 0f), Vector3.one);
        b.AddCone(StylizedColor.FlowerYellow, Vector3.zero, 0.02f, 0.05f, 5);
        b.Pop();
        b.AddLowPolySphere(StylizedColor.Black, new Vector3(0.05f, 0.4f, 0.15f), Vector3.one * 0.012f, 0, 0f, 1210);
        b.AddLowPolySphere(StylizedColor.Black, new Vector3(-0.05f, 0.4f, 0.15f), Vector3.one * 0.012f, 0, 0f, 1211);

        // 다리
        for (int side = -1; side <= 1; side += 2)
        {
            b.AddLimb(StylizedColor.FlowerYellow, new Vector3(side * 0.045f, 0.14f, 0f), new Vector3(side * 0.045f, 0.01f, 0.01f), 0.012f, 0.01f, 4);
            b.AddBox(StylizedColor.FlowerYellow, new Vector3(side * 0.045f, 0.008f, 0.03f), new Vector3(0.05f, 0.012f, 0.06f));
        }
    }

    private static void BuildCow(LowPolyMeshBuilder b)
    {
        b.AddBeveledBox(StylizedColor.White, new Vector3(0f, 0.82f, 0f), new Vector3(0.56f, 0.56f, 1.1f), 0.08f);

        // 무늬
        b.AddLowPolySphere(StylizedColor.Black, new Vector3(0.275f, 0.86f, 0.2f), new Vector3(0.02f, 0.15f, 0.2f), 1, 0.08f, 1221);
        b.AddLowPolySphere(StylizedColor.Black, new Vector3(-0.275f, 0.78f, -0.22f), new Vector3(0.02f, 0.14f, 0.22f), 1, 0.08f, 1222);
        b.AddLowPolySphere(StylizedColor.Black, new Vector3(0.08f, 1.1f, -0.12f), new Vector3(0.16f, 0.015f, 0.2f), 1, 0.08f, 1223);

        // 다리와 발굽
        for (int x = -1; x <= 1; x += 2)
        {
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 leg = new Vector3(x * 0.19f, 0.28f, z * 0.4f);
                b.AddBeveledBox(StylizedColor.White, leg, new Vector3(0.12f, 0.52f, 0.12f), 0.02f);
                b.AddBox(StylizedColor.Black, new Vector3(leg.x, 0.035f, leg.z), new Vector3(0.13f, 0.07f, 0.13f));
            }
        }

        // 머리
        b.AddBeveledBox(StylizedColor.White, new Vector3(0f, 1.02f, 0.66f), new Vector3(0.32f, 0.34f, 0.34f), 0.05f);
        b.AddBeveledBox(StylizedColor.CowPink, new Vector3(0f, 0.94f, 0.86f), new Vector3(0.3f, 0.18f, 0.12f), 0.04f);
        b.AddLowPolySphere(StylizedColor.Black, new Vector3(0.07f, 0.96f, 0.925f), Vector3.one * 0.018f, 0, 0f, 1224);
        b.AddLowPolySphere(StylizedColor.Black, new Vector3(-0.07f, 0.96f, 0.925f), Vector3.one * 0.018f, 0, 0f, 1225);
        b.AddLowPolySphere(StylizedColor.Black, new Vector3(0.165f, 1.08f, 0.76f), Vector3.one * 0.025f, 0, 0f, 1226);
        b.AddLowPolySphere(StylizedColor.Black, new Vector3(-0.165f, 1.08f, 0.76f), Vector3.one * 0.025f, 0, 0f, 1227);

        for (int side = -1; side <= 1; side += 2)
        {
            b.AddLimb(StylizedColor.Bone, new Vector3(side * 0.11f, 1.17f, 0.62f), new Vector3(side * 0.2f, 1.28f, 0.6f), 0.025f, 0.012f, 5);
            b.AddBeveledBox(StylizedColor.CowPink, new Vector3(side * 0.22f, 1.1f, 0.6f), new Vector3(0.14f, 0.05f, 0.08f), 0.015f);
        }

        // 젖과 꼬리
        b.AddLowPolySphere(StylizedColor.CowPink, new Vector3(0f, 0.52f, -0.2f), new Vector3(0.13f, 0.08f, 0.13f), 1, 0f, 1228);
        b.AddLimb(StylizedColor.White, new Vector3(0f, 1.02f, -0.55f), new Vector3(0f, 0.58f, -0.63f), 0.025f, 0.018f, 5);
        b.AddLowPolySphere(StylizedColor.Black, new Vector3(0f, 0.54f, -0.635f), new Vector3(0.04f, 0.06f, 0.04f), 0, 0.1f, 1229);
    }

    // ---------------------------------------------------------------- 우리 (1 상자)

    private static void AddPenFence(LowPolyMeshBuilder b, float postHeight, int rails, int postsPerSide, StylizedColor post, StylizedColor rail)
    {
        const float edge = 0.485f;

        for (int side = 0; side < 4; side++)
        {
            Quaternion yaw = Quaternion.Euler(0f, side * 90f, 0f);

            for (int index = 0; index < postsPerSide; index++)
            {
                float t = -edge + index * (edge * 2f / (postsPerSide - 1));
                b.Push(Vector3.zero, yaw, Vector3.one);
                b.AddBox(post, new Vector3(t, postHeight * 0.5f, -edge), new Vector3(0.03f, postHeight, 0.03f));
                b.Pop();
            }

            for (int index = 0; index < rails; index++)
            {
                float y = postHeight * (0.35f + index * (0.55f / Mathf.Max(1, rails - 1)));
                b.Push(Vector3.zero, yaw, Vector3.one);
                b.AddBox(rail, new Vector3(0f, y, -edge + 0.018f), new Vector3(edge * 2f, 0.035f, 0.012f));
                b.Pop();
            }
        }
    }

    // 박공 (앞면 삼각형 벽) : 바닥 중심, 밑변 너비, 높이
    private static void AddGable(LowPolyMeshBuilder b, StylizedColor color, Vector3 baseCenter, float width, float height)
    {
        b.Push(baseCenter + Vector3.up * height * 0.5f, Euler(0f, 90f, 0f), Vector3.one);
        b.AddWedge(color, Vector3.zero, new Vector3(0.012f, height, width));
        b.Pop();
    }

    private static void AddTrough(LowPolyMeshBuilder b, Vector3 center, float width)
    {
        b.AddBox(StylizedColor.WoodDark, center + new Vector3(0f, 0.03f, 0f), new Vector3(width, 0.05f, 0.09f));
        b.AddBox(StylizedColor.BarkDark, center + new Vector3(0f, 0.056f, 0f), new Vector3(width - 0.02f, 0.004f, 0.06f));
        b.AddBox(StylizedColor.WoodDark, center + new Vector3(-width * 0.5f + 0.01f, 0.012f, 0f), new Vector3(0.02f, 0.03f, 0.11f));
        b.AddBox(StylizedColor.WoodDark, center + new Vector3(width * 0.5f - 0.01f, 0.012f, 0f), new Vector3(0.02f, 0.03f, 0.11f));
    }

    private static void BuildChickenCoop(LowPolyMeshBuilder b)
    {
        // 바닥 흙과 짚
        b.AddBox(StylizedColor.Dirt, new Vector3(0f, 0.008f, 0f), new Vector3(0.99f, 0.016f, 0.99f));
        b.AddBox(StylizedColor.Straw, new Vector3(0.05f, 0.018f, -0.18f), new Vector3(0.5f, 0.006f, 0.3f));
        AddPenFence(b, 0.36f, 2, 6, StylizedColor.WoodPlank, StylizedColor.WoodLight);

        // 뒤쪽 닭집 (다리 위)
        float front = CoopHouseFrontUnit;
        for (int x = -1; x <= 1; x += 2)
        {
            b.AddBox(StylizedColor.WoodDark, new Vector3(x * 0.2f - 0.08f, 0.1f, front + 0.03f), new Vector3(0.03f, 0.2f, 0.03f));
            b.AddBox(StylizedColor.WoodDark, new Vector3(x * 0.2f - 0.08f, 0.1f, 0.44f), new Vector3(0.03f, 0.2f, 0.03f));
        }

        b.AddBeveledBox(StylizedColor.WoodPlank, new Vector3(-0.08f, 0.44f, 0.29f), new Vector3(0.56f, 0.46f, 0.34f), 0.02f);
        b.AddBox(StylizedColor.WoodDark, new Vector3(-0.08f, 0.22f, 0.29f), new Vector3(0.6f, 0.03f, 0.38f));
        b.AddBox(StylizedColor.BarkDark, new Vector3(-0.08f, 0.38f, front - 0.001f), new Vector3(0.12f, 0.2f, 0.01f));
        b.AddBox(StylizedColor.WoodLight, new Vector3(-0.08f, 0.56f, front - 0.002f), new Vector3(0.09f, 0.07f, 0.008f));

        // 경사로
        b.Push(new Vector3(-0.08f, 0.12f, front - 0.1f), Euler(-38f, 0f, 0f), Vector3.one);
        b.AddBox(StylizedColor.WoodLight, Vector3.zero, new Vector3(0.1f, 0.012f, 0.26f));
        b.Pop();

        // 지붕 (꼭대기 1.0)
        for (int side = -1; side <= 1; side += 2)
        {
            b.Push(new Vector3(-0.08f + side * 0.16f, 0.83f, 0.29f), Euler(0f, 0f, -side * 38f), Vector3.one);
            b.AddBox(StylizedColor.ClothRed, Vector3.zero, new Vector3(0.44f, 0.035f, 0.42f));
            b.Pop();
        }

        AddGable(b, StylizedColor.WoodPlank, new Vector3(-0.08f, 0.67f, front - 0.004f), 0.54f, 0.29f);
        b.AddBox(StylizedColor.WoodDark, new Vector3(-0.08f, 0.985f, 0.29f), new Vector3(0.04f, 0.03f, 0.44f));

        // 둥지 상자와 먹이통
        Vector3 nest = CoopNestUnit;
        b.AddBox(StylizedColor.WoodDark, nest + new Vector3(0f, 0.04f, 0f), new Vector3(0.18f, 0.08f, 0.14f));
        b.AddBox(StylizedColor.Straw, nest + new Vector3(0f, 0.082f, 0f), new Vector3(0.15f, 0.006f, 0.11f));
        AddTrough(b, CoopTroughUnit, 0.24f);
    }

    private static void BuildBarn(LowPolyMeshBuilder b)
    {
        b.AddBox(StylizedColor.Dirt, new Vector3(0f, 0.008f, 0f), new Vector3(0.99f, 0.016f, 0.99f));
        b.AddBox(StylizedColor.Straw, new Vector3(-0.1f, 0.018f, -0.1f), new Vector3(0.55f, 0.006f, 0.4f));
        AddPenFence(b, 0.44f, 3, 7, StylizedColor.WoodDark, StylizedColor.WoodPlank);

        // 뒤쪽 외양간 건물
        float front = BarnShedFrontUnit;
        float depth = 0.48f - front;
        float centerZ = front + depth * 0.5f;
        b.AddBeveledBox(StylizedColor.ClothRed, new Vector3(0f, 0.29f, centerZ), new Vector3(0.96f, 0.58f, depth), 0.015f);
        b.AddBox(StylizedColor.BarkDark, new Vector3(0f, 0.24f, front - 0.001f), new Vector3(0.36f, 0.46f, 0.01f));
        b.AddBox(StylizedColor.White, new Vector3(0f, 0.475f, front - 0.004f), new Vector3(0.4f, 0.03f, 0.01f));
        b.AddBox(StylizedColor.White, new Vector3(-0.19f, 0.24f, front - 0.004f), new Vector3(0.025f, 0.46f, 0.01f));
        b.AddBox(StylizedColor.White, new Vector3(0.19f, 0.24f, front - 0.004f), new Vector3(0.025f, 0.46f, 0.01f));

        for (int side = -1; side <= 1; side += 2)
        {
            b.Push(new Vector3(0f, 0.24f, front - 0.004f), Euler(0f, 0f, side * 38f), Vector3.one);
            b.AddBox(StylizedColor.White, Vector3.zero, new Vector3(0.025f, 0.56f, 0.01f));
            b.Pop();
            b.AddBox(StylizedColor.White, new Vector3(side * 0.37f, 0.5f, front - 0.004f), new Vector3(0.1f, 0.08f, 0.01f));
        }

        // 지붕 (꼭대기 1.0)
        for (int side = -1; side <= 1; side += 2)
        {
            b.Push(new Vector3(side * 0.25f, 0.79f, centerZ), Euler(0f, 0f, -side * 40f), Vector3.one);
            b.AddBox(StylizedColor.StoneDark, Vector3.zero, new Vector3(0.62f, 0.035f, depth + 0.06f));
            b.Pop();
        }

        AddGable(b, StylizedColor.ClothRed, new Vector3(0f, 0.58f, front + 0.004f), 0.8f, 0.36f);
        b.AddBox(StylizedColor.BarkDark, new Vector3(0f, 0.985f, centerZ), new Vector3(0.05f, 0.03f, depth + 0.08f));

        // 건초 더미, 먹이통, 우유 통 자리
        b.AddBeveledBox(StylizedColor.Straw, new Vector3(-0.36f, 0.06f, 0.04f), new Vector3(0.18f, 0.1f, 0.12f), 0.01f);
        b.AddBeveledBox(StylizedColor.Straw, new Vector3(-0.36f, 0.16f, 0.05f), new Vector3(0.16f, 0.09f, 0.11f), 0.01f);
        b.AddBox(StylizedColor.Rope, new Vector3(-0.36f, 0.06f, 0.04f), new Vector3(0.185f, 0.1f, 0.012f));
        AddTrough(b, BarnTroughUnit, 0.3f);
        b.AddBox(StylizedColor.WoodDark, BarnPailUnit + new Vector3(0f, 0.005f, 0f), new Vector3(0.12f, 0.01f, 0.12f));
    }

    // ---------------------------------------------------------------- 우리 소품 (미터)

    private static void BuildTroughFeed(LowPolyMeshBuilder b)
    {
        for (int index = 0; index < 5; index++)
        {
            float x = -0.24f + index * 0.12f;
            b.AddLowPolySphere(index % 2 == 0 ? StylizedColor.Grain : StylizedColor.Straw, new Vector3(x, 0.02f, 0f), new Vector3(0.08f, 0.025f, 0.05f), 1, 0.2f, 1240 + index);
        }
    }

    private static void BuildNestEggs(LowPolyMeshBuilder b)
    {
        b.AddLowPolySphere(StylizedColor.Straw, new Vector3(0f, 0.02f, 0f), new Vector3(0.14f, 0.025f, 0.11f), 1, 0.2f, 1250);
        Vector3[] eggs = { new Vector3(-0.05f, 0.06f, 0.01f), new Vector3(0.04f, 0.06f, -0.02f), new Vector3(0.01f, 0.065f, 0.05f) };

        for (int index = 0; index < eggs.Length; index++)
        {
            b.AddLowPolySphere(StylizedColor.Egg, eggs[index], new Vector3(0.035f, 0.045f, 0.035f), 1, 0f, 1251 + index);
        }
    }

    private static void BuildMilkPail(LowPolyMeshBuilder b)
    {
        b.AddFrustum(StylizedColor.Iron, Vector3.zero, 0.11f, 0.14f, 0.26f, 12, true, false);
        b.AddDisc(StylizedColor.White, new Vector3(0f, 0.23f, 0f), 0.135f, 12);
        b.AddTorus(StylizedColor.IronDark, new Vector3(0f, 0.26f, 0f), 0.14f, 0.01f, 12, 3);
        b.AddTorus(StylizedColor.IronDark, new Vector3(0f, 0.08f, 0f), 0.118f, 0.008f, 12, 3);
        b.Push(new Vector3(0f, 0.26f, 0f), Euler(0f, 0f, 90f), Vector3.one);
        b.AddTorus(StylizedColor.IronDark, Vector3.zero, 0.13f, 0.006f, 12, 3);
        b.Pop();
    }

    // ---------------------------------------------------------------- 아이템

    private static void BuildEggItem(LowPolyMeshBuilder b)
    {
        b.AddLowPolySphere(StylizedColor.Straw, new Vector3(0f, 0.03f, 0f), new Vector3(0.2f, 0.035f, 0.17f), 1, 0.25f, 1261);
        b.AddLowPolySphere(StylizedColor.Egg, new Vector3(-0.06f, 0.13f, 0f), new Vector3(0.075f, 0.095f, 0.075f), 1, 0f, 1262);
        b.Push(new Vector3(0.07f, 0.1f, 0.03f), Euler(0f, 0f, -60f), Vector3.one);
        b.AddLowPolySphere(StylizedColor.Egg, Vector3.zero, new Vector3(0.07f, 0.09f, 0.07f), 1, 0f, 1263);
        b.Pop();
    }

    private static void BuildMilkBottle(LowPolyMeshBuilder b)
    {
        b.AddFrustum(StylizedColor.White, Vector3.zero, 0.12f, 0.12f, 0.3f, 12);
        b.AddFrustum(StylizedColor.White, new Vector3(0f, 0.3f, 0f), 0.12f, 0.06f, 0.08f, 12, false, false);
        b.AddFrustum(StylizedColor.White, new Vector3(0f, 0.38f, 0f), 0.06f, 0.06f, 0.05f, 12, false, false);
        b.AddFrustum(StylizedColor.ClothBlue, new Vector3(0f, 0.43f, 0f), 0.068f, 0.068f, 0.035f, 12);
        b.AddFrustum(StylizedColor.ClothBlue, new Vector3(0f, 0.1f, 0f), 0.123f, 0.123f, 0.1f, 12, false, false);
        b.AddBox(StylizedColor.White, new Vector3(0f, 0.15f, 0.124f), new Vector3(0.08f, 0.04f, 0.004f));
    }

    private static void BuildFeedSack(LowPolyMeshBuilder b)
    {
        b.AddLowPolySphere(StylizedColor.Sand, new Vector3(0f, 0.2f, 0f), new Vector3(0.2f, 0.2f, 0.16f), 1, 0.08f, 1271);
        b.AddFrustum(StylizedColor.Sand, new Vector3(0f, 0.36f, 0f), 0.1f, 0.13f, 0.08f, 7, false, true);
        b.AddLowPolySphere(StylizedColor.Grain, new Vector3(0f, 0.445f, 0f), new Vector3(0.1f, 0.025f, 0.1f), 1, 0.2f, 1272);
        b.AddTorus(StylizedColor.Rope, new Vector3(0f, 0.37f, 0f), 0.1f, 0.015f, 8, 3);
        b.AddBox(StylizedColor.ClothGreen, new Vector3(0f, 0.2f, 0.158f), new Vector3(0.14f, 0.12f, 0.005f));

        for (int index = 0; index < 4; index++)
        {
            b.AddLowPolySphere(StylizedColor.Grain, new Vector3(0.18f + index * 0.05f, 0.012f, 0.1f - index * 0.04f), new Vector3(0.035f, 0.012f, 0.03f), 0, 0.2f, 1273 + index);
        }
    }

    private static void AddFriedEgg(LowPolyMeshBuilder b, Vector3 center, float scale, int seed)
    {
        b.AddLowPolySphere(StylizedColor.White, center, new Vector3(0.12f, 0.012f, 0.1f) * scale, 1, 0.12f, seed);
        b.AddLowPolySphere(StylizedColor.Yolk, center + new Vector3(0.01f, 0.018f, 0f) * scale, new Vector3(0.045f, 0.028f, 0.045f) * scale, 1, 0f, seed + 1);
    }

    private static void BuildFriedEgg(LowPolyMeshBuilder b)
    {
        AddPlate(b, 0.3f, StylizedColor.WoodLight);
        AddFriedEgg(b, new Vector3(-0.07f, 0.045f, 0.02f), 1f, 1281);
        AddFriedEgg(b, new Vector3(0.09f, 0.045f, -0.04f), 0.9f, 1283);
        b.AddLowPolySphere(StylizedColor.Herb, new Vector3(0.12f, 0.05f, 0.14f), new Vector3(0.04f, 0.008f, 0.02f), 0, 0f, 1285);
        b.AddLowPolySphere(StylizedColor.Black, new Vector3(-0.05f, 0.08f, 0.0f), Vector3.one * 0.006f, 0, 0f, 1286);
        b.AddLowPolySphere(StylizedColor.Black, new Vector3(0.1f, 0.075f, -0.03f), Vector3.one * 0.006f, 0, 0f, 1287);
        AddSteam(b, new Vector3(0f, 0.14f, 0f), 1288);
    }

    private static void BuildOmelette(LowPolyMeshBuilder b)
    {
        AddPlate(b, 0.32f, StylizedColor.WoodLight);
        b.Push(new Vector3(0f, 0.035f, 0f), Euler(0f, 15f, 0f), Vector3.one);
        b.AddLowPolySphere(StylizedColor.Yolk, new Vector3(0f, 0.045f, 0f), new Vector3(0.22f, 0.05f, 0.12f), 1, 0.04f, 1291);
        b.AddLowPolySphere(StylizedColor.Caramel, new Vector3(0.02f, 0.085f, 0.02f), new Vector3(0.12f, 0.012f, 0.05f), 0, 0.1f, 1292);
        b.Pop();
        b.AddLowPolySphere(StylizedColor.Tomato, new Vector3(-0.14f, 0.05f, 0.14f), new Vector3(0.04f, 0.02f, 0.04f), 1, 0f, 1293);
        b.AddLowPolySphere(StylizedColor.Tomato, new Vector3(-0.05f, 0.05f, 0.17f), new Vector3(0.035f, 0.02f, 0.035f), 1, 0f, 1294);
        b.AddLowPolySphere(StylizedColor.MushroomCap, new Vector3(0.15f, 0.05f, -0.13f), new Vector3(0.04f, 0.022f, 0.035f), 1, 0f, 1295);
        b.AddLowPolySphere(StylizedColor.Herb, new Vector3(0.05f, 0.13f, 0.01f), new Vector3(0.03f, 0.008f, 0.015f), 0, 0f, 1296);
        b.AddLowPolySphere(StylizedColor.Herb, new Vector3(-0.06f, 0.125f, -0.02f), new Vector3(0.028f, 0.008f, 0.014f), 0, 0f, 1297);
        AddSteam(b, new Vector3(0.02f, 0.18f, 0f), 1298);
    }

    private static void BuildWarmMilk(LowPolyMeshBuilder b)
    {
        b.AddFrustum(StylizedColor.WoodLight, new Vector3(0f, -0.02f, 0f), 0.19f, 0.19f, 0.02f, 12);
        b.AddFrustum(StylizedColor.ClothBlue, Vector3.zero, 0.1f, 0.12f, 0.24f, 12, true, false);
        b.AddDisc(StylizedColor.White, new Vector3(0f, 0.22f, 0f), 0.115f, 12);
        b.AddTorus(StylizedColor.ClothBlue, new Vector3(0f, 0.24f, 0f), 0.12f, 0.012f, 12, 3);
        b.Push(new Vector3(0.14f, 0.12f, 0f), Euler(90f, 0f, 0f), Vector3.one);
        b.AddTorus(StylizedColor.ClothBlue, Vector3.zero, 0.05f, 0.016f, 8, 3);
        b.Pop();
        b.AddLowPolySphere(StylizedColor.Caramel, new Vector3(0.02f, 0.225f, 0.02f), new Vector3(0.03f, 0.004f, 0.03f), 0, 0f, 1301);
        AddSteam(b, new Vector3(-0.02f, 0.3f, 0f), 1302);
    }
}
