using UnityEngine;

// 90일차: 알파 마을 NPC 7명 · 마을 건물 (카페 · 대장간 · 오두막 · 짐마차 · 게시판)
// 100일차: 나머지 28명의 종족 · 하반신 · 날개는 StylizedModelLibrary.NpcBodies.cs 에 있다.
// NPC는 미터 단위, 발바닥 y = 0, 앞 +Z. 옷·머리 색은 NpcOutfit / NpcAccent / NpcHair 칸으로 만들고
// NpcAppearance가 NPC마다 캐릭터 시트 색으로 덮어쓴다.
// 건물도 미터 단위이며 문이 있는 앞면이 +Z 이다.
public static partial class StylizedModelLibrary
{
    public enum NpcSpecies
    {
        Rabbit,
        Cat,
        Sheep,
        Bull,
        Cow,
        Dragon,
        Rat,
        // 100일차: 나머지 28명 종족
        Succubus,
        Ghost,
        Harpy,
        Android,
        Fairy,
        Wolf,
        Arachne,
        Elf,
        Vampire,
        Slime,
        Dog,
        Bee,
        Fox,
        Mimic,
        Tiger,
        Witch,
        Lamia,
        Mermaid,
        Centaur,
        Dullahan,
        Oni,
        Goblin,
        Scorpion,
        Butterfly,
        Frog,
        Shark,
        Flower,
        Kraken
    }

    public enum NpcHairStyle
    {
        Long,
        Bob,
        Short,
        Curly,
        Wavy,
        Twin,
        Ponytail
    }

    public enum NpcOutfitStyle
    {
        Dress,
        Shorts,
        Robe,
        Apron,
        Smith,
        Cape,
        // 100일차
        Armor,
        Kimono,
        Suit,
        Wrap,
        Gothic
    }

    // 100일차: 하반신 모양 (사람 다리 외의 체형)
    public enum NpcBody
    {
        Legs,
        SnakeTail,
        SpiderLegs,
        HorseBody,
        FishTail,
        Tentacles,
        ScorpionBody,
        Floating,
        SlimeBase,
        MimicChest
    }

    // 100일차: 등 부속 (날개)
    public enum NpcWings
    {
        None,
        Bird,
        Bat,
        Fairy,
        Butterfly,
        Bee
    }

    public readonly struct NpcLook
    {
        public readonly NpcSpecies Species;
        public readonly NpcHairStyle Hair;
        public readonly NpcOutfitStyle Outfit;
        public readonly float Height; // 상반신 기준 키 (사람 체형이면 전체 키)
        public readonly NpcBody Body;
        public readonly NpcWings Wings;

        public NpcLook(NpcSpecies species, NpcHairStyle hair, NpcOutfitStyle outfit, float height, NpcBody body = NpcBody.Legs, NpcWings wings = NpcWings.None)
        {
            Species = species;
            Hair = hair;
            Outfit = outfit;
            Height = height;
            Body = body;
            Wings = wings;
        }
    }

    // 마을 건물 크기 (생성 도구가 충돌체 · NavMesh 장애물에 사용)
    public static readonly Vector3 NpcCafeSize = new Vector3(6f, 3.9f, 5f);
    public static readonly Vector3 NpcSmithySize = new Vector3(5f, 3.6f, 4f);
    public static readonly Vector3 NpcHouseSize = new Vector3(4f, 3.6f, 4f);
    public static readonly Vector3 NpcWagonSize = new Vector3(2.4f, 2.3f, 1.4f);
    private const float NpcBaseHeight = 1.8f; // 기본 체형의 머리 끝 높이

    public static string GetNpcModelId(string characterId)
    {
        return string.IsNullOrEmpty(characterId) ? string.Empty : "npc_" + characterId.Replace("char_", string.Empty);
    }

    private static void RegisterNpc()
    {
        RegisterNpcModel("npc_lunette", new NpcLook(NpcSpecies.Rabbit, NpcHairStyle.Long, NpcOutfitStyle.Dress, 1.58f));
        RegisterNpcModel("npc_verona", new NpcLook(NpcSpecies.Bull, NpcHairStyle.Wavy, NpcOutfitStyle.Robe, 1.76f));
        RegisterNpcModel("npc_mio", new NpcLook(NpcSpecies.Cat, NpcHairStyle.Short, NpcOutfitStyle.Shorts, 1.6f));
        RegisterNpcModel("npc_mireille", new NpcLook(NpcSpecies.Sheep, NpcHairStyle.Curly, NpcOutfitStyle.Dress, 1.55f));
        RegisterNpcModel("npc_milky", new NpcLook(NpcSpecies.Cow, NpcHairStyle.Long, NpcOutfitStyle.Apron, 1.66f));
        RegisterNpcModel("npc_dravia", new NpcLook(NpcSpecies.Dragon, NpcHairStyle.Long, NpcOutfitStyle.Smith, 1.78f));
        RegisterNpcModel("npc_lichel", new NpcLook(NpcSpecies.Rat, NpcHairStyle.Bob, NpcOutfitStyle.Cape, 1.52f));
        RegisterExpansionNpcs(); // 100일차: 나머지 28명 (StylizedModelLibrary.NpcBodies.cs)

        Register("build_npc_cafe", FitMode.UniformHeight, BuildNpcCafe);
        Register("build_npc_smithy", FitMode.UniformHeight, BuildNpcSmithy);
        Register("build_npc_house", FitMode.UniformHeight, BuildNpcHouse);
        Register("prop_npc_wagon", FitMode.UniformHeight, BuildNpcWagon);
        Register("prop_village_board", FitMode.UniformHeight, BuildVillageBoard);
        Register("build_npc_alchemy", FitMode.UniformHeight, BuildNpcAlchemyLab); // 101일차: 벨라모르타의 연금술 공방
        Register("build_npc_scrap_yard", FitMode.UniformHeight, BuildNpcScrapYard); // 101일차: 피피의 폐품 작업장
    }

    private static void RegisterNpcModel(string id, NpcLook look)
    {
        npcLooks ??= new System.Collections.Generic.Dictionary<string, NpcLook>(System.StringComparer.Ordinal);
        npcLooks[id] = look;
        Register(id, FitMode.UniformHeight, b => BuildNpc(b, look));
    }

    // ---------------------------------------------------------------- NPC 공통 체형

    private static void BuildNpc(LowPolyMeshBuilder b, NpcLook look)
    {
        float scale = look.Height / NpcBaseHeight;
        NpcBodyShape shape = GetBodyShape(look.Body);
        StylizedColor skin = GetNpcSkin(look.Species);
        bool upperOnly = look.Body != NpcBody.Legs;
        b.Push(Vector3.zero, Quaternion.identity, Vector3.one * scale);
        b.Push(new Vector3(0f, shape.Hover, shape.Shift));

        if (upperOnly)
        {
            AddNpcLowerBody(b, look, skin, NpcHipHeight + shape.Lift);
        }
        else
        {
            AddNpcLegs(b, look.Outfit, skin);
        }

        b.Push(new Vector3(0f, shape.Lift, 0f));
        AddNpcOutfit(b, look.Outfit, upperOnly, skin);
        AddNpcArms(b, look.Outfit, skin, look.Species == NpcSpecies.Dullahan);

        if (look.Species == NpcSpecies.Dullahan)
        {
            // 듀라한 : 머리를 왼손에 들고, 목에는 푸른 영혼 불꽃
            b.Push(new Vector3(-0.2f, 1.2f, 0.34f), Euler(0f, 18f, 0f), Vector3.one * 0.92f);
            b.Push(new Vector3(0f, -1.58f, 0f));
            AddNpcHead(b, skin);
            AddNpcHair(b, look.Hair, GetNpcHairColor(look.Species));
            b.Pop();
            b.Pop();
        }
        else
        {
            AddNpcHead(b, skin);
            AddNpcHair(b, look.Hair, GetNpcHairColor(look.Species));
        }

        AddNpcSpecies(b, look);
        AddNpcWings(b, look.Wings);
        AddNpcProps(b, look);
        b.Pop();

        b.Pop();
        b.Pop();
    }

