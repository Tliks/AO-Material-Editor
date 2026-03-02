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
    public AllMaterialTargetScope AllMaterialTargetScope = new();

    public MaterialEntrySettings Clone()
    {
        return new MaterialEntrySettings
        {
            Mode = Mode,
            BasicMaterial = BasicMaterial,
            AdvancedTargets = new List<MaterialTargetScope>(AdvancedTargets.Select(t => t.Clone())),
            AllMaterialTargetScope = AllMaterialTargetScope.Clone(),
        };
    }

    public void ResolveReferences(Component container)
    {
        foreach (var advancedTarget in AdvancedTargets)
        {
            advancedTarget.ResolveReferences(container);
        }
        AllMaterialTargetScope.ResolveReferences(container);
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
            return AllMaterialTargetScope.Equals(other.AllMaterialTargetScope);
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
            hash.Add(AllMaterialTargetScope);
        }

        return hash.ToHashCode();
    }
}