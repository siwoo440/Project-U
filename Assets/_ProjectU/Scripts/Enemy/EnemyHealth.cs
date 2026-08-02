using System; // C# 이벤트 기능
using System.Collections; // Unity 코루틴 기능
using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class EnemyHealth : MonoBehaviour, ICombatDamageReceiver // 적 체력과 플레이어 공격 피해 수신 관리자
{
    [Header("Data")] // 적 데이터 참조 묶음
    [Tooltip("최대 체력, 방어력과 사망 설정을 제공할 적 전투 데이터입니다.")] // Inspector 적 데이터 설명
    [SerializeField] private EnemyCombatData combatData; // 적 공통 전투 데이터

    [Header("Damage Root")] // 피해 대상 기준 묶음
    [Tooltip("여러 Collider를 하나의 적 피해 대상으로 묶을 기준 Transform입니다.")] // Inspector 피해 기준 설명
    [SerializeField] private Transform damageRoot; // 적 피해 대상 기준 Transform

    [Header("Duplicate Hit")] // 중복 피해 설정 묶음
    [Tooltip("같은 공격자와 같은 공격 고유 번호가 반복되면 중복 피해를 차단합니다.")] // Inspector 중복 피해 설명
    [SerializeField] private bool rejectDuplicateAttackSequence = true; // 동일 공격 단계 중복 차단 여부

    [Header("Debug")] // 적 체력 Debug 설정 묶음
    [Tooltip("피해와 사망 결과를 Console에 출력합니다.")] // Inspector Debug 로그 설명
    [SerializeField] private bool logHealthResults = true; // 적 체력 로그 출력 여부

    [Tooltip("Inspector Context Menu로 적용할 테스트 피해량입니다.")] // Inspector 테스트 피해량 설명
    [SerializeField, Min(0.1f)] private float testDamageAmount = 10f; // 적 테스트 피해량

    [Header("Runtime")] // 적 체력 실행 상태 묶음
    [Tooltip("현재 적 체력입니다.")] // Inspector 현재 체력 설명
    [SerializeField] private float currentHealth; // 적 현재 체력

    [Tooltip("현재 사망 상태입니다.")] // Inspector 사망 상태 설명
    [SerializeField] private bool isDead; // 적 사망 상태

    [Tooltip("실제로 적용된 피해 횟수입니다.")] // Inspector 피해 횟수 설명
    [SerializeField] private int receivedHitCount; // 적 피해 수신 횟수

    [Tooltip("중복 공격으로 차단한 피해 횟수입니다.")] // Inspector 중복 차단 횟수 설명
    [SerializeField] private int blockedDuplicateHitCount; // 중복 피해 차단 횟수

    [Tooltip("마지막 공격 고유 번호입니다.")] // Inspector 마지막 공격 번호 설명
    [SerializeField] private int lastAttackSequenceId = -1; // 마지막 공격 고유 번호

    private GameObject lastAttacker; // 마지막 공격 주체
    private Collider[] cachedColliders; // 사망 시 비활성화할 적 Collider 목록
    private Coroutine cleanupCoroutine; // 실행 중인 사망 제거 코루틴

    public EnemyCombatData CombatData => combatData; // 적 공통 전투 데이터 제공
    public Transform DamageRoot => damageRoot == null ? transform : damageRoot; // 중복 Collider 피해 기준 제공
    public bool IsAlive => !isDead; // 현재 피해 수신 가능 상태 제공
    public float CurrentHealth => currentHealth; // 현재 적 체력 제공
    public float MaximumHealth => combatData == null ? 1f : combatData.MaximumHealth; // 적 최대 체력 제공
    public float NormalizedHealth => MaximumHealth <= 0f ? 0f : currentHealth / MaximumHealth; // 적 체력 비율 제공
    public bool IsDead => isDead; // 적 사망 상태 제공
    public int ReceivedHitCount => receivedHitCount; // 피해 수신 횟수 제공
    public int BlockedDuplicateHitCount => blockedDuplicateHitCount; // 중복 피해 차단 횟수 제공

    public event Action<CombatHitData, float> Damaged; // 공격 정보와 실제 피해량 이벤트
    public event Action<CombatHitData> Died; // 사망 원인이 된 공격 정보 이벤트
    public event Action Revived; // 적 부활 완료 이벤트

    private void Reset() // 컴포넌트 최초 추가 시 기본 참조 설정
    {
        damageRoot = transform; // 현재 적 루트를 기본 피해 기준으로 연결
    }

    private void Awake() // 적 체력 상태 초기화
    {
        if (damageRoot == null) // 피해 기준 Transform 연결 여부 확인
        {
            damageRoot = transform; // 현재 적 루트를 기본 피해 기준으로 설정
        }

        cachedColliders = GetComponentsInChildren<Collider>(true); // 적 자식의 전체 Collider 저장

        if (combatData == null) // 적 전투 데이터 연결 여부 확인
        {
            Debug.LogError("EnemyHealth에 EnemyCombatData를 연결해야 합니다.", this); // 적 데이터 누락 오류 출력
            enabled = false; // 적 체력 기능 비활성화
            return; // 초기화 처리 중단
        }

        ResetHealthRuntime(); // 시작 체력과 사망 상태 초기화
    }

    private void OnDisable() // 비활성화 시 사망 제거 코루틴 정리
    {
        if (cleanupCoroutine == null) // 실행 중인 제거 코루틴 확인
        {
            return; // 정리할 코루틴 없음
        }

        StopCoroutine(cleanupCoroutine); // 실행 중인 사망 제거 코루틴 중단
        cleanupCoroutine = null; // 코루틴 참조 초기화
    }

    public bool ReceiveDamage(CombatHitData hitData) // 플레이어 무기 전투 피해 수신
    {
        if (!isActiveAndEnabled || isDead) // 적 체력 기능과 생존 상태 확인
        {
            return false; // 피해 수신 실패 반환
        }

        if (hitData.Damage <= 0f) // 전달된 피해량 유효성 확인
        {
            return false; // 피해 수신 실패 반환
        }

        if (IsDuplicateAttackSequence(hitData)) // 동일 공격 단계 중복 여부 확인
        {
            blockedDuplicateHitCount++; // 중복 피해 차단 횟수 증가
            LogDuplicateBlocked(hitData); // 중복 피해 차단 결과 출력
            return false; // 중복 피해 차단 반환
        }

        RememberAttackSequence(hitData); // 현재 공격 단계 정보 저장
        float defenseMultiplier = 1f - combatData.DefensePercent / 100f; // 방어력을 반영한 피해 배율 계산
        float reducedDamage = Mathf.Max(0f, hitData.Damage * defenseMultiplier); // 방어 적용 피해량 계산

        if (reducedDamage <= 0f) // 실제 피해량 존재 여부 확인
        {
            return false; // 체력 변화 없는 피해 차단
        }

        float previousHealth = currentHealth; // 피해 전 체력 저장
        currentHealth = Mathf.Max(0f, currentHealth - reducedDamage); // 현재 체력 감소
        float appliedDamage = previousHealth - currentHealth; // 실제 적용 피해량 계산
        receivedHitCount++; // 실제 피해 수신 횟수 증가
        Damaged?.Invoke(hitData, appliedDamage); // 공격 정보와 실제 피해량 전달
        LogDamageApplied(hitData, appliedDamage); // 피해 적용 결과 출력

        if (currentHealth > 0f) // 피해 후 남은 체력 확인
        {
            return true; // 일반 피해 적용 성공 반환
        }

        HandleDeath(hitData); // 체력 소진 사망 처리
        return true; // 마지막 피해 적용 성공 반환
    }

    [ContextMenu("Revive Enemy")] // Inspector 적 부활 메뉴
    public bool Revive() // 적 체력과 Collider를 최대 상태로 복구
    {
        if (!isDead || combatData == null) // 사망 상태와 적 데이터 확인
        {
            return false; // 부활 처리 실패 반환
        }

        if (cleanupCoroutine != null) // 사망 제거 코루틴 실행 여부 확인
        {
            StopCoroutine(cleanupCoroutine); // 사망 제거 예약 중단
            cleanupCoroutine = null; // 제거 코루틴 참조 초기화
        }

        SetCollidersEnabled(true); // 적 전체 Collider 다시 활성화
        ResetHealthRuntime(); // 체력과 사망 상태 초기화
        Revived?.Invoke(); // 부활 완료 이벤트 전달

        if (logHealthResults) // 적 체력 로그 사용 여부 확인
        {
            Debug.Log($"{combatData.DisplayName} 부활 / 체력 {currentHealth:0.##}", this); // 부활 결과 출력
        }

        return true; // 적 부활 성공 반환
    }

    [ContextMenu("Apply Test Damage")] // Inspector 테스트 피해 메뉴
    private void ApplyTestDamage() // 적에게 개발용 직접 피해 적용
    {
        if (!Application.isPlaying) // Play Mode 여부 확인
        {
            Debug.LogWarning("적 테스트 피해는 Play Mode에서 실행해야 합니다.", this); // Edit Mode 실행 경고 출력
            return; // 테스트 피해 처리 중단
        }

        CombatHitData testHitData = new CombatHitData( // 테스트 전투 피해 정보 생성
            gameObject, // 공격 주체로 현재 적 사용
            null, // 테스트 공격 아이템 없음
            WeaponAttackType.Melee, // 근접 공격 방식 사용
            testDamageAmount, // 설정된 테스트 피해량 전달
            0f, // 테스트 충격량 없음
            transform.position, // 적 현재 위치를 피격 지점으로 사용
            transform.forward, // 적 전방을 테스트 공격 방향으로 사용
            null, // 테스트 충돌 Collider 없음
            0, // 중복 차단을 사용하지 않는 공격 번호
            0); // 첫 번째 공격 단계 사용

        ReceiveDamage(testHitData); // 생성한 테스트 피해 적용
    }

    private bool IsDuplicateAttackSequence(CombatHitData hitData) // 동일 공격 단계 중복 여부 계산
    {
        if (!rejectDuplicateAttackSequence) // 중복 피해 차단 사용 여부 확인
        {
            return false; // 중복 피해 차단 비활성 반환
        }

        if (hitData.Attacker == null || hitData.AttackSequenceId <= 0) // 비교 가능한 공격 정보 확인
        {
            return false; // 중복 비교 불가 반환
        }

        return lastAttacker == hitData.Attacker // 동일 공격자 확인
            && lastAttackSequenceId == hitData.AttackSequenceId; // 동일 공격 고유 번호 확인
    }

    private void RememberAttackSequence(CombatHitData hitData) // 마지막 공격 단계 정보 저장
    {
        if (hitData.Attacker == null || hitData.AttackSequenceId <= 0) // 저장 가능한 공격 정보 확인
        {
            return; // 공격 단계 저장 생략
        }

        lastAttacker = hitData.Attacker; // 마지막 공격자 저장
        lastAttackSequenceId = hitData.AttackSequenceId; // 마지막 공격 고유 번호 저장
    }

    private void HandleDeath(CombatHitData killingHitData) // 적 체력 소진과 사망 후 처리
    {
        isDead = true; // 적 사망 상태 적용
        currentHealth = 0f; // 현재 체력을 0으로 고정

        if (combatData.DisableCollidersOnDeath) // 사망 Collider 비활성화 설정 확인
        {
            SetCollidersEnabled(false); // 적 전체 Collider 비활성화
        }

        Died?.Invoke(killingHitData); // 사망 원인이 된 공격 정보 전달

        if (logHealthResults) // 적 체력 로그 사용 여부 확인
        {
            Debug.Log($"{combatData.DisplayName} 사망 / 누적 피격 {receivedHitCount}", this); // 적 사망 결과 출력
        }

        if (!combatData.DestroyAfterDeath) // 사망 후 제거 설정 확인
        {
            return; // 사망 오브젝트 유지
        }

        cleanupCoroutine = StartCoroutine(CleanupAfterDeath()); // 사망 오브젝트 지연 제거 시작
    }

    private IEnumerator CleanupAfterDeath() // 사망 후 설정 시간만큼 기다렸다가 적 제거
    {
        yield return new WaitForSeconds(combatData.DeathCleanupDelay); // 사망 제거 대기시간만큼 대기
        cleanupCoroutine = null; // 제거 코루틴 참조 초기화
        Destroy(gameObject); // 현재 적 오브젝트 제거
    }

    private void ResetHealthRuntime() // 적 체력 실행 상태 초기화
    {
        currentHealth = combatData.MaximumHealth; // 적 체력을 최대값으로 설정
        isDead = false; // 적 사망 상태 해제
        receivedHitCount = 0; // 피해 수신 횟수 초기화
        blockedDuplicateHitCount = 0; // 중복 차단 횟수 초기화
        lastAttackSequenceId = -1; // 마지막 공격 번호 초기화
        lastAttacker = null; // 마지막 공격자 초기화
    }

    private void SetCollidersEnabled(bool isEnabled) // 적 전체 Collider 활성 상태 변경
    {
        if (cachedColliders == null) // 저장된 Collider 목록 확인
        {
            return; // Collider 변경 처리 중단
        }

        for (int index = 0; index < cachedColliders.Length; index++) // 저장된 Collider 순회
        {
            Collider currentCollider = cachedColliders[index]; // 현재 Collider 가져오기

            if (currentCollider == null) // 제거된 Collider 여부 확인
            {
                continue; // 잘못된 Collider 제외
            }

            currentCollider.enabled = isEnabled; // 현재 Collider 활성 상태 적용
        }
    }

    private void LogDamageApplied(CombatHitData hitData, float appliedDamage) // 적 피해 적용 결과 출력
    {
        if (!logHealthResults) // 적 체력 로그 사용 여부 확인
        {
            return; // 피해 로그 출력 생략
        }

        string attackerName = hitData.Attacker == null // 공격 주체 존재 여부 확인
            ? "UNKNOWN" // 공격 주체 없음 문구
            : hitData.Attacker.name; // 공격 주체 이름 사용

        Debug.Log( // 적 피해 적용 로그 시작
            $"{combatData.DisplayName} 피격 / 공격자 {attackerName} / " // 공격자와 적 이름 출력
            + $"실제 피해 {appliedDamage:0.##} / 남은 체력 {currentHealth:0.##}", // 피해량과 남은 체력 출력
            this); // 현재 적을 Log Context로 지정
    }

    private void LogDuplicateBlocked(CombatHitData hitData) // 적 중복 피해 차단 결과 출력
    {
        if (!logHealthResults) // 적 체력 로그 사용 여부 확인
        {
            return; // 중복 차단 로그 출력 생략
        }

        string attackerName = hitData.Attacker == null // 공격 주체 존재 여부 확인
            ? "UNKNOWN" // 공격 주체 없음 문구
            : hitData.Attacker.name; // 공격 주체 이름 사용

        Debug.Log( // 적 중복 피해 차단 로그 시작
            $"{combatData.DisplayName} 중복 피해 차단 / 공격자 {attackerName} / " // 적 이름과 공격자 출력
            + $"공격 번호 {hitData.AttackSequenceId}", // 중복 공격 고유 번호 출력
            this); // 현재 적을 Log Context로 지정
    }

    private void OnValidate() // Inspector 테스트 설정값 검증
    {
        testDamageAmount = Mathf.Max(0.1f, testDamageAmount); // 테스트 피해량 최소값 적용
    }
}
