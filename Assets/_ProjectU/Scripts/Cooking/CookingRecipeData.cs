using System.Collections.Generic; // 읽기 전용 목록 기능
using UnityEngine; // Unity 기본 기능

[CreateAssetMenu(fileName = "CookingRecipe_New", menuName = "Project U/Cooking Recipe Data")] // 요리법 생성 메뉴
public sealed class CookingRecipeData : ScriptableObject // 85일차: 모닥불 요리법 데이터
{
    [Header("Identity")] // 식별 정보
    [Tooltip("요리법 고유 ID.")]
    [SerializeField] private string recipeId = "cook_new"; // 요리법 ID
    [Tooltip("요리법 표시 이름.")]
    [SerializeField] private string displayName = "NEW DISH"; // 표시 이름
    [Tooltip("목록 정렬 순서 (작을수록 위).")]
    [SerializeField] private int sortOrder; // 정렬 순서

    [Header("Requirements")] // 요구 조건
    [Tooltip("필요한 최소 조리 시설 등급.")]
    [SerializeField] private CookingStationTier requiredStation = CookingStationTier.Campfire; // 필요 시설
    [Tooltip("한 번 조리에 필요한 재료 목록 (연료는 시설에서 따로 사용).")]
    [SerializeField] private CraftingIngredient[] ingredients = new CraftingIngredient[0]; // 재료 목록

    [Header("Result")] // 결과
    [Tooltip("완성 음식.")]
    [SerializeField] private ItemData resultItem; // 완성 음식
    [Tooltip("한 번 조리할 때 나오는 음식 수량.")]
    [SerializeField, Min(1)] private int resultQuantity = 1; // 결과 수량

    [Header("Time")] // 조리 시간
    [Tooltip("한 개를 조리하는 시간 (초).")]
    [SerializeField, Min(0.5f)] private float cookingSeconds = 6f; // 조리 시간
    [Tooltip("여러 개를 한 번에 조리할 때 추가 한 개당 시간 비율.")]
    [SerializeField, Range(0.1f, 1f)] private float extraBatchTimeRatio = 0.6f; // 묶음 추가 시간 비율

    public string RecipeId => recipeId; // ID 제공
    public string DisplayName => displayName; // 이름 제공
    public int SortOrder => sortOrder; // 정렬 순서 제공
    public CookingStationTier RequiredStation => requiredStation; // 필요 시설 제공
    public IReadOnlyList<CraftingIngredient> Ingredients => ingredients; // 재료 제공
    public ItemData ResultItem => resultItem; // 결과 제공
    public int ResultQuantity => Mathf.Max(1, resultQuantity); // 결과 수량 제공
    public float CookingSeconds => Mathf.Max(0.5f, cookingSeconds); // 조리 시간 제공

    public float GetCookingSeconds(int batchCount) // 묶음 조리 시간 계산
    {
        int safeCount = Mathf.Max(1, batchCount); // 최소 1개
        return CookingSeconds * (1f + (safeCount - 1) * extraBatchTimeRatio); // 결과 반환
    }

    private void OnValidate() // Inspector 값 보정
    {
        recipeId = string.IsNullOrWhiteSpace(recipeId) ? string.Empty : recipeId.Trim(); // ID 공백 제거
        displayName = string.IsNullOrWhiteSpace(displayName) ? string.Empty : displayName.Trim(); // 이름 공백 제거
        resultQuantity = Mathf.Max(1, resultQuantity); // 결과 수량 보정
        cookingSeconds = Mathf.Max(0.5f, cookingSeconds); // 시간 보정

        if (ingredients == null) // 재료 배열 확인
        {
            ingredients = new CraftingIngredient[0]; // 빈 배열
        }
    }
}
