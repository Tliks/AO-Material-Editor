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

    public static T CloneAndRegister<T>(T original) where T : Object
    {
        return CloneAndRegister(original, "_AO_MaterialEditor");
    }

    public static T CloneAndRegister<T>(T original, string suffix) where T : Object
    {
        var clone = original is Material material
            ? (T)(Object)new Material(material)
            : Object.Instantiate(original);
        clone.name = $"{original.name}{suffix}";
        ObjectRegistry.RegisterReplacedObject(original, clone);
        return clone;
    }

    public static IEnumerable<Material> GetAllTargetMaterials(GameObject gameObject)
    {
        return MaterialEditorProcessor.GetTargetRenderers(gameObject)
            .SelectMany(x => x.sharedMaterials)
            .SkipDestroyed()
            .Distinct();
    }

    public static IEnumerable<Renderer> GetTargetRenderersUnderInAvatar(GameObject marker, GameObject target)
    {
        var root = FindAvatarInParents(marker);
        if (root == null) return Array.Empty<Renderer>();
        if (!target.transform.IsChildOf(root.transform)) return Array.Empty<Renderer>();

        return MaterialEditorProcessor.GetTargetRenderers(target);
    }

    public static IEnumerable<Material> GetAllTargetMaterialsInAvatar(GameObject marker, Object source)
    {
        switch (source)
        {
            case Material material:
                yield return material;
                yield break;

            case Renderer renderer:
                var root = FindAvatarInParents(marker);
                if (root == null) yield break;
                if (!renderer.transform.IsChildOf(root.transform)) yield break;
                if (!MaterialEditorProcessor.IsTargetRenderer(renderer)) yield break;

                foreach (var material in renderer.sharedMaterials.SkipDestroyed())
                {
                    yield return material;
                }
                yield break;

            case GameObject target:
                foreach (var material in GetTargetRenderersUnderInAvatar(marker, target)
                             .SelectMany(renderer => renderer.sharedMaterials)
                             .SkipDestroyed()
                             .Distinct())
                {
                    yield return material;
                }
                yield break;
        }
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
}
