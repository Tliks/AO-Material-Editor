#if ME_HARMONY

using HarmonyLib;

namespace Aoyon.MaterialEditor.UI;

[InitializeOnLoad]
internal static class MaterialEditorPatcher // Todo: リファクタ
{
    private const string HARMONY_ID = "aoyon.material-editor";

    static MaterialEditorPatcher()
    {
        if (MaterialEditorSettings.EnableMaterialEditorPatcher)
            ApplyPatches();

        MaterialEditorSettings.EnableMaterialEditorPatcherChanged += OnEnabledChanged;
    }

    private static void OnEnabledChanged(bool enabled)
    {
        if (enabled) {
            ApplyPatches();
        }
        else {
            UnapplyPatches();
        }
    }

    private static void ApplyPatches()
    {
        var harmony = new Harmony(HARMONY_ID);

        try
        {
            PatchMaterialProperty(harmony);
            PatchMaterialEditor(harmony);
            PatchGUILabel(harmony);
            PatchPoiyomiLayout(harmony);
        }
        catch (Exception e)
        {
            Debug.LogError($"Harmony patch failed: {e}");
            UnapplyPatches();
        }

        AssemblyReloadEvents.beforeAssemblyReload += () => { UnapplyPatches(); };
    }

    private static void UnapplyPatches()
    {
        var harmony = new Harmony(HARMONY_ID);
        harmony.UnpatchAll(HARMONY_ID);
    }

    private static void PatchMaterialEditor(Harmony harmony)
    {
        var materialEditorType = typeof(UnityEditor.MaterialEditor);

        PatchMethod(harmony, materialEditorType, "SetShader",
            new[] { typeof(Shader), typeof(bool) },
            postfixName: nameof(SetShaderPostfix));
        PatchMethod(harmony, materialEditorType, "EndProperty",
            Type.EmptyTypes,
            postfixName: nameof(PropertyPostfix));

        PatchMethod(harmony, materialEditorType, "ShaderPropertyInternal",
            new[] { typeof(Rect), typeof(UnityEditor.MaterialProperty), typeof(GUIContent) },
            prefixName: nameof(LeafPrefix),
            postfixName: nameof(PropertyPostfix));
        PatchMethod(harmony, materialEditorType, "DoPowerRangeProperty",
            new[] { typeof(Rect), typeof(UnityEditor.MaterialProperty), typeof(GUIContent), typeof(float) },
            prefixName: nameof(LeafPrefix),
            postfixName: nameof(PropertyPostfix));
        PatchMethod(harmony, materialEditorType, "IntegerPropertyInternal",
            new[] { typeof(Rect), typeof(UnityEditor.MaterialProperty), typeof(GUIContent) },
            prefixName: nameof(LeafPrefix),
            postfixName: nameof(PropertyPostfix));
        PatchMethod(harmony, materialEditorType, "FloatPropertyInternal",
            new[] { typeof(Rect), typeof(UnityEditor.MaterialProperty), typeof(GUIContent) },
            prefixName: nameof(LeafPrefix),
            postfixName: nameof(PropertyPostfix));
        PatchMethod(harmony, materialEditorType, "ColorPropertyInternal",
            new[] { typeof(Rect), typeof(UnityEditor.MaterialProperty), typeof(GUIContent) },
            prefixName: nameof(LeafPrefix),
            postfixName: nameof(PropertyPostfix));
        PatchMethod(harmony, materialEditorType, "VectorPropertyInternal",
            new[] { typeof(Rect).MakeByRefType(), typeof(UnityEditor.MaterialProperty).MakeByRefType(), typeof(GUIContent).MakeByRefType() },
            prefixName: nameof(LeafPrefix),
            postfixName: nameof(PropertyPostfix));
        PatchMethod(harmony, materialEditorType, "TextureProperty",
            new[] { typeof(Rect), typeof(UnityEditor.MaterialProperty), typeof(GUIContent), typeof(bool) },
            prefixName: nameof(LeafPrefix),
            postfixName: nameof(PropertyPostfix));
        PatchMethod(harmony, materialEditorType, "TexturePropertyMiniThumbnail",
            new[] { typeof(Rect), typeof(UnityEditor.MaterialProperty), typeof(string), typeof(string) },
            prefixName: nameof(TexturePropertyMiniThumbnailPrefix),
            postfixName: nameof(PropertyPostfix));
        PatchMethod(harmony, materialEditorType, "TexturePropertyWithHDRColor",
            new[] { typeof(GUIContent), typeof(UnityEditor.MaterialProperty), typeof(UnityEditor.MaterialProperty), typeof(bool) },
            prefixName: nameof(TexturePropertyWithHDRColorPrefix),
            postfixName: nameof(PropertyPostfix));
        PatchMethod(harmony, materialEditorType, "TextureScaleOffsetProperty",
            new[] { typeof(Rect), typeof(UnityEditor.MaterialProperty), typeof(bool) },
            prefixName: nameof(TextureScaleOffsetPropertyPrefix),
            postfixName: nameof(PropertyPostfix));
    }

