namespace Aoyon.MaterialEditor.UI;

[CustomPropertyDrawer(typeof(MaterialTargetScope))]
internal class MaterialTargetScopeDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var rect = position;
        rect.height = EditorGUIUtility.singleLineHeight;

        var type = property.FindPropertyRelative(nameof(MaterialTargetScope.Type));

        GUIHelper.SplitRectHorizontally(rect, 0.18f, 60f, out var typeRect, out var valueRect);

        LocalizedPopup.Field(typeRect, type, null, LocalizedUI.GetEnumOptionKeys(typeof(MaterialTargetScope.ScopeType)));

        switch ((MaterialTargetScope.ScopeType)type.enumValueIndex)
        {
            case MaterialTargetScope.ScopeType.Asset:
                var material = property.FindPropertyRelative(nameof(MaterialTargetScope.Material));
                EditorGUI.PropertyField(valueRect, material, GUIContent.none);
                break;
            case MaterialTargetScope.ScopeType.Slot:
                var materialSlotReference = property.FindPropertyRelative(nameof(MaterialTargetScope.MaterialSlotReference));
                EditorGUI.PropertyField(valueRect, materialSlotReference, GUIContent.none);
                break;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }
}