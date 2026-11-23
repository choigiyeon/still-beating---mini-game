using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 챕터 HUD (Canvas). 배치/이미지는 씬에서 수정하고, 이 스크립트는 값만 갱신한다.
/// </summary>
public class GameHUD : MonoBehaviour
{
    [SerializeField] PlayerStatus player;

    [Tooltip("연출 중에는 숨길 HUD 묶음")]
    [SerializeField] GameObject hudRoot;

    [Header("심박수")]
    [SerializeField] Text heartRateText;

    [Header("영양 (닭다리)")]
    [SerializeField] Image[] drumsticks;
    [Tooltip("닭다리 그림 (온전한 것 / 반 개). 둘 다 넣으면 그림을 바꿔서 표시, 비워두면 Image Type = Filled로 잘라서 표시")]
    [SerializeField] Sprite drumstickFull;
    [SerializeField] Sprite drumstickHalf;

    [Header("주머니")]
    [SerializeField] Button[] pocketSlots;
    [SerializeField] Image[] pocketFoods;

    [Header("탈출 타이머")]
    [SerializeField] Text escapeTimerText;

    [Header("종료 화면")]
    [SerializeField] GameObject endScreen;
    [SerializeField] Text endText;
    [SerializeField] Color clearColor = new(0.4f, 0.9f, 0.5f);
    [SerializeField] Color gameOverColor = new(1f, 0.15f, 0.12f);
    [Tooltip("다시하기 버튼. 게임오버 창에서만 보이고, 엔딩(마지막 챕터 클리어) 창에서는 숨김")]
    [SerializeField] GameObject retryButton;
    [Tooltip("종료 화면이 뜨고 버튼을 누를 수 있게 되기까지 (초). 뜨자마자 실수로 눌리지 않게")]
    [SerializeField] float returnDelay = 0.5f;

    float endedAt = -1f;
    bool leaving;

    /// <summary>주머니 칸 버튼에서 호출</summary>
    public void EatFood() => player.TryEatFood();

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || player == null) return;

        if (hudRoot != null) hudRoot.SetActive(gm.State != GameState.Intro);

        if (heartRateText != null) heartRateText.text = $"{player.HeartRate:0}";

        for (int i = 0; i < drumsticks.Length; i++)
            ShowDrumstick(drumsticks[i], Mathf.Clamp(player.HungerHalves - i * 2, 0, 2));

        for (int i = 0; i < pocketSlots.Length; i++)
        {
            bool hasFood = i < player.FoodCount;
            if (i < pocketFoods.Length) pocketFoods[i].enabled = hasFood;
            pocketSlots[i].interactable = hasFood && gm.IsPlaying;
        }

        if (escapeTimerText != null && Door.Instance != null)
            escapeTimerText.text = FormatTime(Door.Instance.Remaining);

        bool ended = (gm.State == GameState.GameOver || gm.State == GameState.Clear) && !gm.GoingToNextScene;
        if (endScreen != null && ended != endScreen.activeSelf)
        {
            endScreen.SetActive(ended);
            bool clear = gm.State == GameState.Clear;
            endText.text = clear ? "CLEAR" : "GAME OVER";
            endText.color = clear ? clearColor : gameOverColor;
            if (retryButton != null) retryButton.SetActive(!clear);
        }

        if (ended && endedAt < 0f) endedAt = Time.unscaledTime; // 게임이 멈춰 있어서 실제 시간 기준
    }

    /// <summary>종료 화면의 메인화면 버튼 OnClick에 연결. 게임오버·엔딩 창 → 처음 화면(타이틀)</summary>
    public void ReturnToTitle() => Leave(SceneFlow.Title);

    /// <summary>게임오버 창의 다시하기 버튼 OnClick에 연결. 지금 챕터 처음부터</summary>
    public void Retry() => Leave(SceneManager.GetActiveScene().name);

    void Leave(string scene)
    {
        if (leaving || endedAt < 0f || Time.unscaledTime - endedAt < returnDelay) return;
        leaving = true;
        Sfx.Heartbeat();
        ScreenFlash.LoadScene(scene);
    }

    // halves: 0 = 없음, 1 = 반 개, 2 = 온전한 닭다리
    void ShowDrumstick(Image image, int halves)
    {
        if (drumstickFull == null || drumstickHalf == null)
        {
            image.fillAmount = halves / 2f;
            return;
        }

        image.enabled = halves > 0;
        var sprite = halves == 2 ? drumstickFull : drumstickHalf;
        if (halves > 0 && image.sprite != sprite)
        {
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.SetNativeSize(); // 반 개 그림은 크기가 달라서 원본 크기로
        }
    }

    /// <summary>초 → "mm:ss"</summary>
    public static string FormatTime(float seconds)
    {
        int total = Mathf.CeilToInt(Mathf.Max(0f, seconds));
        return $"{total / 60:00}:{total % 60:00}";
    }
}
