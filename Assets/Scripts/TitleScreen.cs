using UnityEngine;

/// <summary>
/// 타이틀 화면 (프밍/아트 스케치 1).
/// 새로하기 → 프롤로그, 이어하기 → 바로 챕터1 연출. 버튼을 누르면 심장 뛰는 사운드 1회.
/// 제목은 회색(톤다운), 버튼은 약간 투명한 검정 + 심장 아이콘.
/// 왼쪽은 메인 일러스트 자리 (아트 완성 후 교체).
/// </summary>
public class TitleScreen : MonoBehaviour
{
    static readonly Color TitleGray = new(0.62f, 0.62f, 0.64f);
    static readonly Color HeartRed = new(0.85f, 0.1f, 0.15f);

    GUIStyle title, buttonText;

    void Start()
    {
        var cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.12f, 0.13f);
        }
    }

    void OnGUI()
    {
        if (title == null)
        {
            title = new GUIStyle(GUI.skin.label) { fontSize = 96, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            title.normal.textColor = TitleGray;
            buttonText = new GUIStyle(GUI.skin.label) { fontSize = 40, alignment = TextAnchor.MiddleLeft };
            buttonText.normal.textColor = Color.white;
        }

        float vw = UIUtil.BeginScaled();
        float x = vw * 0.55f, w = vw * 0.38f;

        GUI.Label(new Rect(x, 120, w, 360), "STILL BEATING", title);

        if (HeartButton(new Rect(x + w * 0.1f, 560, w * 0.8f, 110), "새로하기"))
            Go(SceneFlow.Prologue);
        if (HeartButton(new Rect(x + w * 0.1f, 710, w * 0.8f, 110), "이어하기"))
            Go(SceneFlow.Chapter1);
    }

    bool HeartButton(Rect rect, string text)
    {
        bool hover = rect.Contains(Event.current.mousePosition);
        UIUtil.FillRect(rect, new Color(0f, 0f, 0f, hover ? 0.8f : 0.55f));

        var prev = GUI.color;
        GUI.color = HeartRed;
        GUI.DrawTexture(new Rect(rect.x + 30, rect.center.y - 30, 64, 60), SpriteFactory.Heart);
        GUI.color = prev;

        GUI.Label(new Rect(rect.x + 120, rect.y, rect.width - 120, rect.height), text, buttonText);
        return GUI.Button(rect, GUIContent.none, GUIStyle.none);
    }

    static void Go(string scene)
    {
        HeartbeatSound.Play();
        SceneFlow.Load(scene);
    }
}
