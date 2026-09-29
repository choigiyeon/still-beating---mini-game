using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬 이름과 씬 이동. 각 씬이 로드되면 그 씬을 구성하는 컴포넌트를 자동으로 붙여준다.
/// </summary>
public static class SceneFlow
{
    public const string Title = "Title";
    public const string Prologue = "Prologue";
    public const string Chapter1 = "Chapter1";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        switch (scene.name)
        {
            case Title: Ensure<TitleScreen>(); break;
            case Prologue: Ensure<PrologueScreen>(); break;
            // Chapter1 + 이름이 다른 씬(에디터에 열린 예전 SampleScene 등)은 챕터1로 구성
            default: Ensure<GameBootstrap>(); break;
        }
    }

    static void Ensure<T>() where T : Component
    {
        if (Object.FindAnyObjectByType<T>() == null)
            new GameObject(typeof(T).Name).AddComponent<T>();
    }

    public static void Load(string sceneName) => SceneManager.LoadScene(sceneName);
}
