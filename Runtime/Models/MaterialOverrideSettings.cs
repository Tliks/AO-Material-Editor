using UnityEngine.Pool;

namespace Aoyon.MaterialEditor;

[Serializable]
internal class MaterialOverrideSettings : IEquatable<MaterialOverrideSettings>
{
    public bool OverrideShader = false;
    public Shader? TargetShader = null;

    public bool OverrideRenderQueue = false;
    public int RenderQueueValue = -1; // CustomRenderQueue, -1 means from shader

    public List<MaterialProperty> PropertyOverrides = new();

    public static MaterialOverrideSettings Empty => new();
    
    /// <summary>
    /// sourceを自身にマージする。
    /// sourceが優先され、後ろにあるプロパティが優先される。
    /// 同じプロパティ名は上書きし、新規要素を後ろに追加する。
    /// </summary>
    /// <param name="source"></param>
    /// <param name="target"></param>
    public MaterialOverrideSettings Merge(MaterialOverrideSettings source)
    {
        if (source.OverrideShader && source.TargetShader != null)
        {
            OverrideShader = true;
            TargetShader = source.TargetShader;
        }
        if (source.OverrideRenderQueue)
        {
            OverrideRenderQueue = true;
            RenderQueueValue = source.RenderQueueValue;
        }

        {
            using var _1 = DictionaryPool<string, MaterialProperty>.Get(out var srcDict);
            foreach (var p in source.PropertyOverrides) srcDict[p.PropertyName] = p;
            using var _2 = HashSetPool<string>.Get(out var targetKeys);

            var result = new List<MaterialProperty>(PropertyOverrides.Count + source.PropertyOverrides.Count);

            foreach (var p in this.PropertyOverrides)
            {
                targetKeys.Add(p.PropertyName); 
                result.Add(srcDict.TryGetValue(p.PropertyName, out var s) ? s : p);
            }

            foreach (var p in source.PropertyOverrides)
            {
                if (targetKeys.Add(p.PropertyName)) {
                    result.Add(srcDict[p.PropertyName]);
                }
            }

            PropertyOverrides = result;
        }

        return this;
    }

    public MaterialOverrideSettings Where(Predicate<MaterialProperty> predicate)
    {
        PropertyOverrides = PropertyOverrides.Where(p => predicate(p)).ToList();
        return this;
    }

    public MaterialOverrideSettings Except(IEnumerable<MaterialProperty> properties, bool strict = true)
    {
        using var _ = DictionaryPool<string, MaterialProperty>.Get(out var propertyDict);
        foreach (var property in properties) propertyDict[property.PropertyName] = property;

        return Where(property => 
            !propertyDict.TryGetValue(property.PropertyName, out var other) 
            || !property.EqualsImpl(other, strict));
    }

    public int OverrideCount
    {
        get
        {
            var count = 0;
            if (OverrideShader && TargetShader != null) count++;
            if (OverrideRenderQueue) count++;
            count += PropertyOverrides.Count;
            return count;
        }
    }

    public MaterialOverrideSettings Clone()
    {
        return new MaterialOverrideSettings
        {
            OverrideShader = OverrideShader,
            TargetShader = TargetShader,
            OverrideRenderQueue = OverrideRenderQueue,
            RenderQueueValue = RenderQueueValue,
            PropertyOverrides = new List<MaterialProperty>(PropertyOverrides),
        };
    }

    public bool Equals(MaterialOverrideSettings other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        if (OverrideShader != other.OverrideShader) return false;
        if (TargetShader != other.TargetShader) return false;
        if (OverrideRenderQueue != other.OverrideRenderQueue) return false;
        if (RenderQueueValue != other.RenderQueueValue) return false;
        if (PropertyOverrides.Count != other.PropertyOverrides.Count) return false;
        for (var i = 0; i < PropertyOverrides.Count; i++)
        {
            if (!PropertyOverrides[i].Equals(other.PropertyOverrides[i])) return false;
        }
        return true;
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(OverrideShader);
        hash.Add(TargetShader);
        hash.Add(OverrideRenderQueue);
        hash.Add(RenderQueueValue);
        hash.Add(PropertyOverrides.Count);
        foreach (var property in PropertyOverrides)
        {
            hash.Add(property);
        }
        return hash.ToHashCode();
    }
}