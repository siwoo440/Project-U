using System; // C# 이벤트 기능
using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
[RequireComponent(typeof(EnemyHealth))] // 적 체력 컴포넌트 요구
public sealed class EnemyCombatController : MonoBehaviour // 적 탐지와 시간 기반 공격 상태 관리자
{
    [Header("References")] // 적 전투 참조 묶음
    [Tooltip("체력, 피해 수신과 사망을 관리할 EnemyHealth입니다.")] // Inspector 적 체력 설명
    [SerializeField] private EnemyHealth enemyHealth; // 적 체력 관리자

    [Tooltip("적 공격이 시작되는 손 또는 몸 앞 위치입니다. 비우면 적 Transform을 사용합니다.")] // Inspector 공격 시작 위치 설명
    [SerializeField] private Transform attackOrigin; // 적 공격 시작 위치

    [Tooltip("현재 추적과 공격 대상으로 사용할 플레이어 전투 피해 수신기입니다.")] // Inspector 플레이어 대상 설명
    [SerializeField] private PlayerCombatDamageReceiver targetReceiver; // 현재 플레이어 피해 수신 대상

    [Header("Target Search")] // 플레이어 탐색 설정 묶음
    [Tooltip("Scene 시작과 대상 상실 후 PlayerCombatDamageReceiver를 자동 검색합니다.")] // Inspector 자동 탐색 설명
    [SerializeField] private bool autoFindPlayer = true; // 플레이어 자동 검색 여부

    [Tooltip("플레이어 대상이 없을 때 다시 검색할 간격입니다.")] // Inspector 재검색 간격 설명
    [SerializeField, Min(0.1f)] private float targetSearchInterval = 1f; // 플레이어 대상 재검색 간격

    [Header("Behaviour")] // 적 공통 행동 설정 묶음
    [Tooltip("플레이어를 인식한 동안 적 몸을 플레이어 방향으로 회전합니다.")] // Inspector 방향 회전 설명
    [SerializeField] private bool rotateTowardTarget = true; // 플레이어 방향 회전 여부

    [Tooltip("플레이어가 공격 거리 안에 있으면 준비 시간 후 자동 공격합니다.")] // Inspector 자동 공격 설명
    [SerializeField] private bool autoAttackWhenInRange = true; // 공격 범위 자동 공격 여부

    [Header("Debug")] // 적 전투 Debug 설정 묶음
    [Tooltip("상태 변경과 공격 단계 결과를 Console에 출력합니다.")] // Inspector 전투 로그 설명
    [SerializeField] private bool logCombatResults = true; // 적 전투 로그 출력 여부

    [Header("Runtime - Combat State")] // 적 전투 상태 실행값 묶음
    [Tooltip("현재 적 전투 상태입니다.")] // Inspector 현재 상태 설명
    [SerializeField] private EnemyCombatState currentState = EnemyCombatState.Idle; // 현재 적 전투 상태

    [Tooltip("현재 플레이어와의 수평 거리입니다.")] // Inspector 대상 거리 설명
    [SerializeField] private float currentTargetDistance = float.PositiveInfinity; // 현재 플레이어 거리

    [Tooltip("현재 플레이어를 인식하고 추적 중인지 표시합니다.")] // Inspector 추적 잠금 설명
    [SerializeField] private bool hasTargetLock; // 플레이어 추적 유지 여부

    [Tooltip("현재 Hit 상태가 끝날 때까지 남은 시간입니다.")] // Inspector 피격 상태 시간 설명
    [SerializeField] private float hitReactionRemaining; // 피격 상태 남은 시간

    [Header("Runtime - Attack Phase")] // 적 공격 단계 실행값 묶음
    [Tooltip("현재 공격 세부 단계입니다.")] // Inspector 공격 단계 설명
    [SerializeField] private EnemyAttackPhase currentAttackPhase = EnemyAttackPhase.Ready; // 현재 적 공격 단계

    [Tooltip("현재 공격 준비 또는 후딜레이의 진행 비율입니다.")] // Inspector 공격 진행률 설명
    [SerializeField, Range(0f, 1f)] private float attackPhaseNormalized; // 현재 공격 단계 진행 비율

    [Tooltip("현재 준비 또는 후딜레이를 포함한 공격 절차를 실행 중인지 표시합니다.")] // Inspector 공격 절차 실행 설명
    [SerializeField] private bool isAttackSequenceRunning; // 현재 공격 절차 실행 여부

    [Tooltip("다음 공격을 시작할 수 있을 때까지 남은 시간입니다.")] // Inspector 공격 대기시간 설명
    [SerializeField] private float attackCooldownRemaining; // 다음 공격까지 남은 시간

    [Tooltip("현재 공격 절차의 고유 번호입니다.")] // Inspector 현재 공격 번호 설명
    [SerializeField] private int currentAttackSequenceId; // 현재 적 공격 고유 번호

