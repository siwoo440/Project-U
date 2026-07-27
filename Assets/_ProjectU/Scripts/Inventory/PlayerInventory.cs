using System; // 기본 이벤트 기능
using System.Collections.Generic; // List 기능
using UnityEngine; // Unity 기본 기능

public sealed class PlayerInventory : MonoBehaviour // 플레이어 인벤토리 관리
{
    [Header("Capacity")] // 용량 설정 묶음
    [SerializeField] private int slotCapacity = 24; // 전체 슬롯 개수
    [SerializeField] private int hotbarSlotCount = 8; // 핫바 슬롯 개수

    private int selectedHotbarIndex; // 현재 선택 핫바 번호
    private int selectedInventoryIndex = -1; // 현재 클릭한 인벤토리 번호

    private readonly List<InventorySlot> slots = new List<InventorySlot>(); // 고정 슬롯 목록

    public IReadOnlyList<InventorySlot> Slots => slots; // 읽기 전용 슬롯 목록
    public int SlotCapacity => slotCapacity; // 전체 슬롯 개수
    public int HotbarSlotCount => Mathf.Min(hotbarSlotCount, slotCapacity); // 실제 핫바 슬롯 개수
    public int SelectedHotbarIndex => selectedHotbarIndex; // 선택 핫바 번호
    public int SelectedInventoryIndex => selectedInventoryIndex; // 클릭한 인벤토리 번호

    public event Action HotbarSelectionChanged; // 핫바 선택 변경 알림
    public event Action InventorySelectionChanged; // 인벤토리 선택 변경 알림
    public event Action InventoryChanged; // 인벤토리 변경 알림

    public int UsedSlotCount // 사용 중인 슬롯 개수
    {
        get // 사용 슬롯 계산 접근자
        {
            int usedSlotCount = 0; // 사용 슬롯 개수 초기화

            for (int index = 0; index < slots.Count; index++) // 전체 슬롯 순회
            {
                if (slots[index] != null) // 아이템 존재 확인
                {
                    usedSlotCount++; // 사용 슬롯 개수 증가
                }
            }

            return usedSlotCount; // 사용 슬롯 개수 반환
        }
    }

    private void Awake() // 인벤토리 초기화
    {
        EnsureSlotCapacity(); // 고정 슬롯 구조 생성
    }

    private void OnValidate() // Inspector 값 검증
    {
        slotCapacity = Mathf.Max(8, slotCapacity); // 전체 슬롯 최소값 보정
        hotbarSlotCount = Mathf.Clamp(hotbarSlotCount, 1, slotCapacity); // 핫바 슬롯 범위 보정
        selectedHotbarIndex = Mathf.Clamp(selectedHotbarIndex, 0, hotbarSlotCount - 1); // 선택 번호 범위 보정
        selectedInventoryIndex = Mathf.Clamp(selectedInventoryIndex, -1, slotCapacity - 1); // 인벤토리 선택 범위 보정
    }

    private void EnsureSlotCapacity() // 고정 슬롯 개수 확보
    {
        while (slots.Count < slotCapacity) // 부족한 슬롯 확인
        {
            slots.Add(null); // 빈 슬롯 추가
        }

        while (slots.Count > slotCapacity) // 초과 슬롯 확인
        {
            slots.RemoveAt(slots.Count - 1); // 마지막 초과 슬롯 제거
        }
    }

    public InventorySlot GetSlot(int index) // 지정 슬롯 조회
    {
        if (index < 0 || index >= slots.Count) // 슬롯 범위 확인
        {
            return null; // 잘못된 번호 결과
        }

        return slots[index]; // 해당 슬롯 반환
    }

    public void SelectHotbarSlot(int index) // 핫바 슬롯 선택
    {
        if (index < 0 || index >= HotbarSlotCount) // 핫바 범위 확인
        {
            return; // 잘못된 선택 차단
        }

        if (selectedHotbarIndex == index) // 같은 슬롯 확인
        {
            return; // 중복 변경 차단
        }

        selectedHotbarIndex = index; // 선택 번호 저장
        HotbarSelectionChanged?.Invoke(); // 선택 변경 알림
    }

    public void SelectInventorySlot(int index) // 인벤토리 슬롯 클릭 선택
    {
        if (index < 0 || index >= SlotCapacity) // 전체 슬롯 범위 확인
        {
            return; // 잘못된 선택 차단
        }

        bool inventorySelectionChanged = selectedInventoryIndex != index; // 인벤토리 선택 변경 여부
        selectedInventoryIndex = index; // 클릭한 슬롯 번호 저장

        if (index < HotbarSlotCount) // 핫바 영역 선택 확인
        {
            SelectHotbarSlot(index); // 실제 핫바 선택 동기화
        }

        if (inventorySelectionChanged) // 인벤토리 선택 변경 확인
        {
            InventorySelectionChanged?.Invoke(); // 인벤토리 선택 변경 알림
        }
    }

