using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// 78일차: UI에 쓰는 둥근 패널·버튼·슬롯·아이콘 스프라이트를 코드로 그려 PNG로 저장한다.
public static class UISpriteFactory
{
    public const string SpriteFolder = "Assets/_ProjectU/UI/Sprites/Generated";
    public const string IconFolder = "Assets/_ProjectU/UI/Icons/Generated";

    public static Sprite Panel => Get("UI_Panel");
    public static Sprite PanelOutline => Get("UI_PanelOutline");
    public static Sprite Button => Get("UI_Button");
    public static Sprite Slot => Get("UI_Slot");
    public static Sprite Pill => Get("UI_Pill");
    public static Sprite Shadow => Get("UI_Shadow");
    public static Sprite CircleSprite => Get("UI_Circle");
    public static Sprite CircleRing => Get("UI_CircleRing");
    public static Sprite Vignette => Get("UI_Vignette");

    public static Sprite Icon(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>($"{IconFolder}/ICON_{name}.png");
    }

    private static Sprite Get(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/{name}.png");
    }

    public static void GenerateAll()
    {
        StylizedArtAssetFactory.EnsureFolder(SpriteFolder);
        StylizedArtAssetFactory.EnsureFolder(IconFolder);

        Write(SpriteFolder, "UI_Panel", 64, 64, 20, (x, y) => Fill(RoundBox(x, y, 64, 64, 14f)));
        Write(SpriteFolder, "UI_PanelOutline", 64, 64, 20, (x, y) =>
        {
            float d = RoundBox(x, y, 64, 64, 14f);
            float alpha = Fill(d) * (1f - Fill(d + 2.5f));
            return new Color(1f, 1f, 1f, alpha);
        });
        Write(SpriteFolder, "UI_Button", 64, 64, 20, (x, y) =>
        {
            float d = RoundBox(x, y, 64, 64, 14f);
            float t = y / 63f;
            float shade = Mathf.Lerp(0.8f, 1f, t);
            // 위쪽 안쪽 밝은 선
            float highlight = Fill(d + 2f) * (1f - Fill(d + 3.5f)) * Mathf.Clamp01((t - 0.55f) * 2.5f) * 0.25f;
            float value = Mathf.Clamp01(shade + highlight);
            return new Color(value, value, value, Fill(d));
        });
        Write(SpriteFolder, "UI_Slot", 64, 64, 18, (x, y) =>
        {
            float d = RoundBox(x, y, 64, 64, 11f);
            float border = Fill(d) * (1f - Fill(d + 2f));
            float t = y / 63f;
            float inner = Mathf.Lerp(0.78f, 0.9f, t);
            float value = Mathf.Lerp(inner, 1.25f, border);
            return new Color(Mathf.Clamp01(value), Mathf.Clamp01(value), Mathf.Clamp01(value), Fill(d));
        });
        Write(SpriteFolder, "UI_Pill", 64, 32, 15, (x, y) => Fill(RoundBox(x, y, 64, 32, 15.5f)));
        // 게이지 채움용 (Filled 방식은 9-slice를 쓰지 않으므로 실제 비율에 가까운 가로형으로 만든다)
        Write(SpriteFolder, "UI_BarFill", 256, 16, 0, (x, y) =>
        {
            float d = RoundBox(x, y, 256, 16, 7.5f);
            float t = y / 15f;
            float value = Mathf.Lerp(0.82f, 1f, t);
            return new Color(value, value, value, Fill(d));
        });
        Write(SpriteFolder, "UI_Shadow", 96, 96, 44, (x, y) =>
        {
            float d = RoundBox(x, y, 96, 96, 30f, 18f);
            float alpha = Mathf.Clamp01(1f - Mathf.InverseLerp(-8f, 16f, d));
            alpha = alpha * alpha * (3f - 2f * alpha);
            return new Color(0f, 0f, 0f, alpha * 0.6f);
        });
        Write(SpriteFolder, "UI_Circle", 128, 128, 0, (x, y) => Fill(Length(x - 63.5f, y - 63.5f) - 62f));
        Write(SpriteFolder, "UI_CircleRing", 128, 128, 0, (x, y) =>
        {
            float d = Length(x - 63.5f, y - 63.5f) - 62f;
            return new Color(1f, 1f, 1f, Fill(d) * (1f - Fill(d + 6f)));
        });
        Write(SpriteFolder, "UI_Vignette", 128, 128, 0, (x, y) =>
        {
            float distance = Length((x - 63.5f) / 63.5f, (y - 63.5f) / 63.5f);
            float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 1.2f, distance));
            return new Color(0f, 0f, 0f, alpha);
        });

        WriteIcon("Health", HeartIcon);
        WriteIcon("Hunger", FoodIcon);
        WriteIcon("Thirst", DropIcon);
        WriteIcon("Wetness", RainIcon);
        WriteIcon("Temperature", ThermometerIcon);
        WriteIcon("Sun", SunIcon);
        WriteIcon("Leaf", LeafIcon);
        WriteIcon("Bag", BagIcon);
        WriteIcon("Compass", CompassIcon);
        WriteIcon("Pickup", PickupIcon);
        WriteCookingIcons();

        AssetDatabase.SaveAssets();
    }

    // 85일차 추가: 요리 창·음식 효과 아이콘만 따로 만든다
    public static void GenerateCookingIcons()
    {
        StylizedArtAssetFactory.EnsureFolder(IconFolder);
        WriteCookingIcons();
        AssetDatabase.SaveAssets();
    }

    private static void WriteCookingIcons()
    {
        WriteIcon("Flame", FlameIcon);
        WriteIcon("Bolt", BoltIcon);
        WriteIcon("Speed", SpeedIcon);
        WriteIcon("Clock", ClockIcon);
        WriteIcon("Check", CheckIcon);
    }

    // 82일차 추가: 근처 아이템 아이콘만 따로 만든다 (다른 스프라이트는 다시 쓰지 않음)
    public static void GeneratePickupIcon()
    {
        StylizedArtAssetFactory.EnsureFolder(IconFolder);
        WriteIcon("Pickup", PickupIcon);
    }

    // ------------------------------------------------------------ 저장

    private static void Write(string folder, string name, int width, int height, int border, Func<float, float, float> alphaOnly)
    {
        Write(folder, name, width, height, border, (x, y) => new Color(1f, 1f, 1f, alphaOnly(x, y)));
    }

    private static void Write(string folder, string name, int width, int height, int border, Func<float, float, Color> pixel)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                pixels[y * width + x] = pixel(x + 0.5f, y + 0.5f);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        string assetPath = $"{folder}/{name}.png";
        File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), assetPath), texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);

        if (importer == null)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spritePixelsPerUnit = 100f;

        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteBorder = new Vector4(border, Mathf.Min(border, height / 2 - 1), border, Mathf.Min(border, height / 2 - 1));
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    private static void WriteIcon(string name, Func<float, float, float> sdf)
    {
        // 64 픽셀 공간을 -1~1 좌표로 바꿔 부호 거리 함수로 그린다
        Write(IconFolder, $"ICON_{name}", 64, 64, 0, (x, y) =>
        {
            float u = (x / 64f) * 2f - 1f;
            float v = (y / 64f) * 2f - 1f;
            float distance = sdf(u, v) * 32f;
            return Fill(distance);
        });
    }

    // ------------------------------------------------------------ 도형 함수

    private static float Fill(float signedDistance)
    {
        return Mathf.Clamp01(0.5f - signedDistance);
    }

    private static float Length(float x, float y)
    {
        return Mathf.Sqrt(x * x + y * y);
    }

    private static float RoundBox(float x, float y, int width, int height, float radius, float inset = 0.5f)
    {
        float halfX = width * 0.5f - inset;
        float halfY = height * 0.5f - inset;
        float px = Mathf.Abs(x - width * 0.5f) - halfX + radius;
        float py = Mathf.Abs(y - height * 0.5f) - halfY + radius;
        float outside = Length(Mathf.Max(px, 0f), Mathf.Max(py, 0f));
        float inside = Mathf.Min(Mathf.Max(px, py), 0f);
        return outside + inside - radius;
    }

    private static float Circle(float x, float y, float cx, float cy, float r)
    {
        return Length(x - cx, y - cy) - r;
    }

    private static float Capsule(float x, float y, float ax, float ay, float bx, float by, float r)
    {
        float pax = x - ax;
        float pay = y - ay;
        float bax = bx - ax;
        float bay = by - ay;
        float h = Mathf.Clamp01((pax * bax + pay * bay) / (bax * bax + bay * bay));
        return Length(pax - bax * h, pay - bay * h) - r;
    }

    private static float Box(float x, float y, float cx, float cy, float hx, float hy, float r)
    {
        float px = Mathf.Abs(x - cx) - hx + r;
        float py = Mathf.Abs(y - cy) - hy + r;
        return Length(Mathf.Max(px, 0f), Mathf.Max(py, 0f)) + Mathf.Min(Mathf.Max(px, py), 0f) - r;
    }

    private static float Union(float a, float b) => Mathf.Min(a, b);
    private static float Subtract(float a, float b) => Mathf.Max(a, -b);

    private static float HeartIcon(float x, float y)
    {
        // Inigo Quilez 하트 거리 함수 (좌표 보정)
        float px = Mathf.Abs(x) / 1.05f;
        float py = (y + 0.72f) / 1.05f;

        if (py + px > 1f)
        {
            return (Length(px - 0.25f, py - 0.75f) - Mathf.Sqrt(2f) / 4f) * 1.05f;
        }

        float a = (px * px) + (py - 1f) * (py - 1f);
        float m = 0.5f * Mathf.Max(px + py, 0f);
        float b = (px - m) * (px - m) + (py - m) * (py - m);
        return Mathf.Sqrt(Mathf.Min(a, b)) * Mathf.Sign(px - py) * 1.05f;
    }

    private static float FoodIcon(float x, float y)
    {
        float meat = Circle(x, y, 0.22f, 0.22f, 0.5f);
        float bone = Capsule(x, y, -0.1f, -0.1f, -0.62f, -0.62f, 0.11f);
        float knob1 = Circle(x, y, -0.78f, -0.56f, 0.13f);
        float knob2 = Circle(x, y, -0.56f, -0.78f, 0.13f);
        float bite = Circle(x, y, 0.62f, 0.62f, 0.2f);
        return Subtract(Union(Union(meat, bone), Union(knob1, knob2)), bite);
    }

    private static float DropIcon(float x, float y)
    {
        float body = Circle(x, y, 0f, -0.25f, 0.52f);
        // 위쪽 뾰족한 부분 (삼각형 근사)
        float tip = Mathf.Max(Mathf.Abs(x) * 1.25f + (y - 0.85f) * 0.72f, -(y + 0.05f));
        float shine = Circle(x, y, -0.2f, -0.2f, 0.12f);
        return Subtract(Union(body, tip), shine);
    }

    private static float RainIcon(float x, float y)
    {
        float cloud = Union(Union(Circle(x, y, -0.3f, 0.25f, 0.32f), Circle(x, y, 0.12f, 0.38f, 0.4f)), Box(x, y, 0f, 0.08f, 0.62f, 0.2f, 0.18f));
        float drop1 = Capsule(x, y, -0.35f, -0.35f, -0.45f, -0.7f, 0.07f);
        float drop2 = Capsule(x, y, 0.05f, -0.3f, -0.05f, -0.65f, 0.07f);
        float drop3 = Capsule(x, y, 0.45f, -0.35f, 0.35f, -0.7f, 0.07f);
        return Union(cloud, Union(drop1, Union(drop2, drop3)));
    }

    private static float ThermometerIcon(float x, float y)
    {
        float tube = Capsule(x, y, 0f, -0.35f, 0f, 0.72f, 0.2f);
        float bulb = Circle(x, y, 0f, -0.55f, 0.33f);
        float outline = Union(tube, bulb);
        float hollow = Capsule(x, y, 0f, -0.2f, 0f, 0.72f, 0.09f);
        float marks = Union(Box(x, y, 0.38f, 0.45f, 0.1f, 0.03f, 0f), Box(x, y, 0.38f, 0.15f, 0.1f, 0.03f, 0f));
        // 눈금 위쪽만 비워 아래쪽은 수은이 찬 것처럼 보이게 한다
        float hollowUpper = Mathf.Max(hollow, -(y - 0.05f));
        return Union(Subtract(outline, hollowUpper), marks);
    }

    private static float SunIcon(float x, float y)
    {
        float core = Circle(x, y, 0f, 0f, 0.36f);

        for (int index = 0; index < 8; index++)
        {
            float angle = index * Mathf.PI / 4f;
            float cx = Mathf.Cos(angle);
            float cy = Mathf.Sin(angle);
            core = Union(core, Capsule(x, y, cx * 0.56f, cy * 0.56f, cx * 0.82f, cy * 0.82f, 0.075f));
        }

        return core;
    }

    private static float LeafIcon(float x, float y)
    {
        // 두 원의 교집합으로 만든 잎 + 잎맥
        float rx = x * 0.707f - y * 0.707f;
        float ry = x * 0.707f + y * 0.707f;
        float leaf = Mathf.Max(Circle(rx, ry, 0f, -0.55f, 1f), Circle(rx, ry, 0f, 0.55f, 1f));
        float vein = Capsule(rx, ry, 0f, -0.45f, 0f, 0.55f, 0.04f);
        float stem = Capsule(x, y, -0.55f, -0.55f, -0.85f, -0.85f, 0.06f);
        return Union(Subtract(leaf, vein), stem);
    }

    private static float BagIcon(float x, float y)
    {
        float body = Box(x, y, 0f, -0.2f, 0.62f, 0.55f, 0.22f);
        float handle = Mathf.Abs(Circle(x, y, 0f, 0.38f, 0.32f)) - 0.08f;
        handle = Mathf.Max(handle, -(y - 0.38f));
        float pocket = Box(x, y, 0f, -0.35f, 0.3f, 0.02f, 0f);
        return Subtract(Union(body, handle), pocket);
    }

    // 82일차 추가: 근처 아이템 (쟁반으로 들어가는 화살표)
    private static float PickupIcon(float x, float y)
    {
        float tray = Subtract(Box(x, y, 0f, -0.55f, 0.72f, 0.28f, 0.08f), Box(x, y, 0f, -0.4f, 0.52f, 0.28f, 0.04f));
        float shaft = Capsule(x, y, 0f, 0.78f, 0f, 0f, 0.11f);
        float head = Mathf.Max((Mathf.Abs(x) - (y + 0.25f) * 0.9f) * 0.743f, y - 0.15f);
        return Union(tray, Union(shaft, head));
    }

    // 85일차 추가: 불꽃 (아래 둥근 몸통 + 위로 뾰족한 끝 + 안쪽 작은 불꽃 구멍)
    private static float FlameIcon(float x, float y)
    {
        float body = Circle(x, y, 0f, -0.3f, 0.52f);
        float tip = Mathf.Max(Mathf.Abs(x + (y - 0.1f) * 0.18f) * 1.3f + (y - 0.92f) * 0.62f, -(y + 0.1f));
        float outer = Union(body, tip);
        float innerBody = Circle(x, y, 0.02f, -0.42f, 0.24f);
        float innerTip = Mathf.Max(Mathf.Abs(x - 0.02f) * 1.5f + (y - 0.12f) * 0.7f, -(y + 0.3f));
        return Subtract(outer, Union(innerBody, innerTip));
    }

    // 85일차 추가: 번개 (스태미나)
    private static float BoltIcon(float x, float y)
    {
        float upper = Capsule(x, y, 0.3f, 0.85f, -0.22f, -0.02f, 0.13f);
        float middle = Capsule(x, y, -0.22f, -0.02f, 0.24f, 0.02f, 0.12f);
        float lower = Capsule(x, y, 0.24f, 0.02f, -0.3f, -0.85f, 0.13f);
        return Union(upper, Union(middle, lower));
    }

    // 85일차 추가: 속도 (겹친 화살표 두 개와 선)
    private static float SpeedIcon(float x, float y)
    {
        float first = Union(Capsule(x, y, -0.05f, 0.5f, 0.4f, 0f, 0.1f), Capsule(x, y, 0.4f, 0f, -0.05f, -0.5f, 0.1f));
        float second = Union(Capsule(x, y, 0.3f, 0.5f, 0.75f, 0f, 0.1f), Capsule(x, y, 0.75f, 0f, 0.3f, -0.5f, 0.1f));
        float lines = Union(Capsule(x, y, -0.85f, 0.28f, -0.35f, 0.28f, 0.07f), Capsule(x, y, -0.85f, -0.28f, -0.35f, -0.28f, 0.07f));
        return Union(Union(first, second), lines);
    }

    // 85일차 추가: 시계 (조리 시간)
    private static float ClockIcon(float x, float y)
    {
        float ring = Mathf.Abs(Circle(x, y, 0f, 0f, 0.74f)) - 0.1f;
        float hourHand = Capsule(x, y, 0f, 0f, 0f, 0.44f, 0.08f);
        float minuteHand = Capsule(x, y, 0f, 0f, 0.34f, -0.18f, 0.08f);
        return Union(ring, Union(hourHand, minuteHand));
    }

    // 85일차 추가: 체크 (완성)
    private static float CheckIcon(float x, float y)
    {
        return Union(Capsule(x, y, -0.62f, 0.02f, -0.2f, -0.44f, 0.14f), Capsule(x, y, -0.2f, -0.44f, 0.66f, 0.5f, 0.14f));
    }

    private static float CompassIcon(float x, float y)
    {
        float ring = Mathf.Abs(Circle(x, y, 0f, 0f, 0.78f)) - 0.08f;
        float needleUp = Mathf.Max(Mathf.Abs(x) * 2.6f + y * 0.6f - 0.35f, -y);
        float needleDown = Mathf.Max(Mathf.Abs(x) * 2.6f - y * 0.6f - 0.35f, y);
        return Union(ring, Union(needleUp, needleDown));
    }
}
