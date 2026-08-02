using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
[RequireComponent(typeof(Collider))] // 충돌 판정용 Collider 요구
[RequireComponent(typeof(Rigidbody))] // 이동 판정용 Rigidbody 요구
public sealed class CombatProjectile : MonoBehaviour // 공통 원거리 전투 발사체
{
    [Header("References")] // 발사체 참조 묶음
    [Tooltip("발사체 이동에 사용할 Rigidbody입니다.")] // Inspector Rigidbody 설명
    [SerializeField] private Rigidbody projectileRigidbody; // 발사체 Rigidbody

    [Header("Visual")] // 발사체 시각 설정 묶음
    [Tooltip("비행 중 현재 속도 방향을 바라보도록 회전할지 설정합니다.")] // Inspector 방향 회전 설명
    [SerializeField] private bool rotateAlongVelocity = true; // 이동 방향 회전 여부

    [Tooltip("이동 방향 회전에 사용할 초당 회전 속도입니다. 0이면 즉시 회전합니다.")] // Inspector 회전 속도 설명
    [SerializeField, Min(0f)] private float rotationSpeed = 720f; // 이동 방향 회전 속도

    [Header("Runtime")] // 실행 상태 확인 묶음
    [Tooltip("발사체 초기화 완료 여부입니다.")] // Inspector 초기화 상태 설명
    [SerializeField] private bool isInitialized; // 초기화 완료 여부

    [Tooltip("발사체 충돌 처리 완료 여부입니다.")] // Inspector 충돌 상태 설명
    [SerializeField] private bool hasImpacted; // 충돌 처리 완료 여부

    [Tooltip("발사체가 이동한 현재 거리입니다.")] // Inspector 이동 거리 설명
    [SerializeField] private float travelledDistance; // 현재 이동 거리

    private GameObject attacker; // 공격 주체
    private ItemData sourceItem; // 사용 무기 아이템
    private float damage; // 적용 피해량
    private float impactForce; // 적용 충격량
    private float maximumRange; // 최대 이동 거리
    private Vector3 previousPosition; // 이전 프레임 위치
    private Vector3 initialDirection; // 최초 발사 방향
    private int attackSequenceId; // 공격 고유 번호

    private void Awake() // 발사체 참조 초기화
    {
        if (projectileRigidbody == null) // Rigidbody 참조 확인
        {
            projectileRigidbody = GetComponent<Rigidbody>(); // 같은 오브젝트에서 자동 검색
        }

        if (projectileRigidbody == null) // Rigidbody 검색 결과 확인
        {
            Debug.LogError("CombatProjectile에 Rigidbody가 필요합니다.", this); // Rigidbody 누락 오류 출력
            enabled = false; // 발사체 기능 비활성화
            return; // 초기화 중단
        }

        projectileRigidbody.isKinematic = false; // 물리 이동 활성화
        projectileRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // 고속 충돌 누락 방지
        projectileRigidbody.interpolation = RigidbodyInterpolation.Interpolate; // 화면 이동 보간 적용
    }

    public void Initialize( // 메서드 선언
        GameObject attackOwner, // 매개변수 전달
        ItemData attackItem, // 매개변수 전달
        float attackDamage, // 매개변수 전달
        float attackImpactForce, // 매개변수 전달
        Vector3 launchDirection, // 매개변수 전달
        float launchSpeed, // 매개변수 전달
        float range, // 매개변수 전달
        float lifetime, // 매개변수 전달
        bool useGravity, // 매개변수 전달
        int sequenceId) // 발사체 공격 정보와 이동값 초기화
    {
        if (projectileRigidbody == null) // Rigidbody 참조 확인
        {
            Debug.LogError("CombatProjectile 초기화 전에 Rigidbody를 찾을 수 없습니다.", this); // 초기화 실패 오류 출력
            Destroy(gameObject); // 잘못된 발사체 제거
            return; // 초기화 중단
        }

        attacker = attackOwner; // 공격 주체 저장
        sourceItem = attackItem; // 사용 아이템 저장
        damage = Mathf.Max(0f, attackDamage); // 피해량 음수 방지
        impactForce = Mathf.Max(0f, attackImpactForce); // 충격량 음수 방지
        maximumRange = Mathf.Max(0.1f, range); // 최대 이동 거리 최소값 적용
        attackSequenceId = Mathf.Max(0, sequenceId); // 공격 고유 번호 음수 방지
        initialDirection = launchDirection.sqrMagnitude <= 0.0001f // 값 계산 시작
            ? transform.forward.normalized // 참 조건 값
            : launchDirection.normalized; // 유효한 발사 방향 계산
        previousPosition = transform.position; // 시작 위치 저장
        travelledDistance = 0f; // 이동 거리 초기화
        hasImpacted = false; // 충돌 처리 상태 초기화
        isInitialized = true; // 초기화 완료 상태 적용
        transform.rotation = Quaternion.LookRotation(initialDirection, Vector3.up); // 발사 방향 회전 적용
        projectileRigidbody.useGravity = useGravity; // Rigidbody 중력 설정 적용
        projectileRigidbody.maxLinearVelocity = Mathf.Max(projectileRigidbody.maxLinearVelocity, Mathf.Max(0.1f, launchSpeed)); // 발사 속도 제한 상향
        projectileRigidbody.linearVelocity = initialDirection * Mathf.Max(0.1f, launchSpeed); // 초기 발사 속도 적용
        IgnoreAttackerCollisions(); // 공격자와 발사체 충돌 제외
        Destroy(gameObject, Mathf.Max(0.1f, lifetime)); // 최대 수명 이후 자동 제거 예약
    }

