using nadena.dev.ndmf.preview;

namespace Aoyon.MaterialEditor.Processor;

internal static partial class MaterialEditorProcessor
{
    public static bool IsEffective(MaterialEditorComponent component, ComputeContext? observeContext = null)
    {
        observeContext ??= ComputeContext.NullContext;

        var enabled = observeContext.Observe(component, c => c.enabled, (a, b) => a == b);
        if (!enabled) return false;
        var editorOnly = observeContext.EditorOnlyInHierarchy(component.gameObject);
        if (editorOnly) return false;

        return true;
    }

    public static bool IsTargetRenderer(Renderer renderer)
    {
        return renderer is SkinnedMeshRenderer or MeshRenderer;
    }

    public static List<Renderer> GetTargetRenderers(GameObject gameObject, ComputeContext? observeContext = null)
    {
        observeContext ??= ComputeContext.NullContext;
        var renderers = new List<Renderer>();
        observeContext.GetComponentsInChildren<Renderer>(gameObject, true, renderers);
        renderers.RemoveAll(r => !IsTargetRenderer(r));
        return renderers;
    }

    public static Dictionary<MaterialAssignment, MaterialOverrideSettings> BuildOverridePlans(
        IEnumerable<MaterialEditorComponent> components, HashSet<MaterialAssignment> allAssignments,
        Func<Material, Material, bool>? materialCompare = null, 
        Func<Renderer, Renderer, bool>? rendererCompare = null,
        ComputeContext? observeContext = null)
    {
        observeContext ??= ComputeContext.NullContext;
        var plans = new Dictionary<MaterialAssignment, MaterialOverrideSettings>();
        var emptySettings = MaterialOverrideSettings.Empty;
        foreach (var component in components)
        {
            var targetAssignments = SelectTargetAssignments(allAssignments, component, materialCompare, rendererCompare, observeContext);
            if (targetAssignments.Count == 0) continue;

            // read only
            var observed = observeContext.Observe(component, c => c.OverrideSettings.Clone(), (a, b) => a.Equals(b));
            if (observed.Equals(emptySettings)) continue;

            foreach (var assignment in targetAssignments)
            {
                if (!plans.TryGetValue(assignment, out var existingSettings))
                {
                    plans[assignment] = observed.Clone(); // read onlyなのでマージされる方は複製
                }
                else
                {
                    // source(observed)はread only
                    existingSettings.Merge(observed);
                }
            }
        }
        return plans;
    }

    public static Dictionary<MaterialAssignment, Material> BuildReplacements(
        IReadOnlyDictionary<MaterialAssignment, MaterialOverrideSettings> plans,
        Func<Material, Material> clone)
    {
        var editedMaterials = new Dictionary<(Material material, MaterialOverrideSettings settings), Material>();
        var replacements = new Dictionary<MaterialAssignment, Material>();

        foreach (var (assignment, mergedSettings) in plans)
        {
            var before = assignment.Material;
            var after = GetOrCreate(before, mergedSettings);
            if (!ReferenceEquals(before, after))
            {
                replacements[assignment] = after;
            }
        }

        return replacements;

        Material GetOrCreate(Material material, MaterialOverrideSettings settings)
        {
            if (!editedMaterials.TryGetValue((material, settings), out var edited))
            {
                edited = clone(material);
                MaterialUtility.Unlock(edited, material);
                MaterialUtility.ApplyOverrideSettings(edited, settings);
                editedMaterials[(material, settings)] = edited;
            }
            return edited;
        }
    }
}
