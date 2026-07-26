using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능

public sealed class GameStateView : MonoBehaviour // 게임 상태 표시 UI
{
    [SerializeField] private TMP_Text stateText; // 상태 표시 텍스트

    private void OnEnable() // UI 활성화 처리
    {
        if (stateText == null) // 텍스트 연결 여부 확인
        {
            Debug.LogError("State Text가 연결되지 않았습니다.", this); // 연결 오류 출력
            return; // 활성화 처리 중단
        }

        if (GameManager.Instance == null) // 게임 관리자 존재 여부 확인
        {
            stateText.text = "STATE: MANAGER MISSING"; // 관리자 누락 표시
            Debug.LogError("GameManager를 찾을 수 없습니다.", this); // 관리자 누락 오류
            return; // 활성화 처리 중단
        }

        GameManager.Instance.StateChanged += RefreshState; // 상태 변경 이벤트 등록
        RefreshState(GameManager.Instance.CurrentState); // 현재 상태 즉시 표시
    }

    private void OnDisable() // UI 비활성화 처리
    {
        if (GameManager.Instance == null) // 게임 관리자 존재 여부 확인
        {
            return; // 이벤트 해제 중단
        }

        GameManager.Instance.StateChanged -= RefreshState; // 상태 변경 이벤트 해제
    }

    private void RefreshState(GameState gameState) // 상태 텍스트 갱신
    {
        stateText.text = $"STATE: {GetStateLabel(gameState)}"; // 상태 문구 적용
    }

    private string GetStateLabel(GameState gameState) // 상태 표시 문구 반환
    {
        switch (gameState) // 게임 상태 분기
        {
            case GameState.Bootstrap: // 초기화 상태 확인
                return "BOOTSTRAP"; // 초기화 문구 반환

            case GameState.MainMenu: // 메인 메뉴 상태 확인
                return "MAIN MENU"; // 메인 메뉴 문구 반환

            case GameState.Gameplay: // 게임 플레이 상태 확인
                return "GAMEPLAY"; // 게임 플레이 문구 반환

            default: // 미지정 상태 처리
                return "NONE"; // 미지정 문구 반환
        }
    }
}