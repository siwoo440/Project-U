using UnityEngine; // Unity 기본 기능
using UnityEngine.InputSystem; // 새로운 입력 시스템

public sealed class HotbarInput : MonoBehaviour // 핫바 숫자키 입력 처리
{
    [Tooltip("선택 대상 인벤토리.")]
    [SerializeField] private PlayerInventory playerInventory; // 선택 대상 인벤토리
    [Tooltip("인벤토리 팝업 관리자.")]
    [SerializeField] private InventoryPopupController popupController; // 인벤토리 팝업 관리자
    [Tooltip("건축 모드 중 휠 입력을 막기 위한 건축 관리자. 비어 있으면 Scene에서 검색합니다.")]
    [SerializeField] private BuildPlacementController buildPlacementController; // 건축 관리자
    [Tooltip("휠 한 칸 이동 후 다음 이동까지의 최소 간격(초). 터치패드 연속 입력 방지용입니다.")]
    [SerializeField, Min(0f)] private float wheelStepInterval = 0.06f; // 휠 이동 간격

    private float nextWheelStepTime; // 다음 휠 이동 가능 시각

    public static bool IsZoomModifierPressed // 휠을 Camera 줌에 쓰는 보조키(Shift) 입력 여부 (Ctrl은 회피, Alt는 커서 모드)
    {
        get // 보조키 상태 조회
        {
            Keyboard keyboard = Keyboard.current; // 현재 키보드
            return keyboard != null && keyboard.shiftKey.isPressed; // Shift 입력 여부 반환
        }
    }

    private void Awake() // 입력 참조 검사
    {
        if (playerInventory == null || popupController == null) // 필수 참조 확인
        {
            Debug.LogError("HotbarInput의 참조를 모두 연결해야 합니다.", this); // 참조 누락 오류
            enabled = false; // 입력 기능 비활성화
            return; // 초기화 중단
        }

        if (buildPlacementController == null) // 건축 관리자 참조 확인
        {
            buildPlacementController = FindFirstObjectByType<BuildPlacementController>(); // Scene에서 검색 (없어도 동작)
        }
    }

    private void Update() // 숫자키와 마우스 휠 입력 검사
    {
        if (popupController.IsOpen) // 팝업 열림 확인
        {
            return; // 핫바 선택 차단
        }

        HandleWheelInput(); // 마우스 휠 칸 이동 처리

        Keyboard keyboard = Keyboard.current; // 현재 키보드 가져오기

        if (keyboard == null) // 키보드 존재 확인
        {
            return; // 입력 검사 중단
        }

        if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) // 숫자 1 입력 확인
        {
            playerInventory.SelectHotbarSlot(0); // 첫 번째 슬롯 선택
            return; // 입력 처리 종료
        }

        if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) // 숫자 2 입력 확인
        {
            playerInventory.SelectHotbarSlot(1); // 두 번째 슬롯 선택
            return; // 입력 처리 종료
        }

        if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) // 숫자 3 입력 확인
        {
            playerInventory.SelectHotbarSlot(2); // 세 번째 슬롯 선택
            return; // 입력 처리 종료
        }

        if (keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame) // 숫자 4 입력 확인
        {
            playerInventory.SelectHotbarSlot(3); // 네 번째 슬롯 선택
            return; // 입력 처리 종료
        }

        if (keyboard.digit5Key.wasPressedThisFrame || keyboard.numpad5Key.wasPressedThisFrame) // 숫자 5 입력 확인
        {
            playerInventory.SelectHotbarSlot(4); // 다섯 번째 슬롯 선택
            return; // 입력 처리 종료
        }

        if (keyboard.digit6Key.wasPressedThisFrame || keyboard.numpad6Key.wasPressedThisFrame) // 숫자 6 입력 확인
        {
            playerInventory.SelectHotbarSlot(5); // 여섯 번째 슬롯 선택
            return; // 입력 처리 종료
        }

        if (keyboard.digit7Key.wasPressedThisFrame || keyboard.numpad7Key.wasPressedThisFrame) // 숫자 7 입력 확인
        {
            playerInventory.SelectHotbarSlot(6); // 일곱 번째 슬롯 선택
            return; // 입력 처리 종료
        }

        if (keyboard.digit8Key.wasPressedThisFrame || keyboard.numpad8Key.wasPressedThisFrame) // 숫자 8 입력 확인
        {
            playerInventory.SelectHotbarSlot(7); // 여덟 번째 슬롯 선택
        }
    }

    private void HandleWheelInput() // 휠 위쪽은 1번 방향, 아래쪽은 8번 방향으로 선택 이동
    {
        Mouse mouse = Mouse.current; // 현재 마우스

        if (mouse == null || Cursor.lockState != CursorLockMode.Locked) // 게임 조작 상태 확인 (UI 조작 중 차단)
        {
            return; // 휠 처리 중단
        }

        if (buildPlacementController != null && buildPlacementController.BlocksGameplayInput) // 건축 모드 확인
        {
            return; // 건축 Camera 휠과 충돌 방지
        }

        if (IsZoomModifierPressed) // Shift + 휠은 Camera 줌
        {
            return; // 핫바 이동 생략
        }

        float scroll = mouse.scroll.ReadValue().y; // 이번 프레임 휠 입력

        if (Mathf.Abs(scroll) < 0.01f || Time.unscaledTime < nextWheelStepTime) // 유효 입력과 간격 확인
        {
            return; // 이동 생략
        }

        int slotCount = playerInventory.HotbarSlotCount; // 핫바 칸 수

        if (slotCount <= 0) // 핫바 존재 확인
        {
            return; // 이동 생략
        }

        int direction = scroll > 0f ? -1 : 1; // 위쪽은 앞 번호, 아래쪽은 뒤 번호
        int nextIndex = (playerInventory.SelectedHotbarIndex + direction + slotCount) % slotCount; // 끝에서 반대편으로 순환
        playerInventory.SelectHotbarSlot(nextIndex); // 새 칸 선택
        nextWheelStepTime = Time.unscaledTime + wheelStepInterval; // 다음 이동 시각 기록
    }
}