using System; // 기본 이벤트 기능
using System.Collections.Generic; // List 기능
using UnityEngine; // Unity 기본 기능

public sealed class PlayerInventory : MonoBehaviour // 플레이어 인벤토리 관리
{
    [Header("Capacity")] // 용량 설정 묶음
    [SerializeField] private int slotCapacity = 24; // 전체 인벤토리 슬롯 개수
    [SerializeField] private int hotbarSlotCount = 8; // 핫바 슬롯 개수

    private int selectedHotbarIndex; // 현재 선택 핫바 번호

    private readonly List<InventorySlot> slots = new List<InventorySlot>(); // 현재 인벤토리 슬롯 목록

    public IReadOnlyList<InventorySlot> Slots => slots; // 읽기 전용 슬롯 목록 제공
    public int SlotCapacity => slotCapacity; // 최대 슬롯 개수 제공
    public int HotbarSlotCount => Mathf.Min(hotbarSlotCount, slotCapacity); // 실제 핫바 슬롯 개수
    public int SelectedHotbarIndex => selectedHotbarIndex; // 선택 핫바 번호 제공

    public event Action HotbarSelectionChanged; // 핫바 선택 변경 알림
    public event Action InventoryChanged; // 인벤토리 변경 알림

    private void OnValidate() // Inspector 값 검증
    {
        slotCapacity = Mathf.Max(8, slotCapacity); // 전체 슬롯 최소값 보정
        hotbarSlotCount = Mathf.Clamp(hotbarSlotCount, 1, slotCapacity); // 핫바 슬롯 범위 보정
        selectedHotbarIndex = Mathf.Clamp(selectedHotbarIndex, 0, hotbarSlotCount - 1); // 선택 번호 범위 보정
    }

    public InventorySlot GetSlot(int index) // 지정 번호 슬롯 조회
    {
        if (index < 0 || index >= slots.Count) // 슬롯 범위 확인
        {
            return null; // 빈 슬롯 결과
        }

        return slots[index]; // 해당 슬롯 반환
    }

    public void SelectHotbarSlot(int index) // 핫바 슬롯 선택
    {
        if (index < 0 || index >= HotbarSlotCount) // 선택 범위 확인
        {
            return; // 잘못된 선택 차단
        }

        if (selectedHotbarIndex == index) // 같은 슬롯 재선택 확인
        {
            return; // 중복 변경 차단
        }

        selectedHotbarIndex = index; // 선택 번호 저장
        HotbarSelectionChanged?.Invoke(); // 선택 변경 알림
    }

    public int AddItem(ItemData itemData, int amount) // 아이템 추가 시도
    {
        if (itemData == null || amount <= 0) // 유효한 요청 확인
        {
            return amount; // 전체 수량 반환
        }

        int remainingAmount = amount; // 아직 넣지 못한 수량

        for (int index = 0; index < slots.Count; index++) // 기존 슬롯 순회
        {
            InventorySlot slot = slots[index]; // 현재 슬롯 가져오기

            if (!slot.Contains(itemData) || slot.IsFull) // 추가 불가능 슬롯 확인
            {
                continue; // 다음 슬롯 검사
            }

            remainingAmount = slot.AddQuantity(remainingAmount); // 기존 슬롯에 수량 추가

            if (remainingAmount <= 0) // 전체 추가 완료 확인
            {
                break; // 기존 슬롯 검사 종료
            }
        }

        while (remainingAmount > 0 && slots.Count < slotCapacity) // 신규 슬롯 생성 가능 여부
        {
            int newSlotQuantity = Mathf.Min(remainingAmount, itemData.MaximumStack); // 신규 슬롯 수량 계산
            InventorySlot newSlot = new InventorySlot(itemData, newSlotQuantity); // 신규 슬롯 생성
            slots.Add(newSlot); // 인벤토리에 신규 슬롯 추가
            remainingAmount -= newSlotQuantity; // 남은 수량 감소
        }

        if (remainingAmount != amount) // 실제 추가 여부 확인
        {
            InventoryChanged?.Invoke(); // 인벤토리 변경 알림
        }

        return remainingAmount; // 넣지 못한 수량 반환
    }


}