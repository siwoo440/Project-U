using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class FishingCastTarget : InteractableBase // 찌를 던질 물 위 지점과 낚시 중 F키 입력 대상
{
    private FishingController controller; // 연결된 플레이어 낚시

    public override string PromptMessage => controller != null ? controller.Prompt : string.Empty; // 낚시 상태 안내 문구

    public void Bind(FishingController owner) // 플레이어 낚시 연결
    {
        controller = owner; // 연결 저장
    }

    public override void Interact(GameObject interactor) // 던지기·챔질·걷기
    {
        if (controller != null) // 연결 확인
        {
            controller.HandleInteract(); // 상태별 처리
        }
    }
}
