using System.Collections; // 코루틴 기능
using UnityEngine; // Unity 기본 기능

public sealed class GatherableResource : InteractableBase // 반복 채집 자원 관리
{
    [Header("Resource")] // 자원 설정 묶음
    [SerializeField] private ItemData resourceItem; // 획득할 아이템 데이터
    [SerializeField] private int totalQuantity = 5; // 전체 보유 자원 수량
    [SerializeField] private int quantityPerInteraction = 1; // 한 번에 획득할 수량
    [SerializeField] private ToolType requiredToolType = ToolType.None; // 채집에 필요한 도구


    [Header("Respawn")] // 재생성 설정 묶음
    [SerializeField] private bool respawnEnabled = true; // 재생성 사용 여부
    [SerializeField] private float respawnDelay = 10f; // 재생성 대기 시간

    private Renderer[] resourceRenderers; // 자원 외형 목록
    private Collider[] resourceColliders; // 자원 충돌체 목록
    private int remainingQuantity; // 현재 남은 자원 수량
    private bool isDepleted; // 자원 소진 상태

    private void Awake() // 자원 초기화
    {
        resourceRenderers = GetComponentsInChildren<Renderer>(true); // 하위 외형 검색
        resourceColliders = GetComponentsInChildren<Collider>(true); // 하위 충돌체 검색
        remainingQuantity = Mathf.Max(1, totalQuantity); // 시작 자원 수량 설정

        if (resourceItem == null) // 아이템 데이터 확인
        {
            Debug.LogError($"{gameObject.name}의 Resource Item이 연결되지 않았습니다.", this); // 아이템 누락 오류
            enabled = false; // 채집 기능 비활성화
            return; // 초기화 중단
        }

        if (resourceColliders.Length == 0) // 충돌체 존재 확인
        {
            Debug.LogError($"{gameObject.name}에 Collider가 필요합니다.", this); // 충돌체 누락 오류
            enabled = false; // 채집 기능 비활성화
        }
    }

    private void OnValidate() // Inspector 값 검증
    {
        totalQuantity = Mathf.Max(1, totalQuantity); // 전체 수량 최소값 보정
        quantityPerInteraction = Mathf.Max(1, quantityPerInteraction); // 획득 수량 최소값 보정
        respawnDelay = Mathf.Max(0.1f, respawnDelay); // 재생성 시간 최소값 보정
    }

    public override void Interact(GameObject interactor) // 자원 채집 실행
    {
        if (isDepleted || resourceItem == null) // 채집 가능 상태 확인
        {
            return; // 채집 처리 중단
        }

        PlayerInventory inventory = interactor.GetComponent<PlayerInventory>(); // 플레이어 인벤토리 검색

        if (inventory == null) // 인벤토리 존재 확인
        {
            Debug.LogError($"{interactor.name}에서 PlayerInventory를 찾을 수 없습니다.", interactor); // 인벤토리 누락 오류
            return; // 채집 처리 중단
        }

        if (!CanGatherWithSelectedTool(inventory)) // 현재 도구 확인
        {
            return; // 잘못된 도구 채집 차단
        }

        int requestedQuantity = Mathf.Min(quantityPerInteraction, remainingQuantity); // 이번 채집 요청 수량
        int leftoverQuantity = inventory.AddItem(resourceItem, requestedQuantity); // 인벤토리 추가 후 남은 수량
        int gatheredQuantity = requestedQuantity - leftoverQuantity; // 실제 획득 수량

        if (gatheredQuantity <= 0) // 아이템 추가 실패 확인
        {
            Debug.Log("인벤토리가 가득 차 자원을 채집하지 못했습니다.", this); // 가득 참 안내
            return; // 자원 감소 차단
        }

        remainingQuantity -= gatheredQuantity; // 남은 자원 수량 감소
        Debug.Log($"{resourceItem.DisplayName} {gatheredQuantity}개 획득 / 남은 자원 {remainingQuantity}개", this); // 채집 결과 출력

        if (remainingQuantity > 0) // 남은 자원 확인
        {
            return; // 소진 처리 생략
        }

        HandleDepleted(); // 자원 소진 처리
    }

    private bool CanGatherWithSelectedTool(PlayerInventory inventory) // 선택 도구 채집 가능 여부
    {
        if (requiredToolType == ToolType.None) // 도구가 필요 없는 자원 확인
        {
            return true; // 맨손 채집 허용
        }

        ItemData selectedItem = inventory.SelectedHotbarItem; // 현재 핫바 아이템 조회

        if (selectedItem == null) // 선택 아이템 존재 확인
        {
            Debug.Log($"{gameObject.name} 채집에는 {requiredToolType} 도구가 필요합니다.", this); // 도구 미장착 안내
            return false; // 맨손 채집 차단
        }

        if (!selectedItem.IsTool) // 선택 아이템 분류 확인
        {
            Debug.Log($"{selectedItem.DisplayName}은 채집 도구가 아닙니다.", this); // 일반 아이템 안내
            return false; // 일반 아이템 채집 차단
        }

        if (selectedItem.ToolType != requiredToolType) // 필요 도구 일치 확인
        {
            Debug.Log($"{gameObject.name} 채집에는 {requiredToolType} 도구가 필요합니다.", this); // 잘못된 도구 안내
            return false; // 잘못된 도구 채집 차단
        }

        return true; // 올바른 도구 채집 허용
    }

    private void HandleDepleted() // 자원 소진 상태 처리
    {
        isDepleted = true; // 소진 상태 활성화
        SetResourceComponentsEnabled(false); // 외형과 충돌체 숨김

        if (!respawnEnabled) // 재생성 사용 여부 확인
        {
            Destroy(gameObject); // 자원 오브젝트 제거
            return; // 소진 처리 종료
        }

        StartCoroutine(RespawnRoutine()); // 재생성 대기 시작
    }

    private IEnumerator RespawnRoutine() // 자원 재생성 처리
    {
        yield return new WaitForSeconds(respawnDelay); // 재생성 시간 대기

        remainingQuantity = Mathf.Max(1, totalQuantity); // 자원 수량 복구
        SetResourceComponentsEnabled(true); // 외형과 충돌체 복구
        isDepleted = false; // 소진 상태 해제

        Debug.Log($"{gameObject.name} 자원이 다시 생성되었습니다.", this); // 재생성 결과 출력
    }

    private void SetResourceComponentsEnabled(bool shouldEnable) // 자원 표시 상태 변경
    {
        for (int index = 0; index < resourceRenderers.Length; index++) // 전체 외형 순회
        {
            resourceRenderers[index].enabled = shouldEnable; // 외형 표시 상태 적용
        }

        for (int index = 0; index < resourceColliders.Length; index++) // 전체 충돌체 순회
        {
            resourceColliders[index].enabled = shouldEnable; // 충돌체 활성 상태 적용
        }
    }
}