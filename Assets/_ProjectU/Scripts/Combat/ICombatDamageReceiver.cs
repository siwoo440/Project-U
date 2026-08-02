using UnityEngine; // Unity 기본 기능

public interface ICombatDamageReceiver // 전투 피해를 받을 수 있는 대상 규칙
{
    Transform DamageRoot { get; } // 중복 Collider를 하나의 대상으로 묶을 기준 Transform
    bool IsAlive { get; } // 현재 피해 수신 가능 상태
    bool ReceiveDamage(CombatHitData hitData); // 전투 피해 수신
}
