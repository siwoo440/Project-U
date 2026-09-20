using UnityEngine;

// 117일차: 광물 · 주괴 · 등급 도구 · 용광로
// 미터 단위, 바닥 y = 0. 광석은 돌덩이에 광물이 박힌 모양, 보석은 깎은 원석 모양.
public static partial class StylizedModelLibrary
{
    private static void RegisterMinerals()
    {
        // 광석 · 보석 (아이템)
        Register("resource_coal", FitMode.UniformHeight, b => BuildOreChunk(b, StylizedColor.Black, StylizedColor.StoneDark, 3));
        Register("resource_copper_ore", FitMode.UniformHeight, b => BuildOreChunk(b, StylizedColor.Copper, StylizedColor.Stone, 5));
        Register("resource_silver_ore", FitMode.UniformHeight, b => BuildOreChunk(b, StylizedColor.StoneLight, StylizedColor.StoneDark, 7));
        Register("resource_gold_ore", FitMode.UniformHeight, b => BuildOreChunk(b, StylizedColor.Gold, StylizedColor.Stone, 9));
        Register("resource_crystal", FitMode.UniformHeight, b => BuildGem(b, StylizedColor.Crystal, 11));
        Register("resource_meteorite_shard", FitMode.UniformHeight, BuildMeteoriteShard);
        Register("gem_ruby", FitMode.UniformHeight, b => BuildGem(b, StylizedColor.AppleRed, 13));
        Register("gem_sapphire", FitMode.UniformHeight, b => BuildGem(b, StylizedColor.ClothBlue, 15));
        Register("gem_emerald", FitMode.UniformHeight, b => BuildGem(b, StylizedColor.LeafDark, 17));
        Register("gem_amethyst", FitMode.UniformHeight, b => BuildGem(b, StylizedColor.BerryPurple, 19));
        Register("gem_diamond", FitMode.UniformHeight, b => BuildGem(b, StylizedColor.White, 21));

        // 주괴
        Register("ingot_copper", FitMode.UniformHeight, b => BuildIngot(b, StylizedColor.Copper));
        Register("ingot_iron", FitMode.UniformHeight, b => BuildIngot(b, StylizedColor.Iron));
        Register("ingot_silver", FitMode.UniformHeight, b => BuildIngot(b, StylizedColor.StoneLight));
        Register("ingot_gold", FitMode.UniformHeight, b => BuildIngot(b, StylizedColor.Gold));
        Register("ingot_steel", FitMode.UniformHeight, b => BuildIngot(b, StylizedColor.IronDark));
        Register("ingot_meteorite", FitMode.UniformHeight, b => BuildIngot(b, StylizedColor.SoulFlame));

        // 등급 도구 (머리 색만 다름)
        Register("tool_axe_copper", FitMode.UniformHeight, b => BuildTieredAxe(b, StylizedColor.Copper));
        Register("tool_axe_steel", FitMode.UniformHeight, b => BuildTieredAxe(b, StylizedColor.IronDark));
        Register("tool_pickaxe_copper", FitMode.UniformHeight, b => BuildTieredPickaxe(b, StylizedColor.Copper));
        Register("tool_pickaxe_iron", FitMode.UniformHeight, b => BuildTieredPickaxe(b, StylizedColor.Iron));
        Register("tool_pickaxe_steel", FitMode.UniformHeight, b => BuildTieredPickaxe(b, StylizedColor.IronDark));

        // 동굴 광맥 (땅에 놓이는 큰 덩어리) · 용광로 · 운석
        Register("cave_vein_coal", FitMode.UniformHeight, b => BuildVein(b, StylizedColor.Black, 2));
        Register("cave_vein_copper", FitMode.UniformHeight, b => BuildVein(b, StylizedColor.Copper, 4));
        Register("cave_vein_iron", FitMode.UniformHeight, b => BuildVein(b, StylizedColor.Iron, 6));
        Register("cave_vein_silver", FitMode.UniformHeight, b => BuildVein(b, StylizedColor.StoneLight, 8));
        Register("cave_vein_gold", FitMode.UniformHeight, b => BuildVein(b, StylizedColor.Gold, 10));
        Register("cave_vein_crystal", FitMode.UniformHeight, b => BuildVein(b, StylizedColor.Crystal, 12));
        Register("cave_vein_gem", FitMode.UniformHeight, b => BuildVein(b, StylizedColor.BerryPurple, 14));
        Register("cave_vein_ruby", FitMode.UniformHeight, b => BuildVein(b, StylizedColor.AppleRed, 16));
        Register("cave_vein_sapphire", FitMode.UniformHeight, b => BuildVein(b, StylizedColor.ClothBlue, 18));
        Register("cave_vein_emerald", FitMode.UniformHeight, b => BuildVein(b, StylizedColor.LeafDark, 20));
        Register("cave_vein_diamond", FitMode.UniformHeight, b => BuildVein(b, StylizedColor.White, 22));
        Register("build_furnace", FitMode.UniformHeight, BuildFurnace);
        Register("meteor_rock", FitMode.UniformHeight, BuildMeteorRock);
        Register("meteor_crater", FitMode.UniformFootprint, BuildMeteorCrater); // 그을린 바닥과 흙 둔덕
    }

