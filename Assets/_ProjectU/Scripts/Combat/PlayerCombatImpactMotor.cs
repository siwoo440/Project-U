using System; // C# 이벤트 기능
using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
[RequireComponent(typeof(CharacterController))] // 플레이어 충돌 이동기 요구
[RequireComponent(typeof(PlayerHealth))] // 플레이어 체력 관리자 요구
public sealed class PlayerCombatImpactMotor : MonoBehaviour // 플레이어 전투 피격 밀림 관리자
{
    [Header("References")] // 플레이어 피격 밀림 참조 묶음
    [Tooltip("밀림 이동에 사용할 CharacterController입니다.")] // Inspector CharacterController 설명
    [SerializeField] private CharacterController characterController; // 플레이어 충돌 이동기

    [Tooltip("사망 상태에서 밀림을 중단할 PlayerHealth입니다.")] // Inspector PlayerHealth 설명
    [SerializeField] private PlayerHealth playerHealth; // 플레이어 체력 관리자

    [Header("Impact")] // 플레이어 피격 밀림 설정 묶음
    [Tooltip("CombatHitData의 충격량 1당 이동할 거리입니다.")] // Inspector 충격 거리 배율 설명
    [SerializeField, Min(0f)] private float distancePerImpactForce = 0.35f; // 충격량당 플레이어 밀림 거리

    [Tooltip("한 번의 피격 밀림이 진행되는 시간입니다.")] // Inspector 밀림 시간 설명
    [SerializeField, Min(0.02f)] private float impactDuration = 0.18f; // 플레이어 밀림 진행 시간

    [Tooltip("한 번의 피격으로 이동할 수 있는 최대 거리입니다.")] // Inspector 최대 밀림 거리 설명
    [SerializeField, Min(0f)] private float maximumImpactDistance = 1.5f; // 플레이어 최대 밀림 거리

