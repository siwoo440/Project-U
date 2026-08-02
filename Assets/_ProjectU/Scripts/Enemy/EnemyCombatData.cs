using UnityEngine; // Unity 기본 기능

[CreateAssetMenu( // ScriptableObject 생성 메뉴 설정
    fileName = "EnemyCombatData_", // 새 파일 기본 이름
    menuName = "Project U/Combat/Enemy Combat Data")] // Project 창 생성 메뉴 경로
public sealed class EnemyCombatData : ScriptableObject // 적 공통 전투 능력치 데이터
{
    [Header("Identity")] // 적 식별 정보 묶음
    [Tooltip("저장과 데이터 연결에 사용할 고유 식별자입니다.")] // Inspector 고유 식별자 설명
    [SerializeField] private string enemyId = "enemy_basic"; // 적 고유 식별자

    [Tooltip("UI와 Debug Log에 표시할 적 이름입니다.")] // Inspector 표시 이름 설명
    [SerializeField] private string displayName = "Basic Enemy"; // 적 표시 이름

    [Header("Health")] // 적 체력 설정 묶음
    [Tooltip("적의 최대 체력입니다.")] // Inspector 최대 체력 설명
    [SerializeField, Min(1f)] private float maximumHealth = 50f; // 적 최대 체력

    [Tooltip("받는 피해를 감소시키는 방어력 비율입니다.")] // Inspector 방어력 설명
    [SerializeField, Range(0f, 95f)] private float defensePercent; // 적 방어력 비율

    [Header("Movement")] // 적 이동 설정 묶음
    [Tooltip("향후 NavMesh 추적 이동에 사용할 기본 이동 속도입니다.")] // Inspector 이동 속도 설명
    [SerializeField, Min(0f)] private float moveSpeed = 3f; // 적 기본 이동 속도

    [Tooltip("플레이어 방향으로 회전할 때 사용할 초당 회전 각도입니다.")] // Inspector 회전 속도 설명
    [SerializeField, Min(0f)] private float rotationSpeed = 360f; // 적 회전 속도

    [Header("Detection")] // 적 탐지 설정 묶음
    [Tooltip("대기 상태에서 플레이어를 처음 인식하는 거리입니다.")] // Inspector 탐지 거리 설명
    [SerializeField, Min(0.1f)] private float detectionRange = 10f; // 플레이어 탐지 거리

    [Tooltip("한 번 인식한 플레이어 추적을 포기하는 거리입니다.")] // Inspector 추적 해제 거리 설명
    [SerializeField, Min(0.1f)] private float loseTargetRange = 14f; // 플레이어 추적 해제 거리

    [Header("Attack")] // 적 공격 설정 묶음
    [Tooltip("플레이어에게 근접 공격을 시도할 거리입니다.")] // Inspector 공격 거리 설명
    [SerializeField, Min(0.1f)] private float attackRange = 1.8f; // 적 공격 거리

    [Tooltip("한 번의 공격으로 전달할 기본 전투 피해량입니다.")] // Inspector 공격 피해 설명
    [SerializeField, Min(0f)] private float attackDamage = 10f; // 적 기본 공격 피해량

    [Tooltip("향후 피격 밀림에 사용할 공격 충격량입니다.")] // Inspector 공격 충격량 설명
    [SerializeField, Min(0f)] private float attackImpactForce = 2f; // 적 공격 충격량

    [Tooltip("공격 후 다음 공격까지 기다릴 시간입니다.")] // Inspector 공격 대기시간 설명
    [SerializeField, Min(0.05f)] private float attackCooldown = 1.25f; // 적 공격 재사용 대기시간

    [Header("Reaction")] // 적 반응 설정 묶음
    [Tooltip("피해를 받은 뒤 Hit 상태를 유지할 시간입니다.")] // Inspector 피격 반응 시간 설명
    [SerializeField, Min(0f)] private float hitReactionDuration = 0.2f; // 적 피격 상태 유지 시간

