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

        // 캐릭터끼리는 아래쪽(발이 앞)에 있는 쪽이 앞에 그려지도록 (같은 Sorting Order일 때 y 기준 정렬)
        var cam = Camera.main;
        if (cam != null)
        {
            cam.transparencySortMode = TransparencySortMode.CustomAxis;
            cam.transparencySortAxis = Vector3.up;
        }
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

        // 다음 씬(챕터 사이 일러스트 등)이 있으면 CLEAR 화면 없이 바로 넘어간다
        string scene = SceneManager.GetActiveScene().name;
        string next = SceneFlow.NextAfterClear(scene);
        if (next == null) return;

        GoingToNextScene = true;
        if (SceneFlow.EscapesThroughWindow(scene))
            ScreenFlash.LoadSceneThroughBlack(next, Sfx.GlassBreak, 1.5f); // 창문 깨고 탈출
        else
            ScreenFlash.LoadScene(next); // 문으로 탈출 (하얀 플래시)
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
