using System; // 이벤트 기능
using System.Collections.Generic; // 목록 기능
using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class CampfireCookingStation : InteractableBase, IBuildRemovalGuard // 모닥불 조리 처리 (85일차: 요리법 목록과 여러 조리 칸)
{
    private static readonly List<CampfireCookingStation> activeStations = new List<CampfireCookingStation>(); // 활성 모닥불 목록

    [Header("Station")] // 조리 시설 설정 묶음
    [Tooltip("조리 시설 등급. 요리법의 필요 시설보다 같거나 높아야 조리할 수 있습니다.")]
    [SerializeField] private CookingStationTier stationTier = CookingStationTier.Campfire; // 시설 등급
    [Tooltip("동시에 조리할 수 있는 칸 수.")]
    [SerializeField, Range(1, 3)] private int slotCount = 2; // 조리 칸 수
    [Tooltip("한 칸에 한 번에 넣을 수 있는 최대 묶음 수.")]
    [SerializeField, Range(1, 9)] private int maxBatchQuantity = 5; // 최대 묶음 수

    [Header("Fuel")] // 연료 설정 묶음
    [Tooltip("연료 아이템.")]
    [SerializeField] private ItemData fuelItem; // 연료 아이템
    [Tooltip("조리를 한 번 시작할 때 쓰는 연료 수량 (묶음 수와 무관).")]
    [SerializeField] private int fuelAmount = 1; // 필요 연료 수량

    [Header("Recipes")] // 요리법 묶음
    [Tooltip("이 시설에서 보여줄 요리법 목록. 필요 시설이 더 높은 요리법도 NEED STATION으로 표시됩니다.")]
    [SerializeField] private CookingRecipeData[] recipes = new CookingRecipeData[0]; // 요리법 목록

    [Header("Cooking")] // 조리 설정 묶음
    [Tooltip("불꽃 연출 루트.")]
    [SerializeField] private GameObject fireVisualRoot; // 불꽃 연출 루트
    [Tooltip("요리 창 관리자. 비어 있으면 Scene에서 찾습니다.")]
    [SerializeField] private GameUIManager gameUIManager; // 게임 UI 관리자

    [Header("Heat")] // 모닥불 열기 설정 묶음
    [Tooltip("열기 적용 반경.")]
    [SerializeField][Min(0.1f)] private float heatRadius = 4f; // 열기 적용 반경
    [Tooltip("초당 체온 회복량.")]
    [SerializeField][Min(0f)] private float heatPerSecond = 4f; // 초당 체온 회복량

    [Header("Runtime")] // 실행 상태 묶음
    [Tooltip("조리 칸 상태.")]
    [SerializeField] private List<CookingSlot> slots = new List<CookingSlot>(); // 조리 칸

    private readonly Dictionary<ItemData, int> requirementBuffer = new Dictionary<ItemData, int>(); // 재료 합계 계산용
    private PlayerInventory lastPlayerInventory; // 최근 상호작용 인벤토리
    private PlayerTemperature playerTemperature; // 플레이어 체온 관리자
    private bool isConfigured; // 필수 설정 완료 여부

    public static IReadOnlyList<CampfireCookingStation> ActiveStations => activeStations; // 활성 모닥불 목록 제공
    public event Action StateChanged; // 조리 상태 변경 알림

    public CookingStationTier Tier => stationTier; // 시설 등급 제공
    public string StationDisplayName => CookingStationUtility.GetLabel(stationTier); // 시설 이름 제공
    public int SlotCount => Mathf.Clamp(slotCount, 1, 3); // 조리 칸 수 제공
    public int MaxBatchQuantity => Mathf.Max(1, maxBatchQuantity); // 최대 묶음 수 제공
    public ItemData FuelItem => fuelItem; // 연료 제공
    public bool RequiresFuel => fuelItem != null; // 119일차: 연료가 필요한 시설인지 제공
    public int FuelAmount => Mathf.Max(1, fuelAmount); // 연료 수량 제공
    public IReadOnlyList<CookingRecipeData> Recipes => recipes; // 요리법 제공
    public IReadOnlyList<CookingSlot> Slots => slots; // 조리 칸 제공
    public bool IsConfigured => isConfigured; // 설정 완료 여부 제공
    public bool IsCooking => CookingCount > 0; // 조리 중인 칸 존재 여부 제공
    public bool HasReadyResult => ReadyCount > 0; // 완성 음식 존재 여부 제공

    public int CookingCount => CountSlots(slot => slot.IsCooking); // 조리 중인 칸 수
    public int ReadyCount => CountSlots(slot => slot.IsReady); // 완성된 칸 수
    public int FreeSlotCount => CountSlots(slot => slot.IsEmpty); // 빈 칸 수

    public override string PromptMessage // 현재 조리 상태 안내 문구
    {
        get
        {
            if (!isConfigured) // 설정 오류 확인
            {
                return "CAMPFIRE DATA ERROR"; // 설정 오류 문구 반환
            }

            CookingSlot ready = FindFirst(slot => slot.IsReady); // 첫 완성 칸

            if (ready != null) // 완성 음식 존재 확인
            {
                ItemData result = ready.Recipe.ResultItem; // 완성 음식
                bool inventoryIsFull = lastPlayerInventory != null && !lastPlayerInventory.CanAddItem(result, 1); // 공간 확인

                if (inventoryIsFull) // 공간 부족 확인
                {
                    return "INVENTORY FULL | F - OPEN"; // 공간 부족 문구 반환
                }

                int readyCount = ReadyCount; // 완성 칸 수
                string more = readyCount > 1 ? $" (+{readyCount - 1})" : string.Empty; // 추가 완성 표시
                return $"F - TAKE {result.DisplayName} x{ready.ReadyAmount}{more}"; // 회수 문구 반환
            }

            CookingSlot soonest = FindSoonestCooking(); // 가장 먼저 끝나는 칸

            if (soonest != null) // 조리 진행 확인
            {
                return $"F - COOK | COOKING {FoodBuffUtility.FormatTime(soonest.RemainingSeconds)}"; // 조리 진행 문구 반환
            }

            return $"F - COOK ({StationDisplayName})"; // 조리 시작 문구 반환
        }
    }

    public bool CanRemove => !IsCooking && !HasReadyResult; // 유휴 상태 철거 허용

    public string RemovalBlockedMessage => IsCooking
        ? "COOKING IN PROGRESS"
        : "COLLECT COOKED FOOD"; // 철거 차단 원인 제공

    private void Awake() // 모닥불 조리 기능 초기화
    {
        ClampSettings(); // 설정값 범위 보정
        EnsureSlots(); // 조리 칸 준비

        bool hasMissingReference = fireVisualRoot == null; // 필수 참조 누락 확인 (119일차: 절구처럼 연료가 없는 작업대도 있다)

        if (hasMissingReference) // 참조 누락 여부 확인
        {
            Debug.LogError($"{gameObject.name}의 모닥불 조리 참조가 누락되었습니다.", this); // 참조 오류 출력
            enabled = false; // 조리 기능 비활성화
            return; // 초기화 중단
        }

        if (CountValidRecipes() == 0) // 요리법 확인
        {
            Debug.LogError($"{gameObject.name}에 요리법이 없습니다. Tools > Project U > Build Content > 4. Cooking를 실행하세요.", this); // 데이터 오류 출력
            enabled = false; // 조리 기능 비활성화
            return; // 초기화 중단
        }

        isConfigured = true; // 필수 설정 완료
        RefreshFireVisual(); // 불꽃 상태 적용
    }

    private void OnEnable() // 활성 목록 등록
    {
        if (!activeStations.Contains(this)) // 중복 확인
        {
            activeStations.Add(this); // 등록
        }
    }

    private void Update() // 조리 시간과 열기 진행
    {
        if (!IsCooking) // 조리 상태 확인
        {
            return; // 시간과 열기 처리 중단
        }

        ApplyNearbyHeat(); // 조리 중 주변 플레이어 열기 적용
        bool completedAny = false; // 이번 프레임 완성 여부

        for (int index = 0; index < slots.Count; index++) // 칸 순회
        {
            completedAny |= slots[index].Tick(Time.deltaTime); // 시간 진행
        }

        if (completedAny) // 완성 확인
        {
            NotifyChanged(); // 상태 변경 알림
        }
    }

    private void OnValidate() // Inspector 설정값 검증
    {
        ClampSettings(); // 설정값 범위 보정
    }

    // ------------------------------------------------------------ 상호작용

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

        // 완성 음식이 있으면 창을 열지 않고 먼저 꺼낸다 (가방이 가득 차 하나도 못 꺼내면 창을 연다)
        if (HasReadyResult && TryCollectAll(playerInventory) > 0) // 완성 음식 회수 시도
        {
            return; // 회수 완료
        }

        ResolveGameUIManager(); // 요리 창 관리자 확인

        if (gameUIManager != null && gameUIManager.OpenCooking(this)) // 요리 창 열기 시도
        {
            return; // 창 열기 완료
        }

        // 요리 창이 없는 Scene : 만들 수 있는 첫 요리를 바로 조리
        foreach (CookingRecipeData recipe in GetSortedRecipes(playerInventory)) // 요리법 순회
        {
            if (TryStartCooking(recipe, 1, playerInventory, out _)) // 조리 시도
            {
                return; // 조리 시작
            }
        }
    }

    public List<CookingRecipeData> GetSortedRecipes(PlayerInventory inventory) // 만들 수 있는 요리법부터 정렬한 목록
    {
        List<CookingRecipeData> sorted = new List<CookingRecipeData>(); // 결과 목록

        foreach (CookingRecipeData recipe in recipes) // 요리법 순회
        {
            if (IsValidRecipe(recipe)) // 유효 요리법 확인
            {
                sorted.Add(recipe); // 추가
            }
        }

        sorted.Sort((left, right) =>
        {
            int leftStatus = GetStatusRank(GetStatus(left, inventory, 1)); // 왼쪽 순위
            int rightStatus = GetStatusRank(GetStatus(right, inventory, 1)); // 오른쪽 순위

            if (leftStatus != rightStatus) // 상태 순위 비교
            {
                return leftStatus.CompareTo(rightStatus); // 상태 우선
            }

            return left.SortOrder != right.SortOrder
                ? left.SortOrder.CompareTo(right.SortOrder)
                : string.CompareOrdinal(left.RecipeId, right.RecipeId); // 정렬 순서
        });

        return sorted; // 결과 반환
    }

    private static int GetStatusRank(CookingRecipeStatus status) // 목록 정렬 순위
    {
        switch (status) // 상태 분기
        {
            case CookingRecipeStatus.Ready: return 0; // 조리 가능
            case CookingRecipeStatus.NoFreeSlot: return 1; // 칸 부족 (재료는 있음)
            case CookingRecipeStatus.MissingItems: return 2; // 재료 부족
            default: return 3; // 시설 부족
        }
    }

    // ------------------------------------------------------------ 조리

    public bool SupportsRecipe(CookingRecipeData recipe) // 시설 등급 충족 여부
    {
        return recipe != null && stationTier >= recipe.RequiredStation; // 결과 반환
    }

    public CookingRecipeStatus GetStatus(CookingRecipeData recipe, PlayerInventory inventory, int batchCount) // 요리법 상태 계산
    {
        if (!SupportsRecipe(recipe)) // 시설 등급 확인
        {
            return CookingRecipeStatus.NeedStation; // 시설 부족
        }

        if (inventory == null || !HasIngredients(recipe, inventory, batchCount)) // 재료 확인
        {
            return CookingRecipeStatus.MissingItems; // 재료 부족
        }

        return FreeSlotCount > 0 ? CookingRecipeStatus.Ready : CookingRecipeStatus.NoFreeSlot; // 빈 칸 확인
    }

    public int GetRequiredAmount(CookingRecipeData recipe, ItemData item, int batchCount) // 재료 하나의 필요 수량 (연료 포함)
    {
        BuildRequirements(recipe, batchCount); // 합계 계산
        return item != null && requirementBuffer.TryGetValue(item, out int amount) ? amount : 0; // 결과 반환
    }

    public bool HasIngredients(CookingRecipeData recipe, PlayerInventory inventory, int batchCount) // 재료와 연료 보유 확인
    {
        if (recipe == null || inventory == null) // 요청 확인
        {
            return false; // 부족
        }

        BuildRequirements(recipe, batchCount); // 합계 계산

        foreach (KeyValuePair<ItemData, int> pair in requirementBuffer) // 재료 순회
        {
            if (!inventory.HasItem(pair.Key, pair.Value)) // 보유 확인
            {
                return false; // 부족
            }
        }

        return true; // 충분
    }

    public int GetMaxBatch(CookingRecipeData recipe, PlayerInventory inventory) // 지금 재료로 만들 수 있는 최대 묶음 수
    {
        int best = 0; // 결과

        for (int count = 1; count <= MaxBatchQuantity; count++) // 묶음 수 순회
        {
            if (!HasIngredients(recipe, inventory, count)) // 재료 확인
            {
                break; // 중단
            }

            best = count; // 갱신
        }

        return best; // 결과 반환
    }

    public bool TryStartCooking(CookingRecipeData recipe, int batchCount, PlayerInventory inventory, out string message) // 조리 시작 시도
    {
        batchCount = Mathf.Clamp(batchCount, 1, MaxBatchQuantity); // 묶음 수 보정
        CookingRecipeStatus status = GetStatus(recipe, inventory, batchCount); // 상태 확인

        if (status != CookingRecipeStatus.Ready) // 조리 가능 확인
        {
            message = GetStatusMessage(status); // 실패 문구
            return false; // 시작 실패
        }

        CookingSlot freeSlot = FindFirst(slot => slot.IsEmpty); // 빈 칸
        BuildRequirements(recipe, batchCount); // 합계 계산
        // 재료를 뺄 때 인벤토리 변경 알림으로 요리 창이 다시 합계를 계산하므로 복사본을 사용한다
        List<KeyValuePair<ItemData, int>> needed = new List<KeyValuePair<ItemData, int>>(requirementBuffer); // 필요 목록
        List<KeyValuePair<ItemData, int>> removed = new List<KeyValuePair<ItemData, int>>(); // 제거 기록

        foreach (KeyValuePair<ItemData, int> pair in needed) // 재료 제거
        {
            int removedAmount = inventory.RemoveItem(pair.Key, pair.Value); // 제거
            removed.Add(new KeyValuePair<ItemData, int>(pair.Key, removedAmount)); // 기록

            if (removedAmount != pair.Value) // 제거 실패 확인
            {
                foreach (KeyValuePair<ItemData, int> rollback in removed) // 복구
                {
                    if (rollback.Value > 0) // 제거된 수량 확인
                    {
                        inventory.AddItem(rollback.Key, rollback.Value); // 복구
                    }
                }

                message = "MISSING INGREDIENTS"; // 실패 문구
                return false; // 시작 실패
            }
        }

        freeSlot.Start(recipe, batchCount); // 조리 시작
        RefreshFireVisual(); // 불꽃 켜기
        NotifyChanged(); // 상태 변경 알림
        message = $"COOKING {recipe.DisplayName} x{batchCount * recipe.ResultQuantity}"; // 성공 문구
        return true; // 시작 성공
    }

    public static string GetStatusMessage(CookingRecipeStatus status) // 상태별 문구
    {
        switch (status) // 상태 분기
        {
            case CookingRecipeStatus.Ready: return "READY TO COOK"; // 조리 가능
            case CookingRecipeStatus.NeedStation: return "NEEDS STONE CAMPFIRE"; // 시설 부족
            case CookingRecipeStatus.NoFreeSlot: return "ALL SLOTS BUSY"; // 칸 부족
            default: return "MISSING INGREDIENTS"; // 재료 부족
        }
    }

    public bool TryCollect(int slotIndex, PlayerInventory inventory) // 한 칸의 완성 음식 꺼내기
    {
        if (inventory == null || slotIndex < 0 || slotIndex >= slots.Count || !slots[slotIndex].IsReady) // 요청 확인
        {
            return false; // 실패
        }

        CookingSlot slot = slots[slotIndex]; // 칸
        ItemData result = slot.Recipe.ResultItem; // 완성 음식
        int amount = slot.ReadyAmount; // 꺼낼 수량
        int remaining = inventory.AddItem(result, amount); // 가방에 추가
        int added = amount - remaining; // 실제 추가 수량

        if (added <= 0) // 추가 실패 확인
        {
            return false; // 가방 가득 참
        }

        slot.TakeReady(added); // 칸에서 차감
        RefreshFireVisual(); // 불꽃 상태 적용
        NotifyChanged(); // 상태 변경 알림
        return true; // 성공
    }

    public int TryCollectAll(PlayerInventory inventory) // 모든 완성 음식 꺼내기, 꺼낸 칸 수 반환
    {
        int collected = 0; // 결과

        for (int index = 0; index < slots.Count; index++) // 칸 순회
        {
            if (TryCollect(index, inventory)) // 꺼내기
            {
                collected++; // 증가
            }
        }

        return collected; // 결과 반환
    }

    public void DebugCompleteAll() // 테스트용 : 조리 중인 칸 즉시 완성
    {
        foreach (CookingSlot slot in slots) // 칸 순회
        {
            if (slot.IsCooking) // 조리 중 확인
            {
                slot.Complete(); // 완성
            }
        }

        RefreshFireVisual(); // 불꽃 상태 적용
        NotifyChanged(); // 상태 변경 알림
    }

    private void BuildRequirements(CookingRecipeData recipe, int batchCount) // 재료·연료 합계 계산
    {
        requirementBuffer.Clear(); // 초기화

        if (recipe == null) // 요청 확인
        {
            return; // 계산 생략
        }

        int safeCount = Mathf.Max(1, batchCount); // 묶음 수

        foreach (CraftingIngredient ingredient in recipe.Ingredients) // 재료 순회
        {
            if (ingredient == null || ingredient.ItemData == null) // 빈 재료 확인
            {
                continue; // 제외
            }

            AddRequirement(ingredient.ItemData, ingredient.Amount * safeCount); // 합산
        }

        AddRequirement(fuelItem, FuelAmount); // 연료는 묶음 수와 관계없이 한 번
    }

    private void AddRequirement(ItemData item, int amount) // 합계에 더하기
    {
        if (item == null || amount <= 0) // 요청 확인
        {
            return; // 생략
        }

        requirementBuffer.TryGetValue(item, out int current); // 기존 값
        requirementBuffer[item] = current + amount; // 합산
    }

    // ------------------------------------------------------------ 저장

    public List<CookingSlotSaveData> CaptureSlots() // 조리 칸 저장 데이터 생성
    {
        List<CookingSlotSaveData> result = new List<CookingSlotSaveData>(); // 결과 목록

        foreach (CookingSlot slot in slots) // 칸 순회
        {
            result.Add(new CookingSlotSaveData
            {
                recipeId = slot.IsEmpty ? string.Empty : slot.Recipe.RecipeId,
                batchCount = slot.IsEmpty ? 0 : slot.BatchCount,
                remainingSeconds = slot.IsCooking ? slot.RemainingSeconds : 0f,
                readyAmount = slot.IsReady ? slot.ReadyAmount : 0
            }); // 칸 저장
        }

        return result; // 결과 반환
    }

    public void RestoreSlots(List<CookingSlotSaveData> savedSlots) // 조리 칸 저장 상태 적용
    {
        EnsureSlots(); // 칸 준비

        for (int index = 0; index < slots.Count; index++) // 칸 순회
        {
            CookingSlotSaveData saved = savedSlots != null && index < savedSlots.Count ? savedSlots[index] : null; // 저장 칸
            CookingRecipeData recipe = saved != null ? FindRecipe(saved.recipeId) : null; // 요리법 검색
            slots[index].Restore(recipe, saved != null ? saved.batchCount : 0, saved != null ? saved.remainingSeconds : 0f, saved != null ? saved.readyAmount : 0); // 적용
        }

        RefreshFireVisual(); // 불꽃 상태 적용
        NotifyChanged(); // 상태 변경 알림
    }

    public void RestoreFromSave(
        bool savedIsCooking,
        bool savedHasReadyResult,
        float savedRemainingCookingTime) // 85일차 이전 저장 (한 칸 구운 사과) 상태 적용
    {
        EnsureSlots(); // 칸 준비

        foreach (CookingSlot slot in slots) // 칸 비우기
        {
            slot.Clear(); // 초기화
        }

        CookingRecipeData legacyRecipe = FindLegacyRecipe(); // 이전 요리법

        if (legacyRecipe != null && (savedIsCooking || savedHasReadyResult)) // 이전 상태 확인
        {
            slots[0].Restore(legacyRecipe, 1, savedHasReadyResult ? 0f : savedRemainingCookingTime, savedHasReadyResult ? legacyRecipe.ResultQuantity : 0); // 첫 칸에 적용
        }

        RefreshFireVisual(); // 불꽃 상태 적용
        NotifyChanged(); // 상태 변경 알림
    }

    public CookingRecipeData FindRecipe(string recipeId) // ID로 요리법 검색
    {
        if (string.IsNullOrEmpty(recipeId)) // ID 확인
        {
            return null; // 없음
        }

        foreach (CookingRecipeData recipe in recipes) // 요리법 순회
        {
            if (recipe != null && string.Equals(recipe.RecipeId, recipeId, StringComparison.Ordinal)) // ID 비교
            {
                return recipe; // 결과 반환
            }
        }

        return null; // 없음
    }

    private CookingRecipeData FindLegacyRecipe() // 이전 모닥불(구운 사과) 요리법 검색
    {
        foreach (CookingRecipeData recipe in recipes) // 요리법 순회
        {
            if (IsValidRecipe(recipe) && recipe.ResultItem.ItemId == "food_baked_apple") // 구운 사과 확인
            {
                return recipe; // 결과 반환
            }
        }

        foreach (CookingRecipeData recipe in recipes) // 첫 유효 요리법
        {
            if (IsValidRecipe(recipe) && SupportsRecipe(recipe)) // 확인
            {
                return recipe; // 결과 반환
            }
        }

        return null; // 없음
    }

    // ------------------------------------------------------------ 내부 도우미

    private static bool IsValidRecipe(CookingRecipeData recipe) // 유효 요리법 확인
    {
        return recipe != null && recipe.ResultItem != null && !string.IsNullOrEmpty(recipe.RecipeId); // 결과 반환
    }

    private int CountValidRecipes() // 유효 요리법 수
    {
        int count = 0; // 결과

        if (recipes == null) // 배열 확인
        {
            return 0; // 없음
        }

        foreach (CookingRecipeData recipe in recipes) // 순회
        {
            if (IsValidRecipe(recipe)) // 확인
            {
                count++; // 증가
            }
        }

        return count; // 결과 반환
    }

    private void EnsureSlots() // 조리 칸 수 맞추기
    {
        if (slots == null) // 목록 확인
        {
            slots = new List<CookingSlot>(); // 생성
        }

        while (slots.Count < SlotCount) // 부족한 칸 추가
        {
            slots.Add(new CookingSlot()); // 추가
        }

        while (slots.Count > SlotCount) // 남는 칸 제거
        {
            slots.RemoveAt(slots.Count - 1); // 제거
        }
    }

    private int CountSlots(Predicate<CookingSlot> match) // 조건에 맞는 칸 수
    {
        int count = 0; // 결과

        foreach (CookingSlot slot in slots) // 순회
        {
            if (slot != null && match(slot)) // 조건 확인
            {
                count++; // 증가
            }
        }

        return count; // 결과 반환
    }

    private CookingSlot FindFirst(Predicate<CookingSlot> match) // 조건에 맞는 첫 칸
    {
        foreach (CookingSlot slot in slots) // 순회
        {
            if (slot != null && match(slot)) // 조건 확인
            {
                return slot; // 결과 반환
            }
        }

        return null; // 없음
    }

    public CookingSlot FindSoonestCooking() // 가장 먼저 끝나는 조리 칸
    {
        CookingSlot best = null; // 결과

        foreach (CookingSlot slot in slots) // 순회
        {
            if (slot != null && slot.IsCooking && (best == null || slot.RemainingSeconds < best.RemainingSeconds)) // 비교
            {
                best = slot; // 갱신
            }
        }

        return best; // 결과 반환
    }

    private void RefreshFireVisual() // 불꽃 연출 상태 적용
    {
        if (fireVisualRoot != null) // 불꽃 확인
        {
            fireVisualRoot.SetActive(IsCooking); // 조리 중에만 표시
        }
    }

    private void NotifyChanged() // 상태 변경 알림
    {
        StateChanged?.Invoke(); // 알림
    }

    private void ResolveGameUIManager() // 게임 UI 관리자 검색
    {
        if (gameUIManager == null) // 참조 확인
        {
            gameUIManager = FindFirstObjectByType<GameUIManager>(); // Scene 검색
        }
    }

    private void ApplyNearbyHeat() // 주변 플레이어에게 모닥불 열기 적용
    {
        PlayerTemperature targetTemperature = ResolvePlayerTemperature(); // 플레이어 체온 관리자 조회

        if (targetTemperature == null) // 플레이어 체온 관리자 확인
        {
            return; // 열기 처리 중단
        }

        float squaredDistance = (targetTemperature.transform.position - transform.position).sqrMagnitude; // 거리 제곱 계산

        if (squaredDistance > heatRadius * heatRadius) // 열기 범위 밖 확인
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

    private void ClampSettings() // 설정값 범위 보정
    {
        fuelAmount = Mathf.Max(1, fuelAmount); // 연료 수량 최소값 적용
        slotCount = Mathf.Clamp(slotCount, 1, 3); // 칸 수 제한
        maxBatchQuantity = Mathf.Clamp(maxBatchQuantity, 1, 9); // 묶음 수 제한
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
        activeStations.Remove(this); // 활성 목록 해제

        if (fireVisualRoot != null) // 불꽃 오브젝트 존재 확인
        {
            fireVisualRoot.SetActive(false); // 불꽃 연출 비활성화
        }
    }
}
