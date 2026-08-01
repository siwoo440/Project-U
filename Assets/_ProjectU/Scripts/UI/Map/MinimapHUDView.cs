using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // RawImage와 Button 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class MinimapHUDView : MonoBehaviour // 작은 미니맵과 N 키 확장 표시 화면
{
    [Header("UI References")] // 미니맵 UI 참조 묶음
    [Tooltip("미니맵 배경, 지도 이미지와 방향 아이콘을 포함하는 화면 루트입니다.")]
    [SerializeField] private GameObject panelRoot; // 실제 미니맵 표시 루트

    [Tooltip("N 키 크기 전환을 적용할 미니맵 Panel의 RectTransform입니다.")]
    [SerializeField] private RectTransform minimapRect; // 미니맵 크기와 위치 대상

    [Tooltip("MinimapCameraController의 RenderTexture를 표시할 RawImage입니다.")]
    [SerializeField] private RawImage mapImage; // 미니맵 지도 영상

    [Tooltip("플레이어가 바라보는 방향을 표시할 화살표 RectTransform입니다.")]
    [SerializeField] private RectTransform playerDirectionIcon; // 플레이어 방향 아이콘

    [Tooltip("Alt로 커서를 활성화한 뒤 미니맵을 클릭했을 때 전체 지도를 여는 Button입니다.")]
    [SerializeField] private Button minimapClickButton; // 미니맵 클릭 입력 Button

    [Header("Compact Layout")] // 기본 작은 미니맵 배치 묶음
    [Tooltip("기본 작은 미니맵의 가로와 세로 크기입니다.")]
    [SerializeField] private Vector2 compactSize = new Vector2(260f, 260f); // 작은 미니맵 크기

    [Tooltip("기본 작은 미니맵의 Anchored Position입니다.")]
    [SerializeField] private Vector2 compactAnchoredPosition = new Vector2(-30f, -30f); // 작은 미니맵 위치

    [Header("Expanded Layout")] // 확장 미니맵 배치 묶음
    [Tooltip("N 키로 확장한 미니맵의 가로와 세로 크기입니다.")]
    [SerializeField] private Vector2 expandedSize = new Vector2(520f, 520f); // 확장 미니맵 크기

    [Tooltip("N 키로 확장한 미니맵의 Anchored Position입니다.")]
    [SerializeField] private Vector2 expandedAnchoredPosition = new Vector2(-30f, -30f); // 확장 미니맵 위치

    [Header("Direction Icon")] // 방향 아이콘 설정 묶음
    [Tooltip("방향 화살표 Sprite가 기본적으로 위쪽을 바라보지 않을 때 보정할 Z 회전값입니다.")]
    [SerializeField] private float directionRotationOffset; // 방향 아이콘 회전 보정값

    [Header("Start State")] // 미니맵 시작 상태 묶음
    [Tooltip("게임 시작 시 확장 미니맵 상태로 표시할지 설정합니다.")]
    [SerializeField] private bool startExpanded; // 시작 확장 상태 여부

    private WorldMapController worldMapController; // 전체 지도 열기 요청 관리자
    private Transform playerTarget; // 방향을 표시할 플레이어 Transform
    private bool initialized; // 미니맵 UI 초기화 완료 여부
    private bool listenerRegistered; // 미니맵 Button 이벤트 등록 여부
    private bool userVisible = true; // 사용자 설정 미니맵 표시 여부
    private bool suppressedByFullMap; // 전체 지도에 의한 임시 숨김 여부
    private bool isExpanded; // 현재 미니맵 확장 여부

    public bool IsExpanded => isExpanded; // 현재 확장 미니맵 상태 제공
    public bool IsVisible => panelRoot != null && panelRoot.activeSelf; // 실제 미니맵 표시 여부 제공
    public MapCameraViewMode CurrentCameraMode =>
        isExpanded
            ? MapCameraViewMode.Expanded
            : MapCameraViewMode.Compact; // 현재 미니맵에 필요한 Camera 범위 제공

    public bool Initialize(
        WorldMapController owner,
        RenderTexture mapTexture,
        Transform target) // 지도 관리자, 지도 영상과 플레이어 방향 참조 초기화
    {
        if (owner == null
            || panelRoot == null
            || minimapRect == null
            || mapImage == null
            || playerDirectionIcon == null
            || minimapClickButton == null
            || mapTexture == null
            || target == null) // 필수 UI와 런타임 참조 확인
        {
            Debug.LogError(
                $"{gameObject.name}의 MinimapHUDView 필수 참조를 모두 연결해야 합니다.",
                this); // 미니맵 UI 참조 오류 출력

            initialized = false; // 초기화 실패 상태 저장
            return false; // 초기화 실패 반환
        }

        worldMapController = owner; // 전체 지도 관리자 저장
        mapImage.texture = mapTexture; // 지도 RenderTexture를 RawImage에 연결
        playerTarget = target; // 플레이어 방향 추적 대상 저장
        isExpanded = startExpanded; // Inspector 시작 크기 상태 적용
        ConfigureClickButton(); // 미니맵 클릭 Button 이벤트와 Navigation 설정
        ApplyCurrentLayout(); // 시작 미니맵 크기와 위치 적용
        ApplyVisibility(); // 시작 미니맵 표시 상태 적용
        RefreshDirectionIcon(); // 시작 플레이어 방향 적용
        initialized = true; // 초기화 완료 상태 저장
        return true; // 초기화 성공 반환
    }

    public MapCameraViewMode ToggleSize() // N 키로 작은 미니맵과 확장 미니맵 전환
    {
        if (!initialized) // 초기화 상태 확인
        {
            return MapCameraViewMode.Compact; // 기본 카메라 모드 반환
        }

        isExpanded = !isExpanded; // 미니맵 크기 상태 반전
        ApplyCurrentLayout(); // 변경된 미니맵 크기와 위치 적용
        return CurrentCameraMode; // 변경된 지도 카메라 모드 반환
    }

    public void SetUserVisible(bool visible) // 사용자 설정 미니맵 표시 상태 변경
    {
        userVisible = visible; // 사용자 표시 상태 저장
        ApplyVisibility(); // 실제 미니맵 표시 상태 갱신
    }

    public void SetSuppressedByFullMap(bool suppressed) // 전체 지도에 의한 미니맵 임시 숨김
    {
        suppressedByFullMap = suppressed; // 전체 지도 숨김 상태 저장
        ApplyVisibility(); // 실제 미니맵 표시 상태 갱신
    }

    private void LateUpdate() // 플레이어 이동 이후 방향 아이콘 갱신
    {
        if (!initialized || !IsVisible) // 초기화와 실제 표시 상태 확인
        {
            return; // 방향 아이콘 갱신 생략
        }

        RefreshDirectionIcon(); // 현재 플레이어 방향 표시
    }

    private void ConfigureClickButton() // 미니맵 클릭 Button 설정
    {
        if (listenerRegistered || minimapClickButton == null) // 기존 이벤트와 Button 참조 확인
        {
            return; // 중복 이벤트 등록 방지
        }

        Navigation navigation = minimapClickButton.navigation; // 현재 Button Navigation 조회
        navigation.mode = Navigation.Mode.None; // 키보드와 게임패드 선택으로 지도 열기 방지
        minimapClickButton.navigation = navigation; // 변경된 Navigation 적용

        minimapClickButton.onClick.AddListener(
            OnMinimapClicked); // 미니맵 클릭 이벤트 등록

        listenerRegistered = true; // Button 이벤트 등록 완료 기록
    }

    private void RemoveClickButtonListener() // 미니맵 클릭 Button 이벤트 제거
    {
        if (!listenerRegistered || minimapClickButton == null) // 이벤트 등록과 Button 참조 확인
        {
            return; // 제거할 이벤트 없음
        }

        minimapClickButton.onClick.RemoveListener(
            OnMinimapClicked); // 미니맵 클릭 이벤트 제거

        listenerRegistered = false; // Button 이벤트 등록 상태 초기화
    }

    private void OnMinimapClicked() // Alt 커서 상태에서 미니맵 클릭 처리
    {
        if (!initialized || worldMapController == null) // 초기화와 지도 관리자 확인
        {
            return; // 전체 지도 열기 요청 중단
        }

        bool cursorActive =
            Cursor.visible
            && Cursor.lockState != CursorLockMode.Locked; // Alt 입력으로 활성화된 커서 상태 확인

        if (!cursorActive) // 커서 비활성 상태 확인
        {
            return; // Gameplay 중 우발적인 Button 선택 입력 차단
        }

        worldMapController.OpenFullMapFromMinimap(); // 클릭한 미니맵과 동일한 전체 지도 열기
    }

    private void ApplyCurrentLayout() // 현재 크기 상태의 RectTransform 값 적용
    {
        if (minimapRect == null) // 미니맵 RectTransform 존재 확인
        {
            return; // 레이아웃 적용 생략
        }

        minimapRect.sizeDelta =
            isExpanded
                ? expandedSize
                : compactSize; // 현재 상태의 미니맵 크기 적용

        minimapRect.anchoredPosition =
            isExpanded
                ? expandedAnchoredPosition
                : compactAnchoredPosition; // 현재 상태의 미니맵 위치 적용
    }

    private void ApplyVisibility() // 사용자 설정과 전체 지도 상태를 합친 표시 적용
    {
        if (panelRoot == null) // 미니맵 화면 루트 존재 확인
        {
            return; // 표시 상태 적용 생략
        }

        panelRoot.SetActive(
            userVisible
            && !suppressedByFullMap); // 사용자 표시 상태이며 전체 지도가 아닐 때 미니맵 표시
    }

    private void RefreshDirectionIcon() // 플레이어 방향 화살표 회전 적용
    {
        if (playerTarget == null || playerDirectionIcon == null) // 플레이어와 방향 아이콘 확인
        {
            return; // 방향 표시 생략
        }

        float playerYaw = playerTarget.eulerAngles.y; // 플레이어 월드 Y 회전값 조회
        float iconRotation = -playerYaw + directionRotationOffset; // 북쪽 고정 지도 기준 UI Z 회전 계산

        playerDirectionIcon.localRotation =
            Quaternion.Euler(0f, 0f, iconRotation); // 플레이어 방향 화살표 회전 적용
    }

    private void OnDestroy() // 미니맵 UI 제거 정리
    {
        RemoveClickButtonListener(); // 미니맵 Button 이벤트 제거
    }
}
