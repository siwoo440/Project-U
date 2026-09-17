using UnityEngine; // Unity 기본 기능

[CreateAssetMenu(fileName = "AnimalData_New", menuName = "Project U/Animal Data")] // 동물 데이터 생성 메뉴
public sealed class AnimalData : ScriptableObject // 86일차: 가축 한 종류의 먹이·생산·기분 규칙
{
    [Header("Identity")] // 식별 정보
    [Tooltip("동물 고유 ID.")]
    [SerializeField] private string animalId = "animal_new"; // 동물 ID
    [Tooltip("동물 표시 이름 (단수).")]
    [SerializeField] private string displayName = "ANIMAL"; // 표시 이름
    [Tooltip("여러 마리일 때 이름 (복수).")]
    [SerializeField] private string pluralName = "ANIMALS"; // 복수 이름
    [Tooltip("개체 이름 앞부분 (예: HEN → HEN 1).")]
    [SerializeField] private string individualPrefix = "ANIMAL"; // 개체 이름

    [Header("Visual")] // 외형
    [Tooltip("우리 안에 만들 저폴리 모델 Prefab.")]
    [SerializeField] private GameObject modelPrefab; // 모델
    [Tooltip("요리 창·HUD에 쓰는 동물 아이콘.")]
    [SerializeField] private Sprite icon; // 아이콘
    [Tooltip("돌아다니는 속도 (m/s).")]
    [SerializeField, Min(0.05f)] private float walkSpeed = 0.5f; // 걷기 속도

    [Header("Feed")] // 먹이
    [Tooltip("먹이 아이템.")]
    [SerializeField] private ItemData feedItem; // 먹이
    [Tooltip("하루에 필요한 먹이 수량.")]
    [SerializeField, Min(1)] private int feedPerDay = 1; // 하루 먹이
    [Tooltip("우리로 새 동물을 불러올 때 쓰는 먹이 수량.")]
    [SerializeField, Min(1)] private int attractFeedCost = 4; // 불러오기 비용

    [Header("Product")] // 생산물
    [Tooltip("생산물 아이템.")]
    [SerializeField] private ItemData productItem; // 생산물
    [Tooltip("한 번에 생산하는 수량.")]
    [SerializeField, Min(1)] private int productAmount = 1; // 생산 수량
    [Tooltip("생산 주기 (먹이를 먹은 날 기준).")]
    [SerializeField, Min(1)] private int productIntervalDays = 1; // 생산 주기
    [Tooltip("꺼내지 않고 쌓아 둘 수 있는 최대 생산물 수.")]
    [SerializeField, Min(1)] private int maxStoredProduct = 3; // 최대 보관
    [Tooltip("기분이 이 값 이상이면 생산물을 하나 더 얻을 수 있습니다.")]
    [SerializeField, Range(0, 100)] private int bonusMood = 80; // 추가 생산 기분
    [Tooltip("추가 생산 확률.")]
    [SerializeField, Range(0f, 1f)] private float bonusChance = 0.35f; // 추가 생산 확률

    [Header("Mood")] // 기분
    [Tooltip("새로 들어온 동물의 기분.")]
    [SerializeField, Range(0, 100)] private int startMood = 60; // 시작 기분
    [Tooltip("이 기분 미만이면 생산하지 않습니다.")]
    [SerializeField, Range(0, 100)] private int minimumProduceMood = 30; // 생산 최소 기분
    [Tooltip("먹이를 먹은 날이 지나면 오르는 기분.")]
    [SerializeField, Range(0, 100)] private int fedMoodGain = 10; // 먹은 날 기분
    [Tooltip("먹이를 먹지 못한 날이 지나면 내려가는 기분.")]
    [SerializeField, Range(0, 100)] private int hungryMoodLoss = 25; // 굶은 날 기분
    [Tooltip("쓰다듬으면 오르는 기분 (하루 한 번).")]
    [SerializeField, Range(0, 100)] private int petMoodGain = 8; // 쓰다듬기 기분

    public string AnimalId => animalId; // ID 제공
    public string DisplayName => displayName; // 이름 제공
    public string PluralName => pluralName; // 복수 이름 제공
    public string IndividualPrefix => individualPrefix; // 개체 이름 제공
    public GameObject ModelPrefab => modelPrefab; // 모델 제공
    public Sprite Icon => icon; // 아이콘 제공
    public float WalkSpeed => walkSpeed; // 걷기 속도 제공
    public ItemData FeedItem => feedItem; // 먹이 제공
    public int FeedPerDay => Mathf.Max(1, feedPerDay); // 하루 먹이 제공
    public int AttractFeedCost => Mathf.Max(1, attractFeedCost); // 불러오기 비용 제공
    public ItemData ProductItem => productItem; // 생산물 제공
    public int ProductAmount => Mathf.Max(1, productAmount); // 생산 수량 제공
    public int ProductIntervalDays => Mathf.Max(1, productIntervalDays); // 생산 주기 제공
    public int MaxStoredProduct => Mathf.Max(ProductAmount, maxStoredProduct); // 최대 보관 제공
    public int BonusMood => bonusMood; // 추가 생산 기분 제공
    public float BonusChance => bonusChance; // 추가 생산 확률 제공
    public int StartMood => startMood; // 시작 기분 제공
    public int MinimumProduceMood => minimumProduceMood; // 생산 최소 기분 제공
    public int FedMoodGain => fedMoodGain; // 먹은 날 기분 제공
    public int HungryMoodLoss => hungryMoodLoss; // 굶은 날 기분 제공
    public int PetMoodGain => petMoodGain; // 쓰다듬기 기분 제공

    private void OnValidate() // Inspector 값 보정
    {
        animalId = string.IsNullOrWhiteSpace(animalId) ? string.Empty : animalId.Trim(); // ID 공백 제거
        displayName = string.IsNullOrWhiteSpace(displayName) ? string.Empty : displayName.Trim(); // 이름 공백 제거
        maxStoredProduct = Mathf.Max(productAmount, maxStoredProduct); // 최대 보관 보정
    }
}
