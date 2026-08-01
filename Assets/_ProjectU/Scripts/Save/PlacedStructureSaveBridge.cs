using System; // 문자열 비교 기능
using System.Collections.Generic; // ID 사전과 중복 검사 기능
using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 저장 연결 중복 방지
public sealed class PlacedStructureSaveBridge : MonoBehaviour // 설치 건축물 저장 연결
{
    [Header("References")] // 외부 참조 묶음
    [Tooltip("설치 건축물 부모.")]
    [SerializeField] private Transform placedStructureParent; // 설치 건축물 부모
    [Tooltip("등록 건축 데이터 목록.")]
    [SerializeField] private BuildRecipeData[] buildRecipes = new BuildRecipeData[0]; // 등록 건축 데이터 목록

    public bool TryValidateSetup(out string errorMessage) // 건축물 저장 구성 검사
    {
        if (placedStructureParent == null) // 설치 건축물 부모 확인
        {
            errorMessage = "Placed Structure Parent 참조가 누락되었습니다."; // 부모 오류 저장
            return false; // 검사 실패
        }

        if (!TryBuildRecipeMap(
            out Dictionary<string, BuildRecipeData> recipeMap,
            out errorMessage)) // 건축 데이터 사전 생성
        {
            return false; // 검사 실패
        }

        return TryBuildCurrentStructureMap(
            out Dictionary<string, PlacedBuildObject> structureMap,
            out errorMessage); // 현재 건축물 ID 검사
    }

    public bool TryCapture(SaveGameData saveData, out string errorMessage) // 설치 건축물 상태 수집
    {
        if (saveData == null || saveData.world == null) // 저장 데이터 존재 확인
        {
            errorMessage = "설치 건축물 저장 데이터가 누락되었습니다."; // 저장 데이터 오류
            return false; // 수집 실패
        }

        if (!TryBuildRecipeMap(
            out Dictionary<string, BuildRecipeData> recipeMap,
            out errorMessage)) // 등록 건축 데이터 검사
        {
            return false; // 수집 실패
        }

        if (!TryBuildCurrentStructureMap(
            out Dictionary<string, PlacedBuildObject> structureMap,
            out errorMessage)) // 현재 건축물 검사
        {
            return false; // 수집 실패
        }

        saveData.world.placedStructures.Clear(); // 기존 건축물 저장 목록 초기화

        foreach (PlacedBuildObject structure in structureMap.Values) // 전체 설치 건축물 순회
        {
            BuildRecipeData recipeData = structure.RecipeData; // 현재 건축 데이터 조회

            if (recipeData == null) // 건축 데이터 존재 확인
            {
                errorMessage = $"{structure.gameObject.name}의 Recipe Data가 누락되었습니다."; // 건축 데이터 오류
                return false; // 수집 실패
            }

            if (!recipeMap.TryGetValue(
                recipeData.RecipeId,
                out BuildRecipeData registeredRecipe)) // 등록 건축 데이터 확인
            {
                errorMessage = $"등록되지 않은 건축 Recipe ID입니다: {recipeData.RecipeId}"; // 미등록 ID 오류
                return false; // 수집 실패
            }

            PlacedStructureSaveData structureSaveData = new PlacedStructureSaveData(); // 건축물 저장 항목 생성
            structureSaveData.structureId = structure.StructureId; // 건축물 고유 ID 저장
            structureSaveData.recipeId = registeredRecipe.RecipeId; // 건축 Recipe ID 저장
            structureSaveData.position = SaveVector3Data.FromVector3(structure.transform.position); // 설치 위치 저장
            structureSaveData.rotation = SaveQuaternionData.FromQuaternion(structure.transform.rotation); // 설치 회전 저장

            CampfireCookingStation campfire = structure.GetComponentInChildren<CampfireCookingStation>(true); // 모닥불 기능 검색

            if (campfire != null) // 모닥불 건축물 확인
            {
                structureSaveData.hasCampfireState = true; // 모닥불 상태 존재 표시
                structureSaveData.campfire.isCooking = campfire.IsCooking; // 조리 진행 상태 저장
                structureSaveData.campfire.hasReadyResult = campfire.HasReadyResult; // 완성 음식 상태 저장
                structureSaveData.campfire.remainingCookingTime = campfire.RemainingCookingTime; // 남은 조리 시간 저장
            }

            saveData.world.placedStructures.Add(structureSaveData); // 건축물 저장 목록 추가
        }

        saveData.world.hasCapturedPlacedStructureState = true; // 건축물 상태 저장 완료 표시
        errorMessage = string.Empty; // 오류 내용 초기화
        return true; // 수집 성공
    }

