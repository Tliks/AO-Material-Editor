namespace Aoyon.MaterialEditor;

[Serializable]
internal class MaterialTargetScope : IEquatable<MaterialTargetScope>
{
    public enum ScopeType
    {
        Asset,
        Slot,
    }

    public ScopeType Type;

    // Asset
    [MaterialSelector]
    public Material? Material = null;

    // Slot
    public RendererReference RendererReference = new();
    public int MaterialIndex = -1; // -1 means all slots

    public MaterialTargetScope Clone()
    {
        return new MaterialTargetScope
        {
            Type = Type,
            Material = Material,
            RendererReference = RendererReference.Clone(),
            MaterialIndex = MaterialIndex,
        };
    }

    public void ResolveReferences(Component container)
    {
        RendererReference.ResolveReferences(container);
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
            return RendererReference.Equals(other.RendererReference) && MaterialIndex == other.MaterialIndex;
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
                hash.Add(RendererReference);
                hash.Add(MaterialIndex);
                break;
        }
        return hash.ToHashCode();
    }
}