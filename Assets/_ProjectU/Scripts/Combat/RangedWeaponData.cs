using UnityEngine; // Unity 기본 기능
using UnityEngine.Serialization; // 이전 직렬화 이름 유지 기능

[CreateAssetMenu(fileName = "RangedWeaponData_New", menuName = "Project U/Combat/Ranged Weapon Data")] // 원거리 무기 데이터 생성 메뉴
public sealed class RangedWeaponData : ScriptableObject // 원거리 무기 전용 설정 데이터
{
    [Header("Ammunition")] // 탄약 설정 묶음
    [Tooltip("한 번 발사할 때 소비할 탄약 아이템입니다. 비워 두면 탄약을 소비하지 않습니다.")] // Inspector 탄약 설명
    [SerializeField] private ItemData ammunitionItem; // 소비 탄약 아이템

    [Tooltip("한 번 발사할 때 소비할 탄약 수량입니다.")] // Inspector 탄약 수량 설명
    [SerializeField, Min(1)] private int ammunitionPerShot = 1; // 발사당 탄약 수량

    [Header("Projectile")] // 발사체 설정 묶음
    [Tooltip("발사할 CombatProjectile 프리팹입니다.")] // Inspector 발사체 설명
    [SerializeField] private CombatProjectile projectilePrefab; // 발사체 프리팹

    [Tooltip("최소 장력에서 사용할 발사체 속도입니다.")] // Inspector 최소 속도 설명
    [SerializeField, Min(0.1f)] private float minimumProjectileSpeed = 12f; // 최소 발사체 속도

    [FormerlySerializedAs("projectileSpeed")] // 기존 최고 속도 값 유지
    [Tooltip("최대 장력에서 사용할 발사체 속도입니다.")] // Inspector 최대 속도 설명
    [SerializeField, Min(0.1f)] private float maximumProjectileSpeed = 30f; // 최대 발사체 속도

    [Tooltip("발사체가 자동으로 제거될 최대 시간입니다.")] // Inspector 수명 설명
    [SerializeField, Min(0.1f)] private float maximumLifetime = 5f; // 발사체 최대 수명

    [Tooltip("발사체 Rigidbody에 중력을 적용할지 설정합니다.")] // Inspector 중력 설명
    [SerializeField] private bool useGravity = true; // 발사체 중력 사용 여부

    [Tooltip("발사 위치에서 전방으로 띄워 생성할 거리입니다.")] // Inspector 생성 거리 설명
    [SerializeField, Min(0f)] private float spawnForwardOffset = 0.2f; // 발사체 전방 생성 거리

    [Header("Bow Charge")] // 활 장력 설정 묶음
    [Tooltip("최소 장력에 도달할 때까지 필요한 시간입니다.")] // Inspector 최소 장전 시간 설명
    [SerializeField, Min(0f)] private float minimumChargeTime = 0.1f; // 최소 장전 시간

    [Tooltip("최대 장력에 도달할 때까지 필요한 시간입니다.")] // Inspector 최대 장전 시간 설명
    [SerializeField, Min(0.05f)] private float maximumChargeTime = 1.5f; // 최대 장전 시간

    [Tooltip("장력 비율에 따라 피해 배율을 계산하는 곡선입니다.")] // Inspector 피해 곡선 설명
    [SerializeField] private AnimationCurve damageMultiplierCurve = new AnimationCurve( // 피해 곡선 기본값
        new Keyframe(0f, 0.4f), // 최소 장력 피해 배율
        new Keyframe(0.5f, 0.9f), // 중간 장력 피해 배율
        new Keyframe(0.8f, 1.2f), // 높은 장력 피해 배율
        new Keyframe(1f, 1.5f)); // 최대 장력 피해 배율

