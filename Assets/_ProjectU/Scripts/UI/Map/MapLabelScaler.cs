using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class MapLabelScaler : MonoBehaviour // 108일차: 지도 구역 이름 크기를 지도 범위에 맞춤 (섬 전체를 볼수록 크게)
{
    [Tooltip("이 지도 범위(Orthographic Size)에서 이름 크기가 1입니다.")]
    [SerializeField, Min(1f)] private float referenceSize = 140f; // 기준 범위
    [Tooltip("가장 큰 이름 크기.")]
    [SerializeField, Min(1f)] private float maximumScale = 6f; // 가장 큰 크기

    private MinimapCameraController map; // 지도 카메라
    private Camera mapCamera; // 지도 카메라 Camera
    private float lastScale = -1f; // 마지막 크기

    public float CurrentScale => lastScale; // 지금 크기 (테스트용)

    private void LateUpdate() // 지도 범위가 바뀌면 이름 크기 갱신
    {
        if (mapCamera == null)
        {
            map = map != null ? map : FindFirstObjectByType<MinimapCameraController>();
            mapCamera = map != null ? map.GetComponent<Camera>() : null;

            if (mapCamera == null)
            {
                return;
            }
        }

        float scale = Mathf.Clamp(mapCamera.orthographicSize / referenceSize, 1f, maximumScale);

        if (Mathf.Abs(scale - lastScale) < 0.01f)
        {
            return;
        }

        lastScale = scale;

        foreach (Transform label in transform)
        {
            label.localScale = Vector3.one * scale;
        }
    }
}
