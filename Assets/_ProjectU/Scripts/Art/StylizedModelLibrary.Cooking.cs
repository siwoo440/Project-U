using UnityEngine;

// 85일차: 요리 음식 모델 (접시·그릇에 담긴 요리)
public static partial class StylizedModelLibrary
{
    private static void RegisterCooking()
    {
        Register("item_baked_potato", FitMode.UniformLargest, BuildBakedPotato);
        Register("item_grilled_fish", FitMode.UniformLargest, BuildGrilledFish);
        Register("item_mushroom_skewer", FitMode.UniformLargest, BuildMushroomSkewer);
        Register("item_pumpkin_soup", FitMode.UniformLargest, BuildPumpkinSoup);
        Register("item_tomato_stew", FitMode.UniformLargest, BuildTomatoStew);
        Register("item_golden_feast", FitMode.UniformLargest, BuildGoldenFeast);
    }

    private static void AddPlate(LowPolyMeshBuilder b, float radius, StylizedColor color)
    {
        b.AddFrustum(color, Vector3.zero, radius * 0.86f, radius, 0.035f, 14);
        b.AddTorus(StylizedColor.WoodDark, new Vector3(0f, 0.035f, 0f), radius * 0.97f, 0.012f, 14, 3);
    }

    // 깊은 그릇 : 바닥 0, 국물 윗면 높이 BowlSoupHeight
    private const float BowlSoupHeight = 0.25f;

    private static void AddBowl(LowPolyMeshBuilder b, StylizedColor soup)
    {
        b.AddFrustum(StylizedColor.WoodDark, Vector3.zero, 0.1f, 0.12f, 0.04f, 14, true, false);
        b.AddFrustum(StylizedColor.WoodPlank, new Vector3(0f, 0.04f, 0f), 0.13f, 0.21f, 0.12f, 14, false, false);
        b.AddFrustum(StylizedColor.WoodPlank, new Vector3(0f, 0.16f, 0f), 0.21f, 0.25f, 0.1f, 14, false, false);
        b.AddTorus(StylizedColor.WoodDark, new Vector3(0f, 0.17f, 0f), 0.215f, 0.012f, 14, 3);
        b.AddDisc(soup, new Vector3(0f, BowlSoupHeight, 0f), 0.24f, 14);
        b.AddTorus(StylizedColor.WoodLight, new Vector3(0f, BowlSoupHeight + 0.008f, 0f), 0.246f, 0.02f, 14, 4);
    }

    private static void AddSteam(LowPolyMeshBuilder b, Vector3 start, int seed)
    {
        b.AddLowPolySphere(StylizedColor.Snow, start, Vector3.one * 0.026f, 1, 0.05f, seed);
        b.AddLowPolySphere(StylizedColor.Snow, start + new Vector3(0.025f, 0.06f, -0.01f), Vector3.one * 0.02f, 1, 0.05f, seed + 1);
        b.AddLowPolySphere(StylizedColor.Snow, start + new Vector3(0f, 0.11f, 0.01f), Vector3.one * 0.014f, 1, 0.05f, seed + 2);
    }

    // 반으로 가른 구운 감자와 버터
    private static void BuildBakedPotato(LowPolyMeshBuilder b)
    {
        AddPlate(b, 0.3f, StylizedColor.WoodLight);
        b.AddLowPolySphere(StylizedColor.Potato, new Vector3(0f, 0.12f, 0f), new Vector3(0.22f, 0.1f, 0.15f), 1, 0.06f, 1101);
        b.AddLowPolySphere(StylizedColor.Charred, new Vector3(0.12f, 0.17f, 0.08f), new Vector3(0.04f, 0.015f, 0.03f), 0, 0f, 1102);
        b.AddLowPolySphere(StylizedColor.Charred, new Vector3(-0.14f, 0.15f, -0.08f), new Vector3(0.035f, 0.015f, 0.03f), 0, 0f, 1103);
        // 갈라진 속살과 버터
        b.AddLowPolySphere(StylizedColor.ClothCream, new Vector3(0f, 0.2f, 0f), new Vector3(0.15f, 0.03f, 0.06f), 1, 0.05f, 1104);
        b.AddBeveledBox(StylizedColor.Butter, new Vector3(0.01f, 0.235f, 0f), new Vector3(0.07f, 0.035f, 0.06f), 0.008f);
        b.AddLowPolySphere(StylizedColor.Herb, new Vector3(-0.05f, 0.225f, 0.03f), new Vector3(0.02f, 0.006f, 0.012f), 0, 0f, 1105);
        b.AddLowPolySphere(StylizedColor.Herb, new Vector3(0.06f, 0.225f, -0.02f), new Vector3(0.018f, 0.006f, 0.01f), 0, 0f, 1106);
        AddSteam(b, new Vector3(-0.04f, 0.3f, 0f), 1107);
    }

