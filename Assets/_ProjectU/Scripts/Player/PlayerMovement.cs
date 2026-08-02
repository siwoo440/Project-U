using System; // C# 이벤트 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.InputSystem; // 새 Input System 기능

[RequireComponent(typeof(CharacterController))] // 필수 이동 충돌 컴포넌트
[RequireComponent(typeof(PlayerStamina))] // 필수 스태미나 컴포넌트
[RequireComponent(typeof(PlayerEquipment))] // 필수 장비 컴포넌트
[RequireComponent(typeof(PlayerTemperature))] // 필수 체온 컴포넌트
[RequireComponent(typeof(PlayerHealth))] // 필수 체력 컴포넌트
public sealed class PlayerMovement : MonoBehaviour // 플레이어 일반 이동과 회피 처리
{
    [Header("References")] // 외부 참조 묶음
    [Tooltip("이동 기준 카메라.")] // Inspector 카메라 설명
    [SerializeField] private Transform cameraTransform; // 이동 기준 카메라

    [Tooltip("건축 모드에서 이동과 회피 입력을 차단할 건축 관리자입니다.")] // Inspector 건축 관리자 설명
    [SerializeField] private BuildPlacementController buildPlacementController; // 건축 배치 관리자

    [Tooltip("회피 시작 시 진행 중인 근접 공격을 취소할 공격 관리자입니다.")] // Inspector 근접 공격 관리자 설명
    [SerializeField] private PlayerWeaponAttackController weaponAttackController; // 근접 공격 관리자

    [Tooltip("회피 시작 시 진행 중인 활 장전을 취소할 활 관리자입니다.")] // Inspector 활 공격 관리자 설명
    [SerializeField] private PlayerBowChargeController bowChargeController; // 활 장전 관리자

    [Header("Movement")] // 이동 설정 묶음
    [Tooltip("걷기 속도.")] // Inspector 걷기 속도 설명
    [SerializeField] private float walkSpeed = 4f; // 걷기 속도

    [Tooltip("달리기 속도.")] // Inspector 달리기 속도 설명
    [SerializeField] private float runSpeed = 7f; // 달리기 속도

    [Tooltip("점프 높이.")] // Inspector 점프 높이 설명
    [SerializeField] private float jumpHeight = 1.2f; // 점프 높이

    [Tooltip("중력 가속도.")] // Inspector 중력 설명
    [SerializeField] private float gravity = -20f; // 중력 가속도

    [Header("Dodge")] // 회피 설정 묶음
    [Tooltip("한 번 회피할 때 이동할 최대 거리입니다. 벽과 충돌하면 실제 거리는 줄어듭니다.")] // Inspector 회피 거리 설명
    [SerializeField, Min(0.1f)] private float dodgeDistance = 4.5f; // 회피 최대 거리

    [Tooltip("회피 이동이 완료될 때까지의 시간입니다.")] // Inspector 회피 시간 설명
    [SerializeField, Min(0.05f)] private float dodgeDuration = 0.35f; // 회피 이동 시간

    [Tooltip("회피를 다시 사용할 수 있을 때까지의 대기시간입니다.")] // Inspector 회피 대기시간 설명
    [SerializeField, Min(0f)] private float dodgeCooldown = 0.8f; // 회피 재사용 대기시간

    [Tooltip("회피 한 번에 소비할 기본 스태미나입니다.")] // Inspector 회피 비용 설명
    [SerializeField, Min(0f)] private float dodgeStaminaCost = 18f; // 회피 스태미나 비용

    [Tooltip("회피 시작 후 전투 피해를 무시할 시간입니다.")] // Inspector 회피 무적 설명
    [SerializeField, Min(0f)] private float dodgeInvulnerabilityDuration = 0.28f; // 회피 무적 시간

    [Tooltip("이동 입력 없이 회피할 때 카메라 반대 방향으로 후퇴합니다.")] // Inspector 무입력 회피 설명
    [SerializeField] private bool dodgeBackwardWithoutInput = true; // 무입력 후방 회피 여부

    [Tooltip("회피 중 벽의 측면과 충돌하면 회피를 즉시 종료합니다.")] // Inspector 충돌 종료 설명
    [SerializeField] private bool stopDodgeOnSideCollision = true; // 벽 충돌 시 회피 종료 여부