    private static void AddNpcLegs(LowPolyMeshBuilder b, NpcOutfitStyle outfit, StylizedColor skin)
    {
        StylizedColor leg = outfit == NpcOutfitStyle.Apron || outfit == NpcOutfitStyle.Gothic ? StylizedColor.Black
            : outfit == NpcOutfitStyle.Smith ? StylizedColor.WoodDark
            : outfit == NpcOutfitStyle.Armor ? StylizedColor.IronDark
            : outfit == NpcOutfitStyle.Suit ? StylizedColor.NpcOutfit
            : skin;
        StylizedColor shoe = outfit == NpcOutfitStyle.Apron || outfit == NpcOutfitStyle.Suit || outfit == NpcOutfitStyle.Gothic ? StylizedColor.Black : StylizedColor.Leather;

        for (int side = -1; side <= 1; side += 2)
        {
            b.AddBeveledBox(shoe, new Vector3(side * 0.09f, 0.05f, 0.03f), new Vector3(0.12f, 0.1f, 0.22f), 0.03f);
            b.AddLimb(leg, new Vector3(side * 0.09f, 0.1f, 0f), new Vector3(side * 0.085f, 0.82f, 0f), 0.05f, 0.07f, 6);
        }
    }

    // upperOnly : 사람 다리가 아닌 체형은 허리 아래 옷(치마 · 바지)을 빼고 상반신만 입힌다
    private static void AddNpcOutfit(LowPolyMeshBuilder b, NpcOutfitStyle outfit, bool upperOnly, StylizedColor skin)
    {
        switch (outfit)
        {
            case NpcOutfitStyle.Dress:
                if (!upperOnly)
                {
                    b.AddFrustum(StylizedColor.NpcOutfit, new Vector3(0f, 0.5f, 0f), 0.31f, 0.2f, 0.44f, 10);
                }

                b.AddBeveledBox(StylizedColor.NpcOutfit, new Vector3(0f, 1.1f, 0f), new Vector3(0.4f, 0.52f, 0.26f), 0.08f);
                b.AddBox(StylizedColor.NpcAccent, new Vector3(0f, 0.92f, 0f), new Vector3(0.41f, 0.07f, 0.27f));
                b.AddBox(StylizedColor.NpcAccent, new Vector3(0f, 1.3f, 0.132f), new Vector3(0.2f, 0.1f, 0.01f));
                break;
            case NpcOutfitStyle.Shorts:
            case NpcOutfitStyle.Cape:
                if (!upperOnly)
                {
                    b.AddBeveledBox(StylizedColor.NpcAccent, new Vector3(0f, 0.8f, 0f), new Vector3(0.36f, 0.22f, 0.24f), 0.05f);
                }

                b.AddBeveledBox(StylizedColor.NpcOutfit, new Vector3(0f, 1.12f, 0f), new Vector3(0.36f, 0.46f, 0.24f), 0.07f);
                b.AddBox(StylizedColor.Leather, new Vector3(0f, 0.9f, 0f), new Vector3(0.37f, 0.05f, 0.25f));
                break;
            case NpcOutfitStyle.Robe:
                if (!upperOnly)
                {
                    b.AddFrustum(StylizedColor.NpcOutfit, new Vector3(0f, 0.08f, 0f), 0.3f, 0.22f, 0.86f, 10);
                }

                b.AddBeveledBox(StylizedColor.NpcOutfit, new Vector3(0f, 1.12f, 0f), new Vector3(0.44f, 0.52f, 0.28f), 0.08f);
                b.AddBeveledBox(StylizedColor.NpcAccent, new Vector3(0f, 1.18f, 0.06f), new Vector3(0.4f, 0.32f, 0.2f), 0.06f);
                b.AddBox(StylizedColor.Gold, new Vector3(0f, 0.93f, 0f), new Vector3(0.46f, 0.06f, 0.3f));

                if (!upperOnly)
                {
                    b.AddBox(StylizedColor.NpcAccent, new Vector3(0f, 0.55f, 0.21f), new Vector3(0.16f, 0.72f, 0.02f));
                }

                break;
            case NpcOutfitStyle.Apron:
                if (!upperOnly)
                {
                    b.AddFrustum(StylizedColor.NpcOutfit, new Vector3(0f, 0.46f, 0f), 0.32f, 0.2f, 0.48f, 10);
                }

                b.AddBeveledBox(StylizedColor.NpcOutfit, new Vector3(0f, 1.1f, 0f), new Vector3(0.4f, 0.52f, 0.26f), 0.08f);

                if (!upperOnly)
                {
                    b.AddBox(StylizedColor.White, new Vector3(0f, 0.72f, 0.24f), new Vector3(0.3f, 0.42f, 0.02f));
                }

                b.AddBox(StylizedColor.White, new Vector3(0f, 1.1f, 0.135f), new Vector3(0.24f, 0.3f, 0.01f));
                b.AddBox(StylizedColor.White, new Vector3(0f, 0.93f, 0f), new Vector3(0.41f, 0.05f, 0.27f));
                b.AddLowPolySphere(StylizedColor.NpcAccent, new Vector3(0f, 1.33f, 0.14f), new Vector3(0.07f, 0.035f, 0.03f), 0, 0f, 9001);
                break;
            case NpcOutfitStyle.Smith:
                if (!upperOnly)
                {
                    b.AddBeveledBox(StylizedColor.WoodDark, new Vector3(0f, 0.82f, 0f), new Vector3(0.38f, 0.2f, 0.25f), 0.05f);
                }

                b.AddBeveledBox(StylizedColor.NpcOutfit, new Vector3(0f, 1.12f, 0f), new Vector3(0.44f, 0.5f, 0.27f), 0.08f);

                if (!upperOnly)
                {
                    b.AddBox(StylizedColor.Leather, new Vector3(0f, 0.86f, 0.14f), new Vector3(0.34f, 0.7f, 0.02f));
                }

                b.AddBox(StylizedColor.NpcAccent, new Vector3(0f, 0.93f, 0f), new Vector3(0.45f, 0.06f, 0.28f));
                break;
            default:
                AddNpcOutfitExtra(b, outfit, upperOnly, skin); // 100일차 옷 (StylizedModelLibrary.NpcBodies.cs)
                break;
        }

        if (outfit == NpcOutfitStyle.Cape)
        {
            // 망토 (등 뒤로 넓게) · 열쇠 꾸러미
            b.Push(new Vector3(0f, 1.34f, -0.13f), Euler(-8f, 0f, 0f), Vector3.one);
            b.AddFrustum(StylizedColor.NpcOutfit, new Vector3(0f, -0.72f, -0.02f), 0.28f, 0.2f, 0.72f, 8, false, false, 22.5f);
            b.Pop();
            b.AddTorus(StylizedColor.NpcOutfit, new Vector3(0f, 1.35f, 0f), 0.13f, 0.045f, 8, 4);
            b.AddTorus(StylizedColor.Gold, new Vector3(0.19f, 0.84f, 0.06f), 0.035f, 0.008f, 8, 3);
            b.AddBox(StylizedColor.Gold, new Vector3(0.2f, 0.78f, 0.08f), new Vector3(0.015f, 0.06f, 0.01f));
        }
    }

