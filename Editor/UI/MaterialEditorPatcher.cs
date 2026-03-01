#if ME_HARMONY

using HarmonyLib;
using EditorMaterialEditor = UnityEditor.MaterialEditor;
using EditorMaterialProperty = UnityEditor.MaterialProperty;

namespace Aoyon.MaterialEditor.UI;

/// <summary>
/// Material インスペクタで override 中のプロパティ左側にマージンラインを描く。
/// 高さは GetPropertyHeight のみで決める。Drawer が GetPropertyHeight を正しく返せば正しく表示される。
/// Prefab override と同様に EditorGUI.DrawMarginLineForRect を使用（利用可能な場合）。
/// </summary>
[InitializeOnLoad]
internal static class MaterialEditorPatcher
{

    static MaterialEditorPatcher()
    {
        try
        {
            var harmony = new Harmony(typeof(MaterialEditorPatcher).FullName);

            var materialEditorType = typeof(EditorMaterialEditor);
            var shaderPropertyMethod = AccessTools.Method(
                materialEditorType,
                "ShaderPropertyInternal",
                new[] { typeof(Rect), typeof(EditorMaterialProperty), typeof(GUIContent) }
            );
            if (shaderPropertyMethod == null) throw new Exception("ShaderPropertyInternal method not found");

            harmony.Patch(
                shaderPropertyMethod,
                prefix: new HarmonyMethod(typeof(MaterialEditorPatcher), nameof(ShaderPropertyPrefix))
            );
        }
        catch (Exception e)
        {
            Debug.LogWarning(e);
        }
    }

    private static void ShaderPropertyPrefix(
        Rect position,
        EditorMaterialProperty prop,
        GUIContent label,
        EditorMaterialEditor __instance
    )
    {
        var targets = __instance.targets;
        if (targets == null || targets.Length != 1 || targets[0] is not Material recordingMaterial)
            return;

        if (!MaterialEditoEditorContext.OverrideProperties.TryGetValue(recordingMaterial, out var overrideProperties))
            return;

        if (!overrideProperties.Contains(prop.name))
            return;

        if (Event.current.type != EventType.Repaint)
            return;

        var height = __instance.GetPropertyHeight(prop, label?.text ?? prop.displayName);
        var lineRect = new Rect(position.xMin, position.yMin, position.width, height);
        var lineColor = EditorGUIUtility.isProSkin
            ? new Color32(196, 196, 196, 255)
            : new Color32(9, 9, 9, 255);
        GUIHelper.DrawMarginLineForRect(lineRect, lineColor);
    }
}

#endif
