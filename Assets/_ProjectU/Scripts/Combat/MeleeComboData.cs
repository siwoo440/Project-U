using System.Collections.Generic; // 읽기 전용 목록 기능
using UnityEngine; // Unity 기본 기능

[CreateAssetMenu(
    fileName = "MeleeCombo_New",
    menuName = "Project U/Combat/Melee Combo Data")] // 근접 연속 공격 데이터 생성 메뉴
public sealed class MeleeComboData : ScriptableObject // 아이템별 근접 연속 공격 데이터
{
    [Header("Identity")] // 연속 공격 식별 설정 묶음
    [Tooltip("Inspector와 Debug에서 구분할 연속 공격 이름입니다.")]
    [SerializeField] private string comboName = "MELEE COMBO"; // 연속 공격 이름

    [Header("Combo Rule")] // 연속 공격 규칙 설정 묶음
    [Tooltip("공격 단계가 끝난 뒤 다음 단계 입력을 기다릴 최대 시간입니다.")]
    [SerializeField, Min(0.05f)] private float comboResetDelay = 0.9f; // 연속 공격 초기화 대기시간

    [Tooltip("근접 연속 공격 단계 목록입니다.")]
    [SerializeField] private MeleeAttackStepData[] steps =
    {
        new MeleeAttackStepData(),
        new MeleeAttackStepData(),
        new MeleeAttackStepData()
    }; // 기본 3단 공격 단계

    public string ComboName => string.IsNullOrWhiteSpace(comboName) ? name : comboName; // 연속 공격 이름 제공
    public float ComboResetDelay => Mathf.Max(0.05f, comboResetDelay); // 연속 공격 초기화 시간 제공
    public int StepCount => steps == null ? 0 : steps.Length; // 공격 단계 수 제공
    public IReadOnlyList<MeleeAttackStepData> Steps => steps; // 전체 공격 단계 읽기 전용 제공

    public MeleeAttackStepData GetStep(int stepIndex) // 지정 번호의 공격 단계 조회
    {
        if (steps == null || steps.Length == 0) // 공격 단계 배열 존재 확인
        {
            return null; // 공격 단계 없음 반환
        }

        if (stepIndex < 0 || stepIndex >= steps.Length) // 공격 단계 번호 범위 확인
        {
            return null; // 잘못된 번호 반환
        }

        return steps[stepIndex]; // 지정 공격 단계 반환
    }

    private void OnValidate() // Inspector 연속 공격 데이터 검증
    {
        comboName = string.IsNullOrWhiteSpace(comboName)
            ? name
            : comboName.Trim(); // 연속 공격 이름 공백 정리

        comboResetDelay = Mathf.Max(0.05f, comboResetDelay); // 초기화 시간 최소값 적용

        if (steps == null || steps.Length == 0) // 공격 단계 배열 존재 확인
        {
            steps = new[]
            {
                new MeleeAttackStepData()
            }; // 최소 한 단계 자동 생성
        }

        for (int index = 0; index < steps.Length; index++) // 전체 공격 단계 순회
        {
            if (steps[index] == null) // 빈 공격 단계 확인
            {
                steps[index] = new MeleeAttackStepData(); // 빈 요소에 기본 공격 단계 생성
            }

            steps[index].ValidateValues(index); // 현재 공격 단계 값 검증
        }
    }
}
