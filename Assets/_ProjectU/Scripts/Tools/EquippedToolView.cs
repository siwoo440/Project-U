using UnityEngine; // Unity 기본 기능

public sealed class EquippedToolView : MonoBehaviour // 장착 도구 외형 관리
{
    [Tooltip("확인할 플레이어 인벤토리.")]
    [SerializeField] private PlayerInventory playerInventory; // 확인할 플레이어 인벤토리
    [Tooltip("도끼 외형.")]
    [SerializeField] private GameObject axeVisual; // 도끼 외형
    [Tooltip("곡괭이 외형.")]
    [SerializeField] private GameObject pickaxeVisual; // 곡괭이 외형

    private void Awake() // 필수 참조 확인
    {
        bool hasMissingReference = playerInventory == null || axeVisual == null || pickaxeVisual == null; // 참조 누락 확인

        if (hasMissingReference) // 참조 누락 여부 확인
        {
            Debug.LogError("EquippedToolView의 참조를 모두 연결해야 합니다.", this); // 참조 누락 오류
            enabled = false; // 도구 표시 기능 비활성화
        }
    }

    private void OnEnable() // 인벤토리 이벤트 연결
    {
        if (playerInventory == null) // 인벤토리 존재 확인
        {
            return; // 이벤트 연결 중단
        }

        playerInventory.HotbarSelectionChanged += Refresh; // 핫바 선택 이벤트 구독
        playerInventory.InventoryChanged += Refresh; // 슬롯 변경 이벤트 구독
        Refresh(); // 현재 장착 상태 표시
    }

    private void OnDisable() // 인벤토리 이벤트 해제
    {
        if (playerInventory == null) // 인벤토리 존재 확인
        {
            return; // 이벤트 해제 중단
        }

        playerInventory.HotbarSelectionChanged -= Refresh; // 핫바 선택 이벤트 해제
        playerInventory.InventoryChanged -= Refresh; // 슬롯 변경 이벤트 해제
    }

    private void Refresh() // 장착 도구 외형 갱신
    {
        ItemData selectedItem = playerInventory.SelectedHotbarItem; // 선택 핫바 아이템 조회
        ToolType selectedToolType = ToolType.None; // 기본 도구 종류 설정

        if (selectedItem != null && selectedItem.IsTool) // 도구 아이템 선택 확인
        {
            selectedToolType = selectedItem.ToolType; // 선택 도구 종류 저장
        }

        axeVisual.SetActive(selectedToolType == ToolType.Axe); // 도끼 외형 상태 적용
        pickaxeVisual.SetActive(selectedToolType == ToolType.Pickaxe); // 곡괭이 외형 상태 적용
    }
}