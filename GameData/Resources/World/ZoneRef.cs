namespace GameData.Resources.World;

public class ZoneRef : IResource
{
    public ZoneRef(string id) { Id = id; }
    public List<TileCoordinate> Tiles { get; set; } = new();
    public ResourceType Type => ResourceType.DAT;
    public string Id { get; }
}
