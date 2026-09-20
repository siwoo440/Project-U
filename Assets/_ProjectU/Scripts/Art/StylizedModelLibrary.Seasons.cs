using UnityEngine;

// 119일차: 계절 채집물 · 과일나무 · 새 작물 · 절구 모델
// 채집물과 아이템은 작게(손에 드는 크기), 나무와 절구는 미터 단위로 만든다.
public static partial class StylizedModelLibrary
{
    private static void RegisterSeasons()
    {
        // 봄 채집물
        Register("resource_wild_greens", FitMode.UniformLargest, BuildWildGreens);
        Register("resource_wild_flower", FitMode.UniformLargest, BuildWildFlower);
        Register("food_bamboo_shoot", FitMode.UniformLargest, BuildBambooShoot);

        // 여름 채집물
        Register("food_raspberry", FitMode.UniformLargest, BuildRaspberry);
        Register("resource_herb_leaf", FitMode.UniformLargest, BuildHerbLeaf);
        Register("resource_bamboo", FitMode.UniformLargest, BuildBambooStick);

        // 가을 채집물
        Register("food_chestnut", FitMode.UniformLargest, BuildChestnut);
        Register("food_big_mushroom", FitMode.UniformLargest, BuildBigMushroom);
        Register("resource_acorn", FitMode.UniformLargest, BuildAcorn);

        // 겨울 채집물
        Register("resource_dry_branch", FitMode.UniformLargest, BuildDryBranch);
        Register("resource_ice_flower", FitMode.UniformLargest, BuildIceFlower);
        Register("resource_pine_cone", FitMode.UniformLargest, BuildPineCone);

        // 계절 요리 · 약
        Register("food_greens_salad", FitMode.UniformLargest, BuildGreensSalad);
        Register("food_bamboo_stirfry", FitMode.UniformLargest, BuildBambooStirfry);
        Register("food_berry_drink", FitMode.UniformLargest, BuildBerryDrink);
        Register("food_roast_chestnut", FitMode.UniformLargest, BuildRoastChestnut);
        Register("food_mushroom_hotpot", FitMode.UniformLargest, BuildMushroomHotpot);
        Register("medicine_winter_tonic", FitMode.UniformLargest, BuildWinterTonic);

        // 과일 · 묘목
        Register("food_plum", FitMode.UniformLargest, b => BuildRoundFruit(b, StylizedColor.BerryPurple, 0.075f, 4101));
        Register("food_persimmon", FitMode.UniformLargest, b => BuildRoundFruit(b, StylizedColor.Pumpkin, 0.085f, 4102));
        Register("resource_sapling_apple", FitMode.UniformHeight, b => BuildSaplingItem(b, StylizedColor.Leaf));
        Register("resource_sapling_plum", FitMode.UniformHeight, b => BuildSaplingItem(b, StylizedColor.LeafDark));
        Register("resource_sapling_persimmon", FitMode.UniformHeight, b => BuildSaplingItem(b, StylizedColor.LeafAutumn));

        // 과일나무 (자라는 단계)
        Register("tree_sapling", FitMode.UniformHeight, BuildTreeSapling);
        Register("tree_young", FitMode.UniformHeight, BuildTreeYoung);
        Register("tree_apple_mature", FitMode.UniformHeight, b => BuildFruitTreeMature(b, StylizedColor.Leaf, 4201));
        Register("tree_plum_mature", FitMode.UniformHeight, b => BuildFruitTreeMature(b, StylizedColor.LeafDark, 4202));
        Register("tree_persimmon_mature", FitMode.UniformHeight, b => BuildFruitTreeMature(b, StylizedColor.LeafAutumn, 4203));
        Register("tree_apple_fruit", FitMode.UniformHeight, b => BuildTreeFruitCluster(b, StylizedColor.AppleRed, 4211));
        Register("tree_plum_fruit", FitMode.UniformHeight, b => BuildTreeFruitCluster(b, StylizedColor.BerryPurple, 4212));
        Register("tree_persimmon_fruit", FitMode.UniformHeight, b => BuildTreeFruitCluster(b, StylizedColor.Pumpkin, 4213));

        // 새 작물 (씨앗 · 수확물 · 밭 단계)
        Register("item_seed_corn", FitMode.UniformLargest, b => BuildSeedBag(b, StylizedColor.FlowerYellow));
        Register("item_seed_cabbage", FitMode.UniformLargest, b => BuildSeedBag(b, StylizedColor.CropGreen));
        Register("item_seed_sweet_potato", FitMode.UniformLargest, b => BuildSeedBag(b, StylizedColor.Caramel));
        Register("food_corn", FitMode.UniformLargest, BuildCornItem);
        Register("food_cabbage", FitMode.UniformLargest, BuildCabbageItem);
        Register("food_sweet_potato", FitMode.UniformLargest, BuildSweetPotatoItem);
        Register("crop_corn_growing", FitMode.UniformLargest, b => BuildCornPlant(b, false));
        Register("crop_corn_mature", FitMode.UniformLargest, b => BuildCornPlant(b, true));
        Register("crop_cabbage_growing", FitMode.UniformLargest, b => BuildCabbagePlant(b, false));
        Register("crop_cabbage_mature", FitMode.UniformLargest, b => BuildCabbagePlant(b, true));
        Register("crop_sweet_potato_growing", FitMode.UniformLargest, b => BuildSweetPotatoPlant(b, false));
        Register("crop_sweet_potato_mature", FitMode.UniformLargest, b => BuildSweetPotatoPlant(b, true));

        // 새 작물 요리
        Register("food_grilled_corn", FitMode.UniformLargest, BuildGrilledCorn);
        Register("food_cabbage_wrap", FitMode.UniformLargest, BuildCabbageWrap);
        Register("food_baked_sweet_potato", FitMode.UniformLargest, BuildBakedSweetPotato);

        // 절구
        Register("build_mortar", FitMode.UniformFootprint, BuildMortar);
    }

