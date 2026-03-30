#pragma warning disable CS0612

using Aoyon.MaterialEditor.Processor;
using UnityEngine.Pool;
using nadena.dev.modular_avatar.core;

namespace Aoyon.MaterialEditor.Migration;

internal class V0 : IMigrator
{
    public int TargetVersion => 0;

    public void MigrateImpl(MaterialEditorComponentBase component)
    {
        V0ToV1(component);
    }

    public void V0ToV1(MaterialEditorComponentBase component)
    {
        if (component is MaterialEditorComponent materialEditorComponent)
        {
            MigrateEditor(materialEditorComponent);
        }
    }

    private static void MigrateEditor(MaterialEditorComponent component)
    {
        var legacy = component.EntrySettings.Clone();
        var migrated = new MaterialTargetSettings();

        switch (legacy.Mode)
        {
            case MaterialEntrySettings.ApplyMode.Basic:
                migrated.Mode = MaterialTargetSettings.SelectionMode.SingleMaterial;
                migrated.SingleMaterial.TargetMaterial = legacy.BasicMaterial;
                break;

            case MaterialEntrySettings.ApplyMode.Advanced:
                MigrateAdvanced(component, legacy, migrated);
                break;

            case MaterialEntrySettings.ApplyMode.All:
                migrated.Mode = MaterialTargetSettings.SelectionMode.AllMaterials;
                var all = migrated.AllMaterials;
                all.UseExclusions = legacy.AllMaterialTargetScope.ExcludeTargets.Count > 0
                    || legacy.AllMaterialTargetScope.ExcludeObjectReferences.Count > 0;

                foreach (var target in legacy.AllMaterialTargetScope.ExcludeTargets)
                {
                    if (target.Type == MaterialTargetScope.ScopeType.Asset && target.Material != null)
                    {
                        all.ExcludedMaterials.Add(target.Material);
                    }
                    else if (target.Type == MaterialTargetScope.ScopeType.Slot)
                    {
                        all.ExcludedSlots.Add(target.MaterialSlotReference.Clone());
                    }
                }

                foreach (var obj in legacy.AllMaterialTargetScope.ExcludeObjectReferences)
                {
                    all.ExcludedObjects.Add(obj.Clone());
                }
                break;
        }

        Normalize(migrated);
        component.TargetSettings = migrated;
        component.ResolveReferences();
    }

    private static void MigrateAdvanced(
        MaterialEditorComponent component,
        MaterialEntrySettings legacy,
        MaterialTargetSettings migrated)
    {
        var advancedTargets = legacy.AdvancedTargets;
        if (advancedTargets.All(t => t.Type == MaterialTargetScope.ScopeType.Asset))
        {
            migrated.Mode = MaterialTargetSettings.SelectionMode.BulkMaterials;
            foreach (var material in advancedTargets.Select(t => t.Material).OfType<Material>().Distinct())
            {
                migrated.BulkMaterials.TargetMaterials.Add(material);
            }
            return;
        }

        migrated.Mode = MaterialTargetSettings.SelectionMode.SlotTargets;
        foreach (var slot in ResolveAdvancedTargetsToSlots(component, legacy))
        {
            migrated.SlotTargets.TargetSlots.Add(slot);
        }
    }

    private static IEnumerable<MaterialSlotReference> ResolveAdvancedTargetsToSlots(
        MaterialEditorComponent component,
        MaterialEntrySettings legacy)
    {
        var root = Utils.FindAvatarInParents(component.gameObject);
        if (root == null) yield break;

        var renderers = MaterialEditorProcessor.GetTargetRenderers(root);
        var allAssignments = new DefaultMaterialTargeting().GetAssignments(renderers).ToHashSet();
        var resolved = ResolveLegacyTargetAssignments(allAssignments, component, legacy);
        var unique = new HashSet<MaterialSlotReference>();

        foreach (var assignment in resolved)
        {
            var slot = new MaterialSlotReference
            {
                RendererReference = new AvatarObjectReference(assignment.SlotId.Renderer.gameObject),
                MaterialIndex = assignment.SlotId.MaterialIndex,
            };
            if (unique.Add(slot))
            {
                yield return slot;
            }
        }
    }

