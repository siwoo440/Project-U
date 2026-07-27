using UnityEngine; // Unity 기본 기능
using UnityEngine.InputSystem; // 새 Input System 기능

[RequireComponent(typeof(CharacterController))] // 필수 이동 충돌 컴포넌트
[RequireComponent(typeof(PlayerStamina))] // 필수 스태미나 컴포넌트
public sealed class PlayerMovement : MonoBehaviour // 플레이어 이동 처리
{
    [Header("References")] // 외부 참조 묶음
    [SerializeField] private Transform cameraTransform; // 이동 기준 카메라

    [Header("Movement")] // 이동 설정 묶음
    [SerializeField] private float walkSpeed = 4f; // 걷기 속도
    [SerializeField] private float runSpeed = 7f; // 달리기 속도
    [SerializeField] private float jumpHeight = 1.2f; // 점프 높이
    [SerializeField] private float gravity = -20f; // 중력 가속도
   
    [Header("Ground Detection")] // 지면 판정 설정 묶음
    [SerializeField] private LayerMask groundLayerMask = ~0; // 지면 검사 대상 레이어
    [SerializeField] private float groundCheckDistance = 0.15f; // 발밑 지면 검사 거리
    [SerializeField] private float groundProbeRadiusOffset = 0.05f; // 검사 구체 반지름 감소값
    [SerializeField] private float groundedVerticalVelocity = -2f; // 지면 밀착용 하강 속도
    [SerializeField] private float minimumFallDistance = 1.5f; // 유효 낙하 최소 거리

    [Header("Ground Runtime")] // 지면 실행 상태 묶음
    [SerializeField] private bool isGrounded; // 현재 접지 상태
    [SerializeField] private float currentSlopeAngle; // 현재 지면 경사 각도
    [SerializeField] private bool isFalling; // 현재 낙하 상태
    [SerializeField] private float lastFallDistance; // 마지막 낙하 거리
    [SerializeField] private bool wasSignificantFall; // 유효 낙하 발생 여부

    [Header("Input Actions")] // 입력 설정 묶음
    [SerializeField] private InputActionReference moveActionReference; // 이동 액션 참조
    [SerializeField] private InputActionReference sprintActionReference; // 달리기 액션 참조
    [SerializeField] private InputActionReference jumpActionReference; // 점프 액션 참조

    private CharacterController characterController; // 캐릭터 충돌 이동기
    private PlayerStamina playerStamina; // 플레이어 스태미나 관리기
    private float verticalVelocity; // 수직 이동 속도
    private Vector3 groundNormal = Vector3.up; // 현재 지면의 수직 방향
    private float fallStartHeight; // 낙하 시작 높이

    public bool IsGrounded => isGrounded; // 현재 접지 상태 공개
    public bool IsFalling => isFalling; // 현재 낙하 상태 공개
    public float CurrentSlopeAngle => currentSlopeAngle; // 현재 경사 각도 공개
    public float LastFallDistance => lastFallDistance; // 마지막 낙하 거리 공개
    public bool WasSignificantFall => wasSignificantFall; // 유효 낙하 여부 공개

    private void Awake() // 이동 컴포넌트 초기화
    {
        characterController = GetComponent<CharacterController>(); // CharacterController 가져오기
        playerStamina = GetComponent<PlayerStamina>(); // PlayerStamina 가져오기

        if (cameraTransform == null || moveActionReference == null || sprintActionReference == null || jumpActionReference == null) // 필수 참조 연결 확인
        {
            Debug.LogError("Main Camera와 Move, Sprint, Jump Input Action을 연결해야 합니다.", this); // 참조 누락 오류
            enabled = false; // 이동 스크립트 비활성화
            return; // 초기화 처리 중단
        }

        if (playerStamina == null) // 스태미나 컴포넌트 확인
        {
            Debug.LogError("PlayerStamina 컴포넌트가 필요합니다.", this); // 스태미나 누락 오류
            enabled = false; // 이동 스크립트 비활성화
        }
    }

    private void OnEnable() // 입력 활성화
    {
        if (moveActionReference == null || sprintActionReference == null || jumpActionReference == null) // 입력 연결 확인
        {
            return; // 활성화 중단
        }

        moveActionReference.action.Enable(); // 이동 액션 활성화
        sprintActionReference.action.Enable(); // 달리기 액션 활성화
        jumpActionReference.action.Enable(); // 점프 액션 활성화
    }

