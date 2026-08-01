using System.Collections.Generic; // 컬렉션 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.InputSystem; // 새 Input System 기능

public enum PlayerCameraViewMode // 플레이어 Camera 시점 종류
{
    FirstPerson, // 1인칭 시점
    ThirdPerson // 3인칭 시점
}

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
[RequireComponent(typeof(Camera))] // Camera 컴포넌트 필수 지정
public sealed class ThirdPersonCameraFollow : MonoBehaviour // 1인칭과 3인칭 통합 플레이어 Camera 관리자
{
    [Header("Target")] // 추적 대상 설정 묶음
    [Tooltip("플레이어 루트 Transform입니다.")]
    [SerializeField] private Transform target; // 플레이어 루트 Transform

    [Tooltip("3인칭 Camera가 바라볼 플레이어 기준 높이입니다.")]
    [SerializeField] private float targetHeight = 1f; // 3인칭 시선 중심 높이

    [Tooltip("1인칭 Camera가 위치할 머리 높이 기준 Transform입니다. 비어 있으면 Target 위치와 First Person Height를 사용합니다.")]
    [SerializeField] private Transform firstPersonAnchor; // 1인칭 Camera 기준점

    [Tooltip("First Person Anchor가 없을 때 사용할 플레이어 기준 1인칭 Camera 높이입니다.")]
    [SerializeField] private float firstPersonHeight = 1.65f; // 1인칭 Camera 높이

    [Tooltip("1인칭 Camera 기준점에 추가할 로컬 위치 보정값입니다.")]
    [SerializeField] private Vector3 firstPersonLocalOffset = new Vector3(0f, 0f, 0.05f); // 1인칭 Camera 위치 보정값

    [Header("View Mode")] // 시점 전환 설정 묶음
    [Tooltip("게임 시작 시 사용할 Camera 시점입니다.")]
    [SerializeField] private PlayerCameraViewMode startViewMode = PlayerCameraViewMode.ThirdPerson; // 시작 Camera 시점

    [Tooltip("1인칭과 3인칭 시점을 전환할 키입니다.")]
    [SerializeField] private Key viewToggleKey = Key.V; // 시점 전환 키

    [Tooltip("1인칭과 3인칭 Camera 위치가 자연스럽게 전환되는 보간 시간입니다.")]
    [SerializeField, Min(0.01f)] private float viewTransitionSmoothTime = 0.22f; // 시점 전환 보간 시간

    [Tooltip("1인칭 시점의 Camera Field Of View입니다.")]
    [SerializeField, Range(30f, 120f)] private float firstPersonFieldOfView = 70f; // 1인칭 시야각

    [Tooltip("3인칭 시점의 Camera Field Of View입니다.")]
    [SerializeField, Range(30f, 120f)] private float thirdPersonFieldOfView = 60f; // 3인칭 시야각

    [Tooltip("1인칭 시점의 Camera Near Clipping Plane입니다.")]
    [SerializeField, Min(0.001f)] private float firstPersonNearClipPlane = 0.03f; // 1인칭 Near Clip

    [Tooltip("3인칭 시점의 Camera Near Clipping Plane입니다.")]
    [SerializeField, Min(0.001f)] private float thirdPersonNearClipPlane = 0.1f; // 3인칭 Near Clip

    [Header("First Person Visual")] // 1인칭 플레이어 외형 설정 묶음
    [Tooltip("1인칭에서 가릴 플레이어 외형 루트입니다. Player 루트가 아니라 PlayerVisual 같은 외형 전용 자식을 연결합니다.")]
    [SerializeField] private Transform playerVisualRoot; // 플레이어 외형 루트

    [Tooltip("Camera가 플레이어 몸 안으로 들어가기 직전에 외형 Renderer를 숨길 전환 비율입니다.")]
    [SerializeField, Range(0f, 1f)] private float visualHideBlendThreshold = 0.65f; // 외형 숨김 전환 기준

    [Header("Camera Rotation")] // Camera 회전 설정 묶음
    [Tooltip("게임 시작 시 사용할 상하 Camera 각도입니다.")]
    [SerializeField] private float initialPitch = 20f; // 시작 상하 각도

    [Tooltip("Camera가 아래쪽으로 회전할 수 있는 최소 Pitch입니다.")]
    [SerializeField] private float minimumPitch = -80f; // 최소 상하 각도

