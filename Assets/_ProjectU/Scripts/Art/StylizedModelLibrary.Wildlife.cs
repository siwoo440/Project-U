using UnityEngine;

// 118일차: 야생동물 모델 (13종)과 사냥 · 가공 물건 모델
// 동물은 미터 단위 · 앞 방향 +Z · 바닥 y = 0 이다. 들고 다니는 물건은 작게 만든다.
public static partial class StylizedModelLibrary
{
    private static void RegisterWildlife()
    {
        // 순한 동물
        Register("animal_rabbit", FitMode.UniformHeight, BuildRabbit);
        Register("animal_deer", FitMode.UniformHeight, BuildDeer);
        Register("animal_goat", FitMode.UniformHeight, BuildGoat);
        Register("animal_fox", FitMode.UniformHeight, BuildFox);
        Register("animal_lizard", FitMode.UniformHeight, BuildLizard);
        Register("animal_heron", FitMode.UniformHeight, BuildHeron);

        // 사나운 동물
        Register("animal_boar", FitMode.UniformHeight, BuildBoar);
        Register("animal_wolf", FitMode.UniformHeight, BuildWolf);
        Register("animal_bear", FitMode.UniformHeight, BuildBear);
        Register("animal_scorpion", FitMode.UniformHeight, BuildDesertScorpion);

        // 작은 것 (채집망)
        Register("creature_butterfly", FitMode.UniformHeight, BuildButterfly);
        Register("creature_firefly", FitMode.UniformHeight, BuildFirefly);
        Register("creature_crab", FitMode.UniformHeight, BuildCrab);

        // 사냥 전리품
        Register("food_small_meat", FitMode.UniformLargest, b => BuildMeatCut(b, StylizedColor.CowPink, 0.8f));
        Register("food_red_meat", FitMode.UniformLargest, b => BuildMeatCut(b, StylizedColor.AppleRed, 1f));
        Register("food_bear_meat", FitMode.UniformLargest, b => BuildMeatCut(b, StylizedColor.BerryPurple, 1.15f));
        Register("food_crab_meat", FitMode.UniformLargest, BuildCrabMeat);
        Register("resource_small_hide", FitMode.UniformLargest, b => BuildHide(b, StylizedColor.Straw, 0.8f));
        Register("resource_thick_hide", FitMode.UniformLargest, b => BuildHide(b, StylizedColor.Leather, 1f));
        Register("resource_fine_pelt", FitMode.UniformLargest, BuildFinePelt);
        Register("resource_wool", FitMode.UniformLargest, BuildWool);
        Register("resource_scale", FitMode.UniformLargest, BuildScalePile);
        Register("resource_feather", FitMode.UniformLargest, BuildFeather);
        Register("resource_antler", FitMode.UniformLargest, BuildAntlerItem);
        Register("resource_beast_fang", FitMode.UniformLargest, BuildFangItem);
        Register("resource_stinger", FitMode.UniformLargest, BuildStingerItem);
        Register("resource_crab_shell", FitMode.UniformLargest, BuildCrabShellItem);
        Register("resource_tanned_leather", FitMode.UniformLargest, BuildTannedLeather);
        Register("resource_butterfly", FitMode.UniformLargest, b => BuildInsectJar(b, StylizedColor.Flower, false));
        Register("resource_firefly", FitMode.UniformLargest, b => BuildInsectJar(b, StylizedColor.LampGlow, true));

        // 요리
        Register("food_roast_meat", FitMode.UniformLargest, BuildRoastMeat);
        Register("food_meat_stew", FitMode.UniformLargest, BuildMeatStew);
        Register("food_venison_steak", FitMode.UniformLargest, BuildVenisonSteak);
        Register("food_crab_soup", FitMode.UniformLargest, BuildCrabSoup);

        // 장비 · 도구
        Register("equipment_leather_backpack", FitMode.UniformLargest, BuildLeatherBackpack);
        Register("equipment_leather_hat", FitMode.UniformLargest, BuildLeatherHat);
        Register("equipment_winter_coat", FitMode.UniformLargest, BuildWinterCoat);
        Register("tool_net", FitMode.UniformLargest, BuildCatchNet);
        Register("weapon_fine_bow", FitMode.UniformLargest, BuildFineBow);

        // 건축물
        Register("build_animal_trap", FitMode.UniformFootprint, BuildAnimalTrapModel);
        Register("build_tanning_rack", FitMode.Stretch, BuildTanningRackModel);
    }

    // ---------------------------------------------------------------- 네 발 동물 공통

    private struct BeastShape // 네 발 동물 한 마리의 생김새
    {
        public StylizedColor Coat; // 몸 색
        public StylizedColor Belly; // 배 색
        public StylizedColor Detail; // 발굽 · 코 · 뿔 색
        public float Length; // 몸 길이
        public float Height; // 몸 두께
        public float Width; // 몸 너비
        public float Leg; // 다리 길이
        public float LegRadius; // 다리 굵기
        public float Neck; // 목 길이
        public float NeckPitch; // 목 기울기 (도, 0이면 앞으로)
        public float Head; // 머리 크기
        public float Ear; // 귀 크기
        public float Tail; // 꼬리 길이
        public bool Hoof; // 발굽
        public bool Antler; // 뿔 (사슴)
        public bool Horn; // 굽은 뿔 (산양)
        public bool Tusk; // 엄니 (멧돼지)
        public bool Mane; // 목덜미 털 (늑대 · 곰)
        public bool BushyTail; // 풍성한 꼬리
    }

    private static void BuildBeast(LowPolyMeshBuilder b, BeastShape s, int seed)
    {
        float bodyY = s.Leg + s.Height * 0.5f;
        b.AddLowPolySphere(s.Coat, new Vector3(0f, bodyY, 0f), new Vector3(s.Width * 0.5f, s.Height * 0.5f, s.Length * 0.5f), 2, 0.07f, seed);
        b.AddLowPolySphere(s.Belly, new Vector3(0f, bodyY - s.Height * 0.28f, 0f), new Vector3(s.Width * 0.42f, s.Height * 0.26f, s.Length * 0.42f), 1, 0.05f, seed + 1);

        for (int x = -1; x <= 1; x += 2) // 다리 네 개
        {
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 hip = new Vector3(x * s.Width * 0.32f, s.Leg * 0.95f, z * s.Length * 0.3f);
                Vector3 foot = new Vector3(hip.x, 0.02f, hip.z + z * s.Leg * 0.08f);
                b.AddLimb(s.Coat, hip, foot, s.LegRadius, s.LegRadius * 0.7f, 6);

                if (s.Hoof)
                {
                    b.AddCylinder(s.Detail, new Vector3(foot.x, 0f, foot.z), s.LegRadius * 0.85f, 0.04f, 6);
                }
                else
                {
                    b.AddLowPolySphere(s.Detail, new Vector3(foot.x, 0.025f, foot.z + 0.01f), new Vector3(s.LegRadius, s.LegRadius * 0.6f, s.LegRadius * 1.3f), 0, 0f, seed + 10 + x + z);
                }
            }
        }

