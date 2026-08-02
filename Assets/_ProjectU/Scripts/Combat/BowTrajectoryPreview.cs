using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
[RequireComponent(typeof(LineRenderer))] // LineRenderer 필수 지정
public sealed class BowTrajectoryPreview : MonoBehaviour // 활 예상 궤적 표시 관리자
{
    [Header("References")] // 참조 설정 묶음
    [Tooltip("예상 궤적을 표시할 LineRenderer입니다.")] // Inspector LineRenderer 설명
    [SerializeField] private LineRenderer lineRenderer; // 예상 궤적 LineRenderer

    [Header("Runtime")] // 실행 상태 확인 묶음
    [Tooltip("현재 예상 궤적 표시 여부입니다.")] // Inspector 표시 상태 설명
    [SerializeField] private bool isVisible; // 현재 궤적 표시 여부

    private readonly RaycastHit[] trajectoryHits = new RaycastHit[16]; // 궤적 충돌 결과 배열
    private Vector3[] trajectoryPoints = new Vector3[35]; // 궤적 위치 배열

    public bool IsVisible => isVisible; // 현재 궤적 표시 여부 제공

    private void Awake() // LineRenderer 참조 초기화
    {
        if (lineRenderer == null) // LineRenderer 참조 확인
        {
            lineRenderer = GetComponent<LineRenderer>(); // 같은 오브젝트에서 자동 검색
        }

        if (lineRenderer == null) // LineRenderer 검색 결과 확인
        {
            Debug.LogError("BowTrajectoryPreview에 LineRenderer가 필요합니다.", this); // LineRenderer 누락 오류 출력
            enabled = false; // 궤적 기능 비활성화
            return; // 초기화 중단
        }

        lineRenderer.useWorldSpace = true; // 월드 좌표 기준 선 표시
        Hide(); // 시작 시 궤적 숨김
    }

    public void ShowTrajectory( // 메서드 선언
        Vector3 startPosition, // 매개변수 전달
        Vector3 launchDirection, // 매개변수 전달
        float launchSpeed, // 매개변수 전달
        bool useGravity, // 매개변수 전달
        float maximumRange, // 매개변수 전달
        float timeStep, // 매개변수 전달
        int pointCount, // 매개변수 전달
        float collisionRadius, // 매개변수 전달
        LayerMask collisionLayers, // 매개변수 전달
        Transform ignoredRoot) // 현재 장력 기준 예상 궤적 표시
    {
        if (!isActiveAndEnabled || lineRenderer == null) // 궤적 기능 활성 상태 확인
        {
            return; // 표시 처리 중단
        }

        int safePointCount = Mathf.Clamp(pointCount, 4, 100); // 지점 수 범위 제한
        EnsurePointCapacity(safePointCount); // 지점 배열 크기 확보
        float safeTimeStep = Mathf.Clamp(timeStep, 0.01f, 0.2f); // 시간 간격 범위 제한
        float safeSpeed = Mathf.Max(0.1f, launchSpeed); // 발사 속도 최소값 적용
        float safeRange = Mathf.Max(0.1f, maximumRange); // 최대 거리 최소값 적용
        float safeRadius = Mathf.Max(0f, collisionRadius); // 충돌 반지름 음수 방지
        Vector3 safeDirection = launchDirection.sqrMagnitude > 0.0001f // 값 계산 시작
            ? launchDirection.normalized // 참 조건 값
            : transform.forward.normalized; // 유효한 발사 방향 계산
        Vector3 initialVelocity = safeDirection * safeSpeed; // 초기 속도 계산
        Vector3 gravity = useGravity ? Physics.gravity : Vector3.zero; // 중력 사용 여부에 따른 가속도 계산
        Vector3 previousPoint = startPosition; // 이전 궤적 지점 초기화
        float travelledDistance = 0f; // 누적 이동 거리 초기화
        int visiblePointCount = 1; // 표시 지점 수 초기화
        trajectoryPoints[0] = startPosition; // 첫 번째 지점 저장

        for (int index = 1; index < safePointCount; index++) // 나머지 궤적 지점 순회
        {
            float elapsedTime = index * safeTimeStep; // 현재 지점까지의 경과 시간 계산
            Vector3 calculatedPoint = startPosition // 값 계산 시작
                + initialVelocity * elapsedTime // 초기 속도 이동량 적용
                + gravity * (0.5f * elapsedTime * elapsedTime); // 중력 가속 이동량 적용
            Vector3 segment = calculatedPoint - previousPoint; // 이전 지점과 현재 지점 사이 선분 계산
            float segmentDistance = segment.magnitude; // 현재 선분 거리 계산

            if (segmentDistance <= 0.0001f) // 유효한 선분 거리 확인
            {
                continue; // 너무 짧은 선분 제외
            }

            float remainingRange = safeRange - travelledDistance; // 남은 최대 거리 계산

            if (remainingRange <= 0f) // 최대 거리 도달 여부 확인
            {
                break; // 궤적 계산 종료
            }

            float allowedDistance = Mathf.Min(segmentDistance, remainingRange); // 이번 선분에서 사용할 거리 계산
            Vector3 segmentDirection = segment / segmentDistance; // 현재 선분 방향 계산
            Vector3 limitedPoint = previousPoint + segmentDirection * allowedDistance; // 최대 거리 제한 지점 계산

            if (TryFindCollision( // 호출 시작
                    previousPoint, // 매개변수 전달
                    segmentDirection, // 매개변수 전달
                    allowedDistance, // 매개변수 전달
                    safeRadius, // 매개변수 전달
                    collisionLayers, // 매개변수 전달
                    ignoredRoot, // 매개변수 전달
                    out Vector3 collisionPoint)) // 현재 선분 충돌 지점 탐색
            {
                trajectoryPoints[visiblePointCount] = collisionPoint; // 충돌 지점 저장
                visiblePointCount++; // 표시 지점 수 증가
                break; // 충돌 이후 궤적 계산 종료
            }

            trajectoryPoints[visiblePointCount] = limitedPoint; // 제한된 현재 지점 저장
            visiblePointCount++; // 표시 지점 수 증가
            travelledDistance += allowedDistance; // 누적 이동 거리 증가
            previousPoint = limitedPoint; // 다음 계산용 이전 지점 갱신

            if (allowedDistance < segmentDistance) // 최대 거리로 선분이 잘렸는지 확인
            {
                break; // 최대 거리 도달로 계산 종료
            }
        }

        lineRenderer.positionCount = visiblePointCount; // 실제 표시 지점 수 적용

        for (int index = 0; index < visiblePointCount; index++) // 표시할 궤적 지점 순회
        {
            lineRenderer.SetPosition(index, trajectoryPoints[index]); // 계산한 궤적 위치 적용
        }

        lineRenderer.enabled = visiblePointCount > 1; // 유효한 선분 존재 시 표시
        isVisible = lineRenderer.enabled; // 현재 표시 상태 저장
    }

