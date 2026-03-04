namespace GameData.Resources.World;

public class WorldTile : IResource
{
    public WorldTile(string id) { Id = id; }
    public byte ZoneNumber { get; set; }
    public byte X { get; set; }
    public byte Y { get; set; }
    public List<WorldItem> Items { get; set; } = new();
    public ResourceType Type => ResourceType.WLD;
    public string Id { get; }
}
