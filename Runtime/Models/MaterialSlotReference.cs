using nadena.dev.modular_avatar.core;

namespace Aoyon.MaterialEditor;

[Serializable]
internal class MaterialSlotReference : IEquatable<MaterialSlotReference>
{
    public AvatarObjectReference RendererReference = new();
    
    // SkinnedMeshRendererとMeshRendererなどは併用可能だけど…
    // まあエッジケースなので0番目固定で良いでしょう…
    // public int RendererIndex = 0;

    public int MaterialIndex = -1; // -1 means all slots

    public MaterialSlotReference Clone()
    {
        return new MaterialSlotReference
        {
            RendererReference = RendererReference.Clone(),
            MaterialIndex = MaterialIndex,
        };
    }

    public void ResolveReferences(Component container)
    {
        RendererReference.Get(container);
    }

    public bool Equals(MaterialSlotReference other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return RendererReference.Equals(other.RendererReference) && MaterialIndex == other.MaterialIndex;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(RendererReference, MaterialIndex);
    }
}