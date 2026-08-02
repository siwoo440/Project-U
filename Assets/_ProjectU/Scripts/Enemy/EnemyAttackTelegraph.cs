using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
[RequireComponent(typeof(LineRenderer))] // 공격 범위 표시용 LineRenderer 요구
public sealed class EnemyAttackTelegraph : MonoBehaviour // 적 공격 준비 범위 시각화 관리자
{
    [Header("References")] // 공격 예고 참조 묶음
    [Tooltip("공격 준비 단계와 공격 범위 데이터를 제공할 적 전투 관리자입니다.")] // Inspector 적 전투 관리자 설명
    [SerializeField] private EnemyCombatController combatController; // 적 전투 상태 관리자

    [Tooltip("원형 공격 범위를 표시할 LineRenderer입니다.")] // Inspector LineRenderer 설명
    [SerializeField] private LineRenderer telegraphLine; // 공격 범위 LineRenderer

    [Header("Circle")] // 공격 범위 원형 설정 묶음
    [Tooltip("원형 공격 범위를 구성할 선분 수입니다.")] // Inspector 선분 수 설명
    [SerializeField, Range(12, 128)] private int circleSegments = 48; // 공격 범위 원형 선분 수

    [Tooltip("적 발밑과 겹치지 않도록 선을 올릴 높이입니다.")] // Inspector 지면 높이 보정 설명
    [SerializeField, Min(0f)] private float groundOffset = 0.03f; // 공격 범위 선 높이

    [Tooltip("공격 준비 시작 시 선의 기본 굵기입니다.")] // Inspector 선 굵기 설명
    [SerializeField, Min(0.001f)] private float baseLineWidth = 0.05f; // 공격 예고 기본 선 굵기

    [Tooltip("공격 판정 직전에 선 굵기에 적용할 배율입니다.")] // Inspector 선 굵기 배율 설명
    [SerializeField, Min(1f)] private float imminentWidthMultiplier = 1.8f; // 공격 직전 선 굵기 배율

    [Header("Color")] // 공격 예고 색상 설정 묶음
    [Tooltip("공격 준비 시작 시 사용할 색상입니다.")] // Inspector 시작 색상 설명
    [SerializeField] private Color windupStartColor = new Color(1f, 0.75f, 0f, 0.55f); // 공격 준비 시작 색상

    [Tooltip("공격 판정 직전에 사용할 색상입니다.")] // Inspector 판정 직전 색상 설명
    [SerializeField] private Color windupEndColor = new Color(1f, 0.1f, 0.05f, 0.95f); // 공격 판정 직전 색상

    [Header("Runtime")] // 공격 예고 실행 상태 묶음
    [Tooltip("현재 공격 예고 선을 표시 중인지 확인합니다.")] // Inspector 표시 상태 설명
    [SerializeField] private bool isVisible; // 현재 공격 예고 표시 여부

    [Tooltip("현재 표시 중인 공격 범위 반지름입니다.")] // Inspector 반지름 설명
    [SerializeField] private float currentRadius; // 현재 공격 범위 반지름

    public bool IsVisible => isVisible; // 현재 공격 예고 표시 여부 제공
    public float CurrentRadius => currentRadius; // 현재 공격 범위 반지름 제공

    private void Reset() // 컴포넌트 최초 추가 시 참조 자동 연결
    {
        telegraphLine = GetComponent<LineRenderer>(); // 같은 오브젝트의 LineRenderer 연결
        combatController = GetComponentInParent<EnemyCombatController>(); // 부모 적의 전투 관리자 연결
        ConfigureLineRenderer(); // 기본 LineRenderer 설정 적용
    }

    private void Awake() // 공격 예고 참조 초기화
    {
        if (telegraphLine == null) // LineRenderer 참조 확인
        {
            telegraphLine = GetComponent<LineRenderer>(); // 같은 오브젝트에서 LineRenderer 자동 검색
        }

        if (combatController == null) // 적 전투 관리자 참조 확인
        {
            combatController = GetComponentInParent<EnemyCombatController>(); // 부모 오브젝트에서 적 전투 관리자 자동 검색
        }

        if (telegraphLine == null || combatController == null) // 필수 공격 예고 참조 확인
        {
            Debug.LogError("EnemyAttackTelegraph에 LineRenderer와 EnemyCombatController가 필요합니다.", this); // 필수 참조 누락 오류 출력
            enabled = false; // 공격 예고 기능 비활성화
            return; // 공격 예고 초기화 중단
        }

        ConfigureLineRenderer(); // 실행 시 LineRenderer 설정 적용
        RebuildCircle(); // 적 공격 범위 기준 원형 선 생성
        SetVisible(false); // 시작 공격 예고 숨김
    }

    private void OnEnable() // 적 공격 단계 이벤트 연결
    {
        if (combatController == null) // 적 전투 관리자 참조 확인
        {
            return; // 공격 단계 이벤트 연결 중단
        }

        combatController.AttackPhaseChanged += HandleAttackPhaseChanged; // 적 공격 단계 변경 이벤트 구독
    }

    private void Start() // 모든 적 전투 초기화 후 공격 범위 원 재생성
    {
        RebuildCircle(); // EnemyCombatData 공격 거리 기준 원형 선 갱신
        RefreshVisibility(); // 현재 공격 단계 기준 표시 상태 갱신
    }

