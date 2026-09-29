using UnityEngine;

/// <summary>
/// 임시 IMGUI 화면들이 같이 쓰는 도우미.
/// </summary>
public static class UIUtil
{
    public const float RefHeight = 1080f;

    /// <summary>1080p 기준 좌표로 그리도록 GUI.matrix를 설정하고, 기준 좌표계의 화면 너비를 돌려준다.</summary>
    public static float BeginScaled()
    {
        float scale = Screen.height / RefHeight;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        return Screen.width / scale;
    }

    public static void FillRect(Rect rect, Color color)
    {
        var prev = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = prev;
    }
}
