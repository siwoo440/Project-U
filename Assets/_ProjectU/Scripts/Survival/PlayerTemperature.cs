using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
[RequireComponent(typeof(PlayerHealth))] // 필수 체력 컴포넌트
[RequireComponent(typeof(PlayerEquipment))] // 필수 장비 컴포넌트
public sealed class PlayerTemperature : MonoBehaviour // 플레이어 체온 관리
{
    [Header("Temperature")] // 체온 설정 묶음
    [Tooltip("최대 체감 온도.")]
    [SerializeField][Min(1f)] private float maxTemperature = 200f; // 최대 체감 온도
    [Tooltip("정상 체감 온도.")]
    [SerializeField][Min(0f)] private float normalTemperature = 100f; // 정상 체감 온도
    [Tooltip("정상 온도 복귀 속도.")]
    [SerializeField][Min(0f)] private float recoveryPerSecond = 0.2f; // 정상 온도 복귀 속도
    [Tooltip("실내 추위 적용 배율.")]
    [SerializeField][Range(0f, 1f)] private float shelteredColdMultiplier = 0.35f; // 실내 추위 적용 배율
    [Tooltip("열기 수신 상태 유지 시간.")]
    [SerializeField][Min(0.05f)] private float heatIndicatorDuration = 0.25f; // 열기 수신 상태 유지 시간

    [Header("Season Cooling")] // 계절별 추위 설정 묶음
    [Tooltip("봄 초당 추위.")]
    [SerializeField][Min(0f)] private float springCoolingPerSecond = 0.02f; // 봄 초당 추위
    [Tooltip("여름 초당 추위.")]
    [SerializeField][Min(0f)] private float summerCoolingPerSecond = 0f; // 여름 초당 추위
    [Tooltip("가을 초당 추위.")]
    [SerializeField][Min(0f)] private float autumnCoolingPerSecond = 0.06f; // 가을 초당 추위
    [Tooltip("겨울 초당 추위.")]
    [SerializeField][Min(0f)] private float winterCoolingPerSecond = 0.18f; // 겨울 초당 추위

    [Header("Weather Cooling")] // 날씨별 추위 설정 묶음
    [Tooltip("흐림 초당 추위.")]
    [SerializeField][Min(0f)] private float cloudyCoolingPerSecond = 0.02f; // 흐림 초당 추위
    [Tooltip("비 초당 추위.")]
    [SerializeField][Min(0f)] private float rainCoolingPerSecond = 0.15f; // 비 초당 추위
    [Tooltip("눈 초당 추위.")]
    [SerializeField][Min(0f)] private float snowCoolingPerSecond = 0.22f; // 눈 초당 추위
    [Tooltip("폭풍 초당 추위.")]
    [SerializeField][Min(0f)] private float stormCoolingPerSecond = 0.3f; // 폭풍 초당 추위
    [Tooltip("밤 초당 추위.")]
    [SerializeField][Min(0f)] private float nightCoolingPerSecond = 0.08f; // 밤 초당 추위
    [Tooltip("최대 젖음 초당 추위.")]
    [SerializeField][Min(0f)] private float maximumWetnessCoolingPerSecond = 0.35f; // 최대 젖음 초당 추위

    [Header("Environment Heating")] // 환경 더위 설정 묶음
    [Tooltip("여름 낮 초당 더위.")]
    [SerializeField][Min(0f)] private float summerDayHeatingPerSecond = 0.22f; // 여름 낮 초당 더위
    [Tooltip("맑은 낮 초당 더위.")]
    [SerializeField][Min(0f)] private float clearDayHeatingPerSecond = 0.12f; // 맑은 낮 초당 더위
    [Tooltip("실내 더위 적용 배율.")]
    [SerializeField][Range(0f, 1f)] private float shelteredHeatMultiplier = 0.25f; // 실내 더위 적용 배율

