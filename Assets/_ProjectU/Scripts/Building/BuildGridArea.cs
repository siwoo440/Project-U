using System.Collections.Generic; // 목록 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.Rendering; // 그림자 설정 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class BuildGridArea : MonoBehaviour // 가상 건축 그리드 구역
{
    [Header("Grid")] // 그리드 설정 묶음
    [Tooltip("한 타일 크기.")]
    [SerializeField] private float cellSize = 1f; // 한 타일 크기
    [Tooltip("X 방향 타일 개수.")]
    [SerializeField] private int gridWidth = 30; // X 방향 타일 개수
    [Tooltip("Z 방향 타일 개수.")]
    [SerializeField] private int gridDepth = 30; // Z 방향 타일 개수

    [Header("Terrain")] // Terrain 설정 묶음
    [Tooltip("Terrain 레이어.")]
    [SerializeField] private LayerMask groundLayerMask; // Terrain 레이어
    [Tooltip("높이 탐지 시작 거리.")]
    [SerializeField] private float terrainRayHeight = 25f; // 높이 탐지 시작 거리
    [Tooltip("높이 탐지 전체 거리.")]
    [SerializeField] private float terrainRayDistance = 100f; // 높이 탐지 전체 거리
    [Tooltip("그리드 지면 띄우기.")]
    [SerializeField] private float gridSurfaceOffset = 0.03f; // 그리드 지면 띄우기

    [Header("Visual")] // 그리드 표시 설정 묶음
    [Tooltip("그리드 선 재질.")]
    [SerializeField] private Material gridLineMaterial; // 그리드 선 재질
    [Tooltip("그리드 선 색상.")]
    [SerializeField] private Color gridLineColor = new Color(0f, 1f, 1f, 0.6f); // 그리드 선 색상
    [Tooltip("그리드 선 굵기.")]
    [SerializeField] private float gridLineWidth = 0.02f; // 그리드 선 굵기
    [Tooltip("Terrain 높이 표본 간격.")]
    [SerializeField] private float lineSampleSpacing = 0.5f; // Terrain 높이 표본 간격

    private GameObject gridRoot; // 실행 중 생성된 그리드 루트
    private readonly List<LineRenderer> gridLines = new List<LineRenderer>(); // 생성된 그리드 선 목록

    public float CellSize => cellSize; // 타일 크기 제공
    public int GridWidth => gridWidth; // X 타일 개수 제공
    public int GridDepth => gridDepth; // Z 타일 개수 제공

    private void Awake() // 건축 그리드 초기화
    {
        RebuildGrid(); // Terrain 위 그리드 생성
        SetGridVisible(false); // 시작 시 그리드 숨김
    }

    public bool ContainsWorldPoint(Vector3 worldPoint) // 월드 위치의 건축 구역 포함 여부 확인
    {
        Vector3 localPoint = transform.InverseTransformPoint(worldPoint); // 그리드 로컬 위치 변환
        float maximumX = gridWidth * cellSize; // X 방향 전체 길이
        float maximumZ = gridDepth * cellSize; // Z 방향 전체 길이

        return localPoint.x >= 0f // X 최소 경계 확인
            && localPoint.x < maximumX // X 최대 경계 확인
            && localPoint.z >= 0f // Z 최소 경계 확인
            && localPoint.z < maximumZ; // Z 최대 경계 확인
    }

    public bool TryGetCell(Vector3 worldPoint, out Vector2Int cell) // 월드 위치의 타일 좌표 계산
    {
        cell = Vector2Int.zero; // 기본 타일 좌표 설정

        if (!ContainsWorldPoint(worldPoint)) // 건축 구역 포함 여부 확인
        {
            return false; // 구역 밖 결과 반환
        }

        Vector3 localPoint = transform.InverseTransformPoint(worldPoint); // 그리드 로컬 위치 변환
        int cellX = Mathf.FloorToInt(localPoint.x / cellSize); // X 타일 번호 계산
        int cellZ = Mathf.FloorToInt(localPoint.z / cellSize); // Z 타일 번호 계산
        cell = new Vector2Int(cellX, cellZ); // 최종 타일 좌표 저장
        return true; // 타일 계산 성공 반환
    }

    public Vector3 GetCellCenter(Vector2Int cell) // 타일 중앙 월드 위치 계산
    {
        float localX = (cell.x + 0.5f) * cellSize; // 타일 중앙 X 위치
        float localZ = (cell.y + 0.5f) * cellSize; // 타일 중앙 Z 위치
        Vector3 localPosition = new Vector3(localX, 0f, localZ); // 로컬 중앙 위치 생성
        return transform.TransformPoint(localPosition); // 월드 중앙 위치 반환
    }

    public bool TryGetWallSnap(
        Vector3 worldPoint,
        float localYaw,
        out Vector3 wallPosition,
        out Quaternion wallRotation) // 벽 타일 경계 위치 계산
    {
        wallPosition = Vector3.zero; // 기본 벽 위치 설정
        wallRotation = Quaternion.identity; // 기본 벽 회전 설정

        if (!TryGetCell(worldPoint, out Vector2Int cell)) // 대상 타일 계산
        {
            return false; // 구역 밖 결과 반환
        }

        Vector3 localPoint = transform.InverseTransformPoint(worldPoint); // 로컬 대상 위치 계산
        int quarterTurn = Mathf.RoundToInt(localYaw / 90f); // 90도 회전 횟수 계산
        int orientation = ((quarterTurn % 2) + 2) % 2; // 벽 가로세로 방향 계산
        float snappedYaw = quarterTurn * 90f; // 90도 회전값 생성
        float centerX = (cell.x + 0.5f) * cellSize; // 타일 중앙 X 계산
        float centerZ = (cell.y + 0.5f) * cellSize; // 타일 중앙 Z 계산
        Vector3 localWallPosition; // 로컬 벽 위치 선언

        if (orientation == 0) // X 방향 벽 확인
        {
            float cellStartZ = cell.y * cellSize; // 타일 남쪽 경계 계산
            float positionInsideCell = localPoint.z - cellStartZ; // 타일 내부 Z 위치 계산
            float boundaryZ = positionInsideCell < cellSize * 0.5f // 가까운 Z 경계 확인
                ? cellStartZ // 남쪽 경계 선택
                : cellStartZ + cellSize; // 북쪽 경계 선택
            localWallPosition = new Vector3(centerX, 0f, boundaryZ); // X 방향 벽 위치 생성
        }
        else // Z 방향 벽 처리
        {
            float cellStartX = cell.x * cellSize; // 타일 서쪽 경계 계산
            float positionInsideCell = localPoint.x - cellStartX; // 타일 내부 X 위치 계산
            float boundaryX = positionInsideCell < cellSize * 0.5f // 가까운 X 경계 확인
                ? cellStartX // 서쪽 경계 선택
                : cellStartX + cellSize; // 동쪽 경계 선택
            localWallPosition = new Vector3(boundaryX, 0f, centerZ); // Z 방향 벽 위치 생성
        }

        wallPosition = transform.TransformPoint(localWallPosition); // 월드 벽 위치 계산
        wallRotation = transform.rotation * Quaternion.Euler(0f, snappedYaw, 0f); // 월드 벽 회전 계산
        return true; // 벽 위치 계산 성공 반환
    }

    public void SetGridVisible(bool shouldShow) // 그리드 표시 상태 변경
    {
        if (gridRoot != null) // 그리드 루트 존재 확인
        {
            gridRoot.SetActive(shouldShow); // 그리드 표시 상태 적용
        }
    }

    private void RebuildGrid() // Terrain 위 그리드 선 생성
    {
        if (gridRoot != null) // 기존 그리드 존재 확인
        {
            Destroy(gridRoot); // 기존 그리드 제거
        }

        gridLines.Clear(); // 기존 선 목록 제거
        gridRoot = new GameObject("RuntimeGridLines"); // 그리드 루트 생성
        gridRoot.transform.SetParent(transform, false); // BuildGridArea 자식 연결

        for (int xIndex = 0; xIndex <= gridWidth; xIndex++) // X 경계선 순회
        {
            float localX = xIndex * cellSize; // 현재 X 경계 위치
            Vector3 localStart = new Vector3(localX, 0f, 0f); // X 경계선 시작점
            Vector3 localEnd = new Vector3(localX, 0f, gridDepth * cellSize); // X 경계선 끝점
            CreateGridLine($"GridLine_X_{xIndex}", localStart, localEnd); // X 방향 경계선 생성
        }

        for (int zIndex = 0; zIndex <= gridDepth; zIndex++) // Z 경계선 순회
        {
            float localZ = zIndex * cellSize; // 현재 Z 경계 위치
            Vector3 localStart = new Vector3(0f, 0f, localZ); // Z 경계선 시작점
            Vector3 localEnd = new Vector3(gridWidth * cellSize, 0f, localZ); // Z 경계선 끝점
            CreateGridLine($"GridLine_Z_{zIndex}", localStart, localEnd); // Z 방향 경계선 생성
        }
    }

    private void CreateGridLine(string lineName, Vector3 localStart, Vector3 localEnd) // Terrain 추적 그리드 선 생성
    {
        GameObject lineObject = new GameObject(lineName); // 선 오브젝트 생성
        lineObject.transform.SetParent(gridRoot.transform, false); // 그리드 루트 자식 연결

        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>(); // Line Renderer 추가
        float lineLength = Vector3.Distance(localStart, localEnd); // 선 전체 길이 계산
        int positionCount = Mathf.CeilToInt(lineLength / lineSampleSpacing) + 1; // 선 표본 개수 계산

        lineRenderer.useWorldSpace = true; // 월드 위치 사용
        lineRenderer.positionCount = positionCount; // 선 위치 개수 설정
        lineRenderer.startWidth = gridLineWidth; // 시작 굵기 설정
        lineRenderer.endWidth = gridLineWidth; // 끝 굵기 설정
        lineRenderer.startColor = gridLineColor; // 시작 색상 설정
        lineRenderer.endColor = gridLineColor; // 끝 색상 설정
        lineRenderer.sharedMaterial = gridLineMaterial; // 그리드 재질 적용
        lineRenderer.shadowCastingMode = ShadowCastingMode.Off; // 그림자 생성 해제
        lineRenderer.receiveShadows = false; // 그림자 수신 해제
        lineRenderer.alignment = LineAlignment.View; // 카메라 기준 선 정렬

        for (int positionIndex = 0; positionIndex < positionCount; positionIndex++) // 전체 표본 위치 순회
        {
            float ratio = positionCount <= 1 // 나눗셈 안전성 확인
                ? 0f // 단일 표본 비율
                : positionIndex / (float)(positionCount - 1); // 현재 선 진행 비율
            Vector3 localPosition = Vector3.Lerp(localStart, localEnd, ratio); // 로컬 표본 위치 계산
            Vector3 worldPosition = transform.TransformPoint(localPosition); // 월드 표본 위치 계산
            worldPosition.y = GetTerrainHeight(worldPosition) + gridSurfaceOffset; // Terrain 위 선 높이 적용
            lineRenderer.SetPosition(positionIndex, worldPosition); // 현재 선 위치 저장
        }

        gridLines.Add(lineRenderer); // 생성된 선 목록 등록
    }

    private float GetTerrainHeight(Vector3 worldPosition) // Terrain 표면 높이 조회
    {
        Vector3 rayOrigin = worldPosition + Vector3.up * terrainRayHeight; // Terrain 탐지 시작 위치
        bool hasGround = Physics.Raycast(
            rayOrigin,
            Vector3.down,
            out RaycastHit groundHit,
            terrainRayDistance,
            groundLayerMask,
            QueryTriggerInteraction.Ignore); // Terrain 아래 방향 탐지

        return hasGround // Terrain 탐지 결과 확인
            ? groundHit.point.y // 실제 Terrain 높이 반환
            : transform.position.y; // 탐지 실패 시 구역 기본 높이 반환
    }

    private void OnValidate() // Inspector 설정값 검증
    {
        cellSize = Mathf.Max(0.1f, cellSize); // 타일 크기 최소값 적용
        gridWidth = Mathf.Max(1, gridWidth); // X 타일 개수 최소값 적용
        gridDepth = Mathf.Max(1, gridDepth); // Z 타일 개수 최소값 적용
        terrainRayHeight = Mathf.Max(1f, terrainRayHeight); // 탐지 시작 거리 최소값 적용
        terrainRayDistance = Mathf.Max(terrainRayHeight, terrainRayDistance); // 전체 탐지 거리 보정
        gridSurfaceOffset = Mathf.Max(0f, gridSurfaceOffset); // 지면 보정 음수 방지
        gridLineWidth = Mathf.Max(0.001f, gridLineWidth); // 선 굵기 최소값 적용
        lineSampleSpacing = Mathf.Max(0.1f, lineSampleSpacing); // 표본 간격 최소값 적용
    }
}