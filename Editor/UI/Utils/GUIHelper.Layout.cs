namespace Aoyon.MaterialEditor.UI;

internal static partial class GUIHelper
{
    public static readonly float GUI_SPACE = EditorGUIUtility.standardVerticalSpacing;
    public static readonly float propertyHeight = EditorGUIUtility.singleLineHeight;
    private const float INDENT_WIDTH = 15f;

    // 高さを1行ぶんにセット
    internal static Rect SetSingleHeight(this ref Rect position)
    {
        position.height = propertyHeight;
        return position;
    }

    internal static Rect SetHeight(this ref Rect position, SerializedProperty property)
    {
        position.height = EditorGUI.GetPropertyHeight(property);
        return position;
    }

    // 改行
    internal static Rect NewLine(this ref Rect position)
    {
        position.y = position.yMax + GUI_SPACE;
        return position;
    }

    internal static Rect NewLineWithSingleHeight(this ref Rect position)
    {
        position.y = position.yMax + GUI_SPACE;
        position.height = propertyHeight;
        return position;
    }

    internal static Rect Indent(this ref Rect position, int count = 1)
    {
        position.x += INDENT_WIDTH * count;
        position.width -= INDENT_WIDTH * count;
        return position;
    }
    
    internal static Rect Back(this ref Rect position, int count = 1)
    {
        position.x -= INDENT_WIDTH * count;
        position.width += INDENT_WIDTH * count;
        return position;
    }

    internal static void SplitRectHorizontally(in Rect source, float leftRatio, out Rect left, out Rect right)
    {
        left = source;
        right = source;

        left.width = Mathf.Round(source.width * leftRatio);
        right.x = left.xMax + EditorGUIUtility.standardVerticalSpacing;
        right.width = Mathf.Max(0f, source.xMax - right.x);
    }

    internal static void SplitRectHorizontally(in Rect source, float leftRatio, float leftMinWidth, out Rect left, out Rect right)
    {
        left = source;
        right = source;

        left.width = Mathf.Max(leftMinWidth, Mathf.Round(source.width * leftRatio));
        right.x = left.xMax + EditorGUIUtility.standardVerticalSpacing;
        right.width = Mathf.Max(0f, source.xMax - right.x);
    }

    internal static void SplitRectHorizontallyForLeft(in Rect source, float leftWidth, out Rect left, out Rect right)
    {
        left = source;
        right = source;

        left.width = leftWidth;
        right.x = left.xMax + EditorGUIUtility.standardVerticalSpacing;
        right.width = Mathf.Max(0f, source.xMax - right.x);
    }

    internal static void SplitRectHorizontallyForRight(in Rect source, float rightWidth, out Rect left, out Rect right)
    {
        left = source;
        right = source;

        right.width = rightWidth;
        right.x = source.xMax - rightWidth;
        left.width = Mathf.Max(0f, right.x - source.x - EditorGUIUtility.standardVerticalSpacing);
    }
}
