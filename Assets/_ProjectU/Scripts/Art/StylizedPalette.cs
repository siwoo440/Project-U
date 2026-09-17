using UnityEngine;

// 78일차: 저폴리 임시 모델에서 사용하는 공통 색상 목록
// 색상마다 Material 하나를 만들어 모든 모델이 공유한다 (SRP Batcher 친화적).
public enum StylizedColor
{
    BarkDark = 0,
    Bark = 1,
    WoodLight = 2,
    WoodPlank = 3,
    WoodDark = 4,
    LeafDark = 10,
    Leaf = 11,
    LeafLight = 12,
    LeafAutumn = 13,
    Grass = 14,
    StoneDark = 20,
    Stone = 21,
    StoneLight = 22,
    Sand = 23,
    Dirt = 24,
    Iron = 30,
    IronDark = 31,
    Gold = 32,
    Copper = 33,
    ClothRed = 40,
    ClothBlue = 41,
    ClothGreen = 42,
    ClothCream = 43,
    Leather = 44,
    Rope = 45,
    Skin = 50,
    Hair = 51,
    Eye = 52,
    White = 53,
    Black = 54,
    AppleRed = 60,
    BerryPurple = 61,
    MushroomCap = 62,
    MushroomStem = 63,
    Flower = 64,
    FlowerYellow = 65,
    Herb = 66,
    Crystal = 67,
    Ore = 68,
    EnemySkin = 70,
    EnemySkinDark = 71,
    EnemyBelly = 72,
    SpitterSkin = 73,
    SpitterSac = 74,
    Bone = 75,
    Slime = 76,
    Fire = 80,
    FireCore = 81,
    LampGlow = 82,
    Water = 83,
    Glass = 84,
    Snow = 85,
    Mountain = 86,
    MountainDark = 87,
    // 79일차: 농사 콘텐츠 색상
    SoilTilled = 90,
    SoilWet = 91,
    Potato = 92,
    Pumpkin = 93,
    Tomato = 94,
    Strawberry = 96,
    SeedPaper = 97,
    CropGreen = 98,
    // 81일차: 수확 가능 표시
    ReadyGlow = 99,
    // 82일차: 낚시 콘텐츠 색상
    FishSilver = 100,
    FishOlive = 101,
    FishDark = 102,
    FishGold = 103,
    FishBlue = 104,
    PondDeep = 105,
    Worm = 106,
    // 84일차: 아이템 모델 보강 색상
    AppleBaked = 107,
    Caramel = 108,
    // 85일차: 요리 색상
    SoupPumpkin = 109,
    SoupTomato = 110,
    Charred = 111,
    Butter = 112,
    GrilledFish = 113,
    // 86일차: 가축 색상
    CowPink = 114,
    Straw = 115,
    Egg = 116,
    Yolk = 117,
    Grain = 118
}

public static class StylizedPalette
{
    public readonly struct Entry
    {
        public readonly Color Color;
        public readonly float Smoothness;
        public readonly float Metallic;
        public readonly float Emission;
        public readonly bool Transparent;

        public Entry(Color color, float smoothness = 0.15f, float metallic = 0f, float emission = 0f, bool transparent = false)
        {
            Color = color;
            Smoothness = smoothness;
            Metallic = metallic;
            Emission = emission;
            Transparent = transparent;
        }
    }

