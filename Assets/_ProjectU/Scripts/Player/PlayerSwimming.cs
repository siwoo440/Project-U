using UnityEngine; // Unity 기본 기능
using UnityEngine.InputSystem; // 새 Input System 기능

public enum PlayerWaterState // 106일차: 플레이어가 물과 닿은 상태
{
    Dry = 0, // 물 밖
    Wading = 1, // 얕은 물을 걸음
    Surface = 2, // 수면에서 헤엄
    Underwater = 3 // 잠수 (머리가 물속)
}

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
[RequireComponent(typeof(CharacterController))] // 이동 충돌체
[RequireComponent(typeof(PlayerStamina))] // 헤엄 스태미나
[RequireComponent(typeof(PlayerHealth))] // 숨 막힘 피해
public sealed class PlayerSwimming : MonoBehaviour // 106일차: 수영 · 잠수 (깊은 물에서 헤엄 · C 잠수 · Space 떠오르기 · 숨 · 파도)
{
    public const string SwimHintMessage = "헤엄치는 중 · C 잠수 · Space 떠오르기"; // 처음 헤엄칠 때 안내
    public const string LowBreathMessage = "숨이 얼마 남지 않았어요!"; // 숨이 적을 때
    public const string DrowningMessage = "숨이 막혀요! 물 위로 올라가세요."; // 숨이 바닥났을 때
    public const string ExhaustedMessage = "너무 지쳤어요. 물 밖에서 쉬어야 해요."; // 헤엄치다 지쳤을 때

    [Header("References")] // 외부 참조 묶음
    [Tooltip("잠수 입력 (C · Crouch 액션). 비어 있으면 C 키를 직접 읽습니다.")]
    [SerializeField] private InputActionReference diveActionReference; // 잠수 입력
    [Tooltip("헤엄칠 때 눕히는 플레이어 외형 (PlayerVisual).")]
    [SerializeField] private Transform visualRoot; // 플레이어 외형
    [Tooltip("헤엄칠 때 숨기는 손 도구 묶음.")]
    [SerializeField] private GameObject toolHolder; // 손 도구
    [Tooltip("물에 들어갈 때 물보라 · 헤엄칠 때 물결.")]
    [SerializeField] private ParticleSystem splashParticles; // 물보라
    [Tooltip("잠수 중 물방울.")]
    [SerializeField] private ParticleSystem bubbleParticles; // 물방울

    [Header("Water")] // 물 깊이 설정 묶음
    [Tooltip("수면 높이 (IslandShoreGuard가 있으면 그 값을 씁니다).")]
    [SerializeField] private float seaLevel = -0.5f; // 수면
    [Tooltip("헤엄칠 때 발이 수면 아래에 있는 깊이 (m). 머리와 어깨가 물 위에 나옵니다.")]
    [SerializeField, Min(0.5f)] private float floatDepth = 1.15f; // 떠 있는 깊이
    [Tooltip("걸어 들어갈 때 발이 이 깊이보다 깊어지면 헤엄치기 시작합니다 (m).")]
    [SerializeField, Min(0.6f)] private float enterDepth = 1.25f; // 헤엄 시작 깊이
    [Tooltip("헤엄치다 발이 이 깊이보다 얕아지면 다시 걷습니다 (m).")]
    [SerializeField, Min(0.2f)] private float exitDepth = 0.9f; // 걷기로 돌아가는 깊이
    [Tooltip("걸을 때 이 깊이부터 물 때문에 느려집니다 (m).")]
    [SerializeField, Min(0f)] private float wadeStartDepth = 0.35f; // 느려지기 시작
    [Tooltip("헤엄 시작 직전 가장 느린 걷기 배율.")]
    [SerializeField, Range(0.2f, 1f)] private float wadeSlowest = 0.62f; // 가장 느린 걷기
    [Tooltip("발에서 숨 쉬는 곳(입)까지 높이 (m).")]
    [SerializeField, Min(0.5f)] private float breathHeight = 1.55f; // 입 높이

