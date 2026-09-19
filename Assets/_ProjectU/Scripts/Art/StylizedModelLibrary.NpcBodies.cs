using System.Collections.Generic;
using UnityEngine;

// 100일차: 확장 NPC 28명의 저폴리 모델
// 1. 하반신 모양 (NpcBody) : 뱀 꼬리 · 거미 다리 · 말 몸 · 물고기 꼬리 · 촉수 · 전갈 몸 · 떠 있는 옷자락 · 슬라임 · 보물상자
// 2. 종족 부속 : 귀 · 뿔 · 더듬이 · 꼬리 · 꽃 · 머리를 든 듀라한 등
// 3. 날개 (NpcWings) : 새 · 박쥐 · 요정 · 나비 · 벌
// 좌표는 사람 체형(키 1.8) 기준이며, 하반신이 다르면 상반신을 Lift만큼 올리고 몸 전체를 Shift만큼 앞으로 옮겨
// 발 넓이(충돌 중심)가 NPC 기준점 가까이 오게 한다.
public static partial class StylizedModelLibrary
{
    public const float NpcHipHeight = 0.86f; // 사람 체형의 허리(골반) 높이

    public readonly struct NpcBodyShape
    {
        public readonly float Lift; // 상반신을 올리는 높이 (사람 체형 기준 단위)
        public readonly float Shift; // 몸 전체를 앞(+Z)으로 옮기는 거리
        public readonly float Hover; // 바닥에서 떠 있는 높이
        public readonly float FootprintRadius; // 바닥에서 차지하는 반지름
        public readonly float FootprintLength; // 앞뒤 길이

        public NpcBodyShape(float lift, float shift, float hover, float footprintRadius, float footprintLength)
        {
            Lift = lift;
            Shift = shift;
            Hover = hover;
            FootprintRadius = footprintRadius;
            FootprintLength = footprintLength;
        }
    }

    private static Dictionary<string, NpcLook> npcLooks;

    public static NpcBodyShape GetBodyShape(NpcBody body)
    {
        switch (body)
        {
            case NpcBody.SnakeTail: return new NpcBodyShape(0.12f, 0.35f, 0f, 0.55f, 1.7f);
            case NpcBody.SpiderLegs: return new NpcBodyShape(0.25f, 0.35f, 0f, 0.8f, 1.8f);
            case NpcBody.HorseBody: return new NpcBodyShape(0.55f, 0.55f, 0f, 0.4f, 1.85f);
            case NpcBody.FishTail: return new NpcBodyShape(-0.05f, 0.4f, 0f, 0.35f, 1.3f);
            case NpcBody.Tentacles: return new NpcBodyShape(0f, 0f, 0f, 0.95f, 2.1f);
            case NpcBody.ScorpionBody: return new NpcBodyShape(0.1f, 0.4f, 0f, 0.7f, 1.9f);
            case NpcBody.Floating: return new NpcBodyShape(0f, 0f, 0.16f, 0.32f, 0.64f);
            case NpcBody.SlimeBase: return new NpcBodyShape(-0.15f, 0f, 0f, 0.55f, 1.1f);
            case NpcBody.MimicChest: return new NpcBodyShape(-0.3f, 0f, 0f, 0.5f, 0.66f);
            default: return new NpcBodyShape(0f, 0f, 0f, 0.3f, 0.6f);
        }
    }

    // 모델 ID → 모양 값 (생성 도구 · 검사에서 사용)
    public static bool TryGetNpcLook(string modelId, out NpcLook look)
    {
        _ = Catalog; // 도감을 먼저 채운다

        if (npcLooks != null && !string.IsNullOrEmpty(modelId) && npcLooks.TryGetValue(modelId, out look))
        {
            return true;
        }

        look = default;
        return false;
    }

    public static IEnumerable<string> NpcModelIds
    {
        get
        {
            _ = Catalog;
            return npcLooks != null ? npcLooks.Keys : (IEnumerable<string>)new string[0];
        }
    }

    // 사람 체형 기준 모델 윗부분 높이 (m, 날개 · 모자 제외)
    public static float GetNpcStandingHeight(NpcLook look)
    {
        NpcBodyShape shape = GetBodyShape(look.Body);
        float lowered = look.Body == NpcBody.Legs ? 0f : shape.Lift + shape.Hover;
        return (NpcBaseHeight + lowered) * look.Height / NpcBaseHeight;
    }

    private static StylizedColor GetNpcSkin(NpcSpecies species)
    {
        switch (species)
        {
            case NpcSpecies.Ghost:
            case NpcSpecies.Vampire:
                return StylizedColor.SkinPale;
            case NpcSpecies.Android:
                return StylizedColor.SkinCeramic;
            case NpcSpecies.Goblin:
                return StylizedColor.SkinGoblin;
            case NpcSpecies.Slime:
                return StylizedColor.SlimeJelly;
            default:
                return StylizedColor.Skin;
        }
    }

    private static StylizedColor GetNpcHairColor(NpcSpecies species)
    {
        return species == NpcSpecies.Slime ? StylizedColor.SlimeJelly : StylizedColor.NpcHair;
    }

    // ---------------------------------------------------------------- 28명 모양 값 (캐릭터 시트 종족 · 키 · 외형 기준)

    private static void RegisterExpansionNpcs()
    {
        // 2차 : 상인 · 제작형
        RegisterNpcModel("npc_bellamorta", new NpcLook(NpcSpecies.Succubus, NpcHairStyle.Wavy, NpcOutfitStyle.Robe, 1.7f, NpcBody.Legs, NpcWings.Bat));
        RegisterNpcModel("npc_arachne", new NpcLook(NpcSpecies.Arachne, NpcHairStyle.Long, NpcOutfitStyle.Wrap, 1.7f, NpcBody.SpiderLegs));
        RegisterNpcModel("npc_milu", new NpcLook(NpcSpecies.Slime, NpcHairStyle.Bob, NpcOutfitStyle.Wrap, 1.6f, NpcBody.SlimeBase));
        RegisterNpcModel("npc_kasumi", new NpcLook(NpcSpecies.Fox, NpcHairStyle.Long, NpcOutfitStyle.Kimono, 1.66f));
        RegisterNpcModel("npc_seira", new NpcLook(NpcSpecies.Lamia, NpcHairStyle.Long, NpcOutfitStyle.Wrap, 1.72f, NpcBody.SnakeTail));
        RegisterNpcModel("npc_pipi", new NpcLook(NpcSpecies.Goblin, NpcHairStyle.Bob, NpcOutfitStyle.Smith, 1.46f));
        RegisterNpcModel("npc_safira", new NpcLook(NpcSpecies.Scorpion, NpcHairStyle.Long, NpcOutfitStyle.Wrap, 1.69f, NpcBody.ScorpionBody));

        // 3차 : 자연 · 수집형
        RegisterNpcModel("npc_serena", new NpcLook(NpcSpecies.Ghost, NpcHairStyle.Long, NpcOutfitStyle.Gothic, 1.65f, NpcBody.Floating));
        RegisterNpcModel("npc_liriel", new NpcLook(NpcSpecies.Fairy, NpcHairStyle.Twin, NpcOutfitStyle.Dress, 1.42f, NpcBody.Legs, NpcWings.Fairy));
        RegisterNpcModel("npc_chesca", new NpcLook(NpcSpecies.Mimic, NpcHairStyle.Long, NpcOutfitStyle.Gothic, 1.6f, NpcBody.MimicChest));
        RegisterNpcModel("npc_marielle", new NpcLook(NpcSpecies.Mermaid, NpcHairStyle.Wavy, NpcOutfitStyle.Wrap, 1.64f, NpcBody.FishTail));
        RegisterNpcModel("npc_lumina", new NpcLook(NpcSpecies.Butterfly, NpcHairStyle.Long, NpcOutfitStyle.Dress, 1.63f, NpcBody.Legs, NpcWings.Butterfly));
        RegisterNpcModel("npc_neri", new NpcLook(NpcSpecies.Frog, NpcHairStyle.Bob, NpcOutfitStyle.Shorts, 1.54f));
        RegisterNpcModel("npc_aliune", new NpcLook(NpcSpecies.Flower, NpcHairStyle.Wavy, NpcOutfitStyle.Dress, 1.71f));

        // 4차 : 동료형
        RegisterNpcModel("npc_eiri", new NpcLook(NpcSpecies.Harpy, NpcHairStyle.Short, NpcOutfitStyle.Kimono, 1.68f, NpcBody.Legs, NpcWings.Bird));
        RegisterNpcModel("npc_rize", new NpcLook(NpcSpecies.Android, NpcHairStyle.Bob, NpcOutfitStyle.Suit, 1.72f));
        RegisterNpcModel("npc_ragh", new NpcLook(NpcSpecies.Wolf, NpcHairStyle.Short, NpcOutfitStyle.Armor, 1.78f));
        RegisterNpcModel("npc_erina", new NpcLook(NpcSpecies.Elf, NpcHairStyle.Long, NpcOutfitStyle.Armor, 1.72f));
        RegisterNpcModel("npc_harka", new NpcLook(NpcSpecies.Dog, NpcHairStyle.Ponytail, NpcOutfitStyle.Shorts, 1.7f));
        RegisterNpcModel("npc_beatrice", new NpcLook(NpcSpecies.Bee, NpcHairStyle.Ponytail, NpcOutfitStyle.Armor, 1.66f, NpcBody.Legs, NpcWings.Bee));
        RegisterNpcModel("npc_censia", new NpcLook(NpcSpecies.Centaur, NpcHairStyle.Ponytail, NpcOutfitStyle.Armor, 1.7f, NpcBody.HorseBody));

        // 5차 : 동료 · 특수형
        RegisterNpcModel("npc_lanhua", new NpcLook(NpcSpecies.Tiger, NpcHairStyle.Ponytail, NpcOutfitStyle.Kimono, 1.71f));
        RegisterNpcModel("npc_ravenna", new NpcLook(NpcSpecies.Dullahan, NpcHairStyle.Bob, NpcOutfitStyle.Armor, 1.68f));
        RegisterNpcModel("npc_akane", new NpcLook(NpcSpecies.Oni, NpcHairStyle.Long, NpcOutfitStyle.Kimono, 1.72f));
        RegisterNpcModel("npc_sharia", new NpcLook(NpcSpecies.Shark, NpcHairStyle.Short, NpcOutfitStyle.Suit, 1.75f));
        RegisterNpcModel("npc_octavia", new NpcLook(NpcSpecies.Kraken, NpcHairStyle.Long, NpcOutfitStyle.Cape, 1.66f, NpcBody.Tentacles));
        RegisterNpcModel("npc_lilica", new NpcLook(NpcSpecies.Vampire, NpcHairStyle.Twin, NpcOutfitStyle.Gothic, 1.35f));
        RegisterNpcModel("npc_noctia", new NpcLook(NpcSpecies.Witch, NpcHairStyle.Wavy, NpcOutfitStyle.Gothic, 1.7f));
    }

