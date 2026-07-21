namespace GameData.Resources.Content;

using System.Collections.Generic;

/// <summary>A source of content entries for one catalog. Synchronous by design: any async loading
/// (Addressables/provider/file) happens in the Unity binding BEFORE the entries are exposed here,
/// so the merge rule stays pure and engine-independent. <see cref="SourceName"/> is used for
/// provenance and override diagnostics.</summary>
public interface IContentSource<T> {
    string SourceName { get; }
    IReadOnlyList<ContentEntry<T>> Entries { get; }
}