    // ---------------------------------------------------------------- 채집물 (손에 드는 크기)

    private static void BuildWildGreens(LowPolyMeshBuilder b) // 산나물 한 줌
    {
        for (int index = 0; index < 6; index++)
        {
            float angle = index / 6f * Mathf.PI * 2f;
            Vector3 root = new Vector3(Mathf.Sin(angle) * 0.02f, 0.01f, Mathf.Cos(angle) * 0.02f);
            b.AddLimb(StylizedColor.CropGreen, root, root + new Vector3(Mathf.Sin(angle) * 0.06f, 0.16f + (index % 2) * 0.04f, Mathf.Cos(angle) * 0.05f), 0.008f, 0.004f, 4);
            b.AddLowPolySphere(StylizedColor.Leaf, root + new Vector3(Mathf.Sin(angle) * 0.07f, 0.17f + (index % 2) * 0.04f, Mathf.Cos(angle) * 0.06f), new Vector3(0.035f, 0.01f, 0.05f), 1, 0.12f, 4001 + index);
        }

        b.AddLimb(StylizedColor.Rope, new Vector3(-0.03f, 0.05f, 0f), new Vector3(0.03f, 0.05f, 0f), 0.006f, 0.006f, 4);
    }

    private static void BuildWildFlower(LowPolyMeshBuilder b) // 들꽃 다발
    {
        StylizedColor[] colors = { StylizedColor.Flower, StylizedColor.FlowerYellow, StylizedColor.White };

        for (int index = 0; index < 5; index++)
        {
            float angle = index / 5f * Mathf.PI * 2f;
            Vector3 top = new Vector3(Mathf.Sin(angle) * 0.055f, 0.19f + (index % 2) * 0.03f, Mathf.Cos(angle) * 0.05f);
            b.AddLimb(StylizedColor.CropGreen, new Vector3(0f, 0.02f, 0f), top, 0.007f, 0.005f, 4);

            for (int petal = 0; petal < 5; petal++)
            {
                float petalAngle = petal / 5f * Mathf.PI * 2f;
                b.AddLowPolySphere(colors[index % colors.Length], top + new Vector3(Mathf.Sin(petalAngle) * 0.022f, 0f, Mathf.Cos(petalAngle) * 0.022f), new Vector3(0.018f, 0.006f, 0.018f), 0, 0f, 4011 + index * 5 + petal);
            }

            b.AddLowPolySphere(StylizedColor.FlowerYellow, top, Vector3.one * 0.012f, 0, 0f, 4050 + index);
        }
    }

    private static void BuildBambooShoot(LowPolyMeshBuilder b) // 죽순
    {
        b.AddFrustum(StylizedColor.ClothCream, Vector3.zero, 0.06f, 0.03f, 0.2f, 8, true, false);

        for (int index = 0; index < 4; index++) // 겹겹이 싼 껍질
        {
            float height = 0.03f + index * 0.045f;
            b.AddFrustum(StylizedColor.LeafDark, new Vector3(0f, height, 0f), 0.055f - index * 0.008f, 0.04f - index * 0.008f, 0.05f, 8, false, false);
        }

        b.AddCone(StylizedColor.Leaf, new Vector3(0f, 0.2f, 0f), 0.028f, 0.07f, 6);
    }

    private static void BuildRaspberry(LowPolyMeshBuilder b) // 산딸기
    {
        for (int index = 0; index < 3; index++)
        {
            Vector3 center = new Vector3(-0.04f + index * 0.04f, 0.06f + (index % 2) * 0.02f, (index % 2) * 0.02f);

            for (int bump = 0; bump < 6; bump++) // 알알이 붙은 열매
            {
                float angle = bump / 6f * Mathf.PI * 2f;
                b.AddLowPolySphere(StylizedColor.AppleRed, center + new Vector3(Mathf.Sin(angle) * 0.018f, 0f, Mathf.Cos(angle) * 0.018f), Vector3.one * 0.015f, 0, 0f, 4061 + index * 6 + bump);
            }

            b.AddLowPolySphere(StylizedColor.Strawberry, center + Vector3.up * 0.014f, Vector3.one * 0.016f, 0, 0f, 4081 + index);
        }

        b.AddLowPolySphere(StylizedColor.Leaf, new Vector3(0f, 0.02f, -0.02f), new Vector3(0.06f, 0.008f, 0.04f), 1, 0.1f, 4085);
    }

    private static void BuildHerbLeaf(LowPolyMeshBuilder b) // 약초 잎
    {
        b.AddLimb(StylizedColor.Herb, new Vector3(0f, 0.01f, -0.06f), new Vector3(0f, 0.04f, 0.08f), 0.007f, 0.004f, 4);

        for (int index = 0; index < 4; index++)
        {
            float side = index % 2 == 0 ? 1f : -1f;
            Vector3 spot = new Vector3(side * 0.045f, 0.03f, -0.03f + index * 0.035f);
            b.AddLowPolySphere(StylizedColor.Herb, spot, new Vector3(0.05f, 0.008f, 0.03f), 1, 0.14f, 4091 + index);
        }

        b.AddLowPolySphere(StylizedColor.Leaf, new Vector3(0f, 0.045f, 0.09f), new Vector3(0.035f, 0.007f, 0.055f), 1, 0.12f, 4096);
    }

    private static void BuildBambooStick(LowPolyMeshBuilder b) // 대나무 줄기
    {
        for (int index = 0; index < 4; index++)
        {
            b.AddCylinder(StylizedColor.CropGreen, new Vector3(0f, index * 0.09f, 0f), 0.028f, 0.085f, 8);
            b.AddCylinder(StylizedColor.LeafDark, new Vector3(0f, index * 0.09f + 0.085f, 0f), 0.032f, 0.008f, 8);
        }

        b.AddLowPolySphere(StylizedColor.Leaf, new Vector3(0.05f, 0.3f, 0.01f), new Vector3(0.07f, 0.008f, 0.03f), 1, 0.12f, 4101);
        b.AddLowPolySphere(StylizedColor.Leaf, new Vector3(-0.05f, 0.24f, -0.01f), new Vector3(0.07f, 0.008f, 0.03f), 1, 0.12f, 4102);
    }

