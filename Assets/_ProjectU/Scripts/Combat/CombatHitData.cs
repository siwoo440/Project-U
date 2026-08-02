using UnityEngine; // Unity 기본 기능

public readonly struct CombatHitData // 한 번의 전투 피해 정보
{
    public GameObject Attacker { get; } // 공격 주체
    public ItemData SourceItem { get; } // 공격에 사용한 아이템
    public WeaponAttackType AttackType { get; } // 공격 방식
    public float Damage { get; } // 기본 피해량
    public float ImpactForce { get; } // 향후 넉백에 사용할 충격량
    public Vector3 HitPoint { get; } // 충돌 지점
    public Vector3 HitDirection { get; } // 공격 진행 방향
    public Collider HitCollider { get; } // 실제 충돌한 Collider

    public CombatHitData(
        GameObject attacker,
        ItemData sourceItem,
        WeaponAttackType attackType,
        float damage,
        float impactForce,
        Vector3 hitPoint,
        Vector3 hitDirection,
        Collider hitCollider) // 전투 피해 정보 생성
    {
        Attacker = attacker; // 공격 주체 저장
        SourceItem = sourceItem; // 사용 아이템 저장
        AttackType = attackType; // 공격 방식 저장
        Damage = Mathf.Max(0f, damage); // 피해량 음수 방지
        ImpactForce = Mathf.Max(0f, impactForce); // 충격량 음수 방지
        HitPoint = hitPoint; // 충돌 지점 저장
        HitDirection = hitDirection.sqrMagnitude > 0.0001f
            ? hitDirection.normalized
            : Vector3.forward; // 공격 방향 정규화
        HitCollider = hitCollider; // 충돌 Collider 저장
    }
}
