using System; // GUID 생성 기능
using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 ID 컴포넌트 중복 방지
public sealed class WorldObjectIdentity : MonoBehaviour // 월드 오브젝트 고유 식별자
{
    [Header("Identity")] // 식별 정보 묶음
    [SerializeField] private string worldObjectId = string.Empty; // 저장용 고유 ID

    public string WorldObjectId => worldObjectId; // 현재 고유 ID 제공
    public bool HasValidId => !string.IsNullOrWhiteSpace(worldObjectId); // 유효 ID 존재 여부

    public void AssignWorldObjectId(string newWorldObjectId) // 지정 ID 적용
    {
        worldObjectId = string.IsNullOrWhiteSpace(newWorldObjectId)
            ? string.Empty
            : newWorldObjectId.Trim(); // ID 공백 보정
    }

    public void GenerateRuntimeId() // 실행 중 생성 오브젝트 ID 발급
    {
        worldObjectId = Guid.NewGuid().ToString("N"); // 중복 가능성이 낮은 GUID 생성
    }
}