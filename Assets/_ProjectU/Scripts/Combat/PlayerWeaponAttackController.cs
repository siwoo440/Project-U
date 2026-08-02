using System.Collections; // 코루틴 기능
using System.Collections.Generic; // HashSet 기능
using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
[RequireComponent(typeof(PlayerInventory))] // 플레이어 인벤토리 요구
[RequireComponent(typeof(PlayerStamina))] // 플레이어 스태미나 요구
public sealed class PlayerWeaponAttackController : MonoBehaviour // 플레이어 무기 공격 관리자
{
    [Header("References")] // 공격 참조 묶음
    [Tooltip("현재 선택 Hotbar 아이템을 확인할 플레이어 인벤토리입니다.")] // Inspector 인벤토리 설명
    [SerializeField] private PlayerInventory playerInventory; // 플레이어 인벤토리

    [Tooltip("공격 단계별 비용을 소비할 플레이어 스태미나입니다.")] // Inspector 스태미나 설명
    [SerializeField] private PlayerStamina playerStamina; // 플레이어 스태미나

    [Tooltip("근접 공격 SphereCast가 시작될 플레이어 기준 위치입니다.")] // Inspector 근접 시작 위치 설명
    [SerializeField] private Transform attackOrigin; // 근접 공격 시작 위치

    [Tooltip("원거리 발사체가 생성될 위치입니다. 비워 두면 Attack Origin을 사용합니다.")] // Inspector 원거리 시작 위치 설명
    [SerializeField] private Transform rangedFireOrigin; // 원거리 발사체 시작 위치

    [Tooltip("화면 중앙이 바라보는 방향을 계산할 Main Camera Transform입니다.")] // Inspector 시선 기준 설명
    [SerializeField] private Transform viewTransform; // 공격 시선 기준

    [Tooltip("공격 단계별 도구 휘두르기 연출입니다.")] // Inspector 휘두르기 설명
    [SerializeField] private ToolSwingAnimation toolSwingAnimation; // 공격 휘두르기 연출

    [Header("Detection")] // 공격 탐지 설정 묶음
    [Tooltip("TrainingDamageTarget과 이후 EnemyHealth가 사용할 전투 대상 레이어입니다.")] // Inspector 피해 레이어 설명
    [SerializeField] private LayerMask damageableLayers; // 전투 피해 대상 레이어

    [Tooltip("1·3인칭 화면 중앙 조준점을 계산할 때 충돌을 확인할 레이어입니다.")] // Inspector 조준 레이어 설명
    [SerializeField] private LayerMask aimBlockingLayers = ~0; // 조준점 탐지 레이어

    [Tooltip("Main Camera에서 화면 중앙 조준점을 탐색할 최대 거리입니다.")] // Inspector 조준 거리 설명
    [SerializeField, Min(1f)] private float maximumAimDistance = 100f; // 조준점 최대 탐지 거리

    [Tooltip("한 프레임의 근접 공격에서 확인할 최대 Collider 결과 수입니다.")] // Inspector 근접 결과 수 설명
    [SerializeField, Range(4, 64)] private int maximumHitResults = 32; // 공격 탐지 결과 배열 크기

    [Header("Unarmed")] // 맨손 공격 설정 묶음
    [Tooltip("공격 가능한 아이템을 선택하지 않았을 때 맨손 공격을 허용할지 설정합니다.")] // Inspector 맨손 허용 설명
    [SerializeField] private bool allowUnarmedAttack = true; // 맨손 공격 허용 여부

    [Tooltip("맨손 공격의 기본 피해량입니다.")] // Inspector 맨손 피해 설명
    [SerializeField, Min(0f)] private float unarmedDamage = 2f; // 맨손 피해량

    [Tooltip("맨손 공격 데이터가 없을 때 사용할 기본 전체 공격 시간입니다.")] // Inspector 맨손 시간 설명
    [SerializeField, Min(0.05f)] private float unarmedCooldown = 0.65f; // 맨손 기본 공격 시간

    [Tooltip("맨손 공격의 기본 거리입니다.")] // Inspector 맨손 거리 설명
    [SerializeField, Min(0.1f)] private float unarmedRange = 1.4f; // 맨손 공격 거리

    [Tooltip("맨손 공격 SphereCast의 기본 반지름입니다.")] // Inspector 맨손 반지름 설명
    [SerializeField, Min(0.01f)] private float unarmedRadius = 0.25f; // 맨손 공격 반지름

    [Tooltip("맨손 공격에 사용할 기본 스태미나입니다.")] // Inspector 맨손 비용 설명
    [SerializeField, Min(0f)] private float unarmedStaminaCost; // 맨손 스태미나 비용

    [Tooltip("맨손 공격의 향후 넉백 계산용 기본 충격량입니다.")] // Inspector 맨손 충격 설명
    [SerializeField, Min(0f)] private float unarmedImpactForce = 1f; // 맨손 충격량

    [Tooltip("맨손 공격에 사용할 연속 공격 데이터입니다. 비어 있으면 단일 공격으로 동작합니다.")] // Inspector 맨손 연속 공격 설명
    [SerializeField] private MeleeComboData unarmedComboData; // 맨손 연속 공격 데이터

    [Header("Debug")] // 공격 확인 설정 묶음
    [Tooltip("공격 실패, 단계 전환과 명중 결과를 Console에 출력할지 설정합니다.")] // Inspector 로그 설명
    [SerializeField] private bool logAttackResults = true; // 공격 로그 출력 여부

    [Header("Runtime")] // 공격 실행 상태 확인 묶음
    [Tooltip("현재 근접 공격이 진행 중인지 표시합니다.")] // Inspector 공격 상태 설명
    [SerializeField] private bool isAttacking; // 현재 근접 공격 진행 여부

    [Tooltip("현재 근접 공격 진행 단계입니다.")] // Inspector 공격 단계 설명
    [SerializeField] private MeleeAttackPhase currentPhase = MeleeAttackPhase.None; // 현재 공격 단계

    [Tooltip("현재 연속 공격 단계 번호입니다.")] // Inspector 연속 단계 설명
    [SerializeField] private int currentComboStepIndex = -1; // 현재 연속 공격 단계 번호

    [Tooltip("현재 공격 단계 진행 비율입니다.")] // Inspector 진행 비율 설명
    [SerializeField, Range(0f, 1f)] private float currentStepNormalizedTime; // 현재 공격 단계 진행 비율

    [Tooltip("다음 연속 공격 입력이 저장되었는지 표시합니다.")] // Inspector 입력 저장 설명
    [SerializeField] private bool hasQueuedAttack; // 다음 연속 공격 입력 저장 여부

