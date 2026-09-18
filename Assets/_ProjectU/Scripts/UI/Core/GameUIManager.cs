using System; // 이벤트 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.InputSystem; // 키보드 입력 기능

public enum GamePopupType // 게임 팝업 종류
{
    None = 0, // 열린 팝업 없음
    Inventory = 1, // 일반 인벤토리 팝업
    Storage = 2, // 보관함 팝업
    Cooking = 3, // 요리 팝업 (85일차)
    AnimalPen = 4, // 우리 팝업 (86일차)
    Shop = 5, // 상인 팝업 (87일차)
    Dialogue = 6, // NPC 대화 창 (91일차)
    QuestBoard = 7 // 마을 게시판 창 (93일차)
}

public interface IGameScenePopup // 85·86일차: Scene에 배치해 두고 켜고 끄는 팝업
{
    bool IsOpen { get; } // 실제 표시 여부
    void HideFromManager(); // 관리자 요청으로 숨김
}

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class GameUIManager : MonoBehaviour // 게임 팝업 생성과 실행 순서 관리자
{
    private const string PopupLockId = "GameUIManager.Popup"; // 공통 팝업 입력 잠금 ID
    private const string AltCursorLockId = "GameUIManager.AltCursor"; // Alt 커서 입력 잠금 ID

    [Header("Core")] // 핵심 관리자 참조 묶음
    [Tooltip("게임 플레이 입력 잠금 관리자.")]
    [SerializeField] private GameplayInputLock gameplayInputLock; // 게임 플레이 입력 잠금 관리자
    [Tooltip("플레이어 인벤토리.")]
    [SerializeField] private PlayerInventory playerInventory; // 플레이어 인벤토리
    [Tooltip("아이템 버리기 관리자.")]
    [SerializeField] private InventoryItemDropper inventoryItemDropper; // 아이템 버리기 관리자
    [Tooltip("플레이어 장비 관리자.")]
    [SerializeField] private PlayerEquipment playerEquipment; // 플레이어 장비 관리자
    [Tooltip("플레이어 제작 관리자.")]
    [SerializeField] private CraftingManager craftingManager; // 플레이어 제작 관리자

    [Header("Runtime Popup Layer")] // 런타임 팝업 배치 설정 묶음
    [Tooltip("동적 팝업 생성 부모.")]
    [SerializeField] private Transform popupLayer; // 동적 팝업 생성 부모

    [Header("Runtime Popup Prefabs")] // 런타임 팝업 프리팹 설정 묶음
    [Tooltip("일반 인벤토리 팝업 프리팹.")]
    [SerializeField] private InventoryPopupController inventoryPopupPrefab; // 일반 인벤토리 팝업 프리팹
    [Tooltip("보관함 팝업 프리팹.")]
    [SerializeField] private StorageContainerUI storagePopupPrefab; // 보관함 팝업 프리팹

    [Header("Cooking")] // 요리 팝업 설정 묶음 (85일차)
    [Tooltip("Scene에 배치된 요리 팝업. 비어 있으면 모닥불에서 바로 조리합니다.")]
    [SerializeField] private CookingPopupUI cookingPopup; // 요리 팝업
    [Tooltip("Scene에 배치된 우리 팝업. 비어 있으면 우리에서 바로 먹이 주기·수거합니다. (86일차)")]
    [SerializeField] private AnimalPenPopupUI animalPenPopup; // 우리 팝업
    [Tooltip("Scene에 배치된 상인 팝업. (87일차)")]
    [SerializeField] private ShopPopupUI shopPopup; // 상인 팝업
    [Tooltip("Scene에 배치된 NPC 대화 창. (91일차)")]
    [SerializeField] private NpcDialoguePopup npcDialoguePopup; // NPC 대화 창
    [Tooltip("Scene에 배치된 마을 게시판 창. (93일차)")]
    [SerializeField] private NpcQuestBoardPopup questBoardPopup; // 게시판 창

    private InventoryPopupController inventoryPopupInstance; // 생성된 인벤토리 팝업 인스턴스
    private StorageContainerUI storagePopupInstance; // 생성된 보관함 팝업 인스턴스
    private GamePopupType currentPopupType = GamePopupType.None; // 현재 열린 팝업 종류
    private bool referencesValid; // 필수 참조 연결 상태
    private bool isAltCursorActive; // Alt 커서 활성 상태

    public GamePopupType CurrentPopupType => currentPopupType; // 현재 열린 팝업 종류 제공
    public bool HasOpenPopup => currentPopupType != GamePopupType.None; // 현재 팝업 열림 여부 제공
    public bool HasInventoryPopupInstance => inventoryPopupInstance != null; // 인벤토리 팝업 생성 여부 제공
    public bool HasStoragePopupInstance => storagePopupInstance != null; // 보관함 팝업 생성 여부 제공
    public GameplayInputLock InputLock => gameplayInputLock; // 공통 입력 잠금 관리자 제공
    public InventoryPopupController InventoryPopupInstance => inventoryPopupInstance; // 생성된 인벤토리 팝업 제공
    public StorageContainerUI StoragePopupInstance => storagePopupInstance; // 생성된 보관함 팝업 제공
    public CookingPopupUI CookingPopup => cookingPopup; // 요리 팝업 제공
    public NpcDialoguePopup NpcDialoguePopup => npcDialoguePopup; // NPC 대화 창 제공 (91일차)
    public NpcQuestBoardPopup QuestBoardPopup => questBoardPopup; // 게시판 창 제공 (93일차)
    public AnimalPenPopupUI AnimalPenPopup => animalPenPopup; // 우리 팝업 제공
    public ShopPopupUI ShopPopup => shopPopup; // 상인 팝업 제공
    public event Action<GamePopupType, bool> PopupStateChanged; // 팝업 상태 변경 알림

    private void Awake() // 게임 UI 관리자 초기화
    {
        ResolveCoreReferences(); // Scene 핵심 시스템 자동 검색

        referencesValid = gameplayInputLock != null // 입력 잠금 관리자 확인
            && playerInventory != null // 플레이어 인벤토리 확인
            && inventoryItemDropper != null // 아이템 버리기 관리자 확인
            && playerEquipment != null // 플레이어 장비 관리자 확인
            && craftingManager != null // 제작 관리자 확인
            && popupLayer != null // 팝업 생성 부모 확인
            && inventoryPopupPrefab != null // 인벤토리 팝업 프리팹 확인
            && storagePopupPrefab != null; // 보관함 팝업 프리팹 확인

        if (!referencesValid) // 필수 참조 누락 확인
        {
            Debug.LogError($"{gameObject.name}의 GameUIManager 참조가 누락되었습니다.", this); // 참조 오류 출력
            enabled = false; // 게임 UI 관리자 비활성화
            return; // 초기화 중단
        }

        currentPopupType = GamePopupType.None; // 시작 팝업 상태 초기화
        isAltCursorActive = false; // 시작 Alt 상태 초기화
    }

    private void Update() // 공통 UI 입력 처리
    {
        Keyboard keyboard = Keyboard.current; // 현재 키보드 조회

        if (keyboard == null) // 키보드 존재 확인
        {
            return; // 입력 처리 중단
        }

        bool currentAltState =
            keyboard.leftAltKey.isPressed
            || keyboard.rightAltKey.isPressed; // 현재 Alt 입력 상태 계산

        if (currentAltState != isAltCursorActive) // Alt 상태 변경 확인
        {
            isAltCursorActive = currentAltState; // 새로운 Alt 상태 저장
            RefreshAltCursorLock(); // Alt 커서 입력 잠금 갱신
        }

        if (HasOpenPopup && keyboard.escapeKey.wasPressedThisFrame) // 열린 팝업과 ESC 입력 확인
        {
            CloseCurrentPopup(); // 현재 팝업 종료
            return; // 같은 프레임 추가 입력 차단
        }

        if (keyboard.iKey.wasPressedThisFrame) // 인벤토리 키 입력 확인
        {
            ToggleInventory(); // 일반 인벤토리 상태 반전
        }
    }

    public bool ToggleInventory() // 인벤토리 팝업 상태 반전
    {
        if (currentPopupType == GamePopupType.Inventory) // 인벤토리 팝업 열림 확인
        {
            CloseInventory(); // 인벤토리 팝업 닫기
            return false; // 닫힘 상태 반환
        }

        return OpenInventory(); // 인벤토리 팝업 열기 결과 반환
    }

    public bool OpenInventory() // 인벤토리 팝업 열기
    {
        if (!CanUseManager()) // 게임 UI 관리자 사용 가능 여부 확인
        {
            return false; // 팝업 열기 실패 반환
        }

        if (currentPopupType == GamePopupType.Inventory
            && inventoryPopupInstance != null
            && inventoryPopupInstance.IsOpen) // 동일 팝업 열림 확인
        {
            return true; // 기존 열림 상태 반환
        }

        bool popupLockAlreadyHeld = gameplayInputLock.Contains(PopupLockId); // 기존 팝업 입력 잠금 확인
        HideCurrentPopupWithoutUnlock(); // 기존 팝업 화면만 숨김

        InventoryPopupController popupInstance = GetOrCreateInventoryPopup(); // 인벤토리 팝업 인스턴스 준비

        if (popupInstance == null) // 인벤토리 팝업 생성 결과 확인
        {
            if (popupLockAlreadyHeld) // 기존 팝업 입력 잠금 확인
            {
                gameplayInputLock.Release(PopupLockId); // 실패한 팝업 입력 잠금 해제
            }

            return false; // 팝업 열기 실패 반환
        }

        if (!popupLockAlreadyHeld) // 기존 팝업 입력 잠금 없음 확인
        {
            gameplayInputLock.Acquire(PopupLockId); // 공통 팝업 입력 잠금 획득
        }

        if (!popupInstance.ShowFromManager()) // 인벤토리 팝업 표시 시도
        {
            currentPopupType = GamePopupType.None; // 현재 팝업 상태 초기화
            gameplayInputLock.Release(PopupLockId); // 공통 팝업 입력 잠금 해제
            return false; // 팝업 열기 실패 반환
        }

        currentPopupType = GamePopupType.Inventory; // 현재 팝업 종류 저장
        PopupStateChanged?.Invoke(GamePopupType.Inventory, true); // 인벤토리 열림 알림
        return true; // 팝업 열기 성공 반환
    }

    public bool OpenStorage(StorageContainer storageContainer) // 지정 보관함 팝업 열기
    {
        if (!CanUseManager() || storageContainer == null) // 관리자와 보관함 참조 확인
        {
            return false; // 팝업 열기 실패 반환
        }

        bool popupLockAlreadyHeld = gameplayInputLock.Contains(PopupLockId); // 기존 팝업 입력 잠금 확인
        HideCurrentPopupWithoutUnlock(); // 기존 팝업 화면만 숨김

        StorageContainerUI popupInstance = GetOrCreateStoragePopup(); // 보관함 팝업 인스턴스 준비

        if (popupInstance == null) // 보관함 팝업 생성 결과 확인
        {
            if (popupLockAlreadyHeld) // 기존 팝업 입력 잠금 확인
            {
                gameplayInputLock.Release(PopupLockId); // 실패한 팝업 입력 잠금 해제
            }

            return false; // 팝업 열기 실패 반환
        }

        if (!popupLockAlreadyHeld) // 기존 팝업 입력 잠금 없음 확인
        {
            gameplayInputLock.Acquire(PopupLockId); // 공통 팝업 입력 잠금 획득
        }

        if (!popupInstance.ShowFromManager(storageContainer)) // 보관함 팝업 표시 시도
        {
            currentPopupType = GamePopupType.None; // 현재 팝업 상태 초기화
            gameplayInputLock.Release(PopupLockId); // 공통 팝업 입력 잠금 해제
            return false; // 팝업 열기 실패 반환
        }

        currentPopupType = GamePopupType.Storage; // 현재 팝업 종류 저장
        PopupStateChanged?.Invoke(GamePopupType.Storage, true); // 보관함 열림 알림
        return true; // 팝업 열기 성공 반환
    }

    public void CloseInventory() // 인벤토리 팝업 강제 종료
    {
        bool wasInventoryVisible =
            inventoryPopupInstance != null
            && inventoryPopupInstance.IsOpen; // 실제 인벤토리 화면 표시 여부 확인

        if (inventoryPopupInstance != null) // 인벤토리 팝업 인스턴스 확인
        {
            inventoryPopupInstance.HideFromManager(); // 관리자 상태와 관계없이 인벤토리 숨김
        }

        if (currentPopupType != GamePopupType.None && currentPopupType != GamePopupType.Inventory) // 다른 팝업 상태 확인
        {
            return; // 다른 팝업 입력 잠금 유지
        }

        bool shouldReleasePopupLock =
            currentPopupType == GamePopupType.Inventory
            || wasInventoryVisible; // 인벤토리 종료에 따른 입력 잠금 해제 여부 계산

        bool shouldNotifyClosed =
            currentPopupType == GamePopupType.Inventory
            || wasInventoryVisible; // 인벤토리 종료 알림 필요 여부 계산

        currentPopupType = GamePopupType.None; // 현재 팝업 상태 초기화

        if (shouldReleasePopupLock) // 인벤토리 입력 잠금 보유 가능성 확인
        {
            gameplayInputLock.Release(PopupLockId); // 공통 팝업 입력 잠금 해제
        }

        if (shouldNotifyClosed) // 인벤토리 종료 알림 필요 여부 확인
        {
            PopupStateChanged?.Invoke(GamePopupType.Inventory, false); // 인벤토리 닫힘 알림
        }
    }

    public void CloseStorage() // 보관함 팝업 강제 종료
    {
        bool wasStorageVisible =
            storagePopupInstance != null
            && storagePopupInstance.IsOpen; // 실제 보관함 화면 표시 여부 확인

        if (storagePopupInstance != null) // 보관함 팝업 인스턴스 확인
        {
            storagePopupInstance.HideFromManager(); // 관리자 상태와 관계없이 보관함 화면 숨김
        }

        if (currentPopupType != GamePopupType.None && currentPopupType != GamePopupType.Storage) // 다른 팝업 상태 확인
        {
            return; // 다른 팝업 입력 잠금 유지
        }

        bool shouldReleasePopupLock =
            currentPopupType == GamePopupType.Storage
            || wasStorageVisible; // 보관함 종료에 따른 입력 잠금 해제 여부 계산

        bool shouldNotifyClosed =
            currentPopupType == GamePopupType.Storage
            || wasStorageVisible; // 보관함 종료 알림 필요 여부 계산

        currentPopupType = GamePopupType.None; // 현재 팝업 상태 초기화

        if (shouldReleasePopupLock) // 보관함 입력 잠금 보유 가능성 확인
        {
            gameplayInputLock.Release(PopupLockId); // 공통 팝업 입력 잠금 해제
        }

        if (shouldNotifyClosed) // 보관함 종료 알림 필요 여부 확인
        {
            PopupStateChanged?.Invoke(GamePopupType.Storage, false); // 보관함 닫힘 알림
        }
    }

    public bool OpenCooking(CampfireCookingStation station) // 지정 모닥불 요리 팝업 열기 (85일차)
    {
        return cookingPopup != null
            && station != null
            && OpenScenePopup(GamePopupType.Cooking, () => cookingPopup.ShowFromManager(this, station, playerInventory)); // 결과 반환
    }

    public void CloseCooking() // 요리 팝업 강제 종료 (85일차)
    {
        CloseScenePopup(GamePopupType.Cooking, cookingPopup); // 종료
    }

    public bool OpenAnimalPen(AnimalPen pen) // 지정 우리 팝업 열기 (86일차)
    {
        return animalPenPopup != null
            && pen != null
            && OpenScenePopup(GamePopupType.AnimalPen, () => animalPenPopup.ShowFromManager(this, pen, playerInventory)); // 결과 반환
    }

    public void CloseAnimalPen() // 우리 팝업 강제 종료 (86일차)
    {
        CloseScenePopup(GamePopupType.AnimalPen, animalPenPopup); // 종료
    }

    public bool OpenShop(MarketStall stall) // 지정 가판대 상인 팝업 열기 (87일차)
    {
        return shopPopup != null
            && stall != null
            && OpenScenePopup(GamePopupType.Shop, () => shopPopup.ShowFromManager(this, stall, playerInventory)); // 결과 반환
    }

    public bool OpenNpcShop(NpcAgent npc) // NPC 상점 창 열기 (92일차 : 87일차 상인 창 재사용)
    {
        NpcShopManager shops = NpcShopManager.Instance; // NPC 상점 관리자

        return shopPopup != null
            && npc != null
            && shops != null
            && shops.TryGetShop(npc.Character, out NpcShopData shop)
            && OpenScenePopup(GamePopupType.Shop, () => shopPopup.ShowVendor(this, new NpcShopVendor(shops, shop, npc), playerInventory)); // 결과 반환
    }

    public void CloseShop() // 상인 팝업 강제 종료 (87일차)
    {
        CloseScenePopup(GamePopupType.Shop, shopPopup); // 종료
    }

    public bool OpenNpcDialogue(NpcAgent npc) // NPC 대화 창 열기 (91일차)
    {
        return npcDialoguePopup != null
            && npc != null
            && OpenScenePopup(GamePopupType.Dialogue, () => npcDialoguePopup.ShowFromManager(this, npc, playerInventory)); // 결과 반환
    }

    public void CloseNpcDialogue() // NPC 대화 창 강제 종료 (91일차)
    {
        CloseScenePopup(GamePopupType.Dialogue, npcDialoguePopup); // 종료
    }

    public bool OpenQuestBoard(QuestBoard board) // 마을 게시판 창 열기 (93일차)
    {
        return questBoardPopup != null
            && board != null
            && OpenScenePopup(GamePopupType.QuestBoard, () => questBoardPopup.ShowFromManager(this, board, playerInventory)); // 결과 반환
    }

    public void CloseQuestBoard() // 마을 게시판 창 강제 종료 (93일차)
    {
        CloseScenePopup(GamePopupType.QuestBoard, questBoardPopup); // 종료
    }

    private bool OpenScenePopup(GamePopupType popupType, Func<bool> show) // Scene 팝업 공통 열기
    {
        if (!CanUseManager()) // 관리자 확인
        {
            return false; // 팝업 열기 실패 반환
        }

        bool popupLockAlreadyHeld = gameplayInputLock.Contains(PopupLockId); // 기존 팝업 입력 잠금 확인
        HideCurrentPopupWithoutUnlock(); // 기존 팝업 화면만 숨김

        if (!popupLockAlreadyHeld) // 기존 팝업 입력 잠금 없음 확인
        {
            gameplayInputLock.Acquire(PopupLockId); // 공통 팝업 입력 잠금 획득
        }

        if (!show()) // 팝업 표시 시도
        {
            currentPopupType = GamePopupType.None; // 현재 팝업 상태 초기화
            gameplayInputLock.Release(PopupLockId); // 공통 팝업 입력 잠금 해제
            return false; // 팝업 열기 실패 반환
        }

        currentPopupType = popupType; // 현재 팝업 종류 저장
        PopupStateChanged?.Invoke(popupType, true); // 열림 알림
        return true; // 팝업 열기 성공 반환
    }

    private void CloseScenePopup(GamePopupType popupType, IGameScenePopup popup) // Scene 팝업 공통 닫기
    {
        bool wasVisible = popup != null && popup.IsOpen; // 실제 표시 여부 확인

        if (popup != null) // 팝업 확인
        {
            popup.HideFromManager(); // 화면 숨김
        }

        if (currentPopupType != GamePopupType.None && currentPopupType != popupType) // 다른 팝업 상태 확인
        {
            return; // 다른 팝업 입력 잠금 유지
        }

        bool wasOpenState = currentPopupType == popupType || wasVisible; // 종료 여부
        currentPopupType = GamePopupType.None; // 현재 팝업 상태 초기화

        if (wasOpenState) // 입력 잠금 보유 가능성 확인
        {
            gameplayInputLock.Release(PopupLockId); // 공통 팝업 입력 잠금 해제
            PopupStateChanged?.Invoke(popupType, false); // 닫힘 알림
        }
    }

    public void CloseAllPopups() // 인벤토리·보관함·요리 팝업 모두 닫기 (85일차)
    {
        CloseInventory(); // 인벤토리 닫기
        CloseStorage(); // 보관함 닫기
        CloseCooking(); // 요리 닫기
        CloseAnimalPen(); // 우리 닫기
        CloseShop(); // 상인 닫기
        CloseNpcDialogue(); // NPC 대화 닫기 (91일차)
        CloseQuestBoard(); // 게시판 닫기 (93일차)
    }

    public void CloseCurrentPopup() // 현재 열린 팝업 종료
    {
        switch (currentPopupType) // 현재 팝업 종류 분기
        {
            case GamePopupType.Inventory: // 인벤토리 팝업 상태
                CloseInventory(); // 인벤토리 팝업 종료
                return; // 종료 처리 완료

            case GamePopupType.Storage: // 보관함 팝업 상태
                CloseStorage(); // 보관함 팝업 종료
                return; // 종료 처리 완료

            case GamePopupType.Cooking: // 요리 팝업 상태
                CloseCooking(); // 요리 팝업 종료
                return; // 종료 처리 완료

            case GamePopupType.AnimalPen: // 우리 팝업 상태
                CloseAnimalPen(); // 우리 팝업 종료
                return; // 종료 처리 완료

            case GamePopupType.Shop: // 상인 팝업 상태
                CloseShop(); // 상인 팝업 종료
                return; // 종료 처리 완료

            case GamePopupType.Dialogue: // NPC 대화 창 상태 (91일차)
                CloseNpcDialogue(); // NPC 대화 창 종료
                return; // 종료 처리 완료

            case GamePopupType.QuestBoard: // 게시판 창 상태 (93일차)
                CloseQuestBoard(); // 게시판 창 종료
                return; // 종료 처리 완료
        }

        if (inventoryPopupInstance != null && inventoryPopupInstance.IsOpen) // 실제 인벤토리 상태 확인
        {
            CloseInventory(); // 실제 인벤토리 팝업 강제 종료
            return; // 종료 처리 완료
        }

        if (storagePopupInstance != null && storagePopupInstance.IsOpen) // 실제 보관함 상태 확인
        {
            CloseStorage(); // 실제 보관함 팝업 강제 종료
        }
    }

    private InventoryPopupController GetOrCreateInventoryPopup() // 인벤토리 팝업 최초 생성 또는 기존 인스턴스 반환
    {
        if (inventoryPopupInstance != null) // 기존 인벤토리 팝업 인스턴스 확인
        {
            return inventoryPopupInstance; // 기존 인벤토리 팝업 재사용
        }

        inventoryPopupInstance = Instantiate(inventoryPopupPrefab, popupLayer, false); // 인벤토리 팝업 최초 생성

        if (inventoryPopupInstance == null) // 생성 결과 확인
        {
            Debug.LogError("인벤토리 팝업 프리팹 생성에 실패했습니다.", this); // 생성 오류 출력
            return null; // 생성 실패 반환
        }

        inventoryPopupInstance.name = inventoryPopupPrefab.name; // Clone 접미사 제거

        bool initialized = inventoryPopupInstance.Initialize(
            this,
            gameplayInputLock,
            playerInventory,
            inventoryItemDropper,
            playerEquipment,
            craftingManager); // 인벤토리 팝업 런타임 참조 전달

        if (!initialized) // 인벤토리 팝업 초기화 결과 확인
        {
            Debug.LogError("인벤토리 팝업 런타임 초기화에 실패했습니다.", inventoryPopupInstance); // 초기화 오류 출력
            Destroy(inventoryPopupInstance.gameObject); // 잘못 생성된 팝업 제거
            inventoryPopupInstance = null; // 인벤토리 인스턴스 참조 제거
            return null; // 초기화 실패 반환
        }

        inventoryPopupInstance.HideFromManager(); // 생성 직후 인벤토리 숨김
        return inventoryPopupInstance; // 준비된 인벤토리 팝업 반환
    }

    private StorageContainerUI GetOrCreateStoragePopup() // 보관함 팝업 최초 생성 또는 기존 인스턴스 반환
    {
        if (storagePopupInstance != null) // 기존 보관함 팝업 인스턴스 확인
        {
            return storagePopupInstance; // 기존 보관함 팝업 재사용
        }

        storagePopupInstance = Instantiate(storagePopupPrefab, popupLayer, false); // 보관함 팝업 최초 생성

        if (storagePopupInstance == null) // 생성 결과 확인
        {
            Debug.LogError("보관함 팝업 프리팹 생성에 실패했습니다.", this); // 생성 오류 출력
            return null; // 생성 실패 반환
        }

        storagePopupInstance.name = storagePopupPrefab.name; // Clone 접미사 제거

        if (!storagePopupInstance.Initialize(this, playerInventory)) // 런타임 참조 전달 시도
        {
            Debug.LogError("보관함 팝업 런타임 초기화에 실패했습니다.", storagePopupInstance); // 초기화 오류 출력
            Destroy(storagePopupInstance.gameObject); // 잘못 생성된 팝업 제거
            storagePopupInstance = null; // 팝업 인스턴스 참조 제거
            return null; // 초기화 실패 반환
        }

        storagePopupInstance.HideFromManager(); // 생성 직후 팝업 숨김
        return storagePopupInstance; // 준비된 보관함 팝업 반환
    }

    private void HideCurrentPopupWithoutUnlock() // 현재 팝업 화면만 숨김
    {
        GamePopupType hiddenPopupType = currentPopupType; // 숨길 팝업 종류 저장

        switch (hiddenPopupType) // 현재 팝업 종류 분기
        {
            case GamePopupType.Inventory: // 인벤토리 팝업 상태
                if (inventoryPopupInstance != null) // 인벤토리 팝업 인스턴스 확인
                {
                    inventoryPopupInstance.HideFromManager(); // 인벤토리 팝업 숨김
                }

                break; // 인벤토리 처리 종료

            case GamePopupType.Storage: // 보관함 팝업 상태
                if (storagePopupInstance != null) // 보관함 팝업 인스턴스 확인
                {
                    storagePopupInstance.HideFromManager(); // 보관함 팝업 숨김
                }

                break; // 보관함 처리 종료

            case GamePopupType.Cooking: // 요리 팝업 상태
                if (cookingPopup != null) // 요리 팝업 확인
                {
                    cookingPopup.HideFromManager(); // 요리 팝업 숨김
                }

                break; // 요리 처리 종료

            case GamePopupType.AnimalPen: // 우리 팝업 상태
                if (animalPenPopup != null) // 우리 팝업 확인
                {
                    animalPenPopup.HideFromManager(); // 우리 팝업 숨김
                }

                break; // 우리 처리 종료

            case GamePopupType.Shop: // 상인 팝업 상태
                if (shopPopup != null) // 상인 팝업 확인
                {
                    shopPopup.HideFromManager(); // 상인 팝업 숨김
                }

                break; // 상인 처리 종료

            case GamePopupType.Dialogue: // NPC 대화 창 상태 (92일차: 대화 창에서 상점 창으로 바로 넘어갈 때)
                if (npcDialoguePopup != null) // 대화 창 확인
                {
                    npcDialoguePopup.HideFromManager(); // 대화 창 숨김
                }

                break; // 대화 처리 종료

            case GamePopupType.QuestBoard: // 게시판 창 상태 (93일차)
                if (questBoardPopup != null) // 게시판 창 확인
                {
                    questBoardPopup.HideFromManager(); // 게시판 창 숨김
                }

                break; // 게시판 처리 종료
        }

        currentPopupType = GamePopupType.None; // 현재 팝업 상태 초기화

        if (hiddenPopupType != GamePopupType.None) // 실제 숨긴 팝업 확인
        {
            PopupStateChanged?.Invoke(hiddenPopupType, false); // 팝업 닫힘 알림
        }
    }

    private void RefreshAltCursorLock() // Alt 커서 입력 잠금 갱신
    {
        if (isAltCursorActive) // Alt 커서 활성 상태 확인
        {
            gameplayInputLock.Acquire(AltCursorLockId); // Alt 커서 입력 잠금 획득
            return; // 입력 잠금 획득 처리 종료
        }

        gameplayInputLock.Release(AltCursorLockId); // Alt 커서 입력 잠금 해제
    }

    private void ResolveCoreReferences() // Scene 핵심 시스템 자동 검색
    {
        if (playerInventory == null) // 플레이어 인벤토리 참조 확인
        {
            playerInventory = FindFirstObjectByType<PlayerInventory>(); // Scene 플레이어 인벤토리 검색
        }

        if (inventoryItemDropper == null) // 아이템 버리기 관리자 참조 확인
        {
            inventoryItemDropper = FindFirstObjectByType<InventoryItemDropper>(); // Scene 아이템 버리기 관리자 검색
        }

        if (playerEquipment == null) // 플레이어 장비 관리자 참조 확인
        {
            playerEquipment = FindFirstObjectByType<PlayerEquipment>(); // Scene 플레이어 장비 관리자 검색
        }

        if (craftingManager == null) // 제작 관리자 참조 확인
        {
            craftingManager = FindFirstObjectByType<CraftingManager>(); // Scene 제작 관리자 검색
        }
    }

    private bool CanUseManager() // 게임 UI 관리자 사용 가능 여부 확인
    {
        if (!enabled || !referencesValid) // 활성화와 참조 상태 확인
        {
            Debug.LogError("GameUIManager를 사용할 수 없습니다.", this); // 관리자 사용 오류 출력
            return false; // 관리자 사용 불가 반환
        }

        return true; // 관리자 사용 가능 반환
    }

    private void OnDisable() // 게임 UI 관리자 비활성화 정리
    {
        if (inventoryPopupInstance != null) // 인벤토리 팝업 존재 확인
        {
            inventoryPopupInstance.HideFromManager(); // 인벤토리 팝업 숨김
        }

        if (storagePopupInstance != null) // 보관함 팝업 존재 확인
        {
            storagePopupInstance.HideFromManager(); // 보관함 팝업 숨김
        }

        if (cookingPopup != null) // 요리 팝업 존재 확인
        {
            cookingPopup.HideFromManager(); // 요리 팝업 숨김
        }

        if (animalPenPopup != null) // 우리 팝업 존재 확인
        {
            animalPenPopup.HideFromManager(); // 우리 팝업 숨김
        }

        if (shopPopup != null) // 상인 팝업 존재 확인
        {
            shopPopup.HideFromManager(); // 상인 팝업 숨김
        }

        if (npcDialoguePopup != null) // NPC 대화 창 존재 확인 (92일차)
        {
            npcDialoguePopup.HideFromManager(); // 대화 창 숨김
        }

        if (questBoardPopup != null) // 게시판 창 존재 확인 (93일차)
        {
            questBoardPopup.HideFromManager(); // 게시판 창 숨김
        }

        currentPopupType = GamePopupType.None; // 현재 팝업 상태 초기화
        isAltCursorActive = false; // Alt 커서 상태 초기화

        if (gameplayInputLock != null) // 입력 잠금 관리자 존재 확인
        {
            gameplayInputLock.Release(PopupLockId); // 공통 팝업 입력 잠금 해제
            gameplayInputLock.Release(AltCursorLockId); // Alt 커서 입력 잠금 해제
        }
    }
}
