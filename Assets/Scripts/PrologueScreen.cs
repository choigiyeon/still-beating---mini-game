using UnityEngine;

/// <summary>
/// 프롤로그 편지 화면 (프밍/아트 스케치 2). 흰색 화살표를 누르면 심장 사운드 + 챕터1 연출로 이동.
/// 아트: 약간 오염되고 구겨진 노란 종이(양피지) + 직접 쓴 것 같은 글씨체 (폰트 나오면 교체).
/// </summary>
public class PrologueScreen : MonoBehaviour
{
    static readonly string[] Lines =
    {
        "당신의 탈출을 돕기 위해 몇 글자 써내려 본다.",
        "",
        "1. 그들을 가까이 하지 말것.",
        "2. 심박수 180을 넘기지 말것.",
        "3. 먹을 것들을 꼭 챙겨둘 것.",
        "4. 주머니를 잘 확인하여 제때 영양을 섭취할 것.",
        "",
        "",
        "그럼 하나님의 축복이 함께 하기를.",
    };

    static readonly Color Ink = new(0.12f, 0.1f, 0.08f);

    GUIStyle text, button;

    void Start()
    {
        var cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.06f, 0.07f);
        }
    }

    void OnGUI()
    {
        if (text == null)
        {
            text = new GUIStyle(GUI.skin.label) { fontSize = 34 };
            text.normal.textColor = Ink;
            button = new GUIStyle(GUI.skin.label) { fontSize = 90, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            button.normal.textColor = Color.white;
        }

        float vw = UIUtil.BeginScaled();
        float w = 1400f, h = 900f;
        var paper = new Rect((vw - w) / 2f, 90f, w, h);
        UIUtil.FillRect(paper, new Color(0.9f, 0.82f, 0.6f));

        float x = paper.x + 110f, y = paper.y + 70f;

        GUI.Label(new Rect(x, y, 80, 50), "TO", text);
        UIUtil.FillRect(new Rect(x + 70, y + 12, 200, 30), Ink); // 가려진 이름

        y += 110f;
        foreach (var line in Lines)
        {
            GUI.Label(new Rect(x, y, w - 220f, 50), line, text);
            y += 58f;
        }

        GUI.Label(new Rect(paper.xMax - 420, paper.yMax - 110, 100, 50), "from.", text);
        UIUtil.FillRect(new Rect(paper.xMax - 320, paper.yMax - 98, 200, 30), Ink);

        // 흰색 화살표 (노란 종이 위에서 보이도록 그림자)
        var arrow = new Rect(paper.xMax - 250, paper.yMax - 250, 160, 100);
        button.normal.textColor = new Color(0f, 0f, 0f, 0.6f);
        GUI.Label(new Rect(arrow.x + 3, arrow.y + 3, arrow.width, arrow.height), "→", button);
        button.normal.textColor = Color.white;
        if (GUI.Button(arrow, "→", button))
        {
            HeartbeatSound.Play();
            SceneFlow.Load(SceneFlow.Chapter1);
        }
    }
}
