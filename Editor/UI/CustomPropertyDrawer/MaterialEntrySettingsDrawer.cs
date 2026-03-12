namespace Aoyon.MaterialEditor.UI;

[CustomPropertyDrawer(typeof(MaterialEntrySettings))]
internal class MaterialEntrySettingsDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        position.SetSingleHeight();

        var mode = property.FindPropertyRelative(nameof(MaterialEntrySettings.Mode));
        LocalizedToolbar.Field(position, mode, LocalizedUI.GetEnumOptionKeys(typeof(MaterialEntrySettings.ApplyMode)), "Label:ApplyMode");

        position.NewLine();

        switch ((MaterialEntrySettings.ApplyMode)mode.enumValueIndex)
        {
            case MaterialEntrySettings.ApplyMode.Basic:
                var basicMaterial = property.FindPropertyRelative(nameof(MaterialEntrySettings.BasicMaterial));
                LocalizedUI.PropertyField(position, basicMaterial, "Label:Material");
                break;
            case MaterialEntrySettings.ApplyMode.Advanced:
                position.height = EditorStyles.helpBox.CalcHeight("HelpBox:AdvancedModeInfo".LG(), position.width);;
                EditorGUI.HelpBox(position, "HelpBox:AdvancedModeInfo".LS(), MessageType.Info);
                position.NewLineWithSingleHeight();
                var advancedTargets = property.FindPropertyRelative(nameof(MaterialEntrySettings.AdvancedTargets));
                GUIHelper.List(position, advancedTargets, true, "Label:Material".LG(), prop => {
                    var beforeType = prop.FindPropertyRelative(nameof(MaterialTargetScope.Type)).enumValueIndex == 0 
                        ? MaterialTargetScope.ScopeType.Asset : MaterialTargetScope.ScopeType.Slot;
                    prop.CopyFrom(new MaterialTargetScope() { Type = beforeType });
                });
                break;
            case MaterialEntrySettings.ApplyMode.All:
                position.height = EditorStyles.helpBox.CalcHeight("HelpBox:AllModeInfo".LG(), position.width);;
                EditorGUI.HelpBox(position, "HelpBox:AllModeInfo".LS(), MessageType.Info);
                position.NewLineWithSingleHeight();
                var allMaterialTargetScope = property.FindPropertyRelative(nameof(MaterialEntrySettings.AllMaterialTargetScope));
                EditorGUI.PropertyField(position, allMaterialTargetScope);
                break;
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var height = 0f;

        var mode = property.FindPropertyRelative(nameof(MaterialEntrySettings.Mode));
        height += EditorGUI.GetPropertyHeight(mode, GUIContent.none);

        height += GUIHelper.GUI_SPACE;

        var viewWidth = EditorGUIUtility.currentViewWidth - 20f;
        switch ((MaterialEntrySettings.ApplyMode)mode.enumValueIndex)
        {
            case MaterialEntrySettings.ApplyMode.Basic:
                var basicMaterial = property.FindPropertyRelative(nameof(MaterialEntrySettings.BasicMaterial));
                height += EditorGUI.GetPropertyHeight(basicMaterial, GUIContent.none);
                break;
            case MaterialEntrySettings.ApplyMode.Advanced:
                height += EditorStyles.helpBox.CalcHeight("HelpBox:AdvancedModeInfo".LG(), viewWidth) + GUIHelper.GUI_SPACE;
                var advancedTargets = property.FindPropertyRelative(nameof(MaterialEntrySettings.AdvancedTargets));
                height += GUIHelper.GetListHeight(advancedTargets);
                break;
            case MaterialEntrySettings.ApplyMode.All:
                height += EditorStyles.helpBox.CalcHeight("HelpBox:AllModeInfo".LG(), viewWidth) + GUIHelper.GUI_SPACE;
                var allMaterialTargetScope = property.FindPropertyRelative(nameof(MaterialEntrySettings.AllMaterialTargetScope));
                height += EditorGUI.GetPropertyHeight(allMaterialTargetScope, GUIContent.none);
                break;
        }
        return height;
    }
}