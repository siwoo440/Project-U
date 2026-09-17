using System; // 배열 기능
using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // UI 기능

// 83일차: 끌어올리기 미니게임 HUD
// [물고기 이름 ........ 희귀도]
// [====== 목표 구간 ==|== ] ← 왕복하는 표시
// [남은 시간 막대]
// [성공 ●●○○        실수 ○○○]
[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class FishingMinigameUI : MonoBehaviour
{
    [Header("References")] // 참조 묶음
    [Tooltip("미니게임 중에만 켜지는 패널입니다.")]
    [SerializeField] private GameObject panelRoot; // 패널

    [Tooltip("물고기 이름입니다. 처음 보는 물고기는 ???로 표시합니다.")]
    [SerializeField] private TMP_Text titleText; // 이름

    [Tooltip("물고기 희귀도입니다.")]
    [SerializeField] private TMP_Text rarityText; // 희귀도

    [Tooltip("목표 구간입니다. 가로 Anchor로 위치와 폭을 정합니다.")]
    [SerializeField] private RectTransform zoneRect; // 목표 구간

    [SerializeField] private Image zoneImage; // 목표 구간 색상

    [Tooltip("왕복하는 표시입니다.")]
    [SerializeField] private RectTransform markerRect; // 표시

    [SerializeField] private Image markerImage; // 표시 색상

    [Tooltip("남은 시간 막대입니다. 가로 Anchor로 길이를 정합니다.")]
    [SerializeField] private RectTransform timeFillRect; // 시간 막대

    [SerializeField] private Image timeFillImage; // 시간 막대 색상

    [Tooltip("성공 횟수 점입니다. 필요한 수만큼만 켭니다.")]
    [SerializeField] private Image[] successPips = Array.Empty<Image>(); // 성공 점

    [Tooltip("실수 횟수 점입니다. 최대 실수 수만큼만 켭니다.")]
    [SerializeField] private Image[] missPips = Array.Empty<Image>(); // 실수 점

    [Header("Colors")] // 색상 묶음
    [SerializeField] private Color zoneColor = new Color(0.31f, 0.76f, 0.69f, 0.85f); // 목표 기본
    [SerializeField] private Color hitFlashColor = new Color(0.6f, 1f, 0.65f, 1f); // 성공 반짝임
    [SerializeField] private Color missFlashColor = new Color(0.88f, 0.34f, 0.31f, 1f); // 실수 반짝임
    [SerializeField] private Color markerColor = new Color(0.955f, 0.935f, 0.885f, 1f); // 표시 기본
    [SerializeField] private Color markerInZoneColor = new Color(1f, 0.85f, 0.35f, 1f); // 목표 안 표시
    [SerializeField] private Color pipEmptyColor = new Color(1f, 1f, 1f, 0.16f); // 빈 점
    [SerializeField] private Color pipSuccessColor = new Color(0.31f, 0.76f, 0.69f, 1f); // 성공 점
    [SerializeField] private Color pipMissColor = new Color(0.88f, 0.34f, 0.31f, 1f); // 실수 점
    [SerializeField] private Color timeColor = new Color(0.95f, 0.72f, 0.3f, 1f); // 시간 기본
    [SerializeField] private Color timeLowColor = new Color(0.88f, 0.34f, 0.31f, 1f); // 시간 부족

    [Tooltip("판정 후 목표 구간이 반짝이는 시간(초)입니다.")]
    [SerializeField, Min(0.01f)] private float flashDuration = 0.25f; // 반짝임 시간

    private FishingMinigameController source; // 표시 중인 미니게임
    private int shownSuccess = -1; // 마지막 성공 표시
    private int shownMiss = -1; // 마지막 실수 표시

    public bool IsShowing => source != null; // 표시 중 여부 제공

    private void Awake() // 시작 시 숨김
    {
        if (source == null && panelRoot != null) // 미표시 확인
        {
            panelRoot.SetActive(false); // 패널 숨김
        }
    }

    public void Show(FishingMinigameController controller) // 미니게임 표시
    {
        if (controller == null || controller.Fish == null) // 요청 확인
        {
            return; // 표시 생략
        }

        source = controller; // 대상 저장
        FishData fish = controller.Fish; // 물고기

        if (titleText != null) // 이름 확인
        {
            titleText.SetText(controller.IsKnownFish ? fish.DisplayName : "???"); // 처음 보는 물고기는 이름 숨김
        }

        if (rarityText != null) // 희귀도 확인
        {
            rarityText.SetText(FishRarityUtility.GetLabel(fish.Rarity)); // 희귀도 이름
            rarityText.color = FishRarityUtility.GetColor(fish.Rarity); // 희귀도 색상
        }

        SetPipCount(successPips, controller.RequiredSuccessCount); // 필요한 성공 점 수
        SetPipCount(missPips, controller.MaxMisses); // 최대 실수 점 수
        shownSuccess = -1; // 점 갱신 요청
        shownMiss = -1; // 점 갱신 요청

        if (panelRoot != null) // 패널 확인
        {
            panelRoot.SetActive(true); // 표시
        }

        Refresh(); // 첫 화면 갱신
    }

    public void Hide() // 미니게임 숨김
    {
        source = null; // 대상 해제

        if (panelRoot != null && panelRoot.activeSelf) // 패널 확인
        {
            panelRoot.SetActive(false); // 숨김
        }
    }

    private void LateUpdate() // 매 프레임 화면 갱신
    {
        if (source == null) // 표시 확인
        {
            return; // 처리 생략
        }

        if (!source.IsActive) // 종료 확인
        {
            Hide(); // 숨김
            return; // 처리 종료
        }

        Refresh(); // 갱신
    }

    private void Refresh() // 표시·목표·시간·점 갱신
    {
        float half = source.ZoneWidth * 0.5f; // 목표 절반 폭

        if (zoneRect != null) // 목표 확인
        {
            zoneRect.anchorMin = new Vector2(source.ZoneCenter - half, 0f); // 왼쪽 끝
            zoneRect.anchorMax = new Vector2(source.ZoneCenter + half, 1f); // 오른쪽 끝
        }

        if (zoneImage != null) // 목표 색상 확인
        {
            float flash = source.TimeSinceLastResult / flashDuration; // 반짝임 진행
            Color flashColor = source.LastResultWasHit ? hitFlashColor : missFlashColor; // 반짝임 색상
            zoneImage.color = flash < 1f ? Color.Lerp(flashColor, zoneColor, flash) : zoneColor; // 색상 적용
        }

        if (markerRect != null) // 표시 확인
        {
            Vector2 anchor = new Vector2(source.MarkerPosition, 0.5f); // 표시 위치
            markerRect.anchorMin = anchor; // 위치 적용
            markerRect.anchorMax = anchor; // 위치 적용
        }

        if (markerImage != null) // 표시 색상 확인
        {
            markerImage.color = source.IsMarkerInZone ? markerInZoneColor : markerColor; // 목표 안이면 금색
        }

        float remaining = source.TimeRemaining01; // 남은 시간

        if (timeFillRect != null) // 시간 막대 확인
        {
            timeFillRect.anchorMax = new Vector2(remaining, 1f); // 길이 적용
        }

        if (timeFillImage != null) // 시간 색상 확인
        {
            timeFillImage.color = Color.Lerp(timeLowColor, timeColor, Mathf.Clamp01(remaining * 3f)); // 얼마 안 남으면 빨강
        }

        if (source.SuccessCount != shownSuccess) // 성공 변경 확인
        {
            shownSuccess = source.SuccessCount; // 기록
            PaintPips(successPips, shownSuccess, pipSuccessColor); // 점 칠하기
        }

        if (source.MissCount != shownMiss) // 실수 변경 확인
        {
            shownMiss = source.MissCount; // 기록
            PaintPips(missPips, shownMiss, pipMissColor); // 점 칠하기
        }
    }

    private static void SetPipCount(Image[] pips, int count) // 사용할 점 수만큼 켜기
    {
        for (int index = 0; index < pips.Length; index++) // 점 순회
        {
            if (pips[index] != null) // 점 확인
            {
                pips[index].gameObject.SetActive(index < count); // 표시 여부
            }
        }
    }

    private void PaintPips(Image[] pips, int filled, Color filledColor) // 채운 점 색칠
    {
        for (int index = 0; index < pips.Length; index++) // 점 순회
        {
            if (pips[index] != null) // 점 확인
            {
                pips[index].color = index < filled ? filledColor : pipEmptyColor; // 색상 적용
            }
        }
    }
}
