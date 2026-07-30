using System; // 문자열 비교 기능
using System.Collections.Generic; // 사전과 중복 검사 기능
using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 저장 연결 중복 방지
public sealed class StorageSaveBridge : MonoBehaviour // 보관함 저장과 복원 연결
{
    [Header("References")] // 외부 참조 묶음
    [SerializeField] private ItemDatabase itemDatabase; // 아이템 ID 데이터베이스

    public bool TryValidateSetup(out string errorMessage) // 보관함 저장 구성 검사
    {
        if (itemDatabase == null) // 아이템 데이터베이스 참조 확인
        {
            errorMessage = "ItemDatabase 참조가 누락되었습니다."; // 참조 오류 저장
            return false; // 검사 실패
        }

        if (!itemDatabase.TryValidate(out errorMessage)) // 아이템 데이터베이스 검사
        {
            return false; // 검사 실패
        }

        return TryBuildCurrentStorageMap( // 현재 보관함 ID 검사
            out Dictionary<string, StorageContainer> storageMap, // 검사 결과 사전 수신
            out errorMessage); // 검사 오류 수신
    }

    public bool TryCapture(SaveGameData saveData, out string errorMessage) // 현재 보관함 내용 수집
    {
        if (saveData == null || saveData.storage == null) // 저장 데이터 묶음 확인
        {
            errorMessage = "보관함 저장 데이터가 누락되었습니다."; // 저장 묶음 오류
            return false; // 수집 실패
        }

        if (!TryBuildCurrentStorageMap( // 현재 보관함 목록 생성
            out Dictionary<string, StorageContainer> storageMap, // 현재 보관함 사전 수신
            out errorMessage)) // 현재 보관함 오류 수신
        {
            return false; // 수집 실패
        }

        saveData.storage.containers.Clear(); // 기존 보관함 저장 목록 초기화

        foreach (StorageContainer storageContainer in storageMap.Values) // 전체 현재 보관함 순회
        {
            StorageContainerSaveData containerSaveData = new StorageContainerSaveData(); // 보관함 저장 항목 생성
            containerSaveData.structureId = storageContainer.StructureId; // 보관함 고유 ID 저장
            containerSaveData.storageTypeId = storageContainer.StorageTypeId; // 보관함 종류 ID 저장

            for (int slotIndex = 0; slotIndex < storageContainer.SlotCapacity; slotIndex++) // 전체 보관함 슬롯 순회
            {
                InventorySlot inventorySlot = storageContainer.GetSlot(slotIndex); // 현재 보관함 슬롯 조회

                if (inventorySlot == null || inventorySlot.ItemData == null || inventorySlot.Quantity <= 0) // 빈 슬롯 확인
                {
                    continue; // 빈 슬롯 저장 제외
                }

                StorageSlotSaveData slotSaveData = new StorageSlotSaveData(); // 보관함 슬롯 저장 항목 생성
                slotSaveData.slotIndex = slotIndex; // 실제 슬롯 번호 저장
                slotSaveData.itemId = inventorySlot.ItemData.ItemId; // 아이템 고유 ID 저장
                slotSaveData.quantity = inventorySlot.Quantity; // 아이템 수량 저장
                containerSaveData.slots.Add(slotSaveData); // 보관함 슬롯 목록 추가
            }

            saveData.storage.containers.Add(containerSaveData); // 전체 보관함 저장 목록 추가
        }

        saveData.hasStorageData = true; // 보관함 데이터 존재 표시
        errorMessage = string.Empty; // 오류 내용 초기화
        return true; // 수집 성공
    }