    [Header("Speed")] // 헤엄 속도 묶음
    [Tooltip("수면 헤엄 속도.")]
    [SerializeField, Min(0.5f)] private float swimSpeed = 2.6f; // 헤엄
    [Tooltip("Shift 빠른 헤엄 속도 (스태미나를 많이 씀).")]
    [SerializeField, Min(0.5f)] private float fastSwimSpeed = 4.2f; // 빠른 헤엄
    [Tooltip("물속 헤엄 속도.")]
    [SerializeField, Min(0.5f)] private float diveSwimSpeed = 2.3f; // 물속 헤엄
    [Tooltip("C를 누를 때 내려가는 속도.")]
    [SerializeField, Min(0.5f)] private float diveSpeed = 2.4f; // 잠수 속도
    [Tooltip("Space를 누를 때 올라가는 속도.")]
    [SerializeField, Min(0.5f)] private float riseSpeed = 2.8f; // 떠오르기 속도
    [Tooltip("아무것도 누르지 않을 때 천천히 떠오르는 속도.")]
    [SerializeField, Min(0.1f)] private float buoyancy = 0.9f; // 부력
    [Tooltip("수면에서 Space로 물 밖으로 뛰어오르는 속도.")]
    [SerializeField, Min(1f)] private float hopSpeed = 5.2f; // 뛰어오르기
    [Tooltip("지쳤을 때 헤엄 속도 배율.")]
    [SerializeField, Range(0.2f, 1f)] private float exhaustedSpeedMultiplier = 0.6f; // 지친 헤엄

    [Header("Stamina")] // 헤엄 스태미나 묶음
    [Tooltip("수면 헤엄 초당 스태미나.")]
    [SerializeField, Min(0f)] private float swimDrain = 1.5f; // 헤엄 소비
    [Tooltip("빠른 헤엄 초당 스태미나.")]
    [SerializeField, Min(0f)] private float fastSwimDrain = 12f; // 빠른 헤엄 소비
    [Tooltip("물속 헤엄 초당 스태미나.")]
    [SerializeField, Min(0f)] private float diveDrain = 2.5f; // 물속 소비
    [Tooltip("가만히 떠 있을 때 스태미나 회복 배율 (땅 위 회복 대비).")]
    [SerializeField, Range(0f, 1f)] private float treadRecovery = 0.4f; // 떠 있을 때 회복

    [Header("Breath")] // 숨 묶음
    [Tooltip("최대 숨.")]
    [SerializeField, Min(10f)] private float maxBreath = 100f; // 최대 숨
    [Tooltip("물속 초당 숨 소비 (100 ÷ 4 = 25초).")]
    [SerializeField, Min(0.1f)] private float breathDrainPerSecond = 4f; // 숨 소비
    [Tooltip("물 위 초당 숨 회복.")]
    [SerializeField, Min(0.1f)] private float breathRecoveryPerSecond = 40f; // 숨 회복
    [Tooltip("숨이 바닥났을 때 한 번에 받는 피해.")]
    [SerializeField, Min(0f)] private float drownDamage = 8f; // 숨 막힘 피해
    [Tooltip("숨 막힘 피해 간격 (초).")]
    [SerializeField, Min(0.2f)] private float drownInterval = 1.2f; // 피해 간격

    [Header("Wetness")] // 젖음 묶음
    [Tooltip("물에 들어가 있을 때 초당 젖음.")]
    [SerializeField, Min(0f)] private float soakPerSecond = 40f; // 젖음

    [Header("Runtime")] // 실행 상태 묶음
    [SerializeField] private PlayerWaterState state; // 현재 물 상태
    [SerializeField] private float currentBreath = 100f; // 현재 숨

