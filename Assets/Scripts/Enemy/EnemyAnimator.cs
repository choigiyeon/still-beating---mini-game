using UnityEngine;

/// <summary>
/// 적 걷기 애니메이션 (그림 바꾸기 방식). 적은 좌우 모션만 쓴다 (위아래로 움직여도 옆모습 유지).
///  - 걷는 동안 "서기 ↔ 발 내딛기" 그림을 stepInterval초마다 번갈아
///  - 왼쪽/오른쪽으로 갈 때 좌우반전 (위아래로만 움직이면 마지막 방향 유지)
///  - 걷기 그림이 없으면(유령 등) 서기 그림을 좌우반전으로 번갈아 보여서 걷는 느낌을 낸다
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAnimator : MonoBehaviour
{
    [SerializeField] SpriteRenderer spriteRenderer;
    [Tooltip("가만히 있는 그림")]
    [SerializeField] Sprite idle;
    [Tooltip("발을 내딛는 그림 (비워두면 서기 그림을 좌우반전으로 번갈아)")]
    [SerializeField] Sprite walk;
    [Tooltip("그림이 왼쪽을 보고 있으면 체크 (오른쪽으로 갈 때 좌우반전)")]
    [SerializeField] bool facesLeft = true;
    [Tooltip("걸을 때 그림을 바꾸는 간격 (초)")]
    [SerializeField] float stepInterval = 0.25f;

    Rigidbody2D rb;
    bool lookingRight;
    bool stepFrame;
    float timer;

    void Awake() => rb = GetComponent<Rigidbody2D>();

    void Update()
    {
        if (spriteRenderer == null || idle == null) return; // 그림이 없으면 임시 도형 그대로

        Vector2 velocity = rb.linearVelocity;
        bool moving = velocity.sqrMagnitude > 0.01f;
        if (Mathf.Abs(velocity.x) > 0.05f) lookingRight = velocity.x > 0f;

        if (moving)
        {
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
            stepFrame = false;
        }

        bool flip = lookingRight == facesLeft;
        if (walk != null)
        {
            spriteRenderer.sprite = moving && stepFrame ? walk : idle;
        }
        else
        {
            spriteRenderer.sprite = idle;
            if (moving && stepFrame) flip = !flip; // 걷기 그림이 없으면 좌우반전으로 번갈아
        }
        spriteRenderer.flipX = flip;
    }
}
