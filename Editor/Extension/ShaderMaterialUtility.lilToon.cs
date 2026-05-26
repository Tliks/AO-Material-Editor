using lilToon;

namespace Aoyon.MaterialEditor.Extension;

[InitializeOnLoad]
internal sealed class LilToonMaterialUtility : IShaderMaterialUtility
{
    static LilToonMaterialUtility()
    {
        ShaderMaterialUtility.Register(new LilToonMaterialUtility());
    }

    public bool Supports(Shader shader) => IsLilToonShader(shader);

    public void Normalize(Material material) => UpdateKeywords(material);

    public bool Unlock(Material material, Material? sourceMaterial = null) => false;

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

}
