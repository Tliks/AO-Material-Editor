using UnityEngine.Rendering;
using Aoyon.MaterialEditor.Extension;

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

    public static IEnumerable<MaterialProperty> GetShaderDefaultProperties(Shader shader, bool forceTextureNull = false)
    {
        var propertyCount = shader.GetPropertyCount();
        var seenNames = new HashSet<string>();
        for (var i = 0; i < propertyCount; i++)
        {
            if (!MaterialProperty.TryGetDefualtValue(shader, i, out var property, forceTextureNull)) continue;
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

    public static IEnumerable<MaterialProperty> GetVariantPropertyOverrides(Material variant, bool includeTextures = true)
    {
        if (!variant.isVariant) yield break;

        var shader = variant.shader;
        var propertyCount = shader.GetPropertyCount();
        var seenNames = new HashSet<string>();
        for (var i = 0; i < propertyCount; i++)
        {
            if (!variant.IsPropertyOverriden(shader.GetPropertyNameId(i))) continue;
            if (!includeTextures && shader.GetPropertyType(i) == ShaderPropertyType.Texture) continue;
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
        return GetCustomRenderQueue(so);
    }

    public static void ApplyCustomRenderQueue(Material material, int renderQueue)
    {
        using var so = new SerializedObject(material);
        SetCustomRenderQueue(so, renderQueue);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static int CopyCustomRenderQueue(Material source, Material target)
    {
        using var sourceSo = new SerializedObject(source);
        using var targetSo = new SerializedObject(target);
        var renderQueue = GetCustomRenderQueue(sourceSo);
        SetCustomRenderQueue(targetSo, renderQueue);
        targetSo.ApplyModifiedPropertiesWithoutUndo();
        return renderQueue;
    }

    private static int GetCustomRenderQueue(SerializedObject so)
    {
        return so.FindProperty(CustomRenderQueueProperty).intValue;
    }

    private static void SetCustomRenderQueue(SerializedObject so, int renderQueue)
    {
        so.FindProperty(CustomRenderQueueProperty).intValue = renderQueue;
    }

    public static bool GetRenderQueueOverride(Material original, Material overrided, out int targetRenderQueue)
    {
        using var originalSo = new SerializedObject(original);
        using var overridedSo = new SerializedObject(overrided);
        return GetRenderQueueOverride(originalSo, overridedSo, out targetRenderQueue);
    }

    private static bool GetRenderQueueOverride(SerializedObject originalSo, SerializedObject overridedSo, out int targetRenderQueue)
    {
        targetRenderQueue = default;

        var originalRenderQueue = GetCustomRenderQueue(originalSo);
        var overridedRenderQueue = GetCustomRenderQueue(overridedSo);

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
        using var originalSo = new SerializedObject(original);
        using var overridedSo = new SerializedObject(overrided);
        return GetOverrides(original, originalSo, overrided, overridedSo, strict, includeExtra, includeTextures);
    }

    private static MaterialOverrideSettings GetOverrides(
        Material original, SerializedObject originalSo,
        Material overrided, SerializedObject overridedSo,
        bool strict, bool includeExtra, bool includeTextures = true)
    {
        var settings = new MaterialOverrideSettings();

        if (GetShaderOverride(original, overrided, out var targetShader))
        {
            settings.OverrideShader = true;
            settings.TargetShader = targetShader;
        }

        if (GetRenderQueueOverride(originalSo, overridedSo, out var targetRenderQueue))
        {
            settings.OverrideRenderQueue = true;
            settings.RenderQueueValue = targetRenderQueue;
        }

        var propertyOverrides = GetPropertyOverrides(original, overrided, strict, includeExtra, includeTextures).ToList();
        settings.PropertyOverrides = propertyOverrides;

        return settings;
    }

    public static MaterialOverrideSettings GetVariantOverrides(Material variant, bool includeTextures = true)
    {
        var settings = new MaterialOverrideSettings();

        if (!variant.isVariant) return settings;

        var propertyOverrides = GetVariantPropertyOverrides(variant, includeTextures).ToList();
        settings.PropertyOverrides = propertyOverrides;

        return settings;
    }

    public static MaterialOverrideSettings GetTextureReplacementOverrides(Material material, Texture sourceTexture, Texture destinationTexture)
    {
        var overrides = new MaterialOverrideSettings();
        foreach (var property in GetProperties(material))
        {
            if (property.PropertyType != ShaderPropertyType.Texture) continue;
            if (property.TextureValue != sourceTexture) continue;

            var updatedProperty = property;
            updatedProperty.TextureValue = destinationTexture;
            overrides.PropertyOverrides.Add(updatedProperty);
        }

        return overrides;
    }

    public static IEnumerable<Texture> EnumerateTextures(Material material)
    {
        foreach (var property in GetProperties(material))
        {
            if (property.PropertyType != ShaderPropertyType.Texture) continue;
            var texture = property.TextureValue;
            if (texture == null) continue;
            yield return texture;
        }
    }

    public static void ApplyShader(Material editableMaterial, Shader targetShader)
    {
        using var so = new SerializedObject(editableMaterial);
        ApplyShader(editableMaterial, so, targetShader);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ApplyShader(Material editableMaterial, SerializedObject so, Shader targetShader)
    {
        var savedRenderQueue = GetCustomRenderQueue(so);

        editableMaterial.shader = targetShader;
        so.Update();

        // Material.shaderを変更するとCustomRenderQueueが-1(from shader)にリセットされる仕様がある
        // ここではRenderQuqueの変更は意図しないため、シェーダー変更前のRenderQueueを保持しておき、変更後に元に戻す
        SetCustomRenderQueue(so, savedRenderQueue);

        // 新規シェーダーにのみ存在するプロパティはここでデフォルト値が書き込まれる
        // シェーダー変更と同時に発生するこの差分は仕様とする
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
        using var so = new SerializedObject(editableMaterial);
        ApplyOverrideSettings(editableMaterial, so, overrideSettings);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ApplyOverrideSettings(Material editableMaterial, SerializedObject so, MaterialOverrideSettings overrideSettings)
    {
        if (overrideSettings.OverrideShader)
        {
            var targetShader = overrideSettings.TargetShader;
            if (targetShader == null)
            {
                LocalizedLog.Error("error.targetShaderMissing");
            }
            else
            {
                ApplyShader(editableMaterial, so, targetShader);
            }
        }

        if (overrideSettings.OverrideRenderQueue)
        {
            SetCustomRenderQueue(so, overrideSettings.RenderQueueValue);
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
        using var sourceSo = new SerializedObject(source);
        using var targetSo = new SerializedObject(target);
        CopyAllSettings(source, sourceSo, target, targetSo, includeTextures);
        targetSo.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CopyAllSettings(
        Material source, SerializedObject sourceSo,
        Material target, SerializedObject targetSo,
        bool includeTextures = true)
    {
        ApplyShader(target, targetSo, source.shader);
        var renderQueue = GetCustomRenderQueue(sourceSo);
        SetCustomRenderQueue(targetSo, renderQueue);
        CopyPropertiesForSameShader(source, target);
    }

    public static void Unlock(Material editableMaterial, Material? sourceMaterial = null)
    {
        editableMaterial.parent = null;

        if (PoiyomiMaterialUtility.IsPoiyomiMaterial(editableMaterial))
        {
            PoiyomiMaterialUtility.Unlock(editableMaterial, sourceMaterial);
        }
    }
}
