using UnityEngine;

// 79일차: 농사 콘텐츠 모델 (밭, 작물 성장 단계, 씨앗, 수확물, 농사 도구)
// 작물 단계 모델은 크기를 맞추지 않고 그대로 쓰므로 미터 단위로 만든다.
// 기준점은 밭 Prefab의 CropAnchor(밭 이랑 윗면)이며 한 칸(1m)에 네 포기를 심는다.
public static partial class StylizedModelLibrary
{
    public const float CropAnchorHeight = 0.13f;
    private const float PlantScale = 1.3f;

    private static readonly Vector3[] CropPlantSpots =
    {
        new Vector3(-0.22f, 0f, -0.22f),
        new Vector3(0.22f, 0f, -0.22f),
        new Vector3(-0.22f, 0f, 0.22f),
        new Vector3(0.22f, 0f, 0.22f)
    };

    private static void RegisterFarming()
    {
        // 밭
        Register("build_farm_plot", FitMode.Stretch, BuildFarmPlot);
        Register("fx_farm_plot_wet", FitMode.Stretch, BuildFarmPlotWetOverlay);
        Register("fx_ready_sparkle", FitMode.UniformLargest, BuildReadySparkle);

        // 공통 성장 단계
        Register("crop_seeded", FitMode.UniformLargest, BuildCropSeeded);
        Register("crop_sprout", FitMode.UniformLargest, BuildCropSprout);

        // 작물별 성장 단계
        Register("crop_potato_growing", FitMode.UniformLargest, b => BuildPotatoPlant(b, false));
        Register("crop_potato_mature", FitMode.UniformLargest, b => BuildPotatoPlant(b, true));
        Register("crop_strawberry_growing", FitMode.UniformLargest, b => BuildStrawberryPlant(b, false));
        Register("crop_strawberry_mature", FitMode.UniformLargest, b => BuildStrawberryPlant(b, true));
        Register("crop_tomato_growing", FitMode.UniformLargest, b => BuildTomatoPlant(b, false));
        Register("crop_tomato_mature", FitMode.UniformLargest, b => BuildTomatoPlant(b, true));
        Register("crop_pumpkin_growing", FitMode.UniformLargest, b => BuildPumpkinPlant(b, false));
        Register("crop_pumpkin_mature", FitMode.UniformLargest, b => BuildPumpkinPlant(b, true));
        Register("crop_winter_radish_growing", FitMode.UniformLargest, b => BuildRadishPlant(b, false));
        Register("crop_winter_radish_mature", FitMode.UniformLargest, b => BuildRadishPlant(b, true));

        // 수확물
        Register("item_potato", FitMode.UniformLargest, BuildPotatoItem);
        Register("item_strawberry", FitMode.UniformLargest, BuildStrawberryItem);
        Register("item_tomato", FitMode.UniformLargest, BuildTomatoItem);
        Register("item_pumpkin", FitMode.UniformLargest, b => AddPumpkin(b, Vector3.zero, 0.3f, 201));
        Register("item_winter_radish", FitMode.UniformLargest, BuildRadishItem);

        // 씨앗 봉투
        Register("item_seed_potato", FitMode.UniformLargest, b => BuildSeedPouch(b, StylizedColor.BarkDark, StylizedColor.Potato, 211));
        Register("item_seed_strawberry", FitMode.UniformLargest, b => BuildSeedPouch(b, StylizedColor.ClothRed, StylizedColor.Strawberry, 212));
        Register("item_seed_tomato", FitMode.UniformLargest, b => BuildSeedPouch(b, StylizedColor.AppleRed, StylizedColor.Tomato, 213));
        Register("item_seed_pumpkin", FitMode.UniformLargest, b => BuildSeedPouch(b, StylizedColor.LeafAutumn, StylizedColor.Pumpkin, 214));
        Register("item_seed_winter_radish", FitMode.UniformLargest, b => BuildSeedPouch(b, StylizedColor.ClothBlue, StylizedColor.White, 215));

        // 농사 도구
        Register("tool_hoe", FitMode.UniformLargest, BuildHoe);
        Register("tool_watering_can", FitMode.UniformLargest, BuildWateringCan);

        // 밭 주변 소품
        Register("prop_scarecrow", FitMode.UniformHeight, BuildScarecrow);
    }

    // ---------------------------------------------------------------- 밭

