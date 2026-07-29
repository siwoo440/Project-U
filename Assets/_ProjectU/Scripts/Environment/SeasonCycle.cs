using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class SeasonCycle : MonoBehaviour // 계절 순환 관리
{
    [Header("References")] // 참조 설정 묶음
    [SerializeField] private TMP_Text seasonText; // 계절 표시 텍스트

    [Header("Season Length")] // 계절 길이 설정 묶음
    [SerializeField][Min(1)] private int daysPerSeason = 7; // 계절당 날짜 수

    [Header("Spring")] // 봄 색상 설정 묶음
    [SerializeField] private Color springSunTint = new Color(1f, 0.97f, 0.92f); // 봄 태양광 색상
    [SerializeField] private Color springAmbientTint = new Color(0.95f, 1f, 0.95f); // 봄 환경광 색상

    [Header("Summer")] // 여름 색상 설정 묶음
    [SerializeField] private Color summerSunTint = new Color(1f, 1f, 0.94f); // 여름 태양광 색상
    [SerializeField] private Color summerAmbientTint = new Color(1f, 1f, 0.92f); // 여름 환경광 색상

    [Header("Autumn")] // 가을 색상 설정 묶음
    [SerializeField] private Color autumnSunTint = new Color(1f, 0.88f, 0.72f); // 가을 태양광 색상
    [SerializeField] private Color autumnAmbientTint = new Color(1f, 0.9f, 0.8f); // 가을 환경광 색상

    [Header("Winter")] // 겨울 색상 설정 묶음
    [SerializeField] private Color winterSunTint = new Color(0.82f, 0.91f, 1f); // 겨울 태양광 색상
    [SerializeField] private Color winterAmbientTint = new Color(0.82f, 0.9f, 1f); // 겨울 환경광 색상

    [Header("Runtime")] // 실행 상태 묶음
    [SerializeField] private SeasonType currentSeason; // 현재 계절
    [SerializeField] private int currentDayInSeason = 1; // 현재 계절 내부 날짜
    [SerializeField] private Color currentSunTint = Color.white; // 현재 태양광 색상
    [SerializeField] private Color currentAmbientTint = Color.white; // 현재 환경광 색상

    private int lastAppliedDay = -1; // 마지막으로 적용한 전체 날짜

    public SeasonType CurrentSeason => currentSeason; // 현재 계절 제공
    public int CurrentDayInSeason => currentDayInSeason; // 계절 내부 날짜 제공
    public int DaysPerSeason => daysPerSeason; // 계절당 날짜 수 제공
    public Color CurrentSunTint => currentSunTint; // 현재 태양광 색상 제공
    public Color CurrentAmbientTint => currentAmbientTint; // 현재 환경광 색상 제공

    private void Awake() // 계절 시스템 초기화
    {
        ClampSettings(); // 설정값 범위 보정

        if (seasonText == null) // 계절 텍스트 참조 확인
        {
            Debug.LogError($"{gameObject.name}의 계절 표시 텍스트가 연결되지 않았습니다.", this); // 참조 누락 오류
            enabled = false; // 계절 시스템 비활성화
        }
    }

    private void OnValidate() // Inspector 설정값 검증
    {
        ClampSettings(); // 설정값 범위 보정
        lastAppliedDay = -1; // 다음 날짜 적용 강제
    }

    public void SetCurrentDay(int currentDay) // 전체 날짜에 맞는 계절 적용
    {
        int safeDay = Mathf.Max(1, currentDay); // 날짜 최소값 보정

        if (lastAppliedDay == safeDay) // 같은 날짜 중복 적용 확인
        {
            return; // 중복 갱신 종료
        }

        int zeroBasedDay = safeDay - 1; // 날짜 계산용 0 기준 변환
        int seasonIndex = zeroBasedDay / daysPerSeason % 4; // 현재 계절 번호 계산
        currentSeason = (SeasonType)seasonIndex; // 현재 계절 적용
        currentDayInSeason = zeroBasedDay % daysPerSeason + 1; // 계절 내부 날짜 계산
        currentSunTint = GetSeasonSunTint(); // 계절 태양광 색상 적용
        currentAmbientTint = GetSeasonAmbientTint(); // 계절 환경광 색상 적용
        lastAppliedDay = safeDay; // 적용한 날짜 저장
        RefreshSeasonText(); // 계절 UI 갱신
    }

    private Color GetSeasonSunTint() // 계절별 태양광 색상 선택
    {
        switch (currentSeason) // 현재 계절 비교
        {
            case SeasonType.Spring: // 봄 확인
                return springSunTint; // 봄 태양광 반환

            case SeasonType.Summer: // 여름 확인
                return summerSunTint; // 여름 태양광 반환

            case SeasonType.Autumn: // 가을 확인
                return autumnSunTint; // 가을 태양광 반환

            case SeasonType.Winter: // 겨울 확인
                return winterSunTint; // 겨울 태양광 반환

            default: // 잘못된 계절 처리
                return Color.white; // 기본 색상 반환
        }
    }

    private Color GetSeasonAmbientTint() // 계절별 환경광 색상 선택
    {
        switch (currentSeason) // 현재 계절 비교
        {
            case SeasonType.Spring: // 봄 확인
                return springAmbientTint; // 봄 환경광 반환

            case SeasonType.Summer: // 여름 확인
                return summerAmbientTint; // 여름 환경광 반환

            case SeasonType.Autumn: // 가을 확인
                return autumnAmbientTint; // 가을 환경광 반환

            case SeasonType.Winter: // 겨울 확인
                return winterAmbientTint; // 겨울 환경광 반환

            default: // 잘못된 계절 처리
                return Color.white; // 기본 색상 반환
        }
    }

    private void RefreshSeasonText() // 현재 계절 문구 갱신
    {
        if (seasonText == null) // 텍스트 참조 확인
        {
            return; // 문구 갱신 중단
        }

        seasonText.text = $"{GetSeasonLabel()}  DAY {currentDayInSeason}/{daysPerSeason}"; // 계절과 날짜 표시
    }

    private string GetSeasonLabel() // 계절 영문 이름 반환
    {
        switch (currentSeason) // 현재 계절 비교
        {
            case SeasonType.Spring: // 봄 확인
                return "SPRING"; // 봄 문구 반환

            case SeasonType.Summer: // 여름 확인
                return "SUMMER"; // 여름 문구 반환

            case SeasonType.Autumn: // 가을 확인
                return "AUTUMN"; // 가을 문구 반환

            case SeasonType.Winter: // 겨울 확인
                return "WINTER"; // 겨울 문구 반환

            default: // 잘못된 계절 처리
                return "UNKNOWN"; // 알 수 없는 계절 반환
        }
    }

    private void ClampSettings() // 계절 설정값 보정
    {
        daysPerSeason = Mathf.Max(1, daysPerSeason); // 계절 길이 최소값 적용
    }
}