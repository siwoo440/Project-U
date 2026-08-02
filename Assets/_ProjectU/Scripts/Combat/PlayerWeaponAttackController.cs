using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
[RequireComponent(typeof(PlayerInventory))] // 플레이어 인벤토리 요구
[RequireComponent(typeof(PlayerStamina))] // 플레이어 스태미나 요구
public sealed class PlayerWeaponAttackController : MonoBehaviour // 플레이어 공통 무기 공격 관리자
{
    [Header("References")] // 공격 참조 묶음
    [Tooltip("현재 선택 Hotbar 아이템을 확인할 플레이어 인벤토리입니다.")]
    [SerializeField] private PlayerInventory playerInventory; // 플레이어 인벤토리

    [Tooltip("공격 비용을 소비할 플레이어 스태미나입니다.")]
    [SerializeField] private PlayerStamina playerStamina; // 플레이어 스태미나

    [Tooltip("근접 공격 SphereCast가 시작될 플레이어 기준 위치입니다.")]
    [SerializeField] private Transform attackOrigin; // 공격 시작 위치

    [Tooltip("화면 중앙이 바라보는 방향을 계산할 Main Camera Transform입니다.")]
    [SerializeField] private Transform viewTransform; // 공격 시선 기준

    [Tooltip("공격 시작 시 실행할 도구 휘두르기 연출입니다.")]
    [SerializeField] private ToolSwingAnimation toolSwingAnimation; // 공격 휘두르기 연출

    [Header("Detection")] // 공격 탐지 설정 묶음
    [Tooltip("TrainingDamageTarget과 이후 EnemyHealth가 사용할 전투 대상 레이어입니다.")]
    [SerializeField] private LayerMask damageableLayers; // 전투 피해 대상 레이어

    [Tooltip("3인칭 화면 중앙 조준점을 계산할 때 충돌을 확인할 레이어입니다.")]
    [SerializeField] private LayerMask aimBlockingLayers = ~0; // 조준점 탐지 레이어

    [Tooltip("Main Camera에서 화면 중앙 조준점을 탐색할 최대 거리입니다.")]
    [SerializeField, Min(1f)] private float maximumAimDistance = 100f; // 조준점 최대 탐지 거리

    [Tooltip("한 번의 근접 공격에서 확인할 최대 Collider 결과 수입니다.")]
    [SerializeField, Range(4, 64)] private int maximumHitResults = 32; // 공격 탐지 결과 배열 크기

    [Header("Unarmed")] // 맨손 공격 설정 묶음
    [Tooltip("공격 가능한 아이템을 선택하지 않았을 때 맨손 공격을 허용할지 설정합니다.")]
    [SerializeField] private bool allowUnarmedAttack = true; // 맨손 공격 허용 여부

    [Tooltip("맨손 공격의 기본 피해량입니다.")]
    [SerializeField, Min(0f)] private float unarmedDamage = 2f; // 맨손 피해량

    [Tooltip("맨손 공격 사이의 최소 간격입니다.")]
    [SerializeField, Min(0.05f)] private float unarmedCooldown = 0.65f; // 맨손 공격 간격

    [Tooltip("맨손 공격 SphereCast 거리입니다.")]
    [SerializeField, Min(0.1f)] private float unarmedRange = 1.4f; // 맨손 공격 거리

    [Tooltip("맨손 공격 SphereCast 반지름입니다.")]
    [SerializeField, Min(0.01f)] private float unarmedRadius = 0.25f; // 맨손 공격 반지름

    [Tooltip("맨손 공격에 사용할 스태미나입니다.")]
    [SerializeField, Min(0f)] private float unarmedStaminaCost; // 맨손 스태미나 비용

    [Tooltip("맨손 공격의 향후 넉백 계산용 충격량입니다.")]
    [SerializeField, Min(0f)] private float unarmedImpactForce = 1f; // 맨손 충격량

    [Header("Debug")] // 공격 확인 설정 묶음
    [Tooltip("공격 실패와 명중 결과를 Console에 출력할지 설정합니다.")]
    [SerializeField] private bool logAttackResults = true; // 공격 로그 출력 여부

    private RaycastHit[] hitResults = new RaycastHit[32]; // 근접 공격 탐지 결과 배열
    private float nextAttackAllowedTime; // 다음 공격 허용 시각

    public float RemainingCooldown => Mathf.Max(0f, nextAttackAllowedTime - Time.time); // 남은 공격 대기 시간 제공
    public bool IsOnCooldown => Time.time < nextAttackAllowedTime; // 현재 공격 대기 상태 제공

    private void Awake() // 공통 공격 참조 초기화
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

        ResizeHitResults(); // Inspector 결과 수에 맞는 배열 생성