    private void Update() // 공격 준비 진행률에 따라 선 색상과 굵기 갱신
    {
        if (!isVisible || combatController == null || telegraphLine == null) // 표시 상태와 참조 확인
        {
            return; // 공격 예고 갱신 생략
        }

        float normalized = combatController.AttackPhaseNormalized; // 현재 공격 준비 진행 비율 조회
        Color currentColor = Color.Lerp(windupStartColor, windupEndColor, normalized); // 준비 진행률 기반 색상 계산
        float currentWidth = baseLineWidth * Mathf.Lerp(1f, imminentWidthMultiplier, normalized); // 준비 진행률 기반 선 굵기 계산
        telegraphLine.startColor = currentColor; // 공격 예고 선 시작 색상 적용
        telegraphLine.endColor = currentColor; // 공격 예고 선 끝 색상 적용
        telegraphLine.startWidth = currentWidth; // 공격 예고 선 시작 굵기 적용
        telegraphLine.endWidth = currentWidth; // 공격 예고 선 끝 굵기 적용
    }

    private void HandleAttackPhaseChanged(EnemyAttackPhase previousPhase, EnemyAttackPhase newPhase) // 적 공격 단계에 따라 예고 표시 전환
    {
        SetVisible(newPhase == EnemyAttackPhase.Windup); // 공격 준비 단계에서만 예고 선 표시
    }

    private void RefreshVisibility() // 현재 적 공격 단계 기준 예고 표시 상태 갱신
    {
        if (combatController == null) // 적 전투 관리자 참조 확인
        {
            SetVisible(false); // 참조가 없으면 공격 예고 숨김
            return; // 표시 상태 갱신 종료
        }

        SetVisible(combatController.CurrentAttackPhase == EnemyAttackPhase.Windup); // 현재 공격 준비 단계 여부 적용
    }

    private void SetVisible(bool shouldBeVisible) // 공격 예고 선 활성 상태 변경
    {
        isVisible = shouldBeVisible; // 현재 공격 예고 표시 상태 저장

        if (telegraphLine != null) // LineRenderer 참조 존재 여부 확인
        {
            telegraphLine.enabled = shouldBeVisible; // LineRenderer 활성 상태 적용
        }
    }

    private void ConfigureLineRenderer() // 공격 범위 원형 LineRenderer 기본 설정
    {
        if (telegraphLine == null) // LineRenderer 참조 확인
        {
            return; // LineRenderer 설정 처리 중단
        }

        telegraphLine.useWorldSpace = false; // 적 자식 기준 로컬 좌표 사용
        telegraphLine.loop = true; // 마지막 점과 첫 점을 연결한 원형 선 사용
        telegraphLine.alignment = LineAlignment.View; // 카메라를 향하는 선 정렬 사용
        telegraphLine.textureMode = LineTextureMode.Stretch; // 선 전체에 Material 텍스처 늘리기
        telegraphLine.startWidth = baseLineWidth; // 기본 시작 선 굵기 적용
        telegraphLine.endWidth = baseLineWidth; // 기본 끝 선 굵기 적용
    }

    private void RebuildCircle() // 적 공격 거리 기준 원형 선 위치 생성
    {
        if (telegraphLine == null || combatController == null) // LineRenderer와 적 전투 관리자 확인
        {
            return; // 원형 선 생성 처리 중단
        }

        EnemyCombatData combatData = combatController.CombatData; // 적 공통 전투 데이터 조회

        if (combatData == null) // 적 전투 데이터 존재 여부 확인
        {
            return; // 공격 범위 원 생성 중단
        }

        currentRadius = combatData.AttackRange; // 현재 공격 범위 반지름 저장
        telegraphLine.positionCount = circleSegments; // 원형 선 위치 개수 적용

        for (int index = 0; index < circleSegments; index++) // 전체 원형 선분 위치 순회
        {
            float normalized = index / (float)circleSegments; // 현재 선분의 원형 진행 비율 계산
            float angle = normalized * Mathf.PI * 2f; // 현재 선분의 원형 각도 계산
            Vector3 localPosition = new Vector3( // 현재 원형 선분 로컬 위치 생성
                Mathf.Cos(angle) * currentRadius, // 원형 X 위치 계산
                groundOffset, // 지면 위 높이 적용
                Mathf.Sin(angle) * currentRadius); // 원형 Z 위치 계산
            telegraphLine.SetPosition(index, localPosition); // 현재 원형 선분 위치 적용
        }
    }

    private void OnDisable() // 적 공격 단계 이벤트와 표시 상태 정리
    {
        if (combatController != null) // 적 전투 관리자 참조 확인
        {
            combatController.AttackPhaseChanged -= HandleAttackPhaseChanged; // 적 공격 단계 변경 이벤트 구독 해제
        }

        SetVisible(false); // 컴포넌트 비활성화 시 공격 예고 숨김
    }

    private void OnValidate() // Inspector 공격 예고 설정값과 참조 검증
    {
        circleSegments = Mathf.Clamp(circleSegments, 12, 128); // 원형 선분 수 범위 제한
        groundOffset = Mathf.Max(0f, groundOffset); // 지면 높이 보정 음수 방지
        baseLineWidth = Mathf.Max(0.001f, baseLineWidth); // 기본 선 굵기 최소값 적용
        imminentWidthMultiplier = Mathf.Max(1f, imminentWidthMultiplier); // 공격 직전 굵기 배율 최소값 적용

        if (telegraphLine == null) // LineRenderer 참조 확인
        {
            telegraphLine = GetComponent<LineRenderer>(); // 같은 오브젝트의 LineRenderer 자동 연결
        }

        if (combatController == null) // 적 전투 관리자 참조 확인
        {
            combatController = GetComponentInParent<EnemyCombatController>(); // 부모 적 전투 관리자 자동 연결
        }

        ConfigureLineRenderer(); // Inspector 변경값 LineRenderer에 적용

        if (!Application.isPlaying) // Edit Mode 여부 확인
        {
            RebuildCircle(); // Edit Mode에서 공격 범위 원형 선 갱신
        }
    }
}
