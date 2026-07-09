namespace GameData.Resources.Object;

using System.Collections.Generic;

/// <summary>An IResource wrapper over the 138 OBJINFO.DAT item definitions, indexable by object id.</summary>
public class ObjectInfoSet : IResource {
    private readonly Dictionary<int, ObjectInfo> _byId;
    public ObjectInfoSet(string id, IReadOnlyList<ObjectInfo> items) {
        Id = id; Items = items;
        _byId = new Dictionary<int, ObjectInfo>();
        foreach (ObjectInfo o in items) { _byId[o.Number] = o; }
    }
    public IReadOnlyList<ObjectInfo> Items { get; }
    public ObjectInfo GetById(int objectId) => _byId.TryGetValue(objectId, out ObjectInfo o) ? o : null;
    public string Id { get; }
    public ResourceType Type => ResourceType.DAT;
}
