using UnityEngine; // Unity 기본 기능
using UnityEngine.InputSystem; // 새 Input System 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
[RequireComponent(typeof(PlayerHealth))] // 플레이어 체력 컴포넌트 요구
public sealed class PlayerCombatDamageReceiver : MonoBehaviour, ICombatDamageReceiver // 플레이어 전투 피해와 피격 밀림 수신 관리자
{
    [Header("References")] // 전투 피해 참조 묶음
    [Tooltip("전투 피해를 적용할 플레이어 체력 관리자입니다.")] // Inspector 체력 관리자 설명
    [SerializeField] private PlayerHealth playerHealth; // 플레이어 체력 관리자

    [Tooltip("실제 피해가 적용되었을 때 충격량을 밀림으로 변환할 관리자입니다.")] // Inspector 피격 밀림 관리자 설명
    [SerializeField] private PlayerCombatImpactMotor impactMotor; // 플레이어 피격 밀림 관리자

    [Tooltip("여러 Collider를 하나의 피해 대상으로 묶을 기준 Transform입니다.")] // Inspector 피해 기준 설명
    [SerializeField] private Transform damageRoot; // 피해 대상 기준 Transform

    [Header("Duplicate Hit")] // 중복 피해 설정 묶음
    [Tooltip("같은 공격자와 같은 공격 고유 번호가 반복 전달될 때 중복 피해를 차단합니다.")] // Inspector 중복 차단 설명
    [SerializeField] private bool rejectDuplicateAttackSequence = true; // 동일 공격 단계 중복 차단 여부

    [Header("Debug")] // 디버그 설정 묶음
    [Tooltip("전투 피해 적용과 무적 차단 결과를 Console에 출력합니다.")] // Inspector 로그 설명
    [SerializeField] private bool logDamageResults = true; // 피해 결과 로그 여부

    [Tooltip("Inspector Context Menu와 테스트 키로 적용할 전투 피해량입니다.")] // Inspector 테스트 피해 설명
    [SerializeField, Min(0.1f)] private float testCombatDamage = 10f; // 테스트 전투 피해량

    [Tooltip("Editor 또는 Development Build에서 테스트 피해 키를 사용할지 설정합니다.")] // Inspector 테스트 키 사용 설명
    [SerializeField] private bool enableDebugDamageKey; // 테스트 피해 키 사용 여부

    [Tooltip("테스트 전투 피해를 적용할 키입니다.")] // Inspector 테스트 키 설명
    [SerializeField] private Key debugDamageKey = Key.K; // 테스트 전투 피해 키

    [Header("Runtime")] // 실행 상태 확인 묶음
    [Tooltip("실제로 적용된 전투 피해 횟수입니다.")] // Inspector 적용 횟수 설명
    [SerializeField] private int appliedCombatHitCount; // 적용된 전투 피해 횟수

    [Tooltip("무적 또는 중복 판정으로 차단한 전투 피해 횟수입니다.")] // Inspector 차단 횟수 설명
    [SerializeField] private int blockedCombatHitCount; // 차단한 전투 피해 횟수

    [Tooltip("실제 피해와 함께 밀림이 적용된 횟수입니다.")] // Inspector 밀림 적용 횟수 설명
    [SerializeField] private int appliedImpactCount; // 플레이어 밀림 적용 횟수

    [Tooltip("마지막 공격 고유 번호입니다.")] // Inspector 마지막 공격 번호 설명
    [SerializeField] private int lastAttackSequenceId = -1; // 마지막 공격 고유 번호

    private GameObject lastAttacker; // 마지막 공격 주체

    public Transform DamageRoot => damageRoot == null ? transform : damageRoot; // 플레이어 피해 대상 기준 제공
    public bool IsAlive => playerHealth != null && !playerHealth.IsDead; // 플레이어 생존 상태 제공
    public int AppliedCombatHitCount => appliedCombatHitCount; // 적용된 전투 피해 횟수 제공
    public int BlockedCombatHitCount => blockedCombatHitCount; // 차단한 전투 피해 횟수 제공
    public int AppliedImpactCount => appliedImpactCount; // 적용된 피격 밀림 횟수 제공

    private void Reset() // 컴포넌트 최초 추가 시 기본 참조 설정
    {
        playerHealth = GetComponent<PlayerHealth>(); // 같은 Player의 PlayerHealth 연결
        impactMotor = GetComponent<PlayerCombatImpactMotor>(); // 같은 Player의 피격 밀림 관리자 연결
        damageRoot = transform; // Player 루트를 피해 기준으로 연결
    }

