namespace Aoyon.MaterialEditor;

[Serializable]
internal class MaterialTargetScope : IEquatable<MaterialTargetScope>
{
    public enum ScopeType
    {
        Asset,
        Slot,
    }

    public ScopeType Type = ScopeType.Asset;

    // Asset
    [MaterialSelector]
    public Material? Material = null;

    // Slot
    public MaterialSlotReference MaterialSlotReference = new();
    
    public MaterialTargetScope Clone()
    {
        return new MaterialTargetScope
        {
            Type = Type,
            Material = Material,
            MaterialSlotReference = MaterialSlotReference.Clone(),
        };
    }

    public void ResolveReferences(Component container)
    {
        MaterialSlotReference.ResolveReferences(container);
    }

    public bool Equals(MaterialTargetScope other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        if (Type != other.Type) return false;

        if (Type == ScopeType.Asset)
        {
            return Material == other.Material;
        }
        else if (Type == ScopeType.Slot)
        {
            return MaterialSlotReference.Equals(other.MaterialSlotReference);
        }

        return false;
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Type);
        switch (Type)
        {
            case ScopeType.Asset:
                hash.Add(Material);
                break;
            case ScopeType.Slot:
                hash.Add(MaterialSlotReference);
                break;
        }
        return hash.ToHashCode();
    }
}