using UnityEngine;

// 76일차: 피격·처치 시 튀어나가는 작은 파편 (별도 이펙트 에셋 없이 사용하는 기본 효과)
[DisallowMultipleComponent]
public sealed class CombatFeedbackDebris : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private const float Gravity = 12f;

    private static Mesh cubeMesh;
    private static Material cubeMaterial;
    private static MaterialPropertyBlock propertyBlock;

    private Vector3 velocity;
    private Vector3 angularVelocity;
    private Vector3 startScale;
    private float lifetime;
    private float startTime;

    public static void Burst(
        Vector3 position,
        Vector3 mainDirection,
        int count,
        Color color,
        float size,
        float speed,
        float pieceLifetime)
    {
        if (count <= 0 || !EnsureResources())
        {
            return;
        }

        Vector3 direction = mainDirection.sqrMagnitude > 0.0001f ? mainDirection.normalized : Vector3.up;
        propertyBlock.SetColor(BaseColorId, color);
        propertyBlock.SetColor(ColorId, color);

        for (int index = 0; index < count; index++)
        {
            GameObject piece = new GameObject("CombatDebris");
            piece.transform.position = position;
            piece.transform.rotation = Random.rotation;
            float pieceSize = size * Random.Range(0.6f, 1.3f);
            piece.transform.localScale = Vector3.one * pieceSize;

            piece.AddComponent<MeshFilter>().sharedMesh = cubeMesh;
            MeshRenderer renderer = piece.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = cubeMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.SetPropertyBlock(propertyBlock);

            CombatFeedbackDebris debris = piece.AddComponent<CombatFeedbackDebris>();
            Vector3 spread = (direction + Random.insideUnitSphere * 0.9f).normalized;
            debris.velocity = spread * speed * Random.Range(0.5f, 1f) + Vector3.up * speed * 0.35f;
            debris.angularVelocity = Random.insideUnitSphere * 720f;
            debris.startScale = piece.transform.localScale;
            debris.lifetime = pieceLifetime * Random.Range(0.8f, 1.2f);
            debris.startTime = Time.time;
        }
    }

    private void Update()
    {
        float t = (Time.time - startTime) / lifetime;

        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        float deltaTime = Time.deltaTime;
        velocity += Vector3.down * (Gravity * deltaTime);
        transform.position += velocity * deltaTime;
        transform.Rotate(angularVelocity * deltaTime, Space.Self);
        transform.localScale = startScale * (1f - t * t);
    }

    private static bool EnsureResources()
    {
        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        if (cubeMesh != null && cubeMaterial != null)
        {
            return true;
        }

        // 기본 Cube에서 Mesh와 Render Pipeline 기본 Material만 가져온 뒤 원본은 제거
        GameObject template = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cubeMesh = template.GetComponent<MeshFilter>().sharedMesh;
        cubeMaterial = template.GetComponent<MeshRenderer>().sharedMaterial;
        template.SetActive(false);
        Destroy(template);
        return cubeMesh != null && cubeMaterial != null;
    }
}
