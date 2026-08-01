using System.Collections.Generic; // 컬렉션 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.InputSystem; // 새 Input System 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class BuildModeCameraController : MonoBehaviour // 건축 모드 자유 카메라 관리자
{
    [Header("Camera References")] // 카메라 참조 묶음
    [Tooltip("건축 모드에서 조작할 기본 Camera입니다. 비어 있거나 비활성 상태이면 Camera.main을 사용합니다.")]
    [SerializeField] private Camera preferredCamera; // 기본 건축 Camera

    [Tooltip("현재 프로젝트의 3인칭 추적 카메라입니다. 건축 중 내부 시점 값을 유지한 채 외부 제어 상태로 전환합니다.")]
    [SerializeField] private ThirdPersonCameraFollow thirdPersonCameraFollow; // 3인칭 추적 카메라

    [Tooltip("플레이어 위치 고정과 건축 Camera 이동 범위 계산에 사용할 Player Transform입니다.")]
    [SerializeField] private Transform playerTransform; // 플레이어 Transform

    [Header("Gameplay Suspension")] // Gameplay 정지 대상 묶음
    [Tooltip("건축 중 비활성화할 PlayerMovement, PlayerInteractor, HotbarItemUse 등의 Gameplay 컴포넌트입니다.")]
    [SerializeField] private Behaviour[] gameplayBehavioursToDisable = new Behaviour[0]; // 건축 중 Gameplay 비활성화 목록

    [Tooltip("현재 시점 전환 관리자나 별도 1인칭 Camera Controller처럼 건축 중 잠시 정지할 추가 Camera 컴포넌트입니다.")]
    [SerializeField] private Behaviour[] additionalCameraBehavioursToSuspend = new Behaviour[0]; // 추가 Camera 제어 정지 목록

    [Header("Player Lock")] // 플레이어 고정 설정 묶음
    [Tooltip("건축 모드 중 Player Transform의 위치와 회전을 진입 시점 값으로 유지할지 설정합니다.")]
    [SerializeField] private bool lockPlayerTransform = true; // 플레이어 Transform 고정 여부

    [Header("Look Control")] // 카메라 회전 설정 묶음
    [Tooltip("우클릭 홀드 상태에서 마우스 이동에 적용할 회전 감도입니다.")]
    [SerializeField, Min(0.01f)] private float lookSensitivity = 0.15f; // 우클릭 회전 감도

    [Tooltip("건축 Camera가 아래쪽으로 내려다볼 수 있는 최소 Pitch입니다.")]
    [SerializeField] private float minimumPitch = -80f; // 최소 상하 회전값

    [Tooltip("건축 Camera가 위쪽으로 올려다볼 수 있는 최대 Pitch입니다.")]
    [SerializeField] private float maximumPitch = 85f; // 최대 상하 회전값

    [Tooltip("우클릭 카메라 회전의 상하 방향을 반전할지 설정합니다.")]
    [SerializeField] private bool invertLookY; // 상하 회전 반전 여부

    [Header("Pan Control")] // 카메라 평행 이동 설정 묶음
    [Tooltip("마우스 휠 버튼 홀드 상태의 화면 평행 이동 감도입니다.")]
    [SerializeField, Min(0.0001f)] private float panSensitivity = 0.015f; // 휠 버튼 평행 이동 감도

    [Tooltip("휠 버튼 드래그 이동 방향을 반전할지 설정합니다.")]
    [SerializeField] private bool invertPan; // 평행 이동 반전 여부

    [Header("Wheel Control")] // 카메라 전후 이동 설정 묶음
    [Tooltip("마우스 휠 한 단계마다 Camera 전방 또는 후방으로 이동할 거리입니다.")]
    [SerializeField, Min(0.1f)] private float wheelMoveStep = 2f; // 휠 전후 이동 거리

    [Tooltip("마우스 휠 전후 이동 방향을 반전할지 설정합니다.")]
    [SerializeField] private bool invertWheel; // 휠 이동 반전 여부

    [Header("Movement Limits")] // 카메라 이동 제한 묶음
    [Tooltip("건축 Camera가 Player 위치에서 벗어날 수 있는 최대 3차원 거리입니다.")]
    [SerializeField, Min(1f)] private float maximumDistanceFromPlayer = 25f; // 플레이어 기준 최대 이동 거리

    [Tooltip("건축 Camera의 Player 기준 최소 높이입니다.")]
    [SerializeField] private float minimumHeightFromPlayer = 1f; // 플레이어 기준 최소 카메라 높이

    [Tooltip("건축 Camera의 Player 기준 최대 높이입니다.")]
    [SerializeField, Min(1f)] private float maximumHeightFromPlayer = 30f; // 플레이어 기준 최대 카메라 높이

    [Header("Collision")] // 카메라 충돌 설정 묶음
    [Tooltip("건축 Camera 이동을 막을 지형, 구조물과 장애물 레이어입니다.")]
    [SerializeField] private LayerMask collisionLayerMask = ~0; // 카메라 충돌 레이어

    [Tooltip("건축 Camera 이동 충돌 검사에 사용할 SphereCast 반지름입니다.")]
    [SerializeField, Min(0.01f)] private float collisionRadius = 0.3f; // 카메라 충돌 반지름

    [Tooltip("충돌 지점과 건축 Camera 사이에 유지할 안전 거리입니다.")]
    [SerializeField, Min(0f)] private float collisionPadding = 0.2f; // 카메라 충돌 여유 거리

    [Header("Cursor")] // 건축 커서 설정 묶음
    [Tooltip("건축 모드에서 마우스 포인터를 게임 창 안에 제한할지 설정합니다.")]
    [SerializeField] private bool confineCursor = true; // 건축 커서 창 제한 여부

    private readonly Dictionary<Behaviour, bool> previousBehaviourStates = new Dictionary<Behaviour, bool>(); // Gameplay 컴포넌트 기존 활성 상태
    private Camera activeCamera; // 현재 건축 모드에서 사용하는 Camera
    private Vector3 savedCameraPosition; // 건축 진입 전 Camera 위치
    private Quaternion savedCameraRotation; // 건축 진입 전 Camera 회전
    private Vector3 lockedPlayerPosition; // 건축 진입 시 Player 위치
    private Quaternion lockedPlayerRotation; // 건축 진입 시 Player 회전
    private CursorLockMode savedCursorLockMode; // 건축 진입 전 커서 고정 상태
    private bool savedCursorVisible; // 건축 진입 전 커서 표시 상태
    private float cameraYaw; // 건축 Camera 좌우 회전값
    private float cameraPitch; // 건축 Camera 상하 회전값
    private bool isActive; // 현재 자유 건축 Camera 활성 여부
    private bool isManipulatingCamera; // 우클릭 또는 휠 클릭 카메라 조작 여부

    public bool IsActive => isActive; // 현재 건축 Camera 활성 여부 제공
    public bool IsManipulatingCamera => isManipulatingCamera; // 현재 Camera 드래그 조작 여부 제공
    public Camera ActiveCamera => activeCamera; // 현재 건축 배치 Ray에 사용할 Camera 제공

    public Vector2 PointerScreenPosition // 현재 마우스 포인터 화면 좌표 제공
    {
        get
        {
            if (Mouse.current == null) // 마우스 장치 존재 확인
            {
                return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f); // 장치가 없으면 화면 중앙 반환
            }

            Vector2 pointerPosition = Mouse.current.position.ReadValue(); // 현재 포인터 위치 조회
            pointerPosition.x = Mathf.Clamp(pointerPosition.x, 0f, Mathf.Max(0f, Screen.width - 1f)); // X 위치 화면 범위 제한
            pointerPosition.y = Mathf.Clamp(pointerPosition.y, 0f, Mathf.Max(0f, Screen.height - 1f)); // Y 위치 화면 범위 제한
            return pointerPosition; // 제한된 포인터 위치 반환
        }
    }

    private void Awake() // 자유 건축 Camera 참조 검사
    {
        ResolveReferences(); // 누락된 참조 자동 검색

        if (playerTransform == null) // Player Transform 존재 확인
        {
            Debug.LogError(
                $"{gameObject.name}의 BuildModeCameraController에 Player Transform을 연결해야 합니다.",
                this); // Player 참조 오류 출력

            enabled = false; // 자유 건축 Camera 기능 비활성화
        }
    }

    private void Update() // 건축 Camera 마우스 입력 처리
    {
        if (!isActive) // 건축 Camera 상태 확인
        {
            return; // 일반 플레이 중 Camera 입력 생략
        }

        Mouse mouse = Mouse.current; // 현재 마우스 장치 조회

        if (mouse == null || activeCamera == null) // 마우스와 Camera 존재 확인
        {
            isManipulatingCamera = false; // Camera 조작 상태 해제
            return; // 자유 Camera 입력 처리 중단
        }

        bool lookHeld = mouse.rightButton.isPressed; // 우클릭 홀드 상태 조회
        bool panHeld = mouse.middleButton.isPressed; // 휠 버튼 홀드 상태 조회
        isManipulatingCamera = lookHeld || panHeld; // 현재 Camera 조작 상태 계산

        Vector2 mouseDelta = mouse.delta.ReadValue(); // 현재 프레임 마우스 이동량 조회

        if (lookHeld) // 우클릭 회전 상태 확인
        {
            RotateCamera(mouseDelta); // 자유 Camera 시점 회전
        }

        if (panHeld) // 휠 버튼 평행 이동 상태 확인
        {
            PanCamera(mouseDelta); // 자유 Camera 화면 평행 이동
        }

        float scrollDelta = mouse.scroll.ReadValue().y; // 현재 프레임 휠 입력 조회

        if (Mathf.Abs(scrollDelta) > 0.01f) // 유효한 휠 입력 확인
        {
            MoveCameraForward(scrollDelta); // Camera 전방 또는 후방 이동
        }
    }

    private void LateUpdate() // Player 고정 상태 유지
    {
        if (!isActive || !lockPlayerTransform || playerTransform == null) // 건축 상태와 Player 고정 설정 확인
        {
            return; // Player Transform 고정 생략
        }

        playerTransform.SetPositionAndRotation(
            lockedPlayerPosition,
            lockedPlayerRotation); // 건축 진입 시 Player 위치와 회전 유지
    }

    public bool BeginBuildMode() // 현재 활성 시점에서 자유 건축 Camera 시작
    {
        if (isActive) // 기존 건축 Camera 상태 확인
        {
            return true; // 이미 활성 상태면 성공 반환
        }

        ResolveReferences(); // 현재 활성 Camera와 Player 참조 다시 확인
        activeCamera = ResolveActiveCamera(); // 현재 사용 중인 Camera 결정

        if (activeCamera == null || playerTransform == null) // 필수 Camera와 Player 확인
        {
            Debug.LogError(
                "BuildModeCameraController가 활성 Camera 또는 Player Transform을 찾지 못했습니다.",
                this); // 건축 Camera 시작 오류 출력

            return false; // 건축 Camera 시작 실패 반환
        }

        savedCameraPosition = activeCamera.transform.position; // 현재 Camera 위치 저장
        savedCameraRotation = activeCamera.transform.rotation; // 현재 Camera 회전 저장
        lockedPlayerPosition = playerTransform.position; // 현재 Player 위치 저장
        lockedPlayerRotation = playerTransform.rotation; // 현재 Player 회전 저장
        savedCursorLockMode = Cursor.lockState; // 기존 커서 고정 상태 저장
        savedCursorVisible = Cursor.visible; // 기존 커서 표시 상태 저장

        Vector3 currentEulerAngles = activeCamera.transform.eulerAngles; // 현재 Camera Euler 회전 조회
        cameraYaw = currentEulerAngles.y; // 현재 좌우 회전값 저장
        cameraPitch = NormalizeSignedAngle(currentEulerAngles.x); // 현재 상하 회전값 저장
        cameraPitch = Mathf.Clamp(cameraPitch, minimumPitch, maximumPitch); // 시작 Pitch 제한

        SuspendNormalCameraControl(); // 일반 1·3인칭 Camera 제어 정지
        SuspendGameplayBehaviours(); // 플레이어 이동과 상호작용 정지
        ApplyBuildCursorState(); // 건축용 마우스 포인터 표시
        isManipulatingCamera = false; // 시작 Camera 조작 상태 초기화
        isActive = true; // 자유 건축 Camera 활성 상태 저장
        return true; // 건축 Camera 시작 성공 반환
    }

    public void EndBuildMode() // 자유 건축 Camera 종료와 기존 시점 복구
    {
        if (!isActive) // 기존 건축 Camera 상태 확인
        {
            return; // 중복 종료 방지
        }

        isActive = false; // 자유 건축 Camera 상태 우선 해제
        isManipulatingCamera = false; // Camera 조작 상태 해제

        if (activeCamera != null) // 사용 Camera 존재 확인
        {
            activeCamera.transform.SetPositionAndRotation(
                savedCameraPosition,
                savedCameraRotation); // 건축 진입 전 Camera Transform 임시 복구
        }

        RestoreGameplayBehaviours(); // 플레이어 이동과 상호작용 기존 상태 복구
        RestoreNormalCameraControl(); // 진입 전 1·3인칭 Camera 제어 복구
        Cursor.lockState = savedCursorLockMode; // 건축 진입 전 커서 고정 상태 복구
        Cursor.visible = savedCursorVisible; // 건축 진입 전 커서 표시 상태 복구
        activeCamera = null; // 건축 Camera 참조 해제
    }

    private void RotateCamera(Vector2 mouseDelta) // 우클릭 홀드 Camera 회전
    {
        float yDirection = invertLookY ? 1f : -1f; // 상하 회전 방향 계산
        cameraYaw += mouseDelta.x * lookSensitivity; // 좌우 회전값 변경
        cameraPitch += mouseDelta.y * lookSensitivity * yDirection; // 상하 회전값 변경
        cameraYaw = Mathf.Repeat(cameraYaw, 360f); // 좌우 회전값 범위 정리
        cameraPitch = Mathf.Clamp(cameraPitch, minimumPitch, maximumPitch); // 상하 회전 범위 제한

        activeCamera.transform.rotation =
            Quaternion.Euler(cameraPitch, cameraYaw, 0f); // 자유 Camera 회전 적용
    }

    private void PanCamera(Vector2 mouseDelta) // 휠 버튼 홀드 Camera 평행 이동
    {
        float direction = invertPan ? 1f : -1f; // 드래그 이동 방향 계산
        Vector3 rightMovement = activeCamera.transform.right * mouseDelta.x; // Camera 오른쪽 기준 이동량 계산
        Vector3 upMovement = activeCamera.transform.up * mouseDelta.y; // Camera 위쪽 기준 이동량 계산
        Vector3 movement = (rightMovement + upMovement) * panSensitivity * direction; // 최종 화면 평행 이동량 계산
        TryMoveCamera(activeCamera.transform.position + movement); // 충돌과 범위를 적용한 Camera 이동
    }

    private void MoveCameraForward(float scrollDelta) // 마우스 휠 Camera 전후 이동
    {
        float direction = Mathf.Sign(scrollDelta); // 휠 입력 방향 계산

        if (invertWheel) // 휠 이동 반전 여부 확인
        {
            direction *= -1f; // 휠 이동 방향 반전
        }

        Vector3 movement = activeCamera.transform.forward * direction * wheelMoveStep; // Camera 전방 기준 이동량 계산
        TryMoveCamera(activeCamera.transform.position + movement); // 충돌과 범위를 적용한 Camera 이동
    }

    private void TryMoveCamera(Vector3 desiredPosition) // 자유 Camera 이동 범위와 충돌 적용
    {
        if (activeCamera == null || playerTransform == null) // Camera와 Player 참조 확인
        {
            return; // Camera 이동 처리 중단
        }

        Vector3 limitedPosition = LimitPositionAroundPlayer(desiredPosition); // Player 기준 거리와 높이 제한 적용
        Vector3 currentPosition = activeCamera.transform.position; // 현재 Camera 위치 조회
        Vector3 movement = limitedPosition - currentPosition; // 이번 프레임 Camera 이동 벡터 계산
        float movementDistance = movement.magnitude; // Camera 이동 거리 계산

        if (movementDistance <= 0.0001f) // 유효한 이동 거리 확인
        {
            return; // Camera 이동 생략
        }

        Vector3 movementDirection = movement / movementDistance; // Camera 이동 방향 정규화
        Vector3 resolvedPosition = limitedPosition; // 기본 최종 Camera 위치 설정

        bool hasCollision = Physics.SphereCast(
            currentPosition,
            collisionRadius,
            movementDirection,
            out RaycastHit collisionHit,
            movementDistance,
            collisionLayerMask,
            QueryTriggerInteraction.Ignore); // 현재 위치에서 목표 위치까지 Camera 충돌 검사

        if (hasCollision) // Camera 이동 경로 충돌 확인
        {
            float safeDistance = Mathf.Max(0f, collisionHit.distance - collisionPadding); // 충돌 지점 앞 안전 거리 계산
            resolvedPosition = currentPosition + movementDirection * safeDistance; // 안전 거리 기준 최종 위치 계산
        }

        activeCamera.transform.position =
            LimitPositionAroundPlayer(resolvedPosition); // 최종 거리와 높이 제한을 적용한 Camera 위치 저장
    }

    private Vector3 LimitPositionAroundPlayer(Vector3 position) // Player 기준 자유 Camera 이동 범위 제한
    {
        Vector3 playerPosition = playerTransform.position; // 현재 Player 위치 조회
        float minimumY = playerPosition.y + minimumHeightFromPlayer; // 허용 최소 Camera 높이 계산
        float maximumY = playerPosition.y + maximumHeightFromPlayer; // 허용 최대 Camera 높이 계산
        position.y = Mathf.Clamp(position.y, minimumY, maximumY); // Camera 높이 범위 제한

        Vector3 playerToCamera = position - playerPosition; // Player에서 Camera까지 벡터 계산

        if (playerToCamera.magnitude > maximumDistanceFromPlayer) // 최대 이동 거리 초과 확인
        {
            position = playerPosition + playerToCamera.normalized * maximumDistanceFromPlayer; // Player 주변 최대 거리 안으로 제한
            position.y = Mathf.Clamp(position.y, minimumY, maximumY); // 거리 제한 후 높이 다시 보정
        }

        return position; // 제한된 Camera 위치 반환
    }

    private void SuspendNormalCameraControl() // 기존 1·3인칭 Camera 제어 정지
    {
        if (thirdPersonCameraFollow != null
            && thirdPersonCameraFollow.gameObject == activeCamera.gameObject) // 현재 Camera의 3인칭 제어기 확인
        {
            thirdPersonCameraFollow.SetExternalCameraControl(true); // 3인칭 내부 시점 값을 유지한 외부 제어 시작
        }

        CaptureAndDisableBehaviours(additionalCameraBehavioursToSuspend); // 추가 1인칭 또는 시점 전환 컴포넌트 정지
    }

    private void RestoreNormalCameraControl() // 기존 1·3인칭 Camera 제어 복구
    {
        RestoreCapturedBehaviours(additionalCameraBehavioursToSuspend); // 추가 Camera 컴포넌트 기존 상태 복구

        if (thirdPersonCameraFollow != null) // 3인칭 Camera 제어기 존재 확인
        {
            thirdPersonCameraFollow.SetExternalCameraControl(false); // 기존 Yaw, Pitch와 거리 상태로 복귀
        }
    }

    private void SuspendGameplayBehaviours() // 건축 중 Gameplay 기능 정지
    {
        CaptureAndDisableBehaviours(gameplayBehavioursToDisable); // 설정된 Gameplay 컴포넌트 기존 상태 저장 후 비활성화
    }

    private void RestoreGameplayBehaviours() // 건축 종료 후 Gameplay 기능 복구
    {
        RestoreCapturedBehaviours(gameplayBehavioursToDisable); // 설정된 Gameplay 컴포넌트 기존 상태 복구
    }

    private void CaptureAndDisableBehaviours(Behaviour[] behaviours) // 컴포넌트 상태 저장과 비활성화
    {
        if (behaviours == null) // 대상 목록 존재 확인
        {
            return; // 비활성화 처리 생략
        }

        for (int index = 0; index < behaviours.Length; index++) // 전체 대상 컴포넌트 순회
        {
            Behaviour targetBehaviour = behaviours[index]; // 현재 대상 컴포넌트 조회

            if (targetBehaviour == null
                || targetBehaviour == this
                || targetBehaviour is BuildPlacementController
                || targetBehaviour is BuildModeCameraController
                || targetBehaviour == thirdPersonCameraFollow) // 잘못된 대상과 필수 건축 컴포넌트 확인
            {
                continue; // 비활성화하면 안 되는 컴포넌트 제외
            }

            if (previousBehaviourStates.ContainsKey(targetBehaviour)) // 기존 저장 대상 확인
            {
                continue; // 중복 상태 저장 방지
            }

            previousBehaviourStates.Add(targetBehaviour, targetBehaviour.enabled); // 기존 활성 상태 저장
            targetBehaviour.enabled = false; // 건축 중 대상 컴포넌트 비활성화
        }
    }

    private void RestoreCapturedBehaviours(Behaviour[] behaviours) // 지정 목록의 기존 컴포넌트 상태 복구
    {
        if (behaviours == null) // 대상 목록 존재 확인
        {
            return; // 상태 복구 생략
        }

        for (int index = 0; index < behaviours.Length; index++) // 전체 대상 컴포넌트 순회
        {
            Behaviour targetBehaviour = behaviours[index]; // 현재 대상 컴포넌트 조회

            if (targetBehaviour == null) // 제거된 컴포넌트 확인
            {
                continue; // 제거된 대상 제외
            }

            if (!previousBehaviourStates.TryGetValue(targetBehaviour, out bool previousState)) // 기존 활성 상태 존재 확인
            {
                continue; // 저장되지 않은 대상 제외
            }

            targetBehaviour.enabled = previousState; // 건축 진입 전 활성 상태 복구
            previousBehaviourStates.Remove(targetBehaviour); // 복구 완료 상태 제거
        }
    }

    private Camera ResolveActiveCamera() // 현재 플레이어 시점 Camera 결정
    {
        if (preferredCamera != null
            && preferredCamera.isActiveAndEnabled) // Inspector 기본 Camera 활성 상태 확인
        {
            return preferredCamera; // 활성 기본 Camera 반환
        }

        if (Camera.main != null
            && Camera.main.isActiveAndEnabled) // MainCamera 태그 Camera 활성 상태 확인
        {
            return Camera.main; // 현재 Main Camera 반환
        }

        Camera[] cameras = FindObjectsByType<Camera>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None); // Scene의 활성 Camera 목록 검색

        for (int index = 0; index < cameras.Length; index++) // 전체 활성 Camera 순회
        {
            Camera candidate = cameras[index]; // 현재 Camera 조회

            if (candidate != null
                && candidate.isActiveAndEnabled
                && candidate.targetTexture == null) // Gameplay 화면 출력 Camera 확인
            {
                return candidate; // 첫 번째 Gameplay Camera 반환
            }
        }

        return null; // 활성 Gameplay Camera 검색 실패 반환
    }

    private void ResolveReferences() // 누락된 Scene 참조 자동 검색
    {
        if (playerTransform == null) // Player Transform 참조 확인
        {
            PlayerInventory playerInventory = FindFirstObjectByType<PlayerInventory>(
                FindObjectsInactive.Include); // Scene PlayerInventory 검색

            if (playerInventory != null) // PlayerInventory 검색 성공 확인
            {
                playerTransform = playerInventory.transform; // PlayerInventory가 있는 Transform을 Player로 사용
            }
        }

        if (thirdPersonCameraFollow == null) // 3인칭 Camera 참조 확인
        {
            thirdPersonCameraFollow = FindFirstObjectByType<ThirdPersonCameraFollow>(
                FindObjectsInactive.Include); // Scene 3인칭 Camera 제어기 검색
        }

        if (preferredCamera == null && thirdPersonCameraFollow != null) // 기본 Camera 미연결과 3인칭 Camera 존재 확인
        {
            preferredCamera = thirdPersonCameraFollow.GetComponent<Camera>(); // 3인칭 제어기가 있는 Camera 자동 연결
        }
    }

    private void ApplyBuildCursorState() // 건축 모드 마우스 포인터 상태 적용
    {
        Cursor.lockState = confineCursor
            ? CursorLockMode.Confined
            : CursorLockMode.None; // 건축 커서 고정 방식 적용

        Cursor.visible = true; // 건축 마우스 포인터 표시
    }

    private float NormalizeSignedAngle(float angle) // 0부터 360 회전을 -180부터 180 범위로 변환
    {
        if (angle > 180f) // 양수 180도 초과 확인
        {
            angle -= 360f; // 음수 회전값으로 변환
        }

        return angle; // 변환된 회전값 반환
    }

    private void OnDisable() // 자유 건축 Camera 비활성화 정리
    {
        if (!Application.isPlaying || !isActive) // Play Mode와 건축 Camera 상태 확인
        {
            return; // 정리할 건축 Camera 상태 없음
        }

        EndBuildMode(); // Camera, Gameplay와 커서 상태 복구
    }

    private void OnValidate() // Inspector 자유 Camera 값 검증
    {
        lookSensitivity = Mathf.Max(0.01f, lookSensitivity); // 회전 감도 최소값 적용
        minimumPitch = Mathf.Clamp(minimumPitch, -89f, 89f); // 최소 Pitch 범위 제한
        maximumPitch = Mathf.Clamp(maximumPitch, minimumPitch, 89f); // 최대 Pitch 역전 방지
        panSensitivity = Mathf.Max(0.0001f, panSensitivity); // 평행 이동 감도 최소값 적용
        wheelMoveStep = Mathf.Max(0.1f, wheelMoveStep); // 휠 이동 거리 최소값 적용
        maximumDistanceFromPlayer = Mathf.Max(1f, maximumDistanceFromPlayer); // Player 기준 최대 거리 최소값 적용
        maximumHeightFromPlayer = Mathf.Max(minimumHeightFromPlayer, maximumHeightFromPlayer); // 최대 높이 역전 방지
        collisionRadius = Mathf.Max(0.01f, collisionRadius); // 충돌 반지름 최소값 적용
        collisionPadding = Mathf.Max(0f, collisionPadding); // 충돌 여유 거리 음수 방지
    }
}
