using System.Collections.Generic; // 구조 관계 목록 기능
using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
[RequireComponent(typeof(WorldObjectIdentity))] // 월드 고유 ID 컴포넌트 요구
public sealed class PlacedBuildObject : MonoBehaviour, IBuildRemovalGuard // 설치된 건축물 정보
{
    [SerializeField] private BuildRecipeData recipeData; // 설치에 사용된 건축 데이터
    [SerializeField] private BuildPlacementType placementType; // 설치된 건축물 종류
    [SerializeField] private BuildStructureType structureType; // 설치된 구조 역할
    [SerializeField] private PlacedBuildObject supportStructure; // 현재 지지 건축물
    [SerializeField] private BuildConnectionPoint supportConnectionPoint; // 사용 중인 지지 연결점
    [SerializeField] private List<PlacedBuildObject> supportedStructures = new List<PlacedBuildObject>(); // 현재 지지 중인 건축물 목록

    private BuildConnectionPoint[] connectionPoints; // 하위 연결점 목록
    private WorldObjectIdentity worldObjectIdentity; // 월드 고유 ID 컴포넌트

    private Renderer[] cachedRenderers; // 건축물 렌더러 목록
    private Material[][] originalSharedMaterials; // 원래 재질 목록
    private bool isRemovalHighlighted; // 철거 강조 상태

    public BuildRecipeData RecipeData => recipeData; // 건축 데이터 제공
    public BuildPlacementType PlacementType => placementType; // 건축물 종류 제공

    public BuildStructureType StructureType => structureType; // 구조 역할 제공

    public IReadOnlyList<BuildConnectionPoint> ConnectionPoints // 연결점 목록 제공
    {
        get // 연결점 목록 조회
        {
            InitializeConnectionPoints(); // 연결점 목록 준비
            return connectionPoints; // 연결점 목록 반환
        }
    }

    public bool CanRemove // 구조물 철거 가능 여부
    {
        get // 철거 가능 상태 조회
        {
            RemoveMissingSupportedStructures(); // 제거된 건축물 참조 정리
            return supportedStructures.Count == 0; // 지지 중인 구조 없음 확인
        }
    }

    public string RemovalBlockedMessage => CanRemove // 철거 차단 문구 제공
        ? string.Empty // 철거 가능 문구
        : "REMOVE SUPPORTED STRUCTURES FIRST"; // 상위 구조 선철거 안내
    public string StructureId // 설치 건축물 고유 ID 제공
    {
        get // ID 반환 접근자
        {
            WorldObjectIdentity identity = ResolveWorldObjectIdentity(); // ID 컴포넌트 검색
            return identity == null ? string.Empty : identity.WorldObjectId; // ID 또는 빈 값 반환
        }
    }
    private void Awake() // 설치 건축물 초기화
    {
        worldObjectIdentity = GetComponent<WorldObjectIdentity>(); // 월드 고유 ID 컴포넌트 검색
        InitializeConnectionPoints(); // 하위 연결점 초기화
    }
    public void Initialize(BuildRecipeData newRecipeData) // 설치 정보 초기화
    {
        if (newRecipeData == null) // 건축 데이터 존재 확인
        {
            Debug.LogError($"{gameObject.name}의 건축 데이터가 누락되었습니다.", this); // 건축 데이터 오류 출력
            return; // 초기화 중단
        }

        WorldObjectIdentity identity = EnsureWorldObjectIdentity(); // 월드 고유 ID 준비

        if (!identity.HasValidId) // 기존 고유 ID 존재 확인
        {
            identity.GenerateRuntimeId(); // 새 설치 건축물 ID 발급
        }
        recipeData = newRecipeData; // 건축 데이터 저장
        placementType = newRecipeData.PlacementType; // 실제 배치 종류 저장
        structureType = newRecipeData.StructureType; // 실제 구조 역할 저장
        InitializeConnectionPoints(); // 하위 연결점 초기화
        CacheRenderers(); // 건축물 렌더러 저장
    }

