using System.Collections; // 코루틴 형식
using UnityEngine; // Unity 기본 기능
using UnityEngine.SceneManagement; // Scene 관리 기능

public sealed class SceneFlowManager : MonoBehaviour // Scene 전환 관리자
{
    public const string BootstrapSceneName = "00_Bootstrap"; // 초기화 Scene 이름
    public const string MainMenuSceneName = "10_MainMenu"; // 메인 메뉴 Scene 이름
    public const string GameplaySceneName = "20_Gameplay"; // 게임 플레이 Scene 이름

    public static SceneFlowManager Instance { get; private set; } // 전역 관리자 참조

    private bool isLoading; // Scene 전환 진행 여부
    private bool shouldLoadSavedGame; // Gameplay 이동 후 저장 데이터 적용 여부

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
        shouldLoadSavedGame = false; // 남아 있는 이어하기 요청 해제
        LoadScene(MainMenuSceneName); // 메인 메뉴 전환 요청
    }

    public void LoadGameplay() // 새 게임 플레이 이동
    {
        shouldLoadSavedGame = false; // 저장 데이터 불러오기 해제
        LoadScene(GameplaySceneName); // 게임 플레이 전환 요청
    }

    public void LoadSavedGameplay() // 저장된 게임 플레이 이동
    {
        bool canLoadSave = SaveFileService.TryLoad(
            SaveFileService.DefaultSlotId,
            out _,
            out _,
            out string resultMessage); // 저장 파일 유효성 검사

        if (!canLoadSave) // 정상 저장 파일 존재 여부 확인
        {
            Debug.LogWarning($"이어갈 수 있는 저장 파일이 없습니다.\n{resultMessage}", this); // 이어하기 실패 안내
            return; // Scene 이동 중단
        }

        shouldLoadSavedGame = true; // Gameplay 이동 후 저장 적용 요청
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

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(
            sceneName,
            LoadSceneMode.Single); // 단일 Scene 비동기 로드

        while (!loadOperation.isDone) // 로드 완료 전 반복
        {
            yield return null; // 다음 프레임 대기
        }

        bool mustLoadSavedGame = shouldLoadSavedGame
            && sceneName == GameplaySceneName; // 저장 데이터 적용 조건 계산

        shouldLoadSavedGame = false; // 이어하기 요청 사용 완료
        isLoading = false; // Scene 전환 상태 해제

        if (!mustLoadSavedGame) // 새 게임 또는 다른 Scene 확인
        {
            yield break; // 자동 불러오기 없이 종료
        }

        yield return null; // Gameplay Scene 초기화 한 프레임 대기

        GameplaySaveController saveController =
            UnityEngine.Object.FindFirstObjectByType<GameplaySaveController>(); // Gameplay 저장 관리자 검색

        if (saveController == null) // 저장 관리자 검색 결과 확인
        {
            Debug.LogError("GameplaySaveController를 찾을 수 없습니다.", this); // Scene 구성 오류 출력
            yield break; // 자동 불러오기 중단
        }

        saveController.LoadCurrentGame(); // 저장된 게임 상태 자동 복원
    }
}