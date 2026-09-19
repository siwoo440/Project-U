using UnityEngine;

// 103일차: 3차 NPC 집 (새 구역) — 마리엘의 산호 동굴 · 루미나의 고치 집 · 알리우네의 온실
// 미터 단위, 입구는 +Z 쪽이다. 충돌 상자는 WorldZoneBuilder가 따로 붙인다.
public static partial class StylizedModelLibrary
{
    private static void RegisterZoneHomes()
    {
        Register("zone_coral_grotto", FitMode.UniformHeight, BuildCoralGrotto);
        Register("zone_cocoon_house", FitMode.UniformHeight, BuildCocoonHouse);
        Register("zone_flower_greenhouse", FitMode.UniformHeight, BuildFlowerGreenhouse);
        RegisterZoneHomesWave4(); // 110일차: 4차 NPC 집
    }

    // 마리엘 : 바닷물이 드나드는 바위 동굴 (둥근 바위 지붕 · 안쪽 물웅덩이 · 산호 · 조개 발 · 진주 등불)
    private static void BuildCoralGrotto(LowPolyMeshBuilder b)
    {
        // 뒤쪽 · 양옆 바위 벽 (앞쪽 +Z는 입구)
        b.AddLowPolySphere(StylizedColor.StoneDark, new Vector3(0f, 1.05f, -0.9f), new Vector3(1.9f, 1.25f, 0.8f), 1, 0.12f, 10301);
        b.AddLowPolySphere(StylizedColor.Stone, new Vector3(-1.45f, 0.8f, -0.1f), new Vector3(0.6f, 0.95f, 1.05f), 1, 0.14f, 10302);
        b.AddLowPolySphere(StylizedColor.Stone, new Vector3(1.45f, 0.75f, -0.05f), new Vector3(0.62f, 0.9f, 1.0f), 1, 0.14f, 10303);
        // 지붕 바위 (입구 위로 살짝 내민 아치)
        b.AddLowPolySphere(StylizedColor.StoneDark, new Vector3(0f, 1.95f, -0.2f), new Vector3(1.7f, 0.5f, 1.1f), 1, 0.1f, 10304);
        b.AddLowPolySphere(StylizedColor.Stone, new Vector3(-0.9f, 2.15f, 0.35f), new Vector3(0.7f, 0.35f, 0.5f), 1, 0.12f, 10305);
        b.AddLowPolySphere(StylizedColor.Stone, new Vector3(0.95f, 2.1f, 0.3f), new Vector3(0.65f, 0.32f, 0.5f), 1, 0.12f, 10306);

        // 안쪽 바닷물 웅덩이 · 모래 턱
        b.AddDisc(StylizedColor.Sand, new Vector3(0f, 0.02f, 0.35f), 1.35f, 12);
        b.AddDisc(StylizedColor.Water, new Vector3(0f, 0.05f, -0.15f), 0.85f, 12);

        // 산호 (분홍 · 주황 가지)
        Vector3[] corals = { new Vector3(-1.05f, 0f, 0.55f), new Vector3(1.1f, 0f, 0.6f), new Vector3(-0.7f, 0f, -0.55f), new Vector3(0.75f, 0f, -0.5f) };

        for (int index = 0; index < corals.Length; index++)
        {
            Vector3 root = corals[index];
            b.AddLimb(StylizedColor.Coral, root, root + new Vector3(0f, 0.55f, 0f), 0.06f, 0.035f, 5);
            b.AddLimb(StylizedColor.Coral, root + new Vector3(0f, 0.25f, 0f), root + new Vector3(0.2f, 0.5f, 0.05f), 0.04f, 0.025f, 5);
            b.AddLimb(StylizedColor.Coral, root + new Vector3(0f, 0.32f, 0f), root + new Vector3(-0.18f, 0.6f, -0.04f), 0.035f, 0.02f, 5);
            b.AddLowPolySphere(StylizedColor.Coral, root + new Vector3(0f, 0.58f, 0f), Vector3.one * 0.06f, 0, 0f, 10310 + index);
        }

        // 입구 조개 발 (줄에 매단 조개 · 진주)
        for (int strand = 0; strand < 5; strand++)
        {
            float x = -0.8f + strand * 0.4f;
            b.AddLimb(StylizedColor.Rope, new Vector3(x, 1.75f, 0.85f), new Vector3(x, 1.2f, 0.85f), 0.01f, 0.01f, 3);
            b.AddLowPolySphere(strand % 2 == 0 ? StylizedColor.White : StylizedColor.Flower, new Vector3(x, 1.18f, 0.85f), new Vector3(0.07f, 0.05f, 0.03f), 0, 0f, 10320 + strand);
        }

        // 진주 등불 · 조개껍데기
        b.AddLimb(StylizedColor.WoodDark, new Vector3(1.35f, 0f, 0.95f), new Vector3(1.35f, 1.1f, 0.95f), 0.04f, 0.035f, 5);
        b.AddLowPolySphere(StylizedColor.LampGlow, new Vector3(1.35f, 1.2f, 0.95f), Vector3.one * 0.13f, 1, 0f, 10330);
        b.AddLowPolySphere(StylizedColor.White, new Vector3(-0.4f, 0.05f, 0.95f), new Vector3(0.12f, 0.04f, 0.1f), 0, 0.1f, 10331);
        b.AddLowPolySphere(StylizedColor.Flower, new Vector3(0.35f, 0.05f, 1.1f), new Vector3(0.1f, 0.035f, 0.08f), 0, 0.1f, 10332);
    }

