using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
[RequireComponent(typeof(CharacterController))] // 캐릭터 이동 충돌 컴포넌트 요구
[RequireComponent(typeof(PlayerMovement))] // 플레이어 이동 컴포넌트 요구
[RequireComponent(typeof(PlayerHealth))] // 플레이어 체력 컴포넌트 요구
[RequireComponent(typeof(PlayerHunger))] // 플레이어 허기 컴포넌트 요구
[RequireComponent(typeof(PlayerThirst))] // 플레이어 갈증 컴포넌트 요구
[RequireComponent(typeof(PlayerWetness))] // 플레이어 젖음 컴포넌트 요구
[RequireComponent(typeof(PlayerTemperature))] // 플레이어 체온 컴포넌트 요구
public sealed class PlayerRespawnSystem : MonoBehaviour // 플레이어 부활 지점 관리
{
    [Header("References")] // 외부 참조 묶음
    [SerializeField] private DayNightCycle dayNightCycle; // 낮과 밤 시간 관리자
    [SerializeField] private Transform defaultRespawnPoint; // 기본 부활 지점

    [Header("Respawn")] // 부활 설정 묶음
    [SerializeField] private float respawnHealth = 50f; // 부활 체력
    [SerializeField] private float respawnHunger = 50f; // 부활 허기
    [SerializeField] private float respawnThirst = 50f; // 부활 갈증
    [SerializeField] private float respawnWetness = 0f; // 부활 젖음 수치
    [SerializeField] private float respawnTemperature = 100f; // 부활 체온 수치
    [SerializeField] private float respawnHour = 8f; // 부활 시간

    [Header("Runtime")] // 실행 상태 묶음
    [SerializeField] private Transform registeredRespawnPoint; // 등록된 침낭 부활 지점

    private CharacterController characterController; // 캐릭터 이동 충돌기
    private PlayerMovement playerMovement; // 플레이어 이동 관리자
    private PlayerHealth playerHealth; // 플레이어 체력 관리자
    private PlayerHunger playerHunger; // 플레이어 허기 관리자
    private PlayerThirst playerThirst; // 플레이어 갈증 관리자
    private PlayerWetness playerWetness; // 플레이어 젖음 관리자
    private PlayerTemperature playerTemperature; // 플레이어 체온 관리자

    public bool HasRegisteredRespawnPoint => registeredRespawnPoint != null; // 침낭 부활 지점 등록 여부 제공
    public Transform RegisteredRespawnPoint => registeredRespawnPoint; // 현재 등록 침낭 위치 제공

    private void Awake() // 부활 시스템 초기화
    {
        characterController = GetComponent<CharacterController>(); // 캐릭터 이동 충돌기 가져오기
        playerMovement = GetComponent<PlayerMovement>(); // 플레이어 이동 관리자 가져오기
        playerHealth = GetComponent<PlayerHealth>(); // 플레이어 체력 관리자 가져오기
        playerHunger = GetComponent<PlayerHunger>(); // 플레이어 허기 관리자 가져오기
        playerThirst = GetComponent<PlayerThirst>(); // 플레이어 갈증 관리자 가져오기
        playerWetness = GetComponent<PlayerWetness>(); // 플레이어 젖음 관리자 가져오기
        playerTemperature = GetComponent<PlayerTemperature>(); // 플레이어 체온 관리자 가져오기
        ClampSettings(); // 설정값 범위 보정

        if (dayNightCycle == null || defaultRespawnPoint == null) // 필수 Scene 참조 확인
        {
            Debug.LogError("PlayerRespawnSystem의 시간 시스템과 기본 부활 지점을 연결해야 합니다.", this); // 참조 누락 오류 출력
            enabled = false; // 부활 시스템 비활성화
        }
    }

    private void OnValidate() // Inspector 설정값 검증
    {
        ClampSettings(); // 설정값 범위 보정
    }

    public void RegisterRespawnPoint(Transform newRespawnPoint) // 침낭 부활 지점 등록
    {
        if (newRespawnPoint == null) // 새로운 부활 지점 확인
        {
            return; // 잘못된 지점 등록 차단
        }

        registeredRespawnPoint = newRespawnPoint; // 새로운 부활 지점 저장
    }
    public void ClearRegisteredRespawnPoint() // 저장 데이터에 따른 부활 지점 해제
    {
        registeredRespawnPoint = null; // 등록된 침낭 참조 제거
    }

    public bool IsRegisteredRespawnPoint(Transform targetPoint) // 지정 지점의 활성 상태 확인
    {
        if (targetPoint == null) // 확인 대상 존재 여부 확인
        {
            return false; // 빈 지점 비활성 반환
        }

        return registeredRespawnPoint == targetPoint; // 현재 등록 지점 비교 결과 반환
    }

    public bool TryRespawn() // 현재 부활 지점으로 복귀 시도
    {
        if (!enabled) // 부활 시스템 활성 상태 확인
        {
            return false; // 비활성 시스템 부활 차단
        }

        if (!playerHealth.IsDead) // 플레이어 사망 여부 확인
        {
            return false; // 생존 상태 부활 차단
        }

        Vector3 targetPosition = defaultRespawnPoint.position; // 기본 부활 위치 설정
        Quaternion targetRotation = defaultRespawnPoint.rotation; // 기본 부활 회전 설정

        if (registeredRespawnPoint != null) // 등록된 침낭 존재 여부 확인
        {
            targetPosition = registeredRespawnPoint.position; // 침낭 부활 위치 적용
            targetRotation = registeredRespawnPoint.rotation; // 침낭 부활 회전 적용
        }

        targetRotation = Quaternion.Euler(0f, targetRotation.eulerAngles.y, 0f); // 좌우 회전만 유지
        characterController.enabled = false; // 위치 이동 전 충돌기 비활성화
        transform.SetPositionAndRotation(targetPosition, targetRotation); // 플레이어 부활 위치 이동
        playerMovement.ResetMotionState(); // 이동과 낙하 상태 초기화
        characterController.enabled = true; // 위치 이동 후 충돌기 활성화
        playerHunger.SetCurrentHunger(respawnHunger); // 부활 허기 적용
        playerThirst.SetCurrentThirst(respawnThirst); // 부활 갈증 적용
        playerWetness.SetCurrentWetness(respawnWetness); // 부활 젖음 수치 적용
        playerTemperature.SetCurrentTemperature(respawnTemperature); // 부활 체온 수치 적용

        dayNightCycle.AdvanceToHour(respawnHour); // 부활 시간 적용
        return playerHealth.Revive(respawnHealth); // 플레이어 체력과 사망 상태 복구
    }

    private void ClampSettings() // 부활 설정값 보정
    {
        respawnHealth = Mathf.Max(1f, respawnHealth); // 부활 체력 최소값 적용
        respawnHunger = Mathf.Max(0f, respawnHunger); // 부활 허기 음수 방지
        respawnThirst = Mathf.Max(0f, respawnThirst); // 부활 갈증 음수 방지
        respawnWetness = Mathf.Clamp(respawnWetness, 0f, 100f); // 부활 젖음 범위 적용
        respawnTemperature = Mathf.Clamp(respawnTemperature, 0f, 100f); // 부활 체온 범위 적용
        respawnHour = Mathf.Clamp(Mathf.Round(respawnHour), 0f, 23f); // 부활 시간 하루 범위 적용
    }
}