using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.AI; // NavMesh 이동

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
[RequireComponent(typeof(NavMeshAgent))] // 길찾기 이동
public sealed class NpcAgent : MonoBehaviour // 90일차: 마을 NPC 한 명 (일정 위치로 걷기 · 이름표 · 말풍선 · 집 안 들어가기)
{
    [Header("Data")] // 데이터
    [Tooltip("캐릭터 데이터.")]
    [SerializeField] private NpcCharacterData character; // 캐릭터

    [Header("Parts")] // 구성 요소
    [Tooltip("저폴리 모델 (걷기 흔들림 · 집 안에서 숨김).")]
    [SerializeField] private Transform model; // 모델
    [Tooltip("머리 위 이름표.")]
    [SerializeField] private TMP_Text nameTag; // 이름표
    [Tooltip("말풍선.")]
    [SerializeField] private TMP_Text speech; // 말풍선
    [Tooltip("몸 충돌체 (상호작용 탐지).")]
    [SerializeField] private Collider bodyCollider; // 충돌체

    [Header("Motion")] // 움직임
    [Tooltip("걷는 속도 (m/s).")]
    [SerializeField, Min(0.2f)] private float walkSpeed = 2.2f; // 걷기 속도
    [Tooltip("이 거리 안에 오면 도착으로 봅니다.")]
    [SerializeField, Min(0.05f)] private float arriveDistance = 0.35f; // 도착 거리
    [Tooltip("이 거리 안에 플레이어가 오면 바라봅니다.")]
    [SerializeField, Min(0f)] private float lookDistance = 4.5f; // 바라보기 거리
    [Tooltip("이름표가 보이는 거리.")]
    [SerializeField, Min(1f)] private float nameTagDistance = 14f; // 이름표 거리
    [Tooltip("길이 막혀 이 시간(초) 동안 움직이지 못하면 목적지로 옮깁니다.")]
    [SerializeField, Min(0.5f)] private float stuckSeconds = 4f; // 막힘 시간
    [Tooltip("100일차: 하반신 모양별 움직임 (걷기 · 미끄러지기 · 떠다니기 · 말 걸음 · 여러 다리 · 젤리 · 깡충).")]
    [SerializeField] private NpcMotionStyle motionStyle = NpcMotionStyle.Walk; // 움직임

    [Header("Far Travel")] // 107일차: 섬 곳곳 구역 사이 먼 길
    [Tooltip("플레이어에게서 이 거리보다 멀면 보이지 않는 동안 빠르게 이동합니다 (m).")]
    [SerializeField, Min(10f)] private float farDistance = 60f; // 빠른 이동 거리
    [Tooltip("멀리서 이동할 때 걷기 속도 배율.")]
    [SerializeField, Min(1f)] private float farSpeedMultiplier = 16f; // 빠른 이동 배율
    [Tooltip("보이는 곳에서 남은 길이 길 때 서둘러 걷는 배율.")]
    [SerializeField, Min(1f)] private float longTripMultiplier = 1.8f; // 서둘러 걷기 배율
    [Tooltip("남은 길이 이보다 길면 서둘러 걷습니다 (m).")]
    [SerializeField, Min(5f)] private float longTripDistance = 40f; // 서둘러 걷기 거리

    [Header("Companion")] // 109일차: 동료로 따라다니기
    [Tooltip("플레이어가 이 거리보다 멀어지면 플레이어 가까이로 옮깁니다 (m).")]
    [SerializeField, Min(10f)] private float followTeleportDistance = 45f; // 따라가기 순간이동 거리

    [Header("Runtime")] // 실행 상태
    [SerializeField] private string currentLocationId; // 현재 위치 ID
    [SerializeField] private string currentActivity; // 하는 일
    [SerializeField] private bool isInside; // 집 안 여부
    [SerializeField] private bool arrived = true; // 도착 여부
    [SerializeField] private bool isTalking; // 대화 창이 열려 있는지 (91일차)

