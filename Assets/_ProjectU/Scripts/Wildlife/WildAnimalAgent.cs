using System.Collections.Generic; // 목록
using System.Linq; // 목록 계산
using UnityEngine; // Unity 기본 기능
using UnityEngine.AI; // NavMesh 이동

public enum WildAnimalKind // 118일차: 야생동물 성격
{
    Calm = 0, // 순한 동물 (가까이 가면 도망)
    Fierce = 1 // 사나운 동물 (영역 안에서 덤빔)
}

public enum WildAnimalActivity // 118일차: 돌아다니는 때
{
    Always = 0, // 하루 내내
    DayOnly = 1, // 낮에만 (밤에는 숨는다)
    NightOnly = 2 // 밤에만
}

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
[RequireComponent(typeof(NavMeshAgent))] // NavMesh 이동 요구
public sealed class WildAnimalAgent : MonoBehaviour // 118일차: 야생동물 한 마리의 배회 · 도망 · 영역 지키기
{
    private const float ThinkInterval = 0.25f; // 판단 간격 (초)

    [Header("Animal")] // 동물 설정 묶음
    [Tooltip("동물 ID (사냥 통계 · 검사용).")]
    [SerializeField] private string animalId = string.Empty; // 동물 ID
    [Tooltip("순한 동물인지 사나운 동물인지.")]
    [SerializeField] private WildAnimalKind kind = WildAnimalKind.Calm; // 성격
    [Tooltip("돌아다니는 때 (낮 · 밤).")]
    [SerializeField] private WildAnimalActivity activity = WildAnimalActivity.Always; // 활동 시간
    [Tooltip("이 계절에는 숨습니다 (비어 있으면 네 계절 모두 나옵니다).")]
    [SerializeField] private SeasonType[] hiddenSeasons = new SeasonType[0]; // 숨는 계절

    [Header("Move")] // 이동 설정 묶음
    [Tooltip("자기 영역 반경 (이 안에서만 돌아다닙니다).")]
    [SerializeField, Min(5f)] private float roamRadius = 32f; // 영역 반경
    [Tooltip("평소 걷는 속도.")]
    [SerializeField, Min(0.2f)] private float walkSpeed = 1.6f; // 걷는 속도
    [Tooltip("도망칠 때 속도.")]
    [SerializeField, Min(0.5f)] private float fleeSpeed = 5f; // 도망 속도
    [Tooltip("플레이어가 이 거리 안에 오면 도망칩니다.")]
    [SerializeField, Min(2f)] private float fleeDistance = 12f; // 도망 시작 거리
    [Tooltip("플레이어가 이 거리보다 멀어지면 다시 안심합니다.")]
    [SerializeField, Min(4f)] private float calmDistance = 26f; // 안심 거리
    [Tooltip("한 곳에 도착한 뒤 쉬는 시간.")]
    [SerializeField, Min(0f)] private float restSeconds = 3.5f; // 쉬는 시간

    private NavMeshAgent agent; // 이동 Agent
    private EnemyHealth health; // 체력 (사냥 대상)
    private EnemyCombatController combat; // 사나운 동물 전투
    private DayNightCycle cycle; // 낮과 밤
    private SeasonCycle seasons; // 계절
    private Transform player; // 플레이어
    private Renderer[] renderers; // 외형
    private Collider[] colliders; // 충돌체
    private Vector3 home; // 영역 가운데
    private bool hidden; // 숨은 상태
    private bool fleeing; // 도망 중
    private float nextThinkTime; // 다음 판단 시각
    private float restUntil; // 쉬는 끝 시각

    public string AnimalId => animalId; // 동물 ID 제공 (검사용)
    public WildAnimalKind Kind => kind; // 성격 제공
    public WildAnimalActivity Activity => activity; // 활동 시간 제공
    public float RoamRadius => roamRadius; // 영역 반경 제공
    public Vector3 Home => home; // 영역 가운데 제공
    public bool IsHidden => hidden; // 숨은 상태 제공 (검사용)
    public bool IsFleeing => fleeing; // 도망 중 여부 제공 (검사용)
    public IReadOnlyList<SeasonType> HiddenSeasons => hiddenSeasons; // 숨는 계절 제공

