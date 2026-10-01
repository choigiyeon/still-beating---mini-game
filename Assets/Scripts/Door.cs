using UnityEngine;

/// <summary>
/// 문. 타이머(기본 5분)가 0이 되면 열리고, 열린 문에 들어가면 클리어.
/// 열림 스프라이트를 넣으면 스프라이트를 바꾸고, 비워두면 색으로 표시한다.
/// </summary>
public class Door : MonoBehaviour
{
    public static Door Instance { get; private set; }

    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] Sprite openSprite;
    [SerializeField] Color openColor = new(0.3f, 0.85f, 0.45f);

    public float Remaining { get; private set; }
    public bool IsOpen { get; private set; }

    void Awake() => Instance = this;

    void Start() => Remaining = GameBalance.Current.doorOpenSeconds;

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
        if (spriteRenderer == null) return;
        if (openSprite != null) spriteRenderer.sprite = openSprite;
        else spriteRenderer.color = openColor;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (IsOpen && other.GetComponent<PlayerController>() != null)
            GameManager.Instance.Clear("열린 문으로 탈출에 성공했다.");
    }
}
