namespace Presentation.Factories;

public static class ResourceRelations
{
    // Null rather than empty so the key is omitted: empty would be ambiguous between "none referenced" and "none disclosable".
    public static TValue[]? Resolve<TKey, TValue>(IEnumerable<TKey> keys, IReadOnlyDictionary<TKey, TValue> records)
        where TKey : struct
        where TValue : class
    {
        var resolved = keys
            .Distinct()
            .Select(records.GetValueOrDefault)
            .OfType<TValue>()
            .ToArray();

        return resolved.Length == 0 ? null : resolved;
    }
}
