using System.Collections.Generic; // 읽기 전용 목록 기능
using UnityEngine; // Unity 기본 기능

[CreateAssetMenu(fileName = "CraftingRecipe_New", menuName = "Project U/Crafting Recipe Data")] // 제작법 생성 메뉴
public sealed class CraftingRecipeData : ScriptableObject // 제작법 데이터
{
    [Header("Identity")] // 제작법 식별 정보
    [SerializeField] private string recipeId = "recipe_new"; // 제작법 고유 ID
    [SerializeField] private string displayName = "NEW RECIPE"; // 제작법 표시 이름

    [Header("Requirements")] // 제작 요구 조건
    [SerializeField] private CraftingFacilityType requiredFacility = CraftingFacilityType.Hand; // 필요 제작 시설
    [SerializeField] private CraftingUnlockType unlockType = CraftingUnlockType.Default; // 제작법 해금 종류
    [SerializeField] private string unlockId = string.Empty; // 해금 조건 고유 ID

    [Header("Result")] // 제작 결과 정보
    [SerializeField] private ItemData resultItem; // 결과 아이템 데이터
    [SerializeField] private int resultQuantity = 1; // 결과 아이템 수량

    [Header("Ingredients")] // 제작 재료 정보
    [SerializeField] private CraftingIngredient[] ingredients = new CraftingIngredient[0]; // 필요 재료 목록

    public string RecipeId => recipeId; // 제작법 ID 제공
    public string DisplayName => displayName; // 제작법 이름 제공
    public CraftingFacilityType RequiredFacility => requiredFacility; // 필요 시설 제공
    public string RequiredFacilityId => CraftingFacilityIds.GetFacilityId(requiredFacility); // 필요 시설 ID 제공
    public CraftingUnlockType UnlockType => unlockType; // 해금 종류 제공
    public string UnlockId => unlockId; // 해금 조건 ID 제공
    public bool IsDefaultUnlocked => unlockType == CraftingUnlockType.Default; // 기본 해금 여부 제공
    public ItemData ResultItem => resultItem; // 결과 아이템 제공
    public int ResultQuantity => Mathf.Max(1, resultQuantity); // 결과 수량 제공
    public IReadOnlyList<CraftingIngredient> Ingredients => ingredients; // 필요 재료 목록 제공

    private void OnValidate() // Inspector 설정값 검증
    {
        recipeId = string.IsNullOrWhiteSpace(recipeId) ? string.Empty : recipeId.Trim(); // 제작법 ID 공백 제거
        displayName = string.IsNullOrWhiteSpace(displayName) ? string.Empty : displayName.Trim(); // 제작법 이름 공백 제거
        unlockId = string.IsNullOrWhiteSpace(unlockId) ? string.Empty : unlockId.Trim(); // 해금 ID 공백 제거
        resultQuantity = Mathf.Max(1, resultQuantity); // 결과 수량 최소값 적용

        if (unlockType == CraftingUnlockType.Default) // 기본 해금 제작법 확인
        {
            unlockId = string.Empty; // 불필요한 해금 ID 제거
        }

        if (ingredients == null) // 재료 배열 존재 확인
        {
            ingredients = new CraftingIngredient[0]; // 빈 재료 배열 생성
        }
    }
}
