namespace Aoyon.MaterialEditor.UI;

[CustomPropertyDrawer(typeof(MaterialTargetScope))]
internal class MaterialTargetScopeDrawer : PropertyDrawer
{
    private static readonly GUIStyle _typeStyle = StyleHelper.CenteredPopupStyle;
    private static float? _typeWidth;
    private static float TypeWidth =>
        _typeWidth ??= LocalizedUI.GetEnumOptionKeys(typeof(MaterialTargetScope.ScopeType))
            .Select(k => _typeStyle.CalcSize(k.LG()).x)
            .Max() + 8f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        position.SetSingleHeight();

        var type = property.FindPropertyRelative(nameof(MaterialTargetScope.Type));

        GUIHelper.SplitRectHorizontallyForLeft(position, TypeWidth, out var typeRect, out var valueRect);

        LocalizedPopup.Field(typeRect, type, null, LocalizedUI.GetEnumOptionKeys(typeof(MaterialTargetScope.ScopeType)), _typeStyle);

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
        return GUIHelper.propertyHeight;
    }
}