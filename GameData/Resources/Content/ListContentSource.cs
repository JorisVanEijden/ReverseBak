namespace GameData.Resources.Content;

using System.Collections.Generic;

/// <summary>A concrete <see cref="IContentSource{T}"/> backed by an already-materialized list of
/// entries. The Unity bindings (folder / shipped-JSON mod partials) load async, then wrap the
/// loaded entries in one of these to hand to the pure merge rule.</summary>
public sealed class ListContentSource<T> : IContentSource<T> {
    public ListContentSource(string sourceName, IReadOnlyList<ContentEntry<T>> entries) {
        SourceName = sourceName;
        Entries = entries;
    }

    public string SourceName { get; }
    public IReadOnlyList<ContentEntry<T>> Entries { get; }
}
