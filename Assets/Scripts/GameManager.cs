using UnityEngine;

public enum GameState { Intro, Playing, GameOver, Clear }

/// <summary>
/// 게임 진행 상태(플레이 중 / 게임오버 / 클리어)를 담당.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState State { get; private set; } = GameState.Playing;
    public string EndReason { get; private set; } = "";
    public bool IsPlaying => State == GameState.Playing;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
    }

    /// <summary>챕터 시작 연출 동안 게임을 멈춰둔다.</summary>
    public void BeginIntro()
    {
        if (IsPlaying) State = GameState.Intro;
    }

    public void EndIntro()
    {
        if (State == GameState.Intro) State = GameState.Playing;
    }

    public void GameOver(string reason) => End(GameState.GameOver, reason);

    public void Clear(string reason) => End(GameState.Clear, reason);

    void End(GameState state, string reason)
    {
        if (!IsPlaying) return;
        State = state;
        EndReason = reason;
        Time.timeScale = 0f;
    }
}
