using UnityEngine;

public static partial class StylizedModelLibrary
{
    private static void RegisterItems()
    {
        Register("item_wood_bundle", FitMode.UniformLargest, BuildWoodBundle);
        Register("item_stone", FitMode.UniformLargest, BuildStoneChunk);
        Register("item_iron_ore", FitMode.UniformLargest, BuildIronOre);
        Register("item_plant_fiber", FitMode.UniformLargest, BuildPlantFiber);
        Register("item_mushroom", FitMode.UniformLargest, BuildSingleMushroom);
        Register("item_apple", FitMode.UniformLargest, BuildApple);
        Register("item_berry", FitMode.UniformLargest, BuildBerries);
        Register("item_bandage", FitMode.UniformLargest, BuildBandage);
        Register("item_cloth", FitMode.UniformLargest, BuildClothStack);
        Register("item_shirt", FitMode.UniformLargest, BuildShirt);
        Register("item_cap", FitMode.UniformLargest, BuildClothCap);
        Register("item_work_hat", FitMode.UniformLargest, BuildWorkHat);
        Register("item_backpack", FitMode.UniformLargest, BuildBackpack);
        Register("item_water_bottle", FitMode.UniformLargest, BuildCanteen);
        Register("item_herbal_tea", FitMode.UniformLargest, BuildTeaCup);
        Register("item_sack", FitMode.UniformLargest, BuildSack);
        Register("tool_stone_axe", FitMode.UniformLargest, BuildStoneAxe);
        Register("tool_iron_axe", FitMode.UniformLargest, BuildIronAxe);
        Register("tool_pickaxe", FitMode.UniformLargest, BuildPickaxe);
        Register("tool_bow", FitMode.UniformLargest, BuildBow);
        Register("projectile_arrow", FitMode.UniformLargest, BuildArrow);
        Register("projectile_spit", FitMode.UniformLargest, BuildSpitGlob);
    }

    private static void BuildWoodBundle(LowPolyMeshBuilder b)
    {
        AddLog(b, new Vector3(0f, 0.12f, -0.13f), 0.8f, 0.12f, 0f, StylizedColor.Bark, StylizedColor.WoodLight);
        AddLog(b, new Vector3(0f, 0.12f, 0.13f), 0.78f, 0.12f, 4f, StylizedColor.BarkDark, StylizedColor.WoodLight);
        AddLog(b, new Vector3(0.02f, 0.33f, 0f), 0.82f, 0.12f, -3f, StylizedColor.Bark, StylizedColor.WoodLight);
        b.Push(new Vector3(-0.18f, 0.2f, 0f), Euler(0f, 0f, 90f), Vector3.one);
        b.AddTorus(StylizedColor.Rope, Vector3.zero, 0.26f, 0.025f, 10, 4);
        b.Pop();
        b.Push(new Vector3(0.18f, 0.2f, 0f), Euler(0f, 0f, 90f), Vector3.one);
        b.AddTorus(StylizedColor.Rope, Vector3.zero, 0.26f, 0.025f, 10, 4);
        b.Pop();
    }

    private static void BuildStoneChunk(LowPolyMeshBuilder b)
    {
        b.AddLowPolySphere(StylizedColor.Stone, new Vector3(0f, 0.18f, 0f), new Vector3(0.26f, 0.18f, 0.22f), 1, 0.2f, 61);
        b.AddLowPolySphere(StylizedColor.StoneLight, new Vector3(0.2f, 0.1f, 0.12f), new Vector3(0.12f, 0.1f, 0.11f), 0, 0.2f, 62);
    }

    private static void BuildIronOre(LowPolyMeshBuilder b)
    {
        b.AddLowPolySphere(StylizedColor.StoneDark, new Vector3(0f, 0.2f, 0f), new Vector3(0.28f, 0.2f, 0.24f), 1, 0.22f, 71);
        b.AddLowPolySphere(StylizedColor.Ore, new Vector3(0.14f, 0.3f, 0.1f), new Vector3(0.09f, 0.07f, 0.08f), 0, 0.1f, 72);
        b.AddLowPolySphere(StylizedColor.Ore, new Vector3(-0.12f, 0.28f, -0.08f), new Vector3(0.08f, 0.07f, 0.08f), 0, 0.1f, 73);
        b.AddLowPolySphere(StylizedColor.Iron, new Vector3(0.02f, 0.38f, -0.05f), new Vector3(0.06f, 0.05f, 0.06f), 0, 0.1f, 74);
    }

