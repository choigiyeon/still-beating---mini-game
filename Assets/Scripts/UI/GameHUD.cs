using UnityEngine;
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
        }
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