    // 가로·세로 1m, 높이 0.16m 경작지 (이랑 2줄 + 나무 테두리)
    private static void BuildFarmPlot(LowPolyMeshBuilder b)
    {
        b.AddBeveledBox(StylizedColor.SoilTilled, new Vector3(0f, 0.04f, 0f), new Vector3(0.94f, 0.08f, 0.94f), 0.02f);
        b.AddWedge(StylizedColor.SoilTilled, new Vector3(0f, 0.11f, -0.22f), new Vector3(0.82f, 0.06f, 0.3f));
        b.AddWedge(StylizedColor.SoilTilled, new Vector3(0f, 0.11f, 0.22f), new Vector3(0.82f, 0.06f, 0.3f));

        // 고랑에 흩어진 흙덩이
        System.Random random = new System.Random(221);

        for (int index = 0; index < 7; index++)
        {
            Vector3 position = new Vector3(Rand(random, -0.38f, 0.38f), 0.085f, Rand(random, -0.04f, 0.04f));
            b.AddLowPolySphere(StylizedColor.Dirt, position, Vector3.one * Rand(random, 0.015f, 0.025f), 0, 0.2f, index);
        }

        // 나무 테두리와 모서리 말뚝
        b.AddBox(StylizedColor.WoodPlank, new Vector3(0f, 0.06f, 0.47f), new Vector3(1f, 0.12f, 0.06f));
        b.AddBox(StylizedColor.WoodPlank, new Vector3(0f, 0.06f, -0.47f), new Vector3(1f, 0.12f, 0.06f));
        b.AddBox(StylizedColor.WoodDark, new Vector3(0.47f, 0.06f, 0f), new Vector3(0.06f, 0.12f, 0.88f));
        b.AddBox(StylizedColor.WoodDark, new Vector3(-0.47f, 0.06f, 0f), new Vector3(0.06f, 0.12f, 0.88f));

        for (int x = -1; x <= 1; x += 2)
        {
            for (int z = -1; z <= 1; z += 2)
            {
                b.AddBox(StylizedColor.BarkDark, new Vector3(x * 0.47f, 0.08f, z * 0.47f), new Vector3(0.06f, 0.16f, 0.06f));
            }
        }
    }

    // 물을 준 밭 위에 겹쳐 표시하는 젖은 흙 (밭 모델과 같은 좌표계의 자식으로 붙인다)
    private static void BuildFarmPlotWetOverlay(LowPolyMeshBuilder b)
    {
        b.AddBox(StylizedColor.SoilWet, new Vector3(0f, 0.0815f, 0f), new Vector3(0.9f, 0.003f, 0.9f));
        b.AddWedge(StylizedColor.SoilWet, new Vector3(0f, 0.1115f, -0.22f), new Vector3(0.83f, 0.061f, 0.305f));
        b.AddWedge(StylizedColor.SoilWet, new Vector3(0f, 0.1115f, 0.22f), new Vector3(0.83f, 0.061f, 0.305f));
    }

    // 81일차: 수확 가능한 밭 위에 떠 있는 빛나는 마름모 (기준점이 중심)
    private static void BuildReadySparkle(LowPolyMeshBuilder b)
    {
        b.AddCone(StylizedColor.ReadyGlow, Vector3.zero, 0.06f, 0.11f, 4);
        b.Push(Vector3.zero, Quaternion.Euler(180f, 0f, 0f), Vector3.one);
        b.AddCone(StylizedColor.ReadyGlow, Vector3.zero, 0.06f, 0.11f, 4);
        b.Pop();
    }

    // ---------------------------------------------------------------- 공통 성장 단계

    private static void BuildCropSeeded(LowPolyMeshBuilder b)
    {
        ForEachPlantSpot(b, index =>
        {
            Vector3 spot = Vector3.zero;
            b.AddLowPolySphere(StylizedColor.Dirt, spot, new Vector3(0.07f, 0.035f, 0.07f), 1, 0.15f, 230 + index);
            b.AddLowPolySphere(StylizedColor.WoodLight, spot + new Vector3(0.015f, 0.03f, 0.01f), new Vector3(0.012f, 0.008f, 0.01f), 0, 0f, index);
            b.AddLowPolySphere(StylizedColor.WoodLight, spot + new Vector3(-0.012f, 0.028f, -0.014f), new Vector3(0.011f, 0.008f, 0.009f), 0, 0f, index + 4);
        });
    }

    private static void BuildCropSprout(LowPolyMeshBuilder b)
    {
        ForEachPlantSpot(b, index =>
        {
            Vector3 spot = Vector3.zero;
            float yaw = index * 47f;
            b.AddLowPolySphere(StylizedColor.SoilTilled, spot, new Vector3(0.05f, 0.02f, 0.05f), 1, 0.15f, 240 + index);
            b.AddLimb(StylizedColor.CropGreen, spot, spot + new Vector3(0f, 0.08f, 0f), 0.008f, 0.006f, 4);
            AddLeaf(b, StylizedColor.LeafLight, spot + new Vector3(0f, 0.078f, 0f), yaw, 25f, 0.05f, 0.022f, index);
            AddLeaf(b, StylizedColor.LeafLight, spot + new Vector3(0f, 0.078f, 0f), yaw + 180f, 25f, 0.05f, 0.022f, index + 10);
        });
    }

