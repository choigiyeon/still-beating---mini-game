using UnityEngine;

/// <summary>
/// 프롤로그 편지 화면 (스케치 2). 화살표 버튼 OnClick에 연결한다.
/// 누르면 심장 사운드 + 챕터1 연출로 이동.
/// </summary>
public class PrologueScreen : MonoBehaviour
{
    public void Next()
    {
        HeartbeatSound.Play();
        SceneFlow.Load(SceneFlow.Chapter1);
    }
}