    [Tooltip("Camera가 위쪽으로 회전할 수 있는 최대 Pitch입니다.")]
    [SerializeField] private float maximumPitch = 85f; // 최대 상하 각도

    [Header("Third Person Zoom")] // 3인칭 Camera 거리 설정 묶음
    [Tooltip("3인칭 목표 Camera 거리입니다.")]
    [SerializeField] private float distance = 10f; // 3인칭 목표 Camera 거리

    [Tooltip("3인칭 최소 Camera 거리입니다.")]
    [SerializeField] private float minimumDistance = 3f; // 3인칭 최소 Camera 거리

    [Tooltip("3인칭 최대 Camera 거리입니다.")]
    [SerializeField] private float maximumDistance = 10f; // 3인칭 최대 Camera 거리

    [Tooltip("3인칭에서 마우스 휠 한 단계마다 변경할 Camera 거리입니다.")]
    [SerializeField] private float zoomStep = 1.5f; // 3인칭 휠 거리 변화량

    [Tooltip("3인칭 Camera 거리 변화 보간 시간입니다.")]
    [SerializeField] private float zoomSmoothTime = 0.08f; // 3인칭 거리 보간 시간

    [Header("Third Person Collision")] // 3인칭 Camera 충돌 설정 묶음
    [Tooltip("3인칭 Camera 충돌을 검사할 레이어입니다.")]
    [SerializeField] private LayerMask collisionLayerMask; // 3인칭 Camera 충돌 레이어

    [Tooltip("3인칭 Camera 충돌 검사에 사용할 구체 반지름입니다.")]
    [SerializeField] private float collisionRadius = 0.3f; // 충돌 검사 구체 반지름

    [Tooltip("벽과 3인칭 Camera 사이에 유지할 여유 거리입니다.")]
    [SerializeField] private float collisionPadding = 0.15f; // 벽과 Camera 사이 여유 거리

    [Header("Mouse")] // 마우스 설정 묶음
    [Tooltip("1인칭과 3인칭 공통 마우스 회전 감도입니다.")]
    [SerializeField] private float mouseSensitivity = 0.1f; // 공통 마우스 감도

    [Header("Runtime")] // Camera 실행 상태 묶음
    [Tooltip("현재 목표 Camera 시점입니다.")]
    [SerializeField] private PlayerCameraViewMode currentViewMode; // 현재 목표 Camera 시점

    [Tooltip("현재 1인칭 전환 비율입니다. 0은 3인칭이고 1은 1인칭입니다.")]
    [SerializeField, Range(0f, 1f)] private float currentViewBlend; // 현재 시점 전환 비율

    [Tooltip("현재 적용 중인 3인칭 Camera 거리입니다.")]
    [SerializeField] private float currentDistance; // 현재 3인칭 Camera 거리

    [Tooltip("현재 3인칭 Camera가 벽에 가려졌는지 표시합니다.")]
    [SerializeField] private bool isCameraObstructed; // 현재 Camera 차단 여부

    [Tooltip("건축 Camera 등 외부 시스템이 현재 Camera Transform을 제어하는지 표시합니다.")]
    [SerializeField] private bool isExternalCameraControl; // 외부 Camera 제어 상태

    private readonly Dictionary<Renderer, bool> previousRendererStates = new Dictionary<Renderer, bool>(); // 1인칭 전환 전 Renderer 활성 상태
    private Camera controlledCamera; // 현재 제어할 Camera 컴포넌트
    private Renderer[] firstPersonHiddenRenderers = new Renderer[0]; // 1인칭에서 숨길 Renderer 목록
    private float yaw; // 좌우 Camera 회전값
    private float pitch; // 상하 Camera 회전값
    private float distanceSmoothVelocity; // 거리 보간용 변화 속도
    private float viewBlendSmoothVelocity; // 시점 전환 보간용 변화 속도
    private bool runtimeInitialized; // 최초 Camera 상태 초기화 여부
    private bool isPlayerVisualHidden; // 현재 플레이어 외형 숨김 여부

    public float MouseSensitivity => mouseSensitivity; // 현재 마우스 감도 제공
    public PlayerCameraViewMode CurrentViewMode => currentViewMode; // 현재 목표 시점 제공
    public bool IsFirstPerson => currentViewMode == PlayerCameraViewMode.FirstPerson; // 현재 목표가 1인칭인지 제공
    public bool IsExternalCameraControl => isExternalCameraControl; // 외부 Camera 제어 상태 제공
    public bool IsViewTransitioning => Mathf.Abs(currentViewBlend - GetTargetViewBlend()) > 0.01f; // 시점 전환 진행 여부 제공