    [Tooltip("시간에 따른 누적 밀림 거리 비율입니다. 0에서 1로 증가해야 합니다.")] // Inspector 밀림 곡선 설명
    [SerializeField] private AnimationCurve impactDistanceCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); // 플레이어 밀림 거리 곡선

    [Header("Debug")] // 플레이어 피격 밀림 Debug 설정 묶음
    [Tooltip("피격 밀림 시작과 종료 결과를 Console에 출력합니다.")] // Inspector 피격 밀림 로그 설명
    [SerializeField] private bool logImpactResults = true; // 플레이어 밀림 로그 여부

    [Tooltip("Inspector Context Menu에서 사용할 테스트 충격량입니다.")] // Inspector 테스트 충격량 설명
    [SerializeField, Min(0.1f)] private float testImpactForce = 2f; // 테스트 피격 충격량

    [Header("Runtime")] // 플레이어 밀림 실행 상태 묶음
    [Tooltip("현재 피격 밀림을 적용 중인지 표시합니다.")] // Inspector 밀림 실행 설명
    [SerializeField] private bool isApplyingImpact; // 현재 플레이어 밀림 실행 여부

    [Tooltip("현재 피격 밀림 방향입니다.")] // Inspector 밀림 방향 설명
    [SerializeField] private Vector3 currentImpactDirection; // 현재 플레이어 밀림 방향

    [Tooltip("현재 피격으로 이동할 전체 거리입니다.")] // Inspector 전체 밀림 거리 설명
    [SerializeField] private float currentImpactDistance; // 현재 플레이어 전체 밀림 거리

    [Tooltip("현재 피격 밀림 진행 비율입니다.")] // Inspector 밀림 진행률 설명
    [SerializeField, Range(0f, 1f)] private float currentImpactNormalized; // 현재 플레이어 밀림 진행 비율

    private float impactStartedAt; // 플레이어 밀림 시작 시각
    private float previousCurveValue; // 이전 프레임 누적 밀림 곡선값

    public bool IsApplyingImpact => isApplyingImpact; // 현재 플레이어 밀림 상태 제공
    public Vector3 CurrentImpactDirection => currentImpactDirection; // 현재 플레이어 밀림 방향 제공
    public float CurrentImpactDistance => currentImpactDistance; // 현재 플레이어 전체 밀림 거리 제공
    public float CurrentImpactNormalized => currentImpactNormalized; // 현재 플레이어 밀림 진행 비율 제공

    public event Action<Vector3, float> ImpactStarted; // 밀림 방향과 거리 전달 이벤트
    public event Action ImpactFinished; // 플레이어 밀림 종료 이벤트

    private void Reset() // 컴포넌트 최초 추가 시 참조 자동 연결
    {
        characterController = GetComponent<CharacterController>(); // 같은 Player의 CharacterController 연결
        playerHealth = GetComponent<PlayerHealth>(); // 같은 Player의 PlayerHealth 연결
    }

    private void Awake() // 플레이어 피격 밀림 참조 초기화
    {
        if (characterController == null) // CharacterController 참조 확인
        {
            characterController = GetComponent<CharacterController>(); // 같은 Player에서 CharacterController 자동 검색
        }

        if (playerHealth == null) // PlayerHealth 참조 확인
        {
            playerHealth = GetComponent<PlayerHealth>(); // 같은 Player에서 PlayerHealth 자동 검색
        }

        if (characterController == null || playerHealth == null) // 필수 참조 존재 여부 확인
        {
            Debug.LogError("PlayerCombatImpactMotor에 CharacterController와 PlayerHealth가 필요합니다.", this); // 필수 참조 누락 오류 출력
            enabled = false; // 플레이어 밀림 기능 비활성화
        }
    }

    private void LateUpdate() // 일반 플레이어 이동 이후 피격 밀림 추가 적용
    {
        if (!isApplyingImpact) // 현재 밀림 실행 여부 확인
        {
            return; // 플레이어 밀림 처리 생략
        }

        if (playerHealth.IsDead || !characterController.enabled) // 플레이어 생존과 이동기 상태 확인
        {
            CancelImpact(); // 사망 또는 이동기 비활성화 시 밀림 중단
            return; // 플레이어 밀림 처리 종료
        }

        float elapsedTime = Time.time - impactStartedAt; // 밀림 시작 후 경과 시간 계산
        currentImpactNormalized = Mathf.Clamp01(elapsedTime / impactDuration); // 현재 밀림 진행 비율 계산
        float currentCurveValue = Mathf.Clamp01(impactDistanceCurve.Evaluate(currentImpactNormalized)); // 현재 누적 거리 곡선값 계산
        float curveDelta = Mathf.Max(0f, currentCurveValue - previousCurveValue); // 이번 프레임 적용할 곡선 차이 계산
        float frameDistance = currentImpactDistance * curveDelta; // 이번 프레임 플레이어 밀림 거리 계산

        if (frameDistance > 0f) // 실제 이동 거리 존재 여부 확인
        {
            characterController.Move(currentImpactDirection * frameDistance); // 충돌을 적용한 피격 밀림 이동 실행
        }

        previousCurveValue = currentCurveValue; // 현재 곡선값을 다음 프레임 기준으로 저장

        if (currentImpactNormalized >= 1f) // 플레이어 밀림 완료 여부 확인
        {
            FinishImpact(); // 플레이어 밀림 종료 처리
        }
    }

    public bool ApplyImpact(Vector3 impactDirection, float impactForce) // 공격 방향과 충격량으로 플레이어 밀림 시작
    {
        if (!isActiveAndEnabled || playerHealth == null || playerHealth.IsDead) // 컴포넌트와 플레이어 생존 상태 확인
        {
            return false; // 플레이어 밀림 시작 실패 반환
        }

        Vector3 planarDirection = impactDirection; // 전달된 공격 방향 복사
        planarDirection.y = 0f; // 수평 밀림만 사용하도록 높이 방향 제거

        if (planarDirection.sqrMagnitude < 0.0001f || impactForce <= 0f) // 유효한 방향과 충격량 확인
        {
            return false; // 플레이어 밀림 시작 실패 반환
        }

        currentImpactDirection = planarDirection.normalized; // 정규화된 플레이어 밀림 방향 저장
        currentImpactDistance = Mathf.Min( // 최종 플레이어 밀림 거리 계산 시작
            maximumImpactDistance, // 최대 밀림 거리 제한
            impactForce * distancePerImpactForce); // 공격 충격량 기반 밀림 거리 계산

        if (currentImpactDistance <= 0f) // 최종 플레이어 밀림 거리 확인
        {
            return false; // 플레이어 밀림 시작 실패 반환
        }

        impactStartedAt = Time.time; // 플레이어 밀림 시작 시각 저장
        currentImpactNormalized = 0f; // 플레이어 밀림 진행 비율 초기화
        previousCurveValue = Mathf.Clamp01(impactDistanceCurve.Evaluate(0f)); // 밀림 거리 곡선 시작값 저장
        isApplyingImpact = true; // 플레이어 밀림 실행 상태 적용
        ImpactStarted?.Invoke(currentImpactDirection, currentImpactDistance); // 밀림 방향과 전체 거리 전달

        if (logImpactResults) // 플레이어 밀림 로그 사용 여부 확인
        {
            Debug.Log( // 플레이어 밀림 시작 로그 출력 시작
                $"플레이어 피격 밀림 시작 / 충격량 {impactForce:0.##} / " // 전달 충격량 출력
                + $"거리 {currentImpactDistance:0.##}", // 최종 밀림 거리 출력
                this); // 현재 Player를 Log Context로 지정
        }

        return true; // 플레이어 밀림 시작 성공 반환
    }

    public void CancelImpact() // 현재 플레이어 피격 밀림 즉시 중단
    {
        if (!isApplyingImpact) // 실행 중인 밀림 존재 여부 확인
        {
            return; // 플레이어 밀림 중단 처리 생략
        }

        isApplyingImpact = false; // 플레이어 밀림 실행 상태 해제
        currentImpactNormalized = 0f; // 플레이어 밀림 진행 비율 초기화
        previousCurveValue = 0f; // 이전 곡선값 초기화
    }

    [ContextMenu("Apply Test Impact")] // Inspector 플레이어 밀림 테스트 메뉴
    private void ApplyTestImpact() // 플레이어가 바라보는 반대 방향으로 테스트 밀림 적용
    {
        if (!Application.isPlaying) // Play Mode 여부 확인
        {
            Debug.LogWarning("플레이어 피격 밀림 테스트는 Play Mode에서 실행해야 합니다.", this); // Edit Mode 테스트 경고 출력
            return; // 테스트 밀림 처리 중단
        }

        ApplyImpact(-transform.forward, testImpactForce); // 플레이어 후방 방향으로 테스트 충격 적용
    }

    private void FinishImpact() // 플레이어 피격 밀림 정상 종료
    {
        isApplyingImpact = false; // 플레이어 밀림 실행 상태 해제
        currentImpactNormalized = 1f; // 플레이어 밀림 진행 비율 완료 적용
        previousCurveValue = 1f; // 누적 거리 곡선 완료값 적용
        ImpactFinished?.Invoke(); // 플레이어 밀림 종료 이벤트 전달

        if (logImpactResults) // 플레이어 밀림 로그 사용 여부 확인
        {
            Debug.Log("플레이어 피격 밀림 종료", this); // 플레이어 밀림 종료 결과 출력
        }
    }

    private void OnDisable() // 컴포넌트 비활성화 시 피격 밀림 정리
    {
        CancelImpact(); // 실행 중인 플레이어 밀림 중단
    }

    private void OnValidate() // Inspector 피격 밀림 설정값 검증
    {
        distancePerImpactForce = Mathf.Max(0f, distancePerImpactForce); // 충격 거리 배율 음수 방지
        impactDuration = Mathf.Max(0.02f, impactDuration); // 밀림 시간 최소값 적용
        maximumImpactDistance = Mathf.Max(0f, maximumImpactDistance); // 최대 밀림 거리 음수 방지
        testImpactForce = Mathf.Max(0.1f, testImpactForce); // 테스트 충격량 최소값 적용

        if (characterController == null) // CharacterController 참조 확인
        {
            characterController = GetComponent<CharacterController>(); // 같은 Player의 CharacterController 자동 연결
        }

        if (playerHealth == null) // PlayerHealth 참조 확인
        {
            playerHealth = GetComponent<PlayerHealth>(); // 같은 Player의 PlayerHealth 자동 연결
        }
    }
}