    [Tooltip("마지막 원거리 공격 고유 번호입니다.")] // Inspector 원거리 번호 설명
    [SerializeField] private int lastRangedAttackSequenceId; // 마지막 원거리 공격 고유 번호

    private readonly HashSet<Transform> hitDamageRootsThisStep = new HashSet<Transform>(); // 현재 공격 단계에서 이미 피해를 받은 대상
    private RaycastHit[] hitResults = new RaycastHit[32]; // 근접 공격 탐지 결과 배열
    private Coroutine attackCoroutine; // 실행 중인 근접 공격 코루틴
    private ItemData activeAttackItem; // 현재 공격에 사용 중인 아이템
    private AttackProfile activeAttackProfile; // 현재 공격에 사용 중인 기본 능력치
    private MeleeComboData activeComboData; // 현재 실행 중인 연속 공격 데이터
    private MeleeAttackStepData activeStepData; // 현재 실행 중인 공격 단계 데이터
    private GatherableResource activeGatherableTarget; // 현재 공격 단계의 채집 대상
    private GatherableResource queuedGatherableTarget; // 저장된 다음 공격의 채집 대상
    private bool gatherableProcessedThisStep; // 현재 단계 채집 처리 완료 여부
    private int damagedTargetCountThisStep; // 현재 단계에서 피해를 준 서로 다른 대상 수
    private float currentStepElapsedTime; // 현재 공격 단계 누적 시간
    private int attackSequenceCounter; // 공격 단계 고유 번호 생성기
    private int currentAttackSequenceId; // 현재 공격 단계 고유 번호
    private ItemData comboProgressItem; // 다음 연속 공격 단계를 유지할 아이템
    private MeleeComboData comboProgressData; // 다음 연속 공격 단계를 유지할 데이터
    private int nextComboStepIndex; // 다음 입력에서 시작할 연속 공격 단계
    private float comboProgressExpiresAt; // 연속 공격 진행도 만료 시각
    private float nextRangedAttackTime; // 다음 원거리 공격 가능 시각

    public bool IsAttacking => isAttacking; // 현재 근접 공격 진행 여부 제공
    public MeleeAttackPhase CurrentPhase => currentPhase; // 현재 공격 단계 제공
    public int CurrentComboStepIndex => currentComboStepIndex; // 현재 공격 단계 번호 제공
    public int CurrentComboStepNumber => currentComboStepIndex + 1; // 표시용 공격 단계 번호 제공
    public float CurrentStepNormalizedTime => currentStepNormalizedTime; // 현재 공격 진행 비율 제공
    public bool HasQueuedAttack => hasQueuedAttack; // 다음 연속 공격 입력 저장 여부 제공
    public float RangedCooldownRemaining => Mathf.Max(0f, nextRangedAttackTime - Time.time); // 남은 원거리 공격 대기 시간 제공

    private void Awake() // 무기 공격 참조 초기화
    {
        if (playerInventory == null) // 인벤토리 참조 확인
        {
            playerInventory = GetComponent<PlayerInventory>(); // 같은 Player에서 자동 검색
        }

        if (playerStamina == null) // 스태미나 참조 확인
        {
            playerStamina = GetComponent<PlayerStamina>(); // 같은 Player에서 자동 검색
        }

        if (toolSwingAnimation == null) // 휘두르기 연출 참조 확인
        {
            toolSwingAnimation = GetComponentInChildren<ToolSwingAnimation>(true); // Player 자식에서 자동 검색
        }

        if (rangedFireOrigin == null) // 원거리 발사 위치 참조 확인
        {
            rangedFireOrigin = attackOrigin; // 근접 공격 시작 위치를 대체값으로 적용
        }

        ResizeHitResults(); // Inspector 결과 수에 맞는 배열 생성

        bool hasMissingReference = // 값 계산 시작
            playerInventory == null // 값 계산 시작
            || playerStamina == null // 조건 추가
            || attackOrigin == null // 조건 추가
            || viewTransform == null; // 필수 참조 누락 확인

        if (hasMissingReference) // 필수 참조 누락 여부 확인
        {
            Debug.LogError( // 호출 시작
                "PlayerWeaponAttackController의 Player Inventory, Player Stamina, Attack Origin, View Transform을 연결해야 합니다.", // 매개변수 전달
                this); // 공격 참조 오류 출력

            enabled = false; // 무기 공격 기능 비활성화
            return; // 초기화 중단
        }

        if (damageableLayers.value == 0) // 피해 대상 레이어 설정 확인
        {
            Debug.LogWarning( // 호출 시작
                "PlayerWeaponAttackController의 Damageable Layers가 비어 있어 근접 전투 대상에게 피해를 줄 수 없습니다.", // 매개변수 전달
                this); // 피해 레이어 누락 경고 출력
        }
    }

    public bool TryAttack(GatherableResource gatherableTarget) // 선택 아이템으로 공격 또는 채집 시도
    {
        if (!isActiveAndEnabled) // 무기 공격 기능 활성 상태 확인
        {
            return false; // 공격 시작 실패 반환
        }

        ItemData selectedItem = playerInventory.SelectedHotbarItem; // 현재 Hotbar 아이템 조회

        if (isAttacking) // 기존 근접 공격 진행 여부 확인
        {
            return TryQueueNextAttack(selectedItem, gatherableTarget); // 입력 저장 시도 결과 반환
        }

        if (!TryResolveAttackProfile(selectedItem, out AttackProfile attackProfile)) // 아이템 또는 맨손 능력치 계산
        {
            if (logAttackResults) // 공격 실패 로그 사용 여부 확인
            {
                Debug.Log("현재 선택 아이템은 공격 기능이 없습니다.", this); // 공격 불가능 아이템 안내
            }

            return false; // 공격 시작 실패 반환
        }

        if (attackProfile.AttackType == WeaponAttackType.Ranged) // 원거리 공격 방식 확인
        {
            return TryFireRangedAttack(selectedItem, attackProfile); // 원거리 발사 결과 반환
        }

        int startStepIndex = ResolveStartComboStep(selectedItem, attackProfile.ComboData); // 시작 연속 공격 단계 계산
        attackCoroutine = StartCoroutine( // 호출 시작
            AttackRoutine( // 호출 시작
                selectedItem, // 매개변수 전달
                attackProfile, // 매개변수 전달
                attackProfile.ComboData, // 매개변수 전달
                startStepIndex, // 매개변수 전달
                gatherableTarget)); // 근접 공격 단계 실행 시작

        return true; // 근접 공격 입력 처리 성공 반환
    }

