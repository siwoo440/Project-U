using System; // 이벤트 기능
using System.Collections.Generic; // 재료 목록 기능
using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class CraftingManager : MonoBehaviour // 플레이어 제작 관리자
{
    [Header("References")] // 외부 참조 묶음
    [SerializeField] private PlayerInventory playerInventory; // 플레이어 인벤토리
    [SerializeField] private CraftingUnlockManager craftingUnlockManager; // 제작법 해금 관리자
    [SerializeField] private CraftingFacilityType activeFacility = CraftingFacilityType.Hand; // 현재 제작 시설

    public CraftingUnlockManager UnlockManager => craftingUnlockManager; // 해금 관리자 제공
    public CraftingFacilityType ActiveFacilityType => activeFacility; // 현재 시설 종류 제공
    public string ActiveFacilityId => CraftingFacilityIds.GetFacilityId(activeFacility); // 현재 시설 ID 제공
    public event Action ActiveFacilityChanged; // 현재 제작 시설 변경 알림

    public void SetActiveFacility(CraftingFacilityType facilityType) // 현재 제작 시설 변경
    {
        if (activeFacility == facilityType) // 동일 시설 확인
        {
            return; // 중복 변경 차단
        }

        activeFacility = facilityType; // 새로운 제작 시설 저장
        ActiveFacilityChanged?.Invoke(); // 제작 시설 변경 알림
    }

    public void ResetToHand() // 맨손 제작 시설 복귀
    {
        SetActiveFacility(CraftingFacilityType.Hand); // 맨손 제작 시설 적용
    }

    public bool IsRecipeUnlocked(CraftingRecipeData recipeData) // 제작법 해금 여부 확인
    {
        if (craftingUnlockManager == null) // 해금 관리자 확인
        {
            return false; // 잠금 상태 반환
        }

        return craftingUnlockManager.IsRecipeUnlocked(recipeData); // 실제 해금 상태 반환
    }

    public bool HasRequiredFacility(CraftingRecipeData recipeData) // 제작 시설 충족 여부 확인
    {
        if (recipeData == null) // 제작법 존재 확인
        {
            return false; // 시설 불일치 반환
        }

        return recipeData.RequiredFacility == activeFacility; // 필요 시설과 현재 시설 비교
    }

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
        bool isUnlocked = IsRecipeUnlocked(recipeData); // 제작법 해금 여부
        bool hasFacility = HasRequiredFacility(recipeData); // 제작 시설 충족 여부
        bool hasMaterials = HasRequiredMaterials(recipeData); // 제작 재료 충족 여부
        bool hasOutputSpace = HasOutputSpace(recipeData); // 결과 공간 충족 여부
        return isUnlocked && hasFacility && hasMaterials && hasOutputSpace; // 전체 제작 조건 반환
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
            int addedResult = recipeData.ResultQuantity - remainingResult; // 실제 추가 수량 계산

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
                    CraftingIngredient removedIngredient = removedIngredients[restoreIndex]; // 복구 재료 조회
                    playerInventory.AddItem(removedIngredient.ItemData, removedIngredient.Amount); // 제거 재료 복구
                }

                if (removedAmount > 0) // 현재 재료 일부 제거 확인
                {
                    playerInventory.AddItem(ingredient.ItemData, removedAmount); // 일부 제거 재료 복구
                }

                Debug.LogError($"{recipeData.DisplayName} 제작 중 재료 수량이 변경되어 제작을 취소했습니다.", this); // 제작 오류 출력
                return false; // 제작 실패 반환
            }

            removedIngredients.Add(ingredient); // 제거 완료 재료 기록
        }

        return true; // 제작 성공 반환
    }
}
