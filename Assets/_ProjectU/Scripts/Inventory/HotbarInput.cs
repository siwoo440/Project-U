using UnityEngine; // Unity 기본 기능
using UnityEngine.InputSystem; // 새로운 입력 시스템

public sealed class HotbarInput : MonoBehaviour // 핫바 숫자키 입력 처리
{
    [Tooltip("선택 대상 인벤토리.")]
    [SerializeField] private PlayerInventory playerInventory; // 선택 대상 인벤토리
    [Tooltip("인벤토리 팝업 관리자.")]
    [SerializeField] private InventoryPopupController popupController; // 인벤토리 팝업 관리자

    private void Awake() // 입력 참조 검사
    {
        if (playerInventory == null || popupController == null) // 필수 참조 확인
        {
            Debug.LogError("HotbarInput의 참조를 모두 연결해야 합니다.", this); // 참조 누락 오류
            enabled = false; // 입력 기능 비활성화
        }
    }

    private void Update() // 숫자키 입력 검사
    {
        if (popupController.IsOpen) // 팝업 열림 확인
        {
            return; // 핫바 선택 차단
        }

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
}