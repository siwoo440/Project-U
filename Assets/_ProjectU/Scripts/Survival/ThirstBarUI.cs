using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class ThirstBarUI : MonoBehaviour // 갈증 화면 표시
{
    [Header("References")] // UI 참조 묶음
    [SerializeField] private PlayerThirst playerThirst; // 플레이어 갈증
    [SerializeField] private Image fillImage; // 갈증 채움 이미지
    [SerializeField] private TMP_Text valueText; // 갈증 수치 Text

    private void Awake() // UI 참조 검사
    {
        if (playerThirst == null || fillImage == null || valueText == null) // 필수 참조 확인
        {
            Debug.LogError($"{gameObject.name}의 갈증 UI 참조가 누락되었습니다.", this); // 참조 오류 출력
            enabled = false; // 갈증 UI 비활성화
        }
    }

    private void Update() // 갈증 화면 갱신
    {
        fillImage.fillAmount = playerThirst.NormalizedThirst; // 갈증 비율 적용

        int currentValue = Mathf.RoundToInt(playerThirst.CurrentThirst); // 현재 갈증 정수 변환
        int maximumValue = Mathf.RoundToInt(playerThirst.MaxThirst); // 최대 갈증 정수 변환

        valueText.SetText($"THIRST {currentValue} / {maximumValue}"); // 갈증 수치 출력
    }
}