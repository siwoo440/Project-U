using System; // 이벤트 형식
using UnityEngine; // Unity 기본 기능
using UnityEngine.SceneManagement; // Scene 관리 기능

public sealed class GameManager : MonoBehaviour // 게임 전역 상태 관리자
{
    public static GameManager Instance { get; private set; } // 전역 관리자 참조

    public GameState CurrentState { get; private set; } = GameState.None; // 현재 게임 상태

    public event Action<GameState> StateChanged; // 상태 변경 이벤트

    private void Awake() // 관리자 초기화
    {
        if (Instance != null && Instance != this) // 중복 관리자 확인
        {
            Destroy(gameObject); // 중복 오브젝트 제거
            return; // 초기화 중단
        }

        Instance = this; // 현재 관리자 등록
        DontDestroyOnLoad(gameObject); // Scene 전환 후 유지
        SceneManager.sceneLoaded += OnSceneLoaded; // Scene 로드 이벤트 등록
        UpdateState(SceneManager.GetActiveScene()); // 최초 상태 설정
    }

    private void OnDestroy() // 관리자 제거 처리
    {
        if (Instance != this) // 현재 관리자 여부 확인
        {
            return; // 중복 오브젝트 처리 중단
        }

        SceneManager.sceneLoaded -= OnSceneLoaded; // Scene 로드 이벤트 해제
        Instance = null; // 전역 관리자 참조 해제
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode _) // Scene 로드 완료 처리
    {
        UpdateState(scene); // 현재 Scene 기준 상태 변경
    }

    private void UpdateState(Scene scene) // Scene 상태 변환
    {
        GameState nextState = ResolveState(scene.name); // 다음 상태 결정
        SetState(nextState); // 상태 적용
    }

    private GameState ResolveState(string sceneName) // Scene 이름별 상태 결정
    {
        switch (sceneName) // Scene 이름 분기
        {
            case SceneFlowManager.BootstrapSceneName: // Bootstrap Scene 확인
                return GameState.Bootstrap; // 초기화 상태 반환

            case SceneFlowManager.MainMenuSceneName: // 메인 메뉴 Scene 확인
                return GameState.MainMenu; // 메인 메뉴 상태 반환

            case SceneFlowManager.GameplaySceneName: // 게임 플레이 Scene 확인
                return GameState.Gameplay; // 게임 플레이 상태 반환

            default: // 등록되지 않은 Scene 처리
                return GameState.None; // 미지정 상태 반환
        }
    }

    private void SetState(GameState nextState) // 게임 상태 적용
    {
        if (CurrentState == nextState) // 동일 상태 확인
        {
            return; // 중복 변경 중단
        }

        CurrentState = nextState; // 현재 상태 변경
        Debug.Log($"게임 상태 변경: {CurrentState}", this); // 상태 변경 기록
        StateChanged?.Invoke(CurrentState); // 상태 변경 이벤트 실행
    }
}