    // 네 포기 자리마다 원점 기준으로 그린 작물을 PlantScale 크기로 배치한다
    private static void ForEachPlantSpot(LowPolyMeshBuilder b, System.Action<int> plant)
    {
        for (int index = 0; index < CropPlantSpots.Length; index++)
        {
            b.Push(CropPlantSpots[index], Quaternion.identity, Vector3.one * PlantScale);
            plant(index);
            b.Pop();
        }
    }

    // 잎 한 장 : 밑동에서 앞(+Z) 방향으로 뻗고 pitch 만큼 위로 들린다
    private static void AddLeaf(LowPolyMeshBuilder b, StylizedColor color, Vector3 basePosition, float yaw, float pitch, float length, float width, int seed)
    {
        b.Push(basePosition, Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(-pitch, 0f, 0f), Vector3.one);
        b.AddLowPolySphere(color, new Vector3(0f, 0f, length * 0.5f), new Vector3(width, 0.01f, length * 0.5f), 1, 0.05f, seed);
        b.Pop();
    }

    private static void AddLeafRosette(LowPolyMeshBuilder b, Vector3 basePosition, int count, float pitch, float length, float width, StylizedColor main, StylizedColor alt, float yawOffset, int seed)
    {
        for (int index = 0; index < count; index++)
        {
            StylizedColor color = index % 2 == 0 ? main : alt;
            AddLeaf(b, color, basePosition, yawOffset + index * 360f / count, pitch, length, width, seed + index);
        }
    }

    private static void AddSmallFlower(LowPolyMeshBuilder b, Vector3 center, float size, StylizedColor petal, int seed)
    {
        for (int index = 0; index < 5; index++)
        {
            float angle = index * Mathf.PI * 2f / 5f;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * size;
            b.AddLowPolySphere(petal, center + offset, new Vector3(size * 0.8f, size * 0.3f, size * 0.8f), 0, 0f, seed + index);
        }

        b.AddLowPolySphere(StylizedColor.FlowerYellow, center + new Vector3(0f, size * 0.2f, 0f), Vector3.one * size * 0.6f, 0, 0f, seed + 5);
    }

    // ---------------------------------------------------------------- 작물별 성장 단계

    private static void BuildPotatoPlant(LowPolyMeshBuilder b, bool mature)
    {
        ForEachPlantSpot(b, index =>
        {
            Vector3 spot = Vector3.zero;
            float yaw = index * 31f;
            b.AddLowPolySphere(StylizedColor.SoilTilled, spot, new Vector3(0.08f, 0.03f, 0.08f), 1, 0.15f, 250 + index);

            if (!mature)
            {
                b.AddLimb(StylizedColor.CropGreen, spot, spot + new Vector3(0f, 0.07f, 0f), 0.012f, 0.008f, 4);
                AddLeafRosette(b, spot + new Vector3(0f, 0.04f, 0f), 5, 35f, 0.12f, 0.045f, StylizedColor.Leaf, StylizedColor.LeafDark, yaw, 260 + index * 10);
                return;
            }

            b.AddLimb(StylizedColor.CropGreen, spot, spot + new Vector3(0f, 0.16f, 0f), 0.016f, 0.01f, 5);
            AddLeafRosette(b, spot + new Vector3(0f, 0.03f, 0f), 6, 22f, 0.17f, 0.06f, StylizedColor.LeafDark, StylizedColor.Leaf, yaw, 300 + index * 10);
            AddLeafRosette(b, spot + new Vector3(0f, 0.09f, 0f), 4, 50f, 0.13f, 0.05f, StylizedColor.Leaf, StylizedColor.LeafLight, yaw + 45f, 340 + index * 10);
            AddSmallFlower(b, spot + new Vector3(0.02f, 0.17f, 0f), 0.014f, StylizedColor.White, 380 + index * 10);

            // 흙 위로 드러난 감자
            b.AddLowPolySphere(StylizedColor.Potato, spot + new Vector3(0.07f, 0.005f, 0.05f), new Vector3(0.04f, 0.028f, 0.034f), 1, 0.1f, 420 + index);
            b.AddLowPolySphere(StylizedColor.Potato, spot + new Vector3(-0.06f, 0f, -0.06f), new Vector3(0.032f, 0.024f, 0.03f), 1, 0.1f, 430 + index);
        });
    }

