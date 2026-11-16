using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 메뉴 [Still Beating > 챕터1 UI를 챕터2·3에 복사]
/// 챕터1 화면 UI(HUD 캔버스: 심박수, 심전도, 닭다리, 주머니, 탈출 타이머)의 모양을 챕터2·3에 그대로 옮긴다.
///  - 같은 경로(이름)의 UI끼리 위치·크기·그림·색·글자 설정을 복사
///  - UI 안끼리의 연결(음식 버튼 → 먹기, 닭다리 칸 목록 등)은 복사본끼리 다시 연결
///  - UI 밖과의 연결(그 챕터의 주인공 등)은 원래 챕터 것을 그대로 유지
/// 챕터1 UI를 고친 뒤 다시 실행하면 챕터2·3도 같이 맞춰진다.
/// </summary>
static class HudCopier
{
    static readonly string[] Targets = { SceneFlow.Chapter2, SceneFlow.Chapter3 };

    [MenuItem("Still Beating/챕터1 UI를 챕터2·3에 복사")]
    static void CopyMenu()
    {
        if (!EditorUtility.DisplayDialog("Still Beating",
                "챕터1 화면 UI(심박수, 심전도, 닭다리, 주머니, 탈출 타이머)의 모양을\n챕터2·3에 그대로 복사합니다.\n\n각 챕터의 주인공 연결 등은 그대로 유지돼요. 계속할까요?",
                "복사", "취소"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string opened = SceneManager.GetActiveScene().path;
        var report = new StringBuilder();
        foreach (var target in Targets) report.AppendLine(CopyInto(target));
        if (!string.IsNullOrEmpty(opened)) EditorSceneManager.OpenScene(opened);

        Debug.Log("[UI 복사]\n" + report);
        EditorUtility.DisplayDialog("Still Beating", "완료!\n\n" + report, "확인");
    }

    static string CopyInto(string targetName)
    {
        var dstScene = EditorSceneManager.OpenScene($"Assets/Scenes/{targetName}.unity", OpenSceneMode.Single);
        var srcScene = EditorSceneManager.OpenScene($"Assets/Scenes/{SceneFlow.Chapter1}.unity", OpenSceneMode.Additive);
        try
        {
            var srcRoot = FindHud(srcScene);
            var dstRoot = FindHud(dstScene);
            if (srcRoot == null || dstRoot == null) return $"{targetName}: HUD를 찾을 수 없어 건너뜀";

            var srcMap = MapByPath(srcRoot);
            var dstMap = MapByPath(dstRoot);

            // 1) 챕터1 UI 오브젝트/컴포넌트 → 대상 챕터의 짝 (없는 컴포넌트는 추가)
            var pair = new Dictionary<Object, Object>();
            var missing = new List<string>();
            foreach (var (path, s) in srcMap)
            {
                if (!dstMap.TryGetValue(path, out var d)) { missing.Add(path); continue; }
                pair[s.gameObject] = d.gameObject;
                pair[s] = d;
                var srcComponents = s.GetComponents<Component>();
                foreach (var sc in srcComponents)
                {
                    if (sc == null || sc is Transform) continue;
                    var dc = Match(d, sc, srcComponents);
                    if (dc == null)
                    {
                        dc = d.gameObject.AddComponent(sc.GetType());
                        if (dc == null) { missing.Add($"{path} ({sc.GetType().Name} 추가 실패)"); continue; }
                    }
                    pair[sc] = dc;
                }
            }

            // 2) 값 복사
            int copied = 0;
            foreach (var (path, s) in srcMap)
            {
                if (!dstMap.TryGetValue(path, out var d)) continue;
                if (d.gameObject.activeSelf != s.gameObject.activeSelf) d.gameObject.SetActive(s.gameObject.activeSelf);
                CopyRect(s, d);
                foreach (var sc in s.GetComponents<Component>())
                {
                    if (sc == null || sc is Transform || sc is CanvasRenderer) continue;
                    if (!pair.TryGetValue(sc, out var dc)) continue;
                    CopyComponent(sc, (Component)dc, pair);
                    copied++;
                }
            }

            EditorSceneManager.MarkSceneDirty(dstScene);
            EditorSceneManager.SaveScene(dstScene);

            string result = $"{targetName}: UI 설정 {copied}개 복사";
            if (missing.Count > 0) result += $"\n  ⚠ 짝이 없어 건너뜀 {missing.Count}개: {string.Join(", ", missing)}";
            return result;
        }
        finally
        {
            EditorSceneManager.CloseScene(srcScene, true);
        }
    }

    static Transform FindHud(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            var hud = root.GetComponentInChildren<GameHUD>(true);
            if (hud != null) return hud.transform;
        }
        return null;
    }

    // HUD 아래 모든 오브젝트를 "부모/자식" 경로로 (같은 이름 형제는 #번호)
    static Dictionary<string, Transform> MapByPath(Transform root)
    {
        var map = new Dictionary<string, Transform>();
        void Walk(Transform t, string path)
        {
            map[path] = t;
            var seen = new Dictionary<string, int>();
            foreach (Transform child in t)
            {
                seen.TryGetValue(child.name, out int n);
                seen[child.name] = n + 1;
                string name = n == 0 ? child.name : $"{child.name}#{n}";
                Walk(child, path.Length == 0 ? name : $"{path}/{name}");
            }
        }
        Walk(root, "");
        return map;
    }

    // 같은 종류 컴포넌트가 여러 개면 순서대로 짝짓기
    static Component Match(Transform d, Component sc, Component[] srcComponents)
    {
        var type = sc.GetType();
        int index = 0;
        foreach (var c in srcComponents)
        {
            if (c == sc) break;
            if (c != null && c.GetType() == type) index++;
        }
        var candidates = d.GetComponents(type);
        return index < candidates.Length ? candidates[index] : null;
    }

    static void CopyRect(Transform s, Transform d)
    {
        if (s is RectTransform sr && d is RectTransform dr)
        {
            dr.anchorMin = sr.anchorMin;
            dr.anchorMax = sr.anchorMax;
            dr.pivot = sr.pivot;
            dr.anchoredPosition = sr.anchoredPosition;
            dr.sizeDelta = sr.sizeDelta;
        }
        d.localRotation = s.localRotation;
        d.localScale = s.localScale;
    }

    static void CopyComponent(Component src, Component dst, Dictionary<Object, Object> pair)
    {
        // 대상 챕터의 원래 연결을 기억 (UI 밖 연결은 복원하려고)
        var dstSo = new SerializedObject(dst);
        var original = new Dictionary<string, Object>();
        var it = dstSo.GetIterator();
        while (it.Next(true))
            if (it.propertyType == SerializedPropertyType.ObjectReference) original[it.propertyPath] = it.objectReferenceValue;

        EditorUtility.CopySerialized(src, dst);

        dstSo.Update();
        it = dstSo.GetIterator();
        while (it.Next(true))
        {
            if (it.propertyType != SerializedPropertyType.ObjectReference) continue;
            var value = it.objectReferenceValue;
            if (value == null || EditorUtility.IsPersistent(value)) continue; // 그림·폰트 같은 에셋은 그대로

            if (pair.TryGetValue(value, out var mapped)) it.objectReferenceValue = mapped;      // UI 안 → 복사본
            else it.objectReferenceValue = original.TryGetValue(it.propertyPath, out var o) ? o : null; // UI 밖 → 원래 것
        }
        dstSo.ApplyModifiedPropertiesWithoutUndo();
    }
}
