using UnityEngine; // Unity 기본 기능
using UnityEngine.Rendering; // 108일차: 지도 카메라에서만 나무 숨기기

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

    [Header("Island Map")] // 108일차: 2km 무인도 전체 지도
    [Tooltip("휠 한 칸에 전체 지도 범위를 곱하거나 나누는 비율 (넓은 섬에서 고르게 확대 · 축소).")]
    [SerializeField, Range(1.02f, 2f)] private float fullMapZoomFactor = 1.2f; // 전체 지도 줌 비율
    [Tooltip("전체 지도를 멀리 볼수록 가운데가 이 위치(섬 가운데)로 옮겨 갑니다.")]
    [SerializeField] private Vector3 fullMapCenter = Vector3.zero; // 섬 가운데
    [Tooltip("지도 카메라의 가장 낮은 높이 (북쪽 산꼭대기보다 높게).")]
    [SerializeField, Min(10f)] private float minimumCameraHeight = 260f; // 지도 카메라 최소 높이
    [Tooltip("지도에서는 Terrain 나무 · 풀을 그리지 않습니다 (숲 바닥 색으로 표시).")]
    [SerializeField] private bool hideTreesOnMap = true; // 지도에서 나무 숨김

    [Header("Map Light")] // 115일차: 지도는 시간 · 날씨와 관계없이 한낮처럼 밝게
    [Tooltip("지도 카메라가 그리는 동안만 햇빛 · 환경광을 한낮 값으로 바꿉니다 (밤 · 폭풍에도 지도가 어둡지 않게).")]
    [SerializeField] private bool fixedMapLight = true; // 지도 전용 햇빛 사용
    [Tooltip("지도용 햇빛 방향 (높이 · 좌우).")]
    [SerializeField] private Vector3 mapSunEuler = new Vector3(55f, -30f, 0f); // 지도 햇빛 방향
    [Tooltip("지도용 햇빛 밝기.")]
    [SerializeField, Min(0f)] private float mapSunIntensity = 1f; // 지도 햇빛 밝기
    [Tooltip("지도용 햇빛 색.")]
    [SerializeField] private Color mapSunColor = new Color(1f, 0.97f, 0.9f, 1f); // 지도 햇빛 색
    [Tooltip("지도용 환경광 색.")]
    [SerializeField] private Color mapAmbientColor = new Color(0.56f, 0.59f, 0.64f, 1f); // 지도 환경광

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
    private Vector3 viewCenter; // 지금 지도 가운데 (월드)
    private Terrain hiddenTerrain; // 지도 카메라가 그리는 동안 나무를 숨긴 Terrain
    private bool fogHidden; // 지도 카메라가 그리는 동안 안개를 껐는지
    private bool savedFog; // 끄기 전 안개
    private Light litSun; // 115일차: 지도 카메라가 그리는 동안 바꾼 햇빛
    private Quaternion savedSunRotation; // 바꾸기 전 햇빛 방향
    private float savedSunIntensity; // 바꾸기 전 햇빛 밝기
    private Color savedSunColor; // 바꾸기 전 햇빛 색
    private bool ambientChanged; // 환경광을 바꿨는지
    private AmbientMode savedAmbientMode; // 바꾸기 전 환경광 방식
    private Color savedAmbientColor; // 바꾸기 전 환경광 색

    public RenderTexture OutputTexture => runtimeRenderTexture; // UI에서 사용할 지도 RenderTexture 제공
    public MapCameraViewMode CurrentViewMode => currentViewMode; // 현재 지도 표시 범위 제공
    public float CurrentFullMapOrthographicSize => currentFullMapOrthographicSize; // 현재 전체 지도 줌 수치 제공
    public bool IsInitialized => initialized; // 지도 카메라 초기화 상태 제공
    public Vector3 ViewCenter => viewCenter; // 지금 지도 가운데 (108일차)
    public float FullMapMaximumSize => fullMapMaximumOrthographicSize; // 전체 지도 가장 넓은 범위 (108일차)

    public Vector2 WorldToViewport(Vector3 world) // 108일차: 월드 위치 → 지도 영상 안 비율 (0 ~ 1, 북쪽 위)
    {
        float size = mapCamera != null ? mapCamera.orthographicSize : 1f;
        return new Vector2((world.x - viewCenter.x) / (size * 2f) + 0.5f, (world.z - viewCenter.z) / (size * 2f) + 0.5f);
    }

    private void OnEnable() // 108일차: 지도 카메라에서만 나무 숨김
    {
        RenderPipelineManager.beginCameraRendering += HandleBeginCamera;
        RenderPipelineManager.endCameraRendering += HandleEndCamera;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= HandleBeginCamera;
        RenderPipelineManager.endCameraRendering -= HandleEndCamera;
        RestoreTrees();
    }

    private void HandleBeginCamera(ScriptableRenderContext context, Camera camera)
    {
        if (camera != mapCamera)
        {
            return;
        }

        if (!fogHidden) // 108일차: 지도는 날씨 · 물속 안개 없이 선명하게
        {
            savedFog = RenderSettings.fog;
            fogHidden = true;
            RenderSettings.fog = false;
        }

        UseMapLight();

        if (!hideTreesOnMap)
        {
            return;
        }

        Terrain terrain = Terrain.activeTerrain;

        if (terrain != null && terrain.drawTreesAndFoliage)
        {
            hiddenTerrain = terrain;
            terrain.drawTreesAndFoliage = false;
        }
    }

    private void HandleEndCamera(ScriptableRenderContext context, Camera camera)
    {
        if (camera == mapCamera)
        {
            RestoreTrees();
        }
    }

    private void UseMapLight() // 115일차: 지도 카메라가 그리는 동안만 한낮 햇빛 · 환경광
    {
        if (!fixedMapLight)
        {
            return;
        }

        Light sun = RenderSettings.sun;

        if (litSun == null && sun != null)
        {
            litSun = sun;
            savedSunRotation = sun.transform.rotation;
            savedSunIntensity = sun.intensity;
            savedSunColor = sun.color;
            sun.transform.rotation = Quaternion.Euler(mapSunEuler);
            sun.intensity = mapSunIntensity;
            sun.color = mapSunColor;
        }

        if (!ambientChanged)
        {
            ambientChanged = true;
            savedAmbientMode = RenderSettings.ambientMode;
            savedAmbientColor = RenderSettings.ambientLight;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = mapAmbientColor;
        }
    }

    private void RestoreTrees()
    {
        if (fogHidden)
        {
            RenderSettings.fog = savedFog;
            fogHidden = false;
        }

        if (litSun != null) // 115일차: 햇빛 · 환경광 되돌림 (게임 화면은 시간 · 날씨 그대로)
        {
            litSun.transform.rotation = savedSunRotation;
            litSun.intensity = savedSunIntensity;
            litSun.color = savedSunColor;
            litSun = null;
        }

        if (ambientChanged)
        {
            RenderSettings.ambientMode = savedAmbientMode;
            RenderSettings.ambientLight = savedAmbientColor;
            ambientChanged = false;
        }

        if (hiddenTerrain != null)
        {
            hiddenTerrain.drawTreesAndFoliage = true;
            hiddenTerrain = null;
        }
    }

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

        currentFullMapOrthographicSize = zoomDirection > 0f
            ? currentFullMapOrthographicSize / fullMapZoomFactor
            : currentFullMapOrthographicSize * fullMapZoomFactor; // 108일차: 넓은 섬은 비율로 줌 (휠 위쪽은 줌인)

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
        Vector3 center = targetPosition; // 지도 가운데

        if (currentViewMode == MapCameraViewMode.FullMap) // 108일차: 멀리 볼수록 섬 가운데로
        {
            float blend = Mathf.InverseLerp(fullMapDefaultOrthographicSize, fullMapMaximumOrthographicSize, currentFullMapOrthographicSize);
            center = Vector3.Lerp(targetPosition, fullMapCenter, blend * blend * (3f - 2f * blend));
        }

        viewCenter = center;
        float height = Mathf.Max(targetPosition.y + cameraHeight, minimumCameraHeight); // 108일차: 산보다 높게
        Vector3 cameraPosition = new Vector3(
            center.x,
            height,
            center.z); // 지도 가운데 바로 위 카메라 위치 계산

        if (mapCamera != null)
        {
            mapCamera.farClipPlane = Mathf.Max(farClipPlane, height + 60f); // 바다 밑까지 보이게
        }

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
