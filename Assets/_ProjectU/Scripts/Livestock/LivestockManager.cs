using System; // 이벤트·문자열 비교 기능
using System.Collections.Generic; // 목록 기능
using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class LivestockManager : MonoBehaviour // 86일차: 우리 전체의 날짜 처리와 동물 종류 검색
{
    [Header("Time")] // 시간 참조
    [Tooltip("현재 날짜를 제공하는 낮밤 순환입니다.")]
    [SerializeField] private DayNightCycle dayNightCycle; // 날짜 제공

    [Header("Data")] // 데이터
    [Tooltip("저장 파일에서 동물 ID를 찾을 때 쓰는 동물 종류 목록입니다.")]
    [SerializeField] private AnimalData[] animals = new AnimalData[0]; // 동물 종류

    [Header("Drop")] // 바닥 드롭
    [Tooltip("가방에 넣지 못한 생산물을 떨어뜨릴 때 사용할 Pickup Registry입니다.")]
    [SerializeField] private WorldItemPickupRegistry pickupRegistry; // 월드 아이템 Registry
    [Tooltip("바닥에 떨어진 생산물을 모아 둘 부모입니다.")]
    [SerializeField] private WorldItemDropContainer dropContainer; // 드롭 부모

    private static readonly List<AnimalPen> activePens = new List<AnimalPen>(); // 활성 우리 목록
    private static LivestockManager instance; // 현재 Scene 관리자
    private int lastKnownDay = int.MinValue; // 마지막 확인 날짜

    public static LivestockManager Instance // 현재 관리자 제공
    {
        get
        {
            if (instance == null) // 등록 전 조회
            {
                instance = FindFirstObjectByType<LivestockManager>(); // Scene 검색
            }

            return instance; // 결과 반환
        }
    }

    public static IReadOnlyList<AnimalPen> ActivePens => activePens; // 활성 우리 제공
    public IReadOnlyList<AnimalData> Animals => animals; // 동물 종류 제공
    public int CurrentDay => dayNightCycle != null ? dayNightCycle.CurrentDay : 1; // 현재 날짜 제공
    public event Action<int> DayProcessed; // 하루 처리 알림

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] // 도메인 재로드 없는 Play 대비
    private static void ResetStatics() // 정적 상태 초기화
    {
        activePens.Clear(); // 목록 초기화
        instance = null; // 참조 초기화
    }

    public static void Register(AnimalPen pen) // 우리 등록
    {
        if (pen != null && !activePens.Contains(pen)) // 중복 확인
        {
            activePens.Add(pen); // 추가
        }
    }

    public static void Unregister(AnimalPen pen) // 우리 해제
    {
        activePens.Remove(pen); // 제거
    }

    private void Awake() // 초기화
    {
        if (instance != null && instance != this) // 중복 확인
        {
            Debug.LogWarning("LivestockManager가 여러 개 있습니다. 먼저 등록된 관리자를 사용합니다.", this); // 경고
            enabled = false; // 비활성화
            return; // 중단
        }

        instance = this; // 등록

        if (dayNightCycle == null) // 날짜 참조 확인
        {
            dayNightCycle = FindFirstObjectByType<DayNightCycle>(); // Scene 검색
        }

        if (dropContainer == null) // 드롭 부모 확인
        {
            dropContainer = FindFirstObjectByType<WorldItemDropContainer>(); // Scene 검색
        }

        lastKnownDay = CurrentDay; // 시작 날짜
    }

    private void OnDisable() // 해제
    {
        if (instance == this) // 현재 관리자 확인
        {
            instance = null; // 해제
        }
    }

    private void LateUpdate() // 날짜 변경을 프레임 끝에 한 번 처리
    {
        int day = CurrentDay; // 현재 날짜

        if (day == lastKnownDay) // 변경 없음
        {
            return; // 생략
        }

        lastKnownDay = day; // 기록
        ProcessDay(day); // 처리
    }

    public void ProcessDay(int today) // 전체 우리의 지난 날짜 처리
    {
        for (int index = 0; index < activePens.Count; index++) // 우리 순회
        {
            activePens[index].ProcessDays(today); // 날짜 처리
        }

        DayProcessed?.Invoke(today); // 알림
    }

    public void SyncDayForLoad(int loadedDay) // 불러온 날짜를 이미 처리한 날짜로 기록
    {
        lastKnownDay = loadedDay; // 기록
    }

    public bool TryGetAnimal(string animalId, out AnimalData animal) // ID로 동물 종류 검색
    {
        foreach (AnimalData candidate in animals) // 순회
        {
            if (candidate != null && string.Equals(candidate.AnimalId, animalId, StringComparison.Ordinal)) // 비교
            {
                animal = candidate; // 결과
                return true; // 찾음
            }
        }

        animal = null; // 없음
        return false; // 못 찾음
    }

    public bool TryDropItem(ItemData item, int quantity, Vector3 position) // 생산물 바닥 드롭
    {
        return WorldItemDropUtility.TryDrop(pickupRegistry, dropContainer, item, quantity, position, this); // 결과 반환
    }
}
