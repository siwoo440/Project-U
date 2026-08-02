using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
[RequireComponent(typeof(PlayerInventory))] // 플레이어 인벤토리 요구
[RequireComponent(typeof(PlayerStamina))] // 플레이어 스태미나 요구
public sealed class PlayerBowChargeController : MonoBehaviour // 플레이어 활 장전과 발사 관리자
{
    [Header("References")] // 활 공격 참조 묶음
    [Tooltip("현재 선택 Hotbar 아이템과 탄약을 관리할 플레이어 인벤토리입니다.")] // Inspector 인벤토리 설명
    [SerializeField] private PlayerInventory playerInventory; // 플레이어 인벤토리

    [Tooltip("활 발사 비용을 소비할 플레이어 스태미나입니다.")] // Inspector 스태미나 설명
    [SerializeField] private PlayerStamina playerStamina; // 플레이어 스태미나

    [Tooltip("화살이 생성될 활 또는 손 위치입니다.")] // Inspector 발사 위치 설명
    [SerializeField] private Transform rangedFireOrigin; // 화살 발사 위치

    [Tooltip("화면 중앙 조준 방향을 계산할 Main Camera Transform입니다.")] // Inspector 시선 기준 설명
    [SerializeField] private Transform viewTransform; // 조준 시선 기준

    [Tooltip("활을 당기는 동안 예상 경로를 표시할 관리자입니다.")] // Inspector 예상 궤적 설명
    [SerializeField] private BowTrajectoryPreview trajectoryPreview; // 예상 궤적 관리자

    [Tooltip("활을 당기는 동안 Camera Field Of View를 변경할 관리자입니다.")] // Inspector Camera 확대 설명
    [SerializeField] private BowAimCameraController aimCameraController; // 활 조준 Camera 관리자

    [Header("Bow Visual")] // 활 시각 연출 묶음
    [Tooltip("장력에 따라 뒤로 이동할 활시위 중심 Transform입니다. 비워 두면 위치 연출을 사용하지 않습니다.")] // Inspector 활시위 설명
    [SerializeField] private Transform bowStringPullTransform; // 활시위 당김 Transform

    [Tooltip("최대 장력에서 활시위가 이동할 로컬 위치 보정값입니다.")] // Inspector 활시위 이동량 설명
    [SerializeField] private Vector3 maximumStringPullLocalOffset = new Vector3(0f, 0f, -0.25f); // 최대 활시위 당김 위치

    [Tooltip("장전 중에만 표시할 손에 든 화살 시각 오브젝트입니다. 비워 두면 사용하지 않습니다.")] // Inspector 장전 화살 설명
    [SerializeField] private GameObject nockedArrowVisual; // 장전 중 화살 시각 오브젝트

    [Header("Aim Detection")] // 조준 탐지 설정 묶음
    [Tooltip("화면 중앙 조준점과 예상 궤적 충돌을 확인할 레이어입니다.")] // Inspector 조준 레이어 설명
    [SerializeField] private LayerMask aimBlockingLayers = ~0; // 조준 충돌 레이어

    [Tooltip("Main Camera에서 화면 중앙 조준점을 탐색할 최대 거리입니다.")] // Inspector 조준 거리 설명
    [SerializeField, Min(1f)] private float maximumAimDistance = 100f; // 조준점 최대 탐지 거리

    [Header("Debug")] // 디버그 설정 묶음
    [Tooltip("장전 실패와 발사 결과를 Console에 출력할지 설정합니다.")] // Inspector 로그 설명
    [SerializeField] private bool logBowResults = true; // 활 공격 로그 출력 여부

    [Header("Runtime")] // 실행 상태 확인 묶음
    [Tooltip("현재 활시위를 당기고 있는지 표시합니다.")] // Inspector 장전 상태 설명
    [SerializeField] private bool isCharging; // 현재 활 장전 상태

    [Tooltip("현재 활 장력 비율입니다.")] // Inspector 장력 비율 설명
    [SerializeField, Range(0f, 1f)] private float currentChargeNormalized; // 현재 장력 비율

