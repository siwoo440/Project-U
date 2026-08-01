using System.Collections.Generic; // 제작법 ID 목록 기능
using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class CraftingUnlockSaveBridge : MonoBehaviour // 제작법 해금 저장 연결
{
    [Header("References")] // 외부 참조 묶음
    [Tooltip("제작법 해금 관리자.")]
    [SerializeField] private CraftingUnlockManager craftingUnlockManager; // 제작법 해금 관리자

    public bool TryValidateSetup(out string errorMessage) // 저장 연결 설정 검사
    {
        if (craftingUnlockManager == null) // 해금 관리자 참조 확인
        {
            errorMessage = "CraftingUnlockManager 참조가 누락되었습니다."; // 참조 오류 저장
            return false; // 검사 실패
        }

        return craftingUnlockManager.TryValidateSetup(out errorMessage); // 제작법 목록 검사
    }

    public void Capture(SaveGameData saveData) // 현재 제작법 해금 상태 수집
    {
        saveData.hasCraftingData = true; // 제작 해금 데이터 존재 표시

        if (saveData.crafting == null) // 제작 저장 묶음 확인
        {
            saveData.crafting = new CraftingSaveData(); // 제작 저장 묶음 생성
        }

        List<string> unlockedRecipeIds = craftingUnlockManager.CreateUnlockedRecipeIdList(); // 현재 해금 목록 생성
        saveData.crafting.unlockedRecipeIds.Clear(); // 기존 저장 목록 초기화
        saveData.crafting.unlockedRecipeIds.AddRange(unlockedRecipeIds); // 현재 해금 목록 저장
    }

    public bool TryRestore(SaveGameData saveData, out string errorMessage) // 저장된 제작법 해금 상태 복원
    {
        bool hasSavedCraftingData = saveData != null // 전체 저장 데이터 확인
            && saveData.hasCraftingData // 제작 데이터 존재 표시 확인
            && saveData.crafting != null // 제작 저장 묶음 확인
            && saveData.crafting.unlockedRecipeIds != null; // 제작법 ID 목록 확인

        if (!hasSavedCraftingData) // 이전 저장 파일 확인
        {
            craftingUnlockManager.ResetToDefaultRecipes(); // 기본 제작법 상태 적용
            errorMessage = string.Empty; // 오류 내용 초기화
            return true; // 이전 저장 파일 복원 성공
        }

        return craftingUnlockManager.TryRestoreUnlockedRecipes(saveData.crafting.unlockedRecipeIds, out errorMessage); // 해금 목록 복원
    }
}
