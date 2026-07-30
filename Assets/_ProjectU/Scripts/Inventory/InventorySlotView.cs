using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.EventSystems; // UI 포인터 이벤트 기능
using UnityEngine.UI; // Unity UI 기능

public sealed class InventorySlotView : MonoBehaviour, IPointerClickHandler // 인벤토리 슬롯 표시와 클릭 처리
{
    [SerializeField] private TMP_Text shortcutText; // 숫자키 표시 Text
    [SerializeField] private Image itemIconImage; // 아이템 아이콘 Image
    [SerializeField] private TMP_Text itemNameText; // 아이템 이름 Text
    [SerializeField] private TMP_Text quantityText; // 아이템 수량 Text
    [SerializeField] private Outline selectionOutline; // 선택 테두리
    [SerializeField] private ItemSlotDragHandler itemSlotDragHandler; // 공통 슬롯 드래그 처리기

    private PlayerInventory playerInventory; // 연결된 플레이어 인벤토리
    private int inventoryIndex; // 실제 인벤토리 슬롯 번호
    private bool referencesValid; // UI 참조 연결 상태

    private void Awake() // UI 참조 검사
    {
        referencesValid = shortcutText != null // 숫자키 Text 참조 확인
            && itemIconImage != null // 아이템 아이콘 참조 확인
            && itemNameText != null // 아이템 이름 참조 확인
            && quantityText != null // 아이템 수량 참조 확인
            && selectionOutline != null // 선택 테두리 참조 확인
            && itemSlotDragHandler != null; // 드래그 처리기 참조 확인

        if (!referencesValid) // 참조 누락 확인
        {
            Debug.LogError($"{gameObject.name}의 슬롯 UI 참조를 모두 연결해야 합니다.", this); // 연결 오류 출력
            enabled = false; // 슬롯 화면 기능 비활성화
        }
    }

    public void Configure( // 슬롯 기능 설정
        PlayerInventory newPlayerInventory, // 새로운 플레이어 인벤토리
        int newInventoryIndex, // 새로운 실제 슬롯 번호
        bool newAllowItemDrag, // 새로운 드래그 허용 상태
        bool newRequireAltKeyForDrag) // 새로운 Alt 키 요구 상태
    {
        playerInventory = newPlayerInventory; // 플레이어 인벤토리 저장
        inventoryIndex = newInventoryIndex; // 실제 슬롯 번호 저장

        if (itemSlotDragHandler != null) // 드래그 처리기 참조 확인
        {
            itemSlotDragHandler.Configure( // 공통 드래그 대상 설정
                playerInventory, // 플레이어 인벤토리 컨테이너 전달
                inventoryIndex, // 실제 슬롯 번호 전달
                newAllowItemDrag, // 드래그 허용 상태 전달
                newRequireAltKeyForDrag); // Alt 키 요구 상태 전달
        }
    }

    public void SetSlot(InventorySlot slot, int slotNumber, bool showShortcut, bool newIsSelected) // 슬롯 화면 갱신
    {
        if (!referencesValid) // 참조 상태 확인
        {
            return; // 화면 갱신 중단
        }

        shortcutText.gameObject.SetActive(showShortcut); // 숫자 표시 상태 적용
        shortcutText.SetText(slotNumber.ToString()); // 슬롯 번호 출력
        itemSlotDragHandler.SetRestingOutlineState(newIsSelected); // 선택 테두리 상태 적용

        if (slot == null || slot.ItemData == null || slot.Quantity <= 0) // 빈 슬롯 확인
        {
            itemIconImage.gameObject.SetActive(false); // 아이콘 숨김
            itemIconImage.sprite = null; // 기존 아이콘 제거
            itemNameText.SetText(string.Empty); // 아이템 이름 제거
            quantityText.SetText(string.Empty); // 아이템 수량 제거
            return; // 빈 슬롯 처리 종료
        }

        ItemData itemData = slot.ItemData; // 현재 아이템 데이터 조회
        Sprite itemIcon = itemData.Icon; // 아이템 아이콘 조회

        itemIconImage.gameObject.SetActive(true); // 아이콘 오브젝트 표시
        itemIconImage.sprite = itemIcon; // 등록된 아이콘 적용
        itemIconImage.color = itemIcon == null // 실제 아이콘 존재 여부 확인
            ? ItemIconUtility.GetFallbackColor(itemData.ItemCategory) // 분류별 대체 색상 적용
            : Color.white; // 실제 아이콘 기본 색상 적용
        itemNameText.SetText(itemData.DisplayName); // 아이템 이름 출력
        quantityText.SetText($"x{slot.Quantity}"); // 아이템 수량 출력
    }

    public void OnPointerClick(PointerEventData eventData) // 슬롯 클릭 선택 처리
    {
        if (eventData.button != PointerEventData.InputButton.Left) // 왼쪽 버튼 확인
        {
            return; // 다른 버튼 클릭 차단
        }

        if (!referencesValid || playerInventory == null) // 선택 가능 상태 확인
        {
            return; // 선택 처리 중단
        }

        playerInventory.SelectInventorySlot(inventoryIndex); // 클릭한 슬롯 선택
    }
}
