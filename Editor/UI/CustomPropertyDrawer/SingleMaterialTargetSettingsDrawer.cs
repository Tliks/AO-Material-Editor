namespace Aoyon.MaterialEditor.UI;

[CustomPropertyDrawer(typeof(SingleMaterialTargetSettings))]
internal class SingleMaterialTargetSettingsDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var material = property.FindPropertyRelative(nameof(SingleMaterialTargetSettings.TargetMaterial));
        var useExclusions = property.FindPropertyRelative(nameof(SingleMaterialTargetSettings.UseSlotExclusions));
        var excludedSlots = property.FindPropertyRelative(nameof(SingleMaterialTargetSettings.ExcludedSlots));
        var excludedSlotsListOptions = new GUIHelper.ListOptions(
            foldout: new GUIHelper.FoldoutOptions(
                Draw: false,
                RectStrict: true),
            maxVisibleListHeight: GUIHelper.DefaultScrollableListHeight,
            middleContent: new GUIHelper.ListMiddleContentOptions(
                rect => MaterialSlotReferenceCollectionUI.DrawAddSlotsSelector(rect, excludedSlots, () => GetUsageSlots(material, excludedSlots), "targetSettings.exclusions.selectUsageSlots".LS()),
                () => GUIHelper.propertyHeight));

        position.SetSingleHeight();
        LocalizedUI.PropertyField(position, material, "targetSettings.material.label");
        position.NewLine();
        (var isExpanded, var isEnabled) = GUIHelper.FoldoutAndToggleLeft(position, useExclusions, "targetSettings.exclusions.useSlots".LG(), true);

        if (!isExpanded) return;
        position.Indent();

        var hasTargetMaterial = material.objectReferenceValue != null;

        if (!hasTargetMaterial)
        {
            position.NewLine();
            position.height = GUIHelper.GetHelpBoxHeight("editor.noMaterialSelected.help".LS(), MessageType.Warning);
            GUIHelper.HelpBox(position, "editor.noMaterialSelected.help".LS(), MessageType.Warning);
        }

        using (new EditorGUI.DisabledScope(!isEnabled || !hasTargetMaterial))
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

    private static MaterialSlotReference[] GetUsageSlots(SerializedProperty materialProperty, SerializedProperty excludedSlotsProperty)
    {
        if (!materialProperty.TryGetGameObject(out var gameObject)) return Array.Empty<MaterialSlotReference>();
        if (materialProperty.objectReferenceValue is not Material material) return Array.Empty<MaterialSlotReference>();
        return MaterialSlotReferenceCollectionUI.EnumerateMaterialUsages(gameObject, material)
            .Where(slot => !MaterialSlotReferenceCollectionUI.ContainsSlot(excludedSlotsProperty, slot))
            .ToArray();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var material = property.FindPropertyRelative(nameof(SingleMaterialTargetSettings.TargetMaterial));
        var useExclusions = property.FindPropertyRelative(nameof(SingleMaterialTargetSettings.UseSlotExclusions));
        var excludedSlots = property.FindPropertyRelative(nameof(SingleMaterialTargetSettings.ExcludedSlots));
        var excludedSlotsListOptions = new GUIHelper.ListOptions(
            foldout: new GUIHelper.FoldoutOptions(
                Draw: false,
                RectStrict: true),
            maxVisibleListHeight: GUIHelper.DefaultScrollableListHeight,
            middleContent: new GUIHelper.ListMiddleContentOptions(_ => { }, () => GUIHelper.propertyHeight));

        var height = EditorGUI.GetPropertyHeight(material);
        height += GUIHelper.GUI_SPACE + EditorGUI.GetPropertyHeight(useExclusions);
        if (useExclusions.isExpanded)
        {
            if (material.objectReferenceValue == null)
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