    // ---------------------------------------------------------------- 공통 도우미

    // 점을 차례로 잇는 굵기가 변하는 관 (꼬리 · 촉수 · 다리). 관절마다 구를 넣어 이음새를 메운다.
    private static void AddTube(LowPolyMeshBuilder b, StylizedColor color, Vector3[] points, float[] radii, int sides, int seed)
    {
        for (int index = 0; index < points.Length - 1; index++)
        {
            b.AddLimb(color, points[index], points[index + 1], radii[index], radii[index + 1], sides);

            if (index > 0 && radii[index] > 0.02f)
            {
                b.AddLowPolySphere(color, points[index], Vector3.one * radii[index], 0, 0f, seed + index);
            }
        }
    }

    // 양면 평면 다각형 (날개막 · 지느러미)
    private static void AddPanel(LowPolyMeshBuilder b, StylizedColor color, params Vector3[] points)
    {
        for (int index = 1; index < points.Length - 1; index++)
        {
            b.AddTriangle(color, points[0], points[index], points[index + 1]);
            b.AddTriangle(color, points[0], points[index + 1], points[index]);
        }
    }

    // 등 뒤 망토 (Cape 옷과 같은 모양)
    private static void AddBackCape(LowPolyMeshBuilder b, StylizedColor color, StylizedColor collar)
    {
        b.Push(new Vector3(0f, 1.34f, -0.13f), Euler(-8f, 0f, 0f), Vector3.one);
        b.AddFrustum(color, new Vector3(0f, -0.76f, -0.02f), 0.3f, 0.2f, 0.76f, 8, false, false, 22.5f);
        b.Pop();
        b.AddTorus(collar, new Vector3(0f, 1.36f, 0f), 0.13f, 0.045f, 8, 4);
    }

    // 한쪽 손 위치 (side = 1 오른손, -1 왼손)
    private static Vector3 NpcHand(int side)
    {
        return new Vector3(side * 0.305f, 0.81f, 0.05f);
    }

    // ---------------------------------------------------------------- 하반신

    private static void AddNpcLowerBody(LowPolyMeshBuilder b, NpcLook look, StylizedColor skin, float hipY)
    {
        switch (look.Body)
        {
            case NpcBody.SnakeTail:
                AddSnakeTail(b, hipY);
                break;
            case NpcBody.SpiderLegs:
                AddSpiderBody(b, hipY);
                break;
            case NpcBody.HorseBody:
                AddHorseBody(b, skin, hipY);
                break;
            case NpcBody.FishTail:
                AddFishTail(b, hipY);
                break;
            case NpcBody.Tentacles:
                AddTentacles(b, hipY);
                break;
            case NpcBody.ScorpionBody:
                AddScorpionBody(b, skin, hipY);
                break;
            case NpcBody.Floating:
                AddFloatingSkirt(b, hipY);
                break;
            case NpcBody.SlimeBase:
                AddSlimeBase(b, hipY);
                break;
            case NpcBody.MimicChest:
                AddMimicChest(b, hipY);
                break;
            default:
                AddNpcLegs(b, look.Outfit, skin);
                break;
        }
    }

    // 라미아 : 허리에서 내려와 뒤로 S자로 감기는 긴 뱀 꼬리 (앞면은 크림색 배 비늘)
    private static void AddSnakeTail(LowPolyMeshBuilder b, float hipY)
    {
        Vector3[] points =
        {
            new Vector3(0f, hipY + 0.04f, 0f), new Vector3(0f, 0.62f, 0.04f), new Vector3(0f, 0.28f, -0.02f),
            new Vector3(0.28f, 0.17f, -0.34f), new Vector3(0.36f, 0.15f, -0.8f), new Vector3(0.05f, 0.13f, -1.16f),
            new Vector3(-0.38f, 0.11f, -1.28f), new Vector3(-0.66f, 0.09f, -1.06f), new Vector3(-0.72f, 0.07f, -0.7f),
            new Vector3(-0.6f, 0.05f, -0.46f)
        };
        float[] radii = { 0.17f, 0.19f, 0.2f, 0.18f, 0.16f, 0.13f, 0.1f, 0.075f, 0.045f, 0.012f };
        AddTube(b, StylizedColor.SnakeScale, points, radii, 8, 9200);
        b.AddLimb(StylizedColor.Bone, new Vector3(0f, hipY - 0.04f, 0.1f), new Vector3(0f, 0.3f, 0.12f), 0.1f, 0.13f, 6);
        b.AddTorus(StylizedColor.NpcAccent, new Vector3(0f, hipY, 0f), 0.18f, 0.03f, 10, 4);

        // 등쪽 짙은 무늬
        for (int index = 3; index < 8; index++)
        {
            b.AddLowPolySphere(StylizedColor.Chitin, points[index] + new Vector3(0f, radii[index] * 0.82f, 0f), new Vector3(radii[index] * 0.45f, 0.02f, radii[index] * 0.6f), 0, 0f, 9215 + index);
        }
    }

    // 아라크네 : 머리가슴 · 큰 배 · 다리 8개
    private static void AddSpiderBody(LowPolyMeshBuilder b, float hipY)
    {
        b.AddLimb(StylizedColor.Chitin, new Vector3(0f, hipY - 0.16f, 0.02f), new Vector3(0f, hipY + 0.04f, 0f), 0.15f, 0.16f, 8);
        b.AddLowPolySphere(StylizedColor.Chitin, new Vector3(0f, 0.95f, -0.12f), new Vector3(0.3f, 0.2f, 0.34f), 1, 0f, 9230);
        b.AddLowPolySphere(StylizedColor.Chitin, new Vector3(0f, 1.02f, -0.8f), new Vector3(0.4f, 0.34f, 0.48f), 1, 0.02f, 9231);
        b.AddLowPolySphere(StylizedColor.NpcAccent, new Vector3(0f, 1.3f, -0.84f), new Vector3(0.1f, 0.06f, 0.2f), 0, 0f, 9232);
        b.AddLowPolySphere(StylizedColor.NpcAccent, new Vector3(0f, 1.24f, -1.05f), new Vector3(0.07f, 0.05f, 0.08f), 0, 0f, 9233);
        b.AddCone(StylizedColor.Chitin, new Vector3(0f, 0.96f, -1.22f), 0.06f, 0.1f, 5);

        for (int side = -1; side <= 1; side += 2)
        {
            for (int index = 0; index < 4; index++)
            {
                Vector3 root = new Vector3(side * 0.22f, 0.95f, 0.06f - index * 0.13f);
                Vector3 knee = new Vector3(side * (0.52f + index * 0.03f), 1.32f - index * 0.04f, 0.3f - index * 0.3f);
                Vector3 foot = new Vector3(side * (0.78f + index * 0.04f), 0f, 0.52f - index * 0.5f);
                b.AddLimb(StylizedColor.Chitin, root, knee, 0.045f, 0.04f, 5);
                b.AddLowPolySphere(StylizedColor.NpcAccent, knee, Vector3.one * 0.05f, 0, 0f, 9240 + index);
                b.AddLimb(StylizedColor.Chitin, knee, foot, 0.04f, 0.012f, 5);
            }
        }
    }

    // 켄타우로스 : 말 몸통 · 다리 4개 · 꼬리 · 안장 담요와 짐 가방
    private static void AddHorseBody(LowPolyMeshBuilder b, StylizedColor skin, float hipY)
    {
        const StylizedColor coat = StylizedColor.HorseCoat;
        b.AddLimb(skin, new Vector3(0f, 1.25f, 0.02f), new Vector3(0f, hipY + 0.04f, 0f), 0.17f, 0.16f, 8);
        b.AddLowPolySphere(coat, new Vector3(0f, 1.16f, -0.02f), new Vector3(0.3f, 0.32f, 0.3f), 1, 0f, 9250);
        b.AddLimb(coat, new Vector3(0f, 1.16f, -0.1f), new Vector3(0f, 1.2f, -1.02f), 0.3f, 0.28f, 8);
        b.AddLowPolySphere(coat, new Vector3(0f, 1.2f, -1.08f), new Vector3(0.3f, 0.3f, 0.3f), 1, 0f, 9251);

        for (int side = -1; side <= 1; side += 2)
        {
            // 앞다리
            Vector3 frontTop = new Vector3(side * 0.15f, 1.0f, -0.02f);
            Vector3 frontKnee = new Vector3(side * 0.15f, 0.52f, 0.02f);
            Vector3 frontAnkle = new Vector3(side * 0.15f, 0.12f, 0f);
            b.AddLimb(coat, frontTop, frontKnee, 0.075f, 0.055f, 6);
            b.AddLimb(coat, frontKnee, frontAnkle, 0.05f, 0.045f, 6);
            b.AddCylinder(StylizedColor.Black, new Vector3(side * 0.15f, 0f, 0.01f), 0.06f, 0.1f, 6);

            // 뒷다리 (뒤로 꺾인 관절)
            Vector3 backTop = new Vector3(side * 0.15f, 1.02f, -1.08f);
            Vector3 backHock = new Vector3(side * 0.15f, 0.55f, -1.2f);
            Vector3 backAnkle = new Vector3(side * 0.15f, 0.12f, -1.12f);
            b.AddLimb(coat, backTop, backHock, 0.085f, 0.055f, 6);
            b.AddLimb(coat, backHock, backAnkle, 0.05f, 0.045f, 6);
            b.AddCylinder(StylizedColor.Black, new Vector3(side * 0.15f, 0f, -1.12f), 0.06f, 0.1f, 6);

            // 짐 가방
            b.AddBeveledBox(StylizedColor.Leather, new Vector3(side * 0.33f, 1.2f, -0.7f), new Vector3(0.08f, 0.26f, 0.36f), 0.03f);
        }

        AddTube(b, StylizedColor.NpcHair, new[] { new Vector3(0f, 1.32f, -1.34f), new Vector3(0f, 1.0f, -1.5f), new Vector3(0.02f, 0.62f, -1.46f) }, new[] { 0.06f, 0.07f, 0.02f }, 6, 9255);
        b.AddBox(StylizedColor.NpcAccent, new Vector3(0f, 1.47f, -0.62f), new Vector3(0.64f, 0.06f, 0.56f));
        b.AddBox(StylizedColor.Leather, new Vector3(0f, 1.49f, -0.62f), new Vector3(0.66f, 0.04f, 0.08f));
    }

