using System; // 이벤트와 문자열 비교 기능
using System.Collections.Generic; // 해금 목록과 중복 검사 기능
using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class CraftingUnlockManager : MonoBehaviour // 제작법 해금 관리자
{
    [Header("Recipes")] // 제작법 설정 묶음
    [Tooltip("전체 제작법 목록.")]
    [SerializeField] private CraftingRecipeData[] allRecipes = new CraftingRecipeData[0]; // 전체 제작법 목록

    [Header("Debug")] // 테스트 설정 묶음
    [Tooltip("테스트 해금 제작법.")]
    [SerializeField] private CraftingRecipeData debugRecipe; // 테스트 해금 제작법

    private readonly HashSet<string> unlockedRecipeIds = new HashSet<string>(StringComparer.Ordinal); // 해금된 제작법 ID 목록

    public event Action UnlockStateChanged; // 제작법 해금 상태 변경 이벤트

    private void Awake() // 제작법 해금 상태 초기화
    {
        if (!TryValidateSetup(out string errorMessage)) // 제작법 설정 검사
        {
            Debug.LogError($"제작법 해금 시스템 초기화 실패\n{errorMessage}", this); // 초기화 오류 출력
            enabled = false; // 해금 기능 비활성화
            return; // 초기화 중단
        }

        ResetToDefaultRecipes(); // 기본 제작법 해금
    }

    public bool TryValidateSetup(out string errorMessage) // 제작법 목록 설정 검사
    {
        if (allRecipes == null) // 제작법 배열 존재 확인
        {
            errorMessage = "전체 제작법 목록이 누락되었습니다."; // 배열 오류 저장
            return false; // 검사 실패
        }

        HashSet<string> registeredRecipeIds = new HashSet<string>(StringComparer.Ordinal); // 제작법 ID 중복 검사 목록

        for (int index = 0; index < allRecipes.Length; index++) // 전체 제작법 순회
        {
            CraftingRecipeData recipeData = allRecipes[index]; // 현재 제작법 조회

            if (recipeData == null) // 제작법 참조 확인
            {
                errorMessage = $"{index}번 제작법 참조가 비어 있습니다."; // 빈 참조 오류 저장
                return false; // 검사 실패
            }

            if (string.IsNullOrWhiteSpace(recipeData.RecipeId)) // 제작법 ID 확인
            {
                errorMessage = $"{recipeData.name}의 Recipe ID가 비어 있습니다."; // 빈 ID 오류 저장
                return false; // 검사 실패
            }

            if (!registeredRecipeIds.Add(recipeData.RecipeId)) // 제작법 ID 중복 확인
            {
                errorMessage = $"중복 Recipe ID가 있습니다: {recipeData.RecipeId}"; // 중복 오류 저장
                return false; // 검사 실패
            }

            if (!Enum.IsDefined(typeof(CraftingFacilityType), recipeData.RequiredFacility)) // 제작 시설 값 확인
            {
                errorMessage = $"잘못된 제작 시설 값입니다: {recipeData.RecipeId}"; // 시설 오류 저장
                return false; // 검사 실패
            }

            bool requiresUnlockId = recipeData.UnlockType != CraftingUnlockType.Default; // 별도 해금 ID 필요 여부

            if (requiresUnlockId && string.IsNullOrWhiteSpace(recipeData.UnlockId)) // 해금 ID 누락 확인
            {
                errorMessage = $"해금 ID가 비어 있습니다: {recipeData.RecipeId}"; // 해금 ID 오류 저장
                return false; // 검사 실패
            }
        }

        errorMessage = string.Empty; // 오류 내용 초기화
        return true; // 검사 성공
    }

    public bool IsRecipeUnlocked(CraftingRecipeData recipeData) // 제작법 해금 여부 확인
    {
        if (recipeData == null) // 제작법 존재 확인
        {
            return false; // 잠금 상태 반환
        }

        if (recipeData.IsDefaultUnlocked) // 기본 해금 제작법 확인
        {
            return true; // 해금 상태 반환
        }

        return unlockedRecipeIds.Contains(recipeData.RecipeId); // 저장된 해금 상태 반환
    }

    public bool TryUnlockRecipe(string recipeId) // 제작법 ID 해금 시도
    {
        if (!TryGetRecipe(recipeId, out CraftingRecipeData recipeData)) // 제작법 ID 조회
        {
            Debug.LogError($"등록되지 않은 제작법은 해금할 수 없습니다: {recipeId}", this); // 조회 오류 출력
            return false; // 해금 실패
        }

        bool wasAdded = unlockedRecipeIds.Add(recipeData.RecipeId); // 해금 목록 추가

        if (wasAdded) // 새로운 해금 여부 확인
        {
            UnlockStateChanged?.Invoke(); // 해금 상태 변경 알림
        }

        return true; // 해금 성공
    }

    public void ResetToDefaultRecipes() // 기본 제작법 상태 초기화
    {
        unlockedRecipeIds.Clear(); // 기존 해금 목록 제거

        for (int index = 0; index < allRecipes.Length; index++) // 전체 제작법 순회
        {
            CraftingRecipeData recipeData = allRecipes[index]; // 현재 제작법 조회

            if (!recipeData.IsDefaultUnlocked) // 기본 해금 여부 확인
            {
                continue; // 잠금 제작법 제외
            }

            unlockedRecipeIds.Add(recipeData.RecipeId); // 기본 제작법 해금
        }

        UnlockStateChanged?.Invoke(); // 해금 상태 변경 알림
    }

    public List<string> CreateUnlockedRecipeIdList() // 저장용 해금 ID 목록 생성
    {
        List<string> result = new List<string>(unlockedRecipeIds); // 현재 해금 목록 복사
        result.Sort(StringComparer.Ordinal); // 저장 순서 정렬
        return result; // 복사 목록 반환
    }

    public bool TryRestoreUnlockedRecipes(IReadOnlyList<string> savedRecipeIds, out string errorMessage) // 저장된 해금 목록 복원
    {
        if (savedRecipeIds == null) // 저장 목록 존재 확인
        {
            errorMessage = "해금된 제작법 저장 목록이 누락되었습니다."; // 목록 오류 저장
            return false; // 복원 실패
        }

        HashSet<string> restoredRecipeIds = new HashSet<string>(StringComparer.Ordinal); // 복원 결과 임시 목록

        for (int index = 0; index < allRecipes.Length; index++) // 전체 제작법 순회
        {
            CraftingRecipeData recipeData = allRecipes[index]; // 현재 제작법 조회

            if (recipeData.IsDefaultUnlocked) // 기본 해금 제작법 확인
            {
                restoredRecipeIds.Add(recipeData.RecipeId); // 기본 제작법 추가
            }
        }

        for (int index = 0; index < savedRecipeIds.Count; index++) // 저장된 해금 목록 순회
        {
            string recipeId = savedRecipeIds[index]; // 현재 저장 ID 조회

            if (string.IsNullOrWhiteSpace(recipeId)) // 빈 저장 ID 확인
            {
                errorMessage = "비어 있는 제작법 ID가 저장되어 있습니다."; // 빈 ID 오류 저장
                return false; // 복원 실패
            }

            if (!TryGetRecipe(recipeId, out CraftingRecipeData recipeData)) // 저장 ID 조회
            {
                Debug.LogError($"등록되지 않은 제작법 ID를 복원 목록에서 제외했습니다: {recipeId}", this); // 알 수 없는 ID 출력
                continue; // 알 수 없는 ID 제외
            }

            restoredRecipeIds.Add(recipeData.RecipeId); // 정상 제작법 해금
        }

        unlockedRecipeIds.Clear(); // 기존 해금 상태 제거

        foreach (string recipeId in restoredRecipeIds) // 복원된 ID 순회
        {
            unlockedRecipeIds.Add(recipeId); // 최종 해금 상태 적용
        }

        UnlockStateChanged?.Invoke(); // 해금 상태 변경 알림
        errorMessage = string.Empty; // 오류 내용 초기화
        return true; // 복원 성공
    }

    public bool TryGetRecipe(string recipeId, out CraftingRecipeData recipeData) // ID로 제작법 조회
    {
        recipeData = null; // 조회 결과 초기화

        if (string.IsNullOrWhiteSpace(recipeId)) // 요청 ID 확인
        {
            return false; // 조회 실패
        }

        for (int index = 0; index < allRecipes.Length; index++) // 전체 제작법 순회
        {
            CraftingRecipeData candidateRecipe = allRecipes[index]; // 현재 후보 제작법 조회

            if (!string.Equals(candidateRecipe.RecipeId, recipeId, StringComparison.Ordinal)) // 제작법 ID 비교
            {
                continue; // 다음 제작법 검사
            }

            recipeData = candidateRecipe; // 일치 제작법 저장
            return true; // 조회 성공
        }

        return false; // 일치 제작법 없음
    }

    [ContextMenu("Debug Unlock Recipe")] // Inspector 테스트 해금 메뉴
    private void DebugUnlockRecipe() // 지정 제작법 테스트 해금
    {
        if (debugRecipe == null) // 테스트 제작법 확인
        {
            Debug.LogError("Debug Recipe 참조가 누락되었습니다.", this); // 참조 오류 출력
            return; // 테스트 중단
        }

        TryUnlockRecipe(debugRecipe.RecipeId); // 지정 제작법 해금
    }

    [ContextMenu("Debug Reset Recipe Unlocks")] // Inspector 테스트 초기화 메뉴
    private void DebugResetRecipeUnlocks() // 해금 상태 테스트 초기화
    {
        ResetToDefaultRecipes(); // 기본 제작법 상태 복원
    }
}
