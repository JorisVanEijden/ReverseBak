namespace GameData.Resources.Inventory;

/// <summary>Mutable runtime copy of a 4-byte inventoryItem (objectId, variable, itemFlags).</summary>
public sealed class RuntimeItem {
    public byte ObjectId;
    public byte Variable;
    public ushort ItemFlags;
    public RuntimeItem(byte objectId, byte variable, ushort itemFlags) {
        ObjectId = objectId; Variable = variable; ItemFlags = itemFlags;
    }
    public RuntimeItem Clone() => new RuntimeItem(ObjectId, Variable, ItemFlags);
}
