namespace ResourceExtractor.Extractors.Container;

using GameData;

public class InventoryItem {
    public int ObjectId { get; set; }
    public int Quantity { get; set; }
    public ItemFlags Flags { get; set; }
}