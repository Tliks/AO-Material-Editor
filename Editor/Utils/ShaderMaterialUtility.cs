namespace Aoyon.MaterialEditor;

internal interface IShaderMaterialUtility
{
    bool Supports(Shader shader);
    void Normalize(Material material);
    bool Unlock(Material material, Material? sourceMaterial = null);
}

internal static class ShaderMaterialUtility
{
    private static readonly List<IShaderMaterialUtility> Utilities = new();

    public static void Register(IShaderMaterialUtility utility)
    {
        if (Utilities.Any(registered => registered.GetType() == utility.GetType())) return;

        Utilities.Add(utility);
    }

    private static IShaderMaterialUtility? FindUtility(Shader? shader)
    {
        if (shader == null) return null;

        return Utilities.FirstOrDefault(utility => utility.Supports(shader));
    }

    public static void Normalize(Material material)
    {
        var utility = FindUtility(material.shader);
        if (utility == null) return;

        try
        {
            utility.Normalize(material);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AO Material Editor] Failed to normalize material '{material.name}' with {utility.GetType().Name}: {e}");
        }
    }

    public static bool Unlock(Material material, Material? sourceMaterial = null)
    {
        var utility = FindUtility(material.shader);
        if (utility == null) return false;

        try
        {
            return utility.Unlock(material, sourceMaterial);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AO Material Editor] Failed to unlock material '{material.name}' with {utility.GetType().Name}: {e}");
            return false;
        }
    }
}
