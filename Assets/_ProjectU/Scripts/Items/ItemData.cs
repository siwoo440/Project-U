using UnityEngine; // Unity 기본 기능

[CreateAssetMenu(fileName = "ItemData_New", menuName = "Project U/Item Data")] // 아이템 데이터 생성 메뉴
public sealed class ItemData : ScriptableObject // 아이템 공통 데이터
{
    [Header("Identity")] // 식별 정보 묶음
    [SerializeField] private string itemId = "item_new"; // 아이템 고유 ID

    [Header("Display")] // 화면 표시 정보 묶음
    [SerializeField] private string displayName = "NEW ITEM"; // 아이템 표시 이름
    [TextArea(2, 4)] // 여러 줄 설명 입력
    [SerializeField] private string description = "NO DESCRIPTION"; // 아이템 설명
    [SerializeField] private Sprite icon; // 아이템 아이콘

    [Header("Category")] // 아이템 분류 묶음
    [SerializeField] private ItemCategory itemCategory = ItemCategory.CraftingMaterial; // 아이템 기본 분류
    [SerializeField] private ToolType toolType = ToolType.None; // 도구 종류

    [Header("Food")] // 음식 효과 묶음
    [SerializeField] private float hungerRestoreAmount = 0f; // 허기 회복량

    [Header("Stack")] // 중첩 정보 묶음
    [SerializeField] private int maximumStack = 20; // 최대 중첩 수량

    public string ItemId => itemId; // 아이템 ID 제공
    public string DisplayName => displayName; // 표시 이름 제공
    public string Description => description; // 아이템 설명 제공
    public Sprite Icon => icon; // 아이템 아이콘 제공
    public int MaximumStack => Mathf.Max(1, maximumStack); // 최대 중첩 수량 제공
    public ItemCategory ItemCategory => itemCategory; // 아이템 분류 제공
    public ToolType ToolType => toolType; // 도구 종류 제공

    public float HungerRestoreAmount => IsFood ? Mathf.Max(0f, hungerRestoreAmount) : 0f; // 음식 허기 회복량 제공
    public bool IsCraftingMaterial => itemCategory == ItemCategory.CraftingMaterial; // 제작 재료 여부 제공
    public bool IsTool => itemCategory == ItemCategory.Tool; // 도구 여부 제공
    public bool IsFood => itemCategory == ItemCategory.Food; // 음식 여부 제공
    public bool IsEquipment => itemCategory == ItemCategory.Equipment; // 장비 여부 제공


    private void OnValidate() // Inspector 값 검증
    {
        itemId = string.IsNullOrWhiteSpace(itemId) ? string.Empty : itemId.Trim(); // ID 양쪽 공백 제거
        displayName = string.IsNullOrWhiteSpace(displayName) ? string.Empty : displayName.Trim(); // 이름 양쪽 공백 제거
        description = string.IsNullOrWhiteSpace(description) ? string.Empty : description.Trim(); // 설명 양쪽 공백 제거
        maximumStack = Mathf.Max(1, maximumStack); // 최대 중첩 최소값 적용

        bool requiresSingleStack = itemCategory == ItemCategory.Tool || itemCategory == ItemCategory.Equipment; // 단일 보관 분류 확인

        if (requiresSingleStack) // 도구 또는 장비 확인
        {
            maximumStack = 1; // 최대 중첩 1개 적용
        }

        if (itemCategory != ItemCategory.Tool) // 도구가 아닌 분류 확인
        {
            toolType = ToolType.None; // 도구 종류 제거
        }

        if (itemCategory != ItemCategory.Food) // 음식이 아닌 분류 확인
        {
            hungerRestoreAmount = 0f; // 음식 회복량 제거
        }
        else // 음식 분류 확인
        {
            hungerRestoreAmount = Mathf.Max(0f, hungerRestoreAmount); // 회복량 음수 방지
        }
    }
}