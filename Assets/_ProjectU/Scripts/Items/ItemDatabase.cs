using System; // 문자열 비교 기능
using System.Collections.Generic; // 아이템 목록과 중복 검사 기능
using UnityEngine; // Unity 기본 기능

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Project U/Item Database")] // 아이템 데이터베이스 생성 메뉴
public sealed class ItemDatabase : ScriptableObject // 전체 아이템 ID 조회 데이터베이스
{
    [SerializeField] private List<ItemData> items = new List<ItemData>(); // 등록 아이템 목록

    public bool TryValidate(out string errorMessage) // 데이터베이스 등록 상태 검사
    {
        HashSet<string> registeredItemIds = new HashSet<string>(StringComparer.Ordinal); // 등록 ID 중복 검사 목록

        for (int index = 0; index < items.Count; index++) // 전체 등록 아이템 순회
        {
            ItemData itemData = items[index]; // 현재 아이템 조회

            if (itemData == null) // 빈 아이템 참조 확인
            {
                errorMessage = $"ItemDatabase의 {index}번 항목이 비어 있습니다."; // 빈 항목 오류 저장
                return false; // 검사 실패
            }

            if (string.IsNullOrWhiteSpace(itemData.ItemId)) // 아이템 ID 존재 확인
            {
                errorMessage = $"{itemData.name}의 Item ID가 비어 있습니다."; // 빈 ID 오류 저장
                return false; // 검사 실패
            }

            if (!registeredItemIds.Add(itemData.ItemId)) // 동일 ID 등록 확인
            {
                errorMessage = $"중복 Item ID가 등록되었습니다: {itemData.ItemId}"; // 중복 ID 오류 저장
                return false; // 검사 실패
            }
        }

        errorMessage = string.Empty; // 오류 내용 초기화
        return true; // 검사 성공
    }

    public bool TryGetItem(string itemId, out ItemData itemData) // ID로 아이템 데이터 조회
    {
        itemData = null; // 조회 결과 초기화

        if (string.IsNullOrWhiteSpace(itemId)) // 요청 ID 존재 확인
        {
            return false; // 조회 실패
        }

        for (int index = 0; index < items.Count; index++) // 전체 등록 아이템 순회
        {
            ItemData candidateItem = items[index]; // 현재 후보 아이템 조회

            if (candidateItem == null) // 빈 참조 확인
            {
                continue; // 다음 아이템 검사
            }

            if (!string.Equals(candidateItem.ItemId, itemId, StringComparison.Ordinal)) // ID 일치 여부 확인
            {
                continue; // 다음 아이템 검사
            }

            itemData = candidateItem; // 일치 아이템 저장
            return true; // 조회 성공
        }

        return false; // 일치 아이템 없음
    }
}