    [Tooltip("마지막 공격 판정이 플레이어 체력에 실제 적용되었는지 표시합니다.")] // Inspector 마지막 공격 적용 설명
    [SerializeField] private bool lastAttackHitApplied; // 마지막 적 공격 피해 적용 여부

    [Tooltip("Play Mode 시작 후 시작한 총 공격 횟수입니다.")] // Inspector 공격 횟수 설명
    [SerializeField] private int performedAttackCount; // 적 공격 시작 횟수

    private EnemyCombatData combatData; // 현재 적 공통 전투 데이터
    private Transform targetTransform; // 현재 플레이어 대상 Transform
    private Collider targetCollider; // 현재 플레이어 대상 Collider
    private float nextTargetSearchTime; // 다음 플레이어 검색 시각
    private float nextAttackTime; // 다음 적 공격 가능 시각
    private float hitReactionEndsAt; // 피격 상태 종료 시각
    private float attackPhaseStartedAt; // 현재 공격 단계 시작 시각
    private float attackPhaseEndsAt; // 현재 공격 단계 종료 시각
    private int attackSequenceCounter; // 적 공격 고유 번호 생성기

    public EnemyCombatState CurrentState => currentState; // 현재 적 전투 상태 제공
    public EnemyAttackPhase CurrentAttackPhase => currentAttackPhase; // 현재 적 공격 세부 단계 제공
    public EnemyCombatData CombatData => combatData; // 현재 적 전투 데이터 제공
    public Transform CurrentTarget => targetTransform; // 현재 플레이어 대상 제공
    public float CurrentTargetDistance => currentTargetDistance; // 현재 대상 거리 제공
    public bool HasTargetLock => hasTargetLock; // 플레이어 추적 여부 제공
    public bool IsAttackSequenceRunning => isAttackSequenceRunning; // 공격 절차 실행 여부 제공
    public float AttackPhaseNormalized => attackPhaseNormalized; // 현재 공격 단계 진행 비율 제공
    public float AttackCooldownRemaining => Mathf.Max(0f, nextAttackTime - Time.time); // 공격 대기시간 제공
    public int CurrentAttackSequenceId => currentAttackSequenceId; // 현재 공격 고유 번호 제공
    public int PerformedAttackCount => performedAttackCount; // 적 공격 실행 횟수 제공
    public bool LastAttackHitApplied => lastAttackHitApplied; // 마지막 공격 피해 적용 여부 제공

    public event Action<EnemyCombatState, EnemyCombatState> StateChanged; // 이전 상태와 새로운 상태 변경 이벤트
    public event Action<EnemyAttackPhase, EnemyAttackPhase> AttackPhaseChanged; // 이전 공격 단계와 새로운 공격 단계 변경 이벤트
    public event Action<int> AttackWindupStarted; // 공격 준비 시작 이벤트
    public event Action<CombatHitData> AttackHitFrameReached; // 실제 공격 판정 시점 이벤트
    public event Action<int> AttackRecoveryStarted; // 공격 후딜레이 시작 이벤트
    public event Action<int, bool> AttackSequenceFinished; // 공격 번호와 피해 적용 결과를 전달하는 완료 이벤트
    public event Action<int, string> AttackCancelled; // 공격 번호와 취소 사유를 전달하는 취소 이벤트
    public event Action<CombatHitData, bool> AttackPerformed; // 적 공격 정보와 실제 피해 적용 결과 이벤트

    private void Reset() // 컴포넌트 최초 추가 시 기본 참조 설정
    {
        enemyHealth = GetComponent<EnemyHealth>(); // 같은 적 오브젝트의 EnemyHealth 자동 연결
        attackOrigin = transform; // 현재 적 Transform을 기본 공격 위치로 연결
    }

    private void Awake() // 적 전투 참조 초기화
    {
        if (enemyHealth == null) // 적 체력 참조 연결 여부 확인
        {
            enemyHealth = GetComponent<EnemyHealth>(); // 같은 적 오브젝트에서 EnemyHealth 자동 검색
        }

        if (attackOrigin == null) // 공격 시작 위치 연결 여부 확인
        {
            attackOrigin = transform; // 현재 적 Transform을 기본 공격 위치로 사용
        }

        combatData = enemyHealth == null // EnemyHealth 검색 결과 확인
            ? null // EnemyHealth가 없으면 데이터 없음
            : enemyHealth.CombatData; // EnemyHealth에서 공통 전투 데이터 가져오기

        if (enemyHealth == null || combatData == null) // 적 체력과 공통 데이터 확인
        {
            Debug.LogError("EnemyCombatController에 EnemyHealth와 EnemyCombatData가 필요합니다.", this); // 필수 참조 누락 오류 출력
            enabled = false; // 적 전투 기능 비활성화
        }
    }

    private void OnEnable() // 적 체력 이벤트 연결
    {
        if (enemyHealth == null) // 적 체력 참조 확인
        {
            return; // 이벤트 연결 처리 중단
        }

        enemyHealth.Damaged += HandleDamaged; // 적 피격 이벤트 구독
        enemyHealth.Died += HandleDied; // 적 사망 이벤트 구독
        enemyHealth.Revived += HandleRevived; // 적 부활 이벤트 구독
    }