    private static void BuildStrawberryPlant(LowPolyMeshBuilder b, bool mature)
    {
        ForEachPlantSpot(b, index =>
        {
            Vector3 spot = Vector3.zero;
            float yaw = index * 53f;
            b.AddLowPolySphere(StylizedColor.SoilTilled, spot, new Vector3(0.08f, 0.025f, 0.08f), 1, 0.15f, 450 + index);
            float length = mature ? 0.11f : 0.08f;
            AddLeafRosette(b, spot + new Vector3(0f, 0.03f, 0f), 6, 20f, length, 0.05f, StylizedColor.LeafDark, StylizedColor.Leaf, yaw, 460 + index * 10);
            AddLeafRosette(b, spot + new Vector3(0f, 0.035f, 0f), 3, 45f, length * 0.8f, 0.045f, StylizedColor.Leaf, StylizedColor.LeafLight, yaw + 30f, 500 + index * 10);

            if (!mature)
            {
                AddSmallFlower(b, spot + new Vector3(0.03f, 0.08f, 0.02f), 0.012f, StylizedColor.White, 520 + index * 10);
                return;
            }

            AddSmallFlower(b, spot + new Vector3(-0.03f, 0.09f, 0.02f), 0.012f, StylizedColor.White, 540 + index * 10);

            for (int berry = 0; berry < 3; berry++)
            {
                float angle = (yaw + berry * 120f) * Mathf.Deg2Rad;
                Vector3 position = spot + new Vector3(Mathf.Cos(angle) * 0.1f, 0.03f, Mathf.Sin(angle) * 0.1f);
                b.AddLimb(StylizedColor.CropGreen, spot + new Vector3(0f, 0.05f, 0f), position + new Vector3(0f, 0.03f, 0f), 0.004f, 0.004f, 3);
                AddStrawberry(b, position, 0.6f, 0f, 560 + index * 10 + berry);
            }
        });
    }

    // 세워 둔 딸기 한 알 (꼭지가 위)
    private static void AddStrawberry(LowPolyMeshBuilder b, Vector3 bottom, float scale, float roll, int seed)
    {
        b.Push(bottom, Quaternion.Euler(0f, 0f, roll), Vector3.one * scale);
        b.AddLowPolySphere(StylizedColor.Strawberry, new Vector3(0f, 0.075f, 0f), new Vector3(0.05f, 0.035f, 0.05f), 1, 0.03f, seed);
        b.Push(new Vector3(0f, 0.07f, 0f), Quaternion.Euler(180f, 0f, 0f), Vector3.one);
        b.AddCone(StylizedColor.Strawberry, Vector3.zero, 0.05f, 0.07f, 8);
        b.Pop();

        for (int index = 0; index < 5; index++)
        {
            AddLeaf(b, StylizedColor.LeafDark, new Vector3(0f, 0.105f, 0f), index * 72f, -15f, 0.035f, 0.012f, seed + index);
        }

        b.AddLimb(StylizedColor.CropGreen, new Vector3(0f, 0.1f, 0f), new Vector3(0.005f, 0.13f, 0f), 0.005f, 0.004f, 3);
        b.Pop();
    }

    private static void BuildTomatoPlant(LowPolyMeshBuilder b, bool mature)
    {
        float height = mature ? 0.42f : 0.26f;

        ForEachPlantSpot(b, index =>
        {
            Vector3 spot = Vector3.zero;
            float yaw = index * 67f;
            b.AddLowPolySphere(StylizedColor.SoilTilled, spot, new Vector3(0.07f, 0.025f, 0.07f), 1, 0.15f, 600 + index);

            // 지지대와 줄기
            Vector3 stake = spot + new Vector3(-0.04f, 0f, 0f);
            b.AddBox(StylizedColor.WoodLight, stake + new Vector3(0f, height * 0.55f, 0f), new Vector3(0.016f, height * 1.1f, 0.016f));
            b.AddTorus(StylizedColor.Rope, stake + new Vector3(0.02f, height * 0.7f, 0f), 0.026f, 0.005f, 6, 3);
            b.AddLimb(StylizedColor.CropGreen, spot, spot + new Vector3(0.005f, height * 0.95f, 0.01f), 0.012f, 0.007f, 5);

            int levels = mature ? 5 : 3;

            for (int level = 0; level < levels; level++)
            {
                float y = 0.05f + level * (height * 0.85f / levels);
                float leafYaw = yaw + level * 137f;
                AddLeaf(b, StylizedColor.LeafDark, spot + new Vector3(0f, y, 0f), leafYaw, 30f, 0.09f, 0.035f, 610 + index * 10 + level);
                AddLeaf(b, StylizedColor.Leaf, spot + new Vector3(0f, y + 0.02f, 0f), leafYaw + 160f, 35f, 0.075f, 0.03f, 650 + index * 10 + level);
            }

            if (!mature)
            {
                AddSmallFlower(b, spot + new Vector3(0.03f, height * 0.8f, 0.02f), 0.01f, StylizedColor.FlowerYellow, 690 + index * 10);
                return;
            }

            Vector3[] fruits =
            {
                new Vector3(0.05f, 0.14f, 0.03f), new Vector3(-0.02f, 0.2f, 0.05f),
                new Vector3(0.04f, 0.27f, -0.04f), new Vector3(0.02f, 0.33f, 0.04f)
            };

            for (int fruit = 0; fruit < fruits.Length; fruit++)
            {
                StylizedColor color = fruit == fruits.Length - 1 ? StylizedColor.LeafLight : StylizedColor.Tomato;
                Vector3 position = spot + fruits[fruit];
                b.AddLowPolySphere(color, position, new Vector3(0.03f, 0.027f, 0.03f), 1, 0.03f, 700 + index * 10 + fruit);
                AddLeaf(b, StylizedColor.LeafDark, position + new Vector3(0f, 0.025f, 0f), fruit * 70f, -10f, 0.02f, 0.008f, 740 + fruit);
            }
        });
    }

