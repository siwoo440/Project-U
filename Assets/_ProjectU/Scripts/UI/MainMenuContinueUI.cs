using TMPro; // TextMeshPro UI 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // 버튼 UI 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class MainMenuContinueUI : MonoBehaviour // 메인 메뉴 이어하기 UI 관리
{
    [Header("References")] // 외부 참조 묶음
    [SerializeField] private Button continueButton; // 이어하기 버튼
    [SerializeField] private TMP_Text saveInfoText; // 저장 정보 표시 문구

    private void Awake() // 이어하기 UI 참조 초기화
    {
        if (continueButton == null || saveInfoText == null) // 필수 UI 참조 확인
        {
            Debug.LogError("MainMenuContinueUI의 버튼과 저장 정보 문구를 연결해야 합니다.", this); // 참조 누락 오류
            enabled = false; // 이어하기 UI 비활성화
        }
    }

    private void Start() // 메인 메뉴 시작 상태 적용
    {
        RefreshSaveState(); // 저장 파일에 따른 버튼 상태 갱신
    }

    public void OnContinueButtonClicked() // 이어하기 버튼 입력 처리
    {
        bool canLoadSave = SaveFileService.TryLoad(
            SaveFileService.DefaultSlotId,
            out _,
            out _,
            out string errorMessage); // 버튼 입력 시 저장 파일 재검사

        if (!canLoadSave) // 정상 저장 파일 존재 여부 확인
        {
            Debug.LogWarning($"이어갈 수 있는 저장 파일이 없습니다.\n{errorMessage}", this); // 이어하기 실패 안내
            RefreshSaveState(); // 버튼과 문구 상태 다시 갱신
            return; // 버튼 처리 중단
        }

        if (SceneFlowManager.Instance == null) // Scene 관리자 존재 여부 확인
        {
            Debug.LogError("SceneFlowManager를 찾을 수 없습니다.", this); // 관리자 누락 오류
            return; // Scene 이동 중단
        }

        SceneFlowManager.Instance.LoadSavedGameplay(); // 저장된 게임 Scene 이동 요청
    }

    private void RefreshSaveState() // 저장 파일 정보와 버튼 상태 갱신
    {
        bool canContinue = SaveFileService.TryLoad(
            SaveFileService.DefaultSlotId,
            out SaveGameData saveData,
            out bool loadedFromBackup,
            out _); // 저장 데이터와 사용 파일 확인

        continueButton.interactable = canContinue; // 이어하기 버튼 활성 상태 적용

        if (!canContinue) // 정상 저장 데이터 없음 확인
        {
            saveInfoText.text = "NO SAVE DATA"; // 저장 없음 문구 표시
            return; // 정보 갱신 종료
        }

        float normalizedHour = Mathf.Repeat(
            saveData.time.currentHour,
            24f); // 저장 시간을 하루 범위로 보정

        int hour = Mathf.FloorToInt(normalizedHour); // 저장 시간의 시 계산
        int minute = Mathf.FloorToInt(
            (normalizedHour - hour) * 60f); // 저장 시간의 분 계산

        string sourceLabel = loadedFromBackup
            ? "BACKUP"
            : "SAVE"; // 기본 파일과 백업 파일 문구 선택

        saveInfoText.text =
            $"{sourceLabel} / DAY {saveData.time.currentDay} / {hour:00}:{minute:00}"; // 저장 정보 화면 표시
    }
}