using nadena.dev.modular_avatar.core;
using Aoyon.MaterialEditor.Processor;

namespace Aoyon.MaterialEditor.UI;

internal static class AvatarObjectReferenceCollectionUI
{
    public static GUIHelper.DropHandler DropHandler { get; } = new(
        o => o is GameObject,
        OnItemsDropped);

    public static Rect Draw(Rect position, SerializedProperty property, GUIContent label, GUIHelper.ListOptions? options)
    {
        return Draw(position, property, label, options, DropHandler);
    }

    public static Rect Draw(
        Rect position,
        SerializedProperty property,
        GUIContent label,
        GUIHelper.ListOptions? options,
        GUIHelper.DropHandler? dropHandler)
    {
        if (!property.isArray) {
            EditorGUI.PropertyField(position, property, label);
            return position.NewLine();
        }

        return GUIHelper.DragAndDropList(position, property, label, prop => {
            prop.CopyFrom(new AvatarObjectReference());
        }, dropHandler, options ?? new GUIHelper.ListOptions());
    }

    public static float GetHeight(SerializedProperty property, GUIContent label, GUIHelper.ListOptions? options)
    {
        if (!property.isArray) {
            return EditorGUI.GetPropertyHeight(property, GUIContent.none);
        }
        return GUIHelper.GetDragAndDropListHeight(property, options ?? new GUIHelper.ListOptions(), DropHandler);
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
}

internal static class MaterialCollectionUI
{
    public static GUIHelper.DropHandler DefaultDropHandler { get; } = new(
        o => o is Material,
        OnItemsDropped);

    public static GUIHelper.DropHandler MaterialOrRendererDropHandler { get; } = new(
        IsMaterialOrRendererSource,
        OnMaterialsOrRenderersDropped,
        "targetSettings.dragAndDropAdd");

    public static Rect Draw(Rect position, SerializedProperty property, GUIContent label, GUIHelper.ListOptions? options)
    {
        return Draw(position, property, label, options, DefaultDropHandler);
    }

    public static Rect Draw(
        Rect position,
        SerializedProperty property,
        GUIContent label,
        GUIHelper.ListOptions? options,
        GUIHelper.DropHandler? dropHandler)
    {
        if (!property.isArray)
        {
            EditorGUI.PropertyField(position, property, label);
            return position.NewLine();
        }

        return GUIHelper.DragAndDropList(position, property, label, prop =>
        {
            prop.objectReferenceValue = null;
        }, dropHandler, options ?? new GUIHelper.ListOptions());
    }

    public static float GetHeight(SerializedProperty property, GUIContent label, GUIHelper.ListOptions? options)
    {
        return GetHeight(property, label, options, DefaultDropHandler);
    }

    public static float GetHeight(
        SerializedProperty property,
        GUIContent label,
        GUIHelper.ListOptions? options,
        GUIHelper.DropHandler? dropHandler)
    {
        if (!property.isArray)
        {
            return EditorGUI.GetPropertyHeight(property, GUIContent.none);
        }
        return GUIHelper.GetDragAndDropListHeight(property, options ?? new GUIHelper.ListOptions(), dropHandler);
    }

    private static void OnItemsDropped(SerializedProperty property, Object[] items)
    {
        foreach (var material in items.OfType<Material>())
        {
            AppendMaterial(property, material);
        }

        property.serializedObject.ApplyModifiedProperties();
    }

    public static bool ContainsMaterial(SerializedProperty property, Material material)
    {
        for (int i = 0; i < property.arraySize; i++)
        {
            if (property.GetArrayElementAtIndex(i).objectReferenceValue == material)
            {
                return true;
            }
        }
        return false;
    }

    public static void AppendMaterial(SerializedProperty property, Material material)
    {
        if (ContainsMaterial(property, material)) return;

        property.arraySize++;
        property.GetArrayElementAtIndex(property.arraySize - 1).objectReferenceValue = material;
    }

    private static bool IsMaterialOrRendererSource(Object item)
    {
        return item is Material or Renderer or GameObject;
    }

    private static void OnMaterialsOrRenderersDropped(SerializedProperty property, Object[] items)
    {
        if (!property.TryGetGameObject(out var gameObject)) return;

        var seen = new HashSet<Material>();
        foreach (var material in items.SelectMany(item => Utils.GetAllTargetMaterialsInAvatar(gameObject, item)))
        {
            if (ContainsMaterial(property, material)) continue;
            if (!seen.Add(material)) continue;
            AppendMaterial(property, material);
        }

        property.serializedObject.ApplyModifiedProperties();
    }
}

internal static class MaterialSlotReferenceCollectionUI
{
    public static GUIHelper.DropHandler DefaultDropHandler { get; } = new(
        IsSlotSource,
        OnItemsDropped,
        "targetSettings.dragAndDropAdd");

    public static Rect Draw(Rect position, SerializedProperty property, GUIContent label, GUIHelper.ListOptions? options)
    {
        return Draw(position, property, label, options, DefaultDropHandler);
    }

    public static Rect Draw(
        Rect position,
        SerializedProperty property,
        GUIContent label,
        GUIHelper.ListOptions? options,
        GUIHelper.DropHandler? dropHandler)
    {
        if (!property.isArray)
        {
            EditorGUI.PropertyField(position, property, label);
            return position.NewLine();
        }

        return GUIHelper.DragAndDropList(position, property, label, prop =>
        {
            prop.CopyFrom(new MaterialSlotReference());
        }, dropHandler, options ?? new GUIHelper.ListOptions());
    }

