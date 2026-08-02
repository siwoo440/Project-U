public enum EnemyCombatState // 적 공통 전투 상태
{
    Idle = 0, // 플레이어를 인식하지 않은 대기 상태
    Chasing = 1, // 플레이어를 인식했지만 공격 거리 밖인 추적 준비 상태
    Attacking = 2, // 플레이어가 공격 거리 안에 있는 공격 상태
    Hit = 3, // 피해를 받아 짧게 행동이 중단된 피격 상태
    Dead = 4 // 체력이 모두 소진된 사망 상태
}
