using System; // 직렬화 기능
using UnityEngine; // Unity 기본 기능

[Serializable] // Inspector 표시 허용
public sealed class CookingSlot // 85일차: 모닥불 조리 칸 하나의 상태
{
    [Tooltip("조리 중이거나 완성된 요리법. 비어 있으면 빈 칸입니다.")]
    [SerializeField] private CookingRecipeData recipe; // 요리법
    [Tooltip("한 번에 조리하는 묶음 수.")]
    [SerializeField] private int batchCount; // 묶음 수
    [Tooltip("남은 조리 시간.")]
    [SerializeField] private float remainingSeconds; // 남은 시간
    [Tooltip("전체 조리 시간.")]
    [SerializeField] private float totalSeconds; // 전체 시간
    [Tooltip("꺼내기를 기다리는 완성 음식 수량.")]
    [SerializeField] private int readyAmount; // 완성 수량

    public CookingRecipeData Recipe => recipe; // 요리법 제공
    public int BatchCount => batchCount; // 묶음 수 제공
    public float RemainingSeconds => Mathf.Max(0f, remainingSeconds); // 남은 시간 제공
    public float TotalSeconds => totalSeconds; // 전체 시간 제공
    public int ReadyAmount => readyAmount; // 완성 수량 제공
    public bool IsEmpty => recipe == null; // 빈 칸 여부
    public bool IsReady => recipe != null && readyAmount > 0; // 완성 여부
    public bool IsCooking => recipe != null && readyAmount <= 0; // 조리 중 여부
    public float Progress01 => IsReady ? 1f : totalSeconds <= 0f ? 0f : Mathf.Clamp01(1f - remainingSeconds / totalSeconds); // 진행도

    public void Start(CookingRecipeData newRecipe, int count) // 조리 시작
    {
        recipe = newRecipe; // 요리법 저장
        batchCount = Mathf.Max(1, count); // 묶음 수 저장
        totalSeconds = newRecipe.GetCookingSeconds(batchCount); // 전체 시간
        remainingSeconds = totalSeconds; // 남은 시간
        readyAmount = 0; // 완성 없음
    }

    public bool Tick(float deltaTime) // 시간 진행, 이번에 완성되면 true
    {
        if (!IsCooking) // 조리 중 확인
        {
            return false; // 진행 없음
        }

        remainingSeconds -= deltaTime; // 시간 차감

        if (remainingSeconds > 0f) // 남은 시간 확인
        {
            return false; // 계속 조리
        }

        Complete(); // 완성 처리
        return true; // 완성 알림
    }

    public void Complete() // 즉시 완성
    {
        if (recipe == null) // 빈 칸 확인
        {
            return; // 처리 생략
        }

        remainingSeconds = 0f; // 남은 시간 제거
        readyAmount = recipe.ResultQuantity * Mathf.Max(1, batchCount); // 완성 수량
    }

    public void TakeReady(int amount) // 완성 음식 꺼내기
    {
        readyAmount = Mathf.Max(0, readyAmount - Mathf.Max(0, amount)); // 수량 차감

        if (readyAmount <= 0) // 모두 꺼냄 확인
        {
            Clear(); // 빈 칸
        }
    }

    public void Clear() // 빈 칸으로 초기화
    {
        recipe = null; // 요리법 제거
        batchCount = 0; // 묶음 제거
        remainingSeconds = 0f; // 시간 제거
        totalSeconds = 0f; // 전체 시간 제거
        readyAmount = 0; // 완성 제거
    }

    public void Restore(CookingRecipeData savedRecipe, int savedBatch, float savedRemaining, int savedReady) // 저장 상태 적용
    {
        if (savedRecipe == null) // 빈 칸 확인
        {
            Clear(); // 빈 칸
            return; // 적용 완료
        }

        recipe = savedRecipe; // 요리법
        batchCount = Mathf.Max(1, savedBatch); // 묶음 수
        totalSeconds = savedRecipe.GetCookingSeconds(batchCount); // 전체 시간
        remainingSeconds = Mathf.Clamp(savedRemaining, 0f, totalSeconds); // 남은 시간
        readyAmount = Mathf.Max(0, savedReady); // 완성 수량

        if (readyAmount <= 0 && remainingSeconds <= 0f) // 저장 직전에 끝난 조리 확인
        {
            Complete(); // 완성 처리
        }
    }
}
