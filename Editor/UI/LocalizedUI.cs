namespace Aoyon.MaterialEditor.UI;

internal static class LocalizedUI
{

    public static void PropertyField(SerializedProperty property, string key, bool includeChildren = true)
    {
        EditorGUILayout.PropertyField(property, key.LG(), includeChildren);
    }

    public static void PropertyField(Rect position, SerializedProperty property, string key, bool includeChildren = true)
    {
        EditorGUI.PropertyField(position, property, key.LG(), includeChildren);
    }

    /// <summary>
    /// Returns option keys for enum: Enum:{enumType.Name}:{enumValueName}
    /// </summary>
    public static IEnumerable<string> GetEnumOptionKeys(Type enumType) =>
        enumType.GetEnumNames().Select(k => $"Enum:{enumType.Name}:{k}");
}

internal static class LocalizedPopup
{
    public static int Draw(int selectedIndex, string? labelKey, IEnumerable<string> optionKeys, params GUILayoutOption[] layoutOptions)
    {
        var label = labelKey != null ? labelKey.LG() : GUIContent.none;
        var contents = optionKeys.Select(k => k.LG()).ToArray();
        return EditorGUILayout.Popup(label, selectedIndex, contents, layoutOptions);
    }

    public static int Draw(Rect position, int selectedIndex, string? labelKey, IEnumerable<string> optionKeys)
    {
        var label = labelKey != null ? labelKey.LG() : GUIContent.none;
        var contents = optionKeys.Select(k => k.LG()).ToArray();
        return EditorGUI.Popup(position, label, selectedIndex, contents);
    }

    public static void Field(SerializedProperty enumProperty, string? labelKey, IEnumerable<string> optionKeys, Action<int>? onValueChanged = null, params GUILayoutOption[] layoutOptions)
    {
        var currentIndex = enumProperty.enumValueIndex;
        var newIndex = Draw(currentIndex, labelKey, optionKeys, layoutOptions);
        if (newIndex != currentIndex)
        {
            enumProperty.enumValueIndex = newIndex;
            onValueChanged?.Invoke(newIndex);
        }
    }

    public static void Field(Rect position, SerializedProperty enumProperty, string? labelKey, IEnumerable<string> optionKeys, Action<int>? onValueChanged = null)
    {
        var currentIndex = enumProperty.enumValueIndex;
        var newIndex = Draw(position, currentIndex, labelKey, optionKeys);
        if (newIndex != currentIndex)
        {
            enumProperty.enumValueIndex = newIndex;
            onValueChanged?.Invoke(newIndex);
        }
    }
}

internal static class LocalizedToolbar
{
    public static int Draw(int selectedIndex, IEnumerable<string> optionKeys, string? labelKey = null, params GUILayoutOption[] layoutOptions)
    {
        var contents = optionKeys.Select(k => k.LG()).ToArray();
        if (labelKey != null)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(labelKey.LG(), GUI.skin.button);
            selectedIndex = GUILayout.Toolbar(selectedIndex, contents, layoutOptions);
            EditorGUILayout.EndHorizontal();
            return selectedIndex;
        }
        return GUILayout.Toolbar(selectedIndex, contents, layoutOptions);
    }

    public static int Draw(Rect position, int selectedIndex, IEnumerable<string> optionKeys, string? labelKey = null)
    {
        var contents = optionKeys.Select(k => k.LG()).ToArray();
        if (labelKey != null)
        {
            position = EditorGUI.PrefixLabel(position, labelKey.LG());
        }
        return GUI.Toolbar(position, selectedIndex, contents);
    }

    public static void Field(Rect position, SerializedProperty enumProperty, IEnumerable<string> optionKeys, string? labelKey = null, Action<int>? onValueChanged = null)
    {
        var currentIndex = enumProperty.enumValueIndex;
        var newIndex = Draw(position, currentIndex, optionKeys, labelKey);
        if (newIndex != currentIndex)
        {
            enumProperty.enumValueIndex = newIndex;
            onValueChanged?.Invoke(newIndex);
        }
    }

    public static void Field(SerializedProperty enumProperty, IEnumerable<string> optionKeys, string? labelKey = null, Action<int>? onValueChanged = null, params GUILayoutOption[] layoutOptions)
    {
        var currentIndex = enumProperty.enumValueIndex;
        var newIndex = Draw(currentIndex, optionKeys, labelKey, layoutOptions);
        if (newIndex != currentIndex)
        {
            enumProperty.enumValueIndex = newIndex;
            onValueChanged?.Invoke(newIndex);
        }
    }
}