    // 꼬치에 꽂아 구운 물고기
    private static void BuildGrilledFish(LowPolyMeshBuilder b)
    {
        AddPlate(b, 0.32f, StylizedColor.WoodLight);
        b.Push(new Vector3(0f, 0.035f, 0f), Euler(0f, 20f, 0f), Vector3.one);
        BuildFish(b, StylizedColor.GrilledFish, StylizedColor.Caramel, StylizedColor.Charred, 0.24f, 0.06f, 0.1f, FishDetail.None, 1111);

        // 굽기 자국
        for (int index = 0; index < 4; index++)
        {
            float x = -0.12f + index * 0.08f;
            b.Push(new Vector3(x, 0.114f, 0f), Euler(0f, 25f, 0f), Vector3.one);
            b.AddBox(StylizedColor.Charred, Vector3.zero, new Vector3(0.016f, 0.008f, 0.12f));
            b.Pop();
        }

        b.AddLimb(StylizedColor.WoodLight, new Vector3(-0.46f, 0.065f, 0f), new Vector3(0.4f, 0.065f, 0f), 0.01f, 0.007f, 4);
        b.Pop();
        // 레몬 조각 대신 허브 잎
        b.AddLowPolySphere(StylizedColor.Leaf, new Vector3(0.18f, 0.05f, 0.16f), new Vector3(0.06f, 0.012f, 0.03f), 0, 0f, 1112);
        b.AddLowPolySphere(StylizedColor.LeafLight, new Vector3(0.2f, 0.055f, 0.19f), new Vector3(0.04f, 0.01f, 0.02f), 0, 0f, 1113);
    }

    // 버섯 세 개를 꽂은 꼬치 (+X 방향으로 누워 있음)
    private static void BuildMushroomSkewer(LowPolyMeshBuilder b)
    {
        b.AddLimb(StylizedColor.WoodLight, new Vector3(-0.42f, 0.07f, 0f), new Vector3(0.4f, 0.07f, 0f), 0.012f, 0.008f, 5);

        for (int index = 0; index < 3; index++)
        {
            float x = -0.18f + index * 0.19f;
            Vector3 center = new Vector3(x, 0.08f, 0f);
            b.Push(center, Euler(0f, 0f, 90f), Vector3.one);
            b.AddFrustum(StylizedColor.MushroomStem, new Vector3(0f, -0.07f, 0f), 0.04f, 0.035f, 0.06f, 6);
            b.AddFrustum(StylizedColor.Caramel, new Vector3(0f, -0.01f, 0f), 0.1f, 0.075f, 0.035f, 8, true, false);
            b.AddFrustum(StylizedColor.MushroomCap, new Vector3(0f, 0.025f, 0f), 0.075f, 0f, 0.05f, 8, false, false);
            b.Pop();
            b.AddLowPolySphere(StylizedColor.Charred, center + new Vector3(0.05f, 0.05f, 0.03f), new Vector3(0.012f, 0.01f, 0.02f), 0, 0f, 1120 + index);
        }

        b.AddLowPolySphere(StylizedColor.Herb, new Vector3(-0.3f, 0.09f, 0.02f), new Vector3(0.03f, 0.01f, 0.015f), 0, 0f, 1125);
    }

    // 크림을 두른 호박 수프
    private static void BuildPumpkinSoup(LowPolyMeshBuilder b)
    {
        AddBowl(b, StylizedColor.SoupPumpkin);
        float top = BowlSoupHeight;
        b.AddTorus(StylizedColor.ClothCream, new Vector3(0.02f, top + 0.004f, 0f), 0.08f, 0.012f, 12, 3);
        b.AddLowPolySphere(StylizedColor.ClothCream, new Vector3(0.02f, top + 0.005f, 0f), new Vector3(0.03f, 0.006f, 0.03f), 0, 0f, 1131);
        b.AddLowPolySphere(StylizedColor.Leaf, new Vector3(-0.1f, top + 0.008f, 0.06f), new Vector3(0.04f, 0.008f, 0.02f), 0, 0f, 1132);

        for (int index = 0; index < 4; index++)
        {
            float angle = index * 1.7f;
            b.AddLowPolySphere(StylizedColor.ClothCream, new Vector3(Mathf.Cos(angle) * 0.14f, top + 0.006f, Mathf.Sin(angle) * 0.14f), new Vector3(0.016f, 0.005f, 0.009f), 0, 0f, 1133 + index);
        }

        AddSteam(b, new Vector3(0.06f, top + 0.06f, -0.04f), 1140);
    }