    private void Awake() // 전투 피해 수신 참조 초기화
    {
        if (playerHealth == null) // 체력 관리자 참조 확인
        {
            playerHealth = GetComponent<PlayerHealth>(); // 같은 Player에서 자동 검색
        }

        if (impactMotor == null) // 피격 밀림 관리자 참조 확인
        {
            impactMotor = GetComponent<PlayerCombatImpactMotor>(); // 같은 Player에서 자동 검색
        }

        if (damageRoot == null) // 피해 기준 Transform 확인
        {
            damageRoot = transform; // Player 루트를 기본 피해 기준으로 적용
        }

        if (playerHealth == null) // 체력 관리자 검색 결과 확인
        {
            Debug.LogError("PlayerCombatDamageReceiver에 PlayerHealth가 필요합니다.", this); // 체력 관리자 누락 오류 출력
            enabled = false; // 전투 피해 수신 기능 비활성화
            return; // 전투 피해 수신 초기화 중단
        }

        if (impactMotor == null) // 피격 밀림 관리자 검색 결과 확인
        {
            Debug.LogWarning("PlayerCombatImpactMotor가 없어 전투 피해는 적용되지만 피격 밀림은 생략됩니다.", this); // 피격 밀림 누락 경고 출력
        }
    }

    private void Update() // 개발용 테스트 전투 피해 입력 처리
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (!enableDebugDamageKey) // 테스트 피해 키 사용 여부 확인
        {
            return; // 테스트 입력 처리 생략
        }

        Keyboard keyboard = Keyboard.current; // 현재 키보드 장치 조회

        if (keyboard == null || debugDamageKey == Key.None) // 키보드와 테스트 키 설정 확인
        {
            return; // 테스트 입력 처리 중단
        }

