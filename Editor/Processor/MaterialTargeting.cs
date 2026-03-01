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

    public IEnumerable<MaterialAssignment> GetAssignments(IReadOnlyList<Renderer> renderers)
    {
        foreach (var materialTargeting in _materialTargetings)
        {
            foreach (var assignment in materialTargeting.GetAssignments(renderers))
            {
                yield return assignment;
            }
        }
    }

    public IEnumerable<MaterialAssignment> GetAssignments(Renderer renderer)
    {
        foreach (var materialTargeting in _materialTargetings)
        {
            foreach (var assignment in materialTargeting.GetAssignments(renderer))
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
    IEnumerable<MaterialAssignment> GetAssignments(IReadOnlyList<Renderer> renderers);
    IEnumerable<MaterialAssignment> GetAssignments(Renderer renderer);
    void ApplyReplacements(IReadOnlyDictionary<MaterialAssignment, Material> replacements);
}

internal class DefaultMaterialTargeting : IMaterialTargeting
{
    public IEnumerable<MaterialAssignment> GetAssignments(IReadOnlyList<Renderer> renderers)
    {
        foreach (var renderer in renderers)
        {
            foreach (var assignment in GetAssignments(renderer))
            {
                yield return assignment;
            }
        }
    }

    public IEnumerable<MaterialAssignment> GetAssignments(Renderer renderer)
    {
        var materials = renderer.sharedMaterials;
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

    public IEnumerable<MaterialAssignment> GetAssignments(IReadOnlyList<Renderer> renderers)
    {
        return GetMaterialsImpl(renderers.ToHashSet());
    }

    public IEnumerable<MaterialAssignment> GetAssignments(Renderer renderer)
    {
        return GetMaterialsImpl(new HashSet<Renderer> { renderer });
    }

    private IEnumerable<MaterialAssignment> GetMaterialsImpl(HashSet<Renderer> renderers)
    {
        var result = new List<MaterialAssignment>();
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

            result.Add(new MaterialAssignment(new(renderer, index), material));
        }
        return result;
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