    private static void BuildPumpkinPlant(LowPolyMeshBuilder b, bool mature)
    {
        // 호박은 한 칸에 한 포기를 크게 키운다
        b.AddLowPolySphere(StylizedColor.SoilTilled, Vector3.zero, new Vector3(0.14f, 0.03f, 0.14f), 1, 0.15f, 800);

        Vector3[] vine =
        {
            new Vector3(0f, 0.02f, 0f), new Vector3(0.12f, 0.03f, 0.08f), new Vector3(0.24f, 0.02f, 0.02f),
            new Vector3(0.3f, 0.03f, -0.12f), new Vector3(0.18f, 0.02f, -0.26f)
        };

        for (int index = 1; index < vine.Length; index++)
        {
            b.AddLimb(StylizedColor.CropGreen, vine[index - 1], vine[index], 0.012f, 0.01f, 4);
        }

        b.AddLimb(StylizedColor.CropGreen, new Vector3(0f, 0.02f, 0f), new Vector3(-0.2f, 0.03f, 0.18f), 0.012f, 0.009f, 4);
        b.AddLimb(StylizedColor.CropGreen, new Vector3(-0.2f, 0.03f, 0.18f), new Vector3(-0.3f, 0.02f, 0.02f), 0.009f, 0.007f, 4);

        float leafLength = mature ? 0.22f : 0.17f;
        Vector3[] leafSpots =
        {
            new Vector3(0f, 0.03f, 0f), new Vector3(0.24f, 0.03f, 0.02f), new Vector3(-0.2f, 0.03f, 0.18f),
            new Vector3(0.18f, 0.03f, -0.26f), new Vector3(-0.3f, 0.03f, 0.02f)
        };

        for (int index = 0; index < leafSpots.Length; index++)
        {
            StylizedColor color = index % 2 == 0 ? StylizedColor.LeafDark : StylizedColor.Leaf;
            AddLeaf(b, color, leafSpots[index], index * 75f + 20f, 30f, leafLength, leafLength * 0.65f, 810 + index);
            AddLeaf(b, StylizedColor.Leaf, leafSpots[index], index * 75f + 150f, 45f, leafLength * 0.7f, leafLength * 0.45f, 830 + index);
        }

        if (!mature)
        {
            b.AddLowPolySphere(StylizedColor.LeafLight, new Vector3(0.28f, 0.05f, -0.1f), new Vector3(0.05f, 0.04f, 0.05f), 1, 0.05f, 850);
            b.Push(new Vector3(0.12f, 0.05f, 0.1f), Quaternion.Euler(-20f, 0f, 0f), Vector3.one);
            b.AddFrustum(StylizedColor.FlowerYellow, Vector3.zero, 0.008f, 0.04f, 0.06f, 5, true, false);
            b.Pop();
            return;
        }

        AddPumpkin(b, new Vector3(0.06f, 0f, -0.08f), 0.2f, 860);
        b.AddLowPolySphere(StylizedColor.LeafLight, new Vector3(-0.28f, 0.04f, 0.04f), new Vector3(0.04f, 0.035f, 0.04f), 1, 0.05f, 870);
    }

    // 골이 진 호박 (바닥 중심 기준, radius는 가로 반지름)
    private static void AddPumpkin(LowPolyMeshBuilder b, Vector3 bottom, float radius, int seed)
    {
        const int ribs = 8;
        float height = radius * 1.35f;

        for (int index = 0; index < ribs; index++)
        {
            float angle = index * Mathf.PI * 2f / ribs;
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            Vector3 center = bottom + direction * radius * 0.42f + Vector3.up * height * 0.5f;
            b.Push(center, Quaternion.LookRotation(direction, Vector3.up), Vector3.one);
            b.AddLowPolySphere(StylizedColor.Pumpkin, Vector3.zero, new Vector3(radius * 0.42f, height * 0.5f, radius * 0.58f), 1, 0.02f, seed + index);
            b.Pop();
        }

        b.AddLowPolySphere(StylizedColor.Pumpkin, bottom + Vector3.up * height * 0.5f, new Vector3(radius * 0.8f, height * 0.48f, radius * 0.8f), 1, 0f, seed + ribs);
        Vector3 top = bottom + Vector3.up * height * 0.95f;
        b.AddLimb(StylizedColor.BarkDark, top, top + new Vector3(0.02f, radius * 0.3f, 0.01f), radius * 0.09f, radius * 0.06f, 5);
        b.AddLimb(StylizedColor.BarkDark, top + new Vector3(0.02f, radius * 0.3f, 0.01f), top + new Vector3(radius * 0.18f, radius * 0.36f, 0.02f), radius * 0.06f, radius * 0.04f, 5);
        AddLeaf(b, StylizedColor.Leaf, top + new Vector3(0f, radius * 0.05f, 0f), 60f, 15f, radius * 0.5f, radius * 0.3f, seed + 20);
    }

