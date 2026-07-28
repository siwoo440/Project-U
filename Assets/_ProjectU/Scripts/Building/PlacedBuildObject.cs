using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class PlacedBuildObject : MonoBehaviour // 설치된 건축물 정보
{
    [SerializeField] private BuildPlacementType placementType; // 설치된 건축물 종류

    public BuildPlacementType PlacementType => placementType; // 건축물 종류 제공

    public void Initialize(BuildPlacementType newPlacementType) // 설치 정보 초기화
    {
        placementType = newPlacementType; // 실제 배치 종류 저장
    }
}