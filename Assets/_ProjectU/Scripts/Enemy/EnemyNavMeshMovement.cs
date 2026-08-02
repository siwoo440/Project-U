using UnityEngine; // Unity 기본 기능
using UnityEngine.AI; // NavMesh 이동 기능

[DisallowMultipleComponent] // 동일 이동 컴포넌트 중복 방지
[RequireComponent(typeof(EnemyCombatController))] // 적 전투 상태 관리자 요구
[RequireComponent(typeof(NavMeshAgent))] // NavMesh 이동 컴포넌트 요구
public sealed class EnemyNavMeshMovement : MonoBehaviour // 적 NavMesh 추적 이동 관리자
{
    [Header("References")] // 적 이동 참조 묶음
    [Tooltip("현재 적의 탐지와 전투 상태를 제공할 관리자입니다.")] // Inspector 전투 관리자 설명
    [SerializeField] private EnemyCombatController combatController; // 적 전투 상태 관리자

    [Tooltip("NavMesh 위에서 적을 이동시킬 Agent입니다.")] // Inspector NavMesh Agent 설명
    [SerializeField] private NavMeshAgent navMeshAgent; // 적 NavMesh 이동 Agent

    [Header("Path")] // 적 경로 설정 묶음
    [Tooltip("플레이어 목적지를 다시 계산하는 간격입니다.")] // Inspector 경로 갱신 간격 설명
    [SerializeField, Min(0.02f)] private float pathRefreshInterval = 0.15f; // 목적지 갱신 간격

    [Tooltip("공격 거리보다 조금 안쪽에서 정지하도록 빼는 거리입니다.")] // Inspector 정지 거리 보정 설명
    [SerializeField, Min(0f)] private float stoppingDistanceOffset = 0.15f; // 공격 정지 거리 보정값

    [Tooltip("시작 위치가 NavMesh 밖일 때 가장 가까운 NavMesh를 찾는 반경입니다.")] // Inspector NavMesh 검색 반경 설명
    [SerializeField, Min(0.1f)] private float navMeshSampleRadius = 3f; // NavMesh 시작 위치 검색 반경

    [Tooltip("적이 NavMesh 밖으로 벗어났을 때 다시 배치할지 설정합니다.")] // Inspector NavMesh 복구 설명
    [SerializeField] private bool recoverWhenOffNavMesh = true; // NavMesh 이탈 자동 복구 여부

    [Header("Agent")] // NavMesh Agent 설정 묶음
    [Tooltip("적 이동 가속도입니다.")] // Inspector 가속도 설명
    [SerializeField, Min(0.1f)] private float acceleration = 12f; // 적 이동 가속도

    [Tooltip("목적지 근처에서 부드럽게 감속합니다.")] // Inspector 자동 감속 설명
    [SerializeField] private bool autoBraking = true; // 자동 감속 사용 여부

    [Header("Debug")] // 적 이동 Debug 설정 묶음
    [Tooltip("NavMesh 배치 실패와 경로 상태를 Console에 출력합니다.")] // Inspector Debug 로그 설명
    [SerializeField] private bool logMovementResults = true; // 적 이동 결과 로그 여부

    [Header("Runtime")] // 적 이동 실행 상태 묶음
    [Tooltip("현재 NavMesh 위에 정상 배치되어 있는지 표시합니다.")] // Inspector NavMesh 배치 상태 설명
    [SerializeField] private bool isOnNavMesh; // 현재 NavMesh 배치 여부

    [Tooltip("현재 목적지까지 이동 중인지 표시합니다.")] // Inspector 이동 상태 설명
    [SerializeField] private bool isMoving; // 현재 추적 이동 여부

    [Tooltip("현재 목적지까지 남은 거리입니다.")] // Inspector 남은 거리 설명
    [SerializeField] private float remainingDistance; // 현재 목적지 남은 거리

    [Tooltip("마지막으로 전달한 NavMesh 목적지입니다.")] // Inspector 마지막 목적지 설명
    [SerializeField] private Vector3 lastDestination; // 마지막 NavMesh 목적지

    private float nextPathRefreshTime; // 다음 경로 갱신 시각
    private float nextRecoveryAttemptTime; // 다음 NavMesh 복구 시도 시각

