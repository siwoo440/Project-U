using TMPro; // TextMeshPro UI 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.InputSystem; // 새 Input System 기능

public sealed class PlayerInteractor : MonoBehaviour // 플레이어 공격 상호작용 처리
{
    [Header("Detection")] // 탐지 설정 묶음
    [Tooltip("탐지 시작 위치.")]
    [SerializeField] private Transform interactionOrigin; // 탐지 시작 위치
    [Tooltip("탐지 방향 기준.")]
    [SerializeField] private Transform viewTransform; // 탐지 방향 기준
    [Tooltip("최대 상호작용 거리.")]
    [SerializeField] private float interactionDistance = 1.8f; // 최대 상호작용 거리
    [Tooltip("탐지 구체 반지름.")]
    [SerializeField] private float detectionRadius = 0.35f; // 탐지 구체 반지름
    [Tooltip("상호작용 대상 레이어.")]
    [SerializeField] private LayerMask interactableLayers; // 상호작용 대상 레이어

    [Header("Input")] // 입력 설정 묶음
    [Tooltip("F키 상호작용 액션 참조.")]
    [SerializeField] private InputActionReference interactActionReference; // F키 상호작용 액션 참조
    [Tooltip("좌클릭 공격 액션 참조.")]
    [SerializeField] private InputActionReference attackActionReference; // 좌클릭 공격 액션 참조

    [Header("Attack")] // 공격 연출 설정 묶음
    [Tooltip("도구 휘두르기 연출.")]
    [SerializeField] private ToolSwingAnimation toolSwingAnimation; // 도구 휘두르기 연출
    [Tooltip("건축 배치 관리자.")]
    [SerializeField] private BuildPlacementController buildPlacementController; // 건축 배치 관리자

    [Header("UI")] // 안내 UI 설정 묶음
    [Tooltip("안내 UI 루트.")]
    [SerializeField] private GameObject promptRoot; // 안내 UI 루트
    [Tooltip("안내 문구 텍스트.")]
    [SerializeField] private TMP_Text promptText; // 안내 문구 텍스트

    private InteractableBase currentInteractable; // 현재 탐지 대상
    private readonly RaycastHit[] detectionHits = new RaycastHit[16]; // 상호작용 탐지 결과 배열
    private void Awake() // 필수 참조 검사
    {
        bool hasMissingReference = 
            interactionOrigin == null 
            || viewTransform == null 
            || interactActionReference == null 
            || attackActionReference == null
            || promptRoot == null
            || promptText == null
            || buildPlacementController == null; // 필수 참조 누락 확인

        if (hasMissingReference) // 참조 누락 여부 확인
        {
            Debug.LogError("PlayerInteractor의 필수 참조를 모두 연결해야 합니다.", this); // 참조 누락 오류
            enabled = false; // 상호작용 기능 비활성화
            return; // 초기화 중단
        }

        if (interactableLayers.value == 0) // 레이어 마스크 설정 확인
        {
            Debug.LogError("PlayerInteractor의 Interactable Layers를 설정해야 합니다.", this); // 레이어 누락 오류
            enabled = false; // 상호작용 기능 비활성화
            return; // 초기화 중단
        }

        if (toolSwingAnimation == null) // 휘두르기 연출 연결 확인
        {
            toolSwingAnimation = GetComponentInChildren<ToolSwingAnimation>(true); // 자식에서 휘두르기 연출 검색
        }

        promptRoot.SetActive(false); // 초기 안내 UI 숨김
        promptText.SetText(string.Empty); // 초기 안내 문구 제거
    }

    private void OnEnable() // 입력 액션 활성화
    {
        if (interactActionReference != null) // 상호작용 액션 존재 확인
        {
            interactActionReference.action.Enable(); // F키 상호작용 활성화
        }

        if (attackActionReference != null) // 공격 액션 존재 확인
        {
            attackActionReference.action.Enable(); // 좌클릭 공격 활성화
        }
    }

    private void OnDisable() // 입력 액션 비활성화
    {
        if (interactActionReference != null) // 상호작용 액션 존재 확인
        {
            interactActionReference.action.Disable(); // F키 상호작용 비활성화
        }

        if (attackActionReference != null) // 공격 액션 존재 확인
        {
            attackActionReference.action.Disable(); // 좌클릭 공격 비활성화
        }

        ClearInteractable(); // 현재 대상 초기화
    }

    private void Update() // 매 프레임 입력과 상호작용 처리
    {
        if (buildPlacementController.BlocksGameplayInput) // 건축 입력 차단 상태 확인
        {
            ClearInteractable(); // 상호작용 대상과 안내 UI 제거
            return; // 공격과 상호작용 차단
        }

        DetectInteractable(); // 전방 상호작용 대상 탐지

        if (Cursor.lockState != CursorLockMode.Locked) // 게임 조작 상태 확인
        {
            return; // UI 조작 중 입력 차단
        }

        if (attackActionReference.action.WasPressedThisFrame()) // 좌클릭 입력 확인
        {
            HandleAttackInput(); // 공격 및 자원 채집 처리
        }

        if (interactActionReference.action.WasPressedThisFrame()) // F키 입력 확인
        {
            HandleInteractInput(); // 아이템 및 일반 상호작용 처리
        }
    }
    private void HandleAttackInput() // 좌클릭 공격 처리
    {
        PlayAttackAnimation(); // 도구 휘두르기 실행

        if (currentInteractable is not GatherableResource) // 채집 자원 여부 확인
        {
            return; // 자원이 아닌 대상 상호작용 차단
        }

        InteractableBase resource = currentInteractable; // 채집 대상 임시 저장
        ClearInteractable(); // 현재 대상과 안내 UI 초기화
        resource.Interact(gameObject); // 자원 채집 실행
    }

