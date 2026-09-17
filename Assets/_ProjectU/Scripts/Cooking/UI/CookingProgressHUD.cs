using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class CookingProgressHUD : MonoBehaviour // 85일차: 가까운 모닥불 위 조리 진행 고리
{
    [Tooltip("기준 플레이어. 비어 있으면 PlayerInventory를 찾습니다.")]
    [SerializeField] private Transform player; // 플레이어
    [Tooltip("팝업 관리자 (창이 열리면 숨김).")]
    [SerializeField] private GameUIManager gameUIManager; // 팝업 관리자
    [Tooltip("움직이는 표시 루트.")]
    [SerializeField] private RectTransform markerRoot; // 표시 루트
    [Tooltip("진행 고리 (Radial Filled).")]
    [SerializeField] private Image ringFill; // 고리
    [Tooltip("가운데 불꽃·체크 아이콘.")]
    [SerializeField] private Image centerIcon; // 아이콘
    [Tooltip("아래 문구.")]
    [SerializeField] private TMP_Text label; // 문구
    [Tooltip("조리 중 아이콘.")]
    [SerializeField] private Sprite cookingSprite; // 조리 중 아이콘
    [Tooltip("완성 아이콘.")]
    [SerializeField] private Sprite readySprite; // 완성 아이콘
    [Tooltip("표시 거리.")]
    [SerializeField, Min(1f)] private float showDistance = 9f; // 표시 거리
    [Tooltip("모닥불 위 높이.")]
    [SerializeField] private float heightOffset = 1.35f; // 높이

    private RectTransform canvasRect; // Canvas 영역
    private Canvas canvas; // Canvas

    public CampfireCookingStation TrackedStation { get; private set; } // 표시 중인 모닥불

    private void Awake() // 참조 준비
    {
        canvas = GetComponentInParent<Canvas>(); // Canvas
        canvasRect = canvas != null ? (RectTransform)canvas.rootCanvas.transform : null; // Canvas 영역

        if (markerRoot != null) // 표시 확인
        {
            markerRoot.gameObject.SetActive(false); // 숨김
        }
    }

    private void LateUpdate() // 위치와 진행도 갱신
    {
        if (markerRoot == null || canvasRect == null) // 참조 확인
        {
            return; // 생략
        }

        ResolvePlayer(); // 플레이어 확인
        Camera view = Camera.main; // 카메라
        TrackedStation = FindNearestBusyStation(); // 가까운 모닥불
        bool popupOpen = gameUIManager != null && gameUIManager.HasOpenPopup; // 팝업 확인

        if (TrackedStation == null || view == null || popupOpen) // 표시 조건
        {
            SetVisible(false); // 숨김
            return; // 완료
        }

        Vector3 world = TrackedStation.transform.position + Vector3.up * heightOffset; // 표시 위치
        Vector3 screen = view.WorldToScreenPoint(world); // 화면 위치

        if (screen.z <= 0f) // 카메라 뒤
        {
            SetVisible(false); // 숨김
            return; // 완료
        }

        Camera uiCamera = canvas.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.rootCanvas.worldCamera; // UI 카메라
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, uiCamera, out Vector2 local); // Canvas 좌표
        RectTransform parent = (RectTransform)markerRoot.parent; // 부모
        Vector3 worldPoint = canvasRect.TransformPoint(local); // 월드 좌표
        markerRoot.localPosition = parent.InverseTransformPoint(worldPoint); // 부모 기준 위치
        SetVisible(true); // 표시

        int ready = TrackedStation.ReadyCount; // 완성 칸
        CookingSlot soonest = TrackedStation.FindSoonestCooking(); // 가장 먼저 끝나는 칸

        if (ready > 0) // 완성
        {
            ringFill.fillAmount = 1f; // 가득
            ringFill.color = ProjectUUIPalette.Teal; // 청록
            centerIcon.sprite = readySprite; // 체크
            centerIcon.color = ProjectUUIPalette.Teal; // 청록
            label.SetText(ready > 1 ? $"{ready} READY" : "READY"); // 문구
            label.color = ProjectUUIPalette.Teal; // 색
            return; // 완료
        }

        ringFill.fillAmount = soonest != null ? soonest.Progress01 : 0f; // 진행도
        ringFill.color = ProjectUUIPalette.Accent; // 주황
        centerIcon.sprite = cookingSprite; // 불꽃
        centerIcon.color = ProjectUUIPalette.Accent; // 주황
        label.SetText(soonest != null ? FoodBuffUtility.FormatTime(soonest.RemainingSeconds) : string.Empty); // 남은 시간
        label.color = ProjectUUIPalette.TextPrimary; // 색
    }

    private void SetVisible(bool visible) // 표시 전환
    {
        if (markerRoot.gameObject.activeSelf != visible) // 변경 확인
        {
            markerRoot.gameObject.SetActive(visible); // 적용
        }
    }

    private void ResolvePlayer() // 플레이어 검색
    {
        if (player == null) // 참조 확인
        {
            PlayerInventory inventory = FindFirstObjectByType<PlayerInventory>(); // 검색
            player = inventory != null ? inventory.transform : null; // 적용
        }
    }

    private CampfireCookingStation FindNearestBusyStation() // 가까운 조리 중·완성 모닥불
    {
        if (player == null) // 플레이어 확인
        {
            return null; // 없음
        }

        CampfireCookingStation best = null; // 결과
        float bestDistance = showDistance * showDistance; // 최대 거리

        foreach (CampfireCookingStation station in CampfireCookingStation.ActiveStations) // 순회
        {
            if (station == null || (!station.IsCooking && !station.HasReadyResult)) // 상태 확인
            {
                continue; // 제외
            }

            float distance = (station.transform.position - player.position).sqrMagnitude; // 거리

            if (distance < bestDistance) // 비교
            {
                bestDistance = distance; // 갱신
                best = station; // 갱신
            }
        }

        return best; // 결과 반환
    }
}
