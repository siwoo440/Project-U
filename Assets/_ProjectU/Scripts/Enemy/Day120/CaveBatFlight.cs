using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class CaveBatFlight : MonoBehaviour // 120일차: 박쥐가 공중에서 위아래로 흔들리며 나는 모습
{
    [Header("Flight")] // 나는 모습 설정 묶음
    [Tooltip("흔들리는 높이 (m).")]
    [SerializeField, Min(0f)] private float bobHeight = 0.35f; // 흔들리는 높이
    [Tooltip("흔들리는 빠르기.")]
    [SerializeField, Min(0.1f)] private float bobSpeed = 3.2f; // 흔들리는 빠르기
    [Tooltip("좌우로 기우는 각도.")]
    [SerializeField] private float rollAngle = 12f; // 기우는 각도
    [Tooltip("흔들릴 외형 (비우면 Visual 자식을 찾습니다).")]
    [SerializeField] private Transform visual; // 흔들릴 외형

    private Vector3 basePosition; // 처음 자리
    private float phase; // 흔들림 시작 위치

    public Transform Visual => visual; // 외형 제공 (테스트용)

    private void Awake()
    {
        if (visual == null)
        {
            visual = transform.Find("Visual");
        }

        if (visual != null)
        {
            basePosition = visual.localPosition;
        }

        phase = Random.value * 10f;
    }

    private void Update()
    {
        if (visual == null)
        {
            return;
        }

        float wave = Mathf.Sin((Time.time + phase) * bobSpeed);
        visual.localPosition = basePosition + new Vector3(0f, wave * bobHeight * 0.5f, 0f);
        visual.localRotation = Quaternion.Euler(0f, 0f, wave * rollAngle);
    }
}
