using System.Collections; // 코루틴
using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class LoadingScreenUI : MonoBehaviour // 99일차: Scene 전환 로딩 화면 (Bootstrap의 AppRoot 아래, Scene이 바뀌어도 유지)
{
    [SerializeField] private GameObject panelRoot; // 화면 전체
    [SerializeField] private CanvasGroup canvasGroup; // 투명도 · 입력 차단
    [SerializeField] private RectTransform progressFill; // 진행 막대 (anchorMax.x = 진행률)
    [SerializeField] private TMP_Text statusText; // "LOADING WORLD" 등
    [SerializeField] private TMP_Text tipText; // 도움말
    [SerializeField, Min(0.05f)] private float fadeSeconds = 0.25f; // 사라지는 시간

    private static readonly string[] Tips =
    {
        "TIP  Press V to switch between first and third person.",
        "TIP  Give NPCs gifts they love - up to twice a week.",
        "TIP  Crops only grow when watered. Rain waters them for you.",
        "TIP  Put items in the shipping bin. They sell overnight.",
        "TIP  Check the village board for NPC requests.",
        "TIP  Sleep to save stamina, but keep some food and water."
    };

    private Coroutine fadeRoutine; // 사라지는 중
    private int tipIndex; // 다음 도움말

    public bool IsVisible => panelRoot != null && panelRoot.activeSelf; // 표시 여부 (테스트용)
    public float Progress { get; private set; } // 진행률 (테스트용)

    private void Awake() // 처음에는 숨김
    {
        if (panelRoot != null && fadeRoutine == null && Progress <= 0f)
        {
            panelRoot.SetActive(false);
        }
    }

    public void Show(string status) // 로딩 화면 켜기
    {
        if (panelRoot == null)
        {
            return;
        }

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        panelRoot.SetActive(true);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        if (statusText != null) statusText.text = status;
        if (tipText != null) tipText.text = Tips[tipIndex++ % Tips.Length];
        SetProgress(0f);
    }

    public void SetProgress(float value) // 진행률 (0~1)
    {
        Progress = Mathf.Clamp01(value);

        if (progressFill != null)
        {
            progressFill.anchorMax = new Vector2(Mathf.Max(0.001f, Progress), progressFill.anchorMax.y);
        }
    }

    public void Hide() // 서서히 끄기
    {
        if (panelRoot == null || !panelRoot.activeSelf)
        {
            return;
        }

        SetProgress(1f);

        if (!isActiveAndEnabled || canvasGroup == null)
        {
            panelRoot.SetActive(false);
            return;
        }

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        fadeRoutine = StartCoroutine(FadeOut());
    }

    private IEnumerator FadeOut()
    {
        canvasGroup.blocksRaycasts = false;
        float elapsed = 0f;

        while (elapsed < fadeSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeSeconds);
            yield return null;
        }

        panelRoot.SetActive(false);
        canvasGroup.alpha = 1f;
        fadeRoutine = null;
    }

#if UNITY_EDITOR
    public void EditorAssign(GameObject root, CanvasGroup group, RectTransform fill, TMP_Text status, TMP_Text tip) // 생성 도구 전용
    {
        panelRoot = root;
        canvasGroup = group;
        progressFill = fill;
        statusText = status;
        tipText = tip;
    }
#endif
}
