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
    [Tooltip("추적 대상. 비워두면 씬의 플레이어를 찾는다")]
    public Transform target;

    [Header("범위 표시 (크기는 GameBalance의 반지름으로 자동 설정)")]
    [SerializeField] Transform outerRange;
    [SerializeField] Transform innerRange;

    Rigidbody2D rb;
    Vector2 wanderDirection;
    float directionTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // 범위 스프라이트는 지름 1유닛 기준
        var b = GameBalance.Current;
        if (outerRange != null) outerRange.localScale = Vector3.one * b.enemyOuterRadius * 2f;
        if (innerRange != null) innerRange.localScale = Vector3.one * b.enemyInnerRadius * 2f;
    }

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    void Start()
    {
        if (target == null)
        {
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null) target = player.transform;
        }

        // 적이 플레이어를 밀어내지 않도록
        if (target != null && target.TryGetComponent(out Collider2D playerCollider) && TryGetComponent(out Collider2D own))
            Physics2D.IgnoreCollision(own, playerCollider);

        PickWanderDirection();
    }

    void Update()
    {
        UpdateRangeVisibility();

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
            rb.linearVelocity = toTarget.sqrMagnitude > 0.01f ? ChaseDirection(toTarget.normalized) * b.chaseSpeed : Vector2.zero;
        }
        else
        {
            rb.linearVelocity = wanderDirection * b.wanderSpeed;
        }
    }

    // ───────────── 추적 중 장애물 피하기
    // 소파 같은 장애물에 막히면 장애물 표면을 따라 미끄러지듯 돌아간다

    Vector2 obstacleNormal;    // 부딪힌 장애물 표면에서 나를 향하는 방향
    float lastObstacleTime = -1f;
    int detourSide;            // 돌아가는 방향 (1 / -1). 0이면 아직 안 정함

    Vector2 ChaseDirection(Vector2 toTarget)
    {
        bool touching = Time.time - lastObstacleTime < 0.15f;
        if (!touching)
        {
            detourSide = 0;
            return toTarget;
        }

        // 장애물 쪽으로 파고들려는 게 아니면 그대로 쫓아간다
        if (Vector2.Dot(toTarget, obstacleNormal) >= 0f) return toTarget;

        // 장애물 표면 방향 중 플레이어에 가까운 쪽으로 정하고, 다 돌 때까지 유지 (왔다갔다 방지)
        var tangent = new Vector2(-obstacleNormal.y, obstacleNormal.x);
        if (detourSide == 0) detourSide = Vector2.Dot(tangent, toTarget) >= 0f ? 1 : -1;
        return (tangent * detourSide + obstacleNormal * 0.1f).normalized;
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (behaviour != EnemyBehaviour.Chase || collision.contactCount == 0) return;
        obstacleNormal = collision.GetContact(0).normal;
        lastObstacleTime = Time.time;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // 벽/가구에 부딪히면 튕겨나가는 방향으로 바꾼다
        if (behaviour != EnemyBehaviour.Wander || collision.contactCount == 0) return;
        wanderDirection = Vector2.Reflect(wanderDirection, collision.GetContact(0).normal).normalized;
        directionTimer = Random.Range(GameBalance.Current.wanderChangeInterval.x, GameBalance.Current.wanderChangeInterval.y);
    }

    // GameManager의 "적 범위 표시" 체크박스에 맞춰 범위 원을 켜고 끈다 (판정에는 영향 없음)
    void UpdateRangeVisibility()
    {
        bool show = GameManager.Instance.ShowEnemyRanges;
        if (outerRange != null && outerRange.gameObject.activeSelf != show) outerRange.gameObject.SetActive(show);
        if (innerRange != null && innerRange.gameObject.activeSelf != show) innerRange.gameObject.SetActive(show);
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
