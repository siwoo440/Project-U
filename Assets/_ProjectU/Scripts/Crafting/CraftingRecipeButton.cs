using System.Text; // 제작 재료 문자열 조합 기능
using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

public sealed class CraftingRecipeButton : MonoBehaviour // 제작법 UI 항목
{
    [Header("References")] // 기능 참조 묶음
    [SerializeField] private PlayerInventory playerInventory; // 플레이어 인벤토리
    [SerializeField] private CraftingManager craftingManager; // 제작 관리자
    [SerializeField] private CraftingRecipeData recipeData; // 표시할 제작법
    [SerializeField] private TMP_Text recipeNameText; // 제작법 이름 Text
    [SerializeField] private TMP_Text ingredientText; // 제작 재료 Text
    [SerializeField] private TMP_Text statusText; // 제작 상태 Text
    [SerializeField] private Button craftButton; // 제작 실행 버튼

    private bool referencesValid; // 참조 연결 상태

    private void Awake() // 제작법 UI 초기화
    {
        referencesValid = playerInventory != null // 인벤토리 참조 확인
            && craftingManager != null // 제작 관리자 참조 확인
            && craftingManager.UnlockManager != null // 해금 관리자 참조 확인
            && recipeData != null // 제작법 참조 확인
            && recipeNameText != null // 제작법 이름 Text 확인
            && ingredientText != null // 재료 Text 확인
            && statusText != null // 상태 Text 확인
            && craftButton != null; // 제작 버튼 확인

        if (!referencesValid) // 참조 누락 확인
        {
            Debug.LogError($"{gameObject.name}의 제작 UI 참조가 누락되었습니다.", this); // 참조 오류 출력
            enabled = false; // 제작 UI 기능 비활성화
            return; // 초기화 중단
        }

        craftButton.onClick.AddListener(CraftRecipe); // 제작 버튼 기능 연결
    }

    private void OnEnable() // 상태 변경 이벤트 연결
    {
        if (!referencesValid) // 참조 상태 확인
        {
            return; // 이벤트 연결 중단
        }

        playerInventory.InventoryChanged += Refresh; // 인벤토리 변경 구독
        craftingManager.UnlockManager.UnlockStateChanged += Refresh; // 해금 변경 이벤트 구독
        Refresh(); // 현재 제작 상태 표시
    }

    private void OnDisable() // 상태 변경 이벤트 해제
    {
        if (!referencesValid) // 참조 상태 확인
        {
            return; // 이벤트 해제 중단
        }

        playerInventory.InventoryChanged -= Refresh; // 인벤토리 변경 구독 해제
        craftingManager.UnlockManager.UnlockStateChanged -= Refresh; // 해금 변경 이벤트 해제
    }

    private void OnDestroy() // 버튼 이벤트 정리
    {
        if (craftButton != null) // 제작 버튼 존재 확인
        {
            craftButton.onClick.RemoveListener(CraftRecipe); // 제작 버튼 기능 해제
        }
    }

    private void Refresh() // 제작법 화면 갱신
    {
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
            ingredientBuilder.Append($"{ingredient.ItemData.DisplayName}: {ownedQuantity} / {ingredient.Amount}"); // 보유량과 필요량 표시
        }

        ingredientText.SetText(ingredientBuilder.ToString()); // 완성된 재료 문구 표시

        bool isUnlocked = craftingManager.IsRecipeUnlocked(recipeData); // 제작법 해금 여부 확인
        bool hasFacility = craftingManager.HasRequiredFacility(recipeData); // 제작 시설 충족 여부 확인
        bool hasMaterials = craftingManager.HasRequiredMaterials(recipeData); // 재료 충족 여부 확인
        bool hasOutputSpace = craftingManager.HasOutputSpace(recipeData); // 결과 공간 여부 확인
        craftButton.interactable = isUnlocked && hasFacility && hasMaterials && hasOutputSpace; // 전체 조건에 따른 버튼 상태 적용

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
        bool craftSucceeded = craftingManager.TryCraft(recipeData); // 제작 실행

        if (!craftSucceeded) // 제작 실패 확인
        {
            Refresh(); // 최신 제작 상태 표시
            return; // 제작 처리 종료
        }

        Refresh(); // 제작 후 재료 수량 갱신
        statusText.SetText("CRAFTED"); // 제작 완료 문구 표시
    }
}
