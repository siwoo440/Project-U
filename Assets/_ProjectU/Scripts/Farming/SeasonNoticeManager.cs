using System.Linq; // 목록 계산
using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class SeasonNoticeManager : MonoBehaviour // 119일차: 계절이 바뀌면 숲에 무엇이 났는지 알려 준다
{
    private SeasonCycle seasons; // 계절
    private Transform player; // 플레이어
    private SeasonType lastSeason; // 마지막으로 본 계절
    private bool started; // 첫 확인을 지났는지
    private float nextCheckTime; // 다음 확인 시각

    public static SeasonNoticeManager Instance { get; private set; } // Scene 관리자
    public string LastNotice { get; private set; } = string.Empty; // 마지막 알림 문구 (테스트용)
    public int NoticeCount { get; private set; } // 알린 횟수 (테스트용)

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update() // 0.5초마다 계절을 확인한다
    {
        if (Time.time < nextCheckTime)
        {
            return;
        }

        nextCheckTime = Time.time + 0.5f;

        if (seasons == null)
        {
            seasons = FindFirstObjectByType<SeasonCycle>();

            if (seasons == null)
            {
                return;
            }
        }

        if (!started)
        {
            started = true;
            lastSeason = seasons.CurrentSeason;
            return;
        }

        if (lastSeason == seasons.CurrentSeason)
        {
            return;
        }

        lastSeason = seasons.CurrentSeason;
        Announce(lastSeason);
    }

    public void Announce(SeasonType season) // 계절 알림 띄우기 (테스트에서도 사용)
    {
        LastNotice = BuildNotice(season);
        NoticeCount++;
        Debug.Log(LastNotice, this);

        if (player == null)
        {
            PlayerInventory inventory = FindFirstObjectByType<PlayerInventory>();
            player = inventory != null ? inventory.transform : null;
        }

        if (player != null)
        {
            CombatDamagePopup.SpawnText(player.position + Vector3.up * 2.6f, LastNotice, SeasonColor(season), 2.2f);
        }
    }

    private static string BuildNotice(SeasonType season) // 계절마다 나는 것을 문구로
    {
        switch (season)
        {
            case SeasonType.Spring: return "봄이 왔어요. 숲에 산나물과 들꽃, 죽순이 났어요.";
            case SeasonType.Summer: return "여름이 왔어요. 산딸기와 약초 잎, 대나무를 모을 수 있어요.";
            case SeasonType.Autumn: return "가을이 왔어요. 밤과 큰 버섯, 도토리가 떨어졌어요.";
            default: return "겨울이 왔어요. 마른 가지와 얼음꽃, 솔방울을 주울 수 있어요.";
        }
    }

    private static Color SeasonColor(SeasonType season) // 계절 색
    {
        switch (season)
        {
            case SeasonType.Spring: return new Color(0.68f, 0.92f, 0.6f, 1f);
            case SeasonType.Summer: return new Color(0.55f, 0.85f, 1f, 1f);
            case SeasonType.Autumn: return new Color(1f, 0.74f, 0.42f, 1f);
            default: return new Color(0.82f, 0.92f, 1f, 1f);
        }
    }

    public int CountInSeason() // 지금 제철인 채집물 수 (테스트용)
    {
        if (seasons == null)
        {
            seasons = FindFirstObjectByType<SeasonCycle>();
        }

        if (seasons == null)
        {
            return 0;
        }

        return FindObjectsByType<SeasonForage>(FindObjectsInactive.Include, FindObjectsSortMode.None).Count(item => item.IsInSeason(seasons.CurrentSeason));
    }
}
