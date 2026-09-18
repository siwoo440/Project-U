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
    [Tooltip("적 스폰 지점 저장 데이터 존재 여부.")]
    public bool hasEnemySpawnData; // 적 스폰 지점 저장 데이터 존재 여부
    [Tooltip("전체 적 스폰 지점 저장 데이터.")]
    public EnemySpawnSaveData enemySpawns = new EnemySpawnSaveData(); // 전체 적 스폰 지점 저장 데이터
    [Tooltip("밭 저장 데이터 존재 여부.")]
    public bool hasFarmData; // 밭 저장 데이터 존재 여부
    [Tooltip("밭 칸별 상태와 물뿌리개 데이터.")]
    public FarmSaveData farm = new FarmSaveData(); // 밭 저장 데이터
    [Tooltip("낚시 기록 저장 데이터 존재 여부.")]
    public bool hasFishingData; // 낚시 기록 저장 데이터 존재 여부
    [Tooltip("잡은 물고기 기록 데이터.")]
    public FishingSaveData fishing = new FishingSaveData(); // 낚시 기록 데이터
    [Tooltip("음식 보조 효과 저장 데이터 존재 여부.")]
    public bool hasFoodBuffData; // 음식 효과 저장 여부
    [Tooltip("적용 중인 음식 보조 효과 목록.")]
    public List<FoodBuffSaveData> foodBuffs = new List<FoodBuffSaveData>(); // 음식 효과 목록
    [Tooltip("가축 저장 데이터 존재 여부.")]
    public bool hasLivestockData; // 가축 저장 여부
    [Tooltip("우리별 가축 상태.")]
    public LivestockSaveData livestock = new LivestockSaveData(); // 가축 데이터
    [Tooltip("코인·상점 저장 데이터 존재 여부. (87일차)")]
    public bool hasMarketData; // 상점 저장 여부
    [Tooltip("코인·오늘 상인 재고·판매 기록.")]
    public MarketSaveData market = new MarketSaveData(); // 상점 데이터
    [Tooltip("NPC 관계 저장 데이터 존재 여부. (91일차)")]
    public bool hasNpcData; // NPC 저장 여부
    [Tooltip("NPC별 만남 · 호감도 · 하루 대화·선물 기록.")]
    public NpcSaveData npc = new NpcSaveData(); // NPC 관계
    [Tooltip("NPC 상점 저장 데이터 존재 여부. (92일차)")]
    public bool hasNpcShopData; // NPC 상점 저장 여부
    [Tooltip("NPC 상점별 오늘 재고 · 매입 기록 · 거래 합계.")]
    public NpcShopSaveData npcShops = new NpcShopSaveData(); // NPC 상점
    [Tooltip("NPC 의뢰 저장 데이터 존재 여부. (93일차)")]
    public bool hasNpcQuestData; // NPC 의뢰 저장 여부
    [Tooltip("게시판 · 진행 중 의뢰 · 완료 기록.")]
    public NpcQuestSaveData npcQuests = new NpcQuestSaveData(); // NPC 의뢰

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
    [Tooltip("85일차 조리 칸 저장 여부. false이면 위의 한 칸 상태를 사용합니다.")]
    public bool hasSlotState; // 조리 칸 저장 여부
    [Tooltip("조리 칸 목록.")]
    public List<CookingSlotSaveData> slots = new List<CookingSlotSaveData>(); // 조리 칸 목록
}

[Serializable] // JSON 직렬화 허용
public sealed class CookingSlotSaveData // 85일차: 조리 칸 저장 데이터
{
    [Tooltip("요리법 ID. 비어 있으면 빈 칸입니다.")]
    public string recipeId = string.Empty; // 요리법 ID
    [Tooltip("묶음 수.")]
    public int batchCount; // 묶음 수
    [Tooltip("남은 조리 시간.")]
    public float remainingSeconds; // 남은 시간
    [Tooltip("꺼내기를 기다리는 완성 음식 수량.")]
    public int readyAmount; // 완성 수량
}

[Serializable] // JSON 직렬화 허용
public sealed class LivestockSaveData // 86일차: 가축 전체 저장 데이터
{
    [Tooltip("우리 목록.")]
    public List<AnimalPenSaveData> pens = new List<AnimalPenSaveData>(); // 우리 목록
}

[Serializable] // JSON 직렬화 허용
public sealed class AnimalPenSaveData // 86일차: 우리 하나의 저장 데이터
{
    [Tooltip("우리 건축물 ID.")]
    public string structureId = string.Empty; // 건축물 ID
    [Tooltip("마지막으로 하루 처리를 마친 날짜.")]
    public int lastProcessedDay; // 마지막 처리 날짜
    [Tooltip("우리 안 동물 목록.")]
    public List<PenAnimalSaveData> animals = new List<PenAnimalSaveData>(); // 동물 목록
}

[Serializable] // JSON 직렬화 허용
public sealed class PenAnimalSaveData // 86일차: 동물 한 마리의 저장 데이터
{
    [Tooltip("동물 종류 ID.")]
    public string animalId = string.Empty; // 동물 ID
    [Tooltip("개체 번호.")]
    public int nameNumber; // 번호
    [Tooltip("기분 0~100.")]
    public int mood; // 기분
    [Tooltip("오늘 먹이를 먹었는지.")]
    public bool fedToday; // 먹음
    [Tooltip("오늘 쓰다듬었는지.")]
    public bool pettedToday; // 쓰다듬음
    [Tooltip("마지막 생산 후 먹은 날 수.")]
    public int daysSinceProduct; // 생산 경과
    [Tooltip("꺼내기를 기다리는 생산물 수.")]
    public int productReady; // 생산물
}

