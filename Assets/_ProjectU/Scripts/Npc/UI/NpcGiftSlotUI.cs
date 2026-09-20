using System; // Action
using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class NpcGiftSlotUI : MonoBehaviour // 91일차: 선물 목록 한 칸 (아이콘 · 이름 · 수량)
{
    [SerializeField] private Button button; // 누르기
    [SerializeField] private Image icon; // 아이콘
    [SerializeField] private TMP_Text nameText; // 이름
    [SerializeField] private TMP_Text countText; // 수량

    private Action onClick; // 눌렀을 때

    public ItemData Item { get; private set; } // 아이템 제공

    private void Awake() // 버튼 연결
    {
        if (button != null)
        {
            button.onClick.AddListener(() => onClick?.Invoke());
        }
    }

    public void Bind(ItemData item, int quantity, Action clicked) // 표시
    {
        Item = item;
        onClick = clicked;

        if (icon != null)
        {
            icon.sprite = item != null ? item.Icon : null;
            icon.enabled = icon.sprite != null;
        }

        if (nameText != null)
        {
            nameText.text = item != null ? item.KoreanName : string.Empty;
        }

        if (countText != null)
        {
            countText.text = quantity > 1 ? $"x{quantity}" : string.Empty;
        }
    }

    public void Press() // 테스트용 누르기
    {
        onClick?.Invoke();
    }
}