    private static void BuildChestnut(LowPolyMeshBuilder b) // 밤 (가시 껍질 속)
    {
        b.AddLowPolySphere(StylizedColor.Bark, new Vector3(0f, 0.06f, 0f), new Vector3(0.06f, 0.05f, 0.06f), 1, 0.06f, 4111);
        b.AddCone(StylizedColor.BarkDark, new Vector3(0f, 0.1f, 0f), 0.022f, 0.035f, 5);

        for (int index = 0; index < 5; index++) // 밤 두 알과 껍질 조각
        {
            float angle = index / 5f * Mathf.PI * 2f;
            b.AddCone(StylizedColor.LeafDark, new Vector3(Mathf.Sin(angle) * 0.055f, 0.03f, Mathf.Cos(angle) * 0.055f), 0.012f, 0.05f, 4);
        }

        b.AddLowPolySphere(StylizedColor.Bark, new Vector3(0.06f, 0.03f, 0.05f), new Vector3(0.04f, 0.035f, 0.04f), 1, 0.06f, 4112);
    }

    private static void BuildBigMushroom(LowPolyMeshBuilder b) // 큰 버섯
    {
        b.AddFrustum(StylizedColor.MushroomStem, Vector3.zero, 0.035f, 0.03f, 0.13f, 8, true, false);
        b.AddFrustum(StylizedColor.MushroomCap, new Vector3(0f, 0.11f, 0f), 0.05f, 0.11f, 0.06f, 12, false, false);
        b.AddLowPolySphere(StylizedColor.MushroomCap, new Vector3(0f, 0.19f, 0f), new Vector3(0.115f, 0.05f, 0.115f), 1, 0.05f, 4121);
        b.AddLowPolySphere(StylizedColor.ClothCream, new Vector3(0.05f, 0.22f, 0.02f), new Vector3(0.03f, 0.012f, 0.03f), 0, 0f, 4122);
        b.AddLowPolySphere(StylizedColor.ClothCream, new Vector3(-0.04f, 0.21f, -0.04f), new Vector3(0.025f, 0.01f, 0.025f), 0, 0f, 4123);
    }

    private static void BuildAcorn(LowPolyMeshBuilder b) // 도토리
    {
        for (int index = 0; index < 3; index++)
        {
            Vector3 center = new Vector3(-0.035f + index * 0.035f, 0.05f, (index % 2) * 0.03f);
            b.AddLowPolySphere(StylizedColor.Caramel, center, new Vector3(0.028f, 0.035f, 0.028f), 1, 0.05f, 4131 + index);
            b.AddFrustum(StylizedColor.BarkDark, center + new Vector3(0f, 0.02f, 0f), 0.03f, 0.026f, 0.022f, 8, false, true);
            b.AddCone(StylizedColor.BarkDark, center + new Vector3(0f, 0.042f, 0f), 0.008f, 0.02f, 4);
        }
    }

    private static void BuildDryBranch(LowPolyMeshBuilder b) // 마른 가지 묶음
    {
        for (int index = 0; index < 5; index++)
        {
            float angle = index * 33f * Mathf.Deg2Rad;
            Vector3 from = new Vector3(Mathf.Sin(angle) * 0.02f, 0.03f + index * 0.008f, -0.13f);
            Vector3 to = new Vector3(Mathf.Sin(angle) * 0.04f, 0.03f + index * 0.008f, 0.14f);
            b.AddLimb(index % 2 == 0 ? StylizedColor.BarkDark : StylizedColor.Bark, from, to, 0.013f, 0.009f, 5);
        }

        b.AddLimb(StylizedColor.Rope, new Vector3(-0.05f, 0.05f, 0f), new Vector3(0.05f, 0.05f, 0f), 0.008f, 0.008f, 4);
        b.AddLimb(StylizedColor.Bark, new Vector3(0.02f, 0.05f, 0.1f), new Vector3(0.09f, 0.09f, 0.16f), 0.008f, 0.005f, 4);
    }

    private static void BuildIceFlower(LowPolyMeshBuilder b) // 얼음꽃
    {
        b.AddLimb(StylizedColor.SnakeScale, new Vector3(0f, 0.01f, 0f), new Vector3(0f, 0.12f, 0f), 0.008f, 0.006f, 4);

        for (int index = 0; index < 6; index++)
        {
            float angle = index / 6f * Mathf.PI * 2f;
            Vector3 tip = new Vector3(Mathf.Sin(angle) * 0.06f, 0.14f, Mathf.Cos(angle) * 0.06f);
            b.AddLimb(StylizedColor.Ice, new Vector3(0f, 0.12f, 0f), tip, 0.012f, 0.004f, 4);
            b.AddLowPolySphere(StylizedColor.Glass, tip, Vector3.one * 0.016f, 0, 0f, 4141 + index);
        }

        b.AddLowPolySphere(StylizedColor.Snow, new Vector3(0f, 0.135f, 0f), Vector3.one * 0.024f, 1, 0.05f, 4148);
    }

    private static void BuildPineCone(LowPolyMeshBuilder b) // 솔방울
    {
        b.AddFrustum(StylizedColor.BarkDark, new Vector3(0f, 0.02f, 0f), 0.02f, 0.045f, 0.06f, 8, true, false);
        b.AddFrustum(StylizedColor.Bark, new Vector3(0f, 0.08f, 0f), 0.045f, 0.015f, 0.09f, 8, false, true);

        for (int ring = 0; ring < 4; ring++) // 비늘
        {
            for (int index = 0; index < 6; index++)
            {
                float angle = (index + ring * 0.5f) / 6f * Mathf.PI * 2f;
                float radius = 0.044f - ring * 0.008f;
                b.AddLowPolySphere(StylizedColor.BarkDark, new Vector3(Mathf.Sin(angle) * radius, 0.05f + ring * 0.028f, Mathf.Cos(angle) * radius), new Vector3(0.018f, 0.012f, 0.018f), 0, 0f, 4151 + ring * 6 + index);
            }
        }
    }

