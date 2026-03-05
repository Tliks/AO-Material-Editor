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
        
        PatchPrefix(harmony, materialEditorType, "ShaderPropertyInternal", new[] { typeof(Rect), typeof(UnityEditor.MaterialProperty), typeof(GUIContent) }, nameof(ShaderPropertyInternalPrefix));

        // leaf methods
        PatchPrefix(harmony, materialEditorType, "DoPowerRangeProperty", new[] { typeof(Rect), typeof(UnityEditor.MaterialProperty), typeof(GUIContent), typeof(float) }, nameof(LeafPrefix));
        PatchPrefix(harmony, materialEditorType, "IntegerPropertyInternal", new[] { typeof(Rect), typeof(UnityEditor.MaterialProperty), typeof(GUIContent) }, nameof(LeafPrefix));
        PatchPrefix(harmony, materialEditorType, "FloatPropertyInternal", new[] { typeof(Rect), typeof(UnityEditor.MaterialProperty), typeof(GUIContent) }, nameof(LeafPrefix));
        PatchPrefix(harmony, materialEditorType, "ColorPropertyInternal", new[] { typeof(Rect), typeof(UnityEditor.MaterialProperty), typeof(GUIContent) }, nameof(LeafPrefix));
        PatchPrefix(harmony, materialEditorType, "VectorPropertyInternal", new[] { typeof(Rect).MakeByRefType(), typeof(UnityEditor.MaterialProperty).MakeByRefType(), typeof(GUIContent).MakeByRefType() }, nameof(LeafPrefix));
        PatchPrefix(harmony, materialEditorType, "TextureProperty", new[] { typeof(Rect), typeof(UnityEditor.MaterialProperty), typeof(GUIContent), typeof(bool) }, nameof(LeafPrefix));

        PatchPrefix(harmony, materialEditorType, "TexturePropertyMiniThumbnail", new[] { typeof(Rect), typeof(UnityEditor.MaterialProperty), typeof(string), typeof(string) }, nameof(TexturePropertyMiniThumbnailPrefix));
        PatchPrefix(harmony, materialEditorType, "TexturePropertyWithHDRColor", new[] { typeof(GUIContent), typeof(UnityEditor.MaterialProperty), typeof(UnityEditor.MaterialProperty), typeof(bool) }, nameof(TexturePropertyWithHDRColorPrefix));
        PatchPrefix(harmony, materialEditorType, "TextureScaleOffsetProperty", new[] { typeof(Rect), typeof(UnityEditor.MaterialProperty), typeof(bool) }, nameof(TextureScaleOffsetPropertyPrefix));
    }


    private static void PatchGUILabel(Harmony harmony)
    {
        PatchPrefix(harmony, typeof(UnityEngine.GUI), nameof(UnityEngine.GUI.Label), new[] { typeof(Rect), typeof(GUIContent), typeof(GUIStyle) }, nameof(GUILabelPrefix));
    }

    private static void PatchPrefix(Harmony harmony, Type type, string methodName, Type[] args, string prefixName)
    {
        var method = AccessTools.Method(type, methodName, args)
            ?? throw new Exception($"{methodName} method not found");
        harmony.Patch(method, prefix: new HarmonyMethod(typeof(MaterialEditorPatcher), prefixName));
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
        TryDrawRevertButton(position, prop);
    }

    private static void LeafPrefix(Rect position, UnityEditor.MaterialProperty prop)
    {
        TryDrawRevertButton(position, prop);
    }

    private static void TexturePropertyMiniThumbnailPrefix(
        Rect position, 
        UnityEditor.MaterialProperty prop, 
        string label, 
        string tooltip)
    {
        TryDrawRevertButton(position, prop);
    }

    private static void TexturePropertyWithHDRColorPrefix(
        Rect __result,
        GUIContent label,
        UnityEditor.MaterialProperty textureProp,
        UnityEditor.MaterialProperty colorProperty,
        bool showAlpha)
    {
        TryDrawRevertButton(__result, colorProperty);
    }

    private static void TextureScaleOffsetPropertyPrefix(
        Rect position,
        UnityEditor.MaterialProperty property,
        bool partOfTexturePropertyControl)
    {
        if (partOfTexturePropertyControl)
            return;
        TryDrawRevertButton(position, property);
    }

    private static void TryDrawRevertButton(Rect position, UnityEditor.MaterialProperty prop)
    {
        var targets = prop.targets;
        if (targets == null || targets.Length != 1 || targets[0] is not Material recordingMaterial)
            return;

        if (!MaterialEditoEditorContext.RecordingToComponent.TryGetValue(recordingMaterial, out var component))
            return;
        
        if (!MaterialEditoEditorContext.ComponentToOverrideProperties.TryGetValue(component, out var overrideProperties))
            return;

        if (!overrideProperties.Contains(prop.name))
            return;

        DrawRevertButton(position, prop, component);
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
        UnityEditor.MaterialProperty prop,
        MaterialEditorComponent component)
    {
        if (!GUIHelper.TryGetMarginX(out var marginX))
            return;

        var rowRect = new Rect(position.xMin, position.yMin, position.width, position.height);
        var buttonRect = new Rect(
            marginX,
            rowRect.y + (rowRect.height - BUTTON_SIZE) * 0.5f,
            BUTTON_SIZE,
            BUTTON_SIZE
        );

        if (GUI.Button(buttonRect, RevertButtonContent, IconButtonStyle))
            RevertProperty(component, prop);
            if (MaterialEditoEditorContext.ComponentToMaterialEditor.TryGetValue(component, out var materialEditor) && materialEditor != null)
                materialEditor.Repaint();
    }

    private static void RevertProperty(MaterialEditorComponent component, UnityEditor.MaterialProperty prop)
    {
        var propertyName = prop.name;
        Undo.RecordObject(component, "Revert Property");
        component.OverrideSettings.PropertyOverrides.RemoveAll(p => p.PropertyName == propertyName);
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