    public void ConfigureHome(Vector3 center, float radius) // 배치 도구 · 스폰 지점에서 영역 지정
    {
        home = center;
        roamRadius = Mathf.Max(5f, radius);
    }

#if UNITY_EDITOR
    public void EditorAssign(string id, WildAnimalKind newKind, WildAnimalActivity newActivity, float radius, float walk, float flee, float fleeStart, SeasonType[] hidden) // 생성 도구 전용
    {
        animalId = id;
        kind = newKind;
        activity = newActivity;
        roamRadius = radius;
        walkSpeed = walk;
        fleeSpeed = flee;
        fleeDistance = fleeStart;
        calmDistance = Mathf.Max(fleeStart + 8f, fleeStart * 2f);
        hiddenSeasons = hidden ?? new SeasonType[0];
    }
#endif

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<EnemyHealth>();
        combat = GetComponent<EnemyCombatController>();
        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider>(true);
        home = transform.position;
        agent.speed = walkSpeed;
        agent.angularSpeed = 300f;
        agent.acceleration = 12f;
        agent.stoppingDistance = 0.4f;
    }

    private void Start()
    {
        if (combat != null) // 사나운 동물은 영역 밖으로 쫓아가지 않는다
        {
            combat.SetTerritory(home, roamRadius);
        }

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 6f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }
    }

    private void Update()
    {
        if (Time.time < nextThinkTime)
        {
            return;
        }

        nextThinkTime = Time.time + ThinkInterval;

        if (health != null && health.IsDead) // 죽은 동물은 움직이지 않는다
        {
            StopMoving();
            return;
        }

        if (!UpdateHidden()) // 숨는 시간
        {
            return;
        }

        if (kind == WildAnimalKind.Fierce)
        {
            ThinkFierce();
            return;
        }

        ThinkCalm();
    }

    private bool UpdateHidden() // 활동 시간 · 계절에 맞게 나타나거나 숨는다
    {
        bool shouldHide = ShouldHideNow();

        if (shouldHide != hidden)
        {
            hidden = shouldHide;
            SetVisible(!hidden);
        }

        if (!hidden)
        {
            return true;
        }

        StopMoving();
        return false;
    }

    private bool ShouldHideNow()
    {
        FindReferences();

        if (seasons != null && hiddenSeasons != null && hiddenSeasons.Length > 0 && hiddenSeasons.Contains(seasons.CurrentSeason))
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

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = !visible;
        }
    }

    private void ThinkCalm() // 순한 동물 : 사람을 보면 달아난다
    {
        float distance = PlayerDistance();

        if (distance < fleeDistance)
        {
            fleeing = true;
            Flee();
            return;
        }

        if (fleeing && distance < calmDistance) // 아직 멀어지지 않았으면 계속 달린다
        {
            Flee();
            return;
        }

        fleeing = false;
        agent.speed = walkSpeed;
        Roam();
    }

    private void ThinkFierce() // 사나운 동물 : 영역 안에서 덤비고, 밖으로 나가면 돌아온다
    {
        if (combat != null && combat.CurrentState == EnemyCombatState.Chasing && combat.CurrentTarget != null)
        {
            agent.speed = fleeSpeed; // 쫓아갈 때는 빠르게
            SetDestination(combat.CurrentTarget.position);
            return;
        }

        if (combat != null && (combat.CurrentState == EnemyCombatState.Attacking || combat.CurrentState == EnemyCombatState.Hit))
        {
            StopMoving(); // 공격 중에는 제자리
            return;
        }

        float fromHome = Flat(transform.position, home);
        agent.speed = walkSpeed;

        if (fromHome > roamRadius * 1.05f) // 영역 밖 : 집으로
        {
            SetDestination(home);
            return;
        }

        Roam();
    }

    private void Flee() // 플레이어 반대 방향으로 달아난다 (영역에서 너무 멀어지지 않는다)
    {
        if (player == null || agent == null || !agent.isOnNavMesh)
        {
            return;
        }

        agent.speed = fleeSpeed;
        Vector3 away = transform.position - player.position;
        away.y = 0f;

        if (away.sqrMagnitude < 0.01f)
        {
            away = transform.forward;
        }

        Vector3 target = transform.position + away.normalized * 14f;

        if (Flat(target, home) > roamRadius * 1.6f) // 영역 쪽으로 돌려 준다
        {
            Vector3 toHome = home - transform.position;
            toHome.y = 0f;
            target = transform.position + Vector3.Slerp(away.normalized, toHome.normalized, 0.6f) * 12f;
        }

        SetDestination(target);
    }

    private void Roam() // 영역 안을 천천히 돌아다닌다
    {
        if (agent == null || !agent.isOnNavMesh)
        {
            return;
        }

        if (agent.pathPending || agent.remainingDistance > agent.stoppingDistance + 0.3f)
        {
            return; // 가는 중
        }

        if (Time.time < restUntil)
        {
            agent.isStopped = true;
            return;
        }

        Vector2 offset = Random.insideUnitCircle * roamRadius * 0.85f;
        SetDestination(home + new Vector3(offset.x, 0f, offset.y));
        restUntil = Time.time + restSeconds * Random.Range(0.6f, 1.6f);
    }

    private void SetDestination(Vector3 target)
    {
        if (!NavMesh.SamplePosition(target, out NavMeshHit hit, 8f, NavMesh.AllAreas))
        {
            return;
        }

        agent.isStopped = false;
        agent.SetDestination(hit.position);
    }

    private void StopMoving()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }
    }

    private float PlayerDistance()
    {
        FindReferences();
        return player == null ? float.PositiveInfinity : Flat(transform.position, player.position);
    }

    private static float Flat(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void FindReferences()
    {
        if (cycle == null)
        {
            cycle = FindFirstObjectByType<DayNightCycle>();
        }

        if (seasons == null)
        {
            seasons = FindFirstObjectByType<SeasonCycle>();
        }

        if (player == null)
        {
            PlayerInventory inventory = FindFirstObjectByType<PlayerInventory>();
            player = inventory != null ? inventory.transform : null;
        }
    }
}