    // ---------------------------------------------------------------- 계절 요리 · 약

    private static void BuildGreensSalad(LowPolyMeshBuilder b) // 나물 무침
    {
        AddBowl(b, StylizedColor.LeafDark);
        float top = BowlSoupHeight;

        for (int index = 0; index < 6; index++)
        {
            float angle = index / 6f * Mathf.PI * 2f;
            b.AddLowPolySphere(StylizedColor.CropGreen, new Vector3(Mathf.Sin(angle) * 0.07f, top + 0.02f, Mathf.Cos(angle) * 0.06f), new Vector3(0.05f, 0.012f, 0.03f), 1, 0.14f, 4161 + index);
        }

        b.AddLowPolySphere(StylizedColor.AppleRed, new Vector3(0.02f, top + 0.03f, 0f), Vector3.one * 0.016f, 0, 0f, 4168);
        b.AddLowPolySphere(StylizedColor.White, new Vector3(-0.04f, top + 0.03f, 0.03f), Vector3.one * 0.012f, 0, 0f, 4169);
    }

    private static void BuildBambooStirfry(LowPolyMeshBuilder b) // 죽순 볶음
    {
        AddBowl(b, StylizedColor.Caramel);
        float top = BowlSoupHeight;

        for (int index = 0; index < 5; index++)
        {
            b.Push(new Vector3(-0.06f + index * 0.03f, top + 0.02f, (index % 2) * 0.04f - 0.02f), Euler(0f, index * 32f, 12f), Vector3.one);
            b.AddFrustum(StylizedColor.ClothCream, Vector3.zero, 0.022f, 0.012f, 0.05f, 6, true, true);
            b.Pop();
        }

        b.AddLowPolySphere(StylizedColor.AppleRed, new Vector3(0.05f, top + 0.025f, -0.04f), new Vector3(0.03f, 0.015f, 0.025f), 1, 0.1f, 4171);
        b.AddLowPolySphere(StylizedColor.Herb, new Vector3(-0.03f, top + 0.03f, -0.03f), new Vector3(0.02f, 0.005f, 0.012f), 0, 0f, 4172);
        AddSteam(b, new Vector3(0f, top + 0.07f, 0f), 4175);
    }

    private static void BuildBerryDrink(LowPolyMeshBuilder b) // 산딸기 음료
    {
        b.AddCylinder(StylizedColor.Glass, new Vector3(0f, 0.01f, 0f), 0.055f, 0.2f, 10);
        b.AddCylinder(StylizedColor.Strawberry, new Vector3(0f, 0.02f, 0f), 0.05f, 0.15f, 10);
        b.AddCylinder(StylizedColor.WoodDark, Vector3.zero, 0.06f, 0.015f, 10);
        b.AddLowPolySphere(StylizedColor.AppleRed, new Vector3(0.02f, 0.18f, 0.01f), Vector3.one * 0.022f, 1, 0.05f, 4181);
        b.AddLowPolySphere(StylizedColor.Leaf, new Vector3(-0.03f, 0.19f, -0.01f), new Vector3(0.035f, 0.008f, 0.025f), 1, 0.1f, 4182);
        b.AddLimb(StylizedColor.ClothCream, new Vector3(0.03f, 0.16f, 0.02f), new Vector3(0.05f, 0.27f, 0.03f), 0.006f, 0.006f, 4);
    }

    private static void BuildRoastChestnut(LowPolyMeshBuilder b) // 군밤
    {
        b.AddFrustum(StylizedColor.SeedPaper, Vector3.zero, 0.07f, 0.09f, 0.12f, 8, true, false); // 종이 봉지

        for (int index = 0; index < 4; index++)
        {
            float angle = index / 4f * Mathf.PI * 2f;
            b.AddLowPolySphere(StylizedColor.Charred, new Vector3(Mathf.Sin(angle) * 0.04f, 0.13f, Mathf.Cos(angle) * 0.04f), new Vector3(0.04f, 0.035f, 0.04f), 1, 0.07f, 4191 + index);
            b.AddLowPolySphere(StylizedColor.Caramel, new Vector3(Mathf.Sin(angle) * 0.04f, 0.15f, Mathf.Cos(angle) * 0.04f), new Vector3(0.025f, 0.012f, 0.025f), 0, 0f, 4196 + index);
        }

        AddSteam(b, new Vector3(0f, 0.2f, 0f), 4200);
    }

    private static void BuildMushroomHotpot(LowPolyMeshBuilder b) // 버섯 전골
    {
        AddBowl(b, StylizedColor.SoupTomato);
        float top = BowlSoupHeight;

        for (int index = 0; index < 3; index++)
        {
            float angle = index / 3f * Mathf.PI * 2f;
            Vector3 spot = new Vector3(Mathf.Sin(angle) * 0.07f, top + 0.01f, Mathf.Cos(angle) * 0.06f);
            b.AddCylinder(StylizedColor.MushroomStem, spot, 0.016f, 0.035f, 6);
            b.AddLowPolySphere(StylizedColor.MushroomCap, spot + Vector3.up * 0.045f, new Vector3(0.04f, 0.02f, 0.04f), 1, 0.05f, 4211 + index);
        }

        b.AddLowPolySphere(StylizedColor.CropGreen, new Vector3(0f, top + 0.015f, 0f), new Vector3(0.05f, 0.012f, 0.04f), 1, 0.12f, 4215);
        AddSteam(b, new Vector3(0.02f, top + 0.08f, 0.02f), 4218);
    }

