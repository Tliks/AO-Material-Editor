using UnityEngine.Rendering;
using System.Reflection;

namespace Aoyon.MaterialEditor;

internal static class MaterialUtility
{
    private static MethodInfo? _getShaderDefaultTextureMethod;
    private static MethodInfo GetShaderDefaultTextureMethod
    {
        get
        {
            if (_getShaderDefaultTextureMethod != null) return _getShaderDefaultTextureMethod;

            _getShaderDefaultTextureMethod = typeof(EditorMaterialUtility).GetMethod("GetShaderDefaultTexture", BindingFlags.NonPublic | BindingFlags.Static);
            if (_getShaderDefaultTextureMethod == null)
            {
                throw new InvalidOperationException("Failed to find method: EditorMaterialUtility.GetShaderDefaultTexture");
            }
            return _getShaderDefaultTextureMethod;
        }
    }

    private static MethodInfo? _validateMaterialMethod;
    private static MethodInfo? ValidateMaterialMethod
    {
        get
        {
            if (_validateMaterialMethod != null) return _validateMaterialMethod;

            var shaderGuiUtilityType = typeof(ShaderGUI).Assembly.GetType("UnityEditor.ShaderGUIUtility");
            _validateMaterialMethod = shaderGuiUtilityType?.GetMethod("ValidateMaterial", BindingFlags.NonPublic | BindingFlags.Static);
            return _validateMaterialMethod;
        }
    }

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
        var shader = material.shader;
        var propertyCount = shader.GetPropertyCount();
        var seenNames = new HashSet<string>();
        for (var i = 0; i < propertyCount; i++)
        {
            // なんか同名で複数の値が存在することがあるらしい？エッジケースだと思うけど
            // 前と後のどちらが優先されるかは未確認。ここでは実装の簡単のために、前を採用
            // Todo: ちゃんと調べる
            if (!MaterialProperty.TryGet(material, i, out var property)) continue;
            if (!seenNames.Add(property.PropertyName)) continue;
            yield return property;
        }
    }

    public static IEnumerable<string> EnumeratePropertyNames(Shader shader)
    {
        var propertyCount = shader.GetPropertyCount();
        var seenNames = new HashSet<string>();
        for (var i = 0; i < propertyCount; i++)
        {
            var name = shader.GetPropertyName(i);
            if (!seenNames.Add(name)) continue;
            yield return name;
        }
    }

    public static IEnumerable<MaterialProperty> GetShaderDefaultProperties(Shader shader)
    {
        var propertyCount = shader.GetPropertyCount();
        var seenNames = new HashSet<string>();
        var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(shader)) as ShaderImporter;
        for (var i = 0; i < propertyCount; i++)
        {
            if (!MaterialProperty.TryGetDefualtValue(shader, i, GetDefaultTexture, out var property)) continue;
            if (!seenNames.Add(property.PropertyName)) continue;
            yield return property;
        }

        Texture? GetDefaultTexture(string propertyName)
        {
            Texture? defaultTexture = null;
            if (importer != null)
                defaultTexture = importer.GetDefaultTexture(propertyName);
            if (defaultTexture == null)
                defaultTexture = (Texture)GetShaderDefaultTextureMethod.Invoke(null, new object[] { shader, propertyName });
            return defaultTexture;
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

    private const string ShaderProperty = "m_Shader";
    private const string CustomRenderQueueProperty = "m_CustomRenderQueue";
    private const string LightmapFlagsProperty = "m_LightmapFlags";
    private const string EnableInstancingVariantsProperty = "m_EnableInstancingVariants";
    private const string DoubleSidedGIProperty = "m_DoubleSidedGI";
    private const string ValidKeywordsProperty = "m_ValidKeywords";
    private const string InvalidKeywordsProperty = "m_InvalidKeywords";
    private const string SavedPropertiesProperty = "m_SavedProperties";
    private const string StringTagMapProperty = "stringTagMap";
    private const string DisabledShaderPassesProperty = "disabledShaderPasses";

    public static int GetCustomRenderQueue(SerializedObject so) => so.FindProperty(CustomRenderQueueProperty).intValue;

    public static void SetCustomRenderQueue(SerializedObject so, int renderQueue) => so.FindProperty(CustomRenderQueueProperty).intValue = renderQueue;

    public static MaterialGlobalIlluminationFlags GetLightmapFlags(SerializedObject so) => (MaterialGlobalIlluminationFlags)so.FindProperty(LightmapFlagsProperty).intValue;

    public static void SetLightmapFlags(SerializedObject so, MaterialGlobalIlluminationFlags lightmapFlags) => so.FindProperty(LightmapFlagsProperty).intValue = (int)lightmapFlags;

    public static bool GetEnableInstancing(SerializedObject so) => so.FindProperty(EnableInstancingVariantsProperty).boolValue;

    public static void SetEnableInstancing(SerializedObject so, bool enableInstancing) => so.FindProperty(EnableInstancingVariantsProperty).boolValue = enableInstancing;

    public static bool GetDoubleSidedGI(SerializedObject so) => so.FindProperty(DoubleSidedGIProperty).boolValue;

    public static void SetDoubleSidedGI(SerializedObject so, bool doubleSidedGI) => so.FindProperty(DoubleSidedGIProperty).boolValue = doubleSidedGI;

    public static List<string> GetValidKeywords(SerializedObject so) => so.FindProperty(ValidKeywordsProperty).GetStringArrayValues();
    public static void SetValidKeywords(SerializedObject so, IReadOnlyList<string> values) => so.FindProperty(ValidKeywordsProperty).SetStringArrayValues(values);

    public static Dictionary<string, string> GetStringTags(SerializedObject so) => so.FindProperty(StringTagMapProperty).GetStringMapValues();
    public static void SetStringTags(SerializedObject so, IReadOnlyDictionary<string, string> values) => so.FindProperty(StringTagMapProperty).SetStringMapValues(values);

    public static List<string> GetDisabledShaderPasses(SerializedObject so) => so.FindProperty(DisabledShaderPassesProperty).GetStringArrayValues();
    public static void SetDisabledShaderPasses(SerializedObject so, IReadOnlyList<string> values) => so.FindProperty(DisabledShaderPassesProperty).SetStringArrayValues(values);

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

    private static bool GetLightmapFlagsOverride(SerializedObject originalSo, SerializedObject overridedSo, out MaterialGlobalIlluminationFlags targetLightmapFlags)
    {
        targetLightmapFlags = default;

        var originalLightmapFlags = GetLightmapFlags(originalSo);
        var overridedLightmapFlags = GetLightmapFlags(overridedSo);

        if (originalLightmapFlags == overridedLightmapFlags) return false;

        targetLightmapFlags = overridedLightmapFlags;
        return true;
    }

    private static bool GetEnableInstancingOverride(SerializedObject originalSo, SerializedObject overridedSo, out bool targetEnableInstancing)
    {
        targetEnableInstancing = default;

        var originalEnableInstancing = GetEnableInstancing(originalSo);
        var overridedEnableInstancing = GetEnableInstancing(overridedSo);

        if (originalEnableInstancing == overridedEnableInstancing) return false;

        targetEnableInstancing = overridedEnableInstancing;
        return true;
    }

    private static bool GetDoubleSidedGIOverride(SerializedObject originalSo, SerializedObject overridedSo, out bool targetDoubleSidedGI)
    {
        targetDoubleSidedGI = default;

        var originalDoubleSidedGI = GetDoubleSidedGI(originalSo);
        var overridedDoubleSidedGI = GetDoubleSidedGI(overridedSo);

        if (originalDoubleSidedGI == overridedDoubleSidedGI) return false;

        targetDoubleSidedGI = overridedDoubleSidedGI;
        return true;
    }

    private static List<MaterialKeywordStateOverride> GetKeywordStateOverrides(SerializedObject originalSo, SerializedObject overridedSo)
    {
        var originalKeywords = GetValidKeywords(originalSo);
        var overridedKeywords = GetValidKeywords(overridedSo);
        var originalKeywordSet = new HashSet<string>(originalKeywords);
        var overridedKeywordSet = new HashSet<string>(overridedKeywords);
        var overrides = new List<MaterialKeywordStateOverride>();

        foreach (var keyword in overridedKeywords)
        {
            if (originalKeywordSet.Contains(keyword)) continue;
            overrides.Add(new MaterialKeywordStateOverride { Keyword = keyword, Enabled = true });
        }

        foreach (var keyword in originalKeywords)
        {
            if (overridedKeywordSet.Contains(keyword)) continue;
            overrides.Add(new MaterialKeywordStateOverride { Keyword = keyword, Enabled = false });
        }

        return overrides;
    }

    private static List<MaterialStringTagOverride> GetStringTagOverrides(SerializedObject originalSo, SerializedObject overridedSo)
    {
        var originalTags = GetStringTags(originalSo);
        var overridedTags = GetStringTags(overridedSo);

        var overrides = new List<MaterialStringTagOverride>();
        foreach (var (tagName, value) in overridedTags)
        {
            if (originalTags.TryGetValue(tagName, out var originalValue) && originalValue == value) continue;

            overrides.Add(new MaterialStringTagOverride
            {
                TagName = tagName,
                Value = value,
                Remove = false,
            });
        }

        foreach (var tagName in originalTags.Keys)
        {
            if (overridedTags.ContainsKey(tagName)) continue;

            overrides.Add(new MaterialStringTagOverride
            {
                TagName = tagName,
                Value = string.Empty,
                Remove = true,
            });
        }

        return overrides;
    }

    private static List<MaterialShaderPassStateOverride> GetShaderPassStateOverrides(SerializedObject originalSo, SerializedObject overridedSo)
    {
        var originalDisabledPasses = GetDisabledShaderPasses(originalSo);
        var overridedDisabledPasses = GetDisabledShaderPasses(overridedSo);
        var originalDisabledPassSet = new HashSet<string>(originalDisabledPasses);
        var overridedDisabledPassSet = new HashSet<string>(overridedDisabledPasses);
        var overrides = new List<MaterialShaderPassStateOverride>();

        foreach (var passName in overridedDisabledPasses)
        {
            if (originalDisabledPassSet.Contains(passName)) continue;
            overrides.Add(new MaterialShaderPassStateOverride { PassName = passName, Enabled = false });
        }

        foreach (var passName in originalDisabledPasses)
        {
            if (overridedDisabledPassSet.Contains(passName)) continue;
            overrides.Add(new MaterialShaderPassStateOverride { PassName = passName, Enabled = true });
        }

        return overrides;
    }

    public static MaterialOverrideSettings GetOverrides(Material original, Material overrided, 
        bool strict, bool includeExtra = true, bool includeTextures = true)
    {
        using var originalSo = new SerializedObject(original);
        using var overridedSo = new SerializedObject(overrided);
        return GetOverrides(original, originalSo, overrided, overridedSo, strict, includeExtra, includeTextures);
    }

    private static MaterialOverrideSettings GetOverrides(
        Material original, SerializedObject originalSo,
        Material overrided, SerializedObject overridedSo,
        bool strict, bool includeExtra = true, bool includeTextures = true)
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

        if (GetLightmapFlagsOverride(originalSo, overridedSo, out var targetLightmapFlags))
        {
            settings.OverrideLightmapFlags = true;
            settings.LightmapFlagsValue = targetLightmapFlags;
        }

        if (GetEnableInstancingOverride(originalSo, overridedSo, out var targetEnableInstancing))
        {
            settings.OverrideEnableInstancing = true;
            settings.EnableInstancingValue = targetEnableInstancing;
        }

        if (GetDoubleSidedGIOverride(originalSo, overridedSo, out var targetDoubleSidedGI))
        {
            settings.OverrideDoubleSidedGI = true;
            settings.DoubleSidedGIValue = targetDoubleSidedGI;
        }

        settings.PropertyOverrides = GetPropertyOverrides(original, overrided, strict, includeExtra, includeTextures).ToList();
        settings.KeywordStateOverrides = GetKeywordStateOverrides(originalSo, overridedSo);
        settings.StringTagOverrides = GetStringTagOverrides(originalSo, overridedSo);
        settings.ShaderPassStateOverrides = GetShaderPassStateOverrides(originalSo, overridedSo);

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
        ApplyProperties(editableMaterial, overrideSettings.PropertyOverrides); // so.Applyで巻き戻されないよう後で書く。Todo:soとAPIの統一
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

        if (overrideSettings.OverrideLightmapFlags)
        {
            SetLightmapFlags(so, overrideSettings.LightmapFlagsValue);
        }

        if (overrideSettings.OverrideEnableInstancing)
        {
            SetEnableInstancing(so, overrideSettings.EnableInstancingValue);
        }

        if (overrideSettings.OverrideDoubleSidedGI)
        {
            SetDoubleSidedGI(so, overrideSettings.DoubleSidedGIValue);
        }

        ApplyKeywordStateOverrides(so, overrideSettings.KeywordStateOverrides);
        ApplyStringTagOverrides(so, overrideSettings.StringTagOverrides);
        ApplyShaderPassStateOverrides(so, overrideSettings.ShaderPassStateOverrides);
    }

    public static void CopyAllSettings(Material source, Material target)
    {
        using var sourceSo = new SerializedObject(source);
        using var targetSo = new SerializedObject(target);
        CopyAllSettings(sourceSo, targetSo);
        targetSo.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CopyAllSettings(SerializedObject sourceSo, SerializedObject targetSo)
    {
        CopySerializedProperty(sourceSo, targetSo, ShaderProperty);
        CopySerializedProperty(sourceSo, targetSo, SavedPropertiesProperty);
        CopySerializedProperty(sourceSo, targetSo, CustomRenderQueueProperty);
        CopySerializedProperty(sourceSo, targetSo, LightmapFlagsProperty);
        CopySerializedProperty(sourceSo, targetSo, EnableInstancingVariantsProperty);
        CopySerializedProperty(sourceSo, targetSo, DoubleSidedGIProperty);
        CopySerializedProperty(sourceSo, targetSo, ValidKeywordsProperty);
        CopySerializedProperty(sourceSo, targetSo, InvalidKeywordsProperty);
        CopySerializedProperty(sourceSo, targetSo, StringTagMapProperty);
        CopySerializedProperty(sourceSo, targetSo, DisabledShaderPassesProperty);
    }

    private static void CopySerializedProperty(SerializedObject sourceSo, SerializedObject targetSo, string propertyPath)
    {
        targetSo.CopyFromSerializedProperty(sourceSo.FindProperty(propertyPath));
    }

    private static void ApplyKeywordStateOverrides(SerializedObject so, List<MaterialKeywordStateOverride> overrides)
    {
        var keywords = GetValidKeywords(so);
        var keywordSet = new HashSet<string>(keywords);
        foreach (var item in overrides)
        {
            if (item.Enabled)
            {
                if (keywordSet.Add(item.Keyword)) keywords.Add(item.Keyword);
            }
            else if (keywordSet.Remove(item.Keyword))
            {
                keywords.Remove(item.Keyword);
            }
        }

        SetValidKeywords(so, keywords);
    }

    private static void ApplyStringTagOverrides(SerializedObject so, List<MaterialStringTagOverride> overrides)
    {
        var tags = GetStringTags(so);
        foreach (var item in overrides)
        {
            if (item.Remove)
            {
                tags.Remove(item.TagName);
            }
            else
            {
                tags[item.TagName] = item.Value;
            }
        }

        SetStringTags(so, tags);
    }

    private static void ApplyShaderPassStateOverrides(SerializedObject so, List<MaterialShaderPassStateOverride> overrides)
    {
        var disabledPasses = GetDisabledShaderPasses(so);
        var disabledPassSet = new HashSet<string>(disabledPasses);
        foreach (var item in overrides)
        {
            if (item.Enabled)
            {
                if (disabledPassSet.Remove(item.PassName)) disabledPasses.Remove(item.PassName);
            }
            else if (disabledPassSet.Add(item.PassName))
            {
                disabledPasses.Add(item.PassName);
            }
        }

        SetDisabledShaderPasses(so, disabledPasses);
    }

    public static void Unlock(Material editableMaterial, Material? sourceMaterial = null)
    {
        editableMaterial.parent = null;
        ShaderMaterialUtility.Unlock(editableMaterial, sourceMaterial);
    }

    public static void Normalize(Material material)
    {
        NormalizeByUnityMaterialEditor(material);
        ShaderMaterialUtility.Normalize(material);
    }

    private static void NormalizeByUnityMaterialEditor(Material material)
    {
        try
        {
            UnityEditor.MaterialEditor.ApplyMaterialPropertyDrawers(material);
            ValidateMaterialMethod?.Invoke(null, new object[] { material });
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AO Material Editor] Failed to normalize material '{material.name}' with Unity MaterialEditor: {e}");
        }
    }
}