    private static void BuildRadishPlant(LowPolyMeshBuilder b, bool mature)
    {
        ForEachPlantSpot(b, index =>
        {
            Vector3 spot = Vector3.zero;
            float yaw = index * 41f;
            b.AddLowPolySphere(StylizedColor.SoilTilled, spot, new Vector3(0.07f, 0.025f, 0.07f), 1, 0.15f, 900 + index);

            if (!mature)
            {
                AddLeafRosette(b, spot + new Vector3(0f, 0.02f, 0f), 5, 55f, 0.1f, 0.028f, StylizedColor.LeafLight, StylizedColor.Leaf, yaw, 910 + index * 10);
                return;
            }

            // 흙 위로 올라온 흰 무 머리
            b.AddFrustum(StylizedColor.White, spot + new Vector3(0f, -0.01f, 0f), 0.04f, 0.036f, 0.05f, 8, false, false);
            b.AddFrustum(StylizedColor.CropGreen, spot + new Vector3(0f, 0.04f, 0f), 0.036f, 0.02f, 0.02f, 8, false, true);
            AddLeafRosette(b, spot + new Vector3(0f, 0.055f, 0f), 6, 62f, 0.17f, 0.034f, StylizedColor.LeafLight, StylizedColor.Leaf, yaw, 950 + index * 10);
        });
    }

    // ---------------------------------------------------------------- 수확물

    private static void BuildPotatoItem(LowPolyMeshBuilder b)
    {
        b.AddLowPolySphere(StylizedColor.Potato, new Vector3(0f, 0.08f, 0f), new Vector3(0.13f, 0.08f, 0.095f), 1, 0.12f, 1001);
        b.AddLowPolySphere(StylizedColor.Potato, new Vector3(0.15f, 0.06f, 0.1f), new Vector3(0.09f, 0.06f, 0.07f), 1, 0.12f, 1002);
        b.AddLowPolySphere(StylizedColor.Potato, new Vector3(-0.12f, 0.055f, 0.12f), new Vector3(0.08f, 0.055f, 0.065f), 1, 0.12f, 1003);

        Vector3[] eyes =
        {
            new Vector3(0.06f, 0.14f, 0.03f), new Vector3(-0.05f, 0.13f, -0.04f),
            new Vector3(0.17f, 0.11f, 0.12f), new Vector3(-0.1f, 0.1f, 0.14f)
        };

        for (int index = 0; index < eyes.Length; index++)
        {
            b.AddLowPolySphere(StylizedColor.SoilTilled, eyes[index], Vector3.one * 0.012f, 0, 0f, index);
        }
    }

    private static void BuildStrawberryItem(LowPolyMeshBuilder b)
    {
        // 누워 있는 딸기 세 알
        AddLyingStrawberry(b, new Vector3(0f, 0.07f, 0f), 0f, 1011);
        AddLyingStrawberry(b, new Vector3(0.16f, 0.07f, 0.1f), 130f, 1021);
        AddLyingStrawberry(b, new Vector3(-0.14f, 0.07f, 0.12f), 250f, 1031);
    }

    private static void AddLyingStrawberry(LowPolyMeshBuilder b, Vector3 center, float yaw, int seed)
    {
        b.Push(center, Quaternion.Euler(0f, yaw, 0f), Vector3.one);
        AddStrawberry(b, new Vector3(-0.07f, 0f, 0f), 1.3f, -80f, seed);
        b.Pop();
    }

    private static void BuildTomatoItem(LowPolyMeshBuilder b)
    {
        AddTomato(b, new Vector3(0f, 0f, 0f), 0.13f, 1041);
        AddTomato(b, new Vector3(0.2f, 0f, 0.12f), 0.1f, 1051);
    }

    private static void AddTomato(LowPolyMeshBuilder b, Vector3 bottom, float radius, int seed)
    {
        Vector3 center = bottom + Vector3.up * radius * 0.82f;
        b.AddLowPolySphere(StylizedColor.Tomato, center, new Vector3(radius, radius * 0.82f, radius), 1, 0.03f, seed);

        for (int index = 0; index < 5; index++)
        {
            AddLeaf(b, StylizedColor.LeafDark, center + Vector3.up * radius * 0.8f, index * 72f, -8f, radius * 0.45f, radius * 0.13f, seed + index);
        }

        b.AddLimb(StylizedColor.CropGreen, center + Vector3.up * radius * 0.78f, center + new Vector3(0.01f, radius * 1.05f, 0f), radius * 0.07f, radius * 0.05f, 4);
    }

