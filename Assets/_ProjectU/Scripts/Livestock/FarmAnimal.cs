using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class FarmAnimal : MonoBehaviour // 86일차: 우리 안을 천천히 돌아다니는 동물 외형
{
    private enum MoveState // 움직임 단계
    {
        Idle = 0, // 쉬기 (쪼기·풀 뜯기)
        Walk = 1, // 걷기
        Hop = 2 // 기뻐서 폴짝
    }

    private AnimalPen pen; // 소속 우리
    private AnimalData data; // 동물 종류
    private MoveState state = MoveState.Idle; // 현재 단계
    private Vector3 target; // 목표 위치
    private float stateEndTime; // 단계 종료 시각
    private float phase; // 애니메이션 위상
    private Transform body; // 흔들 모델 (자기 자신)
    private Vector3 baseScale; // 원래 크기
    private System.Random random; // 개체별 난수

    public AnimalData Data => data; // 종류 제공
    public AnimalPen Pen => pen; // 우리 제공
    public int Index { get; private set; } // 우리 안 번호

    public void Setup(AnimalPen owner, AnimalData animal, int index) // 초기 설정
    {
        pen = owner; // 우리
        data = animal; // 종류
        Index = index; // 번호
        body = transform; // 모델
        baseScale = transform.localScale; // 크기
        random = new System.Random(owner.GetInstanceID() * 31 + index * 7 + animal.AnimalId.GetHashCode()); // 난수
        phase = Next(0f, 10f); // 위상
        EnterIdle(); // 쉬기로 시작
    }

    public void SetIndex(int index) // 번호 갱신
    {
        Index = index; // 적용
    }

    public void PlayHappy() // 쓰다듬었을 때 폴짝
    {
        state = MoveState.Hop; // 단계
        stateEndTime = Time.time + 0.6f; // 시간
    }

    private void Update() // 움직임
    {
        if (pen == null || data == null) // 설정 확인
        {
            return; // 생략
        }

        phase += Time.deltaTime; // 위상

        switch (state) // 단계 분기
        {
            case MoveState.Walk:
                UpdateWalk(); // 걷기
                break;
            case MoveState.Hop:
                UpdateHop(); // 폴짝
                break;
            default:
                UpdateIdle(); // 쉬기
                break;
        }
    }

    private void UpdateIdle() // 쉬기 : 머리를 숙이는 듯한 기울임
    {
        float peck = Mathf.Max(0f, Mathf.Sin(phase * 5f)) * 8f; // 기울임
        body.localRotation = Quaternion.Euler(0f, body.localEulerAngles.y, 0f) * Quaternion.Euler(peck, 0f, 0f); // 적용
        body.localScale = baseScale; // 크기

        if (Time.time >= stateEndTime) // 쉬기 종료
        {
            state = MoveState.Walk; // 걷기
            target = pen.GetWanderPoint(Next(0f, 1f), Next(0f, 1f)); // 새 목표
            stateEndTime = Time.time + 6f; // 최대 걷기 시간
        }
    }

    private void UpdateWalk() // 걷기 : 목표로 이동하며 통통 튐
    {
        Vector3 position = transform.position; // 현재 위치
        Vector3 flat = target - position; // 목표 방향
        flat.y = 0f; // 수평

        if (flat.sqrMagnitude < 0.01f || Time.time >= stateEndTime) // 도착
        {
            EnterIdle(); // 쉬기
            return; // 완료
        }

        Quaternion look = Quaternion.LookRotation(flat.normalized, Vector3.up); // 바라볼 방향
        transform.rotation = Quaternion.RotateTowards(Quaternion.Euler(0f, transform.eulerAngles.y, 0f), look, 240f * Time.deltaTime); // 회전
        Vector3 next = position + transform.forward * data.WalkSpeed * Time.deltaTime; // 다음 위치
        next.y = target.y; // 바닥 높이

        if (!pen.IsInsideWanderArea(next)) // 영역 밖
        {
            EnterIdle(); // 멈춤
            return; // 완료
        }

        transform.position = next; // 이동
        float bob = Mathf.Abs(Mathf.Sin(phase * data.WalkSpeed * 18f)); // 걸음 흔들림
        body.localScale = new Vector3(baseScale.x, baseScale.y * (1f - bob * 0.04f), baseScale.z); // 눌림
    }

    private void UpdateHop() // 폴짝
    {
        float remaining = Mathf.Clamp01((stateEndTime - Time.time) / 0.6f); // 남은 비율
        float height = Mathf.Sin((1f - remaining) * Mathf.PI) * 0.12f; // 높이
        Vector3 position = transform.position; // 위치
        position.y = pen.transform.position.y + height; // 높이 적용
        transform.position = position; // 적용

        if (remaining <= 0f) // 종료
        {
            position.y = pen.transform.position.y; // 바닥
            transform.position = position; // 적용
            EnterIdle(); // 쉬기
        }
    }

    private void EnterIdle() // 쉬기 시작
    {
        state = MoveState.Idle; // 단계
        stateEndTime = Time.time + Next(1.5f, 4.5f); // 쉬는 시간
    }

    private float Next(float min, float max) // 개체별 난수
    {
        return min + (float)random.NextDouble() * (max - min); // 결과 반환
    }
}
