using UnityEngine; // Unity 기본 기능

public abstract class InteractableBase : MonoBehaviour // 상호작용 대상 공통 부모
{
    [SerializeField] private string promptMessage = "F - INTERACT"; // 상호작용 안내 문구
    public string PromptMessage => promptMessage; // 안내 문구 제공
    public abstract void Interact(GameObject interactor); // 상호작용 실행
}