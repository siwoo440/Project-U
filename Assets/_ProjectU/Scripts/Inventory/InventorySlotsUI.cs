using System.Collections.Generic; // List 기능
using UnityEngine; // Unity 기본 기능

public sealed class InventorySlotsUI : MonoBehaviour // 여러 인벤토리 슬롯 표시
{
    [SerializeField] private PlayerInventory playerInventory; // 표시할 플레이어 인벤토리
    [SerializeField] private Transform slotContainer; // 슬롯 생성 부모
    [SerializeField] private InventorySlotView slotTemplate; // 복제할 슬롯 원본
    [SerializeField] private int visibleSlotCount = 8; // 표시할 슬롯 개수
    [SerializeField] private int startSlotIndex; // 첫 번째 표시 슬롯 번호
    [SerializeField] private bool showShortcutNumbers = true; // 숫자키 표시 여부
    [SerializeField] private bool showSelection = true; // 선택 테두리 표시 여부

    private readonly List<InventorySlotView> slotViews = new List<InventorySlotView>(); // 생성된 슬롯 목록

    private void Awake() // 슬롯 화면 생성
    {
        if (playerInventory == null || slotContainer == null || slotTemplate == null) // 필수 참조 확인
        {
            Debug.LogError($"{gameObject.name}의 인벤토리 UI 참조를 모두 연결해야 합니다.", this); // 참조 누락 오류
            enabled = false; // UI 기능 비활성화
            return; // 초기화 중단
        }

        slotTemplate.gameObject.SetActive(false); // 원본 슬롯 숨김

        int availableSlotCount = Mathf.Max(0, playerInventory.SlotCapacity - startSlotIndex); // 표시 가능한 슬롯 계산
        int targetSlotCount = visibleSlotCount <= 0 ? availableSlotCount : Mathf.Min(visibleSlotCount, availableSlotCount); // 실제 생성 개수 계산

        for (int index = 0; index < targetSlotCount; index++) // 필요한 슬롯 수만큼 반복
        {
            InventorySlotView newSlotView = Instantiate(slotTemplate, slotContainer); // 슬롯 원본 복제
            newSlotView.gameObject.SetActive(true); // 복제 슬롯 표시
            slotViews.Add(newSlotView); // 생성 목록 등록
        }
    }

    private void OnEnable() // 변경 이벤트 연결
    {
        if (playerInventory == null) // 인벤토리 연결 확인
        {
            return; // 이벤트 연결 중단
        }

        playerInventory.InventoryChanged += Refresh; // 아이템 변경 이벤트 구독
        playerInventory.HotbarSelectionChanged += Refresh; // 선택 변경 이벤트 구독
        Refresh(); // 현재 상태 즉시 표시
    }

    private void OnDisable() // 변경 이벤트 해제
    {
        if (playerInventory == null) // 인벤토리 연결 확인
        {
            return; // 이벤트 해제 중단
        }

        playerInventory.InventoryChanged -= Refresh; // 아이템 변경 이벤트 해제
        playerInventory.HotbarSelectionChanged -= Refresh; // 선택 변경 이벤트 해제
    }

    private void Refresh() // 전체 슬롯 화면 갱신
    {
        for (int viewIndex = 0; viewIndex < slotViews.Count; viewIndex++) // 생성 슬롯 순회
        {
            int inventoryIndex = startSlotIndex + viewIndex; // 실제 인벤토리 번호 계산
            InventorySlot slot = playerInventory.GetSlot(inventoryIndex); // 해당 슬롯 조회
            bool isSelected = showSelection && inventoryIndex == playerInventory.SelectedHotbarIndex; // 선택 상태 계산
            bool showShortcut = showShortcutNumbers && inventoryIndex < playerInventory.HotbarSlotCount; // 숫자 표시 상태 계산
            slotViews[viewIndex].SetSlot(slot, inventoryIndex + 1, showShortcut, isSelected); // 슬롯 화면 적용
        }
    }
}
