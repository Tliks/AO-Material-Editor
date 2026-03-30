namespace Aoyon.MaterialEditor.UI;

[CustomPropertyDrawer(typeof(MaterialTargetSettings))]
internal class MaterialTargetSettingsDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        position.SetSingleHeight();

        using (new EditorGUI.PropertyScope(position, label, property))
        {
            EditorGUI.LabelField(position, label, EditorStyles.boldLabel);
            position.NewLine();
        }

        position.y += GUIHelper.GUI_SPACE;
        
        var helpBoxRect = position;
        helpBoxRect.height = GetInnerHeight(property);
        helpBoxRect = new RectOffset(5, 3, 5, 5).Add(helpBoxRect);
        EditorGUI.LabelField(helpBoxRect, GUIContent.none, EditorStyles.helpBox);

        var mode = property.FindPropertyRelative(nameof(MaterialTargetSettings.Mode));
        LocalizedPopup.Field(position, mode, "targetSettings.mode.label", LocalizedUI.GetEnumOptionKeys("targetSettings.mode", typeof(MaterialTargetSettings.SelectionMode)));
        position.NewLine();

        var selectionMode = (MaterialTargetSettings.SelectionMode)mode.enumValueIndex;
        if (ShouldShowHelpBox(selectionMode))
        {
            position = GUIHelper.HelpBox(position, GetHelpKey(selectionMode).LS(), MessageType.Info);
        }

        var settingsProperty = GetModeSettingsProperty(property, selectionMode);
        if (settingsProperty == null) return;

        position.height = EditorGUI.GetPropertyHeight(settingsProperty, includeChildren: true);
        EditorGUI.PropertyField(position, settingsProperty, GUIContent.none, includeChildren: true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var height = GUIHelper.propertyHeight;
        height += GUIHelper.GUI_SPACE;
        height += GetInnerHeight(property);
        return height;
    }

    private static float GetInnerHeight(SerializedProperty property)
    {
        var height = 0f;
        var mode = property.FindPropertyRelative(nameof(MaterialTargetSettings.Mode));
        height += EditorGUI.GetPropertyHeight(mode);
        var selectionMode = (MaterialTargetSettings.SelectionMode)mode.enumValueIndex;
        if (ShouldShowHelpBox(selectionMode))
        {
            height += GUIHelper.GUI_SPACE;
            height += GUIHelper.GetHelpBoxHeight(GetHelpKey(selectionMode).LS(), MessageType.Info);
        }

        var settingsProperty = GetModeSettingsProperty(property, selectionMode);
        if (settingsProperty != null)
        {
            height += GUIHelper.GUI_SPACE;
            height += EditorGUI.GetPropertyHeight(settingsProperty, includeChildren: true);
        }
        return height;
    }

    private static SerializedProperty? GetModeSettingsProperty(SerializedProperty property, MaterialTargetSettings.SelectionMode mode)
    {
        return mode switch
        {
            MaterialTargetSettings.SelectionMode.SingleMaterial => property.FindPropertyRelative(nameof(MaterialTargetSettings.SingleMaterial)),
            MaterialTargetSettings.SelectionMode.BulkMaterials => property.FindPropertyRelative(nameof(MaterialTargetSettings.BulkMaterials)),
            MaterialTargetSettings.SelectionMode.SlotTargets => property.FindPropertyRelative(nameof(MaterialTargetSettings.SlotTargets)),
            MaterialTargetSettings.SelectionMode.AllMaterials => property.FindPropertyRelative(nameof(MaterialTargetSettings.AllMaterials)),
            _ => null,
        };
    }

    private static bool ShouldShowHelpBox(MaterialTargetSettings.SelectionMode mode)
    {
        return MaterialEditorSettings.ShowInspectorDescription && (
            mode == MaterialTargetSettings.SelectionMode.BulkMaterials
            || mode == MaterialTargetSettings.SelectionMode.SlotTargets
            || mode == MaterialTargetSettings.SelectionMode.AllMaterials);
    }

    private static string GetHelpKey(MaterialTargetSettings.SelectionMode mode)
    {
        return mode switch
        {
            MaterialTargetSettings.SelectionMode.SingleMaterial => "targetSettings.mode.singleMaterial.help",
            MaterialTargetSettings.SelectionMode.BulkMaterials => "targetSettings.mode.bulkMaterials.help",
            MaterialTargetSettings.SelectionMode.SlotTargets => "targetSettings.mode.slotTargets.help",
            MaterialTargetSettings.SelectionMode.AllMaterials => "targetSettings.mode.allMaterials.help",
            _ => "targetSettings.mode.singleMaterial.help",
        };
    }
}