    public static float GetHeight(SerializedProperty property, GUIContent label, GUIHelper.ListOptions? options)
    {
        return GetHeight(property, label, options, DefaultDropHandler);
    }

    public static float GetHeight(
        SerializedProperty property,
        GUIContent label,
        GUIHelper.ListOptions? options,
        GUIHelper.DropHandler? dropHandler)
    {
        if (!property.isArray)
        {
            return EditorGUI.GetPropertyHeight(property, GUIContent.none);
        }
        return GUIHelper.GetDragAndDropListHeight(property, options ?? new GUIHelper.ListOptions(), dropHandler);
    }

    private static void OnItemsDropped(SerializedProperty property, Object[] items)
    {
        property.TryGetGameObject(out var gameObject);
        foreach (var item in items)
        {
            switch (item)
            {
                case Material material:
                    if (gameObject == null) continue;
                    foreach (var slot in EnumerateMaterialUsages(gameObject, material))
                    {
                        if (ContainsSlot(property, slot)) continue;
                        AppendSlot(property, slot);
                    }
                    break;

                case Renderer renderer:
                    if (gameObject == null) continue;
                    if (!Utils.GetTargetRenderersUnderInAvatar(gameObject, renderer.gameObject).Contains(renderer)) continue;
                    AppendRendererSlot(property, renderer);
                    break;

                case GameObject go:
                    if (gameObject == null) continue;
                    foreach (var renderer in Utils.GetTargetRenderersUnderInAvatar(gameObject, go))
                    {
                        AppendRendererSlot(property, renderer);
                    }
                    break;
            }
        }

        property.serializedObject.ApplyModifiedProperties();
    }

    private static bool IsSlotSource(Object item)
    {
        return item is Material or Renderer or GameObject;
    }

    private static void AppendRendererSlot(SerializedProperty property, Renderer renderer)
    {
        var slotReference = new MaterialSlotReference
        {
            RendererReference = new AvatarObjectReference(renderer.gameObject),
            MaterialIndex = -1,
        };
        if (ContainsSlot(property, slotReference)) return;
        AppendSlot(property, slotReference);
    }

    public static IEnumerable<MaterialSlotReference> EnumerateMaterialUsages(GameObject maker, Material material)
    {
        var root = Utils.FindAvatarInParents(maker);
        if (root == null) yield break;

        var renderers = MaterialEditorProcessor.GetTargetRenderers(root);
        var assignments = new DefaultMaterialTargeting().GetAssignments(renderers);
        foreach (var assignment in assignments.Where(a => a.Material == material))
        {
            yield return new MaterialSlotReference
            {
                RendererReference = new AvatarObjectReference(assignment.SlotId.Renderer.gameObject),
                MaterialIndex = assignment.SlotId.MaterialIndex,
            };
        }
    }

    public static void AppendSlot(SerializedProperty property, MaterialSlotReference slot)
    {
        property.arraySize++;
        property.GetArrayElementAtIndex(property.arraySize - 1).CopyFrom(slot);
    }

    public static bool ContainsSlot(SerializedProperty property, MaterialSlotReference slot)
    {
        for (int i = 0; i < property.arraySize; i++)
        {
            var element = property.GetArrayElementAtIndex(i);
            var rendererReference = element.FindPropertyRelative(nameof(MaterialSlotReference.RendererReference));
            var materialIndex = element.FindPropertyRelative(nameof(MaterialSlotReference.MaterialIndex));

            if (AvatarObjectReference.Get(rendererReference) == slot.RendererReference.Get(property.serializedObject.targetObject as Component)
                && materialIndex.intValue == slot.MaterialIndex)
            {
                return true;
            }
        }
        return false;
    }

    public static void DrawAddSlotsSelector(Rect position, SerializedProperty slotsProperty, Func<MaterialSlotReference[]> getUsageSlots, string? label = null)
    {
        AdvancedSelector<MaterialSlotReference>.Draw(position, getUsageSlots, (slot, _) => OnUsageSelected(slotsProperty, slot), slots => GetUsageSlotLabels(slotsProperty, slots), 
            label: new(label), style: null, getSelectLabel: () => label ?? "common.select".LS());
    }

    public static void OpenAddSlotsSelector(Rect position, SerializedProperty slotsProperty, Func<MaterialSlotReference[]> getUsageSlots, string? label = null)
    {
        AdvancedSelector<MaterialSlotReference>.Open(
            position,
            getUsageSlots,
            (slot, _) => OnUsageSelected(slotsProperty, slot),
            slots => GetUsageSlotLabels(slotsProperty, slots),
            getSelectLabel: () => label ?? "common.select".LS());
    }

    private static void OnUsageSelected(SerializedProperty slotsProperty, MaterialSlotReference? slot)
    {
        if (slot == null) return;
        slotsProperty.arraySize++;
        slotsProperty.GetArrayElementAtIndex(slotsProperty.arraySize - 1).CopyFrom(slot);
        slotsProperty.serializedObject.ApplyModifiedProperties();
    }

    private static string[] GetUsageSlotLabels(SerializedProperty slotsProperty, MaterialSlotReference?[] slots)
    {
        return slots.Select(slot => GetUsageSlotLabel(slotsProperty, slot)).ToArray();
    }

    private static string GetUsageSlotLabel(SerializedProperty slotsProperty, MaterialSlotReference? slot)
    {
        var rendererName = slot?.RendererReference.Get(slotsProperty.serializedObject.targetObject as Component)?.name ?? "None";
        var materialIndex = slot?.MaterialIndex ?? -1;
        return $"{rendererName}: {materialIndex}";
    }
}
