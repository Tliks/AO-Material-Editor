using System.Reflection;
using EditorMaterialEditor = UnityEditor.MaterialEditor;
using EditorMaterialProperty = UnityEditor.MaterialProperty;

namespace Aoyon.MaterialEditor.Extension;

internal static class PoiyomiShaderUtility
{
    private const BindingFlags StaticBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
    private const string OriginalShaderTag = "OriginalShader";
    private const string OriginalShaderGuidTag = "OriginalShaderGUID";
    private const string OriginalKeywordsTag = "OriginalKeywords";
    private const string AllLockedGuidsTag = "AllLockedGUIDS";
    private const string AnimatedTagSuffix = "Animated";
    private const string StrippedTextureTagPrefix = "_stripped_tex_";

    public static bool IsPoiyomiMaterial(Material material)
    {
        var shaderName = material.shader.name;
        var originalShaderName = material.GetTag(OriginalShaderTag, false, "");
        return shaderName.Contains("poiyomi", StringComparison.OrdinalIgnoreCase)
               || shaderName.Contains("PCSS4Poi", StringComparison.Ordinal)
               || originalShaderName.Contains("poiyomi", StringComparison.OrdinalIgnoreCase);
    }

    // https://github.com/poiyomi/PoiyomiToonShader/blame/c5aaeeb3a67782b7e8a26e184d5e0a1970792294/_PoiyomiShaders/Scripts/ThryEditor/Editor/ShaderOptimizer.cs#L2306

    private static readonly Type? ShaderOptimizerType =
        Type.GetType("Thry.ThryEditor.ShaderOptimizer, ThryAssemblyDefinition", false);

    private static readonly MethodInfo? GetOriginalShaderMethod =
        ShaderOptimizerType?.GetMethod("GetOriginalShader", StaticBindingFlags, null, new[] { typeof(Material) }, null);

    private static readonly MethodInfo? GetRenamedPropertySuffixMethod =
        ShaderOptimizerType?.GetMethod("GetRenamedPropertySuffix", StaticBindingFlags, null, new[] { typeof(Material) }, null);

    private static readonly MethodInfo? CopyPropertyMethod =
        ShaderOptimizerType?.GetMethod("CopyProperty", StaticBindingFlags, null, new[] { typeof(Material), typeof(EditorMaterialProperty), typeof(string) }, null);

    private static readonly FieldInfo? IllegalPropertyRenamesField =
        ShaderOptimizerType?.GetField("IllegalPropertyRenames", StaticBindingFlags);

