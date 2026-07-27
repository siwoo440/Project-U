using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class HealthBarUI : MonoBehaviour // 체력 화면 표시
{
    [Header("References")] // UI 참조 묶음
    [SerializeField] private PlayerHealth playerHealth; // 플레이어 체력
    [SerializeField] private Image fillImage; // 체력 채움 이미지
    [SerializeField] private TMP_Text valueText; // 체력 수치 Text

    private void Awake() // UI 참조 검사
    {
        if (playerHealth == null || fillImage == null || valueText == null) // 필수 참조 확인
        {
            Debug.LogError($"{gameObject.name}의 체력 UI 참조가 누락되었습니다.", this); // 참조 오류 출력
            enabled = false; // 체력 UI 비활성화
        }
    }

    private void Update() // 체력 화면 갱신
    {
        fillImage.fillAmount = playerHealth.NormalizedHealth; // 체력 비율 적용

        int currentValue = Mathf.CeilToInt(playerHealth.CurrentHealth); // 현재 체력 정수 변환
        int maximumValue = Mathf.RoundToInt(playerHealth.MaxHealth); // 최대 체력 정수 변환

        valueText.SetText($"HEALTH {currentValue} / {maximumValue}"); // 체력 수치 출력
    }
}