using nadena.dev.ndmf;
using nadena.dev.ndmf.runtime;

namespace Aoyon.MaterialEditor;

internal static class Utils
{
    public static GameObject? FindAvatarInParents(GameObject gameObject)
    {
        var root = RuntimeUtil.FindAvatarInParents(gameObject.transform);
        return root != null ? root.gameObject : null;
    }

    public static bool OriginalReferenceEquals(Object a, Object b)
    {
        return ObjectRegistry.GetReference(a).Equals(ObjectRegistry.GetReference(b));
    }

    public static Material CloneMaterial(Material material, string suffix = "_AO_MaterialEditor")
    {
        var newMaterial = new Material(material) { name = $"{material.name}{suffix}" };
        ObjectRegistry.RegisterReplacedObject(material, newMaterial);
        return newMaterial;
    }
}

internal static class DictionaryExtensions
{
    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue addValue)
    {
        if (!dict.TryGetValue(key, out var value))
        {
            value = addValue;
            dict[key] = value;
        }
        return value;
    }

    public static TValue GetOrAddNew<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key) where TValue : new()
    {
        if (!dict.TryGetValue(key, out var value))
        {
            value = new TValue();
            dict[key] = value;
        }
        return value;
    }
    
    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, Func<TKey, TValue> valueFactory)
    {
        if (!dict.TryGetValue(key, out var value))
        {
            value = valueFactory(key);
            dict[key] = value;
        }
        return value;
    }
}
