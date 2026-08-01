using System; // 열거형과 문자열 검사 기능
using System.Collections.Generic; // 중복 슬롯 검사 기능
using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class InventorySaveBridge : MonoBehaviour // 인벤토리와 장비 저장 연결 관리
{
    [Header("References")] // 외부 참조 묶음
    [Tooltip("플레이어 인벤토리.")]
    [SerializeField] private PlayerInventory playerInventory; // 플레이어 인벤토리
    [Tooltip("플레이어 장비 관리자.")]
    [SerializeField] private PlayerEquipment playerEquipment; // 플레이어 장비 관리자
    [Tooltip("아이템 ID 데이터베이스.")]
    [SerializeField] private ItemDatabase itemDatabase; // 아이템 ID 데이터베이스

    public bool TryValidateSetup(out string errorMessage) // 저장 연결 참조 검사
    {
        if (playerInventory == null) // 인벤토리 참조 확인
        {
            errorMessage = "PlayerInventory 참조가 누락되었습니다."; // 인벤토리 오류 저장
            return false; // 검사 실패
        }

        if (playerEquipment == null) // 장비 참조 확인
        {
            errorMessage = "PlayerEquipment 참조가 누락되었습니다."; // 장비 오류 저장
            return false; // 검사 실패
        }

        if (itemDatabase == null) // 데이터베이스 참조 확인
        {
            errorMessage = "ItemDatabase 참조가 누락되었습니다."; // 데이터베이스 오류 저장
            return false; // 검사 실패
        }

        return itemDatabase.TryValidate(out errorMessage); // 아이템 데이터베이스 검사 결과 반환
    }

    public void Capture(SaveGameData saveData) // 현재 인벤토리와 장비 상태 수집
    {
        saveData.inventory.slots.Clear(); // 기존 인벤토리 저장 목록 초기화
        saveData.equipment.slots.Clear(); // 기존 장비 저장 목록 초기화
        saveData.inventory.selectedHotbarIndex = playerInventory.SelectedHotbarIndex; // 현재 핫바 선택 번호 저장

        for (int index = 0; index < playerInventory.Slots.Count; index++) // 전체 인벤토리 슬롯 순회
        {
            InventorySlot inventorySlot = playerInventory.GetSlot(index); // 현재 슬롯 조회

            if (inventorySlot == null || inventorySlot.ItemData == null) // 빈 슬롯 확인
            {
                continue; // 빈 슬롯 저장 제외
            }

            InventorySlotSaveData slotSaveData = new InventorySlotSaveData(); // 슬롯 저장 데이터 생성
            slotSaveData.slotIndex = index; // 실제 슬롯 번호 저장
            slotSaveData.itemId = inventorySlot.ItemData.ItemId; // 아이템 ID 저장
            slotSaveData.quantity = inventorySlot.Quantity; // 아이템 수량 저장
            saveData.inventory.slots.Add(slotSaveData); // 인벤토리 저장 목록 추가
        }

        for (int slotIndex = (int)EquipmentSlotType.Head; slotIndex <= (int)EquipmentSlotType.Backpack; slotIndex++) // 전체 장비 슬롯 순회
        {
            EquipmentSlotType slotType = (EquipmentSlotType)slotIndex; // 현재 장비 슬롯 종류 변환
            ItemData equippedItem = playerEquipment.GetEquippedItem(slotType); // 현재 장착 아이템 조회

            if (equippedItem == null) // 빈 장비 슬롯 확인
            {
                continue; // 빈 장비 저장 제외
            }

            EquipmentSlotSaveData equipmentSaveData = new EquipmentSlotSaveData(); // 장비 저장 데이터 생성
            equipmentSaveData.slotType = slotIndex; // 장비 슬롯 숫자값 저장
            equipmentSaveData.itemId = equippedItem.ItemId; // 장비 아이템 ID 저장
            saveData.equipment.slots.Add(equipmentSaveData); // 장비 저장 목록 추가
        }
    }

    public bool TryRestore(SaveGameData saveData, out string errorMessage) // 저장된 인벤토리와 장비 복원
    {
        if (!TryValidateSavedState(saveData, out errorMessage)) // 저장 아이템 데이터 검사
        {
            return false; // 복원 실패
        }

        playerInventory.ClearItemsForLoad(); // 기존 인벤토리 전체 제거

        if (!playerEquipment.ClearEquipmentForLoad()) // 기존 장비와 가방 용량 초기화
        {
            errorMessage = "기존 장비 상태를 초기화하지 못했습니다."; // 초기화 오류 저장
            return false; // 복원 실패
        }

        for (int index = 0; index < saveData.equipment.slots.Count; index++) // 저장 장비 목록 순회
        {
            EquipmentSlotSaveData equipmentSaveData = saveData.equipment.slots[index]; // 현재 장비 저장 데이터 조회
            itemDatabase.TryGetItem(equipmentSaveData.itemId, out ItemData itemData); // 장비 ID로 아이템 조회

            if (!playerEquipment.TrySetEquippedItemForLoad(itemData)) // 저장 장비 직접 적용
            {
                errorMessage = $"장비 복원에 실패했습니다: {equipmentSaveData.itemId}"; // 장비 복원 오류 저장
                return false; // 복원 실패
            }
        }

        for (int index = 0; index < saveData.inventory.slots.Count; index++) // 저장 인벤토리 목록 순회
        {
            InventorySlotSaveData slotSaveData = saveData.inventory.slots[index]; // 현재 슬롯 저장 데이터 조회
            itemDatabase.TryGetItem(slotSaveData.itemId, out ItemData itemData); // 아이템 ID로 데이터 조회

            if (!playerInventory.TrySetSlotForLoad(slotSaveData.slotIndex, itemData, slotSaveData.quantity)) // 지정 슬롯 복원
            {
                errorMessage = $"인벤토리 슬롯 복원에 실패했습니다: {slotSaveData.slotIndex}"; // 슬롯 복원 오류 저장
                return false; // 복원 실패
            }
        }

        playerInventory.SelectHotbarSlot(saveData.inventory.selectedHotbarIndex); // 저장된 핫바 선택 번호 적용
        errorMessage = string.Empty; // 오류 내용 초기화
        return true; // 전체 복원 성공
    }

    private bool TryValidateSavedState(SaveGameData saveData, out string errorMessage) // 저장 아이템 내용 검사
    {
        if (saveData == null || saveData.inventory == null || saveData.equipment == null) // 저장 묶음 존재 확인
        {
            errorMessage = "인벤토리 또는 장비 저장 데이터가 누락되었습니다."; // 저장 묶음 오류 저장
            return false; // 검사 실패
        }

        if (!itemDatabase.TryValidate(out errorMessage)) // 현재 아이템 데이터베이스 검사
        {
            return false; // 검사 실패
        }

        HashSet<int> usedEquipmentSlots = new HashSet<int>(); // 장비 슬롯 중복 검사 목록
        HashSet<int> usedInventorySlots = new HashSet<int>(); // 인벤토리 슬롯 중복 검사 목록
        int savedInventoryCapacity = playerInventory.BaseSlotCapacity; // 저장 상태 기본 인벤토리 용량

        for (int index = 0; index < saveData.equipment.slots.Count; index++) // 저장 장비 목록 순회
        {
            EquipmentSlotSaveData equipmentSaveData = saveData.equipment.slots[index]; // 현재 장비 저장 데이터 조회

            if (equipmentSaveData == null) // 장비 저장 항목 존재 확인
            {
                errorMessage = "비어 있는 장비 저장 항목이 있습니다."; // 빈 장비 오류 저장
                return false; // 검사 실패
            }

            if (!Enum.IsDefined(typeof(EquipmentSlotType), equipmentSaveData.slotType)) // 장비 슬롯 숫자값 확인
            {
                errorMessage = $"잘못된 장비 슬롯 번호입니다: {equipmentSaveData.slotType}"; // 장비 슬롯 오류 저장
                return false; // 검사 실패
            }

            EquipmentSlotType slotType = (EquipmentSlotType)equipmentSaveData.slotType; // 장비 슬롯 열거형 변환

            if (slotType == EquipmentSlotType.None) // 사용 불가능 슬롯 확인
            {
                errorMessage = "None 장비 슬롯에는 아이템을 저장할 수 없습니다."; // None 슬롯 오류 저장
                return false; // 검사 실패
            }

            if (!usedEquipmentSlots.Add(equipmentSaveData.slotType)) // 동일 장비 슬롯 중복 확인
            {
                errorMessage = $"중복 장비 슬롯이 있습니다: {slotType}"; // 장비 중복 오류 저장
                return false; // 검사 실패
            }

            if (!itemDatabase.TryGetItem(equipmentSaveData.itemId, out ItemData itemData)) // 장비 아이템 ID 조회
            {
                errorMessage = $"등록되지 않은 장비 Item ID입니다: {equipmentSaveData.itemId}"; // 장비 ID 오류 저장
                return false; // 검사 실패
            }

            if (!itemData.IsEquipment || itemData.EquipmentSlotType != slotType) // 아이템 장비 슬롯 일치 확인
            {
                errorMessage = $"장비 슬롯과 아이템이 일치하지 않습니다: {equipmentSaveData.itemId}"; // 장비 종류 오류 저장
                return false; // 검사 실패
            }

            if (slotType == EquipmentSlotType.Backpack) // 가방 장비 확인
            {
                savedInventoryCapacity += itemData.InventorySlotBonus; // 저장 상태 추가 슬롯 반영
            }
        }

        for (int index = 0; index < saveData.inventory.slots.Count; index++) // 저장 인벤토리 목록 순회
        {
            InventorySlotSaveData slotSaveData = saveData.inventory.slots[index]; // 현재 슬롯 저장 데이터 조회

            if (slotSaveData == null) // 슬롯 저장 항목 존재 확인
            {
                errorMessage = "비어 있는 인벤토리 저장 항목이 있습니다."; // 빈 슬롯 오류 저장
                return false; // 검사 실패
            }

            if (slotSaveData.slotIndex < 0 || slotSaveData.slotIndex >= savedInventoryCapacity) // 저장 슬롯 범위 확인
            {
                errorMessage = $"저장 슬롯 번호가 인벤토리 용량을 벗어났습니다: {slotSaveData.slotIndex}"; // 슬롯 범위 오류 저장
                return false; // 검사 실패
            }

            if (!usedInventorySlots.Add(slotSaveData.slotIndex)) // 동일 슬롯 중복 확인
            {
                errorMessage = $"중복 인벤토리 슬롯이 있습니다: {slotSaveData.slotIndex}"; // 슬롯 중복 오류 저장
                return false; // 검사 실패
            }

            if (!itemDatabase.TryGetItem(slotSaveData.itemId, out ItemData itemData)) // 인벤토리 아이템 ID 조회
            {
                errorMessage = $"등록되지 않은 Item ID입니다: {slotSaveData.itemId}"; // 아이템 ID 오류 저장
                return false; // 검사 실패
            }

            if (slotSaveData.quantity <= 0 || slotSaveData.quantity > itemData.MaximumStack) // 저장 수량 범위 확인
            {
                errorMessage = $"아이템 수량이 잘못되었습니다: {slotSaveData.itemId} x{slotSaveData.quantity}"; // 수량 오류 저장
                return false; // 검사 실패
            }
        }

        if (saveData.inventory.selectedHotbarIndex < 0 || saveData.inventory.selectedHotbarIndex >= playerInventory.HotbarSlotCount) // 핫바 선택 번호 확인
        {
            errorMessage = $"핫바 선택 번호가 잘못되었습니다: {saveData.inventory.selectedHotbarIndex}"; // 핫바 오류 저장
            return false; // 검사 실패
        }

        errorMessage = string.Empty; // 오류 내용 초기화
        return true; // 저장 아이템 검사 성공
    }
}