    private void HandleInteractInput() // F키 상호작용 처리
    {
        if (currentInteractable == null) // 현재 대상 존재 확인
        {
            return; // 상호작용 처리 중단
        }

        if (currentInteractable is GatherableResource) // 채집 자원 여부 확인
        {
            return; // F키 자원 채집 차단
        }

        InteractableBase interactable = currentInteractable; // 일반 대상 임시 저장
        ClearInteractable(); // 현재 대상과 안내 UI 초기화
        interactable.Interact(gameObject); // 아이템 획득 또는 일반 상호작용 실행
    }

    private void PlayAttackAnimation() // 공격 휘두르기 실행
    {
        if (toolSwingAnimation == null) // 휘두르기 연출 존재 확인
        {
            return; // 연출 처리 중단
        }

        toolSwingAnimation.PlaySwing(); // 도구 휘두르기 시작
    }

    private void DetectInteractable() // 전방 상호작용 대상 탐지
    {
        InteractableBase detectedInteractable = null; // 이번 프레임 탐지 대상
        Vector3 detectionDirection = viewTransform.forward.normalized; // 카메라 시선 방향 계산

        int hitCount = Physics.SphereCastNonAlloc(
            interactionOrigin.position,
            detectionRadius,
            detectionDirection,
            detectionHits,
            interactionDistance,
            interactableLayers,
            QueryTriggerInteraction.Ignore); // 전방 범위의 전체 충돌체 탐지

        float nearestDistance = float.MaxValue; // 가장 가까운 상호작용 거리

        for (int index = 0; index < hitCount; index++) // 탐지된 충돌체 순회
        {
            RaycastHit currentHit = detectionHits[index]; // 현재 충돌 정보

            if (currentHit.collider == null) // 충돌체 존재 확인
            {
                continue; // 잘못된 결과 제외
            }

            InteractableBase candidate =
                currentHit.collider.GetComponentInParent<InteractableBase>(); // 상호작용 대상 검색

            if (candidate == null || !candidate.isActiveAndEnabled) // 사용 가능한 대상 확인
            {
                continue; // 바닥과 일반 건축 Collider 제외
            }

            if (currentHit.distance >= nearestDistance) // 기존 대상보다 가까운지 확인
            {
                continue; // 더 먼 대상 제외
            }

            nearestDistance = currentHit.distance; // 가장 가까운 거리 갱신
            detectedInteractable = candidate; // 가장 가까운 상호작용 대상 저장
        }

        if (detectedInteractable == currentInteractable) // 동일 대상 유지 확인
        {
            RefreshPrompt(); // 변경된 상태 문구 갱신
            return; // 대상 교체 처리 중단
        }

        currentInteractable = detectedInteractable; // 현재 대상 갱신
        RefreshPrompt(); // 안내 UI 갱신
    }

    private void RefreshPrompt() // 안내 UI 갱신
    {
        bool hasInteractable = currentInteractable != null; // 대상 존재 여부 확인
        promptRoot.SetActive(hasInteractable); // 대상 존재에 따른 UI 표시

        if (hasInteractable) // 대상 존재 여부 확인
        {
            promptText.SetText(currentInteractable.PromptMessage); // 대상 안내 문구 표시
            return; // 빈 문구 처리 생략
        }

        promptText.SetText(string.Empty); // 안내 문구 제거
    }

    private void ClearInteractable() // 현재 대상 초기화
    {
        currentInteractable = null; // 현재 대상 제거

        if (promptRoot != null) // UI 루트 존재 확인
        {
            promptRoot.SetActive(false); // 안내 UI 숨김
        }

        if (promptText != null) // 안내 텍스트 존재 확인
        {
            promptText.SetText(string.Empty); // 안내 문구 제거
        }
    }

    private void OnDrawGizmosSelected() // 탐지 범위 시각화
    {
        if (interactionOrigin == null || viewTransform == null) // 탐지 기준 존재 확인
        {
            return; // 기즈모 표시 중단
        }

        Vector3 direction = viewTransform.forward.normalized; // 탐지 방향 계산
        Vector3 endPosition = interactionOrigin.position + direction * interactionDistance; // 탐지 종료 위치 계산
        Gizmos.color = Color.red; // 공격 탐지 색상 설정
        Gizmos.DrawLine(interactionOrigin.position, endPosition); // 탐지 방향선 표시
        Gizmos.DrawWireSphere(endPosition, detectionRadius); // 탐지 끝 범위 표시
    }
}