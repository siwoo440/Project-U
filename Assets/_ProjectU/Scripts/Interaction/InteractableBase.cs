using UnityEngine; // Unity 기본 기능

public abstract class InteractableBase : MonoBehaviour // 상호작용 대상 공통 부모
{
    [Tooltip("상호작용 안내 문구.")]
    [SerializeField] private string promptMessage = "F - INTERACT"; // 상호작용 안내 문구
    public virtual string PromptMessage => promptMessage; // 변경 가능한 안내 문구 제공
    public abstract void Interact(GameObject interactor); // 상호작용 실행
}