    private void Update() // 발사체 이동 거리 확인
    {
        if (!isInitialized || hasImpacted) // 유효한 비행 상태 확인
        {
            return; // 이동 거리 계산 생략
        }

        Vector3 currentPosition = transform.position; // 현재 위치 조회
        travelledDistance += Vector3.Distance(previousPosition, currentPosition); // 프레임 이동 거리 누적
        previousPosition = currentPosition; // 다음 계산용 현재 위치 저장

        if (travelledDistance < maximumRange) // 최대 이동 거리 도달 여부 확인
        {
            return; // 비행 유지
        }

        hasImpacted = true; // 거리 만료 상태 적용
        Destroy(gameObject); // 최대 거리 도달 발사체 제거
    }

    private void FixedUpdate() // 물리 이동 방향에 따른 화살 회전 처리
    {
        if (!isInitialized || hasImpacted || !rotateAlongVelocity || projectileRigidbody == null) // 회전 가능 상태 확인
        {
            return; // 회전 처리 중단
        }

        Vector3 currentVelocity = projectileRigidbody.linearVelocity; // 현재 발사체 속도 조회

        if (currentVelocity.sqrMagnitude <= 0.0001f) // 유효한 이동 속도 확인
        {
            return; // 정지 상태 회전 생략
        }

        Quaternion targetRotation = Quaternion.LookRotation(currentVelocity.normalized, Vector3.up); // 이동 방향 목표 회전 계산

        if (rotationSpeed <= 0f) // 즉시 회전 설정 확인
        {
            transform.rotation = targetRotation; // 이동 방향 회전 즉시 적용
            return; // 보간 회전 생략
        }

        transform.rotation = Quaternion.RotateTowards( // 호출 시작
            transform.rotation, // 매개변수 전달
            targetRotation, // 매개변수 전달
            rotationSpeed * Time.fixedDeltaTime); // 이동 방향으로 부드럽게 회전
    }

    private void OnTriggerEnter(Collider other) // Trigger 충돌 처리
    {
        if (other == null) // 충돌 Collider 존재 확인
        {
            return; // 잘못된 충돌 무시
        }

        ICombatDamageReceiver receiver = other.GetComponentInParent<ICombatDamageReceiver>(); // 피해 수신 대상 검색

        if (other.isTrigger && receiver == null) // 일반 환경 Trigger 여부 확인
        {
            return; // 감지 구역 Trigger 충돌 무시
        }

        Vector3 hitPoint = other.ClosestPoint(transform.position); // Trigger 충돌 지점 계산
        ProcessImpact(other, hitPoint, receiver); // 공통 충돌 처리 실행
    }

    private void OnCollisionEnter(Collision collision) // 일반 Collider 충돌 처리
    {
        if (collision == null || collision.collider == null) // 충돌 정보 유효성 확인
        {
            return; // 잘못된 충돌 무시
        }

        Vector3 hitPoint = collision.contactCount > 0 // 값 계산 시작
            ? collision.GetContact(0).point // 참 조건 값
            : collision.collider.ClosestPoint(transform.position); // 실제 충돌 지점 계산
        ICombatDamageReceiver receiver = collision.collider.GetComponentInParent<ICombatDamageReceiver>(); // 피해 수신 대상 검색
        ProcessImpact(collision.collider, hitPoint, receiver); // 공통 충돌 처리 실행
    }