    public bool TryRestore(SaveGameData saveData, out string errorMessage) // 저장된 보관함 내용 복원
    {
        if (saveData == null) // 전체 저장 데이터 확인
        {
            errorMessage = "저장 데이터가 누락되었습니다."; // 저장 데이터 오류
            return false; // 복원 실패
        }

        if (!saveData.hasStorageData) // 이전 저장 파일 확인
        {
            if (!TryBuildCurrentStorageMap( // 현재 보관함 목록 생성
                out Dictionary<string, StorageContainer> legacyStorageMap, // 이전 저장용 보관함 목록
                out errorMessage)) // 보관함 구성 오류 수신
            {
                return false; // 복원 실패
            }

            foreach (StorageContainer storageContainer in legacyStorageMap.Values) // 전체 현재 보관함 순회
            {
                storageContainer.ClearItemsForLoad(); // 기존 보관함 아이템 초기화
                storageContainer.NotifyContentsChanged(); // 보관함 화면 갱신
            }

            errorMessage = string.Empty; // 오류 내용 초기화
            return true; // 이전 저장 파일 복원 성공
        }

        if (saveData.storage == null || saveData.storage.containers == null) // 보관함 저장 묶음 확인
        {
            errorMessage = "보관함 저장 데이터가 누락되었습니다."; // 보관함 데이터 오류
            return false; // 복원 실패
        }

        if (!itemDatabase.TryValidate(out errorMessage)) // 현재 아이템 데이터베이스 검사
        {
            return false; // 복원 실패
        }

        if (!TryBuildCurrentStorageMap( // 현재 보관함 사전 생성
            out Dictionary<string, StorageContainer> currentStorageMap, // 현재 보관함 사전 수신
            out errorMessage)) // 현재 보관함 오류 수신
        {
            return false; // 복원 실패
        }

        if (!TryValidateSavedState( // 저장된 보관함 전체 내용 검사
            saveData, // 전체 저장 데이터 전달
            currentStorageMap, // 현재 보관함 사전 전달
            out errorMessage)) // 저장 데이터 오류 수신
        {
            return false; // 복원 실패
        }

        foreach (StorageContainer storageContainer in currentStorageMap.Values) // 전체 현재 보관함 순회
        {
            storageContainer.ClearItemsForLoad(); // 기존 보관함 아이템 초기화
        }

        for (int containerIndex = 0; containerIndex < saveData.storage.containers.Count; containerIndex++) // 저장 보관함 목록 순회
        {
            StorageContainerSaveData containerSaveData = saveData.storage.containers[containerIndex]; // 현재 보관함 저장 항목 조회
            StorageContainer storageContainer = currentStorageMap[containerSaveData.structureId]; // 일치하는 현재 보관함 조회

            for (int slotIndex = 0; slotIndex < containerSaveData.slots.Count; slotIndex++) // 저장 슬롯 목록 순회
            {
                StorageSlotSaveData slotSaveData = containerSaveData.slots[slotIndex]; // 현재 저장 슬롯 조회
                itemDatabase.TryGetItem(slotSaveData.itemId, out ItemData itemData); // 아이템 ID로 데이터 조회

                if (!storageContainer.TrySetSlotForLoad( // 저장 슬롯 복원 시도
                    slotSaveData.slotIndex, // 저장 슬롯 번호 전달
                    itemData, // 저장 아이템 데이터 전달
                    slotSaveData.quantity)) // 저장 아이템 수량 전달
                {
                    errorMessage = $"보관함 슬롯 복원에 실패했습니다: {containerSaveData.structureId} / {slotSaveData.slotIndex}"; // 복원 오류 저장
                    return false; // 복원 실패
                }
            }
        }

        foreach (StorageContainer storageContainer in currentStorageMap.Values) // 전체 현재 보관함 순회
        {
            storageContainer.NotifyContentsChanged(); // 복원 완료 변경 알림
        }

        errorMessage = string.Empty; // 오류 내용 초기화
        return true; // 전체 복원 성공
    }

    private bool TryBuildCurrentStorageMap( // 현재 보관함 ID 사전 생성
        out Dictionary<string, StorageContainer> storageMap, // 생성된 보관함 사전
        out string errorMessage) // 검사 오류 내용
    {
        storageMap = new Dictionary<string, StorageContainer>(StringComparer.Ordinal); // 대소문자 구분 ID 사전 생성
        StorageContainer[] storageContainers = FindObjectsByType<StorageContainer>( // 활성 보관함 컴포넌트 조회
            FindObjectsInactive.Exclude, // 비활성 오브젝트 제외
            FindObjectsSortMode.None); // 정렬 비용 제외

        for (int index = 0; index < storageContainers.Length; index++) // 전체 보관함 순회
        {
            StorageContainer storageContainer = storageContainers[index]; // 현재 보관함 조회

            if (storageContainer == null || !storageContainer.gameObject.activeInHierarchy) // 활성 보관함 여부 확인
            {
                continue; // 비활성 보관함 제외
            }

            if (!storageContainer.TryValidateSetup(out string setupError)) // 현재 보관함 설정 검사
            {
                errorMessage = $"{storageContainer.gameObject.name}: {setupError}"; // 설정 오류 저장
                return false; // 사전 생성 실패
            }

            string structureId = storageContainer.StructureId; // 현재 보관함 고유 ID 조회

            if (string.IsNullOrWhiteSpace(structureId)) // 보관함 고유 ID 확인
            {
                errorMessage = $"{storageContainer.gameObject.name}의 Structure ID가 비어 있습니다."; // 빈 ID 오류 저장
                return false; // 사전 생성 실패
            }

            if (!storageMap.TryAdd(structureId, storageContainer)) // 동일 고유 ID 등록 확인
            {
                errorMessage = $"중복 보관함 Structure ID가 있습니다: {structureId}"; // 중복 ID 오류 저장
                return false; // 사전 생성 실패
            }
        }

        errorMessage = string.Empty; // 오류 내용 초기화
        return true; // 사전 생성 성공
    }

