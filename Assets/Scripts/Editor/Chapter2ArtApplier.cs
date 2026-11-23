using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 메뉴 [Still Beating > 챕터2 아트 적용 (맵2)]
/// 아트 레이어(Assets/Art/Chapter2/Layers, 1920x1080 화면 기준으로 제자리에 그려진 PNG)를 Chapter2 씬에 적용한다.
///  - Room: 테두리·아래 회색 바까지 포함된 방 그림. 투명하게 비어 있는 곳은 흰색으로 채움 (그린 사람 화면과 같게)
///  - VendingMachine: 왼쪽 자판기. 오른쪽 자판기 그림이 오기 전까지는 같은 그림을 반대편에 사용
///  - Door / Doorway: 출입문(옆으로 열림)과 그 뒤 검은 통로
///  - InfoDesk: 안내 데스크. 오른쪽 위(그림 위치)와 오른쪽 아래(같은 그림, 아래 벽에 붙임)
///  - Sofa: 가운데 휴식용 소파
///  - Chair: 왼쪽 의자. 왼쪽 위(그림 위치)와 왼쪽 아래(같은 그림, 아래 벽에 붙임)
/// 화면 1920x1080 = 카메라 세로 10.8유닛이라 1유닛 = 100px 로 1:1 대응한다.
/// </summary>
static class Chapter2ArtApplier
{
    const string LayerFolder = "Assets/Art/Chapter2/Layers";
    const string OutFolder = "Assets/Art/Chapter2";
    const string ScenePath = "Assets/Scenes/Chapter2.unity";
    const float PixelsPerUnit = 100f;
    const float ScreenW = 1920f, ScreenH = 1080f;

    // 방 그림에서 직접 잰 값 (px, 화면 왼쪽 위 원점)
    const float RoomLeftPx = 179f;   // 왼쪽 벽 안쪽
    const float RoomRightPx = 1720f; // 오른쪽 벽 안쪽
    const float RoomTopPx = 15f;     // 위쪽 테두리
    const float FloorTopPx = 178f;   // 위쪽 벽(초록 띠 + 흰 띠)이 끝나고 바닥이 시작되는 높이
    const float RoomBottomPx = 916f; // 아래 벽 안쪽

    struct Layer
    {
        public Sprite Sprite;
        public Rect Px;
        public Vector2 World => new((Px.center.x - ScreenW / 2f) / PixelsPerUnit, (ScreenH / 2f - Px.center.y) / PixelsPerUnit);
        public Vector2 Size => Px.size / PixelsPerUnit;
    }

    static float ToWorldX(float px) => (px - ScreenW / 2f) / PixelsPerUnit;
    static float ToWorldY(float px) => (ScreenH / 2f - px) / PixelsPerUnit;

