using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class CookingIngredientRowUI : MonoBehaviour // 85일차: 요리 창 재료 한 줄 (보유 / 필요)
{
    [Tooltip("재료 아이콘.")]
    [SerializeField] private Image icon; // 아이콘
    [Tooltip("재료 이름.")]
    [SerializeField] private TMP_Text nameText; // 이름
    [Tooltip("보유 / 필요 수량.")]
    [SerializeField] private TMP_Text countText; // 수량
    [Tooltip("상태 막대.")]
    [SerializeField] private Image stateBar; // 상태 막대

    public void Bind(ItemData item, int have, int need, bool isFuel) // 표시 적용
    {
        gameObject.SetActive(true); // 표시
        bool enough = have >= need; // 충분 여부
        Color stateColor = enough ? ProjectUUIPalette.Teal : ProjectUUIPalette.Danger; // 상태 색

        icon.sprite = item.Icon; // 아이콘
        icon.color = item.Icon != null ? Color.white : ItemIconUtility.GetFallbackColor(item.ItemCategory); // 아이콘 색
        nameText.SetText(isFuel ? $"{item.DisplayName}  <size=70%><color=#B8B2A6>FUEL</color></size>" : item.DisplayName); // 이름
        countText.SetText($"{have} / {need}"); // 수량
        countText.color = stateColor; // 수량 색
        stateBar.color = stateColor; // 막대 색
    }
}
