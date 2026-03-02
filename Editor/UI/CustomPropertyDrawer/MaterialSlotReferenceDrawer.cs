using nadena.dev.modular_avatar.core;

namespace Aoyon.MaterialEditor.UI;

[CustomPropertyDrawer(typeof(MaterialSlotReference))]
internal class MaterialSlotReferenceDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        var rect = position;
        rect.height = EditorGUIUtility.singleLineHeight;

        var rendererReference = property.FindPropertyRelative(nameof(MaterialSlotReference.RendererReference));
        var materialIndex = property.FindPropertyRelative(nameof(MaterialSlotReference.MaterialIndex));

        GUIHelper.SplitRectHorizontally(rect, 0.4f, out var rendererRect, out var materialSlotRect);

        EditorGUI.PropertyField(rendererRect, rendererReference, GUIContent.none);
        DrawMaterialSlot(materialSlotRect, rendererReference, materialIndex);

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }

    private static readonly Material _allMaterials = new(Shader.Find("Standard")) { name = "All Materials" };
    private void DrawMaterialSlot(Rect position, SerializedProperty rendererReference, SerializedProperty materialIndex)
    {
        var selectorContent = "Label:Select".LG();
        var selectorWidth = GUI.skin.button.CalcSize(selectorContent).x + 16f;

        GUIHelper.SplitRectHorizontallyForRight(position, selectorWidth, out var materialRect, out var selectorRect);

        Renderer? renderer = null;
        var gameObject = AvatarObjectReference.Get(rendererReference);
        if (gameObject != null)
        {
            gameObject.transform.TryGetComponent<Renderer>(out renderer);
        }

        Material? material = null;
        if (renderer != null)
        {
            var mats = renderer.sharedMaterials;
            var materialIndexValue = materialIndex.intValue;

            if (materialIndexValue == -1)
            {
                material = _allMaterials;
            }
            else if (materialIndexValue >= 0 && materialIndexValue < mats.Length)
            {
                material = mats[materialIndexValue].DestroyedAsNull();
            }
        }

        using (new EditorGUI.DisabledGroupScope(true))
        {
            EditorGUI.ObjectField(materialRect, material, typeof(Material), false);
        }

        using (new EditorGUI.DisabledGroupScope(renderer == null))
        {
            if (GUI.Button(selectorRect, selectorContent, StyleHelper.CenteredPopupStyle))
            {
                var dropdown = new MaterialAdvancedDropdown(
                    GetMaterials(renderer!),
                    (material, index) => OnSelected(materialIndex, material, index),
                    new(),
                    (mat, index) => mat.name + " : " + (index-1));
                dropdown.Show(position);
            }
        }
    }

    private static List<Material> GetMaterials(Renderer renderer)
    {
        var materials = renderer.sharedMaterials.SkipDestroyed().ToList();
        materials.Insert(0, _allMaterials);
        return materials;
    }

    private static void OnSelected(SerializedProperty materialIndex, Material material, int index)
    {
        if (index == 0) // all materials
        {
            materialIndex.intValue = -1;
        }
        else
        {
            materialIndex.intValue = index - 1;
        }
        materialIndex.serializedObject.ApplyModifiedProperties();
    }
}