using UnityEngine.Pool;
using nadena.dev.modular_avatar.core;

namespace Aoyon.MaterialEditor.Processor;

internal static class MaterialEditorProcessor
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

    public static HashSet<MaterialAssignment> SelectTargetAssignments(
        HashSet<MaterialAssignment> allAssignments, MaterialEditorComponent component, 
        Func<Material, Material, bool>? materialCompare = null, 
        Func<Renderer, Renderer, bool>? rendererCompare = null,
        IObserveContext? observeContext = null)
    {
        materialCompare ??= (a, b) => a == b;
        rendererCompare ??= (a, b) => a == b;
        observeContext ??= new NonObserveContext();

        // read only
        var entrySettings = observeContext.Observe(component, c => c.EntrySettings.Clone(), (a, b) => a.Equals(b));

        var targetMaterials = new HashSet<MaterialAssignment>();
        ResolveEntrySettings();
        return targetMaterials;

        void ResolveEntrySettings()
        {
            switch (entrySettings.Mode)
            {
                case MaterialEntrySettings.ApplyMode.Basic:
                {
                    var targetMaterial = entrySettings.BasicMaterial;
                    if (targetMaterial == null) break;

                    foreach (var materialSlot in allAssignments)
                        if (materialCompare(materialSlot.Material, targetMaterial) is true)
                            targetMaterials.Add(materialSlot);
                    break;
                }
                case MaterialEntrySettings.ApplyMode.Advanced:
                {
                    foreach (var scope in entrySettings.AdvancedTargets)
                        foreach (var materialSlot in ResolveScope(scope))
                            targetMaterials.Add(materialSlot);
                    break;
                }
                case MaterialEntrySettings.ApplyMode.All:
                {
                    foreach (var materialSlot in ResolveAllMaterialTargetScope(entrySettings.AllMaterialTargetScope))
                        targetMaterials.Add(materialSlot);
                    break;
                }
            }
        }

        IEnumerable<MaterialAssignment> ResolveScope(MaterialTargetScope scope)
        {
            switch (scope.Type)
            {
                case MaterialTargetScope.ScopeType.Asset:
                    var targetMaterial = scope.Material;
                    if (targetMaterial == null) yield break;

                    foreach (var materialSlot in allAssignments)
                        if (materialCompare(materialSlot.Material, targetMaterial) is true)
                            yield return materialSlot;
                    break;
                case MaterialTargetScope.ScopeType.Slot:
                    foreach (var materialSlot in ResolveMaterialSlotReference(scope.MaterialSlotReference))
                        yield return materialSlot;
                    break;
            }
        }

        IEnumerable<MaterialAssignment> ResolveAllMaterialTargetScope(AllMaterialTargetScope scope)
        {
            targetMaterials.UnionWith(allAssignments);

            foreach (var excludeTargetScope in scope.ExcludeTargets)
                foreach (var materialSlot in ResolveScope(excludeTargetScope))
                    targetMaterials.Remove(materialSlot);
            foreach (var excludeObjectReference in scope.ExcludeObjectReferences)
                foreach (var materialSlot in ResolveObjectReference(excludeObjectReference))
                    targetMaterials.Remove(materialSlot);
            return targetMaterials;
        }

        IEnumerable<MaterialAssignment> ResolveObjectReference(AvatarObjectReference objectReference)
        {
            var targetObject = objectReference.Get(component);
            if (targetObject == null) yield break;
            
            using var _1 = ListPool<Renderer>.Get(out var childRenderers);
            observeContext.GetComponentsInChildren<Renderer>(targetObject, true, childRenderers);
            foreach (var materialSlot in allAssignments)
                if (childRenderers.Any(r => rendererCompare(materialSlot.SlotId.Renderer, r)) is true)
                    yield return materialSlot;
        }

        IEnumerable<MaterialAssignment> ResolveMaterialSlotReference(MaterialSlotReference reference)
        {
            var targetRenderer = ResolveRendererReference(reference.RendererReference);
            if (targetRenderer == null) yield break;

            if (reference.MaterialIndex == -1)
            {
                foreach (var materialSlot in allAssignments)
                    if (rendererCompare(materialSlot.SlotId.Renderer, targetRenderer) is true)
                        yield return materialSlot;
            }
            else if (reference.MaterialIndex >= 0)
            {
                // all materialsに含まれるかどうかの確認が必要
                foreach (var materialSlot in allAssignments)
                {
                    var slotId = materialSlot.SlotId;
                    if (rendererCompare(slotId.Renderer, targetRenderer) is true && slotId.MaterialIndex == reference.MaterialIndex)
                        yield return materialSlot;
                }
            }
        }

        Renderer? ResolveRendererReference(AvatarObjectReference rendererReference)
        {
            var gameObject = rendererReference.Get(component);
            if (gameObject == null) return null;

            using var _1 = ListPool<Renderer>.Get(out var renderers);
            observeContext.GetComponents<Renderer>(gameObject, renderers);
            if (renderers.Count == 0) return null;

            return renderers[0];
        }
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
                MaterialUtility.ApplyOverrideSettings(edited, settings);
                editedMaterials[(material, settings)] = edited;
            }
            return edited;
        }
    }
}