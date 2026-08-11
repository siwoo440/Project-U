using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "EnemyLootTable_",
    menuName = "Project U/Combat/Enemy Loot Table")]
public sealed class EnemyLootTable : ScriptableObject
{
    [Serializable]
    public sealed class LootEntry
    {
        [Header("Item")]
        [Tooltip("드롭할 아이템 데이터입니다.")]
        [SerializeField] private ItemData itemData;

        [Tooltip("월드에 실제로 생성할 전용 Pickup Prefab입니다.")]
        [SerializeField] private WorldItemPickup pickupPrefab;

        [Header("Chance")]
        [Tooltip("0부터 1 사이의 드롭 확률입니다.")]
        [SerializeField, Range(0f, 1f)] private float dropChance = 1f;

        [Header("Quantity")]
        [Tooltip("드롭 성공 시 최소 수량입니다.")]
        [SerializeField, Min(1)] private int minimumQuantity = 1;

        [Tooltip("드롭 성공 시 최대 수량입니다.")]
        [SerializeField, Min(1)] private int maximumQuantity = 1;

        public ItemData ItemData => itemData;
        public WorldItemPickup PickupPrefab => pickupPrefab;
        public float DropChance => Mathf.Clamp01(dropChance);
        public int MinimumQuantity => Mathf.Max(1, minimumQuantity);
        public int MaximumQuantity => Mathf.Max(MinimumQuantity, maximumQuantity);

        public bool RollDrop()
        {
            return UnityEngine.Random.value <= DropChance;
        }

        public int RollQuantity()
        {
            return UnityEngine.Random.Range(
                MinimumQuantity,
                MaximumQuantity + 1);
        }

        public void ValidateValues()
        {
            dropChance = Mathf.Clamp01(dropChance);
            minimumQuantity = Mathf.Max(1, minimumQuantity);
            maximumQuantity = Mathf.Max(minimumQuantity, maximumQuantity);
        }
    }

    [Header("Loot Entries")]
    [Tooltip("이 적이 사망했을 때 각각 독립적으로 판정할 드롭 목록입니다.")]
    [SerializeField] private List<LootEntry> entries = new List<LootEntry>();

    public IReadOnlyList<LootEntry> Entries => entries;

    public bool TryValidate(out string errorMessage)
    {
        if (entries == null)
        {
            errorMessage = "Loot Entries 목록이 없습니다.";
            return false;
        }

        HashSet<string> usedItemIds = new HashSet<string>(StringComparer.Ordinal);

        for (int index = 0; index < entries.Count; index++)
        {
            LootEntry entry = entries[index];

            if (entry == null)
            {
                errorMessage = $"Loot Entry {index}가 비어 있습니다.";
                return false;
            }

            if (entry.ItemData == null)
            {
                errorMessage = $"Loot Entry {index}의 Item Data가 비어 있습니다.";
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
                errorMessage = $"Loot Table에 같은 Item ID가 두 번 등록되어 있습니다: {itemId}";
                return false;
            }
        }

        errorMessage = string.Empty;
        return true;
    }

    private void OnValidate()
    {
        if (entries == null)
        {
            return;
        }

        for (int index = 0; index < entries.Count; index++)
        {
            LootEntry entry = entries[index];

            if (entry != null)
            {
                entry.ValidateValues();
            }
        }
    }
}
