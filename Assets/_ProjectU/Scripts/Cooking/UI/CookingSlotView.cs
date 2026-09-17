using System; // 이벤트 기능
using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class CookingSlotView : MonoBehaviour // 85일차: 요리 창 아래 조리 칸 하나
{
    [Tooltip("칸 테두리 (완성 시 청록).")]
    [SerializeField] private Image outline; // 테두리
    [Tooltip("음식 아이콘.")]
    [SerializeField] private Image icon; // 아이콘
    [Tooltip("요리 이름.")]
    [SerializeField] private TMP_Text nameText; // 이름
    [Tooltip("상태 문구.")]
    [SerializeField] private TMP_Text statusText; // 상태
    [Tooltip("진행 막대 채움 (Filled).")]
    [SerializeField] private Image progressFill; // 진행 막대
    [Tooltip("진행 막대 배경.")]
    [SerializeField] private GameObject progressRoot; // 진행 막대 루트
    [Tooltip("꺼내기 버튼.")]
    [SerializeField] private Button takeButton; // 꺼내기 버튼
    [Tooltip("빈 칸 안내.")]
    [SerializeField] private GameObject emptyRoot; // 빈 칸 안내
    [Tooltip("내용 루트.")]
    [SerializeField] private GameObject contentRoot; // 내용 루트

    private Action<int> takeClicked; // 꺼내기 콜백
    private int slotIndex; // 칸 번호

    private void Awake() // 버튼 연결
    {
        if (takeButton != null) // 버튼 확인
        {
            takeButton.onClick.AddListener(() => takeClicked?.Invoke(slotIndex)); // 클릭 연결
        }
    }

    public void Bind(int index, CookingSlot slot, Action<int> onTake) // 표시 적용
    {
        slotIndex = index; // 번호
        takeClicked = onTake; // 콜백
        gameObject.SetActive(true); // 표시
        bool empty = slot == null || slot.IsEmpty; // 빈 칸 여부
        emptyRoot.SetActive(empty); // 빈 칸 안내
        contentRoot.SetActive(!empty); // 내용
        outline.color = empty ? new Color(1f, 1f, 1f, 0.1f) : slot.IsReady ? ProjectUUIPalette.Teal : new Color(0.95f, 0.72f, 0.3f, 0.55f); // 테두리 색

        if (empty) // 빈 칸
        {
            return; // 표시 완료
        }

        ItemData result = slot.Recipe.ResultItem; // 완성 음식
        int amount = slot.IsReady ? slot.ReadyAmount : slot.Recipe.ResultQuantity * slot.BatchCount; // 수량
        icon.sprite = result.Icon; // 아이콘
        icon.color = result.Icon != null ? Color.white : ItemIconUtility.GetFallbackColor(result.ItemCategory); // 아이콘 색
        nameText.SetText($"{result.DisplayName} x{amount}"); // 이름
        takeButton.gameObject.SetActive(slot.IsReady); // 꺼내기 버튼
        progressRoot.SetActive(!slot.IsReady); // 진행 막대

        if (slot.IsReady) // 완성
        {
            statusText.SetText("READY"); // 상태
            statusText.color = ProjectUUIPalette.Teal; // 색
            return; // 표시 완료
        }

        progressFill.fillAmount = slot.Progress01; // 진행도
        statusText.SetText($"COOKING  {FoodBuffUtility.FormatTime(slot.RemainingSeconds)}"); // 상태
        statusText.color = ProjectUUIPalette.Accent; // 색
    }
}
