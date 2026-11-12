using UnityEngine;

/// <summary>
/// 주인공 걷기 애니메이션 (그림 바꾸기 방식).
///  - 움직이는 방향에 따라 아래(정면) / 위(뒷모습) / 옆 그림
///  - 걷는 동안 "서기 ↔ 발 내딛기" 그림을 stepInterval초마다 번갈아
///  - 옆 그림은 한쪽만 그리고, 반대쪽은 좌우반전
///  - 멈추면 마지막 방향의 서기 그림
/// 챕터마다 그림이 다르면(예: 챕터2 상처 난 주인공) 그 챕터 씬의 주인공에서 그림만 바꾸면 된다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerAnimator : MonoBehaviour
{
    [SerializeField] SpriteRenderer spriteRenderer;

    [Header("아래 (정면)")]
    [SerializeField] Sprite downIdle;
    [SerializeField] Sprite downWalk;

    [Header("위 (뒷모습)")]
    [SerializeField] Sprite upIdle;
    [SerializeField] Sprite upWalk;

    [Header("옆")]
    [SerializeField] Sprite sideIdle;
    [SerializeField] Sprite sideWalk;
    [Tooltip("옆 그림이 왼쪽을 보고 있으면 체크 (오른쪽으로 갈 때 좌우반전)")]
    [SerializeField] bool sideFacesLeft = true;

    [Tooltip("걸을 때 그림을 바꾸는 간격 (초). 작을수록 빨리 걷는 느낌")]
    [SerializeField] float stepInterval = 0.2f;

    enum Facing { Down, Up, Side }

    Rigidbody2D rb;
    Facing facing = Facing.Down;
    bool lookingRight;
    bool stepFrame;
    float timer;

    void Awake() => rb = GetComponent<Rigidbody2D>();

    void Update()
    {
        if (spriteRenderer == null || downIdle == null) return; // 그림이 없으면 임시 도형 그대로

        Vector2 velocity = rb.linearVelocity;
        bool moving = velocity.sqrMagnitude > 0.01f;

        if (moving)
        {
            // 더 많이 움직이는 축의 방향 그림 (대각선은 옆 그림)
            if (Mathf.Abs(velocity.x) >= Mathf.Abs(velocity.y))
            {
                facing = Facing.Side;
                lookingRight = velocity.x > 0f;
            }
            else
            {
                facing = velocity.y > 0f ? Facing.Up : Facing.Down;
            }

            timer += Time.deltaTime;
            if (timer >= stepInterval)
            {
                timer = 0f;
                stepFrame = !stepFrame;
            }
        }
        else
        {
            timer = 0f;
            stepFrame = true; // 다시 걷기 시작하면 바로 발을 내딛는 그림부터
        }

        var (idle, walk) = facing switch
        {
            Facing.Up => (upIdle, upWalk),
            Facing.Side => (sideIdle, sideWalk),
            _ => (downIdle, downWalk),
        };
        if (idle == null) (idle, walk) = (downIdle, downWalk);

        spriteRenderer.sprite = moving && stepFrame && walk != null ? walk : idle;
        spriteRenderer.flipX = facing == Facing.Side && lookingRight == sideFacesLeft;
    }
}
