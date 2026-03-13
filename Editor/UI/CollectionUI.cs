using nadena.dev.modular_avatar.core;

namespace Aoyon.MaterialEditor.UI;

internal static class MaterialTargetScopeCollectionUI
{
    public static void Draw (Rect position, SerializedProperty property, GUIContent label)
    {
        if (!property.isArray) {
            EditorGUI.PropertyField(position, property, label);
            return;
        }

        GUIHelper.DragAndDropList(position, property, true, label, prop => {
            var beforeType = prop.FindPropertyRelative(nameof(MaterialTargetScope.Type)).enumValueIndex == 0 
                ? MaterialTargetScope.ScopeType.Asset : MaterialTargetScope.ScopeType.Slot;
            prop.CopyFrom(new MaterialTargetScope() { Type = beforeType });
        },o => o is Material or GameObject, OnItemsDropped);
    }

    private static void OnItemsDropped(SerializedProperty property, Object[] items)
    {
        foreach (var item in items)
        {
            if (item is Material material)
            {
                property.arraySize++;
                var element = property.GetArrayElementAtIndex(property.arraySize - 1);
                element.CopyFrom(new MaterialTargetScope
                {
                    Type = MaterialTargetScope.ScopeType.Asset,
                    Material = material,
                });
            }
            else if (item is GameObject go && go.TryGetComponent<Renderer>(out _))
            {
                property.arraySize++;
                var element = property.GetArrayElementAtIndex(property.arraySize - 1);
                element.CopyFrom(new MaterialTargetScope
                {
                    Type = MaterialTargetScope.ScopeType.Slot,
                    MaterialSlotReference = new MaterialSlotReference
                    {
                        RendererReference = new AvatarObjectReference(go),
                        MaterialIndex = -1,
                    }
                });
            }
        }

        property.serializedObject.ApplyModifiedProperties();
    }

    public static float GetHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isArray) {
            return EditorGUI.GetPropertyHeight(property, GUIContent.none);
        }
        return GUIHelper.GetListHeight(property);
    }
}

internal static class AvatarObjectReferenceCollectionUI
{
    public static void Draw(Rect position, SerializedProperty property, GUIContent label)
    {
        if (!property.isArray) {
            EditorGUI.PropertyField(position, property, label);
            return;
        }

        GUIHelper.DragAndDropList(position, property, true, label, prop => {
            prop.CopyFrom(new AvatarObjectReference());
        },o => o is GameObject, OnItemsDropped);
    }

    private static void OnItemsDropped(SerializedProperty property, Object[] items)
    {
        foreach (var item in items)
        {
            if (item is GameObject go)
            {
                property.arraySize++;
                var element = property.GetArrayElementAtIndex(property.arraySize - 1);
                element.CopyFrom(new AvatarObjectReference(go));
            }
        }

        property.serializedObject.ApplyModifiedProperties();
    }

    public static float GetHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isArray) {
            return EditorGUI.GetPropertyHeight(property, GUIContent.none);
        }
        return GUIHelper.GetListHeight(property);
    }
}