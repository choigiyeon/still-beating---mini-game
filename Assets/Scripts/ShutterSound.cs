using UnityEngine;

/// <summary>
/// 카메라 셔터음 1회 재생. 씬이 바뀌어도 끊기지 않는다.
/// 사운드 소스가 나오기 전까지는 코드로 만든 임시 "찰칵" 소리를 쓴다.
/// </summary>
public static class ShutterSound
{
    const int SampleRate = 44100;

    static AudioSource source;

    public static void Play()
    {
        if (source == null)
        {
            var go = new GameObject("ShutterSound");
            Object.DontDestroyOnLoad(go);
            source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.clip = CreateClip();
        }
        source.Play();
    }

    static AudioClip CreateClip()
    {
        var data = new float[(int)(SampleRate * 0.15f)];
        var random = new System.Random(7);
        AddClick(data, random, 0f, 0.012f, 0.9f);    // 찰-
        AddClick(data, random, 0.06f, 0.03f, 0.6f);  // -칵

        var clip = AudioClip.Create("Shutter", data.Length, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
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
