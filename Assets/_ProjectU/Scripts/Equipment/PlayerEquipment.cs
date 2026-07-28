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
    }

    private void OnValidate() // Inspector 값 검증
    {
        EnsureSlotCapacity(); // 장비 배열 크기 확인
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
        int removedQuantity = playerInventory.RemoveItemFromSlot(inventoryIndex, 1); // 인벤토리 장비 제거

        if (removedQuantity != 1) // 제거 결과 확인
        {
            return false; // 장착 실패 반환
        }

        equippedItems[targetSlotIndex] = newEquipment; // 새로운 장비 장착

        if (previousEquipment != null) // 기존 장비 존재 확인
        {
            int remainingQuantity = playerInventory.AddItem(previousEquipment, 1); // 기존 장비 인벤토리 이동

            if (remainingQuantity > 0) // 기존 장비 이동 실패 확인
            {
                equippedItems[targetSlotIndex] = previousEquipment; // 기존 장비 복구
                playerInventory.AddItem(newEquipment, 1); // 새로운 장비 인벤토리 복구
                return false; // 교체 실패 반환
            }
        }

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

        int remainingQuantity = playerInventory.AddItem(equippedItem, 1); // 장비 인벤토리 이동

        if (remainingQuantity > 0) // 인벤토리 공간 확인
        {
            return false; // 해제 실패 반환
        }

        equippedItems[slotIndex] = null; // 장비 슬롯 비우기
        EquipmentChanged?.Invoke(); // 장비 변경 알림
        return true; // 해제 성공 반환
    }

    private bool IsValidSlotType(EquipmentSlotType slotType) // 장비 슬롯 유효성 검사
    {
        int slotIndex = (int)slotType; // 장비 배열 번호 계산
        return slotIndex > (int)EquipmentSlotType.None && slotIndex < EquipmentSlotArraySize; // 정상 슬롯 여부 반환
    }
}