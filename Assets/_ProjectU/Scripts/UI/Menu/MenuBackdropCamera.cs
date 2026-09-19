using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class MenuBackdropCamera : MonoBehaviour // 99일차: 메인 메뉴 배경 마을을 천천히 도는 카메라
{
    [SerializeField] private Vector3 pivot = Vector3.zero; // 바라보는 중심
    [SerializeField, Min(1f)] private float radius = 22f; // 중심에서 거리
    [SerializeField] private float height = 9f; // 높이
    [SerializeField] private float lookHeight = 1.5f; // 바라보는 높이
    [SerializeField] private float degreesPerSecond = 2.5f; // 도는 속도
    [SerializeField] private float startAngle = 200f; // 시작 각도
    [SerializeField] private float bobAmount = 0.35f; // 위아래 흔들림

    private float angle; // 현재 각도

    public float Angle => angle; // 테스트용

    private void OnEnable()
    {
        angle = startAngle;
        Apply();
    }

    private void LateUpdate()
    {
        angle = Mathf.Repeat(angle + degreesPerSecond * Time.unscaledDeltaTime, 360f);
        Apply();
    }

    private void Apply()
    {
        float radians = angle * Mathf.Deg2Rad;
        float bob = Mathf.Sin(Time.unscaledTime * 0.35f) * bobAmount;
        transform.position = pivot + new Vector3(Mathf.Sin(radians) * radius, height + bob, Mathf.Cos(radians) * radius);
        transform.LookAt(pivot + Vector3.up * lookHeight);
    }

#if UNITY_EDITOR
    public void EditorAssign(Vector3 center, float distance, float cameraHeight, float targetHeight, float speed, float angleDegrees) // 생성 도구 전용
    {
        pivot = center;
        radius = distance;
        height = cameraHeight;
        lookHeight = targetHeight;
        degreesPerSecond = speed;
        startAngle = angleDegrees;
        angle = startAngle;
        Apply();
    }
#endif
}
