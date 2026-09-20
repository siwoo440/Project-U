using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyProjectile : MonoBehaviour
{
    private static readonly RaycastHit[] SharedHitBuffer = new RaycastHit[16];

    [Header("Runtime")]
    [SerializeField] private GameObject attacker;
    [SerializeField] private float damage;
    [SerializeField] private float impactForce;
    [SerializeField] private int attackSequenceId;
    [SerializeField] private Vector3 moveDirection = Vector3.forward;
    [SerializeField] private float moveSpeed = 12f;
    [SerializeField] private float remainingLifetime = 5f;
    [SerializeField] private float hitRadius = 0.12f;
    [SerializeField] private LayerMask collisionMask = ~0;
    [SerializeField] private bool initialized;

    [Header("Debug")]
    [SerializeField] private bool logHitResults;

    public void Initialize(
        GameObject newAttacker,
        float newDamage,
        float newImpactForce,
        int newAttackSequenceId,
        Vector3 newMoveDirection,
        float newMoveSpeed,
        float newLifetime,
        float newHitRadius,
        LayerMask newCollisionMask)
    {
        attacker = newAttacker;
        damage = Mathf.Max(0f, newDamage);
        impactForce = Mathf.Max(0f, newImpactForce);
        attackSequenceId = Mathf.Max(0, newAttackSequenceId);
        moveDirection = newMoveDirection.sqrMagnitude > 0.0001f
            ? newMoveDirection.normalized
            : transform.forward;
        moveSpeed = Mathf.Max(0.1f, newMoveSpeed);
        remainingLifetime = Mathf.Max(0.1f, newLifetime);
        hitRadius = Mathf.Max(0.01f, newHitRadius);
        collisionMask = newCollisionMask;
        initialized = true;

        if (moveDirection.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(moveDirection);
        }
    }

    private void Update()
    {
        if (!initialized)
        {
            return;
        }

        remainingLifetime -= Time.deltaTime;

        if (remainingLifetime <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        float travelDistance = moveSpeed * Time.deltaTime;

        if (travelDistance <= 0f)
        {
            return;
        }

        Vector3 startPosition = transform.position;
        // 모든 투사체가 공유하는 버퍼로 매 프레임 배열 할당 제거
        int hitCount = Physics.SphereCastNonAlloc(
            startPosition,
            hitRadius,
            moveDirection,
            SharedHitBuffer,
            travelDistance,
            collisionMask,
            QueryTriggerInteraction.Ignore);

        if (TryFindNearestValidHit(SharedHitBuffer, hitCount, out RaycastHit nearestHit))
        {
            transform.position = startPosition + moveDirection * nearestHit.distance;
            ResolveHit(nearestHit);
            return;
        }

        transform.position = startPosition + moveDirection * travelDistance;
    }

    private bool TryFindNearestValidHit(RaycastHit[] hits, int hitCount, out RaycastHit nearestHit)
    {
        nearestHit = default;
        bool foundHit = false;
        float nearestDistance = float.PositiveInfinity;

        for (int index = 0; index < hitCount; index++)
        {
            RaycastHit currentHit = hits[index];

            if (currentHit.collider == null)
            {
                continue;
            }

            if (IsAttackerCollider(currentHit.collider))
            {
                continue;
            }

            if (currentHit.distance >= nearestDistance)
            {
                continue;
            }

            nearestDistance = currentHit.distance;
            nearestHit = currentHit;
            foundHit = true;
        }

        return foundHit;
    }

    private bool IsAttackerCollider(Collider candidateCollider)
    {
        if (attacker == null || candidateCollider == null)
        {
            return false;
        }

        Transform candidateTransform = candidateCollider.transform;
        Transform attackerTransform = attacker.transform;

        return candidateTransform == attackerTransform
            || candidateTransform.IsChildOf(attackerTransform);
    }

    private void ResolveHit(RaycastHit hit)
    {
        PlayerCombatDamageReceiver playerReceiver =
            hit.collider.GetComponentInParent<PlayerCombatDamageReceiver>();

        bool damageApplied = false;
        NpcCompanionVitality companion = playerReceiver == null ? hit.collider.GetComponentInParent<NpcCompanionVitality>() : null; // 115일차: 동료도 맞음

        if (playerReceiver != null || (companion != null && companion.IsAlive))
        {
            CombatHitData hitData = new CombatHitData(
                attacker,
                null,
                WeaponAttackType.Ranged,
                damage,
                impactForce,
                hit.point,
                moveDirection,
                hit.collider,
                attackSequenceId,
                0);

            damageApplied = playerReceiver != null ? playerReceiver.ReceiveDamage(hitData) : companion.ReceiveDamage(hitData);
        }

        if (logHitResults)
        {
            string targetName = hit.collider == null
                ? "NONE"
                : hit.collider.gameObject.name;

            Debug.Log(
                $"적 투사체 충돌 / 대상 {targetName} / 피해 적용 {damageApplied}",
                this);
        }

        Destroy(gameObject);
    }

    private void OnValidate()
    {
        damage = Mathf.Max(0f, damage);
        impactForce = Mathf.Max(0f, impactForce);
        moveSpeed = Mathf.Max(0.1f, moveSpeed);
        remainingLifetime = Mathf.Max(0.1f, remainingLifetime);
        hitRadius = Mathf.Max(0.01f, hitRadius);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, hitRadius);
    }
}