    private static void BuildPlantFiber(LowPolyMeshBuilder b)
    {
        for (int index = 0; index < 9; index++)
        {
            float z = -0.12f + (index % 3) * 0.12f;
            float y = 0.05f + (index / 3) * 0.06f;
            StylizedColor color = index % 2 == 0 ? StylizedColor.Herb : StylizedColor.Grass;
            b.Push(new Vector3(0f, y, z), Euler(0f, index * 4f - 16f, 90f), Vector3.one);
            b.AddFrustum(color, new Vector3(0f, -0.4f, 0f), 0.03f, 0.012f, 0.8f, 4, false, false);
            b.Pop();
        }

        b.Push(new Vector3(0f, 0.12f, 0f), Euler(0f, 0f, 90f), Vector3.one);
        b.AddTorus(StylizedColor.Rope, Vector3.zero, 0.2f, 0.022f, 10, 4);
        b.Pop();
    }

    private static void BuildSingleMushroom(LowPolyMeshBuilder b)
    {
        AddMushroom(b, Vector3.zero, 1f, 0f);
    }

    private static void BuildApple(LowPolyMeshBuilder b)
    {
        b.AddLowPolySphere(StylizedColor.AppleRed, new Vector3(0f, 0.2f, 0f), new Vector3(0.21f, 0.19f, 0.21f), 1, 0.04f, 81);
        b.AddLimb(StylizedColor.Bark, new Vector3(0f, 0.36f, 0f), new Vector3(0.02f, 0.46f, 0.01f), 0.015f, 0.012f, 4);
        b.Push(new Vector3(0.02f, 0.42f, 0f), Euler(0f, 0f, -35f), Vector3.one);
        b.AddLowPolySphere(StylizedColor.Leaf, new Vector3(0.07f, 0f, 0f), new Vector3(0.08f, 0.015f, 0.04f), 0, 0f, 1);
        b.Pop();
    }

    private static void BuildBerries(LowPolyMeshBuilder b)
    {
        Vector3[] positions =
        {
            new Vector3(0f, 0.08f, 0f), new Vector3(0.12f, 0.07f, 0.05f), new Vector3(-0.1f, 0.07f, 0.07f),
            new Vector3(0.03f, 0.07f, -0.12f), new Vector3(0.02f, 0.18f, 0.02f), new Vector3(-0.08f, 0.16f, -0.05f)
        };

        for (int index = 0; index < positions.Length; index++)
        {
            b.AddLowPolySphere(StylizedColor.BerryPurple, positions[index], Vector3.one * 0.075f, 1, 0.04f, index);
        }

        b.AddLowPolySphere(StylizedColor.Leaf, new Vector3(0.06f, 0.26f, 0f), new Vector3(0.12f, 0.02f, 0.06f), 0, 0f, 9);
        b.AddLowPolySphere(StylizedColor.LeafDark, new Vector3(-0.06f, 0.25f, 0.02f), new Vector3(0.1f, 0.02f, 0.05f), 0, 0f, 10);
    }

    private static void BuildBandage(LowPolyMeshBuilder b)
    {
        b.Push(new Vector3(0f, 0.14f, 0f), Euler(90f, 0f, 0f), Vector3.one);
        b.AddCylinder(StylizedColor.ClothCream, new Vector3(0f, -0.12f, 0f), 0.14f, 0.24f, 10);
        b.AddCylinder(StylizedColor.White, new Vector3(0f, -0.121f, 0f), 0.05f, 0.242f, 8);
        b.Pop();
        // 풀린 끝 부분
        b.AddBox(StylizedColor.ClothCream, new Vector3(0.2f, 0.01f, 0f), new Vector3(0.3f, 0.02f, 0.22f));
        b.AddBox(StylizedColor.ClothRed, new Vector3(0.24f, 0.022f, 0f), new Vector3(0.08f, 0.005f, 0.06f));
        b.AddBox(StylizedColor.ClothRed, new Vector3(0.24f, 0.022f, 0f), new Vector3(0.025f, 0.006f, 0.16f));
    }

    private static void BuildClothStack(LowPolyMeshBuilder b)
    {
        b.AddBeveledBox(StylizedColor.ClothCream, new Vector3(0f, 0.05f, 0f), new Vector3(0.6f, 0.1f, 0.45f), 0.03f);
        b.AddBeveledBox(StylizedColor.ClothBlue, new Vector3(0.02f, 0.14f, -0.01f), new Vector3(0.56f, 0.08f, 0.42f), 0.03f);
        b.AddBeveledBox(StylizedColor.ClothRed, new Vector3(-0.01f, 0.21f, 0.01f), new Vector3(0.5f, 0.06f, 0.38f), 0.025f);
        b.AddBox(StylizedColor.Rope, new Vector3(0f, 0.12f, 0f), new Vector3(0.04f, 0.25f, 0.47f));
    }

