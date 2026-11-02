using UnityEngine;

/// <summary>
/// 연못 (챕터3). 주인공이 빠지면 바로 게임오버, 적은 막혀서 피해 다닌다.
///  - 트리거 콜라이더(연못 안쪽): 주인공이 들어오면 게임오버
///  - enemyBlocker(연못 테두리): 적만 막는 벽. 주인공과는 충돌하지 않게 해서 빠질 수 있게 한다
/// 트리거를 테두리보다 조금 작게 두면 가장자리를 스치기만 해서는 안 빠진다.
/// </summary>
public class Pond : MonoBehaviour
{
    [Tooltip("적을 막는 연못 테두리 콜라이더 (Is Trigger 꺼짐)")]
    [SerializeField] Collider2D enemyBlocker;

    void Start()
    {
        var player = FindAnyObjectByType<PlayerController>();
        if (player != null && enemyBlocker != null && player.TryGetComponent(out Collider2D playerCollider))
            Physics2D.IgnoreCollision(enemyBlocker, playerCollider);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerController>() != null)
            GameManager.Instance.GameOver("연못에 빠졌다...");
    }
}
