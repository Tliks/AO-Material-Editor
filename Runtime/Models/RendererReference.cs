using nadena.dev.modular_avatar.core;

namespace Aoyon.MaterialEditor;

[Serializable]
internal class RendererReference : IEquatable<RendererReference>
{
    public AvatarObjectReference ObjectReference = new();
    public int RendererIndex = 0;

    public RendererReference Clone()
    {
        return new RendererReference
        {
            ObjectReference = ObjectReference.Clone(),
            RendererIndex = RendererIndex,
        };
    }

    public void ResolveReferences(Component container)
    {
        ObjectReference.Get(container);
    }

    public bool Equals(RendererReference other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return ObjectReference.Equals(other.ObjectReference) && RendererIndex == other.RendererIndex;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ObjectReference, RendererIndex);
    }
}