using Aoyon.MaterialEditor.Processor;
using nadena.dev.modular_avatar.core;

namespace Aoyon.MaterialEditor.UI;

// Todo: リファクタするか直下のみを基準としないもっと良い感じの結合ロジック
internal static class MaterialEditorGenerator
{
    public static void Generate(GameObject selected)
    {
        var selectedAssignments = CollectAssignments(selected).ToList();
        if (selectedAssignments.Count == 0) throw new Exception("No materials found");

        var allSlotsByMaterial = CollectAllSlotsByMaterial(selected, selectedAssignments);
        var groups = BuildGroups(selected.transform, selectedAssignments);

        var root = new GameObject("Material Editor");
        root.transform.SetParent(selected.transform, false);

        GameObject? firstCreated = null;
        foreach (var group in groups)
        {
            CreateGroupEntries(root.transform, group, allSlotsByMaterial, ref firstCreated);
        }

        Undo.RegisterCreatedObjectUndo(root, "Create AO Material Editor");
        EditorGUIUtility.PingObject(firstCreated != null ? firstCreated : root);
        Selection.activeGameObject = firstCreated != null ? firstCreated : root;
    }

    private static IEnumerable<MaterialAssignment> CollectAssignments(GameObject scopeRoot)
    {
        var renderers = MaterialEditorProcessor.GetTargetRenderers(scopeRoot);
        return new DefaultMaterialTargeting().GetAssignments(renderers);
    }

    private static Dictionary<Material, HashSet<MaterialSlotId>> CollectAllSlotsByMaterial(
        GameObject selected,
        IReadOnlyCollection<MaterialAssignment> selectedAssignments)
    {
        var targetMaterials = selectedAssignments.Select(a => a.Material).ToHashSet();
        var searchRoot = Utils.FindAvatarInParents(selected) ?? selected;
        var slotsByMaterial = new Dictionary<Material, HashSet<MaterialSlotId>>();

        foreach (var assignment in CollectAssignments(searchRoot))
        {
            if (!targetMaterials.Contains(assignment.Material)) continue;
            AddSlot(slotsByMaterial, assignment.Material, assignment.SlotId);
        }

        return slotsByMaterial;
    }

    private static List<AssignmentGroup> BuildGroups(Transform selectedTransform, IEnumerable<MaterialAssignment> assignments)
    {
        var directChildren = selectedTransform.Cast<Transform>().ToArray();
        var rootAssignments = new List<MaterialAssignment>();
        var assignmentsByDirectChild = directChildren.ToDictionary(child => child, _ => new List<MaterialAssignment>());

        var rootSlotsByMaterial = new Dictionary<Material, HashSet<MaterialSlotId>>();
        var objectSlotsByTransform = new Dictionary<Transform, Dictionary<Material, HashSet<MaterialSlotId>>>();

        foreach (var assignment in assignments)
        {
            var directChild = FindContainingDirectChild(assignment.SlotId.Renderer.transform);
            if (directChild == null)
            {
                rootAssignments.Add(assignment);
                continue;
            }

            assignmentsByDirectChild[directChild].Add(assignment);
        }

        foreach (var assignment in rootAssignments)
        {
            AddSlot(rootSlotsByMaterial, assignment.Material, assignment.SlotId);
        }

        foreach (var directChild in directChildren)
        {
            var directChildAssignments = assignmentsByDirectChild[directChild];
            if (directChildAssignments.Count == 0) continue;

            var slotsByMaterial = ShouldPlaceDirectChildGroupInRoot(directChild, directChildAssignments)
                ? rootSlotsByMaterial
                : GetOrCreateObjectGroup(directChild);

            foreach (var assignment in directChildAssignments)
            {
                AddSlot(slotsByMaterial, assignment.Material, assignment.SlotId);
            }
        }

        var groups = new List<AssignmentGroup>
        {
            new(null, rootSlotsByMaterial),
        };

        groups.AddRange(objectSlotsByTransform
            .Select(x => new AssignmentGroup(x.Key.name, x.Value)));

        return groups;

        Transform? FindContainingDirectChild(Transform rendererTransform)
        {
            foreach (var directChild in directChildren)
            {
                if (rendererTransform == directChild) return directChild;
                if (rendererTransform.IsChildOf(directChild)) return directChild;
            }

            return null;
        }

        bool ShouldPlaceDirectChildGroupInRoot(
            Transform directChild,
            IEnumerable<MaterialAssignment> childAssignments)
        {
            if (!directChild.TryGetComponent<Renderer>(out var renderer)) return false;
            if (renderer is not (SkinnedMeshRenderer or MeshRenderer)) return false;

            return childAssignments.All(a => a.SlotId.Renderer.transform == directChild);
        }

        Dictionary<Material, HashSet<MaterialSlotId>> GetOrCreateObjectGroup(Transform directChild)
        {
            if (!objectSlotsByTransform.TryGetValue(directChild, out var slotsByMaterial))
            {
                slotsByMaterial = new();
                objectSlotsByTransform.Add(directChild, slotsByMaterial);
            }

            return slotsByMaterial;
        }
    }

    private static void CreateGroupEntries(
        Transform rootParent,
        AssignmentGroup group,
        IReadOnlyDictionary<Material, HashSet<MaterialSlotId>> allSlotsByMaterial,
        ref GameObject? firstCreated)
    {
        var parent = rootParent;
        if (!string.IsNullOrEmpty(group.Name))
        {
            var folder = new GameObject(group.Name);
            folder.transform.SetParent(rootParent, false);
            parent = folder.transform;
        }

        foreach (var (material, includedSlots) in group.SlotsByMaterial)
        {
            var entry = new GameObject(material.name);
            entry.transform.SetParent(parent, false);

            var component = entry.AddComponent<MaterialEditorComponent>();
            component.TargetSettings.Mode = MaterialTargetSettings.SelectionMode.SingleMaterial;
            component.TargetSettings.SingleMaterial.TargetMaterial = material;

            var excludedSlots = allSlotsByMaterial[material]
                .Where(slot => !includedSlots.Contains(slot))
                .Select(slot => new MaterialSlotReference
                {
                    RendererReference = new AvatarObjectReference(slot.Renderer.gameObject),
                    MaterialIndex = slot.MaterialIndex,
                })
                .ToList();

            component.TargetSettings.SingleMaterial.UseSlotExclusions = excludedSlots.Count > 0;
            component.TargetSettings.SingleMaterial.ExcludedSlots.Clear();
            component.TargetSettings.SingleMaterial.ExcludedSlots.AddRange(excludedSlots);

            if (firstCreated == null) firstCreated = entry;
        }
    }

    private static void AddSlot(
        IDictionary<Material, HashSet<MaterialSlotId>> slotsByMaterial,
        Material material,
        MaterialSlotId slotId)
    {
        if (!slotsByMaterial.TryGetValue(material, out var slots))
        {
            slots = new HashSet<MaterialSlotId>();
            slotsByMaterial.Add(material, slots);
        }

        slots.Add(slotId);
    }

    private sealed class AssignmentGroup
    {
        public string? Name { get; }
        public Dictionary<Material, HashSet<MaterialSlotId>> SlotsByMaterial { get; }

        public AssignmentGroup(string? name, Dictionary<Material, HashSet<MaterialSlotId>> slotsByMaterial)
        {
            Name = name;
            SlotsByMaterial = slotsByMaterial;
        }
    }
}
