using UnityEngine;

/// <summary>
/// 타이틀 화면 (스케치 1). 버튼 OnClick에 연결한다.
/// 새로하기 → 프롤로그, 이어하기 → 바로 챕터1 연출. 버튼을 누르면 심장 뛰는 사운드 1회.
/// </summary>
public class TitleScreen : MonoBehaviour
{
    public void NewGame() => Go(SceneFlow.Prologue);

    public void Continue() => Go(SceneFlow.Chapter1);

    static void Go(string scene)
    {
        HeartbeatSound.Play();
        SceneFlow.Load(scene);
    }
}
