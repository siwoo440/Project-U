using System.Collections; // 코루틴 기능
using UnityEngine; // Unity 기본 기능

public sealed class ToolSwingAnimation : MonoBehaviour // 도구와 근접 무기 휘두르기 연출
{
    [Header("References")] // 참조 설정 묶음
    [Tooltip("회전할 도구 보관 위치.")]
    [SerializeField] private Transform toolHolder; // 회전할 도구 보관 위치

    [Header("Default Swing")] // 기본 휘두르기 설정 묶음
    [Tooltip("연속 공격 데이터가 없을 때 사용할 기본 타격 회전 차이.")]
    [SerializeField] private Vector3 swingRotationOffset = new Vector3(65f, 0f, -25f); // 기본 타격 회전 차이

    [Tooltip("타격 방향 회전 시간.")]
    [SerializeField] private float swingDuration = 0.12f; // 기본 타격 방향 회전 시간

    [Tooltip("기본 자세 복귀 시간.")]
    [SerializeField] private float returnDuration = 0.18f; // 기본 자세 복귀 시간

    [Header("Combo Swing")] // 연속 공격 휘두르기 설정 묶음
    [Tooltip("연속 공격 단계별 회전 차이입니다. 단계 번호가 배열 길이를 넘으면 반복 사용합니다.")]
    [SerializeField] private Vector3[] comboSwingRotationOffsets =
    {
        new Vector3(65f, 0f, -25f),
        new Vector3(65f, 0f, 25f),
        new Vector3(85f, 0f, -5f)
    }; // 기본 3단 휘두르기 방향

    private Quaternion idleLocalRotation; // 기본 로컬 회전값
    private Coroutine swingCoroutine; // 실행 중인 휘두르기 코루틴

    private void Awake() // 도구 회전값 초기화
    {
        if (toolHolder == null) // 도구 위치 연결 확인
        {
            toolHolder = transform; // 현재 오브젝트를 기본 대상으로 사용
        }

        idleLocalRotation = toolHolder.localRotation; // 기본 회전값 저장
    }

    private void OnValidate() // Inspector 값 검증
    {
        swingDuration = Mathf.Max(0.01f, swingDuration); // 휘두르기 시간 최소값 보정
        returnDuration = Mathf.Max(0.01f, returnDuration); // 복귀 시간 최소값 보정

        if (comboSwingRotationOffsets == null || comboSwingRotationOffsets.Length == 0) // 연속 공격 회전 배열 확인
        {
            comboSwingRotationOffsets = new[]
            {
                swingRotationOffset
            }; // 기본 회전값으로 최소 배열 생성
        }
    }

    public void PlaySwing() // 기존 단일 도구 휘두르기 시작
    {
        PlaySwing(0, 1f); // 첫 번째 연속 공격 방향과 기본 속도로 재생
    }

    public void PlaySwing(int comboStepIndex, float speedMultiplier) // 연속 공격 단계별 휘두르기 시작
    {
        if (!isActiveAndEnabled || toolHolder == null) // 실행 가능 상태 확인
        {
            return; // 연출 실행 중단
        }

        CancelSwing(); // 기존 휘두르기 연출과 회전 상태 정리

        int safeStepIndex = Mathf.Max(0, comboStepIndex); // 공격 단계 번호 음수 방지
        float safeSpeedMultiplier = Mathf.Max(0.1f, speedMultiplier); // 연출 속도 최소값 적용
        Vector3 selectedRotationOffset = GetRotationOffset(safeStepIndex); // 공격 단계별 회전 차이 조회
        swingCoroutine = StartCoroutine(
            SwingRoutine(selectedRotationOffset, safeSpeedMultiplier)); // 새로운 휘두르기 시작
    }

    public void CancelSwing() // 실행 중인 휘두르기 연출 즉시 취소
    {
        if (swingCoroutine != null) // 기존 연출 실행 여부 확인
        {
            StopCoroutine(swingCoroutine); // 실행 중인 휘두르기 중지
            swingCoroutine = null; // 코루틴 상태 초기화
        }

        if (toolHolder != null) // 도구 위치 존재 확인
        {
            toolHolder.localRotation = idleLocalRotation; // 기본 회전값 복구
        }
    }

    private Vector3 GetRotationOffset(int comboStepIndex) // 연속 공격 단계별 회전 차이 조회
    {
        if (comboSwingRotationOffsets == null || comboSwingRotationOffsets.Length == 0) // 연속 공격 회전 배열 확인
        {
            return swingRotationOffset; // 기본 회전 차이 반환
        }

        int selectedIndex = comboStepIndex % comboSwingRotationOffsets.Length; // 배열 범위 안의 반복 번호 계산
        return comboSwingRotationOffsets[selectedIndex]; // 선택된 회전 차이 반환
    }

    private IEnumerator SwingRoutine(
        Vector3 selectedRotationOffset,
        float speedMultiplier) // 지정 단계의 휘두르기 진행
    {
        Quaternion hitRotation =
            idleLocalRotation * Quaternion.Euler(selectedRotationOffset); // 타격 회전값 계산

        float adjustedSwingDuration = swingDuration / speedMultiplier; // 속도 배율 적용 타격 회전 시간 계산
        float adjustedReturnDuration = returnDuration / speedMultiplier; // 속도 배율 적용 복귀 시간 계산

        yield return RotateRoutine(
            idleLocalRotation,
            hitRotation,
            adjustedSwingDuration); // 타격 방향 회전 대기

        yield return RotateRoutine(
            hitRotation,
            idleLocalRotation,
            adjustedReturnDuration); // 기본 자세 복귀 대기

        toolHolder.localRotation = idleLocalRotation; // 최종 기본 회전값 적용
        swingCoroutine = null; // 코루틴 상태 초기화
    }

    private IEnumerator RotateRoutine(
        Quaternion startRotation,
        Quaternion endRotation,
        float duration) // 회전 보간 처리
    {
        float elapsedTime = 0f; // 진행 시간 초기화

        while (elapsedTime < duration) // 설정 시간 동안 반복
        {
            elapsedTime += Time.deltaTime; // 프레임 시간 누적
            float normalizedTime = Mathf.Clamp01(elapsedTime / duration); // 진행 비율 계산
            float smoothTime = Mathf.SmoothStep(0f, 1f, normalizedTime); // 부드러운 진행 비율 계산
            toolHolder.localRotation =
                Quaternion.Slerp(startRotation, endRotation, smoothTime); // 현재 회전값 적용
            yield return null; // 다음 프레임까지 대기
        }

        toolHolder.localRotation = endRotation; // 목표 회전값 확정
    }

    private void OnDisable() // 비활성화 상태 정리
    {
        CancelSwing(); // 실행 중인 연출과 도구 회전 상태 정리
    }
}
