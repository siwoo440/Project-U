using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class EquipmentStatsUI : MonoBehaviour // 장비 능력치 화면 관리
{
    [SerializeField] private PlayerEquipment playerEquipment; // 플레이어 장비 관리자
    [SerializeField] private TMP_Text statsText; // 능력치 표시 문구

    private bool internalReferencesValid; // UI 내부 참조 상태
    private bool runtimeInitialized; // 런타임 장비 참조 초기화 상태
    private bool eventSubscribed; // 장비 이벤트 구독 상태

    private void Awake() // 능력치 UI 내부 초기화
    {
        internalReferencesValid = statsText != null; // UI 내부 참조 검사

        if (!internalReferencesValid) // 내부 참조 누락 확인
        {
            Debug.LogError($"{gameObject.name}의 장비 능력치 UI 내부 참조가 누락되었습니다.", this); // 참조 오류 출력
            enabled = false; // 능력치 UI 비활성화
        }
    }

    private void OnEnable() // 장비 이벤트 연결
    {
        if (!runtimeInitialized) // 런타임 초기화 상태 확인
        {
            return; // 이벤트 연결 중단
        }

        SubscribeEvent(); // 장비 변경 이벤트 연결
        Refresh(); // 현재 능력치 표시
    }

    private void OnDisable() // 장비 이벤트 해제
    {
        UnsubscribeEvent(); // 장비 변경 이벤트 해제
    }

    public bool Initialize(PlayerEquipment equipment) // 런타임 장비 관리자 초기화
    {
        if (!internalReferencesValid || equipment == null) // 내부와 외부 참조 확인
        {
            Debug.LogError($"{gameObject.name}의 장비 능력치 UI 런타임 참조가 누락되었습니다.", this); // 참조 오류 출력
            runtimeInitialized = false; // 초기화 실패 기록
            return false; // 초기화 실패 반환
        }

        UnsubscribeEvent(); // 기존 장비 이벤트 해제
        playerEquipment = equipment; // 플레이어 장비 관리자 저장
        runtimeInitialized = true; // 런타임 초기화 완료 기록

        if (isActiveAndEnabled) // 현재 화면 활성 상태 확인
        {
            SubscribeEvent(); // 장비 변경 이벤트 연결
            Refresh(); // 현재 능력치 표시
        }

        return true; // 초기화 성공 반환
    }

    private void SubscribeEvent() // 장비 변경 이벤트 연결
    {
        if (eventSubscribed || playerEquipment == null) // 기존 구독과 장비 관리자 확인
        {
            return; // 중복 구독 생략
        }

        playerEquipment.EquipmentChanged += Refresh; // 장비 변경 구독
        eventSubscribed = true; // 이벤트 구독 완료 기록
    }

    private void UnsubscribeEvent() // 장비 변경 이벤트 해제
    {
        if (!eventSubscribed || playerEquipment == null) // 구독 상태와 장비 관리자 확인
        {
            eventSubscribed = false; // 이벤트 상태 초기화
            return; // 이벤트 해제 생략
        }

        playerEquipment.EquipmentChanged -= Refresh; // 장비 변경 구독 해제
        eventSubscribed = false; // 이벤트 구독 상태 초기화
    }

    private void Refresh() // 장비 능력치 화면 갱신
    {
        if (!runtimeInitialized || playerEquipment == null) // 런타임 초기화 확인
        {
            return; // 화면 갱신 중단
        }

        statsText.SetText(
            $"DEFENSE {playerEquipment.TotalDefensePercent:0}%\n"
            + $"MAX HEALTH +{playerEquipment.TotalMaximumHealthBonus:0}\n"
            + $"MOVE SPEED +{playerEquipment.TotalMovementSpeedBonusPercent:0}%\n"
            + $"HUNGER USE -{playerEquipment.TotalHungerReductionPercent:0}%\n"
            + $"THIRST USE -{playerEquipment.TotalThirstReductionPercent:0}%\n"
            + $"COLD RESIST {playerEquipment.TotalColdResistancePercent:0}%\n"
            + $"INVENTORY +{playerEquipment.TotalInventorySlotBonus}"); // 전체 장비 능력치 출력
    }

    private void OnDestroy() // 장비 이벤트 정리
    {
        UnsubscribeEvent(); // 장비 변경 이벤트 해제
    }
}