    private static void BuildShirt(LowPolyMeshBuilder b)
    {
        // 바닥에 펼쳐 놓은 셔츠
        b.AddBeveledBox(StylizedColor.ClothBlue, new Vector3(0f, 0.03f, 0f), new Vector3(0.5f, 0.06f, 0.6f), 0.02f);
        b.Push(new Vector3(0.36f, 0.03f, 0.18f), Euler(0f, -30f, 0f), Vector3.one);
        b.AddBeveledBox(StylizedColor.ClothBlue, Vector3.zero, new Vector3(0.28f, 0.05f, 0.16f), 0.02f);
        b.Pop();
        b.Push(new Vector3(-0.36f, 0.03f, 0.18f), Euler(0f, 30f, 0f), Vector3.one);
        b.AddBeveledBox(StylizedColor.ClothBlue, Vector3.zero, new Vector3(0.28f, 0.05f, 0.16f), 0.02f);
        b.Pop();
        b.AddBox(StylizedColor.ClothCream, new Vector3(0f, 0.065f, 0.26f), new Vector3(0.18f, 0.01f, 0.06f));
        b.AddBox(StylizedColor.Leather, new Vector3(0f, 0.065f, -0.2f), new Vector3(0.52f, 0.012f, 0.05f));
    }

    private static void BuildClothCap(LowPolyMeshBuilder b)
    {
        b.AddFrustum(StylizedColor.ClothRed, Vector3.zero, 0.22f, 0.2f, 0.08f, 10, true, false);
        b.AddFrustum(StylizedColor.ClothRed, new Vector3(0f, 0.08f, 0f), 0.2f, 0.08f, 0.12f, 10, false, true);
        b.AddLowPolySphere(StylizedColor.ClothCream, new Vector3(0f, 0.22f, 0f), Vector3.one * 0.05f, 0, 0f, 1);
        b.AddBox(StylizedColor.ClothRed, new Vector3(0f, 0.02f, 0.26f), new Vector3(0.26f, 0.02f, 0.14f));
    }

    private static void BuildWorkHat(LowPolyMeshBuilder b)
    {
        b.AddFrustum(StylizedColor.FlowerYellow, Vector3.zero, 0.34f, 0.34f, 0.03f, 12);
        b.AddFrustum(StylizedColor.FlowerYellow, new Vector3(0f, 0.03f, 0f), 0.24f, 0.2f, 0.12f, 12, false, false);
        b.AddFrustum(StylizedColor.FlowerYellow, new Vector3(0f, 0.15f, 0f), 0.2f, 0.06f, 0.1f, 12, false, true);
        b.AddBox(StylizedColor.Gold, new Vector3(0f, 0.17f, 0f), new Vector3(0.05f, 0.07f, 0.34f));
        b.AddTorus(StylizedColor.Black, new Vector3(0f, 0.06f, 0f), 0.232f, 0.02f, 12, 3);
    }

    private static void BuildBackpack(LowPolyMeshBuilder b)
    {
        b.AddBeveledBox(StylizedColor.Leather, new Vector3(0f, 0.28f, 0f), new Vector3(0.46f, 0.56f, 0.26f), 0.06f);
        b.AddBeveledBox(StylizedColor.ClothGreen, new Vector3(0f, 0.5f, 0.02f), new Vector3(0.48f, 0.16f, 0.28f), 0.04f);
        b.AddBeveledBox(StylizedColor.Leather, new Vector3(0f, 0.2f, 0.16f), new Vector3(0.3f, 0.2f, 0.08f), 0.03f);
        b.AddBox(StylizedColor.Gold, new Vector3(0f, 0.44f, 0.17f), new Vector3(0.05f, 0.05f, 0.02f));
        b.AddBox(StylizedColor.BarkDark, new Vector3(0.14f, 0.3f, -0.14f), new Vector3(0.05f, 0.5f, 0.03f));
        b.AddBox(StylizedColor.BarkDark, new Vector3(-0.14f, 0.3f, -0.14f), new Vector3(0.05f, 0.5f, 0.03f));
        b.Push(new Vector3(0f, 0.62f, 0f), Euler(0f, 0f, 90f), Vector3.one);
        b.AddCylinder(StylizedColor.ClothRed, new Vector3(0f, -0.24f, 0f), 0.06f, 0.48f, 7);
        b.Pop();
    }

