using UnityEngine; // Unity 기본 기능
[RequireComponent(typeof(WorldObjectIdentity))] // 월드 고유 ID 컴포넌트 요구
public sealed class WorldItemPickup : InteractableBase // 월드 아이템 획득 처리
{
    [Header("Item")] // 아이템 설정 묶음
    [Tooltip("획득 아이템 데이터.")]
    [SerializeField] private ItemData itemData; // 획득 아이템 데이터
    [Tooltip("월드 아이템 수량.")]
    [SerializeField] private int quantity = 1; // 월드 아이템 수량

    private WorldObjectIdentity worldObjectIdentity; // 월드 고유 ID 컴포넌트

    public ItemData ItemData => itemData; // 현재 아이템 데이터 제공
    public int Quantity => quantity; // 현재 월드 수량 제공
    public bool IsAvailable => gameObject.activeSelf; // 현재 획득 가능 여부

    public string WorldObjectId // 현재 월드 오브젝트 ID 제공
    {
        get
        {
            WorldObjectIdentity identity = ResolveWorldObjectIdentity(); // ID 컴포넌트 검색
            return identity == null ? string.Empty : identity.WorldObjectId; // ID 또는 빈 값 반환
        }
    }

    private void Awake() // 월드 아이템 참조 초기화
    {
        worldObjectIdentity = GetComponent<WorldObjectIdentity>(); // 같은 오브젝트의 ID 검색
    }
    private void OnValidate() // Inspector 값 검증
    {
        quantity = Mathf.Max(1, quantity); // 수량 최소값 보정
    }
    public void Initialize(ItemData newItemData, int newQuantity) // 생성된 월드 아이템 초기화
    {
        WorldObjectIdentity identity = EnsureWorldObjectIdentity(); // ID 컴포넌트 준비
        identity.GenerateRuntimeId(); // 새 월드 아이템 ID 발급
        itemData = newItemData; // 아이템 데이터 적용
        quantity = Mathf.Max(1, newQuantity); // 최소 한 개 수량 적용
        gameObject.SetActive(true); // 월드 아이템 활성화
    }
    public void RestoreFromSave(ItemData savedItemData, int savedQuantity, string savedWorldObjectId, Vector3 savedPosition, Quaternion savedRotation) // 저장 월드 아이템 복원
    {
        WorldObjectIdentity identity = EnsureWorldObjectIdentity(); // ID 컴포넌트 준비
        identity.AssignWorldObjectId(savedWorldObjectId); // 저장 ID 복원
        itemData = savedItemData; // 저장 아이템 데이터 적용
        quantity = Mathf.Max(1, savedQuantity); // 저장 수량 적용
        transform.SetPositionAndRotation(savedPosition, savedRotation); // 저장 위치와 회전 적용

        Rigidbody itemRigidbody = GetComponent<Rigidbody>(); // 월드 아이템 물리 검색

        if (itemRigidbody != null) // 물리 컴포넌트 존재 확인
        {
            itemRigidbody.linearVelocity = Vector3.zero; // 이동 속도 초기화
            itemRigidbody.angularVelocity = Vector3.zero; // 회전 속도 초기화
        }

        gameObject.SetActive(true); // 월드 아이템 활성화
    }

    public void SetAvailableForLoad(bool shouldBeAvailable) // 불러오기용 활성 상태 적용
    {
        gameObject.SetActive(shouldBeAvailable); // 오브젝트 활성 상태 변경
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
            quantity = 0; // 남은 수량 초기화
            gameObject.SetActive(false); // 불러오기 복원을 위한 비활성화
            return; // 획득 처리 종료
        }

        Debug.Log($"{itemData.DisplayName} 일부만 획득하고 {quantity}개가 남았습니다.", this); // 부분 획득 결과 출력
    }
    private WorldObjectIdentity ResolveWorldObjectIdentity() // 기존 ID 컴포넌트 검색
    {
        if (worldObjectIdentity == null) // 캐시된 컴포넌트 확인
        {
            worldObjectIdentity = GetComponent<WorldObjectIdentity>(); // 같은 오브젝트에서 재검색
        }

        return worldObjectIdentity; // 검색 결과 반환
    }

    private WorldObjectIdentity EnsureWorldObjectIdentity() // ID 컴포넌트 준비
    {
        WorldObjectIdentity identity = ResolveWorldObjectIdentity(); // 기존 컴포넌트 검색

        if (identity == null) // 기존 컴포넌트 없음 확인
        {
            identity = gameObject.AddComponent<WorldObjectIdentity>(); // 실행 중 ID 컴포넌트 추가
            worldObjectIdentity = identity; // 추가 컴포넌트 캐시
        }

        return identity; // 준비된 컴포넌트 반환
    }
}