using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 타이틀 화면 (스케치 1). 버튼 OnClick에 연결한다.
///  - 새로하기 → 저장을 지우고 프롤로그부터
///  - 이어하기 → 저장된 챕터 처음부터 (저장이 없으면 버튼이 흐려지고 눌리지 않음)
/// 버튼을 누르면 심장 뛰는 사운드 1회 + 플래시 전환.
/// </summary>
public class TitleScreen : MonoBehaviour
{
    [Tooltip("이어하기 버튼. 저장이 없으면 흐리게 하고 못 누르게 한다")]
    [SerializeField] Button continueButton;
    [Tooltip("저장이 없을 때 이어하기 버튼의 투명도")]
    [SerializeField, Range(0f, 1f)] float disabledAlpha = 0.35f;

    void Start()
    {
        if (continueButton == null) return;
        bool hasSave = SaveData.HasSave;
        continueButton.interactable = hasSave;

        // 버튼 글자·아이콘까지 같이 흐리게
        if (!continueButton.TryGetComponent(out CanvasGroup group)) group = continueButton.gameObject.AddComponent<CanvasGroup>();
        group.alpha = hasSave ? 1f : disabledAlpha;
    }

    public void NewGame()
    {
        SaveData.Clear();
        Go(SceneFlow.Prologue);
    }

    public void Continue()
    {
        if (!SaveData.HasSave) return;
        Go(SaveData.SavedChapter);
    }

    static void Go(string scene)
    {
        Sfx.Heartbeat();
        ScreenFlash.LoadScene(scene);
    }
}
