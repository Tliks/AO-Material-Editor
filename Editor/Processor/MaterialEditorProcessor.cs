namespace Aoyon.MaterialEditor.Processor;

internal static partial class MaterialEditorProcessor
{
    public static bool IsEffective(MaterialEditorComponent component, IObserveContext? observeContext = null)
    {
        observeContext ??= new NonObserveContext();

        var enabled = observeContext.Observe(component, c => c.enabled, (a, b) => a == b);
        if (!enabled) return false;
        var editorOnly = observeContext.EditorOnlyInHierarchy(component.gameObject);
        if (editorOnly) return false;

        return true;
    }

    public static List<Renderer> GetTargetRenderers(GameObject gameObject, IObserveContext? observeContext = null)
    {
        observeContext ??= new NonObserveContext();
        var renderers = new List<Renderer>();
        observeContext.GetComponentsInChildren<Renderer>(gameObject, true, renderers);
        renderers.RemoveAll(r => r is not (SkinnedMeshRenderer or MeshRenderer));
        return renderers;
    }

    public static Dictionary<MaterialAssignment, MaterialOverrideSettings> BuildOverridePlans(
        IEnumerable<MaterialEditorComponent> components, HashSet<MaterialAssignment> allAssignments,
        Func<Material, Material, bool>? materialCompare = null, 
        Func<Renderer, Renderer, bool>? rendererCompare = null,
        IObserveContext? observeContext = null)
    {
        var plans = new Dictionary<MaterialAssignment, MaterialOverrideSettings>();
        observeContext ??= new NonObserveContext();
        var emptySettings = MaterialOverrideSettings.Empty;
        foreach (var component in components)
        {
            var targetAssignments = SelectTargetAssignments(allAssignments, component, materialCompare, rendererCompare, observeContext);
            if (targetAssignments.Count == 0) continue;

            var observed = observeContext.Observe(component, c => c.OverrideSettings.Clone(), (a, b) => a.Equals(b));
            if (observed.Equals(emptySettings)) continue;

            foreach (var assignment in targetAssignments)
            {
                if (!plans.TryGetValue(assignment, out var existingSettings))
                {
                    // Extractした設定はクローンされているが、それをNDMFが持っている
                    // この関数内のマージで値を変えてしまうので、NDMF側に波及してループに陥らないように複製
                    plans[assignment] = observed.Clone();
                }
                else
                {
                    // observed(source)はread only
                    MaterialOverrideSettings.MergeInto(observed, existingSettings);
                }
            }
        }
        return plans;
    }

    public static Dictionary<MaterialAssignment, Material> CloneAndApplyOverrides(
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
