using UnityEngine; // Unity 기본 기능

public sealed class SceneNavigationUI : MonoBehaviour // Scene 이동 UI 처리
{
    public void OnStartButtonClicked() // 시작 버튼 처리
    {
        if (SceneFlowManager.Instance == null) // 관리자 존재 여부 확인
        {
            Debug.LogError("SceneFlowManager를 찾을 수 없습니다.", this); // 관리자 누락 오류
            return; // 버튼 처리 중단
        }

        SceneFlowManager.Instance.LoadGameplay(); // 게임 플레이 이동
    }

    public void OnBackButtonClicked() // 돌아가기 버튼 처리
    {
        if (SceneFlowManager.Instance == null) // 관리자 존재 여부 확인
        {
            Debug.LogError("SceneFlowManager를 찾을 수 없습니다.", this); // 관리자 누락 오류
            return; // 버튼 처리 중단
        }

        SceneFlowManager.Instance.LoadMainMenu(); // 메인 메뉴 이동
    }
}