    private static void BuildRadishItem(LowPolyMeshBuilder b)
    {
        // 옆으로 눕힌 겨울무 (뿌리 끝이 -X, 잎이 +X)
        b.Push(new Vector3(0f, 0.07f, 0f), Quaternion.Euler(0f, 0f, -90f), Vector3.one);
        b.AddFrustum(StylizedColor.White, new Vector3(0f, -0.3f, 0f), 0.012f, 0.068f, 0.34f, 8, true, false);
        b.AddFrustum(StylizedColor.CropGreen, new Vector3(0f, 0.04f, 0f), 0.068f, 0.045f, 0.04f, 8, false, true);
        AddLeafRosette(b, new Vector3(0f, 0.075f, 0f), 4, 70f, 0.2f, 0.04f, StylizedColor.LeafLight, StylizedColor.Leaf, 20f, 1061);
        b.Pop();
    }

    // ---------------------------------------------------------------- 씨앗 봉투

    private static void BuildSeedPouch(LowPolyMeshBuilder b, StylizedColor band, StylizedColor picture, int seed)
    {
        b.AddBeveledBox(StylizedColor.SeedPaper, new Vector3(0f, 0.16f, 0f), new Vector3(0.24f, 0.32f, 0.08f), 0.02f);
        b.AddWedge(StylizedColor.WoodLight, new Vector3(0f, 0.34f, 0f), new Vector3(0.23f, 0.04f, 0.07f));
        b.AddBox(band, new Vector3(0f, 0.27f, 0f), new Vector3(0.245f, 0.05f, 0.085f));

        // 앞면 그림 (작물 색 원과 잎)
        b.AddLowPolySphere(picture, new Vector3(0f, 0.14f, 0.042f), new Vector3(0.06f, 0.06f, 0.012f), 1, 0.03f, seed);
        b.AddLowPolySphere(StylizedColor.Leaf, new Vector3(0.035f, 0.2f, 0.043f), new Vector3(0.03f, 0.014f, 0.008f), 0, 0f, seed + 1);

        // 바닥에 흘린 씨앗
        System.Random random = new System.Random(seed);

        for (int index = 0; index < 6; index++)
        {
            Vector3 position = new Vector3(Rand(random, -0.12f, 0.16f), 0.008f, Rand(random, 0.06f, 0.16f));
            b.AddLowPolySphere(StylizedColor.WoodLight, position, new Vector3(0.014f, 0.008f, 0.01f), 0, 0f, seed + index + 2);
        }
    }

    // ---------------------------------------------------------------- 농사 도구

    // 손잡이는 +Y 방향, 날은 +X 방향
    private static void BuildHoe(LowPolyMeshBuilder b)
    {
        b.AddLimb(StylizedColor.WoodLight, Vector3.zero, new Vector3(0f, 1f, 0f), 0.03f, 0.027f, 6);
        b.AddTorus(StylizedColor.Leather, new Vector3(0f, 0.14f, 0f), 0.037f, 0.012f, 8, 3);
        b.AddBox(StylizedColor.IronDark, new Vector3(0.015f, 0.97f, 0f), new Vector3(0.07f, 0.06f, 0.06f));
        b.AddLimb(StylizedColor.IronDark, new Vector3(0.05f, 0.97f, 0f), new Vector3(0.14f, 0.95f, 0f), 0.016f, 0.014f, 5);
        b.AddBox(StylizedColor.Iron, new Vector3(0.155f, 0.87f, 0f), new Vector3(0.024f, 0.17f, 0.2f));
        b.AddBox(StylizedColor.StoneLight, new Vector3(0.155f, 0.78f, 0f), new Vector3(0.018f, 0.012f, 0.2f));
    }