    private void Start() // Scene 시작 후 플레이어 대상 검색
    {
        if (autoFindPlayer) // 플레이어 자동 검색 설정 확인
        {
            FindPlayerTarget(); // Scene의 플레이어 전투 피해 수신기 검색
        }

        SetState(EnemyCombatState.Idle); // 시작 적 전투 상태 적용
        SetAttackPhase(EnemyAttackPhase.Ready); // 시작 공격 단계를 준비 상태로 적용
    }

    private void OnDisable() // 적 체력 이벤트와 공격 절차 연결 해제
    {
        if (enemyHealth != null) // 적 체력 참조 확인
        {
            enemyHealth.Damaged -= HandleDamaged; // 적 피격 이벤트 구독 해제
            enemyHealth.Died -= HandleDied; // 적 사망 이벤트 구독 해제
            enemyHealth.Revived -= HandleRevived; // 적 부활 이벤트 구독 해제
        }

        CancelAttackSequenceInternal("컴포넌트 비활성화", false); // 비활성화 시 현재 공격 절차 정리
    }

    private void Update() // 매 프레임 적 탐지와 시간 기반 공격 단계 갱신
    {
        attackCooldownRemaining = AttackCooldownRemaining; // Inspector 공격 대기시간 갱신
        hitReactionRemaining = Mathf.Max(0f, hitReactionEndsAt - Time.time); // Inspector 피격 상태 시간 갱신
        UpdateReadyFromCooldown(); // 공격 대기시간 종료 여부 갱신

        if (enemyHealth.IsDead) // 적 사망 상태 확인
        {
            CancelAttackSequenceInternal("사망 상태", false); // 사망 상태 공격 절차 정리
            SetState(EnemyCombatState.Dead); // 사망 상태 유지
            return; // 적 전투 처리 중단
        }

        if (Time.time < hitReactionEndsAt) // 현재 피격 반응 시간 확인
        {
            CancelAttackSequenceInternal("피격 경직", true); // 피격 중 실행 중인 공격 취소
            SetState(EnemyCombatState.Hit); // 피격 상태 유지
            return; // 탐지와 공격 처리 일시 중단
        }

        if (!IsTargetValid()) // 현재 플레이어 대상 유효성 확인
        {
            TrySearchTarget(); // 설정 간격에 따른 플레이어 재검색
        }

        if (!IsTargetValid()) // 재검색 후에도 플레이어 대상이 없는지 확인
        {
            CancelAttackSequenceInternal("대상 없음", false); // 대상 상실 공격 절차 정리
            ClearTargetRuntime(); // 플레이어 추적 실행값 초기화
            SetState(EnemyCombatState.Idle); // 적 대기 상태 적용
            return; // 적 전투 처리 중단
        }

        currentTargetDistance = GetPlanarDistance(transform.position, targetTransform.position); // 플레이어와 수평 거리 계산

        if (!hasTargetLock && currentTargetDistance > combatData.DetectionRange) // 최초 탐지 거리 밖인지 확인
        {
            SetState(EnemyCombatState.Idle); // 플레이어 미탐지 대기 상태 적용
            return; // 적 전투 처리 중단
        }

        if (!hasTargetLock) // 이번 프레임 최초 탐지 여부 확인
        {
            hasTargetLock = true; // 플레이어 추적 상태 시작
            LogMessage($"{combatData.DisplayName} 플레이어 탐지 / 거리 {currentTargetDistance:0.##}"); // 플레이어 탐지 결과 출력
        }

        if (currentTargetDistance > combatData.LoseTargetRange) // 플레이어 추적 해제 거리 확인
        {
            CancelAttackSequenceInternal("추적 해제 거리 초과", false); // 추적 해제 시 공격 절차 정리
            ClearTargetRuntime(); // 플레이어 추적 실행값 초기화
            SetState(EnemyCombatState.Idle); // 적 대기 상태 복귀
            return; // 적 전투 처리 중단
        }

        if (rotateTowardTarget && CanRotateTowardTarget()) // 회전 설정과 현재 공격 단계 확인
        {
            RotateToTarget(); // 플레이어 방향으로 적 회전
        }

        if (isAttackSequenceRunning) // 현재 공격 절차 실행 여부 확인
        {
            SetState(EnemyCombatState.Attacking); // 공격 중 전투 상태 유지
            UpdateAttackSequence(); // 현재 공격 준비 또는 후딜레이 갱신
            return; // 거리 기반 상태 전환 처리 생략
        }

        if (currentTargetDistance > combatData.AttackRange) // 플레이어가 공격 거리 밖인지 확인
        {
            SetState(EnemyCombatState.Chasing); // 플레이어 추적 상태 적용
            return; // 공격 처리 생략
        }

        SetState(EnemyCombatState.Attacking); // 플레이어가 공격 거리 안에 있는 상태 적용

        if (autoAttackWhenInRange) // 공격 범위 자동 공격 설정 확인
        {
            TryPerformAttack(); // 재사용 대기시간을 확인하고 공격 준비 시작
        }
    }

