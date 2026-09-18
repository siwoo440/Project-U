using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
[RequireComponent(typeof(StorageContainer))] // 보관함 기능 요구
public sealed class ShippingBin : MonoBehaviour, IStorageInfoProvider // 87일차: 넣어 둔 물건을 자정에 팔아 주는 판매 상자
{
    [Header("Visual")] // 외형
    [Tooltip("물건이 들어 있으면 세우는 깃발 (우체통처럼).")]
    [SerializeField] private Transform flag; // 깃발
    [Tooltip("깃발을 세웠을 때 회전 (로컬).")]
    [SerializeField] private Vector3 flagRaisedEuler = Vector3.zero; // 세운 각도
    [Tooltip("깃발을 눕혔을 때 회전 (로컬).")]
    [SerializeField] private Vector3 flagLoweredEuler = new Vector3(0f, 0f, 90f); // 눕힌 각도
    [Tooltip("깃발 움직이는 속도.")]
    [SerializeField, Min(0.1f)] private float flagSpeed = 6f; // 깃발 속도

    private StorageContainer container; // 보관함
    private bool hasGoods; // 물건 있음
    private float flagBlend; // 깃발 0 눕힘 ~ 1 세움

    public StorageContainer Container // 보관함 제공
    {
        get
        {
            if (container == null) // 참조 확인
            {
                container = GetComponent<StorageContainer>(); // 검색
            }

            return container; // 결과 반환
        }
    }

    public bool HasGoods => hasGoods; // 물건 있음 제공
    public float FlagBlend => flagBlend; // 깃발 상태 제공 (테스트용)

    public string StorageInfo // 보관함 창 안내
    {
        get
        {
            MarketManager manager = MarketManager.Instance; // 관리자

            if (manager == null || manager.Catalog == null) // 상점 없음
            {
                return "NO MERCHANT NEARBY - ITEMS WILL NOT BE SOLD"; // 안내
            }

            manager.EstimateBins(out int coins, out int sellable, out int unsellable); // 예상

            if (sellable == 0 && unsellable == 0) // 빈 상자
            {
                return "PUT ITEMS HERE  ·  THEY SELL AT MIDNIGHT"; // 안내
            }

            string text = sellable > 0 ? $"SELLS AT MIDNIGHT  ·  {sellable} ITEMS  ·  EST. +{coins}" : "NOTHING HERE CAN BE SOLD"; // 기본

            if (unsellable > 0 && sellable > 0) // 팔 수 없는 물건
            {
                text += $"  ·  {unsellable} CAN'T SELL"; // 추가
            }

            return text; // 결과 반환
        }
    }

    public Color StorageInfoColor // 안내 색
    {
        get
        {
            MarketManager manager = MarketManager.Instance; // 관리자

            if (manager == null || manager.Catalog == null) // 상점 없음
            {
                return ProjectUUIPalette.Danger; // 빨강
            }

            manager.EstimateBins(out int coins, out int sellable, out int unsellable); // 예상
            return sellable > 0 ? ProjectUUIPalette.Accent : unsellable > 0 ? ProjectUUIPalette.Danger : ProjectUUIPalette.TextSecondary; // 결과 반환
        }
    }

    public string PromptSuffix // 상호작용 안내 추가 문구
    {
        get
        {
            MarketManager manager = MarketManager.Instance; // 관리자

            if (manager == null || !hasGoods) // 비어 있음
            {
                return "SELLS AT MIDNIGHT"; // 안내
            }

            manager.EstimateBins(out int coins, out int sellable, out _); // 예상
            return sellable > 0 ? $"EST. +{coins} COINS" : "NOTHING TO SELL"; // 결과 반환
        }
    }

    private void Awake() // 참조 준비
    {
        container = GetComponent<StorageContainer>(); // 보관함
    }

    private void OnEnable() // 등록
    {
        MarketManager.RegisterBin(this); // 관리자 등록

        if (Container != null) // 보관함 확인
        {
            Container.StorageChanged += RefreshGoods; // 변경 구독
        }

        RefreshGoods(); // 초기 상태
        flagBlend = hasGoods ? 1f : 0f; // 바로 적용
        ApplyFlag(); // 깃발
    }

    private void OnDisable() // 해제
    {
        MarketManager.UnregisterBin(this); // 관리자 해제

        if (container != null) // 보관함 확인
        {
            container.StorageChanged -= RefreshGoods; // 구독 해제
        }
    }

    private void Update() // 깃발 움직임
    {
        float target = hasGoods ? 1f : 0f; // 목표

        if (Mathf.Approximately(flagBlend, target)) // 도착
        {
            return; // 생략
        }

        flagBlend = Mathf.MoveTowards(flagBlend, target, flagSpeed * Time.deltaTime); // 이동
        ApplyFlag(); // 적용
    }

    private void RefreshGoods() // 물건 여부 갱신
    {
        hasGoods = Container != null && !Container.IsEmpty; // 결과
    }

    private void ApplyFlag() // 깃발 회전
    {
        if (flag != null) // 깃발 확인
        {
            float eased = flagBlend * flagBlend * (3f - 2f * flagBlend); // 부드럽게
            flag.localRotation = Quaternion.Slerp(Quaternion.Euler(flagLoweredEuler), Quaternion.Euler(flagRaisedEuler), eased); // 적용
        }
    }

    public int RemoveSold(MarketCatalogData catalog) // 팔린 (가격표에 있는) 아이템 비우기, 비운 수량 반환
    {
        StorageContainer target = Container; // 보관함

        if (target == null || catalog == null) // 확인
        {
            return 0; // 없음
        }

        int removed = 0; // 결과

        for (int index = 0; index < target.SlotCapacity; index++) // 칸 순회
        {
            InventorySlot slot = target.GetSlot(index); // 칸

            if (slot == null || slot.ItemData == null || !catalog.TryGetPrice(slot.ItemData, out _)) // 팔 수 없는 칸
            {
                continue; // 다음
            }

            removed += slot.Quantity; // 기록
            target.TrySetSlotDirect(index, null); // 비우기
        }

        if (removed > 0) // 변경 알림
        {
            target.NotifyContentsChanged(); // 알림
            CombatDamagePopup.SpawnText(transform.position + Vector3.up * 1.4f, $"SOLD {removed}", new Color(1f, 0.82f, 0.3f, 1f), 2.4f); // 월드 알림
        }

        return removed; // 결과 반환
    }
}
