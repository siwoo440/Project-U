using System; // 문자열 비교 기능
using System.Collections.Generic; // ID 사전과 중복 검사 기능
using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 저장 연결 중복 방지
public sealed class WorldSaveBridge : MonoBehaviour // 월드 아이템과 채집 자원 저장 연결
{
    [Header("References")] // 외부 참조 묶음
    [SerializeField] private ItemDatabase itemDatabase; // 아이템 ID 데이터베이스
    [SerializeField] private WorldItemPickup worldItemPrefab; // 동적 월드 아이템 원본
    [SerializeField] private Transform worldItemParent; // 복원 월드 아이템 부모

    public bool TryValidateSetup(out string errorMessage) // 월드 저장 구성 검사
    {
        if (itemDatabase == null) // 아이템 데이터베이스 확인
        {
            errorMessage = "ItemDatabase 참조가 누락되었습니다."; // 데이터베이스 오류 저장
            return false; // 검사 실패
        }

        if (worldItemPrefab == null) // 월드 아이템 원본 확인
        {
            errorMessage = "World Item Prefab 참조가 누락되었습니다."; // 프리팹 오류 저장
            return false; // 검사 실패
        }

        if (worldItemParent == null) // 월드 아이템 부모 확인
        {
            errorMessage = "World Item Parent 참조가 누락되었습니다."; // 부모 오류 저장
            return false; // 검사 실패
        }

        if (!itemDatabase.TryValidate(out errorMessage)) // 아이템 데이터베이스 검사
        {
            return false; // 검사 실패
        }

        return TryBuildCurrentObjectMaps(
            out Dictionary<string, WorldItemPickup> worldItems,
            out Dictionary<string, GatherableResource> resources,
            out errorMessage); // 현재 월드 ID 검사
    }

    public bool TryCapture(SaveGameData saveData, out string errorMessage) // 현재 월드 상태 수집
    {
        if (saveData == null || saveData.world == null) // 월드 저장 데이터 확인
        {
            errorMessage = "월드 저장 데이터가 누락되었습니다."; // 저장 데이터 오류
            return false; // 수집 실패
        }

        if (!TryBuildCurrentObjectMaps(
            out Dictionary<string, WorldItemPickup> worldItems,
            out Dictionary<string, GatherableResource> resources,
            out errorMessage)) // 현재 월드 오브젝트 검사
        {
            return false; // 수집 실패
        }

        saveData.world.worldItems.Clear(); // 기존 월드 아이템 목록 초기화
        saveData.world.gatherableResources.Clear(); // 기존 채집 자원 목록 초기화

        foreach (WorldItemPickup worldItem in worldItems.Values) // 전체 월드 아이템 순회
        {
            if (!worldItem.IsAvailable) // 비활성화 아이템 확인
            {
                continue; // 획득 완료 아이템 저장 제외
            }

            if (worldItem.ItemData == null) // 아이템 데이터 존재 확인
            {
                errorMessage = $"{worldItem.gameObject.name}의 Item Data가 누락되었습니다."; // 아이템 오류 저장
                return false; // 수집 실패
            }

            if (!itemDatabase.TryGetItem(worldItem.ItemData.ItemId, out ItemData registeredItem)) // 등록 아이템 확인
            {
                errorMessage = $"ItemDatabase에 등록되지 않은 월드 아이템입니다: {worldItem.ItemData.ItemId}"; // 등록 오류 저장
                return false; // 수집 실패
            }

            WorldItemSaveData itemSaveData = new WorldItemSaveData(); // 월드 아이템 저장 데이터 생성
            itemSaveData.worldObjectId = worldItem.WorldObjectId; // 월드 고유 ID 저장
            itemSaveData.itemId = registeredItem.ItemId; // 아이템 ID 저장
            itemSaveData.quantity = worldItem.Quantity; // 현재 수량 저장
            itemSaveData.position = SaveVector3Data.FromVector3(worldItem.transform.position); // 현재 위치 저장
            itemSaveData.rotation = SaveQuaternionData.FromQuaternion(worldItem.transform.rotation); // 현재 회전 저장
            saveData.world.worldItems.Add(itemSaveData); // 월드 아이템 목록 추가
        }

        foreach (GatherableResource resource in resources.Values) // 전체 채집 자원 순회
        {
            GatherableResourceSaveData resourceSaveData = new GatherableResourceSaveData(); // 채집 자원 저장 데이터 생성
            resourceSaveData.worldObjectId = resource.WorldObjectId; // 자원 고유 ID 저장
            resourceSaveData.remainingQuantity = resource.RemainingQuantity; // 남은 수량 저장
            resourceSaveData.isDepleted = resource.IsDepleted; // 소진 상태 저장
            resourceSaveData.respawnRemainingSeconds = resource.RespawnRemainingSeconds; // 재생성 대기 시간 저장
            saveData.world.gatherableResources.Add(resourceSaveData); // 채집 자원 목록 추가
        }

        saveData.world.hasCapturedWorldState = true; // 월드 상태 저장 완료 표시
        errorMessage = string.Empty; // 오류 내용 초기화
        return true; // 수집 성공
    }