    private bool TryFireRangedAttack( // 메서드 선언
        ItemData selectedItem, // 매개변수 전달
        AttackProfile attackProfile) // 원거리 무기 탄약 소비와 발사체 생성
    {
        if (selectedItem == null || attackProfile.RangedData == null) // 원거리 무기 데이터 존재 확인
        {
            if (logAttackResults) // 원거리 설정 오류 로그 사용 여부 확인
            {
                Debug.LogWarning("선택한 원거리 무기에 Ranged Weapon Data가 연결되지 않았습니다.", this); // 원거리 데이터 누락 안내
            }

            return false; // 원거리 공격 실패 반환
        }

        RangedWeaponData rangedData = attackProfile.RangedData; // 원거리 무기 데이터 조회

        if (rangedData.ProjectilePrefab == null) // 발사체 프리팹 존재 확인
        {
            if (logAttackResults) // 발사체 오류 로그 사용 여부 확인
            {
                Debug.LogWarning($"{attackProfile.DisplayName}의 Projectile Prefab이 비어 있습니다.", this); // 발사체 누락 안내
            }

            return false; // 원거리 공격 실패 반환
        }

        if (Time.time < nextRangedAttackTime) // 원거리 공격 재사용 대기시간 확인
        {
            return false; // 대기시간 중 발사 차단
        }

        if (rangedData.RequiresAmmunition // 조건 검사
            && !playerInventory.HasItem(rangedData.AmmunitionItem, rangedData.AmmunitionPerShot)) // 탄약 보유량 확인
        {
            if (logAttackResults) // 탄약 부족 로그 사용 여부 확인
            {
                Debug.Log($"{attackProfile.DisplayName} 발사에 필요한 탄약이 부족합니다.", this); // 탄약 부족 안내
            }

            return false; // 원거리 공격 실패 반환
        }

        if (!playerStamina.CanConsume(attackProfile.StaminaCost)) // 스태미나 소비 가능 여부 확인
        {
            if (logAttackResults) // 스태미나 부족 로그 사용 여부 확인
            {
                Debug.Log($"스태미나가 부족하여 {attackProfile.DisplayName}을 발사할 수 없습니다.", this); // 스태미나 부족 안내
            }

            return false; // 원거리 공격 실패 반환
        }

        if (rangedData.RequiresAmmunition) // 탄약 소비 필요 여부 확인
        {
            int removedAmmunition = playerInventory.RemoveItem( // 호출 시작
                rangedData.AmmunitionItem, // 매개변수 전달
                rangedData.AmmunitionPerShot); // 인벤토리에서 탄약 제거

            if (removedAmmunition < rangedData.AmmunitionPerShot) // 실제 탄약 제거 결과 확인
            {
                Debug.LogError("탄약 보유 확인 후 실제 탄약 제거에 실패했습니다.", this); // 탄약 처리 오류 출력
                return false; // 원거리 공격 실패 반환
            }
        }

        if (!playerStamina.TryConsume(attackProfile.StaminaCost)) // 스태미나 실제 소비 시도
        {
            Debug.LogError("스태미나 보유 확인 후 실제 스태미나 소비에 실패했습니다.", this); // 스태미나 처리 오류 출력
            return false; // 원거리 공격 실패 반환
        }

        Transform fireOrigin = rangedFireOrigin == null ? attackOrigin : rangedFireOrigin; // 실제 발사 위치 결정
        Vector3 fireDirection = ResolveAttackDirection(fireOrigin.position); // 발사 위치 기준 조준 방향 계산
        Vector3 spawnPosition = fireOrigin.position // 값 계산 시작
            + fireDirection * rangedData.SpawnForwardOffset; // 공격자 앞쪽 발사체 생성 위치 계산
        Quaternion spawnRotation = Quaternion.LookRotation(fireDirection, Vector3.up); // 발사 방향 회전 계산
        int sequenceId = ++attackSequenceCounter; // 새로운 원거리 공격 고유 번호 생성
        CombatProjectile projectile = Instantiate( // 호출 시작
            rangedData.ProjectilePrefab, // 매개변수 전달
            spawnPosition, // 매개변수 전달
            spawnRotation); // 원거리 발사체 프리팹 생성

        projectile.Initialize( // 호출 시작
            gameObject, // 매개변수 전달
            selectedItem, // 매개변수 전달
            attackProfile.Damage, // 매개변수 전달
            attackProfile.ImpactForce, // 매개변수 전달
            fireDirection, // 매개변수 전달
            rangedData.ProjectileSpeed, // 매개변수 전달
            attackProfile.Range, // 매개변수 전달
            rangedData.MaximumLifetime, // 매개변수 전달
            rangedData.UseGravity, // 매개변수 전달
            sequenceId); // 발사체 공격 정보와 이동값 전달

        lastRangedAttackSequenceId = sequenceId; // 마지막 원거리 공격 번호 저장
        nextRangedAttackTime = Time.time + attackProfile.Cooldown; // 다음 발사 가능 시각 저장
        ResetComboProgress(); // 원거리 공격 시 근접 연속 진행도 초기화

        if (logAttackResults) // 발사 성공 로그 사용 여부 확인
        {
            int remainingAmmunition = rangedData.RequiresAmmunition // 값 계산 시작
                ? playerInventory.GetItemQuantity(rangedData.AmmunitionItem) // 참 조건 값
                : -1; // 발사 후 남은 탄약 수량 계산
            string ammunitionText = rangedData.RequiresAmmunition // 값 계산 시작
                ? $"남은 탄약 {remainingAmmunition}" // 참 조건 값
                : "무제한 탄약"; // 탄약 표시 문구 계산

            Debug.Log( // 호출 시작
                $"{attackProfile.DisplayName} 발사 / 피해 {attackProfile.Damage:0.##} / {ammunitionText}", // 매개변수 전달
                this); // 원거리 발사 결과 출력
        }

        return true; // 원거리 공격 성공 반환
    }

    public void CancelCurrentAttack() // UI, 건축 모드와 컴포넌트 비활성화 시 근접 공격 즉시 취소
    {
        if (attackCoroutine != null) // 실행 중인 공격 코루틴 확인
        {
            StopCoroutine(attackCoroutine); // 현재 공격 코루틴 중단
            attackCoroutine = null; // 코루틴 상태 초기화
        }

        if (toolSwingAnimation != null) // 휘두르기 연출 존재 확인
        {
            toolSwingAnimation.CancelSwing(); // 현재 휘두르기 연출 취소
        }

        ResetRuntimeAttackState(); // 현재 공격 실행 상태 초기화
        ResetComboProgress(); // 연속 공격 진행도 초기화
    }

