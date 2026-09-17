using System.Collections.Generic; // 목록 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class AnimalStatusHUD : MonoBehaviour // 86일차: 가까운 동물 머리 위 상태 표시 (배고픔 · 생산물)
{
    [Tooltip("팝업 관리자 (창이 열리면 숨김).")]
    [SerializeField] private GameUIManager gameUIManager; // 팝업 관리자
    [Tooltip("기준 플레이어. 비어 있으면 PlayerInventory를 찾습니다.")]
    [SerializeField] private Transform player; // 플레이어
    [Tooltip("표시 템플릿 (배경 + 아이콘).")]
    [SerializeField] private RectTransform markerTemplate; // 템플릿
    [Tooltip("배고픔 아이콘.")]
    [SerializeField] private Sprite hungrySprite; // 배고픔
    [Tooltip("표시 거리.")]
    [SerializeField, Min(1f)] private float showDistance = 12f; // 표시 거리
    [Tooltip("동물 머리 위 높이 (동물 크기에 곱함).")]
    [SerializeField] private float heightPadding = 0.35f; // 높이 여유
    [Tooltip("최대 표시 수.")]
    [SerializeField, Range(1, 16)] private int maxMarkers = 10; // 최대 수

    private readonly List<RectTransform> markers = new List<RectTransform>(); // 표시 목록
    private readonly List<Image> markerIcons = new List<Image>(); // 아이콘 목록
    private readonly List<Image> markerBacks = new List<Image>(); // 배경 목록
    private readonly Dictionary<Transform, float> heights = new Dictionary<Transform, float>(); // 동물 높이
    private RectTransform canvasRect; // Canvas 영역
    private Canvas rootCanvas; // Canvas

    public int VisibleCount { get; private set; } // 표시 중인 수

    private void Awake() // 준비
    {
        Canvas canvas = GetComponentInParent<Canvas>(); // Canvas
        rootCanvas = canvas != null ? canvas.rootCanvas : null; // 최상위
        canvasRect = rootCanvas != null ? (RectTransform)rootCanvas.transform : null; // 영역

        if (markerTemplate != null) // 템플릿 숨김
        {
            markerTemplate.gameObject.SetActive(false); // 숨김
        }
    }

    private void LateUpdate() // 위치 갱신
    {
        VisibleCount = 0; // 초기화
        Camera view = Camera.main; // 카메라
        bool popupOpen = gameUIManager != null && gameUIManager.HasOpenPopup; // 팝업 확인

        if (player == null) // 플레이어 확인
        {
            PlayerInventory inventory = FindFirstObjectByType<PlayerInventory>(); // 검색
            player = inventory != null ? inventory.transform : null; // 적용
        }

        if (view != null && !popupOpen && player != null && canvasRect != null && markerTemplate != null) // 표시 조건
        {
            float maxDistance = showDistance * showDistance; // 거리 제곱

            foreach (AnimalPen pen in LivestockManager.ActivePens) // 우리 순회
            {
                if (pen == null || (pen.transform.position - player.position).sqrMagnitude > maxDistance) // 거리 확인
                {
                    continue; // 제외
                }

                for (int index = 0; index < pen.Animals.Count && index < pen.AnimalVisuals.Count; index++) // 동물 순회
                {
                    PenAnimal animal = pen.Animals[index]; // 상태
                    FarmAnimal visual = pen.AnimalVisuals[index]; // 외형

                    if (visual == null || VisibleCount >= maxMarkers) // 확인
                    {
                        continue; // 제외
                    }

                    Sprite sprite = null; // 아이콘
                    Color color = Color.white; // 색

                    if (animal.HasProduct) // 생산물 우선
                    {
                        sprite = pen.AcceptedAnimal.ProductItem.Icon; // 생산물 아이콘
                        color = Color.white; // 원래 색
                    }
                    else if (!animal.FedToday) // 배고픔
                    {
                        sprite = hungrySprite; // 배고픔 아이콘
                        color = ProjectUUIPalette.Danger; // 빨강
                    }

                    if (sprite == null) // 표시할 상태 없음
                    {
                        continue; // 제외
                    }

                    Vector3 world = visual.transform.position + Vector3.up * GetHeight(visual.transform); // 머리 위
                    Vector3 screen = view.WorldToScreenPoint(world); // 화면 위치

                    if (screen.z <= 0f) // 카메라 뒤
                    {
                        continue; // 제외
                    }

                    Place(VisibleCount, screen, sprite, color, animal.HasProduct); // 표시
                    VisibleCount++; // 증가
                }
            }
        }

        for (int index = VisibleCount; index < markers.Count; index++) // 남는 표시
        {
            if (markers[index].gameObject.activeSelf) markers[index].gameObject.SetActive(false); // 숨김
        }
    }

    private void Place(int index, Vector3 screen, Sprite sprite, Color color, bool product) // 표시 배치
    {
        while (markers.Count <= index) // 부족한 표시
        {
            RectTransform created = Instantiate(markerTemplate, markerTemplate.parent); // 복제
            created.name = $"Marker_{markers.Count:00}"; // 이름
            markers.Add(created); // 추가
            markerBacks.Add(created.GetComponent<Image>()); // 배경
            markerIcons.Add(created.Find("LP_Icon").GetComponent<Image>()); // 아이콘
        }

        RectTransform marker = markers[index]; // 표시
        Camera uiCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera; // UI 카메라
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, uiCamera, out Vector2 local); // Canvas 좌표
        Vector3 worldPoint = canvasRect.TransformPoint(local); // 월드 좌표
        marker.localPosition = ((RectTransform)marker.parent).InverseTransformPoint(worldPoint); // 부모 기준
        float bob = Mathf.Sin(Time.unscaledTime * 3f + index) * 3f; // 둥실
        marker.localPosition += new Vector3(0f, bob, 0f); // 적용
        markerIcons[index].sprite = sprite; // 아이콘
        markerIcons[index].color = color; // 색
        markerBacks[index].color = product ? new Color(0.95f, 0.72f, 0.3f, 0.9f) : new Color(0.05f, 0.06f, 0.08f, 0.8f); // 배경 색

        if (!marker.gameObject.activeSelf) // 표시
        {
            marker.gameObject.SetActive(true); // 켜기
        }
    }

    private float GetHeight(Transform animal) // 동물 모델 높이 (한 번 계산)
    {
        if (heights.TryGetValue(animal, out float height)) // 기록 확인
        {
            return height; // 결과
        }

        Renderer renderer = animal.GetComponentInChildren<Renderer>(); // 렌더러
        height = (renderer != null ? renderer.bounds.max.y - animal.position.y : 0.5f) + heightPadding; // 높이
        heights[animal] = height; // 기록

        if (heights.Count > 64) // 오래된 기록 정리
        {
            heights.Clear(); // 초기화
        }

        return height; // 결과 반환
    }
}
