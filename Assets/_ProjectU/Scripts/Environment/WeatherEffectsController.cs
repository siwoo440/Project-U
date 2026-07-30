using System.Collections; // 코루틴 기능
using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class WeatherEffectsController : MonoBehaviour // 날씨 시각과 음향 효과 관리
{
    [Header("References")] // 필수 참조 묶음
    [SerializeField] private WeatherCycle weatherCycle; // 날씨 순환 관리자
    [SerializeField] private Light sunLight; // 태양 방향광
    [SerializeField] private ParticleSystem rainParticles; // 비 파티클
    [SerializeField] private ParticleSystem snowParticles; // 눈 파티클
    [SerializeField] private AudioSource weatherAudioSource; // 날씨 음향 재생기

    [Header("Audio Clips")] // 날씨 음향 파일 묶음
    [SerializeField] private AudioClip rainLoop; // 비 반복 음향
    [SerializeField] private AudioClip windLoop; // 바람 반복 음향
    [SerializeField] private AudioClip stormLoop; // 폭풍 반복 음향

    [Header("Fog Colors")] // 날씨별 안개 색상 묶음
    [SerializeField] private Color clearFogColor = new Color(0.65f, 0.75f, 0.85f); // 맑음 안개 색상
    [SerializeField] private Color cloudyFogColor = new Color(0.55f, 0.60f, 0.65f); // 흐림 안개 색상
    [SerializeField] private Color rainFogColor = new Color(0.35f, 0.40f, 0.45f); // 비 안개 색상
    [SerializeField] private Color snowFogColor = new Color(0.75f, 0.80f, 0.85f); // 눈 안개 색상
    [SerializeField] private Color stormFogColor = new Color(0.18f, 0.22f, 0.28f); // 폭풍 안개 색상

    [Header("Fog Density")] // 날씨별 안개 밀도 묶음
    [SerializeField][Min(0f)] private float cloudyFogDensity = 0.0015f; // 흐림 안개 밀도
    [SerializeField][Min(0f)] private float rainFogDensity = 0.0025f; // 비 안개 밀도
    [SerializeField][Min(0f)] private float snowFogDensity = 0.003f; // 눈 안개 밀도
    [SerializeField][Min(0f)] private float stormFogDensity = 0.005f; // 폭풍 안개 밀도

    [Header("Sun Multipliers")] // 날씨별 태양 밝기 배율 묶음
    [SerializeField][Range(0f, 1f)] private float clearSunMultiplier = 1f; // 맑음 밝기 배율
    [SerializeField][Range(0f, 1f)] private float cloudySunMultiplier = 0.65f; // 흐림 밝기 배율
    [SerializeField][Range(0f, 1f)] private float rainSunMultiplier = 0.5f; // 비 밝기 배율
    [SerializeField][Range(0f, 1f)] private float snowSunMultiplier = 0.75f; // 눈 밝기 배율
    [SerializeField][Range(0f, 1f)] private float stormSunMultiplier = 0.35f; // 폭풍 밝기 배율

    [Header("Particle Rates")] // 날씨별 파티클 발생량 묶음
    [SerializeField][Min(0f)] private float rainEmissionRate = 700f; // 비 파티클 발생량
    [SerializeField][Min(0f)] private float snowEmissionRate = 300f; // 눈 파티클 발생량
    [SerializeField][Min(0f)] private float stormEmissionRate = 1400f; // 폭풍 파티클 발생량

    [Header("Audio Volumes")] // 날씨별 음향 크기 묶음
    [SerializeField][Range(0f, 1f)] private float cloudyVolume = 0.15f; // 흐림 바람 음량
    [SerializeField][Range(0f, 1f)] private float rainVolume = 0.45f; // 비 음량
    [SerializeField][Range(0f, 1f)] private float snowVolume = 0.25f; // 눈 바람 음량
    [SerializeField][Range(0f, 1f)] private float stormVolume = 0.7f; // 폭풍 음량

    [Header("Shelter")] // 지붕 효과 설정 묶음
    [SerializeField][Range(0f, 1f)] private float shelteredAudioMultiplier = 0.2f; // 지붕 아래 날씨 음량 배율
    [SerializeField] private LayerMask precipitationCollisionMask; // 강수 충돌 대상 Layer

    [Header("Transition")] // 날씨 전환 설정 묶음
    [SerializeField][Min(0.1f)] private float transitionDuration = 2.5f; // 날씨 전환 시간

    [Header("Runtime")] // 실행 상태 묶음
    [SerializeField] private float currentSunMultiplier = 1f; // 현재 태양 밝기 배율
    [SerializeField] private bool isPlayerSheltered; // 플레이어 지붕 아래 상태

    private Coroutine environmentTransitionRoutine; // 환경 전환 코루틴
    private Coroutine audioTransitionRoutine; // 음향 전환 코루틴

    private void Awake() // 날씨 효과 참조 초기화
    {
        ClampSettings(); // 설정값 범위 보정

        if (weatherCycle == null || sunLight == null || rainParticles == null || snowParticles == null || weatherAudioSource == null) // 필수 참조 확인
        {
            Debug.LogError($"{gameObject.name}의 날씨 효과 참조가 누락되었습니다.", this); // 참조 누락 오류
            enabled = false; // 날씨 효과 비활성화
            return; // 초기화 중단
        }

        RenderSettings.fogMode = FogMode.ExponentialSquared; // 지수 제곱 안개 모드 적용
        ConfigureAudioSource(); // 날씨 음향 재생기 설정
        ConfigurePrecipitationCollision(); // 강수 파티클 충돌 설정
    }

    private void OnEnable() // 날씨 변경 이벤트 연결
    {
        if (weatherCycle != null) // 날씨 관리자 존재 확인
        {
            weatherCycle.WeatherChanged += HandleWeatherChanged; // 날씨 변경 이벤트 구독
        }
    }

    private void Start() // 시작 날씨 효과 적용
    {
        if (!enabled) // 컴포넌트 활성 상태 확인
        {
            return; // 시작 처리 중단
        }

        ApplyWeather(weatherCycle.CurrentWeather, true); // 현재 날씨 즉시 적용
    }

    private void LateUpdate() // 시간 조명 계산 이후 날씨 밝기 적용
    {
        sunLight.intensity *= currentSunMultiplier; // 현재 날씨 밝기 배율 적용
    }

    private void OnDisable() // 날씨 변경 이벤트 해제
    {
        if (weatherCycle != null) // 날씨 관리자 존재 확인
        {
            weatherCycle.WeatherChanged -= HandleWeatherChanged; // 날씨 변경 이벤트 구독 해제
        }
    }

    private void OnValidate() // Inspector 설정값 검증
    {
        ClampSettings(); // 설정값 범위 보정
    }

    public void SetPlayerSheltered(bool isSheltered) // 플레이어 지붕 상태 적용
    {
        if (isPlayerSheltered == isSheltered) // 기존 상태와 동일한지 확인
        {
            return; // 중복 효과 갱신 방지
        }

        isPlayerSheltered = isSheltered; // 현재 지붕 상태 저장
        ApplyWeather(weatherCycle.CurrentWeather, false); // 현재 날씨 효과 다시 적용
    }

    private void HandleWeatherChanged(WeatherType weather) // 변경된 날씨 처리
    {
        ApplyWeather(weather, false); // 날씨 효과 전환 적용
    }

    private void ApplyWeather(WeatherType weather, bool immediate) // 날씨별 목표 효과 결정
    {
        float targetFogDensity = 0f; // 목표 안개 밀도 초기값
        Color targetFogColor = clearFogColor; // 목표 안개 색상 초기값
        float targetSunMultiplier = clearSunMultiplier; // 목표 태양 밝기 초기값
        float targetRainRate = 0f; // 목표 비 발생량 초기값
        float targetSnowRate = 0f; // 목표 눈 발생량 초기값
        AudioClip targetAudioClip = null; // 목표 음향 초기값
        float targetAudioVolume = 0f; // 목표 음량 초기값

        switch (weather) // 현재 날씨 비교
        {
            case WeatherType.Clear: // 맑음 확인
                break; // 맑음 기본값 유지

            case WeatherType.Cloudy: // 흐림 확인
                targetFogDensity = cloudyFogDensity; // 흐림 안개 밀도 적용
                targetFogColor = cloudyFogColor; // 흐림 안개 색상 적용
                targetSunMultiplier = cloudySunMultiplier; // 흐림 밝기 배율 적용
                targetAudioClip = windLoop; // 흐림 바람 음향 적용
                targetAudioVolume = cloudyVolume; // 흐림 음량 적용
                break; // 흐림 설정 완료

            case WeatherType.Rain: // 비 확인
                targetFogDensity = rainFogDensity; // 비 안개 밀도 적용
                targetFogColor = rainFogColor; // 비 안개 색상 적용
                targetSunMultiplier = rainSunMultiplier; // 비 밝기 배율 적용
                targetRainRate = rainEmissionRate; // 비 파티클 발생량 적용
                targetAudioClip = rainLoop; // 비 음향 적용
                targetAudioVolume = rainVolume; // 비 음량 적용
                break; // 비 설정 완료

            case WeatherType.Snow: // 눈 확인
                targetFogDensity = snowFogDensity; // 눈 안개 밀도 적용
                targetFogColor = snowFogColor; // 눈 안개 색상 적용
                targetSunMultiplier = snowSunMultiplier; // 눈 밝기 배율 적용
                targetSnowRate = snowEmissionRate; // 눈 파티클 발생량 적용
                targetAudioClip = windLoop; // 눈 바람 음향 적용
                targetAudioVolume = snowVolume; // 눈 음량 적용
                break; // 눈 설정 완료

            case WeatherType.Storm: // 폭풍 확인
                targetFogDensity = stormFogDensity; // 폭풍 안개 밀도 적용
                targetFogColor = stormFogColor; // 폭풍 안개 색상 적용
                targetSunMultiplier = stormSunMultiplier; // 폭풍 밝기 배율 적용
                targetRainRate = stormEmissionRate; // 폭풍 빗방울 발생량 적용
                targetAudioClip = stormLoop; // 폭풍 음향 적용
                targetAudioVolume = stormVolume; // 폭풍 음량 적용
                break; // 폭풍 설정 완료
        }

        if (isPlayerSheltered) // 플레이어 지붕 아래 상태 확인
        {
            targetAudioVolume *= shelteredAudioMultiplier; // 지붕 아래 날씨 음량 감소
        }

        SetParticleEmission(rainParticles, targetRainRate); // 비 파티클 발생량 적용
        SetParticleEmission(snowParticles, targetSnowRate); // 눈 파티클 발생량 적용
        BeginEnvironmentTransition(targetFogColor, targetFogDensity, targetSunMultiplier, immediate); // 환경 효과 전환 시작
        BeginAudioTransition(targetAudioClip, targetAudioVolume, immediate); // 날씨 음향 전환 시작
    }

    private void SetParticleEmission(ParticleSystem particles, float emissionRate) // 파티클 발생량 적용
    {
        ParticleSystem.EmissionModule emission = particles.emission; // 파티클 발생 모듈 가져오기
        emission.rateOverTime = emissionRate; // 초당 파티클 발생량 적용

        if (emissionRate > 0f) // 파티클 재생 필요 여부 확인
        {
            if (!particles.isPlaying) // 현재 재생 상태 확인
            {
                particles.Play(true); // 파티클 재생 시작
            }

            return; // 파티클 설정 종료
        }

        if (particles.isPlaying) // 현재 파티클 재생 여부 확인
        {
            particles.Stop(true, ParticleSystemStopBehavior.StopEmitting); // 새 파티클 발생 중단
        }
    }

    private void BeginEnvironmentTransition(Color targetColor, float targetDensity, float targetSunMultiplier, bool immediate) // 환경 전환 시작
    {
        if (environmentTransitionRoutine != null) // 기존 환경 전환 확인
        {
            StopCoroutine(environmentTransitionRoutine); // 기존 환경 전환 중단
            environmentTransitionRoutine = null; // 환경 코루틴 참조 초기화
        }

        if (immediate) // 즉시 적용 여부 확인
        {
            RenderSettings.fogColor = targetColor; // 안개 색상 즉시 적용
            RenderSettings.fogDensity = targetDensity; // 안개 밀도 즉시 적용
            RenderSettings.fog = targetDensity > 0f; // 안개 사용 여부 즉시 적용
            currentSunMultiplier = targetSunMultiplier; // 태양 밝기 즉시 적용
            return; // 환경 전환 종료
        }

        environmentTransitionRoutine = StartCoroutine(TransitionEnvironment(targetColor, targetDensity, targetSunMultiplier)); // 환경 전환 코루틴 시작
    }

    private IEnumerator TransitionEnvironment(Color targetColor, float targetDensity, float targetSunMultiplier) // 환경 효과 부드러운 전환
    {
        Color startColor = RenderSettings.fogColor; // 시작 안개 색상 저장
        float startDensity = RenderSettings.fogDensity; // 시작 안개 밀도 저장
        float startSunMultiplier = currentSunMultiplier; // 시작 밝기 배율 저장
        float elapsedTime = 0f; // 지난 전환 시간 초기화

        RenderSettings.fog = true; // 전환 중 안개 활성화

        while (elapsedTime < transitionDuration) // 전환 시간 진행 확인
        {
            elapsedTime += Time.deltaTime; // 지난 시간 증가
            float progress = Mathf.Clamp01(elapsedTime / transitionDuration); // 기본 전환 비율 계산
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress); // 부드러운 전환 비율 계산
            RenderSettings.fogColor = Color.Lerp(startColor, targetColor, smoothProgress); // 안개 색상 혼합
            RenderSettings.fogDensity = Mathf.Lerp(startDensity, targetDensity, smoothProgress); // 안개 밀도 혼합
            currentSunMultiplier = Mathf.Lerp(startSunMultiplier, targetSunMultiplier, smoothProgress); // 태양 밝기 혼합
            yield return null; // 다음 프레임 대기
        }

        RenderSettings.fogColor = targetColor; // 최종 안개 색상 적용
        RenderSettings.fogDensity = targetDensity; // 최종 안개 밀도 적용
        RenderSettings.fog = targetDensity > 0f; // 최종 안개 사용 여부 적용
        currentSunMultiplier = targetSunMultiplier; // 최종 태양 밝기 적용
        environmentTransitionRoutine = null; // 환경 코루틴 참조 초기화
    }

    private void BeginAudioTransition(AudioClip targetClip, float targetVolume, bool immediate) // 음향 전환 시작
    {
        if (audioTransitionRoutine != null) // 기존 음향 전환 확인
        {
            StopCoroutine(audioTransitionRoutine); // 기존 음향 전환 중단
            audioTransitionRoutine = null; // 음향 코루틴 참조 초기화
        }

        if (immediate) // 즉시 적용 여부 확인
        {
            ApplyAudioImmediately(targetClip, targetVolume); // 날씨 음향 즉시 적용
            return; // 음향 전환 종료
        }

        audioTransitionRoutine = StartCoroutine(TransitionAudio(targetClip, targetVolume)); // 음향 전환 코루틴 시작
    }

    private void ApplyAudioImmediately(AudioClip targetClip, float targetVolume) // 음향 즉시 적용
    {
        weatherAudioSource.Stop(); // 기존 음향 중단
        weatherAudioSource.clip = targetClip; // 목표 음향 파일 적용
        weatherAudioSource.volume = targetVolume; // 목표 음량 적용

        if (targetClip != null) // 재생할 음향 확인
        {
            weatherAudioSource.Play(); // 목표 음향 재생
        }
    }

    private IEnumerator TransitionAudio(AudioClip targetClip, float targetVolume) // 날씨 음향 부드러운 전환
    {
        if (weatherAudioSource.clip == targetClip && weatherAudioSource.isPlaying) // 같은 음향 재생 여부 확인
        {
            yield return FadeAudioVolume(targetVolume, transitionDuration); // 현재 음향 음량 전환
            audioTransitionRoutine = null; // 음향 코루틴 참조 초기화
            yield break; // 음향 전환 종료
        }

        float halfDuration = transitionDuration * 0.5f; // 절반 전환 시간 계산
        yield return FadeAudioVolume(0f, halfDuration); // 기존 음향 서서히 제거
        weatherAudioSource.Stop(); // 기존 음향 재생 중단
        weatherAudioSource.clip = targetClip; // 새로운 음향 파일 적용

        if (targetClip == null) // 새로운 음향 존재 여부 확인
        {
            weatherAudioSource.volume = 0f; // 음량 초기화
            audioTransitionRoutine = null; // 음향 코루틴 참조 초기화
            yield break; // 음향 전환 종료
        }

        weatherAudioSource.volume = 0f; // 새로운 음향 시작 음량 초기화
        weatherAudioSource.Play(); // 새로운 음향 재생
        yield return FadeAudioVolume(targetVolume, halfDuration); // 새로운 음향 서서히 증가
        audioTransitionRoutine = null; // 음향 코루틴 참조 초기화
    }

    private IEnumerator FadeAudioVolume(float targetVolume, float duration) // 음량 부드러운 전환
    {
        float startVolume = weatherAudioSource.volume; // 시작 음량 저장
        float elapsedTime = 0f; // 지난 전환 시간 초기화
        float safeDuration = Mathf.Max(0.01f, duration); // 전환 시간 최소값 보정

        while (elapsedTime < safeDuration) // 음량 전환 진행 확인
        {
            elapsedTime += Time.deltaTime; // 지난 시간 증가
            float progress = Mathf.Clamp01(elapsedTime / safeDuration); // 음량 전환 비율 계산
            weatherAudioSource.volume = Mathf.Lerp(startVolume, targetVolume, progress); // 현재 음량 혼합
            yield return null; // 다음 프레임 대기
        }

        weatherAudioSource.volume = targetVolume; // 최종 음량 적용
    }

    private void ConfigureAudioSource() // 날씨 음향 재생기 기본 설정
    {
        weatherAudioSource.playOnAwake = false; // 자동 재생 비활성화
        weatherAudioSource.loop = true; // 반복 재생 활성화
        weatherAudioSource.spatialBlend = 0f; // 이차원 환경음 적용
    }

    private void ConfigurePrecipitationCollision() // 강수 파티클 충돌 설정
    {
        ConfigureParticleCollision(rainParticles); // 비 파티클 충돌 설정
        ConfigureParticleCollision(snowParticles); // 눈 파티클 충돌 설정
    }

    private void ConfigureParticleCollision(ParticleSystem particles) // 개별 파티클 충돌 설정
    {
        ParticleSystem.CollisionModule collision = particles.collision; // 파티클 충돌 모듈 가져오기
        collision.enabled = true; // 파티클 충돌 활성화
        collision.type = ParticleSystemCollisionType.World; // 월드 Collider 충돌 적용
        collision.mode = ParticleSystemCollisionMode.Collision3D; // 삼차원 충돌 방식 적용
        collision.collidesWith = precipitationCollisionMask; // 지정 Layer 충돌 적용
        collision.quality = ParticleSystemCollisionQuality.High; // 높은 충돌 정확도 적용
        collision.bounce = 0f; // 충돌 반사 제거
        collision.dampen = 0f; // 충돌 감속 제거
        collision.lifetimeLoss = 1f; // 충돌 파티클 즉시 제거
        collision.enableDynamicColliders = true; // 설치 건축물 충돌 허용
    }

    private void ClampSettings() // Inspector 설정값 보정
    {
        cloudyFogDensity = Mathf.Max(0f, cloudyFogDensity); // 흐림 안개 밀도 보정
        rainFogDensity = Mathf.Max(0f, rainFogDensity); // 비 안개 밀도 보정
        snowFogDensity = Mathf.Max(0f, snowFogDensity); // 눈 안개 밀도 보정
        stormFogDensity = Mathf.Max(0f, stormFogDensity); // 폭풍 안개 밀도 보정
        rainEmissionRate = Mathf.Max(0f, rainEmissionRate); // 비 발생량 보정
        snowEmissionRate = Mathf.Max(0f, snowEmissionRate); // 눈 발생량 보정
        stormEmissionRate = Mathf.Max(0f, stormEmissionRate); // 폭풍 발생량 보정
        transitionDuration = Mathf.Max(0.1f, transitionDuration); // 전환 시간 최소값 보정
        shelteredAudioMultiplier = Mathf.Clamp01(shelteredAudioMultiplier); // 지붕 음량 배율 범위 적용
    }
}