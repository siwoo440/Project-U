using System; // 이벤트 기능
using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class FishingController : MonoBehaviour // 플레이어 낚시 시작·대기·입질·취소 흐름
{
    private const int LineSegments = 14; // 낚싯줄 점 개수

    [Header("References")] // 참조 묶음
    [Tooltip("손에 든 낚싯대와 미끼를 확인할 인벤토리입니다.")]
    [SerializeField] private PlayerInventory playerInventory; // 플레이어 인벤토리

    [Tooltip("던질 때 스태미나를 소비할 대상입니다.")]
    [SerializeField] private PlayerStamina playerStamina; // 플레이어 스태미나

    [Tooltip("피격·사망 시 낚시를 취소하기 위한 체력입니다.")]
    [SerializeField] private PlayerHealth playerHealth; // 플레이어 체력

    [Tooltip("회피 중 낚시를 취소하기 위한 이동 관리자입니다.")]
    [SerializeField] private PlayerMovement playerMovement; // 플레이어 이동

    [Tooltip("건축 모드 중 낚시를 막기 위한 건축 관리자입니다.")]
    [SerializeField] private BuildPlacementController buildPlacementController; // 건축 관리자

    [Tooltip("던질 방향을 정할 Camera Transform입니다.")]
    [SerializeField] private Transform viewTransform; // 시선 기준

    [Tooltip("낚시 공통 규칙입니다.")]
    [SerializeField] private FishingRulesData rules; // 낚시 규칙

    [Header("Visual")] // 외형 묶음
    [Tooltip("찌를 던질 지점 표시와 F키 입력을 받는 대상입니다.")]
    [SerializeField] private FishingCastTarget castTarget; // 던질 지점 대상

    [Tooltip("던질 지점에 보이는 고리 표시입니다.")]
    [SerializeField] private GameObject castMarker; // 던질 지점 표시

    [Tooltip("낚싯대 끝 위치입니다. 비어 있으면 플레이어 앞 위쪽을 사용합니다.")]
    [SerializeField] private Transform rodTip; // 낚싯대 끝

    [Tooltip("낚싯줄 Line Renderer입니다.")]
    [SerializeField] private LineRenderer fishingLine; // 낚싯줄

    [Tooltip("물에 띄울 찌 Prefab입니다.")]
    [SerializeField] private GameObject bobberPrefab; // 찌 Prefab

    [Header("Rules")] // 동작 규칙 묶음
    [Tooltip("UI가 열려 커서가 풀리면 낚시를 취소합니다.")]
    [SerializeField] private bool cancelWhenCursorUnlocked = true; // 커서 해제 취소

    [Tooltip("던질 지점 재계산 간격(초)입니다.")]
    [SerializeField, Min(0f)] private float refreshInterval = 0.08f; // 재계산 간격

    [Tooltip("찌가 날아가는 최고 높이(m)입니다.")]
    [SerializeField, Min(0f)] private float castArcHeight = 1.4f; // 던지기 호 높이

    [Tooltip("낚시 중 낚싯대를 찌 방향으로 기울이는 각도(수직 기준 도)입니다.")]
    [SerializeField, Range(0f, 90f)] private float rodTiltAngle = 60f; // 낚싯대 기울기

    private readonly Vector3[] linePoints = new Vector3[LineSegments]; // 낚싯줄 점
    private FishingState state = FishingState.Idle; // 현재 상태
    private FishingSpot targetSpot; // 대기 전 대상 낚시터
    private FishingSpot activeSpot; // 낚시 중인 낚시터
    private FishingRodData activeRod; // 사용 중인 낚싯대
    private Vector3 targetPoint; // 던질 물 위 지점
    private Vector3 castFrom; // 찌 출발 위치
    private Vector3 bobberRestPoint; // 찌가 떠 있는 위치
    private Vector3 castStartPlayerPosition; // 던질 때 플레이어 위치
    private float stateStartTime; // 현재 상태 시작 시각
    private float biteTime; // 입질 예정 시각
    private float biteEndTime; // 챔질 가능 종료 시각
    private float nextRefreshTime; // 다음 재계산 시각
    private float lastQueryTime = -10f; // 마지막 대상 요청 시각
    private ItemData lastSelectedItem; // 마지막 선택 아이템
    private bool usingBait; // 이번 던지기 미끼 사용 여부
    private GameObject bobber; // 현재 찌
    private Quaternion rodBaseLocalRotation; // 기울이기 전 낚싯대 회전
    private bool rodTilted; // 낚싯대 기울임 여부
    private string prompt = string.Empty; // 현재 안내 문구
    private int promptKey = int.MinValue; // 안내 문구 상태 키

    public static FishingController Local { get; private set; } // 현재 플레이어 낚시
    public static bool IsLocalBusy => Local != null && Local.IsBusy; // 플레이어 낚시 중 여부
    public bool IsBusy => state != FishingState.Idle; // 낚시 중 여부
    public FishingState State => state; // 현재 상태 제공
    public FishingSpot ActiveSpot => activeSpot; // 낚시 중인 낚시터 제공
    public FishingRodData ActiveRod => activeRod; // 사용 중인 낚싯대 제공
    public bool IsUsingBait => usingBait; // 미끼 사용 여부 제공
    public FishingRulesData Rules => rules; // 규칙 제공
    public PlayerInventory Inventory => playerInventory; // 인벤토리 제공
    public Vector3 BobberPosition => bobber != null ? bobber.transform.position : targetPoint; // 찌 위치 제공
    public string Prompt => prompt; // 안내 문구 제공

    public event Action<FishingController> BiteHooked; // 챔질 성공 알림 (83일차 미니게임 시작점)
    public event Action<FishingController, string> FishingEnded; // 낚시 종료 알림

    private void Awake() // 참조 준비
    {
        Local = this; // 현재 플레이어 등록

        if (playerInventory == null) { playerInventory = GetComponent<PlayerInventory>(); } // 인벤토리 자동 검색
        if (playerStamina == null) { playerStamina = GetComponent<PlayerStamina>(); } // 스태미나 자동 검색
        if (playerHealth == null) { playerHealth = GetComponent<PlayerHealth>(); } // 체력 자동 검색
        if (playerMovement == null) { playerMovement = GetComponent<PlayerMovement>(); } // 이동 자동 검색
        if (buildPlacementController == null) { buildPlacementController = GetComponent<BuildPlacementController>(); } // 건축 관리자 자동 검색

        if (viewTransform == null && Camera.main != null) // 시선 기준 누락 확인
        {
            viewTransform = Camera.main.transform; // 기본 Camera 사용
        }

        if (castTarget != null) // 대상 확인
        {
            castTarget.Bind(this); // 대상 연결
        }

        SetMarkerVisible(false); // 표시 숨김
        SetLineVisible(false); // 낚싯줄 숨김
        RefreshPrompt(); // 안내 문구 준비
    }

    private void OnEnable() // 이벤트 연결
    {
        if (playerHealth != null) // 체력 확인
        {
            playerHealth.CombatDamaged += HandleDamaged; // 전투 피격 구독 (굶주림·온도 피해는 제외)
        }
    }

    private void OnDisable() // 이벤트 해제와 정리
    {
        if (playerHealth != null) // 체력 확인
        {
            playerHealth.CombatDamaged -= HandleDamaged; // 전투 피격 해제
        }

        EndFishing(null); // 조용히 정리
    }

    private void OnDestroy() // 등록 해제
    {
        if (Local == this) // 현재 플레이어 확인
        {
            Local = null; // 등록 해제
        }
    }

    public InteractableBase FindTargetInteractable() // PlayerInteractor에 낚시 대상 제공
    {
        lastQueryTime = Time.time; // 요청 시각 기록

        if (castTarget == null || rules == null) // 필수 참조 확인
        {
            return null; // 대상 없음
        }

        if (IsBusy) // 낚시 중 확인
        {
            RefreshPrompt(); // 상태 문구 갱신
            return castTarget; // 낚시 대상 유지
        }

        ItemData selected = SelectedItem(); // 손에 든 아이템

        if (Time.time >= nextRefreshTime || selected != lastSelectedItem) // 재계산 시점 확인
        {
            lastSelectedItem = selected; // 선택 기록
            nextRefreshTime = Time.time + refreshInterval; // 다음 시각 기록
            RefreshCastTarget(selected); // 던질 지점 계산
        }

        RefreshPrompt(); // 안내 문구 갱신
        return targetSpot != null ? castTarget : null; // 대상 반환
    }

    public void HandleInteract() // F키 입력 처리
    {
        switch (state) // 상태별 처리
        {
            case FishingState.Idle: // 대기 중이 아님
                TryCast(); // 던지기 시도
                break;

            case FishingState.Waiting: // 입질 대기
                EndFishing("REELED IN"); // 빈 찌 회수
                break;

            case FishingState.Bite: // 입질
                Hook(); // 챔질
                break;
        }
    }

    public bool TryCast() // 찌 던지기
    {
        if (IsBusy || rules == null || !CanFishNow()) // 시작 조건 확인
        {
            return false; // 던지기 실패
        }

        ItemData selected = SelectedItem(); // 손에 든 아이템
        RefreshCastTarget(selected); // 최신 지점 계산

        if (targetSpot == null || !rules.TryGetRod(selected, out FishingRodData rod)) // 물과 낚싯대 확인
        {
            return false; // 던지기 실패
        }

        if (playerStamina != null && rules.CastStaminaCost > 0f && !playerStamina.TryConsume(rules.CastStaminaCost)) // 스태미나 소비
        {
            Popup("TOO TIRED", new Color(1f, 0.5f, 0.4f, 1f), 2f); // 스태미나 부족 알림
            return false; // 던지기 실패
        }

        activeRod = rod; // 낚싯대 기록
        activeSpot = targetSpot; // 낚시터 기록
        usingBait = rules.BaitItem != null && playerInventory.HasItem(rules.BaitItem, 1); // 미끼 여부
        castStartPlayerPosition = transform.position; // 시작 위치 기록
        castFrom = GetRodTipPosition(); // 찌 출발 위치
        bobberRestPoint = targetPoint; // 찌 도착 위치

        if (bobber == null && bobberPrefab != null) // 찌 생성 확인
        {
            bobber = Instantiate(bobberPrefab); // 찌 생성
            bobber.name = "FishingBobber"; // 이름 지정
        }

        if (bobber != null) // 찌 확인
        {
            bobber.transform.position = castFrom; // 출발 위치
            bobber.SetActive(true); // 표시
        }

        SetMarkerVisible(false); // 지점 표시 숨김
        SetLineVisible(true); // 낚싯줄 표시
        SetState(FishingState.Casting); // 던지기 상태
        return true; // 던지기 성공
    }

    public void CompleteReeling(bool caught, string resultText) // 83일차 미니게임 결과 반영
    {
        if (state != FishingState.Reeling) // 끌어올리는 중 확인
        {
            return; // 처리 생략
        }

        EndFishing(string.IsNullOrEmpty(resultText) ? (caught ? "CAUGHT!" : "IT GOT AWAY...") : resultText); // 낚시 종료
    }

    public void Cancel(string reason) // 외부 요청 취소
    {
        if (IsBusy) // 낚시 중 확인
        {
            EndFishing(reason); // 종료
        }
    }

    private void Update() // 상태 진행과 취소 조건 확인
    {
        if (!IsBusy) // 대기 상태 확인
        {
            if (castMarker != null && castMarker.activeSelf && Time.time - lastQueryTime > 0.15f) // 요청이 끊긴 표시 확인
            {
                SetMarkerVisible(false); // 표시 숨김
                targetSpot = null; // 대상 초기화
            }

            return; // 처리 종료
        }

        string cancelReason = GetCancelReason(); // 취소 사유 확인

        if (cancelReason != null) // 취소 확인
        {
            EndFishing(cancelReason); // 낚시 취소
            return; // 처리 종료
        }

        float elapsed = Time.time - stateStartTime; // 상태 경과 시간

        switch (state) // 상태별 진행
        {
            case FishingState.Casting: // 던지는 중
                UpdateCasting(elapsed); // 찌 비행
                break;

            case FishingState.Waiting: // 입질 대기
                SetBobberHeight(Mathf.Sin(Time.time * 2.2f) * 0.015f); // 잔잔한 흔들림

                if (Time.time >= biteTime) // 입질 시각 확인
                {
                    StartBite(); // 입질 시작
                }

                break;

            case FishingState.Bite: // 입질 중
                SetBobberHeight(-0.04f - Mathf.Abs(Mathf.Sin(Time.time * 18f)) * 0.06f); // 찌가 잠겼다 떠오름

                if (Time.time >= biteEndTime) // 챔질 시간 초과 확인
                {
                    Popup("IT GOT AWAY...", new Color(0.8f, 0.85f, 0.9f, 1f), 2f); // 놓침 알림
                    ScheduleBite(); // 다시 대기
                    SetState(FishingState.Waiting); // 대기 상태
                }

                break;
        }
    }

    private void LateUpdate() // 애니메이션 이후 낚싯대 자세와 낚싯줄 갱신
    {
        UpdateRodTilt(); // 낚싯대 기울이기

        if (IsBusy) // 낚시 중 확인
        {
            UpdateLine(); // 낚싯줄 갱신
        }
    }

    private void UpdateRodTilt() // 낚시 중 낚싯대를 찌 쪽으로 기울임
    {
        Transform rod = rodTip != null ? rodTip.parent : null; // 손에 든 낚싯대

        if (rod == null) // 낚싯대 확인
        {
            return; // 처리 생략
        }

        if (!IsBusy || !rod.gameObject.activeInHierarchy) // 낚시 종료 확인
        {
            if (rodTilted) // 기울임 확인
            {
                rod.localRotation = rodBaseLocalRotation; // 원래 자세 복구
                rodTilted = false; // 기울임 해제
            }

            return; // 처리 종료
        }

        if (rodTilted) // 이전 프레임 기울임 확인
        {
            rod.localRotation = rodBaseLocalRotation; // 기준 자세에서 다시 계산
        }
        else
        {
            rodBaseLocalRotation = rod.localRotation; // 기준 자세 저장
            rodTilted = true; // 기울임 시작
        }

        Vector3 axis = rodTip.position - rod.position; // 현재 낚싯대 방향
        Vector3 forward = bobberRestPoint - rod.position; // 찌 방향
        forward.y = 0f; // 수평 방향

        if (axis.sqrMagnitude < 0.0001f || forward.sqrMagnitude < 0.0001f) // 방향 확인
        {
            return; // 처리 생략
        }

        Vector3 desired = Vector3.Slerp(Vector3.up, forward.normalized, rodTiltAngle / 90f); // 목표 방향
        rod.rotation = Quaternion.FromToRotation(axis, desired) * rod.rotation; // 기울임 적용
    }

    private void UpdateCasting(float elapsed) // 찌 비행
    {
        float duration = rules.CastDuration; // 비행 시간
        float t = Mathf.Clamp01(elapsed / duration); // 진행 비율

        if (bobber != null) // 찌 확인
        {
            Vector3 position = Vector3.Lerp(castFrom, bobberRestPoint, t); // 직선 위치
            position.y += castArcHeight * 4f * t * (1f - t); // 포물선 높이
            bobber.transform.position = position; // 위치 적용
        }

        if (t >= 1f) // 도착 확인
        {
            Popup("~", new Color(0.7f, 0.9f, 1f, 1f), 2.4f, bobberRestPoint); // 물 튀김 표시
            ScheduleBite(); // 입질 예약
            SetState(FishingState.Waiting); // 대기 상태
        }
    }

    private void ScheduleBite() // 입질 시각 예약
    {
        float wait = UnityEngine.Random.Range(rules.MinimumBiteWait, rules.MaximumBiteWait); // 기본 대기
        wait *= activeRod != null ? activeRod.BiteTimeMultiplier : 1f; // 낚싯대 보정
        wait *= usingBait ? rules.BaitBiteTimeMultiplier : 1f; // 미끼 보정
        biteTime = Time.time + wait; // 입질 시각
    }

    private void StartBite() // 입질 시작
    {
        biteEndTime = Time.time + rules.BiteWindow; // 챔질 가능 종료 시각
        Popup("!", new Color(1f, 0.62f, 0.2f, 1f), 5f, bobberRestPoint + Vector3.up * 0.4f); // 입질 표시
        SetState(FishingState.Bite); // 입질 상태
    }

    private void Hook() // 챔질
    {
        if (usingBait && rules.BaitItem != null) // 미끼 사용 확인
        {
            playerInventory.RemoveItem(rules.BaitItem, 1); // 물고기가 문 미끼 1개 소비
        }

        if (BiteHooked != null) // 83일차 미니게임 연결 확인
        {
            SetState(FishingState.Reeling); // 끌어올리기 상태
            BiteHooked.Invoke(this); // 미니게임 시작
            return; // 처리 종료
        }

        EndFishing("IT GOT AWAY..."); // 미니게임이 없으면 놓침 처리
    }

    private string GetCancelReason() // 낚시 취소 사유 계산 (없으면 null)
    {
        if (playerHealth != null && playerHealth.IsDead) // 사망 확인
        {
            return string.Empty; // 조용히 취소
        }

        if (buildPlacementController != null && buildPlacementController.IsBuildMode) // 건축 모드 확인
        {
            return string.Empty; // 조용히 취소
        }

        if (cancelWhenCursorUnlocked && Cursor.lockState != CursorLockMode.Locked) // UI 열림 확인
        {
            return string.Empty; // 조용히 취소
        }

        if (playerMovement != null && playerMovement.IsDodging) // 회피 확인
        {
            return "LINE CUT"; // 취소 알림
        }

        Vector3 moved = transform.position - castStartPlayerPosition; // 이동량
        moved.y = 0f; // 수평만 사용

        if (moved.sqrMagnitude > rules.CancelMoveDistance * rules.CancelMoveDistance) // 이동 확인
        {
            return "LINE CUT"; // 취소 알림
        }

        ItemData selected = SelectedItem(); // 손에 든 아이템

        if (activeRod == null || selected != activeRod.RodItem) // 낚싯대 해제 확인
        {
            return string.Empty; // 조용히 취소
        }

        return null; // 계속 진행
    }

    private void HandleDamaged(float damage) // 피격 시 취소
    {
        if (IsBusy && damage > 0f) // 낚시 중 피격 확인
        {
            EndFishing("LINE CUT"); // 낚시 취소
        }
    }

    private void EndFishing(string message) // 낚시 종료와 정리 (message가 null·빈 문자열이면 알림 없음)
    {
        if (!IsBusy) // 이미 종료 확인
        {
            return; // 처리 생략
        }

        if (!string.IsNullOrEmpty(message)) // 알림 확인
        {
            Popup(message, new Color(0.85f, 0.9f, 0.95f, 1f), 2.2f, BobberPosition + Vector3.up * 0.3f); // 종료 알림
        }

        if (bobber != null) // 찌 확인
        {
            bobber.SetActive(false); // 찌 숨김 (다음에 재사용)
        }

        SetLineVisible(false); // 낚싯줄 숨김
        activeSpot = null; // 낚시터 초기화
        activeRod = null; // 낚싯대 초기화
        SetState(FishingState.Idle); // 대기 상태
        UpdateRodTilt(); // 낚싯대 자세 복구
        nextRefreshTime = 0f; // 바로 재계산
        FishingEnded?.Invoke(this, message ?? string.Empty); // 종료 알림
    }

    private void RefreshCastTarget(ItemData selected) // 던질 지점 계산
    {
        targetSpot = null; // 기본 결과

        if (!rules.TryGetRod(selected, out FishingRodData rod) || !CanFishNow()) // 낚싯대와 조건 확인
        {
            SetMarkerVisible(false); // 표시 숨김
            return; // 계산 종료
        }

        Vector3 forward = viewTransform != null ? viewTransform.forward : transform.forward; // 시선 방향
        forward.y = 0f; // 수평 방향

        if (forward.sqrMagnitude < 0.0001f) // 방향 확인
        {
            forward = transform.forward; // 캐릭터 방향
            forward.y = 0f; // 수평 방향
        }

        Vector3 point = transform.position + forward.normalized * rod.CastDistance; // 던질 지점

        if (!FishingSpot.TryFind(point, out FishingSpot spot)) // 물 확인
        {
            SetMarkerVisible(false); // 표시 숨김
            return; // 계산 종료
        }

        targetSpot = spot; // 낚시터 저장
        targetPoint = spot.GetSurfacePoint(point); // 물 표면 지점

        if (castMarker != null) // 표시 확인
        {
            castMarker.transform.position = targetPoint + Vector3.up * 0.02f; // 표시 위치
        }

        SetMarkerVisible(true); // 표시
    }

    private bool CanFishNow() // 낚시를 시작할 수 있는 상태인지
    {
        if (playerHealth != null && playerHealth.IsDead) // 사망 확인
        {
            return false; // 불가
        }

        if (buildPlacementController != null && buildPlacementController.BlocksGameplayInput) // 건축 확인
        {
            return false; // 불가
        }

        return playerMovement == null || !playerMovement.IsDodging; // 회피 확인 결과 반환
    }

    private ItemData SelectedItem() // 손에 든 아이템
    {
        return playerInventory != null ? playerInventory.SelectedHotbarItem : null; // 아이템 반환
    }

    private void RefreshPrompt() // 상태별 안내 문구 갱신
    {
        int bait = rules != null && rules.BaitItem != null && playerInventory != null ? playerInventory.GetItemQuantity(rules.BaitItem) : 0; // 미끼 수량
        int key = (int)state * 100000 + bait; // 상태 키

        if (key == promptKey) // 같은 상태 확인
        {
            return; // 문자열 재생성 생략
        }

        promptKey = key; // 키 저장

        switch (state) // 상태별 문구
        {
            case FishingState.Casting:
                prompt = "CASTING..."; // 던지는 중
                break;

            case FishingState.Waiting:
                prompt = "WAITING FOR A BITE... | F - REEL IN"; // 대기 중
                break;

            case FishingState.Bite:
                prompt = "! BITE ! | F - HOOK"; // 입질
                break;

            case FishingState.Reeling:
                prompt = "REELING..."; // 끌어올리는 중
                break;

            default:
                prompt = bait > 0 ? $"F - CAST LINE (BAIT {bait})" : "F - CAST LINE (NO BAIT)"; // 던지기 안내
                break;
        }
    }

    private void SetState(FishingState newState) // 상태 변경
    {
        state = newState; // 상태 저장
        stateStartTime = Time.time; // 시작 시각
        RefreshPrompt(); // 안내 문구 갱신
    }

    private Vector3 GetRodTipPosition() // 낚싯대 끝 위치
    {
        if (rodTip != null && rodTip.gameObject.activeInHierarchy) // 낚싯대 끝 확인
        {
            return rodTip.position; // 실제 위치
        }

        return transform.position + Vector3.up * 1.7f + transform.forward * 0.6f; // 대체 위치
    }

    private void SetBobberHeight(float offset) // 찌 수면 높이 적용
    {
        if (bobber != null) // 찌 확인
        {
            bobber.transform.position = bobberRestPoint + Vector3.up * offset; // 위치 적용
        }
    }

    private void UpdateLine() // 낚싯줄 곡선 갱신
    {
        if (fishingLine == null || bobber == null) // 참조 확인
        {
            return; // 처리 생략
        }

        Vector3 start = GetRodTipPosition(); // 시작점
        Vector3 end = bobber.transform.position; // 끝점
        float sag = state == FishingState.Bite ? 0.05f : state == FishingState.Casting ? 0f : 0.45f; // 처짐 정도
        Vector3 control = (start + end) * 0.5f + Vector3.down * sag; // 곡선 조절점

        for (int index = 0; index < LineSegments; index++) // 점 순회
        {
            float t = index / (float)(LineSegments - 1); // 비율
            float u = 1f - t; // 반대 비율
            linePoints[index] = u * u * start + 2f * u * t * control + t * t * end; // 2차 곡선
        }

        fishingLine.SetPositions(linePoints); // 점 적용
    }

    private void SetMarkerVisible(bool visible) // 던질 지점 표시
    {
        if (castMarker != null && castMarker.activeSelf != visible) // 상태 변경 확인
        {
            castMarker.SetActive(visible); // 표시 적용
        }
    }

    private void SetLineVisible(bool visible) // 낚싯줄 표시
    {
        if (fishingLine == null) // 참조 확인
        {
            return; // 처리 생략
        }

        fishingLine.positionCount = LineSegments; // 점 개수
        fishingLine.enabled = visible; // 표시 적용
    }

    private void Popup(string text, Color color, float size) // 플레이어 앞 알림
    {
        Popup(text, color, size, transform.position + Vector3.up * 2.1f); // 머리 위 알림
    }

    private static void Popup(string text, Color color, float size, Vector3 position) // 지정 위치 알림
    {
        CombatDamagePopup.SpawnText(position, text, color, size); // 떠오르는 글자
    }
}
