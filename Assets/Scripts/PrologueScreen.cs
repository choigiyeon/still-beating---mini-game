using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 프롤로그 (스케치 2).
/// 1. 프롤로그 그림(slides)을 한 장당 slideSeconds초씩 자동으로 보여주고, 넘어갈 때마다 플래시 효과
/// 2. 그림이 끝나면 편지 화면 → 화살표를 누르면 심장 사운드 + 플래시와 함께 챕터1로 이동
/// 그림을 비워두면 바로 편지 화면이 나온다.
/// </summary>
public class PrologueScreen : MonoBehaviour
{
    [Tooltip("프롤로그 그림을 보여줄 Image")]
    [SerializeField] Image slideImage;
    [Tooltip("프롤로그 그림들 (순서대로). 기획/아트에서 받은 그림을 여기에 넣기")]
    [SerializeField] Sprite[] slides;
    [Tooltip("그림 한 장당 보여주는 시간 (초)")]
    [SerializeField] float slideSeconds = 5f;
    [Tooltip("그림이 끝난 뒤 나오는 편지 화면")]
    [SerializeField] GameObject letterPage;

    void Start()
    {
        bool hasSlides = slides != null && slides.Length > 0 && slideImage != null;
        if (slideImage != null) slideImage.gameObject.SetActive(hasSlides);
        if (letterPage != null) letterPage.SetActive(!hasSlides);
        if (hasSlides) StartCoroutine(PlaySlides());
    }

    IEnumerator PlaySlides()
    {
        slideImage.sprite = slides[0];
        for (int i = 0; i < slides.Length; i++)
        {
            yield return new WaitForSeconds(slideSeconds);

            int next = i + 1;
            ScreenFlash.Play(() =>
            {
                if (next < slides.Length)
                {
                    slideImage.sprite = slides[next];
                }
                else
                {
                    slideImage.gameObject.SetActive(false);
                    if (letterPage != null) letterPage.SetActive(true);
                }
            });
        }
    }

    /// <summary>편지 화면의 화살표 버튼 OnClick에 연결</summary>
    public void Next()
    {
        HeartbeatSound.Play();
        ScreenFlash.LoadScene(SceneFlow.Chapter1);
    }
}
