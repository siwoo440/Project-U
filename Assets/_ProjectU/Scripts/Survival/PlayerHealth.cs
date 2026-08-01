using System; // C# 이벤트 기능
using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
[RequireComponent(typeof(PlayerEquipment))] // 필수 장비 컴포넌트
public sealed class PlayerHealth : MonoBehaviour // 플레이어 체력 관리
{
    [Header("Health")] // 체력 설정 묶음
    [Tooltip("최대 체력.")]
    [SerializeField] private float maxHealth = 100f; // 최대 체력

    [Header("Runtime")] // 실행 상태 묶음
    [Tooltip("플레이어 장비 관리자.")]
    [SerializeField] private PlayerEquipment playerEquipment; // 플레이어 장비 관리자
    [Tooltip("장비 적용 최대 체력.")]
    [SerializeField] private float currentMaximumHealth = 100f; // 장비 적용 최대 체력
    [Tooltip("현재 체력.")]
    [SerializeField] private float currentHealth = 100f; // 현재 체력
    [Tooltip("사망 상태.")]
    [SerializeField] private bool isDead; // 사망 상태

    public float CurrentHealth => currentHealth; // 현재 체력 제공
    public float MaxHealth => currentMaximumHealth; // 장비 적용 최대 체력 제공
    public float NormalizedHealth => currentHealth / currentMaximumHealth; // 장비 적용 체력 비율 제공
    public bool IsDead => isDead; // 사망 여부 제공
    public event Action<float> Damaged; // 실제 피해량 이벤트
    public event Action<float> Healed; // 실제 회복량 이벤트

    private void Awake() // 체력 초기화
    {
        playerEquipment = GetComponent<PlayerEquipment>(); // 장비 관리자 가져오기
        currentMaximumHealth = maxHealth + playerEquipment.TotalMaximumHealthBonus; // 시작 최대 체력 계산
        ClampSettings(); // 설정값 범위 보정
        currentHealth = currentMaximumHealth; // 시작 체력 최대 적용
        isDead = false; // 시작 사망 상태 해제
    }

    private void Start() // 전체 컴포넌트 초기화 후 능력치 확인
    {
        RefreshEquipmentStats(); // 시작 장비 능력치 재적용
    }

    private void OnEnable() // 장비 이벤트 연결
    {
        if (playerEquipment == null) // 장비 관리자 미연결 확인
        {
            playerEquipment = GetComponent<PlayerEquipment>(); // 장비 관리자 가져오기
        }

        if (playerEquipment != null) // 장비 관리자 확인
        {
            playerEquipment.EquipmentChanged += RefreshEquipmentStats; // 장비 변경 구독
        }
    }

    private void OnDisable() // 장비 이벤트 해제
    {
        if (playerEquipment != null) // 장비 관리자 확인
        {
            playerEquipment.EquipmentChanged -= RefreshEquipmentStats; // 장비 변경 구독 해제
        }
    }

    private void Reset() // 최초 추가값 설정
    {
        playerEquipment = GetComponent<PlayerEquipment>(); // 장비 관리자 자동 연결
        currentMaximumHealth = maxHealth; // 기본 최대 체력 적용
        ClampSettings(); // 설정값 범위 보정
        currentHealth = currentMaximumHealth; // Inspector 체력 초기화
        isDead = false; // Inspector 사망 상태 해제
    }

    private void OnValidate() // Inspector 값 검증
    {
        ClampSettings(); // 설정값 범위 보정
    }

