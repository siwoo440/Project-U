public enum MeleeAttackPhase // 근접 공격 진행 단계
{
    None = 0, // 공격 진행 없음
    Windup = 1, // 공격 준비 단계
    Active = 2, // 실제 피해 판정 단계
    Recovery = 3 // 공격 후 복귀 단계
}
