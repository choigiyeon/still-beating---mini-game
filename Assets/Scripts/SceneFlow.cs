using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬 이름과 씬 이동.
/// 게임 흐름: Title → Prologue → Chapter1 → Interlude1(챕터1→2 일러스트) → Chapter2
/// </summary>
public static class SceneFlow
{
    public const string Title = "Title";
    public const string Prologue = "Prologue";
    public const string Chapter1 = "Chapter1";
    public const string Interlude1 = "Interlude1";
    public const string Chapter2 = "Chapter2";

    public static void Load(string sceneName)
    {
        Time.timeScale = 1f; // 게임오버/클리어로 멈춘 상태에서 넘어가도 다음 씬이 정상 속도로
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>챕터를 클리어했을 때 넘어갈 씬. 없으면 null (클리어 화면에서 멈춤)</summary>
    public static string NextAfterClear(string chapterScene) => chapterScene switch
    {
        Chapter1 => Interlude1,
        _ => null,
    };
}