    private bool TryValidateSavedState( // 저장된 보관함 전체 내용 검사
        SaveGameData saveData, // 전체 저장 데이터
        Dictionary<string, StorageContainer> currentStorageMap, // 현재 보관함 사전
        out string errorMessage) // 검사 오류 내용
    {
        HashSet<string> usedStructureIds = new HashSet<string>(StringComparer.Ordinal); // 저장 고유 ID 중복 검사 목록

        for (int containerIndex = 0; containerIndex < saveData.storage.containers.Count; containerIndex++) // 저장 보관함 목록 순회
        {
            StorageContainerSaveData containerSaveData = saveData.storage.containers[containerIndex]; // 현재 보관함 저장 항목 조회

            if (containerSaveData == null) // 빈 보관함 저장 항목 확인
            {
                errorMessage = "비어 있는 보관함 저장 항목이 있습니다."; // 빈 항목 오류 저장
                return false; // 검사 실패
            }

            if (string.IsNullOrWhiteSpace(containerSaveData.structureId)) // 저장 고유 ID 확인
            {
                errorMessage = "Structure ID가 비어 있는 보관함 저장 항목이 있습니다."; // 빈 ID 오류 저장
                return false; // 검사 실패
            }

            if (!usedStructureIds.Add(containerSaveData.structureId)) // 저장 고유 ID 중복 확인
            {
                errorMessage = $"중복 보관함 Structure ID가 저장되어 있습니다: {containerSaveData.structureId}"; // 중복 ID 오류 저장
                return false; // 검사 실패
            }

            if (!currentStorageMap.TryGetValue( // 현재 보관함 존재 확인
                containerSaveData.structureId, // 저장 고유 ID 전달
                out StorageContainer storageContainer)) // 일치 보관함 수신
            {
                errorMessage = $"저장 데이터와 일치하는 보관함이 없습니다: {containerSaveData.structureId}"; // 누락 보관함 오류 저장
                return false; // 검사 실패
            }

            if (!string.Equals( // 보관함 종류 일치 확인
                containerSaveData.storageTypeId, // 저장된 종류 ID
                storageContainer.StorageTypeId, // 현재 종류 ID
                StringComparison.Ordinal)) // 대소문자 구분 비교
            {
                errorMessage = $"보관함 종류가 일치하지 않습니다: {containerSaveData.structureId}"; // 종류 불일치 오류 저장
                return false; // 검사 실패
            }

            if (containerSaveData.slots == null) // 저장 슬롯 목록 존재 확인
            {
                errorMessage = $"보관함 슬롯 목록이 누락되었습니다: {containerSaveData.structureId}"; // 슬롯 목록 오류 저장
                return false; // 검사 실패
            }

            HashSet<int> usedSlotIndices = new HashSet<int>(); // 현재 보관함 슬롯 중복 검사 목록

            for (int slotIndex = 0; slotIndex < containerSaveData.slots.Count; slotIndex++) // 저장 슬롯 목록 순회
            {
                StorageSlotSaveData slotSaveData = containerSaveData.slots[slotIndex]; // 현재 저장 슬롯 조회

                if (slotSaveData == null) // 빈 슬롯 저장 항목 확인
                {
                    errorMessage = $"비어 있는 보관함 슬롯 저장 항목이 있습니다: {containerSaveData.structureId}"; // 빈 슬롯 오류 저장
                    return false; // 검사 실패
                }

                if (slotSaveData.slotIndex < 0 || slotSaveData.slotIndex >= storageContainer.SlotCapacity) // 슬롯 번호 범위 확인
                {
                    errorMessage = $"보관함 슬롯 번호가 용량을 벗어났습니다: {containerSaveData.structureId} / {slotSaveData.slotIndex}"; // 슬롯 범위 오류 저장
                    return false; // 검사 실패
                }

                if (!usedSlotIndices.Add(slotSaveData.slotIndex)) // 동일 슬롯 번호 중복 확인
                {
                    errorMessage = $"중복 보관함 슬롯 번호가 있습니다: {containerSaveData.structureId} / {slotSaveData.slotIndex}"; // 중복 슬롯 오류 저장
                    return false; // 검사 실패
                }

                if (!itemDatabase.TryGetItem(slotSaveData.itemId, out ItemData itemData)) // 저장 아이템 ID 조회
                {
                    errorMessage = $"등록되지 않은 보관함 Item ID입니다: {slotSaveData.itemId}"; // 아이템 ID 오류 저장
                    return false; // 검사 실패
                }

                if (slotSaveData.quantity <= 0 || slotSaveData.quantity > itemData.MaximumStack) // 저장 수량 범위 확인
                {
                    errorMessage = $"보관함 아이템 수량이 잘못되었습니다: {slotSaveData.itemId} x{slotSaveData.quantity}"; // 수량 오류 저장
                    return false; // 검사 실패
                }
            }
        }

        errorMessage = string.Empty; // 오류 내용 초기화
        return true; // 전체 저장 데이터 검사 성공
    }
}
