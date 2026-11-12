using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 적이 장애물(벽, 가구, 소파, 연못 테두리 등)을 돌아가도록 길을 찾는 격자.
///  - 씬이 바뀌면 처음 쓸 때 자동으로 만든다 (실제로 막는 콜라이더 = 트리거가 아니고, 주인공/적이 아닌 것)
///  - 적 몸 크기만큼 장애물을 두껍게 잡아서, 경로를 따라가면 모서리에 걸리지 않는다
/// </summary>
public static class NavGrid
{
    const float CellSize = 0.25f;
    const float AgentRadius = 0.4f;   // 적 콜라이더 반지름(0.35) + 여유
    public const float CastRadius = 0.3f; // "앞이 비었나" 검사할 때 쓰는 원 크기 (몸보다 살짝 작게)

    static int sceneHandle = -1;
    static Vector2 origin;
    static int width, height;
    static bool[] blocked;

    static readonly List<Collider2D> overlapBuffer = new();
    static readonly List<RaycastHit2D> hitBuffer = new();
    static ContactFilter2D solidOnly = new() { useTriggers = false };

    /// <summary>적을 막는 장애물인지 (트리거, 주인공, 적은 제외)</summary>
    public static bool IsObstacle(Collider2D c)
    {
        if (c == null || c.isTrigger) return false;
        var body = c.attachedRigidbody;
        if (body != null && (body.TryGetComponent(out Enemy _) || body.TryGetComponent(out PlayerController _))) return false;
        return true;
    }

    /// <summary>from에서 dir 방향으로 distance만큼 갈 때 장애물에 막히는지. 막히면 장애물 표면 방향(normal)을 돌려준다</summary>
    public static bool Blocked(Vector2 from, Vector2 dir, float distance, out Vector2 normal)
    {
        normal = Vector2.zero;
        if (distance <= 0.001f) return false;
        int count = Physics2D.CircleCast(from, CastRadius, dir, solidOnly, hitBuffer, distance);
        for (int i = 0; i < count; i++)
        {
            if (!IsObstacle(hitBuffer[i].collider)) continue;
            normal = hitBuffer[i].normal;
            return true;
        }
        return false;
    }

    /// <summary>from에서 to까지 장애물 없이 바로 갈 수 있는지</summary>
    public static bool Clear(Vector2 from, Vector2 to)
    {
        Vector2 d = to - from;
        float dist = d.magnitude;
        return dist < 0.001f || !Blocked(from, d / dist, dist, out _);
    }

    /// <summary>from → to 경로(월드 좌표 지점들)를 path에 채운다. 길이 없으면 false</summary>
    public static bool FindPath(Vector2 from, Vector2 to, List<Vector2> path)
    {
        path.Clear();
        EnsureBuilt();
        if (blocked == null) return false;

        int start = NearestFree(CellOf(from));
        int goal = NearestFree(CellOf(to));
        if (start < 0 || goal < 0) return false;
        if (start == goal) { path.Add(to); return true; }

        int n = width * height;
        var gScore = new float[n];
        var cameFrom = new int[n];
        var closed = new bool[n];
        for (int i = 0; i < n; i++) { gScore[i] = float.MaxValue; cameFrom[i] = -1; }

        var open = new MinHeap();
        gScore[start] = 0f;
        open.Push(start, Heuristic(start, goal));

        while (open.Count > 0)
        {
            int current = open.Pop();
            if (current == goal) break;
            if (closed[current]) continue;
            closed[current] = true;

            int cx = current % width, cy = current / width;
            for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = cx + dx, ny = cy + dy;
                if (!Free(nx, ny)) continue;
                // 대각선은 양옆이 모두 비었을 때만 (모서리 파고들기 방지)
                if (dx != 0 && dy != 0 && (!Free(cx + dx, cy) || !Free(cx, cy + dy))) continue;

                int next = ny * width + nx;
                float cost = gScore[current] + (dx != 0 && dy != 0 ? 1.4142f : 1f);
                if (cost >= gScore[next]) continue;
                gScore[next] = cost;
                cameFrom[next] = current;
                open.Push(next, cost + Heuristic(next, goal));
            }
        }

