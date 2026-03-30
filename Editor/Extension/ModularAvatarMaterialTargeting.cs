using nadena.dev.modular_avatar.core;

namespace Aoyon.MaterialEditor.Processor.Extension;

internal class ModularAvatarMaterialTargeting : IMaterialTargeting
{
    private readonly IMaterialTargeting[] _materialTargetings;

    public ModularAvatarMaterialTargeting(GameObject root)
    {
        _materialTargetings = new IMaterialTargeting[]
        {
            new ModularAvatarMaterialSetterTargeting(root),
            new ModularAvatarMaterialSwapTargeting(root),
        };
    }

    public IEnumerable<MaterialAssignment> GetAssignments(IEnumerable<Renderer> renderers, IObserveContext? context = null)
    {
        foreach (var materialTargeting in _materialTargetings)
        {
            foreach (var assignment in materialTargeting.GetAssignments(renderers, context))
            {
                yield return assignment;
            }
        }
    }

    public IEnumerable<MaterialAssignment> GetAssignments(Renderer renderer, IObserveContext? context = null)
    {
        return GetAssignments(new[] { renderer }, context);
    }

    public void ApplyReplacements(IReadOnlyDictionary<MaterialAssignment, Material> replacements)
    {
        foreach (var materialTargeting in _materialTargetings)
        {
            materialTargeting.ApplyReplacements(replacements);
        }
    }
}

internal class ModularAvatarMaterialSetterTargeting : IMaterialTargeting
{
    private readonly GameObject _root;

    public ModularAvatarMaterialSetterTargeting(GameObject root)
    {
        _root = root;
    }

    public IEnumerable<MaterialAssignment> GetAssignments(IEnumerable<Renderer> renderers, IObserveContext? context = null)
    {
        var rendererSet = renderers.ToHashSet();
        foreach (var setter in _root.GetComponentsInChildren<ModularAvatarMaterialSetter>(true))
        {
            if (setter.Objects == null) continue;

            foreach (var obj in setter.Objects)
            {
                var renderer = ResolveRenderer(obj.Object, setter);
                if (renderer == null || !rendererSet.Contains(renderer)) continue;
                if (obj.Material == null) continue;
                if (obj.MaterialIndex < 0 || obj.MaterialIndex >= renderer.sharedMaterials.Length) continue;

                yield return new MaterialAssignment(new MaterialSlotId(renderer, obj.MaterialIndex), obj.Material);
            }
        }
    }

    public IEnumerable<MaterialAssignment> GetAssignments(Renderer renderer, IObserveContext? context = null)
    {
        return GetAssignments(new[] { renderer }, context);
    }

    public void ApplyReplacements(IReadOnlyDictionary<MaterialAssignment, Material> replacements)
    {
        foreach (var setter in _root.GetComponentsInChildren<ModularAvatarMaterialSetter>(true))
        {
            if (setter.Objects == null) continue;

            foreach (var obj in setter.Objects)
            {
                var renderer = ResolveRenderer(obj.Object, setter);
                if (renderer == null || obj.Material == null) continue;
                if (obj.MaterialIndex < 0 || obj.MaterialIndex >= renderer.sharedMaterials.Length) continue;

                var key = new MaterialAssignment(new MaterialSlotId(renderer, obj.MaterialIndex), obj.Material);
                if (replacements.TryGetValue(key, out var replacement))
                {
                    obj.Material = replacement;
                }
            }
        }
    }

    private static Renderer? ResolveRenderer(AvatarObjectReference? reference, Component container)
    {
        var targetObject = reference?.Get(container);
        return targetObject != null ? targetObject.GetComponent<Renderer>() : null;
    }
}

internal class ModularAvatarMaterialSwapTargeting : IMaterialTargeting
{
    private readonly GameObject _root;
    private readonly IReadOnlyCollection<Renderer> _renderers;

    public ModularAvatarMaterialSwapTargeting(GameObject root)
    {
        _root = root;
        _renderers = MaterialEditorProcessor.GetTargetRenderers(root);
    }

    public IEnumerable<MaterialAssignment> GetAssignments(IEnumerable<Renderer> renderers, IObserveContext? context = null)
    {
        var rendererSet = renderers.ToHashSet();
        foreach (var swap in _root.GetComponentsInChildren<ModularAvatarMaterialSwap>(true))
        {
            if (swap.Swaps == null) continue;

            var swapRoot = swap.Root?.Get(swap);
            foreach (var renderer in rendererSet)
            {
                if (swapRoot != null && !renderer.transform.IsChildOf(swapRoot.transform)) continue;

                foreach (var matSwap in swap.Swaps)
                {
                    if (matSwap.From == null || matSwap.To == null) continue;

                    foreach (var materialIndex in FindMatchingMaterialIndices(renderer, matSwap.From))
                    {
                        yield return new MaterialAssignment(new MaterialSlotId(renderer, materialIndex), matSwap.To);
                    }
                }
            }
        }
    }

    public IEnumerable<MaterialAssignment> GetAssignments(Renderer renderer, IObserveContext? context = null)
    {
        return GetAssignments(new[] { renderer }, context);
    }

    public void ApplyReplacements(IReadOnlyDictionary<MaterialAssignment, Material> replacements)
    {
        foreach (var swap in _root.GetComponentsInChildren<ModularAvatarMaterialSwap>(true))
        {
            if (swap.Swaps == null) continue;

            for (int i = 0; i < swap.Swaps.Count; i++)
            {
                var matSwap = swap.Swaps[i];
                if (matSwap.From == null || matSwap.To == null) continue;

                if (!TryGetReplacement(swap, matSwap, replacements, out var replacement)) continue;

                matSwap.To = replacement;
                swap.Swaps[i] = matSwap;
            }
        }
    }

    private bool TryGetReplacement(
        ModularAvatarMaterialSwap swap,
        MatSwap matSwap,
        IReadOnlyDictionary<MaterialAssignment, Material> replacements,
        out Material? replacement)
    {
        replacement = null;

        var swapRoot = swap.Root?.Get(swap);
        foreach (var renderer in _renderers)
        {
            if (swapRoot != null && !renderer.transform.IsChildOf(swapRoot.transform)) continue;

            foreach (var materialIndex in FindMatchingMaterialIndices(renderer, matSwap.From))
            {
                var key = new MaterialAssignment(new MaterialSlotId(renderer, materialIndex), matSwap.To);
                if (!replacements.TryGetValue(key, out var candidate)) continue;

                if (replacement == null)
                {
                    replacement = candidate;
                }
                else if (replacement != candidate)
                {
                    replacement = null;
                    return false;
                }
            }
        }

        return replacement != null;
    }

    private static IEnumerable<int> FindMatchingMaterialIndices(Renderer renderer, Material from)
    {
        var materials = renderer.sharedMaterials;
        for (int i = 0; i < materials.Length; i++)
        {
            var material = materials[i];
            if (material != null && Utils.OriginalReferenceEquals(material, from))
            {
                yield return i;
            }
        }
    }
}
