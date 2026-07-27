using TMPro; // TextMeshPro UI 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.InputSystem; // 새 Input System 기능

public sealed class PlayerInteractor : MonoBehaviour // 플레이어 상호작용 처리
{
    [Header("Detection")] // 탐지 설정 묶음
    [SerializeField] private Transform interactionOrigin; // 탐지 시작 위치
    [SerializeField] private Transform viewTransform; // 탐지 방향 기준
    [SerializeField] private float interactionDistance = 2.5f; // 최대 상호작용 거리
    [SerializeField] private float detectionRadius = 0.25f; // 탐지 구체 반지름
    [SerializeField] private LayerMask interactableLayers; // 상호작용 대상 Layer

    [Header("Input")] // 입력 설정 묶음
    [SerializeField] private InputActionReference interactActionReference; // 상호작용 액션 참조

    [Header("UI")] // 안내 UI 설정 묶음
    [SerializeField] private GameObject promptRoot; // 안내 UI 루트
    [SerializeField] private TMP_Text promptText; // 안내 문구 Text

    private InteractableBase currentInteractable; // 현재 탐지 대상

    private void Awake() // 필수 참조 검사
    {
        bool hasMissingReference = interactionOrigin == null || viewTransform == null || interactActionReference == null || promptRoot == null || promptText == null; // 참조 누락 확인

        if (hasMissingReference) // 참조 누락 여부 확인
        {
            Debug.LogError("PlayerInteractor의 필수 참조를 모두 연결해야 합니다.", this); // 참조 누락 오류
            enabled = false; // 상호작용 기능 비활성화
            return; // 초기화 중단
        }

        if (interactableLayers.value == 0) // Layer Mask 설정 확인
        {
            Debug.LogError("PlayerInteractor의 Interactable Layers를 설정해야 합니다.", this); // Layer 누락 오류
            enabled = false; // 상호작용 기능 비활성화
            return; // 초기화 중단
        }

        promptRoot.SetActive(false); // 초기 안내 UI 숨김
        promptText.SetText(string.Empty); // 초기 안내 문구 제거
    }

    private void OnEnable() // 입력 활성화
    {
        if (interactActionReference == null) // 액션 참조 확인
        {
            return; // 활성화 중단
        }

        interactActionReference.action.Enable(); // 상호작용 액션 활성화
    }

    private void OnDisable() // 입력 비활성화
    {
        if (interactActionReference != null) // 액션 참조 확인
        {
            interactActionReference.action.Disable(); // 상호작용 액션 비활성화
        }

        ClearInteractable(); // 현재 대상 초기화
    }

    private void Update() // 매 프레임 상호작용 처리
    {
        DetectInteractable(); // 전방 대상 탐지

        if (currentInteractable == null) // 현재 대상 확인
        {
            return; // 입력 처리 중단
        }

        if (Cursor.lockState != CursorLockMode.Locked) // 게임 조작 상태 확인
        {
            return; // 입력 처리 중단
        }

        if (interactActionReference.action.WasPressedThisFrame()) // F 키 입력 확인
        {
            InteractableBase interactedObject = currentInteractable; // 실행할 대상 임시 저장
            ClearInteractable(); // 현재 대상과 안내 UI 초기화
            interactedObject.Interact(gameObject); // 저장된 대상과 상호작용
        }
    }

    private void DetectInteractable() // 전방 상호작용 대상 탐지
    {
        InteractableBase detectedInteractable = null; // 이번 프레임 탐지 대상
        Vector3 detectionDirection = viewTransform.forward.normalized; // 카메라 시선 방향 계산
        bool hasHit = Physics.SphereCast(interactionOrigin.position, detectionRadius, detectionDirection, out RaycastHit hit, interactionDistance, interactableLayers, QueryTriggerInteraction.Ignore); // 전방 구체 탐지

        if (hasHit) // Collider 탐지 여부 확인
        {
            detectedInteractable = hit.collider.GetComponentInParent<InteractableBase>(); // 상호작용 컴포넌트 검색
        }

        if (detectedInteractable == currentInteractable) // 대상 변경 여부 확인
        {
            return; // UI 갱신 중단
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
        }

        if (!hasInteractable) // 대상 부재 여부 확인
        {
            promptText.SetText(string.Empty); // 안내 문구 제거
        }
    }

    private void ClearInteractable() // 현재 대상 초기화
    {
        currentInteractable = null; // 현재 대상 제거

        if (promptRoot != null) // UI 루트 존재 확인
        {
            promptRoot.SetActive(false); // 안내 UI 숨김
        }

        if (promptText != null) // 안내 Text 존재 확인
        {
            promptText.SetText(string.Empty); // 안내 문구 제거
        }
    }

    private void OnDrawGizmosSelected() // 탐지 범위 시각화
    {
        if (interactionOrigin == null || viewTransform == null) // 탐지 기준 존재 확인
        {
            return; // Gizmo 표시 중단
        }

        Vector3 direction = viewTransform.forward.normalized; // 탐지 방향 계산
        Vector3 endPosition = interactionOrigin.position + direction * interactionDistance; // 탐지 종료 위치 계산
        Gizmos.color = Color.yellow; // Gizmo 색상 설정
        Gizmos.DrawLine(interactionOrigin.position, endPosition); // 탐지 방향선 표시
        Gizmos.DrawWireSphere(endPosition, detectionRadius); // 탐지 끝 구체 표시
    }
}