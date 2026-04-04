using UnityEngine.Pool;
using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf.preview;

namespace Aoyon.MaterialEditor.Processor;

internal static partial class MaterialEditorProcessor
{
    public static HashSet<MaterialAssignment> SelectTargetAssignments(
        HashSet<MaterialAssignment> allAssignments, MaterialEditorComponent component, 
        Func<Material, Material, bool>? materialCompare = null, 
        Func<Renderer, Renderer, bool>? rendererCompare = null,
        ComputeContext? observeContext = null)
    {
        materialCompare ??= (a, b) => a == b;
        rendererCompare ??= (a, b) => a == b;
        observeContext ??= ComputeContext.NullContext;

        // read only
        var targetSettings = observeContext.Observe(component, c => c.TargetSettings.Clone(), (a, b) => a.Equals(b));

        var targets = new HashSet<MaterialAssignment>();
        ResolveTargetSettings();
        return targets;

        void ResolveTargetSettings()
        {
            switch (targetSettings.Mode)
            {
                case MaterialTargetSettings.SelectionMode.SingleMaterial:
                {
                    var targetMaterial = targetSettings.SingleMaterial.TargetMaterial;
                    if (targetMaterial == null) break;

                    foreach (var materialSlot in allAssignments)
                    {
                        if (materialCompare(materialSlot.Material, targetMaterial))
                        {
                            targets.Add(materialSlot);
                        }
                    }

                    if (targetSettings.SingleMaterial.UseSlotExclusions)
                    {
                        ExcludeSlots(targets, targetSettings.SingleMaterial.ExcludedSlots);
                    }
                    break;
                }

                case MaterialTargetSettings.SelectionMode.BulkMaterials:
                {
                    var targetMaterials = targetSettings.BulkMaterials.TargetMaterials;
                    foreach (var targetMaterial in targetMaterials)
                    {
                        foreach (var materialSlot in allAssignments)
                        {
                            if (materialCompare(materialSlot.Material, targetMaterial))
                            {
                                targets.Add(materialSlot);
                            }
                        }
                    }

                    if (targetSettings.BulkMaterials.UseSlotExclusions)
                    {
                        ExcludeSlots(targets, targetSettings.BulkMaterials.ExcludedSlots);
                    }
                    break;
                }

                case MaterialTargetSettings.SelectionMode.SlotTargets:
                {
                    foreach (var reference in targetSettings.SlotTargets.TargetSlots)
                    {
                        foreach (var materialSlot in ResolveMaterialSlotReference(reference))
                        {
                            targets.Add(materialSlot);
                        }
                    }
                    break;
                }

                case MaterialTargetSettings.SelectionMode.AllMaterials:
                {
                    foreach (var materialSlot in allAssignments)
                    {
                        targets.Add(materialSlot);
                    }

                    if (!targetSettings.AllMaterials.UseExclusions) break;

                    foreach (var targetMaterial in targetSettings.AllMaterials.ExcludedMaterials)
                    {
                        targets.RemoveWhere(assignment => materialCompare(assignment.Material, targetMaterial));
                    }

                    ExcludeSlots(targets, targetSettings.AllMaterials.ExcludedSlots);

                    foreach (var excludeObjectReference in targetSettings.AllMaterials.ExcludedObjects)
                    {
                        foreach (var materialSlot in ResolveObjectReference(excludeObjectReference))
                        {
                            targets.Remove(materialSlot);
                        }
                    }
                    break;
                }
            }
        }

        IEnumerable<MaterialAssignment> ResolveObjectReference(AvatarObjectReference objectReference)
        {
            var targetObject = objectReference.Get(component);
            if (targetObject == null) yield break;
            
            using var _1 = ListPool<Renderer>.Get(out var childRenderers);
            observeContext.GetComponentsInChildren<Renderer>(targetObject, true, childRenderers);
            foreach (var materialSlot in allAssignments)
                if (childRenderers.Any(r => rendererCompare(materialSlot.SlotId.Renderer, r)))
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

        void ExcludeSlots(HashSet<MaterialAssignment> result, IEnumerable<MaterialSlotReference> excludedSlots)
        {
            foreach (var excludedSlot in excludedSlots)
            {
                foreach (var materialSlot in ResolveMaterialSlotReference(excludedSlot))
                {
                    result.Remove(materialSlot);
                }
            }
        }
    }
}