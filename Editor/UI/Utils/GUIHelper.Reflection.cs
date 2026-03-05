using System.Reflection;

namespace Aoyon.MaterialEditor.UI;

internal static partial class GUIHelper
{
    private static readonly PropertyInfo? LeftMarginProp =
        typeof(EditorGUIUtility).GetProperty(
            "leftMarginCoord",
            BindingFlags.NonPublic | BindingFlags.Static
        );

    private static readonly PropertyInfo? TopmostRectProp =
        typeof(GUI).Assembly
            .GetType("UnityEngine.GUIClip")
            ?.GetProperty(
                "topmostRect",
                BindingFlags.NonPublic | BindingFlags.Static
            );

    public static bool TryGetMarginX(out float marginX)
    {
        marginX = 0f;

        if (LeftMarginProp == null || TopmostRectProp == null)
            return false;

        if (LeftMarginProp.GetValue(null) is not float leftMargin)
            return false;

        if (TopmostRectProp.GetValue(null) is not Rect topmostRect)
            return false;

        marginX = leftMargin - Mathf.Max(0f, topmostRect.xMin);
        return true;
    }
}