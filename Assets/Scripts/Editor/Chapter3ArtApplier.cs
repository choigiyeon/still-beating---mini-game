using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 메뉴 [Still Beating > 챕터3 아트 적용 (맵3)]
/// 아트 레이어(Assets/Art/Chapter3/Layers, 1920x1080 화면 기준으로 제자리에 그려진 PNG)를 Chapter3 씬에 적용한다.
///  - Room: 잔디 바닥 + 위쪽 울타리. 문 자리(하늘색)는 문이 열리면 보이는 바깥
///    방 바깥 테두리·아래 회색 바는 챕터1 Frame 그림을 같이 씀 (챕터1과 같은 화면 틀)
///  - WoodDoor: 나무 문 (옆으로 열림)
///  - StorageBox: 나무 보관함(음식). 왼쪽 위(그림 위치)와 왼쪽 아래(같은 그림, 아래 벽에 붙임)
///  - Bench: 오른쪽 나무 벤치 (침대와 같은 기능)
///  - Pond: 연못 (빠지면 게임오버, 적은 피해 감)
/// 화면 1920x1080 = 카메라 세로 10.8유닛이라 1유닛 = 100px 로 1:1 대응한다.
/// </summary>
static class Chapter3ArtApplier
{
    const string LayerFolder = "Assets/Art/Chapter3/Layers";
    const string OutFolder = "Assets/Art/Chapter3";
    const string ScenePath = "Assets/Scenes/Chapter3.unity";
    const string FramePath = "Assets/Art/Chapter1/Layers/Frame.png"; // 챕터1과 같은 화면 틀
    const float PixelsPerUnit = 100f;
    const float ScreenW = 1920f, ScreenH = 1080f;

    // 방 그림에서 직접 잰 값 (px, 화면 왼쪽 위 원점)
    const float RoomLeftPx = 168f;   // 왼쪽 벽 안쪽
    const float RoomRightPx = 1724f; // 오른쪽 벽 안쪽
    const float RoomTopPx = 21f;     // 위쪽 테두리
    const float FloorTopPx = 180f;   // 울타리가 끝나고 잔디가 시작되는 높이
    const float RoomBottomPx = 913f; // 아래 벽 안쪽

    // 연못: 충돌(적을 막음)은 돌 테두리까지 그림 전체, 빠지는 판정은 안쪽 물 부분만 (그림에서 잰 물 영역 ≈ 80%)
    const float PondWaterRatio = 0.8f;

    struct Layer
    {
        public Sprite Sprite;
        public Rect Px;
        public Vector2 World => new((Px.center.x - ScreenW / 2f) / PixelsPerUnit, (ScreenH / 2f - Px.center.y) / PixelsPerUnit);
        public Vector2 Size => Px.size / PixelsPerUnit;
    }

    static float ToWorldX(float px) => (px - ScreenW / 2f) / PixelsPerUnit;
    static float ToWorldY(float px) => (ScreenH / 2f - px) / PixelsPerUnit;

