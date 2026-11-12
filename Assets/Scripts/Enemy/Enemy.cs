using System.Collections.Generic;
using UnityEngine;

public enum EnemyBehaviour { Wander, Chase }

/// <summary>
/// 적. Wander = 완전 랜덤 이동, Chase = 플레이어를 천천히 추적.
/// 2단계 범위(바깥/안쪽) 안에 플레이어가 있으면 심박수를 올린다.
///  - Chase: 플레이어가 바로 보이면 직진, 장애물에 가려 있으면 길찾기(NavGrid)로 돌아서 온다
///  - Wander: 가는 방향 바로 앞에 장애물이 있으면 막히기 전에 비어 있는 쪽으로 방향을 바꾼다
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
            rb.linearVelocity = toTarget.sqrMagnitude > 0.01f ? ChaseDirection(target.position) * b.chaseSpeed : Vector2.zero;
        }
        else
        {
            // 바로 앞이 막혀 있으면 부딪히기 전에 비어 있는 쪽으로 방향 바꾸기
            if (NavGrid.Blocked(rb.position, wanderDirection, WanderLookAhead, out var normal))
                PickWanderDirection(normal);
            rb.linearVelocity = wanderDirection * b.wanderSpeed;
        }
    }

    // ───────────── 추적: 장애물 돌아오기
    // 플레이어가 바로 보이면 직진, 가려져 있으면 격자 길찾기 경로를 따라간다

    const float RepathInterval = 0.3f; // 경로 다시 계산하는 간격 (초)
    const int ShortcutLookAhead = 6;   // 경로에서 바로 갈 수 있는 먼 지점까지 건너뛰기 (지그재그 방지)

    readonly List<Vector2> path = new();
    int pathIndex;
    float repathTimer;

    Vector2 ChaseDirection(Vector2 targetPosition)
    {
        Vector2 position = rb.position;
        if (NavGrid.Clear(position, targetPosition))
        {
            path.Clear();
            repathTimer = 0f; // 다시 가려지면 바로 길찾기
            return (targetPosition - position).normalized;
        }

        // 길이 없을 때도 매 프레임이 아니라 간격마다만 다시 찾는다
        repathTimer -= Time.fixedDeltaTime;
        if (repathTimer <= 0f)
        {
            repathTimer = RepathInterval;
            NavGrid.FindPath(position, targetPosition, path);
            pathIndex = 0;
        }
        if (path.Count == 0) return (targetPosition - position).normalized; // 길이 없으면 그냥 직진

        for (int i = Mathf.Min(path.Count - 1, pathIndex + ShortcutLookAhead); i > pathIndex; i--)
        {
            if (!NavGrid.Clear(position, path[i])) continue;
            pathIndex = i;
            break;
        }
        if (pathIndex < path.Count - 1 && Vector2.Distance(position, path[pathIndex]) < 0.15f) pathIndex++;

        Vector2 toWaypoint = path[pathIndex] - position;
        return toWaypoint.sqrMagnitude > 0.0001f ? toWaypoint.normalized : (targetPosition - position).normalized;
    }

    // ───────────── 랜덤 이동: 막히기 전에 피하기

    const float WanderLookAhead = 0.45f; // 이만큼 앞에 장애물이 있으면 방향을 바꾼다
    const float WanderClearance = 1.0f;  // 새 방향은 최소 이만큼 비어 있는 쪽으로

    void OnCollisionEnter2D(Collision2D collision)
    {
        // 다른 적과 부딪히는 등 미리 못 피한 경우: 튕겨나가는 방향으로
        if (behaviour != EnemyBehaviour.Wander || collision.contactCount == 0) return;
        PickWanderDirection(collision.GetContact(0).normal);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        // 계속 밀착해서 파고드는 중이면 다시 고르기 (제자리걸음 방지)
        if (behaviour != EnemyBehaviour.Wander || collision.contactCount == 0) return;
        var normal = collision.GetContact(0).normal;
        if (Vector2.Dot(wanderDirection, normal) < -0.2f) PickWanderDirection(normal);
    }

    // GameManager의 "적 범위 표시" 체크박스에 맞춰 범위 원을 켜고 끈다 (판정에는 영향 없음)
    void UpdateRangeVisibility()
    {
        bool show = GameManager.Instance.ShowEnemyRanges;
        if (outerRange != null && outerRange.gameObject.activeSelf != show) outerRange.gameObject.SetActive(show);
        if (innerRange != null && innerRange.gameObject.activeSelf != show) innerRange.gameObject.SetActive(show);
    }

    /// <summary>
    /// 랜덤 방향 고르기. 앞이 비어 있는 방향 중에서 고르고,
    /// awayFrom(부딪힌 장애물 표면 방향)이 있으면 장애물 반대쪽으로만 고른다.
    /// </summary>
    void PickWanderDirection(Vector2 awayFrom = default)
    {
        var b = GameBalance.Current;
        directionTimer = Random.Range(b.wanderChangeInterval.x, b.wanderChangeInterval.y);

        Vector2 position = rb != null ? rb.position : (Vector2)transform.position;
        for (int i = 0; i < 16; i++)
        {
            Vector2 d = Random.insideUnitCircle.normalized;
            if (d == Vector2.zero) continue;
            if (awayFrom != Vector2.zero && Vector2.Dot(d, awayFrom) < 0.2f) continue;
            if (NavGrid.Blocked(position, d, WanderClearance, out _)) continue;
            wanderDirection = d;
            return;
        }

        // 사방이 좁으면 장애물 반대쪽(없으면 아무 방향)으로
        wanderDirection = awayFrom != Vector2.zero ? awayFrom.normalized : Random.insideUnitCircle.normalized;
    }

    /// <summary>플레이어가 position에 있을 때 이 적이 주는 초당 심박수 증가량.</summary>
    public float HeartRateGainAt(Vector2 position) => RangeLevelAt(position) switch
    {
        2 => GameBalance.Current.enemyInnerPerSecond,
        1 => GameBalance.Current.enemyOuterPerSecond,
        _ => 0f,
    };

    /// <summary>position이 이 적의 어느 범위에 있는지. 2 = 안쪽, 1 = 바깥, 0 = 범위 밖</summary>
    public int RangeLevelAt(Vector2 position)
    {
        var b = GameBalance.Current;
        float distance = Vector2.Distance(position, transform.position);
        if (distance <= b.enemyInnerRadius) return 2;
        if (distance <= b.enemyOuterRadius) return 1;
        return 0;
    }
}
