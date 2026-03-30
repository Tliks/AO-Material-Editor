namespace Aoyon.MaterialEditor.UI;

[CustomPropertyDrawer(typeof(AllMaterialSettings))]
internal class AllMaterialSettingsDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var useExclusions = property.FindPropertyRelative(nameof(AllMaterialSettings.UseExclusions));
        var excludedMaterials = property.FindPropertyRelative(nameof(AllMaterialSettings.ExcludedMaterials));
        var excludedSlots = property.FindPropertyRelative(nameof(AllMaterialSettings.ExcludedSlots));
        var excludedObjects = property.FindPropertyRelative(nameof(AllMaterialSettings.ExcludedObjects));

        position.SetSingleHeight();
        (var isExpanded, var isEnabled) = GUIHelper.FoldoutAndToggleLeft(position, useExclusions, "targetSettings.exclusions.use".LG(), true);
        if (!isExpanded) return;

        using (new EditorGUI.DisabledScope(!isEnabled))
        {
            position.Indent();

            var maxVisibleListHeight = GUIHelper.DefaultScrollableListHeight;
            var listOptions = new GUIHelper.ListOptions(
                foldout: new GUIHelper.FoldoutOptions(RectStrict: true),
                maxVisibleListHeight: maxVisibleListHeight);

            position.NewLine();
            position.height = MaterialCollectionUI.GetHeight(excludedMaterials, GUIContent.none, listOptions);
            MaterialCollectionUI.Draw(position, excludedMaterials, "targetSettings.exclusions.materials".LG(), listOptions);
            position.NewLine();
            position.height = MaterialSlotReferenceCollectionUI.GetHeight(excludedSlots, GUIContent.none, listOptions);
            MaterialSlotReferenceCollectionUI.Draw(position, excludedSlots, "targetSettings.exclusions.slots".LG(), listOptions);
            position.NewLine();
            position.height = AvatarObjectReferenceCollectionUI.GetHeight(excludedObjects, GUIContent.none, listOptions);
            AvatarObjectReferenceCollectionUI.Draw(position, excludedObjects, "targetSettings.exclusions.objects".LG(), listOptions);
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var useExclusions = property.FindPropertyRelative(nameof(AllMaterialSettings.UseExclusions));
        var excludedMaterials = property.FindPropertyRelative(nameof(AllMaterialSettings.ExcludedMaterials));
        var excludedSlots = property.FindPropertyRelative(nameof(AllMaterialSettings.ExcludedSlots));
        var excludedObjects = property.FindPropertyRelative(nameof(AllMaterialSettings.ExcludedObjects));

        var height = GUIHelper.propertyHeight;
        if (!useExclusions.isExpanded) return height;

        var maxVisibleListHeight = GUIHelper.DefaultScrollableListHeight;
        var listOptions = new GUIHelper.ListOptions(
            foldout: new GUIHelper.FoldoutOptions(RectStrict: true),
            maxVisibleListHeight: maxVisibleListHeight);
        height += GUIHelper.GUI_SPACE + MaterialCollectionUI.GetHeight(excludedMaterials, GUIContent.none, listOptions);
        height += GUIHelper.GUI_SPACE + MaterialSlotReferenceCollectionUI.GetHeight(excludedSlots, GUIContent.none, listOptions);
        height += GUIHelper.GUI_SPACE + AvatarObjectReferenceCollectionUI.GetHeight(excludedObjects, GUIContent.none, listOptions);
        return height;
    }
}
