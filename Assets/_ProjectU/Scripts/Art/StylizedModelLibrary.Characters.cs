using UnityEngine;

// 캐릭터는 발바닥이 y = 0, 얼굴이 +Z 방향이다.
public static partial class StylizedModelLibrary
{
    private static void RegisterCharacters()
    {
        Register("char_player", FitMode.UniformHeight, BuildPlayer);
        Register("enemy_grunt", FitMode.UniformHeight, BuildGrunt);
        Register("enemy_spitter", FitMode.UniformHeight, BuildSpitter);
        Register("enemy_slime", FitMode.UniformHeight, BuildSlime);
        Register("training_dummy", FitMode.UniformHeight, BuildTrainingDummy);
    }

    private static void BuildPlayer(LowPolyMeshBuilder b)
    {
        // 신발과 다리
        b.AddBeveledBox(StylizedColor.Leather, new Vector3(0.1f, 0.06f, 0.03f), new Vector3(0.14f, 0.12f, 0.24f), 0.03f);
        b.AddBeveledBox(StylizedColor.Leather, new Vector3(-0.1f, 0.06f, 0.03f), new Vector3(0.14f, 0.12f, 0.24f), 0.03f);
        b.AddLimb(StylizedColor.WoodDark, new Vector3(0.1f, 0.1f, 0f), new Vector3(0.1f, 0.82f, 0f), 0.07f, 0.085f, 6);
        b.AddLimb(StylizedColor.WoodDark, new Vector3(-0.1f, 0.1f, 0f), new Vector3(-0.1f, 0.82f, 0f), 0.07f, 0.085f, 6);

        // 몸통과 튜닉
        b.AddFrustum(StylizedColor.ClothBlue, new Vector3(0f, 0.7f, 0f), 0.27f, 0.23f, 0.18f, 8);
        b.AddBeveledBox(StylizedColor.ClothBlue, new Vector3(0f, 1.1f, 0f), new Vector3(0.46f, 0.6f, 0.28f), 0.08f);
        b.AddBox(StylizedColor.Leather, new Vector3(0f, 0.86f, 0f), new Vector3(0.47f, 0.07f, 0.29f));
        b.AddBox(StylizedColor.Gold, new Vector3(0f, 0.86f, 0.15f), new Vector3(0.07f, 0.06f, 0.02f));
        b.AddBox(StylizedColor.Leather, new Vector3(0.1f, 1.12f, 0.145f), new Vector3(0.05f, 0.56f, 0.01f));

        // 팔과 손
        b.AddLimb(StylizedColor.ClothBlue, new Vector3(0.28f, 1.34f, 0f), new Vector3(0.33f, 1.02f, 0.02f), 0.075f, 0.065f, 6);
        b.AddLimb(StylizedColor.Skin, new Vector3(0.33f, 1.02f, 0.02f), new Vector3(0.35f, 0.82f, 0.05f), 0.055f, 0.05f, 6);
        b.AddLimb(StylizedColor.ClothBlue, new Vector3(-0.28f, 1.34f, 0f), new Vector3(-0.33f, 1.02f, 0.02f), 0.075f, 0.065f, 6);
        b.AddLimb(StylizedColor.Skin, new Vector3(-0.33f, 1.02f, 0.02f), new Vector3(-0.35f, 0.82f, 0.05f), 0.055f, 0.05f, 6);
        b.AddLowPolySphere(StylizedColor.Skin, new Vector3(0.355f, 0.78f, 0.05f), Vector3.one * 0.06f, 0, 0f, 1);
        b.AddLowPolySphere(StylizedColor.Skin, new Vector3(-0.355f, 0.78f, 0.05f), Vector3.one * 0.06f, 0, 0f, 2);

        // 목도리와 머리
        b.AddCylinder(StylizedColor.Skin, new Vector3(0f, 1.38f, 0f), 0.07f, 0.1f, 6);
        b.AddTorus(StylizedColor.ClothRed, new Vector3(0f, 1.42f, 0f), 0.12f, 0.05f, 8, 4);
        b.AddBox(StylizedColor.ClothRed, new Vector3(0.1f, 1.3f, 0.14f), new Vector3(0.08f, 0.22f, 0.03f));
        b.AddBeveledBox(StylizedColor.Skin, new Vector3(0f, 1.62f, 0f), new Vector3(0.34f, 0.34f, 0.32f), 0.08f);
        b.AddBeveledBox(StylizedColor.Hair, new Vector3(0f, 1.78f, -0.02f), new Vector3(0.37f, 0.1f, 0.35f), 0.04f);
        b.AddBeveledBox(StylizedColor.Hair, new Vector3(0f, 1.66f, -0.14f), new Vector3(0.37f, 0.26f, 0.08f), 0.03f);
        b.AddBox(StylizedColor.Hair, new Vector3(0.1f, 1.74f, 0.16f), new Vector3(0.16f, 0.06f, 0.03f));
        b.AddBox(StylizedColor.Eye, new Vector3(0.075f, 1.63f, 0.162f), new Vector3(0.045f, 0.06f, 0.01f));
        b.AddBox(StylizedColor.Eye, new Vector3(-0.075f, 1.63f, 0.162f), new Vector3(0.045f, 0.06f, 0.01f));
        b.AddBox(StylizedColor.AppleRed, new Vector3(0f, 1.54f, 0.162f), new Vector3(0.06f, 0.015f, 0.01f));

        // 등에 멘 배낭과 침낭
        b.AddBeveledBox(StylizedColor.Leather, new Vector3(0f, 1.1f, -0.21f), new Vector3(0.36f, 0.46f, 0.16f), 0.05f);
        b.AddBeveledBox(StylizedColor.ClothGreen, new Vector3(0f, 1.25f, -0.23f), new Vector3(0.38f, 0.14f, 0.18f), 0.04f);
        b.Push(new Vector3(0f, 1.38f, -0.22f), Euler(0f, 0f, 90f), Vector3.one);
        b.AddCylinder(StylizedColor.ClothCream, new Vector3(0f, -0.22f, 0f), 0.07f, 0.44f, 7);
        b.Pop();
    }

