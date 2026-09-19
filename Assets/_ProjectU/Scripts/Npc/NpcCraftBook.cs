using System; // Serializable
using System.Collections.Generic; // 목록
using UnityEngine; // Unity 기본 기능

[CreateAssetMenu(fileName = "NpcCraftBook_New", menuName = "Project U/NPC/Craft Book")] // NPC 제작 주문서 생성 메뉴
public sealed class NpcCraftBook : ScriptableObject, INpcWorkplace // 102일차: NPC 제작 주문서 (재료를 가져가면 수수료를 받고 물건을 만들어 줌) / 104일차: 상점 없는 제작 NPC의 작업 장소
{
    [Serializable]
    public sealed class Ingredient // 재료 한 종류
    {
        [Tooltip("재료 아이템.")]
        [SerializeField] private ItemData item; // 아이템
        [Tooltip("필요 수량.")]
        [SerializeField, Min(1)] private int amount = 1; // 수량

        public Ingredient(ItemData item, int amount) // 생성 도구에서 사용
        {
            this.item = item;
            this.amount = amount;
        }

        public ItemData Item => item; // 아이템 제공
        public int Amount => Mathf.Max(1, amount); // 수량 제공
    }

    [Serializable]
    public sealed class Order // 제작 주문 한 가지
    {
        [Tooltip("주문 ID.")]
        [SerializeField] private string orderId = "craft_new"; // ID
        [Tooltip("만들어 주는 아이템.")]
        [SerializeField] private ItemData result; // 결과
        [Tooltip("만들어 주는 수량.")]
        [SerializeField, Min(1)] private int resultAmount = 1; // 결과 수량
        [Tooltip("필요 재료.")]
        [SerializeField] private List<Ingredient> ingredients = new List<Ingredient>(); // 재료
        [Tooltip("수수료 (코인).")]
        [SerializeField, Min(0)] private int fee; // 수수료
        [Tooltip("이 관계 단계부터 주문할 수 있습니다.")]
        [SerializeField] private AffinityStage requiredStage = AffinityStage.Uninterested; // 필요 단계

        public Order(string orderId, ItemData result, int resultAmount, List<Ingredient> ingredients, int fee, AffinityStage requiredStage) // 생성 도구에서 사용
        {
            this.orderId = orderId;
            this.result = result;
            this.resultAmount = resultAmount;
            this.ingredients = ingredients ?? new List<Ingredient>();
            this.fee = fee;
            this.requiredStage = requiredStage;
        }

        public string OrderId => orderId; // ID 제공
        public ItemData Result => result; // 결과 제공
        public int ResultAmount => Mathf.Max(1, resultAmount); // 결과 수량 제공
        public IReadOnlyList<Ingredient> Ingredients => ingredients; // 재료 제공
        public int Fee => Mathf.Max(0, fee); // 수수료 제공
        public AffinityStage RequiredStage => requiredStage; // 필요 단계 제공
    }

    [Tooltip("주문서 주인 캐릭터 ID.")]
    [SerializeField] private string ownerId = "char_new"; // 주인
    [Tooltip("제작 탭 인사 (탭을 열 때 말풍선).")]
    [SerializeField] private string craftLine = string.Empty; // 인사
    [SerializeField] private List<Order> orders = new List<Order>(); // 주문 목록

    [Header("Station")] // 104일차: 상점 없는 제작 NPC (상점이 있으면 상점 영업을 따름)
    [Tooltip("제작 창 제목 (상점 없는 제작 NPC).")]
    [SerializeField] private string stationName = string.Empty; // 작업장 이름
    [Tooltip("주인이 일정상 이 위치에 있을 때 주문을 받습니다 (상점 없는 제작 NPC).")]
    [SerializeField] private string[] openLocationIds = new string[0]; // 작업 위치
    [Tooltip("작업 시간이 끝나 창이 닫힐 때 말풍선.")]
    [SerializeField] private string farewell = string.Empty; // 마감 인사

    public string OwnerId => ownerId; // 주인 제공
    public string CraftLine => craftLine; // 인사 제공
    public IReadOnlyList<Order> Orders => orders; // 주문 제공
    public string StationName => stationName; // 작업장 이름 제공
    public IReadOnlyList<string> OpenLocationIds => openLocationIds; // 작업 위치 제공
    public string Farewell => farewell; // 마감 인사 제공
    public bool HasStation => !string.IsNullOrEmpty(stationName) && openLocationIds != null && openLocationIds.Length > 0; // 상점 없이 주문을 받는지

    public bool IsOpenLocation(string locationId) // 작업 위치 여부
    {
        return !string.IsNullOrEmpty(locationId) && openLocationIds != null && Array.IndexOf(openLocationIds, locationId) >= 0;
    }

#if UNITY_EDITOR
    public void EditorAssign(string owner, string line, List<Order> entries, string station = "", string[] locations = null, string closing = "") // 생성 도구 전용
    {
        ownerId = owner;
        craftLine = line ?? string.Empty;
        orders = entries ?? new List<Order>();
        stationName = station ?? string.Empty;
        openLocationIds = locations ?? new string[0];
        farewell = closing ?? string.Empty;
    }
#endif
}