    [Tooltip("다음 활 발사까지 남은 재사용 대기시간입니다.")] // Inspector 대기시간 설명
    [SerializeField] private float rangedCooldownRemaining; // 현재 원거리 대기시간

    private ItemData activeBowItem; // 현재 장전 중인 활 아이템
    private RangedWeaponData activeRangedData; // 현재 장전 중인 원거리 데이터
    private float chargeStartedAt; // 활 장전 시작 시각
    private float nextFireTime; // 다음 활 발사 가능 시각
    private int attackSequenceCounter; // 원거리 공격 고유 번호 생성기
    private Vector3 initialBowStringLocalPosition; // 활시위 시작 로컬 위치
    private bool hasBowStringInitialPosition; // 활시위 시작 위치 저장 여부

    public bool IsCharging => isCharging; // 현재 활 장전 상태 제공
    public float CurrentChargeNormalized => currentChargeNormalized; // 현재 장력 비율 제공
    public float RangedCooldownRemaining => Mathf.Max(0f, nextFireTime - Time.time); // 남은 발사 대기시간 제공
    public bool IsRangedWeaponSelected => IsValidRangedWeapon(playerInventory == null ? null : playerInventory.SelectedHotbarItem); // 현재 원거리 무기 선택 여부 제공

    private void Awake() // 활 공격 참조 초기화
    {
        if (playerInventory == null) // 인벤토리 참조 확인
        {
            playerInventory = GetComponent<PlayerInventory>(); // 같은 Player에서 자동 검색
        }

        if (playerStamina == null) // 스태미나 참조 확인
        {
            playerStamina = GetComponent<PlayerStamina>(); // 같은 Player에서 자동 검색
        }

        if (viewTransform == null && Camera.main != null) // 시선 기준과 Main Camera 존재 확인
        {
            viewTransform = Camera.main.transform; // Main Camera Transform 자동 연결
        }

        if (aimCameraController == null && Camera.main != null) // Camera 확대 관리자와 Main Camera 존재 확인
        {
            aimCameraController = Camera.main.GetComponent<BowAimCameraController>(); // Main Camera에서 자동 검색
        }

        if (bowStringPullTransform != null) // 활시위 Transform 존재 확인
        {
            initialBowStringLocalPosition = bowStringPullTransform.localPosition; // 활시위 시작 위치 저장
            hasBowStringInitialPosition = true; // 시작 위치 저장 상태 적용
        }

        if (nockedArrowVisual != null) // 장전 화살 시각 오브젝트 존재 확인
        {
            nockedArrowVisual.SetActive(false); // 시작 시 장전 화살 숨김
        }

        bool hasMissingReference = // 값 계산 시작
            playerInventory == null // 조건 시작
            || playerStamina == null // 조건 추가
            || rangedFireOrigin == null // 조건 추가
            || viewTransform == null; // 필수 참조 누락 확인

        if (hasMissingReference) // 필수 참조 누락 여부 확인
        {
            Debug.LogError("PlayerBowChargeController의 Player Inventory, Player Stamina, Ranged Fire Origin, View Transform을 연결해야 합니다.", this); // 참조 누락 오류 출력
            enabled = false; // 활 공격 기능 비활성화
        }
    }

    private void OnEnable() // 인벤토리 선택 변경 구독
    {
        if (playerInventory != null) // 인벤토리 존재 확인
        {
            playerInventory.HotbarSelectionChanged += HandleHotbarSelectionChanged; // Hotbar 선택 변경 구독
        }
    }

    private void OnDisable() // 활 공격 상태와 이벤트 정리
    {
        if (playerInventory != null) // 인벤토리 존재 확인
        {
            playerInventory.HotbarSelectionChanged -= HandleHotbarSelectionChanged; // Hotbar 선택 변경 구독 해제
        }

        CancelCharge(); // 진행 중인 활 장전 취소
    }