    // holdHead : 듀라한은 왼팔을 앞으로 굽혀 머리를 든다
    private static void AddNpcArms(LowPolyMeshBuilder b, NpcOutfitStyle outfit, StylizedColor skin, bool holdHead)
    {
        bool bareArms = outfit == NpcOutfitStyle.Shorts || outfit == NpcOutfitStyle.Cape || outfit == NpcOutfitStyle.Wrap;

        for (int side = -1; side <= 1; side += 2)
        {
            StylizedColor sleeve = bareArms ? skin : StylizedColor.NpcOutfit;
            bool holding = holdHead && side < 0;
            Vector3 elbow = holding ? new Vector3(-0.3f, 1.08f, 0.12f) : new Vector3(side * 0.29f, 1.03f, 0.02f);
            Vector3 wrist = holding ? new Vector3(-0.24f, 1.0f, 0.3f) : new Vector3(side * 0.3f, 0.85f, 0.05f);
            Vector3 hand = holding ? new Vector3(-0.22f, 0.99f, 0.34f) : new Vector3(side * 0.305f, 0.81f, 0.05f);
            b.AddLimb(sleeve, new Vector3(side * 0.24f, 1.3f, 0f), elbow, 0.062f, 0.052f, 6);
            b.AddLimb(skin, elbow, wrist, 0.047f, 0.042f, 6);
            b.AddLowPolySphere(skin, hand, Vector3.one * 0.05f, 0, 0f, 9010 + side);

            if (outfit == NpcOutfitStyle.Smith)
            {
                b.AddCylinder(StylizedColor.Leather, new Vector3(side * 0.295f, 0.92f, 0.035f), 0.052f, 0.08f, 6);
            }
            else if (outfit == NpcOutfitStyle.Armor)
            {
                b.AddLimb(StylizedColor.IronDark, Vector3.Lerp(elbow, wrist, 0.35f), Vector3.Lerp(elbow, wrist, 0.95f), 0.056f, 0.05f, 6); // 건틀릿
            }
            else if (outfit == NpcOutfitStyle.Kimono || outfit == NpcOutfitStyle.Gothic)
            {
                b.AddLimb(StylizedColor.NpcOutfit, elbow + new Vector3(0f, 0.02f, 0f), Vector3.Lerp(elbow, wrist, 0.75f) + new Vector3(0f, -0.04f, -0.02f), 0.07f, 0.11f, 6); // 넓은 소매
            }
        }

        b.AddCylinder(skin, new Vector3(0f, 1.34f, 0f), 0.06f, 0.1f, 6);
    }

    private static void AddNpcHead(LowPolyMeshBuilder b, StylizedColor skin)
    {
        b.AddBeveledBox(skin, new Vector3(0f, 1.58f, 0f), new Vector3(0.34f, 0.34f, 0.32f), 0.09f);

        for (int side = -1; side <= 1; side += 2)
        {
            b.AddBox(StylizedColor.Eye, new Vector3(side * 0.075f, 1.58f, 0.162f), new Vector3(0.05f, 0.07f, 0.01f));
            b.AddBox(StylizedColor.White, new Vector3(side * 0.066f, 1.6f, 0.168f), new Vector3(0.016f, 0.02f, 0.005f));
            b.AddBox(StylizedColor.CowPink, new Vector3(side * 0.115f, 1.52f, 0.162f), new Vector3(0.05f, 0.02f, 0.005f));
        }

        b.AddBox(StylizedColor.AppleRed, new Vector3(0f, 1.485f, 0.162f), new Vector3(0.045f, 0.012f, 0.01f));
    }

    private static void AddNpcHair(LowPolyMeshBuilder b, NpcHairStyle hair, StylizedColor color)
    {
        b.AddBeveledBox(color, new Vector3(0f, 1.73f, -0.01f), new Vector3(0.37f, 0.12f, 0.35f), 0.05f);
        b.AddBeveledBox(color, new Vector3(0f, 1.62f, -0.145f), new Vector3(0.37f, 0.3f, 0.08f), 0.03f);
        b.AddBox(color, new Vector3(0f, 1.7f, 0.162f), new Vector3(0.34f, 0.07f, 0.03f));

        for (int side = -1; side <= 1; side += 2)
        {
            b.AddBox(color, new Vector3(side * 0.175f, 1.6f, 0.06f), new Vector3(0.035f, 0.22f, 0.12f));
        }

        switch (hair)
        {
            case NpcHairStyle.Long:
                b.AddBeveledBox(color, new Vector3(0f, 1.3f, -0.155f), new Vector3(0.34f, 0.62f, 0.08f), 0.03f);
                break;
            case NpcHairStyle.Bob:
                b.AddBeveledBox(color, new Vector3(0f, 1.5f, -0.12f), new Vector3(0.4f, 0.22f, 0.14f), 0.05f);

                for (int side = -1; side <= 1; side += 2)
                {
                    b.AddBeveledBox(color, new Vector3(side * 0.19f, 1.52f, 0.02f), new Vector3(0.05f, 0.2f, 0.22f), 0.02f);
                }

                break;
            case NpcHairStyle.Short:
                b.AddBox(color, new Vector3(0.09f, 1.76f, 0.15f), new Vector3(0.12f, 0.05f, 0.04f));
                break;
            case NpcHairStyle.Curly:
                for (int index = 0; index < 9; index++)
                {
                    float angle = index * Mathf.PI * 2f / 9f;
                    Vector3 offset = new Vector3(Mathf.Cos(angle) * 0.19f, 1.7f + Mathf.Sin(index * 1.7f) * 0.04f, Mathf.Sin(angle) * 0.18f - 0.02f);
                    b.AddLowPolySphere(color, offset, Vector3.one * 0.075f, 0, 0f, 9020 + index);
                }

                b.AddLowPolySphere(color, new Vector3(0f, 1.47f, -0.14f), new Vector3(0.17f, 0.12f, 0.08f), 1, 0.04f, 9030);
                break;
            case NpcHairStyle.Wavy:
                b.AddBeveledBox(color, new Vector3(0.02f, 1.35f, -0.16f), new Vector3(0.36f, 0.4f, 0.09f), 0.04f);
                b.AddBeveledBox(color, new Vector3(-0.02f, 1.02f, -0.17f), new Vector3(0.32f, 0.34f, 0.08f), 0.04f);
                b.AddLowPolySphere(StylizedColor.AppleBaked, new Vector3(0f, 0.86f, -0.17f), new Vector3(0.15f, 0.06f, 0.05f), 0, 0f, 9031);
                break;
            case NpcHairStyle.Twin:
                // 양 갈래 : 머리 옆 매듭 · 늘어진 머리 · 리본
                for (int side = -1; side <= 1; side += 2)
                {
                    b.AddLowPolySphere(color, new Vector3(side * 0.19f, 1.7f, -0.08f), Vector3.one * 0.07f, 0, 0f, 9032 + side);
                    b.AddLimb(color, new Vector3(side * 0.2f, 1.68f, -0.09f), new Vector3(side * 0.27f, 1.38f, -0.13f), 0.06f, 0.05f, 6);
                    b.AddLimb(color, new Vector3(side * 0.27f, 1.38f, -0.13f), new Vector3(side * 0.25f, 1.12f, -0.11f), 0.05f, 0.015f, 6);
                    b.AddTorus(StylizedColor.NpcAccent, new Vector3(side * 0.2f, 1.7f, -0.08f), 0.055f, 0.018f, 6, 3);
                }

                break;
            case NpcHairStyle.Ponytail:
                // 뒤로 높게 묶은 머리
                b.AddLowPolySphere(color, new Vector3(0f, 1.72f, -0.19f), Vector3.one * 0.07f, 0, 0f, 9035);
                b.AddLimb(color, new Vector3(0f, 1.72f, -0.2f), new Vector3(0f, 1.46f, -0.3f), 0.065f, 0.055f, 6);
                b.AddLimb(color, new Vector3(0f, 1.46f, -0.3f), new Vector3(0.02f, 1.16f, -0.26f), 0.055f, 0.015f, 6);
                b.AddTorus(StylizedColor.NpcAccent, new Vector3(0f, 1.72f, -0.2f), 0.06f, 0.02f, 6, 3);
                break;
        }
    }

