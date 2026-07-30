using UnityEngine; // Unity 기본 기능

[CreateAssetMenu(fileName = "StorageType_New", menuName = "Project U/Storage Type Data")] // 보관함 데이터 생성 메뉴
public sealed class StorageTypeData : ScriptableObject // 보관함 종류 데이터
{
    [Header("Identity")] // 식별 정보 묶음
    [SerializeField] private string storageTypeId = "storage_new"; // 보관함 종류 고유 ID
    [SerializeField] private string displayName = "NEW STORAGE"; // 보관함 표시 이름

    [Header("Capacity")] // 용량 정보 묶음
    [SerializeField] private int slotCapacity = 12; // 전체 슬롯 개수
    [SerializeField] private int columnCount = 4; // UI 가로 열 개수

    public string StorageTypeId => storageTypeId; // 보관함 종류 ID 제공
    public string DisplayName => displayName; // 보관함 이름 제공
    public int SlotCapacity => Mathf.Max(1, slotCapacity); // 보관함 슬롯 개수 제공
    public int ColumnCount => Mathf.Clamp(columnCount, 1, SlotCapacity); // UI 열 개수 제공
    public int RowCount => Mathf.CeilToInt(SlotCapacity / (float)ColumnCount); // UI 행 개수 제공

    private void OnValidate() // Inspector 설정값 검증
    {
        storageTypeId = string.IsNullOrWhiteSpace(storageTypeId) ? string.Empty : storageTypeId.Trim(); // ID 공백 제거
        displayName = string.IsNullOrWhiteSpace(displayName) ? string.Empty : displayName.Trim(); // 표시 이름 공백 제거
        slotCapacity = Mathf.Max(1, slotCapacity); // 슬롯 최소값 적용
        columnCount = Mathf.Clamp(columnCount, 1, slotCapacity); // 열 개수 범위 적용
    }
}

public static class StorageTypeIds // 보관함 종류 ID 관리
{
    public const string SmallChest = "storage_small_chest"; // 소형 보관함 ID
    public const string LargeChest = "storage_large_chest"; // 대형 보관함 ID
}