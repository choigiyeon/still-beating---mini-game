using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 메뉴 [Still Beating > 밸런스 수정 적용 (음식·속도)]
/// 11/12 밸런스 점검 결과를 챕터1~3 씬의 GameManager > Balance 에 한 번에 넣는다.
///  - 배고픔 간격: 음식이 필요하도록 (챕터1 1개 / 챕터2 3개 / 챕터3 4개 필요)
///  - 적 안쪽 범위 속도 배율 0.7: 주인공(4 × 0.7 = 2.8)이 랜덤 적보다 빠르게
///  - 챕터3 랜덤 적 속도 2.6: 안쪽 범위에서도 도망칠 수 있게
/// 적용 전/후 값은 Console에 찍힌다. 이후 조절은 Inspector에서 직접.
/// </summary>
static class BalancePatch
{
    [MenuItem("Still Beating/밸런스 수정 적용 (음식·속도)")]
    static void Apply()
    {
        if (!EditorUtility.DisplayDialog("Still Beating",
                "챕터1~3 밸런스를 수정합니다.\n\n" +
                "• 배고픔 간격: 챕터1 15→12, 챕터2 10→9 (챕터3 9 유지)\n" +
                "• 적 안쪽 범위 속도 배율: 0.6→0.7 (전 챕터)\n" +
                "• 챕터3 랜덤 적 속도: 2.8→2.6\n\n계속할까요?",
                "적용", "취소"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string opened = SceneManager.GetActiveScene().path;

        Patch(SceneFlow.Chapter1, ("hungerDecayInterval", 12f), ("innerRangeSpeedMultiplier", 0.7f));
        Patch(SceneFlow.Chapter2, ("hungerDecayInterval", 9f), ("innerRangeSpeedMultiplier", 0.7f));
        Patch(SceneFlow.Chapter3, ("innerRangeSpeedMultiplier", 0.7f), ("wanderSpeed", 2.6f));

        if (!string.IsNullOrEmpty(opened)) EditorSceneManager.OpenScene(opened);
        EditorUtility.DisplayDialog("Still Beating", "완료! 바뀐 값은 Console에서 확인할 수 있어요.", "확인");
    }

    static void Patch(string sceneName, params (string field, float value)[] changes)
    {
        var scene = EditorSceneManager.OpenScene($"Assets/Scenes/{sceneName}.unity");
        var gm = Object.FindAnyObjectByType<GameManager>();
        if (gm == null)
        {
            Debug.LogWarning($"[밸런스] {sceneName} 에 GameManager가 없습니다");
            return;
        }

        var so = new SerializedObject(gm);
        foreach (var (field, value) in changes)
        {
            var p = so.FindProperty("balance." + field);
            if (p == null) { Debug.LogWarning($"[밸런스] {field} 를 찾을 수 없습니다"); continue; }
            Debug.Log($"[밸런스] {sceneName}  {field}: {p.floatValue} → {value}");
            p.floatValue = value;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.SaveScene(scene);
    }
}
