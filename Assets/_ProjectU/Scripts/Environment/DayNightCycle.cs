using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.Rendering; // 환경광 모드 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class DayNightCycle : MonoBehaviour // 낮과 밤 시간 순환 관리
{
    [Header("References")] // 참조 설정 묶음
    [SerializeField] private Light sunLight; // 태양 역할 방향광
    [SerializeField] private TMP_Text timeText; // 날짜와 시간 표시 텍스트

    [Header("Time")] // 시간 설정 묶음
    [SerializeField] private float fullDayDurationSeconds = 600f; // 하루가 흐르는 현실 시간
    [SerializeField] private float startingHour = 8f; // 게임 시작 시간
    [SerializeField] private int startingDay = 1; // 게임 시작 날짜
    [SerializeField] private float cycleSpeedMultiplier = 1f; // 시간 진행 배율

    [Header("Sun")] // 태양 설정 묶음
    [SerializeField] private float sunYaw = -30f; // 태양 좌우 회전값
    [SerializeField] private float daySunIntensity = 1f; // 낮 방향광 밝기
    [SerializeField] private float nightSunIntensity = 0.03f; // 밤 방향광 밝기
    [SerializeField] private Color daySunColor = new Color(1f, 0.96f, 0.84f); // 낮 태양 색상
    [SerializeField] private Color sunsetSunColor = new Color(1f, 0.42f, 0.16f); // 일몰 태양 색상
    [SerializeField] private Color nightSunColor = new Color(0.28f, 0.38f, 0.62f); // 밤 태양 색상

    [Header("Ambient")] // 환경광 설정 묶음
    [SerializeField] private Color dayAmbientColor = new Color(0.54f, 0.57f, 0.62f); // 낮 환경광 색상
    [SerializeField] private Color nightAmbientColor = new Color(0.04f, 0.06f, 0.11f); // 밤 환경광 색상

    [Header("Runtime")] // 실행 상태 묶음
    [SerializeField] private int currentDay; // 현재 날짜
    [SerializeField] private float currentHour; // 현재 시간
    [SerializeField] private float daylightStrength; // 현재 일광 비율

    public int CurrentDay => currentDay; // 현재 날짜 제공
    public float CurrentHour => currentHour; // 현재 시간 제공
    public bool IsNight => currentHour >= 18f || currentHour < 6f; // 현재 야간 여부 제공

    private void Awake() // 시간 시스템 초기화
    {
        ClampSettings(); // 설정값 범위 보정

        if (sunLight == null || timeText == null) // 필수 참조 확인
        {
            Debug.LogError($"{gameObject.name}의 낮과 밤 시스템 참조가 누락되었습니다.", this); // 참조 누락 오류 출력
            enabled = false; // 시간 시스템 비활성화
            return; // 초기화 중단
        }

        if (sunLight.type != LightType.Directional) // 방향광 종류 확인
        {
            Debug.LogError("Sun Light에는 Directional Light가 연결되어야 합니다.", sunLight); // 조명 종류 오류 출력
            enabled = false; // 시간 시스템 비활성화
            return; // 초기화 중단
        }

        currentHour = Mathf.Repeat(startingHour, 24f); // 시작 시간 적용
        currentDay = Mathf.Max(1, startingDay); // 시작 날짜 적용
        RenderSettings.sun = sunLight; // Scene 태양광 지정
        ApplyCycleState(); // 초기 시간 상태 반영
    }

    private void Update() // 매 프레임 시간 진행
    {
        AdvanceTime(); // 게임 시간 증가
        ApplyCycleState(); // 조명과 UI 갱신
    }

    private void OnValidate() // Inspector 설정값 검증
    {
        ClampSettings(); // 설정값 범위 보정
    }

    private void AdvanceTime() // 게임 시간 증가 처리
    {
        float inGameHoursPerSecond = 24f / fullDayDurationSeconds; // 초당 게임 시간 계산
        float advancedHours = inGameHoursPerSecond * Time.deltaTime * cycleSpeedMultiplier; // 현재 프레임 증가 시간 계산
        currentHour += advancedHours; // 현재 시간 증가

        while (currentHour >= 24f) // 자정 통과 여부 확인
        {
            currentHour -= 24f; // 다음 날 시간으로 보정
            currentDay++; // 날짜 증가
        }
    }

    private void ApplyCycleState() // 현재 시간의 환경 상태 적용
    {
        float cycleRadians = ((currentHour - 6f) / 24f) * Mathf.PI * 2f; // 오전 6시 기준 회전값 계산
        float sunHeight = Mathf.Sin(cycleRadians); // 태양 높이 비율 계산
        daylightStrength = Mathf.Clamp01(sunHeight); // 낮 밝기 범위 보정
        float twilightStrength = Mathf.Clamp01(1f - Mathf.Abs(sunHeight) * 5f); // 일출과 일몰 비율 계산
        float sunPitch = (currentHour / 24f * 360f) - 90f; // 태양 상하 회전값 계산

        sunLight.transform.rotation = Quaternion.Euler(sunPitch, sunYaw, 0f); // 태양 방향 회전
        Color currentSunColor = Color.Lerp(nightSunColor, daySunColor, daylightStrength); // 낮과 밤 태양색 혼합
        currentSunColor = Color.Lerp(currentSunColor, sunsetSunColor, twilightStrength * 0.65f); // 일출과 일몰 색상 혼합
        sunLight.color = currentSunColor; // 방향광 색상 적용
        sunLight.intensity = Mathf.Lerp(nightSunIntensity, daySunIntensity, daylightStrength); // 방향광 밝기 적용

        RenderSettings.ambientMode = AmbientMode.Flat; // 단색 환경광 모드 적용
        RenderSettings.ambientLight = Color.Lerp(nightAmbientColor, dayAmbientColor, daylightStrength); // 환경광 색상 적용

        RefreshTimeText(); // 날짜와 시간 UI 갱신
    }

    private void RefreshTimeText() // 날짜와 시간 문구 갱신
    {
        int totalMinutes = Mathf.FloorToInt(currentHour * 60f); // 현재 시간을 전체 분으로 변환
        int displayHour = totalMinutes / 60 % 24; // 화면 표시 시 계산
        int displayMinute = totalMinutes % 60; // 화면 표시 분 계산
        timeText.text = $"DAY {currentDay}  {displayHour:00}:{displayMinute:00}"; // 날짜와 시간 문구 적용
    }

    private void ClampSettings() // Inspector 설정값 범위 보정
    {
        fullDayDurationSeconds = Mathf.Max(1f, fullDayDurationSeconds); // 하루 시간 최소값 적용
        startingHour = Mathf.Repeat(startingHour, 24f); // 시작 시간 하루 범위 적용
        startingDay = Mathf.Max(1, startingDay); // 시작 날짜 최소값 적용
        cycleSpeedMultiplier = Mathf.Max(0f, cycleSpeedMultiplier); // 시간 배율 음수 방지
        sunYaw = Mathf.Clamp(sunYaw, -360f, 360f); // 태양 좌우 회전 범위 적용
        daySunIntensity = Mathf.Max(0f, daySunIntensity); // 낮 밝기 음수 방지
        nightSunIntensity = Mathf.Max(0f, nightSunIntensity); // 밤 밝기 음수 방지
    }
}