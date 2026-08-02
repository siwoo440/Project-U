using System; // 직렬화 기능
using UnityEngine; // Unity 기본 기능

[Serializable] // ItemData 내부 배열 직렬화 허용
public sealed class MeleeAttackStepData // 근접 연속 공격 한 단계 데이터
{
    [Header("Identity")] // 공격 단계 식별 설정 묶음
    [Tooltip("Inspector와 공격 로그에 표시할 공격 단계 이름입니다.")]
    [SerializeField] private string stepName = "ATTACK"; // 공격 단계 이름

    [Header("Timing")] // 공격 단계 시간 설정 묶음
    [Tooltip("입력 후 실제 피해 판정이 시작되기 전 준비 시간입니다.")]
    [SerializeField, Min(0f)] private float windupDuration = 0.12f; // 공격 준비 시간

    [Tooltip("SphereCast 피해 판정을 반복하는 실제 타격 유효 시간입니다.")]
    [SerializeField, Min(0.01f)] private float activeDuration = 0.08f; // 공격 유효 시간

    [Tooltip("타격 유효 시간이 끝난 뒤 다음 행동까지의 복귀 시간입니다.")]
    [SerializeField, Min(0f)] private float recoveryDuration = 0.2f; // 공격 복귀 시간

    [Tooltip("공격 전체 진행도 중 다음 연속 공격 입력을 저장하기 시작할 비율입니다.")]
    [SerializeField, Range(0f, 1f)] private float inputBufferStartNormalized = 0.4f; // 연속 공격 입력 저장 시작 비율

    [Header("Stat Multipliers")] // 공격 단계별 능력치 배율 묶음
    [Tooltip("ItemData 기본 피해량에 곱할 배율입니다.")]
    [SerializeField, Min(0f)] private float damageMultiplier = 1f; // 피해량 배율

    [Tooltip("ItemData 기본 공격 거리에 곱할 배율입니다.")]
    [SerializeField, Min(0.1f)] private float rangeMultiplier = 1f; // 공격 거리 배율

    [Tooltip("ItemData 기본 공격 반지름에 곱할 배율입니다.")]
    [SerializeField, Min(0.1f)] private float radiusMultiplier = 1f; // 공격 반지름 배율

    [Tooltip("ItemData 기본 스태미나 비용에 곱할 배율입니다.")]
    [SerializeField, Min(0f)] private float staminaCostMultiplier = 1f; // 스태미나 비용 배율

    [Tooltip("ItemData 기본 충격량에 곱할 배율입니다.")]
    [SerializeField, Min(0f)] private float impactForceMultiplier = 1f; // 충격량 배율

    [Header("Hit Rules")] // 공격 단계별 명중 규칙 묶음
    [Tooltip("한 공격 단계에서 서로 다른 피해 대상을 맞힐 수 있는 최대 수입니다.")]
    [SerializeField, Range(1, 8)] private int maximumTargets = 1; // 한 단계 최대 피해 대상 수

    [Header("Visual")] // 공격 단계별 연출 설정 묶음
    [Tooltip("ToolSwingAnimation 재생 속도에 곱할 배율입니다.")]
    [SerializeField, Min(0.1f)] private float animationSpeedMultiplier = 1f; // 휘두르기 연출 속도 배율