    public bool TakeDamage(float damageAmount) // 피해 적용
    {
        if (damageAmount <= 0f) // 피해량 유효성 확인
        {
            return false; // 피해 처리 실패
        }

        if (isDead) // 기존 사망 상태 확인
        {
            return false; // 추가 피해 차단
        }

        float defensePercent = playerEquipment == null ? 0f : playerEquipment.TotalDefensePercent; // 현재 방어력 조회
        float damageMultiplier = 1f - defensePercent / 100f; // 실제 피해 배율 계산
        float reducedDamageAmount = damageAmount * damageMultiplier; // 방어 적용 피해량 계산
        float previousHealth = currentHealth; // 피해 전 체력 저장
        currentHealth = Mathf.Max(0f, currentHealth - reducedDamageAmount); // 방어 적용 체력 감소
        float appliedDamage = previousHealth - currentHealth; // 실제 피해량 계산

        if (currentHealth <= 0f) // 체력 소진 확인
        {
            currentHealth = 0f; // 체력 0 고정
            isDead = true; // 사망 상태 적용
        }

        Damaged?.Invoke(appliedDamage); // 실제 피해량 전달
        return true; // 피해 처리 성공
    }

    public bool Heal(float healAmount) // 체력 회복
    {
        if (healAmount <= 0f) // 회복량 유효성 확인
        {
            return false; // 회복 처리 실패
        }

        if (isDead) // 사망 상태 확인
        {
            return false; // 사망 후 회복 차단
        }

        if (currentHealth >= currentMaximumHealth) // 장비 적용 최대 체력 확인
        {
            return false; // 불필요한 회복 차단
        }

        float previousHealth = currentHealth; // 회복 전 체력 저장
        currentHealth = Mathf.Min(currentMaximumHealth, currentHealth + healAmount); // 장비 적용 최대 체력까지 회복
        float appliedHealing = currentHealth - previousHealth; // 실제 회복량 계산

        Healed?.Invoke(appliedHealing); // 실제 회복량 전달
        return true; // 회복 처리 성공
    }

    public void SetCurrentHealth(float healthAmount) // 불러온 현재 체력 적용
    {
        currentHealth = Mathf.Clamp(healthAmount, 0f, currentMaximumHealth); // 장비 적용 최대 체력 범위 제한
        isDead = currentHealth <= 0f; // 체력 기준 사망 상태 적용
    }
    public bool Revive(float reviveHealth) // 사망 상태 부활 처리
    {
        if (!isDead) // 현재 사망 여부 확인
        {
            return false; // 생존 상태 부활 차단
        }

        if (reviveHealth <= 0f) // 부활 체력 유효성 확인
        {
            return false; // 잘못된 부활 체력 차단
        }

        float previousHealth = currentHealth; // 부활 전 체력 저장
        currentHealth = Mathf.Clamp(reviveHealth, 1f, currentMaximumHealth); // 부활 체력 적용
        isDead = false; // 사망 상태 해제
        float appliedHealing = currentHealth - previousHealth; // 실제 회복량 계산
        Healed?.Invoke(appliedHealing); // 체력 회복 이벤트 전달
        return true; // 부활 성공
    }
    private void RefreshEquipmentStats() // 장비 최대 체력 적용
    {
        if (playerEquipment == null) // 장비 관리자 확인
        {
            return; // 능력치 갱신 중단
        }

        float previousMaximumHealth = currentMaximumHealth; // 기존 최대 체력 저장
        currentMaximumHealth = maxHealth + playerEquipment.TotalMaximumHealthBonus; // 새로운 최대 체력 계산
        currentMaximumHealth = Mathf.Max(1f, currentMaximumHealth); // 새로운 최대 체력 최소값 적용
        float maximumHealthDifference = currentMaximumHealth - previousMaximumHealth; // 최대 체력 변화량 계산
        currentHealth = Mathf.Clamp(currentHealth + maximumHealthDifference, 0f, currentMaximumHealth); // 현재 체력 변화 적용
    }

    private void ClampSettings() // 체력 설정값 보정
    {
        maxHealth = Mathf.Max(1f, maxHealth); // 최대 체력 최소값 적용
        currentMaximumHealth = Mathf.Max(1f, currentMaximumHealth); // 장비 적용 최대 체력 최소값 적용
        currentHealth = Mathf.Clamp(currentHealth, 0f, currentMaximumHealth); // 현재 체력 범위 제한
    }
}