        if (keyboard[debugDamageKey].wasPressedThisFrame) // 테스트 피해 키 입력 확인
        {
            ApplyTestCombatDamage(); // 설정된 테스트 전투 피해 적용
        }
#endif
    }

    public bool ReceiveDamage(CombatHitData hitData) // 전투 피해 정보와 충격량 수신
    {
        if (!IsAlive || hitData.Damage <= 0f) // 생존 상태와 피해량 확인
        {
            return false; // 피해 수신 실패 반환
        }

        if (IsDuplicateAttackSequence(hitData)) // 동일 공격 단계 중복 여부 확인
        {
            blockedCombatHitCount++; // 중복 차단 횟수 증가
            LogBlockedResult(hitData, "동일 공격 단계 중복"); // 중복 차단 결과 출력
            return false; // 중복 피해 차단 반환
        }

        RememberAttackSequence(hitData); // 현재 공격 단계 정보 저장
        bool damageApplied = playerHealth.TakeCombatDamage(hitData.Damage); // 전투 무적이 적용된 체력 피해 시도

        if (!damageApplied) // 피해 적용 실패 확인
        {
            blockedCombatHitCount++; // 무적 차단 횟수 증가
            LogBlockedResult(hitData, "전투 무적"); // 무적 차단 결과 출력
            return false; // 전투 피해와 밀림 차단 반환
        }

        appliedCombatHitCount++; // 실제 피해 적용 횟수 증가

        if (impactMotor != null && hitData.ImpactForce > 0f) // 피격 밀림 관리자와 충격량 확인
        {
            if (impactMotor.ApplyImpact(hitData.HitDirection, hitData.ImpactForce)) // 공격 방향으로 플레이어 밀림 적용
            {
                appliedImpactCount++; // 실제 플레이어 밀림 적용 횟수 증가
            }
        }

        if (logDamageResults) // 피해 결과 로그 사용 여부 확인
        {
            string attackerName = hitData.Attacker == null // 공격자 존재 여부 확인
                ? "UNKNOWN" // 공격자 없음 문구
                : hitData.Attacker.name; // 공격자 이름 조회
            Debug.Log( // 플레이어 전투 피해 적용 로그 시작
                $"플레이어 전투 피해 적용 / 공격자 {attackerName} / 피해 {hitData.Damage:0.##} / " // 피해 정보 출력
                + $"충격량 {hitData.ImpactForce:0.##} / 남은 체력 {playerHealth.CurrentHealth:0.##}", // 충격량과 체력 출력
                this); // 현재 Player를 Log Context로 지정
        }

        return true; // 전투 피해 적용 성공 반환
    }

    [ContextMenu("Apply Test Combat Damage")] // Inspector 테스트 피해 메뉴
    private void ApplyTestCombatDamage() // 테스트 전투 피해 적용
    {
        if (!Application.isPlaying) // Play Mode 여부 확인
        {
            Debug.LogWarning("테스트 전투 피해는 Play Mode에서 실행해야 합니다.", this); // Edit Mode 실행 경고 출력
            return; // 테스트 피해 처리 중단
        }

        if (playerHealth == null) // 체력 관리자 존재 확인
        {
            return; // 테스트 피해 처리 중단
        }

        bool damageApplied = playerHealth.TakeCombatDamage(testCombatDamage); // 설정된 테스트 전투 피해 적용

        if (damageApplied) // 테스트 피해 적용 성공 여부 확인
        {
            appliedCombatHitCount++; // 테스트 피해 적용 횟수 증가
        }
        else // 테스트 피해 차단 처리
        {
            blockedCombatHitCount++; // 테스트 피해 차단 횟수 증가
        }

        if (logDamageResults) // 테스트 로그 사용 여부 확인
        {
            Debug.Log( // 테스트 전투 피해 결과 출력 시작
                damageApplied // 피해 적용 결과 확인
                    ? $"테스트 전투 피해 {testCombatDamage:0.##} 적용 완료" // 피해 적용 성공 문구
                    : $"테스트 전투 피해 {testCombatDamage:0.##} 차단", // 피해 차단 문구
                this); // 현재 Player를 Log Context로 지정
        }
    }

    private bool IsDuplicateAttackSequence(CombatHitData hitData) // 동일 공격 단계 중복 여부 계산
    {
        if (!rejectDuplicateAttackSequence) // 중복 차단 사용 여부 확인
        {
            return false; // 중복 차단 비활성 반환
        }

        if (hitData.Attacker == null || hitData.AttackSequenceId <= 0) // 공격자와 유효 공격 번호 확인
        {
            return false; // 중복 비교 불가 반환
        }

        return lastAttacker == hitData.Attacker // 동일 공격자 확인
            && lastAttackSequenceId == hitData.AttackSequenceId; // 동일 공격 고유 번호 확인
    }

    private void RememberAttackSequence(CombatHitData hitData) // 현재 공격 단계 정보 저장
    {
        if (hitData.Attacker == null || hitData.AttackSequenceId <= 0) // 저장 가능한 공격 정보 확인
        {
            return; // 공격 단계 저장 생략
        }

        lastAttacker = hitData.Attacker; // 마지막 공격자 저장
        lastAttackSequenceId = hitData.AttackSequenceId; // 마지막 공격 고유 번호 저장
    }

    private void LogBlockedResult(CombatHitData hitData, string reason) // 전투 피해 차단 결과 출력
    {
        if (!logDamageResults) // 피해 결과 로그 사용 여부 확인
        {
            return; // 로그 출력 생략
        }

        string attackerName = hitData.Attacker == null // 공격자 존재 여부 확인
            ? "UNKNOWN" // 공격자 없음 문구
            : hitData.Attacker.name; // 공격자 이름 조회
        Debug.Log( // 플레이어 전투 피해 차단 로그 시작
            $"플레이어 전투 피해 차단 / 사유 {reason} / 공격자 {attackerName} / " // 차단 사유와 공격자 출력
            + $"피해 {hitData.Damage:0.##} / 충격량 {hitData.ImpactForce:0.##}", // 피해량과 충격량 출력
            this); // 현재 Player를 Log Context로 지정
    }

    private void OnValidate() // Inspector 설정값과 참조 검증
    {
        testCombatDamage = Mathf.Max(0.1f, testCombatDamage); // 테스트 피해량 최소값 적용

        if (playerHealth == null) // 체력 관리자 참조 확인
        {
            playerHealth = GetComponent<PlayerHealth>(); // 같은 Player의 PlayerHealth 자동 연결
        }

        if (impactMotor == null) // 피격 밀림 관리자 참조 확인
        {
            impactMotor = GetComponent<PlayerCombatImpactMotor>(); // 같은 Player의 피격 밀림 관리자 자동 연결
        }

        if (damageRoot == null) // 피해 기준 Transform 참조 확인
        {
            damageRoot = transform; // Player 루트를 피해 기준으로 연결
        }
    }
}
