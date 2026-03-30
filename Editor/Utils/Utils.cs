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

    public static IEnumerable<Material> GetAllTargetMaterials(GameObject gameObject)
    {
        return MaterialEditorProcessor.GetTargetRenderers(gameObject)
            .SelectMany(x => x.sharedMaterials)
            .SkipDestroyed()
            .Distinct();
    }

    public static Material[] GetAllTargetMaterialsInAvatar(GameObject marker)
    {
        var root = FindAvatarInParents(marker);
        if (root == null) return Array.Empty<Material>();

        return GetAllTargetMaterials(root).ToArray();
    }

    public static Material[] GetAllTargetMaterialsInAvatar(SerializedProperty property)
    {
        if (!property.TryGetGameObject(out var gameObject)) return Array.Empty<Material>();

        return GetAllTargetMaterialsInAvatar(gameObject);
    }

    public static bool SequenceEqualReference<T>(T?[] a, T?[] b) where T : class
    {
        if (ReferenceEquals(a, b)) return true;
        
        if (a.Length != b.Length) return false;

        for (int i = 0; i < a.Length; i++)
        {
            if (!ReferenceEquals(a[i], b[i])) return false;
        }

        return true;
    }
}