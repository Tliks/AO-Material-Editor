namespace Aoyon.MaterialEditor;

internal interface IShaderMaterialEditOperationHandler
{
    string ShaderFamily { get; }
    bool Supports(Shader shader);
    bool TryApply(Material material, MaterialShaderSpecificOperation operation);
}

internal static class ShaderMaterialEditOperationUtility
{
    private static readonly List<IShaderMaterialEditOperationHandler> Handlers = new();

    public static void Register(IShaderMaterialEditOperationHandler handler)
    {
        if (Handlers.Any(registered => registered.GetType() == handler.GetType())) return;

        Handlers.Add(handler);
    }

    public static bool TryApply(Material material, MaterialShaderSpecificOperation operation)
    {
        if (material.shader == null) return false;

        var handler = Handlers.FirstOrDefault(handler =>
            handler.ShaderFamily == operation.ShaderFamily && handler.Supports(material.shader));
        return handler?.TryApply(material, operation) ?? false;
    }
}
