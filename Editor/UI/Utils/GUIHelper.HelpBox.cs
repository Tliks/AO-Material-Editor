namespace Aoyon.MaterialEditor.UI;

internal static partial class GUIHelper
{
    private const float HELPBOX_PROPERTY_HEIGHT_WIDTH_OFFSET = 22f;

    internal static Rect HelpBox(Rect position, string key, MessageType messageType)
    {
        var content = CreateHelpBoxContent(key, messageType);
        position.height = EditorStyles.helpBox.CalcHeight(content, position.width);
        GUI.Label(position, content, EditorStyles.helpBox);
        position.NewLineWithSingleHeight();
        return position;
    }

    internal static float GetHelpBoxHeight(string key, MessageType messageType)
    {
        var width = Mathf.Max(0f, EditorGUIUtility.currentViewWidth - HELPBOX_PROPERTY_HEIGHT_WIDTH_OFFSET);
        return GetHelpBoxHeight(key, width, messageType);
    }

    internal static float GetHelpBoxHeight(string key, float width, MessageType messageType)
    {
        return EditorStyles.helpBox.CalcHeight(CreateHelpBoxContent(key, messageType), width);
    }

    private static GUIContent CreateHelpBoxContent(string key, MessageType messageType)
    {
        var icon = messageType switch
        {
            MessageType.None => null,
            MessageType.Info => EditorGUIUtility.IconContent("console.infoicon").image,
            MessageType.Warning => EditorGUIUtility.IconContent("console.warnicon").image,
            MessageType.Error => EditorGUIUtility.IconContent("console.erroricon").image,
            _ => null,
        };

        var content = key.LG();
        return icon == null ? content : new GUIContent(content.text, icon, content.tooltip);
    }
}
