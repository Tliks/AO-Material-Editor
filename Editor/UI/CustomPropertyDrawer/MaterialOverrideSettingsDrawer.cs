namespace Aoyon.MaterialEditor.UI;

[CustomPropertyDrawer(typeof(MaterialOverrideSettings))]
internal class MaterialOverrideSettingsDrawer : PropertyDrawer
{
    private const string DefaultHelpKey = "overrideSettings.help.visibleEditor";
    private const string AdvancedSettingsLabelKey = "overrideSettings.advancedSettings";

    private static string _helpKey = DefaultHelpKey;
    private static readonly HashSet<string> _advancedSettingsExpandedKeys = new();
    private static readonly GUIHelper.FoldoutOptions RectStrictFoldoutOptions = new(RectStrict: true);
    private static readonly GUIHelper.ListOptions OverrideListOptions = new(foldout: RectStrictFoldoutOptions, nest: true);

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
        var overrideLightmapFlags = property.FindPropertyRelative(nameof(MaterialOverrideSettings.OverrideLightmapFlags));
        var lightmapFlagsValue = property.FindPropertyRelative(nameof(MaterialOverrideSettings.LightmapFlagsValue));
        var overrideEnableInstancing = property.FindPropertyRelative(nameof(MaterialOverrideSettings.OverrideEnableInstancing));
        var enableInstancingValue = property.FindPropertyRelative(nameof(MaterialOverrideSettings.EnableInstancingValue));
        var overrideDoubleSidedGI = property.FindPropertyRelative(nameof(MaterialOverrideSettings.OverrideDoubleSidedGI));
        var doubleSidedGIValue = property.FindPropertyRelative(nameof(MaterialOverrideSettings.DoubleSidedGIValue));
        var propertyOverrides = property.FindPropertyRelative(nameof(MaterialOverrideSettings.PropertyOverrides));
        var keywordStateOverrides = property.FindPropertyRelative(nameof(MaterialOverrideSettings.KeywordStateOverrides));
        var stringTagOverrides = property.FindPropertyRelative(nameof(MaterialOverrideSettings.StringTagOverrides));
        var shaderPassStateOverrides = property.FindPropertyRelative(nameof(MaterialOverrideSettings.ShaderPassStateOverrides));

        position.SetSingleHeight();
        GUIHelper.SplitRectHorizontallyForRight(position, GetResetButtonWidth(), out var foldoutRect, out var resetRect);

        var advancedOverrideCount = GetAdvancedOverrideCount(
            overrideLightmapFlags,
            overrideEnableInstancing,
            overrideDoubleSidedGI,
            keywordStateOverrides,
            stringTagOverrides,
            shaderPassStateOverrides);
        var overrideCount = GetOverrideCount(
            overrideShader,
            targetShader,
            overrideRenderQueue,
            propertyOverrides,
            advancedOverrideCount);

        label = new GUIContent(string.Format("overrideSettings.count".LS(), overrideCount));

        var isExpanded = GUIHelper.Foldout(foldoutRect, property, label, RectStrictFoldoutOptions);
        using (new EditorGUI.DisabledGroupScope(overrideCount == 0))
        {
            if (GUI.Button(resetRect, "overrideSettings.reset".LG()))
            {
                property.CopyFrom(MaterialOverrideSettings.Empty);
            }
        }
        if (!isExpanded) return;

        position.NewLine();
        position.Indent();

        // var helpBoxRect = position;
        // helpBoxRect.height = GetInnerHeight(property);
        // helpBoxRect = new RectOffset(3, 3, 3, 3).Add(helpBoxRect);
        // EditorGUI.LabelField(helpBoxRect, GUIContent.none, EditorStyles.helpBox);

        if (MaterialEditorSettings.ShowInspectorDescription)
        {
            position = GUIHelper.HelpBox(position, _helpKey.LS(), MessageType.Info);
        }

        DrawOverrideField(
            ref position,
            overrideShader,
            "common.shader".LG(),
            rect => EditorGUI.PropertyField(rect, targetShader, GUIContent.none));

