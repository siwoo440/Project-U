using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class NearbyLootScanner : MonoBehaviour
{
    private sealed class LootAggregate
    {
        public ItemData ItemData;
        public int TotalQuantity;
        public float NearestDistance = float.PositiveInfinity;
    }

    [Header("References")]
    [Tooltip("거리 계산의 기준이 될 플레이어 Transform입니다.")]
    [SerializeField] private Transform playerTransform;

    [Tooltip("검색 결과를 표시할 Nearby Loot UI입니다.")]
    [SerializeField] private NearbyLootUI nearbyLootUI;

    [Header("Scan")]
    [Tooltip("이 거리 안의 모든 활성 WorldItemPickup을 검색합니다.")]
    [SerializeField, Min(0.5f)] private float scanRadius = 7f;

    [Tooltip("주변 아이템 목록을 다시 계산하는 간격입니다.")]
    [SerializeField, Min(0.05f)] private float scanInterval = 0.2f;

    [Header("Debug")]
    [Tooltip("검색된 아이템 종류 수를 Console에 출력합니다.")]
    [SerializeField] private bool logScanResults;

    private readonly Dictionary<string, LootAggregate> aggregates =
        new Dictionary<string, LootAggregate>(StringComparer.Ordinal);

    private readonly List<NearbyLootDisplayData> displayData =
        new List<NearbyLootDisplayData>();

    private float nextScanTime;

    private void Start()
    {
        ResolveReferences();
        ScanNearbyLoot();
    }

    private void Update()
    {
        if (Time.time < nextScanTime)
        {
            return;
        }

        ScanNearbyLoot();
    }

    [ContextMenu("Scan Nearby Loot Now")]
    public void ScanNearbyLoot()
    {
        nextScanTime = Time.time + scanInterval;
        ResolveReferences();

        if (playerTransform == null || nearbyLootUI == null)
        {
            return;
        }

        aggregates.Clear();
        displayData.Clear();

        WorldItemPickup[] pickups = FindObjectsByType<WorldItemPickup>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int index = 0; index < pickups.Length; index++)
        {
            WorldItemPickup pickup = pickups[index];

            if (pickup == null
                || !pickup.IsAvailable
                || pickup.ItemData == null
                || pickup.Quantity <= 0)
            {
                continue;
            }

            float distance = Vector3.Distance(
                playerTransform.position,
                pickup.transform.position);

            if (distance > scanRadius)
            {
                continue;
            }

            ItemData itemData = pickup.ItemData;
            string itemId = string.IsNullOrWhiteSpace(itemData.ItemId)
                ? itemData.name
                : itemData.ItemId;

            if (!aggregates.TryGetValue(itemId, out LootAggregate aggregate))
            {
                aggregate = new LootAggregate
                {
                    ItemData = itemData
                };

                aggregates.Add(itemId, aggregate);
            }

            aggregate.TotalQuantity += pickup.Quantity;
            aggregate.NearestDistance = Mathf.Min(
                aggregate.NearestDistance,
                distance);
        }

        foreach (LootAggregate aggregate in aggregates.Values)
        {
            displayData.Add(new NearbyLootDisplayData(
                aggregate.ItemData,
                aggregate.TotalQuantity,
                aggregate.NearestDistance));
        }

        displayData.Sort(CompareDisplayData);
        nearbyLootUI.Refresh(displayData);

        if (logScanResults)
        {
            Debug.Log(
                $"Nearby Loot 검색 완료 / 종류 {displayData.Count}개",
                this);
        }
    }

    private int CompareDisplayData(
        NearbyLootDisplayData left,
        NearbyLootDisplayData right)
    {
        int distanceCompare =
            left.NearestDistance.CompareTo(right.NearestDistance);

        if (distanceCompare != 0)
        {
            return distanceCompare;
        }

        string leftName = left.ItemData == null
            ? string.Empty
            : left.ItemData.DisplayName;

        string rightName = right.ItemData == null
            ? string.Empty
            : right.ItemData.DisplayName;

        return string.Compare(
            leftName,
            rightName,
            StringComparison.Ordinal);
    }

    private void ResolveReferences()
    {
        if (playerTransform == null)
        {
            PlayerInventory playerInventory =
                FindFirstObjectByType<PlayerInventory>();

            if (playerInventory != null)
            {
                playerTransform = playerInventory.transform;
            }
        }

        if (nearbyLootUI == null)
        {
            nearbyLootUI = FindFirstObjectByType<NearbyLootUI>();
        }
    }

    private void OnValidate()
    {
        scanRadius = Mathf.Max(0.5f, scanRadius);
        scanInterval = Mathf.Max(0.05f, scanInterval);
    }

    private void OnDrawGizmosSelected()
    {
        Transform centerTransform = playerTransform == null
            ? transform
            : playerTransform;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(centerTransform.position, scanRadius);
    }
}