    public void AssignTarget(PlayerCombatDamageReceiver newTargetReceiver) // 외부 AI에서 플레이어 대상 지정
    {
        targetReceiver = newTargetReceiver; // 새로운 플레이어 피해 수신기 저장
        CacheTargetComponents(); // 플레이어 Transform과 Collider 저장
        hasTargetLock = IsTargetValid(); // 유효 대상 여부에 따라 추적 상태 적용
    }

    public void ClearTarget() // 외부 AI에서 현재 플레이어 대상 해제
    {
        CancelAttackSequenceInternal("대상 수동 해제", false); // 대상 해제 전 현재 공격 정리
        targetReceiver = null; // 플레이어 피해 수신기 참조 제거
        targetTransform = null; // 플레이어 Transform 참조 제거
        targetCollider = null; // 플레이어 Collider 참조 제거
        ClearTargetRuntime(); // 플레이어 추적 실행값 초기화
        SetState(EnemyCombatState.Idle); // 적 대기 상태 적용
    }

    public bool TryPerformAttack() // 현재 플레이어 대상에 대한 공격 준비 시작
    {
        if (currentState != EnemyCombatState.Attacking) // 현재 공격 상태 확인
        {
            return false; // 공격 상태가 아니면 공격 차단
        }

        if (!IsTargetValid()) // 플레이어 대상 유효성 확인
        {
            return false; // 플레이어 대상 없음 반환
        }

        if (isAttackSequenceRunning) // 기존 공격 절차 실행 여부 확인
        {
            return false; // 중복 공격 시작 차단
        }

        if (Time.time < nextAttackTime) // 적 공격 재사용 대기시간 확인
        {
            SetAttackPhase(EnemyAttackPhase.Cooldown); // 공격 대기 상태 표시
            return false; // 공격 대기시간 중 실행 차단
        }

        if (currentTargetDistance > combatData.AttackRange) // 플레이어 공격 시작 거리 확인
        {
            return false; // 공격 거리 밖 시작 차단
        }

        attackSequenceCounter++; // 새로운 적 공격 고유 번호 생성
        currentAttackSequenceId = attackSequenceCounter; // 현재 공격 고유 번호 저장
        performedAttackCount++; // 적 공격 시작 횟수 증가
        lastAttackHitApplied = false; // 이번 공격 피해 적용 상태 초기화
        isAttackSequenceRunning = true; // 공격 절차 실행 상태 적용
        BeginAttackPhase(EnemyAttackPhase.Windup, combatData.AttackWindupDuration); // 공격 준비 단계 시작
        AttackWindupStarted?.Invoke(currentAttackSequenceId); // 공격 준비 시작 이벤트 전달
        LogMessage($"{combatData.DisplayName} 공격 준비 시작 / 번호 {currentAttackSequenceId}"); // 공격 준비 결과 출력

        if (combatData.AttackWindupDuration <= 0f) // 공격 준비 시간이 없는지 확인
        {
            ResolveAttackHitFrame(); // 즉시 공격 판정 처리
            BeginRecoveryPhase(); // 즉시 공격 후딜레이 시작
        }

        return true; // 공격 준비 시작 성공 반환
    }

    public bool CancelCurrentAttack(string reason) // 외부 애니메이션과 상태 시스템에서 현재 공격 취소
    {
        return CancelAttackSequenceInternal(reason, true); // 공격 취소 후 기본 대기시간 적용
    }

    private void UpdateAttackSequence() // 현재 공격 준비 또는 후딜레이 진행
    {
        attackPhaseNormalized = CalculatePhaseNormalized(); // 현재 공격 단계 진행 비율 계산

        if (Time.time < attackPhaseEndsAt) // 현재 공격 단계 종료 시각 확인
        {
            return; // 현재 단계 유지
        }

        if (currentAttackPhase == EnemyAttackPhase.Windup) // 공격 준비 단계 종료 여부 확인
        {
            ResolveAttackHitFrame(); // 실제 전투 피해 판정 실행
            BeginRecoveryPhase(); // 공격 후딜레이 단계 시작
            return; // 현재 프레임 공격 처리 종료
        }

        if (currentAttackPhase == EnemyAttackPhase.Recovery) // 공격 후딜레이 종료 여부 확인
        {
            FinishAttackSequence(); // 공격 절차 완료와 재사용 대기시간 시작
        }
    }