    public void RestoreFromSave(
    BuildRecipeData savedRecipeData,
    string savedStructureId,
    Vector3 savedPosition,
    Quaternion savedRotation) // 저장 건축물 상태 복원
    {
        if (savedRecipeData == null) // 건축 데이터 존재 확인
        {
            Debug.LogError($"{gameObject.name}의 복원 건축 데이터가 누락되었습니다.", this); // 건축 데이터 오류 출력
            return; // 복원 중단
        }

        WorldObjectIdentity identity = EnsureWorldObjectIdentity(); // 월드 고유 ID 준비
        identity.AssignWorldObjectId(savedStructureId); // 저장 고유 ID 적용
        recipeData = savedRecipeData; // 저장 건축 데이터 적용
        placementType = savedRecipeData.PlacementType; // 저장 건축 종류 적용
        structureType = savedRecipeData.StructureType; // 저장 구조 역할 적용
        transform.SetPositionAndRotation(savedPosition, savedRotation); // 저장 위치와 회전 적용
        gameObject.SetActive(true); // 건축물 활성화
        InitializeConnectionPoints(); // 하위 연결점 초기화
        CacheRenderers(); // 건축물 렌더러 저장
        gameObject.SetActive(true); // 건축물 활성화
        CacheRenderers(); // 건축물 렌더러 저장
    }
    public bool TryAttachToConnection(BuildConnectionPoint newConnectionPoint) // 구조 연결점 연결 시도
    {
        if (newConnectionPoint == null) // 연결점 존재 확인
        {
            return false; // 연결 실패 반환
        }

        if (newConnectionPoint.Owner == null) // 연결점 소유자 확인
        {
            return false; // 연결 실패 반환
        }

        if (newConnectionPoint.Owner == this) // 자기 자신 연결 확인
        {
            return false; // 자기 연결 차단
        }

        if (!newConnectionPoint.Accepts(structureType)) // 구조 종류 허용 여부 확인
        {
            return false; // 연결 불가 반환
        }

        if (!newConnectionPoint.TryOccupy(this)) // 연결점 사용 시도
        {
            return false; // 연결점 사용 실패 반환
        }

        DetachFromSupport(); // 기존 지지 관계 해제
        supportStructure = newConnectionPoint.Owner; // 지지 건축물 저장
        supportConnectionPoint = newConnectionPoint; // 지지 연결점 저장
        supportStructure.RegisterSupportedStructure(this); // 지지 건축물에 현재 구조 등록
        return true; // 연결 성공 반환
    }

    public void DetachFromSupport() // 기존 지지 관계 해제
    {
        if (supportConnectionPoint != null) // 기존 연결점 존재 확인
        {
            supportConnectionPoint.Release(this); // 연결점 사용 해제
        }

        if (supportStructure != null) // 기존 지지 건축물 존재 확인
        {
            supportStructure.UnregisterSupportedStructure(this); // 지지 목록에서 현재 구조 제거
        }

        supportStructure = null; // 지지 건축물 참조 제거
        supportConnectionPoint = null; // 지지 연결점 참조 제거
    }

    private void RegisterSupportedStructure(PlacedBuildObject supportedStructure) // 지지 중인 구조 등록
    {
        if (supportedStructure == null) // 등록 대상 존재 확인
        {
            return; // 등록 처리 중단
        }

        if (supportedStructures.Contains(supportedStructure)) // 기존 등록 여부 확인
        {
            return; // 중복 등록 차단
        }

        supportedStructures.Add(supportedStructure); // 지지 구조 목록 추가
    }

    private void UnregisterSupportedStructure(PlacedBuildObject supportedStructure) // 지지 중인 구조 해제
    {
        if (supportedStructure == null) // 해제 대상 존재 확인
        {
            return; // 해제 처리 중단
        }

        supportedStructures.Remove(supportedStructure); // 지지 구조 목록 제거
    }

    private void InitializeConnectionPoints() // 하위 연결점 초기화
    {
        if (connectionPoints == null) // 기존 연결점 캐시 확인
        {
            connectionPoints = GetComponentsInChildren<BuildConnectionPoint>(true); // 전체 하위 연결점 검색
        }

        for (int index = 0; index < connectionPoints.Length; index++) // 전체 연결점 순회
        {
            BuildConnectionPoint connectionPoint = connectionPoints[index]; // 현재 연결점 조회

            if (connectionPoint == null) // 연결점 존재 확인
            {
                continue; // 빈 연결점 제외
            }

            connectionPoint.InitializeOwner(this); // 현재 건축물을 소유자로 적용
        }
    }

    private void RemoveMissingSupportedStructures() // 제거된 지지 구조 참조 정리
    {
        for (int index = supportedStructures.Count - 1; index >= 0; index--) // 지지 구조 역순 순회
        {
            if (supportedStructures[index] == null) // 제거된 구조 확인
            {
                supportedStructures.RemoveAt(index); // 빈 참조 제거
            }
        }
    }

    private void OnDestroy() // 건축물 제거 관계 정리
    {
        DetachFromSupport(); // 하위 지지 관계 해제

        for (int index = supportedStructures.Count - 1; index >= 0; index--) // 지지 구조 역순 순회
        {
            PlacedBuildObject supportedStructure = supportedStructures[index]; // 현재 지지 구조 조회

            if (supportedStructure == null) // 구조 존재 확인
            {
                continue; // 제거된 구조 제외
            }

            supportedStructure.DetachFromSupport(); // 상위 구조 지지 관계 해제
        }

        supportedStructures.Clear(); // 지지 목록 초기화
    }

