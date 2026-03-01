namespace Aoyon.MaterialEditor.UI;

[CustomPropertyDrawer(typeof(MaterialEntrySettings))]
internal class MaterialEntrySettingsDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var mode = property.FindPropertyRelative(nameof(MaterialEntrySettings.Mode));
        var rect = position;
        rect.height = EditorGUI.GetPropertyHeight(mode, GUIContent.none, true);
        LocalizedToolbar.Field(rect, mode, LocalizedUI.GetEnumOptionKeys(typeof(MaterialEntrySettings.ApplyMode)), "Label:ApplyMode");
        rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;

        switch ((MaterialEntrySettings.ApplyMode)mode.enumValueIndex)
        {
            case MaterialEntrySettings.ApplyMode.Basic:
                var basicMaterial = property.FindPropertyRelative(nameof(MaterialEntrySettings.BasicMaterial));
                rect.height = EditorGUI.GetPropertyHeight(basicMaterial, GUIContent.none, true);
                LocalizedUI.PropertyField(rect, basicMaterial, "Label:Material", true);
                break;
            case MaterialEntrySettings.ApplyMode.Advanced:
                var advancedTargets = property.FindPropertyRelative(nameof(MaterialEntrySettings.AdvancedTargets));
                rect = GUIHelper.List(rect, advancedTargets, true, "Label:AdvancedTargets".LG(), prop => prop.CopyFrom(new MaterialTargetScope()));
                break;
            case MaterialEntrySettings.ApplyMode.All:
                var excludeTargets = property.FindPropertyRelative(nameof(MaterialEntrySettings.ExcludeTargets));
                var excludeObjectReferences = property.FindPropertyRelative(nameof(MaterialEntrySettings.ExcludeObjectReferences));
                rect.height = EditorGUI.GetPropertyHeight(excludeTargets, GUIContent.none, true);
                LocalizedUI.PropertyField(rect, excludeTargets, "Label:ExcludeTargets", true);
                rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;
                rect.height = EditorGUI.GetPropertyHeight(excludeObjectReferences, GUIContent.none, true);
                LocalizedUI.PropertyField(rect, excludeObjectReferences, "Label:ExcludeObjectReferences", true);
                break;
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var spacing = EditorGUIUtility.standardVerticalSpacing;
        var height = 0f;
        var mode = property.FindPropertyRelative(nameof(MaterialEntrySettings.Mode));
        height += EditorGUI.GetPropertyHeight(mode, GUIContent.none, true) + spacing;

        switch ((MaterialEntrySettings.ApplyMode)mode.enumValueIndex)
        {
            case MaterialEntrySettings.ApplyMode.Basic:
                var basicMaterial = property.FindPropertyRelative(nameof(MaterialEntrySettings.BasicMaterial));
                height += EditorGUI.GetPropertyHeight(basicMaterial, GUIContent.none, true);
                break;
            case MaterialEntrySettings.ApplyMode.Advanced:
                var advancedTargets = property.FindPropertyRelative(nameof(MaterialEntrySettings.AdvancedTargets));
                height += GUIHelper.GetListHeight(advancedTargets);
                break;
            case MaterialEntrySettings.ApplyMode.All:
                var excludeTargets = property.FindPropertyRelative(nameof(MaterialEntrySettings.ExcludeTargets));
                var excludeObjectReferences = property.FindPropertyRelative(nameof(MaterialEntrySettings.ExcludeObjectReferences));
                height += EditorGUI.GetPropertyHeight(excludeTargets, GUIContent.none, true) + spacing
                    + EditorGUI.GetPropertyHeight(excludeObjectReferences, GUIContent.none, true);
                break;
        }
        return height;
    }
}