namespace Aoyon.MaterialEditor.UI;

[CustomPropertyDrawer(typeof(MaterialSelectorAttribute))]
internal class MaterialSelectorDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        using var _ = new EditorGUI.PropertyScope(position, GUIContent.none, property);

        position.SetSingleHeight();

        var selectorWidth = MaterialSelector.GetSize().x;
        var fieldAreaRect = position;
        if (label != GUIContent.none)
        {
            var labelWidth = GetLabelWidth(label, selectorWidth);
            GUIHelper.SplitRectHorizontallyForLeft(position, labelWidth, out var labelRect, out fieldAreaRect);
            EditorGUI.LabelField(labelRect, label);
        }
        GUIHelper.SplitRectHorizontallyForLeft(fieldAreaRect, selectorWidth, out var selectorRect, out var objectFieldRect);

        MaterialSelector.Draw(selectorRect, () => Utils.GetAllTargetMaterialsInAvatar(property), (m, i) => OnSelected(property, m, i));
        property.objectReferenceValue = EditorGUI.ObjectField(objectFieldRect, GUIContent.none, property.objectReferenceValue, typeof(Material), true);
    }

    private static float GetLabelWidth(GUIContent label, float selectorWidth)
    {
        var preferredWidth = EditorStyles.label.CalcSize(label).x;
        return Mathf.Max(preferredWidth, EditorGUIUtility.labelWidth - selectorWidth / 2f);
    }

    private static void OnSelected(SerializedProperty property, Material? material, int index)
    {
        property.objectReferenceValue = material;
        property.serializedObject.ApplyModifiedProperties();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }
}
