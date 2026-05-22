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

    private static bool IsLilToonShader(Shader shader) => IsLilToonShaderName(shader.name);

    private static void UpdateKeywords(Material material)
    {
        try
        {
            var shaderName = material.shader != null ? material.shader.name : string.Empty;
            var isMulti = lilShaderUtils.IsMultiShaderName(shaderName);

            material.SetFloat("_lilToonVersion", lilConstants.currentVersionValue);
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
            Debug.LogWarning($"Failed to update lilToon material version and keywords: {e}");
        }
    }

    private static bool IsLilToonShaderName(string shaderName)
    {
        return shaderName.Contains("lilToon", StringComparison.OrdinalIgnoreCase)
            || shaderName.Contains("_lil/", StringComparison.Ordinal);
    }
}
