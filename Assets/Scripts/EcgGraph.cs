using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 심전도 그래프 (UI). 플레이어 심박수 속도에 맞춰 파형이 흐른다. 선 색은 Graphic의 Color.
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class EcgGraph : MaskableGraphic
{
    const int Samples = 160;
    const float SampleInterval = 1f / 60f;

    [SerializeField] PlayerStatus player;
    [SerializeField] float lineWidth = 3f;

    readonly float[] samples = new float[Samples];
    float phase, timer;

    protected override void Awake()
    {
        // UI는 CanvasRenderer가 있어야 그려진다 (예전에 만든 씬에는 빠져 있을 수 있음)
        if (!TryGetComponent(out CanvasRenderer _)) gameObject.AddComponent<CanvasRenderer>();
        base.Awake();
    }

    void Update()
    {
        if (!Application.isPlaying || player == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

        timer += Time.deltaTime;
        while (timer >= SampleInterval)
        {
            timer -= SampleInterval;
            phase = (phase + SampleInterval * player.HeartRate / 60f) % 1f;
            System.Array.Copy(samples, 1, samples, 0, Samples - 1);
            samples[Samples - 1] = Wave(phase);
        }
        SetVerticesDirty();
    }

    /// <summary>한 박동(0~1) 동안의 심전도 모양: P파 → QRS → T파</summary>
    static float Wave(float p)
    {
        float Bump(float center, float width, float height) =>
            height * Mathf.Exp(-(p - center) * (p - center) / (2f * width * width));

        return Bump(0.12f, 0.025f, 0.12f)
             - Bump(0.27f, 0.008f, 0.15f)
             + Bump(0.3f, 0.01f, 1f)
             - Bump(0.33f, 0.01f, 0.3f)
             + Bump(0.55f, 0.04f, 0.22f);
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        var r = GetPixelAdjustedRect();
        float baseline = r.yMin + r.height * 0.38f, amplitude = r.height * 0.5f, step = r.width / (Samples - 1);

        for (int i = 1; i < Samples; i++)
        {
            var a = new Vector2(r.xMin + (i - 1) * step, baseline + samples[i - 1] * amplitude);
            var b = new Vector2(r.xMin + i * step, baseline + samples[i] * amplitude);
            AddLine(vh, a, b);
        }
    }

    void AddLine(VertexHelper vh, Vector2 a, Vector2 b)
    {
        Vector2 normal = Vector2.Perpendicular((b - a).normalized) * (lineWidth * 0.5f);
        int start = vh.currentVertCount;
        vh.AddVert(a - normal, color, Vector2.zero);
        vh.AddVert(a + normal, color, Vector2.zero);
        vh.AddVert(b + normal, color, Vector2.zero);
        vh.AddVert(b - normal, color, Vector2.zero);
        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start, start + 2, start + 3);
    }
}