    // 인어 : 허리에서 내려와 뒤로 눕는 물고기 꼬리 · 꼬리지느러미 · 옆 지느러미
    private static void AddFishTail(LowPolyMeshBuilder b, float hipY)
    {
        Vector3[] points =
        {
            new Vector3(0f, hipY + 0.04f, 0f), new Vector3(0f, 0.52f, 0.06f), new Vector3(0f, 0.24f, 0f),
            new Vector3(0f, 0.12f, -0.36f), new Vector3(0f, 0.11f, -0.7f), new Vector3(0f, 0.14f, -0.92f)
        };
        float[] radii = { 0.16f, 0.17f, 0.15f, 0.11f, 0.07f, 0.04f };
        AddTube(b, StylizedColor.MermaidScale, points, radii, 8, 9260);
        b.AddTorus(StylizedColor.Coral, new Vector3(0f, hipY, 0f), 0.17f, 0.03f, 10, 4);

        for (int side = -1; side <= 1; side += 2)
        {
            b.Push(new Vector3(side * 0.14f, 0.2f, -1.02f), Euler(-12f, side * 35f, 0f), Vector3.one);
            b.AddLowPolySphere(StylizedColor.FishBlue, Vector3.zero, new Vector3(0.18f, 0.025f, 0.1f), 1, 0f, 9270 + side);
            b.Pop();
            b.Push(new Vector3(side * 0.17f, 0.52f, 0.02f), Euler(0f, side * 20f, side * 25f), Vector3.one);
            b.AddLowPolySphere(StylizedColor.FishBlue, Vector3.zero, new Vector3(0.02f, 0.1f, 0.07f), 0, 0f, 9273 + side);
            b.Pop();
        }
    }

    // 크라켄 : 치마 모양 몸통 아래로 퍼지는 촉수 8개 (위쪽에 빨판 점)
    private static void AddTentacles(LowPolyMeshBuilder b, float hipY)
    {
        b.AddLowPolySphere(StylizedColor.KrakenSkin, new Vector3(0f, 0.28f, 0f), new Vector3(0.34f, 0.18f, 0.34f), 1, 0f, 9280);
        b.AddFrustum(StylizedColor.KrakenSkin, new Vector3(0f, 0.28f, 0f), 0.32f, 0.17f, hipY + 0.04f - 0.28f, 8);

        for (int index = 0; index < 8; index++)
        {
            float angle = (index * 45f + 22.5f) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
            Vector3 curl = new Vector3(Mathf.Sin(angle + 0.52f), 0f, Mathf.Cos(angle + 0.52f));
            Vector3 tip = new Vector3(Mathf.Sin(angle + 1.05f), 0f, Mathf.Cos(angle + 1.05f));
            Vector3[] points =
            {
                dir * 0.22f + Vector3.up * 0.26f, dir * 0.5f + Vector3.up * 0.1f, dir * 0.82f + Vector3.up * 0.07f,
                curl * 1.05f + Vector3.up * 0.1f, tip * 1.08f + Vector3.up * 0.22f
            };
            AddTube(b, StylizedColor.KrakenSkin, points, new[] { 0.1f, 0.085f, 0.06f, 0.035f, 0.012f }, 6, 9281 + index * 5);
            b.AddLowPolySphere(StylizedColor.CowPink, points[1] + Vector3.up * 0.06f, Vector3.one * 0.025f, 0, 0f, 9330 + index);
            b.AddLowPolySphere(StylizedColor.CowPink, points[2] + Vector3.up * 0.045f, Vector3.one * 0.02f, 0, 0f, 9340 + index);
        }
    }

    // 스콜피온 : 앞 몸통 · 마디진 배 · 등 위로 휘어 오는 독침 꼬리 · 다리 6개 · 작은 집게 2개
    private static void AddScorpionBody(LowPolyMeshBuilder b, StylizedColor skin, float hipY)
    {
        const StylizedColor shell = StylizedColor.ScorpionShell;
        b.AddLimb(skin, new Vector3(0f, 0.72f, 0.04f), new Vector3(0f, hipY + 0.04f, 0f), 0.16f, 0.16f, 8);
        b.AddLowPolySphere(shell, new Vector3(0f, 0.62f, -0.08f), new Vector3(0.28f, 0.2f, 0.32f), 1, 0f, 9350);
        b.AddLowPolySphere(shell, new Vector3(0f, 0.6f, -0.46f), new Vector3(0.3f, 0.19f, 0.26f), 1, 0f, 9351);
        b.AddLowPolySphere(shell, new Vector3(0f, 0.58f, -0.78f), new Vector3(0.28f, 0.18f, 0.24f), 1, 0f, 9352);
        b.AddLowPolySphere(shell, new Vector3(0f, 0.55f, -1.06f), new Vector3(0.23f, 0.16f, 0.2f), 1, 0f, 9353);

        Vector3[] tail =
        {
            new Vector3(0f, 0.6f, -1.2f), new Vector3(0f, 0.9f, -1.42f), new Vector3(0f, 1.3f, -1.44f),
            new Vector3(0f, 1.66f, -1.26f), new Vector3(0f, 1.86f, -0.98f), new Vector3(0f, 1.84f, -0.74f)
        };
        AddTube(b, shell, tail, new[] { 0.1f, 0.1f, 0.09f, 0.08f, 0.07f, 0.05f }, 6, 9355);
        b.AddLowPolySphere(shell, new Vector3(0f, 1.78f, -0.64f), Vector3.one * 0.075f, 0, 0f, 9362);
        b.AddLimb(StylizedColor.Black, new Vector3(0f, 1.76f, -0.6f), new Vector3(0f, 1.6f, -0.47f), 0.035f, 0.003f, 5);

        for (int side = -1; side <= 1; side += 2)
        {
            for (int index = 0; index < 3; index++)
            {
                Vector3 root = new Vector3(side * 0.22f, 0.58f, -0.36f - index * 0.3f);
                Vector3 knee = new Vector3(side * 0.5f, 0.78f, -0.3f - index * 0.34f);
                Vector3 foot = new Vector3(side * 0.72f, 0f, -0.18f - index * 0.42f);
                b.AddLimb(shell, root, knee, 0.04f, 0.035f, 5);
                b.AddLimb(shell, knee, foot, 0.035f, 0.01f, 5);
            }

            // 집게
            b.AddLimb(shell, new Vector3(side * 0.18f, 0.56f, 0.18f), new Vector3(side * 0.32f, 0.5f, 0.42f), 0.045f, 0.04f, 5);
            b.AddLowPolySphere(shell, new Vector3(side * 0.34f, 0.5f, 0.52f), new Vector3(0.07f, 0.05f, 0.1f), 0, 0f, 9365 + side);
            b.AddLimb(StylizedColor.Black, new Vector3(side * 0.31f, 0.52f, 0.58f), new Vector3(side * 0.3f, 0.52f, 0.68f), 0.025f, 0.004f, 4);
            b.AddLimb(StylizedColor.Black, new Vector3(side * 0.37f, 0.48f, 0.58f), new Vector3(side * 0.38f, 0.49f, 0.67f), 0.022f, 0.004f, 4);
        }
    }

    // 유령 : 허리 아래는 치맛자락이 안개로 흐려지며 끝이 뒤로 흩어진다 (몸 전체는 Hover만큼 떠 있음)
    private static void AddFloatingSkirt(LowPolyMeshBuilder b, float hipY)
    {
        b.AddFrustum(StylizedColor.NpcOutfit, new Vector3(0f, 0.42f, 0f), 0.3f, 0.2f, hipY + 0.02f - 0.42f, 10);
        b.AddTorus(StylizedColor.NpcAccent, new Vector3(0f, 0.43f, 0f), 0.3f, 0.03f, 10, 4);
        b.AddFrustum(StylizedColor.SpiritMist, new Vector3(0f, 0.14f, 0f), 0.24f, 0.3f, 0.29f, 10, false, false);
        b.AddLimb(StylizedColor.SpiritMist, new Vector3(0f, 0.16f, 0f), new Vector3(0.04f, -0.08f, -0.32f), 0.24f, 0.03f, 8);

        for (int index = 0; index < 4; index++)
        {
            float angle = index * 1.7f;
            b.AddLowPolySphere(StylizedColor.SpiritMist, new Vector3(Mathf.Cos(angle) * 0.42f, 0.25f + index * 0.22f, Mathf.Sin(angle) * 0.3f - 0.1f), Vector3.one * (0.035f - index * 0.004f), 0, 0f, 9370 + index);
        }
    }

    // 슬라임 : 바닥에 퍼진 젤리 웅덩이에서 몸이 솟아오른다 (가운데 핵 결정)
    private static void AddSlimeBase(LowPolyMeshBuilder b, float hipY)
    {
        b.AddLowPolySphere(StylizedColor.SlimeJelly, new Vector3(0f, 0.14f, 0f), new Vector3(0.55f, 0.13f, 0.55f), 1, 0.06f, 9380);
        b.AddLowPolySphere(StylizedColor.SlimeJelly, new Vector3(0f, 0.34f, 0f), new Vector3(0.3f, 0.28f, 0.3f), 1, 0.03f, 9381);
        b.AddFrustum(StylizedColor.SlimeJelly, new Vector3(0f, 0.45f, 0f), 0.26f, 0.16f, hipY + 0.04f - 0.45f, 8, false, false);
        b.AddLowPolySphere(StylizedColor.Crystal, new Vector3(0f, 0.36f, 0f), Vector3.one * 0.08f, 0, 0f, 9382);

        for (int index = 0; index < 5; index++)
        {
            float angle = index * 1.26f + 0.4f;
            float drop = 0.07f - index * 0.008f;
            b.AddLowPolySphere(StylizedColor.SlimeJelly, new Vector3(Mathf.Cos(angle) * 0.62f, drop, Mathf.Sin(angle) * 0.62f), Vector3.one * drop, 0, 0f, 9383 + index);
        }
    }

