using UnityEngine;

/// <summary>
/// 문. 타이머(기본 5분)가 0이 되면 열리고, 열린 문에 들어가면 클리어.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Door : MonoBehaviour
{
    public static Door Instance { get; private set; }

    static readonly Color ClosedColor = new(0.42f, 0.45f, 0.5f); // 철문
    static readonly Color OpenColor = new(0.3f, 0.85f, 0.45f);

    SpriteRenderer sr;

    public float Remaining { get; private set; }
    public bool IsOpen { get; private set; }

    void Awake()
    {
        Instance = this;
        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        Remaining = GameBalance.Current.doorOpenSeconds;
        sr.color = ClosedColor;
    }

    void Update()
    {
        if (IsOpen || !GameManager.Instance.IsPlaying) return;

        Remaining -= Time.deltaTime;
        if (Remaining <= 0f) Open();
    }

    void Open()
    {
        Remaining = 0f;
        IsOpen = true;
        sr.color = OpenColor;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (IsOpen && other.GetComponent<PlayerController>() != null)
            GameManager.Instance.Clear("열린 문으로 탈출에 성공했다.");
    }
}
