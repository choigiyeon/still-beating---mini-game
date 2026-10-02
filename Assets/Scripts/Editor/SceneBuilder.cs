using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 메뉴 [Still Beating > 씬·프리팹 생성]: 임시 아트, 프리팹, Title/Prologue/Chapter1 씬을 만든다.
/// 한 번 만든 뒤에는 Unity 에디터에서 직접 배치/스프라이트를 수정하면 된다.
/// (다시 실행하면 씬과 프리팹을 덮어쓰니 주의)
/// </summary>
static class SceneBuilder
{
    const string PrefabFolder = "Assets/Prefabs";
    const string SceneFolder = "Assets/Scenes";
    static readonly Vector2 Center = new(0.5f, 0.5f);

    // 방 크기 (하단 UI 영역을 비워두기 위해 위로 약간 올려서 배치)
    const float RoomLeft = -8.5f, RoomRight = 8.5f, RoomBottom = -3.8f, RoomTop = 4.8f, Wall = 0.5f;

    // 색은 아트 스케치 메모 기준 (임시 도형용)
    static readonly Color FloorColor = new(0.3f, 0.3f, 0.32f);      // 어두운 회색
    static readonly Color TileWallColor = new(0.62f, 0.64f, 0.66f); // 위쪽 타일벽
    static readonly Color WallColor = new(0.18f, 0.18f, 0.2f);
    static readonly Color DrawerColor = new(0.33f, 0.2f, 0.12f);    // 어두운 갈색 서랍장
    static readonly Color DoorColor = new(0.42f, 0.45f, 0.5f);      // 철문
    static readonly Color BedColor = new(0.93f, 0.93f, 0.92f);      // 흰색 침대
    static readonly Color PlayerColor = new(0.95f, 0.95f, 0.95f);
    static readonly Color EnemyColor = new(0.9f, 0.12f, 0.15f);
    static readonly Color InnerRangeColor = new(1f, 0.5f, 0.1f, 0.75f);
    static readonly Color OuterRangeColor = new(0.2f, 0.75f, 0.35f, 0.6f);
    static readonly Color HeartRed = new(0.85f, 0.1f, 0.15f);
    static readonly Color DigitalRed = new(1f, 0.15f, 0.12f);
    static readonly Color PanelColor = new(0.2f, 0.2f, 0.22f, 0.97f);
    static readonly Color PocketColor = new(0.78f, 0.78f, 0.8f);    // 연한 회색
    static readonly Color DarkBackground = new(0.12f, 0.12f, 0.13f);

    static Font font;
    static Font UIFont => font != null ? font : font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    struct Prefabs
    {
        public GameObject Player, EnemyWander, EnemyChase, FoodDrawer, Bed, Door;
    }

