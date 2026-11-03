using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 메뉴 [Still Beating > 챕터1 아트 적용]
/// 아트팀 레이어(Assets/Art/Chapter1/Layers, 1920x1080 화면 기준으로 제자리에 그려진 투명 PNG)를
/// Chapter1 씬에 적용한다.
///  - 각 레이어에서 그림이 있는 부분만 잘라 Assets/Art/Chapter1/에 저장
///  - 그림 위치 그대로 오브젝트 위치 / 충돌 범위 / 벽을 맞춤
///  - HUD(하트, 닭다리, 주머니, 탈출 타이머)를 그림 위치·모양으로 변경
///  - 밸런스 수치(GameManager)는 건드리지 않음
/// 화면 1920x1080 = 카메라 세로 10.8유닛이라 1유닛 = 100px 로 1:1 대응한다.
/// </summary>
static class Chapter1ArtApplier
{
    const string LayerFolder = "Assets/Art/Chapter1/Layers";
    const string OutFolder = "Assets/Art/Chapter1";
    const string ScenePath = "Assets/Scenes/Chapter1.unity";
    const float PixelsPerUnit = 100f;
    const float ScreenW = 1920f, ScreenH = 1080f;

    // 방 그림에서 위쪽 타일벽이 끝나는 높이 (px, 위에서부터). 그림에서 직접 잰 값
    const float TileWallBottomPx = 178f;

    /// <summary>잘라낸 레이어: 스프라이트 + 화면 기준 위치(px, 왼쪽 위 원점)</summary>
    struct Layer
    {
        public Sprite Sprite;
        public Rect Px;
        public Vector2 World => new((Px.center.x - ScreenW / 2f) / PixelsPerUnit, (ScreenH / 2f - Px.center.y) / PixelsPerUnit);
        public Vector2 Size => Px.size / PixelsPerUnit;
    }