    public void SetRemovalHighlight(bool shouldHighlight, Material highlightMaterial) // 철거 대상 강조 변경
    {
        if (isRemovalHighlighted == shouldHighlight) // 동일 강조 상태 확인
        {
            return; // 중복 변경 차단
        }

        CacheRenderers(); // 렌더러 목록 확인

        if (shouldHighlight) // 강조 활성화 확인
        {
            if (highlightMaterial == null) // 강조 재질 확인
            {
                return; // 재질 누락 시 처리 중단
            }

            CaptureOriginalMaterials(); // 원래 재질 저장

            for (int rendererIndex = 0; rendererIndex < cachedRenderers.Length; rendererIndex++) // 전체 렌더러 순회
            {
                Renderer targetRenderer = cachedRenderers[rendererIndex]; // 현재 렌더러 조회

                if (targetRenderer == null) // 렌더러 존재 확인
                {
                    continue; // 빈 렌더러 제외
                }

                Material[] currentMaterials = targetRenderer.sharedMaterials; // 현재 재질 슬롯 조회
                Material[] highlightedMaterials = new Material[currentMaterials.Length]; // 강조 재질 배열 생성

                for (int materialIndex = 0; materialIndex < highlightedMaterials.Length; materialIndex++) // 전체 재질 슬롯 순회
                {
                    highlightedMaterials[materialIndex] = highlightMaterial; // 강조 재질 적용
                }

                targetRenderer.sharedMaterials = highlightedMaterials; // 렌더러 강조 재질 적용
            }
        }
        else // 강조 비활성화 처리
        {
            RestoreOriginalMaterials(); // 원래 재질 복구
        }

        isRemovalHighlighted = shouldHighlight; // 최종 강조 상태 저장
    }

    private void CacheRenderers() // 건축물 렌더러 저장
    {
        if (cachedRenderers != null && cachedRenderers.Length > 0) // 기존 렌더러 목록 확인
        {
            return; // 중복 조회 차단
        }

        cachedRenderers = GetComponentsInChildren<Renderer>(true); // 전체 자식 렌더러 조회
    }

    private void CaptureOriginalMaterials() // 원래 재질 목록 저장
    {
        if (originalSharedMaterials != null) // 기존 재질 목록 확인
        {
            return; // 중복 저장 차단
        }

        originalSharedMaterials = new Material[cachedRenderers.Length][]; // 렌더러별 재질 배열 생성

        for (int rendererIndex = 0; rendererIndex < cachedRenderers.Length; rendererIndex++) // 전체 렌더러 순회
        {
            Renderer targetRenderer = cachedRenderers[rendererIndex]; // 현재 렌더러 조회

            if (targetRenderer == null) // 렌더러 존재 확인
            {
                originalSharedMaterials[rendererIndex] = new Material[0]; // 빈 재질 배열 저장
                continue; // 다음 렌더러 진행
            }

            originalSharedMaterials[rendererIndex] = targetRenderer.sharedMaterials; // 원래 재질 배열 저장
        }
    }

    private void RestoreOriginalMaterials() // 원래 재질 복구
    {
        if (originalSharedMaterials == null) // 저장된 재질 확인
        {
            return; // 복구 처리 중단
        }

        for (int rendererIndex = 0; rendererIndex < cachedRenderers.Length; rendererIndex++) // 전체 렌더러 순회
        {
            Renderer targetRenderer = cachedRenderers[rendererIndex]; // 현재 렌더러 조회

            if (targetRenderer == null) // 렌더러 존재 확인
            {
                continue; // 빈 렌더러 제외
            }

            targetRenderer.sharedMaterials = originalSharedMaterials[rendererIndex]; // 원래 재질 복구
        }
    }

    private void OnDisable() // 건축물 비활성화 정리
    {
        if (isRemovalHighlighted) // 철거 강조 상태 확인
        {
            RestoreOriginalMaterials(); // 비활성화 전 재질 복구
            isRemovalHighlighted = false; // 강조 상태 해제
        }
    }

    private WorldObjectIdentity ResolveWorldObjectIdentity() // 월드 ID 컴포넌트 검색
    {
        if (worldObjectIdentity == null) // 기존 캐시 확인
        {
            worldObjectIdentity = GetComponent<WorldObjectIdentity>(); // 같은 오브젝트에서 재검색
        }

        return worldObjectIdentity; // 검색 결과 반환
    }

    private WorldObjectIdentity EnsureWorldObjectIdentity() // 월드 ID 컴포넌트 준비
    {
        WorldObjectIdentity identity = ResolveWorldObjectIdentity(); // 기존 컴포넌트 검색

        if (identity == null) // 기존 컴포넌트 없음 확인
        {
            identity = gameObject.AddComponent<WorldObjectIdentity>(); // 실행 중 ID 컴포넌트 추가
            worldObjectIdentity = identity; // 추가 컴포넌트 캐시
        }

        return identity; // 준비된 컴포넌트 반환
    }
}