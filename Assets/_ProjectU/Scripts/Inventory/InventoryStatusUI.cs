using System.Text; // 문자열 조립 기능
using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능

[RequireComponent(typeof(TMP_Text))] // 필수 TextMeshPro 컴포넌트
public sealed class InventoryStatusUI : MonoBehaviour // 인벤토리 상태 UI
{
    [Tooltip("표시할 플레이어 인벤토리.")]
    [SerializeField] private PlayerInventory playerInventory; // 표시할 플레이어 인벤토리

    private TMP_Text statusText; // 인벤토리 출력 Text
    private readonly StringBuilder textBuilder = new StringBuilder(); // 문자열 조립 도구

    private void Awake() // UI 초기화
    {
        statusText = GetComponent<TMP_Text>(); // 현재 오브젝트 Text 가져오기

        if (playerInventory == null) // 인벤토리 연결 확인
        {
            Debug.LogError("InventoryStatusUI의 Player Inventory가 연결되지 않았습니다.", this); // 참조 누락 오류
            enabled = false; // UI 기능 비활성화
        }
    }

    private void OnEnable() // 변경 이벤트 연결
    {
        if (playerInventory == null) // 인벤토리 존재 확인
        {
            return; // 이벤트 연결 중단
        }

        playerInventory.InventoryChanged += Refresh; // 변경 이벤트 구독
        Refresh(); // 현재 상태 즉시 표시
    }

    private void OnDisable() // 변경 이벤트 해제
    {
        if (playerInventory != null) // 인벤토리 존재 확인
        {
            playerInventory.InventoryChanged -= Refresh; // 변경 이벤트 구독 해제
        }
    }

    private void Refresh() // 인벤토리 문구 갱신
    {
        textBuilder.Clear(); // 기존 문자열 제거
        textBuilder.AppendLine("INVENTORY"); // 제목 추가
        textBuilder.AppendLine($"SLOTS {playerInventory.UsedSlotCount}/{playerInventory.SlotCapacity}"); // 실제 사용량 추가

        if (playerInventory.UsedSlotCount == 0) // 빈 인벤토리 확인
        {
            textBuilder.Append("EMPTY"); // 빈 상태 문구 추가
            statusText.SetText(textBuilder.ToString()); // 빈 상태 화면 출력
            return; // 슬롯 출력 중단
        }

        for (int index = 0; index < playerInventory.SlotCapacity; index++) // 전체 슬롯 순회
        {
            InventorySlot slot = playerInventory.GetSlot(index); // 현재 슬롯 가져오기

            if (slot == null) // 빈 슬롯 확인
            {
                continue; // 빈 슬롯 출력 생략
            }

            textBuilder.AppendLine($"{index + 1}. {slot.ItemData.DisplayName} x{slot.Quantity}"); // 슬롯 정보 추가
        }

        statusText.SetText(textBuilder.ToString()); // 완성 문구 화면 출력
    }
}