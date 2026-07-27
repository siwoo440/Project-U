using UnityEngine; // Unity 기본 기능
using UnityEngine.SceneManagement; // Scene 관리 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class PlayerDeathUI : MonoBehaviour // 플레이어 사망 화면 관리
{
    [Header("References")] // 외부 참조 묶음
    [SerializeField] private PlayerHealth playerHealth; // 플레이어 체력
    [SerializeField] private InventoryPopupController inventoryPopupController; // 인벤토리 팝업 관리자
    [SerializeField] private GameObject deathPanel; // 사망 화면 패널
    [SerializeField] private Behaviour[] blockedBehaviours; // 사망 시 중지할 기능

    [Header("Runtime")] // 실행 상태 묶음
    [SerializeField] private bool isDeathScreenShown; // 사망 화면 표시 상태

    private void Awake() // 사망 화면 초기화
    {
        if (playerHealth == null || deathPanel == null) // 필수 참조 확인
        {
            Debug.LogError($"{gameObject.name}의 사망 화면 참조가 누락되었습니다.", this); // 참조 오류 출력
            enabled = false; // 사망 화면 기능 비활성화
            return; // 초기화 처리 중단
        }

        Time.timeScale = 1f; // 게임 시간 정상화
        deathPanel.SetActive(false); // 시작 사망 화면 숨김
        isDeathScreenShown = false; // 시작 표시 상태 초기화
    }

    private void Update() // 사망 상태 검사
    {
        if (isDeathScreenShown) // 기존 화면 표시 확인
        {
            return; // 중복 처리 차단
        }

        if (!playerHealth.IsDead) // 생존 상태 확인
        {
            return; // 사망 처리 대기
        }

        ShowDeathScreen(); // 사망 화면 표시
    }

    private void ShowDeathScreen() // 사망 상태 적용
    {
        isDeathScreenShown = true; // 사망 화면 표시 상태 저장

        if (inventoryPopupController != null && inventoryPopupController.IsOpen) // 열린 인벤토리 확인
        {
            inventoryPopupController.SetOpen(false); // 인벤토리 팝업 닫기
        }

        DisableBlockedBehaviours(); // 플레이 기능 중지
        deathPanel.SetActive(true); // 사망 화면 표시
        Cursor.lockState = CursorLockMode.None; // 마우스 잠금 해제
        Cursor.visible = true; // 마우스 포인터 표시
        Time.timeScale = 0f; // 게임 시간 정지
    }

    private void DisableBlockedBehaviours() // 플레이 기능 중지
    {
        if (blockedBehaviours == null) // 차단 목록 확인
        {
            return; // 기능 중지 처리 종료
        }

        for (int index = 0; index < blockedBehaviours.Length; index++) // 차단 목록 순회
        {
            Behaviour targetBehaviour = blockedBehaviours[index]; // 현재 대상 가져오기

            if (targetBehaviour == null) // 빈 참조 확인
            {
                continue; // 빈 대상 건너뛰기
            }

            targetBehaviour.enabled = false; // 대상 기능 비활성화
        }
    }

    public void RetryCurrentScene() // 현재 게임 Scene 재시작
    {
        Time.timeScale = 1f; // 게임 시간 정상화

        Scene currentScene = SceneManager.GetActiveScene(); // 현재 Scene 정보 가져오기

        if (currentScene.buildIndex < 0) // Build Profile 등록 확인
        {
            Debug.LogError("현재 Scene을 Build Profile의 Scene List에 추가해야 합니다.", this); // Scene 등록 오류 출력
            return; // Scene 재시작 중단
        }

        SceneManager.LoadScene(currentScene.buildIndex); // 현재 Scene 다시 불러오기
    }

    private void OnDestroy() // 컴포넌트 제거 상태 정리
    {
        if (isDeathScreenShown) // 사망 화면 표시 상태 확인
        {
            Time.timeScale = 1f; // 게임 시간 정상화
        }
    }
}