    private static void BuildWinterTonic(LowPolyMeshBuilder b) // 겨울 약
    {
        b.AddFrustum(StylizedColor.Glass, Vector3.zero, 0.045f, 0.035f, 0.14f, 8, true, false);
        b.AddCylinder(StylizedColor.Ice, new Vector3(0f, 0.01f, 0f), 0.035f, 0.1f, 8);
        b.AddCylinder(StylizedColor.WoodDark, new Vector3(0f, 0.14f, 0f), 0.022f, 0.035f, 8);
        b.AddLowPolySphere(StylizedColor.Snow, new Vector3(0f, 0.18f, 0f), Vector3.one * 0.02f, 1, 0.05f, 4221);
        b.AddLowPolySphere(StylizedColor.Herb, new Vector3(0.03f, 0.06f, 0.02f), new Vector3(0.02f, 0.006f, 0.012f), 0, 0f, 4222);
    }

    // ---------------------------------------------------------------- 과일 · 묘목 · 나무

    private static void BuildRoundFruit(LowPolyMeshBuilder b, StylizedColor color, float radius, int seed) // 자두 · 감처럼 둥근 과일
    {
        b.AddLowPolySphere(color, new Vector3(0f, radius, 0f), new Vector3(radius, radius * 0.92f, radius), 1, 0.05f, seed);
        b.AddLimb(StylizedColor.BarkDark, new Vector3(0f, radius * 1.8f, 0f), new Vector3(0.008f, radius * 2.3f, 0.004f), 0.006f, 0.004f, 4);
        b.AddLowPolySphere(StylizedColor.Leaf, new Vector3(0.022f, radius * 2.15f, 0.008f), new Vector3(0.03f, 0.006f, 0.02f), 1, 0.1f, seed + 1);
        b.AddLowPolySphere(StylizedColor.White, new Vector3(-radius * 0.45f, radius * 1.1f, radius * 0.4f), new Vector3(radius * 0.3f, radius * 0.22f, radius * 0.22f), 0, 0f, seed + 2);
    }

    private static void BuildSaplingItem(LowPolyMeshBuilder b, StylizedColor leaf) // 들고 다니는 묘목
    {
        b.AddFrustum(StylizedColor.SoilTilled, Vector3.zero, 0.075f, 0.06f, 0.09f, 8, true, false); // 흙 화분
        b.AddCylinder(StylizedColor.SoilWet, new Vector3(0f, 0.085f, 0f), 0.06f, 0.012f, 8);
        b.AddLimb(StylizedColor.Bark, new Vector3(0f, 0.09f, 0f), new Vector3(0.005f, 0.26f, 0f), 0.012f, 0.008f, 5);

        for (int index = 0; index < 4; index++)
        {
            float angle = index / 4f * Mathf.PI * 2f;
            b.AddLowPolySphere(leaf, new Vector3(Mathf.Sin(angle) * 0.045f, 0.2f + (index % 2) * 0.05f, Mathf.Cos(angle) * 0.04f), new Vector3(0.05f, 0.012f, 0.035f), 1, 0.12f, 4231 + index);
        }

        b.AddLowPolySphere(leaf, new Vector3(0f, 0.28f, 0f), new Vector3(0.05f, 0.03f, 0.05f), 1, 0.14f, 4236);
    }

    private static void BuildTreeSapling(LowPolyMeshBuilder b) // 심은 묘목 (0.7m)
    {
        b.AddLowPolySphere(StylizedColor.SoilWet, new Vector3(0f, 0.03f, 0f), new Vector3(0.24f, 0.04f, 0.24f), 1, 0.1f, 4241);
        b.AddLimb(StylizedColor.Bark, new Vector3(0f, 0.02f, 0f), new Vector3(0.02f, 0.5f, 0f), 0.035f, 0.022f, 6);

        for (int index = 0; index < 5; index++)
        {
            float angle = index / 5f * Mathf.PI * 2f;
            b.AddLowPolySphere(StylizedColor.Leaf, new Vector3(Mathf.Sin(angle) * 0.12f, 0.42f + (index % 2) * 0.1f, Mathf.Cos(angle) * 0.11f), new Vector3(0.13f, 0.035f, 0.1f), 1, 0.14f, 4242 + index);
        }

        b.AddLowPolySphere(StylizedColor.LeafLight, new Vector3(0f, 0.6f, 0f), new Vector3(0.14f, 0.1f, 0.14f), 1, 0.16f, 4248);
    }

    private static void BuildTreeYoung(LowPolyMeshBuilder b) // 어린 나무 (1.8m)
    {
        b.AddLowPolySphere(StylizedColor.SoilWet, new Vector3(0f, 0.03f, 0f), new Vector3(0.36f, 0.04f, 0.36f), 1, 0.1f, 4251);
        b.AddLimb(StylizedColor.Bark, new Vector3(0f, 0.02f, 0f), new Vector3(0.03f, 1.1f, 0.02f), 0.09f, 0.055f, 7);

        for (int index = 0; index < 3; index++) // 가지
        {
            float angle = index / 3f * Mathf.PI * 2f;
            b.AddLimb(StylizedColor.Bark, new Vector3(0f, 0.85f, 0f), new Vector3(Mathf.Sin(angle) * 0.3f, 1.2f, Mathf.Cos(angle) * 0.28f), 0.035f, 0.02f, 5);
        }

        b.AddLowPolySphere(StylizedColor.LeafDark, new Vector3(0f, 1.35f, 0f), new Vector3(0.52f, 0.34f, 0.5f), 2, 0.16f, 4252);
        b.AddLowPolySphere(StylizedColor.Leaf, new Vector3(0.18f, 1.2f, 0.12f), new Vector3(0.28f, 0.2f, 0.26f), 1, 0.18f, 4253);
        b.AddLowPolySphere(StylizedColor.Leaf, new Vector3(-0.2f, 1.28f, -0.1f), new Vector3(0.26f, 0.19f, 0.24f), 1, 0.18f, 4254);
    }

