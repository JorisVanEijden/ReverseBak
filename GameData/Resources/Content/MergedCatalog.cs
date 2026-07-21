namespace GameData.Resources.Content;

using System.Collections.Generic;

/// <summary>Result of merging ordered content sources: the winning entry per key, the source that
/// won it (provenance), and every override that occurred.</summary>
public sealed class MergedCatalog<T> {
    public MergedCatalog(
        IReadOnlyDictionary<string, T> entries,
        IReadOnlyDictionary<string, string> provenance,
        IReadOnlyList<KeyOverride> overrides) {
        Entries = entries;
        Provenance = provenance;
        Overrides = overrides;
    }

    public IReadOnlyDictionary<string, T> Entries { get; }
    public IReadOnlyDictionary<string, string> Provenance { get; }
    public IReadOnlyList<KeyOverride> Overrides { get; }
    public int Count => Entries.Count;

    public bool TryGet(string key, out T value) => Entries.TryGetValue(key, out value!);
}
