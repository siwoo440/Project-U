using TMPro; // TextMeshPro UI 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // RawImage와 Button 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class WorldMapPanelView : MonoBehaviour // M과 Alt+N에서 공통으로 사용하는 전체 지도 패널
{
    [Header("Panel")] // 전체 지도 화면 참조 묶음
    [Tooltip("전체 지도 배경과 지도 UI를 표시하거나 숨길 자식 Panel 루트입니다.")]
    [SerializeField] private GameObject panelRoot; // 전체 지도 실제 표시 루트

    [Tooltip("MinimapCameraController의 RenderTexture를 전체 화면으로 표시할 RawImage입니다.")]
    [SerializeField] private RawImage mapImage; // 전체 지도 영상

    [Tooltip("현재 플레이어 위치 중앙에 표시할 방향 화살표 RectTransform입니다.")]
    [SerializeField] private RectTransform playerDirectionIcon; // 전체 지도 플레이어 방향 아이콘

    [Tooltip("현재 플레이어의 X와 Z 좌표를 표시할 선택 Text입니다.")]
    [SerializeField] private TMP_Text coordinateText; // 현재 플레이어 좌표 Text

    [Tooltip("전체 지도 패널을 닫는 버튼입니다.")]
    [SerializeField] private Button closeButton; // 전체 지도 닫기 버튼

    [Header("Direction Icon")] // 방향 아이콘 설정 묶음
    [Tooltip("방향 화살표 Sprite의 기본 방향을 보정할 Z 회전값입니다.")]
    [SerializeField] private float directionRotationOffset; // 방향 아이콘 회전 보정값

    private WorldMapController controller; // 전체 지도 입력과 상태 관리자
    private Transform playerTarget; // 좌표와 방향을 표시할 플레이어 Transform
    private bool internalReferencesValid; // 프리팹 내부 참조 상태
    private bool listenerRegistered; // 닫기 버튼 이벤트 등록 여부

    public bool IsVisible =>
        panelRoot != null
        && panelRoot.activeSelf; // 전체 지도 실제 표시 여부 제공

    private void Awake() // 전체 지도 프리팹 내부 참조 초기화
    {
        internalReferencesValid =
            panelRoot != null
            && mapImage != null
            && playerDirectionIcon != null
            && closeButton != null; // 필수 프리팹 내부 참조 상태 계산

        if (!internalReferencesValid) // 내부 참조 누락 확인
        {
            Debug.LogError(
                $"{gameObject.name}의 WorldMapPanelView 필수 참조를 모두 연결해야 합니다.",
                this); // 전체 지도 프리팹 오류 출력

            enabled = false; // 잘못된 전체 지도 기능 비활성화
            return; // 초기화 중단
        }

        RegisterCloseButton(); // 닫기 버튼 이벤트 등록
        HideImmediate(); // 생성 직후 전체 지도 화면 숨김
    }

    public bool Initialize(
        WorldMapController owner,
        RenderTexture mapTexture,
        Transform target) // 전체 지도 관리자와 런타임 참조 연결
    {
        if (!internalReferencesValid
            || owner == null
            || mapTexture == null
            || target == null) // 내부 참조와 런타임 참조 확인
        {
            return false; // 전체 지도 초기화 실패 반환
        }

        controller = owner; // 전체 지도 관리자 저장
        playerTarget = target; // 플레이어 Transform 저장
        mapImage.texture = mapTexture; // 지도 RenderTexture 연결
        RefreshPlayerInformation(); // 플레이어 방향과 좌표 즉시 표시
        return true; // 전체 지도 초기화 성공 반환
    }

    public void Show() // 전체 지도 화면 표시
    {
        if (!internalReferencesValid) // 내부 참조 상태 확인
        {
            return; // 전체 지도 표시 중단
        }

        panelRoot.SetActive(true); // 전체 지도 화면 활성화
        RefreshPlayerInformation(); // 현재 플레이어 정보 갱신
        closeButton.Select(); // 키보드와 게임패드 기본 선택 버튼 지정
    }

    public void HideImmediate() // 전체 지도 화면 즉시 숨김
    {
        if (panelRoot == null) // 전체 지도 화면 루트 확인
        {
            return; // 화면 숨김 생략
        }

        panelRoot.SetActive(false); // 전체 지도 화면 비활성화
    }

    private void LateUpdate() // 플레이어 이동 이후 지도 정보 갱신
    {
        if (!internalReferencesValid || !IsVisible) // 초기화와 표시 상태 확인
        {
            return; // 지도 정보 갱신 생략
        }

        RefreshPlayerInformation(); // 현재 플레이어 방향과 좌표 표시
    }

    private void RefreshPlayerInformation() // 플레이어 방향 아이콘과 좌표 갱신
    {
        if (playerTarget == null) // 플레이어 Transform 존재 확인
        {
            return; // 플레이어 정보 갱신 생략
        }

        float playerYaw = playerTarget.eulerAngles.y; // 플레이어 월드 Y 회전값 조회
        float iconRotation = -playerYaw + directionRotationOffset; // 북쪽 고정 지도 기준 UI 회전 계산

        playerDirectionIcon.localRotation =
            Quaternion.Euler(0f, 0f, iconRotation); // 플레이어 방향 화살표 회전 적용

        if (coordinateText != null) // 선택 좌표 Text 연결 여부 확인
        {
            Vector3 playerPosition = playerTarget.position; // 현재 플레이어 월드 위치 조회
            coordinateText.SetText(
                $"X {playerPosition.x:0.0}   Z {playerPosition.z:0.0}"); // 현재 X와 Z 좌표 표시
        }
    }

    private void RegisterCloseButton() // 닫기 버튼 이벤트 등록
    {
        if (listenerRegistered || closeButton == null) // 기존 등록과 버튼 참조 확인
        {
            return; // 중복 등록 방지
        }

        closeButton.onClick.AddListener(
            OnCloseButtonClicked); // 전체 지도 닫기 이벤트 등록

        listenerRegistered = true; // 버튼 이벤트 등록 완료 기록
    }

    private void RemoveCloseButton() // 닫기 버튼 이벤트 제거
    {
        if (!listenerRegistered || closeButton == null) // 이벤트 등록과 버튼 참조 확인
        {
            return; // 제거할 이벤트 없음
        }

        closeButton.onClick.RemoveListener(
            OnCloseButtonClicked); // 전체 지도 닫기 이벤트 제거

        listenerRegistered = false; // 버튼 이벤트 등록 상태 초기화
    }

    private void OnCloseButtonClicked() // CLOSE 버튼 클릭 처리
    {
        if (controller == null) // 전체 지도 관리자 존재 확인
        {
            Debug.LogError(
                "WorldMapPanelView에 WorldMapController가 연결되지 않았습니다.",
                this); // 전체 지도 관리자 누락 오류 출력

            return; // 닫기 처리 중단
        }

        controller.CloseFullMap(); // 전체 지도 패널 닫기
    }

    private void OnDestroy() // 전체 지도 프리팹 제거 정리
    {
        RemoveCloseButton(); // 닫기 버튼 이벤트 제거
    }
}
