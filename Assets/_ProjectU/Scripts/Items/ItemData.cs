using UnityEngine; // Unity 기본 기능

[CreateAssetMenu(fileName = "ItemData_New", menuName = "Project U/Item Data")] // 아이템 데이터 생성 메뉴
public sealed class ItemData : ScriptableObject // 아이템 공통 데이터
{
    [Header("Identity")] // 식별 정보 묶음
    [Tooltip("아이템 고유 ID.")]
    [SerializeField] private string itemId = "item_new"; // 아이템 고유 ID

    [Header("Display")] // 화면 표시 정보 묶음
    [Tooltip("아이템 표시 이름.")]
    [SerializeField] private string displayName = "NEW ITEM"; // 아이템 표시 이름

    [Tooltip("아이템 설명.")]
    [TextArea(2, 4)] // 여러 줄 설명 입력
    [SerializeField] private string description = "NO DESCRIPTION"; // 아이템 설명

    [Tooltip("아이템 아이콘.")]
    [SerializeField] private Sprite icon; // 아이템 아이콘

    [Header("Category")] // 아이템 분류 묶음
    [Tooltip("아이템 기본 분류.")]
    [SerializeField] private ItemCategory itemCategory = ItemCategory.CraftingMaterial; // 아이템 기본 분류

    [Tooltip("도구 종류.")]
    [SerializeField] private ToolType toolType = ToolType.None; // 도구 종류

    [Header("Weapon Attack")] // 무기 공통 공격 능력치 묶음
    [Tooltip("아이템의 공격 방식입니다. 도구 또는 무기 분류에서 사용합니다.")]
    [SerializeField] private WeaponAttackType weaponAttackType = WeaponAttackType.None; // 공격 방식

    [Tooltip("공격 한 번의 기본 피해량입니다.")]
    [SerializeField, Min(0f)] private float baseDamage = 10f; // 기본 피해량

    [Tooltip("근접 연속 공격 데이터가 없거나 원거리 공격일 때 사용할 기본 공격 간격입니다.")]
    [SerializeField, Min(0.05f)] private float attackCooldown = 0.6f; // 기본 공격 재사용 대기시간

    [Tooltip("Player 공격 시작 위치에서 적용할 기본 근접 공격 거리입니다.")]
    [SerializeField, Min(0.1f)] private float attackRange = 2f; // 기본 공격 거리

    [Tooltip("근접 공격 SphereCast의 기본 반지름입니다.")]
    [SerializeField, Min(0.01f)] private float attackRadius = 0.4f; // 기본 공격 반지름

    [Tooltip("공격 한 번에 소비할 기본 스태미나입니다.")]
    [SerializeField, Min(0f)] private float staminaCost = 5f; // 기본 공격 스태미나 비용

    [Tooltip("향후 넉백 계산에 사용할 기본 충격량입니다.")]
    [SerializeField, Min(0f)] private float impactForce = 2f; // 기본 충격량

    [Tooltip("근접 공격의 준비·타격·복귀 시간과 단계별 배율을 정의한 데이터입니다.")]
    [SerializeField] private MeleeComboData meleeComboData; // 근접 연속 공격 데이터

    [Header("Equipment")] // 장비 설정 묶음
    [Tooltip("장착 슬롯 종류.")]
    [SerializeField] private EquipmentSlotType equipmentSlotType = EquipmentSlotType.None; // 장착 슬롯 종류

    [Header("Equipment Stats")] // 장비 능력치 묶음
    [Tooltip("피해 감소 비율.")]
    [SerializeField, Range(0f, 80f)] private float defensePercent; // 피해 감소 비율

    [Tooltip("최대 체력 증가량.")]
    [SerializeField] private float maximumHealthBonus; // 최대 체력 증가량

    [Tooltip("이동 속도 증가 비율.")]
    [SerializeField] private float movementSpeedBonusPercent; // 이동 속도 증가 비율

