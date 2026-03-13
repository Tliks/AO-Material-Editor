using UnityEngine.Rendering;

namespace Aoyon.MaterialEditor;

internal static class MaterialUtility
{
    public static IEnumerable<MaterialProperty> EnumerateRawProperties(Material material)
    {
        var shader = material.shader;
        var propertyCount = shader.GetPropertyCount();
        for (var i = 0; i < propertyCount; i++)
        {
            if (!MaterialProperty.TryGet(material, i, out var property)) continue;
            yield return property;
        }
    }
    
    public static IEnumerable<MaterialProperty> GetProperties(Material material)
    {
        var seenNames = new HashSet<string>();
        foreach (var property in EnumerateRawProperties(material))
        {
            // なんか同名で複数の値が存在することがあるらしい？エッジケースだと思うけど
            // 前と後のどちらが優先されるかは未確認。ここでは実装の簡単のために、前を採用
            // Todo: ちゃんと調べる
            if (!seenNames.Add(property.PropertyName)) continue;
            yield return property;
        }
    }

    public static IEnumerable<MaterialProperty> GetPropertyOverrides(Material original, Material overrided, 
        bool strict, bool includeExtra, bool includeTextures = true)
    {
        var namedToOriginalProperty = GetProperties(original).ToDictionary(p => p.PropertyName);

        var overridedProperties = GetProperties(overrided);
        foreach (var property in overridedProperties)
        {
            if (!includeTextures && property.PropertyType == ShaderPropertyType.Texture) continue;
            
            var propertyName = property.PropertyName;
            
            if (namedToOriginalProperty.TryGetValue(propertyName, out var originalProperty))
            {
                if (originalProperty.EqualsImpl(property, strict)) continue;
                yield return property;
            }
            else
            {
                if (includeExtra) yield return property;
            }
        }
    }

    public static IEnumerable<MaterialProperty> GetVariantPropertyOverrides(Material variant)
    {
        if (!variant.isVariant) yield break;

        var shader = variant.shader;
        var propertyCount = shader.GetPropertyCount();
        var seenNames = new HashSet<string>();
        for (var i = 0; i < propertyCount; i++)
        {
            if (!variant.IsPropertyOverriden(shader.GetPropertyNameId(i))) continue;
            if (!MaterialProperty.TryGet(variant, i, out var property)) continue;
            if (!seenNames.Add(property.PropertyName)) continue;
            yield return property;
        }
    }

    public static bool GetShaderOverride(Material original, Material overrided, [NotNullWhen(true)] out Shader? targetShader)
    {
        targetShader = null;

        if (original.shader == overrided.shader) return false;

        targetShader = overrided.shader;
        return true;
    }

    private const string CustomRenderQueueProperty = "m_CustomRenderQueue";

    public static int GetCustomRenderQueue(Material material)
    {
        using var so = new SerializedObject(material);
        var customQueueProp = so.FindProperty(CustomRenderQueueProperty);
        return customQueueProp.intValue;
    }

    public static void ApplyCustomRenderQueue(Material material, int renderQueue)
    {
        using var so = new SerializedObject(material);
        var customQueueProp = so.FindProperty(CustomRenderQueueProperty);
        customQueueProp.intValue = renderQueue;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    
    public static bool GetRenderQueueOverride(Material original, Material overrided, out int targetRenderQueue)
    {
        targetRenderQueue = default;

        var originalRenderQueue = GetCustomRenderQueue(original);
        var overridedRenderQueue = GetCustomRenderQueue(overrided);

        if (originalRenderQueue != overridedRenderQueue)
        {
            targetRenderQueue = overridedRenderQueue;
            return true;
        }

        return false;
    }

    public static MaterialOverrideSettings GetOverrides(Material original, Material overrided, 
        bool strict, bool includeExtra, bool includeTextures = true)
    {
        var settings = new MaterialOverrideSettings();

        if (GetShaderOverride(original, overrided, out var targetShader))
        {
            settings.OverrideShader = true;
            settings.TargetShader = targetShader;
        }

        if (GetRenderQueueOverride(original, overrided, out var targetRenderQueue))
        {
            settings.OverrideRenderQueue = true;
            settings.RenderQueueValue = targetRenderQueue;
        }

        var propertyOverrides = GetPropertyOverrides(original, overrided, strict, includeExtra, includeTextures).ToList();
        settings.PropertyOverrides = propertyOverrides;

        return settings;
    }

    public static MaterialOverrideSettings GetVariantOverrides(Material variant)
    {
        var settings = new MaterialOverrideSettings();

        if (!variant.isVariant) return settings;

        var propertyOverrides = GetVariantPropertyOverrides(variant).ToList();
        settings.PropertyOverrides = propertyOverrides;

        return settings;
    }

    public static void ApplyShader(Material editableMaterial, Shader targetShader)
    {
        // Material.shaderを変更するとMaterial.renderQueueが変更先のShader.renderQueueに自動で置き換わる仕様がある
        // ここではShaderのみを変更するため、シェーダー変更前のRenderQueueを保持しておき、変更後に元に戻す
        var savedRenderQueue = GetCustomRenderQueue(editableMaterial);
        editableMaterial.shader = targetShader;
        ApplyCustomRenderQueue(editableMaterial, savedRenderQueue);
    }

    public static void ApplyProperties(Material editableMaterial, List<MaterialProperty> properties)
    {
        foreach (var property in properties)
        {
            property.TrySet(editableMaterial);
        }
    }

    public static void ApplyOverrideSettings(Material editableMaterial, MaterialOverrideSettings overrideSettings)
    {
        if (overrideSettings.OverrideShader)
        {
            var targetShader = overrideSettings.TargetShader;
            if (targetShader == null)
            {
                LocalizedLog.Error("Log:TargetShaderIsNull");
            }
            else
            {
                ApplyShader(editableMaterial, targetShader);
            }
        }

        if (overrideSettings.OverrideRenderQueue)
        {
            ApplyCustomRenderQueue(editableMaterial, overrideSettings.RenderQueueValue);
        }

        ApplyProperties(editableMaterial, overrideSettings.PropertyOverrides);
    }

    public static void CopyPropertiesForSameShader(Material source, Material target)
    {
        if (source.shader != target.shader) throw new Exception("Source and target shaders are different");

        var sourceProperties = GetProperties(source).ToList();
        ApplyProperties(target, sourceProperties);
    }

    // 古いシェーダーに存在してかつ新しいシェーダーに存在しないプロパティが残るのであまり良くない
    // シェーダー未参照のプロパティの削除はここで行うにはコストがかなり高い
    public static void CopyAllSettings(Material source, Material target, bool includeTextures = true)
    {
        ApplyShader(target, source.shader);
        ApplyCustomRenderQueue(target, GetCustomRenderQueue(source));
        CopyPropertiesForSameShader(source, target);
    }
}