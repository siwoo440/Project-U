using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
[RequireComponent(typeof(PlacedBuildObject))] // 설치 건축물 정보 요구
public sealed class MarketStall : InteractableBase // 87일차: 떠돌이 상인이 낮 동안 머무는 가판대
{
    [Header("Stall")] // 가판대 설정
    [Tooltip("가판대 표시 이름.")]
    [SerializeField] private string stallDisplayName = "MARKET STALL"; // 표시 이름
    [Tooltip("상인 이름 (창 제목).")]
    [SerializeField] private string merchantName = "TRAVELING MERCHANT"; // 상인 이름

    [Header("Merchant")] // 상인 외형
    [Tooltip("영업 시간에만 보이는 상인 모델.")]
    [SerializeField] private Transform merchant; // 상인
    [Tooltip("이 거리 안에 플레이어가 오면 상인이 플레이어를 바라봅니다.")]
    [SerializeField, Min(0f)] private float lookDistance = 7f; // 바라보기 거리
    [Tooltip("상인 기본 방향 (가판대 기준 Y 회전).")]
    [SerializeField] private float merchantBaseYaw = 180f; // 기본 방향
    [Tooltip("상인이 고개를 돌리는 최대 각도.")]
    [SerializeField, Range(0f, 180f)] private float maxLookAngle = 70f; // 최대 각도

    private GameUIManager gameUIManager; // 팝업 관리자
    private Transform player; // 플레이어
    private float idleTime; // 숨쉬기 시간
    private Vector3 merchantBaseScale = Vector3.one; // 기본 크기

    public string StallDisplayName => stallDisplayName; // 이름 제공
    public string MerchantName => merchantName; // 상인 이름 제공
    public bool IsOpen => MarketManager.Instance != null && MarketManager.Instance.IsShopOpen; // 영업 여부
    public Transform Merchant => merchant; // 상인 제공

    public override string PromptMessage // 안내 문구
    {
        get
        {
            MarketManager manager = MarketManager.Instance; // 관리자

            if (manager == null || manager.Catalog == null) // 상점 없음
            {
                return $"{stallDisplayName} | NO MERCHANT"; // 안내
            }

            return manager.IsShopOpen
                ? $"F - TRADE ({stallDisplayName})"
                : $"{stallDisplayName} | OPENS AT {FormatHour(manager.Catalog.OpenHour)}"; // 결과 반환
        }
    }

    private void Awake() // 준비
    {
        if (merchant != null) // 상인 확인
        {
            merchantBaseScale = merchant.localScale; // 기본 크기
        }

        idleTime = (GetInstanceID() & 1023) * 0.01f; // 가판대마다 다른 박자
    }

    private void Update() // 상인 표시와 바라보기
    {
        if (merchant == null) // 상인 확인
        {
            return; // 생략
        }

        bool open = IsOpen && !NpcManager.HasStallMerchant; // 영업 여부 (90일차: 마을 NPC 리첼이 가판대에 서면 기본 상인 모델은 숨김)

        if (merchant.gameObject.activeSelf != open) // 표시 전환
        {
            merchant.gameObject.SetActive(open); // 적용
        }

        if (!open) // 영업 안 함
        {
            return; // 생략
        }

        idleTime += Time.deltaTime; // 시간
        float breath = 1f + Mathf.Sin(idleTime * 2.1f) * 0.012f; // 숨쉬기
        merchant.localScale = new Vector3(merchantBaseScale.x, merchantBaseScale.y * breath, merchantBaseScale.z); // 적용

        if (player == null) // 플레이어 확인
        {
            PlayerInventory inventory = FindFirstObjectByType<PlayerInventory>(); // 검색
            player = inventory != null ? inventory.transform : null; // 적용
        }

        float targetYaw = merchantBaseYaw; // 기본 방향

        if (player != null) // 플레이어 바라보기
        {
            Vector3 local = transform.InverseTransformPoint(player.position) - merchant.localPosition; // 가판대 기준 방향
            local.y = 0f; // 수평

            if (local.sqrMagnitude <= lookDistance * lookDistance && local.sqrMagnitude > 0.01f) // 거리 확인
            {
                float yaw = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg; // 방향
                float delta = Mathf.Clamp(Mathf.DeltaAngle(merchantBaseYaw, yaw), -maxLookAngle, maxLookAngle); // 제한
                targetYaw = merchantBaseYaw + delta; // 목표
            }
        }

        Quaternion target = Quaternion.Euler(0f, targetYaw, 0f); // 목표 회전
        merchant.localRotation = Quaternion.Slerp(merchant.localRotation, target, 1f - Mathf.Exp(-5f * Time.deltaTime)); // 부드럽게
    }

    public override void Interact(GameObject interactor) // 상호작용
    {
        if (interactor == null) // 확인
        {
            return; // 중단
        }

        MarketManager manager = MarketManager.Instance; // 관리자

        if (manager == null || manager.Catalog == null) // 상점 없음
        {
            return; // 중단
        }

        if (!manager.IsShopOpen) // 영업 시간 아님
        {
            CombatDamagePopup.SpawnText(transform.position + Vector3.up * 2.2f, $"BACK AT {FormatHour(manager.Catalog.OpenHour)}", new Color(0.72f, 0.7f, 0.65f, 1f), 2f); // 알림
            return; // 중단
        }

        if (gameUIManager == null) // 관리자 확인
        {
            gameUIManager = FindFirstObjectByType<GameUIManager>(); // Scene 검색
        }

        if (gameUIManager == null || !gameUIManager.OpenShop(this)) // 상점 창 열기
        {
            CombatDamagePopup.SpawnText(transform.position + Vector3.up * 2.2f, "SHOP WINDOW MISSING", ProjectUUIPalette.Danger, 2f); // 알림
        }
    }

    public static string FormatHour(float hour) // 시각 문구 (06:00)
    {
        int minutes = Mathf.RoundToInt(Mathf.Repeat(hour, 24f) * 60f); // 분
        return $"{minutes / 60 % 24:00}:{minutes % 60:00}"; // 결과 반환
    }
}