    private static void BuildOreChunk(LowPolyMeshBuilder builder, StylizedColor ore, StylizedColor rock, int seed) // 손에 드는 광석 조각
    {
        builder.AddLowPolySphere(rock, new Vector3(0f, 0.16f, 0f), new Vector3(0.24f, 0.19f, 0.22f), 1, 0.2f, seed);

        for (int index = 0; index < 3; index++)
        {
            float angle = index / 3f * Mathf.PI * 2f + seed;
            builder.AddLowPolySphere(ore, new Vector3(Mathf.Sin(angle) * 0.12f, 0.2f + (index % 2) * 0.05f, Mathf.Cos(angle) * 0.11f), Vector3.one * 0.08f, 1, 0.12f, seed + index);
        }
    }

    private static void BuildGem(LowPolyMeshBuilder builder, StylizedColor color, int seed) // 깎은 보석 (위아래 뿔)
    {
        builder.AddCone(color, new Vector3(0f, 0.17f, 0f), 0.11f, 0.17f, 6);
        builder.Push(new Vector3(0f, 0.17f, 0f), Quaternion.Euler(180f, seed * 7f, 0f), Vector3.one);
        builder.AddCone(color, Vector3.zero, 0.11f, 0.12f, 6);
        builder.Pop();
    }

    private static void BuildIngot(LowPolyMeshBuilder builder, StylizedColor color) // 주괴 (사다리꼴 막대)
    {
        builder.AddFrustum(color, new Vector3(0f, 0f, 0f), 0.14f, 0.1f, 0.12f, 4, true, true, 45f);
        builder.AddBeveledBox(color, new Vector3(0f, 0.06f, 0f), new Vector3(0.34f, 0.1f, 0.18f), 0.03f);
    }

    private static void BuildMeteoriteShard(LowPolyMeshBuilder builder) // 운석 조각 (그을린 돌 + 푸른 빛)
    {
        builder.AddLowPolySphere(StylizedColor.Black, new Vector3(0f, 0.15f, 0f), new Vector3(0.22f, 0.17f, 0.2f), 1, 0.28f, 31);
        builder.AddLowPolySphere(StylizedColor.SoulFlame, new Vector3(0.05f, 0.2f, 0.03f), Vector3.one * 0.08f, 1, 0.2f, 32);
        builder.AddCone(StylizedColor.SoulFlame, new Vector3(-0.08f, 0.16f, -0.05f), 0.05f, 0.12f, 5);
    }

    private static void BuildVein(LowPolyMeshBuilder builder, StylizedColor ore, int seed) // 동굴 벽 광맥 (캐는 자원)
    {
        builder.AddLowPolySphere(StylizedColor.StoneDark, new Vector3(0f, 0.55f, 0f), new Vector3(0.9f, 0.75f, 0.85f), 2, 0.16f, seed);
        builder.AddLowPolySphere(StylizedColor.Stone, new Vector3(0.5f, 0.4f, 0.28f), Vector3.one * 0.48f, 1, 0.16f, seed + 1);

        for (int index = 0; index < 5; index++)
        {
            float angle = index / 5f * Mathf.PI * 2f + seed * 0.3f;
            builder.AddLowPolySphere(ore, new Vector3(Mathf.Sin(angle) * 0.58f, 0.55f + (index % 3) * 0.2f, Mathf.Cos(angle) * 0.55f), Vector3.one * (0.2f + (index % 2) * 0.06f), 1, 0.12f, seed + 2 + index);
        }
    }

    private static void BuildTieredAxe(LowPolyMeshBuilder builder, StylizedColor head) // 등급 도끼
    {
        builder.AddLimb(StylizedColor.Bark, new Vector3(0f, 0f, 0f), new Vector3(0f, 0.62f, 0f), 0.035f, 0.04f, 6);
        builder.AddBeveledBox(head, new Vector3(0.09f, 0.6f, 0f), new Vector3(0.22f, 0.16f, 0.07f), 0.03f);
        builder.AddWedge(head, new Vector3(0.22f, 0.6f, 0f), new Vector3(0.12f, 0.16f, 0.06f));
    }

