using System.Linq; // 목록 계산
using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class SeasonForage : MonoBehaviour // 119일차: 제철에만 나는 숲 채집물 (봄 산나물 · 가을 밤처럼)
{
    [Header("Season")] // 계절 설정 묶음
    [Tooltip("이 계절에만 보입니다 (비어 있으면 네 계절 모두).")]
    [SerializeField] private SeasonType[] seasons = new SeasonType[0]; // 나는 계절
    [Tooltip("채집물 ID (검사용).")]
    [SerializeField] private string forageId = string.Empty; // 채집물 ID

    private SeasonCycle cycle; // 계절
    private Renderer[] renderers; // 외형
    private Collider[] colliders; // 충돌체
    private GatherableResource resource; // 채집 기능
    private bool hidden; // 숨은 상태
    private float nextCheckTime; // 다음 확인 시각
    private bool checkedOnce; // 한 번이라도 확인했는지

    public string ForageId => forageId; // 채집물 ID 제공 (검사용)
    public bool IsHidden => hidden; // 숨은 상태 제공 (검사용)
    public SeasonType[] Seasons => seasons; // 나는 계절 제공 (검사용)

    public bool IsInSeason(SeasonType season) // 그 계절에 나는지
    {
        return seasons == null || seasons.Length == 0 || seasons.Contains(season);
    }

#if UNITY_EDITOR
    public void EditorAssign(string id, SeasonType[] newSeasons) // 생성 도구 전용
    {
        forageId = id;
        seasons = newSeasons ?? new SeasonType[0];
    }
#endif

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider>(true);
        resource = GetComponent<GatherableResource>();
    }

    private void Update() // 0.5초마다 계절을 확인한다 (계절이 바뀌면 다음 아침에 갈린다)
    {
        if (Time.time < nextCheckTime)
        {
            return;
        }

        nextCheckTime = Time.time + 0.5f;

        if (cycle == null)
        {
            cycle = FindFirstObjectByType<SeasonCycle>();

            if (cycle == null)
            {
                return;
            }
        }

        bool shouldHide = !IsInSeason(cycle.CurrentSeason);

        if (shouldHide == hidden && checkedOnce)
        {
            return;
        }

        checkedOnce = true;
        hidden = shouldHide;
        SetVisible(!hidden);
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

        if (visible && resource != null && resource.IsDepleted) // 제철이 되면 다시 자란다
        {
            resource.ResetForLoad();
        }
    }
}
