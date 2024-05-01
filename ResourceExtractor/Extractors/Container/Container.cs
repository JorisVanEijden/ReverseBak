namespace ResourceExtractor.Extractors.Container;

using GameData.Resources;

public class Container : IResource {
    public int Zone { get; set; }
    public int MinChapter { get; set; }
    public int MaxChapter { get; set; }
    public int WorldItemId { get; set; }
    public uint XPosition { get; set; }
    public uint YPosition { get; set; }
    public ContainerTypes ContainerType { get; set; }
    public int NumberOfItems { get; set; }
    public int Capacity { get; set; }
    public List<InventoryItem>? Items { get; set; }
    public LockData? LockData { get; set; }
    public DialogData? DialogData { get; set; }
    public ShopData? ShopData { get; set; }
    public EncounterData? EncounterData { get; set; }
    public uint Timestamp { get; set; }
    public int Unknown20 { get; set; }
    public ResourceType Type { get => ResourceType.DAT; }
}