    private static void BuildGrunt(LowPolyMeshBuilder b)
    {
        // 다리와 발
        b.AddLimb(StylizedColor.EnemySkinDark, new Vector3(0.2f, 0.08f, 0f), new Vector3(0.22f, 0.62f, 0f), 0.11f, 0.14f, 6);
        b.AddLimb(StylizedColor.EnemySkinDark, new Vector3(-0.2f, 0.08f, 0f), new Vector3(-0.22f, 0.62f, 0f), 0.11f, 0.14f, 6);
        b.AddBeveledBox(StylizedColor.EnemySkinDark, new Vector3(0.21f, 0.06f, 0.06f), new Vector3(0.2f, 0.12f, 0.3f), 0.04f);
        b.AddBeveledBox(StylizedColor.EnemySkinDark, new Vector3(-0.21f, 0.06f, 0.06f), new Vector3(0.2f, 0.12f, 0.3f), 0.04f);

        // 허리 천과 몸통
        b.AddFrustum(StylizedColor.Leather, new Vector3(0f, 0.5f, 0f), 0.36f, 0.4f, 0.28f, 8);
        b.AddBox(StylizedColor.Bone, new Vector3(0f, 0.72f, 0.3f), new Vector3(0.12f, 0.1f, 0.04f));
        b.AddBeveledBox(StylizedColor.EnemySkin, new Vector3(0f, 1.12f, 0f), new Vector3(0.8f, 0.72f, 0.52f), 0.16f);
        b.AddBeveledBox(StylizedColor.EnemyBelly, new Vector3(0f, 1.0f, 0.2f), new Vector3(0.5f, 0.44f, 0.16f), 0.08f);

        // 어깨 갑옷과 가시
        b.AddBeveledBox(StylizedColor.IronDark, new Vector3(0.42f, 1.45f, 0f), new Vector3(0.3f, 0.16f, 0.36f), 0.05f);
        b.Push(new Vector3(0.45f, 1.52f, -0.05f), Euler(0f, 0f, -20f), Vector3.one);
        b.AddCone(StylizedColor.Bone, Vector3.zero, 0.05f, 0.2f, 5);
        b.Pop();
        b.Push(new Vector3(0.45f, 1.52f, 0.1f), Euler(0f, 0f, -20f), Vector3.one);
        b.AddCone(StylizedColor.Bone, Vector3.zero, 0.04f, 0.15f, 5);
        b.Pop();
        b.AddBox(StylizedColor.Leather, new Vector3(0.05f, 1.18f, 0f), new Vector3(0.12f, 0.75f, 0.54f));

        // 팔
        b.AddLimb(StylizedColor.EnemySkin, new Vector3(0.44f, 1.36f, 0f), new Vector3(0.58f, 0.95f, 0.08f), 0.13f, 0.11f, 6);
        b.AddLimb(StylizedColor.EnemySkin, new Vector3(0.58f, 0.95f, 0.08f), new Vector3(0.58f, 0.7f, 0.2f), 0.1f, 0.09f, 6);
        b.AddLowPolySphere(StylizedColor.EnemySkinDark, new Vector3(0.58f, 0.66f, 0.22f), Vector3.one * 0.11f, 0, 0.1f, 1);
        b.AddLimb(StylizedColor.EnemySkin, new Vector3(-0.44f, 1.36f, 0f), new Vector3(-0.56f, 0.98f, 0.1f), 0.13f, 0.11f, 6);
        b.AddLimb(StylizedColor.EnemySkin, new Vector3(-0.56f, 0.98f, 0.1f), new Vector3(-0.58f, 0.78f, 0.3f), 0.1f, 0.09f, 6);
        b.AddLowPolySphere(StylizedColor.EnemySkinDark, new Vector3(-0.58f, 0.75f, 0.33f), Vector3.one * 0.11f, 0, 0.1f, 2);

        // 가시 몽둥이
        b.AddLimb(StylizedColor.WoodDark, new Vector3(-0.58f, 0.62f, 0.2f), new Vector3(-0.62f, 0.95f, 0.95f), 0.045f, 0.1f, 6);
        Vector3 clubEnd = new Vector3(-0.62f, 0.95f, 0.95f);
        Vector3[] spikeDirections = { Vector3.up, Vector3.left, Vector3.right, new Vector3(0f, 0.5f, 1f) };
        for (int index = 0; index < spikeDirections.Length; index++)
        {
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, spikeDirections[index].normalized);
            b.Push(clubEnd - new Vector3(0f, 0.08f, 0.15f), rotation, Vector3.one);
            b.AddCone(StylizedColor.Iron, Vector3.zero, 0.03f, 0.12f, 4);
            b.Pop();
        }

