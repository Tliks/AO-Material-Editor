#if ME_HARMONY

using HarmonyLib;

namespace Aoyon.MaterialEditor.UI;

[InitializeOnLoad]
internal static class MaterialEditorPatcher
{
    private const string HARMONY_ID = "aoyon.material-editor";

    static MaterialEditorPatcher()
    {
        if (MaterialEditorSettings.EnableMaterialEditorPatcher)
            ApplyPatches();

        MaterialEditorSettings.EnableMaterialEditorPatcherChanged += (enabled) => {
            if (enabled) {
                ApplyPatches();
            }
            else {
                UnapplyPatches();
            }
        };
    }

    private static void ApplyPatches()
    {
        var harmony = new Harmony(HARMONY_ID);

        try
        {
            PatchMaterialEditor(harmony);
            PatchGUILabel(harmony);
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
        
        PatchMethod(harmony, materialEditorType, "ShaderPropertyInternal",
            new[] { typeof(Rect), typeof(UnityEditor.MaterialProperty), typeof(GUIContent) },
            prefixName: nameof(ShaderPropertyInternalPrefix),
            postfixName: nameof(PropertyPostfix));

        // leaf methods
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


    private static void PatchGUILabel(Harmony harmony)
    {
        PatchMethod(harmony, typeof(UnityEngine.GUI), nameof(UnityEngine.GUI.Label),
            new[] { typeof(Rect), typeof(GUIContent), typeof(GUIStyle) },
            prefixName: nameof(GUILabelPrefix));
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

    private static void ShaderPropertyInternalPrefix(
        Rect position,
        UnityEditor.MaterialProperty prop,
        GUIContent label)
    {
        // // カスタムドロワーがない際はデフォルトの描画になるので他のパッチで重複する
        // ただ同時にボタンは消えるのと判定ロジックが大変なので、重複を許容する
        // if (!HasCustomDrawer(prop)) 
        //     return;
        BeginPropertyGUI(position, prop);
    }

    private static void LeafPrefix(Rect position, UnityEditor.MaterialProperty prop)
    {
        BeginPropertyGUI(position, prop);
    }

    private static void TexturePropertyMiniThumbnailPrefix(
        Rect position, 
        UnityEditor.MaterialProperty prop, 
        string label, 
        string tooltip)
    {
        BeginPropertyGUI(position, prop);
    }

    private static void TexturePropertyWithHDRColorPrefix(
        Rect __result,
        GUIContent label,
        UnityEditor.MaterialProperty textureProp,
        UnityEditor.MaterialProperty colorProperty,
        bool showAlpha)
    {
        BeginPropertyGUI(__result, colorProperty);
    }

    private static void TextureScaleOffsetPropertyPrefix(
        Rect position,
        UnityEditor.MaterialProperty property,
        bool partOfTexturePropertyControl)
    {
        if (partOfTexturePropertyControl)
            return;
        BeginPropertyGUI(position, property);
    }

    private static void PropertyPostfix()
    {
        EndPropertyGUI();
    }

    private readonly record struct PropertyGUIState(
        bool Enabled,
        Rect Position,
        string? Tooltip,
        MaterialEditorComponent? Component,
        string? PropertyName,
        bool ShowRevertButton);

    private static readonly Stack<PropertyGUIState> _guiStateStack = new();
    private static GUIContent? _tooltipOverlayContent;
    private static GUIContent TooltipOverlayContent => _tooltipOverlayContent ??= new GUIContent("");

    // control idの衝突を防ぐ為、postfixでrevert buttonは描画する
    private static void BeginPropertyGUI(Rect position, UnityEditor.MaterialProperty prop)
    {
        var enabled = GUI.enabled;
        string? tooltip = null;

        if (!TryGetRecordingContext(prop, out var component, out var overrideProperties, out var lockedProperties))
        {
            _guiStateStack.Push(new PropertyGUIState(enabled, position, null, null, null, false));
            return;
        }

        if (lockedProperties.Contains(prop.name))
        {
            GUI.enabled = false;
            tooltip = "lock.property.tooltip".LS();
        }

        _guiStateStack.Push(new PropertyGUIState(
            enabled,
            position,
            tooltip,
            component,
            prop.name,
            overrideProperties.Contains(prop.name)));
    }

    private static void EndPropertyGUI()
    {
        if (_guiStateStack.Count == 0)
            return;

        var state = _guiStateStack.Pop();
        GUI.enabled = state.Enabled;

        if (state.ShowRevertButton && state.Component != null && state.PropertyName != null)
        {
            DrawRevertButton(state.Position, state.PropertyName, state.Component);
        }

        if (!string.IsNullOrEmpty(state.Tooltip))
        {
            TooltipOverlayContent.tooltip = state.Tooltip;
            GUI.Label(state.Position, TooltipOverlayContent, GUIStyle.none);
        }
    }

    private static bool TryGetRecordingContext(
        UnityEditor.MaterialProperty prop,
        out MaterialEditorComponent component,
        out HashSet<string> overrideProperties,
        out HashSet<string> lockedProperties)
    {
        component = null!;
        overrideProperties = null!;
        lockedProperties = null!;

        var targets = prop.targets;
        if (targets == null || targets.Length != 1 || targets[0] is not Material recordingMaterial)
            return false;

        if (!MaterialEditoEditorContext.RecordingToComponent.TryGetValue(recordingMaterial, out component))
            return false;

        if (!MaterialEditoEditorContext.ComponentToOverrideProperties.TryGetValue(component, out overrideProperties))
            return false;

        if (!MaterialEditoEditorContext.ComponentToLockedProperties.TryGetValue(component, out lockedProperties))
            return false;

        return true;
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
        string propertyName,
        MaterialEditorComponent component)
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

        if (GUI.Button(buttonRect, RevertButtonContent, IconButtonStyle)) {
            RevertProperty(component, propertyName);
            if (MaterialEditoEditorContext.ComponentToMaterialEditor.TryGetValue(component, out var materialEditor) && materialEditor != null)
                EditorApplication.delayCall += () => materialEditor.Repaint();
        }
    }

    private static void RevertProperty(MaterialEditorComponent component, string propertyName)
    {
        var edited = new List<MaterialProperty>(component.OverrideSettings.PropertyOverrides);
        edited.RemoveAll(p => p.PropertyName == propertyName);
        using var so = new SerializedObject(component);
        so.FindProperty(nameof(MaterialEditorComponent.OverrideSettings)).FindPropertyRelative(nameof(MaterialOverrideSettings.PropertyOverrides)).CopyFrom(edited);
        so.ApplyModifiedProperties();
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

        if (!MaterialEditoEditorContext.IsRecording)
            return true;

        return false;
    }
}

#endif
