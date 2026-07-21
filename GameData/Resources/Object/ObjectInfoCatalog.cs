namespace GameData.Resources.Object;

using GameData.Resources.Content;

/// <summary>A numeric-id view over a merged item catalog, preserving the legacy
/// <see cref="ObjectInfoSet.GetById"/> surface for existing consumers (spec §5.1 "both views").
/// Archive originals resolve by their numeric id via the canonical <c>base:objinfo:&lt;id&gt;</c>
/// key; mod-added items have no numeric slot and are addressed by their string key on
/// <see cref="Merged"/>.</summary>
public sealed class ObjectInfoCatalog {
    private readonly MergedCatalog<ObjectInfo> _merged;

    public ObjectInfoCatalog(MergedCatalog<ObjectInfo> merged) {
        _merged = merged;
    }

    /// <summary>The full string-keyed merged catalog (originals + mod-added).</summary>
    public MergedCatalog<ObjectInfo> Merged => _merged;

    /// <summary>The original <see cref="ObjectInfoSet.GetById"/> behaviour: returns the archive
    /// original with numeric <paramref name="objectId"/>, or <c>null</c> if absent.</summary>
    public ObjectInfo? GetById(int objectId) =>
        _merged.TryGet(ContentKey.ForBase(ObjectInfoContentSource.Catalog, objectId), out ObjectInfo o) ? o : null;
}