    private static void PatchMaterialProperty(Harmony harmony)
    {
        var materialPropertyType = typeof(UnityEditor.MaterialProperty);

        var beginPropertyMethod = AccessTools.GetDeclaredMethods(materialPropertyType)
            .FirstOrDefault(IsMaterialPropertyBeginPropertyMethod)
            ?? throw new Exception("MaterialProperty.BeginProperty method not found");
        PatchMethod(harmony, beginPropertyMethod, prefixName: nameof(MaterialPropertyBeginPropertyPrefix));

        var propertyDataType = AccessTools.TypeByName("UnityEditor.MaterialProperty+PropertyData");
        if (propertyDataType == null)
        {
            Debug.LogWarning("MaterialProperty.PropertyData type not found; lock action patch skipped.");
            return;
        }

        PatchMethod(harmony, propertyDataType, "DoLockAction",
            new[] { typeof(UnityEngine.Object[]) },
            prefixName: nameof(MaterialPropertyDoLockActionPrefix));
    }

    private static bool IsMaterialPropertyBeginPropertyMethod(System.Reflection.MethodInfo method)
    {
        if (method.Name != "BeginProperty") return false;

        var parameters = method.GetParameters();
        return parameters.Length == 5
               && parameters[0].ParameterType == typeof(Rect)
               && parameters[1].ParameterType == typeof(UnityEditor.MaterialProperty)
               && parameters[3].ParameterType == typeof(UnityEngine.Object[])
               && parameters[4].ParameterType == typeof(float);
    }

    private static bool MaterialPropertyDoLockActionPrefix()
    {
        return !MaterialEditorSession.IsRecording;
    }

    private static void PatchGUILabel(Harmony harmony)
    {
        PatchMethod(harmony, typeof(UnityEngine.GUI), nameof(UnityEngine.GUI.Label),
            new[] { typeof(Rect), typeof(GUIContent), typeof(GUIStyle) },
            prefixName: nameof(GUILabelPrefix));
    }

    private static void PatchPoiyomiLayout(Harmony harmony)
    {
        var rectifiedLayoutType = AccessTools.TypeByName("Thry.ThryEditor.RectifiedLayout");
        if (rectifiedLayoutType == null)
            return;

        PatchMethod(harmony, rectifiedLayoutType, "GetRect",
            new[] { typeof(int) },
            postfixName: nameof(PoiyomiRectifiedLayoutGetRectPostfix));
    }

    private static void PatchMethod(
        Harmony harmony,
        Type type,
        string methodName,
        Type[] args,
        string? prefixName = null,
        string? postfixName = null)
    {
        var method = AccessTools.Method(type, methodName, args)
            ?? throw new Exception($"{methodName} method not found");

        var prefix = prefixName != null ? new HarmonyMethod(typeof(MaterialEditorPatcher), prefixName) : null;
        var postfix = postfixName != null ? new HarmonyMethod(typeof(MaterialEditorPatcher), postfixName) : null;
        harmony.Patch(method, prefix: prefix, postfix: postfix);
    }

    private static void PatchMethod(
        Harmony harmony,
        System.Reflection.MethodInfo method,
        string? prefixName = null,
        string? postfixName = null)
    {
        var prefix = prefixName != null ? new HarmonyMethod(typeof(MaterialEditorPatcher), prefixName) : null;
        var postfix = postfixName != null ? new HarmonyMethod(typeof(MaterialEditorPatcher), postfixName) : null;
        harmony.Patch(method, prefix: prefix, postfix: postfix);
    }

    private static void MaterialPropertyBeginPropertyPrefix(
        Rect __0,
        UnityEditor.MaterialProperty __1,
        object __2,
        UnityEngine.Object[] __3,
        float __4)
    {
        if (__1 == null)
        {
            BeginSerializedPropertyGUI(__0, __4, ToSerializedPropertyKind(__2), __3);
        }
    }

    private static void PropertyPostfix()
    {
        EndPropertyGUI();
    }

    private static void LeafPrefix(Rect position, UnityEditor.MaterialProperty prop)
    {
        BeginLeafPropertyGUI(position, prop);
    }

    private static void TexturePropertyMiniThumbnailPrefix(
        Rect position,
        UnityEditor.MaterialProperty prop,
        string label,
        string tooltip)
    {
        BeginLeafPropertyGUI(position, prop);
    }

    private static void TexturePropertyWithHDRColorPrefix(
        Rect __result,
        GUIContent label,
        UnityEditor.MaterialProperty textureProp,
        UnityEditor.MaterialProperty colorProperty,
        bool showAlpha)
    {
        BeginLeafPropertyGUI(__result, colorProperty);
    }