        bool hasMissingReference =
            playerInventory == null
            || playerStamina == null
            || attackOrigin == null
            || viewTransform == null; // 필수 참조 누락 확인

        if (hasMissingReference) // 필수 참조 누락 여부 확인
        {
            Debug.LogError(
                "PlayerWeaponAttackController의 Player Inventory, Player Stamina, Attack Origin, View Transform을 연결해야 합니다.",
                this); // 공격 참조 오류 출력

            enabled = false; // 공통 공격 기능 비활성화
            return; // 초기화 중단
        }

        if (damageableLayers.value == 0) // 피해 대상 레이어 설정 확인
        {
            Debug.LogWarning(
                "PlayerWeaponAttackController의 Damageable Layers가 비어 있어 전투 대상에게 피해를 줄 수 없습니다.",
                this); // 피해 레이어 누락 경고 출력
        }
    }

    public bool TryAttack(GatherableResource gatherableTarget) // 선택 아이템으로 공격 또는 채집 시도
    {
        if (!isActiveAndEnabled) // 공통 공격 기능 활성 상태 확인
        {
            return false; // 공격 시작 실패 반환
        }

        if (IsOnCooldown) // 공격 재사용 대기시간 확인
        {
            return false; // 빠른 연속 공격 차단
        }

        ItemData selectedItem = playerInventory.SelectedHotbarItem; // 현재 Hotbar 아이템 조회
        AttackProfile attackProfile; // 이번 공격에 사용할 능력치

        if (!TryResolveAttackProfile(selectedItem, out attackProfile)) // 아이템 또는 맨손 공격 능력치 계산
        {
            if (logAttackResults) // 공격 실패 로그 사용 여부 확인
            {
                Debug.Log("현재 선택 아이템은 공격 기능이 없습니다.", this); // 공격 불가능 아이템 안내
            }

            return false; // 공격 시작 실패 반환
        }

        if (!playerStamina.TryConsume(attackProfile.StaminaCost)) // 공격 스태미나 소비 시도
        {
            if (logAttackResults) // 스태미나 실패 로그 사용 여부 확인
            {
                Debug.Log(
                    $"스태미나가 부족하여 {attackProfile.DisplayName} 공격을 실행할 수 없습니다.",
                    this); // 스태미나 부족 안내
            }

            return false; // 공격 시작 실패 반환
        }

        nextAttackAllowedTime = Time.time + attackProfile.Cooldown; // 다음 공격 허용 시각 저장
        PlayAttackAnimation(); // 공통 휘두르기 연출 실행

        if (gatherableTarget != null) // 현재 시선의 채집 자원 확인
        {
            gatherableTarget.Interact(gameObject); // 기존 자원 채집 규칙 실행
            return true; // 공격 입력 처리 성공 반환
        }

        if (attackProfile.AttackType == WeaponAttackType.Melee) // 근접 공격 방식 확인
        {
            PerformMeleeAttack(selectedItem, attackProfile); // 가장 가까운 전투 대상 공격
            return true; // 근접 공격 입력 처리 성공 반환
        }

        if (attackProfile.AttackType == WeaponAttackType.Ranged && logAttackResults) // 원거리 공격 예약 상태 확인
        {
            Debug.Log(
                $"{attackProfile.DisplayName}은 원거리 공격 데이터입니다. 발사체 생성은 62일차에서 연결합니다.",
                this); // 원거리 공격 미구현 안내
        }

        return true; // 공격 입력과 비용 처리 성공 반환
    }

    private bool TryResolveAttackProfile(
        ItemData selectedItem,
        out AttackProfile attackProfile) // 선택 아이템 또는 맨손 공격 능력치 계산
    {
        if (selectedItem != null && selectedItem.CanAttack) // 공격 가능한 아이템 확인
        {
            attackProfile = new AttackProfile(
                selectedItem.DisplayName,
                selectedItem.WeaponAttackType,
                selectedItem.BaseDamage,
                selectedItem.AttackCooldown,
                selectedItem.AttackRange,
                selectedItem.AttackRadius,
                selectedItem.StaminaCost,
                selectedItem.ImpactForce); // ItemData 전투 능력치 복사

            return true; // 아이템 공격 능력치 계산 성공
        }

        if (!allowUnarmedAttack) // 맨손 공격 허용 여부 확인
        {
            attackProfile = default; // 빈 공격 능력치 반환
            return false; // 공격 능력치 계산 실패
        }

        attackProfile = new AttackProfile(
            "UNARMED",
            WeaponAttackType.Melee,
            unarmedDamage,
            unarmedCooldown,
            unarmedRange,
            unarmedRadius,
            unarmedStaminaCost,
            unarmedImpactForce); // 맨손 공격 능력치 생성

        return true; // 맨손 공격 능력치 계산 성공
    }

    private void PerformMeleeAttack(
        ItemData selectedItem,
        AttackProfile attackProfile) // 근접 공격 판정 실행
    {
        if (damageableLayers.value == 0) // 피해 대상 레이어 설정 확인
        {
            return; // 피해 판정 생략
        }

        Vector3 attackDirection = ResolveAttackDirection(); // 1·3인칭 공통 공격 방향 계산

        int hitCount = Physics.SphereCastNonAlloc(
            attackOrigin.position,
            attackProfile.Radius,
            attackDirection,
            hitResults,
            attackProfile.Range,
            damageableLayers,
            QueryTriggerInteraction.Ignore); // 공격 범위 안의 Collider 탐지

        ICombatDamageReceiver nearestReceiver = null; // 가장 가까운 피해 수신 대상
        Component nearestReceiverComponent = null; // 가장 가까운 피해 수신 컴포넌트
        Collider nearestCollider = null; // 가장 가까운 충돌 Collider
        Vector3 nearestHitPoint = Vector3.zero; // 가장 가까운 충돌 지점
        float nearestDistance = float.MaxValue; // 가장 가까운 충돌 거리

        for (int index = 0; index < hitCount; index++) // 전체 공격 탐지 결과 순회
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

            ICombatDamageReceiver candidate =
                currentHit.collider.GetComponentInParent<ICombatDamageReceiver>(); // 부모에서 피해 수신 대상 검색

            Component candidateComponent = candidate as Component; // Unity Component 참조 변환

            if (candidate == null
                || candidateComponent == null
                || !candidate.IsAlive) // 유효하고 생존한 피해 대상 확인
            {
                continue; // 피해 불가능 대상 제외
            }

            Transform damageRoot = candidate.DamageRoot == null
                ? candidateComponent.transform
                : candidate.DamageRoot; // 중복 Collider 기준 Transform 결정

            if (damageRoot == transform.root || damageRoot.IsChildOf(transform.root)) // Player 계층 대상 확인
            {
                continue; // 자기 자신 피해 제외
            }

            if (currentHit.distance >= nearestDistance) // 기존 대상보다 가까운지 확인
            {
                continue; // 더 먼 대상 제외
            }

            nearestDistance = currentHit.distance; // 가장 가까운 거리 갱신
            nearestReceiver = candidate; // 가장 가까운 피해 대상 저장
            nearestReceiverComponent = candidateComponent; // 가장 가까운 Component 저장
            nearestCollider = currentHit.collider; // 가장 가까운 Collider 저장
            nearestHitPoint = currentHit.point == Vector3.zero
                ? currentHit.collider.ClosestPoint(attackOrigin.position)
                : currentHit.point; // 실제 충돌 지점 계산
        }

        if (nearestReceiver == null || nearestReceiverComponent == null) // 명중 대상 존재 확인
        {
            if (logAttackResults) // 빗나감 로그 사용 여부 확인
            {
                Debug.Log($"{attackProfile.DisplayName} 공격이 전투 대상에 명중하지 않았습니다.", this); // 빗나감 결과 출력
            }

            return; // 피해 처리 종료
        }

        CombatHitData hitData = new CombatHitData(
            gameObject,
            selectedItem,
            attackProfile.AttackType,
            attackProfile.Damage,
            attackProfile.ImpactForce,
            nearestHitPoint,
            attackDirection,
            nearestCollider); // 공통 피해 정보 생성

        bool damageApplied = nearestReceiver.ReceiveDamage(hitData); // 가장 가까운 대상에게 피해 전달

        if (logAttackResults && damageApplied) // 피해 적용과 로그 사용 여부 확인
        {
            Debug.Log(
                $"{attackProfile.DisplayName} 공격 명중: {nearestReceiverComponent.gameObject.name} / 피해 {attackProfile.Damage:0.##}",
                this); // 공격 명중 결과 출력
        }
    }

    private Vector3 ResolveAttackDirection() // 1·3인칭 화면 중앙 기준 공격 방향 계산
    {
        Vector3 fallbackDirection = viewTransform.forward.normalized; // 기본 Camera 전방 방향 계산

        if (aimBlockingLayers.value == 0) // 조준 차단 레이어 설정 확인
        {
            return fallbackDirection; // Camera 전방 방향 반환
        }

        bool hasAimHit = Physics.Raycast(
            viewTransform.position,
            fallbackDirection,
            out RaycastHit aimHit,
            maximumAimDistance,
            aimBlockingLayers,
            QueryTriggerInteraction.Ignore); // Camera 화면 중앙의 월드 조준점 탐색

        if (!hasAimHit) // 조준점 미탐지 확인
        {
            return fallbackDirection; // Camera 전방 방향 반환
        }

        Vector3 originToAimPoint = aimHit.point - attackOrigin.position; // Player 공격 위치에서 조준점까지 방향 계산

        if (originToAimPoint.sqrMagnitude <= 0.0001f) // 유효한 방향 길이 확인
        {
            return fallbackDirection; // Camera 전방 방향 반환
        }

        return originToAimPoint.normalized; // 3인칭 Camera 보정 공격 방향 반환
    }

    private void PlayAttackAnimation() // 공통 공격 휘두르기 연출 실행
    {
        if (toolSwingAnimation == null) // 휘두르기 연출 존재 확인
        {
            return; // 연출 처리 생략
        }

        toolSwingAnimation.PlaySwing(); // 기존 도구 휘두르기 실행
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

    private void OnDrawGizmosSelected() // 근접 공격 범위 시각화
    {
        if (attackOrigin == null || viewTransform == null) // 공격 기준 참조 확인
        {
            return; // 기즈모 표시 중단
        }

        float previewRange = unarmedRange; // 기본 기즈모 공격 거리
        float previewRadius = unarmedRadius; // 기본 기즈모 공격 반지름

        if (playerInventory != null
            && playerInventory.SelectedHotbarItem != null
            && playerInventory.SelectedHotbarItem.CanAttack) // 현재 선택 무기 데이터 확인
        {
            previewRange = playerInventory.SelectedHotbarItem.AttackRange; // 선택 무기 거리 적용
            previewRadius = playerInventory.SelectedHotbarItem.AttackRadius; // 선택 무기 반지름 적용
        }

        Vector3 direction = viewTransform.forward.normalized; // 편집 화면 Camera 전방 방향 계산
        Vector3 endPosition = attackOrigin.position + direction * previewRange; // 공격 끝 위치 계산
        Gizmos.color = Color.yellow; // 공격 기즈모 색상 설정
        Gizmos.DrawLine(attackOrigin.position, endPosition); // 공격 진행 방향 표시
        Gizmos.DrawWireSphere(endPosition, previewRadius); // 공격 끝 Sphere 범위 표시
    }

    private void OnValidate() // Inspector 공격 설정값 검증
    {
        maximumAimDistance = Mathf.Max(1f, maximumAimDistance); // 조준 최대 거리 최소값 적용
        maximumHitResults = Mathf.Clamp(maximumHitResults, 4, 64); // 공격 결과 배열 범위 제한
        unarmedDamage = Mathf.Max(0f, unarmedDamage); // 맨손 피해량 음수 방지
        unarmedCooldown = Mathf.Max(0.05f, unarmedCooldown); // 맨손 공격 간격 최소값 적용
        unarmedRange = Mathf.Max(0.1f, unarmedRange); // 맨손 공격 거리 최소값 적용
        unarmedRadius = Mathf.Max(0.01f, unarmedRadius); // 맨손 공격 반지름 최소값 적용
        unarmedStaminaCost = Mathf.Max(0f, unarmedStaminaCost); // 맨손 스태미나 비용 음수 방지
        unarmedImpactForce = Mathf.Max(0f, unarmedImpactForce); // 맨손 충격량 음수 방지

        if (Application.isPlaying) // Play Mode 여부 확인
        {
            ResizeHitResults(); // 실행 중 배열 크기 변경 반영
        }
    }

    private readonly struct AttackProfile // 한 번의 공격에 사용할 계산 완료 능력치
    {
        public string DisplayName { get; } // 공격 표시 이름
        public WeaponAttackType AttackType { get; } // 공격 방식
        public float Damage { get; } // 피해량
        public float Cooldown { get; } // 공격 대기시간
        public float Range { get; } // 공격 거리
        public float Radius { get; } // 공격 반지름
        public float StaminaCost { get; } // 스태미나 비용
        public float ImpactForce { get; } // 충격량

        public AttackProfile(
            string displayName,
            WeaponAttackType attackType,
            float damage,
            float cooldown,
            float range,
            float radius,
            float staminaCost,
            float impactForce) // 공격 능력치 생성
        {
            DisplayName = string.IsNullOrWhiteSpace(displayName)
                ? "ATTACK"
                : displayName; // 공격 표시 이름 저장

            AttackType = attackType; // 공격 방식 저장
            Damage = Mathf.Max(0f, damage); // 피해량 음수 방지
            Cooldown = Mathf.Max(0.05f, cooldown); // 공격 대기시간 최소값 적용
            Range = Mathf.Max(0.1f, range); // 공격 거리 최소값 적용
            Radius = Mathf.Max(0.01f, radius); // 공격 반지름 최소값 적용
            StaminaCost = Mathf.Max(0f, staminaCost); // 스태미나 비용 음수 방지
            ImpactForce = Mathf.Max(0f, impactForce); // 충격량 음수 방지
        }
    }
}
