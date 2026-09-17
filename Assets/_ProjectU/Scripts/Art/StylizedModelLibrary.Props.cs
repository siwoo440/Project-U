using UnityEngine;

// 맵 장식용 소품
public static partial class StylizedModelLibrary
{
    private static void RegisterProps()
    {
        Register("prop_fence", FitMode.UniformLargest, BuildFence);
        Register("prop_lantern_post", FitMode.UniformHeight, BuildLanternPost);
        Register("prop_signpost", FitMode.UniformHeight, BuildSignpost);
        Register("prop_crate", FitMode.UniformLargest, BuildCrate);
        Register("prop_barrel", FitMode.UniformHeight, BuildBarrel);
        Register("prop_tent", FitMode.UniformLargest, BuildTent);
        Register("prop_well", FitMode.UniformHeight, BuildWell);
        Register("prop_banner", FitMode.UniformHeight, BuildBanner);
        Register("prop_woodpile", FitMode.UniformLargest, BuildWoodpile);
        Register("prop_bench", FitMode.UniformLargest, BuildBench);
    }

    // X 방향 길이 2m
    private static void BuildFence(LowPolyMeshBuilder b)
    {
        float[] posts = { -1f, 1f };

        for (int index = 0; index < posts.Length; index++)
        {
            b.AddBox(StylizedColor.WoodDark, new Vector3(posts[index], 0.5f, 0f), new Vector3(0.12f, 1f, 0.12f));
            b.Push(new Vector3(posts[index], 1f, 0f), Quaternion.identity, Vector3.one);
            b.AddFrustum(StylizedColor.WoodDark, Vector3.zero, 0.085f, 0f, 0.12f, 4, false, false, 45f);
            b.Pop();
        }

        b.AddBox(StylizedColor.WoodPlank, new Vector3(0f, 0.72f, 0.07f), new Vector3(2.1f, 0.1f, 0.04f));
        b.AddBox(StylizedColor.WoodLight, new Vector3(0f, 0.36f, 0.07f), new Vector3(2.1f, 0.1f, 0.04f));
        b.Push(new Vector3(0f, 0.54f, 0.1f), Euler(0f, 0f, 21f), Vector3.one);
        b.AddBox(StylizedColor.WoodPlank, Vector3.zero, new Vector3(2.05f, 0.08f, 0.03f));
        b.Pop();
    }

    private static void BuildLanternPost(LowPolyMeshBuilder b)
    {
        b.AddBeveledBox(StylizedColor.Stone, new Vector3(0f, 0.12f, 0f), new Vector3(0.4f, 0.24f, 0.4f), 0.04f);
        b.AddBox(StylizedColor.WoodDark, new Vector3(0f, 1.25f, 0f), new Vector3(0.14f, 2.1f, 0.14f));
        b.AddBox(StylizedColor.WoodDark, new Vector3(0.28f, 2.2f, 0f), new Vector3(0.6f, 0.1f, 0.1f));
        b.AddLimb(StylizedColor.WoodDark, new Vector3(0.04f, 1.95f, 0f), new Vector3(0.3f, 2.17f, 0f), 0.03f, 0.03f, 4);
        b.AddLimb(StylizedColor.IronDark, new Vector3(0.5f, 2.15f, 0f), new Vector3(0.5f, 2.02f, 0f), 0.01f, 0.01f, 3);
        b.AddFrustum(StylizedColor.IronDark, new Vector3(0.5f, 1.98f, 0f), 0.02f, 0.14f, 0.06f, 4, false, true, 45f);
        b.AddBox(StylizedColor.LampGlow, new Vector3(0.5f, 1.86f, 0f), new Vector3(0.16f, 0.2f, 0.16f));
        b.AddBox(StylizedColor.IronDark, new Vector3(0.5f, 1.75f, 0f), new Vector3(0.2f, 0.03f, 0.2f));
    }

    private static void BuildSignpost(LowPolyMeshBuilder b)
    {
        b.AddBox(StylizedColor.WoodDark, new Vector3(0f, 0.9f, 0f), new Vector3(0.12f, 1.8f, 0.12f));
        b.Push(new Vector3(0.3f, 1.5f, 0.07f), Euler(0f, 0f, 3f), Vector3.one);
        b.AddBox(StylizedColor.WoodLight, Vector3.zero, new Vector3(0.7f, 0.2f, 0.04f));
        b.AddWedge(StylizedColor.WoodLight, new Vector3(0.4f, 0f, 0f), new Vector3(0.04f, 0.1f, 0.2f));
        b.Pop();
        b.Push(new Vector3(-0.28f, 1.2f, 0.07f), Euler(0f, 0f, -4f), Vector3.one);
        b.AddBox(StylizedColor.WoodPlank, Vector3.zero, new Vector3(0.62f, 0.18f, 0.04f));
        b.Pop();
        b.AddBox(StylizedColor.ClothRed, new Vector3(0.3f, 1.5f, 0.095f), new Vector3(0.4f, 0.04f, 0.01f));
        b.AddBox(StylizedColor.ClothBlue, new Vector3(-0.28f, 1.2f, 0.095f), new Vector3(0.36f, 0.04f, 0.01f));
    }

