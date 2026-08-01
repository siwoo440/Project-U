using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class InventoryDetailUI : MonoBehaviour // 아이템 상세 정보 UI
{
    [Header("Runtime References")] // 런타임 기능 참조 묶음
    [Tooltip("플레이어 인벤토리.")]
    [SerializeField] private PlayerInventory playerInventory; // 플레이어 인벤토리
    [Tooltip("아이템 버리기 기능.")]
    [SerializeField] private InventoryItemDropper itemDropper; // 아이템 버리기 기능
    [Tooltip("플레이어 장비 관리자.")]
    [SerializeField] private PlayerEquipment playerEquipment; // 플레이어 장비 관리자

    [Header("State")] // 상태 화면 묶음
    [Tooltip("미선택 화면.")]
    [SerializeField] private GameObject emptyStateRoot; // 미선택 화면
    [Tooltip("상세 정보 화면.")]
    [SerializeField] private GameObject detailContentRoot; // 상세 정보 화면

    [Header("Display")] // 표시 요소 묶음
    [Tooltip("아이템 아이콘.")]
    [SerializeField] private Image itemIconImage; // 아이템 아이콘
    [Tooltip("아이템 이름.")]
    [SerializeField] private TMP_Text itemNameText; // 아이템 이름
    [Tooltip("아이템 분류.")]
    [SerializeField] private TMP_Text categoryText; // 아이템 분류
    [Tooltip("아이템 설명.")]
    [SerializeField] private TMP_Text descriptionText; // 아이템 설명
    [Tooltip("아이템 수량.")]
    [SerializeField] private TMP_Text quantityText; // 아이템 수량

    [Header("Actions")] // 동작 버튼 묶음
    [Tooltip("한 개 제거 버튼.")]
    [SerializeField] private Button removeOneButton; // 한 개 제거 버튼
    [Tooltip("한 개 버리기 버튼.")]
    [SerializeField] private Button dropOneButton; // 한 개 버리기 버튼
    [Tooltip("선택 장비 장착 버튼.")]
    [SerializeField] private Button equipButton; // 선택 장비 장착 버튼

    private bool internalReferencesValid; // UI 내부 참조 연결 상태
    private bool runtimeInitialized; // 런타임 기능 참조 초기화 상태
    private bool eventsSubscribed; // 인벤토리 이벤트 구독 상태

    private void Awake() // 상세 UI 내부 초기화
    {
        internalReferencesValid =
            emptyStateRoot != null
            && detailContentRoot != null
            && itemIconImage != null
            && itemNameText != null
            && categoryText != null
            && descriptionText != null
            && quantityText != null
            && removeOneButton != null
            && dropOneButton != null
            && equipButton != null; // UI 내부 참조 검사

        if (!internalReferencesValid) // 내부 참조 누락 확인
        {
            Debug.LogError($"{gameObject.name}의 아이템 상세 UI 내부 참조가 누락되었습니다.", this); // 참조 오류 출력
            enabled = false; // 상세 기능 비활성화
            return; // 초기화 중단
        }

        removeOneButton.onClick.AddListener(RemoveSelectedItem); // 제거 버튼 기능 연결
        dropOneButton.onClick.AddListener(DropSelectedItem); // 버리기 버튼 기능 연결
        equipButton.onClick.AddListener(EquipSelectedItem); // 장비 장착 기능 연결
    }

    private void OnEnable() // 변경 이벤트 연결
    {
        if (!runtimeInitialized) // 런타임 초기화 상태 확인
        {
            return; // 이벤트 연결 중단
        }

        SubscribeEvents(); // 인벤토리 변경 이벤트 연결
        Refresh(); // 현재 정보 표시
    }

    private void OnDisable() // 변경 이벤트 해제
    {
        UnsubscribeEvents(); // 인벤토리 변경 이벤트 해제
    }

    public bool Initialize(
        PlayerInventory inventory,
        InventoryItemDropper dropper,
        PlayerEquipment equipment) // 런타임 기능 참조 초기화
    {
        if (!internalReferencesValid
            || inventory == null
            || dropper == null
            || equipment == null) // 내부와 외부 참조 확인
        {
            Debug.LogError($"{gameObject.name}의 아이템 상세 UI 런타임 참조가 누락되었습니다.", this); // 참조 오류 출력
            runtimeInitialized = false; // 초기화 실패 기록
            return false; // 초기화 실패 반환
        }

        UnsubscribeEvents(); // 기존 인벤토리 이벤트 해제
        playerInventory = inventory; // 플레이어 인벤토리 저장
        itemDropper = dropper; // 아이템 버리기 관리자 저장
        playerEquipment = equipment; // 플레이어 장비 관리자 저장
        runtimeInitialized = true; // 런타임 초기화 완료 기록

        if (isActiveAndEnabled) // 현재 화면 활성 상태 확인
        {
            SubscribeEvents(); // 인벤토리 변경 이벤트 연결
            Refresh(); // 현재 정보 표시
        }

        return true; // 초기화 성공 반환
    }

    private void SubscribeEvents() // 인벤토리 변경 이벤트 연결
    {
        if (eventsSubscribed || playerInventory == null) // 기존 구독과 인벤토리 확인
        {
            return; // 중복 구독 생략
        }

        playerInventory.InventoryChanged += Refresh; // 인벤토리 변경 구독
        playerInventory.InventorySelectionChanged += Refresh; // 슬롯 선택 변경 구독
        eventsSubscribed = true; // 이벤트 구독 완료 기록
    }

    private void UnsubscribeEvents() // 인벤토리 변경 이벤트 해제
    {
        if (!eventsSubscribed || playerInventory == null) // 구독 상태와 인벤토리 확인
        {
            eventsSubscribed = false; // 이벤트 상태 초기화
            return; // 이벤트 해제 생략
        }

        playerInventory.InventoryChanged -= Refresh; // 인벤토리 변경 해제
        playerInventory.InventorySelectionChanged -= Refresh; // 슬롯 선택 변경 해제
        eventsSubscribed = false; // 이벤트 구독 상태 초기화
    }

    private void Refresh() // 상세 정보 갱신
    {
        if (!runtimeInitialized || playerInventory == null) // 런타임 초기화 확인
        {
            return; // 화면 갱신 중단
        }

        int selectedIndex = playerInventory.SelectedInventoryIndex; // 선택 슬롯 번호 조회
        InventorySlot selectedSlot = playerInventory.GetSlot(selectedIndex); // 선택 슬롯 조회

        if (selectedSlot == null) // 선택 아이템 없음 확인
        {
            emptyStateRoot.SetActive(true); // 미선택 화면 표시
            detailContentRoot.SetActive(false); // 상세 정보 숨김
            equipButton.interactable = false; // 장비 장착 버튼 비활성화
            return; // 갱신 종료
        }

        ItemData itemData = selectedSlot.ItemData; // 선택 아이템 데이터 조회
        Sprite itemIcon = itemData.Icon; // 아이템 아이콘 조회

        emptyStateRoot.SetActive(false); // 미선택 화면 숨김
        detailContentRoot.SetActive(true); // 상세 정보 표시
        itemIconImage.sprite = itemIcon; // 아이콘 적용
        itemIconImage.color = itemIcon == null
            ? ItemIconUtility.GetFallbackColor(itemData.ItemCategory)
            : Color.white; // 실제 또는 대체 색상 적용
        itemNameText.SetText(itemData.DisplayName); // 아이템 이름 출력
        categoryText.SetText(GetCategoryLabel(itemData.ItemCategory)); // 아이템 분류 출력
        descriptionText.SetText(
            string.IsNullOrWhiteSpace(itemData.Description)
                ? "NO DESCRIPTION"
                : itemData.Description); // 아이템 설명 출력
        quantityText.SetText($"QUANTITY: {selectedSlot.Quantity} / {itemData.MaximumStack}"); // 아이템 수량 출력
        removeOneButton.interactable = true; // 제거 버튼 활성화
        dropOneButton.interactable = true; // 버리기 버튼 활성화
        equipButton.interactable =
            itemData.IsEquipment
            && itemData.EquipmentSlotType != EquipmentSlotType.None; // 장착 가능한 장비 여부 적용
    }

    private void RemoveSelectedItem() // 선택 아이템 한 개 제거
    {
        if (!runtimeInitialized) // 런타임 초기화 상태 확인
        {
            return; // 제거 처리 중단
        }

        int selectedIndex = playerInventory.SelectedInventoryIndex; // 선택 슬롯 번호 조회
        playerInventory.RemoveItemFromSlot(selectedIndex, 1); // 아이템 한 개 영구 제거
    }

    private void DropSelectedItem() // 선택 아이템 한 개 버리기
    {
        if (!runtimeInitialized) // 런타임 초기화 상태 확인
        {
            return; // 버리기 처리 중단
        }

        int selectedIndex = playerInventory.SelectedInventoryIndex; // 선택 슬롯 번호 조회
        itemDropper.DropFromSlot(selectedIndex, 1); // 아이템 한 개 월드 생성
    }

    private void EquipSelectedItem() // 선택 장비 장착
    {
        if (!runtimeInitialized) // 런타임 초기화 상태 확인
        {
            return; // 장착 처리 중단
        }

        int selectedIndex = playerInventory.SelectedInventoryIndex; // 선택 슬롯 번호 조회

        if (selectedIndex < 0) // 선택 슬롯 존재 확인
        {
            return; // 장착 처리 중단
        }

        playerEquipment.TryEquipFromInventory(selectedIndex); // 선택 장비 장착 시도
        Refresh(); // 상세 화면 갱신
    }

    private void OnDestroy() // 버튼과 이벤트 연결 정리
    {
        UnsubscribeEvents(); // 인벤토리 변경 이벤트 해제

        if (removeOneButton != null) // 제거 버튼 존재 확인
        {
            removeOneButton.onClick.RemoveListener(RemoveSelectedItem); // 제거 이벤트 해제
        }

        if (dropOneButton != null) // 버리기 버튼 존재 확인
        {
            dropOneButton.onClick.RemoveListener(DropSelectedItem); // 버리기 이벤트 해제
        }

        if (equipButton != null) // 장착 버튼 존재 확인
        {
            equipButton.onClick.RemoveListener(EquipSelectedItem); // 장착 이벤트 해제
        }
    }

    private string GetCategoryLabel(ItemCategory itemCategory) // 분류 표시 문구 반환
    {
        switch (itemCategory) // 아이템 분류 확인
        {
            case ItemCategory.CraftingMaterial: // 제작 재료 분기
                return "CRAFTING MATERIAL"; // 제작 재료 문구 반환

            case ItemCategory.Tool: // 도구 분기
                return "TOOL"; // 도구 문구 반환

            case ItemCategory.Food: // 음식 분기
                return "FOOD"; // 음식 문구 반환

            case ItemCategory.Drink: // 음료 분기
                return "DRINK"; // 음료 문구 반환

            case ItemCategory.Medicine: // 의약품 분기
                return "MEDICINE"; // 의약품 문구 반환

            case ItemCategory.Equipment: // 장비 분기
                return "EQUIPMENT"; // 장비 문구 반환

            default: // 미정 분류
                return "UNKNOWN"; // 미정 문구 반환
        }
    }
}
