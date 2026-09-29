using UnityEngine;

/// <summary>
/// 데모용 임시 UI (IMGUI). 배치/색은 아트 스케치 4 기준, 아트가 나오면 UGUI로 교체 예정.
/// 1080p 기준으로 그리고 화면 해상도에 맞춰 스케일한다.
/// </summary>
public class GameHUD : MonoBehaviour
{
    const float RefHeight = UIUtil.RefHeight;
    const float PanelHeight = 150f;
    const int EcgSamples = 160;
    const float EcgSampleInterval = 1f / 60f;

    static readonly Color PanelColor = new(0.2f, 0.2f, 0.22f, 0.97f); // 어두운 회색
    static readonly Color HeartRed = new(0.85f, 0.1f, 0.15f);
    static readonly Color DigitalRed = new(1f, 0.15f, 0.12f);
    static readonly Color PocketColor = new(0.78f, 0.78f, 0.8f);      // 연한 회색
    static readonly Color Green = new(0.4f, 0.9f, 0.5f);

    public PlayerStatus Player;

    readonly float[] ecg = new float[EcgSamples];
    float ecgPhase, ecgTimer;

    GUIStyle label, heartNumber, digital, huge;
    float scale;

    // ───────────────────────── 심전도 파형 (심박수에 맞춰 흐름)

    void Update()
    {
        if (Player == null || !GameManager.Instance.IsPlaying) return;

        ecgTimer += Time.deltaTime;
        while (ecgTimer >= EcgSampleInterval)
        {
            ecgTimer -= EcgSampleInterval;
            ecgPhase = (ecgPhase + EcgSampleInterval * Player.HeartRate / 60f) % 1f;
            System.Array.Copy(ecg, 1, ecg, 0, EcgSamples - 1);
            ecg[EcgSamples - 1] = EcgWave(ecgPhase);
        }
    }

    /// <summary>한 박동(0~1) 동안의 심전도 모양: P파 → QRS → T파</summary>
    static float EcgWave(float p)
    {
        float Bump(float center, float width, float height) =>
            height * Mathf.Exp(-(p - center) * (p - center) / (2f * width * width));

        return Bump(0.12f, 0.025f, 0.12f)
             - Bump(0.27f, 0.008f, 0.15f)
             + Bump(0.3f, 0.01f, 1f)
             - Bump(0.33f, 0.01f, 0.3f)
             + Bump(0.55f, 0.04f, 0.22f);
    }

    // ───────────────────────── 그리기

    void OnGUI()
    {
        if (Player == null || GameManager.Instance.State == GameState.Intro) return;
        EnsureStyles();

        scale = Screen.height / RefHeight;
        float vw = UIUtil.BeginScaled();

        DrawDrawerTimers();
        DrawBottomPanel(vw);
        DrawEndScreen(vw);
    }