    private static void BuildWateringCan(LowPolyMeshBuilder b)
    {
        b.AddFrustum(StylizedColor.ClothGreen, Vector3.zero, 0.15f, 0.13f, 0.26f, 10);
        b.AddTorus(StylizedColor.IronDark, new Vector3(0f, 0.02f, 0f), 0.15f, 0.012f, 10, 3);
        b.AddTorus(StylizedColor.IronDark, new Vector3(0f, 0.25f, 0f), 0.132f, 0.012f, 10, 3);
        b.AddFrustum(StylizedColor.ClothGreen, new Vector3(0f, 0.26f, 0f), 0.13f, 0.05f, 0.04f, 10, false, true);

        // 물줄기 관과 꼭지
        Vector3 spoutStart = new Vector3(0.1f, 0.05f, 0f);
        Vector3 spoutEnd = new Vector3(0.36f, 0.3f, 0f);
        b.AddLimb(StylizedColor.ClothGreen, spoutStart, spoutEnd, 0.03f, 0.016f, 6);
        b.Push(spoutEnd, Quaternion.FromToRotation(Vector3.up, (spoutEnd - spoutStart).normalized), Vector3.one);
        b.AddFrustum(StylizedColor.Copper, Vector3.zero, 0.02f, 0.045f, 0.04f, 8);
        b.Pop();

        // 위 손잡이와 뒤 손잡이
        b.Push(new Vector3(-0.02f, 0.26f, 0f), Quaternion.Euler(90f, 0f, 0f), Vector3.one);
        b.AddTorus(StylizedColor.IronDark, Vector3.zero, 0.1f, 0.014f, 12, 3);
        b.Pop();
        b.AddLimb(StylizedColor.IronDark, new Vector3(-0.14f, 0.22f, 0f), new Vector3(-0.2f, 0.17f, 0f), 0.014f, 0.014f, 4);
        b.AddLimb(StylizedColor.IronDark, new Vector3(-0.2f, 0.17f, 0f), new Vector3(-0.14f, 0.06f, 0f), 0.014f, 0.014f, 4);
    }

    // ---------------------------------------------------------------- 소품

    private static void BuildScarecrow(LowPolyMeshBuilder b)
    {
        b.AddBox(StylizedColor.WoodDark, new Vector3(0f, 0.8f, 0f), new Vector3(0.08f, 1.6f, 0.08f));
        b.AddBox(StylizedColor.WoodDark, new Vector3(0f, 1.25f, 0f), new Vector3(1.1f, 0.07f, 0.07f));

        // 옷과 짚
        b.AddBeveledBox(StylizedColor.ClothBlue, new Vector3(0f, 1.12f, 0f), new Vector3(0.42f, 0.46f, 0.2f), 0.05f);
        b.AddBeveledBox(StylizedColor.ClothBlue, new Vector3(0.36f, 1.24f, 0f), new Vector3(0.32f, 0.14f, 0.16f), 0.03f);
        b.AddBeveledBox(StylizedColor.ClothBlue, new Vector3(-0.36f, 1.24f, 0f), new Vector3(0.32f, 0.14f, 0.16f), 0.03f);
        b.AddBox(StylizedColor.ClothRed, new Vector3(0.12f, 1.05f, 0.101f), new Vector3(0.1f, 0.1f, 0.01f));
        b.AddBox(StylizedColor.Rope, new Vector3(0f, 0.9f, 0f), new Vector3(0.44f, 0.04f, 0.22f));

        for (int index = 0; index < 5; index++)
        {
            float x = -0.16f + index * 0.08f;
            b.AddLimb(StylizedColor.FlowerYellow, new Vector3(x, 0.9f, 0.02f), new Vector3(x * 1.4f, 0.74f, 0.05f), 0.012f, 0.004f, 3);
        }

        for (int side = -1; side <= 1; side += 2)
        {
            for (int index = 0; index < 3; index++)
            {
                Vector3 start = new Vector3(side * 0.52f, 1.24f, (index - 1) * 0.04f);
                b.AddLimb(StylizedColor.FlowerYellow, start, start + new Vector3(side * 0.1f, -0.06f + index * 0.03f, (index - 1) * 0.03f), 0.012f, 0.004f, 3);
            }
        }

        // 자루 머리와 밀짚모자
        b.AddLowPolySphere(StylizedColor.Sand, new Vector3(0f, 1.5f, 0f), new Vector3(0.17f, 0.18f, 0.16f), 1, 0.06f, 1101);
        b.AddBox(StylizedColor.Black, new Vector3(-0.06f, 1.53f, 0.15f), new Vector3(0.04f, 0.04f, 0.02f));
        b.AddBox(StylizedColor.Black, new Vector3(0.06f, 1.53f, 0.15f), new Vector3(0.04f, 0.04f, 0.02f));
        b.AddBox(StylizedColor.BarkDark, new Vector3(0f, 1.45f, 0.155f), new Vector3(0.1f, 0.015f, 0.02f));
        b.AddTorus(StylizedColor.Rope, new Vector3(0f, 1.38f, 0f), 0.12f, 0.02f, 8, 3);
        b.AddFrustum(StylizedColor.FlowerYellow, new Vector3(0f, 1.6f, 0f), 0.32f, 0.3f, 0.025f, 12);
        b.AddFrustum(StylizedColor.FlowerYellow, new Vector3(0f, 1.625f, 0f), 0.17f, 0.12f, 0.12f, 12);
        b.AddTorus(StylizedColor.ClothRed, new Vector3(0f, 1.65f, 0f), 0.16f, 0.015f, 12, 3);
    }
}