    // 미믹 : 뒤로 열린 보물상자 안에서 상반신이 나온다 (테두리 이빨 · 자물쇠 보석 · 혀)
    private static void AddMimicChest(LowPolyMeshBuilder b, float hipY)
    {
        b.AddBeveledBox(StylizedColor.WoodDark, new Vector3(0f, 0.29f, 0f), new Vector3(0.92f, 0.58f, 0.64f), 0.03f);
        b.AddBox(StylizedColor.Black, new Vector3(0f, 0.582f, 0f), new Vector3(0.84f, 0.01f, 0.56f));

        for (int side = -1; side <= 1; side += 2)
        {
            b.AddBox(StylizedColor.IronDark, new Vector3(side * 0.3f, 0.29f, 0f), new Vector3(0.06f, 0.6f, 0.66f));
            b.AddBox(StylizedColor.Gold, new Vector3(side * 0.43f, 0.55f, 0.3f), new Vector3(0.08f, 0.08f, 0.06f));
        }

        b.AddBox(StylizedColor.Gold, new Vector3(0f, 0.46f, 0.325f), new Vector3(0.1f, 0.12f, 0.03f));
        b.AddLowPolySphere(StylizedColor.Crystal, new Vector3(0f, 0.46f, 0.345f), Vector3.one * 0.03f, 0, 0f, 9390);

        for (int index = 0; index < 7; index++)
        {
            b.AddCone(StylizedColor.Bone, new Vector3(-0.36f + index * 0.12f, 0.58f, 0.27f), 0.028f, 0.07f, 4);
        }

        // 뚜껑 (뒤로 열림)
        b.Push(new Vector3(0f, 0.58f, -0.32f), Euler(-115f, 0f, 0f), Vector3.one);
        b.AddBeveledBox(StylizedColor.WoodDark, new Vector3(0f, 0.06f, 0.33f), new Vector3(0.94f, 0.12f, 0.66f), 0.03f);
        b.AddBox(StylizedColor.IronDark, new Vector3(0.3f, 0.06f, 0.33f), new Vector3(0.06f, 0.13f, 0.68f));
        b.AddBox(StylizedColor.IronDark, new Vector3(-0.3f, 0.06f, 0.33f), new Vector3(0.06f, 0.13f, 0.68f));

        for (int index = 0; index < 7; index++)
        {
            b.Push(new Vector3(-0.36f + index * 0.12f, 0f, 0.6f), Euler(180f, 0f, 0f), Vector3.one);
            b.AddCone(StylizedColor.Bone, Vector3.zero, 0.028f, 0.07f, 4);
            b.Pop();
        }

        b.Pop();

        b.Push(new Vector3(0.16f, 0.55f, 0.36f), Euler(35f, 0f, 0f), Vector3.one);
        b.AddLowPolySphere(StylizedColor.CowPink, Vector3.zero, new Vector3(0.07f, 0.02f, 0.11f), 0, 0f, 9391);
        b.Pop();

        b.AddFrustum(StylizedColor.NpcOutfit, new Vector3(0f, 0.4f, 0f), 0.22f, 0.17f, hipY + 0.04f - 0.4f, 8, false, false);
    }

    // ---------------------------------------------------------------- 100일차 옷

    private static void AddNpcOutfitExtra(LowPolyMeshBuilder b, NpcOutfitStyle outfit, bool upperOnly, StylizedColor skin)
    {
        switch (outfit)
        {
            case NpcOutfitStyle.Armor:
                // 갑옷 : 가죽 흉갑 · 어깨 보호대 · 허리 갑주
                if (!upperOnly)
                {
                    b.AddFrustum(StylizedColor.NpcOutfit, new Vector3(0f, 0.64f, 0f), 0.27f, 0.22f, 0.3f, 8);
                }

                b.AddBeveledBox(StylizedColor.NpcOutfit, new Vector3(0f, 1.12f, 0f), new Vector3(0.44f, 0.52f, 0.28f), 0.08f);
                b.AddBeveledBox(StylizedColor.NpcAccent, new Vector3(0f, 1.2f, 0.07f), new Vector3(0.38f, 0.3f, 0.18f), 0.06f);
                b.AddBox(StylizedColor.Leather, new Vector3(0f, 0.92f, 0f), new Vector3(0.46f, 0.07f, 0.3f));
                b.AddBox(StylizedColor.Gold, new Vector3(0f, 0.92f, 0.155f), new Vector3(0.07f, 0.05f, 0.01f));

                for (int side = -1; side <= 1; side += 2)
                {
                    b.AddLowPolySphere(StylizedColor.NpcAccent, new Vector3(side * 0.25f, 1.33f, 0f), new Vector3(0.11f, 0.07f, 0.11f), 1, 0f, 9400 + side);
                }

                break;
            case NpcOutfitStyle.Kimono:
                // 기모노풍 : 겹친 옷깃 · 넓은 허리띠(오비)와 뒤 매듭 · 무릎 아래까지 오는 하카마
                if (!upperOnly)
                {
                    b.AddFrustum(StylizedColor.NpcOutfit, new Vector3(0f, 0.3f, 0f), 0.3f, 0.22f, 0.64f, 10);
                }

                b.AddBeveledBox(StylizedColor.NpcOutfit, new Vector3(0f, 1.11f, 0f), new Vector3(0.42f, 0.52f, 0.27f), 0.08f);

                for (int side = -1; side <= 1; side += 2)
                {
                    b.Push(new Vector3(side * 0.05f, 1.22f, 0.137f), Euler(0f, 0f, -side * 25f), Vector3.one); // V자 옷깃
                    b.AddBox(StylizedColor.White, Vector3.zero, new Vector3(0.05f, 0.3f, 0.01f));
                    b.Pop();
                    b.AddLowPolySphere(StylizedColor.NpcAccent, new Vector3(side * 0.08f, 0.97f, -0.17f), new Vector3(0.08f, 0.05f, 0.03f), 0, 0f, 9405 + side);
                }

                b.AddBox(StylizedColor.NpcAccent, new Vector3(0f, 0.95f, 0f), new Vector3(0.44f, 0.13f, 0.29f));
                b.AddLowPolySphere(StylizedColor.NpcAccent, new Vector3(0f, 0.97f, -0.16f), Vector3.one * 0.04f, 0, 0f, 9407);
                break;
            case NpcOutfitStyle.Suit:
                // 몸에 붙는 보디슈트 : 가운데 · 어깨 선 무늬 · 금속 허리띠 (다리는 옷 색)
                if (!upperOnly)
                {
                    b.AddBeveledBox(StylizedColor.NpcOutfit, new Vector3(0f, 0.82f, 0f), new Vector3(0.36f, 0.2f, 0.24f), 0.05f);
                }

                b.AddBeveledBox(StylizedColor.NpcOutfit, new Vector3(0f, 1.12f, 0f), new Vector3(0.38f, 0.48f, 0.25f), 0.07f);
                b.AddBox(StylizedColor.NpcAccent, new Vector3(0f, 1.12f, 0.127f), new Vector3(0.03f, 0.44f, 0.01f));

                for (int side = -1; side <= 1; side += 2)
                {
                    b.AddBox(StylizedColor.NpcAccent, new Vector3(side * 0.12f, 1.3f, 0.12f), new Vector3(0.1f, 0.03f, 0.01f));
                }

                b.AddBox(StylizedColor.IronDark, new Vector3(0f, 0.92f, 0f), new Vector3(0.39f, 0.05f, 0.26f));
                break;
            case NpcOutfitStyle.Wrap:
                // 가슴을 감싼 짧은 상의 · 드러난 허리 · 허리 장식띠
                if (!upperOnly)
                {
                    b.AddFrustum(StylizedColor.NpcOutfit, new Vector3(0f, 0.64f, 0f), 0.26f, 0.2f, 0.26f, 8);
                }

                b.AddBeveledBox(skin, new Vector3(0f, 1.1f, 0f), new Vector3(0.36f, 0.48f, 0.24f), 0.08f);
                b.AddBeveledBox(StylizedColor.NpcOutfit, new Vector3(0f, 1.2f, 0.005f), new Vector3(0.38f, 0.17f, 0.26f), 0.05f);
                b.AddBox(StylizedColor.NpcAccent, new Vector3(0f, 0.9f, 0f), new Vector3(0.38f, 0.08f, 0.26f));
                break;
            case NpcOutfitStyle.Gothic:
                // 고딕 드레스 : 긴 치마와 주름 · 레이스 깃 · 가슴 리본 · 코르셋 끈
                if (!upperOnly)
                {
                    b.AddFrustum(StylizedColor.NpcOutfit, new Vector3(0f, 0.12f, 0f), 0.33f, 0.21f, 0.82f, 10);
                    b.AddTorus(StylizedColor.NpcAccent, new Vector3(0f, 0.14f, 0f), 0.33f, 0.035f, 10, 4);
                }

                b.AddBeveledBox(StylizedColor.NpcOutfit, new Vector3(0f, 1.1f, 0f), new Vector3(0.4f, 0.52f, 0.26f), 0.08f);
                b.AddTorus(StylizedColor.White, new Vector3(0f, 1.34f, 0f), 0.1f, 0.035f, 8, 4);
                b.AddLowPolySphere(StylizedColor.NpcAccent, new Vector3(0f, 1.29f, 0.135f), new Vector3(0.06f, 0.03f, 0.02f), 0, 0f, 9410);
                b.AddBox(StylizedColor.NpcAccent, new Vector3(0f, 1.0f, 0.132f), new Vector3(0.1f, 0.2f, 0.01f));
                break;
        }
    }

    // ---------------------------------------------------------------- 100일차 종족 부속

