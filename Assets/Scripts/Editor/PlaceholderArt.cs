using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 아트 리소스가 나오기 전까지 쓰는 임시 도형 스프라이트를 PNG로 만든다 (Assets/Art/Placeholder).
/// 실제 아트가 나오면 프리팹/씬의 스프라이트만 바꿔 끼우면 된다.
/// </summary>
public static class PlaceholderArt
{
    public const string Folder = "Assets/Art/Placeholder";

    public struct Set
    {
        public Sprite Square;    // 1x1 유닛
        public Sprite Circle;    // 지름 1유닛
        public Sprite Ring;      // 지름 1유닛, 얇은 고리 (적 범위)
        public Sprite Heart;
        public Sprite Drumstick;
    }

    public static Set CreateAll()
    {
        Directory.CreateDirectory(Folder);
        return new Set
        {
            Square = Save("Square", 4, FilterMode.Point, (x, y) => Color.white),
            Circle = Save("Circle", 128, FilterMode.Bilinear, (x, y) => Disc(x, y, 0.5f, 128)),
            Ring = Save("Ring", 512, FilterMode.Bilinear, (x, y) =>
            {
                float d = Distance(x, y);
                float edge = 1f / 512f;
                float a = Mathf.Clamp01((0.5f - d) / edge) * Mathf.Clamp01((d - 0.488f) / edge);
                return new Color(1, 1, 1, a);
            }),
            Heart = Save("Heart", 128, FilterMode.Bilinear, (x, y) =>
            {
                // 하트 곡선: (x²+y²-1)³ - x²y³ <= 0
                float hx = (x - 0.5f) * 2.6f, hy = (y - 0.45f) * 2.6f;
                float a = hx * hx + hy * hy - 1f;
                return a * a * a - hx * hx * hy * hy * hy <= 0f ? Color.white : Color.clear;
            }),
            Drumstick = Save("Drumstick", 128, FilterMode.Bilinear, (x, y) =>
            {
                var meat = new Color(0.6f, 0.35f, 0.18f);
                var bone = new Color(0.96f, 0.94f, 0.9f);
                var p = new Vector2(x, y);
                if (Vector2.Distance(p, new Vector2(0.38f, 0.62f)) < 0.3f) return meat;
                if (DistanceToSegment(p, new Vector2(0.5f, 0.45f), new Vector2(0.8f, 0.15f)) < 0.06f) return bone;
                if (Vector2.Distance(p, new Vector2(0.76f, 0.1f)) < 0.08f || Vector2.Distance(p, new Vector2(0.88f, 0.2f)) < 0.08f) return bone;
                return Color.clear;
            }),
        };
    }

    static Sprite Save(string name, int size, FilterMode filter, Func<float, float, Color> shape)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                pixels[y * size + x] = shape((x + 0.5f) / size, (y + 0.5f) / size);
        tex.SetPixels(pixels);
        tex.Apply();

        string path = $"{Folder}/{name}.png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = size; // 모든 스프라이트는 1유닛 크기
        importer.filterMode = filter;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static float Distance(float x, float y) => Vector2.Distance(new Vector2(x, y), new Vector2(0.5f, 0.5f));

    static Color Disc(float x, float y, float radius, int size) =>
        new(1, 1, 1, Mathf.Clamp01((radius - Distance(x, y)) * size + 0.5f));

    static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return Vector2.Distance(p, a + ab * t);
    }
}