    public bool TryRestore(SaveGameData saveData, out string errorMessage) // 설치 건축물 상태 복원
    {
        if (saveData == null || saveData.world == null) // 저장 데이터 존재 확인
        {
            errorMessage = "설치 건축물 저장 데이터가 누락되었습니다."; // 저장 데이터 오류
            return false; // 복원 실패
        }

        if (!saveData.world.hasCapturedPlacedStructureState) // 이전 저장 파일 여부 확인
        {
            errorMessage = string.Empty; // 오류 내용 초기화
            return true; // 기존 건축물 상태 유지
        }

        if (!TryBuildRecipeMap(
            out Dictionary<string, BuildRecipeData> recipeMap,
            out errorMessage)) // 건축 데이터 사전 생성
        {
            return false; // 복원 실패
        }

        if (!TryBuildCurrentStructureMap(
            out Dictionary<string, PlacedBuildObject> currentStructures,
            out errorMessage)) // 현재 건축물 검사
        {
            return false; // 복원 실패
        }

        if (!TryValidateSavedState(
            saveData,
            recipeMap,
            out errorMessage)) // 저장 건축물 데이터 검사
        {
            return false; // 복원 실패
        }

        foreach (PlacedBuildObject currentStructure in currentStructures.Values) // 현재 건축물 순회
        {
            currentStructure.gameObject.SetActive(false); // 기존 건축물 즉시 비활성화
            Destroy(currentStructure.gameObject); // 기존 건축물 제거 예약
        }

        for (int index = 0; index < saveData.world.placedStructures.Count; index++) // 저장 건축물 순회
        {
            PlacedStructureSaveData structureSaveData = saveData.world.placedStructures[index]; // 현재 저장 항목 조회
            BuildRecipeData recipeData = recipeMap[structureSaveData.recipeId]; // 건축 Recipe 조회
            Vector3 spawnPosition = structureSaveData.position.ToVector3(); // 저장 위치 변환
            Quaternion spawnRotation = structureSaveData.rotation.ToQuaternion(); // 저장 회전 변환

            GameObject structureInstance = Instantiate(
                recipeData.PlacedPrefab,
                spawnPosition,
                spawnRotation,
                placedStructureParent); // 저장 건축물 생성

            PlacedBuildObject placedBuildObject = structureInstance.GetComponent<PlacedBuildObject>(); // 건축 정보 검색

            if (placedBuildObject == null) // 건축 정보 누락 확인
            {
                placedBuildObject = structureInstance.AddComponent<PlacedBuildObject>(); // 건축 정보 자동 추가
            }

            placedBuildObject.RestoreFromSave(
                recipeData,
                structureSaveData.structureId,
                spawnPosition,
                spawnRotation); // 저장 건축 정보 적용

            if (structureSaveData.hasCampfireState) // 모닥불 상태 존재 확인
            {
                CampfireCookingStation campfire = structureInstance.GetComponentInChildren<CampfireCookingStation>(true); // 모닥불 기능 검색
                campfire.RestoreFromSave(
                    structureSaveData.campfire.isCooking,
                    structureSaveData.campfire.hasReadyResult,
                    structureSaveData.campfire.remainingCookingTime); // 저장 조리 상태 적용
            }
        }

        errorMessage = string.Empty; // 오류 내용 초기화
        return true; // 복원 성공
    }

