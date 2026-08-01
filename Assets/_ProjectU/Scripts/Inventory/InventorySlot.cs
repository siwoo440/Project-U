using UnityEngine; // Unity 기본 기능

[System.Serializable] // Unity 직렬화 허용
public sealed class InventorySlot // 인벤토리 슬롯 데이터
{
    [Tooltip("보관 아이템 데이터.")]
    [SerializeField] private ItemData itemData; // 보관 아이템 데이터
    [Tooltip("현재 보관 수량.")]
    [SerializeField] private int quantity; // 현재 보관 수량

    public ItemData ItemData => itemData; // 아이템 데이터 제공
    public int Quantity => quantity; // 현재 수량 제공
    public bool IsFull => itemData != null && quantity >= itemData.MaximumStack; // 슬롯 최대 중첩 여부

    public InventorySlot(ItemData newItemData, int newQuantity) // 신규 슬롯 생성
    {
        itemData = newItemData; // 아이템 데이터 저장
        quantity = Mathf.Clamp(newQuantity, 0, newItemData.MaximumStack); // 초기 수량 제한
    }

    public bool Contains(ItemData targetItemData) // 동일 아이템 확인
    {
        return itemData == targetItemData; // 데이터 에셋 비교
    }

    public int AddQuantity(int amount) // 슬롯 수량 추가
    {
        int safeAmount = Mathf.Max(0, amount); // 음수 수량 방지
        int freeSpace = itemData.MaximumStack - quantity; // 남은 중첩 공간
        int addedAmount = Mathf.Min(freeSpace, safeAmount); // 실제 추가 수량
        quantity += addedAmount; // 보관 수량 증가
        return safeAmount - addedAmount; // 남은 수량 반환
    }

    public int RemoveQuantity(int amount) // 슬롯 수량 제거
    {
        int safeAmount = Mathf.Max(0, amount); // 음수 수량 방지
        int removedAmount = Mathf.Min(quantity, safeAmount); // 실제 제거 수량
        quantity -= removedAmount; // 보관 수량 감소
        return removedAmount; // 제거 수량 반환
    }
}