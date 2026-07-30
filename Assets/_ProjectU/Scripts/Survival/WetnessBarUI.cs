using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class WetnessBarUI : MonoBehaviour // 젖음 수치 화면 표시
{
    [Header("References")] // UI 참조 묶음
    [SerializeField] private PlayerWetness playerWetness; // 플레이어 젖음 관리자
    [SerializeField] private Image fillImage; // 젖음 채움 이미지
    [SerializeField] private TMP_Text valueText; // 젖음 수치 문구

    private void Awake() // UI 참조 검사
    {
        if (playerWetness == null || fillImage == null || valueText == null) // 필수 참조 확인
        {
            Debug.LogError($"{gameObject.name}의 젖음 UI 참조가 누락되었습니다.", this); // 참조 오류 출력
            enabled = false; // 젖음 UI 비활성화
        }
    }

    private void Update() // 젖음 화면 갱신
    {
        fillImage.fillAmount = playerWetness.NormalizedWetness; // 젖음 비율 적용

        int currentValue = Mathf.RoundToInt(playerWetness.CurrentWetness); // 현재 젖음 정수 변환
        int maximumValue = Mathf.RoundToInt(playerWetness.MaxWetness); // 최대 젖음 정수 변환

        valueText.SetText($"WETNESS {currentValue} / {maximumValue}"); // 젖음 수치 출력
    }
}