    public bool TryRestore(SaveGameData saveData, out string errorMessage) // 저장 월드 상태 복원
    {
        if (saveData == null || saveData.world == null) // 월드 저장 데이터 확인
        {
            errorMessage = "월드 저장 데이터가 누락되었습니다."; // 저장 데이터 오류
            return false; // 복원 실패
        }

        if (!saveData.world.hasCapturedWorldState) // 이전 저장 파일 확인
        {
            errorMessage = string.Empty; // 오류 내용 초기화
            return true; // 기존 Scene 상태 유지
        }

        if (!TryBuildCurrentObjectMaps(
            out Dictionary<string, WorldItemPickup> currentWorldItems,
            out Dictionary<string, GatherableResource> currentResources,
            out errorMessage)) // 현재 월드 오브젝트 검사
        {
            return false; // 복원 실패
        }

        if (!TryValidateSavedState(saveData, currentResources, out errorMessage)) // 저장 월드 내용 검사
        {
            return false; // 복원 실패
        }

        foreach (WorldItemPickup currentWorldItem in currentWorldItems.Values) // 현재 월드 아이템 순회
        {
            currentWorldItem.SetAvailableForLoad(false); // 불러오기 전 모든 아이템 숨김
        }

        foreach (GatherableResource currentResource in currentResources.Values) // 현재 채집 자원 순회
        {
            currentResource.ResetForLoad(); // 불러오기 전 기본 상태 복구
        }

        for (int index = 0; index < saveData.world.worldItems.Count; index++) // 저장 월드 아이템 순회
        {
            WorldItemSaveData itemSaveData = saveData.world.worldItems[index]; // 현재 저장 항목 조회
            itemDatabase.TryGetItem(itemSaveData.itemId, out ItemData itemData); // 아이템 ID 조회

            if (!currentWorldItems.TryGetValue(itemSaveData.worldObjectId, out WorldItemPickup worldItem)) // 기존 월드 아이템 확인
            {
                Vector3 spawnPosition = itemSaveData.position.ToVector3(); // 저장 위치 변환
                Quaternion spawnRotation = itemSaveData.rotation.ToQuaternion(); // 저장 회전 변환
                worldItem = Instantiate(worldItemPrefab, spawnPosition, spawnRotation, worldItemParent); // 동적 월드 아이템 생성
            }

            worldItem.RestoreFromSave(
                itemData,
                itemSaveData.quantity,
                itemSaveData.worldObjectId,
                itemSaveData.position.ToVector3(),
                itemSaveData.rotation.ToQuaternion()); // 저장 아이템 상태 적용
        }

        for (int index = 0; index < saveData.world.gatherableResources.Count; index++) // 저장 채집 자원 순회
        {
            GatherableResourceSaveData resourceSaveData = saveData.world.gatherableResources[index]; // 현재 자원 저장 항목 조회
            GatherableResource resource = currentResources[resourceSaveData.worldObjectId]; // ID와 일치하는 자원 조회
            resource.RestoreFromSave(
                resourceSaveData.remainingQuantity,
                resourceSaveData.isDepleted,
                resourceSaveData.respawnRemainingSeconds); // 저장 자원 상태 적용
        }

        errorMessage = string.Empty; // 오류 내용 초기화
        return true; // 복원 성공
    }

