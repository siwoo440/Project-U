public interface IItemSlotContainer // 아이템 슬롯 공통 컨테이너 규격
{
    int SlotCapacity { get; } // 전체 슬롯 개수 제공

    InventorySlot GetSlot(int index); // 지정 슬롯 조회

    bool TrySetSlotDirect(int index, InventorySlot slot); // 지정 슬롯 직접 변경

    void NotifyContentsChanged(); // 슬롯 내용 변경 알림
}
