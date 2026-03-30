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
    /// Returns option keys for enum values: {prefix}.{enumValueName}
    /// </summary>
    public static IEnumerable<string> GetEnumOptionKeys(string prefix, Type enumType) =>
        enumType.GetEnumNames().Select(k => $"{prefix}.{ToLowerCamelCase(k)}");

    public static List<string> GetEnumOptionKeys(string prefix, Type enumType, List<string> result)
    {
        foreach (var k in enumType.GetEnumNames())
        {
            result.Add($"{prefix}.{ToLowerCamelCase(k)}");
        }
        return result;
    }

    private static string ToLowerCamelCase(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        if (value.Length == 1) return value.ToLowerInvariant();
        return char.ToLowerInvariant(value[0]) + value.Substring(1);
    }
}

internal static class LocalizedPopup
{
    public static int Draw(int selectedIndex, string? labelKey, IEnumerable<string> optionKeys, GUIStyle? style = null, params GUILayoutOption[] layoutOptions)
    {
        var label = labelKey != null ? labelKey.LG() : GUIContent.none;
        var contents = optionKeys.Select(k => k.LG() ?? new GUIContent(k)).ToArray();
        style ??= EditorStyles.popup;
        return EditorGUILayout.Popup(label, selectedIndex, contents, style, layoutOptions);
    }

    public static int Draw(Rect position, int selectedIndex, string? labelKey, IEnumerable<string> optionKeys, GUIStyle? style = null)
    {
        var label = labelKey != null ? labelKey.LG() : GUIContent.none;
        var contents = optionKeys.Select(k => k.LG() ?? new GUIContent(k)).ToArray();
        style ??= EditorStyles.popup;
        return EditorGUI.Popup(position, label, selectedIndex, contents, style);
    }

    public static void Field(SerializedProperty enumProperty, string? labelKey, IEnumerable<string> optionKeys, GUIStyle? style = null, Action<int>? onValueChanged = null, params GUILayoutOption[] layoutOptions)
    {
        // Todo: BeginProperty
        
        var currentIndex = enumProperty.enumValueIndex;
        var newIndex = Draw(currentIndex, labelKey, optionKeys, style, layoutOptions);
        if (newIndex != currentIndex)
        {
            enumProperty.enumValueIndex = newIndex;
            onValueChanged?.Invoke(newIndex);
        }
    }

    public static void Field(Rect position, SerializedProperty enumProperty, string? labelKey, IEnumerable<string> optionKeys, GUIStyle? style = null, Action<int>? onValueChanged = null)
    {
        using var _ = new EditorGUI.PropertyScope(position, GUIContent.none, enumProperty);
        
        var currentIndex = enumProperty.enumValueIndex;
        var newIndex = Draw(position, currentIndex, labelKey, optionKeys, style);
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
        using var _ = new EditorGUI.PropertyScope(position, GUIContent.none, enumProperty);
        
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
        // Todo: BeginProperty

        var currentIndex = enumProperty.enumValueIndex;
        var newIndex = Draw(currentIndex, optionKeys, labelKey, layoutOptions);
        if (newIndex != currentIndex)
        {
            enumProperty.enumValueIndex = newIndex;
            onValueChanged?.Invoke(newIndex);
        }
    }
}
