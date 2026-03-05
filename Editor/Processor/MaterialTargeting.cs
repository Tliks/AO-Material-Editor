using nadena.dev.ndmf.animator;

namespace Aoyon.MaterialEditor.Processor;

internal readonly record struct MaterialSlotId(Renderer Renderer, int MaterialIndex);

internal readonly record struct MaterialAssignment(MaterialSlotId SlotId, Material Material);

internal class MaterialTargeting
{
    private readonly IMaterialTargeting[] _materialTargetings;

    public MaterialTargeting(params IMaterialTargeting[] materialTargetings)
    {
        _materialTargetings = materialTargetings;
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
        foreach (var materialTargeting in _materialTargetings)
        {
            foreach (var assignment in materialTargeting.GetAssignments(renderer, context))
            {
                yield return assignment;
            }
        }
    }

    public void ApplyReplacements(IReadOnlyDictionary<MaterialAssignment, Material> replacements)
    {
        foreach (var materialTargeting in _materialTargetings)
        {
            materialTargeting.ApplyReplacements(replacements);
        }
    }
}

internal interface IMaterialTargeting
{
    IEnumerable<MaterialAssignment> GetAssignments(IEnumerable<Renderer> renderers, IObserveContext? context = null);
    IEnumerable<MaterialAssignment> GetAssignments(Renderer renderer, IObserveContext? context = null);
    void ApplyReplacements(IReadOnlyDictionary<MaterialAssignment, Material> replacements);
}

internal class DefaultMaterialTargeting : IMaterialTargeting
{
    public IEnumerable<MaterialAssignment> GetAssignments(IEnumerable<Renderer> renderers, IObserveContext? context = null)
    {
        foreach (var renderer in renderers)
        {
            foreach (var assignment in GetAssignments(renderer, context))
            {
                yield return assignment;
            }
        }
    }

    public IEnumerable<MaterialAssignment> GetAssignments(Renderer renderer, IObserveContext? context = null)
    {
        context ??= new NonObserveContext();

        var materials = context.Observe(renderer, r => (Material?[])r.sharedMaterials.Clone(), Utils.SequenceEqualReference);
        for (int i = 0; i < materials.Length; i++)
        {
            var material = materials[i];
            if (material == null) continue;
            yield return new MaterialAssignment(new(renderer, i), material);
        }
    }

    public void ApplyReplacements(IReadOnlyDictionary<MaterialAssignment, Material> replacements)
    {
        var renderers = replacements.Keys.Select(k => k.SlotId.Renderer).ToHashSet();
        foreach (var renderer in renderers)
        {
            var materials = renderer.sharedMaterials;
            var modified = false;
            for (int i = 0; i < materials.Length; i++)
            {
                var currentMaterial = materials[i];

                var key = new MaterialAssignment(new(renderer, i), currentMaterial);
                if (!replacements.TryGetValue(key, out var replacement)) continue;

                materials[i] = replacement;
                modified = true;
            }

            if (modified)
            {
                renderer.sharedMaterials = materials.ToArray();
            }
        }
    }
}

internal class AnimatorMaterialTargeting : IMaterialTargeting
{
    private readonly GameObject _root;
    private readonly AnimationIndex _animationIndex;

    private const string materialPropertyPrefix = "m_Materials.Array.data[";

    public AnimatorMaterialTargeting(GameObject root, AnimationIndex animationIndex)
    {
        _root = root;
        _animationIndex = animationIndex;
    }

    public IEnumerable<MaterialAssignment> GetAssignments(IEnumerable<Renderer> renderers, IObserveContext? context = null)
    {
        return GetMaterialsImpl(renderers.ToHashSet(), context);
    }

    public IEnumerable<MaterialAssignment> GetAssignments(Renderer renderer, IObserveContext? context = null)
    {
        return GetMaterialsImpl(new HashSet<Renderer> { renderer }, context);
    }

    private IEnumerable<MaterialAssignment> GetMaterialsImpl(HashSet<Renderer> renderers, IObserveContext? context = null)
    {
        foreach (var (binding, obj) in _animationIndex.GetPPtrReferencedObjectsWithBinding)
        {
            var material = obj as Material;
            if (material == null) continue;

            var propertyName = binding.propertyName;
            if (!propertyName.StartsWith(materialPropertyPrefix)) continue;

            var indexStr = propertyName.Substring(
                materialPropertyPrefix.Length,
                propertyName.Length - materialPropertyPrefix.Length - 1);
            if (!int.TryParse(indexStr, out var index)) continue;

            var target = binding.path == "" ? _root.transform : _root.transform.Find(binding.path);
            if (target == null) continue;

            var renderer = target.GetComponent<Renderer>();
            if (renderer == null) continue;

            if (!renderers.Contains(renderer)) continue;

            yield return new MaterialAssignment(new(renderer, index), material);
        }
    }

    public void ApplyReplacements(IReadOnlyDictionary<MaterialAssignment, Material> replacements)
    {
        _animationIndex.RewriteObjectCurves((binding, obj) =>
        {
            if (TryGetReplacement(binding, obj, out var replacement)) return replacement;
            return obj;
        });

        bool TryGetReplacement(EditorCurveBinding binding, Object obj, [NotNullWhen(true)] out Material? replacement)
        {
            replacement = null;

            if (obj is not Material mat) return false;

            var propertyName = binding.propertyName;
            if (!propertyName.StartsWith(materialPropertyPrefix)) return false;

            var indexStr = propertyName.Substring(
                materialPropertyPrefix.Length,
                propertyName.Length - materialPropertyPrefix.Length - 1);
            if (!int.TryParse(indexStr, out var index)) return false;

            var target = binding.path == "" ? _root.transform : _root.transform.Find(binding.path);
            if (target == null) return false;

            var renderer = target.GetComponent<Renderer>();
            if (renderer == null) return false;

            var key = new MaterialAssignment(new(renderer, index), mat);
            return replacements.TryGetValue(key, out replacement);
        }
    }
}