        DrawOverrideField(
            ref position,
            overrideRenderQueue,
            "common.renderQueue".LG(),
            rect => DrawRenderQueueGUI(rect, renderQueueValue));

        position = DrawOverrideList(position, propertyOverrides, "overrideSettings.properties".LG(), prop => prop.CopyFrom(new MaterialProperty()));

        var advancedExpanded = DrawAdvancedSettingsFoldout(position, property, advancedOverrideCount);
        position.NewLine();
        if (advancedExpanded)
        {
            position.Indent();
            DrawOverrideField(
                ref position,
                overrideLightmapFlags,
                new GUIContent("Lightmap Flags"),
                rect => EditorGUI.PropertyField(rect, lightmapFlagsValue, new GUIContent("Flags")));
            DrawOverrideField(
                ref position,
                overrideEnableInstancing,
                new GUIContent("GPU Instancing"),
                rect => EditorGUI.PropertyField(rect, enableInstancingValue, new GUIContent("Enabled")));
            DrawOverrideField(
                ref position,
                overrideDoubleSidedGI,
                new GUIContent("Double Sided Global Illumination"),
                rect => EditorGUI.PropertyField(rect, doubleSidedGIValue, new GUIContent("Enabled")));

            position = DrawOverrideList(position, keywordStateOverrides, new GUIContent("Keywords"), prop => prop.CopyFrom(new MaterialKeywordStateOverride()));
            position = DrawOverrideList(position, stringTagOverrides, new GUIContent("String Tags"), prop => prop.CopyFrom(new MaterialStringTagOverride()));
            position = DrawOverrideList(position, shaderPassStateOverrides, new GUIContent("Shader Passes"), prop => prop.CopyFrom(new MaterialShaderPassStateOverride()));
            position.Back();
        }

        position.Back();
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

