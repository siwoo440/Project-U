using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class CraftingFacilityInteractable : InteractableBase // 제작 시설 상호작용 처리
{
    [Header("Facility")] // 제작 시설 설정 묶음
    [Tooltip("제공 제작 시설.")]
    [SerializeField] private CraftingFacilityType facilityType = CraftingFacilityType.Workbench; // 제공 제작 시설
    [Tooltip("시설 표시 이름.")]
    [SerializeField] private string facilityDisplayName = "WORKBENCH"; // 시설 표시 이름

    [Header("Runtime References")] // 런타임 외부 참조 묶음
    [Tooltip("플레이어 제작 관리자.")]
    [SerializeField] private CraftingManager craftingManager; // 플레이어 제작 관리자
    [Tooltip("공통 게임 UI 관리자.")]
    [SerializeField] private GameUIManager gameUIManager; // 공통 게임 UI 관리자

    private bool ownsCurrentSession; // 현재 시설 세션 소유 상태

    public override string PromptMessage => $"F - USE {facilityDisplayName}"; // 제작 시설 안내 문구 제공

    private void Awake() // 제작 시설 초기화
    {
        facilityDisplayName = string.IsNullOrWhiteSpace(facilityDisplayName)
            ? facilityType.ToString().ToUpperInvariant()
            : facilityDisplayName.Trim(); // 시설 표시 이름 보정

        ResolveManagers(); // 제작과 UI 관리자 자동 검색

        if (craftingManager == null || gameUIManager == null) // 필수 참조 확인
        {
            Debug.LogError($"{gameObject.name}의 제작 시설 참조가 누락되었습니다.", this); // 참조 오류 출력
            enabled = false; // 제작 시설 상호작용 비활성화
        }
    }

    private void OnEnable() // 팝업 상태 이벤트 연결
    {
        ResolveManagers(); // 공통 관리자 참조 확인

        if (gameUIManager == null) // 게임 UI 관리자 존재 확인
        {
            return; // 이벤트 연결 중단
        }

        gameUIManager.PopupStateChanged += HandlePopupStateChanged; // 팝업 상태 변경 구독
    }

    private void OnDisable() // 팝업 상태 이벤트 해제
    {
        if (gameUIManager != null) // 게임 UI 관리자 존재 확인
        {
            gameUIManager.PopupStateChanged -= HandlePopupStateChanged; // 팝업 상태 변경 구독 해제
        }

        ReleaseFacilitySession(); // 현재 제작 시설 세션 해제
    }

    public override void Interact(GameObject interactor) // 제작 시설 사용 처리
    {
        if (!enabled || interactor == null) // 상호작용 가능 여부 확인
        {
            return; // 상호작용 중단
        }

        ResolveManagers(); // 제작과 UI 관리자 참조 확인

        if (craftingManager == null || gameUIManager == null) // 필수 참조 확인
        {
            return; // 시설 사용 중단
        }

        craftingManager.SetActiveFacility(facilityType); // 현재 제작 시설 적용
        ownsCurrentSession = true; // 시설 세션 소유 적용

        if (!gameUIManager.OpenInventory()) // 인벤토리 팝업 열기 시도
        {
            ReleaseFacilitySession(); // 팝업 열기 실패 시 시설 세션 해제
        }
    }

    private void HandlePopupStateChanged(
        GamePopupType popupType,
        bool isOpen) // 팝업 상태 변경 처리
    {
        if (popupType != GamePopupType.Inventory || isOpen) // 인벤토리 종료 여부 확인
        {
            return; // 시설 세션 종료 처리 생략
        }

        ReleaseFacilitySession(); // 인벤토리 팝업 종료 후 시설 세션 해제
    }

    private void ReleaseFacilitySession() // 현재 제작 시설 세션 해제
    {
        if (!ownsCurrentSession) // 세션 소유 여부 확인
        {
            return; // 해제 처리 중단
        }

        ownsCurrentSession = false; // 세션 소유 상태 해제

        if (craftingManager == null) // 제작 관리자 존재 확인
        {
            return; // 시설 초기화 중단
        }

        if (craftingManager.ActiveFacilityType == facilityType) // 현재 시설 일치 여부 확인
        {
            craftingManager.ResetToHand(); // 맨손 제작 시설 복귀
        }
    }

    private void ResolveManagers() // 제작과 UI 관리자 자동 검색
    {
        if (craftingManager == null) // 제작 관리자 참조 확인
        {
            craftingManager = FindFirstObjectByType<CraftingManager>(); // Scene 제작 관리자 검색
        }

        if (gameUIManager == null) // 게임 UI 관리자 참조 확인
        {
            gameUIManager = FindFirstObjectByType<GameUIManager>(); // Scene 게임 UI 관리자 검색
        }
    }

    private void OnValidate() // Inspector 설정값 검증
    {
        facilityDisplayName = string.IsNullOrWhiteSpace(facilityDisplayName)
            ? facilityType.ToString().ToUpperInvariant()
            : facilityDisplayName.Trim(); // 시설 표시 이름 보정
    }
}
