using TMPro; // TextMeshPro UI 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // UGUI 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class WateringCanGaugeUI : MonoBehaviour // 물뿌리개를 들었을 때 남은 물 표시
{
    [Tooltip("손에 든 아이템을 확인할 플레이어 인벤토리입니다.")]
    [SerializeField] private PlayerInventory playerInventory; // 플레이어 인벤토리

    [Tooltip("남은 물을 제공하는 밭 관리자입니다.")]
    [SerializeField] private FarmManager farmManager; // 밭 관리자

    [Tooltip("물뿌리개를 들었을 때만 켜는 게이지 패널입니다.")]
    [SerializeField] private GameObject gaugeRoot; // 게이지 패널

    [Tooltip("남은 물 비율을 표시할 채움 이미지입니다.")]
    [SerializeField] private Image fillImage; // 채움 이미지

    [Tooltip("남은 물 수치를 표시할 텍스트입니다.")]
    [SerializeField] private TMP_Text valueText; // 수치 텍스트

    private int lastWater = -1; // 마지막 표시 물
    private int lastCapacity = -1; // 마지막 표시 용량

    private void OnEnable() // 이벤트 연결
    {
        if (playerInventory != null) // 인벤토리 확인
        {
            playerInventory.HotbarSelectionChanged += Refresh; // 핫바 선택 변경 구독
            playerInventory.InventoryChanged += Refresh; // 인벤토리 변경 구독
        }

        if (farmManager != null) // 밭 관리자 확인
        {
            farmManager.WaterChanged += Refresh; // 물 변경 구독
        }

        Refresh(); // 현재 상태 표시
    }

    private void OnDisable() // 이벤트 해제
    {
        if (playerInventory != null) // 인벤토리 확인
        {
            playerInventory.HotbarSelectionChanged -= Refresh; // 핫바 선택 변경 해제
            playerInventory.InventoryChanged -= Refresh; // 인벤토리 변경 해제
        }

        if (farmManager != null) // 밭 관리자 확인
        {
            farmManager.WaterChanged -= Refresh; // 물 변경 해제
        }
    }

    private void Refresh() // 게이지 표시 갱신
    {
        if (gaugeRoot == null || playerInventory == null || farmManager == null || farmManager.Rules == null) // 참조 확인
        {
            return; // 갱신 중단
        }

        ItemData held = playerInventory.SelectedHotbarItem; // 손에 든 아이템
        bool show = held != null && held.IsTool && held.ToolType == farmManager.Rules.WateringTool; // 물뿌리개 여부

        if (gaugeRoot.activeSelf != show) // 표시 상태 변경 확인
        {
            gaugeRoot.SetActive(show); // 표시 적용
        }

        if (!show) // 숨김 확인
        {
            return; // 수치 갱신 생략
        }

        int water = farmManager.WateringCanWater; // 남은 물
        int capacity = farmManager.WateringCanCapacity; // 용량

        if (water == lastWater && capacity == lastCapacity) // 같은 수치 확인
        {
            return; // 갱신 생략
        }

        lastWater = water; // 수치 저장
        lastCapacity = capacity; // 용량 저장

        if (fillImage != null) // 채움 이미지 확인
        {
            fillImage.fillAmount = capacity > 0 ? water / (float)capacity : 0f; // 비율 적용
        }

        if (valueText != null) // 텍스트 확인
        {
            valueText.SetText("{0}/{1}", water, capacity); // 수치 적용 (물방울 아이콘과 함께 표시)
        }
    }
}
