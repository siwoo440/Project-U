using System; // Serializable
using System.Collections.Generic; // 목록
using UnityEngine; // Unity 기본 기능

[CreateAssetMenu(fileName = "NpcSchedule_New", menuName = "Project U/NPC/Schedule")] // 일정 생성 메뉴
public sealed class NpcScheduleData : ScriptableObject // 89일차: NPC 하루 일정 (요일·계절·날씨 조건별 계획)
{
    [Serializable]
    public sealed class Stop // 일정 한 칸 : 이 시각부터 다음 칸까지 머무는 곳
    {
        [Tooltip("시작 시각 (0~24).")]
        [SerializeField, Range(0f, 24f)] private float hour; // 시작 시각
        [Tooltip("위치 ID (NpcDatabase 위치 목록).")]
        [SerializeField] private string locationId; // 위치
        [Tooltip("하는 일 (대화·말풍선에 사용).")]
        [SerializeField] private string activity; // 하는 일

        public Stop(float hour, string locationId, string activity) // 생성 도구에서 사용
        {
            this.hour = hour;
            this.locationId = locationId;
            this.activity = activity;
        }

        public float Hour => hour; // 시각 제공
        public string LocationId => locationId; // 위치 제공
        public string Activity => activity; // 하는 일 제공
    }

    [Serializable]
    public sealed class Plan // 조건이 맞는 날 쓰는 하루 계획
    {
        [Tooltip("계획 이름 (default · rain · rest 등).")]
        [SerializeField] private string planId = "default"; // 계획 이름
        [Tooltip("여러 계획이 맞으면 숫자가 큰 계획을 씁니다.")]
        [SerializeField] private int priority; // 우선순위
        [Tooltip("적용 요일.")]
        [SerializeField] private NpcWeekdays days = NpcWeekdays.All; // 요일
        [Tooltip("적용 계절.")]
        [SerializeField] private NpcSeasons seasons = NpcSeasons.All; // 계절
        [Tooltip("적용 날씨.")]
        [SerializeField] private NpcWeatherCondition weather = NpcWeatherCondition.Any; // 날씨
        [Tooltip("시각 순서의 일정.")]
        [SerializeField] private List<Stop> stops = new List<Stop>(); // 일정

        public Plan(string planId, int priority, NpcWeekdays days, NpcSeasons seasons, NpcWeatherCondition weather, List<Stop> stops) // 생성 도구에서 사용
        {
            this.planId = planId;
            this.priority = priority;
            this.days = days;
            this.seasons = seasons;
            this.weather = weather;
            this.stops = stops ?? new List<Stop>();
        }

        public string PlanId => planId; // 이름 제공
        public int Priority => priority; // 우선순위 제공
        public NpcWeekdays Days => days; // 요일 제공
        public NpcSeasons Seasons => seasons; // 계절 제공
        public NpcWeatherCondition Weather => weather; // 날씨 제공
        public IReadOnlyList<Stop> Stops => stops; // 일정 제공

        public bool Matches(NpcWeekdays weekday, NpcSeasons season, WeatherType currentWeather) // 오늘 조건 일치 여부
        {
            return (days & weekday) != 0 && (seasons & season) != 0 && NpcCalendar.Matches(weather, currentWeather);
        }

        public Stop GetStop(float currentHour) // 현재 시각의 일정 칸
        {
            Stop current = stops.Count > 0 ? stops[stops.Count - 1] : null; // 자정 넘긴 시간은 마지막 칸

            foreach (Stop stop in stops)
            {
                if (stop.Hour <= currentHour)
                {
                    current = stop;
                }
            }

            return current;
        }
    }

    [Tooltip("일정 ID (schedule_lunette).")]
    [SerializeField] private string scheduleId = "schedule_new"; // 일정 ID
    [SerializeField] private List<Plan> plans = new List<Plan>(); // 계획 목록

    public string ScheduleId => scheduleId; // ID 제공
    public IReadOnlyList<Plan> Plans => plans; // 계획 제공

    public Plan SelectPlan(int day, SeasonType season, WeatherType weather) // 오늘 쓸 계획
    {
        NpcWeekdays weekday = NpcCalendar.GetWeekday(day);
        NpcSeasons seasonMask = NpcCalendar.ToMask(season);
        Plan selected = null;

        foreach (Plan plan in plans)
        {
            if (plan != null && plan.Matches(weekday, seasonMask, weather) && (selected == null || plan.Priority > selected.Priority))
            {
                selected = plan; // 우선순위가 가장 높은 계획
            }
        }

        return selected;
    }

#if UNITY_EDITOR
    public void EditorAssign(string id, List<Plan> newPlans) // 생성 도구 전용
    {
        scheduleId = id;
        plans = newPlans ?? new List<Plan>();
    }
#endif
}