    public bool MoveOrMergeSlot(int sourceIndex, int targetIndex) // 슬롯 이동과 합치기
    {
        EnsureSlotCapacity(); // 고정 슬롯 구조 확인

        if (sourceIndex < 0 || sourceIndex >= slots.Count) // 출발 슬롯 범위 확인
        {
            return false; // 이동 실패 반환
        }

        if (targetIndex < 0 || targetIndex >= slots.Count) // 대상 슬롯 범위 확인
        {
            return false; // 이동 실패 반환
        }

        if (sourceIndex == targetIndex) // 같은 슬롯 확인
        {
            return false; // 같은 위치 이동 차단
        }

        InventorySlot sourceSlot = slots[sourceIndex]; // 출발 슬롯 가져오기
        InventorySlot targetSlot = slots[targetIndex]; // 대상 슬롯 가져오기

        if (sourceSlot == null) // 출발 아이템 확인
        {
            return false; // 빈 슬롯 이동 차단
        }

        if (targetSlot == null) // 대상 빈 슬롯 확인
        {
            slots[targetIndex] = sourceSlot; // 대상 위치로 이동
            slots[sourceIndex] = null; // 출발 위치 비우기
            InventoryChanged?.Invoke(); // 인벤토리 변경 알림
            return true; // 이동 성공 반환
        }

        if (targetSlot.Contains(sourceSlot.ItemData)) // 같은 아이템 확인
        {
            if (targetSlot.IsFull) // 대상 최대 중첩 확인
            {
                return false; // 중첩 이동 차단
            }

            int sourceQuantity = sourceSlot.Quantity; // 출발 수량 저장
            int remainingQuantity = targetSlot.AddQuantity(sourceQuantity); // 대상 슬롯 수량 추가
            int movedQuantity = sourceQuantity - remainingQuantity; // 실제 이동 수량 계산

            if (movedQuantity <= 0) // 실제 이동 여부 확인
            {
                return false; // 변경 없음 반환
            }

            sourceSlot.RemoveQuantity(movedQuantity); // 출발 슬롯 수량 감소

            if (sourceSlot.Quantity <= 0) // 출발 슬롯 소진 확인
            {
                slots[sourceIndex] = null; // 소진 슬롯 비우기
            }

            InventoryChanged?.Invoke(); // 인벤토리 변경 알림
            return true; // 합치기 성공 반환
        }

        slots[sourceIndex] = targetSlot; // 대상 아이템 출발 위치 배치
        slots[targetIndex] = sourceSlot; // 출발 아이템 대상 위치 배치
        InventoryChanged?.Invoke(); // 인벤토리 변경 알림
        return true; // 교환 성공 반환
    }

    public int AddItem(ItemData itemData, int amount) // 아이템 추가
    {
        if (itemData == null || amount <= 0) // 요청값 유효성 확인
        {
            return amount; // 전체 수량 반환
        }

        EnsureSlotCapacity(); // 고정 슬롯 구조 확인

        int remainingAmount = amount; // 아직 넣지 못한 수량

        for (int index = 0; index < slots.Count; index++) // 기존 슬롯 순회
        {
            InventorySlot slot = slots[index]; // 현재 슬롯 가져오기

            if (slot == null) // 빈 슬롯 확인
            {
                continue; // 다음 슬롯 검사
            }

            if (!slot.Contains(itemData) || slot.IsFull) // 중첩 불가능 여부 확인
            {
                continue; // 다음 슬롯 검사
            }

            remainingAmount = slot.AddQuantity(remainingAmount); // 기존 슬롯 수량 추가

            if (remainingAmount <= 0) // 전체 추가 완료 확인
            {
                break; // 기존 슬롯 검사 종료
            }
        }

        for (int index = 0; index < slots.Count && remainingAmount > 0; index++) // 빈 슬롯 순회
        {
            if (slots[index] != null) // 사용 중인 슬롯 확인
            {
                continue; // 다음 슬롯 검사
            }

            int newSlotQuantity = Mathf.Min(remainingAmount, itemData.MaximumStack); // 신규 슬롯 수량 계산
            slots[index] = new InventorySlot(itemData, newSlotQuantity); // 신규 슬롯 생성
            remainingAmount -= newSlotQuantity; // 남은 수량 감소
        }

        if (remainingAmount != amount) // 실제 추가 여부 확인
        {
            InventoryChanged?.Invoke(); // 인벤토리 변경 알림
        }

        return remainingAmount; // 넣지 못한 수량 반환
    }
}