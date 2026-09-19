using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
[RequireComponent(typeof(CanvasGroup))] // 보이기 · 숨기기
public sealed class StaminaBarUI : MonoBehaviour // 106일차: 스태미나 화면 표시 (달리기 · 헤엄으로 줄었을 때만 보임)
{
    [Header("References")] // UI 참조 묶음
    [Tooltip("플레이어 스태미나.")]
    [SerializeField] private PlayerStamina playerStamina; // 플레이어 스태미나
    [Tooltip("스태미나 채움 이미지.")]
    [SerializeField] private Image fillImage; // 스태미나 채움 이미지
    [Tooltip("스태미나 수치 Text.")]
    [SerializeField] private TMP_Text valueText; // 스태미나 수치 Text

    private CanvasGroup canvasGroup; // 보이기 · 숨기기
    private int lastCurrentValue = int.MinValue; // 마지막 표시 현재값
    private bool lastExhausted; // 마지막 표시 지침 상태
    private Color baseFillColor; // 기본 채움 색

    public bool IsShown => canvasGroup != null && canvasGroup.alpha > 0.5f; // 보이는지 (테스트용)

    private void Awake() // UI 참조 검사
    {
        canvasGroup = GetComponent<CanvasGroup>();

        if (playerStamina == null || fillImage == null || valueText == null) // 필수 참조 확인
        {
            Debug.LogError($"{gameObject.name}의 스태미나 UI 참조가 누락되었습니다.", this); // 참조 오류 출력
            enabled = false; // 스태미나 UI 비활성화
            return;
        }

        baseFillColor = fillImage.color;
        canvasGroup.alpha = 0f;
    }

    private void Update() // 스태미나 화면 갱신
    {
        bool show = playerStamina.CurrentStamina < playerStamina.MaxStamina - 0.5f || PlayerSwimming.IsLocalSwimming; // 줄었거나 헤엄 중
        canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, show ? 1f : 0f, Time.unscaledDeltaTime * 3f);

        if (canvasGroup.alpha <= 0f)
        {
            return;
        }

        fillImage.fillAmount = playerStamina.NormalizedStamina;
        fillImage.color = playerStamina.IsExhausted ? new Color(0.6f, 0.58f, 0.52f, 1f) : baseFillColor; // 지치면 흐린 색

        int currentValue = Mathf.RoundToInt(playerStamina.CurrentStamina); // 현재 스태미나 정수 변환

        if (currentValue != lastCurrentValue || playerStamina.IsExhausted != lastExhausted) // 표시 값 변경 시에만 문자열 생성
        {
            lastCurrentValue = currentValue;
            lastExhausted = playerStamina.IsExhausted;
            valueText.SetText(playerStamina.IsExhausted ? $"STAMINA {currentValue} / {Mathf.RoundToInt(playerStamina.MaxStamina)}  TIRED" : $"STAMINA {currentValue} / {Mathf.RoundToInt(playerStamina.MaxStamina)}"); // 스태미나 수치 출력
        }
    }

#if UNITY_EDITOR
    public void EditorAssign(PlayerStamina stamina, Image fill, TMP_Text text) // 생성 도구 전용
    {
        playerStamina = stamina;
        fillImage = fill;
        valueText = text;
    }
#endif
}
