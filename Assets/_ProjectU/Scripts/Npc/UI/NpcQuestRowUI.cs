using System; // Action
using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class NpcQuestRowUI : MonoBehaviour // 93일차: 게시판 왼쪽 목록 한 줄 (초상 · 제목 · 의뢰인 · 상태 표시)
{
    [SerializeField] private Button button; // 누르기
    [SerializeField] private Image background; // 배경
    [SerializeField] private Image selectionOutline; // 선택 테두리
    [SerializeField] private Image portrait; // 의뢰인 초상
    [SerializeField] private TMP_Text titleText; // 제목
    [SerializeField] private TMP_Text subtitleText; // 의뢰인 · 기한
    [SerializeField] private Image tagBackground; // 상태 표시 배경
    [SerializeField] private TMP_Text tagText; // 상태 표시 (특별 · 진행 중 · 전달 가능)

    private Action<int> clicked; // 눌렀을 때
    private int rowIndex; // 줄 번호

    public string TitleLabel => titleText != null ? titleText.text : string.Empty; // 제목 (테스트용)
    public string TagLabel => tagText != null && tagBackground != null && tagBackground.gameObject.activeSelf ? tagText.text : string.Empty; // 상태 (테스트용)

    private void Awake() // 버튼 연결
    {
        if (button != null)
        {
            button.onClick.AddListener(() => clicked?.Invoke(rowIndex));
        }
    }

    public void Bind(int index, Sprite face, string title, string subtitle, string tag, Color tagColor, bool selected, Action<int> onClick) // 표시
    {
        rowIndex = index;
        clicked = onClick;
        gameObject.SetActive(true);
        portrait.sprite = face;
        portrait.enabled = face != null;
        titleText.text = title;
        subtitleText.text = subtitle;
        bool hasTag = !string.IsNullOrEmpty(tag);
        tagBackground.gameObject.SetActive(hasTag);
        tagText.text = tag ?? string.Empty;
        tagBackground.color = new Color(tagColor.r, tagColor.g, tagColor.b, 0.9f);
        background.color = selected ? ProjectUUIPalette.PanelLight : new Color(1f, 1f, 1f, 0.035f);
        selectionOutline.gameObject.SetActive(selected);
    }

    public void Press() // 테스트용 누르기
    {
        clicked?.Invoke(rowIndex);
    }
}