    private bool TryQueueNextAttack( // 메서드 선언
        ItemData selectedItem, // 매개변수 전달
        GatherableResource gatherableTarget) // 현재 공격 중 다음 연속 입력 저장
    {
        if (activeComboData == null || activeComboData.StepCount <= 1) // 연속 공격 데이터 존재 확인
        {
            return false; // 다음 공격 입력 저장 불가
        }

        if (selectedItem != activeAttackItem) // 공격 도중 선택 아이템 변경 여부 확인
        {
            return false; // 다른 아이템의 공격 입력 저장 차단
        }

        if (currentComboStepIndex < 0 // 조건 검사
            || currentComboStepIndex >= activeComboData.StepCount - 1) // 마지막 공격 단계 여부 확인
        {
            return false; // 마지막 단계 이후 입력 저장 차단
        }

        if (hasQueuedAttack) // 이미 다음 입력이 저장되었는지 확인
        {
            return false; // 한 단계당 하나의 입력만 저장
        }

        if (!IsInputBufferOpen()) // 현재 연속 입력 저장 구간 확인
        {
            return false; // 너무 이른 연속 입력 차단
        }

        hasQueuedAttack = true; // 다음 연속 공격 입력 저장
        queuedGatherableTarget = gatherableTarget; // 다음 공격의 채집 대상 저장

        if (logAttackResults) // 입력 저장 로그 사용 여부 확인
        {
            Debug.Log( // 호출 시작
                $"{activeAttackProfile.DisplayName} {currentComboStepIndex + 2}단 공격 입력 저장", // 매개변수 전달
                this); // 다음 연속 공격 입력 저장 결과 출력
        }

        return true; // 연속 공격 입력 저장 성공 반환
    }

    private IEnumerator AttackRoutine( // 메서드 선언
        ItemData selectedItem, // 매개변수 전달
        AttackProfile attackProfile, // 매개변수 전달
        MeleeComboData comboData, // 매개변수 전달
        int startStepIndex, // 매개변수 전달
        GatherableResource gatherableTarget) // 근접 공격과 저장된 연속 공격 실행
    {
        isAttacking = true; // 공격 진행 상태 적용
        activeAttackItem = selectedItem; // 현재 공격 아이템 저장
        activeAttackProfile = attackProfile; // 현재 기본 공격 능력치 저장
        activeComboData = comboData; // 현재 연속 공격 데이터 저장

        int stepIndex = startStepIndex; // 첫 실행 공격 단계 저장
        GatherableResource stepGatherableTarget = gatherableTarget; // 첫 공격 단계 채집 대상 저장

        while (true) // 저장된 연속 공격이 끝날 때까지 반복
        {
            MeleeAttackStepData stepData = // 값 계산 시작
                ResolveStepData(comboData, stepIndex, attackProfile.Cooldown); // 현재 공격 단계 데이터 조회

            float stepStaminaCost = // 값 계산 시작
                attackProfile.StaminaCost * stepData.StaminaCostMultiplier; // 현재 단계 스태미나 비용 계산

            if (!playerStamina.TryConsume(stepStaminaCost)) // 현재 단계 스태미나 소비 시도
            {
                if (logAttackResults) // 스태미나 실패 로그 사용 여부 확인
                {
                    Debug.Log( // 호출 시작
                        $"스태미나가 부족하여 {attackProfile.DisplayName} {stepIndex + 1}단 공격을 실행할 수 없습니다.", // 매개변수 전달
                        this); // 단계별 스태미나 부족 안내
                }

                ResetComboProgress(); // 스태미나 부족 시 연속 공격 초기화
                break; // 공격 실행 반복 종료
            }

            BeginAttackStep( // 호출 시작
                stepIndex, // 매개변수 전달
                stepData, // 매개변수 전달
                stepGatherableTarget); // 현재 공격 단계 실행 상태 초기화

            if (logAttackResults) // 공격 단계 로그 사용 여부 확인
            {
                Debug.Log( // 호출 시작
                    $"{attackProfile.DisplayName} {stepIndex + 1}단 시작 / {stepData.StepName}", // 매개변수 전달
                    this); // 공격 단계 시작 결과 출력
            }

            yield return RunWindupPhase(stepData); // 공격 준비 단계 진행

            Vector3 attackDirection = ResolveAttackDirection(); // 타격 시작 시 공격 방향 고정
            yield return RunActivePhase( // 호출 시작
                selectedItem, // 매개변수 전달
                attackProfile, // 매개변수 전달
                stepData, // 매개변수 전달
                attackDirection); // 실제 피해와 채집 판정 단계 진행

            yield return RunRecoveryPhase(stepData); // 공격 후 복귀 단계 진행

            int followingStepIndex = stepIndex + 1; // 다음 연속 공격 단계 번호 계산
            bool hasFollowingStep = // 값 계산 시작
                comboData != null // 값 계산 시작
                && followingStepIndex < comboData.StepCount; // 다음 공격 단계 존재 여부 계산

            if (hasQueuedAttack && hasFollowingStep) // 저장된 입력과 다음 단계 존재 확인
            {
                stepIndex = followingStepIndex; // 다음 연속 공격 단계로 이동
                stepGatherableTarget = queuedGatherableTarget; // 저장된 다음 채집 대상 적용
                hasQueuedAttack = false; // 저장된 입력 소비
                queuedGatherableTarget = null; // 저장된 채집 대상 제거
                continue; // 다음 공격 단계 즉시 시작
            }

            StoreComboProgress( // 호출 시작
                selectedItem, // 매개변수 전달
                comboData, // 매개변수 전달
                followingStepIndex); // 다음 입력을 위한 연속 공격 진행도 저장
            break; // 현재 공격 실행 반복 종료
        }

        attackCoroutine = null; // 공격 코루틴 상태 초기화
        ResetRuntimeAttackState(); // 현재 공격 실행 상태 초기화
    }

    private void BeginAttackStep( // 메서드 선언
        int stepIndex, // 매개변수 전달
        MeleeAttackStepData stepData, // 매개변수 전달
        GatherableResource gatherableTarget) // 공격 단계 실행 상태 초기화
    {
        currentComboStepIndex = stepIndex; // 현재 연속 공격 단계 번호 저장
        activeStepData = stepData; // 현재 공격 단계 데이터 저장
        activeGatherableTarget = gatherableTarget; // 현재 공격 단계 채집 대상 저장
        gatherableProcessedThisStep = false; // 채집 처리 상태 초기화
        damagedTargetCountThisStep = 0; // 피해 대상 수 초기화
        hitDamageRootsThisStep.Clear(); // 중복 피해 방지 대상 목록 초기화
        currentStepElapsedTime = 0f; // 공격 단계 누적 시간 초기화
        currentStepNormalizedTime = 0f; // 공격 단계 진행 비율 초기화
        currentAttackSequenceId = ++attackSequenceCounter; // 새로운 공격 단계 고유 번호 생성
        hasQueuedAttack = false; // 이전 입력 저장 상태 초기화
        queuedGatherableTarget = null; // 이전 저장 채집 대상 제거

        if (toolSwingAnimation != null) // 휘두르기 연출 존재 확인
        {
            toolSwingAnimation.PlaySwing( // 호출 시작
                stepIndex, // 매개변수 전달
                stepData.AnimationSpeedMultiplier); // 공격 단계별 휘두르기 방향과 속도 적용
        }
    }

