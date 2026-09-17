using UnityEngine; // Unity 기본 기능

// 83일차: 챔질 후 끌어올리기 미니게임
// 막대 위를 왕복하는 표시가 목표 구간 안에 있을 때 F를 누르면 성공, 밖이면 실수로 센다.
// 필요한 성공 횟수를 채우면 낚고, 실수가 쌓이거나 시간이 다 되면 놓친다.
[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class FishingMinigameController : MonoBehaviour
{
    [Header("References")] // 참조 묶음
    [Tooltip("같은 플레이어의 낚시 조작입니다.")]
    [SerializeField] private FishingController fishingController; // 낚시 조작

    [Tooltip("미니게임을 보여줄 HUD입니다.")]
    [SerializeField] private FishingMinigameUI minigameUI; // 미니게임 HUD

    [Header("Rules")] // 동작 규칙 묶음
    [Tooltip("새 목표 구간이 현재 표시 위치에서 최소 이만큼 떨어지게 합니다(막대 비율).")]
    [SerializeField, Range(0f, 0.5f)] private float minimumZoneJump = 0.22f; // 목표 이동 최소 거리

    [Tooltip("같은 입력이 겹쳐 두 번 처리되지 않도록 막는 시간(초)입니다.")]
    [SerializeField, Min(0f)] private float inputCooldown = 0.12f; // 입력 간격

    private bool isActive; // 진행 중 여부
    private FishData fish; // 현재 물고기
    private float markerPosition; // 표시 위치 (0~1)
    private float markerDirection = 1f; // 표시 이동 방향
    private float markerSpeed; // 표시 속도 (막대 길이/초)
    private float zoneCenter; // 목표 구간 중심 (0~1)
    private float zoneWidth; // 목표 구간 폭 (0~1)
    private int successCount; // 성공 횟수
    private int requiredSuccessCount; // 필요 성공 횟수
    private int missCount; // 실수 횟수
    private int maxMisses; // 최대 실수 횟수
    private float timeLimit; // 제한 시간
    private float endTime; // 종료 시각
    private float nextInputTime; // 다음 입력 가능 시각
    private float lastResultTime = -10f; // 마지막 입력 판정 시각
    private bool lastResultWasHit; // 마지막 입력 성공 여부

    public bool IsActive => isActive; // 진행 중 여부 제공
    public FishData Fish => fish; // 현재 물고기 제공
    public bool IsKnownFish => fish != null && fishingController != null && fishingController.Journal != null && fishingController.Journal.HasCaught(fish.FishId); // 잡아 본 물고기 여부 제공
    public float MarkerPosition => markerPosition; // 표시 위치 제공
    public float ZoneCenter => zoneCenter; // 목표 중심 제공
    public float ZoneWidth => zoneWidth; // 목표 폭 제공
    public int SuccessCount => successCount; // 성공 횟수 제공
    public int RequiredSuccessCount => requiredSuccessCount; // 필요 성공 횟수 제공
    public int MissCount => missCount; // 실수 횟수 제공
    public int MaxMisses => maxMisses; // 최대 실수 횟수 제공
    public float TimeRemaining01 => isActive && timeLimit > 0f ? Mathf.Clamp01((endTime - Time.time) / timeLimit) : 0f; // 남은 시간 비율 제공
    public float TimeSinceLastResult => Time.time - lastResultTime; // 마지막 판정 후 경과 시간 제공
    public bool LastResultWasHit => lastResultWasHit; // 마지막 판정 결과 제공
    public bool IsMarkerInZone => Mathf.Abs(markerPosition - zoneCenter) <= zoneWidth * 0.5f; // 표시가 목표 안인지 제공

    private void Awake() // 참조 준비
    {
        if (fishingController == null) // 참조 확인
        {
            fishingController = GetComponent<FishingController>(); // 같은 플레이어에서 검색
        }

        if (minigameUI == null) // HUD 참조 확인
        {
            minigameUI = FindFirstObjectByType<FishingMinigameUI>(FindObjectsInactive.Include); // Scene에서 검색
        }
    }

    private void OnEnable() // 낚시 이벤트 연결
    {
        if (fishingController == null) // 참조 확인
        {
            return; // 연결 생략
        }

        fishingController.BiteHooked += HandleBiteHooked; // 챔질 구독
        fishingController.ReelInput += HandleReelInput; // F키 구독
        fishingController.FishingEnded += HandleFishingEnded; // 종료 구독
    }

    private void OnDisable() // 낚시 이벤트 해제
    {
        if (fishingController != null) // 참조 확인
        {
            fishingController.BiteHooked -= HandleBiteHooked; // 챔질 해제
            fishingController.ReelInput -= HandleReelInput; // F키 해제
            fishingController.FishingEnded -= HandleFishingEnded; // 종료 해제
        }

        StopSession(); // 진행 중인 미니게임 정리
    }

    private void Update() // 표시 이동과 시간 확인
    {
        if (!isActive) // 진행 확인
        {
            return; // 처리 생략
        }

        markerPosition += markerDirection * markerSpeed * Time.deltaTime; // 표시 이동

        if (markerPosition >= 1f) // 오른쪽 끝 확인
        {
            markerPosition = 2f - markerPosition; // 되튀기
            markerDirection = -1f; // 방향 반전
        }
        else if (markerPosition <= 0f) // 왼쪽 끝 확인
        {
            markerPosition = -markerPosition; // 되튀기
            markerDirection = 1f; // 방향 반전
        }

        markerPosition = Mathf.Clamp01(markerPosition); // 범위 제한

        if (Time.time >= endTime) // 제한 시간 확인
        {
            Finish(false, "IT GOT AWAY..."); // 시간 초과
        }
    }

    public void Press() // F키 입력 판정 (ReelInput 또는 테스트에서 호출)
    {
        if (!isActive || Time.time < nextInputTime) // 입력 가능 확인
        {
            return; // 입력 무시
        }

        nextInputTime = Time.time + inputCooldown; // 다음 입력 시각
        lastResultTime = Time.time; // 판정 시각
        lastResultWasHit = IsMarkerInZone; // 판정 결과

        if (lastResultWasHit) // 성공 확인
        {
            successCount++; // 성공 증가
            fishingController.SetReelProgress((float)successCount / requiredSuccessCount); // 찌가 다가옴

            if (successCount >= requiredSuccessCount) // 완료 확인
            {
                Finish(true, null); // 낚기 성공
                return; // 처리 종료
            }

            markerSpeed *= fishingController.Rules != null ? fishingController.Rules.SpeedUpPerSuccess : 1f; // 점점 빨라짐
            MoveZone(); // 목표 이동
            return; // 처리 종료
        }

        missCount++; // 실수 증가

        if (missCount >= maxMisses) // 줄 끊김 확인
        {
            Finish(false, "LINE SNAPPED!"); // 낚기 실패
        }
    }

    public void DebugWin() // 테스트 메뉴 전용 : 바로 낚기 성공
    {
        if (isActive) // 진행 확인
        {
            successCount = requiredSuccessCount; // 성공 채움
            fishingController.SetReelProgress(1f); // 진행도 완료
            Finish(true, null); // 성공 처리
        }
    }

    private void HandleBiteHooked(FishingController controller) // 챔질 시 미니게임 시작
    {
        FishingRulesData rules = controller.Rules; // 낚시 규칙
        fish = controller.HookedFish; // 물고기

        if (fish == null || rules == null) // 필수 데이터 확인
        {
            controller.CompleteReeling(false, null); // 바로 놓침 처리
            return; // 시작 중단
        }

        float difficulty = fish.Difficulty; // 난이도
        float rodBonus = controller.ActiveRod != null ? controller.ActiveRod.InputTimeBonus : 0f; // 낚싯대 여유 시간
        requiredSuccessCount = fish.RequiredSuccessCount; // 필요 성공 횟수
        maxMisses = rules.MinigameMaxMisses; // 최대 실수 횟수
        markerSpeed = rules.GetMarkerSpeed(difficulty); // 표시 속도
        zoneWidth = rules.GetZoneWidth(difficulty); // 목표 폭
        timeLimit = rules.GetTimeLimit(requiredSuccessCount, rodBonus); // 제한 시간
        endTime = Time.time + timeLimit; // 종료 시각
        successCount = 0; // 성공 초기화
        missCount = 0; // 실수 초기화
        markerPosition = 0f; // 왼쪽 끝에서 시작
        markerDirection = 1f; // 오른쪽으로 이동
        nextInputTime = Time.time + inputCooldown; // 챔질 입력과 겹침 방지
        lastResultTime = -10f; // 판정 표시 초기화
        isActive = true; // 진행 시작
        MoveZone(); // 첫 목표

        if (minigameUI != null) // HUD 확인
        {
            minigameUI.Show(this); // HUD 표시
        }
    }

    private void HandleReelInput(FishingController controller) // F키 입력
    {
        Press(); // 판정
    }

    private void HandleFishingEnded(FishingController controller, string message) // 외부 취소 (이동·피격 등)
    {
        StopSession(); // 미니게임 정리
    }

    private void MoveZone() // 새 목표 구간 위치
    {
        float half = zoneWidth * 0.5f; // 절반 폭
        float center = Random.Range(half, 1f - half); // 기본 위치

        for (int attempt = 0; attempt < 6 && Mathf.Abs(center - markerPosition) < minimumZoneJump; attempt++) // 표시와 너무 가까우면 다시 뽑기
        {
            center = Random.Range(half, 1f - half); // 재추첨
        }

        zoneCenter = center; // 위치 적용
    }

    private void Finish(bool caught, string resultText) // 미니게임 종료와 결과 전달
    {
        if (!isActive) // 진행 확인
        {
            return; // 처리 생략
        }

        StopSession(); // 정리
        fishingController.CompleteReeling(caught, resultText); // 결과 전달 (보상 지급)
    }

    private void StopSession() // 진행 상태와 HUD 정리
    {
        isActive = false; // 진행 종료

        if (minigameUI != null) // HUD 확인
        {
            minigameUI.Hide(); // HUD 숨김
        }
    }
}