[Serializable] // JSON 직렬화 허용
public sealed class MarketSaveData // 87일차: 코인과 상점 저장 데이터
{
    [Tooltip("플레이어 코인.")]
    public int coins; // 코인
    [Tooltip("마지막으로 판매 상자를 처리한 날짜.")]
    public int lastProcessedDay; // 처리 날짜
    [Tooltip("상인 재고를 만든 날짜.")]
    public int stockDay; // 재고 날짜
    [Tooltip("오늘 상인 재고의 남은 수량.")]
    public List<MarketStockSaveData> stock = new List<MarketStockSaveData>(); // 재고
    [Tooltip("지금까지 판매로 받은 코인 합계.")]
    public int totalCoinsEarned; // 누적 수입
    [Tooltip("지금까지 판매한 아이템 수 합계.")]
    public int totalItemsSold; // 누적 판매 수
}

[Serializable] // JSON 직렬화 허용
public sealed class MarketStockSaveData // 87일차: 상인 재고 한 칸
{
    [Tooltip("아이템 ID.")]
    public string itemId = string.Empty; // 아이템 ID
    [Tooltip("남은 수량.")]
    public int remaining; // 남은 수량
}

[Serializable] // JSON 직렬화 허용
public sealed class FoodBuffSaveData // 85일차: 음식 보조 효과 저장 데이터
{
    [Tooltip("효과 종류.")]
    public int buffType; // 효과 종류
    [Tooltip("효과 세기 (%).")]
    public float strength; // 세기
    [Tooltip("남은 시간.")]
    public float remainingSeconds; // 남은 시간
    [Tooltip("전체 시간.")]
    public float durationSeconds; // 전체 시간
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
public sealed class EnemySpawnSaveData // 전체 적 스폰 지점 저장 데이터
{
    [Tooltip("적 스폰 지점 목록.")]
    public List<EnemySpawnPointSaveData> spawnPoints = new List<EnemySpawnPointSaveData>(); // 적 스폰 지점 목록
}

[Serializable] // JSON 직렬화 허용
public sealed class EnemySpawnPointSaveData // 단일 적 스폰 지점 저장 데이터
{
    [Tooltip("적 스폰 지점 고유 ID.")]
    public string spawnPointId = string.Empty; // 적 스폰 지점 고유 ID
    [Tooltip("적이 처치되어 비어 있는 상태.")]
    public bool isDefeated; // 적 처치 상태
    [Tooltip("다시 생성되기까지 남은 시간. 재생성하지 않는 지점은 -1.")]
    public float respawnRemainingSeconds; // 재생성까지 남은 시간
}

[Serializable] // JSON 직렬화 허용
public sealed class FarmSaveData // 밭 전체 저장 데이터
{
    [Tooltip("물뿌리개에 남은 물 사용 횟수.")]
    public int wateringCanWater; // 물뿌리개 남은 물
    [Tooltip("밭 칸별 상태 목록.")]
    public List<FarmPlotSaveData> plots = new List<FarmPlotSaveData>(); // 밭 칸 목록
}

[Serializable] // JSON 직렬화 허용
public sealed class FarmPlotSaveData // 밭 한 칸 저장 데이터
{
    [Tooltip("밭 건축물 고유 ID.")]
    public string structureId = string.Empty; // 밭 건축물 ID
    [Tooltip("작물 상태 (0 빈 밭, 1 성장, 2 수확 가능, 3 시듦).")]
    public int state; // 작물 상태
    [Tooltip("심은 작물 ID. 빈 밭은 빈 문자열.")]
    public string cropId = string.Empty; // 작물 ID
    [Tooltip("작물을 심은 날짜.")]
    public int plantedDay; // 심은 날짜
    [Tooltip("물을 받아 성장한 누적 일수.")]
    public int grownDays; // 누적 성장 일수
    [Tooltip("마지막으로 물을 받은 날짜. -1은 받은 적 없음.")]
    public int lastWateredDay = -1; // 물 받은 날짜
    [Tooltip("성장 계산을 마지막으로 처리한 날짜.")]
    public int lastGrowthDay; // 성장 처리 날짜
    [Tooltip("폭풍 피해 판정을 마지막으로 한 날짜. 81일차 이전 저장 파일은 0.")]
    public int lastStormCheckDay; // 폭풍 판정 날짜
}

[Serializable] // JSON 직렬화 허용
public sealed class FishingSaveData // 낚시 기록 전체 저장 데이터
{
    [Tooltip("물고기 종류별 잡은 기록 목록.")]
    public List<FishCatchSaveData> catches = new List<FishCatchSaveData>(); // 잡은 기록 목록
}

[Serializable] // JSON 직렬화 허용
public sealed class FishCatchSaveData // 물고기 한 종류의 잡은 기록
{
    [Tooltip("물고기 고유 ID.")]
    public string fishId = string.Empty; // 물고기 ID
    [Tooltip("잡은 횟수.")]
    public int caughtCount; // 잡은 횟수
    [Tooltip("처음 잡은 날짜.")]
    public int firstCaughtDay; // 처음 잡은 날짜
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