    private CharacterController characterController; // 이동 충돌체
    private PlayerStamina playerStamina; // 스태미나
    private PlayerHealth playerHealth; // 체력
    private PlayerWetness playerWetness; // 젖음
    private Terrain terrain; // 섬 Terrain
    private Quaternion visualBaseRotation; // 외형 기본 회전
    private Vector3 visualBasePosition; // 외형 기본 위치
    private float poseBlend; // 헤엄 자세 비율 (0 서기 ~ 1 헤엄)
    private float posePitch; // 현재 몸 기울기
    private float verticalSpeed; // 헤엄 수직 속도
    private bool moving; // 이번 프레임 헤엄 이동 입력
    private bool touchingBottom; // 지난 헤엄 이동에서 발이 바닥 · 바위에 닿았는지
    private float drownTimer; // 숨 막힘 피해 누적 시간
    private bool lowBreathWarned; // 이번 잠수에서 숨 경고를 했는지
    private bool exhaustedWarned; // 이번 헤엄에서 지침 안내를 했는지
    private bool hintShown; // 헤엄 안내를 했는지 (게임 켤 때마다 한 번)
    private float nextWaveMessageTime; // 파도 말풍선 간격
    private float nextRippleTime; // 헤엄 물결 간격
    private bool toolHolderHidden; // 손 도구를 숨겼는지

    public static PlayerSwimming Local { get; private set; } // 조작 중인 플레이어
    public static bool IsLocalSwimming => Local != null && Local.IsSwimming; // 헤엄 중 (공격 · 건축 · 도구 차단)
    public static bool IsLocalUnderwater => Local != null && Local.IsUnderwater; // 잠수 중 (먹기 차단)

    public PlayerWaterState State => state; // 현재 물 상태
    public bool IsSwimming => state == PlayerWaterState.Surface || state == PlayerWaterState.Underwater; // 헤엄 중
    public bool IsUnderwater => state == PlayerWaterState.Underwater; // 잠수 중
    public float SeaLevel => IslandShoreGuard.Instance != null ? IslandShoreGuard.Instance.SeaLevel : seaLevel; // 수면
    public float FloatDepth => floatDepth; // 떠 있는 깊이
    public float EnterDepth => enterDepth; // 헤엄 시작 깊이
    public float FastSwimSpeed => fastSwimSpeed; // 가장 빠른 헤엄
    public float Breath => currentBreath; // 현재 숨
    public float MaxBreath => maxBreath; // 최대 숨
    public float NormalizedBreath => currentBreath / maxBreath; // 숨 비율
    public float FeetDepth => SeaLevel - transform.position.y; // 발이 잠긴 깊이
    public float WadeSpeedMultiplier { get; private set; } = 1f; // 얕은 물 걷기 배율
    public Transform VisualRoot => visualRoot; // 외형 (검사용)
    public GameObject ToolHolder => toolHolder; // 손 도구 (검사용)
    public InputActionReference DiveAction => diveActionReference; // 잠수 입력 (검사용)
    public ParticleSystem SplashParticles => splashParticles; // 물보라 (검사용)
    public ParticleSystem BubbleParticles => bubbleParticles; // 물방울 (검사용)
    public int SwimStartCount { get; private set; } // 헤엄을 시작한 횟수 (테스트용)
    public int DrownTickCount { get; private set; } // 숨 막힘 피해 횟수 (테스트용)
    public float LastWavePush { get; private set; } // 마지막 파도 세기 (테스트용)

    private void Awake() // 준비
    {
        characterController = GetComponent<CharacterController>();
        playerStamina = GetComponent<PlayerStamina>();
        playerHealth = GetComponent<PlayerHealth>();
        playerWetness = GetComponent<PlayerWetness>();
        currentBreath = maxBreath;

        if (visualRoot != null)
        {
            visualBaseRotation = visualRoot.localRotation;
            visualBasePosition = visualRoot.localPosition;
        }
    }

    private void OnEnable() // 입력 · 조작 플레이어 등록
    {
        Local = this;

        if (diveActionReference != null)
        {
            diveActionReference.action.Enable();
        }
    }

    private void OnDisable() // 정리
    {
        if (Local == this)
        {
            Local = null;
        }

        ShowTools(true);
    }

    // ---------------------------------------------------------------- 물 확인

    private float GroundHeight(Vector3 position) // Terrain 높이 (Terrain이 없으면 아주 낮게)
    {
        if (terrain == null)
        {
            terrain = Terrain.activeTerrain != null ? Terrain.activeTerrain : FindFirstObjectByType<Terrain>();
        }

        return terrain != null ? terrain.SampleHeight(position) + terrain.transform.position.y : float.MinValue;
    }

    public float WaterDepthAt(Vector3 position) // 이 위치의 물 깊이 (수면 ~ Terrain 바닥, 뭍이면 0 이하)
    {
        return SeaLevel - GroundHeight(position);
    }