    private static void AddNpcSpecies(LowPolyMeshBuilder b, NpcLook look)
    {
        NpcSpecies species = look.Species;

        switch (species)
        {
            case NpcSpecies.Rabbit:
                // 긴 롭 이어 (귀 끝은 연한 갈색) · 둥근 꼬리
                for (int side = -1; side <= 1; side += 2)
                {
                    b.AddLimb(StylizedColor.NpcHair, new Vector3(side * 0.16f, 1.76f, -0.02f), new Vector3(side * 0.25f, 1.4f, 0.01f), 0.05f, 0.07f, 6);
                    b.AddLowPolySphere(StylizedColor.Potato, new Vector3(side * 0.255f, 1.36f, 0.01f), new Vector3(0.065f, 0.075f, 0.05f), 0, 0f, 9040 + side);
                }

                b.AddLowPolySphere(StylizedColor.White, new Vector3(0f, 0.84f, -0.3f), Vector3.one * 0.07f, 1, 0.02f, 9043);
                break;
            case NpcSpecies.Cat:
                for (int side = -1; side <= 1; side += 2)
                {
                    b.Push(new Vector3(side * 0.12f, 1.77f, -0.01f), Euler(0f, 0f, -side * 16f), Vector3.one);
                    b.AddCone(StylizedColor.NpcHair, Vector3.zero, 0.075f, 0.15f, 4);
                    b.AddCone(StylizedColor.CowPink, new Vector3(0f, 0.01f, 0.03f), 0.04f, 0.1f, 4);
                    b.Pop();
                }

                b.AddLimb(StylizedColor.NpcHair, new Vector3(0f, 0.8f, -0.13f), new Vector3(0f, 0.62f, -0.34f), 0.035f, 0.033f, 5);
                b.AddLimb(StylizedColor.NpcHair, new Vector3(0f, 0.62f, -0.34f), new Vector3(0.02f, 0.86f, -0.5f), 0.033f, 0.03f, 5);
                b.AddLimb(StylizedColor.NpcHair, new Vector3(0.02f, 0.86f, -0.5f), new Vector3(0.08f, 1.06f, -0.48f), 0.03f, 0.022f, 5);
                b.AddLowPolySphere(StylizedColor.Gold, new Vector3(0f, 1.3f, 0.09f), Vector3.one * 0.032f, 0, 0f, 9044);
                break;
            case NpcSpecies.Sheep:
                // 말린 양뿔 · 양털 목도리 · 솜털 꼬리
                for (int side = -1; side <= 1; side += 2)
                {
                    b.Push(new Vector3(side * 0.2f, 1.64f, -0.03f), Euler(0f, 90f, 0f), Vector3.one);
                    b.AddTorus(StylizedColor.Bone, Vector3.zero, 0.065f, 0.028f, 8, 4);
                    b.Pop();
                }

                b.AddTorus(StylizedColor.White, new Vector3(0f, 1.36f, 0f), 0.1f, 0.05f, 8, 4);
                b.AddLowPolySphere(StylizedColor.White, new Vector3(0f, 0.86f, -0.3f), Vector3.one * 0.075f, 1, 0.03f, 9045);
                break;
            case NpcSpecies.Bull:
            case NpcSpecies.Cow:
                bool bull = species == NpcSpecies.Bull;

                for (int side = -1; side <= 1; side += 2)
                {
                    b.Push(new Vector3(side * (bull ? 0.18f : 0.13f), bull ? 1.74f : 1.78f, -0.02f), Euler(0f, 0f, -side * (bull ? 62f : 28f)), Vector3.one);
                    b.AddCone(StylizedColor.Bone, Vector3.zero, bull ? 0.055f : 0.032f, bull ? 0.2f : 0.08f, 6);
                    b.Pop();
                    b.AddLowPolySphere(bull ? StylizedColor.Skin : StylizedColor.White, new Vector3(side * 0.21f, 1.62f, -0.01f), new Vector3(0.08f, 0.035f, 0.05f), 0, 0f, 9046 + side);

                    if (!bull)
                    {
                        b.AddLowPolySphere(StylizedColor.Black, new Vector3(side * 0.225f, 1.625f, -0.035f), new Vector3(0.035f, 0.025f, 0.03f), 0, 0f, 9050 + side);
                    }
                }

                b.AddLimb(StylizedColor.NpcHair, new Vector3(0f, 0.86f, -0.16f), new Vector3(0f, 0.46f, -0.26f), 0.022f, 0.018f, 5);
                b.AddLowPolySphere(bull ? StylizedColor.NpcHair : StylizedColor.Black, new Vector3(0f, 0.42f, -0.265f), new Vector3(0.04f, 0.06f, 0.04f), 0, 0f, 9053);
                break;
            case NpcSpecies.Dragon:
                // 검은 뿔 · 접힌 날개 · 비늘 꼬리
                for (int side = -1; side <= 1; side += 2)
                {
                    b.Push(new Vector3(side * 0.1f, 1.77f, -0.05f), Euler(-38f, 0f, -side * 14f), Vector3.one);
                    b.AddCone(StylizedColor.Black, Vector3.zero, 0.045f, 0.24f, 5);
                    b.Pop();

                    b.Push(new Vector3(side * 0.13f, 1.22f, -0.16f), Euler(18f, side * 28f, side * 8f), Vector3.one);
                    b.AddBox(StylizedColor.NpcOutfit, new Vector3(0f, 0.08f, -0.16f), new Vector3(0.03f, 0.46f, 0.3f));
                    b.AddLimb(StylizedColor.Black, new Vector3(0f, 0.31f, -0.02f), new Vector3(0f, 0.2f, -0.34f), 0.025f, 0.015f, 4);
                    b.Pop();
                }

                b.AddLimb(StylizedColor.NpcOutfit, new Vector3(0f, 0.84f, -0.12f), new Vector3(0f, 0.5f, -0.42f), 0.09f, 0.07f, 6);
                b.AddLimb(StylizedColor.NpcOutfit, new Vector3(0f, 0.5f, -0.42f), new Vector3(0.05f, 0.2f, -0.76f), 0.07f, 0.04f, 6);
                b.AddLimb(StylizedColor.NpcOutfit, new Vector3(0.05f, 0.2f, -0.76f), new Vector3(0.14f, 0.1f, -1.02f), 0.04f, 0.012f, 5);
                break;
            case NpcSpecies.Rat:
                for (int side = -1; side <= 1; side += 2)
                {
                    b.AddLowPolySphere(StylizedColor.NpcHair, new Vector3(side * 0.18f, 1.8f, -0.02f), new Vector3(0.095f, 0.095f, 0.03f), 1, 0f, 9060 + side);
                    b.AddLowPolySphere(StylizedColor.CowPink, new Vector3(side * 0.18f, 1.8f, 0.005f), new Vector3(0.06f, 0.06f, 0.012f), 1, 0f, 9063 + side);
                }

                b.AddLimb(StylizedColor.CowPink, new Vector3(0f, 0.82f, -0.13f), new Vector3(0.05f, 0.5f, -0.42f), 0.02f, 0.017f, 5);
                b.AddLimb(StylizedColor.CowPink, new Vector3(0.05f, 0.5f, -0.42f), new Vector3(0.16f, 0.16f, -0.58f), 0.017f, 0.013f, 5);
                b.AddLimb(StylizedColor.CowPink, new Vector3(0.16f, 0.16f, -0.58f), new Vector3(0.34f, 0.05f, -0.6f), 0.013f, 0.008f, 5);
                break;
            default:
                AddNpcSpeciesExtra(b, look); // 100일차 종족 (StylizedModelLibrary.NpcBodies.cs)
                break;
        }
    }