    private void OnDisable() // 입력 비활성화
    {
        if (moveActionReference != null) // 이동 액션 존재 확인
        {
            moveActionReference.action.Disable(); // 이동 액션 비활성화
        }

        if (sprintActionReference != null) // 달리기 액션 존재 확인
        {
            sprintActionReference.action.Disable(); // 달리기 액션 비활성화
        }

        if (jumpActionReference != null) // 점프 액션 존재 확인
        {
            jumpActionReference.action.Disable(); // 점프 액션 비활성화
        }
    }

    private void Update() // 매 프레임 이동 처리
    {
        Vector2 moveInput = moveActionReference.action.ReadValue<Vector2>(); // 이동 입력 읽기

        Vector3 cameraForward = cameraTransform.forward; // 카메라 전방 방향 가져오기
        cameraForward.y = 0f; // 상하 방향 제거
        cameraForward.Normalize(); // 전방 방향 크기 정규화

        Vector3 cameraRight = cameraTransform.right; // 카메라 오른쪽 방향 가져오기
        cameraRight.y = 0f; // 상하 방향 제거
        cameraRight.Normalize(); // 오른쪽 방향 크기 정규화



        Vector3 moveDirection = cameraForward * moveInput.y + cameraRight * moveInput.x; // 카메라 기준 이동 방향 계산
        moveDirection = Vector3.ClampMagnitude(moveDirection, 1f); // 대각선 이동 속도 제한

        bool wasGrounded = isGrounded; // 이전 프레임 접지 상태 저장
        UpdateGroundState(); // 현재 지면 상태 검사
        UpdateLandingState(wasGrounded); // 착지와 낙하 거리 판정

        Vector3 adjustedMoveDirection = GetSlopeAdjustedDirection(moveDirection); // 경사면 기준 이동 방향 계산

        bool hasMovementInput = moveDirection.sqrMagnitude > 0.01f; // 실제 이동 입력 존재 확인
        bool wantsToSprint = sprintActionReference.action.IsPressed() && hasMovementInput; // 이동 중 달리기 입력 확인
        bool isSprinting = playerStamina.UpdateSprint(wantsToSprint, Time.deltaTime); // 스태미나 기반 달리기 판정
        float currentSpeed = isSprinting ? runSpeed : walkSpeed; // 현재 이동 속도 결정

        UpdateVerticalVelocity(); // 점프와 중력 계산
        UpdateFallingState(); // 공중 하강 상태 검사

        Vector3 horizontalMovement = adjustedMoveDirection * currentSpeed; // 경사면 적용 수평 이동량 계산




        Vector3 verticalMovement = Vector3.up * verticalVelocity; // 수직 이동량 계산
        Vector3 finalMovement = horizontalMovement + verticalMovement; // 최종 이동량 결합

        FaceCameraDirection(cameraForward); // 카메라 시선 방향 적용
        characterController.Move(finalMovement * Time.deltaTime); // 충돌을 적용한 이동 실행
    }