    // 루미나 : 굽은 나뭇가지에 매달린 커다란 고치 (둥근 문 · 날개 모양 차양 · 줄사다리 · 꽃)
    private static void BuildCocoonHouse(LowPolyMeshBuilder b)
    {
        // 굽은 나무 (뒤쪽에서 올라와 앞으로 휘어짐)
        AddTube(b, StylizedColor.Bark, new[] { new Vector3(0f, 0f, -1.3f), new Vector3(0f, 1.6f, -1.25f), new Vector3(0f, 3.1f, -0.7f), new Vector3(0f, 3.5f, 0.1f) }, new[] { 0.28f, 0.24f, 0.18f, 0.12f }, 7, 10340);
        b.AddLowPolySphere(StylizedColor.Leaf, new Vector3(0f, 3.75f, -0.6f), new Vector3(1.1f, 0.55f, 0.9f), 1, 0.15f, 10341);
        b.AddLowPolySphere(StylizedColor.LeafLight, new Vector3(0.6f, 3.55f, 0.05f), new Vector3(0.55f, 0.35f, 0.5f), 1, 0.15f, 10342);

        // 고치 (가지에 매달림)
        b.AddLimb(StylizedColor.White, new Vector3(0f, 3.45f, 0.1f), new Vector3(0f, 2.75f, 0.1f), 0.05f, 0.08f, 5);
        b.AddLowPolySphere(StylizedColor.ClothCream, new Vector3(0f, 1.75f, 0.1f), new Vector3(0.95f, 1.15f, 0.95f), 2, 0.05f, 10343);

        for (int ring = 0; ring < 4; ring++)
        {
            float y = 1.05f + ring * 0.42f;
            float radius = Mathf.Lerp(0.8f, 0.92f, Mathf.Sin((ring + 0.5f) / 4f * Mathf.PI));
            b.AddTorus(StylizedColor.White, new Vector3(0f, y, 0.1f), radius, 0.035f, 12, 3);
        }

        // 둥근 문 · 문틀 · 창
        b.AddDisc(StylizedColor.LampGlow, new Vector3(0f, 1.5f, 1.09f), 0.36f, 10, true);
        b.Push(new Vector3(0f, 1.5f, 1.08f), Euler(90f, 0f, 0f), Vector3.one);
        b.AddTorus(StylizedColor.WoodDark, Vector3.zero, 0.38f, 0.05f, 10, 3);
        b.Pop();
        b.AddLowPolySphere(StylizedColor.LampGlow, new Vector3(0.55f, 2.2f, 0.78f), Vector3.one * 0.1f, 0, 0f, 10344);

        // 날개 모양 차양 (반투명 날개 두 장)
        for (int side = -1; side <= 1; side += 2)
        {
            b.Push(new Vector3(side * 0.55f, 2.35f, 0.75f), Euler(15f, side * 30f, side * 25f), Vector3.one);
            b.AddLowPolySphere(StylizedColor.WingGlass, Vector3.zero, new Vector3(0.55f, 0.03f, 0.35f), 1, 0f, 10345 + side);
            b.Pop();
        }

        // 줄사다리 · 발판
        for (int rail = -1; rail <= 1; rail += 2)
        {
            b.AddLimb(StylizedColor.Rope, new Vector3(rail * 0.22f, 1.05f, 0.95f), new Vector3(rail * 0.22f, 0f, 1.35f), 0.02f, 0.02f, 3);
        }

        for (int step = 0; step < 3; step++)
        {
            float t = (step + 1) / 4f;
            b.AddBox(StylizedColor.WoodLight, new Vector3(0f, Mathf.Lerp(1.05f, 0f, t), Mathf.Lerp(0.95f, 1.35f, t)), new Vector3(0.48f, 0.04f, 0.1f));
        }

        b.AddDisc(StylizedColor.WoodPlank, new Vector3(0f, 0.03f, 1.45f), 0.45f, 8);

        // 꽃 (분홍 · 노랑)
        Vector3[] flowers = { new Vector3(-0.9f, 0f, 0.9f), new Vector3(0.95f, 0f, 1.0f), new Vector3(-0.6f, 0f, 1.5f), new Vector3(0.7f, 0f, 1.6f) };

        for (int index = 0; index < flowers.Length; index++)
        {
            Vector3 root = flowers[index];
            b.AddLimb(StylizedColor.Leaf, root, root + new Vector3(0f, 0.35f, 0f), 0.015f, 0.012f, 4);
            b.AddLowPolySphere(index % 2 == 0 ? StylizedColor.Flower : StylizedColor.FlowerYellow, root + new Vector3(0f, 0.38f, 0f), new Vector3(0.1f, 0.05f, 0.1f), 0, 0f, 10350 + index);
        }
    }

