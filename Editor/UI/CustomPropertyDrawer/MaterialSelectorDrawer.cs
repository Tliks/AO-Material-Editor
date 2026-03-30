namespace Aoyon.MaterialEditor.UI;

[CustomPropertyDrawer(typeof(MaterialSelectorAttribute))]
internal class MaterialSelectorDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        using var _ = new EditorGUI.PropertyScope(position, GUIContent.none, property);

        position.SetSingleHeight();

        var selectorWidth = MaterialSelector.GetSize().x;

        GUIHelper.SplitRectHorizontallyForRight(position, selectorWidth, out var objectFieldRect, out var selectorRect);

        property.objectReferenceValue = EditorGUI.ObjectField(objectFieldRect, label, property.objectReferenceValue, typeof(Material), true);
        MaterialSelector.Draw(selectorRect, () => Utils.GetAllTargetMaterialsInAvatar(property), (m, i) => OnSelected(property, m, i));
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
