using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class DehydrationDamage : MonoBehaviour // 탈수 피해 관리
{
    [Header("References")] // 외부 참조 묶음
    [SerializeField] private PlayerThirst playerThirst; // 플레이어 갈증
    [SerializeField] private PlayerHealth playerHealth; // 플레이어 체력

    [Header("Dehydration Damage")] // 탈수 피해 설정 묶음
    [SerializeField] private float damagePerTick = 2f; // 회당 피해량
    [SerializeField] private float damageInterval = 2f; // 피해 발생 간격

    [Header("Runtime")] // 실행 상태 묶음
    [SerializeField] private float dehydrationTimer; // 현재 피해 대기시간

    private void Awake() // 탈수 피해 초기화
    {
        if (playerThirst == null || playerHealth == null) // 필수 참조 확인
        {
            Debug.LogError($"{gameObject.name}의 탈수 피해 참조가 누락되었습니다.", this); // 참조 오류 출력
            enabled = false; // 탈수 피해 기능 비활성화
            return; // 초기화 처리 중단
        }

        ClampSettings(); // 설정값 범위 보정
        dehydrationTimer = 0f; // 시작 피해 시간 초기화
    }

    private void Reset() // 컴포넌트 최초 추가값 설정
    {
        playerThirst = GetComponent<PlayerThirst>(); // 동일 오브젝트의 갈증 가져오기
        playerHealth = GetComponent<PlayerHealth>(); // 동일 오브젝트의 체력 가져오기
        ClampSettings(); // 설정값 범위 보정
    }

    private void Update() // 탈수 상태 검사
    {
        if (playerHealth.IsDead) // 사망 상태 확인
        {
            return; // 추가 피해 중단
        }

        if (!playerThirst.IsDehydrated) // 탈수 상태 해제 확인
        {
            dehydrationTimer = 0f; // 피해 대기시간 초기화
            return; // 탈수 피해 중단
        }

        dehydrationTimer += Time.deltaTime; // 피해 대기시간 누적

        if (dehydrationTimer < damageInterval) // 피해 발생시간 확인
        {
            return; // 피해 처리 대기
        }

        dehydrationTimer -= damageInterval; // 다음 피해 주기 유지
        playerHealth.TakeDamage(damagePerTick); // 탈수 피해 적용
    }

    private void OnValidate() // Inspector 값 검증
    {
        ClampSettings(); // 설정값 범위 보정
    }

    private void ClampSettings() // 탈수 피해 설정값 보정
    {
        damagePerTick = Mathf.Max(0f, damagePerTick); // 피해량 음수 방지
        damageInterval = Mathf.Max(0.1f, damageInterval); // 피해 간격 최소값 적용
        dehydrationTimer = Mathf.Max(0f, dehydrationTimer); // 대기시간 음수 방지
    }
}