    // 프로젝트를 처음 열었을 때 아직 생성 전이면 바로 만들지 물어본다 (세션당 한 번)
    [InitializeOnLoadMethod]
    static void AskOnFirstOpen() => EditorApplication.delayCall += () =>
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetBool("StillBeating.Asked", false)) return;
        if (AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/Player.prefab") != null) return;
        SessionState.SetBool("StillBeating.Asked", true);

        if (EditorUtility.DisplayDialog("Still Beating",
                "아직 씬과 프리팹이 생성되지 않았습니다.\n지금 Title / Prologue / Chapter1 씬과 프리팹을 만들까요?\n\n(나중에 메뉴 Still Beating > 씬·프리팹 생성 으로도 할 수 있습니다)",
                "지금 생성", "나중에"))
            Build();
    };

    [MenuItem("Still Beating/씬·프리팹 생성 (덮어쓰기)")]
    static void BuildAll()
    {
        if (EditorUtility.DisplayDialog("Still Beating",
                "임시 아트, 프리팹, Title / Prologue / Chapter1 씬을 새로 만듭니다.\n같은 이름의 기존 파일은 덮어씁니다. (에디터에서 수정한 배치도 사라짐)\n\n계속할까요?",
                "생성", "취소"))
            Build();
    }

    static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var art = PlaceholderArt.CreateAll();
        var prefabs = CreatePrefabs(art);

        BuildTitle(art);
        BuildPrologue();
        BuildChapter1(art, prefabs);

        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene($"{SceneFolder}/{SceneFlow.Title}.unity");
        EditorUtility.DisplayDialog("Still Beating", "완료! Title 씬을 열었습니다. Play로 확인해 보세요.", "확인");
    }

    // ───────────────────────── 프리팹

    static Prefabs CreatePrefabs(PlaceholderArt.Set art)
    {
        if (!AssetDatabase.IsValidFolder(PrefabFolder)) AssetDatabase.CreateFolder("Assets", "Prefabs");

        return new Prefabs
        {
            Player = SavePrefab(CreatePlayer(art), "Player"),
            EnemyWander = SavePrefab(CreateEnemy(art, "Enemy_Wander", EnemyBehaviour.Wander), "Enemy_Wander"),
            EnemyChase = SavePrefab(CreateEnemy(art, "Enemy_Chase", EnemyBehaviour.Chase), "Enemy_Chase"),
            FoodDrawer = SavePrefab(CreateFoodDrawer(art), "FoodDrawer"),
            Bed = SavePrefab(CreateBed(art), "Bed"),
            Door = SavePrefab(CreateDoor(art), "Door"),
        };
    }

    static GameObject SavePrefab(GameObject go, string name)
    {
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabFolder}/{name}.prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    static GameObject CreatePlayer(PlaceholderArt.Set art)
    {
        var go = new GameObject("Player");
        go.AddComponent<CircleCollider2D>().radius = 0.3f;
        var rb = AddBody(go);
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep; // 멈춰 있어도 문 트리거가 감지되도록
        go.AddComponent<PlayerStatus>();
        go.AddComponent<PlayerController>();
        AddSprite(go.transform, "Sprite", art.Circle, PlayerColor, 5, new Vector2(0.6f, 0.6f));
        return go;
    }

    static GameObject CreateEnemy(PlaceholderArt.Set art, string name, EnemyBehaviour behaviour)
    {
        var go = new GameObject(name);
        go.AddComponent<CircleCollider2D>().radius = 0.35f;
        AddBody(go);
        AddSprite(go.transform, "Sprite", art.Circle, EnemyColor, 4, new Vector2(0.7f, 0.7f));
        var outer = AddSprite(go.transform, "Range_Outer", art.Ring, OuterRangeColor, 2, Vector2.one);
        var inner = AddSprite(go.transform, "Range_Inner", art.Ring, InnerRangeColor, 2, Vector2.one);

        var enemy = go.AddComponent<Enemy>();
        enemy.behaviour = behaviour;
        Set(enemy, "outerRange", outer.transform);
        Set(enemy, "innerRange", inner.transform);
        return go;
    }

    static GameObject CreateFoodDrawer(PlaceholderArt.Set art)
    {
        var size = new Vector2(2.4f, 1.1f);
        var go = new GameObject("FoodDrawer");
        go.AddComponent<BoxCollider2D>().size = size;
        AddSprite(go.transform, "Sprite", art.Square, DrawerColor, 0, size);

        // 서랍 아래 디지털 타이머 (검정 배경 + 빨간 숫자)
        var canvasGo = new GameObject("Timer", typeof(RectTransform));
        canvasGo.transform.SetParent(go.transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;
        var canvasRect = (RectTransform)canvasGo.transform;
        canvasRect.sizeDelta = new Vector2(100, 36);
        canvasRect.localScale = Vector3.one * 0.01f;
        canvasRect.localPosition = new Vector3(0, -0.85f, 0);

        AddImage(Stretch("Border", canvasRect), new Color(0.35f, 0.35f, 0.37f));
        var bg = Stretch("Background", canvasRect);
        bg.offsetMin = new Vector2(3, 3);
        bg.offsetMax = new Vector2(-3, -3);
        AddImage(bg, Color.black);
        var text = AddText(Stretch("Text", canvasRect), "00:00", 26, DigitalRed, TextAnchor.MiddleCenter, FontStyle.Bold);

        var drawer = go.AddComponent<FoodDrawer>();
        Set(drawer, "timerText", text);
        return go;
    }

    static GameObject CreateBed(PlaceholderArt.Set art)
    {
        var size = new Vector2(1.8f, 2.8f);
        var go = new GameObject("Bed");
        go.AddComponent<BoxCollider2D>().size = size;
        var sprite = AddSprite(go.transform, "Sprite", art.Square, BedColor, 0, size);
        Set(go.AddComponent<Bed>(), "spriteRenderer", sprite);
        return go;
    }

    static GameObject CreateDoor(PlaceholderArt.Set art)
    {
        var go = new GameObject("Door");
        // 트리거를 방 안쪽까지 늘려서 벽에 붙으면 들어간 것으로 처리
        var trigger = go.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(4f, 1.1f);
        // 문짝 뒤 어두운 통로 (문짝이 옆으로 밀리면 드러남)
        AddSprite(go.transform, "Doorway", art.Square, new Color(0.04f, 0.04f, 0.05f), -4, new Vector2(4f, Wall));
        var sprite = AddSprite(go.transform, "Sprite", art.Square, DoorColor, -3, new Vector2(4f, Wall));
        Set(go.AddComponent<Door>(), "spriteRenderer", sprite);
        return go;
    }

    static Rigidbody2D AddBody(GameObject go)
    {
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        return rb;
    }

    static SpriteRenderer AddSprite(Transform parent, string name, Sprite sprite, Color color, int order, Vector2 scale)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = order;
        return sr;
    }

    // ───────────────────────── Title

    static void BuildTitle(PlaceholderArt.Set art)
    {
        NewScene(DarkBackground);
        var canvas = CreateCanvas("Canvas", 0);
        var screen = canvas.gameObject.AddComponent<TitleScreen>();

        // 왼쪽: 메인 일러스트 자리 (아트 완성 후 Sprite 지정)
        var illust = UI("MainIllustration", canvas.transform, new Vector2(0.27f, 0.5f), Vector2.zero, new Vector2(900, 900), Center);
        AddImage(illust, new Color(1, 1, 1, 0.04f));

        var title = UI("Title", canvas.transform, new Vector2(0.74f, 0.72f), Vector2.zero, new Vector2(800, 300), Center);
        AddText(title, "STILL BEATING", 96, new Color(0.62f, 0.62f, 0.64f), TextAnchor.MiddleCenter, FontStyle.Bold);

        HeartButton(canvas.transform, "NewGameButton", "새로하기", new Vector2(0.74f, 0.42f), art, screen.NewGame);
        HeartButton(canvas.transform, "ContinueButton", "이어하기", new Vector2(0.74f, 0.28f), art, screen.Continue);

        CreateEventSystem();
        SaveScene(SceneFlow.Title);
    }

    // 약간 투명한 검정 버튼 + 심장 아이콘
    static void HeartButton(Transform parent, string name, string label, Vector2 anchor, PlaceholderArt.Set art, UnityAction onClick)
    {
        var rect = UI(name, parent, anchor, Vector2.zero, new Vector2(600, 110), Center);
        var image = AddImage(rect, new Color(0, 0, 0, 0.55f), raycast: true);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        UnityEventTools.AddPersistentListener(button.onClick, onClick);

        var icon = UI("Heart", rect, new Vector2(0, 0.5f), new Vector2(40, 0), new Vector2(64, 60), new Vector2(0, 0.5f));
        AddImage(icon, HeartRed, art.Heart);

        var text = Stretch("Text", rect);
        text.offsetMin = new Vector2(130, 0);
        AddText(text, label, 40, Color.white, TextAnchor.MiddleLeft);
    }

    // ───────────────────────── Prologue

    static void BuildPrologue()
    {
        NewScene(DarkBackground);
        var canvas = CreateCanvas("Canvas", 0);
        var screen = canvas.gameObject.AddComponent<PrologueScreen>();
        var ink = new Color(0.12f, 0.1f, 0.08f);

        // 약간 오염되고 구겨진 노란 종이 (아트 완성 후 Sprite 지정)
        var paper = UI("Paper", canvas.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400, 900));
        AddImage(paper, new Color(0.9f, 0.82f, 0.6f));

        var topLeft = new Vector2(0, 1);
        AddText(UI("To", paper, topLeft, new Vector2(110, -70), new Vector2(80, 50), topLeft), "TO", 34, ink, TextAnchor.MiddleLeft);
        AddImage(UI("ToName (가림)", paper, topLeft, new Vector2(180, -80), new Vector2(200, 30), topLeft), ink);

        // 손글씨 폰트가 나오면 Font만 교체
        const string letter =
            "당신의 탈출을 돕기 위해 몇 글자 써내려 본다.\n\n" +
            "1. 그들을 가까이 하지 말것.\n" +
            "2. 심박수 180을 넘기지 말것.\n" +
            "3. 먹을 것들을 꼭 챙겨둘 것.\n" +
            "4. 주머니를 잘 확인하여 제때 영양을 섭취할 것.\n\n\n" +
            "그럼 하나님의 축복이 함께 하기를.";
        var body = AddText(UI("Letter", paper, topLeft, new Vector2(110, -180), new Vector2(1180, 560), topLeft), letter, 34, ink, TextAnchor.UpperLeft);
        body.lineSpacing = 1.3f;

        var bottomRight = new Vector2(1, 0);
        AddText(UI("From", paper, bottomRight, new Vector2(-330, 60), new Vector2(100, 50), bottomRight), "from.", 34, ink, TextAnchor.MiddleLeft);
        AddImage(UI("FromName (가림)", paper, bottomRight, new Vector2(-120, 70), new Vector2(200, 30), bottomRight), ink);

        // 흰색 화살표
        var arrow = UI("NextButton", paper, bottomRight, new Vector2(-90, 150), new Vector2(160, 100), bottomRight);
        var arrowText = AddText(arrow, "→", 90, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
        arrowText.raycastTarget = true;
        arrow.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(3, -3);
        var button = arrow.gameObject.AddComponent<Button>();
        button.targetGraphic = arrowText;
        UnityEventTools.AddPersistentListener(button.onClick, screen.Next);

        CreateEventSystem();
        SaveScene(SceneFlow.Prologue);
    }

    // ───────────────────────── Chapter1

    static void BuildChapter1(PlaceholderArt.Set art, Prefabs prefabs)
    {
        NewScene(new Color(0.1f, 0.1f, 0.12f));
        new GameObject("GameManager").AddComponent<GameManager>();

        // 방
        var room = new GameObject("Room").transform;
        float width = RoomRight - RoomLeft, height = RoomTop - RoomBottom;
        float cx = (RoomLeft + RoomRight) / 2f, cy = (RoomBottom + RoomTop) / 2f;
        Block(room, "Floor", art, new Vector2(cx, cy), new Vector2(width, height), FloorColor, -10, false);
        Block(room, "Wall_Top (타일벽)", art, new Vector2(cx, RoomTop + Wall / 2f), new Vector2(width + Wall * 2f, Wall), TileWallColor, -5, true);
        Block(room, "Wall_Bottom", art, new Vector2(cx, RoomBottom - Wall / 2f), new Vector2(width + Wall * 2f, Wall), WallColor, -5, true);
        Block(room, "Wall_Left", art, new Vector2(RoomLeft - Wall / 2f, cy), new Vector2(Wall, height), WallColor, -5, true);
        Block(room, "Wall_Right", art, new Vector2(RoomRight + Wall / 2f, cy), new Vector2(Wall, height), WallColor, -5, true);

        Place(prefabs.Door, "문", new Vector2(cx, RoomTop + Wall / 2f));
        Place(prefabs.FoodDrawer, "음식서랍_왼쪽", new Vector2(-5.8f, RoomTop - 0.55f));
        Place(prefabs.FoodDrawer, "음식서랍_오른쪽", new Vector2(5.8f, RoomTop - 0.55f));
        Place(prefabs.Bed, "침대", new Vector2(RoomLeft + 0.9f, -1.3f));

        var player = Place(prefabs.Player, "주인공", new Vector2(0f, 0.5f));
        foreach (var (prefab, name, pos) in new[]
                 {
                     (prefabs.EnemyWander, "적_랜덤1", new Vector2(-5.5f, 1f)),
                     (prefabs.EnemyWander, "적_랜덤2", new Vector2(5.5f, 1f)),
                     (prefabs.EnemyChase, "적_추적", new Vector2(0f, -3f)),
                 })
        {
            Set(Place(prefab, name, pos).GetComponent<Enemy>(), "target", player.transform);
        }

        BuildHud(art, player.GetComponent<PlayerStatus>());
        BuildIntro();
        CreateEventSystem();
        SaveScene(SceneFlow.Chapter1);
    }

    static void BuildHud(PlaceholderArt.Set art, PlayerStatus player)
    {
        var canvas = CreateCanvas("HUD", 0);
        var hud = canvas.gameObject.AddComponent<GameHUD>();
        var root = Stretch("HudRoot", canvas.transform);
        var bottomLeft = Vector2.zero;
        var bottomRight = new Vector2(1, 0);

        // 하단 패널 (어두운 회색)
        var panel = UI("BottomPanel", root, new Vector2(0.5f, 0), Vector2.zero, new Vector2(0, 150), new Vector2(0.5f, 0));
        panel.anchorMin = new Vector2(0, 0);
        panel.anchorMax = new Vector2(1, 0);
        AddImage(panel, PanelColor);

        // 심박수: 빨간 하트 안에 하얀 숫자
        var heart = UI("HeartRate", root, bottomLeft, new Vector2(30, 15), new Vector2(130, 120), bottomLeft);
        AddImage(heart, HeartRed, art.Heart);
        var heartTextRect = Stretch("Text", heart);
        heartTextRect.offsetMin = new Vector2(0, 8);
        var heartText = AddText(heartTextRect, "80", 34, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);

        // 심전도: 검정 배경
        var ecgRect = UI("ECG", root, bottomLeft, new Vector2(180, 25), new Vector2(340, 100), bottomLeft);
        AddImage(ecgRect, Color.black);
        var ecgLine = Stretch("Line", ecgRect).gameObject.AddComponent<EcgGraph>();
        ecgLine.color = DigitalRed;
        ecgLine.raycastTarget = false;
        Set(ecgLine, "player", player);

        // 영양: 닭다리 5개 (반 개 단위로 Filled)
        int slots = Mathf.CeilToInt(GameBalance.Current.maxHungerHalves / 2f);
        var drumsticks = new Image[slots];
        for (int i = 0; i < slots; i++)
        {
            var slot = UI($"Drumstick_{i + 1}", root, bottomLeft, new Vector2(600 + i * 98, 30), new Vector2(90, 90), bottomLeft);
            AddImage(slot, new Color(1, 1, 1, 0.12f), art.Drumstick);
            var fill = AddImage(Stretch("Fill", slot), Color.white, art.Drumstick);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            drumsticks[i] = fill;
        }

        // 주머니: 연한 회색 칸 2개, 클릭하면 먹기
        int capacity = GameBalance.Current.pocketCapacity;
        var pocketSlots = new Button[capacity];
        var pocketFoods = new Image[capacity];
        for (int i = 0; i < capacity; i++)
        {
            float x = -(30 + (capacity - 1 - i) * 150);
            var slot = UI($"Pocket_{i + 1}", root, bottomRight, new Vector2(x, 25), new Vector2(130, 100), bottomRight);
            var image = AddImage(slot, PocketColor, raycast: true);
            var button = slot.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            UnityEventTools.AddPersistentListener(button.onClick, hud.EatFood);
            pocketSlots[i] = button;

            var inner = Stretch("Inner", slot);
            inner.offsetMin = new Vector2(8, 8);
            inner.offsetMax = new Vector2(-8, -8);
            AddImage(inner, new Color(0.68f, 0.68f, 0.7f));
            pocketFoods[i] = AddImage(UI("Food", slot, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(80, 80)), Color.white, art.Drumstick);
        }

        // 탈출 타이머 (맵 오른쪽 아래, 빨간 디지털)
        var timer = UI("EscapeTimer", root, bottomRight, new Vector2(-30, 165), new Vector2(220, 56), bottomRight);
        AddImage(timer, new Color(0.35f, 0.35f, 0.37f));
        var timerBg = Stretch("Background", timer);
        timerBg.offsetMin = new Vector2(3, 3);
        timerBg.offsetMax = new Vector2(-3, -3);
        AddImage(timerBg, Color.black);
        var timerText = AddText(Stretch("Text", timer), "05:00", 40, DigitalRed, TextAnchor.MiddleCenter, FontStyle.Bold);

        // 종료 화면
        var end = Stretch("EndScreen", canvas.transform);
        AddImage(end, new Color(0, 0, 0, 0.75f));
        var endText = AddText(Stretch("Text", end), "GAME OVER", 90, DigitalRed, TextAnchor.MiddleCenter, FontStyle.Bold);
        end.gameObject.SetActive(false);

        Set(hud, "player", player);
        Set(hud, "hudRoot", root.gameObject);
        Set(hud, "heartRateText", heartText);
        SetArray(hud, "drumsticks", drumsticks);
        SetArray(hud, "pocketSlots", pocketSlots);
        SetArray(hud, "pocketFoods", pocketFoods);
        Set(hud, "escapeTimerText", timerText);
        Set(hud, "endScreen", end.gameObject);
        Set(hud, "endText", endText);
    }

    static void BuildIntro()
    {
        var canvas = CreateCanvas("IntroCanvas", 100);
        var intro = canvas.gameObject.AddComponent<ChapterIntro>();
        var blur = Stretch("Blur", canvas.transform).gameObject.AddComponent<RawImage>();
        blur.raycastTarget = false;
        var black = AddImage(Stretch("Black", canvas.transform), Color.black);
        blur.enabled = black.enabled = false; // 에디터에서 화면을 가리지 않도록 (Play 시 ChapterIntro가 켬)
        Set(intro, "blurImage", blur);
        Set(intro, "blackOverlay", black);
    }

    static void Block(Transform parent, string name, PlaceholderArt.Set art, Vector2 position, Vector2 size, Color color, int order, bool solid)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = art.Square;
        sr.color = color;
        sr.sortingOrder = order;
        if (solid) go.AddComponent<BoxCollider2D>();
    }

    static GameObject Place(GameObject prefab, string name, Vector2 position)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = name;
        go.transform.position = position;
        return go;
    }

    // ───────────────────────── 씬 / UI 공통

    static void NewScene(Color background)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var go = new GameObject("Main Camera") { tag = "MainCamera" };
        go.transform.position = new Vector3(0, 0, -10);
        var cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5.4f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = background;
        go.AddComponent<AudioListener>();
    }

    static void SaveScene(string name) =>
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), $"{SceneFolder}/{name}.unity");

    static Canvas CreateCanvas(string name, int sortingOrder)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 1f;
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    static void CreateEventSystem() =>
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

    /// <summary>anchor 한 점 기준으로 놓는 UI 요소. pivot을 생략하면 anchor와 같다.</summary>
    static RectTransform UI(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Vector2? pivot = null)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot ?? anchor;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        return rt;
    }

    /// <summary>부모를 꽉 채우는 UI 요소</summary>
    static RectTransform Stretch(string name, Transform parent)
    {
        var rt = UI(name, parent, Vector2.zero, Vector2.zero, Vector2.zero);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    static Image AddImage(RectTransform rt, Color color, Sprite sprite = null, bool raycast = false)
    {
        var image = rt.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.preserveAspect = sprite != null;
        image.raycastTarget = raycast;
        return image;
    }

    static Text AddText(RectTransform rt, string content, int size, Color color, TextAnchor alignment, FontStyle style = FontStyle.Normal)
    {
        var text = rt.gameObject.AddComponent<Text>();
        text.font = UIFont;
        text.text = content;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    // private [SerializeField] 필드 연결
    static void Set(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetArray(Object target, string field, Object[] values)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(field);
        prop.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
