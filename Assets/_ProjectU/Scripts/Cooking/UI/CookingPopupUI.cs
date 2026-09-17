using System.Collections.Generic; // 목록 기능
using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class CookingPopupUI : MonoBehaviour // 85일차: 모닥불 요리 창 (왼쪽 요리법 · 오른쪽 상세 · 아래 조리 칸)
{
    [Header("Root")] // 루트 묶음
    [Tooltip("켜고 끄는 창 전체 (어두운 배경 포함).")]
    [SerializeField] private GameObject panelRoot; // 창 루트
    [Tooltip("창 제목.")]
    [SerializeField] private TMP_Text titleText; // 제목
    [Tooltip("시설 정보 알약.")]
    [SerializeField] private CookingChipUI stationInfoChip; // 시설 정보
    [Tooltip("닫기 버튼.")]
    [SerializeField] private Button closeButton; // 닫기 버튼
    [Tooltip("효과·시간 아이콘 묶음.")]
    [SerializeField] private FoodEffectIconSet iconSet; // 아이콘 묶음

    [Header("Recipe List")] // 요리법 목록 묶음
    [Tooltip("요리법 줄 부모.")]
    [SerializeField] private Transform recipeListRoot; // 목록 부모
    [Tooltip("요리법 줄 템플릿 (꺼진 상태).")]
    [SerializeField] private CookingRecipeRowUI recipeRowTemplate; // 줄 템플릿

    [Header("Detail")] // 상세 묶음
    [Tooltip("상세 아이콘.")]
    [SerializeField] private Image detailIcon; // 아이콘
    [Tooltip("상세 이름.")]
    [SerializeField] private TMP_Text detailNameText; // 이름
    [Tooltip("상세 분류·설명.")]
    [SerializeField] private TMP_Text detailInfoText; // 설명
    [Tooltip("효과 알약 부모.")]
    [SerializeField] private Transform effectListRoot; // 효과 부모
    [Tooltip("효과 알약 템플릿 (꺼진 상태).")]
    [SerializeField] private CookingChipUI effectChipTemplate; // 효과 템플릿
    [Tooltip("재료 줄 부모.")]
    [SerializeField] private Transform ingredientListRoot; // 재료 부모
    [Tooltip("재료 줄 템플릿 (꺼진 상태).")]
    [SerializeField] private CookingIngredientRowUI ingredientRowTemplate; // 재료 템플릿
    [Tooltip("조리 시간 알약.")]
    [SerializeField] private CookingChipUI timeChip; // 시간
    [Tooltip("필요 시설 알약.")]
    [SerializeField] private CookingChipUI requiredStationChip; // 필요 시설
    [Tooltip("수량 문구.")]
    [SerializeField] private TMP_Text quantityText; // 수량
    [Tooltip("수량 감소 버튼.")]
    [SerializeField] private Button minusButton; // 감소
    [Tooltip("수량 증가 버튼.")]
    [SerializeField] private Button plusButton; // 증가
    [Tooltip("조리 버튼.")]
    [SerializeField] private Button cookButton; // 조리
    [Tooltip("조리 버튼 배경.")]
    [SerializeField] private Image cookButtonImage; // 조리 버튼 배경
    [Tooltip("조리 버튼 문구.")]
    [SerializeField] private TMP_Text cookButtonLabel; // 조리 버튼 문구

    [Header("Slots")] // 조리 칸 묶음
    [Tooltip("조리 칸 표시 (최대 3칸).")]
    [SerializeField] private CookingSlotView[] slotViews = new CookingSlotView[0]; // 조리 칸
    [Tooltip("조리 칸 제목.")]
    [SerializeField] private TMP_Text slotHeaderText; // 칸 제목

    [Header("Feedback")] // 알림 묶음
    [Tooltip("결과 알림 문구.")]
    [SerializeField] private TMP_Text messageText; // 알림
    [Tooltip("알림 표시 시간.")]
    [SerializeField, Min(0.5f)] private float messageDuration = 2.2f; // 알림 시간

    private readonly List<CookingRecipeRowUI> rows = new List<CookingRecipeRowUI>(); // 요리법 줄
    private readonly List<CookingChipUI> effectChips = new List<CookingChipUI>(); // 효과 알약
    private readonly List<CookingIngredientRowUI> ingredientRows = new List<CookingIngredientRowUI>(); // 재료 줄
    private readonly List<FoodEffectEntry> effectBuffer = new List<FoodEffectEntry>(); // 효과 계산용
    private readonly List<ItemData> ingredientBuffer = new List<ItemData>(); // 재료 계산용

    private GameUIManager manager; // 팝업 관리자
    private CampfireCookingStation station; // 현재 모닥불
    private PlayerInventory inventory; // 플레이어 인벤토리
    private CookingRecipeData selectedRecipe; // 선택 요리법
    private int quantity = 1; // 선택 수량
    private bool isDirty; // 다시 그리기 필요
    private float messageHideTime; // 알림 숨김 시각

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf && station != null; // 열림 여부 제공
    public CampfireCookingStation Station => station; // 현재 모닥불 제공
    public CookingRecipeData SelectedRecipe => selectedRecipe; // 선택 요리법 제공
    public int Quantity => quantity; // 선택 수량 제공

    private void Awake() // 버튼 연결과 템플릿 숨김
    {
        if (closeButton != null) closeButton.onClick.AddListener(RequestClose); // 닫기
        if (minusButton != null) minusButton.onClick.AddListener(() => ChangeQuantity(-1)); // 감소
        if (plusButton != null) plusButton.onClick.AddListener(() => ChangeQuantity(1)); // 증가
        if (cookButton != null) cookButton.onClick.AddListener(CookSelected); // 조리
        if (recipeRowTemplate != null) recipeRowTemplate.gameObject.SetActive(false); // 템플릿 숨김
        if (effectChipTemplate != null) effectChipTemplate.gameObject.SetActive(false); // 템플릿 숨김
        if (ingredientRowTemplate != null) ingredientRowTemplate.gameObject.SetActive(false); // 템플릿 숨김

        if (panelRoot != null && station == null) // 시작 상태
        {
            panelRoot.SetActive(false); // 숨김
        }
    }

    private void OnDestroy() // 구독 해제
    {
        Unsubscribe(); // 해제
    }

    public bool ShowFromManager(GameUIManager owner, CampfireCookingStation targetStation, PlayerInventory playerInventory) // 창 열기
    {
        if (panelRoot == null || targetStation == null || playerInventory == null || recipeRowTemplate == null) // 참조 확인
        {
            Debug.LogError("요리 창 참조가 누락되었습니다. Tools > Project U > Cooking > 1. Build Cooking Content를 다시 실행하세요.", this); // 오류 출력
            return false; // 열기 실패
        }

        Unsubscribe(); // 이전 구독 해제
        manager = owner; // 관리자
        station = targetStation; // 모닥불
        inventory = playerInventory; // 인벤토리
        station.StateChanged += MarkDirty; // 모닥불 변경 구독
        inventory.InventoryChanged += MarkDirty; // 인벤토리 변경 구독
        quantity = 1; // 수량 초기화
        List<CookingRecipeData> sorted = station.GetSortedRecipes(inventory); // 정렬 목록
        selectedRecipe = sorted.Count > 0 ? sorted[0] : null; // 첫 요리법 선택
        panelRoot.SetActive(true); // 표시
        ShowMessage(string.Empty, Color.clear, 0f); // 알림 초기화
        RebuildAll(); // 그리기
        return true; // 열기 성공
    }

    public void HideFromManager() // 창 닫기
    {
        Unsubscribe(); // 구독 해제
        station = null; // 모닥불 해제
        inventory = null; // 인벤토리 해제

        if (panelRoot != null) // 루트 확인
        {
            panelRoot.SetActive(false); // 숨김
        }
    }

    private void Unsubscribe() // 이벤트 해제
    {
        if (station != null) station.StateChanged -= MarkDirty; // 모닥불 해제
        if (inventory != null) inventory.InventoryChanged -= MarkDirty; // 인벤토리 해제
    }

    private void MarkDirty() // 다시 그리기 요청
    {
        isDirty = true; // 기록
    }

    private void Update() // 상태 갱신
    {
        if (panelRoot == null || !panelRoot.activeSelf) // 열림 확인
        {
            return; // 생략
        }

        if (station == null || !station.isActiveAndEnabled) // 모닥불이 사라짐 (철거·불러오기)
        {
            RequestClose(); // 닫기
            return; // 생략
        }

        if (isDirty) // 변경 확인
        {
            RebuildAll(); // 다시 그리기
        }
        else
        {
            RefreshSlots(); // 진행도만 갱신
        }

        if (messageText != null && messageText.gameObject.activeSelf && Time.unscaledTime >= messageHideTime) // 알림 시간 확인
        {
            messageText.gameObject.SetActive(false); // 알림 숨김
        }
    }

    private void RequestClose() // 닫기 요청
    {
        if (manager != null) // 관리자 확인
        {
            manager.CloseCooking(); // 관리자 통해 닫기 (입력 잠금 해제)
            return; // 완료
        }

        HideFromManager(); // 직접 닫기
    }

    // ------------------------------------------------------------ 그리기

    private void RebuildAll() // 전체 다시 그리기
    {
        isDirty = false; // 기록 해제

        if (station == null || inventory == null) // 상태 확인
        {
            return; // 생략
        }

        titleText.SetText($"{station.StationDisplayName} COOKING"); // 제목

        if (stationInfoChip != null) // 시설 정보
        {
            stationInfoChip.Bind($"{station.SlotCount} SLOTS  ·  {station.FuelAmount} {station.FuelItem.DisplayName} / COOK", ProjectUUIPalette.Accent, iconSet != null ? iconSet.Flame : null); // 표시
        }

        List<CookingRecipeData> sorted = station.GetSortedRecipes(inventory); // 정렬 목록

        if (selectedRecipe == null || !sorted.Contains(selectedRecipe)) // 선택 확인
        {
            selectedRecipe = sorted.Count > 0 ? sorted[0] : null; // 첫 요리법
            quantity = 1; // 수량 초기화
        }

        for (int index = 0; index < sorted.Count; index++) // 줄 그리기
        {
            CookingRecipeRowUI row = GetRow(index); // 줄
            CookingRecipeData recipe = sorted[index]; // 요리법
            row.transform.SetSiblingIndex(index + 1); // 순서 (템플릿 다음)
            row.Bind(recipe, station.GetStatus(recipe, inventory, 1), recipe == selectedRecipe, SelectRecipe); // 표시
        }

        for (int index = sorted.Count; index < rows.Count; index++) // 남는 줄
        {
            rows[index].gameObject.SetActive(false); // 숨김
        }

        RefreshDetail(); // 상세
        RefreshSlots(); // 조리 칸
    }

    private CookingRecipeRowUI GetRow(int index) // 줄 가져오기 (없으면 만들기)
    {
        while (rows.Count <= index) // 부족한 줄
        {
            CookingRecipeRowUI created = Instantiate(recipeRowTemplate, recipeListRoot); // 복제
            created.name = $"Recipe_{rows.Count:00}"; // 이름
            rows.Add(created); // 추가
        }

        return rows[index]; // 결과 반환
    }

    private void RefreshDetail() // 오른쪽 상세 그리기
    {
        if (selectedRecipe == null) // 선택 확인
        {
            detailNameText.SetText("NO RECIPES"); // 이름
            detailInfoText.SetText(string.Empty); // 설명
            detailIcon.enabled = false; // 아이콘
            SetCookButton(false, "NO RECIPES"); // 버튼
            HideFrom(effectChips, 0); // 효과 숨김
            HideFrom(ingredientRows, 0); // 재료 숨김
            return; // 완료
        }

        ItemData result = selectedRecipe.ResultItem; // 완성 음식
        detailIcon.enabled = true; // 아이콘 표시
        detailIcon.sprite = result.Icon; // 아이콘
        detailIcon.color = result.Icon != null ? Color.white : ItemIconUtility.GetFallbackColor(result.ItemCategory); // 아이콘 색
        detailNameText.SetText(result.DisplayName); // 이름
        detailInfoText.SetText(string.IsNullOrWhiteSpace(result.Description) ? "FOOD" : result.Description); // 설명

        // 효과 알약
        FoodEffectUtility.Collect(result, effectBuffer); // 효과 목록

        for (int index = 0; index < effectBuffer.Count; index++) // 알약 그리기
        {
            FoodEffectEntry entry = effectBuffer[index]; // 효과
            GetPooled(effectChips, effectChipTemplate, effectListRoot, index, "Effect").Bind(entry.Label, entry.Color, iconSet != null ? iconSet.Get(entry) : null); // 표시
        }

        HideFrom(effectChips, effectBuffer.Count); // 남는 알약 숨김

        // 재료 (연료 포함, 같은 아이템은 합쳐서)
        int maxBatch = station.GetMaxBatch(selectedRecipe, inventory); // 최대 묶음
        quantity = Mathf.Clamp(quantity, 1, station.MaxBatchQuantity); // 수량 보정
        ingredientBuffer.Clear(); // 초기화

        foreach (CraftingIngredient ingredient in selectedRecipe.Ingredients) // 재료 순회
        {
            if (ingredient != null && ingredient.ItemData != null && !ingredientBuffer.Contains(ingredient.ItemData)) // 중복 제외
            {
                ingredientBuffer.Add(ingredient.ItemData); // 추가
            }
        }

        if (station.FuelItem != null && !ingredientBuffer.Contains(station.FuelItem)) // 연료
        {
            ingredientBuffer.Add(station.FuelItem); // 추가
        }

        for (int index = 0; index < ingredientBuffer.Count; index++) // 재료 줄 그리기
        {
            ItemData item = ingredientBuffer[index]; // 재료
            int need = station.GetRequiredAmount(selectedRecipe, item, quantity); // 필요 수량
            int have = inventory.GetItemQuantity(item); // 보유 수량
            GetPooled(ingredientRows, ingredientRowTemplate, ingredientListRoot, index, "Ingredient").Bind(item, have, need, item == station.FuelItem); // 표시
        }

        HideFrom(ingredientRows, ingredientBuffer.Count); // 남는 줄 숨김

        // 시간·시설·수량
        timeChip.Bind(FoodBuffUtility.FormatTime(selectedRecipe.GetCookingSeconds(quantity)), ProjectUUIPalette.TextSecondary, iconSet != null ? iconSet.Time : null); // 시간
        bool supported = station.SupportsRecipe(selectedRecipe); // 시설 충족
        requiredStationChip.Bind(CookingStationUtility.GetLabel(selectedRecipe.RequiredStation), supported ? ProjectUUIPalette.Teal : ProjectUUIPalette.Danger, iconSet != null ? iconSet.Flame : null); // 시설
        quantityText.SetText($"x{quantity}"); // 수량
        minusButton.interactable = quantity > 1; // 감소 가능
        plusButton.interactable = quantity < station.MaxBatchQuantity; // 증가 가능

        CookingRecipeStatus status = station.GetStatus(selectedRecipe, inventory, quantity); // 상태
        bool ready = status == CookingRecipeStatus.Ready; // 조리 가능
        bool slotsBusy = supported && station.FreeSlotCount == 0; // 빈 칸 없음
        string label = ready
            ? $"COOK  x{quantity * selectedRecipe.ResultQuantity}"
            : slotsBusy
                ? CampfireCookingStation.GetStatusMessage(CookingRecipeStatus.NoFreeSlot)
                : status == CookingRecipeStatus.MissingItems && maxBatch > 0
                ? $"ONLY x{maxBatch} POSSIBLE"
                : CampfireCookingStation.GetStatusMessage(status); // 버튼 문구
        SetCookButton(ready, label); // 버튼
    }

    private void SetCookButton(bool ready, string label) // 조리 버튼 상태
    {
        cookButton.interactable = ready; // 조작 가능
        cookButtonLabel.SetText(label); // 문구
        cookButtonLabel.color = ready ? ProjectUUIPalette.TextDark : ProjectUUIPalette.TextSecondary; // 문구 색
        cookButtonImage.color = ready ? ProjectUUIPalette.Accent : ProjectUUIPalette.ButtonDisabled; // 배경 색
    }

    private void RefreshSlots() // 조리 칸 그리기
    {
        if (station == null) // 상태 확인
        {
            return; // 생략
        }

        IReadOnlyList<CookingSlot> slots = station.Slots; // 칸 목록

        for (int index = 0; index < slotViews.Length; index++) // 칸 순회
        {
            if (slotViews[index] == null) // 빈 참조
            {
                continue; // 제외
            }

            if (index >= station.SlotCount) // 없는 칸
            {
                slotViews[index].gameObject.SetActive(false); // 숨김
                continue; // 다음
            }

            slotViews[index].Bind(index, index < slots.Count ? slots[index] : null, TakeSlot); // 표시
        }

        if (slotHeaderText != null) // 칸 제목
        {
            slotHeaderText.SetText($"FIRE SLOTS   {station.CookingCount} COOKING · {station.ReadyCount} READY · {station.FreeSlotCount} EMPTY"); // 제목
        }
    }

    // ------------------------------------------------------------ 입력

    private void SelectRecipe(CookingRecipeData recipe) // 요리법 선택
    {
        if (recipe == null || recipe == selectedRecipe) // 변경 확인
        {
            return; // 생략
        }

        selectedRecipe = recipe; // 선택
        quantity = 1; // 수량 초기화
        RebuildAll(); // 다시 그리기
    }

    public void SelectRecipeForTest(CookingRecipeData recipe, int newQuantity) // 테스트용 선택
    {
        SelectRecipe(recipe); // 선택
        quantity = Mathf.Clamp(newQuantity, 1, station != null ? station.MaxBatchQuantity : 1); // 수량
        RebuildAll(); // 다시 그리기
    }

    private void ChangeQuantity(int delta) // 수량 변경
    {
        if (station == null) // 상태 확인
        {
            return; // 생략
        }

        quantity = Mathf.Clamp(quantity + delta, 1, station.MaxBatchQuantity); // 수량 적용
        RefreshDetail(); // 상세 갱신
    }

    public void CookSelected() // 조리 시작
    {
        if (station == null || selectedRecipe == null) // 상태 확인
        {
            return; // 생략
        }

        bool started = station.TryStartCooking(selectedRecipe, quantity, inventory, out string message); // 조리 시도
        ShowMessage(message, started ? ProjectUUIPalette.Teal : ProjectUUIPalette.Danger, messageDuration); // 알림

        if (started) // 성공
        {
            quantity = 1; // 수량 초기화
        }

        RebuildAll(); // 다시 그리기
    }

    public void TakeSlot(int slotIndex) // 완성 음식 꺼내기
    {
        if (station == null || slotIndex < 0 || slotIndex >= station.Slots.Count) // 상태 확인
        {
            return; // 생략
        }

        CookingSlot slot = station.Slots[slotIndex]; // 칸
        string name = slot.IsReady ? slot.Recipe.ResultItem.DisplayName : string.Empty; // 이름
        bool taken = station.TryCollect(slotIndex, inventory); // 꺼내기
        ShowMessage(taken ? $"TOOK {name}" : "INVENTORY FULL", taken ? ProjectUUIPalette.Teal : ProjectUUIPalette.Danger, messageDuration); // 알림
        RebuildAll(); // 다시 그리기
    }

    private void ShowMessage(string text, Color color, float duration) // 알림 표시
    {
        if (messageText == null) // 알림 확인
        {
            return; // 생략
        }

        bool visible = !string.IsNullOrEmpty(text) && duration > 0f; // 표시 여부
        messageText.gameObject.SetActive(visible); // 표시
        messageText.SetText(text); // 문구
        messageText.color = color; // 색
        messageHideTime = Time.unscaledTime + duration; // 숨김 시각
    }

    // ------------------------------------------------------------ 목록 재사용

    private static T GetPooled<T>(List<T> pool, T template, Transform parent, int index, string prefix) where T : Component // 재사용 항목
    {
        while (pool.Count <= index) // 부족한 항목
        {
            T created = Instantiate(template, parent); // 복제
            created.name = $"{prefix}_{pool.Count:00}"; // 이름
            pool.Add(created); // 추가
        }

        pool[index].transform.SetSiblingIndex(index + 1); // 순서 (템플릿 다음)
        return pool[index]; // 결과 반환
    }

    private static void HideFrom<T>(List<T> pool, int start) where T : Component // 남는 항목 숨김
    {
        for (int index = start; index < pool.Count; index++) // 순회
        {
            pool[index].gameObject.SetActive(false); // 숨김
        }
    }
}
