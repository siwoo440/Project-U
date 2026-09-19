using UnityEngine;

// 102일차: 2차 NPC 가게 · 제작 물건 (물약 · 거미 실 · 젤리 · 유부초밥 · 해독제 · 고철 부품 · 사막 연고)
// 모두 손바닥 크기 소품이다. 아이콘 · 바닥용 Prefab · 손에 든 외형이 이 모델을 쓴다.
public static partial class StylizedModelLibrary
{
    private static void RegisterNpcGoods()
    {
        Register("item_vitality_potion", FitMode.UniformLargest, BuildVitalityPotion);
        Register("item_spider_silk", FitMode.UniformLargest, BuildSpiderSilk);
        Register("item_sweet_jelly", FitMode.UniformLargest, BuildSweetJelly);
        Register("item_inari_sushi", FitMode.UniformLargest, BuildInariSushi);
        Register("item_antidote", FitMode.UniformLargest, BuildAntidote);
        Register("item_scrap_parts", FitMode.UniformLargest, BuildScrapParts);
        Register("item_desert_salve", FitMode.UniformLargest, BuildDesertSalve);
    }

    // 벨라모르타 : 둥근 플라스크 (붉은 물약 · 금 고리 · 코르크)
    private static void BuildVitalityPotion(LowPolyMeshBuilder b)
    {
        b.AddLowPolySphere(StylizedColor.ClothRed, new Vector3(0f, 0.13f, 0f), new Vector3(0.13f, 0.13f, 0.13f), 1, 0f, 10201);
        b.AddLowPolySphere(StylizedColor.Flower, new Vector3(-0.05f, 0.18f, 0.08f), new Vector3(0.03f, 0.04f, 0.02f), 0, 0f, 10202);
        b.AddFrustum(StylizedColor.Glass, new Vector3(0f, 0.24f, 0f), 0.05f, 0.04f, 0.1f, 10);
        b.AddTorus(StylizedColor.Gold, new Vector3(0f, 0.26f, 0f), 0.05f, 0.01f, 10, 3);
        b.AddFrustum(StylizedColor.WoodLight, new Vector3(0f, 0.33f, 0f), 0.038f, 0.045f, 0.05f, 8);
        b.AddBox(StylizedColor.ClothCream, new Vector3(0f, 0.12f, 0.128f), new Vector3(0.08f, 0.06f, 0.006f));
        b.AddLowPolySphere(StylizedColor.BerryPurple, new Vector3(0f, 0.12f, 0.133f), new Vector3(0.02f, 0.02f, 0.004f), 0, 0f, 10203);
    }

    // 아라크네 : 실타래 (나무 심 · 흰 실 · 풀린 실)
    private static void BuildSpiderSilk(LowPolyMeshBuilder b)
    {
        b.AddFrustum(StylizedColor.WoodDark, Vector3.zero, 0.1f, 0.1f, 0.03f, 10);
        b.AddFrustum(StylizedColor.WoodDark, new Vector3(0f, 0.24f, 0f), 0.1f, 0.1f, 0.03f, 10);
        b.AddFrustum(StylizedColor.WoodLight, new Vector3(0f, 0.03f, 0f), 0.04f, 0.04f, 0.21f, 8, false, false);

        for (int layer = 0; layer < 5; layer++)
        {
            b.AddTorus(StylizedColor.White, new Vector3(0f, 0.05f + layer * 0.04f, 0f), 0.06f, 0.025f, 10, 4);
        }

        b.AddLimb(StylizedColor.White, new Vector3(0.08f, 0.14f, 0.02f), new Vector3(0.2f, 0.01f, 0.08f), 0.006f, 0.005f, 4);
        b.AddLimb(StylizedColor.White, new Vector3(0.2f, 0.01f, 0.08f), new Vector3(0.28f, 0.005f, -0.02f), 0.005f, 0.004f, 4);
    }

    // 미루 : 달콤한 젤리 (접시 위 말랑한 젤리 · 딸기)
    private static void BuildSweetJelly(LowPolyMeshBuilder b)
    {
        AddPlate(b, 0.26f, StylizedColor.White);
        b.AddFrustum(StylizedColor.SlimeJelly, new Vector3(0f, 0.035f, 0f), 0.15f, 0.12f, 0.12f, 10);
        b.AddLowPolySphere(StylizedColor.SlimeJelly, new Vector3(0f, 0.155f, 0f), new Vector3(0.12f, 0.05f, 0.12f), 1, 0f, 10211);
        b.AddLowPolySphere(StylizedColor.Strawberry, new Vector3(0f, 0.22f, 0f), new Vector3(0.04f, 0.045f, 0.04f), 1, 0.05f, 10212);
        b.AddLowPolySphere(StylizedColor.Leaf, new Vector3(0f, 0.262f, 0f), new Vector3(0.025f, 0.008f, 0.025f), 0, 0f, 10213);
        b.AddLowPolySphere(StylizedColor.White, new Vector3(0.06f, 0.14f, 0.09f), new Vector3(0.02f, 0.03f, 0.01f), 0, 0f, 10214);
    }

