using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
[RequireComponent(typeof(PlayerHealth))] // 필수 체력 컴포넌트
[RequireComponent(typeof(PlayerEquipment))] // 필수 장비 컴포넌트
public sealed class PlayerTemperature : MonoBehaviour // 플레이어 체온 관리
{
    [Header("Temperature")] // 체온 설정 묶음
    [SerializeField][Min(1f)] private float maxTemperature = 100f; // 최대 체온 수치
    [SerializeField][Min(0f)] private float recoveryPerSecond = 0.2f; // 초당 기본 체온 회복량
    [SerializeField][Range(0f, 1f)] private float shelteredColdMultiplier = 0.35f; // 실내 추위 적용 배율
    [SerializeField][Min(0.05f)] private float heatIndicatorDuration = 0.25f; // 열기 수신 상태 유지 시간

    [Header("Season Cooling")] // 계절별 추위 설정 묶음
    [SerializeField][Min(0f)] private float springCoolingPerSecond = 0.02f; // 봄 초당 추위
    [SerializeField][Min(0f)] private float summerCoolingPerSecond = 0f; // 여름 초당 추위
    [SerializeField][Min(0f)] private float autumnCoolingPerSecond = 0.06f; // 가을 초당 추위
    [SerializeField][Min(0f)] private float winterCoolingPerSecond = 0.18f; // 겨울 초당 추위

    [Header("Weather Cooling")] // 날씨별 추위 설정 묶음
    [SerializeField][Min(0f)] private float cloudyCoolingPerSecond = 0.02f; // 흐림 초당 추위
    [SerializeField][Min(0f)] private float rainCoolingPerSecond = 0.15f; // 비 초당 추위
    [SerializeField][Min(0f)] private float snowCoolingPerSecond = 0.22f; // 눈 초당 추위
    [SerializeField][Min(0f)] private float stormCoolingPerSecond = 0.3f; // 폭풍 초당 추위
    [SerializeField][Min(0f)] private float nightCoolingPerSecond = 0.08f; // 밤 초당 추위
    [SerializeField][Min(0f)] private float maximumWetnessCoolingPerSecond = 0.35f; // 최대 젖음 초당 추위

    [Header("Cold Damage")] // 저체온 피해 설정 묶음
    [SerializeField][Range(0f, 100f)] private float coldThreshold = 50f; // 추위 진입 기준
    [SerializeField][Range(0f, 100f)] private float hypothermiaThreshold = 20f; // 저체온 진입 기준
    [SerializeField][Min(0f)] private float hypothermiaDamage = 5f; // 저체온 피해량
    [SerializeField][Min(0.1f)] private float damageInterval = 3f; // 저체온 피해 주기

    [Header("Runtime")] // 실행 상태 묶음
    [SerializeField] private float currentTemperature = 100f; // 현재 체온 수치

    private PlayerHealth playerHealth; // 플레이어 체력 관리자
    private PlayerEquipment playerEquipment; // 플레이어 장비 관리자
    private float nextDamageTime; // 다음 저체온 피해 시각
    private float lastHeatReceivedTime; // 마지막 열기 수신 시각

    public float CurrentTemperature => currentTemperature; // 현재 체온 제공
    public float MaxTemperature => maxTemperature; // 최대 체온 제공
    public float NormalizedTemperature => currentTemperature / maxTemperature; // 체온 비율 제공
    public bool IsCold => currentTemperature <= coldThreshold; // 추위 상태 제공
    public bool IsHypothermic => currentTemperature <= hypothermiaThreshold; // 저체온 상태 제공
    public bool IsReceivingHeat => Time.time - lastHeatReceivedTime <= heatIndicatorDuration; // 열기 수신 상태 제공

    private void Awake() // 체온 시스템 초기화
    {
        playerHealth = GetComponent<PlayerHealth>(); // 체력 관리자 가져오기
        playerEquipment = GetComponent<PlayerEquipment>(); // 장비 관리자 가져오기
        lastHeatReceivedTime = float.NegativeInfinity; // 시작 열기 수신 기록 제거
        ClampSettings(); // 설정값 범위 보정
        currentTemperature = maxTemperature; // 시작 체온 최대 적용
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

        float coldResistanceMultiplier = 1f
            - playerEquipment.TotalColdResistancePercent / 100f; // 방한 능력치 적용 배율 계산

        totalCooling *= coldResistanceMultiplier; // 장비 방한 효과 적용

        if (isSheltered) // 실내 상태 확인
        {
            totalCooling *= shelteredColdMultiplier; // 실내 추위 감소
        }

        float temperatureChange = recoveryPerSecond - totalCooling; // 초당 체온 변화량 계산
        currentTemperature = Mathf.Clamp(
            currentTemperature + temperatureChange * deltaTime,
            0f,
            maxTemperature); // 현재 체온 범위 적용

        UpdateHypothermiaDamage(); // 저체온 피해 처리
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

    [ContextMenu("Debug Reset Temperature")] // 체온 초기화 테스트 메뉴
    private void DebugResetTemperature() // 정상 체온 복구
    {
        currentTemperature = maxTemperature; // 최대 체온 적용
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

    private void UpdateHypothermiaDamage() // 저체온 체력 피해 처리
    {
        if (!IsHypothermic || IsReceivingHeat || playerHealth.IsDead) // 저체온과 열기 및 생존 상태 확인
        {
            nextDamageTime = Time.time + damageInterval; // 다음 피해 시각 갱신
            return; // 피해 처리 중단
        }

        if (Time.time < nextDamageTime) // 피해 주기 확인
        {
            return; // 피해 시각까지 대기
        }

        playerHealth.TakeDamage(hypothermiaDamage); // 저체온 체력 피해 적용
        nextDamageTime = Time.time + damageInterval; // 다음 피해 시각 설정
    }

    private void ClampSettings() // 체온 설정값 보정
    {
        maxTemperature = Mathf.Max(1f, maxTemperature); // 최대 체온 최소값 적용
        recoveryPerSecond = Mathf.Max(0f, recoveryPerSecond); // 회복량 음수 방지
        shelteredColdMultiplier = Mathf.Clamp01(shelteredColdMultiplier); // 실내 배율 범위 적용
        heatIndicatorDuration = Mathf.Max(0.05f, heatIndicatorDuration); // 열기 표시 시간 최소값 적용
        coldThreshold = Mathf.Clamp(coldThreshold, 0f, maxTemperature); // 추위 기준 범위 적용
        hypothermiaThreshold = Mathf.Clamp(hypothermiaThreshold, 0f, coldThreshold); // 저체온 기준 범위 적용
        hypothermiaDamage = Mathf.Max(0f, hypothermiaDamage); // 저체온 피해 음수 방지
        damageInterval = Mathf.Max(0.1f, damageInterval); // 피해 주기 최소값 적용
        currentTemperature = Mathf.Clamp(currentTemperature, 0f, maxTemperature); // 현재 체온 범위 적용
    }
}
