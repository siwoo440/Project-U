using UnityEngine; // Unity 기본 기능
using UnityEngine.InputSystem; // 새 Input System 기능

[RequireComponent(typeof(CharacterController))] // 필수 이동 충돌 컴포넌트
public sealed class PlayerMovement : MonoBehaviour // 플레이어 이동 처리
{
    [Header("References")] // 외부 참조 묶음
    [SerializeField] private Transform cameraTransform; // 이동 기준 카메라

    [Header("Movement")] // 이동 설정 묶음
    [SerializeField] private float walkSpeed = 4f; // 걷기 속도
    [SerializeField] private float runSpeed = 7f; // 달리기 속도
    [SerializeField] private float jumpHeight = 1.2f; // 점프 높이
    [SerializeField] private float gravity = -20f; // 중력 가속도

    [Header("Input Actions")] // 입력 설정 묶음
    [SerializeField] private InputActionReference moveActionReference; // 이동 액션 참조
    [SerializeField] private InputActionReference sprintActionReference; // 달리기 액션 참조
    [SerializeField] private InputActionReference jumpActionReference; // 점프 액션 참조

    private CharacterController characterController; // 캐릭터 충돌 이동기
    private float verticalVelocity; // 수직 이동 속도

    private void Awake() // 이동 컴포넌트 초기화
    {
        characterController = GetComponent<CharacterController>(); // CharacterController 가져오기

        if (cameraTransform == null || moveActionReference == null || sprintActionReference == null || jumpActionReference == null) // 필수 참조 연결 확인
        {
            Debug.LogError("Main Camera와 Move, Sprint, Jump Input Action을 연결해야 합니다.", this); // 참조 누락 오류
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

        bool isSprinting = sprintActionReference.action.IsPressed(); // 달리기 입력 확인
        float currentSpeed = isSprinting ? runSpeed : walkSpeed; // 현재 이동 속도 결정

        UpdateVerticalVelocity(); // 점프와 중력 계산

        Vector3 horizontalMovement = moveDirection * currentSpeed; // 수평 이동량 계산
        Vector3 verticalMovement = Vector3.up * verticalVelocity; // 수직 이동량 계산
        Vector3 finalMovement = horizontalMovement + verticalMovement; // 최종 이동량 결합

        FaceCameraDirection(cameraForward); // 카메라 시선 방향 적용
        characterController.Move(finalMovement * Time.deltaTime); // 충돌을 적용한 이동 실행
    }

    private void UpdateVerticalVelocity() // 점프와 중력 계산
    {
        bool isGrounded = characterController.isGrounded; // 지면 접촉 확인

        if (isGrounded && verticalVelocity < 0f) // 지면의 하강 상태 확인
        {
            verticalVelocity = -2f; // 안정적인 지면 밀착
        }

        if (isGrounded && jumpActionReference.action.WasPressedThisFrame()) // 점프 입력 확인
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity); // 점프 초기 속도 계산
        }

        verticalVelocity += gravity * Time.deltaTime; // 중력 누적
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
}