    private NavMeshAgent agent; // 길찾기
    private Transform player; // 플레이어
    private Transform viewCamera; // 카메라
    private Vector3 targetPosition; // 목적지
    private Quaternion targetFacing = Quaternion.identity; // 도착 후 방향
    private bool hideOnArrival; // 도착하면 집 안으로
    private float speechTimer; // 말풍선 남은 시간
    private float stuckTimer; // 막힘 시간
    private float motionTime; // 걷기·숨쉬기 시간
    private Vector3 modelBasePosition; // 모델 기본 위치
    private Vector3 modelBaseScale = Vector3.one; // 모델 기본 크기
    private Vector3 talkTarget; // 대화 상대 위치
    private string nameTagBaseText; // 의뢰 표시 없는 이름표 문구 (93일차)
    private NpcQuestMarker questMarker = NpcQuestMarker.None; // 현재 의뢰 표시
    private bool hasEventMarker; // 이벤트 표시 (94일차)
    private int repathCount; // 먼 길 중간(부분 경로)에서 다시 길을 찾은 횟수 (107일차)
    private Transform followTarget; // 109일차: 따라다니는 대상 (동료)
    private Vector3 engagePosition; // 109일차: 전투형 동료가 다가가는 적 위치
    private bool hasEngage; // 적에게 다가가는 중인지
    private float followRepathTimer; // 따라가기 길 다시 찾기 간격
    private const float ShoreSearchRadius = 90f; // 헤엄치는 플레이어 근처 물가를 찾는 거리
    private float followSide = 1f; // 플레이어 뒤 왼쪽 · 오른쪽
    private float shoreSearchTime; // 물가 찾기 다음 시각 (플레이어가 헤엄칠 때)

    public NpcCharacterData Character => character; // 캐릭터 제공
    public string CharacterId => character != null ? character.CharacterId : string.Empty; // ID 제공
    public string DisplayName => character != null ? character.DisplayName : name; // 이름 제공
    public string CurrentLocationId => currentLocationId; // 위치 제공
    public string CurrentActivity => currentActivity; // 하는 일 제공
    public NpcLocationPoint CurrentPoint { get; private set; } // 위치 지점 제공
    public bool IsInside => isInside; // 집 안 여부 제공
    public bool HasArrived => arrived; // 도착 여부 제공
    public bool IsTalking => isTalking; // 대화 중 여부 제공
    public NpcQuestMarker QuestMarker => questMarker; // 의뢰 표시 제공 (93일차)
    public bool HasEventMarker => hasEventMarker; // 이벤트 표시 제공 (94일차)
    public Vector3 TargetPosition => targetPosition; // 목적지 제공
    public NpcMotionStyle MotionStyle => motionStyle; // 움직임 제공 (100일차)
    public float WalkSpeed => walkSpeed; // 걷기 속도 제공 (100일차)
    public string StopKey { get; set; } // 관리자가 쓰는 현재 일정 칸 키
    public bool IsTravelingFast { get; private set; } // 107일차: 멀리서 빠르게 이동 중
    public float FarDistance => farDistance; // 빠른 이동 거리 (테스트용)
    public bool IsFollowing => followTarget != null; // 109일차: 동료로 따라다니는 중
    public int FollowTeleportCount { get; private set; } // 따라가다 순간이동한 횟수 (테스트용)

    private void Awake() // 준비
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = walkSpeed;
        agent.stoppingDistance = Mathf.Min(arriveDistance, 0.2f);
        agent.autoBraking = true;

        if (model != null)
        {
            modelBasePosition = model.localPosition;
            modelBaseScale = model.localScale;
        }

        motionTime = (GetInstanceID() & 1023) * 0.013f; // NPC마다 다른 박자
        targetPosition = transform.position;
        targetFacing = transform.rotation;

