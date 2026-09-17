using UnityEngine;

// 76일차: 전투 효과음 재생과 오디오 파일이 없을 때 사용할 기본 효과음 생성
public static class CombatFeedbackAudio
{
    private const int SampleRate = 44100;

    private static AudioClip hitClip;
    private static AudioClip killClip;
    private static AudioClip warningClip;

    public static AudioClip HitClip => hitClip != null ? hitClip : hitClip = CreateHitClip();
    public static AudioClip KillClip => killClip != null ? killClip : killClip = CreateKillClip();
    public static AudioClip WarningClip => warningClip != null ? warningClip : warningClip = CreateWarningClip();

    public static void PlayAt(AudioClip clip, Vector3 position, float volume)
    {
        if (clip == null || volume <= 0f)
        {
            return;
        }

        GameObject soundObject = new GameObject("CombatSfx_" + clip.name);
        soundObject.transform.position = position;

        AudioSource source = soundObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = Mathf.Clamp01(volume);
        source.pitch = Random.Range(0.94f, 1.06f);
        source.spatialBlend = 0.7f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 3f;
        source.maxDistance = 40f;
        source.dopplerLevel = 0f;
        source.Play();

        Object.Destroy(soundObject, clip.length / source.pitch + 0.1f);
    }

    private static AudioClip CreateHitClip()
    {
        return CreateClip("Sfx_Hit_Default", 0.14f, (time, progress) =>
        {
            float envelope = Mathf.Exp(-time * 38f);
            float noise = Random.Range(-1f, 1f) * Mathf.Exp(-time * 60f);
            float frequency = Mathf.Lerp(190f, 80f, progress);
            float thump = Mathf.Sin(2f * Mathf.PI * frequency * time);
            return (noise * 0.55f + thump * 0.75f) * envelope;
        });
    }

    private static AudioClip CreateKillClip()
    {
        return CreateClip("Sfx_Kill_Default", 0.5f, (time, progress) =>
        {
            float envelope = Mathf.Exp(-time * 6f);
            float frequency = Mathf.Lerp(420f, 60f, progress);
            float tone = Mathf.Sin(2f * Mathf.PI * frequency * time);
            float noise = Random.Range(-1f, 1f) * Mathf.Exp(-time * 18f);
            return (tone * 0.6f + noise * 0.45f) * envelope;
        });
    }

    private static AudioClip CreateWarningClip()
    {
        return CreateClip("Sfx_Warning_Default", 0.2f, (time, progress) =>
        {
            float frequency = progress < 0.5f ? 740f : 988f;
            float envelope = Mathf.Sin(progress * Mathf.PI);
            float tone = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * frequency * time)) * 0.3f
                + Mathf.Sin(2f * Mathf.PI * frequency * time) * 0.5f;
            return tone * envelope * 0.6f;
        });
    }

    private static AudioClip CreateClip(string clipName, float duration, System.Func<float, float, float> sampler)
    {
        int sampleCount = Mathf.CeilToInt(duration * SampleRate);
        float[] samples = new float[sampleCount];

        for (int index = 0; index < sampleCount; index++)
        {
            float time = index / (float)SampleRate;
            float progress = index / (float)sampleCount;
            samples[index] = Mathf.Clamp(sampler(time, progress), -1f, 1f);
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
