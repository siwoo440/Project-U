using System; // 기본 직렬화와 날짜 기능
using System.Collections.Generic; // 목록 데이터 기능
using UnityEngine; // Unity 위치와 회전 기능

[Serializable] // JSON 직렬화 허용
public sealed class SaveGameData // 전체 저장 파일 최상위 데이터
{
    [Tooltip("저장 형식 버전.")]
    public int saveVersion = SaveVersionPolicy.CurrentVersion; // 저장 형식 버전
    [Tooltip("저장 슬롯 식별자.")]
    public string saveSlotId = "slot_01"; // 저장 슬롯 식별자
    [Tooltip("UTC 저장 시각.")]
    public string savedAtUtc = string.Empty; // UTC 저장 시각
    [Tooltip("저장된 Scene 이름.")]
    public string sceneName = "20_Gameplay"; // 저장된 Scene 이름
    [Tooltip("플레이어 데이터.")]
    public PlayerSaveData player = new PlayerSaveData(); // 플레이어 데이터
    [Tooltip("시간 데이터.")]
    public TimeSaveData time = new TimeSaveData(); // 시간 데이터
    [Tooltip("인벤토리 데이터.")]
    public InventorySaveData inventory = new InventorySaveData(); // 인벤토리 데이터
    [Tooltip("장비 데이터.")]
    public EquipmentSaveData equipment = new EquipmentSaveData(); // 장비 데이터
    [Tooltip("월드 데이터.")]
    public WorldSaveData world = new WorldSaveData(); // 월드 데이터
    [Tooltip("부활 지점 데이터.")]
    public RespawnSaveData respawn = new RespawnSaveData(); // 부활 지점 데이터
    [Tooltip("제작법 해금 저장 데이터 존재 여부.")]
    public bool hasCraftingData; // 제작법 해금 저장 데이터 존재 여부
    [Tooltip("제작법 해금 데이터.")]
    public CraftingSaveData crafting = new CraftingSaveData(); // 제작법 해금 데이터
    [Tooltip("보관함 저장 데이터 존재 여부.")]
    public bool hasStorageData; // 보관함 저장 데이터 존재 여부
    [Tooltip("전체 보관함 저장 데이터.")]
    public StorageSaveData storage = new StorageSaveData(); // 전체 보관함 저장 데이터

    public static SaveGameData CreateNew(string newSceneName) // 새로운 저장 데이터 생성
    {
        SaveGameData saveData = new SaveGameData(); // 기본 저장 데이터 생성
        saveData.saveVersion = SaveVersionPolicy.CurrentVersion; // 현재 버전 적용
        saveData.savedAtUtc = DateTime.UtcNow.ToString("O"); // 국제 표준 UTC 시각 적용
        saveData.sceneName = string.IsNullOrWhiteSpace(newSceneName)
            ? "20_Gameplay"
            : newSceneName.Trim(); // Scene 이름 보정
        return saveData; // 생성 데이터 반환
    }
}

[Serializable] // JSON 직렬화 허용
public sealed class PlayerSaveData // 플레이어 상태 저장 데이터
{
    [Tooltip("플레이어 위치.")]
    public SaveVector3Data position = new SaveVector3Data(); // 플레이어 위치
    [Tooltip("플레이어 회전.")]
    public SaveQuaternionData rotation = new SaveQuaternionData(); // 플레이어 회전
    [Tooltip("현재 체력.")]
    public float health = 100f; // 현재 체력
    [Tooltip("현재 허기.")]
    public float hunger = 100f; // 현재 허기
    [Tooltip("현재 갈증.")]
    public float thirst = 100f; // 현재 갈증
    [Tooltip("현재 스태미나.")]
    public float stamina = 100f; // 현재 스태미나
    [Tooltip("현재 젖음 수치.")]
    public float wetness; // 현재 젖음 수치
    [Tooltip("체온 저장 데이터 존재 여부.")]
    public bool hasTemperatureData; // 체온 저장 데이터 존재 여부
    [Tooltip("현재 체온 수치.")]
    public float temperature = 100f; // 현재 체온 수치
}

[Serializable] // JSON 직렬화 허용
public sealed class TimeSaveData // 시간과 날씨 저장 데이터
{
    [Tooltip("현재 날짜.")]
    public int currentDay = 1; // 현재 날짜
    [Tooltip("현재 시각.")]
    public float currentHour = 8f; // 현재 시각
    [Tooltip("날씨 저장 데이터 존재 여부.")]
    public bool hasWeatherData; // 날씨 저장 데이터 존재 여부
    [Tooltip("현재 날씨 숫자값.")]
    public int currentWeather = (int)WeatherType.Clear; // 현재 날씨 숫자값
    [Tooltip("현재 날씨 남은 시간.")]
    public float remainingWeatherHours = 6f; // 현재 날씨 남은 시간
}