    [Tooltip("허기 감소 방지 비율.")]
    [SerializeField, Range(0f, 80f)] private float hungerDepletionReductionPercent; // 허기 감소 방지 비율

    [Tooltip("갈증 감소 방지 비율.")]
    [SerializeField, Range(0f, 80f)] private float thirstDepletionReductionPercent; // 갈증 감소 방지 비율

    [Tooltip("추위 감소 방지 비율.")]
    [SerializeField, Range(0f, 80f)] private float coldResistancePercent; // 추위 감소 방지 비율

    [Tooltip("인벤토리 추가 슬롯.")]
    [SerializeField] private int inventorySlotBonus; // 인벤토리 추가 슬롯

    [Header("Food")] // 음식 효과 묶음
    [Tooltip("허기 회복량.")]
    [SerializeField] private float hungerRestoreAmount; // 허기 회복량

    [Header("Drink")] // 음료 효과 묶음
    [Tooltip("갈증 회복량.")]
    [SerializeField] private float thirstRestoreAmount; // 갈증 회복량

    [Header("Medicine")] // 의약품 효과 묶음
    [Tooltip("체력 회복량.")]
    [SerializeField] private float healthRestoreAmount; // 체력 회복량

    [Header("Stack")] // 중첩 정보 묶음
    [Tooltip("최대 중첩 수량.")]
    [SerializeField] private int maximumStack = 20; // 최대 중첩 수량

    public string ItemId => itemId; // 아이템 ID 제공
    public string DisplayName => displayName; // 표시 이름 제공
    public string Description => description; // 아이템 설명 제공
    public Sprite Icon => icon; // 아이템 아이콘 제공
    public int MaximumStack => Mathf.Max(1, maximumStack); // 최대 중첩 수량 제공
    public ItemCategory ItemCategory => itemCategory; // 아이템 분류 제공
    public ToolType ToolType => toolType; // 도구 종류 제공
    public WeaponAttackType WeaponAttackType => CanAttack ? weaponAttackType : WeaponAttackType.None; // 공격 방식 제공
    public float BaseDamage => CanAttack ? Mathf.Max(0f, baseDamage) : 0f; // 기본 피해량 제공
    public float AttackCooldown => CanAttack ? Mathf.Max(0.05f, attackCooldown) : 0f; // 기본 공격 간격 제공
    public float AttackRange => CanAttack ? Mathf.Max(0.1f, attackRange) : 0f; // 기본 공격 거리 제공
    public float AttackRadius => CanAttack ? Mathf.Max(0.01f, attackRadius) : 0f; // 기본 공격 반지름 제공
    public float StaminaCost => CanAttack ? Mathf.Max(0f, staminaCost) : 0f; // 기본 스태미나 비용 제공
    public float ImpactForce => CanAttack ? Mathf.Max(0f, impactForce) : 0f; // 기본 충격량 제공
    public MeleeComboData MeleeComboData => CanAttack && weaponAttackType == WeaponAttackType.Melee
        ? meleeComboData
        : null; // 근접 연속 공격 데이터 제공
    public EquipmentSlotType EquipmentSlotType => IsEquipment ? equipmentSlotType : EquipmentSlotType.None; // 장비 슬롯 종류 제공
    public float DefensePercent => IsEquipment ? Mathf.Clamp(defensePercent, 0f, 80f) : 0f; // 방어력 제공
    public float MaximumHealthBonus => IsEquipment ? Mathf.Max(0f, maximumHealthBonus) : 0f; // 최대 체력 증가량 제공
    public float MovementSpeedBonusPercent => IsEquipment ? Mathf.Max(0f, movementSpeedBonusPercent) : 0f; // 이동 속도 증가량 제공
    public float HungerDepletionReductionPercent => IsEquipment ? Mathf.Clamp(hungerDepletionReductionPercent, 0f, 80f) : 0f; // 허기 감소 방지량 제공
    public float ThirstDepletionReductionPercent => IsEquipment ? Mathf.Clamp(thirstDepletionReductionPercent, 0f, 80f) : 0f; // 갈증 감소 방지량 제공
    public float ColdResistancePercent => IsEquipment ? Mathf.Clamp(coldResistancePercent, 0f, 80f) : 0f; // 장비 방한 능력치 제공
    public int InventorySlotBonus => IsEquipment && equipmentSlotType == EquipmentSlotType.Backpack ? Mathf.Max(0, inventorySlotBonus) : 0; // 가방 슬롯 증가량 제공
    public float HungerRestoreAmount => IsFood ? Mathf.Max(0f, hungerRestoreAmount) : 0f; // 음식 허기 회복량 제공
    public float ThirstRestoreAmount => IsDrink ? Mathf.Max(0f, thirstRestoreAmount) : 0f; // 음료 갈증 회복량 제공
    public float HealthRestoreAmount => IsMedicine ? Mathf.Max(0f, healthRestoreAmount) : 0f; // 의약품 체력 회복량 제공
    public bool IsCraftingMaterial => itemCategory == ItemCategory.CraftingMaterial; // 제작 재료 여부 제공
    public bool IsTool => itemCategory == ItemCategory.Tool; // 도구 여부 제공
    public bool IsFood => itemCategory == ItemCategory.Food; // 음식 여부 제공
    public bool IsDrink => itemCategory == ItemCategory.Drink; // 음료 여부 제공
    public bool IsMedicine => itemCategory == ItemCategory.Medicine; // 의약품 여부 제공
    public bool IsEquipment => itemCategory == ItemCategory.Equipment; // 장비 여부 제공
    public bool IsWeapon => itemCategory == ItemCategory.Weapon; // 전투 무기 여부 제공
    public bool SupportsCombat => IsTool || IsWeapon; // 공격 능력치를 사용할 수 있는 분류 여부 제공
    public bool CanAttack => SupportsCombat
        && weaponAttackType != WeaponAttackType.None
        && baseDamage > 0f; // 실제 공격 가능한 아이템 여부 제공

