using System; // 이벤트·문자열 비교 기능
using System.Collections.Generic; // 목록과 Dictionary 기능
using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class FishingJournal : MonoBehaviour // 83일차: 잡은 물고기 기록 (도감 기초)
{
    [Header("Runtime")] // 실행 상태 묶음
    [Tooltip("물고기 종류별 잡은 기록입니다.")]
    [SerializeField] private List<FishCatchSaveData> entries = new List<FishCatchSaveData>(); // 잡은 기록

    private readonly Dictionary<string, FishCatchSaveData> entryById =
        new Dictionary<string, FishCatchSaveData>(StringComparer.Ordinal); // ID별 기록

    public static FishingJournal Local { get; private set; } // 현재 플레이어 기록
    public int SpeciesCount => entries.Count; // 잡아 본 종류 수 제공
    public IReadOnlyList<FishCatchSaveData> Entries => entries; // 기록 목록 제공
    public event Action<FishData, bool> CatchRecorded; // 기록 추가 알림 (새 종류 여부)

    private void Awake() // 기록 준비
    {
        Local = this; // 현재 플레이어 등록
        RebuildLookup(); // 검색 목록 준비
    }

    private void OnDestroy() // 등록 해제
    {
        if (Local == this) // 현재 플레이어 확인
        {
            Local = null; // 등록 해제
        }
    }

    public bool HasCaught(string fishId) // 잡아 본 적 있는지 확인
    {
        return !string.IsNullOrEmpty(fishId) && entryById.ContainsKey(fishId); // 결과 반환
    }

    public int GetCatchCount(string fishId) // 잡은 횟수 조회
    {
        return !string.IsNullOrEmpty(fishId) && entryById.TryGetValue(fishId, out FishCatchSaveData entry) ? entry.caughtCount : 0; // 횟수 반환
    }

    public bool RecordCatch(FishData fish, int day) // 잡은 기록 추가 (처음 잡은 종류면 true)
    {
        if (fish == null || string.IsNullOrEmpty(fish.FishId)) // 물고기 확인
        {
            return false; // 기록 생략
        }

        bool isNew = !entryById.TryGetValue(fish.FishId, out FishCatchSaveData entry); // 새 종류 여부

        if (isNew) // 첫 기록 확인
        {
            entry = new FishCatchSaveData { fishId = fish.FishId, firstCaughtDay = Mathf.Max(0, day) }; // 기록 생성
            entries.Add(entry); // 목록 추가
            entryById.Add(entry.fishId, entry); // 검색 등록
        }

        entry.caughtCount++; // 횟수 증가
        CatchRecorded?.Invoke(fish, isNew); // 알림
        return isNew; // 새 종류 여부 반환
    }

    public List<FishCatchSaveData> CaptureSaveData() // 저장 데이터 생성
    {
        List<FishCatchSaveData> saved = new List<FishCatchSaveData>(entries.Count); // 저장 목록

        for (int index = 0; index < entries.Count; index++) // 기록 순회
        {
            FishCatchSaveData entry = entries[index]; // 현재 기록
            saved.Add(new FishCatchSaveData { fishId = entry.fishId, caughtCount = entry.caughtCount, firstCaughtDay = entry.firstCaughtDay }); // 복사본 추가
        }

        return saved; // 저장 목록 반환
    }

    public void RestoreSaveData(IReadOnlyList<FishCatchSaveData> savedEntries) // 저장 데이터 적용 (null이면 빈 기록)
    {
        entries.Clear(); // 기존 기록 제거

        if (savedEntries != null) // 저장 목록 확인
        {
            for (int index = 0; index < savedEntries.Count; index++) // 저장 목록 순회
            {
                FishCatchSaveData saved = savedEntries[index]; // 현재 항목
                entries.Add(new FishCatchSaveData { fishId = saved.fishId, caughtCount = saved.caughtCount, firstCaughtDay = saved.firstCaughtDay }); // 복사본 추가
            }
        }

        RebuildLookup(); // 검색 목록 갱신
    }

    private void RebuildLookup() // ID 검색 목록 재구성
    {
        entryById.Clear(); // 초기화

        for (int index = entries.Count - 1; index >= 0; index--) // 기록 역순 순회 (잘못된 항목 제거)
        {
            FishCatchSaveData entry = entries[index]; // 현재 기록

            if (entry == null || string.IsNullOrEmpty(entry.fishId) || entryById.ContainsKey(entry.fishId)) // 항목 확인
            {
                entries.RemoveAt(index); // 잘못된 항목 제거
                continue; // 다음 항목
            }

            entryById.Add(entry.fishId, entry); // 검색 등록
        }
    }
}