    private void Update() // 활 장력과 예상 궤적 갱신
    {
        rangedCooldownRemaining = RangedCooldownRemaining; // Inspector 대기시간 갱신

        if (!isCharging) // 현재 장전 상태 확인
        {
            return; // 장력 갱신 중단
        }

        if (Cursor.lockState != CursorLockMode.Locked || Time.timeScale <= 0f) // Gameplay 입력 가능 상태 확인
        {
            CancelCharge(); // UI 또는 일시정지 진입 시 장전 취소
            return; // 장력 갱신 중단
        }

        if (playerInventory.SelectedHotbarItem != activeBowItem) // 장전 중 활 선택 유지 확인
        {
            CancelCharge(); // 장비 변경 시 장전 취소
            return; // 장력 갱신 중단
        }

        float elapsedTime = Time.time - chargeStartedAt; // 현재까지 활을 당긴 시간 계산
        currentChargeNormalized = activeRangedData.GetChargeNormalized(elapsedTime); // 장전 시간 기준 장력 비율 계산
        ApplyBowVisual(currentChargeNormalized); // 활시위와 장전 화살 시각 갱신

        if (aimCameraController != null) // Camera 확대 관리자 존재 확인
        {
            aimCameraController.SetCharge(currentChargeNormalized); // 현재 장력에 따른 Camera 확대 적용
        }

        RefreshTrajectoryPreview(); // 현재 장력 기준 예상 궤적 갱신
    }

    public bool TryBeginCharge() // 현재 선택 활 장전 시작 시도
    {
        if (!isActiveAndEnabled || isCharging) // 활 공격 활성 상태와 기존 장전 확인
        {
            return false; // 장전 시작 실패 반환
        }

        if (Cursor.lockState != CursorLockMode.Locked || Time.timeScale <= 0f) // Gameplay 입력 가능 상태 확인
        {
            return false; // UI 상태 장전 차단
        }

        ItemData selectedItem = playerInventory.SelectedHotbarItem; // 현재 Hotbar 아이템 조회

        if (!IsValidRangedWeapon(selectedItem)) // 원거리 무기 데이터 확인
        {
            return false; // 원거리 무기 아님 반환
        }

        RangedWeaponData rangedData = selectedItem.RangedWeaponData; // 선택 활의 원거리 데이터 조회

        if (rangedData.ProjectilePrefab == null) // 발사체 프리팹 존재 확인
        {
            LogWarning($"{selectedItem.DisplayName}의 Projectile Prefab이 비어 있습니다."); // 발사체 누락 안내
            return false; // 장전 시작 실패 반환
        }

        if (Time.time < nextFireTime) // 활 발사 재사용 대기시간 확인
        {
            return false; // 대기시간 중 장전 차단
        }

        if (rangedData.RequiresAmmunition // 조건 검사
            && !playerInventory.HasItem(rangedData.AmmunitionItem, rangedData.AmmunitionPerShot)) // 탄약 보유량 확인
        {
            LogMessage($"{selectedItem.DisplayName} 장전에 필요한 탄약이 부족합니다."); // 탄약 부족 안내
            return false; // 장전 시작 실패 반환
        }

        if (!playerStamina.CanConsume(selectedItem.StaminaCost)) // 스태미나 소비 가능 여부 확인
        {
            LogMessage($"스태미나가 부족하여 {selectedItem.DisplayName}을 당길 수 없습니다."); // 스태미나 부족 안내
            return false; // 장전 시작 실패 반환
        }

        activeBowItem = selectedItem; // 현재 장전 활 저장
        activeRangedData = rangedData; // 현재 원거리 데이터 저장
        chargeStartedAt = Time.time; // 장전 시작 시각 저장
        currentChargeNormalized = 0f; // 시작 장력 초기화
        isCharging = true; // 장전 상태 적용
        ApplyBowVisual(0f); // 최소 장력 시각 상태 적용

        if (aimCameraController != null) // Camera 확대 관리자 존재 확인
        {
            aimCameraController.BeginAim( // 호출 시작
                rangedData.MaximumChargeFieldOfView, // 매개변수 전달
                rangedData.CameraZoomSpeed, // 매개변수 전달
                rangedData.CameraReturnSpeed); // 활 조준 Camera 확대 시작
        }

        RefreshTrajectoryPreview(); // 최소 장력 예상 궤적 표시
        return true; // 장전 시작 성공 반환
    }