    private IEnumerator RunWindupPhase( // 메서드 선언
        MeleeAttackStepData stepData) // 공격 준비 단계 진행
    {
        currentPhase = MeleeAttackPhase.Windup; // 현재 준비 단계 적용
        yield return RunTimedPhase(stepData.WindupDuration, stepData); // 준비 시간 진행
    }

    private IEnumerator RunActivePhase( // 메서드 선언
        ItemData selectedItem, // 매개변수 전달
        AttackProfile attackProfile, // 매개변수 전달
        MeleeAttackStepData stepData, // 매개변수 전달
        Vector3 attackDirection) // 실제 타격 유효 단계 진행
    {
        currentPhase = MeleeAttackPhase.Active; // 현재 타격 단계 적용
        float phaseElapsedTime = 0f; // 타격 단계 진행 시간 초기화

        while (phaseElapsedTime < stepData.ActiveDuration) // 타격 유효 시간 동안 반복
        {
            ProcessActiveAttackFrame( // 호출 시작
                selectedItem, // 매개변수 전달
                attackProfile, // 매개변수 전달
                stepData, // 매개변수 전달
                attackDirection); // 현재 프레임 피해 또는 채집 판정

            float deltaTime = Time.deltaTime; // 현재 프레임 시간 조회
            phaseElapsedTime += deltaTime; // 타격 단계 시간 누적
            AdvanceStepTime(deltaTime, stepData); // 전체 공격 단계 진행도 갱신
            yield return null; // 다음 프레임까지 대기
        }

        ProcessActiveAttackFrame( // 호출 시작
            selectedItem, // 매개변수 전달
            attackProfile, // 매개변수 전달
            stepData, // 매개변수 전달
            attackDirection); // 마지막 프레임 경계의 피해 판정 보장
    }

    private IEnumerator RunRecoveryPhase( // 메서드 선언
        MeleeAttackStepData stepData) // 공격 후 복귀 단계 진행
    {
        currentPhase = MeleeAttackPhase.Recovery; // 현재 복귀 단계 적용
        yield return RunTimedPhase(stepData.RecoveryDuration, stepData); // 복귀 시간 진행
    }

    private IEnumerator RunTimedPhase( // 메서드 선언
        float duration, // 매개변수 전달
        MeleeAttackStepData stepData) // 피해 판정 없는 공격 단계 시간 진행
    {
        float phaseElapsedTime = 0f; // 현재 단계 진행 시간 초기화

        while (phaseElapsedTime < duration) // 설정 시간 동안 반복
        {
            float deltaTime = Time.deltaTime; // 현재 프레임 시간 조회
            phaseElapsedTime += deltaTime; // 현재 단계 시간 누적
            AdvanceStepTime(deltaTime, stepData); // 전체 공격 단계 진행도 갱신
            yield return null; // 다음 프레임까지 대기
        }
    }

    private void AdvanceStepTime( // 메서드 선언
        float deltaTime, // 매개변수 전달
        MeleeAttackStepData stepData) // 전체 공격 단계 진행도 갱신
    {
        currentStepElapsedTime += Mathf.Max(0f, deltaTime); // 전체 공격 단계 시간 누적
        currentStepNormalizedTime = stepData.TotalDuration <= 0f // 값 계산 시작
            ? 1f // 참 조건 값
            : Mathf.Clamp01(currentStepElapsedTime / stepData.TotalDuration); // 전체 공격 단계 진행 비율 계산
    }

    private void ProcessActiveAttackFrame( // 메서드 선언
        ItemData selectedItem, // 매개변수 전달
        AttackProfile attackProfile, // 매개변수 전달
        MeleeAttackStepData stepData, // 매개변수 전달
        Vector3 attackDirection) // 타격 유효 시간의 피해 또는 채집 처리
    {
        if (activeGatherableTarget != null) // 현재 채집 대상 존재 확인
        {
            if (gatherableProcessedThisStep) // 현재 단계 채집 완료 여부 확인
            {
                return; // 같은 공격 단계의 중복 채집 차단
            }

            activeGatherableTarget.Interact(gameObject); // 기존 자원 채집 규칙 실행
            gatherableProcessedThisStep = true; // 현재 단계 채집 완료 저장
            return; // 전투 피해 판정 생략
        }

        PerformMeleeDamageFrame( // 호출 시작
            selectedItem, // 매개변수 전달
            attackProfile, // 매개변수 전달
            stepData, // 매개변수 전달
            attackDirection); // 현재 타격 범위의 전투 대상 피해 처리
    }

