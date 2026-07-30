using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class TemperatureBarUI : MonoBehaviour // 체온 화면 표시
{
    [Header("References")] // UI 참조 묶음
    [SerializeField] private PlayerTemperature playerTemperature; // 플레이어 체온 관리자
    [SerializeField] private Image fillImage; // 체온 채움 이미지
    [SerializeField] private TMP_Text valueText; // 체온 수치 문구

    [Header("Colors")] // 상태별 색상 묶음
    [SerializeField] private Color heatingColor = new Color(1f, 0.3f, 0.1f); // 열기 회복 색상
    [SerializeField] private Color warmColor = new Color(1f, 0.65f, 0.15f); // 정상 체온 색상
    [SerializeField] private Color coldColor = new Color(0.3f, 0.8f, 1f); // 추위 색상
    [SerializeField] private Color hypothermiaColor = new Color(0.1f, 0.3f, 1f); // 저체온 색상
    [SerializeField] private Color hotColor = new Color(1f, 0.35f, 0.05f); // 더위 상태 색상
    [SerializeField] private Color heatstrokeColor = new Color(0.85f, 0.05f, 0.02f); // 열사병 상태 색상

    private void Awake() // 체온 UI 참조 검사
    {
        if (playerTemperature == null || fillImage == null || valueText == null) // 필수 참조 확인
        {
            Debug.LogError($"{gameObject.name}의 체온 UI 참조가 누락되었습니다.", this); // 참조 오류 출력
            enabled = false; // 체온 UI 비활성화
        }
    }

    private void Update() // 체온 화면 갱신
    {
        fillImage.fillAmount = playerTemperature.NormalizedTemperature; // 체온 비율 적용

        int currentValue = Mathf.RoundToInt(playerTemperature.CurrentTemperature); // 현재 체온 정수 변환
        int maximumValue = Mathf.RoundToInt(playerTemperature.MaxTemperature); // 최대 체온 정수 변환
        string stateText = GetStateText(); // 현재 체온 상태 문구 조회

        valueText.SetText($"TEMP {currentValue} / {maximumValue}  {stateText}"); // 체온 수치 출력
        fillImage.color = GetStateColor(); // 현재 상태 색상 적용
    }

    private string GetStateText() // 체온 상태 문구 조회
    {
        if (playerTemperature.IsHeatstroke) // 열사병 상태 확인
        {
            return "HEATSTROKE"; // 열사병 문구 반환
        }

        if (playerTemperature.IsHot) // 더위 상태 확인
        {
            return "HOT"; // 더위 문구 반환
        }

        if (playerTemperature.IsReceivingHeat) // 열기 수신 상태 확인
        {
            return "WARMING"; // 체온 회복 문구 반환
        }

        if (playerTemperature.IsHypothermic) // 저체온 상태 확인
        {
            return "HYPOTHERMIA"; // 저체온 문구 반환
        }

        if (playerTemperature.IsCold) // 추위 상태 확인
        {
            return "COLD"; // 추위 문구 반환
        }

        return "COMFORTABLE"; // 쾌적 문구 반환
    }

    private Color GetStateColor() // 체온 상태 색상 조회
    {
        if (playerTemperature.IsHeatstroke) // 열사병 상태 확인
        {
            return heatstrokeColor; // 열사병 색상 반환
        }

        if (playerTemperature.IsHot) // 더위 상태 확인
        {
            return hotColor; // 더위 색상 반환
        }

        if (playerTemperature.IsReceivingHeat) // 열기 수신 상태 확인
        {
            return heatingColor; // 열기 회복 색상 반환
        }

        if (playerTemperature.IsHypothermic) // 저체온 상태 확인
        {
            return hypothermiaColor; // 저체온 색상 반환
        }

        if (playerTemperature.IsCold) // 추위 상태 확인
        {
            return coldColor; // 추위 색상 반환
        }

        return warmColor; // 쾌적 색상 반환
    }
}

