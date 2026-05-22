using UnityEngine.Pool;

namespace Aoyon.MaterialEditor;

[Serializable]
internal class MaterialOverrideSettings : IEquatable<MaterialOverrideSettings>
{
    public bool OverrideShader = false;
    public Shader? TargetShader = null;

    public bool OverrideRenderQueue = false;
    public int RenderQueueValue = -1; // CustomRenderQueue, -1 means from shader

    public bool OverrideLightmapFlags = false;
    public MaterialGlobalIlluminationFlags LightmapFlagsValue = MaterialGlobalIlluminationFlags.EmissiveIsBlack;

    public bool OverrideEnableInstancing = false;
    public bool EnableInstancingValue = false;

    public bool OverrideDoubleSidedGI = false;
    public bool DoubleSidedGIValue = false;

    public List<MaterialProperty> PropertyOverrides = new(); // valid properties
    public List<MaterialKeywordStateOverride> KeywordStateOverrides = new(); // valid keywords
    public List<MaterialStringTagOverride> StringTagOverrides = new();
    public List<MaterialShaderPassStateOverride> ShaderPassStateOverrides = new();

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
        if (source.OverrideLightmapFlags)
        {
            OverrideLightmapFlags = true;
            LightmapFlagsValue = source.LightmapFlagsValue;
        }
        if (source.OverrideEnableInstancing)
        {
            OverrideEnableInstancing = true;
            EnableInstancingValue = source.EnableInstancingValue;
        }
        if (source.OverrideDoubleSidedGI)
        {
            OverrideDoubleSidedGI = true;
            DoubleSidedGIValue = source.DoubleSidedGIValue;
        }

        PropertyOverrides = MergeByKey(PropertyOverrides, source.PropertyOverrides, item => item.PropertyName);
        KeywordStateOverrides = MergeByKey(KeywordStateOverrides, source.KeywordStateOverrides, item => item.Keyword);
        StringTagOverrides = MergeByKey(StringTagOverrides, source.StringTagOverrides, item => item.TagName);
        ShaderPassStateOverrides = MergeByKey(ShaderPassStateOverrides, source.ShaderPassStateOverrides, item => item.PassName);

        return this;
    }

    private static List<T> MergeByKey<T>(List<T> target, List<T> source, Func<T, string> keySelector)
    {
        using var _1 = DictionaryPool<string, T>.Get(out var srcDict);
        foreach (var item in source) srcDict[keySelector(item)] = item;
        using var _2 = HashSetPool<string>.Get(out var targetKeys);

        var result = new List<T>(target.Count + source.Count);

        foreach (var item in target)
        {
            var key = keySelector(item);
            targetKeys.Add(key);
            result.Add(srcDict.TryGetValue(key, out var sourceItem) ? sourceItem : item);
        }

        foreach (var item in source)
        {
            var key = keySelector(item);
            if (targetKeys.Add(key))
            {
                result.Add(srcDict[key]);
            }
        }

        return result;
    }

    public int OverrideCount
    {
        get
        {
            var count = 0;
            if (OverrideShader && TargetShader != null) count++;
            if (OverrideRenderQueue) count++;
            if (OverrideLightmapFlags) count++;
            if (OverrideEnableInstancing) count++;
            if (OverrideDoubleSidedGI) count++;
            count += PropertyOverrides.Count;
            count += KeywordStateOverrides.Count;
            count += StringTagOverrides.Count;
            count += ShaderPassStateOverrides.Count;
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
            OverrideLightmapFlags = OverrideLightmapFlags,
            LightmapFlagsValue = LightmapFlagsValue,
            OverrideEnableInstancing = OverrideEnableInstancing,
            EnableInstancingValue = EnableInstancingValue,
            OverrideDoubleSidedGI = OverrideDoubleSidedGI,
            DoubleSidedGIValue = DoubleSidedGIValue,
            PropertyOverrides = new List<MaterialProperty>(PropertyOverrides),
            KeywordStateOverrides = new List<MaterialKeywordStateOverride>(KeywordStateOverrides),
            StringTagOverrides = new List<MaterialStringTagOverride>(StringTagOverrides),
            ShaderPassStateOverrides = new List<MaterialShaderPassStateOverride>(ShaderPassStateOverrides),
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
        if (OverrideLightmapFlags != other.OverrideLightmapFlags) return false;
        if (LightmapFlagsValue != other.LightmapFlagsValue) return false;
        if (OverrideEnableInstancing != other.OverrideEnableInstancing) return false;
        if (EnableInstancingValue != other.EnableInstancingValue) return false;
        if (OverrideDoubleSidedGI != other.OverrideDoubleSidedGI) return false;
        if (DoubleSidedGIValue != other.DoubleSidedGIValue) return false;
        if (!ListEquals(PropertyOverrides, other.PropertyOverrides)) return false;
        if (!ListEquals(KeywordStateOverrides, other.KeywordStateOverrides)) return false;
        if (!ListEquals(StringTagOverrides, other.StringTagOverrides)) return false;
        if (!ListEquals(ShaderPassStateOverrides, other.ShaderPassStateOverrides)) return false;
        return true;
    }

    private static bool ListEquals<T>(List<T> self, List<T> other)
        where T : IEquatable<T>
    {
        if (self.Count != other.Count) return false;
        for (var i = 0; i < self.Count; i++)
        {
            if (!self[i].Equals(other[i])) return false;
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
        hash.Add(OverrideLightmapFlags);
        hash.Add(LightmapFlagsValue);
        hash.Add(OverrideEnableInstancing);
        hash.Add(EnableInstancingValue);
        hash.Add(OverrideDoubleSidedGI);
        hash.Add(DoubleSidedGIValue);
        AddListHash(ref hash, PropertyOverrides);
        AddListHash(ref hash, KeywordStateOverrides);
        AddListHash(ref hash, StringTagOverrides);
        AddListHash(ref hash, ShaderPassStateOverrides);
        return hash.ToHashCode();
    }

    private static void AddListHash<T>(ref HashCode hash, List<T> list)
    {
        hash.Add(list.Count);
        foreach (var item in list)
        {
            hash.Add(item);
        }
    }
}
