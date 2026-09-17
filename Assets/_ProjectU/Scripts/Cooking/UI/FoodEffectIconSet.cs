using UnityEngine; // Unity 기본 기능

[CreateAssetMenu(fileName = "FoodEffectIcons", menuName = "Project U/Food Effect Icon Set")] // 생성 메뉴
public sealed class FoodEffectIconSet : ScriptableObject // 85일차: 요리 창·효과 HUD 공통 아이콘
{
    [Tooltip("허기 아이콘.")]
    [SerializeField] private Sprite hunger; // 허기
    [Tooltip("갈증 아이콘.")]
    [SerializeField] private Sprite thirst; // 갈증
    [Tooltip("체력 아이콘.")]
    [SerializeField] private Sprite health; // 체력
    [Tooltip("스태미나 효과 아이콘.")]
    [SerializeField] private Sprite stamina; // 스태미나
    [Tooltip("보온 효과 아이콘.")]
    [SerializeField] private Sprite warmth; // 보온
    [Tooltip("이동 속도 효과 아이콘.")]
    [SerializeField] private Sprite speed; // 이동 속도
    [Tooltip("포만감 효과 아이콘.")]
    [SerializeField] private Sprite satiety; // 포만감
    [Tooltip("조리 시간 아이콘.")]
    [SerializeField] private Sprite time; // 시간
    [Tooltip("조리 시설 아이콘.")]
    [SerializeField] private Sprite flame; // 불꽃

    public Sprite Time => time; // 시간 아이콘 제공
    public Sprite Flame => flame; // 불꽃 아이콘 제공

    public Sprite Get(FoodEffectEntry entry) // 효과 아이콘 조회
    {
        switch (entry.Kind) // 종류 분기
        {
            case FoodEffectKind.Hunger: return hunger; // 허기
            case FoodEffectKind.Thirst: return thirst; // 갈증
            case FoodEffectKind.Health: return health; // 체력
            default: return Get(entry.BuffType); // 보조 효과
        }
    }

    public Sprite Get(FoodBuffType type) // 보조 효과 아이콘 조회
    {
        switch (type) // 종류 분기
        {
            case FoodBuffType.StaminaRecovery: return stamina; // 스태미나
            case FoodBuffType.Warmth: return warmth; // 보온
            case FoodBuffType.MoveSpeed: return speed; // 이동 속도
            case FoodBuffType.Satiety: return satiety; // 포만감
            default: return null; // 없음
        }
    }
}
