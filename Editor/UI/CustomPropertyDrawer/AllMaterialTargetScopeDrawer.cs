namespace Aoyon.MaterialEditor.UI;

[CustomPropertyDrawer(typeof(AllMaterialTargetScope))]
internal class AllMaterialTargetScopeDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        var isExpanded = GUIHelper.Foldout(position, property, "Label:ExcludeTargets".LG());
        if (!isExpanded) return;

        var excludeTargets = property.FindPropertyRelative(nameof(AllMaterialTargetScope.ExcludeTargets));
        var excludeObjectReferences = property.FindPropertyRelative(nameof(AllMaterialTargetScope.ExcludeObjectReferences));

        position.NewLine();
        position.Indent();
        
        position.height = MaterialTargetScopeCollectionUI.GetHeight(excludeTargets, GUIContent.none);
        MaterialTargetScopeCollectionUI.Draw(position, excludeTargets, "Label:Material".LG());
        position.NewLine();
        position.height = AvatarObjectReferenceCollectionUI.GetHeight(excludeObjectReferences, GUIContent.none);
        AvatarObjectReferenceCollectionUI.Draw(position, excludeObjectReferences, "Label:ExcludeObjectReferences".LG());
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var height = GUIHelper.propertyHeight;

        if (property.isExpanded)
        {
            height += GUIHelper.GUI_SPACE;

            var excludeTargets = property.FindPropertyRelative(nameof(AllMaterialTargetScope.ExcludeTargets));
            var excludeObjectReferences = property.FindPropertyRelative(nameof(AllMaterialTargetScope.ExcludeObjectReferences));

            height += MaterialTargetScopeCollectionUI.GetHeight(excludeTargets, GUIContent.none);
            height += GUIHelper.GUI_SPACE;
            height += AvatarObjectReferenceCollectionUI.GetHeight(excludeObjectReferences, GUIContent.none);
        }

        return height;
    }
}