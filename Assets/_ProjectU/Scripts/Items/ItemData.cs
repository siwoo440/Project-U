using UnityEngine; // Unity 기본 기능

[CreateAssetMenu(fileName = "ItemData_New", menuName = "Project U/Item Data")] // 아이템 데이터 생성 메뉴
public sealed class ItemData : ScriptableObject // 아이템 공통 데이터
{
    [Header("Identity")] // 식별 정보 묶음
    [SerializeField] private string itemId = "item_new"; // 아이템 고유 ID
    [SerializeField] private string displayName = "NEW ITEM"; // 화면 표시 이름

    [Header("Category")] // 아이템 분류 묶음
    [SerializeField] private ItemCategory itemCategory = ItemCategory.Material; // 아이템 기본 분류
    [SerializeField] private ToolType toolType = ToolType.None; // 도구 종류

    [Header("Stack")] // 중첩 정보 묶음
    [SerializeField] private int maximumStack = 20; // 최대 중첩 수량

    public string ItemId => itemId; // 아이템 ID 제공
    public string DisplayName => displayName; // 표시 이름 제공
    public int MaximumStack => Mathf.Max(1, maximumStack); // 최소 1 이상의 중첩값 제공

    public ItemCategory ItemCategory => itemCategory; // 아이템 분류 제공
    public ToolType ToolType => toolType; // 도구 종류 제공
    public bool IsTool => itemCategory == ItemCategory.Tool; // 도구 여부 제공


    private void OnValidate() // Inspector 값 검증
    {
        maximumStack = Mathf.Max(1, maximumStack); // 최대 중첩 최소값 보정

        if (itemCategory == ItemCategory.Tool) // 도구 아이템 확인
        {
            maximumStack = 1; // 도구 중첩 수량 제한
            return; // 재료 처리 생략
        }

        toolType = ToolType.None; // 일반 아이템 도구 종류 제거
    }
}