    private void Awake() // Camera와 플레이어 참조 검사
    {
        controlledCamera = GetComponent<Camera>(); // 같은 오브젝트의 Camera 가져오기
        RefreshFirstPersonRenderers(); // 플레이어 외형 Renderer 목록 구성

        if (target == null) // 추적 대상 확인
        {
            Debug.LogError("카메라의 Target이 연결되지 않았습니다.", this); // 대상 누락 오류
            enabled = false; // Camera 기능 비활성화
        }
    }

    private void OnEnable() // Camera 활성화 처리
    {
        if (target == null || controlledCamera == null) // 필수 참조 확인
        {
            return; // 활성화 처리 중단
        }

        if (!runtimeInitialized) // 최초 Camera 실행 여부 확인
        {
            InitializeRuntimeState(); // 최초 시점과 Camera 상태 초기화
        }

        isExternalCameraControl = false; // 일반 플레이 Camera 제어 상태 적용
        SetCursorLocked(true); // 마우스 커서 잠금
        ApplyCameraTransform(true); // 현재 시점 Camera 위치 즉시 적용
        RefreshPlayerVisualVisibility(); // 현재 시점의 플레이어 외형 표시 적용
    }

    private void OnDisable() // Camera 비활성화 처리
    {
        isExternalCameraControl = false; // 외부 Camera 제어 상태 해제
        RestorePlayerVisualRenderers(); // 플레이어 외형 Renderer 기존 상태 복구
        SetCursorLocked(false); // 마우스 커서 잠금 해제
    }

    private void InitializeRuntimeState() // 최초 Camera 실행 상태 초기화
    {
        yaw = target.eulerAngles.y; // 플레이어 기준 좌우 각도 설정
        pitch = Mathf.Clamp(initialPitch, minimumPitch, maximumPitch); // 시작 상하 각도 설정
        distance = Mathf.Clamp(distance, minimumDistance, maximumDistance); // 시작 거리 범위 제한
        currentDistance = distance; // 현재 3인칭 거리 초기화
        distanceSmoothVelocity = 0f; // 거리 보간 속도 초기화
        currentViewMode = startViewMode; // Inspector 시작 시점 적용
        currentViewBlend = GetTargetViewBlend(); // 시작 시점 전환 비율 즉시 적용
        viewBlendSmoothVelocity = 0f; // 시점 보간 속도 초기화
        isCameraObstructed = false; // Camera 차단 상태 초기화
        runtimeInitialized = true; // 최초 Camera 초기화 완료 기록
    }

    private void Update() // 시점 전환과 3인칭 줌 입력 처리
    {
        if (isExternalCameraControl) // 외부 Camera 제어 상태 확인
        {
            return; // 건축 Camera 사용 중 일반 입력 차단
        }

        HandleViewToggleInput(); // 1인칭과 3인칭 전환 입력 처리

        if (currentViewMode == PlayerCameraViewMode.ThirdPerson) // 현재 목표 3인칭 여부 확인
        {
            HandleZoomInput(); // 3인칭 마우스 휠 줌 처리
        }
    }

    private void HandleViewToggleInput() // 1인칭과 3인칭 전환 키 처리
    {
        if (Cursor.lockState != CursorLockMode.Locked) // 일반 Gameplay 커서 상태 확인
        {
            return; // UI 사용 중 시점 전환 차단
        }

        Keyboard keyboard = Keyboard.current; // 현재 키보드 장치 조회

        if (keyboard == null || viewToggleKey == Key.None) // 키보드와 전환 키 설정 확인
        {
            return; // 시점 전환 입력 처리 중단
        }

        if (!keyboard[viewToggleKey].wasPressedThisFrame) // 시점 전환 키 입력 확인
        {
            return; // 전환 입력 없음
        }

        ToggleViewMode(); // 현재 1인칭과 3인칭 상태 반전
    }

