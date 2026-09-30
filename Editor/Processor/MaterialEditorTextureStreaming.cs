#if ME_VRCSDK
using UnityEngine.Rendering;

namespace Aoyon.MaterialEditor.Processor;

internal static class VRCCompatibility
{
    public static void AddStreamingMipmapsOverrides(
        IReadOnlyCollection<MaterialAssignment> allAssignments,
        Dictionary<MaterialAssignment, MaterialOverrideSettings> plans)
    {
        var textureReplacements = plans
            .SelectMany(p => GetApplicableProperties(p.Key.Material, p.Value))
            .Where(p => p.PropertyType == ShaderPropertyType.Texture)
            .Select(p => p.TextureValue)
            .SkipDestroyed()
            .OfType<Texture2D>()
            .Distinct()
            .Where(NeedsStreaming)
            .ToDictionary(texture => texture, CloneWithStreaming);
        if (textureReplacements.Count == 0) return;

        foreach (var assignment in allAssignments)
        {
            plans.TryGetValue(assignment, out var settings);
            var correction = CreateTextureOverrides(assignment.Material, settings, textureReplacements);
            if (correction.OverrideCount == 0) continue;

            settings ??= new MaterialOverrideSettings();
            MaterialOverrideSettings.MergeInto(correction, settings);
            plans[assignment] = settings;
        }
    }

    private static IEnumerable<MaterialProperty> GetApplicableProperties(
        Material material, MaterialOverrideSettings settings)
    {
        var shader = settings.OverrideShader && settings.TargetShader != null
            ? settings.TargetShader
            : material.shader;
        return settings.PropertyOverrides.Where(p => p.CanSet(shader));
    }

    private static bool NeedsStreaming(Texture2D texture)
    {
        if (texture.mipmapCount <= 1) return false;
        using var serialized = new SerializedObject(texture);
        return serialized.FindProperty("m_StreamingMipmaps")?.boolValue == false;
    }

    private static Texture2D CloneWithStreaming(Texture2D texture)
    {
        var clone = Utils.CloneAndRegister(texture);
        using var serialized = new SerializedObject(clone);
        serialized.FindProperty("m_StreamingMipmaps").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return clone;
    }

    private static MaterialOverrideSettings CreateTextureOverrides(
        Material material, MaterialOverrideSettings? settings,
        IReadOnlyDictionary<Texture2D, Texture2D> replacements)
    {
        var effectiveProperties = MaterialUtility.GetProperties(material).ToDictionary(p => p.PropertyName);
        if (settings != null)
        {
            foreach (var property in GetApplicableProperties(material, settings))
            {
                effectiveProperties[property.PropertyName] = property;
            }
        }

        var overrides = new MaterialOverrideSettings();
        foreach (var property in effectiveProperties.Values)
        {
            if (property.PropertyType != ShaderPropertyType.Texture) continue;
            if (property.TextureValue is not Texture2D texture || texture == null) continue;
            if (!replacements.TryGetValue(texture, out var replacement)) continue;

            var corrected = property;
            corrected.TextureValue = replacement;
            overrides.PropertyOverrides.Add(corrected);
        }
        return overrides;
    }
}
#endif