    public string StepName => string.IsNullOrWhiteSpace(stepName) ? "ATTACK" : stepName; // 공격 단계 이름 제공
    public float WindupDuration => Mathf.Max(0f, windupDuration); // 준비 시간 제공
    public float ActiveDuration => Mathf.Max(0.01f, activeDuration); // 유효 시간 제공
    public float RecoveryDuration => Mathf.Max(0f, recoveryDuration); // 복귀 시간 제공
    public float TotalDuration => WindupDuration + ActiveDuration + RecoveryDuration; // 전체 공격 단계 시간 제공
    public float InputBufferStartNormalized => Mathf.Clamp01(inputBufferStartNormalized); // 입력 저장 시작 비율 제공
    public float DamageMultiplier => Mathf.Max(0f, damageMultiplier); // 피해량 배율 제공
    public float RangeMultiplier => Mathf.Max(0.1f, rangeMultiplier); // 공격 거리 배율 제공
    public float RadiusMultiplier => Mathf.Max(0.1f, radiusMultiplier); // 공격 반지름 배율 제공
    public float StaminaCostMultiplier => Mathf.Max(0f, staminaCostMultiplier); // 스태미나 비용 배율 제공
    public float ImpactForceMultiplier => Mathf.Max(0f, impactForceMultiplier); // 충격량 배율 제공
    public int MaximumTargets => Mathf.Clamp(maximumTargets, 1, 8); // 최대 피해 대상 수 제공
    public float AnimationSpeedMultiplier => Mathf.Max(0.1f, animationSpeedMultiplier); // 휘두르기 연출 속도 제공

    public static MeleeAttackStepData CreateFallback(float totalDuration) // 연속 공격 데이터가 없을 때 단일 공격 단계 생성
    {
        float safeDuration = Mathf.Max(0.05f, totalDuration); // 최소 전체 공격 시간 계산
        MeleeAttackStepData fallbackStep = new MeleeAttackStepData(); // 기본 공격 단계 생성
        fallbackStep.stepName = "BASIC ATTACK"; // 기본 공격 단계 이름 설정
        fallbackStep.windupDuration = safeDuration * 0.3f; // 전체 시간의 30퍼센트를 준비 시간으로 설정
        fallbackStep.activeDuration = Mathf.Max(0.05f, safeDuration * 0.2f); // 전체 시간의 20퍼센트를 유효 시간으로 설정
        fallbackStep.recoveryDuration = Mathf.Max(
            0f,
            safeDuration - fallbackStep.windupDuration - fallbackStep.activeDuration); // 남은 시간을 복귀 시간으로 설정
        fallbackStep.inputBufferStartNormalized = 1f; // 단일 공격은 연속 입력 저장 비활성화
        fallbackStep.maximumTargets = 1; // 기본 공격은 한 대상만 피해 적용
        return fallbackStep; // 생성된 기본 공격 단계 반환
    }

    public void ValidateValues(int stepIndex) // MeleeComboData에서 배열 요소 값 검증
    {
        stepName = string.IsNullOrWhiteSpace(stepName)
            ? $"ATTACK {stepIndex + 1}"
            : stepName.Trim(); // 빈 공격 단계 이름 자동 생성

        windupDuration = Mathf.Max(0f, windupDuration); // 준비 시간 음수 방지
        activeDuration = Mathf.Max(0.01f, activeDuration); // 유효 시간 최소값 적용
        recoveryDuration = Mathf.Max(0f, recoveryDuration); // 복귀 시간 음수 방지
        inputBufferStartNormalized = Mathf.Clamp01(inputBufferStartNormalized); // 입력 저장 비율 범위 제한
        damageMultiplier = Mathf.Max(0f, damageMultiplier); // 피해량 배율 음수 방지
        rangeMultiplier = Mathf.Max(0.1f, rangeMultiplier); // 거리 배율 최소값 적용
        radiusMultiplier = Mathf.Max(0.1f, radiusMultiplier); // 반지름 배율 최소값 적용
        staminaCostMultiplier = Mathf.Max(0f, staminaCostMultiplier); // 스태미나 배율 음수 방지
        impactForceMultiplier = Mathf.Max(0f, impactForceMultiplier); // 충격량 배율 음수 방지
        maximumTargets = Mathf.Clamp(maximumTargets, 1, 8); // 최대 대상 수 범위 제한
        animationSpeedMultiplier = Mathf.Max(0.1f, animationSpeedMultiplier); // 연출 속도 최소값 적용
    }
}
