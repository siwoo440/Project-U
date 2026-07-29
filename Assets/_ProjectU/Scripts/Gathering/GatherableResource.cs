using System.Collections; // 코루틴 기능
using UnityEngine; // Unity 기본 기능

[RequireComponent(typeof(WorldObjectIdentity))] // 월드 고유 ID 컴포넌트 요구
public sealed class GatherableResource : InteractableBase // 반복 채집 자원 관리
{
    [Header("Resource")] // 자원 설정 묶음
    [SerializeField] private ItemData resourceItem; // 획득할 아이템 데이터
    [SerializeField] private int totalQuantity = 5; // 전체 보유 자원 수량
    [SerializeField] private int quantityPerInteraction = 1; // 한 번에 획득할 수량
    [SerializeField] private ToolType requiredToolType = ToolType.None; // 채집에 필요한 도구

    [Header("Feedback")] // 채집 반응 설정 묶음
    [SerializeField] private ResourceHitFeedback hitFeedback; // 자원 타격 반응
    [SerializeField] private float gatherCooldown = 0.45f; // 연속 채집 최소 간격

    [Header("Respawn")] // 재생성 설정 묶음
    [SerializeField] private bool respawnEnabled = true; // 재생성 사용 여부
    [SerializeField] private float respawnDelay = 10f; // 재생성 대기 시간

    private Renderer[] resourceRenderers; // 자원 외형 목록
    private Collider[] resourceColliders; // 자원 충돌체 목록
    private int remainingQuantity; // 현재 남은 자원 수량
    private bool isDepleted; // 자원 소진 상태
    private float nextGatherAllowedTime; // 다음 채집 허용 시간

    private WorldObjectIdentity worldObjectIdentity; // 월드 고유 ID 컴포넌트
    private float respawnReadyTime; // 재생성 완료 예정 시각

    public int TotalQuantity => Mathf.Max(1, totalQuantity); // 전체 자원 수량 제공
    public int RemainingQuantity => remainingQuantity; // 현재 남은 수량 제공
    public bool IsDepleted => isDepleted; // 현재 소진 상태 제공

    public float RespawnRemainingSeconds // 재생성 남은 시간 제공
    {
        get
        {
            if (!isDepleted || !respawnEnabled) // 재생성 대기 상태 확인
            {
                return 0f; // 대기 시간 없음 반환
            }

            return Mathf.Max(0f, respawnReadyTime - Time.time); // 현재 기준 남은 시간 계산
        }
    }

    public string WorldObjectId // 현재 월드 오브젝트 ID 제공
    {
        get
        {
            WorldObjectIdentity identity = ResolveWorldObjectIdentity(); // ID 컴포넌트 검색
            return identity == null ? string.Empty : identity.WorldObjectId; // ID 또는 빈 값 반환
        }
    }


    private void Awake() // 자원 초기화
    {
        worldObjectIdentity = GetComponent<WorldObjectIdentity>(); // 월드 고유 ID 컴포넌트 검색
        resourceRenderers = GetComponentsInChildren<Renderer>(true); // 하위 외형 검색
        resourceColliders = GetComponentsInChildren<Collider>(true); // 하위 충돌체 검색
        if (hitFeedback == null) // 타격 반응 연결 확인
        {
            hitFeedback = GetComponent<ResourceHitFeedback>(); // 같은 오브젝트에서 반응 검색
        }

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
        gatherCooldown = Mathf.Max(0.1f, gatherCooldown); // 채집 간격 최소값 보정
    }

