namespace GameData.Resources.World;

public class ZoneBounds : IResource {
    public ZoneBounds(string id) { Id = id; }
    public ushort XOffset { get; set; }
    public ushort YOffset { get; set; }
    public ushort Width { get; set; }
    public ushort Height { get; set; }
    public ResourceType Type => ResourceType.DAT;
    public string Id { get; }
}
