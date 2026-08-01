using System.Collections; // 코루틴 기능
using UnityEngine; // Unity 기본 기능

public sealed class ToolSwingAnimation : MonoBehaviour // 도구 휘두르기 연출
{
    [Header("References")] // 참조 설정 묶음
    [Tooltip("회전할 도구 보관 위치.")]
    [SerializeField] private Transform toolHolder; // 회전할 도구 보관 위치

    [Header("Swing")] // 휘두르기 설정 묶음
    [Tooltip("타격 회전 차이.")]
    [SerializeField] private Vector3 swingRotationOffset = new Vector3(65f, 0f, -25f); // 타격 회전 차이
    [Tooltip("타격 방향 회전 시간.")]
    [SerializeField] private float swingDuration = 0.12f; // 타격 방향 회전 시간
    [Tooltip("기본 자세 복귀 시간.")]
    [SerializeField] private float returnDuration = 0.18f; // 기본 자세 복귀 시간

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
    }

    public void PlaySwing() // 도구 휘두르기 시작
    {
        if (!isActiveAndEnabled || toolHolder == null) // 실행 가능 상태 확인
        {
            return; // 연출 실행 중단
        }

        if (swingCoroutine != null) // 기존 연출 실행 여부 확인
        {
            StopCoroutine(swingCoroutine); // 기존 휘두르기 중지
            toolHolder.localRotation = idleLocalRotation; // 기본 회전값 복구
        }

        swingCoroutine = StartCoroutine(SwingRoutine()); // 새로운 휘두르기 시작
    }

    private IEnumerator SwingRoutine() // 도구 휘두르기 진행
    {
        Quaternion hitRotation = idleLocalRotation * Quaternion.Euler(swingRotationOffset); // 타격 회전값 계산

        yield return RotateRoutine(idleLocalRotation, hitRotation, swingDuration); // 타격 방향 회전 대기
        yield return RotateRoutine(hitRotation, idleLocalRotation, returnDuration); // 기본 자세 복귀 대기

        toolHolder.localRotation = idleLocalRotation; // 최종 기본 회전값 적용
        swingCoroutine = null; // 코루틴 상태 초기화
    }

    private IEnumerator RotateRoutine(Quaternion startRotation, Quaternion endRotation, float duration) // 회전 보간 처리
    {
        float elapsedTime = 0f; // 진행 시간 초기화

        while (elapsedTime < duration) // 설정 시간 동안 반복
        {
            elapsedTime += Time.deltaTime; // 프레임 시간 누적
            float normalizedTime = Mathf.Clamp01(elapsedTime / duration); // 진행 비율 계산
            float smoothTime = Mathf.SmoothStep(0f, 1f, normalizedTime); // 부드러운 진행 비율 계산
            toolHolder.localRotation = Quaternion.Slerp(startRotation, endRotation, smoothTime); // 현재 회전값 적용
            yield return null; // 다음 프레임까지 대기
        }

        toolHolder.localRotation = endRotation; // 목표 회전값 확정
    }

    private void OnDisable() // 비활성화 상태 정리
    {
        if (swingCoroutine != null) // 실행 중인 연출 확인
        {
            StopCoroutine(swingCoroutine); // 실행 중인 코루틴 중지
            swingCoroutine = null; // 코루틴 상태 초기화
        }

        if (toolHolder != null) // 도구 위치 존재 확인
        {
            toolHolder.localRotation = idleLocalRotation; // 기본 회전값 복구
        }
    }
}