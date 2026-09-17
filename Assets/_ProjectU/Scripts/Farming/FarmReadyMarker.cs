using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class FarmReadyMarker : MonoBehaviour // 수확 가능한 밭 위에서 위아래로 떠다니며 도는 표시
{
    [Tooltip("위아래로 움직이는 높이입니다.")]
    [SerializeField, Min(0f)] private float bobHeight = 0.08f; // 흔들림 높이

    [Tooltip("위아래로 움직이는 속도입니다.")]
    [SerializeField, Min(0f)] private float bobSpeed = 2.2f; // 흔들림 속도

    [Tooltip("초당 회전 각도입니다.")]
    [SerializeField] private float spinSpeed = 90f; // 회전 속도

    private Vector3 baseLocalPosition; // 기준 위치
    private float phase; // 시작 위상

    private void Awake() // 기준 위치 저장
    {
        baseLocalPosition = transform.localPosition; // 기준 위치
        phase = Random.value * Mathf.PI * 2f; // 여러 밭이 같은 박자로 움직이지 않도록 위상 분산
    }

    private void Update() // 흔들림과 회전
    {
        float offset = Mathf.Sin(Time.time * bobSpeed + phase) * bobHeight; // 높이 변화
        transform.localPosition = baseLocalPosition + Vector3.up * offset; // 위치 적용
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.Self); // 회전 적용
    }
}
