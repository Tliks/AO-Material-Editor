namespace Aoyon.MaterialEditor.UI;

[CustomPropertyDrawer(typeof(MaterialOverrideSettings))]
internal class MaterialOverrideSettingsDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        position.SetSingleHeight();

        var overrideShader = property.FindPropertyRelative(nameof(MaterialOverrideSettings.OverrideShader));
        var targetShader = property.FindPropertyRelative(nameof(MaterialOverrideSettings.TargetShader));
        var overrideRenderQueue = property.FindPropertyRelative(nameof(MaterialOverrideSettings.OverrideRenderQueue));
        var renderQueueValue = property.FindPropertyRelative(nameof(MaterialOverrideSettings.RenderQueueValue));
        var propertyOverrides = property.FindPropertyRelative(nameof(MaterialOverrideSettings.PropertyOverrides));

        overrideShader.isExpanded = EditorGUI.Foldout(position, overrideShader.isExpanded, "Label:Shader".LS(), true);
        position.NewLine();
        if (overrideShader.isExpanded)
        {
            using var indent = new EditorGUI.IndentLevelScope();
            LocalizedUI.PropertyField(position, overrideShader, "Label:Edit");
            position.NewLine();
            LocalizedUI.PropertyField(position, targetShader, "Label:Shader");
            position.NewLine();
        }

        overrideRenderQueue.isExpanded = EditorGUI.Foldout(position, overrideRenderQueue.isExpanded, "Label:RenderQueue".LS(), true);
        position.NewLine();
        if (overrideRenderQueue.isExpanded)
        {
            using var indent = new EditorGUI.IndentLevelScope();
            LocalizedUI.PropertyField(position, overrideRenderQueue, "Label:Edit");
            position.NewLine();
            LocalizedUI.PropertyField(position, renderQueueValue, "Label:RenderQueue");
            position.NewLine();
        }

        GUIHelper.List(position, propertyOverrides, true, "Label:PropertyOverrides".LG(), prop => prop.CopyFrom(new MaterialProperty()));
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var overrideShader = property.FindPropertyRelative(nameof(MaterialOverrideSettings.OverrideShader));
        var overrideRenderQueue = property.FindPropertyRelative(nameof(MaterialOverrideSettings.OverrideRenderQueue));
        var propertyOverrides = property.FindPropertyRelative(nameof(MaterialOverrideSettings.PropertyOverrides));

        var height = 0f;
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