    private void UpdateVerticalVelocity() // 점프와 중력 계산
    {
        if (isGrounded && verticalVelocity < 0f) // 지면의 하강 상태 확인
        {
            verticalVelocity = groundedVerticalVelocity; // 지면 밀착 속도 적용
        }

        if (isGrounded && jumpActionReference.action.WasPressedThisFrame()) // 지면 점프 입력 확인
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity); // 점프 초기 속도 계산
            isGrounded = false; // 점프 시작 접지 해제
        }

        verticalVelocity += gravity * Time.deltaTime; // 중력 누적
    }

    private void UpdateGroundState() // 발밑 지면과 경사 상태 검사
    {
        Vector3 worldCenter = transform.TransformPoint(characterController.center); // CharacterController 중심 위치 계산
        float capsuleHalfHeight = Mathf.Max(characterController.height * 0.5f, characterController.radius); // 캡슐 절반 높이 계산
        float bottomSphereOffset = capsuleHalfHeight - characterController.radius; // 아래쪽 구체 중심 거리 계산
        Vector3 castOrigin = worldCenter - Vector3.up * bottomSphereOffset; // 발밑 검사 시작 위치 계산
        float probeRadius = Mathf.Max(0.01f, characterController.radius - groundProbeRadiusOffset); // 검사 구체 반지름 계산

        bool hasGroundHit = Physics.SphereCast(castOrigin, probeRadius, Vector3.down, out RaycastHit groundHit, groundCheckDistance, groundLayerMask, QueryTriggerInteraction.Ignore); // 발밑 지면 구체 검사

        if (!hasGroundHit) // 지면 미검출 확인
        {
            isGrounded = false; // 공중 상태 적용
            groundNormal = Vector3.up; // 지면 방향 초기화
            currentSlopeAngle = 0f; // 경사 각도 초기화
            return; // 지면 처리 중단
        }

        groundNormal = groundHit.normal; // 충돌 지면 방향 저장
        currentSlopeAngle = Vector3.Angle(groundNormal, Vector3.up); // 지면 경사 각도 계산

        bool isWalkableSlope = currentSlopeAngle <= characterController.slopeLimit; // 이동 가능한 경사 확인
        bool isDescending = verticalVelocity <= 0f; // 상승 상태 종료 확인
        isGrounded = isWalkableSlope && isDescending; // 최종 접지 상태 적용
    }

    private Vector3 GetSlopeAdjustedDirection(Vector3 moveDirection) // 경사면 기준 이동 방향 계산
    {
        if (!isGrounded) // 공중 상태 확인
        {
            return moveDirection; // 기존 이동 방향 반환
        }

        if (moveDirection.sqrMagnitude < 0.001f) // 이동 입력 크기 확인
        {
            return Vector3.zero; // 이동하지 않는 방향 반환
        }

        Vector3 slopeDirection = Vector3.ProjectOnPlane(moveDirection, groundNormal); // 지면을 따라가는 방향 계산

        if (slopeDirection.sqrMagnitude < 0.001f) // 계산된 방향 크기 확인
        {
            return Vector3.zero; // 유효하지 않은 이동 차단
        }

        return slopeDirection.normalized * moveDirection.magnitude; // 입력 크기를 유지한 경사 방향 반환
    }

    private void UpdateFallingState() // 공중 하강과 낙하 시작 판정
    {
        if (isGrounded) // 접지 상태 확인
        {
            return; // 낙하 시작 차단
        }

        if (verticalVelocity >= 0f) // 상승 상태 확인
        {
            return; // 하강 전 처리 중단
        }

        if (isFalling) // 기존 낙하 상태 확인
        {
            return; // 낙하 시작 높이 유지
        }

        isFalling = true; // 낙하 상태 적용
        fallStartHeight = transform.position.y; // 낙하 시작 높이 저장
        wasSignificantFall = false; // 이전 유효 낙하 결과 초기화
    }

    private void UpdateLandingState(bool wasGrounded) // 착지와 낙하 거리 판정
    {
        if (wasGrounded) // 이전 프레임 접지 상태 확인
        {
            return; // 계속 접지 중인 상태 제외
        }

        if (!isGrounded) // 현재 공중 상태 확인
        {
            return; // 착지 처리 대기
        }

        if (!isFalling) // 낙하 상태 확인
        {
            return; // 일반 접지 처리 제외
        }

        lastFallDistance = Mathf.Max(0f, fallStartHeight - transform.position.y); // 실제 낙하 거리 계산
        wasSignificantFall = lastFallDistance >= minimumFallDistance; // 유효 낙하 기준 비교
        isFalling = false; // 낙하 상태 종료
    }


    private void FaceCameraDirection(Vector3 cameraForward) // 카메라 방향으로 플레이어 회전
    {
        if (cameraForward.sqrMagnitude < 0.001f) // 유효한 방향 확인
        {
            return; // 회전 처리 중단
        }

        float targetYaw = Quaternion.LookRotation(cameraForward).eulerAngles.y; // 목표 좌우 각도 계산
        transform.rotation = Quaternion.Euler(0f, targetYaw, 0f); // X축과 Z축 기울기 제거
    }

    private void OnValidate() // Inspector 지면 설정값 검증
    {
        groundCheckDistance = Mathf.Max(0.01f, groundCheckDistance); // 지면 검사 거리 최소값 적용
        groundProbeRadiusOffset = Mathf.Max(0.001f, groundProbeRadiusOffset); // 구체 감소값 최소값 적용
        groundedVerticalVelocity = Mathf.Min(-0.01f, groundedVerticalVelocity); // 지면 밀착 속도 음수 제한
        minimumFallDistance = Mathf.Max(0f, minimumFallDistance); // 유효 낙하 거리 음수 방지
    }
}