    private bool TryBuildCurrentObjectMaps(
        out Dictionary<string, WorldItemPickup> worldItems,
        out Dictionary<string, GatherableResource> resources,
        out string errorMessage) // 현재 월드 ID 사전 생성
    {
        worldItems = new Dictionary<string, WorldItemPickup>(StringComparer.Ordinal); // 월드 아이템 ID 사전 생성
        resources = new Dictionary<string, GatherableResource>(StringComparer.Ordinal); // 채집 자원 ID 사전 생성
        HashSet<string> usedWorldObjectIds = new HashSet<string>(StringComparer.Ordinal); // 전체 중복 ID 검사 목록

        WorldItemPickup[] foundWorldItems = UnityEngine.Object.FindObjectsByType<WorldItemPickup>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None); // 비활성화 포함 월드 아이템 검색

        for (int index = 0; index < foundWorldItems.Length; index++) // 전체 월드 아이템 순회
        {
            WorldItemPickup worldItem = foundWorldItems[index]; // 현재 월드 아이템 조회
            string worldObjectId = worldItem.WorldObjectId; // 현재 고유 ID 조회

            if (string.IsNullOrWhiteSpace(worldObjectId)) // ID 존재 확인
            {
                errorMessage = $"{worldItem.gameObject.name}의 World Object ID가 비어 있습니다."; // 빈 ID 오류 저장
                return false; // 사전 생성 실패
            }

            if (!usedWorldObjectIds.Add(worldObjectId)) // 전체 ID 중복 확인
            {
                errorMessage = $"중복 World Object ID가 있습니다: {worldObjectId}"; // 중복 ID 오류 저장
                return false; // 사전 생성 실패
            }

            worldItems.Add(worldObjectId, worldItem); // 월드 아이템 사전 등록
        }

