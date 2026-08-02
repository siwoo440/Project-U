using System; // C# 이벤트 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.AI; // NavMeshAgent 이동 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
[RequireComponent(typeof(EnemyHealth))] // 적 체력 관리자 요구
[RequireComponent(typeof(NavMeshAgent))] // 적 NavMeshAgent 요구
public sealed class EnemyCombatImpactMotor : MonoBehaviour // 적 전투 피격 밀림 관리자
{
    [Header("References")] // 적 피격 밀림 참조 묶음
    [Tooltip("플레이어 공격 피해와 사망 이벤트를 제공할 EnemyHealth입니다.")] // Inspector 적 체력 설명
    [SerializeField] private EnemyHealth enemyHealth; // 적 체력 관리자

    [Tooltip("NavMesh 위에서 적을 밀어낼 Agent입니다.")] // Inspector NavMeshAgent 설명
    [SerializeField] private NavMeshAgent navMeshAgent; // 적 NavMesh 이동 Agent

    [Header("Impact")] // 적 피격 밀림 설정 묶음
    [Tooltip("CombatHitData의 충격량 1당 이동할 거리입니다.")] // Inspector 충격 거리 배율 설명
    [SerializeField, Min(0f)] private float distancePerImpactForce = 0.12f; // 충격량당 적 밀림 거리

    [Tooltip("한 번의 적 피격 밀림이 진행되는 시간입니다.")] // Inspector 적 밀림 시간 설명
    [SerializeField, Min(0.02f)] private float impactDuration = 0.16f; // 적 밀림 진행 시간

    [Tooltip("한 번의 피격으로 적이 이동할 수 있는 최대 거리입니다.")] // Inspector 적 최대 밀림 거리 설명
    [SerializeField, Min(0f)] private float maximumImpactDistance = 1.5f; // 적 최대 밀림 거리

