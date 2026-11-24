using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 메뉴 [Still Beating > 게임오버·엔딩 창·이어하기 설정]
///  - Title: 이어하기 버튼을 TitleScreen에 연결 (저장이 없으면 흐리게)
///  - Chapter1~3 종료 화면
///    · 게임오버 창: GAME OVER 글자 + [다시하기] (지금 챕터 처음부터) + [메인화면으로]
///    · 엔딩 창 (챕터3 클리어): CLEAR 글자 + [메인화면으로] (다시하기는 GameHUD가 숨김)
/// 여러 번 실행해도 이미 된 건 다시 만들지 않는다.
/// (닭다리 회복 범위는 BalancePatch의 "닭다리 회복 범위 적용 (챕터별)" 메뉴에서)
/// </summary>
static class EndAndContinueSetup
{
    [MenuItem("Still Beating/게임오버·엔딩 창·이어하기 설정")]
    static void Apply()
    {
        if (!EditorUtility.DisplayDialog("Still Beating",
                "• 타이틀: 이어하기 버튼 연결 (저장이 없으면 흐리게)\n" +
                "• 챕터1~3 게임오버 창: [다시하기] [메인화면으로] 버튼\n" +
                "• 챕터3 엔딩 창: [메인화면으로] 버튼\n\n계속할까요?",
                "적용", "취소"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string opened = SceneManager.GetActiveScene().path;
        var report = new System.Text.StringBuilder();

        report.AppendLine(SetupTitle());
        foreach (var chapter in new[] { SceneFlow.Chapter1, SceneFlow.Chapter2, SceneFlow.Chapter3 })
            report.AppendLine(SetupChapter(chapter));

        if (!string.IsNullOrEmpty(opened)) EditorSceneManager.OpenScene(opened);
        Debug.Log("[게임오버·엔딩 창·이어하기]\n" + report);
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

        var hudSo = new SerializedObject(hud);
        var endText = hudSo.FindProperty("endText").objectReferenceValue as Text;
        if (endText == null) return $"{chapter}: 종료 화면 글자(endText)가 연결돼 있지 않음";
        var screen = endText.transform.parent;

        // 전에 만든 안내 글자가 있으면 정리 (기획에 없어서 뺌)
        var oldHint = screen.Find("Hint");
        if (oldHint != null) Object.DestroyImmediate(oldHint.gameObject);

        // GAME OVER / CLEAR 글자는 이제 제목만 (전에 글자에 붙였던 버튼은 뺌), 버튼들 위쪽에
        if (endText.TryGetComponent(out Button oldButton)) Object.DestroyImmediate(oldButton);
        endText.raycastTarget = false;
        var rt = endText.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, 140f);
        rt.sizeDelta = new Vector2(900f, 180f);

        // 버튼 묶음: 세로로 쌓고, 숨긴 버튼(엔딩 창의 다시하기)은 자리도 비우지 않게
        var group = screen.Find("Buttons") as RectTransform;
        if (group == null)
        {
            group = new GameObject("Buttons", typeof(RectTransform), typeof(VerticalLayoutGroup)).GetComponent<RectTransform>();
            group.SetParent(screen, false);
        }
        group.anchorMin = group.anchorMax = new Vector2(0.5f, 0.5f);
        group.pivot = new Vector2(0.5f, 1f);
        group.anchoredPosition = new Vector2(0f, 0f);
        group.sizeDelta = new Vector2(600f, 240f);
        var layout = group.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 20f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = layout.childControlHeight = false;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;

        var retry = MakeButton(group, "RetryButton", "다시하기", endText.font, hud.Retry);
        MakeButton(group, "TitleButton", "메인화면으로", endText.font, hud.ReturnToTitle);
        retry.transform.SetSiblingIndex(0);

        hudSo.FindProperty("retryButton").objectReferenceValue = retry.gameObject;
        hudSo.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return $"{chapter}: 다시하기·메인화면으로 버튼";
    }

    // 타이틀 화면 버튼과 같은 모양 (반투명 검은 상자 + 흰 글자 40)
    static Button MakeButton(Transform parent, string name, string label, Font font, UnityAction onClick)
    {
        var t = parent.Find(name);
        GameObject go;
        if (t != null) go = t.gameObject;
        else
        {
            go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            new GameObject("Text", typeof(RectTransform), typeof(Text)).transform.SetParent(go.transform, false);
        }

        ((RectTransform)go.transform).sizeDelta = new Vector2(600f, 110f);
        var image = go.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.55f);

        var text = go.GetComponentInChildren<Text>(true);
        var textRt = text.rectTransform;
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = textRt.offsetMax = Vector2.zero;
        text.text = label;
        text.font = font;
        text.fontSize = 40;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;

        var button = go.GetComponent<Button>();
        button.targetGraphic = image;
        var nav = button.navigation;
        nav.mode = Navigation.Mode.None; // 방향키로 버튼이 선택되지 않게
        button.navigation = nav;
        if (!HasListener(button, onClick.Method.Name))
            UnityEventTools.AddPersistentListener(button.onClick, onClick);
        return button;
    }

    static bool HasListener(Button button, string method)
    {
        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            if (button.onClick.GetPersistentMethodName(i) == method) return true;
        return false;
    }
}
