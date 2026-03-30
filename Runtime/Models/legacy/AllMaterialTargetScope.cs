using nadena.dev.modular_avatar.core;

namespace Aoyon.MaterialEditor;

[Serializable, Obsolete]
internal class AllMaterialTargetScope : IEquatable<AllMaterialTargetScope>
{
    public List<MaterialTargetScope> ExcludeTargets = new();
    public List<AvatarObjectReference> ExcludeObjectReferences = new(); // children renders with all slots

    public AllMaterialTargetScope Clone()
    {
        return new AllMaterialTargetScope
        {
            ExcludeTargets = new List<MaterialTargetScope>(ExcludeTargets.Select(t => t.Clone())),
            ExcludeObjectReferences = new List<AvatarObjectReference>(ExcludeObjectReferences.Select(r => r.Clone())),
        };
    }

    public void ResolveReferences(Component container)
    {
        foreach (var excludeTarget in ExcludeTargets)
        {
            excludeTarget.ResolveReferences(container);
        }
        foreach (var excludeObjectReference in ExcludeObjectReferences)
        {
            excludeObjectReference.Get(container);
        }
    }

    public bool Equals(AllMaterialTargetScope other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        if (ExcludeTargets.Count != other.ExcludeTargets.Count) return false;
        return ExcludeTargets.SequenceEqual(other.ExcludeTargets) && ExcludeObjectReferences.SequenceEqual(other.ExcludeObjectReferences);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var excludeTarget in ExcludeTargets)
        {
            hash.Add(excludeTarget);
        }
        foreach (var excludeObjectReference in ExcludeObjectReferences)
        {
            hash.Add(excludeObjectReference);
        }
        return hash.ToHashCode();
    }
}