    private void PerformMeleeDamageFrame( // 메서드 선언
        ItemData selectedItem, // 매개변수 전달
        AttackProfile attackProfile, // 매개변수 전달
        MeleeAttackStepData stepData, // 매개변수 전달
        Vector3 attackDirection) // 한 프레임 근접 피해 판정
    {
        if (damageableLayers.value == 0) // 피해 대상 레이어 설정 확인
        {
            return; // 피해 판정 생략
        }

        if (damagedTargetCountThisStep >= stepData.MaximumTargets) // 현재 단계 최대 피해 대상 수 확인
        {
            return; // 추가 피해 대상 탐색 중단
        }

        float attackRange = // 값 계산 시작
            attackProfile.Range * stepData.RangeMultiplier; // 현재 단계 최종 공격 거리 계산
        float attackRadius = // 값 계산 시작
            attackProfile.Radius * stepData.RadiusMultiplier; // 현재 단계 최종 공격 반지름 계산

        int hitCount = Physics.SphereCastNonAlloc( // 호출 시작
            attackOrigin.position, // 매개변수 전달
            attackRadius, // 매개변수 전달
            attackDirection, // 매개변수 전달
            hitResults, // 매개변수 전달
            attackRange, // 매개변수 전달
            damageableLayers, // 매개변수 전달
            QueryTriggerInteraction.Ignore); // 현재 공격 범위의 Collider 탐지

        while (damagedTargetCountThisStep < stepData.MaximumTargets) // 남은 피해 대상 수만큼 반복
        {
            ICombatDamageReceiver nearestReceiver = null; // 가장 가까운 미피해 대상
            Component nearestReceiverComponent = null; // 가장 가까운 피해 수신 컴포넌트
            Transform nearestDamageRoot = null; // 가장 가까운 피해 대상 기준 Transform
            Collider nearestCollider = null; // 가장 가까운 충돌 Collider
            Vector3 nearestHitPoint = Vector3.zero; // 가장 가까운 충돌 지점
            float nearestDistance = float.MaxValue; // 가장 가까운 충돌 거리

            for (int index = 0; index < hitCount; index++) // 전체 충돌 결과 순회
            {
                RaycastHit currentHit = hitResults[index]; // 현재 충돌 결과 조회

                if (currentHit.collider == null) // Collider 존재 확인
                {
                    continue; // 잘못된 충돌 결과 제외
                }

                if (currentHit.collider.transform.IsChildOf(transform.root)) // Player 자신의 Collider 확인
                {
                    continue; // 자기 자신 공격 제외
                }

                ICombatDamageReceiver candidate = // 값 계산 시작
                    currentHit.collider.GetComponentInParent<ICombatDamageReceiver>(); // 부모에서 피해 수신 대상 검색

                Component candidateComponent = candidate as Component; // Unity Component 참조 변환

                if (candidate == null // 조건 검사
                    || candidateComponent == null // 조건 추가
                    || !candidate.IsAlive) // 유효하고 생존한 피해 대상 확인
                {
                    continue; // 피해 불가능 대상 제외
                }

                Transform damageRoot = candidate.DamageRoot == null // 값 계산 시작
                    ? candidateComponent.transform // 참 조건 값
                    : candidate.DamageRoot; // 중복 Collider 기준 Transform 결정

                if (damageRoot == transform.root // 조건 검사
                    || damageRoot.IsChildOf(transform.root) // 조건 추가
                    || hitDamageRootsThisStep.Contains(damageRoot)) // 자기 자신과 현재 단계 기존 피해 대상 확인
                {
                    continue; // 중복 피해 대상 제외
                }

                if (currentHit.distance >= nearestDistance) // 기존 후보보다 가까운지 확인
                {
                    continue; // 더 먼 대상 제외
                }

                nearestDistance = currentHit.distance; // 가장 가까운 거리 갱신
                nearestReceiver = candidate; // 가장 가까운 피해 대상 저장
                nearestReceiverComponent = candidateComponent; // 가장 가까운 Component 저장
                nearestDamageRoot = damageRoot; // 가장 가까운 피해 기준 Transform 저장
                nearestCollider = currentHit.collider; // 가장 가까운 Collider 저장
                nearestHitPoint = currentHit.point == Vector3.zero // 값 계산 시작
                    ? currentHit.collider.ClosestPoint(attackOrigin.position) // 참 조건 값
                    : currentHit.point; // 실제 충돌 지점 계산
            }

            if (nearestReceiver == null // 조건 검사
                || nearestReceiverComponent == null // 조건 추가
                || nearestDamageRoot == null) // 추가 명중 대상 존재 확인
            {
                break; // 현재 프레임 추가 피해 대상 탐색 종료
            }

            float finalDamage = // 값 계산 시작
                attackProfile.Damage * stepData.DamageMultiplier; // 현재 단계 최종 피해량 계산
            float finalImpactForce = // 값 계산 시작
                attackProfile.ImpactForce * stepData.ImpactForceMultiplier; // 현재 단계 최종 충격량 계산

            CombatHitData hitData = new CombatHitData( // 호출 시작
                gameObject, // 매개변수 전달
                selectedItem, // 매개변수 전달
                attackProfile.AttackType, // 매개변수 전달
                finalDamage, // 매개변수 전달
                finalImpactForce, // 매개변수 전달
                nearestHitPoint, // 매개변수 전달
                attackDirection, // 매개변수 전달
                nearestCollider, // 매개변수 전달
                currentAttackSequenceId, // 매개변수 전달
                currentComboStepIndex); // 연속 공격 단계가 포함된 공통 피해 정보 생성

            hitDamageRootsThisStep.Add(nearestDamageRoot); // 현재 단계 피해 완료 대상 기록
            damagedTargetCountThisStep++; // 현재 단계 피해 대상 수 증가
            bool damageApplied = nearestReceiver.ReceiveDamage(hitData); // 대상에게 실제 피해 전달

            if (logAttackResults && damageApplied) // 피해 적용과 로그 사용 여부 확인
            {
                Debug.Log( // 호출 시작
                    $"{attackProfile.DisplayName} {currentComboStepIndex + 1}단 명중: " // 코드 연결
                    + $"{nearestReceiverComponent.gameObject.name} / 피해 {finalDamage:0.##}", // 매개변수 전달
                    this); // 단계별 공격 명중 결과 출력
            }
        }
    }

    private bool TryResolveAttackProfile( // 메서드 선언
        ItemData selectedItem, // 매개변수 전달
        out AttackProfile attackProfile) // 선택 아이템 또는 맨손 공격 능력치 계산
    {
        if (selectedItem != null && selectedItem.CanAttack) // 공격 가능한 아이템 확인
        {
            attackProfile = new AttackProfile( // 호출 시작
                selectedItem.DisplayName, // 매개변수 전달
                selectedItem.WeaponAttackType, // 매개변수 전달
                selectedItem.BaseDamage, // 매개변수 전달
                selectedItem.AttackCooldown, // 매개변수 전달
                selectedItem.AttackRange, // 매개변수 전달
                selectedItem.AttackRadius, // 매개변수 전달
                selectedItem.StaminaCost, // 매개변수 전달
                selectedItem.ImpactForce, // 매개변수 전달
                selectedItem.MeleeComboData, // 매개변수 전달
                selectedItem.RangedWeaponData); // ItemData 전투 능력치와 공격 데이터 복사

            return true; // 아이템 공격 능력치 계산 성공
        }

        if (!allowUnarmedAttack) // 맨손 공격 허용 여부 확인
        {
            attackProfile = default; // 빈 공격 능력치 반환
            return false; // 공격 능력치 계산 실패
        }

        attackProfile = new AttackProfile( // 호출 시작
            "UNARMED", // 매개변수 전달
            WeaponAttackType.Melee, // 매개변수 전달
            unarmedDamage, // 매개변수 전달
            unarmedCooldown, // 매개변수 전달
            unarmedRange, // 매개변수 전달
            unarmedRadius, // 매개변수 전달
            unarmedStaminaCost, // 매개변수 전달
            unarmedImpactForce, // 매개변수 전달
            unarmedComboData, // 매개변수 전달
            null); // 맨손 공격 능력치 생성

        return true; // 맨손 공격 능력치 계산 성공
    }