    private static void BuildFruitTreeMature(LowPolyMeshBuilder b, StylizedColor leaf, int seed) // 큰 과일나무 (3.2m)
    {
        b.AddLowPolySphere(StylizedColor.SoilWet, new Vector3(0f, 0.04f, 0f), new Vector3(0.5f, 0.05f, 0.5f), 1, 0.1f, seed);
        b.AddLimb(StylizedColor.BarkDark, new Vector3(0f, 0f, 0f), new Vector3(0.04f, 1.5f, 0.02f), 0.17f, 0.1f, 8);

        for (int index = 0; index < 5; index++) // 굵은 가지
        {
            float angle = index / 5f * Mathf.PI * 2f + 0.4f;
            Vector3 from = new Vector3(0f, 1.1f + (index % 2) * 0.25f, 0f);
            Vector3 to = new Vector3(Mathf.Sin(angle) * 0.72f, 1.9f + (index % 2) * 0.2f, Mathf.Cos(angle) * 0.68f);
            b.AddLimb(StylizedColor.Bark, from, to, 0.07f, 0.035f, 6);
            b.AddLowPolySphere(leaf, to + Vector3.up * 0.1f, new Vector3(0.46f, 0.32f, 0.44f), 1, 0.18f, seed + 10 + index);
        }

        b.AddLowPolySphere(leaf, new Vector3(0f, 2.3f, 0f), new Vector3(0.95f, 0.6f, 0.92f), 2, 0.16f, seed + 1);
        b.AddLowPolySphere(StylizedColor.LeafLight, new Vector3(0.2f, 2.6f, 0.16f), new Vector3(0.5f, 0.34f, 0.48f), 1, 0.18f, seed + 2);
        b.AddLowPolySphere(leaf, new Vector3(-0.3f, 2.5f, -0.2f), new Vector3(0.52f, 0.36f, 0.5f), 1, 0.18f, seed + 3);
    }

    private static void BuildTreeFruitCluster(LowPolyMeshBuilder b, StylizedColor fruit, int seed) // 나무에 달린 열매 (큰 나무에 덧붙임)
    {
        for (int index = 0; index < 9; index++)
        {
            float angle = index / 9f * Mathf.PI * 2f + 0.3f;
            float radius = 0.55f + (index % 3) * 0.16f;
            Vector3 spot = new Vector3(Mathf.Sin(angle) * radius, 1.95f + (index % 4) * 0.22f, Mathf.Cos(angle) * radius);
            b.AddLowPolySphere(fruit, spot, Vector3.one * 0.1f, 1, 0.05f, seed + index);
            b.AddLimb(StylizedColor.BarkDark, spot + Vector3.up * 0.1f, spot + Vector3.up * 0.17f, 0.012f, 0.008f, 4);
        }
    }

    // ---------------------------------------------------------------- 새 작물

    private static void BuildSeedBag(LowPolyMeshBuilder b, StylizedColor seedColor) // 씨앗 봉지
    {
        b.AddBeveledBox(StylizedColor.SeedPaper, new Vector3(0f, 0.09f, 0f), new Vector3(0.14f, 0.18f, 0.06f), 0.02f);
        b.AddBox(StylizedColor.Rope, new Vector3(0f, 0.17f, 0f), new Vector3(0.12f, 0.02f, 0.065f));
        b.AddLowPolySphere(seedColor, new Vector3(0f, 0.09f, 0.035f), new Vector3(0.05f, 0.05f, 0.01f), 1, 0.06f, 4261);
        b.AddLowPolySphere(seedColor, new Vector3(0.03f, 0.2f, 0f), Vector3.one * 0.016f, 0, 0f, 4262);
        b.AddLowPolySphere(seedColor, new Vector3(-0.03f, 0.19f, 0.01f), Vector3.one * 0.014f, 0, 0f, 4263);
    }

    private static void BuildCornItem(LowPolyMeshBuilder b) // 옥수수
    {
        b.AddFrustum(StylizedColor.FlowerYellow, new Vector3(0f, 0.02f, 0f), 0.045f, 0.04f, 0.2f, 10, true, true);

        for (int ring = 0; ring < 6; ring++) // 알알이
        {
            for (int index = 0; index < 8; index++)
            {
                float angle = (index + ring * 0.5f) / 8f * Mathf.PI * 2f;
                b.AddLowPolySphere(StylizedColor.Butter, new Vector3(Mathf.Sin(angle) * 0.044f, 0.04f + ring * 0.03f, Mathf.Cos(angle) * 0.044f), Vector3.one * 0.013f, 0, 0f, 4271 + ring * 8 + index);
            }
        }

        b.AddLowPolySphere(StylizedColor.CropGreen, new Vector3(0.03f, 0.1f, 0.03f), new Vector3(0.02f, 0.11f, 0.03f), 1, 0.12f, 4331);
        b.AddLowPolySphere(StylizedColor.CropGreen, new Vector3(-0.03f, 0.09f, -0.02f), new Vector3(0.02f, 0.1f, 0.03f), 1, 0.12f, 4332);
        b.AddLowPolySphere(StylizedColor.Straw, new Vector3(0f, 0.23f, 0f), new Vector3(0.02f, 0.04f, 0.02f), 1, 0.16f, 4333);
    }

    private static void BuildCabbageItem(LowPolyMeshBuilder b) // 양배추
    {
        b.AddLowPolySphere(StylizedColor.CropGreen, new Vector3(0f, 0.09f, 0f), new Vector3(0.1f, 0.09f, 0.1f), 2, 0.08f, 4341);

        for (int index = 0; index < 5; index++) // 겉잎
        {
            float angle = index / 5f * Mathf.PI * 2f;
            b.Push(new Vector3(Mathf.Sin(angle) * 0.075f, 0.06f, Mathf.Cos(angle) * 0.075f), Euler(-32f, -angle * Mathf.Rad2Deg, 0f), Vector3.one);
            b.AddLowPolySphere(StylizedColor.Leaf, Vector3.zero, new Vector3(0.07f, 0.012f, 0.06f), 1, 0.16f, 4342 + index);
            b.Pop();
        }

        b.AddLowPolySphere(StylizedColor.LeafLight, new Vector3(0f, 0.16f, 0f), new Vector3(0.05f, 0.025f, 0.05f), 1, 0.14f, 4348);
    }

