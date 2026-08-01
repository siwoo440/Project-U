using UnityEngine; // Unity 기본 기능

public enum GamePopupType // 게임 팝업 종류
{
    None = 0, // 열린 팝업 없음
    Inventory = 1, // 일반 인벤토리 팝업
    Storage = 2 // 보관함 팝업
}

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class GameUIManager : MonoBehaviour // 게임 팝업 생성과 실행 순서 관리자
{
    private const string PopupLockId = "GameUIManager.Popup"; // 공통 팝업 입력 잠금 ID

    [Header("Core")] // 핵심 관리자 참조 묶음
    [SerializeField] private GameplayInputLock gameplayInputLock; // 게임 플레이 입력 잠금 관리자
    [SerializeField] private PlayerInventory playerInventory; // 팝업에 전달할 플레이어 인벤토리

    [Header("Scene Popup Instances")] // Scene에 유지할 팝업 참조 묶음
    [SerializeField] private InventoryPopupController inventoryPopupController; // 일반 인벤토리 팝업 관리자

    [Header("Runtime Popup Layer")] // 런타임 팝업 배치 설정 묶음
    [SerializeField] private Transform popupLayer; // 동적 팝업 생성 부모

    [Header("Runtime Popup Prefabs")] // 런타임 팝업 프리팹 설정 묶음
    [SerializeField] private StorageContainerUI storagePopupPrefab; // 보관함 팝업 프리팹

    private StorageContainerUI storagePopupInstance; // 생성된 보관함 팝업 인스턴스
    private GamePopupType currentPopupType = GamePopupType.None; // 현재 열린 팝업 종류
    private bool referencesValid; // 필수 참조 연결 상태

    public GamePopupType CurrentPopupType => currentPopupType; // 현재 열린 팝업 종류 제공
    public bool HasOpenPopup => currentPopupType != GamePopupType.None; // 현재 팝업 열림 여부 제공
    public bool HasStoragePopupInstance => storagePopupInstance != null; // 보관함 팝업 생성 여부 제공
    public GameplayInputLock InputLock => gameplayInputLock; // 공통 입력 잠금 관리자 제공
    public StorageContainerUI StoragePopupInstance => storagePopupInstance; // 생성된 보관함 팝업 제공

    private void Awake() // 게임 UI 관리자 초기화
    {
        if (playerInventory == null) // 플레이어 인벤토리 참조 확인
        {
            playerInventory = FindFirstObjectByType<PlayerInventory>(); // Scene 플레이어 인벤토리 검색
        }

        referencesValid = gameplayInputLock != null // 입력 잠금 관리자 확인
            && playerInventory != null // 플레이어 인벤토리 확인
            && inventoryPopupController != null // 인벤토리 팝업 확인
            && popupLayer != null // 팝업 생성 부모 확인
            && storagePopupPrefab != null; // 보관함 팝업 프리팹 확인

        if (!referencesValid) // 필수 참조 누락 확인
        {
            Debug.LogError($"{gameObject.name}의 GameUIManager 참조가 누락되었습니다.", this); // 참조 오류 출력
            enabled = false; // 게임 UI 관리자 비활성화
            return; // 초기화 중단
        }

        inventoryPopupController.Initialize(this, gameplayInputLock); // 인벤토리 팝업 공통 관리자 연결
        inventoryPopupController.HideFromManager(); // 인벤토리 팝업 초기 숨김
        currentPopupType = GamePopupType.None; // 시작 팝업 상태 초기화
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

        if (currentPopupType == GamePopupType.Inventory) // 동일 팝업 열림 확인
        {
            return true; // 기존 열림 상태 반환
        }

        bool popupLockAlreadyHeld = gameplayInputLock.Contains(PopupLockId); // 기존 팝업 입력 잠금 확인
        HideCurrentPopupWithoutUnlock(); // 기존 팝업 화면만 숨김

        if (!popupLockAlreadyHeld) // 기존 팝업 입력 잠금 없음 확인
        {
            gameplayInputLock.Acquire(PopupLockId); // 공통 팝업 입력 잠금 획득
        }

        if (!inventoryPopupController.ShowFromManager()) // 인벤토리 팝업 표시 시도
        {
            currentPopupType = GamePopupType.None; // 현재 팝업 상태 초기화
            gameplayInputLock.Release(PopupLockId); // 공통 팝업 입력 잠금 해제
            return false; // 팝업 열기 실패 반환
        }

        currentPopupType = GamePopupType.Inventory; // 현재 팝업 종류 저장
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
        return true; // 팝업 열기 성공 반환
    }

    public void CloseInventory() // 인벤토리 팝업 닫기
    {
        if (inventoryPopupController != null) // 인벤토리 팝업 참조 확인
        {
            inventoryPopupController.HideFromManager(); // 인벤토리 팝업 강제 숨김
        }

        if (currentPopupType != GamePopupType.Inventory) // 현재 팝업 종류 확인
        {
            return; // 다른 팝업의 입력 잠금 유지
        }

        currentPopupType = GamePopupType.None; // 현재 팝업 상태 초기화
        gameplayInputLock.Release(PopupLockId); // 공통 팝업 입력 잠금 해제
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

        if (currentPopupType == GamePopupType.Inventory) // 일반 인벤토리 팝업 상태 확인
        {
            return; // 인벤토리 입력 잠금 유지
        }

        bool shouldReleasePopupLock =
            currentPopupType == GamePopupType.Storage
            || wasStorageVisible; // 보관함 종료에 따른 입력 잠금 해제 여부 계산

        currentPopupType = GamePopupType.None; // 현재 팝업 상태 초기화

        if (shouldReleasePopupLock) // 보관함 입력 잠금 보유 가능성 확인
        {
            gameplayInputLock.Release(PopupLockId); // 공통 팝업 입력 잠금 해제
        }
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
        }

        if (storagePopupInstance != null && storagePopupInstance.IsOpen) // 관리자 상태와 실제 보관함 상태 불일치 확인
        {
            CloseStorage(); // 실제 보관함 화면 강제 종료
            return; // 종료 처리 완료
        }

        if (inventoryPopupController != null && inventoryPopupController.IsOpen) // 관리자 상태와 실제 인벤토리 상태 불일치 확인
        {
            inventoryPopupController.HideFromManager(); // 실제 인벤토리 화면 강제 종료
            gameplayInputLock.Release(PopupLockId); // 공통 팝업 입력 잠금 해제
        }
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
        switch (currentPopupType) // 현재 팝업 종류 분기
        {
            case GamePopupType.Inventory: // 인벤토리 팝업 상태
                inventoryPopupController.HideFromManager(); // 인벤토리 팝업 숨김
                break; // 인벤토리 처리 종료

            case GamePopupType.Storage: // 보관함 팝업 상태
                if (storagePopupInstance != null) // 보관함 팝업 인스턴스 확인
                {
                    storagePopupInstance.HideFromManager(); // 보관함 팝업 숨김
                }

                break; // 보관함 처리 종료
        }

        currentPopupType = GamePopupType.None; // 현재 팝업 상태 초기화
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
        if (inventoryPopupController != null) // 인벤토리 팝업 존재 확인
        {
            inventoryPopupController.HideFromManager(); // 인벤토리 팝업 숨김
        }

        if (storagePopupInstance != null) // 보관함 팝업 인스턴스 확인
        {
            storagePopupInstance.HideFromManager(); // 보관함 팝업 숨김
        }

        currentPopupType = GamePopupType.None; // 현재 팝업 상태 초기화

        if (gameplayInputLock != null) // 입력 잠금 관리자 존재 확인
        {
            gameplayInputLock.Release(PopupLockId); // 공통 팝업 입력 잠금 해제
        }
    }
}