    private static bool DrawAdvancedSettingsFoldout(Rect position, SerializedProperty property, int overrideCount)
    {
        GUIHelper.SplitRectHorizontallyForRight(position, EditorGUIUtility.fieldWidth, out var foldoutRect, out var countRect);
        var key = GetAdvancedSettingsExpandedKey(property);
        var isExpanded = _advancedSettingsExpandedKeys.Contains(key);
        var nextExpanded = GUIHelper.Foldout(foldoutRect, isExpanded, AdvancedSettingsLabelKey.LG(), RectStrictFoldoutOptions);
        if (nextExpanded) _advancedSettingsExpandedKeys.Add(key);
        else _advancedSettingsExpandedKeys.Remove(key);

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUI.IntField(countRect, overrideCount);
        }
        return nextExpanded;
    }

    private static void DrawOverrideField(
        ref Rect position,
        SerializedProperty enabledProperty,
        GUIContent label,
        Action<Rect> drawValue)
    {
        var (isExpanded, isEnabled) = GUIHelper.FoldoutAndToggleLeft(position, enabledProperty, label, rectStrict: RectStrictFoldoutOptions.RectStrict);
        position.NewLine();
        if (!isExpanded) return;

        position.Indent();
        using (new EditorGUI.DisabledGroupScope(!isEnabled))
        {
            drawValue(position);
            position.NewLine();
        }

        position.Back();
    }

    private float GetInnerHeight(SerializedProperty property)
    {
        var height = 0f;

        if (MaterialEditorSettings.ShowInspectorDescription)
        {
            height += GUIHelper.GetHelpBoxHeight(_helpKey.LS(), MessageType.Info);
            height += GUIHelper.GUI_SPACE;
        }

        var overrideShader = property.FindPropertyRelative(nameof(MaterialOverrideSettings.OverrideShader));
        var overrideRenderQueue = property.FindPropertyRelative(nameof(MaterialOverrideSettings.OverrideRenderQueue));
        var overrideLightmapFlags = property.FindPropertyRelative(nameof(MaterialOverrideSettings.OverrideLightmapFlags));
        var overrideEnableInstancing = property.FindPropertyRelative(nameof(MaterialOverrideSettings.OverrideEnableInstancing));
        var overrideDoubleSidedGI = property.FindPropertyRelative(nameof(MaterialOverrideSettings.OverrideDoubleSidedGI));
        var propertyOverrides = property.FindPropertyRelative(nameof(MaterialOverrideSettings.PropertyOverrides));
        var keywordStateOverrides = property.FindPropertyRelative(nameof(MaterialOverrideSettings.KeywordStateOverrides));
        var stringTagOverrides = property.FindPropertyRelative(nameof(MaterialOverrideSettings.StringTagOverrides));
        var shaderPassStateOverrides = property.FindPropertyRelative(nameof(MaterialOverrideSettings.ShaderPassStateOverrides));
        height += GetOverrideFieldHeight(overrideShader);
        height += GetOverrideFieldHeight(overrideRenderQueue);
        height += GetOverrideListHeight(propertyOverrides);
        height += GUIHelper.propertyHeight + GUIHelper.GUI_SPACE;
        if (IsAdvancedSettingsExpanded(property))
        {
            height += GetOverrideFieldHeight(overrideLightmapFlags);
            height += GetOverrideFieldHeight(overrideEnableInstancing);
            height += GetOverrideFieldHeight(overrideDoubleSidedGI);
            height += GetOverrideListHeight(keywordStateOverrides);
            height += GetOverrideListHeight(stringTagOverrides);
            height += GetOverrideListHeight(shaderPassStateOverrides);
        }

        return height;
    }

    private static Rect DrawOverrideList(
        Rect position,
        SerializedProperty property,
        GUIContent label,
        Action<SerializedProperty> initializeFunction)
    {
        return GUIHelper.List(position, property, label, OverrideListOptions, initializeFunction);
    }

    private static float GetOverrideListHeight(SerializedProperty property)
    {
        // GUIHelper.List always advances to the next line after the list header/body.
        // GUIHelper.GetListHeight returns the drawn controls height, so include that consumed spacing here.
        return GUIHelper.GetListHeight(property, OverrideListOptions) + GUIHelper.GUI_SPACE;
    }

    private static float GetOverrideFieldHeight(SerializedProperty enabledProperty)
    {
        var height = GUIHelper.propertyHeight + GUIHelper.GUI_SPACE;
        if (enabledProperty.isExpanded)
        {
            height += GUIHelper.propertyHeight + GUIHelper.GUI_SPACE;
        }

        return height;
    }

    private static int GetOverrideCount(
        SerializedProperty overrideShader,
        SerializedProperty targetShader,
        SerializedProperty overrideRenderQueue,
        SerializedProperty propertyOverrides,
        int advancedOverrideCount)
    {
        var overrideCount = 0;
        if (overrideShader.boolValue && targetShader.objectReferenceValue != null) overrideCount++;
        if (overrideRenderQueue.boolValue) overrideCount++;
        overrideCount += propertyOverrides.arraySize;
        overrideCount += advancedOverrideCount;
        return overrideCount;
    }

    private static int GetAdvancedOverrideCount(
        SerializedProperty overrideLightmapFlags,
        SerializedProperty overrideEnableInstancing,
        SerializedProperty overrideDoubleSidedGI,
        SerializedProperty keywordStateOverrides,
        SerializedProperty stringTagOverrides,
        SerializedProperty shaderPassStateOverrides)
    {
        var count = 0;
        if (overrideLightmapFlags.boolValue) count++;
        if (overrideEnableInstancing.boolValue) count++;
        if (overrideDoubleSidedGI.boolValue) count++;
        count += keywordStateOverrides.arraySize;
        count += stringTagOverrides.arraySize;
        count += shaderPassStateOverrides.arraySize;
        return count;
    }

    private static bool IsAdvancedSettingsExpanded(SerializedProperty property)
    {
        return _advancedSettingsExpandedKeys.Contains(GetAdvancedSettingsExpandedKey(property));
    }

    private static string GetAdvancedSettingsExpandedKey(SerializedProperty property)
    {
        return $"{property.serializedObject.targetObject.GetInstanceID()}:{property.propertyPath}";
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

        height += GUIHelper.GUI_SPACE;
        height += GetInnerHeight(property);
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
