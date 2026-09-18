using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// 84일차: 저폴리 모델 Prefab을 찍어 투명 배경 아이템 아이콘 PNG를 만든다.
// 미리보기 전용 Scene(PreviewRenderUtility)에서 검은 배경과 흰 배경으로 두 번 찍어
// 두 결과의 차이로 투명도를 계산하므로 렌더 파이프라인의 알파 출력에 영향을 받지 않는다.
// 미리보기 Scene은 여러 번 만들고 지우면 불안정하므로 한 세션에서 모든 아이콘을 찍는다.
public sealed class ItemIconRenderer : IDisposable
{
    public const int IconSize = 128;
    private const int Supersample = 4;
    private const float FieldOfView = 14f;
    private const float Fill = 0.84f;

    public struct Framing
    {
        public Vector3 ModelEuler; // 모델 회전
        public float Yaw; // 카메라 좌우 각도
        public float Pitch; // 카메라 내려다보는 각도
        public float Zoom; // 1보다 크면 더 크게

        public Framing(Vector3 modelEuler, float yaw = 35f, float pitch = 24f, float zoom = 1f)
        {
            ModelEuler = modelEuler;
            Yaw = yaw;
            Pitch = pitch;
            Zoom = zoom;
        }

        public static Framing Default => new Framing(Vector3.zero);
    }

    private PreviewRenderUtility preview;
    private RenderTexture target;
    private Texture2D readback;

    public ItemIconRenderer()
    {
        int size = IconSize * Supersample;
        preview = new PreviewRenderUtility();
        preview.cameraFieldOfView = FieldOfView;
        target = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        target.Create();
        readback = new Texture2D(size, size, TextureFormat.RGBA32, false, false);

        Camera camera = preview.camera;
        camera.fieldOfView = FieldOfView;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.allowHDR = false;
        camera.allowMSAA = false;

        // 따뜻한 주광 + 차가운 역광, 은은한 환경광 (게임 시간·날씨와 무관하게 고정)
        preview.lights[0].intensity = 1.25f;
        preview.lights[0].color = new Color(1f, 0.96f, 0.9f);
        preview.lights[1].intensity = 0.55f;
        preview.lights[1].color = new Color(0.75f, 0.85f, 1f);
        preview.ambientColor = new Color(0.42f, 0.43f, 0.46f);
    }

    public void Dispose()
    {
        if (preview != null)
        {
            preview.camera.targetTexture = null;
            preview.Cleanup();
            preview = null;
        }

        if (target != null)
        {
            target.Release();
            Object.DestroyImmediate(target);
            target = null;
        }

        if (readback != null)
        {
            Object.DestroyImmediate(readback);
            readback = null;
        }
    }

    // 찍은 결과를 PNG 바이트로 돌려준다 (에셋 가져오기는 세션을 닫은 뒤에 한다)
    public byte[] RenderPng(GameObject modelPrefab, Framing framing, out string error, Action<GameObject> prepare = null, Func<Vector3, bool> includeVertex = null)
    {
        Texture2D icon = Render(modelPrefab, framing, out error, prepare, includeVertex);

        if (icon == null)
        {
            return null;
        }

        byte[] png = icon.EncodeToPNG();
        Object.DestroyImmediate(icon);
        return png;
    }

