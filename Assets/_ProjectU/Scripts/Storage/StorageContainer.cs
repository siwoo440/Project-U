using System; // 이벤트 기능
using System.Collections.Generic; // 슬롯 목록 기능
using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class StorageContainer : MonoBehaviour // 보관함 슬롯 관리자
{
    [Header("Storage")] // 보관함 설정 묶음
    [SerializeField] private StorageTypeData storageTypeData; // 보관함 종류 데이터
    [SerializeField] private List<InventorySlot> slots = new List<InventorySlot>(); // 보관함 슬롯 목록

    public StorageTypeData StorageTypeData => storageTypeData; // 보관함 종류 데이터 제공
    public string StorageTypeId => storageTypeData == null ? string.Empty : storageTypeData.StorageTypeId; // 보관함 종류 ID 제공
    public string DisplayName => storageTypeData == null ? string.Empty : storageTypeData.DisplayName; // 보관함 표시 이름 제공
    public int SlotCapacity => storageTypeData == null ? 0 : storageTypeData.SlotCapacity; // 보관함 슬롯 개수 제공
    public int ColumnCount => storageTypeData == null ? 1 : storageTypeData.ColumnCount; // UI 열 개수 제공
    public IReadOnlyList<InventorySlot> Slots => slots; // 읽기 전용 슬롯 목록 제공

    public event Action StorageChanged; // 보관함 내용 변경 알림

    private void Awake() // 보관함 초기화
    {
        if (!TryValidateSetup(out string errorMessage)) // 보관함 설정 검사
        {
            Debug.LogError($"보관함 초기화 실패\n{errorMessage}", this); // 설정 오류 출력
            enabled = false; // 보관함 기능 비활성화
            return; // 초기화 중단
        }

        EnsureSlotCapacity(); // 고정 슬롯 개수 생성
    }

    private void OnValidate() // Inspector 설정값 검증
    {
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

    public bool TryValidateSetup(out string errorMessage) // 보관함 설정 검사
    {
        if (storageTypeData == null) // 보관함 종류 데이터 확인
        {
            errorMessage = "Storage Type Data 참조가 누락되었습니다."; // 데이터 오류 저장
            return false; // 검사 실패
        }

        if (string.IsNullOrWhiteSpace(storageTypeData.StorageTypeId)) // 보관함 ID 확인
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

    public void NotifyStorageChanged() // 보관함 변경 상태 알림
    {
        StorageChanged?.Invoke(); // 보관함 UI 갱신 요청
    }

    private void EnsureSlotCapacity() // 고정 슬롯 개수 적용
    {
        int targetCapacity = storageTypeData.SlotCapacity; // 목표 슬롯 개수 조회

        while (slots.Count < targetCapacity) // 부족한 슬롯 확인
        {
            slots.Add(null); // 빈 슬롯 추가
        }

        while (slots.Count > targetCapacity) // 초과 슬롯 확인
        {
            slots.RemoveAt(slots.Count - 1); // 마지막 초과 슬롯 제거
        }
    }
}