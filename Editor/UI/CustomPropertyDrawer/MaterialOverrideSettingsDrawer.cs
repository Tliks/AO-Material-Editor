namespace Aoyon.MaterialEditor.UI;

[CustomPropertyDrawer(typeof(MaterialOverrideSettings))]
internal class MaterialOverrideSettingsDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var overrideShader = property.FindPropertyRelative(nameof(MaterialOverrideSettings.OverrideShader));
        var targetShader = property.FindPropertyRelative(nameof(MaterialOverrideSettings.TargetShader));
        var overrideRenderQueue = property.FindPropertyRelative(nameof(MaterialOverrideSettings.OverrideRenderQueue));
        var renderQueueValue = property.FindPropertyRelative(nameof(MaterialOverrideSettings.RenderQueueValue));
        var propertyOverrides = property.FindPropertyRelative(nameof(MaterialOverrideSettings.PropertyOverrides));

        var rect = position;
        var spacing = EditorGUIUtility.standardVerticalSpacing;

        rect.height = EditorGUI.GetPropertyHeight(overrideShader, GUIContent.none, true);
        EditorGUI.PropertyField(rect, overrideShader, true);
        rect.y += rect.height + spacing;

        rect.height = EditorGUI.GetPropertyHeight(targetShader, GUIContent.none, true);
        using (new EditorGUI.DisabledScope(!overrideShader.boolValue))
        {
            EditorGUI.PropertyField(rect, targetShader, true);
        }
        rect.y += rect.height + spacing;

        rect.height = EditorGUI.GetPropertyHeight(overrideRenderQueue, GUIContent.none, true);
        EditorGUI.PropertyField(rect, overrideRenderQueue, true);
        rect.y += rect.height + spacing;

        rect.height = EditorGUI.GetPropertyHeight(renderQueueValue, GUIContent.none, true);
        using (new EditorGUI.DisabledScope(!overrideRenderQueue.boolValue))
        {
            EditorGUI.PropertyField(rect, renderQueueValue, true);
        }
        rect.y += rect.height + spacing;

        rect.height = EditorGUI.GetPropertyHeight(propertyOverrides, GUIContent.none, true);
        EditorGUI.PropertyField(rect, propertyOverrides, true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var overrideShader = property.FindPropertyRelative(nameof(MaterialOverrideSettings.OverrideShader));
        var targetShader = property.FindPropertyRelative(nameof(MaterialOverrideSettings.TargetShader));
        var overrideRenderQueue = property.FindPropertyRelative(nameof(MaterialOverrideSettings.OverrideRenderQueue));
        var renderQueueValue = property.FindPropertyRelative(nameof(MaterialOverrideSettings.RenderQueueValue));
        var propertyOverrides = property.FindPropertyRelative(nameof(MaterialOverrideSettings.PropertyOverrides));

        var spacing = EditorGUIUtility.standardVerticalSpacing;
        var height = 0f;
        height += EditorGUI.GetPropertyHeight(overrideShader, GUIContent.none, true) + spacing;
        height += EditorGUI.GetPropertyHeight(targetShader, GUIContent.none, true) + spacing;
        height += EditorGUI.GetPropertyHeight(overrideRenderQueue, GUIContent.none, true) + spacing;
        height += EditorGUI.GetPropertyHeight(renderQueueValue, GUIContent.none, true) + spacing;
        height += EditorGUI.GetPropertyHeight(propertyOverrides, GUIContent.none, true);
        return height;
    }
}
