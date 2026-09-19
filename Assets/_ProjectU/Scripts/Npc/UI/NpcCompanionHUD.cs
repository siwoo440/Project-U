using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class NpcCompanionHUD : MonoBehaviour // 109일차: 화면 왼쪽 아래 동료 표시 (초상 · 이름 · 역할)
{
    [SerializeField] private GameObject panelRoot; // 표시 전체
    [SerializeField] private Image portrait; // 초상
    [SerializeField] private Image portraitFrame; // 초상 테두리 (대표 색)
    [SerializeField] private TMP_Text label; // 이름 · 역할

    private NpcCompanionManager manager; // 동료 관리자

    public bool IsShown => panelRoot != null && panelRoot.activeSelf; // 보이는지 (테스트용)
    public string Label => label != null ? label.text : string.Empty; // 문구 (테스트용)

    private void Start()
    {
        Refresh(null);
    }

    private void Update() // 관리자 연결 (Scene 순서와 관계없이)
    {
        if (manager != null)
        {
            return;
        }

        manager = NpcCompanionManager.Instance;

        if (manager != null)
        {
            manager.CompanionChanged += Refresh;
            Refresh(manager.Companion);
        }
    }

    private void OnDestroy()
    {
        if (manager != null)
        {
            manager.CompanionChanged -= Refresh;
        }
    }

    private void Refresh(NpcAgent companion) // 동료가 바뀌면 다시 그림
    {
        if (panelRoot == null)
        {
            return;
        }

        NpcCompanionBook.Entry entry = manager != null ? manager.CurrentEntry : null;
        bool show = companion != null && companion.Character != null && entry != null;
        panelRoot.SetActive(show);

        if (!show)
        {
            return;
        }

        if (portrait != null)
        {
            portrait.sprite = companion.Character.Portrait;
            portrait.enabled = companion.Character.Portrait != null;
        }

        if (portraitFrame != null)
        {
            portraitFrame.color = companion.Character.ThemeColor;
        }

        if (label != null)
        {
            label.text = $"동료  <b>{companion.Character.DisplayName}</b>  · {NpcCompanionBook.RoleName(entry.role)}";
            RectTransform root = (RectTransform)transform;
            float margin = label.rectTransform.offsetMin.x - label.rectTransform.offsetMax.x; // 초상 자리 + 오른쪽 여백
            root.sizeDelta = new Vector2(Mathf.Clamp(label.GetPreferredValues(label.text).x + margin + 8f, 180f, 420f), root.sizeDelta.y); // 글자 길이에 맞춤
        }
    }

#if UNITY_EDITOR
    public void EditorAssign(GameObject root, Image portraitImage, Image frame, TMP_Text text) // 생성 도구 전용
    {
        panelRoot = root;
        portrait = portraitImage;
        portraitFrame = frame;
        label = text;
    }
#endif
}
