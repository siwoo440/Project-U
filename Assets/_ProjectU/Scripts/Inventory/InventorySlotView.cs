using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

public sealed class InventorySlotView : MonoBehaviour // 개별 슬롯 화면 표시
{
    [SerializeField] private TMP_Text shortcutText; // 숫자키 표시 Text
    [SerializeField] private TMP_Text itemNameText; // 아이템 이름 Text
    [SerializeField] private TMP_Text quantityText; // 아이템 수량 Text
    [SerializeField] private Outline selectionOutline; // 선택 테두리

    private bool referencesValid; // 참조 연결 상태

    private void Awake() // UI 참조 검사
    {
        referencesValid = shortcutText != null && itemNameText != null && quantityText != null && selectionOutline != null; // 필수 참조 검사

        if (!referencesValid) // 참조 누락 확인
        {
            Debug.LogError($"{gameObject.name}의 슬롯 UI 참조를 모두 연결해야 합니다.", this); // 연결 오류 출력
        }
    }

    public void SetSlot(InventorySlot slot, int slotNumber, bool showShortcut, bool isSelected) // 슬롯 화면 갱신
    {
        if (!referencesValid) // 참조 상태 확인
        {
            return; // 갱신 중단
        }

        shortcutText.gameObject.SetActive(showShortcut); // 숫자 표시 상태 적용
        shortcutText.SetText(slotNumber.ToString()); // 슬롯 번호 출력
        selectionOutline.enabled = isSelected; // 선택 테두리 적용

        if (slot == null) // 빈 슬롯 확인
        {
            itemNameText.SetText(string.Empty); // 아이템 이름 제거
            quantityText.SetText(string.Empty); // 아이템 수량 제거
            return; // 빈 슬롯 처리 종료
        }

        itemNameText.SetText(slot.ItemData.DisplayName); // 아이템 이름 출력
        quantityText.SetText($"x{slot.Quantity}"); // 아이템 수량 출력
    }
}