        if (s.Mane) // 목덜미 털
        {
            b.AddLowPolySphere(s.Coat, new Vector3(0f, bodyY + s.Height * 0.3f, s.Length * 0.24f), new Vector3(s.Width * 0.46f, s.Height * 0.32f, s.Length * 0.2f), 1, 0.16f, seed + 2);
        }

        // 목과 머리
        Vector3 neckBase = new Vector3(0f, bodyY + s.Height * 0.22f, s.Length * 0.4f);
        b.Push(neckBase, Euler(s.NeckPitch, 0f, 0f), Vector3.one);
        b.AddLimb(s.Coat, Vector3.zero, new Vector3(0f, 0f, s.Neck), s.Width * 0.24f, s.Width * 0.2f, 7);
        Vector3 head = new Vector3(0f, 0f, s.Neck + s.Head * 0.35f);
        b.AddLowPolySphere(s.Coat, head, new Vector3(s.Head * 0.42f, s.Head * 0.44f, s.Head * 0.56f), 1, 0.05f, seed + 3);
        b.AddLowPolySphere(s.Coat, head + new Vector3(0f, -s.Head * 0.1f, s.Head * 0.48f), new Vector3(s.Head * 0.26f, s.Head * 0.24f, s.Head * 0.3f), 1, 0.04f, seed + 4);
        b.AddLowPolySphere(s.Detail, head + new Vector3(0f, -s.Head * 0.12f, s.Head * 0.72f), Vector3.one * s.Head * 0.11f, 0, 0f, seed + 5);
        b.AddLowPolySphere(StylizedColor.Black, head + new Vector3(s.Head * 0.3f, s.Head * 0.14f, s.Head * 0.3f), Vector3.one * s.Head * 0.075f, 0, 0f, seed + 6);
        b.AddLowPolySphere(StylizedColor.Black, head + new Vector3(-s.Head * 0.3f, s.Head * 0.14f, s.Head * 0.3f), Vector3.one * s.Head * 0.075f, 0, 0f, seed + 7);

        for (int side = -1; side <= 1; side += 2) // 귀
        {
            b.Push(head + new Vector3(side * s.Head * 0.28f, s.Head * 0.34f, -s.Head * 0.04f), Euler(-24f, side * 18f, 0f), Vector3.one);
            b.AddLowPolySphere(s.Coat, new Vector3(0f, s.Ear * 0.5f, 0f), new Vector3(s.Ear * 0.34f, s.Ear * 0.6f, s.Ear * 0.16f), 1, 0.05f, seed + 8 + side);
            b.Pop();

            if (s.Antler) // 사슴뿔 (두 갈래)
            {
                Vector3 root = head + new Vector3(side * s.Head * 0.2f, s.Head * 0.4f, 0f);
                Vector3 top = root + new Vector3(side * s.Head * 0.35f, s.Head * 0.95f, -s.Head * 0.1f);
                b.AddLimb(StylizedColor.Bone, root, top, s.Head * 0.07f, s.Head * 0.04f, 5);
                b.AddLimb(StylizedColor.Bone, root + new Vector3(side * s.Head * 0.14f, s.Head * 0.38f, 0f), root + new Vector3(side * s.Head * 0.55f, s.Head * 0.62f, s.Head * 0.22f), s.Head * 0.05f, s.Head * 0.03f, 5);
                b.AddLimb(StylizedColor.Bone, root + new Vector3(side * s.Head * 0.24f, s.Head * 0.66f, 0f), root + new Vector3(side * s.Head * 0.62f, s.Head * 0.92f, -s.Head * 0.3f), s.Head * 0.045f, s.Head * 0.025f, 5);
            }

            if (s.Horn) // 산양 뿔 (뒤로 굽음)
            {
                Vector3 root = head + new Vector3(side * s.Head * 0.22f, s.Head * 0.36f, 0f);
                Vector3 mid = root + new Vector3(side * s.Head * 0.12f, s.Head * 0.5f, -s.Head * 0.3f);
                Vector3 tip = mid + new Vector3(side * s.Head * 0.06f, -s.Head * 0.06f, -s.Head * 0.42f);
                b.AddLimb(StylizedColor.Bone, root, mid, s.Head * 0.09f, s.Head * 0.06f, 6);
                b.AddLimb(StylizedColor.Bone, mid, tip, s.Head * 0.06f, s.Head * 0.03f, 6);
            }

            if (s.Tusk) // 멧돼지 엄니
            {
                Vector3 root = head + new Vector3(side * s.Head * 0.18f, -s.Head * 0.16f, s.Head * 0.6f);
                b.AddLimb(StylizedColor.White, root, root + new Vector3(side * s.Head * 0.1f, s.Head * 0.3f, s.Head * 0.12f), s.Head * 0.05f, s.Head * 0.015f, 5);
            }
        }

        b.Pop();

        // 꼬리
        Vector3 tailRoot = new Vector3(0f, bodyY + s.Height * 0.18f, -s.Length * 0.46f);
        Vector3 tailTip = tailRoot + new Vector3(0f, s.Tail * 0.35f, -s.Tail);
        b.AddLimb(s.Coat, tailRoot, tailTip, s.Width * 0.12f, s.Width * 0.07f, 5);

