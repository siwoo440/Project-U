using System.Collections.Generic; // 제거된 재료 임시 목록 기능
using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class CraftingManager : MonoBehaviour // 플레이어 제작 관리자
{
    [Header("References")] // 외부 참조 묶음
    [SerializeField] private PlayerInventory playerInventory; // 플레이어 인벤토리

    public bool HasRequiredMaterials(CraftingRecipeData recipeData) // 제작 재료 충족 여부 확인
    {
        if (playerInventory == null || recipeData == null) // 필수 데이터 확인
        {
            return false; // 제작 재료 부족 반환
        }

        IReadOnlyList<CraftingIngredient> ingredients = recipeData.Ingredients; // 제작 재료 목록 조회

        for (int index = 0; index < ingredients.Count; index++) // 전체 제작 재료 순회
        {
            CraftingIngredient ingredient = ingredients[index]; // 현재 제작 재료 조회

            if (ingredient == null || ingredient.ItemData == null) // 재료 설정 확인
            {
                return false; // 제작 불가능 반환
            }

            if (!playerInventory.HasItem(ingredient.ItemData, ingredient.Amount)) // 보유 수량 확인
            {
                return false; // 제작 재료 부족 반환
            }
        }

        return true; // 모든 재료 충족 반환
    }

    public bool HasOutputSpace(CraftingRecipeData recipeData) // 제작 결과 보관 공간 확인
    {
        if (playerInventory == null || recipeData == null || recipeData.ResultItem == null) // 필수 데이터 확인
        {
            return false; // 공간 확인 실패 반환
        }

        return playerInventory.CanAddItem(recipeData.ResultItem, recipeData.ResultQuantity); // 결과 배치 가능 여부 반환
    }

    public bool CanCraft(CraftingRecipeData recipeData) // 최종 제작 가능 여부 확인
    {
        return HasRequiredMaterials(recipeData) && HasOutputSpace(recipeData); // 재료와 공간 조건 반환
    }

    public bool TryCraft(CraftingRecipeData recipeData) // 제작 실행 시도
    {
        if (!CanCraft(recipeData)) // 제작 조건 확인
        {
            return false; // 제작 실패 반환
        }

        int remainingResult = playerInventory.AddItem(recipeData.ResultItem, recipeData.ResultQuantity); // 결과 아이템 우선 추가

        if (remainingResult > 0) // 결과 아이템 추가 실패 확인
        {
            int addedResult = recipeData.ResultQuantity - remainingResult; // 실제 추가된 결과 수량 계산

            if (addedResult > 0) // 일부 결과 추가 여부 확인
            {
                playerInventory.RemoveItem(recipeData.ResultItem, addedResult); // 잘못 추가된 결과 회수
            }

            return false; // 제작 실패 반환
        }

        List<CraftingIngredient> removedIngredients = new List<CraftingIngredient>(); // 제거 완료 재료 목록

        for (int index = 0; index < recipeData.Ingredients.Count; index++) // 전체 재료 순회
        {
            CraftingIngredient ingredient = recipeData.Ingredients[index]; // 현재 제작 재료 조회
            int removedAmount = playerInventory.RemoveItem(ingredient.ItemData, ingredient.Amount); // 제작 재료 제거

            if (removedAmount != ingredient.Amount) // 예상 수량 제거 실패 확인
            {
                playerInventory.RemoveItem(recipeData.ResultItem, recipeData.ResultQuantity); // 제작 결과 회수

                for (int restoreIndex = 0; restoreIndex < removedIngredients.Count; restoreIndex++) // 제거 완료 재료 순회
                {
                    CraftingIngredient removedIngredient = removedIngredients[restoreIndex]; // 복구할 재료 조회
                    playerInventory.AddItem(removedIngredient.ItemData, removedIngredient.Amount); // 제거된 재료 복구
                }

                if (removedAmount > 0) // 현재 재료 일부 제거 여부 확인
                {
                    playerInventory.AddItem(ingredient.ItemData, removedAmount); // 일부 제거된 재료 복구
                }

                Debug.LogError($"{recipeData.DisplayName} 제작 중 재료 수량이 변경되어 제작을 취소했습니다.", this); // 제작 동기화 오류 출력
                return false; // 제작 실패 반환
            }

            removedIngredients.Add(ingredient); // 제거 완료 재료 기록
        }

        return true; // 제작 성공 반환
    }
}
