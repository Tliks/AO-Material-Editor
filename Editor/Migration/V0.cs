#pragma warning disable CS0612

using Aoyon.MaterialEditor.Processor;
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
        var assetTargets = advancedTargets
            .Where(t => t.Type == MaterialTargetScope.ScopeType.Asset)
            .ToList();
        if (assetTargets.Count == advancedTargets.Count)
        {
            migrated.Mode = MaterialTargetSettings.SelectionMode.BulkMaterials;
            foreach (var material in assetTargets.Select(t => t.Material).OfType<Material>().Distinct())
            {
                migrated.BulkMaterials.TargetMaterials.Add(material);
            }
            return;
        }

        migrated.Mode = MaterialTargetSettings.SelectionMode.SlotTargets;
        foreach (var slot in advancedTargets
                     .Where(t => t.Type == MaterialTargetScope.ScopeType.Slot)
                     .Select(t => t.MaterialSlotReference.Clone()))
        {
            migrated.SlotTargets.TargetSlots.Add(slot);
        }

        foreach (var slot in ResolveAssetTargetsToSlots(component, assetTargets))
        {
            migrated.SlotTargets.TargetSlots.Add(slot);
        }
    }

    private static IEnumerable<MaterialSlotReference> ResolveAssetTargetsToSlots(
        MaterialEditorComponent component,
        IEnumerable<MaterialTargetScope> assetTargets)
    {
        var targetMaterials = assetTargets
            .Select(t => t.Material)
            .OfType<Material>()
            .ToHashSet();
        if (targetMaterials.Count == 0) yield break;

        var root = Utils.FindAvatarInParents(component.gameObject);
        var scopeRoot = root != null ? root : component.transform.root.gameObject;
        var renderers = MaterialEditorProcessor.GetTargetRenderers(scopeRoot);
        var allAssignments = new DefaultMaterialTargeting().GetAssignments(renderers).ToHashSet();
        var unique = new HashSet<MaterialSlotReference>();

        foreach (var assignment in allAssignments)
        {
            if (!targetMaterials.Contains(assignment.Material)) continue;

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