    public bool IsOverWater(Vector3 position) // 바닷물 위인지 (Terrain 바닥이 수면보다 낮음, 땅 밑 동굴 제외)
    {
        return WaterDepthAt(position) > 0.2f;
    }

    private bool DiveHeld() // C 누르고 있는지
    {
        if (diveActionReference != null)
        {
            return diveActionReference.action.IsPressed();
        }

        return Keyboard.current != null && Keyboard.current.cKey.isPressed;
    }

    // ---------------------------------------------------------------- 이동 (PlayerMovement가 매 프레임 부름)

    public bool HandleMovement(Vector3 moveDirection, bool grounded, ref float verticalVelocity, bool sprintHeld, bool jumpHeld, bool jumpPressed, float deltaTime) // 헤엄 중이면 이동을 대신하고 true
    {
        float feet = FeetDepth;
        bool overWater = IsOverWater(transform.position);

        if (!IsSwimming)
        {
            bool walkIn = feet >= enterDepth; // 걸어 들어감
            bool fallIn = !grounded && verticalVelocity < 0f && feet >= 0.45f && WaterDepthAt(transform.position) >= floatDepth + 0.3f; // 뛰어듦
            WadeSpeedMultiplier = overWater && feet > wadeStartDepth ? Mathf.Lerp(1f, wadeSlowest, Mathf.InverseLerp(wadeStartDepth, enterDepth, feet)) : 1f;
            state = overWater && feet > 0.05f ? PlayerWaterState.Wading : PlayerWaterState.Dry;

            if (!overWater || (!walkIn && !fallIn) || playerHealth.IsDead)
            {
                return false;
            }

            BeginSwim(verticalVelocity);
        }
        else if (!overWater || (feet < exitDepth && (touchingBottom || verticalSpeed > 0.5f))) // 발이 땅 · 바위에 닿아 얕아짐, 또는 뛰어오름
        {
            EndSwim(ref verticalVelocity);
            return false;
        }

        bool dead = playerHealth.IsDead;
        bool exhausted = playerStamina.IsExhausted;
        bool atSurface = feet <= floatDepth + 0.12f;

        // 수직 : 잠수 · 떠오르기 · 부력 · 수면 유지
        if (dead)
        {
            verticalSpeed = Mathf.MoveTowards(verticalSpeed, feet > floatDepth ? buoyancy : 0f, 4f * deltaTime);
        }
        else if (verticalSpeed > riseSpeed + 0.2f) // 뛰어오르는 중 (중력)
        {
            verticalSpeed -= 20f * deltaTime;
        }
        else if (jumpPressed && atSurface) // 수면에서 뛰어오르기
        {
            verticalSpeed = hopSpeed;
        }
        else if (DiveHeld() && !exhausted) // 잠수
        {
            verticalSpeed = Mathf.MoveTowards(verticalSpeed, -diveSpeed, 8f * deltaTime);
        }
        else if (jumpHeld && !atSurface) // 떠오르기
        {
            verticalSpeed = Mathf.MoveTowards(verticalSpeed, riseSpeed, 8f * deltaTime);
        }
        else if (feet > floatDepth + 0.12f) // 천천히 떠오름
        {
            verticalSpeed = Mathf.MoveTowards(verticalSpeed, exhausted ? riseSpeed * 0.6f : buoyancy, 3f * deltaTime);
        }
        else // 수면에 떠 있음 (살짝 출렁임)
        {
            float bob = Mathf.Sin(Time.time * 2.2f) * 0.04f;
            verticalSpeed = (feet - (floatDepth + bob)) * 5f;
        }

        if (verticalSpeed < -diveSpeed) // 뛰어든 속도는 물이 빠르게 줄임
        {
            verticalSpeed = Mathf.MoveTowards(verticalSpeed, -diveSpeed, 30f * deltaTime);
        }

        // 수평 : 헤엄 · 빠른 헤엄 · 파도
        moving = !dead && moveDirection.sqrMagnitude > 0.01f;
        bool underwater = IsHeadUnderwater();
        bool fast = moving && sprintHeld && !exhausted;
        float speed = underwater ? diveSwimSpeed : swimSpeed;

        if (fast)
        {
            speed = underwater ? diveSwimSpeed * 1.5f : fastSwimSpeed;
        }

        if (exhausted)
        {
            speed *= exhaustedSpeedMultiplier;
        }

        Vector3 wave = IslandShoreGuard.Instance != null ? IslandShoreGuard.Instance.WavePushAt(transform.position) : Vector3.zero;
        LastWavePush = wave.magnitude;

        if (LastWavePush > 0.5f && moving && Vector3.Dot(moveDirection, wave) < 0f && Time.time >= nextWaveMessageTime)
        {
            nextWaveMessageTime = Time.time + 3f;
            IslandShoreGuard.Instance.ShowBlockedMessage();
        }

        Vector3 motion = (dead ? Vector3.zero : moveDirection * speed) + wave + Vector3.up * verticalSpeed;
        CollisionFlags flags = characterController.Move(motion * deltaTime);

        touchingBottom = (flags & CollisionFlags.Below) != 0;

        if (touchingBottom && verticalSpeed < 0f) // 바다 밑에 닿음
        {
            verticalSpeed = 0f;
        }

        // 스태미나 : 움직이면 조금씩 소비, 떠 있으면 천천히 회복
        float drain = !moving ? 0f : fast ? fastSwimDrain : underwater ? diveDrain : swimDrain;
        playerStamina.UpdateSwim(drain, treadRecovery, deltaTime);

        if (playerStamina.IsExhausted && !exhaustedWarned)
        {
            exhaustedWarned = true;
            Say(ExhaustedMessage, new Color(1f, 0.78f, 0.45f, 1f));
        }

        verticalVelocity = verticalSpeed; // 헤엄을 끝낼 때 이어받음
        state = underwater ? PlayerWaterState.Underwater : PlayerWaterState.Surface;
        return true;
    }

