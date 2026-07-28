using UnityEngine; // Unity 기본 기능
using UnityEngine.InputSystem; // 새로운 입력 시스템

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class HotbarItemUse : MonoBehaviour // 핫바 아이템 사용 처리
{
    [Header("References")] // 기능 참조 묶음
    [SerializeField] private PlayerInventory playerInventory; // 플레이어 인벤토리
    [SerializeField] private PlayerHunger playerHunger; // 플레이어 허기
    [SerializeField] private PlayerThirst playerThirst; // 플레이어 갈증
    [SerializeField] private PlayerHealth playerHealth; // 플레이어 체력
    [SerializeField] private InventoryPopupController popupController; // 인벤토리 팝업 관리자
    [SerializeField] private BuildPlacementController buildPlacementController; // 건축 배치 관리자


    private void Awake() // 필수 참조 검사
    {
        if (playerInventory == null 
            || playerHunger == null 
            || playerThirst == null 
            || playerHealth == null 
            || popupController == null
            || buildPlacementController == null) // 참조 누락 확인
        {
            Debug.LogError($"{gameObject.name}의 핫바 아이템 사용 참조가 누락되었습니다.", this); // 참조 오류 출력
            enabled = false; // 아이템 사용 기능 비활성화
        }
    }

    private void Update() // 아이템 사용 입력 검사
    {
        if (buildPlacementController.BlocksGameplayInput) // 건축 입력 차단 상태 확인
        {
            return; // 건축 중 핫바 아이템 사용 차단
        }

        if (popupController.IsOpen) // 인벤토리 팝업 확인
        {
            return; // 팝업 중 아이템 사용 차단
        }

        if (Cursor.lockState != CursorLockMode.Locked) // 게임 커서 상태 확인
        {
            return; // UI 조작 중 아이템 사용 차단
        }

        Mouse mouse = Mouse.current; // 현재 마우스 가져오기

        if (mouse == null) // 마우스 연결 확인
        {
            return; // 입력 검사 중단
        }

        if (!mouse.rightButton.wasPressedThisFrame) // 마우스 오른쪽 입력 확인
        {
            return; // 사용 입력 없음
        }

        TryUseSelectedItem(); // 선택 아이템 사용 시도
    }

    private void TryUseSelectedItem() // 선택 핫바 아이템 사용
    {
        if (playerHealth.IsDead) // 플레이어 사망 상태 확인
        {
            return; // 사망 후 아이템 사용 차단
        }

        int selectedIndex = playerInventory.SelectedHotbarIndex; // 선택 핫바 번호 조회
        InventorySlot selectedSlot = playerInventory.GetSlot(selectedIndex); // 선택 슬롯 조회

        if (selectedSlot == null) { return; } // 빈 슬롯 확인 -> 아이템 사용 중단

        ItemData itemData = selectedSlot.ItemData; // 선택 아이템 데이터 조회

        if (itemData.IsFood) // 음식 분류 확인
        {
            if (itemData.HungerRestoreAmount <= 0f) { return; }// 허기 회복 효과 확인 -> 효과 없는 음식 사용 차단
            bool eatSucceeded = playerHunger.TryEat(itemData.HungerRestoreAmount); // 허기 회복 시도
            if (!eatSucceeded) { return; }// 음식 사용 결과 확인 // 수량 감소 차단
            playerInventory.RemoveItemFromSlot(selectedIndex, 1); // 음식 한 개 소비
            return; // 음식 처리 종료
        }

        if (itemData.IsDrink) // 음료 분류 확인
        {
            if (itemData.ThirstRestoreAmount <= 0f) { return; }// 갈증 회복 효과 확인 -> 효과 없는 음료 사용 차단
            bool drinkSucceeded = playerThirst.TryDrink(itemData.ThirstRestoreAmount); // 갈증 회복 시도
            if (!drinkSucceeded) { return; } // 음료 사용 결과 확인 -> 수량 감소 차단
            playerInventory.RemoveItemFromSlot(selectedIndex, 1); // 음료 한 개 소비
            return; // 음료 처리 종료
        }

        if (itemData.IsMedicine) // 의약품 분류 확인
        {
            if (itemData.HealthRestoreAmount <= 0f) { return; } // 체력 회복 효과 확인// 효과 없는 의약품 사용 차단
            bool healSucceeded = playerHealth.Heal(itemData.HealthRestoreAmount); // 체력 회복 시도
            if (!healSucceeded) { return; }// 의약품 사용 결과 확인 // 수량 감소 차단
            playerInventory.RemoveItemFromSlot(selectedIndex, 1); // 의약품 한 개 소비
        }
    }
}