    private static void AddNpcSpeciesExtra(LowPolyMeshBuilder b, NpcLook look)
    {
        switch (look.Species)
        {
            case NpcSpecies.Succubus:
                // 뒤로 휜 짧은 뿔 · 끝이 하트 모양인 가는 꼬리
                for (int side = -1; side <= 1; side += 2)
                {
                    AddTube(b, StylizedColor.Black, new[] { new Vector3(side * 0.1f, 1.74f, 0.02f), new Vector3(side * 0.15f, 1.85f, -0.06f), new Vector3(side * 0.18f, 1.84f, -0.18f) }, new[] { 0.035f, 0.025f, 0.006f }, 5, 9500 + side * 3);
                }

                AddTube(b, StylizedColor.Black, new[] { new Vector3(0f, 0.86f, -0.13f), new Vector3(0.05f, 0.56f, -0.4f), new Vector3(0.2f, 0.36f, -0.55f), new Vector3(0.26f, 0.34f, -0.62f) }, new[] { 0.02f, 0.017f, 0.013f, 0.01f }, 5, 9510);
                b.Push(new Vector3(0.29f, 0.33f, -0.66f), Euler(0f, 30f, 0f), Vector3.one);
                AddPanel(b, StylizedColor.NpcOutfit, new Vector3(0f, 0f, 0.06f), new Vector3(0.06f, 0f, -0.02f), new Vector3(0f, 0f, -0.1f), new Vector3(-0.06f, 0f, -0.02f));
                b.Pop();
                break;
            case NpcSpecies.Ghost:
                // 끝이 투명해지는 긴 머리 · 주위를 떠도는 빛 입자
                b.AddBeveledBox(StylizedColor.SpiritMist, new Vector3(0f, 0.96f, -0.16f), new Vector3(0.3f, 0.3f, 0.07f), 0.03f);
                b.AddLowPolySphere(StylizedColor.SpiritMist, new Vector3(0.36f, 1.5f, 0.1f), Vector3.one * 0.03f, 0, 0f, 9520);
                b.AddLowPolySphere(StylizedColor.SpiritMist, new Vector3(-0.34f, 1.7f, -0.05f), Vector3.one * 0.025f, 0, 0f, 9521);
                break;
            case NpcSpecies.Harpy:
                // 팔의 푸른 깃털 · 깃털 귀걸이 · 발톱
                for (int side = -1; side <= 1; side += 2)
                {
                    for (int index = 0; index < 3; index++)
                    {
                        Vector3 along = Vector3.Lerp(new Vector3(side * 0.29f, 1.03f, 0.02f), new Vector3(side * 0.3f, 0.85f, 0.05f), index * 0.45f);
                        b.Push(along + new Vector3(side * 0.05f, 0f, -0.04f), Euler(0f, side * 20f, side * 35f), Vector3.one);
                        b.AddLowPolySphere(StylizedColor.FishBlue, Vector3.zero, new Vector3(0.02f, 0.11f, 0.05f), 0, 0f, 9530 + index);
                        b.Pop();
                    }

                    b.AddLowPolySphere(StylizedColor.White, new Vector3(side * 0.19f, 1.49f, 0.02f), new Vector3(0.02f, 0.05f, 0.02f), 0, 0f, 9535 + side);

                    for (int claw = -1; claw <= 1; claw++)
                    {
                        b.Push(new Vector3(side * 0.09f + claw * 0.035f, 0.02f, 0.14f), Euler(90f, 0f, 0f), Vector3.one);
                        b.AddCone(StylizedColor.Bone, Vector3.zero, 0.015f, 0.06f, 4);
                        b.Pop();
                    }
                }

                break;
            case NpcSpecies.Android:
                // 귀 옆 통신기 · 목 고리 · 팔꿈치와 무릎 관절
                for (int side = -1; side <= 1; side += 2)
                {
                    b.AddBox(StylizedColor.IronDark, new Vector3(side * 0.185f, 1.58f, 0f), new Vector3(0.04f, 0.1f, 0.1f));
                    b.AddLowPolySphere(StylizedColor.Crystal, new Vector3(side * 0.19f, 1.58f, 0.055f), Vector3.one * 0.015f, 0, 0f, 9540 + side);
                    b.AddLowPolySphere(StylizedColor.IronDark, new Vector3(side * 0.29f, 1.03f, 0.02f), Vector3.one * 0.05f, 0, 0f, 9543 + side);
                    b.AddLowPolySphere(StylizedColor.IronDark, new Vector3(side * 0.087f, 0.46f, 0.02f), Vector3.one * 0.06f, 0, 0f, 9546 + side);
                }

                b.AddTorus(StylizedColor.IronDark, new Vector3(0f, 1.38f, 0f), 0.065f, 0.018f, 8, 3);
                break;
            case NpcSpecies.Fairy:
                // 작고 뾰족한 귀
                for (int side = -1; side <= 1; side += 2)
                {
                    b.Push(new Vector3(side * 0.18f, 1.61f, 0f), Euler(0f, 0f, -side * 75f), Vector3.one);
                    b.AddCone(StylizedColor.Skin, Vector3.zero, 0.03f, 0.12f, 4);
                    b.Pop();
                }

                break;
            case NpcSpecies.Wolf:
                AddPointedEars(b, 0.085f, 0.2f, 12f, StylizedColor.White);
                AddTube(b, StylizedColor.NpcHair, new[] { new Vector3(0f, 0.84f, -0.14f), new Vector3(0f, 0.66f, -0.34f), new Vector3(0.04f, 0.44f, -0.44f) }, new[] { 0.06f, 0.11f, 0.07f }, 6, 9550);
                b.AddLowPolySphere(StylizedColor.NpcHair, new Vector3(0f, 0.6f, -0.37f), new Vector3(0.11f, 0.17f, 0.11f), 1, 0.05f, 9553);
                b.AddLowPolySphere(StylizedColor.White, new Vector3(0.05f, 0.4f, -0.45f), new Vector3(0.07f, 0.09f, 0.07f), 0, 0f, 9554);
                break;
            case NpcSpecies.Arachne:
                // 이마의 작은 눈 한 쌍
                for (int side = -1; side <= 1; side += 2)
                {
                    b.AddLowPolySphere(StylizedColor.AppleRed, new Vector3(side * 0.04f, 1.7f, 0.16f), Vector3.one * 0.015f, 0, 0f, 9560 + side);
                }

                break;
            case NpcSpecies.Elf:
                // 길고 뾰족한 귀
                for (int side = -1; side <= 1; side += 2)
                {
                    b.Push(new Vector3(side * 0.18f, 1.6f, 0f), Euler(-12f, 0f, -side * 68f), Vector3.one);
                    b.AddCone(StylizedColor.Skin, Vector3.zero, 0.035f, 0.21f, 4);
                    b.Pop();
                }

                break;
            case NpcSpecies.Vampire:
                // 송곳니 · 붉은 안감 망토
                for (int side = -1; side <= 1; side += 2)
                {
                    b.Push(new Vector3(side * 0.025f, 1.495f, 0.163f), Euler(180f, 0f, 0f), Vector3.one);
                    b.AddCone(StylizedColor.White, Vector3.zero, 0.01f, 0.025f, 3);
                    b.Pop();
                }

                AddBackCape(b, StylizedColor.NpcOutfit, StylizedColor.NpcAccent);
                break;
            case NpcSpecies.Slime:
                // 머리 위 물방울 더듬이
                b.AddLowPolySphere(StylizedColor.SlimeJelly, new Vector3(0.04f, 1.84f, 0.02f), new Vector3(0.05f, 0.07f, 0.05f), 0, 0f, 9570);
                break;
            case NpcSpecies.Dog:
                // 늘어진 귀 · 위로 말린 꼬리
                for (int side = -1; side <= 1; side += 2)
                {
                    b.Push(new Vector3(side * 0.2f, 1.66f, -0.02f), Euler(0f, 0f, side * 18f), Vector3.one);
                    b.AddLowPolySphere(StylizedColor.NpcHair, new Vector3(0f, -0.06f, 0f), new Vector3(0.05f, 0.13f, 0.08f), 0, 0f, 9575 + side);
                    b.Pop();
                }

                AddTube(b, StylizedColor.NpcHair, new[] { new Vector3(0f, 0.84f, -0.14f), new Vector3(0f, 1.0f, -0.36f), new Vector3(0.06f, 1.12f, -0.3f) }, new[] { 0.05f, 0.08f, 0.04f }, 6, 9578);
                break;
            case NpcSpecies.Bee:
                // 더듬이 · 줄무늬 배 · 작은 침
                AddAntennae(b, StylizedColor.Black, StylizedColor.FlowerYellow);
                b.AddLowPolySphere(StylizedColor.FlowerYellow, new Vector3(0f, 0.78f, -0.32f), new Vector3(0.15f, 0.17f, 0.21f), 1, 0f, 9580);

                for (int index = 0; index < 2; index++)
                {
                    b.Push(new Vector3(0f, 0.78f, -0.26f - index * 0.12f), Euler(90f, 0f, 0f), Vector3.one);
                    b.AddTorus(StylizedColor.Black, Vector3.zero, 0.14f - index * 0.03f, 0.025f, 10, 3);
                    b.Pop();
                }

                b.AddLimb(StylizedColor.Black, new Vector3(0f, 0.72f, -0.5f), new Vector3(0f, 0.66f, -0.58f), 0.025f, 0.003f, 4);
                break;
            case NpcSpecies.Fox:
                // 큰 여우 귀 · 끝이 흰 큰 꼬리 · 꼬리 리본
                AddPointedEars(b, 0.095f, 0.2f, 20f, StylizedColor.White);
                b.AddLowPolySphere(StylizedColor.NpcHair, new Vector3(0f, 0.8f, -0.2f), new Vector3(0.1f, 0.1f, 0.14f), 1, 0f, 9590);
                b.AddLowPolySphere(StylizedColor.NpcHair, new Vector3(0.1f, 0.7f, -0.45f), new Vector3(0.16f, 0.17f, 0.2f), 1, 0.04f, 9591);
                b.AddLowPolySphere(StylizedColor.NpcHair, new Vector3(0.22f, 0.85f, -0.62f), new Vector3(0.14f, 0.16f, 0.18f), 1, 0.04f, 9592);
                b.AddLowPolySphere(StylizedColor.White, new Vector3(0.28f, 1.02f, -0.66f), new Vector3(0.09f, 0.11f, 0.1f), 1, 0f, 9593);
                b.AddTorus(StylizedColor.ClothRed, new Vector3(0.05f, 0.76f, -0.3f), 0.08f, 0.02f, 6, 3);
                break;
            case NpcSpecies.Mimic:
                // 열쇠 귀걸이
                for (int side = -1; side <= 1; side += 2)
                {
                    b.AddBox(StylizedColor.Gold, new Vector3(side * 0.19f, 1.47f, 0.02f), new Vector3(0.015f, 0.07f, 0.025f));
                }

                break;
            case NpcSpecies.Tiger:
                // 둥근 호랑이 귀 · 줄무늬 꼬리
                for (int side = -1; side <= 1; side += 2)
                {
                    b.AddLowPolySphere(StylizedColor.FlowerYellow, new Vector3(side * 0.15f, 1.8f, -0.02f), new Vector3(0.07f, 0.07f, 0.03f), 1, 0f, 9600 + side);
                    b.AddLowPolySphere(StylizedColor.Black, new Vector3(side * 0.15f, 1.8f, 0.005f), new Vector3(0.035f, 0.035f, 0.012f), 0, 0f, 9603 + side);
                }

                Vector3[] tail = { new Vector3(0f, 0.84f, -0.14f), new Vector3(0f, 0.6f, -0.4f), new Vector3(0.1f, 0.4f, -0.6f), new Vector3(0.25f, 0.45f, -0.75f), new Vector3(0.3f, 0.64f, -0.8f) };

                for (int index = 0; index < tail.Length - 1; index++)
                {
                    b.AddLimb(index % 2 == 0 ? StylizedColor.FlowerYellow : StylizedColor.Black, tail[index], tail[index + 1], 0.045f - index * 0.004f, 0.041f - index * 0.004f, 6);
                }

                break;
            case NpcSpecies.Witch:
                // 챙 넓은 뾰족 모자 · 까마귀 깃털 망토
                b.AddFrustum(StylizedColor.NpcOutfit, new Vector3(0f, 1.78f, -0.01f), 0.36f, 0.33f, 0.03f, 12);
                b.AddFrustum(StylizedColor.NpcOutfit, new Vector3(0f, 1.8f, -0.01f), 0.19f, 0.11f, 0.24f, 8);
                b.Push(new Vector3(0f, 2.04f, -0.02f), Euler(-24f, 0f, 0f), Vector3.one);
                b.AddCone(StylizedColor.NpcOutfit, Vector3.zero, 0.11f, 0.24f, 8);
                b.Pop();
                b.AddTorus(StylizedColor.NpcAccent, new Vector3(0f, 1.83f, -0.01f), 0.18f, 0.02f, 8, 3);
                b.Push(new Vector3(0.16f, 1.9f, 0.06f), Euler(0f, 0f, -30f), Vector3.one);
                b.AddLowPolySphere(StylizedColor.Black, Vector3.zero, new Vector3(0.02f, 0.1f, 0.03f), 0, 0f, 9610);
                b.Pop();
                AddBackCape(b, StylizedColor.Black, StylizedColor.NpcAccent);

                for (int index = 0; index < 5; index++)
                {
                    b.AddLowPolySphere(StylizedColor.Black, new Vector3(-0.24f + index * 0.12f, 0.62f + Mathf.Abs(index - 2) * 0.04f, -0.3f), new Vector3(0.05f, 0.12f, 0.02f), 0, 0f, 9611 + index);
                }

                break;
            case NpcSpecies.Mermaid:
                // 귀 지느러미
                for (int side = -1; side <= 1; side += 2)
                {
                    b.Push(new Vector3(side * 0.19f, 1.6f, -0.02f), Euler(0f, side * 25f, 0f), Vector3.one);
                    b.AddLowPolySphere(StylizedColor.FishBlue, Vector3.zero, new Vector3(0.015f, 0.08f, 0.07f), 0, 0f, 9620 + side);
                    b.Pop();
                }

                break;
            case NpcSpecies.Dullahan:
                // 목 위 푸른 영혼 불꽃 · 기사 망토
                b.AddLowPolySphere(StylizedColor.SoulFlame, new Vector3(0f, 1.47f, 0f), new Vector3(0.07f, 0.05f, 0.07f), 0, 0f, 9630);

                for (int index = 0; index < 3; index++)
                {
                    float angle = index * 2.1f;
                    b.AddCone(StylizedColor.SoulFlame, new Vector3(Mathf.Cos(angle) * 0.04f, 1.46f, Mathf.Sin(angle) * 0.04f), 0.035f, 0.14f - index * 0.02f, 5);
                }

                AddBackCape(b, StylizedColor.NpcOutfit, StylizedColor.NpcAccent);
                break;
            case NpcSpecies.Oni:
                // 붉은 뿔 한 쌍 · 송곳니
                for (int side = -1; side <= 1; side += 2)
                {
                    b.Push(new Vector3(side * 0.09f, 1.76f, 0.04f), Euler(0f, 0f, -side * 14f), Vector3.one);
                    b.AddCone(StylizedColor.AppleRed, Vector3.zero, 0.04f, 0.18f, 5);
                    b.Pop();
                    b.Push(new Vector3(side * 0.03f, 1.49f, 0.164f), Euler(180f, 0f, 0f), Vector3.one);
                    b.AddCone(StylizedColor.White, Vector3.zero, 0.01f, 0.022f, 3);
                    b.Pop();
                }

                break;
            case NpcSpecies.Goblin:
                // 옆으로 뻗은 큰 귀 · 이마 위 고글
                for (int side = -1; side <= 1; side += 2)
                {
                    b.Push(new Vector3(side * 0.17f, 1.62f, 0f), Euler(0f, 0f, -side * 80f), Vector3.one);
                    b.AddCone(StylizedColor.SkinGoblin, Vector3.zero, 0.06f, 0.26f, 4);
                    b.Pop();
                    b.Push(new Vector3(side * 0.07f, 1.72f, 0.16f), Euler(90f, 0f, 0f), Vector3.one);
                    b.AddTorus(StylizedColor.IronDark, Vector3.zero, 0.045f, 0.012f, 8, 3);
                    b.AddDisc(StylizedColor.Glass, new Vector3(0f, 0.005f, 0f), 0.04f, 8, true);
                    b.Pop();
                }

                b.AddBox(StylizedColor.Leather, new Vector3(0f, 1.72f, 0.15f), new Vector3(0.36f, 0.025f, 0.02f));
                break;
            case NpcSpecies.Butterfly:
                AddAntennae(b, StylizedColor.Black, StylizedColor.NpcAccent);
                break;
            case NpcSpecies.Frog:
                // 물갈퀴 손 · 연잎 모자
                for (int side = -1; side <= 1; side += 2)
                {
                    b.AddLowPolySphere(StylizedColor.Herb, NpcHand(side) + new Vector3(0f, -0.03f, 0.02f), new Vector3(0.07f, 0.02f, 0.06f), 0, 0f, 9640 + side);
                }

                b.AddLowPolySphere(StylizedColor.Leaf, new Vector3(0f, 1.8f, 0f), new Vector3(0.26f, 0.03f, 0.26f), 1, 0.03f, 9643);
                b.AddLowPolySphere(StylizedColor.Flower, new Vector3(0.1f, 1.84f, 0.08f), Vector3.one * 0.04f, 0, 0f, 9644);
                break;
            case NpcSpecies.Shark:
                // 등지느러미 · 굵은 상어 꼬리와 꼬리지느러미 · 뾰족한 이
                b.Push(new Vector3(0f, 1.2f, -0.16f), Euler(-30f, 0f, 0f), Vector3.one);
                b.AddLowPolySphere(StylizedColor.SharkSkin, new Vector3(0f, 0.08f, -0.04f), new Vector3(0.025f, 0.17f, 0.1f), 0, 0f, 9650);
                b.Pop();
                AddTube(b, StylizedColor.SharkSkin, new[] { new Vector3(0f, 0.86f, -0.14f), new Vector3(0f, 0.62f, -0.44f), new Vector3(0f, 0.46f, -0.7f) }, new[] { 0.1f, 0.07f, 0.04f }, 6, 9651);
                b.Push(new Vector3(0f, 0.46f, -0.72f), Euler(0f, 0f, 0f), Vector3.one);
                AddPanel(b, StylizedColor.SharkSkin, new Vector3(0f, 0f, 0f), new Vector3(0f, 0.22f, -0.14f), new Vector3(0f, 0.04f, -0.08f), new Vector3(0f, -0.16f, -0.12f));
                b.Pop();

                for (int index = 0; index < 4; index++)
                {
                    b.Push(new Vector3(-0.03f + index * 0.02f, 1.49f, 0.164f), Euler(180f, 0f, 0f), Vector3.one);
                    b.AddCone(StylizedColor.White, Vector3.zero, 0.008f, 0.02f, 3);
                    b.Pop();
                }

                break;
            case NpcSpecies.Flower:
                // 머리 꽃 3송이 · 어깨를 감는 덩굴과 잎
                AddSmallFlower(b, new Vector3(0.14f, 1.76f, 0.06f), 0.05f, StylizedColor.Flower, 9660);
                AddSmallFlower(b, new Vector3(-0.16f, 1.72f, 0f), 0.045f, StylizedColor.Flower, 9667);
                AddSmallFlower(b, new Vector3(0.02f, 1.82f, -0.1f), 0.04f, StylizedColor.FlowerYellow, 9674);

                for (int side = -1; side <= 1; side += 2)
                {
                    AddTube(b, StylizedColor.Leaf, new[] { new Vector3(side * 0.16f, 1.38f, -0.05f), new Vector3(side * 0.27f, 1.28f, 0.06f), new Vector3(side * 0.3f, 1.1f, -0.04f), new Vector3(side * 0.31f, 0.96f, 0.07f) }, new[] { 0.02f, 0.018f, 0.016f, 0.01f }, 4, 9680 + side * 5);
                    b.AddLowPolySphere(StylizedColor.LeafLight, new Vector3(side * 0.3f, 1.2f, 0.08f), new Vector3(0.05f, 0.015f, 0.03f), 0, 0f, 9690 + side);
                    b.AddLowPolySphere(StylizedColor.LeafLight, new Vector3(side * 0.33f, 1.02f, -0.05f), new Vector3(0.045f, 0.015f, 0.03f), 0, 0f, 9693 + side);
                }

                break;
            case NpcSpecies.Kraken:
                // 가슴 가죽 끈 (하네스)
                for (int side = -1; side <= 1; side += 2)
                {
                    b.Push(new Vector3(side * 0.08f, 1.12f, 0.125f), Euler(0f, 0f, side * 28f), Vector3.one);
                    b.AddBox(StylizedColor.Leather, Vector3.zero, new Vector3(0.035f, 0.46f, 0.02f));
                    b.Pop();
                }

                break;
        }
    }

