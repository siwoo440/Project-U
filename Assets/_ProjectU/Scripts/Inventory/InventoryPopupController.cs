using System; // 이벤트 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.InputSystem; // 새로운 입력 시스템

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class InventoryPopupController : MonoBehaviour // 인벤토리 팝업 관리자
{
    private const string AltCursorLockId = "InventoryPopupController.AltCursor"; // Alt 커서 입력 잠금 ID
    private const string FallbackPopupLockId = "InventoryPopupController.PopupFallback"; // 독립 실행 팝업 입력 잠금 ID

    [SerializeField] private GameObject popupPanel; // 전체 인벤토리 팝업

    private GameUIManager gameUIManager; // 공통 게임 UI 관리자
    private GameplayInputLock gameplayInputLock; // 공통 입력 잠금 관리자
    private bool isAltCursorActive; // Alt 커서 활성 상태
    private bool isInitialized; // 공통 관리자 초기화 상태

    public bool IsOpen { get; private set; } // 팝업 열림 상태 제공
    public event Action<bool> OpenStateChanged; // 팝업 상태 변경 알림

    private void Awake() // 인벤토리 팝업 초기화
    {
        if (popupPanel == null) // 팝업 연결 확인
        {
            Debug.LogError("InventoryPopupController의 Popup Panel을 연결해야 합니다.", this); // 참조 오류 출력
            enabled = false; // 팝업 기능 비활성화
            return; // 초기화 중단
        }

        popupPanel.SetActive(false); // 시작 팝업 숨김
        IsOpen = false; // 시작 팝업 상태 저장
        isAltCursorActive = false; // 시작 Alt 상태 저장
    }

    private void Start() // 공통 관리자 자동 검색
    {
        ResolveManagers(); // 게임 UI와 입력 잠금 관리자 검색
    }

    private void Update() // 팝업 입력 검사
    {
        Keyboard keyboard = Keyboard.current; // 현재 키보드 조회

        if (keyboard == null) // 키보드 존재 확인
        {
            return; // 입력 검사 중단
        }

        bool currentAltState = keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed; // 현재 Alt 입력 확인

        if (isAltCursorActive != currentAltState) // Alt 상태 변경 확인
        {
            isAltCursorActive = currentAltState; // 새로운 Alt 상태 저장
            RefreshAltCursorLock(); // Alt 커서 입력 잠금 갱신
        }

        if (IsOpen && keyboard.escapeKey.wasPressedThisFrame) // 열린 상태 ESC 입력 확인
        {
            SetOpen(false); // 인벤토리 팝업 닫기
            return; // 같은 프레임 추가 입력 차단
        }

        if (keyboard.iKey.wasPressedThisFrame) // I 키 입력 확인
        {
            SetOpen(!IsOpen); // 인벤토리 팝업 상태 반전
        }
    }

    public void Initialize(GameUIManager manager, GameplayInputLock inputLock) // 공통 UI 시스템 참조 전달
    {
        gameUIManager = manager; // 공통 게임 UI 관리자 저장
        gameplayInputLock = inputLock; // 공통 입력 잠금 관리자 저장
        isInitialized = gameUIManager != null && gameplayInputLock != null; // 공통 관리자 초기화 상태 저장
    }

    public void SetOpen(bool shouldOpen) // 외부 요청으로 팝업 상태 변경
    {
        ResolveManagers(); // 공통 관리자 참조 확인

        if (gameUIManager != null) // 공통 게임 UI 관리자 확인
        {
            if (shouldOpen) // 팝업 열기 요청 확인
            {
                gameUIManager.OpenInventory(); // 공통 관리자에서 인벤토리 열기
            }
            else // 팝업 닫기 요청
            {
                gameUIManager.CloseInventory(); // 공통 관리자에서 인벤토리 닫기
            }

            return; // 공통 관리자 처리 종료
        }

        SetOpenWithoutManager(shouldOpen); // 공통 관리자 없는 상태에서 팝업 변경
    }

    public bool ShowFromManager() // 공통 관리자에서 인벤토리 표시
    {
        if (popupPanel == null) // 팝업 패널 확인
        {
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
        if (!IsOpen && (popupPanel == null || !popupPanel.activeSelf)) // 이미 닫힌 상태 확인
        {
            return; // 중복 숨김 처리 생략
        }

        IsOpen = false; // 팝업 닫힘 상태 저장

        if (popupPanel != null) // 팝업 패널 존재 확인
        {
            popupPanel.SetActive(false); // 팝업 화면 숨김
        }

        OpenStateChanged?.Invoke(false); // 팝업 닫힘 알림
    }

    private void SetOpenWithoutManager(bool shouldOpen) // 공통 관리자 없는 상태의 팝업 변경
    {
        ResolveManagers(); // 입력 잠금 관리자 참조 확인

        if (shouldOpen) // 팝업 열기 요청 확인
        {
            if (ShowFromManager() && gameplayInputLock != null) // 팝업 표시와 입력 잠금 관리자 확인
            {
                gameplayInputLock.Acquire(FallbackPopupLockId); // 독립 실행 팝업 입력 잠금 획득
            }

            return; // 팝업 열기 처리 종료
        }

        HideFromManager(); // 팝업 화면 숨김

        if (gameplayInputLock != null) // 입력 잠금 관리자 확인
        {
            gameplayInputLock.Release(FallbackPopupLockId); // 독립 실행 팝업 입력 잠금 해제
        }
    }

    private void RefreshAltCursorLock() // Alt 커서 입력 잠금 갱신
    {
        ResolveManagers(); // 입력 잠금 관리자 참조 확인

        if (gameplayInputLock == null) // 입력 잠금 관리자 확인
        {
            Cursor.lockState = isAltCursorActive ? CursorLockMode.None : CursorLockMode.Locked; // Alt 상태에 맞는 커서 고정 적용
            Cursor.visible = isAltCursorActive; // Alt 상태에 맞는 커서 표시 적용
            return; // 입력 잠금 처리 종료
        }

        if (isAltCursorActive) // Alt 커서 활성 상태 확인
        {
            gameplayInputLock.Acquire(AltCursorLockId); // Alt 커서 입력 잠금 획득
            return; // 입력 잠금 획득 처리 종료
        }

        gameplayInputLock.Release(AltCursorLockId); // Alt 커서 입력 잠금 해제
    }

    private void ResolveManagers() // 공통 관리자 자동 검색
    {
        if (gameUIManager == null) // 게임 UI 관리자 참조 확인
        {
            gameUIManager = FindFirstObjectByType<GameUIManager>(); // Scene 게임 UI 관리자 검색
        }

        if (gameplayInputLock == null) // 입력 잠금 관리자 참조 확인
        {
            gameplayInputLock = gameUIManager != null // 게임 UI 관리자 존재 여부 확인
                ? gameUIManager.InputLock // 게임 UI 관리자에서 입력 잠금 조회
                : FindFirstObjectByType<GameplayInputLock>(); // Scene 입력 잠금 관리자 검색
        }

        isInitialized = gameUIManager != null && gameplayInputLock != null; // 공통 관리자 초기화 상태 갱신
    }

    private void OnDisable() // 인벤토리 팝업 비활성화 정리
    {
        bool wasOpen = IsOpen; // 기존 팝업 열림 상태 저장
        IsOpen = false; // 팝업 닫힘 상태 저장
        isAltCursorActive = false; // Alt 커서 상태 해제

        if (popupPanel != null) // 팝업 패널 존재 확인
        {
            popupPanel.SetActive(false); // 팝업 화면 숨김
        }

        if (gameplayInputLock != null) // 입력 잠금 관리자 확인
        {
            gameplayInputLock.Release(AltCursorLockId); // Alt 커서 입력 잠금 해제
            gameplayInputLock.Release(FallbackPopupLockId); // 독립 실행 팝업 입력 잠금 해제
        }

        if (wasOpen) // 기존 팝업 열림 확인
        {
            OpenStateChanged?.Invoke(false); // 팝업 닫힘 알림
        }
    }
}