    private void ResolveAttackHitFrame() // 공격 준비 종료 시 실제 플레이어 피해 판정 실행
    {
        if (!IsTargetValid()) // 플레이어 대상 유효성 확인
        {
            LogMessage($"{combatData.DisplayName} 공격 판정 실패 / 대상 없음"); // 대상 없음 결과 출력
            return; // 피해 판정 처리 중단
        }

        currentTargetDistance = GetPlanarDistance(transform.position, targetTransform.position); // 판정 순간 플레이어 거리 다시 계산
        Vector3 attackDirection = targetTransform.position - attackOrigin.position; // 적에서 플레이어로 향하는 공격 방향 계산
        attackDirection.y = 0f; // 공격 방향의 높이 차이 제거

        if (attackDirection.sqrMagnitude < 0.0001f) // 유효한 공격 방향 확인
        {
            attackDirection = transform.forward; // 적 전방 방향을 대체 공격 방향으로 사용
        }

        Vector3 hitPoint = targetCollider == null // 플레이어 Collider 존재 여부 확인
            ? targetTransform.position // Collider가 없으면 플레이어 위치 사용
            : targetCollider.ClosestPoint(attackOrigin.position); // Collider가 있으면 가장 가까운 충돌 지점 사용

        CombatHitData hitData = new CombatHitData( // 플레이어에게 전달할 적 공격 정보 생성
            gameObject, // 현재 적을 공격 주체로 전달
            null, // 적 공격에 사용하는 플레이어 ItemData 없음
            WeaponAttackType.Melee, // 적 기본 공격을 근접 공격으로 분류
            combatData.AttackDamage, // 적 데이터의 기본 공격 피해량 전달
            combatData.AttackImpactForce, // 플레이어 밀림에 사용할 충격량 전달
            hitPoint, // 플레이어 피격 지점 전달
            attackDirection, // 적 공격 진행 방향 전달
            targetCollider, // 실제 플레이어 충돌 Collider 전달
            currentAttackSequenceId, // 현재 적 공격 고유 번호 전달
            0); // 적 기본 공격을 첫 번째 단계로 전달

        AttackHitFrameReached?.Invoke(hitData); // 애니메이션과 효과용 공격 판정 시점 전달
        float validHitRange = combatData.AttackRange + combatData.AttackRangeGraceDistance; // 판정 허용 최대 거리 계산
        bool isWithinHitRange = currentTargetDistance <= validHitRange; // 판정 순간 공격 거리 확인
        bool damageApplied = isWithinHitRange && targetReceiver.ReceiveDamage(hitData); // 거리와 무적 판정을 포함한 플레이어 피해 적용
        lastAttackHitApplied = damageApplied; // 마지막 공격 피해 적용 상태 저장
        AttackPerformed?.Invoke(hitData, damageApplied); // 공격 정보와 실제 피해 적용 결과 전달

        if (!isWithinHitRange) // 플레이어가 판정 거리 밖인지 확인
        {
            LogMessage( // 공격 빗나감 결과 출력 시작
                $"{combatData.DisplayName} 공격 빗나감 / 거리 {currentTargetDistance:0.##} / " // 현재 거리 출력
                + $"허용 거리 {validHitRange:0.##}"); // 판정 허용 거리 출력
            return; // 빗나감 로그 처리 종료
        }

        LogAttackResult(damageApplied); // 플레이어 피해 적용 또는 무적 차단 결과 출력
    }

    private void BeginRecoveryPhase() // 공격 판정 이후 후딜레이 단계 시작
    {
        BeginAttackPhase(EnemyAttackPhase.Recovery, combatData.AttackRecoveryDuration); // 공격 후딜레이 시간 적용
        AttackRecoveryStarted?.Invoke(currentAttackSequenceId); // 공격 후딜레이 시작 이벤트 전달

        if (combatData.AttackRecoveryDuration <= 0f) // 공격 후딜레이 시간이 없는지 확인
        {
            FinishAttackSequence(); // 공격 절차 즉시 완료
        }
    }

    private void FinishAttackSequence() // 공격 후딜레이 종료와 재사용 대기시간 시작
    {
        int finishedSequenceId = currentAttackSequenceId; // 완료할 공격 고유 번호 저장
        bool finishedHitApplied = lastAttackHitApplied; // 완료할 공격 피해 결과 저장
        isAttackSequenceRunning = false; // 공격 절차 실행 상태 해제
        attackPhaseNormalized = 0f; // 공격 단계 진행 비율 초기화
        nextAttackTime = Time.time + combatData.AttackCooldown; // 다음 적 공격 가능 시각 저장
        SetAttackPhase( // 공격 대기시간 존재 여부에 따른 단계 적용
            combatData.AttackCooldown > 0f // 공격 대기시간 존재 여부 확인
                ? EnemyAttackPhase.Cooldown // 대기시간이 있으면 Cooldown 적용
                : EnemyAttackPhase.Ready); // 대기시간이 없으면 즉시 Ready 적용
        AttackSequenceFinished?.Invoke(finishedSequenceId, finishedHitApplied); // 공격 절차 완료 결과 전달
        LogMessage($"{combatData.DisplayName} 공격 종료 / 번호 {finishedSequenceId}"); // 공격 절차 완료 결과 출력
    }