    public bool IsOnNavMesh => isOnNavMesh; // NavMesh 배치 여부 제공
    public bool IsMoving => isMoving; // 현재 추적 이동 여부 제공
    public float RemainingDistance => remainingDistance; // 목적지까지 남은 거리 제공

    private void Reset() // 컴포넌트 최초 추가 시 참조 자동 연결
    {
        combatController = GetComponent<EnemyCombatController>(); // 같은 적의 전투 관리자 연결
        navMeshAgent = GetComponent<NavMeshAgent>(); // 같은 적의 NavMesh Agent 연결
    }

    private void Awake() // 적 NavMesh 이동 참조 초기화
    {
        if (combatController == null) // 적 전투 관리자 참조 확인
        {
            combatController = GetComponent<EnemyCombatController>(); // 같은 적에서 전투 관리자 자동 검색
        }

        if (navMeshAgent == null) // NavMesh Agent 참조 확인
        {
            navMeshAgent = GetComponent<NavMeshAgent>(); // 같은 적에서 NavMesh Agent 자동 검색
        }

        if (combatController == null || navMeshAgent == null) // 필수 이동 참조 확인
        {
            Debug.LogError("EnemyNavMeshMovement에 EnemyCombatController와 NavMeshAgent가 필요합니다.", this); // 필수 참조 누락 오류 출력
            enabled = false; // 적 이동 기능 비활성화
            return; // 초기화 처리 중단
        }

        ConfigureAgentFromCombatData(); // 적 데이터 기준 Agent 설정 적용
    }

    private void Start() // Scene 시작 후 적을 NavMesh에 배치
    {
        TryPlaceOnNearestNavMesh(); // 현재 위치 주변 NavMesh 배치 시도
    }

    private void Update() // 매 프레임 적 상태에 따른 NavMesh 이동 처리
    {
        if (combatController == null || navMeshAgent == null) // 필수 참조 존재 여부 확인
        {
            return; // 적 이동 처리 중단
        }

        RefreshRuntimeValues(); // Inspector 실행 상태 갱신

        if (!navMeshAgent.enabled || !gameObject.activeInHierarchy) // Agent와 적 활성 상태 확인
        {
            isMoving = false; // 이동 상태 해제
            return; // NavMesh 이동 처리 중단
        }

        if (!navMeshAgent.isOnNavMesh) // 현재 NavMesh 배치 상태 확인
        {
            HandleOffNavMesh(); // NavMesh 이탈 복구 처리
            return; // 경로 이동 처리 중단
        }

        EnemyCombatState currentState = combatController.CurrentState; // 현재 적 전투 상태 조회

        if (currentState != EnemyCombatState.Chasing) // 추적 상태 여부 확인
        {
            StopMovement(); // 추적 상태가 아니면 이동 정지
            return; // 목적지 갱신 처리 중단
        }

        Transform targetTransform = combatController.CurrentTarget; // 현재 플레이어 대상 조회

        if (targetTransform == null) // 플레이어 대상 존재 여부 확인
        {
            StopMovement(); // 대상이 없으면 이동 정지
            return; // 목적지 갱신 처리 중단
        }

        if (Time.time < nextPathRefreshTime) // 경로 갱신 시각 확인
        {
            isMoving = navMeshAgent.hasPath && !navMeshAgent.isStopped; // 현재 이동 상태 갱신
            return; // 경로 재계산 생략
        }

        nextPathRefreshTime = Time.time + pathRefreshInterval; // 다음 경로 갱신 시각 저장
        UpdateDestination(targetTransform.position); // 플레이어 현재 위치를 새로운 목적지로 설정
    }

