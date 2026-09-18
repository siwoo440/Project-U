using System; // 이벤트 기능
using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class ShopRowUI : MonoBehaviour // 87일차: 상인 창 왼쪽 목록 한 줄 (살 물건 · 판매 가격)
{
    [Tooltip("클릭 버튼.")]
    [SerializeField] private Button button; // 버튼
    [Tooltip("줄 배경.")]
    [SerializeField] private Image background; // 배경
    [Tooltip("선택 테두리.")]
    [SerializeField] private Image selectionOutline; // 선택 테두리
    [Tooltip("아이콘 배경.")]
    [SerializeField] private Image iconBackground; // 아이콘 배경
    [Tooltip("아이템 아이콘.")]
    [SerializeField] private Image icon; // 아이콘
    [Tooltip("아이템 이름.")]
    [SerializeField] private TMP_Text nameText; // 이름
    [Tooltip("상태 문구 (남은 수 · 제철).")]
    [SerializeField] private TMP_Text statusText; // 상태
    [Tooltip("가격.")]
    [SerializeField] private TMP_Text priceText; // 가격
    [Tooltip("가격 옆 코인 아이콘.")]
    [SerializeField] private Image priceIcon; // 코인 아이콘

    private Action<int> clicked; // 클릭 콜백
    private int rowIndex; // 줄 번호

    public int RowIndex => rowIndex; // 줄 번호 제공
    public string NameLabel => nameText != null ? nameText.text : string.Empty; // 이름 제공 (테스트용)
    public string StatusLabel => statusText != null ? statusText.text : string.Empty; // 상태 제공 (테스트용)
    public string PriceLabel => priceText != null ? priceText.text : string.Empty; // 가격 제공 (테스트용)

    private void Awake() // 버튼 연결
    {
        if (button != null) // 버튼 확인
        {
            button.onClick.AddListener(() => clicked?.Invoke(rowIndex)); // 클릭 연결
        }
    }

    public void Bind(int index, ItemData item, string status, Color statusColor, int price, Color priceColor, bool selected, bool dim, Action<int> onClick) // 표시 적용
    {
        rowIndex = index; // 번호
        clicked = onClick; // 콜백
        gameObject.SetActive(true); // 표시

        nameText.SetText(item != null ? item.DisplayName : "UNKNOWN"); // 이름
        nameText.color = dim ? ProjectUUIPalette.TextSecondary : ProjectUUIPalette.TextPrimary; // 이름 색
        statusText.SetText(status); // 상태
        statusText.color = statusColor; // 상태 색
        priceText.SetText(price.ToString()); // 가격
        priceText.color = priceColor; // 가격 색
        priceIcon.color = dim ? new Color(priceColor.r, priceColor.g, priceColor.b, 0.55f) : priceColor; // 코인 색

        Sprite sprite = item != null ? item.Icon : null; // 아이콘
        icon.sprite = sprite; // 적용
        icon.color = sprite != null ? new Color(1f, 1f, 1f, dim ? 0.5f : 1f) : ItemIconUtility.GetFallbackColor(item != null ? item.ItemCategory : ItemCategory.CraftingMaterial); // 색
        iconBackground.color = selected ? new Color(0.95f, 0.72f, 0.3f, 0.22f) : new Color(1f, 1f, 1f, 0.06f); // 아이콘 배경
        background.color = selected ? ProjectUUIPalette.PanelLight : new Color(1f, 1f, 1f, 0.035f); // 줄 배경
        selectionOutline.gameObject.SetActive(selected); // 선택 테두리
    }
}