    private static void AddNpcProps(LowPolyMeshBuilder b, NpcLook look)
    {
        switch (look.Species)
        {
            case NpcSpecies.Rabbit:
                // 약초 가죽 파우치 · 토끼발 펜던트
                b.AddBeveledBox(StylizedColor.Leather, new Vector3(0.24f, 0.86f, 0.06f), new Vector3(0.1f, 0.12f, 0.08f), 0.02f);
                b.AddLowPolySphere(StylizedColor.Herb, new Vector3(0.24f, 0.94f, 0.06f), new Vector3(0.05f, 0.03f, 0.03f), 0, 0f, 9070);
                b.AddLowPolySphere(StylizedColor.Gold, new Vector3(0f, 1.24f, 0.14f), Vector3.one * 0.022f, 0, 0f, 9071);
                break;
            case NpcSpecies.Cat:
                // 허리 도구 가방
                b.AddBeveledBox(StylizedColor.Leather, new Vector3(-0.2f, 0.84f, 0.08f), new Vector3(0.1f, 0.12f, 0.1f), 0.02f);
                break;
            case NpcSpecies.Sheep:
                // 양치기 지팡이 (오른손)
                b.AddLimb(StylizedColor.WoodDark, new Vector3(0.33f, 0.02f, 0.1f), new Vector3(0.33f, 1.5f, 0.1f), 0.02f, 0.02f, 5);
                b.Push(new Vector3(0.33f, 1.58f, 0.05f), Euler(0f, 90f, 0f), Vector3.one);
                b.AddTorus(StylizedColor.WoodDark, Vector3.zero, 0.07f, 0.02f, 8, 4);
                b.Pop();
                break;
            case NpcSpecies.Bull:
                // 대지 결정 목걸이
                b.AddLowPolySphere(StylizedColor.Crystal, new Vector3(0f, 1.36f, 0.17f), new Vector3(0.035f, 0.05f, 0.025f), 0, 0f, 9072);
                break;
            case NpcSpecies.Cow:
                // 소뿔 머리핀
                b.AddBox(StylizedColor.NpcAccent, new Vector3(-0.12f, 1.76f, 0.1f), new Vector3(0.06f, 0.025f, 0.02f));
                break;
            case NpcSpecies.Dragon:
                // 대장장이 망치 (오른손)
                b.AddLimb(StylizedColor.WoodDark, new Vector3(0.3f, 0.76f, 0.06f), new Vector3(0.3f, 0.46f, 0.16f), 0.018f, 0.018f, 5);
                b.AddBeveledBox(StylizedColor.IronDark, new Vector3(0.3f, 0.44f, 0.17f), new Vector3(0.14f, 0.07f, 0.07f), 0.015f);
                break;
            default:
                AddNpcPropsExtra(b, look); // 100일차 소지품 (StylizedModelLibrary.NpcBodies.cs)
                break;
        }
    }

    // ---------------------------------------------------------------- 마을 건물

    private static void BuildNpcCafe(LowPolyMeshBuilder b)
    {
        Vector3 size = NpcCafeSize;

        // 기초 · 벽 · 나무 골조
        b.AddBox(StylizedColor.StoneLight, new Vector3(0f, 0.08f, 0f), new Vector3(size.x + 0.2f, 0.16f, size.z + 0.2f));
        b.AddBox(StylizedColor.ClothCream, new Vector3(0f, 1.46f, 0f), new Vector3(size.x, 2.6f, size.z));

        foreach (float x in new[] { -2.98f, -1f, 1f, 2.98f })
        {
            b.AddBox(StylizedColor.WoodDark, new Vector3(x, 1.46f, 2.49f), new Vector3(0.16f, 2.6f, 0.06f));
        }

        b.AddBox(StylizedColor.WoodDark, new Vector3(0f, 2.72f, 2.49f), new Vector3(size.x, 0.14f, 0.06f));

        // 지붕 · 굴뚝
        b.AddWedge(StylizedColor.ClothRed, new Vector3(0f, 3.28f, 0f), new Vector3(size.x + 0.6f, 1.2f, size.z + 0.6f));
        b.AddBox(StylizedColor.StoneDark, new Vector3(1.8f, 3.5f, -1.1f), new Vector3(0.5f, 1.2f, 0.5f));

        // 문 · 창문
        b.AddBox(StylizedColor.WoodDark, new Vector3(0f, 1.16f, 2.51f), new Vector3(1.1f, 2f, 0.06f));
        b.AddLowPolySphere(StylizedColor.Gold, new Vector3(0.38f, 1.12f, 2.56f), Vector3.one * 0.05f, 0, 0f, 9101);

        for (int side = -1; side <= 1; side += 2)
        {
            b.AddBox(StylizedColor.WoodLight, new Vector3(side * 2f, 1.7f, 2.51f), new Vector3(1.2f, 1f, 0.06f));
            b.AddBox(StylizedColor.Glass, new Vector3(side * 2f, 1.7f, 2.53f), new Vector3(1f, 0.8f, 0.04f));
        }

        // 줄무늬 차양
        b.Push(new Vector3(0f, 2.62f, 2.9f), Euler(22f, 0f, 0f), Vector3.one);

        for (int index = 0; index < 8; index++)
        {
            StylizedColor stripe = index % 2 == 0 ? StylizedColor.ClothRed : StylizedColor.ClothCream;
            b.AddBox(stripe, new Vector3(-2.625f + index * 0.75f, 0f, 0f), new Vector3(0.75f, 0.05f, 1f));
        }

        b.Pop();

        // 바깥 카운터 · 간판 (컵)
        b.AddBox(StylizedColor.WoodPlank, new Vector3(1.9f, 0.5f, 3.6f), new Vector3(1.6f, 1f, 0.55f));
        b.AddBox(StylizedColor.WoodDark, new Vector3(1.9f, 1.02f, 3.6f), new Vector3(1.7f, 0.06f, 0.65f));
        b.AddCylinder(StylizedColor.White, new Vector3(1.5f, 1.05f, 3.6f), 0.06f, 0.1f, 8);
        b.AddCylinder(StylizedColor.White, new Vector3(2.2f, 1.05f, 3.55f), 0.06f, 0.1f, 8);
        b.AddLimb(StylizedColor.WoodDark, new Vector3(-2.6f, 0f, 3.4f), new Vector3(-2.6f, 2f, 3.4f), 0.05f, 0.05f, 6);
        b.AddBox(StylizedColor.WoodLight, new Vector3(-2.6f, 1.9f, 3.4f), new Vector3(0.8f, 0.5f, 0.06f));
        b.AddCylinder(StylizedColor.Leather, new Vector3(-2.6f, 1.75f, 3.44f), 0.1f, 0.22f, 8);
        b.AddTorus(StylizedColor.Leather, new Vector3(-2.47f, 1.87f, 3.44f), 0.05f, 0.015f, 6, 3);
    }

