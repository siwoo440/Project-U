using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

public sealed class StorageSlotView : MonoBehaviour // 보관함 슬롯 화면
{
    [SerializeField] private TMP_Text slotNumberText; // 슬롯 번호 Text
    [SerializeField] private Image itemIconImage; // 아이템 아이콘 Image
    [SerializeField] private TMP_Text itemNameText; // 아이템 이름 Text
    [SerializeField] private TMP_Text quantityText; // 아이템 수량 Text

    private bool referencesValid; // UI 참조 연결 상태

    private void Awake() // UI 참조 검사
    {
        referencesValid = slotNumberText != null // 슬롯 번호 참조 확인
            && itemIconImage != null // 아이템 아이콘 참조 확인
            && itemNameText != null // 아이템 이름 참조 확인
            && quantityText != null; // 아이템 수량 참조 확인

        if (!referencesValid) // 참조 누락 확인
        {
            Debug.LogError($"{gameObject.name}의 보관함 슬롯 UI 참조가 누락되었습니다.", this); // 연결 오류 출력
            enabled = false; // 슬롯 화면 비활성화
        }
    }

    public void SetSlot(InventorySlot slot, int slotNumber) // 보관함 슬롯 화면 갱신
    {
        if (!referencesValid) // 참조 상태 확인
        {
            return; // 화면 갱신 중단
        }

        slotNumberText.SetText(slotNumber.ToString()); // 슬롯 번호 표시

        if (slot == null || slot.ItemData == null || slot.Quantity <= 0) // 빈 슬롯 확인
        {
            itemIconImage.gameObject.SetActive(false); // 아이템 아이콘 숨김
            itemIconImage.sprite = null; // 기존 아이콘 제거
            itemNameText.SetText(string.Empty); // 아이템 이름 제거
            quantityText.SetText(string.Empty); // 아이템 수량 제거
            return; // 빈 슬롯 처리 종료
        }

        ItemData itemData = slot.ItemData; // 현재 아이템 데이터 조회
        Sprite itemIcon = itemData.Icon; // 현재 아이템 아이콘 조회

        itemIconImage.gameObject.SetActive(true); // 아이템 아이콘 표시
        itemIconImage.sprite = itemIcon; // 아이템 아이콘 적용
        itemIconImage.color = itemIcon == null ? ItemIconUtility.GetFallbackColor(itemData.ItemCategory) : Color.white; // 아이콘 색상 적용
        itemNameText.SetText(itemData.DisplayName); // 아이템 이름 표시
        quantityText.SetText($"x{slot.Quantity}"); // 아이템 수량 표시
    }
}