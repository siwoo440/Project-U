using System; // 이벤트 기능
using System.Collections.Generic; // 슬롯 목록 기능
using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class StorageContainer : MonoBehaviour, IItemSlotContainer, IBuildRemovalGuard // 보관함 슬롯과 철거 제한 관리자
{
    [Header("Storage")] // 보관함 설정 묶음
    [Tooltip("보관함 종류 데이터.")]
    [SerializeField] private StorageTypeData storageTypeData; // 보관함 종류 데이터
    [Tooltip("보관함 슬롯 목록.")]
    [SerializeField] private List<InventorySlot> slots = new List<InventorySlot>(); // 보관함 슬롯 목록

    [Header("Identity")] // 저장 고유 ID 설정 묶음
    [Tooltip("실제 설치 건축물 정보.")]
    [SerializeField] private PlacedBuildObject placedBuildObject; // 실제 설치 건축물 정보
    [Tooltip("Debug 보관함 임시 ID.")]
    [SerializeField] private string debugStructureId = string.Empty; // Debug 보관함 임시 ID

    public StorageTypeData StorageTypeData => storageTypeData; // 보관함 종류 데이터 제공
    public string StorageTypeId => storageTypeData == null ? string.Empty : storageTypeData.StorageTypeId; // 보관함 종류 ID 제공
    public string DisplayName => storageTypeData == null ? string.Empty : storageTypeData.DisplayName; // 보관함 이름 제공
    public int SlotCapacity => storageTypeData == null ? 0 : storageTypeData.SlotCapacity; // 보관함 슬롯 개수 제공
    public int ColumnCount => storageTypeData == null ? 1 : storageTypeData.ColumnCount; // UI 열 개수 제공
    public IReadOnlyList<InventorySlot> Slots => slots; // 읽기 전용 슬롯 목록 제공
    public bool CanRemove => IsEmpty; // 빈 보관함 철거 허용
    public string RemovalBlockedMessage => "EMPTY STORAGE FIRST"; // 보관함 철거 차단 문구

    public string StructureId // 저장용 보관함 고유 ID 제공
    {
        get // 고유 ID 조회 접근자
        {
            ResolvePlacedBuildObject(); // 실제 설치 건축물 검색

            if (placedBuildObject != null && !string.IsNullOrWhiteSpace(placedBuildObject.StructureId)) // 실제 건축물 ID 확인
            {
                return placedBuildObject.StructureId.Trim(); // 실제 건축물 ID 반환
            }

            return string.IsNullOrWhiteSpace(debugStructureId)
                ? string.Empty
                : debugStructureId.Trim(); // Debug ID 반환
        }
    }

    public bool IsEmpty // 보관함 전체 비어 있음 제공
    {
        get // 빈 보관함 검사 접근자
        {
            for (int index = 0; index < slots.Count; index++) // 전체 슬롯 순회
            {
                InventorySlot slot = slots[index]; // 현재 슬롯 조회

                if (slot != null && slot.ItemData != null && slot.Quantity > 0) // 사용 슬롯 확인
                {
                    return false; // 아이템 존재 반환
                }
            }

            return true; // 전체 빈 상태 반환
        }
    }

    public event Action StorageChanged; // 보관함 내용 변경 알림

    private void Awake() // 보관함 초기화
    {
        ResolvePlacedBuildObject(); // 실제 설치 건축물 검색

        if (!TryValidateStorageType(out string errorMessage)) // 고유 ID 제외 기본 설정 검사
        {
            Debug.LogError($"보관함 초기화 실패\n{errorMessage}", this); // 설정 오류 출력
            enabled = false; // 보관함 기능 비활성화
            return; // 초기화 중단
        }

        EnsureSlotCapacity(); // 고정 슬롯 개수 생성
    }

    private void OnValidate() // Inspector 설정값 검증
    {
        debugStructureId = string.IsNullOrWhiteSpace(debugStructureId)
            ? string.Empty
            : debugStructureId.Trim(); // Debug ID 공백 제거

        if (storageTypeData == null) // 보관함 데이터 존재 확인
        {
            return; // 슬롯 생성 중단
        }

        EnsureSlotCapacity(); // 설정된 용량 적용
    }

    public InventorySlot GetSlot(int index) // 지정 보관함 슬롯 조회
    {
        if (index < 0 || index >= slots.Count) // 슬롯 범위 확인
        {
            return null; // 잘못된 번호 결과
        }

        return slots[index]; // 지정 슬롯 반환
    }

    public bool TrySetSlotDirect(int index, InventorySlot slot) // 공통 이동용 슬롯 직접 변경
    {
        EnsureSlotCapacity(); // 현재 슬롯 구조 확인

        if (index < 0 || index >= slots.Count) // 슬롯 번호 범위 확인
        {
            return false; // 슬롯 변경 실패
        }

        slots[index] = slot; // 지정 슬롯 참조 적용
        return true; // 슬롯 변경 성공
    }

    public bool TryValidateSetup(out string errorMessage) // 전체 보관함 설정 검사
    {
        if (!TryValidateStorageType(out errorMessage)) // 보관함 종류 설정 검사
        {
            return false; // 검사 실패
        }

        if (string.IsNullOrWhiteSpace(StructureId)) // 저장 고유 ID 확인
        {
            errorMessage = "Placed Build Object 또는 Debug Structure ID가 필요합니다."; // ID 오류 저장
            return false; // 검사 실패
        }

        errorMessage = string.Empty; // 오류 내용 초기화
        return true; // 검사 성공
    }

    public void NotifyContentsChanged() // 공통 이동 후 보관함 변경 알림
    {
        StorageChanged?.Invoke(); // 보관함 UI 갱신 요청
    }

    public void NotifyStorageChanged() // 기존 변경 알림 호환
    {
        NotifyContentsChanged(); // 공통 변경 알림 호출
    }

    public void ClearItemsForLoad() // 불러오기 전 보관함 초기화
    {
        EnsureSlotCapacity(); // 현재 슬롯 구조 확인

        for (int index = 0; index < slots.Count; index++) // 전체 슬롯 순회
        {
            slots[index] = null; // 현재 슬롯 아이템 제거
        }
    }

    public bool TrySetSlotForLoad(int index, ItemData itemData, int quantity) // 저장 슬롯 복원
    {
        EnsureSlotCapacity(); // 현재 슬롯 구조 확인

        if (index < 0 || index >= slots.Count) // 슬롯 번호 범위 확인
        {
            return false; // 슬롯 복원 실패
        }

        if (itemData == null) // 아이템 데이터 존재 확인
        {
            return false; // 슬롯 복원 실패
        }

        if (quantity <= 0 || quantity > itemData.MaximumStack) // 저장 수량 범위 확인
        {
            return false; // 슬롯 복원 실패
        }

        if (slots[index] != null) // 기존 슬롯 사용 여부 확인
        {
            return false; // 중복 슬롯 복원 차단
        }

        slots[index] = new InventorySlot(itemData, quantity); // 저장 아이템 배치
        return true; // 슬롯 복원 성공
    }

    private bool TryValidateStorageType(out string errorMessage) // 고유 ID 제외 보관함 설정 검사
    {
        if (storageTypeData == null) // 보관함 종류 데이터 확인
        {
            errorMessage = "Storage Type Data 참조가 누락되었습니다."; // 데이터 오류 저장
            return false; // 검사 실패
        }

        if (string.IsNullOrWhiteSpace(storageTypeData.StorageTypeId)) // 보관함 종류 ID 확인
        {
            errorMessage = "Storage Type ID가 비어 있습니다."; // ID 오류 저장
            return false; // 검사 실패
        }

        if (storageTypeData.SlotCapacity <= 0) // 슬롯 개수 확인
        {
            errorMessage = "보관함 슬롯 개수는 1개 이상이어야 합니다."; // 용량 오류 저장
            return false; // 검사 실패
        }

        if (storageTypeData.ColumnCount <= 0) // UI 열 개수 확인
        {
            errorMessage = "보관함 UI 열 개수는 1개 이상이어야 합니다."; // 열 개수 오류 저장
            return false; // 검사 실패
        }

        errorMessage = string.Empty; // 오류 내용 초기화
        return true; // 검사 성공
    }

    private void ResolvePlacedBuildObject() // 설치 건축물 참조 검색
    {
        if (placedBuildObject != null) // 기존 참조 확인
        {
            return; // 재검색 중단
        }

        placedBuildObject = GetComponentInParent<PlacedBuildObject>(); // 상위 건축물 정보 검색
    }

    private void EnsureSlotCapacity() // 고정 슬롯 개수 적용
    {
        int targetCapacity = storageTypeData == null ? 0 : storageTypeData.SlotCapacity; // 목표 슬롯 개수 조회

        while (slots.Count < targetCapacity) // 부족 슬롯 확인
        {
            slots.Add(null); // 빈 슬롯 추가
        }

        while (slots.Count > targetCapacity) // 초과 슬롯 확인
        {
            slots.RemoveAt(slots.Count - 1); // 마지막 초과 슬롯 제거
        }
    }
}