    [MenuItem("Still Beating/챕터2 아트 적용 (맵2)")]
    static void Apply()
    {
        if (!EditorUtility.DisplayDialog("Still Beating",
                "Assets/Art/Chapter2/Layers 의 맵2 그림을 Chapter2 씬에 적용합니다.\n\n" +
                "• 방, 벽 충돌, 출입문, 자판기, 안내 데스크(오른쪽 위·아래)\n" +
                "• 가운데 소파, 왼쪽 의자(위·아래)\n" +
                "• 오른쪽 자판기: 그림이 올 때까지 왼쪽 자판기 그림을 같이 사용\n\n" +
                "밸런스 수치는 그대로예요. 계속할까요?",
                "적용", "취소"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        if (!AssetDatabase.IsValidFolder(OutFolder)) AssetDatabase.CreateFolder("Assets/Art", "Chapter2");
        var scene = EditorSceneManager.OpenScene(ScenePath);

        ApplyRoom(MakeRoom());

        var vending = Crop("VendingMachine");
        ApplyDrawer(Find("자판기_왼쪽"), vending);
        ApplyDrawer(Find("자판기_오른쪽"), Mirror(vending));

        ApplyDoor(Find("출입문"), Crop("Door"), Crop("Doorway"));

        // 안내 데스크: 오른쪽 위는 그림 위치 그대로, 오른쪽 아래는 같은 그림을 아래 벽에 붙여서
        // (사이 틈으로 적이 드나듦)
        var desk = Crop("InfoDesk");
        ApplyBlock(Find("안내데스크_위"), desk);
        ApplyBlock(Find("안내데스크_아래"), AtBottom(desk));

        // 가운데 휴식용 소파 (쓰고 나면 어두워짐)
        ApplyObject(Find("휴식용 소파"), Crop("Sofa"));

        // 왼쪽 의자: 위는 그림 위치 그대로, 아래는 같은 그림을 아래 벽에 붙여서 (사이 틈으로 적이 드나듦)
        // 처음엔 임시 이름이 '계단'이었다 → '의자'로 정리
        var chair = Crop("Chair");
        ApplyBlock(Rename(FindEither("의자_위", "계단_위"), "의자_위"), chair);
        ApplyBlock(Rename(FindEither("의자_아래", "계단_아래"), "의자_아래"), AtBottom(chair));

        // 왼쪽 틈에서 시작하는 적이 의자와 겹치지 않게 틈 한가운데로
        var chaser = FindEither("적_추적 (의자 쪽)", "적_추적 (계단 쪽)");
        if (chaser != null)
        {
            float gapTop = ToWorldY(chair.Px.yMax), gapBottom = ToWorldY(AtBottom(chair).Px.yMin);
            Undo.RecordObject(chaser.transform, "Enemy");
            chaser.transform.position = new Vector3(chaser.transform.position.x, (gapTop + gapBottom) / 2f, 0f);
            chaser.name = "적_추적 (의자 쪽)";
            PrefabUtility.RecordPrefabInstancePropertyModifications(chaser.transform);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Still Beating", "완료! Chapter2 씬에 맵2 그림을 적용했습니다.\nPlay로 벽·문·자판기 위치를 확인해 보세요.", "확인");
    }

    // ───────────────────────── 방 (배경, 벽)

    // 방 그림의 투명한 곳을 채워서 저장: 방 안(위 띠, 벽 아래 흰 띠 등)은 흰색, 바깥 테두리는 테두리 회색
    static Sprite MakeRoom()
    {
        var texture = LoadTexture($"{LayerFolder}/Room.png");
        int w = texture.width, h = texture.height;
        var pixels = texture.GetPixels32();
        var frame = pixels[(h - 1 - 5) * w + 5]; // 화면 왼쪽 위 테두리 색
        frame.a = 255;
        for (int i = 0; i < pixels.Length; i++)
        {
            var c = pixels[i];
            if (c.a == 255) continue;
            int x = i % w, screenY = h - 1 - i / w; // 텍스처는 y가 아래부터
            bool inside = x >= RoomLeftPx - 8 && x <= RoomRightPx + 8 && screenY >= RoomTopPx && screenY <= RoomBottomPx + 8;
            Color32 bg = inside ? new Color32(255, 255, 255, 255) : frame;
            float a = c.a / 255f;
            pixels[i] = new Color32(
                (byte)Mathf.RoundToInt(c.r * a + bg.r * (1f - a)),
                (byte)Mathf.RoundToInt(c.g * a + bg.g * (1f - a)),
                (byte)Mathf.RoundToInt(c.b * a + bg.b * (1f - a)), 255);
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        string path = $"{OutFolder}/Room.png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        return ImportSprite(path);
    }

    static void ApplyRoom(Sprite room)
    {
        var roomRoot = Find("Room").transform;

        // 임시 바닥·벽 그림은 숨기고 충돌만 남긴다 (계단·안내 데스크 임시 도형은 그림이 올 때까지 보이게 둠)
        foreach (var sr in roomRoot.GetComponentsInChildren<SpriteRenderer>(true))
            if (sr.name == "Floor" || sr.name.StartsWith("Wall_")) sr.enabled = false;

        PlaceLayer(roomRoot, "Art_Room", room, Vector2.zero, -20);

        float left = ToWorldX(RoomLeftPx), right = ToWorldX(RoomRightPx);
        float top = ToWorldY(RoomTopPx), floorTop = ToWorldY(FloorTopPx), bottom = ToWorldY(RoomBottomPx);
        float midX = (left + right) / 2f, midY = (floorTop + bottom) / 2f;
        const float t = 0.5f; // 벽 두께

        SetBlock(roomRoot, "Wall_Left", new Vector2(left - t / 2f, midY), new Vector2(t, floorTop - bottom + 1f));
        SetBlock(roomRoot, "Wall_Right", new Vector2(right + t / 2f, midY), new Vector2(t, floorTop - bottom + 1f));
        SetBlock(roomRoot, "Wall_Bottom", new Vector2(midX, bottom - t / 2f), new Vector2(right - left + 1f, t));
        SetBlock(roomRoot, "Wall_Top (타일벽)", new Vector2(midX, (floorTop + top) / 2f), new Vector2(right - left + 1f, top - floorTop));
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
        if (t == null) { Debug.LogWarning($"[챕터2 아트] Room/{name} 을(를) 찾을 수 없음"); return; }
        t.position = position;
        t.localScale = new Vector3(size.x, size.y, 1f);
    }

    // ───────────────────────── 오브젝트

    // 프리팹 오브젝트(자판기, 문): 그림 위치로 옮기고, 충돌 범위를 그림 크기로, Sprite 자식에 그림
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

    // 자판기 = 서랍과 같은 기능. 남은 시간 글자는 자판기 아래
    static void ApplyDrawer(GameObject drawer, Layer art)
    {
        ApplyObject(drawer, art);
        if (drawer == null) return;
        var timer = drawer.transform.Find("Timer");
        if (timer == null) return;
        Undo.RecordObject(timer, "Timer");
        timer.localPosition = new Vector3(0f, -(art.Size.y / 2f + 0.3f), 0f);
        PrefabUtility.RecordPrefabInstancePropertyModifications(timer);
    }

    static void ApplyDoor(GameObject door, Layer panel, Layer doorway)
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

        // 문이 옆으로 밀리면 보이는 검은 통로 = 받은 그림 그대로
        var t = door.transform.Find("Doorway");
        if (t == null) return;
        Undo.RecordObject(t, "Doorway");
        t.position = doorway.World;
        t.localScale = Vector3.one;
        PrefabUtility.RecordPrefabInstancePropertyModifications(t);
        if (t.TryGetComponent(out SpriteRenderer sr))
        {
            Undo.RecordObject(sr, "Doorway");
            sr.sprite = doorway.Sprite;
            sr.color = Color.white;
            PrefabUtility.RecordPrefabInstancePropertyModifications(sr);
        }
    }

    // 씬에 바로 만든 도형(안내 데스크): 그림·위치·충돌을 그림에 맞춤
    static void ApplyBlock(GameObject go, Layer art)
    {
        if (go == null) return;
        go.transform.position = art.World;
        go.transform.localScale = Vector3.one;
        if (go.TryGetComponent(out SpriteRenderer sr))
        {
            sr.sprite = art.Sprite;
            sr.color = Color.white;
            sr.sortingOrder = 0;
        }
        if (go.TryGetComponent(out BoxCollider2D col))
        {
            col.size = art.Size;
            col.offset = Vector2.zero;
        }
    }

    // 같은 그림을 같은 가로 위치에서 아래 벽에 붙인 위치
    static Layer AtBottom(Layer art)
    {
        var px = art.Px;
        px.y = RoomBottomPx - px.height;
        return new Layer { Sprite = art.Sprite, Px = px };
    }

    // 방 가운데를 기준으로 좌우 반대편 위치 (그림은 그대로)
    static Layer Mirror(Layer art)
    {
        var px = art.Px;
        px.x = RoomLeftPx + RoomRightPx - (px.x + px.width);
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
        if (maxX < 0) throw new System.Exception($"[챕터2 아트] {name}.png 에 그림이 없습니다");

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

    // 새 이름으로 먼저 찾고, 없으면 예전 이름으로 (메뉴를 여러 번 실행해도 되게)
    static GameObject FindEither(string name, string oldName)
    {
        var go = GameObject.Find(name);
        return go != null ? go : Find(oldName);
    }

    static GameObject Rename(GameObject go, string name)
    {
        if (go != null) go.name = name;
        return go;
    }

    static GameObject Find(string name)
    {
        var go = GameObject.Find(name);
        if (go == null) Debug.LogWarning($"[챕터2 아트] 씬에서 '{name}' 을(를) 찾을 수 없음");
        return go;
    }
}
