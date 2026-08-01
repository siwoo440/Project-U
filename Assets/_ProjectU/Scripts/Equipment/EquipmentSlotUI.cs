using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class EquipmentSlotUI : MonoBehaviour // 장비 슬롯 화면 관리
{
    [Header("Runtime Reference")] // 런타임 외부 참조 묶음
    [Tooltip("플레이어 장비 관리자.")]
    [SerializeField] private PlayerEquipment playerEquipment; // 플레이어 장비 관리자

    [Header("Display")] // 화면 요소 참조 묶음
    [Tooltip("장비 아이콘 이미지.")]
    [SerializeField] private Image itemIconImage; // 장비 아이콘 이미지
    [Tooltip("슬롯 이름 문구.")]
    [SerializeField] private TMP_Text slotNameText; // 슬롯 이름 문구
    [Tooltip("장비 이름 문구.")]
    [SerializeField] private TMP_Text itemNameText; // 장비 이름 문구
    [Tooltip("장비 해제 버튼.")]
    [SerializeField] private Button unequipButton; // 장비 해제 버튼

    [Header("Slot")] // 슬롯 설정 묶음
    [Tooltip("담당 장비 슬롯.")]
    [SerializeField] private EquipmentSlotType slotType = EquipmentSlotType.None; // 담당 장비 슬롯

    private bool internalReferencesValid; // UI 내부 참조 상태
    private bool runtimeInitialized; // 런타임 장비 참조 초기화 상태
    private bool eventSubscribed; // 장비 이벤트 구독 상태

    private void Awake() // 장비 슬롯 내부 초기화
    {
        internalReferencesValid =
            itemIconImage != null
            && slotNameText != null
            && itemNameText != null
            && unequipButton != null; // UI 내부 참조 검사

        if (!internalReferencesValid) // 내부 참조 누락 확인
        {
            Debug.LogError($"{gameObject.name}의 장비 슬롯 UI 내부 참조가 누락되었습니다.", this); // 참조 오류 출력
            enabled = false; // 장비 슬롯 기능 비활성화
            return; // 초기화 중단
        }

        slotNameText.SetText(GetSlotLabel(slotType)); // 슬롯 이름 표시
        unequipButton.onClick.AddListener(UnequipCurrentItem); // 장비 해제 기능 연결
    }

    private void OnEnable() // 장비 변경 이벤트 연결
    {
        if (!runtimeInitialized) // 런타임 초기화 상태 확인
        {
            return; // 이벤트 연결 중단
        }

        SubscribeEvent(); // 장비 변경 이벤트 연결
        Refresh(); // 현재 장비 표시
    }

    private void OnDisable() // 장비 변경 이벤트 해제
    {
        UnsubscribeEvent(); // 장비 변경 이벤트 해제
    }

    public bool Initialize(PlayerEquipment equipment) // 런타임 장비 관리자 초기화
    {
        if (!internalReferencesValid || equipment == null) // 내부와 외부 참조 확인
        {
            Debug.LogError($"{gameObject.name}의 장비 슬롯 UI 런타임 참조가 누락되었습니다.", this); // 참조 오류 출력
            runtimeInitialized = false; // 초기화 실패 기록
            return false; // 초기화 실패 반환
        }

        UnsubscribeEvent(); // 기존 장비 이벤트 해제
        playerEquipment = equipment; // 플레이어 장비 관리자 저장
        runtimeInitialized = true; // 런타임 초기화 완료 기록

        if (isActiveAndEnabled) // 현재 화면 활성 상태 확인
        {
            SubscribeEvent(); // 장비 변경 이벤트 연결
            Refresh(); // 현재 장비 표시
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
            eventSubscribed = false; // 이벤트 구독 상태 초기화
            return; // 이벤트 해제 생략
        }

        playerEquipment.EquipmentChanged -= Refresh; // 장비 변경 구독 해제
        eventSubscribed = false; // 이벤트 구독 상태 초기화
    }

    private void Refresh() // 장비 슬롯 화면 갱신
    {
        if (!runtimeInitialized || playerEquipment == null) // 런타임 초기화 확인
        {
            return; // 화면 갱신 중단
        }

        ItemData equippedItem = playerEquipment.GetEquippedItem(slotType); // 현재 장착 장비 조회

        if (equippedItem == null) // 빈 장비 슬롯 확인
        {
            itemIconImage.sprite = null; // 장비 아이콘 제거
            itemIconImage.color = new Color(1f, 1f, 1f, 0.15f); // 빈 슬롯 색상 적용
            itemNameText.SetText("EMPTY"); // 빈 슬롯 문구 표시
            unequipButton.interactable = false; // 장비 해제 차단
            return; // 화면 갱신 종료
        }

        Sprite itemIcon = equippedItem.Icon; // 장비 아이콘 조회
        itemIconImage.sprite = itemIcon; // 장비 아이콘 적용
        itemIconImage.color = itemIcon == null
            ? ItemIconUtility.GetFallbackColor(equippedItem.ItemCategory)
            : Color.white; // 실제 또는 대체 색상 적용
        itemNameText.SetText(equippedItem.DisplayName); // 장비 이름 표시
        unequipButton.interactable = true; // 장비 해제 허용
    }

    private void UnequipCurrentItem() // 현재 장비 해제
    {
        if (!runtimeInitialized || playerEquipment == null) // 런타임 장비 관리자 확인
        {
            return; // 장비 해제 중단
        }

        playerEquipment.TryUnequip(slotType); // 장비 인벤토리 이동 시도
    }

    private void OnDestroy() // 버튼과 이벤트 정리
    {
        UnsubscribeEvent(); // 장비 변경 이벤트 해제

        if (unequipButton != null) // 버튼 참조 확인
        {
            unequipButton.onClick.RemoveListener(UnequipCurrentItem); // 장비 해제 기능 제거
        }
    }

    private string GetSlotLabel(EquipmentSlotType targetSlotType) // 장비 슬롯 문구 반환
    {
        switch (targetSlotType) // 장비 슬롯 종류 확인
        {
            case EquipmentSlotType.Head: // 머리 슬롯 분기
                return "HEAD"; // 머리 문구 반환

            case EquipmentSlotType.Body: // 몸 슬롯 분기
                return "BODY"; // 몸 문구 반환

            case EquipmentSlotType.Legs: // 다리 슬롯 분기
                return "LEGS"; // 다리 문구 반환

            case EquipmentSlotType.Feet: // 신발 슬롯 분기
                return "FEET"; // 신발 문구 반환

            case EquipmentSlotType.Backpack: // 가방 슬롯 분기
                return "BACKPACK"; // 가방 문구 반환

            default: // 미정 슬롯 분기
                return "NONE"; // 미정 문구 반환
        }
    }
}
