using UnityEngine;
using UnityEngine.SceneManagement;

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

    [Header("테스트")]
    [Tooltip("적 범위(초록/주황 원) 표시. 테스트할 때만 켜고, 실제 플레이용으로는 끄기. Play 중에도 바로 바뀜")]
    [SerializeField] bool showEnemyRanges = true;

    public bool ShowEnemyRanges => showEnemyRanges;

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

    public void Clear(string reason)
    {
        if (!IsPlaying) return;
        End(GameState.Clear, reason);

        // 다음 씬(챕터 사이 일러스트 등)이 있으면 CLEAR 화면 없이 바로 플래시와 함께 넘어간다
        string next = SceneFlow.NextAfterClear(SceneManager.GetActiveScene().name);
        if (next != null)
        {
            GoingToNextScene = true;
            ScreenFlash.LoadScene(next);
        }
    }

    /// <summary>클리어 후 다음 씬으로 넘어가는 중 (이때는 CLEAR 화면을 띄우지 않음)</summary>
    public bool GoingToNextScene { get; private set; }

    void End(GameState state, string reason)
    {
        if (!IsPlaying) return;
        State = state;
        EndReason = reason;
        Time.timeScale = 0f;
    }
}
