using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 메뉴 [Still Beating > 캐릭터 아트 적용]
/// Assets/Art/Characters 의 캐릭터 그림을 프리팹에 적용하고 걷기 애니메이션을 붙인다.
///  - 그림 크기(PPU): 캐릭터 키가 targetHeight 유닛이 되도록 자동 계산
///  - 기준점(Pivot): 발 위치. 같은 방향의 서기/걷기 그림은 같은 기준점이라 걸을 때 흔들리지 않음
///  - Player: 정면/뒷모습/옆 × 서기/걷기 → PlayerAnimator
///  - 적: 좀비(서기/걷기) → Enemy_Chase, 유령(1장) → Enemy_Wander  ※ 어느 적이 어느 그림인지는 임시 배정
/// 캐릭터 그림은 투명 배경 + 프레임마다 같은 캔버스 크기여야 한다.
/// </summary>
static class CharacterArtApplier
{
    const string Folder = "Assets/Art/Characters";
    const string PrefabFolder = "Assets/Prefabs";
    const int CharacterSortingOrder = 5; // 주인공과 적이 같은 순서여야 위아래 위치로 앞뒤가 정해짐

    // 게임 속 키 (유닛). 1유닛 = 화면 100px
    const float PlayerHeight = 1.4f;
    const float ZombieHeight = 1.5f;
    const float GhostHeight = 1.3f;
    const float DoctorHeight = 1.5f; // 챕터2 적1(마스크 의사), 적2(안경 + 흰 가운)

