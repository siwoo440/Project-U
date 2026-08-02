using System.Collections; // 코루틴 기능
using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
[RequireComponent(typeof(Collider))] // 공격 판정용 Collider 요구
public sealed class TrainingDamageTarget : MonoBehaviour, ICombatDamageReceiver // 전투 기반 확인용 훈련 표적
{
    [Header("Health")] // 훈련 표적 체력 설정 묶음
    [Tooltip("훈련 표적의 최대 체력입니다.")]
    [SerializeField, Min(1f)] private float maximumHealth = 50f; // 최대 체력

    [Header("Reset")] // 훈련 표적 복구 설정 묶음
    [Tooltip("체력이 0이 된 뒤 자동으로 최대 체력을 복구할지 설정합니다.")]
    [SerializeField] private bool resetAfterDefeat = true; // 자동 복구 여부

    [Tooltip("체력이 0이 된 뒤 복구까지 기다릴 시간입니다.")]
    [SerializeField, Min(0.1f)] private float resetDelay = 2f; // 자동 복구 대기 시간

    [Header("Debug")] // 훈련 표적 확인 설정 묶음
    [Tooltip("피해 수신 결과를 Console에 출력할지 설정합니다.")]
    [SerializeField] private bool logDamage = true; // 피해 로그 출력 여부

    [Header("Runtime")] // 훈련 표적 실행 상태 묶음
    [Tooltip("현재 훈련 표적 체력입니다.")]
    [SerializeField] private float currentHealth; // 현재 체력

    private Coroutine resetCoroutine; // 실행 중인 복구 코루틴

    public Transform DamageRoot => transform; // 현재 오브젝트를 피해 대상 기준으로 제공
    public bool IsAlive => currentHealth > 0f; // 현재 생존 상태 제공
    public float CurrentHealth => currentHealth; // 현재 체력 제공
    public float MaximumHealth => maximumHealth; // 최대 체력 제공

    private void Awake() // 훈련 표적 초기화
    {
        maximumHealth = Mathf.Max(1f, maximumHealth); // 최대 체력 최소값 적용
        currentHealth = maximumHealth; // 시작 체력 최대값 적용
    }

    public bool ReceiveDamage(CombatHitData hitData) // 플레이어 무기 피해 수신
    {
        if (!IsAlive || hitData.Damage <= 0f) // 생존 상태와 유효 피해 확인
        {
            return false; // 피해 처리 실패 반환
        }

        currentHealth = Mathf.Max(0f, currentHealth - hitData.Damage); // 현재 체력 감소

        if (logDamage) // 피해 로그 사용 여부 확인
        {
            string itemName = hitData.SourceItem == null
                ? "UNARMED"
                : hitData.SourceItem.DisplayName; // 사용 아이템 이름 계산

            Debug.Log(
                $"{gameObject.name} 피해 {hitData.Damage:0.##} / 남은 체력 {currentHealth:0.##} / 공격 {itemName}",
                this); // 피해 결과 출력
        }

        if (currentHealth > 0f) // 남은 체력 확인
        {
            return true; // 일반 피해 성공 반환
        }

        HandleDefeated(); // 체력 소진 처리
        return true; // 마지막 피해 성공 반환
    }

    private void HandleDefeated() // 훈련 표적 체력 소진 처리
    {
        if (logDamage) // 결과 로그 사용 여부 확인
        {
            Debug.Log($"{gameObject.name} 훈련 표적이 파괴 상태가 되었습니다.", this); // 체력 소진 결과 출력
        }

        if (!resetAfterDefeat) // 자동 복구 사용 여부 확인
        {
            return; // 체력 0 상태 유지
        }

        if (resetCoroutine != null) // 기존 복구 코루틴 확인
        {
            StopCoroutine(resetCoroutine); // 기존 복구 대기 중단
        }

        resetCoroutine = StartCoroutine(ResetRoutine()); // 새로운 자동 복구 시작
    }

    private IEnumerator ResetRoutine() // 훈련 표적 자동 복구
    {
        yield return new WaitForSeconds(resetDelay); // 설정된 복구 시간 대기
        currentHealth = maximumHealth; // 체력 최대값 복구
        resetCoroutine = null; // 복구 코루틴 상태 초기화

        if (logDamage) // 복구 로그 사용 여부 확인
        {
            Debug.Log($"{gameObject.name} 훈련 표적 체력이 복구되었습니다.", this); // 복구 결과 출력
        }
    }

    private void OnDisable() // 비활성화 상태 정리
    {
        if (resetCoroutine == null) // 복구 코루틴 실행 여부 확인
        {
            return; // 정리할 코루틴 없음
        }

        StopCoroutine(resetCoroutine); // 실행 중인 복구 코루틴 중단
        resetCoroutine = null; // 코루틴 상태 초기화
    }

    private void OnValidate() // Inspector 값 검증
    {
        maximumHealth = Mathf.Max(1f, maximumHealth); // 최대 체력 최소값 적용
        resetDelay = Mathf.Max(0.1f, resetDelay); // 복구 시간 최소값 적용

        if (!Application.isPlaying) // Edit Mode 여부 확인
        {
            currentHealth = maximumHealth; // Inspector 현재 체력 표시 동기화
        }
    }
}
