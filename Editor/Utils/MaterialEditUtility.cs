using UnityEngine.Rendering;

namespace Aoyon.MaterialEditor;

internal static class MaterialEditUtility
{
    public static void Apply(Material editableMaterial, MaterialEditSettings editSettings)
    {
        foreach (var condition in editSettings.Conditions)
        {
            Apply(editableMaterial, condition);
        }
    }

    public static MaterialOverrideSettings CompileOverrideSettings(Material baseMaterial, MaterialEditSettings editSettings)
    {
        var editedMaterial = new Material(baseMaterial) { name = $"{baseMaterial.name} (Aoyon Material Edit)" };
        try
        {
            MaterialUtility.Unlock(editedMaterial, baseMaterial);
            Apply(editedMaterial, editSettings);
            return MaterialUtility.GetOverrides(baseMaterial, editedMaterial, false, true);
        }
        finally
        {
            Object.DestroyImmediate(editedMaterial);
        }
    }

    private static void Apply(Material material, MaterialEditCondition condition)
    {
        switch (condition.ConditionKind)
        {
            case MaterialEditCondition.Kind.Direct:
                ApplyDirect(material, condition.Direct);
                break;
            case MaterialEditCondition.Kind.ShaderAttribute:
                ApplyShaderAttribute(material, condition.ShaderAttribute);
                break;
            case MaterialEditCondition.Kind.ShaderSpecific:
                ShaderMaterialEditOperationUtility.TryApply(material, condition.ShaderSpecific);
                break;
        }
    }

    private static void ApplyDirect(Material material, MaterialDirectWrite write)
    {
        switch (write.WriteKind)
        {
            case MaterialDirectWrite.Kind.Shader:
                if (write.Shader != null) MaterialUtility.ApplyShader(material, write.Shader);
                break;
            case MaterialDirectWrite.Kind.Property:
                write.Property.TrySet(material);
                break;
            case MaterialDirectWrite.Kind.PropertyTexture:
                if (HasPropertyType(material, GetPropertyName(write), ShaderPropertyType.Texture))
                    material.SetTexture(GetPropertyName(write), write.TextureValue);
                break;
            case MaterialDirectWrite.Kind.PropertyTextureOffset:
                if (HasPropertyType(material, GetPropertyName(write), ShaderPropertyType.Texture))
                    material.SetTextureOffset(GetPropertyName(write), write.Vector2Value);
                break;
            case MaterialDirectWrite.Kind.PropertyTextureScale:
                if (HasPropertyType(material, GetPropertyName(write), ShaderPropertyType.Texture))
                    material.SetTextureScale(GetPropertyName(write), write.Vector2Value);
                break;
            case MaterialDirectWrite.Kind.RenderQueue:
                ApplySerialized(material, so => MaterialUtility.SetCustomRenderQueue(so, write.RenderQueueValue));
                break;
            case MaterialDirectWrite.Kind.LightmapFlags:
                ApplySerialized(material, so => MaterialUtility.SetLightmapFlags(so, write.LightmapFlagsValue));
                break;
            case MaterialDirectWrite.Kind.EnableInstancing:
                ApplySerialized(material, so => MaterialUtility.SetEnableInstancing(so, write.BoolValue));
                break;
            case MaterialDirectWrite.Kind.DoubleSidedGI:
                ApplySerialized(material, so => MaterialUtility.SetDoubleSidedGI(so, write.BoolValue));
                break;
            case MaterialDirectWrite.Kind.Keyword:
                ApplyKeyword(material, write.Keyword);
                break;
            case MaterialDirectWrite.Kind.StringTag:
                ApplyStringTag(material, write.StringTag);
                break;
            case MaterialDirectWrite.Kind.ShaderPass:
                ApplyShaderPass(material, write.ShaderPass);
                break;
        }
    }

    private static void ApplyShaderAttribute(Material material, MaterialShaderAttributeWrite write)
    {
        if (string.IsNullOrEmpty(write.Property.PropertyName)) return;
        if (!IsNumericProperty(write.Property)) return;
        if (!write.Property.TrySet(material)) return;

        switch (write.AttributeKind)
        {
            case MaterialShaderAttributeWrite.Kind.Toggle:
                ApplyKeyword(material, KeywordState(GetToggleKeyword(write, "_ON"), GetNumber(write.Property) != 0));
                break;
            case MaterialShaderAttributeWrite.Kind.ToggleOff:
                ApplyKeyword(material, KeywordState(GetToggleKeyword(write, "_OFF"), GetNumber(write.Property) == 0));
                break;
            case MaterialShaderAttributeWrite.Kind.KeywordEnum:
                ApplyKeywordEnum(material, write);
                break;
        }
    }

