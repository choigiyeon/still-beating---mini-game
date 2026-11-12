using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>이 장면으로 넘어올 때 효과</summary>
public enum SlideTransition
{
    Flash,          // 하얀 플래시 + 셔터음
    Cut,            // 바로 바뀜 (같은 그림 확대, 검은 화면 등)
    FadeFromBlack,  // 검은 화면에서 서서히 보임
}

/// <summary>연출 한 단계 (그림 한 장 또는 검은 화면)</summary>
[Serializable]
public class SlideStep
{
    [Tooltip("보여줄 그림. 비워두면 검은 화면")]
    public Sprite image;
    [Tooltip("보여주는 시간 (초)")]
    public float seconds = 2.5f;
    [Tooltip("이 장면으로 넘어올 때 효과")]
    public SlideTransition transition = SlideTransition.Flash;

    [Header("확대 연출 (흔들리다 멈추며 초점이 맞음)")]
    public bool zoomFocus;
    [Tooltip("확대할 위치. 그림 기준 0~1 (왼쪽 아래 = 0,0 / 가운데 = 0.5,0.5)")]
    public Vector2 zoomCenter = new(0.5f, 0.5f);
    [Tooltip("최종 확대 배율")]
    public float zoomScale = 2f;
    [Tooltip("확대되는 데 걸리는 시간 (초). 0이면 처음부터 확대된 상태로 보임")]
    public float zoomSeconds = 0.8f;
    [Tooltip("흔들리는 시간 (초). 점점 약해지다 멈춤")]
    public float shakeSeconds = 1.2f;
    [Tooltip("흔들림 세기 (px)")]
    public float shakeStrength = 25f;
    [Tooltip("흔들림 빠르기. 클수록 빠르게 덜덜, 작을수록 천천히 휘청")]
    public float shakeSpeed = 12f;
    [Tooltip("흔들릴 때 화면이 기우는 최대 각도 (도). 휘청이는 느낌")]
    public float shakeTilt = 0f;
    [Tooltip("흐릿하다가 선명해지는 데 걸리는 시간 (초). 확대·흔들림과 동시에 시작")]
    public float focusSeconds = 1.5f;
    [Tooltip("흐린 버전 그림 (메뉴 [챕터 전환 연출 적용]이 자동으로 만듦). 비워두면 흐림 효과 없음")]
    public Sprite blurredImage;
}

/// <summary>
/// 일러스트 연출 화면. 프롤로그와 챕터 사이 전환(Interlude) 씬에서 같이 쓴다.
///
/// [단계(Steps) 방식] steps를 채우면 단계마다 그림/검은 화면, 시간, 전환 효과, 확대 연출을 따로 정할 수 있다.
///   예) 챕터2→3: 사진1 → 사진2 → 사진3 → 사진3 문 확대 → 검은 화면 → 사진4(짧게) → 검은 화면 → 챕터3
///
/// [간단 방식] steps가 비어 있으면 slides를 한 장당 slideSeconds초씩, 넘어갈 때마다 플래시.
///
/// 끝나면 편지 화면(letterPage)이 있으면 편지 → 화살표로 다음 씬 (프롤로그),
/// 없으면 바로 다음 씬 (챕터 사이 전환).
/// </summary>
public class SlideshowScreen : MonoBehaviour
{
    [Tooltip("일러스트를 보여줄 Image")]
    [SerializeField] Image slideImage;

    [Header("단계 방식 (채우면 아래 간단 방식 대신 사용)")]
    [SerializeField] SlideStep[] steps;

    [Header("간단 방식")]
    [Tooltip("일러스트들 (순서대로). 기획/아트에서 받은 그림을 여기에 넣기")]
    [SerializeField] Sprite[] slides;
    [Tooltip("일러스트 한 장당 보여주는 시간 (초)")]
    [SerializeField] float slideSeconds = 5f;

    [Header("끝나고")]
    [Tooltip("일러스트가 끝난 뒤 나오는 편지 화면 (비워두면 바로 다음 씬으로)")]
    [SerializeField] GameObject letterPage;
    [Tooltip("끝나면 넘어갈 씬 이름")]
    [SerializeField] string nextScene = SceneFlow.Chapter1;

    Image blurImage;

    void Start()
    {
        bool hasSteps = steps != null && steps.Length > 0 && slideImage != null;
        bool hasSlides = !hasSteps && slides != null && slides.Length > 0 && slideImage != null;

        if (slideImage != null) slideImage.gameObject.SetActive(hasSteps || hasSlides);
        if (letterPage != null) letterPage.SetActive(!hasSteps && !hasSlides);

        if (hasSteps) StartCoroutine(PlaySteps());
        else if (hasSlides) StartCoroutine(PlaySlides());
        else if (letterPage == null) ScreenFlash.LoadScene(nextScene);
    }

    // ───────────────────────── 간단 방식

    IEnumerator PlaySlides()
    {
        slideImage.sprite = slides[0];
        for (int i = 0; i < slides.Length; i++)
        {
            yield return new WaitForSeconds(slideSeconds);

            int next = i + 1;
            if (next < slides.Length)
                ScreenFlash.Play(() => slideImage.sprite = slides[next]);
            else
                Finish();
        }
    }

    // ───────────────────────── 단계 방식