    [Header("Temperature States")] // 온도 상태 기준 묶음
    [Tooltip("추위 진입 기준.")]
    [SerializeField][Range(0f, 200f)] private float coldThreshold = 50f; // 추위 진입 기준
    [Tooltip("저체온 진입 기준.")]
    [SerializeField][Range(0f, 200f)] private float hypothermiaThreshold = 20f; // 저체온 진입 기준
    [Tooltip("더위 진입 기준.")]
    [SerializeField][Range(0f, 200f)] private float hotThreshold = 130f; // 더위 진입 기준
    [Tooltip("열사병 진입 기준.")]
    [SerializeField][Range(0f, 200f)] private float heatstrokeThreshold = 170f; // 열사병 진입 기준

    [Header("Temperature Damage")] // 온도 피해 설정 묶음
    [Tooltip("저체온 피해량.")]
    [SerializeField][Min(0f)] private float hypothermiaDamage = 5f; // 저체온 피해량
    [Tooltip("열사병 피해량.")]
    [SerializeField][Min(0f)] private float heatstrokeDamage = 5f; // 열사병 피해량
    [Tooltip("온도 피해 주기.")]
    [SerializeField][Min(0.1f)] private float damageInterval = 3f; // 온도 피해 주기

    [Header("Cold Effects")] // 추위 상태 효과 묶음
    [Tooltip("추위 이동 속도 배율.")]
    [SerializeField][Range(0.1f, 1f)] private float coldMovementMultiplier = 0.9f; // 추위 이동 속도 배율
    [Tooltip("저체온 이동 속도 배율.")]
    [SerializeField][Range(0.1f, 1f)] private float hypothermiaMovementMultiplier = 0.65f; // 저체온 이동 속도 배율
    [Tooltip("추위 스태미나 소비 배율.")]
    [SerializeField][Min(1f)] private float coldStaminaDrainMultiplier = 1.15f; // 추위 스태미나 소비 배율
    [Tooltip("저체온 스태미나 소비 배율.")]
    [SerializeField][Min(1f)] private float hypothermiaStaminaDrainMultiplier = 1.5f; // 저체온 스태미나 소비 배율
    [Tooltip("추위 스태미나 회복 배율.")]
    [SerializeField][Range(0f, 1f)] private float coldStaminaRecoveryMultiplier = 0.9f; // 추위 스태미나 회복 배율
    [Tooltip("저체온 스태미나 회복 배율.")]
    [SerializeField][Range(0f, 1f)] private float hypothermiaStaminaRecoveryMultiplier = 0.5f; // 저체온 스태미나 회복 배율

    [Header("Heat Effects")] // 더위 상태 효과 묶음
    [Tooltip("더위 이동 속도 배율.")]
    [SerializeField][Range(0.1f, 1f)] private float hotMovementMultiplier = 0.9f; // 더위 이동 속도 배율
    [Tooltip("열사병 이동 속도 배율.")]
    [SerializeField][Range(0.1f, 1f)] private float heatstrokeMovementMultiplier = 0.7f; // 열사병 이동 속도 배율
    [Tooltip("더위 스태미나 소비 배율.")]
    [SerializeField][Min(1f)] private float hotStaminaDrainMultiplier = 1.25f; // 더위 스태미나 소비 배율
    [Tooltip("열사병 스태미나 소비 배율.")]
    [SerializeField][Min(1f)] private float heatstrokeStaminaDrainMultiplier = 1.75f; // 열사병 스태미나 소비 배율
    [Tooltip("더위 스태미나 회복 배율.")]
    [SerializeField][Range(0f, 1f)] private float hotStaminaRecoveryMultiplier = 0.75f; // 더위 스태미나 회복 배율
    [Tooltip("열사병 스태미나 회복 배율.")]
    [SerializeField][Range(0f, 1f)] private float heatstrokeStaminaRecoveryMultiplier = 0.4f; // 열사병 스태미나 회복 배율
    [Tooltip("더위 갈증 감소 배율.")]
    [SerializeField][Min(1f)] private float hotThirstMultiplier = 1.5f; // 더위 갈증 감소 배율
    [Tooltip("열사병 갈증 감소 배율.")]
    [SerializeField][Min(1f)] private float heatstrokeThirstMultiplier = 2f; // 열사병 갈증 감소 배율

