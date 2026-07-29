using System.Collections; // 코루틴 기능
using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class SleepSystem : MonoBehaviour // 플레이어 수면 진행 관리
{
    [Header("References")] // 외부 참조 묶음
    [SerializeField] private DayNightCycle dayNightCycle; // 낮과 밤 시간 관리자
    [SerializeField] private PlayerHealth playerHealth; // 플레이어 체력
    [SerializeField] private PlayerHunger playerHunger; // 플레이어 허기
    [SerializeField] private PlayerThirst playerThirst; // 플레이어 갈증
    [SerializeField] private PlayerInteractor playerInteractor; // 플레이어 상호작용
    [SerializeField] private BuildPlacementController buildPlacementController; // 건축 배치 관리자
    [SerializeField] private CanvasGroup fadeCanvasGroup; // 수면 암전 화면

    [Header("Sleep")] // 수면 설정 묶음
    [SerializeField] private float wakeHour = 8f; // 수면 종료 시간
    [SerializeField] private float healthRestoreAmount = 30f; // 수면 체력 회복량
    [SerializeField] private float hungerCost = 15f; // 수면 허기 소비량
    [SerializeField] private float thirstCost = 20f; // 수면 갈증 소비량

    [Header("Fade")] // 암전 설정 묶음
    [SerializeField] private float fadeDuration = 0.75f; // 암전 전환 시간
    [SerializeField] private float blackScreenDuration = 0.25f; // 검은 화면 유지 시간

    [Header("Runtime")] // 실행 상태 묶음
    [SerializeField] private bool isSleeping; // 현재 수면 상태

    private float storedTimeScale = 1f; // 수면 전 시간 배율
    private bool wasInteractorEnabled; // 기존 상호작용 활성 상태
    private bool wasBuildControllerEnabled; // 기존 건축 기능 활성 상태

    public bool IsSleeping => isSleeping; // 현재 수면 상태 제공

    public string SleepPrompt // 현재 수면 안내 문구 제공
    {
        get
        {
            if (!enabled || dayNightCycle == null) // 수면 시스템 사용 가능 여부 확인
            {
                return "SLEEP UNAVAILABLE"; // 기능 사용 불가 표시
            }

            if (isSleeping) // 수면 진행 여부 확인
            {
                return "SLEEPING..."; // 수면 진행 문구
            }

            if (playerHealth.IsDead) // 플레이어 사망 여부 확인
            {
                return "CANNOT SLEEP"; // 사망 상태 수면 차단 문구
            }

            if (!dayNightCycle.IsNight) // 현재 야간 여부 확인
            {
                return "SLEEP AT NIGHT"; // 낮 시간 수면 차단 문구
            }

            if (playerHunger.CurrentHunger < hungerCost) // 허기 수치 확인
            {
                return "TOO HUNGRY TO SLEEP"; // 허기 부족 문구
            }

            if (playerThirst.CurrentThirst < thirstCost) // 갈증 수치 확인
            {
                return "TOO THIRSTY TO SLEEP"; // 갈증 부족 문구
            }

            return $"F - SLEEP UNTIL {wakeHour:00}:00"; // 수면 가능 문구
        }
    }

    private void Awake() // 수면 시스템 초기화
    {
        ClampSettings(); // 설정값 범위 보정

        bool hasMissingReference =
            dayNightCycle == null
            || playerHealth == null
            || playerHunger == null
            || playerThirst == null
            || playerInteractor == null
            || buildPlacementController == null
            || fadeCanvasGroup == null; // 필수 참조 누락 확인

        if (hasMissingReference) // 필수 참조 누락 여부 확인
        {
            Debug.LogError("SleepSystem의 필수 참조를 모두 연결해야 합니다.", this); // 참조 누락 오류 출력
            enabled = false; // 수면 시스템 비활성화
            return; // 초기화 중단
        }

        fadeCanvasGroup.alpha = 0f; // 시작 암전 화면 투명 처리
        fadeCanvasGroup.interactable = false; // 암전 화면 UI 조작 차단
        fadeCanvasGroup.blocksRaycasts = false; // 시작 UI 광선 차단 해제
        isSleeping = false; // 시작 수면 상태 해제
    }

    private void OnValidate() // Inspector 설정값 검증
    {
        ClampSettings(); // 설정값 범위 보정
    }

    public bool TrySleep() // 현재 조건으로 수면 시작 시도
    {
        if (!CanStartSleep()) // 전체 수면 조건 확인
        {
            return false; // 수면 시작 실패
        }

        StartCoroutine(SleepRoutine()); // 수면 처리 시작
        return true; // 수면 시작 성공
    }

    private bool CanStartSleep() // 수면 시작 가능 여부 확인
    {
        if (!enabled || isSleeping) // 시스템과 진행 상태 확인
        {
            return false; // 중복 수면 차단
        }

        if (playerHealth.IsDead) // 플레이어 사망 여부 확인
        {
            return false; // 사망 상태 수면 차단
        }

        if (!dayNightCycle.IsNight) // 현재 야간 여부 확인
        {
            return false; // 낮 시간 수면 차단
        }

        if (playerHunger.CurrentHunger < hungerCost) // 허기 수치 확인
        {
            return false; // 허기 부족 수면 차단
        }

        if (playerThirst.CurrentThirst < thirstCost) // 갈증 수치 확인
        {
            return false; // 갈증 부족 수면 차단
        }

        return true; // 모든 수면 조건 충족
    }

    private IEnumerator SleepRoutine() // 암전과 수면 결과 처리
    {
        isSleeping = true; // 수면 진행 상태 적용
        storedTimeScale = Time.timeScale; // 기존 시간 배율 저장
        wasInteractorEnabled = playerInteractor.enabled; // 기존 상호작용 상태 저장
        wasBuildControllerEnabled = buildPlacementController.enabled; // 기존 건축 상태 저장
        playerInteractor.enabled = false; // 수면 중 상호작용 차단
        buildPlacementController.enabled = false; // 수면 중 건축 입력 차단
        fadeCanvasGroup.blocksRaycasts = true; // 암전 화면 입력 차단
        Time.timeScale = 0f; // 수면 중 게임 진행 정지

        yield return FadeCanvas(1f); // 화면 검게 전환

        playerHunger.TryConsume(hungerCost); // 수면 허기 소비
        playerThirst.TryConsume(thirstCost); // 수면 갈증 소비
        dayNightCycle.AdvanceToHour(wakeHour); // 다음 아침 시간 적용
        playerHealth.Heal(healthRestoreAmount); // 수면 체력 회복

        yield return new WaitForSecondsRealtime(blackScreenDuration); // 검은 화면 잠시 유지
        yield return FadeCanvas(0f); // 화면 다시 표시

        RestoreGameplayState(); // 일반 플레이 상태 복구
    }

    private IEnumerator FadeCanvas(float targetAlpha) // 암전 화면 투명도 전환
    {
        float startingAlpha = fadeCanvasGroup.alpha; // 시작 투명도 저장
        float elapsedTime = 0f; // 전환 경과 시간 초기화

        while (elapsedTime < fadeDuration) // 전환 시간 진행 여부 확인
        {
            elapsedTime += Time.unscaledDeltaTime; // 시간 배율과 무관한 경과 시간 증가
            float progress = Mathf.Clamp01(elapsedTime / fadeDuration); // 전환 진행 비율 계산
            fadeCanvasGroup.alpha = Mathf.Lerp(startingAlpha, targetAlpha, progress); // 화면 투명도 보간
            yield return null; // 다음 프레임 대기
        }

        fadeCanvasGroup.alpha = targetAlpha; // 최종 투명도 고정
    }

    private void RestoreGameplayState() // 수면 후 입력과 시간 복구
    {
        Time.timeScale = storedTimeScale; // 기존 시간 배율 복구
        fadeCanvasGroup.alpha = 0f; // 암전 화면 완전 투명 처리
        fadeCanvasGroup.blocksRaycasts = false; // UI 광선 차단 해제
        playerInteractor.enabled = wasInteractorEnabled; // 기존 상호작용 상태 복구
        buildPlacementController.enabled = wasBuildControllerEnabled; // 기존 건축 상태 복구
        isSleeping = false; // 수면 상태 해제
    }

    private void OnDisable() // 비활성화 중 수면 상태 정리
    {
        if (!isSleeping) // 현재 수면 상태 확인
        {
            return; // 정리 불필요
        }

        StopAllCoroutines(); // 실행 중인 수면 처리 중단
        RestoreGameplayState(); // 시간과 입력 상태 복구
    }

    private void ClampSettings() // 수면 설정값 보정
    {
        wakeHour = Mathf.Clamp(Mathf.Round(wakeHour), 0f, 23f); // 기상 시간 정수 범위 적용
        healthRestoreAmount = Mathf.Max(0f, healthRestoreAmount); // 회복량 음수 방지
        hungerCost = Mathf.Max(0f, hungerCost); // 허기 비용 음수 방지
        thirstCost = Mathf.Max(0f, thirstCost); // 갈증 비용 음수 방지
        fadeDuration = Mathf.Max(0.01f, fadeDuration); // 암전 시간 최소값 적용
        blackScreenDuration = Mathf.Max(0f, blackScreenDuration); // 검은 화면 시간 음수 방지
    }
}