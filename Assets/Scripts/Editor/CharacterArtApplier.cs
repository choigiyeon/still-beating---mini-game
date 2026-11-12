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

    [MenuItem("Still Beating/캐릭터 아트 적용")]
    static void Apply()
    {
        if (!EditorUtility.DisplayDialog("Still Beating",
                "Assets/Art/Characters 의 그림을 주인공/적 프리팹에 적용하고 걷기 애니메이션을 붙입니다.\n(적 배정은 임시: 좀비 → 추적하는 적, 유령 → 랜덤 이동 적)\n\n계속할까요?",
                "적용", "취소"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // 주인공: 키 1.4유닛. PPU는 정면 서기 그림 기준으로 전체 통일, 기준점은 방향마다
        float playerPpu = PixelsPerUnitFor($"{Folder}/Player/Down_Idle.png", 1.4f);
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
            PixelsPerUnitFor($"{Folder}/Enemies/Zombie_Idle.png", 1.5f));
        EditPrefab("Enemy_Chase", root => SetupEnemy(root, zombie.idle, zombie.walk));

        // 유령: 키 1.3유닛 (1장 - 좌우반전으로 번갈아 걷는 느낌)
        var ghost = ImportPair($"{Folder}/Enemies/Ghost.png", null, PixelsPerUnitFor($"{Folder}/Enemies/Ghost.png", 1.3f));
        EditPrefab("Enemy_Wander", root => SetupEnemy(root, ghost.idle, null));

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Still Beating", "완료! 주인공과 적 프리팹에 그림과 걷기 애니메이션을 적용했습니다.\n챕터 씬에서 Play로 확인해 보세요.", "확인");
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