    // 알리우네 : 육각 유리 온실 (돌 기단 · 나무 기둥 · 유리 벽 · 둥근 유리 지붕 · 덩굴 · 화분)
    private static void BuildFlowerGreenhouse(LowPolyMeshBuilder b)
    {
        const float radius = 1.9f;
        b.AddFrustum(StylizedColor.StoneLight, Vector3.zero, radius + 0.15f, radius + 0.1f, 0.25f, 6, true, true, 30f);
        b.AddFrustum(StylizedColor.Glass, new Vector3(0f, 0.25f, 0f), radius, radius, 1.9f, 6, false, false, 30f);
        b.AddFrustum(StylizedColor.Glass, new Vector3(0f, 2.15f, 0f), radius, 0.35f, 1.05f, 6, false, true, 30f);

        // 기둥 · 지붕 살
        for (int corner = 0; corner < 6; corner++)
        {
            float angle = (corner * 60f + 30f) * Mathf.Deg2Rad;
            Vector3 foot = new Vector3(Mathf.Sin(angle) * radius, 0.25f, Mathf.Cos(angle) * radius);
            b.AddLimb(StylizedColor.WoodLight, foot, foot + new Vector3(0f, 1.9f, 0f), 0.06f, 0.06f, 5);
            b.AddLimb(StylizedColor.WoodLight, foot + new Vector3(0f, 1.9f, 0f), new Vector3(0f, 3.2f, 0f), 0.05f, 0.04f, 5);
        }

        b.AddTorus(StylizedColor.WoodLight, new Vector3(0f, 2.15f, 0f), radius, 0.05f, 6, 3);
        b.AddLowPolySphere(StylizedColor.FlowerYellow, new Vector3(0f, 3.28f, 0f), Vector3.one * 0.12f, 0, 0f, 10360);

        // 문 (앞쪽 +Z, 나무 문틀 · 열린 유리문)
        b.AddBox(StylizedColor.WoodDark, new Vector3(-0.45f, 1.1f, radius * 0.88f), new Vector3(0.08f, 1.7f, 0.08f));
        b.AddBox(StylizedColor.WoodDark, new Vector3(0.45f, 1.1f, radius * 0.88f), new Vector3(0.08f, 1.7f, 0.08f));
        b.AddBox(StylizedColor.WoodDark, new Vector3(0f, 1.95f, radius * 0.88f), new Vector3(0.98f, 0.08f, 0.08f));
        b.AddBox(StylizedColor.StoneLight, new Vector3(0f, 0.08f, radius + 0.35f), new Vector3(0.9f, 0.16f, 0.5f));

        // 안쪽 화분 · 꽃 (유리 너머로 보임)
        Vector3[] pots = { new Vector3(-0.9f, 0.25f, -0.6f), new Vector3(0.9f, 0.25f, -0.5f), new Vector3(0f, 0.25f, -1.2f), new Vector3(-1.1f, 0.25f, 0.5f), new Vector3(1.1f, 0.25f, 0.55f) };

        for (int index = 0; index < pots.Length; index++)
        {
            Vector3 pot = pots[index];
            b.AddFrustum(StylizedColor.Dirt, pot, 0.18f, 0.24f, 0.3f, 7);
            b.AddLowPolySphere(StylizedColor.Leaf, pot + new Vector3(0f, 0.5f, 0f), new Vector3(0.28f, 0.26f, 0.28f), 1, 0.2f, 10361 + index);
            b.AddLowPolySphere(index % 2 == 0 ? StylizedColor.Flower : StylizedColor.FlowerYellow, pot + new Vector3(0.08f, 0.72f, 0.05f), Vector3.one * 0.09f, 0, 0f, 10370 + index);
        }

        // 바깥 덩굴 (기둥을 감고 올라감)
        for (int vine = 0; vine < 3; vine++)
        {
            float angle = (vine * 120f + 30f) * Mathf.Deg2Rad;
            Vector3 foot = new Vector3(Mathf.Sin(angle) * (radius + 0.05f), 0.25f, Mathf.Cos(angle) * (radius + 0.05f));
            b.AddLimb(StylizedColor.LeafDark, foot, foot + new Vector3(0.1f, 1.2f, 0.05f), 0.035f, 0.025f, 4);

            for (int leaf = 0; leaf < 3; leaf++)
            {
                b.AddLowPolySphere(StylizedColor.Leaf, foot + new Vector3(0.08f, 0.35f + leaf * 0.35f, 0.04f), new Vector3(0.12f, 0.05f, 0.08f), 0, 0f, 10380 + vine * 3 + leaf);
            }
        }
    }
}
