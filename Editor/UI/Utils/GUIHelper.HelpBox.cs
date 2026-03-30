namespace Aoyon.MaterialEditor.UI;

internal static partial class GUIHelper
{
    private const float HELPBOX_PROPERTY_HEIGHT_WIDTH_OFFSET = 22f;

    internal static Rect HelpBox(Rect position, string text, MessageType messageType)
    {
        var content = CreateHelpBoxContent(text, messageType);
        position.height = EditorStyles.helpBox.CalcHeight(content, position.width);
        GUI.Label(position, content, EditorStyles.helpBox);
        position.NewLine();
        position.SetSingleHeight();
        return position;
    }

    internal static float GetHelpBoxHeight(string text, MessageType messageType)
    {
        var width = Mathf.Max(0f, EditorGUIUtility.currentViewWidth - HELPBOX_PROPERTY_HEIGHT_WIDTH_OFFSET);
        return GetHelpBoxHeight(text, width, messageType);
    }

    internal static float GetHelpBoxHeight(string text, float width, MessageType messageType)
    {
        return EditorStyles.helpBox.CalcHeight(CreateHelpBoxContent(text, messageType), width);
    }

    private static GUIContent CreateHelpBoxContent(string text, MessageType messageType)
    {
        var icon = messageType switch
        {
            MessageType.None => null,
            MessageType.Info => EditorGUIUtility.IconContent("console.infoicon").image,
            MessageType.Warning => EditorGUIUtility.IconContent("console.warnicon").image,
            MessageType.Error => EditorGUIUtility.IconContent("console.erroricon").image,
            _ => null,
        };

        return icon == null ? new GUIContent(text) : new GUIContent(text, icon, text);
    }
}