    // 91일차: prepare = 찍기 전 인스턴스 손질 (NPC 색 적용 등), includeVertex = 화면 맞춤에 쓸 꼭짓점 (초상은 가슴 위만)
    public Texture2D Render(GameObject modelPrefab, Framing framing, out string error, Action<GameObject> prepare = null, Func<Vector3, bool> includeVertex = null)
    {
        error = string.Empty;

        if (preview == null)
        {
            error = "아이콘 렌더러가 이미 닫혔습니다.";
            return null;
        }

        if (modelPrefab == null)
        {
            error = "모델 Prefab이 없습니다.";
            return null;
        }

        GameObject instance = preview.InstantiatePrefabInScene(modelPrefab);

        try
        {
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(framing.ModelEuler));
            prepare?.Invoke(instance);
            MeshFilter[] filters = instance.GetComponentsInChildren<MeshFilter>(true);

            if (filters.Length == 0)
            {
                error = $"{modelPrefab.name}에 Mesh가 없습니다.";
                return null;
            }

            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            Quaternion viewRotation = Quaternion.Euler(framing.Pitch, framing.Yaw + 180f, 0f);
            Vector3 forward = viewRotation * Vector3.forward;
            Vector3 right = viewRotation * Vector3.right;
            Vector3 up = viewRotation * Vector3.up;

            // 모든 꼭짓점을 카메라 방향 기준으로 투영해 화면에 꽉 차는 위치와 거리를 구한다
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;

            foreach (MeshFilter filter in filters)
            {
                Mesh mesh = filter.sharedMesh;

                if (mesh == null)
                {
                    continue;
                }

                Matrix4x4 matrix = filter.transform.localToWorldMatrix;
                Vector3[] vertices = mesh.vertices;

                for (int index = 0; index < vertices.Length; index++)
                {
                    Vector3 world = matrix.MultiplyPoint3x4(vertices[index]);

                    if (includeVertex != null && !includeVertex(world))
                    {
                        continue;
                    }

                    float x = Vector3.Dot(world, right);
                    float y = Vector3.Dot(world, up);
                    float z = Vector3.Dot(world, forward);
                    minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
                    minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
                    minZ = Mathf.Min(minZ, z); maxZ = Mathf.Max(maxZ, z);
                }
            }

            float halfExtent = Mathf.Max(maxX - minX, maxY - minY) * 0.5f / Mathf.Max(0.1f, framing.Zoom);
            float depth = maxZ - minZ;
            float distance = halfExtent / Mathf.Tan(FieldOfView * 0.5f * Mathf.Deg2Rad) / Fill + depth * 0.5f;
            Vector3 center = right * ((minX + maxX) * 0.5f) + up * ((minY + maxY) * 0.5f) + forward * ((minZ + maxZ) * 0.5f);

            Camera camera = preview.camera;
            camera.nearClipPlane = Mathf.Max(0.01f, distance - depth - 0.5f);
            camera.farClipPlane = distance + depth + 0.5f;
            camera.transform.SetPositionAndRotation(center - forward * distance, viewRotation);
            preview.lights[0].transform.rotation = Quaternion.Euler(48f, framing.Yaw + 150f, 0f);
            preview.lights[1].transform.rotation = Quaternion.Euler(-10f, framing.Yaw - 20f, 0f);

            Color32[] black = RenderOnce(Color.black);
            Color32[] white = RenderOnce(Color.white);
            return Compose(black, white, target.width);
        }
        finally
        {
            // 미리보기 Scene이 만든 오브젝트는 세션을 닫을 때 함께 지운다
            instance.SetActive(false);
        }
    }

    private Color32[] RenderOnce(Color background)
    {
        Camera camera = preview.camera;
        RenderTexture previousActive = RenderTexture.active;

        // Inspector 미리보기와 같은 순서(Begin → Render → End)로 조명 설정 전환·복구를 맡긴다
        preview.BeginStaticPreview(new Rect(0f, 0f, 16f, 16f));

        try
        {
            camera.backgroundColor = background;
            camera.targetTexture = target;
            preview.Render(true, false);
            RenderTexture.active = target;
            readback.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            readback.Apply();
            return readback.GetPixels32();
        }
        finally
        {
            RenderTexture.active = previousActive;
            Texture2D unused = preview.EndStaticPreview();
            Object.DestroyImmediate(unused);
        }
    }

    // 검은·흰 배경 결과로 색과 투명도를 되살린 뒤 축소하고 옅은 그림자를 깐다
    private static Texture2D Compose(Color32[] black, Color32[] white, int size)
    {
        float[] r = new float[size * size];
        float[] g = new float[size * size];
        float[] b = new float[size * size];
        float[] a = new float[size * size];

        for (int index = 0; index < black.Length; index++)
        {
            Color32 onBlack = black[index];
            Color32 onWhite = white[index];
            float difference = ((onWhite.r - onBlack.r) + (onWhite.g - onBlack.g) + (onWhite.b - onBlack.b)) / (3f * 255f);
            float alpha = Mathf.Clamp01(1f - difference);
            a[index] = alpha;

            if (alpha > 0.001f)
            {
                r[index] = Mathf.Clamp01(onBlack.r / 255f / alpha);
                g[index] = Mathf.Clamp01(onBlack.g / 255f / alpha);
                b[index] = Mathf.Clamp01(onBlack.b / 255f / alpha);
            }
        }

        int output = size / Supersample;
        Color[] small = new Color[output * output];
        float count = Supersample * Supersample;

        for (int y = 0; y < output; y++)
        {
            for (int x = 0; x < output; x++)
            {
                float sumR = 0f, sumG = 0f, sumB = 0f, sumA = 0f;

                for (int sy = 0; sy < Supersample; sy++)
                {
                    for (int sx = 0; sx < Supersample; sx++)
                    {
                        int source = (y * Supersample + sy) * size + (x * Supersample + sx);
                        float alpha = a[source];
                        sumR += r[source] * alpha;
                        sumG += g[source] * alpha;
                        sumB += b[source] * alpha;
                        sumA += alpha;
                    }
                }

                small[y * output + x] = sumA > 0.0001f
                    ? new Color(sumR / sumA, sumG / sumA, sumB / sumA, sumA / count)
                    : new Color(0f, 0f, 0f, 0f);
            }
        }

        // 어두운 슬롯에서도 모양이 보이도록 오른쪽 아래로 옅은 그림자
        Color[] result = new Color[small.Length];

        for (int y = 0; y < output; y++)
        {
            for (int x = 0; x < output; x++)
            {
                float shadow = 0f;
                int sx = x - 2;
                int sy = y + 2;

                if (sx >= 0 && sy < output)
                {
                    shadow = small[sy * output + sx].a * 0.45f;
                }

                Color top = small[y * output + x];
                float alpha = top.a + shadow * (1f - top.a);

                if (alpha <= 0.0001f)
                {
                    result[y * output + x] = new Color(0f, 0f, 0f, 0f);
                    continue;
                }

                float weight = top.a / alpha;
                result[y * output + x] = new Color(top.r * weight, top.g * weight, top.b * weight, alpha);
            }
        }

        Texture2D texture = new Texture2D(output, output, TextureFormat.RGBA32, false, false);
        texture.SetPixels(result);
        texture.Apply();
        return texture;
    }

    public static void WritePng(byte[] png, string assetPath)
    {
        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
        File.WriteAllBytes(fullPath, png);
    }

    public static void ImportAsSprite(string assetPath)
    {
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;

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
        importer.maxTextureSize = IconSize;
        importer.spritePixelsPerUnit = 100f;
        importer.SaveAndReimport();
    }
}
