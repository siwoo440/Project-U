using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class SleepingBagInteractable : InteractableBase // 침낭 상호작용 처리
{
    [SerializeField] private SleepSystem sleepSystem; // 수면 시스템 참조

    public override string PromptMessage => sleepSystem == null ? "SLEEP UNAVAILABLE" : sleepSystem.SleepPrompt; // 현재 수면 안내 문구 제공

    private void Awake() // 수면 시스템 참조 초기화
    {
        if (sleepSystem == null) // 수면 시스템 미연결 확인
        {
            sleepSystem = FindFirstObjectByType<SleepSystem>(); // Scene 수면 시스템 검색
        }

        if (sleepSystem == null) // 수면 시스템 검색 실패 확인
        {
            Debug.LogError($"{gameObject.name}에서 SleepSystem을 찾을 수 없습니다.", this); // 수면 시스템 누락 오류 출력
            enabled = false; // 침낭 상호작용 비활성화
        }
    }

    public override void Interact(GameObject interactor) // 침낭 사용 처리
    {
        if (!enabled || sleepSystem == null) // 수면 기능 사용 가능 여부 확인
        {
            return; // 상호작용 중단
        }

        if (interactor == null) // 상호작용 주체 존재 확인
        {
            return; // 잘못된 상호작용 차단
        }

        sleepSystem.TrySleep(); // 현재 조건으로 수면 시작 시도
    }
}