    [MenuItem("Still Beating/캐릭터 아트 적용")]
    static void Apply()
    {
        if (!EditorUtility.DisplayDialog("Still Beating",
                "Assets/Art/Characters 의 그림을 주인공/적 프리팹에 적용하고 걷기 애니메이션을 붙입니다.\n(적 배정은 임시: 좀비 → 추적하는 적, 유령 → 랜덤 이동 적)\n\n계속할까요?",
                "적용", "취소"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // 주인공: 키 1.4유닛. PPU는 정면 서기 그림 기준으로 전체 통일, 기준점은 방향마다
        float playerPpu = PixelsPerUnitFor($"{Folder}/Player/Down_Idle.png", PlayerHeight);
        var down = ImportPair($"{Folder}/Player/Down_Idle.png", $"{Folder}/Player/Down_Walk.png", playerPpu);
        var up = ImportPair($"{Folder}/Player/Up_Idle.png", $"{Folder}/Player/Up_Walk.png", playerPpu);
        var side = ImportPair($"{Folder}/Player/Side_Idle.png", $"{Folder}/Player/Side_Walk.png", playerPpu);

        EditPrefab("Player", root =>
        {
            var sr = SetupSprite(root, down.idle);
            if (!root.TryGetComponent(out PlayerAnimator anim)) anim = root.AddComponent<PlayerAnimator>();
            var so = new SerializedObject(anim);
            so.FindProperty("spriteRenderer").objectReferenceValue = sr;
            so.FindProperty("downIdle").objectReferenceValue = down.idle;
            so.FindProperty("downWalk").objectReferenceValue = down.walk;
            so.FindProperty("upIdle").objectReferenceValue = up.idle;
            so.FindProperty("upWalk").objectReferenceValue = up.walk;
            so.FindProperty("sideIdle").objectReferenceValue = side.idle;
            so.FindProperty("sideWalk").objectReferenceValue = side.walk;
            so.FindProperty("sideFacesLeft").boolValue = true; // 옆 그림이 왼쪽을 봄
            so.ApplyModifiedPropertiesWithoutUndo();
        });

        // 좀비: 키 1.5유닛 (서기 + 걷기)
        var zombie = ImportPair($"{Folder}/Enemies/Zombie_Idle.png", $"{Folder}/Enemies/Zombie_Walk.png",
            PixelsPerUnitFor($"{Folder}/Enemies/Zombie_Idle.png", ZombieHeight));
        EditPrefab("Enemy_Chase", root => SetupEnemy(root, zombie.idle, zombie.walk));

        // 유령: 키 1.3유닛 (1장 - 좌우반전으로 번갈아 걷는 느낌)
        var ghost = ImportPair($"{Folder}/Enemies/Ghost.png", null, PixelsPerUnitFor($"{Folder}/Enemies/Ghost.png", GhostHeight));
        EditPrefab("Enemy_Wander", root => SetupEnemy(root, ghost.idle, null));

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Still Beating", "완료! 주인공과 적 프리팹에 그림과 걷기 애니메이션을 적용했습니다.\n챕터 씬에서 Play로 확인해 보세요.", "확인");
    }

    // ───────────────────────── 챕터2 전용 (씬에서만 덮어쓰기, 프리팹은 그대로)

    // 챕터2 적 배정 (임시): 추적하는 적 = 적1 마스크 의사, 랜덤 이동 적 = 적2 안경 + 흰 가운
    // 기획에서 반대로 정하면 아래 두 줄의 Surgeon / Doctor 를 바꾸고 메뉴를 다시 실행
    const string Chapter2ChaseLook = "Surgeon";
    const string Chapter2WanderLook = "Doctor";

    [MenuItem("Still Beating/챕터2 캐릭터 아트 적용 (다친 주인공·적1·적2)")]
    static void ApplyChapter2()
    {
        if (!EditorUtility.DisplayDialog("Still Beating",
                "챕터2 씬의 캐릭터 그림을 바꿉니다.\n\n" +
                "• 주인공 → 다친 주인공 (옆모습 좌우 따로)\n" +
                $"• 추적하는 적 → {LookName(Chapter2ChaseLook)}\n" +
                $"• 랜덤 이동 적 → {LookName(Chapter2WanderLook)}\n\n" +
                "다른 챕터와 프리팹은 그대로예요. 계속할까요?",
                "적용", "취소"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // 다친 주인공: 기본 주인공과 같은 키, 옆모습은 왼쪽/오른쪽 따로
        string injured = $"{Folder}/Player_Injured";
        float ppu = PixelsPerUnitFor($"{injured}/Down_Idle.png", PlayerHeight);
        var down = ImportPair($"{injured}/Down_Idle.png", $"{injured}/Down_Walk.png", ppu);
        var up = ImportPair($"{injured}/Up_Idle.png", $"{injured}/Up_Walk.png", ppu);
        var left = ImportPair($"{injured}/Side_Idle.png", $"{injured}/Side_Walk.png", ppu);
        var right = ImportPair($"{injured}/SideRight_Idle.png", $"{injured}/SideRight_Walk.png", ppu);

        var chaseLook = ImportEnemy(Chapter2ChaseLook);
        var wanderLook = ImportEnemy(Chapter2WanderLook);

        var scene = EditorSceneManager.OpenScene($"Assets/Scenes/{SceneFlow.Chapter2}.unity");

        var player = UnityEngine.Object.FindAnyObjectByType<PlayerAnimator>();
        if (player != null)
        {
            var so = new SerializedObject(player);
            so.FindProperty("downIdle").objectReferenceValue = down.idle;
            so.FindProperty("downWalk").objectReferenceValue = down.walk;
            so.FindProperty("upIdle").objectReferenceValue = up.idle;
            so.FindProperty("upWalk").objectReferenceValue = up.walk;
            so.FindProperty("sideIdle").objectReferenceValue = left.idle;
            so.FindProperty("sideWalk").objectReferenceValue = left.walk;
            so.FindProperty("sideFacesLeft").boolValue = true;
            so.FindProperty("sideRightIdle").objectReferenceValue = right.idle;
            so.FindProperty("sideRightWalk").objectReferenceValue = right.walk;
            so.ApplyModifiedPropertiesWithoutUndo();
            SetSceneSprite(so.FindProperty("spriteRenderer").objectReferenceValue as SpriteRenderer, down.idle);
        }
        else Debug.LogWarning("[챕터2 캐릭터] 주인공(PlayerAnimator)을 찾을 수 없습니다");

        int enemies = 0;
        foreach (var enemy in UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            if (!enemy.TryGetComponent(out EnemyAnimator anim)) continue;
            var (idle, walk) = enemy.behaviour == EnemyBehaviour.Chase ? chaseLook : wanderLook;
            var so = new SerializedObject(anim);
            so.FindProperty("idle").objectReferenceValue = idle;
            so.FindProperty("walk").objectReferenceValue = walk;
            so.FindProperty("facesLeft").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            SetSceneSprite(so.FindProperty("spriteRenderer").objectReferenceValue as SpriteRenderer, idle);
            enemies++;
        }

        EditorSceneManager.SaveScene(scene);
        EditorUtility.DisplayDialog("Still Beating", $"완료! 챕터2 주인공과 적 {enemies}마리의 그림을 바꿨습니다.\nChapter2 씬에서 Play로 확인해 보세요.", "확인");
    }

    static (Sprite idle, Sprite walk) ImportEnemy(string look)
    {
        string idle = $"{Folder}/Enemies/{look}_Idle.png";
        return ImportPair(idle, $"{Folder}/Enemies/{look}_Walk.png", PixelsPerUnitFor(idle, DoctorHeight));
    }

    static string LookName(string look) => look switch
    {
        "Surgeon" => "적1 마스크 의사",
        "Doctor" => "적2 안경 + 흰 가운",
        _ => look,
    };

    // 씬에 놓인 프리팹의 그림만 이 씬에서 바꾼다 (프리팹 원본은 그대로)
    static void SetSceneSprite(SpriteRenderer sr, Sprite sprite)
    {
        if (sr == null) return;
        var so = new SerializedObject(sr);
        so.FindProperty("m_Sprite").objectReferenceValue = sprite;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetupEnemy(GameObject root, Sprite idle, Sprite walk)
    {
        var sr = SetupSprite(root, idle);
        if (!root.TryGetComponent(out EnemyAnimator anim)) anim = root.AddComponent<EnemyAnimator>();
        var so = new SerializedObject(anim);
        so.FindProperty("spriteRenderer").objectReferenceValue = sr;
        so.FindProperty("idle").objectReferenceValue = idle;
        so.FindProperty("walk").objectReferenceValue = walk;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // 프리팹의 Sprite 자식: 그림 넣고, 임시 도형용 크기·색 되돌리고, 발 기준으로 앞뒤 정렬
    static SpriteRenderer SetupSprite(GameObject root, Sprite sprite)
    {
        var sr = root.transform.Find("Sprite").GetComponent<SpriteRenderer>();
        sr.transform.localPosition = Vector3.zero;
        sr.transform.localScale = Vector3.one;
        sr.sprite = sprite;
        sr.color = Color.white;
        sr.sortingOrder = CharacterSortingOrder;
        sr.spriteSortPoint = SpriteSortPoint.Pivot;
        return sr;
    }

    static void EditPrefab(string name, Action<GameObject> edit)
    {
        string path = $"{PrefabFolder}/{name}.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            edit(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // ───────────────────────── 그림 설정

    // 그림 속 캐릭터 키(픽셀)를 targetHeight 유닛으로 맞추는 PPU
    static float PixelsPerUnitFor(string path, float targetHeight)
    {
        var bounds = Bounds(path, out _, out _);
        return (bounds.maxY - bounds.minY + 1) / targetHeight;
    }

    // 서기 그림의 발 위치를 기준점으로 계산해서, 서기/걷기 그림에 같이 적용
    static (Sprite idle, Sprite walk) ImportPair(string idlePath, string walkPath, float ppu)
    {
        var b = Bounds(idlePath, out int w, out int h);
        var pivot = new Vector2((b.minX + b.maxX + 1) / 2f / w, (float)b.minY / h); // 가로 가운데, 발 끝
        var idle = Import(idlePath, ppu, pivot);
        var walk = walkPath != null && File.Exists(walkPath) ? Import(walkPath, ppu, pivot) : null;
        return (idle, walk);
    }

    static (int minX, int minY, int maxX, int maxY) Bounds(string path, out int w, out int h)
    {
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.LoadImage(File.ReadAllBytes(path));
        w = texture.width;
        h = texture.height;
        var bounds = Chapter1ArtApplier.LargestBlobBounds(texture.GetPixels32(), w, h);
        UnityEngine.Object.DestroyImmediate(texture);
        if (bounds.maxX < 0) throw new Exception($"[캐릭터 아트] {path} 에 그림이 없습니다");
        return bounds;
    }

    static Sprite Import(string path, float ppu, Vector2 pivot)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = ppu;
        importer.mipmapEnabled = true; // 작게 보여서 축소 품질이 중요
        importer.filterMode = FilterMode.Trilinear;
        importer.alphaIsTransparency = true;
        importer.maxTextureSize = 1024;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = pivot;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
