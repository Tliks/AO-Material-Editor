namespace Aoyon.MaterialEditor.UI;

internal static class StyleHelper
{
    private static GUIStyle? _centeredPopupStyle;
    public static GUIStyle CenteredPopupStyle => _centeredPopupStyle ??= new GUIStyle(EditorStyles.popup)
    {
        alignment = TextAnchor.MiddleCenter
    };
}