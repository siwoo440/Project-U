using UnityEngine; // Unity 기본 기능
using UnityEngine.InputSystem; // 새 Input System 기능

public sealed class ThirdPersonCameraFollow : MonoBehaviour // 마우스 회전식 추적 카메라
{
    [Header("Target")] // 추적 대상 설정
    [SerializeField] private Transform target; // 플레이어 Transform
    [SerializeField] private float targetHeight = 1f; // 시선 중심 높이

    [Header("Camera")] // 카메라 위치 설정
    [SerializeField] private float initialPitch = 30f; // 시작 상하 각도
    [SerializeField] private float minimumPitch = -20f; // 최소 상하 각도
    [SerializeField] private float maximumPitch = 65f; // 최대 상하 각도

    [Header("Zoom")] // 카메라 줌 설정
    [SerializeField] private float distance = 10f; // 목표 카메라 거리
    [SerializeField] private float minimumDistance = 3f; // 최소 카메라 거리
    [SerializeField] private float maximumDistance = 10f; // 최대 카메라 거리
    [SerializeField] private float zoomStep = 1.5f; // 휠 한 단계 거리 변화량
    [SerializeField] private float zoomSmoothTime = 0.08f; // 거리 변화 보간 시간

    [Header("Collision")] // 카메라 충돌 설정
    [SerializeField] private LayerMask collisionLayerMask; // 카메라 충돌 대상 레이어
    [SerializeField] private float collisionRadius = 0.3f; // 충돌 검사 구체 반지름
    [SerializeField] private float collisionPadding = 0.15f; // 벽과 카메라 사이 여유 거리

    [Header("Runtime")] // 카메라 실행 상태
    [SerializeField] private float currentDistance; // 현재 적용 카메라 거리
    [SerializeField] private bool isCameraObstructed; // 현재 카메라 차단 여부

    [Header("Mouse")] // 마우스 설정
    [SerializeField] private float mouseSensitivity = 0.1f; // 마우스 감도

    private float yaw; // 좌우 회전값
    private float pitch; // 상하 회전값
    private float distanceSmoothVelocity; // 거리 보간용 변화 속도

    private void Awake() // 카메라 참조 검사
    {
        if (target == null) // 추적 대상 확인
        {
            Debug.LogError("카메라의 Target이 연결되지 않았습니다.", this); // 대상 누락 오류
            enabled = false; // 카메라 기능 비활성화
        }
    }

    private void OnEnable() // 카메라 활성화 처리
    {
        if (target == null) // 추적 대상 확인
        {
            return; // 활성화 처리 중단
        }

        yaw = target.eulerAngles.y; // 플레이어 기준 좌우 각도 설정
        pitch = Mathf.Clamp(initialPitch, minimumPitch, maximumPitch); // 시작 상하 각도 설정
        distance = Mathf.Clamp(distance, minimumDistance, maximumDistance); // 시작 거리 범위 제한
        currentDistance = distance; // 현재 거리 초기화
        distanceSmoothVelocity = 0f; // 거리 보간 속도 초기화
        isCameraObstructed = false; // 카메라 차단 상태 초기화
        SetCursorLocked(true); // 마우스 커서 잠금
    }

    private void OnDisable() // 카메라 비활성화 처리
    {
        SetCursorLocked(false); // 마우스 커서 잠금 해제
    }

