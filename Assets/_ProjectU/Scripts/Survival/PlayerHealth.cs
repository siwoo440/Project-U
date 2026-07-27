using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class PlayerHealth : MonoBehaviour // 플레이어 체력 관리
{
    [Header("Health")] // 체력 설정 묶음
    [SerializeField] private float maxHealth = 100f; // 최대 체력

    [Header("Runtime")] // 실행 상태 묶음
    [SerializeField] private float currentHealth = 100f; // 현재 체력
    [SerializeField] private bool isDead; // 사망 상태

    public float CurrentHealth => currentHealth; // 현재 체력 제공
    public float MaxHealth => maxHealth; // 최대 체력 제공
    public float NormalizedHealth => currentHealth / maxHealth; // 체력 비율 제공
    public bool IsDead => isDead; // 사망 여부 제공

    private void Awake() // 체력 초기화
    {
        ClampSettings(); // 설정값 범위 보정
        currentHealth = maxHealth; // 시작 체력 최대 적용
        isDead = false; // 시작 사망 상태 해제
    }

    private void Reset() // 최초 추가값 설정
    {
        ClampSettings(); // 설정값 범위 보정
        currentHealth = maxHealth; // Inspector 체력 초기화
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

        currentHealth = Mathf.Max(0f, currentHealth - damageAmount); // 체력 감소 적용

        if (currentHealth <= 0f) // 체력 소진 확인
        {
            currentHealth = 0f; // 체력 0 고정
            isDead = true; // 사망 상태 적용
        }

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

        if (currentHealth >= maxHealth) // 최대 체력 확인
        {
            return false; // 불필요한 회복 차단
        }

        currentHealth = Mathf.Min(maxHealth, currentHealth + healAmount); // 체력 회복 적용
        return true; // 회복 처리 성공
    }

    private void ClampSettings() // 체력 설정값 보정
    {
        maxHealth = Mathf.Max(1f, maxHealth); // 최대 체력 최소값 적용
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth); // 현재 체력 범위 제한
    }
}