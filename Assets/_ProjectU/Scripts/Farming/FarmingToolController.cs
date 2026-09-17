using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class FarmingToolController : MonoBehaviour // 플레이어 앞 칸의 밭 대상 탐지와 괭이 경작
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"); // URP 기본 색상 속성
    private static readonly int ColorId = Shader.PropertyToID("_Color"); // 기본 색상 속성

    [Header("References")] // 참조 묶음
    [Tooltip("손에 든 아이템을 확인할 플레이어 인벤토리입니다.")]
    [SerializeField] private PlayerInventory playerInventory; // 플레이어 인벤토리

    [Tooltip("경작·물주기 스태미나를 소비할 대상입니다.")]
    [SerializeField] private PlayerStamina playerStamina; // 플레이어 스태미나

    [Tooltip("사망 중 농사 작업을 막기 위한 플레이어 체력입니다.")]
    [SerializeField] private PlayerHealth playerHealth; // 플레이어 체력

    [Tooltip("그리드·지형·충돌 검사와 밭 설치에 사용할 건축 관리자입니다.")]
    [SerializeField] private BuildPlacementController buildPlacementController; // 건축 관리자

    [Tooltip("대상 칸 방향을 정할 Camera Transform입니다.")]
    [SerializeField] private Transform viewTransform; // 시선 기준

    [Header("Target Cell")] // 대상 칸 묶음
    [Tooltip("경작 가능한 칸을 표시하고 F키 경작을 받는 대상입니다.")]
    [SerializeField] private FarmTillTarget tillTarget; // 경작 대상

    [Tooltip("대상 칸 표시 Renderer입니다.")]
    [SerializeField] private Renderer highlightRenderer; // 대상 칸 표시

    [Tooltip("플레이어 위치에서 대상 칸까지의 거리입니다.")]
    [SerializeField, Min(0.3f)] private float targetDistance = 1.3f; // 대상 거리

    [Tooltip("대상 칸 재계산 간격(초)입니다.")]
    [SerializeField, Min(0f)] private float refreshInterval = 0.08f; // 재계산 간격

    [Tooltip("작업 가능한 칸 색상입니다.")]
    [SerializeField] private Color validColor = new Color(0.45f, 0.95f, 0.45f, 0.35f); // 가능 색상

    [Tooltip("작업할 수 없는 칸 색상입니다.")]
    [SerializeField] private Color invalidColor = new Color(1f, 0.35f, 0.3f, 0.35f); // 불가 색상

    private MaterialPropertyBlock propertyBlock; // 표시 색상 블록
    private FarmPlot targetPlot; // 현재 대상 밭
    private bool hasTillCell; // 경작 대상 칸 존재 여부
    private bool tillValid; // 경작 가능 여부
    private string tillStatus = string.Empty; // 경작 불가 사유
    private string tillPrompt = string.Empty; // 경작 안내 문구
    private Vector3 targetPoint; // 대상 지점
    private Vector3 tillPosition; // 경작 위치
    private Quaternion tillRotation = Quaternion.identity; // 경작 회전
    private float nextRefreshTime; // 다음 재계산 시각
    private float lastQueryTime = -10f; // 마지막 대상 요청 시각
    private ItemData lastSelectedItem; // 마지막 선택 아이템
    private bool highlightVisible; // 표시 상태
    private bool highlightValid; // 표시 색상 상태

    public static FarmingToolController Local { get; private set; } // 현재 플레이어 농사 도구
    public PlayerInventory Inventory => playerInventory; // 인벤토리 제공
    public ItemData SelectedItem => playerInventory != null ? playerInventory.SelectedHotbarItem : null; // 손에 든 아이템 제공
    public bool CanAct => playerHealth == null || !playerHealth.IsDead; // 작업 가능 여부 제공
    public string TillPrompt => tillPrompt; // 경작 안내 문구 제공

    private void Awake() // 참조 준비
    {
        Local = this; // 현재 플레이어 등록
        propertyBlock = new MaterialPropertyBlock(); // 색상 블록 생성

        if (playerInventory == null) { playerInventory = GetComponent<PlayerInventory>(); } // 인벤토리 자동 검색
        if (playerStamina == null) { playerStamina = GetComponent<PlayerStamina>(); } // 스태미나 자동 검색
        if (playerHealth == null) { playerHealth = GetComponent<PlayerHealth>(); } // 체력 자동 검색
        if (buildPlacementController == null) { buildPlacementController = GetComponent<BuildPlacementController>(); } // 건축 관리자 자동 검색

        if (viewTransform == null && Camera.main != null) // 시선 기준 누락 확인
        {
            viewTransform = Camera.main.transform; // 기본 Camera 사용
        }

        if (tillTarget != null) // 경작 대상 확인
        {
            tillTarget.Bind(this); // 경작 대상 연결
        }

        SetHighlight(false, false); // 시작 시 표시 숨김
    }

    private void OnDestroy() // 등록 해제
    {
        if (Local == this) // 현재 플레이어 확인
        {
            Local = null; // 등록 해제
        }
    }

    private void Update() // 요청이 끊긴 표시 정리
    {
        if (highlightVisible && Time.time - lastQueryTime > 0.15f) // 다른 대상을 보거나 입력이 막힌 상태 확인
        {
            SetHighlight(false, false); // 표시 숨김
            targetPlot = null; // 대상 초기화
            hasTillCell = false; // 경작 대상 초기화
        }
    }

    public InteractableBase FindTargetInteractable() // PlayerInteractor가 다른 대상을 찾지 못했을 때 농사 대상 반환
    {
        lastQueryTime = Time.time; // 요청 시각 기록
        FarmManager manager = FarmManager.Instance; // 밭 관리자 조회

        if (!isActiveAndEnabled || manager == null || manager.Rules == null || !CanAct) // 사용 가능 상태 확인
        {
            SetHighlight(false, false); // 표시 숨김
            return null; // 대상 없음
        }

        ItemData selected = SelectedItem; // 손에 든 아이템

        if (Time.time >= nextRefreshTime || selected != lastSelectedItem) // 재계산 시점 확인
        {
            lastSelectedItem = selected; // 선택 아이템 기록
            nextRefreshTime = Time.time + refreshInterval; // 다음 시각 기록
            RefreshTarget(manager, selected); // 대상 재계산
        }

        if (targetPlot != null && targetPlot.isActiveAndEnabled) // 밭 대상 확인
        {
            return targetPlot; // 밭 반환
        }

        return hasTillCell && tillTarget != null ? tillTarget : null; // 경작 대상 반환
    }

    public bool TryTill() // 대상 칸에 밭 만들기
    {
        FarmManager manager = FarmManager.Instance; // 밭 관리자 조회

        if (manager == null || manager.Rules == null || !CanAct) // 사용 가능 상태 확인
        {
            return false; // 경작 실패
        }

        RefreshTarget(manager, SelectedItem); // 최신 대상 계산

        if (!hasTillCell || !tillValid) // 경작 가능 여부 확인
        {
            return false; // 경작 실패
        }

        if (!TryConsumeStamina(manager.Rules.TillingStaminaCost)) // 스태미나 소비
        {
            SetTillStatus(false, "TOO TIRED"); // 스태미나 부족 안내
            return false; // 경작 실패
        }

        bool placed = buildPlacementController.TryPlaceGroundStructure(
            manager.Rules.FarmPlotRecipe,
            targetPoint,
            out PlacedBuildObject _,
            out string failureStatus); // 밭 설치

        if (!placed) // 설치 실패 확인
        {
            SetTillStatus(false, failureStatus); // 실패 사유 안내
            return false; // 경작 실패
        }

        nextRefreshTime = 0f; // 다음 요청에서 대상 재계산
        GiveTillingBonus(manager); // 흙에서 나온 보너스 아이템
        return true; // 경작 성공
    }

    private void GiveTillingBonus(FarmManager manager) // 경작 보너스 아이템 지급 (82일차: 지렁이 미끼)
    {
        ItemData bonus = manager.Rules.TillingBonusItem; // 보너스 아이템

        if (bonus == null || playerInventory == null || Random.value >= manager.Rules.TillingBonusChance) // 확률 확인
        {
            return; // 지급 없음
        }

        int remaining = playerInventory.AddItem(bonus, 1); // 인벤토리 추가

        if (remaining > 0) // 넘침 확인
        {
            manager.TryDropItem(bonus, remaining, tillPosition); // 바닥 드롭
        }

        CombatDamagePopup.SpawnText(tillPosition + Vector3.up * 0.6f, "+1 " + bonus.DisplayName, new Color(0.9f, 0.75f, 0.55f, 1f), 2f); // 획득 알림
    }

    public bool TryConsumeStamina(float cost) // 농사 작업 스태미나 소비
    {
        return cost <= 0f || playerStamina == null || playerStamina.TryConsume(cost); // 소비 결과 반환
    }

    private void RefreshTarget(FarmManager manager, ItemData selected) // 플레이어 앞 칸 대상 계산
    {
        Vector3 forward = viewTransform != null ? viewTransform.forward : transform.forward; // 시선 방향
        forward.y = 0f; // 수평 방향만 사용

        if (forward.sqrMagnitude < 0.0001f) // 방향 확인
        {
            forward = transform.forward; // 캐릭터 방향 사용
            forward.y = 0f; // 수평 방향만 사용
        }

        targetPoint = transform.position + forward.normalized * targetDistance; // 대상 지점 계산
        hasTillCell = false; // 경작 대상 초기화
        manager.TryFindPlotAt(targetPoint, out targetPlot); // 대상 밭 검색

        if (targetPlot != null) // 밭 대상 확인
        {
            bool showPlot = IsFarmingItem(manager, selected); // 농사 아이템을 들었을 때만 칸 표시
            SetHighlightTransform(targetPlot.transform.position, targetPlot.transform.rotation); // 표시 위치 적용
            SetHighlight(showPlot, true); // 표시 적용
            return; // 처리 종료
        }

        bool holdingHoe = selected != null && selected.IsTool && selected.ToolType == manager.Rules.TillingTool; // 괭이 확인

        if (!holdingHoe || buildPlacementController == null || manager.Rules.FarmPlotRecipe == null) // 경작 조건 확인
        {
            SetHighlight(false, false); // 표시 숨김
            return; // 처리 종료
        }

        tillValid = buildPlacementController.TryResolveGroundStructure(
            manager.Rules.FarmPlotRecipe,
            targetPoint,
            true,
            out tillPosition,
            out tillRotation,
            out string failureStatus); // 설치 가능 여부 검사

        if (!tillValid && failureStatus == BuildPlacementController.OutsideBuildAreaStatus) // 거점 밖 확인
        {
            SetHighlight(false, false); // 거점 밖에서는 안내하지 않음
            return; // 처리 종료
        }

        hasTillCell = true; // 경작 대상 존재
        SetTillStatus(tillValid, failureStatus); // 안내 문구 갱신
        SetHighlightTransform(tillPosition, tillRotation); // 표시 위치 적용
        SetHighlight(true, tillValid); // 표시 적용
    }

    private static bool IsFarmingItem(FarmManager manager, ItemData item) // 농사 작업 아이템 여부
    {
        if (item == null) // 빈손 확인
        {
            return false; // 농사 아이템 아님
        }

        if (item.ItemCategory == ItemCategory.Seed) // 씨앗 확인
        {
            return true; // 농사 아이템
        }

        return item.IsTool && (item.ToolType == manager.Rules.TillingTool || item.ToolType == manager.Rules.WateringTool); // 농사 도구 여부 반환
    }

    private void SetTillStatus(bool isValid, string failureStatus) // 경작 안내 문구 갱신
    {
        tillValid = isValid; // 가능 여부 저장
        string status = isValid ? string.Empty : failureStatus; // 불가 사유

        if (status == tillStatus && tillPrompt.Length > 0) // 같은 안내 확인
        {
            return; // 문자열 재생성 생략
        }

        tillStatus = status; // 사유 저장
        tillPrompt = isValid ? "F - TILL SOIL" : "CAN'T TILL | " + status; // 안내 문구 적용
    }

    private void SetHighlightTransform(Vector3 position, Quaternion rotation) // 표시 위치 적용
    {
        if (highlightRenderer == null) // 표시 확인
        {
            return; // 처리 생략
        }

        Transform highlight = highlightRenderer.transform; // 표시 Transform
        highlight.SetPositionAndRotation(position + Vector3.up * 0.03f, rotation * Quaternion.Euler(90f, 0f, 0f)); // 바닥에 눕혀 배치
    }

    private void SetHighlight(bool visible, bool valid) // 표시 상태와 색상 적용
    {
        if (highlightRenderer == null) // 표시 확인
        {
            return; // 처리 생략
        }

        if (highlightVisible != visible) // 표시 상태 변경 확인
        {
            highlightVisible = visible; // 상태 저장
            highlightRenderer.enabled = visible; // 표시 적용
        }

        if (!visible || (highlightValid == valid && propertyBlock.isEmpty == false)) // 색상 변경 필요 확인
        {
            return; // 처리 생략
        }

        highlightValid = valid; // 색상 상태 저장
        Color color = valid ? validColor : invalidColor; // 표시 색상
        propertyBlock.SetColor(BaseColorId, color); // URP 색상 적용
        propertyBlock.SetColor(ColorId, color); // 기본 색상 적용
        highlightRenderer.SetPropertyBlock(propertyBlock); // 색상 블록 적용
    }
}