    private static void BuildTieredPickaxe(LowPolyMeshBuilder builder, StylizedColor head) // 등급 곡괭이
    {
        builder.AddLimb(StylizedColor.Bark, new Vector3(0f, 0f, 0f), new Vector3(0f, 0.64f, 0f), 0.035f, 0.04f, 6);
        builder.AddLimb(head, new Vector3(-0.22f, 0.58f, 0f), new Vector3(0.22f, 0.58f, 0f), 0.03f, 0.03f, 5);
        builder.AddCone(head, new Vector3(-0.22f, 0.58f, 0f), 0.04f, 0.1f, 5);
        builder.AddCone(head, new Vector3(0.22f, 0.58f, 0f), 0.04f, 0.1f, 5);
    }

    private static void BuildFurnace(LowPolyMeshBuilder builder) // 용광로 (돌 화덕 + 굴뚝 + 불구멍)
    {
        builder.AddBeveledBox(StylizedColor.StoneDark, new Vector3(0f, 0.45f, 0f), new Vector3(1.5f, 0.9f, 1.3f), 0.1f);
        builder.AddBeveledBox(StylizedColor.Stone, new Vector3(0f, 1.15f, -0.1f), new Vector3(1.25f, 0.6f, 1.1f), 0.1f);
        builder.AddFrustum(StylizedColor.StoneDark, new Vector3(0.35f, 1.45f, -0.25f), 0.24f, 0.16f, 0.75f, 6);
        builder.AddQuad(StylizedColor.Black, // 불구멍
            new Vector3(-0.32f, 0.22f, 0.66f), new Vector3(0.32f, 0.22f, 0.66f),
            new Vector3(0.32f, 0.72f, 0.66f), new Vector3(-0.32f, 0.72f, 0.66f));
        builder.AddCone(StylizedColor.Fire, new Vector3(0f, 0.24f, 0.5f), 0.16f, 0.32f, 6);
        builder.AddCone(StylizedColor.FireCore, new Vector3(0f, 0.3f, 0.5f), 0.1f, 0.22f, 5);
        builder.AddBeveledBox(StylizedColor.Iron, new Vector3(0f, 0.95f, 0.6f), new Vector3(0.9f, 0.12f, 0.2f), 0.03f); // 쇠 선반
    }

    private static void BuildMeteorCrater(LowPolyMeshBuilder builder) // 운석 구덩이 (지름 9m 그을린 자리)
    {
        builder.AddDisc(StylizedColor.Black, new Vector3(0f, 0.04f, 0f), 3.1f, 16); // 가운데 그을음
        builder.AddDisc(StylizedColor.Dirt, new Vector3(0f, 0.02f, 0f), 4.5f, 18); // 파헤쳐진 흙

        for (int index = 0; index < 12; index++) // 둘레 흙 둔덕
        {
            float angle = index / 12f * Mathf.PI * 2f;
            Vector3 spot = new Vector3(Mathf.Sin(angle) * 4.3f, 0.02f, Mathf.Cos(angle) * 4.3f);
            builder.AddLowPolySphere(StylizedColor.Dirt, spot, new Vector3(1.1f, 0.34f, 0.9f), 1, 0.22f, 61 + index);
        }

        for (int index = 0; index < 5; index++) // 튀어나온 돌조각
        {
            float angle = index / 5f * Mathf.PI * 2f + 0.5f;
            Vector3 spot = new Vector3(Mathf.Sin(angle) * 2.2f, 0.1f, Mathf.Cos(angle) * 2.2f);
            builder.AddLowPolySphere(StylizedColor.StoneDark, spot, Vector3.one * 0.32f, 1, 0.22f, 71 + index);
        }
    }

    private static void BuildMeteorRock(LowPolyMeshBuilder builder) // 운석 덩어리 (캐는 자원)
    {
        builder.AddLowPolySphere(StylizedColor.Black, new Vector3(0f, 0.5f, 0f), new Vector3(0.8f, 0.62f, 0.75f), 2, 0.24f, 41);

        for (int index = 0; index < 4; index++)
        {
            float angle = index / 4f * Mathf.PI * 2f + 0.7f;
            builder.AddLowPolySphere(StylizedColor.SoulFlame, new Vector3(Mathf.Sin(angle) * 0.42f, 0.55f + (index % 2) * 0.22f, Mathf.Cos(angle) * 0.4f), Vector3.one * 0.19f, 1, 0.14f, 42 + index);
        }

        builder.AddCone(StylizedColor.SoulFlame, new Vector3(0f, 0.95f, 0f), 0.14f, 0.3f, 5);
    }
}
