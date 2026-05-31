using System.Reflection;

namespace Aoyon.MaterialEditor;

[InitializeOnLoad]
internal sealed class StandardMaterialEditOperationHandler : IShaderMaterialEditOperationHandler
{
    private const string ShaderFamilyName = "Standard";
    private const string BlendModeOperation = "blend-mode";
    private const string BlendModeArgument = "blendMode";
    private const string OverrideRenderQueueArgument = "overrideRenderQueue";
    private const string ModeProperty = "_Mode";

    private static readonly Type? StandardShaderGuiType =
        typeof(ShaderGUI).Assembly.GetType("UnityEditor.StandardShaderGUI");

    private static readonly Type? BlendModeType =
        StandardShaderGuiType?.GetNestedType("BlendMode", BindingFlags.Public | BindingFlags.NonPublic);

    private static readonly MethodInfo? SetupMaterialWithBlendModeMethod =
        StandardShaderGuiType?.GetMethod(
            "SetupMaterialWithBlendMode",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

    static StandardMaterialEditOperationHandler()
    {
        ShaderMaterialEditOperationUtility.Register(new StandardMaterialEditOperationHandler());
    }

    public string ShaderFamily => ShaderFamilyName;

    public bool Supports(Shader shader) => shader.name == "Standard";

    public bool TryApply(Material material, MaterialShaderSpecificOperation operation)
    {
        if (operation.OperationVersion != 1) return false;
        if (operation.OperationId != BlendModeOperation) return false;
        if (!operation.TryGetInt(BlendModeArgument, out var blendMode)) return false;
        if (BlendModeType == null || SetupMaterialWithBlendModeMethod == null) return false;

        var overrideRenderQueue = true;
        if (operation.TryGetBool(OverrideRenderQueueArgument, out var overrideRenderQueueValue))
        {
            overrideRenderQueue = overrideRenderQueueValue;
        }

        if (material.HasProperty(ModeProperty))
        {
            material.SetFloat(ModeProperty, blendMode);
        }

        var blendModeValue = Enum.ToObject(BlendModeType, blendMode);
        SetupMaterialWithBlendModeMethod.Invoke(null, new[] { material, blendModeValue, overrideRenderQueue });
        return true;
    }
}
