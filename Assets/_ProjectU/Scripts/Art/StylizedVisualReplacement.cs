using UnityEngine;

// 78일차: 저폴리 모델로 교체하면서 비운 기존 임시 메시를 기억한다.
// Editor 메뉴의 "원래 외형 복구"에서 이 기록을 사용한다. 실행 중에는 아무 동작도 하지 않는다.
[DisallowMultipleComponent]
public sealed class StylizedVisualReplacement : MonoBehaviour
{
    [SerializeField] private string modelId = string.Empty;
    [SerializeField] private GameObject generatedVisual;
    [SerializeField] private MeshFilter[] hiddenMeshFilters = new MeshFilter[0];
    [SerializeField] private Mesh[] originalMeshes = new Mesh[0];

    public string ModelId => modelId;
    public GameObject GeneratedVisual => generatedVisual;
    public MeshFilter[] HiddenMeshFilters => hiddenMeshFilters;
    public Mesh[] OriginalMeshes => originalMeshes;

    public void Record(string newModelId, GameObject newGeneratedVisual, MeshFilter[] filters, Mesh[] meshes)
    {
        modelId = newModelId;
        generatedVisual = newGeneratedVisual;
        hiddenMeshFilters = filters ?? new MeshFilter[0];
        originalMeshes = meshes ?? new Mesh[0];
    }
}