    private void Update() // 커서와 줌 입력 처리
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) // Escape 입력 확인
        {
            bool shouldLock = Cursor.lockState != CursorLockMode.Locked; // 다음 잠금 상태 계산
            SetCursorLocked(shouldLock); // 커서 상태 변경
        }

        HandleZoomInput(); // 마우스 휠 줌 입력 처리
    }
    private void HandleZoomInput() // 마우스 휠 줌 입력 처리
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
        distance -= zoomDirection * zoomStep; // 목표 카메라 거리 변경
        distance = Mathf.Clamp(distance, minimumDistance, maximumDistance); // 최소·최대 거리 제한
    }

    private void LateUpdate() // 플레이어 이동 후 카메라 처리
    {
        if (target == null) // 추적 대상 확인
        {
            return; // 카메라 처리 중단
        }

        if (Cursor.lockState == CursorLockMode.Locked && Mouse.current != null) // 마우스 회전 가능 여부 확인
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue(); // 마우스 이동량 읽기
            yaw += mouseDelta.x * mouseSensitivity; // 좌우 각도 변경
            pitch -= mouseDelta.y * mouseSensitivity; // 상하 각도 변경
            yaw = Mathf.Repeat(yaw, 360f); // 좌우 각도 범위 정리
            pitch = Mathf.Clamp(pitch, minimumPitch, maximumPitch); // 상하 각도 제한
        }

        Vector3 focusPosition = target.position + Vector3.up * targetHeight; // 카메라 시선 중심 계산
        Quaternion cameraRotation = Quaternion.Euler(pitch, yaw, 0f); // Z축 기울기 없는 회전 계산
        Vector3 backwardDirection = -(cameraRotation * Vector3.forward); // 플레이어 뒤쪽 방향 계산
        float targetCameraDistance = GetCollisionAdjustedDistance(focusPosition, backwardDirection, out bool hasCollision); // 충돌 적용 거리 계산

        isCameraObstructed = hasCollision; // 카메라 차단 상태 저장

        if (hasCollision && targetCameraDistance < currentDistance) // 벽 진입과 거리 감소 확인
        {
            currentDistance = targetCameraDistance; // 카메라를 벽 앞으로 즉시 이동
            distanceSmoothVelocity = 0f; // 기존 거리 보간 속도 제거
        }
        else // 일반 줌 또는 벽 이탈 상태
        {
            currentDistance = Mathf.SmoothDamp(currentDistance, targetCameraDistance, ref distanceSmoothVelocity, zoomSmoothTime); // 목표 거리까지 부드럽게 이동
        }

        Vector3 cameraPosition = focusPosition + backwardDirection * currentDistance; // 충돌 거리 기반 카메라 위치 계산
        transform.SetPositionAndRotation(cameraPosition, cameraRotation); // 위치와 회전 동시 적용
    }
    private float GetCollisionAdjustedDistance(Vector3 focusPosition, Vector3 backwardDirection, out bool hasCollision) // 벽 충돌 적용 거리 계산
    {
        hasCollision = Physics.SphereCast(focusPosition, collisionRadius, backwardDirection, out RaycastHit collisionHit, distance, collisionLayerMask, QueryTriggerInteraction.Ignore); // 플레이어와 카메라 사이 구체 검사

        if (!hasCollision) // 충돌 대상 미검출 확인
        {
            return distance; // 사용자 목표 거리 반환
        }

        float safeDistance = collisionHit.distance - collisionPadding; // 벽 앞 안전 거리 계산
        return Mathf.Max(0.1f, safeDistance); // 지나치게 작은 거리 방지
    }
    private void SetCursorLocked(bool isLocked) // 마우스 커서 상태 설정
    {
        Cursor.lockState = isLocked ? CursorLockMode.Locked : CursorLockMode.None; // 커서 잠금 방식 적용
        Cursor.visible = !isLocked; // 커서 표시 상태 적용
    }
    private void OnValidate() // Inspector 카메라 설정값 검증
    {
        minimumDistance = Mathf.Max(0.5f, minimumDistance); // 최소 거리 하한 적용
        maximumDistance = Mathf.Max(minimumDistance, maximumDistance); // 최대 거리 역전 방지
        distance = Mathf.Clamp(distance, minimumDistance, maximumDistance); // 목표 거리 범위 제한
        zoomStep = Mathf.Max(0.1f, zoomStep); // 줌 변화량 최소값 적용
        zoomSmoothTime = Mathf.Max(0.01f, zoomSmoothTime); // 보간 시간 최소값 적용
        collisionRadius = Mathf.Max(0.01f, collisionRadius); // 충돌 반지름 최소값 적용
        collisionPadding = Mathf.Max(0f, collisionPadding); // 벽 여유 거리 음수 방지
        minimumPitch = Mathf.Clamp(minimumPitch, -89f, 89f); // 최소 상하 각도 제한
        maximumPitch = Mathf.Clamp(maximumPitch, minimumPitch, 89f); // 최대 상하 각도 제한
        initialPitch = Mathf.Clamp(initialPitch, minimumPitch, maximumPitch); // 시작 상하 각도 제한
    }
}