    private bool CancelAttackSequenceInternal(string reason, bool applyCooldown) // 현재 공격 절차 취소와 실행 상태 정리
    {
        if (!isAttackSequenceRunning) // 취소할 공격 절차 존재 여부 확인
        {
            return false; // 공격 취소 대상 없음 반환
        }

        int cancelledSequenceId = currentAttackSequenceId; // 취소할 공격 번호 저장
        isAttackSequenceRunning = false; // 공격 절차 실행 상태 해제
        attackPhaseNormalized = 0f; // 공격 단계 진행 비율 초기화
        lastAttackHitApplied = false; // 취소된 공격 피해 상태 초기화
        nextAttackTime = applyCooldown // 공격 취소 대기시간 적용 여부 확인
            ? Time.time + combatData.AttackCooldown // 피격 취소 후 기본 대기시간 적용
            : 0f; // 사망과 대상 상실에서는 공격 대기시간 제거
        SetAttackPhase( // 취소 후 공격 단계 적용
            applyCooldown && combatData.AttackCooldown > 0f // 대기시간 적용 여부 확인
                ? EnemyAttackPhase.Cooldown // 공격 대기시간 단계 적용
                : EnemyAttackPhase.Ready); // 즉시 공격 준비 단계 적용
        AttackCancelled?.Invoke(cancelledSequenceId, reason); // 취소된 공격 번호와 사유 전달
        LogMessage($"{combatData.DisplayName} 공격 취소 / 번호 {cancelledSequenceId} / 사유 {reason}"); // 공격 취소 결과 출력
        return true; // 공격 취소 성공 반환
    }

    private void BeginAttackPhase(EnemyAttackPhase newPhase, float duration) // 새로운 공격 단계와 종료 시각 설정
    {
        SetAttackPhase(newPhase); // 새로운 공격 단계 적용
        attackPhaseStartedAt = Time.time; // 공격 단계 시작 시각 저장
        attackPhaseEndsAt = Time.time + Mathf.Max(0f, duration); // 공격 단계 종료 시각 저장
        attackPhaseNormalized = 0f; // 새로운 공격 단계 진행 비율 초기화
    }

    private float CalculatePhaseNormalized() // 현재 공격 단계의 시간 진행 비율 계산
    {
        float phaseDuration = attackPhaseEndsAt - attackPhaseStartedAt; // 현재 공격 단계 전체 시간 계산

        if (phaseDuration <= 0f) // 유효한 공격 단계 시간 확인
        {
            return 1f; // 시간이 없으면 완료 비율 반환
        }

        return Mathf.Clamp01((Time.time - attackPhaseStartedAt) / phaseDuration); // 현재 공격 단계 진행 비율 반환
    }

    private void UpdateReadyFromCooldown() // 공격 재사용 대기시간 종료 시 Ready 단계 복귀
    {
        if (isAttackSequenceRunning) // 공격 절차 실행 중인지 확인
        {
            return; // 실행 중에는 Cooldown 갱신 생략
        }

        if (currentAttackPhase != EnemyAttackPhase.Cooldown) // 현재 Cooldown 단계 여부 확인
        {
            return; // Cooldown 단계가 아니면 갱신 생략
        }

        if (Time.time < nextAttackTime) // 공격 재사용 대기시간 확인
        {
            return; // 남은 대기시간 유지
        }

        SetAttackPhase(EnemyAttackPhase.Ready); // 공격 준비 가능 단계 적용
    }

    private bool CanRotateTowardTarget() // 현재 공격 단계에서 플레이어 방향 회전 가능 여부 계산
    {
        if (!isAttackSequenceRunning) // 공격 절차 실행 여부 확인
        {
            return true; // 일반 추적과 대기 중 회전 허용
        }

        if (currentAttackPhase == EnemyAttackPhase.Windup) // 공격 준비 단계 여부 확인
        {
            return combatData.TrackTargetDuringWindup; // 데이터 설정에 따라 준비 중 추적 회전 허용
        }

        return false; // 공격 후딜레이 중 회전 차단
    }

    private void FindPlayerTarget() // Scene의 플레이어 전투 피해 수신기 자동 검색
    {
        targetReceiver = FindFirstObjectByType<PlayerCombatDamageReceiver>(); // 현재 Scene의 플레이어 피해 수신기 검색
        CacheTargetComponents(); // 플레이어 Transform과 Collider 저장
        nextTargetSearchTime = Time.time + targetSearchInterval; // 다음 대상 검색 시각 저장
    }

    private void TrySearchTarget() // 설정된 간격에 따라 플레이어 대상 재검색
    {
        if (!autoFindPlayer || Time.time < nextTargetSearchTime) // 자동 탐색 설정과 재검색 시각 확인
        {
            return; // 플레이어 재검색 생략
        }

        FindPlayerTarget(); // Scene의 플레이어 피해 수신기 다시 검색
    }

    private void CacheTargetComponents() // 플레이어 피해 수신기에서 Transform과 Collider 저장
    {
        if (targetReceiver == null) // 플레이어 피해 수신기 존재 확인
        {
            targetTransform = null; // 플레이어 Transform 참조 제거
            targetCollider = null; // 플레이어 Collider 참조 제거
            return; // 대상 정보 저장 중단
        }

        targetTransform = targetReceiver.transform; // 플레이어 Transform 저장
        targetCollider = targetReceiver.GetComponent<Collider>(); // Player 루트의 CharacterController 또는 Collider 검색

        if (targetCollider == null) // Player 루트 Collider 검색 결과 확인
        {
            targetCollider = targetReceiver.GetComponentInChildren<Collider>(); // Player 자식에서 Collider 대체 검색
        }
    }

