using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
[RequireComponent(typeof(CanvasGroup))] // 보이기 · 숨기기
public sealed class BreathBarUI : MonoBehaviour // 106일차: 잠수 숨 화면 표시 (숨이 가득하면 숨김)
{
    [Header("References")] // UI 참조 묶음
    [Tooltip("플레이어 수영.")]
    [SerializeField] private PlayerSwimming playerSwimming; // 플레이어 수영
    [Tooltip("숨 채움 이미지.")]
    [SerializeField] private Image fillImage; // 숨 채움 이미지
    [Tooltip("숨 수치 Text.")]
    [SerializeField] private TMP_Text valueText; // 숨 수치 Text

    private CanvasGroup canvasGroup; // 보이기 · 숨기기
    private int lastCurrentValue = int.MinValue; // 마지막 표시 현재값
    private Color baseFillColor; // 기본 채움 색

    public bool IsShown => canvasGroup != null && canvasGroup.alpha > 0.5f; // 보이는지 (테스트용)

    private void Awake() // UI 참조 검사
    {
        canvasGroup = GetComponent<CanvasGroup>();

        if (playerSwimming == null || fillImage == null || valueText == null) // 필수 참조 확인
        {
            Debug.LogError($"{gameObject.name}의 숨 UI 참조가 누락되었습니다.", this); // 참조 오류 출력
            enabled = false; // 숨 UI 비활성화
            return;
        }

        baseFillColor = fillImage.color;
        canvasGroup.alpha = 0f;
    }

    private void Update() // 숨 화면 갱신
    {
        bool show = playerSwimming.IsUnderwater || playerSwimming.Breath < playerSwimming.MaxBreath - 0.5f; // 잠수 중이거나 숨이 덜 찼을 때
        canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, show ? 1f : 0f, Time.unscaledDeltaTime * 4f);

        if (canvasGroup.alpha <= 0f)
        {
            return;
        }

        float ratio = playerSwimming.NormalizedBreath;
        fillImage.fillAmount = ratio;
        fillImage.color = ratio < 0.25f ? Color.Lerp(new Color(0.95f, 0.4f, 0.35f, 1f), baseFillColor, Mathf.PingPong(Time.time * 3f, 1f)) : baseFillColor; // 숨이 적으면 깜빡임

        int currentValue = Mathf.CeilToInt(playerSwimming.Breath); // 현재 숨 정수 변환

        if (currentValue != lastCurrentValue) // 표시 값 변경 시에만 문자열 생성
        {
            lastCurrentValue = currentValue;
            valueText.SetText($"BREATH {currentValue} / {Mathf.RoundToInt(playerSwimming.MaxBreath)}"); // 숨 수치 출력
        }
    }

#if UNITY_EDITOR
    public void EditorAssign(PlayerSwimming swimming, Image fill, TMP_Text text) // 생성 도구 전용
    {
        playerSwimming = swimming;
        fillImage = fill;
        valueText = text;
    }
#endif
}
