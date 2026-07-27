using System.Collections.Generic; // Dictionary 기능
using System.Text.RegularExpressions; // 정규식 검사 기능
using UnityEditor; // Unity 편집기 기능
using UnityEngine; // Unity 기본 기능

public static class ItemDataValidator // 아이템 데이터 일괄 검증
{
    private static readonly Regex ItemIdPattern = new Regex("^[a-z][a-z0-9]*(?:_[a-z0-9]+)+$", RegexOptions.Compiled); // 소문자 밑줄 ID 형식

    [MenuItem("Project U/Data/Validate Item Data")] // 상단 검증 메뉴 등록
    private static void ValidateAllItemData() // 전체 아이템 데이터 검증
    {
        string[] searchFolders = { "Assets/_ProjectU/Data/Items" }; // 아이템 검색 폴더
        string[] itemGuids = AssetDatabase.FindAssets("t:ItemData", searchFolders); // ItemData 에셋 GUID 검색
        Dictionary<string, string> idToPath = new Dictionary<string, string>(); // ID별 에셋 경로 저장
        int errorCount = 0; // 전체 오류 개수

        for (int index = 0; index < itemGuids.Length; index++) // 검색된 에셋 순회
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(itemGuids[index]); // GUID를 경로로 변환
            ItemData itemData = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath); // ItemData 에셋 불러오기

            if (itemData == null) // 에셋 로드 실패 확인
            {
                Debug.LogError($"[ItemData] 에셋을 불러올 수 없습니다: {assetPath}"); // 로드 실패 출력
                errorCount++; // 오류 개수 증가
                continue; // 다음 에셋 검사
            }

            errorCount += ValidateItemData(itemData, assetPath, idToPath); // 현재 아이템 검사
        }

        if (errorCount == 0) // 오류 없음 확인
        {
            Debug.Log($"[ItemData] 검증 완료: {itemGuids.Length}개 / 오류 0개"); // 검증 성공 출력
            return; // 검증 종료
        }

        Debug.LogError($"[ItemData] 검증 실패: {itemGuids.Length}개 / 오류 {errorCount}개"); // 검증 실패 출력
    }

    private static int ValidateItemData(ItemData itemData, string assetPath, Dictionary<string, string> idToPath) // 단일 아이템 검사
    {
        int errorCount = 0; // 현재 아이템 오류 개수
        string itemId = itemData.ItemId; // 현재 아이템 ID
        string expectedPrefix = GetExpectedIdPrefix(itemData.ItemCategory); // 분류별 접두사 계산

        if (string.IsNullOrWhiteSpace(itemId)) // 빈 ID 확인
        {
            Debug.LogError($"[ItemData] ID가 비어 있습니다: {assetPath}", itemData); // 빈 ID 오류 출력
            errorCount++; // 오류 개수 증가
        }
        else // ID 존재 상태
        {
            if (!ItemIdPattern.IsMatch(itemId)) // ID 형식 확인
            {
                Debug.LogError($"[ItemData] ID 형식이 잘못되었습니다: {itemId} / {assetPath}", itemData); // 형식 오류 출력
                errorCount++; // 오류 개수 증가
            }

            if (!itemId.StartsWith(expectedPrefix)) // 분류 접두사 확인
            {
                Debug.LogError($"[ItemData] {itemData.ItemCategory} ID는 {expectedPrefix}로 시작해야 합니다: {itemId}", itemData); // 접두사 오류 출력
                errorCount++; // 오류 개수 증가
            }

            if (idToPath.TryGetValue(itemId, out string existingPath)) // 동일 ID 존재 확인
            {
                Debug.LogError($"[ItemData] 중복 ID입니다: {itemId} / 기존: {existingPath} / 중복: {assetPath}", itemData); // 중복 오류 출력
                errorCount++; // 오류 개수 증가
            }
            else // 최초 ID 확인
            {
                idToPath.Add(itemId, assetPath); // ID와 경로 등록
            }
        }

        if (string.IsNullOrWhiteSpace(itemData.DisplayName)) // 표시 이름 확인
        {
            Debug.LogError($"[ItemData] 표시 이름이 비어 있습니다: {assetPath}", itemData); // 이름 오류 출력
            errorCount++; // 오류 개수 증가
        }

        if (itemData.IsTool && itemData.ToolType == ToolType.None) // 도구 종류 누락 확인
        {
            Debug.LogError($"[ItemData] 도구에는 Tool Type이 필요합니다: {assetPath}", itemData); // 도구 종류 오류 출력
            errorCount++; // 오류 개수 증가
        }

        if (!itemData.IsTool && itemData.ToolType != ToolType.None) // 일반 아이템의 도구 종류 확인
        {
            Debug.LogError($"[ItemData] 도구가 아닌 아이템은 Tool Type이 None이어야 합니다: {assetPath}", itemData); // 잘못된 도구 종류 출력
            errorCount++; // 오류 개수 증가
        }

        if ((itemData.IsTool || itemData.IsEquipment) && itemData.MaximumStack != 1) // 단일 중첩 분류 확인
        {
            Debug.LogError($"[ItemData] 도구와 장비의 Maximum Stack은 1이어야 합니다: {assetPath}", itemData); // 중첩 오류 출력
            errorCount++; // 오류 개수 증가
        }

        return errorCount; // 현재 아이템 오류 개수 반환
    }

    private static string GetExpectedIdPrefix(ItemCategory itemCategory) // 분류별 접두사 반환
    {
        switch (itemCategory) // 아이템 분류 확인
        {
            case ItemCategory.CraftingMaterial: // 제작 재료 분기
                return "resource_"; // 제작 재료 접두사 반환

            case ItemCategory.Tool: // 도구 분기
                return "tool_"; // 도구 접두사 반환

            case ItemCategory.Food: // 음식 분기
                return "food_"; // 음식 접두사 반환

            case ItemCategory.Equipment: // 장비 분기
                return "equipment_"; // 장비 접두사 반환

            default: // 정의되지 않은 분류
                return string.Empty; // 빈 접두사 반환
        }
    }
}