namespace Aoyon.MaterialEditor.UI;

[CustomPropertyDrawer(typeof(MaterialOverrideSettings))]
internal class MaterialOverrideSettingsDrawer : PropertyDrawer
{
    private static GUIContent? _tooltipOverlayContent;
    private static GUIContent TooltipOverlayContent => _tooltipOverlayContent ??= new GUIContent("");

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // using var _ = new EditorGUI.PropertyScope(position, GUIContent.none, property);

        position.SetSingleHeight();

        var isExpanded = GUIHelper.Foldout(position, property, label);
        if (!isExpanded) return;

        position.NewLine();
        // position.Indent();

        var helpBoxRect = position;
        helpBoxRect.height = GetInnerHeight(property);
        helpBoxRect = new RectOffset(3, 3, 3, 3).Add(helpBoxRect);
        EditorGUI.LabelField(helpBoxRect, GUIContent.none, EditorStyles.helpBox);

        if (MaterialEditorSettings.ShowInspectorDescription)
        {
            position = GUIHelper.HelpBox(position, "overrideSettings.help".LS(), MessageType.Info);
        }
        if (GUI.Button(position, "overrideSettings.reset".LS()))
        {
            property.CopyFrom(MaterialOverrideSettings.Empty);
        }
        position.NewLine();
        position.Indent();

        var overrideShader = property.FindPropertyRelative(nameof(MaterialOverrideSettings.OverrideShader));
        var targetShader = property.FindPropertyRelative(nameof(MaterialOverrideSettings.TargetShader));
        var overrideRenderQueue = property.FindPropertyRelative(nameof(MaterialOverrideSettings.OverrideRenderQueue));
        var renderQueueValue = property.FindPropertyRelative(nameof(MaterialOverrideSettings.RenderQueueValue));
        var propertyOverrides = property.FindPropertyRelative(nameof(MaterialOverrideSettings.PropertyOverrides));
        var propertyOverridesListOptions = new GUIHelper.ListOptions(nest: false);

        var component = property.serializedObject.targetObject as MaterialEditorComponent;
        var shaderLocked = component != null
            && MaterialEditoEditorContext.ComponentToShaderLocked.TryGetValue(component, out var isShaderLocked)
            && isShaderLocked;
        var renderQueueLocked = component != null
            && MaterialEditoEditorContext.ComponentToRenderQueueLocked.TryGetValue(component, out var isRenderQueueLocked)
            && isRenderQueueLocked;

        var isExpandedShader = GUIHelper.Foldout(position, overrideShader, "common.shader".LG());
        position.NewLine();
        if (isExpandedShader)
        {
            position.Indent();
            var shaderScopePosition = position;
            using (new EditorGUI.DisabledGroupScope(shaderLocked))
            {
                LocalizedUI.PropertyField(position, overrideShader, "common.edit");
                position.NewLine();
                LocalizedUI.PropertyField(position, targetShader, "common.shader");
                position.NewLine();
            }
            if (shaderLocked)
            {
                DrawTooltipOverlay(shaderScopePosition, position, "lock.shader.tooltip".LS());
            }
            position.Back();
        }

        var isExpandedRenderQueue = GUIHelper.Foldout(position, overrideRenderQueue, "common.renderQueue".LG());
        position.NewLine();
        if (isExpandedRenderQueue)
        {
            position.Indent();
            var renderQueueScopePosition = position;
            using (new EditorGUI.DisabledGroupScope(renderQueueLocked))
            {
                LocalizedUI.PropertyField(position, overrideRenderQueue, "common.edit");
                position.NewLine();
                DrawRenderQueueGUI(position, renderQueueValue);
                position.NewLine();
            }
            if (renderQueueLocked)
            {
                DrawTooltipOverlay(renderQueueScopePosition, position, "lock.renderQueue.tooltip".LS());
            }
            position.Back();
        }

        GUIHelper.List(position, propertyOverrides, "overrideSettings.properties".LG(), propertyOverridesListOptions, prop => prop.CopyFrom(new MaterialProperty()));
    }

    private static readonly GUIContent[] _renderQueuePresets = new GUIContent[] { new("From Shader"), new("Custom") };
    private void DrawRenderQueueGUI(Rect position, SerializedProperty renderQueueValue)
    {
        var presetWidth = EditorStyles.popup.CalcSize(_renderQueuePresets[0]).x;
        GUIHelper.SplitRectHorizontallyForRight(position, presetWidth, out var valueRect, out var presetRect);

        LocalizedUI.PropertyField(valueRect, renderQueueValue, "common.renderQueue");

        var index = renderQueueValue.intValue == -1 ? 0 : 1;
        var newIndex = EditorGUI.Popup(presetRect, index, _renderQueuePresets);
        if (newIndex != index) renderQueueValue.intValue = newIndex == 0 ? -1 : 2000;
    }

    private static void DrawTooltipOverlay(Rect startPosition, Rect endPosition, string tooltip)
    {
        var rect = new Rect(
            startPosition.xMin,
            startPosition.yMin,
            startPosition.width,
            Mathf.Max(0f, endPosition.yMin - startPosition.yMin));

        TooltipOverlayContent.tooltip = tooltip;
        GUI.Label(rect, TooltipOverlayContent, GUIStyle.none);
    }

    private float GetInnerHeight(SerializedProperty property)
    {
        var height = 0f;

        if (MaterialEditorSettings.ShowInspectorDescription)
        {
            height += GUIHelper.GUI_SPACE;
            height += GUIHelper.GetHelpBoxHeight("overrideSettings.help".LS(), MessageType.Info);
        }
        height += GUIHelper.GUI_SPACE + GUIHelper.propertyHeight;

        var overrideShader = property.FindPropertyRelative(nameof(MaterialOverrideSettings.OverrideShader));
        var overrideRenderQueue = property.FindPropertyRelative(nameof(MaterialOverrideSettings.OverrideRenderQueue));
        var propertyOverrides = property.FindPropertyRelative(nameof(MaterialOverrideSettings.PropertyOverrides));
        var propertyOverridesListOptions = new GUIHelper.ListOptions();

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
        height += GUIHelper.GetListHeight(propertyOverrides, propertyOverridesListOptions);

        return height;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var height = 0f;
        height += GUIHelper.propertyHeight;

        if (!property.isExpanded) return height;

        height += GetInnerHeight(property);

        height += GUIHelper.GUI_SPACE * 2; // 背景の為にスペース多め
        return height;
    }
}
