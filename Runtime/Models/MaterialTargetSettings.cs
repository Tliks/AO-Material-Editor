using nadena.dev.modular_avatar.core;

namespace Aoyon.MaterialEditor;

[Serializable]
internal class MaterialTargetSettings : IEquatable<MaterialTargetSettings>
{
    public enum SelectionMode
    {
        SingleMaterial,
        BulkMaterials,
        SlotTargets,
        AllMaterials,
    }

    public SelectionMode Mode = SelectionMode.SingleMaterial;
    
    public SingleMaterialTargetSettings SingleMaterial = new();
    public BulkMaterialTargetSettings BulkMaterials = new();
    public SlotTargetSettings SlotTargets = new();
    public AllMaterialSettings AllMaterials = new();

    public MaterialTargetSettings Clone()
    {
        return new MaterialTargetSettings
        {
            Mode = Mode,
            SingleMaterial = SingleMaterial.Clone(),
            BulkMaterials = BulkMaterials.Clone(),
            SlotTargets = SlotTargets.Clone(),
            AllMaterials = AllMaterials.Clone(),
        };
    }

    public void ResolveReferences(Component container)
    {
        SingleMaterial.ResolveReferences(container);
        BulkMaterials.ResolveReferences(container);
        SlotTargets.ResolveReferences(container);
        AllMaterials.ResolveReferences(container);
    }

    public bool Equals(MaterialTargetSettings? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (Mode != other.Mode) return false;

        return Mode switch
        {
            SelectionMode.SingleMaterial => SingleMaterial.Equals(other.SingleMaterial),
            SelectionMode.BulkMaterials => BulkMaterials.Equals(other.BulkMaterials),
            SelectionMode.SlotTargets => SlotTargets.Equals(other.SlotTargets),
            SelectionMode.AllMaterials => AllMaterials.Equals(other.AllMaterials),
            _ => false,
        };
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Mode);
        switch (Mode)
        {
            case SelectionMode.SingleMaterial:
                hash.Add(SingleMaterial);
                break;
            case SelectionMode.BulkMaterials:
                hash.Add(BulkMaterials);
                break;
            case SelectionMode.SlotTargets:
                hash.Add(SlotTargets);
                break;
            case SelectionMode.AllMaterials:
                hash.Add(AllMaterials);
                break;
        }
        return hash.ToHashCode();
    }
}

[Serializable]
internal class SingleMaterialTargetSettings : IEquatable<SingleMaterialTargetSettings>
{
    [MaterialSelector]
    public Material? TargetMaterial = null;
    public bool UseSlotExclusions = false;
    public List<MaterialSlotReference> ExcludedSlots = new();

    public SingleMaterialTargetSettings Clone()
    {
        return new SingleMaterialTargetSettings
        {
            TargetMaterial = TargetMaterial,
            UseSlotExclusions = UseSlotExclusions,
            ExcludedSlots = new List<MaterialSlotReference>(ExcludedSlots.Select(s => s.Clone())),
        };
    }

    public void ResolveReferences(Component container)
    {
        foreach (var slot in ExcludedSlots)
        {
            slot.ResolveReferences(container);
        }
    }

    public bool Equals(SingleMaterialTargetSettings? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return TargetMaterial == other.TargetMaterial
            && UseSlotExclusions == other.UseSlotExclusions
            && ExcludedSlots.SequenceEqual(other.ExcludedSlots);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(TargetMaterial);
        hash.Add(UseSlotExclusions);
        if (UseSlotExclusions) foreach (var slot in ExcludedSlots) hash.Add(slot);
        return hash.ToHashCode();
    }
}

[Serializable]
internal class BulkMaterialTargetSettings : IEquatable<BulkMaterialTargetSettings>
{
    [MaterialSelector]
    public List<Material> TargetMaterials = new();
    public bool UseSlotExclusions = false;
    public List<MaterialSlotReference> ExcludedSlots = new();

    public BulkMaterialTargetSettings Clone()
    {
        return new BulkMaterialTargetSettings
        {
            TargetMaterials = new List<Material>(TargetMaterials),
            UseSlotExclusions = UseSlotExclusions,
            ExcludedSlots = new List<MaterialSlotReference>(ExcludedSlots.Select(s => s.Clone())),
        };
    }

    public void ResolveReferences(Component container)
    {
        foreach (var slot in ExcludedSlots)
        {
            slot.ResolveReferences(container);
        }
    }

    public bool Equals(BulkMaterialTargetSettings? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return TargetMaterials.SequenceEqual(other.TargetMaterials)
            && UseSlotExclusions == other.UseSlotExclusions
            && ExcludedSlots.SequenceEqual(other.ExcludedSlots);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var material in TargetMaterials) hash.Add(material);
        hash.Add(UseSlotExclusions);
        if (UseSlotExclusions) foreach (var slot in ExcludedSlots) hash.Add(slot);
        return hash.ToHashCode();
    }
}

[Serializable]
internal class SlotTargetSettings : IEquatable<SlotTargetSettings>
{
    public List<MaterialSlotReference> TargetSlots = new();

    public SlotTargetSettings Clone()
    {
        return new SlotTargetSettings
        {
            TargetSlots = new List<MaterialSlotReference>(TargetSlots.Select(s => s.Clone())),
        };
    }

    public void ResolveReferences(Component container)
    {
        foreach (var slot in TargetSlots)
        {
            slot.ResolveReferences(container);
        }
    }

    public bool Equals(SlotTargetSettings? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return TargetSlots.SequenceEqual(other.TargetSlots);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var slot in TargetSlots) hash.Add(slot);
        return hash.ToHashCode();
    }
}

[Serializable]
internal class AllMaterialSettings : IEquatable<AllMaterialSettings>
{
    public bool UseExclusions = false;
    [MaterialSelector]
    public List<Material> ExcludedMaterials = new();
    public List<MaterialSlotReference> ExcludedSlots = new();
    public List<AvatarObjectReference> ExcludedObjects = new();

    public AllMaterialSettings Clone()
    {
        return new AllMaterialSettings
        {
            UseExclusions = UseExclusions,
            ExcludedMaterials = new List<Material>(ExcludedMaterials),
            ExcludedSlots = new List<MaterialSlotReference>(ExcludedSlots.Select(s => s.Clone())),
            ExcludedObjects = new List<AvatarObjectReference>(ExcludedObjects.Select(o => o.Clone())),
        };
    }

    public void ResolveReferences(Component container)
    {
        foreach (var slot in ExcludedSlots)
        {
            slot.ResolveReferences(container);
        }

        foreach (var obj in ExcludedObjects)
        {
            obj.Get(container);
        }
    }

    public bool Equals(AllMaterialSettings? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return UseExclusions == other.UseExclusions
            && ExcludedMaterials.SequenceEqual(other.ExcludedMaterials)
            && ExcludedSlots.SequenceEqual(other.ExcludedSlots)
            && ExcludedObjects.SequenceEqual(other.ExcludedObjects);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(UseExclusions);
        if (UseExclusions)
        {
            foreach (var material in ExcludedMaterials) hash.Add(material);
            foreach (var slot in ExcludedSlots) hash.Add(slot);
            foreach (var obj in ExcludedObjects) hash.Add(obj);
        }
        return hash.ToHashCode();
    }
}
