using System; // 이벤트 기능
using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class PlayerWallet : MonoBehaviour // 87일차: 플레이어 코인 지갑
{
    public const int MaxCoins = 9999999; // 최대 코인

    [Tooltip("현재 코인.")]
    [SerializeField, Min(0)] private int coins; // 현재 코인

    public static PlayerWallet Local { get; private set; } // 현재 플레이어 지갑
    public int Coins => coins; // 코인 제공
    public event Action<int, int> CoinsChanged; // 코인 변경 알림 (현재 값, 변화량)

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] // 도메인 재로드 없는 Play 대비
    private static void ResetStatics() // 정적 상태 초기화
    {
        Local = null; // 해제
    }

    private void OnEnable() // 등록
    {
        Local = this; // 현재 플레이어 등록
    }

    private void OnDisable() // 해제
    {
        if (Local == this) // 현재 지갑 확인
        {
            Local = null; // 해제
        }
    }

    public bool CanAfford(int amount) // 지불 가능 여부
    {
        return amount >= 0 && coins >= amount; // 결과 반환
    }

    public void Add(int amount) // 코인 받기
    {
        if (amount <= 0) // 요청 확인
        {
            return; // 생략
        }

        int before = coins; // 이전 값
        coins = (int)Math.Min((long)coins + amount, MaxCoins); // 상한 적용
        CoinsChanged?.Invoke(coins, coins - before); // 알림
    }

    public bool TrySpend(int amount) // 코인 쓰기
    {
        if (amount < 0 || coins < amount) // 부족 확인
        {
            return false; // 실패
        }

        if (amount == 0) // 공짜
        {
            return true; // 성공
        }

        coins -= amount; // 차감
        CoinsChanged?.Invoke(coins, -amount); // 알림
        return true; // 성공
    }

    public void SetForLoad(int value) // 불러온 코인 적용
    {
        coins = Mathf.Clamp(value, 0, MaxCoins); // 적용
        CoinsChanged?.Invoke(coins, 0); // 알림 (변화량 0 = 연출 없음)
    }

    private void OnValidate() // Inspector 값 보정
    {
        coins = Mathf.Clamp(coins, 0, MaxCoins); // 범위
    }
}
