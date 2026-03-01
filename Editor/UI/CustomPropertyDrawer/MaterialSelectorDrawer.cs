using UnityEditor.IMGUI.Controls;

namespace Aoyon.MaterialEditor.UI;

[CustomPropertyDrawer(typeof(MaterialSelectorAttribute))]
internal class MaterialSelectorDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        float spacing = 4f;
        var selectorLabel = "Label:Select".LS();
        var selectorContent = new GUIContent(selectorLabel);
        float selectorWidth = GUI.skin.button.CalcSize(selectorContent).x + 16f;

        var objectFieldRect = new Rect(position.x, position.y, position.width - selectorWidth - spacing, position.height);
        var selectorRect = new Rect(position.x + position.width - selectorWidth, position.y, selectorWidth, position.height);

        property.objectReferenceValue = EditorGUI.ObjectField(objectFieldRect, label, property.objectReferenceValue, typeof(Material), true);
        if (GUI.Button(selectorRect, selectorContent, StyleHelper.CenteredPopupStyle))
        {
            var dropdown = new MaterialAdvancedDropdown(GetMaterials(property), 
                (material) => OnSelected(property, material), 
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

    private static void OnSelected(SerializedProperty property, Material material)
    {
        property.objectReferenceValue = material;
        property.serializedObject.ApplyModifiedProperties();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }
}