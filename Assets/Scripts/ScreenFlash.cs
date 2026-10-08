using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 카메라 플래시처럼 하얀 화면이 순간적으로 확 나왔다가 사라지는 전환 효과 (+ 셔터음).
/// 화면이 완전히 하얀 순간에 다음 화면으로 바꾼다. 씬이 바뀌어도 효과가 이어진다.
/// </summary>
public class ScreenFlash : MonoBehaviour
{
    // 시간 (초) - 실제 시간 기준이라 게임이 멈춰 있어도 동작
    const float FlashIn = 0.04f;  // 하얗게 터지는 시간 (거의 순간)
    const float Hold = 0.12f;     // 완전히 하얀 상태 유지
    const float FadeOut = 0.6f;   // 하얀 화면이 걷히는 시간

    static ScreenFlash instance;

    Image white;
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

            var imageGo = new GameObject("White", typeof(RectTransform));
            imageGo.transform.SetParent(go.transform, false);
            var rect = (RectTransform)imageGo.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            instance = go.AddComponent<ScreenFlash>();
            instance.white = imageGo.AddComponent<Image>();
            instance.SetAlpha(0f);
            return instance;
        }
    }

    /// <summary>플래시를 터뜨리고, 화면이 하얀 순간에 atPeak를 실행한다. 이미 진행 중이면 무시.</summary>
    public static void Play(Action atPeak = null)
    {
        var flash = Instance;
        if (!flash.busy) flash.StartCoroutine(flash.Run(atPeak));
    }

    /// <summary>플래시와 함께 씬 이동</summary>
    public static void LoadScene(string sceneName) => Play(() => SceneManager.LoadScene(sceneName));

    IEnumerator Run(Action atPeak)
    {
        busy = true;
        white.raycastTarget = true; // 효과 중에는 버튼 연타 막기
        ShutterSound.Play();

        for (float t = 0f; t < FlashIn; t += Time.unscaledDeltaTime)
        {
            SetAlpha(t / FlashIn);
            yield return null;
        }
        SetAlpha(1f);

        atPeak?.Invoke();
        yield return new WaitForSecondsRealtime(Hold);

        for (float t = 0f; t < FadeOut; t += Time.unscaledDeltaTime)
        {
            SetAlpha(1f - t / FadeOut);
            yield return null;
        }
        SetAlpha(0f);

        white.raycastTarget = false;
        busy = false;
    }

    void SetAlpha(float a) => white.color = new Color(1f, 1f, 1f, a);
}