    [Tooltip("시간에 따른 누적 적 밀림 거리 비율입니다. 0에서 1로 증가해야 합니다.")] // Inspector 적 밀림 곡선 설명
    [SerializeField] private AnimationCurve impactDistanceCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); // 적 밀림 거리 곡선

    [Header("Debug")] // 적 피격 밀림 Debug 설정 묶음
    [Tooltip("적 피격 밀림 시작과 종료 결과를 Console에 출력합니다.")] // Inspector 적 밀림 로그 설명
    [SerializeField] private bool logImpactResults = true; // 적 밀림 로그 여부

    [Tooltip("Inspector Context Menu에서 사용할 테스트 충격량입니다.")] // Inspector 테스트 충격량 설명
    [SerializeField, Min(0.1f)] private float testImpactForce = 5f; // 테스트 적 충격량

    [Header("Runtime")] // 적 밀림 실행 상태 묶음
    [Tooltip("현재 적 피격 밀림을 적용 중인지 표시합니다.")] // Inspector 적 밀림 실행 설명
    [SerializeField] private bool isApplyingImpact; // 현재 적 밀림 실행 여부

    [Tooltip("현재 적 피격 밀림 방향입니다.")] // Inspector 적 밀림 방향 설명
    [SerializeField] private Vector3 currentImpactDirection; // 현재 적 밀림 방향

    [Tooltip("현재 피격으로 적이 이동할 전체 거리입니다.")] // Inspector 적 전체 밀림 거리 설명
    [SerializeField] private float currentImpactDistance; // 현재 적 전체 밀림 거리

    [Tooltip("현재 적 피격 밀림 진행 비율입니다.")] // Inspector 적 밀림 진행률 설명
    [SerializeField, Range(0f, 1f)] private float currentImpactNormalized; // 현재 적 밀림 진행 비율

    private float impactStartedAt; // 적 밀림 시작 시각
    private float previousCurveValue; // 이전 프레임 적 누적 밀림 곡선값

    public bool IsApplyingImpact => isApplyingImpact; // 현재 적 밀림 상태 제공
    public Vector3 CurrentImpactDirection => currentImpactDirection; // 현재 적 밀림 방향 제공
    public float CurrentImpactDistance => currentImpactDistance; // 현재 적 전체 밀림 거리 제공
    public float CurrentImpactNormalized => currentImpactNormalized; // 현재 적 밀림 진행 비율 제공

    public event Action<Vector3, float> ImpactStarted; // 적 밀림 방향과 거리 전달 이벤트
    public event Action ImpactFinished; // 적 밀림 종료 이벤트

    private void Reset() // 컴포넌트 최초 추가 시 참조 자동 연결
    {
        enemyHealth = GetComponent<EnemyHealth>(); // 같은 적의 EnemyHealth 연결
        navMeshAgent = GetComponent<NavMeshAgent>(); // 같은 적의 NavMeshAgent 연결
    }

    private void Awake() // 적 피격 밀림 참조 초기화
    {
        if (enemyHealth == null) // 적 체력 참조 확인
        {
            enemyHealth = GetComponent<EnemyHealth>(); // 같은 적에서 EnemyHealth 자동 검색
        }

        if (navMeshAgent == null) // NavMeshAgent 참조 확인
        {
            navMeshAgent = GetComponent<NavMeshAgent>(); // 같은 적에서 NavMeshAgent 자동 검색
        }

        if (enemyHealth == null || navMeshAgent == null) // 필수 적 피격 참조 존재 여부 확인
        {
            Debug.LogError("EnemyCombatImpactMotor에 EnemyHealth와 NavMeshAgent가 필요합니다.", this); // 필수 참조 누락 오류 출력
            enabled = false; // 적 밀림 기능 비활성화
        }
    }

    private void OnEnable() // 적 체력 이벤트 연결
    {
        if (enemyHealth == null) // 적 체력 참조 확인
        {
            return; // 이벤트 연결 처리 중단
        }

        enemyHealth.Damaged += HandleDamaged; // 적 피해 적용 이벤트 구독
        enemyHealth.Died += HandleDied; // 적 사망 이벤트 구독
        enemyHealth.Revived += HandleRevived; // 적 부활 이벤트 구독
    }

    private void LateUpdate() // 적 추적 이동 처리 이후 피격 밀림 추가 적용
    {
        if (!isApplyingImpact) // 현재 적 밀림 실행 여부 확인
        {
            return; // 적 밀림 처리 생략
        }

        if (enemyHealth.IsDead || !navMeshAgent.enabled || !navMeshAgent.isOnNavMesh) // 적 생존과 NavMesh 상태 확인
        {
            CancelImpact(); // 사망 또는 NavMesh 이탈 시 적 밀림 중단
            return; // 적 밀림 처리 종료
        }

        float elapsedTime = Time.time - impactStartedAt; // 적 밀림 시작 후 경과 시간 계산
        currentImpactNormalized = Mathf.Clamp01(elapsedTime / impactDuration); // 현재 적 밀림 진행 비율 계산
        float currentCurveValue = Mathf.Clamp01(impactDistanceCurve.Evaluate(currentImpactNormalized)); // 현재 적 누적 거리 곡선값 계산
        float curveDelta = Mathf.Max(0f, currentCurveValue - previousCurveValue); // 이번 프레임 적용할 곡선 차이 계산
        float frameDistance = currentImpactDistance * curveDelta; // 이번 프레임 적 밀림 거리 계산

        if (frameDistance > 0f) // 실제 적 이동 거리 존재 여부 확인
        {
            navMeshAgent.Move(currentImpactDirection * frameDistance); // NavMesh 제약을 적용한 적 밀림 이동 실행
        }

        previousCurveValue = currentCurveValue; // 현재 곡선값을 다음 프레임 기준으로 저장

        if (currentImpactNormalized >= 1f) // 적 밀림 완료 여부 확인
        {
            FinishImpact(); // 적 밀림 종료 처리
        }
    }

    public bool ApplyImpact(Vector3 impactDirection, float impactForce) // 공격 방향과 충격량으로 적 밀림 시작
    {
        if (!isActiveAndEnabled || enemyHealth == null || enemyHealth.IsDead) // 컴포넌트와 적 생존 상태 확인
        {
            return false; // 적 밀림 시작 실패 반환
        }

        if (!navMeshAgent.enabled || !navMeshAgent.isOnNavMesh) // NavMeshAgent 사용 가능 상태 확인
        {
            return false; // NavMesh 밖 적 밀림 시작 실패 반환
        }

        Vector3 planarDirection = impactDirection; // 전달된 공격 방향 복사
        planarDirection.y = 0f; // 수평 밀림만 사용하도록 높이 방향 제거

        if (planarDirection.sqrMagnitude < 0.0001f || impactForce <= 0f) // 유효한 방향과 충격량 확인
        {
            return false; // 적 밀림 시작 실패 반환
        }

        currentImpactDirection = planarDirection.normalized; // 정규화된 적 밀림 방향 저장
        currentImpactDistance = Mathf.Min( // 최종 적 밀림 거리 계산 시작
            maximumImpactDistance, // 최대 적 밀림 거리 제한
            impactForce * distancePerImpactForce); // 공격 충격량 기반 적 밀림 거리 계산

        if (currentImpactDistance <= 0f) // 최종 적 밀림 거리 확인
        {
            return false; // 적 밀림 시작 실패 반환
        }

        if (!navMeshAgent.isStopped) // 현재 Agent 이동 상태 확인
        {
            navMeshAgent.isStopped = true; // 피격 밀림 중 추적 이동 즉시 정지
        }

        impactStartedAt = Time.time; // 적 밀림 시작 시각 저장
        currentImpactNormalized = 0f; // 적 밀림 진행 비율 초기화
        previousCurveValue = Mathf.Clamp01(impactDistanceCurve.Evaluate(0f)); // 적 밀림 거리 곡선 시작값 저장
        isApplyingImpact = true; // 적 밀림 실행 상태 적용
        ImpactStarted?.Invoke(currentImpactDirection, currentImpactDistance); // 적 밀림 방향과 전체 거리 전달

        if (logImpactResults) // 적 밀림 로그 사용 여부 확인
        {
            Debug.Log( // 적 밀림 시작 로그 출력 시작
                $"{gameObject.name} 피격 밀림 시작 / 충격량 {impactForce:0.##} / " // 전달 충격량 출력
                + $"거리 {currentImpactDistance:0.##}", // 최종 적 밀림 거리 출력
                this); // 현재 적을 Log Context로 지정
        }

        return true; // 적 밀림 시작 성공 반환
    }

    public void CancelImpact() // 현재 적 피격 밀림 즉시 중단
    {
        if (!isApplyingImpact) // 실행 중인 적 밀림 존재 여부 확인
        {
            return; // 적 밀림 중단 처리 생략
        }

        isApplyingImpact = false; // 적 밀림 실행 상태 해제
        currentImpactNormalized = 0f; // 적 밀림 진행 비율 초기화
        previousCurveValue = 0f; // 이전 적 곡선값 초기화
    }

    [ContextMenu("Apply Test Impact")] // Inspector 적 밀림 테스트 메뉴
    private void ApplyTestImpact() // 적 전방 방향으로 테스트 밀림 적용
    {
        if (!Application.isPlaying) // Play Mode 여부 확인
        {
            Debug.LogWarning("적 피격 밀림 테스트는 Play Mode에서 실행해야 합니다.", this); // Edit Mode 테스트 경고 출력
            return; // 테스트 적 밀림 처리 중단
        }

        ApplyImpact(transform.forward, testImpactForce); // 적 전방 방향으로 테스트 충격 적용
    }

    private void HandleDamaged(CombatHitData hitData, float appliedDamage) // 실제 적 피해와 함께 피격 밀림 시작
    {
        if (appliedDamage <= 0f || hitData.ImpactForce <= 0f) // 실제 피해량과 충격량 확인
        {
            return; // 적 밀림 적용 생략
        }

        ApplyImpact(hitData.HitDirection, hitData.ImpactForce); // 플레이어 공격 방향으로 적 밀림 적용
    }

    private void HandleDied(CombatHitData killingHitData) // 적 사망 시 피격 밀림 정리
    {
        CancelImpact(); // 사망한 적의 밀림 즉시 중단
    }

    private void HandleRevived() // 적 부활 시 피격 밀림 상태 초기화
    {
        CancelImpact(); // 남아 있는 적 밀림 상태 정리
    }

    private void FinishImpact() // 적 피격 밀림 정상 종료
    {
        isApplyingImpact = false; // 적 밀림 실행 상태 해제
        currentImpactNormalized = 1f; // 적 밀림 진행 비율 완료 적용
        previousCurveValue = 1f; // 적 누적 거리 곡선 완료값 적용
        ImpactFinished?.Invoke(); // 적 밀림 종료 이벤트 전달

        if (logImpactResults) // 적 밀림 로그 사용 여부 확인
        {
            Debug.Log($"{gameObject.name} 피격 밀림 종료", this); // 적 밀림 종료 결과 출력
        }
    }

    private void OnDisable() // 적 체력 이벤트와 밀림 상태 정리
    {
        if (enemyHealth != null) // 적 체력 참조 확인
        {
            enemyHealth.Damaged -= HandleDamaged; // 적 피해 이벤트 구독 해제
            enemyHealth.Died -= HandleDied; // 적 사망 이벤트 구독 해제
            enemyHealth.Revived -= HandleRevived; // 적 부활 이벤트 구독 해제
        }

        CancelImpact(); // 실행 중인 적 밀림 중단
    }

    private void OnValidate() // Inspector 적 밀림 설정값과 참조 검증
    {
        distancePerImpactForce = Mathf.Max(0f, distancePerImpactForce); // 충격 거리 배율 음수 방지
        impactDuration = Mathf.Max(0.02f, impactDuration); // 적 밀림 시간 최소값 적용
        maximumImpactDistance = Mathf.Max(0f, maximumImpactDistance); // 적 최대 밀림 거리 음수 방지
        testImpactForce = Mathf.Max(0.1f, testImpactForce); // 테스트 충격량 최소값 적용

        if (enemyHealth == null) // 적 체력 참조 확인
        {
            enemyHealth = GetComponent<EnemyHealth>(); // 같은 적의 EnemyHealth 자동 연결
        }

        if (navMeshAgent == null) // NavMeshAgent 참조 확인
        {
            navMeshAgent = GetComponent<NavMeshAgent>(); // 같은 적의 NavMeshAgent 자동 연결
        }
    }
}