    private void HandleZoomInput() // 3인칭 마우스 휠 줌 입력 처리
    {
        if (Cursor.lockState != CursorLockMode.Locked) // 플레이 커서 잠금 상태 확인
        {
            return; // UI 사용 중 줌 입력 차단
        }

        if (Mouse.current == null) // 마우스 장치 확인
        {
            return; // 마우스 입력 처리 중단
        }

        float scrollAmount = Mouse.current.scroll.ReadValue().y; // 현재 프레임 휠 입력 읽기

        if (Mathf.Abs(scrollAmount) < 0.01f) // 유효한 휠 입력 확인
        {
            return; // 거리 변경 중단
        }

        float zoomDirection = Mathf.Sign(scrollAmount); // 휠 입력 방향 계산
        distance -= zoomDirection * zoomStep; // 목표 Camera 거리 변경
        distance = Mathf.Clamp(distance, minimumDistance, maximumDistance); // 최소·최대 거리 제한
    }

    private void LateUpdate() // 플레이어 이동 후 Camera 처리
    {
        if (target == null || controlledCamera == null || isExternalCameraControl) // 필수 참조와 외부 제어 상태 확인
        {
            return; // 일반 Camera 처리 중단
        }

        HandleLookInput(); // 마우스 Camera 회전 처리
        UpdateViewBlend(); // 1인칭과 3인칭 전환 비율 갱신
        UpdateThirdPersonDistance(); // 3인칭 충돌과 거리 상태 갱신
        ApplyCameraTransform(false); // 현재 시점 비율의 Camera 위치와 회전 적용
        RefreshPlayerVisualVisibility(); // Camera 위치에 맞는 플레이어 외형 표시 갱신
    }

    private void HandleLookInput() // 1인칭과 3인칭 공통 마우스 회전 처리
    {
        if (Cursor.lockState != CursorLockMode.Locked || Mouse.current == null) // 회전 입력 가능 여부 확인
        {
            return; // Camera 회전 입력 생략
        }

        Vector2 mouseDelta = Mouse.current.delta.ReadValue(); // 현재 마우스 이동량 조회
        yaw += mouseDelta.x * mouseSensitivity; // 좌우 회전값 변경
        pitch -= mouseDelta.y * mouseSensitivity; // 상하 회전값 변경
        yaw = Mathf.Repeat(yaw, 360f); // 좌우 회전값 범위 정리
        pitch = Mathf.Clamp(pitch, minimumPitch, maximumPitch); // 상하 회전값 제한
    }

    private void UpdateViewBlend() // 목표 시점까지 자연스러운 전환 비율 갱신
    {
        float targetBlend = GetTargetViewBlend(); // 현재 목표 시점 비율 조회
        currentViewBlend = Mathf.SmoothDamp(
            currentViewBlend,
            targetBlend,
            ref viewBlendSmoothVelocity,
            viewTransitionSmoothTime,
            Mathf.Infinity,
            Time.unscaledDeltaTime); // 시간 정지와 무관한 시점 전환 보간

        if (Mathf.Abs(currentViewBlend - targetBlend) < 0.001f) // 목표 시점 도착 확인
        {
            currentViewBlend = targetBlend; // 미세한 오차 제거
            viewBlendSmoothVelocity = 0f; // 시점 보간 속도 초기화
        }
    }

    private void UpdateThirdPersonDistance() // 3인칭 Camera 충돌 거리 갱신
    {
        Vector3 focusPosition = GetThirdPersonFocusPosition(); // 3인칭 시선 중심 조회
        Quaternion cameraRotation = Quaternion.Euler(pitch, yaw, 0f); // 현재 Camera 회전 계산
        Vector3 backwardDirection = -(cameraRotation * Vector3.forward); // 플레이어 뒤쪽 방향 계산
        float targetCameraDistance = GetCollisionAdjustedDistance(
            focusPosition,
            backwardDirection,
            out bool hasCollision); // 벽 충돌 적용 3인칭 거리 계산

        isCameraObstructed = hasCollision; // Camera 차단 상태 저장

        if (hasCollision && targetCameraDistance < currentDistance) // 벽 진입과 거리 감소 확인
        {
            currentDistance = targetCameraDistance; // Camera를 벽 앞으로 즉시 이동
            distanceSmoothVelocity = 0f; // 기존 거리 보간 속도 제거
            return; // 거리 처리 종료
        }

        currentDistance = Mathf.SmoothDamp(
            currentDistance,
            targetCameraDistance,
            ref distanceSmoothVelocity,
            zoomSmoothTime); // 목표 3인칭 거리까지 부드럽게 이동
    }

