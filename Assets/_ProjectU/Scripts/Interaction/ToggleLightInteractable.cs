using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class ToggleLightInteractable : InteractableBase // 설치 조명 상호작용 처리
{
    [Tooltip("제어 대상 조명.")]
    [SerializeField] private Light targetLight; // 제어 대상 조명
    [Tooltip("시작 점등 상태.")]
    [SerializeField] private bool startsEnabled = true; // 시작 점등 상태

    public bool IsLightEnabled => targetLight != null && targetLight.enabled; // 현재 점등 상태 제공
    public override string PromptMessage => IsLightEnabled
        ? "F - TURN OFF LIGHT"
        : "F - TURN ON LIGHT"; // 현재 조명 안내 문구 제공

    private void Awake() // 조명 상호작용 초기화
    {
        if (targetLight == null) // 조명 참조 확인
        {
            targetLight = GetComponentInChildren<Light>(true); // 하위 조명 검색
        }

        if (targetLight == null) // 조명 존재 확인
        {
            Debug.LogError($"{gameObject.name}의 Light 참조가 누락되었습니다.", this); // 참조 오류 출력
            enabled = false; // 조명 상호작용 비활성화
            return; // 초기화 중단
        }

        targetLight.enabled = startsEnabled; // 시작 점등 상태 적용
    }

    public override void Interact(GameObject interactor) // 조명 점등 상태 전환
    {
        if (!enabled || interactor == null || targetLight == null) // 상호작용 가능 여부 확인
        {
            return; // 상호작용 중단
        }

        targetLight.enabled = !targetLight.enabled; // 조명 점등 상태 반전
    }
}