    private static void ApplyKeywordEnum(Material material, MaterialShaderAttributeWrite write)
    {
        var selectedIndex = GetNumber(write.Property);
        for (var i = 0; i < write.Keywords.Count; i++)
        {
            ApplyKeyword(material, KeywordState(GetKeywordEnumName(write.Property.PropertyName, write.Keywords[i]), i == selectedIndex));
        }
    }

    private static int GetNumber(MaterialProperty property)
    {
        return property.PropertyType == ShaderPropertyType.Int
            ? property.IntValue
            : (int)property.FloatValue;
    }

    private static bool IsNumericProperty(MaterialProperty property)
    {
        return property.PropertyType is ShaderPropertyType.Int or ShaderPropertyType.Float or ShaderPropertyType.Range;
    }

    private static string GetToggleKeyword(MaterialShaderAttributeWrite write, string suffix)
    {
        return string.IsNullOrEmpty(write.Keyword)
            ? write.Property.PropertyName.ToUpperInvariant() + suffix
            : write.Keyword;
    }

    private static string GetKeywordEnumName(string propertyName, string name)
    {
        return (propertyName + "_" + name).Replace(' ', '_').ToUpperInvariant();
    }

    private static MaterialKeywordStateOverride KeywordState(string keyword, bool enabled)
    {
        return new MaterialKeywordStateOverride { Keyword = keyword, Enabled = enabled };
    }

    private static bool HasPropertyType(Material material, string propertyName, ShaderPropertyType propertyType)
    {
        if (string.IsNullOrEmpty(propertyName)) return false;
        if (material.shader == null) return false;

        var propertyIndex = material.shader.FindPropertyIndex(propertyName);
        return propertyIndex >= 0 && material.shader.GetPropertyType(propertyIndex) == propertyType;
    }

    private static void ApplyKeyword(Material material, MaterialKeywordStateOverride write)
    {
        if (string.IsNullOrEmpty(write.Keyword)) return;

        ApplySerialized(material, so =>
        {
            var keywords = MaterialUtility.GetValidKeywords(so);
            var keywordSet = new HashSet<string>(keywords);

            if (write.Enabled)
            {
                if (keywordSet.Add(write.Keyword)) keywords.Add(write.Keyword);
            }
            else if (keywordSet.Remove(write.Keyword))
            {
                keywords.Remove(write.Keyword);
            }

            MaterialUtility.SetValidKeywords(so, keywords);
        });
    }

    private static void ApplyStringTag(Material material, MaterialStringTagOverride write)
    {
        if (string.IsNullOrEmpty(write.TagName)) return;

        ApplySerialized(material, so =>
        {
            var tags = MaterialUtility.GetStringTags(so);
            if (write.Remove) tags.Remove(write.TagName);
            else tags[write.TagName] = write.Value;
            MaterialUtility.SetStringTags(so, tags);
        });
    }

    private static void ApplyShaderPass(Material material, MaterialShaderPassStateOverride write)
    {
        if (string.IsNullOrEmpty(write.PassName)) return;

        ApplySerialized(material, so =>
        {
            var disabledPasses = MaterialUtility.GetDisabledShaderPasses(so);
            var disabledPassSet = new HashSet<string>(disabledPasses);

            if (write.Enabled)
            {
                if (disabledPassSet.Remove(write.PassName)) disabledPasses.Remove(write.PassName);
            }
            else if (disabledPassSet.Add(write.PassName))
            {
                disabledPasses.Add(write.PassName);
            }

            MaterialUtility.SetDisabledShaderPasses(so, disabledPasses);
        });
    }

    private static void ApplySerialized(Material material, Action<SerializedObject> mutate)
    {
        using var so = new SerializedObject(material);
        mutate(so);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static string GetPropertyName(MaterialDirectWrite write)
    {
        return string.IsNullOrEmpty(write.PropertyName)
            ? write.Property.PropertyName
            : write.PropertyName;
    }
}