    private static void BuildCrate(LowPolyMeshBuilder b)
    {
        b.AddBox(StylizedColor.WoodPlank, new Vector3(0f, 0.35f, 0f), new Vector3(0.7f, 0.7f, 0.7f));
        Vector3[] faces = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };

        for (int index = 0; index < faces.Length; index++)
        {
            Quaternion rotation = Quaternion.LookRotation(faces[index]);
            b.Push(new Vector3(0f, 0.35f, 0f), rotation, Vector3.one);
            b.AddBox(StylizedColor.WoodDark, new Vector3(0f, 0.3f, 0.355f), new Vector3(0.72f, 0.08f, 0.02f));
            b.AddBox(StylizedColor.WoodDark, new Vector3(0f, -0.3f, 0.355f), new Vector3(0.72f, 0.08f, 0.02f));
            b.Push(new Vector3(0f, 0f, 0.355f), Euler(0f, 0f, 45f), Vector3.one);
            b.AddBox(StylizedColor.WoodDark, Vector3.zero, new Vector3(0.8f, 0.07f, 0.02f));
            b.Pop();
            b.Pop();
        }

        b.AddBox(StylizedColor.WoodLight, new Vector3(0f, 0.705f, 0f), new Vector3(0.66f, 0.01f, 0.66f));
    }

    private static void BuildBarrel(LowPolyMeshBuilder b)
    {
        b.AddFrustum(StylizedColor.WoodPlank, Vector3.zero, 0.3f, 0.36f, 0.45f, 10, true, false);
        b.AddFrustum(StylizedColor.WoodPlank, new Vector3(0f, 0.45f, 0f), 0.36f, 0.3f, 0.45f, 10, false, true);
        b.AddDisc(StylizedColor.WoodLight, new Vector3(0f, 0.905f, 0f), 0.28f, 10);
        b.AddTorus(StylizedColor.IronDark, new Vector3(0f, 0.12f, 0f), 0.32f, 0.02f, 10, 3);
        b.AddTorus(StylizedColor.IronDark, new Vector3(0f, 0.45f, 0f), 0.36f, 0.02f, 10, 3);
        b.AddTorus(StylizedColor.IronDark, new Vector3(0f, 0.78f, 0f), 0.32f, 0.02f, 10, 3);
    }

    private static void BuildTent(LowPolyMeshBuilder b)
    {
        // 입구는 +X 쪽 삼각형 면
        b.AddWedge(StylizedColor.ClothCream, new Vector3(0f, 0.8f, 0f), new Vector3(2.2f, 1.6f, 2f));
        b.Push(new Vector3(0f, 0.8f, 0f), Euler(0f, 90f, 0f), Vector3.one);
        b.AddBox(StylizedColor.ClothRed, new Vector3(0f, 0.8f, 0f), new Vector3(0.1f, 0.06f, 2.24f));
        b.Pop();
        b.AddQuad(StylizedColor.Black, new Vector3(1.101f, 0.02f, -0.45f), new Vector3(1.101f, 1.2f, 0f), new Vector3(1.101f, 0.02f, 0.45f), new Vector3(1.101f, 0.02f, 0f));
        b.AddLimb(StylizedColor.WoodDark, new Vector3(1.15f, 0f, 0f), new Vector3(1.15f, 1.75f, 0f), 0.03f, 0.03f, 4);
        b.AddLimb(StylizedColor.WoodDark, new Vector3(-1.15f, 0f, 0f), new Vector3(-1.15f, 1.75f, 0f), 0.03f, 0.03f, 4);
        b.AddLimb(StylizedColor.Rope, new Vector3(1.15f, 1.7f, 0f), new Vector3(1.9f, 0f, 0f), 0.01f, 0.01f, 3);
        b.AddLimb(StylizedColor.Rope, new Vector3(-1.15f, 1.7f, 0f), new Vector3(-1.9f, 0f, 0f), 0.01f, 0.01f, 3);
        b.AddBox(StylizedColor.WoodDark, new Vector3(1.9f, 0.05f, 0f), new Vector3(0.05f, 0.14f, 0.05f));
        b.AddBox(StylizedColor.WoodDark, new Vector3(-1.9f, 0.05f, 0f), new Vector3(0.05f, 0.14f, 0.05f));
    }

    private static void BuildWell(LowPolyMeshBuilder b)
    {
        for (int index = 0; index < 12; index++)
        {
            float angle = index * 30f;
            b.Push(Vector3.zero, Euler(0f, angle, 0f), Vector3.one);
            StylizedColor color = index % 2 == 0 ? StylizedColor.Stone : StylizedColor.StoneLight;
            b.AddBeveledBox(color, new Vector3(0f, 0.3f, 0.62f), new Vector3(0.34f, 0.6f, 0.24f), 0.03f);
            b.Pop();
        }

        b.AddDisc(StylizedColor.Water, new Vector3(0f, 0.4f, 0f), 0.55f, 12);
        b.AddBox(StylizedColor.WoodDark, new Vector3(0.7f, 1.1f, 0f), new Vector3(0.1f, 1.6f, 0.1f));
        b.AddBox(StylizedColor.WoodDark, new Vector3(-0.7f, 1.1f, 0f), new Vector3(0.1f, 1.6f, 0.1f));
        b.AddLimb(StylizedColor.Bark, new Vector3(-0.75f, 1.45f, 0f), new Vector3(0.75f, 1.45f, 0f), 0.05f, 0.05f, 6);
        b.AddLimb(StylizedColor.Rope, new Vector3(0f, 1.42f, 0f), new Vector3(0f, 0.95f, 0f), 0.012f, 0.012f, 3);
        b.AddFrustum(StylizedColor.WoodPlank, new Vector3(0f, 0.72f, 0f), 0.1f, 0.13f, 0.22f, 8, true, false);
        b.AddWedge(StylizedColor.ClothRed, new Vector3(0f, 2.05f, 0f), new Vector3(1.8f, 0.5f, 1.3f));
    }

    private static void BuildBanner(LowPolyMeshBuilder b)
    {
        b.AddLimb(StylizedColor.WoodDark, Vector3.zero, new Vector3(0f, 3f, 0f), 0.05f, 0.04f, 6);
        b.AddLowPolySphere(StylizedColor.Gold, new Vector3(0f, 3.05f, 0f), Vector3.one * 0.07f, 0, 0f, 1);
        b.AddLimb(StylizedColor.WoodDark, new Vector3(0f, 2.85f, 0f), new Vector3(0.8f, 2.85f, 0f), 0.025f, 0.025f, 4);
        b.AddQuad(StylizedColor.ClothRed, new Vector3(0.05f, 1.6f, 0.01f), new Vector3(0.05f, 2.82f, 0.01f), new Vector3(0.78f, 2.82f, 0.01f), new Vector3(0.78f, 1.75f, 0.01f));
        b.AddQuad(StylizedColor.ClothRed, new Vector3(0.78f, 1.75f, -0.01f), new Vector3(0.78f, 2.82f, -0.01f), new Vector3(0.05f, 2.82f, -0.01f), new Vector3(0.05f, 1.6f, -0.01f));
        b.AddQuad(StylizedColor.Gold, new Vector3(0.3f, 2.2f, 0.02f), new Vector3(0.42f, 2.45f, 0.02f), new Vector3(0.54f, 2.2f, 0.02f), new Vector3(0.42f, 2.05f, 0.02f));
    }

    private static void BuildWoodpile(LowPolyMeshBuilder b)
    {
        int count = 0;

        for (int row = 0; row < 3; row++)
        {
            int logs = 4 - row;

            for (int index = 0; index < logs; index++)
            {
                float z = (index - (logs - 1) * 0.5f) * 0.26f;
                float y = 0.13f + row * 0.22f;
                StylizedColor bark = count % 2 == 0 ? StylizedColor.Bark : StylizedColor.BarkDark;
                AddLog(b, new Vector3(z, y, 0f), 1.1f, 0.13f, 90f + (count % 3 - 1) * 3f, bark, StylizedColor.WoodLight);
                count++;
            }
        }
    }

    private static void BuildBench(LowPolyMeshBuilder b)
    {
        b.AddBeveledBox(StylizedColor.WoodLight, new Vector3(0f, 0.45f, 0f), new Vector3(1.6f, 0.08f, 0.4f), 0.02f);
        b.AddBox(StylizedColor.WoodDark, new Vector3(0.65f, 0.22f, 0f), new Vector3(0.1f, 0.44f, 0.36f));
        b.AddBox(StylizedColor.WoodDark, new Vector3(-0.65f, 0.22f, 0f), new Vector3(0.1f, 0.44f, 0.36f));
        b.AddBox(StylizedColor.WoodPlank, new Vector3(0f, 0.15f, 0f), new Vector3(1.3f, 0.05f, 0.05f));
    }
}
