using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 추가 방지
[RequireComponent(typeof(PlayerTemperature))] // 필수 체온 컴포넌트
public sealed class PlayerStamina : MonoBehaviour // 플레이어 스태미나 관리
{
    [Header("Stamina")] // 스태미나 설정 묶음
    [Tooltip("최대 스태미나.")]
    [SerializeField] private float maxStamina = 100f; // 최대 스태미나

    [Tooltip("초당 달리기 소비량.")]
    [SerializeField] private float sprintDrainPerSecond = 20f; // 초당 달리기 소비량

    [Tooltip("초당 회복량.")]
    [SerializeField] private float recoveryPerSecond = 15f; // 초당 회복량

    [Tooltip("회복 시작 대기 시간.")]
    [SerializeField] private float recoveryDelay = 1.5f; // 회복 시작 대기 시간

    [Tooltip("탈진 해제 필요 수치.")]
    [SerializeField] private float staminaRequiredToResume = 20f; // 탈진 해제 필요 수치

    [Header("Runtime")] // 실행 상태 확인 묶음
    [Tooltip("현재 스태미나.")]
    [SerializeField] private float currentStamina = 100f; // 현재 스태미나

    [Tooltip("현재 탈진 상태.")]
    [SerializeField] private bool isExhausted; // 현재 탈진 상태

    private float nextRecoveryTime; // 회복 시작 가능 시각
    private PlayerTemperature playerTemperature; // 플레이어 체온 관리자

    public float CurrentStamina => currentStamina; // 현재 스태미나 공개
    public float MaxStamina => maxStamina; // 최대 스태미나 공개
    public float NormalizedStamina => currentStamina / maxStamina; // 스태미나 비율 공개
    public bool CanSprint => !isExhausted && currentStamina > 0f; // 달리기 가능 여부 공개
    public bool IsExhausted => isExhausted; // 현재 탈진 상태 공개

    private void Awake() // 스태미나 실행 초기화
    {
        playerTemperature = GetComponent<PlayerTemperature>(); // 체온 관리자 가져오기
        ClampSettings(); // 설정값 정상 범위 보정
        currentStamina = maxStamina; // 시작 스태미나 최대치 적용
        isExhausted = false; // 시작 탈진 상태 해제
        nextRecoveryTime = 0f; // 시작 회복 대기 초기화
    }

    private void Reset() // 컴포넌트 최초 추가값 설정
    {
        ClampSettings(); // 설정값 정상 범위 보정
        currentStamina = maxStamina; // Inspector 시작 수치 적용
        isExhausted = false; // Inspector 탈진 상태 해제
    }

    private void OnValidate() // Inspector 설정값 검증
    {
        ClampSettings(); // 설정값 정상 범위 보정
    }

    public void SetCurrentStamina(float staminaAmount) // 불러온 현재 스태미나 적용
    {
        currentStamina = Mathf.Clamp(staminaAmount, 0f, maxStamina); // 스태미나 범위 제한
        isExhausted = currentStamina <= 0f; // 스태미나 기준 탈진 상태 적용
        nextRecoveryTime = Time.time + recoveryDelay; // 불러오기 직후 회복 대기 적용
    }

    public bool UpdateSprint(bool wantsToSprint, float deltaTime) // 달리기와 회복 상태 갱신
    {
        if (wantsToSprint && CanSprint) // 달리기 입력과 스태미나 확인
        {
            ConsumeSprintStamina(deltaTime); // 달리기 스태미나 소비
            return true; // 달리기 허용
        }

        RecoverStamina(deltaTime); // 달리지 않을 때 스태미나 회복
        return false; // 걷기 적용
    }

    public bool CanConsume(float baseAmount) // 공격과 행동 스태미나 소비 가능 여부
    {
        float finalAmount = GetFinalActionCost(baseAmount); // 체온 배율이 적용된 최종 비용 계산

        if (finalAmount <= 0f) // 비용이 없는 행동 확인
        {
            return true; // 소비 가능 반환
        }

        return !isExhausted && currentStamina >= finalAmount; // 탈진과 현재 수치 기준 소비 가능 여부 반환
    }