    private bool IsHeadUnderwater() // 입이 물속인지
    {
        return transform.position.y + breathHeight < SeaLevel - 0.05f;
    }

    private void BeginSwim(float incomingVerticalVelocity) // 헤엄 시작
    {
        state = PlayerWaterState.Surface;
        verticalSpeed = Mathf.Min(0f, incomingVerticalVelocity);
        touchingBottom = false;
        WadeSpeedMultiplier = 1f;
        exhaustedWarned = false;
        SwimStartCount++;
        ShowTools(false);
        float impact = Mathf.Abs(incomingVerticalVelocity);
        Splash(impact > 4f ? 40 : 14, impact > 4f ? 0.9f : 0.45f);

        if (!hintShown)
        {
            hintShown = true;
            Say(SwimHintMessage, new Color(0.7f, 0.9f, 1f, 1f));
        }
    }

    private void EndSwim(ref float verticalVelocity) // 걷기로 돌아감
    {
        verticalVelocity = Mathf.Max(verticalSpeed, -2f);
        verticalSpeed = 0f;
        state = PlayerWaterState.Wading;
        moving = false;
        ShowTools(true);
    }

    public void ResetState() // 부활 · 불러오기 뒤 (걷기 · 숨 가득)
    {
        state = PlayerWaterState.Dry;
        verticalSpeed = 0f;
        currentBreath = maxBreath;
        drownTimer = 0f;
        lowBreathWarned = false;
        exhaustedWarned = false;
        WadeSpeedMultiplier = 1f;
        moving = false;
        ShowTools(true);

        if (bubbleParticles != null)
        {
            bubbleParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    // ---------------------------------------------------------------- 숨 · 젖음 · 효과

    private void Update() // 숨 · 숨 막힘 · 젖음
    {
        float deltaTime = Time.deltaTime;

        if (deltaTime <= 0f)
        {
            return;
        }

        if (playerWetness != null && IsOverWater(transform.position) && FeetDepth > 0.3f) // 물에 잠김
        {
            playerWetness.Soak(soakPerSecond, deltaTime);
        }

        if (playerHealth.IsDead)
        {
            drownTimer = 0f;
            return;
        }

        if (IsUnderwater)
        {
            currentBreath = Mathf.Max(0f, currentBreath - breathDrainPerSecond * deltaTime);
        }
        else
        {
            currentBreath = Mathf.Min(maxBreath, currentBreath + breathRecoveryPerSecond * deltaTime);
            lowBreathWarned = currentBreath < maxBreath * 0.25f && lowBreathWarned;
        }

        if (IsUnderwater && !lowBreathWarned && currentBreath <= maxBreath * 0.25f)
        {
            lowBreathWarned = true;
            Say(LowBreathMessage, new Color(0.7f, 0.9f, 1f, 1f));
        }

        if (!IsUnderwater || currentBreath > 0f)
        {
            drownTimer = 0f;
            return;
        }

        drownTimer += deltaTime;

        if (drownTimer < drownInterval)
        {
            return;
        }

        drownTimer -= drownInterval;
        DrownTickCount++;
        playerHealth.TakeDamage(drownDamage);

        if (DrownTickCount % 3 == 1)
        {
            Say(DrowningMessage, new Color(1f, 0.5f, 0.45f, 1f));
        }
    }

    private void LateUpdate() // 헤엄 자세 · 물방울 · 물결
    {
        float deltaTime = Time.deltaTime;
        bool swimming = IsSwimming;
        poseBlend = Mathf.MoveTowards(poseBlend, swimming ? 1f : 0f, deltaTime * 4f);

        if (visualRoot != null)
        {
            float targetPitch = IsUnderwater ? Mathf.Clamp(72f - verticalSpeed * 14f, 35f, 110f) : 72f; // 잠수하면 머리를 아래로
            posePitch = Mathf.Lerp(posePitch, targetPitch, 1f - Mathf.Exp(-6f * deltaTime));
            float stroke = moving ? Mathf.Sin(Time.time * 7f) * 9f : Mathf.Sin(Time.time * 2f) * 3f; // 팔 젓기처럼 좌우로 흔들림
            visualRoot.localRotation = visualBaseRotation * Quaternion.Euler(posePitch * poseBlend, 0f, stroke * poseBlend);
            visualRoot.localPosition = visualBasePosition + Vector3.up * (0.05f * poseBlend);
        }

        if (bubbleParticles != null)
        {
            bool bubbles = IsUnderwater && !playerHealth.IsDead;

            if (bubbles && !bubbleParticles.isEmitting)
            {
                bubbleParticles.Play(true);
            }
            else if (!bubbles && bubbleParticles.isEmitting)
            {
                bubbleParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        if (swimming && !IsUnderwater && moving && Time.time >= nextRippleTime) // 수면 헤엄 물결
        {
            nextRippleTime = Time.time + 0.35f;
            Splash(3, 0f);
        }
    }

    private void Splash(int count, float soundVolume) // 수면 물보라 · 소리
    {
        Vector3 point = new Vector3(transform.position.x, SeaLevel + 0.05f, transform.position.z);

        if (splashParticles != null)
        {
            splashParticles.transform.position = point;
            splashParticles.Emit(count);
        }

        if (soundVolume > 0f)
        {
            CombatFeedbackAudio.PlayAt(SwimFeedbackAudio.SplashClip, point, soundVolume);
        }
    }

    private void ShowTools(bool visible) // 헤엄칠 때 손 도구 숨김
    {
        if (toolHolder == null || toolHolderHidden == !visible)
        {
            return;
        }

        toolHolderHidden = !visible;
        toolHolder.SetActive(visible);
    }

    private void Say(string message, Color color) // 머리 위 말풍선
    {
        CombatDamagePopup.SpawnText(transform.position + Vector3.up * 2.2f, message, color, 2f);
    }

#if UNITY_EDITOR
    public void EditorAssign(InputActionReference dive, Transform visual, GameObject tools, ParticleSystem splash, ParticleSystem bubbles, float level) // 생성 도구 전용
    {
        diveActionReference = dive;
        visualRoot = visual;
        toolHolder = tools;
        splashParticles = splash;
        bubbleParticles = bubbles;
        seaLevel = level;
    }

    public void EditorSetBreath(float value) // 테스트 전용
    {
        currentBreath = Mathf.Clamp(value, 0f, maxBreath);
    }
#endif
}
