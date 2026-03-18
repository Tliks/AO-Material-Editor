using UnityEngine.Pool;
using nadena.dev.modular_avatar.core;

namespace Aoyon.MaterialEditor.Processor;

internal static partial class MaterialEditorProcessor
{
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

        HashSet<MaterialAssignment> ResolveAllMaterialTargetScope(AllMaterialTargetScope scope)
        {
            var result = new HashSet<MaterialAssignment>();

            result.UnionWith(allAssignments);

            foreach (var excludeTargetScope in scope.ExcludeTargets)
                foreach (var materialSlot in ResolveScope(excludeTargetScope))
                    result.Remove(materialSlot);
            foreach (var excludeObjectReference in scope.ExcludeObjectReferences)
                foreach (var materialSlot in ResolveObjectReference(excludeObjectReference))
                    result.Remove(materialSlot);
                    
            return result;
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

            foreach (var materialSlot in allAssignments)
            {
                if (!rendererCompare(materialSlot.SlotId.Renderer, targetRenderer)) continue;

                if (reference.MaterialIndex == -1)
                {
                    yield return materialSlot;
                    continue;
                }

                var slotId = materialSlot.SlotId;
                if (reference.MaterialIndex >= 0 && slotId.MaterialIndex == reference.MaterialIndex)
                    yield return materialSlot;
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
}