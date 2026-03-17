namespace Aoyon.MaterialEditor.UI;

[CustomPropertyDrawer(typeof(MaterialOverrideSettings))]
internal class MaterialOverrideSettingsDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // using var _ = new EditorGUI.PropertyScope(position, GUIContent.none, property);

        position.SetSingleHeight();

        var isExpanded = GUIHelper.Foldout(position, property, label);
        if (!isExpanded) return;

        position.NewLine();
        position.Indent();

        position = GUIHelper.HelpBox(position, "HelpBox:OverridesInfo", MessageType.Info);
        
        var overrideShader = property.FindPropertyRelative(nameof(MaterialOverrideSettings.OverrideShader));
        var targetShader = property.FindPropertyRelative(nameof(MaterialOverrideSettings.TargetShader));
        var overrideRenderQueue = property.FindPropertyRelative(nameof(MaterialOverrideSettings.OverrideRenderQueue));
        var renderQueueValue = property.FindPropertyRelative(nameof(MaterialOverrideSettings.RenderQueueValue));
        var propertyOverrides = property.FindPropertyRelative(nameof(MaterialOverrideSettings.PropertyOverrides));

        var isExpandedShader = GUIHelper.Foldout(position, overrideShader, "Label:Shader".LG());
        position.NewLine();
        if (isExpandedShader)
        {
            position.Indent();
            LocalizedUI.PropertyField(position, overrideShader, "Label:Edit");
            position.NewLine();
            LocalizedUI.PropertyField(position, targetShader, "Label:Shader");
            position.NewLine();
            position.Back();
        }

        var isExpandedRenderQueue = GUIHelper.Foldout(position, overrideRenderQueue, "Label:RenderQueue".LG());
        position.NewLine();
        if (isExpandedRenderQueue)
        {
            position.Indent();
            LocalizedUI.PropertyField(position, overrideRenderQueue, "Label:Edit");
            position.NewLine();
            DrawRenderQueueGUI(position, renderQueueValue);
            position.NewLine();
            position.Back();
        }

        GUIHelper.List(position, propertyOverrides, true, "Label:PropertyOverrides".LG(), prop => prop.CopyFrom(new MaterialProperty()));
    }

    private static readonly GUIContent[] _renderQueuePresets = new GUIContent[] { new("From Shader"), new("Custom") };
    private void DrawRenderQueueGUI(Rect position, SerializedProperty renderQueueValue)
    {
        var presetWidth = EditorStyles.popup.CalcSize(_renderQueuePresets[0]).x;
        GUIHelper.SplitRectHorizontallyForRight(position, presetWidth, out var valueRect, out var presetRect);

        LocalizedUI.PropertyField(valueRect, renderQueueValue, "Label:RenderQueue");

        var index = renderQueueValue.intValue == -1 ? 0 : 1;
        var newIndex = EditorGUI.Popup(presetRect, index, _renderQueuePresets);
        if (newIndex != index) renderQueueValue.intValue = newIndex == 0 ? -1 : 2000;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var height = 0f;
        height += GUIHelper.propertyHeight;

        if (!property.isExpanded) return height;

        height += GUIHelper.GetHelpBoxHeight("HelpBox:OverridesInfo", MessageType.Info);

        var overrideShader = property.FindPropertyRelative(nameof(MaterialOverrideSettings.OverrideShader));
        var overrideRenderQueue = property.FindPropertyRelative(nameof(MaterialOverrideSettings.OverrideRenderQueue));
        var propertyOverrides = property.FindPropertyRelative(nameof(MaterialOverrideSettings.PropertyOverrides));

        height += GUIHelper.propertyHeight + GUIHelper.GUI_SPACE;
        if (overrideShader.isExpanded)
        {
            height += (GUIHelper.propertyHeight + GUIHelper.GUI_SPACE) * 2;
        }
        height += GUIHelper.propertyHeight + GUIHelper.GUI_SPACE;
        if (overrideRenderQueue.isExpanded)
        {
            height += (GUIHelper.propertyHeight + GUIHelper.GUI_SPACE) * 2;
        }
        height += GUIHelper.GetListHeight(propertyOverrides);
        return height;
    }
}
