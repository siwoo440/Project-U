using UnityEngine; // Unity 기본 기능

public sealed class BootstrapLoader : MonoBehaviour // 초기 Scene 실행 처리
{
    private void Start() // 게임 시작 후 실행
    {
        if (SceneFlowManager.Instance == null) // Scene 관리자 존재 여부 확인
        {
            Debug.LogError("SceneFlowManager를 찾을 수 없습니다.", this); // 관리자 누락 오류
            return; // 초기화 중단
        }

        if (GameManager.Instance == null) // 게임 관리자 존재 여부 확인
        {
            Debug.LogError("GameManager를 찾을 수 없습니다.", this); // 관리자 누락 오류
            return; // 초기화 중단
        }

        SceneFlowManager.Instance.LoadMainMenu(); // 메인 메뉴 이동
    }
}