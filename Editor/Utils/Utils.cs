using Aoyon.MaterialEditor.Processor;
using nadena.dev.ndmf;
using nadena.dev.ndmf.runtime;

namespace Aoyon.MaterialEditor;

internal static class Utils
{
    public static GameObject? FindAvatarInParents(GameObject gameObject)
    {
        var root = RuntimeUtil.FindAvatarInParents(gameObject.transform);
        return root != null ? root.gameObject : null;
    }

    public static bool OriginalReferenceEquals(Object a, Object b)
    {
        return ObjectRegistry.GetReference(a).Equals(ObjectRegistry.GetReference(b));
    }

    public static Material CloneAndRegister(Material material, string suffix = "_AO_MaterialEditor")
    {
        var newMaterial = new Material(material) { name = $"{material.name}{suffix}" };
        ObjectRegistry.RegisterReplacedObject(material, newMaterial);
        return newMaterial;
    }

    public static List<Material> GetTargetMaterials(GameObject gameObject)
    {
        return MaterialEditorProcessor.GetTargetRenderers(gameObject)
            .SelectMany(x => x.sharedMaterials)
            .SkipDestroyed()
            .Distinct()
            .ToList();
    }

    public static List<Material> GetMaterials(Renderer renderer)
    {
        return renderer.sharedMaterials
            .SkipDestroyed()
            .Distinct()
            .ToList();
    }
}