    public void Hide() // 예상 궤적 숨김
    {
        isVisible = false; // 표시 상태 해제

        if (lineRenderer == null) // LineRenderer 존재 확인
        {
            return; // 숨김 처리 중단
        }

        lineRenderer.positionCount = 0; // 모든 궤적 지점 제거
        lineRenderer.enabled = false; // LineRenderer 비활성화
    }

    private bool TryFindCollision( // 메서드 선언
        Vector3 origin, // 매개변수 전달
        Vector3 direction, // 매개변수 전달
        float distance, // 매개변수 전달
        float radius, // 매개변수 전달
        LayerMask collisionLayers, // 매개변수 전달
        Transform ignoredRoot, // 매개변수 전달
        out Vector3 collisionPoint) // 현재 궤적 선분의 가장 가까운 충돌 탐색
    {
        collisionPoint = Vector3.zero; // 기본 충돌 지점 초기화
        int hitCount = radius > 0f // 값 계산 시작
            ? Physics.SphereCastNonAlloc( // 호출 시작
                origin, // 매개변수 전달
                radius, // 매개변수 전달
                direction, // 매개변수 전달
                trajectoryHits, // 매개변수 전달
                distance, // 매개변수 전달
                collisionLayers, // 매개변수 전달
                QueryTriggerInteraction.Ignore) // 구체 궤적 충돌 탐색
            : Physics.RaycastNonAlloc( // 호출 시작
                origin, // 매개변수 전달
                direction, // 매개변수 전달
                trajectoryHits, // 매개변수 전달
                distance, // 매개변수 전달
                collisionLayers, // 매개변수 전달
                QueryTriggerInteraction.Ignore); // 선 궤적 충돌 탐색
        float nearestDistance = float.MaxValue; // 가장 가까운 충돌 거리 초기화
        bool hasCollision = false; // 충돌 발견 여부 초기화

        for (int index = 0; index < hitCount; index++) // 충돌 결과 순회
        {
            RaycastHit currentHit = trajectoryHits[index]; // 현재 충돌 정보 조회

            if (currentHit.collider == null) // Collider 존재 확인
            {
                continue; // 잘못된 충돌 결과 제외
            }

            if (ignoredRoot != null // 조건 검사
                && (currentHit.transform == ignoredRoot || currentHit.transform.IsChildOf(ignoredRoot))) // 무시 대상 계층 여부 확인
            {
                continue; // 플레이어 자신의 Collider 제외
            }

            if (currentHit.distance >= nearestDistance) // 기존 충돌보다 가까운지 확인
            {
                continue; // 더 먼 충돌 제외
            }

            nearestDistance = currentHit.distance; // 가장 가까운 거리 갱신
            collisionPoint = currentHit.point; // 가장 가까운 충돌 지점 저장
            hasCollision = true; // 충돌 발견 상태 적용
        }

        return hasCollision; // 충돌 발견 여부 반환
    }

    private void EnsurePointCapacity(int requiredCount) // 궤적 위치 배열 크기 확보
    {
        if (trajectoryPoints != null && trajectoryPoints.Length >= requiredCount) // 기존 배열 크기 확인
        {
            return; // 배열 재생성 생략
        }

        trajectoryPoints = new Vector3[requiredCount]; // 필요한 크기의 새 배열 생성
    }

    private void OnDisable() // 컴포넌트 비활성화 처리
    {
        Hide(); // 비활성화 시 궤적 숨김
    }
}
