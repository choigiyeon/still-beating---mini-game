using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 일러스트 슬라이드 화면. 프롤로그와 챕터 사이 전환(Interlude) 씬에서 같이 쓴다.
/// 1. 일러스트(slides)를 한 장당 slideSeconds초씩 자동으로 보여주고, 넘어갈 때마다 플래시 효과
/// 2. 일러스트가 끝나면
///    - 편지 화면(letterPage)이 있으면 편지 → 화살표를 누르면 다음 씬 (프롤로그)
///    - 편지 화면이 없으면 바로 플래시와 함께 다음 씬 (챕터 사이 전환)
/// </summary>
public class PrologueScreen : MonoBehaviour
{
    [Tooltip("일러스트를 보여줄 Image")]
    [SerializeField] Image slideImage;
    [Tooltip("일러스트들 (순서대로). 기획/아트에서 받은 그림을 여기에 넣기")]
    [SerializeField] Sprite[] slides;
    [Tooltip("일러스트 한 장당 보여주는 시간 (초)")]
    [SerializeField] float slideSeconds = 5f;
    [Tooltip("일러스트가 끝난 뒤 나오는 편지 화면 (비워두면 바로 다음 씬으로)")]
    [SerializeField] GameObject letterPage;
    [Tooltip("끝나면 넘어갈 씬 이름")]
    [SerializeField] string nextScene = SceneFlow.Chapter1;

    void Start()
    {
        bool hasSlides = slides != null && slides.Length > 0 && slideImage != null;
        if (slideImage != null) slideImage.gameObject.SetActive(hasSlides);
        if (letterPage != null) letterPage.SetActive(!hasSlides);

        if (hasSlides) StartCoroutine(PlaySlides());
        else if (letterPage == null) ScreenFlash.LoadScene(nextScene);
    }

    IEnumerator PlaySlides()
    {
        slideImage.sprite = slides[0];
        for (int i = 0; i < slides.Length; i++)
        {
            yield return new WaitForSeconds(slideSeconds);

            int next = i + 1;
            if (next < slides.Length)
                ScreenFlash.Play(() => slideImage.sprite = slides[next]);
            else if (letterPage != null)
                ScreenFlash.Play(() =>
                {
                    slideImage.gameObject.SetActive(false);
                    letterPage.SetActive(true);
                });
            else
                ScreenFlash.LoadScene(nextScene);
        }
    }

    /// <summary>편지 화면의 화살표 버튼 OnClick에 연결</summary>
    public void Next()
    {
        HeartbeatSound.Play();
        ScreenFlash.LoadScene(nextScene);
    }
}