    private static void BuildNpcSmithy(LowPolyMeshBuilder b)
    {
        Vector3 size = NpcSmithySize;

        // 돌 바닥 · 뒤쪽 돌벽 · 기둥 · 지붕
        b.AddBox(StylizedColor.StoneDark, new Vector3(0f, 0.06f, 0f), new Vector3(size.x, 0.12f, size.z));
        b.AddBox(StylizedColor.Stone, new Vector3(0f, 1.2f, -1.85f), new Vector3(size.x, 2.4f, 0.3f));

        foreach (Vector2 post in new[] { new Vector2(-2.35f, 1.85f), new Vector2(2.35f, 1.85f), new Vector2(-2.35f, -1.6f), new Vector2(2.35f, -1.6f) })
        {
            b.AddBox(StylizedColor.WoodDark, new Vector3(post.x, 1.3f, post.y), new Vector3(0.2f, 2.6f, 0.2f));
        }

        b.AddWedge(StylizedColor.WoodDark, new Vector3(0f, 3.0f, 0f), new Vector3(size.x + 0.5f, 0.9f, size.z + 0.5f));

        // 화로 · 굴뚝 · 불
        b.AddBeveledBox(StylizedColor.StoneDark, new Vector3(-1.4f, 0.55f, -1.1f), new Vector3(1.4f, 1.1f, 1.2f), 0.08f);
        b.AddBox(StylizedColor.Charred, new Vector3(-1.4f, 1.12f, -1.1f), new Vector3(1f, 0.06f, 0.8f));
        b.AddLowPolySphere(StylizedColor.Fire, new Vector3(-1.4f, 1.2f, -1.1f), new Vector3(0.35f, 0.12f, 0.25f), 1, 0.03f, 9110);
        b.AddLowPolySphere(StylizedColor.FireCore, new Vector3(-1.4f, 1.22f, -1.1f), new Vector3(0.18f, 0.08f, 0.14f), 0, 0f, 9111);
        b.AddBox(StylizedColor.StoneDark, new Vector3(-1.4f, 2.8f, -1.4f), new Vector3(0.6f, 3.4f, 0.6f));

        // 모루 · 물통 · 무기 거치대
        b.AddBeveledBox(StylizedColor.WoodDark, new Vector3(0.7f, 0.25f, 0.2f), new Vector3(0.4f, 0.5f, 0.4f), 0.04f);
        b.AddBeveledBox(StylizedColor.IronDark, new Vector3(0.7f, 0.6f, 0.2f), new Vector3(0.7f, 0.2f, 0.3f), 0.04f);
        b.AddCone(StylizedColor.IronDark, new Vector3(1.05f, 0.62f, 0.2f), 0.09f, 0.22f, 5);
        b.AddCylinder(StylizedColor.WoodPlank, new Vector3(-0.2f, 0f, -1.2f), 0.3f, 0.55f, 8);
        b.AddDisc(StylizedColor.Water, new Vector3(-0.2f, 0.5f, -1.2f), 0.27f, 8);
        b.AddBox(StylizedColor.WoodDark, new Vector3(1.6f, 1.2f, -1.6f), new Vector3(1.3f, 0.08f, 0.12f));

        for (int index = 0; index < 3; index++)
        {
            b.AddBox(StylizedColor.Iron, new Vector3(1.2f + index * 0.4f, 1.0f, -1.55f), new Vector3(0.06f, 0.7f, 0.02f));
        }
    }

    private static void BuildNpcHouse(LowPolyMeshBuilder b)
    {
        Vector3 size = NpcHouseSize;

        b.AddBox(StylizedColor.Stone, new Vector3(0f, 0.1f, 0f), new Vector3(size.x + 0.2f, 0.2f, size.z + 0.2f));
        b.AddBox(StylizedColor.WoodPlank, new Vector3(0f, 1.3f, 0f), new Vector3(size.x, 2.2f, size.z));

        foreach (float x in new[] { -1.98f, 1.98f })
        {
            foreach (float z in new[] { -1.98f, 1.98f })
            {
                b.AddBox(StylizedColor.WoodDark, new Vector3(x, 1.3f, z), new Vector3(0.16f, 2.2f, 0.16f));
            }
        }

        b.AddWedge(StylizedColor.Straw, new Vector3(0f, 3.0f, 0f), new Vector3(size.x + 0.6f, 1.4f, size.z + 0.6f));
        b.AddBox(StylizedColor.StoneDark, new Vector3(-1.2f, 3.2f, -0.8f), new Vector3(0.4f, 1.2f, 0.4f));

        // 문 · 창문 · 발판 · 화분
        b.AddBox(StylizedColor.WoodDark, new Vector3(0.6f, 1.1f, 2.01f), new Vector3(0.9f, 1.8f, 0.06f));
        b.AddLowPolySphere(StylizedColor.Gold, new Vector3(0.9f, 1.05f, 2.06f), Vector3.one * 0.04f, 0, 0f, 9120);
        b.AddBox(StylizedColor.WoodLight, new Vector3(-0.9f, 1.5f, 2.01f), new Vector3(0.9f, 0.8f, 0.06f));
        b.AddBox(StylizedColor.Glass, new Vector3(-0.9f, 1.5f, 2.03f), new Vector3(0.74f, 0.64f, 0.04f));
        b.AddBox(StylizedColor.Stone, new Vector3(0.6f, 0.08f, 2.4f), new Vector3(1.1f, 0.16f, 0.6f));
        b.AddBox(StylizedColor.WoodDark, new Vector3(-0.9f, 1.05f, 2.2f), new Vector3(0.9f, 0.14f, 0.3f));
        b.AddLowPolySphere(StylizedColor.Flower, new Vector3(-1.1f, 1.18f, 2.2f), Vector3.one * 0.08f, 0, 0f, 9121);
        b.AddLowPolySphere(StylizedColor.FlowerYellow, new Vector3(-0.8f, 1.18f, 2.2f), Vector3.one * 0.08f, 0, 0f, 9122);
        b.AddLowPolySphere(StylizedColor.Leaf, new Vector3(-0.95f, 1.16f, 2.24f), new Vector3(0.12f, 0.06f, 0.06f), 0, 0f, 9123);
    }

