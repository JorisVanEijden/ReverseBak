namespace GameData.Resources.Object;

using System.Collections.Generic;
using GameData.Resources.Content;

/// <summary>Exposes an <see cref="ObjectInfoSet"/> as the base content source for the item catalog:
/// one <see cref="ContentEntry{T}"/> per item under its canonical <c>base:objinfo:&lt;Number&gt;</c>
/// key. The priority-0 source in the item registry (spec §7 proof-of-concept).</summary>
public sealed class ObjectInfoContentSource : IContentSource<ObjectInfo> {
    /// <summary>Catalog name used in canonical keys (<c>base:objinfo:&lt;index&gt;</c>).</summary>
    public const string Catalog = "objinfo";

    private readonly List<ContentEntry<ObjectInfo>> _entries;

    public ObjectInfoContentSource(ObjectInfoSet set) {
        SourceName = $"{ContentKey.BaseNamespace}:{Catalog}";
        _entries = new List<ContentEntry<ObjectInfo>>();
        foreach (ObjectInfo o in set.Items) {
            _entries.Add(new ContentEntry<ObjectInfo>(ContentKey.ForBase(Catalog, o.Number), o));
        }
    }

    public string SourceName { get; }
    public IReadOnlyList<ContentEntry<ObjectInfo>> Entries => _entries;
}
