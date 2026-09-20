using UnityEngine;

// 78일차: 저폴리 불꽃과 조명을 가볍게 흔들어 살아있는 불처럼 보이게 한다.
[DisallowMultipleComponent]
public sealed class StylizedFlameFlicker : MonoBehaviour
{
    [SerializeField] private Light targetLight;
    [SerializeField, Min(0f)] private float scaleAmount = 0.12f;
    [SerializeField, Min(0f)] private float lightAmount = 0.25f;
    [SerializeField, Min(0.1f)] private float speed = 7f;

    private Vector3 baseScale;
    private float baseIntensity;
    private float seed;

    private void Awake()
    {
        baseScale = transform.localScale;
        seed = Random.value * 100f;

        if (targetLight == null) // 116일차: 같은 오브젝트의 불빛을 자동으로 사용 (횃불처럼 코드로 붙일 때)
        {
            targetLight = GetComponent<Light>();
        }

        if (targetLight != null)
        {
            baseIntensity = targetLight.intensity;
        }
    }

    private void OnDisable()
    {
        transform.localScale = baseScale;

        if (targetLight != null)
        {
            targetLight.intensity = baseIntensity;
        }
    }

    private void Update()
    {
        float time = Time.time * speed + seed;
        float noise = Mathf.PerlinNoise(time, seed) * 2f - 1f;
        float sway = Mathf.Sin(time * 1.7f) * 0.5f;
        transform.localScale = new Vector3(
            baseScale.x * (1f - noise * scaleAmount * 0.5f),
            baseScale.y * (1f + noise * scaleAmount + sway * scaleAmount * 0.3f),
            baseScale.z * (1f - noise * scaleAmount * 0.5f));

        if (targetLight != null)
        {
            targetLight.intensity = baseIntensity * (1f + noise * lightAmount);
        }
    }

    public void Configure(Light light)
    {
        targetLight = light;
        baseIntensity = light != null ? light.intensity : 0f;
    }
}
