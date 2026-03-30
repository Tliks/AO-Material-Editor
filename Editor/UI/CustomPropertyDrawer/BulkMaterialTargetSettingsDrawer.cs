namespace Aoyon.MaterialEditor.UI;

[CustomPropertyDrawer(typeof(BulkMaterialTargetSettings))]
internal class BulkMaterialTargetSettingsDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var materials = property.FindPropertyRelative(nameof(BulkMaterialTargetSettings.TargetMaterials));
        var useExclusions = property.FindPropertyRelative(nameof(BulkMaterialTargetSettings.UseSlotExclusions));
        var excludedSlots = property.FindPropertyRelative(nameof(BulkMaterialTargetSettings.ExcludedSlots));
        var maxVisibleListHeight = GUIHelper.DefaultScrollableListHeight;
        var materialsListOptions = new GUIHelper.ListOptions(
            foldout: new GUIHelper.FoldoutOptions(Draw: false),
            maxVisibleListHeight: maxVisibleListHeight);
        var excludedSlotsListOptions = new GUIHelper.ListOptions(
            foldout: new GUIHelper.FoldoutOptions(Draw: false, RectStrict: true),
            maxVisibleListHeight: maxVisibleListHeight,
            middleContent: new GUIHelper.ListMiddleContentOptions(
                rect => MaterialSlotReferenceCollectionUI.DrawAddSlotsSelector(rect, excludedSlots, () => GetUsageSlots(materials, excludedSlots), "targetSettings.exclusions.selectUsageSlots".LS()),
                () => GUIHelper.propertyHeight));

        position.height = MaterialCollectionUI.GetHeight(materials, GUIContent.none, materialsListOptions);
        MaterialCollectionUI.Draw(position, materials, "targetSettings.materials.label".LG(), materialsListOptions);
        position.NewLine();
        position.SetSingleHeight();
        (var isExpanded, var isEnabled) = GUIHelper.FoldoutAndToggleLeft(position, useExclusions, "targetSettings.exclusions.useSlots".LG(), true);

        if (!isExpanded) return;
        position.Indent();

        var hasTargetMaterials = HasTargetMaterials(materials);

        if (!hasTargetMaterials)
        {
            position.NewLine();
            position.height = GUIHelper.GetHelpBoxHeight("editor.noMaterialSelected.help".LS(), MessageType.Warning);
            GUIHelper.HelpBox(position, "editor.noMaterialSelected.help".LS(), MessageType.Warning);
        }

        using (new EditorGUI.DisabledScope(!isEnabled || !hasTargetMaterials))
        {
            if (MaterialEditorSettings.ShowInspectorDescription)
            {
                position.NewLine();
                position.height = GUIHelper.GetHelpBoxHeight("targetSettings.exclusions.useSlots.help".LS(), MessageType.Info);
                GUIHelper.HelpBox(position, "targetSettings.exclusions.useSlots.help".LS(), MessageType.Info);
            }

            position.NewLine();
            position.SetSingleHeight();
            position.height = MaterialSlotReferenceCollectionUI.GetHeight(excludedSlots, GUIContent.none, excludedSlotsListOptions);
            MaterialSlotReferenceCollectionUI.Draw(position, excludedSlots, "targetSettings.exclusions.slots".LG(), excludedSlotsListOptions);
        }
    }

    private static bool HasTargetMaterials(SerializedProperty materialsProperty)
    {
        for (int i = 0; i < materialsProperty.arraySize; i++)
        {
            var material = materialsProperty.GetArrayElementAtIndex(i).objectReferenceValue as Material;
            if (material == null) continue;
            return true;
        }
        return false;
    }

    private static MaterialSlotReference[] GetUsageSlots(SerializedProperty materialsProperty, SerializedProperty excludedSlotsProperty)
    {
        if (!materialsProperty.TryGetGameObject(out var gameObject)) return Array.Empty<MaterialSlotReference>();
        var result = new List<MaterialSlotReference>();
        for (int i = 0; i < materialsProperty.arraySize; i++)
        {
            var material = materialsProperty.GetArrayElementAtIndex(i).objectReferenceValue as Material;
            if (material == null) continue;
            foreach (var slot in MaterialSlotReferenceCollectionUI.EnumerateMaterialUsages(gameObject, material))
            {
                if (MaterialSlotReferenceCollectionUI.ContainsSlot(excludedSlotsProperty, slot)) continue;
                result.Add(slot);
            }
        }
        return result.ToArray();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var materials = property.FindPropertyRelative(nameof(BulkMaterialTargetSettings.TargetMaterials));
        var useExclusions = property.FindPropertyRelative(nameof(BulkMaterialTargetSettings.UseSlotExclusions));
        var excludedSlots = property.FindPropertyRelative(nameof(BulkMaterialTargetSettings.ExcludedSlots));
        var maxVisibleListHeight = GUIHelper.DefaultScrollableListHeight;
        var materialsListOptions = new GUIHelper.ListOptions(
            foldout: new GUIHelper.FoldoutOptions(Draw: false),
            maxVisibleListHeight: maxVisibleListHeight);
        var excludedSlotsListOptions = new GUIHelper.ListOptions(
            foldout: new GUIHelper.FoldoutOptions(Draw: false, RectStrict: true),
            maxVisibleListHeight: maxVisibleListHeight,
            middleContent: new GUIHelper.ListMiddleContentOptions(_ => { }, () => GUIHelper.propertyHeight));

        var height = MaterialCollectionUI.GetHeight(materials, GUIContent.none, materialsListOptions);
        height += GUIHelper.GUI_SPACE + EditorGUI.GetPropertyHeight(useExclusions);
        if (useExclusions.isExpanded)
        {
            if (!HasTargetMaterials(materials))
            {
                height += GUIHelper.GUI_SPACE + GUIHelper.GetHelpBoxHeight("editor.noMaterialSelected.help".LS(), MessageType.Warning);
            }
            if (MaterialEditorSettings.ShowInspectorDescription)
            {
                height += GUIHelper.GUI_SPACE + GUIHelper.GetHelpBoxHeight("targetSettings.exclusions.useSlots.help".LS(), MessageType.Info);
            }
            height += GUIHelper.GUI_SPACE + MaterialSlotReferenceCollectionUI.GetHeight(excludedSlots, GUIContent.none, excludedSlotsListOptions);
        }
        return height;
    }
}
