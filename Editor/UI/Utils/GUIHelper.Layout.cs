namespace Aoyon.MaterialEditor.UI;

internal static partial class GUIHelper
{
    // 高さを1行ぶんにセット
    internal static Rect SingleLine(this ref Rect position)
    {
        position.height = propertyHeight;
        return position;
    }

    // 改行
    internal static Rect NewLine(this ref Rect position)
    {
        position.y = position.yMax + GUI_SPACE;
        return position;
    }
}
