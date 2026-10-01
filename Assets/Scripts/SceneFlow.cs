using UnityEngine.SceneManagement;

/// <summary>
/// 씬 이름과 씬 이동.
/// </summary>
public static class SceneFlow
{
    public const string Title = "Title";
    public const string Prologue = "Prologue";
    public const string Chapter1 = "Chapter1";

    public static void Load(string sceneName) => SceneManager.LoadScene(sceneName);
}