    // 뾰족한 동물 귀 (늑대 · 여우)
    private static void AddPointedEars(LowPolyMeshBuilder b, float radius, float height, float tilt, StylizedColor inner)
    {
        for (int side = -1; side <= 1; side += 2)
        {
            b.Push(new Vector3(side * 0.12f, 1.76f, -0.01f), Euler(0f, 0f, -side * tilt), Vector3.one);
            b.AddCone(StylizedColor.NpcHair, Vector3.zero, radius, height, 4);
            b.AddCone(inner, new Vector3(0f, 0.01f, 0.035f), radius * 0.5f, height * 0.65f, 4);
            b.Pop();
        }
    }

    // 곤충 더듬이 (벌 · 나비)
    private static void AddAntennae(LowPolyMeshBuilder b, StylizedColor stalk, StylizedColor tip)
    {
        for (int side = -1; side <= 1; side += 2)
        {
            AddTube(b, stalk, new[] { new Vector3(side * 0.06f, 1.74f, 0.08f), new Vector3(side * 0.1f, 1.88f, 0.12f), new Vector3(side * 0.16f, 1.96f, 0.1f) }, new[] { 0.01f, 0.009f, 0.008f }, 4, 9700 + side * 3);
            b.AddLowPolySphere(tip, new Vector3(side * 0.17f, 1.97f, 0.1f), Vector3.one * 0.025f, 0, 0f, 9706 + side);
        }
    }

    // ---------------------------------------------------------------- 날개

