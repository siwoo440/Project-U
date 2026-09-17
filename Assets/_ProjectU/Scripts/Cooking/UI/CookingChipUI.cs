using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class CookingChipUI : MonoBehaviour // 85일차: 아이콘 + 문구 알약 (효과·시간·시설 표시)
{
    [Tooltip("배경.")]
    [SerializeField] private Image background; // 배경
    [Tooltip("아이콘.")]
    [SerializeField] private Image icon; // 아이콘
    [Tooltip("문구.")]
    [SerializeField] private TMP_Text label; // 문구
    [Tooltip("배경 투명도.")]
    [SerializeField, Range(0f, 1f)] private float backgroundAlpha = 0.16f; // 배경 투명도

    private CanvasGroup canvasGroup; // 깜빡임용

    public void Bind(string text, Color color, Sprite sprite) // 표시 적용
    {
        gameObject.SetActive(true); // 표시

        if (label != null) // 문구 확인
        {
            label.SetText(text); // 문구
            label.color = Color.Lerp(color, Color.white, 0.25f); // 색상
        }

        if (icon != null) // 아이콘 확인
        {
            icon.sprite = sprite; // 스프라이트
            icon.color = color; // 색상
            icon.enabled = sprite != null; // 없으면 숨김
        }

        if (background != null) // 배경 확인
        {
            background.color = new Color(color.r, color.g, color.b, backgroundAlpha); // 반투명 배경
        }
    }

    public void SetAlpha(float alpha) // 전체 투명도 (깜빡임)
    {
        if (canvasGroup == null) // 그룹 확인
        {
            canvasGroup = GetComponent<CanvasGroup>(); // 검색

            if (canvasGroup == null) // 없음 확인
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>(); // 추가
                canvasGroup.blocksRaycasts = false; // 입력 통과
                canvasGroup.interactable = false; // 조작 없음
            }
        }

        canvasGroup.alpha = alpha; // 적용
    }
}
