using nadena.dev.modular_avatar.core;

namespace Aoyon.MaterialEditor.UI;

[CustomPropertyDrawer(typeof(AllMaterialTargetScope))]
internal class AllMaterialTargetScopeDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        position.SetSingleHeight();

        var isExpanded = GUIHelper.Foldout(position, property, "Label:ExcludeTargets".LG());
        if (!isExpanded) return;

        var excludeTargets = property.FindPropertyRelative(nameof(AllMaterialTargetScope.ExcludeTargets));
        var excludeObjectReferences = property.FindPropertyRelative(nameof(AllMaterialTargetScope.ExcludeObjectReferences));

        position.NewLine();
        position.Indent();

        position = GUIHelper.List(position, excludeTargets, true, "Label:Material".LG(), prop => prop.CopyFrom(new MaterialTargetScope()));
        position = GUIHelper.List(position, excludeObjectReferences, true, "Label:ExcludeObjectReferences".LG(), prop => prop.CopyFrom(new AvatarObjectReference()));
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var height = GUIHelper.propertyHeight;

        if (property.isExpanded)
        {
            height += GUIHelper.GUI_SPACE;

            var excludeTargets = property.FindPropertyRelative(nameof(AllMaterialTargetScope.ExcludeTargets));
            var excludeObjectReferences = property.FindPropertyRelative(nameof(AllMaterialTargetScope.ExcludeObjectReferences));

            height += GUIHelper.GetListHeight(excludeTargets, true);
            height += GUIHelper.GUI_SPACE;
            height += GUIHelper.GetListHeight(excludeObjectReferences, true);
        }

        return height;
    }
}