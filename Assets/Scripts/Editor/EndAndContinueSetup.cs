using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 메뉴 [Still Beating > 게임오버·이어하기 설정]
///  - Title: 이어하기 버튼을 TitleScreen에 연결 (저장이 없으면 흐리게)
///  - Chapter1~3: 종료 화면의 GAME OVER(CLEAR) 글자를 버튼으로 만들어, 누르면 처음 화면으로 (GameHUD.ReturnToTitle)
/// 여러 번 실행해도 이미 된 건 다시 만들지 않는다.
/// </summary>
static class EndAndContinueSetup
{
    [MenuItem("Still Beating/게임오버·이어하기 설정")]
    static void Apply()
    {
        if (!EditorUtility.DisplayDialog("Still Beating",
                "• 타이틀: 이어하기 버튼 연결 (저장이 없으면 흐리게)\n" +
                "• 챕터1~3: GAME OVER 글자를 누르면 처음 화면으로\n\n계속할까요?",
                "적용", "취소"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string opened = SceneManager.GetActiveScene().path;
        var report = new System.Text.StringBuilder();

        report.AppendLine(SetupTitle());
        foreach (var chapter in new[] { SceneFlow.Chapter1, SceneFlow.Chapter2, SceneFlow.Chapter3 })
            report.AppendLine(SetupChapter(chapter));

        if (!string.IsNullOrEmpty(opened)) EditorSceneManager.OpenScene(opened);
        Debug.Log("[게임오버·이어하기]\n" + report);
        EditorUtility.DisplayDialog("Still Beating", "완료!\n\n" + report, "확인");
    }

    static string SetupTitle()
    {
        var scene = EditorSceneManager.OpenScene($"Assets/Scenes/{SceneFlow.Title}.unity");
        var title = Object.FindAnyObjectByType<TitleScreen>();
        if (title == null) return "Title: TitleScreen을 찾을 수 없음";

        // OnClick에 Continue가 연결된 버튼 = 이어하기 버튼
        Button continueButton = null;
        foreach (var button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (HasListener(button, nameof(TitleScreen.Continue))) continueButton = button;
        if (continueButton == null) return "Title: 이어하기 버튼(OnClick = Continue)을 찾을 수 없음";

        var so = new SerializedObject(title);
        so.FindProperty("continueButton").objectReferenceValue = continueButton;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.SaveScene(scene);
        return $"Title: 이어하기 버튼 연결 ({continueButton.name})";
    }

    static string SetupChapter(string chapter)
    {
        var scene = EditorSceneManager.OpenScene($"Assets/Scenes/{chapter}.unity");
        var hud = Object.FindAnyObjectByType<GameHUD>();
        if (hud == null) return $"{chapter}: GameHUD를 찾을 수 없음";

        var endText = new SerializedObject(hud).FindProperty("endText").objectReferenceValue as Text;
        if (endText == null) return $"{chapter}: 종료 화면 글자(endText)가 연결돼 있지 않음";

        // 전에 만든 안내 글자가 있으면 정리 (기획에 없어서 뺌)
        var oldHint = endText.transform.parent != null ? endText.transform.parent.Find("Hint") : null;
        if (oldHint != null) Object.DestroyImmediate(oldHint.gameObject);

        // 누르는 범위 = 글자 근처 (화면 전체가 아니라)
        var rt = endText.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(900f, 180f);
        endText.raycastTarget = true;

        if (!endText.TryGetComponent(out Button button)) button = endText.gameObject.AddComponent<Button>();
        button.targetGraphic = endText;
        var colors = button.colors;
        colors.normalColor = new Color(0.85f, 0.85f, 0.85f);      // 평소 살짝 어둡게
        colors.highlightedColor = Color.white;                     // 마우스를 올리면 밝게
        colors.pressedColor = new Color(0.65f, 0.65f, 0.65f);
        colors.selectedColor = colors.normalColor;
        button.colors = colors;
        var nav = button.navigation;
        nav.mode = Navigation.Mode.None; // 방향키로 버튼이 선택되지 않게
        button.navigation = nav;

        if (!HasListener(button, nameof(GameHUD.ReturnToTitle)))
            UnityEventTools.AddPersistentListener(button.onClick, hud.ReturnToTitle);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return $"{chapter}: GAME OVER 글자 → 처음 화면 버튼";
    }

    static bool HasListener(Button button, string method)
    {
        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            if (button.onClick.GetPersistentMethodName(i) == method) return true;
        return false;
    }
}