    [Header("Runtime")] // 실행 상태 묶음
    [Tooltip("현재 체온 수치.")]
    [SerializeField] private float currentTemperature = 100f; // 현재 체온 수치

    private PlayerHealth playerHealth; // 플레이어 체력 관리자
    private PlayerEquipment playerEquipment; // 플레이어 장비 관리자
    private float nextDamageTime; // 다음 저체온 피해 시각
    private float lastHeatReceivedTime; // 마지막 열기 수신 시각

    public float CurrentTemperature => currentTemperature; // 현재 체온 제공
    public float MaxTemperature => maxTemperature; // 최대 체온 제공
    public float NormalTemperature => normalTemperature; // 정상 체감 온도 제공
    public float NormalizedTemperature => currentTemperature / maxTemperature; // 체온 비율 제공
    public bool IsCold => currentTemperature <= coldThreshold; // 추위 상태 제공
    public bool IsHypothermic => currentTemperature <= hypothermiaThreshold; // 저체온 상태 제공
    public bool IsHot => currentTemperature >= hotThreshold; // 더위 상태 제공
    public bool IsHeatstroke => currentTemperature >= heatstrokeThreshold; // 열사병 상태 제공
    public bool IsReceivingHeat => Time.time - lastHeatReceivedTime <= heatIndicatorDuration; // 열기 수신 상태 제공
    public TemperatureState CurrentState => ResolveCurrentState(); // 현재 온도 상태 제공
    public float MovementSpeedMultiplier => CurrentState switch // 이동 속도 배율 제공
    {
        TemperatureState.Hypothermia => hypothermiaMovementMultiplier, // 저체온 이동 배율
        TemperatureState.Cold => coldMovementMultiplier, // 추위 이동 배율
        TemperatureState.Hot => hotMovementMultiplier, // 더위 이동 배율
        TemperatureState.Heatstroke => heatstrokeMovementMultiplier, // 열사병 이동 배율
        _ => 1f // 쾌적 이동 배율
    };
    public float StaminaDrainMultiplier => CurrentState switch // 스태미나 소비 배율 제공
    {
        TemperatureState.Hypothermia => hypothermiaStaminaDrainMultiplier, // 저체온 소비 배율
        TemperatureState.Cold => coldStaminaDrainMultiplier, // 추위 소비 배율
        TemperatureState.Hot => hotStaminaDrainMultiplier, // 더위 소비 배율
        TemperatureState.Heatstroke => heatstrokeStaminaDrainMultiplier, // 열사병 소비 배율
        _ => 1f // 쾌적 소비 배율
    };
    public float StaminaRecoveryMultiplier => CurrentState switch // 스태미나 회복 배율 제공
    {
        TemperatureState.Hypothermia => hypothermiaStaminaRecoveryMultiplier, // 저체온 회복 배율
        TemperatureState.Cold => coldStaminaRecoveryMultiplier, // 추위 회복 배율
        TemperatureState.Hot => hotStaminaRecoveryMultiplier, // 더위 회복 배율
        TemperatureState.Heatstroke => heatstrokeStaminaRecoveryMultiplier, // 열사병 회복 배율
        _ => 1f // 쾌적 회복 배율
    };
    public float ThirstDepletionMultiplier => CurrentState switch // 갈증 감소 배율 제공
    {
        TemperatureState.Hot => hotThirstMultiplier, // 더위 갈증 배율
        TemperatureState.Heatstroke => heatstrokeThirstMultiplier, // 열사병 갈증 배율
        _ => 1f // 나머지 상태 기본 배율
    };

    private void Awake() // 체온 시스템 초기화
    {
        playerHealth = GetComponent<PlayerHealth>(); // 체력 관리자 가져오기
        playerEquipment = GetComponent<PlayerEquipment>(); // 장비 관리자 가져오기
        lastHeatReceivedTime = float.NegativeInfinity; // 시작 열기 수신 기록 제거
        ClampSettings(); // 설정값 범위 보정
        currentTemperature = normalTemperature; // 시작 체온 정상값 적용
        nextDamageTime = Time.time + damageInterval; // 첫 피해 대기 시각 설정
    }