        if (s.BushyTail)
        {
            b.AddLowPolySphere(s.Coat, tailTip, new Vector3(s.Width * 0.22f, s.Width * 0.22f, s.Tail * 0.42f), 1, 0.16f, seed + 20);
            b.AddLowPolySphere(s.Belly, tailTip + new Vector3(0f, 0f, -s.Tail * 0.28f), Vector3.one * s.Width * 0.16f, 1, 0.12f, seed + 21);
        }
    }

    // ---------------------------------------------------------------- 순한 동물

    private static void BuildRabbit(LowPolyMeshBuilder b)
    {
        BuildBeast(b, new BeastShape
        {
            Coat = StylizedColor.Straw, Belly = StylizedColor.White, Detail = StylizedColor.CowPink,
            Length = 0.34f, Height = 0.2f, Width = 0.17f, Leg = 0.09f, LegRadius = 0.03f,
            Neck = 0.04f, NeckPitch = -26f, Head = 0.16f, Ear = 0.16f, Tail = 0.05f, BushyTail = true
        }, 3101);
    }

    private static void BuildDeer(LowPolyMeshBuilder b)
    {
        BuildBeast(b, new BeastShape
        {
            Coat = StylizedColor.Leather, Belly = StylizedColor.ClothCream, Detail = StylizedColor.BarkDark,
            Length = 1.1f, Height = 0.5f, Width = 0.36f, Leg = 0.66f, LegRadius = 0.06f,
            Neck = 0.34f, NeckPitch = -42f, Head = 0.3f, Ear = 0.18f, Tail = 0.14f, Hoof = true, Antler = true
        }, 3121);
    }

    private static void BuildGoat(LowPolyMeshBuilder b)
    {
        BuildBeast(b, new BeastShape
        {
            Coat = StylizedColor.White, Belly = StylizedColor.ClothCream, Detail = StylizedColor.StoneDark,
            Length = 0.78f, Height = 0.4f, Width = 0.3f, Leg = 0.4f, LegRadius = 0.05f,
            Neck = 0.2f, NeckPitch = -30f, Head = 0.24f, Ear = 0.14f, Tail = 0.1f, Hoof = true, Horn = true
        }, 3141);

        b.AddLowPolySphere(StylizedColor.White, new Vector3(0f, 0.32f, 0.42f), new Vector3(0.1f, 0.12f, 0.08f), 1, 0.2f, 3149); // 턱수염
    }

    private static void BuildFox(LowPolyMeshBuilder b)
    {
        BuildBeast(b, new BeastShape
        {
            Coat = StylizedColor.AppleRed, Belly = StylizedColor.White, Detail = StylizedColor.Black,
            Length = 0.56f, Height = 0.24f, Width = 0.2f, Leg = 0.24f, LegRadius = 0.035f,
            Neck = 0.1f, NeckPitch = -14f, Head = 0.2f, Ear = 0.15f, Tail = 0.34f, BushyTail = true
        }, 3161);
    }

    private static void BuildLizard(LowPolyMeshBuilder b)
    {
        b.AddLowPolySphere(StylizedColor.SnakeScale, new Vector3(0f, 0.09f, 0f), new Vector3(0.08f, 0.06f, 0.2f), 1, 0.07f, 3181);
        b.AddLowPolySphere(StylizedColor.Sand, new Vector3(0f, 0.055f, 0f), new Vector3(0.065f, 0.03f, 0.17f), 1, 0.05f, 3182);
        b.AddLowPolySphere(StylizedColor.SnakeScale, new Vector3(0f, 0.1f, 0.24f), new Vector3(0.06f, 0.05f, 0.09f), 1, 0.05f, 3183);
        b.AddLowPolySphere(StylizedColor.Black, new Vector3(0.04f, 0.13f, 0.28f), Vector3.one * 0.014f, 0, 0f, 3184);
        b.AddLowPolySphere(StylizedColor.Black, new Vector3(-0.04f, 0.13f, 0.28f), Vector3.one * 0.014f, 0, 0f, 3185);
        b.AddLimb(StylizedColor.SnakeScale, new Vector3(0f, 0.09f, -0.18f), new Vector3(0f, 0.05f, -0.46f), 0.035f, 0.008f, 6); // 긴 꼬리

        for (int x = -1; x <= 1; x += 2) // 옆으로 벌린 네 발
        {
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 hip = new Vector3(x * 0.06f, 0.08f, z * 0.1f);
                b.AddLimb(StylizedColor.SnakeScale, hip, hip + new Vector3(x * 0.07f, -0.07f, z * 0.03f), 0.018f, 0.01f, 5);
            }
        }

        for (int index = 0; index < 4; index++) // 등 무늬
        {
            b.AddLowPolySphere(StylizedColor.Dirt, new Vector3(0f, 0.14f, -0.12f + index * 0.08f), new Vector3(0.035f, 0.012f, 0.025f), 0, 0f, 3190 + index);
        }
    }

    private static void BuildHeron(LowPolyMeshBuilder b)
    {
        b.AddLowPolySphere(StylizedColor.White, new Vector3(0f, 0.52f, 0f), new Vector3(0.12f, 0.14f, 0.26f), 1, 0.05f, 3201);
        b.AddLowPolySphere(StylizedColor.StoneLight, new Vector3(0f, 0.46f, -0.08f), new Vector3(0.1f, 0.08f, 0.22f), 1, 0.08f, 3202);

        for (int side = -1; side <= 1; side += 2) // 날개
        {
            b.AddLowPolySphere(StylizedColor.StoneLight, new Vector3(side * 0.11f, 0.54f, -0.02f), new Vector3(0.03f, 0.08f, 0.22f), 1, 0.06f, 3203 + side);
            b.AddLimb(StylizedColor.FlowerYellow, new Vector3(side * 0.045f, 0.4f, 0.02f), new Vector3(side * 0.05f, 0.02f, 0.03f), 0.014f, 0.01f, 5); // 긴 다리
            b.AddBox(StylizedColor.FlowerYellow, new Vector3(side * 0.05f, 0.012f, 0.05f), new Vector3(0.05f, 0.012f, 0.08f));
        }

        b.AddLimb(StylizedColor.White, new Vector3(0f, 0.6f, 0.14f), new Vector3(0f, 0.86f, 0.1f), 0.035f, 0.028f, 6); // 목
        b.AddLowPolySphere(StylizedColor.White, new Vector3(0f, 0.9f, 0.12f), new Vector3(0.05f, 0.055f, 0.07f), 1, 0.04f, 3206);
        b.Push(new Vector3(0f, 0.9f, 0.17f), Euler(96f, 0f, 0f), Vector3.one);
        b.AddCone(StylizedColor.FlowerYellow, Vector3.zero, 0.022f, 0.17f, 5); // 긴 부리
        b.Pop();
        b.AddLowPolySphere(StylizedColor.Black, new Vector3(0.035f, 0.93f, 0.14f), Vector3.one * 0.011f, 0, 0f, 3207);
        b.AddLowPolySphere(StylizedColor.Black, new Vector3(-0.035f, 0.93f, 0.14f), Vector3.one * 0.011f, 0, 0f, 3208);
        b.AddLowPolySphere(StylizedColor.IronDark, new Vector3(0f, 0.95f, 0.02f), new Vector3(0.02f, 0.03f, 0.1f), 1, 0.1f, 3209); // 뒤통수 깃
    }

    // ---------------------------------------------------------------- 사나운 동물

    private static void BuildBoar(LowPolyMeshBuilder b)
    {
        BuildBeast(b, new BeastShape
        {
            Coat = StylizedColor.BarkDark, Belly = StylizedColor.Bark, Detail = StylizedColor.Black,
            Length = 0.9f, Height = 0.46f, Width = 0.36f, Leg = 0.3f, LegRadius = 0.055f,
            Neck = 0.1f, NeckPitch = 10f, Head = 0.3f, Ear = 0.12f, Tail = 0.1f, Hoof = true, Tusk = true, Mane = true
        }, 3221);

        for (int index = 0; index < 5; index++) // 등 갈기
        {
            b.AddCone(StylizedColor.Black, new Vector3(0f, 0.52f + index * 0.002f, 0.22f - index * 0.1f), 0.022f, 0.12f, 4);
        }
    }

    private static void BuildWolf(LowPolyMeshBuilder b)
    {
        BuildBeast(b, new BeastShape
        {
            Coat = StylizedColor.StoneDark, Belly = StylizedColor.Stone, Detail = StylizedColor.Black,
            Length = 0.86f, Height = 0.36f, Width = 0.28f, Leg = 0.42f, LegRadius = 0.05f,
            Neck = 0.14f, NeckPitch = -8f, Head = 0.26f, Ear = 0.15f, Tail = 0.34f, Mane = true, BushyTail = true
        }, 3241);

        b.AddLowPolySphere(StylizedColor.White, new Vector3(0.05f, 0.58f, 0.74f), Vector3.one * 0.02f, 0, 0f, 3249); // 드러난 이빨
        b.AddLowPolySphere(StylizedColor.White, new Vector3(-0.05f, 0.58f, 0.74f), Vector3.one * 0.02f, 0, 0f, 3250);
    }

    private static void BuildBear(LowPolyMeshBuilder b)
    {
        BuildBeast(b, new BeastShape
        {
            Coat = StylizedColor.Bark, Belly = StylizedColor.BarkDark, Detail = StylizedColor.Black,
            Length = 1.3f, Height = 0.72f, Width = 0.6f, Leg = 0.46f, LegRadius = 0.1f,
            Neck = 0.12f, NeckPitch = -6f, Head = 0.4f, Ear = 0.16f, Tail = 0.08f, Mane = true
        }, 3261);

        for (int side = -1; side <= 1; side += 2) // 앞발톱
        {
            for (int claw = -1; claw <= 1; claw++)
            {
                b.Push(new Vector3(side * 0.19f + claw * 0.03f, 0.03f, 0.47f), Euler(70f, 0f, 0f), Vector3.one);
                b.AddCone(StylizedColor.Bone, Vector3.zero, 0.014f, 0.06f, 4);
                b.Pop();
            }
        }
    }

    private static void BuildDesertScorpion(LowPolyMeshBuilder b)
    {
        b.AddLowPolySphere(StylizedColor.ScorpionShell, new Vector3(0f, 0.1f, 0f), new Vector3(0.13f, 0.06f, 0.2f), 1, 0.07f, 3281);
        b.AddLowPolySphere(StylizedColor.Chitin, new Vector3(0f, 0.12f, 0.14f), new Vector3(0.09f, 0.05f, 0.09f), 1, 0.05f, 3282);

        for (int side = -1; side <= 1; side += 2) // 집게
        {
            b.AddLimb(StylizedColor.ScorpionShell, new Vector3(side * 0.08f, 0.1f, 0.18f), new Vector3(side * 0.2f, 0.07f, 0.32f), 0.025f, 0.02f, 5);
            b.AddLowPolySphere(StylizedColor.Chitin, new Vector3(side * 0.22f, 0.07f, 0.36f), new Vector3(0.05f, 0.03f, 0.07f), 1, 0.06f, 3283 + side);
            b.AddCone(StylizedColor.Chitin, new Vector3(side * 0.24f, 0.07f, 0.4f), 0.02f, 0.06f, 4);

            for (int leg = 0; leg < 4; leg++) // 다리 여덟 개
            {
                Vector3 hip = new Vector3(side * 0.1f, 0.09f, 0.1f - leg * 0.09f);
                b.AddLimb(StylizedColor.ScorpionShell, hip, hip + new Vector3(side * 0.12f, -0.08f, -0.02f), 0.013f, 0.007f, 4);
            }
        }

        Vector3 tail = new Vector3(0f, 0.13f, -0.18f); // 말린 꼬리

        for (int index = 0; index < 5; index++)
        {
            Vector3 next = tail + new Vector3(0f, 0.07f + index * 0.012f, index < 3 ? -0.03f : 0.05f);
            b.AddLimb(StylizedColor.ScorpionShell, tail, next, 0.028f - index * 0.003f, 0.025f - index * 0.003f, 5);
            tail = next;
        }

        b.AddCone(StylizedColor.Slime, tail + new Vector3(0f, 0.02f, 0.02f), 0.02f, 0.07f, 5); // 독침
    }

    // ---------------------------------------------------------------- 작은 것

    private static void BuildButterfly(LowPolyMeshBuilder b)
    {
        b.AddLowPolySphere(StylizedColor.Black, new Vector3(0f, 0.2f, 0f), new Vector3(0.012f, 0.014f, 0.05f), 1, 0f, 3301);

        for (int side = -1; side <= 1; side += 2)
        {
            b.Push(new Vector3(side * 0.02f, 0.2f, 0.01f), Euler(0f, 0f, side * -22f), Vector3.one);
            b.AddLowPolySphere(StylizedColor.Flower, new Vector3(side * 0.07f, 0f, 0.02f), new Vector3(0.075f, 0.005f, 0.055f), 1, 0.04f, 3302 + side);
            b.AddLowPolySphere(StylizedColor.FlowerYellow, new Vector3(side * 0.06f, 0.002f, -0.05f), new Vector3(0.055f, 0.004f, 0.04f), 1, 0.04f, 3304 + side);
            b.Pop();
            b.AddLimb(StylizedColor.Black, new Vector3(0f, 0.215f, 0.04f), new Vector3(side * 0.03f, 0.25f, 0.07f), 0.003f, 0.002f, 4); // 더듬이
        }
    }

    private static void BuildFirefly(LowPolyMeshBuilder b)
    {
        b.AddLowPolySphere(StylizedColor.BarkDark, new Vector3(0f, 0.16f, 0f), new Vector3(0.016f, 0.016f, 0.045f), 1, 0f, 3321);
        b.AddLowPolySphere(StylizedColor.LampGlow, new Vector3(0f, 0.16f, -0.045f), Vector3.one * 0.026f, 1, 0f, 3322); // 빛나는 꼬리

        for (int side = -1; side <= 1; side += 2)
        {
            b.AddLowPolySphere(StylizedColor.Glass, new Vector3(side * 0.03f, 0.175f, 0f), new Vector3(0.035f, 0.004f, 0.03f), 1, 0.05f, 3323 + side);
        }
    }

    private static void BuildCrab(LowPolyMeshBuilder b)
    {
        b.AddLowPolySphere(StylizedColor.Coral, new Vector3(0f, 0.09f, 0f), new Vector3(0.14f, 0.055f, 0.11f), 1, 0.05f, 3341);
        b.AddLowPolySphere(StylizedColor.AppleRed, new Vector3(0f, 0.11f, -0.02f), new Vector3(0.1f, 0.03f, 0.07f), 1, 0.08f, 3342);

        for (int side = -1; side <= 1; side += 2)
        {
            b.AddLimb(StylizedColor.Coral, new Vector3(side * 0.1f, 0.09f, 0.06f), new Vector3(side * 0.2f, 0.07f, 0.16f), 0.022f, 0.018f, 5);
            b.AddLowPolySphere(StylizedColor.AppleRed, new Vector3(side * 0.22f, 0.07f, 0.19f), new Vector3(0.04f, 0.03f, 0.055f), 1, 0.05f, 3343 + side);
            b.AddLimb(StylizedColor.Coral, new Vector3(side * 0.05f, 0.135f, 0.06f), new Vector3(side * 0.05f, 0.18f, 0.08f), 0.008f, 0.006f, 4); // 눈자루
            b.AddLowPolySphere(StylizedColor.Black, new Vector3(side * 0.05f, 0.19f, 0.085f), Vector3.one * 0.014f, 0, 0f, 3345 + side);

            for (int leg = 0; leg < 3; leg++)
            {
                Vector3 hip = new Vector3(side * 0.11f, 0.08f, 0.02f - leg * 0.06f);
                b.AddLimb(StylizedColor.Coral, hip, hip + new Vector3(side * 0.09f, -0.07f, -0.02f), 0.012f, 0.007f, 4);
            }
        }
    }

    // ---------------------------------------------------------------- 전리품 · 가공 물건

    private static void BuildMeatCut(LowPolyMeshBuilder b, StylizedColor meat, float size)
    {
        b.AddLowPolySphere(meat, new Vector3(0f, 0.12f * size, 0f), new Vector3(0.17f, 0.07f, 0.13f) * size, 1, 0.09f, 3401);
        b.AddLowPolySphere(StylizedColor.ClothCream, new Vector3(0f, 0.16f * size, 0.02f * size), new Vector3(0.11f, 0.02f, 0.08f) * size, 1, 0.12f, 3402);
        b.AddLimb(StylizedColor.Bone, new Vector3(-0.16f * size, 0.12f * size, 0f), new Vector3(0.18f * size, 0.13f * size, 0f), 0.02f * size, 0.02f * size, 5);
    }

    private static void BuildCrabMeat(LowPolyMeshBuilder b)
    {
        b.AddLowPolySphere(StylizedColor.Coral, new Vector3(0f, 0.11f, 0f), new Vector3(0.09f, 0.055f, 0.13f), 1, 0.07f, 3411);

        for (int index = 0; index < 4; index++)
        {
            b.AddLowPolySphere(StylizedColor.White, new Vector3(-0.05f + index * 0.035f, 0.16f, 0.02f), new Vector3(0.02f, 0.015f, 0.08f), 1, 0.1f, 3412 + index);
        }
    }

    private static void BuildHide(LowPolyMeshBuilder b, StylizedColor color, float size)
    {
        b.AddLowPolySphere(color, new Vector3(0f, 0.05f * size, 0f), new Vector3(0.2f, 0.022f, 0.16f) * size, 1, 0.1f, 3421);
        b.AddLowPolySphere(color, new Vector3(0f, 0.09f * size, -0.04f * size), new Vector3(0.14f, 0.02f, 0.1f) * size, 1, 0.12f, 3422);
        b.AddLowPolySphere(StylizedColor.ClothCream, new Vector3(0f, 0.035f * size, 0.02f * size), new Vector3(0.15f, 0.012f, 0.11f) * size, 1, 0.1f, 3423);
    }

    private static void BuildFinePelt(LowPolyMeshBuilder b)
    {
        BuildHide(b, StylizedColor.StoneDark, 1.05f);
        b.AddLowPolySphere(StylizedColor.White, new Vector3(0f, 0.12f, -0.08f), new Vector3(0.09f, 0.03f, 0.06f), 1, 0.18f, 3431);
        b.AddLowPolySphere(StylizedColor.Stone, new Vector3(0.08f, 0.11f, 0.06f), new Vector3(0.07f, 0.025f, 0.05f), 1, 0.2f, 3432);
    }

    private static void BuildWool(LowPolyMeshBuilder b)
    {
        for (int index = 0; index < 6; index++)
        {
            float angle = index / 6f * Mathf.PI * 2f;
            b.AddLowPolySphere(StylizedColor.White, new Vector3(Mathf.Sin(angle) * 0.07f, 0.1f + (index % 2) * 0.04f, Mathf.Cos(angle) * 0.06f), Vector3.one * 0.07f, 1, 0.2f, 3441 + index);
        }

        b.AddLowPolySphere(StylizedColor.ClothCream, new Vector3(0f, 0.13f, 0f), Vector3.one * 0.08f, 1, 0.22f, 3448);
    }

    private static void BuildScalePile(LowPolyMeshBuilder b)
    {
        for (int index = 0; index < 7; index++)
        {
            float angle = index * 51f * Mathf.Deg2Rad;
            b.Push(new Vector3(Mathf.Sin(angle) * 0.06f, 0.04f + index * 0.012f, Mathf.Cos(angle) * 0.05f), Euler(index * 9f, index * 47f, 0f), Vector3.one);
            b.AddLowPolySphere(StylizedColor.SnakeScale, Vector3.zero, new Vector3(0.06f, 0.012f, 0.05f), 1, 0.06f, 3451 + index);
            b.Pop();
        }
    }

    private static void BuildFeather(LowPolyMeshBuilder b)
    {
        b.AddLimb(StylizedColor.ClothCream, new Vector3(0f, 0.02f, -0.12f), new Vector3(0f, 0.06f, 0.16f), 0.008f, 0.004f, 5);
        b.AddLowPolySphere(StylizedColor.White, new Vector3(0f, 0.05f, 0.03f), new Vector3(0.045f, 0.008f, 0.12f), 1, 0.12f, 3461);
        b.AddLowPolySphere(StylizedColor.StoneLight, new Vector3(0f, 0.06f, 0.09f), new Vector3(0.03f, 0.006f, 0.06f), 1, 0.14f, 3462);
    }

    private static void BuildAntlerItem(LowPolyMeshBuilder b)
    {
        b.AddLimb(StylizedColor.Bone, new Vector3(-0.02f, 0.03f, -0.1f), new Vector3(0.04f, 0.22f, 0.04f), 0.024f, 0.014f, 6);
        b.AddLimb(StylizedColor.Bone, new Vector3(0f, 0.11f, -0.04f), new Vector3(0.14f, 0.18f, 0.06f), 0.016f, 0.009f, 5);
        b.AddLimb(StylizedColor.Bone, new Vector3(0.02f, 0.17f, -0.01f), new Vector3(0.12f, 0.26f, -0.06f), 0.014f, 0.008f, 5);
        b.AddLimb(StylizedColor.ClothCream, new Vector3(-0.02f, 0.03f, -0.1f), new Vector3(-0.08f, 0.02f, -0.16f), 0.02f, 0.012f, 5);
    }

    private static void BuildFangItem(LowPolyMeshBuilder b)
    {
        for (int index = 0; index < 3; index++)
        {
            b.Push(new Vector3(-0.05f + index * 0.05f, 0.02f, index * 0.01f), Euler(-12f - index * 6f, index * 24f, 0f), Vector3.one);
            b.AddCone(StylizedColor.White, Vector3.zero, 0.022f - index * 0.003f, 0.14f - index * 0.02f, 6);
            b.Pop();
        }

        b.AddLimb(StylizedColor.Rope, new Vector3(-0.07f, 0.03f, 0.01f), new Vector3(0.07f, 0.03f, 0.01f), 0.005f, 0.005f, 4);
    }

    private static void BuildStingerItem(LowPolyMeshBuilder b)
    {
        b.AddLowPolySphere(StylizedColor.ScorpionShell, new Vector3(0f, 0.06f, -0.02f), new Vector3(0.05f, 0.05f, 0.06f), 1, 0.06f, 3471);
        b.Push(new Vector3(0f, 0.09f, 0.02f), Euler(52f, 0f, 0f), Vector3.one);
        b.AddCone(StylizedColor.Chitin, Vector3.zero, 0.028f, 0.16f, 6);
        b.Pop();
        b.AddLowPolySphere(StylizedColor.Slime, new Vector3(0f, 0.2f, 0.11f), Vector3.one * 0.018f, 1, 0f, 3472);
    }

    private static void BuildCrabShellItem(LowPolyMeshBuilder b)
    {
        b.AddLowPolySphere(StylizedColor.Coral, new Vector3(0f, 0.06f, 0f), new Vector3(0.14f, 0.055f, 0.11f), 1, 0.06f, 3481);
        b.AddLowPolySphere(StylizedColor.AppleRed, new Vector3(0f, 0.085f, -0.02f), new Vector3(0.1f, 0.025f, 0.07f), 1, 0.09f, 3482);
        b.AddLowPolySphere(StylizedColor.ClothCream, new Vector3(0f, 0.035f, 0.01f), new Vector3(0.11f, 0.012f, 0.08f), 1, 0.08f, 3483);
    }

    private static void BuildTannedLeather(LowPolyMeshBuilder b)
    {
        b.Push(new Vector3(0f, 0.09f, 0f), Euler(0f, 0f, 90f), Vector3.one);
        b.AddCylinder(StylizedColor.Leather, new Vector3(0f, -0.13f, 0f), 0.085f, 0.26f, 10);
        b.Pop();
        b.AddLowPolySphere(StylizedColor.ClothCream, new Vector3(0.13f, 0.09f, 0f), new Vector3(0.012f, 0.08f, 0.08f), 1, 0.04f, 3491);
        b.AddBox(StylizedColor.Rope, new Vector3(0f, 0.09f, 0f), new Vector3(0.28f, 0.03f, 0.19f));
    }

    private static void BuildInsectJar(LowPolyMeshBuilder b, StylizedColor creature, bool glow)
    {
        b.AddCylinder(StylizedColor.Glass, new Vector3(0f, 0.02f, 0f), 0.07f, 0.17f, 10);
        b.AddCylinder(StylizedColor.WoodDark, new Vector3(0f, 0f, 0f), 0.075f, 0.025f, 10);
        b.AddCylinder(StylizedColor.Rope, new Vector3(0f, 0.19f, 0f), 0.075f, 0.03f, 10);
        b.AddLowPolySphere(creature, new Vector3(0f, 0.1f, 0f), Vector3.one * (glow ? 0.03f : 0.045f), 1, 0.05f, 3501);

        if (!glow) // 나비는 날개를 펴 둔다
        {
            b.AddLowPolySphere(creature, new Vector3(0.035f, 0.11f, 0f), new Vector3(0.035f, 0.005f, 0.03f), 1, 0.05f, 3502);
            b.AddLowPolySphere(creature, new Vector3(-0.035f, 0.11f, 0f), new Vector3(0.035f, 0.005f, 0.03f), 1, 0.05f, 3503);
            return;
        }

        b.AddLowPolySphere(StylizedColor.LampGlow, new Vector3(0f, 0.07f, 0f), Vector3.one * 0.022f, 1, 0f, 3504);
        b.AddLowPolySphere(StylizedColor.LampGlow, new Vector3(0.025f, 0.14f, 0.01f), Vector3.one * 0.016f, 1, 0f, 3505);
    }

    // ---------------------------------------------------------------- 요리

    private static void BuildRoastMeat(LowPolyMeshBuilder b)
    {
        b.AddLimb(StylizedColor.WoodLight, new Vector3(0f, 0.04f, -0.16f), new Vector3(0f, 0.04f, 0.2f), 0.012f, 0.008f, 5);

        for (int index = 0; index < 3; index++)
        {
            b.AddLowPolySphere(StylizedColor.Charred, new Vector3(0f, 0.05f, -0.09f + index * 0.09f), new Vector3(0.06f, 0.05f, 0.05f), 1, 0.12f, 3521 + index);
            b.AddLowPolySphere(StylizedColor.Caramel, new Vector3(0f, 0.08f, -0.09f + index * 0.09f), new Vector3(0.045f, 0.02f, 0.04f), 1, 0.14f, 3524 + index);
        }

        AddSteam(b, new Vector3(0.02f, 0.13f, 0.02f), 3530);
    }

    private static void BuildMeatStew(LowPolyMeshBuilder b)
    {
        AddBowl(b, StylizedColor.Caramel);
        float top = BowlSoupHeight;

        for (int index = 0; index < 4; index++)
        {
            float angle = index * 90f * Mathf.Deg2Rad;
            b.AddLowPolySphere(StylizedColor.AppleRed, new Vector3(Mathf.Sin(angle) * 0.08f, top + 0.012f, Mathf.Cos(angle) * 0.07f), new Vector3(0.045f, 0.02f, 0.04f), 1, 0.12f, 3541 + index);
        }

        b.AddLowPolySphere(StylizedColor.Potato, new Vector3(0.02f, top + 0.014f, 0.02f), new Vector3(0.04f, 0.022f, 0.035f), 1, 0.1f, 3545);
        b.AddLowPolySphere(StylizedColor.Herb, new Vector3(-0.05f, top + 0.02f, -0.02f), new Vector3(0.025f, 0.006f, 0.012f), 0, 0f, 3546);
        AddSteam(b, new Vector3(-0.04f, top + 0.06f, 0.03f), 3550);
    }

    private static void BuildVenisonSteak(LowPolyMeshBuilder b)
    {
        b.AddLowPolySphere(StylizedColor.Charred, new Vector3(0f, 0.09f, 0f), new Vector3(0.16f, 0.035f, 0.12f), 1, 0.06f, 3561);
        b.AddLowPolySphere(StylizedColor.AppleRed, new Vector3(0f, 0.115f, 0f), new Vector3(0.11f, 0.015f, 0.08f), 1, 0.08f, 3562);

        for (int index = 0; index < 3; index++) // 구운 자국
        {
            b.AddBox(StylizedColor.Black, new Vector3(0f, 0.125f, -0.05f + index * 0.05f), new Vector3(0.2f, 0.005f, 0.014f));
        }

        b.AddLowPolySphere(StylizedColor.Herb, new Vector3(0.06f, 0.13f, 0.05f), new Vector3(0.03f, 0.008f, 0.02f), 0, 0f, 3563);
        AddSteam(b, new Vector3(-0.03f, 0.17f, 0.02f), 3570);
    }

    private static void BuildCrabSoup(LowPolyMeshBuilder b)
    {
        AddBowl(b, StylizedColor.SoupPumpkin);
        float top = BowlSoupHeight;
        b.AddLimb(StylizedColor.Coral, new Vector3(-0.04f, top - 0.01f, 0.02f), new Vector3(0.04f, top + 0.09f, 0.06f), 0.02f, 0.016f, 5);
        b.AddLowPolySphere(StylizedColor.AppleRed, new Vector3(0.05f, top + 0.1f, 0.07f), new Vector3(0.035f, 0.025f, 0.05f), 1, 0.05f, 3581);
        b.AddLowPolySphere(StylizedColor.White, new Vector3(-0.05f, top + 0.014f, -0.04f), new Vector3(0.03f, 0.012f, 0.05f), 1, 0.1f, 3582);
        b.AddLowPolySphere(StylizedColor.Herb, new Vector3(0.03f, top + 0.018f, -0.06f), new Vector3(0.022f, 0.006f, 0.012f), 0, 0f, 3583);
        AddSteam(b, new Vector3(-0.02f, top + 0.07f, -0.02f), 3590);
    }

    // ---------------------------------------------------------------- 장비 · 도구

    private static void BuildLeatherBackpack(LowPolyMeshBuilder b)
    {
        b.AddBeveledBox(StylizedColor.Leather, new Vector3(0f, 0.3f, 0f), new Vector3(0.5f, 0.6f, 0.3f), 0.07f);
        b.AddBeveledBox(StylizedColor.BarkDark, new Vector3(0f, 0.54f, 0.02f), new Vector3(0.52f, 0.18f, 0.32f), 0.05f);
        b.AddLowPolySphere(StylizedColor.Straw, new Vector3(0f, 0.6f, 0.04f), new Vector3(0.22f, 0.05f, 0.14f), 1, 0.16f, 3601); // 털 장식
        b.AddBeveledBox(StylizedColor.Leather, new Vector3(0f, 0.22f, 0.18f), new Vector3(0.32f, 0.22f, 0.08f), 0.03f);
        b.AddBox(StylizedColor.Copper, new Vector3(0f, 0.47f, 0.19f), new Vector3(0.06f, 0.06f, 0.02f));
        b.AddBox(StylizedColor.BarkDark, new Vector3(0.16f, 0.32f, -0.16f), new Vector3(0.06f, 0.54f, 0.03f));
        b.AddBox(StylizedColor.BarkDark, new Vector3(-0.16f, 0.32f, -0.16f), new Vector3(0.06f, 0.54f, 0.03f));
        b.AddBox(StylizedColor.Rope, new Vector3(0f, 0.12f, 0f), new Vector3(0.52f, 0.04f, 0.32f));
    }

    private static void BuildLeatherHat(LowPolyMeshBuilder b)
    {
        b.AddFrustum(StylizedColor.Leather, new Vector3(0f, 0.06f, 0f), 0.17f, 0.13f, 0.16f, 12, false, true);
        b.AddCylinder(StylizedColor.Leather, new Vector3(0f, 0.04f, 0f), 0.28f, 0.03f, 14);
        b.AddCylinder(StylizedColor.BarkDark, new Vector3(0f, 0.07f, 0f), 0.175f, 0.035f, 12);
        b.AddLowPolySphere(StylizedColor.Straw, new Vector3(0.13f, 0.09f, 0.1f), new Vector3(0.04f, 0.02f, 0.03f), 1, 0.14f, 3611);
    }

    private static void BuildWinterCoat(LowPolyMeshBuilder b)
    {
        b.AddBeveledBox(StylizedColor.Leather, new Vector3(0f, 0.32f, 0f), new Vector3(0.46f, 0.6f, 0.22f), 0.06f);
        b.AddLowPolySphere(StylizedColor.White, new Vector3(0f, 0.62f, 0f), new Vector3(0.24f, 0.07f, 0.16f), 1, 0.18f, 3621); // 털 목깃
        b.AddLowPolySphere(StylizedColor.White, new Vector3(0f, 0.04f, 0f), new Vector3(0.24f, 0.04f, 0.13f), 1, 0.16f, 3622);
        b.AddBeveledBox(StylizedColor.Leather, new Vector3(0.29f, 0.42f, 0f), new Vector3(0.14f, 0.4f, 0.16f), 0.05f);
        b.AddBeveledBox(StylizedColor.Leather, new Vector3(-0.29f, 0.42f, 0f), new Vector3(0.14f, 0.4f, 0.16f), 0.05f);
        b.AddBox(StylizedColor.BarkDark, new Vector3(0f, 0.32f, 0.12f), new Vector3(0.05f, 0.58f, 0.03f));
        b.AddBox(StylizedColor.Copper, new Vector3(0f, 0.3f, 0.14f), new Vector3(0.03f, 0.03f, 0.02f));
    }

    private static void BuildCatchNet(LowPolyMeshBuilder b)
    {
        b.AddLimb(StylizedColor.WoodLight, new Vector3(0f, 0.02f, 0f), new Vector3(0f, 0.5f, 0f), 0.018f, 0.014f, 6); // 손잡이
        b.AddCylinder(StylizedColor.Rope, new Vector3(0f, 0.46f, 0f), 0.022f, 0.06f, 6);

        for (int index = 0; index < 10; index++) // 둥근 테
        {
            float angle = index / 10f * Mathf.PI * 2f;
            float next = (index + 1) / 10f * Mathf.PI * 2f;
            Vector3 from = new Vector3(Mathf.Sin(angle) * 0.13f, 0.56f, Mathf.Cos(angle) * 0.13f);
            Vector3 to = new Vector3(Mathf.Sin(next) * 0.13f, 0.56f, Mathf.Cos(next) * 0.13f);
            b.AddLimb(StylizedColor.Iron, from, to, 0.008f, 0.008f, 4);
        }

        b.AddLowPolySphere(StylizedColor.Glass, new Vector3(0f, 0.63f, 0f), new Vector3(0.12f, 0.1f, 0.12f), 1, 0.06f, 3631); // 그물
        b.AddLowPolySphere(StylizedColor.ClothCream, new Vector3(0f, 0.7f, 0f), new Vector3(0.05f, 0.04f, 0.05f), 1, 0.1f, 3632);
    }

    private static void BuildFineBow(LowPolyMeshBuilder b)
    {
        for (int index = 0; index < 8; index++) // 휜 활대
        {
            float t = index / 8f;
            float next = (index + 1) / 8f;
            Vector3 from = new Vector3(Mathf.Sin(t * Mathf.PI) * 0.12f, -0.3f + t * 0.6f, 0f);
            Vector3 to = new Vector3(Mathf.Sin(next * Mathf.PI) * 0.12f, -0.3f + next * 0.6f, 0f);
            b.AddLimb(index % 2 == 0 ? StylizedColor.WoodDark : StylizedColor.Leather, from, to, 0.017f, 0.015f, 5);
        }

        b.AddLimb(StylizedColor.Rope, new Vector3(0f, -0.3f, 0f), new Vector3(0f, 0.3f, 0f), 0.004f, 0.004f, 4); // 활줄
        b.AddCylinder(StylizedColor.Leather, new Vector3(0.115f, -0.06f, 0f), 0.026f, 0.12f, 8); // 가죽 손잡이
        b.AddLowPolySphere(StylizedColor.IronDark, new Vector3(0f, 0.3f, 0f), Vector3.one * 0.022f, 1, 0f, 3641); // 강철 끝
        b.AddLowPolySphere(StylizedColor.IronDark, new Vector3(0f, -0.3f, 0f), Vector3.one * 0.022f, 1, 0f, 3642);
        b.AddLowPolySphere(StylizedColor.Straw, new Vector3(0.1f, 0.14f, 0f), new Vector3(0.02f, 0.05f, 0.02f), 1, 0.16f, 3643); // 털 장식
    }

    // ---------------------------------------------------------------- 건축물

    private static void BuildAnimalTrapModel(LowPolyMeshBuilder b) // 덫 (지름 1m 정도)
    {
        b.AddCylinder(StylizedColor.WoodDark, new Vector3(0f, 0f, 0f), 0.34f, 0.05f, 12);
        b.AddCylinder(StylizedColor.Bark, new Vector3(0f, 0.05f, 0f), 0.28f, 0.03f, 12);

        for (int side = -1; side <= 1; side += 2) // 양쪽 나무 턱
        {
            b.Push(new Vector3(side * 0.22f, 0.06f, 0f), Euler(0f, 0f, side * 34f), Vector3.one);
            b.AddBeveledBox(StylizedColor.WoodPlank, new Vector3(0f, 0.12f, 0f), new Vector3(0.06f, 0.26f, 0.5f), 0.02f);
            b.Pop();

            for (int tooth = -2; tooth <= 2; tooth++) // 톱니
            {
                b.Push(new Vector3(side * 0.2f, 0.18f, tooth * 0.11f), Euler(0f, 0f, side * 52f), Vector3.one);
                b.AddCone(StylizedColor.WoodLight, Vector3.zero, 0.028f, 0.1f, 4);
                b.Pop();
            }
        }

        b.AddLimb(StylizedColor.Rope, new Vector3(-0.3f, 0.06f, 0.26f), new Vector3(0.3f, 0.06f, -0.26f), 0.012f, 0.012f, 4);
        b.AddLowPolySphere(StylizedColor.Straw, new Vector3(0f, 0.08f, 0f), new Vector3(0.16f, 0.03f, 0.16f), 1, 0.2f, 3651); // 미끼 자리 풀
    }

    private static void BuildTanningRackModel(LowPolyMeshBuilder b) // 무두질대 (1 상자 안)
    {
        for (int side = -1; side <= 1; side += 2) // 기둥 둘
        {
            b.AddCylinder(StylizedColor.WoodDark, new Vector3(side * 0.34f, 0f, 0.1f), 0.05f, 0.9f, 8);
            b.AddLimb(StylizedColor.Bark, new Vector3(side * 0.34f, 0.1f, 0.1f), new Vector3(side * 0.44f, 0.02f, -0.22f), 0.035f, 0.025f, 5);
        }

        b.AddCylinder(StylizedColor.WoodDark, new Vector3(-0.34f, 0.86f, 0.1f), 0.035f, 0.68f, 8); // 위 가로대는 회전해서 놓는다
        b.Push(new Vector3(-0.34f, 0.86f, 0.1f), Euler(0f, 0f, -90f), Vector3.one);
        b.AddCylinder(StylizedColor.WoodDark, Vector3.zero, 0.035f, 0.68f, 8);
        b.Pop();
        b.AddBeveledBox(StylizedColor.Leather, new Vector3(0f, 0.5f, 0.1f), new Vector3(0.56f, 0.6f, 0.04f), 0.03f); // 펼친 가죽
        b.AddLowPolySphere(StylizedColor.ClothCream, new Vector3(0f, 0.5f, 0.07f), new Vector3(0.22f, 0.24f, 0.01f), 1, 0.08f, 3661);

        for (int index = -2; index <= 2; index++) // 묶은 끈
        {
            b.AddLimb(StylizedColor.Rope, new Vector3(index * 0.14f, 0.8f, 0.1f), new Vector3(index * 0.14f, 0.86f, 0.1f), 0.008f, 0.008f, 4);
        }

        b.AddBox(StylizedColor.WoodPlank, new Vector3(0f, 0.05f, -0.24f), new Vector3(0.5f, 0.1f, 0.24f)); // 손질용 낮은 대
        b.AddLimb(StylizedColor.Iron, new Vector3(-0.12f, 0.12f, -0.22f), new Vector3(0.1f, 0.12f, -0.26f), 0.014f, 0.01f, 5); // 무두질 칼
    }
}
