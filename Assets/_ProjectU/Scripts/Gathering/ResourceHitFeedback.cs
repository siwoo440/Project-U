using System.Collections; // 코루틴 기능
using UnityEngine; // Unity 기본 기능

public sealed class ResourceHitFeedback : MonoBehaviour // 자원 타격 반응 연출
{
    [Header("References")] // 참조 설정 묶음
    [SerializeField] private Transform visualRoot; // 크기를 변경할 자원 외형

    [Header("Feedback")] // 타격 반응 설정 묶음
    [SerializeField] private float compressedScaleMultiplier = 0.9f; // 눌린 상태 크기 배율
    [SerializeField] private float compressDuration = 0.08f; // 눌리는 시간
    [SerializeField] private float recoverDuration = 0.12f; // 원래 크기 복귀 시간

    private Vector3 idleLocalScale; // 기본 로컬 크기
    private Coroutine feedbackCoroutine; // 실행 중인 반응 코루틴

    private void Awake() // 자원 외형 초기화
    {
        if (visualRoot == null) // 외형 참조 확인
        {
            visualRoot = transform; // 현재 오브젝트를 기본 외형으로 사용
        }

        idleLocalScale = visualRoot.localScale; // 기본 크기 저장
    }

    private void OnValidate() // Inspector 값 검증
    {
        compressedScaleMultiplier = Mathf.Clamp(compressedScaleMultiplier, 0.5f, 1f); // 크기 배율 범위 보정
        compressDuration = Mathf.Max(0.01f, compressDuration); // 눌림 시간 최소값 보정
        recoverDuration = Mathf.Max(0.01f, recoverDuration); // 복귀 시간 최소값 보정
    }

    public void PlayHit() // 자원 타격 반응 시작
    {
        if (!isActiveAndEnabled || visualRoot == null) // 실행 가능 상태 확인
        {
            return; // 반응 실행 중단
        }

        if (feedbackCoroutine != null) // 기존 반응 실행 여부 확인
        {
            StopCoroutine(feedbackCoroutine); // 기존 반응 중지
            visualRoot.localScale = idleLocalScale; // 기본 크기 복구
        }

        feedbackCoroutine = StartCoroutine(FeedbackRoutine()); // 새로운 타격 반응 시작
    }

    private IEnumerator FeedbackRoutine() // 자원 타격 반응 진행
    {
        Vector3 compressedScale = idleLocalScale * compressedScaleMultiplier; // 눌린 크기 계산

        yield return ScaleRoutine(idleLocalScale, compressedScale, compressDuration); // 눌리는 연출 대기
        yield return ScaleRoutine(compressedScale, idleLocalScale, recoverDuration); // 복귀 연출 대기

        visualRoot.localScale = idleLocalScale; // 최종 기본 크기 적용
        feedbackCoroutine = null; // 코루틴 상태 초기화
    }

    private IEnumerator ScaleRoutine(Vector3 startScale, Vector3 endScale, float duration) // 크기 보간 처리
    {
        float elapsedTime = 0f; // 진행 시간 초기화

        while (elapsedTime < duration) // 설정 시간 동안 반복
        {
            elapsedTime += Time.deltaTime; // 프레임 시간 누적
            float normalizedTime = Mathf.Clamp01(elapsedTime / duration); // 진행 비율 계산
            float smoothTime = Mathf.SmoothStep(0f, 1f, normalizedTime); // 부드러운 진행 비율 계산
            visualRoot.localScale = Vector3.Lerp(startScale, endScale, smoothTime); // 현재 크기 적용
            yield return null; // 다음 프레임까지 대기
        }

        visualRoot.localScale = endScale; // 목표 크기 확정
    }

    private void OnDisable() // 비활성화 상태 정리
    {
        if (feedbackCoroutine != null) // 실행 중인 반응 확인
        {
            StopCoroutine(feedbackCoroutine); // 실행 중인 코루틴 중지
            feedbackCoroutine = null; // 코루틴 상태 초기화
        }

        if (visualRoot != null) // 외형 참조 존재 확인
        {
            visualRoot.localScale = idleLocalScale; // 기본 크기 복구
        }
    }
}