    // 감자 조각이 든 토마토 스튜
    private static void BuildTomatoStew(LowPolyMeshBuilder b)
    {
        AddBowl(b, StylizedColor.SoupTomato);
        float top = BowlSoupHeight;
        Vector3[] chunks =
        {
            new Vector3(0.08f, top + 0.012f, 0.05f), new Vector3(-0.09f, top + 0.012f, 0.02f), new Vector3(0.01f, top + 0.012f, -0.1f), new Vector3(-0.02f, top + 0.012f, 0.12f)
        };

        for (int index = 0; index < chunks.Length; index++)
        {
            b.Push(chunks[index], Euler(0f, index * 37f, 0f), Vector3.one);
            b.AddBeveledBox(StylizedColor.Potato, Vector3.zero, new Vector3(0.05f, 0.03f, 0.045f), 0.008f);
            b.Pop();
        }

        b.AddLowPolySphere(StylizedColor.Tomato, new Vector3(0.1f, top + 0.01f, -0.06f), new Vector3(0.035f, 0.015f, 0.03f), 0, 0f, 1151);
        b.AddLowPolySphere(StylizedColor.Herb, new Vector3(-0.04f, top + 0.02f, -0.03f), new Vector3(0.025f, 0.006f, 0.012f), 0, 0f, 1152);
        b.AddLowPolySphere(StylizedColor.Herb, new Vector3(0.03f, top + 0.02f, 0.07f), new Vector3(0.022f, 0.006f, 0.011f), 0, 0f, 1153);
        AddSteam(b, new Vector3(-0.05f, top + 0.06f, 0.04f), 1160);
    }

    // 큰 접시 위 황금잉어 구이와 곁들이 채소
    private static void BuildGoldenFeast(LowPolyMeshBuilder b)
    {
        b.AddFrustum(StylizedColor.ClothCream, Vector3.zero, 0.34f, 0.4f, 0.04f, 16);
        b.AddTorus(StylizedColor.Gold, new Vector3(0f, 0.04f, 0f), 0.385f, 0.014f, 16, 3);
        b.Push(new Vector3(-0.02f, 0.04f, 0.04f), Euler(0f, 10f, 0f), Vector3.one);
        BuildFish(b, StylizedColor.FishGold, StylizedColor.Caramel, StylizedColor.AppleRed, 0.26f, 0.065f, 0.12f, FishDetail.Whiskers, 1171);

        for (int index = 0; index < 3; index++)
        {
            b.Push(new Vector3(-0.1f + index * 0.09f, 0.128f, 0.01f), Euler(0f, 25f, 0f), Vector3.one);
            b.AddBox(StylizedColor.Charred, Vector3.zero, new Vector3(0.012f, 0.006f, 0.1f));
            b.Pop();
        }

        b.Pop();

        // 무 조각과 토마토 반쪽
        for (int index = 0; index < 3; index++)
        {
            float x = -0.22f + index * 0.1f;
            b.AddFrustum(StylizedColor.White, new Vector3(x, 0.04f, -0.24f + index * 0.02f), 0.045f, 0.045f, 0.018f, 8);
        }

        AddTomato(b, new Vector3(0.24f, 0.03f, -0.18f), 0.06f, 1180);
        AddTomato(b, new Vector3(0.28f, 0.03f, -0.06f), 0.05f, 1190);
        b.AddLowPolySphere(StylizedColor.Leaf, new Vector3(-0.25f, 0.05f, 0.2f), new Vector3(0.07f, 0.014f, 0.035f), 0, 0f, 1195);
        b.AddLowPolySphere(StylizedColor.LeafLight, new Vector3(-0.2f, 0.055f, 0.24f), new Vector3(0.05f, 0.012f, 0.025f), 0, 0f, 1196);
        AddSteam(b, new Vector3(0.02f, 0.2f, 0.02f), 1197);
    }
}
