using System; // 이벤트 기능
using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class WeatherCycle : MonoBehaviour // 기본 날씨 순환 관리
{
    [Header("References")] // 참조 설정 묶음
    [SerializeField] private DayNightCycle dayNightCycle; // 날짜와 시간 관리자
    [SerializeField] private SeasonCycle seasonCycle; // 현재 계절 관리자
    [SerializeField] private TMP_Text weatherText; // 날씨 표시 텍스트

    [Header("Clear Duration")] // 맑음 지속 시간 묶음
    [SerializeField][Min(0.1f)] private float clearMinHours = 6f; // 맑음 최소 지속 시간
    [SerializeField][Min(0.1f)] private float clearMaxHours = 10f; // 맑음 최대 지속 시간

    [Header("Cloudy Duration")] // 흐림 지속 시간 묶음
    [SerializeField][Min(0.1f)] private float cloudyMinHours = 4f; // 흐림 최소 지속 시간
    [SerializeField][Min(0.1f)] private float cloudyMaxHours = 8f; // 흐림 최대 지속 시간

    [Header("Rain Duration")] // 비 지속 시간 묶음
    [SerializeField][Min(0.1f)] private float rainMinHours = 3f; // 비 최소 지속 시간
    [SerializeField][Min(0.1f)] private float rainMaxHours = 6f; // 비 최대 지속 시간

    [Header("Snow Duration")] // 눈 지속 시간 묶음
    [SerializeField][Min(0.1f)] private float snowMinHours = 4f; // 눈 최소 지속 시간
    [SerializeField][Min(0.1f)] private float snowMaxHours = 8f; // 눈 최대 지속 시간

    [Header("Storm Duration")] // 폭풍 지속 시간 묶음
    [SerializeField][Min(0.1f)] private float stormMinHours = 2f; // 폭풍 최소 지속 시간
    [SerializeField][Min(0.1f)] private float stormMaxHours = 4f; // 폭풍 최대 지속 시간

    [Header("Debug")] // 테스트 설정 묶음
    [SerializeField] private bool logWeatherChanges = true; // 날씨 변경 로그 사용 여부

    [Header("Runtime")] // 실행 상태 묶음
    [SerializeField] private WeatherType currentWeather; // 현재 날씨
    [SerializeField] private float remainingWeatherHours; // 남은 날씨 시간

    private float lastObservedTotalHours; // 마지막으로 확인한 전체 게임 시간
    private bool isInitialized; // 날씨 초기화 완료 여부

    public WeatherType CurrentWeather => currentWeather; // 현재 날씨 제공
    public float RemainingWeatherHours => remainingWeatherHours; // 남은 날씨 시간 제공
    public event Action<WeatherType> WeatherChanged; // 날씨 변경 이벤트

    private void Awake() // 날씨 시스템 참조 초기화
    {
        ClampSettings(); // 지속 시간 설정값 보정

        if (dayNightCycle == null || seasonCycle == null || weatherText == null) // 필수 참조 확인
        {
            Debug.LogError($"{gameObject.name}의 날씨 시스템 참조가 누락되었습니다.", this); // 참조 누락 오류
            enabled = false; // 날씨 시스템 비활성화
        }
    }

    private void Start() // 첫 날씨 상태 생성
    {
        if (!enabled) // 컴포넌트 활성 상태 확인
        {
            return; // 초기화 중단
        }

        lastObservedTotalHours = GetTotalGameHours(); // 시작 시점 전체 시간 저장
        SelectNextWeather(); // 현재 계절 기준 첫 날씨 선택
        isInitialized = true; // 초기화 완료 처리
    }

    private void Update() // 게임 시간에 따른 날씨 진행
    {
        if (!isInitialized) // 초기화 여부 확인
        {
            return; // 날씨 진행 중단
        }

        float currentTotalHours = GetTotalGameHours(); // 현재 전체 게임 시간 계산
        float elapsedHours = currentTotalHours - lastObservedTotalHours; // 지난 게임 시간 계산
        lastObservedTotalHours = currentTotalHours; // 마지막 확인 시간 갱신

        if (elapsedHours > 0f) // 시간이 앞으로 진행됐는지 확인
        {
            ConsumeElapsedHours(elapsedHours); // 지난 시간만큼 날씨 시간 감소
        }

        RefreshWeatherText(); // 날씨 HUD 갱신
    }

    private void OnValidate() // Inspector 설정값 검증
    {
        ClampSettings(); // 지속 시간 설정값 보정
    }

    [ContextMenu("Force Next Weather")] // Inspector 날씨 강제 변경 메뉴
    public void ForceWeather(WeatherType weather) // 테스트용 특정 날씨 적용
    {
        if (!Application.isPlaying) // Play Mode 여부 확인
        {
            Debug.LogWarning("날씨 변경 테스트는 Play Mode에서 실행해야 합니다.", this); // 실행 상태 안내
            return; // 테스트 중단
        }

        ApplyWeather(weather); // 지정한 날씨 적용
        lastObservedTotalHours = GetTotalGameHours(); // 시간 비교 기준 갱신
        RefreshWeatherText(); // 날씨 HUD 즉시 갱신
    }

    [ContextMenu("Force Clear Weather")] // 맑음 강제 변경 메뉴
    private void ForceClearWeather() // 맑음 테스트 실행
    {
        ForceWeather(WeatherType.Clear); // 맑음 강제 적용
    }

    [ContextMenu("Force Cloudy Weather")] // 흐림 강제 변경 메뉴
    private void ForceCloudyWeather() // 흐림 테스트 실행
    {
        ForceWeather(WeatherType.Cloudy); // 흐림 강제 적용
    }

    [ContextMenu("Force Rain Weather")] // 비 강제 변경 메뉴
    private void ForceRainWeather() // 비 테스트 실행
    {
        ForceWeather(WeatherType.Rain); // 비 강제 적용
    }

    [ContextMenu("Force Snow Weather")] // 눈 강제 변경 메뉴
    private void ForceSnowWeather() // 눈 테스트 실행
    {
        ForceWeather(WeatherType.Snow); // 눈 강제 적용
    }

    [ContextMenu("Force Storm Weather")] // 폭풍 강제 변경 메뉴
    private void ForceStormWeather() // 폭풍 테스트 실행
    {
        ForceWeather(WeatherType.Storm); // 폭풍 강제 적용
    }

    private void ConsumeElapsedHours(float elapsedHours) // 지난 게임 시간 처리
    {
        float unprocessedHours = elapsedHours; // 아직 처리하지 않은 시간 저장

        while (unprocessedHours > 0f) // 남은 시간 처리 반복
        {
            if (unprocessedHours < remainingWeatherHours) // 현재 날씨가 유지되는지 확인
            {
                remainingWeatherHours -= unprocessedHours; // 현재 날씨 남은 시간 감소
                unprocessedHours = 0f; // 전체 지난 시간 처리 완료
                continue; // 반복 조건 다시 확인
            }

            unprocessedHours -= remainingWeatherHours; // 현재 날씨 종료까지의 시간 차감
            SelectNextWeather(); // 다음 날씨 선택
        }
    }

    private void SelectNextWeather() // 현재 계절에 맞는 날씨 선택
    {
        ApplyWeather(RollWeather(seasonCycle.CurrentSeason)); // 계절별 무작위 날씨 적용
    }
    private void ApplyWeather(WeatherType weather) // 선택된 날씨 상태 적용
    {
        currentWeather = weather; // 현재 날씨 적용
        remainingWeatherHours = GetRandomDuration(currentWeather); // 날씨 지속 시간 결정
        WeatherChanged?.Invoke(currentWeather); // 날씨 변경 이벤트 전달

        if (logWeatherChanges) // 변경 로그 사용 여부 확인
        {
            Debug.Log($"날씨 변경: {currentWeather} / 지속 시간: {remainingWeatherHours:F1}시간", this); // 날씨 변경 결과 출력
        }
    }
    private WeatherType RollWeather(SeasonType season) // 계절별 날씨 확률 계산
    {
        float randomValue = UnityEngine.Random.Range(0f, 100f); // 백분율 무작위 값 생성

        switch (season) // 현재 계절 비교
        {
            case SeasonType.Spring: // 봄 날씨 확률
                if (randomValue < 30f) // 맑음 30퍼센트 확인
                {
                    return WeatherType.Clear; // 맑음 반환
                }

                if (randomValue < 55f) // 흐림 누적 55퍼센트 확인
                {
                    return WeatherType.Cloudy; // 흐림 반환
                }

                if (randomValue < 90f) // 비 누적 90퍼센트 확인
                {
                    return WeatherType.Rain; // 비 반환
                }

                return WeatherType.Storm; // 폭풍 10퍼센트 반환

            case SeasonType.Summer: // 여름 날씨 확률
                if (randomValue < 50f) // 맑음 50퍼센트 확인
                {
                    return WeatherType.Clear; // 맑음 반환
                }

                if (randomValue < 70f) // 흐림 누적 70퍼센트 확인
                {
                    return WeatherType.Cloudy; // 흐림 반환
                }

                if (randomValue < 90f) // 비 누적 90퍼센트 확인
                {
                    return WeatherType.Rain; // 비 반환
                }

                return WeatherType.Storm; // 폭풍 10퍼센트 반환

            case SeasonType.Autumn: // 가을 날씨 확률
                if (randomValue < 30f) // 맑음 30퍼센트 확인
                {
                    return WeatherType.Clear; // 맑음 반환
                }

                if (randomValue < 60f) // 흐림 누적 60퍼센트 확인
                {
                    return WeatherType.Cloudy; // 흐림 반환
                }

                if (randomValue < 90f) // 비 누적 90퍼센트 확인
                {
                    return WeatherType.Rain; // 비 반환
                }

                return WeatherType.Storm; // 폭풍 10퍼센트 반환

            case SeasonType.Winter: // 겨울 날씨 확률
                if (randomValue < 20f) // 맑음 20퍼센트 확인
                {
                    return WeatherType.Clear; // 맑음 반환
                }

                if (randomValue < 45f) // 흐림 누적 45퍼센트 확인
                {
                    return WeatherType.Cloudy; // 흐림 반환
                }

                if (randomValue < 55f) // 비 누적 55퍼센트 확인
                {
                    return WeatherType.Rain; // 비 반환
                }

                if (randomValue < 90f) // 눈 누적 90퍼센트 확인
                {
                    return WeatherType.Snow; // 눈 반환
                }

                return WeatherType.Storm; // 폭풍 10퍼센트 반환

            default: // 잘못된 계절 처리
                return WeatherType.Clear; // 기본 맑음 반환
        }
    }

    private float GetRandomDuration(WeatherType weather) // 날씨별 지속 시간 생성
    {
        switch (weather) // 선택된 날씨 비교
        {
            case WeatherType.Clear: // 맑음 확인
                return UnityEngine.Random.Range(clearMinHours, clearMaxHours); // 맑음 지속 시간 반환

            case WeatherType.Cloudy: // 흐림 확인
                return UnityEngine.Random.Range(cloudyMinHours, cloudyMaxHours); // 흐림 지속 시간 반환

            case WeatherType.Rain: // 비 확인
                return UnityEngine.Random.Range(rainMinHours, rainMaxHours); // 비 지속 시간 반환

            case WeatherType.Snow: // 눈 확인
                return UnityEngine.Random.Range(snowMinHours, snowMaxHours); // 눈 지속 시간 반환

            case WeatherType.Storm: // 폭풍 확인
                return UnityEngine.Random.Range(stormMinHours, stormMaxHours); // 폭풍 지속 시간 반환

            default: // 잘못된 날씨 처리
                return clearMinHours; // 기본 지속 시간 반환
        }
    }

    private float GetTotalGameHours() // 날짜와 시간을 전체 시간으로 변환
    {
        int completedDays = Mathf.Max(0, dayNightCycle.CurrentDay - 1); // 완료된 날짜 수 계산
        return completedDays * 24f + dayNightCycle.CurrentHour; // 전체 누적 게임 시간 반환
    }

    private void RefreshWeatherText() // 날씨와 남은 시간 HUD 갱신
    {
        int totalMinutes = Mathf.CeilToInt(Mathf.Max(0f, remainingWeatherHours) * 60f); // 남은 시간을 전체 분으로 변환
        int displayHours = totalMinutes / 60; // 화면 표시 시간 계산
        int displayMinutes = totalMinutes % 60; // 화면 표시 분 계산
        weatherText.text = $"{GetWeatherLabel()}  {displayHours:00}:{displayMinutes:00}"; // 날씨와 남은 시간 표시
    }

    private string GetWeatherLabel() // 현재 날씨 영문 이름 반환
    {
        switch (currentWeather) // 현재 날씨 비교
        {
            case WeatherType.Clear: // 맑음 확인
                return "CLEAR"; // 맑음 문구 반환

            case WeatherType.Cloudy: // 흐림 확인
                return "CLOUDY"; // 흐림 문구 반환

            case WeatherType.Rain: // 비 확인
                return "RAIN"; // 비 문구 반환

            case WeatherType.Snow: // 눈 확인
                return "SNOW"; // 눈 문구 반환

            case WeatherType.Storm: // 폭풍 확인
                return "STORM"; // 폭풍 문구 반환

            default: // 잘못된 날씨 처리
                return "UNKNOWN"; // 알 수 없는 날씨 반환
        }
    }

    private void ClampSettings() // 날씨 지속 시간 설정값 보정
    {
        clearMinHours = Mathf.Max(0.1f, clearMinHours); // 맑음 최소 시간 보정
        clearMaxHours = Mathf.Max(clearMinHours, clearMaxHours); // 맑음 최대 시간 보정
        cloudyMinHours = Mathf.Max(0.1f, cloudyMinHours); // 흐림 최소 시간 보정
        cloudyMaxHours = Mathf.Max(cloudyMinHours, cloudyMaxHours); // 흐림 최대 시간 보정
        rainMinHours = Mathf.Max(0.1f, rainMinHours); // 비 최소 시간 보정
        rainMaxHours = Mathf.Max(rainMinHours, rainMaxHours); // 비 최대 시간 보정
        snowMinHours = Mathf.Max(0.1f, snowMinHours); // 눈 최소 시간 보정
        snowMaxHours = Mathf.Max(snowMinHours, snowMaxHours); // 눈 최대 시간 보정
        stormMinHours = Mathf.Max(0.1f, stormMinHours); // 폭풍 최소 시간 보정
        stormMaxHours = Mathf.Max(stormMinHours, stormMaxHours); // 폭풍 최대 시간 보정
    }
}
