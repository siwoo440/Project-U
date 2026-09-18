using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class CoinHUD : MonoBehaviour // 87일차: 생존 게이지 아래 코인 알약 + 아침 판매 결과 알림
{
    [Header("Coins")] // 코인
    [Tooltip("코인 알약.")]
    [SerializeField] private CookingChipUI coinChip; // 코인 알약
    [Tooltip("코인 아이콘.")]
    [SerializeField] private Sprite coinSprite; // 코인 아이콘
    [Tooltip("코인이 바뀔 때 떠오르는 +/- 문구.")]
    [SerializeField] private TMP_Text deltaText; // 변화량 문구
    [Tooltip("숫자가 올라가는 시간.")]
    [SerializeField, Min(0.05f)] private float countDuration = 0.8f; // 올라가는 시간
    [Tooltip("변화량 문구 표시 시간.")]
    [SerializeField, Min(0.2f)] private float deltaDuration = 1.6f; // 변화량 시간

    [Header("Sale Banner")] // 판매 알림
    [Tooltip("아침 판매 결과 알림 전체.")]
    [SerializeField] private RectTransform banner; // 알림
    [SerializeField] private TMP_Text bannerTitle; // 제목
    [SerializeField] private TMP_Text bannerDetail; // 내용
    [SerializeField] private TMP_Text bannerCoins; // 금액
    [Tooltip("알림 표시 시간.")]
    [SerializeField, Min(1f)] private float bannerDuration = 6f; // 알림 시간
    [Tooltip("잠자는 동안에는 알림을 미룹니다.")]
    [SerializeField] private SleepSystem sleepSystem; // 수면

    private PlayerWallet wallet; // 지갑
    private MarketManager market; // 상점
    private float shownCoins; // 표시 중인 코인
    private int targetCoins; // 실제 코인
    private float countFrom; // 올라가기 시작 값
    private float countStart = -1f; // 올라가기 시작 시각
    private float deltaStart = -1f; // 변화량 시작 시각
    private float bannerStart = -1f; // 알림 시작 시각
    private bool bannerPending; // 알림 대기
    private CanvasGroup bannerGroup; // 알림 투명도
    private Vector2 deltaBasePosition; // 변화량 기본 위치
    private int lastDrawnCoins = int.MinValue; // 마지막 표시 값

    public int DisplayedCoins => Mathf.RoundToInt(shownCoins); // 표시 코인 제공 (테스트용)
    public bool BannerVisible => banner != null && banner.gameObject.activeSelf; // 알림 표시 제공 (테스트용)
    public string BannerText => bannerTitle != null ? $"{bannerTitle.text} | {bannerDetail.text} | {bannerCoins.text}" : string.Empty; // 알림 문구 제공 (테스트용)

    private void Awake() // 준비
    {
        if (deltaText != null) // 변화량 확인
        {
            deltaBasePosition = deltaText.rectTransform.anchoredPosition; // 기본 위치
            deltaText.gameObject.SetActive(false); // 숨김
        }

        if (banner != null) // 알림 확인
        {
            bannerGroup = banner.GetComponent<CanvasGroup>(); // 투명도

            if (bannerGroup == null) // 없음
            {
                bannerGroup = banner.gameObject.AddComponent<CanvasGroup>(); // 추가
            }

            bannerGroup.blocksRaycasts = false; // 입력 통과
            bannerGroup.interactable = false; // 조작 없음
            bannerBaseY = banner.anchoredPosition.y; // 기본 높이
            banner.gameObject.SetActive(false); // 숨김
        }

        if (sleepSystem == null) // 수면 확인
        {
            sleepSystem = FindFirstObjectByType<SleepSystem>(); // 검색
        }
    }

    private void OnDisable() // 구독 해제
    {
        Detach(); // 해제
    }

    private void Update() // 연결 · 연출
    {
        Attach(); // 지갑·상점 연결
        bool hasWallet = wallet != null; // 지갑 여부

        if (coinChip != null && coinChip.gameObject.activeSelf != hasWallet) // 지갑 없으면 숨김
        {
            coinChip.gameObject.SetActive(hasWallet); // 적용
        }

        if (!hasWallet) // 지갑 없음
        {
            return; // 생략
        }

        UpdateCount(); // 숫자
        UpdateDelta(); // 변화량
        UpdateBanner(); // 알림
    }

    private void Attach() // 지갑·상점 연결 (늦게 생겨도 연결)
    {
        PlayerWallet currentWallet = PlayerWallet.Local; // 현재 지갑

        if (currentWallet != wallet) // 바뀜
        {
            if (wallet != null) wallet.CoinsChanged -= HandleCoins; // 이전 해제
            wallet = currentWallet; // 적용

            if (wallet != null) // 새 지갑
            {
                wallet.CoinsChanged += HandleCoins; // 구독
                shownCoins = targetCoins = wallet.Coins; // 바로 표시
                countStart = -1f; // 연출 없음
                Draw(); // 그리기
            }
        }

        MarketManager currentMarket = MarketManager.Instance; // 현재 상점

        if (currentMarket != market) // 바뀜
        {
            if (market != null) market.SaleCompleted -= HandleSale; // 이전 해제
            market = currentMarket; // 적용
            if (market != null) market.SaleCompleted += HandleSale; // 구독
        }
    }

    private void Detach() // 구독 해제
    {
        if (wallet != null) wallet.CoinsChanged -= HandleCoins; // 지갑
        if (market != null) market.SaleCompleted -= HandleSale; // 상점
        wallet = null; // 해제
        market = null; // 해제
    }

    private void HandleCoins(int coins, int delta) // 코인 변경
    {
        targetCoins = coins; // 목표

        if (delta == 0) // 불러오기 : 연출 없이 바로
        {
            shownCoins = coins; // 적용
            countStart = -1f; // 연출 없음
            Draw(); // 그리기
            return; // 완료
        }

        countFrom = shownCoins; // 시작 값
        countStart = Time.unscaledTime; // 시작 시각

        if (deltaText != null) // 변화량 문구
        {
            deltaText.SetText(delta > 0 ? $"+{delta:N0}" : $"{delta:N0}"); // 문구
            deltaText.color = delta > 0 ? ProjectUUIPalette.Accent : ProjectUUIPalette.Danger; // 색
            deltaText.gameObject.SetActive(true); // 표시
            deltaStart = Time.unscaledTime; // 시작
        }
    }

    private void HandleSale(ShippingSaleReport report) // 판매 결과
    {
        if (banner == null || report == null) // 확인
        {
            return; // 생략
        }

        string unsold = report.UnsoldItems > 0 ? $"  ·  {report.UnsoldItems} NOT SOLD" : string.Empty; // 남은 물건
        bannerTitle.SetText(report.HasSales ? $"SHIPPING BIN  ·  SOLD OVERNIGHT{unsold}" : "SHIPPING BIN  ·  NOTHING SOLD"); // 제목

        if (report.HasSales) // 판매함
        {
            ShippingSaleLine top = report.Lines[0]; // 가장 비싼 줄
            bannerDetail.SetText($"{report.ItemsSold} ITEMS  ·  BEST: {top.Quantity} {top.Item.DisplayName} +{top.Coins}"); // 내용
            bannerCoins.SetText($"+{report.Coins:N0}"); // 금액
            bannerCoins.color = ProjectUUIPalette.Accent; // 색
        }
        else // 팔 물건 없음
        {
            bannerDetail.SetText($"{report.UnsoldItems} ITEMS CAN'T BE SOLD - TAKE THEM BACK"); // 내용
            bannerCoins.SetText("+0"); // 금액
            bannerCoins.color = ProjectUUIPalette.TextSecondary; // 색
        }

        bannerPending = true; // 표시 대기 (잠에서 깬 뒤)
    }

    private void UpdateCount() // 숫자 올라가기
    {
        if (countStart >= 0f) // 연출 중
        {
            float t = Mathf.Clamp01((Time.unscaledTime - countStart) / countDuration); // 진행
            float eased = 1f - (1f - t) * (1f - t); // 감속
            shownCoins = Mathf.Lerp(countFrom, targetCoins, eased); // 값
            float pulse = 1f + Mathf.Sin(t * Mathf.PI) * 0.12f; // 커졌다 작아짐
            coinChip.transform.localScale = new Vector3(pulse, pulse, 1f); // 적용

            if (t >= 1f) // 끝
            {
                shownCoins = targetCoins; // 확정
                countStart = -1f; // 종료
                coinChip.transform.localScale = Vector3.one; // 원래 크기
            }
        }

        Draw(); // 그리기
    }

    private void Draw() // 코인 알약 그리기
    {
        int value = Mathf.RoundToInt(shownCoins); // 표시 값

        if (coinChip == null || value == lastDrawnCoins) // 변경 없음
        {
            return; // 생략
        }

        lastDrawnCoins = value; // 기록
        coinChip.Bind($"{value:N0}", ProjectUUIPalette.Accent, coinSprite); // 적용
    }

    private void UpdateDelta() // 변화량 떠오르기
    {
        if (deltaText == null || deltaStart < 0f) // 확인
        {
            return; // 생략
        }

        float t = (Time.unscaledTime - deltaStart) / deltaDuration; // 진행

        if (t >= 1f) // 끝
        {
            deltaText.gameObject.SetActive(false); // 숨김
            deltaStart = -1f; // 종료
            return; // 완료
        }

        deltaText.rectTransform.anchoredPosition = deltaBasePosition + new Vector2(0f, t * 18f); // 위로
        Color color = deltaText.color; // 색
        color.a = t < 0.7f ? 1f : 1f - (t - 0.7f) / 0.3f; // 사라짐
        deltaText.color = color; // 적용
    }

    private void UpdateBanner() // 판매 알림
    {
        if (banner == null) // 확인
        {
            return; // 생략
        }

        if (bannerPending && (sleepSystem == null || !sleepSystem.IsSleeping)) // 깨어 있을 때 표시
        {
            bannerPending = false; // 대기 해제
            bannerStart = Time.unscaledTime; // 시작
            banner.gameObject.SetActive(true); // 표시
        }

        if (bannerStart < 0f) // 표시 안 함
        {
            return; // 생략
        }

        float elapsed = Time.unscaledTime - bannerStart; // 경과

        if (elapsed >= bannerDuration) // 끝
        {
            banner.gameObject.SetActive(false); // 숨김
            bannerStart = -1f; // 종료
            return; // 완료
        }

        float fadeIn = Mathf.Clamp01(elapsed / 0.35f); // 나타남
        float fadeOut = Mathf.Clamp01((bannerDuration - elapsed) / 0.6f); // 사라짐
        bannerGroup.alpha = Mathf.Min(fadeIn, fadeOut); // 투명도
        float slide = (1f - fadeIn) * 16f; // 내려오기
        banner.localScale = Vector3.one; // 크기
        banner.anchoredPosition = new Vector2(banner.anchoredPosition.x, BannerBaseY + slide); // 위치
    }

    private float bannerBaseY = float.NaN; // 알림 기본 높이

    private float BannerBaseY // 알림 기본 높이 제공
    {
        get
        {
            if (float.IsNaN(bannerBaseY) && banner != null) // 처음
            {
                bannerBaseY = banner.anchoredPosition.y; // 기록
            }

            return bannerBaseY; // 결과 반환
        }
    }

    public void DebugShowBanner(ShippingSaleReport report) // 테스트용 알림 표시
    {
        HandleSale(report); // 표시
    }
}