    public bool ReleaseCharge() // 현재 장력으로 활 발사 시도
    {
        if (!isCharging || activeBowItem == null || activeRangedData == null) // 유효한 장전 상태 확인
        {
            return false; // 발사 실패 반환
        }

        ItemData bowItem = activeBowItem; // 발사에 사용할 활 임시 저장
        RangedWeaponData rangedData = activeRangedData; // 발사에 사용할 원거리 데이터 임시 저장
        float releasedCharge = currentChargeNormalized; // 발사 순간 장력 비율 저장

        if (playerInventory.SelectedHotbarItem != bowItem) // 현재 선택 활 유지 확인
        {
            CancelCharge(); // 장비 변경 상태 정리
            return false; // 발사 실패 반환
        }

        if (rangedData.RequiresAmmunition // 조건 검사
            && !playerInventory.HasItem(rangedData.AmmunitionItem, rangedData.AmmunitionPerShot)) // 발사 순간 탄약 보유량 확인
        {
            LogMessage($"{bowItem.DisplayName} 발사에 필요한 탄약이 부족합니다."); // 탄약 부족 안내
            CancelCharge(); // 장전 상태 정리
            return false; // 발사 실패 반환
        }

        if (!playerStamina.CanConsume(bowItem.StaminaCost)) // 발사 순간 스태미나 확인
        {
            LogMessage($"스태미나가 부족하여 {bowItem.DisplayName}을 발사할 수 없습니다."); // 스태미나 부족 안내
            CancelCharge(); // 장전 상태 정리
            return false; // 발사 실패 반환
        }

        if (rangedData.RequiresAmmunition) // 탄약 소비 필요 여부 확인
        {
            int removedAmmunition = playerInventory.RemoveItem( // 호출 시작
                rangedData.AmmunitionItem, // 매개변수 전달
                rangedData.AmmunitionPerShot); // 인벤토리에서 탄약 제거

            if (removedAmmunition < rangedData.AmmunitionPerShot) // 실제 탄약 제거 결과 확인
            {
                Debug.LogError("탄약 보유 확인 후 실제 탄약 제거에 실패했습니다.", this); // 탄약 처리 오류 출력
                CancelCharge(); // 장전 상태 정리
                return false; // 발사 실패 반환
            }
        }

        if (!playerStamina.TryConsume(bowItem.StaminaCost)) // 스태미나 실제 소비 시도
        {
            Debug.LogError("스태미나 보유 확인 후 실제 스태미나 소비에 실패했습니다.", this); // 스태미나 처리 오류 출력
            CancelCharge(); // 장전 상태 정리
            return false; // 발사 실패 반환
        }

        Vector3 fireDirection = ResolveAttackDirection(rangedFireOrigin.position); // 발사 위치 기준 조준 방향 계산
        Vector3 spawnPosition = rangedFireOrigin.position // 값 계산 시작
            + fireDirection * rangedData.SpawnForwardOffset; // 화살 생성 위치 계산
        float finalDamage = bowItem.BaseDamage * rangedData.EvaluateDamageMultiplier(releasedCharge); // 장력 기반 최종 피해량 계산
        float finalSpeed = rangedData.EvaluateProjectileSpeed(releasedCharge); // 장력 기반 최종 속도 계산
        float finalImpactForce = bowItem.ImpactForce * rangedData.EvaluateImpactMultiplier(releasedCharge); // 장력 기반 최종 충격량 계산
        attackSequenceCounter++; // 원거리 공격 고유 번호 증가
        int attackSequenceId = attackSequenceCounter; // 이번 공격 고유 번호 저장
        CombatProjectile projectile = Instantiate( // 호출 시작
            rangedData.ProjectilePrefab, // 매개변수 전달
            spawnPosition, // 매개변수 전달
            Quaternion.LookRotation(fireDirection, Vector3.up)); // 화살 발사체 생성
        projectile.Initialize( // 호출 시작
            gameObject, // 매개변수 전달
            bowItem, // 매개변수 전달
            finalDamage, // 매개변수 전달
            finalImpactForce, // 매개변수 전달
            fireDirection, // 매개변수 전달
            finalSpeed, // 매개변수 전달
            bowItem.AttackRange, // 매개변수 전달
            rangedData.MaximumLifetime, // 매개변수 전달
            rangedData.UseGravity, // 매개변수 전달
            attackSequenceId); // 최종 장력 정보로 화살 초기화
        nextFireTime = Time.time + bowItem.AttackCooldown; // 다음 발사 가능 시각 저장
        FinishChargeState(); // 장전 시각과 Camera 상태 정리
        LogMessage($"{bowItem.DisplayName} 발사 완료 - 장력 {releasedCharge:P0}, 피해 {finalDamage:0.##}, 속도 {finalSpeed:0.##}"); // 발사 결과 안내
        return true; // 활 발사 성공 반환
    }