    private void OnValidate() // Inspector 설정값 검증
    {
        ClampSettings(); // 설정값 범위 보정
    }

    public void UpdateEnvironment(
        SeasonType season,
        WeatherType weather,
        bool isNight,
        bool isSheltered,
        float wetnessNormalized,
        float deltaTime) // 환경에 따른 체온 갱신
    {
        if (deltaTime <= 0f) // 정상 시간값 확인
        {
            return; // 체온 갱신 중단
        }

        float seasonCooling = GetSeasonCooling(season); // 계절 추위 계산
        float weatherCooling = GetWeatherCooling(weather); // 날씨 추위 계산
        float nightCooling = isNight ? nightCoolingPerSecond : 0f; // 밤 추위 계산
        float wetnessCooling = Mathf.Clamp01(wetnessNormalized)
            * maximumWetnessCoolingPerSecond; // 젖음 추위 계산

        float totalCooling = seasonCooling
            + weatherCooling
            + nightCooling
            + wetnessCooling; // 전체 추위 계산

        float coldResistanceMultiplier = Mathf.Clamp01(
            1f - playerEquipment.TotalColdResistancePercent / 100f); // 방한 능력치 적용 배율 계산

        totalCooling *= coldResistanceMultiplier; // 장비 방한 효과 적용

        float summerHeating = season == SeasonType.Summer && !isNight
            ? summerDayHeatingPerSecond
            : 0f; // 여름 낮 더위 계산

        float clearHeating = weather == WeatherType.Clear && !isNight
            ? clearDayHeatingPerSecond
            : 0f; // 맑은 낮 더위 계산

        float totalHeating = summerHeating + clearHeating; // 전체 환경 더위 계산

        if (isSheltered) // 실내 상태 확인
        {
            totalCooling *= shelteredColdMultiplier; // 실내 추위 감소
            totalHeating *= shelteredHeatMultiplier; // 실내 더위 감소
        }

        currentTemperature = Mathf.MoveTowards(
            currentTemperature,
            normalTemperature,
            recoveryPerSecond * deltaTime); // 정상 체온 방향 자연 회복

        float environmentChange = totalHeating - totalCooling; // 환경 체온 변화량 계산
        currentTemperature = Mathf.Clamp(
            currentTemperature + environmentChange * deltaTime,
            0f,
            maxTemperature); // 환경 체온 변화 적용

        UpdateTemperatureDamage(); // 위험 온도 피해 처리
    }

    public void SetCurrentTemperature(float temperatureAmount) // 저장 체온 수치 적용
    {
        currentTemperature = Mathf.Clamp(
            temperatureAmount,
            0f,
            maxTemperature); // 저장 체온 범위 적용

        nextDamageTime = Time.time + damageInterval; // 불러오기 직후 피해 대기
    }

    public void ReceiveHeat(float heatAmount) // 외부 열기로 체온 회복
    {
        if (heatAmount <= 0f) // 정상 열기 수치 확인
        {
            return; // 체온 회복 중단
        }

        currentTemperature = Mathf.Clamp(
            currentTemperature + heatAmount,
            0f,
            maxTemperature); // 열기 체온 회복 적용

        lastHeatReceivedTime = Time.time; // 마지막 열기 수신 시각 기록
    }

    [ContextMenu("Debug Set Cold Temperature")] // 추위 상태 테스트 메뉴
    private void DebugSetColdTemperature() // 추위 체온 강제 적용
    {
        currentTemperature = coldThreshold; // 추위 기준 체온 적용
    }

    [ContextMenu("Debug Set Hypothermia Temperature")] // 저체온 테스트 메뉴
    private void DebugSetHypothermiaTemperature() // 저체온 강제 적용
    {
        currentTemperature = hypothermiaThreshold; // 저체온 기준 체온 적용
        nextDamageTime = Time.time + damageInterval; // 첫 피해 대기 적용
    }

    [ContextMenu("Debug Set Hot Temperature")] // 더위 테스트 메뉴
    private void DebugSetHotTemperature() // 더위 체온 강제 적용
    {
        currentTemperature = hotThreshold; // 더위 기준 체온 적용
    }

