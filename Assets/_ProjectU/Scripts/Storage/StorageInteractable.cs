using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class StorageInteractable : InteractableBase // 설치 보관함 상호작용 처리
{
    [SerializeField] private StorageContainer storageContainer; // 대상 보관함
    [SerializeField] private StorageContainerUI storageContainerUI; // 보관함 화면 관리자

    public override string PromptMessage => storageContainer == null
        ? "STORAGE UNAVAILABLE"
        : $"F - OPEN {storageContainer.DisplayName}"; // 보관함 안내 문구 제공

    private void Awake() // 보관함 상호작용 초기화
    {
        if (storageContainer == null) // 보관함 참조 확인
        {
            storageContainer = GetComponentInParent<StorageContainer>(); // 상위 보관함 검색
        }

        if (storageContainerUI == null) // 보관함 UI 참조 확인
        {
            storageContainerUI = FindFirstObjectByType<StorageContainerUI>(); // Scene 보관함 UI 검색
        }

        if (storageContainer == null || storageContainerUI == null) // 필수 참조 확인
        {
            Debug.LogError($"{gameObject.name}의 보관함 상호작용 참조가 누락되었습니다.", this); // 참조 오류 출력
            enabled = false; // 보관함 상호작용 비활성화
        }
    }

    public override void Interact(GameObject interactor) // 보관함 화면 열기
    {
        if (!enabled || interactor == null) // 상호작용 가능 여부 확인
        {
            return; // 상호작용 중단
        }

        if (storageContainer == null || storageContainerUI == null) // 보관함 참조 확인
        {
            return; // 화면 열기 중단
        }

        storageContainerUI.Open(storageContainer); // 지정 보관함 화면 열기
    }
}