    private bool IsTargetValid() // 현재 플레이어 대상 사용 가능 여부 계산
    {
        return targetReceiver != null // 플레이어 피해 수신기 존재 확인
            && targetReceiver.isActiveAndEnabled // 플레이어 피해 수신기 활성 상태 확인
            && targetReceiver.IsAlive // 플레이어 생존 상태 확인
            && targetTransform != null; // 플레이어 Transform 존재 확인
    }

    private void RotateToTarget() // 적을 플레이어 수평 방향으로 부드럽게 회전
    {
        Vector3 targetDirection = targetTransform.position - transform.position; // 적에서 플레이어로 향하는 방향 계산
        targetDirection.y = 0f; // 수평 회전만 사용하도록 높이 차이 제거

        if (targetDirection.sqrMagnitude < 0.0001f) // 유효한 회전 방향 확인
        {
            return; // 적 회전 처리 중단
        }

        Quaternion targetRotation = Quaternion.LookRotation(targetDirection.normalized); // 플레이어를 바라보는 목표 회전 계산
        transform.rotation = Quaternion.RotateTowards( // 현재 회전에서 목표 회전으로 이동
            transform.rotation, // 현재 적 회전
            targetRotation, // 플레이어 방향 목표 회전
            combatData.RotationSpeed * Time.deltaTime); // 초당 회전 속도 적용
    }

    private void HandleDamaged(CombatHitData hitData, float appliedDamage) // 적 피격과 공격 중단 처리
    {
        CancelAttackSequenceInternal("피격", true); // 공격 준비와 후딜레이를 피격으로 취소
        PlayerCombatDamageReceiver attackerPlayer = hitData.Attacker == null // 공격 주체 존재 여부 확인
            ? null // 공격 주체가 없으면 플레이어 없음
            : hitData.Attacker.GetComponentInParent<PlayerCombatDamageReceiver>(); // 공격 주체 부모에서 플레이어 피해 수신기 검색

        if (attackerPlayer != null) // 플레이어 공격으로 피격되었는지 확인
        {
            AssignTarget(attackerPlayer); // 공격한 플레이어를 즉시 추적 대상으로 지정
        }

        hitReactionEndsAt = Time.time + combatData.HitReactionDuration; // 피격 상태 종료 시각 저장
        hitReactionRemaining = combatData.HitReactionDuration; // Inspector 피격 상태 시간 즉시 적용
        SetState(EnemyCombatState.Hit); // 적 피격 상태 적용

        if (appliedDamage > 0f) // 실제 피해량 존재 여부 확인
        {
            LogMessage($"{combatData.DisplayName} 피격 상태 / {combatData.HitReactionDuration:0.##}초"); // 피격 상태 결과 출력
        }
    }

    private void HandleDied(CombatHitData killingHitData) // 적 사망 상태 처리
    {
        CancelAttackSequenceInternal("사망", false); // 사망 시 공격 절차 즉시 정리
        hitReactionEndsAt = 0f; // 피격 상태 종료 시각 초기화
        hitReactionRemaining = 0f; // 피격 상태 남은 시간 초기화
        hasTargetLock = false; // 플레이어 추적 상태 해제
        SetState(EnemyCombatState.Dead); // 적 사망 상태 적용
        LogMessage($"{combatData.DisplayName} 전투 상태 종료"); // 적 전투 종료 결과 출력
    }

    private void HandleRevived() // 적 부활 후 전투 상태 초기화
    {
        nextAttackTime = 0f; // 공격 재사용 대기시간 초기화
        hitReactionEndsAt = 0f; // 피격 상태 종료 시각 초기화
        attackCooldownRemaining = 0f; // Inspector 공격 대기시간 초기화
        hitReactionRemaining = 0f; // Inspector 피격 상태 시간 초기화
        performedAttackCount = 0; // 적 공격 실행 횟수 초기화
        attackSequenceCounter = 0; // 적 공격 고유 번호 초기화
        currentAttackSequenceId = 0; // 현재 공격 고유 번호 초기화
        lastAttackHitApplied = false; // 마지막 공격 피해 상태 초기화
        isAttackSequenceRunning = false; // 공격 절차 실행 상태 초기화
        attackPhaseNormalized = 0f; // 공격 단계 진행 비율 초기화
        SetAttackPhase(EnemyAttackPhase.Ready); // 공격 준비 단계 복귀
        ClearTargetRuntime(); // 플레이어 추적 실행값 초기화
        SetState(EnemyCombatState.Idle); // 적 대기 상태 복귀
    }

