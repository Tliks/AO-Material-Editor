using nadena.dev.modular_avatar.core;

namespace Aoyon.MaterialEditor.UI;

[CustomPropertyDrawer(typeof(MaterialSlotReference))]
internal class MaterialSlotReferenceDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        using var _ = new EditorGUI.PropertyScope(position, GUIContent.none, property);
        
        position.SetSingleHeight();

        var rendererReference = property.FindPropertyRelative(nameof(MaterialSlotReference.RendererReference));
        var materialIndex = property.FindPropertyRelative(nameof(MaterialSlotReference.MaterialIndex));

        GUIHelper.SplitRectHorizontally(position, 0.5f, out var rendererRect, out var materialSlotRect);

        EditorGUI.PropertyField(rendererRect, rendererReference, GUIContent.none);
        MaterialSlotSelector.Draw(materialSlotRect, rendererReference, materialIndex);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return GUIHelper.propertyHeight;
    }

    class MaterialSlotSelector : MaterialSelector
    {
        public static void Draw(Rect position, SerializedProperty rendererReference, SerializedProperty materialIndex)
        {
            Renderer? renderer = null;
            var gameObject = AvatarObjectReference.Get(rendererReference);
            if (gameObject != null)
            {
                gameObject.transform.TryGetComponent<Renderer>(out renderer);
            }

            if (renderer != null)
            {
                using var _ = new EditorGUI.PropertyScope(position, GUIContent.none, materialIndex);

                var currentIndex = materialIndex.intValue;
                var materials = renderer.sharedMaterials;
                var currentMaterial = 0 <= currentIndex && currentIndex < materials.Length ? materials[currentIndex] : null;

                Draw(position,
                    () => (new Material?[] { null }).Concat(materials).ToArray(),
                    (m, i) => OnSelected(materialIndex, m, i - 1),
                    (mats) => GetItemLabels(mats),
                    null,
                    new(GetItemLabel(currentMaterial, currentIndex)),
                    EditorStyles.popup,
                    GetSelectLabel
                );
            }
            else
            {
                EditorGUI.PropertyField(position, materialIndex, GUIContent.none);
            }
        }

        private static string[] GetItemLabels(Material?[] materials)
        {
            var labels = new string[materials.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                labels[i] = GetItemLabel(materials[i], i - 1);
            }
            return labels;
        }

        private static string GetItemLabel(Material? material, int slotIndex)
        {
            var name = slotIndex == -1 ? "Label:AllMaterials".LS() : GetDefaultItemLabel(material);
            return string.Format("{0} : {1}", slotIndex, name);
        }

        private static string GetSelectLabel()
        {
            return string.Format("Label:SelectWithName".LS(), "Label:MaterialSlot".LS());
        }

        private static void OnSelected(SerializedProperty materialIndex, Material? material, int slotIndex)
        {
            materialIndex.intValue = slotIndex;
            materialIndex.serializedObject.ApplyModifiedProperties();
        }
    }
}
