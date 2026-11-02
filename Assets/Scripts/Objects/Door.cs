using System.Collections;
using UnityEngine;

/// <summary>
/// 문(철문). 타이머(기본 5분)가 0이 되면 문짝이 옆으로 밀려 열리고, 다 열린 문에 들어가면 클리어.
/// 문짝 뒤의 통로(Doorway)는 프리팹에 따로 있어서, 문짝이 밀리면 드러난다.
/// </summary>
public class Door : MonoBehaviour
{
    public static Door Instance { get; private set; }

    [Tooltip("옆으로 밀려 열리는 문짝")]
    [SerializeField] SpriteRenderer spriteRenderer;
    [Tooltip("다 열렸을 때 바꿀 문짝 스프라이트 (비워두면 그대로)")]
    [SerializeField] Sprite openSprite;
    [Tooltip("열리는 데 걸리는 시간 (초)")]
    [SerializeField] float slideDuration = 1.2f;
    [Tooltip("오른쪽으로 밀리는 거리 (0이면 문짝 너비만큼, 음수면 왼쪽)")]
    [SerializeField] float slideDistance = 0f;

    public float Remaining { get; private set; }
    /// <summary>문짝이 끝까지 밀려 완전히 열린 상태</summary>
    public bool IsOpen { get; private set; }

    bool opening;

    void Awake() => Instance = this;

    void Start() => Remaining = GameBalance.Current.doorOpenSeconds;

    void Update()
    {
        if (opening || !GameManager.Instance.IsPlaying) return;

        Remaining -= Time.deltaTime;
        if (Remaining <= 0f)
        {
            Remaining = 0f;
            opening = true;
            StartCoroutine(SlideOpen());
        }
    }

    IEnumerator SlideOpen()
    {
        if (spriteRenderer != null)
        {
            var panel = spriteRenderer.transform;
            float distance = slideDistance != 0f ? slideDistance : panel.localScale.x;
            Vector3 start = panel.localPosition, end = start + Vector3.right * distance;

            for (float t = 0f; t < slideDuration; t += Time.deltaTime)
            {
                float k = t / slideDuration;
                panel.localPosition = Vector3.Lerp(start, end, k * k * (3f - 2f * k));
                yield return null;
            }
            panel.localPosition = end;
            if (openSprite != null) spriteRenderer.sprite = openSprite;
        }
        IsOpen = true;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (IsOpen && other.GetComponent<PlayerController>() != null)
            GameManager.Instance.Clear("열린 문으로 탈출에 성공했다.");
    }
}
