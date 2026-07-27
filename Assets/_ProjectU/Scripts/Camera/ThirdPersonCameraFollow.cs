using UnityEngine; // Unity 기본 기능
using UnityEngine.InputSystem; // 새 Input System 기능

public sealed class ThirdPersonCameraFollow : MonoBehaviour // 마우스 회전식 추적 카메라
{
    [Header("Target")] // 추적 대상 설정
    [SerializeField] private Transform target; // 플레이어 Transform
    [SerializeField] private float targetHeight = 1f; // 시선 중심 높이

    [Header("Camera")] // 카메라 위치 설정
    [SerializeField] private float distance = 10f; // 플레이어와의 거리
    [SerializeField] private float initialPitch = 30f; // 시작 상하 각도
    [SerializeField] private float minimumPitch = -20f; // 최소 상하 각도
    [SerializeField] private float maximumPitch = 65f; // 최대 상하 각도

    [Header("Mouse")] // 마우스 설정
    [SerializeField] private float mouseSensitivity = 0.1f; // 마우스 감도

    private float yaw; // 좌우 회전값
    private float pitch; // 상하 회전값

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
        SetCursorLocked(true); // 마우스 커서 잠금
    }

    private void OnDisable() // 카메라 비활성화 처리
    {
        SetCursorLocked(false); // 마우스 커서 잠금 해제
    }

    private void Update() // 커서 상태 변경 처리
    {
        if (Keyboard.current == null) // 키보드 존재 확인
        {
            return; // 입력 처리 중단
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame) // Escape 입력 확인
        {
            bool shouldLock = Cursor.lockState != CursorLockMode.Locked; // 다음 잠금 상태 계산
            SetCursorLocked(shouldLock); // 커서 상태 변경
        }
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
        Vector3 cameraPosition = focusPosition - cameraRotation * Vector3.forward * distance; // 카메라 위치 계산

        transform.SetPositionAndRotation(cameraPosition, cameraRotation); // 위치와 회전 동시 적용
    }

    private void SetCursorLocked(bool isLocked) // 마우스 커서 상태 설정
    {
        Cursor.lockState = isLocked ? CursorLockMode.Locked : CursorLockMode.None; // 커서 잠금 방식 적용
        Cursor.visible = !isLocked; // 커서 표시 상태 적용
    }
}