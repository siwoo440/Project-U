using System; // 기본 이벤트 기능
using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class PlayerEquipment : MonoBehaviour // 플레이어 장비 관리
{
    private const int EquipmentSlotArraySize = 6; // 장비 배열 전체 크기

    [Header("References")] // 외부 참조 묶음
    [SerializeField] private PlayerInventory playerInventory; // 플레이어 인벤토리

    [Header("Runtime")] // 실행 상태 묶음
    [SerializeField] private ItemData[] equippedItems = new ItemData[EquipmentSlotArraySize]; // 장착 아이템 목록
    [SerializeField] private float totalDefensePercent; // 전체 방어력
    [SerializeField] private float totalMaximumHealthBonus; // 전체 최대 체력 증가량
    [SerializeField] private float totalMovementSpeedBonusPercent; // 전체 이동 속도 증가량
    [SerializeField] private float totalHungerReductionPercent; // 전체 허기 감소 방지량
    [SerializeField] private float totalThirstReductionPercent; // 전체 갈증 감소 방지량
    [SerializeField] private float totalColdResistancePercent; // 전체 방한 능력치
    [SerializeField] private int totalInventorySlotBonus; // 전체 인벤토리 추가 슬롯

    public float TotalDefensePercent => totalDefensePercent; // 전체 방어력 제공
    public float TotalMaximumHealthBonus => totalMaximumHealthBonus; // 전체 최대 체력 증가량 제공
    public float TotalMovementSpeedBonusPercent => totalMovementSpeedBonusPercent; // 전체 이동 속도 증가량 제공
    public float TotalHungerReductionPercent => totalHungerReductionPercent; // 전체 허기 감소 방지량 제공
    public float TotalThirstReductionPercent => totalThirstReductionPercent; // 전체 갈증 감소 방지량 제공
    public float TotalColdResistancePercent => totalColdResistancePercent; // 전체 방한 능력치 제공
    public int TotalInventorySlotBonus => totalInventorySlotBonus; // 전체 인벤토리 추가 슬롯 제공
    public event Action EquipmentChanged; // 장비 변경 알림

    private void Awake() // 장비 관리자 초기화
    {
        if (playerInventory == null) // 인벤토리 참조 확인
        {
            Debug.LogError($"{gameObject.name}의 PlayerInventory 참조가 누락되었습니다.", this); // 참조 오류 출력
            enabled = false; // 장비 기능 비활성화
            return; // 초기화 중단
        }

        EnsureSlotCapacity(); // 장비 배열 크기 확인
        RefreshTotalStats(); // 시작 장비 능력치 합산
        playerInventory.TrySetEquipmentBonusSlotCapacity(totalInventorySlotBonus); // 시작 가방 용량 적용
    }

    private void OnValidate() // Inspector 값 검증
    {
        EnsureSlotCapacity(); // 장비 배열 크기 확인
        RefreshTotalStats(); // Inspector 장비 능력치 합산
    }

    private void EnsureSlotCapacity() // 장비 배열 크기 보정
    {
        if (equippedItems != null && equippedItems.Length == EquipmentSlotArraySize) // 정상 배열 확인
        {
            return; // 배열 재생성 차단
        }

        ItemData[] previousItems = equippedItems; // 기존 장비 목록 저장
        equippedItems = new ItemData[EquipmentSlotArraySize]; // 새로운 장비 배열 생성

        if (previousItems == null) // 기존 배열 존재 확인
        {
            return; // 장비 복사 중단
        }

        int copyCount = Mathf.Min(previousItems.Length, equippedItems.Length); // 복사 개수 계산

        for (int index = 0; index < copyCount; index++) // 기존 장비 순회
        {
            equippedItems[index] = previousItems[index]; // 기존 장비 복사
        }
    }

    public ItemData GetEquippedItem(EquipmentSlotType slotType) // 지정 슬롯 장비 조회
    {
        if (!IsValidSlotType(slotType)) // 슬롯 종류 유효성 확인
        {
            return null; // 장비 없음 반환
        }

        return equippedItems[(int)slotType]; // 장착 아이템 반환
    }

    public bool TryEquipFromInventory(int inventoryIndex) // 인벤토리 장비 장착 시도
    {
        InventorySlot inventorySlot = playerInventory.GetSlot(inventoryIndex); // 대상 인벤토리 슬롯 조회

        if (inventorySlot == null) // 아이템 존재 확인
        {
            return false; // 장착 실패 반환
        }

        ItemData newEquipment = inventorySlot.ItemData; // 장착할 아이템 조회

        if (newEquipment == null || !newEquipment.IsEquipment) // 장비 분류 확인
        {
            return false; // 장착 실패 반환
        }

        EquipmentSlotType targetSlotType = newEquipment.EquipmentSlotType; // 대상 장비 슬롯 조회

        if (!IsValidSlotType(targetSlotType)) // 장비 슬롯 설정 확인
        {
            return false; // 장착 실패 반환
        }

        int targetSlotIndex = (int)targetSlotType; // 장비 배열 번호 계산
        ItemData previousEquipment = equippedItems[targetSlotIndex]; // 기존 장착 장비 저장
        int previousBackpackBonus = previousEquipment == null ? 0 : previousEquipment.InventorySlotBonus; // 기존 가방 용량 저장
        int newBackpackBonus = newEquipment.InventorySlotBonus; // 새로운 가방 용량 조회
        int removedQuantity = playerInventory.RemoveItemFromSlot(inventoryIndex, 1); // 인벤토리 장비 제거

        if (removedQuantity != 1) // 제거 결과 확인
        {
            return false; // 장착 실패 반환
        }

        if (targetSlotType == EquipmentSlotType.Backpack) // 가방 슬롯 확인
        {
            bool capacityChanged = playerInventory.TrySetEquipmentBonusSlotCapacity(newBackpackBonus); // 새로운 가방 용량 적용

            if (!capacityChanged) // 용량 변경 실패 확인
            {
                playerInventory.AddItem(newEquipment, 1); // 제거한 가방 복구
                return false; // 가방 장착 실패
            }
        }

        equippedItems[targetSlotIndex] = newEquipment; // 새로운 장비 장착

        if (previousEquipment != null) // 기존 장비 존재 확인
        {
            int remainingQuantity = playerInventory.AddItem(previousEquipment, 1); // 기존 장비 인벤토리 이동

            if (remainingQuantity > 0) // 기존 장비 이동 실패 확인
            {
                equippedItems[targetSlotIndex] = previousEquipment; // 기존 장비 복구

                if (targetSlotType == EquipmentSlotType.Backpack) // 가방 교체 실패 확인
                {
                    playerInventory.TrySetEquipmentBonusSlotCapacity(previousBackpackBonus); // 기존 가방 용량 복구
                }

                playerInventory.AddItem(newEquipment, 1); // 새로운 장비 인벤토리 복구
                return false; // 교체 실패 반환
            }
        }

        RefreshTotalStats(); // 장착 후 능력치 갱신
        EquipmentChanged?.Invoke(); // 장비 변경 알림
        return true; // 장착 성공 반환
    }

    public bool TryUnequip(EquipmentSlotType slotType) // 장비 해제 시도
    {
        if (!IsValidSlotType(slotType)) // 슬롯 종류 유효성 확인
        {
            return false; // 해제 실패 반환
        }

        int slotIndex = (int)slotType; // 장비 배열 번호 계산
        ItemData equippedItem = equippedItems[slotIndex]; // 현재 장비 조회

        if (equippedItem == null) // 장비 존재 확인
        {
            return false; // 해제 실패 반환
        }

        int previousBackpackBonus = equippedItem.InventorySlotBonus; // 기존 가방 증가량 저장

        if (slotType == EquipmentSlotType.Backpack) // 가방 해제 확인
        {
            bool capacityReduced = playerInventory.TrySetEquipmentBonusSlotCapacity(0); // 기본 인벤토리 용량 복구

            if (!capacityReduced) // 추가 슬롯 아이템 확인
            {
                return false; // 가방 해제 차단
            }
        }

        int remainingQuantity = playerInventory.AddItem(equippedItem, 1); // 장비 인벤토리 이동

        if (remainingQuantity > 0) // 인벤토리 공간 확인
        {
            if (slotType == EquipmentSlotType.Backpack) // 가방 반환 실패 확인
            {
                playerInventory.TrySetEquipmentBonusSlotCapacity(previousBackpackBonus); // 가방 용량 복구
            }

            return false; // 해제 실패 반환
        }

        equippedItems[slotIndex] = null; // 장비 슬롯 비우기
        RefreshTotalStats(); // 해제 후 능력치 갱신
        EquipmentChanged?.Invoke(); // 장비 변경 알림
        return true; // 해제 성공 반환
    }
    public bool ClearEquipmentForLoad() // 불러오기 전 전체 장비 초기화
    {
        EnsureSlotCapacity(); // 장비 배열 크기 확인

        if (!playerInventory.TrySetEquipmentBonusSlotCapacity(0)) // 가방 추가 용량 초기화
        {
            return false; // 장비 초기화 실패
        }

        for (int index = 0; index < equippedItems.Length; index++) // 전체 장비 슬롯 순회
        {
            equippedItems[index] = null; // 현재 장비 제거
        }

        RefreshTotalStats(); // 장비 능력치 초기화
        EquipmentChanged?.Invoke(); // 장비 변경 알림
        return true; // 장비 초기화 성공
    }

    public bool TrySetEquippedItemForLoad(ItemData itemData) // 저장된 장비 직접 복원
    {
        if (itemData == null || !itemData.IsEquipment) // 장비 데이터 여부 확인
        {
            return false; // 장비 복원 실패
        }

        EquipmentSlotType slotType = itemData.EquipmentSlotType; // 아이템 장비 슬롯 조회

        if (!IsValidSlotType(slotType)) // 장비 슬롯 유효성 확인
        {
            return false; // 장비 복원 실패
        }

        int slotIndex = (int)slotType; // 장비 배열 번호 계산

        if (equippedItems[slotIndex] != null) // 기존 장비 존재 확인
        {
            return false; // 동일 슬롯 중복 복원 차단
        }

        if (slotType == EquipmentSlotType.Backpack) // 가방 장비 여부 확인
        {
            bool capacityChanged = playerInventory.TrySetEquipmentBonusSlotCapacity(itemData.InventorySlotBonus); // 가방 추가 슬롯 적용

            if (!capacityChanged) // 가방 용량 적용 결과 확인
            {
                return false; // 가방 복원 실패
            }
        }

        equippedItems[slotIndex] = itemData; // 장비 슬롯에 아이템 적용
        RefreshTotalStats(); // 전체 장비 능력치 재계산
        EquipmentChanged?.Invoke(); // 장비 변경 알림
        return true; // 장비 복원 성공
    }
    private void RefreshTotalStats() // 장착 장비 능력치 합산
    {
        totalDefensePercent = 0f; // 방어력 초기화
        totalMaximumHealthBonus = 0f; // 최대 체력 증가량 초기화
        totalMovementSpeedBonusPercent = 0f; // 이동 속도 증가량 초기화
        totalHungerReductionPercent = 0f; // 허기 감소 방지량 초기화
        totalThirstReductionPercent = 0f; // 갈증 감소 방지량 초기화
        totalColdResistancePercent = 0f; // 방한 능력치 초기화
        totalInventorySlotBonus = 0; // 인벤토리 증가량 초기화

        for (int index = 0; index < equippedItems.Length; index++) // 장비 슬롯 순회
        {
            ItemData equippedItem = equippedItems[index]; // 현재 장비 조회

            if (equippedItem == null) // 빈 슬롯 확인
            {
                continue; // 빈 슬롯 제외
            }

            totalDefensePercent += equippedItem.DefensePercent; // 방어력 합산
            totalMaximumHealthBonus += equippedItem.MaximumHealthBonus; // 최대 체력 증가량 합산
            totalMovementSpeedBonusPercent += equippedItem.MovementSpeedBonusPercent; // 이동 속도 증가량 합산
            totalHungerReductionPercent += equippedItem.HungerDepletionReductionPercent; // 허기 감소 방지량 합산
            totalThirstReductionPercent += equippedItem.ThirstDepletionReductionPercent; // 갈증 감소 방지량 합산
            totalColdResistancePercent += equippedItem.ColdResistancePercent; // 방한 능력치 합산
            totalInventorySlotBonus += equippedItem.InventorySlotBonus; // 인벤토리 증가량 합산
        }

        totalDefensePercent = Mathf.Clamp(totalDefensePercent, 0f, 80f); // 전체 방어력 제한
        totalMaximumHealthBonus = Mathf.Max(0f, totalMaximumHealthBonus); // 전체 체력 증가량 보정
        totalMovementSpeedBonusPercent = Mathf.Max(0f, totalMovementSpeedBonusPercent); // 전체 이동 속도 증가량 보정
        totalHungerReductionPercent = Mathf.Clamp(totalHungerReductionPercent, 0f, 80f); // 전체 허기 감소 방지 제한
        totalThirstReductionPercent = Mathf.Clamp(totalThirstReductionPercent, 0f, 80f); // 전체 갈증 감소 방지 제한
        totalColdResistancePercent = Mathf.Clamp(totalColdResistancePercent, 0f, 80f); // 전체 방한 능력치 제한
        totalInventorySlotBonus = Mathf.Max(0, totalInventorySlotBonus); // 전체 인벤토리 증가량 보정
    }

    private bool IsValidSlotType(EquipmentSlotType slotType) // 장비 슬롯 유효성 검사
    {
        int slotIndex = (int)slotType; // 장비 배열 번호 계산
        return slotIndex > (int)EquipmentSlotType.None && slotIndex < EquipmentSlotArraySize; // 정상 슬롯 여부 반환
    }
}
