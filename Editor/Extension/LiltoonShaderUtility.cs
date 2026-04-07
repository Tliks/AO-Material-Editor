using System.Reflection;

namespace Aoyon.MaterialEditor.Extension;

#if ME_LILTOON
internal static class LiltoonShaderUtility
{
    private const BindingFlags InstanceBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static Type[]? _lilToonInspectorTypes;
    public static Type[] LilToonInspectorTypes => _lilToonInspectorTypes ??= GetDerivedTypesOfLilToonInspector();
    
    private static Type[] GetDerivedTypesOfLilToonInspector()
    {
        return TypeCache.GetTypesDerivedFrom<lilToon.lilToonInspector>()
            .Where(type =>
            {
                if (type.IsAbstract) return false;

                var method = type.GetMethod("ReplaceToCustomShaders", InstanceBindingFlags);
                return method != null && method.DeclaringType == type && method.GetBaseDefinition().DeclaringType != method.DeclaringType;
            })
            .ToArray();
    }

    public static bool TryConvertToLilToonCustomShader(Material editableMaterial, Type inspectorType)
    {
        var inspector = Activator.CreateInstance(inspectorType);
        var convertMethod = inspectorType.GetMethod("ConvertMaterialToCustomShader", InstanceBindingFlags);
        if (convertMethod == null) { return false; }

        try
        {
            convertMethod.Invoke(inspector, new object[] { editableMaterial });
            return true;
        }
        catch
        {
            return false;
        }
    }
}
#endif