    private static void TextureScaleOffsetPropertyPrefix(
        Rect position,
        UnityEditor.MaterialProperty property,
        bool partOfTexturePropertyControl)
    {
        if (partOfTexturePropertyControl)
        {
            PushInactivePropertyGUIState(position, -1f);
            return;
        }

        BeginLeafPropertyGUI(position, property);
    }

    private static void SetShaderPostfix(UnityEditor.MaterialEditor __instance)
    {
        foreach (var target in __instance.targets)
        {
            if (target is not Material material) continue;
            if (!MaterialEditorSession.TryGetRecordingSession(material, out _)) continue;

            MaterialUtility.Normalize(material);
        }
    }

    private readonly record struct PropertyGUIState(
        bool Enabled,
        Rect Position,
        float StartY,
        string? Tooltip,
        Material? RecordingMaterial,
        string? PropertyName,
        RecordingMaterialSerializedProperty? SerializedProperty,
        bool ShowRevertButton)
    {
        public bool IsActive => RecordingMaterial != null || SerializedProperty != null;
    }

    private static readonly Stack<PropertyGUIState> _guiStateStack = new();
    private static GUIContent? _tooltipOverlayContent;
    private static GUIContent TooltipOverlayContent => _tooltipOverlayContent ??= new GUIContent("");

    // control idの衝突を防ぐ為、postfixでrevert buttonは描画する
    private static void BeginPropertyGUI(Rect position, float startY, UnityEditor.MaterialProperty prop)
    {
        var enabled = GUI.enabled;
        string? tooltip = null;

        if (!TryGetRecordingContext(prop, out var editor, out var recordingMaterial))
        {
            PushInactivePropertyGUIState(position, startY);
            return;
        }

        if (editor.IsPropertyLocked(prop.name))
        {
            GUI.enabled = false;
            tooltip = "lock.property.tooltip".LS();
        }

        _guiStateStack.Push(new PropertyGUIState(
            enabled,
            position,
            startY,
            tooltip,
            recordingMaterial,
            prop.name,
            null,
            editor.IsPropertyOverriden(prop.name)));
    }

    private static void BeginLeafPropertyGUI(Rect position, UnityEditor.MaterialProperty prop)
    {
        BeginPropertyGUI(position, -1f, prop);
    }

    private static void BeginSerializedPropertyGUI(
        Rect position,
        float startY,
        RecordingMaterialSerializedProperty? serializedProperty,
        UnityEngine.Object[] targets)
    {
        var enabled = GUI.enabled;
        string? tooltip = null;

        if (serializedProperty == null ||
            !TryGetRecordingContext(targets, out var editor, out var recordingMaterial))
        {
            PushInactivePropertyGUIState(position, startY);
            return;
        }

        if (editor.IsSerializedPropertyLocked(serializedProperty.Value))
        {
            GUI.enabled = false;
            tooltip = "lock.property.tooltip".LS();
        }

        _guiStateStack.Push(new PropertyGUIState(
            enabled,
            position,
            startY,
            tooltip,
            recordingMaterial,
            null,
            serializedProperty,
            editor.IsSerializedPropertyOverriden(serializedProperty.Value)));
    }

    private static void PushInactivePropertyGUIState(Rect position, float startY)
    {
        _guiStateStack.Push(new PropertyGUIState(GUI.enabled, position, startY, null, null, null, null, false));
    }

    private static void EndPropertyGUI()
    {
        if (_guiStateStack.Count == 0)
            return;

        var state = _guiStateStack.Pop();
        GUI.enabled = state.Enabled;
        var hasPosition = TryGetButtonSourceRect(state.Position, state.StartY, out var position);

        if (state.ShowRevertButton && state.RecordingMaterial != null && hasPosition)
        {
            DrawRevertButton(position, state.RecordingMaterial, state.PropertyName, state.SerializedProperty);
        }

        if (!string.IsNullOrEmpty(state.Tooltip) && hasPosition)
        {
            TooltipOverlayContent.tooltip = state.Tooltip;
            GUI.Label(position, TooltipOverlayContent, GUIStyle.none);
        }
    }

    private static bool TryGetButtonSourceRect(Rect rect, float startY, out Rect result)
    {
        result = rect;
        if (startY != -1f)
        {
            var lastRect = GUILayoutUtility.GetLastRect();
            result = lastRect;
            result.yMin = startY;
            result.x = 1f;
            result.width = EditorGUIUtility.labelWidth;
            return true;
        }

        if (rect.width > 0f || rect.height > 0f)
            return true;

        return false;
    }