    private void ProcessImpact( // 메서드 선언
        Collider hitCollider, // 매개변수 전달
        Vector3 hitPoint, // 매개변수 전달
        ICombatDamageReceiver receiver) // 발사체 충돌과 피해 처리
    {
        if (!isInitialized || hasImpacted || hitCollider == null) // 충돌 처리 가능 상태 확인
        {
            return; // 중복 또는 잘못된 충돌 차단
        }

        if (IsAttackerCollider(hitCollider)) // 공격자 자신의 Collider 확인
        {
            return; // 자기 자신 충돌 제외
        }

        hasImpacted = true; // 최초 충돌 처리 완료 저장
        Component receiverComponent = receiver as Component; // 피해 대상 Component 변환

        if (receiver != null && receiverComponent != null && receiver.IsAlive) // 생존한 피해 대상 확인
        {
            Transform damageRoot = receiver.DamageRoot == null // 값 계산 시작
                ? receiverComponent.transform // 참 조건 값
                : receiver.DamageRoot; // 피해 대상 기준 Transform 계산

            if (!IsAttackerTransform(damageRoot)) // 공격자 자신의 피해 대상 여부 확인
            {
                Vector3 hitDirection = ResolveCurrentDirection(); // 충돌 순간 공격 방향 계산
                CombatHitData hitData = new CombatHitData( // 호출 시작
                    attacker, // 매개변수 전달
                    sourceItem, // 매개변수 전달
                    WeaponAttackType.Ranged, // 매개변수 전달
                    damage, // 매개변수 전달
                    impactForce, // 매개변수 전달
                    hitPoint, // 매개변수 전달
                    hitDirection, // 매개변수 전달
                    hitCollider, // 매개변수 전달
                    attackSequenceId, // 매개변수 전달
                    0); // 원거리 공통 피해 정보 생성
                receiver.ReceiveDamage(hitData); // 대상에게 원거리 피해 전달
            }
        }

        projectileRigidbody.linearVelocity = Vector3.zero; // 충돌 후 이동 속도 제거
        projectileRigidbody.angularVelocity = Vector3.zero; // 충돌 후 회전 속도 제거
        projectileRigidbody.detectCollisions = false; // 추가 물리 충돌 차단
        Destroy(gameObject); // 충돌 완료 발사체 제거
    }

    private Vector3 ResolveCurrentDirection() // 충돌 순간 발사체 이동 방향 계산
    {
        Vector3 currentVelocity = projectileRigidbody == null // 값 계산 시작
            ? Vector3.zero // 참 조건 값
            : projectileRigidbody.linearVelocity; // 현재 Rigidbody 속도 조회

        if (currentVelocity.sqrMagnitude > 0.0001f) // 유효한 현재 속도 확인
        {
            return currentVelocity.normalized; // 현재 비행 방향 반환
        }

        return initialDirection.sqrMagnitude > 0.0001f // 값 계산 시작
            ? initialDirection.normalized // 참 조건 값
            : transform.forward.normalized; // 초기 방향 또는 전방 방향 반환
    }

    private void IgnoreAttackerCollisions() // 공격자와 발사체 Collider 충돌 제외
    {
        if (attacker == null) // 공격 주체 존재 확인
        {
            return; // 충돌 제외 처리 생략
        }

        Collider[] projectileColliders = GetComponentsInChildren<Collider>(true); // 발사체 전체 Collider 조회
        Collider[] attackerColliders = attacker.transform.root.GetComponentsInChildren<Collider>(true); // 공격자 전체 Collider 조회

        for (int projectileIndex = 0; projectileIndex < projectileColliders.Length; projectileIndex++) // 발사체 Collider 순회
        {
            Collider projectileCollider = projectileColliders[projectileIndex]; // 현재 발사체 Collider 조회

            if (projectileCollider == null) // 발사체 Collider 유효성 확인
            {
                continue; // 잘못된 Collider 제외
            }

            for (int attackerIndex = 0; attackerIndex < attackerColliders.Length; attackerIndex++) // 공격자 Collider 순회
            {
                Collider attackerCollider = attackerColliders[attackerIndex]; // 현재 공격자 Collider 조회

                if (attackerCollider == null) // 공격자 Collider 유효성 확인
                {
                    continue; // 잘못된 Collider 제외
                }

                Physics.IgnoreCollision(projectileCollider, attackerCollider, true); // 두 Collider 충돌 무시 적용
            }
        }
    }

    private bool IsAttackerCollider(Collider targetCollider) // 공격자 Collider 여부 계산
    {
        if (attacker == null || targetCollider == null) // 공격자와 Collider 존재 확인
        {
            return false; // 공격자 Collider 아님 반환
        }

        return IsAttackerTransform(targetCollider.transform); // Transform 기준 공격자 여부 반환
    }

    private bool IsAttackerTransform(Transform targetTransform) // 공격자 Transform 여부 계산
    {
        if (attacker == null || targetTransform == null) // 공격자와 Transform 존재 확인
        {
            return false; // 공격자 Transform 아님 반환
        }

        Transform attackerRoot = attacker.transform.root; // 공격자 최상위 Transform 조회
        return targetTransform == attackerRoot || targetTransform.IsChildOf(attackerRoot); // 공격자 계층 포함 여부 반환
    }

    private void OnValidate() // Inspector 발사체 설정값 검증
    {
        rotationSpeed = Mathf.Max(0f, rotationSpeed); // 회전 속도 음수 방지
    }
}
