using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 메뉴 [Still Beating > 챕터 전환 연출 적용]
///  - Interlude1 (챕터1→2): 일러스트 한 장당 시간을 줄인다 (회의: 2~3초)
///  - Interlude2 (챕터2→3): 다연님 연출 순서대로 새로 만든다
///    사진1 → 사진2 → 사진3 → 검은 화면 → 문이 확대된 사진3(휘청이다 멈춤 + 흐림→선명) → 검은 화면 → 사진4(짧게) → 검은 화면 → 챕터3
/// 세부 시간/확대 위치는 Interlude2 씬의 Canvas > Slideshow Screen > Steps 에서 조절한다.
/// </summary>
static partial class SceneBuilder
{
    const float Interlude1SlideSeconds = 2f;
    const string IllustrationFolder = "Assets/Art/Prologue";

    [MenuItem("Still Beating/챕터 전환 연출 적용")]
    static void BuildCutscenesMenu()
    {
        if (!EditorUtility.DisplayDialog("Still Beating",
                $"챕터1→2 일러스트 시간을 장당 {Interlude1SlideSeconds}초로 줄이고,\n챕터2→3 전환 연출(Interlude2) 씬을 새로 만듭니다.\n(Interlude2가 이미 있으면 덮어씁니다)\n\n계속할까요?",
                "적용", "취소"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        ShortenInterlude1();
        BuildInterlude2();
        BuildScenesSetup.Apply();

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Still Beating", "완료! Interlude2 씬을 열었습니다.\nChapter2를 클리어하면 창문 탈출 → 이 연출 → 챕터3 순서로 진행됩니다.", "확인");
    }

    static void ShortenInterlude1()
    {
        // 16:9로 바뀐 그림을 다시 불러와서 4장 모두 다시 넣는다
        var slides = new Sprite[4];
        for (int i = 0; i < slides.Length; i++) slides[i] = LoadIllustration($"챕터 2-{i + 1}");

        var scene = EditorSceneManager.OpenScene($"{SceneFolder}/{SceneFlow.Interlude1}.unity");
        var screen = Object.FindAnyObjectByType<SlideshowScreen>();
        if (screen != null)
        {
            var so = new SerializedObject(screen);
            var list = so.FindProperty("slides");
            list.arraySize = slides.Length;
            for (int i = 0; i < slides.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = slides[i];
            so.FindProperty("slideSeconds").floatValue = Interlude1SlideSeconds;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene);
        }
    }

    static void BuildInterlude2()
    {
        var p1 = LoadIllustration("챕터 3-1");
        var p2 = LoadIllustration("챕터 3-2");
        var p3 = LoadIllustration("챕터 3-3");
        var p4 = LoadIllustration("챕터 3-4");

        NewScene(Color.black);
        var canvas = CreateCanvas("Canvas", 0);
        var screen = canvas.gameObject.AddComponent<SlideshowScreen>();
        var slide = AddImage(Stretch("Slide", canvas.transform), Color.white, p1);
        slide.preserveAspect = true;
        Set(screen, "slideImage", slide);

        var so = new SerializedObject(screen);
        so.FindProperty("nextScene").stringValue = SceneFlow.Chapter3;
        var steps = so.FindProperty("steps");
        steps.arraySize = 8;

        // (그림, 시간, 전환) - 그림이 null이면 검은 화면
        // 사진1: 창문 탈출 검은 화면(1.5초)이 걷히면서 보이므로 그만큼 길게
        SetStep(steps.GetArrayElementAtIndex(0), p1, 4.0f, SlideTransition.Cut);
        SetStep(steps.GetArrayElementAtIndex(1), p2, 2.5f, SlideTransition.Flash);
        SetStep(steps.GetArrayElementAtIndex(2), p3, 2.0f, SlideTransition.Flash);
        SetStep(steps.GetArrayElementAtIndex(3), null, 0.6f, SlideTransition.Cut); // 확대 전에 검은 화면으로 한 번 끊기
        var zoom = steps.GetArrayElementAtIndex(4);
        // 문이 확대된 상태로 등장. 동시에: 주인공이 휘청이듯 천천히 흔들리다 멈춤 2.2초 + 흐릿→선명 2.0초 → 선명한 상태 잠깐
        SetStep(zoom, p3, 3.4f, SlideTransition.Cut);
        zoom.FindPropertyRelative("zoomFocus").boolValue = true;
        // 다연님 확정(A안): 문 위 하늘 ~ 경비 벨트 아래(허벅지 조금), 경비는 반쯤 (원본 기준 y 70~565)
        zoom.FindPropertyRelative("zoomCenter").vector2Value = new Vector2(0.5f, 0.662f);
        zoom.FindPropertyRelative("zoomScale").floatValue = 1.9f;
        zoom.FindPropertyRelative("zoomSeconds").floatValue = 0f;   // 처음부터 확대된 상태
        zoom.FindPropertyRelative("shakeSeconds").floatValue = 2.2f;
        zoom.FindPropertyRelative("shakeStrength").floatValue = 40f;
        zoom.FindPropertyRelative("shakeSpeed").floatValue = 1.2f;  // 천천히 휘청 (처음 12 → 1.8 → 1.2)
        zoom.FindPropertyRelative("shakeTilt").floatValue = 2f;     // 살짝 기울어짐 (휘청)
        zoom.FindPropertyRelative("focusSeconds").floatValue = 2.0f;
        zoom.FindPropertyRelative("blurredImage").objectReferenceValue = MakeBlurred("챕터 3-3");
        SetStep(steps.GetArrayElementAtIndex(5), null, 1.0f, SlideTransition.Cut); // 검은 화면
        SetStep(steps.GetArrayElementAtIndex(6), p4, 1.2f, SlideTransition.Cut);   // 사진4: 다른 장보다 짧게
        SetStep(steps.GetArrayElementAtIndex(7), null, 1.0f, SlideTransition.Cut); // 검은 화면 → 챕터3
        so.ApplyModifiedPropertiesWithoutUndo();

        SaveScene(SceneFlow.Interlude2);
    }

    static void SetStep(SerializedProperty step, Sprite image, float seconds, SlideTransition transition)
    {
        step.FindPropertyRelative("image").objectReferenceValue = image;
        step.FindPropertyRelative("seconds").floatValue = seconds;
        step.FindPropertyRelative("transition").enumValueIndex = (int)transition;
        step.FindPropertyRelative("zoomFocus").boolValue = false;
        step.FindPropertyRelative("blurredImage").objectReferenceValue = null;
    }

    // ───────────────────────── 흐린 버전 그림 만들기

    const int BlurDownscale = 4; // 1/4 크기로 줄여서
    const int BlurRadius = 4;    // 줄인 그림 기준 번짐 반경 (px). 클수록 더 흐림

    /// <summary>일러스트의 흐린 버전을 Assets/Art/Prologue/Blur 에 PNG로 만든다</summary>
    static Sprite MakeBlurred(string name)
    {
        string src = $"{IllustrationFolder}/{name}.png";
        if (!File.Exists(src)) return null;

        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(File.ReadAllBytes(src));
        int sw = tex.width, sh = tex.height;
        var pixels = tex.GetPixels32();
        Object.DestroyImmediate(tex);

        // 1) 4×4 칸씩 평균 내서 작게 줄이기
        int w = Mathf.Max(1, sw / BlurDownscale), h = Mathf.Max(1, sh / BlurDownscale);
        var r = new float[w * h];
        var g = new float[w * h];
        var b = new float[w * h];
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float sr = 0, sg = 0, sb = 0;
            for (int dy = 0; dy < BlurDownscale; dy++)
            for (int dx = 0; dx < BlurDownscale; dx++)
            {
                var c = pixels[Mathf.Min(sh - 1, y * BlurDownscale + dy) * sw + Mathf.Min(sw - 1, x * BlurDownscale + dx)];
                sr += c.r; sg += c.g; sb += c.b;
            }
            float n = BlurDownscale * BlurDownscale * 255f;
            r[y * w + x] = sr / n; g[y * w + x] = sg / n; b[y * w + x] = sb / n;
        }

        // 2) 가로·세로로 평균 번지기를 3번 반복 (부드러운 가우시안 흐림과 비슷해짐)
        for (int pass = 0; pass < 3; pass++)
        {
            BoxBlur(r, w, h); BoxBlur(g, w, h); BoxBlur(b, w, h);
        }

        var colors = new Color[w * h];
        for (int i = 0; i < colors.Length; i++) colors[i] = new Color(r[i], g[i], b[i], 1f);
        var outTex = new Texture2D(w, h, TextureFormat.RGB24, false);
        outTex.SetPixels(colors);
        outTex.Apply();

        string dir = $"{IllustrationFolder}/Blur";
        if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder(IllustrationFolder, "Blur");
        string dst = $"{dir}/{name}.png";
        File.WriteAllBytes(dst, outTex.EncodeToPNG());
        Object.DestroyImmediate(outTex);

        AssetDatabase.ImportAsset(dst, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(dst);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed; // 압축 얼룩 방지 (작은 그림이라 용량 부담 없음)
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(dst);
    }

    // 각 픽셀을 주변 (2×BlurRadius+1)칸 평균으로 (가로 한 번, 세로 한 번)
    static void BoxBlur(float[] ch, int w, int h)
    {
        var tmp = new float[ch.Length];
        float size = BlurRadius * 2 + 1;
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float sum = 0;
            for (int k = -BlurRadius; k <= BlurRadius; k++) sum += ch[y * w + Mathf.Clamp(x + k, 0, w - 1)];
            tmp[y * w + x] = sum / size;
        }
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float sum = 0;
            for (int k = -BlurRadius; k <= BlurRadius; k++) sum += tmp[Mathf.Clamp(y + k, 0, h - 1) * w + x];
            ch[y * w + x] = sum / size;
        }
    }

    // 일러스트를 그림 한 장 통째로(Single) Sprite로 불러온다.
    // (Multiple로 잘라 둔 설정이 남아 있으면 그림을 바꿨을 때 크기가 안 맞아 그림이 사라진다)
    static Sprite LoadIllustration(string name)
    {
        string path = $"{IllustrationFolder}/{name}.png";
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[전환 연출] {path} 이(가) 없습니다");
            return null;
        }
        if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
