using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class FarmTillTarget : InteractableBase // 괭이로 밭을 만들 칸의 상호작용 대상
{
    private FarmingToolController controller; // 연결된 플레이어 농사 도구

    public override string PromptMessage => controller != null ? controller.TillPrompt : string.Empty; // 경작 안내 문구

    public void Bind(FarmingToolController owner) // 플레이어 농사 도구 연결
    {
        controller = owner; // 연결 저장
    }

    public override void Interact(GameObject interactor) // F키 경작 실행
    {
        if (controller != null) // 연결 확인
        {
            controller.TryTill(); // 경작 시도
        }
    }
}