    private void ClearTargetRuntime() // 플레이어 추적 실행 상태 초기화
    {
        hasTargetLock = false; // 플레이어 추적 상태 해제
        currentTargetDistance = float.PositiveInfinity; // 대상 거리를 무한대로 초기화
    }

    private void SetState(EnemyCombatState newState) // 적 공통 전투 상태 변경
    {
        if (currentState == newState) // 기존 상태와 새로운 상태 비교
        {
            return; // 동일 상태 중복 처리 생략
        }

        EnemyCombatState previousState = currentState; // 변경 전 상태 저장
        currentState = newState; // 새로운 적 전투 상태 적용
        StateChanged?.Invoke(previousState, currentState); // 이전 상태와 현재 상태 전달
        LogMessage($"{combatData.DisplayName} 상태 변경 / {previousState} -> {currentState}"); // 상태 변경 결과 출력
    }

    private void SetAttackPhase(EnemyAttackPhase newPhase) // 적 공격 세부 단계 변경
    {
        if (currentAttackPhase == newPhase) // 기존 공격 단계와 새로운 단계 비교
        {
            return; // 동일 공격 단계 중복 처리 생략
        }

        EnemyAttackPhase previousPhase = currentAttackPhase; // 변경 전 공격 단계 저장
        currentAttackPhase = newPhase; // 새로운 적 공격 단계 적용
        AttackPhaseChanged?.Invoke(previousPhase, currentAttackPhase); // 이전 단계와 현재 단계 전달
        LogMessage($"{combatData.DisplayName} 공격 단계 / {previousPhase} -> {currentAttackPhase}"); // 공격 단계 변경 결과 출력
    }

    private float GetPlanarDistance(Vector3 fromPosition, Vector3 toPosition) // 두 위치의 수평 거리 계산
    {
        Vector3 difference = toPosition - fromPosition; // 두 위치 차이 계산
        difference.y = 0f; // 높이 차이 제거
        return difference.magnitude; // 수평 거리 반환
    }

    private void LogAttackResult(bool damageApplied) // 적 공격 피해 적용 결과 출력
    {
        if (!logCombatResults) // 적 전투 로그 사용 여부 확인
        {
            return; // 공격 결과 로그 출력 생략
        }

        string resultMessage = damageApplied // 실제 플레이어 피해 적용 여부 확인
            ? "피해 적용" // 플레이어 체력 감소 결과
            : "회피 또는 피격 무적으로 차단"; // 플레이어 전투 무적 차단 결과
        Debug.Log( // 적 공격 결과 로그 시작
            $"{combatData.DisplayName} 공격 판정 / 번호 {currentAttackSequenceId} / " // 적 이름과 공격 번호 출력
            + $"피해 {combatData.AttackDamage:0.##} / 결과 {resultMessage}", // 공격 피해량과 결과 출력
            this); // 현재 적을 Log Context로 지정
    }

    private void LogMessage(string message) // 적 전투 일반 결과 출력
    {
        if (!logCombatResults) // 적 전투 로그 사용 여부 확인
        {
            return; // 일반 전투 로그 출력 생략
        }

        Debug.Log(message, this); // 전달받은 적 전투 문구 출력
    }

    private void OnDrawGizmosSelected() // 적 탐지와 공격 범위 Scene Gizmo 표시
    {
        EnemyCombatData gizmoData = combatData; // Play Mode에서 저장된 적 데이터 사용

        if (gizmoData == null && enemyHealth != null) // Edit Mode 적 데이터 대체 검색 여부 확인
        {
            gizmoData = enemyHealth.CombatData; // EnemyHealth에 연결된 적 데이터 사용
        }

        if (gizmoData == null) // 표시할 적 데이터 존재 여부 확인
        {
            return; // Gizmo 표시 중단
        }

        Gizmos.color = Color.yellow; // 최초 탐지 범위 색상 설정
        Gizmos.DrawWireSphere(transform.position, gizmoData.DetectionRange); // 플레이어 최초 탐지 범위 표시
        Gizmos.color = new Color(1f, 0.5f, 0f); // 추적 해제 범위 색상 설정
        Gizmos.DrawWireSphere(transform.position, gizmoData.LoseTargetRange); // 플레이어 추적 해제 범위 표시
        Gizmos.color = Color.red; // 공격 범위 색상 설정
        Gizmos.DrawWireSphere(transform.position, gizmoData.AttackRange); // 플레이어 공격 가능 범위 표시
    }

    private void OnValidate() // Inspector 적 전투 설정값과 참조 검증
    {
        targetSearchInterval = Mathf.Max(0.1f, targetSearchInterval); // 플레이어 재검색 간격 최소값 적용

        if (enemyHealth == null) // 적 체력 참조 연결 여부 확인
        {
            enemyHealth = GetComponent<EnemyHealth>(); // 같은 적 오브젝트의 EnemyHealth 자동 연결
        }

        if (attackOrigin == null) // 공격 시작 위치 연결 여부 확인
        {
            attackOrigin = transform; // 현재 적 Transform을 기본 공격 위치로 연결
        }
    }
}
