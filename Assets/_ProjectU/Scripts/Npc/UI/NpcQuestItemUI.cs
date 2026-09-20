using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class NpcQuestItemUI : MonoBehaviour // 93일차: 게시판 상세의 필요한 물건 한 칸 (아이콘 · 이름 · 가방 수량/필요 수량)
{
    [SerializeField] private Image background; // 배경
    [SerializeField] private Image icon; // 아이콘
    [SerializeField] private TMP_Text nameText; // 이름
    [SerializeField] private TMP_Text countText; // 수량

    public string CountLabel => countText != null ? countText.text : string.Empty; // 수량 (테스트용)

    public void Bind(ItemData item, int have, int need) // 표시
    {
        gameObject.SetActive(true);
        bool enough = have >= need;
        icon.sprite = item != null ? item.Icon : null;
        icon.color = icon.sprite != null ? Color.white : ItemIconUtility.GetFallbackColor(item != null ? item.ItemCategory : ItemCategory.CraftingMaterial);
        nameText.text = item != null ? item.KoreanName : "?";
        countText.text = $"{Mathf.Min(have, need)} / {need}";
        countText.color = enough ? ProjectUUIPalette.Teal : ProjectUUIPalette.TextSecondary;
        background.color = enough ? new Color(0.31f, 0.76f, 0.69f, 0.16f) : new Color(1f, 1f, 1f, 0.05f);
    }
}