        // 머리
        b.AddBeveledBox(StylizedColor.EnemySkin, new Vector3(0f, 1.66f, 0.06f), new Vector3(0.44f, 0.36f, 0.42f), 0.09f);
        b.AddBox(StylizedColor.EnemySkinDark, new Vector3(0f, 1.76f, 0.26f), new Vector3(0.42f, 0.07f, 0.06f));
        b.AddBeveledBox(StylizedColor.EnemySkinDark, new Vector3(0f, 1.52f, 0.14f), new Vector3(0.4f, 0.12f, 0.34f), 0.04f);
        b.AddBox(StylizedColor.Fire, new Vector3(0.1f, 1.7f, 0.275f), new Vector3(0.07f, 0.04f, 0.01f));
        b.AddBox(StylizedColor.Fire, new Vector3(-0.1f, 1.7f, 0.275f), new Vector3(0.07f, 0.04f, 0.01f));
        b.AddCone(StylizedColor.Bone, new Vector3(0.12f, 1.54f, 0.3f), 0.035f, 0.14f, 4);
        b.AddCone(StylizedColor.Bone, new Vector3(-0.12f, 1.54f, 0.3f), 0.035f, 0.14f, 4);
        b.Push(new Vector3(0.26f, 1.7f, 0.02f), Euler(0f, 0f, -70f), Vector3.one);
        b.AddCone(StylizedColor.EnemySkin, Vector3.zero, 0.07f, 0.2f, 4);
        b.Pop();
        b.Push(new Vector3(-0.26f, 1.7f, 0.02f), Euler(0f, 0f, 70f), Vector3.one);
        b.AddCone(StylizedColor.EnemySkin, Vector3.zero, 0.07f, 0.2f, 4);
        b.Pop();
        b.AddFrustum(StylizedColor.Black, new Vector3(0f, 1.83f, -0.02f), 0.08f, 0.02f, 0.14f, 5);
    }

    private static void BuildSpitter(LowPolyMeshBuilder b)
    {
        // 몸통과 배
        b.AddLowPolySphere(StylizedColor.SpitterSkin, new Vector3(0f, 0.62f, -0.05f), new Vector3(0.56f, 0.46f, 0.62f), 1, 0.08f, 201);
        b.AddLowPolySphere(StylizedColor.EnemyBelly, new Vector3(0f, 0.5f, 0.2f), new Vector3(0.42f, 0.32f, 0.42f), 1, 0.06f, 202);

        // 등 위의 빛나는 독 주머니와 반점
        b.AddLowPolySphere(StylizedColor.SpitterSac, new Vector3(0f, 1.0f, -0.28f), new Vector3(0.34f, 0.3f, 0.34f), 1, 0.08f, 203);
        b.AddLowPolySphere(StylizedColor.Slime, new Vector3(0.18f, 1.18f, -0.2f), Vector3.one * 0.06f, 0, 0f, 204);
        b.AddLowPolySphere(StylizedColor.Slime, new Vector3(-0.14f, 1.22f, -0.34f), Vector3.one * 0.05f, 0, 0f, 205);
        for (int index = 0; index < 4; index++)
        {
            b.Push(new Vector3(0f, 0.95f - index * 0.05f, -0.62f + index * 0.02f), Euler(-35f - index * 10f, 0f, 0f), Vector3.one);
            b.AddCone(StylizedColor.Bone, Vector3.zero, 0.045f, 0.16f, 4);
            b.Pop();
        }

        // 다리
        b.AddLimb(StylizedColor.SpitterSkin, new Vector3(0.38f, 0.5f, -0.3f), new Vector3(0.6f, 0.35f, -0.45f), 0.13f, 0.09f, 6);
        b.AddLimb(StylizedColor.SpitterSkin, new Vector3(0.6f, 0.35f, -0.45f), new Vector3(0.55f, 0.02f, -0.2f), 0.09f, 0.06f, 6);
        b.AddLimb(StylizedColor.SpitterSkin, new Vector3(-0.38f, 0.5f, -0.3f), new Vector3(-0.6f, 0.35f, -0.45f), 0.13f, 0.09f, 6);
        b.AddLimb(StylizedColor.SpitterSkin, new Vector3(-0.6f, 0.35f, -0.45f), new Vector3(-0.55f, 0.02f, -0.2f), 0.09f, 0.06f, 6);
        b.AddLimb(StylizedColor.SpitterSkin, new Vector3(0.3f, 0.45f, 0.3f), new Vector3(0.38f, 0.02f, 0.42f), 0.08f, 0.06f, 6);
        b.AddLimb(StylizedColor.SpitterSkin, new Vector3(-0.3f, 0.45f, 0.3f), new Vector3(-0.38f, 0.02f, 0.42f), 0.08f, 0.06f, 6);
        b.AddBeveledBox(StylizedColor.SpitterSac, new Vector3(0.55f, 0.02f, -0.14f), new Vector3(0.18f, 0.04f, 0.2f), 0.01f);
        b.AddBeveledBox(StylizedColor.SpitterSac, new Vector3(-0.55f, 0.02f, -0.14f), new Vector3(0.18f, 0.04f, 0.2f), 0.01f);
        b.AddBeveledBox(StylizedColor.SpitterSac, new Vector3(0.39f, 0.02f, 0.48f), new Vector3(0.14f, 0.04f, 0.16f), 0.01f);
        b.AddBeveledBox(StylizedColor.SpitterSac, new Vector3(-0.39f, 0.02f, 0.48f), new Vector3(0.14f, 0.04f, 0.16f), 0.01f);

        // 머리와 뿜는 입
        b.AddLowPolySphere(StylizedColor.SpitterSkin, new Vector3(0f, 0.82f, 0.42f), new Vector3(0.36f, 0.26f, 0.26f), 1, 0.06f, 206);
        b.AddLimb(StylizedColor.SpitterSkin, new Vector3(0f, 0.76f, 0.6f), new Vector3(0f, 0.74f, 0.92f), 0.1f, 0.14f, 7);
        b.Push(new Vector3(0f, 0.74f, 0.92f), Euler(90f, 0f, 0f), Vector3.one);
        b.AddDisc(StylizedColor.Slime, new Vector3(0f, 0.001f, 0f), 0.11f, 7);
        b.Pop();
        b.AddTorus(StylizedColor.SpitterSac, new Vector3(0f, 0.74f, 0.9f), 0.13f, 0.025f, 7, 3);

        // 눈
        b.AddLowPolySphere(StylizedColor.White, new Vector3(0.2f, 1.05f, 0.42f), Vector3.one * 0.11f, 1, 0f, 207);
        b.AddLowPolySphere(StylizedColor.White, new Vector3(-0.2f, 1.05f, 0.42f), Vector3.one * 0.11f, 1, 0f, 208);
        b.AddLowPolySphere(StylizedColor.Black, new Vector3(0.21f, 1.06f, 0.52f), Vector3.one * 0.045f, 0, 0f, 209);
        b.AddLowPolySphere(StylizedColor.Black, new Vector3(-0.21f, 1.06f, 0.52f), Vector3.one * 0.045f, 0, 0f, 210);
    }

    private static void BuildSlime(LowPolyMeshBuilder b)
    {
        b.AddLowPolySphere(StylizedColor.Slime, new Vector3(0f, 0.45f, 0f), new Vector3(0.62f, 0.48f, 0.62f), 1, 0.06f, 301);
        b.AddFrustum(StylizedColor.Slime, Vector3.zero, 0.66f, 0.6f, 0.2f, 10, true, false);
        b.AddLowPolySphere(StylizedColor.SpitterSac, new Vector3(-0.1f, 0.55f, -0.05f), Vector3.one * 0.18f, 0, 0.1f, 302);
        b.AddLowPolySphere(StylizedColor.White, new Vector3(0.18f, 0.66f, 0.46f), Vector3.one * 0.11f, 1, 0f, 303);
        b.AddLowPolySphere(StylizedColor.White, new Vector3(-0.18f, 0.66f, 0.46f), Vector3.one * 0.11f, 1, 0f, 304);
        b.AddLowPolySphere(StylizedColor.Black, new Vector3(0.18f, 0.66f, 0.56f), Vector3.one * 0.05f, 0, 0f, 305);
        b.AddLowPolySphere(StylizedColor.Black, new Vector3(-0.18f, 0.66f, 0.56f), Vector3.one * 0.05f, 0, 0f, 306);
        b.AddBox(StylizedColor.EnemySkinDark, new Vector3(0f, 0.46f, 0.585f), new Vector3(0.18f, 0.04f, 0.02f));
    }

    private static void BuildTrainingDummy(LowPolyMeshBuilder b)
    {
        b.AddBox(StylizedColor.WoodDark, new Vector3(0f, 0.04f, 0f), new Vector3(0.8f, 0.08f, 0.14f));
        b.AddBox(StylizedColor.WoodDark, new Vector3(0f, 0.04f, 0f), new Vector3(0.14f, 0.08f, 0.8f));
        b.AddLimb(StylizedColor.Bark, new Vector3(0f, 0f, 0f), new Vector3(0f, 1.75f, 0f), 0.06f, 0.05f, 6);
        b.AddLimb(StylizedColor.Bark, new Vector3(-0.55f, 1.35f, 0f), new Vector3(0.55f, 1.35f, 0f), 0.045f, 0.045f, 6);
        b.AddLowPolySphere(StylizedColor.Rope, new Vector3(0f, 1.15f, 0f), new Vector3(0.3f, 0.42f, 0.24f), 1, 0.08f, 401);
        b.AddLowPolySphere(StylizedColor.Sand, new Vector3(0f, 1.72f, 0f), new Vector3(0.18f, 0.2f, 0.18f), 1, 0.05f, 402);
        b.AddTorus(StylizedColor.Leather, new Vector3(0f, 0.95f, 0f), 0.27f, 0.025f, 10, 3);
        b.AddTorus(StylizedColor.Leather, new Vector3(0f, 1.45f, 0f), 0.2f, 0.025f, 10, 3);
        // 가슴 과녁
        b.Push(new Vector3(0f, 1.18f, 0.23f), Euler(90f, 0f, 0f), Vector3.one);
        b.AddCylinder(StylizedColor.White, new Vector3(0f, -0.01f, 0f), 0.2f, 0.02f, 12);
        b.AddCylinder(StylizedColor.ClothRed, new Vector3(0f, -0.01f, 0f), 0.14f, 0.03f, 12);
        b.AddCylinder(StylizedColor.White, new Vector3(0f, -0.01f, 0f), 0.08f, 0.04f, 12);
        b.AddCylinder(StylizedColor.ClothRed, new Vector3(0f, -0.01f, 0f), 0.035f, 0.05f, 10);
        b.Pop();
        b.AddBox(StylizedColor.Black, new Vector3(0.06f, 1.75f, 0.175f), new Vector3(0.05f, 0.012f, 0.01f));
        b.AddBox(StylizedColor.Black, new Vector3(-0.06f, 1.75f, 0.175f), new Vector3(0.05f, 0.012f, 0.01f));
    }
}
