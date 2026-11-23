using UnityEngine;

/// <summary>
/// 이어하기 저장. 어디까지 왔는지(챕터)만 저장하고, 이어하기를 누르면 그 챕터 처음부터 시작한다.
/// (챕터 중간 상태 - 심박수, 남은 시간, 음식 - 는 저장하지 않음)
///  - 챕터를 시작하면 그 챕터를 저장
///  - 챕터를 깨면 다음 챕터를 저장 (전환 연출 도중에 꺼도 다음 챕터부터)
///  - 새로하기를 누르면 지움
/// PlayerPrefs라서 컴퓨터(레지스트리)에 남고, 게임을 껐다 켜도 유지된다.
/// </summary>
public static class SaveData
{
    const string Key = "SavedChapter";

    /// <summary>이어할 챕터가 저장돼 있는지</summary>
    public static bool HasSave => SceneFlow.IsChapter(PlayerPrefs.GetString(Key, ""));

    /// <summary>이어할 챕터 씬 이름 (없으면 챕터1)</summary>
    public static string SavedChapter => HasSave ? PlayerPrefs.GetString(Key) : SceneFlow.Chapter1;

    public static void SaveChapter(string chapterScene)
    {
        if (!SceneFlow.IsChapter(chapterScene)) return;
        PlayerPrefs.SetString(Key, chapterScene);
        PlayerPrefs.Save();
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.Save();
    }
}