    void EnsureStyles()
    {
        if (label != null) return;
        label = new GUIStyle(GUI.skin.label) { fontSize = 22 };
        label.normal.textColor = Color.white;
        heartNumber = new GUIStyle(label) { fontSize = 34, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        digital = new GUIStyle(label) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        digital.normal.textColor = DigitalRed;
        huge = new GUIStyle(label) { fontSize = 90, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
    }

    // 서랍 아래 디지털 타이머 (검정 배경 + 빨간 숫자)
    void DrawDrawerTimers()
    {
        var cam = Camera.main;
        if (cam == null) return;

        foreach (var drawer in FoodDrawer.All)
        {
            Vector3 sp = cam.WorldToScreenPoint(drawer.transform.position + Vector3.down * 0.9f);
            DrawDigitalTimer(new Rect(sp.x / scale - 45f, (Screen.height - sp.y) / scale - 16f, 90f, 32f), drawer.Remaining, digital);
        }
    }

    void DrawBottomPanel(float vw)
    {
        var b = GameBalance.Current;
        float y = RefHeight - PanelHeight;
        UIUtil.FillRect(new Rect(0, y, vw, PanelHeight), PanelColor);

        DrawHeartRate(y);
        DrawNutrition(b, 600f, y);
        DrawPocket(b, vw - 330f, y);
        DrawEscapeTimer(new Rect(vw - 250f, y - 70f, 220f, 56f));
    }

    // 빨간 하트 안에 하얀 숫자 + 검정 배경 심전도
    void DrawHeartRate(float y)
    {
        var heartRect = new Rect(30, y + 15, 130, 120);
        var prev = GUI.color;
        GUI.color = HeartRed;
        GUI.DrawTexture(heartRect, SpriteFactory.Heart);
        GUI.color = prev;
        GUI.Label(new Rect(heartRect.x, heartRect.y - 8, heartRect.width, heartRect.height), $"{Player.HeartRate:0}", heartNumber);

        var graph = new Rect(180, y + 25, 340, 100);
        UIUtil.FillRect(graph, Color.black);
        float mid = graph.y + graph.height * 0.62f, amplitude = graph.height * 0.5f, step = graph.width / (EcgSamples - 1);
        for (int i = 1; i < EcgSamples; i++)
        {
            float y0 = mid - ecg[i - 1] * amplitude, y1 = mid - ecg[i] * amplitude;
            float top = Mathf.Min(y0, y1) - 1f;
            UIUtil.FillRect(new Rect(graph.x + (i - 1) * step, top, step + 1f, Mathf.Abs(y1 - y0) + 2.5f), DigitalRed);
        }
    }

    // 영양: 닭다리 (반 개 단위)
    void DrawNutrition(GameBalance b, float x, float y)
    {
        int slots = Mathf.CeilToInt(b.maxHungerHalves / 2f);
        const float size = 90f, gap = 8f;
        var prev = GUI.color;
        for (int i = 0; i < slots; i++)
        {
            var r = new Rect(x + i * (size + gap), y + 30, size, size);
            int halves = Mathf.Clamp(Player.HungerHalves - i * 2, 0, 2);

            GUI.color = new Color(1f, 1f, 1f, 0.12f);
            GUI.DrawTexture(r, SpriteFactory.Drumstick);
            GUI.color = prev;
            if (halves == 2)
                GUI.DrawTexture(r, SpriteFactory.Drumstick);
            else if (halves == 1)
                GUI.DrawTextureWithTexCoords(new Rect(r.x, r.y, r.width / 2f, r.height), SpriteFactory.Drumstick, new Rect(0, 0, 0.5f, 1));
        }
        GUI.color = prev;
    }

    // 주머니: 연한 회색 칸 2개, 클릭하면 먹기
    void DrawPocket(GameBalance b, float x, float y)
    {
        for (int i = 0; i < b.pocketCapacity; i++)
        {
            var r = new Rect(x + i * 150, y + 25, 130, 100);
            UIUtil.FillRect(r, PocketColor);
            UIUtil.FillRect(new Rect(r.x + 8, r.y + 8, r.width - 16, r.height - 16), new Color(0.68f, 0.68f, 0.7f));

            bool hasFood = i < Player.FoodCount;
            if (!hasFood) continue;
            GUI.DrawTexture(new Rect(r.center.x - 40, r.center.y - 40, 80, 80), SpriteFactory.Drumstick);
            if (GUI.Button(r, GUIContent.none, GUIStyle.none))
                Player.TryEatFood();
        }
    }

    // 전체 탈출 타이머 (문)
    void DrawEscapeTimer(Rect rect)
    {
        if (Door.Instance == null) return;
        var style = new GUIStyle(digital) { fontSize = 40 };
        DrawDigitalTimer(rect, Door.Instance.Remaining, style);
    }

    static void DrawDigitalTimer(Rect rect, float seconds, GUIStyle style)
    {
        UIUtil.FillRect(new Rect(rect.x - 3, rect.y - 3, rect.width + 6, rect.height + 6), new Color(0.35f, 0.35f, 0.37f));
        UIUtil.FillRect(rect, Color.black);
        int total = Mathf.CeilToInt(Mathf.Max(0f, seconds));
        GUI.Label(rect, $"{total / 60:00}:{total % 60:00}", style);
    }

    void DrawEndScreen(float vw)
    {
        var gm = GameManager.Instance;
        if (gm == null || (gm.State != GameState.GameOver && gm.State != GameState.Clear)) return;

        UIUtil.FillRect(new Rect(0, 0, vw, RefHeight), new Color(0, 0, 0, 0.75f));
        bool clear = gm.State == GameState.Clear;
        huge.normal.textColor = clear ? Green : DigitalRed;
        GUI.Label(new Rect(0, 400, vw, 130), clear ? "CLEAR" : "GAME OVER", huge);
    }
}
