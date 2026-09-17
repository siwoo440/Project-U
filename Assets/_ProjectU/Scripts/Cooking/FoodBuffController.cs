using System; // 이벤트 기능
using System.Collections.Generic; // 목록 기능
using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class FoodBuffController : MonoBehaviour // 85일차: 요리 보조 효과 관리 (플레이어)
{
    [Serializable] // Inspector 표시 허용
    public sealed class ActiveBuff // 적용 중인 효과
    {
        public FoodBuffType type; // 종류
        public float strength; // 세기 (%)
        public float remainingSeconds; // 남은 시간
        public float durationSeconds; // 전체 시간
    }

    [Header("Rules")] // 규칙 묶음
    [Tooltip("효과 세기의 최대값 (%).")]
    [SerializeField, Range(10f, 90f)] private float maximumStrength = 80f; // 최대 세기

    [Header("Runtime")] // 실행 상태 묶음
    [Tooltip("적용 중인 효과 목록.")]
    [SerializeField] private List<ActiveBuff> buffs = new List<ActiveBuff>(); // 효과 목록

    public static FoodBuffController Local { get; private set; } // 현재 플레이어 효과
    public IReadOnlyList<ActiveBuff> Buffs => buffs; // 효과 목록 제공
    public event Action BuffsChanged; // 효과 추가·만료 알림

    private void Awake() // 등록
    {
        Local = this; // 현재 플레이어 등록
    }

    private void OnDestroy() // 등록 해제
    {
        if (Local == this) // 현재 플레이어 확인
        {
            Local = null; // 해제
        }
    }

    private void Update() // 남은 시간 진행
    {
        if (buffs.Count == 0) // 효과 확인
        {
            return; // 처리 생략
        }

        bool expired = false; // 만료 여부

        for (int index = buffs.Count - 1; index >= 0; index--) // 역순 순회
        {
            buffs[index].remainingSeconds -= Time.deltaTime; // 시간 차감

            if (buffs[index].remainingSeconds <= 0f) // 만료 확인
            {
                buffs.RemoveAt(index); // 제거
                expired = true; // 기록
            }
        }

        if (expired) // 만료 알림
        {
            BuffsChanged?.Invoke(); // 알림
        }
    }

    public bool Apply(FoodBuffType type, float strength, float durationSeconds) // 효과 적용 (같은 효과는 시간을 새로 채움)
    {
        if (type == FoodBuffType.None || strength <= 0f || durationSeconds <= 0f) // 요청 확인
        {
            return false; // 적용 실패
        }

        float safeStrength = Mathf.Min(strength, maximumStrength); // 세기 제한
        ActiveBuff existing = Find(type); // 기존 효과

        if (existing != null) // 같은 효과 확인
        {
            existing.strength = Mathf.Max(existing.strength, safeStrength); // 더 강한 세기 유지
            existing.durationSeconds = Mathf.Max(durationSeconds, existing.remainingSeconds); // 전체 시간
            existing.remainingSeconds = existing.durationSeconds; // 시간 새로 채움
        }
        else // 새 효과
        {
            buffs.Add(new ActiveBuff { type = type, strength = safeStrength, remainingSeconds = durationSeconds, durationSeconds = durationSeconds }); // 추가
        }

        BuffsChanged?.Invoke(); // 알림
        return true; // 적용 성공
    }

    public void ClearAll() // 모든 효과 제거
    {
        buffs.Clear(); // 초기화
        BuffsChanged?.Invoke(); // 알림
    }

    public float GetStrength(FoodBuffType type) // 효과 세기 (%) 조회
    {
        ActiveBuff buff = Find(type); // 검색
        return buff != null ? buff.strength : 0f; // 결과 반환
    }

    public static float LocalBonusMultiplier(FoodBuffType type) // 증가 배율 (1 + 세기%)
    {
        return Local != null ? 1f + Local.GetStrength(type) / 100f : 1f; // 결과 반환
    }

    public static float LocalReductionMultiplier(FoodBuffType type) // 감소 배율 (1 - 세기%)
    {
        return Local != null ? Mathf.Clamp01(1f - Local.GetStrength(type) / 100f) : 1f; // 결과 반환
    }

    public List<FoodBuffSaveData> CaptureSaveData() // 저장 데이터 생성
    {
        List<FoodBuffSaveData> result = new List<FoodBuffSaveData>(); // 결과

        foreach (ActiveBuff buff in buffs) // 순회
        {
            result.Add(new FoodBuffSaveData { buffType = (int)buff.type, strength = buff.strength, remainingSeconds = buff.remainingSeconds, durationSeconds = buff.durationSeconds }); // 추가
        }

        return result; // 결과 반환
    }

    public void RestoreSaveData(List<FoodBuffSaveData> saved) // 저장 데이터 적용
    {
        buffs.Clear(); // 초기화

        if (saved != null) // 목록 확인
        {
            foreach (FoodBuffSaveData data in saved) // 순회
            {
                FoodBuffType type = (FoodBuffType)data.buffType; // 종류

                if (type == FoodBuffType.None || data.remainingSeconds <= 0f || Find(type) != null) // 잘못된 항목 제외
                {
                    continue; // 제외
                }

                buffs.Add(new ActiveBuff
                {
                    type = type,
                    strength = Mathf.Clamp(data.strength, 0f, maximumStrength),
                    remainingSeconds = data.remainingSeconds,
                    durationSeconds = Mathf.Max(data.durationSeconds, data.remainingSeconds)
                }); // 추가
            }
        }

        BuffsChanged?.Invoke(); // 알림
    }

    private ActiveBuff Find(FoodBuffType type) // 종류로 검색
    {
        foreach (ActiveBuff buff in buffs) // 순회
        {
            if (buff.type == type) // 비교
            {
                return buff; // 결과 반환
            }
        }

        return null; // 없음
    }
}