    private static void BuildCanteen(LowPolyMeshBuilder b)
    {
        b.Push(new Vector3(0f, 0.2f, 0f), Euler(90f, 0f, 0f), new Vector3(1f, 1f, 1f));
        b.AddCylinder(StylizedColor.IronDark, new Vector3(0f, -0.06f, 0f), 0.19f, 0.12f, 12);
        b.AddCylinder(StylizedColor.ClothBlue, new Vector3(0f, -0.065f, 0f), 0.16f, 0.13f, 12);
        b.Pop();
        b.AddCylinder(StylizedColor.Iron, new Vector3(0f, 0.38f, 0f), 0.04f, 0.06f, 8);
        b.AddCylinder(StylizedColor.BarkDark, new Vector3(0f, 0.44f, 0f), 0.05f, 0.04f, 8);
        b.Push(new Vector3(0f, 0.2f, 0f), Euler(90f, 0f, 0f), Vector3.one);
        b.AddTorus(StylizedColor.Leather, Vector3.zero, 0.24f, 0.015f, 12, 3);
        b.Pop();
    }

    private static void BuildTeaCup(LowPolyMeshBuilder b)
    {
        b.AddFrustum(StylizedColor.ClothCream, Vector3.zero, 0.12f, 0.17f, 0.2f, 10, true, false);
        b.AddDisc(StylizedColor.Herb, new Vector3(0f, 0.17f, 0f), 0.15f, 10);
        b.Push(new Vector3(0.17f, 0.1f, 0f), Euler(90f, 0f, 0f), Vector3.one);
        b.AddTorus(StylizedColor.ClothCream, Vector3.zero, 0.05f, 0.015f, 8, 3);
        b.Pop();
        b.AddFrustum(StylizedColor.WoodLight, new Vector3(0f, -0.02f, 0f), 0.22f, 0.22f, 0.02f, 10);
        b.AddLowPolySphere(StylizedColor.Leaf, new Vector3(-0.02f, 0.18f, 0.04f), new Vector3(0.06f, 0.01f, 0.03f), 0, 0f, 3);
    }

    private static void BuildSack(LowPolyMeshBuilder b)
    {
        b.AddLowPolySphere(StylizedColor.Sand, new Vector3(0f, 0.28f, 0f), new Vector3(0.3f, 0.3f, 0.26f), 1, 0.08f, 91);
        b.AddFrustum(StylizedColor.Sand, new Vector3(0f, 0.5f, 0f), 0.12f, 0.16f, 0.14f, 7, false, true);
        b.AddTorus(StylizedColor.Rope, new Vector3(0f, 0.55f, 0f), 0.12f, 0.02f, 8, 3);
    }

    // 손잡이는 +Y 방향, 날은 +X 방향
    private static void BuildStoneAxe(LowPolyMeshBuilder b)
    {
        b.AddLimb(StylizedColor.Bark, new Vector3(0f, 0f, 0f), new Vector3(0f, 0.9f, 0f), 0.035f, 0.03f, 6);
        b.AddLowPolySphere(StylizedColor.Stone, new Vector3(0.1f, 0.82f, 0f), new Vector3(0.16f, 0.1f, 0.05f), 1, 0.12f, 101);
        b.AddTorus(StylizedColor.Rope, new Vector3(0f, 0.8f, 0f), 0.045f, 0.015f, 8, 3);
        b.AddTorus(StylizedColor.Rope, new Vector3(0f, 0.86f, 0f), 0.045f, 0.015f, 8, 3);
        b.AddTorus(StylizedColor.Leather, new Vector3(0f, 0.12f, 0f), 0.042f, 0.014f, 8, 3);
    }

    private static void BuildIronAxe(LowPolyMeshBuilder b)
    {
        b.AddLimb(StylizedColor.WoodDark, new Vector3(0f, 0f, 0f), new Vector3(0f, 0.95f, 0f), 0.035f, 0.03f, 6);
        b.AddBox(StylizedColor.IronDark, new Vector3(0.02f, 0.84f, 0f), new Vector3(0.12f, 0.1f, 0.07f));
        b.Push(new Vector3(0.14f, 0.84f, 0f), Euler(0f, 0f, -90f), Vector3.one);
        b.AddWedge(StylizedColor.Iron, Vector3.zero, new Vector3(0.24f, 0.18f, 0.04f));
        b.Pop();
        b.AddTorus(StylizedColor.Leather, new Vector3(0f, 0.12f, 0f), 0.042f, 0.014f, 8, 3);
        b.AddTorus(StylizedColor.Leather, new Vector3(0f, 0.2f, 0f), 0.042f, 0.014f, 8, 3);
    }

