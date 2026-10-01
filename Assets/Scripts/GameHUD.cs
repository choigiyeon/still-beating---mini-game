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

    [Header("영양 (닭다리, Image Type = Filled)")]
    [SerializeField] Image[] drumsticks;

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
            drumsticks[i].fillAmount = Mathf.Clamp(player.HungerHalves - i * 2, 0, 2) / 2f;

        for (int i = 0; i < pocketSlots.Length; i++)
        {
            bool hasFood = i < player.FoodCount;
            if (i < pocketFoods.Length) pocketFoods[i].enabled = hasFood;
            pocketSlots[i].interactable = hasFood && gm.IsPlaying;
        }

        if (escapeTimerText != null && Door.Instance != null)
            escapeTimerText.text = FormatTime(Door.Instance.Remaining);

        bool ended = gm.State == GameState.GameOver || gm.State == GameState.Clear;
        if (endScreen != null && ended != endScreen.activeSelf)
        {
            endScreen.SetActive(ended);
            bool clear = gm.State == GameState.Clear;
            endText.text = clear ? "CLEAR" : "GAME OVER";
            endText.color = clear ? clearColor : gameOverColor;
        }
    }

    /// <summary>초 → "mm:ss"</summary>
    public static string FormatTime(float seconds)
    {
        int total = Mathf.CeilToInt(Mathf.Max(0f, seconds));
        return $"{total / 60:00}:{total % 60:00}";
    }
}