    private static void BuildSweetPotatoItem(LowPolyMeshBuilder b) // 고구마
    {
        b.AddLowPolySphere(StylizedColor.Strawberry, new Vector3(0f, 0.05f, 0f), new Vector3(0.05f, 0.045f, 0.11f), 1, 0.09f, 4351);
        b.AddLowPolySphere(StylizedColor.Caramel, new Vector3(0f, 0.05f, 0.09f), new Vector3(0.03f, 0.028f, 0.05f), 1, 0.1f, 4352);
        b.AddLimb(StylizedColor.BarkDark, new Vector3(0f, 0.06f, -0.11f), new Vector3(0.01f, 0.08f, -0.15f), 0.008f, 0.004f, 4);
        b.AddLowPolySphere(StylizedColor.SoilTilled, new Vector3(0.03f, 0.03f, 0.02f), new Vector3(0.02f, 0.008f, 0.03f), 0, 0f, 4353);
    }

    private static void BuildCornPlant(LowPolyMeshBuilder b, bool mature) // 밭의 옥수수
    {
        float height = mature ? 1.1f : 0.6f;
        b.AddLimb(StylizedColor.CropGreen, new Vector3(0f, 0.02f, 0f), new Vector3(0f, height, 0f), 0.035f, 0.022f, 6);

        for (int index = 0; index < 6; index++) // 넓은 잎
        {
            float angle = index / 6f * Mathf.PI * 2f;
            float leafHeight = 0.2f + index * (height * 0.12f);
            b.Push(new Vector3(0f, leafHeight, 0f), Euler(-24f, angle * Mathf.Rad2Deg, 0f), Vector3.one);
            b.AddLowPolySphere(StylizedColor.Leaf, new Vector3(0f, 0f, 0.16f), new Vector3(0.05f, 0.014f, 0.19f), 1, 0.14f, 4361 + index);
            b.Pop();
        }

        if (!mature)
        {
            return;
        }

        for (int index = 0; index < 2; index++) // 익은 옥수수
        {
            Vector3 spot = new Vector3(index == 0 ? 0.11f : -0.1f, 0.62f + index * 0.1f, index == 0 ? 0.04f : -0.05f);
            b.AddFrustum(StylizedColor.FlowerYellow, spot, 0.045f, 0.035f, 0.2f, 8, true, true);
            b.AddLowPolySphere(StylizedColor.Butter, spot + Vector3.up * 0.1f, new Vector3(0.05f, 0.08f, 0.05f), 1, 0.08f, 4371 + index);
            b.AddLowPolySphere(StylizedColor.Straw, spot + Vector3.up * 0.22f, new Vector3(0.02f, 0.05f, 0.02f), 1, 0.16f, 4373 + index);
        }

        b.AddLowPolySphere(StylizedColor.Straw, new Vector3(0f, height + 0.08f, 0f), new Vector3(0.03f, 0.1f, 0.03f), 1, 0.18f, 4375);
    }

    private static void BuildCabbagePlant(LowPolyMeshBuilder b, bool mature) // 밭의 양배추
    {
        float size = mature ? 1f : 0.6f;

        for (int index = 0; index < 6; index++) // 펼친 잎
        {
            float angle = index / 6f * Mathf.PI * 2f;
            b.Push(new Vector3(Mathf.Sin(angle) * 0.1f * size, 0.04f, Mathf.Cos(angle) * 0.1f * size), Euler(-26f, -angle * Mathf.Rad2Deg, 0f), Vector3.one);
            b.AddLowPolySphere(StylizedColor.Leaf, Vector3.zero, new Vector3(0.13f, 0.018f, 0.11f) * size, 1, 0.16f, 4381 + index);
            b.Pop();
        }

        b.AddLowPolySphere(mature ? StylizedColor.CropGreen : StylizedColor.LeafLight, new Vector3(0f, 0.09f * size, 0f), new Vector3(0.12f, 0.1f, 0.12f) * size, 2, 0.08f, 4388);

        if (mature)
        {
            b.AddLowPolySphere(StylizedColor.LeafLight, new Vector3(0f, 0.2f, 0f), new Vector3(0.06f, 0.03f, 0.06f), 1, 0.14f, 4389);
        }
    }

    private static void BuildSweetPotatoPlant(LowPolyMeshBuilder b, bool mature) // 밭의 고구마 (덩굴)
    {
        for (int index = 0; index < 7; index++) // 덩굴 잎
        {
            float angle = index / 7f * Mathf.PI * 2f;
            float radius = (mature ? 0.2f : 0.13f) + (index % 2) * 0.05f;
            Vector3 spot = new Vector3(Mathf.Sin(angle) * radius, 0.06f + (index % 3) * 0.03f, Mathf.Cos(angle) * radius);
            b.AddLimb(StylizedColor.CropGreen, new Vector3(0f, 0.04f, 0f), spot, 0.012f, 0.008f, 4);
            b.AddLowPolySphere(StylizedColor.Leaf, spot, new Vector3(0.09f, 0.016f, 0.08f), 1, 0.16f, 4391 + index);
        }

        b.AddLowPolySphere(StylizedColor.SoilTilled, new Vector3(0f, 0.03f, 0f), new Vector3(0.14f, 0.03f, 0.14f), 1, 0.12f, 4399);

        if (!mature)
        {
            return;
        }

        b.AddLowPolySphere(StylizedColor.Strawberry, new Vector3(0.09f, 0.05f, 0.03f), new Vector3(0.04f, 0.035f, 0.07f), 1, 0.1f, 4400);
        b.AddLowPolySphere(StylizedColor.Strawberry, new Vector3(-0.08f, 0.045f, -0.05f), new Vector3(0.035f, 0.03f, 0.06f), 1, 0.1f, 4401);
        b.AddLowPolySphere(StylizedColor.Flower, new Vector3(0f, 0.22f, 0.02f), new Vector3(0.05f, 0.03f, 0.05f), 1, 0.12f, 4402);
    }