    private static void BuildPickaxe(LowPolyMeshBuilder b)
    {
        b.AddLimb(StylizedColor.Bark, new Vector3(0f, 0f, 0f), new Vector3(0f, 0.95f, 0f), 0.035f, 0.03f, 6);
        b.AddBox(StylizedColor.IronDark, new Vector3(0f, 0.86f, 0f), new Vector3(0.09f, 0.09f, 0.08f));
        b.AddLimb(StylizedColor.Iron, new Vector3(0.03f, 0.86f, 0f), new Vector3(0.24f, 0.8f, 0f), 0.035f, 0.012f, 5);
        b.AddLimb(StylizedColor.Iron, new Vector3(-0.03f, 0.86f, 0f), new Vector3(-0.24f, 0.8f, 0f), 0.035f, 0.012f, 5);
        b.AddTorus(StylizedColor.Leather, new Vector3(0f, 0.12f, 0f), 0.042f, 0.014f, 8, 3);
    }

    // 세워 둔 활 (몸체는 +X 방향으로 휘고 시위는 X = 0)
    private static void BuildBow(LowPolyMeshBuilder b)
    {
        const int segments = 8;
        Vector3 previous = Vector3.zero;

        for (int index = 0; index <= segments; index++)
        {
            float t = index / (float)segments;
            float centered = t * 2f - 1f;
            Vector3 point = new Vector3(0.2f * (1f - centered * centered), 0.02f + t * 1.1f, 0f);

            if (index > 0)
            {
                float thickness = Mathf.Lerp(0.032f, 0.014f, Mathf.Abs(centered));
                StylizedColor color = Mathf.Abs(centered) < 0.2f ? StylizedColor.Leather : StylizedColor.WoodPlank;
                b.AddLimb(color, previous, point, thickness, thickness, 5);
            }

            previous = point;
        }

        b.AddLimb(StylizedColor.White, new Vector3(0.002f, 0.03f, 0f), new Vector3(0.002f, 1.11f, 0f), 0.005f, 0.005f, 3);
        b.AddTorus(StylizedColor.Gold, new Vector3(0.2f, 0.47f, 0f), 0.036f, 0.008f, 8, 3);
        b.AddTorus(StylizedColor.Gold, new Vector3(0.2f, 0.65f, 0f), 0.036f, 0.008f, 8, 3);
    }

    // 앞 방향(+Z)으로 날아가는 화살
    private static void BuildArrow(LowPolyMeshBuilder b)
    {
        b.AddLimb(StylizedColor.WoodLight, new Vector3(0f, 0f, -0.35f), new Vector3(0f, 0f, 0.3f), 0.012f, 0.012f, 4);
        b.Push(new Vector3(0f, 0f, 0.3f), Euler(90f, 0f, 0f), Vector3.one);
        b.AddCone(StylizedColor.Iron, Vector3.zero, 0.03f, 0.09f, 4);
        b.Pop();

        for (int index = 0; index < 3; index++)
        {
            b.Push(new Vector3(0f, 0f, -0.3f), Euler(0f, 0f, index * 120f), Vector3.one);
            b.AddQuad(StylizedColor.ClothRed, new Vector3(0f, 0.012f, -0.05f), new Vector3(0f, 0.05f, -0.03f), new Vector3(0f, 0.05f, 0.07f), new Vector3(0f, 0.012f, 0.09f));
            b.AddQuad(StylizedColor.ClothRed, new Vector3(0f, 0.012f, 0.09f), new Vector3(0f, 0.05f, 0.07f), new Vector3(0f, 0.05f, -0.03f), new Vector3(0f, 0.012f, -0.05f));
            b.Pop();
        }
    }

    private static void BuildSpitGlob(LowPolyMeshBuilder b)
    {
        b.AddLowPolySphere(StylizedColor.Slime, new Vector3(0f, 0f, 0f), new Vector3(0.18f, 0.16f, 0.24f), 1, 0.12f, 111);
        b.AddLowPolySphere(StylizedColor.SpitterSac, new Vector3(0.05f, 0.06f, -0.18f), Vector3.one * 0.07f, 0, 0.1f, 112);
        b.AddLowPolySphere(StylizedColor.SpitterSac, new Vector3(-0.04f, -0.05f, -0.26f), Vector3.one * 0.045f, 0, 0.1f, 113);
    }
}
