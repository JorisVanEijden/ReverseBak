namespace GameData.Resources.Content;

using System.Collections.Generic;

/// <summary>The additive-merge rule: fold an ordered list of sources into one keyed catalog.
/// New key adds; existing key is overridden by the later source (later-source-wins), and the
/// override is recorded. Pure and synchronous — see the design spec §4/§5.</summary>
public static class ContentRegistry {
    public static MergedCatalog<T> Merge<T>(IReadOnlyList<IContentSource<T>> orderedSources) {
        var map = new Dictionary<string, T>();
        var provenance = new Dictionary<string, string>();
        var overrides = new List<KeyOverride>();

        foreach (IContentSource<T> source in orderedSources) {
            foreach (ContentEntry<T> entry in source.Entries) {
                if (provenance.TryGetValue(entry.Key, out string? from)) {
                    overrides.Add(new KeyOverride(entry.Key, from, source.SourceName));
                }
                map[entry.Key] = entry.Value;
                provenance[entry.Key] = source.SourceName;
            }
        }

        return new MergedCatalog<T>(map, provenance, overrides);
    }
}