    [MenuItem("Still Beating/챕터3 아트 적용 (맵3)")]
    static void Apply()
    {
        if (!EditorUtility.DisplayDialog("Still Beating",
                "Assets/Art/Chapter3/Layers 의 맵3 그림을 Chapter3 씬에 적용합니다.\n\n" +
                "• 방(잔디·울타리), 벽 충돌, 나무 문\n" +
                "• 나무 보관함(왼쪽 위·아래), 나무 벤치, 연못\n" +
                "• 화면 바깥 테두리·아래 회색 바는 챕터1과 같은 그림\n\n" +
                "밸런스 수치는 그대로예요. 계속할까요?",
                "적용", "취소"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        if (!AssetDatabase.IsValidFolder(OutFolder)) AssetDatabase.CreateFolder("Assets/Art", "Chapter3");
        var scene = EditorSceneManager.OpenScene(ScenePath);

        ApplyRoom(Crop("Room"));

        ApplyDoor(Find("나무 문"), Crop("WoodDoor"));

        // 나무 보관함: 위는 그림 위치 그대로, 아래는 같은 그림을 아래 벽에 붙여서 (사이 틈으로 지나다님)
        var box = Crop("StorageBox");
        ApplyDrawer(Find("나무 보관함_위"), box);
        ApplyDrawer(Find("나무 보관함_아래"), AtBottom(box));

        ApplyObject(Find("나무 벤치"), Crop("Bench"));
        ApplyPond(Find("연못"), Crop("Pond"));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Still Beating", "완료! Chapter3 씬에 맵3 그림을 적용했습니다.\nPlay로 벽·문·연못 위치를 확인해 보세요.", "확인");
    }

    // ───────────────────────── 방 (배경, 벽)

    static void ApplyRoom(Layer room)
    {
        var roomRoot = Find("Room").transform;

        // 임시 바닥·벽 그림은 숨기고 충돌만 남긴다
        foreach (var sr in roomRoot.GetComponentsInChildren<SpriteRenderer>(true))
            if (!sr.name.StartsWith("Art_")) sr.enabled = false;

        var frame = AssetDatabase.LoadAssetAtPath<Sprite>(FramePath);
        if (frame != null) PlaceLayer(roomRoot, "Art_Frame", frame, Vector2.zero, -30);
        else Debug.LogWarning($"[챕터3 아트] {FramePath} 를 찾을 수 없음 (먼저 [챕터1 아트 적용]을 한 번 실행해야 함)");
        PlaceLayer(roomRoot, "Art_Room", room.Sprite, room.World, -20);

        float left = ToWorldX(RoomLeftPx), right = ToWorldX(RoomRightPx);
        float top = ToWorldY(RoomTopPx), floorTop = ToWorldY(FloorTopPx), bottom = ToWorldY(RoomBottomPx);
        float midX = (left + right) / 2f, midY = (floorTop + bottom) / 2f;
        const float t = 0.5f; // 벽 두께

        SetBlock(roomRoot, "Wall_Left", new Vector2(left - t / 2f, midY), new Vector2(t, floorTop - bottom + 1f));
        SetBlock(roomRoot, "Wall_Right", new Vector2(right + t / 2f, midY), new Vector2(t, floorTop - bottom + 1f));
        SetBlock(roomRoot, "Wall_Bottom", new Vector2(midX, bottom - t / 2f), new Vector2(right - left + 1f, t));
        SetBlock(roomRoot, "Wall_Top (울타리)", new Vector2(midX, (floorTop + top) / 2f), new Vector2(right - left + 1f, top - floorTop));
    }

    static void PlaceLayer(Transform parent, string name, Sprite sprite, Vector2 position, int order)
    {
        var existing = parent.Find(name);
        var go = existing != null ? existing.gameObject : new GameObject(name);
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
        if (t == null) { Debug.LogWarning($"[챕터3 아트] Room/{name} 을(를) 찾을 수 없음"); return; }
        t.position = position;
        t.localScale = new Vector3(size.x, size.y, 1f);
    }

    // ───────────────────────── 오브젝트

    // 프리팹 오브젝트: 그림 위치로 옮기고, 충돌 범위를 그림 크기로, Sprite 자식에 그림
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

    // 나무 보관함 = 서랍과 같은 기능. 남은 시간 글자는 보관함 위 (아래쪽은 벽이라)
    static void ApplyDrawer(GameObject drawer, Layer art)
    {
        ApplyObject(drawer, art);
        if (drawer == null) return;
        var timer = drawer.transform.Find("Timer");
        if (timer == null) return;
        Undo.RecordObject(timer, "Timer");
        timer.localPosition = new Vector3(0f, art.Size.y / 2f + 0.3f, 0f);
        PrefabUtility.RecordPrefabInstancePropertyModifications(timer);
    }

    static void ApplyDoor(GameObject door, Layer panel)
    {
        if (door == null) return;
        ApplyObject(door, panel, setCollider: false);

        // 들어가는 판정: 문 크기 + 방 안쪽으로 조금 더 (벽에 붙으면 들어간 것으로 처리)
        if (door.TryGetComponent(out BoxCollider2D trigger))
        {
            Undo.RecordObject(trigger, "Door");
            trigger.size = panel.Size + new Vector2(0f, 0.6f);
            trigger.offset = new Vector2(0f, -0.3f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(trigger);
        }

        // 문 뒤는 방 그림에 그려진 바깥(하늘색)이 그대로 보이게: 임시 검은 통로는 끈다
        var doorway = door.transform.Find("Doorway");
        if (doorway != null && doorway.TryGetComponent(out SpriteRenderer sr))
        {
            Undo.RecordObject(sr, "Doorway");
            sr.enabled = false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(sr);
        }
    }

    // 연못: 테두리(적을 막음) = 그림 크기, 빠지는 판정 = 안쪽 물 부분
    static void ApplyPond(GameObject pond, Layer art)
    {
        if (pond == null) return;
        ApplyObject(pond, art, setCollider: false);

        var direction = art.Size.x >= art.Size.y ? CapsuleDirection2D.Horizontal : CapsuleDirection2D.Vertical;
        foreach (var col in pond.GetComponents<CapsuleCollider2D>())
        {
            Undo.RecordObject(col, "Pond");
            col.direction = direction;
            col.size = col.isTrigger ? art.Size * PondWaterRatio : art.Size;
            col.offset = Vector2.zero;
            PrefabUtility.RecordPrefabInstancePropertyModifications(col);
        }
    }

    // 같은 그림을 같은 가로 위치에서 아래 벽에 붙인 위치
    static Layer AtBottom(Layer art)
    {
        var px = art.Px;
        px.y = RoomBottomPx - px.height;
        return new Layer { Sprite = art.Sprite, Px = px };
    }

    // ───────────────────────── 그림 자르기

    // 레이어에서 그림 부분만 잘라 OutFolder에 저장하고, 화면 기준 위치(px)와 함께 돌려준다
    static Layer Crop(string name)
    {
        var source = LoadTexture($"{LayerFolder}/{name}.png");
        var pixels = source.GetPixels32();
        int w = source.width, h = source.height;

        var (minX, minY, maxX, maxY) = Chapter1ArtApplier.LargestBlobBounds(pixels, w, h);
        if (maxX < 0) throw new System.Exception($"[챕터3 아트] {name}.png 에 그림이 없습니다");

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
        importer.mipmapEnabled = true;
        importer.filterMode = FilterMode.Trilinear;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static GameObject Find(string name)
    {
        var go = GameObject.Find(name);
        if (go == null) Debug.LogWarning($"[챕터3 아트] 씬에서 '{name}' 을(를) 찾을 수 없음");
        return go;
    }
}
