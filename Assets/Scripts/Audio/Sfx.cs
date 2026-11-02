using UnityEngine;

/// <summary>
/// 효과음 재생. 씬이 바뀌어도 끊기지 않는다.
/// 사운드 소스가 나오기 전까지는 코드로 만든 임시 소리를 쓴다.
/// 소스가 나오면 각 Create...() 대신 Resources 등에서 불러온 AudioClip을 쓰면 된다.
/// </summary>
public static class Sfx
{
    const int SampleRate = 44100;

    static AudioSource heartbeat, shutter;

    /// <summary>심장 뛰는 소리 (쿵-쿵). 타이틀 버튼, 편지 화살표</summary>
    public static void Heartbeat() => Play(ref heartbeat, "Heartbeat", CreateHeartbeat);

    /// <summary>카메라 셔터음 (찰칵). 플래시 화면 전환</summary>
    public static void Shutter() => Play(ref shutter, "Shutter", CreateShutter);

    // 소리마다 AudioSource를 하나씩 둬서 동시에 나도 서로 끊기지 않게 한다
    static void Play(ref AudioSource source, string name, System.Func<AudioClip> createClip)
    {
        if (source == null)
        {
            var go = new GameObject($"Sfx_{name}");
            Object.DontDestroyOnLoad(go);
            source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.clip = createClip();
        }
        source.Play();
    }

    static AudioClip ToClip(string name, float[] data)
    {
        var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // ───────────── 임시 심장 소리

    static AudioClip CreateHeartbeat()
    {
        var data = new float[(int)(SampleRate * 0.7f)];
        AddThump(data, 0f, 1f);
        AddThump(data, 0.22f, 0.7f);
        return ToClip("Heartbeat", data);
    }

    static void AddThump(float[] data, float startSeconds, float volume)
    {
        int start = (int)(startSeconds * SampleRate);
        int length = (int)(0.2f * SampleRate);
        float phase = 0f;
        for (int i = 0; i < length && start + i < data.Length; i++)
        {
            float t = (float)i / SampleRate;
            float frequency = 70f + 90f * Mathf.Exp(-t * 30f);
            phase += 2f * Mathf.PI * frequency / SampleRate;
            float envelope = Mathf.Exp(-t * 22f) * (1f - Mathf.Exp(-t * 400f));

            // 저음만 있으면 노트북 스피커에서 안 들려서, 배음과 짧은 "툭" 타격음을 섞는다
            float body = Mathf.Sin(phase) + 0.5f * Mathf.Sin(2f * phase) + 0.25f * Mathf.Sin(3f * phase);
            float knock = Mathf.Sin(2f * Mathf.PI * 900f * t) * Mathf.Exp(-t * 250f) * 0.4f;
            data[start + i] += (body * 0.55f * envelope + knock) * volume;
        }
    }

    // ───────────── 임시 셔터음

    static AudioClip CreateShutter()
    {
        var data = new float[(int)(SampleRate * 0.15f)];
        var random = new System.Random(7);
        AddClick(data, random, 0f, 0.012f, 0.9f);    // 찰-
        AddClick(data, random, 0.06f, 0.03f, 0.6f);  // -칵
        return ToClip("Shutter", data);
    }

    // 짧게 터지고 빠르게 사라지는 잡음
    static void AddClick(float[] data, System.Random random, float startSeconds, float decaySeconds, float volume)
    {
        int start = (int)(startSeconds * SampleRate);
        int length = (int)(decaySeconds * 5f * SampleRate);
        float previous = 0f;
        for (int i = 0; i < length && start + i < data.Length; i++)
        {
            float t = (float)i / SampleRate;
            float noise = (float)(random.NextDouble() * 2.0 - 1.0);
            previous = previous * 0.5f + noise * 0.5f; // 살짝 둔탁하게
            data[start + i] += previous * Mathf.Exp(-t / decaySeconds) * volume;
        }
    }
}