    [MenuItem("Still Beating/챕터1 아트 적용")]
    static void Apply()
    {
        if (!EditorUtility.DisplayDialog("Still Beating",
                "Assets/Art/Chapter1/Layers 의 그림을 Chapter1 씬에 적용합니다.\n오브젝트 위치·벽·HUD가 그림에 맞게 바뀝니다. (밸런스 수치는 그대로)\n\n계속할까요?",
                "적용", "취소"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var scene = EditorSceneManager.OpenScene(ScenePath);

        var frame = FullScreen("Frame");
        var room = Crop("Room");
        ApplyRoom(frame, room);

        ApplyDoor(Crop("IronDoor"));
        ApplyObject(Find("침대"), Crop("Bed"));
        ApplyDrawer(Find("음식서랍_왼쪽"), Crop("Drawer_TopLeft"), timerBelow: true);
        ApplyDrawer(Find("음식서랍_오른쪽"), Crop("Drawer_TopRight"), timerBelow: true);
        ApplyDrawer(FindOrCreateDrawer("음식서랍_아래"), Crop("Drawer_BottomRight"), timerBelow: false);

        ApplyHud(Crop("UI_HeartRate"), Crop("UI_Drumstick"), Crop("UI_DrumstickHalf"), Crop("UI_Pocket"), Crop("UI_EscapeTimer"));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Still Beating", "완료! Chapter1 씬에 아트를 적용했습니다.\nPlay로 위치와 충돌을 확인해 보세요.", "확인");
    }

    // ───────────────────────── 방 (배경, 벽)

    static void ApplyRoom(Sprite frame, Layer room)
    {
        var roomRoot = Find("Room").transform;

        // 임시 바닥/벽 그림은 숨기고 충돌만 남긴다
        foreach (var sr in roomRoot.GetComponentsInChildren<SpriteRenderer>(true))
            if (!sr.name.StartsWith("Art_")) SetEnabled(sr, false);

        PlaceLayer(roomRoot, "Art_Frame", frame, Vector2.zero, -30);
        PlaceLayer(roomRoot, "Art_Room", room.Sprite, room.World, -20);

        // 그림의 방 테두리에 맞춰 벽 충돌 위치 조정
        float left = room.World.x - room.Size.x / 2f, right = room.World.x + room.Size.x / 2f;
        float top = room.World.y + room.Size.y / 2f, bottom = room.World.y - room.Size.y / 2f;
        float floorTop = (ScreenH / 2f - TileWallBottomPx) / PixelsPerUnit;
        float midX = (left + right) / 2f, midY = (floorTop + bottom) / 2f;
        const float t = 0.5f, border = 0.06f; // 벽 두께, 그림 테두리 선 두께

        SetBlock(roomRoot, "Wall_Left", new Vector2(left + border - t / 2f, midY), new Vector2(t, floorTop - bottom + 1f));
        SetBlock(roomRoot, "Wall_Right", new Vector2(right - border + t / 2f, midY), new Vector2(t, floorTop - bottom + 1f));
        SetBlock(roomRoot, "Wall_Bottom", new Vector2(midX, bottom + border - t / 2f), new Vector2(right - left + 1f, t));
        SetBlock(roomRoot, "Wall_Top (타일벽)", new Vector2(midX, (floorTop + top) / 2f), new Vector2(right - left + 1f, top - floorTop));
    }

    static void PlaceLayer(Transform parent, string name, Sprite sprite, Vector2 position, int order)
    {
        var t = parent.Find(name);
        var go = t != null ? t.gameObject : new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.localScale = Vector3.one;
        if (!go.TryGetComponent(out SpriteRenderer sr)) sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = Color.white;
        sr.sortingOrder = order;
    }

    static void SetBlock(Transform roomRoot, string name, Vector2 position, Vector2 size)
    {
        var t = roomRoot.Find(name);
        if (t == null) { Debug.LogWarning($"[챕터1 아트] Room/{name} 을(를) 찾을 수 없음"); return; }
        Undo.RecordObject(t, "Wall");
        t.position = position;
        t.localScale = new Vector3(size.x, size.y, 1f);
    }

    // ───────────────────────── 오브젝트

    static void ApplyDoor(Layer bars)
    {
        var door = Find("문");
        ApplyObject(door, bars, setCollider: false);

        // 트리거는 문 높이 + 방 안쪽으로 조금 더 (벽에 붙으면 들어간 것으로 처리)
        var trigger = door.GetComponent<BoxCollider2D>();
        Undo.RecordObject(trigger, "Door");
        trigger.size = bars.Size + new Vector2(0f, 0.6f);
        trigger.offset = new Vector2(0f, -0.3f);
        PrefabUtility.RecordPrefabInstancePropertyModifications(trigger);

        // 문짝 뒤 어두운 통로 = 철창 크기 그대로
        var doorway = door.transform.Find("Doorway");
        if (doorway != null)
        {
            Undo.RecordObject(doorway, "Doorway");
            doorway.localPosition = Vector3.zero;
            doorway.localScale = new Vector3(bars.Size.x, bars.Size.y, 1f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(doorway);
        }
    }

    static void ApplyDrawer(GameObject drawer, Layer art, bool timerBelow)
    {
        ApplyObject(drawer, art);
        var timer = drawer.transform.Find("Timer");
        if (timer == null) return;
        Undo.RecordObject(timer, "Timer");
        float y = art.Size.y / 2f + 0.3f;
        timer.localPosition = new Vector3(0f, timerBelow ? -y : y, 0f);
        PrefabUtility.RecordPrefabInstancePropertyModifications(timer);
    }

    // 오브젝트를 그림 위치로 옮기고, 충돌 범위를 그림 크기로, Sprite 자식에 그림을 넣는다
    static void ApplyObject(GameObject go, Layer art, bool setCollider = true)
    {
        if (go == null) return;

        Undo.RecordObject(go.transform, "Art");
        go.transform.position = art.World;
        PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);

        if (setCollider && go.TryGetComponent(out BoxCollider2D col))
        {
            Undo.RecordObject(col, "Art");
            col.size = art.Size;
            col.offset = Vector2.zero;
            PrefabUtility.RecordPrefabInstancePropertyModifications(col);
        }

        var spriteTransform = go.transform.Find("Sprite");
        if (spriteTransform == null) return;
        Undo.RecordObject(spriteTransform, "Art");
        spriteTransform.localPosition = Vector3.zero;
        spriteTransform.localScale = Vector3.one;
        PrefabUtility.RecordPrefabInstancePropertyModifications(spriteTransform);

        var sr = spriteTransform.GetComponent<SpriteRenderer>();
        Undo.RecordObject(sr, "Art");
        sr.sprite = art.Sprite;
        sr.color = Color.white;
        PrefabUtility.RecordPrefabInstancePropertyModifications(sr);
    }

    static GameObject FindOrCreateDrawer(string name)
    {
        var existing = GameObject.Find(name);
        if (existing != null) return existing;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/FoodDrawer.prefab");
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = name;
        return go;
    }

    // ───────────────────────── HUD

    static void ApplyHud(Layer heart, Layer drumFull, Layer drumHalf, Layer pocket, Layer timer)
    {
        var hudCanvas = Find("HUD");
        var hud = hudCanvas.GetComponent<GameHUD>();
        var root = hudCanvas.transform.Find("HudRoot");

        SetActive(root.Find("BottomPanel"), false); // 하단 바는 Frame 그림에 포함

        // 심전도: 검정 상자는 하트 그림에 포함, 움직이는 선만 상자 위에 올린다.
        // 상자는 하트 그림의 오른쪽 약 70% 영역 (그림에서 잰 비율)
        var ecg = (RectTransform)root.Find("ECG");
        if (ecg != null)
        {
            ecg.gameObject.SetActive(true);
            var box = heart.Px;
            box.xMin += heart.Px.width * 0.32f;
            box.xMax -= 4f;
            box.yMin += heart.Px.height * 0.22f;
            box.yMax -= heart.Px.height * 0.13f;
            PlaceUI(ecg, box);
            ecg.GetComponent<Image>().enabled = false; // 검정 배경은 그림에 있음
            var line = ecg.GetComponentInChildren<EcgGraph>(true);
            if (line != null) line.color = new Color(0.2f, 0.9f, 0.35f); // 그림과 같은 초록
        }

        // 하트: 그림 + 심박수 숫자는 하트 부분(왼쪽 약 1/3) 가운데
        var heartRect = (RectTransform)root.Find("HeartRate");
        SetImage(heartRect, heart.Sprite);
        PlaceUI(heartRect, heart.Px);
        var heartText = (RectTransform)heartRect.Find("Text");
        heartText.anchorMin = Vector2.zero;
        heartText.anchorMax = new Vector2(0.32f, 1f);
        heartText.offsetMin = heartText.offsetMax = Vector2.zero;
        heartText.GetComponent<Text>().fontSize = 22;

        // 닭다리 5개: 그림에는 2개만 그려져 있어서, 하트와 탈출 타이머 사이 공간에 고르게 배치
        int drumCount = 0;
        while (root.Find($"Drumstick_{drumCount + 1}") != null) drumCount++;
        const float margin = 10f;
        float areaLeft = heart.Px.xMax + margin, areaRight = timer.Px.xMin - margin;
        float step = (areaRight - areaLeft) / Mathf.Max(1, drumCount);
        for (int i = 0; i < drumCount; i++)
        {
            var slot = (RectTransform)root.Find($"Drumstick_{i + 1}");
            var px = drumFull.Px;
            px.x = areaLeft + step * (i + 0.5f) - px.width / 2f;
            PlaceUI(slot, px);
            slot.GetComponent<Image>().enabled = false; // 흐린 배경 닭다리는 숨김
            // 그림 크기가 바뀌어도(반 개) 가운데 기준으로 원본 크기 표시되도록
            var fill = (RectTransform)slot.Find("Fill");
            fill.anchorMin = fill.anchorMax = fill.pivot = new Vector2(0.5f, 0.5f);
            fill.anchoredPosition = Vector2.zero;
            fill.sizeDelta = drumFull.Px.size;
            SetImage(fill, drumFull.Sprite);
            fill.GetComponent<Image>().type = Image.Type.Simple;
        }
        var so = new SerializedObject(hud);
        so.FindProperty("drumstickFull").objectReferenceValue = drumFull.Sprite;
        so.FindProperty("drumstickHalf").objectReferenceValue = drumHalf.Sprite;
        so.ApplyModifiedPropertiesWithoutUndo();

        // 주머니 2칸: 그림 위치부터 오른쪽으로
        for (int i = 0; ; i++)
        {
            var slot = (RectTransform)root.Find($"Pocket_{i + 1}");
            if (slot == null) break;
            var px = pocket.Px;
            px.x += i * (pocket.Px.width + 20f);
            PlaceUI(slot, px);
            SetImage(slot, pocket.Sprite);
            SetActive(slot.Find("Inner"), false);
            var food = (RectTransform)slot.Find("Food");
            food.sizeDelta = pocket.Px.size * 0.7f;
            food.GetComponent<Image>().sprite = drumFull.Sprite;
        }

        // 탈출 타이머: 검정 상자 그림 + 빨간 숫자
        var timerRect = (RectTransform)root.Find("EscapeTimer");
        PlaceUI(timerRect, timer.Px);
        SetImage(timerRect, timer.Sprite);
        SetActive(timerRect.Find("Background"), false);
    }

    // 화면 px(왼쪽 위 원점) 위치를 HUD 좌표(1920x1080, 왼쪽 아래 기준)로
    static void PlaceUI(RectTransform rt, Rect px)
    {
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(px.center.x, ScreenH - px.center.y);
        rt.sizeDelta = px.size;
    }

    static void SetImage(RectTransform rt, Sprite sprite)
    {
        var image = rt.GetComponent<Image>();
        image.sprite = sprite;
        image.color = Color.white;
        image.preserveAspect = true;
    }

    // ───────────────────────── 그림 자르기 / 불러오기

    // 레이어에서 그림 부분만 잘라 저장하고 위치를 함께 돌려준다.
    // 그림 파일에 흩어진 작은 점(지우개 자국 등)이 있을 수 있어서, 가장 큰 그림 덩어리만 기준으로 자른다.
    static Layer Crop(string name)
    {
        var source = LoadTexture($"{LayerFolder}/{name}.png");
        var pixels = source.GetPixels32();
        int w = source.width, h = source.height;

        var (minX, minY, maxX, maxY) = LargestBlobBounds(pixels, w, h);
        if (maxX < 0) throw new System.Exception($"[챕터1 아트] {name}.png 에 그림이 없습니다");

        int cw = maxX - minX + 1, ch = maxY - minY + 1;
        var cropped = new Texture2D(cw, ch, TextureFormat.RGBA32, false);
        cropped.SetPixels(source.GetPixels(minX, minY, cw, ch));
        cropped.Apply();

        string path = $"{OutFolder}/{name}.png";
        File.WriteAllBytes(path, cropped.EncodeToPNG());
        Object.DestroyImmediate(cropped);
        Object.DestroyImmediate(source);

        // 텍스처는 y가 아래부터라서, 화면 기준(위에서부터)으로 바꿔서 기록
        return new Layer { Sprite = ImportSprite(path), Px = new Rect(minX, h - 1 - maxY, cw, ch) };
    }

    // 불투명한 픽셀끼리 이어진 덩어리들을 찾아, 작은 점(가장 큰 덩어리의 2% 미만)을 뺀 나머지 전체 범위
    // (텍스처 좌표, y는 아래부터)
    static (int minX, int minY, int maxX, int maxY) LargestBlobBounds(Color32[] pixels, int w, int h)
    {
        const byte AlphaThreshold = 32;
        var visited = new bool[pixels.Length];
        var stack = new System.Collections.Generic.Stack<int>();
        var blobs = new System.Collections.Generic.List<(int count, int minX, int minY, int maxX, int maxY)>();

        for (int start = 0; start < pixels.Length; start++)
        {
            if (visited[start] || pixels[start].a <= AlphaThreshold) continue;

            int count = 0, minX = w, minY = h, maxX = -1, maxY = -1;
            visited[start] = true;
            stack.Push(start);
            while (stack.Count > 0)
            {
                int i = stack.Pop(), x = i % w, y = i / w;
                count++;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;

                // 상하좌우 + 대각선으로 이어진 픽셀 (선이 가늘어도 끊기지 않게)
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                        int n = ny * w + nx;
                        if (visited[n] || pixels[n].a <= AlphaThreshold) continue;
                        visited[n] = true;
                        stack.Push(n);
                    }
            }

            blobs.Add((count, minX, minY, maxX, maxY));
        }

        int largest = 0;
        foreach (var b in blobs) largest = Mathf.Max(largest, b.count);
        int minCount = Mathf.Max(30, largest / 50);

        var bounds = (minX: w, minY: h, maxX: -1, maxY: -1);
        foreach (var b in blobs)
        {
            if (b.count < minCount) continue; // 흩어진 작은 점은 무시
            bounds = (Mathf.Min(bounds.minX, b.minX), Mathf.Min(bounds.minY, b.minY),
                      Mathf.Max(bounds.maxX, b.maxX), Mathf.Max(bounds.maxY, b.maxY));
        }
        return bounds;
    }

    // 화면 전체 크기 그대로 쓰는 레이어 (프레임 등)
    static Sprite FullScreen(string name) => ImportSprite($"{LayerFolder}/{name}.png");

    static Texture2D LoadTexture(string path)
    {
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.LoadImage(File.ReadAllBytes(path));
        return texture;
    }

    static Sprite ImportSprite(string path)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.mipmapEnabled = true; // 작은 화면에서 축소될 때 점 무늬(모아레)가 생기지 않게
        importer.filterMode = FilterMode.Trilinear;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed; // 선이 뭉개지지 않게
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // ───────────────────────── 공통

    static GameObject Find(string name)
    {
        var go = GameObject.Find(name);
        if (go == null) Debug.LogWarning($"[챕터1 아트] 씬에서 '{name}' 을(를) 찾을 수 없음");
        return go;
    }

    static void SetActive(Transform t, bool active)
    {
        if (t != null) t.gameObject.SetActive(active);
    }

    static void SetEnabled(Renderer r, bool enabled)
    {
        Undo.RecordObject(r, "Art");
        r.enabled = enabled;
        PrefabUtility.RecordPrefabInstancePropertyModifications(r);
    }
}