    private static HashSet<MaterialAssignment> ResolveLegacyTargetAssignments(
        HashSet<MaterialAssignment> allAssignments,
        MaterialEditorComponent component,
        MaterialEntrySettings entrySettings)
    {
        var targetMaterials = new HashSet<MaterialAssignment>();

        switch (entrySettings.Mode)
        {
            case MaterialEntrySettings.ApplyMode.Basic:
            {
                var targetMaterial = entrySettings.BasicMaterial;
                if (targetMaterial == null) break;

                foreach (var materialSlot in allAssignments)
                {
                    if (materialSlot.Material == targetMaterial)
                    {
                        targetMaterials.Add(materialSlot);
                    }
                }
                break;
            }

            case MaterialEntrySettings.ApplyMode.Advanced:
                foreach (var scope in entrySettings.AdvancedTargets)
                {
                    foreach (var materialSlot in ResolveScope(scope))
                    {
                        targetMaterials.Add(materialSlot);
                    }
                }
                break;

            case MaterialEntrySettings.ApplyMode.All:
                foreach (var materialSlot in ResolveAllMaterialTargetScope(entrySettings.AllMaterialTargetScope))
                {
                    targetMaterials.Add(materialSlot);
                }
                break;
        }

        return targetMaterials;

        IEnumerable<MaterialAssignment> ResolveScope(MaterialTargetScope scope)
        {
            switch (scope.Type)
            {
                case MaterialTargetScope.ScopeType.Asset:
                    var targetMaterial = scope.Material;
                    if (targetMaterial == null) yield break;

                    foreach (var materialSlot in allAssignments)
                    {
                        if (materialSlot.Material == targetMaterial)
                        {
                            yield return materialSlot;
                        }
                    }
                    break;

                case MaterialTargetScope.ScopeType.Slot:
                    foreach (var materialSlot in ResolveMaterialSlotReference(scope.MaterialSlotReference))
                    {
                        yield return materialSlot;
                    }
                    break;
            }
        }

        HashSet<MaterialAssignment> ResolveAllMaterialTargetScope(AllMaterialTargetScope scope)
        {
            var result = new HashSet<MaterialAssignment>(allAssignments);

            foreach (var excludeTargetScope in scope.ExcludeTargets)
            {
                foreach (var materialSlot in ResolveScope(excludeTargetScope))
                {
                    result.Remove(materialSlot);
                }
            }

            foreach (var excludeObjectReference in scope.ExcludeObjectReferences)
            {
                foreach (var materialSlot in ResolveObjectReference(excludeObjectReference))
                {
                    result.Remove(materialSlot);
                }
            }

            return result;
        }

        IEnumerable<MaterialAssignment> ResolveObjectReference(AvatarObjectReference objectReference)
        {
            var targetObject = objectReference.Get(component);
            if (targetObject == null) yield break;

            using var _ = ListPool<Renderer>.Get(out var childRenderers);
            targetObject.GetComponentsInChildren(true, childRenderers);
            foreach (var materialSlot in allAssignments)
            {
                if (childRenderers.Any(r => r == materialSlot.SlotId.Renderer))
                {
                    yield return materialSlot;
                }
            }
        }

        IEnumerable<MaterialAssignment> ResolveMaterialSlotReference(MaterialSlotReference reference)
        {
            var targetRenderer = ResolveRendererReference(reference.RendererReference);
            if (targetRenderer == null) yield break;

            foreach (var materialSlot in allAssignments)
            {
                if (materialSlot.SlotId.Renderer != targetRenderer) continue;

                if (reference.MaterialIndex == -1 || materialSlot.SlotId.MaterialIndex == reference.MaterialIndex)
                {
                    yield return materialSlot;
                }
            }
        }

        Renderer? ResolveRendererReference(AvatarObjectReference rendererReference)
        {
            var gameObject = rendererReference.Get(component);
            if (gameObject == null) return null;
            return gameObject.GetComponent<Renderer>();
        }
    }

    private static void Normalize(MaterialTargetSettings settings)
    {
        settings.BulkMaterials.TargetMaterials = settings.BulkMaterials.TargetMaterials
            .Distinct()
            .ToList();
        settings.SingleMaterial.ExcludedSlots = settings.SingleMaterial.ExcludedSlots
            .Distinct()
            .ToList();
        settings.BulkMaterials.ExcludedSlots = settings.BulkMaterials.ExcludedSlots
            .Distinct()
            .ToList();
        settings.SlotTargets.TargetSlots = settings.SlotTargets.TargetSlots
            .Distinct()
            .ToList();
        settings.AllMaterials.ExcludedMaterials = settings.AllMaterials.ExcludedMaterials
            .Distinct()
            .ToList();
        settings.AllMaterials.ExcludedSlots = settings.AllMaterials.ExcludedSlots
            .Distinct()
            .ToList();
        settings.AllMaterials.ExcludedObjects = settings.AllMaterials.ExcludedObjects
            .Distinct()
            .ToList();
    }
}
