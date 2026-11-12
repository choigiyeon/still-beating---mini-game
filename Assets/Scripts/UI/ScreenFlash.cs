using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 전환 효과. 씬이 바뀌어도 효과가 이어진다.
///  - Play / LoadScene: 카메라 플래시처럼 하얀 화면이 순간적으로 확 나왔다가 사라짐 (+ 셔터음)
///  - LoadSceneThroughBlack: 화면이 바로 까매지고(+ 효과음) 잠시 뒤 다음 씬이 서서히 보임 (창문 탈출 등)
/// 화면이 완전히 덮인 순간에 다음 화면으로 바꾼다.
/// </summary>
public class ScreenFlash : MonoBehaviour
{
    // 하얀 플래시 시간 (초) - 실제 시간 기준이라 게임이 멈춰 있어도 동작
    const float FlashIn = 0.04f;  // 하얗게 터지는 시간 (거의 순간)
    const float FlashHold = 0.12f; // 완전히 하얀 상태 유지
    const float FlashOut = 0.6f;  // 하얀 화면이 걷히는 시간

    static ScreenFlash instance;

    Image cover;
    bool busy;

    static ScreenFlash Instance
    {
        get
        {
            if (instance != null) return instance;

            var go = new GameObject("ScreenFlash");
            DontDestroyOnLoad(go);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // 모든 UI 위
            go.AddComponent<GraphicRaycaster>();

            var imageGo = new GameObject("Cover", typeof(RectTransform));
            imageGo.transform.SetParent(go.transform, false);
            var rect = (RectTransform)imageGo.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            instance = go.AddComponent<ScreenFlash>();
            instance.cover = imageGo.AddComponent<Image>();
            instance.cover.color = Color.clear;
            instance.cover.raycastTarget = false;
            return instance;
        }
    }

    /// <summary>하얀 플래시(+ 셔터음)를 터뜨리고, 화면이 하얀 순간에 atPeak를 실행한다. 이미 진행 중이면 무시.</summary>
    public static void Play(Action atPeak = null) =>
        Run(Color.white, FlashIn, FlashHold, FlashOut, Sfx.Shutter, atPeak);

    /// <summary>하얀 플래시와 함께 씬 이동</summary>
    public static void LoadScene(string sceneName) => Play(() => SceneFlow.Load(sceneName));

    /// <summary>화면이 바로 까매지고(+ sound) blackSeconds 뒤 다음 씬이 서서히 보인다</summary>
    public static void LoadSceneThroughBlack(string sceneName, Action sound, float blackSeconds = 1.2f, float fadeOut = 0.6f) =>
        Run(Color.black, 0f, blackSeconds, fadeOut, sound, () => SceneFlow.Load(sceneName));

    /// <summary>
    /// 범용 전환: color로 fadeIn초 동안 덮고 → atPeak 실행 → hold초 유지 → fadeOut초 동안 걷힘.
    /// 이미 진행 중이면 무시한다.
    /// </summary>
    public static void Run(Color color, float fadeIn, float hold, float fadeOut, Action sound, Action atPeak)
    {
        var flash = Instance;
        if (!flash.busy) flash.StartCoroutine(flash.Sequence(color, fadeIn, hold, fadeOut, sound, atPeak));
    }

    IEnumerator Sequence(Color color, float fadeIn, float hold, float fadeOut, Action sound, Action atPeak)
    {
        busy = true;
        cover.raycastTarget = true; // 효과 중에는 버튼 연타 막기
        sound?.Invoke();

        for (float t = 0f; t < fadeIn; t += Time.unscaledDeltaTime)
        {
            SetCover(color, t / fadeIn);
            yield return null;
        }
        SetCover(color, 1f);

        atPeak?.Invoke();
        yield return new WaitForSecondsRealtime(hold);

        for (float t = 0f; t < fadeOut; t += Time.unscaledDeltaTime)
        {
            SetCover(color, 1f - t / fadeOut);
            yield return null;
        }
        SetCover(color, 0f);

        cover.raycastTarget = false;
        busy = false;
    }

    void SetCover(Color color, float alpha) => cover.color = new Color(color.r, color.g, color.b, alpha);
}
