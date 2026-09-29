using UnityEngine;

/// <summary>
/// 챕터 시작 연출 (프밍 스케치 3).
/// 검은 화면 → 블러 처리된 화면 → 검은 화면 → 원래 화면이 서서히 보이며 게임 시작.
/// 연출 중에는 GameState.Intro라서 타이머/심박수/이동이 멈춰 있다.
/// </summary>
public class ChapterIntro : MonoBehaviour
{
    const float BlurFadeIn = 0.8f;  // 검은 화면 → 블러 화면
    const float BlurHold = 1.2f;    // 블러 화면 유지
    const float BlurFadeOut = 0.6f; // 블러 화면 → 검은 화면
    const float Dark = 0.5f;        // 검은 화면 유지
    const float FadeIn = 1.2f;      // 검은 화면 → 원래 화면 (서서히)
    const int BlurDownscale = 10;   // 클수록 더 흐림

    const float BlurEnd = BlurFadeIn + BlurHold + BlurFadeOut;
    const float Total = BlurEnd + Dark + FadeIn;

    Camera cam;
    RenderTexture blurTexture;
    float time;

    void Awake() => GameManager.Instance.BeginIntro();

    void Start()
    {
        cam = Camera.main;
        blurTexture = new RenderTexture(
            Mathf.Max(1, Screen.width / BlurDownscale), Mathf.Max(1, Screen.height / BlurDownscale), 16)
        { filterMode = FilterMode.Bilinear };
    }

    void LateUpdate()
    {
        time += Time.unscaledDeltaTime;

        // 블러 화면: 저해상도로 한 번 더 렌더링한 뒤 크게 늘려서 보여준다
        if (time < BlurEnd && cam != null)
        {
            cam.targetTexture = blurTexture;
            cam.Render();
            cam.targetTexture = null;
        }

        if (time >= Total)
        {
            GameManager.Instance.EndIntro();
            Destroy(this);
        }
    }

    void OnDestroy()
    {
        if (blurTexture != null) blurTexture.Release();
    }

    void OnGUI()
    {
        GUI.depth = -100; // HUD보다 위에 그리기
        GUI.matrix = Matrix4x4.identity;
        var full = new Rect(0, 0, Screen.width, Screen.height);
        float t = time;

        if (t < BlurEnd)
        {
            GUI.DrawTexture(full, blurTexture);
            float black = t < BlurFadeIn ? 1f - t / BlurFadeIn
                : t < BlurFadeIn + BlurHold ? 0f
                : (t - BlurFadeIn - BlurHold) / BlurFadeOut;
            UIUtil.FillRect(full, new Color(0, 0, 0, black));
        }
        else if (t < BlurEnd + Dark)
        {
            UIUtil.FillRect(full, Color.black);
        }
        else
        {
            UIUtil.FillRect(full, new Color(0, 0, 0, 1f - Mathf.Clamp01((t - BlurEnd - Dark) / FadeIn)));
        }
    }
}
