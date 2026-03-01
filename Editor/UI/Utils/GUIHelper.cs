using System.Reflection;

namespace Aoyon.MaterialEditor.UI;

internal static partial class GUIHelper
{
    public static void DrawLine(Rect rectAbove, float lineHeight = 1f, float lineSpacing = 2f, Color? color = null)
    {
        var c = color ?? Color.white;
        var lineRect = new Rect(rectAbove.x, rectAbove.yMax + lineSpacing, rectAbove.width, lineHeight);
        EditorGUI.DrawRect(lineRect, c);
        GUILayout.Space(lineHeight + lineSpacing);
    }

    private static Action<Rect, Color>? drawMarginLineForRectDelegate = null;
    private static Action<Rect, Color>? DrawMarginLineForRectDelegate => drawMarginLineForRectDelegate ??= CreateDrawMarginLineForRectDelegate();

    private static Action<Rect, Color>? CreateDrawMarginLineForRectDelegate()
    {
        var method = typeof(EditorGUI).GetMethod(
            "DrawMarginLineForRect",
            BindingFlags.NonPublic | BindingFlags.Static,
            null,
            new[] { typeof(Rect), typeof(Color) },
            null);
        if (method == null) {
            Debug.LogWarning("DrawMarginLineForRect method not found");
            return null;
        }
        return (Action<Rect, Color>)Delegate.CreateDelegate(typeof(Action<Rect, Color>), method);
    }

    public static void DrawMarginLineForRect(Rect position, Color color)
    {
        if (DrawMarginLineForRectDelegate == null) {
            EditorGUI.DrawRect(position, color); // fallback
        }
        else {
            DrawMarginLineForRectDelegate(position, color);
        }
    }
}