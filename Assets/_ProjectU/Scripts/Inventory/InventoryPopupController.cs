using UnityEngine; // Unity 기본 기능
using UnityEngine.InputSystem; // 새로운 입력 시스템

public sealed class InventoryPopupController : MonoBehaviour // 인벤토리 팝업 관리
{
    [SerializeField] private GameObject popupPanel; // 전체 인벤토리 팝업
    [SerializeField] private Behaviour[] blockedBehaviours; // 팝업 중 정지할 기능

    private bool[] previousBehaviourStates; // 기존 컴포넌트 활성 상태
    private bool isAltCursorActive; // Alt 커서 활성 상태
    private bool isInteractionActive; // UI 상호작용 활성 상태

    public bool IsOpen { get; private set; } // 팝업 열림 상태 제공

    private void Awake() // 팝업 초기화
    {
        if (popupPanel == null) // 팝업 연결 확인
        {
            Debug.LogError("InventoryPopupController의 Popup Panel을 연결해야 합니다.", this); // 참조 누락 오류
            enabled = false; // 팝업 기능 비활성화
            return; // 초기화 중단
        }

        int behaviourCount = blockedBehaviours == null ? 0 : blockedBehaviours.Length; // 차단 컴포넌트 개수 계산
        previousBehaviourStates = new bool[behaviourCount]; // 기존 상태 저장 배열 생성
        popupPanel.SetActive(false); // 시작 시 팝업 숨김
        IsOpen = false; // 시작 상태 저장
        isAltCursorActive = false; // 시작 Alt 상태 저장
        isInteractionActive = false; // 시작 상호작용 상태 저장
    }

    private void Update() // 팝업 입력 검사
    {
        Keyboard keyboard = Keyboard.current; // 현재 키보드 가져오기

        if (keyboard == null) // 키보드 존재 확인
        {
            return; // 입력 검사 중단
        }

        bool currentAltState = keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed; // 현재 Alt 입력 확인

        if (isAltCursorActive != currentAltState) // Alt 상태 변경 확인
        {
            isAltCursorActive = currentAltState; // 새로운 Alt 상태 저장
            RefreshInteractionState(); // 커서와 게임 조작 상태 갱신
        }

        if (IsOpen && keyboard.escapeKey.wasPressedThisFrame) // 열린 상태의 ESC 입력 확인
        {
            SetOpen(false); // 인벤토리 팝업 닫기
            return; // 같은 프레임의 추가 입력 차단
        }

        if (keyboard.iKey.wasPressedThisFrame) // I 키 입력 확인
        {
            SetOpen(!IsOpen); // 팝업 상태 반전
        }
    }

    public void SetOpen(bool shouldOpen) // 팝업 상태 변경
    {
        if (IsOpen == shouldOpen) // 동일 상태 확인
        {
            return; // 중복 변경 차단
        }

        IsOpen = shouldOpen; // 새로운 상태 저장
        popupPanel.SetActive(IsOpen); // 팝업 화면 상태 적용
        RefreshInteractionState(); // 커서와 게임 조작 상태 갱신
    }

    private void OnDisable() // 비활성화 상태 정리
    {
        if (!isInteractionActive) // 상호작용 활성 상태 확인
        {
            return; // 정리 처리 중단
        }

        RestoreBlockedBehaviours(); // 게임 조작 기능 복구
        Cursor.lockState = CursorLockMode.Locked; // 마우스 게임 화면 고정
        Cursor.visible = false; // 마우스 포인터 숨김
        isInteractionActive = false; // 상호작용 상태 해제
    }

    private void RefreshInteractionState() // UI 상호작용 상태 갱신
    {
        bool shouldActivateInteraction = IsOpen || isAltCursorActive; // 팝업과 Alt 기반 활성 상태 계산

        if (isInteractionActive == shouldActivateInteraction) // 동일 상태 확인
        {
            return; // 중복 변경 차단
        }

        isInteractionActive = shouldActivateInteraction; // 새로운 상호작용 상태 저장

        if (isInteractionActive) // 상호작용 활성화 확인
        {
            DisableBlockedBehaviours(); // 게임 조작 기능 정지
            Cursor.lockState = CursorLockMode.None; // 마우스 잠금 해제
            Cursor.visible = true; // 마우스 포인터 표시
            return; // 활성화 처리 종료
        }

        RestoreBlockedBehaviours(); // 게임 조작 기능 복구
        Cursor.lockState = CursorLockMode.Locked; // 마우스 게임 화면 고정
        Cursor.visible = false; // 마우스 포인터 숨김
    }

    private void DisableBlockedBehaviours() // 게임 조작 기능 정지
    {
        if (blockedBehaviours == null) // 차단 목록 확인
        {
            return; // 정지 처리 중단
        }

        for (int index = 0; index < blockedBehaviours.Length; index++) // 차단 목록 순회
        {
            Behaviour targetBehaviour = blockedBehaviours[index]; // 현재 대상 가져오기

            if (targetBehaviour == null) // 대상 연결 확인
            {
                continue; // 빈 대상 건너뛰기
            }

            previousBehaviourStates[index] = targetBehaviour.enabled; // 기존 활성 상태 저장
            targetBehaviour.enabled = false; // 대상 기능 정지
        }
    }

    private void RestoreBlockedBehaviours() // 게임 조작 기능 복구
    {
        if (blockedBehaviours == null) // 차단 목록 확인
        {
            return; // 복구 처리 중단
        }

        for (int index = 0; index < blockedBehaviours.Length; index++) // 차단 목록 순회
        {
            Behaviour targetBehaviour = blockedBehaviours[index]; // 현재 대상 가져오기

            if (targetBehaviour == null) // 대상 연결 확인
            {
                continue; // 빈 대상 건너뛰기
            }

            targetBehaviour.enabled = previousBehaviourStates[index]; // 기존 활성 상태 복구
        }
    }
}