    private bool TryBuildRecipeMap(
        out Dictionary<string, BuildRecipeData> recipeMap,
        out string errorMessage) // 건축 Recipe ID 사전 생성
    {
        recipeMap = new Dictionary<string, BuildRecipeData>(StringComparer.Ordinal); // 건축 데이터 사전 생성

        if (buildRecipes == null || buildRecipes.Length == 0) // 건축 데이터 목록 확인
        {
            errorMessage = "Build Recipes가 비어 있습니다."; // 건축 데이터 오류
            return false; // 사전 생성 실패
        }

        for (int index = 0; index < buildRecipes.Length; index++) // 전체 건축 데이터 순회
        {
            BuildRecipeData recipeData = buildRecipes[index]; // 현재 건축 데이터 조회

            if (recipeData == null) // 빈 건축 데이터 확인
            {
                errorMessage = $"Build Recipes의 Element {index}가 비어 있습니다."; // 빈 참조 오류
                return false; // 사전 생성 실패
            }

            if (string.IsNullOrWhiteSpace(recipeData.RecipeId)) // 건축 ID 존재 확인
            {
                errorMessage = $"{recipeData.name}의 Recipe ID가 비어 있습니다."; // 빈 ID 오류
                return false; // 사전 생성 실패
            }

            if (recipeData.PlacedPrefab == null) // 설치 프리팹 존재 확인
            {
                errorMessage = $"{recipeData.RecipeId}의 Placed Prefab이 누락되었습니다."; // 프리팹 오류
                return false; // 사전 생성 실패
            }

            if (!recipeMap.TryAdd(recipeData.RecipeId, recipeData)) // 중복 건축 ID 확인
            {
                errorMessage = $"중복 건축 Recipe ID입니다: {recipeData.RecipeId}"; // 중복 ID 오류
                return false; // 사전 생성 실패
            }
        }

        errorMessage = string.Empty; // 오류 내용 초기화
        return true; // 사전 생성 성공
    }

    private bool TryBuildCurrentStructureMap(
        out Dictionary<string, PlacedBuildObject> structureMap,
        out string errorMessage) // 현재 설치 건축물 ID 사전 생성
    {
        structureMap = new Dictionary<string, PlacedBuildObject>(StringComparer.Ordinal); // 건축물 ID 사전 생성

        PlacedBuildObject[] foundStructures = UnityEngine.Object.FindObjectsByType<PlacedBuildObject>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None); // 비활성화 포함 건축물 검색

        for (int index = 0; index < foundStructures.Length; index++) // 전체 건축물 순회
        {
            PlacedBuildObject structure = foundStructures[index]; // 현재 건축물 조회
            string structureId = structure.StructureId; // 현재 건축물 ID 조회

            if (string.IsNullOrWhiteSpace(structureId)) // 건축물 ID 존재 확인
            {
                errorMessage = $"{structure.gameObject.name}의 Structure ID가 비어 있습니다."; // 빈 ID 오류
                return false; // 사전 생성 실패
            }

            if (!structureMap.TryAdd(structureId, structure)) // 건축물 ID 중복 확인
            {
                errorMessage = $"중복 Structure ID가 있습니다: {structureId}"; // 중복 ID 오류
                return false; // 사전 생성 실패
            }
        }