        if (speech != null)
        {
            speech.gameObject.SetActive(false);
        }
    }

    public void GoTo(NpcLocationPoint point, Vector3 position, Quaternion facing, string activity, bool hideWhenArrived, bool snap) // 일정 위치로 이동
    {
        CurrentPoint = point;
        currentLocationId = point != null ? point.LocationId : string.Empty;
        currentActivity = activity;
        hideOnArrival = hideWhenArrived;
        targetFacing = facing;
        targetPosition = NavMesh.SamplePosition(position, out NavMeshHit hit, 2.5f, NavMesh.AllAreas) ? hit.position : position;
        stuckTimer = 0f;
        repathCount = 0;

        if (snap || !agent.isOnNavMesh)
        {
            Arrive(true);
            return;
        }

        SetInside(false);
        arrived = false;
        agent.isStopped = isTalking; // 대화 중에는 끝날 때까지 기다렸다가 출발

        if (!agent.SetDestination(targetPosition))
        {
            Arrive(true); // 길을 못 찾으면 바로 옮김
        }
    }

    public void Say(string text, float seconds) // 말풍선 표시
    {
        if (speech == null || isInside || string.IsNullOrEmpty(text))
        {
            return;
        }

        speech.text = $"<mark=#1C1C20D0 padding=\"14,14,6,6\">{text}</mark>";
        speech.gameObject.SetActive(true);
        speechTimer = Mathf.Max(1f, seconds);
    }

    public void SetQuestMarker(NpcQuestMarker marker) // 93일차: 이름표 위 의뢰 표시 (! 전달 가능 · ? 진행 중)
    {
        if (marker == questMarker)
        {
            return;
        }

        questMarker = marker;
        RefreshNameTag();
    }

    public void SetEventMarker(bool ready) // 94일차: 이름표 위 이벤트 표시 (… 말을 걸면 이야기 시작, 의뢰 표시보다 먼저)
    {
        if (ready == hasEventMarker)
        {
            return;
        }

        hasEventMarker = ready;
        RefreshNameTag();
    }

    private void RefreshNameTag() // 이름표 = 표시 + 이름 · 직업
    {
        if (nameTag == null)
        {
            return;
        }

        if (nameTagBaseText == null)
        {
            nameTagBaseText = nameTag.text; // 생성 도구가 만든 이름 · 직업 문구
        }

        if (hasEventMarker)
        {
            nameTag.text = $"<size=170%><b><color=#F07A9A>…</color></b></size>\n{nameTagBaseText}";
            return;
        }

        switch (questMarker)
        {
            case NpcQuestMarker.Ready:
                nameTag.text = $"<size=170%><b><color=#F2B84B>!</color></b></size>\n{nameTagBaseText}";
                break;
            case NpcQuestMarker.Active:
                nameTag.text = $"<size=150%><b><color=#C9C4B8>?</color></b></size>\n{nameTagBaseText}";
                break;
            default:
                nameTag.text = nameTagBaseText;
                break;
        }
    }

    public void SetTalking(bool talking, Vector3 partnerPosition) // 91일차: 대화 창이 열린 동안 멈춰서 상대를 바라봄
    {
        isTalking = talking;
        talkTarget = partnerPosition;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = talking;

            if (talking)
            {
                agent.velocity = Vector3.zero; // 미끄러지지 않고 바로 멈춤
            }
        }

        if (talking && speech != null)
        {
            speech.gameObject.SetActive(false);
            speechTimer = 0f;
        }

        if (talking)
        {
            FaceTowards(partnerPosition);
        }
    }

    public void FaceTowards(Vector3 worldPosition) // 말을 건 사람 바라보기
    {
        Vector3 direction = worldPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    private void Update() // 도착 확인 · 방향 · 걷기 흔들림
    {
        float deltaTime = Time.deltaTime;

        if (IsFollowing) // 109일차: 동료는 일정 대신 플레이어를 따라감
        {
            UpdateFollow(deltaTime);
        }
        else if (!arrived && !isTalking && agent.isOnNavMesh && !agent.pathPending)
        {
            if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                Arrive(true);
            }
            else if (agent.remainingDistance <= arriveDistance)
            {
                bool notThere = FlatDistance(transform.position, targetPosition) > arriveDistance + 0.5f;

                if (agent.pathStatus == NavMeshPathStatus.PathPartial && notThere && repathCount < 6) // 107일차: 아주 먼 길은 중간까지만 찾을 수 있어 이어서 다시 찾음
                {
                    repathCount++;
                    agent.SetDestination(targetPosition);
                }
                else
                {
                    Arrive(notThere); // 끝까지 못 가면 목적지로 옮김
                }
            }
            else if (agent.velocity.sqrMagnitude < 0.01f || agent.remainingDistance <= arriveDistance + 1.2f) // 110일차: 코앞에서 다른 NPC에 막혀 맴도는 경우도
            {
                stuckTimer += deltaTime;

                if (stuckTimer >= stuckSeconds)
                {
                    Arrive(true); // 막혀 있으면 목적지로 옮김
                }
            }
            else
            {
                stuckTimer = 0f;
            }
        }

        UpdateTravelSpeed();

        if (isTalking && !isInside)
        {
            Transform partner = FindPlayer();
            Vector3 toPartner = (partner != null ? partner.position : talkTarget) - transform.position;
            toPartner.y = 0f;

            if (toPartner.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toPartner), 1f - Mathf.Exp(-8f * deltaTime));
            }
        }
        else if (arrived && !isInside)
        {
            Quaternion desired = targetFacing;
            Transform target = FindPlayer();

            if (target != null)
            {
                Vector3 toPlayer = target.position - transform.position;
                toPlayer.y = 0f;

                if (toPlayer.sqrMagnitude <= lookDistance * lookDistance && toPlayer.sqrMagnitude > 0.04f)
                {
                    desired = Quaternion.LookRotation(toPlayer);
                }
            }

            transform.rotation = Quaternion.Slerp(transform.rotation, desired, 1f - Mathf.Exp(-6f * deltaTime));
        }

        AnimateModel(deltaTime);

        if (speechTimer > 0f)
        {
            speechTimer -= deltaTime;

            if (speechTimer <= 0f && speech != null)
            {
                speech.gameObject.SetActive(false);
            }
        }
    }

    private void LateUpdate() // 이름표·말풍선이 카메라를 향하게
    {
        if (viewCamera == null)
        {
            Camera main = Camera.main;
            viewCamera = main != null ? main.transform : null;
        }

        if (viewCamera == null)
        {
            return;
        }

        Transform target = FindPlayer();
        bool nearby = target != null && (target.position - transform.position).sqrMagnitude <= nameTagDistance * nameTagDistance;

        if (nameTag != null)
        {
            bool show = nearby && !isInside;

            if (nameTag.gameObject.activeSelf != show)
            {
                nameTag.gameObject.SetActive(show);
            }

            if (show)
            {
                nameTag.transform.rotation = Quaternion.LookRotation(nameTag.transform.position - viewCamera.position);
            }
        }

        if (speech != null && speech.gameObject.activeSelf)
        {
            speech.transform.rotation = Quaternion.LookRotation(speech.transform.position - viewCamera.position);
        }
    }

    private void Arrive(bool warp) // 도착 처리
    {
        if (warp && agent.isOnNavMesh)
        {
            agent.Warp(targetPosition);
        }
        else if (warp)
        {
            transform.position = targetPosition;
        }

        if (agent.isOnNavMesh)
        {
            agent.ResetPath();
        }

        if (warp)
        {
            transform.rotation = targetFacing;
        }

        arrived = true;
        stuckTimer = 0f;
        SetInside(hideOnArrival);
        UpdateTravelSpeed();
    }

    // ------------------------------------------------------------ 109일차: 동료 따라다니기

    public void BeginFollow(Transform target, string activity) // 동료가 되어 따라다니기 시작
    {
        followTarget = target;
        hasEngage = false;
        CurrentPoint = null;
        currentLocationId = string.Empty;
        currentActivity = activity;
        hideOnArrival = false;
        arrived = false;
        StopKey = null;
        followRepathTimer = 0f;
        followSide = (GetInstanceID() & 1) == 0 ? 1f : -1f;
        SetInside(false);
    }

    public void EndFollow() // 동료를 그만두고 일정으로 돌아갈 준비 (NpcManager가 다음 일정 위치로 보냄)
    {
        followTarget = null;
        hasEngage = false;
        arrived = true;
        StopKey = null;
        currentActivity = string.Empty;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.ResetPath();
        }

        if (agent != null)
        {
            agent.speed = walkSpeed;
            agent.acceleration = 8f;
            agent.autoBraking = true;
            agent.stoppingDistance = Mathf.Min(arriveDistance, 0.2f);
        }
    }

    public void SetEngageTarget(bool engage, Vector3 position) // 전투형 동료 : 적에게 다가가기 (false면 다시 따라가기)
    {
        hasEngage = engage;
        engagePosition = position;

        if (engage)
        {
            followRepathTimer = 0f;
        }
    }

    public bool TeleportNear(Vector3 position) // 가까운 걸을 수 있는 곳으로 옮기기 (없으면 false)
    {
        if (!NavMesh.SamplePosition(position, out NavMeshHit hit, 8f, NavMesh.AllAreas))
        {
            return false;
        }

        if (agent.isOnNavMesh)
        {
            agent.Warp(hit.position);
        }
        else
        {
            transform.position = hit.position;
            agent.Warp(hit.position);
        }

        FollowTeleportCount++;
        return true;
    }

    private void UpdateFollow(float deltaTime)
    {
        Vector3 toTarget = followTarget.position - transform.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;
        Vector3 behind = followTarget.position - followTarget.forward * 2.4f + followTarget.right * (1.3f * followSide);

        if (distance > followTeleportDistance || !agent.isOnNavMesh) // 너무 멀면 플레이어 뒤로 (물 위라 설 곳이 없으면 가장 가까운 물가에서 기다림)
        {
            if (!TeleportNear(behind) && agent.isOnNavMesh)
            {
                WaitAtShore();
            }

            return;
        }

        if (isTalking)
        {
            return;
        }

        float speed = hasEngage ? walkSpeed * 2.2f : distance > 10f ? walkSpeed * 2.8f : distance > 5f ? walkSpeed * 1.6f : walkSpeed;

        if (!Mathf.Approximately(agent.speed, speed))
        {
            agent.speed = speed;
            agent.acceleration = Mathf.Max(8f, speed * 4f);
        }

        agent.autoBraking = true;
        agent.stoppingDistance = hasEngage ? 1.6f : 0.8f;
        followRepathTimer -= deltaTime;

        if (followRepathTimer <= 0f && !agent.pathPending)
        {
            followRepathTimer = 0.25f;
            Vector3 goal = hasEngage ? engagePosition : behind;

            if (NavMesh.SamplePosition(goal, out NavMeshHit hit, 6f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
            else if (!hasEngage)
            {
                WaitAtShore();
            }
        }

        if (!hasEngage && distance < 3.6f && agent.velocity.sqrMagnitude < 0.05f && toTarget.sqrMagnitude > 0.04f) // 가까이 서 있으면 플레이어를 봄
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toTarget), 1f - Mathf.Exp(-5f * deltaTime));
        }
    }

    private void WaitAtShore() // 플레이어가 물 위에 있으면 플레이어와 가장 가까운 물가로 가서 기다림 (1초마다 찾음)
    {
        if (Time.time < shoreSearchTime)
        {
            return;
        }

        shoreSearchTime = Time.time + 1f;

        if (!NavMesh.SamplePosition(followTarget.position, out NavMeshHit shore, ShoreSearchRadius, NavMesh.AllAreas))
        {
            return; // 너무 먼 바다 : 지금 자리에서 기다림
        }

        float gap = FlatDistance(shore.position, transform.position);

        if (gap > followTeleportDistance)
        {
            TeleportNear(shore.position);
        }
        else if (gap > 1.5f)
        {
            agent.SetDestination(shore.position);
        }
    }

    // 107일차: 플레이어에게서 멀면 보이지 않는 동안 빠르게, 보이는 곳에서 먼 길이면 서둘러 걷기
    private void UpdateTravelSpeed()
    {
        if (IsFollowing) // 109일차: 동료는 따라가기 속도
        {
            IsTravelingFast = false;
            return;
        }

        float multiplier = 1f;
        bool fast = false;

        if (!arrived && !isTalking)
        {
            Transform target = FindPlayer();
            float playerDistance = target != null ? FlatDistance(target.position, transform.position) : float.MaxValue;
            float remaining = agent.isOnNavMesh && !agent.pathPending ? agent.remainingDistance : float.PositiveInfinity;
            fast = playerDistance > farDistance;
            multiplier = fast ? farSpeedMultiplier : remaining > longTripDistance ? longTripMultiplier : 1f;
        }

        IsTravelingFast = fast;
        float speed = walkSpeed * multiplier;

        if (!Mathf.Approximately(agent.speed, speed))
        {
            agent.speed = speed;
            agent.acceleration = Mathf.Max(8f, speed * 4f);
            agent.autoBraking = !fast;
        }
    }

    public float EstimateTravelSeconds(float pathLength) // 107일차: 일정 이동 시간 어림 (플레이어가 한쪽 끝에 있다고 봄 : 가까운 곳은 걷거나 서둘러 걷고, 나머지는 빠르게)
    {
        float near = Mathf.Min(pathLength, farDistance * 2f);
        float calm = Mathf.Min(near, longTripDistance);
        float hurry = near - calm;
        float far = pathLength - near;
        return calm / walkSpeed + hurry / (walkSpeed * longTripMultiplier) + far / (walkSpeed * farSpeedMultiplier);
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        return new Vector2(a.x - b.x, a.z - b.z).magnitude;
    }

    private void SetInside(bool inside) // 집 안으로 들어가기 (보이지 않음)
    {
        isInside = inside;

        if (model != null && model.gameObject.activeSelf == inside)
        {
            model.gameObject.SetActive(!inside);
        }

        if (bodyCollider != null)
        {
            bodyCollider.enabled = !inside;
        }

        if (inside && speech != null)
        {
            speech.gameObject.SetActive(false);
            speechTimer = 0f;
        }
    }

    private void AnimateModel(float deltaTime) // 걷기 흔들림 · 숨쉬기
    {
        if (model == null || isInside)
        {
            return;
        }

        float speed = agent.velocity.magnitude;
        bool moving = speed > 0.1f;
        motionTime += deltaTime * (moving ? MotionFrequency() : 2f);
        float wave = Mathf.Sin(motionTime);
        Vector3 offset = Vector3.zero;
        Vector3 euler = Vector3.zero;
        Vector3 scale = modelBaseScale;

        switch (motionStyle)
        {
            case NpcMotionStyle.Slither: // 위아래 대신 좌우로 흔들림
                euler = new Vector3(0f, wave * (moving ? 7f : 2f), wave * (moving ? 2f : 0f));
                scale.y *= moving ? 1f : 1f + wave * 0.01f;
                break;
            case NpcMotionStyle.Hover: // 멈춰 있어도 둥실 떠다님, 움직일 때 앞으로 기울어짐
                offset = Vector3.up * (0.06f + Mathf.Sin(Time.time * 1.6f + motionTime * 0.2f) * 0.05f);
                euler = new Vector3(moving ? 6f : 0f, 0f, wave * 1.5f);
                break;
            case NpcMotionStyle.Gallop: // 크게 오르내리고 앞뒤로 기울어짐
                offset = moving ? Vector3.up * Mathf.Abs(wave) * 0.07f : Vector3.zero;
                euler = moving ? new Vector3(wave * 2.5f, 0f, 0f) : Vector3.zero;
                scale.y *= moving ? 1f : 1f + wave * 0.01f;
                break;
            case NpcMotionStyle.Skitter: // 빠르고 작은 떨림
                offset = moving ? Vector3.up * Mathf.Abs(wave) * 0.02f : Vector3.zero;
                euler = moving ? new Vector3(0f, 0f, wave * 1.2f) : Vector3.zero;
                scale.y *= moving ? 1f : 1f + wave * 0.01f;
                break;
            case NpcMotionStyle.Jelly: // 눌렸다 늘어남 (부피 유지)
                float squash = wave * (moving ? 0.06f : 0.025f);
                scale = new Vector3(modelBaseScale.x * (1f - squash * 0.5f), modelBaseScale.y * (1f + squash), modelBaseScale.z * (1f - squash * 0.5f));
                break;
            case NpcMotionStyle.Hop: // 깡충깡충
                offset = moving ? Vector3.up * Mathf.Max(0f, wave) * 0.12f : Vector3.zero;
                scale.y *= moving ? 1f - Mathf.Max(0f, -wave) * 0.05f : 1f + wave * 0.008f;
                break;
            default: // 걷기 (90일차)
                if (moving)
                {
                    offset = Vector3.up * Mathf.Abs(wave) * 0.045f;
                    euler = new Vector3(0f, 0f, wave * 3f);
                }
                else
                {
                    scale.y *= 1f + wave * 0.012f;
                }

                break;
        }

        model.localPosition = modelBasePosition + offset;
        model.localRotation = Quaternion.Euler(euler);
        model.localScale = scale;
    }

    private float MotionFrequency() // 움직일 때 박자
    {
        switch (motionStyle)
        {
            case NpcMotionStyle.Slither: return 4.5f;
            case NpcMotionStyle.Hover: return 3f;
            case NpcMotionStyle.Gallop: return 9f;
            case NpcMotionStyle.Skitter: return 14f;
            case NpcMotionStyle.Jelly: return 6f;
            case NpcMotionStyle.Hop: return 8f;
            default: return 7.5f;
        }
    }

    private Transform FindPlayer() // 플레이어 찾기
    {
        if (player == null)
        {
            PlayerInventory inventory = FindFirstObjectByType<PlayerInventory>();
            player = inventory != null ? inventory.transform : null;
        }

        return player;
    }

#if UNITY_EDITOR
    public void EditorAssign(NpcCharacterData data, Transform modelRoot, TMP_Text tag, TMP_Text bubble, Collider body) // 생성 도구 전용
    {
        character = data;
        model = modelRoot;
        nameTag = tag;
        speech = bubble;
        bodyCollider = body;
    }

    public void EditorAssignBody(NpcMotionStyle motion, float speed) // 100일차: 하반신 모양별 움직임 · 속도 (생성 도구 전용)
    {
        motionStyle = motion;
        walkSpeed = Mathf.Max(0.2f, speed);
    }
#endif
}
