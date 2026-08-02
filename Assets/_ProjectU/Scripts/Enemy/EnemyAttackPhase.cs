public enum EnemyAttackPhase // 적 공격 세부 진행 단계
{
    Ready = 0, // 새로운 공격을 시작할 수 있는 준비 상태
    Windup = 1, // 공격 전에 플레이어에게 예고를 보여주는 준비 상태
    Recovery = 2, // 공격 판정 이후 행동할 수 없는 후딜레이 상태
    Cooldown = 3 // 후딜레이 종료 후 다음 공격까지 기다리는 상태
}