    private int ResolveStartComboStep( // 메서드 선언
        ItemData selectedItem, // 매개변수 전달
        MeleeComboData comboData) // 새 공격 입력의 시작 연속 공격 단계 계산
    {
        if (comboData == null || comboData.StepCount <= 1) // 연속 공격 데이터 존재 확인
        {
            ResetComboProgress(); // 단일 공격은 연속 진행도 제거
            return 0; // 첫 공격 단계 반환
        }

        bool canContinueCombo = // 값 계산 시작
            comboProgressItem == selectedItem // 값 계산 시작
            && comboProgressData == comboData // 조건 추가
            && Time.time <= comboProgressExpiresAt // 조건 추가
            && nextComboStepIndex > 0 // 조건 추가
            && nextComboStepIndex < comboData.StepCount; // 기존 연속 공격 진행 가능 여부 계산

        if (!canContinueCombo) // 연속 공격 진행 불가 확인
        {
            ResetComboProgress(); // 만료되거나 다른 아이템 진행도 제거
            return 0; // 첫 공격 단계 반환
        }

        return nextComboStepIndex; // 저장된 다음 공격 단계 반환
    }

    private MeleeAttackStepData ResolveStepData( // 메서드 선언
        MeleeComboData comboData, // 매개변수 전달
        int stepIndex, // 매개변수 전달
        float fallbackCooldown) // 현재 공격 단계 데이터 조회
    {
        if (comboData == null || comboData.StepCount == 0) // 연속 공격 데이터 존재 확인
        {
            return MeleeAttackStepData.CreateFallback(fallbackCooldown); // 기본 단일 공격 단계 반환
        }

        MeleeAttackStepData stepData = comboData.GetStep(stepIndex); // 지정 공격 단계 조회

        if (stepData != null) // 공격 단계 조회 성공 확인
        {
            return stepData; // 지정 공격 단계 반환
        }

        return MeleeAttackStepData.CreateFallback(fallbackCooldown); // 잘못된 배열 요소의 기본 공격 단계 반환
    }

    private bool IsInputBufferOpen() // 현재 다음 연속 공격 입력 저장 가능 여부 계산
    {
        if (activeStepData == null) // 현재 공격 단계 데이터 존재 확인
        {
            return false; // 입력 저장 불가 반환
        }

        return currentStepNormalizedTime >= activeStepData.InputBufferStartNormalized; // 설정된 진행 비율 이후 입력 저장 허용
    }

    private void StoreComboProgress( // 메서드 선언
        ItemData selectedItem, // 매개변수 전달
        MeleeComboData comboData, // 매개변수 전달
        int followingStepIndex) // 현재 공격 종료 후 다음 단계 진행도 저장
    {
        if (comboData == null // 조건 검사
            || comboData.StepCount <= 1 // 조건 추가
            || followingStepIndex <= 0 // 조건 추가
            || followingStepIndex >= comboData.StepCount) // 다음 연속 공격 단계 유효성 확인
        {
            ResetComboProgress(); // 마지막 단계 또는 단일 공격 진행도 초기화
            return; // 저장 처리 종료
        }

        comboProgressItem = selectedItem; // 연속 공격을 이어갈 아이템 저장
        comboProgressData = comboData; // 연속 공격 데이터 저장
        nextComboStepIndex = followingStepIndex; // 다음 시작 공격 단계 저장
        comboProgressExpiresAt = // 값 계산 시작
            Time.time + comboData.ComboResetDelay; // 연속 공격 진행도 만료 시각 저장
    }

    private void ResetComboProgress() // 다음 연속 공격 진행도 초기화
    {
        comboProgressItem = null; // 진행 중인 아이템 제거
        comboProgressData = null; // 진행 중인 데이터 제거
        nextComboStepIndex = 0; // 다음 공격 단계 첫 번째로 초기화
        comboProgressExpiresAt = 0f; // 진행도 만료 시각 초기화
    }

    private void ResetRuntimeAttackState() // 현재 실행 중인 공격 상태 초기화
    {
        isAttacking = false; // 공격 진행 상태 해제
        currentPhase = MeleeAttackPhase.None; // 공격 단계 없음 적용
        currentComboStepIndex = -1; // 연속 공격 단계 번호 초기화
        currentStepNormalizedTime = 0f; // 공격 진행 비율 초기화
        hasQueuedAttack = false; // 저장된 다음 입력 제거
        activeAttackItem = null; // 현재 공격 아이템 제거
        activeAttackProfile = default; // 현재 기본 공격 능력치 초기화
        activeComboData = null; // 현재 연속 공격 데이터 제거
        activeStepData = null; // 현재 공격 단계 데이터 제거
        activeGatherableTarget = null; // 현재 채집 대상 제거
        queuedGatherableTarget = null; // 저장된 채집 대상 제거
        gatherableProcessedThisStep = false; // 채집 처리 상태 초기화
        damagedTargetCountThisStep = 0; // 피해 대상 수 초기화
        currentStepElapsedTime = 0f; // 공격 단계 누적 시간 초기화
        currentAttackSequenceId = 0; // 현재 공격 고유 번호 초기화
        hitDamageRootsThisStep.Clear(); // 중복 피해 방지 대상 목록 제거
    }

    private Vector3 ResolveAttackDirection() // 근접 공격 위치 기준 조준 방향 계산
    {
        return ResolveAttackDirection(attackOrigin.position); // 근접 공격 위치를 사용한 방향 반환
    }

    private Vector3 ResolveAttackDirection(Vector3 originPosition) // 1·3인칭 화면 중앙 기준 공격 방향 계산
    {
        Vector3 fallbackDirection = viewTransform.forward.normalized; // 기본 Camera 전방 방향 계산

        if (aimBlockingLayers.value == 0) // 조준 차단 레이어 설정 확인
        {
            return fallbackDirection; // Camera 전방 방향 반환
        }

        bool hasAimHit = Physics.Raycast( // 호출 시작
            viewTransform.position, // 매개변수 전달
            fallbackDirection, // 매개변수 전달
            out RaycastHit aimHit, // 매개변수 전달
            maximumAimDistance, // 매개변수 전달
            aimBlockingLayers, // 매개변수 전달
            QueryTriggerInteraction.Ignore); // Camera 화면 중앙의 월드 조준점 탐색

        if (!hasAimHit) // 조준점 미탐지 확인
        {
            return fallbackDirection; // Camera 전방 방향 반환
        }

        Vector3 originToAimPoint = aimHit.point - originPosition; // 공격 시작 위치에서 조준점까지 방향 계산

        if (originToAimPoint.sqrMagnitude <= 0.0001f) // 유효한 방향 길이 확인
        {
            return fallbackDirection; // Camera 전방 방향 반환
        }

        return originToAimPoint.normalized; // 3인칭 Camera 보정 공격 방향 반환
    }

