using System.Text; // 제작 재료 문자열 조합 기능
using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class CraftingRecipeButton : MonoBehaviour // 제작법 UI 항목
{
    [Header("Runtime References")] // 런타임 기능 참조 묶음
    [Tooltip("플레이어 인벤토리.")]
    [SerializeField] private PlayerInventory playerInventory; // 플레이어 인벤토리
    [Tooltip("제작 관리자.")]
    [SerializeField] private CraftingManager craftingManager; // 제작 관리자

    [Header("Recipe")] // 제작법 데이터 묶음
    [Tooltip("표시 제작법.")]
    [SerializeField] private CraftingRecipeData recipeData; // 표시 제작법

    [Header("Display")] // 화면 요소 참조 묶음
    [Tooltip("제작법 이름 Text.")]
    [SerializeField] private TMP_Text recipeNameText; // 제작법 이름 Text
    [Tooltip("제작 재료 Text.")]
    [SerializeField] private TMP_Text ingredientText; // 제작 재료 Text
    [Tooltip("제작 상태 Text.")]
    [SerializeField] private TMP_Text statusText; // 제작 상태 Text
    [Tooltip("제작 실행 버튼.")]
    [SerializeField] private Button craftButton; // 제작 실행 버튼

    private bool internalReferencesValid; // UI 내부 참조 상태
    private bool runtimeInitialized; // 런타임 기능 참조 초기화 상태
    private bool eventsSubscribed; // 제작 이벤트 구독 상태

    private void Awake() // 제작법 UI 내부 초기화
    {
        internalReferencesValid =
            recipeData != null
            && recipeNameText != null
            && ingredientText != null
            && statusText != null
            && craftButton != null; // UI 내부 참조 확인

        if (!internalReferencesValid) // 내부 참조 누락 확인
        {
            Debug.LogError($"{gameObject.name}의 제작 UI 내부 참조가 누락되었습니다.", this); // 참조 오류 출력
            enabled = false; // 제작 UI 기능 비활성화
            return; // 초기화 중단
        }

        craftButton.onClick.AddListener(CraftRecipe); // 제작 버튼 기능 연결
    }

    private void OnEnable() // 상태 변경 이벤트 연결
    {
        if (!runtimeInitialized) // 런타임 초기화 상태 확인
        {
            return; // 이벤트 연결 중단
        }

        SubscribeEvents(); // 제작 상태 이벤트 연결
        Refresh(); // 현재 제작 상태 표시
    }

    private void OnDisable() // 상태 변경 이벤트 해제
    {
        UnsubscribeEvents(); // 제작 상태 이벤트 해제
    }

    public bool Initialize(
        PlayerInventory inventory,
        CraftingManager manager) // 런타임 제작 기능 참조 초기화
    {
        if (!internalReferencesValid
            || inventory == null
            || manager == null
            || manager.UnlockManager == null) // 내부와 외부 참조 확인
        {
            Debug.LogError($"{gameObject.name}의 제작 UI 런타임 참조가 누락되었습니다.", this); // 참조 오류 출력
            runtimeInitialized = false; // 초기화 실패 기록
            return false; // 초기화 실패 반환
        }

        UnsubscribeEvents(); // 기존 제작 이벤트 해제
        playerInventory = inventory; // 플레이어 인벤토리 저장
        craftingManager = manager; // 제작 관리자 저장
        runtimeInitialized = true; // 런타임 초기화 완료 기록

        if (isActiveAndEnabled) // 현재 화면 활성 상태 확인
        {
            SubscribeEvents(); // 제작 상태 이벤트 연결
            Refresh(); // 현재 제작 상태 표시
        }

        return true; // 초기화 성공 반환
    }

    private void SubscribeEvents() // 제작 상태 이벤트 연결
    {
        if (eventsSubscribed
            || playerInventory == null
            || craftingManager == null
            || craftingManager.UnlockManager == null) // 기존 구독과 관리자 확인
        {
            return; // 중복 구독 생략
        }

        playerInventory.InventoryChanged += Refresh; // 인벤토리 변경 구독
        craftingManager.UnlockManager.UnlockStateChanged += Refresh; // 해금 변경 구독
        craftingManager.ActiveFacilityChanged += Refresh; // 제작 시설 변경 구독
        eventsSubscribed = true; // 이벤트 구독 완료 기록
    }

    private void UnsubscribeEvents() // 제작 상태 이벤트 해제
    {
        if (!eventsSubscribed
            || playerInventory == null
            || craftingManager == null
            || craftingManager.UnlockManager == null) // 구독 상태와 관리자 확인
        {
            eventsSubscribed = false; // 이벤트 구독 상태 초기화
            return; // 이벤트 해제 생략
        }

        playerInventory.InventoryChanged -= Refresh; // 인벤토리 변경 구독 해제
        craftingManager.UnlockManager.UnlockStateChanged -= Refresh; // 해금 변경 구독 해제
        craftingManager.ActiveFacilityChanged -= Refresh; // 제작 시설 변경 구독 해제
        eventsSubscribed = false; // 이벤트 구독 상태 초기화
    }

    private void Refresh() // 제작법 화면 갱신
    {
        if (!runtimeInitialized
            || playerInventory == null
            || craftingManager == null) // 런타임 초기화 상태 확인
        {
            return; // 제작 화면 갱신 중단
        }

        recipeNameText.SetText($"{recipeData.DisplayName} x{recipeData.ResultQuantity}"); // 제작 결과 이름 표시

        StringBuilder ingredientBuilder = new StringBuilder(); // 재료 문구 조합기 생성

        for (int index = 0; index < recipeData.Ingredients.Count; index++) // 전체 재료 순회
        {
            CraftingIngredient ingredient = recipeData.Ingredients[index]; // 현재 재료 조회

            if (ingredient == null || ingredient.ItemData == null) // 재료 데이터 연결 확인
            {
                continue; // 잘못된 재료 제외
            }

            if (ingredientBuilder.Length > 0) // 기존 재료 문구 확인
            {
                ingredientBuilder.AppendLine(); // 다음 재료 줄 추가
            }

            int ownedQuantity = playerInventory.GetItemQuantity(ingredient.ItemData); // 현재 보유량 조회
            ingredientBuilder.Append(
                $"{ingredient.ItemData.DisplayName}: {ownedQuantity} / {ingredient.Amount}"); // 보유량 표시
        }

        ingredientText.SetText(ingredientBuilder.ToString()); // 완성 재료 문구 표시

        bool isUnlocked = craftingManager.IsRecipeUnlocked(recipeData); // 제작법 해금 여부 확인
        bool hasFacility = craftingManager.HasRequiredFacility(recipeData); // 제작 시설 충족 여부 확인
        bool hasMaterials = craftingManager.HasRequiredMaterials(recipeData); // 재료 충족 여부 확인
        bool hasOutputSpace = craftingManager.HasOutputSpace(recipeData); // 결과 공간 여부 확인
        craftButton.interactable =
            isUnlocked
            && hasFacility
            && hasMaterials
            && hasOutputSpace; // 제작 버튼 상태 적용

        if (!isUnlocked) // 제작법 잠금 확인
        {
            statusText.SetText("LOCKED"); // 잠금 상태 표시
            return; // 상태 갱신 종료
        }

        if (!hasFacility) // 제작 시설 불일치 확인
        {
            statusText.SetText("WRONG FACILITY"); // 시설 불일치 표시
            return; // 상태 갱신 종료
        }

        if (!hasMaterials) // 재료 부족 확인
        {
            statusText.SetText("NEED MATERIALS"); // 재료 부족 표시
            return; // 상태 갱신 종료
        }

        if (!hasOutputSpace) // 인벤토리 공간 부족 확인
        {
            statusText.SetText("INVENTORY FULL"); // 공간 부족 표시
            return; // 상태 갱신 종료
        }

        statusText.SetText("READY"); // 제작 가능 상태 표시
    }

    private void CraftRecipe() // 제작 버튼 실행
    {
        if (!runtimeInitialized || craftingManager == null) // 런타임 제작 관리자 확인
        {
            return; // 제작 실행 중단
        }

        bool craftSucceeded = craftingManager.TryCraft(recipeData); // 제작 실행

        if (!craftSucceeded) // 제작 실패 확인
        {
            Refresh(); // 최신 제작 상태 표시
            return; // 제작 처리 종료
        }

        Refresh(); // 제작 후 수량 갱신
        statusText.SetText("CRAFTED"); // 제작 완료 문구 표시
    }

    private void OnDestroy() // 버튼과 이벤트 정리
    {
        UnsubscribeEvents(); // 제작 상태 이벤트 해제

        if (craftButton != null) // 제작 버튼 존재 확인
        {
            craftButton.onClick.RemoveListener(CraftRecipe); // 제작 버튼 기능 해제
        }
    }
}
