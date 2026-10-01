using UnityEngine;

public enum GameState { Intro, Playing, GameOver, Clear }

/// <summary>
/// 게임 진행 상태(플레이 중 / 게임오버 / 클리어)와 밸런스 수치를 담당.
/// 다른 스크립트보다 먼저 Awake되어야 해서 실행 순서를 앞당겨 둔다.
/// </summary>
[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Tooltip("밸런스 수치 (심박수, 허기, 적, 타이머 등)")]
    [SerializeField] GameBalance balance = new();

    public GameState State { get; private set; } = GameState.Playing;
    public string EndReason { get; private set; } = "";
    public bool IsPlaying => State == GameState.Playing;

    void Awake()
    {
        Instance = this;
        GameBalance.Current = balance;
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