    public void CancelCharge() // 현재 활 장전 취소
    {
        if (!isCharging && activeBowItem == null && activeRangedData == null) // 정리할 장전 상태 확인
        {
            ResetBowVisual(); // 남아 있을 수 있는 시각 상태 초기화

            if (trajectoryPreview != null) // 궤적 관리자 존재 확인
            {
                trajectoryPreview.Hide(); // 예상 궤적 숨김
            }

            if (aimCameraController != null && aimCameraController.IsAiming) // Camera 조준 상태 확인
            {
                aimCameraController.EndAim(); // Camera 확대 복귀 시작
            }

            return; // 추가 상태 정리 생략
        }

        FinishChargeState(); // 장전 상태와 연출 정리
    }

    private void FinishChargeState() // 활 장전 공통 종료 처리
    {
        isCharging = false; // 장전 상태 해제
        currentChargeNormalized = 0f; // 장력 비율 초기화
        activeBowItem = null; // 현재 활 참조 제거
        activeRangedData = null; // 현재 원거리 데이터 참조 제거
        chargeStartedAt = 0f; // 장전 시작 시각 초기화
        ResetBowVisual(); // 활시위와 장전 화살 초기화

        if (trajectoryPreview != null) // 예상 궤적 관리자 존재 확인
        {
            trajectoryPreview.Hide(); // 예상 궤적 숨김
        }

        if (aimCameraController != null) // Camera 확대 관리자 존재 확인
        {
            aimCameraController.EndAim(); // 기존 시야각 복귀 시작
        }
    }

    private void RefreshTrajectoryPreview() // 현재 장력 기준 예상 화살 경로 갱신
    {
        if (trajectoryPreview == null || activeRangedData == null) // 궤적 관리자와 원거리 데이터 확인
        {
            return; // 궤적 갱신 중단
        }

        if (!activeRangedData.ShowTrajectoryPreview) // 현재 활의 예상 궤적 표시 설정 확인
        {
            trajectoryPreview.Hide(); // 예상 궤적 숨김
            return; // 궤적 계산 중단
        }

        Vector3 fireDirection = ResolveAttackDirection(rangedFireOrigin.position); // 발사 위치 기준 조준 방향 계산
        Vector3 previewStartPosition = rangedFireOrigin.position // 값 계산 시작
            + fireDirection * activeRangedData.SpawnForwardOffset; // 예상 궤적 시작 위치 계산
        float previewSpeed = activeRangedData.EvaluateProjectileSpeed(currentChargeNormalized); // 현재 장력 발사 속도 계산
        trajectoryPreview.ShowTrajectory( // 호출 시작
            previewStartPosition, // 매개변수 전달
            fireDirection, // 매개변수 전달
            previewSpeed, // 매개변수 전달
            activeRangedData.UseGravity, // 매개변수 전달
            activeBowItem.AttackRange, // 매개변수 전달
            activeRangedData.TrajectoryTimeStep, // 매개변수 전달
            activeRangedData.TrajectoryPointCount, // 매개변수 전달
            activeRangedData.TrajectoryCollisionRadius, // 매개변수 전달
            aimBlockingLayers, // 매개변수 전달
            transform.root); // 플레이어 Collider를 제외한 예상 궤적 표시
    }

