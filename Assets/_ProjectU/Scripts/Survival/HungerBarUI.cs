using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class HungerBarUI : MonoBehaviour // 허기 화면 표시
{
    [Header("References")] // UI 참조 묶음
    [SerializeField] private PlayerHunger playerHunger; // 플레이어 허기
    [SerializeField] private Image fillImage; // 허기 채움 이미지
    [SerializeField] private TMP_Text valueText; // 허기 수치 Text

    private void Awake() // UI 참조 검사
    {
        if (playerHunger == null || fillImage == null || valueText == null) // 필수 참조 확인
        {
            Debug.LogError($"{gameObject.name}의 허기 UI 참조가 누락되었습니다.", this); // 참조 오류 출력
            enabled = false; // 허기 UI 비활성화
        }
    }

    private void Update() // 허기 화면 갱신
    {
        fillImage.fillAmount = playerHunger.NormalizedHunger; // 허기 비율 적용

        int currentValue = Mathf.RoundToInt(playerHunger.CurrentHunger); // 현재 허기 정수 변환
        int maximumValue = Mathf.RoundToInt(playerHunger.MaxHunger); // 최대 허기 정수 변환

        valueText.SetText($"HUNGER {currentValue} / {maximumValue}"); // 허기 수치 출력
    }
}