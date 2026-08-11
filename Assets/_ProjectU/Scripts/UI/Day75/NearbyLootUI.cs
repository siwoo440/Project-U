using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class NearbyLootUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("아이템이 없을 때 숨길 Nearby Items Panel입니다.")]
    [SerializeField] private GameObject panelRoot;

    [Tooltip("Scroll View의 Content RectTransform입니다.")]
    [SerializeField] private RectTransform contentRoot;

    [Tooltip("각 아이템 한 줄을 표시할 UI Entry Prefab입니다.")]
    [SerializeField] private NearbyLootUIEntry entryPrefab;

    [Header("Display")]
    [Tooltip("한 번에 생성해 둘 수 있는 최대 Entry 수입니다.")]
    [SerializeField, Min(1)] private int maximumEntries = 50;

    [Header("Runtime")]
    [SerializeField] private int visibleEntryCount;

    private readonly List<NearbyLootUIEntry> entryPool =
        new List<NearbyLootUIEntry>();

    public int VisibleEntryCount => visibleEntryCount;

    private void Awake()
    {
        SetPanelVisible(false);
    }

    public void Refresh(IReadOnlyList<NearbyLootDisplayData> data)
    {
        int requestedCount = data == null
            ? 0
            : Mathf.Min(data.Count, maximumEntries);

        EnsureEntryPool(requestedCount);

        for (int index = 0; index < entryPool.Count; index++)
        {
            NearbyLootUIEntry entry = entryPool[index];
            bool shouldShow = index < requestedCount;

            entry.gameObject.SetActive(shouldShow);

            if (!shouldShow)
            {
                continue;
            }

            entry.Bind(data[index]);
        }

        visibleEntryCount = requestedCount;
        SetPanelVisible(visibleEntryCount > 0);

        if (contentRoot != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
        }
    }

    private void EnsureEntryPool(int requiredCount)
    {
        if (contentRoot == null || entryPrefab == null)
        {
            return;
        }

        while (entryPool.Count < requiredCount)
        {
            NearbyLootUIEntry newEntry = Instantiate(
                entryPrefab,
                contentRoot);

            newEntry.gameObject.SetActive(false);
            entryPool.Add(newEntry);
        }
    }

    private void SetPanelVisible(bool shouldBeVisible)
    {
        if (panelRoot != null && panelRoot.activeSelf != shouldBeVisible)
        {
            panelRoot.SetActive(shouldBeVisible);
        }
    }

    private void OnValidate()
    {
        maximumEntries = Mathf.Max(1, maximumEntries);
    }
}
