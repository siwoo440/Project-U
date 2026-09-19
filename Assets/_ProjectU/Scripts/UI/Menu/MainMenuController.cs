using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.InputSystem; // 키보드 입력
using UnityEngine.UI; // Unity UI 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class MainMenuController : MonoBehaviour // 99일차: 메인 메뉴 (새 게임 · 이어하기 · 설정 · 게임 종료)
{
    [Header("Buttons")]
    [SerializeField] private Button newGameButton; // 새 게임
    [SerializeField] private Button continueButton; // 이어하기
    [SerializeField] private Button settingsButton; // 설정
    [SerializeField] private Button quitButton; // 게임 종료

    [Header("Info")]
    [SerializeField] private TMP_Text saveInfoText; // 저장 정보
    [SerializeField] private TMP_Text versionText; // 버전

    [Header("Windows")]
    [SerializeField] private SettingsWindowUI settingsWindow; // 설정 창
    [SerializeField] private GameObject confirmRoot; // 새 게임 확인 창
    [SerializeField] private TMP_Text confirmText; // 확인 문구
    [SerializeField] private Button confirmYesButton; // 새로 시작
    [SerializeField] private Button confirmNoButton; // 취소

    private bool hasSave; // 이어할 저장이 있는지
    private int savedDay; // 저장된 날짜

    public bool HasSave => hasSave; // 테스트용
    public bool IsConfirmOpen => confirmRoot != null && confirmRoot.activeSelf; // 테스트용
    public SettingsWindowUI SettingsWindow => settingsWindow; // 테스트용
    public string SaveInfo => saveInfoText != null ? saveInfoText.text : string.Empty; // 테스트용

    private void Awake() // 버튼 연결
    {
        if (newGameButton != null) newGameButton.onClick.AddListener(OnNewGameClicked);
        if (continueButton != null) continueButton.onClick.AddListener(OnContinueClicked);
        if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsClicked);
        if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);
        if (confirmYesButton != null) confirmYesButton.onClick.AddListener(StartNewGame);
        if (confirmNoButton != null) confirmNoButton.onClick.AddListener(CloseConfirm);

        if (confirmRoot != null)
        {
            confirmRoot.SetActive(false);
        }
    }

    private void Start() // 메뉴 상태
    {
        Cursor.lockState = CursorLockMode.None; // 메뉴에서는 커서 사용
        Cursor.visible = true;
        Time.timeScale = 1f;

        if (versionText != null)
        {
            versionText.text = $"ALPHA {Application.version}";
        }

        RefreshSaveState();
    }

    private void Update() // ESC : 열린 창 닫기
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame)
        {
            return;
        }

        if (settingsWindow != null && settingsWindow.IsOpen)
        {
            settingsWindow.Close();
        }
        else if (IsConfirmOpen)
        {
            CloseConfirm();
        }
    }

    public void RefreshSaveState() // 저장 파일에 따라 이어하기 · 저장 정보
    {
        hasSave = SaveFileService.TryLoad(SaveFileService.DefaultSlotId, out SaveGameData saveData, out bool fromBackup, out _);

        if (continueButton != null)
        {
            continueButton.interactable = hasSave;
        }

        if (saveInfoText == null)
        {
            return;
        }

        if (!hasSave)
        {
            saveInfoText.text = "NO SAVED GAME";
            return;
        }

        float hourValue = Mathf.Repeat(saveData.time.currentHour, 24f);
        int hour = Mathf.FloorToInt(hourValue);
        int minute = Mathf.FloorToInt((hourValue - hour) * 60f);
        savedDay = saveData.time.currentDay;
        saveInfoText.text = $"{(fromBackup ? "BACKUP SAVE" : "LAST SAVE")}  ·  DAY {savedDay}  ·  {hour:00}:{minute:00}";
    }

    private bool CanNavigate() => SceneFlowManager.Instance != null && !SceneFlowManager.Instance.IsLoading; // Scene 이동 가능 여부

    public void OnNewGameClicked() // 새 게임 (저장이 있으면 확인)
    {
        if (hasSave && confirmRoot != null)
        {
            if (confirmText != null)
            {
                confirmText.text = $"You have a saved game (DAY {savedDay}).\nStarting a new game will replace it the next time you save.";
            }

            confirmRoot.SetActive(true);
            return;
        }

        StartNewGame();
    }

    public void StartNewGame() // 새 게임 시작
    {
        CloseConfirm();

        if (!CanNavigate())
        {
            Debug.LogWarning("Scene 이동 관리자가 없거나 이미 이동 중입니다. 00_Bootstrap Scene에서 시작하세요.", this);
            return;
        }

        SceneFlowManager.Instance.LoadGameplay();
    }

    public void OnContinueClicked() // 이어하기
    {
        RefreshSaveState();

        if (!hasSave || !CanNavigate())
        {
            return;
        }

        SceneFlowManager.Instance.LoadSavedGameplay();
    }

    public void OnSettingsClicked() // 설정 창
    {
        CloseConfirm();

        if (settingsWindow != null)
        {
            settingsWindow.Show(null);
        }
    }

    public void OnQuitClicked() // 게임 종료
    {
#if UNITY_EDITOR
        Debug.Log("Unity Editor에서는 Application.Quit이 실행 파일을 종료하지 않습니다.", this);
#endif
        Application.Quit();
    }

    private void CloseConfirm()
    {
        if (confirmRoot != null)
        {
            confirmRoot.SetActive(false);
        }
    }

#if UNITY_EDITOR
    public void EditorAssign(Button newGame, Button continueGame, Button settings, Button quit, TMP_Text saveInfo, TMP_Text version, SettingsWindowUI window, GameObject confirm, TMP_Text confirmMessage, Button yes, Button no) // 생성 도구 전용
    {
        newGameButton = newGame;
        continueButton = continueGame;
        settingsButton = settings;
        quitButton = quit;
        saveInfoText = saveInfo;
        versionText = version;
        settingsWindow = window;
        confirmRoot = confirm;
        confirmText = confirmMessage;
        confirmYesButton = yes;
        confirmNoButton = no;
    }
#endif
}
