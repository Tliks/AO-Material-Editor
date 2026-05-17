namespace Aoyon.MaterialEditor.UI;

internal static class StyleHelper
{
    private static GUIStyle? _middleCenteredPopupStyle;
    public static GUIStyle MiddleCenteredPopupStyle => _middleCenteredPopupStyle ??= new GUIStyle(EditorStyles.popup)
    {
        alignment = TextAnchor.MiddleCenter
    };

    private static GUIStyle? _lowerCenteredPopupStyle;
    public static GUIStyle LowerCenteredPopupStyle => _lowerCenteredPopupStyle ??= new GUIStyle(EditorStyles.popup)
    {
        alignment = TextAnchor.LowerCenter
    };

    private static GUIStyle? _middleCenteredToolbarStyle;
    public static GUIStyle MiddleCenteredToolbarStyle => _middleCenteredToolbarStyle ??= new GUIStyle(GUI.skin.button)
    {
        alignment = TextAnchor.MiddleCenter
    };

    private static GUIStyle? _lowerCenteredToolbarStyle;
    public static GUIStyle LowerCenteredToolbarStyle => _lowerCenteredToolbarStyle ??= new GUIStyle(GUI.skin.button)
    {
        alignment = TextAnchor.LowerCenter
    };

    private static GUIStyle? _dropStyle;
    public static GUIStyle DropStyle => _dropStyle ??= new GUIStyle(EditorStyles.helpBox)
    {
        alignment = TextAnchor.MiddleCenter
    };
}
