using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class EquipmentStatsUI : MonoBehaviour // 장비 능력치 화면 관리
{
    [SerializeField] private PlayerEquipment playerEquipment; // 플레이어 장비 관리자
    [SerializeField] private TMP_Text statsText; // 능력치 표시 문구

    private bool referencesValid; // 참조 연결 상태

    private void Awake() // 능력치 UI 초기화
    {
        referencesValid = playerEquipment != null && statsText != null; // 필수 참조 검사

        if (!referencesValid) // 참조 누락 확인
        {
            Debug.LogError($"{gameObject.name}의 장비 능력치 UI 참조가 누락되었습니다.", this); // 참조 오류 출력
            enabled = false; // 능력치 UI 비활성화
            return; // 초기화 중단
        }
    }

    private void OnEnable() // 장비 이벤트 연결
    {
        if (!referencesValid) // 참조 상태 확인
        {
            return; // 이벤트 연결 중단
        }

        playerEquipment.EquipmentChanged += Refresh; // 장비 변경 구독
        Refresh(); // 현재 능력치 표시
    }

    private void OnDisable() // 장비 이벤트 해제
    {
        if (!referencesValid) // 참조 상태 확인
        {
            return; // 이벤트 해제 중단
        }

        playerEquipment.EquipmentChanged -= Refresh; // 장비 변경 구독 해제
    }

    private void Refresh() // 장비 능력치 화면 갱신
    {
        statsText.SetText($"DEFENSE {playerEquipment.TotalDefensePercent:0}%\nMAX HEALTH +{playerEquipment.TotalMaximumHealthBonus:0}\nMOVE SPEED +{playerEquipment.TotalMovementSpeedBonusPercent:0}%\nHUNGER USE -{playerEquipment.TotalHungerReductionPercent:0}%\nTHIRST USE -{playerEquipment.TotalThirstReductionPercent:0}%\nINVENTORY +{playerEquipment.TotalInventorySlotBonus}"); // 전체 장비 능력치 출력
    }
}
