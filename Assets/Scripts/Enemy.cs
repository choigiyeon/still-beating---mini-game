using System.Collections.Generic;
using UnityEngine;

public enum EnemyBehaviour { Wander, Chase }

/// <summary>
/// 적. Wander = 완전 랜덤 이동, Chase = 플레이어를 천천히 추적.
/// 2단계 범위(바깥/안쪽) 안에 플레이어가 있으면 심박수를 올린다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour
{
    public static readonly List<Enemy> All = new();

    public EnemyBehaviour behaviour = EnemyBehaviour.Wander;
    public Transform target;

    Rigidbody2D rb;
    Vector2 wanderDirection;
    float directionTimer;

    void Awake() => rb = GetComponent<Rigidbody2D>();
    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    void Start() => PickWanderDirection();

    void Update()
    {
        if (behaviour != EnemyBehaviour.Wander || !GameManager.Instance.IsPlaying) return;
        directionTimer -= Time.deltaTime;
        if (directionTimer <= 0f) PickWanderDirection();
    }

    void FixedUpdate()
    {
        if (!GameManager.Instance.IsPlaying)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        var b = GameBalance.Current;
        if (behaviour == EnemyBehaviour.Chase && target != null)
        {
            Vector2 toTarget = (Vector2)target.position - rb.position;
            rb.linearVelocity = toTarget.sqrMagnitude > 0.01f ? toTarget.normalized * b.chaseSpeed : Vector2.zero;
        }
        else
        {
            rb.linearVelocity = wanderDirection * b.wanderSpeed;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // 벽/가구에 부딪히면 튕겨나가는 방향으로 바꾼다
        if (behaviour != EnemyBehaviour.Wander || collision.contactCount == 0) return;
        wanderDirection = Vector2.Reflect(wanderDirection, collision.GetContact(0).normal).normalized;
        directionTimer = Random.Range(GameBalance.Current.wanderChangeInterval.x, GameBalance.Current.wanderChangeInterval.y);
    }

    void PickWanderDirection()
    {
        var b = GameBalance.Current;
        wanderDirection = Random.insideUnitCircle.normalized;
        directionTimer = Random.Range(b.wanderChangeInterval.x, b.wanderChangeInterval.y);
    }

    /// <summary>플레이어가 position에 있을 때 이 적이 주는 초당 심박수 증가량.</summary>
    public float HeartRateGainAt(Vector2 position)
    {
        var b = GameBalance.Current;
        float distance = Vector2.Distance(position, transform.position);
        if (distance <= b.enemyInnerRadius) return b.enemyInnerPerSecond;
        if (distance <= b.enemyOuterRadius) return b.enemyOuterPerSecond;
        return 0f;
    }
}
