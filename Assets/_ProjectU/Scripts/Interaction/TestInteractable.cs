using UnityEngine; // Unity 기본 기능

[RequireComponent(typeof(Renderer))] // 필수 Renderer 컴포넌트
public sealed class TestInteractable : InteractableBase // 테스트 상호작용 오브젝트
{
    [Tooltip("비활성 상태 색상.")]
    [SerializeField] private Color inactiveColor = Color.gray; // 비활성 상태 색상
    [Tooltip("활성 상태 색상.")]
    [SerializeField] private Color activeColor = Color.green; // 활성 상태 색상

    private Renderer targetRenderer; // 색상 변경 대상 Renderer
    private bool isActivated; // 현재 활성 상태

    private void Awake() // Renderer 초기화
    {
        targetRenderer = GetComponent<Renderer>(); // 현재 오브젝트 Renderer 가져오기
        ApplyColor(); // 초기 색상 적용
    }

    public override void Interact(GameObject interactor) // 테스트 상호작용 실행
    {
        isActivated = !isActivated; // 활성 상태 반전
        ApplyColor(); // 변경 색상 적용
        Debug.Log($"{interactor.name}이 {gameObject.name}과 상호작용했습니다.", this); // 상호작용 결과 출력
    }

    private void ApplyColor() // 현재 상태 색상 적용
    {
        Color targetColor = isActivated ? activeColor : inactiveColor; // 적용할 색상 결정
        targetRenderer.material.color = targetColor; // Renderer Material 색상 변경
    }
}