    [Header("Death")] // 적 사망 처리 설정 묶음
    [Tooltip("사망하면 적의 전체 Collider를 비활성화합니다.")] // Inspector 사망 Collider 설명
    [SerializeField] private bool disableCollidersOnDeath = true; // 사망 후 Collider 비활성화 여부

    [Tooltip("사망한 적 오브젝트를 일정 시간 뒤 제거합니다.")] // Inspector 사망 제거 설명
    [SerializeField] private bool destroyAfterDeath; // 사망 후 오브젝트 제거 여부

    [Tooltip("사망 후 오브젝트 제거까지 기다릴 시간입니다.")] // Inspector 사망 제거 시간 설명
    [SerializeField, Min(0f)] private float deathCleanupDelay = 5f; // 사망 오브젝트 제거 대기시간

    public string EnemyId => enemyId; // 적 고유 식별자 제공
    public string DisplayName => displayName; // 적 표시 이름 제공
    public float MaximumHealth => maximumHealth; // 적 최대 체력 제공
    public float DefensePercent => defensePercent; // 적 방어력 비율 제공
    public float MoveSpeed => moveSpeed; // 적 이동 속도 제공
    public float RotationSpeed => rotationSpeed; // 적 회전 속도 제공
    public float DetectionRange => detectionRange; // 적 탐지 거리 제공
    public float LoseTargetRange => loseTargetRange; // 적 추적 해제 거리 제공
    public float AttackRange => attackRange; // 적 공격 거리 제공
    public float AttackDamage => attackDamage; // 적 공격 피해량 제공
    public float AttackImpactForce => attackImpactForce; // 적 공격 충격량 제공
    public float AttackCooldown => attackCooldown; // 적 공격 대기시간 제공
    public float HitReactionDuration => hitReactionDuration; // 적 피격 상태 시간 제공
    public bool DisableCollidersOnDeath => disableCollidersOnDeath; // 사망 Collider 비활성화 여부 제공
    public bool DestroyAfterDeath => destroyAfterDeath; // 사망 후 제거 여부 제공
    public float DeathCleanupDelay => deathCleanupDelay; // 사망 제거 대기시간 제공

    private void OnValidate() // Inspector 입력값 검증
    {
        enemyId = string.IsNullOrWhiteSpace(enemyId) // 고유 식별자 입력 여부 확인
            ? name.ToLowerInvariant() // 비어 있으면 Asset 이름 사용
            : enemyId.Trim(); // 앞뒤 공백 제거

        displayName = string.IsNullOrWhiteSpace(displayName) // 표시 이름 입력 여부 확인
            ? name // 비어 있으면 Asset 이름 사용
            : displayName.Trim(); // 앞뒤 공백 제거

        maximumHealth = Mathf.Max(1f, maximumHealth); // 최대 체력 최소값 적용
        defensePercent = Mathf.Clamp(defensePercent, 0f, 95f); // 방어력 범위 제한
        moveSpeed = Mathf.Max(0f, moveSpeed); // 이동 속도 음수 방지
        rotationSpeed = Mathf.Max(0f, rotationSpeed); // 회전 속도 음수 방지
        detectionRange = Mathf.Max(0.1f, detectionRange); // 탐지 거리 최소값 적용
        loseTargetRange = Mathf.Max(detectionRange, loseTargetRange); // 추적 해제 거리를 탐지 거리 이상으로 제한
        attackRange = Mathf.Clamp(attackRange, 0.1f, detectionRange); // 공격 거리를 탐지 거리 안으로 제한
        attackDamage = Mathf.Max(0f, attackDamage); // 공격 피해량 음수 방지
        attackImpactForce = Mathf.Max(0f, attackImpactForce); // 공격 충격량 음수 방지
        attackCooldown = Mathf.Max(0.05f, attackCooldown); // 공격 대기시간 최소값 적용
        hitReactionDuration = Mathf.Max(0f, hitReactionDuration); // 피격 상태 시간 음수 방지
        deathCleanupDelay = Mathf.Max(0f, deathCleanupDelay); // 사망 제거 시간 음수 방지
    }
}