        errorMessage = string.Empty; // 오류 내용 초기화
        return true; // 사전 생성 성공
    }

    private bool TryValidateSavedState(
        SaveGameData saveData,
        Dictionary<string, BuildRecipeData> recipeMap,
        out string errorMessage) // 저장 건축물 데이터 검사
    {
        if (saveData.world.placedStructures == null) // 건축물 저장 목록 존재 확인
        {
            errorMessage = "설치 건축물 저장 목록이 누락되었습니다."; // 목록 오류
            return false; // 검사 실패
        }

        HashSet<string> usedStructureIds = new HashSet<string>(StringComparer.Ordinal); // 저장 ID 중복 검사 목록

        for (int index = 0; index < saveData.world.placedStructures.Count; index++) // 저장 건축물 순회
        {
            PlacedStructureSaveData structureSaveData = saveData.world.placedStructures[index]; // 현재 저장 항목 조회

            if (structureSaveData == null) // 저장 항목 존재 확인
            {
                errorMessage = "비어 있는 설치 건축물 저장 항목이 있습니다."; // 빈 항목 오류
                return false; // 검사 실패
            }

            if (string.IsNullOrWhiteSpace(structureSaveData.structureId)) // 건축물 ID 존재 확인
            {
                errorMessage = "설치 건축물의 Structure ID가 비어 있습니다."; // 빈 ID 오류
                return false; // 검사 실패
            }

            if (!usedStructureIds.Add(structureSaveData.structureId)) // 저장 ID 중복 확인
            {
                errorMessage = $"중복 저장 Structure ID입니다: {structureSaveData.structureId}"; // 중복 ID 오류
                return false; // 검사 실패
            }

            if (!recipeMap.TryGetValue(
                structureSaveData.recipeId,
                out BuildRecipeData recipeData)) // 등록 Recipe ID 확인
            {
                errorMessage = $"등록되지 않은 건축 Recipe ID입니다: {structureSaveData.recipeId}"; // 미등록 ID 오류
                return false; // 검사 실패
            }

            if (structureSaveData.position == null || structureSaveData.rotation == null) // 위치와 회전 확인
            {
                errorMessage = $"건축물 위치 또는 회전이 누락되었습니다: {structureSaveData.structureId}"; // Transform 오류
                return false; // 검사 실패
            }

            CampfireCookingStation campfirePrefab = recipeData.PlacedPrefab.GetComponentInChildren<CampfireCookingStation>(true); // 모닥불 프리팹 확인
            bool recipeHasCampfire = campfirePrefab != null; // 모닥불 기능 존재 여부

            if (recipeHasCampfire != structureSaveData.hasCampfireState) // Recipe와 저장 상태 일치 확인
            {
                errorMessage = $"모닥불 저장 상태가 Recipe와 일치하지 않습니다: {structureSaveData.structureId}"; // 모닥불 구성 오류
                return false; // 검사 실패
            }

            if (!structureSaveData.hasCampfireState) // 일반 건축물 확인
            {
                continue; // 모닥불 검사 생략
            }

            CampfireSaveData campfireSaveData = structureSaveData.campfire; // 모닥불 저장 데이터 조회

            if (campfireSaveData == null) // 모닥불 데이터 존재 확인
            {
                errorMessage = $"모닥불 저장 데이터가 누락되었습니다: {structureSaveData.structureId}"; // 모닥불 데이터 오류
                return false; // 검사 실패
            }

            if (campfireSaveData.isCooking && campfireSaveData.hasReadyResult) // 동시 상태 확인
            {
                errorMessage = $"모닥불이 조리 중이면서 완료 상태입니다: {structureSaveData.structureId}"; // 상태 충돌 오류
                return false; // 검사 실패
            }

            bool hasInvalidTime = campfireSaveData.remainingCookingTime < 0f
                || float.IsNaN(campfireSaveData.remainingCookingTime)
                || float.IsInfinity(campfireSaveData.remainingCookingTime); // 조리 시간 유효성 계산

            if (hasInvalidTime) // 잘못된 조리 시간 확인
            {
                errorMessage = $"모닥불 조리 시간이 잘못되었습니다: {structureSaveData.structureId}"; // 시간 오류
                return false; // 검사 실패
            }

            if (campfireSaveData.isCooking && campfireSaveData.remainingCookingTime <= 0f) // 진행 상태 시간 확인
            {
                errorMessage = $"조리 중인 모닥불의 남은 시간이 없습니다: {structureSaveData.structureId}"; // 진행 시간 오류
                return false; // 검사 실패
            }

            if (!campfireSaveData.isCooking && campfireSaveData.remainingCookingTime > 0f) // 유휴 상태 시간 확인
            {
                errorMessage = $"조리하지 않는 모닥불에 남은 시간이 있습니다: {structureSaveData.structureId}"; // 유휴 시간 오류
                return false; // 검사 실패
            }
        }

        errorMessage = string.Empty; // 오류 내용 초기화
        return true; // 검사 성공
    }
}