    [ContextMenu("Debug Set Heatstroke Temperature")] // 열사병 테스트 메뉴
    private void DebugSetHeatstrokeTemperature() // 열사병 체온 강제 적용
    {
        currentTemperature = heatstrokeThreshold; // 열사병 기준 체온 적용
        nextDamageTime = Time.time + damageInterval; // 첫 피해 대기 적용
    }

    [ContextMenu("Debug Reset Temperature")] // 체온 초기화 테스트 메뉴
    private void DebugResetTemperature() // 정상 체온 복구
    {
        currentTemperature = normalTemperature; // 정상 체온 적용
        nextDamageTime = Time.time + damageInterval; // 피해 시각 초기화
    }

    private float GetSeasonCooling(SeasonType season) // 계절별 추위 조회
    {
        switch (season) // 현재 계절 분기
        {
            case SeasonType.Spring: // 봄 확인
                return springCoolingPerSecond; // 봄 추위 반환

            case SeasonType.Summer: // 여름 확인
                return summerCoolingPerSecond; // 여름 추위 반환

            case SeasonType.Autumn: // 가을 확인
                return autumnCoolingPerSecond; // 가을 추위 반환

            case SeasonType.Winter: // 겨울 확인
                return winterCoolingPerSecond; // 겨울 추위 반환

            default: // 미지정 계절 처리
                return 0f; // 추위 없음 반환
        }
    }

    private float GetWeatherCooling(WeatherType weather) // 날씨별 추위 조회
    {
        switch (weather) // 현재 날씨 분기
        {
            case WeatherType.Cloudy: // 흐림 확인
                return cloudyCoolingPerSecond; // 흐림 추위 반환

            case WeatherType.Rain: // 비 확인
                return rainCoolingPerSecond; // 비 추위 반환

            case WeatherType.Snow: // 눈 확인
                return snowCoolingPerSecond; // 눈 추위 반환

            case WeatherType.Storm: // 폭풍 확인
                return stormCoolingPerSecond; // 폭풍 추위 반환

            default: // 맑음 처리
                return 0f; // 추가 추위 없음 반환
        }
    }

    private TemperatureState ResolveCurrentState() // 현재 온도 상태 판정
    {
        if (IsHypothermic) // 저체온 기준 확인
        {
            return TemperatureState.Hypothermia; // 저체온 상태 반환
        }

        if (IsCold) // 추위 기준 확인
        {
            return TemperatureState.Cold; // 추위 상태 반환
        }

        if (IsHeatstroke) // 열사병 기준 확인
        {
            return TemperatureState.Heatstroke; // 열사병 상태 반환
        }

        if (IsHot) // 더위 기준 확인
        {
            return TemperatureState.Hot; // 더위 상태 반환
        }

        return TemperatureState.Comfortable; // 쾌적 상태 반환
    }

    private void UpdateTemperatureDamage() // 위험 온도 체력 피해 처리
    {
        bool hasDangerousTemperature = IsHypothermic || IsHeatstroke; // 위험 온도 여부 확인
        bool isRecoveringFromCold = IsHypothermic && IsReceivingHeat; // 저체온 열기 회복 확인

        if (!hasDangerousTemperature || isRecoveringFromCold || playerHealth.IsDead) // 피해 제외 조건 확인
        {
            nextDamageTime = Time.time + damageInterval; // 다음 피해 시각 갱신
            return; // 온도 피해 처리 중단
        }

        if (Time.time < nextDamageTime) // 피해 주기 확인
        {
            return; // 피해 시각까지 대기
        }

        float damageAmount = IsHeatstroke
            ? heatstrokeDamage
            : hypothermiaDamage; // 현재 상태 피해량 결정

        playerHealth.TakeDamage(damageAmount); // 온도 체력 피해 적용
        nextDamageTime = Time.time + damageInterval; // 다음 피해 시각 설정
    }

