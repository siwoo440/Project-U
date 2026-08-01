using UnityEngine; // Unity 기본 기능

public sealed class InventoryItemDropper : MonoBehaviour // 인벤토리 아이템 버리기
{
    [Header("References")] // 참조 설정 묶음
    [Tooltip("플레이어 인벤토리.")]
    [SerializeField] private PlayerInventory playerInventory; // 플레이어 인벤토리
    [Tooltip("버리기 방향 기준.")]
    [SerializeField] private Transform viewTransform; // 버리기 방향 기준
    [Tooltip("월드 아이템 원본.")]
    [SerializeField] private WorldItemPickup worldItemPrefab; // 월드 아이템 원본

    [Header("Drop")] // 버리기 설정 묶음
    [Tooltip("전방 생성 거리.")]
    [SerializeField] private float forwardDistance = 1.5f; // 전방 생성 거리
    [Tooltip("생성 높이.")]
    [SerializeField] private float verticalOffset = 0.5f; // 생성 높이
    [Tooltip("전방 힘.")]
    [SerializeField] private float forwardForce = 2f; // 전방 힘
    [Tooltip("위쪽 힘.")]
    [SerializeField] private float upwardForce = 1f; // 위쪽 힘

    private void OnValidate() // Inspector 값 검증
    {
        forwardDistance = Mathf.Max(0.5f, forwardDistance); // 전방 거리 최소값
        verticalOffset = Mathf.Max(0f, verticalOffset); // 높이 음수 방지
        forwardForce = Mathf.Max(0f, forwardForce); // 전방 힘 음수 방지
        upwardForce = Mathf.Max(0f, upwardForce); // 위쪽 힘 음수 방지
    }

    public bool DropFromSlot(int slotIndex, int amount) // 지정 슬롯 아이템 버리기
    {
        if (playerInventory == null || viewTransform == null || worldItemPrefab == null) // 필수 참조 확인
        {
            Debug.LogError($"{gameObject.name}의 아이템 버리기 참조가 누락되었습니다.", this); // 참조 누락 출력
            return false; // 버리기 실패 반환
        }

        InventorySlot slot = playerInventory.GetSlot(slotIndex); // 선택 슬롯 조회

        if (slot == null || amount <= 0) // 버리기 대상 확인
        {
            return false; // 버리기 실패 반환
        }

        int dropQuantity = Mathf.Min(amount, slot.Quantity); // 실제 버릴 수량 계산
        Vector3 flatForward = Vector3.ProjectOnPlane(viewTransform.forward, Vector3.up); // 카메라 전방 수평화

        if (flatForward.sqrMagnitude < 0.001f) // 전방 방향 유효성 확인
        {
            flatForward = transform.forward; // 플레이어 전방 사용
        }

        flatForward.Normalize(); // 전방 방향 정규화

        Vector3 spawnPosition = transform.position + flatForward * forwardDistance + Vector3.up * verticalOffset; // 생성 위치 계산
        WorldItemPickup droppedItem = Instantiate(worldItemPrefab, spawnPosition, Quaternion.identity); // 월드 아이템 생성
        droppedItem.Initialize(slot.ItemData, dropQuantity); // 생성 아이템 데이터 적용

        int removedQuantity = playerInventory.RemoveItemFromSlot(slotIndex, dropQuantity); // 인벤토리 수량 차감

        if (removedQuantity != dropQuantity) // 차감 실패 확인
        {
            Destroy(droppedItem.gameObject); // 잘못 생성된 아이템 제거
            return false; // 버리기 실패 반환
        }

        Rigidbody droppedRigidbody = droppedItem.GetComponent<Rigidbody>(); // 생성 아이템 물리 컴포넌트 조회

        if (droppedRigidbody != null) // 물리 컴포넌트 확인
        {
            Vector3 dropForce = flatForward * forwardForce + Vector3.up * upwardForce; // 투척 방향 계산
            droppedRigidbody.AddForce(dropForce, ForceMode.Impulse); // 월드 아이템에 힘 적용
        }

        return true; // 버리기 성공 반환
    }
}