    // 카스미 : 유부초밥 3개 (나무 쟁반)
    private static void BuildInariSushi(LowPolyMeshBuilder b)
    {
        b.AddBeveledBox(StylizedColor.WoodDark, new Vector3(0f, 0.015f, 0f), new Vector3(0.42f, 0.03f, 0.2f), 0.01f);

        for (int index = 0; index < 3; index++)
        {
            float x = (index - 1) * 0.13f;
            b.Push(new Vector3(x, 0.03f, 0f), Euler(0f, index * 7f - 7f, 0f), Vector3.one);
            b.AddLowPolySphere(StylizedColor.Caramel, new Vector3(0f, 0.045f, 0f), new Vector3(0.055f, 0.05f, 0.075f), 1, 0.06f, 10221 + index * 3);
            b.AddLowPolySphere(StylizedColor.White, new Vector3(0f, 0.09f, 0.035f), new Vector3(0.045f, 0.02f, 0.035f), 1, 0.1f, 10222 + index * 3);
            b.Pop();
        }

        b.AddLowPolySphere(StylizedColor.Herb, new Vector3(0.17f, 0.04f, 0.07f), new Vector3(0.03f, 0.008f, 0.015f), 0, 0f, 10231);
    }

    // 세이라 : 가는 해독제 병 (초록 약 · 뱀 문양 꼬리표)
    private static void BuildAntidote(LowPolyMeshBuilder b)
    {
        b.AddFrustum(StylizedColor.Herb, Vector3.zero, 0.06f, 0.06f, 0.22f, 8);
        b.AddFrustum(StylizedColor.Glass, new Vector3(0f, 0.22f, 0f), 0.06f, 0.03f, 0.05f, 8, false, false);
        b.AddFrustum(StylizedColor.Glass, new Vector3(0f, 0.27f, 0f), 0.03f, 0.03f, 0.05f, 8, false, false);
        b.AddFrustum(StylizedColor.WoodLight, new Vector3(0f, 0.31f, 0f), 0.028f, 0.034f, 0.04f, 6);
        b.AddBox(StylizedColor.SeedPaper, new Vector3(0f, 0.11f, 0.061f), new Vector3(0.07f, 0.09f, 0.004f));
        b.AddLimb(StylizedColor.SnakeScale, new Vector3(-0.02f, 0.08f, 0.064f), new Vector3(0.02f, 0.14f, 0.064f), 0.008f, 0.005f, 4);
        b.AddLimb(StylizedColor.Rope, new Vector3(0.03f, 0.28f, 0f), new Vector3(0.06f, 0.2f, 0.03f), 0.004f, 0.004f, 3);
    }

    // 피피 : 고철 부품 (톱니바퀴 · 볼트 · 구리판)
    private static void BuildScrapParts(LowPolyMeshBuilder b)
    {
        b.Push(new Vector3(-0.04f, 0.02f, 0f), Euler(0f, 0f, 0f), Vector3.one);
        b.AddFrustum(StylizedColor.Iron, Vector3.zero, 0.12f, 0.12f, 0.035f, 12);

        for (int tooth = 0; tooth < 8; tooth++)
        {
            float angle = tooth * 45f * Mathf.Deg2Rad;
            b.AddBox(StylizedColor.Iron, new Vector3(Mathf.Cos(angle) * 0.135f, 0.0175f, Mathf.Sin(angle) * 0.135f), new Vector3(0.04f, 0.035f, 0.04f));
        }

        b.AddFrustum(StylizedColor.IronDark, new Vector3(0f, 0.035f, 0f), 0.035f, 0.035f, 0.01f, 8);
        b.Pop();
        b.Push(new Vector3(0.14f, 0.03f, 0.08f), Euler(0f, 30f, 90f), Vector3.one);
        b.AddFrustum(StylizedColor.IronDark, Vector3.zero, 0.018f, 0.018f, 0.14f, 6);
        b.AddFrustum(StylizedColor.IronDark, Vector3.zero, 0.035f, 0.035f, 0.025f, 6);
        b.Pop();
        b.Push(new Vector3(0.1f, 0.05f, -0.1f), Euler(12f, -20f, 8f), Vector3.one);
        b.AddBox(StylizedColor.Copper, Vector3.zero, new Vector3(0.14f, 0.015f, 0.09f));
        b.Pop();
    }

    // 사피라 : 사막 연고 (구리 통 · 모래색 뚜껑 · 노란 연고)
    private static void BuildDesertSalve(LowPolyMeshBuilder b)
    {
        b.AddFrustum(StylizedColor.Copper, Vector3.zero, 0.13f, 0.13f, 0.08f, 12, true, false);
        b.AddDisc(StylizedColor.Butter, new Vector3(0f, 0.07f, 0f), 0.12f, 12);
        b.AddLowPolySphere(StylizedColor.Butter, new Vector3(0.02f, 0.075f, 0f), new Vector3(0.05f, 0.02f, 0.05f), 1, 0f, 10241);
        b.Push(new Vector3(0.17f, 0.03f, 0.1f), Euler(-60f, 0f, 20f), Vector3.one);
        b.AddFrustum(StylizedColor.Sandstone, Vector3.zero, 0.135f, 0.135f, 0.03f, 12);
        b.AddLowPolySphere(StylizedColor.ScorpionShell, new Vector3(0f, 0.035f, 0f), new Vector3(0.05f, 0.01f, 0.03f), 0, 0f, 10242);
        b.Pop();
    }
}
