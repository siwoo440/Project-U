using System.Collections.Generic; // 목록 기능
using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class FishingSpot : MonoBehaviour // 찌를 던질 수 있는 원형 물 영역
{
    private static readonly List<FishingSpot> activeSpots = new List<FishingSpot>(); // 활성 낚시터 목록

    [Tooltip("낚시터 고유 ID입니다.")]
    [SerializeField] private string spotId = "spot_new"; // 낚시터 ID

    [Tooltip("물가 종류입니다. 출현 물고기 판정에 사용합니다.")]
    [SerializeField] private WaterBodyType waterBodyType = WaterBodyType.Lake; // 물가 종류

    [Tooltip("찌를 던질 수 있는 물의 반지름(m, 로컬)입니다.")]
    [SerializeField, Min(0.5f)] private float radius = 3f; // 물 반지름

    [Tooltip("물 표면의 로컬 높이입니다.")]
    [SerializeField] private float surfaceHeight = 0.12f; // 물 표면 높이

    public string SpotId => spotId; // ID 제공
    public WaterBodyType WaterBodyType => waterBodyType; // 물가 종류 제공
    public float WorldRadius => radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z); // 월드 반지름 제공

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] // 도메인 재로드 없는 Play 대비
    private static void ResetStatics() // 정적 목록 초기화
    {
        activeSpots.Clear(); // 목록 비우기
    }

    private void OnEnable() // 활성 목록 등록
    {
        activeSpots.Add(this); // 등록
    }

    private void OnDisable() // 활성 목록 해제
    {
        activeSpots.Remove(this); // 해제
    }

    public bool ContainsPoint(Vector3 worldPoint) // 수평 위치가 물 안인지 확인
    {
        Vector3 offset = worldPoint - transform.position; // 중심 차이
        offset.y = 0f; // 수평만 사용
        return offset.sqrMagnitude <= WorldRadius * WorldRadius; // 반지름 안 여부 반환
    }

    public Vector3 GetSurfacePoint(Vector3 worldPoint) // 지정 위치의 물 표면 좌표
    {
        float surfaceY = transform.TransformPoint(new Vector3(0f, surfaceHeight, 0f)).y; // 표면 높이
        return new Vector3(worldPoint.x, surfaceY, worldPoint.z); // 표면 좌표 반환
    }

    public static bool TryFind(Vector3 worldPoint, out FishingSpot spot) // 지정 위치의 낚시터 검색
    {
        for (int index = 0; index < activeSpots.Count; index++) // 활성 낚시터 순회
        {
            if (activeSpots[index].ContainsPoint(worldPoint)) // 포함 확인
            {
                spot = activeSpots[index]; // 결과 저장
                return true; // 검색 성공
            }
        }

        spot = null; // 결과 없음
        return false; // 검색 실패
    }

    private void OnDrawGizmosSelected() // 물 영역 표시
    {
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.8f); // 표시 색상
        Vector3 center = transform.TransformPoint(new Vector3(0f, surfaceHeight, 0f)); // 표면 중심
        const int segments = 32; // 원 분할 수
        Vector3 previous = center + new Vector3(WorldRadius, 0f, 0f); // 시작점

        for (int index = 1; index <= segments; index++) // 원 둘레 순회
        {
            float angle = index * Mathf.PI * 2f / segments; // 각도
            Vector3 next = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * WorldRadius; // 다음 점
            Gizmos.DrawLine(previous, next); // 선 그리기
            previous = next; // 이전 점 갱신
        }
    }
}
