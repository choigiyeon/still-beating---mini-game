using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 연못 (챕터3). 주인공이 연못(돌 테두리 포함)을 밟으면 바로 게임오버, 적은 막혀서 피해 다닌다.
///  - fallZone(연못 그림 바깥선을 따라 그린 범위, Is Trigger): 주인공 발 위치가 이 안에 들어가면 게임오버.
///    몸(동그란 충돌 범위) 가장자리가 스치기만 해서는 탈락하지 않는다 (억울하게 죽지 않게)
///  - enemyBlocker(연못 테두리): 적만 막는 벽. 주인공과는 충돌하지 않게 한다
/// </summary>
public class Pond : MonoBehaviour
{
    [Tooltip("적을 막는 연못 테두리 콜라이더 (Is Trigger 꺼짐)")]
    [SerializeField] Collider2D enemyBlocker;
    [Tooltip("탈락 판정 범위 (Is Trigger 켬). 주인공 발이 이 안에 들어가면 게임오버. 비워두면 켜져 있는 트리거 콜라이더를 씀")]
    [FormerlySerializedAs("water")]
    [SerializeField] Collider2D fallZone;

    Transform player;

    void Start()
    {
        if (fallZone == null)
            foreach (var col in GetComponents<Collider2D>())
                if (col.isTrigger && col.enabled) { fallZone = col; break; }

        var pc = FindAnyObjectByType<PlayerController>();
        if (pc == null) return;
        player = pc.transform; // 기준점 = 발 위치
        if (enemyBlocker != null && pc.TryGetComponent(out Collider2D playerCollider))
            Physics2D.IgnoreCollision(enemyBlocker, playerCollider);
    }

    void Update()
    {
        if (player == null || fallZone == null || !GameManager.Instance.IsPlaying) return;
        if (fallZone.OverlapPoint(player.position))
            GameManager.Instance.GameOver("연못에 빠졌다...");
    }
}