        GatherableResource[] foundResources = UnityEngine.Object.FindObjectsByType<GatherableResource>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None); // 비활성화 포함 채집 자원 검색

        for (int index = 0; index < foundResources.Length; index++) // 전체 채집 자원 순회
        {
            GatherableResource resource = foundResources[index]; // 현재 채집 자원 조회
            string worldObjectId = resource.WorldObjectId; // 현재 고유 ID 조회

            if (string.IsNullOrWhiteSpace(worldObjectId)) // ID 존재 확인
            {
                errorMessage = $"{resource.gameObject.name}의 World Object ID가 비어 있습니다."; // 빈 ID 오류 저장
                return false; // 사전 생성 실패
            }

            if (!usedWorldObjectIds.Add(worldObjectId)) // 전체 ID 중복 확인
            {
                errorMessage = $"중복 World Object ID가 있습니다: {worldObjectId}"; // 중복 ID 오류 저장
                return false; // 사전 생성 실패
            }

            resources.Add(worldObjectId, resource); // 채집 자원 사전 등록
        }

        errorMessage = string.Empty; // 오류 내용 초기화
        return true; // 사전 생성 성공
    }

    private bool TryValidateSavedState(
        SaveGameData saveData,
        Dictionary<string, GatherableResource> currentResources,
        out string errorMessage) // 저장 월드 데이터 검사
    {
        if (saveData.world.worldItems == null || saveData.world.gatherableResources == null) // 저장 목록 존재 확인
        {
            errorMessage = "월드 아이템 또는 채집 자원 목록이 누락되었습니다."; // 목록 오류 저장
            return false; // 검사 실패
        }

        HashSet<string> usedWorldObjectIds = new HashSet<string>(StringComparer.Ordinal); // 저장 ID 중복 검사 목록

        for (int index = 0; index < saveData.world.worldItems.Count; index++) // 저장 월드 아이템 순회
        {
            WorldItemSaveData itemSaveData = saveData.world.worldItems[index]; // 현재 저장 항목 조회

            if (itemSaveData == null) // 저장 항목 존재 확인
            {
                errorMessage = "비어 있는 월드 아이템 저장 항목이 있습니다."; // 빈 항목 오류
                return false; // 검사 실패
            }

            if (string.IsNullOrWhiteSpace(itemSaveData.worldObjectId)) // 월드 ID 존재 확인
            {
                errorMessage = "월드 아이템의 World Object ID가 비어 있습니다."; // 빈 ID 오류
                return false; // 검사 실패
            }

            if (!usedWorldObjectIds.Add(itemSaveData.worldObjectId)) // 저장 ID 중복 확인
            {
                errorMessage = $"중복 저장 World Object ID입니다: {itemSaveData.worldObjectId}"; // 중복 오류
                return false; // 검사 실패
            }

            if (!itemDatabase.TryGetItem(itemSaveData.itemId, out ItemData itemData)) // 아이템 ID 등록 확인
            {
                errorMessage = $"등록되지 않은 월드 Item ID입니다: {itemSaveData.itemId}"; // 아이템 ID 오류
                return false; // 검사 실패
            }

            if (itemSaveData.quantity <= 0) // 저장 수량 확인
            {
                errorMessage = $"월드 아이템 수량이 잘못되었습니다: {itemData.ItemId}"; // 수량 오류
                return false; // 검사 실패
            }

            if (itemSaveData.position == null || itemSaveData.rotation == null) // 위치와 회전 존재 확인
            {
                errorMessage = $"월드 아이템 위치 또는 회전이 누락되었습니다: {itemSaveData.worldObjectId}"; // Transform 오류
                return false; // 검사 실패
            }
        }

        for (int index = 0; index < saveData.world.gatherableResources.Count; index++) // 저장 채집 자원 순회
        {
            GatherableResourceSaveData resourceSaveData = saveData.world.gatherableResources[index]; // 현재 저장 항목 조회

            if (resourceSaveData == null) // 저장 항목 존재 확인
            {
                errorMessage = "비어 있는 채집 자원 저장 항목이 있습니다."; // 빈 항목 오류
                return false; // 검사 실패
            }

            if (string.IsNullOrWhiteSpace(resourceSaveData.worldObjectId)) // 자원 ID 존재 확인
            {
                errorMessage = "채집 자원의 World Object ID가 비어 있습니다."; // 빈 ID 오류
                return false; // 검사 실패
            }

            if (!usedWorldObjectIds.Add(resourceSaveData.worldObjectId)) // 전체 저장 ID 중복 확인
            {
                errorMessage = $"중복 저장 World Object ID입니다: {resourceSaveData.worldObjectId}"; // 중복 오류
                return false; // 검사 실패
            }

            if (!currentResources.TryGetValue(resourceSaveData.worldObjectId, out GatherableResource resource)) // Scene 자원 존재 확인
            {
                errorMessage = $"Scene에서 채집 자원을 찾을 수 없습니다: {resourceSaveData.worldObjectId}"; // 자원 누락 오류
                return false; // 검사 실패
            }

            if (resourceSaveData.remainingQuantity < 0 || resourceSaveData.remainingQuantity > resource.TotalQuantity) // 남은 수량 범위 확인
            {
                errorMessage = $"채집 자원 수량이 잘못되었습니다: {resourceSaveData.worldObjectId}"; // 자원 수량 오류
                return false; // 검사 실패
            }

            if (resourceSaveData.isDepleted && resourceSaveData.remainingQuantity != 0) // 소진 상태 수량 확인
            {
                errorMessage = $"소진 자원의 남은 수량이 0이 아닙니다: {resourceSaveData.worldObjectId}"; // 소진 상태 오류
                return false; // 검사 실패
            }

            if (!resourceSaveData.isDepleted && resourceSaveData.remainingQuantity <= 0) // 활성 상태 수량 확인
            {
                errorMessage = $"활성 자원의 남은 수량이 잘못되었습니다: {resourceSaveData.worldObjectId}"; // 활성 상태 오류
                return false; // 검사 실패
            }

            bool hasInvalidRespawnTime = resourceSaveData.respawnRemainingSeconds < 0f
                || float.IsNaN(resourceSaveData.respawnRemainingSeconds)
                || float.IsInfinity(resourceSaveData.respawnRemainingSeconds); // 재생성 시간 유효성 확인

            if (hasInvalidRespawnTime) // 잘못된 재생성 시간 확인
            {
                errorMessage = $"채집 자원 재생성 시간이 잘못되었습니다: {resourceSaveData.worldObjectId}"; // 시간 오류
                return false; // 검사 실패
            }
        }

        errorMessage = string.Empty; // 오류 내용 초기화
        return true; // 검사 성공
    }
}