    private static void BuildGrilledCorn(LowPolyMeshBuilder b) // 옥수수 구이
    {
        b.AddLimb(StylizedColor.WoodLight, new Vector3(0f, 0.05f, -0.16f), new Vector3(0f, 0.05f, 0.18f), 0.012f, 0.008f, 5);
        b.AddFrustum(StylizedColor.Butter, new Vector3(0f, 0.05f, -0.1f), 0.05f, 0.045f, 0.001f, 10, true, false);
        b.Push(new Vector3(0f, 0.05f, -0.1f), Euler(-90f, 0f, 0f), Vector3.one);
        b.AddCylinder(StylizedColor.Butter, Vector3.zero, 0.05f, 0.22f, 10);
        b.Pop();

        for (int index = 0; index < 8; index++) // 구운 자국
        {
            float angle = index / 8f * Mathf.PI * 2f;
            b.AddLowPolySphere(StylizedColor.Charred, new Vector3(Mathf.Sin(angle) * 0.05f, 0.05f, -0.02f + (index % 3) * 0.06f), new Vector3(0.02f, 0.02f, 0.03f), 0, 0f, 4411 + index);
        }

        AddSteam(b, new Vector3(0f, 0.13f, 0f), 4420);
    }

    private static void BuildCabbageWrap(LowPolyMeshBuilder b) // 양배추 쌈
    {
        AddBowl(b, StylizedColor.LeafDark);
        float top = BowlSoupHeight;

        for (int index = 0; index < 3; index++)
        {
            Vector3 spot = new Vector3(-0.05f + index * 0.05f, top + 0.02f, (index % 2) * 0.05f - 0.02f);
            b.AddLowPolySphere(StylizedColor.Leaf, spot, new Vector3(0.05f, 0.03f, 0.045f), 1, 0.12f, 4421 + index);
            b.AddLowPolySphere(StylizedColor.AppleRed, spot + Vector3.up * 0.025f, new Vector3(0.02f, 0.01f, 0.018f), 0, 0f, 4424 + index);
        }

        b.AddLowPolySphere(StylizedColor.SoupTomato, new Vector3(0.06f, top + 0.015f, -0.05f), new Vector3(0.03f, 0.012f, 0.03f), 1, 0.08f, 4428);
    }

    private static void BuildBakedSweetPotato(LowPolyMeshBuilder b) // 고구마 구이
    {
        b.AddLowPolySphere(StylizedColor.Charred, new Vector3(0f, 0.06f, 0f), new Vector3(0.055f, 0.05f, 0.13f), 1, 0.09f, 4431);
        b.AddLowPolySphere(StylizedColor.Caramel, new Vector3(0.01f, 0.1f, 0f), new Vector3(0.035f, 0.02f, 0.08f), 1, 0.12f, 4432);
        b.AddLowPolySphere(StylizedColor.Butter, new Vector3(0.01f, 0.115f, 0.01f), new Vector3(0.022f, 0.012f, 0.05f), 1, 0.1f, 4433);
        b.AddLowPolySphere(StylizedColor.Charred, new Vector3(-0.02f, 0.05f, -0.1f), new Vector3(0.03f, 0.028f, 0.04f), 1, 0.1f, 4434);
        AddSteam(b, new Vector3(0.02f, 0.15f, 0.01f), 4440);
    }

    // ---------------------------------------------------------------- 절구

    private static void BuildMortar(LowPolyMeshBuilder b) // 절구 (지름 0.9m)
    {
        b.AddCylinder(StylizedColor.StoneDark, Vector3.zero, 0.42f, 0.06f, 12); // 받침돌
        b.AddFrustum(StylizedColor.Stone, new Vector3(0f, 0.06f, 0f), 0.3f, 0.36f, 0.42f, 12, false, false); // 돌 절구
        b.AddCylinder(StylizedColor.StoneDark, new Vector3(0f, 0.44f, 0f), 0.33f, 0.05f, 12);
        b.AddCylinder(StylizedColor.SoilTilled, new Vector3(0f, 0.4f, 0f), 0.27f, 0.04f, 12); // 안에 든 곡식
        b.AddLowPolySphere(StylizedColor.Grain, new Vector3(0.06f, 0.43f, 0.04f), new Vector3(0.09f, 0.02f, 0.08f), 1, 0.14f, 4451);
        b.AddLowPolySphere(StylizedColor.FlowerYellow, new Vector3(-0.07f, 0.43f, -0.05f), new Vector3(0.07f, 0.018f, 0.06f), 1, 0.14f, 4452);

        b.Push(new Vector3(0.22f, 0.5f, 0.1f), Euler(-24f, 20f, 14f), Vector3.one); // 공이 (기대어 둔다)
        b.AddCylinder(StylizedColor.WoodDark, Vector3.zero, 0.045f, 0.6f, 8);
        b.AddLowPolySphere(StylizedColor.Stone, new Vector3(0f, 0.62f, 0f), new Vector3(0.08f, 0.07f, 0.08f), 1, 0.06f, 4453);
        b.Pop();

        b.AddBox(StylizedColor.WoodPlank, new Vector3(-0.3f, 0.05f, -0.28f), new Vector3(0.3f, 0.1f, 0.22f)); // 곡식 자루 놓는 판
        b.AddLowPolySphere(StylizedColor.Grain, new Vector3(-0.3f, 0.14f, -0.28f), new Vector3(0.12f, 0.05f, 0.09f), 1, 0.16f, 4454);
    }
}
