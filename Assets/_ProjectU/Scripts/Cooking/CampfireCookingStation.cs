using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class CampfireCookingStation : InteractableBase, IBuildRemovalGuard // 모닥불 조리 처리
{
    [Header("Recipe")] // 조리법 설정 묶음
    [Tooltip("연료 아이템.")]
    [SerializeField] private ItemData fuelItem; // 연료 아이템
    [Tooltip("필요 연료 수량.")]
    [SerializeField] private int fuelAmount = 1; // 필요 연료 수량
    [Tooltip("조리 재료 아이템.")]
    [SerializeField] private ItemData inputItem; // 조리 재료 아이템
    [Tooltip("필요 재료 수량.")]
    [SerializeField] private int inputAmount = 1; // 필요 재료 수량
    [Tooltip("완성 음식 아이템.")]
    [SerializeField] private ItemData outputItem; // 완성 음식 아이템
    [Tooltip("완성 음식 수량.")]
    [SerializeField] private int outputAmount = 1; // 완성 음식 수량

    [Header("Cooking")] // 조리 설정 묶음
    [Tooltip("조리 소요 시간.")]
    [SerializeField] private float cookingDuration = 5f; // 조리 소요 시간
    [Tooltip("불꽃 연출 루트.")]
    [SerializeField] private GameObject fireVisualRoot; // 불꽃 연출 루트

    [Header("Heat")] // 모닥불 열기 설정 묶음
    [Tooltip("열기 적용 반경.")]
    [SerializeField][Min(0.1f)] private float heatRadius = 4f; // 열기 적용 반경
    [Tooltip("초당 체온 회복량.")]
    [SerializeField][Min(0f)] private float heatPerSecond = 4f; // 초당 체온 회복량

    [Header("Runtime")] // 실행 상태 묶음
    [Tooltip("현재 조리 상태.")]
    [SerializeField] private bool isCooking; // 현재 조리 상태
    [Tooltip("완성 음식 보관 상태.")]
    [SerializeField] private bool hasReadyResult; // 완성 음식 보관 상태
    [Tooltip("남은 조리 시간.")]
    [SerializeField] private float remainingCookingTime; // 남은 조리 시간

    private PlayerInventory lastPlayerInventory; // 최근 상호작용 인벤토리
    private PlayerTemperature playerTemperature; // 플레이어 체온 관리자
    private bool isConfigured; // 필수 설정 완료 여부
    public bool IsCooking => isCooking; // 현재 조리 진행 상태 제공
    public bool HasReadyResult => hasReadyResult; // 완성 음식 보관 상태 제공
    public float RemainingCookingTime => Mathf.Max(0f, remainingCookingTime); // 남은 조리 시간 제공
    public float CookingDuration => cookingDuration; // 전체 조리 시간 제공
    public override string PromptMessage // 현재 조리 상태 안내 문구
    {
        get
        {
            if (!isConfigured) // 설정 오류 확인
            {
                return "CAMPFIRE DATA ERROR"; // 설정 오류 문구 반환
            }

            if (hasReadyResult) // 완성 음식 존재 확인
            {
                bool inventoryIsFull = lastPlayerInventory != null
                    && !lastPlayerInventory.CanAddItem(outputItem, outputAmount); // 결과물 추가 공간 확인

                if (inventoryIsFull) // 인벤토리 공간 부족 확인
                {
                    return "INVENTORY FULL"; // 공간 부족 문구 반환
                }

                return $"F - TAKE {outputItem.DisplayName}"; // 결과물 회수 문구 반환
            }

            if (isCooking) // 조리 진행 여부 확인
            {
                int remainingSeconds = Mathf.CeilToInt(remainingCookingTime); // 남은 초 계산
                return $"COOKING - {remainingSeconds} SEC"; // 조리 진행 문구 반환
            }

            if (lastPlayerInventory != null) // 인벤토리 확인 가능 여부
            {
                bool hasFuel = lastPlayerInventory.HasItem(fuelItem, fuelAmount); // 연료 보유 확인
                bool hasInput = lastPlayerInventory.HasItem(inputItem, inputAmount); // 재료 보유 확인

                if (!hasFuel || !hasInput) // 필요 아이템 부족 확인
                {
                    return $"NEED {inputAmount} {inputItem.DisplayName} + {fuelAmount} {fuelItem.DisplayName}"; // 부족 문구 반환
                }
            }

            return $"F - COOK {inputItem.DisplayName}"; // 조리 시작 문구 반환
        }
    }

    public bool CanRemove => !isCooking && !hasReadyResult; // 유휴 상태 철거 허용

    public string RemovalBlockedMessage => isCooking
        ? "COOKING IN PROGRESS"
        : "COLLECT COOKED FOOD"; // 철거 차단 원인 제공

    private void Awake() // 모닥불 조리 기능 초기화
    {
        ClampSettings(); // 설정값 범위 보정

        bool hasMissingReference = fuelItem == null
            || inputItem == null
            || outputItem == null
            || fireVisualRoot == null; // 필수 참조 누락 확인

        if (hasMissingReference) // 참조 누락 여부 확인
        {
            Debug.LogError($"{gameObject.name}의 모닥불 조리 참조가 누락되었습니다.", this); // 참조 오류 출력
            enabled = false; // 조리 기능 비활성화
            return; // 초기화 중단
        }

        if (fuelItem == inputItem) // 연료와 조리 재료 중복 확인
        {
            Debug.LogError($"{gameObject.name}의 연료와 조리 재료는 서로 달라야 합니다.", this); // 데이터 오류 출력
            enabled = false; // 조리 기능 비활성화
            return; // 초기화 중단
        }

        isConfigured = true; // 필수 설정 완료
        RestoreFromSave(false, false, 0f); // 초기 유휴 상태 적용
    }

    private void Update() // 조리 시간과 열기 진행
    {
        if (!isCooking) // 조리 상태 확인
        {
            return; // 시간과 열기 처리 중단
        }

        ApplyNearbyHeat(); // 조리 중 주변 플레이어 열기 적용
        remainingCookingTime -= Time.deltaTime; // 경과 시간 차감

        if (remainingCookingTime > 0f) // 남은 시간 확인
        {
            return; // 조리 계속 진행
        }

        CompleteCooking(); // 조리 완료 처리
    }

    private void OnValidate() // Inspector 설정값 검증
    {
        ClampSettings(); // 설정값 범위 보정
    }

    public override void Interact(GameObject interactor) // 플레이어 상호작용 처리
    {
        if (!isConfigured || interactor == null) // 기능과 상호작용 대상 확인
        {
            return; // 상호작용 중단
        }

        PlayerInventory playerInventory = interactor.GetComponent<PlayerInventory>(); // 플레이어 인벤토리 조회

        if (playerInventory == null) // 인벤토리 존재 확인
        {
            Debug.LogError("모닥불을 사용하는 오브젝트에 PlayerInventory가 없습니다.", interactor); // 인벤토리 오류 출력
            return; // 상호작용 중단
        }

        lastPlayerInventory = playerInventory; // 최근 인벤토리 저장

        if (hasReadyResult) // 완성 음식 존재 확인
        {
            TryCollectResult(playerInventory); // 결과물 회수 시도
            return; // 조리 시작 처리 차단
        }

        if (isCooking) // 현재 조리 진행 확인
        {
            return; // 중복 조리 차단
        }

        TryStartCooking(playerInventory); // 새로운 조리 시작 시도
    }

    private void TryStartCooking(PlayerInventory playerInventory) // 조리 시작 시도
    {
        bool hasFuel = playerInventory.HasItem(fuelItem, fuelAmount); // 연료 보유 확인
        bool hasInput = playerInventory.HasItem(inputItem, inputAmount); // 조리 재료 보유 확인

        if (!hasFuel || !hasInput) // 필요 아이템 부족 확인
        {
            return; // 조리 시작 차단
        }

        int removedFuelAmount = playerInventory.RemoveItem(fuelItem, fuelAmount); // 연료 제거
        int removedInputAmount = playerInventory.RemoveItem(inputItem, inputAmount); // 조리 재료 제거

        bool removalSucceeded = removedFuelAmount == fuelAmount
            && removedInputAmount == inputAmount; // 전체 재료 제거 결과 확인

        if (!removalSucceeded) // 재료 제거 실패 확인
        {
            RollbackIngredients(playerInventory, removedFuelAmount, removedInputAmount); // 제거된 재료 복구
            return; // 조리 시작 차단
        }

        isCooking = true; // 조리 상태 활성화
        hasReadyResult = false; // 이전 결과물 상태 해제
        remainingCookingTime = cookingDuration; // 조리 시간 설정
        fireVisualRoot.SetActive(true); // 불꽃 연출 활성화
    }

    private void CompleteCooking() // 조리 완료 처리
    {
        isCooking = false; // 조리 상태 해제
        hasReadyResult = true; // 완성 음식 보관
        remainingCookingTime = 0f; // 남은 시간 제거
        fireVisualRoot.SetActive(false); // 불꽃 연출 비활성화
    }

    private void ApplyNearbyHeat() // 주변 플레이어에게 모닥불 열기 적용
    {
        PlayerTemperature targetTemperature = ResolvePlayerTemperature(); // 플레이어 체온 관리자 조회

        if (targetTemperature == null) // 플레이어 체온 관리자 확인
        {
            return; // 열기 처리 중단
        }

        Vector3 campfirePosition = transform.position; // 모닥불 위치 저장
        Vector3 playerPosition = targetTemperature.transform.position; // 플레이어 위치 저장
        float squaredDistance = (playerPosition - campfirePosition).sqrMagnitude; // 거리 제곱 계산
        float squaredHeatRadius = heatRadius * heatRadius; // 열기 반경 제곱 계산

        if (squaredDistance > squaredHeatRadius) // 열기 범위 밖 확인
        {
            return; // 체온 회복 중단
        }

        targetTemperature.ReceiveHeat(heatPerSecond * Time.deltaTime); // 현재 프레임 열기 적용
    }

    private PlayerTemperature ResolvePlayerTemperature() // 플레이어 체온 관리자 검색
    {
        if (playerTemperature == null) // 기존 체온 참조 확인
        {
            playerTemperature = FindFirstObjectByType<PlayerTemperature>(); // Scene의 플레이어 체온 검색
        }

        return playerTemperature; // 체온 관리자 반환
    }

    private void TryCollectResult(PlayerInventory playerInventory) // 완성 음식 회수 시도
    {
        if (!playerInventory.CanAddItem(outputItem, outputAmount)) // 인벤토리 공간 확인
        {
            return; // 결과물 회수 차단
        }

        int remainingAmount = playerInventory.AddItem(outputItem, outputAmount); // 완성 음식 추가

        if (remainingAmount > 0) // 일부 수량 추가 실패 확인
        {
            int addedAmount = outputAmount - remainingAmount; // 실제 추가 수량 계산

            if (addedAmount > 0) // 추가된 수량 확인
            {
                playerInventory.RemoveItem(outputItem, addedAmount); // 부분 추가 결과 복구
            }

            return; // 결과물 보관 유지
        }

        hasReadyResult = false; // 결과물 보관 상태 해제
    }

    private void RollbackIngredients(
        PlayerInventory playerInventory,
        int removedFuelAmount,
        int removedInputAmount) // 제거된 조리 재료 복구
    {
        if (removedFuelAmount > 0) // 제거된 연료 확인
        {
            playerInventory.AddItem(fuelItem, removedFuelAmount); // 연료 복구
        }

        if (removedInputAmount > 0) // 제거된 재료 확인
        {
            playerInventory.AddItem(inputItem, removedInputAmount); // 조리 재료 복구
        }
    }
    public void RestoreFromSave(
    bool savedIsCooking,
    bool savedHasReadyResult,
    float savedRemainingCookingTime) // 저장 모닥불 상태 복원
    {
        float safeRemainingCookingTime = Mathf.Clamp(
            savedRemainingCookingTime,
            0f,
            cookingDuration); // 저장 남은 시간 범위 보정

        if (savedHasReadyResult) // 완성 음식 보관 상태 확인
        {
            isCooking = false; // 조리 진행 상태 해제
            hasReadyResult = true; // 완성 음식 상태 적용
            remainingCookingTime = 0f; // 남은 시간 초기화
            fireVisualRoot.SetActive(false); // 불꽃 연출 비활성화
            return; // 복원 완료
        }

        if (savedIsCooking) // 조리 진행 상태 확인
        {
            if (safeRemainingCookingTime <= 0f) // 조리 완료 시간 확인
            {
                CompleteCooking(); // 즉시 조리 완료
                return; // 복원 완료
            }

            isCooking = true; // 조리 진행 상태 적용
            hasReadyResult = false; // 완성 음식 상태 해제
            remainingCookingTime = safeRemainingCookingTime; // 남은 조리 시간 적용
            fireVisualRoot.SetActive(true); // 불꽃 연출 활성화
            return; // 복원 완료
        }

        isCooking = false; // 조리 상태 해제
        hasReadyResult = false; // 완성 음식 상태 해제
        remainingCookingTime = 0f; // 남은 시간 초기화
        fireVisualRoot.SetActive(false); // 불꽃 연출 비활성화
    }
    private void ClampSettings() // 설정값 범위 보정
    {
        fuelAmount = Mathf.Max(1, fuelAmount); // 연료 수량 최소값 적용
        inputAmount = Mathf.Max(1, inputAmount); // 조리 재료 수량 최소값 적용
        outputAmount = Mathf.Max(1, outputAmount); // 결과물 수량 최소값 적용
        cookingDuration = Mathf.Max(0.1f, cookingDuration); // 조리 시간 최소값 적용
        remainingCookingTime = Mathf.Max(0f, remainingCookingTime); // 남은 시간 음수 방지
        heatRadius = Mathf.Max(0.1f, heatRadius); // 열기 반경 최소값 적용
        heatPerSecond = Mathf.Max(0f, heatPerSecond); // 체온 회복량 음수 방지
    }

    private void OnDrawGizmosSelected() // 모닥불 열기 범위 표시
    {
        Gizmos.color = new Color(1f, 0.35f, 0.1f, 0.8f); // 열기 범위 색상 설정
        Gizmos.DrawWireSphere(transform.position, heatRadius); // 열기 범위 원형 표시
    }

    private void OnDisable() // 모닥불 비활성화 정리
    {
        if (fireVisualRoot != null) // 불꽃 오브젝트 존재 확인
        {
            fireVisualRoot.SetActive(false); // 불꽃 연출 비활성화
        }
    }
}
