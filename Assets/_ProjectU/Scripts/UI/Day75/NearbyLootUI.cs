using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 근처 아이템 HUD : 가까운 순으로 최대 몇 줄만 보여주고 나머지는 "+N MORE"로 요약한다.
// (게임 중에는 커서가 잠기고 휠이 핫바 전환이라 스크롤 목록은 사용할 수 없다)
[DisallowMultipleComponent]
public sealed class NearbyLootUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("아이템이 없을 때 숨길 Nearby Items Panel입니다.")]
    [SerializeField] private GameObject panelRoot;

    [Tooltip("Entry가 세로로 쌓일 목록 RectTransform입니다.")]
    [SerializeField] private RectTransform contentRoot;

    [Tooltip("각 아이템 한 줄을 표시할 UI Entry Prefab입니다.")]
    [SerializeField] private NearbyLootUIEntry entryPrefab;

    [Tooltip("근처 아이템 종류 수를 표시할 TMP Text입니다.")]
    [SerializeField] private TMP_Text countText;

    [Tooltip("표시하지 못한 아이템 수를 요약할 TMP Text입니다.")]
    [SerializeField] private TMP_Text moreText;

    [Header("Display")]
    [Tooltip("한 번에 보여줄 최대 줄 수입니다.")]
    [SerializeField, Min(1)] private int maximumEntries = 6;

    [SerializeField] private Color countColor = new Color(0.95f, 0.72f, 0.3f, 1f);

    [Header("Runtime")]
    [SerializeField] private int visibleEntryCount;
    [SerializeField] private int totalEntryCount;

    private readonly List<NearbyLootUIEntry> entryPool =
        new List<NearbyLootUIEntry>();

    private int shownCountValue = -1;
    private int shownHiddenValue = -1;

    public int VisibleEntryCount => visibleEntryCount;
    public int TotalEntryCount => totalEntryCount;

    private void Awake()
    {
        if (countText != null)
        {
            countText.color = countColor;
        }

        SetPanelVisible(false);
    }

    public void Refresh(IReadOnlyList<NearbyLootDisplayData> data)
    {
        int totalCount = data == null ? 0 : data.Count;
        int requestedCount = Mathf.Min(totalCount, maximumEntries);

        EnsureEntryPool(requestedCount);

        for (int index = 0; index < entryPool.Count; index++)
        {
            NearbyLootUIEntry entry = entryPool[index];
            bool shouldShow = index < requestedCount;

            if (entry.gameObject.activeSelf != shouldShow)
            {
                entry.gameObject.SetActive(shouldShow);
            }

            if (shouldShow)
            {
                entry.Bind(data[index]);
            }
        }

        visibleEntryCount = Mathf.Min(requestedCount, entryPool.Count);
        totalEntryCount = totalCount;
        RefreshSummary(totalCount, totalCount - visibleEntryCount);
        SetPanelVisible(totalCount > 0);
    }

    private void RefreshSummary(int totalCount, int hiddenCount)
    {
        if (countText != null && totalCount != shownCountValue)
        {
            shownCountValue = totalCount;
            countText.SetText("{0}", totalCount);
        }

        if (moreText == null || hiddenCount == shownHiddenValue)
        {
            return;
        }

        shownHiddenValue = hiddenCount;
        bool showMore = hiddenCount > 0;

        if (moreText.gameObject.activeSelf != showMore)
        {
            moreText.gameObject.SetActive(showMore);
        }

        if (showMore)
        {
            moreText.SetText("+{0} MORE", hiddenCount);
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
