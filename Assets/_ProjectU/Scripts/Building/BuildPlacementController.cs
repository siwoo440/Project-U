using System.Collections.Generic; // 목록 기능
using System.Text; // 상태 문자열 조합 기능
using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.InputSystem; // 새로운 입력 시스템

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class BuildPlacementController : MonoBehaviour // 혼합형 건축 배치 관리자
{
    [Header("References")] // 외부 참조 묶음
    [Tooltip("일반 플레이 중 사용하는 기본 플레이어 Camera입니다.")]
    [SerializeField] private Camera mainCamera; // 기본 플레이어 Camera

    [Tooltip("건축 모드 자유 Camera 전환과 마우스 포인터 위치를 제공하는 관리자입니다.")]
    [SerializeField] private BuildModeCameraController buildModeCameraController; // 건축 자유 Camera 관리자

    [Tooltip("플레이어 위치입니다.")]
    [SerializeField] private Transform playerTransform; // 플레이어 위치

    [Tooltip("플레이어 인벤토리입니다.")]
    [SerializeField] private PlayerInventory playerInventory; // 플레이어 인벤토리

    [Tooltip("플레이어 체력입니다.")]
    [SerializeField] private PlayerHealth playerHealth; // 플레이어 체력

    [Tooltip("공통 게임 UI 관리자입니다.")]
    [SerializeField] private GameUIManager gameUIManager; // 공통 게임 UI 관리자

    [Tooltip("전체 지도 상태를 확인할 지도 관리자입니다.")]
    [SerializeField] private WorldMapController worldMapController; // 전체 지도 관리자

    [Tooltip("건축 그리드 구역입니다.")]
    [SerializeField] private BuildGridArea gridArea; // 건축 그리드 구역

    [Tooltip("설치된 건축물을 정리할 부모 Transform입니다.")]
    [SerializeField] private Transform placedObjectRoot; // 설치 건축물 부모

    [Header("Recipes")] // 건축 데이터 묶음
    [Tooltip("선택 가능한 건축물 목록입니다.")]
    [SerializeField] private BuildRecipeData[] buildRecipes = new BuildRecipeData[0]; // 선택 건축물 목록

    [Header("Materials")] // 건축 재질 묶음
    [Tooltip("설치 가능한 Preview에 적용할 재질입니다.")]
    [SerializeField] private Material validPreviewMaterial; // 설치 가능 재질

    [Tooltip("설치 불가능한 Preview에 적용할 재질입니다.")]
    [SerializeField] private Material invalidPreviewMaterial; // 설치 불가능 재질

    [Tooltip("철거 대상에 적용할 강조 재질입니다.")]
    [SerializeField] private Material removalTargetMaterial; // 철거 대상 재질

    [Header("Detection")] // 배치 탐지 설정 묶음
    [Tooltip("Terrain과 건축 가능한 지면 레이어입니다.")]
    [SerializeField] private LayerMask groundLayerMask; // Terrain 레이어

    [Tooltip("설치를 막는 장애물 레이어입니다.")]
    [SerializeField] private LayerMask obstructionLayerMask; // 설치 방해 레이어

    [Tooltip("설치된 건축물 레이어입니다.")]
    [SerializeField] private LayerMask structureLayerMask; // 건축물 레이어

    [Tooltip("Player 위치에서 Preview 또는 철거 대상까지 허용할 최대 거리입니다.")]
    [SerializeField] private float maximumBuildDistance = 25f; // Player 기준 최대 건축 거리

    [Tooltip("자유 건축 Camera에서 마우스 포인터 Ray를 탐색할 최대 거리입니다.")]
    [SerializeField] private float maximumPlacementRayDistance = 200f; // Camera 기준 최대 Ray 거리

    [Tooltip("Terrain 표본 검사를 시작할 높이입니다.")]
    [SerializeField] private float terrainProbeHeight = 5f; // Terrain 표본 시작 높이

    [Tooltip("Terrain 표본을 아래쪽으로 검사할 거리입니다.")]
    [SerializeField] private float terrainProbeDistance = 12f; // Terrain 표본 탐지 거리

    [Tooltip("배치 충돌 검사 Box 크기에서 줄일 여유값입니다.")]
    [SerializeField] private float collisionPadding = 0.02f; // 충돌 검사 여유값

    [Tooltip("마우스 포인터 충돌 지점에서 연결점을 찾을 최대 거리입니다.")]
    [SerializeField] private float connectionSnapDistance = 0.8f; // 연결점 탐지 거리

    [Header("UI")] // 건축 UI 참조 묶음
    [Tooltip("건축 모드에서 표시할 HUD 루트입니다.")]
    [SerializeField] private GameObject buildHudRoot; // 건축 HUD 루트

    [Tooltip("건축 상태와 조작법을 표시할 Text입니다.")]
    [SerializeField] private TMP_Text buildStatusText; // 건축 상태 Text

    private GameObject previewInstance; // 현재 미리보기 오브젝트
    private Renderer[] previewRenderers; // 미리보기 렌더러 목록
    private BuildRecipeData currentRecipe; // 현재 건축 데이터
    private int currentRecipeIndex; // 현재 건축 데이터 번호
    private float currentLocalYaw; // 그리드 기준 회전값
    private bool isBuildMode; // 건축 모드 상태
    private bool canPlace; // 현재 설치 가능 상태
    private int lastBuildInputFrame = -1; // 마지막 건축 입력 프레임
    private bool isRemovalMode; // 철거 모드 상태
    private PlacedBuildObject currentRemovalTarget; // 현재 철거 대상
    private BuildConnectionPoint currentConnectionPoint; // 현재 구조 연결점

    public bool IsBuildMode => isBuildMode; // 건축 모드 상태 제공
    public bool BlocksGameplayInput => isBuildMode || Time.frameCount == lastBuildInputFrame; // 일반 입력 차단 상태

    private void Awake() // 건축 관리자 초기화
    {
        ResolveReferences(); // 누락된 Scene 참조 자동 검색

        bool hasMissingReference =
            mainCamera == null
            || buildModeCameraController == null
            || playerTransform == null
            || playerInventory == null
            || playerHealth == null
            || gameUIManager == null
            || gridArea == null
            || placedObjectRoot == null
            || validPreviewMaterial == null
            || invalidPreviewMaterial == null
            || removalTargetMaterial == null
            || buildHudRoot == null
            || buildStatusText == null
            || buildRecipes == null
            || buildRecipes.Length == 0; // 필수 참조 누락 확인

        if (hasMissingReference) // 필수 참조 누락 여부 확인
        {
            Debug.LogError("BuildPlacementController의 필수 참조를 모두 연결해야 합니다.", this); // 참조 오류 출력
            enabled = false; // 건축 기능 비활성화
            return; // 초기화 중단
        }

        if (groundLayerMask.value == 0
            || obstructionLayerMask.value == 0
            || structureLayerMask.value == 0) // 건축 레이어 설정 확인
        {
            Debug.LogError("건축 Ground, Obstruction, Structure Layer Mask를 설정해야 합니다.", this); // 레이어 오류 출력
            enabled = false; // 건축 기능 비활성화
            return; // 초기화 중단
        }

        currentRecipeIndex = 0; // 시작 건축 데이터 번호 설정
        currentRecipe = buildRecipes[currentRecipeIndex]; // 시작 건축 데이터 선택
        buildHudRoot.SetActive(false); // 시작 건축 HUD 숨김
        buildStatusText.SetText(string.Empty); // 시작 상태 문구 제거
        gridArea.SetGridVisible(false); // 시작 그리드 숨김
    }

    private void Update() // 건축 입력과 포인터 기반 Preview 처리
    {
        Keyboard keyboard = Keyboard.current; // 현재 키보드 조회
        Mouse mouse = Mouse.current; // 현재 마우스 조회

        if (keyboard == null || mouse == null) // 입력 장치 존재 확인
        {
            return; // 입력 처리 중단
        }

        bool hasOpenPopup = gameUIManager != null && gameUIManager.HasOpenPopup; // 현재 공통 팝업 열림 상태 확인
        bool hasOpenWorldMap = worldMapController != null && worldMapController.IsFullMapOpen; // 전체 지도 열림 상태 확인
        bool hasOpenPauseMenu = PauseMenuController.IsPaused; // 일시정지 메뉴 열림 상태 확인

        if (isBuildMode
            && (hasOpenPopup
                || hasOpenWorldMap
                || hasOpenPauseMenu
                || playerHealth.IsDead
                || !buildModeCameraController.IsActive)) // 건축 강제 종료 조건 확인
        {
            ExitBuildMode(); // 건축 모드 종료
            return; // 같은 프레임 처리 중단
        }

        if (!isBuildMode) // 일반 플레이 상태 확인
        {
            bool canEnterBuildMode =
                keyboard.bKey.wasPressedThisFrame
                && Cursor.lockState == CursorLockMode.Locked
                && !hasOpenPopup
                && !hasOpenWorldMap
                && !hasOpenPauseMenu
                && !playerHealth.IsDead; // 건축 진입 조건 계산

            if (canEnterBuildMode) // 건축 진입 입력 확인
            {
                EnterBuildMode(); // 건축 모드 시작
            }

            return; // 일반 상태 처리 종료
        }

        bool cancelRequested =
            keyboard.bKey.wasPressedThisFrame
            || keyboard.escapeKey.wasPressedThisFrame; // B 또는 ESC 건축 종료 입력 계산

        if (cancelRequested) // 건축 종료 입력 확인
        {
            ExitBuildMode(); // 건축 모드 종료
            return; // 같은 프레임 설치 차단
        }

        if (keyboard.rKey.wasPressedThisFrame) // 설치와 철거 전환 입력 확인
        {
            SetRemovalMode(!isRemovalMode); // 현재 모드 반전
            return; // 전환 프레임 입력 차단
        }

        bool isCameraManipulation = buildModeCameraController.IsManipulatingCamera; // 우클릭 또는 휠 클릭 Camera 조작 상태 확인

        if (isRemovalMode) // 철거 모드 확인
        {
            if (!isCameraManipulation) // Camera 조작 중이 아닌지 확인
            {
                UpdateRemovalTarget(); // 마우스 포인터 위치의 철거 대상 갱신
            }

            if (!isCameraManipulation && mouse.leftButton.wasPressedThisFrame) // 철거 입력 가능 여부 확인
            {
                TryRemoveCurrentTarget(); // 현재 대상 철거 시도
            }

            return; // 설치 입력 처리 차단
        }

        if (keyboard.zKey.wasPressedThisFrame) // 이전 건축물 입력 확인
        {
            ChangeRecipe(-1); // 이전 건축물 선택
        }

        if (keyboard.xKey.wasPressedThisFrame) // 다음 건축물 입력 확인
        {
            ChangeRecipe(1); // 다음 건축물 선택
        }

        if (keyboard.qKey.wasPressedThisFrame) // 왼쪽 회전 입력 확인
        {
            RotatePreview(-1f); // 미리보기 왼쪽 회전
        }

        if (keyboard.eKey.wasPressedThisFrame) // 오른쪽 회전 입력 확인
        {
            RotatePreview(1f); // 미리보기 오른쪽 회전
        }

        if (!isCameraManipulation) // Camera 드래그 조작 상태 확인
        {
            UpdatePreview(); // 마우스 포인터 위치의 미리보기 상태 갱신
        }

        if (!isCameraManipulation && mouse.leftButton.wasPressedThisFrame) // 설치 입력 가능 여부 확인
        {
            TryPlaceStructure(); // 건축물 설치 시도
        }
    }

    private void EnterBuildMode() // 건축 모드 시작
    {
        currentRecipe = buildRecipes[currentRecipeIndex]; // 현재 건축 데이터 갱신

        if (currentRecipe == null) // 건축 데이터 연결 확인
        {
            Debug.LogError("Build Recipes에 비어 있는 Element가 있습니다.", this); // 건축 데이터 오류 출력
            return; // 건축 모드 시작 중단
        }

        if (!buildModeCameraController.BeginBuildMode()) // 자유 건축 Camera 시작 시도
        {
            Debug.LogError("자유 건축 Camera 시작에 실패했습니다.", this); // 자유 Camera 시작 오류 출력
            return; // 건축 모드 시작 중단
        }

        float relativePlayerYaw = playerTransform.eulerAngles.y - gridArea.transform.eulerAngles.y; // 그리드 기준 플레이어 회전 계산
        float rotationStep = GetCurrentRotationStep(); // 현재 회전 단위 조회
        currentLocalYaw = Mathf.Round(relativePlayerYaw / rotationStep) * rotationStep; // 시작 회전값 정렬
        isBuildMode = true; // 건축 모드 활성화
        isRemovalMode = false; // 설치 모드 설정
        currentRemovalTarget = null; // 기존 철거 대상 제거
        currentConnectionPoint = null; // 기존 연결점 제거
        canPlace = false; // 설치 가능 상태 해제
        lastBuildInputFrame = Time.frameCount; // 일반 입력 차단 프레임 저장
        CreatePreview(); // 미리보기 생성
        gridArea.SetGridVisible(true); // 건축 그리드 표시
        buildHudRoot.SetActive(true); // 건축 HUD 표시
        RefreshStatus("SEARCHING POINTER"); // 초기 포인터 탐색 상태 표시
    }

    private void ExitBuildMode() // 건축 모드 종료
    {
        ClearRemovalTarget(); // 철거 대상 강조 해제
        isRemovalMode = false; // 철거 모드 비활성화
        isBuildMode = false; // 건축 모드 비활성화
        canPlace = false; // 설치 가능 상태 해제
        currentConnectionPoint = null; // 현재 연결점 제거
        lastBuildInputFrame = Time.frameCount; // 종료 프레임 저장

        if (previewInstance != null) // 미리보기 존재 확인
        {
            Destroy(previewInstance); // 미리보기 제거
        }

        previewInstance = null; // 미리보기 참조 제거
        previewRenderers = null; // 렌더러 참조 제거
        gridArea.SetGridVisible(false); // 건축 그리드 숨김
        buildHudRoot.SetActive(false); // 건축 HUD 숨김
        buildStatusText.SetText(string.Empty); // 상태 문구 제거

        if (buildModeCameraController != null) // 자유 건축 Camera 관리자 존재 확인
        {
            buildModeCameraController.EndBuildMode(); // 진입 전 플레이어 시점과 Gameplay 상태 복구
        }
    }

    public void ExitBuildModeFromExternal() // 일시정지 메뉴와 다른 시스템의 건축 모드 종료 요청
    {
        if (!isBuildMode) // 현재 건축 모드 확인
        {
            return; // 종료할 건축 모드 없음
        }

        ExitBuildMode(); // 건축 Preview와 자유 Camera 전체 정리
    }

    private void ChangeRecipe(int direction) // 선택 건축물 변경
    {
        currentRecipeIndex += direction; // 건축 데이터 번호 이동

        if (currentRecipeIndex < 0) // 첫 번째 이전 이동 확인
        {
            currentRecipeIndex = buildRecipes.Length - 1; // 마지막 건축 데이터 선택
        }
        else if (currentRecipeIndex >= buildRecipes.Length) // 마지막 이후 이동 확인
        {
            currentRecipeIndex = 0; // 첫 번째 건축 데이터 선택
        }

        currentRecipe = buildRecipes[currentRecipeIndex]; // 새로운 건축 데이터 저장
        currentConnectionPoint = null; // 이전 연결점 제거

        if (currentRecipe == null) // 새로운 건축 데이터 연결 확인
        {
            Debug.LogError("Build Recipes에 비어 있는 Element가 있습니다.", this); // 건축 데이터 오류 출력
            ExitBuildMode(); // 잘못된 건축 모드 종료
            return; // 변경 처리 중단
        }

        float rotationStep = GetCurrentRotationStep(); // 새로운 회전 단위 조회
        currentLocalYaw = Mathf.Round(currentLocalYaw / rotationStep) * rotationStep; // 회전값 재정렬
        CreatePreview(); // 새로운 미리보기 생성
        lastBuildInputFrame = Time.frameCount; // 변경 프레임 저장
    }

    private void RotatePreview(float direction) // 현재 미리보기 회전
    {
        float rotationStep = GetCurrentRotationStep(); // 현재 회전 단위 조회
        currentLocalYaw += rotationStep * direction; // 회전값 변경
        currentLocalYaw = Mathf.Repeat(currentLocalYaw, 360f); // 회전 범위 정규화
        lastBuildInputFrame = Time.frameCount; // 회전 프레임 저장
    }

    private float GetCurrentRotationStep() // 현재 회전 단위 조회
    {
        if (currentRecipe.PlacementType == BuildPlacementType.Floor) // 바닥 배치 확인
        {
            return 90f; // 바닥 회전 단위 반환
        }

        if (currentRecipe.PlacementType == BuildPlacementType.Wall) // 벽 배치 확인
        {
            return 90f; // 벽 회전 단위 반환
        }

        return currentRecipe.RotationStep; // 자유 배치 회전 단위 반환
    }

    private void CreatePreview() // 현재 미리보기 생성
    {
        if (previewInstance != null) // 기존 미리보기 존재 확인
        {
            Destroy(previewInstance); // 기존 미리보기 제거
        }

        previewInstance = Instantiate(currentRecipe.PreviewPrefab); // 새로운 미리보기 생성
        previewInstance.name = $"{currentRecipe.DisplayName}_Preview"; // 미리보기 이름 설정
        previewRenderers = previewInstance.GetComponentsInChildren<Renderer>(true); // 전체 렌더러 조회
        Collider[] previewColliders = previewInstance.GetComponentsInChildren<Collider>(true); // 전체 충돌체 조회

        for (int index = 0; index < previewColliders.Length; index++) // 미리보기 충돌체 순회
        {
            previewColliders[index].enabled = false; // 미리보기 충돌체 비활성화
        }

        SetPreviewMaterial(false); // 시작 불가능 재질 적용
    }

    private void UpdatePreview() // 마우스 포인터 건축 미리보기 갱신
    {
        Ray placementRay = GetPointerRay(); // 현재 마우스 포인터 위치의 Camera 광선 생성
        Vector3 previewPosition; // 최종 미리보기 위치
        Quaternion previewRotation; // 최종 미리보기 회전
        string failureStatus; // 배치 실패 문구
        bool placementResolved; // 배치 위치 계산 결과

        currentConnectionPoint = null; // 이전 연결점 제거

        if (currentRecipe.StructureType == BuildStructureType.Furniture) // 기능성 가구 배치 확인
        {
            placementResolved = TryResolveFurniturePlacement(
                placementRay,
                out previewPosition,
                out previewRotation,
                out failureStatus); // 가구 배치 계산
        }
        else if (RequiresStructureConnection()) // 필수 구조 연결 여부 확인
        {
            placementResolved = TryResolveConnectionPlacement(
                placementRay,
                out previewPosition,
                out previewRotation,
                out failureStatus); // 연결점 배치 계산
        }
        else // Terrain 배치 처리
        {
            placementResolved = TryResolveGroundPlacement(
                placementRay,
                out previewPosition,
                out previewRotation,
                out failureStatus); // 지면 배치 계산
        }

        if (!placementResolved) // 배치 위치 계산 실패 확인
        {
            SetPreviewUnavailable(failureStatus); // 실패 상태 적용
            return; // 미리보기 갱신 중단
        }

        previewInstance.SetActive(true); // 미리보기 표시
        previewInstance.transform.SetPositionAndRotation(previewPosition, previewRotation); // 미리보기 위치 적용

        bool hasObstruction = HasBlockingOverlap(previewPosition, previewRotation); // 공간 장애물 검사
        bool hasMaterials = HasRequiredMaterials(); // 필요 재료 검사

        canPlace = !hasObstruction && hasMaterials; // 최종 설치 가능 상태 계산
        SetPreviewMaterial(canPlace); // 설치 상태 재질 적용

        if (hasObstruction) // 장애물 존재 확인
        {
            RefreshStatus("SPACE BLOCKED"); // 공간 차단 문구 표시
            return; // 상태 처리 종료
        }

        if (!hasMaterials) // 재료 부족 확인
        {
            RefreshStatus("NEED MATERIALS"); // 재료 부족 문구 표시
            return; // 상태 처리 종료
        }

        RefreshStatus("READY"); // 설치 가능 문구 표시
    }

    private Ray GetPointerRay() // 현재 활성 건축 Camera와 마우스 포인터로 Ray 생성
    {
        Camera placementCamera = buildModeCameraController != null
            ? buildModeCameraController.ActiveCamera
            : null; // 자유 건축 Camera 조회

        if (placementCamera == null) // 자유 Camera 참조 확인
        {
            placementCamera = mainCamera; // 기본 플레이어 Camera 사용
        }

        Vector2 pointerPosition = buildModeCameraController != null
            ? buildModeCameraController.PointerScreenPosition
            : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f); // 마우스 포인터 또는 화면 중앙 좌표 조회

        return placementCamera.ScreenPointToRay(pointerPosition); // 포인터 위치 기준 월드 Ray 반환
    }

    private bool RequiresStructureConnection() // 구조 연결 필요 여부 확인
    {
        BuildStructureType structureType = currentRecipe.StructureType; // 현재 구조 역할 조회

        return structureType == BuildStructureType.Floor
            || structureType == BuildStructureType.Wall; // 필수 연결점 배치 구조 반환
    }

    private bool TryResolveFurniturePlacement(
        Ray placementRay,
        out Vector3 previewPosition,
        out Quaternion previewRotation,
        out string failureStatus) // 기능성 가구 배치 위치 계산
    {
        bool hasStructure = Physics.Raycast(
            placementRay,
            maximumPlacementRayDistance,
            structureLayerMask,
            QueryTriggerInteraction.Ignore); // 포인터 위치 구조물 존재 확인

        if (hasStructure) // 구조물 탐지 확인
        {
            return TryResolveConnectionPlacement(
                placementRay,
                out previewPosition,
                out previewRotation,
                out failureStatus); // 구조물 연결점 배치 계산
        }

        if (currentRecipe.AllowGroundPlacement) // 지면 배치 허용 여부 확인
        {
            return TryResolveGroundPlacement(
                placementRay,
                out previewPosition,
                out previewRotation,
                out failureStatus); // 기존 Terrain 배치 계산
        }

        previewPosition = Vector3.zero; // 실패 위치 초기화
        previewRotation = Quaternion.identity; // 실패 회전 초기화
        failureStatus = "POINT AT SUPPORT"; // 지지 구조 안내 문구
        return false; // 가구 배치 계산 실패
    }

    private bool TryResolveConnectionPlacement(
        Ray placementRay,
        out Vector3 previewPosition,
        out Quaternion previewRotation,
        out string failureStatus) // 구조 연결 배치 위치 계산
    {
        previewPosition = Vector3.zero; // 기본 위치 초기화
        previewRotation = Quaternion.identity; // 기본 회전 초기화
        failureStatus = string.Empty; // 기본 실패 문구 초기화

        bool hasStructure = Physics.Raycast(
            placementRay,
            out RaycastHit structureHit,
            maximumPlacementRayDistance,
            structureLayerMask,
            QueryTriggerInteraction.Ignore); // 포인터 위치 구조물 탐지

        if (!hasStructure) // 구조물 미탐지 확인
        {
            failureStatus = "POINT AT SUPPORT"; // 지지 구조 안내 문구
            return false; // 배치 계산 실패 반환
        }

        float playerDistance = Vector3.Distance(playerTransform.position, structureHit.point); // 플레이어 거리 계산

        if (playerDistance > maximumBuildDistance) // 최대 건축 거리 확인
        {
            failureStatus = "TOO FAR FROM PLAYER"; // 거리 초과 문구
            return false; // 배치 계산 실패 반환
        }

        PlacedBuildObject supportObject = structureHit.collider.GetComponentInParent<PlacedBuildObject>(); // 지지 건축물 검색

        if (supportObject == null) // 지지 건축물 정보 확인
        {
            failureStatus = "INVALID SUPPORT"; // 잘못된 지지 구조 문구
            return false; // 배치 계산 실패 반환
        }

        if (!TryFindNearestConnectionPoint(
            supportObject,
            structureHit.point,
            out BuildConnectionPoint connectionPoint)) // 가장 가까운 연결점 검색
        {
            failureStatus = "NO FREE CONNECTION"; // 빈 연결점 없음 문구
            return false; // 배치 계산 실패 반환
        }

        currentConnectionPoint = connectionPoint; // 현재 연결점 저장
        previewRotation = connectionPoint.SnapRotation; // 연결점 회전 적용

        bool usesFreeRotation =
            currentRecipe.StructureType == BuildStructureType.Floor
            || currentRecipe.StructureType == BuildStructureType.Furniture; // 추가 회전 사용 여부 계산

        if (usesFreeRotation) // 추가 회전 사용 확인
        {
            previewRotation *= Quaternion.Euler(0f, currentLocalYaw, 0f); // 사용자 회전 적용
        }

        Vector3 rotatedOffset = previewRotation * currentRecipe.PreviewOffset; // 회전 적용 위치 보정
        previewPosition = connectionPoint.SnapPosition + rotatedOffset; // 연결점 기준 위치 적용
        return true; // 배치 계산 성공 반환
    }

    private bool TryFindNearestConnectionPoint(
        PlacedBuildObject supportObject,
        Vector3 hitPoint,
        out BuildConnectionPoint nearestConnectionPoint) // 가장 가까운 빈 연결점 검색
    {
        nearestConnectionPoint = null; // 기본 검색 결과 초기화
        float nearestDistance = float.MaxValue; // 최소 거리 초기화
        IReadOnlyList<BuildConnectionPoint> connectionPoints = supportObject.ConnectionPoints; // 지지 구조 연결점 조회

        for (int index = 0; index < connectionPoints.Count; index++) // 전체 연결점 순회
        {
            BuildConnectionPoint connectionPoint = connectionPoints[index]; // 현재 연결점 조회

            if (connectionPoint == null) // 연결점 존재 확인
            {
                continue; // 빈 연결점 제외
            }

            if (connectionPoint.IsOccupied) // 연결점 사용 여부 확인
            {
                continue; // 사용 중인 연결점 제외
            }

            if (!connectionPoint.Accepts(currentRecipe.StructureType)) // 구조 역할 허용 여부 확인
            {
                continue; // 연결 불가능 지점 제외
            }

            float connectionDistance = Vector3.Distance(hitPoint, connectionPoint.SnapPosition); // 충돌 위치와 연결점 거리 계산

            if (connectionDistance > connectionSnapDistance) // 최대 탐지 거리 확인
            {
                continue; // 먼 연결점 제외
            }

            if (connectionDistance >= nearestDistance) // 기존 결과보다 가까운지 확인
            {
                continue; // 더 먼 연결점 제외
            }

            nearestDistance = connectionDistance; // 최소 거리 갱신
            nearestConnectionPoint = connectionPoint; // 가장 가까운 연결점 저장
        }

        return nearestConnectionPoint != null; // 검색 성공 여부 반환
    }

    private bool TryResolveGroundPlacement(
        Ray placementRay,
        out Vector3 previewPosition,
        out Quaternion previewRotation,
        out string failureStatus) // Terrain 배치 위치 계산
    {
        previewPosition = Vector3.zero; // 기본 위치 초기화
        previewRotation = Quaternion.identity; // 기본 회전 초기화
        failureStatus = string.Empty; // 기본 실패 문구 초기화

        bool hasGround = Physics.Raycast(
            placementRay,
            out RaycastHit groundHit,
            maximumPlacementRayDistance,
            groundLayerMask,
            QueryTriggerInteraction.Ignore); // 포인터 위치 Terrain 탐지

        if (!hasGround) // Terrain 미탐지 확인
        {
            failureStatus = "NO BUILDABLE GROUND"; // 지면 없음 문구
            return false; // 배치 계산 실패 반환
        }

        if (!gridArea.ContainsWorldPoint(groundHit.point)) // 건축 구역 포함 여부 확인
        {
            failureStatus = "OUTSIDE BUILD AREA"; // 구역 밖 문구
            return false; // 배치 계산 실패 반환
        }

        float playerDistance = Vector3.Distance(playerTransform.position, groundHit.point); // 플레이어 거리 계산

        if (playerDistance > maximumBuildDistance) // 최대 건축 거리 확인
        {
            failureStatus = "TOO FAR FROM PLAYER"; // 거리 초과 문구
            return false; // 배치 계산 실패 반환
        }

        Vector3 basePosition; // 배치 기준 위치

        if (currentRecipe.PlacementType == BuildPlacementType.Floor) // 타일 중앙 배치 확인
        {
            if (!gridArea.TryGetCell(groundHit.point, out Vector2Int cell)) // 대상 타일 계산
            {
                failureStatus = "OUTSIDE BUILD AREA"; // 구역 밖 문구
                return false; // 배치 계산 실패 반환
            }

            basePosition = gridArea.GetCellCenter(cell); // 타일 중앙 위치 계산
            previewRotation = gridArea.transform.rotation * Quaternion.Euler(0f, currentLocalYaw, 0f); // 타일 회전 계산
        }
        else if (currentRecipe.PlacementType == BuildPlacementType.Wall) // 기존 벽 배치 확인
        {
            bool wallSnapSucceeded = gridArea.TryGetWallSnap(
                groundHit.point,
                currentLocalYaw,
                out basePosition,
                out previewRotation); // 타일 경계 계산

            if (!wallSnapSucceeded) // 벽 경계 계산 실패 확인
            {
                failureStatus = "OUTSIDE BUILD AREA"; // 구역 밖 문구
                return false; // 배치 계산 실패 반환
            }
        }
        else // 자유 배치 처리
        {
            basePosition = groundHit.point; // 실제 지면 위치 적용
            previewRotation = gridArea.transform.rotation * Quaternion.Euler(0f, currentLocalYaw, 0f); // 자유 배치 회전 계산
        }

        Vector3 horizontalOffset = previewRotation * new Vector3(
            currentRecipe.PreviewOffset.x,
            0f,
            currentRecipe.PreviewOffset.z); // 수평 위치 보정 계산

        basePosition += horizontalOffset; // 수평 위치 보정 적용

        bool terrainValid = TrySampleTerrain(
            basePosition,
            previewRotation,
            currentRecipe.PlacementCheckHalfExtents,
            out float maximumTerrainY,
            out float maximumSlope,
            out float terrainHeightDifference); // Terrain 표본 검사

        if (!terrainValid) // Terrain 표본 실패 확인
        {
            failureStatus = "UNSUPPORTED TERRAIN"; // Terrain 미지원 문구
            return false; // 배치 계산 실패 반환
        }

        if (maximumSlope > currentRecipe.MaximumSlopeAngle) // 최대 경사 확인
        {
            failureStatus = "SLOPE TOO STEEP"; // 경사 초과 문구
            return false; // 배치 계산 실패 반환
        }

        if (terrainHeightDifference > currentRecipe.MaximumHeightDifference) // 지형 높이 차이 확인
        {
            failureStatus = "UNEVEN TERRAIN"; // 지형 불균형 문구
            return false; // 배치 계산 실패 반환
        }

        previewPosition = new Vector3(
            basePosition.x,
            maximumTerrainY + currentRecipe.PreviewOffset.y,
            basePosition.z); // Terrain 위 최종 위치 계산

        return true; // 배치 계산 성공 반환
    }

    private bool TrySampleTerrain(
        Vector3 center,
        Quaternion rotation,
        Vector3 halfExtents,
        out float maximumTerrainY,
        out float maximumSlope,
        out float heightDifference) // 배치 바닥 다중 지점 검사
    {
        maximumTerrainY = float.MinValue; // 최대 Terrain 높이 초기화
        maximumSlope = 0f; // 최대 경사 초기화
        float minimumTerrainY = float.MaxValue; // 최소 Terrain 높이 초기화

        Vector3[] localSampleOffsets =
        {
            Vector3.zero, // 중앙 표본
            new Vector3(-halfExtents.x * 0.9f, 0f, -halfExtents.z * 0.9f), // 왼쪽 아래 표본
            new Vector3(-halfExtents.x * 0.9f, 0f, halfExtents.z * 0.9f), // 왼쪽 위 표본
            new Vector3(halfExtents.x * 0.9f, 0f, -halfExtents.z * 0.9f), // 오른쪽 아래 표본
            new Vector3(halfExtents.x * 0.9f, 0f, halfExtents.z * 0.9f) // 오른쪽 위 표본
        };

        for (int index = 0; index < localSampleOffsets.Length; index++) // 전체 Terrain 표본 순회
        {
            Vector3 rotatedOffset = rotation * localSampleOffsets[index]; // 회전 적용 표본 위치 계산
            Vector3 samplePosition = center + rotatedOffset; // 월드 표본 위치 계산
            Vector3 rayOrigin = samplePosition + Vector3.up * terrainProbeHeight; // 표본 광선 시작 위치

            bool hasGround = Physics.Raycast(
                rayOrigin,
                Vector3.down,
                out RaycastHit terrainHit,
                terrainProbeDistance,
                groundLayerMask,
                QueryTriggerInteraction.Ignore); // 현재 표본 Terrain 탐지

            if (!hasGround) // Terrain 미탐지 확인
            {
                heightDifference = float.MaxValue; // 실패 높이 차이 설정
                return false; // Terrain 검사 실패 반환
            }

            maximumTerrainY = Mathf.Max(maximumTerrainY, terrainHit.point.y); // 최대 높이 갱신
            minimumTerrainY = Mathf.Min(minimumTerrainY, terrainHit.point.y); // 최소 높이 갱신
            float slopeAngle = Vector3.Angle(terrainHit.normal, Vector3.up); // 현재 지면 경사 계산
            maximumSlope = Mathf.Max(maximumSlope, slopeAngle); // 최대 경사 갱신
        }

        heightDifference = maximumTerrainY - minimumTerrainY; // 전체 높이 차이 계산
        return true; // Terrain 검사 성공 반환
    }

    private bool HasBlockingOverlap(Vector3 previewPosition, Quaternion previewRotation) // 배치 공간 장애물 검사
    {
        Vector3 checkCenter = previewPosition + previewRotation * currentRecipe.PlacementCheckCenter; // 충돌 검사 중심 계산
        Vector3 halfExtents = currentRecipe.PlacementCheckHalfExtents; // 기본 충돌 절반 크기 조회
        halfExtents.x = Mathf.Max(0.01f, halfExtents.x - collisionPadding); // X 충돌 크기 축소
        halfExtents.y = Mathf.Max(0.01f, halfExtents.y - collisionPadding); // Y 충돌 크기 축소
        halfExtents.z = Mathf.Max(0.01f, halfExtents.z - collisionPadding); // Z 충돌 크기 축소

        Collider[] overlaps = Physics.OverlapBox(
            checkCenter,
            halfExtents,
            previewRotation,
            obstructionLayerMask,
            QueryTriggerInteraction.Ignore); // 주변 장애물 탐지

        for (int index = 0; index < overlaps.Length; index++) // 탐지된 충돌체 순회
        {
            Collider overlap = overlaps[index]; // 현재 충돌체 조회
            PlacedBuildObject existingBuildObject = overlap.GetComponentInParent<PlacedBuildObject>(); // 설치 건축물 조회

            if (existingBuildObject == null) // 일반 장애물 확인
            {
                return true; // 일반 장애물 차단
            }

            if (CanSharePlacementSpace(
                currentRecipe.PlacementType,
                previewPosition,
                previewRotation,
                existingBuildObject)) // 건축물 공존 가능 여부 확인
            {
                continue; // 공존 가능한 건축물 제외
            }

            return true; // 공존 불가능 건축물 차단
        }

        return false; // 장애물 없음 반환
    }

    private bool CanSharePlacementSpace(
        BuildPlacementType targetType,
        Vector3 targetPosition,
        Quaternion targetRotation,
        PlacedBuildObject existingObject) // 건축물 공간 공유 확인
    {
        if (currentConnectionPoint != null && existingObject == currentConnectionPoint.Owner) // 지지 구조물 확인
        {
            return true; // 지지 구조물 공간 공유 허용
        }

        BuildPlacementType existingType = existingObject.PlacementType; // 기존 배치 종류 조회

        if (targetType == BuildPlacementType.Floor && existingType == BuildPlacementType.Wall) // 바닥과 벽 조합 확인
        {
            return true; // 바닥과 벽 공존 허용
        }

        if (targetType == BuildPlacementType.Wall && existingType == BuildPlacementType.Floor) // 벽과 바닥 조합 확인
        {
            return true; // 벽과 바닥 공존 허용
        }

        if (targetType == BuildPlacementType.Free && existingType == BuildPlacementType.Floor) // 자유 물체와 바닥 조합 확인
        {
            return true; // 바닥 위 자유 물체 허용
        }

        if (targetType == BuildPlacementType.Wall && existingType == BuildPlacementType.Wall) // 벽끼리 충돌 확인
        {
            Vector2 targetXZ = new Vector2(targetPosition.x, targetPosition.z); // 새 벽 평면 위치 계산
            Vector2 existingXZ = new Vector2(existingObject.transform.position.x, existingObject.transform.position.z); // 기존 벽 평면 위치 계산
            bool hasSamePosition = Vector2.SqrMagnitude(targetXZ - existingXZ) < 0.0025f; // 같은 위치 확인
            float orientationDot = Mathf.Abs(Vector3.Dot(targetRotation * Vector3.right, existingObject.transform.right)); // 방향 일치도 계산
            bool hasSameOrientation = orientationDot > 0.99f; // 같은 방향 확인
            return !hasSamePosition || !hasSameOrientation; // 같은 벽만 차단
        }

        return false; // 나머지 조합 차단
    }

    private bool HasRequiredMaterials() // 전체 설치 재료 보유 여부 확인
    {
        IReadOnlyList<CraftingIngredient> ingredients = currentRecipe.Ingredients; // 필요 재료 목록 조회

        for (int index = 0; index < ingredients.Count; index++) // 전체 재료 순회
        {
            CraftingIngredient ingredient = ingredients[index]; // 현재 재료 조회

            if (ingredient == null || ingredient.ItemData == null) // 재료 설정 확인
            {
                return false; // 잘못된 재료 결과 반환
            }

            if (!playerInventory.HasItem(ingredient.ItemData, ingredient.Amount)) // 필요 수량 확인
            {
                return false; // 재료 부족 반환
            }
        }

        return true; // 모든 재료 보유 반환
    }

    private void TryPlaceStructure() // 건축물 실제 설치 시도
    {
        if (!canPlace || previewInstance == null) // 현재 설치 조건 확인
        {
            return; // 설치 처리 중단
        }

        if (!HasRequiredMaterials()) // 설치 직전 재료 확인
        {
            canPlace = false; // 설치 불가능 상태 적용
            SetPreviewMaterial(false); // 불가능 재질 적용
            RefreshStatus("NEED MATERIALS"); // 재료 부족 문구 표시
            return; // 설치 처리 중단
        }

        List<CraftingIngredient> removedIngredients = new List<CraftingIngredient>(); // 제거 완료 재료 목록
        IReadOnlyList<CraftingIngredient> ingredients = currentRecipe.Ingredients; // 제거 대상 재료 목록

        for (int index = 0; index < ingredients.Count; index++) // 전체 재료 순회
        {
            CraftingIngredient ingredient = ingredients[index]; // 현재 재료 조회
            int removedAmount = playerInventory.RemoveItem(ingredient.ItemData, ingredient.Amount); // 필요 재료 제거

            if (removedAmount != ingredient.Amount) // 예상 수량 제거 실패 확인
            {
                RestoreRemovedIngredients(removedIngredients); // 제거 완료 재료 복구

                if (removedAmount > 0) // 현재 재료 일부 제거 확인
                {
                    playerInventory.AddItem(ingredient.ItemData, removedAmount); // 일부 제거 재료 복구
                }

                Debug.LogError($"{currentRecipe.DisplayName} 설치 중 재료 수량 오류가 발생했습니다.", this); // 재료 오류 출력
                RefreshStatus("MATERIAL ERROR"); // 재료 오류 문구 표시
                return; // 설치 처리 중단
            }

            removedIngredients.Add(ingredient); // 제거 완료 재료 기록
        }

        GameObject placedStructure = Instantiate(
            currentRecipe.PlacedPrefab,
            previewInstance.transform.position,
            previewInstance.transform.rotation,
            placedObjectRoot); // 실제 건축물 생성

        PlacedBuildObject placedBuildObject = placedStructure.GetComponent<PlacedBuildObject>(); // 설치 정보 컴포넌트 조회

        if (placedBuildObject == null) // 설치 정보 컴포넌트 확인
        {
            placedBuildObject = placedStructure.AddComponent<PlacedBuildObject>(); // 설치 정보 컴포넌트 추가
        }

        placedBuildObject.Initialize(currentRecipe); // 건축 데이터 초기화

        bool shouldAttachToConnection = RequiresStructureConnection() || currentConnectionPoint != null; // 구조 연결 실행 여부 계산

        if (shouldAttachToConnection) // 구조 연결 실행 확인
        {
            bool connectionSucceeded = currentConnectionPoint != null
                && placedBuildObject.TryAttachToConnection(currentConnectionPoint); // 구조 연결 시도

            if (!connectionSucceeded) // 연결 처리 실패 확인
            {
                Destroy(placedStructure); // 생성 건축물 제거
                RestoreRemovedIngredients(removedIngredients); // 소비 재료 복구
                canPlace = false; // 설치 불가능 상태 적용
                SetPreviewMaterial(false); // 불가능 재질 적용
                RefreshStatus("CONNECTION FAILED"); // 연결 실패 문구 표시
                return; // 설치 처리 중단
            }
        }

        currentConnectionPoint = null; // 설치 완료 연결점 제거
        lastBuildInputFrame = Time.frameCount; // 설치 프레임 저장
        UpdatePreview(); // 남은 재료와 충돌 상태 갱신
    }

    private void RestoreRemovedIngredients(List<CraftingIngredient> removedIngredients) // 설치 실패 재료 복구
    {
        for (int index = 0; index < removedIngredients.Count; index++) // 제거 완료 재료 순회
        {
            CraftingIngredient ingredient = removedIngredients[index]; // 현재 복구 재료 조회

            if (ingredient == null || ingredient.ItemData == null) // 재료 데이터 확인
            {
                continue; // 잘못된 재료 제외
            }

            int remainingAmount = playerInventory.AddItem(ingredient.ItemData, ingredient.Amount); // 제거 재료 복구

            if (remainingAmount > 0) // 복구 실패 수량 확인
            {
                Debug.LogError($"{ingredient.ItemData.DisplayName} 재료 복구에 실패했습니다.", this); // 복구 실패 오류 출력
            }
        }
    }

    private void SetRemovalMode(bool shouldEnable) // 설치와 철거 모드 전환
    {
        ClearRemovalTarget(); // 기존 철거 대상 강조 해제
        isRemovalMode = shouldEnable; // 새로운 철거 모드 저장
        canPlace = false; // 설치 가능 상태 해제
        currentConnectionPoint = null; // 현재 연결점 제거
        lastBuildInputFrame = Time.frameCount; // 전환 프레임 저장

        if (previewInstance != null) // 기존 미리보기 확인
        {
            Destroy(previewInstance); // 기존 미리보기 제거
        }

        previewInstance = null; // 미리보기 참조 제거
        previewRenderers = null; // 렌더러 참조 제거

        if (isRemovalMode) // 철거 모드 진입 확인
        {
            gridArea.SetGridVisible(false); // 철거 중 그리드 숨김
            RefreshRemovalStatus("REMOVE MODE"); // 철거 상태 표시
            return; // 미리보기 생성 차단
        }

        CreatePreview(); // 설치 미리보기 재생성
        gridArea.SetGridVisible(true); // 설치 그리드 표시
        RefreshStatus("SEARCHING POINTER"); // 설치 상태 표시
    }

    private void UpdateRemovalTarget() // 마우스 포인터 철거 대상 갱신
    {
        Ray removalRay = GetPointerRay(); // 현재 마우스 포인터 위치의 Camera 광선 생성

        bool hasTarget = Physics.Raycast(
            removalRay,
            out RaycastHit removalHit,
            maximumPlacementRayDistance,
            structureLayerMask,
            QueryTriggerInteraction.Ignore); // 포인터 위치 Structure 레이어 탐지

        if (!hasTarget) // 건축물 미탐지 확인
        {
            SetRemovalTarget(null); // 기존 철거 대상 해제
            RefreshRemovalStatus("POINT AT STRUCTURE"); // 대상 탐색 문구 표시
            return; // 갱신 중단
        }

        float playerDistance = Vector3.Distance(playerTransform.position, removalHit.point); // Player와 철거 지점 거리 계산

        if (playerDistance > maximumBuildDistance) // 철거 허용 거리 확인
        {
            SetRemovalTarget(null); // 기존 철거 대상 해제
            RefreshRemovalStatus("TOO FAR FROM PLAYER"); // 철거 거리 초과 문구 표시
            return; // 갱신 중단
        }

        PlacedBuildObject targetObject = removalHit.collider.GetComponentInParent<PlacedBuildObject>(); // 설치 건축물 정보 조회

        if (targetObject == null || targetObject.RecipeData == null) // 유효한 건축물 데이터 확인
        {
            SetRemovalTarget(null); // 기존 철거 대상 해제
            RefreshRemovalStatus("INVALID STRUCTURE"); // 철거 불가능 문구 표시
            return; // 갱신 중단
        }

        SetRemovalTarget(targetObject); // 새로운 철거 대상 적용
        RefreshRemovalStatus("READY TO REMOVE"); // 철거 가능 문구 표시
    }

    private void SetRemovalTarget(PlacedBuildObject newTarget) // 현재 철거 대상 변경
    {
        if (currentRemovalTarget == newTarget) // 동일 철거 대상 확인
        {
            return; // 중복 변경 차단
        }

        if (currentRemovalTarget != null) // 기존 철거 대상 확인
        {
            currentRemovalTarget.SetRemovalHighlight(false, removalTargetMaterial); // 기존 강조 해제
        }

        currentRemovalTarget = newTarget; // 새로운 철거 대상 저장

        if (currentRemovalTarget != null) // 새로운 대상 존재 확인
        {
            currentRemovalTarget.SetRemovalHighlight(true, removalTargetMaterial); // 새로운 강조 적용
        }
    }

    private void ClearRemovalTarget() // 현재 철거 대상 제거
    {
        if (currentRemovalTarget != null) // 기존 철거 대상 확인
        {
            currentRemovalTarget.SetRemovalHighlight(false, removalTargetMaterial); // 기존 강조 해제
        }

        currentRemovalTarget = null; // 철거 대상 참조 제거
    }

    private void TryRemoveCurrentTarget() // 현재 건축물 철거 시도
    {
        if (currentRemovalTarget == null) // 철거 대상 존재 확인
        {
            RefreshRemovalStatus("NO STRUCTURE"); // 철거 대상 없음 표시
            return; // 철거 처리 중단
        }

        if (!TryValidateRemovalGuards(currentRemovalTarget, out string removalFailureStatus)) // 전체 철거 제한 검사
        {
            RefreshRemovalStatus(removalFailureStatus); // 철거 차단 문구 표시
            return; // 철거 처리 중단
        }

        BuildRecipeData targetRecipe = currentRemovalTarget.RecipeData; // 철거 대상 건축 데이터 조회

        if (!TryRefundMaterials(targetRecipe, out string failureStatus)) // 재료 반환 가능 여부 확인
        {
            RefreshRemovalStatus(failureStatus); // 반환 실패 문구 표시
            return; // 철거 처리 중단
        }

        PlacedBuildObject removedObject = currentRemovalTarget; // 제거 대상 참조 저장
        ClearRemovalTarget(); // 제거 전 강조 해제
        Destroy(removedObject.gameObject); // 실제 건축물 제거
        lastBuildInputFrame = Time.frameCount; // 철거 프레임 저장
        RefreshRemovalStatus("STRUCTURE REMOVED"); // 철거 완료 문구 표시
    }

    private bool TryValidateRemovalGuards(
        PlacedBuildObject targetObject,
        out string failureStatus) // 전체 철거 제한 검사
    {
        failureStatus = string.Empty; // 기본 실패 문구 초기화
        MonoBehaviour[] behaviours = targetObject.GetComponentsInChildren<MonoBehaviour>(true); // 하위 컴포넌트 조회

        for (int index = 0; index < behaviours.Length; index++) // 전체 컴포넌트 순회
        {
            MonoBehaviour behaviour = behaviours[index]; // 현재 컴포넌트 조회

            if (!(behaviour is IBuildRemovalGuard removalGuard)) // 철거 제한 인터페이스 확인
            {
                continue; // 일반 컴포넌트 제외
            }

            if (removalGuard.CanRemove) // 철거 가능 여부 확인
            {
                continue; // 철거 가능 규칙 통과
            }

            failureStatus = string.IsNullOrWhiteSpace(removalGuard.RemovalBlockedMessage)
                ? "REMOVAL BLOCKED"
                : removalGuard.RemovalBlockedMessage; // 철거 차단 문구 설정

            return false; // 철거 차단 반환
        }

        return true; // 전체 철거 규칙 통과
    }

    private bool TryRefundMaterials(BuildRecipeData targetRecipe, out string failureStatus) // 철거 재료 반환 시도
    {
        failureStatus = string.Empty; // 기본 실패 문구 제거
        List<ItemData> refundedItems = new List<ItemData>(); // 반환 완료 아이템 목록
        List<int> refundedAmounts = new List<int>(); // 반환 완료 수량 목록
        IReadOnlyList<CraftingIngredient> ingredients = targetRecipe.Ingredients; // 설치 재료 목록 조회

        for (int index = 0; index < ingredients.Count; index++) // 전체 설치 재료 순회
        {
            CraftingIngredient ingredient = ingredients[index]; // 현재 설치 재료 조회

            if (ingredient == null || ingredient.ItemData == null) // 재료 데이터 확인
            {
                RollbackRefund(refundedItems, refundedAmounts); // 기존 반환 재료 회수
                failureStatus = "REFUND DATA ERROR"; // 데이터 오류 문구 설정
                return false; // 재료 반환 실패
            }

            int refundAmount = Mathf.FloorToInt(ingredient.Amount * targetRecipe.DemolitionRefundRatio); // 반환 수량 계산

            if (refundAmount <= 0) // 반환 수량 확인
            {
                continue; // 반환 없는 재료 제외
            }

            int remainingAmount = playerInventory.AddItem(ingredient.ItemData, refundAmount); // 반환 재료 추가
            int addedAmount = refundAmount - remainingAmount; // 실제 추가 수량 계산

            if (addedAmount > 0) // 실제 반환 여부 확인
            {
                refundedItems.Add(ingredient.ItemData); // 반환 아이템 기록
                refundedAmounts.Add(addedAmount); // 반환 수량 기록
            }

            if (remainingAmount > 0) // 인벤토리 공간 부족 확인
            {
                RollbackRefund(refundedItems, refundedAmounts); // 전체 반환 취소
                failureStatus = "INVENTORY FULL"; // 공간 부족 문구 설정
                return false; // 재료 반환 실패
            }
        }

        return true; // 전체 재료 반환 성공
    }

    private void RollbackRefund(List<ItemData> refundedItems, List<int> refundedAmounts) // 반환 재료 회수
    {
        for (int index = 0; index < refundedItems.Count; index++) // 반환 목록 순회
        {
            playerInventory.RemoveItem(refundedItems[index], refundedAmounts[index]); // 반환 재료 다시 제거
        }
    }

    private void SetPreviewUnavailable(string status) // 미리보기 사용 불가 상태 적용
    {
        canPlace = false; // 설치 불가능 상태 적용
        currentConnectionPoint = null; // 사용 불가 연결점 제거

        if (previewInstance != null) // 미리보기 존재 확인
        {
            previewInstance.SetActive(false); // 미리보기 숨김
        }

        RefreshStatus(status); // 현재 실패 상태 표시
    }

    private void SetPreviewMaterial(bool isValid) // 미리보기 재질 변경
    {
        Material targetMaterial = isValid ? validPreviewMaterial : invalidPreviewMaterial; // 적용 재질 선택

        if (previewRenderers == null) // 렌더러 목록 확인
        {
            return; // 재질 처리 중단
        }

        for (int index = 0; index < previewRenderers.Length; index++) // 전체 렌더러 순회
        {
            Renderer targetRenderer = previewRenderers[index]; // 현재 렌더러 조회

            if (targetRenderer == null) // 렌더러 존재 확인
            {
                continue; // 빈 렌더러 제외
            }

            targetRenderer.sharedMaterial = targetMaterial; // 설치 상태 재질 적용
        }
    }

    private void RefreshStatus(string headline) // 건축 상태 문구 갱신
    {
        StringBuilder statusBuilder = new StringBuilder(); // 상태 문구 조합기 생성
        statusBuilder.AppendLine(headline); // 설치 상태 추가
        statusBuilder.AppendLine(currentRecipe.DisplayName); // 건축물 이름 추가
        statusBuilder.AppendLine($"TYPE: {currentRecipe.PlacementType}"); // 배치 종류 추가
        statusBuilder.AppendLine($"ROLE: {currentRecipe.StructureType}"); // 구조 역할 추가

        IReadOnlyList<CraftingIngredient> ingredients = currentRecipe.Ingredients; // 필요 재료 목록 조회

        for (int index = 0; index < ingredients.Count; index++) // 전체 재료 순회
        {
            CraftingIngredient ingredient = ingredients[index]; // 현재 재료 조회

            if (ingredient == null || ingredient.ItemData == null) // 재료 연결 확인
            {
                continue; // 잘못된 재료 제외
            }

            int ownedAmount = playerInventory.GetItemQuantity(ingredient.ItemData); // 현재 보유량 조회
            statusBuilder.AppendLine($"{ingredient.ItemData.DisplayName}: {ownedAmount} / {ingredient.Amount}"); // 보유량 표시
        }

        AppendCameraControls(statusBuilder); // 자유 건축 Camera 공통 조작 안내 추가
        statusBuilder.AppendLine("LMB - PLACE"); // 설치 입력 안내 추가
        statusBuilder.AppendLine("Q / E - ROTATE"); // Preview 회전 안내 추가
        statusBuilder.AppendLine("Z / X - CHANGE STRUCTURE"); // 건축물 선택 안내 추가
        statusBuilder.AppendLine("R - REMOVE MODE"); // 철거 모드 안내 추가
        buildStatusText.SetText(statusBuilder.ToString()); // 완성 상태 문구 표시
    }

    private void RefreshRemovalStatus(string headline) // 철거 상태 문구 갱신
    {
        StringBuilder statusBuilder = new StringBuilder(); // 상태 문구 조합기 생성
        statusBuilder.AppendLine(headline); // 현재 철거 상태 추가

        if (currentRemovalTarget == null || currentRemovalTarget.RecipeData == null) // 유효한 철거 대상 확인
        {
            AppendCameraControls(statusBuilder); // 자유 Camera 조작 안내 추가
            statusBuilder.AppendLine("LMB - REMOVE"); // 철거 입력 안내 추가
            statusBuilder.AppendLine("R - PLACEMENT MODE"); // 설치 모드 안내 추가
            buildStatusText.SetText(statusBuilder.ToString()); // 철거 대기 문구 표시
            return; // 대상 정보 처리 중단
        }

        BuildRecipeData targetRecipe = currentRemovalTarget.RecipeData; // 철거 대상 데이터 조회
        statusBuilder.AppendLine(targetRecipe.DisplayName); // 건축물 이름 추가
        statusBuilder.AppendLine("REFUND"); // 반환 재료 제목 추가

        IReadOnlyList<CraftingIngredient> ingredients = targetRecipe.Ingredients; // 설치 재료 목록 조회

        for (int index = 0; index < ingredients.Count; index++) // 전체 설치 재료 순회
        {
            CraftingIngredient ingredient = ingredients[index]; // 현재 설치 재료 조회

            if (ingredient == null || ingredient.ItemData == null) // 재료 연결 확인
            {
                continue; // 잘못된 재료 제외
            }

            int refundAmount = Mathf.FloorToInt(ingredient.Amount * targetRecipe.DemolitionRefundRatio); // 반환 수량 계산

            if (refundAmount <= 0) // 반환 수량 확인
            {
                continue; // 반환 없는 재료 제외
            }

            statusBuilder.AppendLine($"{ingredient.ItemData.DisplayName}: +{refundAmount}"); // 반환 아이템 표시
        }

        AppendCameraControls(statusBuilder); // 자유 Camera 조작 안내 추가
        statusBuilder.AppendLine("LMB - REMOVE"); // 철거 입력 안내 추가
        statusBuilder.AppendLine("R - PLACEMENT MODE"); // 설치 모드 안내 추가
        buildStatusText.SetText(statusBuilder.ToString()); // 완성 철거 문구 표시
    }

    private void AppendCameraControls(StringBuilder statusBuilder) // 자유 건축 Camera 공통 조작 안내 추가
    {
        statusBuilder.AppendLine("B / ESC - EXIT"); // 건축 종료 안내 추가
        statusBuilder.AppendLine("RMB DRAG - LOOK"); // Camera 회전 안내 추가
        statusBuilder.AppendLine("MMB DRAG - MOVE CAMERA"); // Camera 평행 이동 안내 추가
        statusBuilder.AppendLine("WHEEL - FORWARD / BACK"); // Camera 전후 이동 안내 추가
    }

    private void ResolveReferences() // 누락된 건축 Scene 참조 자동 검색
    {
        if (gameUIManager == null) // 게임 UI 관리자 참조 확인
        {
            gameUIManager = FindFirstObjectByType<GameUIManager>(FindObjectsInactive.Include); // Scene 게임 UI 관리자 검색
        }

        if (worldMapController == null) // 전체 지도 관리자 참조 확인
        {
            worldMapController = FindFirstObjectByType<WorldMapController>(FindObjectsInactive.Include); // Scene 전체 지도 관리자 검색
        }

        if (buildModeCameraController == null) // 자유 건축 Camera 관리자 참조 확인
        {
            buildModeCameraController = FindFirstObjectByType<BuildModeCameraController>(FindObjectsInactive.Include); // Scene 자유 건축 Camera 검색
        }

        if (mainCamera == null) // 기본 플레이어 Camera 참조 확인
        {
            mainCamera = Camera.main; // MainCamera 태그 Camera 연결
        }
    }

    private void OnDisable() // 비활성화 상태 정리
    {
        if (isBuildMode) // 건축 모드 실행 여부 확인
        {
            ExitBuildMode(); // 미리보기, 자유 Camera와 그리드 정리
        }
    }

    private void OnValidate() // Inspector 설정값 검증
    {
        maximumBuildDistance = Mathf.Max(1f, maximumBuildDistance); // Player 기준 최대 건축 거리 최소값 적용
        maximumPlacementRayDistance = Mathf.Max(maximumBuildDistance, maximumPlacementRayDistance); // Camera Ray 거리 최소값 적용
        terrainProbeHeight = Mathf.Max(1f, terrainProbeHeight); // 표본 높이 최소값 적용
        terrainProbeDistance = Mathf.Max(terrainProbeHeight, terrainProbeDistance); // 표본 거리 보정
        collisionPadding = Mathf.Clamp(collisionPadding, 0f, 0.1f); // 충돌 여유 범위 제한
        connectionSnapDistance = Mathf.Max(0.1f, connectionSnapDistance); // 연결 탐지 거리 최소값 적용
    }
}
