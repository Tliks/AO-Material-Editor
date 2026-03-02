using UnityEditor.IMGUI.Controls;

namespace Aoyon.MaterialEditor.UI;

[CustomPropertyDrawer(typeof(MaterialSelectorAttribute))]
internal class MaterialSelectorDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var selectorContent = "Label:Select".LG();
        var selectorWidth = GUI.skin.button.CalcSize(selectorContent).x + 16f;

        GUIHelper.SplitRectHorizontallyForRight(position, selectorWidth, out var objectFieldRect, out var selectorRect);

        property.objectReferenceValue = EditorGUI.ObjectField(objectFieldRect, label, property.objectReferenceValue, typeof(Material), true);
        if (GUI.Button(selectorRect, selectorContent, StyleHelper.CenteredPopupStyle))
        {
            var dropdown = new MaterialAdvancedDropdown(GetMaterials(property), 
                (material, index) => OnSelected(property, material, index), 
                new AdvancedDropdownState());
            dropdown.Show(position);
        }

        EditorGUI.EndProperty();
    }

    private static List<Material> GetMaterials(SerializedProperty property)
    {
        var component = property.serializedObject.targetObject as Component;
        var gameObject = component != null ? component.gameObject : property.serializedObject.targetObject as GameObject;
        if (gameObject == null) return new List<Material>();

        var root = Utils.FindAvatarInParents(gameObject);
        if (root == null) return new List<Material>();

        return root.GetComponentsInChildren<Renderer>(true)
            .SelectMany(x => x.sharedMaterials)
            .Where(x => x != null)
            .Distinct()
            .ToList();
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