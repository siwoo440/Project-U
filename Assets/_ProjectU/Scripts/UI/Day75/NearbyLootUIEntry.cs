using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class NearbyLootUIEntry : MonoBehaviour
{
    [Header("References")]
    [Tooltip("아이템 아이콘을 표시할 Image입니다.")]
    [SerializeField] private Image iconImage;

    [Tooltip("아이템 이름을 표시할 TMP Text입니다.")]
    [SerializeField] private TMP_Text itemNameText;

    [Tooltip("주변에 있는 같은 아이템의 총 수량을 표시할 TMP Text입니다.")]
    [SerializeField] private TMP_Text quantityText;

    [Tooltip("같은 아이템 중 가장 가까운 Pickup까지의 거리를 표시할 TMP Text입니다.")]
    [SerializeField] private TMP_Text distanceText;

    public void Bind(NearbyLootDisplayData data)
    {
        ItemData itemData = data.ItemData;

        if (iconImage != null)
        {
            Sprite icon = itemData == null
                ? null
                : itemData.Icon;

            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        if (itemNameText != null)
        {
            itemNameText.text = itemData == null
                ? "UNKNOWN ITEM"
                : itemData.DisplayName;
        }

        if (quantityText != null)
        {
            quantityText.text = $"x{Mathf.Max(0, data.TotalQuantity)}";
        }

        if (distanceText != null)
        {
            distanceText.text = $"{Mathf.Max(0f, data.NearestDistance):0.0}m";
        }
    }
}
