using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class HealthRecoveryFlashUI : MonoBehaviour // 체력 회복 화면 효과
{
    [Header("References")] // 외부 참조 묶음
    [Tooltip("플레이어 체력.")]
    [SerializeField] private PlayerHealth playerHealth; // 플레이어 체력
    [Tooltip("회복 화면 이미지.")]
    [SerializeField] private Image flashImage; // 회복 화면 이미지

    [Header("Flash")] // 회복 효과 설정 묶음
    [Tooltip("효과 지속 시간.")]
    [SerializeField] private float flashDuration = 0.35f; // 효과 지속 시간
    [Tooltip("최소 불투명도.")]
    [SerializeField] private float minimumAlpha = 0.1f; // 최소 불투명도
    [Tooltip("최대 불투명도.")]
    [SerializeField] private float maximumAlpha = 0.3f; // 최대 불투명도

    [Header("Runtime")] // 실행 상태 묶음
    [Tooltip("남은 효과 시간.")]
    [SerializeField] private float flashTimer; // 남은 효과 시간
    [Tooltip("시작 불투명도.")]
    [SerializeField] private float flashStartAlpha; // 시작 불투명도

    private void Awake() // 회복 화면 초기화
    {
        if (playerHealth == null || flashImage == null) // 필수 참조 확인
        {
            Debug.LogError($"{gameObject.name}의 회복 화면 참조가 누락되었습니다.", this); // 참조 오류 출력
            enabled = false; // 회복 화면 기능 비활성화
            return; // 초기화 처리 중단
        }

        ClampSettings(); // 설정값 범위 보정
        SetFlashAlpha(0f); // 시작 효과 숨김
    }

    private void OnEnable() // 회복 이벤트 연결
    {
        if (playerHealth == null) // 체력 참조 확인
        {
            return; // 이벤트 연결 중단
        }

        playerHealth.Healed += HandleHealed; // 회복 이벤트 구독
    }

    private void OnDisable() // 회복 이벤트 해제
    {
        if (playerHealth == null) // 체력 참조 확인
        {
            return; // 이벤트 해제 중단
        }

        playerHealth.Healed -= HandleHealed; // 회복 이벤트 구독 해제
    }

    private void Update() // 회복 화면 효과 갱신
    {
        if (flashTimer <= 0f) // 효과 종료 확인
        {
            return; // 화면 갱신 중단
        }

        flashTimer = Mathf.Max(0f, flashTimer - Time.unscaledDeltaTime); // 남은 시간 감소
        float remainingRatio = flashTimer / flashDuration; // 남은 시간 비율 계산
        float currentAlpha = flashStartAlpha * remainingRatio; // 현재 불투명도 계산

        SetFlashAlpha(currentAlpha); // 화면 불투명도 적용
    }

    private void OnValidate() // Inspector 값 검증
    {
        ClampSettings(); // 설정값 범위 보정
    }

    private void HandleHealed(float healAmount) // 회복 화면 표시
    {
        if (healAmount <= 0f) // 실제 회복량 확인
        {
            return; // 효과 표시 중단
        }

        float healRatio = Mathf.Clamp01(healAmount / playerHealth.MaxHealth); // 최대 체력 대비 회복 비율
        flashStartAlpha = Mathf.Lerp(minimumAlpha, maximumAlpha, healRatio); // 회복량별 불투명도 계산
        flashTimer = flashDuration; // 효과 시간 초기화

        SetFlashAlpha(flashStartAlpha); // 회복 화면 즉시 표시
    }

    private void SetFlashAlpha(float alpha) // 회복 화면 불투명도 적용
    {
        Color flashColor = flashImage.color; // 현재 이미지 색상 가져오기
        flashColor.a = Mathf.Clamp01(alpha); // 불투명도 범위 제한
        flashImage.color = flashColor; // 변경 색상 적용
    }

    private void ClampSettings() // 회복 화면 설정값 보정
    {
        flashDuration = Mathf.Max(0.01f, flashDuration); // 지속 시간 최소값 적용
        minimumAlpha = Mathf.Clamp01(minimumAlpha); // 최소 불투명도 범위 제한
        maximumAlpha = Mathf.Clamp(maximumAlpha, minimumAlpha, 1f); // 최대 불투명도 범위 제한
        flashTimer = Mathf.Max(0f, flashTimer); // 남은 시간 음수 방지
        flashStartAlpha = Mathf.Clamp01(flashStartAlpha); // 시작 불투명도 범위 제한
    }
}