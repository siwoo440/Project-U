using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "WorldItemPickupRegistry_",
    menuName = "Project U/World/World Item Pickup Registry")]
public sealed class WorldItemPickupRegistry : ScriptableObject
{
    [Serializable]
    public sealed class PickupEntry
    {
        [Tooltip("이 Pickup Prefab이 표현할 아이템 데이터입니다.")]
        [SerializeField] private ItemData itemData;

        [Tooltip("저장 복원과 동적 드롭에 사용할 아이템 전용 Pickup Prefab입니다.")]
        [SerializeField] private WorldItemPickup pickupPrefab;

        public ItemData ItemData => itemData;
        public WorldItemPickup PickupPrefab => pickupPrefab;
    }

    [Header("Entries")]
    [Tooltip("ItemData와 실제 외형을 가진 Pickup Prefab의 연결 목록입니다.")]
    [SerializeField] private List<PickupEntry> entries = new List<PickupEntry>();

    public IReadOnlyList<PickupEntry> Entries => entries;

    public bool TryGetPickup(ItemData itemData, out WorldItemPickup pickupPrefab)
    {
        pickupPrefab = null;

        if (itemData == null)
        {
            return false;
        }

        return TryGetPickup(itemData.ItemId, out pickupPrefab);
    }

    public bool TryGetPickup(string itemId, out WorldItemPickup pickupPrefab)
    {
        pickupPrefab = null;

        if (string.IsNullOrWhiteSpace(itemId) || entries == null)
        {
            return false;
        }

        for (int index = 0; index < entries.Count; index++)
        {
            PickupEntry entry = entries[index];

            if (entry == null || entry.ItemData == null || entry.PickupPrefab == null)
            {
                continue;
            }

            if (!string.Equals(
                entry.ItemData.ItemId,
                itemId,
                StringComparison.Ordinal))
            {
                continue;
            }

            pickupPrefab = entry.PickupPrefab;
            return true;
        }

        return false;
    }

    public bool TryValidate(out string errorMessage)
    {
        if (entries == null)
        {
            errorMessage = "Pickup Registry Entries가 없습니다.";
            return false;
        }

        HashSet<string> usedItemIds = new HashSet<string>(StringComparer.Ordinal);

        for (int index = 0; index < entries.Count; index++)
        {
            PickupEntry entry = entries[index];

            if (entry == null)
            {
                errorMessage = $"Pickup Entry {index}가 비어 있습니다.";
                return false;
            }

            if (entry.ItemData == null)
            {
                errorMessage = $"Pickup Entry {index}의 Item Data가 비어 있습니다.";
                return false;
            }

            if (entry.PickupPrefab == null)
            {
                errorMessage = $"{entry.ItemData.DisplayName}의 Pickup Prefab이 비어 있습니다.";
                return false;
            }

            string itemId = entry.ItemData.ItemId;

            if (string.IsNullOrWhiteSpace(itemId))
            {
                errorMessage = $"{entry.ItemData.name}의 Item ID가 비어 있습니다.";
                return false;
            }

            if (!usedItemIds.Add(itemId))
            {
                errorMessage = $"Pickup Registry에 중복 Item ID가 있습니다: {itemId}";
                return false;
            }
        }

        errorMessage = string.Empty;
        return true;
    }
}