    public void ToggleViewMode() // 1인칭과 3인칭 목표 시점 반전
    {
        PlayerCameraViewMode nextViewMode =
            currentViewMode == PlayerCameraViewMode.FirstPerson
                ? PlayerCameraViewMode.ThirdPerson
                : PlayerCameraViewMode.FirstPerson; // 다음 시점 계산

        SetViewMode(nextViewMode, false); // 자연스러운 시점 전환 시작
    }

    public void SetViewMode(
        PlayerCameraViewMode viewMode,
        bool immediate) // 외부 시스템에서 목표 시점 변경
    {
        currentViewMode = viewMode; // 새로운 목표 시점 저장

        if (!immediate) // 자연스러운 전환 여부 확인
        {
            return; // LateUpdate 보간에 전환 위임
        }

        currentViewBlend = GetTargetViewBlend(); // 목표 시점 비율 즉시 적용
        viewBlendSmoothVelocity = 0f; // 기존 시점 보간 속도 제거
        ApplyCameraTransform(true); // 변경된 시점 Camera Transform 즉시 적용
        RefreshPlayerVisualVisibility(); // 변경된 시점 외형 표시 즉시 적용
    }

    public void SetYaw(float targetYaw) // 불러온 좌우 시점 적용
    {
        yaw = Mathf.Repeat(targetYaw, 360f); // 좌우 회전값 범위 제한

        if (!isExternalCameraControl) // 일반 Camera 제어 상태 확인
        {
            ApplyCameraTransform(true); // 현재 시점 Camera 회전과 위치 즉시 적용
        }
    }

    public void SetMouseSensitivity(
        float targetSensitivity) // 외부 설정값으로 마우스 감도 변경
    {
        mouseSensitivity = Mathf.Clamp(
            targetSensitivity,
            GameSettingsService.MinimumMouseSensitivity,
            GameSettingsService.MaximumMouseSensitivity); // 허용 범위 안에서 감도 적용
    }

    public void SetExternalCameraControl(
        bool shouldUseExternalControl) // 자유 건축 Camera 등 외부 Transform 제어 전환
    {
        if (isExternalCameraControl == shouldUseExternalControl) // 기존 외부 제어 상태 확인
        {
            return; // 동일 상태 재적용 생략
        }

        isExternalCameraControl = shouldUseExternalControl; // 새로운 외부 Camera 제어 상태 저장
        distanceSmoothVelocity = 0f; // 기존 거리 보간 속도 제거
        viewBlendSmoothVelocity = 0f; // 기존 시점 보간 속도 제거

        if (isExternalCameraControl) // 외부 Camera 제어 시작 확인
        {
            RestorePlayerVisualRenderers(); // 자유 건축 Camera에서 플레이어 외형 표시
            return; // 현재 1·3인칭 상태 값을 유지한 채 일반 추적 계산 정지
        }

        SetCursorLocked(true); // 일반 플레이 커서 상태 복구
        ApplyCameraTransform(true); // 현재 1·3인칭 상태로 플레이어 추적 위치 복구
        RefreshPlayerVisualVisibility(); // 복구한 시점의 플레이어 외형 적용
    }

    private void ApplyCameraTransform(bool applyImmediately) // 현재 시점 비율의 Camera Transform 적용
    {
        if (target == null || controlledCamera == null) // 필수 참조 확인
        {
            return; // Camera Transform 적용 중단
        }

        Quaternion cameraRotation = Quaternion.Euler(pitch, yaw, 0f); // 현재 공통 Camera 회전 계산
        Vector3 thirdPersonFocusPosition = GetThirdPersonFocusPosition(); // 3인칭 시선 중심 계산
        Vector3 backwardDirection = -(cameraRotation * Vector3.forward); // 3인칭 뒤쪽 방향 계산
        Vector3 thirdPersonPosition =
            thirdPersonFocusPosition + backwardDirection * currentDistance; // 3인칭 Camera 위치 계산

        Vector3 firstPersonPosition = GetFirstPersonPosition(cameraRotation); // 1인칭 Camera 위치 계산
        float blend = Mathf.Clamp01(currentViewBlend); // 유효한 시점 전환 비율 계산
        Vector3 cameraPosition = Vector3.Lerp(
            thirdPersonPosition,
            firstPersonPosition,
            blend); // 3인칭과 1인칭 위치 자연스러운 보간

        transform.SetPositionAndRotation(
            cameraPosition,
            cameraRotation); // 통합 Camera 위치와 회전 적용

        controlledCamera.fieldOfView = Mathf.Lerp(
            thirdPersonFieldOfView,
            firstPersonFieldOfView,
            blend); // 시점 전환 비율에 맞는 FOV 적용

        controlledCamera.nearClipPlane = Mathf.Lerp(
            thirdPersonNearClipPlane,
            firstPersonNearClipPlane,
            blend); // 시점 전환 비율에 맞는 Near Clip 적용

        if (applyImmediately) // 즉시 적용 요청 확인
        {
            distanceSmoothVelocity = 0f; // 거리 보간 속도 제거
        }
    }