    public bool TryConsume(float baseAmount) // 공격과 행동 스태미나 즉시 소비 시도
    {
        float finalAmount = GetFinalActionCost(baseAmount); // 체온 배율이 적용된 최종 비용 계산

        if (finalAmount <= 0f) // 비용이 없는 행동 확인
        {
            return true; // 소비 성공 반환
        }

        if (!CanConsume(baseAmount)) // 현재 스태미나 소비 가능 여부 확인
        {
            return false; // 소비 실패 반환
        }

        currentStamina = Mathf.Max(0f, currentStamina - finalAmount); // 행동 비용만큼 스태미나 감소
        nextRecoveryTime = Time.time + recoveryDelay; // 행동 후 회복 지연 적용

        if (currentStamina <= 0f) // 스태미나 소진 확인
        {
            currentStamina = 0f; // 스태미나 0 고정
            isExhausted = true; // 탈진 상태 적용
        }

        return true; // 행동 스태미나 소비 성공 반환
    }

    private void ConsumeSprintStamina(float deltaTime) // 달리기 스태미나 소비
    {
        float temperatureDrainMultiplier = GetTemperatureDrainMultiplier(); // 온도 소비 배율 조회
        float drainAmount = sprintDrainPerSecond
            * temperatureDrainMultiplier
            * deltaTime; // 체온 적용 스태미나 소비량 계산

        currentStamina = Mathf.Max(0f, currentStamina - drainAmount); // 스태미나 최소값 제한
        nextRecoveryTime = Time.time + recoveryDelay; // 회복 시작 시각 갱신

        if (currentStamina <= 0f) // 스태미나 소진 확인
        {
            currentStamina = 0f; // 스태미나 0 고정
            isExhausted = true; // 탈진 상태 적용
        }
    }

    private void RecoverStamina(float deltaTime) // 스태미나 자동 회복
    {
        if (currentStamina >= maxStamina) // 최대 스태미나 확인
        {
            currentStamina = maxStamina; // 최대 수치 고정
            return; // 추가 회복 중단
        }

        if (Time.time < nextRecoveryTime) // 회복 지연 시간 확인
        {
            return; // 회복 처리 대기
        }

        float temperatureRecoveryMultiplier = playerTemperature == null
            ? 1f
            : playerTemperature.StaminaRecoveryMultiplier; // 온도 회복 배율 조회

        float recoveryAmount = recoveryPerSecond
            * temperatureRecoveryMultiplier
            * deltaTime; // 체온 적용 스태미나 회복량 계산

        currentStamina = Mathf.Min(maxStamina, currentStamina + recoveryAmount); // 최대 수치 제한

        if (isExhausted && currentStamina >= staminaRequiredToResume) // 탈진 해제 수치 확인
        {
            isExhausted = false; // 달리기와 행동 다시 허용
        }
    }

    private float GetFinalActionCost(float baseAmount) // 체온 배율 적용 행동 비용 계산
    {
        float safeBaseAmount = Mathf.Max(0f, baseAmount); // 기본 비용 음수 방지
        return safeBaseAmount * GetTemperatureDrainMultiplier(); // 최종 행동 비용 반환
    }

    private float GetTemperatureDrainMultiplier() // 안전한 체온 소비 배율 조회
    {
        if (playerTemperature == null) // 체온 관리자 존재 확인
        {
            return 1f; // 기본 소비 배율 반환
        }

        return Mathf.Max(0f, playerTemperature.StaminaDrainMultiplier); // 음수가 아닌 체온 배율 반환
    }

    private void ClampSettings() // 스태미나 설정값 보정
    {
        maxStamina = Mathf.Max(1f, maxStamina); // 최대 스태미나 최소값 적용
        sprintDrainPerSecond = Mathf.Max(0f, sprintDrainPerSecond); // 소비량 음수 방지
        recoveryPerSecond = Mathf.Max(0f, recoveryPerSecond); // 회복량 음수 방지
        recoveryDelay = Mathf.Max(0f, recoveryDelay); // 회복 지연 음수 방지
        staminaRequiredToResume = Mathf.Clamp(staminaRequiredToResume, 0f, maxStamina); // 탈진 해제 수치 제한
        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina); // 현재 수치 범위 제한
    }
}
