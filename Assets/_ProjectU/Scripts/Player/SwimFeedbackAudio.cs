using UnityEngine; // Unity 기본 기능

// 106일차: 물에 들어갈 때 첨벙 소리 (오디오 파일 없이 코드로 만든 기본 효과음)
public static class SwimFeedbackAudio
{
    private const int SampleRate = 44100; // 샘플 수
    private static AudioClip splashClip; // 첨벙

    public static AudioClip SplashClip => splashClip != null ? splashClip : splashClip = CreateSplashClip(); // 첨벙 소리

    private static AudioClip CreateSplashClip() // 낮게 깔리는 물 소리 + 흩어지는 물방울
    {
        const float duration = 0.55f;
        int sampleCount = Mathf.CeilToInt(duration * SampleRate);
        float[] samples = new float[sampleCount];
        System.Random random = new System.Random(106);
        float low = 0f; // 낮은 소리만 남기는 필터 값

        for (int index = 0; index < sampleCount; index++)
        {
            float time = index / (float)SampleRate;
            float noise = (float)random.NextDouble() * 2f - 1f;
            low += (noise - low) * 0.08f; // 부드러운 물소리
            float body = low * Mathf.Exp(-time * 7f) * 2.4f;
            float drops = 0f;

            if (random.NextDouble() < 0.0016 * Mathf.Exp(-time * 4f)) // 가끔 물방울 톡
            {
                drops = 0.5f;
            }

            float plop = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(260f, 90f, time / duration) * time) * Mathf.Exp(-time * 18f) * 0.5f;
            samples[index] = Mathf.Clamp((body + drops * noise + plop) * 0.7f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Sfx_Splash_Default", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
