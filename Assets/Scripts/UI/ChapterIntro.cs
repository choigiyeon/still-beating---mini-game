using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 챕터 시작 연출 (프밍 스케치 3).
/// 검은 화면 → 블러 처리된 화면 → 검은 화면 → 원래 화면이 서서히 보이며 게임 시작.
/// 연출 중에는 GameState.Intro라서 타이머/심박수/이동이 멈춰 있다.
/// </summary>
public class ChapterIntro : MonoBehaviour
{
    [Tooltip("블러 화면을 보여줄 전체 화면 RawImage")]
    [SerializeField] RawImage blurImage;
    [Tooltip("전체 화면 검정 Image")]
    [SerializeField] Image blackOverlay;

    [Header("시간 (초)")]
    [SerializeField] float blurFadeIn = 0.8f;  // 검은 화면 → 블러 화면
    [SerializeField] float blurHold = 1.2f;    // 블러 화면 유지
    [SerializeField] float blurFadeOut = 0.6f; // 블러 화면 → 검은 화면
    [SerializeField] float dark = 0.5f;        // 검은 화면 유지
    [SerializeField] float fadeIn = 1.2f;      // 검은 화면 → 원래 화면 (서서히)

    [Tooltip("클수록 더 흐림")]
    [SerializeField] int blurDownscale = 10;

    Camera cam;
    RenderTexture blurTexture;
    float time;

    float BlurEnd => blurFadeIn + blurHold + blurFadeOut;

    void Awake() => GameManager.Instance.BeginIntro();

    void Start()
    {
        cam = Camera.main;
        blurTexture = new RenderTexture(
            Mathf.Max(1, Screen.width / blurDownscale), Mathf.Max(1, Screen.height / blurDownscale), 16)
        { filterMode = FilterMode.Bilinear };
        blurImage.texture = blurTexture;
        blurImage.enabled = true;
        blackOverlay.enabled = true;
        Apply();
    }

    void LateUpdate()
    {
        time += Time.unscaledDeltaTime;
        Apply();

        if (time >= BlurEnd + dark + fadeIn)
        {
            blurImage.enabled = false;
            blackOverlay.enabled = false;
            GameManager.Instance.EndIntro();
            enabled = false;
        }
    }

    void Apply()
    {
        // 블러 화면: 저해상도로 한 번 더 렌더링한 뒤 크게 늘려서 보여준다
        bool blurring = time < BlurEnd;
        blurImage.enabled = blurring;
        if (blurring && cam != null)
        {
            cam.targetTexture = blurTexture;
            cam.Render();
            cam.targetTexture = null;
        }

        float black;
        if (time < blurFadeIn) black = 1f - time / blurFadeIn;
        else if (time < blurFadeIn + blurHold) black = 0f;
        else if (time < BlurEnd) black = (time - blurFadeIn - blurHold) / blurFadeOut;
        else if (time < BlurEnd + dark) black = 1f;
        else black = 1f - Mathf.Clamp01((time - BlurEnd - dark) / fadeIn);

        blackOverlay.color = new Color(0f, 0f, 0f, black);
    }

    void OnDestroy()
    {
        if (blurTexture != null) blurTexture.Release();
    }
}