    private void ConfigureAgentFromCombatData() // 적 전투 데이터 기준 NavMesh Agent 설정
    {
        EnemyCombatData combatData = combatController == null // 적 전투 관리자 존재 여부 확인
            ? null // 전투 관리자가 없으면 데이터 없음
            : combatController.CombatData; // 적 공통 전투 데이터 조회

        if (combatData == null || navMeshAgent == null) // 적 데이터와 Agent 존재 여부 확인
        {
            return; // Agent 설정 처리 중단
        }

        navMeshAgent.speed = combatData.MoveSpeed; // 적 데이터 이동 속도 적용
        navMeshAgent.angularSpeed = combatData.RotationSpeed; // 적 데이터 회전 속도 적용
        navMeshAgent.acceleration = acceleration; // 설정된 이동 가속도 적용
        navMeshAgent.stoppingDistance = Mathf.Max( // 정지 거리 계산 시작
            0.05f, // 최소 정지 거리
            combatData.AttackRange - stoppingDistanceOffset); // 공격 거리 안쪽 정지 거리 적용
        navMeshAgent.autoBraking = autoBraking; // 목적지 자동 감속 설정 적용
        navMeshAgent.updateRotation = false; // EnemyCombatController가 플레이어 방향 회전 담당
        navMeshAgent.updateUpAxis = true; // NavMesh 표면 높이와 기울기 적용
    }

    private void UpdateDestination(Vector3 targetPosition) // 플레이어 위치를 NavMesh 목적지로 갱신
    {
        if (!navMeshAgent.isOnNavMesh) // Agent NavMesh 배치 상태 확인
        {
            return; // 목적지 설정 처리 중단
        }

        ResumeMovement(); // 이전 상태에서 정지된 Agent 이동 재개
        lastDestination = targetPosition; // 마지막 플레이어 목적지 저장
        bool destinationAccepted = navMeshAgent.SetDestination(targetPosition); // 새로운 NavMesh 경로 요청
        isMoving = destinationAccepted; // 경로 요청 결과를 이동 상태로 저장

        if (!destinationAccepted && logMovementResults) // 목적지 요청 실패와 로그 설정 확인
        {
            Debug.LogWarning($"{gameObject.name}의 NavMesh 목적지 설정에 실패했습니다.", this); // 목적지 설정 실패 경고 출력
        }
    }

    private void StopMovement() // 현재 NavMesh 경로 이동 정지
    {
        isMoving = false; // 이동 상태 해제

        if (!navMeshAgent.enabled || !navMeshAgent.isOnNavMesh) // Agent 사용 가능 상태 확인
        {
            return; // 이동 정지 처리 중단
        }

        navMeshAgent.isStopped = true; // 현재 Agent 이동 정지

        if (navMeshAgent.hasPath) // 기존 경로 존재 여부 확인
        {
            navMeshAgent.ResetPath(); // 기존 NavMesh 경로 제거
        }
    }

    private void ResumeMovement() // 정지된 NavMesh Agent 이동 재개
    {
        if (!navMeshAgent.enabled || !navMeshAgent.isOnNavMesh) // Agent 사용 가능 상태 확인
        {
            return; // 이동 재개 처리 중단
        }

        navMeshAgent.isStopped = false; // NavMesh Agent 이동 허용
    }

    private void HandleOffNavMesh() // NavMesh 밖 상태에서 자동 복구 처리
    {
        isOnNavMesh = false; // NavMesh 배치 상태 해제
        isMoving = false; // 적 이동 상태 해제
        remainingDistance = 0f; // 남은 거리 초기화

        if (!recoverWhenOffNavMesh || Time.time < nextRecoveryAttemptTime) // 자동 복구 설정과 재시도 시간 확인
        {
            return; // NavMesh 복구 시도 생략
        }

        nextRecoveryAttemptTime = Time.time + 1f; // 다음 NavMesh 복구 시각 저장
        TryPlaceOnNearestNavMesh(); // 현재 위치 주변 NavMesh 재배치 시도
    }

