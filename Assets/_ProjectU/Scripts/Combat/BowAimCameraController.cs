using UnityEngine; // Unity 기본 기능

[DefaultExecutionOrder(1000)] // 일반 Camera 처리 이후 실행
[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class BowAimCameraController : MonoBehaviour // 활 조준 Camera 확대 관리자
{
    [Header("References")] // Camera 참조 묶음
    [Tooltip("활 조준 확대를 적용할 플레이어 Camera입니다.")] // Inspector Camera 설명
    [SerializeField] private Camera controlledCamera; // 조준 확대 대상 Camera

    [Header("Runtime")] // 실행 상태 확인 묶음
    [Tooltip("현재 활 조준 상태입니다.")] // Inspector 조준 상태 설명
    [SerializeField] private bool isAiming; // 현재 조준 상태

    [Tooltip("현재 장력 비율입니다.")] // Inspector 장력 상태 설명
    [SerializeField, Range(0f, 1f)] private float chargeNormalized; // 현재 장력 비율

    [Tooltip("조준 시작 전에 저장한 Field Of View입니다.")] // Inspector 기본 시야각 설명
    [SerializeField] private float baseFieldOfView; // 조준 전 시야각

    [Tooltip("최대 장력에서 사용할 Field Of View입니다.")] // Inspector 목표 시야각 설명
    [SerializeField] private float maximumChargeFieldOfView = 46f; // 최대 장력 목표 시야각

    private float zoomSpeed = 45f; // Camera 확대 속도
    private float returnSpeed = 80f; // Camera 복귀 속도
    private bool hasCapturedBaseFieldOfView; // 기본 시야각 저장 여부
    private bool isRestoring; // 시야각 복구 진행 여부

    public bool IsAiming => isAiming; // 현재 조준 상태 제공
    public float ChargeNormalized => chargeNormalized; // 현재 장력 비율 제공

    private void Awake() // Camera 참조 초기화
    {
        if (controlledCamera == null) // Camera 참조 확인
        {
            controlledCamera = GetComponent<Camera>(); // 같은 오브젝트에서 Camera 검색
        }

        if (controlledCamera == null) // 같은 오브젝트 검색 결과 확인
        {
            controlledCamera = Camera.main; // Main Camera 대체 검색
        }

        if (controlledCamera == null) // Camera 최종 검색 결과 확인
        {
            Debug.LogError("BowAimCameraController에 Camera를 연결해야 합니다.", this); // Camera 누락 오류 출력
            enabled = false; // 조준 Camera 기능 비활성화
        }
    }

    public void BeginAim( // 메서드 선언
        float targetFieldOfView, // 매개변수 전달
        float aimZoomSpeed, // 매개변수 전달
        float aimReturnSpeed) // 활 조준 확대 시작
    {
        if (!isActiveAndEnabled || controlledCamera == null) // Camera 기능 활성 상태 확인
        {
            return; // 조준 시작 중단
        }

        baseFieldOfView = controlledCamera.fieldOfView; // 조준 직전 시야각 저장
        maximumChargeFieldOfView = Mathf.Clamp(targetFieldOfView, 20f, 100f); // 최대 장력 시야각 저장
        zoomSpeed = Mathf.Max(0.1f, aimZoomSpeed); // 확대 속도 저장
        returnSpeed = Mathf.Max(0.1f, aimReturnSpeed); // 복귀 속도 저장
        chargeNormalized = 0f; // 시작 장력 초기화
        hasCapturedBaseFieldOfView = true; // 기본 시야각 저장 상태 적용
        isRestoring = false; // 기존 복구 상태 해제
        isAiming = true; // 조준 상태 적용
    }

    public void SetCharge(float normalizedCharge) // 현재 장력 비율 적용
    {
        chargeNormalized = Mathf.Clamp01(normalizedCharge); // 장력 비율 범위 제한
    }

    public void EndAim() // 활 조준 확대 종료
    {
        isAiming = false; // 조준 상태 해제
        chargeNormalized = 0f; // 장력 비율 초기화
        isRestoring = hasCapturedBaseFieldOfView; // 기본 시야각 저장 시 복구 시작
    }

    private void LateUpdate() // 일반 Camera 처리 이후 시야각 적용
    {
        if (controlledCamera == null) // Camera 존재 확인
        {
            return; // 시야각 처리 중단
        }

        float unscaledDeltaTime = Time.unscaledDeltaTime; // 일시정지와 무관한 프레임 시간 조회

        if (isAiming) // 현재 조준 상태 확인
        {
            float targetFieldOfView = Mathf.Lerp( // 호출 시작
                baseFieldOfView, // 매개변수 전달
                maximumChargeFieldOfView, // 매개변수 전달
                chargeNormalized); // 장력에 따른 목표 시야각 계산
            controlledCamera.fieldOfView = Mathf.MoveTowards( // 호출 시작
                controlledCamera.fieldOfView, // 매개변수 전달
                targetFieldOfView, // 매개변수 전달
                zoomSpeed * unscaledDeltaTime); // 목표 시야각으로 부드럽게 확대
            return; // 복구 처리 생략
        }

        if (!isRestoring || !hasCapturedBaseFieldOfView) // 시야각 복구 필요 여부 확인
        {
            return; // 복구 처리 중단
        }

        controlledCamera.fieldOfView = Mathf.MoveTowards( // 호출 시작
            controlledCamera.fieldOfView, // 매개변수 전달
            baseFieldOfView, // 매개변수 전달
            returnSpeed * unscaledDeltaTime); // 기존 시야각으로 부드럽게 복구

        if (Mathf.Abs(controlledCamera.fieldOfView - baseFieldOfView) > 0.01f) // 복구 완료 여부 확인
        {
            return; // 다음 프레임 복구 유지
        }

        controlledCamera.fieldOfView = baseFieldOfView; // 최종 기본 시야각 고정
        isRestoring = false; // 복구 상태 해제
        hasCapturedBaseFieldOfView = false; // 기본 시야각 저장 상태 해제
    }

    private void OnDisable() // 컴포넌트 비활성화 처리
    {
        if (controlledCamera != null && hasCapturedBaseFieldOfView) // Camera와 저장값 존재 확인
        {
            controlledCamera.fieldOfView = baseFieldOfView; // 기존 시야각 즉시 복원
        }

        isAiming = false; // 조준 상태 해제
        isRestoring = false; // 복구 상태 해제
        hasCapturedBaseFieldOfView = false; // 기본 시야각 저장 상태 해제
        chargeNormalized = 0f; // 장력 비율 초기화
    }
}
