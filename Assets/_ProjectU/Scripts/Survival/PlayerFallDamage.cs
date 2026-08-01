using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
[RequireComponent(typeof(PlayerMovement))] // 이동 컴포넌트 필수 지정
[RequireComponent(typeof(PlayerHealth))] // 체력 컴포넌트 필수 지정
public sealed class PlayerFallDamage : MonoBehaviour // 플레이어 낙하 피해 관리
{
    [Header("References")] // 외부 참조 묶음
    [Tooltip("플레이어 이동.")]
    [SerializeField] private PlayerMovement playerMovement; // 플레이어 이동
    [Tooltip("플레이어 체력.")]
    [SerializeField] private PlayerHealth playerHealth; // 플레이어 체력

    [Header("Fall Damage")] // 낙하 피해 설정 묶음
    [Tooltip("피해 없는 최대 거리.")]
    [SerializeField] private float safeFallDistance = 3f; // 피해 없는 최대 거리
    [Tooltip("최대 피해 도달 거리.")]
    [SerializeField] private float fatalFallDistance = 12f; // 최대 피해 도달 거리
    [Tooltip("최대 낙하 피해.")]
    [SerializeField] private float maximumDamage = 100f; // 최대 낙하 피해

    [Header("Runtime")] // 실행 상태 묶음
    [Tooltip("마지막 낙하 거리.")]
    [SerializeField] private float lastFallDistance; // 마지막 낙하 거리
    [Tooltip("마지막 적용 피해.")]
    [SerializeField] private float lastAppliedDamage; // 마지막 적용 피해

    private void Awake() // 낙하 피해 초기화
    {
        if (playerMovement == null) // 이동 참조 확인
        {
            playerMovement = GetComponent<PlayerMovement>(); // 동일 오브젝트 이동 가져오기
        }

        if (playerHealth == null) // 체력 참조 확인
        {
            playerHealth = GetComponent<PlayerHealth>(); // 동일 오브젝트 체력 가져오기
        }

        if (playerMovement == null || playerHealth == null) // 필수 참조 확인
        {
            Debug.LogError($"{gameObject.name}의 낙하 피해 참조가 누락되었습니다.", this); // 참조 오류 출력
            enabled = false; // 낙하 피해 기능 비활성화
            return; // 초기화 처리 중단
        }

        ClampSettings(); // 설정값 범위 보정
    }

    private void Reset() // 컴포넌트 최초 추가값 설정
    {
        playerMovement = GetComponent<PlayerMovement>(); // 동일 오브젝트 이동 가져오기
        playerHealth = GetComponent<PlayerHealth>(); // 동일 오브젝트 체력 가져오기
        ClampSettings(); // 설정값 범위 보정
    }

    private void OnEnable() // 착지 이벤트 연결
    {
        if (playerMovement == null) // 이동 참조 확인
        {
            return; // 이벤트 연결 중단
        }

        playerMovement.Landed += HandleLanded; // 착지 이벤트 구독
    }

    private void OnDisable() // 착지 이벤트 해제
    {
        if (playerMovement == null) // 이동 참조 확인
        {
            return; // 이벤트 해제 중단
        }

        playerMovement.Landed -= HandleLanded; // 착지 이벤트 구독 해제
    }

    private void OnValidate() // Inspector 값 검증
    {
        ClampSettings(); // 설정값 범위 보정
    }

    private void HandleLanded(float fallDistance) // 착지 피해 처리
    {
        lastFallDistance = Mathf.Max(0f, fallDistance); // 마지막 낙하 거리 저장
        lastAppliedDamage = 0f; // 마지막 피해 초기화

        if (playerHealth.IsDead) // 사망 상태 확인
        {
            return; // 사망 후 피해 차단
        }

        if (lastFallDistance <= safeFallDistance) // 안전 낙하 거리 확인
        {
            return; // 낙하 피해 제외
        }

        float damageRatio = Mathf.InverseLerp(safeFallDistance, fatalFallDistance, lastFallDistance); // 낙하 피해 비율 계산
        float damageAmount = maximumDamage * damageRatio; // 최종 낙하 피해 계산

        lastAppliedDamage = damageAmount; // 마지막 피해 저장
        playerHealth.TakeDamage(damageAmount); // 체력 피해 적용
    }

    private void ClampSettings() // 낙하 피해 설정값 보정
    {
        safeFallDistance = Mathf.Max(0f, safeFallDistance); // 안전 거리 음수 방지
        fatalFallDistance = Mathf.Max(safeFallDistance + 0.1f, fatalFallDistance); // 최대 피해 거리 보정
        maximumDamage = Mathf.Max(0f, maximumDamage); // 최대 피해 음수 방지
        lastFallDistance = Mathf.Max(0f, lastFallDistance); // 마지막 거리 음수 방지
        lastAppliedDamage = Mathf.Max(0f, lastAppliedDamage); // 마지막 피해 음수 방지
    }
}