using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class PlacedBuildObject : MonoBehaviour // 설치된 건축물 정보
{
    [SerializeField] private BuildRecipeData recipeData; // 설치에 사용된 건축 데이터
    [SerializeField] private BuildPlacementType placementType; // 설치된 건축물 종류

    private Renderer[] cachedRenderers; // 건축물 렌더러 목록
    private Material[][] originalSharedMaterials; // 원래 재질 목록
    private bool isRemovalHighlighted; // 철거 강조 상태

    public BuildRecipeData RecipeData => recipeData; // 건축 데이터 제공
    public BuildPlacementType PlacementType => placementType; // 건축물 종류 제공

    public void Initialize(BuildRecipeData newRecipeData) // 설치 정보 초기화
    {
        if (newRecipeData == null) // 건축 데이터 존재 확인
        {
            Debug.LogError($"{gameObject.name}의 건축 데이터가 누락되었습니다.", this); // 건축 데이터 오류 출력
            return; // 초기화 중단
        }

        recipeData = newRecipeData; // 건축 데이터 저장
        placementType = newRecipeData.PlacementType; // 실제 배치 종류 저장
        CacheRenderers(); // 건축물 렌더러 저장
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
}