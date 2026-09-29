using UnityEngine;

/// <summary>
/// 챕터1 데모 씬을 코드로 구성한다. 씬에 빈 오브젝트 하나로 붙여두고 Play하면 된다.
/// 밸런스 수치는 이 컴포넌트의 인스펙터(Balance)에서 수정.
/// 아트 리소스가 나오면 프리팹 + 씬 배치 방식으로 옮길 예정.
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    [SerializeField] GameBalance balance = new();

    // 방 크기 (하단 UI 영역을 비워두기 위해 위로 약간 올려서 배치)
    const float RoomLeft = -8.5f, RoomRight = 8.5f, RoomBottom = -3.8f, RoomTop = 4.8f;
    const float WallThickness = 0.5f;

    // 색은 아트 스케치 4의 메모 기준 (임시 도형용)
    static readonly Color FloorColor = new(0.3f, 0.3f, 0.32f);      // 어두운 회색
    static readonly Color TileWallColor = new(0.62f, 0.64f, 0.66f); // 위쪽 타일벽
    static readonly Color WallColor = new(0.18f, 0.18f, 0.2f);
    static readonly Color DrawerColor = new(0.33f, 0.2f, 0.12f);    // 어두운 갈색 서랍장
    static readonly Color BedColor = new(0.93f, 0.93f, 0.92f);      // 흰색 침대
    static readonly Color PlayerColor = new(0.95f, 0.95f, 0.95f);
    static readonly Color EnemyColor = new(0.9f, 0.12f, 0.15f);
    static readonly Color InnerRangeColor = new(1f, 0.5f, 0.1f, 0.75f);
    static readonly Color OuterRangeColor = new(0.2f, 0.75f, 0.35f, 0.6f);

    void Awake()
    {
        GameBalance.Current = balance;

        SetupCamera();

        var manager = new GameObject("GameManager");
        manager.AddComponent<GameManager>();
        manager.AddComponent<ChapterIntro>();
        var hud = manager.AddComponent<GameHUD>();

        BuildRoom();

        var player = CreatePlayer(new Vector2(0f, 0.5f));
        hud.Player = player.GetComponent<PlayerStatus>();

        var playerCollider = player.GetComponent<Collider2D>();
        CreateEnemy("적_랜덤1", EnemyBehaviour.Wander, new Vector2(-5.5f, 1f), player.transform, playerCollider);
        CreateEnemy("적_랜덤2", EnemyBehaviour.Wander, new Vector2(5.5f, 1f), player.transform, playerCollider);
        CreateEnemy("적_추적", EnemyBehaviour.Chase, new Vector2(0f, -3f), player.transform, playerCollider);
    }

    static void SetupCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;
        cam.orthographic = true;
        cam.orthographicSize = 5.4f;
        cam.transform.position = new Vector3(0f, 0f, -10f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.1f, 0.1f, 0.12f);
    }

    static void BuildRoom()
    {
        float width = RoomRight - RoomLeft, height = RoomTop - RoomBottom;
        float cx = (RoomLeft + RoomRight) / 2f, cy = (RoomBottom + RoomTop) / 2f;

        CreateBlock("바닥", new Vector2(cx, cy), new Vector2(width, height), FloorColor, -10, false);

        float t = WallThickness;
        CreateBlock("벽_위", new Vector2(cx, RoomTop + t / 2f), new Vector2(width + t * 2f, t), TileWallColor, -5, true);
        CreateBlock("벽_아래", new Vector2(cx, RoomBottom - t / 2f), new Vector2(width + t * 2f, t), WallColor, -5, true);
        CreateBlock("벽_왼쪽", new Vector2(RoomLeft - t / 2f, cy), new Vector2(t, height), WallColor, -5, true);
        CreateBlock("벽_오른쪽", new Vector2(RoomRight + t / 2f, cy), new Vector2(t, height), WallColor, -5, true);

        // 문(철문): 위쪽 벽 가운데. 트리거를 방 안쪽까지 늘려서 벽에 붙으면 들어간 것으로 처리
        var door = CreateBlock("문", new Vector2(cx, RoomTop + t / 2f), new Vector2(4f, t), Color.white, -4, false);
        var doorTrigger = door.AddComponent<BoxCollider2D>();
        doorTrigger.isTrigger = true;
        doorTrigger.size = new Vector2(1f, 2.2f);
        door.AddComponent<Door>();

        // 음식서랍(서랍장): 위쪽 왼쪽/오른쪽에 하나씩
        CreateBlock("음식서랍_왼쪽", new Vector2(-5.8f, RoomTop - 0.55f), new Vector2(2.4f, 1.1f), DrawerColor, 0, true)
            .AddComponent<FoodDrawer>();
        CreateBlock("음식서랍_오른쪽", new Vector2(5.8f, RoomTop - 0.55f), new Vector2(2.4f, 1.1f), DrawerColor, 0, true)
            .AddComponent<FoodDrawer>();

        // 침대: 왼쪽 벽
        CreateBlock("침대", new Vector2(RoomLeft + 0.9f, -1.3f), new Vector2(1.8f, 2.8f), BedColor, 0, true)
            .AddComponent<Bed>();
    }

    static GameObject CreatePlayer(Vector2 position)
    {
        var go = new GameObject("주인공");
        go.transform.position = position;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteFactory.Disc(0.3f);
        sr.color = PlayerColor;
        sr.sortingOrder = 5;

        go.AddComponent<CircleCollider2D>().radius = 0.3f;
        var rb = CreateBody(go);
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep; // 멈춰 있어도 문 트리거가 감지되도록

        go.AddComponent<PlayerStatus>();
        go.AddComponent<PlayerController>();
        return go;
    }

    void CreateEnemy(string name, EnemyBehaviour behaviour, Vector2 position, Transform target, Collider2D playerCollider)
    {
        var go = new GameObject(name);
        go.transform.position = position;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteFactory.Disc(0.35f);
        sr.color = EnemyColor;
        sr.sortingOrder = 4;

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.35f;
        Physics2D.IgnoreCollision(col, playerCollider); // 적이 플레이어를 밀어내지 않도록
        CreateBody(go);

        CreateRangeRing(go.transform, "범위_바깥", balance.enemyOuterRadius, OuterRangeColor);
        CreateRangeRing(go.transform, "범위_안쪽", balance.enemyInnerRadius, InnerRangeColor);

        var enemy = go.AddComponent<Enemy>();
        enemy.behaviour = behaviour;
        enemy.target = target;
    }

    static void CreateRangeRing(Transform parent, string name, float radius, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteFactory.Ring(radius, 0.06f);
        sr.color = color;
        sr.sortingOrder = 2;
    }

    static Rigidbody2D CreateBody(GameObject go)
    {
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        return rb;
    }

    static GameObject CreateBlock(string name, Vector2 position, Vector2 size, Color color, int sortingOrder, bool solid)
    {
        var go = new GameObject(name);
        go.transform.position = position;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteFactory.Square;
        sr.color = color;
        sr.sortingOrder = sortingOrder;

        if (solid) go.AddComponent<BoxCollider2D>();
        return go;
    }
}
