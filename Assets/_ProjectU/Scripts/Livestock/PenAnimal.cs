using System; // 직렬화 기능
using UnityEngine; // Unity 기본 기능

public enum AnimalMoodLevel // 86일차: 기분 단계
{
    Sad = 0, // 우울 (생산 안 함)
    Content = 1, // 보통
    Happy = 2 // 행복 (추가 생산 가능)
}

[Serializable] // Inspector 표시 허용
public sealed class PenAnimal // 86일차: 우리 안 동물 한 마리의 상태
{
    public const int MaxMood = 100; // 최대 기분

    [SerializeField] private AnimalData data; // 동물 종류
    [SerializeField] private int nameNumber; // 개체 번호 (HEN 1)
    [SerializeField] private int mood; // 기분 0~100
    [SerializeField] private bool fedToday; // 오늘 먹이 먹음
    [SerializeField] private bool pettedToday; // 오늘 쓰다듬음
    [SerializeField] private int daysSinceProduct; // 마지막 생산 후 먹은 날 수
    [SerializeField] private int productReady; // 꺼내기를 기다리는 생산물 수

    public AnimalData Data => data; // 종류 제공
    public int NameNumber => nameNumber; // 번호 제공
    public string DisplayName => data != null ? $"{data.IndividualPrefix} {nameNumber}" : "ANIMAL"; // 개체 이름 제공
    public int Mood => mood; // 기분 제공
    public bool FedToday => fedToday; // 오늘 먹음 제공
    public bool PettedToday => pettedToday; // 오늘 쓰다듬음 제공
    public int DaysSinceProduct => daysSinceProduct; // 생산 경과 제공
    public int ProductReady => productReady; // 생산물 수 제공
    public bool HasProduct => productReady > 0; // 생산물 존재 여부
    public bool IsStorageFull => data != null && productReady >= data.MaxStoredProduct; // 보관 가득 참 여부

    public AnimalMoodLevel MoodLevel // 기분 단계 제공
    {
        get
        {
            if (data == null || mood < data.MinimumProduceMood) return AnimalMoodLevel.Sad; // 우울
            return mood >= data.BonusMood ? AnimalMoodLevel.Happy : AnimalMoodLevel.Content; // 행복·보통
        }
    }

    public int DaysUntilProduct => data == null ? 0 : Mathf.Max(1, data.ProductIntervalDays - daysSinceProduct); // 다음 생산까지 남은 먹은 날 수

    public PenAnimal() // Unity 직렬화용
    {
    }

    public PenAnimal(AnimalData animal, int number) // 새 동물
    {
        data = animal; // 종류
        nameNumber = Mathf.Max(1, number); // 번호
        mood = animal != null ? animal.StartMood : 50; // 시작 기분
    }

    public static PenAnimal FromSave(AnimalData animal, PenAnimalSaveData saved) // 저장 데이터로 만들기
    {
        PenAnimal result = new PenAnimal(animal, saved.nameNumber); // 생성
        result.mood = Mathf.Clamp(saved.mood, 0, MaxMood); // 기분
        result.fedToday = saved.fedToday; // 먹음
        result.pettedToday = saved.pettedToday; // 쓰다듬음
        result.daysSinceProduct = Mathf.Max(0, saved.daysSinceProduct); // 생산 경과
        result.productReady = Mathf.Clamp(saved.productReady, 0, animal.MaxStoredProduct); // 생산물
        return result; // 결과 반환
    }

    public PenAnimalSaveData ToSave() // 저장 데이터 만들기
    {
        return new PenAnimalSaveData
        {
            animalId = data != null ? data.AnimalId : string.Empty,
            nameNumber = nameNumber,
            mood = mood,
            fedToday = fedToday,
            pettedToday = pettedToday,
            daysSinceProduct = daysSinceProduct,
            productReady = productReady
        }; // 결과 반환
    }

    public void MarkFed() // 먹이 먹음
    {
        fedToday = true; // 기록
    }

    public bool TryPet() // 쓰다듬기 (하루 한 번)
    {
        if (pettedToday || data == null) // 이미 쓰다듬음
        {
            return false; // 실패
        }

        pettedToday = true; // 기록
        mood = Mathf.Min(MaxMood, mood + data.PetMoodGain); // 기분 상승
        return true; // 성공
    }

    public int TakeProduct(int amount) // 생산물 꺼내기, 꺼낸 수 반환
    {
        int taken = Mathf.Clamp(amount, 0, productReady); // 꺼낼 수
        productReady -= taken; // 차감
        return taken; // 결과 반환
    }

    public int ProcessDay(bool firstDay, float bonusRoll) // 하루 지나감 처리, 새로 생긴 생산물 수 반환
    {
        if (data == null) // 종류 확인
        {
            return 0; // 처리 생략
        }

        // 여러 날을 건너뛴 경우 첫날만 먹이 기록을 쓰고, 나머지 날은 굶은 날로 계산한다
        bool ate = firstDay && fedToday; // 이 날 먹었는지
        int produced = 0; // 생산 수

        if (ate) // 먹은 날
        {
            mood = Mathf.Min(MaxMood, mood + data.FedMoodGain); // 기분 상승
            daysSinceProduct++; // 생산 경과

            if (daysSinceProduct >= data.ProductIntervalDays && mood >= data.MinimumProduceMood) // 생산 조건
            {
                produced = data.ProductAmount; // 기본 생산

                if (mood >= data.BonusMood && bonusRoll < data.BonusChance) // 추가 생산
                {
                    produced++; // 하나 더
                }

                int space = data.MaxStoredProduct - productReady; // 남은 보관 공간
                produced = Mathf.Clamp(produced, 0, Mathf.Max(0, space)); // 보관 제한
                productReady += produced; // 쌓기
                daysSinceProduct = 0; // 경과 초기화
            }
        }
        else // 굶은 날
        {
            mood = Mathf.Max(0, mood - data.HungryMoodLoss); // 기분 하락
        }

        fedToday = false; // 새 날 먹이 초기화
        pettedToday = false; // 새 날 쓰다듬기 초기화
        return produced; // 결과 반환
    }

    public void DebugSetMood(int value) // 테스트용 기분 설정
    {
        mood = Mathf.Clamp(value, 0, MaxMood); // 적용
    }
}