    [Tooltip("장력 비율에 따라 최소와 최대 속도 사이의 비율을 계산하는 곡선입니다.")] // Inspector 속도 곡선 설명
    [SerializeField] private AnimationCurve projectileSpeedCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f); // 발사 속도 곡선

    [Tooltip("장력 비율에 따라 충격량 배율을 계산하는 곡선입니다.")] // Inspector 충격 곡선 설명
    [SerializeField] private AnimationCurve impactMultiplierCurve = new AnimationCurve( // 충격 곡선 기본값
        new Keyframe(0f, 0.3f), // 최소 장력 충격 배율
        new Keyframe(1f, 1.5f)); // 최대 장력 충격 배율

    [Header("Aim Camera")] // 조준 Camera 설정 묶음
    [Tooltip("최대 장력에서 적용할 Camera Field Of View입니다.")] // Inspector 조준 시야각 설명
    [SerializeField, Range(20f, 100f)] private float maximumChargeFieldOfView = 46f; // 최대 장력 시야각

    [Tooltip("활을 당길 때 초당 변경할 Field Of View 속도입니다.")] // Inspector 확대 속도 설명
    [SerializeField, Min(0.1f)] private float cameraZoomSpeed = 45f; // Camera 확대 속도

    [Tooltip("발사 또는 취소 후 초당 복구할 Field Of View 속도입니다.")] // Inspector 복귀 속도 설명
    [SerializeField, Min(0.1f)] private float cameraReturnSpeed = 80f; // Camera 복귀 속도

    [Header("Trajectory Preview")] // 예상 궤적 설정 묶음
    [Tooltip("예상 궤적을 표시할지 설정합니다.")] // Inspector 궤적 표시 설명
    [SerializeField] private bool showTrajectoryPreview = true; // 예상 궤적 표시 여부

    [Tooltip("예상 궤적 지점 사이의 시간 간격입니다.")] // Inspector 궤적 시간 설명
    [SerializeField, Range(0.01f, 0.2f)] private float trajectoryTimeStep = 0.05f; // 궤적 시간 간격

    [Tooltip("예상 궤적에 사용할 최대 지점 수입니다.")] // Inspector 궤적 지점 설명
    [SerializeField, Range(4, 100)] private int trajectoryPointCount = 35; // 궤적 최대 지점 수

    [Tooltip("예상 궤적 충돌 검사에 사용할 구체 반지름입니다.")] // Inspector 궤적 반지름 설명
    [SerializeField, Min(0f)] private float trajectoryCollisionRadius = 0.04f; // 궤적 충돌 반지름

    public ItemData AmmunitionItem => ammunitionItem; // 소비 탄약 아이템 제공
    public int AmmunitionPerShot => Mathf.Max(1, ammunitionPerShot); // 발사당 탄약 수량 제공
    public CombatProjectile ProjectilePrefab => projectilePrefab; // 발사체 프리팹 제공
    public float ProjectileSpeed => MaximumProjectileSpeed; // 기존 최고 속도 제공
    public float MinimumProjectileSpeed => Mathf.Max(0.1f, minimumProjectileSpeed); // 최소 발사체 속도 제공
    public float MaximumProjectileSpeed => Mathf.Max(MinimumProjectileSpeed, maximumProjectileSpeed); // 최대 발사체 속도 제공
    public float MaximumLifetime => Mathf.Max(0.1f, maximumLifetime); // 발사체 최대 수명 제공
    public bool UseGravity => useGravity; // 발사체 중력 사용 여부 제공
    public float SpawnForwardOffset => Mathf.Max(0f, spawnForwardOffset); // 발사체 전방 생성 거리 제공
    public bool RequiresAmmunition => ammunitionItem != null; // 탄약 소비 필요 여부 제공
    public float MinimumChargeTime => Mathf.Max(0f, minimumChargeTime); // 최소 장전 시간 제공
    public float MaximumChargeTime => Mathf.Max(MinimumChargeTime + 0.05f, maximumChargeTime); // 최대 장전 시간 제공
    public float MaximumChargeFieldOfView => Mathf.Clamp(maximumChargeFieldOfView, 20f, 100f); // 최대 장력 시야각 제공
    public float CameraZoomSpeed => Mathf.Max(0.1f, cameraZoomSpeed); // Camera 확대 속도 제공
    public float CameraReturnSpeed => Mathf.Max(0.1f, cameraReturnSpeed); // Camera 복귀 속도 제공
    public bool ShowTrajectoryPreview => showTrajectoryPreview; // 예상 궤적 표시 여부 제공
    public float TrajectoryTimeStep => Mathf.Clamp(trajectoryTimeStep, 0.01f, 0.2f); // 궤적 시간 간격 제공
    public int TrajectoryPointCount => Mathf.Clamp(trajectoryPointCount, 4, 100); // 궤적 지점 수 제공
    public float TrajectoryCollisionRadius => Mathf.Max(0f, trajectoryCollisionRadius); // 궤적 충돌 반지름 제공

    public float GetChargeNormalized(float elapsedTime) // 누적 시간 기준 장력 비율 계산
    {
        return Mathf.InverseLerp(MinimumChargeTime, MaximumChargeTime, Mathf.Max(0f, elapsedTime)); // 최소와 최대 장전 시간 사이 비율 반환
    }

    public float EvaluateDamageMultiplier(float chargeNormalized) // 장력 기준 피해 배율 계산
    {
        float safeCharge = Mathf.Clamp01(chargeNormalized); // 장력 비율 범위 제한
        return Mathf.Max(0f, damageMultiplierCurve.Evaluate(safeCharge)); // 피해 배율 음수 방지 반환
    }

    public float EvaluateProjectileSpeed(float chargeNormalized) // 장력 기준 발사 속도 계산
    {
        float safeCharge = Mathf.Clamp01(chargeNormalized); // 장력 비율 범위 제한
        float speedRatio = Mathf.Clamp01(projectileSpeedCurve.Evaluate(safeCharge)); // 속도 비율 범위 제한
        return Mathf.Lerp(MinimumProjectileSpeed, MaximumProjectileSpeed, speedRatio); // 최소와 최대 속도 보간 반환
    }

    public float EvaluateImpactMultiplier(float chargeNormalized) // 장력 기준 충격 배율 계산
    {
        float safeCharge = Mathf.Clamp01(chargeNormalized); // 장력 비율 범위 제한
        return Mathf.Max(0f, impactMultiplierCurve.Evaluate(safeCharge)); // 충격 배율 음수 방지 반환
    }

    private void OnValidate() // Inspector 설정값 검증
    {
        ammunitionPerShot = Mathf.Max(1, ammunitionPerShot); // 탄약 수량 최소값 적용
        minimumProjectileSpeed = Mathf.Max(0.1f, minimumProjectileSpeed); // 최소 발사체 속도 보정
        maximumProjectileSpeed = Mathf.Max(minimumProjectileSpeed, maximumProjectileSpeed); // 최대 발사체 속도 보정
        maximumLifetime = Mathf.Max(0.1f, maximumLifetime); // 발사체 수명 최소값 적용
        spawnForwardOffset = Mathf.Max(0f, spawnForwardOffset); // 생성 거리 음수 방지
        minimumChargeTime = Mathf.Max(0f, minimumChargeTime); // 최소 장전 시간 음수 방지
        maximumChargeTime = Mathf.Max(minimumChargeTime + 0.05f, maximumChargeTime); // 최대 장전 시간 보정
        maximumChargeFieldOfView = Mathf.Clamp(maximumChargeFieldOfView, 20f, 100f); // 조준 시야각 범위 제한
        cameraZoomSpeed = Mathf.Max(0.1f, cameraZoomSpeed); // 확대 속도 최소값 적용
        cameraReturnSpeed = Mathf.Max(0.1f, cameraReturnSpeed); // 복귀 속도 최소값 적용
        trajectoryTimeStep = Mathf.Clamp(trajectoryTimeStep, 0.01f, 0.2f); // 궤적 시간 간격 범위 제한
        trajectoryPointCount = Mathf.Clamp(trajectoryPointCount, 4, 100); // 궤적 지점 수 범위 제한
        trajectoryCollisionRadius = Mathf.Max(0f, trajectoryCollisionRadius); // 궤적 반지름 음수 방지

        if (damageMultiplierCurve == null || damageMultiplierCurve.length == 0) // 피해 곡선 존재 확인
        {
            damageMultiplierCurve = AnimationCurve.Linear(0f, 0.4f, 1f, 1.5f); // 피해 곡선 기본값 복구
        }

        if (projectileSpeedCurve == null || projectileSpeedCurve.length == 0) // 속도 곡선 존재 확인
        {
            projectileSpeedCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f); // 속도 곡선 기본값 복구
        }

        if (impactMultiplierCurve == null || impactMultiplierCurve.length == 0) // 충격 곡선 존재 확인
        {
            impactMultiplierCurve = AnimationCurve.Linear(0f, 0.3f, 1f, 1.5f); // 충격 곡선 기본값 복구
        }
    }
}