    private void ClampSettings() // 체온 설정값 보정
    {
        maxTemperature = Mathf.Max(1f, maxTemperature); // 최대 체온 최소값 적용
        normalTemperature = Mathf.Clamp(normalTemperature, 0f, maxTemperature); // 정상 체온 범위 적용
        recoveryPerSecond = Mathf.Max(0f, recoveryPerSecond); // 회복량 음수 방지
        shelteredColdMultiplier = Mathf.Clamp01(shelteredColdMultiplier); // 실내 배율 범위 적용
        heatIndicatorDuration = Mathf.Max(0.05f, heatIndicatorDuration); // 열기 표시 시간 최소값 적용
        summerDayHeatingPerSecond = Mathf.Max(0f, summerDayHeatingPerSecond); // 여름 더위 음수 방지
        clearDayHeatingPerSecond = Mathf.Max(0f, clearDayHeatingPerSecond); // 맑음 더위 음수 방지
        shelteredHeatMultiplier = Mathf.Clamp01(shelteredHeatMultiplier); // 실내 더위 배율 범위 적용
        coldThreshold = Mathf.Clamp(coldThreshold, 0f, normalTemperature); // 추위 기준 범위 적용
        hypothermiaThreshold = Mathf.Clamp(hypothermiaThreshold, 0f, coldThreshold); // 저체온 기준 범위 적용
        hotThreshold = Mathf.Clamp(hotThreshold, normalTemperature, maxTemperature); // 더위 기준 범위 적용
        heatstrokeThreshold = Mathf.Clamp(heatstrokeThreshold, hotThreshold, maxTemperature); // 열사병 기준 범위 적용
        hypothermiaDamage = Mathf.Max(0f, hypothermiaDamage); // 저체온 피해 음수 방지
        heatstrokeDamage = Mathf.Max(0f, heatstrokeDamage); // 열사병 피해 음수 방지
        damageInterval = Mathf.Max(0.1f, damageInterval); // 피해 주기 최소값 적용
        coldMovementMultiplier = Mathf.Clamp(coldMovementMultiplier, 0.1f, 1f); // 추위 이동 배율 범위 적용
        hypothermiaMovementMultiplier = Mathf.Clamp(hypothermiaMovementMultiplier, 0.1f, 1f); // 저체온 이동 배율 범위 적용
        coldStaminaDrainMultiplier = Mathf.Max(1f, coldStaminaDrainMultiplier); // 추위 소비 배율 최소값 적용
        hypothermiaStaminaDrainMultiplier = Mathf.Max(1f, hypothermiaStaminaDrainMultiplier); // 저체온 소비 배율 최소값 적용
        coldStaminaRecoveryMultiplier = Mathf.Clamp01(coldStaminaRecoveryMultiplier); // 추위 회복 배율 범위 적용
        hypothermiaStaminaRecoveryMultiplier = Mathf.Clamp01(hypothermiaStaminaRecoveryMultiplier); // 저체온 회복 배율 범위 적용
        hotMovementMultiplier = Mathf.Clamp(hotMovementMultiplier, 0.1f, 1f); // 더위 이동 배율 범위 적용
        heatstrokeMovementMultiplier = Mathf.Clamp(heatstrokeMovementMultiplier, 0.1f, 1f); // 열사병 이동 배율 범위 적용
        hotStaminaDrainMultiplier = Mathf.Max(1f, hotStaminaDrainMultiplier); // 더위 소비 배율 최소값 적용
        heatstrokeStaminaDrainMultiplier = Mathf.Max(1f, heatstrokeStaminaDrainMultiplier); // 열사병 소비 배율 최소값 적용
        hotStaminaRecoveryMultiplier = Mathf.Clamp01(hotStaminaRecoveryMultiplier); // 더위 회복 배율 범위 적용
        heatstrokeStaminaRecoveryMultiplier = Mathf.Clamp01(heatstrokeStaminaRecoveryMultiplier); // 열사병 회복 배율 범위 적용
        hotThirstMultiplier = Mathf.Max(1f, hotThirstMultiplier); // 더위 갈증 배율 최소값 적용
        heatstrokeThirstMultiplier = Mathf.Max(1f, heatstrokeThirstMultiplier); // 열사병 갈증 배율 최소값 적용
        currentTemperature = Mathf.Clamp(currentTemperature, 0f, maxTemperature); // 현재 체온 범위 적용
    }
}

