using UnityEngine; // Unity 기본 기능

[System.Serializable] // Unity 직렬화 허용
public sealed class InventorySlot // 인벤토리 슬롯 데이터
{
    [SerializeField] private ItemData itemData; // 보관 아이템 데이터
    [SerializeField] private int quantity; // 현재 보관 수량

    public ItemData ItemData => itemData; // 아이템 데이터 제공
    public int Quantity => quantity; // 현재 수량 제공
    public bool IsFull => itemData != null && quantity >= itemData.MaximumStack; // 슬롯 가득 참 여부

    public InventorySlot(ItemData newItemData, int newQuantity) // 신규 슬롯 생성
    {
        itemData = newItemData; // 아이템 데이터 저장
        quantity = Mathf.Clamp(newQuantity, 0, newItemData.MaximumStack); // 초기 수량 제한
    }

    public bool Contains(ItemData targetItemData) // 동일 아이템 확인
    {
        return itemData == targetItemData; // 데이터 에셋 비교 결과
    }

    public int AddQuantity(int amount) // 현재 슬롯에 수량 추가
    {
        int freeSpace = itemData.MaximumStack - quantity; // 남은 중첩 공간 계산
        int addedAmount = Mathf.Min(freeSpace, amount); // 실제 추가 수량 계산
        quantity += addedAmount; // 슬롯 수량 증가
        return amount - addedAmount; // 남은 수량 반환
    }
}