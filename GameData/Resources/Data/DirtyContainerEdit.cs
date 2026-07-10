namespace GameData.Resources.Data;

/// <summary>A looted container's changed bytes to patch in place: the numberOfItems byte and the
/// live item array (count*4 bytes). Everything else in the container is byte-unchanged by looting.</summary>
public sealed class DirtyContainerEdit {
    public int BodyOffset;       // absolute offset in the save BODY of this container's header
    public byte NumberOfItems;   // new live item count
    public byte[] LiveItemBytes; // NumberOfItems*4 bytes: each item = [ObjectId, Variable, ItemFlags&0xFF, ItemFlags>>8]
}
