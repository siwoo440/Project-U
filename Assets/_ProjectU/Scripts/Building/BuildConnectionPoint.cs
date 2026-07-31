using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class BuildConnectionPoint : MonoBehaviour // 건축 구조 연결점
{
    [Header("Connection")] // 연결 설정 묶음
    [SerializeField] private string connectionPointId = "connection_new"; // 연결점 고유 ID
    [SerializeField] private BuildStructureType[] acceptedStructureTypes = new BuildStructureType[0]; // 연결 가능한 구조 종류

    private PlacedBuildObject owner; // 연결점 소유 건축물
    private PlacedBuildObject connectedObject; // 현재 연결된 건축물

    public string ConnectionPointId => connectionPointId; // 연결점 ID 제공
    public PlacedBuildObject Owner => owner; // 소유 건축물 제공
    public PlacedBuildObject ConnectedObject => connectedObject; // 연결 건축물 제공
    public bool IsOccupied => connectedObject != null; // 연결점 사용 여부 제공
    public Vector3 SnapPosition => transform.position; // 설치 위치 제공
    public Quaternion SnapRotation => transform.rotation; // 설치 회전 제공

    public void InitializeOwner(PlacedBuildObject newOwner) // 연결점 소유자 초기화
    {
        owner = newOwner; // 소유 건축물 저장
    }

    public bool Accepts(BuildStructureType structureType) // 구조 종류 연결 가능 여부 확인
    {
        if (structureType == BuildStructureType.None) // 구조 역할 없음 확인
        {
            return false; // 연결 불가능 반환
        }

        if (acceptedStructureTypes == null) // 허용 목록 존재 확인
        {
            return false; // 연결 불가능 반환
        }

        for (int index = 0; index < acceptedStructureTypes.Length; index++) // 허용 구조 목록 순회
        {
            if (acceptedStructureTypes[index] == structureType) // 같은 구조 종류 확인
            {
                return true; // 연결 가능 반환
            }
        }

        return false; // 일치 구조 없음 반환
    }

    public bool TryOccupy(PlacedBuildObject newConnectedObject) // 연결점 사용 시도
    {
        if (newConnectedObject == null) // 연결 대상 존재 확인
        {
            return false; // 연결 실패 반환
        }

        if (IsOccupied) // 기존 연결 여부 확인
        {
            return false; // 중복 연결 차단
        }

        if (!Accepts(newConnectedObject.StructureType)) // 구조 종류 허용 여부 확인
        {
            return false; // 잘못된 구조 연결 차단
        }

        connectedObject = newConnectedObject; // 연결 건축물 저장
        return true; // 연결 성공 반환
    }

    public void Release(PlacedBuildObject existingConnectedObject) // 연결점 사용 해제
    {
        if (connectedObject != existingConnectedObject) // 현재 연결 대상 확인
        {
            return; // 다른 대상 해제 차단
        }

        connectedObject = null; // 연결 건축물 제거
    }

    private void OnValidate() // Inspector 설정값 검증
    {
        connectionPointId = string.IsNullOrWhiteSpace(connectionPointId) // 연결점 ID 공백 확인
            ? string.Empty // 빈 ID 적용
            : connectionPointId.Trim(); // ID 앞뒤 공백 제거

        if (acceptedStructureTypes == null) // 허용 목록 존재 확인
        {
            acceptedStructureTypes = new BuildStructureType[0]; // 빈 허용 목록 생성
        }
    }
}