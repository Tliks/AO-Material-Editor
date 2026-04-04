namespace Aoyon.MaterialEditor;

internal static class CollectionEquality
{
    public static bool SequenceEquals<T>(
        IEnumerable<T> left,
        IEnumerable<T> right)
    {
        return SequenceEquals(left, right, null);
    }

    public static bool SequenceEquals<T>(
        IEnumerable<T> left,
        IEnumerable<T> right,
        IEqualityComparer<T>? elementComparer = null)
    {
        elementComparer ??= EqualityComparer<T>.Default;

        if (ReferenceEquals(left, right)) return true;

        using var leftEnumerator = left.GetEnumerator();
        using var rightEnumerator = right.GetEnumerator();

        while (true)
        {
            var leftMoved = leftEnumerator.MoveNext();
            var rightMoved = rightEnumerator.MoveNext();

            if (leftMoved != rightMoved) return false;
            if (!leftMoved) return true;
            if (!elementComparer.Equals(leftEnumerator.Current, rightEnumerator.Current)) return false;
        }
    }

    public static int GetSequenceHashCode<T>(IEnumerable<T> values)
    {
        return GetSequenceHashCode(values, null);
    }

    public static bool SetEquals<T>(
        IEnumerable<T> left,
        IEnumerable<T> right)
    {
        return SetEquals(left, right, null);
    }

    public static bool SetEquals<T>(
        IEnumerable<T> left,
        IEnumerable<T> right,
        IEqualityComparer<T>? elementComparer = null)
    {
        elementComparer ??= EqualityComparer<T>.Default;

        if (ReferenceEquals(left, right)) return true;

        var leftSet = left.ToHashSet(elementComparer);
        var rightSet = right.ToHashSet(elementComparer);

        return leftSet.SetEquals(rightSet);
    }

    public static int GetSetHashCode<T>(IEnumerable<T> values)
    {
        return GetSetHashCode(values, null);
    }

    public static int GetSetHashCode<T>(
        IEnumerable<T> values,
        IEqualityComparer<T>? elementComparer = null)
    {
        elementComparer ??= EqualityComparer<T>.Default;

        var hash = 0;
        var set = values.ToHashSet(elementComparer);
        foreach (var value in set)
        {
            hash ^= elementComparer.GetHashCode(value!);
        }

        return HashCode.Combine(set.Count, hash);
    }

    public static int GetSequenceHashCode<T>(
        IEnumerable<T> values,
        IEqualityComparer<T>? elementComparer = null)
    {
        elementComparer ??= EqualityComparer<T>.Default;

        var hash = 0;
        var count = 0;
        foreach (var value in values)
        {
            hash = HashCode.Combine(hash, elementComparer.GetHashCode(value!));
            count++;
        }

        return HashCode.Combine(count, hash);
    }

    public static bool DictionaryEquals<TKey, TValue>(
        IReadOnlyDictionary<TKey, TValue> left,
        IReadOnlyDictionary<TKey, TValue> right,
        Func<TValue, TValue, bool> valueEquals)
        where TKey : notnull
    {
        if (ReferenceEquals(left, right)) return true;
        if (left.Count != right.Count) return false;

        foreach (var (key, value) in left)
        {
            if (!right.TryGetValue(key, out var otherValue) || !valueEquals(value, otherValue)) return false;
        }

        return true;
    }

    public static int GetDictionaryHashCode<TKey, TValue>(
        IReadOnlyDictionary<TKey, TValue> values,
        Func<TValue, int> valueHash,
        IEqualityComparer<TKey>? keyComparer = null)
        where TKey : notnull
    {
        keyComparer ??= EqualityComparer<TKey>.Default;

        var hash = 0;
        foreach (var (key, value) in values)
        {
            hash ^= HashCode.Combine(keyComparer.GetHashCode(key), valueHash(value));
        }

        return HashCode.Combine(values.Count, hash);
    }
}
