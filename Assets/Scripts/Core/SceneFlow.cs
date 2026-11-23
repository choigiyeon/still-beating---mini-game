using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬 이름과 씬 이동.
/// 게임 흐름: Title → Prologue → Chapter1 → Interlude1(챕터1→2 일러스트) → Chapter2
///          → Interlude2(챕터2→3 일러스트) → Chapter3
/// </summary>
public static class SceneFlow
{
    public const string Title = "Title";
    public const string Prologue = "Prologue";
    public const string Chapter1 = "Chapter1";
    public const string Interlude1 = "Interlude1";
    public const string Chapter2 = "Chapter2";
    public const string Interlude2 = "Interlude2";
    public const string Chapter3 = "Chapter3";

    public static void Load(string sceneName)
    {
        Time.timeScale = 1f; // 게임오버/클리어로 멈춘 상태에서 넘어가도 다음 씬이 정상 속도로
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>챕터를 클리어했을 때 넘어갈 씬. 없으면 null (클리어 화면에서 멈춤)</summary>
    public static string NextAfterClear(string chapterScene) => chapterScene switch
    {
        Chapter1 => Interlude1,
        Chapter2 => Interlude2,
        _ => null,
    };

    /// <summary>챕터를 클리어하면 이어서 하게 될 다음 챕터 (이어하기 저장용). 없으면 null</summary>
    public static string ChapterAfter(string chapterScene) => chapterScene switch
    {
        Chapter1 => Chapter2,
        Chapter2 => Chapter3,
        _ => null,
    };

    /// <summary>플레이하는 챕터 씬인지 (타이틀·프롤로그·전환 연출이 아닌)</summary>
    public static bool IsChapter(string sceneName) =>
        sceneName == Chapter1 || sceneName == Chapter2 || sceneName == Chapter3;

    /// <summary>
    /// 챕터 탈출 연출. true면 창문을 깨고 탈출 (검은 화면 + 유리 깨지는 소리),
    /// false면 문으로 탈출 (하얀 플래시).
    /// 지금은 모든 챕터가 문 (11/21: 맵2 그림이 출입문이라 챕터2도 문으로).
    /// 창문 탈출을 다시 쓰려면 예: chapterScene == Chapter2
    /// </summary>
    public static bool EscapesThroughWindow(string chapterScene) => false;
}
