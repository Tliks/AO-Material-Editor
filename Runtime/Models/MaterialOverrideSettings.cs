using UnityEngine.Pool;

namespace Aoyon.MaterialEditor;

[Serializable]
internal class MaterialOverrideSettings : IEquatable<MaterialOverrideSettings>
{
    public bool OverrideShader = false;
    public Shader? TargetShader = null;

    public bool OverrideRenderQueue = false;
    public int RenderQueueValue = 2000;

    public List<MaterialProperty> PropertyOverrides = new();

    public static MaterialOverrideSettings Empty => new();
    
    public static void MergeInto(MaterialOverrideSettings source, MaterialOverrideSettings target)
    {
        if (source.OverrideShader && source.TargetShader != null)
        {
            target.OverrideShader = true;
            target.TargetShader = source.TargetShader;
        }
        if (source.OverrideRenderQueue)
        {
            target.OverrideRenderQueue = true;
            target.RenderQueueValue = source.RenderQueueValue;
        }

        using var _1 = DictionaryPool<string, MaterialProperty>.Get(out var src);
        foreach (var p in source.PropertyOverrides) src[p.PropertyName] = p;
        using var _2 = DictionaryPool<string, MaterialProperty>.Get(out var tgt);
        foreach (var p in target.PropertyOverrides) tgt[p.PropertyName] = p;

        using var _3 = HashSetPool<string>.Get(out var seen);
        var result = new List<MaterialProperty>();
        foreach (var p in target.PropertyOverrides)
        {
            var n = p.PropertyName;
            if (seen.Add(n))
                result.Add(src.TryGetValue(n, out var s) ? s : tgt[n]);
        }
        foreach (var p in source.PropertyOverrides)
        {
            var n = p.PropertyName;
            if (seen.Add(n))
                result.Add(src[n]);
        }
        target.PropertyOverrides = result;
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
        foreach (var property in PropertyOverrides)
        {
            hash.Add(property);
        }
        return hash.ToHashCode();
    }
}