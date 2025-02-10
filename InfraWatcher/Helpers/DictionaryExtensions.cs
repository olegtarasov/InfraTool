namespace InfraWatcher.Helpers;

/// <summary>
/// Dictionary extensions.
/// </summary>
public static class DictionaryExtensions
{
    /// <summary>
    /// Adds pairs from <paramref name="source"/> to <paramref name="dictionary"/> silently overwriting
    /// duplicate values.
    /// </summary>
    public static void AddRange<TKey, TValue>(
        this IDictionary<TKey, TValue> dictionary,
        IDictionary<TKey, TValue>? source)
    {
        if (source == null)
            return;
        
        foreach (var pair in source)
        {
            dictionary[pair.Key] = pair.Value;
        }
    }

    /// <summary>
    /// Adds values from <paramref name="source"/> to <paramref name="dictionary"/> deriving keys with
    /// <paramref name="keyFunc"/>. Silently ovewrites duplicate keys.
    /// </summary>
    public static void AddRange<TKey, TValue>(
        this IDictionary<TKey, TValue> dictionary,
        IEnumerable<TValue> source,
        Func<TValue, TKey> keyFunc)
    {
        if (dictionary == null)
            throw new ArgumentNullException(nameof(dictionary));
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (keyFunc == null)
            throw new ArgumentNullException(nameof(keyFunc));

        foreach (var item in source)
        {
            dictionary[keyFunc(item)] = item;
        }
    }
}