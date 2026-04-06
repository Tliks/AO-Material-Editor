#if ME_LI

using jp.lilxyzw.lilycalinventory.runtime;

namespace Aoyon.MaterialEditor.Processor.Extension;

internal class LilycalInventoryMaterialTargeting : IMaterialTargeting
{
    private readonly GameObject _root;

    public LilycalInventoryMaterialTargeting(GameObject root)
    {
        _root = root;
    }

    public IEnumerable<MaterialAssignment> GetAssignments(IEnumerable<Renderer> renderers)
    {
        var rendererSet = renderers.ToHashSet();
        foreach (var replacer in EnumerateMaterialReplacers(_root))
        {
            foreach (var assignment in GetAssignments(replacer, rendererSet))
            {
                yield return assignment;
            }
        }
    }

    public IEnumerable<MaterialAssignment> GetAssignments(Renderer renderer)
    {
        return GetAssignments(new[] { renderer });
    }

    public void ApplyReplacements(IReadOnlyDictionary<MaterialAssignment, Material> replacements)
    {
        foreach (var replacer in EnumerateMaterialReplacers(_root))
        {
            ApplyReplacements(replacer, replacements);
        }
    }

    private static IEnumerable<MaterialReplacer> EnumerateMaterialReplacers(GameObject avatarRoot)
    {
        foreach (var component in avatarRoot.GetComponentsInChildren<ItemToggler>(true))
        {
            foreach (var replacer in EnumerateMaterialReplacers(component.parameter))
            {
                yield return replacer;
            }
        }

        foreach (var component in avatarRoot.GetComponentsInChildren<Prop>(true))
        {
            foreach (var replacer in EnumerateMaterialReplacers(component.parameter))
            {
                yield return replacer;
            }
        }

        foreach (var component in avatarRoot.GetComponentsInChildren<AutoDresser>(true))
        {
            foreach (var replacer in EnumerateMaterialReplacers(component.parameter))
            {
                yield return replacer;
            }
        }

        foreach (var component in avatarRoot.GetComponentsInChildren<CostumeChanger>(true))
        {
            foreach (var costume in component.costumes ?? Array.Empty<Costume>())
            {
                foreach (var replacer in EnumerateMaterialReplacers(costume.parametersPerMenu))
                {
                    yield return replacer;
                }
            }
        }

        foreach (var component in avatarRoot.GetComponentsInChildren<SmoothChanger>(true))
        {
            foreach (var frame in component.frames ?? Array.Empty<Frame>())
            {
                foreach (var replacer in EnumerateMaterialReplacers(frame.parametersPerMenu))
                {
                    yield return replacer;
                }
            }
        }
    }

    private static IEnumerable<MaterialReplacer> EnumerateMaterialReplacers(ParametersPerMenu parameters)
    {
        if (parameters.materialReplacers == null) yield break;

        foreach (var replacer in parameters.materialReplacers)
        {
            if (replacer != null)
            {
                yield return replacer;
            }
        }
    }

    private static IEnumerable<MaterialAssignment> GetAssignments(MaterialReplacer replacer, HashSet<Renderer> renderers)
    {
        var renderer = replacer.renderer;
        if (renderer == null || !renderers.Contains(renderer)) yield break;
        if (replacer.replaceTo == null) yield break;

        var length = Mathf.Min(renderer.sharedMaterials.Length, replacer.replaceTo.Length);
        for (int i = 0; i < length; i++)
        {
            var material = replacer.replaceTo[i];
            if (material == null) continue;

            yield return new MaterialAssignment(new MaterialSlotId(renderer, i), material);
        }
    }

    private static void ApplyReplacements(
        MaterialReplacer replacer,
        IReadOnlyDictionary<MaterialAssignment, Material> replacements)
    {
        var renderer = replacer.renderer;
        if (renderer == null || replacer.replaceTo == null) return;

        var length = Mathf.Min(renderer.sharedMaterials.Length, replacer.replaceTo.Length);
        for (int i = 0; i < length; i++)
        {
            var current = replacer.replaceTo[i];
            if (current == null) continue;

            var key = new MaterialAssignment(new MaterialSlotId(renderer, i), current);
            if (replacements.TryGetValue(key, out var replacement))
            {
                replacer.replaceTo[i] = replacement;
            }
        }
    }
}
#endif