namespace Aoyon.MaterialEditor.UI;

[CustomPropertyDrawer(typeof(MaterialTargetScope))]
internal class MaterialTargetScopeDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var type = property.FindPropertyRelative(nameof(MaterialTargetScope.Type));
        var rect = position;
        rect.height = EditorGUI.GetPropertyHeight(type, GUIContent.none, true);
        EditorGUI.PropertyField(rect, type, true);
        rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;

        switch ((MaterialTargetScope.ScopeType)type.enumValueIndex)
        {
            case MaterialTargetScope.ScopeType.Asset:
                var material = property.FindPropertyRelative(nameof(MaterialTargetScope.Material));
                rect.height = EditorGUI.GetPropertyHeight(material, GUIContent.none, true);
                EditorGUI.PropertyField(rect, material, true);
                break;
            case MaterialTargetScope.ScopeType.Slot:
                var rendererReference = property.FindPropertyRelative(nameof(MaterialTargetScope.RendererReference));
                var materialIndex = property.FindPropertyRelative(nameof(MaterialTargetScope.MaterialIndex));
                rect.height = EditorGUI.GetPropertyHeight(rendererReference, GUIContent.none, true);
                EditorGUI.PropertyField(rect, rendererReference, true);
                rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;
                rect.height = EditorGUI.GetPropertyHeight(materialIndex, GUIContent.none, true);
                EditorGUI.PropertyField(rect, materialIndex, true);
                break;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var spacing = EditorGUIUtility.standardVerticalSpacing;
        var type = property.FindPropertyRelative(nameof(MaterialTargetScope.Type));
        var height = EditorGUI.GetPropertyHeight(type, GUIContent.none, true) + spacing;
        switch ((MaterialTargetScope.ScopeType)type.enumValueIndex)
        {
            case MaterialTargetScope.ScopeType.Asset:
                var material = property.FindPropertyRelative(nameof(MaterialTargetScope.Material));
                height += EditorGUI.GetPropertyHeight(material, GUIContent.none, true);
                break;
            case MaterialTargetScope.ScopeType.Slot:
                var rendererReference = property.FindPropertyRelative(nameof(MaterialTargetScope.RendererReference));
                var materialIndex = property.FindPropertyRelative(nameof(MaterialTargetScope.MaterialIndex));
                height += EditorGUI.GetPropertyHeight(rendererReference, GUIContent.none, true) + spacing
                    + EditorGUI.GetPropertyHeight(materialIndex, GUIContent.none, true);
                break;
        }
        return height;
    }
}