    private Vector3 ResolveAttackDirection(Vector3 firePosition) // Camera 중앙 조준점 기준 발사 방향 계산
    {
        Vector3 viewDirection = viewTransform.forward.sqrMagnitude > 0.0001f // 값 계산 시작
            ? viewTransform.forward.normalized // 참 조건 값
            : transform.forward.normalized; // 유효한 Camera 방향 계산
        Vector3 aimPoint = viewTransform.position + viewDirection * maximumAimDistance; // 기본 최대 거리 조준점 계산

        if (Physics.Raycast( // 호출 시작
                viewTransform.position, // 매개변수 전달
                viewDirection, // 매개변수 전달
                out RaycastHit aimHit, // 매개변수 전달
                maximumAimDistance, // 매개변수 전달
                aimBlockingLayers, // 매개변수 전달
                QueryTriggerInteraction.Ignore)) // Camera 중앙 전방 충돌 탐색
        {
            aimPoint = aimHit.point; // 실제 충돌 지점을 조준점으로 적용
        }

        Vector3 directionFromFireOrigin = aimPoint - firePosition; // 발사 위치에서 조준점 방향 계산

        if (directionFromFireOrigin.sqrMagnitude <= 0.0001f) // 유효한 방향 여부 확인
        {
            return viewDirection; // Camera 전방 방향 대체 반환
        }

        return directionFromFireOrigin.normalized; // 발사 위치 보정 방향 반환
    }

    private void ApplyBowVisual(float normalizedCharge) // 장력에 따른 활시위와 화살 시각 적용
    {
        float safeCharge = Mathf.Clamp01(normalizedCharge); // 장력 비율 범위 제한

        if (bowStringPullTransform != null && hasBowStringInitialPosition) // 활시위 Transform과 시작 위치 확인
        {
            bowStringPullTransform.localPosition = initialBowStringLocalPosition // 값 계산 시작
                + maximumStringPullLocalOffset * safeCharge; // 장력에 따른 활시위 위치 적용
        }

        if (nockedArrowVisual != null && !nockedArrowVisual.activeSelf) // 장전 화살 표시 상태 확인
        {
            nockedArrowVisual.SetActive(true); // 장전 화살 표시
        }
    }

    private void ResetBowVisual() // 활시위와 장전 화살 시각 초기화
    {
        if (bowStringPullTransform != null && hasBowStringInitialPosition) // 활시위 Transform과 시작 위치 확인
        {
            bowStringPullTransform.localPosition = initialBowStringLocalPosition; // 활시위 시작 위치 복원
        }

        if (nockedArrowVisual != null && nockedArrowVisual.activeSelf) // 장전 화살 표시 상태 확인
        {
            nockedArrowVisual.SetActive(false); // 장전 화살 숨김
        }
    }

    private bool IsValidRangedWeapon(ItemData itemData) // 원거리 공격 가능한 아이템 여부 계산
    {
        return itemData != null // 아이템 존재 확인
            && itemData.CanAttack // 공격 가능 여부 확인
            && itemData.WeaponAttackType == WeaponAttackType.Ranged // 원거리 공격 방식 확인
            && itemData.RangedWeaponData != null; // 원거리 데이터 연결 확인
    }

    private void HandleHotbarSelectionChanged() // Hotbar 선택 변경 처리
    {
        if (isCharging) // 현재 활 장전 상태 확인
        {
            CancelCharge(); // 장비 변경 시 활 장전 취소
        }
    }

    private void LogMessage(string message) // 설정에 따른 일반 로그 출력
    {
        if (logBowResults) // 활 로그 사용 여부 확인
        {
            Debug.Log(message, this); // 일반 로그 출력
        }
    }

    private void LogWarning(string message) // 설정에 따른 경고 로그 출력
    {
        if (logBowResults) // 활 로그 사용 여부 확인
        {
            Debug.LogWarning(message, this); // 경고 로그 출력
        }
    }

    private void OnValidate() // Inspector 설정값 검증
    {
        maximumAimDistance = Mathf.Max(1f, maximumAimDistance); // 조준 거리 최소값 적용
    }
}
