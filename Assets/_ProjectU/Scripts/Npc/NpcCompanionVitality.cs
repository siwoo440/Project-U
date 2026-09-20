using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class NpcCompanionVitality : MonoBehaviour // 115일차: 동료 기력 (적이 동료도 공격하고, 기력이 다 떨어지면 지쳐서 집으로 돌아감)
{
    [Tooltip("맞지 않고 이 시간(초)이 지나면 기력이 다시 찹니다.")]
    [SerializeField, Min(0f)] private float recoverDelay = 6f; // 회복 시작 시간
    [Tooltip("초당 회복량.")]
    [SerializeField, Min(0f)] private float recoverPerSecond = 4f; // 초당 회복

    private float maximum = 100f; // 최대 기력
    private float current = 100f; // 지금 기력
    private float lastHitTime = -999f; // 마지막으로 맞은 시각
    private float hurtLineTime; // 아파하는 말풍선 다음 시각

    public static NpcCompanionVitality Active { get; private set; } // 지금 동료 (적이 찾음)
    public float Current => current; // 지금 기력
    public float Maximum => maximum; // 최대 기력
    public float Normalized => maximum > 0f ? current / maximum : 0f; // 기력 비율
    public bool IsAlive => isActiveAndEnabled && current > 0f; // 공격받을 수 있는지
    public int HitsTaken { get; private set; } // 맞은 횟수 (테스트용)

    public static float MaximumFor(NpcCompanionRole role) // 역할별 최대 기력 (전투형이 가장 튼튼)
    {
        switch (role)
        {
            case NpcCompanionRole.Fighter:
                return 160f;
            case NpcCompanionRole.Healer:
                return 110f;
            default:
                return 100f;
        }
    }

    public void Begin(float maximumVitality) // 동료가 되면 가득 채우고 켬
    {
        maximum = Mathf.Max(1f, maximumVitality);
        current = maximum;
        lastHitTime = -999f;
        HitsTaken = 0;
        enabled = true;
        Active = this;
    }

    public void End() // 동료가 끝나면 끔 (적이 더 이상 노리지 않음)
    {
        enabled = false;

        if (Active == this)
        {
            Active = null;
        }
    }

    private void OnDisable()
    {
        if (Active == this)
        {
            Active = null;
        }
    }

    public bool ReceiveDamage(CombatHitData hitData) // 적의 공격 (근접 · 투사체)
    {
        if (!IsAlive || hitData.Damage <= 0f)
        {
            return false;
        }

        current = Mathf.Max(0f, current - hitData.Damage);
        lastHitTime = Time.time;
        HitsTaken++;
        CombatDamagePopup.SpawnText(transform.position + Vector3.up * 2.3f, $"-{Mathf.RoundToInt(hitData.Damage)}", new Color(1f, 0.55f, 0.45f, 1f), 1.4f);
        NpcAgent agent = GetComponent<NpcAgent>();

        if (current <= 0f)
        {
            NpcCompanionManager.Instance?.HandleExhausted(agent); // 지쳐서 집으로
            return true;
        }

        if (agent != null && Time.time >= hurtLineTime && Normalized < 0.35f)
        {
            hurtLineTime = Time.time + 8f;
            agent.Say("헉… 헉…", 1.6f);
        }

        return true;
    }

    private void Update() // 한동안 맞지 않으면 천천히 회복
    {
        if (current < maximum && Time.time - lastHitTime >= recoverDelay)
        {
            current = Mathf.Min(maximum, current + recoverPerSecond * Time.deltaTime);
        }
    }

#if UNITY_EDITOR
    public void EditorSetCurrent(float value) // 테스트용
    {
        current = Mathf.Clamp(value, 0f, maximum);
    }
#endif
}
