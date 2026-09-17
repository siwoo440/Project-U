using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class WaterSource : InteractableBase // 물뿌리개를 채우는 우물·물가
{
    [Tooltip("안내 문구에 표시할 물 공급처 이름입니다.")]
    [SerializeField] private string sourceName = "WELL"; // 물 공급처 이름

    private string cachedPrompt = string.Empty; // 마지막 안내 문구
    private int cachedKey = int.MinValue; // 마지막 안내 상태 키

    public override string PromptMessage // 물뿌리개 상태에 맞는 안내 문구
    {
        get // 안내 문구 계산
        {
            FarmManager manager = FarmManager.Instance; // 밭 관리자 조회
            bool holdingCan = IsHoldingWateringCan(manager); // 물뿌리개 확인
            int water = manager != null ? manager.WateringCanWater : 0; // 남은 물
            int key = (holdingCan ? 100000 : 0) + water; // 상태 키

            if (key == cachedKey) // 같은 상태 확인
            {
                return cachedPrompt; // 이전 문구 반환
            }

            cachedKey = key; // 상태 키 저장

            if (!holdingCan) // 물뿌리개 미선택
            {
                cachedPrompt = sourceName + " | HOLD A WATERING CAN TO REFILL"; // 사용 안내
            }
            else if (water >= manager.WateringCanCapacity) // 가득 참
            {
                cachedPrompt = sourceName + " | WATERING CAN IS FULL"; // 가득 참 안내
            }
            else // 채우기 가능
            {
                cachedPrompt = $"F - REFILL WATERING CAN ({water}/{manager.WateringCanCapacity})"; // 채우기 안내
            }

            return cachedPrompt; // 문구 반환
        }
    }

    public override void Interact(GameObject interactor) // 물뿌리개 채우기
    {
        FarmManager manager = FarmManager.Instance; // 밭 관리자 조회

        if (IsHoldingWateringCan(manager)) // 물뿌리개 확인
        {
            manager.Refill(); // 가득 채우기
        }
    }

    private static bool IsHoldingWateringCan(FarmManager manager) // 물뿌리개를 들고 있는지 확인
    {
        FarmingToolController tools = FarmingToolController.Local; // 플레이어 농사 도구
        ItemData held = tools != null ? tools.SelectedItem : null; // 손에 든 아이템
        return manager != null
            && manager.Rules != null
            && held != null
            && held.IsTool
            && held.ToolType == manager.Rules.WateringTool; // 물뿌리개 여부 반환
    }
}
