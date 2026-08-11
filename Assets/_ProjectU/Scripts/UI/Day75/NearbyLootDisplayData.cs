public readonly struct NearbyLootDisplayData
{
    public ItemData ItemData { get; }
    public int TotalQuantity { get; }
    public float NearestDistance { get; }

    public NearbyLootDisplayData(
        ItemData itemData,
        int totalQuantity,
        float nearestDistance)
    {
        ItemData = itemData;
        TotalQuantity = totalQuantity;
        NearestDistance = nearestDistance;
    }
}
