namespace Aoyon.MaterialEditor.UI;

[CustomPropertyDrawer(typeof(MaterialSelectorAttribute))]
internal class MaterialSelectorDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        position.SetSingleHeight();

        var selectorWidth = MaterialSelector.GetSize().x;

        GUIHelper.SplitRectHorizontallyForRight(position, selectorWidth, out var objectFieldRect, out var selectorRect);

        property.objectReferenceValue = EditorGUI.ObjectField(objectFieldRect, label, property.objectReferenceValue, typeof(Material), true);
        MaterialSelector.Draw(selectorRect, () => GetMaterials(property), (m, i) => OnSelected(property, m, i));

        EditorGUI.EndProperty();
    }

    private static List<Material> GetMaterials(SerializedProperty property)
    {
        var component = property.serializedObject.targetObject as Component;
        var gameObject = component != null ? component.gameObject : property.serializedObject.targetObject as GameObject;
        if (gameObject == null) return new List<Material>();

        var root = Utils.FindAvatarInParents(gameObject);
        if (root == null) return new List<Material>();

        return Utils.GetTargetMaterials(root);
    }

    private static void OnSelected(SerializedProperty property, Material material, int index)
    {
        property.objectReferenceValue = material;
        property.serializedObject.ApplyModifiedProperties();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }
}