    private static void BuildNpcWagon(LowPolyMeshBuilder b)
    {
        // 짐칸 · 바퀴 · 천 덮개 · 끌채 · 등불
        b.AddBeveledBox(StylizedColor.WoodPlank, new Vector3(0f, 0.85f, 0f), new Vector3(2.2f, 0.6f, 1.2f), 0.05f);

        for (int x = -1; x <= 1; x += 2)
        {
            for (int z = -1; z <= 1; z += 2)
            {
                b.Push(new Vector3(x * 0.7f, 0.42f, z * 0.66f), Euler(90f, 0f, 0f), Vector3.one);
                b.AddCylinder(StylizedColor.WoodDark, new Vector3(0f, -0.05f, 0f), 0.42f, 0.1f, 10);
                b.AddCylinder(StylizedColor.IronDark, new Vector3(0f, -0.07f, 0f), 0.08f, 0.14f, 6);
                b.Pop();
            }
        }

        b.AddBeveledBox(StylizedColor.ClothCream, new Vector3(0f, 1.62f, 0f), new Vector3(2.1f, 0.95f, 1.24f), 0.34f);

        foreach (float x in new[] { -0.7f, 0f, 0.7f })
        {
            b.AddBox(StylizedColor.WoodDark, new Vector3(x, 1.62f, 0f), new Vector3(0.06f, 1f, 1.28f));
        }

        b.AddLimb(StylizedColor.WoodDark, new Vector3(1.1f, 0.7f, 0.35f), new Vector3(2f, 0.35f, 0.35f), 0.04f, 0.035f, 5);
        b.AddLimb(StylizedColor.WoodDark, new Vector3(1.1f, 0.7f, -0.35f), new Vector3(2f, 0.35f, -0.35f), 0.04f, 0.035f, 5);
        b.AddLimb(StylizedColor.WoodDark, new Vector3(-1.05f, 1.1f, 0.55f), new Vector3(-1.05f, 2.1f, 0.55f), 0.025f, 0.025f, 5);
        b.AddLowPolySphere(StylizedColor.LampGlow, new Vector3(-1.05f, 2.02f, 0.62f), Vector3.one * 0.07f, 0, 0f, 9130);

        // 뒤쪽 상자 짐
        b.AddBeveledBox(StylizedColor.WoodLight, new Vector3(-1.45f, 0.3f, 0.3f), new Vector3(0.5f, 0.6f, 0.5f), 0.04f);
        b.AddBeveledBox(StylizedColor.Leather, new Vector3(-1.4f, 0.2f, -0.35f), new Vector3(0.4f, 0.4f, 0.4f), 0.06f);
    }

    // 101일차: 연금술 공방 (5 × 4m, 보랏빛 뾰족 지붕 · 초록 연기 굴뚝 · 둥근 창 · 약병 카운터 · 가마솥, 문은 앞면 왼쪽)
    private static void BuildNpcAlchemyLab(LowPolyMeshBuilder b)
    {
        b.AddBox(StylizedColor.StoneDark, new Vector3(0f, 0.1f, 0f), new Vector3(5.2f, 0.2f, 4.2f));
        b.AddBox(StylizedColor.Stone, new Vector3(0f, 0.6f, 0f), new Vector3(5f, 0.8f, 4f));
        b.AddBox(StylizedColor.Bark, new Vector3(0f, 1.8f, 0f), new Vector3(5f, 1.6f, 4f));

        foreach (float x in new[] { -2.45f, -0.2f, 2.45f })
        {
            b.AddBox(StylizedColor.BarkDark, new Vector3(x, 1.8f, 2.01f), new Vector3(0.14f, 1.6f, 0.06f));
        }

        b.AddBox(StylizedColor.BerryPurple, new Vector3(0f, 2.62f, 2.01f), new Vector3(5f, 0.1f, 0.06f));

        // 뾰족한 보랏빛 지붕 · 굴뚝 · 초록 연기
        b.AddWedge(StylizedColor.BerryPurple, new Vector3(0f, 3.25f, 0f), new Vector3(5.6f, 1.5f, 4.6f));
        b.AddBox(StylizedColor.Gold, new Vector3(0f, 4.02f, 0f), new Vector3(5.62f, 0.05f, 0.08f));
        b.AddBox(StylizedColor.StoneDark, new Vector3(1.7f, 3.7f, -1f), new Vector3(0.5f, 1.4f, 0.5f));

        for (int index = 0; index < 3; index++)
        {
            b.AddLowPolySphere(StylizedColor.SpitterSac, new Vector3(1.7f + index * 0.12f, 4.55f + index * 0.35f, -1f + index * 0.08f), Vector3.one * (0.18f - index * 0.04f), 0, 0f, 9150 + index);
        }

        // 문 (왼쪽) · 둥근 창 (오른쪽 위) · 간판
        b.AddBox(StylizedColor.WoodDark, new Vector3(-0.9f, 1.15f, 2.02f), new Vector3(0.95f, 1.9f, 0.06f));
        b.AddLowPolySphere(StylizedColor.Gold, new Vector3(-0.58f, 1.1f, 2.07f), Vector3.one * 0.05f, 0, 0f, 9153);
        b.Push(new Vector3(1.6f, 1.95f, 2.02f), Euler(90f, 0f, 0f), Vector3.one);
        b.AddCylinder(StylizedColor.WoodDark, Vector3.zero, 0.42f, 0.06f, 10);
        b.AddDisc(StylizedColor.LampGlow, new Vector3(0f, 0.065f, 0f), 0.34f, 10);
        b.Pop();
        b.AddLimb(StylizedColor.WoodDark, new Vector3(-2.2f, 2.3f, 2.05f), new Vector3(-2.2f, 2.3f, 2.7f), 0.03f, 0.03f, 5);
        b.AddBox(StylizedColor.WoodLight, new Vector3(-2.2f, 1.95f, 2.7f), new Vector3(0.06f, 0.5f, 0.6f));
        b.AddCylinder(StylizedColor.Crystal, new Vector3(-2.16f, 1.8f, 2.7f), 0.08f, 0.22f, 6);

        // 약병 카운터 (NPC는 카운터와 벽 사이에 선다)
        b.AddBox(StylizedColor.WoodDark, new Vector3(1.6f, 0.5f, 3.6f), new Vector3(1.6f, 1f, 0.55f));
        b.AddBox(StylizedColor.BerryPurple, new Vector3(1.6f, 1.02f, 3.6f), new Vector3(1.7f, 0.06f, 0.65f));
        StylizedColor[] potions = { StylizedColor.AppleRed, StylizedColor.Crystal, StylizedColor.SpitterSac, StylizedColor.BerryPurple, StylizedColor.FlowerYellow };

        for (int index = 0; index < potions.Length; index++)
        {
            Vector3 bottle = new Vector3(1.0f + index * 0.3f, 1.05f, 3.55f + (index % 2) * 0.12f);
            b.AddCylinder(StylizedColor.Glass, bottle, 0.07f, 0.24f, 6);
            b.AddCylinder(potions[index], bottle + new Vector3(0f, 0.02f, 0f), 0.06f, 0.14f, 6);
            b.AddCylinder(StylizedColor.Leather, bottle + new Vector3(0f, 0.24f, 0f), 0.03f, 0.05f, 5);
        }

        // 말린 약초 · 가마솥
        for (int index = 0; index < 3; index++)
        {
            b.AddLimb(StylizedColor.Herb, new Vector3(-0.1f + index * 0.25f, 2.55f, 2.2f), new Vector3(-0.1f + index * 0.25f, 2.2f, 2.22f), 0.05f, 0.02f, 4);
        }

        b.AddLowPolySphere(StylizedColor.IronDark, new Vector3(-2.3f, 0.4f, 2.8f), new Vector3(0.42f, 0.36f, 0.42f), 1, 0f, 9154);
        b.AddDisc(StylizedColor.SpitterSac, new Vector3(-2.3f, 0.7f, 2.8f), 0.33f, 10);
        b.AddLowPolySphere(StylizedColor.Fire, new Vector3(-2.3f, 0.06f, 2.8f), new Vector3(0.25f, 0.08f, 0.25f), 0, 0f, 9155);
    }

