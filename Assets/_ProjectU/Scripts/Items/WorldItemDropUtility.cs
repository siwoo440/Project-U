using UnityEngine; // Unity 기본 기능

// 83일차: 인벤토리에 넣지 못한 아이템(수확물·물고기)을 바닥에 떨어뜨리는 공통 기능
public static class WorldItemDropUtility
{
    public static bool TryDrop(WorldItemPickupRegistry pickupRegistry, WorldItemDropContainer dropContainer, ItemData itemData, int quantity, Vector3 position, Object context) // 월드 아이템 생성
    {
        if (itemData == null || quantity <= 0) // 요청 확인
        {
            return false; // 드롭 생략
        }

        if (pickupRegistry == null || !pickupRegistry.TryGetPickup(itemData, out WorldItemPickup pickupPrefab)) // Pickup Prefab 확인
        {
            Debug.LogWarning($"{itemData.DisplayName}의 월드 아이템 Prefab이 없어 바닥에 떨어뜨리지 못했습니다.", context); // 드롭 실패 경고
            return false; // 드롭 실패
        }

        Vector2 offset = Random.insideUnitCircle * 0.35f; // 흩어짐
        Vector3 spawnPosition = position + new Vector3(offset.x, 0.5f, offset.y); // 생성 위치
        Transform parent = dropContainer != null ? dropContainer.transform : null; // 드롭 부모
        WorldItemPickup pickup = Object.Instantiate(pickupPrefab, spawnPosition, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), parent); // 월드 아이템 생성
        pickup.Initialize(itemData, quantity); // 아이템과 수량 적용
        return true; // 드롭 성공
    }
}
