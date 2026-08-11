namespace GameData.Resources.World;

public class WorldTile : IResource
{
    public WorldTile(string id) { Id = id; }
    public byte ZoneNumber { get; set; }
    public byte X { get; set; }
    public byte Y { get; set; }
    public List<WorldItem> Items { get; set; } = new();

    /// <summary>
    /// Records read from the file but discarded as unplaceable — their position lay far outside
    /// this tile, which only corrupt data does. Non-zero for exactly one shipped tile (T091011,
    /// 8 records); everywhere else this is 0 and says so out loud, rather than the extractor
    /// dropping data silently.
    /// </summary>
    public int DiscardedItems { get; set; }
    public ResourceType Type => ResourceType.WLD;
    public string Id { get; }
}
