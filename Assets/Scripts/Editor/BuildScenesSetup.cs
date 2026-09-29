using System.IO;
using System.Linq;
using UnityEditor;

/// <summary>
/// Build Settings 씬 목록을 Title → Prologue → Chapter1 순서로 맞춰준다.
/// (SceneManager.LoadScene은 목록에 있는 씬만 불러올 수 있다)
/// </summary>
[InitializeOnLoad]
static class BuildScenesSetup
{
    static readonly string[] Scenes =
    {
        "Assets/Scenes/Title.unity",
        "Assets/Scenes/Prologue.unity",
        "Assets/Scenes/Chapter1.unity",
    };

    static BuildScenesSetup() => EditorApplication.delayCall += Apply;

    static void Apply()
    {
        var wanted = Scenes.Where(File.Exists).ToArray();
        var current = EditorBuildSettings.scenes;
        if (current.Take(wanted.Length).Select(s => s.enabled ? s.path : null).SequenceEqual(wanted)) return;

        var others = current.Where(s => !wanted.Contains(s.path) && File.Exists(s.path));
        EditorBuildSettings.scenes = wanted.Select(p => new EditorBuildSettingsScene(p, true)).Concat(others).ToArray();
    }
}
