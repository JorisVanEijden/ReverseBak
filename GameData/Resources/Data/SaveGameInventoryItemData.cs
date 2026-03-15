namespace GameData.Resources.Data;

public class SaveGameInventoryItemData {
    public SaveGameInventoryItemData(
        byte objectId,
        byte variable,
        ushort itemFlags
    ) {
        ObjectId = objectId;
        Variable = variable;
        ItemFlags = itemFlags;
    }

    public byte ObjectId { get; }
    public byte Variable { get; }
    public ushort ItemFlags { get; }
}