    private void ResizeHitResults() // 공격 탐지 결과 배열 크기 적용
    {
        int safeResultCount = Mathf.Clamp(maximumHitResults, 4, 64); // Inspector 결과 수 범위 제한

        if (hitResults != null && hitResults.Length == safeResultCount) // 기존 배열 크기 확인
        {
            return; // 배열 재생성 생략
        }

        hitResults = new RaycastHit[safeResultCount]; // 새로운 공격 탐지 배열 생성
    }

    private void OnDrawGizmosSelected() // 현재 선택 무기 공격 범위 시각화
    {
        if (attackOrigin == null || viewTransform == null) // 공격 기준 참조 확인
        {
            return; // 기즈모 표시 중단
        }

        float previewRange = unarmedRange; // 기본 기즈모 공격 거리
        float previewRadius = unarmedRadius; // 기본 기즈모 공격 반지름
        bool isRangedPreview = false; // 원거리 기즈모 여부 초기화

        if (playerInventory != null // 조건 검사
            && playerInventory.SelectedHotbarItem != null // 조건 추가
            && playerInventory.SelectedHotbarItem.CanAttack) // 현재 선택 무기 데이터 확인
        {
            ItemData previewItem = playerInventory.SelectedHotbarItem; // 현재 선택 아이템 조회
            previewRange = previewItem.AttackRange; // 선택 무기 거리 적용
            previewRadius = previewItem.AttackRadius; // 선택 무기 반지름 적용
            isRangedPreview = previewItem.WeaponAttackType == WeaponAttackType.Ranged; // 원거리 무기 여부 계산
        }

        Transform previewOrigin = isRangedPreview && rangedFireOrigin != null // 값 계산 시작
            ? rangedFireOrigin // 참 조건 값
            : attackOrigin; // 공격 방식에 따른 기즈모 시작 위치 결정
        Vector3 direction = viewTransform.forward.normalized; // 편집 화면 Camera 전방 방향 계산
        Vector3 endPosition = previewOrigin.position + direction * previewRange; // 공격 끝 위치 계산
        Gizmos.color = isRangedPreview ? Color.cyan : Color.yellow; // 공격 방식별 기즈모 색상 설정
        Gizmos.DrawLine(previewOrigin.position, endPosition); // 공격 진행 방향 표시

        if (!isRangedPreview) // 근접 공격 기즈모 여부 확인
        {
            Gizmos.DrawWireSphere(endPosition, previewRadius); // 근접 공격 끝 Sphere 범위 표시
        }
    }

    private void OnDisable() // UI, 건축 또는 Scene 전환 시 공격 정리
    {
        CancelCurrentAttack(); // 실행 중인 근접 공격과 연속 공격 진행도 취소
    }

    private void OnValidate() // Inspector 공격 설정값 검증
    {
        maximumAimDistance = Mathf.Max(1f, maximumAimDistance); // 조준 최대 거리 최소값 적용
        maximumHitResults = Mathf.Clamp(maximumHitResults, 4, 64); // 공격 결과 배열 범위 제한
        unarmedDamage = Mathf.Max(0f, unarmedDamage); // 맨손 피해량 음수 방지
        unarmedCooldown = Mathf.Max(0.05f, unarmedCooldown); // 맨손 공격 시간 최소값 적용
        unarmedRange = Mathf.Max(0.1f, unarmedRange); // 맨손 공격 거리 최소값 적용
        unarmedRadius = Mathf.Max(0.01f, unarmedRadius); // 맨손 공격 반지름 최소값 적용
        unarmedStaminaCost = Mathf.Max(0f, unarmedStaminaCost); // 맨손 스태미나 비용 음수 방지
        unarmedImpactForce = Mathf.Max(0f, unarmedImpactForce); // 맨손 충격량 음수 방지

        if (Application.isPlaying) // Play Mode 여부 확인
        {
            ResizeHitResults(); // 실행 중 배열 크기 변경 반영
        }
    }

    private readonly struct AttackProfile // 한 번의 공격에서 사용할 기본 능력치
    {
        public string DisplayName { get; } // 공격 표시 이름
        public WeaponAttackType AttackType { get; } // 공격 방식
        public float Damage { get; } // 기본 피해량
        public float Cooldown { get; } // 기본 공격 재사용 시간
        public float Range { get; } // 기본 공격 거리
        public float Radius { get; } // 기본 공격 반지름
        public float StaminaCost { get; } // 기본 스태미나 비용
        public float ImpactForce { get; } // 기본 충격량
        public MeleeComboData ComboData { get; } // 근접 연속 공격 데이터
        public RangedWeaponData RangedData { get; } // 원거리 무기 데이터

        public AttackProfile( // 메서드 선언
            string displayName, // 매개변수 전달
            WeaponAttackType attackType, // 매개변수 전달
            float damage, // 매개변수 전달
            float cooldown, // 매개변수 전달
            float range, // 매개변수 전달
            float radius, // 매개변수 전달
            float staminaCost, // 매개변수 전달
            float impactForce, // 매개변수 전달
            MeleeComboData comboData, // 매개변수 전달
            RangedWeaponData rangedData) // 기본 공격 능력치 생성
        {
            DisplayName = string.IsNullOrWhiteSpace(displayName) // 값 계산 시작
                ? "ATTACK" // 참 조건 값
                : displayName; // 공격 표시 이름 저장

            AttackType = attackType; // 공격 방식 저장
            Damage = Mathf.Max(0f, damage); // 피해량 음수 방지
            Cooldown = Mathf.Max(0.05f, cooldown); // 기본 공격 시간 최소값 적용
            Range = Mathf.Max(0.1f, range); // 공격 거리 최소값 적용
            Radius = Mathf.Max(0.01f, radius); // 공격 반지름 최소값 적용
            StaminaCost = Mathf.Max(0f, staminaCost); // 스태미나 비용 음수 방지
            ImpactForce = Mathf.Max(0f, impactForce); // 충격량 음수 방지
            ComboData = comboData; // 근접 연속 공격 데이터 저장
            RangedData = rangedData; // 원거리 무기 데이터 저장
        }
    }
}