    private static void AddNpcWings(LowPolyMeshBuilder b, NpcWings wings)
    {
        if (wings == NpcWings.None)
        {
            return;
        }

        for (int side = -1; side <= 1; side += 2)
        {
            switch (wings)
            {
                case NpcWings.Bird:
                    // 흰 새 날개 : 날개뼈를 따라 깃털을 겹쳐 늘어뜨린다 (끝 깃털은 하늘색)
                    Vector3 root = new Vector3(side * 0.1f, 1.26f, -0.15f);
                    Vector3 elbow = new Vector3(side * 0.5f, 1.56f, -0.36f);
                    Vector3 tip = new Vector3(side * 0.86f, 1.3f, -0.46f);
                    AddTube(b, StylizedColor.White, new[] { root, elbow, tip }, new[] { 0.05f, 0.045f, 0.02f }, 5, 9720 + side * 3);

                    for (int index = 0; index < 6; index++)
                    {
                        float t = index / 5f;
                        Vector3 along = t < 0.5f ? Vector3.Lerp(root, elbow, t * 2f) : Vector3.Lerp(elbow, tip, (t - 0.5f) * 2f);
                        float length = 0.2f + t * 0.14f;
                        b.Push(along + new Vector3(0f, -length * 0.9f, -0.02f), Euler(0f, side * 25f, side * (8f + t * 20f)), Vector3.one);
                        b.AddLowPolySphere(index >= 4 ? StylizedColor.FishBlue : StylizedColor.White, Vector3.zero, new Vector3(0.075f, length, 0.025f), 0, 0f, 9730 + index);
                        b.Pop();
                    }

                    break;
                case NpcWings.Bat:
                    // 박쥐 날개 : 뼈대와 그 사이의 날개막
                    Vector3 batRoot = new Vector3(side * 0.1f, 1.26f, -0.15f);
                    Vector3 batElbow = new Vector3(side * 0.38f, 1.5f, -0.3f);
                    Vector3 batTip = new Vector3(side * 0.64f, 1.28f, -0.36f);
                    Vector3 fingerA = new Vector3(side * 0.56f, 0.98f, -0.33f);
                    Vector3 fingerB = new Vector3(side * 0.34f, 0.94f, -0.26f);
                    AddTube(b, StylizedColor.Black, new[] { batRoot, batElbow, batTip }, new[] { 0.025f, 0.02f, 0.008f }, 4, 9740 + side * 3);
                    b.AddLimb(StylizedColor.Black, batElbow, fingerA, 0.015f, 0.006f, 4);
                    b.AddLimb(StylizedColor.Black, batElbow, fingerB, 0.015f, 0.006f, 4);
                    AddPanel(b, StylizedColor.NpcOutfit, batRoot, batElbow, batTip, fingerA, fingerB, new Vector3(side * 0.14f, 1.05f, -0.18f));
                    break;
                case NpcWings.Fairy:
                    // 투명한 요정 날개 2쌍
                    AddWingLeaf(b, StylizedColor.WingGlass, new Vector3(side * 0.28f, 1.45f, -0.2f), side, 25f, new Vector3(0.3f, 0.2f, 0.012f), 9750);
                    AddWingLeaf(b, StylizedColor.WingGlass, new Vector3(side * 0.22f, 1.12f, -0.19f), side, -20f, new Vector3(0.2f, 0.14f, 0.012f), 9752);
                    break;
                case NpcWings.Butterfly:
                    // 큰 나비 날개 (위 날개 옷 색 · 아래 날개 보조 색 · 흰 점)
                    AddWingLeaf(b, StylizedColor.NpcOutfit, new Vector3(side * 0.4f, 1.55f, -0.22f), side, 22f, new Vector3(0.42f, 0.34f, 0.015f), 9760);
                    AddWingLeaf(b, StylizedColor.NpcAccent, new Vector3(side * 0.3f, 1.08f, -0.2f), side, -24f, new Vector3(0.28f, 0.26f, 0.015f), 9762);
                    b.AddLowPolySphere(StylizedColor.White, new Vector3(side * 0.58f, 1.7f, -0.28f), new Vector3(0.06f, 0.05f, 0.02f), 0, 0f, 9764 + side);
                    b.AddLowPolySphere(StylizedColor.White, new Vector3(side * 0.38f, 1.02f, -0.24f), new Vector3(0.04f, 0.04f, 0.02f), 0, 0f, 9767 + side);
                    break;
                case NpcWings.Bee:
                    // 투명한 벌 날개 2쌍 (가늘고 길게)
                    AddWingLeaf(b, StylizedColor.Glass, new Vector3(side * 0.3f, 1.42f, -0.2f), side, 20f, new Vector3(0.32f, 0.11f, 0.012f), 9770);
                    AddWingLeaf(b, StylizedColor.Glass, new Vector3(side * 0.26f, 1.28f, -0.2f), side, -12f, new Vector3(0.24f, 0.09f, 0.012f), 9772);
                    break;
            }
        }
    }

    // 납작한 타원 날개 한 장 (side 방향으로 뻗고 뒤로 조금 젖혀짐)
    private static void AddWingLeaf(LowPolyMeshBuilder b, StylizedColor color, Vector3 center, int side, float roll, Vector3 radii, int seed)
    {
        b.Push(center, Euler(0f, side * 20f, side * roll), Vector3.one);
        b.AddLowPolySphere(color, Vector3.zero, radii, 1, 0f, seed + (side > 0 ? 1 : 0));
        b.Pop();
    }

    // ---------------------------------------------------------------- 100일차 소지품

