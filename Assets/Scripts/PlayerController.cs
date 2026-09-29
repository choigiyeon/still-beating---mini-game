using UnityEngine;

/// <summary>
/// 방향키로 360° 자유 이동 + Space 상호작용.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(PlayerStatus))]
public class PlayerController : MonoBehaviour
{
    Rigidbody2D rb;
    PlayerStatus status;
    Vector2 moveInput;

    /// <summary>지금 Space를 누르면 상호작용할 대상 (없으면 null).</summary>
    public Interactable NearestInteractable { get; private set; }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        status = GetComponent<PlayerStatus>();
    }

    void Update()
    {
        if (!GameManager.Instance.IsPlaying)
        {
            moveInput = Vector2.zero;
            NearestInteractable = null;
            return;
        }

        moveInput = new Vector2(Axis(KeyCode.LeftArrow, KeyCode.RightArrow), Axis(KeyCode.DownArrow, KeyCode.UpArrow));
        // 대각선 이동이 더 빨라지지 않도록 길이를 1로 제한
        if (moveInput.sqrMagnitude > 1f) moveInput.Normalize();

        NearestInteractable = Interactable.FindNearest(transform.position, GameBalance.Current.interactRange);
        if (NearestInteractable != null && Input.GetKeyDown(KeyCode.Space))
            NearestInteractable.Interact(status);
    }

    static float Axis(KeyCode negative, KeyCode positive) =>
        (Input.GetKey(positive) ? 1f : 0f) - (Input.GetKey(negative) ? 1f : 0f);

    void FixedUpdate()
    {
        rb.linearVelocity = moveInput * GameBalance.Current.playerMoveSpeed;
    }
}
