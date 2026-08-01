using UnityEngine; // Unity 기본 기능

public enum MapCameraViewMode // 지도 카메라 표시 범위
{
    Compact, // 기본 작은 미니맵 범위
    Expanded, // N 키 확장 미니맵 범위
    FullMap // 전체 화면 지도 범위
}

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class MinimapCameraController : MonoBehaviour // 미니맵 전용 카메라와 RenderTexture 관리자
{
    [Header("Camera References")] // 지도 카메라 참조 묶음
    [Tooltip("플레이어를 위에서 내려다보며 지도 영상을 만드는 전용 Camera입니다.")]
    [SerializeField] private Camera mapCamera; // 미니맵 전용 Camera

    [Tooltip("지도 카메라가 중심으로 따라갈 Player Transform입니다.")]
    [SerializeField] private Transform target; // 지도 카메라 추적 대상

    [Header("Camera Position")] // 지도 카메라 위치 설정 묶음
    [Tooltip("플레이어 위치에서 지도 카메라를 위쪽으로 배치할 높이입니다.")]
    [SerializeField, Min(1f)] private float cameraHeight = 80f; // 지도 카메라 높이

    [Tooltip("지도 카메라가 아래를 바라보도록 적용할 월드 회전값입니다.")]
    [SerializeField] private Vector3 cameraEulerAngles = new Vector3(90f, 0f, 0f); // 지도 카메라 회전값

    [Header("Minimap View Sizes")] // 미니맵 표시 범위 묶음
    [Tooltip("기본 작은 미니맵에서 사용할 Orthographic Size입니다.")]
    [SerializeField, Min(1f)] private float compactOrthographicSize = 35f; // 작은 미니맵 표시 범위

    [Tooltip("N 키로 확장한 미니맵에서 사용할 Orthographic Size입니다.")]
    [SerializeField, Min(1f)] private float expandedOrthographicSize = 60f; // 확장 미니맵 표시 범위

    [Header("Full Map Zoom")] // 전체 지도 줌 설정 묶음
    [Tooltip("전체 지도를 처음 열 때 사용할 기본 Orthographic Size입니다.")]
    [SerializeField, Min(1f)] private float fullMapDefaultOrthographicSize = 140f; // 전체 지도 기본 표시 범위

    [Tooltip("휠 줌인으로 접근할 수 있는 가장 작은 Orthographic Size입니다.")]
    [SerializeField, Min(1f)] private float fullMapMinimumOrthographicSize = 45f; // 전체 지도 최대 줌인 범위

    [Tooltip("휠 줌아웃으로 접근할 수 있는 가장 큰 Orthographic Size입니다.")]
    [SerializeField, Min(1f)] private float fullMapMaximumOrthographicSize = 220f; // 전체 지도 최대 줌아웃 범위

    [Tooltip("마우스 휠 한 번에 변경할 Orthographic Size 값입니다.")]
    [SerializeField, Min(0.1f)] private float fullMapZoomStep = 12f; // 전체 지도 휠 줌 변화량

    [Header("Rendering")] // 지도 렌더링 설정 묶음
    [Tooltip("지도 카메라가 표시할 월드 레이어입니다.")]
    [SerializeField] private LayerMask mapLayerMask = ~0; // 지도 카메라 렌더링 레이어

    [Tooltip("지도 카메라가 월드 밖을 표시할 때 사용할 배경색입니다.")]
    [SerializeField] private Color backgroundColor = new Color(0.035f, 0.05f, 0.04f, 1f); // 지도 배경색

    [Tooltip("런타임에 생성할 정사각형 RenderTexture의 가로와 세로 해상도입니다.")]
    [SerializeField, Range(256, 2048)] private int textureSize = 1024; // 지도 RenderTexture 해상도

    [Tooltip("지도 RenderTexture의 Depth Buffer 비트 수입니다.")]
    [SerializeField] private int depthBufferBits = 16; // 지도 RenderTexture 깊이 버퍼

    [Tooltip("지도 카메라의 Near Clipping Plane 값입니다.")]
    [SerializeField, Min(0.01f)] private float nearClipPlane = 0.1f; // 지도 카메라 최소 렌더링 거리

    [Tooltip("지도 카메라의 Far Clipping Plane 값입니다.")]
    [SerializeField, Min(10f)] private float farClipPlane = 500f; // 지도 카메라 최대 렌더링 거리

    private RenderTexture runtimeRenderTexture; // 런타임 생성 지도 RenderTexture
    private MapCameraViewMode currentViewMode = MapCameraViewMode.Compact; // 현재 지도 카메라 범위
    private float currentFullMapOrthographicSize; // 현재 전체 지도 휠 줌 범위
    private bool initialized; // 지도 카메라 초기화 완료 여부

    public RenderTexture OutputTexture => runtimeRenderTexture; // UI에서 사용할 지도 RenderTexture 제공
    public MapCameraViewMode CurrentViewMode => currentViewMode; // 현재 지도 표시 범위 제공
    public float CurrentFullMapOrthographicSize => currentFullMapOrthographicSize; // 현재 전체 지도 줌 수치 제공
    public bool IsInitialized => initialized; // 지도 카메라 초기화 상태 제공

    private void Awake() // 지도 카메라 기본 참조 검사
    {
        if (mapCamera == null) // 전용 Camera 참조 확인
        {
            mapCamera = GetComponent<Camera>(); // 같은 오브젝트의 Camera 자동 검색
        }
    }

    public bool Initialize(Transform followTarget) // 플레이어와 지도 RenderTexture 초기화
    {
        if (mapCamera == null) // Awake 실행 순서와 관계없이 Camera 참조 확인
        {
            mapCamera = GetComponent<Camera>(); // 같은 오브젝트의 Camera 자동 검색
        }

        if (followTarget != null) // 외부 플레이어 참조 존재 확인
        {
            target = followTarget; // 지도 카메라 추적 대상 저장
        }

        if (mapCamera == null || target == null) // 필수 Camera와 Player 참조 확인
        {
            Debug.LogError(
                $"{gameObject.name}의 MinimapCameraController에 Map Camera와 Target을 연결해야 합니다.",
                this); // 지도 카메라 참조 오류 출력

            initialized = false; // 초기화 실패 상태 저장
            return false; // 초기화 실패 반환
        }

        currentFullMapOrthographicSize =
            Mathf.Clamp(
                fullMapDefaultOrthographicSize,
                fullMapMinimumOrthographicSize,
                fullMapMaximumOrthographicSize); // 전체 지도 시작 줌 수치 초기화

        CreateRuntimeRenderTexture(); // 지도 출력 RenderTexture 생성
        ConfigureMapCamera(); // 전용 Camera 렌더링 설정
        SetViewMode(MapCameraViewMode.Compact); // 시작 작은 미니맵 범위 적용
        SnapToTarget(); // 플레이어 위로 카메라 즉시 이동
        initialized = true; // 초기화 완료 상태 저장
        return true; // 초기화 성공 반환
    }

    public void SetViewMode(MapCameraViewMode viewMode) // 지도 표시 범위 변경
    {
        currentViewMode = viewMode; // 현재 지도 표시 모드 저장

        if (mapCamera == null) // 전용 Camera 존재 확인
        {
            return; // 카메라 범위 적용 생략
        }

        mapCamera.orthographicSize = GetOrthographicSize(viewMode); // 현재 모드의 지도 범위 적용
    }

    public void ZoomFullMap(float scrollDelta) // 전체 지도 마우스 휠 줌인과 줌아웃
    {
        if (!initialized
            || mapCamera == null
            || currentViewMode != MapCameraViewMode.FullMap
            || Mathf.Abs(scrollDelta) < 0.01f) // 초기화와 전체 지도 상태 및 유효 입력 확인
        {
            return; // 전체 지도 줌 처리 생략
        }

        float zoomDirection = Mathf.Sign(scrollDelta); // 휠 위쪽과 아래쪽 방향 계산

        currentFullMapOrthographicSize -=
            zoomDirection * fullMapZoomStep; // 휠 위쪽은 줌인하고 아래쪽은 줌아웃

        currentFullMapOrthographicSize =
            Mathf.Clamp(
                currentFullMapOrthographicSize,
                fullMapMinimumOrthographicSize,
                fullMapMaximumOrthographicSize); // 전체 지도 줌 범위 제한

        mapCamera.orthographicSize =
            currentFullMapOrthographicSize; // 변경된 전체 지도 범위 즉시 적용
    }

    public void ResetFullMapZoom() // 전체 지도 줌을 Inspector 기본값으로 복구
    {
        currentFullMapOrthographicSize =
            Mathf.Clamp(
                fullMapDefaultOrthographicSize,
                fullMapMinimumOrthographicSize,
                fullMapMaximumOrthographicSize); // 전체 지도 기본 줌 범위 복구

        if (mapCamera != null
            && currentViewMode == MapCameraViewMode.FullMap) // 현재 전체 지도 표시 여부 확인
        {
            mapCamera.orthographicSize =
                currentFullMapOrthographicSize; // 전체 지도 Camera에 기본 범위 즉시 적용
        }
    }

    private float GetOrthographicSize(MapCameraViewMode viewMode) // 모드별 Orthographic Size 반환
    {
        switch (viewMode) // 지도 표시 모드 분기
        {
            case MapCameraViewMode.Expanded: // 확장 미니맵 모드
                return expandedOrthographicSize; // 확장 범위 반환

            case MapCameraViewMode.FullMap: // 전체 화면 지도 모드
                return currentFullMapOrthographicSize; // 현재 휠 줌 범위 반환

            default: // 기본 작은 미니맵 모드
                return compactOrthographicSize; // 기본 범위 반환
        }
    }

    private void LateUpdate() // 플레이어 이동 이후 지도 카메라 위치 갱신
    {
        if (!initialized || target == null) // 초기화와 추적 대상 확인
        {
            return; // 지도 카메라 추적 생략
        }

        SnapToTarget(); // 플레이어 중심 지도 카메라 위치 갱신
    }

    private void SnapToTarget() // 플레이어 위 지도 카메라 위치와 회전 적용
    {
        Vector3 targetPosition = target.position; // 현재 플레이어 위치 조회
        Vector3 cameraPosition = new Vector3(
            targetPosition.x,
            targetPosition.y + cameraHeight,
            targetPosition.z); // 플레이어 바로 위 카메라 위치 계산

        transform.SetPositionAndRotation(
            cameraPosition,
            Quaternion.Euler(cameraEulerAngles)); // 지도 카메라 위치와 하향 회전 적용
    }

    private void CreateRuntimeRenderTexture() // 런타임 지도 RenderTexture 생성
    {
        ReleaseRuntimeRenderTexture(); // 기존 런타임 RenderTexture 정리

        int validTextureSize = Mathf.Clamp(textureSize, 256, 2048); // 지원 범위 안에서 해상도 제한
        int validDepthBufferBits = depthBufferBits <= 0 ? 0 : 16; // 깊이 버퍼 값을 0 또는 16으로 정리

        runtimeRenderTexture = new RenderTexture(
            validTextureSize,
            validTextureSize,
            validDepthBufferBits,
            RenderTextureFormat.ARGB32); // 정사각형 지도 RenderTexture 생성

        runtimeRenderTexture.name = "RT_Runtime_Minimap"; // 런타임 RenderTexture 이름 설정
        runtimeRenderTexture.filterMode = FilterMode.Bilinear; // 지도 화면 확대 시 부드러운 필터 적용
        runtimeRenderTexture.wrapMode = TextureWrapMode.Clamp; // 지도 가장자리 반복 표시 방지
        runtimeRenderTexture.useMipMap = false; // 불필요한 Mip Map 생성 방지
        runtimeRenderTexture.autoGenerateMips = false; // 자동 Mip Map 갱신 방지
        runtimeRenderTexture.Create(); // GPU 지도 RenderTexture 생성
    }

    private void ConfigureMapCamera() // 미니맵 Camera 렌더링 설정
    {
        mapCamera.orthographic = true; // 원근감 없는 정사영 카메라 적용
        mapCamera.clearFlags = CameraClearFlags.SolidColor; // 단색 지도 배경 적용
        mapCamera.backgroundColor = backgroundColor; // 지도 배경색 적용
        mapCamera.cullingMask = mapLayerMask.value; // 지도에 표시할 레이어 적용
        mapCamera.nearClipPlane = nearClipPlane; // Near Clipping Plane 적용
        mapCamera.farClipPlane = Mathf.Max(farClipPlane, cameraHeight + 10f); // 카메라 높이를 포함하는 Far Plane 적용
        mapCamera.allowHDR = false; // 지도에 불필요한 HDR 비활성화
        mapCamera.allowMSAA = false; // RenderTexture MSAA 비활성화
        mapCamera.useOcclusionCulling = false; // 위쪽 지도 시점의 오클루전 누락 방지
        mapCamera.targetTexture = runtimeRenderTexture; // 지도 출력 RenderTexture 연결
        mapCamera.enabled = true; // 지도 Camera 렌더링 활성화
    }

    private void ReleaseRuntimeRenderTexture() // 런타임 RenderTexture 정리
    {
        if (runtimeRenderTexture == null) // 생성된 RenderTexture 존재 확인
        {
            return; // 정리할 RenderTexture 없음
        }

        if (mapCamera != null && mapCamera.targetTexture == runtimeRenderTexture) // Camera 출력 연결 확인
        {
            mapCamera.targetTexture = null; // Camera와 RenderTexture 연결 해제
        }

        if (runtimeRenderTexture.IsCreated()) // GPU RenderTexture 생성 여부 확인
        {
            runtimeRenderTexture.Release(); // GPU RenderTexture 자원 해제
        }

        Destroy(runtimeRenderTexture); // 런타임 RenderTexture 오브젝트 제거
        runtimeRenderTexture = null; // RenderTexture 참조 초기화
    }

    private void OnDestroy() // 지도 카메라 제거 정리
    {
        ReleaseRuntimeRenderTexture(); // 런타임 RenderTexture 자원 해제
    }

    private void OnValidate() // Inspector 지도 카메라 값 검증
    {
        cameraHeight = Mathf.Max(1f, cameraHeight); // 카메라 높이 최소값 적용
        compactOrthographicSize = Mathf.Max(1f, compactOrthographicSize); // 작은 미니맵 범위 최소값 적용
        expandedOrthographicSize = Mathf.Max(compactOrthographicSize, expandedOrthographicSize); // 확장 범위 역전 방지
        fullMapMinimumOrthographicSize = Mathf.Max(1f, fullMapMinimumOrthographicSize); // 전체 지도 최소 범위 제한
        fullMapMaximumOrthographicSize = Mathf.Max(
            fullMapMinimumOrthographicSize,
            fullMapMaximumOrthographicSize); // 전체 지도 최대 범위 역전 방지

        fullMapDefaultOrthographicSize =
            Mathf.Clamp(
                fullMapDefaultOrthographicSize,
                fullMapMinimumOrthographicSize,
                fullMapMaximumOrthographicSize); // 전체 지도 기본 범위 제한

        fullMapZoomStep = Mathf.Max(0.1f, fullMapZoomStep); // 휠 줌 변화량 최소값 적용
        textureSize = Mathf.Clamp(textureSize, 256, 2048); // RenderTexture 해상도 범위 제한
        depthBufferBits = depthBufferBits <= 0 ? 0 : 16; // 깊이 버퍼 값 정리
        nearClipPlane = Mathf.Max(0.01f, nearClipPlane); // Near Plane 최소값 적용
        farClipPlane = Mathf.Max(10f, farClipPlane); // Far Plane 최소값 적용
    }
}