    public static bool Unlock(Material material, Material? sourceMaterial = null)
    {
        if (sourceMaterial != null)
        {
            foreach (var tagName in GetTagNames(sourceMaterial))
            {
                material.SetOverrideTag(tagName, sourceMaterial.GetTag(tagName, false, ""));
            }
        }

        if (material.shader == null)
        {
            DebugLogWarning($"Failed to unlock Poiyomi material '{material.name}'. Shader is missing.");
            return false;
        }

        if (ShaderOptimizerType == null || GetOriginalShaderMethod == null || GetRenamedPropertySuffixMethod == null || CopyPropertyMethod == null)
        {
            DebugLogWarning($"Failed to unlock Poiyomi material '{material.name}' ({material.shader.name}). ShaderOptimizer helpers were not found.");
            return false;
        }

        if (!material.shader.name.StartsWith("Hidden/", StringComparison.Ordinal))
        {
            foreach (var tagName in GetTagNames(material))
            {
                if (tagName is OriginalShaderTag or OriginalShaderGuidTag or OriginalKeywordsTag or AllLockedGuidsTag
                    || tagName.StartsWith(StrippedTextureTagPrefix, StringComparison.Ordinal))
                {
                    material.SetOverrideTag(tagName, "");
                }
            }

            return true;
        }

        var originalShader = ResolveOriginalShader(material, out var failureReason);
        if (originalShader == null)
        {
            DebugLogWarning($"Failed to unlock Poiyomi material '{material.name}' ({material.shader.name}). {failureReason}");
            return false;
        }

        var renamedPropertySuffix = "_" + (string)GetRenamedPropertySuffixMethod.Invoke(null, new object[] { material })!;
        var illegalPropertyRenames = IllegalPropertyRenamesField?.GetValue(null) as HashSet<string>;
        var renamedProperties = new List<EditorMaterialProperty>();
        foreach (var property in EditorMaterialEditor.GetMaterialProperties(new Object[] { material }))
        {
            if (property == null || !property.name.EndsWith(renamedPropertySuffix, StringComparison.Ordinal)) continue;

            var propertyName = property.name[..^renamedPropertySuffix.Length];
            var animatedTag = material.GetTag(propertyName + AnimatedTagSuffix, false, "");
            if (!string.Equals(animatedTag, "2", StringComparison.Ordinal)
                || propertyName.EndsWith("UV", StringComparison.Ordinal)
                || propertyName.EndsWith("Pan", StringComparison.Ordinal)
                || (illegalPropertyRenames?.Contains(propertyName) ?? false))
            {
                continue;
            }

            renamedProperties.Add(property);
        }

        var renderType = material.GetTag("RenderType", false, "");
        var renderQueue = material.renderQueue;
        var originalKeywords = material.GetTag(OriginalKeywordsTag, false, string.Join(" ", material.shaderKeywords));

        material.shader = originalShader;
        material.SetOverrideTag("RenderType", renderType);
        material.renderQueue = renderQueue;
        material.shaderKeywords = originalKeywords.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (material.HasProperty("_ShaderOptimizerEnabled"))
        {
            material.SetFloat("_ShaderOptimizerEnabled", 0f);
        }

        foreach (var textureName in material.GetTexturePropertyNames())
        {
            var textureGuid = material.GetTag(StrippedTextureTagPrefix + textureName, false, "");
            if (string.IsNullOrWhiteSpace(textureGuid)) continue;

            var texturePath = AssetDatabase.GUIDToAssetPath(textureGuid);
            var texture = AssetDatabase.LoadAssetAtPath<Texture>(texturePath);
            if (texture != null)
            {
                material.SetTexture(textureName, texture);
            }
        }

        foreach (var property in renamedProperties)
        {
            var propertyName = property.name[..^renamedPropertySuffix.Length];
            if (material.HasProperty(propertyName))
            {
                CopyPropertyMethod.Invoke(null, new object[] { material, property, propertyName });
            }
        }

        foreach (var tagName in GetTagNames(material))
        {
            if (tagName is OriginalShaderTag or OriginalShaderGuidTag or OriginalKeywordsTag or AllLockedGuidsTag
                || tagName.StartsWith(StrippedTextureTagPrefix, StringComparison.Ordinal))
            {
                material.SetOverrideTag(tagName, "");
            }
        }

        if (material.shader.name.StartsWith("Hidden/", StringComparison.Ordinal))
        {
            DebugLogWarning($"Failed to unlock Poiyomi material '{material.name}' ({material.shader.name}). Shader stayed locked after restore.");
            return false;
        }

        return true;
    }

    static void DebugLogWarning(string message)
    {
        Debug.LogWarning($"[AO Material Editor] {message}");
    }

    private static List<string> GetTagNames(Material material)
    {
        var tagNames = new List<string>();
        var iterator = new SerializedObject(material).GetIterator();
        while (iterator.Next(true))
        {
            if (iterator.name != "stringTagMap") continue;

            for (var index = 0; index < iterator.arraySize; index++)
            {
                tagNames.Add(iterator.GetArrayElementAtIndex(index).displayName);
            }

            break;
        }

        return tagNames;
    }

    private static Shader? ResolveOriginalShader(Material material, out string failureReason)
    {
        if (GetOriginalShaderMethod?.Invoke(null, new object[] { material }) is Shader shader)
        {
            failureReason = string.Empty;
            return shader;
        }

        var originalShaderGuid = material.GetTag(OriginalShaderGuidTag, false, "");
        var originalShaderName = material.GetTag(OriginalShaderTag, false, "");
        failureReason =
            $"Original shader could not be resolved. OriginalShaderGUID='{originalShaderGuid}', OriginalShader='{originalShaderName}'.";
        return null;
    }
}
