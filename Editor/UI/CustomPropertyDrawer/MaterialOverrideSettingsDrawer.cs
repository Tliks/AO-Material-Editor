namespace Aoyon.MaterialEditor.UI;

[CustomPropertyDrawer(typeof(MaterialOverrideSettings))]
internal class MaterialOverrideSettingsDrawer : PropertyDrawer
{
    private const string DefaultHelpKey = "overrideSettings.help.visibleEditor";

    private static string _helpKey = DefaultHelpKey;
    private static GUIContent? _tooltipOverlayContent;
    private static GUIContent TooltipOverlayContent => _tooltipOverlayContent ??= new GUIContent("");

    public static IDisposable HelpKeyScope(string helpKey)
    {
        return new HelpKeyScopeImpl(helpKey);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        using var propertyScope = new EditorGUI.PropertyScope(position, label, property);

        var overrideShader = property.FindPropertyRelative(nameof(MaterialOverrideSettings.OverrideShader));
        var targetShader = property.FindPropertyRelative(nameof(MaterialOverrideSettings.TargetShader));
        var overrideRenderQueue = property.FindPropertyRelative(nameof(MaterialOverrideSettings.OverrideRenderQueue));
        var renderQueueValue = property.FindPropertyRelative(nameof(MaterialOverrideSettings.RenderQueueValue));
        var propertyOverrides = property.FindPropertyRelative(nameof(MaterialOverrideSettings.PropertyOverrides));

        position.SetSingleHeight();
        GUIHelper.SplitRectHorizontallyForRight(position, GetResetButtonWidth(), out var foldoutRect, out var resetRect);

        var overrideCount = 0;
        if (overrideShader.boolValue && targetShader.objectReferenceValue != null) overrideCount++;
        if (overrideRenderQueue.boolValue) overrideCount++;
        overrideCount += propertyOverrides.arraySize;

        label = new GUIContent(string.Format("overrideSettings.count".LS(), overrideCount));

        var isExpanded = GUIHelper.Foldout(foldoutRect, property, label);
        using (new EditorGUI.DisabledGroupScope(overrideCount == 0))
        {
            if (GUI.Button(resetRect, "overrideSettings.reset".LG()))
            {
                property.CopyFrom(MaterialOverrideSettings.Empty);
            }
        }
        if (!isExpanded) return;

        position.NewLine();
        // position.Indent();

        // var helpBoxRect = position;
        // helpBoxRect.height = GetInnerHeight(property);
        // helpBoxRect = new RectOffset(3, 3, 3, 3).Add(helpBoxRect);
        // EditorGUI.LabelField(helpBoxRect, GUIContent.none, EditorStyles.helpBox);

        if (MaterialEditorSettings.ShowInspectorDescription)
        {
            position = GUIHelper.HelpBox(position, _helpKey.LS(), MessageType.Info);
        }

        var component = property.serializedObject.targetObject as MaterialEditorComponent;
        var shaderLocked = component != null
            && MaterialEditoEditorContext.ComponentToShaderLocked.TryGetValue(component, out var isShaderLocked)
            && isShaderLocked;
        var renderQueueLocked = component != null
            && MaterialEditoEditorContext.ComponentToRenderQueueLocked.TryGetValue(component, out var isRenderQueueLocked)
            && isRenderQueueLocked;

        var (isExpandedShader, isShaderEnabled) = GUIHelper.FoldoutAndToggleLeft(position, overrideShader, "common.shader".LG(), rectStrict: true);
        position.NewLine();
        if (isExpandedShader)
        {
            position.Indent();
            var shaderScopePosition = position;
            using (new EditorGUI.DisabledGroupScope(shaderLocked || !isShaderEnabled))
            {
                EditorGUI.PropertyField(position, targetShader, GUIContent.none);
                position.NewLine();
            }
            if (shaderLocked)
            {
                DrawTooltipOverlay(shaderScopePosition, position, "lock.shader.tooltip".LS());
            }
            position.Back();
        }

        var (isExpandedRenderQueue, isRenderQueueEnabled) = GUIHelper.FoldoutAndToggleLeft(position, overrideRenderQueue, "common.renderQueue".LG(), rectStrict: true);
        position.NewLine();
        if (isExpandedRenderQueue)
        {
            position.Indent();
            var renderQueueScopePosition = position;
            using (new EditorGUI.DisabledGroupScope(renderQueueLocked || !isRenderQueueEnabled))
            {
                DrawRenderQueueGUI(position, renderQueueValue);
                position.NewLine();
            }
            if (renderQueueLocked)
            {
                DrawTooltipOverlay(renderQueueScopePosition, position, "lock.renderQueue.tooltip".LS());
            }
            position.Back();
        }

        var propertyOverridesListOptions = new GUIHelper.ListOptions(foldout: new(RectStrict: true), nest: true);
        GUIHelper.List(position, propertyOverrides, "overrideSettings.properties".LG(), propertyOverridesListOptions, prop => prop.CopyFrom(new MaterialProperty()));
    }

    private static readonly GUIContent[] _renderQueuePresets = new GUIContent[] { new("From Shader"), new("Custom") };
    private void DrawRenderQueueGUI(Rect position, SerializedProperty renderQueueValue)
    {
        var presetWidth = EditorStyles.popup.CalcSize(_renderQueuePresets[0]).x;
        GUIHelper.SplitRectHorizontallyForRight(position, presetWidth, out var valueRect, out var presetRect);

        EditorGUI.PropertyField(valueRect, renderQueueValue, GUIContent.none);

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
            height += GUIHelper.GetHelpBoxHeight(_helpKey.LS(), MessageType.Info);
        }

        var overrideShader = property.FindPropertyRelative(nameof(MaterialOverrideSettings.OverrideShader));
        var overrideRenderQueue = property.FindPropertyRelative(nameof(MaterialOverrideSettings.OverrideRenderQueue));
        var propertyOverrides = property.FindPropertyRelative(nameof(MaterialOverrideSettings.PropertyOverrides));
        var propertyOverridesListOptions = new GUIHelper.ListOptions(foldout: new(RectStrict: true));

        height += GUIHelper.propertyHeight + GUIHelper.GUI_SPACE;
        if (overrideShader.isExpanded)
        {
            height += GUIHelper.propertyHeight + GUIHelper.GUI_SPACE;
        }
        height += GUIHelper.propertyHeight + GUIHelper.GUI_SPACE;
        if (overrideRenderQueue.isExpanded)
        {
            height += GUIHelper.propertyHeight + GUIHelper.GUI_SPACE;
        }
        height += GUIHelper.GetListHeight(propertyOverrides, propertyOverridesListOptions);

        return height;
    }

    private static float GetResetButtonWidth()
    {
        return EditorStyles.miniButton.CalcSize("overrideSettings.reset".LG()).x + 8f;
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

    private sealed class HelpKeyScopeImpl : IDisposable
    {
        private readonly string _previousHelpKey;

        public HelpKeyScopeImpl(string helpKey)
        {
            _previousHelpKey = _helpKey;
            _helpKey = helpKey;
        }

        public void Dispose()
        {
            _helpKey = _previousHelpKey;
        }
    }
}