    private Vector3 GetThirdPersonFocusPosition() // 3인칭 시선 중심 위치 반환
    {
        return target.position + Vector3.up * targetHeight; // 플레이어 기준 시선 중심 반환
    }

    private Vector3 GetFirstPersonPosition(
        Quaternion cameraRotation) // 1인칭 Camera 위치 반환
    {
        Vector3 anchorPosition = firstPersonAnchor != null
            ? firstPersonAnchor.position
            : target.position + Vector3.up * firstPersonHeight; // Anchor 또는 Player 높이 기준 위치 계산

        Vector3 rotatedOffset =
            cameraRotation * firstPersonLocalOffset; // Camera 방향 기준 위치 보정 계산

        return anchorPosition + rotatedOffset; // 최종 1인칭 Camera 위치 반환
    }

    private float GetTargetViewBlend() // 현재 목표 시점의 전환 비율 반환
    {
        return currentViewMode == PlayerCameraViewMode.FirstPerson
            ? 1f
            : 0f; // 1인칭은 1이고 3인칭은 0 반환
    }

    private float GetCollisionAdjustedDistance(
        Vector3 focusPosition,
        Vector3 backwardDirection,
        out bool hasCollision) // 벽 충돌 적용 3인칭 거리 계산
    {
        hasCollision = Physics.SphereCast(
            focusPosition,
            collisionRadius,
            backwardDirection,
            out RaycastHit collisionHit,
            distance,
            collisionLayerMask,
            QueryTriggerInteraction.Ignore); // 플레이어와 3인칭 Camera 사이 구체 검사

        if (!hasCollision) // 충돌 대상 미검출 확인
        {
            return distance; // 사용자 목표 3인칭 거리 반환
        }

        float safeDistance = collisionHit.distance - collisionPadding; // 벽 앞 안전 거리 계산
        return Mathf.Max(0.1f, safeDistance); // 지나치게 작은 거리 방지
    }

    private void RefreshFirstPersonRenderers() // 1인칭에서 숨길 플레이어 외형 Renderer 구성
    {
        if (playerVisualRoot == null) // 플레이어 외형 루트 존재 확인
        {
            firstPersonHiddenRenderers = new Renderer[0]; // 빈 Renderer 목록 적용
            return; // Renderer 검색 생략
        }

        firstPersonHiddenRenderers =
            playerVisualRoot.GetComponentsInChildren<Renderer>(true); // 외형 루트 아래 전체 Renderer 검색
    }

    private void RefreshPlayerVisualVisibility() // 시점과 전환 비율에 맞는 외형 표시 갱신
    {
        bool shouldHideVisual =
            !isExternalCameraControl
            && currentViewBlend >= visualHideBlendThreshold; // 1인칭 Camera가 몸에 가까워졌는지 계산

        if (shouldHideVisual == isPlayerVisualHidden) // 기존 외형 표시 상태 확인
        {
            return; // 중복 Renderer 변경 생략
        }

        if (shouldHideVisual) // 플레이어 외형 숨김 필요 확인
        {
            HidePlayerVisualRenderers(); // Renderer 기존 상태 저장 후 숨김
            return; // 외형 표시 처리 종료
        }

        RestorePlayerVisualRenderers(); // 3인칭 또는 자유 Camera에서 기존 외형 상태 복구
    }