    public static Entry Get(StylizedColor color)
    {
        switch (color)
        {
            case StylizedColor.BarkDark: return new Entry(Hex(0x4A3322));
            case StylizedColor.Bark: return new Entry(Hex(0x6B4A2F));
            case StylizedColor.WoodLight: return new Entry(Hex(0xC39A63));
            case StylizedColor.WoodPlank: return new Entry(Hex(0xA37848));
            case StylizedColor.WoodDark: return new Entry(Hex(0x7A5433));
            case StylizedColor.LeafDark: return new Entry(Hex(0x2F6B3A));
            case StylizedColor.Leaf: return new Entry(Hex(0x4C9A45));
            case StylizedColor.LeafLight: return new Entry(Hex(0x7FBF4F));
            case StylizedColor.LeafAutumn: return new Entry(Hex(0xD98B2B));
            case StylizedColor.Grass: return new Entry(Hex(0x6DAE45));
            case StylizedColor.StoneDark: return new Entry(Hex(0x5E6268));
            case StylizedColor.Stone: return new Entry(Hex(0x8A8F96));
            case StylizedColor.StoneLight: return new Entry(Hex(0xB5B8BC));
            case StylizedColor.Sand: return new Entry(Hex(0xD8C38F));
            case StylizedColor.Dirt: return new Entry(Hex(0x8B6A45));
            case StylizedColor.Iron: return new Entry(Hex(0xA7AFB8), 0.55f, 0.6f);
            case StylizedColor.IronDark: return new Entry(Hex(0x565D66), 0.45f, 0.5f);
            case StylizedColor.Gold: return new Entry(Hex(0xE3B341), 0.6f, 0.8f);
            case StylizedColor.Copper: return new Entry(Hex(0xC0703A), 0.5f, 0.6f);
            case StylizedColor.ClothRed: return new Entry(Hex(0xB8433A));
            case StylizedColor.ClothBlue: return new Entry(Hex(0x3F6FA8));
            case StylizedColor.ClothGreen: return new Entry(Hex(0x557A3C));
            case StylizedColor.ClothCream: return new Entry(Hex(0xE9DDC0));
            case StylizedColor.Leather: return new Entry(Hex(0x7B4F2C), 0.25f);
            case StylizedColor.Rope: return new Entry(Hex(0xC9A86A));
            case StylizedColor.Skin: return new Entry(Hex(0xF1C6A0));
            case StylizedColor.Hair: return new Entry(Hex(0x5A3A26));
            case StylizedColor.Eye: return new Entry(Hex(0x1E1E24), 0.6f);
            case StylizedColor.White: return new Entry(Hex(0xF4F4F0));
            case StylizedColor.Black: return new Entry(Hex(0x1C1C20));
            case StylizedColor.AppleRed: return new Entry(Hex(0xD13A34), 0.45f);
            case StylizedColor.BerryPurple: return new Entry(Hex(0x6B3FA0), 0.45f);
            case StylizedColor.MushroomCap: return new Entry(Hex(0xC8453B), 0.3f);
            case StylizedColor.MushroomStem: return new Entry(Hex(0xEFE6D2));
            case StylizedColor.Flower: return new Entry(Hex(0xE86FA6));
            case StylizedColor.FlowerYellow: return new Entry(Hex(0xF2CE3F));
            case StylizedColor.Herb: return new Entry(Hex(0x8FBF6A));
            case StylizedColor.Crystal: return new Entry(Hex(0x6FD3E8), 0.8f, 0f, 0.35f);
            case StylizedColor.Ore: return new Entry(Hex(0xB07B55), 0.45f, 0.4f);
            case StylizedColor.EnemySkin: return new Entry(Hex(0x6E8F3E), 0.2f);
            case StylizedColor.EnemySkinDark: return new Entry(Hex(0x4A6329), 0.2f);
            case StylizedColor.EnemyBelly: return new Entry(Hex(0xB9C27A), 0.2f);
            case StylizedColor.SpitterSkin: return new Entry(Hex(0x7A4FA3), 0.35f);
            case StylizedColor.SpitterSac: return new Entry(Hex(0xB6E35A), 0.55f, 0f, 0.45f);
            case StylizedColor.Bone: return new Entry(Hex(0xE8DFC8));
            case StylizedColor.Slime: return new Entry(Hex(0x9BE04F), 0.7f, 0f, 0.6f);
            case StylizedColor.Fire: return new Entry(Hex(0xFF6A1A), 0f, 0f, 1.4f);
            case StylizedColor.FireCore: return new Entry(Hex(0xFFC84A), 0f, 0f, 1.9f);
            case StylizedColor.LampGlow: return new Entry(Hex(0xFFE3A1), 0f, 0f, 2.5f);
            case StylizedColor.Water: return new Entry(new Color(0.24f, 0.56f, 0.75f, 0.72f), 0.85f, 0f, 0f, true);
            case StylizedColor.Glass: return new Entry(new Color(0.75f, 0.9f, 1f, 0.45f), 0.9f, 0f, 0f, true);
            case StylizedColor.Snow: return new Entry(Hex(0xF3F7FA));
            case StylizedColor.Mountain: return new Entry(Hex(0x7C8C9E));
            case StylizedColor.MountainDark: return new Entry(Hex(0x5A6878));
            case StylizedColor.SoilTilled: return new Entry(Hex(0x7A5A40));
            case StylizedColor.SoilWet: return new Entry(Hex(0x4A3526), 0.3f);
            case StylizedColor.Potato: return new Entry(Hex(0xC9A26B));
            case StylizedColor.Pumpkin: return new Entry(Hex(0xE8822A), 0.3f);
            case StylizedColor.Tomato: return new Entry(Hex(0xE0402E), 0.55f);
            case StylizedColor.Strawberry: return new Entry(Hex(0xD92E45), 0.5f);
            case StylizedColor.SeedPaper: return new Entry(Hex(0xDCC7A1));
            case StylizedColor.CropGreen: return new Entry(Hex(0x5DB84A));
            case StylizedColor.ReadyGlow: return new Entry(Hex(0xFFC93C), 0.4f, 0f, 0.9f);
            case StylizedColor.FishSilver: return new Entry(Hex(0xAFC0C8), 0.7f, 0.3f);
            case StylizedColor.FishOlive: return new Entry(Hex(0x7D8A4E), 0.55f);
            case StylizedColor.FishDark: return new Entry(Hex(0x4B4A45), 0.5f);
            case StylizedColor.FishGold: return new Entry(Hex(0xF0A93A), 0.7f, 0.35f);
            case StylizedColor.FishBlue: return new Entry(Hex(0x8DB6D6), 0.75f, 0.2f);
            case StylizedColor.PondDeep: return new Entry(Hex(0x1E3A40));
            case StylizedColor.Worm: return new Entry(Hex(0xC97A7A), 0.45f);
            case StylizedColor.AppleBaked: return new Entry(Hex(0x9A3B26), 0.35f);
            case StylizedColor.Caramel: return new Entry(Hex(0xD08A32), 0.65f);
            case StylizedColor.SoupPumpkin: return new Entry(Hex(0xF0A13A), 0.55f);
            case StylizedColor.SoupTomato: return new Entry(Hex(0xC4412C), 0.55f);
            case StylizedColor.Charred: return new Entry(Hex(0x3A2A20), 0.2f);
            case StylizedColor.Butter: return new Entry(Hex(0xF6DE7A), 0.6f);
            case StylizedColor.GrilledFish: return new Entry(Hex(0xC98A4E), 0.5f);
            case StylizedColor.CowPink: return new Entry(Hex(0xE9A3A0), 0.3f);
            case StylizedColor.Straw: return new Entry(Hex(0xDDBA5C));
            case StylizedColor.Egg: return new Entry(Hex(0xF3E6CF), 0.45f);
            case StylizedColor.Yolk: return new Entry(Hex(0xF5B42A), 0.6f);
            case StylizedColor.Grain: return new Entry(Hex(0xB98E4B));
            default: return new Entry(Color.magenta);
        }
    }

    private static Color Hex(int rgb)
    {
        return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
    }
}
