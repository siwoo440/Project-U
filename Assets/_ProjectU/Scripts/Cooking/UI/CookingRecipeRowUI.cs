using System; // 이벤트 기능
using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class CookingRecipeRowUI : MonoBehaviour // 85일차: 요리 창 왼쪽 요리법 한 줄
{
    [Tooltip("클릭 버튼.")]
    [SerializeField] private Button button; // 버튼
    [Tooltip("줄 배경.")]
    [SerializeField] private Image background; // 배경
    [Tooltip("선택 테두리.")]
    [SerializeField] private Image selectionOutline; // 선택 테두리
    [Tooltip("아이콘 배경.")]
    [SerializeField] private Image iconBackground; // 아이콘 배경
    [Tooltip("음식 아이콘.")]
    [SerializeField] private Image icon; // 아이콘
    [Tooltip("요리 이름.")]
    [SerializeField] private TMP_Text nameText; // 이름
    [Tooltip("상태 문구.")]
    [SerializeField] private TMP_Text statusText; // 상태
    [Tooltip("조리 시간.")]
    [SerializeField] private TMP_Text timeText; // 시간

    private Action<CookingRecipeData> clicked; // 클릭 콜백
    private CookingRecipeData boundRecipe; // 표시 요리법

    public CookingRecipeData Recipe => boundRecipe; // 요리법 제공

    private void Awake() // 버튼 연결
    {
        if (button != null) // 버튼 확인
        {
            button.onClick.AddListener(() => clicked?.Invoke(boundRecipe)); // 클릭 연결
        }
    }

    public void Bind(CookingRecipeData recipe, CookingRecipeStatus status, bool selected, Action<CookingRecipeData> onClick) // 표시 적용
    {
        boundRecipe = recipe; // 요리법
        clicked = onClick; // 콜백
        gameObject.SetActive(true); // 표시
        ItemData result = recipe.ResultItem; // 완성 음식
        bool dim = status == CookingRecipeStatus.NeedStation || status == CookingRecipeStatus.MissingItems; // 흐리게 표시 여부

        nameText.SetText(recipe.ResultQuantity > 1 ? $"{recipe.DisplayName} x{recipe.ResultQuantity}" : recipe.DisplayName); // 이름
        nameText.color = dim ? ProjectUUIPalette.TextSecondary : ProjectUUIPalette.TextPrimary; // 이름 색
        statusText.SetText(GetStatusLabel(status)); // 상태
        statusText.color = GetStatusColor(status); // 상태 색
        timeText.SetText($"{recipe.CookingSeconds:0}s"); // 시간

        icon.sprite = result.Icon; // 아이콘
        icon.color = result.Icon != null ? new Color(1f, 1f, 1f, dim ? 0.55f : 1f) : ItemIconUtility.GetFallbackColor(result.ItemCategory); // 아이콘 색
        iconBackground.color = selected ? new Color(0.95f, 0.72f, 0.3f, 0.22f) : new Color(1f, 1f, 1f, 0.06f); // 아이콘 배경 색
        background.color = selected ? ProjectUUIPalette.PanelLight : new Color(1f, 1f, 1f, 0.035f); // 줄 배경 색
        selectionOutline.gameObject.SetActive(selected); // 선택 테두리와 강조 막대
    }

    public static string GetStatusLabel(CookingRecipeStatus status) // 상태 짧은 문구
    {
        switch (status) // 상태 분기
        {
            case CookingRecipeStatus.Ready: return "READY TO COOK"; // 조리 가능
            case CookingRecipeStatus.NoFreeSlot: return "SLOTS BUSY"; // 칸 부족
            case CookingRecipeStatus.NeedStation: return "NEED STONE CAMPFIRE"; // 시설 부족
            default: return "MISSING ITEMS"; // 재료 부족
        }
    }

    public static Color GetStatusColor(CookingRecipeStatus status) // 상태 색상
    {
        switch (status) // 상태 분기
        {
            case CookingRecipeStatus.Ready: return ProjectUUIPalette.Teal; // 청록
            case CookingRecipeStatus.NoFreeSlot: return ProjectUUIPalette.Accent; // 주황
            case CookingRecipeStatus.NeedStation: return ProjectUUIPalette.Danger; // 빨강
            default: return new Color(0.72f, 0.7f, 0.65f, 0.7f); // 흐림
        }
    }
}