    private void OnValidate() // Inspector 값 검증
    {
        itemId = string.IsNullOrWhiteSpace(itemId) ? string.Empty : itemId.Trim(); // ID 양쪽 공백 제거
        displayName = string.IsNullOrWhiteSpace(displayName) ? string.Empty : displayName.Trim(); // 이름 양쪽 공백 제거
        description = string.IsNullOrWhiteSpace(description) ? string.Empty : description.Trim(); // 설명 양쪽 공백 제거
        maximumStack = Mathf.Max(1, maximumStack); // 최대 중첩 최소값 적용

        bool requiresSingleStack =
            itemCategory == ItemCategory.Tool
            || itemCategory == ItemCategory.Equipment
            || itemCategory == ItemCategory.Weapon; // 단일 보관 분류 확인

        if (requiresSingleStack) // 도구, 장비 또는 무기 확인
        {
            maximumStack = 1; // 최대 중첩 1개 적용
        }

        if (itemCategory != ItemCategory.Tool) // 도구가 아닌 분류 확인
        {
            toolType = ToolType.None; // 도구 종류 제거
        }

        if (!SupportsCombat) // 공격 능력치를 사용하지 않는 분류 확인
        {
            weaponAttackType = WeaponAttackType.None; // 공격 방식 제거
            baseDamage = 0f; // 피해량 제거
            attackCooldown = 0.6f; // 기본 공격 간격 복구
            attackRange = 2f; // 기본 공격 거리 복구
            attackRadius = 0.4f; // 기본 공격 반지름 복구
            staminaCost = 0f; // 스태미나 비용 제거
            impactForce = 0f; // 충격량 제거
            meleeComboData = null; // 근접 연속 공격 데이터 제거
        }
        else if (weaponAttackType != WeaponAttackType.None) // 공격 가능한 도구 또는 무기 확인
        {
            baseDamage = Mathf.Max(0f, baseDamage); // 피해량 음수 방지
            attackCooldown = Mathf.Max(0.05f, attackCooldown); // 공격 간격 최소값 적용
            attackRange = Mathf.Max(0.1f, attackRange); // 공격 거리 최소값 적용
            attackRadius = Mathf.Max(0.01f, attackRadius); // 공격 반지름 최소값 적용
            staminaCost = Mathf.Max(0f, staminaCost); // 스태미나 비용 음수 방지
            impactForce = Mathf.Max(0f, impactForce); // 충격량 음수 방지

            if (weaponAttackType != WeaponAttackType.Melee) // 근접 공격이 아닌지 확인
            {
                meleeComboData = null; // 근접 연속 공격 데이터 제거
            }
        }

        if (itemCategory != ItemCategory.Equipment) // 장비가 아닌 분류 확인
        {
            equipmentSlotType = EquipmentSlotType.None; // 장비 슬롯 종류 제거
            defensePercent = 0f; // 방어력 제거
            maximumHealthBonus = 0f; // 체력 증가량 제거
            movementSpeedBonusPercent = 0f; // 이동 속도 증가량 제거
            hungerDepletionReductionPercent = 0f; // 허기 감소 방지량 제거
            thirstDepletionReductionPercent = 0f; // 갈증 감소 방지량 제거
            coldResistancePercent = 0f; // 방한 능력치 제거
            inventorySlotBonus = 0; // 인벤토리 증가량 제거
        }
        else // 장비 분류 확인
        {
            defensePercent = Mathf.Clamp(defensePercent, 0f, 80f); // 방어력 범위 제한
            maximumHealthBonus = Mathf.Max(0f, maximumHealthBonus); // 체력 증가량 음수 방지
            movementSpeedBonusPercent = Mathf.Max(0f, movementSpeedBonusPercent); // 이동 속도 증가량 음수 방지
            hungerDepletionReductionPercent = Mathf.Clamp(hungerDepletionReductionPercent, 0f, 80f); // 허기 감소 방지 범위 제한
            thirstDepletionReductionPercent = Mathf.Clamp(thirstDepletionReductionPercent, 0f, 80f); // 갈증 감소 방지 범위 제한
            coldResistancePercent = Mathf.Clamp(coldResistancePercent, 0f, 80f); // 방한 능력치 범위 제한

            if (equipmentSlotType != EquipmentSlotType.Backpack) // 가방 외 장비 확인
            {
                inventorySlotBonus = 0; // 인벤토리 증가 제거
            }
            else // 가방 장비 확인
            {
                inventorySlotBonus = Mathf.Max(0, inventorySlotBonus); // 인벤토리 증가량 음수 방지
            }
        }

        if (itemCategory != ItemCategory.Food) // 음식이 아닌 분류 확인
        {
            hungerRestoreAmount = 0f; // 음식 회복량 제거
        }
        else // 음식 분류 확인
        {
            hungerRestoreAmount = Mathf.Max(0f, hungerRestoreAmount); // 회복량 음수 방지
        }

        if (itemCategory != ItemCategory.Drink) // 음료가 아닌 분류 확인
        {
            thirstRestoreAmount = 0f; // 음료 회복량 제거
        }
        else // 음료 분류 확인
        {
            thirstRestoreAmount = Mathf.Max(0f, thirstRestoreAmount); // 회복량 음수 방지
        }

        if (itemCategory != ItemCategory.Medicine) // 의약품이 아닌 분류 확인
        {
            healthRestoreAmount = 0f; // 의약품 회복량 제거
        }
        else // 의약품 분류 확인
        {
            healthRestoreAmount = Mathf.Max(0f, healthRestoreAmount); // 회복량 음수 방지
        }
    }
}