    // 101일차: 폐품 작업장 (5 × 4m 열린 창고, 함석 벽 · 기울어진 지붕 · 작업대 · 고철 더미 · 풍차 안테나)
    private static void BuildNpcScrapYard(LowPolyMeshBuilder b)
    {
        b.AddBox(StylizedColor.StoneDark, new Vector3(0f, 0.05f, 0f), new Vector3(5f, 0.1f, 4f));
        b.AddBox(StylizedColor.IronDark, new Vector3(0f, 1.2f, -1.85f), new Vector3(5f, 2.4f, 0.2f));

        for (int index = 0; index < 9; index++)
        {
            b.AddBox(index % 3 == 0 ? StylizedColor.Copper : StylizedColor.Iron, new Vector3(-2.2f + index * 0.55f, 1.2f, -1.73f), new Vector3(0.1f, 2.3f, 0.05f));
        }

        foreach (float x in new[] { -2.35f, 2.35f })
        {
            foreach (float z in new[] { 1.85f, -1.7f })
            {
                b.AddBox(StylizedColor.WoodDark, new Vector3(x, 1.3f, z), new Vector3(0.18f, 2.6f, 0.18f));
            }
        }

        b.Push(new Vector3(0f, 2.75f, 0f), Euler(-8f, 0f, 0f), Vector3.one);
        b.AddBox(StylizedColor.IronDark, Vector3.zero, new Vector3(5.5f, 0.08f, 4.5f));
        b.AddBox(StylizedColor.Copper, new Vector3(-1.2f, 0.05f, 0.8f), new Vector3(1.4f, 0.03f, 1.1f));
        b.AddBox(StylizedColor.Copper, new Vector3(1.5f, 0.05f, -1f), new Vector3(0.9f, 0.03f, 0.8f));
        b.Pop();

        // 작업대 · 바이스 · 공구 · 등불
        b.AddBox(StylizedColor.WoodPlank, new Vector3(0.8f, 0.95f, -1.05f), new Vector3(2.1f, 0.1f, 0.8f));

        foreach (float x in new[] { -0.15f, 1.75f })
        {
            b.AddBox(StylizedColor.WoodDark, new Vector3(x, 0.45f, -1.05f), new Vector3(0.1f, 0.9f, 0.7f));
        }

        b.AddBeveledBox(StylizedColor.IronDark, new Vector3(1.5f, 1.08f, -0.8f), new Vector3(0.24f, 0.16f, 0.2f), 0.02f);
        b.AddLimb(StylizedColor.Iron, new Vector3(0.3f, 1.02f, -1.2f), new Vector3(0.75f, 1.02f, -0.9f), 0.02f, 0.02f, 5);
        b.Push(new Vector3(0.9f, 1.03f, -1.2f), Quaternion.identity, Vector3.one);
        b.AddTorus(StylizedColor.Copper, Vector3.zero, 0.12f, 0.03f, 8, 4);
        b.Pop();
        b.AddLimb(StylizedColor.IronDark, new Vector3(-0.1f, 2.7f, -1f), new Vector3(-0.1f, 2.1f, -1f), 0.01f, 0.01f, 3);
        b.AddLowPolySphere(StylizedColor.LampGlow, new Vector3(-0.1f, 2.02f, -1f), Vector3.one * 0.09f, 0, 0f, 9160);

        // 고철 더미 (상자 · 통 · 바퀴 · 관 · 톱니)
        b.AddBeveledBox(StylizedColor.WoodLight, new Vector3(-1.9f, 0.3f, -1.2f), new Vector3(0.7f, 0.6f, 0.6f), 0.03f);
        b.AddBeveledBox(StylizedColor.WoodPlank, new Vector3(-1.75f, 0.85f, -1.15f), new Vector3(0.55f, 0.5f, 0.5f), 0.03f);
        b.AddCylinder(StylizedColor.IronDark, new Vector3(-2.0f, 0f, -0.4f), 0.3f, 0.8f, 8);
        b.Push(new Vector3(-1.4f, 0.35f, -0.3f), Euler(90f, 20f, 0f), Vector3.one);
        b.AddTorus(StylizedColor.Black, Vector3.zero, 0.3f, 0.1f, 10, 5);
        b.Pop();
        b.AddLimb(StylizedColor.Iron, new Vector3(-2.3f, 0.1f, 0.2f), new Vector3(-1.2f, 0.5f, -0.1f), 0.06f, 0.06f, 6);
        b.Push(new Vector3(-1.9f, 1.25f, -1.1f), Euler(80f, 0f, 10f), Vector3.one);
        b.AddTorus(StylizedColor.Copper, Vector3.zero, 0.2f, 0.05f, 10, 4);
        b.Pop();

        // 지붕 위 풍차 안테나 · 간판
        b.AddLimb(StylizedColor.WoodDark, new Vector3(2f, 2.9f, -1.5f), new Vector3(2f, 4.2f, -1.5f), 0.05f, 0.04f, 5);

        for (int blade = 0; blade < 4; blade++)
        {
            b.Push(new Vector3(2f, 4.2f, -1.4f), Euler(0f, 0f, blade * 90f + 20f), Vector3.one);
            b.AddBox(StylizedColor.Iron, new Vector3(0f, 0.3f, 0f), new Vector3(0.12f, 0.55f, 0.02f));
            b.Pop();
        }

        b.AddBox(StylizedColor.WoodLight, new Vector3(0f, 2.55f, 1.95f), new Vector3(1.6f, 0.4f, 0.06f));
        b.Push(new Vector3(0.55f, 2.55f, 2f), Euler(90f, 0f, 0f), Vector3.one);
        b.AddTorus(StylizedColor.Copper, Vector3.zero, 0.12f, 0.03f, 8, 3);
        b.Pop();
    }

    private static void BuildVillageBoard(LowPolyMeshBuilder b)
    {
        foreach (float x in new[] { -0.7f, 0.7f })
        {
            b.AddLimb(StylizedColor.WoodDark, new Vector3(x, 0f, 0f), new Vector3(x, 2.1f, 0f), 0.06f, 0.05f, 6);
        }

        b.AddBox(StylizedColor.WoodLight, new Vector3(0f, 1.35f, 0.02f), new Vector3(1.6f, 1f, 0.08f));
        b.AddWedge(StylizedColor.WoodDark, new Vector3(0f, 2.05f, 0f), new Vector3(1.9f, 0.3f, 0.5f));
        b.AddBox(StylizedColor.ClothCream, new Vector3(-0.4f, 1.45f, 0.07f), new Vector3(0.4f, 0.5f, 0.01f));
        b.AddBox(StylizedColor.SeedPaper, new Vector3(0.15f, 1.3f, 0.07f), new Vector3(0.36f, 0.44f, 0.01f));
        b.AddBox(StylizedColor.ClothCream, new Vector3(0.52f, 1.5f, 0.07f), new Vector3(0.3f, 0.36f, 0.01f));

        foreach (Vector2 pin in new[] { new Vector2(-0.4f, 1.66f), new Vector2(0.15f, 1.49f), new Vector2(0.52f, 1.65f) })
        {
            b.AddLowPolySphere(StylizedColor.AppleRed, new Vector3(pin.x, pin.y, 0.085f), Vector3.one * 0.025f, 0, 0f, 9140);
        }
    }
}
