using System.Collections; // 코루틴 기능
using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class EnvironmentSliceTestController : MonoBehaviour // 환경 생존 통합 테스트 관리
{
    [Header("References")] // 테스트 참조 묶음
    [Tooltip("날짜와 시간 시스템.")]
    [SerializeField] private DayNightCycle dayNightCycle; // 날짜와 시간 시스템
    [Tooltip("계절 시스템.")]
    [SerializeField] private SeasonCycle seasonCycle; // 계절 시스템
    [Tooltip("날씨 시스템.")]
    [SerializeField] private WeatherCycle weatherCycle; // 날씨 시스템
    [Tooltip("플레이어 젖음 시스템.")]
    [SerializeField] private PlayerWetness playerWetness; // 플레이어 젖음 시스템
    [Tooltip("플레이어 체온 시스템.")]
    [SerializeField] private PlayerTemperature playerTemperature; // 플레이어 체온 시스템
    [Tooltip("플레이어 체력 시스템.")]
    [SerializeField] private PlayerHealth playerHealth; // 플레이어 체력 시스템
    [Tooltip("게임 저장 시스템.")]
    [SerializeField] private GameplaySaveController gameplaySaveController; // 게임 저장 시스템

    private void Awake() // 테스트 참조 검사
    {
        bool hasMissingReference = dayNightCycle == null // 시간 참조 확인
            || seasonCycle == null // 계절 참조 확인
            || weatherCycle == null // 날씨 참조 확인
            || playerWetness == null // 젖음 참조 확인
            || playerTemperature == null // 체온 참조 확인
            || playerHealth == null // 체력 참조 확인
            || gameplaySaveController == null; // 저장 참조 확인

        if (hasMissingReference) // 참조 누락 여부 확인
        {
            Debug.LogError("EnvironmentSliceTestController의 참조가 누락되었습니다.", this); // 참조 오류 출력
            enabled = false; // 테스트 기능 비활성화
        }
    }

    [ContextMenu("46/Prepare Season Boundary Test")] // 계절 경계 테스트 메뉴
    private void PrepareSeasonBoundaryTest() // 봄 마지막 날 테스트 준비
    {
        dayNightCycle.SetTime(28, 18f); // 봄 마지막 날 오후 6시 적용
        weatherCycle.ForceWeather(WeatherType.Rain); // 비 날씨 적용
        playerWetness.SetCurrentWetness(0f); // 젖음 초기화
        playerTemperature.SetCurrentTemperature(playerTemperature.NormalTemperature); // 정상 체온 적용
        StartCoroutine(LogSnapshotNextFrame("SEASON BOUNDARY TEST")); // 다음 프레임 상태 출력
    }

    [ContextMenu("46/Prepare Winter Storm Test")] // 겨울 폭풍 테스트 메뉴
    private void PrepareWinterStormTest() // 겨울 저체온 테스트 준비
    {
        dayNightCycle.SetTime(85, 22f); // 겨울 첫날 오후 10시 적용
        weatherCycle.ForceWeather(WeatherType.Storm); // 폭풍 날씨 적용
        playerWetness.SetCurrentWetness(playerWetness.MaxWetness); // 최대 젖음 적용
        playerTemperature.SetCurrentTemperature(15f); // 저체온 수치 적용
        StartCoroutine(LogSnapshotNextFrame("WINTER STORM TEST")); // 다음 프레임 상태 출력
    }

    [ContextMenu("46/Prepare Summer Heat Test")] // 여름 더위 테스트 메뉴
    private void PrepareSummerHeatTest() // 여름 열사병 테스트 준비
    {
        dayNightCycle.SetTime(29, 12f); // 여름 첫날 정오 적용
        weatherCycle.ForceWeather(WeatherType.Clear); // 맑음 날씨 적용
        playerWetness.SetCurrentWetness(0f); // 젖음 초기화
        playerTemperature.SetCurrentTemperature(175f); // 열사병 수치 적용
        StartCoroutine(LogSnapshotNextFrame("SUMMER HEAT TEST")); // 다음 프레임 상태 출력
    }

    [ContextMenu("46/Reset Comfortable State")] // 쾌적 상태 초기화 메뉴
    private void ResetComfortableState() // 환경 상태 초기화
    {
        dayNightCycle.SetTime(1, 12f); // 첫날 정오 적용
        weatherCycle.ForceWeather(WeatherType.Clear); // 맑음 날씨 적용
        playerWetness.SetCurrentWetness(0f); // 젖음 제거
        playerTemperature.SetCurrentTemperature(playerTemperature.NormalTemperature); // 정상 체온 적용
        StartCoroutine(LogSnapshotNextFrame("COMFORTABLE STATE")); // 다음 프레임 상태 출력
    }

    [ContextMenu("46/Save Current Test State")] // 테스트 상태 저장 메뉴
    private void SaveCurrentTestState() // 현재 게임 저장
    {
        gameplaySaveController.SaveCurrentGame(); // 저장 시스템 실행
        StartCoroutine(LogSnapshotNextFrame("STATE SAVED")); // 저장 상태 출력
    }

    [ContextMenu("46/Load Current Test State")] // 테스트 상태 불러오기 메뉴
    private void LoadCurrentTestState() // 저장 게임 불러오기
    {
        gameplaySaveController.LoadCurrentGame(); // 불러오기 시스템 실행
        StartCoroutine(LogSnapshotNextFrame("STATE LOADED")); // 복원 상태 출력
    }

    [ContextMenu("46/Kill Player For Respawn Test")] // 부활 테스트 메뉴
    private void KillPlayerForRespawnTest() // 플레이어 사망 처리
    {
        playerHealth.TakeDamage(playerHealth.MaxHealth); // 최대 체력만큼 피해 적용
    }

    [ContextMenu("46/Print Current Snapshot")] // 현재 상태 출력 메뉴
    private void PrintCurrentSnapshot() // 현재 환경 상태 출력
    {
        LogSnapshot("CURRENT SNAPSHOT"); // 환경 정보 출력
    }

    private IEnumerator LogSnapshotNextFrame(string label) // 다음 프레임 상태 출력
    {
        yield return null; // 시스템 갱신 한 프레임 대기
        LogSnapshot(label); // 갱신된 상태 출력
    }

    private void LogSnapshot(string label) // 환경 상태 Console 출력
    {
        float healthPercent = playerHealth.NormalizedHealth * 100f; // 현재 체력 백분율 계산
        Debug.Log($"{label}\nDAY: {dayNightCycle.CurrentDay} / HOUR: {dayNightCycle.CurrentHour:F1}\nSEASON: {seasonCycle.CurrentSeason}\nWEATHER: {weatherCycle.CurrentWeather} / REMAINING: {weatherCycle.RemainingWeatherHours:F1}\nWETNESS: {playerWetness.CurrentWetness:F1} / {playerWetness.MaxWetness:F1}\nTEMPERATURE: {playerTemperature.CurrentTemperature:F1} / STATE: {playerTemperature.CurrentState}\nHEALTH: {healthPercent:F0}%", this); // 통합 상태 출력
    }
}