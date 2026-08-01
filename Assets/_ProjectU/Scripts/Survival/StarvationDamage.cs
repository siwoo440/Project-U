using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class StarvationDamage : MonoBehaviour // 굶주림 피해 관리
{
    [Header("References")] // 외부 참조 묶음
    [Tooltip("플레이어 허기.")]
    [SerializeField] private PlayerHunger playerHunger; // 플레이어 허기
    [Tooltip("플레이어 체력.")]
    [SerializeField] private PlayerHealth playerHealth; // 플레이어 체력

    [Header("Starvation Damage")] // 굶주림 피해 설정 묶음
    [Tooltip("회당 피해량.")]
    [SerializeField] private float damagePerTick = 5f; // 회당 피해량
    [Tooltip("피해 발생 간격.")]
    [SerializeField] private float damageInterval = 2f; // 피해 발생 간격

    [Header("Runtime")] // 실행 상태 묶음
    [Tooltip("현재 피해 대기시간.")]
    [SerializeField] private float starvationTimer; // 현재 피해 대기시간

    private void Awake() // 굶주림 피해 초기화
    {
        if (playerHunger == null || playerHealth == null) // 필수 참조 확인
        {
            Debug.LogError($"{gameObject.name}의 굶주림 피해 참조가 누락되었습니다.", this); // 참조 오류 출력
            enabled = false; // 굶주림 피해 기능 비활성화
            return; // 초기화 처리 중단
        }

        ClampSettings(); // 설정값 범위 보정
        starvationTimer = 0f; // 시작 피해 시간 초기화
    }

    private void Reset() // 컴포넌트 최초 추가값 설정
    {
        playerHunger = GetComponent<PlayerHunger>(); // 동일 오브젝트의 허기 가져오기
        playerHealth = GetComponent<PlayerHealth>(); // 동일 오브젝트의 체력 가져오기
        ClampSettings(); // 설정값 범위 보정
    }

    private void Update() // 굶주림 상태 검사
    {
        if (playerHealth.IsDead) // 사망 상태 확인
        {
            return; // 추가 피해 중단
        }

        if (!playerHunger.IsStarving) // 굶주림 상태 해제 확인
        {
            starvationTimer = 0f; // 피해 대기시간 초기화
            return; // 굶주림 피해 중단
        }

        starvationTimer += Time.deltaTime; // 피해 대기시간 누적

        if (starvationTimer < damageInterval) // 피해 발생시간 확인
        {
            return; // 피해 처리 대기
        }

        starvationTimer -= damageInterval; // 다음 피해 주기 유지
        playerHealth.TakeDamage(damagePerTick); // 굶주림 피해 적용
    }

    private void OnValidate() // Inspector 값 검증
    {
        ClampSettings(); // 설정값 범위 보정
    }

    private void ClampSettings() // 굶주림 피해 설정값 보정
    {
        damagePerTick = Mathf.Max(0f, damagePerTick); // 피해량 음수 방지
        damageInterval = Mathf.Max(0.1f, damageInterval); // 피해 간격 최소값 적용
        starvationTimer = Mathf.Max(0f, starvationTimer); // 대기시간 음수 방지
    }
}