[Serializable] // JSON 직렬화 허용
public sealed class InventorySaveData // 인벤토리 저장 데이터
{
    [Tooltip("선택 핫바 번호.")]
    public int selectedHotbarIndex; // 선택 핫바 번호
    [Tooltip("사용 슬롯 목록.")]
    public List<InventorySlotSaveData> slots = new List<InventorySlotSaveData>(); // 사용 슬롯 목록
}

[Serializable] // JSON 직렬화 허용
public sealed class InventorySlotSaveData // 단일 인벤토리 슬롯 데이터
{
    [Tooltip("실제 슬롯 번호.")]
    public int slotIndex; // 실제 슬롯 번호
    [Tooltip("보관 아이템 ID.")]
    public string itemId = string.Empty; // 보관 아이템 ID
    [Tooltip("보관 수량.")]
    public int quantity; // 보관 수량
}

[Serializable] // JSON 직렬화 허용
public sealed class EquipmentSaveData // 플레이어 장비 저장 데이터
{
    [Tooltip("장비 슬롯 목록.")]
    public List<EquipmentSlotSaveData> slots = new List<EquipmentSlotSaveData>(); // 장비 슬롯 목록
}

[Serializable] // JSON 직렬화 허용
public sealed class EquipmentSlotSaveData // 단일 장비 슬롯 데이터
{
    [Tooltip("EquipmentSlotType 숫자값.")]
    public int slotType; // EquipmentSlotType 숫자값
    [Tooltip("장착 아이템 ID.")]
    public string itemId = string.Empty; // 장착 아이템 ID
}

[Serializable] // JSON 직렬화 허용
public sealed class WorldSaveData // 월드 진행 상태 저장 데이터
{
    [Tooltip("월드 상태 저장 완료 여부.")]
    public bool hasCapturedWorldState; // 월드 상태 저장 완료 여부
    [Tooltip("설치 건축물 상태 저장 완료 여부.")]
    public bool hasCapturedPlacedStructureState; // 설치 건축물 상태 저장 완료 여부
    [Tooltip("월드 아이템 목록.")]
    public List<WorldItemSaveData> worldItems = new List<WorldItemSaveData>(); // 월드 아이템 목록
    [Tooltip("채집 자원 목록.")]
    public List<GatherableResourceSaveData> gatherableResources = new List<GatherableResourceSaveData>(); // 채집 자원 목록
    [Tooltip("설치 건축물 목록.")]
    public List<PlacedStructureSaveData> placedStructures = new List<PlacedStructureSaveData>(); // 설치 건축물 목록
}

[Serializable] // JSON 직렬화 허용
public sealed class WorldItemSaveData // 월드 아이템 저장 데이터
{
    [Tooltip("월드 오브젝트 고유 ID.")]
    public string worldObjectId = string.Empty; // 월드 오브젝트 고유 ID
    [Tooltip("아이템 데이터 ID.")]
    public string itemId = string.Empty; // 아이템 데이터 ID
    [Tooltip("월드 보관 수량.")]
    public int quantity; // 월드 보관 수량
    [Tooltip("월드 위치.")]
    public SaveVector3Data position = new SaveVector3Data(); // 월드 위치
    [Tooltip("월드 회전.")]
    public SaveQuaternionData rotation = new SaveQuaternionData(); // 월드 회전
}

[Serializable] // JSON 직렬화 허용
public sealed class GatherableResourceSaveData // 채집 자원 저장 데이터
{
    [Tooltip("채집 자원 고유 ID.")]
    public string worldObjectId = string.Empty; // 채집 자원 고유 ID
    [Tooltip("남은 채집 수량.")]
    public int remainingQuantity; // 남은 채집 수량
    [Tooltip("현재 소진 상태.")]
    public bool isDepleted; // 현재 소진 상태
    [Tooltip("재생성까지 남은 시간.")]
    public float respawnRemainingSeconds; // 재생성까지 남은 시간
}

[Serializable] // JSON 직렬화 허용
public sealed class PlacedStructureSaveData // 설치 건축물 저장 데이터
{
    [Tooltip("설치 건축물 고유 ID.")]
    public string structureId = string.Empty; // 설치 건축물 고유 ID
    [Tooltip("건축 Recipe ID.")]
    public string recipeId = string.Empty; // 건축 Recipe ID
    [Tooltip("설치 위치.")]
    public SaveVector3Data position = new SaveVector3Data(); // 설치 위치
    [Tooltip("설치 회전.")]
    public SaveQuaternionData rotation = new SaveQuaternionData(); // 설치 회전
    [Tooltip("모닥불 추가 상태 존재 여부.")]
    public bool hasCampfireState; // 모닥불 추가 상태 존재 여부
    [Tooltip("모닥불 추가 상태.")]
    public CampfireSaveData campfire = new CampfireSaveData(); // 모닥불 추가 상태
}

