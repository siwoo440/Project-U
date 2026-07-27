using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

public sealed class InventoryDetailUI : MonoBehaviour // 아이템 상세 정보 UI
{
    [Header("References")] // 기능 참조 묶음
    [SerializeField] private PlayerInventory playerInventory; // 플레이어 인벤토리
    [SerializeField] private InventoryItemDropper itemDropper; // 아이템 버리기 기능

    [Header("State")] // 상태 화면 묶음
    [SerializeField] private GameObject emptyStateRoot; // 미선택 화면
    [SerializeField] private GameObject detailContentRoot; // 상세 정보 화면

    [Header("Display")] // 표시 요소 묶음
    [SerializeField] private Image itemIconImage; // 아이템 아이콘
    [SerializeField] private TMP_Text itemNameText; // 아이템 이름
    [SerializeField] private TMP_Text categoryText; // 아이템 분류
    [SerializeField] private TMP_Text descriptionText; // 아이템 설명
    [SerializeField] private TMP_Text quantityText; // 아이템 수량

    [Header("Actions")] // 동작 버튼 묶음
    [SerializeField] private Button removeOneButton; // 한 개 제거 버튼
    [SerializeField] private Button dropOneButton; // 한 개 버리기 버튼

    private bool referencesValid; // 참조 연결 상태

    private void Awake() // 상세 UI 초기화
    {
        referencesValid = playerInventory != null && itemDropper != null && emptyStateRoot != null && detailContentRoot != null && itemIconImage != null && itemNameText != null && categoryText != null && descriptionText != null && quantityText != null && removeOneButton != null && dropOneButton != null; // 필수 참조 검사

        if (!referencesValid) // 참조 누락 확인
        {
            Debug.LogError($"{gameObject.name}의 아이템 상세 UI 참조가 누락되었습니다.", this); // 참조 누락 출력
            enabled = false; // 상세 기능 비활성화
            return; // 초기화 중단
        }

        removeOneButton.onClick.AddListener(RemoveSelectedItem); // 제거 버튼 기능 연결
        dropOneButton.onClick.AddListener(DropSelectedItem); // 버리기 버튼 기능 연결
    }

    private void OnEnable() // 변경 이벤트 연결
    {
        if (!referencesValid) // 참조 상태 확인
        {
            return; // 이벤트 연결 중단
        }

        playerInventory.InventoryChanged += Refresh; // 인벤토리 변경 구독
        playerInventory.InventorySelectionChanged += Refresh; // 슬롯 선택 변경 구독
        Refresh(); // 현재 정보 표시
    }

    private void OnDisable() // 변경 이벤트 해제
    {
        if (!referencesValid) // 참조 상태 확인
        {
            return; // 이벤트 해제 중단
        }

        playerInventory.InventoryChanged -= Refresh; // 인벤토리 변경 해제
        playerInventory.InventorySelectionChanged -= Refresh; // 슬롯 선택 변경 해제
    }

    private void Refresh() // 상세 정보 갱신
    {
        int selectedIndex = playerInventory.SelectedInventoryIndex; // 선택 슬롯 번호 조회
        InventorySlot selectedSlot = playerInventory.GetSlot(selectedIndex); // 선택 슬롯 조회

        if (selectedSlot == null) // 선택 아이템 없음 확인
        {
            emptyStateRoot.SetActive(true); // 미선택 화면 표시
            detailContentRoot.SetActive(false); // 상세 정보 숨김
            return; // 갱신 종료
        }

        ItemData itemData = selectedSlot.ItemData; // 선택 아이템 데이터 조회
        Sprite itemIcon = itemData.Icon; // 아이템 아이콘 조회

        emptyStateRoot.SetActive(false); // 미선택 화면 숨김
        detailContentRoot.SetActive(true); // 상세 정보 표시
        itemIconImage.sprite = itemIcon; // 아이콘 적용
        itemIconImage.color = itemIcon == null ? ItemIconUtility.GetFallbackColor(itemData.ItemCategory) : Color.white; // 실제 또는 대체 색상 적용
        itemNameText.SetText(itemData.DisplayName); // 아이템 이름 출력
        categoryText.SetText(GetCategoryLabel(itemData.ItemCategory)); // 아이템 분류 출력
        descriptionText.SetText(string.IsNullOrWhiteSpace(itemData.Description) ? "NO DESCRIPTION" : itemData.Description); // 아이템 설명 출력
        quantityText.SetText($"QUANTITY: {selectedSlot.Quantity} / {itemData.MaximumStack}"); // 아이템 수량 출력
        removeOneButton.interactable = true; // 제거 버튼 활성화
        dropOneButton.interactable = true; // 버리기 버튼 활성화
    }

    private void RemoveSelectedItem() // 선택 아이템 한 개 제거
    {
        int selectedIndex = playerInventory.SelectedInventoryIndex; // 선택 슬롯 번호 조회
        playerInventory.RemoveItemFromSlot(selectedIndex, 1); // 아이템 한 개 영구 제거
    }

    private void DropSelectedItem() // 선택 아이템 한 개 버리기
    {
        int selectedIndex = playerInventory.SelectedInventoryIndex; // 선택 슬롯 번호 조회
        itemDropper.DropFromSlot(selectedIndex, 1); // 아이템 한 개 월드 생성
    }

    private string GetCategoryLabel(ItemCategory itemCategory) // 분류 표시 문구 반환
    {
        switch (itemCategory) // 아이템 분류 확인
        {
            case ItemCategory.CraftingMaterial: // 제작 재료 분기
                return "CRAFTING MATERIAL"; // 제작 재료 문구 반환

            case ItemCategory.Tool: // 도구 분기
                return "TOOL"; // 도구 문구 반환

            case ItemCategory.Food: // 음식 분기
                return "FOOD"; // 음식 문구 반환

            case ItemCategory.Equipment: // 장비 분기
                return "EQUIPMENT"; // 장비 문구 반환

            default: // 미정 분류
                return "UNKNOWN"; // 미정 문구 반환
        }
    }
}