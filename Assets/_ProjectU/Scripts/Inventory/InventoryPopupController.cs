using System; // 이벤트 기능
using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class InventoryPopupController : MonoBehaviour // 인벤토리 팝업 관리자
{
    [SerializeField] private GameObject popupPanel; // 전체 인벤토리 팝업

    private GameUIManager gameUIManager; // 공통 게임 UI 관리자
    private GameplayInputLock gameplayInputLock; // 공통 입력 잠금 관리자
    private InventorySlotsUI[] inventorySlotViews = Array.Empty<InventorySlotsUI>(); // 인벤토리 슬롯 화면 목록
    private InventoryDetailUI[] inventoryDetailViews = Array.Empty<InventoryDetailUI>(); // 아이템 상세 화면 목록
    private EquipmentSlotUI[] equipmentSlotViews = Array.Empty<EquipmentSlotUI>(); // 장비 슬롯 화면 목록
    private EquipmentStatsUI[] equipmentStatsViews = Array.Empty<EquipmentStatsUI>(); // 장비 능력치 화면 목록
    private CraftingRecipeButton[] craftingRecipeButtons = Array.Empty<CraftingRecipeButton>(); // 제작법 버튼 목록
    private bool internalReferencesValid; // 프리팹 내부 참조 상태
    private bool runtimeInitialized; // 런타임 외부 참조 초기화 상태

    public bool IsOpen { get; private set; } // 팝업 열림 상태 제공
    public bool IsRuntimeInitialized => runtimeInitialized; // 런타임 초기화 여부 제공
    public event Action<bool> OpenStateChanged; // 팝업 상태 변경 알림

    private void Awake() // 인벤토리 팝업 내부 초기화
    {
        internalReferencesValid = popupPanel != null; // 팝업 패널 연결 상태 확인

        if (!internalReferencesValid) // 팝업 연결 누락 확인
        {
            Debug.LogError(
                $"{gameObject.name}의 InventoryPopupController에 Popup Panel이 연결되지 않았습니다.",
                this); // 참조 오류 출력

            enabled = false; // 팝업 기능 비활성화
            return; // 초기화 중단
        }

        CacheChildViews(); // 프리팹 내부 기능 화면 검색
        IsOpen = false; // 시작 팝업 상태 저장
    }

    public bool Initialize(
        GameUIManager manager,
        GameplayInputLock inputLock,
        PlayerInventory playerInventory,
        InventoryItemDropper itemDropper,
        PlayerEquipment playerEquipment,
        CraftingManager craftingManager) // 런타임 외부 참조 초기화
    {
        runtimeInitialized = false; // 초기화 상태 초기화

        if (!ValidateRuntimeReferences(
            manager,
            inputLock,
            playerInventory,
            itemDropper,
            playerEquipment,
            craftingManager)) // 필수 런타임 참조 검사
        {
            return false; // 초기화 실패 반환
        }

        gameUIManager = manager; // 공통 게임 UI 관리자 저장
        gameplayInputLock = inputLock; // 공통 입력 잠금 관리자 저장
        CacheChildViews(); // 현재 프리팹 내부 기능 화면 다시 검색

        if (!InitializeInventorySlotViews(playerInventory)) // 핵심 슬롯 화면 초기화
        {
            return false; // 핵심 슬롯 초기화 실패 반환
        }

        InitializeOptionalDetailViews(
            playerInventory,
            itemDropper,
            playerEquipment); // 아이템 상세 화면 초기화

        InitializeOptionalEquipmentViews(playerEquipment); // 장비 화면 초기화
        InitializeOptionalCraftingViews(
    playerInventory,
    craftingManager); // 제작 화면 초기화

        popupPanel.SetActive(false); // 모든 자식 UI 초기화 완료 후 팝업 숨김
        IsOpen = false; // 시작 팝업 상태 확정
        runtimeInitialized = true; // 런타임 초기화 완료 기록
        return true; // 초기화 성공 반환
    }

    public void SetOpen(bool shouldOpen) // 외부 요청으로 팝업 상태 변경
    {
        ResolveManager(); // 공통 게임 UI 관리자 검색

        if (gameUIManager == null) // 게임 UI 관리자 확인
        {
            Debug.LogError(
                $"{gameObject.name}에서 GameUIManager를 찾을 수 없습니다.",
                this); // 관리자 누락 오류 출력

            return; // 상태 변경 중단
        }

        if (shouldOpen) // 팝업 열기 요청 확인
        {
            gameUIManager.OpenInventory(); // 공통 관리자에서 인벤토리 열기
            return; // 열기 처리 종료
        }

        gameUIManager.CloseInventory(); // 공통 관리자에서 인벤토리 닫기
    }

    public bool ShowFromManager() // 공통 관리자에서 인벤토리 표시
    {
        if (!internalReferencesValid) // 내부 참조 상태 확인
        {
            Debug.LogError(
                $"{gameObject.name}의 인벤토리 팝업 내부 참조가 유효하지 않습니다.",
                this); // 내부 참조 오류 출력

            return false; // 팝업 표시 실패 반환
        }

        if (!runtimeInitialized) // 런타임 초기화 상태 확인
        {
            Debug.LogError(
                $"{gameObject.name}의 인벤토리 팝업 런타임 초기화가 완료되지 않았습니다.",
                this); // 런타임 초기화 오류 출력

            return false; // 팝업 표시 실패 반환
        }

        if (IsOpen) // 기존 열림 상태 확인
        {
            return true; // 기존 열림 상태 반환
        }

        IsOpen = true; // 팝업 열림 상태 저장
        popupPanel.SetActive(true); // 팝업 화면 표시
        OpenStateChanged?.Invoke(true); // 팝업 열림 알림
        return true; // 팝업 표시 성공 반환
    }

    public void HideFromManager() // 공통 관리자에서 인벤토리 숨김
    {
        bool wasOpen =
            IsOpen
            || (popupPanel != null && popupPanel.activeSelf); // 기존 열림 상태 확인

        IsOpen = false; // 팝업 닫힘 상태 저장

        if (popupPanel != null) // 팝업 패널 존재 확인
        {
            popupPanel.SetActive(false); // 팝업 화면 숨김
        }

        if (wasOpen) // 기존 팝업 열림 확인
        {
            OpenStateChanged?.Invoke(false); // 팝업 닫힘 알림
        }
    }

    private bool ValidateRuntimeReferences(
        GameUIManager manager,
        GameplayInputLock inputLock,
        PlayerInventory playerInventory,
        InventoryItemDropper itemDropper,
        PlayerEquipment playerEquipment,
        CraftingManager craftingManager) // 필수 런타임 참조 검사
    {
        if (!internalReferencesValid) // 팝업 내부 참조 확인
        {
            Debug.LogError(
                $"{gameObject.name}의 Popup Panel 참조가 누락되었습니다.",
                this); // 팝업 내부 참조 오류 출력

            return false; // 참조 검사 실패 반환
        }

        if (manager == null) // 게임 UI 관리자 확인
        {
            Debug.LogError(
                $"{gameObject.name}에 전달된 GameUIManager가 없습니다.",
                this); // 관리자 참조 오류 출력

            return false; // 참조 검사 실패 반환
        }

        if (inputLock == null) // 입력 잠금 관리자 확인
        {
            Debug.LogError(
                $"{gameObject.name}에 전달된 GameplayInputLock이 없습니다.",
                this); // 입력 잠금 참조 오류 출력

            return false; // 참조 검사 실패 반환
        }

        if (playerInventory == null) // 플레이어 인벤토리 확인
        {
            Debug.LogError(
                $"{gameObject.name}에 전달된 PlayerInventory가 없습니다.",
                this); // 인벤토리 참조 오류 출력

            return false; // 참조 검사 실패 반환
        }

        if (itemDropper == null) // 아이템 버리기 관리자 확인
        {
            Debug.LogError(
                $"{gameObject.name}에 전달된 InventoryItemDropper가 없습니다.",
                this); // 아이템 버리기 참조 오류 출력

            return false; // 참조 검사 실패 반환
        }

        if (playerEquipment == null) // 플레이어 장비 관리자 확인
        {
            Debug.LogError(
                $"{gameObject.name}에 전달된 PlayerEquipment가 없습니다.",
                this); // 장비 참조 오류 출력

            return false; // 참조 검사 실패 반환
        }

        if (craftingManager == null) // 제작 관리자 확인
        {
            Debug.LogError(
                $"{gameObject.name}에 전달된 CraftingManager가 없습니다.",
                this); // 제작 참조 오류 출력

            return false; // 참조 검사 실패 반환
        }

        return true; // 참조 검사 성공 반환
    }

    private bool InitializeInventorySlotViews(
        PlayerInventory playerInventory) // 핵심 슬롯 화면 초기화
    {
        if (inventorySlotViews == null || inventorySlotViews.Length == 0) // 슬롯 화면 존재 확인
        {
            Debug.LogError(
                $"{gameObject.name} 프리팹 아래에서 InventorySlotsUI를 찾을 수 없습니다.",
                this); // 슬롯 화면 누락 오류 출력

            return false; // 슬롯 초기화 실패 반환
        }

        bool allSlotViewsInitialized = true; // 전체 슬롯 화면 초기화 상태

        for (int index = 0; index < inventorySlotViews.Length; index++) // 인벤토리 슬롯 화면 순회
        {
            InventorySlotsUI targetView = inventorySlotViews[index]; // 현재 슬롯 화면 조회

            if (targetView == null) // 슬롯 화면 존재 확인
            {
                Debug.LogError(
                    $"{gameObject.name}의 InventorySlotsUI 배열 {index}번 참조가 비어 있습니다.",
                    this); // 빈 슬롯 화면 오류 출력

                allSlotViewsInitialized = false; // 전체 초기화 실패 기록
                continue; // 다음 슬롯 화면 확인
            }

            if (targetView.Initialize(playerInventory)) // 플레이어 인벤토리 전달 시도
            {
                continue; // 초기화 성공 화면 통과
            }

            Debug.LogError(
                $"{targetView.gameObject.name}의 InventorySlotsUI 초기화에 실패했습니다. "
                + "Slot Container와 Slot Template 연결을 확인하십시오.",
                targetView); // 슬롯 초기화 오류 출력

            allSlotViewsInitialized = false; // 전체 초기화 실패 기록
        }

        return allSlotViewsInitialized; // 전체 슬롯 초기화 결과 반환
    }

    private void InitializeOptionalDetailViews(
        PlayerInventory playerInventory,
        InventoryItemDropper itemDropper,
        PlayerEquipment playerEquipment) // 아이템 상세 화면 초기화
    {
        for (int index = 0; index < inventoryDetailViews.Length; index++) // 아이템 상세 화면 순회
        {
            InventoryDetailUI targetView = inventoryDetailViews[index]; // 현재 상세 화면 조회

            if (targetView == null) // 상세 화면 존재 확인
            {
                continue; // 빈 참조 제외
            }

            if (targetView.Initialize(
                playerInventory,
                itemDropper,
                playerEquipment)) // 상세 화면 런타임 초기화
            {
                continue; // 초기화 성공 화면 통과
            }

            Debug.LogError(
                $"{targetView.gameObject.name}의 InventoryDetailUI 초기화에 실패했습니다. "
                + "해당 영역은 비활성화되지만 인벤토리 팝업은 계속 열립니다.",
                targetView); // 상세 화면 초기화 오류 출력

            targetView.enabled = false; // 잘못된 상세 기능 비활성화
        }
    }

    private void InitializeOptionalEquipmentViews(
        PlayerEquipment playerEquipment) // 장비 화면 초기화
    {
        for (int index = 0; index < equipmentSlotViews.Length; index++) // 장비 슬롯 화면 순회
        {
            EquipmentSlotUI targetView = equipmentSlotViews[index]; // 현재 장비 슬롯 화면 조회

            if (targetView == null) // 장비 슬롯 화면 존재 확인
            {
                continue; // 빈 참조 제외
            }

            if (targetView.Initialize(playerEquipment)) // 장비 관리자 전달 시도
            {
                continue; // 초기화 성공 화면 통과
            }

            Debug.LogError(
                $"{targetView.gameObject.name}의 EquipmentSlotUI 초기화에 실패했습니다. "
                + "해당 슬롯은 비활성화되지만 인벤토리 팝업은 계속 열립니다.",
                targetView); // 장비 슬롯 초기화 오류 출력

            targetView.enabled = false; // 잘못된 장비 슬롯 기능 비활성화
        }

        for (int index = 0; index < equipmentStatsViews.Length; index++) // 장비 능력치 화면 순회
        {
            EquipmentStatsUI targetView = equipmentStatsViews[index]; // 현재 능력치 화면 조회

            if (targetView == null) // 능력치 화면 존재 확인
            {
                continue; // 빈 참조 제외
            }

            if (targetView.Initialize(playerEquipment)) // 장비 관리자 전달 시도
            {
                continue; // 초기화 성공 화면 통과
            }

            Debug.LogError(
                $"{targetView.gameObject.name}의 EquipmentStatsUI 초기화에 실패했습니다. "
                + "해당 화면은 비활성화되지만 인벤토리 팝업은 계속 열립니다.",
                targetView); // 능력치 화면 초기화 오류 출력

            targetView.enabled = false; // 잘못된 능력치 기능 비활성화
        }
    }

    private void InitializeOptionalCraftingViews(
        PlayerInventory playerInventory,
        CraftingManager craftingManager) // 제작 화면 초기화
    {
        for (int index = 0; index < craftingRecipeButtons.Length; index++) // 제작법 버튼 순회
        {
            CraftingRecipeButton targetButton = craftingRecipeButtons[index]; // 현재 제작법 버튼 조회

            if (targetButton == null) // 제작법 버튼 존재 확인
            {
                continue; // 빈 참조 제외
            }

            if (targetButton.Initialize(
                playerInventory,
                craftingManager)) // 제작 시스템 전달 시도
            {
                continue; // 초기화 성공 버튼 통과
            }

            Debug.LogError(
                $"{targetButton.gameObject.name}의 CraftingRecipeButton 초기화에 실패했습니다. "
                + "해당 버튼은 비활성화되지만 인벤토리 팝업은 계속 열립니다.",
                targetButton); // 제작 버튼 초기화 오류 출력

            targetButton.enabled = false; // 잘못된 제작 버튼 기능 비활성화
        }
    }

    private void CacheChildViews() // 프리팹 내부 기능 화면 검색
    {
        inventorySlotViews = GetComponentsInChildren<InventorySlotsUI>(true); // 인벤토리 슬롯 화면 검색
        inventoryDetailViews = GetComponentsInChildren<InventoryDetailUI>(true); // 아이템 상세 화면 검색
        equipmentSlotViews = GetComponentsInChildren<EquipmentSlotUI>(true); // 장비 슬롯 화면 검색
        equipmentStatsViews = GetComponentsInChildren<EquipmentStatsUI>(true); // 장비 능력치 화면 검색
        craftingRecipeButtons = GetComponentsInChildren<CraftingRecipeButton>(true); // 제작법 버튼 검색
    }

    private void ResolveManager() // 공통 게임 UI 관리자 자동 검색
    {
        if (gameUIManager == null) // 게임 UI 관리자 참조 확인
        {
            gameUIManager = FindFirstObjectByType<GameUIManager>(); // Scene 게임 UI 관리자 검색
        }

        if (gameplayInputLock == null && gameUIManager != null) // 입력 잠금 관리자 참조 확인
        {
            gameplayInputLock = gameUIManager.InputLock; // 공통 관리자에서 입력 잠금 조회
        }
    }

    private void OnDisable() // 인벤토리 팝업 비활성화 정리
    {
        if (!IsOpen) // 기존 열림 상태 확인
        {
            return; // 중복 종료 처리 생략
        }

        IsOpen = false; // 팝업 닫힘 상태 저장
        OpenStateChanged?.Invoke(false); // 팝업 닫힘 알림
    }
}
