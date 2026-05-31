using lilToon;

namespace Aoyon.MaterialEditor.Extension;

[InitializeOnLoad]
internal sealed class LilToonMaterialUtility : IShaderMaterialUtility, IShaderMaterialEditOperationHandler
{
    private const string ShaderFamilyName = "lilToon";
    private const string RenderingModeOperation = "rendering-mode";
    private const string OutlineOperation = "outline";
    private const string RenderingModeArgument = "renderingMode";
    private const string TransparentModeArgument = "transparentMode";
    private const string EnabledArgument = "enabled";
    private const string TransparentModeProperty = "_TransparentMode";

    static LilToonMaterialUtility()
    {
        var utility = new LilToonMaterialUtility();
        ShaderMaterialUtility.Register(utility);
        ShaderMaterialEditOperationUtility.Register(utility);
    }

    public string ShaderFamily => ShaderFamilyName;

    public bool Supports(Shader shader) => IsLilToonShader(shader);

    public void Normalize(Material material) => UpdateKeywords(material);

    public bool Unlock(Material material, Material? sourceMaterial = null) => false;

    public bool TryApply(Material material, MaterialShaderSpecificOperation operation)
    {
        if (operation.OperationVersion != 1) return false;

        return operation.OperationId switch
        {
            RenderingModeOperation => TryApplyRenderingMode(material, operation),
            OutlineOperation => TryApplyOutline(material, operation),
            _ => false,
        };
    }

    private static readonly Dictionary<Shader, bool> _islilToonCache = new();
    private static bool IsLilToonShader(Shader shader)
    {
        if (_islilToonCache.TryGetValue(shader, out bool isLilToon)) return isLilToon;
        return _islilToonCache[shader] = IsLilToonShaderImpl(shader);
    }
    private static bool IsLilToonShaderImpl(Shader shader)
    {
        if (shader.name.Contains("lilToon") || shader.name.Contains("lts_pass")) return true;
        var shaderPath = AssetDatabase.GetAssetPath(shader);
        return !string.IsNullOrEmpty(shaderPath) && shaderPath.Contains(".lilcontainer");
    }

    private static void UpdateKeywords(Material material)
    {
        try
        {
            var shaderName = material.shader != null ? material.shader.name : string.Empty;
            var isMulti = lilShaderUtils.IsMultiShaderName(shaderName);

            if (isMulti)
            {
                lilMaterialUtils.SetupMultiMaterial(material);
            }
            else
            {
                lilMaterialUtils.RemoveShaderKeywords(material);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to update lilToon material keywords: {e}");
        }
    }

    private static bool TryApplyRenderingMode(Material material, MaterialShaderSpecificOperation operation)
    {
        if (!operation.TryGetInt(RenderingModeArgument, out var renderingModeValue)) return false;
        if (!Enum.IsDefined(typeof(RenderingMode), renderingModeValue)) return false;

        var renderingMode = (RenderingMode)renderingModeValue;
        var transparentMode = GetTransparentMode(material);
        if (operation.TryGetInt(TransparentModeArgument, out var transparentModeValue))
        {
            if (!Enum.IsDefined(typeof(TransparentMode), transparentModeValue)) return false;
            transparentMode = (TransparentMode)transparentModeValue;
        }

        ApplyRenderingMode(material, renderingMode, transparentMode, GetOutline(material));
        return true;
    }

    private static bool TryApplyOutline(Material material, MaterialShaderSpecificOperation operation)
    {
        if (!operation.TryGetBool(EnabledArgument, out var enabled)) return false;

        ApplyRenderingMode(material, GetRenderingMode(material), GetTransparentMode(material), enabled);
        return true;
    }

    private static void ApplyRenderingMode(
        Material material,
        RenderingMode renderingMode,
        TransparentMode transparentMode,
        bool outline)
    {
        var shaderName = material.shader.name;
        var isLite = lilShaderUtils.IsLiteShaderName(shaderName);
        var isTessellation = lilShaderUtils.IsTessellationShaderName(shaderName);
        var isMulti = lilShaderUtils.IsMultiShaderName(shaderName);

        if (isMulti && material.HasProperty(TransparentModeProperty))
        {
            material.SetFloat(TransparentModeProperty, ToMultiTransparentModeValue(renderingMode));
        }

        lilMaterialUtils.SetupMaterialWithRenderingMode(
            material,
            renderingMode,
            transparentMode,
            outline,
            isLite,
            isTessellation,
            isMulti);
    }

    private static RenderingMode GetRenderingMode(Material material)
    {
        var shaderName = material.shader.name;
        if (lilShaderUtils.IsMultiShaderName(shaderName) && material.HasProperty(TransparentModeProperty))
        {
            return (int)material.GetFloat(TransparentModeProperty) switch
            {
                1 => RenderingMode.Cutout,
                2 => RenderingMode.Transparent,
                3 => RenderingMode.Refraction,
                4 => RenderingMode.Fur,
                5 => RenderingMode.FurCutout,
                6 => RenderingMode.Gem,
                _ => RenderingMode.Opaque,
            };
        }

        if (lilShaderUtils.IsRefractionBlurShaderName(shaderName)) return RenderingMode.RefractionBlur;
        if (lilShaderUtils.IsRefractionShaderName(shaderName)) return RenderingMode.Refraction;
        if (lilShaderUtils.IsFurTwoPassShaderName(shaderName)) return RenderingMode.FurTwoPass;
        if (lilShaderUtils.IsFurCutoutShaderName(shaderName)) return RenderingMode.FurCutout;
        if (lilShaderUtils.IsFurShaderName(shaderName)) return RenderingMode.Fur;
        if (lilShaderUtils.IsGemShaderName(shaderName)) return RenderingMode.Gem;
        if (lilShaderUtils.IsCutoutShaderName(shaderName)) return RenderingMode.Cutout;
        if (lilShaderUtils.IsTransparentShaderName(shaderName) || lilShaderUtils.IsOverlayShaderName(shaderName)) return RenderingMode.Transparent;
        return RenderingMode.Opaque;
    }

    private static TransparentMode GetTransparentMode(Material material)
    {
        var shaderName = material.shader.name;
        if (lilShaderUtils.IsTwoPassShaderName(shaderName)) return TransparentMode.TwoPass;
        if (lilShaderUtils.IsOnePassShaderName(shaderName)) return TransparentMode.OnePass;
        return TransparentMode.Normal;
    }

    private static bool GetOutline(Material material)
    {
        return lilShaderUtils.IsOutlineShaderName(material.shader.name);
    }

    private static float ToMultiTransparentModeValue(RenderingMode renderingMode)
    {
        return renderingMode switch
        {
            RenderingMode.Cutout => 1f,
            RenderingMode.Transparent => 2f,
            RenderingMode.Refraction => 3f,
            RenderingMode.Fur => 4f,
            RenderingMode.FurCutout => 5f,
            RenderingMode.Gem => 6f,
            _ => 0f,
        };
    }

}
