using nadena.dev.modular_avatar.core;

namespace Aoyon.MaterialEditor;

[Serializable]
internal class MaterialEntrySettings : IEquatable<MaterialEntrySettings>
{
    public enum ApplyMode
    {
        Basic,
        Advanced,
        All
    }

    public ApplyMode Mode = ApplyMode.Basic;

    // basic
    [MaterialSelector]
    public Material? BasicMaterial = null;

    // Advanced
    public List<MaterialTargetScope> AdvancedTargets = new();

    // All
    public List<MaterialTargetScope> ExcludeTargets = new();
    public List<AvatarObjectReference> ExcludeObjectReferences = new(); // children renders with all slots

    public MaterialEntrySettings Clone()
    {
        return new MaterialEntrySettings
        {
            Mode = Mode,
            BasicMaterial = BasicMaterial,
            AdvancedTargets = new List<MaterialTargetScope>(AdvancedTargets.Select(t => t.Clone())),
            ExcludeTargets = new List<MaterialTargetScope>(ExcludeTargets.Select(t => t.Clone())),
            ExcludeObjectReferences = new List<AvatarObjectReference>(ExcludeObjectReferences.Select(r => r.Clone())),
        };
    }

    public void ResolveReferences(Component container)
    {
        foreach (var advancedTarget in AdvancedTargets)
        {
            advancedTarget.ResolveReferences(container);
        }
        foreach (var excludeTarget in ExcludeTargets)
        {
            excludeTarget.ResolveReferences(container);
        }
        foreach (var excludeObjectReference in ExcludeObjectReferences)
        {
            excludeObjectReference.Get(container);
        }
    }

    public bool Equals(MaterialEntrySettings? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        if (Mode != other.Mode) return false;

        if (Mode == ApplyMode.Basic)
        {
            return BasicMaterial == other.BasicMaterial;
        }
        else if (Mode == ApplyMode.Advanced)
        {
            return AdvancedTargets.SequenceEqual(other.AdvancedTargets);
        }
        else if (Mode == ApplyMode.All)
        {
            return ExcludeTargets.SequenceEqual(other.ExcludeTargets)
             && ExcludeObjectReferences.SequenceEqual(other.ExcludeObjectReferences);
        }

        return false;
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();

        hash.Add(Mode);

        if (Mode == ApplyMode.Basic)
        {
            hash.Add(BasicMaterial);
        }
        else if (Mode == ApplyMode.Advanced)
        {
            foreach (var advancedTarget in AdvancedTargets)
            {
                hash.Add(advancedTarget);
            }
        }
        else if (Mode == ApplyMode.All)
        {
            foreach (var excludeTarget in ExcludeTargets)
            {
                hash.Add(excludeTarget);
            }
            foreach (var excludeObjectReference in ExcludeObjectReferences)
            {
                hash.Add(excludeObjectReference);
            }
        }

        return hash.ToHashCode();
    }
}