[Serializable] // JSON 직렬화 허용
public sealed class CampfireSaveData // 모닥불 조리 저장 데이터
{
    [Tooltip("조리 진행 상태.")]
    public bool isCooking; // 조리 진행 상태
    [Tooltip("완성 음식 보관 상태.")]
    public bool hasReadyResult; // 완성 음식 보관 상태
    [Tooltip("남은 조리 시간.")]
    public float remainingCookingTime; // 남은 조리 시간
}

[Serializable] // JSON 직렬화 허용
public sealed class RespawnSaveData // 부활 지점 저장 데이터
{
    [Tooltip("침낭 등록 여부.")]
    public bool hasRegisteredPoint; // 침낭 등록 여부
    [Tooltip("등록 침낭 건축물 ID.")]
    public string registeredStructureId = string.Empty; // 등록 침낭 건축물 ID
}

[Serializable] // JSON 직렬화 허용
public sealed class CraftingSaveData // 제작법 해금 저장 데이터
{
    [Tooltip("해금된 제작법 ID 목록.")]
    public List<string> unlockedRecipeIds = new List<string>(); // 해금된 제작법 ID 목록
}

[Serializable] // JSON 직렬화 허용
public sealed class StorageSaveData // 전체 보관함 저장 데이터
{
    [Tooltip("전체 보관함 목록.")]
    public List<StorageContainerSaveData> containers = new List<StorageContainerSaveData>(); // 전체 보관함 목록
}

[Serializable] // JSON 직렬화 허용
public sealed class StorageContainerSaveData // 단일 보관함 저장 데이터
{
    [Tooltip("보관함 건축물 고유 ID.")]
    public string structureId = string.Empty; // 보관함 건축물 고유 ID
    [Tooltip("보관함 종류 고유 ID.")]
    public string storageTypeId = string.Empty; // 보관함 종류 고유 ID
    [Tooltip("사용 중인 슬롯 목록.")]
    public List<StorageSlotSaveData> slots = new List<StorageSlotSaveData>(); // 사용 중인 슬롯 목록
}

[Serializable] // JSON 직렬화 허용
public sealed class StorageSlotSaveData // 단일 보관함 슬롯 저장 데이터
{
    [Tooltip("실제 보관함 슬롯 번호.")]
    public int slotIndex; // 실제 보관함 슬롯 번호
    [Tooltip("보관 아이템 고유 ID.")]
    public string itemId = string.Empty; // 보관 아이템 고유 ID
    [Tooltip("보관 아이템 수량.")]
    public int quantity; // 보관 아이템 수량
}

[Serializable] // JSON 직렬화 허용
public sealed class SaveVector3Data // Vector3 저장용 데이터
{
    [Tooltip("X 위치값.")]
    public float x; // X 위치값
    [Tooltip("Y 위치값.")]
    public float y; // Y 위치값
    [Tooltip("Z 위치값.")]
    public float z; // Z 위치값

    public static SaveVector3Data FromVector3(Vector3 value) // Vector3 저장 데이터 변환
    {
        SaveVector3Data data = new SaveVector3Data(); // 새로운 위치 데이터 생성
        data.x = value.x; // X 위치 저장
        data.y = value.y; // Y 위치 저장
        data.z = value.z; // Z 위치 저장
        return data; // 변환 데이터 반환
    }

    public Vector3 ToVector3() // 저장 데이터를 Vector3로 변환
    {
        return new Vector3(x, y, z); // Unity 위치값 반환
    }
}

[Serializable] // JSON 직렬화 허용
public sealed class SaveQuaternionData // Quaternion 저장용 데이터
{
    [Tooltip("X 회전값.")]
    public float x; // X 회전값
    [Tooltip("Y 회전값.")]
    public float y; // Y 회전값
    [Tooltip("Z 회전값.")]
    public float z; // Z 회전값
    [Tooltip("W 회전값.")]
    public float w = 1f; // W 회전값

    public static SaveQuaternionData FromQuaternion(Quaternion value) // Quaternion 저장 데이터 변환
    {
        SaveQuaternionData data = new SaveQuaternionData(); // 새로운 회전 데이터 생성
        data.x = value.x; // X 회전 저장
        data.y = value.y; // Y 회전 저장
        data.z = value.z; // Z 회전 저장
        data.w = value.w; // W 회전 저장
        return data; // 변환 데이터 반환
    }

    public Quaternion ToQuaternion() // 저장 데이터를 Quaternion으로 변환
    {
        return new Quaternion(x, y, z, w); // Unity 회전값 반환
    }
}
