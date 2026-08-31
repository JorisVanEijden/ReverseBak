namespace GameData.Resources.Object;

using GameData.Resources.Content;

/// <summary>A numeric-id view over a merged item catalog, preserving the legacy
/// <see cref="ObjectInfoSet.GetById"/> surface for existing consumers (spec §5.1 "both views").
/// Anything keyed <c>base:objinfo:&lt;id&gt;</c> resolves by that numeric id — which since
/// TASK-259 includes MOD-ADDED items, because the mod source keys an added item by its own
/// <c>Number</c> and so gives it a real numeric slot. That is deliberate: every gameplay caller
/// addresses items by object id, so an item without one would be unreachable. Content keyed
/// outside the base namespace has no numeric slot and is addressed by string key on
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
