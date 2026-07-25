using System.Collections; // 코루틴 형식
using UnityEngine; // Unity 기본 기능
using UnityEngine.SceneManagement; // Scene 관리 기능

public sealed class SceneFlowManager : MonoBehaviour // Scene 전환 관리자
{
    public const string MainMenuSceneName = "10_MainMenu"; // 메인 메뉴 Scene 이름
    public const string GameplaySceneName = "20_Gameplay"; // 게임 플레이 Scene 이름

    public static SceneFlowManager Instance { get; private set; } // 전역 관리자 참조

    private bool isLoading; // Scene 전환 진행 여부

    private void Awake() // 관리자 초기화
    {
        if (Instance != null && Instance != this) // 중복 관리자 확인
        {
            Destroy(gameObject); // 중복 오브젝트 제거
            return; // 초기화 중단
        }

        Instance = this; // 현재 관리자 등록
        DontDestroyOnLoad(gameObject); // Scene 전환 후 유지
    }

    public void LoadMainMenu() // 메인 메뉴 이동
    {
        LoadScene(MainMenuSceneName); // 메인 메뉴 전환 요청
    }

    public void LoadGameplay() // 게임 플레이 이동
    {
        LoadScene(GameplaySceneName); // 게임 플레이 전환 요청
    }

    private void LoadScene(string sceneName) // 공통 Scene 전환
    {
        if (isLoading) // 중복 전환 확인
        {
            return; // 중복 요청 중단
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName)) // Scene 등록 여부 확인
        {
            Debug.LogError($"Scene을 불러올 수 없습니다: {sceneName}", this); // 등록 오류 출력
            return; // 전환 중단
        }

        StartCoroutine(LoadSceneRoutine(sceneName)); // 비동기 전환 시작
    }

    private IEnumerator LoadSceneRoutine(string sceneName) // 비동기 Scene 전환 처리
    {
        isLoading = true; // 전환 상태 활성화

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single); // Scene 비동기 로드

        while (!loadOperation.isDone) // 로드 완료 전 반복
        {
            yield return null; // 다음 프레임 대기
        }

        isLoading = false; // 전환 상태 해제
    }
}