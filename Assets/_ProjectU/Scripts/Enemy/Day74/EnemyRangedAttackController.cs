using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyCombatController))]
public sealed class EnemyRangedAttackController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyCombatController combatController;
    [SerializeField] private Transform attackOrigin;
    [SerializeField] private EnemyProjectile projectilePrefab;

    [Header("Projectile")]
    [SerializeField, Min(0.1f)] private float projectileSpeed = 12f;
    [SerializeField, Min(0.1f)] private float projectileLifetime = 5f;
    [SerializeField, Min(0.01f)] private float projectileHitRadius = 0.12f;
    [SerializeField] private LayerMask projectileCollisionMask = ~0;
    [SerializeField] private float targetHeightOffset;

    [Header("Behaviour")]
    [SerializeField] private bool autoFireWhenInRange = true;

    [Header("Debug")]
    [SerializeField] private bool logRangedAttackResults = true;

    [Header("Runtime")]
    [SerializeField] private EnemyAttackPhase currentPhase = EnemyAttackPhase.Ready;
    [SerializeField] private bool isAttackSequenceRunning;
    [SerializeField, Range(0f, 1f)] private float phaseNormalized;
    [SerializeField] private float attackCooldownRemaining;
    [SerializeField] private int currentAttackSequenceId;
    [SerializeField] private int performedShotCount;

    private EnemyCombatData combatData;
    private float phaseStartedAt;
    private float phaseEndsAt;
    private float nextAttackTime;
    private int attackSequenceCounter;

    public EnemyAttackPhase CurrentPhase => currentPhase;
    public bool IsAttackSequenceRunning => isAttackSequenceRunning;
    public float PhaseNormalized => phaseNormalized;
    public float AttackCooldownRemaining => Mathf.Max(0f, nextAttackTime - Time.time);
    public int CurrentAttackSequenceId => currentAttackSequenceId;
    public int PerformedShotCount => performedShotCount;

    private void Reset()
    {
        combatController = GetComponent<EnemyCombatController>();
        attackOrigin = transform;
    }

    private void Awake()
    {
        if (combatController == null)
        {
            combatController = GetComponent<EnemyCombatController>();
        }

        if (attackOrigin == null)
        {
            attackOrigin = transform;
        }

        if (combatController == null)
        {
            Debug.LogError(
                "EnemyRangedAttackController에 EnemyCombatController가 필요합니다.",
                this);
            enabled = false;
        }
    }

    private void Start()
    {
        if (!enabled)
        {
            return;
        }

        combatData = combatController.CombatData;

        if (combatData == null)
        {
            Debug.LogError(
                "EnemyRangedAttackController에서 EnemyCombatData를 찾을 수 없습니다.",
                this);
            enabled = false;
            return;
        }

        if (projectilePrefab == null)
        {
            Debug.LogError(
                "EnemyRangedAttackController의 Projectile Prefab을 연결해야 합니다.",
                this);
            enabled = false;
        }
    }

    private void Update()
    {
        attackCooldownRemaining = AttackCooldownRemaining;
        RefreshCooldownPhase();

        if (!autoFireWhenInRange)
        {
            CancelCurrentSequence("자동 발사 비활성화");
            return;
        }

        if (combatController == null || combatData == null)
        {
            return;
        }

        EnemyCombatState combatState = combatController.CurrentState;

        if (combatState == EnemyCombatState.Dead
            || combatState == EnemyCombatState.Hit
            || combatState == EnemyCombatState.Idle
            || combatState == EnemyCombatState.Chasing)
        {
            CancelCurrentSequence($"전투 상태 {combatState}");
            return;
        }

        if (combatState != EnemyCombatState.Attacking)
        {
            return;
        }

        if (isAttackSequenceRunning)
        {
            UpdateAttackSequence();
            return;
        }

        if (Time.time < nextAttackTime)
        {
            SetPhase(EnemyAttackPhase.Cooldown);
            return;
        }

        if (combatController.CurrentTarget == null)
        {
            return;
        }

        if (combatController.CurrentTargetDistance > combatData.AttackRange)
        {
            return;
        }

        BeginAttackSequence();
    }

    private void BeginAttackSequence()
    {
        attackSequenceCounter++;
        currentAttackSequenceId = attackSequenceCounter;
        isAttackSequenceRunning = true;
        phaseNormalized = 0f;

        BeginPhase(
            EnemyAttackPhase.Windup,
            combatData.AttackWindupDuration);

        LogMessage(
            $"{combatData.DisplayName} 원거리 공격 준비 / 번호 {currentAttackSequenceId}");

        if (combatData.AttackWindupDuration <= 0f)
        {
            FireProjectile();
            BeginRecovery();
        }
    }

    private void UpdateAttackSequence()
    {
        phaseNormalized = CalculatePhaseNormalized();

        if (Time.time < phaseEndsAt)
        {
            return;
        }

        if (currentPhase == EnemyAttackPhase.Windup)
        {
            FireProjectile();
            BeginRecovery();
            return;
        }

        if (currentPhase == EnemyAttackPhase.Recovery)
        {
            FinishAttackSequence();
        }
    }

    private void FireProjectile()
    {
        Transform targetTransform = combatController.CurrentTarget;

        if (targetTransform == null)
        {
            LogMessage($"{combatData.DisplayName} 원거리 공격 취소 / 대상 없음");
            return;
        }

        float validFireRange =
            combatData.AttackRange + combatData.AttackRangeGraceDistance;

        if (combatController.CurrentTargetDistance > validFireRange)
        {
            LogMessage(
                $"{combatData.DisplayName} 원거리 공격 빗나감 / "
                + $"거리 {combatController.CurrentTargetDistance:0.##} / "
                + $"허용 거리 {validFireRange:0.##}");
            return;
        }

        Vector3 spawnPosition = attackOrigin.position;
        Vector3 targetPosition =
            GetTargetPoint(targetTransform) + Vector3.up * targetHeightOffset;
        Vector3 fireDirection = targetPosition - spawnPosition;

        if (fireDirection.sqrMagnitude < 0.0001f)
        {
            fireDirection = transform.forward;
        }

        fireDirection.Normalize();

        EnemyProjectile projectileInstance = Instantiate(
            projectilePrefab,
            spawnPosition,
            Quaternion.LookRotation(fireDirection));

        projectileInstance.Initialize(
            gameObject,
            combatData.AttackDamage,
            combatData.AttackImpactForce,
            currentAttackSequenceId,
            fireDirection,
            projectileSpeed,
            projectileLifetime,
            projectileHitRadius,
            projectileCollisionMask);

        performedShotCount++;

        LogMessage(
            $"{combatData.DisplayName} 투사체 발사 / "
            + $"번호 {currentAttackSequenceId} / "
            + $"피해 {combatData.AttackDamage:0.##}");
    }

    private Vector3 GetTargetPoint(Transform targetTransform)
    {
        Collider targetCollider = targetTransform.GetComponent<Collider>();

        if (targetCollider == null)
        {
            targetCollider = targetTransform.GetComponentInChildren<Collider>();
        }

        if (targetCollider != null)
        {
            return targetCollider.bounds.center;
        }

        return targetTransform.position;
    }

    private void BeginRecovery()
    {
        BeginPhase(
            EnemyAttackPhase.Recovery,
            combatData.AttackRecoveryDuration);

        if (combatData.AttackRecoveryDuration <= 0f)
        {
            FinishAttackSequence();
        }
    }

    private void FinishAttackSequence()
    {
        isAttackSequenceRunning = false;
        phaseNormalized = 0f;
        nextAttackTime = Time.time + combatData.AttackCooldown;

        SetPhase(
            combatData.AttackCooldown > 0f
                ? EnemyAttackPhase.Cooldown
                : EnemyAttackPhase.Ready);

        LogMessage(
            $"{combatData.DisplayName} 원거리 공격 종료 / 번호 {currentAttackSequenceId}");
    }

    private void CancelCurrentSequence(string reason)
    {
        if (!isAttackSequenceRunning)
        {
            return;
        }

        int cancelledSequenceId = currentAttackSequenceId;
        isAttackSequenceRunning = false;
        phaseNormalized = 0f;
        nextAttackTime = Time.time + combatData.AttackCooldown;

        SetPhase(
            combatData.AttackCooldown > 0f
                ? EnemyAttackPhase.Cooldown
                : EnemyAttackPhase.Ready);

        LogMessage(
            $"{combatData.DisplayName} 원거리 공격 취소 / "
            + $"번호 {cancelledSequenceId} / 사유 {reason}");
    }

    private void BeginPhase(EnemyAttackPhase newPhase, float duration)
    {
        SetPhase(newPhase);
        phaseStartedAt = Time.time;
        phaseEndsAt = Time.time + Mathf.Max(0f, duration);
        phaseNormalized = 0f;
    }

    private float CalculatePhaseNormalized()
    {
        float duration = phaseEndsAt - phaseStartedAt;

        if (duration <= 0f)
        {
            return 1f;
        }

        return Mathf.Clamp01(
            (Time.time - phaseStartedAt) / duration);
    }

    private void RefreshCooldownPhase()
    {
        if (isAttackSequenceRunning)
        {
            return;
        }

        if (currentPhase != EnemyAttackPhase.Cooldown)
        {
            return;
        }

        if (Time.time < nextAttackTime)
        {
            return;
        }

        SetPhase(EnemyAttackPhase.Ready);
    }

    private void SetPhase(EnemyAttackPhase newPhase)
    {
        if (currentPhase == newPhase)
        {
            return;
        }

        EnemyAttackPhase previousPhase = currentPhase;
        currentPhase = newPhase;

        LogMessage(
            $"{combatData.DisplayName} 원거리 공격 단계 / "
            + $"{previousPhase} -> {currentPhase}");
    }

    private void LogMessage(string message)
    {
        if (!logRangedAttackResults)
        {
            return;
        }

        Debug.Log(message, this);
    }

    private void OnValidate()
    {
        projectileSpeed = Mathf.Max(0.1f, projectileSpeed);
        projectileLifetime = Mathf.Max(0.1f, projectileLifetime);
        projectileHitRadius = Mathf.Max(0.01f, projectileHitRadius);

        if (combatController == null)
        {
            combatController = GetComponent<EnemyCombatController>();
        }

        if (attackOrigin == null)
        {
            attackOrigin = transform;
        }
    }
}