    [Tooltip("0에서 1까지 회피 거리 진행 비율을 정의합니다. 시작값 0, 종료값 1을 권장합니다.")] // Inspector 회피 곡선 설명
    [SerializeField] private AnimationCurve dodgeDistanceCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); // 회피 거리 진행 곡선

    [Header("Ground Detection")] // 지면 판정 설정 묶음
    [Tooltip("지면 검사 대상 레이어.")] // Inspector 지면 레이어 설명
    [SerializeField] private LayerMask groundLayerMask = ~0; // 지면 검사 대상 레이어

    [Tooltip("발밑 지면 검사 거리.")] // Inspector 지면 검사 거리 설명
    [SerializeField] private float groundCheckDistance = 0.15f; // 발밑 지면 검사 거리

    [Tooltip("검사 구체 반지름 감소값.")] // Inspector 검사 반지름 설명
    [SerializeField] private float groundProbeRadiusOffset = 0.05f; // 검사 구체 반지름 감소값

    [Tooltip("지면 밀착용 하강 속도.")] // Inspector 지면 밀착 설명
    [SerializeField] private float groundedVerticalVelocity = -2f; // 지면 밀착용 하강 속도

    [Tooltip("유효 낙하 최소 거리.")] // Inspector 낙하 거리 설명
    [SerializeField] private float minimumFallDistance = 1.5f; // 유효 낙하 최소 거리

    [Header("Ground Runtime")] // 지면 실행 상태 묶음
    [Tooltip("현재 접지 상태.")] // Inspector 접지 설명
    [SerializeField] private bool isGrounded; // 현재 접지 상태

    [Tooltip("현재 지면 경사 각도.")] // Inspector 경사 설명
    [SerializeField] private float currentSlopeAngle; // 현재 지면 경사 각도

    [Tooltip("현재 낙하 상태.")] // Inspector 낙하 설명
    [SerializeField] private bool isFalling; // 현재 낙하 상태

    [Tooltip("마지막 낙하 거리.")] // Inspector 마지막 낙하 설명
    [SerializeField] private float lastFallDistance; // 마지막 낙하 거리

    [Tooltip("유효 낙하 발생 여부.")] // Inspector 유효 낙하 설명
    [SerializeField] private bool wasSignificantFall; // 유효 낙하 발생 여부

    [Header("Dodge Runtime")] // 회피 실행 상태 묶음
    [Tooltip("현재 회피 이동 중인지 표시합니다.")] // Inspector 회피 상태 설명
    [SerializeField] private bool isDodging; // 현재 회피 상태

    [Tooltip("현재 회피 진행 비율입니다.")] // Inspector 회피 진행 설명
    [SerializeField, Range(0f, 1f)] private float currentDodgeNormalizedTime; // 현재 회피 진행 비율

    [Tooltip("현재 회피 이동 방향입니다.")] // Inspector 회피 방향 설명
    [SerializeField] private Vector3 currentDodgeDirection; // 현재 회피 방향

    [Tooltip("다음 회피까지 남은 대기시간입니다.")] // Inspector 회피 대기시간 설명
    [SerializeField] private float dodgeCooldownRemaining; // 회피 대기시간 표시

    [Header("Input Actions")] // 입력 설정 묶음
    [Tooltip("이동 액션 참조.")] // Inspector 이동 입력 설명
    [SerializeField] private InputActionReference moveActionReference; // 이동 액션 참조

    [Tooltip("달리기 액션 참조.")] // Inspector 달리기 입력 설명
    [SerializeField] private InputActionReference sprintActionReference; // 달리기 액션 참조

    [Tooltip("점프 액션 참조.")] // Inspector 점프 입력 설명
    [SerializeField] private InputActionReference jumpActionReference; // 점프 액션 참조

    [Tooltip("회피 액션 참조. Button 타입과 Left Ctrl 바인딩을 권장합니다.")] // Inspector 회피 입력 설명
    [SerializeField] private InputActionReference dodgeActionReference; // 회피 액션 참조

    private CharacterController characterController; // 캐릭터 충돌 이동기
    private PlayerStamina playerStamina; // 플레이어 스태미나 관리기
    private PlayerEquipment playerEquipment; // 플레이어 장비 관리자
    private PlayerTemperature playerTemperature; // 플레이어 체온 관리자
    private PlayerHealth playerHealth; // 플레이어 체력 관리자
    private float verticalVelocity; // 수직 이동 속도
    private Vector3 groundNormal = Vector3.up; // 현재 지면의 수직 방향
    private float fallStartHeight; // 낙하 시작 높이
    private float dodgeStartedAt; // 회피 시작 시각
    private float nextDodgeTime; // 다음 회피 가능 시각
    private float previousDodgeCurveValue; // 이전 회피 거리 곡선값

    public bool IsGrounded => isGrounded; // 현재 접지 상태 공개
    public bool IsFalling => isFalling; // 현재 낙하 상태 공개
    public float CurrentSlopeAngle => currentSlopeAngle; // 현재 경사 각도 공개
    public float LastFallDistance => lastFallDistance; // 마지막 낙하 거리 공개
    public bool WasSignificantFall => wasSignificantFall; // 유효 낙하 여부 공개
    public bool IsDodging => isDodging; // 현재 회피 상태 공개
    public float CurrentDodgeNormalizedTime => currentDodgeNormalizedTime; // 현재 회피 진행 비율 공개
    public Vector3 CurrentDodgeDirection => currentDodgeDirection; // 현재 회피 방향 공개
    public float DodgeCooldownRemaining => Mathf.Max(0f, nextDodgeTime - Time.time); // 남은 회피 대기시간 공개

    public event Action<float> Landed; // 착지 거리 이벤트
    public event Action DodgeStarted; // 회피 시작 이벤트
    public event Action DodgeEnded; // 회피 종료 이벤트

    private void Awake() // 이동 컴포넌트 초기화
    {
        characterController = GetComponent<CharacterController>(); // CharacterController 가져오기
        playerStamina = GetComponent<PlayerStamina>(); // PlayerStamina 가져오기
        playerEquipment = GetComponent<PlayerEquipment>(); // PlayerEquipment 가져오기
        playerTemperature = GetComponent<PlayerTemperature>(); // PlayerTemperature 가져오기
        playerHealth = GetComponent<PlayerHealth>(); // PlayerHealth 가져오기

        if (weaponAttackController == null) // 근접 공격 관리자 참조 확인
        {
            weaponAttackController = GetComponent<PlayerWeaponAttackController>(); // 같은 Player에서 자동 검색
        }

        if (bowChargeController == null) // 활 공격 관리자 참조 확인
        {
            bowChargeController = GetComponent<PlayerBowChargeController>(); // 같은 Player에서 자동 검색
        }

        if (buildPlacementController == null) // 건축 관리자 참조 확인
        {
            buildPlacementController = FindFirstObjectByType<BuildPlacementController>(); // 현재 Scene에서 건축 관리자 자동 검색
        }

        bool hasMissingReference = // 값 계산 시작
            cameraTransform == null // 조건 시작
            || moveActionReference == null // 조건 추가
            || sprintActionReference == null // 조건 추가
            || jumpActionReference == null // 조건 추가
            || dodgeActionReference == null // 조건 추가
            || playerStamina == null // 조건 추가
            || playerEquipment == null // 조건 추가
            || playerTemperature == null // 조건 추가
            || playerHealth == null; // 필수 참조 누락 확인

        if (hasMissingReference) // 필수 참조 연결 확인
        {
            Debug.LogError("PlayerMovement의 Main Camera, Move, Sprint, Jump, Dodge Input Action과 플레이어 상태 컴포넌트를 연결해야 합니다.", this); // 참조 누락 오류
            enabled = false; // 이동 스크립트 비활성화
        }
    }

    private void OnEnable() // 입력 활성화
    {
        if (moveActionReference != null) // 이동 액션 존재 확인
        {
            moveActionReference.action.Enable(); // 이동 액션 활성화
        }

        if (sprintActionReference != null) // 달리기 액션 존재 확인
        {
            sprintActionReference.action.Enable(); // 달리기 액션 활성화
        }

        if (jumpActionReference != null) // 점프 액션 존재 확인
        {
            jumpActionReference.action.Enable(); // 점프 액션 활성화
        }

        if (dodgeActionReference != null) // 회피 액션 존재 확인
        {
            dodgeActionReference.action.Enable(); // 회피 액션 활성화
        }
    }

    private void OnDisable() // 입력과 회피 상태 비활성화
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

        if (dodgeActionReference != null) // 회피 액션 존재 확인
        {
            dodgeActionReference.action.Disable(); // 회피 액션 비활성화
        }

        FinishDodge(true); // 비활성화 시 회피와 회피 무적 즉시 종료
    }

    private void Update() // 매 프레임 이동과 회피 처리
    {
        dodgeCooldownRemaining = DodgeCooldownRemaining; // Inspector 회피 대기시간 갱신
        Vector2 moveInput = moveActionReference.action.ReadValue<Vector2>(); // 이동 입력 읽기
        Vector3 cameraForward = GetPlanarCameraForward(); // 카메라 수평 전방 방향 계산
        Vector3 cameraRight = GetPlanarCameraRight(); // 카메라 수평 오른쪽 방향 계산
        Vector3 moveDirection = cameraForward * moveInput.y + cameraRight * moveInput.x; // 카메라 기준 이동 방향 계산
        moveDirection = Vector3.ClampMagnitude(moveDirection, 1f); // 대각선 이동 속도 제한

        bool wasGrounded = isGrounded; // 이전 프레임 접지 상태 저장
        UpdateGroundState(); // 현재 지면 상태 검사
        UpdateLandingState(wasGrounded); // 착지와 낙하 거리 판정

        if (isDodging) // 현재 회피 이동 여부 확인
        {
            if (!CanContinueDodge()) // 회피 유지 가능 상태 확인
            {
                FinishDodge(true); // UI 또는 사망 상태에서 회피 즉시 종료
            }
            else // 회피 유지 가능 상태 처리
            {
                UpdateDodgeMovement(cameraForward); // 현재 회피 이동 갱신
                return; // 일반 이동 처리 차단
            }
        }

        if (TryStartDodge(moveDirection, cameraForward)) // 이번 프레임 회피 시작 시도
        {
            UpdateDodgeMovement(cameraForward); // 회피 시작 프레임 이동 적용
            return; // 일반 이동 처리 차단
        }

        Vector3 adjustedMoveDirection = GetSlopeAdjustedDirection(moveDirection); // 경사면 기준 이동 방향 계산
        bool hasMovementInput = moveDirection.sqrMagnitude > 0.01f; // 실제 이동 입력 존재 확인
        bool wantsToSprint = sprintActionReference.action.IsPressed() && hasMovementInput; // 이동 중 달리기 입력 확인
        bool isSprinting = playerStamina.UpdateSprint(wantsToSprint, Time.deltaTime); // 스태미나 기반 달리기 판정
        float baseMovementSpeed = isSprinting ? runSpeed : walkSpeed; // 기본 이동 속도 결정
        float equipmentSpeedMultiplier = 1f + playerEquipment.TotalMovementSpeedBonusPercent / 100f; // 장비 이동 속도 배율 계산
        float temperatureSpeedMultiplier = playerTemperature.MovementSpeedMultiplier; // 온도 이동 속도 배율 조회
        float currentSpeed = baseMovementSpeed // 값 계산 시작
            * equipmentSpeedMultiplier // 장비 이동 속도 적용
            * temperatureSpeedMultiplier; // 체온 이동 속도 적용

        UpdateVerticalVelocity(true); // 점프와 중력 계산
        UpdateFallingState(); // 공중 하강 상태 검사

        Vector3 horizontalMovement = adjustedMoveDirection * currentSpeed; // 경사면 적용 수평 이동량 계산
        Vector3 verticalMovement = Vector3.up * verticalVelocity; // 수직 이동량 계산
        Vector3 finalMovement = horizontalMovement + verticalMovement; // 최종 이동량 결합

        FaceCameraDirection(cameraForward); // 카메라 시선 방향 적용
        characterController.Move(finalMovement * Time.deltaTime); // 충돌을 적용한 일반 이동 실행
    }

    private bool TryStartDodge(Vector3 moveDirection, Vector3 cameraForward) // 현재 입력으로 회피 시작 시도
    {
        if (dodgeActionReference == null || !dodgeActionReference.action.WasPressedThisFrame()) // 회피 입력 여부 확인
        {
            return false; // 회피 입력 없음 반환
        }

        if (!CanStartDodge()) // 현재 회피 시작 가능 상태 확인
        {
            return false; // 회피 시작 실패 반환
        }

        if (!playerStamina.TryConsume(dodgeStaminaCost)) // 회피 스태미나 실제 소비 시도
        {
            return false; // 스태미나 부족 회피 실패 반환
        }

        currentDodgeDirection = ResolveDodgeDirection(moveDirection, cameraForward); // 현재 회피 방향 계산

        if (currentDodgeDirection.sqrMagnitude < 0.001f) // 유효한 회피 방향 확인
        {
            return false; // 회피 방향 없음 반환
        }

        weaponAttackController?.CancelCurrentAttack(); // 진행 중인 근접 공격과 연속 입력 취소
        bowChargeController?.CancelCharge(); // 진행 중인 활 장전 취소
        dodgeStartedAt = Time.time; // 회피 시작 시각 저장
        nextDodgeTime = Time.time + dodgeCooldown; // 다음 회피 가능 시각 저장
        currentDodgeNormalizedTime = 0f; // 회피 진행 비율 초기화
        previousDodgeCurveValue = Mathf.Clamp01(dodgeDistanceCurve.Evaluate(0f)); // 회피 거리 곡선 시작값 저장
        isDodging = true; // 회피 상태 적용
        playerHealth.BeginDodgeInvulnerability(dodgeInvulnerabilityDuration); // 회피 전투 무적 시작
        DodgeStarted?.Invoke(); // 회피 시작 이벤트 전달
        return true; // 회피 시작 성공 반환
    }

    private bool CanStartDodge() // 회피 시작 가능 상태 계산
    {
        if (Cursor.lockState != CursorLockMode.Locked || Time.timeScale <= 0f) // Gameplay 입력 가능 상태 확인
        {
            return false; // UI 또는 일시정지 중 회피 차단
        }

        if (playerHealth.IsDead) // 사망 상태 확인
        {
            return false; // 사망 상태 회피 차단
        }

        if (buildPlacementController != null && buildPlacementController.BlocksGameplayInput) // 건축 입력 차단 상태 확인
        {
            return false; // 건축 모드 회피 차단
        }

        if (!isGrounded || isFalling) // 접지와 낙하 상태 확인
        {
            return false; // 공중 회피 차단
        }

        if (Time.time < nextDodgeTime) // 회피 재사용 대기시간 확인
        {
            return false; // 대기시간 중 회피 차단
        }

        return playerStamina.CanConsume(dodgeStaminaCost); // 스태미나 보유량 기준 회피 가능 여부 반환
    }

    private bool CanContinueDodge() // 진행 중인 회피 유지 가능 상태 계산
    {
        if (Cursor.lockState != CursorLockMode.Locked || Time.timeScale <= 0f) // Gameplay 입력 가능 상태 확인
        {
            return false; // UI 또는 일시정지 진입 회피 종료
        }

        if (playerHealth.IsDead) // 사망 상태 확인
        {
            return false; // 사망 상태 회피 종료
        }

        if (buildPlacementController != null && buildPlacementController.BlocksGameplayInput) // 건축 입력 차단 상태 확인
        {
            return false; // 건축 모드 회피 종료
        }

        return true; // 회피 유지 가능 반환
    }

    private Vector3 ResolveDodgeDirection(Vector3 moveDirection, Vector3 cameraForward) // 입력과 카메라 기준 회피 방향 계산
    {
        Vector3 requestedDirection = moveDirection; // 이동 입력 회피 방향 저장

        if (requestedDirection.sqrMagnitude < 0.01f) // 이동 입력 없음 확인
        {
            requestedDirection = dodgeBackwardWithoutInput // 설정된 무입력 방향 확인
                ? -cameraForward // 카메라 반대 방향 후퇴
                : cameraForward; // 카메라 전방 방향 전진
        }

        Vector3 slopeDirection = Vector3.ProjectOnPlane(requestedDirection, groundNormal); // 지면 경사면에 회피 방향 투영

        if (slopeDirection.sqrMagnitude < 0.001f) // 경사면 회피 방향 유효성 확인
        {
            slopeDirection = requestedDirection; // 기존 요청 방향 대체 사용
        }

        slopeDirection.y = 0f; // 회피 방향 수직 성분 제거

        if (slopeDirection.sqrMagnitude < 0.001f) // 최종 회피 방향 유효성 확인
        {
            return Vector3.zero; // 회피 방향 없음 반환
        }

        return slopeDirection.normalized; // 정규화된 회피 방향 반환
    }

    private void UpdateDodgeMovement(Vector3 cameraForward) // 회피 거리 곡선과 중력을 적용한 이동 갱신
    {
        float elapsedTime = Time.time - dodgeStartedAt; // 회피 시작 후 경과 시간 계산
        currentDodgeNormalizedTime = Mathf.Clamp01(elapsedTime / dodgeDuration); // 회피 진행 비율 계산
        float evaluatedCurveValue = Mathf.Clamp01(dodgeDistanceCurve.Evaluate(currentDodgeNormalizedTime)); // 현재 거리 곡선값 계산
        float currentCurveValue = Mathf.Max(previousDodgeCurveValue, evaluatedCurveValue); // 뒤로 감소하지 않는 거리 진행값 계산
        float frameDistance = dodgeDistance * (currentCurveValue - previousDodgeCurveValue); // 이번 프레임 회피 이동 거리 계산
        previousDodgeCurveValue = currentCurveValue; // 다음 프레임용 거리 진행값 저장

        UpdateVerticalVelocity(false); // 회피 중 점프를 제외한 중력 계산
        UpdateFallingState(); // 회피 중 공중 하강 상태 검사

        Vector3 horizontalDisplacement = currentDodgeDirection * frameDistance; // 이번 프레임 회피 수평 이동량 계산
        Vector3 verticalDisplacement = Vector3.up * verticalVelocity * Time.deltaTime; // 이번 프레임 수직 이동량 계산
        FaceCameraDirection(cameraForward); // 회피 중 카메라 시선 방향 유지
        CollisionFlags collisionFlags = characterController.Move(horizontalDisplacement + verticalDisplacement); // 충돌 적용 회피 이동 실행
        bool hitSideCollision = (collisionFlags & CollisionFlags.Sides) != 0; // 벽 측면 충돌 여부 계산

        if (currentDodgeNormalizedTime >= 1f) // 회피 시간 완료 여부 확인
        {
            FinishDodge(false); // 정상 회피 종료
            return; // 추가 종료 검사 생략
        }

        if (stopDodgeOnSideCollision && hitSideCollision) // 벽 충돌 종료 설정과 충돌 여부 확인
        {
            FinishDodge(false); // 벽 충돌 회피 종료
        }
    }

    private void FinishDodge(bool resetProgress) // 회피 이동과 회피 무적 종료
    {
        if (!isDodging) // 현재 회피 상태 확인
        {
            playerHealth?.EndDodgeInvulnerability(); // 남아 있을 수 있는 회피 무적 정리
            return; // 회피 종료 처리 생략
        }

        isDodging = false; // 회피 상태 해제
        currentDodgeDirection = Vector3.zero; // 회피 방향 초기화
        previousDodgeCurveValue = 0f; // 거리 곡선 진행값 초기화
        playerHealth?.EndDodgeInvulnerability(); // 회피 전투 무적 종료

        if (resetProgress) // 회피 진행값 초기화 여부 확인
        {
            currentDodgeNormalizedTime = 0f; // 회피 진행 비율 초기화
        }
        else // 정상 회피 종료 처리
        {
            currentDodgeNormalizedTime = 1f; // 완료된 회피 진행 비율 적용
        }

        DodgeEnded?.Invoke(); // 회피 종료 이벤트 전달
    }

    private void UpdateVerticalVelocity(bool allowJump) // 점프 허용 여부와 중력 계산
    {
        if (isGrounded && verticalVelocity < 0f) // 지면의 하강 상태 확인
        {
            verticalVelocity = groundedVerticalVelocity; // 지면 밀착 속도 적용
        }

        if (allowJump && isGrounded && jumpActionReference.action.WasPressedThisFrame()) // 점프 허용과 지면 점프 입력 확인
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
        bool hasGroundHit = Physics.SphereCast( // 호출 시작
            castOrigin, // 매개변수 전달
            probeRadius, // 매개변수 전달
            Vector3.down, // 매개변수 전달
            out RaycastHit groundHit, // 매개변수 전달
            groundCheckDistance, // 매개변수 전달
            groundLayerMask, // 매개변수 전달
            QueryTriggerInteraction.Ignore); // 발밑 지면 구체 검사

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

        if (!wasSignificantFall) // 유효 낙하 여부 확인
        {
            return; // 짧은 낙하 이벤트 제외
        }

        Landed?.Invoke(lastFallDistance); // 착지 거리 전달
    }

    public void ResetMotionState() // 부활 후 이동과 회피 상태 초기화
    {
        FinishDodge(true); // 진행 중인 회피와 무적 종료
        verticalVelocity = groundedVerticalVelocity; // 수직 속도 초기화
        isGrounded = false; // 접지 상태 재검사 준비
        isFalling = false; // 낙하 상태 해제
        lastFallDistance = 0f; // 이전 낙하 거리 제거
        wasSignificantFall = false; // 유효 낙하 결과 제거
        groundNormal = Vector3.up; // 지면 방향 초기화
        currentSlopeAngle = 0f; // 경사 각도 초기화
        fallStartHeight = transform.position.y; // 새로운 낙하 기준점 적용
        nextDodgeTime = 0f; // 부활 후 회피 대기시간 초기화
        dodgeCooldownRemaining = 0f; // Inspector 회피 대기시간 초기화
    }

    private Vector3 GetPlanarCameraForward() // 카메라 수평 전방 방향 계산
    {
        Vector3 cameraForward = cameraTransform.forward; // 카메라 전방 방향 가져오기
        cameraForward.y = 0f; // 상하 방향 제거

        if (cameraForward.sqrMagnitude < 0.001f) // 유효한 전방 방향 확인
        {
            return transform.forward; // 플레이어 전방 방향 대체 반환
        }

        return cameraForward.normalized; // 정규화된 카메라 전방 방향 반환
    }

    private Vector3 GetPlanarCameraRight() // 카메라 수평 오른쪽 방향 계산
    {
        Vector3 cameraRight = cameraTransform.right; // 카메라 오른쪽 방향 가져오기
        cameraRight.y = 0f; // 상하 방향 제거

        if (cameraRight.sqrMagnitude < 0.001f) // 유효한 오른쪽 방향 확인
        {
            return transform.right; // 플레이어 오른쪽 방향 대체 반환
        }

        return cameraRight.normalized; // 정규화된 카메라 오른쪽 방향 반환
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

    private void OnValidate() // Inspector 이동과 회피 설정값 검증
    {
        walkSpeed = Mathf.Max(0f, walkSpeed); // 걷기 속도 음수 방지
        runSpeed = Mathf.Max(walkSpeed, runSpeed); // 달리기 속도 걷기 이상 적용
        jumpHeight = Mathf.Max(0f, jumpHeight); // 점프 높이 음수 방지
        gravity = Mathf.Min(-0.01f, gravity); // 중력 음수 제한
        dodgeDistance = Mathf.Max(0.1f, dodgeDistance); // 회피 거리 최소값 적용
        dodgeDuration = Mathf.Max(0.05f, dodgeDuration); // 회피 시간 최소값 적용
        dodgeCooldown = Mathf.Max(0f, dodgeCooldown); // 회피 대기시간 음수 방지
        dodgeStaminaCost = Mathf.Max(0f, dodgeStaminaCost); // 회피 스태미나 비용 음수 방지
        dodgeInvulnerabilityDuration = Mathf.Clamp(dodgeInvulnerabilityDuration, 0f, dodgeDuration); // 회피 시간 범위로 무적 시간 제한
        groundCheckDistance = Mathf.Max(0.01f, groundCheckDistance); // 지면 검사 거리 최소값 적용
        groundProbeRadiusOffset = Mathf.Max(0.001f, groundProbeRadiusOffset); // 구체 감소값 최소값 적용
        groundedVerticalVelocity = Mathf.Min(-0.01f, groundedVerticalVelocity); // 지면 밀착 속도 음수 제한
        minimumFallDistance = Mathf.Max(0f, minimumFallDistance); // 유효 낙하 거리 음수 방지

        if (dodgeDistanceCurve == null || dodgeDistanceCurve.length == 0) // 회피 거리 곡선 존재 확인
        {
            dodgeDistanceCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); // 기본 회피 거리 곡선 복구
        }
    }
}
