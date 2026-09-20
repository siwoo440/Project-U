using System.Linq; // 목록 계산
using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class SmallCreatureLife : MonoBehaviour // 118일차: 나비 · 반딧불이 · 게 (제자리에서 살랑이고, 때가 아니면 숨는다)
{
    [Header("Activity")] // 활동 설정 묶음
    [Tooltip("돌아다니는 때 (낮 · 밤).")]
    [SerializeField] private WildAnimalActivity activity = WildAnimalActivity.DayOnly; // 활동 시간
    [Tooltip("이 계절에는 숨습니다.")]
    [SerializeField] private SeasonType[] hiddenSeasons = new SeasonType[0]; // 숨는 계절

    [Header("Motion")] // 움직임 설정 묶음
    [Tooltip("위아래로 흔들리는 높이 (m). 0이면 움직이지 않습니다.")]
    [SerializeField, Min(0f)] private float bobHeight = 0.25f; // 흔들리는 높이
    [Tooltip("흔들리는 빠르기.")]
    [SerializeField, Min(0.1f)] private float bobSpeed = 1.6f; // 흔들리는 빠르기
    [Tooltip("제자리에서 도는 빠르기 (도/초).")]
    [SerializeField] private float turnSpeed = 35f; // 도는 빠르기

    private DayNightCycle cycle; // 낮과 밤
    private SeasonCycle seasons; // 계절
    private Renderer[] renderers; // 외형
    private Collider[] colliders; // 충돌체
    private Vector3 basePosition; // 처음 자리
    private float phase; // 흔들림 시작 위치
    private bool hidden; // 숨은 상태
    private float nextCheckTime; // 다음 확인 시각

    public bool IsHidden => hidden; // 숨은 상태 제공 (검사용)
    public WildAnimalActivity Activity => activity; // 활동 시간 제공

#if UNITY_EDITOR
    public void EditorAssign(WildAnimalActivity newActivity, SeasonType[] hidden, float bob, float turn) // 생성 도구 전용
    {
        activity = newActivity;
        hiddenSeasons = hidden ?? new SeasonType[0];
        bobHeight = bob;
        turnSpeed = turn;
    }
#endif

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider>(true);
        basePosition = transform.position;
        phase = Random.value * 10f;
    }

    private void Update()
    {
        if (Time.time >= nextCheckTime) // 활동 시간 확인은 0.5초마다
        {
            nextCheckTime = Time.time + 0.5f;
            bool shouldHide = ShouldHideNow();

            if (shouldHide != hidden)
            {
                hidden = shouldHide;
                SetVisible(!hidden);
            }
        }

        if (hidden || bobHeight <= 0f)
        {
            return;
        }

        float offset = Mathf.Sin((Time.time + phase) * bobSpeed) * bobHeight * 0.5f;
        transform.position = basePosition + new Vector3(0f, offset + bobHeight * 0.5f, 0f);
        transform.Rotate(Vector3.up, turnSpeed * Time.deltaTime, Space.World);
    }

    private bool ShouldHideNow()
    {
        if (cycle == null)
        {
            cycle = FindFirstObjectByType<DayNightCycle>();
        }

        if (seasons == null)
        {
            seasons = FindFirstObjectByType<SeasonCycle>();
        }

        if (seasons != null && hiddenSeasons != null && hiddenSeasons.Contains(seasons.CurrentSeason))
        {
            return true;
        }

        if (cycle == null || activity == WildAnimalActivity.Always)
        {
            return false;
        }

        return activity == WildAnimalActivity.DayOnly ? cycle.IsNight : !cycle.IsNight;
    }

    private void SetVisible(bool visible)
    {
        foreach (Renderer item in renderers)
        {
            if (item != null)
            {
                item.enabled = visible;
            }
        }

        foreach (Collider item in colliders)
        {
            if (item != null)
            {
                item.enabled = visible;
            }
        }
    }
}