    private static void AddNpcPropsExtra(LowPolyMeshBuilder b, NpcLook look)
    {
        Vector3 right = NpcHand(1);

        switch (look.Species)
        {
            case NpcSpecies.Succubus:
                // 붉은 결정 목걸이 (계약의 증표)
                b.AddTorus(StylizedColor.Gold, new Vector3(0f, 1.37f, 0f), 0.075f, 0.008f, 8, 3);
                b.AddLowPolySphere(StylizedColor.AppleRed, new Vector3(0f, 1.3f, 0.15f), new Vector3(0.03f, 0.045f, 0.02f), 0, 0f, 9800);
                break;
            case NpcSpecies.Ghost:
                // 책갈피 브로치
                b.AddBox(StylizedColor.Gold, new Vector3(0.08f, 1.24f, 0.135f), new Vector3(0.04f, 0.08f, 0.01f));
                break;
            case NpcSpecies.Harpy:
                // 허리의 작은 하프(리라)
                b.Push(new Vector3(0.24f, 0.86f, 0.02f), Euler(0f, 90f, 90f), Vector3.one);
                b.AddTorus(StylizedColor.Gold, Vector3.zero, 0.07f, 0.012f, 8, 3);
                b.Pop();
                b.AddBox(StylizedColor.White, new Vector3(0.24f, 0.86f, 0.02f), new Vector3(0.005f, 0.12f, 0.08f));
                break;
            case NpcSpecies.Android:
                // 데이터 코어 펜던트
                b.AddLowPolySphere(StylizedColor.Crystal, new Vector3(0f, 1.3f, 0.14f), new Vector3(0.035f, 0.035f, 0.02f), 0, 0f, 9805);
                break;
            case NpcSpecies.Fairy:
                // 요정 가루 유리병
                b.AddCylinder(StylizedColor.Glass, new Vector3(0.21f, 0.78f, 0.1f), 0.035f, 0.09f, 6);
                b.AddLowPolySphere(StylizedColor.GlowMushroom, new Vector3(0.21f, 0.81f, 0.1f), Vector3.one * 0.022f, 0, 0f, 9806);
                b.AddCylinder(StylizedColor.Leather, new Vector3(0.21f, 0.87f, 0.1f), 0.02f, 0.02f, 5);
                break;
            case NpcSpecies.Wolf:
                // 늑대 가죽 어깨 망토 · 송곳니 펜던트
                b.AddTorus(StylizedColor.NpcHair, new Vector3(0f, 1.36f, 0f), 0.17f, 0.07f, 8, 4);
                b.Push(new Vector3(0f, 1.28f, 0.15f), Euler(180f, 0f, 0f), Vector3.one);
                b.AddCone(StylizedColor.Bone, Vector3.zero, 0.018f, 0.06f, 4);
                b.Pop();
                break;
            case NpcSpecies.Arachne:
                // 거미줄 초커 · 허리의 작살형 단검
                b.AddTorus(StylizedColor.White, new Vector3(0f, 1.37f, 0f), 0.065f, 0.01f, 8, 3);
                b.AddLimb(StylizedColor.Iron, new Vector3(0.21f, 0.9f, 0.1f), new Vector3(0.23f, 0.66f, 0.14f), 0.018f, 0.004f, 4);
                b.AddBox(StylizedColor.Leather, new Vector3(0.21f, 0.92f, 0.1f), new Vector3(0.05f, 0.06f, 0.03f));
                break;
            case NpcSpecies.Elf:
                // 등의 활과 화살통 · 잎사귀 귀걸이
                AddTube(b, StylizedColor.WoodLight, new[] { new Vector3(0.24f, 1.62f, -0.2f), new Vector3(0.02f, 1.28f, -0.25f), new Vector3(-0.18f, 0.9f, -0.2f) }, new[] { 0.018f, 0.022f, 0.018f }, 5, 9810);
                b.AddLimb(StylizedColor.White, new Vector3(0.24f, 1.62f, -0.2f), new Vector3(-0.18f, 0.9f, -0.2f), 0.004f, 0.004f, 3);
                b.AddLimb(StylizedColor.Leather, new Vector3(-0.12f, 1.42f, -0.17f), new Vector3(0.1f, 1.02f, -0.19f), 0.05f, 0.045f, 6);
                b.AddLowPolySphere(StylizedColor.White, new Vector3(-0.13f, 1.46f, -0.17f), new Vector3(0.05f, 0.04f, 0.04f), 0, 0f, 9813);
                b.AddLowPolySphere(StylizedColor.LeafLight, new Vector3(0.2f, 1.48f, 0.02f), new Vector3(0.012f, 0.035f, 0.02f), 0, 0f, 9814);
                break;
            case NpcSpecies.Vampire:
                // 붉은 장미 브로치 · 십자 펜던트
                b.AddLowPolySphere(StylizedColor.AppleRed, new Vector3(-0.1f, 1.28f, 0.14f), Vector3.one * 0.035f, 1, 0.1f, 9815);
                b.AddBox(StylizedColor.Gold, new Vector3(0f, 1.2f, 0.136f), new Vector3(0.015f, 0.07f, 0.01f));
                b.AddBox(StylizedColor.Gold, new Vector3(0f, 1.215f, 0.136f), new Vector3(0.045f, 0.015f, 0.01f));
                break;
            case NpcSpecies.Slime:
                // 핵 결정을 담은 작은 유리병 목걸이
                b.AddTorus(StylizedColor.Rope, new Vector3(0f, 1.37f, 0f), 0.07f, 0.007f, 8, 3);
                b.AddCylinder(StylizedColor.Glass, new Vector3(0f, 1.24f, 0.15f), 0.025f, 0.06f, 6);
                b.AddLowPolySphere(StylizedColor.Crystal, new Vector3(0f, 1.26f, 0.15f), Vector3.one * 0.015f, 0, 0f, 9816);
                break;
            case NpcSpecies.Dog:
                // 붉은 스카프 · 개뼈 펜던트
                b.AddTorus(StylizedColor.ClothRed, new Vector3(0f, 1.37f, 0f), 0.08f, 0.03f, 8, 4);
                b.AddBox(StylizedColor.Bone, new Vector3(0f, 1.29f, 0.14f), new Vector3(0.06f, 0.02f, 0.015f));
                break;
            case NpcSpecies.Bee:
                // 꿀병 파우치 · 벌침 단검
                b.AddCylinder(StylizedColor.Glass, new Vector3(-0.22f, 0.8f, 0.08f), 0.04f, 0.1f, 6);
                b.AddCylinder(StylizedColor.Caramel, new Vector3(-0.22f, 0.8f, 0.08f), 0.034f, 0.07f, 6);
                b.AddLimb(StylizedColor.Black, new Vector3(0.22f, 0.9f, 0.1f), new Vector3(0.24f, 0.68f, 0.13f), 0.02f, 0.003f, 4);
                break;
            case NpcSpecies.Fox:
                // 부적 주머니 · 부적 팔찌
                b.AddBox(StylizedColor.White, new Vector3(-0.2f, 0.88f, 0.12f), new Vector3(0.06f, 0.09f, 0.02f));
                b.AddTorus(StylizedColor.ClothRed, new Vector3(0.3f, 0.88f, 0.045f), 0.05f, 0.012f, 6, 3);
                break;
            case NpcSpecies.Tiger:
                // 붉은 손띠 · 호랑이 이빨 펜던트
                for (int side = -1; side <= 1; side += 2)
                {
                    b.AddTorus(StylizedColor.ClothRed, new Vector3(side * 0.3f, 0.88f, 0.045f), 0.05f, 0.014f, 6, 3);
                }

                b.Push(new Vector3(0f, 1.28f, 0.15f), Euler(180f, 0f, 0f), Vector3.one);
                b.AddCone(StylizedColor.Bone, Vector3.zero, 0.016f, 0.05f, 4);
                b.Pop();
                break;
            case NpcSpecies.Witch:
                // 수정 구슬 지팡이 (오른손)
                b.AddLimb(StylizedColor.WoodDark, new Vector3(right.x + 0.03f, 0.02f, 0.1f), new Vector3(right.x + 0.03f, 1.62f, 0.1f), 0.02f, 0.018f, 5);
                b.AddLowPolySphere(StylizedColor.Crystal, new Vector3(right.x + 0.03f, 1.7f, 0.1f), Vector3.one * 0.07f, 1, 0f, 9820);
                b.AddLowPolySphere(StylizedColor.RuneGlow, new Vector3(right.x + 0.03f, 1.7f, 0.1f), Vector3.one * 0.03f, 0, 0f, 9821);
                break;
            case NpcSpecies.Lamia:
                // 금 팔찌 · 루비 목걸이
                for (int side = -1; side <= 1; side += 2)
                {
                    b.AddTorus(StylizedColor.Gold, new Vector3(side * 0.3f, 0.9f, 0.04f), 0.05f, 0.012f, 6, 3);
                }

                b.AddLowPolySphere(StylizedColor.AppleRed, new Vector3(0f, 1.3f, 0.14f), Vector3.one * 0.03f, 0, 0f, 9825);
                break;
            case NpcSpecies.Mermaid:
                // 진주 목걸이 · 산호 빗
                for (int index = 0; index < 7; index++)
                {
                    float angle = (index - 3) * 0.28f;
                    b.AddLowPolySphere(StylizedColor.White, new Vector3(Mathf.Sin(angle) * 0.1f, 1.33f - Mathf.Cos(angle) * 0.02f, Mathf.Cos(angle) * 0.1f + 0.03f), Vector3.one * 0.018f, 0, 0f, 9830 + index);
                }

                b.AddBox(StylizedColor.Coral, new Vector3(0.17f, 1.7f, -0.04f), new Vector3(0.03f, 0.09f, 0.1f));
                break;
            case NpcSpecies.Centaur:
                // 가슴 가죽 끈 · 나침반
                b.Push(new Vector3(0f, 1.14f, 0.13f), Euler(0f, 0f, 35f), Vector3.one);
                b.AddBox(StylizedColor.Leather, Vector3.zero, new Vector3(0.04f, 0.56f, 0.02f));
                b.Pop();
                b.Push(new Vector3(-0.06f, 1.2f, 0.15f), Euler(90f, 0f, 0f), Vector3.one);
                b.AddCylinder(StylizedColor.Gold, Vector3.zero, 0.035f, 0.015f, 8);
                b.Pop();
                break;
            case NpcSpecies.Dullahan:
                // 은 목걸이 (머리 고정)
                b.AddTorus(StylizedColor.Iron, new Vector3(0f, 1.38f, 0f), 0.07f, 0.012f, 8, 3);
                break;
            case NpcSpecies.Oni:
                // 방울 달린 곤봉 (오른손) · 머리 옆 오니 가면
                b.AddLimb(StylizedColor.WoodDark, right + new Vector3(0f, 0.08f, 0.02f), right + new Vector3(0.04f, -0.5f, 0.2f), 0.025f, 0.06f, 6);

                for (int index = 0; index < 3; index++)
                {
                    b.AddLowPolySphere(StylizedColor.Iron, right + new Vector3(0.03f + (index % 2) * 0.03f, -0.28f - index * 0.08f, 0.13f + index * 0.03f), Vector3.one * 0.018f, 0, 0f, 9840 + index);
                }

                b.AddLowPolySphere(StylizedColor.Gold, right + new Vector3(-0.03f, 0.02f, 0.03f), Vector3.one * 0.025f, 0, 0f, 9843);
                b.Push(new Vector3(0.18f, 1.7f, 0.06f), Euler(0f, 70f, 0f), Vector3.one);
                b.AddBeveledBox(StylizedColor.White, Vector3.zero, new Vector3(0.12f, 0.14f, 0.03f), 0.02f);
                b.AddBox(StylizedColor.AppleRed, new Vector3(0f, 0.02f, 0.016f), new Vector3(0.09f, 0.015f, 0.005f));
                b.Pop();
                break;
            case NpcSpecies.Goblin:
                // 다목적 렌치 (오른손)
                b.AddLimb(StylizedColor.Iron, right + new Vector3(0f, 0.04f, 0.02f), right + new Vector3(0f, -0.26f, 0.12f), 0.018f, 0.018f, 5);
                b.AddTorus(StylizedColor.Iron, right + new Vector3(0f, -0.29f, 0.13f), 0.04f, 0.014f, 6, 3);
                break;
            case NpcSpecies.Scorpion:
                // 사막 부적 · 독침 단검
                b.AddLowPolySphere(StylizedColor.Gold, new Vector3(0f, 1.3f, 0.14f), new Vector3(0.035f, 0.035f, 0.015f), 0, 0f, 9850);
                b.AddLimb(StylizedColor.Black, new Vector3(-0.22f, 0.9f, 0.1f), new Vector3(-0.24f, 0.66f, 0.14f), 0.02f, 0.003f, 4);
                break;
            case NpcSpecies.Butterfly:
                // 꽃가루 병
                b.AddCylinder(StylizedColor.Glass, new Vector3(0.21f, 0.78f, 0.1f), 0.035f, 0.09f, 6);
                b.AddLowPolySphere(StylizedColor.FlowerYellow, new Vector3(0.21f, 0.81f, 0.1f), Vector3.one * 0.022f, 0, 0f, 9851);
                break;
            case NpcSpecies.Frog:
                // 기압계
                b.Push(new Vector3(-0.21f, 0.86f, 0.11f), Euler(90f, 0f, 0f), Vector3.one);
                b.AddCylinder(StylizedColor.Gold, Vector3.zero, 0.045f, 0.02f, 8);
                b.AddDisc(StylizedColor.White, new Vector3(0f, 0.021f, 0f), 0.035f, 8);
                b.Pop();
                break;
            case NpcSpecies.Shark:
                // 상어 이빨 목걸이 · 구조용 밧줄
                b.Push(new Vector3(0f, 1.28f, 0.15f), Euler(180f, 0f, 0f), Vector3.one);
                b.AddCone(StylizedColor.White, Vector3.zero, 0.02f, 0.06f, 3);
                b.Pop();
                b.Push(new Vector3(-0.23f, 0.84f, 0.02f), Euler(0f, 0f, 90f), Vector3.one);
                b.AddTorus(StylizedColor.Rope, Vector3.zero, 0.08f, 0.022f, 8, 4);
                b.Pop();
                break;
            case NpcSpecies.Flower:
                // 씨앗 목걸이 · 정원 가위
                b.AddLowPolySphere(StylizedColor.Grain, new Vector3(0f, 1.3f, 0.14f), new Vector3(0.025f, 0.035f, 0.02f), 0, 0f, 9855);
                b.AddLimb(StylizedColor.Iron, new Vector3(0.22f, 0.88f, 0.1f), new Vector3(0.23f, 0.74f, 0.12f), 0.015f, 0.004f, 4);
                b.AddTorus(StylizedColor.ClothGreen, new Vector3(0.22f, 0.9f, 0.1f), 0.025f, 0.008f, 6, 3);
                break;
            case NpcSpecies.Kraken:
                // 잉크병 펜던트 · 심해 등불 (오른손)
                b.AddCylinder(StylizedColor.Glass, new Vector3(0f, 1.22f, 0.15f), 0.025f, 0.06f, 6);
                b.AddCylinder(StylizedColor.Black, new Vector3(0f, 1.22f, 0.15f), 0.02f, 0.04f, 6);
                b.AddLimb(StylizedColor.IronDark, right, right + new Vector3(0f, -0.14f, 0.04f), 0.008f, 0.008f, 4);
                b.AddLowPolySphere(StylizedColor.LampGlow, right + new Vector3(0f, -0.2f, 0.05f), Vector3.one * 0.055f, 0, 0f, 9856);
                b.AddCylinder(StylizedColor.IronDark, right + new Vector3(0f, -0.15f, 0.05f), 0.035f, 0.02f, 6);
                break;
        }
    }
}
