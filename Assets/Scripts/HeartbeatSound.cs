using UnityEngine;

/// <summary>
/// 심장 뛰는 사운드 1회 재생. 씬이 바뀌어도 끊기지 않는다.
/// 사운드 소스가 나오기 전까지는 코드로 만든 임시 "쿵-쿵" 소리를 쓴다.
/// </summary>
public static class HeartbeatSound
{
    const int SampleRate = 44100;

    static AudioSource source;

    public static void Play()
    {
        if (source == null)
        {
            var go = new GameObject("HeartbeatSound");
            Object.DontDestroyOnLoad(go);
            source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.clip = CreateClip();
        }
        source.Play();
    }

    static AudioClip CreateClip()
    {
        var data = new float[(int)(SampleRate * 0.7f)];
        AddThump(data, 0f, 1f);
        AddThump(data, 0.22f, 0.7f);

        var clip = AudioClip.Create("Heartbeat", data.Length, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
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
}