    public override void Interact(GameObject interactor) // 자원 채집 실행
    {
        if (isDepleted || resourceItem == null) // 채집 가능 상태 확인
        {
            return; // 채집 처리 중단
        }

        if (Time.time < nextGatherAllowedTime) // 채집 대기 시간 확인
        {
            return; // 빠른 연속 채집 차단
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
        nextGatherAllowedTime = Time.time + gatherCooldown; // 다음 채집 가능 시간 설정
        PlayGatheringFeedback(); // 자원 타격 반응 실행
        Debug.Log($"{resourceItem.DisplayName} {gatheredQuantity}개 획득 / 남은 자원 {remainingQuantity}개", this); // 채집 결과 출력

        if (remainingQuantity > 0) // 남은 자원 확인
        {
            return; // 소진 처리 생략
        }

        HandleDepleted(); // 자원 소진 처리
    }
    private void PlayGatheringFeedback() // 채집 성공 반응 실행
    {
        if (hitFeedback == null) // 자원 반응 존재 확인
        {
            return; // 반응 처리 중단
        }

        hitFeedback.PlayHit(); // 자원 타격 반응 실행
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
        remainingQuantity = 0; // 남은 자원 수량 초기화
        isDepleted = true; // 소진 상태 활성화
        SetResourceComponentsEnabled(false); // 외형과 충돌체 숨김

        if (!respawnEnabled) // 재생성 사용 여부 확인
        {
            respawnReadyTime = 0f; // 재생성 시각 초기화
            return; // 소진 상태 유지
        }

        respawnReadyTime = Time.time + respawnDelay; // 재생성 완료 예정 시각 계산
        StartCoroutine(RespawnRoutine(respawnDelay)); // 재생성 대기 시작
    }

    private IEnumerator RespawnRoutine(float delaySeconds) // 지정 시간 후 자원 재생성
    {
        yield return new WaitForSeconds(Mathf.Max(0f, delaySeconds)); // 저장된 재생성 시간 대기
        RespawnNow(); // 자원 상태 즉시 복구
    }

    public void ResetForLoad() // 저장 항목이 없는 자원 기본 상태 복구
    {
        StopAllCoroutines(); // 기존 재생성 대기 중단
        remainingQuantity = Mathf.Max(1, totalQuantity); // 전체 자원 수량 복구
        isDepleted = false; // 소진 상태 해제
        nextGatherAllowedTime = 0f; // 채집 대기 시간 초기화
        respawnReadyTime = 0f; // 재생성 예정 시각 초기화
        SetResourceComponentsEnabled(true); // 외형과 충돌체 활성화
    }

    public void RestoreFromSave(int savedRemainingQuantity, bool savedIsDepleted, float savedRespawnRemainingSeconds) // 저장 자원 상태 복원
    {
        StopAllCoroutines(); // 기존 재생성 대기 중단
        remainingQuantity = Mathf.Clamp(savedRemainingQuantity, 0, TotalQuantity); // 저장 수량 범위 적용
        isDepleted = savedIsDepleted || remainingQuantity <= 0; // 저장 소진 상태 적용
        nextGatherAllowedTime = 0f; // 채집 대기 시간 초기화
        respawnReadyTime = 0f; // 재생성 예정 시각 초기화

        if (!isDepleted) // 채집 가능한 상태 확인
        {
            SetResourceComponentsEnabled(true); // 외형과 충돌체 활성화
            return; // 복원 완료
        }

        remainingQuantity = 0; // 소진 수량 통일
        SetResourceComponentsEnabled(false); // 외형과 충돌체 비활성화

        if (!respawnEnabled) // 재생성 미사용 자원 확인
        {
            return; // 소진 상태 유지
        }

        float remainingSeconds = Mathf.Max(0f, savedRespawnRemainingSeconds); // 저장 대기 시간 보정

        if (remainingSeconds <= 0f) // 재생성 시간이 지난 상태 확인
        {
            RespawnNow(); // 자원 즉시 복구
            return; // 복원 완료
        }

        respawnReadyTime = Time.time + remainingSeconds; // 새로운 완료 예정 시각 계산
        StartCoroutine(RespawnRoutine(remainingSeconds)); // 남은 시간만큼 재생성 대기
    }

    private void RespawnNow() // 자원 즉시 재생성
    {
        remainingQuantity = Mathf.Max(1, totalQuantity); // 전체 자원 수량 복구
        nextGatherAllowedTime = 0f; // 채집 대기 시간 초기화
        respawnReadyTime = 0f; // 재생성 예정 시각 초기화
        isDepleted = false; // 소진 상태 해제
        SetResourceComponentsEnabled(true); // 외형과 충돌체 복구
        Debug.Log($"{gameObject.name} 자원이 다시 생성되었습니다.", this); // 재생성 결과 출력
    }

    private WorldObjectIdentity ResolveWorldObjectIdentity() // ID 컴포넌트 검색
    {
        if (worldObjectIdentity == null) // 캐시된 컴포넌트 확인
        {
            worldObjectIdentity = GetComponent<WorldObjectIdentity>(); // 같은 오브젝트에서 재검색
        }

        return worldObjectIdentity; // 검색 결과 반환
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