    IEnumerator PlaySteps()
    {
        for (int i = 0; i < steps.Length; i++)
        {
            var step = steps[i];
            if (i == 0) Show(step); // 첫 장면은 바로 (앞 씬의 전환 효과가 덮고 있음)
            else ShowWith(step);
            yield return new WaitForSeconds(step.seconds);
        }

        // 마지막이 검은 화면이면 그대로 다음 씬으로, 아니면 끝 처리(플래시 또는 편지)
        if (steps[^1].image == null && letterPage == null) SceneFlow.Load(nextScene);
        else Finish();
    }

    void ShowWith(SlideStep step)
    {
        switch (step.transition)
        {
            case SlideTransition.Flash:
                ScreenFlash.Play(() => Show(step));
                break;
            case SlideTransition.FadeFromBlack:
                ScreenFlash.Run(Color.black, 0f, 0f, 0.5f, null, () => Show(step));
                break;
            default:
                Show(step);
                break;
        }
    }

    Coroutine zoomRoutine;

    // 화면이 실제로 바뀌는 순간 (플래시라면 하얗게 덮인 순간)
    void Show(SlideStep step)
    {
        if (zoomRoutine != null) StopCoroutine(zoomRoutine);
        zoomRoutine = null;

        var rt = slideImage.rectTransform;
        rt.localScale = Vector3.one;
        rt.anchoredPosition = Vector2.zero;
        rt.localRotation = Quaternion.identity;
        if (blurImage != null) blurImage.enabled = false;

        // 그림이 없으면 검은 화면 (Interlude 씬 카메라 배경이 검정)
        slideImage.enabled = step.image != null;
        if (step.image == null) return;

        slideImage.sprite = step.image;
        if (step.zoomFocus) zoomRoutine = StartCoroutine(ZoomFocus(step));
    }

    // 동시에: 문 쪽으로 확대 + 살짝 흔들리다 문이 가운데 오면 멈춤 / 흐릿하다가 서서히 선명해짐 (초점이 맞음)
    IEnumerator ZoomFocus(SlideStep step)
    {
        var rt = slideImage.rectTransform;
        Vector2 size = rt.rect.size;
        Vector2 target = -(step.zoomCenter - new Vector2(0.5f, 0.5f)) * size * step.zoomScale;
        bool blur = PrepareBlur(step.blurredImage);

        float total = Mathf.Max(step.zoomSeconds, step.shakeSeconds, blur ? step.focusSeconds : 0f);
        float seed = UnityEngine.Random.value * 100f;
        for (float t = 0f; t < total; t += Time.deltaTime)
        {
            float z = step.zoomSeconds > 0f ? Smooth(Mathf.Clamp01(t / step.zoomSeconds)) : 1f;

            // 흔들림: 처음엔 크게 → 점점 약해지다 멈춤. 느린 노이즈로 휘청이듯 밀리고 기울어짐
            float decay = step.shakeSeconds > 0f ? 1f - Smooth(Mathf.Clamp01(t / step.shakeSeconds)) : 0f;
            float n = t * step.shakeSpeed;
            var shake = new Vector2(Mathf.PerlinNoise(seed, n) - 0.5f, Mathf.PerlinNoise(seed + 50f, n) - 0.5f) * 2f * step.shakeStrength * decay;
            float tilt = (Mathf.PerlinNoise(seed + 100f, n) - 0.5f) * 2f * step.shakeTilt * decay;

            rt.localScale = Vector3.one * Mathf.Lerp(1f, step.zoomScale, z);
            rt.anchoredPosition = Vector2.Lerp(Vector2.zero, target, z) + shake;
            rt.localRotation = Quaternion.Euler(0f, 0f, tilt);

            // 처음엔 완전히 흐림 → 서서히 선명 (처음엔 천천히, 중간에 빠르게, 끝에 다시 천천히 맞음)
            if (blur)
                blurImage.color = new Color(1f, 1f, 1f, 1f - Smooth(Mathf.Clamp01(t / Mathf.Max(0.01f, step.focusSeconds))));
            yield return null;
        }

        rt.localScale = Vector3.one * step.zoomScale;
        rt.anchoredPosition = target;
        rt.localRotation = Quaternion.identity;
        if (blurImage != null) blurImage.enabled = false;
    }

    // 흐린 버전 그림을 원래 그림 위에 같은 크기로 겹쳐 둔다 (투명도로 흐림 정도 조절)
    bool PrepareBlur(Sprite blurred)
    {
        if (blurred == null) return false;
        if (blurImage == null)
        {
            var go = new GameObject("Blur", typeof(RectTransform));
            go.transform.SetParent(slideImage.transform, false);
            var r = (RectTransform)go.transform;
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
            blurImage = go.AddComponent<Image>();
            blurImage.raycastTarget = false;
            blurImage.preserveAspect = true; // 원래 그림과 같은 비율·위치로 그려짐
        }
        blurImage.sprite = blurred;
        blurImage.color = Color.white; // 시작은 완전히 흐린 상태
        blurImage.enabled = true;
        return true;
    }

    static float Smooth(float x) => x * x * (3f - 2f * x);

    // ───────────────────────── 끝

    void Finish()
    {
        if (letterPage != null)
        {
            ScreenFlash.Play(() =>
            {
                slideImage.gameObject.SetActive(false);
                letterPage.SetActive(true);
            });
        }
        else
        {
            ScreenFlash.LoadScene(nextScene);
        }
    }

    /// <summary>편지 화면의 화살표 버튼 OnClick에 연결</summary>
    public void Next()
    {
        Sfx.Heartbeat();
        ScreenFlash.LoadScene(nextScene);
    }
}