    private void HidePlayerVisualRenderers() // 플레이어 외형 Renderer 숨김
    {
        previousRendererStates.Clear(); // 이전 Renderer 상태 목록 초기화

        for (int index = 0; index < firstPersonHiddenRenderers.Length; index++) // 전체 외형 Renderer 순회
        {
            Renderer targetRenderer = firstPersonHiddenRenderers[index]; // 현재 Renderer 조회

            if (targetRenderer == null) // Renderer 존재 확인
            {
                continue; // 제거된 Renderer 제외
            }

            previousRendererStates.Add(
                targetRenderer,
                targetRenderer.enabled); // 1인칭 전 Renderer 활성 상태 저장

            targetRenderer.enabled = false; // 1인칭에서 외형 Renderer 숨김
        }

        isPlayerVisualHidden = true; // 외형 숨김 상태 저장
    }

    private void RestorePlayerVisualRenderers() // 플레이어 외형 Renderer 기존 상태 복구
    {
        foreach (KeyValuePair<Renderer, bool> rendererState in previousRendererStates) // 저장된 Renderer 상태 순회
        {
            if (rendererState.Key == null) // Renderer 존재 확인
            {
                continue; // 제거된 Renderer 제외
            }

            rendererState.Key.enabled = rendererState.Value; // 1인칭 전 Renderer 활성 상태 복구
        }

        previousRendererStates.Clear(); // 복구 완료 Renderer 상태 제거
        isPlayerVisualHidden = false; // 외형 숨김 상태 해제
    }

    private void SetCursorLocked(bool isLocked) // 마우스 커서 상태 설정
    {
        Cursor.lockState = isLocked
            ? CursorLockMode.Locked
            : CursorLockMode.None; // 커서 잠금 방식 적용

        Cursor.visible = !isLocked; // 커서 표시 상태 적용
    }

    private void OnValidate() // Inspector Camera 설정값 검증
    {
        targetHeight = Mathf.Max(0f, targetHeight); // 3인칭 시선 높이 음수 방지
        firstPersonHeight = Mathf.Max(0f, firstPersonHeight); // 1인칭 높이 음수 방지
        viewTransitionSmoothTime = Mathf.Max(0.01f, viewTransitionSmoothTime); // 시점 전환 시간 최소값 적용
        firstPersonFieldOfView = Mathf.Clamp(firstPersonFieldOfView, 30f, 120f); // 1인칭 FOV 범위 제한
        thirdPersonFieldOfView = Mathf.Clamp(thirdPersonFieldOfView, 30f, 120f); // 3인칭 FOV 범위 제한
        firstPersonNearClipPlane = Mathf.Max(0.001f, firstPersonNearClipPlane); // 1인칭 Near Clip 최소값 적용
        thirdPersonNearClipPlane = Mathf.Max(0.001f, thirdPersonNearClipPlane); // 3인칭 Near Clip 최소값 적용
        visualHideBlendThreshold = Mathf.Clamp01(visualHideBlendThreshold); // 외형 숨김 전환 기준 제한
        minimumDistance = Mathf.Max(0.5f, minimumDistance); // 최소 3인칭 거리 하한 적용
        maximumDistance = Mathf.Max(minimumDistance, maximumDistance); // 최대 3인칭 거리 역전 방지
        distance = Mathf.Clamp(distance, minimumDistance, maximumDistance); // 목표 3인칭 거리 범위 제한
        zoomStep = Mathf.Max(0.1f, zoomStep); // 줌 변화량 최소값 적용
        zoomSmoothTime = Mathf.Max(0.01f, zoomSmoothTime); // 거리 보간 시간 최소값 적용
        collisionRadius = Mathf.Max(0.01f, collisionRadius); // 충돌 반지름 최소값 적용
        collisionPadding = Mathf.Max(0f, collisionPadding); // 벽 여유 거리 음수 방지
        minimumPitch = Mathf.Clamp(minimumPitch, -89f, 89f); // 최소 상하 각도 제한
        maximumPitch = Mathf.Clamp(maximumPitch, minimumPitch, 89f); // 최대 상하 각도 제한
        initialPitch = Mathf.Clamp(initialPitch, minimumPitch, maximumPitch); // 시작 상하 각도 제한
        mouseSensitivity = Mathf.Clamp(
            mouseSensitivity,
            GameSettingsService.MinimumMouseSensitivity,
            GameSettingsService.MaximumMouseSensitivity); // 마우스 감도 범위 제한

        if (!Application.isPlaying) // Edit Mode 여부 확인
        {
            RefreshFirstPersonRenderers(); // Inspector 외형 루트 변경 내용을 Renderer 목록에 반영
        }
    }
}