        if (cameFrom[goal] < 0) return false;
        for (int c = goal; c != start; c = cameFrom[c]) path.Add(CellCenter(c));
        path.Reverse();
        return true;
    }

    // ───────────────────────── 격자 만들기

    static void EnsureBuilt()
    {
        int handle = SceneManager.GetActiveScene().handle;
        if (handle == sceneHandle) return;
        sceneHandle = handle;
        blocked = null;

        Physics2D.SyncTransforms();

        // 방 크기 = 장애물(벽 포함) 전체를 감싸는 범위
        bool any = false;
        var bounds = new Bounds();
        foreach (var c in Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None))
        {
            if (!IsObstacle(c)) continue;
            if (!any) { bounds = c.bounds; any = true; }
            else bounds.Encapsulate(c.bounds);
        }
        if (!any) return;

        origin = bounds.min;
        width = Mathf.CeilToInt(bounds.size.x / CellSize) + 1;
        height = Mathf.CeilToInt(bounds.size.y / CellSize) + 1;
        blocked = new bool[width * height];

        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            int count = Physics2D.OverlapCircle(CellCenter(y * width + x), AgentRadius, solidOnly, overlapBuffer);
            for (int i = 0; i < count; i++)
            {
                if (!IsObstacle(overlapBuffer[i])) continue;
                blocked[y * width + x] = true;
                break;
            }
        }
    }

    static bool Free(int x, int y) => x >= 0 && y >= 0 && x < width && y < height && !blocked[y * width + x];

    static int CellOf(Vector2 p)
    {
        int x = Mathf.Clamp(Mathf.FloorToInt((p.x - origin.x) / CellSize), 0, width - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt((p.y - origin.y) / CellSize), 0, height - 1);
        return y * width + x;
    }

    static Vector2 CellCenter(int cell) =>
        origin + new Vector2((cell % width + 0.5f) * CellSize, (cell / width + 0.5f) * CellSize);

    // 막힌 칸이면 가장 가까운 빈 칸 (벽에 붙어 있거나 주인공이 적보다 좁은 틈에 있을 때)
    static int NearestFree(int cell)
    {
        int cx = cell % width, cy = cell / width;
        if (Free(cx, cy)) return cell;
        for (int r = 1; r <= 12; r++)
        for (int dy = -r; dy <= r; dy++)
        for (int dx = -r; dx <= r; dx++)
        {
            if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) continue; // 테두리만
            if (Free(cx + dx, cy + dy)) return (cy + dy) * width + cx + dx;
        }
        return -1;
    }

    static float Heuristic(int a, int b)
    {
        int dx = Mathf.Abs(a % width - b % width), dy = Mathf.Abs(a / width - b / width);
        return Mathf.Max(dx, dy) + 0.4142f * Mathf.Min(dx, dy);
    }

    // A*에서 다음에 볼 칸을 빨리 꺼내기 위한 최소 힙
    class MinHeap
    {
        readonly List<(int node, float score)> items = new();
        public int Count => items.Count;

        public void Push(int node, float score)
        {
            items.Add((node, score));
            int i = items.Count - 1;
            while (i > 0)
            {
                int parent = (i - 1) / 2;
                if (items[parent].score <= items[i].score) break;
                (items[parent], items[i]) = (items[i], items[parent]);
                i = parent;
            }
        }

        public int Pop()
        {
            int top = items[0].node;
            int last = items.Count - 1;
            items[0] = items[last];
            items.RemoveAt(last);
            int i = 0;
            while (true)
            {
                int l = i * 2 + 1, r = l + 1, smallest = i;
                if (l < items.Count && items[l].score < items[smallest].score) smallest = l;
                if (r < items.Count && items[r].score < items[smallest].score) smallest = r;
                if (smallest == i) break;
                (items[smallest], items[i]) = (items[i], items[smallest]);
                i = smallest;
            }
            return top;
        }
    }
}
