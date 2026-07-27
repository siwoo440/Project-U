using UnityEngine; // Unity 기본 기능

public sealed class WorldItemPickup : InteractableBase // 월드 아이템 획득 처리
{
    [Header("Item")] // 아이템 설정 묶음
    [SerializeField] private ItemData itemData; // 획득 아이템 데이터
    [SerializeField] private int quantity = 1; // 월드 아이템 수량

    private void OnValidate() // Inspector 값 검증
    {
        quantity = Mathf.Max(1, quantity); // 수량 최소값 보정
    }
    public void Initialize(ItemData newItemData, int newQuantity) // 생성된 월드 아이템 초기화
    {
        itemData = newItemData; // 아이템 데이터 적용
        quantity = Mathf.Max(1, newQuantity); // 최소 한 개 수량 적용
    }

    public override void Interact(GameObject interactor) // 아이템 획득 실행
    {
        if (itemData == null) // 아이템 데이터 연결 확인
        {
            Debug.LogError($"{gameObject.name}의 Item Data가 연결되지 않았습니다.", this); // 데이터 누락 오류
            return; // 획득 처리 중단
        }

        PlayerInventory inventory = interactor.GetComponent<PlayerInventory>(); // 플레이어 인벤토리 검색

        if (inventory == null) // 인벤토리 존재 확인
        {
            Debug.LogError($"{interactor.name}에서 PlayerInventory를 찾을 수 없습니다.", interactor); // 인벤토리 누락 오류
            return; // 획득 처리 중단
        }

        int previousQuantity = quantity; // 획득 전 수량 저장
        int remainingQuantity = inventory.AddItem(itemData, quantity); // 인벤토리 추가 후 남은 수량 계산
        quantity = remainingQuantity; // 월드 아이템 수량 갱신

        if (quantity == previousQuantity) // 추가 실패 여부 확인
        {
            Debug.Log("인벤토리가 가득 차 아이템을 획득하지 못했습니다.", this); // 가득 참 결과 출력
            return; // 오브젝트 유지
        }

        if (quantity <= 0) // 전체 획득 여부 확인
        {
            Destroy(gameObject); // 월드 아이템 제거
            return; // 획득 처리 종료
        }

        Debug.Log($"{itemData.DisplayName} 일부만 획득하고 {quantity}개가 남았습니다.", this); // 부분 획득 결과 출력
    }
}