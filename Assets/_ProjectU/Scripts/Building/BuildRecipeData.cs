using System.Collections.Generic; // 읽기 전용 목록 기능
using UnityEngine; // Unity 기본 기능

[CreateAssetMenu(fileName = "BuildRecipe_New", menuName = "Project U/Build Recipe Data")] // 건축 데이터 생성 메뉴
public sealed class BuildRecipeData : ScriptableObject // 건축물 제작과 배치 데이터
{
    [Header("Identity")] // 식별 정보 묶음
    [SerializeField] private string recipeId = "structure_new"; // 건축 데이터 고유 ID
    [SerializeField] private string displayName = "NEW STRUCTURE"; // 건축물 표시 이름

    [Header("Structure")] // 구조 설정 묶음
    [SerializeField] private BuildStructureType structureType = BuildStructureType.None; // 건축 구조 역할
    [SerializeField] private bool allowGroundPlacement; // 기능성 가구 Terrain 배치 허용

    [Header("Placement")] // 배치 설정 묶음
    [SerializeField] private BuildPlacementType placementType = BuildPlacementType.Free; // 건축물 배치 종류
    [SerializeField] private float rotationStep = 45f; // 자유 배치 회전 단위
    [SerializeField] private Vector3 previewOffset; // 미리보기 위치 보정

    [Header("Prefabs")] // 프리팹 설정 묶음
    [SerializeField] private GameObject placedPrefab; // 실제 설치 프리팹
    [SerializeField] private GameObject previewPrefab; // 설치 미리보기 프리팹

    [Header("Collision")] // 충돌 검사 설정 묶음
    [SerializeField] private Vector3 placementCheckCenter = new Vector3(0f, 0.5f, 0f); // 충돌 검사 중심
    [SerializeField] private Vector3 placementCheckHalfExtents = new Vector3(0.45f, 0.5f, 0.45f); // 충돌 검사 절반 크기

    [Header("Terrain")] // Terrain 검사 설정 묶음
    [SerializeField] private float maximumSlopeAngle = 20f; // 최대 허용 경사
    [SerializeField] private float maximumHeightDifference = 0.15f; // 배치 영역 최대 높이 차이

    [Header("Ingredients")] // 필요 재료 설정 묶음
    [SerializeField] private CraftingIngredient[] ingredients = new CraftingIngredient[0]; // 설치 필요 재료 목록

    [Header("Removal")] // 철거 설정 묶음
    [SerializeField, Range(0f, 1f)] private float demolitionRefundRatio = 0.5f; // 철거 재료 반환 비율

    public string RecipeId => recipeId; // 건축 데이터 ID 제공
    public string DisplayName => displayName; // 건축물 이름 제공
    public BuildStructureType StructureType => structureType; // 건축 구조 역할 제공
    public bool AllowGroundPlacement => structureType == BuildStructureType.Furniture && allowGroundPlacement; // 가구 지면 배치 허용 제공
    public BuildPlacementType PlacementType => placementType; // 배치 종류 제공
    public float RotationStep => rotationStep; // 회전 단위 제공
    public Vector3 PreviewOffset => previewOffset; // 위치 보정 제공
    public GameObject PlacedPrefab => placedPrefab; // 실제 프리팹 제공
    public GameObject PreviewPrefab => previewPrefab; // 미리보기 프리팹 제공
    public Vector3 PlacementCheckCenter => placementCheckCenter; // 충돌 중심 제공
    public Vector3 PlacementCheckHalfExtents => placementCheckHalfExtents; // 충돌 크기 제공
    public float MaximumSlopeAngle => maximumSlopeAngle; // 최대 경사 제공
    public float MaximumHeightDifference => maximumHeightDifference; // 최대 높이 차이 제공
    public IReadOnlyList<CraftingIngredient> Ingredients => ingredients; // 필요 재료 제공
    public float DemolitionRefundRatio => demolitionRefundRatio; // 철거 반환 비율 제공

    private void OnValidate() // Inspector 설정값 검증
    {
        recipeId = string.IsNullOrWhiteSpace(recipeId) ? string.Empty : recipeId.Trim(); // ID 공백 제거
        displayName = string.IsNullOrWhiteSpace(displayName) ? string.Empty : displayName.Trim(); // 이름 공백 제거
        rotationStep = Mathf.Clamp(rotationStep, 1f, 180f); // 회전 범위 제한
        placementCheckHalfExtents.x = Mathf.Max(0.01f, placementCheckHalfExtents.x); // X 충돌 크기 최소값
        placementCheckHalfExtents.y = Mathf.Max(0.01f, placementCheckHalfExtents.y); // Y 충돌 크기 최소값
        placementCheckHalfExtents.z = Mathf.Max(0.01f, placementCheckHalfExtents.z); // Z 충돌 크기 최소값
        maximumSlopeAngle = Mathf.Clamp(maximumSlopeAngle, 0f, 60f); // 최대 경사 범위 제한
        maximumHeightDifference = Mathf.Max(0f, maximumHeightDifference); // 높이 차이 음수 방지
        demolitionRefundRatio = Mathf.Clamp01(demolitionRefundRatio); // 반환 비율 범위 제한

        if (structureType != BuildStructureType.Furniture) // 기능성 가구 여부 확인
        {
            allowGroundPlacement = false; // 비가구 지면 허용 설정 제거
        }

        if (ingredients == null) // 재료 배열 존재 확인
        {
            ingredients = new CraftingIngredient[0]; // 빈 재료 배열 생성
        }
    }
}
