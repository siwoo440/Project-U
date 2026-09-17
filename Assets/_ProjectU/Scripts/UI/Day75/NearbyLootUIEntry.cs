using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 근처 아이템 목록의 한 줄 : [분류 색 아이콘] 이름 / 분류 ........ 수량 / 거리
[DisallowMultipleComponent]
public sealed class NearbyLootUIEntry : MonoBehaviour
{
    [Header("References")]
    [Tooltip("아이템 아이콘을 표시할 Image입니다.")]
    [SerializeField] private Image iconImage;

    [Tooltip("아이콘 뒤 분류 색상 배경 Image입니다.")]
    [SerializeField] private Image iconBackground;

    [Tooltip("아이콘이 없을 때 대신 보여줄 첫 글자 TMP Text입니다.")]
    [SerializeField] private TMP_Text iconLetterText;

    [Tooltip("아이템 이름을 표시할 TMP Text입니다.")]
    [SerializeField] private TMP_Text itemNameText;

    [Tooltip("아이템 분류를 표시할 TMP Text입니다.")]
    [SerializeField] private TMP_Text categoryText;

    [Tooltip("주변에 있는 같은 아이템의 총 수량을 표시할 TMP Text입니다.")]
    [SerializeField] private TMP_Text quantityText;

    [Tooltip("같은 아이템 중 가장 가까운 Pickup까지의 거리를 표시할 TMP Text입니다.")]
    [SerializeField] private TMP_Text distanceText;

    [Header("Style")]
    [Tooltip("이 거리 안이면 바로 주울 수 있는 거리로 강조합니다.")]
    [SerializeField, Min(0f)] private float reachDistance = 2.2f;

    [SerializeField] private Color quantityColor = new Color(0.95f, 0.72f, 0.3f, 1f);
    [SerializeField] private Color distanceColor = new Color(0.72f, 0.7f, 0.65f, 1f);
    [SerializeField] private Color reachColor = new Color(0.31f, 0.76f, 0.69f, 1f);
    [SerializeField] private Color iconSlotColor = new Color(0.14f, 0.165f, 0.205f, 0.95f);

    // 같은 값이면 문자열·색상 갱신 생략
    private ItemData boundItem;
    private bool hasBoundItem;
    private int boundQuantity = -1;
    private int boundDistanceTenths = -1;
    private bool boundInReach;

    private void Awake()
    {
        if (quantityText != null)
        {
            quantityText.color = quantityColor;
        }
    }

    public void Bind(NearbyLootDisplayData data)
    {
        if (!hasBoundItem || data.ItemData != boundItem)
        {
            hasBoundItem = true;
            boundItem = data.ItemData;
            ApplyItem(boundItem);
        }

        int quantity = Mathf.Max(0, data.TotalQuantity);

        if (quantity != boundQuantity && quantityText != null)
        {
            boundQuantity = quantity;
            quantityText.SetText("x{0}", quantity);
        }

        float distance = Mathf.Max(0f, data.NearestDistance);
        int distanceTenths = Mathf.RoundToInt(distance * 10f);
        bool inReach = distance <= reachDistance;

        if (distanceText == null || (distanceTenths == boundDistanceTenths && inReach == boundInReach))
        {
            return;
        }

        boundDistanceTenths = distanceTenths;
        boundInReach = inReach;
        distanceText.SetText("{0:1}m", distanceTenths / 10f);
        distanceText.color = inReach ? reachColor : distanceColor;
    }

    private void ApplyItem(ItemData itemData)
    {
        string displayName = itemData == null || string.IsNullOrWhiteSpace(itemData.DisplayName)
            ? "UNKNOWN ITEM"
            : itemData.DisplayName;
        Sprite icon = itemData != null ? itemData.Icon : null;
        Color categoryColor = itemData != null
            ? ItemIconUtility.GetFallbackColor(itemData.ItemCategory)
            : ItemIconUtility.GetFallbackColor((ItemCategory)(-1));

        if (itemNameText != null)
        {
            itemNameText.SetText(displayName);
        }

        if (categoryText != null)
        {
            categoryText.SetText(itemData != null ? GetCategoryLabel(itemData.ItemCategory) : "-");
            categoryText.color = Color.Lerp(categoryColor, Color.white, 0.35f);
        }

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        if (iconLetterText != null)
        {
            iconLetterText.enabled = icon == null;
            iconLetterText.SetText(displayName.Substring(0, 1).ToUpperInvariant());
        }

        if (iconBackground != null)
        {
            // 아이콘이 없으면 인벤토리처럼 분류 색상으로 구분
            iconBackground.color = icon != null
                ? iconSlotColor
                : new Color(categoryColor.r * 0.72f, categoryColor.g * 0.72f, categoryColor.b * 0.72f, 1f);
        }
    }

    private static string GetCategoryLabel(ItemCategory category)
    {
        switch (category)
        {
            case ItemCategory.CraftingMaterial: return "MATERIAL";
            case ItemCategory.Tool: return "TOOL";
            case ItemCategory.Food: return "FOOD";
            case ItemCategory.Equipment: return "EQUIPMENT";
            case ItemCategory.Drink: return "DRINK";
            case ItemCategory.Medicine: return "MEDICINE";
            case ItemCategory.Weapon: return "WEAPON";
            case ItemCategory.Seed: return "SEED";
            default: return "ITEM";
        }
    }
}
