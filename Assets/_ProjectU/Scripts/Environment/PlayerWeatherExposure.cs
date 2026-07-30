using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
[RequireComponent(typeof(PlayerWetness))] // 필수 젖음 컴포넌트
[RequireComponent(typeof(PlayerTemperature))] // 필수 체온 컴포넌트

public sealed class PlayerWeatherExposure : MonoBehaviour // 플레이어 날씨 노출 판정
{
    [Header("References")] // 필수 참조 묶음
    [SerializeField] private WeatherCycle weatherCycle; // 현재 날씨 관리자
    [SerializeField] private SeasonCycle seasonCycle; // 현재 계절 관리자
    [SerializeField] private DayNightCycle dayNightCycle; // 날짜와 시간 관리자
    [SerializeField] private PlayerTemperature playerTemperature; // 플레이어 체온 관리자
    [SerializeField] private WeatherEffectsController weatherEffectsController; // 날씨 효과 관리자
    [SerializeField] private PlayerWetness playerWetness; // 플레이어 젖음 관리자
    [SerializeField] private TMP_Text exposureText; // 노출 상태 표시 문구

    [Header("Shelter Detection")] // 지붕 판정 설정 묶음
    [SerializeField] private LayerMask shelterMask; // 지붕 판정 Layer
    [SerializeField][Min(0f)] private float rayOriginHeight = 1.6f; // Ray 시작 높이
    [SerializeField][Min(0.1f)] private float shelterCheckDistance = 20f; // 지붕 검사 거리
    [SerializeField][Min(0.05f)] private float shelterCheckInterval = 0.2f; // 지붕 검사 주기

    [Header("Runtime")] // 실행 상태 묶음
    [SerializeField] private bool isSheltered; // 현재 지붕 아래 상태

    private float nextShelterCheckTime; // 다음 지붕 검사 시각
    private bool hasCheckedShelter; // 최초 지붕 검사 완료 여부

    public bool IsSheltered => isSheltered; // 지붕 아래 상태 제공

    private void Awake() // 날씨 노출 참조 초기화
    {
        if (playerWetness == null) // 젖음 참조 확인
        {
            playerWetness = GetComponent<PlayerWetness>(); // 같은 오브젝트에서 젖음 관리자 가져오기
        }

        if (playerTemperature == null) // 체온 참조 확인
        {
            playerTemperature = GetComponent<PlayerTemperature>(); // 같은 오브젝트에서 체온 관리자 가져오기
        }

        if (weatherCycle == null || seasonCycle == null || dayNightCycle == null
            || weatherEffectsController == null || playerWetness == null
            || playerTemperature == null || exposureText == null) // 필수 참조 확인
        {
            Debug.LogError($"{gameObject.name}의 날씨 노출 참조가 누락되었습니다.", this); // 참조 누락 오류
            enabled = false; // 날씨 노출 기능 비활성화
        }
    }

    private void Start() // 시작 지붕 상태 검사
    {
        if (!enabled) // 컴포넌트 활성 상태 확인
        {
            return; // 시작 처리 중단
        }

        CheckShelter(); // 최초 지붕 검사
    }

    private void Update() // 지붕과 젖음 상태 갱신
    {
        if (Time.time >= nextShelterCheckTime) // 다음 검사 시각 확인
        {
            CheckShelter(); // 현재 지붕 상태 검사
            nextShelterCheckTime = Time.time + shelterCheckInterval; // 다음 검사 시각 설정
        }

        playerWetness.UpdateWeatherExposure(weatherCycle.CurrentWeather, isSheltered, Time.deltaTime); // 현재 날씨 노출 적용

        playerTemperature.UpdateEnvironment(
            seasonCycle.CurrentSeason,
            weatherCycle.CurrentWeather,
            dayNightCycle.IsNight,
            isSheltered,
            playerWetness.NormalizedWetness,
            Time.deltaTime); // 현재 환경에 따른 체온 갱신
    }

    private void OnValidate() // Inspector 설정값 검증
    {
        rayOriginHeight = Mathf.Max(0f, rayOriginHeight); // Ray 시작 높이 보정
        shelterCheckDistance = Mathf.Max(0.1f, shelterCheckDistance); // 검사 거리 보정
        shelterCheckInterval = Mathf.Max(0.05f, shelterCheckInterval); // 검사 주기 보정
    }

    private void CheckShelter() // 플레이어 위쪽 지붕 검사
    {
        Vector3 rayOrigin = transform.position + Vector3.up * rayOriginHeight; // Ray 시작 위치 계산
        bool detectedShelter = Physics.Raycast(rayOrigin, Vector3.up, shelterCheckDistance, shelterMask, QueryTriggerInteraction.Ignore); // 위쪽 Collider 검사

        if (hasCheckedShelter && detectedShelter == isSheltered) // 기존 상태와 동일한지 확인
        {
            return; // 중복 상태 적용 방지
        }

        isSheltered = detectedShelter; // 현재 지붕 상태 적용
        hasCheckedShelter = true; // 최초 검사 완료 처리
        weatherEffectsController.SetPlayerSheltered(isSheltered); // 날씨 효과에 지붕 상태 전달
        RefreshExposureText(); // 노출 상태 문구 갱신
    }

    private void RefreshExposureText() // 노출 상태 문구 갱신
    {
        exposureText.text = isSheltered ? "SHELTERED" : "OUTDOOR"; // 현재 노출 상태 표시
    }
}