    private static bool TryGetRecordingContext(
        UnityEditor.MaterialProperty prop,
        out MaterialEditorSession session,
        out Material recordingMaterial)
    {
        session = null!;
        recordingMaterial = null!;

        var targets = prop.targets;
        if (targets == null || targets.Length != 1 || targets[0] is not Material material)
            return false;

        recordingMaterial = material;
        if (!MaterialEditorSession.TryGetRecordingSession(recordingMaterial, out var foundSession))
            return false;

        session = foundSession;
        return true;
    }

    private static bool TryGetRecordingContext(
        UnityEngine.Object[] targets,
        out MaterialEditorSession session,
        out Material recordingMaterial)
    {
        session = null!;
        recordingMaterial = null!;

        if (targets == null || targets.Length != 1 || targets[0] is not Material material)
            return false;

        recordingMaterial = material;
        if (!MaterialEditorSession.TryGetRecordingSession(recordingMaterial, out var foundSession))
            return false;

        session = foundSession;
        return true;
    }

    private static RecordingMaterialSerializedProperty? ToSerializedPropertyKind(object property)
    {
        return property.ToString() switch
        {
            "CustomRenderQueue" => RecordingMaterialSerializedProperty.CustomRenderQueue,
            "LightmapFlags" => RecordingMaterialSerializedProperty.LightmapFlags,
            "EnableInstancingVariants" => RecordingMaterialSerializedProperty.EnableInstancingVariants,
            "DoubleSidedGI" => RecordingMaterialSerializedProperty.DoubleSidedGI,
            _ => null
        };
    }

    private const float BUTTON_SIZE = 16f;
    private static readonly Texture _MinusIcon = EditorGUIUtility.IconContent("d_Toolbar Minus").image;
    private static GUIStyle? _iconButtonStyle;
    private static GUIStyle IconButtonStyle => _iconButtonStyle ??= new GUIStyle(EditorStyles.miniButton)
    {
        fixedWidth = BUTTON_SIZE,
        fixedHeight = BUTTON_SIZE,
        padding = new RectOffset(0, 0, 0, 0),
        margin  = new RectOffset(0, 0, 0, 0),
    };
    private static GUIContent? _revertButtonContent;
    private static GUIContent RevertButtonContent => _revertButtonContent ??= new GUIContent("") { image = _MinusIcon };

    private static void DrawRevertButton(
        Rect position,
        Material recordingMaterial,
        string? propertyName,
        RecordingMaterialSerializedProperty? serializedProperty)
    {
        if (!GUIHelper.TryGetMarginX(out var marginX))
            return;

        var rowRect = new Rect(position.xMin, position.yMin, position.width, position.height);
        var buttonRect = new Rect(
            marginX + 1,
            rowRect.y + (rowRect.height - BUTTON_SIZE) * 0.5f,
            BUTTON_SIZE,
            BUTTON_SIZE
        );

        if (RevertButton(buttonRect))
        {
            RevertProperty(recordingMaterial, propertyName, serializedProperty);
            GUIUtility.ExitGUI();
        }
    }

    private static bool RevertButton(Rect rect)
    {
        var current = Event.current;
        if (current == null) return false;

        switch (current.type)
        {
            case EventType.MouseDown:
                if (GUI.enabled && current.button == 0 && rect.Contains(current.mousePosition))
                {
                    current.Use();
                    return true;
                }
                break;

            case EventType.Repaint:
                IconButtonStyle.Draw(rect, RevertButtonContent, false, false, false, false);
                break;
        }

        return false;
    }

    private static void RevertProperty(
        Material recordingMaterial,
        string? propertyName,
        RecordingMaterialSerializedProperty? serializedProperty)
    {
        if (MaterialEditorSession.TryGetRecordingSession(recordingMaterial, out var session))
        {
            if (propertyName != null)
            {
                session.RevertRecordingProperty(propertyName);
            }
            else if (serializedProperty != null)
            {
                session.RevertRecordingSerializedProperty(serializedProperty.Value);
            }
        }
    }

    private static readonly Texture _lockInChildrenIcon =
        EditorGUIUtility.IconContent("HierarchyLock").image;
    private static readonly Texture _lockedByAncestorIcon =
        EditorGUIUtility.IconContent("IN LockButton on").image;

    private static bool GUILabelPrefix(Rect position, GUIContent content, GUIStyle style)
    {
        if (content == null || content.image == null)
            return true;

        var tex = content.image;
        if (tex != _lockInChildrenIcon && tex != _lockedByAncestorIcon)
            return true;

        if (!MaterialEditorSession.IsRecording)
            return true;

        return false;
    }

    private static void PoiyomiRectifiedLayoutGetRectPostfix(ref Rect __result)
    {
#if UNITY_2022_1_OR_NEWER
        if (!MaterialEditorSession.IsRecording)
            return;

        __result.x += 30;
        __result.width = Mathf.Max(0, __result.width - 30);
#endif
    }
}

#endif