    [ContextMenu("Place On Nearest NavMesh")] // Inspector NavMesh 재배치 메뉴
    public bool TryPlaceOnNearestNavMesh() // 현재 위치에서 가장 가까운 NavMesh로 적 배치
    {
        if (navMeshAgent == null) // NavMesh Agent 참조 확인
        {
            return false; // NavMesh 배치 실패 반환
        }

        bool foundPosition = NavMesh.SamplePosition( // 가장 가까운 NavMesh 위치 검색
            transform.position, // 현재 적 위치 기준
            out NavMeshHit navMeshHit, // 검색된 NavMesh 위치 결과
            navMeshSampleRadius, // 검색할 최대 반경
            NavMesh.AllAreas); // 모든 NavMesh Area 검색

        if (!foundPosition) // 주변 NavMesh 검색 결과 확인
        {
            isOnNavMesh = false; // NavMesh 배치 상태 해제

            if (logMovementResults) // 이동 로그 사용 여부 확인
            {
                Debug.LogError( // NavMesh 배치 실패 로그 시작
                    $"{gameObject.name} 주변 {navMeshSampleRadius:0.##}m 안에 NavMesh가 없습니다. " // 검색 반경 안내
                    + "NavMeshSurface를 Bake하고 적을 파란색 NavMesh 위에 배치해야 합니다.", // 해결 방향 안내
                    this); // 현재 적을 Log Context로 지정
            }

            return false; // NavMesh 배치 실패 반환
        }

        if (!navMeshAgent.enabled) // NavMesh Agent 활성 상태 확인
        {
            navMeshAgent.enabled = true; // Agent 활성화
        }

        bool warped = navMeshAgent.Warp(navMeshHit.position); // 검색한 NavMesh 위치로 적 이동
        isOnNavMesh = warped && navMeshAgent.isOnNavMesh; // 최종 NavMesh 배치 상태 저장

        if (!isOnNavMesh) // Agent 배치 실패 여부 확인
        {
            if (logMovementResults) // 이동 로그 사용 여부 확인
            {
                Debug.LogError($"{gameObject.name}을 NavMesh 위에 배치하지 못했습니다.", this); // Agent 배치 실패 오류 출력
            }

            return false; // NavMesh 배치 실패 반환
        }

        ConfigureAgentFromCombatData(); // 적 데이터 기준 Agent 설정 재적용
        ResumeMovement(); // NavMesh Agent 이동 허용
        nextPathRefreshTime = 0f; // 즉시 경로를 갱신하도록 시각 초기화

        if (logMovementResults) // 이동 로그 사용 여부 확인
        {
            Debug.Log($"{gameObject.name} NavMesh 배치 완료 / {navMeshHit.position}", this); // NavMesh 배치 성공 결과 출력
        }

        return true; // NavMesh 배치 성공 반환
    }

    private void RefreshRuntimeValues() // Inspector 적 이동 실행 상태 갱신
    {
        isOnNavMesh = navMeshAgent.enabled && navMeshAgent.isOnNavMesh; // 현재 NavMesh 배치 여부 갱신

        if (!isOnNavMesh) // NavMesh 배치 상태 확인
        {
            remainingDistance = 0f; // NavMesh 밖에서는 남은 거리 초기화
            return; // 실행 상태 갱신 종료
        }

        remainingDistance = navMeshAgent.pathPending // 경로 계산 중인지 확인
            ? 0f // 경로 계산 중에는 남은 거리 초기값 사용
            : navMeshAgent.remainingDistance; // 계산된 목적지 남은 거리 사용
    }

    private void OnDisable() // 적 이동 컴포넌트 비활성화 처리
    {
        if (navMeshAgent == null) // NavMesh Agent 존재 여부 확인
        {
            return; // 비활성화 처리 중단
        }

        if (navMeshAgent.enabled && navMeshAgent.isOnNavMesh) // Agent와 NavMesh 상태 확인
        {
            navMeshAgent.isStopped = true; // 컴포넌트 비활성화 시 이동 정지
            navMeshAgent.ResetPath(); // 기존 경로 제거
        }

        isMoving = false; // 이동 상태 해제
    }

    private void OnValidate() // Inspector 적 NavMesh 이동 설정값 검증
    {
        pathRefreshInterval = Mathf.Max(0.02f, pathRefreshInterval); // 경로 갱신 간격 최소값 적용
        stoppingDistanceOffset = Mathf.Max(0f, stoppingDistanceOffset); // 정지 거리 보정값 음수 방지
        navMeshSampleRadius = Mathf.Max(0.1f, navMeshSampleRadius); // NavMesh 검색 반경 최소값 적용
        acceleration = Mathf.Max(0.1f, acceleration); // 이동 가속도 최소값 적용

        if (combatController == null) // 적 전투 관리자 참조 확인
        {
            combatController = GetComponent<EnemyCombatController>(); // 같은 적에서 전투 관리자 자동 연결
        }

        if (navMeshAgent == null) // NavMesh Agent 참조 확인
        {
            navMeshAgent = GetComponent<NavMeshAgent>(); // 같은 적에서 NavMesh Agent 자동 연결
        }
    }
}
