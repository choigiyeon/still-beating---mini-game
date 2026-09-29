using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 아트 리소스가 나오기 전까지 쓰는 임시 도형 스프라이트 생성기.
/// </summary>
public static class SpriteFactory
{
    const int PixelsPerUnit = 64;

    static Sprite square;
    static readonly Dictionary<(float, float), Sprite> discs = new();

    /// <summary>1x1 유닛 흰색 사각형. transform.localScale로 크기를 조절한다.</summary>
    public static Sprite Square
    {
        get
        {
            if (square != null) return square;
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var pixels = new Color32[16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(pixels);
            tex.Apply();
            square = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
            return square;
        }
    }

    static Texture2D heart, drumstick;

    /// <summary>흰색 하트 아이콘 (GUI.color로 색을 입혀서 사용)</summary>
    public static Texture2D Heart => heart != null ? heart : heart = CreateIcon(128, (x, y) =>
    {
        // 하트 곡선: (x²+y²-1)³ - x²y³ <= 0
        float hx = (x - 0.5f) * 2.6f, hy = (y - 0.45f) * 2.6f;
        float a = hx * hx + hy * hy - 1f;
        return a * a * a - hx * hx * hy * hy * hy <= 0f ? Color.white : Color.clear;
    });

    /// <summary>닭다리 아이콘 (갈색 살 + 흰 뼈)</summary>
    public static Texture2D Drumstick => drumstick != null ? drumstick : drumstick = CreateIcon(64, (x, y) =>
    {
        var meat = new Color(0.6f, 0.35f, 0.18f);
        var bone = new Color(0.96f, 0.94f, 0.9f);
        var p = new Vector2(x, y);
        if (Vector2.Distance(p, new Vector2(0.38f, 0.62f)) < 0.3f) return meat;
        if (DistanceToSegment(p, new Vector2(0.5f, 0.45f), new Vector2(0.8f, 0.15f)) < 0.06f) return bone;
        if (Vector2.Distance(p, new Vector2(0.76f, 0.1f)) < 0.08f || Vector2.Distance(p, new Vector2(0.88f, 0.2f)) < 0.08f) return bone;
        return Color.clear;
    });

    static Texture2D CreateIcon(int size, System.Func<float, float, Color> shape)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                pixels[y * size + x] = shape((x + 0.5f) / size, (y + 0.5f) / size);
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return Vector2.Distance(p, a + ab * t);
    }

    /// <summary>반지름 radius(유닛)의 꽉 찬 원.</summary>
    public static Sprite Disc(float radius) => Ring(radius, 0f);

    /// <summary>반지름 radius(유닛), 두께 thickness(유닛)의 고리. thickness가 0 이하면 꽉 찬 원.</summary>
    public static Sprite Ring(float radius, float thickness)
    {
        if (discs.TryGetValue((radius, thickness), out var cached)) return cached;

        int size = Mathf.CeilToInt(radius * 2f * PixelsPerUnit) + 2;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var pixels = new Color32[size * size];
        float center = size * 0.5f;
        float inner = radius - thickness;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(center, center)) / PixelsPerUnit;
                float a = Mathf.Clamp01((radius - d) * PixelsPerUnit + 0.5f);
                if (thickness > 0f) a *= Mathf.Clamp01((d - inner) * PixelsPerUnit + 0.5f);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        discs[(radius, thickness)] = sprite;
        return sprite;
    }
}
